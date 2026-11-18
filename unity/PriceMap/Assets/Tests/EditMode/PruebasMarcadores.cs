using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Marcadores de los lugares encima del mapa (historia 1.4, parte 2): una prueba por fila de la
    /// matriz de la especificación, con un mapa falso que proyecta a puntos conocidos.
    /// </summary>
    public class PruebasMarcadores
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        const float Escala = 0.5f;            // px → dp
        const float AnchoPx = 1080f, AltoPx = 1920f;

        /// <summary>Proyecta cada coordenada al punto que se le diga; las demás, detrás de la cámara.</summary>
        class MapaFalso : IMapa
        {
            public readonly Dictionary<Coordenada, PuntoPantalla> puntos = new Dictionary<Coordenada, PuntoPantalla>();
            public void Centrar(Coordenada destino) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => puntos.TryGetValue(punto, out var p) ? p : PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres { get; } = new PuntoInteres[0];
            public event System.Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
        }

        /// <summary>Textos es-ES del generador (en EditMode Localization no carga la locale).</summary>
        static string TextosEs(string clave) => PrepararInterfaz.TextosEs[clave];

        static IReadOnlyList<Lugar> reales;
        static IReadOnlyList<Lugar> Reales()
        {
            if (reales != null) return reales;
            var r = new RepositorioLugaresJson().Cargar();
            Assert.IsTrue(r.Ok, r.Error);
            return reales = r.Valor;
        }

        static Lugar Real(string id) => Reales().Single(l => l.Id == id);

        static Lugar Sintetico(string id, string tipo, Plantilla plantilla = Plantilla.ServicioLocal, bool publico = false, double lat = 36.78) =>
            new Lugar(id, "Lugar " + id, tipo, plantilla, new Coordenada(lat, -6.35), publico: publico);

        static (CapaMarcadores capa, MapaFalso mapa) Capa(params Lugar[] lugares)
        {
            var mapa = new MapaFalso();
            var capa = new CapaMarcadores(new VisualElement(), mapa, lugares.Select(l => (l, ReglaResumen.Resumir(l, Ahora))), TextosEs);
            return (capa, mapa);
        }

        static MarcadorLugar Marcador(Lugar l) => Capa(l).capa.Marcadores[0];

        // ── Matriz ──

        [Test]
        public void ConPrecioClinicaAreasEsAzulConCruzEInsigniaEuro()
        {
            var m = Marcador(Real("clinica-areas"));
            Assert.IsTrue(m.TienePrecio);
            Assert.IsTrue(m.ClassListContains("marcador--servicio"));
            Assert.IsFalse(m.ClassListContains("marcador--sin-precio"), "Con precio: 42 dp, sin atenuar.");
            Assert.AreEqual("cross", m.Glifo);
            Assert.IsNotNull(m.Insignia, "Con precio lleva la insignia €.");
            Assert.AreEqual(m.Cuerpo, m.Insignia.parent);
        }

        [Test]
        public void SinPrecioBarCachonEsNaranjaConMartiniAtenuadoYSinEuro()
        {
            var m = Marcador(Real("bar-cachon"));
            Assert.IsFalse(m.TienePrecio);
            Assert.IsTrue(m.ClassListContains("marcador--hosteleria"));
            Assert.IsTrue(m.ClassListContains("marcador--sin-precio"), "Sin precio: 32 dp y opacidad 0,7 (PantallaMapa.uss).");
            Assert.AreEqual("martini", m.Glifo);
            Assert.IsNull(m.Insignia);
        }

        [Test]
        public void UnLugarPublicoNoLlevaEuroYSeVeAtenuado()
        {
            var m = Marcador(Sintetico("centro-salud", "Clínica médica", publico: true));
            Assert.IsFalse(m.TienePrecio);
            Assert.IsNull(m.Insignia);
            Assert.IsTrue(m.ClassListContains("marcador--sin-precio"));
        }

        [Test]
        public void UnTipoDesconocidoUsaElPinYAvisaUnaVezPorTipo()
        {
            string tipo = "Tipo inventado " + Guid.NewGuid().ToString("N");
            LogAssert.Expect(LogType.Warning, new Regex("^PriceMap: .*" + Regex.Escape(tipo)));
            var (capa, _) = Capa(Sintetico("a", tipo), Sintetico("b", tipo));
            Assert.AreEqual(IconosLugar.Generico, capa.Marcadores[0].Glifo);
            Assert.AreEqual(IconosLugar.Generico, capa.Marcadores[1].Glifo);
            LogAssert.NoUnexpectedReceived(); // el segundo marcador del mismo tipo no vuelve a avisar
        }

        [Test]
        public void LaPuntaVaAlPuntoProyectadoEnDp()
        {
            var l = Sintetico("p", "Bar", Plantilla.Hosteleria);
            var (capa, mapa) = Capa(l);
            mapa.puntos[l.Posicion] = new PuntoPantalla(400.4f, 900.6f, 300f);
            capa.Actualizar(Escala, AnchoPx, AltoPx);
            var m = capa.Marcadores[0];
            Assert.IsTrue(m.Visible);
            Assert.AreEqual(DisplayStyle.Flex, m.style.display.value);
            Assert.AreEqual(200.2f, m.Posicion.x, 1e-4f);
            Assert.AreEqual(450.3f, m.Posicion.y, 1e-4f);
            var t = m.style.translate.value;
            Assert.AreEqual(200.2f, t.x.value, 1e-4f, "Se mueve con translate (la punta cuelga del punto 0,0 del marcador).");
            Assert.AreEqual(450.3f, t.y.value, 1e-4f);
            Assert.AreEqual(UsageHints.DynamicTransform, m.usageHints & UsageHints.DynamicTransform);

            // Al mover el mapa, sigue a su coordenada
            mapa.puntos[l.Posicion] = new PuntoPantalla(10f, 20f, 300f);
            capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.AreEqual(new Vector2(5f, 10f), m.Posicion);
        }

        [Test]
        public void FueraDeVistaSeOcultaYNoTieneNodoDeTalkBack()
        {
            var detras = Sintetico("detras", "Bar", lat: 36.70);
            var fuera = Sintetico("fuera", "Bar", lat: 36.71);
            var enMargen = Sintetico("margen", "Bar", lat: 36.72);
            var dentro = Sintetico("dentro", "Bar", lat: 36.73);
            var arriba = Sintetico("arriba", "Bar", lat: 36.74);
            var abajoFuera = Sintetico("abajo-fuera", "Bar", lat: 36.75);
            var abajoMargen = Sintetico("abajo-margen", "Bar", lat: 36.76);
            var (capa, mapa) = Capa(detras, fuera, enMargen, dentro, arriba, abajoFuera, abajoMargen);
            mapa.puntos[detras.Posicion] = new PuntoPantalla(500f, 500f, -10f);                 // detrás de la cámara
            mapa.puntos[fuera.Posicion] = new PuntoPantalla(AnchoPx + 200f, 500f, 100f);        // 100 dp fuera
            mapa.puntos[enMargen.Posicion] = new PuntoPantalla(-60f, 500f, 100f);               // 30 dp fuera: aún asoma
            mapa.puntos[dentro.Posicion] = new PuntoPantalla(500f, 500f, 100f);
            mapa.puntos[arriba.Posicion] = new PuntoPantalla(500f, -2f, 100f);                  // 1 dp por encima: cuelga fuera
            mapa.puntos[abajoFuera.Posicion] = new PuntoPantalla(500f, AltoPx + 200f, 100f);    // 100 dp por debajo
            mapa.puntos[abajoMargen.Posicion] = new PuntoPantalla(500f, AltoPx + 60f, 100f);    // 30 dp por debajo: asoma

            Assert.IsTrue(capa.Actualizar(Escala, AnchoPx, AltoPx), "Ha cambiado el conjunto de visibles.");
            var m = capa.Marcadores;
            Assert.IsFalse(m[0].Visible); Assert.AreEqual(DisplayStyle.None, m[0].style.display.value);
            Assert.IsFalse(m[1].Visible); Assert.AreEqual(DisplayStyle.None, m[1].style.display.value);
            Assert.IsTrue(m[2].Visible);
            Assert.IsTrue(m[3].Visible);
            Assert.IsFalse(m[4].Visible, "Con la punta por encima del borde superior, el marcador entero queda fuera.");
            Assert.IsFalse(m[5].Visible);
            Assert.IsTrue(m[6].Visible, "Por debajo, dentro del margen, el marcador aún asoma.");
            Assert.IsFalse(capa.Actualizar(Escala, AnchoPx, AltoPx), "Sin cambios, no hay que tocar TalkBack.");

            var acc = new AccesibilidadPantalla(new VisualElement());
            foreach (var x in m) { var mm = x; acc.Marcador(mm.Cuerpo, () => mm.Lugar.Id, () => mm.Visible, () => mm.Posicion); }
            CollectionAssert.AreEquivalent(new[] { "margen", "dentro", "abajo-margen" }, acc.Recorrido());
        }

        [Test]
        public void ElTemaNoCambiaLosColoresDelMarcador()
        {
            var tokens = File.ReadAllText(Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var claro = Regex.Match(tokens, @"\.pm-raiz\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            var oscuro = Regex.Match(tokens, @"\.pm-raiz\.tema-oscuro\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            Assert.AreEqual("#2F5BEA", Token(claro, "--color-pin-servicio"));
            Assert.AreEqual("#D6740F", Token(claro, "--color-pin-hosteleria"));
            Assert.AreEqual("#1F9D6B", Token(claro, "--color-pin-ocio"));
            Assert.AreEqual("#FFFFFF", Token(claro, "--color-pin-borde"));
            StringAssert.DoesNotContain("--color-pin-", oscuro, "Los colores del marcador no se redefinen en el tema oscuro.");

            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            StringAssert.DoesNotContain("--color-tpl-", uss, "El marcador usa los tokens fijos --color-pin-*, no los de plantilla que cambian con el tema.");
        }

        /// <summary>El logo blanco es un icono: necesita ≥ 3:1 sobre el color de cada plantilla (NFR-4).</summary>
        [TestCase("--color-pin-servicio")]
        [TestCase("--color-pin-hosteleria")]
        [TestCase("--color-pin-ocio")]
        public void ElLogoBlancoContrastaConElColorDelMarcador(string token)
        {
            var tokens = File.ReadAllText(Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var claro = Regex.Match(tokens, @"\.pm-raiz\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            string hex = Token(claro, token);
            Assert.IsNotEmpty(hex, $"Falta {token} en Tokens.uss.");
            float contraste = PruebasContraste.Contraste(PruebasContraste.Leer("#FFFFFF"), PruebasContraste.Leer(hex));
            Assert.GreaterOrEqual(contraste, 3f, $"Blanco sobre {token} ({hex}): {contraste:F3}:1");
        }

        static string Token(string bloque, string nombre) =>
            Regex.Match(bloque, Regex.Escape(nombre) + @":\s*(?<v>#[0-9A-Fa-f]{6})").Groups["v"].Value.ToUpperInvariant();

        [Test]
        public void ArrastrarSobreUnMarcadorMueveElMapa()
        {
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var raiz = arbol.Q("raiz");
            var contenedor = raiz.Q("capa-marcadores");
            // Debajo de los botones: solo la capa de puntos de interés (1.6) va antes, por debajo de los marcadores
            Assert.AreEqual(0, raiz.IndexOf(raiz.Q("capa-puntos")), "Los puntos de interés son el primer hijo de la raíz.");
            Assert.AreEqual(1, raiz.IndexOf(contenedor), "La capa de marcadores va justo encima de los puntos y debajo de los botones.");
            var capa = new CapaMarcadores(contenedor, new MapaFalso(), new[] { (Real("clinica-areas"), ReglaResumen.Resumir(Real("clinica-areas"), Ahora)) }, TextosEs);
            Assert.AreEqual(PickingMode.Ignore, contenedor.pickingMode);
            Assert.IsFalse(PantallaMapa.EsControl(raiz, contenedor));
            contenedor.Query<VisualElement>().ForEach(e =>
            {
                Assert.AreEqual(PickingMode.Ignore, e.pickingMode, $"{e.name} [{string.Join(" ", e.GetClasses())}] captura toques.");
                Assert.IsFalse(PantallaMapa.EsControl(raiz, e), "TapaPunto debe dar false sobre un marcador.");
            });
            Assert.AreEqual(1, capa.Marcadores.Count);
        }

        [Test]
        public void SinLugaresLaCapaQuedaVaciaYLaPantallaFunciona()
        {
            var (capa, _) = Capa();
            Assert.AreEqual(0, capa.Marcadores.Count);
            Assert.IsFalse(capa.Actualizar(Escala, AnchoPx, AltoPx));

            var objeto = new GameObject("Pantalla de prueba");
            try
            {
                var pantalla = objeto.AddComponent<PantallaMapa>();
                var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
                var lugares = new ServicioLugares(new RepositorioQueFalla());
                Assert.IsFalse(lugares.Cargar().Ok);
                Assert.DoesNotThrow(() => pantalla.Inicializar(new MapaFalso(), new ServicioTema(new PreferenciasFalsas(), new SistemaClaro()), lugares, Ahora, arbol));
                Assert.IsNotNull(pantalla.Capa);
                Assert.AreEqual(0, pantalla.Capa.Marcadores.Count);
                Assert.AreEqual(0, arbol.Q("capa-marcadores").childCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(objeto); }
        }

        [Test]
        public void LaPantallaCreaUnMarcadorPorLugarRealConSuResumenYSuNodoDeTalkBack()
        {
            var objeto = new GameObject("Pantalla de prueba");
            try
            {
                var pantalla = objeto.AddComponent<PantallaMapa>();
                var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
                var lugares = new ServicioLugares(new RepositorioLugaresJson());
                Assert.IsTrue(lugares.Cargar().Ok);
                var mapa = new MapaFalso();
                var areas = lugares.Buscar("clinica-areas");
                mapa.puntos[areas.Posicion] = new PuntoPantalla(500f, 900f, 300f);

                pantalla.Inicializar(mapa, new ServicioTema(new PreferenciasFalsas(), new SistemaClaro()), lugares, Ahora, arbol);
                pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);

                Assert.AreEqual(lugares.Lugares.Count, pantalla.Capa.Marcadores.Count, "Un marcador por lugar.");
                var m = pantalla.Capa.Marcadores.Single(x => x.Lugar.Id == "clinica-areas");
                Assert.IsTrue(m.TienePrecio);
                Assert.IsNotNull(m.Insignia);
                Assert.IsTrue(m.Visible);
                // En EditMode el anuncio sale de las claves (Localization no carga): basta con que esté en el recorrido
                var recorrido = pantalla.Accesibilidad.Recorrido();
                CollectionAssert.Contains(recorrido, m.Anuncio);
                Assert.AreEqual(m.Anuncio, recorrido[recorrido.Count - 1], "Es el único marcador visible: va detrás de los botones.");
            }
            finally { UnityEngine.Object.DestroyImmediate(objeto); }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class RepositorioQueFalla : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Fallo("La carga falló.", new Lugar[0]);
        }

        // ── Orden de pintado ──

        [Test]
        public void LosMasCercanosSePintanEncima()
        {
            var a = Sintetico("a", "Bar", lat: 36.70); var b = Sintetico("b", "Bar", lat: 36.71); var c = Sintetico("c", "Bar", lat: 36.72);
            var (capa, mapa) = Capa(a, b, c);
            mapa.puntos[a.Posicion] = new PuntoPantalla(100f, 100f, 50f);   // el más cercano
            mapa.puntos[b.Posicion] = new PuntoPantalla(100f, 100f, 900f);  // el más lejano
            mapa.puntos[c.Posicion] = new PuntoPantalla(100f, 100f, 300f);
            capa.Actualizar(Escala, AnchoPx, AltoPx);
            CollectionAssert.AreEqual(new[] { "b", "c", "a" }, Orden(capa), "Del más lejano al más cercano (el último se pinta encima).");

            mapa.puntos[a.Posicion] = new PuntoPantalla(100f, 100f, 2000f); // ahora "a" es el más lejano
            capa.Actualizar(Escala, AnchoPx, AltoPx);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Orden(capa));
        }

        static string[] Orden(CapaMarcadores capa) =>
            capa.Contenedor.Children().OfType<MarcadorLugar>().Where(m => m.Visible).Select(m => m.Lugar.Id).ToArray();

        // ── Logos ──

        [Test]
        public void TodosLosTiposDelJsonRealTienenUnLogoPropio()
        {
            var sinLogo = Reales().Select(l => l.Tipo).Distinct().Where(t => !IconosLugar.TieneGlifoPropio(t)).ToList();
            CollectionAssert.IsEmpty(sinLogo, "Tipos sin logo: " + string.Join(", ", sinLogo));
        }

        [Test]
        public void TodosLosGlifosSeInterpretanSinError()
        {
            foreach (var g in IconosLugar.Glifos)
            {
                IReadOnlyList<IconosLugar.Orden> ordenes = null;
                Assert.DoesNotThrow(() => ordenes = IconosLugar.Interpretar(g), g);
                Assert.IsNotEmpty(ordenes, g);
                foreach (var o in ordenes)
                {
                    Assert.IsFalse(float.IsNaN(o.P1.x) || float.IsNaN(o.P2.x) || float.IsNaN(o.P3.x) || float.IsNaN(o.P3.y), g);
                    Assert.That(o.P3.x, Is.InRange(-0.01f, 24.01f), $"{g}: se sale del viewBox");
                    Assert.That(o.P3.y, Is.InRange(-0.01f, 24.01f), $"{g}: se sale del viewBox");
                }
            }
        }

        [Test]
        public void ElInterpreteLeeNumerosCompactosRelativosYArcos()
        {
            var o = new List<IconosLugar.Orden>();
            // "1.1.9" = 1.1 y .9; "2-2" = 2 y -2; tras M, los pares que siguen son líneas
            IconosLugar.InterpretarRuta("M1.1.9 3 4 5 6l2-2h.5v-1Z", o);
            Assert.AreEqual(IconosLugar.TipoOrden.Mover, o[0].Tipo); Assert.AreEqual(new Vector2(1.1f, 0.9f), o[0].P3);
            Assert.AreEqual(IconosLugar.TipoOrden.Linea, o[1].Tipo); Assert.AreEqual(new Vector2(3f, 4f), o[1].P3);
            Assert.AreEqual(new Vector2(5f, 6f), o[2].P3);
            Assert.AreEqual(new Vector2(7f, 4f), o[3].P3);
            Assert.AreEqual(new Vector2(7.5f, 4f), o[4].P3);
            Assert.AreEqual(new Vector2(7.5f, 3f), o[5].P3);
            Assert.AreEqual(IconosLugar.TipoOrden.Cerrar, o[6].Tipo);

            // Semicírculo de radio 5 de (0,5) a (10,5): dos curvas que pasan por (5,0) o (5,10) según el barrido
            o.Clear();
            IconosLugar.InterpretarRuta("M0 5a5 5 0 0 1 10 0", o);
            Assert.AreEqual(3, o.Count);
            Assert.AreEqual(new Vector2(10f, 5f), o[2].P3);
            Assert.AreEqual(5f, o[1].P3.x, 1e-4f); Assert.AreEqual(0f, o[1].P3.y, 1e-4f);
            o.Clear();
            IconosLugar.InterpretarRuta("M0 5A5 5 0 0 0 10 5", o);
            Assert.AreEqual(10f, o[1].P3.y, 1e-4f);

            // Banderas pegadas: "a1 1 0 0 0-1 1"
            o.Clear();
            Assert.DoesNotThrow(() => IconosLugar.InterpretarRuta("M2 2a1 1 0 0 0-1 1", o));
            Assert.AreEqual(new Vector2(1f, 3f), o[o.Count - 1].P3);

            // Curva suave: refleja el último control de la curva anterior
            o.Clear();
            IconosLugar.InterpretarRuta("M0 0C1 1 2 1 3 0s2-1 3 0", o);
            Assert.AreEqual(new Vector2(4f, -1f), o[2].P1);
            Assert.AreEqual(new Vector2(6f, 0f), o[2].P3);

            // rect con rx y circle
            var r = IconosLugar.InterpretarSvg("<rect x=\"4\" y=\"3\" width=\"16\" height=\"16\" rx=\"2\"/><circle cx=\"6\" cy=\"6\" r=\"3\"/>");
            Assert.AreEqual(new Vector2(6f, 3f), r[0].P3);
            Assert.AreEqual(IconosLugar.TipoOrden.Circulo, r[r.Count - 1].Tipo);
            Assert.AreEqual(3f, r[r.Count - 1].Radio);

            Assert.Throws<FormatException>(() => IconosLugar.InterpretarRuta("M1 2Q3 4 5 6", new List<IconosLugar.Orden>()));
        }

        // ── Euros y TalkBack ──

        // El espacio antes de € es de no separación (U+00A0); los miles solo se agrupan desde 5 cifras
        [TestCase(2000, "20 €")]
        [TestCase(2250, "22,50 €")]
        [TestCase(5, "0,05 €")]
        [TestCase(123400, "1234 €")]
        [TestCase(123456, "1234,56 €")]
        [TestCase(1234500, "12.345 €")]
        public void EurosEnEspanol(int centimos, string esperado) => Assert.AreEqual(esperado, FormatoPrecio.Euros(centimos));

        [Test]
        public void AnunciosDeTalkBack()
        {
            Assert.AreEqual("Clínica Areas, clínica privada, desde 20 €", Marcador(Real("clinica-areas")).Anuncio);
            Assert.AreEqual("Bar Cachón, bar, aún no tiene precio", Marcador(Real("bar-cachon")).Anuncio);
        }

        [Test]
        public void TalkBackRecorrePrimeroLosBotonesYLuegoLosMarcadoresDeArribaAbajoYDeIzquierdaADerecha()
        {
            var acc = new AccesibilidadPantalla(new VisualElement());
            acc.Boton(new VisualElement(), () => "tema", () => { });
            acc.Boton(new VisualElement(), () => "oculto", () => { }, visible: () => false);
            void M(string nombre, float x, float y, bool visible = true) =>
                acc.Marcador(new VisualElement(), () => nombre, () => visible, () => new Vector2(x, y));
            M("abajo", 10f, 300f);
            M("arriba-derecha", 200f, 50f);
            M("arriba-izquierda", 20f, 50f);
            M("invisible", 0f, 0f, false);
            M("centro", 100f, 120f);
            acc.Boton(new VisualElement(), () => "zoom", () => { });
            CollectionAssert.AreEqual(new[] { "tema", "zoom", "arriba-izquierda", "arriba-derecha", "centro", "abajo" }, acc.Recorrido());
        }
    }
}
