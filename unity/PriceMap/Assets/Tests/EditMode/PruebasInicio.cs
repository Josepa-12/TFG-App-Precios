using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using PriceMap.Adaptadores.Sistema;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Pantalla de inicio (spec pantalla-de-inicio): cuándo se va (ServicioInicio y la pantalla del mapa con un reloj y una
    /// pantalla falsos), el mapa que se acerca y su vista cargada, los recursos que van a Android, los ajustes sin el logo
    /// de Unity y la lógica de Java, leyendo el código (en el editor no hay Android).
    /// </summary>
    public class PruebasInicio
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Usuario = new Coordenada(36.7790, -6.3540);
        static readonly Coordenada OtroSitio = new Coordenada(36.7700, -6.3500);
        static readonly PuntoPantalla PuntoUsuario = new PuntoPantalla(206f, 457f, 900f);
        const float Escala = 1f;

        // ───────────────────────── Dobles ─────────────────────────

        class PantallaFalsa : IPantallaInicio
        {
            public double? Segundos { get; set; } = 0;
            public readonly List<PuntoPantalla?> Idas = new List<PuntoPantalla?>();
            public List<string> Orden = new List<string>();
            public void Irse(PuntoPantalla? destino) { Idas.Add(destino); Orden.Add("irse"); }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class AccesibilidadFalsa : IAccesibilidadSistema { public bool AnimacionesQuitadas { get; set; } }

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; }
            public Coordenada? Posicion { get; set; }
            public Coordenada? UltimaConocida { get; set; }
            public float? Rumbo => null;
            public event Action Cambiado { add { } remove { } }
            public int Permisos;
            public void PedirPermiso() => Permisos++;
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
            public bool RutaActiva => false;
            public bool RutaSoloPrimerPlano => false;
            public Resultado<bool> IniciarRuta(string textoNotificacion) => Resultado<bool>.Exito(false);
            public void ActualizarNotificacion(string textoNotificacion) { }
            public void TerminarRuta() { }
        }

        class MapaFalso : IMapa
        {
            public readonly List<string> Orden = new List<string>();
            bool cargada;
            public bool VistaCargada { get { Orden.Add("vista"); return cargada; } set => cargada = value; }
            public readonly List<Coordenada> Centrados = new List<Coordenada>();
            public readonly List<(float factor, float segundos)> Acercamientos = new List<(float, float)>();
            public void AcercarDesde(float factor, float segundos) { Acercamientos.Add((factor, segundos)); Orden.Add("acercar"); }
            public void Centrar(Coordenada destino) { Centrados.Add(destino); Orden.Add("centrar"); }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) => Centrados.Add(destino);
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) { Orden.Add("proyectar"); return punto == Usuario ? PuntoUsuario : PuntoPantalla.Ninguno; }
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => Usuario;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
        }

        class ResolutorSinRespuesta : IResolutorEnlaces
        {
            public int Peticiones;
            public Task<Resultado<string>> Resolver(string url, CancellationToken ct)
            {
                Peticiones++;
                return new TaskCompletionSource<Resultado<string>>().Task;
            }
        }

        class Repositorio : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(Array.Empty<Lugar>());
        }

        // ───────────────────────── Pantalla del mapa ─────────────────────────

        readonly List<GameObject> objetos = new List<GameObject>();
        SynchronizationContext contextoAntes;
        PantallaFalsa nativa;
        UbicacionFalsa gps;
        MapaFalso mapa;
        PantallaMapa pantalla;
        AccesibilidadFalsa accesibilidad;
        double reloj;

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            Textos.Forzados = clave => PrepararInterfaz.TextosEs[clave];
            nativa = new PantallaFalsa();
            gps = new UbicacionFalsa { Estado = EstadoUbicacion.Buscando };
            mapa = new MapaFalso();
            nativa.Orden = mapa.Orden;
            accesibilidad = new AccesibilidadFalsa();
            reloj = 100;
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (var o in objetos) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            objetos.Clear();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t != null && t.name.StartsWith("Tesela simulada")) UnityEngine.Object.DestroyImmediate(t.gameObject);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        ServicioInicio Servicio() => new ServicioInicio(nativa, () => reloj);

        ServicioInicio Crear(bool conInicio = true, IResolutorEnlaces resolutor = null)
        {
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            pantalla = objeto.AddComponent<PantallaMapa>();
            pantalla.RelojCamara = () => Ahora.AddSeconds(reloj);
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var lugares = new ServicioLugares(new Repositorio());
            lugares.Cargar();
            var inicio = conInicio ? Servicio() : null;
            pantalla.Inicializar(mapa, tema, lugares, Ahora, arbol, filtro, new ServicioUbicacion(gps, prefs), accesibilidad,
                new ServicioBusqueda(lugares, mapa, Ahora), resolutorEnlaces: resolutor, inicio: inicio);
            tema.Iniciar();
            filtro.Iniciar();
            pantalla.Buscador.Medir(800f);
            return inicio;
        }

        /// <summary>Un fotograma de la pantalla del mapa, esos segundos después (también para el pin).</summary>
        void Fotograma(double segundos = 0)
        {
            reloj += segundos;
            if (nativa.Segundos.HasValue) nativa.Segundos += segundos;
            pantalla.ActualizarCapas(Escala);
        }

        // ───────────────────────── Matriz ─────────────────────────

        [Test]
        public void ConGps_PosicionYTeselasALos2Segundos_SeVaYElPinAcabaEnElUsuario()
        {
            var inicio = Crear();
            Fotograma(1.5);
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Usuario;
            Fotograma();
            Assert.IsEmpty(nativa.Idas, "Faltan las teselas.");
            mapa.VistaCargada = true;
            Fotograma(0.5);
            CollectionAssert.AreEqual(new PuntoPantalla?[] { PuntoUsuario }, nativa.Idas);
            Assert.AreEqual(Usuario, mapa.Centrados.Last(), "Centrada antes de irse.");
            CollectionAssert.AreEqual(new[] { (ParametrosNegocio.InicioAcercarDesde, ParametrosNegocio.InicioAcercarSegundos) }, mapa.Acercamientos);
            Assert.IsTrue(inicio.ALaVista, "Aún se está yendo.");
            Fotograma(0.3);
            Assert.AreEqual(1, nativa.Idas.Count, "Solo una vez.");
            Assert.AreEqual(1, mapa.Acercamientos.Count);
            nativa.Segundos = null;
            Fotograma();
            Assert.IsFalse(inicio.ALaVista);
            Assert.IsFalse(pantalla.InicioALaVista);
        }

        [Test]
        public void Rapido_TodoListoALosDosDecimas_EsperaAQueElPinCaiga()
        {
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Usuario;
            mapa.VistaCargada = true;
            Crear();
            Fotograma(0.2);
            Assert.IsEmpty(nativa.Idas);
            Fotograma(0.75);
            Assert.IsEmpty(nativa.Idas);
            Fotograma(0.1);
            Assert.AreEqual(1, nativa.Idas.Count);
        }

        [Test]
        public void ElPinNoHaPodidoEmpezarACaer_NoSeVaAunquePasenLos4Segundos()
        {
            Crear();
            reloj += 10;
            nativa.Segundos = 0;
            Fotograma();
            Assert.IsEmpty(nativa.Idas);
        }

        [Test]
        public void GpsLento_ALos4SegundosSeVaAlCentroYSeCentraAlLlegar()
        {
            gps.UltimaConocida = OtroSitio;
            mapa.VistaCargada = true;
            Crear();
            CollectionAssert.AreEqual(new[] { OtroSitio }, mapa.Centrados);
            Fotograma(3.9);
            Assert.IsEmpty(nativa.Idas);
            Fotograma(0.2);
            CollectionAssert.AreEqual(new PuntoPantalla?[] { null }, nativa.Idas, "Al centro: la última conocida.");
            CollectionAssert.AreEqual(new[] { OtroSitio }, mapa.Centrados);
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Usuario;
            Fotograma(1);
            CollectionAssert.AreEqual(new[] { OtroSitio, Usuario }, mapa.Centrados, "Como hoy.");
        }

        [Test]
        public void MapaLento_ConPosicionYSinTeselasSeVaALos4Segundos()
        {
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Usuario;
            Crear();
            Fotograma(3.9);
            Assert.IsEmpty(nativa.Idas);
            Fotograma(0.2);
            CollectionAssert.AreEqual(new PuntoPantalla?[] { PuntoUsuario }, nativa.Idas);
        }

        [TestCase(EstadoUbicacion.SinPermiso)]
        [TestCase(EstadoUbicacion.Denegado)]
        [TestCase(EstadoUbicacion.Apagada)]
        public void SinPermisoOApagada_NoEsperaALaPosicionSoloAlMapa(EstadoUbicacion estado)
        {
            gps.Estado = estado;
            Crear();
            Fotograma(1.5);
            Assert.IsEmpty(nativa.Idas, "El mapa sí se espera.");
            mapa.VistaCargada = true;
            Fotograma();
            CollectionAssert.AreEqual(new PuntoPantalla?[] { null }, nativa.Idas, "Al centro.");
            Assert.AreEqual(0, gps.Permisos, "La pantalla de inicio no pide permisos.");
        }

        [Test]
        public void SinPermiso_NoSaleNingunaVentanaNiDebajoNiAlIrse()
        {
            gps.Estado = EstadoUbicacion.SinPermiso;
            mapa.VistaCargada = true;
            Crear();
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            Fotograma(1.5);
            Fotograma(0.5);
            Assert.AreEqual(1, nativa.Idas.Count);
            nativa.Segundos = null;
            Fotograma();
            Assert.IsFalse(pantalla.ExplicacionAbierta, "Desde la 3.1, el permiso se pide en la bienvenida.");
            Assert.IsFalse(pantalla.Accesibilidad.Modal);
        }

        [Test]
        public void SinPantallaDeInicio_LaAppSaleAlMomento()
        {
            gps.Estado = EstadoUbicacion.SinPermiso;
            Crear(conInicio: false);
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            Assert.IsFalse(pantalla.InicioALaVista);
            Fotograma(5);
            Assert.IsEmpty(mapa.Acercamientos);
        }

        [Test]
        public void SinPantallaNativa_ElServicioNoHaceNada()
        {
            nativa.Segundos = null;
            gps.Estado = EstadoUbicacion.SinPermiso;
            mapa.VistaCargada = true;
            var inicio = Crear();
            Assert.IsFalse(inicio.ALaVista);
            Fotograma(5);
            Assert.IsEmpty(nativa.Idas);
            Assert.IsEmpty(mapa.Acercamientos);
        }

        [Test]
        public void Compartido_NoEsperaALaPosicionYAbreSuFicha()
        {
            Crear();
            pantalla.RecibirCompartido("geo:36.77,-6.35");
            Assert.IsTrue(pantalla.FichaAbierta, "Debajo de la pantalla de inicio.");
            Fotograma(1.5);
            Assert.IsEmpty(nativa.Idas, "El mapa sí se espera.");
            gps.Estado = EstadoUbicacion.Activa; // aunque llegue la posición, el mapa está en otro sitio
            gps.Posicion = Usuario;
            mapa.VistaCargada = true;
            Fotograma();
            CollectionAssert.AreEqual(new PuntoPantalla?[] { null }, nativa.Idas, "Al centro.");
            CollectionAssert.DoesNotContain(mapa.Centrados, Usuario);
            nativa.Segundos = null;
            Fotograma();
            Assert.AreEqual(new Coordenada(36.77, -6.35), pantalla.PuntoAbierto);
        }

        [Test]
        public void CompartidoConTextoNormal_SiEsperaALaPosicion()
        {
            mapa.VistaCargada = true;
            Crear();
            pantalla.RecibirCompartido("bar pepe"); // no lleva a otro sitio
            Fotograma(2);
            Assert.IsEmpty(nativa.Idas);
        }

        [Test]
        public void CompartidoSinPermiso_AbreSuFichaDebajo()
        {
            gps.Estado = EstadoUbicacion.SinPermiso;
            mapa.VistaCargada = true;
            Crear();
            pantalla.RecibirCompartido("geo:36.77,-6.35");
            Assert.AreEqual(new Coordenada(36.77, -6.35), pantalla.PuntoAbierto, "No hay ninguna ventana que esperar.");
            Fotograma(1.5);
            Assert.AreEqual(1, nativa.Idas.Count);
            nativa.Segundos = null;
            Fotograma();
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            Assert.AreEqual(new Coordenada(36.77, -6.35), pantalla.PuntoAbierto);
        }

        [Test]
        public void SinAnimaciones_ElMapaNoSeAcerca()
        {
            accesibilidad.AnimacionesQuitadas = true;
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Usuario;
            mapa.VistaCargada = true;
            Crear();
            Fotograma(1.5);
            Assert.AreEqual(1, nativa.Idas.Count);
            Assert.IsEmpty(mapa.Acercamientos);
        }

        [Test]
        public void BloqueoAMitad_AlVolverSeVa()
        {
            Crear();
            Fotograma(0.5);
            reloj += 60; // Unity en pausa; la nativa sigue contando
            nativa.Segundos = 12;
            Fotograma();
            Assert.AreEqual(1, nativa.Idas.Count, "Pasados los 4 s, se va aunque no haya nada.");
        }

        [Test]
        public void BloqueoAMitad_LaNativaSeFueSola_LaAppSigueSinDecirleNada()
        {
            gps.Estado = EstadoUbicacion.SinPermiso;
            var inicio = Crear();
            int terminada = 0;
            inicio.Terminada += () => terminada++;
            Fotograma(0.5);
            nativa.Segundos = null; // 15 s con el móvil bloqueado
            Fotograma(60);
            Fotograma(1);
            Assert.IsEmpty(nativa.Idas);
            Assert.AreEqual(1, terminada, "Una sola vez.");
            Assert.IsFalse(pantalla.ExplicacionAbierta);
        }

        [Test]
        public void LosCuatroSegundosCuentanDesdeQueUnityEstaListoYNoDesdeElPin()
        {
            nativa.Segundos = 2.5; // Unity ha tardado: el pin lleva ya un rato
            Crear();
            Fotograma(3.9);
            Assert.IsEmpty(nativa.Idas);
            Fotograma(0.2);
            Assert.AreEqual(1, nativa.Idas.Count);
        }

        [Test]
        public void EnElMismoFotograma_CentraPreguntaProyectaAcercaYSeVa_EnEseOrden()
        {
            mapa.VistaCargada = true;
            Crear();
            Fotograma(1.5);
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Usuario;
            mapa.Orden.Clear();
            Fotograma();
            CollectionAssert.AreEqual(new[] { "centrar", "vista", "proyectar", "acercar", "irse" }, mapa.Orden.Take(5),
                "Centrado antes de mirar la vista; el destino, antes de alejar la cámara.");
            CollectionAssert.AreEqual(new PuntoPantalla?[] { PuntoUsuario }, nativa.Idas);
        }

        [Test]
        public void CompartidoConEnlaceCortoResolviendose_NoEsperaYSeVaAlCentro()
        {
            var resolutor = new ResolutorSinRespuesta();
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Usuario;
            mapa.VistaCargada = true;
            Crear(resolutor: resolutor);
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            Assert.AreEqual(1, resolutor.Peticiones);
            Assert.IsFalse(pantalla.FichaAbierta);
            Fotograma(1.5);
            CollectionAssert.AreEqual(new PuntoPantalla?[] { null }, nativa.Idas, "Aunque haya posición: va a otro sitio.");
        }

        [Test]
        public void CompartidoConEnlaceCortoResolviendose_SinPosicionTampocoEspera()
        {
            mapa.VistaCargada = true;
            Crear(resolutor: new ResolutorSinRespuesta());
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            Fotograma(1.5);
            CollectionAssert.AreEqual(new PuntoPantalla?[] { null }, nativa.Idas);
        }

        [Test]
        public void ConLaPantallaDeInicio_LaTeclaAtrasNoHaceNada()
        {
            Crear();
            pantalla.RecibirCompartido("geo:36.77,-6.35");
            pantalla.TeclaAtras();
            Assert.IsTrue(pantalla.FichaAbierta);
            nativa.Segundos = null;
            Fotograma();
            pantalla.TeclaAtras();
            Assert.IsFalse(pantalla.FichaAbierta, "Ya se ha ido: como siempre.");
            StringAssert.Contains("if (Input.GetKeyDown(KeyCode.Escape)) TeclaAtras();", File.ReadAllText("Assets/Scripts/UI/PantallaMapa.cs"));
        }

        [Test]
        public void AlIrse_ConTalkBackVuelveElFocoPedidoMientrasUnityEstabaTapado()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Crear();
            pantalla.RecibirCompartido("geo:36.77,-6.35");
            var acc = pantalla.Accesibilidad;
            acc.Reconstruir(); // nodos nuevos: el foco de antes ya no vale
            Assert.IsNotNull(acc.NodoDe(pantalla.Ficha.Titulo));
            Assert.AreNotSame(acc.NodoDe(pantalla.Ficha.Titulo), acc.UltimoNodoEnfocado);
            nativa.Segundos = null;
            Fotograma();
            Assert.AreSame(acc.NodoDe(pantalla.Ficha.Titulo), acc.UltimoNodoEnfocado);
        }

        [Test]
        public void Componer_EntregaLaPantallaDeInicioALaDelMapa()
        {
            var objeto = new GameObject("Pantalla con interfaz");
            objetos.Add(objeto);
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
            documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
            var conUi = objeto.AddComponent<PantallaMapa>();
            objeto.SetActive(true);
            Assert.IsNotNull(documento.rootVisualElement?.Q("raiz"), "El UIDocument no ha creado el árbol de la interfaz.");
            var prefs = new PreferenciasFalsas();
            var lugares = new ServicioLugares(new Repositorio());
            lugares.Cargar();
            Assert.IsTrue(Arranque.Arranque.Componer(mapa, conUi, new ServicioTema(prefs, new SistemaClaro()), lugares,
                new ServicioFiltro(prefs), Ahora, inicio: Servicio()));
            Assert.IsTrue(conUi.InicioALaVista);
        }

        [Test]
        public void Servicio_SoloPreguntaPorLaVistaCuandoHaceFalta()
        {
            var inicio = Servicio();
            int preguntas = 0;
            bool Vista() { preguntas++; return true; }
            Assert.IsFalse(inicio.Comprobar(true, true, Vista), "El pin no ha caído.");
            nativa.Segundos = 1.2;
            Assert.IsFalse(inicio.Comprobar(true, false, Vista), "Sin posición.");
            Assert.AreEqual(0, preguntas);
            Assert.IsTrue(inicio.Comprobar(true, true, Vista));
            Assert.AreEqual(1, preguntas);
            inicio.Irse(null);
            Assert.IsTrue(inicio.Yendose);
            Assert.IsFalse(inicio.Comprobar(true, true, Vista));
            inicio.Irse(null);
            Assert.AreEqual(1, nativa.Idas.Count);
        }

        [Test]
        public void Adaptador_ElDestinoEsUnaFraccionDeLaPantallaYFueraDeEllaElCentro()
        {
            Assert.AreEqual((0.25f, 0.75f), PantallaInicioAndroid.Fraccion(new PuntoPantalla(250f, 1500f, 10f), 1000f, 2000f));
            Assert.AreEqual((0.5f, 0.5f), PantallaInicioAndroid.Fraccion(null, 1000f, 2000f));
            Assert.AreEqual((0.5f, 0.5f), PantallaInicioAndroid.Fraccion(PuntoPantalla.Ninguno, 1000f, 2000f), "Detrás de la cámara.");
            Assert.AreEqual((0.5f, 0.5f), PantallaInicioAndroid.Fraccion(new PuntoPantalla(1200f, 1500f, 10f), 1000f, 2000f));
            Assert.AreEqual((0.5f, 0.5f), PantallaInicioAndroid.Fraccion(new PuntoPantalla(float.NaN, 10f, 10f), 1000f, 2000f));
            Assert.AreEqual((0.5f, 0.5f), PantallaInicioAndroid.Fraccion(new PuntoPantalla(1f, 1f, 10f), 0f, 0f));
            Assert.IsNull(new PantallaInicioAndroid().Segundos, "En el editor no hay pantalla de inicio.");
        }

        // ───────────────────────── El mapa ─────────────────────────

        CamaraMapa Camara()
        {
            var o = new GameObject("Cámara de prueba", typeof(Camera));
            objetos.Add(o);
            var c = o.AddComponent<CamaraMapa>();
            c.CentrarEn(36.7779, -6.3528);
            return c;
        }

        static float Lejos(CamaraMapa c) => Vector3.Distance(c.transform.position, c.pivote);

        [Test]
        public void Acercarse_EmpiezaA122VecesYAcabaEnSuDistanciaSinCambiarla()
        {
            var c = Camara();
            float d = c.distancia;
            c.AcercarDesde(1.22f, 0.8f);
            Assert.AreEqual(d * 1.22f, Lejos(c), 0.5f);
            Assert.AreEqual(d, c.distancia);
            c.AvanzarAcercamiento(0.4f);
            Assert.That(Lejos(c), Is.InRange(d * 1.001f, d * 1.11f), "A la mitad del tiempo ya ha hecho más de la mitad.");
            c.AvanzarAcercamiento(0.4f);
            Assert.AreEqual(d, Lejos(c), 0.5f);
            Assert.AreEqual(1f, c.FactorAcercar);
            c.AvanzarAcercamiento(0.4f);
            Assert.AreEqual(d, Lejos(c), 0.5f);
        }

        [Test]
        public void Acercarse_OtroMovimientoLoAcaba_YUnGestoSinSalto()
        {
            var c = Camara();
            float d = c.distancia;
            c.AcercarDesde(1.22f, 0.8f);
            c.CentrarEn(36.7700, -6.3500, new Vector2(10f, 10f));
            Assert.AreEqual(1f, c.FactorAcercar, "El punto queda donde se pide.");
            Assert.AreEqual(d, Lejos(c), 0.5f);
            c.AcercarDesde(1.22f, 0.8f);
            c.Seguir(36.7700, -6.3500, 0f, 45f, 350f, new Vector2(10f, 10f), animado: false);
            Assert.AreEqual(350f, Lejos(c), 0.5f);
            c.AcercarDesde(1.22f, 0.8f);
            c.Acercar();
            Assert.AreEqual(210f, Lejos(c), 0.5f);
            c.AcercarDesde(1.22f, 0.8f);
            c.Alejar();
            Assert.AreEqual(350f, Lejos(c), 0.5f);

            c.AcercarDesde(1.22f, 0.8f);
            float antes = Lejos(c);
            c.SimularGesto();
            Assert.AreEqual(antes, Lejos(c), 0.5f, "Con un gesto se queda donde está.");
            Assert.AreEqual(1f, c.FactorAcercar);
            c.AcercarDesde(0f, 0.8f);
            c.AcercarDesde(1.22f, 0f);
            Assert.AreEqual(1f, c.FactorAcercar, "Valores sin sentido: nada.");
        }

        [Test]
        public void Acercarse_ElTiempoDeCadaFotogramaLoAvanza()
        {
            var c = Camara();
            c.AcercarDesde(1.22f, 0.8f);
            c.Avanzar(0.4f);
            Assert.That(c.FactorAcercar, Is.InRange(1.001f, 1.11f));
            c.Avanzar(0.5f);
            Assert.AreEqual(1f, c.FactorAcercar);
            StringAssert.Contains("Avanzar(Time.unscaledDeltaTime);", File.ReadAllText("Assets/Scripts/Vectorial/CamaraMapa.cs"), "Lo llama Update.");
        }

        MapaVectorial Mapa()
        {
            var o = new GameObject("Mapa de prueba");
            objetos.Add(o);
            var m = o.AddComponent<MapaVectorial>();
            m.sombras = false;
            return m;
        }

        [Test]
        public void Acercarse_ElPuertoDelMapaMueveLaCamaraDeVerdad()
        {
            var c = Camara();
            ((IMapa)Mapa()).AcercarDesde(1.22f, 0.8f);
            Assert.AreEqual(1.22f, c.FactorAcercar);
        }

        [Test]
        public void VistaCargada_LoQueSeVeraAlAcercarseDesdeLejosTambienCuenta()
        {
            var c = Camara();
            var vectorial = Mapa();
            var cam = c.GetComponent<Camera>();
            var t = Geo.Tesela(36.7779, -6.3528, vectorial.zoom);
            var centro = Geo.CentroTesela(t.x, t.y, vectorial.zoom, out float lado);
            c.inclinacion = 85f;
            c.distancia = 300f;
            c.pivote = centro;
            c.OrientarNorte();
            // Lo más al este que se ve (esquina de arriba) y a un 11 % más por cada lado
            float borde = c.PuntoEnSuelo(new Vector2(cam.pixelWidth, cam.pixelHeight)).Value.x - centro.x;
            float margen = c.PuntoEnSuelo(new Vector2(cam.pixelWidth * 1.11f, cam.pixelHeight * 1.11f)).Value.x - centro.x;
            Assert.Greater(margen, borde + 2f);
            vectorial.SimularCargada(t);

            c.pivote = centro + new Vector3(lado / 2f - margen - 2f, 0f, 0f); // la de al lado, justo fuera del margen
            c.OrientarNorte();
            Assert.IsTrue(vectorial.VistaCargada);
            c.pivote = centro + new Vector3(lado / 2f - (borde + margen) / 2f, 0f, 0f); // fuera de la pantalla, dentro del margen
            c.OrientarNorte();
            Assert.IsFalse(vectorial.VistaCargada);
            vectorial.SimularCargada(new Vector2Int(t.x + 1, t.y));
            Assert.IsTrue(vectorial.VistaCargada);
        }

        [Test]
        public void VistaCargada_SoloConTodasLasTeselasQueSeVenDelCentroDeAhora()
        {
            var c = Camara();
            var o = new GameObject("Mapa de prueba");
            objetos.Add(o);
            var vectorial = o.AddComponent<MapaVectorial>();
            vectorial.sombras = false;
            Assert.IsFalse(vectorial.VistaCargada, "Nada cargado.");

            // Cenital y cerca, en el centro de una tesela: se ve solo esa
            var t = Geo.Tesela(36.7779, -6.3528, vectorial.zoom);
            c.inclinacion = 85f;
            c.distancia = 300f;
            c.pivote = Geo.CentroTesela(t.x, t.y, vectorial.zoom, out float lado);
            c.OrientarNorte();
            vectorial.SimularCargada(t);
            Assert.IsTrue(vectorial.VistaCargada);

            // En el borde con la del este se ven las dos
            c.pivote += new Vector3(lado / 2f - 5f, 0f, 0f);
            c.OrientarNorte();
            Assert.IsFalse(vectorial.VistaCargada, "La de al lado también se ve.");
            vectorial.SimularCargada(new Vector2Int(t.x + 1, t.y));
            Assert.IsTrue(vectorial.VistaCargada);

            // Centrada en otro sitio: cuenta ya el centro nuevo, sin esperar a que el mapa se entere
            c.pivote += new Vector3(lado * 3f, 0f, 0f);
            c.OrientarNorte();
            Assert.IsFalse(vectorial.VistaCargada);
        }

        [Test]
        public void VistaCargada_ElHorizonteNoCuenta()
        {
            var c = Camara();
            var o = new GameObject("Mapa de prueba");
            objetos.Add(o);
            var vectorial = o.AddComponent<MapaVectorial>();
            vectorial.sombras = false;
            var t = Geo.Tesela(36.7779, -6.3528, vectorial.zoom);
            c.inclinacion = 20f; // se ve muy lejos
            c.distancia = 1500f;
            c.pivote = Geo.CentroTesela(t.x, t.y, vectorial.zoom, out _);
            c.OrientarNorte();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    vectorial.SimularCargada(new Vector2Int(t.x + dx, t.y + dy));
            Assert.IsTrue(vectorial.VistaCargada, "Solo se esperan las que se van a cargar (3×3).");
        }

        // ───────────────────────── Recursos y ajustes de Android ─────────────────────────

        [Test]
        public void Recursos_ImagenesColoresYArranqueDelMismoColor_TambienDosVeces()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "PriceMapInicio_" + Guid.NewGuid().ToString("N"));
            string res = Path.Combine(carpeta, "src", "main", "res");
            Directory.CreateDirectory(Path.Combine(res, "values"));
            Directory.CreateDirectory(Path.Combine(res, "values-v31"));
            // Como los genera Unity 6000.3
            File.WriteAllText(Path.Combine(res, "values", "colors.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><resources><color name=\"staticSplashScreenBackgroundColor\">#FF1F1F20</color></resources>");
            File.WriteAllText(Path.Combine(res, "values", "styles.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><resources><style name=\"UnityThemeSelector\" parent=\"BaseUnityTheme\">" +
                "<item name=\"android:windowBackground\">@android:color/black</item></style>" +
                "<style name=\"UnityThemeSelector.Translucent\" parent=\"@style/UnityThemeSelector\"><item name=\"android:windowBackground\">@android:color/transparent</item></style></resources>");
            File.WriteAllText(Path.Combine(res, "values-v31", "styles.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><resources><style name=\"BaseUnityTheme\" parent=\"android:Theme.Holo.Light.NoActionBar.Fullscreen\">" +
                "<item name=\"android:windowSplashScreenAnimatedIcon\">@color/windowSplashScreenAnimatedIconColor</item>" +
                "<item name=\"android:windowSplashScreenBackground\">@color/staticSplashScreenBackgroundColor</item>" +
                "<item name=\"android:windowBackground\">@android:color/black</item></style></resources>");
            try
            {
                for (int i = 0; i < 2; i++) new PantallaInicioRecursos().OnPostGenerateGradleAndroidProject(carpeta);

                foreach (var imagen in PantallaInicioRecursos.Imagenes)
                    CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(PantallaInicioRecursos.CarpetaImagenes, imagen.Key)),
                        File.ReadAllBytes(Path.Combine(res, "drawable-nodpi", imagen.Value)), imagen.Key);

                string Texto(string fichero, string nodo)
                {
                    var xml = new XmlDocument();
                    xml.Load(Path.Combine(res, fichero));
                    return xml.SelectNodes(nodo).Cast<XmlNode>().Single().InnerText;
                }
                Assert.AreEqual("#FDFDFD", Texto("values/inicio.xml", "/resources/color[@name='inicio_claro']"));
                Assert.AreEqual("#0F1424", Texto("values/inicio.xml", "/resources/color[@name='inicio_oscuro']"));
                Assert.AreEqual("@color/inicio_claro", Texto("values/inicio.xml", "/resources/color[@name='inicio_fondo']"));
                Assert.AreEqual("@color/inicio_oscuro", Texto("values-night/inicio.xml", "/resources/color[@name='inicio_fondo']"));
                Assert.AreEqual("PriceMap, cargando", Texto("values/inicio.xml", "/resources/string[@name='inicio_cargando']"));

                Assert.AreEqual("@color/inicio_fondo", Texto("values/colors.xml", "/resources/color"));
                Assert.AreEqual("@color/inicio_fondo", Texto("values/styles.xml", "/resources/style[@name='UnityThemeSelector']/item"));
                Assert.AreEqual("@android:color/transparent", Texto("values/styles.xml", "/resources/style[@name='UnityThemeSelector.Translucent']/item"), "Lo demás, igual.");
                Assert.AreEqual("@color/inicio_fondo", Texto("values-v31/styles.xml", "//item[@name='android:windowSplashScreenBackground']"));
                Assert.AreEqual("@color/inicio_fondo", Texto("values-v31/styles.xml", "//item[@name='android:windowBackground']"));
                Assert.AreEqual("@color/windowSplashScreenAnimatedIconColor", Texto("values-v31/styles.xml", "//item[@name='android:windowSplashScreenAnimatedIcon']"), "Sin icono (transparente).");
                foreach (var tema in new[] { ("Claro", "claro"), ("Oscuro", "oscuro") })
                    foreach (string atributo in new[] { "windowBackground", "windowSplashScreenBackground" })
                        Assert.AreEqual("@color/inicio_" + tema.Item2,
                            Texto("values-v31/inicio.xml", $"//style[@name='PriceMapInicio{tema.Item1}']/item[@name='android:{atributo}']"));
            }
            finally { Directory.Delete(carpeta, true); }
        }

        [Test]
        public void Recursos_SiUnityCambiaSusFicherosLaCompilacionFalla()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "PriceMapInicio_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(carpeta, "src", "main", "res", "values"));
            File.WriteAllText(Path.Combine(carpeta, "src", "main", "res", "values", "colors.xml"), "<resources></resources>");
            try
            {
                var e = Assert.Throws<BuildFailedException>(() => new PantallaInicioRecursos().OnPostGenerateGradleAndroidProject(carpeta));
                StringAssert.Contains("staticSplashScreenBackgroundColor", e.Message);
            }
            finally { Directory.Delete(carpeta, true); }
        }

        [Test]
        public void Manifiesto_LaActividadSeDibujaConAceleracionGrafica()
        {
            const string android = "http://schemas.android.com/apk/res/android";
            string carpeta = Path.Combine(Path.GetTempPath(), "PriceMapInicio_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(carpeta, "src", "main"));
            string manifiesto = Path.Combine(carpeta, "src", "main", "AndroidManifest.xml");
            File.WriteAllText(manifiesto, "<manifest xmlns:android=\"" + android + "\"><application><activity android:name=\"x\" android:hardwareAccelerated=\"false\">" +
                "<intent-filter><action android:name=\"android.intent.action.MAIN\" /><category android:name=\"android.intent.category.LAUNCHER\" /></intent-filter></activity></application></manifest>");
            try
            {
                new ActividadAndroid().OnPostGenerateGradleAndroidProject(carpeta);
                var xml = new XmlDocument();
                xml.Load(manifiesto);
                Assert.AreEqual("true", ((XmlElement)xml.GetElementsByTagName("activity")[0]).GetAttribute("hardwareAccelerated", android));
            }
            finally { Directory.Delete(carpeta, true); }
        }

        [Test]
        public void Ajustes_SinElLogoDeUnityTampocoEnPc()
        {
            Assert.IsFalse(PlayerSettings.SplashScreen.show);
            Assert.IsFalse(PlayerSettings.SplashScreen.showUnityLogo);
            string codigo = File.ReadAllText("Assets/Editor/ConfigurarAndroid.cs");
            StringAssert.Contains("PlayerSettings.SplashScreen.show = false;", codigo);
            StringAssert.Contains("PlayerSettings.SplashScreen.showUnityLogo = false;", codigo);
            Assert.AreEqual(3, Regex.Matches(codigo, @"QuitarLogoUnity\(\)").Count, "En Configurar (APK) y en CompilarPcPruebas.");
        }

        [Test]
        public void Arranque_LaPantallaDeInicioNoSaleEnLaCaptura()
        {
            string codigo = File.ReadAllText("Assets/Scripts/Arranque/Arranque.cs");
            StringAssert.Contains("CapturaPruebas.SePide() ? null : new ServicioInicio(new PantallaInicioAndroid()", codigo);
            StringAssert.Contains("inicio?.Irse(null)", codigo, "Si la app no puede arrancar, no se queda.");
        }

        // ───────────────────────── Java, leyendo el código ─────────────────────────

        static string Java(string nombre) => File.ReadAllText("Assets/Plugins/Android/" + nombre + ".java");

        /// <summary>El cuerpo del método Java con esa firma (hasta la llave que lo cierra).</summary>
        static string Cuerpo(string java, string firma)
        {
            int inicio = java.IndexOf(firma, StringComparison.Ordinal);
            Assert.GreaterOrEqual(inicio, 0, "No está " + firma);
            int abre = java.IndexOf('{', inicio), nivel = 0;
            for (int i = abre; i < java.Length; i++)
            {
                if (java[i] == '{') nivel++;
                else if (java[i] == '}' && --nivel == 0) return java.Substring(abre, i - abre);
            }
            Assert.Fail("Sin cerrar: " + firma);
            return null;
        }

        static int Entero(string java, string nombre)
        {
            var m = Regex.Match(java, @"\b" + nombre + @" = (\d+)");
            Assert.IsTrue(m.Success, nombre);
            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Test]
        public void Actividad_LaPoneEncimaDeUnityEnOnCreateYLaQuitaAlDestruirse()
        {
            string java = Java("ActividadPrincipal");
            string crear = Cuerpo(java, "protected void onCreate(");
            int ventana = crear.IndexOf("PuenteInicio.prepararVentana(this);", StringComparison.Ordinal);
            int contenido = crear.IndexOf("setContentView(", StringComparison.Ordinal);
            int mostrar = crear.IndexOf("PuenteInicio.mostrar(this, mUnityPlayer.getFrameLayout());", StringComparison.Ordinal);
            Assert.That(ventana, Is.GreaterThan(0).And.LessThan(contenido), "El fondo de la ventana, antes de que se vea nada.");
            Assert.Greater(mostrar, contenido, "Encima de Unity.");
            StringAssert.Contains("PuenteInicio.quitar();", Cuerpo(Cuerpo(java, "protected void onDestroy ("), "if (instancia == this)"), "La de una actividad más nueva se queda.");
            StringAssert.Contains("PuenteInicio.fijarArranque(this);", Cuerpo(java, "protected void onStop("));
            foreach (string vuelta in new[] { "protected void onStart(", "protected void onResume(", "protected void onNewIntent(" })
                StringAssert.DoesNotContain("PuenteInicio.mostrar", Cuerpo(java, vuelta), "Al volver de segundo plano no sale.");
        }

        [Test]
        public void Puente_LoQueLlamaCSharpYElSeguroDe15Segundos()
        {
            string java = Java("PuenteInicio");
            string cs = File.ReadAllText("Assets/Scripts/Adaptadores/Sistema/PantallaInicioAndroid.cs");
            StringAssert.Contains("package com.pricemap.inicio;", java);
            StringAssert.Contains("\"com.pricemap.inicio.PuenteInicio\"", cs);
            StringAssert.Contains("public static int estado()", java);
            StringAssert.Contains("CallStatic<int>(\"estado\")", cs);
            StringAssert.Contains("public static void irse(final float x, final float y)", java);
            StringAssert.Contains("CallStatic(\"irse\", x, y)", cs);
            StringAssert.Contains("principal.post(", Cuerpo(java, "public static void irse("), "Llega desde el hilo de Unity.");
            Assert.AreEqual(15000, Entero(java, "SEGURO_MS"));
            StringAssert.Contains("principal.postDelayed(seguro, SEGURO_MS);", Cuerpo(java, "public static void mostrar("));
            string salir = Cuerpo(java, "private static void irseAhora(");
            StringAssert.Contains("postDelayed", salir, "Se quita con el reloj: con la pantalla apagada también.");
            StringAssert.Contains("yendose()", salir, "Solo una vez.");
            string quitar = Cuerpo(java, "public static void quitar(");
            Assert.That(quitar.IndexOf("v.liberar();", StringComparison.Ordinal),
                Is.GreaterThan(0).And.LessThan(quitar.IndexOf("vista = null;", StringComparison.Ordinal)), "Unity vuelve a TalkBack antes de que C# lo sepa.");
        }

        [Test]
        public void Tema_ElGuardadoPorUnityYSinElElDelSistema()
        {
            string java = Java("PuenteInicio");
            StringAssert.Contains("\"PriceMap." + ServicioTema.ClavePreferencia + "\"", java, "La clave de PreferenciasLocales.");
            StringAssert.Contains("const string Prefijo = \"PriceMap.\";", File.ReadAllText("Assets/Scripts/Adaptadores/Sistema/PreferenciasLocales.cs"));
            StringAssert.Contains(".v2.playerprefs\"", java);
            StringAssert.Contains("Uri.decode(valor)", java);
            string oscuro = Cuerpo(java, "static boolean oscuro(");
            StringAssert.Contains($"if (\"{ReglaTema.AClave(Tema.Oscuro)}\".equals(guardado)) return true;", oscuro);
            StringAssert.Contains($"if (\"{ReglaTema.AClave(Tema.Claro)}\".equals(guardado)) return false;", oscuro);
            Assert.Less(oscuro.IndexOf("equals(guardado)", StringComparison.Ordinal), oscuro.IndexOf("uiMode", StringComparison.Ordinal), "Manda el guardado.");
            StringAssert.Contains("setSplashScreenTheme(tema)", Cuerpo(java, "private static void fijarArranque31("));
        }

        [Test]
        public void Vista_TiemposDeCSharpMedidasDeLaImagenYSinAnimaciones()
        {
            string java = Java("PantallaInicio");
            Assert.AreEqual(ParametrosNegocio.InicioPinSegundos * 1000, Entero(java, "CAER"), "El pin ha caído cuando C# lo da por caído.");
            Assert.AreEqual(Mathf.RoundToInt(ParametrosNegocio.InicioAcercarSegundos * 1000), Entero(java, "SALIR"), "El mapa se acerca mientras se va.");
            Assert.Less(Entero(java, "SALIR"), 1000, "Menos de 1 s.");
            Assert.Less(Entero(java, "FUNDIDO"), Entero(java, "SALIR"), "Fundido corto.");
            StringAssert.Contains("ANCHO = 852f, ALTO = 1846f", java);
            StringAssert.Contains("ANIMATOR_DURATION_SCALE", java);
            string empezar = Cuerpo(java, "private void empezar(");
            Assert.Less(empezar.IndexOf("if (sinAnimaciones) return;", StringComparison.Ordinal), empezar.IndexOf("animar(pin", StringComparison.Ordinal),
                "Sin caída, flotar ni ondas.");
            StringAssert.Contains("if (sinAnimaciones)", Cuerpo(java, "long irse("));
            StringAssert.Contains("AlphaAnimation(1f, 0f)", Cuerpo(java, "long irse("));
        }

        [Test]
        public void Vista_SeComeLosToquesTalkBackSoloLaLeeYSeLiberaAlIrse()
        {
            string java = Java("PantallaInicio");
            foreach (string toque in new[] { "onInterceptTouchEvent", "onTouchEvent", "onGenericMotionEvent", "onHoverEvent" })
                StringAssert.Contains("return true;", Cuerpo(java, "public boolean " + toque + "("), toque);
            StringAssert.Contains("setContentDescription(getResources().getString(R.string.inicio_cargando));", java);
            StringAssert.Contains("unity.setImportantForAccessibility(IMPORTANT_FOR_ACCESSIBILITY_NO_HIDE_DESCENDANTS);", java);
            string liberar = Cuerpo(java, "void liberar(");
            StringAssert.Contains("unity.setImportantForAccessibility(accesibilidadUnity);", liberar);
            Assert.That(liberar.IndexOf("dejarDeEsperar();", StringComparison.Ordinal),
                Is.GreaterThan(0).And.LessThan(liberar.IndexOf("padre.removeView(this);", StringComparison.Ordinal)), "Sin el aviso de dibujo pendiente.");
            StringAssert.Contains("removeOnPreDrawListener(alVerse)", Cuerpo(java, "private void dejarDeEsperar("));
            StringAssert.Contains("soltar(pin);", liberar);
            StringAssert.Contains("soltar(nombre);", liberar);
            StringAssert.Contains("recycle()", Cuerpo(java, "private static void soltar("));
            StringAssert.DoesNotContain("requestPermissions", java + Java("PuenteInicio"), "No pide permisos.");
        }
    }
}
