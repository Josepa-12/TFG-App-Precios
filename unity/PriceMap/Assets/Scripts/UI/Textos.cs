using System;
using UnityEngine.Localization.Settings;

namespace PriceMap.UI
{
    /// <summary>Lee los textos de la tabla de Localization en el idioma activo (es-ES en la v1).</summary>
    public static class Textos
    {
        /// <summary>Solo para las pruebas: si no es null, da los textos en lugar de Localization (sin locale en EditMode).</summary>
        internal static Func<string, string> Forzados;

        public static string T(string clave)
        {
            if (Forzados != null) return Forzados(clave);
            // Si falta la entrada, se ve la clave (fácil de detectar) y no el aviso interno de Localization
            var entrada = LocalizationSettings.StringDatabase.GetTable(ClavesTexto.Tabla)?.GetEntry(clave);
            var texto = entrada?.GetLocalizedString();
            return string.IsNullOrEmpty(texto) ? clave : texto;
        }

        /// <summary>Texto con huecos {0}, {1}... rellenados con los argumentos (ya formateados por la UI).</summary>
        public static string T(string clave, params object[] argumentos) => Formatear(T(clave), argumentos);

        /// <summary>string.Format sin cultura: los números llegan ya como texto es-ES (FormatoPrecio).</summary>
        public static string Formatear(string plantilla, params object[] argumentos) =>
            argumentos == null || argumentos.Length == 0
                ? plantilla
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, plantilla, argumentos);
    }
}
