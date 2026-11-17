using NUnit.Framework;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.Vectorial;
using UnityEngine;

namespace PriceMap.Tests.EditMode
{
    /// <summary>El adaptador real del mapa cumple el puerto IMapa y sus shaders existen con su nombre nuevo.</summary>
    public class PruebasMapaVectorial
    {
        GameObject objCamara, objMapa;
        double latOrigen, lonOrigen;

        [SetUp]
        public void Preparar()
        {
            latOrigen = Geo.LatitudOrigen; lonOrigen = Geo.LongitudOrigen;
            // En EditMode no se ejecuta Awake: el origen se fija a mano, como haría MapaVectorial
            Geo.LatitudOrigen = 36.7779; Geo.LongitudOrigen = -6.3528;
            objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            objCamara.AddComponent<CamaraMapa>();
            objMapa = new GameObject("Mapa de prueba");
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(objMapa);
            Object.DestroyImmediate(objCamara);
            Geo.LatitudOrigen = latOrigen; Geo.LongitudOrigen = lonOrigen;
        }

        [Test]
        public void CentrarAntesDeStartMueveLaCamaraALaCoordenada()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>(); // sin Start: la cámara se busca al centrar
            var chemaBarber = new Coordenada(36.7601585, -6.3624415);

            mapa.Centrar(chemaBarber);

            var esperado = Geo.ALocal(chemaBarber.Latitud, chemaBarber.Longitud);
            var pivote = objCamara.GetComponent<CamaraMapa>().pivote;
            Assert.That(Vector3.Distance(pivote, esperado), Is.LessThan(0.01f), $"pivote {pivote}, esperado {esperado}");
        }

        [Test]
        public void ElTemaDeLaAppEligeLaPaletaDelMapa()
        {
            var mapa = objMapa.AddComponent<MapaVectorial>();
            ((IMapa)mapa).AplicarTema(Tema.Oscuro);
            Assert.AreEqual(TemaMapa.Oscuro, mapa.tema);
            ((IMapa)mapa).AplicarTema(Tema.Claro);
            Assert.AreEqual(TemaMapa.Claro, mapa.tema);
        }

        [Test]
        public void LosBotonesDeZoomAcercanYAlejan()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>();
            var camara = objCamara.GetComponent<CamaraMapa>();
            float inicial = camara.distancia;

            mapa.Acercar();
            Assert.Less(camara.distancia, inicial, "Acercar debe reducir la distancia de la cámara.");
            mapa.Alejar();
            Assert.AreEqual(inicial, camara.distancia, 0.01f, "Alejar deshace un paso de Acercar.");
            mapa.Alejar();
            Assert.Greater(camara.distancia, inicial, "Alejar debe aumentar la distancia de la cámara.");
        }

        [Test]
        public void ElMapaCargaAlrededorDeLaVistaYLiberaLoLejano()
        {
            var mapa = objMapa.AddComponent<MapaVectorial>();
            var camara = objCamara.GetComponent<CamaraMapa>();
            ((IMapa)mapa).Centrar(new Coordenada(36.7779, -6.3528));

            mapa.ActualizarTeselas();
            Assert.AreEqual(9, mapa.TeselasPendientes, "Al empezar, 3×3 teselas en cola.");

            // Supongamos que ya se construyeron las 3×3 del centro
            var origen = Geo.Tesela(36.7779, -6.3528, mapa.zoom);
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++) mapa.SimularCargada(new Vector2Int(origen.x + dx, origen.y + dy));
            Assert.AreEqual(9, mapa.TeselasCargadas);

            // Cinco teselas al este (≈ 0,11° de longitud a zoom 14): todas quedan a más de 3 y se liberan
            ((IMapa)mapa).Centrar(new Coordenada(36.7779, -6.3528 + 5 * 360.0 / (1 << mapa.zoom)));
            mapa.ActualizarTeselas();
            Assert.AreEqual(0, mapa.TeselasCargadas, "Las teselas lejanas se liberan.");
            Assert.AreEqual(18, mapa.TeselasPendientes, "Las 9 iniciales siguen en cola y se añaden las 9 nuevas.");

            // Con la cámara lejos se añade un anillo: 5×5 alrededor del nuevo centro
            camara.distancia = 3000f;
            mapa.ActualizarTeselas();
            Assert.AreEqual(9 + 25, mapa.TeselasPendientes);
        }

        [TestCase("PriceMap/PlanoPaleta")]
        [TestCase("PriceMap/EdificiosPaleta")]
        public void ElShaderDelMapaExisteConSuNombre(string nombre)
        {
            var shader = Shader.Find(nombre);
            Assert.IsNotNull(shader, $"No se encuentra el shader {nombre}");
            Assert.IsTrue(shader.isSupported, $"El shader {nombre} no es compatible");
        }
    }
}
