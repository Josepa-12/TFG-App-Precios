namespace PriceMap.Dominio
{
    /// <summary>
    /// Una maniobra de la ruta (historia 2.6): su tipo, su texto en castellano (el de Valhalla; vacío con OSRM hasta la
    /// historia 2.7), dónde empieza y los metros que hay hasta la siguiente.
    /// </summary>
    public sealed class Maniobra
    {
        public TipoManiobra Tipo { get; }
        /// <summary>Texto de la maniobra ("Gira a la derecha en Calle Ancha."); vacío si el servicio no lo da.</summary>
        public string Texto { get; }
        public Coordenada Posicion { get; }
        public double Metros { get; }

        public Maniobra(TipoManiobra tipo, string texto, Coordenada posicion, double metros)
        {
            Tipo = tipo;
            Texto = texto ?? "";
            Posicion = posicion;
            Metros = metros < 0 || double.IsNaN(metros) ? 0 : metros;
        }
    }
}
