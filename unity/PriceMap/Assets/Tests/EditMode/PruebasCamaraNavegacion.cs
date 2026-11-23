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
using PriceMap.Vectorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// La cámara sigue al usuario (historia 2.7, parte 2b): ReglaRumbo (rumbo de la marcha, velocidad, elegir el de
    /// arriba), SeguimientoCamara (esperar 2 s, seguir, soltar, recentrar), la cámara real (transición, al instante,
    /// gesto) y la pantalla real con un mapa falso, una prueba por fila de la matriz. Pantalla encendida por su puerto.
    /// Nada sale a la red.
    /// </summary>
    public class PruebasCamaraNavegacion
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

        // ───────────────────────── ReglaRumbo ─────────────────────────

        DateTime reloj;

        [Test]
        public void Rumbo_DosPosicionesAlEsteA5ms_Da90()
        {
            reloj = Ahora;
            var r = new ReglaRumbo(() => reloj);
            r.Nueva(M(0, 0));
            Assert.IsNull(r.Rumbo, "Con una sola posición, aún no hay rumbo.");
            reloj = reloj.AddSeconds(1);
            r.Nueva(M(0, 5));
            Assert.AreEqual(90, r.Rumbo.Value, 0.1);
            Assert.AreEqual(5, r.Velocidad.Value, 0.05);
        }

        [Test]
        public void Rumbo_Parado_SeQuedaElUltimo()
        {
            reloj = Ahora;
            var r = new ReglaRumbo(() => reloj);
            r.Nueva(M(0, 0));
            reloj = reloj.AddSeconds(1);
            r.Nueva(M(0, 5)); // al este
            // Hacia el norte a 0,55 m/s: los pasos cortos se suman hasta los 3 m, pero va despacio
            for (int i = 1; i <= 8; i++)
            {
                reloj = reloj.AddSeconds(1);
                r.Nueva(M(0.55 * i, 5));
            }
            Assert.AreEqual(90, r.Rumbo.Value, 0.1, "Parado (≤ 1 m/s) se queda el último.");
            Assert.AreEqual(0.55, r.Velocidad.Value, 0.05);
        }

        [Test]
        public void Rumbo_MenosDe3Metros_NoCambiaNada()
        {
            reloj = Ahora;
            var r = new ReglaRumbo(() => reloj);
            r.Nueva(M(0, 0));
            reloj = reloj.AddSeconds(1);
            r.Nueva(M(2, 0));
            Assert.IsNull(r.Rumbo);
            Assert.IsNull(r.Velocidad);
            reloj = reloj.AddSeconds(1);
            r.Nueva(M(4, 0)); // 4 m desde la referencia en 2 s: 2 m/s al norte
            Assert.AreEqual(0, r.Rumbo.Value, 0.1);
            Assert.AreEqual(2, r.Velocidad.Value, 0.05);
            r.Reiniciar();
            Assert.IsNull(r.Rumbo);
        }

        [Test]
        public void Rumbo_MismaHoraORelojAtras_TomaElRumboYDejaLaVelocidadAnterior()
        {
            reloj = Ahora;
            var r = new ReglaRumbo(() => reloj);
            r.Nueva(M(0, 0));
            r.Nueva(M(5, 0)); // misma hora
            Assert.AreEqual(0, r.Rumbo.Value, 0.1);
            Assert.IsNull(r.Velocidad, "Sin tiempo no hay velocidad (nunca infinito).");
            reloj = reloj.AddSeconds(1);
            r.Nueva(M(5, 5)); // 5 m/s al este
            Assert.AreEqual(5, r.Velocidad.Value, 0.05);
            reloj = reloj.AddSeconds(-3);
            r.Nueva(M(5, 10)); // reloj hacia atrás
            Assert.AreEqual(90, r.Rumbo.Value, 0.1);
            Assert.AreEqual(5, r.Velocidad.Value, 0.05, "Se queda la anterior.");
            Assert.IsFalse(double.IsInfinity(r.Velocidad.Value));
        }

        [Test]
        public void Rumbo_Entre_PuntosCardinales()
        {
            Assert.AreEqual(0, ReglaRumbo.RumboEntre(M(0, 0), M(100, 0)), 0.1);
            Assert.AreEqual(90, ReglaRumbo.RumboEntre(M(0, 0), M(0, 100)), 0.1);
            Assert.AreEqual(180, ReglaRumbo.RumboEntre(M(0, 0), M(-100, 0)), 0.1);
            Assert.AreEqual(270, ReglaRumbo.RumboEntre(M(0, 0), M(0, -100)), 0.1);
            Assert.AreEqual(45, ReglaRumbo.RumboEntre(M(0, 0), M(100, 100)), 0.2);
        }

        [Test]
        public void Rumbo_DeLaRuta_PorTramoYSaltandoLosVacios()
        {
            var puntos = new[] { M(0, 0), M(0, 0), M(100, 0), M(100, 100) };
            Assert.AreEqual(0, ReglaRumbo.RumboRuta(puntos, 0).Value, 0.1, "El tramo vacío se salta.");
            Assert.AreEqual(90, ReglaRumbo.RumboRuta(puntos, 2).Value, 0.1);
            Assert.IsNull(ReglaRumbo.RumboRuta(puntos, 3));
            Assert.IsNull(ReglaRumbo.RumboRuta(null, 0));
        }

        [Test]
        public void Rumbo_Elegir_APieBrujulaYSinEllaLaMarcha_EnCocheLaMarcha_SinNadaLaRuta()
        {
            Assert.AreEqual(200, ReglaRumbo.Elegir(ModoViaje.APie, 200, 90, 0));
            Assert.AreEqual(90, ReglaRumbo.Elegir(ModoViaje.APie, null, 90, 0));
            Assert.AreEqual(90, ReglaRumbo.Elegir(ModoViaje.Coche, 200, 90, 0), "En coche no cuenta la brújula.");
            Assert.AreEqual(30, ReglaRumbo.Elegir(ModoViaje.Coche, 200, null, 30));
            Assert.IsNull(ReglaRumbo.Elegir(ModoViaje.Coche, null, null, null));
        }

        [Test]
        public void LosParametrosEstanEnParametrosNegocio()
        {
            Assert.AreEqual(3.0, ParametrosNegocio.RumboDistanciaMinimaMetros);
            Assert.AreEqual(1.0, ParametrosNegocio.RumboVelocidadMinima);
            Assert.AreEqual(0.9, ParametrosNegocio.CamaraAcercarSegundos);
            Assert.Less(ParametrosNegocio.CamaraAcercarSegundos, 1.0, "Menos de 1 s.");
            Assert.Greater(ParametrosNegocio.CamaraAcercarSegundos, ParametrosNegocio.CamaraTransicionSegundos, "La cámara ha llegado.");
            Assert.AreEqual(45f, ParametrosNegocio.CamaraSeguirInclinacion);
            Assert.AreEqual(350f, ParametrosNegocio.CamaraSeguirDistanciaCoche);
            Assert.AreEqual(180f, ParametrosNegocio.CamaraSeguirDistanciaPie);
            Assert.AreEqual(0.6f, ParametrosNegocio.CamaraTransicionSegundos);
            Assert.Greater(ParametrosNegocio.CamaraSeguirAlturaZona, 2f / 3f, "En el tercio de abajo.");
            Assert.Less(ParametrosNegocio.CamaraSeguirAlturaZona, 1f);
        }

        // ───────────────────────── SeguimientoCamara ─────────────────────────

        [Test]
        public void Seguimiento_LlegaUnaVezSigueSeSueltaYRecentra()
        {
            reloj = Ahora;
            var s = new SeguimientoCamara(() => reloj);
            Assert.AreEqual(EstadoSeguimiento.Apagado, s.Estado);
            Assert.IsFalse(s.Soltar(), "Sin navegación no hay nada que soltar.");
            s.Recentrar();
            Assert.AreEqual(EstadoSeguimiento.Apagado, s.Estado);
            s.Empezar(alMomento: false);
            Assert.IsTrue(s.Siguiendo, "La cámara ya va al usuario.");
            Assert.IsTrue(s.Acercando);
            reloj = reloj.AddSeconds(0.8);
            Assert.IsFalse(s.Llegar());
            reloj = reloj.AddSeconds(0.1);
            Assert.IsTrue(s.Llegar());
            Assert.IsFalse(s.Acercando);
            Assert.IsTrue(s.Siguiendo);
            Assert.IsFalse(s.Llegar(), "Solo una vez.");
            Assert.IsTrue(s.Soltar());
            Assert.IsTrue(s.Suelto);
            Assert.IsFalse(s.Soltar());
            reloj = reloj.AddMinutes(5);
            Assert.IsTrue(s.Suelto, "No se vuelve a centrar solo.");
            s.Recentrar();
            Assert.IsTrue(s.Siguiendo);
            s.Terminar();
            Assert.AreEqual(EstadoSeguimiento.Apagado, s.Estado);
            s.Empezar(alMomento: true);
            Assert.IsTrue(s.Siguiendo, "Sin animaciones, sigue ya.");
            Assert.IsFalse(s.Acercando, "Y ha llegado.");
        }

        [Test]
        public void Seguimiento_SoltarPorElCamino_LlegaIgualSuelto()
        {
            reloj = Ahora;
            var s = new SeguimientoCamara(() => reloj);
            s.Empezar(alMomento: false);
            Assert.IsTrue(s.Soltar());
            Assert.IsTrue(s.Acercando);
            reloj = reloj.AddSeconds(1);
            Assert.IsTrue(s.Llegar());
            Assert.IsTrue(s.Suelto);
        }

        [Test]
        public void Seguimiento_ElRelojSaltaAtras_NoSeQuedaEsperando()
        {
            reloj = Ahora;
            var s = new SeguimientoCamara(() => reloj);
            s.Empezar(alMomento: false);
            reloj = reloj.AddHours(-1);
            Assert.IsTrue(s.Llegar());
        }

        [Test]
        public void Seguimiento_TerminarPorElCamino_YaNoLlega()
        {
            reloj = Ahora;
            var s = new SeguimientoCamara(() => reloj);
            s.Empezar(alMomento: false);
            s.Terminar();
            reloj = reloj.AddSeconds(2);
            Assert.IsFalse(s.Llegar());
        }

        // ───────────────────────── Cámara real ─────────────────────────

        GameObject objCamara, objMapa;
        double latAntes, lonAntes;

        CamaraMapa CrearCamara()
        {
            latAntes = Geo.LatitudOrigen; lonAntes = Geo.LongitudOrigen;
            Geo.LatitudOrigen = Cabildo.Latitud; Geo.LongitudOrigen = Cabildo.Longitud;
            objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = objCamara.AddComponent<CamaraMapa>();
            camara.distancia = 900f;
            camara.inclinacion = 50f;
            camara.giro = -15f;
            return camara;
        }

        void LimpiarCamara()
        {
            if (objMapa != null) UnityEngine.Object.DestroyImmediate(objMapa);
            if (objCamara != null) UnityEngine.Object.DestroyImmediate(objCamara);
            objMapa = objCamara = null;
            Geo.LatitudOrigen = latAntes; Geo.LongitudOrigen = lonAntes;
        }

        static Vector2 EnPantalla(CamaraMapa camara, Coordenada c)
        {
            var p = camara.GetComponent<Camera>().WorldToScreenPoint(Geo.ALocal(c.Latitud, c.Longitud));
            return new Vector2(p.x, p.y);
        }

        [Test]
        public void Camara_SeguirAnimado_TransicionSuaveHastaElObjetivo()
        {
            var camara = CrearCamara();
            try
            {
                var cam = camara.GetComponent<Camera>();
                var objetivo = new Vector2(cam.pixelWidth / 2f, cam.pixelHeight * 0.25f);
                var pos = M(200, 100);
                camara.Seguir(pos.Latitud, pos.Longitud, 90f, 45f, 350f, objetivo, animado: true);
                Assert.IsTrue(camara.Siguiendo);
                Assert.AreEqual(-15f, camara.giro, 1e-3f, "Animado, no salta.");
                Assert.AreEqual(900f, camara.distancia, 1e-3f);
                camara.AvanzarSeguimiento(0.3f);
                Assert.Greater(camara.giro, -15f);
                Assert.Less(camara.giro, 90f, "A medio camino.");
                camara.AvanzarSeguimiento(0.3f); // 0,6 s: al 95 %
                Assert.AreEqual(90f, camara.giro, 105f * 0.06f);
                Assert.AreEqual(350f, camara.distancia, 550f * 0.06f);
                for (int i = 0; i < 20; i++) camara.AvanzarSeguimiento(0.5f);
                Assert.AreEqual(90f, camara.giro, 0.05f);
                Assert.AreEqual(45f, camara.inclinacion, 0.05f);
                Assert.AreEqual(350f, camara.distancia, 0.05f);
                Assert.Less(Vector2.Distance(EnPantalla(camara, pos), objetivo), 1.5f, "La posición queda en el punto pedido.");

                // Una posición nueva se persigue desde donde está, sin saltar
                var otra = M(210, 100);
                var antes = camara.pivote;
                camara.Seguir(otra.Latitud, otra.Longitud, 90f, 45f, 350f, objetivo, animado: true);
                Assert.AreEqual(antes, camara.pivote);
                for (int i = 0; i < 20; i++) camara.AvanzarSeguimiento(0.5f);
                Assert.Less(Vector2.Distance(EnPantalla(camara, otra), objetivo), 1.5f);
            }
            finally { LimpiarCamara(); }
        }

        [Test]
        public void Camara_EsGesto()
        {
            var quieto = (pivote: new Vector3(10f, 0f, 20f), distancia: 350f, inclinacion: 45f, giro: 90f);
            var movido = (pivote: new Vector3(12f, 0f, 20f), quieto.distancia, quieto.inclinacion, quieto.giro);
            Assert.IsFalse(CamaraMapa.EsGesto(quieto, quieto, false, false), "Nada cambia.");
            Assert.IsFalse(CamaraMapa.EsGesto(quieto, quieto, true, true), "Nada cambia aunque el dedo se haya movido.");
            Assert.IsFalse(CamaraMapa.EsGesto(quieto, movido, false, false), "Pivote movido sin arrastre largo (un toque).");
            Assert.IsTrue(CamaraMapa.EsGesto(quieto, movido, true, false), "Arrastre largo.");
            Assert.IsTrue(CamaraMapa.EsGesto(quieto, movido, false, true), "Teclado.");
            Assert.IsTrue(CamaraMapa.EsGesto(quieto, (quieto.pivote, 300f, quieto.inclinacion, quieto.giro), false, false), "Distancia.");
            Assert.IsTrue(CamaraMapa.EsGesto(quieto, (quieto.pivote, quieto.distancia, 50f, quieto.giro), false, false), "Inclinación.");
            Assert.IsTrue(CamaraMapa.EsGesto(quieto, (quieto.pivote, quieto.distancia, quieto.inclinacion, 80f), false, false), "Giro.");
        }

        [Test]
        public void Camara_GiroPorElLadoMasCorto()
        {
            var camara = CrearCamara();
            try
            {
                camara.giro = 350f;
                var cam = camara.GetComponent<Camera>();
                camara.Seguir(Cabildo.Latitud, Cabildo.Longitud, 10f, 45f, 350f, new Vector2(cam.pixelWidth / 2f, cam.pixelHeight / 2f), true);
                camara.AvanzarSeguimiento(0.2f);
                Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(camara.giro, 0f)) < 10f, $"Pasa por el norte, no por el sur ({camara.giro}).");
            }
            finally { LimpiarCamara(); }
        }

        [Test]
        public void Camara_SeguirSinAnimar_AlInstante()
        {
            var camara = CrearCamara();
            try
            {
                var cam = camara.GetComponent<Camera>();
                var objetivo = new Vector2(cam.pixelWidth / 2f, cam.pixelHeight * 0.3f);
                var pos = M(50, -40);
                camara.Seguir(pos.Latitud, pos.Longitud, 200f, 45f, 180f, objetivo, animado: false);
                Assert.AreEqual(200f, camara.giro, 1e-3f);
                Assert.AreEqual(45f, camara.inclinacion, 1e-3f);
                Assert.AreEqual(180f, camara.distancia, 1e-3f);
                Assert.Less(Vector2.Distance(EnPantalla(camara, pos), objetivo), 1.5f);
            }
            finally { LimpiarCamara(); }
        }

        [Test]
        public void Camara_GestoDelUsuario_DejaDeSeguirYAvisa()
        {
            var camara = CrearCamara();
            try
            {
                int gestos = 0;
                camara.GestoUsuario += () => gestos++;
                var cam = camara.GetComponent<Camera>();
                camara.Seguir(Cabildo.Latitud, Cabildo.Longitud, 90f, 45f, 350f, new Vector2(cam.pixelWidth / 2f, cam.pixelHeight / 2f), true);
                camara.SimularGesto();
                Assert.AreEqual(1, gestos);
                Assert.IsFalse(camara.Siguiendo);
                float giro = camara.giro;
                camara.AvanzarSeguimiento(1f);
                Assert.AreEqual(giro, camara.giro, "Suelta, se queda donde está.");

                int toques = 0;
                camara.Tocado += _ => toques++;
                camara.Seguir(Cabildo.Latitud, Cabildo.Longitud, 90f, 45f, 350f, new Vector2(cam.pixelWidth / 2f, cam.pixelHeight / 2f), true);
                camara.SimularToque(new Vector2(10, 10));
                Assert.AreEqual(1, toques);
                Assert.IsTrue(camara.Siguiendo, "Un toque no suelta.");
                Assert.AreEqual(1, gestos);

                camara.DejarDeSeguir();
                Assert.IsFalse(camara.Siguiendo);
                Assert.AreEqual(1, gestos, "Dejar de seguir no es un gesto.");
            }
            finally { LimpiarCamara(); }
        }

        [Test]
        public void MapaVectorial_SeguirYGestoPorElPuerto()
        {
            var camara = CrearCamara();
            try
            {
                objMapa = new GameObject("Mapa de prueba");
                IMapa puerto = objMapa.AddComponent<MapaVectorial>();
                int gestos = 0;
                puerto.GestoUsuario += () => gestos++;
                var cam = camara.GetComponent<Camera>();
                // En px con origen arriba (como Proyectar): un cuarto desde abajo
                var objetivo = new PuntoPantalla(cam.pixelWidth / 2f, Screen.height - cam.pixelHeight * 0.25f, 1f);
                var pos = M(120, 80);
                puerto.Seguir(pos, 90f, 45f, 350f, objetivo, animado: false);
                Assert.AreEqual(90f, puerto.Giro, 1e-3f);
                Assert.AreEqual(350f, puerto.DistanciaCamara, 1e-3f);
                var p = puerto.Proyectar(pos);
                Assert.Less(Vector2.Distance(new Vector2(p.X, p.Y), new Vector2(objetivo.X, objetivo.Y)), 1.5f);
                Assert.IsTrue(camara.Siguiendo);
                camara.SimularGesto();
                Assert.AreEqual(1, gestos);
                puerto.Seguir(pos, 90f, 45f, 350f, objetivo, animado: true);
                puerto.DejarDeSeguir();
                Assert.IsFalse(camara.Siguiendo);
            }
            finally { LimpiarCamara(); }
        }

        // ───────────────────────── Falsos ─────────────────────────

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

        static Task Nunca(TimeSpan duracion, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            ct.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
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

        class PantallaFalsa : IPantallaSistema
        {
            public readonly List<bool> Llamadas = new List<bool>();
            public void MantenerEncendida(bool encendida) => Llamadas.Add(encendida);
        }

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
            public Resultado<bool> IniciarRuta(string texto) { RutaActiva = true; return Resultado<bool>.Exito(true); }
            public void ActualizarNotificacion(string texto) { }
            public void TerminarRuta() => RutaActiva = false;
            public void Mover(Coordenada c) { Posicion = c; Cambiado?.Invoke(); }
        }

        /// <summary>Mapa falso que anota lo que se le pide a la cámara y deja lanzar un gesto del usuario.</summary>
        class MapaFalso : IMapa
        {
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public int acercados;
            public void Acercar() => acercados++;
            public void Alejar() { }
            public readonly HashSet<Coordenada> proyectadas = new HashSet<Coordenada>();
            public PuntoPantalla Proyectar(Coordenada punto) { proyectadas.Add(punto); return PuntoPantalla.Ninguno; }
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float giro;
            public float Giro => giro;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque;
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public int encuadres;
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) => encuadres++;

            public readonly List<(Coordenada posicion, float giro, float inclinacion, float distancia, PuntoPantalla enPantalla, bool animado)> seguidos =
                new List<(Coordenada, float, float, float, PuntoPantalla, bool)>();
            public int dejados;
            public void Seguir(Coordenada posicion, float giro, float inclinacion, float distancia, PuntoPantalla enPantalla, bool animado) =>
                seguidos.Add((posicion, giro, inclinacion, distancia, enPantalla, animado));
            public void DejarDeSeguir() => dejados++;
            public event Action GestoUsuario;
            public void Gesto() => GestoUsuario?.Invoke();
            public void Tocar(PuntoPantalla p) => Toque?.Invoke(p);
        }

        // ───────────────────────── Pantalla (matriz) ─────────────────────────

        SynchronizationContext contextoAntes;
        RutasFalsas valhalla;
        UbicacionFalsa gps;
        ServicioUbicacion ubicacion;
        ServicioRutas rutas;
        ServicioNavegacion nav;
        AccesibilidadFalsa sistema;
        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        VisualElement arbol;
        float giroMapa; // el del mapa antes de Iniciar

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones corren al responder
            Textos.Forzados = T;
            reloj = Ahora;
            giroMapa = 0f;
            valhalla = new RutasFalsas();
            gps = new UbicacionFalsa { Posicion = M(0, 0) };
            ubicacion = new ServicioUbicacion(gps, new PreferenciasFalsas());
            rutas = new ServicioRutas(valhalla, null, Nunca, () => reloj);
            nav = new ServicioNavegacion(ubicacion, rutas, Nunca, () => reloj);
            sistema = new AccesibilidadFalsa();
        }

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = null;
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
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

        void CrearPantalla()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            prefs.Guardar(ServicioUbicacion.ClaveExplicacion, "1");
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso { giro = giroMapa };
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, ubicacion, sistema, rutas: rutas, navegacion: nav);
            pantalla.RelojCamara = () => reloj;
            tema.Iniciar();
            filtro.Iniciar();
        }

        /// <summary>Ficha de Clínica Areas, Ir en ese modo, la ruta llega e Iniciar: la cámara va al usuario.</summary>
        void IrAAreas(ModoViaje modo = ModoViaje.Coche)
        {
            CrearPantalla();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.Tocar(modo);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.De(modo).Responder(RutaEle());
            pantalla.IniciarRuta(0);
            Assert.AreEqual(!sistema.AnimacionesQuitadas, pantalla.AcercandoCamara);
        }

        /// <summary>La cámara llega al usuario y pasa un fotograma: empieza la navegación.</summary>
        void PasarEspera()
        {
            reloj = reloj.AddSeconds(1);
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.IsTrue(pantalla.Navegando);
        }

        /// <summary>Se mueve a esa posición pasados esos segundos desde la anterior.</summary>
        void Ir(Coordenada c, double segundos = 1)
        {
            reloj = reloj.AddSeconds(segundos);
            gps.Mover(c);
        }

        BotonRedondo BotonUbicacion => pantalla.BotonUbicacion;

        [Test]
        public void Pantalla_Iniciar_LaCamaraVaAlUsuarioA45Y350mYAlLlegarEmpieza()
        {
            giroMapa = 77f;
            IrAAreas();
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.AreEqual(1, mapa.encuadres, "Solo el de «Cómo llegar».");
            Assert.IsNotEmpty(mapa.seguidos, "La cámara se acerca ya.");
            Assert.AreEqual(350f, mapa.seguidos.Last().distancia);
            Assert.AreEqual(0f, mapa.seguidos.Last().giro, 0.1f, "El rumbo del principio de la ruta.");
            Assert.IsTrue(mapa.seguidos.Last().animado);
            reloj = reloj.AddSeconds(0.8);
            pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(nav.Activa, "Aún no ha llegado: ni navegación ni tarjetas.");
            Assert.IsFalse(pantalla.TarjetasRutaVisibles);
            Assert.AreEqual(DisplayStyle.None, pantalla.TarjetaManiobra.style.display.value);
            Assert.AreEqual(DisplayStyle.None, pantalla.TarjetaRuta.style.display.value);
            reloj = reloj.AddSeconds(0.1);
            pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(pantalla.Navegando);
            Assert.AreEqual(DisplayStyle.Flex, pantalla.TarjetaManiobra.style.display.value);
            Assert.AreSame(pantalla.TarjetaManiobra, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual(1, mapa.encuadres, "Y no se vuelve a encuadrar la ruta.");
            var s = mapa.seguidos.Last();
            Assert.AreEqual(45f, s.inclinacion);
            Assert.AreEqual(350f, s.distancia);
            Assert.IsTrue(s.animado, "Transición suave.");
            Assert.AreEqual(0f, s.giro, 0.1f, "Sin rumbo de la marcha aún, el de la ruta (al norte).");
            Assert.Less(s.posicion.DistanciaMetros(M(0, 0)), 0.5);
            // En el tercio de abajo de la zona libre, centrado
            var zona = PantallaMapa.ZonaEncuadre(Screen.width, Screen.height, pantalla.EscalaDp > 0 ? pantalla.EscalaDp : Escala, null, null);
            Assert.AreEqual((zona.Izquierda + zona.Derecha) / 2f, s.enPantalla.X, 1f);
            Assert.Greater(s.enPantalla.Y, zona.Arriba + (zona.Abajo - zona.Arriba) * 2f / 3f - 1f);
            Assert.Less(s.enPantalla.Y, zona.Abajo);
        }

        [Test]
        public void Pantalla_Empezar_ReducirAnimaciones_SigueAlInstanteSinTransicion()
        {
            sistema.AnimacionesQuitadas = true;
            IrAAreas();
            Assert.IsTrue(pantalla.Navegando, "Al momento.");
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            Assert.AreEqual(1, mapa.encuadres, "Solo el de «Cómo llegar».");
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.IsNotEmpty(mapa.seguidos);
            Assert.IsFalse(mapa.seguidos.Last().animado);
        }

        // En coche y en ruta el rumbo sale del punto proyectado: para ver el de la marcha distinto del de la ruta (al
        // norte), estas pruebas van hacia el este fuera de ruta (a más de 40 m), donde cuenta la posición real
        [Test]
        public void Pantalla_CocheRumbo_AlEsteA5ms_Giro90()
        {
            IrAAreas();
            PasarEspera();
            Ir(M(0, 50), 10); // fuera de ruta, al este a 5 m/s
            Ir(M(0, 55));
            Assert.AreEqual(90f, mapa.seguidos.Last().giro, 0.5f);
        }

        [Test]
        public void Pantalla_Parado_SeQuedaEn90()
        {
            IrAAreas();
            PasarEspera();
            Ir(M(0, 50), 10);
            Ir(M(0, 55));
            for (int i = 1; i <= 8; i++) Ir(M(0.55 * i, 55)); // 0,55 m/s al norte
            Assert.AreEqual(90f, mapa.seguidos.Last().giro, 0.5f);
        }

        [Test]
        public void Pantalla_CocheParadoConRuidoDelGps_ElGiroSigueSiendoElDeLaRuta()
        {
            IrAAreas();
            PasarEspera();
            Ir(M(150, 0), 30); // 5 m/s al norte por la ruta
            var ruido = new[] { M(150, 5), M(150, -5), M(155, 0), M(145, 0), M(150, 4), M(146, -3), M(154, 5), M(150, -5) };
            foreach (var p in ruido)
            {
                Ir(p);
                Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(mapa.seguidos.Last().giro, 0f)), 0.5f, $"Gira con el ruido en {p}.");
            }
        }

        [Test]
        public void Pantalla_APieParado_LaCamaraSigueALaBrujula()
        {
            gps.Rumbo = 100f;
            IrAAreas(ModoViaje.APie);
            PasarEspera();
            Assert.AreEqual(100f, mapa.seguidos.Last().giro, 0.01f);
            int antes = mapa.seguidos.Count;
            gps.Rumbo = 101.5f; // menos de 2°
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(antes, mapa.seguidos.Count, "Con menos de 2°, no se recoloca.");
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(antes, mapa.seguidos.Count, "Ni en cada fotograma.");
            gps.Rumbo = 130f; // sin posición nueva
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(antes + 1, mapa.seguidos.Count);
            Assert.AreEqual(130f, mapa.seguidos.Last().giro, 0.01f);
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(antes + 1, mapa.seguidos.Count, "Sin cambio, no se vuelve a pedir.");
        }

        [Test]
        public void Pantalla_FichaNavegandoYSiguiendo_NoSigueYAlCerrarlaVuelveSinEncuadrar()
        {
            IrAAreas();
            PasarEspera();
            int dejados = mapa.dejados, encuadres = mapa.encuadres, seguidos = mapa.seguidos.Count;
            pantalla.AbrirFicha(Areas);
            Assert.Greater(mapa.dejados, dejados, "Con la ficha abierta, la cámara no sigue.");
            pantalla.CerrarFicha();
            Assert.Greater(mapa.seguidos.Count, seguidos, "Al cerrarla, vuelve a seguir.");
            Assert.AreEqual(encuadres, mapa.encuadres, "Y no se encuadra la ruta.");
        }

        [Test]
        public void Pantalla_FichaConLaCamaraSuelta_AlCerrarlaNiSigueNiEncuadra()
        {
            IrAAreas();
            PasarEspera();
            mapa.Gesto();
            int encuadres = mapa.encuadres, seguidos = mapa.seguidos.Count;
            pantalla.AbrirFicha(Areas);
            pantalla.CerrarFicha();
            Assert.AreEqual(seguidos, mapa.seguidos.Count);
            Assert.AreEqual(encuadres, mapa.encuadres);
            Assert.AreEqual(EstadoSeguimiento.Suelto, pantalla.EstadoCamara);
        }

        [Test]
        public void Pantalla_ElMarcadorDePosicionRecibeElPuntoQueSeVe()
        {
            IrAAreas();
            PasarEspera();
            Ir(M(150, 8), 30);
            mapa.proyectadas.Clear();
            pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(mapa.proyectadas.Contains(nav.Estado.Proyectado), "En coche y en ruta, el proyectado.");
            Assert.IsFalse(mapa.proyectadas.Contains(M(150, 8)), "No la posición real.");
        }

        [Test]
        public void Pantalla_ReducirAnimacionesDespuesDeIniciar_IrSigueAlInstante()
        {
            CrearPantalla();
            sistema.AnimacionesQuitadas = true;
            pantalla.LeerAccesibilidadSistema();
            pantalla.AbrirFicha(Areas);
            pantalla.Ficha.Acciones.PulsarIr();
            valhalla.De(ModoViaje.Coche).Responder(RutaEle());
            pantalla.IniciarRuta(0);
            Assert.IsTrue(pantalla.Navegando, "Al momento.");
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.IsNotEmpty(mapa.seguidos);
            Assert.IsFalse(mapa.seguidos.Last().animado);
        }

        [Test]
        public void Pantalla_APie_Brujula200_Giro200A180m()
        {
            gps.Rumbo = 200f;
            IrAAreas(ModoViaje.APie);
            Assert.IsTrue(pantalla.AcercandoCamara);
            Assert.AreEqual(200f, mapa.seguidos.Last().giro, 0.01f, "Ya al acercarse.");
            Assert.AreEqual(180f, mapa.seguidos.Last().distancia);
            PasarEspera();
            Ir(M(0, 5));
            var s = mapa.seguidos.Last();
            Assert.AreEqual(200f, s.giro, 0.01f);
            Assert.AreEqual(180f, s.distancia);
        }

        [Test]
        public void Pantalla_APieSinBrujula_ElDeLaMarcha()
        {
            gps.Rumbo = null;
            IrAAreas(ModoViaje.APie);
            PasarEspera();
            Ir(M(0, 5));
            Assert.AreEqual(90f, mapa.seguidos.Last().giro, 0.5f);
        }

        [Test]
        public void Pantalla_CocheEnRuta_PuntoYCamaraSobreLaLinea_FueraDeRutaLaReal()
        {
            IrAAreas();
            PasarEspera();
            Ir(M(150, 8), 30);
            var proyectado = nav.Estado.Proyectado;
            Assert.Less(proyectado.DistanciaMetros(M(150, 0)), 0.5);
            Assert.AreEqual(proyectado, pantalla.PosicionMostrada, "El punto va sobre la línea.");
            Assert.AreEqual(proyectado, mapa.seguidos.Last().posicion, "La cámara sigue al punto que se ve.");
            Ir(M(150, 45));
            Assert.AreEqual(M(150, 45), pantalla.PosicionMostrada, "Fuera de ruta, la posición real.");
            Assert.AreEqual(M(150, 45), mapa.seguidos.Last().posicion);
        }

        [Test]
        public void Pantalla_APieEnRuta_PuntoEnLaPosicionReal()
        {
            IrAAreas(ModoViaje.APie);
            PasarEspera();
            Ir(M(150, 8), 30);
            Assert.AreEqual(M(150, 8), pantalla.PosicionMostrada);
            Assert.AreEqual(M(150, 8), mapa.seguidos.Last().posicion);
        }

        [Test]
        public void Pantalla_Arrastre_DejaDeSeguirYElBotonPasaAVolverACentrar()
        {
            IrAAreas();
            PasarEspera();
            Assert.AreEqual(Icono.Navegar, BotonUbicacion.Icono);
            Assert.AreEqual(ClavesTexto.UbicacionSiguiendo, pantalla.ClaveBotonUbicacion());
            mapa.Tocar(new PuntoPantalla(10, 10, 1));
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara, "Un toque no suelta.");
            mapa.Gesto();
            Assert.AreEqual(EstadoSeguimiento.Suelto, pantalla.EstadoCamara);
            Assert.AreEqual(1, mapa.dejados);
            Assert.AreEqual(Icono.Recentrar, BotonUbicacion.Icono);
            Assert.AreEqual(ClavesTexto.UbicacionRecentrar, pantalla.ClaveBotonUbicacion());
            int antes = mapa.seguidos.Count;
            Ir(M(100, 0));
            reloj = reloj.AddMinutes(1);
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(antes, mapa.seguidos.Count, "No se vuelve a centrar solo.");
        }

        [Test]
        public void Pantalla_GestoPorElCamino_EmpiezaIgualConLaCamaraSuelta()
        {
            IrAAreas();
            int seguidos = mapa.seguidos.Count;
            mapa.Gesto();
            Assert.AreEqual(EstadoSeguimiento.Suelto, pantalla.EstadoCamara);
            Assert.AreEqual(Icono.Recentrar, BotonUbicacion.Icono);
            reloj = reloj.AddSeconds(1);
            pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(pantalla.Navegando, "Empieza igual.");
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            Assert.AreEqual(EstadoSeguimiento.Suelto, pantalla.EstadoCamara);
            Assert.AreEqual(seguidos, mapa.seguidos.Count, "Sin recolocar.");
        }

        [Test]
        public void Pantalla_AtrasPorElCamino_QuitaLaRutaYNoEmpieza()
        {
            IrAAreas();
            int dejados = mapa.dejados;
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.RutaActiva);
            Assert.Greater(mapa.dejados, dejados);
            Assert.AreEqual(EstadoSeguimiento.Apagado, pantalla.EstadoCamara);
            reloj = reloj.AddSeconds(2);
            pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(nav.Activa);
            Assert.IsTrue(pantalla.Buscador.Visible);
        }

        [Test]
        public void Pantalla_FichaPorElCamino_EmpiezaDebajoYAlCerrarlaSigue()
        {
            IrAAreas();
            pantalla.AbrirFicha(Areas);
            reloj = reloj.AddSeconds(1);
            pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(nav.Activa);
            Assert.IsFalse(pantalla.TarjetasRutaVisibles);
            int seguidos = mapa.seguidos.Count;
            pantalla.CerrarFicha();
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            Assert.Greater(mapa.seguidos.Count, seguidos);
        }

        [Test]
        public void Pantalla_BotonDeZoom_Suelta()
        {
            IrAAreas();
            PasarEspera();
            arbol.Q<BotonRedondo>("boton-acercar").Pulsar();
            Assert.AreEqual(1, mapa.acercados);
            Assert.AreEqual(EstadoSeguimiento.Suelto, pantalla.EstadoCamara);
            Assert.AreEqual(Icono.Recentrar, BotonUbicacion.Icono);
        }

        [Test]
        public void Pantalla_Recentrar_SigueOtraVezYElBotonVuelveASuIcono()
        {
            IrAAreas();
            PasarEspera();
            mapa.Gesto();
            int antes = mapa.seguidos.Count;
            BotonUbicacion.Pulsar();
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.AreEqual(antes + 1, mapa.seguidos.Count, "Vuelve a la posición ya.");
            Assert.AreEqual(Icono.Navegar, BotonUbicacion.Icono);
            Assert.AreEqual(ClavesTexto.UbicacionSiguiendo, pantalla.ClaveBotonUbicacion());
        }

        [Test]
        public void Pantalla_RecentrarPorElCamino_SigueOtraVez()
        {
            IrAAreas();
            mapa.Gesto();
            int seguidos = mapa.seguidos.Count;
            BotonUbicacion.Pulsar();
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.Greater(mapa.seguidos.Count, seguidos);
        }

        [Test]
        public void Pantalla_TerminarConLaX_NoSigueYBotonDeSiempre()
        {
            IrAAreas();
            PasarEspera();
            mapa.Gesto();
            int dejados = mapa.dejados;
            pantalla.TarjetaRuta.Desplegar();
            pantalla.TarjetaRuta.PulsarFinalizar();
            Assert.AreEqual(EstadoSeguimiento.Apagado, pantalla.EstadoCamara);
            Assert.Greater(mapa.dejados, dejados);
            Assert.AreEqual(Icono.Navegar, BotonUbicacion.Icono);
            Assert.AreNotEqual(ClavesTexto.UbicacionRecentrar, pantalla.ClaveBotonUbicacion());
            Assert.AreNotEqual(ClavesTexto.UbicacionSiguiendo, pantalla.ClaveBotonUbicacion());
            int seguidos = mapa.seguidos.Count;
            gps.Mover(M(10, 0));
            pantalla.ActualizarCapas(Escala);
            Assert.AreEqual(seguidos, mapa.seguidos.Count);
        }

        [Test]
        public void Pantalla_TerminarConAtras_NoSigue()
        {
            IrAAreas();
            PasarEspera();
            Assert.IsTrue(pantalla.Atras());
            Assert.AreEqual(EstadoSeguimiento.Apagado, pantalla.EstadoCamara);
            Assert.AreEqual(Icono.Navegar, BotonUbicacion.Icono);
            Assert.AreEqual(PantallaMapa.ClaveBotonUbicacion(pantalla.AccionUbicacionActual()), pantalla.ClaveBotonUbicacion());
        }

        [Test]
        public void Pantalla_OtraRuta_LaCamaraVaOtraVezAlUsuario()
        {
            IrAAreas();
            PasarEspera();
            mapa.Gesto();
            pantalla.ReabrirFichaRuta();
            pantalla.Ficha.Acciones.PulsarIr(); // las rutas salen de la caché al momento
            Assert.IsTrue(pantalla.Navegando, "Abrir «Cómo llegar» no termina la de antes.");
            mapa.Gesto(); // mover el mapa con la hoja no cambia la cámara de la navegación
            pantalla.IniciarRuta(0);
            Assert.IsFalse(nav.Activa, "«Iniciar» la termina.");
            Assert.IsTrue(pantalla.AcercandoCamara);
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara);
            Assert.AreEqual(Icono.Navegar, BotonUbicacion.Icono);
            PasarEspera();
        }

        [Test]
        public void Pantalla_TalkBack_SiguiendoTuPosicionYVolverACentrar()
        {
            IrAAreas();
            PasarEspera();
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.Contains("Siguiendo tu posición", recorrido, string.Join(" | ", recorrido));
            mapa.Gesto();
            recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.Contains("Volver a centrar", recorrido, string.Join(" | ", recorrido));
            Assert.IsFalse(recorrido.Contains("Siguiendo tu posición"));
        }

        [Test]
        public void Pantalla_SinNavegar_ElBotonDeUbicacionComoSiempre()
        {
            CrearPantalla();
            Assert.AreEqual(EstadoSeguimiento.Apagado, pantalla.EstadoCamara);
            Assert.AreEqual(PantallaMapa.ClaveBotonUbicacion(pantalla.AccionUbicacionActual()), pantalla.ClaveBotonUbicacion());
            Assert.AreEqual(M(0, 0), pantalla.PosicionMostrada);
            mapa.Gesto();
            Assert.AreEqual(EstadoSeguimiento.Apagado, pantalla.EstadoCamara);
            Assert.AreEqual(Icono.Navegar, BotonUbicacion.Icono);
        }

        // ───────────────────────── Pantalla encendida ─────────────────────────

        [Test]
        public void PantallaEncendida_MientrasSeNavega()
        {
            var falsa = new PantallaFalsa();
            _ = new PantallaEncendida(nav, falsa);
            nav.Empezar(RutaEle(), Destino, "Clínica Areas", ModoViaje.Coche);
            CollectionAssert.AreEqual(new[] { true }, falsa.Llamadas);
            nav.Terminar();
            CollectionAssert.AreEqual(new[] { true, false }, falsa.Llamadas, "Al terminar, el valor del sistema.");
        }

        [Test]
        public void ElArranqueConectaLaPantallaEncendida()
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
                var prefs = new PreferenciasFalsas();
                var falsa = new PantallaFalsa();
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), conUi, new ServicioTema(prefs, new SistemaClaro()), Lugares(),
                    new ServicioFiltro(prefs), Ahora, ubicacion, rutas: rutas, navegacion: nav, pantallaSistema: falsa));
                nav.Empezar(RutaEle(), Destino, "Clínica Areas", ModoViaje.Coche);
                CollectionAssert.AreEqual(new[] { true }, falsa.Llamadas);
                nav.Terminar();
                CollectionAssert.AreEqual(new[] { true, false }, falsa.Llamadas);
            }
            finally { UnityEngine.Object.DestroyImmediate(conInterfaz); }
        }

        [Test]
        public void LosTextosEstanEnLaTabla()
        {
            foreach (var clave in new[] { ClavesTexto.UbicacionRecentrar, ClavesTexto.UbicacionSiguiendo })
            {
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
                Assert.IsTrue(PrepararInterfaz.TextosEs.ContainsKey(clave), clave);
            }
            Assert.AreEqual("Volver a centrar", T(ClavesTexto.UbicacionRecentrar));
            Assert.AreEqual("Siguiendo tu posición", T(ClavesTexto.UbicacionSiguiendo));
        }
    }
}
