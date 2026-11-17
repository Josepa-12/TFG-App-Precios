namespace PriceMap.Dominio
{
    /// <summary>
    /// Lo que el mapa necesita saber del precio de un lugar (insignia €, anuncio de TalkBack):
    /// si tiene precio y el que se anuncia ("desde X"). Calculado al leer, nunca guardado (AD-3).
    /// </summary>
    public sealed class ResumenLugar
    {
        public static readonly ResumenLugar SinPrecio = new ResumenLugar(null);

        /// <summary>Precio que se anuncia, siempre con forma "desde"; null si no hay ninguno.</summary>
        public ValorPrecio? PrecioAnunciado { get; }

        // Solo lo construye ReglaResumen
        internal ResumenLugar(ValorPrecio? precioAnunciado)
        {
            PrecioAnunciado = precioAnunciado;
        }

        public bool TienePrecio => PrecioAnunciado != null;
    }
}
