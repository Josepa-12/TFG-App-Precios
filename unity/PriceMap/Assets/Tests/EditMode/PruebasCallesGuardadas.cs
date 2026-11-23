using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Calles guardadas del buscador (spec buscar-calles-guardadas): la regla (abreviaturas, número, orden, repetidas), el
    /// punto medio del trazado, la lectura de teselas MVT de prueba escritas en una carpeta temporal, la búsqueda que se
    /// cancela y la hoja en la pantalla real, una prueba por fila de la matriz. Nada sale a la red.
    /// </summary>
    public class PruebasCallesGuardadas
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        const double MetrosGrado = Math.PI / 180.0 * Coordenada.RadioTierraMetros;
        const string Sanlucar = "Sanlúcar de Barrameda";

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        /// <summary>Punto a (este, norte) metros de la Plaza del Cabildo.</summary>
        static Coordenada M(double este, double norte) =>
            new Coordenada(Cabildo.Latitud + norte / MetrosGrado,
                Cabildo.Longitud + este / (MetrosGrado * Math.Cos(Cabildo.Latitud * Math.PI / 180.0)));

        static Coordenada[] Linea(params (double este, double norte)[] puntos) => puntos.Select(p => M(p.este, p.norte)).ToArray();

        SynchronizationContext contextoAntes;
        string carpeta;

        [SetUp]
        public void Preparar()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "PriceMapPruebas", Guid.NewGuid().ToString("N"));
            espera = null;
            // Sin contexto de sincronización, las continuaciones corren en cuanto la prueba completa una tarea falsa
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
        }

        [TearDown]
        public void Limpiar()
        {
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            if (oir != null) AccesibilidadPantalla.AnuncioPedido -= oir;
            oir = null;
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = null;
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        // ───────────────────────── Punto medio del trazado ─────────────────────────

        static void EsPunto(Coordenada esperado, Coordenada punto, string mensaje = null) =>
            Assert.Less(esperado.DistanciaMetros(punto), 1.0, mensaje ?? $"{punto} no es {esperado}");

        [Test]
        public void Medio_DeUnaCalleRecta() => EsPunto(M(100, 0), TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (30, 0), (200, 0)) }));

        [Test]
        public void Medio_DeUnaCalleEnL_SigueElTrazado() =>
            EsPunto(M(100, 50), TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (100, 0), (100, 200)) }), "A 150 m por la calle, no en línea recta.");

        [Test]
        public void Medio_DeTramosEnCualquierOrdenYSentido() =>
            EsPunto(M(150, 0), TrazadoCalle.PuntoMedio(new[] { Linea((300, 0), (200, 0)), Linea((0, 0), (100, 0)), Linea((200, 0), (100, 0)) }));

        [Test]
        public void Medio_LosTramosSuperpuestosNoCuentanDosVeces() =>
            EsPunto(M(100, 0), TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (50, 0), (200, 0)), Linea((0, 0), (50, 0)) }));

        [Test]
        public void Medio_CoseLosExtremosDelCorteEntreTeselas() =>
            EsPunto(M(100, 0), TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (149, 0)), Linea((150, 1), (200, 0)) }));

        [Test]
        public void Medio_EnElCosidoEsUnExtremoDeLaCalle()
        {
            var medio = TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (98, 0)), Linea((102, 0), (200, 0)) });
            Assert.IsTrue(medio.DistanciaMetros(M(98, 0)) < 0.01 || medio.DistanciaMetros(M(102, 0)) < 0.01, "Nunca fuera de la calle.");
        }

        [Test]
        public void Medio_ConTrozosSueltosEsElDelMasLargo() =>
            EsPunto(M(1100, 0), TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (50, 0)), Linea((1000, 0), (1200, 0)) }));

        [Test]
        public void Medio_UnTrozoRepetidoNoCuentaDosVecesNiDejaDeCoserse()
        {
            // El corto repetido (60 + 60) no gana al largo suelto (100)
            EsPunto(M(1050, 0), TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (60, 0)), Linea((0, 0), (60, 0)), Linea((1000, 0), (1100, 0)) }));
            // Y su extremo se sigue cosiendo con el de la tesela de al lado
            EsPunto(M(50, 0), TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (30, 0)), Linea((30, 0), (0, 0)), Linea((32, 0), (100, 0)), Linea((32, 0), (100, 0)) }));
        }

        [Test]
        public void Medio_DeUnaGlorietaEstaEnElla()
        {
            var medio = TrazadoCalle.PuntoMedio(new[] { Linea((0, 0), (40, 0), (40, 40), (0, 40), (0, 0)) });
            double lejos = new[] { M(0, 0), M(40, 0), M(40, 40), M(0, 40) }.Min(c => c.DistanciaMetros(medio));
            Assert.Less(Math.Min(lejos, Math.Min(Math.Abs(medio.DistanciaMetros(M(0, 0)) - 40), Math.Abs(medio.DistanciaMetros(M(40, 40)) - 40))), 1.0);
        }

        [Test]
        public void Medio_SinPuntosNoLanza() => Assert.AreEqual(default(Coordenada), TrazadoCalle.PuntoMedio(null));

        // ───────────────────────── Regla ─────────────────────────

        [TestCase("calle farinas", "calle farinas")]
        [TestCase("Calle Fariñas, 12", "calle farinas 12|calle farinas")]
        [TestCase("c/ fariñas", "c farinas|calle farinas")]
        [TestCase("avda. calzada 3", "avda calzada 3|avenida calzada 3|avda calzada|avenida calzada")]
        [TestCase("plza cabildo", "plza cabildo|plaza cabildo")]
        [TestCase("fariñas nº 12 b", "farinas nº 12 b|farinas")]
        [TestCase("fariñas 12b", "farinas 12b|farinas")]
        [TestCase("fariñas 12, 2º b", "farinas 12 2º b|farinas")]
        [TestCase("fariñas s/n", "farinas s n|farinas")]
        [TestCase("fariñas nº12", "farinas nº12|farinas")]
        [TestCase("fariñas n12", "farinas n12|farinas")]
        [TestCase("fariñas n.º 12", "farinas n º 12|farinas")]
        [TestCase("fariñas 12bis", "farinas 12bis|farinas")]
        [TestCase("fariñas 12 bis", "farinas 12 bis|farinas")]
        [TestCase("fariñas 12 bajo", "farinas 12 bajo|farinas")]
        [TestCase("fariñas 12, 1º izda", "farinas 12 1º izda|farinas")]
        [TestCase("fariñas 12 portal 3", "farinas 12 portal 3|farinas")]
        [TestCase("fariñas portal 3", "farinas portal 3|farinas")]
        [TestCase("fariñas nº", "farinas nº|farinas")]
        [TestCase("fariñas num", "farinas num|farinas")]
        [TestCase("fariñas número", "farinas numero|farinas")]
        [TestCase("fariñas n", "farinas n")]     // suelta, puede ser otra palabra a medias
        [TestCase("fariñas no", "farinas no")]
        [TestCase("fariñas bajo", "farinas bajo")] // sin número al lado, es parte del nombre
        [TestCase("callejón bajo 12", "callejon bajo 12|callejon bajo")]
        [TestCase("calle portal 3", "calle portal 3|calle portal")]
        [TestCase("calle 12 de octubre", "calle 12 de octubre")]
        [TestCase("calle 12 de octubre 4", "calle 12 de octubre 4|calle 12 de octubre")]
        [TestCase("calle 12", "calle 12")] // sin el número, «calle» no dice qué calle es
        [TestCase("avda 1492", "avda 1492|avenida 1492")]
        [TestCase("a 12", "a 12")]         // sin el número no llega a 3 caracteres
        [TestCase("1492", "1492")]
        [TestCase("fa", "")]
        [TestCase(" ¡Fa! ", "")]
        [TestCase("", "")]
        public void Formas(string consulta, string esperadas) =>
            Assert.AreEqual(esperadas, string.Join("|", ReglaCalles.Formas(consulta)));

        static TramoCalle Tramo(string nombre, params (double este, double norte)[] puntos) => new TramoCalle(nombre, Linea(puntos));

        static readonly LocalidadMapa[] SoloSanlucar = { new LocalidadMapa(Sanlucar, Cabildo) };

        static string[] Nombres(IReadOnlyList<CalleGuardada> calles, string consulta) =>
            ReglaCalles.Buscar(calles, consulta, Cabildo).Select(d => d.Nombre).ToArray();

        [TestCase("farinas")]
        [TestCase("c farinas")]
        [TestCase("c/ fariñas")]
        [TestCase("calle fari")]
        [TestCase("calle fariñas 12")]
        public void EncuentraSinTildesPorPrincipioDePalabraYSinNumero(string consulta)
        {
            var calles = ReglaCalles.Juntar(new[] { Tramo("Calle Fariñas", (0, 0), (100, 0)), Tramo("Calle Ancha", (0, 50), (100, 50)) }, SoloSanlucar);
            CollectionAssert.AreEqual(new[] { "Calle Fariñas" }, Nombres(calles, consulta));
        }

        [Test]
        public void AvdaEsAvenida()
        {
            var calles = ReglaCalles.Juntar(new[] { Tramo("Avenida Calzada de la Duquesa Isabel", (0, 0), (100, 0)), Tramo("Calle Calzada", (0, 50), (100, 50)) }, SoloSanlucar);
            CollectionAssert.AreEqual(new[] { "Avenida Calzada de la Duquesa Isabel" }, Nombres(calles, "avda calzada"));
        }

        [Test]
        public void LaAbreviaturaNoPierdeLoQueYaEncontraba()
        {
            var calles = ReglaCalles.Juntar(new[] { Tramo("Camino Ancho", (0, 0), (10, 0)), Tramo("Calle Ancha", (500, 0), (600, 0)) }, SoloSanlucar);
            CollectionAssert.AreEqual(new[] { "Calle Ancha", "Camino Ancho" }, Nombres(calles, "c anch"), "Primero la calle.");
        }

        [Test]
        public void OrdenPorPuntuacionYDespuesPorDistanciaComoMuchoOcho()
        {
            var tramos = new List<TramoCalle> { Tramo("Calle Ancha Baja", (10, 0), (20, 0)), Tramo("Calle Ancha", (900, 0), (950, 0)), Tramo("Plaza Ancha", (0, 0), (5, 0)) };
            for (int i = 0; i < 10; i++) tramos.Add(Tramo("Calle Ancha " + (char)('j' - i), (100 + 10 * i, 0), (105 + 10 * i, 0)));
            var nombres = Nombres(ReglaCalles.Juntar(tramos, SoloSanlucar), "calle ancha");
            Assert.AreEqual(ReglaCalles.MaximoCalles, nombres.Length);
            Assert.AreEqual("Calle Ancha", nombres[0], "La exacta, aunque esté más lejos.");
            CollectionAssert.AreEqual(new[] { "Calle Ancha Baja", "Calle Ancha j", "Calle Ancha i" }, nombres.Skip(1).Take(3).ToArray());
            CollectionAssert.DoesNotContain(nombres, "Plaza Ancha");
        }

        [Test]
        public void UnaFilaPorLocalidad()
        {
            var chipiona = new LocalidadMapa("Chipiona", M(-8000, 0));
            var calles = ReglaCalles.Juntar(new[]
            {
                Tramo("Calle Real", (0, 0), (100, 0)), Tramo("Calle Real", (100, 0), (200, 0)), Tramo("CALLE REAL", (200, 0), (300, 0)),
                Tramo("Calle Real", (-8000, 0), (-7900, 0)),
            }, new[] { SoloSanlucar[0], chipiona });
            var d = ReglaCalles.Buscar(calles, "calle real", Cabildo);
            CollectionAssert.AreEqual(new[] { Sanlucar, "Chipiona" }, d.Select(x => x.Localidad).ToArray());
            EsPunto(M(150, 0), d[0].Posicion, "Los tres tramos de Sanlúcar, juntos.");
            EsPunto(M(-7950, 0), d[1].Posicion);
            Assert.AreEqual("Calle Real", d[0].Calle);
            Assert.AreEqual("", d[0].Numero, "Sin inventar números.");
        }

        [Test]
        public void SinLocalidadesNiNombre()
        {
            var calles = ReglaCalles.Juntar(new[] { Tramo("Calle Real", (0, 0), (100, 0)), Tramo("  ", (0, 0), (100, 0)), Tramo("…", (0, 0), (9, 0)), Tramo("Calle Punto", (0, 0)) }, null);
            Assert.AreEqual(1, calles.Count);
            Assert.AreEqual("", calles[0].Localidad);
        }

        [TestCase("calle fariñas sanlucar", "Calle Fariñas")]
        [TestCase("farinas barrameda", "Calle Fariñas")]
        [TestCase("fariñas 12 sanlucar", "Calle Fariñas")]
        [TestCase("c/ fariñas nº 12 bajo, sanlúcar de barrameda", "Calle Fariñas")]
        [TestCase("sanlucar", "Avenida de Sanlúcar")]          // sola, solo las que la llevan en el nombre
        [TestCase("calle sanlucar", "")]                       // ni un tipo de vía…
        [TestCase("calle de la sanlucar", "")]                 // …ni una partícula dicen qué calle es
        [TestCase("call sanlucar", "")]                        // ni un tipo de vía a medio escribir
        [TestCase("calle 12 sanlucar", "")]
        [TestCase("fariñas chipiona", "")]
        [TestCase("sanlucar chipiona", "Avenida de Sanlúcar")]
        public void LasPalabrasQueFaltanPuedenSerDeLaLocalidad(string consulta, string esperada)
        {
            var calles = ReglaCalles.Juntar(new[]
            {
                Tramo("Calle Fariñas", (0, 0), (100, 0)), Tramo("Calle de la Ancha", (0, 50), (100, 50)), Tramo("Avenida de Sanlúcar", (-8000, 0), (-7900, 0)),
            }, new[] { SoloSanlucar[0], new LocalidadMapa("Chipiona", M(-8000, 0)) });
            Assert.AreEqual(esperada, string.Join("|", Nombres(calles, consulta)));
        }

        [Test]
        public void ConLaLocalidadPuntuaContiene()
        {
            Assert.AreEqual(ReglaBusqueda.Contiene, ReglaCalles.Puntuar(ReglaCalles.Formas("calle fariñas sanlucar"), "Calle Fariñas", Sanlucar));
            Assert.AreEqual(ReglaBusqueda.NoCoincide, ReglaCalles.Puntuar(ReglaCalles.Formas("calle fariñas sanlucar"), "Calle Fariñas", ""));
        }

        static ResultadoBusqueda R(string nombre, Coordenada c) => ResultadoBusqueda.DeDireccion(new Direccion(nombre, Sanlucar, c), 0);

        [Test]
        public void SinRepetidas_MismoNombreAMenosDeUnKilometro()
        {
            var calles = new[] { R("Calle Ancha", Cabildo) };
            var photon = new[] { R("calle  ancha", M(900, 0)), R("Calle Ancha", M(1100, 0)), R("Calle Ancha 10", M(0, 0)), R("Calle Áncha", M(0, 10)) };
            CollectionAssert.AreEqual(new[] { photon[1], photon[2] }, ReglaCalles.SinRepetidas(calles, photon).ToArray());
            Assert.AreSame(photon, ReglaCalles.SinRepetidas(null, photon));
            Assert.IsNull(ReglaCalles.SinRepetidas(calles, null));
        }

        // ───────────────────────── Teselas guardadas ─────────────────────────

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

        /// <summary>Elemento de una capa de prueba: clase, nombre (null sin nombre) y puntos en la rejilla de 4096.</summary>
        class Elemento
        {
            public string Clase, Nombre;
            public (int x, int y)[] Puntos;
            public Elemento(string clase, string nombre, params (int x, int y)[] puntos) { Clase = clase; Nombre = nombre; Puntos = puntos; }
        }

        static List<byte> Capa(string nombre, Elemento[] elementos, int extension = 4096)
        {
            var valores = elementos.SelectMany(e => new[] { e.Clase, e.Nombre }).Where(v => v != null).Distinct().ToList();
            var capa = new List<byte>();
            Campo(capa, 15, 0); Varint(capa, 2);
            Bytes(capa, 1, Encoding.UTF8.GetBytes(nombre));
            foreach (var e in elementos)
            {
                var elemento = new List<byte>();
                var etiquetas = new List<byte>();
                if (e.Clase != null) { Varint(etiquetas, 0); Varint(etiquetas, (ulong)valores.IndexOf(e.Clase)); }
                if (e.Nombre != null) { Varint(etiquetas, 1); Varint(etiquetas, (ulong)valores.IndexOf(e.Nombre)); }
                Bytes(elemento, 2, etiquetas);
                Campo(elemento, 3, 0); Varint(elemento, e.Puntos.Length == 1 ? 1ul : 2ul);
                var geo = new List<byte>();
                int x = 0, y = 0;
                for (int i = 0; i < e.Puntos.Length; i++)
                {
                    if (i == 0) Varint(geo, (1 << 3) | 1);
                    if (i == 1) Varint(geo, ((ulong)(e.Puntos.Length - 1) << 3) | 2);
                    Varint(geo, ZigZag(e.Puntos[i].x - x));
                    Varint(geo, ZigZag(e.Puntos[i].y - y));
                    (x, y) = e.Puntos[i];
                }
                Bytes(elemento, 4, geo);
                Bytes(capa, 2, elemento);
            }
            Bytes(capa, 3, Encoding.UTF8.GetBytes("class"));
            Bytes(capa, 3, Encoding.UTF8.GetBytes("name"));
            foreach (var v in valores)
            {
                var valor = new List<byte>();
                Bytes(valor, 1, Encoding.UTF8.GetBytes(v));
                Bytes(capa, 4, valor);
            }
            Campo(capa, 5, 0); Varint(capa, (ulong)extension);
            return capa;
        }

        static byte[] Tesela(Elemento[] calles, params Elemento[] lugares) => Tesela(4096, calles, lugares);

        static byte[] Tesela(int extension, Elemento[] calles, params Elemento[] lugares)
        {
            var tesela = new List<byte>();
            Bytes(tesela, 3, Capa("transportation_name", calles, extension));
            if (lugares.Length > 0) Bytes(tesela, 3, Capa("place", lugares, extension));
            return tesela.ToArray();
        }

        static Coordenada EnTesela(int dx, int dy, double px, double py, int zoom = 14)
        {
            var (tx, ty) = RutasCallesGuardadas.Tesela(Cabildo, zoom);
            return RutasCallesGuardadas.ACoordenada((tx + dx) * 4096.0 + px, (ty + dy) * 4096.0 + py, zoom);
        }

        string Guardar(int dx, int dy, byte[] datos, int zoom = 14)
        {
            var (tx, ty) = RutasCallesGuardadas.Tesela(Cabildo, zoom);
            var dir = Path.Combine(carpeta, "vectorial", zoom.ToString(), (tx + dx).ToString());
            Directory.CreateDirectory(dir);
            var ruta = Path.Combine(dir, (ty + dy) + ".pbf");
            File.WriteAllBytes(ruta, datos);
            return ruta;
        }

        static IReadOnlyList<Direccion> Buscar(BusquedaCallesGuardadas b, string consulta)
        {
            var r = b.BuscarAhora(consulta, Cabildo, CancellationToken.None);
            Assert.IsTrue(r.Ok, r.Error);
            return r.Valor;
        }

        static readonly Elemento PuebloSanlucar = new Elemento("town", Sanlucar, (2000, 2000));

        [Test]
        public void Teselas_JuntaLosTramosDeDosTeselasYDaElMedio()
        {
            // La calle va de x = 1000 a x = 7000 de la rejilla global; cada tesela sobresale 128 en la otra
            Guardar(0, 0, Tesela(new[] { new Elemento("tertiary", "Calle Fariñas", (1000, 2000), (4224, 2000)) }, PuebloSanlucar));
            Guardar(1, 0, Tesela(new[] { new Elemento("tertiary", "Calle Fariñas", (-128, 2001), (2904, 2001)) }));
            var d = Buscar(new BusquedaCallesGuardadas(carpeta), "calle farinas").Single();
            Assert.AreEqual("Calle Fariñas", d.Nombre);
            Assert.AreEqual(Sanlucar, d.Localidad);
            EsPunto(EnTesela(0, 0, 4000, 2000), d.Posicion);
        }

        [Test]
        public void Teselas_UnaCalleQueSaleYVuelveAEntrarSeParteEnElBorde()
        {
            var tramos = new List<TramoCalle>();
            var (tx, ty) = RutasCallesGuardadas.Tesela(Cabildo);
            BusquedaCallesGuardadas.Leer(Tesela(new[] { new Elemento("minor", "Calle Borde", (3996, 100), (4196, 100), (4196, 300), (3996, 300)) }),
                tx, ty, 14, tramos, new List<LocalidadMapa>());
            Assert.AreEqual(2, tramos.Count);
            EsPunto(EnTesela(0, 0, 4096, 100), tramos[0].Puntos[1]);
            EsPunto(EnTesela(0, 0, 4096, 300), tramos[1].Puntos[0]);
        }

        [Test]
        public void Teselas_FueraBarcosTrenesCircuitosYLoQueNoTieneNombreOEsUnPunto()
        {
            Guardar(0, 0, Tesela(new[]
            {
                new Elemento("ferry", "Ruta del Barco", (0, 0), (900, 0)), new Elemento("rail", "Ruta del Tren", (0, 10), (900, 10)),
                new Elemento("transit", "Ruta del Tranvía", (0, 20), (900, 20)), new Elemento("raceway", "Ruta del Circuito", (0, 30), (900, 30)),
                new Elemento("minor", null, (0, 40), (900, 40)), new Elemento("motorway", "Ruta de la Salida", (50, 50)),
                new Elemento("path", "Ruta Peatonal", (0, 60), (900, 60)), new Elemento(null, "Ruta sin Clase", (0, 70), (900, 70)),
            }, PuebloSanlucar));
            CollectionAssert.AreEquivalent(new[] { "Ruta Peatonal", "Ruta sin Clase" },
                Buscar(new BusquedaCallesGuardadas(carpeta), "ruta").Select(d => d.Nombre).ToArray());
        }

        [TestCase("city")]
        [TestCase("town")]
        [TestCase("village")]
        public void Teselas_LaLocalidadEsCityTownOVillage(string clase)
        {
            Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (0, 0), (100, 0)) },
                new Elemento("suburb", "La Jara", (50, 0)), new Elemento("neighbourhood", "Capuchinos", (60, 0)),
                new Elemento("hamlet", "Las Brevas", (70, 0)), new Elemento(clase, Sanlucar, (2000, 2000))));
            Assert.AreEqual(Sanlucar, Buscar(new BusquedaCallesGuardadas(carpeta), "calle real").Single().Localidad);
        }

        [Test]
        public void Teselas_ConElZoomDelMapa()
        {
            // El mapa guarda a zoom 13: con ese zoom se encuentran sus teselas; con el de por defecto (14), no
            Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (1000, 1000), (3000, 1000)) }), zoom: 13);
            EsPunto(EnTesela(0, 0, 2000, 1000, 13), Buscar(new BusquedaCallesGuardadas(carpeta, 13), "calle real").Single().Posicion);
            Assert.IsEmpty(Buscar(new BusquedaCallesGuardadas(carpeta), "calle real"));
        }

        [Test]
        public void Teselas_ConOtraExtension()
        {
            Guardar(0, 0, Tesela(512, new[] { new Elemento("minor", "Calle Real", (100, 100), (300, 100)) },
                new Elemento("town", "Chipiona", (500, 500)), new Elemento("town", Sanlucar, (200, 110))));
            var d = Buscar(new BusquedaCallesGuardadas(carpeta), "calle real").Single();
            EsPunto(EnTesela(0, 0, 1600, 800), d.Posicion);
            Assert.AreEqual(Sanlucar, d.Localidad);
        }

        [Test]
        public void Teselas_LaBorradaDejaDeContar()
        {
            Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (0, 0), (100, 0)) }));
            var ruta = Guardar(1, 0, Tesela(new[] { new Elemento("minor", "Calle Ancha", (0, 0), (100, 0)) }));
            var b = new BusquedaCallesGuardadas(carpeta);
            Assert.AreEqual(2, Buscar(b, "calle").Count);
            File.Delete(ruta);
            Assert.AreEqual("Calle Real", Buscar(b, "calle").Single().Nombre);
        }

        [Test]
        public void Teselas_LaQueNoSePudoAbrirSeReintenta()
        {
            var ruta = Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (0, 0), (100, 0)) }));
            var b = new BusquedaCallesGuardadas(carpeta);
            LogAssert.Expect(LogType.Warning, new Regex("ilegible para buscar calles"));
            using (new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.IsEmpty(Buscar(b, "calle real"));
            Assert.AreEqual(1, Buscar(b, "calle real").Count);
        }

        // Largos dañados: negativo al pasarlo a int (volvía atrás para siempre) o que se sale, en la tesela y en la capa
        [TestCase(new byte[] { 0x1a, 0xfa, 0xff, 0xff, 0xff, 0x0f })]
        [TestCase(new byte[] { 0x1a, 0xfa, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01 })]
        [TestCase(new byte[] { 0x22, 0xfa, 0xff, 0xff, 0xff, 0x0f })]
        [TestCase(new byte[] { 0x1a, 0x06, 0x12, 0xfa, 0xff, 0xff, 0xff, 0x0f })]
        [TestCase(new byte[] { 0x1a, 0x02, 0x12, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [TestCase(new byte[] { 0x1a, 0xff, 0xff, 0xff, 0xff, 0x07 })]
        public void Teselas_UnLargoDanadoSeSaltaSinQuedarseEnBucle(byte[] datos)
        {
            Assert.Throws<FormatException>(() => LectorMVT.Leer(datos));
            Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (0, 0), (100, 0)) }));
            Guardar(1, 0, datos);
            LogAssert.Expect(LogType.Warning, new Regex("ilegible para buscar calles"));
            Assert.AreEqual(1, Buscar(new BusquedaCallesGuardadas(carpeta), "calle real").Count);
        }

        [Test]
        public void Teselas_SinTeselasNoHayCalles()
        {
            Assert.IsEmpty(Buscar(new BusquedaCallesGuardadas(carpeta), "calle real"));
            Assert.IsEmpty(Buscar(new BusquedaCallesGuardadas(null), "calle real"));
        }

        [Test]
        public void Teselas_LaIlegibleSeSaltaConUnSoloAviso()
        {
            Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (0, 0), (100, 0)) }));
            Guardar(1, 0, new byte[] { 0x1a, 0x7f, 0x01 }); // una capa que se sale del fichero
            LogAssert.Expect(LogType.Warning, new Regex("ilegible para buscar calles"));
            var b = new BusquedaCallesGuardadas(carpeta);
            Assert.AreEqual(1, Buscar(b, "calle real").Count);
            Assert.AreEqual(1, Buscar(b, "calle real").Count);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Teselas_LasGuardadasConLaAppAbiertaCuentan()
        {
            var b = new BusquedaCallesGuardadas(carpeta);
            Assert.IsEmpty(Buscar(b, "calle"));
            Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (0, 0), (100, 0)) }));
            Assert.AreEqual(1, Buscar(b, "calle").Count);
            Guardar(1, 0, Tesela(new[] { new Elemento("minor", "Calle Ancha", (0, 0), (100, 0)) }, PuebloSanlucar));
            var d = Buscar(b, "calle");
            CollectionAssert.AreEquivalent(new[] { "Calle Real", "Calle Ancha" }, d.Select(x => x.Nombre).ToArray());
            Assert.IsTrue(d.All(x => x.Localidad == Sanlucar), "Las localidades también se ponen al día.");
            // Una que el mapa aún estaba escribiendo (cortada): al acabar cuenta
            var entera = Tesela(new[] { new Elemento("minor", "Calle Nueva", (0, 0), (100, 0)) });
            var ruta = Guardar(0, 1, entera.Take(entera.Length / 2).ToArray());
            LogAssert.Expect(LogType.Warning, new Regex("ilegible para buscar calles"));
            Assert.AreEqual(2, Buscar(b, "calle").Count);
            File.WriteAllBytes(ruta, entera);
            Assert.AreEqual(3, Buscar(b, "calle").Count);
        }

        [Test]
        public void Teselas_NoSeVuelvenALeerSiNoCambian()
        {
            Guardar(0, 0, Tesela(new[] { new Elemento("minor", "Calle Real", (0, 0), (100, 0)) }));
            var b = new BusquedaCallesGuardadas(carpeta);
            Assert.AreSame(Buscar(b, "calle real").Single(), Buscar(b, "real").Single(), "La misma dirección ya calculada.");
        }

        [Test]
        public void Teselas_FueraDelHiloPrincipal()
        {
            // El aviso de la tesela ilegible dice en qué hilo se ha leído
            Guardar(0, 0, new byte[] { 0x1a, 0x7f, 0x01 });
            int hilo = Thread.CurrentThread.ManagedThreadId, leido = -1;
            void Oir(string mensaje, string traza, LogType tipo) { if (mensaje.Contains("ilegible para buscar calles")) leido = Thread.CurrentThread.ManagedThreadId; }
            Application.logMessageReceivedThreaded += Oir;
            LogAssert.Expect(LogType.Warning, new Regex("ilegible para buscar calles"));
            try
            {
                Assert.IsTrue(new BusquedaCallesGuardadas(carpeta).Buscar("calle real", Cabildo, CancellationToken.None).Result.Ok);
            }
            finally { Application.logMessageReceivedThreaded -= Oir; }
            Assert.AreNotEqual(-1, leido);
            Assert.AreNotEqual(hilo, leido);
        }

        [Test]
        public void Teselas_Cancelar()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<OperationCanceledException>(() => new BusquedaCallesGuardadas(carpeta).BuscarAhora("calle", Cabildo, cts.Token));
        }

        [Test]
        public void Teselas_LasDeSanlucarGuardadasEnElPc_DanCalleFarinas()
        {
            var ruta = Path.Combine(Application.persistentDataPath, "vectorial", "14", "7903", "6389.pbf");
            if (!File.Exists(ruta)) Assert.Ignore("Sin las teselas de Sanlúcar en este PC.");
            var b = new BusquedaCallesGuardadas(Application.persistentDataPath);
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            var d = Buscar(b, "calle farinas")[0];
            long primera = reloj.ElapsedMilliseconds;
            reloj.Restart();
            Buscar(b, "calle");
            long todas = reloj.ElapsedMilliseconds;
            reloj.Restart();
            Buscar(b, "calle a");
            TestContext.WriteLine($"Primera {primera} ms; «calle» {todas} ms; la siguiente {reloj.ElapsedMilliseconds} ms; {d.Posicion}");
            Assert.AreEqual("Calle Fariñas", d.Nombre);
            Assert.AreEqual(Sanlucar, d.Localidad);
            // El punto está sobre un tramo de la calle
            var tramos = new List<TramoCalle>();
            foreach (int x in new[] { 7902, 7903 })
            {
                var t = Path.Combine(Application.persistentDataPath, "vectorial", "14", x.ToString(), "6389.pbf");
                if (File.Exists(t)) BusquedaCallesGuardadas.Leer(File.ReadAllBytes(t), x, 6389, 14, tramos, new List<LocalidadMapa>());
            }
            double cerca = tramos.Where(t => t.Nombre == "Calle Fariñas")
                .SelectMany(t => t.Puntos.Zip(t.Puntos.Skip(1), (p, q) => Desvio(p, q, d.Posicion))).Min();
            Assert.Less(cerca, 1.0, "En la calle.");
            Assert.AreEqual("Calle Fariñas", Buscar(b, "c/ fariñas 12")[0].Nombre);
        }

        /// <summary>Lo que se alarga el camino de p a q al pasar por c (0 si c está en el segmento).</summary>
        static double Desvio(Coordenada p, Coordenada q, Coordenada c) =>
            p.DistanciaMetros(c) + c.DistanciaMetros(q) - p.DistanciaMetros(q);

        // ───────────────────────── BuscadorCalles ─────────────────────────

        class Peticion
        {
            public string Consulta;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<IReadOnlyList<Direccion>>> Tcs;
        }

        /// <summary>Calles guardadas falsas: responden ya con esta lista (Calles) o dejan la petición pendiente (null).</summary>
        class CallesFalsas : ICallesGuardadas
        {
            public Func<string, Resultado<IReadOnlyList<Direccion>>> Responder;
            public readonly List<Peticion> Peticiones = new List<Peticion>();

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct)
            {
                var p = new Peticion { Consulta = consulta, Ct = ct, Tcs = new TaskCompletionSource<Resultado<IReadOnlyList<Direccion>>>() };
                Peticiones.Add(p);
                if (Responder != null) p.Tcs.SetResult(Responder(consulta));
                return p.Tcs.Task;
            }
        }

        static Resultado<IReadOnlyList<Direccion>> Unas(params Direccion[] d) => Resultado<IReadOnlyList<Direccion>>.Exito(d);
        static Resultado<IReadOnlyList<Direccion>> SinRed() => Resultado<IReadOnlyList<Direccion>>.Fallo("sin conexión", Array.Empty<Direccion>());

        static readonly Direccion Farinas = new Direccion("Calle Fariñas", Sanlucar, M(0, 300), calle: "Calle Fariñas");
        static readonly Direccion Ancha = new Direccion("Calle Ancha", Sanlucar, M(0, 200), calle: "Calle Ancha");

        [Test]
        public void Buscador_AvisaConLaDistanciaYSoloDeLaUltima()
        {
            var falsas = new CallesFalsas();
            var b = new BuscadorCalles(falsas);
            var avisos = new List<(string, Resultado<IReadOnlyList<ResultadoBusqueda>>)>();
            b.Cambiado += (q, r) => avisos.Add((q, r));
            Assert.IsTrue(b.Pedir("far", Cabildo));
            Assert.IsTrue(b.Pedir("fari", Cabildo));
            Assert.IsTrue(falsas.Peticiones[0].Ct.IsCancellationRequested);
            falsas.Peticiones[0].Tcs.SetResult(Unas(Ancha)); // la vieja acaba tarde
            Assert.IsEmpty(avisos);
            falsas.Peticiones[1].Tcs.SetResult(Unas(Farinas));
            Assert.AreEqual("fari", avisos.Single().Item1);
            var r = avisos[0].Item2.Valor.Single();
            Assert.AreEqual(TipoResultado.Direccion, r.Tipo);
            Assert.AreEqual(300, r.Metros, 1);
            Assert.IsFalse(b.Pendiente);
        }

        [Test]
        public void Buscador_CortoNoBuscaYCancelaLaAnterior()
        {
            var falsas = new CallesFalsas();
            var b = new BuscadorCalles(falsas);
            b.Pedir("far", Cabildo);
            Assert.IsFalse(b.Pedir("fa", Cabildo));
            Assert.IsFalse(b.Pendiente);
            Assert.IsTrue(falsas.Peticiones.Single().Ct.IsCancellationRequested);
        }

        [Test]
        public void Buscador_SiElPuertoLanzaEsUnFallo()
        {
            var b = new BuscadorCalles(new CallesFalsas { Responder = _ => throw new IOException("disco") });
            Resultado<IReadOnlyList<ResultadoBusqueda>>? aviso = null;
            b.Cambiado += (_, r) => aviso = r;
            Assert.DoesNotThrow(() => b.Pedir("calle", Cabildo));
            Assert.IsFalse(aviso.Value.Ok);
            Assert.IsEmpty(aviso.Value.Valor);
        }

        /// <summary>Contexto que guarda lo que se le manda hasta que la prueba lo suelta (como el de Unity, en su hilo).</summary>
        class ContextoCola : SynchronizationContext
        {
            readonly System.Collections.Concurrent.ConcurrentQueue<(SendOrPostCallback, object)> cola =
                new System.Collections.Concurrent.ConcurrentQueue<(SendOrPostCallback, object)>();
            public bool Hay => !cola.IsEmpty;
            public override void Post(SendOrPostCallback d, object estado) => cola.Enqueue((d, estado));
            public void Soltar() { while (cola.TryDequeue(out var x)) x.Item1(x.Item2); }
        }

        /// <summary>Calles que contestan desde otro hilo, cuando la prueba abre la puerta.</summary>
        class CallesEnOtroHilo : ICallesGuardadas
        {
            public readonly ManualResetEventSlim Puerta = new ManualResetEventSlim();
            public int Hilo;

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct) => Task.Run(() =>
            {
                Puerta.Wait(5000);
                Hilo = Thread.CurrentThread.ManagedThreadId;
                return Unas(Farinas);
            });
        }

        [Test]
        public void Buscador_AvisaEnElHiloQuePidio()
        {
            var contexto = new ContextoCola();
            SynchronizationContext.SetSynchronizationContext(contexto); // Limpiar repone el de antes
            var otro = new CallesEnOtroHilo();
            var b = new BuscadorCalles(otro);
            int hiloAviso = 0;
            b.Cambiado += (_, __) => hiloAviso = Thread.CurrentThread.ManagedThreadId;
            b.Pedir("fari", Cabildo);
            otro.Puerta.Set();
            Assert.IsTrue(SpinWait.SpinUntil(() => contexto.Hay, 5000), "La respuesta vuelve por el contexto.");
            Assert.AreEqual(0, hiloAviso, "No avisa desde el hilo del disco.");
            contexto.Soltar();
            Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, hiloAviso);
            Assert.AreNotEqual(otro.Hilo, hiloAviso);
        }

        [Test]
        public void Buscador_SiElPuertoSeCancelaSoloEsUnFallo()
        {
            var falsas = new CallesFalsas();
            var b = new BuscadorCalles(falsas);
            Resultado<IReadOnlyList<ResultadoBusqueda>>? aviso = null;
            b.Cambiado += (_, r) => aviso = r;
            b.Pedir("fari", Cabildo);
            falsas.Peticiones.Single().Tcs.SetCanceled();
            Assert.IsFalse(aviso.Value.Ok, "Si no, la pantalla esperaría para siempre.");
            Assert.IsFalse(b.Pendiente);
        }

        // ───────────────────────── Pantalla ─────────────────────────

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class MapaFalso : IMapa
        {
            public readonly List<Coordenada> centrados = new List<Coordenada>();
            public void Centrar(Coordenada destino) => centrados.Add(destino);
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) => centrados.Add(destino);
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada centro = Cabildo;
            public Coordenada Centro => centro;
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

        class GeoFalso : IGeocodificador
        {
            public Func<string, Resultado<IReadOnlyList<Direccion>>> Responder = _ => Unas();
            public int Inversas;
            public readonly List<string> Consultas = new List<string>();

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct)
            {
                Consultas.Add(consulta);
                return Task.FromResult(Responder(consulta));
            }

            public Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct)
            {
                Inversas++;
                return Task.FromResult(Resultado<Direccion>.Fallo("sin conexión"));
            }
        }

        class RutasFalsas : IRutas
        {
            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct) =>
                Task.FromResult(Resultado<Ruta>.Fallo("sin conexión"));
        }

        GameObject objeto;
        PantallaMapa pantalla;
        HojaBuscador hoja;
        MapaFalso mapa;
        GeoFalso geo;
        CallesFalsas falsas;
        TaskCompletionSource<bool> espera; // la de Photon; null: no espera
        List<string> anuncios;
        Action<string> oir;

        static Lugar BarPepe() => new Lugar("bar-pepe", "Bar Pepe", "Bar", Plantilla.Hosteleria, M(0, 100), "Calle Ancha, 10");

        void CrearPantalla(params Lugar[] lugares)
        {
            Textos.Forzados = T;
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var servicioLugares = new ServicioLugares(new Repositorio(lugares));
            Assert.IsTrue(servicioLugares.Cargar().Ok);
            mapa = new MapaFalso();
            geo = new GeoFalso();
            falsas = new CallesFalsas { Responder = Responder };
            Task Esperar(TimeSpan _, CancellationToken ct)
            {
                if (espera == null) return Task.CompletedTask;
                var t = espera = new TaskCompletionSource<bool>();
                ct.Register(() => t.TrySetCanceled());
                return t.Task;
            }
            pantalla.Inicializar(mapa, tema, servicioLugares, Ahora, arbol, filtro,
                busqueda: new ServicioBusqueda(servicioLugares, mapa, Ahora),
                direcciones: new BuscadorDirecciones(geo, Esperar), geocodificador: geo,
                rutas: new ServicioRutas(new RutasFalsas(), null, (_, __) => Task.CompletedTask), calles: new BuscadorCalles(falsas));
            tema.Iniciar();
            filtro.Iniciar();
            hoja = pantalla.Buscador;
            hoja.Medir(800f);
        }

        /// <summary>Calle Fariñas y Calle Ancha con la regla de verdad, en esas posiciones.</summary>
        static Resultado<IReadOnlyList<Direccion>> Responder(string consulta)
        {
            var formas = ReglaCalles.Formas(consulta);
            return Unas(new[] { Ancha, Farinas }.Where(c => ReglaCalles.Puntuar(formas, c.Nombre, c.Localidad) > 0).ToArray());
        }

        void ConLector()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            anuncios = new List<string>();
            oir = anuncios.Add;
            AccesibilidadPantalla.AnuncioPedido += oir;
        }

        void AbrirYEscribir(string texto)
        {
            hoja.Abrir();
            pantalla.ActualizarCapas(0f);
            hoja.Escribir(texto);
        }

        string[] Filas => hoja.Filas.Select(f => f.Nombre.text).ToArray();

        [Test]
        public void Pantalla_PhotonNoLaDa_FilaDeDireccionYFicha()
        {
            CrearPantalla();
            AbrirYEscribir("calle fariñas");
            var fila = hoja.Filas.Single();
            Assert.AreEqual("Calle Fariñas", fila.Nombre.text);
            Assert.AreEqual("Dirección · Sanlúcar de Barrameda · a 300\u00A0m", fila.Detalle.text);
            Assert.AreEqual("Calle Fariñas, Dirección, Sanlúcar de Barrameda, a 300\u00A0m", fila.Anuncio);
            Assert.IsTrue(fila.Icono.ClassListContains("buscador__resultado-icono--direccion"));
            Assert.IsFalse(hoja.SinResultadosVisible, "Con solo calles no sale «No hay resultados».");
            Assert.IsFalse(hoja.BuscarDireccionVisible);

            pantalla.ElegirResultado(fila.Resultado);
            Assert.AreEqual(Farinas.Posicion, pantalla.PuntoAbierto);
            Assert.AreEqual("Calle Fariñas", pantalla.Ficha.Titulo.text);
            Assert.AreEqual(Sanlucar, pantalla.Ficha.Direccion.text);
            Assert.IsTrue(pantalla.Ficha.AccionesVisibles, "Con su Ir.");
            Assert.AreEqual(0, geo.Inversas, "Sin preguntar a Photon.");
        }

        [Test]
        public void Pantalla_OrdenLocalesCallesYPhoton()
        {
            CrearPantalla(BarPepe());
            geo.Responder = _ => Unas(new Direccion("Calle Ancha", "Cádiz", M(40000, 0)));
            AbrirYEscribir("calle ancha");
            CollectionAssert.AreEqual(new[] { "Bar Pepe", "Calle Ancha", "Calle Ancha" }, Filas);
            Assert.AreEqual(Sanlucar, hoja.Filas[1].Resultado.Direccion.Localidad);
            Assert.AreEqual("Cádiz", hoja.Filas[2].Resultado.Direccion.Localidad);
            Assert.AreEqual(1, hoja.DireccionesVisibles);
        }

        [Test]
        public void Pantalla_SalenAlMomentoSinEsperarAPhoton()
        {
            CrearPantalla();
            espera = new TaskCompletionSource<bool>();
            AbrirYEscribir("ancha");
            CollectionAssert.AreEqual(new[] { "Calle Ancha" }, Filas);
            Assert.IsFalse(hoja.SinResultadosVisible);
            Assert.IsFalse(hoja.BuscarDireccionVisible);
            Assert.IsEmpty(geo.Consultas);
        }

        [Test]
        public void Pantalla_SinConexion_LasCallesYElAviso()
        {
            CrearPantalla();
            geo.Responder = _ => SinRed();
            AbrirYEscribir("ancha");
            CollectionAssert.AreEqual(new[] { "Calle Ancha" }, Filas);
            Assert.IsTrue(hoja.AvisoDireccionesVisible);
            Assert.AreEqual("Las direcciones necesitan conexión", hoja.AvisoDirecciones.text);
            Assert.IsFalse(hoja.SinResultadosVisible);
            Assert.IsFalse(hoja.BuscarDireccionVisible);
        }

        [Test]
        public void Pantalla_Repetida_UnaSolaFila()
        {
            CrearPantalla();
            geo.Responder = _ => Unas(new Direccion("Calle Ancha", Sanlucar, M(0, 500)));
            AbrirYEscribir("calle ancha");
            CollectionAssert.AreEqual(new[] { "Calle Ancha" }, Filas);
            Assert.AreEqual(Ancha.Posicion, hoja.Filas[0].Resultado.Posicion, "Se queda la guardada, que ya se veía.");
        }

        [Test]
        public void Pantalla_ConNumeroEnPhoton_EsaFilaYLaDeLaCalle()
        {
            CrearPantalla();
            geo.Responder = _ => Unas(new Direccion("Calle Ancha 10", Sanlucar, M(0, 250), "Calle Ancha", "10"));
            AbrirYEscribir("calle ancha 10");
            CollectionAssert.AreEqual(new[] { "Calle Ancha", "Calle Ancha 10" }, Filas);
            Assert.AreEqual("calle ancha 10", geo.Consultas.Single(), "A Photon se le pregunta lo mismo de siempre.");
        }

        [Test]
        public void Pantalla_NumeroQuePhotonNoDa_LaCalleSinNumero()
        {
            CrearPantalla();
            AbrirYEscribir("calle fariñas 12");
            CollectionAssert.AreEqual(new[] { "Calle Fariñas" }, Filas);
            Assert.AreEqual(Farinas.Posicion, hoja.Filas[0].Resultado.Posicion);
        }

        [Test]
        public void Pantalla_Abreviatura()
        {
            CrearPantalla();
            AbrirYEscribir("c/ fariñas");
            CollectionAssert.AreEqual(new[] { "Calle Fariñas" }, Filas);
        }

        [Test]
        public void Pantalla_TextoCorto_Ninguna()
        {
            CrearPantalla();
            AbrirYEscribir("far");
            Assert.AreEqual(1, hoja.Filas.Count);
            hoja.Escribir("fa");
            Assert.IsEmpty(hoja.Filas);
            Assert.AreEqual(1, falsas.Peticiones.Count);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.IsTrue(hoja.BuscarDireccionVisible);
        }

        [Test]
        public void Pantalla_SinTeselas_ComoHoy()
        {
            CrearPantalla();
            falsas.Responder = _ => Unas();
            geo.Responder = _ => SinRed();
            AbrirYEscribir("calle ancha");
            Assert.IsEmpty(hoja.Filas);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.IsTrue(hoja.BuscarDireccionVisible);
            Assert.IsTrue(hoja.AvisoDireccionesVisible);
        }

        [Test]
        public void Pantalla_TextoCambia_LaViejaNoSePinta()
        {
            CrearPantalla();
            falsas.Responder = null;
            AbrirYEscribir("fari");
            hoja.Escribir("anch");
            falsas.Peticiones[0].Tcs.SetResult(Unas(Farinas));
            Assert.IsEmpty(hoja.Filas);
            falsas.Peticiones[1].Tcs.SetResult(Unas(Ancha));
            CollectionAssert.AreEqual(new[] { "Calle Ancha" }, Filas);
        }

        [Test]
        public void Pantalla_AlSeguirEscribiendoLasQueCoincidenNoParpadean()
        {
            CrearPantalla();
            AbrirYEscribir("calle");
            CollectionAssert.AreEqual(new[] { "Calle Ancha", "Calle Fariñas" }, Filas);
            falsas.Responder = null; // la siguiente tarda
            hoja.Escribir("calle f");
            CollectionAssert.AreEqual(new[] { "Calle Fariñas" }, Filas, "Solo las que coinciden con el texto nuevo.");
            var fila = hoja.Filas[0];
            falsas.Peticiones.Last().Tcs.SetResult(Unas(Farinas));
            Assert.AreSame(fila, hoja.Filas.Single(), "Llega lo mismo: la fila no se rehace.");
        }

        [TestCase("calle")]
        [TestCase("ninguna")]
        [TestCase("fallo")]
        public void Pantalla_MientrasSeBuscanNoSeDiceQueNoHayResultados(string respuesta)
        {
            CrearPantalla();
            ConLector();
            falsas.Responder = null; // contesta después
            geo.Responder = _ => SinRed();
            AbrirYEscribir("fari");
            Assert.IsEmpty(hoja.Filas);
            Assert.IsFalse(hoja.SinResultadosVisible);
            Assert.IsFalse(hoja.BuscarDireccionVisible);
            Assert.IsFalse(anuncios.Any(a => a.Contains("No hay resultados")), string.Join(" / ", anuncios));
            if (respuesta == "fallo") LogAssert.Expect(LogType.Warning, new Regex("calles guardadas no disponibles"));
            falsas.Peticiones.Last().Tcs.SetResult(respuesta == "calle" ? Unas(Farinas)
                : respuesta == "ninguna" ? Unas() : Resultado<IReadOnlyList<Direccion>>.Fallo("disco"));
            bool hay = respuesta == "calle";
            Assert.AreEqual(hay ? 1 : 0, hoja.Filas.Count);
            Assert.AreEqual(!hay, hoja.SinResultadosVisible);
            Assert.AreEqual(!hay, hoja.BuscarDireccionVisible);
            Assert.AreEqual(hay ? "1 resultado" : "No hay resultados", anuncios.Last());
            Assert.AreEqual(hay ? 0 : 1, anuncios.Count(a => a.Contains("No hay resultados")));
        }

        [Test]
        public void Pantalla_SiLleganMasLaQueSigueConservaSuFila()
        {
            CrearPantalla();
            ConLector();
            falsas.Responder = _ => Unas(Ancha);
            AbrirYEscribir("calle");
            falsas.Responder = null;
            hoja.Escribir("calle "); // sigue la que se veía mientras llega la respuesta
            var fila = hoja.Filas.Single();
            pantalla.Accesibilidad.PedirFoco(fila);
            falsas.Peticiones.Last().Tcs.SetResult(Unas(new Direccion(Ancha.Nombre, Ancha.Localidad, Ancha.Posicion, calle: Ancha.Nombre), Farinas));
            CollectionAssert.AreEqual(new[] { "Calle Ancha", "Calle Fariñas" }, Filas);
            Assert.AreSame(fila, hoja.Filas[0]);
            Assert.AreSame(fila, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void Hoja_SiCambiaElOrdenLasFilasSeMueven()
        {
            CrearPantalla();
            AbrirYEscribir("calle");
            var a = R("A", M(0, 1));
            var b = R("B", M(0, 2));
            var c = R("C", M(0, 3));
            var locales = new List<ResultadoBusqueda>();
            hoja.MostrarResultados(locales, calles: new[] { a, b });
            var filaA = hoja.Filas[0];
            var filaB = hoja.Filas[1];
            hoja.MostrarResultados(locales, calles: new[] { b, c, a });
            CollectionAssert.AreEqual(new[] { "B", "C", "A" }, Filas);
            Assert.AreSame(filaB, hoja.Filas[0]);
            Assert.AreSame(filaA, hoja.Filas[2]);
            CollectionAssert.AreEqual(hoja.Filas.ToArray(), hoja.Resultados.Children().ToArray(), "En pantalla, en el mismo orden.");
        }

        [Test]
        public void Pantalla_ConLaLocalidadAlSeguirEscribiendoNoDesaparece()
        {
            CrearPantalla();
            AbrirYEscribir("fariñas");
            falsas.Responder = null;
            hoja.Escribir("fariñas sanl");
            CollectionAssert.AreEqual(new[] { "Calle Fariñas" }, Filas);
        }

        [Test]
        public void Pantalla_SiFallanNoHayCallesNiExcepcion()
        {
            CrearPantalla();
            falsas.Responder = _ => Resultado<IReadOnlyList<Direccion>>.Fallo("disco");
            LogAssert.Expect(LogType.Warning, new Regex("calles guardadas no disponibles"));
            AbrirYEscribir("calle ancha");
            Assert.IsEmpty(hoja.Filas);
        }

        [Test]
        public void Pantalla_BorrarCategoriaYEnlaceCancelan()
        {
            CrearPantalla();
            falsas.Responder = null;
            AbrirYEscribir("fari");
            pantalla.BorrarBusqueda();
            Assert.IsFalse(pantalla.Calles.Pendiente);
            hoja.Escribir("fari");
            pantalla.ElegirCategoria(CategoriaPoi.Salud);
            Assert.IsFalse(pantalla.Calles.Pendiente);
            pantalla.QuitarCategoria();
            Assert.IsTrue(pantalla.Calles.Pendiente, "Vuelve la búsqueda.");
            hoja.Escribir("36.7781, -6.3515");
            Assert.IsFalse(pantalla.Calles.Pendiente);
            Assert.AreEqual(TipoResultado.Coordenada, hoja.Filas.Single().Resultado.Tipo);
        }

        [Test]
        public void Pantalla_AlVolverADesplegarCuentanLasTeselasNuevasYLaDistancia()
        {
            CrearPantalla();
            falsas.Responder = _ => Unas();
            AbrirYEscribir("fari");
            Assert.IsEmpty(hoja.Filas);
            pantalla.PlegarBuscador();
            falsas.Responder = Responder; // el mapa ha guardado la tesela
            hoja.Abrir();
            pantalla.Buscar(hoja.Campo.value); // lo que hace la pantalla al desplegar la hoja
            Assert.AreEqual(300, hoja.Filas.Single().Resultado.Metros, 1);
            pantalla.PlegarBuscador();
            mapa.centro = Farinas.Posicion;
            hoja.Abrir();
            pantalla.Buscar(hoja.Campo.value);
            Assert.AreEqual(0, hoja.Filas.Single().Resultado.Metros, 1);
        }

        [Test]
        public void TalkBack_CuentaYAnunciaLasCalles()
        {
            CrearPantalla(BarPepe());
            ConLector();
            AbrirYEscribir("calle ancha");
            Assert.AreEqual("2 resultados", anuncios.Last());
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.AreEqual(recorrido.IndexOf(hoja.Filas[0].Anuncio) + 1, recorrido.IndexOf("Calle Ancha, Dirección, Sanlúcar de Barrameda, a 200\u00A0m"));
        }

        [Test]
        public void TalkBack_AlLlegarPhotonElFocoSigueEnLaCalle()
        {
            CrearPantalla();
            ConLector();
            espera = new TaskCompletionSource<bool>();
            geo.Responder = _ => Unas(new Direccion("Calle Ancha", "Cádiz", M(40000, 0)));
            AbrirYEscribir("calle ancha");
            Assert.AreEqual("1 resultado", anuncios.Last());
            var fila = hoja.Filas[0];
            pantalla.Accesibilidad.PedirFoco(fila);
            espera.SetResult(true);
            Assert.AreSame(fila, hoja.Filas[0], "La fila de la calle no se rehace.");
            Assert.AreEqual(2, hoja.Filas.Count);
            Assert.AreSame(fila, pantalla.Accesibilidad.Enfocado);
            Assert.AreEqual("2 resultados", anuncios.Last());
        }

        [Test]
        public void TalkBack_SiLasCallesLleganDespuesElFocoSigueEnLaDireccion()
        {
            CrearPantalla();
            ConLector();
            falsas.Responder = null;
            geo.Responder = _ => Unas(new Direccion("Calle Ancha 10", Sanlucar, M(0, 250)));
            AbrirYEscribir("calle ancha 10");
            var fila = hoja.Filas.Single();
            pantalla.Accesibilidad.PedirFoco(fila);
            falsas.Peticiones.Single().Tcs.SetResult(Unas(Ancha));
            CollectionAssert.AreEqual(new[] { "Calle Ancha", "Calle Ancha 10" }, Filas);
            Assert.AreSame(fila, hoja.Filas[1]);
            Assert.AreEqual(1, hoja.Resultados.IndexOf(fila), "También en pantalla va detrás.");
            Assert.AreSame(fila, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ElArranqueLeDaLasCallesALaPantalla()
        {
            var otro = new GameObject("Otra pantalla");
            try
            {
                var p = otro.AddComponent<PantallaMapa>();
                var prefs = new PreferenciasFalsas();
                var c = new BuscadorCalles(new CallesFalsas());
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), p, new ServicioTema(prefs, new SistemaClaro()),
                    new ServicioLugares(new Repositorio(BarPepe())), new ServicioFiltro(prefs), Ahora, calles: c));
                Assert.AreSame(c, p.Calles);
            }
            finally { UnityEngine.Object.DestroyImmediate(otro); }
        }
    }
}
