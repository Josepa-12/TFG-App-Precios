using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PriceMap.Adaptadores.Gps;
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
    /// Historia 1.7: la posición del usuario en el mapa, el botón de ubicación (centrar, orientar al norte y sus
    /// etiquetas de TalkBack), la explicación del permiso y el aviso de ubicación desactivada. Cada fila de la matriz
    /// de las dos specs, siempre con una IUbicacion o una IFuenteUbicacion falsas: ninguna prueba lee el GPS ni la
    /// brújula reales (Input.location, Input.compass).
    /// </summary>
    public class PruebasUbicacion
    {
        const float Escala = 0.5f;
        const float AnchoPx = 1080f, AltoPx = 1920f;
        static readonly Coordenada Usuario = new Coordenada(36.78, -6.35);

        // ───────────────────────── Dobles ─────────────────────────

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.SinPermiso;
            public Coordenada? Posicion { get; set; }
            public Coordenada? UltimaConocida { get; set; }
            public float? Rumbo { get; set; }
            public event Action Cambiado;
            public int permisos, inicios, detenciones, ajustes;
            public void PedirPermiso() => permisos++;
            public void Iniciar() => inicios++;
            public void Detener() => detenciones++;
            public void AbrirAjustes() => ajustes++;
            public bool RutaActiva { get; set; }
            public bool RutaSoloPrimerPlano { get; set; }
            public readonly List<string> rutas = new List<string>();
            public Resultado<bool> IniciarRuta(string textoNotificacion) { rutas.Add(textoNotificacion); RutaActiva = true; return Resultado<bool>.Exito(true); }
            public void ActualizarNotificacion(string textoNotificacion) => rutas.Add(textoNotificacion);
            public void TerminarRuta() { RutaActiva = false; RutaSoloPrimerPlano = false; }

            public void Poner(EstadoUbicacion estado, Coordenada? posicion = null, float? rumbo = null)
            {
                Estado = estado;
                Posicion = posicion;
                Rumbo = rumbo;
                Cambiado?.Invoke();
            }
        }

        /// <summary>
        /// Proyecta cualquier coordenada en el mismo punto, salvo las de "proyecciones". Centrar mueve el centro de la
        /// vista; cambiar "centro" a mano es arrastrar el mapa. OrientarNorte pone el giro a 0.
        /// </summary>
        class MapaFalso : IMapa
        {
            public readonly List<Coordenada> centrados = new List<Coordenada>();
            public readonly Dictionary<Coordenada, PuntoPantalla> proyecciones = new Dictionary<Coordenada, PuntoPantalla>();
            public PuntoPantalla proyeccion = new PuntoPantalla(500f, 900f, 300f);
            public float giro;
            public Coordenada centro;
            public int nortes;
            public void Centrar(Coordenada destino) { centrados.Add(destino); centro = destino; }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => proyecciones.TryGetValue(punto, out var p) ? p : proyeccion;
            public IReadOnlyList<PuntoInteres> PuntosInteres { get; } = new PuntoInteres[0];
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => giro;
            public Coordenada Centro => centro;
            public void OrientarNorte() { nortes++; giro = 0f; }
            public event Action GestoUsuario;
            public void Gesto() => GestoUsuario?.Invoke();
            public event System.Action<PuntoPantalla> Toque { add { } remove { } }
            public event System.Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
        }

        class PreferenciasFalsas : IPreferencias
        {
            public readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class SinLugares : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(new Lugar[0]);
        }

        static ServicioLugares Lugares() { var l = new ServicioLugares(new SinLugares()); l.Cargar(); return l; }
        static readonly DateTime Ahora = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        static string TextoEs(string clave) => PrepararInterfaz.TextosEs[clave];

        readonly List<GameObject> objetos = new List<GameObject>();

        [SetUp]
        public void Preparar() => Textos.Forzados = TextoEs; // en EditMode Localization no tiene locale

        [TearDown]
        public void Limpiar()
        {
            foreach (var o in objetos) UnityEngine.Object.DestroyImmediate(o);
            objetos.Clear();
            AccesibilidadPantalla.LectorForzado = null;
            Textos.Forzados = null;
            AssistiveSupport.activeHierarchy = null;
        }

        class Montaje
        {
            public PantallaMapa pantalla;
            public VisualElement arbol;
            public MapaFalso mapa;
            public UbicacionFalsa gps;
            public ServicioUbicacion servicio;
            public PreferenciasFalsas prefs;
        }

        /// <summary>Pantalla con la interfaz real, compuesta como en Arranque.Componer.</summary>
        Montaje Pantalla(UbicacionFalsa gps, PreferenciasFalsas prefs = null)
        {
            prefs = prefs ?? new PreferenciasFalsas();
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            var pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var servicio = new ServicioUbicacion(gps, prefs);
            var mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, servicio);
            tema.Iniciar();
            filtro.Iniciar();
            servicio.Iniciar();
            return new Montaje { pantalla = pantalla, arbol = arbol, mapa = mapa, gps = gps, servicio = servicio, prefs = prefs };
        }

        static void Actualizar(Montaje m) =>
            m.pantalla.MarcadorPosicion.Actualizar(m.mapa, m.servicio.Posicion, m.servicio.Rumbo, Escala, AnchoPx, AltoPx);

        static List<string> RecorridoExplicacion() => new List<string>
        {
            TextoEs(ClavesTexto.UbicacionExplicacion), TextoEs(ClavesTexto.UbicacionAhoraNo), TextoEs(ClavesTexto.UbicacionPermitir),
        };

        // ───────────────────────── Explicación del permiso ─────────────────────────

        [Test]
        public void AlAbrirNoSaleLaExplicacionYElBotonLaSaca()
        {
            var m = Pantalla(new UbicacionFalsa());
            Assert.IsFalse(m.pantalla.ExplicacionAbierta, "Desde la 3.1, el primer arranque es de la bienvenida.");
            m.pantalla.TocarUbicacion();
            Assert.IsTrue(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(DisplayStyle.Flex, m.pantalla.Explicacion.style.display.value);
            Assert.AreEqual(TextoEs(ClavesTexto.UbicacionExplicacion), m.pantalla.Explicacion.Texto.text);
            Assert.AreEqual("Permitir", m.pantalla.Explicacion.Permitir.text);
            Assert.AreEqual("Ahora no", m.pantalla.Explicacion.AhoraNo.text);
            Assert.AreEqual(0, m.gps.permisos, "El permiso de Android no se pide hasta pulsar \"Permitir\".");
        }

        [Test]
        public void ConLaExplicacionAbiertaTalkBackEsModalYElMapaNoSeMueve()
        {
            var m = Pantalla(new UbicacionFalsa());
            m.pantalla.TocarUbicacion();
            Assert.IsTrue(m.pantalla.Accesibilidad.Modal);
            CollectionAssert.AreEqual(RecorridoExplicacion(), m.pantalla.Accesibilidad.Recorrido());
            Assert.AreSame(m.pantalla.Explicacion.Texto, m.pantalla.Accesibilidad.Enfocado, "El foco va al texto de la explicación.");
            Assert.IsTrue(m.pantalla.TapaPunto(new Vector2(10f, 10f)), "Con la ventana abierta, ningún toque mueve el mapa.");
            Assert.AreSame(m.arbol.Q("hueco-explicacion"), m.pantalla.Explicacion.parent);
        }

        [Test]
        public void ConElLectorElTextoDeLaExplicacionEsTextoYSusBotonesSonBotones()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var m = Pantalla(new UbicacionFalsa());
            m.pantalla.TocarUbicacion();
            Assert.IsTrue(m.pantalla.ExplicacionAbierta);
            var jerarquia = AssistiveSupport.activeHierarchy;
            if (jerarquia == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");

            var nodos = jerarquia.rootNodes;
            CollectionAssert.AreEqual(RecorridoExplicacion(), nodos.Select(n => n.label), "Recorrido modal: texto, \"Ahora no\" y \"Permitir\".");
            var texto = nodos.Single(n => n.label == TextoEs(ClavesTexto.UbicacionExplicacion));
            Assert.AreEqual(AccessibilityRole.StaticText, texto.role, "El texto de la explicación no es un botón.");
            Assert.AreEqual(AccessibilityRole.Button, nodos.Single(n => n.label == TextoEs(ClavesTexto.UbicacionAhoraNo)).role);
            Assert.AreEqual(AccessibilityRole.Button, nodos.Single(n => n.label == TextoEs(ClavesTexto.UbicacionPermitir)).role);
        }

        [Test]
        public void SiguientesAperturasNoSaleLaExplicacion()
        {
            var m = Pantalla(new UbicacionFalsa());
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(DisplayStyle.None, m.pantalla.Explicacion.style.display.value);
            Assert.IsFalse(m.pantalla.Accesibilidad.Modal);
        }

        [Test]
        public void AhoraNoYTocarElBotonVuelveASalirLaExplicacion()
        {
            var m = Pantalla(new UbicacionFalsa());
            m.pantalla.TocarUbicacion();
            m.pantalla.AhoraNoUbicacion();
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.IsFalse(m.pantalla.Accesibilidad.Modal);
            Assert.AreSame(m.pantalla.BotonUbicacion, m.pantalla.Accesibilidad.Enfocado, "Al cerrar, el foco vuelve al botón de ubicación.");
            Assert.AreEqual(0, m.gps.permisos);

            m.pantalla.TocarUbicacion(); // SinPermiso
            Assert.IsTrue(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(AccionUbicacion.Explicar, m.servicio.AlTocarBoton());
        }

        [Test]
        public void PermitirCierraLaExplicacionYLanzaElPermisoDeAndroid()
        {
            var m = Pantalla(new UbicacionFalsa());
            m.pantalla.TocarUbicacion();
            m.pantalla.PermitirUbicacion();
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(1, m.gps.permisos);
        }

        [Test]
        public void AtrasConLaExplicacionAbiertaEsAhoraNo()
        {
            var m = Pantalla(new UbicacionFalsa());
            m.pantalla.TocarUbicacion();
            Assert.IsTrue(m.pantalla.Atras());
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(0, m.gps.permisos);
            Assert.IsFalse(m.pantalla.Atras(), "Sin ventana ni menú abiertos, atrás no hace nada.");
        }

        // ───────────────────────── Círculo y cono ─────────────────────────

        [Test]
        public void ConPermisoElCirculoVaEnSuPosicionConElConoAlEste()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            Assert.IsFalse(m.pantalla.ExplicacionAbierta, "Con permiso no hace falta la explicación.");
            Actualizar(m);

            var marcador = m.pantalla.MarcadorPosicion;
            Assert.IsTrue(marcador.Visible);
            Assert.AreEqual(new Vector2(250f, 450f), marcador.Posicion, "En la proyección de su coordenada (px × escala dp).");
            Assert.IsTrue(marcador.ConoVisible);
            Assert.AreEqual(90f, marcador.AnguloCono, 0.001f, "Rumbo 90° con el mapa sin girar: el cono mira al este.");
            Assert.AreEqual(DisplayStyle.Flex, marcador.Cono.style.display.value);
        }

        /// <summary>El camino de cada fotograma (LateUpdate → ActualizarCapas) también coloca el círculo y gira el cono.</summary>
        [Test]
        public void CadaFotogramaActualizarCapasColocaElCirculoYElCono()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            // ActualizarCapas usa Screen.width/height: el punto cae en el centro de la pantalla del editor
            m.mapa.proyeccion = new PuntoPantalla(Screen.width / 2f, Screen.height / 2f, 300f);
            m.mapa.giro = 40f;
            m.pantalla.ActualizarCapas(Escala);

            var marcador = m.pantalla.MarcadorPosicion;
            Assert.IsTrue(marcador.Visible);
            Assert.IsTrue(marcador.ConoVisible);
            Assert.AreEqual(50f, marcador.AnguloCono, 0.001f);
            Assert.AreEqual(new Vector2(Screen.width / 2f * Escala, Screen.height / 2f * Escala), marcador.Posicion);
        }

        [Test]
        public void ElCirculoVaPorEncimaDeLosLugaresYDeLosPuntos()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            var raiz = m.arbol.Q("raiz");
            var capa = m.pantalla.MarcadorPosicion.parent;
            Assert.AreEqual("capa-posicion", capa.name);
            Assert.Greater(raiz.IndexOf(capa), raiz.IndexOf(raiz.Q("capa-marcadores")));
            Assert.Greater(raiz.IndexOf(capa), raiz.IndexOf(raiz.Q("capa-puntos")));
            Assert.Less(raiz.IndexOf(capa), raiz.IndexOf(raiz.Q("zona-segura")), "Los botones siguen encima.");
            Assert.AreEqual(PickingMode.Ignore, m.pantalla.MarcadorPosicion.pickingMode, "El círculo no captura toques.");
        }

        [Test]
        public void ConElMapaGiradoElConoSeGiraConEl()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            m.mapa.giro = 40f;
            Actualizar(m);
            Assert.AreEqual(50f, m.pantalla.MarcadorPosicion.AnguloCono, 0.001f, "Rumbo 90° y giro 40°: el cono a 50° en pantalla.");
            Assert.AreEqual(330f, MarcadorPosicion.AnguloEnPantalla(10f, 40f), 0.001f, "Siempre en [0, 360).");
        }

        [Test]
        public void SinBrujulaSoloElCirculo()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, null);
            var m = Pantalla(gps);
            Actualizar(m);
            Assert.IsTrue(m.pantalla.MarcadorPosicion.Visible);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.ConoVisible);
            Assert.AreEqual(DisplayStyle.None, m.pantalla.MarcadorPosicion.Cono.style.display.value);
        }

        [Test]
        public void DetrasDeLaCamaraOFueraDeLaPantallaNoSeVe()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            m.mapa.proyeccion = PuntoPantalla.Ninguno;
            Actualizar(m);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.Visible);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.ConoVisible);
            m.mapa.proyeccion = new PuntoPantalla(5000f, 900f, 300f);
            Actualizar(m);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.Visible);
        }

        /// <summary>Cursor más grande: a menos de medio cono (50 dp) del borde aún se coloca; más allá, no.</summary>
        [Test]
        public void ElCursorSeColocaHastaMedioConoFueraDelBorde()
        {
            var tokens = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            float cono = float.Parse(System.Text.RegularExpressions.Regex.Match(tokens, @"--posicion-cono:\s*(?<v>[\d.]+)px").Groups["v"].Value,
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(cono / 2f, MarcadorPosicion.MargenDp, "La mitad del cono (--posicion-cono).");
            var mapa = new MapaFalso();
            var marcador = new MarcadorPosicion();
            void Poner(float x, float y)
            {
                mapa.proyeccion = new PuntoPantalla(x, y, 300f);
                marcador.Actualizar(mapa, Usuario, 90f, 1f, 400f, 800f);
            }
            Poner(-49f, 400f);
            Assert.IsTrue(marcador.Visible, "A 49 dp por la izquierda.");
            Assert.AreEqual(new Vector2(-49f, 400f), marcador.Posicion, "Centrado en la coordenada.");
            Poner(400f + 49f, 800f + 49f);
            Assert.IsTrue(marcador.Visible, "A 49 dp por abajo a la derecha.");
            Poner(-50f, -50f);
            Assert.IsTrue(marcador.Visible, "Justo a 50 dp por arriba a la izquierda.");
            Poner(400f + 50f, 800f + 50f);
            Assert.IsTrue(marcador.Visible, "Justo a 50 dp por abajo a la derecha.");
            Poner(-51f, 400f);
            Assert.IsFalse(marcador.Visible);
            Poner(200f, -51f);
            Assert.IsFalse(marcador.Visible);
            Poner(400f + 51f, 400f);
            Assert.IsFalse(marcador.Visible, "Pasado por la derecha.");
            Poner(200f, 800f + 51f);
            Assert.IsFalse(marcador.Visible, "Pasado por abajo.");
        }

        /// <summary>
        /// Tamaños de la maqueta B (22/11/2026): punto de 24 dp con borde de 4, cono de 100, flecha de 30 sobre un disco
        /// de 48 con sombra suave. Todo centrado en la coordenada (translate −50 %).
        /// </summary>
        [Test]
        public void ElCursorTieneLosTamanosDeLaMaqueta()
        {
            var tokens = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            string Token(string nombre) =>
                System.Text.RegularExpressions.Regex.Match(tokens, System.Text.RegularExpressions.Regex.Escape(nombre) + @":\s*(?<v>[^;]+);").Groups["v"].Value.Trim();
            Assert.AreEqual("24px", Token("--posicion-punto"));
            Assert.AreEqual("4px", Token("--posicion-punto-borde"));
            Assert.AreEqual("100px", Token("--posicion-cono"));
            Assert.AreEqual("30px", Token("--posicion-flecha"));
            Assert.AreEqual("48px", Token("--posicion-disco"));

            var uss = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            string Regla(string clase) =>
                System.Text.RegularExpressions.Regex.Match(uss, System.Text.RegularExpressions.Regex.Escape(clase) + @"\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            foreach (var clase in new[] { ".marcador-posicion__cono", ".marcador-posicion__punto", ".marcador-posicion__disco", ".marcador-posicion__flecha" })
                StringAssert.Contains("translate: -50% -50%", Regla(clase), $"{clase} centrado en la coordenada.");
            StringAssert.Contains("border-width: var(--posicion-punto-borde)", Regla(".marcador-posicion__punto"));
            StringAssert.Contains("background-color: var(--color-posicion-borde)", Regla(".marcador-posicion__disco"), "Disco blanco.");

            var marcador = new MarcadorPosicion();
            Assert.IsTrue(SombraSuave.Tiene(marcador.Disco), "El disco lleva la sombra suave.");
            Assert.Less(marcador.IndexOf(marcador.Disco), marcador.IndexOf(marcador.Flecha), "La flecha, encima del disco.");
            Assert.AreEqual(DisplayStyle.None, marcador.Disco.style.display.value, "Sin flecha, sin disco.");
        }

        // ───────────────────────── Botón de ubicación ─────────────────────────

        [Test]
        public void ElBotonVaEnLaColumnaDebajoDelDeTemaConSuTextoParaTalkBack()
        {
            var m = Pantalla(new UbicacionFalsa());
            var boton = m.pantalla.BotonUbicacion;
            Assert.IsNotNull(boton);
            Assert.AreEqual(Icono.Navegar, boton.Icono, "La flecha de navegación del prototipo, no la diana.");
            Assert.AreEqual("hueco-ubicacion", boton.parent.name);
            var columna = m.arbol.Q("columna-controles");
            Assert.AreSame(columna, boton.parent.parent);
            Assert.AreEqual(columna.IndexOf(m.arbol.Q("hueco-tema")) + 1, columna.IndexOf(boton.parent), "Justo debajo del botón de tema.");
            Assert.IsTrue(PantallaMapa.EsControl(m.arbol.Q("raiz"), boton), "El botón tapa el mapa al pulsarlo.");

            // Esquinas y separador de la columna en los botones reales
            var tema = m.pantalla.BotonTema;
            Assert.IsTrue(tema.ClassListContains("boton-redondo--arriba"));
            Assert.IsFalse(tema.ClassListContains("boton-redondo--abajo"));
            Assert.IsFalse(tema.ClassListContains(BotonRedondo.ClaseSeparado));
            Assert.IsTrue(boton.ClassListContains(BotonRedondo.ClaseSeparado), "Línea entre tema y ubicación.");
            Assert.IsTrue(boton.ClassListContains("boton-redondo--abajo"));
            var alejar = m.arbol.Q<BotonRedondo>("boton-alejar");
            Assert.IsTrue(alejar.ClassListContains(BotonRedondo.ClaseSeparado));
            Assert.IsTrue(alejar.ClassListContains("boton-redondo--abajo"));
            Assert.IsTrue(m.arbol.Q<BotonRedondo>("boton-acercar").ClassListContains("boton-redondo--arriba"));

            var recorrido = m.pantalla.Accesibilidad.Recorrido();
            Assert.AreEqual("Centrar en mi posición", TextoEs(ClavesTexto.UbicacionCentrar));
            Assert.AreEqual(recorrido.IndexOf(TextoEs(ClavesTexto.TemaCambiar)) + 1, recorrido.IndexOf(TextoEs(ClavesTexto.UbicacionCentrar)),
                "TalkBack lo lee después del botón de tema.");
        }

        /// <summary>Ajuste visual de los controles: la flecha se queda en todos los estados y la etiqueta los sigue marcando.</summary>
        [TestCase(EstadoUbicacion.Buscando, false, ClavesTexto.UbicacionBuscando)]
        [TestCase(EstadoUbicacion.Denegado, false, ClavesTexto.UbicacionDesactivada)]
        [TestCase(EstadoUbicacion.Activa, false, ClavesTexto.UbicacionCentrar)]
        [TestCase(EstadoUbicacion.Activa, true, ClavesTexto.UbicacionNorte)]
        public void LaFlechaSeQuedaYLosEstadosSeSiguenMarcando(EstadoUbicacion estado, bool centradoYGirado, string clave)
        {
            Montaje m;
            if (centradoYGirado)
            {
                m = ActivaLejos(); // mapa girado 40°
                m.pantalla.TocarUbicacion(); // centra en el usuario: el siguiente toque orientaría al norte
            }
            else
            {
                var gps = new UbicacionFalsa();
                gps.Poner(estado, estado == EstadoUbicacion.Activa ? Usuario : (Coordenada?)null);
                m = Pantalla(gps);
            }
            m.pantalla.ActualizarCapas(Escala);
            var etiqueta = TextoEs(clave);
            Assert.AreEqual(Icono.Navegar, m.pantalla.BotonUbicacion.Icono);
            Assert.IsTrue(m.pantalla.BotonUbicacion.EnColumna);
            Assert.AreEqual(etiqueta, EtiquetaBoton(m));
            CollectionAssert.Contains(m.pantalla.Accesibilidad.Recorrido(), etiqueta);
        }

        [Test]
        public void SinServicioDeUbicacionNoHayBotonNiCirculoNiExplicacion()
        {
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            var pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            pantalla.Inicializar(new MapaFalso(), new ServicioTema(new PreferenciasFalsas(), new SistemaClaro()), Lugares(), Ahora, arbol);
            Assert.IsNull(pantalla.BotonUbicacion);
            Assert.IsNull(pantalla.MarcadorPosicion);
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            // Sin ubicación, el de tema es el único hueco de la columna: las cuatro esquinas y sin separador
            Assert.IsTrue(pantalla.BotonTema.ClassListContains("boton-redondo--arriba"));
            Assert.IsTrue(pantalla.BotonTema.ClassListContains("boton-redondo--abajo"));
            Assert.IsFalse(pantalla.BotonTema.ClassListContains(BotonRedondo.ClaseSeparado));
        }

        [Test]
        public void BuscandoNoHayCirculoYTocarElBotonNoHaceNada()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Buscando);
            var m = Pantalla(gps);
            Actualizar(m);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.Visible);
            m.pantalla.TocarUbicacion();
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial }, m.mapa.centrados, "No se centra en nada.");
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(AccionUbicacion.Nada, m.servicio.AlTocarBoton());
        }

        // ───────────────────────── Centrado al abrir ─────────────────────────

        static readonly Coordenada OtroSitio = new Coordenada(36.7750, -6.3500);

        [Test]
        public void AlAbrirSeCentraEnLasPosicionesDeLosPrimeros10Segundos()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Buscando);
            var m = Pantalla(gps);
            var reloj = Ahora;
            m.pantalla.RelojCamara = () => reloj;
            m.pantalla.ActualizarCapas(Escala);
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial }, m.mapa.centrados, "Buscando, la vista inicial.");

            reloj = reloj.AddSeconds(60); // el plazo empieza con la primera posición
            gps.Poner(EstadoUbicacion.Activa, Usuario);
            m.pantalla.ActualizarCapas(Escala);
            m.pantalla.ActualizarCapas(Escala);
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial, Usuario }, m.mapa.centrados, "Una vez por posición.");

            reloj = reloj.AddSeconds(10); // la primera era poco precisa: llega la buena
            gps.Poner(EstadoUbicacion.Activa, OtroSitio);
            m.pantalla.ActualizarCapas(Escala);
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial, Usuario, OtroSitio }, m.mapa.centrados);

            reloj = reloj.AddSeconds(0.1);
            gps.Poner(EstadoUbicacion.Activa, Usuario);
            m.pantalla.ActualizarCapas(Escala);
            gps.Poner(EstadoUbicacion.Buscando);
            m.pantalla.ActualizarCapas(Escala);
            gps.Poner(EstadoUbicacion.Activa, OtroSitio);
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(3, m.mapa.centrados.Count, "Después se mueve el cursor y no el mapa.");
        }

        [Test]
        public void AlAbrirUnGestoTrasLaPrimeraPosicionDejaDeCentrar()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario); // ya con posición en el primer fotograma
            var m = Pantalla(gps);
            m.pantalla.RelojCamara = () => Ahora;
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(Usuario, m.mapa.centro);
            m.mapa.Gesto();
            gps.Poner(EstadoUbicacion.Activa, OtroSitio);
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(2, m.mapa.centrados.Count);
        }

        [Test]
        public void AlAbrirLaVistaEmpiezaEnLaUltimaUbicacionDelSistemaYElGpsLaAfina()
        {
            var gps = new UbicacionFalsa { UltimaConocida = OtroSitio };
            gps.Poner(EstadoUbicacion.Buscando);
            var m = Pantalla(gps);
            CollectionAssert.AreEqual(new[] { OtroSitio }, m.mapa.centrados, "Desde el principio, sin pasar por la vista inicial.");
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(1, m.mapa.centrados.Count);
            Assert.IsNull(m.servicio.Posicion, "No es una lectura: el cursor no sale todavía.");
            gps.Poner(EstadoUbicacion.Activa, Usuario);
            m.pantalla.ActualizarCapas(Escala);
            CollectionAssert.AreEqual(new[] { OtroSitio, Usuario }, m.mapa.centrados);
        }

        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        public void LaUltimaUbicacionDelSistemaSoloConPermisoYEncendida(bool permiso, bool encendida, bool hay)
        {
            var gps = new UbicacionGps(new FuenteFalsa { TienePermiso = permiso, UbicacionEncendida = encendida, UltimaConocida = OtroSitio });
            Assert.AreEqual(hay ? OtroSitio : (Coordenada?)null, gps.UltimaConocida);
            Assert.IsNull(gps.Posicion);
        }

        [Test]
        public void ElPluginDaLaUltimaUbicacionSinEncenderElGps()
        {
            string java = System.IO.File.ReadAllText("Assets/Plugins/Android/PuenteUbicacionRuta.java");
            int i = java.IndexOf("public static String ultimaConocida()", StringComparison.Ordinal);
            Assert.Greater(i, 0);
            string metodo = java.Substring(i, java.IndexOf("static String texto()", StringComparison.Ordinal) - i);
            StringAssert.Contains("gestor.getProviders(true)", metodo);
            StringAssert.Contains("getLastKnownLocation(proveedor)", metodo);
            StringAssert.Contains("catch (SecurityException", metodo, "Con solo la aproximada no se puede leer el GPS.");
            StringAssert.Contains("ServicioUbicacionRuta.linea(mejor)", metodo, "El formato que lee LecturaRuta.");
            StringAssert.DoesNotContain("requestLocationUpdates", metodo);
            string fuente = System.IO.File.ReadAllText("Assets/Scripts/Adaptadores/Gps/FuenteUbicacionUnity.cs");
            StringAssert.Contains("CallStatic<string>(\"ultimaConocida\")", fuente);
        }

        [Test]
        public void AlAbrirSinPermisoYAlConcederloSeCentra()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.SinPermiso);
            var m = Pantalla(gps);
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(1, m.mapa.centrados.Count);
            gps.Poner(EstadoUbicacion.Activa, Usuario);
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(Usuario, m.mapa.centro);
        }

        [TestCase("gesto")]
        [TestCase("acercar")]
        [TestCase("alejar")]
        [TestCase("ficha")]
        public void AlAbrirSiElUsuarioYaHaUsadoElMapaNoSeCentra(string accion)
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Buscando);
            var m = Pantalla(gps);
            if (accion == "gesto") m.mapa.Gesto();
            else if (accion == "ficha") { m.pantalla.AbrirPunto(OtroSitio); m.pantalla.CerrarFicha(); }
            else m.arbol.Q<BotonRedondo>("boton-" + accion).Pulsar();
            gps.Poner(EstadoUbicacion.Activa, Usuario);
            m.pantalla.ActualizarCapas(Escala);
            CollectionAssert.DoesNotContain(m.mapa.centrados, Usuario);
            m.pantalla.TocarUbicacion();
            CollectionAssert.Contains(m.mapa.centrados, Usuario, "El botón sigue centrando.");
        }

        [Test]
        public void TocarConPosicionCentraElMapaEnElUsuario()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            m.pantalla.TocarUbicacion();
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial, Usuario }, m.mapa.centrados);
        }

        [Test]
        public void ConPosicionLaBusquedaMideDesdeElUsuarioYNoDesdeElCentro()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            Assert.AreNotEqual(Usuario, m.mapa.Centro, "El centro del mapa no es el usuario.");
            Assert.AreEqual(Usuario, m.pantalla.PuntoCercania(), "Con posición: la del usuario (historia 2.3).");
            gps.Poner(EstadoUbicacion.Denegado);
            Assert.AreEqual(m.mapa.Centro, m.pantalla.PuntoCercania(), "Sin posición: el centro del mapa.");
        }

        [Test]
        public void DenegadoElMapaSigueEnLaVistaInicialSinCirculoYTocarSacaElAviso()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Denegado);
            var m = Pantalla(gps);
            Assert.IsFalse(m.pantalla.ExplicacionAbierta, "Denegado no es SinPermiso: la explicación no sale al abrir.");
            Assert.IsFalse(m.pantalla.AvisoAbierto, "El aviso solo sale al tocar el botón.");
            Actualizar(m);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.Visible);
            m.pantalla.TocarUbicacion();
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial }, m.mapa.centrados);
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.IsTrue(m.pantalla.AvisoAbierto, "Ahora sale el aviso de ubicación desactivada.");
            Assert.AreEqual(0, gps.permisos);
        }

        [Test]
        public void LaPosicionSoloCuentaEnActiva()
        {
            var gps = new UbicacionFalsa();
            var servicio = new ServicioUbicacion(gps, new PreferenciasFalsas());
            int avisos = 0;
            servicio.Cambiado += () => avisos++;
            gps.Poner(EstadoUbicacion.Buscando, Usuario, 90f); // una posición vieja mientras busca no se pinta
            Assert.IsNull(servicio.Posicion);
            Assert.IsNull(servicio.Rumbo);
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            Assert.AreEqual(Usuario, servicio.Posicion);
            Assert.AreEqual(90f, servicio.Rumbo);
            Assert.AreEqual(2, avisos, "El servicio reenvía cada cambio de la ubicación.");
        }

        // ───────────────────────── Segundo plano ─────────────────────────

        [Test]
        public void EnSegundoPlanoSeDetienenYAlVolverSeReanudan()
        {
            var gps = new UbicacionFalsa();
            var servicio = new ServicioUbicacion(gps, new PreferenciasFalsas());
            servicio.Iniciar();
            Assert.AreEqual(1, gps.inicios);

            servicio.Pausar();                 // OnApplicationPause(true)
            servicio.Pausar();                 // y OnApplicationFocus(false): una sola vez
            Assert.AreEqual(1, gps.detenciones);
            Assert.IsTrue(servicio.Pausada);

            servicio.Reanudar();               // OnApplicationPause(false)
            servicio.Reanudar();               // y OnApplicationFocus(true)
            Assert.AreEqual(2, gps.inicios);
            Assert.IsFalse(servicio.Pausada);
        }

        [Test]
        public void EnPausaIniciarNoEnciendeNadaHastaVolver()
        {
            var gps = new UbicacionFalsa();
            var servicio = new ServicioUbicacion(gps, new PreferenciasFalsas());
            servicio.Pausar();
            servicio.Iniciar();
            Assert.AreEqual(0, gps.inicios, "Con la app en segundo plano no se enciende el GPS.");
            servicio.Reanudar();
            Assert.AreEqual(1, gps.inicios);

            var otro = new UbicacionFalsa();
            var sinIniciar = new ServicioUbicacion(otro, new PreferenciasFalsas());
            sinIniciar.Pausar();
            sinIniciar.Reanudar();
            Assert.AreEqual(0, otro.inicios, "Volver a la app no enciende el GPS si el Arranque aún no lo ha iniciado.");
        }

        /// <summary>
        /// Lo que hace el Arranque con cada aviso de Unity: OnApplicationPause(true) y OnApplicationFocus(false)
        /// detienen; OnApplicationPause(false) y OnApplicationFocus(true) reanudan.
        /// </summary>
        [Test]
        public void ElArranqueDetieneYReanudaConCadaAvisoDeUnity()
        {
            var gps = new UbicacionFalsa();
            var servicio = new ServicioUbicacion(gps, new PreferenciasFalsas());
            servicio.Iniciar();
            Assert.AreEqual((1, 0), (gps.inicios, gps.detenciones));

            Arranque.Arranque.AlPausar(servicio, true);          // OnApplicationPause(true): detiene
            Assert.AreEqual((1, 1), (gps.inicios, gps.detenciones));
            Arranque.Arranque.AlPausar(servicio, false);         // OnApplicationPause(false): reanuda
            Assert.AreEqual((2, 1), (gps.inicios, gps.detenciones));
            Arranque.Arranque.AlCambiarFoco(servicio, false);    // OnApplicationFocus(false): detiene
            Assert.AreEqual((2, 2), (gps.inicios, gps.detenciones));
            Arranque.Arranque.AlCambiarFoco(servicio, true);     // OnApplicationFocus(true): reanuda
            Assert.AreEqual((3, 2), (gps.inicios, gps.detenciones));

            Assert.DoesNotThrow(() => { Arranque.Arranque.AlPausar(null, true); Arranque.Arranque.AlCambiarFoco(null, true); },
                "Sin servicio (antes de componer) no hace nada.");
        }

        [Test]
        public void ComponerEntregaLaUbicacionALaPantallaYLaInicia()
        {
            var gps = new UbicacionFalsa();
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            var pantalla = objeto.AddComponent<PantallaMapa>();
            var prefs = new PreferenciasFalsas();
            var servicio = new ServicioUbicacion(gps, prefs);
            Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), pantalla, new ServicioTema(prefs, new SistemaClaro()), Lugares(),
                new ServicioFiltro(prefs), Ahora, servicio));
            Assert.AreEqual(1, gps.inicios);
        }

        // ───────────────────────── Parte 2: centrar, norte y etiquetas ─────────────────────────

        static readonly Coordenada Lejos = new Coordenada(36.80, -6.35);
        static readonly Coordenada Arrastrado = new Coordenada(36.781, -6.35);

        /// <summary>Usuario Activo con el mapa girado 40°, lejos del usuario (400 px = 200 dp del centro) y la escala aplicada.</summary>
        Montaje ActivaLejos(float giro = 40f)
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f);
            var m = Pantalla(gps);
            m.mapa.giro = giro;
            m.mapa.proyecciones[Lejos] = new PuntoPantalla(900f, 900f, 300f);
            m.mapa.proyecciones[Arrastrado] = new PuntoPantalla(600f, 900f, 300f); // 100 px = 50 dp
            m.mapa.Gesto(); // el usuario ya ha movido el mapa: no se centra solo al abrir
            m.mapa.centro = Lejos;
            m.pantalla.ActualizarCapas(Escala);
            return m;
        }

        string EtiquetaBoton(Montaje m) => TextoEs(m.pantalla.ClaveBotonUbicacion());

        [Test]
        public void PrimerToqueLejosCentraYLaEtiquetaPasaAOrientarAlNorte()
        {
            var m = ActivaLejos();
            Assert.AreEqual(200f, m.pantalla.DistanciaAlCentroDp(), 0.001f);
            Assert.AreEqual(AccionUbicacion.Centrar, m.pantalla.AccionUbicacionActual());
            Assert.AreEqual("Centrar en mi posición", EtiquetaBoton(m));
            CollectionAssert.Contains(m.pantalla.Accesibilidad.Recorrido(), "Centrar en mi posición");

            m.pantalla.TocarUbicacion();
            Assert.AreEqual(Usuario, m.mapa.centrados.Last(), "El primer toque centra en el usuario.");
            Assert.AreEqual(0, m.mapa.nortes);
            Assert.AreEqual(40f, m.mapa.giro, "Centrar no cambia el giro.");
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreEqual("Orientar al norte", EtiquetaBoton(m));
            CollectionAssert.Contains(m.pantalla.Accesibilidad.Recorrido(), "Orientar al norte");
        }

        [Test]
        public void PrimerToqueLejosConElMapaAlNorteLaEtiquetaSigueEnCentrar()
        {
            var m = ActivaLejos(giro: 0f);
            m.pantalla.TocarUbicacion();
            Assert.AreEqual(Usuario, m.mapa.centrados.Last());
            Assert.AreEqual("Centrar en mi posición", EtiquetaBoton(m), "Sin giro no hay nada que orientar.");
        }

        [Test]
        public void SegundoToqueCentradoYGiradoOrientaAlNorte()
        {
            var m = ActivaLejos();
            m.pantalla.TocarUbicacion(); // centra
            int centrados = m.mapa.centrados.Count;
            m.pantalla.TocarUbicacion(); // ya centrado y con giro 40°
            Assert.AreEqual(1, m.mapa.nortes);
            Assert.AreEqual(0f, m.mapa.giro);
            Assert.AreEqual(centrados, m.mapa.centrados.Count, "Orientar al norte no vuelve a centrar.");
            Assert.AreEqual("Centrar en mi posición", EtiquetaBoton(m));
        }

        [Test]
        public void CentradoYAlNorteVuelveACentrarSinEfectoVisible()
        {
            var m = ActivaLejos(giro: 0f);
            m.mapa.centro = Usuario;
            Assert.AreEqual(AccionUbicacion.Centrar, m.pantalla.AccionUbicacionActual());
            m.pantalla.TocarUbicacion();
            Assert.AreEqual(Usuario, m.mapa.centrados.Last());
            Assert.AreEqual(0, m.mapa.nortes, "Sin modo brújula: nada más.");
        }

        [Test]
        public void TrasArrastrar50DpElSiguienteToqueCentraDeNuevo()
        {
            var m = ActivaLejos();
            m.pantalla.TocarUbicacion(); // centra
            m.mapa.centro = Arrastrado;  // el usuario mueve el mapa 50 dp
            Assert.AreEqual(50f, m.pantalla.DistanciaAlCentroDp(), 0.001f);
            Assert.AreEqual("Centrar en mi posición", EtiquetaBoton(m));
            int centrados = m.mapa.centrados.Count;
            m.pantalla.TocarUbicacion();
            Assert.AreEqual(centrados + 1, m.mapa.centrados.Count);
            Assert.AreEqual(Usuario, m.mapa.centrados.Last());
            Assert.AreEqual(0, m.mapa.nortes, "Tras el arrastre no orienta al norte: centra.");
        }

        [Test]
        public void CentradoEsMenosDe8DpDelCentro()
        {
            var servicio = new ServicioUbicacion(new UbicacionFalsa { Estado = EstadoUbicacion.Activa, Posicion = Usuario }, new PreferenciasFalsas());
            Assert.AreEqual(AccionUbicacion.OrientarNorte, servicio.AlTocarBoton(7.9f, 40f));
            Assert.AreEqual(AccionUbicacion.Centrar, servicio.AlTocarBoton(8f, 40f));
            Assert.AreEqual(AccionUbicacion.Centrar, servicio.AlTocarBoton(), "Sin distancia conocida, centra.");
            Assert.AreEqual(AccionUbicacion.Centrar, servicio.AlTocarBoton(0f, 360f), "360° es el norte.");
            Assert.AreEqual(AccionUbicacion.OrientarNorte, servicio.AlTocarBoton(0f, -15f));
            Assert.IsTrue(ServicioUbicacion.AlNorte(0f));
            Assert.IsTrue(ServicioUbicacion.AlNorte(-720f));
            Assert.IsFalse(ServicioUbicacion.AlNorte(1f));
            Assert.IsFalse(ServicioUbicacion.AlNorte(359f));
            Assert.IsTrue(ServicioUbicacion.AlNorte(359.6f), "Bordes del giro alrededor de 0 (umbral 0,5°).");
            Assert.IsTrue(ServicioUbicacion.AlNorte(-0.4f));
            Assert.IsFalse(ServicioUbicacion.AlNorte(0.6f));
        }

        [Test]
        public void AccionDelBotonEnCadaEstado()
        {
            var gps = new UbicacionFalsa();
            var servicio = new ServicioUbicacion(gps, new PreferenciasFalsas());
            gps.Poner(EstadoUbicacion.SinPermiso);
            Assert.AreEqual(AccionUbicacion.Explicar, servicio.AlTocarBoton(0f, 40f));
            gps.Poner(EstadoUbicacion.Denegado);
            Assert.AreEqual(AccionUbicacion.Avisar, servicio.AlTocarBoton(0f, 40f));
            gps.Poner(EstadoUbicacion.Apagada);
            Assert.AreEqual(AccionUbicacion.Avisar, servicio.AlTocarBoton(0f, 40f));
            gps.Poner(EstadoUbicacion.Buscando);
            Assert.AreEqual(AccionUbicacion.Nada, servicio.AlTocarBoton(0f, 40f));
            gps.Poner(EstadoUbicacion.Activa, Usuario);
            Assert.AreEqual(AccionUbicacion.OrientarNorte, servicio.AlTocarBoton(0f, 40f));
            Assert.AreEqual(AccionUbicacion.Centrar, servicio.AlTocarBoton(50f, 40f));
        }

        [Test]
        public void EtiquetasDelBotonParaTalkBack()
        {
            Assert.AreEqual("Centrar en mi posición", TextoEs(PantallaMapa.ClaveBotonUbicacion(AccionUbicacion.Centrar)));
            Assert.AreEqual("Centrar en mi posición", TextoEs(PantallaMapa.ClaveBotonUbicacion(AccionUbicacion.Explicar)));
            Assert.AreEqual("Orientar al norte", TextoEs(PantallaMapa.ClaveBotonUbicacion(AccionUbicacion.OrientarNorte)));
            Assert.AreEqual("Buscando tu posición", TextoEs(PantallaMapa.ClaveBotonUbicacion(AccionUbicacion.Nada)));
            Assert.AreEqual("Ubicación desactivada", TextoEs(PantallaMapa.ClaveBotonUbicacion(AccionUbicacion.Avisar)));
        }

        [Test]
        public void BuscandoTalkBackDiceBuscandoTuPosicionYTocarNoHaceNada()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Buscando);
            var m = Pantalla(gps);
            Assert.AreEqual("Buscando tu posición", EtiquetaBoton(m));
            CollectionAssert.Contains(m.pantalla.Accesibilidad.Recorrido(), "Buscando tu posición");
            m.pantalla.TocarUbicacion();
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial }, m.mapa.centrados);
            Assert.AreEqual(0, m.mapa.nortes);
            Assert.IsFalse(m.pantalla.AvisoAbierto);
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
        }

        /// <summary>Con el lector, al cambiar lo que hará el botón se rehace la jerarquía con la etiqueta nueva.</summary>
        [Test]
        public void ConElLectorLaEtiquetaNuevaLlegaATalkBack()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Buscando);
            var m = Pantalla(gps);
            m.pantalla.ActualizarCapas(Escala);
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            CollectionAssert.Contains(AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList(), "Buscando tu posición");

            var jerarquia = AssistiveSupport.activeHierarchy;
            gps.Poner(EstadoUbicacion.Activa, Usuario, 90f); // centrado (misma proyección) y con giro
            m.mapa.giro = 40f;
            m.pantalla.ActualizarCapas(Escala);
            Assert.AreSame(jerarquia, AssistiveSupport.activeHierarchy, "La etiqueta cambia en su nodo: la jerarquía no se rehace.");
            var etiquetas = AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList();
            CollectionAssert.Contains(etiquetas, "Orientar al norte");
            CollectionAssert.DoesNotContain(etiquetas, "Buscando tu posición");
        }

        [Test]
        public void ConElAvisoAbiertoSiSeEnciendeLaUbicacionElAvisoSeCierra()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Apagada);
            var m = Pantalla(gps);
            m.pantalla.TocarUbicacion();
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            gps.Poner(EstadoUbicacion.Buscando); // vuelve a la app con la ubicación encendida
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(m.pantalla.AvisoAbierto);
            Assert.IsFalse(m.pantalla.Accesibilidad.Modal);
        }

        [Test]
        public void ConElAvisoAbiertoSiCambiaElMotivoCambiaElTexto()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Denegado);
            var m = Pantalla(gps);
            m.pantalla.TocarUbicacion();
            Assert.AreEqual(TextoEs(ClavesTexto.UbicacionAvisoDenegado), m.pantalla.Aviso.Texto.text);
            gps.Poner(EstadoUbicacion.Apagada);
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            Assert.AreEqual(EstadoUbicacion.Apagada, m.pantalla.Aviso.Motivo);
            Assert.AreEqual(TextoEs(ClavesTexto.UbicacionAvisoApagada), m.pantalla.Aviso.Texto.text);
            CollectionAssert.AreEqual(RecorridoAviso(ClavesTexto.UbicacionAvisoApagada), m.pantalla.Accesibilidad.Recorrido());
        }

        // ───────────────────────── Parte 2: aviso de ubicación desactivada ─────────────────────────

        static List<string> RecorridoAviso(string claveTexto) => new List<string>
        {
            TextoEs(claveTexto), TextoEs(ClavesTexto.UbicacionCerrar), TextoEs(ClavesTexto.UbicacionAbrirAjustes),
        };

        [Test]
        public void DenegadoYTocarSacaElAvisoDePermisoConAbrirAjustes()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Denegado);
            var m = Pantalla(gps);
            Assert.AreEqual("Ubicación desactivada", EtiquetaBoton(m));
            m.pantalla.TocarUbicacion();

            var aviso = m.pantalla.Aviso;
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            Assert.AreEqual(DisplayStyle.Flex, aviso.style.display.value);
            Assert.AreEqual("La ubicación está desactivada. Puedes activarla en Ajustes > Aplicaciones > PriceMap > Permisos.", aviso.Texto.text);
            Assert.AreEqual("Abrir Ajustes", aviso.AbrirAjustes.text);
            Assert.AreEqual("Cerrar", aviso.Cerrar.text);
            Assert.AreSame(m.arbol.Q("hueco-explicacion"), aviso.parent, "Mismo hueco que la explicación, encima de todo.");
            Assert.IsTrue(aviso.ClassListContains("explicacion-ubicacion"), "Comparte los estilos de la explicación.");
            Assert.IsTrue(m.pantalla.Accesibilidad.Modal);
            CollectionAssert.AreEqual(RecorridoAviso(ClavesTexto.UbicacionAvisoDenegado), m.pantalla.Accesibilidad.Recorrido());
            Assert.AreSame(aviso.Texto, m.pantalla.Accesibilidad.Enfocado);
            Assert.IsTrue(m.pantalla.TapaPunto(new Vector2(10f, 10f)), "Con el aviso abierto, ningún toque mueve el mapa.");

            m.pantalla.TocarUbicacion(); // con el aviso abierto no hace nada más
            m.pantalla.AbrirAjustesUbicacion();
            Assert.AreEqual(1, gps.ajustes, "\"Abrir Ajustes\" va por el puerto IUbicacion.");
            Assert.IsFalse(m.pantalla.AvisoAbierto);
            Assert.IsFalse(m.pantalla.Accesibilidad.Modal);
        }

        [Test]
        public void ApagadaYTocarSacaElAvisoDeUbicacionApagada()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Apagada);
            var m = Pantalla(gps);
            Assert.IsFalse(m.pantalla.ExplicacionAbierta, "Con permiso no sale la explicación.");
            Actualizar(m);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.Visible);
            Assert.AreEqual("Ubicación desactivada", EtiquetaBoton(m));
            m.pantalla.TocarUbicacion();
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            Assert.AreEqual(EstadoUbicacion.Apagada, m.pantalla.Aviso.Motivo);
            Assert.AreEqual("La ubicación del móvil está apagada. Actívala en los ajustes rápidos del móvil.", m.pantalla.Aviso.Texto.text);
            CollectionAssert.AreEqual(RecorridoAviso(ClavesTexto.UbicacionAvisoApagada), m.pantalla.Accesibilidad.Recorrido());
            m.pantalla.CerrarAviso();
            Assert.IsFalse(m.pantalla.AvisoAbierto);
            Assert.AreEqual(0, gps.ajustes, "\"Cerrar\" no abre los ajustes.");
        }

        [Test]
        public void AtrasConElAvisoAbiertoLoCierraYElFocoVuelveAlBoton()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Denegado);
            var m = Pantalla(gps);
            m.pantalla.TocarUbicacion();
            Assert.IsTrue(m.pantalla.Atras());
            Assert.IsFalse(m.pantalla.AvisoAbierto);
            Assert.IsFalse(m.pantalla.Accesibilidad.Modal);
            Assert.AreSame(m.pantalla.BotonUbicacion, m.pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(0, gps.ajustes);
            Assert.IsFalse(m.pantalla.Atras(), "Sin ventana ni menú abiertos, atrás no hace nada.");
        }

        [Test]
        public void ElAvisoCierraElMenuPreciosYLaPildoraSaleDelRecorrido()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Denegado);
            var m = Pantalla(gps);
            m.pantalla.AlternarMenu();
            Assert.IsTrue(m.pantalla.MenuAbierto);
            m.pantalla.TocarUbicacion(); // (el velo lo impide en la app; por TalkBack o código, el menú se cierra)
            Assert.IsFalse(m.pantalla.MenuAbierto);
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            CollectionAssert.DoesNotContain(m.pantalla.Accesibilidad.Recorrido(), TextoEs(ClavesTexto.PreciosControl));
        }

        // ───────────────────────── Parte 2: adaptador del GPS con una fuente falsa ─────────────────────────

        class FuenteFalsa : IFuenteUbicacion
        {
            public bool TienePermiso { get; set; }
            public bool UbicacionEncendida { get; set; } = true;
            public LocationServiceStatus EstadoServicio { get; set; } = LocationServiceStatus.Stopped;
            public (Coordenada posicion, double marca) UltimaLectura { get; set; }
            public Coordenada? UltimaConocida { get; set; }
            public float? Rumbo { get; set; }
            public Action<bool> respuesta;
            public int peticiones, encendidos, apagados;
            public readonly List<string> ajustes = new List<string>();
            public void PedirPermiso(Action<bool> r) { peticiones++; respuesta = r; }
            public void Encender() { encendidos++; EstadoServicio = LocationServiceStatus.Initializing; }
            public void Apagar() { apagados++; EstadoServicio = LocationServiceStatus.Stopped; }
            public void AbrirAjustes(string accion) => ajustes.Add(accion);

            /// <summary>El GPS da una lectura nueva.</summary>
            public void Leer(Coordenada c, double marca)
            {
                EstadoServicio = LocationServiceStatus.Running;
                UltimaLectura = (c, marca);
            }
        }

        [Test]
        public void GpsSinPermisoYDenegadoAbreLosAjustesDeLaApp()
        {
            var fuente = new FuenteFalsa();
            var gps = new UbicacionGps(fuente);
            Assert.AreEqual(EstadoUbicacion.SinPermiso, gps.Estado);
            gps.Iniciar();
            Assert.AreEqual(0, fuente.encendidos, "Sin permiso no se enciende el GPS.");

            gps.PedirPermiso();
            Assert.AreEqual(1, fuente.peticiones);
            fuente.respuesta(false); // desde el hilo de Java
            Assert.AreEqual(EstadoUbicacion.SinPermiso, gps.Estado, "La respuesta se aplica en el siguiente fotograma.");
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Denegado, gps.Estado);
            Assert.AreEqual(0, fuente.encendidos);

            gps.AbrirAjustes();
            CollectionAssert.AreEqual(new[] { UbicacionGps.AjustesApp }, fuente.ajustes);
            Assert.AreEqual("android.settings.APPLICATION_DETAILS_SETTINGS", UbicacionGps.AjustesApp);
        }

        [Test]
        public void GpsConcedidoBuscaYConLaPrimeraLecturaPasaAActiva()
        {
            var fuente = new FuenteFalsa();
            var gps = new UbicacionGps(fuente);
            int avisos = 0;
            gps.Cambiado += () => avisos++;
            gps.Iniciar();
            gps.PedirPermiso();
            fuente.TienePermiso = true;
            fuente.respuesta(false); // la precisa denegada...
            fuente.respuesta(true);  // ...y la aproximada concedida: gana el "concedido"
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Buscando, gps.Estado);
            Assert.IsTrue(gps.Encendido);
            Assert.AreEqual(1, fuente.encendidos);

            gps.Actualizar(); // sin lectura todavía
            Assert.AreEqual(EstadoUbicacion.Buscando, gps.Estado);
            fuente.Leer(Usuario, 10.0);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Activa, gps.Estado);
            Assert.AreEqual(Usuario, gps.Posicion);
            Assert.AreEqual(2, avisos);
        }

        [Test]
        public void GpsUnConcedidoNoLoPisaUnDenegadoPosterior()
        {
            var fuente = new FuenteFalsa();
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            gps.PedirPermiso();
            fuente.respuesta(true);
            fuente.respuesta(false);
            fuente.TienePermiso = true;
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Buscando, gps.Estado);
        }

        [Test]
        public void GpsPermisoRetiradoAlVolverPasaASinPermiso()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var gps = new UbicacionGps(fuente);
            Assert.AreEqual(EstadoUbicacion.Buscando, gps.Estado, "Con permiso ya concedido, empieza buscando.");
            gps.Iniciar();
            fuente.Leer(Usuario, 1.0);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Activa, gps.Estado);

            gps.Detener(); // segundo plano
            Assert.IsFalse(gps.Encendido);
            Assert.AreEqual(1, fuente.apagados);
            fuente.TienePermiso = false; // se quita desde Ajustes
            gps.Iniciar();
            Assert.AreEqual(EstadoUbicacion.SinPermiso, gps.Estado);
            Assert.IsNull(gps.Posicion);
            Assert.IsFalse(gps.Encendido);
        }

        [Test]
        public void GpsConLaUbicacionDelMovilApagadaEstaApagadaYAbreLosAjustesDeUbicacion()
        {
            var fuente = new FuenteFalsa { TienePermiso = true, UbicacionEncendida = false };
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            Assert.AreEqual(EstadoUbicacion.Apagada, gps.Estado);
            Assert.IsFalse(gps.Encendido, "Con la ubicación apagada no se enciende el GPS.");
            gps.AbrirAjustes();
            CollectionAssert.AreEqual(new[] { UbicacionGps.AjustesUbicacion }, fuente.ajustes);
            Assert.AreEqual("android.settings.LOCATION_SOURCE_SETTINGS", UbicacionGps.AjustesUbicacion);
        }

        [Test]
        public void GpsSeApagaLaUbicacionConLaAppAbierta()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            fuente.Leer(Usuario, 1.0);
            fuente.Rumbo = 90f;
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Activa, gps.Estado);

            fuente.UbicacionEncendida = false;
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Apagada, gps.Estado);
            Assert.IsNull(gps.Posicion);
            Assert.IsNull(gps.Rumbo);
            Assert.IsFalse(gps.Encendido);
        }

        [Test]
        public void GpsServicioFailedEsApagada()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            fuente.EstadoServicio = LocationServiceStatus.Failed;
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Apagada, gps.Estado);
            Assert.IsFalse(gps.Encendido);
        }

        [Test]
        public void GpsApagadaAlVolverConLaUbicacionEncendidaBuscaYLuegoActiva()
        {
            var fuente = new FuenteFalsa { TienePermiso = true, UbicacionEncendida = false };
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            Assert.AreEqual(EstadoUbicacion.Apagada, gps.Estado);

            gps.Detener();            // sale a encenderla
            fuente.UbicacionEncendida = true;
            gps.Iniciar();            // vuelve a la app
            Assert.AreEqual(EstadoUbicacion.Buscando, gps.Estado);
            Assert.IsTrue(gps.Encendido);
            fuente.Leer(Usuario, 5.0);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Activa, gps.Estado);
        }

        [Test]
        public void GpsDenegadoYConcedidoEnAjustesAlVolverBuscaYLuegoActiva()
        {
            var fuente = new FuenteFalsa();
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            gps.PedirPermiso();
            fuente.respuesta(false);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Denegado, gps.Estado);

            gps.Detener();               // sale a Ajustes
            fuente.TienePermiso = true;  // concede el permiso allí
            gps.Iniciar();               // vuelve a la app
            Assert.AreEqual(EstadoUbicacion.Buscando, gps.Estado);
            Assert.IsTrue(gps.Encendido);
            fuente.Leer(Usuario, 3.0);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Activa, gps.Estado);
        }

        // Brújula más calmada (24/11/2026): GPS con reloj falso a 60 fotogramas por segundo
        class Brujula
        {
            public readonly FuenteFalsa fuente = new FuenteFalsa { TienePermiso = true };
            public readonly UbicacionGps gps;
            public int avisos;
            double ahora = 100.0;

            public Brujula(float rumbo)
            {
                gps = new UbicacionGps(fuente, null, () => ahora);
                gps.Iniciar();
                fuente.Leer(Usuario, 1.0);
                Girar(rumbo, 1.0 / 60.0);
                gps.Cambiado += () => avisos++;
            }

            /// <summary>Deja la brújula en ese rumbo los segundos dados; devuelve el mayor salto entre fotogramas.</summary>
            public float Girar(float rumbo, double segundos)
            {
                float salto = 0f;
                int fotogramas = Mathf.RoundToInt((float)segundos * 60f);
                for (int i = 0; i < fotogramas; i++)
                {
                    var antes = gps.Rumbo;
                    Volver(rumbo, 1.0 / 60.0);
                    if (antes.HasValue) salto = Mathf.Max(salto, Mathf.Abs(Mathf.DeltaAngle(antes.Value, gps.Rumbo.Value)));
                }
                return salto;
            }

            /// <summary>Un solo fotograma, los segundos dados después del anterior.</summary>
            public void Volver(float rumbo, double segundos)
            {
                fuente.Rumbo = rumbo;
                ahora += segundos;
                gps.Actualizar();
            }
        }

        [Test]
        public void BrujulaPrimeraLecturaTalCualYNormalizada()
        {
            Assert.AreEqual(90f, new Brujula(90f).gps.Rumbo);
            Assert.AreEqual(350f, new Brujula(-10f).gps.Rumbo.Value, 0.001f);
        }

        [Test]
        public void BrujulaQuietaMientrasNoGireDiezGrados()
        {
            var b = new Brujula(90f);
            b.Girar(99f, 5.0);
            b.Girar(81f, 5.0);
            Assert.AreEqual(90f, b.gps.Rumbo, "9° a cada lado: el cono no se mueve.");
            Assert.AreEqual(0, b.avisos);
            b.Girar(102f, 2.0);
            Assert.AreEqual(102f, b.gps.Rumbo.Value, 1f, "12°: gira.");
        }

        [Test]
        public void BrujulaTemblorFuerteYCortoNoMueveElCono()
        {
            var b = new Brujula(90f);
            for (int i = 0; i < 60; i++)
            {
                b.Girar(105f, 1.0 / 60.0);
                b.Girar(75f, 1.0 / 60.0);
            }
            Assert.AreEqual(90f, b.gps.Rumbo, "Temblor de ±15° de un fotograma a otro: se quita antes de mirar los 10°.");
            Assert.AreEqual(0, b.avisos);
        }

        [Test]
        public void BrujulaGiroDeVerdadSuaveYSinPasarse()
        {
            var b = new Brujula(90f);
            float salto = b.Girar(180f, 0.5);
            Assert.Greater(b.gps.Rumbo.Value, 150f, "A medio segundo ha hecho casi todo el giro.");
            Assert.Less(salto, 8f, "90° sin saltos: menos de 8° por fotograma.");
            float maximo = 0f;
            b.gps.Cambiado += () => maximo = Mathf.Max(maximo, b.gps.Rumbo.Value);
            Assert.Less(b.Girar(180f, 1.5), 8f);
            Assert.AreEqual(180f, b.gps.Rumbo.Value, 1f);
            Assert.LessOrEqual(maximo, 180f, "No se pasa.");

            // Y vuelve a quedarse quieto
            b.avisos = 0;
            float final = b.gps.Rumbo.Value;
            b.Girar(188f, 3.0);
            Assert.AreEqual(final, b.gps.Rumbo.Value);
            Assert.AreEqual(0, b.avisos);
        }

        // Balanceo de la mano al andar (un paso por segundo y dos) y temblor más rápido, recién terminado un giro
        [TestCase(1.0, 6f)]
        [TestCase(2.0, 8f)]
        [TestCase(5.0, 15f)]
        public void BrujulaBalanceoTrasUnGiroSeQuedaQuieta(double hercios, float grados)
        {
            var b = new Brujula(90f);
            int n = 0;
            void Balancear(float centro, int fotogramas)
            {
                for (int i = 0; i < fotogramas; i++, n++)
                    b.Girar(centro + grados * Mathf.Sin((float)(2.0 * Math.PI * hercios * n / 60.0)), 1.0 / 60.0);
            }
            Balancear(180f, 180); // gira 90° balanceándose y le da tiempo a pararse
            float parado = b.gps.Rumbo.Value;
            Assert.AreEqual(180f, parado, 10f);
            b.avisos = 0;
            Balancear(180f, 600);
            Assert.AreEqual(parado, b.gps.Rumbo.Value);
            Assert.AreEqual(0, b.avisos, "Diez segundos andando recto: el cono no se mueve.");
        }

        [TestCase(5)]
        [TestCase(15)]
        [TestCase(30)]
        [TestCase(120)]
        public void BrujulaGiraIgualConOtrosFotogramasPorSegundo(int fps)
        {
            var b = new Brujula(90f);
            for (int i = 0; i < fps; i++) b.Volver(180f, 1.0 / fps);
            Assert.AreEqual(180f, b.gps.Rumbo.Value, 3f, "Un segundo después ha llegado.");
        }

        [Test]
        public void BrujulaGiraPorElLadoCortoAlPasarPorElNorte()
        {
            var b = new Brujula(350f);
            bool fuera = false;
            b.gps.Cambiado += () => { float r = b.gps.Rumbo.Value; if (r > 21f && r < 349f) fuera = true; };
            b.Girar(20f, 2.0);
            Assert.AreEqual(20f, b.gps.Rumbo.Value, 1f);
            b.Girar(350f, 2.0);
            Assert.AreEqual(0f, Mathf.DeltaAngle(350f, b.gps.Rumbo.Value), 1f);
            Assert.IsFalse(fuera, "De 350° a 20° y vuelta por el norte, no por el sur.");
        }

        [Test]
        public void BrujulaAlVolverDeLaPausaApuntaYaAlRumboNuevo()
        {
            var b = new Brujula(90f);
            b.Volver(200f, 30.0);
            Assert.AreEqual(200f, b.gps.Rumbo.Value, 0.5f);
        }

        [Test]
        public void BrujulaRelojParadoOHaciaAtrasNoMueveNiRompe()
        {
            var b = new Brujula(90f);
            b.Volver(200f, 0.0);
            b.Volver(200f, -5.0);
            Assert.AreEqual(90f, b.gps.Rumbo);
            b.Girar(200f, 3.0);
            Assert.AreEqual(200f, b.gps.Rumbo.Value, 1f);
        }

        [Test]
        public void BrujulaLecturaImposibleNoSeQueda()
        {
            var b = new Brujula(90f);
            b.Girar(float.NaN, 0.5);
            Assert.AreEqual(90f, b.gps.Rumbo);
            b.Girar(200f, 2.0);
            Assert.AreEqual(200f, b.gps.Rumbo.Value, 1f);

            b.fuente.Rumbo = null;
            b.gps.Actualizar();
            b.Volver(float.NaN, 0.1);
            Assert.IsNull(b.gps.Rumbo, "Sin rumbo antes: sigue sin cono, no apunta al norte.");
        }

        [Test]
        public void BrujulaSinLecturaSoloElCirculoYDespuesEmpiezaDeNuevo()
        {
            var b = new Brujula(90f);
            b.fuente.Rumbo = null;
            b.gps.Actualizar();
            Assert.IsNull(b.gps.Rumbo, "Sin brújula: solo el círculo.");
            b.Volver(95f, 1.0 / 60.0);
            Assert.AreEqual(95f, b.gps.Rumbo, "La primera lectura tras no tener rumbo sale tal cual, aunque esté a 5°.");
        }

        // ───────────────────────── Historia 2.1: GPS durante una ruta ─────────────────────────

        /// <summary>Servicio de ruta falso: cuenta arranques y paradas y tiene la cola como texto, igual que el plugin.</summary>
        class ServicioRutaFalso : IServicioRuta
        {
            public bool NecesitaPermisoNotificaciones { get; set; }
            public Action<bool> respuesta;
            public int peticiones, paradas;
            public readonly List<string> inicios = new List<string>();
            public readonly List<string> textos = new List<string>();
            public bool enMarcha;
            public bool EnMarcha => enMarcha;
            readonly List<string> cola = new List<string>();

            public void PedirPermisoNotificaciones(Action<bool> r) { peticiones++; respuesta = r; }
            public void Iniciar(string texto) { inicios.Add(texto); enMarcha = true; }
            public void ActualizarTexto(string texto) => textos.Add(texto);
            public void Parar() { paradas++; enMarcha = false; cola.Clear(); }

            public string VaciarCola()
            {
                var texto = string.Join("\n", cola);
                cola.Clear();
                return texto;
            }

            /// <summary>El servicio recibe una posición del GPS (solo si está en marcha, como el de Android).</summary>
            public void Posicion(double lat, double lon, long horaMs)
            {
                if (!enMarcha) return;
                cola.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:R};{1:R};90.0;1.40;5.0;{2}", lat, lon, horaMs));
            }
        }

        const string TextoRuta = "Navegando a Clínica Areas · 6 min";

        /// <summary>GPS con permiso, encendido y con la app delante, con un servicio de ruta falso.</summary>
        static UbicacionGps GpsConRuta(FuenteFalsa fuente, ServicioRutaFalso servicio)
        {
            var gps = new UbicacionGps(fuente, servicio);
            gps.Iniciar();
            fuente.Leer(Usuario, 1.0);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Activa, gps.Estado);
            return gps;
        }

        [Test]
        public void RutaArrancaElServicioConLaNotificacionYLasPosicionesLleganAlPuerto()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso();
            var gps = GpsConRuta(fuente, servicio);

            var r = gps.IniciarRuta(TextoRuta);
            Assert.IsTrue(r.Ok);
            Assert.IsTrue(r.Valor, "Con permisos el GPS en segundo plano ya está en marcha.");
            Assert.IsTrue(gps.RutaActiva);
            Assert.IsTrue(gps.ServicioRutaActivo);
            Assert.IsFalse(gps.RutaSoloPrimerPlano);
            CollectionAssert.AreEqual(new[] { TextoRuta }, servicio.inicios, "Notificación fija con el texto de Unity.");
            Assert.AreEqual(0, servicio.peticiones, "Con el permiso de notificaciones ya concedido no se pide.");

            var vistas = new List<Coordenada?>();
            gps.Cambiado += () => vistas.Add(gps.Posicion);
            var a = new Coordenada(36.7781, -6.3530);
            servicio.Posicion(a.Latitud, a.Longitud, 1000);
            gps.Actualizar();
            Assert.AreEqual(a, gps.Posicion);
            Assert.AreEqual(EstadoUbicacion.Activa, gps.Estado);
            CollectionAssert.AreEqual(new Coordenada?[] { a }, vistas);

            // Con el servicio, las lecturas de Unity no se mezclan con las suyas
            fuente.Leer(new Coordenada(1, 1), 2.0);
            gps.Actualizar();
            Assert.AreEqual(a, gps.Posicion);

            gps.ActualizarNotificacion("Navegando a Clínica Areas · 5 min");
            CollectionAssert.AreEqual(new[] { "Navegando a Clínica Areas · 5 min" }, servicio.textos);
            Assert.IsTrue(gps.IniciarRuta("Navegando a Clínica Areas · 4 min").Valor, "Otra vez IniciarRuta solo cambia el texto.");
            Assert.AreEqual(1, servicio.inicios.Count);
            Assert.AreEqual(2, servicio.textos.Count);
        }

        [Test]
        public void PausaConRutaNoParaElServicioYAlVolverLlegaLaColaEnOrden()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso();
            var gps = GpsConRuta(fuente, servicio);
            var ubicacion = new ServicioUbicacion(gps, new PreferenciasFalsas());
            ubicacion.Iniciar();
            Assert.IsTrue(ubicacion.IniciarRuta(TextoRuta).Ok);
            Assert.IsTrue(ubicacion.RutaActiva);

            ubicacion.Pausar(); // pantalla apagada (Arranque.OnApplicationPause)
            Assert.IsTrue(ubicacion.Pausada);
            Assert.IsTrue(gps.ServicioRutaActivo, "Con ruta, la pausa no para el GPS del servicio.");
            Assert.AreEqual(0, servicio.paradas);
            Assert.AreEqual(1, fuente.apagados, "Unity apaga lo suyo: en segundo plano el GPS es el servicio.");

            // 2 min con la pantalla apagada: Unity no corre y el servicio acumula
            var puntos = new List<Coordenada>();
            for (int i = 0; i < 120; i++)
            {
                var c = new Coordenada(36.7781 + i * 0.00001, -6.3530);
                puntos.Add(c);
                servicio.Posicion(c.Latitud, c.Longitud, 1000 + i * 1000);
            }

            var vistas = new List<Coordenada>();
            ubicacion.Cambiado += () => { if (ubicacion.Posicion.HasValue) vistas.Add(ubicacion.Posicion.Value); };
            ubicacion.Reanudar(); // vuelve a la app
            gps.Actualizar();
            CollectionAssert.AreEqual(puntos, vistas, "El puerto recibe las posiciones acumuladas, en orden.");
            Assert.AreEqual(puntos[puntos.Count - 1], ubicacion.Posicion);
            Assert.IsTrue(gps.Encendido, "Al volver se enciende otra vez la ubicación de Unity (y la brújula).");

            vistas.Clear();
            gps.RecogerRuta(); // aviso del plugin sin nada nuevo
            gps.Actualizar();
            CollectionAssert.IsEmpty(vistas, "Cada posición llega una sola vez.");
        }

        [Test]
        public void TerminarRutaParaElServicioYNoLleganMasPosiciones()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso();
            var gps = GpsConRuta(fuente, servicio);
            var ubicacion = new ServicioUbicacion(gps, new PreferenciasFalsas());
            ubicacion.Iniciar();
            ubicacion.IniciarRuta(TextoRuta);
            ubicacion.Pausar();

            ubicacion.TerminarRuta(); // p. ej. al llegar, con la app detrás
            Assert.IsFalse(ubicacion.RutaActiva);
            Assert.IsFalse(gps.ServicioRutaActivo);
            Assert.AreEqual(1, servicio.paradas, "La notificación se va con el servicio.");
            Assert.IsFalse(gps.Encendido, "Terminada en segundo plano: GPS apagado (NFR-3).");

            servicio.Posicion(36.0, -6.0, 5000); // el servicio ya no está: no hay posiciones
            var antes = gps.Posicion;
            ubicacion.Reanudar();
            gps.Actualizar();
            Assert.AreEqual(antes, gps.Posicion);

            ubicacion.TerminarRuta(); // sin ruta: nada
            Assert.AreEqual(1, servicio.paradas);
            gps.ActualizarNotificacion("x"); // sin ruta: nada
            CollectionAssert.IsEmpty(servicio.textos);
        }

        [Test]
        public void PausaSinRutaApagaElGpsComoEn17()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso();
            var gps = GpsConRuta(fuente, servicio);
            var ubicacion = new ServicioUbicacion(gps, new PreferenciasFalsas());
            ubicacion.Iniciar();
            ubicacion.Pausar();
            Assert.IsFalse(gps.Encendido);
            Assert.AreEqual(1, fuente.apagados);
            CollectionAssert.IsEmpty(servicio.inicios, "Sin ruta no hay GPS en segundo plano.");
            Assert.IsFalse(ubicacion.RutaActiva);
        }

        [Test]
        public void SinPermisoDeUbicacionIniciarRutaDevuelveErrorYNoArrancaNada()
        {
            var fuente = new FuenteFalsa();
            var servicio = new ServicioRutaFalso { NecesitaPermisoNotificaciones = true };
            var gps = new UbicacionGps(fuente, servicio);
            gps.Iniciar();
            gps.PedirPermiso();
            fuente.respuesta(false);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Denegado, gps.Estado);

            var r = gps.IniciarRuta(TextoRuta);
            Assert.IsFalse(r.Ok);
            Assert.AreEqual(UbicacionGps.ErrorSinPermiso, r.Error);
            Assert.IsFalse(gps.RutaActiva);
            CollectionAssert.IsEmpty(servicio.inicios);
            Assert.AreEqual(0, servicio.peticiones, "Ni siquiera se pide el permiso de notificaciones.");
            Assert.AreEqual(0, fuente.encendidos);
        }

        [Test]
        public void Android13PidePermisoDeNotificacionesAntesDeArrancar()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso { NecesitaPermisoNotificaciones = true };
            var gps = GpsConRuta(fuente, servicio);

            var r = gps.IniciarRuta(TextoRuta);
            Assert.IsTrue(r.Ok);
            Assert.IsFalse(r.Valor, "Hasta la respuesta, solo con la app delante.");
            Assert.AreEqual(1, servicio.peticiones);
            CollectionAssert.IsEmpty(servicio.inicios, "No se arranca antes de pedir el permiso.");

            // El diálogo quita el foco (pausa); al volver con el permiso concedido arranca el servicio
            gps.Detener();
            servicio.respuesta(true); // desde el hilo de Java
            CollectionAssert.IsEmpty(servicio.inicios, "La respuesta se aplica en el siguiente fotograma.");
            gps.Iniciar();
            gps.Actualizar();
            CollectionAssert.AreEqual(new[] { TextoRuta }, servicio.inicios);
            Assert.IsTrue(gps.ServicioRutaActivo);
            Assert.IsFalse(gps.RutaSoloPrimerPlano);
        }

        [Test]
        public void NotificacionesDenegadasLaRutaSigueSoloConLaAppDelante()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso { NecesitaPermisoNotificaciones = true };
            var gps = GpsConRuta(fuente, servicio);
            int avisos = 0;
            gps.Cambiado += () => avisos++;
            gps.IniciarRuta(TextoRuta);
            servicio.respuesta(false);
            gps.Actualizar();
            Assert.IsTrue(gps.RutaActiva);
            Assert.IsTrue(gps.RutaSoloPrimerPlano);
            Assert.IsFalse(gps.ServicioRutaActivo);
            CollectionAssert.IsEmpty(servicio.inicios);
            Assert.AreEqual(1, avisos, "La UI se entera por Cambiado.");

            // Con la app delante siguen llegando las posiciones de Unity
            var b = new Coordenada(36.779, -6.351);
            fuente.Leer(b, 2.0);
            gps.Actualizar();
            Assert.AreEqual(b, gps.Posicion);

            // Sin servicio no hay GPS en segundo plano
            gps.Detener();
            Assert.IsFalse(gps.Encendido);

            gps.TerminarRuta();
            Assert.IsFalse(gps.RutaSoloPrimerPlano);
            Assert.AreEqual(0, servicio.paradas, "No había servicio que parar.");
        }

        [Test]
        public void FueraDeAndroidLaRutaMantieneLaUbicacionDeUnity()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var gps = new UbicacionGps(fuente); // editor y PC: sin servicio
            gps.Iniciar();
            var r = gps.IniciarRuta(TextoRuta);
            Assert.IsTrue(r.Ok);
            Assert.IsFalse(r.Valor);
            Assert.IsTrue(gps.RutaActiva);
            Assert.IsFalse(gps.RutaSoloPrimerPlano, "En PC no hay aviso de notificaciones.");
            gps.Detener();
            Assert.IsTrue(gps.Encendido, "Durante la ruta, la pausa no apaga la ubicación de Unity.");
            Assert.AreEqual(0, fuente.apagados);
            gps.TerminarRuta();
            Assert.IsFalse(gps.Encendido, "Terminada con la app detrás: GPS apagado.");
        }

        [Test]
        public void ColaDelServicioSeLeeEnOrdenYSaltaLasLineasDanadas()
        {
            var texto = "36.7781000;-6.3530000;90.5;1.40;5.0;1000\n" +
                        "basura\n" +
                        "36.7782;-6.3531;;;;2000\n" +
                        "999;0;;;;3000\n" +
                        "36,7;-6,3;;;;4000\n" +
                        "36.7783;-6.3532;10;0.5;3;5000";
            var l = LecturaRuta.Leer(texto);
            Assert.AreEqual(3, l.Count);
            Assert.AreEqual(new Coordenada(36.7781, -6.353), l[0].Posicion);
            Assert.AreEqual(90.5f, l[0].RumboMarcha);
            Assert.AreEqual(1.4f, l[0].Velocidad);
            Assert.AreEqual(5f, l[0].Precision);
            Assert.AreEqual(1000L, l[0].HoraMs);
            Assert.IsNull(l[1].RumboMarcha);
            Assert.IsNull(l[1].Velocidad);
            Assert.IsNull(l[1].Precision);
            Assert.AreEqual(5000L, l[2].HoraMs);
            CollectionAssert.IsEmpty(LecturaRuta.Leer(""));
            CollectionAssert.IsEmpty(LecturaRuta.Leer(null));
        }

        [Test]
        public void ElAvisoDeUbicacionPliegaLaHojaDelBuscadorYQuedaSoloElEnTalkBack()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Apagada, null);
            var m = Pantalla(gps);
            var hoja = m.pantalla.Buscador;
            hoja.Medir(800f);
            hoja.Poner(0.5f);
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(m.pantalla.Accesibilidad.Modal, "La hoja desplegada es modal.");

            m.pantalla.TocarUbicacion(); // aviso de ubicación apagada
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            Assert.AreEqual(0f, hoja.Fraccion, "La ventana pliega la hoja.");
            Assert.IsTrue(m.pantalla.Accesibilidad.Modal);
            CollectionAssert.DoesNotContain(m.pantalla.Accesibilidad.Recorrido(), PrepararInterfaz.TextosEs[ClavesTexto.BuscadorCampo]);
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(m.pantalla.AvisoAbierto, "La hoja plegada no le quita el modo modal.");

            m.pantalla.CerrarAviso();
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(m.pantalla.Accesibilidad.Modal, "Al cerrarla, la hoja (plegada) no vuelve a ponerlo.");
        }

        [Test]
        public void SinNotificacionesSaleElAvisoUnaVezPorRuta()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Activa, Usuario);
            var m = Pantalla(gps);
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(m.pantalla.AvisoAbierto);

            gps.RutaActiva = true;
            gps.RutaSoloPrimerPlano = true; // Android 13 con POST_NOTIFICATIONS denegado
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            Assert.IsTrue(m.pantalla.Aviso.SinNotificaciones);
            Assert.AreEqual("Sin permiso de notificaciones, la navegación se pausa al apagar la pantalla.", m.pantalla.Aviso.Texto.text);
            Assert.IsTrue(m.pantalla.Accesibilidad.Modal);
            CollectionAssert.AreEqual(RecorridoAviso(ClavesTexto.UbicacionAvisoNotificaciones), m.pantalla.Accesibilidad.Recorrido());

            m.pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(m.pantalla.AvisoAbierto, "Con la ubicación activa el aviso de notificaciones no se cierra solo.");
            m.pantalla.CerrarAviso();
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(m.pantalla.AvisoAbierto, "Sale una sola vez en la misma ruta.");

            gps.TerminarRuta();
            m.pantalla.ActualizarCapas(Escala);
            gps.RutaActiva = true;
            gps.RutaSoloPrimerPlano = true; // otra ruta
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(m.pantalla.AvisoAbierto, "En otra ruta vuelve a salir.");
            m.pantalla.AbrirAjustesUbicacion();
            Assert.AreEqual(1, gps.ajustes, "\"Abrir Ajustes\" lleva a los de la app (notificaciones).");
            Assert.IsFalse(m.pantalla.AvisoAbierto);
        }

        [Test]
        public void AvisoDeNotificacionesEsperaAlDeUbicacionYSeCierraAlTerminarLaRuta()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Apagada, null);
            var m = Pantalla(gps);
            m.pantalla.ActualizarCapas(Escala);
            m.pantalla.TocarUbicacion(); // aviso de ubicación apagada
            Assert.IsTrue(m.pantalla.AvisoAbierto);
            Assert.IsFalse(m.pantalla.Aviso.SinNotificaciones);

            gps.RutaActiva = true;
            gps.RutaSoloPrimerPlano = true;
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(m.pantalla.Aviso.SinNotificaciones, "No tapa el aviso de ubicación.");
            m.pantalla.CerrarAviso();
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(m.pantalla.AvisoAbierto, "Sale al cerrarse el otro.");
            Assert.IsTrue(m.pantalla.Aviso.SinNotificaciones);

            gps.RutaSoloPrimerPlano = false; // la ruta termina o se da el permiso
            m.pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(m.pantalla.AvisoAbierto, "El aviso ya no es verdad: se cierra.");
        }

        [Test]
        public void QuitarElPermisoDeUbicacionDuranteLaRutaLaTermina()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso();
            var gps = GpsConRuta(fuente, servicio);
            var ubicacion = new ServicioUbicacion(gps, new PreferenciasFalsas());
            ubicacion.Iniciar();
            ubicacion.IniciarRuta(TextoRuta);
            ubicacion.Pausar();
            fuente.TienePermiso = false; // quitado desde Ajustes con la app detrás
            ubicacion.Reanudar();
            Assert.AreEqual(1, servicio.paradas, "Se para el servicio y se va la notificación.");
            Assert.IsFalse(gps.RutaActiva);
            Assert.IsFalse(gps.ServicioRutaActivo);
            Assert.AreEqual(EstadoUbicacion.SinPermiso, gps.Estado);
        }

        [Test]
        public void SiElServicioNoSigueVuelvenLasLecturasDeUnity()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso();
            var gps = GpsConRuta(fuente, servicio);
            gps.IniciarRuta(TextoRuta);
            servicio.enMarcha = false; // no pudo arrancar en primer plano o el sistema lo paró
            var b = new Coordenada(36.779, -6.351);
            fuente.Leer(b, 2.0);
            gps.Actualizar();
            Assert.AreEqual(b, gps.Posicion, "La posición no se congela.");
        }

        [Test]
        public void NotificacionesConcedidasDesdeAjustesArrancanElServicioAlVolver()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso { NecesitaPermisoNotificaciones = true };
            var gps = GpsConRuta(fuente, servicio);
            gps.IniciarRuta(TextoRuta);
            servicio.respuesta(false);
            gps.Actualizar();
            Assert.IsTrue(gps.RutaSoloPrimerPlano);

            gps.Detener(); // "Abrir Ajustes"
            servicio.NecesitaPermisoNotificaciones = false; // lo concede allí
            gps.Iniciar(); // vuelve a la app
            CollectionAssert.AreEqual(new[] { TextoRuta }, servicio.inicios);
            Assert.IsTrue(gps.ServicioRutaActivo);
            Assert.IsFalse(gps.RutaSoloPrimerPlano);
        }

        [Test]
        public void ConLaUbicacionDelMovilApagadaLaColaViejaNoDaPosicion()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var servicio = new ServicioRutaFalso();
            var gps = GpsConRuta(fuente, servicio);
            gps.IniciarRuta(TextoRuta);
            gps.Detener();
            servicio.Posicion(36.0, -6.0, 5000); // antes de apagar la ubicación del móvil
            fuente.UbicacionEncendida = false;
            gps.Iniciar();
            Assert.AreEqual(EstadoUbicacion.Apagada, gps.Estado);
            gps.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Apagada, gps.Estado, "No vuelve a Activa con posiciones viejas.");
            Assert.IsNull(gps.Posicion);
        }
    }
}
