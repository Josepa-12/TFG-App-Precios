using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
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
    /// Llegar y terminar la ruta (historia 2.9): la regla de los 30 m, ServicioNavegacion y ServicioVoz con ubicación,
    /// rutas, voz y reloj falsos, y la pantalla real; una prueba por fila de la matriz. Nada sale a la red.
    /// </summary>
    public class PruebasLlegada
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
        const float Escala = 0.5f;

        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];
        static string T(string clave, params object[] a) => Textos.Formatear(T(clave), a);

        const double MetrosPorGrado = Coordenada.RadioTierraMetros * Math.PI / 180.0;

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        static Lugar Areas => Lugares().Lugares.First(l => l.Id == "clinica-areas");
        static Lugar Otro => Lugares().Lugares.First(l => l.Id != "clinica-areas");

        /// <summary>Punto a esos metros al norte y al este del destino (Clínica Areas).</summary>
        static Coordenada D(double norte, double este)
        {
            var d = Areas.Posicion;
            return new Coordenada(d.Latitud + norte / MetrosPorGrado,
                d.Longitud + este / (MetrosPorGrado * Math.Cos(d.Latitud * Math.PI / 180.0)));
        }

        /// <summary>De 200 m al sur y 150 al oeste, al norte y al este hasta el destino (o hasta esos metros antes), 6 min.</summary>
        static Ruta RutaHasta(double antes = 0) => new Ruta(new[] { D(-200, -150), D(0, -150), D(0, -antes) }, 350 - antes, 360, new[]
        {
            new Maniobra(TipoManiobra.Salida, "Diríjase al norte por Calle Ancha.", D(-200, -150), 200, "Diríjase al norte."),
            new Maniobra(TipoManiobra.Derecha, "Gire a la derecha hacia Calle Regina.", D(0, -150), 150 - antes, "Gire a la derecha."),
            new Maniobra(TipoManiobra.Llegada, "Ha llegado a su destino.", D(0, -antes), 0, "Ha llegado."),
        });

        static readonly Func<Coordenada> Origen = () => D(-200, -150);

        // ───────────────────────── Falsos ─────────────────────────

        class Peticion
        {
            public ModoViaje Modo;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<Ruta>> Tcs;
            public void Responder(Ruta ruta) => Tcs.TrySetResult(Resultado<Ruta>.Exito(ruta));
            public void Fallar() => Tcs.TrySetResult(Resultado<Ruta>.Fallo("sin conexión"));
        }

        class RutasFalsas : IRutas
        {
            public readonly List<Peticion> Peticiones = new List<Peticion>();
            /// <summary>Sin avisar de la cancelación: como un servicio que responde tarde.</summary>
            public bool SordoALaCancelacion;

            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                var p = new Peticion { Modo = modo, Ct = ct, Tcs = new TaskCompletionSource<Resultado<Ruta>>() };
                Peticiones.Add(p);
                if (!SordoALaCancelacion) ct.Register(() => p.Tcs.TrySetCanceled());
                return p.Tcs.Task;
            }

            public Peticion De(ModoViaje modo) => Peticiones.Last(p => p.Modo == modo);
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public int Guardadas;
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) { datos[clave] = valor; Guardadas++; }
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class AccesibilidadFalsa : IAccesibilidadSistema { public bool AnimacionesQuitadas { get; set; } }

        class VozFalsa : IVoz
        {
            public readonly List<string> Dicho = new List<string>();
            public int Paradas;
            public void Hablar(string texto) => Dicho.Add(texto);
            public void Parar() => Paradas++;
        }

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.Activa;
            public Coordenada? Posicion { get; set; }
            public float? Rumbo => null;
            public event Action Cambiado;
            public int Lecturas { get; set; }
            public void PedirPermiso() { }
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
            public bool RutaActiva { get; private set; }
            public bool RutaSoloPrimerPlano { get; set; }
            public readonly List<string> Notificaciones = new List<string>(); // la de empezar y sus cambios
            public int Terminadas;
            public Resultado<bool> IniciarRuta(string texto) { Notificaciones.Add(texto); RutaActiva = true; return Resultado<bool>.Exito(true); }
            public void ActualizarNotificacion(string texto) => Notificaciones.Add(texto);
            public void TerminarRuta() { Terminadas++; RutaActiva = false; }
            public void Mover(Coordenada c) { Posicion = c; Lecturas++; Cambiado?.Invoke(); }
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
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public IReadOnlyList<Coordenada> linea;
            public int dibujadas;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { linea = puntos; dibujadas++; }
            public void QuitarRuta() => linea = null;
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
            public readonly List<(Coordenada posicion, float giro)> seguidos = new List<(Coordenada, float)>();
            public int dejados;
            public void Seguir(Coordenada posicion, float giro, float inclinacion, float distancia, PuntoPantalla enPantalla, bool animado) =>
                seguidos.Add((posicion, giro));
            public void DejarDeSeguir() => dejados++;
        }

        SynchronizationContext contextoAntes;
        RutasFalsas servicio;
        DateTime reloj;
        UbicacionFalsa gps;
        PreferenciasFalsas prefs;
        ServicioUbicacion ubicacion;
        ServicioRutas rutas;
        ServicioNavegacion nav;
        VozFalsa vozFalsa;
        ServicioVoz voz;
        readonly List<string> eventos = new List<string>();
        readonly List<string> anuncios = new List<string>();

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones corren al responder
            Textos.Forzados = T;
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            servicio = new RutasFalsas();
            reloj = Ahora;
            gps = new UbicacionFalsa { Posicion = Origen() };
            prefs = new PreferenciasFalsas();
            ubicacion = new ServicioUbicacion(gps, prefs);
            rutas = new ServicioRutas(servicio, null, (t, ct) => new TaskCompletionSource<bool>().Task, () => reloj);
            nav = new ServicioNavegacion(ubicacion, rutas, (t, ct) => new TaskCompletionSource<bool>().Task, () => reloj,
                (destino, minutos) => T(ClavesTexto.NotificacionNavegando, destino, minutos), destino => T(ClavesTexto.LlegadaA, destino));
            vozFalsa = new VozFalsa();
            voz = new ServicioVoz(nav, vozFalsa, prefs, () => reloj, destino => T(ClavesTexto.LlegadaA, destino));
            nav.RutaIniciada += d => eventos.Add("iniciada " + d);
            nav.HaLlegado += d => eventos.Add("llegado " + d);
            nav.RutaTerminada += m => eventos.Add("terminada " + m);
            nav.RutaCambiada += () => eventos.Add("cambiada");
            nav.RecalculoFallido += () => eventos.Add("fallo");
            eventos.Clear();
            anuncios.Clear();
        }

        void Anotar(string texto) => anuncios.Add(texto);

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = null;
            accesibilidad = null;
            AccesibilidadPantalla.AnuncioPedido -= Anotar;
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        const string Nombre = "Clínica Areas";
        const string Dicho = "Has llegado a Clínica Areas";
        const string Anuncio = "Has llegado a Clínica Areas. La ruta se cerrará en un minuto.";

        void Empezar(Ruta ruta = null, ModoViaje modo = ModoViaje.Coche) => nav.Empezar(ruta ?? RutaHasta(), Areas.Posicion, Nombre, modo);

        /// <summary>Se mueve a esa posición pasados esos segundos.</summary>
        void Ir(Coordenada c, double segundos = 1)
        {
            reloj = reloj.AddSeconds(segundos);
            gps.Mover(c);
        }

        int Veces(string texto) => vozFalsa.Dicho.Count(d => d == texto);

        // ───────────────────────── Regla ─────────────────────────

        [Test]
        public void Regla_A30MetrosDelDestinoODelFinalDeLaLinea()
        {
            var regla = new ReglaNavegacion(RutaHasta());
            Assert.IsTrue(regla.HaLlegado(D(0, -25), Areas.Posicion));
            Assert.IsTrue(regla.HaLlegado(D(0, -29.9), Areas.Posicion));
            Assert.IsFalse(regla.HaLlegado(D(0, -30.2), Areas.Posicion));
            Assert.IsFalse(regla.HaLlegado(D(0, -35), Areas.Posicion));
            var apartado = new ReglaNavegacion(RutaHasta(antes: 80));
            Assert.IsTrue(apartado.HaLlegado(D(0, -100), Areas.Posicion), "A 20 m del final de la línea.");
            Assert.IsTrue(apartado.HaLlegado(D(10, 0), Areas.Posicion), "Y en el destino mismo.");
            Assert.IsFalse(apartado.HaLlegado(D(0, -40), Areas.Posicion), "A 40 m de los dos.");
            var vacia = new ReglaNavegacion(new Ruta(null, 0, 0, null));
            Assert.IsFalse(vacia.HaLlegado(D(0, -40), Areas.Posicion));
            Assert.IsTrue(vacia.HaLlegado(D(0, -10), Areas.Posicion));
        }

        [Test]
        public void Parametros_EnParametrosNegocio()
        {
            Assert.AreEqual(30.0, ParametrosNegocio.LlegadaMetros);
            Assert.AreEqual(60.0, ParametrosNegocio.LlegadaCierreSegundos);
            Assert.AreEqual(TimeSpan.FromMinutes(1), ServicioNavegacion.TiempoCierre);
        }

        // ───────────────────────── Servicio (matriz) ─────────────────────────

        [Test]
        public void Servicio_Llegar_UnaVezConEventoVozYNotificacion()
        {
            Empezar();
            Ir(D(0, -100));
            Ir(D(0, -25));
            Assert.IsTrue(nav.Llegado);
            Assert.IsTrue(nav.Activa, "La ruta sigue abierta hasta finalizarla o el minuto.");
            Ir(D(0, -10));
            Ir(D(0, -5));
            Assert.AreEqual(1, eventos.Count(e => e == "llegado " + Nombre), string.Join(" | ", eventos));
            Assert.AreEqual(1, Veces(Dicho), string.Join(" | ", vozFalsa.Dicho));
            Assert.AreEqual(Dicho, vozFalsa.Dicho.Last(), "Después no se dice ninguna maniobra.");
            Assert.AreEqual(Dicho, gps.Notificaciones.Last());
            Assert.AreEqual(0, gps.Terminadas);
        }

        [Test]
        public void Servicio_Casi_SigueNavegando()
        {
            Empezar();
            Ir(D(0, -100));
            Ir(D(0, -35));
            Assert.IsFalse(nav.Llegado);
            Assert.IsFalse(eventos.Any(e => e.StartsWith("llegado")));
            Assert.IsFalse(gps.Notificaciones.Contains(Dicho));
        }

        [Test]
        public void Servicio_DestinoApartado_LlegaEnElFinalDeLaLinea()
        {
            Empezar(RutaHasta(antes: 80));
            Ir(D(0, -115)); // a 35 m del final
            Assert.IsFalse(nav.Llegado);
            Ir(D(0, -100));
            Assert.IsTrue(nav.Llegado);
        }

        [Test]
        public void Servicio_DestinoApartado_ElDestinoFueraDeLaRutaTambienLlegaSinRecalcular()
        {
            Empezar(RutaHasta(antes: 80));
            Ir(D(0, -35)); // a 45 m de la línea y a 35 del destino: desviado
            Assert.IsTrue(nav.Estado.FueraDeRuta);
            Ir(D(0, -25), 10);
            Assert.IsTrue(nav.Llegado);
            Assert.IsEmpty(servicio.Peticiones, "Llegar va antes que recalcular.");
        }

        [Test]
        public void Servicio_YaAlli_LlegaAlEmpezar()
        {
            gps.Posicion = D(0, -10);
            Empezar();
            Assert.IsTrue(nav.Llegado);
            CollectionAssert.AreEqual(new[] { "iniciada " + Nombre, "llegado " + Nombre }, eventos);
            CollectionAssert.AreEqual(new[] { Dicho }, vozFalsa.Dicho, "Sin la primera maniobra.");
            Assert.AreEqual(Dicho, gps.Notificaciones.Last());
        }

        [Test]
        public void Servicio_Alejarse_SigueLlegadoSinRecalculo()
        {
            Empezar();
            Ir(D(0, -25));
            var estado = nav.Estado;
            int notificaciones = gps.Notificaciones.Count;
            int avisos = 0;
            nav.Actualizado += () => avisos++;
            Ir(D(60, 0));
            Ir(D(120, 0), 10);
            Ir(D(200, 0), 10);
            Assert.IsTrue(nav.Llegado);
            Assert.AreEqual(3, avisos, "La cámara se entera de cada posición.");
            Assert.AreSame(estado, nav.Estado, "El estado se queda como al llegar.");
            Assert.IsFalse(nav.Recalculando);
            Assert.IsEmpty(servicio.Peticiones);
            Assert.AreEqual(notificaciones, gps.Notificaciones.Count, "La notificación sigue en «Has llegado…».");
            Assert.AreEqual(1, Veces(Dicho));
        }

        [Test]
        public void Servicio_Finalizar_MotivoLlegada()
        {
            Empezar();
            Ir(D(0, -25));
            nav.Finalizar();
            Assert.IsFalse(nav.Activa);
            Assert.IsFalse(nav.Llegado);
            Assert.AreEqual("terminada Llegada", eventos.Last());
            Assert.AreEqual(1, gps.Terminadas);
            Assert.IsFalse(gps.RutaActiva, "Fuera el GPS de fondo y su notificación.");
        }

        [Test]
        public void Servicio_FinalizarAntesDeLlegar_Manual()
        {
            Empezar();
            Ir(D(0, -100));
            nav.Finalizar();
            Assert.AreEqual("terminada Manual", eventos.Last());
        }

        [Test]
        public void Servicio_OtraRutaTrasLlegar_Manual()
        {
            Empezar();
            Ir(D(0, -25));
            gps.Posicion = Origen();
            Empezar();
            CollectionAssert.AreEqual(new[] { "iniciada " + Nombre, "llegado " + Nombre, "terminada Manual", "iniciada " + Nombre }, eventos);
            Assert.IsFalse(nav.Llegado, "La nueva empieza sin llegar.");
            Ir(D(0, -25));
            Assert.AreEqual(2, eventos.Count(e => e.StartsWith("llegado")), "Una vez por ruta.");
        }

        [Test]
        public void Servicio_Automatico_A60SegundosDeLlegar()
        {
            Empezar();
            Ir(D(0, -25));
            reloj = reloj.AddSeconds(59.9);
            nav.Comprobar();
            Assert.IsTrue(nav.Activa);
            reloj = reloj.AddSeconds(0.1);
            nav.Comprobar();
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual("terminada Automatico", eventos.Last());
            Assert.AreEqual(1, gps.Terminadas);
            nav.Comprobar();
            Assert.AreEqual(1, eventos.Count(e => e.StartsWith("terminada")));
        }

        [Test]
        public void Servicio_Automatico_NoDependeDeLaSenalNiDeMoverse()
        {
            Empezar();
            Ir(D(0, -25));
            int cambios = 0;
            nav.SenalCambiada += () => cambios++;
            for (int i = 0; i < 59; i++) { reloj = reloj.AddSeconds(1); nav.Comprobar(); }
            Assert.IsTrue(nav.Activa);
            Assert.IsFalse(nav.SinSenal, "Ya en el destino la señal no importa.");
            Assert.AreEqual(0, cambios);
            Ir(D(0, -24)); // al segundo 60, una posición
            nav.Comprobar();
            Assert.IsFalse(nav.Activa);
        }

        [Test]
        public void Servicio_Automatico_ElRelojSaltaAtras_CuentaDesdeAhi()
        {
            Empezar();
            Ir(D(0, -25));
            reloj = reloj.AddHours(-1);
            nav.Comprobar();
            reloj = reloj.AddSeconds(60);
            nav.Comprobar();
            Assert.IsFalse(nav.Activa);
        }

        [Test]
        public void Servicio_RecalculoEnCamino_SeCancela([Values(false, true)] bool respondeTarde)
        {
            servicio.SordoALaCancelacion = respondeTarde;
            Empezar();
            Ir(D(60, -60));
            Ir(D(62, -60), 6);
            var p = servicio.Peticiones.Single();
            Assert.IsTrue(nav.Recalculando);
            var ruta = nav.Ruta;
            Ir(D(25, 0));
            Assert.IsTrue(nav.Llegado);
            Assert.IsFalse(nav.Recalculando);
            Assert.IsTrue(p.Ct.IsCancellationRequested, "Se cancela.");
            p.Responder(RutaHasta(antes: 80)); // llega tarde (o ya estaba cancelada)
            Assert.AreSame(ruta, nav.Ruta, "Su ruta no se usa.");
            CollectionAssert.DoesNotContain(eventos, "cambiada");
            CollectionAssert.DoesNotContain(eventos, "fallo");
        }

        [Test]
        public void Servicio_ElRecalculoAcabaDondeYaEsta_Llega()
        {
            Empezar();
            Ir(D(60, -60));
            Ir(D(62, -60), 6);
            servicio.Peticiones.Single().Responder(new Ruta(new[] { D(70, -60), D(64, -60) }, 6, 5, null));
            Assert.IsTrue(nav.Llegado, "A 2 m del final de la línea nueva.");
            CollectionAssert.AreEqual(new[] { "iniciada " + Nombre, "cambiada", "llegado " + Nombre }, eventos);
        }

        [Test]
        public void Servicio_ElRecalculoDeLaCacheAcabaDondeYaEsta_LlegaYNoVuelveANavegando()
        {
            var t = rutas.Calcular(D(62, -60), Areas.Posicion, ModoViaje.Coche, CancellationToken.None);
            servicio.Peticiones.Single().Responder(new Ruta(new[] { D(70, -60), D(64, -60) }, 6, 5, null));
            Assert.IsTrue(t.IsCompleted);
            Empezar();
            Ir(D(60, -60));
            int avisos = 0, tiempos = 0;
            nav.Actualizado += () => avisos++;
            nav.TiempoRestanteCambiado += _ => tiempos++;
            Ir(D(62, -60), 6); // el recálculo sale de la caché, al momento
            Assert.AreEqual(1, servicio.Peticiones.Count);
            Assert.IsTrue(nav.Llegado);
            Assert.AreEqual(Dicho, gps.Notificaciones.Last());
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(0, tiempos);
        }

        [Test]
        public void Servicio_SinSenal_NoSeLlega()
        {
            Empezar();
            Ir(D(0, -35));
            for (int i = 0; i < 70; i++) { reloj = reloj.AddSeconds(1); nav.Comprobar(); }
            Assert.IsTrue(nav.SinSenal);
            Assert.IsFalse(nav.Llegado);
            Assert.IsTrue(nav.Activa);
            Ir(D(0, -25));
            Assert.IsTrue(nav.Llegado, "Con la primera posición del GPS, sí.");
            Assert.IsFalse(nav.SinSenal);
        }

        [Test]
        public void Servicio_SinPosicionAlEmpezar_NoSeLlega()
        {
            gps.Posicion = null;
            gps.Estado = EstadoUbicacion.Buscando;
            Empezar(new Ruta(new[] { D(0, -20), D(0, 0) }, 20, 15, null)); // el principio de la línea ya está a 20 m
            Assert.IsFalse(nav.Llegado);
        }

        [Test]
        public void Servicio_TerminarDesdeElAvisoDeLlegada_NoAvisaDespues()
        {
            nav.HaLlegado += _ => nav.Finalizar();
            Empezar();
            int avisos = 0;
            nav.Actualizado += () => avisos++;
            Ir(D(0, -25));
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual(0, avisos);
            Assert.AreEqual("terminada Llegada", eventos.Last());
        }

        // ───────────────────────── Voz ─────────────────────────

        [TestCase(ModoVoz.Todo, 1)]
        [TestCase(ModoVoz.Importante, 1)]
        [TestCase(ModoVoz.Nada, 0)]
        public void Voz_LaLlegadaSegunElModo(ModoVoz modo, int veces)
        {
            voz.Elegir(modo);
            Empezar();
            Ir(D(0, -25));
            Ir(D(0, -10));
            Assert.AreEqual(veces, Veces(Dicho));
            if (modo == ModoVoz.Nada) Assert.IsEmpty(vozFalsa.Dicho);
        }

        [Test]
        public void Voz_AlVolverDeUnaPausaTambienDiceLaLlegada()
        {
            Empezar();
            vozFalsa.Dicho.Clear();
            voz.Reanudar();
            Ir(D(0, -100)); // posiciones guardadas de golpe
            gps.Mover(D(0, -60));
            gps.Mover(D(0, -25));
            CollectionAssert.AreEqual(new[] { Dicho }, vozFalsa.Dicho);
        }

        [Test]
        public void Voz_ElTextoDeLaLlegadaFalla_LosDemasSeEnteranIgual()
        {
            _ = new ServicioVoz(nav, new VozFalsa(), prefs, () => reloj, _ => throw new InvalidOperationException("sin texto"));
            int despues = 0;
            nav.HaLlegado += _ => despues++;
            Empezar();
            Ir(D(0, -25));
            Assert.AreEqual(1, despues);
        }

        /// <summary>Pasa a 10 m del destino y gira allí mismo hacia el sur: al llegar aún queda ese giro.</summary>
        static Ruta RutaQueGiraAlli() => new Ruta(new[] { D(0, -200), D(0, -10), D(-100, -10), D(-100, 60) }, 360, 300, new[]
        {
            new Maniobra(TipoManiobra.Salida, "Diríjase al este por Calle Ancha.", D(0, -200), 190, "Diríjase al este."),
            new Maniobra(TipoManiobra.Derecha, "Gire a la derecha hacia Calle Regina.", D(0, -10), 100, "Gire a la derecha."),
            new Maniobra(TipoManiobra.Izquierda, "Gire a la izquierda hacia Calle Mar.", D(-100, -10), 70, "Gire a la izquierda."),
            new Maniobra(TipoManiobra.Llegada, "Ha llegado a su destino.", D(-100, 60), 0, "Ha llegado."),
        });

        [Test]
        public void Voz_LlegarConUnGiroPendiente_LaLlegadaEsLoUltimoQueDice([Values(false, true)] bool yaAlli)
        {
            gps.Posicion = yaAlli ? D(0, -28) : D(0, -200);
            Empezar(RutaQueGiraAlli());
            if (!yaAlli)
            {
                Ir(D(0, -100));
                Assert.IsNotEmpty(vozFalsa.Dicho);
                Ir(D(0, -28)); // a 18 m del giro
            }
            Assert.IsTrue(nav.Llegado);
            Ir(D(0, -27));
            Ir(D(0, -12));
            Assert.AreEqual(Dicho, vozFalsa.Dicho.Last(), string.Join(" | ", vozFalsa.Dicho));
            Assert.AreEqual(1, Veces(Dicho));
            CollectionAssert.DoesNotContain(vozFalsa.Dicho, "Gire a la derecha.");
            if (yaAlli) Assert.AreEqual(1, vozFalsa.Dicho.Count);
        }

        [Test]
        public void Voz_TerminarCalla()
        {
            Empezar();
            Ir(D(0, -25));
            int paradas = vozFalsa.Paradas;
            nav.Finalizar();
            Assert.AreEqual(paradas + 1, vozFalsa.Paradas);
        }

        // ───────────────────────── Pantalla (matriz) ─────────────────────────

        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        VisualElement raiz;
        AccesibilidadFalsa accesibilidad;

        TarjetaLlegada Arriba => pantalla.TarjetaLlegada;
        HojaLlegada Hoja => pantalla.HojaLlegada;
        static bool Visible(VisualElement e) => e.style.display.value == DisplayStyle.Flex;

        void CrearPantalla()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            accesibilidad = accesibilidad ?? new AccesibilidadFalsa();
            pantalla.RelojCamara = () => reloj;
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, ubicacion, accesibilidad, rutas: rutas,
                navegacion: nav, voz: voz);
            raiz = arbol.Q("raiz");
            tema.Iniciar();
            filtro.Iniciar();
        }

        /// <summary>Ficha de Clínica Areas, Ir, la ruta llega e Iniciar.</summary>
        void IrAAreas(Ruta ruta = null, ModoViaje modo = ModoViaje.Coche)
        {
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.Tocar(modo);
            pantalla.Ficha.Acciones.PulsarIr();
            servicio.De(modo).Responder(ruta ?? RutaHasta());
            pantalla.Iniciar();
            pantalla.RelojCamara = () => reloj;
        }

        void Llegar()
        {
            IrAAreas();
            Ir(D(0, -100), 20);
            anuncios.Clear();
            Ir(D(0, -25), 10);
        }

        void Fotograma(double segundos = 0)
        {
            reloj = reloj.AddSeconds(segundos);
            pantalla.ActualizarCapas(Escala);
        }

        void ComprobarMapaNormal()
        {
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual(1, gps.Terminadas);
            Assert.IsFalse(gps.RutaActiva, "Fuera el GPS de fondo y su notificación.");
            Assert.IsNull(mapa.linea);
            Assert.IsFalse(pantalla.LlegadaVisible);
            Assert.IsFalse(Visible(Arriba));
            Assert.IsFalse(Visible(Hoja));
            Assert.IsFalse(Visible(pantalla.TarjetaManiobra));
            Assert.IsFalse(Visible(pantalla.TarjetaRuta));
            Assert.IsFalse(raiz.ClassListContains(PantallaMapa.ClaseConRuta));
            Assert.AreEqual(EstadoSeguimiento.Apagado, pantalla.EstadoCamara);
        }

        [Test]
        public void Pantalla_Llegar_TarjetaHojaYSinLineaNiVozNiTarjetasDeNavegar()
        {
            Llegar();
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.IsTrue(Visible(Arriba));
            Assert.AreEqual("Has llegado", Arriba.Titulo.text);
            Assert.AreEqual(Nombre, Arriba.Destino.text);
            Assert.IsTrue(Visible(Hoja));
            Assert.AreEqual("Finalizar ruta", Hoja.TextoFinalizar.text);
            Assert.AreEqual("La ruta se cerrará en 1 minuto", Hoja.Aviso.text);
            Assert.IsFalse(pantalla.TarjetasRutaVisibles);
            Assert.IsFalse(Visible(pantalla.TarjetaManiobra));
            Assert.IsFalse(Visible(pantalla.TarjetaRuta));
            Assert.IsNull(mapa.linea, "La línea se quita.");
            Assert.IsFalse(pantalla.BotonVozVisible);
            Assert.IsFalse(raiz.ClassListContains(PantallaMapa.ClaseConVoz));
            Assert.IsTrue(raiz.ClassListContains(PantallaMapa.ClaseConRuta), "La píldora y el tema siguen fuera.");
            Assert.IsFalse(pantalla.Buscador.Visible);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio), string.Join(" | ", anuncios));
            Assert.AreEqual(1, Veces(Dicho));
            Ir(D(0, -10));
            Fotograma(1);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio), "Una sola vez.");
            Assert.IsNull(mapa.linea);
        }

        [Test]
        public void Pantalla_Llegar_TalkBackEnFinalizarRutaYElRecorrido()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Llegar();
            Assert.AreSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
            Assert.AreSame(pantalla.Accesibilidad.NodoDe(Hoja.Finalizar), pantalla.Accesibilidad.UltimoNodoEnfocado);
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.AreEqual(Dicho, recorrido[0], string.Join(" | ", recorrido));
            int i = recorrido.IndexOf("Finalizar ruta");
            Assert.Greater(i, 0, string.Join(" | ", recorrido));
            Assert.AreEqual("La ruta se cerrará en 1 minuto", recorrido[i + 1]);
            Assert.IsFalse(recorrido.Contains(pantalla.TarjetaRuta.Anuncio));
            Assert.IsFalse(recorrido.Contains("Indicaciones por voz"));
        }

        [Test]
        public void Pantalla_Casi_SigueNavegando()
        {
            IrAAreas();
            Ir(D(0, -100), 20);
            Ir(D(0, -35), 5);
            Assert.IsFalse(pantalla.HaLlegado);
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            Assert.IsFalse(Visible(Arriba));
            Assert.IsNotNull(mapa.linea);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pantalla_YaAlli_HasLlegadoAlEmpezar(bool sinAnimaciones)
        {
            gps.Posicion = D(0, -10);
            accesibilidad = new AccesibilidadFalsa { AnimacionesQuitadas = sinAnimaciones };
            IrAAreas();
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.IsTrue(Visible(Arriba));
            Assert.IsTrue(Visible(Hoja));
            Assert.IsNull(mapa.linea);
            Assert.AreSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio), string.Join(" | ", anuncios));
            Assert.IsFalse(pantalla.BotonVozVisible);
            Assert.IsFalse(pantalla.Toast.Visible);
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Pantalla_YaAlliConRutaAproximada_SinToast(bool sinAnimaciones)
        {
            gps.Posicion = D(0, -10);
            accesibilidad = new AccesibilidadFalsa { AnimacionesQuitadas = sinAnimaciones };
            IrAAreas(new Ruta(new[] { D(-200, -150), D(0, -150), D(0, 0) }, 350, 360, null, aproximada: true));
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.IsTrue(pantalla.RutaDibujada.Aproximada);
            Assert.IsFalse(pantalla.Toast.Visible);
        }

        [Test]
        public void Pantalla_LlegarConElToastDelRecalculoFallido_SeOculta()
        {
            IrAAreas();
            Ir(D(60, -60), 30);
            Ir(D(62, -60), 6);
            servicio.Peticiones.Last().Fallar();
            Assert.IsTrue(pantalla.Toast.Visible);
            Ir(D(25, 0), 1);
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.IsFalse(pantalla.Toast.Visible);
        }

        [Test]
        public void Pantalla_YaAlliConUnaVentanaDelante_ElFocoYElAnuncioAlCerrarla()
        {
            gps.Posicion = D(0, -10);
            gps.RutaSoloPrimerPlano = true;
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.PulsarIr();
            servicio.De(ModoViaje.Coche).Responder(RutaHasta());
            pantalla.IniciarRuta(0);
            Fotograma();
            Assert.IsTrue(pantalla.AvisoAbierto, "Sale con la cámara aún de camino.");
            Assert.IsTrue(pantalla.AcercandoCamara);
            Fotograma(1);
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.AreNotSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
            CollectionAssert.DoesNotContain(anuncios, Anuncio);
            pantalla.CerrarAviso();
            Assert.AreSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio));
        }

        [Test]
        public void Pantalla_YaAlliConLaFichaAbiertaPorElCamino_LoAnunciaYSaleAlCerrarla()
        {
            gps.Posicion = D(0, -10);
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.PulsarIr();
            servicio.De(ModoViaje.Coche).Responder(RutaHasta());
            pantalla.IniciarRuta(0);
            pantalla.AbrirFicha(Otro);
            Fotograma(1);
            Assert.IsTrue(pantalla.HaLlegado);
            Assert.IsFalse(pantalla.LlegadaVisible);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio));
            pantalla.CerrarFicha();
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio));
        }

        [Test]
        public void Pantalla_Alejarse_SigueHasLlegadoYLaCamaraSigueALaPosicionReal()
        {
            Llegar();
            int dibujadas = mapa.dibujadas, peticiones = servicio.Peticiones.Count;
            Ir(D(60, 0), 10);
            Ir(D(120, 0), 10);
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.AreEqual(D(120, 0), pantalla.PosicionMostrada);
            Assert.AreEqual(D(120, 0), mapa.seguidos.Last().posicion);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, mapa.seguidos.Last().giro), 1f, "Con el rumbo de la marcha, al norte.");
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.AreEqual(dibujadas, mapa.dibujadas, "La línea no vuelve.");
            Assert.AreEqual(peticiones, servicio.Peticiones.Count, "Sin recálculo.");
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio));
        }

        [Test]
        public void Pantalla_Llegar_ElPasoDeLaLineaALaPosicionRealNoGiraLaCamara()
        {
            IrAAreas();
            Ir(D(0, -100), 20); // sobre la línea, al este
            Ir(D(0, -40), 5);
            Assert.AreEqual(90f, mapa.seguidos.Last().giro, 1f);
            Ir(D(-24, -15), 1); // 24 m a la derecha de la línea: ha llegado
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.AreEqual(D(-24, -15), mapa.seguidos.Last().posicion);
            Assert.AreEqual(90f, mapa.seguidos.Last().giro, 1f, "El salto de la línea al sitio real no es un rumbo.");
        }

        [Test]
        public void Pantalla_LlegarDesviado_SigueElRumboDeLaMarcha()
        {
            IrAAreas();
            Ir(D(0, -120), 20);
            Ir(D(70, 0), 30); // fuera de la ruta: se ve la posición real
            Assert.IsTrue(nav.Estado.FueraDeRuta);
            Ir(D(45, 0), 2); // hacia el sur
            Assert.AreEqual(180f, Mathf.Repeat(pantalla.RumboCamara.Value, 360f), 1f);
            Ir(D(28, 0), 2);
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.AreEqual(180f, Mathf.Repeat(mapa.seguidos.Last().giro, 360f), 1f, "No gira hacia el tramo de la ruta.");
            Assert.AreEqual(180f, Mathf.Repeat(pantalla.RumboCamara.Value, 360f), 1f);
        }

        [Test]
        public void Pantalla_Boton_TerminaConMotivoLlegadaYMapaNormal()
        {
            Llegar();
            Hoja.PulsarFinalizar();
            ComprobarMapaNormal();
            Assert.AreEqual("terminada Llegada", eventos.Last());
            Assert.AreEqual(1, eventos.Count(e => e.StartsWith("terminada")));
            Assert.IsTrue(pantalla.Buscador.Visible);
            Assert.AreSame(pantalla.Buscador, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void Pantalla_TalkBack_ElNodoDeFinalizarRutaTerminaConMotivoLlegada()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Llegar();
            var nodo = pantalla.Accesibilidad.NodoDe(Hoja.Finalizar);
            Assert.AreEqual("Finalizar ruta", nodo.label);
            // El doble toque de TalkBack (como en PruebasFicha): el evento no se puede lanzar desde fuera
            var campo = typeof(AccessibilityNode).GetField("invoked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(campo);
            Assert.IsTrue(((Func<bool>)campo.GetValue(nodo))());
            ComprobarMapaNormal();
            Assert.AreEqual("terminada Llegada", eventos.Last());
        }

        [Test]
        public void Pantalla_AlDestruirseTrasLlegar_MotivoLlegadaYLaHojaYaNoEscucha()
        {
            Llegar();
            typeof(PantallaMapa).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(pantalla, null);
            Assert.AreEqual("terminada Llegada", eventos.Last());
            Assert.IsTrue(pantalla.RutaActiva);
            Hoja.PulsarFinalizar();
            Assert.IsTrue(pantalla.RutaActiva, "Destruida, el botón no hace nada.");
        }

        [Test]
        public void Pantalla_Automatico_AlMinutoMapaNormal()
        {
            Llegar();
            Fotograma(59);
            Assert.IsTrue(pantalla.LlegadaVisible);
            Fotograma(1);
            ComprobarMapaNormal();
            Assert.AreEqual("terminada Automatico", eventos.Last());
            Assert.AreEqual(1, eventos.Count(e => e.StartsWith("terminada")));
            Assert.IsTrue(pantalla.Buscador.Visible);
            Fotograma(60);
            Assert.AreEqual(1, gps.Terminadas);
        }

        [Test]
        public void Pantalla_AntesDeLlegar_FinalizarRutaManual()
        {
            IrAAreas();
            pantalla.TarjetaRuta.Desplegar();
            pantalla.TarjetaRuta.PulsarFinalizar();
            ComprobarMapaNormal();
            Assert.AreEqual("terminada Manual", eventos.Last());
        }

        [Test]
        public void Pantalla_AtrasEnRuta_NoTerminaYDesplegadaSoloLaPliega()
        {
            IrAAreas();
            Assert.IsTrue(pantalla.Atras(), "Plegada: se gasta sin hacer nada.");
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            pantalla.TarjetaRuta.Desplegar();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.HojaRutaDesplegada);
            Assert.IsTrue(pantalla.Navegando);
            Assert.AreEqual(0, gps.Terminadas);
            // Con la fila de voz, con una ficha y con «Cómo llegar» encima, solo los cierra
            pantalla.BotonVoz.Pulsar();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FilaVozAbierta);
            pantalla.AbrirFicha(Otro);
            pantalla.Ficha.Acciones.PulsarIr();
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.EligiendoRuta);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            Assert.AreEqual(0, gps.Terminadas);
        }

        [Test]
        public void Pantalla_AtrasEnHasLlegado_NoTermina()
        {
            Llegar();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.AreEqual(0, gps.Terminadas);
        }

        [Test]
        public void Pantalla_LlegarConLaHojaDesplegadaYLaFilaDeVoz_SeCierran()
        {
            IrAAreas();
            pantalla.TarjetaRuta.Desplegar();
            pantalla.BotonVoz.Pulsar();
            Ir(D(0, -25), 30);
            Assert.IsFalse(pantalla.HojaRutaDesplegada);
            Assert.IsFalse(pantalla.FilaVozAbierta);
            Assert.AreSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void Pantalla_FichaEncima_SigueYAlCerrarlaHasLlegado()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            IrAAreas();
            pantalla.AbrirFicha(Otro);
            anuncios.Clear();
            Ir(D(0, -25), 30);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreSame(Otro, pantalla.Ficha.Lugar);
            Assert.IsTrue(pantalla.HaLlegado);
            Assert.IsFalse(Visible(Arriba));
            Assert.IsFalse(Visible(Hoja));
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio), "Se anuncia ya.");
            Assert.AreSame(pantalla.Accesibilidad.NodoDe(pantalla.Ficha.Titulo), pantalla.Accesibilidad.UltimoNodoEnfocado, "Sin mover el foco de la ficha.");
            Assert.AreEqual(1, Veces(Dicho));
            pantalla.CerrarFicha();
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.IsTrue(Visible(Arriba));
            Assert.AreSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio));
            Assert.IsNull(mapa.linea);
        }

        [Test]
        public void Pantalla_FichaEncima_PasadoElMinutoElMapaNormal()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            IrAAreas();
            pantalla.AbrirFicha(Otro);
            Ir(D(0, -25), 30);
            Fotograma(60);
            Assert.AreEqual("terminada Automatico", eventos.Last());
            Assert.IsTrue(pantalla.FichaAbierta, "La ficha sigue.");
            Assert.IsFalse(pantalla.Buscador.Visible);
            Assert.AreSame(pantalla.Accesibilidad.NodoDe(pantalla.Ficha.Titulo), pantalla.Accesibilidad.UltimoNodoEnfocado);
            pantalla.CerrarFicha();
            ComprobarMapaNormal();
            Assert.IsTrue(pantalla.Buscador.Visible);
        }

        [Test]
        public void Pantalla_ComoLlegarEncima_SigueConSusLineasYAlCerrarlaHasLlegado()
        {
            IrAAreas();
            pantalla.AbrirFicha(Otro);
            pantalla.Ficha.Acciones.PulsarIr();
            servicio.De(ModoViaje.Coche).Responder(new Ruta(new[] { Origen(), Otro.Posicion }, 500, 300, null));
            Assert.IsNotNull(mapa.linea);
            Ir(D(0, -25), 30);
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.AreEqual(Otro.Posicion, mapa.linea.Last(), "La línea es la de la elegida.");
            pantalla.CerrarComoLlegar();
            Assert.IsNull(mapa.linea, "La de la ruta terminada no vuelve.");
            Assert.IsTrue(pantalla.FichaAbierta);
            pantalla.CerrarFicha();
            Assert.IsTrue(pantalla.LlegadaVisible);
        }

        [Test]
        public void Pantalla_ComoLlegarEncima_PasadoElMinutoSigueYAlCerrarElMapaNormal()
        {
            IrAAreas();
            pantalla.AbrirFicha(Otro);
            pantalla.Ficha.Acciones.PulsarIr();
            servicio.De(ModoViaje.Coche).Responder(new Ruta(new[] { Origen(), Otro.Posicion }, 500, 300, null));
            Ir(D(0, -25), 30);
            Fotograma(60);
            Assert.IsFalse(nav.Activa);
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.AreEqual(Otro.Posicion, mapa.linea.Last());
            pantalla.CerrarComoLlegar();
            pantalla.CerrarFicha();
            ComprobarMapaNormal();
            Assert.IsTrue(pantalla.Buscador.Visible);
        }

        [Test]
        public void Pantalla_IniciarOtraRutaTrasLlegar_TerminaLaQueHabiaManual()
        {
            Llegar();
            pantalla.AbrirFicha(Otro);
            pantalla.Ficha.Acciones.PulsarIr();
            servicio.De(ModoViaje.Coche).Responder(new Ruta(new[] { D(0, -25), D(300, 0), Otro.Posicion }, 2000, 600, null));
            Assert.IsTrue(pantalla.HaLlegado, "Elegir no la termina.");
            pantalla.IniciarRuta(0);
            Assert.AreEqual("terminada Manual", eventos.Last());
            Assert.IsFalse(pantalla.HaLlegado);
            Fotograma(1);
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            Assert.IsTrue(Visible(pantalla.TarjetaManiobra));
            Assert.IsFalse(Visible(Arriba));
            Assert.IsFalse(Visible(Hoja));
            Assert.IsNotNull(mapa.linea);
            Assert.IsTrue(pantalla.BotonVozVisible);
            Fotograma(120);
            Assert.IsTrue(nav.Activa, "El minuto era de la otra.");
        }

        [Test]
        public void Pantalla_RecalculoEnCamino_SuRutaNoSeDibuja()
        {
            servicio.SordoALaCancelacion = true;
            IrAAreas();
            Ir(D(60, -60), 30);
            Ir(D(62, -60), 6);
            var p = servicio.Peticiones.Last();
            Assert.IsTrue(nav.Recalculando);
            Ir(D(25, 0), 5);
            Assert.IsTrue(pantalla.LlegadaVisible);
            int dibujadas = mapa.dibujadas;
            p.Responder(RutaHasta(antes: 80));
            Assert.AreEqual(dibujadas, mapa.dibujadas);
            Assert.IsNull(mapa.linea);
            Assert.IsTrue(pantalla.LlegadaVisible);
        }

        [Test]
        public void Pantalla_SinSenal_NoSeLlegaNiSaleLaPastillaTrasLlegar()
        {
            IrAAreas();
            Ir(D(0, -35), 30);
            for (int i = 0; i < 30; i++) Fotograma(0.5);
            Assert.IsTrue(pantalla.PastillaSenal.Visible);
            Assert.IsFalse(pantalla.HaLlegado);
            Ir(D(0, -25));
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.IsFalse(pantalla.PastillaSenal.Visible);
            for (int i = 0; i < 40; i++) Fotograma(0.5);
            Assert.IsFalse(pantalla.PastillaSenal.Visible, "Ya en el destino la señal no importa.");
            Assert.IsTrue(pantalla.LlegadaVisible);
        }

        [Test]
        public void Pantalla_ConElAvisoDeNotificacionesDelante_AnunciaYElFocoVaAlCerrarlo()
        {
            IrAAreas();
            gps.RutaSoloPrimerPlano = true;
            Fotograma();
            Assert.IsTrue(pantalla.AvisoAbierto);
            anuncios.Clear();
            Ir(D(0, -25), 30);
            Assert.AreNotSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio));
            pantalla.CerrarAviso();
            Assert.AreSame(Hoja.Finalizar, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(1, anuncios.Count(a => a == Anuncio));
        }

        [Test]
        public void Pantalla_LlegarNoGuardaNada()
        {
            IrAAreas();
            int antes = prefs.Guardadas;
            Ir(D(0, -25), 30);
            Fotograma(60);
            Assert.AreEqual(antes, prefs.Guardadas);
        }

        [Test]
        public void Pantalla_AlDestruirse_DejaDeEscucharLaLlegada()
        {
            IrAAreas();
            typeof(PantallaMapa).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(pantalla, null);
            anuncios.Clear();
            gps.Posicion = D(0, -10);
            Empezar();
            Assert.IsTrue(nav.Llegado);
            Assert.IsEmpty(anuncios);
        }

        // ───────────────────────── Textos y disposición ─────────────────────────

        [Test]
        public void LosTextosEstanEnLaTabla()
        {
            foreach (var clave in new[] { ClavesTexto.LlegadaTitulo, ClavesTexto.LlegadaA, ClavesTexto.LlegadaCierre, ClavesTexto.LlegadaAnuncio })
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
            Assert.AreEqual(Dicho, T(ClavesTexto.LlegadaA, Nombre));
            Assert.AreEqual(Anuncio, T(ClavesTexto.LlegadaAnuncio, Nombre));
            Assert.AreEqual("La ruta se cerrará en 1 minuto", T(ClavesTexto.LlegadaCierre));
            Assert.AreEqual(60.0, ParametrosNegocio.LlegadaCierreSegundos, "Los dos textos dicen un minuto: si cambia, cambian.");
        }

        // Evento de puntero de toque en ese punto del panel (como en PruebasRutas)
        static void Puntero(VisualElement objetivo, Vector2 punto, EventType tipo)
        {
            var sistema = new Event
            {
                type = tipo, mousePosition = punto, button = 0, clickCount = 1, pointerType = UnityEngine.PointerType.Touch,
            };
            using (EventBase e = tipo == EventType.MouseDown ? PointerDownEvent.GetPooled(sistema) : (EventBase)PointerUpEvent.GetPooled(sistema))
            {
                e.target = objetivo;
                objetivo.SendEvent(e);
            }
        }

        static void Disponer(VisualElement e)
        {
            var validar = e.panel.GetType().GetMethod("ValidateLayout",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            validar.Invoke(e.panel, null);
        }

        [TestCase(1f)]
        [TestCase(2f)]
        public void EnUnPanel_ConElTextoGrandeNadaSeCorta(float fuente)
        {
            var objetoPanel = new GameObject("Llegada en un panel");
            try
            {
                objetoPanel.SetActive(false);
                var documento = objetoPanel.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                objetoPanel.SetActive(true);
                var marco = new VisualElement();
                marco.style.position = Position.Absolute;
                marco.style.width = 360f;
                marco.style.height = 800f;
                documento.rootVisualElement.Q("raiz").Add(marco);
                var arriba = new TarjetaLlegada(T);
                var hoja = new HojaLlegada(T);
                marco.Add(arriba);
                marco.Add(hoja);
                arriba.Poner("un destino con un nombre bastante largo que ocupa dos líneas");
                arriba.style.display = DisplayStyle.Flex;
                hoja.style.display = DisplayStyle.Flex;
                Disponer(hoja);
                EscalaFuente.Aplicar(marco, fuente);
                Disponer(hoja);
                Disponer(hoja);
                Assert.AreEqual(marco.worldBound.yMax, hoja.worldBound.yMax, 2f, "Anclada abajo.");
                // El panel de las pruebas redondea a su píxel (96 dpi, 1,67 dp)
                Assert.GreaterOrEqual(hoja.Finalizar.worldBound.height, 50f);
                Assert.GreaterOrEqual(hoja.Finalizar.worldBound.height, hoja.TextoFinalizar.worldBound.height, "El botón crece con su texto.");
                Assert.GreaterOrEqual(hoja.Aviso.worldBound.yMin, hoja.Finalizar.worldBound.yMax);
                Assert.LessOrEqual(hoja.Aviso.worldBound.yMax, hoja.worldBound.yMax);
                Assert.GreaterOrEqual(arriba.Destino.worldBound.yMin, arriba.Titulo.worldBound.yMax - 2f);
                Assert.LessOrEqual(arriba.Destino.worldBound.yMax, arriba.worldBound.yMax);
                Assert.LessOrEqual(arriba.Destino.worldBound.xMax, arriba.worldBound.xMax);
                Assert.GreaterOrEqual(arriba.Titulo.worldBound.xMin, arriba.Marca.worldBound.xMax);
                Assert.AreEqual(44f, arriba.Marca.worldBound.width, 2f);
                foreach (var t in new[] { arriba.Titulo, arriba.Destino, hoja.TextoFinalizar, hoja.Aviso })
                {
                    Assert.AreEqual(WhiteSpace.Normal, t.resolvedStyle.whiteSpace, t.name + " pasa a varias líneas.");
                    float alto = t.MeasureTextSize(t.text, t.contentRect.width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined).y;
                    Assert.GreaterOrEqual(t.worldBound.height + 2f, alto, t.name + " cabe entero.");
                }

                // Barra de gestos de 24 dp: el aviso queda por encima
                hoja.PonerMargenInferior(24f);
                Disponer(hoja);
                Assert.AreEqual(marco.worldBound.yMax, hoja.worldBound.yMax, 2f);
                Assert.GreaterOrEqual(hoja.worldBound.yMax - hoja.Aviso.worldBound.yMax, 24f + HojaLlegada.RellenoAbajo - 2f);

                // El dedo pulsa «Finalizar ruta»: una vez
                int pulsado = 0;
                hoja.FinalizarPulsado += () => pulsado++;
                var punto = hoja.Finalizar.worldBound.center;
                Puntero(hoja.Finalizar, punto, EventType.MouseDown);
                Assert.AreEqual(0, pulsado, "Al soltar.");
                Puntero(hoja.Finalizar, punto, EventType.MouseUp);
                Assert.AreEqual(1, pulsado);
            }
            finally { UnityEngine.Object.DestroyImmediate(objetoPanel); }
        }
    }
}
