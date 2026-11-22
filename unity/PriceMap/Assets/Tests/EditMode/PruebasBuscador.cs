using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Buscador (historia 2.2, parte 1): la física de la hoja (FisicaHoja, una prueba por fila de la matriz) y la
    /// cápsula y la hoja en la pantalla real (PantallaMapa.uxml): gestos, botón atrás y exclusión con la ficha.
    /// Sin panel (EditMode) la hoja no se dispone: se le da el alto del hueco con Medir.
    /// </summary>
    public class PruebasBuscador
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        const float AltoUtil = 600f;

        // ───────────────────────── Física (FisicaHoja) ─────────────────────────

        static FisicaHoja Fisica(float fraccion = 0f)
        {
            var f = new FisicaHoja();
            f.PonerAltoUtil(AltoUtil);
            f.Poner(fraccion);
            return f;
        }

        /// <summary>
        /// Arrastra desde y0 hasta y1 a velocidad constante (dp/ms; muestras cada 10 ms) y suelta <paramref name="esperaMs"/>
        /// después del último movimiento.
        /// </summary>
        static FisicaHoja.Suelta Arrastrar(FisicaHoja f, float y0, float y1, float velocidad, double esperaMs = 5.0)
        {
            f.Bajar(new Vector2(200f, y0), 0.0);
            float signo = Mathf.Sign(y1 - y0), distancia = Mathf.Abs(y1 - y0);
            double fin = distancia / velocidad; // el último movimiento llega justo a y1 a la misma velocidad
            double t = 0.0;
            while (t < fin)
            {
                t = Math.Min(t + 10.0, fin);
                float y = t >= fin ? y1 : y0 + signo * velocidad * (float)t;
                f.Mover(new Vector2(200f, y), t);
            }
            return f.Soltar(t + esperaMs);
        }

        [Test]
        public void ArrastrarHastaLaMitadYSoltarDespacioLaDejaEnLaMitad()
        {
            var f = Fisica();
            var r = Arrastrar(f, 700f, 700f - 0.5f * AltoUtil, 0.1f);
            Assert.IsTrue(r.FueArrastre);
            Assert.AreEqual(DestinoHoja.Mitad, r.Destino);
            Assert.AreEqual(0.5f, f.Fraccion, 0.001f);
            Assert.AreEqual(-0.1f, f.Velocidad, 0.001f, "Velocidad medida hacia arriba.");
        }

        // Matriz de la spec de las tres paradas: soltar sin lanzamiento va a la parada más cercana
        [TestCase(0.8f, DestinoHoja.Abierta, 1f)]   // cerca de arriba: completa
        [TestCase(0.6f, DestinoHoja.Mitad, 0.5f)]   // en medio: mitad
        [TestCase(0.4f, DestinoHoja.Mitad, 0.5f)]
        [TestCase(0.2f, DestinoHoja.Plegada, 0f)]   // cerca de abajo: cerrada
        [TestCase(0.26f, DestinoHoja.Mitad, 0.5f)]  // junto a los cortes (0,25 y 0,75)
        [TestCase(0.74f, DestinoHoja.Mitad, 0.5f)]
        [TestCase(0.76f, DestinoHoja.Abierta, 1f)]
        [TestCase(0.24f, DestinoHoja.Plegada, 0f)]
        public void SoltarSinLanzamientoVaALaParadaMasCercana(float fraccion, DestinoHoja destino, float queda)
        {
            var f = Fisica();
            var r = Arrastrar(f, 700f, 700f - fraccion * AltoUtil, 0.05f, esperaMs: 200.0);
            Assert.AreEqual(destino, r.Destino);
            Assert.AreEqual(queda, f.Fraccion, 0.0001f);
            Assert.AreEqual(queda, r.Fraccion, 0.0001f);
        }

        [Test]
        public void DesdeArribaSoltarDespacioTambienVaALaMasCercana()
        {
            var f = Fisica(1f);
            Arrastrar(f, 100f, 100f + 0.45f * AltoUtil, 0.05f, esperaMs: 200.0); // de 1 a 0,55
            Assert.AreEqual(0.5f, f.Fraccion, 0.0001f);
            Arrastrar(f, 400f, 400f + 0.3f * AltoUtil, 0.05f, esperaMs: 200.0);  // de 0,5 a 0,2
            Assert.AreEqual(0f, f.Fraccion);
        }

        [Test]
        public void LanzarHaciaArribaDesdeCerradaVaALaMitadYOtroACompleta()
        {
            var f = Fisica();
            var r = Arrastrar(f, 700f, 650f, 0.9f); // al 8 %: sin lanzamiento se cerraría
            Assert.AreEqual(DestinoHoja.Mitad, r.Destino);
            Assert.AreEqual(0.5f, f.Fraccion, 0.0001f);
            r = Arrastrar(f, 400f, 350f, 0.9f);
            Assert.AreEqual(DestinoHoja.Abierta, r.Destino);
            Assert.AreEqual(1f, f.Fraccion);
            r = Arrastrar(f, 100f, 50f, 0.9f);
            Assert.AreEqual(DestinoHoja.Abierta, r.Destino, "En el extremo se queda.");
            Assert.AreEqual(1f, f.Fraccion);
        }

        [Test]
        public void LanzarHaciaAbajoDesdeCompletaVaALaMitadYOtroACerrada()
        {
            var f = Fisica(1f);
            var r = Arrastrar(f, 100f, 150f, 0.9f); // al 92 %: sin lanzamiento volvería a completa
            Assert.AreEqual(DestinoHoja.Mitad, r.Destino);
            Assert.AreEqual(0.5f, f.Fraccion, 0.0001f);
            r = Arrastrar(f, 400f, 450f, 0.9f);
            Assert.AreEqual(DestinoHoja.Plegada, r.Destino);
            Assert.AreEqual(0f, f.Fraccion);
            r = Arrastrar(f, 700f, 750f, 0.9f);
            Assert.AreEqual(DestinoHoja.Plegada, r.Destino, "En el extremo se queda.");
            Assert.AreEqual(0f, f.Fraccion);
        }

        /// <summary>
        /// Arrastra la hoja (desde la fracción en que esté) hasta la fracción k y la suelta con la velocidad v (dp/ms,
        /// positiva hacia abajo): el último movimiento, de 10 ms, llega a k a esa velocidad.
        /// </summary>
        static FisicaHoja.Suelta SoltarEn(FisicaHoja f, float k, float v)
        {
            const float y0 = 1000f;
            float objetivo = y0 - Mathf.Round((k - f.Fraccion) * AltoUtil); // dp enteros: k exacta en los cortes
            f.Bajar(new Vector2(200f, y0), 0.0);
            f.Mover(new Vector2(200f, y0 + 50f), 500.0);           // empieza el arrastre, despacio
            f.Mover(new Vector2(200f, objetivo - Mathf.Round(v * 100f) / 10f), 1000.0); // recorrido exacto (6 dp a 0,6)
            f.Mover(new Vector2(200f, objetivo), 1010.0);           // velocidad medida: v
            return f.Soltar(1015.0);
        }

        // Fracción en que queda la hoja soltada en k = i/20 (i = 0…20), sin lanzamiento y con lanzamiento arriba y abajo
        static readonly float[] QuedaSinLanzar =
            { 0, 0, 0, 0, 0, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, 1, 1, 1, 1, 1 };
        static readonly float[] QuedaLanzandoArriba =
            { .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        static readonly float[] QuedaLanzandoAbajo =
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f };

        [TestCase(0f)]
        [TestCase(-2f)]
        [TestCase(2f)]
        public void SoltarEnCadaFraccionLlevaASuParada(float v)
        {
            var esperado = v < 0f ? QuedaLanzandoArriba : v > 0f ? QuedaLanzandoAbajo : QuedaSinLanzar;
            for (int i = 0; i <= 20; i++)
            {
                var f = Fisica();
                var r = SoltarEn(f, i / 20f, v);
                Assert.IsTrue(r.FueArrastre, $"k = {i}/20");
                Assert.AreEqual(v, f.Velocidad, 0.0001f, $"k = {i}/20: velocidad");
                Assert.AreEqual(esperado[i], f.Fraccion, 0.0001f, $"k = {i}/20, v = {v}");
                Assert.AreEqual(esperado[i], FisicaHoja.FraccionDe(r.Destino), 0.0001f, $"k = {i}/20: destino");
            }
        }

        [Test]
        public void EnLosCortesExactosVaALaMitad()
        {
            Assert.AreEqual(DestinoHoja.Mitad, FisicaHoja.Destino(0.25f, 0f));
            Assert.AreEqual(DestinoHoja.Mitad, FisicaHoja.Destino(0.75f, 0f));
            Assert.AreEqual(DestinoHoja.Mitad, SoltarEn(Fisica(), 0.25f, 0f).Destino);
            Assert.AreEqual(DestinoHoja.Mitad, SoltarEn(Fisica(), 0.75f, 0f).Destino);
        }

        [Test]
        public void A06DpPorMsJustoNoEsUnLanzamiento()
        {
            Assert.AreEqual(DestinoHoja.Plegada, FisicaHoja.Destino(0.2f, -FisicaHoja.Lanzamiento));
            Assert.AreEqual(DestinoHoja.Abierta, FisicaHoja.Destino(0.8f, FisicaHoja.Lanzamiento));
            var f = Fisica();
            var r = SoltarEn(f, 0.2f, -0.6f);
            Assert.AreEqual(-FisicaHoja.Lanzamiento, f.Velocidad, 0f, "Medida exactamente a 0,6 dp/ms.");
            Assert.AreEqual(DestinoHoja.Plegada, r.Destino, "No es un lanzamiento: la más cercana.");
            var g = Fisica();
            Assert.AreEqual(DestinoHoja.Abierta, SoltarEn(g, 0.8f, 0.6f).Destino);
        }

        [Test]
        public void UnLanzamientoVaALaParadaSiguienteDesdeDondeEstaLaHoja()
        {
            // De cerrada, arrastrada más allá de la mitad y lanzada hacia arriba: completa, no de vuelta a la mitad
            Assert.AreEqual(DestinoHoja.Abierta, SoltarEn(Fisica(0f), 0.6f, -2f).Destino);
            // De completa, arrastrada por debajo de la mitad y lanzada hacia abajo: cerrada
            Assert.AreEqual(DestinoHoja.Plegada, SoltarEn(Fisica(1f), 0.4f, 2f).Destino);
            // Cambiando de sentido a medio arrastre: la parada siguiente desde donde está, no desde la de partida
            Assert.AreEqual(DestinoHoja.Mitad, SoltarEn(Fisica(0.5f), 0.3f, -2f).Destino,
                "De la mitad bajada al 30 % y lanzada hacia arriba: mitad.");
            Assert.AreEqual(DestinoHoja.Mitad, SoltarEn(Fisica(0f), 0.9f, 2f).Destino,
                "De cerrada subida al 90 % y lanzada hacia abajo: mitad.");
        }

        [Test]
        public void CancelarUnArrastreLaLlevaALaParadaMasCercana()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            f.Mover(new Vector2(200f, 700f - 0.4f * AltoUtil), 400.0);
            f.Cancelar();
            Assert.IsFalse(f.Pulsado);
            Assert.AreEqual(0.5f, f.Fraccion, 0.0001f, "No se queda a medias.");
        }

        [Test]
        public void LaHojaSigueAlDedo()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            Assert.IsTrue(f.Mover(new Vector2(200f, 640f), 50.0));
            Assert.AreEqual(60f / AltoUtil, f.Fraccion, 0.0001f);
            f.Mover(new Vector2(200f, 400f), 300.0);
            Assert.AreEqual(0.5f, f.Fraccion, 0.0001f);
            f.Mover(new Vector2(200f, 900f), 600.0);
            Assert.AreEqual(0f, f.Fraccion, "No baja de plegada.");
            f.Mover(new Vector2(200f, -200f), 900.0);
            Assert.AreEqual(1f, f.Fraccion, "No sube de abierta del todo.");
        }

        [Test]
        public void SiElDedoSeParaAntesDeSoltarNoEsUnLanzamiento()
        {
            var f = Fisica();
            var r = Arrastrar(f, 700f, 700f - 0.6f * AltoUtil, 2f, esperaMs: 91.0);
            Assert.AreEqual(0.5f, r.Fraccion, 0.001f, "Parado más de 90 ms: velocidad 0 y va a la más cercana (mitad).");
            Assert.AreEqual(DestinoHoja.Mitad, r.Destino);
            Assert.AreEqual(0f, f.Velocidad);

            var g = Fisica();
            Arrastrar(g, 700f, 700f - 0.6f * AltoUtil, 2f, esperaMs: 80.0);
            Assert.AreEqual(1f, g.Fraccion, "Soltado a los 80 ms: es un lanzamiento hacia arriba (pasada la mitad, completa).");
        }

        [Test]
        public void LaVelocidadSoloSeMuestreaPasados4Ms()
        {
            var f = Fisica();
            f.Bajar(new Vector2(0f, 700f), 0.0);
            f.Mover(new Vector2(0f, 690f), 10.0); // empieza el arrastre: la primera muestra abarca desde que bajó el dedo
            Assert.AreEqual(-1f, f.Velocidad, 0.001f, "(690 − 700) / 10 ms: recorrido y tiempo del mismo tramo.");
            f.Mover(new Vector2(0f, 680f), 20.0);
            Assert.AreEqual(-1f, f.Velocidad, 0.001f, "(680 − 690) / 10 ms.");
            f.Mover(new Vector2(0f, 600f), 23.0); // 3 ms: no se vuelve a medir
            Assert.AreEqual(-1f, f.Velocidad, 0.001f);
            f.Mover(new Vector2(0f, 590f), 25.0); // 5 ms desde la última muestra (20 ms)
            Assert.AreEqual(-18f, f.Velocidad, 0.001f, "(590 − 680) / 5 ms.");
        }

        [Test]
        public void UnArrastreLentoQueAcabaDePasarElUmbralNoEsUnLanzamiento()
        {
            var f = Fisica(0.5f);
            f.Bajar(new Vector2(200f, 400f), 0.0);
            f.Mover(new Vector2(200f, 397f), 30.0);  // 0,1 dp/ms, aún toque
            f.Mover(new Vector2(200f, 393f), 70.0);  // pasa los 6 dp: arrastre
            var r = f.Soltar(75.0);
            Assert.AreEqual(-0.1f, f.Velocidad, 0.001f, "La velocidad real, no la del recorrido previo dividido por un instante.");
            Assert.AreEqual(DestinoHoja.Mitad, r.Destino, "Sin lanzamiento sigue en la mitad.");
        }

        [Test]
        public void UnLanzamientoDeUnSoloMovimientoCuenta()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            f.Mover(new Vector2(200f, 670f), 20.0); // 1,5 dp/ms de una vez
            var r = f.Soltar(25.0);
            Assert.AreEqual(DestinoHoja.Mitad, r.Destino, "Lanzada desde cerrada: la parada siguiente.");
        }

        [Test]
        public void UnGestoHorizontalNoMueveLaHoja()
        {
            var f = Fisica(1f);
            f.Bajar(new Vector2(200f, 120f), 0.0); // en el campo, p. ej. moviendo el cursor
            Assert.IsFalse(f.Mover(new Vector2(215f, 122f), 30.0));
            Assert.IsTrue(f.Ignorado);
            var r = f.Soltar(60.0);
            Assert.IsFalse(r.EsToque, "Tampoco es un toque.");
            Assert.AreEqual(1f, f.Fraccion);
        }

        [Test]
        public void LosValoresSonLosDelPrototipo()
        {
            Assert.AreEqual(64f, FisicaHoja.HuecoArriba);
            Assert.AreEqual(76f, FisicaHoja.AltoPlegado);
            Assert.AreEqual(0.6f, FisicaHoja.Lanzamiento);
            Assert.AreEqual(0.15f, FisicaHoja.MezclaHasta);
            Assert.AreEqual(0.5f, FisicaHoja.FraccionMitad);
            Assert.AreEqual(0.25f, FisicaHoja.CorteCerrada);
            Assert.AreEqual(0.75f, FisicaHoja.CorteCompleta);
            Assert.AreEqual(6f, FisicaHoja.UmbralToque);
            Assert.AreEqual(800f - 76f - 64f, FisicaHoja.AltoUtilDe(800f, 0f, 0f));
            Assert.AreEqual(800f - 24f - 76f - 64f - 30f, FisicaHoja.AltoUtilDe(800f, 30f, 24f), "Con las zonas seguras.");
        }

        [Test]
        public void UnMovimientoDe3DpEsUnToque()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            Assert.IsFalse(f.Mover(new Vector2(202f, 697f), 30.0));
            var r = f.Soltar(60.0);
            Assert.IsTrue(r.EsToque);
            Assert.AreEqual(DestinoHoja.Abierta, r.Destino);
            Assert.AreEqual(1f, f.Fraccion, "Un toque la abre del todo.");
        }

        [Test]
        public void UnMovimientoDe10DpEsUnArrastre()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            Assert.IsTrue(f.Mover(new Vector2(200f, 690f), 30.0));
            Assert.IsTrue(f.Arrastrando);
            var r = f.Soltar(200.0);
            Assert.IsFalse(r.EsToque);
            Assert.IsTrue(r.FueArrastre);
        }

        [TestCase(false, 0f, -10f, true)]   // no abierta del todo: siempre
        [TestCase(false, 50f, 10f, true)]
        [TestCase(true, 0f, 10f, true)]     // abierta, lista arriba del todo y hacia abajo
        [TestCase(true, 0f, -10f, false)]   // abierta y hacia arriba: se desplaza la lista
        [TestCase(true, 40f, 10f, false)]   // abierta con la lista desplazada
        public void LaListaMueveLaHoja(bool abierta, float desplazamiento, float dy, bool mueve)
        {
            Assert.AreEqual(mueve, FisicaHoja.ListaMueveHoja(abierta, desplazamiento, dy));

            var f = Fisica(abierta ? 1f : 0.5f);
            f.Bajar(new Vector2(200f, 400f), 0.0, enLista: true, desplazamientoLista: desplazamiento);
            f.Mover(new Vector2(200f, 400f + dy), 20.0);
            Assert.AreEqual(mueve, f.Arrastrando);
            Assert.AreEqual(!mueve, f.Ignorado, "Si no la mueve, la lista se desplaza y la hoja no hace nada.");
        }

        [Test]
        public void UnToqueEnLaListaNoEsUnToqueDelCampo()
        {
            var f = Fisica(1f);
            f.Bajar(new Vector2(200f, 400f), 0.0, enLista: true);
            var r = f.Soltar(50.0);
            Assert.IsFalse(r.EsToque);
            Assert.AreEqual(1f, f.Fraccion);
        }

        // ───────────────────────── Pantalla ─────────────────────────

        class MapaFalso : IMapa
        {
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event System.Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class AccesibilidadSistemaFalsa : IAccesibilidadSistema { public bool AnimacionesQuitadas { get; set; } }

        AccesibilidadSistemaFalsa sistema;

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        GameObject objeto;
        PantallaMapa pantalla;
        VisualElement arbol;
        HojaBuscador hoja;

        [SetUp]
        public void Crear()
        {
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            sistema = new AccesibilidadSistemaFalsa();
            pantalla.Inicializar(new MapaFalso(), tema, Lugares(), Ahora, arbol, filtro, accesibilidadSistema: sistema);
            tema.Iniciar();
            filtro.Iniciar();
            hoja = pantalla.Buscador;
            Assert.IsNotNull(hoja, "La pantalla crea el buscador.");
            hoja.Medir(800f); // sin panel no hay disposición: hueco de 800 dp
        }

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
        }

        /// <summary>Toque en la cápsula: baja y sube el dedo moviéndolo 3 dp.</summary>
        void TocarCapsula()
        {
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -3f), 30.0);
            hoja.Soltar(60.0);
        }

        [Test]
        public void LaCapsulaVaAbajoConElCampoYElAvatar()
        {
            Assert.AreSame(arbol.Q("hueco-buscador"), hoja.parent);
            Assert.IsTrue(hoja.Visible);
            Assert.IsFalse(hoja.Abierta, "Al arrancar, plegada.");
            Assert.AreEqual("Buscar en Sanlúcar", hoja.Campo.textEdition.placeholder);
            Assert.AreEqual(PickingMode.Position, hoja.pickingMode, "La cápsula recoge los toques: no pasan al mapa.");
            Assert.AreEqual(PickingMode.Position, hoja.Avatar.pickingMode);
            Assert.IsTrue(hoja.Avatar.ClassListContains("buscador__avatar"));
            Assert.IsTrue(SombraSuave.Tiene(hoja), "Sombra suave de los controles flotantes.");
            Assert.AreEqual(HojaBuscador.AltoCapsulaMin, hoja.AltoCapsula, "56 dp como mínimo.");
            Assert.AreEqual(800f - 56f - 20f, hoja.TopPlegada);
            Assert.AreEqual(800f - 76f - 64f, hoja.Fisica.AltoUtil, "Recorre de la cápsula a 64 dp de arriba.");
        }

        [Test]
        public void ElContenidoInicialSonLasCategoriasYEncuentraCerca()
        {
            var esperados = new[] { "Restaurantes", "Bares", "Supermercados", "Farmacias", "Gasolineras", "Deporte", "Salud", "Cultura" };
            CollectionAssert.AreEqual(esperados, hoja.Chips.Select(c => c.Q<Label>().text).ToArray());
            for (int i = 0; i < HojaBuscador.Categorias.Length; i++)
            {
                var icono = hoja.Chips[i].Q(className: "buscador__chip-icono");
                Assert.IsTrue(icono.ClassListContains("buscador__chip-icono--" + HojaBuscador.Nombre(HojaBuscador.Categorias[i])),
                    $"El icono de {esperados[i]} lleva el color de su categoría.");
            }
            CollectionAssert.AreEqual(new[] { CategoriaPoi.Comer, CategoriaPoi.Bares, CategoriaPoi.Super, CategoriaPoi.Farmacia,
                CategoriaPoi.Gasolina, CategoriaPoi.Deporte, CategoriaPoi.Salud, CategoriaPoi.Cultura }, HojaBuscador.Categorias);
            Assert.AreEqual("Encuentra cerca", hoja.Cerca.Q<Label>().text);
            Assert.IsTrue(hoja.Panel.contentContainer.Contains(hoja.Cerca));
        }

        [Test]
        public void LosGlifosDelBuscadorSonVectoriales()
        {
            CollectionAssert.Contains(IconosLugar.Glifos.ToList(), IconosLugar.Lupa);
            CollectionAssert.Contains(IconosLugar.Glifos.ToList(), IconosLugar.Avatar);
            Assert.IsNotEmpty(IconosLugar.Interpretar(IconosLugar.Lupa));
            Assert.IsNotEmpty(IconosLugar.Interpretar(IconosLugar.Avatar));
            foreach (var c in HojaBuscador.Categorias)
                Assert.AreNotEqual(IconosLugar.Generico, IconosLugar.GlifoDe(c), c.ToString());
        }

        [Test]
        public void UnToqueEnLaCapsulaLaAbreDelTodoYPideElTeclado()
        {
            TocarCapsula();
            Assert.IsTrue(hoja.AbiertaDelTodo);
            Assert.IsTrue(hoja.TecladoPedido, "Foco al campo y teclado.");
            Assert.IsTrue(pantalla.BuscadorAbierto);
            Assert.AreEqual(hoja.TopAbierta, hoja.Top, 0.001f, "Abierta del todo, a 64 dp de arriba.");
        }

        [Test]
        public void UnArrastreCortoNoSacaElTeclado()
        {
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -10f), 30.0);
            Assert.IsTrue(hoja.Arrastrando, "10 dp: arrastre.");
            Assert.IsTrue(hoja.ClassListContains(HojaBuscador.ClaseArrastrando), "Sin transición mientras sigue al dedo.");
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)), "Mientras se arrastra, el mapa no recibe el gesto.");
            hoja.Soltar(300.0);
            Assert.IsFalse(hoja.TecladoPedido);
            Assert.IsFalse(hoja.ClassListContains(HojaBuscador.ClaseArrastrando));
        }

        [Test]
        public void ArrastrarYSoltarALaMitadLaDejaAMediaAltura()
        {
            float y0 = hoja.TopPlegada + 20f, recorrido = hoja.Fisica.AltoUtil;
            hoja.Bajar(new Vector2(150f, y0), 0.0);
            for (int i = 1; i <= 10; i++) hoja.Mover(new Vector2(150f, y0 - recorrido * 0.05f * i), 500.0 * i);
            hoja.Soltar(5200.0);
            Assert.AreEqual(0.5f, hoja.Fraccion, 0.001f);
            Assert.AreEqual(hoja.TopPlegada - recorrido * 0.5f, hoja.Top, 0.01f);
            Assert.AreEqual(hoja.Top, hoja.style.top.value.value, 0.01f, "La hoja se pone en su sitio.");
        }

        /// <summary>Arrastra la hoja despacio (sin lanzamiento) desde la cápsula hasta esa fracción y la suelta.</summary>
        FisicaHoja.Suelta ArrastrarHojaHasta(float fraccion)
        {
            float y0 = hoja.TopPlegada + 20f, recorrido = hoja.Fisica.AltoUtil;
            hoja.Bajar(new Vector2(150f, y0), 0.0);
            for (int i = 1; i <= 10; i++) hoja.Mover(new Vector2(150f, y0 - recorrido * fraccion * i / 10f), 500.0 * i);
            return hoja.Soltar(5200.0);
        }

        [TestCase(0.4f)]
        [TestCase(0.6f)]
        public void SoltarEnMedioLaDejaEnLaMitadConLoEscrito(float fraccion)
        {
            hoja.Escribir("clinica");
            var r = ArrastrarHojaHasta(fraccion);
            Assert.AreEqual(DestinoHoja.Mitad, r.Destino);
            Assert.AreEqual(hoja.TopPlegada - hoja.Fisica.AltoUtil * 0.5f, hoja.Top, 0.01f, "A la mitad del recorrido.");
            Assert.IsTrue(hoja.Abierta);
            Assert.AreEqual("clinica", hoja.Campo.value, "Se conserva lo escrito.");
            Assert.IsFalse(hoja.TecladoPedido, "Un arrastre nunca saca el teclado.");
        }

        [Test]
        public void SoltarCercaDeArribaLaAbreCompleta()
        {
            ArrastrarHojaHasta(0.8f);
            Assert.IsTrue(hoja.AbiertaDelTodo);
            Assert.AreEqual(hoja.TopAbierta, hoja.Top, 0.01f);
        }

        [Test]
        public void SoltarCercaDeAbajoLaCierraSinFoco()
        {
            hoja.Escribir("clinica");
            TocarCapsula();
            Assert.IsTrue(hoja.TecladoPedido);
            hoja.Poner(0.5f);
            // De la mitad, bajándola despacio hasta el 20 %
            float y0 = hoja.Top + 20f;
            hoja.Bajar(new Vector2(150f, y0), 0.0, enLista: false);
            for (int i = 1; i <= 10; i++) hoja.Mover(new Vector2(150f, y0 + hoja.Fisica.AltoUtil * 0.03f * i), 500.0 * i);
            var r = hoja.Soltar(5200.0);
            Assert.AreEqual(DestinoHoja.Plegada, r.Destino);
            Assert.IsFalse(hoja.Abierta);
            Assert.IsFalse(hoja.TecladoPedido, "Cerrada, sin foco en el campo.");
            Assert.AreEqual("clinica", hoja.Campo.value, "El texto se queda para cuando se vuelva a abrir.");
        }

        [Test]
        public void LanzarLaHojaHaciaArribaVaParadaAParada()
        {
            void Lanzar(float dy)
            {
                float y0 = hoja.Top + 20f;
                hoja.Bajar(new Vector2(150f, y0), 0.0);
                hoja.Mover(new Vector2(150f, y0 + dy), 20.0); // 1,5 dp/ms
                hoja.Soltar(25.0);
            }
            Lanzar(-30f);
            Assert.AreEqual(0.5f, hoja.Fraccion, 0.0001f, "De cerrada, a la mitad.");
            Lanzar(-30f);
            Assert.IsTrue(hoja.AbiertaDelTodo, "De la mitad, completa.");
            Lanzar(30f);
            Assert.AreEqual(0.5f, hoja.Fraccion, 0.0001f, "De completa hacia abajo, a la mitad.");
            Lanzar(30f);
            Assert.IsFalse(hoja.Abierta, "De la mitad hacia abajo, cerrada.");
        }

        [Test]
        public void AtrasEnLaMitadLaCierra()
        {
            ArrastrarHojaHasta(0.5f);
            Assert.AreEqual(0.5f, hoja.Fraccion, 0.0001f);
            Assert.IsTrue(pantalla.Atras());
            Assert.AreEqual(0f, hoja.Fraccion);
        }

        [Test]
        public void LaFormaPasaDeCapsulaAHoja()
        {
            void Forma(float left, float bottom, float arriba, float abajo, bool abierta, string caso)
            {
                Assert.AreEqual(left, hoja.style.left.value.value, 0.01f, caso + ": izquierda");
                Assert.AreEqual(left, hoja.style.right.value.value, 0.01f, caso + ": derecha");
                Assert.AreEqual(bottom, hoja.style.bottom.value.value, 0.01f, caso + ": abajo");
                Assert.AreEqual(arriba, hoja.style.borderTopLeftRadius.value.value, 0.01f, caso + ": esquinas de arriba");
                Assert.AreEqual(abajo, hoja.style.borderBottomRightRadius.value.value, 0.01f, caso + ": esquinas de abajo");
                Assert.AreEqual(abierta, hoja.ClassListContains(HojaBuscador.ClaseAbierta), caso + ": fondo opaco");
            }
            Forma(12f, 20f, 28f, 28f, false, "Plegada");
            hoja.Poner(0.05f); // s = 1/3
            Forma(8f, 20f * 2f / 3f, 28f - 4f / 3f, 28f * 2f / 3f, false, "Al 5 %");
            hoja.Poner(0.15f);
            Forma(0f, 0f, 24f, 0f, true, "Al 15 %");
            hoja.Abrir();
            Forma(0f, 0f, 24f, 0f, true, "Abierta");
        }

        [Test]
        public void LasZonasSegurasMuevenLosExtremos()
        {
            hoja.PonerZonaSegura(30f, 24f);
            Assert.AreEqual(800f - 24f - 20f - 56f, hoja.TopPlegada, "Plegada, encima de la barra de gestos.");
            Assert.AreEqual(30f + 64f, hoja.TopAbierta, "Abierta, 64 dp por debajo de la barra de estado.");
            Assert.AreEqual(44f, hoja.style.bottom.value.value, 0.01f);
            hoja.Abrir();
            Assert.AreEqual(24f, hoja.style.paddingBottom.value.value, 0.01f, "Abierta, el contenido termina encima de la barra de gestos.");
        }

        [Test]
        public void AtrasPliegaLaHoja()
        {
            hoja.Abrir();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(hoja.Abierta);
            Assert.IsFalse(hoja.TecladoPedido);
            Assert.IsFalse(pantalla.Atras(), "Plegada, atrás no hace nada más.");

            hoja.Poner(0.5f);
            Assert.IsTrue(pantalla.Atras(), "A media altura también se pliega.");
            Assert.AreEqual(0f, hoja.Fraccion);
        }

        [Test]
        public void TocarLaCapsulaConLaFichaAbiertaCierraLaFichaYAbreLaHoja()
        {
            pantalla.AbrirFicha(Lugares().Lugares.First());
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsFalse(hoja.Visible, "La cápsula se oculta mientras la ficha está abierta.");
            Assert.AreEqual(DisplayStyle.None, hoja.style.display.value);

            TocarCapsula();
            Assert.IsFalse(pantalla.FichaAbierta, "Abrir la hoja cierra la ficha.");
            Assert.IsTrue(hoja.AbiertaDelTodo);
            Assert.IsTrue(hoja.Visible, "Al cerrar la ficha vuelve la cápsula.");
        }

        [Test]
        public void AbrirLaFichaPliegaLaHojaYCerrarlaDevuelveLaCapsula()
        {
            hoja.Poner(0.5f);
            pantalla.AbrirFicha(Lugares().Lugares.First());
            Assert.AreEqual(0f, hoja.Fraccion, "Nunca abiertas a la vez.");
            Assert.IsFalse(hoja.Visible);
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(hoja.Visible);
        }

        [Test]
        public void DesplegarLaHojaCierraElMenuPrecios()
        {
            pantalla.AlternarMenu();
            Assert.IsTrue(pantalla.MenuAbierto);
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -40f), 30.0);
            Assert.IsFalse(pantalla.MenuAbierto, "Al empezar a desplegarla se cierra el menú.");
            hoja.Soltar(200.0);
        }

        [Test]
        public void AbrirElMenuPreciosPliegaLaHoja()
        {
            hoja.Poner(0.5f);
            pantalla.AlternarMenu();
            Assert.IsTrue(pantalla.MenuAbierto);
            Assert.AreEqual(0f, hoja.Fraccion, "A media altura la hoja taparía el menú.");
        }

        [Test]
        public void OcultarLaCapsulaAMitadDeGestoNoLaBloquea()
        {
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -40f), 30.0);
            hoja.Mostrar(false); // p. ej. se abre la ficha con el dedo aún abajo
            Assert.IsFalse(hoja.Fisica.Pulsado);
            Assert.IsFalse(hoja.Arrastrando);
            hoja.Mostrar(true);
            TocarCapsula();
            Assert.IsTrue(hoja.AbiertaDelTodo, "La hoja sigue respondiendo.");
        }

        [Test]
        public void OcultarLaCapsulaAMitadDeArrastreLaDejaEnUnaParada()
        {
            float y0 = hoja.TopPlegada + 20f, recorrido = hoja.Fisica.AltoUtil;
            hoja.Bajar(new Vector2(150f, y0), 0.0);
            for (int i = 1; i <= 10; i++) hoja.Mover(new Vector2(150f, y0 - recorrido * 0.04f * i), 500.0 * i);
            Assert.AreEqual(0.4f, hoja.Fraccion, 0.001f, "Sin soltar, sigue al dedo.");
            hoja.Mostrar(false);
            Assert.AreEqual(0.5f, hoja.Fraccion, 0.0001f, "No se queda a medias: va a la mitad.");
            Assert.AreEqual(hoja.TopPlegada - 0.5f * recorrido, hoja.Top, 0.01f);
            Assert.AreEqual(hoja.Top, hoja.style.top.value.value, 0.01f, "Y se dibuja ahí.");
        }

        [Test]
        public void ConLaHojaAbiertaDelTodoElMapaNoRecibeToques()
        {
            Assert.IsFalse(pantalla.TapaPunto(new Vector2(10f, 10f)), "Plegada (y sin panel), el mapa sigue libre.");
            hoja.Abrir();
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)));
        }

        [Test]
        public void LaCapsulaEsDeCristalEnClaroYOpacaEnOscuro()
        {
            var tokens = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var claro = System.Text.RegularExpressions.Regex.Match(tokens, @"\.pm-raiz\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            var oscuro = System.Text.RegularExpressions.Regex.Match(tokens, @"\.pm-raiz\.tema-oscuro\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("--color-capsula: rgba(255, 255, 255, 0.90)", claro, "Cristal, como --color-glass.");
            StringAssert.Contains("--color-capsula: #1A2238", oscuro, "Opaca (surface) en oscuro.");
        }

        // ───────────────────────── TalkBack y quitar animaciones (parte 2) ─────────────────────────

        AccesibilidadPantalla Acc => pantalla.Accesibilidad;

        /// <summary>Recorrido de la hoja desplegada: campo, [Expandir], [Contraer], 8 chips y "Encuentra cerca".</summary>
        static List<string> RecorridoDeLaHoja(bool expandir, bool contraer)
        {
            var r = new List<string> { T(ClavesTexto.BuscadorCampo) };
            if (expandir) r.Add(T(ClavesTexto.BuscadorExpandir));
            if (contraer) r.Add(T(ClavesTexto.BuscadorContraer));
            r.AddRange(HojaBuscador.Categorias.Select(c => T(HojaBuscador.ClaveCategoria(c))));
            r.Add(T(ClavesTexto.BuscadorCerca));
            return r;
        }

        /// <summary>Despliega la hoja a media altura como lo haría el dedo (la pantalla la sigue en su fotograma).</summary>
        void DesplegarAMedias()
        {
            hoja.Poner(0.5f);
            pantalla.ActualizarCapas(0f);
        }

        [Test]
        public void ConTalkBackLaCapsulaEsUnBotonDetrasDeLosControlesDelMapa()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var recorrido = Acc.Recorrido();
            int capsula = recorrido.IndexOf(T(ClavesTexto.BuscadorCampo));
            Assert.AreEqual(recorrido.IndexOf(T(ClavesTexto.MapaAlejar)) + 1, capsula, "Justo después del último control del mapa.");
            Assert.AreEqual(1, recorrido.Count(e => e == T(ClavesTexto.BuscadorCampo)), "Plegada, solo la cápsula (no el campo).");
            Assert.AreEqual(Acc.Recorrido().Count - Acc.RecorridoDeMarcas().Count - 1, capsula, "Los marcadores van después.");
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorContraer));
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorExpandir));
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorCerca));
            Assert.IsFalse(Acc.Modal);
        }

        [Test]
        public void ConTalkBackActivarLaCapsulaLaAbreDelTodoModalConElFocoEnElCampo()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            pantalla.AbrirBuscadorConLector();
            Assert.IsTrue(hoja.AbiertaDelTodo);
            Assert.IsTrue(Acc.Modal);
            Assert.AreSame(hoja.Campo, Acc.Enfocado, "El foco va al campo.");
            CollectionAssert.AreEqual(RecorridoDeLaHoja(expandir: false, contraer: true), Acc.Recorrido(),
                "Campo, Contraer, chips y Encuentra cerca; sin marcadores ni controles del mapa.");
            Assert.IsEmpty(Acc.RecorridoDeMarcas());
        }

        [Test]
        public void ConTalkBackLaJerarquiaRealEsLaDeLaHoja()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Acc.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            pantalla.AbrirBuscadorConLector();
            var nodos = AssistiveSupport.activeHierarchy.rootNodes.ToList();
            CollectionAssert.AreEqual(RecorridoDeLaHoja(expandir: false, contraer: true), nodos.Select(n => n.label).ToList());
            Assert.AreEqual(AccessibilityRole.Button, nodos[0].role, "El campo.");
            Assert.AreEqual(AccessibilityRole.Button, nodos[1].role, "Contraer.");
            Assert.IsTrue(nodos.Skip(2).All(n => n.role == AccessibilityRole.Button), "Chips y Encuentra cerca, botones (2.4, parte 3).");
        }

        [Test]
        public void ConTalkBackAMediaAlturaTambienEsModalYTieneExpandir()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            DesplegarAMedias();
            Assert.IsTrue(Acc.Modal, "Desplegada a cualquier altura, modal.");
            CollectionAssert.AreEqual(RecorridoDeLaHoja(expandir: true, contraer: true), Acc.Recorrido());
        }

        [Test]
        public void ExpandirAbreLaHojaDelTodoYSaleDelRecorrido()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            DesplegarAMedias();
            pantalla.ExpandirBuscador();
            Assert.IsTrue(hoja.AbiertaDelTodo);
            Assert.IsTrue(Acc.Modal);
            CollectionAssert.AreEqual(RecorridoDeLaHoja(expandir: false, contraer: true), Acc.Recorrido());
            Assert.AreSame(hoja.Contraer, Acc.Enfocado, "El foco pasa a Contraer.");
        }

        [Test]
        public void ContraerPliegaQuitaElModoModalYDevuelveElFocoALaCapsula()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            pantalla.AbrirBuscadorConLector();
            pantalla.PlegarBuscador(); // la acción de "Contraer"
            Assert.AreEqual(0f, hoja.Fraccion);
            Assert.IsFalse(Acc.Modal);
            Assert.AreSame(hoja, Acc.Enfocado, "El foco vuelve a la cápsula.");
            var recorrido = Acc.Recorrido();
            CollectionAssert.Contains(recorrido, T(ClavesTexto.TemaCambiar), "Vuelven los controles del mapa.");
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorContraer));
        }

        [Test]
        public void ConTalkBackAtrasHaceLoMismoQueContraer()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            pantalla.AbrirBuscadorConLector();
            Assert.IsTrue(pantalla.Atras());
            Assert.AreEqual(0f, hoja.Fraccion);
            Assert.IsFalse(Acc.Modal);
            Assert.AreSame(hoja, Acc.Enfocado);
        }

        [Test]
        public void SinLectorNiExpandirNiContraer()
        {
            AccesibilidadPantalla.LectorForzado = () => false;
            DesplegarAMedias();
            Assert.IsTrue(Acc.Modal);
            CollectionAssert.AreEqual(RecorridoDeLaHoja(expandir: false, contraer: false), Acc.Recorrido());
        }

        [Test]
        public void PlegarlaConElDedoQuitaElModoModal()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            DesplegarAMedias();
            Assert.IsTrue(Acc.Modal);
            // Arrastre rápido hacia abajo: se pliega
            var p = new Vector2(150f, hoja.Top + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, 40f), 20.0);
            hoja.Mover(p + new Vector2(0f, 400f), 40.0);
            hoja.Soltar(45.0);
            Assert.AreEqual(0f, hoja.Fraccion, "Se ha plegado.");
            pantalla.ActualizarCapas(0f);
            Assert.IsFalse(Acc.Modal);
            Assert.AreSame(hoja, Acc.Enfocado, "El foco vuelve a la cápsula.");
        }

        [Test]
        public void AbrirLaFichaConLaHojaDesplegadaDejaElModoModalDeLaFicha()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            DesplegarAMedias();
            pantalla.AbrirFicha(Lugares().Lugares.First());
            pantalla.ActualizarCapas(0f);
            Assert.IsTrue(Acc.Modal, "La hoja plegada no quita el modo modal de la ficha.");
            Assert.AreSame(pantalla.Ficha.Titulo, Acc.Enfocado);
            CollectionAssert.DoesNotContain(Acc.Recorrido(), T(ClavesTexto.BuscadorContraer));
        }

        [Test]
        public void AbrirElMenuConLaHojaDesplegadaDejaElRecorridoDelMenu()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            DesplegarAMedias();
            pantalla.AlternarMenu();
            pantalla.ActualizarCapas(0f);
            Assert.IsTrue(pantalla.MenuAbierto);
            Assert.IsTrue(Acc.Modal);
            var recorrido = Acc.Recorrido();
            Assert.AreEqual(T(ClavesTexto.PreciosControl), recorrido[0], "La píldora y sus opciones.");
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorCampo));
        }

        /// <summary>Etiquetas de la jerarquía que lee TalkBack de verdad (no el recorrido calculado).</summary>
        static List<string> JerarquiaReal() => AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList();

        [Test]
        public void TrasExpandirLaJerarquiaRealYaNoTieneExpandir()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Acc.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            DesplegarAMedias();
            CollectionAssert.AreEqual(RecorridoDeLaHoja(expandir: true, contraer: true), JerarquiaReal());
            pantalla.ExpandirBuscador();
            CollectionAssert.AreEqual(RecorridoDeLaHoja(expandir: false, contraer: true), JerarquiaReal());
        }

        [Test]
        public void ConElMenuSobreLaHojaLaJerarquiaRealEsLaDelMenu()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Acc.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            DesplegarAMedias();
            pantalla.AlternarMenu();
            pantalla.ActualizarCapas(0f);
            var nodos = JerarquiaReal();
            Assert.AreEqual(T(ClavesTexto.PreciosControl), nodos[0]);
            CollectionAssert.DoesNotContain(nodos, T(ClavesTexto.BuscadorCampo));
        }

        [Test]
        public void DesplegadaConElDedoElFocoDeTalkBackVaAlCampo()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            DesplegarAMedias();
            Assert.AreSame(hoja.Campo, Acc.Enfocado, "No se queda en un nodo del mapa que ya no está.");
        }

        [Test]
        public void ConLaHojaDesplegadaLaPildoraNoEstaEnElRecorrido()
        {
            DesplegarAMedias();
            CollectionAssert.DoesNotContain(Acc.Recorrido(), T(ClavesTexto.PreciosControl));
        }

        [Test]
        public void ConAnimacionesNormalesLaHojaTieneTransicion()
        {
            Assert.IsFalse(hoja.SinTransicion);
            Assert.IsFalse(hoja.ClassListContains(HojaBuscador.ClaseSinTransicion));
            var uss = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            var buscador = System.Text.RegularExpressions.Regex.Match(uss, @"\n\.buscador\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("transition-duration: 0.32s", buscador);
            Assert.AreEqual(0.32f, HojaBuscador.Transicion);
        }

        [Test]
        public void ConLasAnimacionesQuitadasLaHojaNoTieneTransicion()
        {
            sistema.AnimacionesQuitadas = true;
            pantalla.LeerAccesibilidadSistema(); // al volver a primer plano
            Assert.IsTrue(hoja.ClassListContains(HojaBuscador.ClaseSinTransicion));
            var uss = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            var regla = System.Text.RegularExpressions.Regex.Match(uss, @"\.buscador--sin-transicion\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("transition-duration: 0s", regla);

            sistema.AnimacionesQuitadas = false; // se vuelven a poner en Ajustes
            pantalla.LeerAccesibilidadSistema();
            Assert.IsFalse(hoja.SinTransicion, "Al volver a primer plano se aplica la nueva escala.");
        }

        [Test]
        public void SinAccesibilidadDelSistemaLasAnimacionesSonNormales()
        {
            Assert.IsFalse(new PriceMap.Adaptadores.Sistema.AccesibilidadSistemaAndroid().AnimacionesQuitadas,
                "Fuera de Android, false.");
        }
    }
}
