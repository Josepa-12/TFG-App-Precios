using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    /// Historia 1.7 (parte 1): la posición del usuario en el mapa, el botón de ubicación y la explicación del
    /// permiso. Cada fila de la matriz de la spec, siempre con una IUbicacion falsa: ninguna prueba lee el
    /// GPS ni la brújula reales (Input.location, Input.compass).
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
            public int permisos, inicios, detenciones;
            public void PedirPermiso() => permisos++;
            public void Iniciar() => inicios++;
            public void Detener() => detenciones++;

            public void Poner(EstadoUbicacion estado, Coordenada? posicion = null, float? rumbo = null)
            {
                Estado = estado;
                Posicion = posicion;
                Rumbo = rumbo;
                Cambiado?.Invoke();
            }
        }

        /// <summary>Proyecta cualquier coordenada en el mismo punto, salvo que se diga otra cosa.</summary>
        class MapaFalso : IMapa
        {
            public readonly List<Coordenada> centrados = new List<Coordenada>();
            public PuntoPantalla proyeccion = new PuntoPantalla(500f, 900f, 300f);
            public float giro;
            public void Centrar(Coordenada destino) => centrados.Add(destino);
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => proyeccion;
            public IReadOnlyList<PuntoInteres> PuntosInteres { get; } = new PuntoInteres[0];
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => giro;
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
        public void DenegadoElMapaSigueEnLaVistaInicialSinCirculoYTocarNoHaceNada()
        {
            var gps = new UbicacionFalsa();
            gps.Poner(EstadoUbicacion.Denegado);
            var m = Pantalla(gps);
            Assert.IsFalse(m.pantalla.ExplicacionAbierta, "Denegado no es SinPermiso: la explicación no sale al abrir.");
            Actualizar(m);
            Assert.IsFalse(m.pantalla.MarcadorPosicion.Visible);
            m.pantalla.TocarUbicacion();
            CollectionAssert.AreEqual(new[] { PantallaMapa.VistaInicial }, m.mapa.centrados);
            Assert.IsFalse(m.pantalla.ExplicacionAbierta, "El aviso de denegado es de la parte 2.");
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
    }
}
