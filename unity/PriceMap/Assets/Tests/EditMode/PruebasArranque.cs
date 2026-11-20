using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using PriceMap.EditorTools;
using UnityEditor;

namespace PriceMap.Tests.EditMode
{
    /// <summary>La raíz de composición entrega el mapa a la UI, o avisa sin romper si falta una pieza.</summary>
    public class PruebasArranque
    {
        class MapaFalso : IMapa
        {
            public readonly List<Coordenada> centrados = new List<Coordenada>();
            public readonly List<Tema> temas = new List<Tema>();
            public void Centrar(Coordenada destino) => centrados.Add(destino);
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) => temas.Add(tema);
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres { get; } = new PuntoInteres[0];
            public event System.Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event System.Action<PuntoPantalla> Toque { add { } remove { } }
            public event System.Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos) { }
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

        class SistemaOscuro : ITemaSistema { public bool EsOscuro => true; }

        static ServicioTema NuevoTema() => new ServicioTema(new PreferenciasFalsas(), new SistemaOscuro());
        static ServicioFiltro Filtro() => new ServicioFiltro(new PreferenciasFalsas());

        class SinLugares : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Fallo("sin datos", new Lugar[0]);
        }

        static ServicioLugares Lugares() { var l = new ServicioLugares(new SinLugares()); l.Cargar(); return l; }
        static readonly System.DateTime Ahora = new System.DateTime(2026, 11, 17, 12, 0, 0, System.DateTimeKind.Utc);

        GameObject objeto;
        PantallaMapa pantalla;

        [SetUp]
        public void Preparar()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(objeto);

        [Test]
        public void ConTodasLasPiezasLaPantallaRecibeElMapaYCentraEnLaPlazaDelCabildo()
        {
            var mapa = new MapaFalso();
            Assert.IsTrue(Arranque.Arranque.Componer(mapa, pantalla, NuevoTema(), Lugares(), Filtro(), Ahora));
            CollectionAssert.AreEqual(new[] { new Coordenada(36.7779, -6.3528) }, mapa.centrados);
        }

        class AccesibilidadFalsa : IAccesibilidadSistema
        {
            public bool AnimacionesQuitadas => true;
        }

        [Test]
        public void LaPantallaRecibeLosAjustesDeAccesibilidadDelSistema()
        {
            var sistema = new AccesibilidadFalsa();
            Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), pantalla, NuevoTema(), Lugares(), Filtro(), Ahora,
                accesibilidadSistema: sistema));
            Assert.AreSame(sistema, pantalla.AccesibilidadSistema, "Componer se los pasa a la pantalla (quitar animaciones).");
        }

        [Test]
        public void ElMapaRecibeElTemaInicialYCadaCambio()
        {
            var mapa = new MapaFalso();
            var tema = NuevoTema();
            Arranque.Arranque.Componer(mapa, pantalla, tema, Lugares(), Filtro(), Ahora);
            tema.Alternar();
            CollectionAssert.AreEqual(new[] { Tema.Oscuro, Tema.Claro }, mapa.temas);
        }

        [Test]
        public void SinMapaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el mapa"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(null, pantalla, NuevoTema(), Lugares(), Filtro(), Ahora));
            Assert.IsFalse(ok);
        }

        [Test]
        public void SinServicioDeTemaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el ServicioTema"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), pantalla, null, Lugares(), Filtro(), Ahora));
            Assert.IsFalse(ok);
        }

        [Test]
        public void SinPantallaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta la PantallaMapa"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), null, NuevoTema(), Lugares(), Filtro(), Ahora));
            Assert.IsFalse(ok);
        }

        [Test]
        public void SinServicioDeFiltroAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el ServicioFiltro"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), pantalla, NuevoTema(), Lugares(), null, Ahora));
            Assert.IsFalse(ok);
        }

        [Test]
        public void ComponerIniciaElFiltroGuardado()
        {
            var prefs = new PreferenciasFalsas();
            prefs.Guardar(ServicioFiltro.ClavePreferencia, "ocio");
            var filtro = new ServicioFiltro(prefs);
            Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), pantalla, NuevoTema(), Lugares(), filtro, Ahora));
            Assert.AreEqual(FiltroPrecios.Ocio, filtro.Actual);
        }

        /// <summary>
        /// Componer entrega el filtro a una pantalla con interfaz de verdad (UIDocument con PantallaMapa.uxml):
        /// con "ocio" guardado, la píldora dice "Ocio" y la capa filtra por ocio.
        /// </summary>
        [Test]
        public void ComponerEntregaElFiltroGuardadoALaInterfaz()
        {
            var conInterfaz = new GameObject("Pantalla con interfaz");
            try
            {
                conInterfaz.SetActive(false);
                var documento = conInterfaz.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                var conUi = conInterfaz.AddComponent<PantallaMapa>();
                conInterfaz.SetActive(true); // UIDocument (ExecuteAlways) crea su árbol también en el editor
                Assert.IsNotNull(documento.rootVisualElement?.Q("raiz"), "El UIDocument no ha creado el árbol de la interfaz.");

                var prefs = new PreferenciasFalsas();
                prefs.Guardar(ServicioFiltro.ClavePreferencia, "ocio");
                var filtro = new ServicioFiltro(prefs);
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), conUi, NuevoTema(), Lugares(), filtro, Ahora));

                Assert.IsNotNull(conUi.Pildora);
                Assert.AreEqual(FiltroPrecios.Ocio, conUi.Capa.Filtro);
                Assert.AreEqual(Textos.T(ClavesTexto.PreciosOpcionOcio), conUi.Pildora.Texto.text);
                Assert.AreEqual("Ocio", PrepararInterfaz.TextosEs[ClavesTexto.PreciosOpcionOcio]);
            }
            finally { Object.DestroyImmediate(conInterfaz); }
        }

        /// <summary>Resolutor falso: apunta cada enlace y lo deja pendiente.</summary>
        class ResolutorFalso : IResolutorEnlaces
        {
            public readonly List<string> Urls = new List<string>();
            public System.Threading.Tasks.Task<Resultado<string>> Resolver(string url, System.Threading.CancellationToken ct)
            {
                Urls.Add(url);
                return new System.Threading.Tasks.TaskCompletionSource<Resultado<string>>().Task;
            }
        }

        /// <summary>
        /// Historia 2.5, parte 2: Componer pasa el resolutor de enlaces cortos hasta la pantalla con interfaz de verdad
        /// (como Arranque.Start): un enlace corto pegado en el buscador le llega.
        /// </summary>
        [Test]
        public void ComponerEntregaElResolutorDeEnlacesAlBuscador()
        {
            var conInterfaz = new GameObject("Pantalla con interfaz");
            Textos.Forzados = c => PrepararInterfaz.TextosEs[c];
            try
            {
                conInterfaz.SetActive(false);
                var documento = conInterfaz.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                var conUi = conInterfaz.AddComponent<PantallaMapa>();
                conInterfaz.SetActive(true);
                Assert.IsNotNull(documento.rootVisualElement?.Q("raiz"), "El UIDocument no ha creado el árbol de la interfaz.");

                var resolutor = new ResolutorFalso();
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), conUi, NuevoTema(), Lugares(), Filtro(), Ahora,
                    resolutorEnlaces: resolutor));
                conUi.Buscador.Medir(800f);
                conUi.Buscador.Abrir();
                conUi.Buscador.Escribir("https://maps.app.goo.gl/abc");

                CollectionAssert.AreEqual(new[] { "https://maps.app.goo.gl/abc" }, resolutor.Urls);
                Assert.AreEqual(PrepararInterfaz.TextosEs[ClavesTexto.BuscadorComprobandoEnlace], conUi.Buscador.SinResultados.text);
            }
            finally
            {
                Textos.Forzados = null;
                Object.DestroyImmediate(conInterfaz);
            }
        }

        [Test]
        public void SinServicioDeLugaresAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el ServicioLugares"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), pantalla, NuevoTema(), null, Filtro(), Ahora));
            Assert.IsFalse(ok);
        }
    }
}
