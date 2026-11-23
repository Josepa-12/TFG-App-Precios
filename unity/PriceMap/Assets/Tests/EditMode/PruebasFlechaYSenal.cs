using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
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
    /// Flecha en coche y "Buscando señal GPS" (historia 2.7, parte 2c): las lecturas del GPS en el adaptador (también las
    /// que repiten la coordenada), la señal en ServicioNavegacion con reloj falso (10 s, parado, vuelve, sin recálculo) y la
    /// pantalla real con un mapa falso, una prueba por fila de la matriz. Nada sale a la red.
    /// </summary>
    public class PruebasFlechaYSenal
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
        const float Escala = 0.5f;

        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        static readonly Coordenada Destino = new Coordenada(36.78060, -6.34945);

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        const double MetrosPorGrado = Coordenada.RadioTierraMetros * Math.PI / 180.0;

        /// <summary>Punto a esos metros al norte y al este del Cabildo.</summary>
        static Coordenada M(double norte, double este) =>
            new Coordenada(Cabildo.Latitud + norte / MetrosPorGrado,
                Cabildo.Longitud + este / (MetrosPorGrado * Math.Cos(Cabildo.Latitud * Math.PI / 180.0)));

        /// <summary>450 m al norte y 150 m al este, 6 min, con la salida, el giro a la derecha y la llegada.</summary>
        static Ruta RutaEle() =>
            new Ruta(new[] { M(0, 0), M(450, 0), M(450, 150) }, 600, 360, new[]
            {
                new Maniobra(TipoManiobra.Salida, "Diríjase al norte por Calle Ancha.", M(0, 0), 450),
                new Maniobra(TipoManiobra.Derecha, "Gire a la derecha hacia Calle Regina.", M(450, 0), 150),
                new Maniobra(TipoManiobra.Llegada, "Ha llegado a su destino.", M(450, 150), 0),
            });

        // ───────────────────────── Adaptador del GPS: lecturas ─────────────────────────

        class FuenteFalsa : IFuenteUbicacion
        {
            public bool TienePermiso { get; set; } = true;
            public bool UbicacionEncendida { get; set; } = true;
            public LocationServiceStatus EstadoServicio { get; set; } = LocationServiceStatus.Stopped;
            public (Coordenada posicion, double marca) UltimaLectura { get; set; }
            public float? Rumbo { get; set; }
            public void PedirPermiso(Action<bool> r) => r(true);
            public void Encender() => EstadoServicio = LocationServiceStatus.Initializing;
            public void Apagar() => EstadoServicio = LocationServiceStatus.Stopped;
            public void AbrirAjustes(string accion) { }

            public void Leer(Coordenada c, double marca)
            {
                EstadoServicio = LocationServiceStatus.Running;
                UltimaLectura = (c, marca);
            }
        }

        class ServicioRutaFalso : IServicioRuta
        {
            public bool NecesitaPermisoNotificaciones => false;
            public bool EnMarcha { get; private set; }
            readonly List<string> cola = new List<string>();
            public void PedirPermisoNotificaciones(Action<bool> r) => r(true);
            public void Iniciar(string texto) => EnMarcha = true;
            public void ActualizarTexto(string texto) { }
            public void Parar() { EnMarcha = false; cola.Clear(); }

            public string VaciarCola()
            {
                var texto = string.Join("\n", cola);
                cola.Clear();
                return texto;
            }

            public void Posicion(Coordenada c, long horaMs) =>
                cola.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:R};{1:R};90.0;1.40;5.0;{2}", c.Latitud, c.Longitud, horaMs));
        }

        [Test]
        public void Gps_CadaLecturaNuevaCuentaAunqueRepitaLaCoordenada()
        {
            var fuente = new FuenteFalsa();
            var gps = new UbicacionGps(fuente);
            gps.Iniciar();
            Assert.AreEqual(0, gps.Lecturas);
            fuente.Leer(Cabildo, 1.0);
            gps.Actualizar();
            Assert.AreEqual(1, gps.Lecturas);
            int avisos = 0;
            gps.Cambiado += () => avisos++;
            fuente.Leer(Cabildo, 2.0); // parado: misma coordenada, lectura nueva
            gps.Actualizar();
            Assert.AreEqual(2, gps.Lecturas);
            Assert.AreEqual(0, avisos, "Sin nada distinto no avisa por Cambiado.");
            gps.Actualizar(); // la misma lectura otra vez: no cuenta
            Assert.AreEqual(2, gps.Lecturas);
        }

        [Test]
        public void Gps_LasLecturasDelServicioDeRutaCuentanUnaAUna()
        {
            var fuente = new FuenteFalsa();
            var servicio = new ServicioRutaFalso();
            var gps = new UbicacionGps(fuente, servicio);
            gps.Iniciar();
            fuente.Leer(Cabildo, 1.0);
            gps.Actualizar();
            Assert.IsTrue(gps.IniciarRuta("Navegando").Ok);
            int antes = gps.Lecturas;
            servicio.Posicion(Cabildo, 1000);
            servicio.Posicion(Cabildo, 2000); // la misma coordenada dos veces
            gps.Actualizar();
            Assert.AreEqual(antes + 2, gps.Lecturas);
        }

        /// <summary>
        /// UbicacionUnity (la de la app) declara cada miembro de IUbicacion, también los que tienen cuerpo por defecto: si
        /// no, en la app valdría el de la interfaz (p. ej. Lecturas = 0 y un coche parado saldría "sin señal").
        /// </summary>
        [Test]
        public void UbicacionUnity_DeclaraTodosLosMiembrosDeIUbicacion()
        {
            const System.Reflection.BindingFlags Publicos = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var clase = typeof(UbicacionUnity);
            var interfaz = typeof(IUbicacion);
            foreach (var p in interfaz.GetProperties())
                Assert.IsNotNull(clase.GetProperty(p.Name, Publicos), $"Falta la propiedad {p.Name}.");
            foreach (var e in interfaz.GetEvents())
                Assert.IsNotNull(clase.GetEvent(e.Name, Publicos), $"Falta el evento {e.Name}.");
            foreach (var m in interfaz.GetMethods().Where(m => !m.IsSpecialName))
                Assert.IsNotNull(clase.GetMethod(m.Name, Publicos, null, m.GetParameters().Select(x => x.ParameterType).ToArray(), null),
                    $"Falta el método {m.Name}.");
            Assert.IsNotNull(clase.GetProperty(nameof(IUbicacion.Lecturas), Publicos));
        }

        [Test]
        public void ElUmbralEstaEnParametrosNegocio()
        {
            Assert.AreEqual(10.0, ParametrosNegocio.NavegacionSinSenalSegundos);
            Assert.AreEqual(TimeSpan.FromSeconds(10), ServicioNavegacion.TiempoSinSenal);
        }

        // ───────────────────────── Falsos ─────────────────────────

        class Peticion
        {
            public ModoViaje Modo;
            public TaskCompletionSource<Resultado<Ruta>> Tcs;
            public void Responder(Ruta ruta) => Tcs.TrySetResult(Resultado<Ruta>.Exito(ruta));
            public void Fallar() => Tcs.TrySetResult(Resultado<Ruta>.Fallo("sin conexión"));
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

            public int Pendientes(TimeSpan duracion) => Lista.Count(e => e.duracion == duracion && !e.tcs.Task.IsCompleted);

            public void Vencer(TimeSpan duracion)
            {
                foreach (var (d, tcs) in Lista.ToList()) if (d == duracion) tcs.TrySetResult(true);
            }
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

        /// <summary>GPS falso con lecturas: Mover da una posición nueva; Repetir, una lectura de la misma (parado).</summary>
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
            public void Repetir() => Lecturas++;
        }

        /// <summary>Mapa falso: todo cae en el centro de la pantalla (el marcador se ve) y el giro se fija.</summary>
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
            public float giro;
            public float Giro => giro;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
            public void Seguir(Coordenada posicion, float giro, float inclinacion, float distancia, PuntoPantalla enPantalla, bool animado) { }
            public void DejarDeSeguir() { }
            public event Action GestoUsuario { add { } remove { } }
        }

        SynchronizationContext contextoAntes;
        RutasFalsas valhalla;
        Esperas esperas;
        DateTime reloj;
        UbicacionFalsa gps;
        ServicioUbicacion ubicacion;
        ServicioRutas rutas;
        ServicioNavegacion nav;
        readonly List<string> anuncios = new List<string>();
        int senales;

        static readonly TimeSpan Reintento = TimeSpan.FromSeconds(10);

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones corren al responder
            Textos.Forzados = T;
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            reloj = Ahora;
            valhalla = new RutasFalsas();
            esperas = new Esperas();
            gps = new UbicacionFalsa { Posicion = M(0, 0) };
            ubicacion = new ServicioUbicacion(gps, new PreferenciasFalsas());
            rutas = new ServicioRutas(valhalla, null, esperas.Esperar, () => reloj);
            nav = new ServicioNavegacion(ubicacion, rutas, esperas.Esperar, () => reloj);
            nav.SenalCambiada += () => senales++;
            anuncios.Clear();
            senales = 0;
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

        void Empezar() => nav.Empezar(RutaEle(), Destino, "Clínica Areas", ModoViaje.Coche);

        /// <summary>Se mueve a esa posición pasados esos segundos desde la anterior; después, un fotograma (Comprobar).</summary>
        void Ir(Coordenada c, double segundos = 1)
        {
            reloj = reloj.AddSeconds(segundos);
            gps.Mover(c);
            nav.Comprobar();
        }

        /// <summary>Pasan esos segundos sin lecturas, comprobando la señal cada segundo como mucho (la app delante).</summary>
        void Esperar(double segundos)
        {
            do
            {
                double paso = Math.Min(1.0, segundos);
                reloj = reloj.AddSeconds(paso);
                segundos -= paso;
                nav.Comprobar();
            } while (segundos > 0);
        }

        // ───────────────────────── ServicioNavegacion: la señal ─────────────────────────

        [Test]
        public void Servicio_12sSinLecturas_SinSenalYUnSoloAviso()
        {
            Empezar();
            Esperar(10);
            Assert.IsFalse(nav.SinSenal, "A los 10 s justos aún no.");
            Esperar(2);
            Assert.IsTrue(nav.SinSenal);
            Assert.AreEqual(1, senales);
            Esperar(5);
            Assert.AreEqual(1, senales, "Una vez.");
        }

        [Test]
        public void Servicio_Parado60sConLecturasDeLaMismaCoordenada_SigueConSenal()
        {
            Empezar();
            for (int i = 0; i < 60; i++)
            {
                gps.Repetir();
                Esperar(1);
            }
            Assert.IsFalse(nav.SinSenal);
            Assert.AreEqual(0, senales);
        }

        [Test]
        public void Servicio_Vuelve_ConUnaPosicionNuevaOUnaLecturaRepetida()
        {
            Empezar();
            Esperar(12);
            Ir(M(10, 0), 0);
            Assert.IsFalse(nav.SinSenal, "Con una posición nueva.");
            Assert.AreEqual(2, senales);

            Esperar(12);
            Assert.IsTrue(nav.SinSenal);
            gps.Repetir();
            Esperar(0);
            Assert.IsFalse(nav.SinSenal, "Con una lectura de la misma coordenada.");
            Assert.AreEqual(4, senales);
        }

        [Test]
        public void Servicio_SinSenal_NoRecalculaYElDesvioCuentaDesdeCero()
        {
            Empezar();
            Ir(M(150, 45)); // desviado
            Esperar(12);
            Assert.IsTrue(nav.SinSenal);
            Assert.IsEmpty(valhalla.Peticiones, "Sin señal no se recalcula.");
            Ir(M(150, 46), 0); // vuelve, aún desviado: 13 s desde que se desvió
            Assert.IsFalse(nav.SinSenal);
            Assert.IsEmpty(valhalla.Peticiones, "El desvío cuenta desde la vuelta de la señal.");
            Ir(M(150, 47), 4);
            Assert.IsEmpty(valhalla.Peticiones);
            Ir(M(150, 48), 2); // 6 s desviado con señal
            Assert.AreEqual(1, valhalla.Peticiones.Count);
        }

        [Test]
        public void Servicio_VueltaDeSegundoPlano_ElTiempoEnPausaNoCuenta()
        {
            Empezar();
            reloj = reloj.AddSeconds(60); // la app en segundo plano: nadie llama a Comprobar
            nav.Comprobar();
            Assert.IsFalse(nav.SinSenal, "El tiempo en pausa no es tiempo sin señal.");
            Esperar(10);
            Assert.IsFalse(nav.SinSenal);
            Esperar(1);
            Assert.IsTrue(nav.SinSenal, "11 s sin lecturas con la app delante.");
        }

        [Test]
        public void Servicio_SePierdeLaSenalMientrasSeRecalcula_LaRespuestaSeDescarta()
        {
            var eventos = new List<string>();
            nav.RutaCambiada += () => eventos.Add("cambiada");
            nav.RecalculoFallido += () => eventos.Add("fallo");
            Empezar();
            var vieja = nav.Ruta;
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            Assert.AreEqual(1, valhalla.Peticiones.Count);
            Esperar(11);
            Assert.IsTrue(nav.SinSenal);
            valhalla.Peticiones[0].Responder(new Ruta(new[] { M(150, 46), M(300, 300) }, 300, 200, null));
            Assert.AreSame(vieja, nav.Ruta, "Sin señal no se cambia la ruta.");
            Assert.IsEmpty(eventos);
            Assert.IsFalse(nav.Recalculando);
        }

        [Test]
        public void Servicio_SePierdeLaSenalMientrasSeRecalcula_ElFalloNoAvisaNiReintenta()
        {
            var eventos = new List<string>();
            nav.RecalculoFallido += () => eventos.Add("fallo");
            Empezar();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            Esperar(11);
            Assert.IsTrue(nav.SinSenal);
            valhalla.Peticiones[0].Fallar();
            Assert.IsEmpty(eventos, "Sin toast de «No se ha podido recalcular» en el túnel.");
            Assert.AreEqual(0, esperas.Pendientes(Reintento));
            Assert.IsFalse(nav.Recalculando);
        }

        [Test]
        public void Servicio_ReintentoTrasPerderYRecuperarLaSenal_ElDesvioCuentaDesdeCero()
        {
            Empezar();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones[0].Fallar();
            Assert.AreEqual(1, esperas.Pendientes(Reintento));
            Esperar(11);
            Assert.IsTrue(nav.SinSenal);
            Ir(M(150, 47), 0); // vuelve la señal, aún desviado
            Assert.IsFalse(nav.SinSenal);
            esperas.Vencer(Reintento);
            Assert.AreEqual(1, valhalla.Peticiones.Count, "El reintento no recalcula enseguida.");
            Assert.IsFalse(nav.Recalculando);
            Ir(M(150, 48), 4);
            Assert.AreEqual(1, valhalla.Peticiones.Count);
            Ir(M(150, 49), 2); // 6 s desviado desde que volvió la señal
            Assert.AreEqual(2, valhalla.Peticiones.Count);
        }

        [Test]
        public void Servicio_SinSenalEsperandoElReintento_NoReintenta()
        {
            Empezar();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones[0].Fallar();
            Assert.AreEqual(1, esperas.Pendientes(Reintento));
            Esperar(11);
            Assert.IsTrue(nav.SinSenal);
            esperas.Vencer(Reintento);
            Assert.AreEqual(1, valhalla.Peticiones.Count, "Sin señal no se pide otra vez.");
            Assert.IsFalse(nav.Recalculando);
        }

        [Test]
        public void Servicio_Terminar_QuitaLaSenalPerdidaSinAvisar()
        {
            Empezar();
            Esperar(12);
            Assert.IsTrue(nav.SinSenal);
            nav.Terminar();
            Assert.IsFalse(nav.SinSenal);
            Assert.AreEqual(1, senales, "Al terminar no se anuncia «Señal GPS recuperada».");
            Esperar(30);
            Assert.IsFalse(nav.SinSenal, "Sin ruta no se comprueba.");
        }

        [Test]
        public void Servicio_SinRuta_NuncaSinSenal()
        {
            Esperar(60);
            Assert.IsFalse(nav.SinSenal);
            Assert.AreEqual(0, senales);
        }

        // ───────────────────────── Pantalla (matriz) ─────────────────────────

        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        static Lugar Areas => Lugares().Lugares.First(l => l.Id == "clinica-areas");

        MarcadorPosicion Marcador => pantalla.MarcadorPosicion;

        void CrearPantalla()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, ubicacion, new AccesibilidadFalsa(), rutas: rutas, navegacion: nav);
            pantalla.RelojCamara = () => reloj;
            tema.Iniciar();
            filtro.Iniciar();
        }

        /// <summary>Ficha de Clínica Areas, Ir en ese modo y la ruta llega.</summary>
        void IrAAreas(ModoViaje modo = ModoViaje.Coche)
        {
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.Tocar(modo);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.De(modo).Responder(RutaEle());
            pantalla.Iniciar();
            Assert.IsTrue(pantalla.Navegando);
        }

        /// <summary>Pasan esos segundos sin lecturas, con un fotograma cada segundo como mucho (la app delante).</summary>
        void Fotograma(double segundos = 0)
        {
            do
            {
                double paso = Math.Min(1.0, segundos);
                reloj = reloj.AddSeconds(paso);
                segundos -= paso;
                pantalla.ActualizarCapas(Escala);
            } while (segundos > 0);
        }

        [TestCase(ModoViaje.Coche)]
        [TestCase(ModoViaje.APie)]
        public void Pantalla_ConLaCamaraDeCamino_YaConElAspectoDeNavegar(ModoViaje modo)
        {
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.Tocar(modo);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.De(modo).Responder(RutaEle());
            Fotograma();
            Assert.IsFalse(Marcador.FlechaVisible, "Eligiendo, el círculo.");
            pantalla.IniciarRuta(0);
            Fotograma();
            Assert.IsTrue(pantalla.AcercandoCamara);
            Assert.AreEqual(modo == ModoViaje.Coche, Marcador.FlechaVisible);
        }

        [Test]
        public void Pantalla_Coche_FlechaSinConoHaciaLaMarchaYArribaConLaCamaraSiguiendo()
        {
            gps.Rumbo = 200f; // brújula: en coche no cuenta
            IrAAreas();
            Fotograma();
            Assert.IsTrue(Marcador.Visible);
            Assert.IsTrue(Marcador.FlechaVisible);
            Assert.IsFalse(Marcador.ConoVisible, "Sin cono.");
            Assert.AreEqual(DisplayStyle.None, Marcador.Punto.style.display.value, "Sin círculo.");
            Assert.AreEqual(DisplayStyle.Flex, Marcador.Flecha.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, Marcador.Disco.style.display.value, "Sobre el disco blanco.");
            Assert.AreEqual(0f, Marcador.AnguloFlecha, 0.5f, "Sin rumbo de la marcha aún, el de la ruta (al norte).");

            Ir(M(0, 50), 10); // fuera de ruta, hacia el este a 5 m/s: cuenta la posición real
            Ir(M(0, 55));
            Fotograma();
            Assert.AreEqual(90f, Marcador.AnguloFlecha, 0.5f, "Hacia el este con el mapa al norte.");
            Assert.AreEqual(90f, Marcador.Flecha.style.rotate.value.angle.ToDegrees(), 0.5f, "La flecha gira.");
            Assert.AreEqual(StyleKeyword.Null, Marcador.Disco.style.rotate.keyword, "El disco no gira.");
            Assert.AreEqual(0f, Marcador.Disco.resolvedStyle.rotate.angle.ToDegrees(), 1e-4f, "El disco sigue a 0°.");
            mapa.giro = 90f; // la cámara que sigue pone el rumbo arriba
            Fotograma();
            Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(Marcador.AnguloFlecha, 0f)), 0.5f, "Hacia arriba.");

            pantalla.QuitarRuta(); // sin ruta, vuelve el círculo y se va el disco
            Fotograma();
            Assert.IsTrue(Marcador.Visible);
            Assert.IsFalse(Marcador.FlechaVisible);
            Assert.AreEqual(DisplayStyle.None, Marcador.Disco.style.display.value, "Sin flecha, sin disco.");
            Assert.AreEqual(DisplayStyle.None, Marcador.Flecha.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, Marcador.Punto.style.display.value, "Vuelve el círculo.");
        }

        [Test]
        public void Pantalla_APie_CirculoConCono()
        {
            gps.Rumbo = 200f;
            IrAAreas(ModoViaje.APie);
            Fotograma();
            Assert.IsTrue(Marcador.Visible);
            Assert.IsFalse(Marcador.FlechaVisible);
            Assert.IsTrue(Marcador.ConoVisible);
            Assert.AreEqual(200f, Marcador.AnguloCono, 0.01f);
            Assert.AreNotEqual(DisplayStyle.None, Marcador.Punto.style.display.value);
        }

        [Test]
        public void Pantalla_APieSinSenal12s_CirculoConConoAtenuadoYPastilla()
        {
            gps.Rumbo = 200f;
            IrAAreas(ModoViaje.APie);
            Fotograma(12);
            Assert.IsTrue(Marcador.Visible);
            Assert.IsFalse(Marcador.FlechaVisible);
            Assert.IsTrue(Marcador.ConoVisible, "Círculo con cono.");
            Assert.AreNotEqual(DisplayStyle.None, Marcador.Punto.style.display.value);
            Assert.IsTrue(Marcador.Atenuado);
            Assert.AreEqual(0.4f, Marcador.style.opacity.value, 1e-4f);
            Assert.AreEqual(StyleKeyword.Null, Marcador.Disco.style.opacity.keyword);
            Assert.IsTrue(pantalla.PastillaSenal.Visible);
            Assert.AreEqual("Buscando señal GPS", pantalla.PastillaSenal.Texto.text);
        }

        [Test]
        public void Pantalla_SinRuta_CirculoConCono()
        {
            gps.Rumbo = 45f;
            CrearPantalla();
            Fotograma();
            Assert.IsTrue(Marcador.Visible);
            Assert.IsFalse(Marcador.FlechaVisible);
            Assert.IsTrue(Marcador.ConoVisible);
            Assert.IsFalse(Marcador.Atenuado);
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
        }

        [Test]
        public void Pantalla_SinSenal12s_AtenuadaPastillaTalkBackYSinRecalculo()
        {
            IrAAreas();
            Ir(M(150, 45)); // desviado
            Fotograma();
            var posicion = Marcador.Posicion;
            int peticiones = valhalla.Peticiones.Count;
            anuncios.Clear();
            Fotograma(12);
            Assert.IsTrue(Marcador.Atenuado);
            Assert.AreEqual(0.7f, Marcador.Disco.style.opacity.value, 1e-4f, "El disco, atenuado.");
            Assert.IsTrue(Marcador.ClassListContains(MarcadorPosicion.ClaseSinSenal));
            Assert.AreEqual(StyleKeyword.Null, Marcador.style.opacity.keyword, "La flecha se ve entera.");
            Assert.AreEqual(StyleKeyword.Null, Marcador.Flecha.style.opacity.keyword);
            Assert.AreEqual(0.4f, MarcadorPosicion.OpacidadSinSenal, 1e-4f);
            Assert.IsTrue(Marcador.FlechaVisible, "La flecha se queda.");
            Assert.AreEqual(posicion, Marcador.Posicion, "En su último sitio.");
            var pastilla = pantalla.PastillaSenal;
            Assert.IsTrue(pastilla.Visible);
            Assert.AreEqual("Buscando señal GPS", pastilla.Texto.text);
            Assert.IsTrue(pastilla.Texto.ClassListContains("t-caption"), "Texto pequeño.");
            Assert.IsTrue(pastilla.Texto.ClassListContains("pastilla-senal__texto"));
            Assert.Greater(pastilla.style.top.value.value, PantallaMapa.AltoSupuestoTarjetaManiobraDp, "Bajo la tarjeta de arriba.");
            CollectionAssert.AreEqual(new[] { "Buscando señal GPS" }, anuncios);
            Assert.AreEqual(peticiones, valhalla.Peticiones.Count, "Sin recálculo.");
            Fotograma(5);
            Assert.AreEqual(1, anuncios.Count, "Una vez.");
        }

        [Test]
        public void SinSenal_LaAtenuacionPasaDelDiscoAlMarcadorYVuelve()
        {
            var m = new MarcadorPosicion();
            var mapaMarcador = new MapaFalso();
            void Poner(bool flecha, bool atenuado) =>
                m.Actualizar(mapaMarcador, Cabildo, 0f, 1f, 4000f, 4000f, flecha, 0f, atenuado);
            void Comprobar(bool marcador, bool disco, string cuando)
            {
                Assert.IsTrue(m.Visible, cuando);
                Assert.AreEqual(marcador, m.style.opacity.keyword == StyleKeyword.Undefined, cuando + ": marcador");
                Assert.AreEqual(disco, m.Disco.style.opacity.keyword == StyleKeyword.Undefined, cuando + ": disco");
                Assert.AreEqual(disco, m.ClassListContains(MarcadorPosicion.ClaseSinSenal), cuando + ": clase");
            }
            Poner(true, true); Comprobar(false, true, "flecha sin señal");
            Poner(false, true); Comprobar(true, false, "círculo sin señal");
            Poner(true, true); Comprobar(false, true, "otra vez la flecha");
            Poner(true, false); Comprobar(false, false, "con señal");
            Poner(false, false); Comprobar(false, false, "círculo con señal");
        }

        [Test]
        public void Pantalla_Parado60sConLecturas_SinPastilla()
        {
            IrAAreas();
            for (int i = 0; i < 60; i++)
            {
                gps.Repetir();
                Fotograma(1);
            }
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
            Assert.IsFalse(Marcador.Atenuado);
        }

        [Test]
        public void Pantalla_Vuelve_NormalYSeAnunciaUnaVez()
        {
            IrAAreas();
            Fotograma(12);
            Assert.IsTrue(pantalla.PastillaSenal.Visible);
            anuncios.Clear();
            gps.Repetir(); // lectura nueva en el mismo sitio (sin cambiar de maniobra)
            Fotograma();
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
            Assert.IsFalse(Marcador.Atenuado);
            Assert.AreEqual(StyleKeyword.Null, Marcador.style.opacity.keyword);
            Assert.AreEqual(StyleKeyword.Null, Marcador.Disco.style.opacity.keyword);
            Assert.IsFalse(Marcador.ClassListContains(MarcadorPosicion.ClaseSinSenal));
            CollectionAssert.AreEqual(new[] { "Señal GPS recuperada" }, anuncios);
            Fotograma();
            Assert.AreEqual(1, anuncios.Count);
        }

        [Test]
        public void Pantalla_ConLaFichaAbierta_NoSeVeLaPastilla()
        {
            IrAAreas();
            Fotograma(12);
            Assert.IsTrue(pantalla.PastillaSenal.Visible);
            pantalla.ReabrirFichaRuta();
            Fotograma();
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
            pantalla.CerrarFicha();
            Fotograma();
            Assert.IsTrue(pantalla.PastillaSenal.Visible, "Al cerrarla, vuelve si aún no hay señal.");
        }

        [Test]
        public void Pantalla_TerminarConLaPastilla_CirculoNormalSinPastilla()
        {
            gps.Rumbo = 30f;
            IrAAreas();
            Fotograma(12);
            Assert.IsTrue(pantalla.PastillaSenal.Visible);
            anuncios.Clear();
            pantalla.QuitarRuta(); // la X "Terminar ruta"
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
            Fotograma();
            Assert.IsFalse(nav.Activa);
            Assert.IsFalse(Marcador.FlechaVisible);
            Assert.IsTrue(Marcador.ConoVisible, "Círculo y cono de siempre.");
            Assert.IsFalse(Marcador.Atenuado);
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
            CollectionAssert.DoesNotContain(anuncios, "Señal GPS recuperada");
        }

        [Test]
        public void Pantalla_ElToastVaBajoLaPastilla()
        {
            IrAAreas();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones.Last().Fallar(); // toast "No se ha podido recalcular la ruta"
            Assert.IsTrue(pantalla.Toast.Visible);
            Fotograma(12);
            Assert.IsTrue(pantalla.PastillaSenal.Visible);
            Assert.IsTrue(pantalla.Toast.Visible);
            Assert.GreaterOrEqual(pantalla.Toast.style.top.value.value, pantalla.PastillaSenal.Abajo, "El toast no tapa la pastilla.");
            gps.Repetir(); // vuelve la señal con el toast aún a la vista
            Fotograma();
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
            Assert.AreEqual(16f + PantallaMapa.AltoSupuestoTarjetaManiobraDp + 12f, pantalla.Toast.style.top.value.value, 1e-3f,
                "Sin la pastilla, el toast vuelve bajo la tarjeta de arriba.");
        }

        [Test]
        public void LosTextosEstanEnLaTabla()
        {
            Assert.AreEqual("Buscando señal GPS", PrepararInterfaz.TextosEs[ClavesTexto.SenalBuscando]);
            Assert.AreEqual("Señal GPS recuperada", PrepararInterfaz.TextosEs[ClavesTexto.SenalRecuperada]);
            CollectionAssert.Contains(ClavesTexto.Todas, ClavesTexto.SenalBuscando);
            CollectionAssert.Contains(ClavesTexto.Todas, ClavesTexto.SenalRecuperada);
        }
    }
}
