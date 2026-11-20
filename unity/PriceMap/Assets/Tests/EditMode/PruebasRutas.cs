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
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Rutas y tiempos de la ficha (historia 2.6, parte 1): polilínea, lectores de Valhalla y OSRM con JSON en línea,
    /// URL y cuerpo de las peticiones, ServicioRutas (dos modos, reserva, 8 s, caché y cancelación) con servicios
    /// falsos, y la fila de acciones en la pantalla real, una prueba por fila de la matriz. Nada sale a la red.
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
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add => pulsacion += value; remove => pulsacion -= value; }
            public Coordenada? Desproyectar(PuntoPantalla punto) => suelo.TryGetValue((punto.X, punto.Y), out var c) ? c : (Coordenada?)null;
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
        void Crear(EstadoUbicacion estado, Coordenada? posicion, bool conRutas = true)
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            prefs.Guardar(ServicioUbicacion.ClaveExplicacion, "1"); // la explicación ya salió: no se abre sola
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            gps = new UbicacionFalsa { Estado = estado, Posicion = posicion };
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, new ServicioUbicacion(gps, prefs), rutas: conRutas ? servicio : null);
            tema.Iniciar();
            filtro.Iniciar();
        }

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
            Assert.AreEqual(TimeSpan.FromSeconds(8), ServicioRutas.TiempoMaximo);
            Assert.AreEqual(TimeSpan.FromMinutes(2), ServicioRutas.DuracionCache);
        }

        // ───────────────────────── Peticiones ─────────────────────────

        static readonly Coordenada OrigenExacto = new Coordenada(36.7779123456, -6.3528123456);
        static readonly Coordenada DestinoExacto = new Coordenada(36.7781412345, -6.3514812345);

        [Test]
        public void Valhalla_CuerpoConShortestIdiomaManiobrasYPrecisiones()
        {
            Assert.AreEqual("https://valhalla1.openstreetmap.de/route", RutasValhalla.Url);
            Assert.AreEqual(
                "{\"locations\":[{\"lat\":36.77791,\"lon\":-6.35281},{\"lat\":36.778141,\"lon\":-6.351481}]," +
                "\"costing\":\"auto\",\"costing_options\":{\"auto\":{\"shortest\":true}},\"language\":\"es-ES\"," +
                "\"directions_type\":\"maneuvers\",\"units\":\"kilometers\"}",
                RutasValhalla.Cuerpo(OrigenExacto, DestinoExacto, ModoViaje.Coche),
                "Origen con 5 decimales y destino con 6.");
            var aPie = RutasValhalla.Cuerpo(OrigenExacto, DestinoExacto, ModoViaje.APie);
            StringAssert.Contains("\"costing\":\"pedestrian\"", aPie);
            StringAssert.Contains("\"costing_options\":{\"pedestrian\":{\"shortest\":true}}", aPie);
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

        // Dos alternativas: la primera más rápida pero más larga; se queda la más corta (la segunda)
        const string RespuestaOsrm = @"{""code"":""Ok"",""routes"":[
            {""geometry"":""_p~iF~ps|U_ulLnnqC"",""legs"":[{""steps"":[]}],""distance"":950.2,""duration"":120.0,""weight"":120},
            {""geometry"":""wyvceA~xvbK_NoqAgVqb@"",""legs"":[{""steps"":[
              {""geometry"":""x"",""maneuver"":{""bearing_after"":90,""location"":[-6.3528,36.7779],""type"":""depart""},""distance"":120.4,""duration"":20,""name"":""Plaza del Cabildo""},
              {""geometry"":""x"",""maneuver"":{""location"":[-6.35148,36.77814],""modifier"":""right"",""type"":""turn""},""distance"":60.1,""duration"":30,""name"":""Calle Ancha""},
              {""geometry"":""x"",""maneuver"":{""location"":[-6.350911,36.778512],""type"":""arrive""},""distance"":0,""duration"":0,""name"":""""}],
              ""distance"":180.5,""duration"":150.0}],""distance"":180.5,""duration"":150.0,""weight"":150}],
            ""waypoints"":[]}";

        [Test]
        public void LecturaOsrm_SeQuedaLaAlternativaMasCortaConManiobrasSinTexto()
        {
            var r = LecturaOsrm.Leer(RespuestaOsrm);
            Assert.IsTrue(r.Ok, r.Error);
            var ruta = r.Valor;
            Assert.AreEqual(180.5, ruta.Metros, 1e-6, "La de menor distancia, aunque tarde más.");
            Assert.AreEqual(150.0, ruta.Segundos, 1e-6);
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
        public void Servicio_ElegirParaIrGuardaModoDestinoYRuta()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            valhalla.De(ModoViaje.APie).Responder(900);
            servicio.ElegirParaIr(ModoViaje.APie, CalleAncha);
            Assert.AreEqual(ModoViaje.APie, servicio.ModoElegido);
            Assert.AreEqual(CalleAncha, servicio.DestinoElegido);
            Assert.AreEqual(15, servicio.RutaElegida.Minutos);
            servicio.ElegirParaIr(ModoViaje.Coche, CalleAncha);
            Assert.IsNull(servicio.RutaElegida, "El coche aún no ha llegado.");
        }

        [Test]
        public void Servicio_IrAntesDeLaRutaLaRellenaAlLlegar()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            servicio.ElegirParaIr(ModoViaje.APie, CalleAncha);
            Assert.IsNull(servicio.RutaElegida);
            valhalla.De(ModoViaje.Coche).Responder(330);
            Assert.IsNull(servicio.RutaElegida, "La del otro modo no cuenta.");
            valhalla.De(ModoViaje.APie).Responder(900);
            Assert.AreEqual(15, servicio.RutaElegida.Minutos, "Llega después de Ir y queda elegida.");
        }

        [Test]
        public void Servicio_CancelarOlvidaLoElegido()
        {
            servicio.PedirTiempos(Cabildo, CalleAncha);
            valhalla.De(ModoViaje.Coche).Responder(330);
            servicio.ElegirParaIr(ModoViaje.Coche, CalleAncha);
            Assert.IsNotNull(servicio.RutaElegida);
            servicio.Cancelar();
            Assert.IsNull(servicio.ModoElegido);
            Assert.IsNull(servicio.DestinoElegido);
            Assert.IsNull(servicio.RutaElegida);

            // Abrir otra ficha (PedirTiempos cancela) también lo olvida
            servicio.ElegirParaIr(ModoViaje.Coche, CalleAncha);
            servicio.PedirTiempos(Cabildo, Areas.Posicion);
            Assert.IsNull(servicio.ModoElegido);
        }

        [Test]
        public void CerrarLaFicha_OlvidaLoElegidoConIr()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.Coche).Responder(330);
            Fila.PulsarIr();
            Assert.IsNotNull(servicio.RutaElegida);
            pantalla.CerrarFicha();
            Assert.IsNull(servicio.ModoElegido);
            Assert.IsNull(servicio.RutaElegida, "La parte 2 no dibuja la ruta de una ficha cerrada.");
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
            Assert.IsNull(servicio.ModoElegido);
        }

        [TestCase(EstadoUbicacion.Denegado)]
        [TestCase(EstadoUbicacion.Apagada)]
        public void SinPosicion_IrConLaUbicacionDesactivadaAbreElAviso(EstadoUbicacion estado)
        {
            Crear(estado, null);
            pantalla.AbrirFicha(Areas);
            Fila.PulsarIr();
            Assert.IsTrue(pantalla.AvisoAbierto);
            Assert.IsNull(servicio.ModoElegido);
        }

        [Test]
        public void SinPosicion_IrBuscandoNoHaceNadaComoElBoton()
        {
            Crear(EstadoUbicacion.Buscando, null);
            pantalla.AbrirFicha(Areas);
            Fila.PulsarIr();
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.ModalUbicacionAbierta);
            Assert.IsNull(servicio.ModoElegido);
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
        public void IrConPosicion_DejaElModoYLaRutaElegidosSinCerrarLaFicha()
        {
            CrearConPosicion();
            pantalla.AbrirFicha(Areas);
            valhalla.De(ModoViaje.APie).Responder(900);
            Fila.APie.Pulsar();
            Fila.PulsarIr();
            Assert.AreEqual(ModoViaje.APie, servicio.ModoElegido);
            Assert.AreEqual(Areas.Posicion, servicio.DestinoElegido);
            Assert.AreEqual(15, servicio.RutaElegida.Minutos);
            Assert.IsTrue(pantalla.FichaAbierta, "Dibujar la ruta es la parte 2.");
            Assert.IsFalse(pantalla.ModalUbicacionAbierta);
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
    }
}
