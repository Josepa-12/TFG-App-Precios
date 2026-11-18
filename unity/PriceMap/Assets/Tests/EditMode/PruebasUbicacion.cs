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
            public float? Rumbo { get; set; }
            public event Action Cambiado;
            public int permisos, inicios, detenciones, ajustes;
            public void PedirPermiso() => permisos++;
            public void Iniciar() => inicios++;
            public void Detener() => detenciones++;
            public void AbrirAjustes() => ajustes++;

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
            public event System.Action<PuntoPantalla> Toque { add { } remove { } }
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

        static PreferenciasFalsas YaExplicado()
        {
            var p = new PreferenciasFalsas();
            p.Guardar(ServicioUbicacion.ClaveExplicacion, "1");
            return p;
        }

        static void Actualizar(Montaje m) =>
            m.pantalla.MarcadorPosicion.Actualizar(m.mapa, m.servicio.Posicion, m.servicio.Rumbo, Escala, AnchoPx, AltoPx);

        static List<string> RecorridoExplicacion() => new List<string>
        {
            TextoEs(ClavesTexto.UbicacionExplicacion), TextoEs(ClavesTexto.UbicacionAhoraNo), TextoEs(ClavesTexto.UbicacionPermitir),
        };

        // ───────────────────────── Explicación del permiso ─────────────────────────

        [Test]
        public void PrimeraAperturaSaleLaExplicacionYQuedaGuardadoQueSalio()
        {
            var m = Pantalla(new UbicacionFalsa());
            Assert.IsTrue(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(DisplayStyle.Flex, m.pantalla.Explicacion.style.display.value);
            Assert.IsNotNull(m.prefs.Leer(ServicioUbicacion.ClaveExplicacion), "Queda guardado que la explicación salió.");
            Assert.AreEqual(TextoEs(ClavesTexto.UbicacionExplicacion), m.pantalla.Explicacion.Texto.text);
            Assert.AreEqual("Permitir", m.pantalla.Explicacion.Permitir.text);
            Assert.AreEqual("Ahora no", m.pantalla.Explicacion.AhoraNo.text);
            Assert.AreEqual(0, m.gps.permisos, "El permiso de Android no se pide hasta pulsar \"Permitir\".");
        }

        [Test]
        public void ConLaExplicacionAbiertaTalkBackEsModalYElMapaNoSeMueve()
        {
            var m = Pantalla(new UbicacionFalsa());
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
            var m = Pantalla(new UbicacionFalsa()); // sin preferencia guardada: la explicación sale al abrir
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
            var m = Pantalla(new UbicacionFalsa(), YaExplicado());
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(DisplayStyle.None, m.pantalla.Explicacion.style.display.value);
            Assert.IsFalse(m.pantalla.Accesibilidad.Modal);
        }

        [Test]
        public void AhoraNoYTocarElBotonVuelveASalirLaExplicacion()
        {
            var m = Pantalla(new UbicacionFalsa());
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
            m.pantalla.PermitirUbicacion();
            Assert.IsFalse(m.pantalla.ExplicacionAbierta);
            Assert.AreEqual(1, m.gps.permisos);
        }

        [Test]
        public void AtrasConLaExplicacionAbiertaEsAhoraNo()
        {
            var m = Pantalla(new UbicacionFalsa());
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
            var m = Pantalla(gps, YaExplicado());
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

        // ───────────────────────── Botón de ubicación ─────────────────────────

        [Test]
        public void ElBotonVaEnLaPilaDerechaDebajoDelDeTemaConSuTextoParaTalkBack()
        {
            var m = Pantalla(new UbicacionFalsa(), YaExplicado());
            var boton = m.pantalla.BotonUbicacion;
            Assert.IsNotNull(boton);
            Assert.AreEqual(Icono.Diana, boton.Icono);
            Assert.AreEqual("hueco-ubicacion", boton.parent.name);
            var pila = m.arbol.Q("pila-derecha");
            Assert.AreSame(pila, boton.parent.parent);
            Assert.AreEqual(pila.IndexOf(m.arbol.Q("hueco-tema")) + 1, pila.IndexOf(boton.parent), "Justo debajo del botón de tema.");
            Assert.IsTrue(PantallaMapa.EsControl(m.arbol.Q("raiz"), boton), "El botón tapa el mapa al pulsarlo.");

            var recorrido = m.pantalla.Accesibilidad.Recorrido();
            Assert.AreEqual("Centrar en mi posición", TextoEs(ClavesTexto.UbicacionCentrar));
            Assert.AreEqual(recorrido.IndexOf(TextoEs(ClavesTexto.TemaCambiar)) + 1, recorrido.IndexOf(TextoEs(ClavesTexto.UbicacionCentrar)),
                "TalkBack lo lee después del botón de tema.");
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
            var m = Pantalla(gps, YaExplicado());
            m.mapa.giro = giro;
            m.mapa.proyecciones[Lejos] = new PuntoPantalla(900f, 900f, 300f);
            m.mapa.proyecciones[Arrastrado] = new PuntoPantalla(600f, 900f, 300f); // 100 px = 50 dp
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
            var m = Pantalla(gps, YaExplicado());
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
            var m = Pantalla(gps, YaExplicado());
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

        [Test]
        public void GpsRumboConUmbralDe1Grado()
        {
            var fuente = new FuenteFalsa { TienePermiso = true };
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            fuente.Leer(Usuario, 1.0);
            fuente.Rumbo = 90f;
            gps.Actualizar();
            Assert.AreEqual(90f, gps.Rumbo);

            int avisos = 0;
            gps.Cambiado += () => avisos++;
            fuente.Rumbo = 90.6f; // tiembla menos de 1°: no se avisa
            gps.Actualizar();
            Assert.AreEqual(90f, gps.Rumbo);
            Assert.AreEqual(0, avisos);
            fuente.Rumbo = 92f;
            gps.Actualizar();
            Assert.AreEqual(92f, gps.Rumbo);
            Assert.AreEqual(1, avisos);
            fuente.Rumbo = -10f; // se normaliza a [0, 360)
            gps.Actualizar();
            Assert.AreEqual(350f, gps.Rumbo.Value, 0.001f);
            fuente.Rumbo = null; // sin brújula: solo el círculo
            gps.Actualizar();
            Assert.IsNull(gps.Rumbo);
        }
    }
}
