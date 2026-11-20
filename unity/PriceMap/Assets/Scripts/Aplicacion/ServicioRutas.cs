using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Rutas y tiempos de la ficha (historia 2.6; FR7, FR10, FR18). Calcular pregunta al servicio principal
    /// (Valhalla) y, si falla o tarda más de 8 s, al de reserva (OSRM); si los dos fallan, devuelve el fallo. Las
    /// rutas buenas se guardan en una caché en memoria durante 2 min, y valen mientras el origen no se mueva más de
    /// 20 m y el destino y el modo sean los mismos (ParametrosNegocio).
    ///
    /// PedirTiempos pide coche y a pie a la vez y avisa de cada uno (TiempoCalculado). Cada petición, y Cancelar,
    /// cancela las anteriores: de ellas no llega ningún aviso. Se usa desde el hilo principal (la UI).
    /// </summary>
    public sealed class ServicioRutas
    {
        public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(ParametrosNegocio.RutaTiempoMaximoSegundos);
        public static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(ParametrosNegocio.RutaCacheMinutos);

        static readonly ModoViaje[] Modos = { ModoViaje.Coche, ModoViaje.APie };

        readonly IRutas principal;
        readonly IRutas reserva;
        readonly Func<TimeSpan, CancellationToken, Task> esperar;
        readonly Func<DateTime> reloj;
        readonly List<Guardada> cache = new List<Guardada>();
        readonly Dictionary<ModoViaje, Ruta> ultimas = new Dictionary<ModoViaje, Ruta>();
        CancellationTokenSource actual; // los tiempos en curso; null si no hay ninguno
        int pendientes;

        sealed class Guardada
        {
            public Coordenada Origen, Destino;
            public ModoViaje Modo;
            public DateTime Hora;
            public Ruta Ruta;
        }

        /// <summary>Tiempo de un modo para el destino pedido: la ruta o el fallo (el modo queda sin tiempo).</summary>
        public event Action<ModoViaje, Coordenada, Resultado<Ruta>> TiempoCalculado;

        /// <param name="principal">Servicio de rutas principal (Valhalla).</param>
        /// <param name="reserva">Servicio de reserva (OSRM); null si no hay.</param>
        /// <param name="esperar">Espera cancelable (en la app, Task.Delay); las pruebas pasan una controlada.</param>
        /// <param name="reloj">Hora actual para la caché (por defecto, DateTime.UtcNow).</param>
        public ServicioRutas(IRutas principal, IRutas reserva, Func<TimeSpan, CancellationToken, Task> esperar, Func<DateTime> reloj = null)
        {
            this.principal = principal ?? throw new ArgumentNullException(nameof(principal));
            this.reserva = reserva;
            this.esperar = esperar ?? throw new ArgumentNullException(nameof(esperar));
            this.reloj = reloj ?? (() => DateTime.UtcNow);
        }

        /// <summary>Hay tiempos pedidos sin respuesta.</summary>
        public bool Pendiente => actual != null && pendientes > 0;

        /// <summary>Destino de los últimos tiempos pedidos; null si no hay o se cancelaron.</summary>
        public Coordenada? Destino { get; private set; }

        /// <summary>Última ruta llegada de ese modo para el destino actual; null si no ha llegado o falló.</summary>
        public Ruta Ultima(ModoViaje modo) => ultimas.TryGetValue(modo, out var r) ? r : null;

        /// <summary>
        /// Modo, destino y ruta que dejó elegidos el botón Ir (ElegirParaIr); la parte 2 la dibuja. Null hasta entonces.
        /// </summary>
        public ModoViaje? ModoElegido { get; private set; }
        public Coordenada? DestinoElegido { get; private set; }
        public Ruta RutaElegida { get; private set; }

        /// <summary>
        /// Ir con posición (parte 1): deja el modo, el destino y su ruta listos para dibujarla. Si la ruta aún no ha
        /// llegado, se pone al llegar. Cancelar lo olvida.
        /// </summary>
        public void ElegirParaIr(ModoViaje modo, Coordenada destino)
        {
            ModoElegido = modo;
            DestinoElegido = destino;
            RutaElegida = Destino == destino ? Ultima(modo) : null;
        }

        /// <summary>Pide coche y a pie a la vez para ese destino; cancela lo anterior.</summary>
        public void PedirTiempos(Coordenada origen, Coordenada destino)
        {
            Cancelar();
            var cts = new CancellationTokenSource();
            actual = cts;
            Destino = destino;
            pendientes = Modos.Length;
            foreach (var modo in Modos) _ = Tiempo(origen, destino, modo, cts);
        }

        /// <summary>
        /// Cancela los tiempos en curso (de ellos no llegará ningún aviso) y olvida lo elegido con Ir: la ficha se
        /// cierra o cambia, y su ruta ya no se dibuja.
        /// </summary>
        public void Cancelar()
        {
            ultimas.Clear();
            Destino = null;
            ModoElegido = null;
            DestinoElegido = null;
            RutaElegida = null;
            pendientes = 0;
            if (actual == null) return;
            var cts = actual;
            actual = null;
            cts.Cancel(); // sin Dispose: las tareas canceladas aún miran su token
        }

        async Task Tiempo(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationTokenSource cts)
        {
            Resultado<Ruta> r;
            try
            {
                r = await Calcular(origen, destino, modo, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                r = Resultado<Ruta>.Fallo(Mensaje(e));
            }
            // Una respuesta tardía (cancelada o de otro destino) no se pinta nunca
            if (cts.IsCancellationRequested || !ReferenceEquals(actual, cts)) return;
            pendientes--;
            if (r.Ok && r.Valor != null)
            {
                ultimas[modo] = r.Valor;
                // Ir se pulsó antes de que llegara: la ruta del modo y destino elegidos queda lista para dibujarla
                if (ModoElegido == modo && DestinoElegido == destino) RutaElegida = r.Valor;
            }
            TiempoCalculado?.Invoke(modo, destino, r);
        }

        /// <summary>
        /// La ruta más corta en ese modo: de la caché, del principal o del de reserva. Fallo si ninguno la da; cancelar
        /// termina en OperationCanceledException.
        /// </summary>
        public async Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var guardada = Buscar(origen, destino, modo);
            if (guardada != null) return Resultado<Ruta>.Exito(guardada);

            var r = await ConLimite(principal, origen, destino, modo, ct);
            if (!r.Ok && reserva != null)
            {
                var r2 = await ConLimite(reserva, origen, destino, modo, ct);
                r = r2.Ok ? r2 : Resultado<Ruta>.Fallo(r.Error + " Reserva: " + r2.Error);
            }
            ct.ThrowIfCancellationRequested();
            if (r.Ok && r.Valor == null) r = Resultado<Ruta>.Fallo("El servicio de rutas no ha dado ninguna ruta.");
            if (r.Ok) Guardar(origen, destino, modo, r.Valor);
            return r;
        }

        /// <summary>Pregunta a un servicio con el tiempo máximo; pasado, o si lanza, es un fallo.</summary>
        async Task<Resultado<Ruta>> ConLimite(IRutas rutas, Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task<Resultado<Ruta>> peticion;
            try { peticion = rutas.Calcular(origen, destino, modo, limite.Token); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e) { return Resultado<Ruta>.Fallo(Mensaje(e)); }
            if (peticion == null) return Resultado<Ruta>.Fallo("El servicio de rutas no ha devuelto ninguna tarea.");

            if (!peticion.IsCompleted)
            {
                var espera = esperar(TiempoMaximo, limite.Token);
                var primera = await Task.WhenAny(peticion, espera);
                limite.Cancel(); // se para la espera o la petición que siga en marcha
                Observar(espera);
                if (primera != peticion)
                {
                    ct.ThrowIfCancellationRequested();
                    Observar(peticion);
                    return Resultado<Ruta>.Fallo($"El servicio de rutas no responde en {ParametrosNegocio.RutaTiempoMaximoSegundos} s.");
                }
            }

            ct.ThrowIfCancellationRequested();
            try
            {
                return await peticion;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                return Resultado<Ruta>.Fallo(Mensaje(e));
            }
        }

        /// <summary>Una tarea que ya no se espera no deja su excepción sin observar.</summary>
        static void Observar(Task t) =>
            t?.ContinueWith(x => _ = x.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        Ruta Buscar(Coordenada origen, Coordenada destino, ModoViaje modo)
        {
            var ahora = reloj();
            cache.RemoveAll(g => ahora - g.Hora >= DuracionCache || ahora < g.Hora);
            foreach (var g in cache)
                if (g.Modo == modo && g.Destino == destino && g.Origen.DistanciaMetros(origen) <= ParametrosNegocio.RutaCacheMetros)
                    return g.Ruta;
            return null;
        }

        void Guardar(Coordenada origen, Coordenada destino, ModoViaje modo, Ruta ruta)
        {
            cache.RemoveAll(g => g.Modo == modo && g.Destino == destino && g.Origen.DistanciaMetros(origen) <= ParametrosNegocio.RutaCacheMetros);
            cache.Add(new Guardada { Origen = origen, Destino = destino, Modo = modo, Hora = reloj(), Ruta = ruta });
        }

        /// <summary>Rutas guardadas en la caché (para las pruebas).</summary>
        public int EnCache => cache.Count;

        static string Mensaje(Exception e) => string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message;
    }
}
