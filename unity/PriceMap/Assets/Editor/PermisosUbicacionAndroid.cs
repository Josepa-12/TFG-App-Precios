using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Al generar el proyecto Gradle de Android, asegura en el manifiesto los permisos de ubicación precisa y
    /// aproximada (desde Android 12 se piden juntos; Unity solo añade la precisa al usar Input.location) y
    /// que Unity no pida permisos al arrancar: el permiso se pide tras la explicación (historia 1.7). Para la ruta
    /// (historia 2.1, AD-19) añade el servicio en primer plano de tipo "location" del plugin (ServicioUbicacionRuta,
    /// no exportado) con sus permisos FOREGROUND_SERVICE, FOREGROUND_SERVICE_LOCATION y POST_NOTIFICATIONS. Nunca
    /// ACCESS_BACKGROUND_LOCATION: si apareciera, se quita.
    /// </summary>
    public class PermisosUbicacionAndroid : IPostGenerateGradleAndroidProject
    {
        const string EspacioAndroid = "http://schemas.android.com/apk/res/android";
        static readonly string[] Permisos =
        {
            "android.permission.ACCESS_FINE_LOCATION", "android.permission.ACCESS_COARSE_LOCATION",
            "android.permission.FOREGROUND_SERVICE", "android.permission.FOREGROUND_SERVICE_LOCATION",
            "android.permission.POST_NOTIFICATIONS",
        };
        /// <summary>Permiso que la app no pide nunca (AD-19): el GPS en segundo plano va con el servicio de ruta.</summary>
        public const string PermisoProhibido = "android.permission.ACCESS_BACKGROUND_LOCATION";
        /// <summary>Clase del servicio de ruta del plugin (Assets/Plugins/Android/ServicioUbicacionRuta.java).</summary>
        public const string ServicioRuta = "com.pricemap.ruta.ServicioUbicacionRuta";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string ruta)
        {
            var manifiesto = Path.Combine(ruta, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifiesto)) { Debug.LogWarning("PriceMap: no se encuentra " + manifiesto + " para los permisos de ubicación."); return; }
            var xml = new XmlDocument();
            xml.Load(manifiesto);
            var nombres = new XmlNamespaceManager(xml.NameTable);
            nombres.AddNamespace("android", EspacioAndroid);
            var raiz = xml.DocumentElement;

            foreach (var permiso in Permisos)
            {
                if (raiz.SelectSingleNode($"uses-permission[@android:name='{permiso}']", nombres) != null) continue;
                var e = xml.CreateElement("uses-permission");
                e.SetAttribute("name", EspacioAndroid, permiso);
                raiz.PrependChild(e);
            }
            foreach (var prohibido in raiz.SelectNodes($"uses-permission[@android:name='{PermisoProhibido}']", nombres).Cast<XmlNode>().ToList())
                raiz.RemoveChild(prohibido);

            var aplicacion = raiz.SelectSingleNode("application");
            if (aplicacion != null && aplicacion.SelectSingleNode($"service[@android:name='{ServicioRuta}']", nombres) == null)
            {
                var servicio = xml.CreateElement("service");
                servicio.SetAttribute("name", EspacioAndroid, ServicioRuta);
                servicio.SetAttribute("foregroundServiceType", EspacioAndroid, "location");
                servicio.SetAttribute("exported", EspacioAndroid, "false");
                aplicacion.AppendChild(servicio);
            }
            const string Saltar = "unityplayer.SkipPermissionsDialog";
            if (aplicacion != null && aplicacion.SelectSingleNode($"meta-data[@android:name='{Saltar}']", nombres) == null)
            {
                var meta = xml.CreateElement("meta-data");
                meta.SetAttribute("name", EspacioAndroid, Saltar);
                meta.SetAttribute("value", EspacioAndroid, "true");
                aplicacion.AppendChild(meta);
            }
            xml.Save(manifiesto);
        }
    }
}
