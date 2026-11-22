using System;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Seguir la ruta (historia 2.7, parte 1; FR19, FR20). Empieza al pulsar Ir, con la ruta ya calculada: enciende el
    /// GPS de fondo con la notificación "Navegando a &lt;destino&gt; · N min" (AD-19) y, con cada posición nueva de
    /// ServicioUbicacion (también las que se tomaron con la pantalla apagada, en orden), calcula con ReglaNavegacion la
    /// maniobra siguiente, su distancia y lo que queda, y avisa (Actualizado). Cambia el texto de la notificación cuando
    /// cambian los minutos.
    ///
    /// Desvío: más de 40 m de la ruta durante más de 5 s seguidos (ParametrosNegocio, con el reloj inyectado) recalcula
    /// desde la posición con ServicioRutas.Calcular (sin conexión, la aproximada). La ruta vieja sigue hasta tener la
    /// nueva; solo hay un recálculo a la vez; si falla, avisa (RecalculoFallido) y lo intenta otra vez a los 10 s si aún
    /// está desviado.
    ///
    /// Eventos para las visitas (AD-9; los consumirá ServicioVisitas en la épica 4): RutaIniciada, TiempoRestanteCambiado
    /// (al cambiar el minuto) y RutaTerminada. No crea visitas. Se usa desde el hilo principal (la UI).
    /// </summary>
    public sealed class ServicioNavegacion
    {
        public static readonly TimeSpan TiempoDesvio = TimeSpan.FromSeconds(ParametrosNegocio.NavegacionDesvioSegundos);
        public static readonly TimeSpan EsperaReintento = TimeSpan.FromSeconds(ParametrosNegocio.NavegacionReintentoSegundos);

        readonly ServicioUbicacion ubicacion;
        readonly ServicioRutas rutas;
        readonly Func<TimeSpan, CancellationToken, Task> esperar;
        readonly Func<DateTime> reloj;
        readonly Func<string, int, string> textoNotificacion;

        ReglaNavegacion regla;
        CancellationTokenSource sesion; // la navegación en curso; se cancela al terminar (recálculos y reintentos)
        Coordenada? ultimaPosicion;
        Coordenada? origenRuta; // desde donde se pidió la ruta que se sigue: desde ahí no se recalcula (daría la misma)
        DateTime? desvioDesde;
        int ultimoMinuto;

        /// <param name="ubicacion">Posiciones del usuario y GPS de fondo (IniciarRuta, ActualizarNotificacion, TerminarRuta).</param>
        /// <param name="rutas">Recálculo (Calcular: servicios y, sin conexión, la aproximada).</param>
        /// <param name="esperar">Espera cancelable del reintento (en la app, Task.Delay); las pruebas pasan una controlada.</param>
        /// <param name="reloj">Hora actual para el desvío (por defecto, DateTime.UtcNow).</param>
        /// <param name="textoNotificacion">Texto de la notificación con el destino y los minutos (la app lo saca de Localization).</param>
        public ServicioNavegacion(ServicioUbicacion ubicacion, ServicioRutas rutas, Func<TimeSpan, CancellationToken, Task> esperar,
            Func<DateTime> reloj = null, Func<string, int, string> textoNotificacion = null)
        {
            this.ubicacion = ubicacion ?? throw new ArgumentNullException(nameof(ubicacion));
            this.rutas = rutas ?? throw new ArgumentNullException(nameof(rutas));
            this.esperar = esperar ?? throw new ArgumentNullException(nameof(esperar));
            this.reloj = reloj ?? (() => DateTime.UtcNow);
            this.textoNotificacion = textoNotificacion ?? ((destino, minutos) => $"Navegando a {destino} · {minutos} min");
            this.ubicacion.Cambiado += AlCambiarUbicacion;
        }

        /// <summary>Hay una ruta en marcha (entre Empezar y Terminar).</summary>
        public bool Activa => regla != null;

        /// <summary>La ruta que se sigue (la recalculada, si la hay); null sin navegación.</summary>
        public Ruta Ruta => regla?.Ruta;

        /// <summary>La regla de la ruta que se sigue (para recortar la línea); null sin navegación.</summary>
        public ReglaNavegacion Regla => regla;

        /// <summary>Dónde va el usuario en la ruta; null sin navegación.</summary>
        public EstadoNavegacion Estado { get; private set; }

        /// <summary>Nombre del destino ("Clínica Areas").</summary>
        public string Destino { get; private set; } = "";

        /// <summary>Coordenada del destino (a donde va el recálculo); null sin navegación.</summary>
        public Coordenada? PuntoDestino { get; private set; }

        public ModoViaje Modo { get; private set; }

        /// <summary>Hay un recálculo en marcha o esperando su reintento.</summary>
        public bool Recalculando { get; private set; }

        /// <summary>El GPS de fondo arrancó al empezar (false: de momento solo con la app delante, o sin servicio).</summary>
        public bool GpsDeFondo { get; private set; }

        /// <summary>Hay un estado nuevo (posición nueva, o ruta nueva al empezar o al recalcular).</summary>
        public event Action Actualizado;

        /// <summary>La ruta se ha recalculado (Ruta es la nueva); llega antes de su Actualizado.</summary>
        public event Action RutaCambiada;

        /// <summary>Un recálculo ha fallado; se intenta otra vez a los 10 s.</summary>
        public event Action RecalculoFallido;

        /// <summary>Empieza una ruta (AD-9): el nombre del destino.</summary>
        public event Action<string> RutaIniciada;

        /// <summary>Cambian los minutos que quedan (AD-9): los segundos que quedan.</summary>
        public event Action<double> TiempoRestanteCambiado;

        /// <summary>Termina la ruta (AD-9) y por qué.</summary>
        public event Action<MotivoFinRuta> RutaTerminada;

        /// <summary>
        /// Empieza a seguir la ruta hacia el destino (Ir). Con otra en marcha, la termina antes (Manual). El primer
        /// estado sale de la posición actual (sin ella, del principio de la ruta).
        /// </summary>
        public void Empezar(Ruta ruta, Coordenada destino, string nombreDestino, ModoViaje modo)
        {
            if (ruta == null) throw new ArgumentNullException(nameof(ruta));
            if (Activa) Terminar(MotivoFinRuta.Manual);
            sesion = new CancellationTokenSource();
            regla = new ReglaNavegacion(ruta);
            Destino = nombreDestino ?? "";
            PuntoDestino = destino;
            Modo = modo;
            Recalculando = false;
            desvioDesde = null;
            ultimaPosicion = ubicacion.Posicion;
            origenRuta = ultimaPosicion;
            Estado = regla.Seguir(ultimaPosicion ?? (ruta.Puntos.Count > 0 ? ruta.Puntos[0] : destino));
            ultimoMinuto = Estado.MinutosRestantes;
            var r = ubicacion.IniciarRuta(Notificacion(ultimoMinuto));
            GpsDeFondo = r.Ok && r.Valor;
            RutaIniciada?.Invoke(Destino);
            Actualizado?.Invoke();
        }

        /// <summary>Termina la ruta: fuera el GPS de fondo y la notificación, y RutaTerminada. Sin ruta, nada.</summary>
        public void Terminar(MotivoFinRuta motivo = MotivoFinRuta.Manual)
        {
            if (!Activa) return;
            var s = sesion;
            sesion = null;
            s?.Cancel(); // sin Dispose: el recálculo cancelado aún mira su token
            regla = null;
            Estado = null;
            PuntoDestino = null;
            Recalculando = false;
            desvioDesde = null;
            ultimaPosicion = null;
            origenRuta = null;
            GpsDeFondo = false;
            ubicacion.TerminarRuta();
            RutaTerminada?.Invoke(motivo);
        }

        // ServicioUbicacion avisa también del rumbo: solo cuenta una posición distinta
        void AlCambiarUbicacion()
        {
            if (!Activa) return;
            var posicion = ubicacion.Posicion;
            if (!posicion.HasValue || posicion == ultimaPosicion) return;
            Seguir(posicion.Value);
        }

        void Seguir(Coordenada posicion)
        {
            ultimaPosicion = posicion;
            Estado = regla.Seguir(posicion, Estado);
            var ahora = reloj();
            if (Estado.FueraDeRuta)
            {
                if (!desvioDesde.HasValue || ahora < desvioDesde.Value) desvioDesde = ahora;
                // Lejos de una calle (la ruta empieza a más de 40 m): desde el mismo sitio saldría la misma ruta otra vez
                bool desdeOtroSitio = !origenRuta.HasValue
                    || origenRuta.Value.DistanciaMetros(posicion) > ParametrosNegocio.NavegacionDesvioMetros;
                if (!Recalculando && desdeOtroSitio && ahora - desvioDesde.Value > TiempoDesvio) _ = Recalcular(posicion, sesion);
            }
            else desvioDesde = null;
            if (!Activa) return;
            AvisarMinuto();
            Actualizado?.Invoke();
        }

        /// <summary>
        /// Ruta nueva desde la posición; la vieja se sigue viendo hasta tenerla. Si falla, RecalculoFallido y, a los 10 s,
        /// otra vez si aún está desviado. Terminar lo cancela.
        /// </summary>
        async Task Recalcular(Coordenada desde, CancellationTokenSource s)
        {
            if (s == null || !PuntoDestino.HasValue) return;
            Recalculando = true;
            var destino = PuntoDestino.Value;
            while (true)
            {
                Resultado<Ruta> r;
                try
                {
                    r = await rutas.Calcular(desde, destino, Modo, s.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    r = Resultado<Ruta>.Fallo(string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message);
                }
                if (s.IsCancellationRequested || !ReferenceEquals(s, sesion)) return;
                if (Estado != null && !Estado.FueraDeRuta)
                {
                    Recalculando = false; // ha vuelto a la ruta mientras se pedía: se sigue con la de antes
                    return;
                }
                if (r.Ok && r.Valor != null && r.Valor.Puntos.Count >= 2)
                {
                    Recalculando = false;
                    desvioDesde = null;
                    origenRuta = desde;
                    regla = new ReglaNavegacion(r.Valor);
                    Estado = regla.Seguir(ultimaPosicion ?? desde);
                    RutaCambiada?.Invoke();
                    if (!ReferenceEquals(s, sesion)) return; // terminada desde el aviso
                    AvisarMinuto();
                    Actualizado?.Invoke();
                    return;
                }

                RecalculoFallido?.Invoke();
                try
                {
                    await esperar(EsperaReintento, s.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                if (s.IsCancellationRequested || !ReferenceEquals(s, sesion)) return;
                if (Estado == null || !Estado.FueraDeRuta)
                {
                    Recalculando = false; // ha vuelto a la ruta: ya no hace falta
                    return;
                }
                desde = ultimaPosicion ?? desde;
            }
        }

        void AvisarMinuto()
        {
            if (Estado == null) return;
            int minutos = Estado.MinutosRestantes;
            if (minutos == ultimoMinuto) return;
            ultimoMinuto = minutos;
            ubicacion.ActualizarNotificacion(Notificacion(minutos));
            TiempoRestanteCambiado?.Invoke(Estado.SegundosRestantes);
        }

        string Notificacion(int minutos) => textoNotificacion(Destino, minutos);
    }
}
