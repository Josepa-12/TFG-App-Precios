using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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
    /// Compartir una ubicación con PriceMap (historia 2.5, parte 3; FR17): los filtros del manifiesto (SEND text/plain y
    /// VIEW geo:), el texto que va al campo (LectorUbicacionCompartida.TextoCompartido), el paso del Arranque y cada fila
    /// de la matriz sobre la pantalla real con un resolutor de enlaces cortos falso. Nada sale a la red.
    /// </summary>
    public class PruebasCompartir
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        // ───────────────────────── Manifiesto ─────────────────────────

        const string Android = "http://schemas.android.com/apk/res/android";

        static string ManifiestoMinimo(string filtrosExtra = "") =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<manifest xmlns:android=\"" + Android + "\">\n" +
            "  <application>\n" +
            "    <activity android:name=\"com.otra.Actividad\" />\n" +
            "    <activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\" android:launchMode=\"singleTask\">\n" +
            "      <intent-filter>\n" +
            "        <category android:name=\"android.intent.category.LAUNCHER\" />\n" +
            "        <action android:name=\"android.intent.action.MAIN\" />\n" +
            "      </intent-filter>\n" + filtrosExtra +
            "    </activity>\n" +
            "  </application>\n" +
            "</manifest>\n";

        static string CarpetaManifiesto(string contenido)
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "PriceMapCompartir_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(carpeta, "src", "main"));
            File.WriteAllText(Path.Combine(carpeta, "src", "main", "AndroidManifest.xml"), contenido);
            return carpeta;
        }

        static List<XmlElement> Filtros(string carpeta, string accion)
        {
            var xml = new XmlDocument();
            xml.Load(Path.Combine(carpeta, "src", "main", "AndroidManifest.xml"));
            var lista = new List<XmlElement>();
            foreach (XmlElement f in xml.GetElementsByTagName("intent-filter"))
                foreach (XmlElement a in f.GetElementsByTagName("action"))
                    if (a.GetAttribute("name", Android) == accion) lista.Add(f);
            return lista;
        }

        static List<string> Valores(XmlElement filtro, string etiqueta, string atributo) =>
            filtro.GetElementsByTagName(etiqueta).Cast<XmlElement>().Select(e => e.GetAttribute(atributo, Android)).ToList();

        static void ComprobarFiltros(string carpeta)
        {
            var enviar = Filtros(carpeta, "android.intent.action.SEND");
            Assert.AreEqual(1, enviar.Count, "Un filtro SEND.");
            Assert.AreEqual("com.unity3d.player.UnityPlayerGameActivity", ((XmlElement)enviar[0].ParentNode).GetAttribute("name", Android),
                "En la actividad lanzadora de Unity.");
            CollectionAssert.AreEqual(new[] { "android.intent.category.DEFAULT" }, Valores(enviar[0], "category", "name"));
            CollectionAssert.AreEqual(new[] { "text/plain" }, Valores(enviar[0], "data", "mimeType"));

            var ver = Filtros(carpeta, "android.intent.action.VIEW");
            Assert.AreEqual(1, ver.Count, "Un filtro VIEW (solo geo:, nunca https de Google Maps).");
            Assert.AreEqual("com.unity3d.player.UnityPlayerGameActivity", ((XmlElement)ver[0].ParentNode).GetAttribute("name", Android));
            CollectionAssert.AreEquivalent(new[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" }, Valores(ver[0], "category", "name"));
            CollectionAssert.AreEqual(new[] { "geo" }, Valores(ver[0], "data", "scheme"));
            Assert.AreEqual(1, Filtros(carpeta, "android.intent.action.MAIN").Count, "El filtro lanzador se queda.");
        }

        [Test]
        public void Manifiesto_AnadeSendYGeoALaActividadLanzadora()
        {
            string carpeta = CarpetaManifiesto(ManifiestoMinimo());
            try
            {
                new CompartirAndroid().OnPostGenerateGradleAndroidProject(carpeta);
                ComprobarFiltros(carpeta);
            }
            finally { Directory.Delete(carpeta, true); }
        }

        [Test]
        public void Manifiesto_GenerarDosVecesNoDuplica()
        {
            string carpeta = CarpetaManifiesto(ManifiestoMinimo());
            try
            {
                var paso = new CompartirAndroid();
                paso.OnPostGenerateGradleAndroidProject(carpeta);
                paso.OnPostGenerateGradleAndroidProject(carpeta);
                ComprobarFiltros(carpeta);
            }
            finally { Directory.Delete(carpeta, true); }
        }

        [Test]
        public void Manifiesto_SiYaEstanNoSeRepiten()
        {
            const string ya =
                "      <intent-filter>\n" +
                "        <action android:name=\"android.intent.action.SEND\" />\n" +
                "        <category android:name=\"android.intent.category.DEFAULT\" />\n" +
                "        <data android:mimeType=\"text/plain\" />\n" +
                "      </intent-filter>\n" +
                "      <intent-filter>\n" +
                "        <action android:name=\"android.intent.action.VIEW\" />\n" +
                "        <category android:name=\"android.intent.category.DEFAULT\" />\n" +
                "        <category android:name=\"android.intent.category.BROWSABLE\" />\n" +
                "        <data android:scheme=\"geo\" />\n" +
                "      </intent-filter>\n";
            string carpeta = CarpetaManifiesto(ManifiestoMinimo(ya));
            try
            {
                new CompartirAndroid().OnPostGenerateGradleAndroidProject(carpeta);
                ComprobarFiltros(carpeta);
            }
            finally { Directory.Delete(carpeta, true); }
        }

        // ───────────────────────── Texto para el campo ─────────────────────────

        [Test]
        public void Texto_ConEnlaceVaSoloElEnlace()
        {
            Assert.AreEqual("https://maps.app.goo.gl/abc",
                LectorUbicacionCompartida.TextoCompartido("Bar Pepe\nCalle Ancha 3\nhttps://maps.app.goo.gl/abc"));
            Assert.AreEqual("geo:36.778,-6.351?q=36.778,-6.351(Casa)",
                LectorUbicacionCompartida.TextoCompartido("geo:36.778,-6.351?q=36.778,-6.351(Casa)"));
            Assert.AreEqual("https://maps.app.goo.gl/abc", LectorUbicacionCompartida.TextoCompartido("Mira: maps.app.goo.gl/abc"),
                "Uno de mapas sin esquema, con https://.");
        }

        [Test]
        public void Texto_SinEnlaceEnUnaLineaYRecortado()
        {
            Assert.AreEqual("36.7781, -6.3515", LectorUbicacionCompartida.TextoCompartido("  36.7781, -6.3515 \n"));
            Assert.AreEqual("Bar Pepe Calle Ancha 3", LectorUbicacionCompartida.TextoCompartido("Bar Pepe\r\nCalle Ancha 3"));
            Assert.AreEqual("a b", LectorUbicacionCompartida.TextoCompartido("a\t\n\n  b"));
            string largo = LectorUbicacionCompartida.TextoCompartido(new string('x', 250));
            Assert.AreEqual(LectorUbicacionCompartida.LargoMaximoCompartido, largo.Length);
            string emoji = new string('x', 199) + "\U0001F600" + "yyy";
            Assert.AreEqual(new string('x', 199), LectorUbicacionCompartida.TextoCompartido(emoji), "Sin partir un emoji.");
        }

        [Test]
        public void Texto_VacioEsNull()
        {
            Assert.IsNull(LectorUbicacionCompartida.TextoCompartido(null));
            Assert.IsNull(LectorUbicacionCompartida.TextoCompartido(""));
            Assert.IsNull(LectorUbicacionCompartida.TextoCompartido(" \n\t "));
        }

        // ───────────────────────── Falsos ─────────────────────────

        class Resolucion
        {
            public string Url;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<string>> Tcs;
            public void Responder(string url) => Tcs.TrySetResult(Resultado<string>.Exito(url));
            public void Fallar() => Tcs.TrySetResult(Resultado<string>.Fallo("sin conexión"));
        }

        class ResolutorFalso : IResolutorEnlaces
        {
            public bool IgnorarCancelacion;
            public readonly List<Resolucion> Peticiones = new List<Resolucion>();

            public Task<Resultado<string>> Resolver(string url, CancellationToken ct)
            {
                var r = new Resolucion { Url = url, Ct = ct, Tcs = new TaskCompletionSource<Resultado<string>>() };
                Peticiones.Add(r);
                if (!IgnorarCancelacion) ct.Register(() => r.Tcs.TrySetCanceled());
                return r.Tcs.Task;
            }
        }

        class GeoFalso : IGeocodificador
        {
            public readonly List<string> Busquedas = new List<string>();

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct)
            {
                Busquedas.Add(consulta);
                return Task.FromResult(Resultado<IReadOnlyList<Direccion>>.Exito(Array.Empty<Direccion>()));
            }

            public Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct) =>
                new TaskCompletionSource<Resultado<Direccion>>().Task;
        }

        static Task EsperaInmediata(TimeSpan t, CancellationToken ct) => Task.CompletedTask;

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; }
            public Coordenada? Posicion => null;
            public float? Rumbo => null;
            public event Action Cambiado { add { } remove { } }
            public void PedirPermiso() { }
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
            public Coordenada Centro => Cabildo;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
        }

        class Repositorio : IRepositorioLugares
        {
            readonly IReadOnlyList<Lugar> lugares;
            public Repositorio(params Lugar[] lugares) => this.lugares = lugares;
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(lugares);
        }

        class EntradaFalsa : IEntradaCompartida
        {
            public readonly Queue<string> Cola = new Queue<string>();
            public string Tomar() => Cola.Count > 0 ? Cola.Dequeue() : null;
        }

        // ───────────────────────── Pantalla ─────────────────────────

        static readonly Lugar BarPepe = new Lugar("bar-pepe", "Bar Pepe", "Bar", Plantilla.Hosteleria, Cabildo, "Calle Ancha, 3");

        GameObject objeto;
        PantallaMapa pantalla;
        HojaBuscador hoja;
        GeoFalso geo;
        ResolutorFalso resolutor;
        SynchronizationContext contextoAntes;
        List<string> anuncios;
        Action<string> oir;

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            Textos.Forzados = T;
        }

        /// <param name="explicacion">Sin permiso, con la explicación de ubicación abierta (botón de ubicación).</param>
        void Crear(bool explicacion = false)
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var lugares = new ServicioLugares(new Repositorio(BarPepe));
            Assert.IsTrue(lugares.Cargar().Ok);
            var mapa = new MapaFalso();
            geo = new GeoFalso();
            resolutor = new ResolutorFalso();
            var ubicacion = explicacion ? new ServicioUbicacion(new UbicacionFalsa { Estado = EstadoUbicacion.SinPermiso }, prefs) : null;
            pantalla.Inicializar(mapa, tema, lugares, Ahora, arbol, filtro, ubicacion,
                busqueda: new ServicioBusqueda(lugares, mapa, Ahora),
                direcciones: new BuscadorDirecciones(geo, EsperaInmediata),
                geocodificador: geo, resolutorEnlaces: resolutor);
            tema.Iniciar();
            filtro.Iniciar();
            if (explicacion) pantalla.TocarUbicacion();
            hoja = pantalla.Buscador;
            hoja.Medir(800f);
        }

        [TearDown]
        public void Limpiar()
        {
            if (oir != null) AccesibilidadPantalla.AnuncioPedido -= oir;
            oir = null;
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        void ConLector()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            anuncios = new List<string>();
            oir = anuncios.Add;
            AccesibilidadPantalla.AnuncioPedido += oir;
        }

        /// <summary>
        /// Un fotograma: el Update real de la pantalla. En EditMode Unity no lo llama (ni con SendMessage), así que se
        /// invoca por reflexión.
        /// </summary>
        void Fotograma() =>
            typeof(PantallaMapa).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(pantalla, null);

        void AssertPuntoAbierto(Coordenada c)
        {
            Assert.IsTrue(pantalla.FichaAbierta, "Se abre sola la ficha de punto libre.");
            Assert.AreEqual(c, pantalla.PuntoAbierto);
            Assert.IsNull(pantalla.Ficha.Lugar);
        }

        void AssertMensajeEnHoja(string clave)
        {
            Assert.IsFalse(pantalla.FichaAbierta, "No se abre nada.");
            Assert.IsTrue(hoja.Abierta);
            Assert.IsEmpty(hoja.Filas);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.AreEqual(T(clave), hoja.SinResultados.text);
        }

        [Test]
        public void DesdeGoogleMaps_EnlaceCortoComprobandoYAlResolverseSeAbreLaFicha()
        {
            Crear();
            pantalla.RecibirCompartido("Bar Pepe\nCalle Ancha 3\nhttps://maps.app.goo.gl/abc");
            Assert.AreEqual("https://maps.app.goo.gl/abc", hoja.Campo.value, "Al campo va solo el enlace.");
            Assert.AreEqual("https://maps.app.goo.gl/abc", resolutor.Peticiones.Single().Url);
            AssertMensajeEnHoja(ClavesTexto.BuscadorComprobandoEnlace);

            resolutor.Peticiones.Single().Responder("https://www.google.com/maps/place/Bar/@36.77,-6.35,17z");
            AssertPuntoAbierto(new Coordenada(36.77, -6.35));
        }

        [Test]
        public void DesdeGoogleMapsSinRed_MensajeDeNoReconocidoEnLaHoja()
        {
            Crear();
            pantalla.RecibirCompartido("Bar Pepe\nhttps://maps.app.goo.gl/abc");
            resolutor.Peticiones.Single().Fallar();
            AssertMensajeEnHoja(ClavesTexto.BuscadorEnlaceNoReconocido);
        }

        [Test]
        public void SiCambiaElTextoMientrasSeResuelve_NoSeAbreNada()
        {
            Crear();
            resolutor.IgnorarCancelacion = true;
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            var r = resolutor.Peticiones.Single();
            hoja.Escribir("bar");
            r.Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            Assert.IsFalse(pantalla.FichaAbierta);

            // Ni aunque el usuario vuelva a escribir el mismo enlace: ya no es lo compartido
            hoja.Escribir("https://maps.app.goo.gl/abc");
            resolutor.Peticiones.Last().Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.AreEqual(TipoResultado.Coordenada, hoja.Filas.Single().Resultado.Tipo, "Se queda la fila «Ir a este punto».");
        }

        [Test]
        public void SiSeEligeUnaCategoriaMientrasSeResuelve_NoSeAbreNada()
        {
            Crear();
            resolutor.IgnorarCancelacion = true;
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            pantalla.ElegirCategoria(null);
            resolutor.Peticiones.Single().Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            Assert.IsFalse(pantalla.FichaAbierta);
        }

        [Test]
        public void SiSeAbreOtraFichaMientrasSeResuelve_SeQuedaEsa()
        {
            Crear();
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            pantalla.AbrirFicha(BarPepe);
            resolutor.Peticiones.Single().Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreSame(BarPepe, pantalla.Ficha.Lugar, "Sigue la ficha del lugar.");
            Assert.IsNull(pantalla.PuntoAbierto);
        }

        [Test]
        public void CoordenadasCompartidas_AbrenLaFichaAlMomento()
        {
            Crear();
            pantalla.RecibirCompartido("36.7781, -6.3515");
            AssertPuntoAbierto(new Coordenada(36.7781, -6.3515));
            Assert.AreEqual("36.7781, -6.3515", hoja.Campo.value);
            Assert.IsEmpty(geo.Busquedas, "Con unas coordenadas no se buscan direcciones.");
        }

        [Test]
        public void Geo_AbreLaFichaEnSuPunto()
        {
            Crear();
            pantalla.RecibirCompartido("geo:36.778,-6.351?q=36.778,-6.351(Casa)");
            AssertPuntoAbierto(new Coordenada(36.778, -6.351));
            Assert.IsEmpty(resolutor.Peticiones);
        }

        [Test]
        public void NoReconocido_MensajeEnLaHojaYNoSeAbreNada()
        {
            Crear();
            pantalla.RecibirCompartido("https://maps.google.com/?cid=123");
            Assert.AreEqual("https://maps.google.com/?cid=123", hoja.Campo.value);
            AssertMensajeEnHoja(ClavesTexto.BuscadorEnlaceNoReconocido);
            Assert.IsEmpty(resolutor.Peticiones);
        }

        [Test]
        public void TextoCualquiera_BusquedaNormal()
        {
            Crear();
            pantalla.RecibirCompartido("Bar Pepe");
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(hoja.Abierta, "La hoja se abre del todo.");
            Assert.IsFalse(pantalla.EnEnlace);
            Assert.AreEqual("Bar Pepe", hoja.Campo.value);
            Assert.AreEqual("Bar Pepe", hoja.Filas.First().Resultado.Nombre, "Se busca como un texto escrito.");
            CollectionAssert.Contains(geo.Busquedas, "Bar Pepe", "Y las direcciones, como siempre.");
        }

        [Test]
        public void ConLaFichaDeUnLugarAbierta_SeCierraYSeAbreLaHoja()
        {
            Crear();
            pantalla.AbrirFicha(BarPepe);
            Assert.IsTrue(pantalla.FichaAbierta);
            pantalla.RecibirCompartido("https://maps.google.com/?cid=123");
            Assert.IsFalse(pantalla.FichaAbierta, "La ficha del lugar se cierra.");
            Assert.IsTrue(hoja.Visible);
            Assert.IsTrue(hoja.Abierta);
            Assert.AreEqual("https://maps.google.com/?cid=123", hoja.Campo.value);
        }

        [Test]
        public void ConLaFichaDeUnLugarAbierta_UnaUbicacionAbreSuPunto()
        {
            Crear();
            pantalla.AbrirFicha(BarPepe);
            pantalla.RecibirCompartido("36.7781, -6.3515");
            AssertPuntoAbierto(new Coordenada(36.7781, -6.3515));
        }

        [Test]
        public void ConLaExplicacionAbierta_EsperaAQueSeCierre()
        {
            Crear(explicacion: true);
            Assert.IsTrue(pantalla.ExplicacionAbierta);
            pantalla.RecibirCompartido("geo:36.778,-6.351");
            Assert.IsTrue(pantalla.CompartidoPendiente);
            Assert.IsFalse(hoja.Abierta, "Con la explicación a la vista no se abre la hoja.");
            Assert.AreEqual("", hoja.Campo.value);
            Fotograma();
            Assert.IsTrue(pantalla.CompartidoPendiente, "Mientras sigue abierta, Update no lo aplica.");
            Assert.IsFalse(hoja.Abierta);

            pantalla.AhoraNoUbicacion();
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            Fotograma(); // el siguiente fotograma
            Assert.IsFalse(pantalla.CompartidoPendiente, "Al cerrarse, Update lo aplica.");
            Assert.AreEqual("geo:36.778,-6.351", hoja.Campo.value);
            AssertPuntoAbierto(new Coordenada(36.778, -6.351));
        }

        [Test]
        public void ConLaExplicacionAbierta_TextoCualquieraAbreLaHojaAlCerrarla()
        {
            Crear(explicacion: true);
            pantalla.RecibirCompartido("Bar Pepe");
            pantalla.AhoraNoUbicacion();
            Fotograma();
            Assert.IsFalse(pantalla.CompartidoPendiente);
            Assert.IsTrue(hoja.Abierta);
            Assert.AreEqual("Bar Pepe", hoja.Campo.value);
        }

        [Test]
        public void Vacio_Nada()
        {
            Crear();
            pantalla.RecibirCompartido("");
            pantalla.RecibirCompartido(null);
            pantalla.RecibirCompartido("  \n ");
            Assert.IsFalse(pantalla.CompartidoPendiente);
            Assert.IsFalse(hoja.Abierta);
            Assert.AreEqual("", hoja.Campo.value);
        }

        [Test]
        public void ElMismoEnlaceCortoOtraVez_SeVuelveAComprobar()
        {
            Crear();
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            resolutor.Peticiones.Single().Fallar();
            AssertMensajeEnHoja(ClavesTexto.BuscadorEnlaceNoReconocido);
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            Assert.AreEqual(2, resolutor.Peticiones.Count, "Un corto que falló se reintenta.");
            resolutor.Peticiones[1].Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            AssertPuntoAbierto(new Coordenada(36.77, -6.35));
        }

        [Test]
        public void ConLaListaCerca_ElTextoCompartidoLaQuita()
        {
            Crear();
            hoja.Abrir();
            pantalla.ElegirCategoria(null);
            Assert.IsTrue(pantalla.CercaActiva);
            pantalla.RecibirCompartido("Bar Pepe");
            Assert.IsFalse(pantalla.CercaActiva);
            Assert.AreEqual("Bar Pepe", hoja.Filas.First().Resultado.Nombre);
        }

        [Test]
        public void TalkBack_LaHojaEsModalConElFocoEnElCampo()
        {
            Crear();
            ConLector();
            pantalla.RecibirCompartido("https://maps.google.com/?cid=123");
            Assert.IsTrue(hoja.Abierta);
            Assert.IsTrue(pantalla.Accesibilidad.Modal, "Como AbrirBuscadorConLector: la hoja es modal.");
            Assert.AreSame(hoja.Campo, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(T(ClavesTexto.BuscadorEnlaceNoReconocido), anuncios.LastOrDefault(), "Se anuncia el mensaje.");
        }

        // ───────────────────────── Arranque ─────────────────────────

        [Test]
        public void Arranque_PasaLoCompartidoUnaVezYVolverSinCompartirNoCambiaNada()
        {
            Crear();
            var entrada = new EntradaFalsa();
            entrada.Cola.Enqueue("Bar Pepe");
            PriceMap.Arranque.Arranque.PasarCompartido(entrada, pantalla);
            Assert.AreEqual("Bar Pepe", hoja.Campo.value);

            pantalla.PlegarBuscador();
            PriceMap.Arranque.Arranque.PasarCompartido(entrada, pantalla); // la app vuelve delante: ya se leyó
            Assert.IsFalse(hoja.Abierta, "Nada cambia.");
            Assert.AreEqual("Bar Pepe", hoja.Campo.value);
        }

        [Test]
        public void Arranque_AlVolverDelantePasaLoCompartidoSoloConLaAppDelanteYActiva()
        {
            Crear();
            var entrada = new EntradaFalsa();
            entrada.Cola.Enqueue("Bar Pepe");
            PriceMap.Arranque.Arranque.AlVolverDelante(entrada, pantalla, delante: false, activo: true);
            PriceMap.Arranque.Arranque.AlVolverDelante(entrada, pantalla, delante: true, activo: false);
            Assert.AreEqual(1, entrada.Cola.Count, "Sin foco o con el Arranque inactivo no se lee.");
            Assert.AreEqual("", hoja.Campo.value);

            PriceMap.Arranque.Arranque.AlVolverDelante(entrada, pantalla, delante: true, activo: true);
            Assert.AreEqual(0, entrada.Cola.Count);
            Assert.AreEqual("Bar Pepe", hoja.Campo.value);
            Assert.IsTrue(hoja.Abierta);
        }

        [Test]
        public void Arranque_SinEntradaOSinPantallaNoFalla()
        {
            Crear();
            PriceMap.Arranque.Arranque.PasarCompartido(null, pantalla);
            PriceMap.Arranque.Arranque.PasarCompartido(new EntradaFalsa(), null);
            Assert.IsNull(new PriceMap.Adaptadores.Sistema.EntradaCompartidaAndroid().Tomar(), "Fuera de Android no hay nada.");
        }
    }
}
