using System.IO;
using System.Xml;
using NUnit.Framework;
using PriceMap.EditorTools;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Historia 1.7: al generar el proyecto de Android, el manifiesto lleva la ubicación precisa y la aproximada
    /// (una vez cada una) y le dice a Unity que no pida permisos al arrancar. Se prueba sobre un manifiesto mínimo.
    /// </summary>
    public class PruebasPermisosUbicacionAndroid
    {
        const string Android = "http://schemas.android.com/apk/res/android";
        string carpeta;

        [SetUp]
        public void Preparar()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "PriceMapManifiesto_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(carpeta, "src", "main"));
            File.WriteAllText(Manifiesto,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<manifest xmlns:android=\"" + Android + "\">\n" +
                "  <application />\n" +
                "</manifest>\n");
        }

        [TearDown]
        public void Limpiar()
        {
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        }

        string Manifiesto => Path.Combine(carpeta, "src", "main", "AndroidManifest.xml");

        int Contar(XmlDocument xml, string etiqueta, string nombre)
        {
            int n = 0;
            foreach (XmlElement e in xml.GetElementsByTagName(etiqueta))
                if (e.GetAttribute("name", Android) == nombre) n++;
            return n;
        }

        void Comprobar()
        {
            var xml = new XmlDocument();
            xml.Load(Manifiesto);
            Assert.AreEqual(1, Contar(xml, "uses-permission", "android.permission.ACCESS_FINE_LOCATION"));
            Assert.AreEqual(1, Contar(xml, "uses-permission", "android.permission.ACCESS_COARSE_LOCATION"));
            Assert.AreEqual(1, Contar(xml, "meta-data", "unityplayer.SkipPermissionsDialog"));
            foreach (XmlElement e in xml.GetElementsByTagName("meta-data"))
                if (e.GetAttribute("name", Android) == "unityplayer.SkipPermissionsDialog")
                {
                    Assert.AreEqual("true", e.GetAttribute("value", Android));
                    Assert.AreEqual("application", e.ParentNode.Name, "El meta-data va dentro de <application>.");
                }
        }

        [Test]
        public void AnadeLosDosPermisosYQueUnityNoLosPidaAlArrancar()
        {
            new PermisosUbicacionAndroid().OnPostGenerateGradleAndroidProject(carpeta);
            Comprobar();
        }

        [Test]
        public void GenerarDosVecesNoDuplicaNada()
        {
            var paso = new PermisosUbicacionAndroid();
            paso.OnPostGenerateGradleAndroidProject(carpeta);
            paso.OnPostGenerateGradleAndroidProject(carpeta);
            Comprobar();
        }
    }
}
