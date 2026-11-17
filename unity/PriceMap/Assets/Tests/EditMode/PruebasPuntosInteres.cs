using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Historia 1.6 (parte 1): puntos de interés de OpenStreetMap en el mapa. La regla del Dominio
    /// (categoría, ocultos, sanidad pública), la lectura de la capa poi y su id, la lista del mapa por
    /// teselas y la capa de la UI (umbral de distancia y visibilidad).
    /// </summary>
    public class PruebasPuntosInteres
    {
        // ───────────────────────── Dobles ─────────────────────────

        class MapaFalso : IMapa
        {
            public readonly List<PuntoInteres> puntos = new List<PuntoInteres>();
            public readonly Dictionary<Coordenada, PuntoPantalla> proyecciones = new Dictionary<Coordenada, PuntoPantalla>();
            public PuntoPantalla porDefecto = new PuntoPantalla(500f, 900f, 300f);
            public float distancia = 900f;
            public void Centrar(Coordenada destino) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => proyecciones.TryGetValue(punto, out var p) ? p : porDefecto;
            public IReadOnlyList<PuntoInteres> PuntosInteres => puntos;
            public event Action PuntosInteresCambiados;
            public float DistanciaCamara => distancia;
            public void Cambiar() => PuntosInteresCambiados?.Invoke();
        }

        const float Ancho = 1080f, Alto = 1920f;

        static PuntoInteres Punto(string clase, string subclase, string nombre = "Punto", double lat = 36.7779, ulong id = 1) =>
            new PuntoInteres(id, nombre, clase, subclase, 10, new Coordenada(lat, -6.3528));

        static (CapaPuntosInteres capa, MapaFalso mapa) Capa(params PuntoInteres[] puntos)
        {
            var mapa = new MapaFalso();
            mapa.puntos.AddRange(puntos);
            return (new CapaPuntosInteres(new VisualElement(), mapa), mapa);
        }

        // ───────────────────────── Regla del Dominio (matriz) ─────────────────────────

        [Test]
        public void SupermercadoEsSuperConCesta()
        {
            Assert.AreEqual(CategoriaPoi.Super, ReglaPuntosInteres.Categoria("grocery", "supermarket"));
            var (capa, _) = Capa(Punto("grocery", "supermarket", "Mercadona"));
            var v = capa.Vistas.Single();
            Assert.AreEqual(CategoriaPoi.Super, v.Categoria);
            Assert.AreEqual("basket", v.Glifo);
            Assert.AreEqual("Mercadona", v.Nombre.text);
            Assert.IsTrue(v.ClassListContains("poi--super"));
        }

        [Test]
        public void LaSubclaseMandaSobreLaClase()
        {
            Assert.AreEqual(CategoriaPoi.Carga, ReglaPuntosInteres.Categoria("fuel", "charging_station"));
            Assert.AreEqual("zap", IconosLugar.GlifoDe(CategoriaPoi.Carga));
            Assert.AreEqual(CategoriaPoi.Gasolina, ReglaPuntosInteres.Categoria("fuel", "fuel"));
        }

        [TestCase("bus", "bus_stop")]
        [TestCase("office", "office")]
        [TestCase("restaurant", "office")]   // aunque la clase tenga categoría
        public void ParadasYOficinasNoSeDibujan(string clase, string subclase)
        {
            Assert.AreEqual(CategoriaPoi.Ninguna, ReglaPuntosInteres.Categoria(clase, subclase));
            Assert.IsFalse(ReglaPuntosInteres.SeDibuja(clase, subclase));
            Assert.AreEqual(0, Capa(Punto(clase, subclase)).capa.Vistas.Count);
        }

        [TestCase("xyz", "")]
        [TestCase(null, null)]
        [TestCase("", "zzz")]
        public void SinCategoriaNoSeDibuja(string clase, string subclase)
        {
            Assert.AreEqual(CategoriaPoi.Ninguna, ReglaPuntosInteres.Categoria(clase, subclase));
            Assert.AreEqual(0, Capa(Punto(clase, subclase)).capa.Vistas.Count);
        }

        [Test]
        public void CentroDeSaludEsSanidadPublica()
        {
            var cat = ReglaPuntosInteres.Categoria("hospital", null);
            Assert.AreEqual(CategoriaPoi.Salud, cat);
            Assert.IsTrue(ReglaPuntosInteres.EsSanidadPublica(cat, "Centro de Salud Barrio Bajo"));
            foreach (var n in new[] { "Consultorio Bonanza", "Urgencias", "Hospital Virgen del Camino", "Ambulatorio", "CHARE Sanlúcar" })
                Assert.IsTrue(ReglaPuntosInteres.EsSanidadPublica(CategoriaPoi.Salud, n), n);
            Assert.IsFalse(ReglaPuntosInteres.EsSanidadPublica(CategoriaPoi.Salud, "Clínica Dental Sonrisa"));
            Assert.IsFalse(ReglaPuntosInteres.EsSanidadPublica(CategoriaPoi.Salud, ""));
            // Solo en la categoría salud
            Assert.IsFalse(ReglaPuntosInteres.EsSanidadPublica(CategoriaPoi.Cafe, "Cafetería del Hospital"));

            var v = Capa(Punto("hospital", null, "Centro de Salud Barrio Bajo")).capa.Vistas.Single();
            Assert.IsTrue(v.SanidadPublica);
            Assert.IsTrue(v.ClassListContains("poi--sanidad-publica"));
        }

        [TestCase("veterinary", null, "Hospital Veterinario Sanlúcar", false)]
        [TestCase("doctors", "veterinary", "Urgencias Veterinarias", false)]
        [TestCase("hospital", null, "Centro de Salud Barrio Bajo", true)]
        [TestCase("doctors", null, "Consulta Muchares", false)]   // "chare" dentro de otra palabra
        [TestCase("doctors", null, "Hospitalillo San Juan", false)]
        [TestCase("hospital", null, "CHARE de Sanlúcar", true)]
        public void SanidadPublicaPorPalabraYNuncaVeterinarios(string clase, string subclase, string nombre, bool publica)
        {
            var cat = ReglaPuntosInteres.Categoria(clase, subclase);
            Assert.AreEqual(CategoriaPoi.Salud, cat);
            Assert.AreEqual(publica, ReglaPuntosInteres.EsSanidadPublica(cat, nombre, clase, subclase), nombre);
            Assert.AreEqual(publica, Capa(Punto(clase, subclase, nombre)).capa.Vistas.Single().SanidadPublica, nombre);
        }

        [Test]
        public void SinIdElNombreDelElementoNoRepitePoi0()
        {
            var (capa, _) = Capa(Punto("bank", null, "A", id: 0), Punto("bank", null, "B", id: 77));
            Assert.AreEqual("poi-sin-id", capa.Vistas[0].name);
            Assert.AreEqual("poi-77", capa.Vistas[1].name);
        }

        [Test]
        public void TodasLasCategoriasTienenGlifoPropio()
        {
            foreach (CategoriaPoi c in Enum.GetValues(typeof(CategoriaPoi)))
            {
                if (c == CategoriaPoi.Ninguna) continue;
                var g = IconosLugar.GlifoDe(c);
                Assert.AreNotEqual(IconosLugar.Generico, g, c.ToString());
                CollectionAssert.Contains(IconosLugar.Glifos.ToList(), g, c.ToString());
            }
        }

        [Test]
        public void CadaCategoriaTieneSusTokensDeColor()
        {
            var tokens = File.ReadAllText(Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            foreach (CategoriaPoi c in Enum.GetValues(typeof(CategoriaPoi)))
            {
                if (c == CategoriaPoi.Ninguna) continue;
                var n = c.ToString().ToLowerInvariant();
                StringAssert.Contains($"--color-poi-{n}:", tokens, c.ToString());
                StringAssert.Contains($"--color-poi-{n}-texto:", tokens, c.ToString());
                StringAssert.Contains($".{PuntoInteresVista.ClaseCategoria(c)} .poi__icono", uss, c.ToString());
                StringAssert.Contains($".{PuntoInteresVista.ClaseCategoria(c)} .poi__nombre", uss, c.ToString());
            }
        }

        // ───────────────────────── Vista ─────────────────────────

        [Test]
        public void SinNombreSoloElIcono()
        {
            var v = Capa(Punto("pharmacy", null, "")).capa.Vistas.Single();
            Assert.IsNull(v.Nombre);
            Assert.IsNotNull(v.Icono);
            Assert.AreEqual("pill", v.Glifo);
        }

        [Test]
        public void ElNombreEnCastellanoMandaSobreElNombre()
        {
            var e = new ElementoMVT();
            e.atributos["name"] = "X";
            e.atributos["name:es"] = "Y";
            Assert.AreEqual("Y", MapaVectorial.NombreDe(e));
            e.atributos["name:es"] = " ";
            Assert.AreEqual("X", MapaVectorial.NombreDe(e));
            e.atributos.Clear();
            Assert.AreEqual("", MapaVectorial.NombreDe(e));
        }

        // ───────────────────────── Capa: distancia y pantalla ─────────────────────────

        [Test]
        public void CercaYEnPantallaSeVe()
        {
            var (capa, _) = Capa(Punto("bank", null, "Banco"));
            capa.Actualizar(1f, Ancho, Alto);
            var v = capa.Vistas.Single();
            Assert.IsTrue(v.Visible);
            Assert.AreEqual(DisplayStyle.Flex, v.style.display.value);
            Assert.AreEqual(new Vector2(500f, 900f), v.Posicion);
            Assert.AreEqual(1, capa.Visibles);
        }

        [Test]
        public void LejosNoSeVeNingunPunto()
        {
            var (capa, mapa) = Capa(Punto("bank", null), Punto("cafe", null, lat: 36.778));
            mapa.distancia = 2000f;
            capa.Actualizar(1f, Ancho, Alto);
            Assert.AreEqual(0, capa.Visibles);

            mapa.distancia = CapaPuntosInteres.DistanciaMaxima; // justo en el umbral, sí
            capa.Actualizar(1f, Ancho, Alto);
            Assert.AreEqual(2, capa.Visibles);

            mapa.distancia = 900f; // la cámara al arrancar
            capa.Actualizar(1f, Ancho, Alto);
            Assert.AreEqual(2, capa.Visibles);
        }

        [Test]
        public void FueraDePantallaMasAllaDelMargenSeOculta()
        {
            var dentro = Punto("bank", null, lat: 36.70);
            var margen = Punto("bank", null, lat: 36.71);
            var fuera = Punto("bank", null, lat: 36.72);
            var abajo = Punto("bank", null, lat: 36.73);
            var detras = Punto("bank", null, lat: 36.74);
            var (capa, mapa) = Capa(dentro, margen, fuera, abajo, detras);
            mapa.proyecciones[dentro.Posicion] = new PuntoPantalla(10f, 10f, 100f);
            mapa.proyecciones[margen.Posicion] = new PuntoPantalla(-CapaPuntosInteres.MargenDp + 1f, 500f, 100f);
            mapa.proyecciones[fuera.Posicion] = new PuntoPantalla(-CapaPuntosInteres.MargenDp - 1f, 500f, 100f);
            mapa.proyecciones[abajo.Posicion] = new PuntoPantalla(500f, Alto + CapaPuntosInteres.MargenDp + 1f, 100f);
            mapa.proyecciones[detras.Posicion] = new PuntoPantalla(500f, 500f, -5f);

            capa.Actualizar(1f, Ancho, Alto);

            bool Ver(PuntoInteres p) => capa.Vistas.Single(v => v.Punto == p).Visible;
            Assert.IsTrue(Ver(dentro));
            Assert.IsTrue(Ver(margen), "Dentro del margen aún se coloca.");
            Assert.IsFalse(Ver(fuera));
            Assert.IsFalse(Ver(abajo));
            Assert.IsFalse(Ver(detras), "Detrás de la cámara no se ve.");
        }

        [Test]
        public void AlLiberarseLaTeselaElPuntoDesaparece()
        {
            var a = Punto("bank", null, "A", 36.70);
            var b = Punto("cafe", null, "B", 36.71);
            var (capa, mapa) = Capa(a, b);
            Assert.AreEqual(2, capa.Contenedor.childCount);

            mapa.puntos.Remove(a);
            Assert.IsTrue(capa.Sincronizar());
            Assert.AreEqual(1, capa.Contenedor.childCount);
            Assert.AreSame(b, capa.Vistas.Single().Punto);

            Assert.IsFalse(capa.Sincronizar(), "Sin cambios en la lista, no se rehace nada.");
        }

        [Test]
        public void LosPuntosNoCapturanToques()
        {
            var v = Capa(Punto("grocery", "supermarket", "Mercadona")).capa.Vistas.Single();
            Assert.AreEqual(PickingMode.Ignore, v.pickingMode);
            Assert.AreEqual(PickingMode.Ignore, v.Icono.pickingMode);
            Assert.AreEqual(PickingMode.Ignore, v.Nombre.pickingMode);
        }

        // ───────────────────────── Pantalla ─────────────────────────

        [Test]
        public void LaCapaDePuntosVaDebajoDeLosMarcadores()
        {
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var raiz = arbol.Q("raiz");
            var puntos = raiz.Q("capa-puntos");
            Assert.IsNotNull(puntos);
            Assert.Less(raiz.IndexOf(puntos), raiz.IndexOf(raiz.Q("capa-marcadores")));
        }

        [Test]
        public void LaPantallaCreaLaCapaYSigueLosCambiosDelMapa()
        {
            var objeto = new GameObject("Pantalla de prueba");
            try
            {
                var pantalla = objeto.AddComponent<PantallaMapa>();
                var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
                var mapa = new MapaFalso();
                var tema = new ServicioTema(new PreferenciasEnMemoria(), new SistemaClaro());
                pantalla.Inicializar(mapa, tema, null, DateTime.UtcNow, arbol);
                Assert.IsNotNull(pantalla.CapaPuntos);
                Assert.AreEqual(0, pantalla.CapaPuntos.Vistas.Count);

                var mercadona = Punto("grocery", "supermarket", "Mercadona");
                mapa.puntos.Add(mercadona);
                mapa.Cambiar();
                Assert.AreEqual(1, pantalla.CapaPuntos.Vistas.Count);
                Assert.AreSame(arbol.Q("capa-puntos"), pantalla.CapaPuntos.Contenedor);

                // El paso de cada fotograma coloca el punto (cerca de la esquina: dentro con cualquier tamaño de pantalla)
                mapa.proyecciones[mercadona.Posicion] = new PuntoPantalla(5f, 6f, 100f);
                var vista = pantalla.CapaPuntos.Vistas[0];
                Assert.IsFalse(vista.Visible, "Hasta la primera actualización no se ve.");
                pantalla.ActualizarCapas(2f);
                Assert.IsTrue(vista.Visible);
                Assert.AreEqual(new Vector2(10f, 12f), vista.Posicion, "Píxeles × escala = dp del panel.");

                // Con la cámara lejos se oculta
                mapa.distancia = 2000f;
                pantalla.ActualizarCapas(2f);
                Assert.IsFalse(vista.Visible);
            }
            finally { UnityEngine.Object.DestroyImmediate(objeto); }
        }

        class PreferenciasEnMemoria : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        // ───────────────────────── Lectura de la tesela ─────────────────────────

        static void Varint(List<byte> b, ulong v)
        {
            while (v >= 0x80) { b.Add((byte)(v | 0x80)); v >>= 7; }
            b.Add((byte)v);
        }

        static void Clave(List<byte> b, int campo, int tipo) => Varint(b, (ulong)((campo << 3) | tipo));

        static void Bytes(List<byte> b, int campo, IReadOnlyCollection<byte> datos)
        {
            Clave(b, campo, 2);
            Varint(b, (ulong)datos.Count);
            b.AddRange(datos);
        }

        static void Texto(List<byte> b, int campo, string s) => Bytes(b, campo, Encoding.UTF8.GetBytes(s));

        static uint ZigZag(int n) => (uint)((n << 1) ^ (n >> 31));

        /// <summary>Elemento de punto con id, atributos (índices clave/valor) y una coordenada de tesela.</summary>
        static List<byte> Elemento(ulong id, int[] etiquetas, int x, int y)
        {
            var e = new List<byte>();
            Clave(e, 1, 0); Varint(e, id);
            var tags = new List<byte>(); foreach (var t in etiquetas) Varint(tags, (ulong)t);
            Bytes(e, 2, tags);
            Clave(e, 3, 0); Varint(e, 1); // Punto
            var geo = new List<byte>();
            Varint(geo, (1 << 3) | 1); Varint(geo, ZigZag(x)); Varint(geo, ZigZag(y)); // MoveTo(1)
            Bytes(e, 4, geo);
            return e;
        }

        /// <summary>Tesela MVT con una capa "poi": un supermercado con nombre en castellano y otro fuera de la tesela.</summary>
        static byte[] TeselaPoi()
        {
            var capa = new List<byte>();
            Clave(capa, 15, 0); Varint(capa, 2);
            Texto(capa, 1, "poi");
            Bytes(capa, 2, Elemento(4242424242UL, new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 }, 2048, 1024));
            Bytes(capa, 2, Elemento(7UL, new[] { 0, 0 }, -10, 100)); // en el margen: es de la tesela vecina
            foreach (var k in new[] { "class", "subclass", "name", "name:es", "rank" }) Texto(capa, 3, k);
            foreach (var v in new[] { "grocery", "supermarket", "Mercadona X", "Mercadona" })
            {
                var valor = new List<byte>(); Texto(valor, 1, v); Bytes(capa, 4, valor);
            }
            var rank = new List<byte>(); Clave(rank, 5, 0); Varint(rank, 12); Bytes(capa, 4, rank);
            Clave(capa, 5, 0); Varint(capa, 4096);
            var tesela = new List<byte>();
            Bytes(tesela, 3, capa);
            return tesela.ToArray();
        }

        [Test]
        public void ElLectorGuardaElIdDelElemento()
        {
            var capas = LectorMVT.Leer(TeselaPoi());
            var poi = capas["poi"];
            Assert.AreEqual(2, poi.elementos.Count);
            Assert.AreEqual(4242424242UL, poi.elementos[0].id);
            Assert.AreEqual(7UL, poi.elementos[1].id);
            Assert.AreEqual(TipoGeometria.Punto, poi.elementos[0].tipo);
            Assert.AreEqual("supermarket", poi.elementos[0].Texto("subclass"));
        }

        [Test]
        public void LaCapaPoiDaPuntosConCoordenadaYSoloLosDeLaTesela()
        {
            var capa = LectorMVT.Leer(TeselaPoi())["poi"];
            var puntos = MapaVectorial.PuntosDe(capa, p => new Coordenada(p.y, p.x));
            var p0 = puntos.Single();
            Assert.AreEqual(4242424242UL, p0.Id);
            Assert.AreEqual("Mercadona", p0.Nombre, "name:es manda sobre name.");
            Assert.AreEqual("grocery", p0.Clase);
            Assert.AreEqual("supermarket", p0.Subclase);
            Assert.AreEqual(12, p0.Rank);
            Assert.AreEqual(new Coordenada(1024, 2048), p0.Posicion);
        }

        // ───────────────────────── Mapa: lista por teselas ─────────────────────────

        [Test]
        public void ElMapaJuntaLosPuntosDeSusTeselasYLosQuitaAlLiberarlas()
        {
            double latOrigen = Geo.LatitudOrigen, lonOrigen = Geo.LongitudOrigen;
            Geo.LatitudOrigen = 36.7779; Geo.LongitudOrigen = -6.3528;
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            objCamara.AddComponent<CamaraMapa>();
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                var mapa = objMapa.AddComponent<MapaVectorial>();
                IMapa puerto = mapa;
                int avisos = 0;
                puerto.PuntosInteresCambiados += () => avisos++;
                puerto.Centrar(new Coordenada(36.7779, -6.3528));
                Assert.AreEqual(900f, puerto.DistanciaCamara, 0.01f, "Al arrancar la cámara está a 900 m.");

                var centro = Geo.Tesela(36.7779, -6.3528, mapa.zoom);
                mapa.SimularCargada(centro, new[] { Punto("grocery", "supermarket", "Mercadona") });
                mapa.SimularCargada(centro + Vector2Int.right, new[] { Punto("bank", null, "Banco"), Punto("bus", "bus_stop", "Parada") });
                Assert.AreEqual(3, puerto.PuntosInteres.Count);
                Assert.AreEqual(2, avisos);

                // Cinco teselas al este: se liberan todas y sus puntos desaparecen
                puerto.Centrar(new Coordenada(36.7779, -6.3528 + 5 * 360.0 / (1 << mapa.zoom)));
                mapa.ActualizarTeselas();
                Assert.AreEqual(0, mapa.TeselasCargadas);
                Assert.AreEqual(0, puerto.PuntosInteres.Count);
                Assert.Greater(avisos, 2, "Liberar una tesela con puntos avisa del cambio.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                UnityEngine.Object.DestroyImmediate(objCamara);
                Geo.LatitudOrigen = latOrigen; Geo.LongitudOrigen = lonOrigen;
            }
        }

        [Test]
        public void LosBytesDeLaTeselaLleganAlPuertoConSuCoordenadaReal()
        {
            double latOrigen = Geo.LatitudOrigen, lonOrigen = Geo.LongitudOrigen;
            Geo.LatitudOrigen = 36.7779; Geo.LongitudOrigen = -6.3528;
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                var mapa = objMapa.AddComponent<MapaVectorial>();
                IMapa puerto = mapa;
                int avisos = 0;
                puerto.PuntosInteresCambiados += () => avisos++;
                var t = Geo.Tesela(36.7779, -6.3528, mapa.zoom);

                Assert.IsTrue(mapa.ConstruirDesde(TeselaPoi(), t.x, t.y));

                Assert.AreEqual(1, mapa.TeselasCargadas);
                Assert.AreEqual(1, puerto.PuntosInteres.Count, "Solo el de dentro de la tesela.");
                Assert.AreEqual(1, avisos);
                // Esperado, sin pasar por Geo: el punto está en (2048, 1024) de 4096 → x + 0,5 y y + 0,25 en teselas XYZ
                double n = 1 << mapa.zoom, fx = t.x + 0.5, fy = t.y + 0.25;
                double lon = fx / n * 360.0 - 180.0;
                double lat = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * fy / n))) * 180.0 / Math.PI;
                var p = puerto.PuntosInteres[0];
                Assert.AreEqual("Mercadona", p.Nombre);
                double metrosLat = (p.Posicion.Latitud - lat) * 111320.0;
                double metrosLon = (p.Posicion.Longitud - lon) * 111320.0 * Math.Cos(lat * Math.PI / 180.0);
                Assert.Less(Math.Sqrt(metrosLat * metrosLat + metrosLon * metrosLon), 3.0, $"{p.Posicion} frente a {lat:F6}, {lon:F6}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                Geo.LatitudOrigen = latOrigen; Geo.LongitudOrigen = lonOrigen;
            }
        }

        [Test]
        public void UnaTeselaSinPuntosNoAvisa()
        {
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                var mapa = objMapa.AddComponent<MapaVectorial>();
                int avisos = 0;
                ((IMapa)mapa).PuntosInteresCambiados += () => avisos++;
                var vacia = new List<byte>();
                var capa = new List<byte>(); Texto(capa, 1, "water"); Clave(capa, 5, 0); Varint(capa, 4096);
                Bytes(vacia, 3, capa);
                Assert.IsTrue(mapa.ConstruirDesde(vacia.ToArray(), 8000, 6000));
                Assert.AreEqual(1, mapa.TeselasCargadas);
                Assert.AreEqual(0, avisos);
            }
            finally { UnityEngine.Object.DestroyImmediate(objMapa); }
        }

        [Test]
        public void UnaTeselaRealDeLaCacheDaPuntosConCategoria()
        {
            var t = Geo.Tesela(36.7779, -6.3528, 14);
            var ruta = Path.Combine(Application.persistentDataPath, "vectorial", "14", t.x.ToString(), t.y + ".pbf");
            if (!File.Exists(ruta)) Assert.Ignore($"No hay tesela en caché ({ruta}): se comprueba al pasar por el mapa o con la captura.");

            var capas = LectorMVT.Leer(File.ReadAllBytes(ruta));
            Assert.IsTrue(capas.TryGetValue("poi", out var poi), "La tesela del centro de Sanlúcar trae la capa poi.");
            var puntos = MapaVectorial.PuntosDe(poi, p => new Coordenada(p.y, p.x));
            var conCategoria = puntos.Where(p => ReglaPuntosInteres.SeDibuja(p.Clase, p.Subclase)).ToList();
            Assert.Greater(conCategoria.Count, 0, "Debe haber puntos con categoría en el centro de Sanlúcar.");
            Assert.That(puntos.Any(p => p.Id != 0), "Los elementos de la capa poi traen id.");
            Assert.IsFalse(conCategoria.Any(p => p.Subclase == "bus_stop" || p.Subclase == "office"));
            TestContext.WriteLine($"{puntos.Count} puntos, {conCategoria.Count} con categoría: " +
                string.Join(", ", conCategoria.GroupBy(p => ReglaPuntosInteres.Categoria(p.Clase, p.Subclase)).Select(g => $"{g.Key}={g.Count()}")));
        }
    }
}
