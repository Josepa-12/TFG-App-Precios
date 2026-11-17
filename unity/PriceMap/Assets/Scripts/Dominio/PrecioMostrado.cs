namespace PriceMap.Dominio
{
    /// <summary>
    /// Lo que la ficha muestra de un concepto o variante (AD-3: calculado al leer, nunca guardado):
    /// el precio (o "A consultar" o "sin precio"), su origen y su frescura.
    /// </summary>
    public sealed class PrecioMostrado
    {
        /// <summary>Importe, o null si es "A consultar" o no hay precio.</summary>
        public ValorPrecio? Valor { get; }
        /// <summary>La web nombra el servicio pero no da el importe.</summary>
        public bool EsConsultar { get; }
        public Origen Origen { get; }
        /// <summary>Solo si hay importe.</summary>
        public Frescura? Frescura { get; }

        // Solo lo construye CalculadoraPrecio: así no hay "A consultar" con importe ni importe sin frescura
        internal PrecioMostrado(ValorPrecio? valor, bool esConsultar, Origen origen, Frescura? frescura)
        {
            Valor = valor;
            EsConsultar = esConsultar;
            Origen = origen;
            Frescura = frescura;
        }

        public bool TienePrecio => Valor != null;
    }
}
