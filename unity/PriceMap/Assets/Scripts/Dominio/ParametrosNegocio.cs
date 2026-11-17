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
    }
}
