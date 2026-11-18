using System.Collections.Generic;
using NUnit.Framework;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// La interfaz real (PantallaMapa.uxml) reacciona al tema y sus controles bloquean el mapa;
    /// el árbol se construye sin panel, como en el UIDocument.
    /// </summary>
    public class PruebasPantallaMapa
    {
        class MapaFalso : IMapa
        {
            public int acercar, alejar;
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() => acercar++;
            public void Alejar() => alejar++;
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres { get; } = new PuntoInteres[0];
            public event System.Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event System.Action<PuntoPantalla> Toque { add { } remove { } }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        GameObject objeto;
        PantallaMapa pantalla;
        VisualElement arbol;
        ServicioTema tema;

        [SetUp]
        public void Preparar()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            tema = new ServicioTema(new PreferenciasFalsas(), new SistemaClaro());
            pantalla.Inicializar(new MapaFalso(), tema, null, System.DateTime.UtcNow, arbol);
            tema.Iniciar(); // como en el Arranque
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(objeto);

        [Test]
        public void ElBotonDeTemaCambiaLaInterfazYSuIcono()
        {
            Assert.IsFalse(pantalla.InterfazOscura);
            Assert.AreEqual(Icono.Luna, pantalla.BotonTema.Icono, "En claro, el botón ofrece pasar a oscuro.");

            tema.Alternar();

            Assert.IsTrue(pantalla.InterfazOscura, "La raíz debe llevar .tema-oscuro.");
            Assert.AreEqual(Icono.Sol, pantalla.BotonTema.Icono);
        }

        /// <summary>
        /// En EditMode Localization no carga la locale (fuera de Play), así que se ve la clave de reserva;
        /// el texto real se comprueba en el ejecutable con -captura. Aquí: nunca vacía.
        /// </summary>
        [Test]
        public void LaAtribucionNuncaQuedaVacia()
        {
            var atribucion = arbol.Q<Label>("atribucion");
            Assert.IsFalse(string.IsNullOrWhiteSpace(atribucion.text));
        }

        [Test]
        public void SoloLosControlesBloqueanElMapa()
        {
            var raiz = arbol.Q("raiz");
            Assert.IsTrue(PantallaMapa.EsControl(raiz, pantalla.BotonTema), "El botón de tema es un control.");
            Assert.IsFalse(PantallaMapa.EsControl(raiz, raiz), "La raíz no debe tapar el mapa.");
            Assert.IsFalse(PantallaMapa.EsControl(raiz, arbol.Q("zona-segura")), "Los contenedores no tapan el mapa.");
            Assert.IsFalse(PantallaMapa.EsControl(raiz, arbol.Q("atribucion")), "La atribución no tapa el mapa.");
            Assert.IsFalse(PantallaMapa.EsControl(raiz, null));
        }
    }
}
