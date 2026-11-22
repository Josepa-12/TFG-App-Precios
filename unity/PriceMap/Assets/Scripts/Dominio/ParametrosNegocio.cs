namespace PriceMap.Dominio
{
    /// <summary>
    /// Único sitio de los parámetros de negocio (AD-14). Nadie más declara estos umbrales.
    /// Las siguientes historias añaden aquí los suyos (aportes, reputación, recompensas...).
    /// </summary>
    public static class ParametrosNegocio
    {
        // Frescura (FR-33). Los límites exactos cuentan como "podría haber cambiado".
        /// <summary>Hostelería: al día por debajo de estos días.</summary>
        public const int FrescuraHosteleriaDiasAlDia = 30;
        /// <summary>Hostelería: antiguo por encima de estos días.</summary>
        public const int FrescuraHosteleriaDiasAntiguo = 60;
        /// <summary>Servicio local y ocio: al día por debajo de estos meses.</summary>
        public const int FrescuraOtrosMesesAlDia = 3;
        /// <summary>Servicio local y ocio: antiguo por encima de estos meses.</summary>
        public const int FrescuraOtrosMesesAntiguo = 6;

        // Rutas y tiempos de la ficha (historia 2.6)
        /// <summary>Segundos que se espera a un servicio de rutas antes de pasar al de reserva (o dejar el modo sin tiempo).</summary>
        public const int RutaTiempoMaximoSegundos = 8;
        /// <summary>Metros que puede moverse el usuario sin que la caché de rutas deje de valer.</summary>
        public const double RutaCacheMetros = 20.0;
        /// <summary>Minutos que vale una ruta guardada en la caché (solo en memoria).</summary>
        public const int RutaCacheMinutos = 2;

        // Línea y encuadre de la ruta (historia 2.6, parte 2a; decisiones del 20/11/2026)
        /// <summary>Ancho de la línea de la ruta en el suelo, con su borde (una calle de Sanlúcar), en metros.</summary>
        public const float RutaAnchoMetros = 9f;
        /// <summary>Ancho mínimo de la línea en pantalla (dp): al alejar, no se queda más fina.</summary>
        public const float RutaAnchoMinimoDp = 6f;
        /// <summary>Cambio relativo del ancho a partir del cual se rehace la línea al hacer zoom (10 %).</summary>
        public const float RutaAnchoCambioRehacer = 0.10f;
        /// <summary>Margen (dp) entre la ruta encuadrada y las tarjetas de arriba y de abajo.</summary>
        public const float RutaEncuadreMargenDp = 16f;
        /// <summary>Margen (dp) entre la ruta encuadrada y los lados de la pantalla.</summary>
        public const float RutaEncuadreLadoDp = 32f;

        // Ruta aproximada sin conexión (historia 2.6, parte 2b; decisiones del 20 y 21/11/2026)
        /// <summary>Velocidad (km/h) con la que se calcula el tiempo de la ruta aproximada en coche.</summary>
        public const double RutaAproximadaCocheKmh = 30.0;
        /// <summary>Velocidad (km/h) con la que se calcula el tiempo de la ruta aproximada a pie.</summary>
        public const double RutaAproximadaPieKmh = 5.0;
        /// <summary>Metros a los que un extremo suelto de una calle se une a otra (cruces sin vértice común).</summary>
        public const double RutaAproximadaUnionMetros = 5.0;
        /// <summary>Metros máximos del origen o del destino a la calle guardada más cercana.</summary>
        public const double RutaAproximadaCalleMetros = 300.0;
        /// <summary>Teselas por lado, como mucho, que se leen para la ruta aproximada (5 × 5).</summary>
        public const int RutaAproximadaTeselasLado = 5;

        // Seguir la ruta (historia 2.7, parte 1)
        /// <summary>Metros a la ruta por encima de los cuales el usuario se ha desviado.</summary>
        public const double NavegacionDesvioMetros = 40.0;
        /// <summary>Segundos seguidos desviado a partir de los cuales se recalcula la ruta.</summary>
        public const double NavegacionDesvioSegundos = 5.0;
        /// <summary>Segundos que se espera para volver a intentar un recálculo que ha fallado.</summary>
        public const double NavegacionReintentoSegundos = 10.0;
        /// <summary>
        /// Metros hacia atrás del último avance en los que aún se busca el tramo más cercano (el avance nunca baja: el
        /// ruido del GPS por detrás se queda en el último punto).
        /// </summary>
        public const double NavegacionMargenAtrasMetros = 30.0;
        /// <summary>
        /// Metros por delante del último avance a partir de los cuales un tramo cuenta como salto (p. ej. la vuelta por la
        /// misma calle): solo gana si está claramente más cerca (NavegacionEmpateMetros).
        /// </summary>
        public const double NavegacionSaltoMetros = 100.0;
        /// <summary>Metros que un tramo lejano por delante tiene que estar más cerca que el cercano para saltar a él.</summary>
        public const double NavegacionEmpateMetros = 15.0;

        // La cámara sigue al usuario (historia 2.7, parte 2b; decisiones del 21 y 22/11/2026)
        /// <summary>Metros mínimos entre dos posiciones para sacar de ellas el rumbo de la marcha.</summary>
        public const double RumboDistanciaMinimaMetros = 3.0;
        /// <summary>Velocidad (m/s) hasta la que se considera parado: se queda el último rumbo de la marcha.</summary>
        public const double RumboVelocidadMinima = 1.0;
        /// <summary>Segundos que se ve la ruta entera tras Ir antes de que la cámara pase a seguir.</summary>
        public const double CamaraEsperaSeguirSegundos = 2.0;
        /// <summary>Inclinación de la cámara al seguir (grados desde la horizontal; 85 es la vista cenital).</summary>
        public const float CamaraSeguirInclinacion = 45f;
        /// <summary>Distancia (m) de la cámara a la posición al seguir en coche.</summary>
        public const float CamaraSeguirDistanciaCoche = 350f;
        /// <summary>Distancia (m) de la cámara a la posición al seguir a pie.</summary>
        public const float CamaraSeguirDistanciaPie = 180f;
        /// <summary>
        /// Altura de la posición en la zona libre entre las tarjetas al seguir (0 = borde de arriba, 1 = el de abajo):
        /// en el tercio de abajo, con la ruta delante.
        /// </summary>
        public const float CamaraSeguirAlturaZona = 0.75f;
        /// <summary>Segundos aproximados de la transición suave de la cámara (al 95 %).</summary>
        public const float CamaraTransicionSegundos = 0.6f;
        /// <summary>Grados que tiene que moverse la brújula, a pie y sin posición nueva, para volver a girar la cámara.</summary>
        public const float CamaraBrujulaMinimaGrados = 2f;

        // Flecha y sin señal (historia 2.7, parte 2c; decisiones del 22/11/2026)
        /// <summary>
        /// Segundos sin ninguna lectura nueva del GPS (también cuenta una con la misma coordenada) a partir de los cuales,
        /// navegando, se ha perdido la señal: posición atenuada, "Buscando señal GPS" y sin recálculo.
        /// </summary>
        public const double NavegacionSinSenalSegundos = 10.0;

        // Indicaciones por voz (historia 2.8; decisiones del 22/11/2026)
        /// <summary>Metros a los que se dice la maniobra en el momento («Gire a la derecha.») en coche.</summary>
        public const double VozMomentoCocheMetros = 40.0;
        /// <summary>Lo mismo a pie.</summary>
        public const double VozMomentoPieMetros = 15.0;
        /// <summary>Metros del aviso intermedio (solo con «todas las indicaciones») en coche.</summary>
        public const double VozIntermedioCocheMetros = 300.0;
        /// <summary>Lo mismo a pie.</summary>
        public const double VozIntermedioPieMetros = 50.0;
        /// <summary>
        /// El aviso intermedio solo se dice si el de la maniobra siguiente se dijo a más de estas veces su distancia (si no,
        /// se acaba de oír y sería repetirlo).
        /// </summary>
        public const double VozIntermedioFactor = 1.5;
        /// <summary>Las distancias dichas se redondean a estos metros en coche.</summary>
        public const double VozRedondeoCocheMetros = 50.0;
        /// <summary>Lo mismo a pie.</summary>
        public const double VozRedondeoPieMetros = 10.0;
    }
}
