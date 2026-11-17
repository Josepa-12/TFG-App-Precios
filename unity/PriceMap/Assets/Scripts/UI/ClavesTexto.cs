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

        /// <summary>TalkBack de un marcador con precio: {0} nombre, {1} tipo en minúsculas, {2} precio.</summary>
        public const string MarcadorConPrecio = "marcador.anuncio.conprecio";
        /// <summary>TalkBack de un marcador sin precio: {0} nombre, {1} tipo en minúsculas.</summary>
        public const string MarcadorSinPrecio = "marcador.anuncio.sinprecio";

        /// <summary>Píldora del filtro "Precios" para TalkBack (el valor es la opción elegida).</summary>
        public const string PreciosControl = "precios.control";
        /// <summary>Texto de la píldora con Ninguno.</summary>
        public const string PreciosVer = "precios.ver";
        /// <summary>Texto de la píldora con Todos.</summary>
        public const string PreciosTodos = "precios.todos";
        public const string PreciosTitulo = "precios.titulo";
        public const string PreciosOpcionNinguno = "precios.opcion.ninguno";
        public const string PreciosOpcionTodos = "precios.opcion.todos";
        public const string PreciosOpcionServicios = "precios.opcion.servicios";
        public const string PreciosOpcionHosteleria = "precios.opcion.hosteleria";
        public const string PreciosOpcionOcio = "precios.opcion.ocio";
        /// <summary>TalkBack al elegir una opción: {0} nombre de la opción.</summary>
        public const string PreciosAnuncio = "precios.anuncio";

        public static readonly string[] Todas =
        {
            TemaCambiar, TemaClaro, TemaOscuro, MapaAcercar, MapaAlejar, MapaAtribucion,
            MarcadorConPrecio, MarcadorSinPrecio,
            PreciosControl, PreciosVer, PreciosTodos, PreciosTitulo,
            PreciosOpcionNinguno, PreciosOpcionTodos, PreciosOpcionServicios, PreciosOpcionHosteleria, PreciosOpcionOcio,
            PreciosAnuncio,
        };
    }
}
