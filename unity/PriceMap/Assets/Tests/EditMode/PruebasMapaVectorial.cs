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
        public void ElCentroEsLaCoordenadaCentrada()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>();
            var c = new Coordenada(36.7601585, -6.3624415);
            mapa.Centrar(c);
            Assert.That(mapa.Centro.DistanciaMetros(c), Is.LessThan(0.05), $"centro {mapa.Centro.Latitud}, {mapa.Centro.Longitud}");

            var antes = mapa.Centro;
            objCamara.GetComponent<CamaraMapa>().pivote += new Vector3(100f, 0f, 0f); // arrastre de 100 m al este
            Assert.That(mapa.Centro.DistanciaMetros(antes), Is.GreaterThan(90.0), "Mover el pivote mueve el centro.");
            Assert.Greater(mapa.Centro.Longitud, antes.Longitud);
        }

        [Test]
        public void OrientarNortePoneElGiroACeroSinTocarZoomNiInclinacion()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>();
            var camara = objCamara.GetComponent<CamaraMapa>();
            camara.giro = 40f;
            float distancia = camara.distancia, inclinacion = camara.inclinacion;

            mapa.OrientarNorte();

            Assert.AreEqual(0f, camara.giro);
            Assert.AreEqual(0f, mapa.Giro);
            Assert.AreEqual(distancia, camara.distancia);
            Assert.AreEqual(inclinacion, camara.inclinacion);
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

        [Test]
        public void ProyectarDaPixelesDesdeArribaYLaProfundidad()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>();
            var cam = objCamara.GetComponent<Camera>();
            var c = new Coordenada(36.78, -6.35);
            mapa.Centrar(c);

            // El pivote está en el centro de la vista; Y se cuenta desde arriba de la pantalla
            var centro = mapa.Proyectar(c);
            Assert.IsTrue(centro.Delante);
            Assert.AreEqual(cam.pixelWidth / 2f, centro.X, 1f);
            Assert.AreEqual(Screen.height - cam.pixelHeight / 2f, centro.Y, 1f);

            // Unos 220 m al norte: la cámara mira hacia el norte, así que queda más arriba (Y menor)
            var norte = mapa.Proyectar(new Coordenada(c.Latitud + 0.002, c.Longitud));
            Assert.IsTrue(norte.Delante);
            Assert.Less(norte.Y, centro.Y);

            // Unos 3 km al sur: detrás de la cámara
            var detras = mapa.Proyectar(new Coordenada(c.Latitud - 0.03, c.Longitud));
            Assert.LessOrEqual(detras.Profundidad, 0f);
            Assert.IsFalse(detras.Delante);
        }

        [Test]
        public void CentrarEnUnPuntoDePantallaDejaAhiLaCoordenadaSinTocarLaCamara()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>();
            var camara = objCamara.GetComponent<CamaraMapa>();
            var cam = objCamara.GetComponent<Camera>();
            var clinicaAreas = new Coordenada(36.7779, -6.3528);
            // Inclinado y girado, como queda tras unos gestos
            camara.giro = 35f; camara.inclinacion = 50f; camara.distancia = 900f;
            float giro = camara.giro, inclinacion = camara.inclinacion, distancia = camara.distancia;

            // Centro horizontal y mitad de la zona libre sobre una hoja del 72 % (px, Y desde arriba)
            float ancho = cam.pixelWidth, arriba = Screen.height - cam.pixelHeight;
            var objetivo = new PuntoPantalla(ancho / 2f, arriba + cam.pixelHeight * (1f - 0.72f) / 2f, 1f);
            mapa.Centrar(clinicaAreas, objetivo);

            var p = mapa.Proyectar(clinicaAreas);
            Assert.IsTrue(p.Delante);
            Assert.AreEqual(objetivo.X, p.X, 2f, $"proyección {p}, objetivo {objetivo}");
            Assert.AreEqual(objetivo.Y, p.Y, 2f, $"proyección {p}, objetivo {objetivo}");
            Assert.AreEqual(giro, camara.giro);
            Assert.AreEqual(inclinacion, camara.inclinacion);
            Assert.AreEqual(distancia, camara.distancia);
        }

        [Test]
        public void ConLaInclinacionMinimaElLugarQuedaPorEncimaDelCentro()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>();
            var camara = objCamara.GetComponent<CamaraMapa>();
            var cam = objCamara.GetComponent<Camera>();
            var c = new Coordenada(36.7779, -6.3528);
            camara.giro = 35f; camara.inclinacion = 20f; camara.distancia = 900f;
            float giro = camara.giro, inclinacion = camara.inclinacion, distancia = camara.distancia;

            // El 14 % desde arriba cae por encima del horizonte: se usa el punto más alto que toca el suelo
            float arriba = Screen.height - cam.pixelHeight;
            var objetivo = new PuntoPantalla(cam.pixelWidth / 2f, arriba + cam.pixelHeight * (1f - 0.72f) / 2f, 1f);
            mapa.Centrar(c, objetivo);

            var p = mapa.Proyectar(c);
            float centroY = arriba + cam.pixelHeight / 2f;
            Assert.IsTrue(p.Delante);
            Assert.Less(p.Y, centroY - 1f, $"proyección {p}: debe quedar por encima del centro ({centroY}).");
            Assert.AreEqual(objetivo.X, p.X, 2f);
            Assert.AreEqual(giro, camara.giro);
            Assert.AreEqual(inclinacion, camara.inclinacion);
            Assert.AreEqual(distancia, camara.distancia);
        }

        [Test]
        public void ElToqueDeLaCamaraLlegaPorElPuertoUnaVezYConLaYDesdeArriba()
        {
            IMapa mapa = objMapa.AddComponent<MapaVectorial>();
            var camara = objCamara.GetComponent<CamaraMapa>();
            var recibidos = new System.Collections.Generic.List<PuntoPantalla>();
            mapa.Toque += recibidos.Add;
            // La cámara se busca (y se suscribe) al usarla; dos usos no deben suscribirla dos veces
            mapa.Centrar(new Coordenada(36.78, -6.35));
            mapa.Centrar(new Coordenada(36.78, -6.35));

            camara.SimularToque(new Vector2(120f, 30f));

            Assert.AreEqual(1, recibidos.Count, "El toque llega una sola vez.");
            Assert.AreEqual(120f, recibidos[0].X, 1e-3f);
            Assert.AreEqual(Screen.height - 30f, recibidos[0].Y, 1e-3f, "Y se cuenta desde arriba, como Proyectar.");
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
