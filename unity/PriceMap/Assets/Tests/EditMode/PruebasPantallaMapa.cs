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
            public event System.Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
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
            Assert.IsFalse(PantallaMapa.EsControl(raiz, arbol.Q("columna-controles")), "La columna de controles no tapa el mapa.");
        }

        /// <summary>
        /// Sombra suave (prototipo 0 4px 16px): más larga por debajo que por encima, nula lejos del control, y en el
        /// borde la de un rectángulo desplazado 4 dp y desenfocado con σ = 8.
        /// </summary>
        [Test]
        public void LaSombraSuaveCaeHaciaAbajoYSeDesvanece()
        {
            var control = new Rect(0f, 0f, 48f, 96f);
            const float radio = 16f, alfa = 0.16f;
            float Alfa(float x, float y) => SombraSuave.Alfa(new Vector2(x, y), control, radio, alfa);
            float abajo = Alfa(24f, 96f + 8f), arriba = Alfa(24f, -8f);
            Assert.Greater(abajo, arriba * 2f, "Cae hacia abajo (desplazada 4 dp).");
            Assert.AreEqual(alfa * 0.5f, Alfa(24f, 96f + SombraSuave.DesplazamientoY), 0.001f, "En el borde de la sombra, la mitad.");
            Assert.Less(Alfa(24f, 96f + 30f), 0.002f, "A 26 dp del borde de la sombra ya no se ve.");
            Assert.Less(Alfa(-30f, 48f), 0.002f);
            Assert.AreEqual(Alfa(-6f, 48f), Alfa(48f + 6f, 48f), 0.0001f, "Simétrica a izquierda y derecha.");
            Assert.AreEqual(16f, SombraSuave.Desenfoque);
            Assert.AreEqual(4f, SombraSuave.DesplazamientoY);
        }

        /// <summary>La sombra cambia con el tema: 16 % de --text en claro y 45 % de negro en oscuro (prototipo --shadow).</summary>
        [Test]
        public void ElColorDeLaSombraYElBordeDeCristalVanEnLosTokens()
        {
            var tokens = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var claro = System.Text.RegularExpressions.Regex.Match(tokens, @"\.pm-raiz\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            var oscuro = System.Text.RegularExpressions.Regex.Match(tokens, @"\.pm-raiz\.tema-oscuro\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("--color-sombra: rgba(19, 35, 91, 0.16)", claro);
            StringAssert.Contains("--color-sombra: rgba(0, 0, 0, 0.45)", oscuro);
            StringAssert.Contains("--color-glass-edge: rgba(255, 255, 255, 0.9)", claro);
            StringAssert.Contains("--color-glass-edge: rgba(76, 125, 255, 0.28)", oscuro);
            // Fila "Cristal": sin desenfoque en Unity, el cristal sube a 0,90 en los dos temas
            StringAssert.Contains("--color-glass: rgba(255, 255, 255, 0.90)", claro);
            StringAssert.Contains("--color-glass: rgba(26, 34, 56, 0.90)", oscuro);
        }

        /// <summary>
        /// Fila "Bajo un control": la píldora y la columna de tema y ubicación cuentan como zonas ocupadas (sin
        /// panel no tienen medida y no cuentan; con el menú cerrado, el menú no cuenta).
        /// </summary>
        [Test]
        public void SinMedidaLosControlesNoOcupanNada()
        {
            var objeto = new GameObject("Pantalla de prueba");
            try
            {
                var pantalla = objeto.AddComponent<PantallaMapa>();
                var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
                var prefs = new PreferenciasFalsas();
                pantalla.Inicializar(new MapaFalso(), new ServicioTema(prefs, new SistemaClaro()), null, System.DateTime.UtcNow, arbol, new ServicioFiltro(prefs));
                Assert.IsEmpty(pantalla.ZonasControles(), "Sin disposición (EditMode, sin panel) no hay rectángulos que contar.");
            }
            finally { UnityEngine.Object.DestroyImmediate(objeto); }
        }

        /// <summary>Captura de PC: el panel mide 412 dp de ancho, sea cual sea la ventana (escala con el ancho).</summary>
        [TestCase(412, 915)]
        [TestCase(1440, 2560)]
        [TestCase(900, 1600)]
        public void LaCapturaDePcTieneElAnchoDeUnMovil(int anchoPx, int altoPx)
        {
            var ajustes = ScriptableObject.CreateInstance<PanelSettings>();
            try
            {
                PriceMap.Arranque.CapturaPruebas.AnchoDeMovil(ajustes, anchoPx, altoPx);
                Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, ajustes.scaleMode);
                Assert.AreEqual(PanelScreenMatchMode.MatchWidthOrHeight, ajustes.screenMatchMode);
                Assert.AreEqual(0f, ajustes.match, "Solo cuenta el ancho.");
                Assert.AreEqual(412, ajustes.referenceResolution.x);
                Assert.AreEqual(412f, PriceMap.Arranque.CapturaPruebas.AnchoMovilDp);
            }
            finally { Object.DestroyImmediate(ajustes); }
        }

        /// <summary>
        /// La píldora tiene extremos semicirculares (radio = mitad del alto, prototipo .pill), no un óvalo: UI Toolkit
        /// no reduce un radio de 999px como CSS. Con la fuente grande crece y el radio la sigue.
        /// </summary>
        [Test]
        public void LaPildoraTieneRadioIgualALaMitadDeSuAlto()
        {
            Assert.AreEqual(24f, Semicirculo.Radio(new Vector2(150f, 48f)));
            Assert.AreEqual(24f, Semicirculo.Radio(new Vector2(48f, 48f)), "Un círculo de 48 dp.");
            var pildora = new PildoraPrecios(() => { }, c => c);
            foreach (var alto in new[] { 48f, 70f })
            {
                Semicirculo.Aplicar(pildora, new Vector2(160f, alto));
                foreach (var esquina in new[] { pildora.style.borderTopLeftRadius, pildora.style.borderTopRightRadius,
                                                 pildora.style.borderBottomLeftRadius, pildora.style.borderBottomRightRadius })
                    Assert.AreEqual(alto / 2f, esquina.value.value, $"Radio con {alto} dp de alto.");
            }

            // Los círculos de tamaño fijo llevan en los tokens la mitad de su lado; ninguno queda en 999px
            var tokens = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var claro = System.Text.RegularExpressions.Regex.Match(tokens, @"\.pm-raiz\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            float Px(string nombre) => float.Parse(System.Text.RegularExpressions.Regex.Match(claro, System.Text.RegularExpressions.Regex.Escape(nombre) + @":\s*(?<v>[\d.]+)px").Groups["v"].Value,
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(Px("--zona-tactil-min") / 2f, Px("--radio-pill"), "Píldora antes de disponerse: mitad de 48.");
            Assert.AreEqual(Px("--boton-redondo") / 2f, Px("--radio-boton-redondo"));
            Assert.AreEqual(Px("--marcador-insignia") / 2f, Px("--radio-insignia"));
            Assert.AreEqual(Px("--posicion-punto") / 2f, Px("--radio-posicion-punto"));
            Assert.AreEqual(Px("--poi-icono") / 2f, Px("--radio-poi-icono"));
            Assert.AreEqual(Px("--frescura-punto") / 2f, Px("--radio-frescura-punto"));
            StringAssert.DoesNotContain("999px", tokens);
        }

        [Test]
        public void ElMovilSigueConUnDpPorCadaPixelA160Dpi()
        {
            var ajustes = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
            Assert.AreEqual(PanelScaleMode.ConstantPhysicalSize, ajustes.scaleMode, "La captura no toca el PanelSettings guardado.");
            Assert.AreEqual(160f, ajustes.referenceDpi);
        }
    }
}
