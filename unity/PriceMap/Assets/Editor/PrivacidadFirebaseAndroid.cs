using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;
using UnityEditor.Build;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Historia 3.2: el SDK de Firebase trae Analytics aunque la app solo use cuentas y Firestore. En el manifiesto de la
    /// aplicación (el que manda al juntarlos) queda desactivado del todo y sin sus permisos (identificador de publicidad y
    /// referencia de instalación): la app no recoge nada de eso.
    /// </summary>
    public class PrivacidadFirebaseAndroid : IPostGenerateGradleAndroidProject
    {
        const string EspacioAndroid = "http://schemas.android.com/apk/res/android";
        const string EspacioTools = "http://schemas.android.com/tools";
        public const string AnalyticsDesactivado = "firebase_analytics_collection_deactivated";
        /// <summary>Los que añade Analytics (firebase-analytics) y la app no quiere.</summary>
        public static readonly string[] PermisosQuitados =
        {
            "com.google.android.gms.permission.AD_ID",
            "android.permission.ACCESS_ADSERVICES_ATTRIBUTION",
            "android.permission.ACCESS_ADSERVICES_AD_ID",
            "com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE",
        };

        public int callbackOrder => 3;

        /// <param name="ruta">La del módulo unityLibrary; el de la aplicación (launcher) está al lado.</param>
        public void OnPostGenerateGradleAndroidProject(string ruta)
        {
            var manifiesto = Path.GetFullPath(Path.Combine(ruta, "..", "launcher", "src", "main", "AndroidManifest.xml"));
            if (!File.Exists(manifiesto))
                throw new BuildFailedException("PriceMap: no se encuentra " + manifiesto + "; la app saldría con los permisos de publicidad de Firebase.");
            var xml = new XmlDocument();
            xml.Load(manifiesto);
            var nombres = new XmlNamespaceManager(xml.NameTable);
            nombres.AddNamespace("android", EspacioAndroid);
            var raiz = xml.DocumentElement;

            foreach (var permiso in PermisosQuitados)
            {
                var e = raiz.SelectSingleNode($"uses-permission[@android:name='{permiso}']", nombres) as XmlElement;
                if (e == null) raiz.AppendChild(e = xml.CreateElement("uses-permission"));
                e.SetAttribute("name", EspacioAndroid, permiso);
                Quitar(e);
            }

            if (!(raiz.SelectSingleNode("application") is XmlElement aplicacion))
                throw new BuildFailedException("PriceMap: no hay <application> en " + manifiesto + "; Analytics de Firebase quedaría activo.");
            var meta = aplicacion.SelectNodes($"meta-data[@android:name='{AnalyticsDesactivado}']", nombres).Cast<XmlElement>().FirstOrDefault();
            if (meta == null) aplicacion.AppendChild(meta = xml.CreateElement("meta-data"));
            meta.SetAttribute("name", EspacioAndroid, AnalyticsDesactivado);
            meta.SetAttribute("value", EspacioAndroid, "true");
            xml.Save(manifiesto);
        }

        // Con su prefijo: el que lee el que junta los manifiestos
        static void Quitar(XmlElement e)
        {
            var nodo = e.OwnerDocument.CreateAttribute("tools", "node", EspacioTools);
            nodo.Value = "remove";
            e.SetAttributeNode(nodo);
        }
    }
}
