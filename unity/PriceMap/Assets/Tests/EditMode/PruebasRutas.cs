using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
using PriceMap.Adaptadores.Rutas;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Rutas y tiempos de la ficha (historia 2.6, parte 1): polilínea, lectores de Valhalla y OSRM con JSON en línea,
    /// URL y cuerpo de las peticiones, ServicioRutas (dos modos, reserva, 8 s, caché y cancelación) con servicios
    /// falsos, las alternativas (parte 5a de «Cómo llegar»), y la fila de acciones en la pantalla real, una prueba por fila de la matriz. Nada sale a la red.
    /// </summary>
    public class PruebasRutas
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        const float Escala = 0.5f;

        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        static readonly Coordenada CalleAncha = new Coordenada(36.77814, -6.35148);

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        // ───────────────────────── Falsos ─────────────────────────

        class Peticion
        {
            public Coordenada Origen, Destino;
            public ModoViaje Modo;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<Ruta>> Tcs;

            public void Responder(double segundos, double metros = 1000) =>
                Tcs.TrySetResult(Resultado<Ruta>.Exito(new Ruta(new[] { Origen, Destino }, metros, segundos, null)));

            public void Fallar() => Tcs.TrySetResult(Resultado<Ruta>.Fallo("sin conexión"));

            /// <summary>Respuesta de Valhalla: por tres puntos, con la salida, un giro y la llegada en castellano.</summary>
            public void ResponderConManiobras(double segundos, double metros)
            {
                var medio = new Coordenada((Origen.Latitud + Destino.Latitud) / 2, Origen.Longitud);
                var maniobras = new[]
                {
                    new Maniobra(TipoManiobra.Salida, "Sal hacia el norte por Calle Ancha.", Origen, 300),
                    new Maniobra(TipoManiobra.Derecha, "Gira a la derecha en Calle Santo Domingo.", medio, 900),
                    new Maniobra(TipoManiobra.Llegada, "Has llegado a tu destino.", Destino, 0),
                };
                Tcs.TrySetResult(Resultado<Ruta>.Exito(new Ruta(new[] { Origen, medio, Destino }, metros, segundos, maniobras)));
            }
        }

        /// <summary>Servicio de rutas falso: cada petición queda pendiente hasta que la prueba responde.</summary>
        class RutasFalsas : IRutas
        {
            public bool IgnorarCancelacion;
            public readonly List<Peticion> Peticiones = new List<Peticion>();

            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                var p = new Peticion { Origen = origen, Destino = destino, Modo = modo, Ct = ct, Tcs = new TaskCompletionSource<Resultado<Ruta>>() };
                Peticiones.Add(p);
                if (!IgnorarCancelacion) ct.Register(() => p.Tcs.TrySetCanceled());
                return p.Tcs.Task;
            }

            public Peticion De(ModoViaje modo) => Peticiones.Last(p => p.Modo == modo);
        }

        /// <summary>Esperas falsas: no vencen hasta que la prueba lo dice (los 8 s).</summary>
        class Esperas
        {
            public readonly List<(TimeSpan duracion, TaskCompletionSource<bool> tcs)> Lista = new List<(TimeSpan, TaskCompletionSource<bool>)>();

            public Task Esperar(TimeSpan duracion, CancellationToken ct)
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                Lista.Add((duracion, tcs));
                return tcs.Task;
            }

            public void VencerTodas() { foreach (var (_, tcs) in Lista.ToList()) tcs.TrySetResult(true); }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        /// <summary>Ubicación que la prueba cambia a mano (estado y posición).</summary>
        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; }
            public Coordenada? Posicion { get; set; }
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
            public Action<PuntoPantalla> pulsacion;
            public readonly Dictionary<(float, float), Coordenada> suelo = new Dictionary<(float, float), Coordenada>();
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public Func<Coordenada, PuntoPantalla> proyectar;
            public PuntoPantalla Proyectar(Coordenada punto) => proyectar?.Invoke(punto) ?? PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add => pulsacion += value; remove => pulsacion -= value; }
            public Coordenada? Desproyectar(PuntoPantalla punto) => suelo.TryGetValue((punto.X, punto.Y), out var c) ? c : (Coordenada?)null;
            /// <summary>Línea que se ve ahora (null sin ruta), cuántas veces se ha dibujado y los encuadres pedidos.</summary>
            public IReadOnlyList<Coordenada> linea;
            public int dibujadas;
            public readonly List<(IReadOnlyList<Coordenada> puntos, RectPantalla libre)> encuadres = new List<(IReadOnlyList<Coordenada>, RectPantalla)>();
            public bool aproximada;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { linea = puntos; dibujadas++; this.aproximada = aproximada; }
            public void QuitarRuta() => linea = null;
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) => encuadres.Add((puntos, libre));
        }

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        static Lugar Areas => Lugares().Lugares.First(l => l.Id == "clinica-areas");

        SynchronizationContext contextoAntes;
        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        RutasFalsas valhalla, osrm;
        Esperas esperas;
        DateTime reloj;
        ServicioRutas servicio;
        UbicacionFalsa gps;
        readonly List<(ModoViaje modo, Coordenada destino, Resultado<Ruta> r)> avisos = new List<(ModoViaje, Coordenada, Resultado<Ruta>)>();
        readonly List<string> anuncios = new List<string>();

        FichaLugar Ficha => pantalla.Ficha;
        FilaAcciones Fila => pantalla.Ficha.Acciones;
        AccesibilidadPantalla Acc => pantalla.Accesibilidad;

        [SetUp]
        public void Preparar()
        {
            // Sin contexto de sincronización, la continuación corre en cuanto la prueba responde la petición falsa
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            valhalla = new RutasFalsas();
            osrm = new RutasFalsas();
            esperas = new Esperas();
            reloj = Ahora;
            servicio = new ServicioRutas(valhalla, osrm, esperas.Esperar, () => reloj);
            servicio.TiempoCalculado += (m, d, r) => avisos.Add((m, d, r));
            avisos.Clear();
            anuncios.Clear();
        }

        void Anotar(string texto) => anuncios.Add(texto);

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = null;
            AccesibilidadPantalla.AnuncioPedido -= Anotar;
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        /// <summary>La pantalla real con el servicio de rutas (o sin él) y una ubicación en ese estado.</summary>
        void Crear(EstadoUbicacion estado, Coordenada? posicion, bool conRutas = true, IGeocodificador geocodificador = null)
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            gps = new UbicacionFalsa { Estado = estado, Posicion = posicion };
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, new ServicioUbicacion(gps, prefs),
                geocodificador: geocodificador, rutas: conRutas ? servicio : null);
            pantalla.RelojLocal = () => HoraLocal;
            raizArbol = arbol.Q("raiz");
            tema.Iniciar();
            filtro.Iniciar();
        }

        static readonly DateTime HoraLocal = new DateTime(2026, 11, 20, 13, 30, 0, DateTimeKind.Local);
        VisualElement raizArbol;

        void CrearConPosicion() => Crear(EstadoUbicacion.Activa, Cabildo);

        // ───────────────────────── Modelo y polilínea ─────────────────────────

        [Test]
        public void Polilinea_EjemploDeGoogleConPrecision5()
        {
            var p = Polilinea.Decodificar("_p~iF~ps|U_ulLnnqC_mqNvxq`@", 5);
            Assert.AreEqual(3, p.Count);
            AreEqual(new Coordenada(38.5, -120.2), p[0]);
            AreEqual(new Coordenada(40.7, -120.95), p[1]);
            AreEqual(new Coordenada(43.252, -126.453), p[2]);
        }

        [Test]
        public void Polilinea_Polyline6ConocidaDaLasCoordenadasEsperadas()
        {
            var p = Polilinea.Decodificar(Linea6, 6);
            Assert.AreEqual(3, p.Count);
            AreEqual(new Coordenada(36.7779, -6.3528), p[0]);
            AreEqual(new Coordenada(36.77814, -6.35148), p[1]);
            AreEqual(new Coordenada(36.778512, -6.350911), p[2]);
        }

        /// <summary>(36.7779, -6.3528), (36.77814, -6.35148), (36.778512, -6.350911) en polyline6.</summary>
        const string Linea6 = "wyvceA~xvbK_NoqAgVqb@";

        [Test]
        public void Polilinea_VaciaYRota()
        {
            Assert.AreEqual(0, Polilinea.Decodificar("", 6).Count);
            Assert.AreEqual(0, Polilinea.Decodificar(null, 6).Count);
            Assert.Throws<FormatException>(() => Polilinea.Decodificar("_p~iF~ps|U_", 5), "Cortada a medias.");
            Assert.Throws<FormatException>(() => Polilinea.Decodificar("_p~iF~ps|U\u0001", 5), "Carácter fuera de rango.");
        }

        static void AreEqual(Coordenada esperada, Coordenada real)
        {
            Assert.AreEqual(esperada.Latitud, real.Latitud, 1e-9);
            Assert.AreEqual(esperada.Longitud, real.Longitud, 1e-9);
        }

        [Test]
        public void LosMinutosSeRedondeanHaciaArribaConUnoComoMinimo()
        {
            Assert.AreEqual(1, Ruta.MinutosDe(0));
            Assert.AreEqual(1, Ruta.MinutosDe(10));
            Assert.AreEqual(1, Ruta.MinutosDe(60));
            Assert.AreEqual(2, Ruta.MinutosDe(61));
            Assert.AreEqual(6, Ruta.MinutosDe(330));
            Assert.AreEqual(6, Ruta.MinutosDe(360));
            Assert.AreEqual(15, Ruta.MinutosDe(900));
        }

        [Test]
        public void LosParametrosEstanEnParametrosNegocio()
        {
            Assert.AreEqual(8, ParametrosNegocio.RutaTiempoMaximoSegundos);
            Assert.AreEqual(20.0, ParametrosNegocio.RutaCacheMetros);
            Assert.AreEqual(2, ParametrosNegocio.RutaCacheMinutos);
            Assert.AreEqual(3, ParametrosNegocio.RutaAlternativasMaximo);
            Assert.AreEqual(0.02, ParametrosNegocio.RutaAlternativasMargen);
            Assert.Less(ParametrosNegocio.RutaAlternativasPlazoSegundos, ParametrosNegocio.RutaTiempoMaximoSegundos);
            Assert.AreEqual(TimeSpan.FromSeconds(8), ServicioRutas.TiempoMaximo);
            Assert.AreEqual(TimeSpan.FromMinutes(2), ServicioRutas.DuracionCache);
        }

        // ───────────────────────── Peticiones ─────────────────────────

        static readonly Coordenada OrigenExacto = new Coordenada(36.7779123456, -6.3528123456);
        static readonly Coordenada DestinoExacto = new Coordenada(36.7781412345, -6.3514812345);

        [Test]
        public void Valhalla_CuerpoConLaMasRapidaEnCocheIdiomaManiobrasYPrecisiones()
        {
            Assert.AreEqual("https://valhalla1.openstreetmap.de/route", RutasValhalla.Url);
            Assert.AreEqual(
                "{\"locations\":[{\"lat\":36.77791,\"lon\":-6.35281},{\"lat\":36.778141,\"lon\":-6.351481}]," +
                "\"costing\":\"auto\",\"costing_options\":{\"auto\":{\"shortest\":false}},\"language\":\"es-ES\"," +
                "\"directions_type\":\"instructions\",\"units\":\"kilometers\"}",
                RutasValhalla.Cuerpo(OrigenExacto, DestinoExacto, ModoViaje.Coche),
                "Origen con 5 decimales y destino con 6.");
            var aPie = RutasValhalla.Cuerpo(OrigenExacto, DestinoExacto, ModoViaje.APie);
            StringAssert.Contains("\"costing\":\"pedestrian\"", aPie);
            StringAssert.Contains("\"costing_options\":{\"pedestrian\":{\"shortest\":true}}", aPie);
            StringAssert.DoesNotContain("alternates", aPie);
            var varias = RutasValhalla.Cuerpo(OrigenExacto, DestinoExacto, ModoViaje.Coche, 2);
            StringAssert.Contains("\"shortest\":false", varias);
            StringAssert.Contains("\"alternates\":2,", varias);
            StringAssert.Contains("\"costing_options\":{\"auto\":{\"shortest\":true}}",
                RutasValhalla.Cuerpo(OrigenExacto, DestinoExacto, ModoViaje.Coche, laMasCorta: true));
        }

        [Test]
        public void Osrm_UrlConPerfilAlternativasPasosYPolyline6()
        {
            Assert.AreEqual(
                "https://routing.openstreetmap.de/routed-car/route/v1/driving/-6.35281,36.77791;-6.351481,36.778141" +
                "?alternatives=3&steps=true&overview=full&geometries=polyline6",
                RutasOsrm.Url(OrigenExacto, DestinoExacto, ModoViaje.Coche));
            Assert.AreEqual(
                "https://routing.openstreetmap.de/routed-foot/route/v1/foot/-6.35281,36.77791;-6.351481,36.778141" +
                "?alternatives=3&steps=true&overview=full&geometries=polyline6",
                RutasOsrm.Url(OrigenExacto, DestinoExacto, ModoViaje.APie));
        }

        // ───────────────────────── Lectores ─────────────────────────

        const string RespuestaValhalla = @"{""trip"":{""locations"":[{""type"":""break"",""lat"":36.7779,""lon"":-6.3528},
            {""type"":""break"",""lat"":36.778512,""lon"":-6.350911}],
            ""legs"":[{""maneuvers"":[
              {""type"":1,""instruction"":""Conduce hacia el este por Plaza del Cabildo."",""time"":20.1,""length"":0.12,""begin_shape_index"":0,""end_shape_index"":1},
              {""type"":10,""instruction"":""Gira a la derecha en Calle Ancha."",""time"":30.5,""length"":0.06,""begin_shape_index"":1,""end_shape_index"":2},
              {""type"":4,""instruction"":""Has llegado a tu destino."",""time"":0,""length"":0,""begin_shape_index"":2,""end_shape_index"":2}],
              ""summary"":{""length"":0.18,""time"":50.6},
              ""shape"":""wyvceA~xvbK_NoqAgVqb@""}],
            ""summary"":{""length"":0.18,""time"":50.6},""status_message"":""Found route between points"",""status"":0,""units"":""kilometers"",""language"":""es-ES""}}";

        [Test]
        public void LecturaValhalla_LineaDistanciaTiempoYManiobrasConTexto()
        {
            var r = LecturaValhalla.Leer(RespuestaValhalla);
            Assert.IsTrue(r.Ok, r.Error);
            var ruta = r.Valor;
            Assert.AreEqual(3, ruta.Puntos.Count);
            AreEqual(new Coordenada(36.778512, -6.350911), ruta.Puntos[2]);
            Assert.AreEqual(180.0, ruta.Metros, 1e-6, "Kilómetros a metros.");
            Assert.AreEqual(50.6, ruta.Segundos, 1e-6);
            Assert.AreEqual(1, ruta.Minutos);
            Assert.AreEqual(3, ruta.Maniobras.Count);
            Assert.AreEqual(TipoManiobra.Salida, ruta.Maniobras[0].Tipo);
            Assert.AreEqual(TipoManiobra.Derecha, ruta.Maniobras[1].Tipo);
            Assert.AreEqual("Gira a la derecha en Calle Ancha.", ruta.Maniobras[1].Texto);
            AreEqual(new Coordenada(36.77814, -6.35148), ruta.Maniobras[1].Posicion);
            Assert.AreEqual(60.0, ruta.Maniobras[1].Metros, 1e-6);
            Assert.AreEqual(TipoManiobra.Llegada, ruta.Maniobras[2].Tipo);
        }

        [Test]
        public void LecturaValhalla_ErrorOVacioEsFallo()
        {
            Assert.IsFalse(LecturaValhalla.Leer(@"{""error_code"":442,""error"":""No path could be found for input"",""status_code"":400,""status"":""Bad Request""}").Ok);
            Assert.IsFalse(LecturaValhalla.Leer("").Ok);
            Assert.IsFalse(LecturaValhalla.Leer("no es json").Ok);
            Assert.IsFalse(LecturaValhalla.Leer(@"{""trip"":{""legs"":[{""shape"":""_p~iF~ps|U_""}]}}").Ok, "Línea rota.");
        }

        [Test]
        public void LecturaValhalla_TiposDeManiobra()
        {
            Assert.AreEqual(TipoManiobra.Salida, LecturaValhalla.Tipo(2));
            Assert.AreEqual(TipoManiobra.Llegada, LecturaValhalla.Tipo(6));
            Assert.AreEqual(TipoManiobra.Recto, LecturaValhalla.Tipo(8));
            Assert.AreEqual(TipoManiobra.LigeroIzquierda, LecturaValhalla.Tipo(16));
            Assert.AreEqual(TipoManiobra.Izquierda, LecturaValhalla.Tipo(15));
            Assert.AreEqual(TipoManiobra.CambioSentido, LecturaValhalla.Tipo(12));
            Assert.AreEqual(TipoManiobra.Rotonda, LecturaValhalla.Tipo(26));
            Assert.AreEqual(TipoManiobra.SalidaRotonda, LecturaValhalla.Tipo(27));
            Assert.AreEqual(TipoManiobra.Otra, LecturaValhalla.Tipo(28));
        }

        // Tres alternativas: la primera más corta pero más lenta, la segunda la más rápida y la tercera con la línea rota
        const string RespuestaOsrm = @"{""code"":""Ok"",""routes"":[
            {""geometry"":""_p~iF~ps|U_ulLnnqC"",""legs"":[{""steps"":[]}],""distance"":150.2,""duration"":320.0,""weight"":320},
            {""geometry"":""wyvceA~xvbK_NoqAgVqb@"",""legs"":[{""steps"":[
              {""geometry"":""x"",""maneuver"":{""bearing_after"":90,""location"":[-6.3528,36.7779],""type"":""depart""},""distance"":120.4,""duration"":20,""name"":""Plaza del Cabildo""},
              {""geometry"":""x"",""maneuver"":{""location"":[-6.35148,36.77814],""modifier"":""right"",""type"":""turn""},""distance"":60.1,""duration"":30,""name"":""Calle Ancha""},
              {""geometry"":""x"",""maneuver"":{""location"":[-6.350911,36.778512],""type"":""arrive""},""distance"":0,""duration"":0,""name"":""""}],
              ""distance"":180.5,""duration"":150.0}],""distance"":180.5,""duration"":150.0,""weight"":150},
            {""geometry"":""_p~iF~ps|U_"",""legs"":[],""distance"":100,""duration"":10,""weight"":10}],
            ""waypoints"":[]}";

        [Test]
        public void LecturaOsrm_SeQuedaLaAlternativaMasRapidaConManiobrasSinTexto()
        {
            var r = LecturaOsrm.Leer(RespuestaOsrm);
            Assert.IsTrue(r.Ok, r.Error);
            var ruta = r.Valor;
            Assert.AreEqual(150.0, ruta.Segundos, 1e-6, "La de menos tiempo, aunque sea más larga.");
            Assert.AreEqual(180.5, ruta.Metros, 1e-6);
            Assert.AreEqual(3, ruta.Puntos.Count);
            Assert.AreEqual(3, ruta.Maniobras.Count);
            Assert.AreEqual(TipoManiobra.Salida, ruta.Maniobras[0].Tipo);
            Assert.AreEqual(TipoManiobra.Derecha, ruta.Maniobras[1].Tipo);
            Assert.AreEqual(TipoManiobra.Llegada, ruta.Maniobras[2].Tipo);
            Assert.IsTrue(ruta.Maniobras.All(m => m.Texto == ""), "Sin texto: los textos son de la historia 2.7.");
            AreEqual(new Coordenada(36.77814, -6.35148), ruta.Maniobras[1].Posicion);
            Assert.AreEqual(60.1, ruta.Maniobras[1].Metros, 1e-6);
        }

        [Test]
        public void LecturaOsrm_SinRutaEsFallo()
        {
            Assert.IsFalse(LecturaOsrm.Leer(@"{""code"":""NoRoute"",""message"":""Impossible route between points"",""routes"":[]}").Ok);
            Assert.IsFalse(LecturaOsrm.Leer(@"{""code"":""Ok"",""routes"":[]}").Ok);
            Assert.IsFalse(LecturaOsrm.Leer("").Ok);
        }

        [Test]
        public void LecturaOsrm_TiposYModificadores()
        {
            Assert.AreEqual(TipoManiobra.Izquierda, LecturaOsrm.Tipo("turn", "left"));
            Assert.AreEqual(TipoManiobra.LigeroDerecha, LecturaOsrm.Tipo("fork", "slight right"));
            Assert.AreEqual(TipoManiobra.CerradoIzquierda, LecturaOsrm.Tipo("turn", "sharp left"));
            Assert.AreEqual(TipoManiobra.CambioSentido, LecturaOsrm.Tipo("continue", "uturn"));
            Assert.AreEqual(TipoManiobra.Recto, LecturaOsrm.Tipo("new name", "straight"));
            Assert.AreEqual(TipoManiobra.Recto, LecturaOsrm.Tipo("new name", null));
            Assert.AreEqual(TipoManiobra.Rotonda, LecturaOsrm.Tipo("roundabout", "right"));
            Assert.AreEqual(TipoManiobra.SalidaRotonda, LecturaOsrm.Tipo("exit roundabout", "right"));
            Assert.AreEqual(TipoManiobra.Incorporacion, LecturaOsrm.Tipo("merge", "slight left"));
            Assert.AreEqual(TipoManiobra.Otra, LecturaOsrm.Tipo("notification", null));
        }

        // ───────────────────────── ServicioRutas ─────────────────────────

        [Test]
        public void Servicio_PideCocheYAPieALaVezYAvisaDeCadaUno()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            Assert.AreEqual(2, valhalla.Peticiones.Count, "Los dos modos a la vez.");
            CollectionAssert.AreEquivalent(new[] { ModoViaje.Coche, ModoViaje.APie }, valhalla.Peticiones.Select(p => p.Modo));
            Assert.IsTrue(servicio.Pendiente);

            valhalla.De(ModoViaje.APie).Responder(900);
            valhalla.De(ModoViaje.Coche).Responder(330);
            Assert.AreEqual(2, avisos.Count);
            Assert.AreEqual(15, avisos.Single(a => a.modo == ModoViaje.APie).r.Valor.Minutos);
            Assert.AreEqual(6, avisos.Single(a => a.modo == ModoViaje.Coche).r.Valor.Minutos);
            Assert.IsTrue(avisos.All(a => a.destino == CalleAncha));
            Assert.IsFalse(servicio.Pendiente);
            Assert.IsEmpty(osrm.Peticiones, "Sin fallos, OSRM no se usa.");
            Assert.AreEqual(6, servicio.Ultima(ModoViaje.Coche).Minutos);
        }

        [Test]
        public void Servicio_ValhallaFallaYRespondeOsrm()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            valhalla.De(ModoViaje.Coche).Fallar();
            Assert.IsEmpty(avisos, "Aún queda OSRM.");
            Assert.AreEqual(ModoViaje.Coche, osrm.Peticiones.Single().Modo);
            osrm.De(ModoViaje.Coche).Responder(400);
            Assert.AreEqual(7, avisos.Single().r.Valor.Minutos);
        }

        [Test]
        public void Servicio_ValhallaTardaOchoSegundosYPasaAOsrm()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            Assert.AreEqual(2, esperas.Lista.Count);
            Assert.IsTrue(esperas.Lista.All(e => e.duracion == TimeSpan.FromSeconds(8)));
            var coche = valhalla.De(ModoViaje.Coche);
            esperas.Lista[0].tcs.TrySetResult(true); // vencen los 8 s del primero (el coche)
            Assert.IsTrue(coche.Ct.IsCancellationRequested, "Se cancela la petición a Valhalla.");
            Assert.AreEqual(ModoViaje.Coche, osrm.Peticiones.Single().Modo);
            osrm.Peticiones.Single().Responder(120);
            Assert.AreEqual(2, avisos.Single().r.Valor.Minutos);
            coche.Responder(60); // respuesta tardía de Valhalla: ya no cuenta
            Assert.AreEqual(1, avisos.Count);
        }

        [Test]
        public void Servicio_LosDosCaenYElModoQuedaSinTiempo()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            valhalla.De(ModoViaje.APie).Fallar();
            osrm.De(ModoViaje.APie).Fallar();
            var aviso = avisos.Single();
            Assert.AreEqual(ModoViaje.APie, aviso.modo);
            Assert.IsFalse(aviso.r.Ok);
            Assert.IsNull(servicio.Ultima(ModoViaje.APie));
            Assert.AreEqual(0, servicio.EnCache, "Los fallos no se guardan.");
        }

        [Test]
        public void Servicio_CacheDosMinutosSinMoverse20Metros()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            Assert.AreEqual(2, servicio.EnCache);

            // 1 min después y 10 m más al norte: de la caché, al momento y sin peticiones
            reloj = Ahora.AddMinutes(1);
            avisos.Clear();
            var cerca = new Coordenada(Cabildo.Latitud + 10.0 / 111195.0, Cabildo.Longitud);
            servicio.PedirTiempos(cerca, CalleAncha);
            Assert.AreEqual(2, valhalla.Peticiones.Count, "Sin peticiones nuevas.");
            Assert.AreEqual(2, avisos.Count, "Los dos tiempos llegan ya.");

            // 30 m más allá: se pide de nuevo
            var lejos = new Coordenada(Cabildo.Latitud + 30.0 / 111195.0, Cabildo.Longitud);
            servicio.PedirTiempos(lejos, CalleAncha);
            Assert.AreEqual(4, valhalla.Peticiones.Count);

            // Otro destino: también
            servicio.PedirTiempos(Cabildo, Areas.Posicion);
            Assert.AreEqual(6, valhalla.Peticiones.Count);

            // Pasados 2 min, la caché ya no vale
            reloj = Ahora.AddMinutes(2);
            servicio.PedirTiempos(Cabildo, CalleAncha);
            Assert.AreEqual(8, valhalla.Peticiones.Count);
        }

        [Test]
        public void Servicio_CancelarCancelaLasPeticionesYNoAvisa()
        {
            valhalla.IgnorarCancelacion = true;
            servicio.PedirTiempos(Cabildo, CalleAncha);
            servicio.Cancelar();
            Assert.IsTrue(valhalla.Peticiones.All(p => p.Ct.IsCancellationRequested));
            Assert.IsFalse(servicio.Pendiente);
            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            Assert.IsEmpty(avisos, "Una respuesta tardía no avisa.");
        }

        [Test]
        public void Servicio_UnaPeticionNuevaDejaSinAvisoALaAnterior()
        {
            valhalla.IgnorarCancelacion = true;
            servicio.PedirTiempos(Cabildo, CalleAncha);
            var viejas = valhalla.Peticiones.ToList();
            servicio.PedirTiempos(Cabildo, Areas.Posicion);
            foreach (var p in viejas) p.Responder(60);
            Assert.IsEmpty(avisos);
            valhalla.De(ModoViaje.Coche).Responder(330);
            Assert.AreEqual(Areas.Posicion, avisos.Single().destino);
        }

        [Test]
        public void CerrarLaFichaSinIr_NoHayRuta()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).Responder(330);
            pantalla.CerrarFicha();
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsNull(mapa.linea);
            Assert.IsTrue(pantalla.Buscador.Visible, "Vuelve la cápsula, no las tarjetas.");
            Assert.AreEqual(DisplayStyle.None, pantalla.TarjetaRuta.style.display.value);
        }

        // ───────────────────────── Varias rutas (parte 5a de «Cómo llegar») ─────────────────────────

        static Ruta R(double metros, double segundos, bool aproximada = false) =>
            new Ruta(new[] { Cabildo, CalleAncha }, metros, segundos, null, aproximada);

        static void Son(IEnumerable<Ruta> rutas, params (double metros, double segundos)[] esperadas) =>
            CollectionAssert.AreEqual(esperadas.Select(e => $"{e.metros:0} m {e.segundos:0} s"), rutas.Select(x => $"{x.Metros:0} m {x.Segundos:0} s"));

        [Test]
        public void ReglaAlternativas_OrdenaPorTiempoQuitaRepetidasYLimita()
        {
            Son(ReglaAlternativas.Juntar(new[] { R(9700, 2825), null, R(10360, 1319) }), (10360, 1319), (9700, 2825));
            Son(ReglaAlternativas.Juntar(new[] { R(1000, 100), R(980, 102) }), (1000, 100)); // justo el 2 %
            Son(ReglaAlternativas.Juntar(new[] { R(1000, 100), R(970, 100) }), (970, 100), (1000, 100)); // otros metros
            Son(ReglaAlternativas.Juntar(new[] { R(1000, 100), R(1000, 103) }), (1000, 100), (1000, 103)); // otro tiempo
            Assert.IsEmpty(ReglaAlternativas.Juntar(null));
        }

        [Test]
        public void ReglaAlternativas_SiSobranLaMasCortaSigueEntreLasOpciones()
        {
            Son(ReglaAlternativas.Juntar(new[] { R(5000, 400), R(8000, 100), R(7000, 200), R(6000, 300) }), (8000, 100), (7000, 200), (5000, 400));
            Son(ReglaAlternativas.Juntar(new[] { R(5000, 100), R(8000, 400), R(7000, 200), R(6000, 300) }), (5000, 100), (7000, 200), (6000, 300));
        }

        /// <summary>Un viaje de Valhalla (línea legible o rota) con esos metros y segundos.</summary>
        static string Viaje(double metros, double segundos, string linea = Linea6) =>
            "{\"legs\":[{\"maneuvers\":[],\"shape\":\"" + linea + "\"}],\"summary\":{\"length\":"
            + (metros / 1000.0).ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"time\":"
            + segundos.ToString(System.Globalization.CultureInfo.InvariantCulture) + "},\"units\":\"kilometers\"}";

        static string RespuestaVarias(string viaje, params string[] alternativas) =>
            "{\"trip\":" + viaje + (alternativas.Length == 0 ? "" :
                ",\"alternates\":[" + string.Join(",", alternativas.Select(a => "{\"trip\":" + a + "}")) + "]") + "}";

        const string LineaRota = "_p~iF~ps|U_";

        /// <summary>El servidor de Valhalla o de OSRM: cada petición queda pendiente hasta que la prueba responde.</summary>
        class ServidorFalso
        {
            public bool IgnorarCancelacion;
            public readonly List<(string texto, CancellationToken ct, TaskCompletionSource<Resultado<string>> tcs)> Peticiones =
                new List<(string, CancellationToken, TaskCompletionSource<Resultado<string>>)>();

            public Task<Resultado<string>> Pedir(string texto, CancellationToken ct)
            {
                var tcs = new TaskCompletionSource<Resultado<string>>();
                Peticiones.Add((texto, ct, tcs));
                if (!IgnorarCancelacion) ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            }

            public void Responder(string json) => Peticiones.Last().tcs.TrySetResult(Resultado<string>.Exito(json));
            public void Fallar() => Peticiones.Last().tcs.TrySetResult(Resultado<string>.Fallo("sin conexión"));
        }

        readonly ServidorFalso servidor = new ServidorFalso();
        Esperas plazos;

        Task<Resultado<IReadOnlyList<Ruta>>> PedirAValhalla(ModoViaje modo, CancellationToken ct = default)
        {
            servidor.Peticiones.Clear();
            plazos = new Esperas();
            return new RutasValhalla(servidor.Pedir, plazos.Esperar).Alternativas(Cabildo, CalleAncha, modo, ct);
        }

        [Test]
        public void Alternativas_CocheConTres_UnaSolaPeticionYPorTiempo()
        {
            var t = PedirAValhalla(ModoViaje.Coche);
            StringAssert.Contains("\"alternates\":2,", servidor.Peticiones.Single().texto);
            StringAssert.Contains("\"shortest\":false", servidor.Peticiones.Single().texto);
            servidor.Responder(RespuestaVarias(Viaje(10000, 1300), Viaje(9000, 2000), Viaje(9500, 1200)));
            Assert.IsTrue(t.Result.Ok, t.Result.Error);
            Son(t.Result.Valor, (9500, 1200), (10000, 1300), (9000, 2000));
            Assert.AreEqual(1, servidor.Peticiones.Count);
            Assert.AreEqual(3, t.Result.Valor[0].Puntos.Count, "Cada alternativa con su línea.");
        }

        [Test]
        public void Alternativas_CocheConUna_LaSegundaDetrasAnadeLaMasCorta()
        {
            var t = PedirAValhalla(ModoViaje.Coche);
            Assert.AreEqual(1, servidor.Peticiones.Count, "Detrás, no a la vez.");
            servidor.Responder(RespuestaVarias(Viaje(10360, 1319)));
            Assert.IsFalse(t.IsCompleted);
            Assert.AreEqual(2, servidor.Peticiones.Count);
            StringAssert.Contains("\"shortest\":true", servidor.Peticiones[1].texto);
            StringAssert.DoesNotContain("alternates", servidor.Peticiones[1].texto);
            servidor.Responder(RespuestaVarias(Viaje(9700, 2825)));
            Son(t.Result.Valor, (10360, 1319), (9700, 2825));
            Assert.AreEqual(2, servidor.Peticiones.Count);
        }

        [Test]
        public void Alternativas_CocheConDos_LaSegundaPoneLaMasCortaEnSuSitioPorTiempo()
        {
            foreach (var segundosCorta in new[] { 2825, 1600 })
            {
                var t = PedirAValhalla(ModoViaje.Coche);
                servidor.Responder(RespuestaVarias(Viaje(10360, 1319), Viaje(11000, 2000)));
                Assert.AreEqual(2, servidor.Peticiones.Count);
                StringAssert.Contains("\"shortest\":true", servidor.Peticiones[1].texto);
                servidor.Responder(RespuestaVarias(Viaje(9700, segundosCorta)));
                if (segundosCorta == 2825) Son(t.Result.Valor, (10360, 1319), (11000, 2000), (9700, 2825));
                else Son(t.Result.Valor, (10360, 1319), (9700, 1600), (11000, 2000));
            }
        }

        [Test]
        public void Alternativas_CocheConTresYDosRepetidas_TambienSePideLaMasCorta()
        {
            var t = PedirAValhalla(ModoViaje.Coche);
            servidor.Responder(RespuestaVarias(Viaje(10000, 1300), Viaje(10050, 1310), Viaje(11000, 2000)));
            Assert.AreEqual(2, servidor.Peticiones.Count, "Solo dos distintas.");
            StringAssert.Contains("\"shortest\":true", servidor.Peticiones[1].texto);
            servidor.Responder(RespuestaVarias(Viaje(9000, 2500)));
            Son(t.Result.Valor, (10000, 1300), (11000, 2000), (9000, 2500));
        }

        [Test]
        public void Alternativas_LaCortaEsLaMisma_UnaRuta()
        {
            var t = PedirAValhalla(ModoViaje.Coche);
            servidor.Responder(RespuestaVarias(Viaje(1000, 200)));
            servidor.Responder(RespuestaVarias(Viaje(990, 202)));
            Son(t.Result.Valor, (1000, 200));
        }

        [Test]
        public void Alternativas_LaSegundaFalla_LasDeLaPrimera()
        {
            var t = PedirAValhalla(ModoViaje.Coche);
            servidor.Responder(RespuestaVarias(Viaje(10000, 1300), Viaje(9000, 2000)));
            Assert.AreEqual(2, servidor.Peticiones.Count);
            servidor.Fallar();
            Assert.IsTrue(t.Result.Ok);
            Son(t.Result.Valor, (10000, 1300), (9000, 2000));

            t = PedirAValhalla(ModoViaje.Coche);
            servidor.Responder(RespuestaVarias(Viaje(10000, 1300)));
            Assert.AreEqual(2, servidor.Peticiones.Count);
            servidor.Peticiones.Last().tcs.TrySetException(new InvalidOperationException("rota"));
            Assert.IsTrue(t.Result.Ok);
            Son(t.Result.Valor, (10000, 1300));
        }

        [Test]
        public void Alternativas_LaSegundaNoRespondeATiempo_LasDeLaPrimera()
        {
            var t = PedirAValhalla(ModoViaje.Coche);
            Assert.AreEqual(TimeSpan.FromSeconds(7), plazos.Lista.Single().duracion, "El plazo se cuenta desde el principio.");
            servidor.Responder(RespuestaVarias(Viaje(10000, 1300)));
            plazos.VencerTodas();
            Assert.IsTrue(t.Result.Ok);
            Son(t.Result.Valor, (10000, 1300));
            Assert.IsTrue(servidor.Peticiones[1].ct.IsCancellationRequested, "La segunda se corta.");

            // El plazo vence durante la primera: ya no hay segunda
            t = PedirAValhalla(ModoViaje.Coche);
            plazos.VencerTodas();
            servidor.Responder(RespuestaVarias(Viaje(10000, 1300)));
            Son(t.Result.Valor, (10000, 1300));
            Assert.AreEqual(1, servidor.Peticiones.Count);
        }

        [Test]
        public void Alternativas_LaSegundaNoResponde_ElServicioNoPasaALaReserva()
        {
            var plazo = new Esperas();
            var otro = new ServicioRutas(new RutasValhalla(servidor.Pedir, plazo.Esperar), osrm, esperas.Esperar, () => reloj);
            servidor.Peticiones.Clear();
            var t = otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None);
            servidor.Responder(RespuestaVarias(Viaje(10000, 1300)));
            plazo.VencerTodas(); // a los 7 s
            esperas.VencerTodas(); // a los 8 s: ya no cuenta
            Son(t.Result.Valor, (10000, 1300));
            Assert.IsEmpty(osrm.Peticiones);
        }

        [Test]
        public void Alternativas_APie_PorTiempoYNuncaSegundaPeticion()
        {
            var t = PedirAValhalla(ModoViaje.APie);
            StringAssert.Contains("\"pedestrian\":{\"shortest\":true}", servidor.Peticiones.Single().texto);
            StringAssert.Contains("\"alternates\":2,", servidor.Peticiones.Single().texto);
            servidor.Responder(RespuestaVarias(Viaje(1000, 800), Viaje(1100, 700)));
            Son(t.Result.Valor, (1100, 700), (1000, 800));
            Assert.AreEqual(1, servidor.Peticiones.Count);
            Assert.IsEmpty(plazos.Lista);
        }

        [Test]
        public void Alternativas_UnaRota_SeSaltaYLasDemasValen()
        {
            var t = PedirAValhalla(ModoViaje.APie);
            servidor.Responder(RespuestaVarias(Viaje(1000, 800), Viaje(900, 700, LineaRota), Viaje(1200, 600)));
            Son(t.Result.Valor, (1200, 600), (1000, 800));

            t = PedirAValhalla(ModoViaje.APie); // la rota es la del viaje
            servidor.Responder(RespuestaVarias(Viaje(1000, 800, LineaRota), Viaje(1200, 600)));
            Son(t.Result.Valor, (1200, 600));

            t = PedirAValhalla(ModoViaje.APie);
            servidor.Responder(RespuestaVarias(Viaje(1000, 800, LineaRota)));
            Assert.IsFalse(t.Result.Ok, "Ninguna legible.");
        }

        [Test]
        public void Alternativas_ValhallaFalla_Fallo()
        {
            var t = PedirAValhalla(ModoViaje.Coche);
            servidor.Fallar();
            Assert.IsFalse(t.Result.Ok);
            Assert.AreEqual(1, servidor.Peticiones.Count);
        }

        [Test]
        public void Alternativas_Osrm_PorTiempoYTresComoMucho()
        {
            var t = new RutasOsrm(servidor.Pedir).Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None);
            string Una(double metros, double segundos, string linea = Linea6) =>
                "{\"geometry\":\"" + linea + "\",\"legs\":[],\"distance\":" + metros + ",\"duration\":" + segundos + "}";
            servidor.Responder("{\"code\":\"Ok\",\"routes\":[" + string.Join(",", Una(3000, 400), Una(2000, 300), Una(1800, 100, LineaRota),
                Una(5000, 500), Una(4000, 200)) + "]}");
            Assert.IsTrue(t.Result.Ok, t.Result.Error);
            Son(t.Result.Valor, (4000, 200), (2000, 300), (3000, 400));
        }

        [TestCase(ModoViaje.Coche)]
        [TestCase(ModoViaje.APie)]
        public void Osrm_PideSuUrlUnaSolaVez(ModoViaje modo)
        {
            string url = RutasOsrm.Url(Cabildo, CalleAncha, modo);
            servidor.Peticiones.Clear();
            var una = new RutasOsrm(servidor.Pedir).Calcular(Cabildo, CalleAncha, modo, CancellationToken.None);
            servidor.Responder(RespuestaOsrm);
            Assert.IsTrue(una.Result.Ok, una.Result.Error);
            Assert.AreEqual(url, servidor.Peticiones.Single().texto);

            servidor.Peticiones.Clear();
            var varias = new RutasOsrm(servidor.Pedir).Alternativas(Cabildo, CalleAncha, modo, CancellationToken.None);
            servidor.Responder(RespuestaOsrm);
            Assert.IsTrue(varias.Result.Ok, varias.Result.Error);
            Assert.AreEqual(url, servidor.Peticiones.Single().texto);
        }

        [Test]
        public void Calcular_EnCoche_LaMasRapidaConUnaPeticion()
        {
            servidor.Peticiones.Clear();
            var t = new RutasValhalla(servidor.Pedir, esperas.Esperar).Calcular(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None);
            StringAssert.Contains("\"shortest\":false", servidor.Peticiones.Single().texto);
            StringAssert.DoesNotContain("alternates", servidor.Peticiones.Single().texto);
            servidor.Responder(RespuestaVarias(Viaje(10360, 1319)));
            Assert.AreEqual(1319, t.Result.Valor.Segundos);

            var o = new RutasOsrm(servidor.Pedir).Calcular(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None);
            servidor.Responder(RespuestaOsrm);
            Assert.AreEqual(150.0, o.Result.Valor.Segundos, "OSRM, la de menos tiempo.");
        }

        /// <summary>Servicio con alternativas propias, que responde al momento (o falla).</summary>
        class RutasConLista : IRutas
        {
            public Ruta[] Lista; // null = fallo
            public int Pedidas, Calculadas;

            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                Calculadas++;
                return Task.FromResult(Lista == null ? Resultado<Ruta>.Fallo("sin conexión") : Resultado<Ruta>.Exito(Lista[0]));
            }

            public Task<Resultado<IReadOnlyList<Ruta>>> Alternativas(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                Pedidas++;
                return Task.FromResult(Lista == null
                    ? Resultado<IReadOnlyList<Ruta>>.Fallo("sin conexión")
                    : Resultado<IReadOnlyList<Ruta>>.Exito(Lista));
            }
        }

        [Test]
        public void Alternativas_ValhallaFalla_LasDeLaReserva()
        {
            var principal = new RutasConLista();
            var reserva = new RutasConLista { Lista = new[] { R(2000, 100), R(1000, 200) } };
            var otro = new ServicioRutas(principal, reserva, esperas.Esperar, () => reloj);
            var r = otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result;
            Son(r.Valor, (2000, 100), (1000, 200));

            principal.Lista = new Ruta[0]; // una lista vacía tampoco vale
            otro.VaciarCache();
            Son(otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result.Valor, (2000, 100), (1000, 200));
        }

        [Test]
        public void Alternativas_ValhallaTardaOchoSegundos_LasDeLaReserva()
        {
            var reserva = new RutasConLista { Lista = new[] { R(2000, 100) } };
            var otro = new ServicioRutas(valhalla, reserva, esperas.Esperar, () => reloj);
            var t = otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None);
            Assert.AreEqual(TimeSpan.FromSeconds(8), esperas.Lista.Single().duracion);
            esperas.VencerTodas();
            Assert.IsTrue(valhalla.Peticiones.Single().Ct.IsCancellationRequested);
            Son(t.Result.Valor, (2000, 100));
        }

        [Test]
        public void Alternativas_SinRed_LaAproximadaSolaYSinGuardar()
        {
            var principal = new RutasConLista();
            var local = new RutasFalsas(); // solo Calcular: la lista por defecto de IRutas
            var otro = new ServicioRutas(principal, new RutasConLista(), esperas.Esperar, () => reloj, local);
            var t = otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None);
            local.Peticiones.Single().Tcs.TrySetResult(Resultado<Ruta>.Exito(R(1200, 240, aproximada: true)));
            Son(t.Result.Valor, (1200, 240));
            Assert.IsTrue(t.Result.Valor[0].Aproximada);
            Assert.AreEqual(0, otro.EnCache);

            // Vuelve la red: no estaba guardada
            principal.Lista = new[] { R(1000, 100) };
            Son(otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result.Valor, (1000, 100));
            Assert.AreEqual(2, principal.Pedidas);
        }

        [Test]
        public void Alternativas_TodosFallan_Fallo()
        {
            var otro = new ServicioRutas(new RutasConLista(), new RutasConLista(), esperas.Esperar, () => reloj, new RutasConLista());
            var r = otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result;
            Assert.IsFalse(r.Ok);
            StringAssert.Contains("Calles guardadas", r.Error);
        }

        [Test]
        public void Alternativas_Cache_LaMismaListaSinRedYLaPrimeraParaCalcular()
        {
            var principal = new RutasConLista { Lista = new[] { R(2000, 100), R(1000, 200) } };
            var otro = new ServicioRutas(principal, null, esperas.Esperar, () => reloj);
            Resultado<IReadOnlyList<Ruta>> Pedir(Coordenada origen, ModoViaje modo = ModoViaje.Coche) =>
                otro.Alternativas(origen, CalleAncha, modo, CancellationToken.None).Result;
            var primera = Pedir(Cabildo).Valor;

            reloj = Ahora.AddSeconds(119);
            Assert.AreSame(primera, Pedir(new Coordenada(Cabildo.Latitud + 10.0 / 111195.0, Cabildo.Longitud)).Valor);
            Assert.AreEqual(1, principal.Pedidas);
            Assert.AreSame(primera[0], otro.Calcular(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result.Valor);
            Assert.AreEqual(0, principal.Calculadas, "Calcular usa la primera de la lista.");

            Pedir(Cabildo, ModoViaje.APie); // otro modo
            Assert.AreEqual(2, principal.Pedidas);
            Pedir(new Coordenada(Cabildo.Latitud + 30.0 / 111195.0, Cabildo.Longitud)); // más de 20 m
            Assert.AreEqual(3, principal.Pedidas);

            reloj = Ahora.AddSeconds(120); // 2 min
            Pedir(Cabildo);
            Assert.AreEqual(4, principal.Pedidas);
            otro.VaciarCache();
            Pedir(Cabildo);
            Assert.AreEqual(5, principal.Pedidas);
        }

        [Test]
        public void Alternativas_SiCalcularYaLaTenia_NoSeCambia()
        {
            var principal = new RutasConLista { Lista = new[] { R(3000, 90) } };
            var otro = new ServicioRutas(principal, null, esperas.Esperar, () => reloj);
            var deLaFicha = otro.Calcular(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result.Valor;
            principal.Lista = new[] { R(2000, 80), R(1000, 200) };
            Son(otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result.Valor, (2000, 80), (1000, 200));
            Assert.AreSame(deLaFicha, otro.Calcular(Cabildo, CalleAncha, ModoViaje.Coche, CancellationToken.None).Result.Valor);
            Assert.AreEqual(1, otro.EnCache);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Alternativas_CancelarDuranteLaPrimeraOLaSegunda(int cual)
        {
            servidor.Peticiones.Clear();
            var cts = new CancellationTokenSource();
            var t = PedirAValhalla(ModoViaje.Coche, cts.Token);
            if (cual == 2) servidor.Responder(RespuestaVarias(Viaje(10000, 1300)));
            Assert.AreEqual(cual, servidor.Peticiones.Count);
            cts.Cancel();
            Assert.Catch<OperationCanceledException>(() => t.GetAwaiter().GetResult());
            Assert.IsTrue(t.IsCanceled);

            // Igual con el servicio por delante: ni reserva ni caché
            cts = new CancellationTokenSource();
            servidor.Peticiones.Clear();
            var otro = new ServicioRutas(new RutasValhalla(servidor.Pedir, new Esperas().Esperar), osrm, esperas.Esperar, () => reloj);
            t = otro.Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, cts.Token);
            if (cual == 2) servidor.Responder(RespuestaVarias(Viaje(10000, 1300)));
            cts.Cancel();
            Assert.IsTrue(t.IsCanceled);
            Assert.IsEmpty(osrm.Peticiones);
            Assert.AreEqual(0, otro.EnCache);
        }

        [Test]
        public void Alternativas_CancelarConUnaRespuestaQueLlegaIgual()
        {
            var sordo = new ServidorFalso { IgnorarCancelacion = true };
            var cts = new CancellationTokenSource();
            var t = new RutasValhalla(sordo.Pedir, new Esperas().Esperar).Alternativas(Cabildo, CalleAncha, ModoViaje.Coche, cts.Token);
            cts.Cancel();
            sordo.Responder(RespuestaVarias(Viaje(10000, 1300)));
            Assert.IsTrue(t.IsCanceled);
            Assert.AreEqual(1, sordo.Peticiones.Count, "Cancelada, no hay segunda.");
        }

        [Test]
        public void Alternativas_PorDefecto_LaDeCalcularSola()
        {
            IRutas falsas = valhalla;
            var t = falsas.Alternativas(Cabildo, CalleAncha, ModoViaje.APie, CancellationToken.None);
            valhalla.De(ModoViaje.APie).Responder(60, 500);
            Son(t.Result.Valor, (500, 60));
            t = falsas.Alternativas(Cabildo, CalleAncha, ModoViaje.APie, CancellationToken.None);
            valhalla.De(ModoViaje.APie).Fallar();
            Assert.IsFalse(t.Result.Ok);
        }

        // ───────────────────────── Fila de la ficha (matriz) ─────────────────────────

        [Test]
        public void AbrirFichaConPosicion_PuntosSuspensivosYLuegoMinutosConCocheElegido()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            Assert.IsTrue(Ficha.AccionesVisibles);
            Assert.AreEqual("…", Fila.Coche.Tiempo.text);
            Assert.AreEqual("…", Fila.APie.Tiempo.text);
            Assert.AreEqual(ModoViaje.Coche, Fila.Elegido);
            Assert.IsTrue(Fila.Coche.ClassListContains(BotonModo.ClaseElegido));
            Assert.IsFalse(Fila.APie.ClassListContains(BotonModo.ClaseElegido));
            Assert.AreEqual("Coche", Fila.Coche.Nombre.text);
            Assert.AreEqual("A pie", Fila.APie.Nombre.text);
            Assert.AreEqual("Ir", Fila.TextoIr.text);
            var p = valhalla.De(ModoViaje.Coche);
            Assert.AreEqual(Cabildo, p.Origen);
            Assert.AreEqual(Areas.Posicion, p.Destino);

            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            Assert.AreEqual("6 min", Fila.Coche.Tiempo.text);
            Assert.AreEqual("15 min", Fila.APie.Tiempo.text);
            Assert.AreEqual(ModoViaje.Coche, Fila.Elegido, "Sigue elegido el coche.");
        }

        [Test]
        public void LaFilaVaEntreLaCabeceraYElContenidoSinDesplazarse()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            var hoja = Ficha.Hoja;
            Assert.AreSame(Ficha.FilaAcciones, Fila.parent);
            Assert.Less(hoja.IndexOf(hoja.Q("ficha-cabecera")), hoja.IndexOf(Ficha.FilaAcciones));
            Assert.Less(hoja.IndexOf(Ficha.FilaAcciones), hoja.IndexOf(Ficha.Contenido));
            Assert.IsFalse(Ficha.Contenido.Contains(Fila), "La fila no se desplaza con los conceptos.");
            Assert.IsTrue(Fila.Coche.focusable && Fila.APie.focusable && Fila.Ir.focusable);
            Assert.AreEqual(PickingMode.Position, Fila.Ir.pickingMode);
        }

        [Test]
        public void PuntoLibre_LaMismaFilaEIrDiceIrAEstePunto()
        {
            CrearConPosicion();
            var px = new PuntoPantalla(100f, 200f, 300f);
            mapa.suelo[(px.X, px.Y)] = CalleAncha;
            Assert.IsTrue(pantalla.PulsarLargoMapa(px));
            Assert.IsTrue(Ficha.AccionesVisibles);
            Assert.AreEqual("Ir a este punto", Fila.AnuncioIr);
            Assert.AreEqual(CalleAncha, valhalla.De(ModoViaje.APie).Destino);
            valhalla.De(ModoViaje.APie).Responder(200);
            Assert.AreEqual("4 min", Fila.APie.Tiempo.text);
            Assert.AreEqual("…", Fila.Coche.Tiempo.text);
        }

        [TestCase(EstadoUbicacion.Buscando)]
        [TestCase(EstadoUbicacion.SinPermiso)]
        [TestCase(EstadoUbicacion.Apagada)]
        public void SinPosicion_RayaEnLosDosYSinPeticiones(EstadoUbicacion estado)
        {
            Crear(estado, null);
            pantalla.AbrirFicha(Areas);
            Assert.AreEqual("—", Fila.Coche.Tiempo.text);
            Assert.AreEqual("—", Fila.APie.Tiempo.text);
            Assert.IsEmpty(valhalla.Peticiones);
            AccesibilidadPantalla.LectorForzado = () => false;
            Assert.AreEqual("Coche, sin tiempo", Fila.Anuncio(ModoViaje.Coche));
        }

        [Test]
        public void SinPosicion_IrSinPermisoAbreLaExplicacion()
        {
            Crear(EstadoUbicacion.SinPermiso, null);
            pantalla.AbrirFicha(Areas);
            Fila.PulsarIr();
            Assert.IsTrue(pantalla.ExplicacionAbierta, "Lo mismo que el botón de ubicación.");
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsEmpty(valhalla.Peticiones);
        }

        [TestCase(EstadoUbicacion.Denegado)]
        [TestCase(EstadoUbicacion.Apagada)]
        public void SinPosicion_IrConLaUbicacionDesactivadaAbreElAviso(EstadoUbicacion estado)
        {
            Crear(estado, null);
            pantalla.AbrirFicha(Areas);
            Fila.PulsarIr();
            Assert.IsTrue(pantalla.AvisoAbierto);
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsEmpty(valhalla.Peticiones);
        }

        [Test]
        public void SinPosicion_IrBuscandoNoHaceNadaComoElBoton()
        {
            Crear(EstadoUbicacion.Buscando, null);
            pantalla.AbrirFicha(Areas);
            Fila.PulsarIr();
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.ModalUbicacionAbierta);
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsEmpty(valhalla.Peticiones);
        }

        [Test]
        public void LlegaLaPosicion_ConLaFichaAbiertaSePidenLosMinutos()
        {
            Crear(EstadoUbicacion.Buscando, null);
            pantalla.AbrirFicha(Areas);
            pantalla.ActualizarCapas(Escala);
            Assert.IsEmpty(valhalla.Peticiones);

            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Cabildo;
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(2, valhalla.Peticiones.Count);
            Assert.AreEqual("…", Fila.Coche.Tiempo.text);
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(2, valhalla.Peticiones.Count, "Una sola vez.");
            valhalla.De(ModoViaje.Coche).Responder(330);
            Assert.AreEqual("6 min", Fila.Coche.Tiempo.text);
        }

        [Test]
        public void ValhallaCae_SalenLosMinutosDeOsrm()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).Fallar();
            osrm.De(ModoViaje.Coche).Responder(100);
            Assert.AreEqual("2 min", Fila.Coche.Tiempo.text);
        }

        [Test]
        public void LosDosCaen_RayaEnEseModo()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.APie).Fallar();
            osrm.De(ModoViaje.APie).Fallar();
            Assert.AreEqual("—", Fila.APie.Tiempo.text);
            Assert.AreEqual("…", Fila.Coche.Tiempo.text, "El otro modo sigue a lo suyo.");
        }

        [Test]
        public void CambiarAPie_QuedaElegidoYTalkBackLoDice()
        {
            CrearConPosicion();
            AccesibilidadPantalla.LectorForzado = () => false;
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            Fila.APie.Pulsar();
            Assert.AreEqual(ModoViaje.APie, Fila.Elegido);
            Assert.IsTrue(Fila.APie.Elegido);
            Assert.IsFalse(Fila.Coche.Elegido);
            Assert.Contains("A pie, 15 minutos, seleccionado", anuncios);
            var estado = Acc.RecorridoConEstado();
            Assert.Contains(("A pie, 15 minutos", true), estado);
            Assert.Contains(("Coche, 6 minutos", false), estado);

            anuncios.Clear();
            Fila.APie.Pulsar();
            Assert.IsEmpty(anuncios, "Tocar el modo ya elegido no cambia nada.");
        }

        [Test]
        public void TalkBack_ModosEIrEntreLaCabeceraYLosConceptos()
        {
            CrearConPosicion();
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            pantalla.AbrirFicha(Areas);
            var recorrido = Acc.Recorrido();
            int coche = recorrido.IndexOf("Coche, calculando el tiempo");
            Assert.GreaterOrEqual(coche, 0, string.Join(" | ", recorrido));
            Assert.AreEqual("A pie, calculando el tiempo", recorrido[coche + 1]);
            Assert.AreEqual("Ir a Clínica Areas", recorrido[coche + 2]);
            var finCabecera = string.IsNullOrWhiteSpace(Ficha.Direccion.text) ? Ficha.Tipo.text : Ficha.Direccion.text;
            Assert.AreEqual(finCabecera, recorrido[coche - 1], "Justo después de la cabecera.");
            var primeroContenido = Ficha.TextosTalkBack().First(t => Ficha.Contenido.Contains(t.elemento)).texto;
            Assert.AreEqual(primeroContenido, recorrido[coche + 3], "Antes de los conceptos.");
            Assert.AreEqual("Cerrar ficha", recorrido.Last());

            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            var estado = Acc.RecorridoConEstado();
            Assert.Contains(("Coche, 6 minutos", true), estado, "Coche, 6 minutos, seleccionado.");
            Assert.Contains(("A pie, 15 minutos", false), estado);
        }

        [Test]
        public void TalkBack_PuntoLibreSinContenidoLlevaModosEIrAntesDeCerrar()
        {
            CrearConPosicion();
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            var px = new PuntoPantalla(100f, 200f, 300f);
            mapa.suelo[(px.X, px.Y)] = CalleAncha;
            Assert.IsTrue(pantalla.PulsarLargoMapa(px));
            CollectionAssert.AreEqual(new[]
            {
                "36,7781, -6,3515", "Punto en el mapa", "Coche, calculando el tiempo", "A pie, calculando el tiempo",
                "Ir a este punto", "Cerrar ficha",
            }, Acc.Recorrido());
        }

        [Test]
        public void TalkBack_UnMinutoYSinTiempo()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).Responder(30);
            valhalla.De(ModoViaje.APie).Fallar();
            osrm.De(ModoViaje.APie).Fallar();
            Assert.AreEqual("Coche, 1 minuto", Fila.Anuncio(ModoViaje.Coche));
            Assert.AreEqual("A pie, sin tiempo", Fila.Anuncio(ModoViaje.APie));
            Assert.AreEqual("1 min", Fila.Coche.Tiempo.text);
        }

        [Test]
        public void TalkBack_ElNodoDelModoCambiaDeEstadoSinRehacerLaJerarquia()
        {
            CrearConPosicion();
            AccesibilidadPantalla.LectorForzado = () => true;
            Acc.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            pantalla.AbrirFicha(Areas);
            var jerarquia = AssistiveSupport.activeHierarchy;
            var nodos = jerarquia.rootNodes.ToList();
            var coche = nodos.Single(n => n.label == "Coche, calculando el tiempo");
            var aPie = nodos.Single(n => n.label == "A pie, calculando el tiempo");
            Assert.AreEqual(AccessibilityState.Selected, coche.state);

            valhalla.De(ModoViaje.Coche).Responder(330);
            Assert.AreEqual("Coche, 6 minutos", coche.label, "El mismo nodo lleva los minutos.");
            Fila.APie.Pulsar();
            Assert.AreSame(jerarquia, AssistiveSupport.activeHierarchy, "No se rehace la jerarquía.");
            Assert.AreEqual(AccessibilityState.Selected, aPie.state);
            Assert.AreNotEqual(AccessibilityState.Selected, coche.state);
        }

        [Test]
        public void Cache_ReabrirAntesDeDosMinutosSinMoverseNoPideNada()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            pantalla.CerrarFicha();

            reloj = Ahora.AddSeconds(90);
            pantalla.AbrirFicha(Areas);
            Assert.AreEqual(2, valhalla.Peticiones.Count, "Sin peticiones nuevas.");
            Assert.AreEqual("6 min", Fila.Coche.Tiempo.text);
            Assert.AreEqual("15 min", Fila.APie.Tiempo.text);
        }

        [Test]
        public void CerrarPronto_CancelaLasPeticionesYNadaSePinta()
        {
            valhalla.IgnorarCancelacion = true;
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            pantalla.CerrarFicha();
            Assert.IsTrue(valhalla.Peticiones.All(p => p.Ct.IsCancellationRequested), "Peticiones canceladas.");
            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            Assert.IsEmpty(avisos, "La respuesta tardía no llega.");
            Assert.AreEqual("…", Fila.Coche.Tiempo.text, "Nada se pinta.");
        }

        [Test]
        public void DeUnaFichaAOtra_LaRespuestaDeLaAnteriorNoSePinta()
        {
            valhalla.IgnorarCancelacion = true;
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            var viejas = valhalla.Peticiones.ToList();
            var px = new PuntoPantalla(100f, 200f, 300f);
            mapa.suelo[(px.X, px.Y)] = CalleAncha;
            pantalla.PulsarLargoMapa(px);
            foreach (var p in viejas) p.Responder(60);
            Assert.AreEqual("…", Fila.Coche.Tiempo.text);
            Assert.AreEqual(4, valhalla.Peticiones.Count);
        }

        [Test]
        public void SinServicioDeRutas_LaFilaQuedaVaciaYFueraDeTalkBack()
        {
            Crear(EstadoUbicacion.Activa, Cabildo, conRutas: false);
            AccesibilidadPantalla.LectorForzado = () => false;
            pantalla.AbrirFicha(Areas);
            Assert.IsFalse(Ficha.AccionesVisibles);
            Assert.IsFalse(Acc.Recorrido().Any(e => e.StartsWith("Coche") || e.StartsWith("Ir a")));
            Assert.IsNull(pantalla.Rutas);
        }

        [Test]
        public void ElArranqueLeDaLasRutasALaPantalla()
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
                var prefs = new PreferenciasFalsas();
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), conUi, new ServicioTema(prefs, new SistemaClaro()), Lugares(),
                    new ServicioFiltro(prefs), Ahora, rutas: servicio));
                Assert.AreSame(servicio, conUi.Rutas);
                conUi.AbrirFicha(Areas);
                Assert.IsTrue(conUi.Ficha.AccionesVisibles);
            }
            finally { UnityEngine.Object.DestroyImmediate(conInterfaz); }
        }

        [Test]
        public void LosTextosDeLaFilaEstanEnLaTabla()
        {
            foreach (var clave in new[]
                     {
                         ClavesTexto.FichaModoCoche, ClavesTexto.FichaModoPie, ClavesTexto.FichaIr, ClavesTexto.FichaIrA,
                         ClavesTexto.FichaIrPunto, ClavesTexto.FichaMinutos, ClavesTexto.FichaCalculando, ClavesTexto.FichaSinTiempo,
                         ClavesTexto.FichaAnuncioMinutos, ClavesTexto.FichaAnuncioMinuto, ClavesTexto.FichaAnuncioSinTiempo,
                         ClavesTexto.FichaAnuncioCalculando, ClavesTexto.FichaModoElegido,
                     })
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
        }

        // ───────────────────────── Ir dibuja la ruta (parte 2a, matriz) ─────────────────────────

        static readonly Coordenada CincuentaMetrosAlNorte = new Coordenada(36.77835, -6.3528);

        TarjetaManiobra Arriba => pantalla.TarjetaManiobra;
        TarjetaRuta Abajo => pantalla.TarjetaRuta;

        static bool Visible(VisualElement e) => e.style.display.value == DisplayStyle.Flex;

        /// <summary>Ficha de Clínica Areas abierta con posición e Ir pulsado en ese modo (coche por defecto).</summary>
        void IrAAreas(ModoViaje modo = ModoViaje.Coche)
        {
            pantalla.AbrirFicha(Areas);
            if (modo != Fila.Elegido) Fila.Boton(modo).Pulsar();
            Fila.PulsarIr();
        }

        void ComprobarControlesDeRuta(bool conRuta)
        {
            Assert.AreEqual(conRuta, Visible(Arriba), "Tarjeta de arriba.");
            Assert.AreEqual(conRuta, Visible(Abajo), "Tarjeta de abajo.");
            Assert.AreEqual(conRuta, raizArbol.ClassListContains(PantallaMapa.ClaseConRuta), "Sin píldora ni tema con ruta.");
            Assert.AreEqual(!conRuta, pantalla.Buscador.Visible, "La cápsula.");
        }

        [Test]
        public void IrEIniciar_CierraLaFichaDibujaEncuadraYEnsenaLasTarjetas()
        {
            CrearConPosicion();
            IrAAreas();
            Assert.IsFalse(pantalla.RutaActiva, "Ir no empieza: abre «Cómo llegar».");

            var p = valhalla.De(ModoViaje.Coche);
            Assert.AreEqual(Cabildo, p.Origen, "Desde la posición actual.");
            Assert.AreEqual(Areas.Posicion, p.Destino);
            p.ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            Assert.IsFalse(pantalla.FichaAbierta, "Iniciar cierra la ficha.");
            Assert.IsTrue(pantalla.RutaActiva);
            ComprobarControlesDeRuta(true);

            Assert.AreEqual(3, mapa.linea.Count, "La línea va por los puntos de la ruta.");
            Assert.IsNotEmpty(mapa.encuadres, "La ruta se encuadra.");
            Assert.AreSame(mapa.linea, mapa.encuadres.Last().puntos);
            Assert.IsTrue(mapa.encuadres.Last().libre.Valido);
            Assert.AreEqual("Sal hacia el norte por Calle Ancha", Arriba.Principal.text);
            Assert.IsTrue(Visible(Arriba.FilaSiguiente));
            Assert.AreEqual("Gira a la derecha en Calle Santo Domingo", Arriba.Siguiente.text);
            Assert.AreEqual(TipoManiobra.Salida, Arriba.IconoPrincipal.Tipo);
            Assert.AreEqual(TipoManiobra.Derecha, Arriba.IconoSiguiente.Tipo);
            Assert.AreEqual("13:34", Abajo.Llegada.text);
            Assert.AreEqual("Llegada", Abajo.RotuloLlegada.text);
            Assert.AreEqual("4", Abajo.Minutos.text);
            Assert.AreEqual("min", Abajo.RotuloMinutos.text);
            Assert.AreEqual("1,2", Abajo.Distancia.text);
            Assert.AreEqual("km", Abajo.RotuloDistancia.text);
            var guardada = servicio.Calcular(Cabildo, Areas.Posicion, ModoViaje.Coche, CancellationToken.None);
            Assert.IsTrue(guardada.IsCompleted, "La ruta queda en la caché.");
            Assert.AreSame(pantalla.RutaDibujada, guardada.Result.Valor);
        }

        [Test]
        public void IrEIniciar_MenosDeUnKilometroEnMetros()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 950);
            pantalla.Iniciar();
            Assert.AreEqual("950", Abajo.Distancia.text);
            Assert.AreEqual("m", Abajo.RotuloDistancia.text);
        }

        [Test]
        public void IrConLosMinutosYaCalculados_LaHojaLosTieneYPideLasRutas()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            valhalla.De(ModoViaje.APie).Responder(900);
            gps.Posicion = new Coordenada(36.77799, -6.3528); // 10 m: la caché sigue valiendo
            Fila.PulsarIr();
            Assert.AreEqual(3, valhalla.Peticiones.Count, "Solo las rutas del modo elegido.");
            Assert.AreEqual("4 min", pantalla.ComoLlegar.Coche.Tiempo.text);
            Assert.AreEqual("15 min", pantalla.ComoLlegar.APie.Tiempo.text);

            // Otra vez sin moverse: las rutas salen de la caché al momento
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(250, 1200);
            pantalla.CerrarComoLlegar();
            Fila.PulsarIr();
            Assert.AreEqual(3, valhalla.Peticiones.Count);
            Assert.AreEqual("5 min", pantalla.ComoLlegar.Tarjetas[0].Minutos.text);
            Assert.IsNotNull(mapa.linea);
        }

        [Test]
        public void SeHaMovido_LaRutaSaleDeLaPosicionActual()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).Responder(330);
            valhalla.De(ModoViaje.APie).Responder(900);
            gps.Posicion = CincuentaMetrosAlNorte;
            Fila.PulsarIr();
            Assert.AreEqual(3, valhalla.Peticiones.Count, "Más de 20 m: ruta nueva.");
            Assert.AreEqual(CincuentaMetrosAlNorte, valhalla.De(ModoViaje.Coche).Origen);
        }

        [Test]
        public void RutaDeOsrm_RutaAlDestinoSinSiguiente()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).Fallar();
            osrm.De(ModoViaje.Coche).Responder(240, 1200); // sin textos de maniobra
            pantalla.Iniciar();
            Assert.AreEqual("Ruta a Clínica Areas", Arriba.Principal.text);
            Assert.AreEqual("Ruta a Clínica Areas", Arriba.Anuncio);
            Assert.IsFalse(Visible(Arriba.FilaSiguiente));
            Assert.IsNotNull(mapa.linea);
        }

        [Test]
        public void QuitarRuta_ConLaX()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            // Con ruta: el botón de ubicación abajo a la izquierda y la atribución, encima de la tarjeta (sin medida, 120 dp)
            var pila = raizArbol.Q("pila-derecha");
            var atribucion = raizArbol.Q("atribucion");
            float encima = PantallaMapa.AltoSupuestoTarjetaRutaDp + 12f;
            Assert.AreEqual(12f, pila.style.left.value.value, 0.01f);
            Assert.AreEqual(encima, pila.style.bottom.value.value, 0.01f);
            Assert.AreEqual(StyleKeyword.Auto, pila.style.top.keyword);
            Assert.AreEqual(StyleKeyword.Auto, pila.style.right.keyword);
            Assert.AreEqual(encima, atribucion.style.bottom.value.value, 0.01f);

            Abajo.Desplegar();
            Abajo.PulsarFinalizar();
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsNull(mapa.linea);
            ComprobarControlesDeRuta(false);
            // Sin ruta: la columna vuelve a su sitio (el de la USS) y la atribución, encima de la cápsula
            Assert.AreEqual(StyleKeyword.Null, pila.style.left.keyword);
            Assert.AreEqual(StyleKeyword.Null, pila.style.bottom.keyword);
            Assert.AreEqual(StyleKeyword.Null, pila.style.top.keyword);
            Assert.AreEqual(StyleKeyword.Null, pila.style.right.keyword);
            Assert.AreEqual(pantalla.Buscador.AltoCapsula + HojaBuscador.MargenAbajo + 8f, atribucion.style.bottom.value.value, 0.01f);
        }

        [Test]
        public void AtrasNoQuitaLaRuta()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            Assert.IsTrue(pantalla.Atras(), "Se gasta: la app no sale.");
            Assert.IsTrue(pantalla.RutaActiva);
            ComprobarControlesDeRuta(true);
            Assert.IsNotNull(mapa.linea);
            pantalla.QuitarRuta(); // «Finalizar ruta»
            Assert.IsFalse(pantalla.RutaActiva);
            ComprobarControlesDeRuta(false);
            Assert.IsNull(mapa.linea);
            Assert.IsFalse(pantalla.Atras(), "Sin ruta, atrás ya no hace nada.");
        }

        [Test]
        public void Reabrir_LaFichaTapaLasTarjetasYLaLineaSigue()
        {
            CrearConPosicion();
            IrAAreas(ModoViaje.APie);
            valhalla.De(ModoViaje.APie).ResponderConManiobras(900, 1100);
            pantalla.Iniciar();
            int encuadres = mapa.encuadres.Count;

            pantalla.ReabrirFichaRuta();
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreSame(Areas, Ficha.Lugar);
            Assert.AreEqual(ModoViaje.APie, Fila.Elegido, "Con el modo de la ruta elegido.");
            Assert.IsFalse(Visible(Arriba));
            Assert.IsFalse(Visible(Abajo));
            Assert.IsNotNull(mapa.linea, "La línea sigue.");
            Assert.IsTrue(pantalla.RutaActiva);

            pantalla.CerrarFicha();
            ComprobarControlesDeRuta(true);
            Assert.IsNotNull(mapa.linea);
            Assert.Greater(mapa.encuadres.Count, encuadres, "Al volver, la ruta se encuadra otra vez.");

            // Atrás con la ficha reabierta la cierra (vuelven las tarjetas); no quita la ruta
            pantalla.ReabrirFichaRuta();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsTrue(pantalla.RutaActiva);
            ComprobarControlesDeRuta(true);
        }

        [Test]
        public void OtroModo_FichaReabiertaAPieEIrDaLaRutaAPie()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            pantalla.ReabrirFichaRuta();
            Fila.APie.Pulsar();
            Fila.PulsarIr();
            Assert.IsNull(mapa.linea, "La del coche se oculta mientras se elige.");
            var p = valhalla.De(ModoViaje.APie);
            p.ResponderConManiobras(900, 1100);
            pantalla.Iniciar();
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.AreSame(p.Tcs.Task.Result.Valor, pantalla.RutaDibujada);
            Assert.AreEqual("15", Abajo.Minutos.text);
            ComprobarControlesDeRuta(true);
        }

        [Test]
        public void PuntoLibre_IrYReabrirVuelvenAlPunto()
        {
            CrearConPosicion();
            var px = new PuntoPantalla(100f, 200f, 300f);
            mapa.suelo[(px.X, px.Y)] = CalleAncha;
            pantalla.PulsarLargoMapa(px);
            Fila.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(120, 300);
            pantalla.Iniciar();
            Assert.AreEqual("Ruta a 36,7781, -6,3515", Arriba.Principal.text);
            // El destino conserva su chincheta con la ficha cerrada, como los lugares su marcador
            mapa.proyectar = c => c == CalleAncha ? new PuntoPantalla(200f, 300f, 300f) : PuntoPantalla.Ninguno;
            Assert.AreEqual(CalleAncha, pantalla.PuntoChincheta);
            pantalla.Chincheta.Actualizar(mapa, pantalla.PuntoChincheta, Escala, 1000f, 2000f);
            Assert.IsTrue(pantalla.Chincheta.Visible, "La chincheta del destino sigue.");

            pantalla.ReabrirFichaRuta();
            Assert.AreEqual(CalleAncha, pantalla.PuntoAbierto);
            Assert.IsFalse(Visible(Arriba), "Con la ficha del punto, las tarjetas se ocultan.");
            Assert.IsFalse(Visible(Abajo));
            pantalla.CerrarFicha();
            ComprobarControlesDeRuta(true);
            Assert.AreEqual(CalleAncha, pantalla.PuntoChincheta);

            pantalla.QuitarRuta();
            Assert.IsNull(pantalla.PuntoChincheta);
            Assert.IsFalse(pantalla.Chincheta.Visible, "Al quitar la ruta se va la chincheta.");
        }

        [Test]
        public void CompartirConRuta_QuitaLaRutaYAbreLaHoja()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            pantalla.RecibirCompartido("farmacia de guardia");
            Assert.IsFalse(pantalla.RutaActiva, "Lo compartido no se pierde detrás de la ruta.");
            Assert.IsNull(mapa.linea);
            Assert.IsTrue(pantalla.Buscador.Visible);
            Assert.IsTrue(pantalla.Buscador.Abierta, "La hoja se abre con lo compartido.");
            Assert.AreEqual("farmacia de guardia", pantalla.Buscador.Campo.value);
        }

        [Test]
        public void TalkBack_TarjetaDeArribaPrimeroYLaDeAbajoComoBoton()
        {
            CrearConPosicion();
            AccesibilidadPantalla.LectorForzado = () => false;
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            Assert.AreSame(Arriba, Acc.Enfocado, "Al iniciar la ruta, el foco va a la tarjeta de arriba.");
            var recorrido = Acc.Recorrido();
            Assert.AreEqual("Sal hacia el norte por Calle Ancha. Después, gira a la derecha en Calle Santo Domingo", recorrido[0],
                string.Join(" | ", recorrido));
            int llegada = recorrido.IndexOf("Llegada 13:34, 4 minutos, 1,2 km");
            Assert.Greater(llegada, 0, string.Join(" | ", recorrido));
            Assert.IsFalse(recorrido.Contains("Quitar ruta"), "Plegada, sin botón.");
            Assert.IsFalse(recorrido.Contains("Precios en el mapa"), "Sin píldora.");
            Assert.IsFalse(recorrido.Contains("Cambiar tema"), "Sin botón de tema.");

            // Desplegada (sin navegación): el destino y "Quitar ruta" detrás de la hoja
            Abajo.Desplegar();
            recorrido = Acc.Recorrido();
            llegada = recorrido.IndexOf("Llegada 13:34, 4 minutos, 1,2 km");
            Assert.AreEqual(Abajo.AnuncioDestino, recorrido[llegada + 1], string.Join(" | ", recorrido));
            Assert.AreEqual("Quitar ruta", recorrido[llegada + 2]);
            Abajo.Plegar();

            // La ficha se reabre desde la pantalla (la hoja ya no la abre)
            pantalla.ReabrirFichaRuta();
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsFalse(Acc.Recorrido().Contains("Quitar ruta"), "Con la ficha, fuera las tarjetas.");
            pantalla.CerrarFicha();
            Assert.AreSame(Abajo.Cuerpo, Acc.Enfocado, "Al cerrar la ficha, el foco vuelve a la tarjeta que la abrió.");

            pantalla.QuitarRuta();
            var sinRuta = Acc.Recorrido();
            Assert.Contains("Precios en el mapa", sinRuta);
            Assert.Contains("Cambiar tema", sinRuta);
            Assert.IsFalse(sinRuta.Any(e => e.StartsWith("Llegada") || e == "Quitar ruta"));
        }

        // ───────────────────────── Hoja de la ruta que solo informa (22/11/2026) ─────────────────────────

        [Test]
        public void Hoja_TocarNoReabreLaFichaNiQuitaLaRuta()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            Abajo.Tocar();
            Assert.IsFalse(pantalla.FichaAbierta, "Tocar la hoja ya no reabre la ficha.");
            Assert.IsTrue(pantalla.RutaActiva);
            Assert.IsTrue(Abajo.Desplegada);
            ComprobarControlesDeRuta(true);
            Assert.IsNull(raizArbol.Q("tarjeta-ruta-quitar"), "La hoja plegada no lleva X.");
        }

        [Test]
        public void Hoja_SinNavegacion_ConQuitarRuta()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            Abajo.Tocar();
            Assert.IsTrue(Abajo.Desplegada);
            Assert.AreEqual("Quitar ruta", Abajo.TextoFinalizar.text);
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(pantalla.Atras(), "Atrás pliega.");
            Assert.IsFalse(Abajo.Desplegada);
            Assert.IsTrue(pantalla.RutaActiva);
            Abajo.Desplegar();
            Abajo.PulsarFinalizar();
            Assert.IsFalse(pantalla.RutaActiva);
            ComprobarControlesDeRuta(false);
            Assert.IsFalse(Abajo.Desplegada);
        }

        [Test]
        public void Hoja_OtraRutaEmpiezaPlegada()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(240, 1200);
            pantalla.Iniciar();
            Abajo.Desplegar();
            pantalla.QuitarRuta();
            IrAAreas();
            pantalla.Iniciar();
            Assert.IsFalse(Abajo.Desplegada, "La siguiente empieza plegada.");
        }

        [Test]
        public void Hoja_PuntoLibre_ChinchetaYSinCategoria()
        {
            CrearConPosicion();
            var px = new PuntoPantalla(100f, 200f, 300f);
            mapa.suelo[(px.X, px.Y)] = CalleAncha;
            pantalla.PulsarLargoMapa(px);
            Fila.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(120);
            pantalla.Iniciar();
            Assert.AreEqual("36,7781, -6,3515", Abajo.DestinoNombre.text);
            Assert.AreEqual(FilaResultado.GlifoChincheta, Abajo.GlifoDestino);
            Assert.IsTrue(Abajo.DestinoIcono.ClassListContains(TarjetaRuta.ClaseIconoPunto));
            Assert.AreEqual(T(ClavesTexto.PuntoTipo), Abajo.DestinoDetalle.text, "Sin dirección, «Punto en el mapa».");
        }

        /// <summary>
        /// Los gestos de verdad (AlBajar, AlMover, AlSoltar) con la hoja en un panel dispuesto y eventos de puntero:
        /// tocar arriba despliega o pliega; tocar el destino o dentro de lo desplegado no cambia nada; "Finalizar ruta"
        /// se pulsa una vez y no pliega; un arrastre que empieza en el botón no lo pulsa; uno de lado no cambia nada; si
        /// la app pliega la hoja con el dedo en el botón, soltarlo no la vuelve a desplegar.
        /// </summary>
        [Test]
        public void Hoja_GestosDeVerdadEnUnPanel()
        {
            var objetoPanel = new GameObject("Hoja en un panel");
            try
            {
                objetoPanel.SetActive(false);
                var documento = objetoPanel.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                objetoPanel.SetActive(true);
                var raiz = documento.rootVisualElement.Q("raiz");
                var marco = new VisualElement();
                marco.style.position = Position.Absolute;
                marco.style.left = 0f;
                marco.style.top = 0f;
                marco.style.width = 412f;
                marco.style.height = 915f;
                raiz.Add(marco);
                var hoja = new TarjetaRuta(T);
                hoja.style.display = DisplayStyle.Flex;
                marco.Add(hoja);
                hoja.PonerDestino("Clínica Areas", "Clínica privada · en coche", IconosLugar.Generico, null, "Destino");
                int finalizadas = 0;
                hoja.FinalizarPulsado += () => finalizadas++;
                Disponer(hoja);
                Vector2 arriba = hoja.Cuerpo.worldBound.center;
                Assert.Greater(hoja.Cuerpo.worldBound.height, 0f, "La hoja está dispuesta.");

                Puntero(hoja.Cuerpo, arriba, EventType.MouseDown);
                Assert.IsTrue(hoja.Cabecera.ClassListContains(TarjetaRuta.ClaseCabeceraPulsada), "La parte de arriba se ve pulsada.");
                Puntero(hoja.Cuerpo, arriba, EventType.MouseUp);
                Assert.IsFalse(hoja.Cabecera.ClassListContains(TarjetaRuta.ClaseCabeceraPulsada));
                Assert.IsTrue(hoja.Desplegada, "Tocar arriba la despliega.");
                Disponer(hoja);
                Vector2 destino = hoja.Destino.worldBound.center, boton = hoja.Finalizar.worldBound.center;
                Vector2 hueco = new Vector2(boton.x, (hoja.Destino.worldBound.yMax + hoja.Finalizar.worldBound.yMin) / 2f);
                Assert.Greater(hoja.Finalizar.worldBound.height, 0f);

                Tocar(hoja.Desplegable, destino);
                Assert.IsTrue(hoja.Desplegada, "Tocar la fila del destino no cambia nada.");
                Tocar(hoja.Desplegable, hueco);
                Assert.IsTrue(hoja.Desplegada, "Tocar dentro de lo desplegado no cambia nada.");

                Puntero(hoja.Finalizar, boton, EventType.MouseDown);
                Assert.IsTrue(hoja.Finalizar.ClassListContains(TarjetaRuta.ClaseFinalizarPulsado), "Se ve pulsado.");
                Puntero(hoja.Finalizar, boton, EventType.MouseUp);
                Assert.IsFalse(hoja.Finalizar.ClassListContains(TarjetaRuta.ClaseFinalizarPulsado));
                Assert.AreEqual(1, finalizadas, "«Finalizar ruta» se pulsa una vez.");
                Assert.IsTrue(hoja.Desplegada, "Pulsar el botón no pliega la hoja.");

                Puntero(hoja.Finalizar, boton, EventType.MouseDown);
                Puntero(hoja.Finalizar, boton + new Vector2(0f, 10f), EventType.MouseDrag);
                Puntero(hoja.Finalizar, boton + new Vector2(0f, 10f), EventType.MouseUp);
                Assert.AreEqual(1, finalizadas, "Un arrastre que empieza en el botón no lo pulsa.");
                Assert.IsFalse(hoja.Desplegada, "Arrastrar hacia abajo la pliega.");

                Puntero(hoja.Cuerpo, arriba, EventType.MouseDown);
                Puntero(hoja.Cuerpo, arriba + new Vector2(20f, 0f), EventType.MouseDrag);
                Puntero(hoja.Cuerpo, arriba + new Vector2(20f, 0f), EventType.MouseUp);
                Assert.IsFalse(hoja.Desplegada, "Un arrastre de lado no cambia nada.");

                Puntero(hoja.Cuerpo, arriba, EventType.MouseDown);
                Puntero(hoja.Cuerpo, arriba - new Vector2(0f, 10f), EventType.MouseDrag);
                Puntero(hoja.Cuerpo, arriba - new Vector2(0f, 10f), EventType.MouseUp);
                Assert.IsTrue(hoja.Desplegada, "Arrastrar hacia arriba la despliega.");

                // La app la pliega (recálculo) con el dedo en el botón: soltarlo no la vuelve a desplegar
                Disponer(hoja);
                boton = hoja.Finalizar.worldBound.center;
                Puntero(hoja.Finalizar, boton, EventType.MouseDown);
                hoja.Plegar(porUsuario: false);
                Puntero(hoja.Finalizar, boton, EventType.MouseUp);
                Assert.IsFalse(hoja.Desplegada, "Una pulsación que empezó en lo desplegado nunca despliega.");
                Assert.AreEqual(1, finalizadas, "Con la hoja ya plegada, el botón no se pulsa.");
            }
            finally { UnityEngine.Object.DestroyImmediate(objetoPanel); }
        }

        // El panel se dispone ya (estilos y disposición), como en el siguiente fotograma
        static void Disponer(VisualElement e)
        {
            var panel = e.panel;
            Assert.IsNotNull(panel, "La hoja está en un panel.");
            var validar = panel.GetType().GetMethod("ValidateLayout",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(validar, "El panel tiene ValidateLayout.");
            validar.Invoke(panel, null);
        }

        static void Tocar(VisualElement objetivo, Vector2 punto)
        {
            Puntero(objetivo, punto, EventType.MouseDown);
            Puntero(objetivo, punto, EventType.MouseUp);
        }

        // Un evento de puntero táctil (como en el móvil) en ese punto del panel, sobre ese elemento
        static void Puntero(VisualElement objetivo, Vector2 punto, EventType tipo)
        {
            var sistema = new Event
            {
                type = tipo, mousePosition = punto, button = 0, clickCount = 1, pointerType = UnityEngine.PointerType.Touch,
            };
            EventBase e = tipo == EventType.MouseDown ? PointerDownEvent.GetPooled(sistema)
                : tipo == EventType.MouseDrag ? PointerMoveEvent.GetPooled(sistema)
                : (EventBase)PointerUpEvent.GetPooled(sistema);
            using (e)
            {
                e.target = objetivo;
                objetivo.SendEvent(e);
            }
        }

        static readonly Direccion DireccionCalleAncha =
            new Direccion("Calle Ancha 10", "Sanlúcar de Barrameda", CalleAncha, "Calle Ancha", "10");

        /// <summary>Punto libre con geocodificador: Ir e Iniciar con la ficha en ese estado de la dirección.</summary>
        GeoFalso IrAlPuntoConDireccion(bool llegaAntes)
        {
            var geo = new GeoFalso();
            Crear(EstadoUbicacion.Activa, Cabildo, geocodificador: geo);
            var px = new PuntoPantalla(100f, 200f, 300f);
            mapa.suelo[(px.X, px.Y)] = CalleAncha;
            pantalla.PulsarLargoMapa(px);
            if (llegaAntes) geo.Inversas.Single().Responder(DireccionCalleAncha);
            Fila.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(120);
            pantalla.Iniciar();
            return geo;
        }

        [Test]
        public void Hoja_PuntoLibre_ConLaDireccionYaConocida()
        {
            IrAlPuntoConDireccion(llegaAntes: true);
            string calle = FichaLugar.TituloDireccion(DireccionCalleAncha, T);
            Assert.AreEqual(calle, Abajo.DestinoNombre.text);
            Assert.AreEqual("Sanlúcar de Barrameda", Abajo.DestinoDetalle.text);
            Assert.AreEqual(Textos.Formatear(T(ClavesTexto.RutaAnuncioDestino), calle, "Sanlúcar de Barrameda"), Abajo.AnuncioDestino);
        }

        [Test]
        public void Hoja_PuntoLibre_DireccionQueLlegaDespuesDeIr()
        {
            var geo = IrAlPuntoConDireccion(llegaAntes: false);
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.AreEqual(T(ClavesTexto.PuntoTipo), Abajo.DestinoDetalle.text, "Mientras se busca, «Punto en el mapa».");
            Assert.AreEqual(Textos.Formatear(T(ClavesTexto.RutaAnuncioDestino), "36,7781, -6,3515", T(ClavesTexto.PuntoTipo)),
                Abajo.AnuncioDestino);
            var inversa = geo.Inversas.Single();
            Assert.IsFalse(inversa.Ct.IsCancellationRequested, "Ir no cancela la dirección del punto de destino.");

            inversa.Responder(DireccionCalleAncha);
            string calle = FichaLugar.TituloDireccion(DireccionCalleAncha, T);
            Assert.AreEqual(calle, Abajo.DestinoNombre.text, "La fila se rehace al llegar la dirección.");
            Assert.AreEqual("Sanlúcar de Barrameda", Abajo.DestinoDetalle.text);
            Assert.AreEqual(Textos.Formatear(T(ClavesTexto.RutaAnuncioDestino), calle, "Sanlúcar de Barrameda"), Abajo.AnuncioDestino);
        }

        [Test]
        public void ComoLlegar_PuntoLibre_DireccionQueLlegaConLaHojaAbierta()
        {
            var geo = new GeoFalso();
            Crear(EstadoUbicacion.Activa, Cabildo, geocodificador: geo);
            var px = new PuntoPantalla(100f, 200f, 300f);
            mapa.suelo[(px.X, px.Y)] = CalleAncha;
            pantalla.PulsarLargoMapa(px);
            Fila.PulsarIr();
            Assert.AreEqual("a 36,7781, -6,3515", pantalla.ComoLlegar.Destino.text);
            geo.Inversas.Single().Responder(DireccionCalleAncha);
            Assert.AreEqual("a " + FichaLugar.TituloDireccion(DireccionCalleAncha, T), pantalla.ComoLlegar.Destino.text);
        }

        [Test]
        public void Hoja_PuntoLibre_QuitarLaRutaCancelaLaDireccion()
        {
            var geo = IrAlPuntoConDireccion(llegaAntes: false);
            pantalla.QuitarRuta();
            Assert.IsTrue(geo.Inversas.Single().Ct.IsCancellationRequested);
        }

        // ───────────────── Dirección o punto del mapa elegidos en el buscador (22/11/2026) ─────────────────

        /// <summary>Elige ese resultado en el buscador con un geocodificador que cuenta las preguntas de dirección.</summary>
        GeoFalso Elegir(ResultadoBusqueda resultado)
        {
            var geo = new GeoFalso();
            Crear(EstadoUbicacion.Activa, Cabildo, geocodificador: geo);
            pantalla.ElegirResultado(resultado);
            return geo;
        }

        static ResultadoBusqueda DeDireccion(Direccion d) => ResultadoBusqueda.DeDireccion(d, 100);

        static ResultadoBusqueda DePunto(string nombre, CategoriaPoi categoria, bool sanidadPublica = false) =>
            ResultadoBusqueda.DePunto(new PuntoInteres(1, nombre, "bank", "bank", 1, CalleAncha), categoria, sanidadPublica, 100, 3);

        /// <summary>Ficha de punto libre en CalleAncha con esos textos, los minutos pidiéndose y sin preguntar la dirección.</summary>
        void ComprobarFichaConocida(GeoFalso geo, string titulo, string tipo, string debajo)
        {
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreEqual(CalleAncha, pantalla.PuntoAbierto);
            Assert.AreEqual(CalleAncha, pantalla.PuntoChincheta, "Con su chincheta.");
            Assert.AreEqual(titulo, Ficha.Titulo.text);
            Assert.AreEqual(tipo, Ficha.Tipo.text);
            Assert.AreEqual(DisplayStyle.Flex, Ficha.GotaPunto.style.display.value);
            Assert.AreEqual(DisplayStyle.None, Ficha.Muestra.style.display.value);
            Assert.AreEqual(debajo ?? "", Ficha.Direccion.text);
            Assert.AreEqual(debajo != null ? DisplayStyle.Flex : DisplayStyle.None, Ficha.Direccion.style.display.value);
            Assert.IsFalse(Ficha.BuscandoDireccion);
            Assert.IsEmpty(geo.Inversas, "Sin petición a /reverse.");
            Assert.IsTrue(Ficha.AccionesVisibles);
            valhalla.De(ModoViaje.Coche).Responder(120);
            valhalla.De(ModoViaje.APie).Responder(300);
            Assert.AreEqual("2 min", Fila.Coche.Tiempo.text);
            Assert.AreEqual("5 min", Fila.APie.Tiempo.text);
        }

        [Test]
        public void Buscada_DireccionConCalle()
        {
            var geo = Elegir(DeDireccion(DireccionCalleAncha));
            ComprobarFichaConocida(geo, "Calle Ancha, 10", "Punto en el mapa", "Sanlúcar de Barrameda");
        }

        [Test]
        public void Buscada_DireccionSinCalle_ElNombreDelResultado()
        {
            var geo = Elegir(DeDireccion(new Direccion("Plaza del Cabildo", "Sanlúcar de Barrameda", CalleAncha)));
            ComprobarFichaConocida(geo, "Plaza del Cabildo", "Punto en el mapa", "Sanlúcar de Barrameda");
        }

        [Test]
        public void Buscada_LaLocalidadNoRepiteElTituloNiSaleVacia()
        {
            var geo = Elegir(DeDireccion(new Direccion("Chipiona", "chipiona", CalleAncha)));
            ComprobarFichaConocida(geo, "Chipiona", "Punto en el mapa", null);
            pantalla.ElegirResultado(DeDireccion(new Direccion("Calle Real", "", CalleAncha, "Calle Real")));
            Assert.AreEqual("Calle Real", Ficha.Titulo.text);
            Assert.AreEqual(DisplayStyle.None, Ficha.Direccion.style.display.value);
            Fila.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(120);
            pantalla.Iniciar();
            Assert.AreEqual("Calle Real", Abajo.DestinoNombre.text);
            Assert.AreEqual("Punto en el mapa", Abajo.DestinoDetalle.text);
        }

        [Test]
        public void Buscada_SitioConNombreYCalle_ElNombreYDebajoLaDireccion()
        {
            const string debajo = "Avenida Calzada, 20, Sanlúcar de Barrameda";
            var geo = Elegir(DeDireccion(new Direccion("Hotel Guadalquivir", "Sanlúcar de Barrameda", CalleAncha, "Avenida Calzada", "20")));
            ComprobarFichaConocida(geo, "Hotel Guadalquivir", "Punto en el mapa", debajo);
            Fila.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(120);
            pantalla.Iniciar();
            Assert.AreEqual("Hotel Guadalquivir", Abajo.DestinoNombre.text);
            Assert.AreEqual(debajo, Abajo.DestinoDetalle.text);
            Assert.IsNotNull(pantalla.RutaDibujada);
            Assert.AreEqual("Ruta a Hotel Guadalquivir", Arriba.Principal.text);
            Assert.IsEmpty(geo.Inversas);
        }

        [Test]
        public void Buscada_SitioConNombreYCalle_SinLocalidadNiNumero()
        {
            var geo = Elegir(DeDireccion(new Direccion("Hotel Guadalquivir", "", CalleAncha, "Avenida Calzada")));
            ComprobarFichaConocida(geo, "Hotel Guadalquivir", "Punto en el mapa", "Avenida Calzada");
        }

        [TestCase("Calle Ancha", "", "Calle Ancha")]
        [TestCase("CALLE ANCHÁ", "", "Calle Ancha")]
        [TestCase("calle ancha", "10", "Calle Ancha, 10")]
        [TestCase("Calle Ancha 10", "10", "Calle Ancha, 10")]
        [TestCase("Calle Ancha, 10", "10", "Calle Ancha, 10")]
        public void Buscada_ElNombreEsLaCalle_DebajoLaLocalidad(string nombre, string numero, string titulo)
        {
            var geo = Elegir(DeDireccion(new Direccion(nombre, "Sanlúcar de Barrameda", CalleAncha, "Calle Ancha", numero)));
            ComprobarFichaConocida(geo, titulo, "Punto en el mapa", "Sanlúcar de Barrameda");
        }

        [Test]
        public void Buscada_PuntoDelMapa()
        {
            var geo = Elegir(DePunto("Caixabank", CategoriaPoi.Bancos));
            ComprobarFichaConocida(geo, "Caixabank", "Banco", null);
        }

        [Test]
        public void Buscada_PuntoDelMapa_SanidadPublicaYSinCategoria()
        {
            var geo = Elegir(DePunto("Centro de Salud", CategoriaPoi.Salud, sanidadPublica: true));
            ComprobarFichaConocida(geo, "Centro de Salud", "Centro sanitario público", null);
            geo = Elegir(DePunto("Algo", CategoriaPoi.Ninguna));
            Assert.AreEqual("Algo", Ficha.Titulo.text);
            Assert.AreEqual("Punto en el mapa", Ficha.Tipo.text);
            Assert.IsEmpty(geo.Inversas);
        }

        [Test]
        public void Buscada_Ir_RutaAlPuntoYReabrirSinPreguntar()
        {
            var geo = Elegir(DeDireccion(DireccionCalleAncha));
            Fila.PulsarIr();
            Assert.AreEqual("a Calle Ancha, 10", pantalla.ComoLlegar.Destino.text);
            Assert.AreEqual(CalleAncha, valhalla.De(ModoViaje.Coche).Destino);
            valhalla.De(ModoViaje.Coche).Responder(120, 300);
            pantalla.Iniciar();
            Assert.IsTrue(pantalla.RutaActiva);
            Assert.AreEqual("Calle Ancha, 10", Abajo.DestinoNombre.text);
            Assert.AreEqual("Sanlúcar de Barrameda", Abajo.DestinoDetalle.text);
            Assert.AreEqual(FilaResultado.GlifoChincheta, Abajo.GlifoDestino);
            Assert.AreEqual("Ruta a Calle Ancha, 10", Arriba.Principal.text);
            Assert.IsNotNull(mapa.linea);

            pantalla.ReabrirFichaRuta();
            Assert.AreEqual("Calle Ancha, 10", Ficha.Titulo.text);
            Assert.AreEqual("Sanlúcar de Barrameda", Ficha.Direccion.text);
            Assert.IsEmpty(geo.Inversas, "Reabierta, tampoco se pregunta.");
            pantalla.CerrarFicha();
            Assert.AreEqual(CalleAncha, pantalla.PuntoChincheta);

            // Con otra ficha por medio (un punto libre de verdad, que sí pregunta), al reabrir vuelve la dirección
            pantalla.AbrirPunto(Cabildo);
            Assert.AreEqual(1, geo.Inversas.Count);
            Assert.IsNull(Ficha.Conocido);
            pantalla.CerrarFicha();
            pantalla.ReabrirFichaRuta();
            Assert.AreEqual("Calle Ancha, 10", Ficha.Titulo.text);
            Assert.AreEqual(1, geo.Inversas.Count);
        }

        [Test]
        public void Buscada_IrAUnPuntoDelMapa_ElDestinoLlevaSuCategoria()
        {
            var geo = Elegir(DePunto("Caixabank", CategoriaPoi.Bancos));
            Fila.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(120);
            pantalla.Iniciar();
            Assert.AreEqual("Caixabank", Abajo.DestinoNombre.text);
            Assert.AreEqual("Banco", Abajo.DestinoDetalle.text);
            Assert.AreEqual(Textos.Formatear(T(ClavesTexto.RutaAnuncioDestino), "Caixabank", "Banco"), Abajo.AnuncioDestino);
            pantalla.ReabrirFichaRuta();
            Assert.AreEqual("Caixabank", Ficha.Titulo.text);
            Assert.AreEqual("Banco", Ficha.Tipo.text);
            Assert.IsEmpty(geo.Inversas);
        }

        [Test]
        public void Buscada_TrasQuitarLaRuta_OtraRutaNoHeredaLoSabido()
        {
            var geo = Elegir(DePunto("Caixabank", CategoriaPoi.Bancos));
            Fila.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(120);
            pantalla.Iniciar();
            pantalla.QuitarRuta();
            pantalla.AbrirPunto(CalleAncha); // el mismo sitio, ahora como punto libre
            Assert.AreEqual("36,7781, -6,3515", Ficha.Titulo.text);
            Assert.AreEqual("Punto en el mapa", Ficha.Tipo.text);
            Assert.IsTrue(Ficha.BuscandoDireccion);
            Assert.AreEqual(1, geo.Inversas.Count);
            Fila.PulsarIr(); // las rutas salen de la caché
            pantalla.Iniciar();
            Assert.AreEqual("Punto en el mapa", Abajo.DestinoDetalle.text);
            pantalla.ReabrirFichaRuta();
            Assert.IsNull(Ficha.Conocido);

            pantalla.AbrirFicha(Areas);
            Assert.IsNull(Ficha.Conocido);
        }

        [Test]
        public void Buscada_CancelaLaDireccionDelPuntoAnterior()
        {
            var geo = new GeoFalso();
            Crear(EstadoUbicacion.Activa, Cabildo, geocodificador: geo);
            pantalla.AbrirPunto(Cabildo);
            pantalla.ElegirResultado(DePunto("Caixabank", CategoriaPoi.Bancos));
            Assert.IsTrue(geo.Inversas.Single().Ct.IsCancellationRequested);
            geo.Inversas.Single().Responder(DireccionCalleAncha);
            Assert.AreEqual("Caixabank", Ficha.Titulo.text);
        }

        [Test]
        public void Buscada_TalkBackModalYSeCierraConAtrasXYMapa()
        {
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            Elegir(DeDireccion(DireccionCalleAncha));
            Assert.IsTrue(Acc.Modal);
            Assert.AreSame(Ficha.Titulo, Acc.Enfocado);
            var recorrido = Acc.Recorrido();
            Assert.AreEqual("Calle Ancha, 10, Sanlúcar de Barrameda", recorrido[0]);
            Assert.AreEqual("Punto en el mapa", recorrido[1]);
            CollectionAssert.Contains(recorrido, "Ir a este punto");
            Assert.AreEqual("Cerrar ficha", recorrido.Last());
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(Acc.Modal);
            Assert.IsNull(pantalla.PuntoChincheta);
            Assert.AreSame(pantalla.Buscador, Acc.Enfocado, "El foco vuelve a la cápsula del buscador.");

            pantalla.ElegirResultado(DePunto("Caixabank", CategoriaPoi.Bancos));
            Assert.AreEqual("Caixabank", Acc.Recorrido()[0]);
            Assert.AreEqual("Banco", Acc.Recorrido()[1]);
            Ficha.BotonCerrar.Pulsar();
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.AreSame(pantalla.Buscador, Acc.Enfocado);

            pantalla.ElegirResultado(DePunto("Caixabank", CategoriaPoi.Bancos));
            Ficha.TocarFuera();
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsNull(pantalla.PuntoChincheta);
            Assert.AreSame(pantalla.Buscador, Acc.Enfocado);
        }

        class Inversa
        {
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<Direccion>> Tcs;
            public void Responder(Direccion d) => Tcs.TrySetResult(Resultado<Direccion>.Exito(d));
        }

        /// <summary>Geocodificador falso: cada geocodificación inversa queda pendiente hasta que la prueba responde.</summary>
        class GeoFalso : IGeocodificador
        {
            public readonly List<Inversa> Inversas = new List<Inversa>();

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct) =>
                Task.FromResult(Resultado<IReadOnlyList<Direccion>>.Fallo("no se usa", Array.Empty<Direccion>()));

            public Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct)
            {
                var i = new Inversa { Ct = ct, Tcs = new TaskCompletionSource<Resultado<Direccion>>() };
                Inversas.Add(i);
                ct.Register(() => i.Tcs.TrySetCanceled());
                return i.Tcs.Task;
            }
        }

        [Test]
        public void Hoja_LosTextosEstanEnLaTabla()
        {
            foreach (var clave in new[]
                     {
                         ClavesTexto.RutaFinalizar, ClavesTexto.RutaDesplegar, ClavesTexto.RutaPlegar, ClavesTexto.RutaEnCoche,
                         ClavesTexto.RutaAPie, ClavesTexto.RutaDestinoDetalle, ClavesTexto.RutaAnuncioDestino,
                     })
            {
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
                Assert.IsTrue(PrepararInterfaz.TextosEs.ContainsKey(clave), clave);
            }
        }

        [Test]
        public void TalkBack_UnMinutoYDistanciaEnMetros()
        {
            CrearConPosicion();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).ResponderConManiobras(45, 350);
            pantalla.Iniciar();
            Assert.AreEqual("Llegada 13:30, 1 minuto, 350 m", Abajo.Anuncio);
        }

        // ───────────────────────── Funciones puras de la ruta ─────────────────────────

        [Test]
        public void ZonaEncuadre_EntreLasTarjetasConSusMargenes()
        {
            // 412 × 915 dp en una pantalla de 824 × 1830 px (escala 0,5 dp por px)
            var z = PantallaMapa.ZonaEncuadre(824f, 1830f, 0.5f, 120f, 800f);
            Assert.AreEqual(64f, z.Izquierda, 1e-3f, "32 dp a la izquierda.");
            Assert.AreEqual(824f - 64f, z.Derecha, 1e-3f, "32 dp a la derecha.");
            Assert.AreEqual((120f + 16f) / 0.5f, z.Arriba, 1e-3f, "16 dp bajo la tarjeta de arriba.");
            Assert.AreEqual((800f - 16f) / 0.5f, z.Abajo, 1e-3f, "16 dp sobre la tarjeta de abajo.");

            // Sin medida, alto supuesto de las tarjetas
            var s = PantallaMapa.ZonaEncuadre(824f, 1830f, 0.5f, null, null);
            Assert.AreEqual((16f + PantallaMapa.AltoSupuestoTarjetaManiobraDp + 16f) / 0.5f, s.Arriba, 1e-3f);
            Assert.AreEqual((915f - PantallaMapa.AltoSupuestoTarjetaRutaDp - 16f) / 0.5f, s.Abajo, 1e-3f);

            // Sin sitio entre las tarjetas: toda la pantalla menos los lados
            var sinSitio = PantallaMapa.ZonaEncuadre(824f, 1830f, 0.5f, 600f, 500f);
            Assert.IsTrue(sinSitio.Valido);
            Assert.AreEqual(0f, sinSitio.Arriba);
            Assert.AreEqual(1830f, sinSitio.Abajo);
        }

        [Test]
        public void AnchoDeLaLinea_NueveMetrosYNuncaMenosDeSeisDp()
        {
            Assert.AreEqual(9f, ParametrosNegocio.RutaAnchoMetros);
            Assert.AreEqual(6f, ParametrosNegocio.RutaAnchoMinimoDp);
            Assert.AreEqual(16f, ParametrosNegocio.RutaEncuadreMargenDp);
            Assert.AreEqual(32f, ParametrosNegocio.RutaEncuadreLadoDp);
            // Cerca: 6 dp son menos de 9 m, se queda la calle
            Assert.AreEqual(9f, MapaVectorial.AnchoRutaMetros(300f, 60f, 1000f, 160f), 1e-4f);
            // Lejos: 2 · 4000 · tan 30° / 1000 = 4,62 m por px; 6 dp a 160 dpi son 6 px → 27,7 m
            float lejos = MapaVectorial.AnchoRutaMetros(4000f, 60f, 1000f, 160f);
            Assert.AreEqual(6f * 2f * 4000f * Mathf.Tan(30f * Mathf.Deg2Rad) / 1000f, lejos, 1e-3f);
            // A 480 dpi un dp son 3 px: el triple; sin dpi, como 160
            Assert.AreEqual(lejos * 3f, MapaVectorial.AnchoRutaMetros(4000f, 60f, 1000f, 480f), 1e-2f);
            Assert.AreEqual(lejos, MapaVectorial.AnchoRutaMetros(4000f, 60f, 1000f, 0f), 1e-3f);
            // Se rehace al cambiar más de un 10 %
            Assert.IsFalse(MapaVectorial.RehacerRuta(10f, 10.9f));
            Assert.IsTrue(MapaVectorial.RehacerRuta(10f, 11.2f));
            Assert.IsTrue(MapaVectorial.RehacerRuta(10f, 8.8f));
            Assert.IsTrue(MapaVectorial.RehacerRuta(0f, 9f), "Sin línea aún.");
        }

        [Test]
        public void TextosDeLasTarjetas()
        {
            Assert.AreEqual("Gira a la derecha en Calle Ancha", TarjetaManiobra.SinPunto("Gira a la derecha en Calle Ancha. "));
            Assert.AreEqual("Sigue recto", TarjetaManiobra.SinPunto("Sigue recto"));
            Assert.AreEqual("gira a la izquierda", TarjetaManiobra.Minuscula("Gira a la izquierda"));
            Assert.AreEqual("13:34", TarjetaRuta.HoraLlegada(HoraLocal, 240));
            Assert.AreEqual("00:03", TarjetaRuta.HoraLlegada(new DateTime(2026, 11, 20, 23, 58, 0), 300), "Pasada la medianoche.");
            Assert.AreEqual(("950", "m"), TarjetaRuta.PartirDistancia(950));
            Assert.AreEqual(("1,2", "km"), TarjetaRuta.PartirDistancia(1200));
        }

        [Test]
        public void LosTextosDeLaRutaEstanEnLaTabla()
        {
            foreach (var clave in new[]
                     {
                         ClavesTexto.RutaCalculando, ClavesTexto.RutaFallo, ClavesTexto.RutaA, ClavesTexto.RutaDespues,
                         ClavesTexto.RutaLlegada, ClavesTexto.RutaMin, ClavesTexto.RutaAnuncio, ClavesTexto.RutaAnuncioMinuto,
                         ClavesTexto.RutaAnuncioCalculando, ClavesTexto.RutaQuitar,
                     })
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
        }

        // ───────────────────────── Puerto real: línea y encuadre ─────────────────────────

        [Test]
        public void MapaVectorial_LineaConBordeColasTemaYQuitar()
        {
            double latAntes = Geo.LatitudOrigen, lonAntes = Geo.LongitudOrigen;
            Geo.LatitudOrigen = 36.7779; Geo.LongitudOrigen = -6.3528;
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = objCamara.AddComponent<CamaraMapa>();
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                var real = objMapa.AddComponent<MapaVectorial>();
                camara.distancia = 300f;
                IMapa puerto = real;
                puerto.MostrarRuta(new[] { Cabildo, CalleAncha, Areas.Posicion });
                Assert.IsNotNull(real.ObjetoRuta);
                Assert.AreSame(objMapa.transform, real.ObjetoRuta.transform.parent, "Cuelga del mapa, no de una tesela.");
                Assert.AreEqual(2, real.ObjetoRuta.transform.childCount, "Borde y relleno.");
                Assert.AreEqual(9f, real.AnchoRuta, 1e-3f, "Cerca, el ancho de la calle.");
                Assert.AreEqual(1960, real.MaterialBordeRuta.renderQueue);
                Assert.AreEqual(1970, real.MaterialRuta.renderQueue);
                Assert.AreEqual((float)UnityEngine.Rendering.CompareFunction.Always, real.MaterialRuta.GetFloat("_ZTest"));
                Assert.AreEqual(ColorHex("#2F5BEA"), real.MaterialRuta.color);
                Assert.AreEqual(ColorHex("#1E3FB0"), real.MaterialBordeRuta.color);

                puerto.AplicarTema(Tema.Oscuro);
                real.ColorearRutaAhora();
                Assert.AreEqual(ColorHex("#4C7DFF"), real.MaterialRuta.color);
                Assert.AreEqual(ColorHex("#2F5BEA"), real.MaterialBordeRuta.color);

                // Lejos, al menos 6 dp
                camara.distancia = 4000f;
                puerto.MostrarRuta(new[] { Cabildo, CalleAncha });
                var cam = objCamara.GetComponent<Camera>();
                Assert.AreEqual(MapaVectorial.AnchoRutaMetros(4000f, cam.fieldOfView, cam.pixelHeight, Screen.dpi), real.AnchoRuta, 1e-3f);
                Assert.Greater(real.AnchoRuta, 9f);

                puerto.QuitarRuta();
                Assert.IsNull(real.ObjetoRuta);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                UnityEngine.Object.DestroyImmediate(objCamara);
                Geo.LatitudOrigen = latAntes; Geo.LongitudOrigen = lonAntes;
            }
        }

        static Color ColorHex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static void FotogramaDe(MapaVectorial m) =>
            typeof(MapaVectorial).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(m, null);

        [Test]
        public void MapaVectorial_UpdateRecoloreaConElTemaYRehaceConElZoom()
        {
            double latAntes = Geo.LatitudOrigen, lonAntes = Geo.LongitudOrigen;
            Geo.LatitudOrigen = 36.7779; Geo.LongitudOrigen = -6.3528;
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = objCamara.AddComponent<CamaraMapa>();
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                var real = objMapa.AddComponent<MapaVectorial>();
                real.sombras = false; // Update no toca QualitySettings (se guardaría en el proyecto)
                IMapa puerto = real;
                camara.distancia = 300f;
                puerto.MostrarRuta(new[] { Cabildo, CalleAncha });
                FotogramaDe(real);
                Assert.AreEqual(9f, real.AnchoRuta, 1e-3f);
                var antes = real.ObjetoRuta;

                puerto.AplicarTema(Tema.Oscuro);
                FotogramaDe(real);
                Assert.AreEqual(ColorHex("#4C7DFF"), real.MaterialRuta.color, "Update recolorea con el tema.");
                Assert.AreEqual(ColorHex("#2F5BEA"), real.MaterialBordeRuta.color);
                Assert.AreSame(antes, real.ObjetoRuta, "Cambiar el tema no rehace la línea.");

                camara.distancia = 4000f;
                FotogramaDe(real);
                Assert.Greater(real.AnchoRuta, 9f, "Al alejar, la línea crece para no bajar de 6 dp.");
                Assert.AreNotSame(antes, real.ObjetoRuta, "Se ha rehecho.");
                var lejos = real.ObjetoRuta;
                FotogramaDe(real);
                Assert.AreSame(lejos, real.ObjetoRuta, "Sin cambio de zoom no se rehace.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                UnityEngine.Object.DestroyImmediate(objCamara);
                Geo.LatitudOrigen = latAntes; Geo.LongitudOrigen = lonAntes;
            }
        }

        [Test]
        public void MapaVectorial_EncuadrarDejaLaRutaEnLaZonaLibre()
        {
            double latAntes = Geo.LatitudOrigen, lonAntes = Geo.LongitudOrigen;
            Geo.LatitudOrigen = 36.7779; Geo.LongitudOrigen = -6.3528;
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            objCamara.AddComponent<CamaraMapa>();
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                IMapa puerto = objMapa.AddComponent<MapaVectorial>();
                var cam = objCamara.GetComponent<Camera>();
                float w = cam.pixelWidth, h = cam.pixelHeight, alto = Screen.height;
                // Márgenes desiguales (origen arriba): poco arriba, mucho abajo y a la derecha; dentro de la imagen de la cámara
                float techo = alto - h; // fila de arriba de la imagen de la cámara, contada desde arriba
                var libre = new RectPantalla(w * 0.05f, techo + h * 0.1f, w * 0.7f, techo + h * 0.6f);
                var ruta = new[] { Cabildo, CalleAncha, Areas.Posicion, new Coordenada(36.7760, -6.3560) };
                puerto.Encuadrar(ruta, libre);
                foreach (var c in ruta)
                {
                    var p = puerto.Proyectar(c);
                    Assert.IsTrue(p.Delante);
                    Assert.That(p.X >= libre.Izquierda - 1f && p.X <= libre.Derecha + 1f && p.Y >= libre.Arriba - 1f && p.Y <= libre.Abajo + 1f,
                        $"{c} cae en {p}, fuera de {libre}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                UnityEngine.Object.DestroyImmediate(objCamara);
                Geo.LatitudOrigen = latAntes; Geo.LongitudOrigen = lonAntes;
            }
        }

        [TestCase(0f, 50f)]
        [TestCase(-15f, 50f)]
        [TestCase(40f, 70f)]
        public void Encuadrar_TodosLosPuntosCabenEnElRectanguloSinGirarNiInclinar(float giro, float inclinacion)
        {
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            try
            {
                var camara = objCamara.AddComponent<CamaraMapa>();
                var cam = objCamara.GetComponent<Camera>();
                camara.giro = giro; camara.inclinacion = inclinacion; camara.distancia = 900f;
                float w = cam.pixelWidth, h = cam.pixelHeight;
                // Zona libre: deja arriba y abajo sitio para las tarjetas
                var libre = Rect.MinMaxRect(w * 0.08f, h * 0.15f, w * 0.92f, h * 0.8f);
                var puntos = new List<Vector3>
                {
                    new Vector3(-150f, 0f, -300f), new Vector3(-150f, 0f, 20f), new Vector3(210f, 0f, 20f), new Vector3(210f, 0f, 260f),
                };
                camara.Encuadrar(puntos, libre);

                Assert.AreEqual(giro, camara.giro, 1e-4f);
                Assert.AreEqual(inclinacion, camara.inclinacion, 1e-4f);
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var p in puntos)
                {
                    var s = cam.WorldToScreenPoint(p);
                    Assert.Greater(s.z, 0f);
                    Assert.That(libre.Contains(new Vector2(s.x, s.y)), $"{p} cae en {s}, fuera de {libre}");
                    x0 = Mathf.Min(x0, s.x); y0 = Mathf.Min(y0, s.y); x1 = Mathf.Max(x1, s.x); y1 = Mathf.Max(y1, s.y);
                }
                // Lo más cerca posible: la caja llena el ancho o el alto de la zona
                Assert.That((x1 - x0) / libre.width > 0.85f || (y1 - y0) / libre.height > 0.85f,
                    $"caja {x1 - x0:F0}×{y1 - y0:F0} en {libre.width:F0}×{libre.height:F0}");
            }
            finally { UnityEngine.Object.DestroyImmediate(objCamara); }
        }

        [Test]
        public void Encuadrar_SinPasarLosLimitesDeLaCamara()
        {
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            try
            {
                var camara = objCamara.AddComponent<CamaraMapa>();
                var cam = objCamara.GetComponent<Camera>();
                var libre = new Rect(0f, 0f, cam.pixelWidth, cam.pixelHeight);
                camara.Encuadrar(new List<Vector3> { new Vector3(-40000f, 0f, 0f), new Vector3(40000f, 0f, 0f) }, libre);
                Assert.AreEqual(camara.distanciaMax, camara.distancia, 1e-3f, "80 km no caben: se queda en el máximo.");
                camara.Encuadrar(new List<Vector3> { new Vector3(10f, 0f, 10f), new Vector3(12f, 0f, 11f) }, libre);
                Assert.AreEqual(camara.distanciaMin, camara.distancia, 1e-3f, "2 m: el mínimo.");
            }
            finally { UnityEngine.Object.DestroyImmediate(objCamara); }
        }
    }
}
