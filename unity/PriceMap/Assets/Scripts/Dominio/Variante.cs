namespace PriceMap.Dominio
{
    /// <summary>
    /// Variante de un concepto (id "concepto@variante", AD-11) con su condición y su propio precio:
    /// las reglas y la frescura se aplican a cada variante por separado (FR-60).
    /// </summary>
    public sealed class Variante
    {
        public string Id { get; }
        public CondicionVariante Condicion { get; }
        public FuentesPrecio Fuentes { get; }

        public Variante(string id, CondicionVariante condicion, FuentesPrecio fuentes)
        {
            Id = id;
            Condicion = condicion;
            Fuentes = fuentes;
        }
    }
}
