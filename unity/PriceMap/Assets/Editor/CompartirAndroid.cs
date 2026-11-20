using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Historia 2.5 (parte 3, FR17): al generar el proyecto Gradle de Android, la actividad de Unity (la que tiene el
    /// filtro MAIN/LAUNCHER) recibe lo compartido: texto (<c>SEND</c> + <c>text/plain</c>, categoría <c>DEFAULT</c>) y
    /// enlaces <c>geo:</c> (<c>VIEW</c>, categorías <c>DEFAULT</c> y <c>BROWSABLE</c>). Sin duplicarlos si ya están. Nunca
    /// filtros <c>VIEW</c> de <c>https</c> de Google Maps: desde Android 12 no se ofrecerían a PriceMap.
    /// </summary>
    public class CompartirAndroid : IPostGenerateGradleAndroidProject
    {
        const string EspacioAndroid = "http://schemas.android.com/apk/res/android";
        public const string AccionEnviar = "android.intent.action.SEND";
        public const string AccionVer = "android.intent.action.VIEW";
        const string CategoriaDefecto = "android.intent.category.DEFAULT";
        const string CategoriaNavegable = "android.intent.category.BROWSABLE";

        public int callbackOrder => 1; // tras PermisosUbicacionAndroid

        public void OnPostGenerateGradleAndroidProject(string ruta)
        {
            var manifiesto = Path.Combine(ruta, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifiesto)) { Debug.LogWarning("PriceMap: no se encuentra " + manifiesto + " para compartir con la app."); return; }
            var xml = new XmlDocument();
            xml.Load(manifiesto);
            var nombres = new XmlNamespaceManager(xml.NameTable);
            nombres.AddNamespace("android", EspacioAndroid);

            var actividad = xml.DocumentElement.SelectNodes("application/activity", nombres).Cast<XmlElement>()
                .FirstOrDefault(a => a.SelectSingleNode(
                    "intent-filter[action/@android:name='android.intent.action.MAIN' and category/@android:name='android.intent.category.LAUNCHER']",
                    nombres) != null);
            if (actividad == null) { Debug.LogWarning("PriceMap: no hay actividad lanzadora en " + manifiesto + "; no se registra compartir."); return; }

            bool cambiado = false;
            if (actividad.SelectSingleNode($"intent-filter[action/@android:name='{AccionEnviar}' and data/@android:mimeType='text/plain']", nombres) == null)
            {
                var filtro = Filtro(xml, actividad, AccionEnviar, CategoriaDefecto);
                Dato(xml, filtro, "mimeType", "text/plain");
                cambiado = true;
            }
            if (actividad.SelectSingleNode($"intent-filter[action/@android:name='{AccionVer}' and data/@android:scheme='geo']", nombres) == null)
            {
                var filtro = Filtro(xml, actividad, AccionVer, CategoriaDefecto, CategoriaNavegable);
                Dato(xml, filtro, "scheme", "geo");
                cambiado = true;
            }
            if (cambiado) xml.Save(manifiesto);
        }

        static XmlElement Filtro(XmlDocument xml, XmlElement actividad, string accion, params string[] categorias)
        {
            var filtro = xml.CreateElement("intent-filter");
            var a = xml.CreateElement("action");
            a.SetAttribute("name", EspacioAndroid, accion);
            filtro.AppendChild(a);
            foreach (var categoria in categorias)
            {
                var c = xml.CreateElement("category");
                c.SetAttribute("name", EspacioAndroid, categoria);
                filtro.AppendChild(c);
            }
            actividad.AppendChild(filtro);
            return filtro;
        }

        static void Dato(XmlDocument xml, XmlElement filtro, string atributo, string valor)
        {
            var d = xml.CreateElement("data");
            d.SetAttribute(atributo, EspacioAndroid, valor);
            filtro.AppendChild(d);
        }
    }
}
