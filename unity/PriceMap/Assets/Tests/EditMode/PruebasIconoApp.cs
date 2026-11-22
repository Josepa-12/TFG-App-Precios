using NUnit.Framework;
using PriceMap.EditorTools;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Icono de la app (logo del pin con el euro): las tres imágenes están, son cuadradas de 1024, el fondo
    /// adaptativo no tiene transparencias (cada móvil lo recorta con su forma) y la capa delantera va vacía.
    /// Tras PonerIconos, Android lleva el adaptativo con dos capas y el clásico con el logo.
    /// </summary>
    public class PruebasIconoApp
    {
        static Texture2D Cargar(string ruta)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            Assert.IsNotNull(t, ruta);
            return t;
        }

        static Color32[] Pixeles(string ruta)
        {
            // Se lee el PNG de disco: la textura importada puede ir comprimida y sin lectura
            var t = new Texture2D(2, 2);
            Assert.IsTrue(t.LoadImage(System.IO.File.ReadAllBytes(ruta)), ruta);
            var px = t.GetPixels32();
            Object.DestroyImmediate(t);
            return px;
        }

        [TestCase(ConfigurarAndroid.IconoApp)]
        [TestCase(ConfigurarAndroid.IconoFondo)]
        [TestCase(ConfigurarAndroid.IconoFrente)]
        public void LasImagenesSonCuadradasDe1024(string ruta)
        {
            var t = Cargar(ruta);
            Assert.AreEqual(1024, t.width);
            Assert.AreEqual(1024, t.height);
        }

        [Test]
        public void ElFondoAdaptativoEsOpacoYElFrenteVacio()
        {
            foreach (var p in Pixeles(ConfigurarAndroid.IconoFondo)) Assert.AreEqual(255, p.a);
            foreach (var p in Pixeles(ConfigurarAndroid.IconoFrente)) Assert.AreEqual(0, p.a);
        }

        [Test]
        public void ElLogoClasicoTieneLasEsquinasTransparentesYElCentroOpaco()
        {
            var px = Pixeles(ConfigurarAndroid.IconoApp);
            Assert.AreEqual(0, px[0].a);                    // esquina redondeada
            Assert.AreEqual(255, px[512 * 1024 + 512].a);   // la moneda del centro
        }

        [Test]
        public void PonerIconosDejaElAdaptativoConDosCapas()
        {
            ConfigurarAndroid.PonerIconos();
            var adaptativos = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
            Assert.IsNotEmpty(adaptativos);
            foreach (var i in adaptativos)
            {
                var capas = i.GetTextures();
                Assert.AreEqual(Cargar(ConfigurarAndroid.IconoFondo), capas[0]);
                Assert.AreEqual(Cargar(ConfigurarAndroid.IconoFrente), capas[1]);
            }
            foreach (var i in PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Legacy))
                Assert.AreEqual(Cargar(ConfigurarAndroid.IconoApp), i.GetTexture());
        }
    }
}
