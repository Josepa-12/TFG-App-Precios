using UnityEngine.Localization.Settings;

namespace PriceMap.UI
{
    /// <summary>Lee los textos de la tabla de Localization en el idioma activo (es-ES en la v1).</summary>
    public static class Textos
    {
        public static string T(string clave)
        {
            // Si falta la entrada, se ve la clave (fácil de detectar) y no el aviso interno de Localization
            var entrada = LocalizationSettings.StringDatabase.GetTable(ClavesTexto.Tabla)?.GetEntry(clave);
            var texto = entrada?.GetLocalizedString();
            return string.IsNullOrEmpty(texto) ? clave : texto;
        }
    }
}
