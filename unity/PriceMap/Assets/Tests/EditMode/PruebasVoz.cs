using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
using PriceMap.Adaptadores.Rutas;
using PriceMap.Adaptadores.Voz;
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
    /// Indicaciones por voz (historia 2.8, parte 1): el texto corto de Valhalla, ReglaVoz (una prueba por fila de la
    /// matriz), ServicioVoz con una voz falsa que apunta lo dicho, el plugin y el manifiesto de Android, y la pantalla real
    /// con un mapa falso (botón de voz, fila de los modos, TalkBack). Nada sale a la red ni suena.
    /// </summary>
    public class PruebasVoz
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        static readonly Coordenada Destino = new Coordenada(36.78060, -6.34945);
        const double MetrosPorGrado = Coordenada.RadioTierraMetros * Math.PI / 180.0;

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        /// <summary>Punto a esos metros al norte y al este del Cabildo.</summary>
        static Coordenada M(double norte, double este) =>
            new Coordenada(Cabildo.Latitud + norte / MetrosPorGrado,
                Cabildo.Longitud + este / (MetrosPorGrado * Math.Cos(Cabildo.Latitud * Math.PI / 180.0)));

        const string Giro = "Gire a la derecha por Calle Ancha.";
        const string GiroCorto = "Gire a la derecha.";

        static Maniobra Derecha(Coordenada donde, double metros = 200) =>
            new Maniobra(TipoManiobra.Derecha, Giro, donde, metros, GiroCorto);

        /// <summary>1000 m al norte, a la derecha 200 m y la llegada.</summary>
        static Ruta RutaLarga(bool conTextos = true) =>
            new Ruta(new[] { M(0, 0), M(500, 0), M(1000, 0), M(1000, 200) }, 1200, 600, new[]
            {
                new Maniobra(TipoManiobra.Salida, conTextos ? "Diríjase al norte por Calle Regina." : "", M(0, 0), 1000),
                new Maniobra(TipoManiobra.Derecha, conTextos ? Giro : "", M(1000, 0), 200, conTextos ? GiroCorto : null),
                new Maniobra(TipoManiobra.Llegada, conTextos ? "Ha llegado a su destino." : "", M(1000, 200), 0),
            });

        /// <summary>Estado con esa maniobra siguiente a esos metros (y, si se da, la de después).</summary>
        static EstadoNavegacion Estado(Maniobra siguiente, double metros, Maniobra despues = null, double metrosDespues = 0) =>
            new EstadoNavegacion(default, 0, 0, 0, 1000, 600, siguiente == null ? -1 : 1, siguiente, metros, despues, metrosDespues);

        /// <summary>Lo que dice la regla con cada distancia, en orden (sin los null).</summary>
        static List<string> Decir(ReglaVoz regla, ModoVoz modo, Maniobra m, params double[] metros)
        {
            var dicho = new List<string>();
            foreach (var d in metros)
            {
                var t = regla.Avisar(Estado(m, d), modo);
                if (t != null) dicho.Add(t);
            }
            return dicho;
        }

        // ───────────────────────── Texto corto de Valhalla ─────────────────────────

        [Test]
        public void Maniobra_SinTextoCorto_EsElTexto()
        {
            var m = new Maniobra(TipoManiobra.Derecha, Giro, Cabildo, 10);
            Assert.AreEqual(Giro, m.TextoCorto);
            Assert.AreEqual(GiroCorto, Derecha(Cabildo).TextoCorto);
        }

        [Test]
        public void Valhalla_LeeElTextoCortoDeLaManiobra()
        {
            const string json = @"{""trip"":{""units"":""kilometers"",""summary"":{""length"":0.18,""time"":50.6},
            ""legs"":[{""maneuvers"":[
              {""type"":1,""instruction"":""Diríjase al este por Plaza del Cabildo."",""length"":0.12,""begin_shape_index"":0},
              {""type"":10,""instruction"":""Gire a la derecha por Calle Ancha."",""verbal_succinct_transition_instruction"":""Gire a la derecha."",""length"":0.06,""begin_shape_index"":1},
              {""type"":4,""instruction"":""Ha llegado a su destino."",""length"":0,""begin_shape_index"":2}],
              ""shape"":""wyvceA~xvbK_NoqAgVqb@"",""summary"":{""length"":0.18,""time"":50.6}}]}}";
            var r = LecturaValhalla.Leer(json);
            Assert.IsTrue(r.Ok, r.Error);
            var maniobras = r.Valor.Maniobras;
            Assert.AreEqual("Gire a la derecha.", maniobras[1].TextoCorto);
            Assert.AreEqual("Gire a la derecha por Calle Ancha.", maniobras[1].Texto);
            Assert.AreEqual(maniobras[0].Texto, maniobras[0].TextoCorto, "Sin texto corto, el mismo texto.");
        }

        // ───────────────────────── ReglaVoz: la matriz ─────────────────────────

        [Test]
        public void Regla_Empezar_DiceLaSiguienteConSuDistanciaRedondeada()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var dicho = Decir(regla, ModoVoz.Todo, Derecha(M(1000, 0)), 820, 815, 790);
            CollectionAssert.AreEqual(new[] { "En 800 metros, gire a la derecha por Calle Ancha." }, dicho);
        }

        [Test]
        public void Regla_Empezar_ConLaSalidaDelanteDiceLaDeDespues()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var salida = new Maniobra(TipoManiobra.Salida, "Diríjase al norte.", Cabildo, 820);
            var giro = Derecha(M(820, 0));
            Assert.AreEqual("En 800 metros, gire a la derecha por Calle Ancha.", regla.Avisar(Estado(salida, 0, giro, 820), ModoVoz.Todo));
            // Al dejar atrás la salida, la siguiente es el mismo giro: no se repite
            Assert.IsNull(regla.Avisar(Estado(giro, 810), ModoVoz.Todo));
        }

        [Test]
        public void Regla_Intermedio_ConTodoA300mUnaVez()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var m = Derecha(M(1000, 0));
            var dicho = Decir(regla, ModoVoz.Todo, m, 820, 310, 290, 280, 200);
            CollectionAssert.AreEqual(new[]
            {
                "En 800 metros, gire a la derecha por Calle Ancha.",
                "En 300 metros, gire a la derecha por Calle Ancha.",
            }, dicho);
        }

        [Test]
        public void Regla_Importante_SinIntermedio()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var dicho = Decir(regla, ModoVoz.Importante, Derecha(M(1000, 0)), 820, 310, 290, 200);
            CollectionAssert.AreEqual(new[] { "En 800 metros, gire a la derecha por Calle Ancha." }, dicho);
        }

        [Test]
        public void Regla_Intermedio_NoSiLaSiguienteSeDijoCerca()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            // Dicha a 400 m (menos de 1,5 × 300): el de 300 m sería repetirla
            var dicho = Decir(regla, ModoVoz.Todo, Derecha(M(400, 0)), 400, 290, 200);
            CollectionAssert.AreEqual(new[] { "En 400 metros, gire a la derecha por Calle Ancha." }, dicho);
            // A más de 450 m, sí
            regla = new ReglaVoz(ModoViaje.Coche);
            dicho = Decir(regla, ModoVoz.Todo, Derecha(M(460, 0)), 460, 290);
            Assert.AreEqual(2, dicho.Count);
        }

        [Test]
        public void Regla_Momento_A40mUnaVezConElTextoCorto()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var dicho = Decir(regla, ModoVoz.Todo, Derecha(M(1000, 0)), 820, 290, 41, 39, 30, 10, 0);
            CollectionAssert.AreEqual(new[]
            {
                "En 800 metros, gire a la derecha por Calle Ancha.",
                "En 300 metros, gire a la derecha por Calle Ancha.",
                "Gire a la derecha.",
            }, dicho);
        }

        [Test]
        public void Regla_Cerca_SiYaEstaAMenosDe40mSoloElDelMomento()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var dicho = Decir(regla, ModoVoz.Todo, Derecha(M(30, 0)), 30, 20, 5);
            CollectionAssert.AreEqual(new[] { "Gire a la derecha." }, dicho);
        }

        [Test]
        public void Regla_APie_Intermedio50mYMomento15m()
        {
            var regla = new ReglaVoz(ModoViaje.APie);
            var dicho = Decir(regla, ModoVoz.Todo, Derecha(M(300, 0)), 296, 52, 48, 16, 14);
            CollectionAssert.AreEqual(new[]
            {
                "En 300 metros, gire a la derecha por Calle Ancha.",
                "En 50 metros, gire a la derecha por Calle Ancha.",
                "Gire a la derecha.",
            }, dicho);
        }

        [Test]
        public void Regla_Nada_Calla()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            CollectionAssert.IsEmpty(Decir(regla, ModoVoz.Nada, Derecha(M(1000, 0)), 820, 290, 39, 0));
        }

        [Test]
        public void Regla_PasarUnaManiobra_DiceLaSiguiente()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var primera = Derecha(M(100, 0));
            var segunda = new Maniobra(TipoManiobra.Izquierda, "Gire a la izquierda por Calle Bolsa.", M(700, 0), 0, "Gire a la izquierda.");
            Assert.IsNotNull(regla.Avisar(Estado(primera, 100), ModoVoz.Importante));
            Assert.AreEqual("Gire a la derecha.", regla.Avisar(Estado(primera, 35), ModoVoz.Importante));
            Assert.AreEqual("En 600 metros, gire a la izquierda por Calle Bolsa.", regla.Avisar(Estado(segunda, 590), ModoVoz.Importante));
        }

        [Test]
        public void Regla_SalidaLlegadaYSinTexto_NoSeDicen()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var llegada = new Maniobra(TipoManiobra.Llegada, "Ha llegado a su destino.", M(500, 0), 0);
            CollectionAssert.IsEmpty(Decir(regla, ModoVoz.Todo, llegada, 500, 290, 30, 0));
            var sinTexto = new Maniobra(TipoManiobra.Derecha, "", M(500, 0), 0);
            CollectionAssert.IsEmpty(Decir(new ReglaVoz(ModoViaje.Coche), ModoVoz.Todo, sinTexto, 500, 290, 30));
            Assert.IsNull(new ReglaVoz(ModoViaje.Coche).Avisar(Estado(null, 0), ModoVoz.Todo), "Sin maniobra siguiente (OSRM, aproximada), calla.");
        }

        [Test]
        public void Regla_DeNadaATodo_DiceLaSiguienteConSuDistanciaDeAhora()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var m = Derecha(M(1000, 0));
            Assert.IsNull(regla.Avisar(Estado(m, 800), ModoVoz.Nada));
            Assert.AreEqual("En 600 metros, gire a la derecha por Calle Ancha.", regla.Avisar(Estado(m, 600), ModoVoz.Todo));
        }

        [Test]
        public void Regla_Reiniciar_EmpiezaDeCero()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var m = Derecha(M(1000, 0));
            Assert.IsNotNull(regla.Avisar(Estado(m, 820), ModoVoz.Todo));
            regla.Reiniciar();
            Assert.AreEqual("En 800 metros, gire a la derecha por Calle Ancha.", regla.Avisar(Estado(m, 800), ModoVoz.Todo));
        }

        [TestCase(820, ModoViaje.Coche, "800 metros")]
        [TestCase(824.9, ModoViaje.Coche, "800 metros")]
        [TestCase(825, ModoViaje.Coche, "850 metros")]
        [TestCase(5, ModoViaje.Coche, "50 metros")]
        [TestCase(48, ModoViaje.APie, "50 metros")]
        [TestCase(296, ModoViaje.APie, "300 metros")]
        [TestCase(3, ModoViaje.APie, "10 metros")]
        [TestCase(980, ModoViaje.Coche, "1 kilómetro")]
        [TestCase(1500, ModoViaje.Coche, "1,5 kilómetros")]
        [TestCase(1530, ModoViaje.Coche, "1,5 kilómetros")]
        [TestCase(2010, ModoViaje.Coche, "2 kilómetros")]
        [TestCase(12340, ModoViaje.Coche, "12,3 kilómetros")]
        [TestCase(12350, ModoViaje.Coche, "12,4 kilómetros")]
        public void Regla_Distancias(double metros, ModoViaje viaje, string esperado) =>
            Assert.AreEqual(esperado, ReglaVoz.Distancia(metros, viaje));

        [Test]
        public void Regla_ClavesDelModo()
        {
            foreach (var m in new[] { ModoVoz.Todo, ModoVoz.Importante, ModoVoz.Nada })
                Assert.AreEqual(m, ReglaVoz.DeClave(ReglaVoz.AClave(m)));
            Assert.AreEqual(ModoVoz.Todo, ReglaVoz.DeClave(null), "Sin valor guardado, todo.");
            Assert.AreEqual(ModoVoz.Todo, ReglaVoz.DeClave("raro"));
            Assert.AreEqual("importante", ReglaVoz.AClave(ModoVoz.Importante));
        }

        // ───────────────────────── Falsos ─────────────────────────

        /// <summary>Voz falsa: apunta lo dicho y las veces que se le manda callar.</summary>
        class VozFalsa : IVoz
        {
            public readonly List<string> Dicho = new List<string>();
            public int Paradas;
            public void Hablar(string texto) => Dicho.Add(texto);
            public void Parar() => Paradas++;
        }

        class Peticion
        {
            public ModoViaje Modo;
            public TaskCompletionSource<Resultado<Ruta>> Tcs;
            public void Responder(Ruta ruta) => Tcs.TrySetResult(Resultado<Ruta>.Exito(ruta));
        }

        class RutasFalsas : IRutas
        {
            public readonly List<Peticion> Peticiones = new List<Peticion>();

            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                var p = new Peticion { Modo = modo, Tcs = new TaskCompletionSource<Resultado<Ruta>>() };
                Peticiones.Add(p);
                ct.Register(() => p.Tcs.TrySetCanceled());
                return p.Tcs.Task;
            }

            public Peticion De(ModoViaje modo) => Peticiones.Last(p => p.Modo == modo);
        }

        static Task Esperar(TimeSpan duracion, CancellationToken ct) => new TaskCompletionSource<bool>().Task;

        class PreferenciasFalsas : IPreferencias
        {
            public readonly Dictionary<string, string> Datos = new Dictionary<string, string>();
            public string Leer(string clave) => Datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => Datos[clave] = valor;
            public void Borrar(string clave) => Datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class AccesibilidadFalsa : IAccesibilidadSistema { public bool AnimacionesQuitadas { get; set; } }

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.Activa;
            public Coordenada? Posicion { get; set; }
            public float? Rumbo { get; set; }
            public event Action Cambiado;
            public int Lecturas { get; private set; }
            public void PedirPermiso() { }
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
            public bool RutaActiva { get; private set; }
            public bool RutaSoloPrimerPlano => false;
            public Resultado<bool> IniciarRuta(string texto) { RutaActiva = true; return Resultado<bool>.Exito(true); }
            public void ActualizarNotificacion(string texto) { }
            public void TerminarRuta() => RutaActiva = false;
            public void Mover(Coordenada c) { Lecturas++; Posicion = c; Cambiado?.Invoke(); }
        }

        /// <summary>Mapa falso: todo cae en el centro de la pantalla; Tocar lanza su toque.</summary>
        class MapaFalso : IMapa
        {
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => new PuntoPantalla(Screen.width / 2f, Screen.height / 2f, 300f);
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque;
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
            public void Seguir(Coordenada posicion, float giro, float inclinacion, float distancia, PuntoPantalla enPantalla, bool animado) { }
            public void DejarDeSeguir() { }
            public event Action GestoUsuario { add { } remove { } }
            public void Tocar(PuntoPantalla p) => Toque?.Invoke(p);
        }

        SynchronizationContext contextoAntes;
        RutasFalsas valhalla;
        UbicacionFalsa gps;
        ServicioUbicacion ubicacion;
        ServicioRutas rutas;
        ServicioNavegacion nav;
        VozFalsa vozFalsa;
        PreferenciasFalsas prefs;
        ServicioVoz voz;
        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        readonly List<string> anuncios = new List<string>();

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones corren al responder
            Textos.Forzados = T;
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            valhalla = new RutasFalsas();
            gps = new UbicacionFalsa { Posicion = M(180, 0) };
            prefs = new PreferenciasFalsas();
            ubicacion = new ServicioUbicacion(gps, prefs);
            rutas = new ServicioRutas(valhalla, null, Esperar);
            nav = new ServicioNavegacion(ubicacion, rutas, Esperar);
            vozFalsa = new VozFalsa();
            voz = new ServicioVoz(nav, vozFalsa, prefs);
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

        void Empezar(ModoViaje modo = ModoViaje.Coche, Ruta ruta = null) =>
            nav.Empezar(ruta ?? RutaLarga(), Destino, "Clínica Areas", modo);

        // ───────────────────────── ServicioVoz con una voz falsa ─────────────────────────

        [Test]
        public void Servicio_RecorridoEnCoche_SiguienteIntermedioYMomentoUnaVezCadaUno()
        {
            Empezar();
            CollectionAssert.AreEqual(new[] { "En 800 metros, gire a la derecha por Calle Ancha." }, vozFalsa.Dicho, "Al empezar, a 820 m.");
            gps.Mover(M(690, 0)); // 310 m
            gps.Mover(M(710, 0)); // 290 m
            gps.Mover(M(720, 0));
            gps.Mover(M(959, 0)); // 41 m
            gps.Mover(M(961, 0)); // 39 m
            gps.Mover(M(970, 0));
            gps.Mover(M(1000, 50)); // pasado el giro: la siguiente es la llegada (2.9)
            gps.Mover(M(1000, 150));
            CollectionAssert.AreEqual(new[]
            {
                "En 800 metros, gire a la derecha por Calle Ancha.",
                "En 300 metros, gire a la derecha por Calle Ancha.",
                "Gire a la derecha.",
            }, vozFalsa.Dicho);
        }

        [Test]
        public void Servicio_Importante_SinElIntermedio()
        {
            voz.Elegir(ModoVoz.Importante);
            Empezar();
            gps.Mover(M(690, 0));
            gps.Mover(M(710, 0));
            CollectionAssert.AreEqual(new[] { "En 800 metros, gire a la derecha por Calle Ancha." }, vozFalsa.Dicho);
            gps.Mover(M(961, 0));
            Assert.AreEqual("Gire a la derecha.", vozFalsa.Dicho.Last());
        }

        [Test]
        public void Servicio_APie_EnCincuentaMetros()
        {
            gps.Posicion = M(700, 0);
            Empezar(ModoViaje.APie);
            Assert.AreEqual("En 300 metros, gire a la derecha por Calle Ancha.", vozFalsa.Dicho.Single());
            gps.Mover(M(948, 0)); // 52 m
            gps.Mover(M(952, 0)); // 48 m
            Assert.AreEqual("En 50 metros, gire a la derecha por Calle Ancha.", vozFalsa.Dicho.Last());
            Assert.AreEqual(2, vozFalsa.Dicho.Count);
        }

        [Test]
        public void Servicio_Nada_NoHablaYLaNavegacionSigue()
        {
            voz.Elegir(ModoVoz.Nada);
            Empezar();
            gps.Mover(M(710, 0));
            gps.Mover(M(961, 0));
            CollectionAssert.IsEmpty(vozFalsa.Dicho);
            Assert.IsTrue(nav.Activa);
            Assert.AreEqual(39, nav.Estado.MetrosASiguiente, 0.5);
        }

        [Test]
        public void Servicio_SinTextos_Calla()
        {
            Empezar(ruta: RutaLarga(conTextos: false));
            gps.Mover(M(710, 0));
            gps.Mover(M(961, 0));
            CollectionAssert.IsEmpty(vozFalsa.Dicho);
        }

        [Test]
        public void Servicio_Recalculo_DiceLaSiguienteDeLaRutaNueva()
        {
            var reloj = Ahora;
            nav = new ServicioNavegacion(ubicacion, rutas, Esperar, () => reloj);
            vozFalsa = new VozFalsa();
            voz = new ServicioVoz(nav, vozFalsa, prefs);
            Empezar();
            Assert.AreEqual(1, vozFalsa.Dicho.Count);
            // Se va 100 m al este más de 5 s: recálculo
            gps.Mover(M(300, 100));
            reloj = reloj.AddSeconds(6);
            gps.Mover(M(310, 100));
            var nueva = new Ruta(new[] { M(310, 100), M(610, 100), M(610, 300) }, 500, 300, new[]
            {
                new Maniobra(TipoManiobra.Salida, "Diríjase al norte.", M(310, 100), 300),
                new Maniobra(TipoManiobra.Izquierda, "Gire a la izquierda por Calle Bolsa.", M(610, 100), 200, "Gire a la izquierda."),
                new Maniobra(TipoManiobra.Llegada, "Ha llegado a su destino.", M(610, 300), 0),
            });
            valhalla.De(ModoViaje.Coche).Responder(nueva);
            Assert.AreSame(nueva, nav.Ruta);
            Assert.AreEqual("En 300 metros, gire a la izquierda por Calle Bolsa.", vozFalsa.Dicho.Last());
        }

        [Test]
        public void Servicio_FueraDeLaRuta_NoDiceLaManiobraDeLaRutaVieja()
        {
            Empezar();
            Assert.AreEqual(1, vozFalsa.Dicho.Count);
            gps.Mover(M(690, -100)); // a 100 m al oeste de la ruta: 310 m por la vieja
            gps.Mover(M(710, -100)); // cruza los 300 m
            gps.Mover(M(959, -100));
            gps.Mover(M(961, -100)); // cruza los 40 m
            Assert.IsTrue(nav.Estado.FueraDeRuta);
            Assert.AreEqual(1, vozFalsa.Dicho.Count, string.Join(" | ", vozFalsa.Dicho));
        }

        class VozQueFalla : IVoz
        {
            public void Hablar(string texto) => throw new InvalidOperationException("sin motor");
            public void Parar() => throw new InvalidOperationException("sin motor");
        }

        [Test]
        public void Servicio_UnaVozQueFallaNoParaLaNavegacion()
        {
            _ = new ServicioVoz(nav, new VozQueFalla(), prefs);
            Assert.DoesNotThrow(() => Empezar());
            Assert.DoesNotThrow(() => gps.Mover(M(961, 0)));
            Assert.AreEqual(39, nav.Estado.MetrosASiguiente, 0.5, "El estado sigue al día.");
            Assert.DoesNotThrow(() => nav.Terminar());
            Assert.IsFalse(nav.Activa);
        }

        [Test]
        public void Servicio_AlTerminarCallaYNoSigueHablando()
        {
            Empezar();
            int antes = vozFalsa.Paradas;
            nav.Terminar();
            Assert.AreEqual(antes + 1, vozFalsa.Paradas);
            gps.Mover(M(961, 0));
            Assert.AreEqual(1, vozFalsa.Dicho.Count);
        }

        [Test]
        public void Servicio_ModoGuardado()
        {
            Assert.AreEqual(ModoVoz.Todo, voz.Modo, "Sin valor guardado, todo.");
            ModoVoz? avisado = null;
            voz.ModoCambiado += m => avisado = m;
            voz.Elegir(ModoVoz.Importante);
            Assert.AreEqual("importante", prefs.Leer(ServicioVoz.ClavePreferencia));
            Assert.AreEqual(ModoVoz.Importante, avisado);
            Assert.AreEqual(ModoVoz.Importante, new ServicioVoz(nav, new VozFalsa(), prefs).Modo, "Al volver a abrir la app.");
            int paradas = vozFalsa.Paradas;
            voz.Elegir(ModoVoz.Nada);
            Assert.AreEqual(paradas + 1, vozFalsa.Paradas, "Sin voz calla lo que esté diciendo.");
            Assert.AreEqual("nada", prefs.Leer(ServicioVoz.ClavePreferencia));
        }

        // ───────────────────────── Pausa forzada (historia 2.8, parte 2) ─────────────────────────

        [Test]
        public void Regla_Sincronizar_ApuntaSinDecirYLuegoSigue()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var giro = Derecha(M(1000, 0));
            regla.Sincronizar(Estado(giro, 820));
            Assert.IsNull(regla.Avisar(Estado(giro, 810), ModoVoz.Todo), "La de ahora cuenta como dicha a su distancia.");
            Assert.AreEqual("En 300 metros, gire a la derecha por Calle Ancha.", regla.Avisar(Estado(giro, 290), ModoVoz.Todo),
                "Se dijo a 820 m: el intermedio sigue tocando.");
            regla.Sincronizar(Estado(giro, 39));
            Assert.IsNull(regla.Avisar(Estado(giro, 30), ModoVoz.Todo), "El del momento ya pasó: no se dice tarde.");
        }

        [Test]
        public void Regla_Sincronizar_DentroDelIntermedioNoLoDiceDespues()
        {
            var regla = new ReglaVoz(ModoViaje.Coche);
            var giro = Derecha(M(1000, 0));
            regla.Sincronizar(Estado(giro, 250));
            Assert.IsNull(regla.Avisar(Estado(giro, 240), ModoVoz.Todo));
            Assert.AreEqual("Gire a la derecha.", regla.Avisar(Estado(giro, 39), ModoVoz.Todo), "El del momento, a su hora.");
        }

        [Test]
        public void Servicio_Reanudar_UnSegundoSinHablarYDespuesALaHora()
        {
            var reloj = Ahora;
            vozFalsa = new VozFalsa();
            voz = new ServicioVoz(nav, vozFalsa, prefs, () => reloj);
            Empezar();
            Assert.AreEqual(1, vozFalsa.Dicho.Count);
            // Unity vuelve de una pausa real: las posiciones guardadas llegan de golpe
            voz.Reanudar();
            gps.Mover(M(690, 0));
            gps.Mover(M(710, 0)); // cruza los 300 m: aviso viejo
            reloj = reloj.AddSeconds(0.9);
            gps.Mover(M(720, 0));
            Assert.AreEqual(1, vozFalsa.Dicho.Count, "Durante 1 s no habla: " + string.Join(" | ", vozFalsa.Dicho));
            reloj = reloj.AddSeconds(0.2);
            gps.Mover(M(730, 0));
            Assert.AreEqual(1, vozFalsa.Dicho.Count, "El intermedio ya se dio por dicho.");
            gps.Mover(M(961, 0));
            Assert.AreEqual("Gire a la derecha.", vozFalsa.Dicho.Last(), "Pasado el segundo, el del momento a su hora.");
            Assert.AreEqual(2, vozFalsa.Dicho.Count);
        }

        [Test]
        public void Servicio_Reanudar_ElSegundoEmpiezaConLaPrimeraActualizacion()
        {
            var reloj = Ahora;
            vozFalsa = new VozFalsa();
            voz = new ServicioVoz(nav, vozFalsa, prefs, () => reloj);
            Empezar();
            voz.Reanudar();
            reloj = reloj.AddSeconds(2); // pausa larga: el primer fotograma llega tarde
            gps.Mover(M(690, 0));
            gps.Mover(M(710, 0)); // aviso viejo del intermedio
            gps.Mover(M(961, 0)); // y del momento
            Assert.AreEqual(1, vozFalsa.Dicho.Count, "Siguen callados: " + string.Join(" | ", vozFalsa.Dicho));
        }

        [Test]
        public void Servicio_Reanudar_UnaRutaNuevaJustoDespuesDiceSuPrimerAviso()
        {
            var reloj = Ahora;
            vozFalsa = new VozFalsa();
            voz = new ServicioVoz(nav, vozFalsa, prefs, () => reloj);
            voz.Reanudar();
            Empezar();
            CollectionAssert.AreEqual(new[] { "En 800 metros, gire a la derecha por Calle Ancha." }, vozFalsa.Dicho);
        }

        [Test]
        public void Arranque_AlVolverDeLaPausaCallaLoViejo_YAlPausarNo()
        {
            int fotogramas = Application.targetFrameRate;
            try
            {
                var reloj = Ahora;
                vozFalsa = new VozFalsa();
                voz = new ServicioVoz(nav, vozFalsa, prefs, () => reloj);
                Empezar();
                Arranque.Arranque.AlVolverDePausa(voz, null, enPausa: true);
                gps.Mover(M(690, 0));
                gps.Mover(M(710, 0));
                Assert.AreEqual("En 300 metros, gire a la derecha por Calle Ancha.", vozFalsa.Dicho.Last(), "Al pausar no se calla.");
                Arranque.Arranque.AlVolverDePausa(voz, null, enPausa: false);
                gps.Mover(M(961, 0)); // aviso viejo del momento
                Assert.AreEqual(2, vozFalsa.Dicho.Count, "Al volver, callado: " + string.Join(" | ", vozFalsa.Dicho));
            }
            finally { Application.targetFrameRate = fotogramas; }
        }

        [Test]
        public void Servicio_SinReanudar_HablaComoSiempre()
        {
            var reloj = Ahora;
            vozFalsa = new VozFalsa();
            voz = new ServicioVoz(nav, vozFalsa, prefs, () => reloj);
            Empezar();
            gps.Mover(M(690, 0));
            gps.Mover(M(710, 0));
            Assert.AreEqual("En 300 metros, gire a la derecha por Calle Ancha.", vozFalsa.Dicho.Last());
        }

        [Test]
        public void VozNula_NoHaceNada()
        {
            var v = new VozNula();
            Assert.DoesNotThrow(() => { v.Hablar("Gire a la derecha."); v.Parar(); });
        }

        // ───────────────────────── Plugin y manifiesto de Android ─────────────────────────

        [Test]
        public void Plugin_CastellanoNavegacionFocoYSilencio()
        {
            string java = File.ReadAllText("Assets/Plugins/Android/PuenteVoz.java");
            StringAssert.Contains("package com.pricemap.voz;", java);
            StringAssert.Contains("new Locale(\"es\", \"ES\")", java);
            StringAssert.Contains("TextToSpeech.QUEUE_FLUSH", java);
            StringAssert.Contains("USAGE_ASSISTANCE_NAVIGATION_GUIDANCE", java);
            StringAssert.Contains("AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK", java);
            StringAssert.Contains("abandonAudioFocusRequest", java);
            StringAssert.Contains("UtteranceProgressListener", java);
            StringAssert.Contains("getRingerMode() != AudioManager.RINGER_MODE_NORMAL", java, "En silencio o vibración no habla.");
            StringAssert.Contains("public static void hablar(", java);
            StringAssert.Contains("public static void parar(", java);
        }

        const string EspacioAndroid = "http://schemas.android.com/apk/res/android";

        static string CarpetaManifiesto()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "PriceMapVoz_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(carpeta, "src", "main"));
            File.WriteAllText(Path.Combine(carpeta, "src", "main", "AndroidManifest.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns:android=\"" + EspacioAndroid + "\" package=\"com.unity3d.player\">\n" +
                "  <application><activity android:name=\"com.unity3d.player.UnityPlayerActivity\" /></application>\n</manifest>");
            return carpeta;
        }

        [Test]
        public void Manifiesto_DeclaraElMotorDeVozUnaSolaVez()
        {
            string carpeta = CarpetaManifiesto();
            try
            {
                var paso = new MotorVozAndroid();
                paso.OnPostGenerateGradleAndroidProject(carpeta);
                paso.OnPostGenerateGradleAndroidProject(carpeta);
                var xml = new XmlDocument();
                xml.Load(Path.Combine(carpeta, "src", "main", "AndroidManifest.xml"));
                var nombres = new XmlNamespaceManager(xml.NameTable);
                nombres.AddNamespace("android", EspacioAndroid);
                var nodos = xml.DocumentElement.SelectNodes("queries/intent/action[@android:name='android.intent.action.TTS_SERVICE']", nombres);
                Assert.AreEqual(1, nodos.Count);
            }
            finally { Directory.Delete(carpeta, true); }
        }

        // ───────────────────────── Pantalla ─────────────────────────

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        static Lugar Areas => Lugares().Lugares.First(l => l.Id == "clinica-areas");

        VisualElement raiz;

        void CrearPantalla()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, ubicacion, new AccesibilidadFalsa(), rutas: rutas,
                navegacion: nav, voz: voz);
            raiz = arbol.Q("raiz");
            tema.Iniciar();
            filtro.Iniciar();
        }

        void IrAAreas(ModoViaje modo = ModoViaje.Coche)
        {
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.Tocar(modo);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.De(modo).Responder(RutaLarga());
            pantalla.Iniciar();
            Assert.IsTrue(pantalla.Navegando);
        }

        [Test]
        public void Pantalla_ElBotonDeVozSoloSeVeNavegandoEncimaDeUbicacion()
        {
            CrearPantalla();
            Assert.IsNotNull(pantalla.BotonVoz);
            Assert.IsFalse(pantalla.BotonVozVisible);
            Assert.IsFalse(raiz.ClassListContains(PantallaMapa.ClaseConVoz));
            var columna = raiz.Q("columna-controles");
            Assert.AreSame(columna, pantalla.BotonVoz.parent.parent, "En la columna de cristal.");
            Assert.Less(columna.IndexOf(pantalla.BotonVoz.parent), columna.IndexOf(pantalla.BotonUbicacion.parent), "Encima del de ubicación.");
            Assert.IsTrue(pantalla.BotonVoz.EnColumna);

            IrAAreas();
            Assert.IsTrue(pantalla.BotonVozVisible);
            Assert.IsTrue(raiz.ClassListContains(PantallaMapa.ClaseConVoz));
            Assert.AreEqual(Icono.Voz, pantalla.BotonVoz.Icono);
            CollectionAssert.AreEqual(new[] { "En 800 metros, gire a la derecha por Calle Ancha." }, vozFalsa.Dicho, "Ir dice la primera.");

            pantalla.QuitarRuta();
            Assert.IsFalse(pantalla.BotonVozVisible);
            Assert.IsFalse(raiz.ClassListContains(PantallaMapa.ClaseConVoz));
        }

        [Test]
        public void Pantalla_FilaDeTresModosConElElegidoMarcado()
        {
            IrAAreas();
            var fila = pantalla.FilaVoz;
            Assert.IsFalse(fila.Abierta);
            pantalla.BotonVoz.Pulsar();
            Assert.IsTrue(pantalla.FilaVozAbierta);
            Assert.IsTrue(fila.ClassListContains(FilaVoz.ClaseAbierta));
            Assert.AreEqual(3, fila.childCount);
            Assert.AreEqual(Icono.Voz, fila.Opcion(ModoVoz.Todo).Icono);
            Assert.AreEqual(Icono.VozImportante, fila.Opcion(ModoVoz.Importante).Icono);
            Assert.AreEqual(Icono.VozNada, fila.Opcion(ModoVoz.Nada).Icono);
            Assert.IsTrue(fila.Opcion(ModoVoz.Todo).ClassListContains(FilaVoz.ClaseElegida));
            Assert.IsFalse(fila.Opcion(ModoVoz.Nada).ClassListContains(FilaVoz.ClaseElegida));
            Assert.IsTrue(PantallaMapa.EsControl(raiz, fila.Opcion(ModoVoz.Nada)));
            Assert.IsTrue(SombraSuave.Tiene(fila), "Cristal con sombra, como la columna.");
            pantalla.BotonVoz.Pulsar();
            Assert.IsFalse(pantalla.FilaVozAbierta, "Tocar el botón otra vez la cierra.");
        }

        [Test]
        public void Pantalla_ElegirCierraGuardaCambiaElIconoYLoAnuncia()
        {
            IrAAreas();
            pantalla.BotonVoz.Pulsar();
            anuncios.Clear();
            pantalla.FilaVoz.Opcion(ModoVoz.Importante).Pulsar();
            Assert.IsFalse(pantalla.FilaVozAbierta);
            Assert.AreEqual(ModoVoz.Importante, voz.Modo);
            Assert.AreEqual("importante", prefs.Leer(ServicioVoz.ClavePreferencia));
            Assert.AreEqual(Icono.VozImportante, pantalla.BotonVoz.Icono);
            Assert.Contains("Solo las importantes", anuncios);
            pantalla.BotonVoz.Pulsar();
            Assert.IsTrue(pantalla.FilaVoz.Opcion(ModoVoz.Importante).ClassListContains(FilaVoz.ClaseElegida));
            Assert.IsFalse(pantalla.FilaVoz.Opcion(ModoVoz.Todo).ClassListContains(FilaVoz.ClaseElegida));
        }

        [Test]
        public void Pantalla_SinVoz_NoHablaYLaTarjetaSigue()
        {
            IrAAreas();
            pantalla.BotonVoz.Pulsar();
            pantalla.FilaVoz.Opcion(ModoVoz.Nada).Pulsar();
            vozFalsa.Dicho.Clear();
            gps.Mover(M(710, 0));
            gps.Mover(M(961, 0));
            CollectionAssert.IsEmpty(vozFalsa.Dicho);
            Assert.AreEqual(Icono.VozNada, pantalla.BotonVoz.Icono);
            Assert.IsTrue(pantalla.TarjetasRutaVisibles, "El mapa y las tarjetas, igual.");
        }

        [Test]
        public void Pantalla_TocarElMapaOAtrasCierraLaFilaSinQuitarLaRuta()
        {
            IrAAreas();
            pantalla.BotonVoz.Pulsar();
            mapa.Tocar(new PuntoPantalla(10, 10, 0));
            Assert.IsFalse(pantalla.FilaVozAbierta);
            Assert.IsTrue(pantalla.Navegando);
            pantalla.BotonVoz.Pulsar();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FilaVozAbierta);
            Assert.IsTrue(pantalla.Navegando, "Atrás solo cierra la fila.");
            Assert.AreEqual(ModoVoz.Todo, voz.Modo, "Cerrar no cambia el modo.");
        }

        [Test]
        public void Pantalla_TerminarLaRutaConLaFilaAbierta_LaCierraYNoSeTragaAtrasNiElToque()
        {
            IrAAreas();
            pantalla.BotonVoz.Pulsar();
            Assert.IsTrue(pantalla.FilaVozAbierta);
            pantalla.QuitarRuta(); // la X de la tarjeta
            Assert.IsFalse(pantalla.Navegando);
            Assert.IsFalse(pantalla.FilaVozAbierta);
            pantalla.AbrirFicha(Areas);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FichaAbierta, "Atrás cierra la ficha, no una fila oculta.");
            pantalla.AbrirFicha(Areas);
            mapa.Tocar(new PuntoPantalla(10, 10, 0));
            Assert.IsFalse(pantalla.FichaAbierta, "El toque en el mapa cierra la ficha, no una fila oculta.");
        }

        [Test]
        public void Arranque_ComponerPasaLaVozALaPantalla()
        {
            var conInterfaz = new GameObject("Pantalla con interfaz");
            try
            {
                conInterfaz.SetActive(false);
                var documento = conInterfaz.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                var conUi = conInterfaz.AddComponent<PantallaMapa>();
                conInterfaz.SetActive(true);
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), conUi, new ServicioTema(prefs, new SistemaClaro()), Lugares(),
                    new ServicioFiltro(prefs), Ahora, ubicacion, rutas: rutas, navegacion: nav, voz: voz));
                Assert.AreSame(voz, conUi.Voz);
                Assert.IsNotNull(conUi.BotonVoz);
                Assert.IsNotNull(conUi.FilaVoz);
            }
            finally { UnityEngine.Object.DestroyImmediate(conInterfaz); }
        }

        [Test]
        public void Pantalla_TalkBack_BotonConSuModoYFilaConSusNombres()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            voz.Elegir(ModoVoz.Importante);
            IrAAreas();
            var nodo = pantalla.Accesibilidad.NodoDe(pantalla.BotonVoz);
            Assert.IsNotNull(nodo);
            Assert.AreEqual("Indicaciones por voz", nodo.label);
            Assert.AreEqual("solo las importantes", nodo.value);
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.IsFalse(recorrido.Contains("Sin voz"), "Con la fila cerrada, sus iconos no están.");
            pantalla.BotonVoz.Pulsar();
            var conEstado = pantalla.Accesibilidad.RecorridoConEstado();
            var etiquetas = conEstado.Select(e => e.etiqueta).ToList();
            int i = etiquetas.IndexOf("Indicaciones por voz");
            Assert.GreaterOrEqual(i, 0, string.Join(" | ", etiquetas));
            CollectionAssert.AreEqual(new[] { "Todas las indicaciones", "Solo las importantes", "Sin voz" }, etiquetas.Skip(i + 1).Take(3).ToList());
            Assert.IsTrue(conEstado[i + 2].seleccionada, "El elegido, seleccionado.");
            Assert.IsFalse(conEstado[i + 1].seleccionada);
            pantalla.FilaVoz.Opcion(ModoVoz.Nada).Pulsar();
            Assert.AreEqual("sin voz", pantalla.Accesibilidad.NodoDe(pantalla.BotonVoz).value);
        }

        [Test]
        public void Pantalla_SinServicioDeVoz_NoHayBoton()
        {
            voz = null;
            CrearPantalla();
            Assert.IsNull(pantalla.BotonVoz);
            Assert.IsNull(pantalla.FilaVoz);
            Assert.IsFalse(pantalla.Atras());
        }

        [Test]
        public void Textos_DeLaVozEnEspanol()
        {
            Assert.AreEqual("Indicaciones por voz", T(ClavesTexto.VozBoton));
            Assert.AreEqual("Todas las indicaciones", T(ClavesTexto.VozTodo));
            Assert.AreEqual("Solo las importantes", T(ClavesTexto.VozImportante));
            Assert.AreEqual("Sin voz", T(ClavesTexto.VozNada));
            foreach (var clave in new[] { ClavesTexto.VozBoton, ClavesTexto.VozTodo, ClavesTexto.VozImportante, ClavesTexto.VozNada,
                         ClavesTexto.VozValorTodo, ClavesTexto.VozValorImportante, ClavesTexto.VozValorNada })
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
        }
    }
}
