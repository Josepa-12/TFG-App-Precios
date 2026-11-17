using System.Collections.Generic;
using System.IO;
using PriceMap.UI;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Crea (o actualiza) lo que necesita la interfaz y no es código:
    ///  - Localization: ajustes, locale es-ES y la tabla "Textos" con todos los textos de la app (NFR-5);
    ///  - PanelSettings de UI Toolkit: 1 px = 1 dp (tamaño físico constante a 160 dpi) y el tema PriceMap.tss.
    /// Menú: PriceMap ▸ Preparar interfaz. Lo llama también la preparación de la escena.
    /// </summary>
    public static class PrepararInterfaz
    {
        public const string CarpetaLocalizacion = "Assets/Localizacion";
        public const string RutaPanelSettings = "Assets/UI/PanelSettings.asset";
        public const string RutaTema = "Assets/UI/Estilos/PriceMap.tss";
        public const string RutaPantallaMapa = "Assets/UI/PantallaMapa.uxml";
        static readonly LocaleIdentifier Espanol = new LocaleIdentifier("es-ES");

        /// <summary>Textos en castellano (tuteo, frases cortas: EXPERIENCE.md · Voice and Tone).</summary>
        public static readonly Dictionary<string, string> TextosEs = new Dictionary<string, string>
        {
            [ClavesTexto.TemaCambiar] = "Cambiar tema",
            [ClavesTexto.TemaClaro] = "Tema claro",
            [ClavesTexto.TemaOscuro] = "Tema oscuro",
            [ClavesTexto.MapaAcercar] = "Acercar",
            [ClavesTexto.MapaAlejar] = "Alejar",
            [ClavesTexto.MapaAtribucion] = "OpenFreeMap © OpenMapTiles · Datos © colaboradores de OpenStreetMap",
            [ClavesTexto.MarcadorConPrecio] = "{0}, {1}, desde {2}",
            [ClavesTexto.MarcadorSinPrecio] = "{0}, {1}, aún no tiene precio",
            [ClavesTexto.PreciosControl] = "Precios en el mapa",
            [ClavesTexto.PreciosVer] = "Ver precios",
            [ClavesTexto.PreciosTodos] = "Todos los precios",
            [ClavesTexto.PreciosTitulo] = "Mostrar precios en el mapa",
            [ClavesTexto.PreciosOpcionNinguno] = "Ninguno",
            [ClavesTexto.PreciosOpcionTodos] = "Todos",
            [ClavesTexto.PreciosOpcionServicios] = "Servicios locales",
            [ClavesTexto.PreciosOpcionHosteleria] = "Hostelería",
            [ClavesTexto.PreciosOpcionOcio] = "Ocio",
        };

        [MenuItem("PriceMap/Preparar interfaz")]
        public static void Asegurar()
        {
            AsegurarTextos();
            AsegurarPanelSettings();
            AssetDatabase.SaveAssets();
        }

        static void AsegurarTextos()
        {
            Directory.CreateDirectory(CarpetaLocalizacion);

            var ajustes = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (ajustes == null)
            {
                ajustes = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(ajustes, CarpetaLocalizacion + "/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = ajustes;
            }

            var locale = LocalizationEditorSettings.GetLocale(Espanol);
            if (locale == null)
            {
                locale = Locale.CreateLocale(Espanol);
                AssetDatabase.CreateAsset(locale, CarpetaLocalizacion + "/Espanol (es-ES).asset");
                LocalizationEditorSettings.AddLocale(locale);
            }

            // Arranca siempre en es-ES (única locale de la v1)
            var selectores = ajustes.GetStartupLocaleSelectors();
            var especifico = selectores.Find(s => s is SpecificLocaleSelector) as SpecificLocaleSelector;
            if (especifico == null) selectores.Insert(0, new SpecificLocaleSelector { LocaleId = Espanol });
            else especifico.LocaleId = Espanol;
            EditorUtility.SetDirty(ajustes);

            var coleccion = LocalizationEditorSettings.GetStringTableCollection(ClavesTexto.Tabla)
                            ?? LocalizationEditorSettings.CreateStringTableCollection(ClavesTexto.Tabla, CarpetaLocalizacion);
            var tabla = coleccion.GetTable(Espanol) as StringTable ?? coleccion.AddNewTable(Espanol) as StringTable;
            foreach (var par in TextosEs)
            {
                var entrada = tabla.GetEntry(par.Key) ?? tabla.AddEntry(par.Key, par.Value);
                entrada.Value = par.Value;
            }
            EditorUtility.SetDirty(tabla);
            EditorUtility.SetDirty(tabla.SharedData);
        }

        static void AsegurarPanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(RutaPanelSettings);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, RutaPanelSettings);
            }
            panel.scaleMode = PanelScaleMode.ConstantPhysicalSize; // 1 px del panel = 1 dp
            panel.referenceDpi = 160f;
            panel.fallbackDpi = 160f;
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(RutaTema);
            panel.clearColor = false; // el mapa se ve detrás
            EditorUtility.SetDirty(panel);
        }
    }
}
