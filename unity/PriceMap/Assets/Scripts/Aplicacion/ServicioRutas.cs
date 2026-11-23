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
    /// 20 m y el destino y el modo sean los mismos (ParametrosNegocio). Si los dos fallan y hay servicio local (parte 2b:
    /// las calles guardadas, sin red), se le pide la ruta aproximada; esa no se guarda en la caché, para que al volver la
    /// red la siguiente petición vaya a los servicios.
    ///
    /// Alternativas (parte 5a de «Cómo llegar») da hasta tres rutas para elegir, por tiempo, con los mismos pasos, el
    /// mismo límite por servicio y las mismas reglas de caché.
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
        readonly IRutas local;
        readonly Func<TimeSpan, CancellationToken, Task> esperar;
        readonly Func<DateTime> reloj;
        readonly List<Guardada<Ruta>> cache = new List<Guardada<Ruta>>();
        readonly List<Guardada<IReadOnlyList<Ruta>>> cacheAlternativas = new List<Guardada<IReadOnlyList<Ruta>>>();
        readonly Dictionary<ModoViaje, Ruta> ultimas = new Dictionary<ModoViaje, Ruta>();
        CancellationTokenSource actual; // los tiempos en curso; null si no hay ninguno
        int pendientes;

        sealed class Guardada<T>
        {
            public Coordenada Origen, Destino;
            public ModoViaje Modo;
            public DateTime Hora;
            public T Valor;
        }

        /// <summary>Tiempo de un modo para el destino pedido: la ruta o el fallo (el modo queda sin tiempo).</summary>
        public event Action<ModoViaje, Coordenada, Resultado<Ruta>> TiempoCalculado;

        /// <param name="principal">Servicio de rutas principal (Valhalla).</param>
        /// <param name="reserva">Servicio de reserva (OSRM); null si no hay.</param>
        /// <param name="esperar">Espera cancelable (en la app, Task.Delay); las pruebas pasan una controlada.</param>
        /// <param name="reloj">Hora actual para la caché (por defecto, DateTime.UtcNow).</param>
        /// <param name="local">Ruta aproximada por las calles guardadas si fallan los dos (parte 2b); null si no hay.</param>
        public ServicioRutas(IRutas principal, IRutas reserva, Func<TimeSpan, CancellationToken, Task> esperar, Func<DateTime> reloj = null,
            IRutas local = null)
        {
            this.local = local;
            this.principal = principal ?? throw new ArgumentNullException(nameof(principal));
            this.reserva = reserva;
            this.esperar = esperar ?? throw new ArgumentNullException(nameof(esperar));
            this.reloj = reloj ?? (() => DateTime.UtcNow);
        }

        /// <summary>
        /// Hay servicio de ruta aproximada (parte 2b): un fallo de Calcular quiere decir que tampoco hay calles guardadas
        /// que unan el origen y el destino.
        /// </summary>
        public bool ConRutaAproximada => local != null;

        /// <summary>Hay tiempos pedidos sin respuesta.</summary>
        public bool Pendiente => actual != null && pendientes > 0;

        /// <summary>Destino de los últimos tiempos pedidos; null si no hay o se cancelaron.</summary>
        public Coordenada? Destino { get; private set; }

        /// <summary>Última ruta llegada de ese modo para el destino actual; null si no ha llegado o falló.</summary>
        public Ruta Ultima(ModoViaje modo) => ultimas.TryGetValue(modo, out var r) ? r : null;

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
        /// Cancela los tiempos en curso (de ellos no llegará ningún aviso): la ficha se cierra o cambia.
        /// </summary>
        public void Cancelar()
        {
            ultimas.Clear();
            Destino = null;
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
            }
            TiempoCalculado?.Invoke(modo, destino, r);
        }

        /// <summary>
        /// La ruta de siempre en ese modo (en coche, la más rápida): de la caché, del principal o del de reserva y, si los
        /// dos fallan, la aproximada del servicio local (sin guardarla). Fallo si ninguno la da; cancelar termina en
        /// OperationCanceledException.
        /// </summary>
        public async Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var guardada = Buscar(cache, origen, destino, modo);
            if (guardada != null) return Resultado<Ruta>.Exito(guardada);

            var (r, delMovil) = await PorOrden((s, t) => s.Calcular(origen, destino, modo, t), ruta => ruta != null, ct);
            if (r.Ok && !delMovil) Guardar(cache, origen, destino, modo, r.Valor);
            return r;
        }

        /// <summary>
        /// Hasta tres rutas distintas para elegir, de menos a más tiempo, con los mismos pasos que Calcular; si fallan los
        /// dos servicios, la aproximada sola. Al llegar, la primera queda como la ruta guardada de Calcular si no tenía ya
        /// una. Fallo si ninguno da rutas; cancelar termina en OperationCanceledException.
        /// </summary>
        public async Task<Resultado<IReadOnlyList<Ruta>>> Alternativas(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var guardadas = Buscar(cacheAlternativas, origen, destino, modo);
            if (guardadas != null) return Resultado<IReadOnlyList<Ruta>>.Exito(guardadas);

            var (r, delMovil) = await PorOrden((s, t) => s.Alternativas(origen, destino, modo, t), rutas => rutas != null && rutas.Count > 0, ct);
            if (r.Ok && !delMovil)
            {
                Guardar(cacheAlternativas, origen, destino, modo, r.Valor);
                if (Buscar(cache, origen, destino, modo) == null) Guardar(cache, origen, destino, modo, r.Valor[0]);
            }
            return r;
        }

        /// <summary>
        /// Pregunta al principal, al de reserva y al local, por ese orden, hasta que uno dé ruta. delMovil: la ha dado el
        /// local (aproximada: no va a la caché).
        /// </summary>
        async Task<(Resultado<T> r, bool delMovil)> PorOrden<T>(Func<IRutas, CancellationToken, Task<Resultado<T>>> pedir,
            Func<T, bool> hayRuta, CancellationToken ct)
        {
            var r = await ConLimite(principal, pedir, hayRuta, ct);
            if (!r.Ok && reserva != null)
            {
                var r2 = await ConLimite(reserva, pedir, hayRuta, ct);
                r = r2.Ok ? r2 : Resultado<T>.Fallo(r.Error + " Reserva: " + r2.Error);
            }
            ct.ThrowIfCancellationRequested();
            if (r.Ok || local == null) return (r, false);
            var r3 = await ConLimite(local, pedir, hayRuta, ct);
            ct.ThrowIfCancellationRequested();
            return r3.Ok ? (r3, true) : (Resultado<T>.Fallo(r.Error + " Calles guardadas: " + r3.Error), true);
        }

        /// <summary>Pregunta a un servicio con el tiempo máximo; pasado, si lanza o si no da ninguna ruta, es un fallo.</summary>
        async Task<Resultado<T>> ConLimite<T>(IRutas rutas, Func<IRutas, CancellationToken, Task<Resultado<T>>> pedir,
            Func<T, bool> hayRuta, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task<Resultado<T>> peticion;
            try { peticion = pedir(rutas, limite.Token); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e) { return Resultado<T>.Fallo(Mensaje(e)); }
            if (peticion == null) return Resultado<T>.Fallo("El servicio de rutas no ha devuelto ninguna tarea.");

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
                    return Resultado<T>.Fallo($"El servicio de rutas no responde en {ParametrosNegocio.RutaTiempoMaximoSegundos} s.");
                }
            }

            ct.ThrowIfCancellationRequested();
            try
            {
                var r = await peticion;
                return !r.Ok || hayRuta(r.Valor) ? r : Resultado<T>.Fallo("El servicio de rutas no ha dado ninguna ruta.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                return Resultado<T>.Fallo(Mensaje(e));
            }
        }

        /// <summary>Una tarea que ya no se espera no deja su excepción sin observar.</summary>
        static void Observar(Task t) =>
            t?.ContinueWith(x => _ = x.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        T Buscar<T>(List<Guardada<T>> en, Coordenada origen, Coordenada destino, ModoViaje modo) where T : class
        {
            var ahora = reloj();
            en.RemoveAll(g => ahora - g.Hora >= DuracionCache || ahora < g.Hora);
            foreach (var g in en)
                if (g.Modo == modo && g.Destino == destino && g.Origen.DistanciaMetros(origen) <= ParametrosNegocio.RutaCacheMetros)
                    return g.Valor;
            return null;
        }

        void Guardar<T>(List<Guardada<T>> en, Coordenada origen, Coordenada destino, ModoViaje modo, T valor)
        {
            en.RemoveAll(g => g.Modo == modo && g.Destino == destino && g.Origen.DistanciaMetros(origen) <= ParametrosNegocio.RutaCacheMetros);
            en.Add(new Guardada<T> { Origen = origen, Destino = destino, Modo = modo, Hora = reloj(), Valor = valor });
        }

        /// <summary>Rutas de Calcular guardadas en la caché (para las pruebas).</summary>
        public int EnCache => cache.Count;

        /// <summary>Vacía la caché (la captura de PC, para que la ruta sin red no salga de una guardada antes).</summary>
        public void VaciarCache()
        {
            cache.Clear();
            cacheAlternativas.Clear();
        }

        static string Mensaje(Exception e) => string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message;
    }
}
