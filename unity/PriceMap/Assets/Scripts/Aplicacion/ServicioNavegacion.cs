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
    /// Sin señal (parte 2c): Comprobar (la UI lo llama en cada fotograma) mira si han pasado más de 10 s sin una lectura
    /// nueva del GPS (IUbicacion.Lecturas o una posición distinta; repetir la coordenada no es perder la señal). Entonces
    /// SinSenal, SenalCambiada y el contador del desvío a cero: no se recalcula hasta la primera lectura nueva, que lo
    /// deshace (y el desvío cuenta desde cero).
    ///
    /// Llegar (historia 2.9): con una posición a 30 m o menos del destino o del final de la línea (ReglaNavegacion.HaLlegado),
    /// Llegado y HaLlegado, una vez por ruta: se cancela el recálculo, la notificación pasa a "Has llegado a &lt;destino&gt;" y
    /// desde ahí las posiciones solo avisan (Actualizado, para la cámara), sin maniobras, desvíos ni señal. La ruta sigue
    /// Activa (con el GPS de fondo) hasta Finalizar o hasta que Comprobar la cierra al minuto (Automatico).
    ///
    /// Eventos para las visitas (AD-9; los consumirá ServicioVisitas en la épica 4): RutaIniciada, TiempoRestanteCambiado
    /// (al cambiar el minuto), HaLlegado y RutaTerminada. No crea visitas. Se usa desde el hilo principal (la UI).
    /// </summary>
    public sealed class ServicioNavegacion
    {
        public static readonly TimeSpan TiempoDesvio = TimeSpan.FromSeconds(ParametrosNegocio.NavegacionDesvioSegundos);
        public static readonly TimeSpan EsperaReintento = TimeSpan.FromSeconds(ParametrosNegocio.NavegacionReintentoSegundos);
        public static readonly TimeSpan TiempoSinSenal = TimeSpan.FromSeconds(ParametrosNegocio.NavegacionSinSenalSegundos);
        public static readonly TimeSpan TiempoCierre = TimeSpan.FromSeconds(ParametrosNegocio.LlegadaCierreSegundos);
        /// <summary>Hueco entre dos Comprobar a partir del cual la app estaba en segundo plano (no cuenta como sin señal).</summary>
        static readonly TimeSpan PausaComprobacion = TimeSpan.FromSeconds(1);

        readonly ServicioUbicacion ubicacion;
        readonly ServicioRutas rutas;
        readonly Func<TimeSpan, CancellationToken, Task> esperar;
        readonly Func<DateTime> reloj;
        readonly Func<string, int, string> textoNotificacion;
        readonly Func<string, string> textoLlegada;

        ReglaNavegacion regla;
        CancellationTokenSource sesion; // la navegación en curso; se cancela al terminar (recálculos y reintentos)
        Coordenada? ultimaPosicion;
        Coordenada? origenRuta; // desde donde se pidió la ruta que se sigue: desde ahí no se recalcula (daría la misma)
        DateTime? desvioDesde;
        int ultimoMinuto;
        DateTime ultimaLectura; // hora de la última lectura del GPS (o de Empezar)
        int lecturasVistas;     // IUbicacion.Lecturas la última vez que se miró
        DateTime ultimaComprobacion; // la última llamada a Comprobar (o Empezar)
        DateTime horaLlegada;        // con Llegado: cuándo (la ruta se cierra sola al minuto)

        /// <param name="ubicacion">Posiciones del usuario y GPS de fondo (IniciarRuta, ActualizarNotificacion, TerminarRuta).</param>
        /// <param name="rutas">Recálculo (Calcular: servicios y, sin conexión, la aproximada).</param>
        /// <param name="esperar">Espera cancelable del reintento (en la app, Task.Delay); las pruebas pasan una controlada.</param>
        /// <param name="reloj">Hora actual para el desvío (por defecto, DateTime.UtcNow).</param>
        /// <param name="textoNotificacion">Texto de la notificación con el destino y los minutos (la app lo saca de Localization).</param>
        /// <param name="textoLlegada">Texto de la notificación al llegar, con el destino (la app lo saca de Localization).</param>
        public ServicioNavegacion(ServicioUbicacion ubicacion, ServicioRutas rutas, Func<TimeSpan, CancellationToken, Task> esperar,
            Func<DateTime> reloj = null, Func<string, int, string> textoNotificacion = null, Func<string, string> textoLlegada = null)
        {
            this.ubicacion = ubicacion ?? throw new ArgumentNullException(nameof(ubicacion));
            this.rutas = rutas ?? throw new ArgumentNullException(nameof(rutas));
            this.esperar = esperar ?? throw new ArgumentNullException(nameof(esperar));
            this.reloj = reloj ?? (() => DateTime.UtcNow);
            this.textoNotificacion = textoNotificacion ?? ((destino, minutos) => $"Navegando a {destino} · {minutos} min");
            this.textoLlegada = textoLlegada ?? (destino => $"Has llegado a {destino}");
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

        /// <summary>
        /// Navegando, más de 10 s sin una lectura nueva del GPS: la posición se queda atenuada en su último sitio, sale
        /// "Buscando señal GPS" y no se recalcula. Vuelve a false con la primera lectura nueva o al terminar.
        /// </summary>
        public bool SinSenal { get; private set; }

        /// <summary>Se ha llegado al destino y la ruta aún no ha terminado (historia 2.9).</summary>
        public bool Llegado { get; private set; }

        /// <summary>Se llega al destino, una vez por ruta: el nombre del destino (historia 2.9; lo usará la épica 4).</summary>
        public event Action<string> HaLlegado;

        /// <summary>SinSenal ha cambiado navegando (se pierde o vuelve la señal). Al terminar la ruta no se avisa.</summary>
        public event Action SenalCambiada;

        /// <summary>
        /// Hay un estado nuevo (posición nueva, o ruta nueva al empezar o al recalcular). Con Llegado, solo una posición
        /// nueva: el estado se queda como al llegar.
        /// </summary>
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
        /// estado sale de la posición actual (sin ella, del principio de la ruta); si ya está en el destino, llega.
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
            SinSenal = false;
            Llegado = false;
            ultimaLectura = reloj();
            ultimaComprobacion = ultimaLectura;
            lecturasVistas = ubicacion.Lecturas;
            ultimaPosicion = ubicacion.Posicion;
            origenRuta = ultimaPosicion;
            Estado = regla.Seguir(ultimaPosicion ?? (ruta.Puntos.Count > 0 ? ruta.Puntos[0] : destino));
            ultimoMinuto = Estado.MinutosRestantes;
            var r = ubicacion.IniciarRuta(Notificacion(ultimoMinuto));
            GpsDeFondo = r.Ok && r.Valor;
            var s = sesion;
            RutaIniciada?.Invoke(Destino);
            if (!ReferenceEquals(s, sesion)) return; // terminada desde el aviso
            if (ultimaPosicion.HasValue && Llegar(ultimaPosicion.Value)) return;
            Actualizado?.Invoke();
        }

        /// <summary>«Finalizar ruta»: antes de llegar, Manual; ya en el destino, Llegada.</summary>
        public void Finalizar() => Terminar(Llegado ? MotivoFinRuta.Llegada : MotivoFinRuta.Manual);

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
            SinSenal = false; // sin aviso: la pantalla quita la pastilla al terminar
            Llegado = false;
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
            lecturasVistas = ubicacion.Lecturas;
            HayLectura(reloj());
            if (!Activa) return; // terminada desde el aviso de la señal
            Seguir(posicion.Value);
        }

        /// <summary>
        /// Mira la señal del GPS (la pantalla lo llama en cada fotograma): una lectura nueva (aunque repita la coordenada)
        /// la mantiene o la recupera; más de 10 s sin ninguna, SinSenal, el desvío a cero y SenalCambiada. Ya en el destino,
        /// la señal no importa: termina la ruta (Automatico) al minuto de llegar. Sin ruta, nada.
        /// </summary>
        public void Comprobar()
        {
            if (!Activa) return;
            var ahora = reloj();
            if (Llegado)
            {
                if (ahora < horaLlegada) horaLlegada = ahora; // el reloj ha saltado atrás: el minuto cuenta desde aquí
                if (ahora - horaLlegada >= TiempoCierre) Terminar(MotivoFinRuta.Automatico);
                return;
            }
            // Más de 1 s sin llamar: la app no estaba delante (LateUpdate parado). Ese tiempo no cuenta como sin señal
            if (ahora - ultimaComprobacion > PausaComprobacion) ultimaLectura = ahora;
            ultimaComprobacion = ahora;
            int lecturas = ubicacion.Lecturas;
            if (lecturas != lecturasVistas)
            {
                lecturasVistas = lecturas;
                HayLectura(ahora);
                return;
            }
            if (SinSenal || ahora - ultimaLectura <= TiempoSinSenal) return;
            SinSenal = true;
            desvioDesde = null; // al volver, el desvío cuenta desde cero
            SenalCambiada?.Invoke();
        }

        // Ha llegado una lectura del GPS: si se había perdido la señal, vuelve
        void HayLectura(DateTime ahora)
        {
            ultimaLectura = ahora;
            if (!SinSenal) return;
            SinSenal = false;
            desvioDesde = null;
            SenalCambiada?.Invoke();
        }

        void Seguir(Coordenada posicion)
        {
            ultimaPosicion = posicion;
            if (Llegado) { Actualizado?.Invoke(); return; } // solo la posición: la cámara sigue
            Estado = regla.Seguir(posicion, Estado);
            if (Llegar(posicion)) return;
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
            if (!Activa || Llegado) return; // un recálculo de la caché acaba al momento y puede haber llegado
            AvisarMinuto();
            Actualizado?.Invoke();
        }

        /// <summary>
        /// Si la posición está en el destino, llega: fuera el recálculo en curso, la notificación de llegada, HaLlegado y
        /// Actualizado. Devuelve si ha llegado.
        /// </summary>
        bool Llegar(Coordenada posicion)
        {
            if (!PuntoDestino.HasValue || !regla.HaLlegado(posicion, PuntoDestino.Value)) return false;
            Llegado = true;
            horaLlegada = reloj();
            desvioDesde = null;
            Recalculando = false;
            var antes = sesion;
            var propia = sesion = new CancellationTokenSource();
            antes?.Cancel(); // un recálculo pedido ya no cambia la ruta
            ubicacion.ActualizarNotificacion(textoLlegada(Destino));
            HaLlegado?.Invoke(Destino);
            if (ReferenceEquals(propia, sesion)) Actualizado?.Invoke(); // si no, terminada desde el aviso
            return true;
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
                if (SinSenal)
                {
                    Recalculando = false; // se perdió la señal mientras se pedía: se descarta en silencio y se espera a que vuelva
                    return;
                }
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
                    if (Llegar(ultimaPosicion ?? desde)) return; // la línea nueva acaba donde ya está
                    AvisarMinuto();
                    Actualizado?.Invoke();
                    return;
                }

                RecalculoFallido?.Invoke();
                // Si cambia durante la espera (se perdió o volvió la señal, o volvió a la ruta y se desvió otra vez), el desvío
                // cuenta de nuevo: este reintento se deja y el siguiente lo lanza Seguir
                var desvioAntes = desvioDesde;
                try
                {
                    await esperar(EsperaReintento, s.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                if (s.IsCancellationRequested || !ReferenceEquals(s, sesion)) return;
                if (Estado == null || !Estado.FueraDeRuta || SinSenal || desvioDesde == null || desvioDesde != desvioAntes)
                {
                    // Ha vuelto a la ruta (ya no hace falta), no hay señal o se perdió y volvió (el desvío cuenta desde cero)
                    Recalculando = false;
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
