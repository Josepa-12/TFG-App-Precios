using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Recursos de la pantalla de inicio (spec pantalla-de-inicio) en el proyecto Gradle de Android: las imágenes del logo
    /// (docs/diseno/inicio) en <c>drawable-nodpi</c>, sus colores y el texto de TalkBack, y el arranque del sistema
    /// (Android 12+) y el fondo de la ventana del mismo color, claro u oscuro, en vez del gris y el negro de Unity.
    /// Los usan PuenteInicio y PantallaInicio (Assets/Plugins/Android).
    /// </summary>
    public class PantallaInicioRecursos : IPostGenerateGradleAndroidProject
    {
        public const string ColorClaro = "#FDFDFD";
        public const string ColorOscuro = "#0F1424";
        const string ColorFondo = "@color/inicio_fondo"; // el del tema del sistema

        /// <summary>Capas recortadas de la imagen (fuera del proyecto de Unity) y el recurso de cada una.</summary>
        public static readonly IReadOnlyDictionary<string, string> Imagenes = new Dictionary<string, string>
        {
            { "pin.png", "inicio_pin.png" },
            { "nombre-claro.png", "inicio_nombre_claro.png" },
            { "nombre-oscuro.png", "inicio_nombre_oscuro.png" },
        };

        public static string CarpetaImagenes => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "docs", "diseno", "inicio"));

        public int callbackOrder => 2;

        public void OnPostGenerateGradleAndroidProject(string ruta)
        {
            string res = Path.Combine(ruta, "src", "main", "res");
            string dibujos = Directory.CreateDirectory(Path.Combine(res, "drawable-nodpi")).FullName;
            foreach (var imagen in Imagenes)
            {
                string origen = Path.Combine(CarpetaImagenes, imagen.Key);
                if (!File.Exists(origen)) throw new BuildFailedException("PriceMap: falta la imagen de la pantalla de inicio " + origen);
                File.Copy(origen, Path.Combine(dibujos, imagen.Value), overwrite: true);
            }

            Escribir(res, "values",
                "  <color name=\"inicio_claro\">" + ColorClaro + "</color>\n" +
                "  <color name=\"inicio_oscuro\">" + ColorOscuro + "</color>\n" +
                "  <color name=\"inicio_fondo\">@color/inicio_claro</color>\n" +
                "  <string name=\"inicio_cargando\">PriceMap, cargando</string>\n" +
                Estilo("PriceMapInicioClaro", "@color/inicio_claro", android12: false) +
                Estilo("PriceMapInicioOscuro", "@color/inicio_oscuro", android12: false));
            Escribir(res, "values-night", "  <color name=\"inicio_fondo\">@color/inicio_oscuro</color>\n");
            // Android 12+: el tema del arranque cuando el guardado en la app no es el del sistema (PuenteInicio.fijarArranque)
            Escribir(res, "values-v31",
                Estilo("PriceMapInicioClaro", "@color/inicio_claro", android12: true) +
                Estilo("PriceMapInicioOscuro", "@color/inicio_oscuro", android12: true));

            Poner(Path.Combine(res, "values", "colors.xml"), "/resources/color[@name='staticSplashScreenBackgroundColor']", obligatorio: true);
            Poner(Path.Combine(res, "values", "styles.xml"), "/resources/style[@name='UnityThemeSelector']/item[@name='android:windowBackground']", obligatorio: true);
            Poner(Path.Combine(res, "values-v31", "styles.xml"), "/resources/style[@name='BaseUnityTheme']/item[@name='android:windowBackground']", obligatorio: false);
            Poner(Path.Combine(res, "values-v31", "styles.xml"), "/resources/style[@name='BaseUnityTheme']/item[@name='android:windowSplashScreenBackground']", obligatorio: true);
        }

        static string Estilo(string nombre, string color, bool android12) =>
            "  <style name=\"" + nombre + "\" parent=\"UnityThemeSelector\">\n" +
            "    <item name=\"android:windowBackground\">" + color + "</item>\n" +
            (android12 ? "    <item name=\"android:windowSplashScreenBackground\">" + color + "</item>\n" : "") +
            "  </style>\n";

        static void Escribir(string res, string carpeta, string contenido) =>
            File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(res, carpeta)).FullName, "inicio.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n" + contenido + "</resources>\n", new UTF8Encoding(false));

        // El color de la pantalla de inicio en ese recurso de Unity
        static void Poner(string fichero, string nodo, bool obligatorio)
        {
            XmlDocument xml = null;
            if (File.Exists(fichero)) { xml = new XmlDocument(); xml.Load(fichero); }
            var nodos = xml?.SelectNodes(nodo).Cast<XmlNode>().ToList() ?? new List<XmlNode>();
            if (nodos.Count == 0)
            {
                if (obligatorio) throw new BuildFailedException($"PriceMap: no se encuentra {nodo} en {fichero}; el arranque saldría con el color de Unity.");
                return;
            }
            foreach (var n in nodos) n.InnerText = ColorFondo;
            xml.Save(fichero);
        }
    }
}
