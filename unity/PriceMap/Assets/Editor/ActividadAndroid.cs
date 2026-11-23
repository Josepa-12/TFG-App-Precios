using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Historia 2.8 (parte 2): al generar el proyecto Gradle de Android, la actividad lanzadora (la del filtro
    /// MAIN/LAUNCHER) pasa a ser la de la app, <c>com.pricemap.app.ActividadPrincipal</c>
    /// (Assets/Plugins/Android/ActividadPrincipal.java): navegando no pausa Unity en segundo plano. Se queda con sus
    /// filtros (los de compartir los añade después CompartirAndroid) y se le quita lo que solo usa GameActivity
    /// (<c>android.app.lib_name</c>). La app usa la entrada Activity (ConfigurarAndroid). Con aceleración gráfica (Unity la
    /// quita): la pantalla de inicio se anima encima de Unity. Sin copia de seguridad de Android (historia 3.1): los datos se
    /// quedan en el móvil y, al reinstalar, la bienvenida vuelve a salir.
    /// </summary>
    public class ActividadAndroid : IPostGenerateGradleAndroidProject
    {
        const string EspacioAndroid = "http://schemas.android.com/apk/res/android";
        public const string Actividad = "com.pricemap.app.ActividadPrincipal";
        /// <summary>Meta-dato que solo usa GameActivity (la biblioteca nativa que carga).</summary>
        public const string SoloGameActivity = "android.app.lib_name";

        public int callbackOrder => 0; // antes que CompartirAndroid (1): la lanzadora ya es la nuestra

        public void OnPostGenerateGradleAndroidProject(string ruta)
        {
            var manifiesto = Path.Combine(ruta, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifiesto)) { Debug.LogWarning("PriceMap: no se encuentra " + manifiesto + " para la actividad."); return; }
            var xml = new XmlDocument();
            xml.Load(manifiesto);
            var nombres = new XmlNamespaceManager(xml.NameTable);
            nombres.AddNamespace("android", EspacioAndroid);

            (xml.DocumentElement.SelectSingleNode("application") as XmlElement)?.SetAttribute("allowBackup", EspacioAndroid, "false");

            var actividad = xml.DocumentElement.SelectNodes("application/activity", nombres).Cast<XmlElement>()
                .FirstOrDefault(a => a.SelectSingleNode(
                    "intent-filter[action/@android:name='android.intent.action.MAIN' and category/@android:name='android.intent.category.LAUNCHER']",
                    nombres) != null);
            if (actividad == null)
            {
                Debug.LogWarning("PriceMap: no hay actividad lanzadora en " + manifiesto + "; se queda la de Unity.");
                xml.Save(manifiesto);
                return;
            }

            actividad.SetAttribute("name", EspacioAndroid, Actividad);
            actividad.SetAttribute("hardwareAccelerated", EspacioAndroid, "true");
            foreach (var meta in actividad.SelectNodes($"meta-data[@android:name='{SoloGameActivity}']", nombres).Cast<XmlNode>().ToList())
                actividad.RemoveChild(meta);
            xml.Save(manifiesto);
        }
    }
}
