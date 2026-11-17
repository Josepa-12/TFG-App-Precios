namespace PriceMap.UI
{
    /// <summary>
    /// Claves de la tabla de Localization "Textos" (NFR-5). El código solo usa claves; el texto en
    /// castellano vive en la tabla es-ES. Una prueba comprueba que todas existen en la tabla.
    /// </summary>
    public static class ClavesTexto
    {
        public const string Tabla = "Textos";

        public const string TemaCambiar = "tema.cambiar";
        public const string TemaClaro = "tema.claro";
        public const string TemaOscuro = "tema.oscuro";
        public const string MapaAcercar = "mapa.acercar";
        public const string MapaAlejar = "mapa.alejar";
        public const string MapaAtribucion = "mapa.atribucion";

        public static readonly string[] Todas =
        {
            TemaCambiar, TemaClaro, TemaOscuro, MapaAcercar, MapaAlejar, MapaAtribucion,
        };
    }
}
