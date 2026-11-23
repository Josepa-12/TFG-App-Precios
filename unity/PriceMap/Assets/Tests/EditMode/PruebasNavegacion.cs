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
    /// Seguir la ruta (historia 2.7, parte 1): ReglaNavegacion con rutas sintéticas (proyección, restante, maniobra
    /// siguiente, ruido, desvío y saltos), ServicioNavegacion con ubicación, rutas y reloj falsos (empezar, minutos,
    /// notificación, eventos, desvío, recálculo y reintento, terminar) y la pantalla real, una prueba por fila de la
    /// matriz. Nada sale a la red.
    /// </summary>
    public class PruebasNavegacion
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
        static readonly DateTime HoraLocal = new DateTime(2026, 11, 21, 13, 30, 0, DateTimeKind.Local);
        const float Escala = 0.5f;

        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        static readonly Coordenada Destino = new Coordenada(36.78060, -6.34945);

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        const double MetrosPorGrado = Coordenada.RadioTierraMetros * Math.PI / 180.0;

        /// <summary>Punto a esos metros al norte y al este del Cabildo.</summary>
        static Coordenada M(double norte, double este) =>
            new Coordenada(Cabildo.Latitud + norte / MetrosPorGrado,
                Cabildo.Longitud + este / (MetrosPorGrado * Math.Cos(Cabildo.Latitud * Math.PI / 180.0)));

        const string Salida = "Diríjase al norte por Calle Ancha.";
        const string Giro = "Gire a la derecha hacia Calle Regina.";
        const string Llegada = "Ha llegado a su destino.";

        /// <summary>450 m al norte y 150 m al este, 6 min, con la salida, el giro a la derecha y la llegada.</summary>
        static Ruta RutaEle(double metros = 600, double segundos = 360) =>
            new Ruta(new[] { M(0, 0), M(450, 0), M(450, 150) }, metros, segundos, new[]
            {
                new Maniobra(TipoManiobra.Salida, Salida, M(0, 0), 450),
                new Maniobra(TipoManiobra.Derecha, Giro, M(450, 0), 150),
                new Maniobra(TipoManiobra.Llegada, Llegada, M(450, 150), 0),
            });

        /// <summary>La misma línea sin maniobras (OSRM o aproximada).</summary>
        static Ruta RutaSinManiobras(bool aproximada) =>
            new Ruta(new[] { M(0, 0), M(450, 0), M(450, 150) }, 600, 360, null, aproximada);

        // ───────────────────────── ReglaNavegacion ─────────────────────────

        [Test]
        public void Regla_AlEmpezarLaPrimeraManiobraYTodoPorDelante()
        {
            var regla = new ReglaNavegacion(RutaEle());
            Assert.AreEqual(600, regla.Total, 1.0);
            var e = regla.Seguir(M(0, 0));
            Assert.AreEqual(0, e.Avance, 0.01);
            Assert.AreEqual(0, e.IndiceSiguiente);
            Assert.AreEqual(Salida, e.Siguiente.Texto);
            Assert.AreEqual(Giro, e.Despues.Texto);
            Assert.AreEqual(450, e.MetrosADespues, 1.0);
            Assert.AreEqual(600, e.MetrosRestantes, 0.5);
            Assert.AreEqual(360, e.SegundosRestantes, 0.5);
            Assert.AreEqual(6, e.MinutosRestantes);
            Assert.IsFalse(e.FueraDeRuta);
        }

        [Test]
        public void Regla_AMitadLaMitadDelTiempoYLaManiobraSiguienteConSuDistancia()
        {
            var regla = new ReglaNavegacion(RutaEle());
            var e = regla.Seguir(M(300, 5), regla.Seguir(M(0, 0)));
            Assert.AreEqual(300, e.Avance, 1.0);
            Assert.AreEqual(5, e.DistanciaRuta, 0.2);
            Assert.AreEqual(0, e.Tramo);
            Assert.AreEqual(1, e.IndiceSiguiente);
            Assert.AreEqual(Giro, e.Siguiente.Texto);
            Assert.AreEqual(150, e.MetrosASiguiente, 1.0);
            Assert.AreEqual(Llegada, e.Despues.Texto);
            Assert.AreEqual(180, e.SegundosRestantes, 1.0);
            Assert.AreEqual(3, e.MinutosRestantes);
            Assert.AreEqual(300, e.MetrosRestantes, 1.0);
            Assert.Less(e.Proyectado.DistanciaMetros(M(300, 0)), 0.2);
        }

        [Test]
        public void Regla_LosMetrosQueQuedanSonLosDeLaRutaEnProporcion()
        {
            var regla = new ReglaNavegacion(RutaEle(metros: 1200));
            Assert.AreEqual(1200, regla.Seguir(M(0, 0)).MetrosRestantes, 1.0);
            Assert.AreEqual(600, regla.Seguir(M(300, 0)).MetrosRestantes, 2.0);
        }

        [Test]
        public void Regla_LineaRecortadaEmpiezaEnElPuntoProyectado()
        {
            var regla = new ReglaNavegacion(RutaEle());
            var e = regla.Seguir(M(150, 5));
            var resto = regla.PuntosRestantes(e);
            Assert.AreEqual(3, resto.Count);
            Assert.AreEqual(e.Proyectado, resto[0]);
            Assert.AreEqual(M(450, 0), resto[1]);
            Assert.AreEqual(M(450, 150), resto[2]);

            var alFinal = regla.Seguir(M(450, 150), e);
            var ultimo = regla.PuntosRestantes(alFinal);
            Assert.GreaterOrEqual(ultimo.Count, 2, "Al llegar, una línea de dos puntos (o ninguna se dibuja).");
        }

        [Test]
        public void Regla_PasarLaManiobraSaleLaSiguiente()
        {
            var regla = new ReglaNavegacion(RutaEle());
            var antes = regla.Seguir(M(440, 0));
            Assert.AreEqual(1, antes.IndiceSiguiente);
            Assert.AreEqual(10, antes.MetrosASiguiente, 0.5);
            var e = regla.Seguir(M(450, 20), antes);
            Assert.AreEqual(1, e.Tramo);
            Assert.AreEqual(470, e.Avance, 1.0);
            Assert.AreEqual(2, e.IndiceSiguiente);
            Assert.AreEqual(Llegada, e.Siguiente.Texto);
            Assert.IsNull(e.Despues);
            Assert.AreEqual(130, e.MetrosASiguiente, 1.0);
        }

        [Test]
        public void Regla_RuidoPorDetrasNoVuelveALaManiobraAnterior()
        {
            var regla = new ReglaNavegacion(RutaEle());
            var tras = regla.Seguir(M(450, 20), regla.Seguir(M(440, 0)));
            var ruido = regla.Seguir(M(450, 10), tras); // 10 m por detrás
            Assert.AreEqual(tras.Avance, ruido.Avance, 1e-6, "El avance no baja.");
            Assert.AreEqual(2, ruido.IndiceSiguiente, "Sigue la llegada, no el giro.");
            Assert.Less(ruido.Proyectado.DistanciaMetros(tras.Proyectado), 0.01);

            var aMitad = regla.Seguir(M(150, 0));
            var atras = regla.Seguir(M(140, 0), aMitad);
            Assert.AreEqual(aMitad.Avance, atras.Avance, 1e-6);
            Assert.AreEqual(0, atras.DistanciaRuta, 0.2, "Sigue en la ruta: no cuenta como desvío.");
        }

        [Test]
        public void Regla_DesvioMasDeCuarentaMetros()
        {
            var regla = new ReglaNavegacion(RutaEle());
            var e = regla.Seguir(M(150, 45));
            Assert.AreEqual(45, e.DistanciaRuta, 0.5);
            Assert.IsTrue(e.FueraDeRuta);
            Assert.IsFalse(regla.Seguir(M(150, 39)).FueraDeRuta);
        }

        [Test]
        public void Regla_VueltaPorLaMismaCalle_NoSaltaAlTramoDeVuelta()
        {
            // 300 m al norte, cambio de sentido y 300 m de vuelta por la misma calle
            var ruta = new Ruta(new[] { M(0, 0), M(300, 0), M(0, 1) }, 600, 360, new[]
            {
                new Maniobra(TipoManiobra.Salida, Salida, M(0, 0), 300),
                new Maniobra(TipoManiobra.CambioSentido, "Cambie de sentido.", M(300, 0), 300),
                new Maniobra(TipoManiobra.Llegada, Llegada, M(0, 1), 0),
            });
            var regla = new ReglaNavegacion(ruta);
            Assert.AreEqual(300, regla.PosicionManiobra(1).Value, 1.0);
            Assert.AreEqual(600, regla.PosicionManiobra(2).Value, 2.0, "La llegada, en la vuelta (sus metros lo dicen).");
            var e = regla.Seguir(M(50, 0.6), regla.Seguir(M(40, 0)));
            Assert.AreEqual(50, e.Avance, 2.0, "A la ida, aunque la vuelta esté igual de cerca.");
            Assert.AreEqual(1, e.IndiceSiguiente);
            // Mucho después (p. ej. sin señal un rato) y en la vuelta, sí salta
            var vuelta = regla.Seguir(M(100, 1), regla.Seguir(M(300, 0), e));
            Assert.Greater(vuelta.Avance, 450);
            Assert.AreEqual(2, vuelta.IndiceSiguiente);
        }

        [Test]
        public void Regla_SinManiobrasConTexto_NoHaySiguiente()
        {
            var regla = new ReglaNavegacion(RutaSinManiobras(aproximada: true));
            var e = regla.Seguir(M(150, 0));
            Assert.AreEqual(-1, e.IndiceSiguiente);
            Assert.IsNull(e.Siguiente);
            Assert.AreEqual(450, e.MetrosRestantes, 1.0);

            // OSRM: maniobras con tipo pero sin texto
            var osrm = new Ruta(new[] { M(0, 0), M(300, 0) }, 300, 60, new[] { new Maniobra(TipoManiobra.Derecha, "", M(300, 0), 0) });
            Assert.IsNull(new ReglaNavegacion(osrm).Seguir(M(10, 0)).Siguiente);
        }

        [Test]
        public void Regla_RutasDegeneradas()
        {
            var vacia = new ReglaNavegacion(new Ruta(null, 0, 0, null));
            Assert.AreEqual(0, vacia.Seguir(M(0, 0)).MetrosRestantes);
            var uno = new ReglaNavegacion(new Ruta(new[] { M(0, 0) }, 0, 0, null));
            Assert.AreEqual(10, uno.Seguir(M(10, 0)).DistanciaRuta, 0.1);
            var repetidos = new ReglaNavegacion(new Ruta(new[] { M(0, 0), M(0, 0), M(100, 0) }, 100, 60, null));
            Assert.AreEqual(50, repetidos.Seguir(M(50, 0)).Avance, 0.5);
        }

        // ───────────────────────── Falsos ─────────────────────────

        class Peticion
        {
            public Coordenada Origen, Destino;
            public ModoViaje Modo;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<Ruta>> Tcs;
            public void Responder(Ruta ruta) => Tcs.TrySetResult(Resultado<Ruta>.Exito(ruta));
            public void Fallar() => Tcs.TrySetResult(Resultado<Ruta>.Fallo("sin conexión"));
        }

        class RutasFalsas : IRutas
        {
            public readonly List<Peticion> Peticiones = new List<Peticion>();

            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                var p = new Peticion { Origen = origen, Destino = destino, Modo = modo, Ct = ct, Tcs = new TaskCompletionSource<Resultado<Ruta>>() };
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

        /// <summary>GPS falso: la prueba lo mueve (Mover avisa por Cambiado) y anota lo que se pide de la ruta.</summary>
        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.Activa;
            public Coordenada? Posicion { get; set; }
            public float? Rumbo { get; set; }
            public event Action Cambiado;
            public void PedirPermiso() { }
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
            public bool RutaActiva { get; private set; }
            public bool RutaSoloPrimerPlano => false;
            public readonly List<string> Iniciadas = new List<string>();
            public readonly List<string> Notificaciones = new List<string>();
            public int Terminadas;
            public Resultado<bool> IniciarRuta(string texto) { Iniciadas.Add(texto); RutaActiva = true; return Resultado<bool>.Exito(true); }
            public void ActualizarNotificacion(string texto) => Notificaciones.Add(texto);
            public void TerminarRuta() { Terminadas++; RutaActiva = false; }
            public void Mover(Coordenada c) { Posicion = c; Cambiado?.Invoke(); }
            public void Avisar() => Cambiado?.Invoke();
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
            public bool aproximada;
            public int dibujadas;
            public readonly List<IReadOnlyList<Coordenada>> encuadres = new List<IReadOnlyList<Coordenada>>();
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { linea = puntos; this.aproximada = aproximada; dibujadas++; }
            public void QuitarRuta() => linea = null;
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) => encuadres.Add(puntos);
        }

        SynchronizationContext contextoAntes;
        RutasFalsas valhalla;
        Esperas esperas;
        DateTime reloj;
        UbicacionFalsa gps;
        ServicioUbicacion ubicacion;
        ServicioRutas rutas;
        ServicioNavegacion nav;
        readonly List<string> eventos = new List<string>();
        readonly List<string> anuncios = new List<string>();
        int actualizados;

        static readonly TimeSpan Reintento = TimeSpan.FromSeconds(10);

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones corren al responder
            Textos.Forzados = T;
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            valhalla = new RutasFalsas();
            esperas = new Esperas();
            reloj = Ahora;
            gps = new UbicacionFalsa { Posicion = M(0, 0) };
            ubicacion = new ServicioUbicacion(gps, new PreferenciasFalsas());
            rutas = new ServicioRutas(valhalla, null, esperas.Esperar, () => reloj);
            nav = new ServicioNavegacion(ubicacion, rutas, esperas.Esperar, () => reloj,
                (destino, minutos) => Textos.Formatear(T(ClavesTexto.NotificacionNavegando), destino, minutos));
            nav.RutaIniciada += d => eventos.Add("iniciada " + d);
            nav.TiempoRestanteCambiado += s => eventos.Add($"tiempo {s:F0}");
            nav.RutaTerminada += m => eventos.Add("terminada " + m);
            nav.RutaCambiada += () => eventos.Add("cambiada");
            nav.RecalculoFallido += () => eventos.Add("fallo");
            nav.Actualizado += () => actualizados++;
            eventos.Clear();
            anuncios.Clear();
            actualizados = 0;
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

        void Empezar(Ruta ruta = null) => nav.Empezar(ruta ?? RutaEle(), Destino, "Clínica Areas", ModoViaje.Coche);

        /// <summary>Se mueve a esa posición pasados esos segundos desde la anterior.</summary>
        void Ir(Coordenada c, double segundos = 1)
        {
            reloj = reloj.AddSeconds(segundos);
            gps.Mover(c);
        }

        // ───────────────────────── ServicioNavegacion ─────────────────────────

        [Test]
        public void Servicio_EmpezarEnciendeElGpsDeFondoConLaNotificacionYAvisa()
        {
            Empezar();
            Assert.IsTrue(nav.Activa);
            Assert.IsTrue(nav.GpsDeFondo);
            CollectionAssert.AreEqual(new[] { "Navegando a Clínica Areas · 6 min" }, gps.Iniciadas);
            CollectionAssert.AreEqual(new[] { "iniciada Clínica Areas" }, eventos);
            Assert.AreEqual(1, actualizados);
            Assert.AreEqual(0, nav.Estado.IndiceSiguiente);
            Assert.AreEqual(6, nav.Estado.MinutosRestantes);
        }

        [Test]
        public void Servicio_AvanzarBajaElTiempoYCambiaLaNotificacionAlCambiarElMinuto()
        {
            Empezar();
            Ir(M(110, 0)); // 294 s: 5 min
            CollectionAssert.AreEqual(new[] { "Navegando a Clínica Areas · 5 min" }, gps.Notificaciones);
            Assert.Contains("tiempo 294", eventos);
            Ir(M(115, 0)); // 291 s: siguen 5
            Assert.AreEqual(1, gps.Notificaciones.Count, "Solo al cambiar el minuto.");
            Assert.AreEqual(1, eventos.Count(e => e.StartsWith("tiempo")));
            Ir(M(300, 0)); // a mitad: 180 s
            Assert.AreEqual("Navegando a Clínica Areas · 3 min", gps.Notificaciones.Last());
            Assert.AreEqual(4, actualizados, "Un Actualizado al empezar y uno por posición.");
        }

        [Test]
        public void Servicio_ElRumboSoloNoCuentaComoPosicion()
        {
            Empezar();
            gps.Rumbo = 90f;
            gps.Avisar();
            Assert.AreEqual(1, actualizados);
        }

        [Test]
        public void Servicio_DesvioCorto_NoRecalcula()
        {
            Empezar();
            Ir(M(150, 45));
            Ir(M(152, 45), 3);
            Assert.IsEmpty(valhalla.Peticiones);
            Ir(M(155, 0), 1); // vuelve
            Ir(M(158, 45), 4);
            Ir(M(160, 45), 4); // 4 s desviado: el contador empezó otra vez
            Assert.IsEmpty(valhalla.Peticiones);
        }

        [Test]
        public void Servicio_DesvioLargo_RecalculaDesdeLaPosicionYLaViejaSigueHastaLaNueva()
        {
            Empezar();
            var vieja = nav.Ruta;
            Ir(M(150, 45));
            Ir(M(152, 45), 3);
            Ir(M(154, 45), 3); // 6 s
            Assert.AreEqual(1, valhalla.Peticiones.Count);
            var p = valhalla.Peticiones[0];
            Assert.AreEqual(M(154, 45), p.Origen);
            Assert.AreEqual(Destino, p.Destino);
            Assert.AreEqual(ModoViaje.Coche, p.Modo);
            Assert.IsTrue(nav.Recalculando);
            Assert.AreSame(vieja, nav.Ruta, "La vieja sigue hasta tener la nueva.");
            Ir(M(156, 45), 1);
            Assert.AreEqual(1, valhalla.Peticiones.Count, "Uno solo a la vez.");

            var nueva = new Ruta(new[] { M(156, 45), M(300, 45) }, 144, 90, null);
            p.Responder(nueva);
            Assert.AreSame(nueva, nav.Ruta);
            Assert.IsFalse(nav.Recalculando);
            Assert.Contains("cambiada", eventos);
            Assert.AreEqual(0, nav.Estado.DistanciaRuta, 0.5);
            Assert.AreEqual("Navegando a Clínica Areas · 2 min", gps.Notificaciones.Last());
        }

        [Test]
        public void Servicio_RecalculoFalla_AvisaYReintentaALosDiezSegundos()
        {
            Empezar();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones[0].Fallar();
            Assert.Contains("fallo", eventos);
            Assert.AreEqual(1, esperas.Pendientes(Reintento));
            Assert.IsTrue(nav.Recalculando, "Esperando el reintento: no hay otro.");
            Ir(M(150, 47), 6);
            Assert.AreEqual(1, valhalla.Peticiones.Count);

            esperas.Vencer(Reintento);
            Assert.AreEqual(2, valhalla.Peticiones.Count, "Otra vez a los 10 s.");
            Assert.AreEqual(M(150, 47), valhalla.Peticiones[1].Origen, "Desde la última posición.");
        }

        [Test]
        public void Servicio_RecalculoFalla_SiHaVueltoALaRutaNoReintenta()
        {
            Empezar();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones[0].Fallar();
            Ir(M(160, 0), 2); // vuelve a la ruta
            esperas.Vencer(Reintento);
            Assert.AreEqual(1, valhalla.Peticiones.Count);
            Assert.IsFalse(nav.Recalculando);
        }

        [Test]
        public void Servicio_SinConexionElRecalculoDaLaAproximada()
        {
            var local = new RutasFalsas();
            rutas = new ServicioRutas(valhalla, null, esperas.Esperar, () => reloj, local);
            nav = new ServicioNavegacion(ubicacion, rutas, esperas.Esperar, () => reloj);
            Empezar();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones[0].Fallar();
            var aproximada = new Ruta(new[] { M(150, 46), M(300, 300) }, 300, 200, null, aproximada: true);
            local.Peticiones[0].Responder(aproximada);
            Assert.AreSame(aproximada, nav.Ruta);
        }

        [Test]
        public void Servicio_LejosDeLaCalle_NoRecalculaDesdeElMismoSitio()
        {
            gps.Posicion = M(0, 60); // la ruta empieza a 60 m: ya está "fuera"
            Empezar();
            Assert.IsTrue(nav.Estado.FueraDeRuta);
            Ir(M(1, 60));
            Ir(M(2, 60), 6);
            Assert.IsEmpty(valhalla.Peticiones, "Desde el mismo sitio saldría la misma ruta.");
            Ir(M(100, 60), 1); // se ha movido: ahora sí
            Assert.AreEqual(1, valhalla.Peticiones.Count);
            Assert.AreEqual(M(100, 60), valhalla.Peticiones[0].Origen);

            valhalla.Peticiones[0].Responder(new Ruta(new[] { M(100, 0), M(450, 0), M(450, 150) }, 500, 300, null));
            Ir(M(101, 60), 1);
            Ir(M(102, 60), 6);
            Assert.AreEqual(1, valhalla.Peticiones.Count, "La nueva también empieza lejos: no se pide otra vez sin moverse.");
        }

        [Test]
        public void Servicio_VuelveALaRutaMientrasSePide_SeQuedaLaDeAntes()
        {
            Empezar();
            var vieja = nav.Ruta;
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            Assert.AreEqual(1, valhalla.Peticiones.Count);
            Ir(M(155, 0), 1); // vuelve antes de la respuesta
            valhalla.Peticiones[0].Responder(new Ruta(new[] { M(150, 46), M(300, 300) }, 300, 200, null));
            Assert.AreSame(vieja, nav.Ruta);
            Assert.IsFalse(nav.Recalculando);
            Assert.IsFalse(eventos.Contains("cambiada"));
        }

        [Test]
        public void Minutos_ConDecimales_IgualEnLaFichaYEnLaNavegacion()
        {
            Assert.AreEqual(6, Ruta.MinutosDe(360.3), "Al segundo: 360 s son 6 min.");
            Assert.AreEqual(7, Ruta.MinutosDe(360.6));
            Assert.AreEqual(3, Ruta.MinutosDe(180.0001));
            var ruta = RutaEle(segundos: 360.3);
            Assert.AreEqual(ruta.Minutos, new ReglaNavegacion(ruta).Seguir(M(0, 0)).MinutosRestantes);
        }

        [Test]
        public void Servicio_TerminarParaElGpsDeFondoYCancelaElRecalculo()
        {
            Empezar();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            var p = valhalla.Peticiones[0];
            nav.Terminar();
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual(1, gps.Terminadas);
            Assert.AreEqual("terminada Manual", eventos.Last());
            Assert.IsTrue(p.Ct.IsCancellationRequested);
            int antes = actualizados;
            Ir(M(160, 0));
            Assert.AreEqual(antes, actualizados, "Sin ruta, las posiciones no hacen nada.");
            nav.Terminar();
            Assert.AreEqual(1, gps.Terminadas, "Terminar dos veces no hace nada.");
        }

        [Test]
        public void Servicio_EmpezarOtraTerminaLaDeAntes()
        {
            Empezar();
            Empezar(RutaSinManiobras(false));
            CollectionAssert.AreEqual(new[] { "iniciada Clínica Areas", "terminada Manual", "iniciada Clínica Areas" }, eventos);
            Assert.AreEqual(1, gps.Terminadas);
            Assert.AreEqual(2, gps.Iniciadas.Count);
        }

        [Test]
        public void Parametros_EnParametrosNegocio()
        {
            Assert.AreEqual(40.0, ParametrosNegocio.NavegacionDesvioMetros);
            Assert.AreEqual(5.0, ParametrosNegocio.NavegacionDesvioSegundos);
            Assert.AreEqual(10.0, ParametrosNegocio.NavegacionReintentoSegundos);
            Assert.AreEqual(TimeSpan.FromSeconds(10), ServicioNavegacion.EsperaReintento);
        }

        [Test]
        public void LosTextosEstanEnLaTabla()
        {
            foreach (var clave in new[]
                     {
                         ClavesTexto.RutaEn, ClavesTexto.RutaQuedan, ClavesTexto.RutaAnuncioManiobra,
                         ClavesTexto.ToastRecalculoFallo, ClavesTexto.NotificacionNavegando,
                     })
            {
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
                Assert.IsTrue(PrepararInterfaz.TextosEs.ContainsKey(clave), clave);
            }
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

        TarjetaManiobra Arriba => pantalla.TarjetaManiobra;
        TarjetaRuta Abajo => pantalla.TarjetaRuta;

        void CrearPantalla()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            prefs.Guardar(ServicioUbicacion.ClaveExplicacion, "1");
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, ubicacion, rutas: rutas, navegacion: nav);
            pantalla.RelojLocal = () => HoraLocal;
            tema.Iniciar();
            filtro.Iniciar();
        }

        /// <summary>Ficha de Clínica Areas, Ir, la ruta llega e Iniciar.</summary>
        void IrAAreas(Ruta ruta = null)
        {
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(ruta ?? RutaEle());
            pantalla.Iniciar();
        }

        [Test]
        public void Pantalla_Empezar_PrimeraManiobraSeisMinutosNotificacionYEvento()
        {
            IrAAreas();
            Assert.IsTrue(pantalla.Navegando);
            Assert.AreEqual("Diríjase al norte por Calle Ancha", Arriba.Principal.text);
            Assert.AreEqual("Gire a la derecha hacia Calle Regina", Arriba.Siguiente.text);
            Assert.AreEqual(DisplayStyle.None, Arriba.Distancia.style.display.value, "En la salida misma, sin «En 0 m».");
            Assert.AreEqual("6", Abajo.Minutos.text);
            Assert.AreEqual("13:36", Abajo.Llegada.text);
            CollectionAssert.AreEqual(new[] { "Navegando a Clínica Areas · 6 min" }, gps.Iniciadas);
            Assert.Contains("iniciada Clínica Areas", eventos);
            Assert.AreEqual(Areas.Posicion, nav.PuntoDestino, "El recálculo va al lugar, no a la calle.");
        }

        [Test]
        public void Pantalla_Avanzar_TresMinutosManiobraConSuDistanciaYLineaRecortada()
        {
            IrAAreas();
            Ir(M(300, 0)); // a mitad
            Assert.AreEqual("Gire a la derecha hacia Calle Regina", Arriba.Principal.text);
            Assert.AreEqual(DisplayStyle.Flex, Arriba.Distancia.style.display.value);
            Assert.AreEqual("En 150 m", Arriba.Distancia.text);
            Assert.AreEqual("3", Abajo.Minutos.text);
            Assert.AreEqual("13:33", Abajo.Llegada.text);
            Assert.AreEqual("300", Abajo.Distancia.text);
            Assert.AreEqual(3, mapa.linea.Count);
            Assert.Less(mapa.linea[0].DistanciaMetros(M(300, 0)), 0.2, "La línea empieza en el punto proyectado.");
            Assert.AreEqual(M(450, 150), mapa.linea.Last());
        }

        [Test]
        public void Pantalla_PasarManiobra_SaleLaSiguienteYTalkBackLaAnunciaUnaVez()
        {
            IrAAreas();
            Ir(M(300, 0));
            anuncios.Clear();
            Ir(M(310, 0));
            Assert.IsEmpty(anuncios, "No a cada metro.");
            Ir(M(450, 20));
            Assert.AreEqual("Ha llegado a su destino", Arriba.Principal.text);
            Assert.AreEqual(DisplayStyle.None, Arriba.FilaSiguiente.style.display.value);
            CollectionAssert.AreEqual(new[] { "En 130 m, ha llegado a su destino" }, anuncios);
            Ir(M(450, 30));
            Assert.AreEqual(1, anuncios.Count, "Una sola vez.");
            Assert.AreEqual("Ha llegado a su destino", Arriba.Anuncio, "La etiqueta de TalkBack no lleva los metros.");
        }

        [Test]
        public void Pantalla_Ruido_NoVuelveALaManiobraAnterior()
        {
            IrAAreas();
            Ir(M(450, 20));
            Ir(M(450, 10));
            Assert.AreEqual("Ha llegado a su destino", Arriba.Principal.text);
        }

        [Test]
        public void Pantalla_DesvioLargo_RecalculaYLaViejaSeVeHastaLaNueva()
        {
            IrAAreas();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            var peticion = valhalla.Peticiones.Last();
            Assert.AreEqual(M(150, 46), peticion.Origen);
            Assert.IsNotNull(mapa.linea, "La vieja sigue.");
            Assert.AreEqual(M(450, 150), mapa.linea.Last());
            var nueva = new Ruta(new[] { M(150, 46), M(150, 300), M(300, 300) }, 400, 240, null);
            anuncios.Clear();
            peticion.Responder(nueva);
            Assert.AreSame(nueva, pantalla.RutaDibujada);
            Assert.AreEqual(M(150, 300), mapa.linea[1], "La línea es la nueva.");
            Assert.AreEqual("4", Abajo.Minutos.text);
            Assert.AreEqual(1, anuncios.Count, "TalkBack anuncia una vez lo de la ruta nueva.");
        }

        [Test]
        public void Pantalla_RecalculoSinConexion_ToastDeRutaAproximada()
        {
            var local = new RutasFalsas();
            rutas = new ServicioRutas(valhalla, null, esperas.Esperar, () => reloj, local);
            nav = new ServicioNavegacion(ubicacion, rutas, esperas.Esperar, () => reloj);
            IrAAreas();
            Assert.IsFalse(pantalla.Toast.Visible);
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones.Last().Fallar();
            local.Peticiones.Last().Responder(new Ruta(new[] { M(150, 46), M(300, 300) }, 300, 200, null, aproximada: true));
            Assert.IsTrue(nav.Ruta.Aproximada);
            Assert.IsTrue(pantalla.Toast.Visible);
            Assert.AreEqual(Textos.T(ClavesTexto.ToastRutaAproximada), pantalla.Toast.Texto.text);
            Assert.IsTrue(mapa.aproximada);
        }

        [Test]
        public void Pantalla_AlDestruirse_TerminaLaRuta()
        {
            IrAAreas();
            // En el editor Unity no llama a OnDestroy de un componente sin ExecuteAlways: se llama como lo hace en la app
            typeof(PantallaMapa).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(pantalla, null);
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual(1, gps.Terminadas, "Fuera el GPS de fondo y la notificación.");
        }

        [Test]
        public void ElArranqueLeDaLaNavegacionALaPantalla()
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
                    new ServicioFiltro(prefs), Ahora, ubicacion, rutas: rutas, navegacion: nav));
                Assert.AreSame(nav, conUi.Navegacion, "Con el árbol real (tarjeta de arriba incluida) la pantalla se queda la navegación.");
            }
            finally { UnityEngine.Object.DestroyImmediate(conInterfaz); }
        }

        [Test]
        public void Pantalla_RecalculoFalla_ToastYReintento()
        {
            IrAAreas();
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones.Last().Fallar();
            Assert.IsTrue(pantalla.Toast.Visible);
            Assert.AreEqual("No se ha podido recalcular la ruta", pantalla.Toast.Texto.text);
            Assert.Contains("No se ha podido recalcular la ruta", anuncios);
            Assert.IsNotNull(mapa.linea, "La ruta vieja sigue.");
            int antes = valhalla.Peticiones.Count;
            esperas.Vencer(Reintento);
            Assert.AreEqual(antes + 1, valhalla.Peticiones.Count);
        }

        [Test]
        public void Pantalla_SinManiobras_RutaAproximadaConLoQueQuedaYAproxAbajo()
        {
            var local = new RutasFalsas();
            rutas = new ServicioRutas(valhalla, null, esperas.Esperar, () => reloj, local);
            nav = new ServicioNavegacion(ubicacion, rutas, esperas.Esperar, () => reloj);
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.Peticiones.Last().Fallar();
            local.Peticiones.Last().Responder(RutaSinManiobras(aproximada: true));
            pantalla.Iniciar();
            Ir(M(150, 0));
            Assert.AreEqual("Ruta aproximada por las calles guardadas", Arriba.Principal.text);
            Assert.AreEqual("Quedan 450 m", Arriba.Distancia.text);
            Assert.IsTrue(Abajo.Minutos.text.StartsWith("≈"));
            Assert.IsTrue(mapa.aproximada);
        }

        [Test]
        public void Pantalla_SinManiobrasDeOsrm_RutaAConLoQueQueda()
        {
            IrAAreas(RutaSinManiobras(false));
            Ir(M(100, 0));
            Assert.AreEqual("Ruta a Clínica Areas", Arriba.Principal.text);
            Assert.AreEqual("Quedan 500 m", Arriba.Distancia.text);
            Assert.AreEqual("5", Abajo.Minutos.text);
        }

        [Test]
        public void Pantalla_TerminarConFinalizarRuta()
        {
            IrAAreas();
            Abajo.Desplegar();
            Assert.AreEqual("Finalizar ruta", Abajo.TextoFinalizar.text, "Navegando, el botón dice «Finalizar ruta».");
            Abajo.PulsarFinalizar();
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsFalse(nav.Activa);
            Assert.IsNull(mapa.linea);
            Assert.AreEqual(1, gps.Terminadas);
            Assert.AreEqual("terminada Manual", eventos.Last());
            Assert.IsTrue(pantalla.Buscador.Visible, "Vuelve la cápsula del buscador.");
            Assert.IsFalse(Abajo.Desplegada, "La hoja se cierra plegada con la ruta.");
        }

        // ───────────────────────── Hoja de la ruta que solo informa (22/11/2026) ─────────────────────────

        [Test]
        public void Hoja_TocarPlegada_SeDespliegaYLaRutaSigueSinFicha()
        {
            IrAAreas();
            Assert.IsFalse(Abajo.Desplegada, "La ruta empieza con la hoja plegada.");
            Assert.AreEqual(DisplayStyle.None, Abajo.Desplegable.style.display.value);
            Abajo.Tocar();
            Assert.IsTrue(Abajo.Desplegada);
            Assert.AreEqual(DisplayStyle.Flex, Abajo.Desplegable.style.display.value, "Se ven el destino y el botón.");
            Assert.IsFalse(pantalla.FichaAbierta, "La ficha no se abre.");
            Assert.IsTrue(pantalla.Navegando, "La ruta sigue.");
            Assert.AreEqual(0, gps.Terminadas);
            Assert.AreEqual(DisplayStyle.Flex, Abajo.style.display.value, "Las tarjetas siguen a la vista.");
        }

        [Test]
        public void Hoja_Desplegada_TocarArribaArrastrarAbajoOAtrasLaPliegan()
        {
            IrAAreas();
            Abajo.Desplegar();
            Abajo.Tocar(); // la parte de arriba
            Assert.IsFalse(Abajo.Desplegada, "Tocar arriba la pliega.");
            Abajo.Desplegar();
            Abajo.Soltar(30f);
            Assert.IsFalse(Abajo.Desplegada, "Arrastrar hacia abajo la pliega.");
            Abajo.Desplegar();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(Abajo.Desplegada, "Atrás la pliega.");
            Assert.IsTrue(pantalla.Navegando, "La ruta sigue.");
            Assert.AreEqual(0, gps.Terminadas);
        }

        [Test]
        public void Hoja_AtrasPlegada_QuitaLaRuta()
        {
            IrAAreas();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.IsFalse(nav.Activa);
        }

        [Test]
        public void Hoja_Arrastre_ArribaDespliegaAbajoPliegaYPocoEsUnToque()
        {
            IrAAreas();
            Abajo.Soltar(-7f);
            Assert.IsTrue(Abajo.Desplegada, "Más de 6 dp hacia arriba la despliega.");
            Abajo.Soltar(-20f);
            Assert.IsTrue(Abajo.Desplegada, "Hacia arriba desplegada se queda.");
            Abajo.Soltar(7f);
            Assert.IsFalse(Abajo.Desplegada, "Más de 6 dp hacia abajo la pliega.");
            Abajo.Soltar(20f);
            Assert.IsFalse(Abajo.Desplegada, "Hacia abajo plegada se queda.");
            Abajo.Soltar(3f);
            Assert.IsTrue(Abajo.Desplegada, "Menos de 6 dp es un toque.");
            Assert.IsTrue(pantalla.Navegando);
            Assert.IsFalse(pantalla.FichaAbierta);
        }

        [Test]
        public void Hoja_Recalculo_LaRutaNuevaEmpiezaPlegada()
        {
            IrAAreas();
            Abajo.Desplegar();
            pantalla.Accesibilidad.PedirFoco(Arriba); // TalkBack está en la tarjeta de arriba
            Ir(M(150, 45));
            Ir(M(150, 46), 6);
            valhalla.Peticiones.Last().Responder(new Ruta(new[] { M(150, 46), M(150, 300), M(300, 300) }, 400, 240, null));
            Assert.IsTrue(pantalla.Navegando);
            Assert.IsFalse(Abajo.Desplegada, "Con el recálculo la hoja se pliega.");
            Assert.AreSame(Arriba, pantalla.Accesibilidad.Enfocado, "Plegarla la app no se lleva el foco de TalkBack.");
        }

        [Test]
        public void Hoja_FinDeLaNavegacion_SePliegaYLaSiguienteEmpiezaPlegada()
        {
            IrAAreas();
            Abajo.Desplegar();
            nav.Terminar(); // como la llegada (historia 2.9): el servicio termina la ruta
            Assert.IsFalse(Abajo.Desplegada, "Se cierra plegada.");
            Assert.AreEqual("Quitar ruta", Abajo.TextoFinalizar.text, "Sin navegación, «Quitar ruta».");
            Abajo.Desplegar();
            pantalla.ReabrirFichaRuta();
            pantalla.Ficha.Acciones.PulsarIr(); // las rutas salen de la caché
            pantalla.Iniciar();
            Assert.IsTrue(pantalla.Navegando);
            Assert.IsFalse(Abajo.Desplegada, "La siguiente empieza plegada.");
        }

        [Test]
        public void Hoja_FilaDelDestino_IconoNombreYCategoriaConElModo()
        {
            IrAAreas();
            Assert.AreEqual("Clínica Areas", Abajo.DestinoNombre.text);
            Assert.AreEqual(Areas.Tipo + " · en coche", Abajo.DestinoDetalle.text);
            Assert.AreEqual(IconosLugar.GlifoDe(Areas.Tipo), Abajo.GlifoDestino);
            Assert.IsTrue(Abajo.DestinoIcono.ClassListContains(PantallaMapa.ClaseIconoDestino(Areas.Plantilla)));
            Assert.AreEqual("Destino: Clínica Areas, " + Areas.Tipo + ", en coche", Abajo.AnuncioDestino);
        }

        [Test]
        public void Hoja_TalkBack_DesplegarPlegarYFinalizarRutaComoBotonPropio()
        {
            IrAAreas();
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            var plegada = pantalla.Accesibilidad.Recorrido();
            Assert.Contains(Abajo.Anuncio, plegada, string.Join(" | ", plegada));
            Assert.IsFalse(plegada.Contains("Finalizar ruta"), "Plegada, sin botón.");
            Assert.IsFalse(plegada.Contains(Abajo.AnuncioDestino));
            Assert.AreEqual("Desplegar", ValorTalkBack(Abajo.Cuerpo));

            Abajo.Tocar(); // el doble toque de TalkBack hace lo mismo
            var desplegada = pantalla.Accesibilidad.Recorrido();
            int hoja = desplegada.IndexOf(Abajo.Anuncio);
            Assert.GreaterOrEqual(hoja, 0, string.Join(" | ", desplegada));
            Assert.AreEqual(Abajo.AnuncioDestino, desplegada[hoja + 1]);
            Assert.AreEqual("Finalizar ruta", desplegada[hoja + 2]);
            Assert.IsFalse(desplegada.Contains("Terminar ruta"));
            Assert.AreEqual("Plegar", ValorTalkBack(Abajo.Cuerpo));
            Assert.AreSame(Abajo.Cuerpo, pantalla.Accesibilidad.Enfocado, "El foco se queda en la hoja.");
        }

        // Valor de TalkBack de un control (el "valor" declarado en AccesibilidadPantalla.Boton)
        string ValorTalkBack(VisualElement e)
        {
            var campo = typeof(AccesibilidadPantalla).GetField("controles", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var lista = (System.Collections.IList)campo.GetValue(pantalla.Accesibilidad);
            foreach (var c in lista)
            {
                var t = c.GetType();
                if (!ReferenceEquals(t.GetField("elemento").GetValue(c), e)) continue;
                var valor = (Func<string>)t.GetField("valor").GetValue(c);
                return valor?.Invoke();
            }
            return null;
        }

        [Test]
        public void Pantalla_TerminarConAtras()
        {
            IrAAreas();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual(1, gps.Terminadas);
            Assert.AreEqual("terminada Manual", eventos.Last());
        }

        [Test]
        public void Pantalla_TalkBack_ElBotonEsFinalizarRuta()
        {
            IrAAreas();
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            Abajo.Desplegar();
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.Contains("Finalizar ruta", recorrido, string.Join(" | ", recorrido));
            Assert.IsFalse(recorrido.Contains("Quitar ruta"));
        }

        [Test]
        public void Pantalla_OtraRutaDesdeLaFichaReabierta_TerminaLaDeAntesYEmpiezaLaNueva()
        {
            IrAAreas();
            pantalla.ReabrirFichaRuta();
            pantalla.Ficha.Acciones.PulsarIr(); // las rutas salen de la caché al momento
            pantalla.Iniciar();
            CollectionAssert.AreEqual(new[] { "iniciada Clínica Areas", "terminada Manual", "iniciada Clínica Areas" }, eventos);
            Assert.AreEqual(1, gps.Terminadas);
            Assert.AreEqual(2, gps.Iniciadas.Count);
            Assert.IsTrue(nav.Activa);
            Assert.IsTrue(pantalla.Navegando);
        }

        [Test]
        public void Pantalla_SinServicioDeNavegacion_ComoEnLa26()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            prefs.Guardar(ServicioUbicacion.ClaveExplicacion, "1");
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, new ServicioTema(prefs, new SistemaClaro()), Lugares(), Ahora, arbol, new ServicioFiltro(prefs),
                ubicacion, rutas: rutas);
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(RutaEle());
            pantalla.Iniciar();
            Assert.IsFalse(pantalla.Navegando);
            Assert.IsEmpty(gps.Iniciadas);
            Assert.AreEqual(3, mapa.linea.Count);
            Ir(M(150, 0));
            Assert.AreEqual("Diríjase al norte por Calle Ancha", Arriba.Principal.text, "Sin navegación las tarjetas no cambian.");
        }
    }
}
