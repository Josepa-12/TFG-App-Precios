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
    }
}
