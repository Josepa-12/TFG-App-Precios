namespace PriceMap.Dominio
{
    /// <summary>
    /// Una maniobra de la ruta (historia 2.6): su tipo, su texto en castellano (el de Valhalla; vacío con OSRM hasta la
    /// historia 2.7), dónde empieza y los metros que hay hasta la siguiente. Con Valhalla trae también el texto corto
    /// que se dice justo antes de hacerla (historia 2.8: «Gire a la derecha.»).
    /// </summary>
    public sealed class Maniobra
    {
        public TipoManiobra Tipo { get; }
        /// <summary>Texto de la maniobra ("Gira a la derecha en Calle Ancha."); vacío si el servicio no lo da.</summary>
        public string Texto { get; }
        /// <summary>Texto corto para decirlo en el momento ("Gire a la derecha."); sin él, el mismo Texto.</summary>
        public string TextoCorto { get; }
        public Coordenada Posicion { get; }
        public double Metros { get; }

        public Maniobra(TipoManiobra tipo, string texto, Coordenada posicion, double metros, string textoCorto = null)
        {
            Tipo = tipo;
            Texto = texto ?? "";
            TextoCorto = string.IsNullOrWhiteSpace(textoCorto) ? Texto : textoCorto;
            Posicion = posicion;
            Metros = metros < 0 || double.IsNaN(metros) ? 0 : metros;
        }
    }
}
