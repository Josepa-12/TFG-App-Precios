using System.Linq;
using NUnit.Framework;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace PriceMap.Tests.EditMode
{
    /// <summary>La escena del build tiene un único Arranque con el mapa y la pantalla conectados.</summary>
    public class PruebasEscenaPrincipal
    {
        const string RutaEscena = "Assets/Scenes/Principal.unity";
        Scene escena;

        [SetUp]
        public void Abrir() => escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);

        [TearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        T[] Buscar<T>() where T : UnityEngine.Component =>
            escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void HayUnSoloArranqueConElMapaYLaPantallaConectados()
        {
            var arranques = Buscar<Arranque.Arranque>();
            Assert.AreEqual(1, arranques.Length, "Debe haber exactamente un Arranque en la escena principal.");

            var so = new SerializedObject(arranques[0]);
            Assert.IsNotNull(so.FindProperty("mapa").objectReferenceValue as MapaVectorial, "El Arranque no tiene el mapa conectado.");
            Assert.IsNotNull(so.FindProperty("pantallaMapa").objectReferenceValue as PantallaMapa, "El Arranque no tiene la PantallaMapa conectada.");
            Assert.AreEqual(1, Buscar<MapaVectorial>().Length);
            Assert.AreEqual(1, Buscar<PantallaMapa>().Length);
            Assert.AreEqual(1, Buscar<CamaraMapa>().Length);
        }

        [Test]
        public void LaEscenaPrincipalEsLaUnicaDelBuild()
        {
            CollectionAssert.AreEqual(new[] { RutaEscena }, EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray());
        }
    }
}
