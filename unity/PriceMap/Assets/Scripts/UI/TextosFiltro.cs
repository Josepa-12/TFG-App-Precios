using PriceMap.Dominio;

namespace PriceMap.UI
{
    /// <summary>Claves de texto de cada opción del filtro "Precios" (en el menú y en la píldora).</summary>
    public static class TextosFiltro
    {
        /// <summary>Nombre de la opción en el menú (y valor de la píldora para TalkBack).</summary>
        public static string ClaveOpcion(FiltroPrecios filtro)
        {
            switch (filtro)
            {
                case FiltroPrecios.Todos: return ClavesTexto.PreciosOpcionTodos;
                case FiltroPrecios.ServiciosLocales: return ClavesTexto.PreciosOpcionServicios;
                case FiltroPrecios.Hosteleria: return ClavesTexto.PreciosOpcionHosteleria;
                case FiltroPrecios.Ocio: return ClavesTexto.PreciosOpcionOcio;
                default: return ClavesTexto.PreciosOpcionNinguno;
            }
        }

        /// <summary>Texto de la píldora: "Ver precios", "Todos los precios" o el nombre de la plantilla.</summary>
        public static string ClavePildora(FiltroPrecios filtro)
        {
            switch (filtro)
            {
                case FiltroPrecios.Ninguno: return ClavesTexto.PreciosVer;
                case FiltroPrecios.Todos: return ClavesTexto.PreciosTodos;
                default: return ClaveOpcion(filtro);
            }
        }
    }
}
