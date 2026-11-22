using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Historia 2.8: desde Android 11 una app solo ve los motores de voz del sistema si lo declara en el manifiesto
    /// (<c>&lt;queries&gt;</c> con la acción <c>android.intent.action.TTS_SERVICE</c>); sin eso, el TextToSpeech de
    /// com.pricemap.voz.PuenteVoz no encuentra motor y no habla. Se añade al generar el proyecto Gradle, sin duplicarlo.
    /// </summary>
    public class MotorVozAndroid : IPostGenerateGradleAndroidProject
    {
        const string EspacioAndroid = "http://schemas.android.com/apk/res/android";
        public const string AccionMotorVoz = "android.intent.action.TTS_SERVICE";

        public int callbackOrder => 2; // tras CompartirAndroid

        public void OnPostGenerateGradleAndroidProject(string ruta)
        {
            var manifiesto = Path.Combine(ruta, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifiesto)) { Debug.LogWarning("PriceMap: no se encuentra " + manifiesto + " para la voz."); return; }
            var xml = new XmlDocument();
            xml.Load(manifiesto);
            var nombres = new XmlNamespaceManager(xml.NameTable);
            nombres.AddNamespace("android", EspacioAndroid);
            if (xml.DocumentElement.SelectSingleNode($"queries/intent[action/@android:name='{AccionMotorVoz}']", nombres) != null) return;

            var consultas = xml.DocumentElement.SelectSingleNode("queries") as XmlElement;
            if (consultas == null)
            {
                consultas = xml.CreateElement("queries");
                xml.DocumentElement.AppendChild(consultas);
            }
            var intento = xml.CreateElement("intent");
            var accion = xml.CreateElement("action");
            accion.SetAttribute("name", EspacioAndroid, AccionMotorVoz);
            intento.AppendChild(accion);
            consultas.AppendChild(intento);
            xml.Save(manifiesto);
        }
    }
}
