using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    /// Ruta aproximada sin conexión (historia 2.6, parte 2b): el grafo de calles (cruces sin vértice, modos, lejos, sin
    /// unir, tiempo), la lectura de teselas MVT de prueba escritas en una carpeta temporal (entre teselas, path, sin
    /// teselas, cancelación, rectángulo de 5 × 5), ServicioRutas con un tercer servicio local (sin caché) y la pantalla
    /// real (línea suave, tarjetas con «≈», toast y TalkBack), una prueba por fila de la matriz. Nada sale a la red.
    /// </summary>
    public class PruebasRutaAproximada
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
        static readonly DateTime HoraLocal = new DateTime(2026, 11, 21, 13, 30, 0, DateTimeKind.Local);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        // ───────────────────────── Grafo (Dominio) ─────────────────────────

        const double MetrosGrado = Math.PI / 180.0 * Coordenada.RadioTierraMetros;

        /// <summary>Punto a (este, norte) metros de la Plaza del Cabildo.</summary>
        static Coordenada M(double este, double norte) =>
            new Coordenada(Cabildo.Latitud + norte / MetrosGrado,
                Cabildo.Longitud + este / (MetrosGrado * Math.Cos(Cabildo.Latitud * Math.PI / 180.0)));

        long siguienteClave = 1;

        /// <summary>Calle por esos puntos en metros, cada vértice con su propia clave.</summary>
        void Calle(GrafoCalles g, params (double este, double norte)[] puntos) =>
            g.AnadirCalle(puntos.Select(p => new PuntoCalle(siguienteClave++, M(p.este, p.norte))).ToList());

        [Test]
        public void Grafo_CruceSinVertice_ElCaminoPasaPorElCruce()
        {
            var g = new GrafoCalles();
            Calle(g, (0, 0), (200, 0));
            Calle(g, (100, 3), (100, 200)); // su extremo queda a 3 m de la otra, sin vértice común
            var r = g.Calcular(M(0, -1), M(100, 201), ModoViaje.APie);
            Assert.IsNotNull(r, "El extremo a 3 m se une a la otra calle.");
            Assert.IsTrue(r.Aproximada);
            Assert.IsEmpty(r.Maniobras, "Sin maniobras.");
            Assert.AreEqual(300, r.Metros, 0.5, "100 m por una, 3 m del cruce y 197 por la otra.");
            Assert.IsTrue(r.Puntos.Any(p => p.DistanciaMetros(M(100, 0)) < 0.5), "Pasa por el cruce.");
            Assert.Less(r.Puntos[0].DistanciaMetros(M(0, 0)), 0.5, "Empieza en el punto de calle más cercano.");
            Assert.Less(r.Puntos.Last().DistanciaMetros(M(100, 200)), 0.5, "Acaba en el punto de calle más cercano.");
        }

        [Test]
        public void Grafo_ExtremoAMasDeCincoMetros_NoSeUne()
        {
            Assert.AreEqual(5.0, ParametrosNegocio.RutaAproximadaUnionMetros);
            var g = new GrafoCalles();
            Calle(g, (0, 0), (200, 0));
            Calle(g, (100, 6), (100, 200));
            Assert.IsNull(g.Calcular(M(0, -1), M(100, 201), ModoViaje.APie));
        }

        [Test]
        public void Grafo_VerticeCompartidoPorClave_UneLasCalles()
        {
            var g = new GrafoCalles();
            var cruce = new PuntoCalle(99, M(100, 0));
            g.AnadirCalle(new[] { new PuntoCalle(1, M(0, 0)), cruce });
            g.AnadirCalle(new[] { cruce, new PuntoCalle(2, M(100, 100)) });
            g.AnadirCalle(new[] { new PuntoCalle(1, M(0, 0)), cruce }); // repetida (margen de la tesela vecina): no se duplica
            Assert.AreEqual(3, g.Nodos);
            Assert.AreEqual(2, g.Aristas);
            var r = g.Calcular(M(0, 0), M(100, 100), ModoViaje.Coche);
            Assert.AreEqual(200, r.Metros, 0.5);
        }

        [Test]
        public void Grafo_ElCaminoMasCortoPorMetros()
        {
            var g = new GrafoCalles();
            Calle(g, (0, 0), (0, 200), (300, 200), (300, 0)); // rodeo de 700 m
            Calle(g, (0, 0), (100, -50), (200, -50), (300, 0)); // unos 312 m
            // las dos calles empiezan y acaban en puntos distintos: se cosen por la unión de extremos (0 m)
            var r = g.Calcular(M(-5, 0), M(305, 0), ModoViaje.Coche);
            Assert.IsNotNull(r);
            Assert.Less(r.Metros, 400, "Va por la corta.");
        }

        [Test]
        public void Grafo_LejosDeUnaCalle_SinRuta()
        {
            Assert.AreEqual(300.0, ParametrosNegocio.RutaAproximadaCalleMetros);
            var g = new GrafoCalles();
            Calle(g, (0, 0), (500, 0));
            Assert.IsNull(g.Calcular(M(10, 0), M(250, 400), ModoViaje.APie), "Destino a 400 m de la calle.");
            Assert.IsNotNull(new GrafoCaminoRecto().Grafo.Calcular(M(10, 0), M(250, 250), ModoViaje.APie), "A 250 m sí.");
        }

        /// <summary>Una calle recta de 500 m (grafo nuevo, para no arrastrar los puntos partidos de otra prueba).</summary>
        sealed class GrafoCaminoRecto
        {
            public readonly GrafoCalles Grafo = new GrafoCalles();
            public GrafoCaminoRecto() => Grafo.AnadirCalle(new[] { new PuntoCalle(1, M(0, 0)), new PuntoCalle(2, M(500, 0)) });
        }

        [Test]
        public void Grafo_CallesSinUnir_SinRuta()
        {
            var g = new GrafoCalles();
            Calle(g, (0, 0), (300, 0));
            Calle(g, (0, 50), (300, 50)); // paralela a 50 m, sin ninguna calle que las una
            Assert.IsNull(g.Calcular(M(0, -1), M(300, 51), ModoViaje.APie));
        }

        [Test]
        public void Grafo_TiempoAVelocidadDelModo()
        {
            Assert.AreEqual(30.0, ParametrosNegocio.RutaAproximadaCocheKmh);
            Assert.AreEqual(5.0, ParametrosNegocio.RutaAproximadaPieKmh);
            var coche = new GrafoCalles();
            coche.AnadirCalle(new[] { new PuntoCalle(1, M(0, 0)), new PuntoCalle(2, M(1000, 0)) });
            var rc = coche.Calcular(M(0, 0), M(1000, 0), ModoViaje.Coche);
            Assert.AreEqual(1000, rc.Metros, 0.5);
            Assert.AreEqual(120, rc.Segundos, 0.5, "1 km a 30 km/h.");
            var pie = new GrafoCalles();
            pie.AnadirCalle(new[] { new PuntoCalle(1, M(0, 0)), new PuntoCalle(2, M(1000, 0)) });
            Assert.AreEqual(720, pie.Calcular(M(0, 0), M(1000, 0), ModoViaje.APie).Segundos, 0.5, "1 km a 5 km/h.");
        }

        [Test]
        public void Grafo_CallesPorModo()
        {
            foreach (var c in new[] { "path", "track", "raceway", "busway", "bus_guideway" })
                Assert.IsFalse(GrafoCalles.Admite(c, ModoViaje.Coche), c + " en coche");
            foreach (var c in new[] { "path", "track", "minor", "service", "primary", "raceway" })
                Assert.IsTrue(GrafoCalles.Admite(c, ModoViaje.APie), c + " a pie");
            foreach (var c in new[] { "motorway", "trunk", "primary", "minor", "service" })
                Assert.IsTrue(GrafoCalles.Admite(c, ModoViaje.Coche), c + " en coche");
            foreach (var c in new[] { "motorway", "trunk" })
                Assert.IsFalse(GrafoCalles.Admite(c, ModoViaje.APie), c + " a pie");
            foreach (var c in new[] { "ferry", "rail", "transit", "aerialway", null, "", "minor_construction",
                                      "motorway_construction", "trunk_construction", "path_construction" })
                foreach (var m in new[] { ModoViaje.Coche, ModoViaje.APie })
                    Assert.IsFalse(GrafoCalles.Admite(c, m), (c ?? "null") + " " + m);
        }

        [Test]
        public void Grafo_Cancelar()
        {
            var g = new GrafoCalles();
            Calle(g, (0, 0), (100, 0));
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<OperationCanceledException>(() => g.Calcular(M(0, 0), M(100, 0), ModoViaje.Coche, cts.Token));
        }

        // ───────────────────────── Teselas guardadas (Vectorial) ─────────────────────────

        static void Varint(List<byte> b, ulong v)
        {
            while (v >= 0x80) { b.Add((byte)(v | 0x80)); v >>= 7; }
            b.Add((byte)v);
        }

        static void Campo(List<byte> b, int campo, int tipo) => Varint(b, (ulong)((campo << 3) | tipo));

        static void Bytes(List<byte> b, int campo, IList<byte> datos)
        {
            Campo(b, campo, 2);
            Varint(b, (ulong)datos.Count);
            b.AddRange(datos);
        }

        static uint ZigZag(int n) => (uint)((n << 1) ^ (n >> 31));

        /// <summary>Tesela MVT de prueba: capa transportation con una línea por calle (clase y puntos en la rejilla de 4096).</summary>
        static byte[] TeselaMvt(params (string clase, (int x, int y)[] puntos)[] calles)
        {
            var clases = calles.Select(c => c.clase).Distinct().ToList();
            var capa = new List<byte>();
            Campo(capa, 15, 0); Varint(capa, 2);
            Bytes(capa, 1, Encoding.UTF8.GetBytes("transportation"));
            foreach (var (clase, puntos) in calles)
            {
                var elemento = new List<byte>();
                var etiquetas = new List<byte>();
                Varint(etiquetas, 0);
                Varint(etiquetas, (ulong)clases.IndexOf(clase));
                Bytes(elemento, 2, etiquetas);
                Campo(elemento, 3, 0); Varint(elemento, 2); // línea
                var geo = new List<byte>();
                int x = 0, y = 0;
                for (int i = 0; i < puntos.Length; i++)
                {
                    if (i == 0) Varint(geo, (1 << 3) | 1);
                    if (i == 1) Varint(geo, ((ulong)(puntos.Length - 1) << 3) | 2);
                    Varint(geo, ZigZag(puntos[i].x - x));
                    Varint(geo, ZigZag(puntos[i].y - y));
                    x = puntos[i].x; y = puntos[i].y;
                }
                Bytes(elemento, 4, geo);
                Bytes(capa, 2, elemento);
            }
            Bytes(capa, 3, Encoding.UTF8.GetBytes("class"));
            foreach (var c in clases)
            {
                var valor = new List<byte>();
                Bytes(valor, 1, Encoding.UTF8.GetBytes(c));
                Bytes(capa, 4, valor);
            }
            Campo(capa, 5, 0); Varint(capa, 4096);
            var tesela = new List<byte>();
            Bytes(tesela, 3, capa);
            return tesela.ToArray();
        }

        static (int x, int y) TeselaCabildo => RutasCallesGuardadas.Tesela(Cabildo);

        /// <summary>Coordenada de un punto (px, py) de la rejilla de la tesela (dx, dy) respecto a la del Cabildo.</summary>
        static Coordenada EnTesela(int dx, int dy, double px, double py, int zoom = 14)
        {
            var (tx, ty) = RutasCallesGuardadas.Tesela(Cabildo, zoom);
            return RutasCallesGuardadas.ACoordenada((tx + dx) * 4096.0 + px, (ty + dy) * 4096.0 + py, zoom);
        }

        string carpeta;

        [SetUp]
        public void Preparar()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "PriceMapPruebas", Guid.NewGuid().ToString("N"));
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            Textos.Forzados = T;
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            anuncios.Clear();
            valhalla = new RutasFalsas();
            osrm = new RutasFalsas();
            local = new RutasFalsas();
            esperas = new Esperas();
            servicio = new ServicioRutas(valhalla, osrm, esperas.Esperar, () => Ahora, local);
        }

        [TearDown]
        public void Limpiar()
        {
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = null;
            AccesibilidadPantalla.AnuncioPedido -= Anotar;
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        void Guardar(int dx, int dy, byte[] datos, int zoom = 14)
        {
            var (tx, ty) = RutasCallesGuardadas.Tesela(Cabildo, zoom);
            var dir = Path.Combine(carpeta, "vectorial", zoom.ToString(), (tx + dx).ToString());
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, (ty + dy) + ".pbf"), datos);
        }

        [Test]
        public void Teselas_PorLasCallesDeDosTeselasVecinas()
        {
            // Una calle de x = 1000 a x = 6000 (rejilla global desde la tesela del Cabildo) partida en el borde: cada
            // tesela la corta en su margen de 64, sin vértice común; la unión de extremos sueltos las cose
            Guardar(0, 0, TeselaMvt(("minor", new[] { (1000, 2000), (4000, 2000), (4160, 2000) })));
            Guardar(1, 0, TeselaMvt(("minor", new[] { (-64, 2000), (104, 2000), (1904, 2000) })));
            var rutas = new RutasCallesGuardadas(carpeta);
            var r = rutas.Calcular(EnTesela(0, 0, 1000, 2020), EnTesela(1, 0, 1904, 2020), ModoViaje.Coche, CancellationToken.None).Result;
            Assert.IsTrue(r.Ok, r.Error);
            Assert.IsTrue(r.Valor.Aproximada);
            double esperado = EnTesela(0, 0, 1000, 2000).DistanciaMetros(EnTesela(1, 0, 1904, 2000));
            Assert.AreEqual(esperado, r.Valor.Metros, esperado * 0.01, "Por la calle entera, de una tesela a la otra.");
            Assert.AreEqual(r.Valor.Metros / (30 / 3.6), r.Valor.Segundos, 0.5);
            Assert.Less(r.Valor.Puntos[0].DistanciaMetros(EnTesela(0, 0, 1000, 2000)), 1.0);
        }

        [Test]
        public void Teselas_PathAPieSiYEnCocheNo()
        {
            // De P (1000, 1000) a Q (3000, 1000): rodeo por calles (6000 px) o atajo por un path (2000 px)
            Guardar(0, 0, TeselaMvt(
                ("minor", new[] { (1000, 1000), (1000, 3000), (3000, 3000), (3000, 1000) }),
                ("path", new[] { (1000, 1000), (3000, 1000) })));
            var rutas = new RutasCallesGuardadas(carpeta);
            var o = EnTesela(0, 0, 990, 1000);
            var d = EnTesela(0, 0, 3010, 1000);
            var pie = rutas.CalcularAhora(o, d, ModoViaje.APie, CancellationToken.None);
            var coche = rutas.CalcularAhora(o, d, ModoViaje.Coche, CancellationToken.None);
            Assert.IsTrue(pie.Ok && coche.Ok, pie.Error + " / " + coche.Error);
            double px = EnTesela(0, 0, 1000, 1000).DistanciaMetros(EnTesela(0, 0, 3000, 1000)) / 2000;
            Assert.AreEqual(2000 * px, pie.Valor.Metros, 2, "A pie, por el path.");
            Assert.AreEqual(6000 * px, coche.Valor.Metros, 6, "En coche, por las calles.");
            Assert.AreEqual(pie.Valor.Metros / (5 / 3.6), pie.Valor.Segundos, 0.5);
        }

        [Test]
        public void Teselas_LejosDeLasCallesOSinTeselas_Fallo()
        {
            var rutas = new RutasCallesGuardadas(carpeta);
            var sin = rutas.CalcularAhora(EnTesela(0, 0, 1000, 1000), EnTesela(0, 0, 2000, 1000), ModoViaje.Coche, CancellationToken.None);
            Assert.IsFalse(sin.Ok, "Sin teselas guardadas.");

            Guardar(0, 0, TeselaMvt(("minor", new[] { (1000, 1000), (3000, 1000) })));
            // 840 px son unos 400 m de la calle
            var lejos = rutas.CalcularAhora(EnTesela(0, 0, 1000, 1000), EnTesela(0, 0, 2000, 1840), ModoViaje.Coche, CancellationToken.None);
            Assert.IsFalse(lejos.Ok, "Destino a unos 400 m de cualquier calle.");
            Assert.IsNull(lejos.Valor);
        }

        [Test]
        public void Teselas_ConElZoomDelMapa()
        {
            // El mapa guarda a zoom 13: con ese zoom se encuentran sus teselas; con el de por defecto (14), no
            Guardar(0, 0, TeselaMvt(("minor", new[] { (1000, 1000), (3000, 1000) })), zoom: 13);
            var o = EnTesela(0, 0, 1000, 1010, 13);
            var d = EnTesela(0, 0, 3000, 1010, 13);
            var trece = new RutasCallesGuardadas(carpeta, 13);
            Assert.AreEqual(13, trece.ZoomTeselas);
            var r = trece.CalcularAhora(o, d, ModoViaje.Coche, CancellationToken.None);
            Assert.IsTrue(r.Ok, r.Error);
            double esperado = EnTesela(0, 0, 1000, 1000, 13).DistanciaMetros(EnTesela(0, 0, 3000, 1000, 13));
            Assert.AreEqual(esperado, r.Valor.Metros, esperado * 0.01, "La rejilla global es la del zoom 13.");
            Assert.IsFalse(new RutasCallesGuardadas(carpeta).CalcularAhora(o, d, ModoViaje.Coche, CancellationToken.None).Ok);
        }

        [Test]
        public void Teselas_Cancelar()
        {
            Guardar(0, 0, TeselaMvt(("minor", new[] { (1000, 1000), (3000, 1000) })));
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var t = new RutasCallesGuardadas(carpeta).Calcular(EnTesela(0, 0, 1000, 1000), EnTesela(0, 0, 3000, 1000), ModoViaje.Coche, cts.Token);
            Assert.That(() => t.GetAwaiter().GetResult(), Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public void Teselas_RectanguloConMargenYComoMuchoCincoPorCinco()
        {
            Assert.AreEqual(5, ParametrosNegocio.RutaAproximadaTeselasLado);
            var (tx, ty) = TeselaCabildo;
            Assert.AreEqual((tx - 1, ty - 1, tx + 1, ty + 1), RutasCallesGuardadas.Teselas(EnTesela(0, 0, 100, 100), EnTesela(0, 0, 4000, 4000)),
                "Una tesela de margen.");
            var ancho = RutasCallesGuardadas.Teselas(EnTesela(0, 0, 100, 100), EnTesela(4, 0, 100, 100)).Value;
            Assert.AreEqual(5, ancho.x1 - ancho.x0 + 1, "Recortado a 5.");
            Assert.LessOrEqual(ancho.x0, tx);
            Assert.GreaterOrEqual(ancho.x1, tx + 4);
            Assert.AreEqual(3, ancho.y1 - ancho.y0 + 1);
            Assert.IsNull(RutasCallesGuardadas.Teselas(EnTesela(0, 0, 100, 100), EnTesela(5, 0, 100, 100)), "A 6 teselas, demasiado lejos.");
            Assert.AreEqual((tx + 2, ty + 3), RutasCallesGuardadas.Tesela(EnTesela(2, 3, 2048, 2048)), "La fórmula de teselas va y vuelve.");
        }

        // ───────────────────────── Servicio (Aplicacion) ─────────────────────────

        class Peticion
        {
            public Coordenada Origen, Destino;
            public ModoViaje Modo;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<Ruta>> Tcs;

            public void Responder(double segundos, double metros = 1000, bool aproximada = false) =>
                Tcs.TrySetResult(Resultado<Ruta>.Exito(new Ruta(
                    new[] { Origen, new Coordenada((Origen.Latitud + Destino.Latitud) / 2, Origen.Longitud), Destino },
                    metros, segundos, null, aproximada)));

            public void Fallar(string error = "sin conexión") => Tcs.TrySetResult(Resultado<Ruta>.Fallo(error));
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
            public Task Esperar(TimeSpan duracion, CancellationToken ct)
            {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            }
        }

        RutasFalsas valhalla, osrm, local;
        Esperas esperas;
        ServicioRutas servicio;
        SynchronizationContext contextoAntes;

        /// <summary>Valhalla y OSRM caen para ese modo (el local queda pidiéndose).</summary>
        void CaenLosDos(ModoViaje modo)
        {
            valhalla.De(modo).Fallar();
            osrm.De(modo).Fallar();
        }

        [Test]
        public void Servicio_LosDosCaen_LaAproximadaSinGuardarYVuelveLaRed()
        {
            Assert.IsTrue(servicio.ConRutaAproximada);
            var t = servicio.Calcular(Cabildo, M(300, 300), ModoViaje.Coche, CancellationToken.None);
            Assert.IsEmpty(local.Peticiones, "El local solo si fallan los dos.");
            CaenLosDos(ModoViaje.Coche);
            Assert.AreEqual(1, local.Peticiones.Count);
            local.De(ModoViaje.Coche).Responder(240, 1200, aproximada: true);
            Assert.IsTrue(t.Result.Ok);
            Assert.IsTrue(t.Result.Valor.Aproximada);
            Assert.AreEqual(0, servicio.EnCache, "La aproximada no va a la caché.");

            // Vuelve la red: la siguiente va a Valhalla, que responde
            var t2 = servicio.Calcular(Cabildo, M(300, 300), ModoViaje.Coche, CancellationToken.None);
            Assert.AreEqual(2, valhalla.Peticiones.Count, "No estaba en la caché.");
            valhalla.De(ModoViaje.Coche).Responder(200, 1100);
            Assert.IsFalse(t2.Result.Valor.Aproximada);
            Assert.AreEqual(1, servicio.EnCache);
            Assert.AreEqual(1, local.Peticiones.Count);
        }

        [Test]
        public void Servicio_ConConexion_NoSePideLaAproximada()
        {
            var t = servicio.Calcular(Cabildo, M(300, 300), ModoViaje.APie, CancellationToken.None);
            valhalla.De(ModoViaje.APie).Fallar();
            osrm.De(ModoViaje.APie).Responder(600);
            Assert.IsTrue(t.Result.Ok);
            Assert.IsFalse(t.Result.Valor.Aproximada);
            Assert.IsEmpty(local.Peticiones);
        }

        [Test]
        public void Servicio_SinCallesGuardadas_Fallo()
        {
            var t = servicio.Calcular(Cabildo, M(300, 300), ModoViaje.Coche, CancellationToken.None);
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Fallar("No hay calles guardadas que unan el origen y el destino.");
            Assert.IsFalse(t.Result.Ok);
            StringAssert.Contains("calles guardadas", t.Result.Error);
            Assert.IsFalse(new ServicioRutas(valhalla, osrm, esperas.Esperar).ConRutaAproximada, "Sin servicio local.");
        }

        // ───────────────────────── Pantalla (UI) ─────────────────────────

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
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false)
            {
                linea = puntos;
                this.aproximada = aproximada;
                dibujadas++;
            }
            public void QuitarRuta() => linea = null;
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
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

        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        readonly List<string> anuncios = new List<string>();

        void Anotar(string texto) => anuncios.Add(texto);

        FilaAcciones Fila => pantalla.Ficha.Acciones;
        TarjetaManiobra Arriba => pantalla.TarjetaManiobra;
        TarjetaRuta Abajo => pantalla.TarjetaRuta;
        Toast Aviso => pantalla.Toast;

        void Crear(ServicioRutas rutas = null)
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            var gps = new UbicacionFalsa { Estado = EstadoUbicacion.Activa, Posicion = Cabildo };
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, new ServicioUbicacion(gps, prefs), rutas: rutas ?? servicio);
            pantalla.RelojLocal = () => HoraLocal;
            tema.Iniciar();
            filtro.Iniciar();
        }

        void IrAAreas(ModoViaje modo = ModoViaje.Coche)
        {
            pantalla.AbrirFicha(Areas);
            if (modo != Fila.Elegido) Fila.Boton(modo).Pulsar();
            Fila.PulsarIr();
        }

        const string Duro = " ";

        [Test]
        public void SinConexion_LineaSuaveTarjetasConAproxYToast()
        {
            Crear();
            IrAAreas();
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Responder(240, 1200, aproximada: true);
            Assert.IsFalse(Aviso.Visible, "Sin toast al elegir.");
            Assert.AreEqual(1, pantalla.ComoLlegar.Rutas, "La aproximada sola.");
            Assert.AreEqual("≈" + Duro + "4 min", pantalla.ComoLlegar.Tarjetas[0].Minutos.text);
            Assert.AreEqual("Llegada ≈" + Duro + "13:34 · ≈" + Duro + "1,2" + Duro + "km", pantalla.ComoLlegar.Tarjetas[0].Detalle.text);
            Assert.AreEqual("Ruta aproximada, unos 4 minutos, llegada hacia las 13:34, unos 1,2 km", pantalla.ComoLlegar.Tarjetas[0].Anuncio);
            pantalla.Iniciar();

            Assert.IsNotNull(mapa.linea, "La línea por las calles guardadas.");
            Assert.IsTrue(mapa.aproximada, "Con los colores suaves.");
            Assert.AreEqual("Ruta aproximada por las calles guardadas", Arriba.Principal.text);
            Assert.AreEqual("Ruta aproximada por las calles guardadas", Arriba.Anuncio);
            Assert.AreEqual("≈" + Duro + "13:34", Abajo.Llegada.text);
            Assert.AreEqual("≈" + Duro + "4", Abajo.Minutos.text);
            Assert.AreEqual("≈" + Duro + "1,2", Abajo.Distancia.text);
            Assert.AreEqual("km", Abajo.RotuloDistancia.text);
            Assert.AreEqual("Ruta aproximada. Llegada hacia las 13:34, unos 4 minutos, unos 1,2 km", Abajo.Anuncio);
            Assert.IsTrue(Aviso.Visible);
            Assert.AreEqual("Sin conexión: ruta aproximada", Aviso.Texto.text);
            Assert.AreEqual(3000, Toast.DuracionMs, "3 s.");
            Assert.AreEqual(16f + PantallaMapa.AltoSupuestoTarjetaManiobraDp + 12f, Aviso.style.top.value.value, 1e-3f,
                "Bajo la tarjeta de arriba (sin medida, la supuesta) y 12 dp de separación.");
            Assert.AreEqual(0, servicio.EnCache);

            pantalla.QuitarRuta();
            Assert.IsFalse(Aviso.Visible, "Con la ruta se va el toast.");
        }

        [Test]
        public void DireccionBuscada_SinRed_MinutosYRutaAproximados()
        {
            Crear();
            var punto = M(300, 300);
            pantalla.ElegirResultado(ResultadoBusqueda.DeDireccion(
                new Direccion("Calle Ancha 10", "Sanlúcar de Barrameda", punto, "Calle Ancha", "10"), 424));
            Assert.AreEqual("Calle Ancha, 10", pantalla.Ficha.Titulo.text);
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Responder(240, 1200, aproximada: true);
            Assert.AreEqual(4, Fila.Coche.Minutos);
            Assert.IsTrue(Fila.Coche.Aproximados, "Como en cualquier punto libre.");

            Fila.PulsarIr();
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Responder(240, 1200, aproximada: true);
            pantalla.Iniciar();
            Assert.IsTrue(mapa.aproximada);
            Assert.AreEqual("Calle Ancha, 10", Abajo.DestinoNombre.text);
            Assert.AreEqual("Sin conexión: ruta aproximada", Aviso.Texto.text);
        }

        [Test]
        public void TalkBack_SeAnunciaElToastYElFocoVaALaTarjetaDeArriba()
        {
            Crear();
            AccesibilidadPantalla.LectorForzado = () => true;
            var oidos = new List<string>();
            void Oir(string t) => oidos.Add(t);
            AccesibilidadPantalla.Anunciado += Oir;
            try
            {
                IrAAreas();
                CaenLosDos(ModoViaje.Coche);
                local.De(ModoViaje.Coche).Responder(45, 350, aproximada: true);
                pantalla.Iniciar();
                Assert.AreSame(Arriba, pantalla.Accesibilidad.Enfocado);
                Assert.Contains("Sin conexión: ruta aproximada", oidos);
                Assert.AreEqual("Ruta aproximada. Llegada hacia las 13:30, alrededor de 1 minuto, unos 350 m", Abajo.Anuncio);
                Assert.AreEqual("Ruta aproximada por las calles guardadas", pantalla.Accesibilidad.Recorrido()[0]);
            }
            finally
            {
                AccesibilidadPantalla.Anunciado -= Oir;
            }
        }

        [Test]
        public void FichaReabierta_SeVaElToast()
        {
            Crear();
            IrAAreas();
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Responder(240, 1200, aproximada: true);
            Assert.IsFalse(anuncios.Contains("Sin conexión: ruta aproximada"), "Al elegir no se anuncia.");
            pantalla.Iniciar();
            Assert.IsTrue(Aviso.Visible);
            pantalla.ReabrirFichaRuta();
            Assert.IsFalse(Aviso.Visible, "Al reabrir la ficha se va el toast.");
        }

        [Test]
        public void UnMinutoAproximado_TalkBackEnLaTarjetaYEnLaFicha()
        {
            Crear();
            IrAAreas();
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Responder(45, 350, aproximada: true);
            Assert.AreEqual("Ruta aproximada, alrededor de 1 minuto, llegada hacia las 13:30, unos 350 m", pantalla.ComoLlegar.Tarjetas[0].Anuncio);
            pantalla.Iniciar();
            Assert.AreEqual("Ruta aproximada. Llegada hacia las 13:30, alrededor de 1 minuto, unos 350 m", Abajo.Anuncio);

            pantalla.QuitarRuta();
            pantalla.AbrirFicha(Areas);
            CaenLosDos(ModoViaje.APie);
            local.De(ModoViaje.APie).Responder(50, 70, aproximada: true);
            Assert.AreEqual("≈" + Duro + "1 min", Fila.APie.Tiempo.text);
            Assert.AreEqual("A pie, alrededor de 1 minuto", Fila.Anuncio(ModoViaje.APie));
            anuncios.Clear();
            Fila.Boton(ModoViaje.APie).Pulsar();
            Assert.Contains("A pie, alrededor de 1 minuto, seleccionado", anuncios, "Lo que anuncia TalkBack al elegirlo.");
        }

        [Test]
        public void ConConexion_ComoLa26bSinToast()
        {
            Crear();
            IrAAreas();
            valhalla.De(ModoViaje.Coche).Responder(240, 1200);
            pantalla.Iniciar();
            Assert.IsNotNull(mapa.linea);
            Assert.IsFalse(mapa.aproximada);
            Assert.AreEqual("4", Abajo.Minutos.text);
            Assert.IsFalse(Aviso.Visible);
            Assert.IsFalse(anuncios.Any(a => a.StartsWith("Sin conexión")));
        }

        [Test]
        public void SinCallesQueUnan_ElFalloYPorQueEnLaHoja()
        {
            Crear();
            IrAAreas();
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Fallar("No hay calles guardadas que unan el origen y el destino.");
            Assert.AreEqual("No se ha podido calcular la ruta", pantalla.ComoLlegar.Estado.text);
            Assert.AreEqual("Sin conexión: no hay calles guardadas para esta ruta", pantalla.ComoLlegar.DetalleEstado.text);
            Assert.AreEqual(0, pantalla.ComoLlegar.Rutas);
            Assert.AreEqual("—", pantalla.ComoLlegar.Coche.Tiempo.text);
            Assert.IsNull(mapa.linea);
            Assert.IsFalse(Aviso.Visible);
            Assert.Contains("No se ha podido calcular la ruta. Sin conexión: no hay calles guardadas para esta ruta", anuncios);
        }

        [Test]
        public void SinServicioLocal_ElFalloSinMas()
        {
            Crear(new ServicioRutas(valhalla, osrm, esperas.Esperar, () => Ahora));
            IrAAreas();
            CaenLosDos(ModoViaje.Coche);
            Assert.AreEqual("No se ha podido calcular la ruta", pantalla.ComoLlegar.Estado.text);
            Assert.AreEqual("", pantalla.ComoLlegar.DetalleEstado.text);
            Assert.IsFalse(Aviso.Visible);
        }

        [Test]
        public void FichaSinRed_MinutosAproximadosEnLosDosModos()
        {
            Crear();
            pantalla.AbrirFicha(Areas);
            CaenLosDos(ModoViaje.Coche);
            CaenLosDos(ModoViaje.APie);
            local.De(ModoViaje.Coche).Responder(240, 1200, aproximada: true);
            local.De(ModoViaje.APie).Responder(840, 1200, aproximada: true);
            Assert.AreEqual("≈" + Duro + "4 min", Fila.Coche.Tiempo.text);
            Assert.AreEqual("≈" + Duro + "14 min", Fila.APie.Tiempo.text);
            Assert.AreEqual("Coche, unos 4 minutos", Fila.Anuncio(ModoViaje.Coche));
            Assert.AreEqual("A pie, unos 14 minutos", Fila.Anuncio(ModoViaje.APie));
            Assert.IsFalse(Aviso.Visible, "El toast solo con Iniciar.");

            // Una ruta de 1 minuto y, en otra ficha, sin camino: "—"
            Fila.PonerMinutos(ModoViaje.Coche, 1, aproximados: true);
            Assert.AreEqual("Coche, alrededor de 1 minuto", Fila.Anuncio(ModoViaje.Coche));
            pantalla.CerrarFicha();
            pantalla.AbrirFicha(Areas);
            CaenLosDos(ModoViaje.Coche);
            local.De(ModoViaje.Coche).Fallar();
            Assert.AreEqual("—", Fila.Coche.Tiempo.text);
            Assert.AreEqual("Coche, sin tiempo", Fila.Anuncio(ModoViaje.Coche));
        }

        [Test]
        public void Cancelar_CerrarLaHojaMientrasCalculaNoDibujaNada()
        {
            Crear();
            IrAAreas();
            CaenLosDos(ModoViaje.Coche);
            var p = local.De(ModoViaje.Coche);
            pantalla.CerrarComoLlegar();
            Assert.IsTrue(p.Ct.IsCancellationRequested, "Se cancela el cálculo local.");
            p.Responder(240, 1200, aproximada: true);
            Assert.IsNull(mapa.linea);
            Assert.AreEqual(0, mapa.dibujadas);
            Assert.IsFalse(Aviso.Visible);
        }

        [Test]
        public void LosTextosEstanEnLaTabla()
        {
            foreach (var clave in new[]
                     {
                         ClavesTexto.RutaAproximada, ClavesTexto.RutaValorAprox, ClavesTexto.RutaAnuncioAprox,
                         ClavesTexto.RutaAnuncioAproxMinuto, ClavesTexto.FichaMinutosAprox, ClavesTexto.FichaAnuncioMinutosAprox,
                         ClavesTexto.FichaAnuncioMinutoAprox, ClavesTexto.ToastRutaAproximada, ClavesTexto.ToastSinCalles,
                     })
                CollectionAssert.Contains(ClavesTexto.Todas, clave);
        }

        // ───────────────────────── Puerto real: colores suaves ─────────────────────────

        static Color ColorHex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        [Test]
        public void MapaVectorial_LaAproximadaConLosColoresSuavesDeCadaTema()
        {
            double latAntes = Geo.LatitudOrigen, lonAntes = Geo.LongitudOrigen;
            Geo.LatitudOrigen = Cabildo.Latitud; Geo.LongitudOrigen = Cabildo.Longitud;
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = objCamara.AddComponent<CamaraMapa>();
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                var real = objMapa.AddComponent<MapaVectorial>();
                camara.distancia = 300f;
                IMapa puerto = real;
                puerto.MostrarRuta(new[] { Cabildo, M(100, 0), M(100, 100) }, aproximada: true);
                Assert.IsTrue(real.RutaAproximada);
                Assert.AreEqual(2, real.ObjetoRuta.transform.childCount, "La misma línea, con borde.");
                Assert.AreEqual(1960, real.MaterialBordeRuta.renderQueue);
                Assert.AreEqual(1970, real.MaterialRuta.renderQueue);
                Assert.AreEqual(ColorHex("#8FA3E6"), real.MaterialRuta.color);
                Assert.AreEqual(ColorHex("#5F78C9"), real.MaterialBordeRuta.color);

                puerto.AplicarTema(Tema.Oscuro);
                real.ColorearRutaAhora();
                Assert.AreEqual(ColorHex("#6F86C9"), real.MaterialRuta.color);
                Assert.AreEqual(ColorHex("#44599A"), real.MaterialBordeRuta.color);

                puerto.MostrarRuta(new[] { Cabildo, M(100, 0) }); // vuelve la red: la de siempre
                Assert.IsFalse(real.RutaAproximada);
                Assert.AreEqual(ColorHex("#4C7DFF"), real.MaterialRuta.color);
                Assert.AreEqual(ColorHex("#2F5BEA"), real.MaterialBordeRuta.color);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                UnityEngine.Object.DestroyImmediate(objCamara);
                Geo.LatitudOrigen = latAntes; Geo.LongitudOrigen = lonAntes;
            }
        }
    }
}
