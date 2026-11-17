namespace PriceMap.Dominio
{
    /// <summary>Qué plantillas entran en cada opción del filtro "Precios" y cómo se guarda la elección.</summary>
    public static class ReglaFiltro
    {
        const string ClaveNinguno = "ninguno";
        const string ClaveTodos = "todos";
        const string ClaveServicios = "servicios-locales";
        const string ClaveHosteleria = "hosteleria";
        const string ClaveOcio = "ocio";

        /// <summary>Las opciones en el orden del menú.</summary>
        public static readonly FiltroPrecios[] Opciones =
        {
            FiltroPrecios.Ninguno, FiltroPrecios.Todos, FiltroPrecios.ServiciosLocales, FiltroPrecios.Hosteleria, FiltroPrecios.Ocio,
        };

        /// <summary>True si un lugar de esa plantilla se ve con ese filtro.</summary>
        public static bool Incluye(FiltroPrecios filtro, Plantilla plantilla)
        {
            switch (filtro)
            {
                case FiltroPrecios.Todos: return true;
                case FiltroPrecios.ServiciosLocales: return plantilla == Plantilla.ServicioLocal;
                case FiltroPrecios.Hosteleria: return plantilla == Plantilla.Hosteleria;
                case FiltroPrecios.Ocio: return plantilla == Plantilla.Ocio;
                default: return false; // Ninguno
            }
        }

        /// <summary>Texto con el que se guarda el filtro en las preferencias.</summary>
        public static string AClave(FiltroPrecios filtro)
        {
            switch (filtro)
            {
                case FiltroPrecios.Todos: return ClaveTodos;
                case FiltroPrecios.ServiciosLocales: return ClaveServicios;
                case FiltroPrecios.Hosteleria: return ClaveHosteleria;
                case FiltroPrecios.Ocio: return ClaveOcio;
                default: return ClaveNinguno;
            }
        }

        /// <summary>Filtro guardado; Ninguno si no hay clave o no se reconoce.</summary>
        public static FiltroPrecios DesdeClave(string clave)
        {
            switch (clave)
            {
                case ClaveTodos: return FiltroPrecios.Todos;
                case ClaveServicios: return FiltroPrecios.ServiciosLocales;
                case ClaveHosteleria: return FiltroPrecios.Hosteleria;
                case ClaveOcio: return FiltroPrecios.Ocio;
                default: return FiltroPrecios.Ninguno;
            }
        }
    }
}
