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
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event System.Action<PuntoPantalla> Toque { add { } remove { } }
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
            var a = Punto("bank", null);
            var b = Punto("cafe", null, lat: 36.778);
            var (capa, mapa) = Capa(a, b);
            // Separados: no se pisan (parte 2)
            mapa.proyecciones[a.Posicion] = new PuntoPantalla(100f, 100f, 300f);
            mapa.proyecciones[b.Posicion] = new PuntoPantalla(100f, 600f, 300f);
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

        // ───────────────────────── Parte 2: repetidos (matriz) ─────────────────────────

        /// <summary>Metros por grado de latitud con el radio de Coordenada (haversine).</summary>
        const double MetrosPorGrado = Coordenada.RadioTierraMetros * Math.PI / 180.0;

        static readonly Coordenada SitioCachon = new Coordenada(36.7779, -6.3528);
        static Coordenada AlNorte(Coordenada c, double metros) => new Coordenada(c.Latitud + metros / MetrosPorGrado, c.Longitud);

        static Lugar LugarApp(string id, string nombre, Coordenada posicion, Plantilla plantilla = Plantilla.Hosteleria) =>
            new Lugar(id, nombre, "Bar", plantilla, posicion);

        static PuntoInteres PuntoEn(string nombre, Coordenada posicion, int rank = 10, ulong id = 1, string clase = "bar") =>
            new PuntoInteres(id, nombre, clase, null, rank, posicion);

        [TestCase("Bar Cachón", "bar cachon")]
        [TestCase("BAR CACHÓN!", "bar cachon")]
        [TestCase("  Bar   -  Cachón  ", "bar cachon")]
        [TestCase("Peña Ü's", "pena u s")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void ElNombreSeNormaliza(string nombre, string esperado) =>
            Assert.AreEqual(esperado, ReglaPuntosInteres.NombreNormalizado(nombre));

        [Test]
        public void LaDistanciaEsHaversine()
        {
            Assert.AreEqual(0.0, SitioCachon.DistanciaMetros(SitioCachon), 1e-9);
            Assert.AreEqual(20.0, SitioCachon.DistanciaMetros(AlNorte(SitioCachon, 20)), 0.01);
            Assert.AreEqual(MetrosPorGrado, new Coordenada(0, 0).DistanciaMetros(new Coordenada(0, 1)), 0.5, "Un grado en el ecuador.");
            var b = new Coordenada(36.7800, -6.3500);
            Assert.AreEqual(SitioCachon.DistanciaMetros(b), b.DistanciaMetros(SitioCachon), 1e-9, "Simétrica.");
        }

        [Test]
        public void RepetidoVisibleSeOcultaEnLaRegla()
        {
            var lugares = new[] { LugarApp("bar-cachon", "Bar Cachón", SitioCachon) };
            Assert.IsTrue(ReglaPuntosInteres.EsRepetido("Bar Cachón", AlNorte(SitioCachon, 20), lugares));
            Assert.IsFalse(ReglaPuntosInteres.EsRepetido("Bar Cachón", AlNorte(SitioCachon, 20), new Lugar[0]), "Sin el lugar visible no es repetido.");
            Assert.IsFalse(ReglaPuntosInteres.EsRepetido("Bar Cachón", AlNorte(SitioCachon, 20), null));
            Assert.IsFalse(ReglaPuntosInteres.EsRepetido("", SitioCachon, lugares), "Sin nombre nunca es repetido.");
            Assert.IsFalse(ReglaPuntosInteres.EsRepetido("Bar Otro", SitioCachon, lugares), "Otro nombre en el mismo sitio.");
        }

        [Test]
        public void TildesYSignosSonElMismoNombre()
        {
            var lugares = new[] { LugarApp("bar-cachon", "Bar Cachón", SitioCachon) };
            Assert.IsTrue(ReglaPuntosInteres.EsRepetido("BAR CACHÓN!", AlNorte(SitioCachon, 10), lugares));
        }

        [Test]
        public void OtraSucursalAMasDe80MetrosSeVe()
        {
            var lugares = new[] { LugarApp("mercadona", "Mercadona", SitioCachon, Plantilla.ServicioLocal) };
            Assert.IsFalse(ReglaPuntosInteres.EsRepetido("Mercadona", AlNorte(SitioCachon, 300), lugares));
            Assert.IsFalse(ReglaPuntosInteres.EsRepetido("Mercadona", AlNorte(SitioCachon, ReglaPuntosInteres.UmbralRepetidoMetros + 1), lugares));
            Assert.IsTrue(ReglaPuntosInteres.EsRepetido("Mercadona", AlNorte(SitioCachon, ReglaPuntosInteres.UmbralRepetidoMetros - 1), lugares));
        }

        /// <summary>Marcadores y puntos sobre el mismo mapa falso, actualizados en el orden de PantallaMapa.</summary>
        static (CapaMarcadores marcadores, CapaPuntosInteres puntos, MapaFalso mapa) Ambas(Lugar lugar, params PuntoInteres[] puntos)
        {
            var mapa = new MapaFalso();
            mapa.puntos.AddRange(puntos);
            var marcadores = new CapaMarcadores(new VisualElement(), mapa,
                new[] { (lugar, ReglaResumen.Resumir(lugar, DateTime.UtcNow)) }, c => PriceMap.EditorTools.PrepararInterfaz.TextosEs[c]);
            return (marcadores, new CapaPuntosInteres(new VisualElement(), mapa), mapa);
        }

        static void Fotograma(CapaMarcadores marcadores, CapaPuntosInteres puntos)
        {
            marcadores.Actualizar(1f, Ancho, Alto);
            var visibles = marcadores.Marcadores.Where(m => m.Visible).Select(m => m.Lugar).ToList();
            puntos.Actualizar(1f, Ancho, Alto, visibles);
        }

        [Test]
        public void RepetidoDeUnMarcadorVisibleSoloSeVeElMarcador()
        {
            var lugar = LugarApp("bar-cachon", "Bar Cachón", SitioCachon);
            var (marcadores, puntos, _) = Ambas(lugar, PuntoEn("Bar Cachón", AlNorte(SitioCachon, 20)));
            marcadores.Filtro = FiltroPrecios.Todos;
            Fotograma(marcadores, puntos);
            Assert.IsTrue(marcadores.Marcadores[0].Visible);
            Assert.IsFalse(puntos.Vistas.Single().Visible);
            Assert.AreEqual(DisplayStyle.None, puntos.Vistas.Single().style.display.value);
            Assert.AreEqual(1, puntos.OcultosPorRepetido);
        }

        [Test]
        public void ConElLugarOcultoPorElFiltroSeVeElPunto()
        {
            var lugar = LugarApp("bar-cachon", "Bar Cachón", SitioCachon);
            var (marcadores, puntos, mapa) = Ambas(lugar, PuntoEn("Bar Cachón", AlNorte(SitioCachon, 20)));
            marcadores.Filtro = FiltroPrecios.Todos;
            Fotograma(marcadores, puntos);
            Assert.IsFalse(puntos.Vistas.Single().Visible);

            marcadores.Filtro = FiltroPrecios.Ninguno;
            Fotograma(marcadores, puntos);
            Assert.IsFalse(marcadores.Marcadores[0].Visible);
            Assert.IsTrue(puntos.Vistas.Single().Visible, "El filtro oculta el lugar: el punto vuelve.");
            Assert.AreEqual(0, puntos.OcultosPorRepetido);

            // Y si el lugar sale de la pantalla, también
            marcadores.Filtro = FiltroPrecios.Todos;
            mapa.proyecciones[lugar.Posicion] = new PuntoPantalla(-500f, 900f, 300f);
            Fotograma(marcadores, puntos);
            Assert.IsFalse(marcadores.Marcadores[0].Visible);
            Assert.IsTrue(puntos.Vistas.Single().Visible);
        }

        [Test]
        public void OtraSucursalSeVenLosDos()
        {
            var lugar = LugarApp("mercadona", "Mercadona", SitioCachon, Plantilla.ServicioLocal);
            var (marcadores, puntos, _) = Ambas(lugar, PuntoEn("Mercadona", AlNorte(SitioCachon, 300), clase: "grocery"));
            marcadores.Filtro = FiltroPrecios.Todos;
            Fotograma(marcadores, puntos);
            Assert.IsTrue(marcadores.Marcadores[0].Visible);
            Assert.IsTrue(puntos.Vistas.Single().Visible);
        }

        // ───────────────────────── Parte 2: solapes (matriz) ─────────────────────────

        [Test]
        public void EnUnSolapeSeVeElDeMenorRank()
        {
            var menos = PuntoEn("Menos importante", new Coordenada(36.70, -6.35), rank: 10, id: 1);
            var mas = PuntoEn("Más importante", new Coordenada(36.71, -6.35), rank: 3, id: 2);
            var (capa, mapa) = Capa(menos, mas); // el de rank 10 va primero en la lista: manda el rank, no el orden
            mapa.proyecciones[menos.Posicion] = new PuntoPantalla(505f, 905f, 300f);
            mapa.proyecciones[mas.Posicion] = new PuntoPantalla(500f, 900f, 300f);
            capa.Actualizar(1f, Ancho, Alto);
            var vMas = capa.Vistas.Single(v => v.Punto == mas);
            var vMenos = capa.Vistas.Single(v => v.Punto == menos);
            Assert.IsTrue(vMas.Visible);
            Assert.IsFalse(vMenos.Visible);
            Assert.AreEqual(DisplayStyle.None, vMenos.style.display.value, "Se oculta entero: icono y nombre.");
            Assert.AreEqual(1, capa.OcultosPorSolape);
        }

        [Test]
        public void SinSolapeSeVenLosDos()
        {
            var a = PuntoEn("Uno", new Coordenada(36.70, -6.35), rank: 3, id: 1);
            var b = PuntoEn("Dos", new Coordenada(36.71, -6.35), rank: 10, id: 2);
            var (capa, mapa) = Capa(a, b);
            mapa.proyecciones[a.Posicion] = new PuntoPantalla(100f, 100f, 300f);
            mapa.proyecciones[b.Posicion] = new PuntoPantalla(100f, 400f, 300f);
            capa.Actualizar(1f, Ancho, Alto);
            Assert.AreEqual(2, capa.Visibles);
            Assert.AreEqual(0, capa.OcultosPorSolape);
        }

        [Test]
        public void EnUnEmpateDeRankSeVeElDeMenorId()
        {
            // El de menor id tiene el nombre que va detrás: solo la regla del id lo deja ver
            var nueve = PuntoEn("Cinco", new Coordenada(36.70, -6.35), rank: 5, id: 9);
            var cinco = PuntoEn("Nueve", new Coordenada(36.71, -6.35), rank: 5, id: 5);
            var (capa, mapa) = Capa(nueve, cinco);
            mapa.proyecciones[nueve.Posicion] = new PuntoPantalla(500f, 900f, 300f);
            mapa.proyecciones[cinco.Posicion] = new PuntoPantalla(510f, 902f, 300f);
            capa.Actualizar(1f, Ancho, Alto);
            Assert.IsTrue(capa.Vistas.Single(v => v.Punto == cinco).Visible);
            Assert.IsFalse(capa.Vistas.Single(v => v.Punto == nueve).Visible);
        }

        [Test]
        public void EnCascadaUnPuntoOcultoNoTapaAOtro()
        {
            // Sin nombre: solo círculos de 24 dp. A pisa a B; C solo pisa a B, que ya está oculto
            var a = PuntoEn("", new Coordenada(36.70, -6.35), rank: 1, id: 1);
            var b = PuntoEn("", new Coordenada(36.71, -6.35), rank: 2, id: 2);
            var c = PuntoEn("", new Coordenada(36.72, -6.35), rank: 3, id: 3);
            var (capa, mapa) = Capa(a, b, c);
            mapa.proyecciones[a.Posicion] = new PuntoPantalla(100f, 500f, 300f); // 88..112
            mapa.proyecciones[b.Posicion] = new PuntoPantalla(110f, 500f, 300f); // 98..122
            mapa.proyecciones[c.Posicion] = new PuntoPantalla(130f, 500f, 300f); // 118..142
            capa.Actualizar(1f, Ancho, Alto);
            Assert.IsTrue(capa.Vistas.Single(v => v.Punto == a).Visible);
            Assert.IsFalse(capa.Vistas.Single(v => v.Punto == b).Visible);
            Assert.IsTrue(capa.Vistas.Single(v => v.Punto == c).Visible, "Solo pisaba a uno oculto.");
            Assert.AreEqual(1, capa.OcultosPorSolape);
        }

        static string NombreDeLugar(Lugar l) => ReglaPuntosInteres.NombreNormalizado(l.Nombre);

        [Test]
        public void A80MetrosJustosNoEsRepetido()
        {
            Assert.IsFalse(ReglaPuntosInteres.DentroDelUmbral(ReglaPuntosInteres.UmbralRepetidoMetros), "Estrictamente menos de 80 m.");
            Assert.IsTrue(ReglaPuntosInteres.DentroDelUmbral(ReglaPuntosInteres.UmbralRepetidoMetros - 1e-6));
            var lugares = new[] { LugarApp("bar-cachon", "Bar Cachón", SitioCachon) };
            Assert.IsFalse(ReglaPuntosInteres.EsRepetido("Bar Cachón", AlNorte(SitioCachon, 80.001), lugares));
            Assert.IsFalse(ReglaPuntosInteres.EsRepetidoNormalizado("bar cachon", AlNorte(SitioCachon, 80.001), lugares,
                NombreDeLugar));
        }

        [Test]
        public void UnLugarSinNombreNuncaHaceRepetido()
        {
            var lugares = new[] { LugarApp("sin-nombre", "", SitioCachon) };
            foreach (var nombre in new[] { "¡!", "Bar", "" })
            {
                Assert.IsFalse(ReglaPuntosInteres.EsRepetido(nombre, SitioCachon, lugares), nombre);
                Assert.IsFalse(ReglaPuntosInteres.EsRepetidoNormalizado(ReglaPuntosInteres.NombreNormalizado(nombre), SitioCachon,
                    lugares, NombreDeLugar), nombre);
            }
            // En la capa: el punto "¡!" (nombre normalizado vacío) junto al lugar sin nombre se ve
            var (capa, _) = Capa(PuntoEn("¡!", SitioCachon));
            capa.Actualizar(1f, Ancho, Alto, lugares);
            Assert.IsTrue(capa.Vistas.Single().Visible);
            Assert.AreEqual(0, capa.OcultosPorRepetido);
        }

        [Test]
        public void OlvidarLasMedidasVuelveALaEstimacion()
        {
            var (capa, _) = Capa(PuntoEn("Bar Cachón", SitioCachon));
            var v = capa.Vistas.Single();
            v.MedidaRecordada = new Vector2(200f, 50f); // como si se hubiera dispuesto con la fuente grande
            Assert.AreEqual(PuntoInteresVista.NombreIzquierdaDp + 200f, v.Caja(Vector2.zero).xMax);

            capa.OlvidarMedidas();
            var estimada = v.EstimarNombre();
            Assert.AreEqual(Vector2.zero, v.MedidaRecordada);
            Assert.AreEqual(estimada, v.MedidaNombre);
            Assert.AreEqual(PuntoInteresVista.NombreIzquierdaDp + estimada.x, v.Caja(Vector2.zero).xMax);
        }

        [Test]
        public void LasMedidasDeLaCajaSonLasDeLosEstilos()
        {
            var tokens = File.ReadAllText(Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            float Px(string texto, string patron)
            {
                var m = System.Text.RegularExpressions.Regex.Match(texto, patron);
                Assert.IsTrue(m.Success, patron);
                return float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            Assert.AreEqual(PuntoInteresVista.IconoDp, Px(tokens, @"--poi-icono:\s*([\d.]+)px"));
            Assert.AreEqual(PuntoInteresVista.NombreAnchoMaxDp, Px(tokens, @"--poi-nombre-ancho:\s*([\d.]+)px"));
            Assert.AreEqual(PuntoInteresVista.NombreIzquierdaDp, Px(uss, @"\.poi__nombre\s*\{[^}]*?\bleft:\s*([\d.]+)px"));
        }

        [Test]
        public void ElNombreCuentaEnLaCaja()
        {
            // El de la izquierda tiene un nombre largo que llega al círculo del de la derecha
            var izq = PuntoEn("Nombre largo de verdad", new Coordenada(36.70, -6.35), rank: 3, id: 1);
            var der = PuntoEn("", new Coordenada(36.71, -6.35), rank: 10, id: 2);
            var (capa, mapa) = Capa(izq, der);
            mapa.proyecciones[izq.Posicion] = new PuntoPantalla(100f, 500f, 300f);
            mapa.proyecciones[der.Posicion] = new PuntoPantalla(180f, 500f, 300f); // 80 dp: lejos para los círculos solos
            capa.Actualizar(1f, Ancho, Alto);
            Assert.IsFalse(capa.Vistas.Single(v => v.Punto == der).Visible, "El nombre del primero lo pisa.");

            // Sin nombre, solo el círculo de 24 dp
            var caja = capa.Vistas.Single(v => v.Punto == der).Caja(new Vector2(50f, 50f));
            Assert.AreEqual(Rect.MinMaxRect(38f, 38f, 62f, 62f), caja);
            var conNombre = capa.Vistas.Single(v => v.Punto == izq).Caja(Vector2.zero);
            Assert.AreEqual(-PuntoInteresVista.IconoDp / 2f, conNombre.xMin);
            Assert.Greater(conNombre.xMax, PuntoInteresVista.NombreIzquierdaDp + 50f);
            Assert.LessOrEqual(conNombre.xMax, PuntoInteresVista.NombreIzquierdaDp + PuntoInteresVista.NombreAnchoMaxDp);
        }

        [Test]
        public void LaPantallaActualizaLosMarcadoresAntesQueLosPuntos()
        {
            var objeto = new GameObject("Pantalla de prueba");
            try
            {
                var pantalla = objeto.AddComponent<PantallaMapa>();
                var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
                var mapa = new MapaFalso();
                var lugar = LugarApp("bar-cachon", "Bar Cachón", SitioCachon);
                var punto = PuntoEn("Bar Cachón", AlNorte(SitioCachon, 20));
                mapa.proyecciones[lugar.Posicion] = new PuntoPantalla(5f, 6f, 100f);
                mapa.proyecciones[punto.Posicion] = new PuntoPantalla(5f, 6f, 100f);
                mapa.puntos.Add(punto);
                var lugares = new ServicioLugares(new UnLugar(lugar));
                lugares.Cargar();
                var filtro = new ServicioFiltro(new PreferenciasEnMemoria());
                pantalla.Inicializar(mapa, new ServicioTema(new PreferenciasEnMemoria(), new SistemaClaro()), lugares, DateTime.UtcNow, arbol, filtro);
                filtro.Iniciar(); // Ninguno por defecto

                pantalla.ActualizarCapas(2f);
                Assert.IsTrue(pantalla.CapaPuntos.Vistas.Single().Visible, "Con Ninguno el lugar no se ve: el punto sí.");

                // Un solo fotograma tras el cambio basta: los marcadores se actualizan primero
                filtro.Elegir(FiltroPrecios.Todos);
                pantalla.ActualizarCapas(2f);
                Assert.IsTrue(pantalla.Capa.Marcadores.Single().Visible);
                Assert.IsFalse(pantalla.CapaPuntos.Vistas.Single().Visible, "Repite el lugar visible: solo se ve el marcador.");
            }
            finally { UnityEngine.Object.DestroyImmediate(objeto); }
        }

        class UnLugar : IRepositorioLugares
        {
            readonly Lugar lugar;
            public UnLugar(Lugar lugar) { this.lugar = lugar; }
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(new[] { lugar });
        }

        // ───────────────────────── Parte 3: TalkBack (matriz) ─────────────────────────

        /// <summary>Textos es-ES del generador (en EditMode Localization no carga la locale).</summary>
        static string TextosEs(string clave) => PriceMap.EditorTools.PrepararInterfaz.TextosEs[clave];

        readonly List<GameObject> objetos = new List<GameObject>();

        [TearDown]
        public void Limpiar()
        {
            foreach (var o in objetos) UnityEngine.Object.DestroyImmediate(o);
            objetos.Clear();
            AccesibilidadPantalla.LectorForzado = null;
            Textos.Forzados = null;
            UnityEngine.Accessibility.AssistiveSupport.activeHierarchy = null;
        }

        /// <summary>
        /// Pantalla real con un lugar de la app y los puntos dados (ya en el mapa al inicializar: los crea el
        /// constructor de la capa), con el filtro en Todos para que el lugar se vea.
        /// </summary>
        PantallaMapa PantallaConPuntos(MapaFalso mapa, Lugar lugar, bool lector = false)
        {
            Textos.Forzados = TextosEs;
            if (lector) AccesibilidadPantalla.LectorForzado = () => true;
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            var pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var lugares = new ServicioLugares(new UnLugar(lugar));
            lugares.Cargar();
            var prefs = new PreferenciasEnMemoria();
            var filtro = new ServicioFiltro(prefs);
            pantalla.Inicializar(mapa, new ServicioTema(prefs, new SistemaClaro()), lugares, DateTime.UtcNow, arbol, filtro);
            filtro.Iniciar();
            filtro.Elegir(FiltroPrecios.Todos);
            return pantalla;
        }

        /// <summary>Un fotograma con pantalla de 1080×1920 px a 1 dp/px: marcadores, puntos y TalkBack, en ese orden.</summary>
        static void FotogramaPantalla(PantallaMapa p)
        {
            p.Capa.Actualizar(1f, Ancho, Alto);
            p.CapaPuntos.Actualizar(1f, Ancho, Alto, p.Capa.Marcadores.Where(m => m.Visible).Select(m => m.Lugar).ToList());
            p.Accesibilidad.ActualizarMarcadores();
        }

        static List<UnityEngine.Accessibility.AccessibilityNode> NodosActivos()
        {
            var j = UnityEngine.Accessibility.AssistiveSupport.activeHierarchy;
            if (j == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            return j.rootNodes.ToList();
        }

        static Lugar LugarAbajo(MapaFalso mapa)
        {
            var lugar = LugarApp("bar-cachon", "Bar Cachón", SitioCachon);
            mapa.proyecciones[lugar.Posicion] = new PuntoPantalla(500f, 1500f, 300f);
            return lugar;
        }

        static PuntoInteres En(MapaFalso mapa, PuntoInteres p, float x, float y)
        {
            mapa.proyecciones[p.Posicion] = new PuntoPantalla(x, y, 300f);
            mapa.puntos.Add(p);
            return p;
        }

        [Test]
        public void PuntoConNombreSeLeeNombreYCategoria()
        {
            Assert.AreEqual("Mercadona, supermercado", TextosPuntos.TextoAnuncio("Mercadona", CategoriaPoi.Super, false, TextosEs));
            var v = Capa(Punto("grocery", "supermarket", "Mercadona")).capa.Vistas.Single();
            Assert.AreEqual("Mercadona, supermercado", TextosPuntos.Anuncio(v, TextosEs));
        }

        [Test]
        public void SinNombreSoloLaCategoria()
        {
            var v = Capa(Punto("pharmacy", null, "")).capa.Vistas.Single();
            Assert.AreEqual("farmacia", TextosPuntos.Anuncio(v, TextosEs));
            Assert.AreEqual("farmacia", TextosPuntos.TextoAnuncio("  ", CategoriaPoi.Farmacia, false, TextosEs));
        }

        [Test]
        public void SanidadPublicaSeLeeCentroSanitarioPublico()
        {
            var v = Capa(Punto("hospital", null, "Centro de Salud Barrio Bajo")).capa.Vistas.Single();
            Assert.IsTrue(v.SanidadPublica);
            Assert.AreEqual("Centro de Salud Barrio Bajo, centro sanitario público", TextosPuntos.Anuncio(v, TextosEs));
            // Un centro de salud privado se lee con su categoría
            var privado = Capa(Punto("doctors", null, "Clínica Dental Sonrisa")).capa.Vistas.Single();
            Assert.AreEqual("Clínica Dental Sonrisa, salud", TextosPuntos.Anuncio(privado, TextosEs));
        }

        [TestCase("Nombre", CategoriaPoi.Ninguna, false, "Nombre")]          // sin texto de categoría: sin ", " colgando
        [TestCase("  Nombre  ", CategoriaPoi.Ninguna, false, "Nombre")]
        [TestCase("", CategoriaPoi.Farmacia, false, "farmacia")]
        [TestCase(" Mercadona ", CategoriaPoi.Super, false, "Mercadona, supermercado")]
        [TestCase("Centro de Salud", CategoriaPoi.Ninguna, true, "Centro de Salud, centro sanitario público")]
        public void ElTextoNuncaDejaElSeparadorColgando(string nombre, CategoriaPoi categoria, bool sanidad, string esperado) =>
            Assert.AreEqual(esperado, TextosPuntos.TextoAnuncio(nombre, categoria, sanidad, TextosEs));

        [Test]
        public void SoloReordenarRehaceLosNodosComoMuchoUnaVezPorSegundo()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            float ahora = 10f;
            var acc = new AccesibilidadPantalla(new VisualElement()) { Reloj = () => ahora };
            var pos = new Dictionary<string, Vector2> { ["a"] = new Vector2(0f, 100f), ["b"] = new Vector2(0f, 200f), ["c"] = new Vector2(0f, 300f) };
            bool cVisible = false;
            foreach (var n in new[] { "a", "b", "c" })
            {
                var nombre = n;
                acc.Marcador(new VisualElement(), () => nombre, () => nombre != "c" || cVisible, () => pos[nombre], AccesibilidadPantalla.GrupoPuntos);
            }
            acc.Reconstruir();
            NodosActivos();
            int Rehechas() => acc.ReconstruccionesMarcas;

            pos["a"] = new Vector2(0f, 250f); // b, a: solo cambia el orden
            acc.ActualizarMarcadores();
            Assert.AreEqual(1, Rehechas(), "La primera reordenación se aplica.");
            CollectionAssert.AreEqual(new[] { "b", "a" }, NodosActivos().Select(x => x.label));

            ahora = 10.5f;
            pos["a"] = new Vector2(0f, 100f); // a, b otra vez, en el mismo segundo
            acc.ActualizarMarcadores();
            Assert.AreEqual(1, Rehechas(), "Dos reordenaciones en el mismo segundo rehacen una sola vez.");

            ahora = 10.6f;
            cVisible = true; // cambia el conjunto: se rehace ya
            acc.ActualizarMarcadores();
            Assert.AreEqual(2, Rehechas(), "Un cambio en los visibles se aplica enseguida.");
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, NodosActivos().Select(x => x.label));

            ahora = 11.0f;
            pos["c"] = new Vector2(0f, 0f); // solo orden, 1 s después de la última reordenación aplicada (a los 10 s)
            acc.ActualizarMarcadores();
            Assert.AreEqual(3, Rehechas());
            CollectionAssert.AreEqual(new[] { "c", "a", "b" }, NodosActivos().Select(x => x.label));
        }

        [Test]
        public void LaZonaDeTalkBackDelPuntoMideAlMenos48Dp()
        {
            var mapa = new MapaFalso();
            var lugar = LugarAbajo(mapa);
            En(mapa, Punto("grocery", "supermarket", "Mercadona", 36.70, 1), 500f, 100f);
            var p = PantallaConPuntos(mapa, lugar, lector: true);
            FotogramaPantalla(p);

            var v = p.CapaPuntos.Vistas.Single();
            var zona = v.ZonaTactil;
            Assert.GreaterOrEqual(zona.width, PuntoInteresVista.ZonaTactilDp);
            Assert.GreaterOrEqual(zona.height, PuntoInteresVista.ZonaTactilDp);
            Assert.AreEqual(48f, PuntoInteresVista.ZonaTactilDp);
            Assert.AreEqual(v.Posicion, zona.center, "Centrada en el círculo (sin disposición, en la coordenada).");

            var nodo = NodosActivos().Single(n => n.label == "Mercadona, supermercado");
            var marco = nodo.frameGetter(); // sin panel, 1 dp = 1 px
            Assert.GreaterOrEqual(marco.width, 48f);
            Assert.GreaterOrEqual(marco.height, 48f);
            Assert.AreEqual(zona, marco);
        }

        [Test]
        public void CadaCategoriaTieneSuNombreEnMinusculas()
        {
            var esperados = new Dictionary<CategoriaPoi, string>
            {
                [CategoriaPoi.Comer] = "restaurante", [CategoriaPoi.Rapida] = "comida rápida", [CategoriaPoi.Cafe] = "cafetería",
                [CategoriaPoi.Bares] = "bar", [CategoriaPoi.Super] = "supermercado", [CategoriaPoi.Tiendas] = "tienda",
                [CategoriaPoi.Gasolina] = "gasolinera", [CategoriaPoi.Carga] = "punto de carga", [CategoriaPoi.Parking] = "aparcamiento",
                [CategoriaPoi.Hoteles] = "alojamiento", [CategoriaPoi.Farmacia] = "farmacia", [CategoriaPoi.Salud] = "salud",
                [CategoriaPoi.Deporte] = "deporte", [CategoriaPoi.Parques] = "parque", [CategoriaPoi.Cultura] = "cultura",
                [CategoriaPoi.Bancos] = "banco", [CategoriaPoi.Belleza] = "peluquería", [CategoriaPoi.Talleres] = "taller",
                [CategoriaPoi.Transporte] = "transporte", [CategoriaPoi.Educacion] = "centro educativo",
                [CategoriaPoi.Publico] = "servicio público", [CategoriaPoi.Culto] = "lugar de culto",
            };
            Assert.IsNull(TextosPuntos.ClaveCategoria(CategoriaPoi.Ninguna));
            var claves = new HashSet<string>();
            foreach (CategoriaPoi c in Enum.GetValues(typeof(CategoriaPoi)))
            {
                if (c == CategoriaPoi.Ninguna) continue;
                var clave = TextosPuntos.ClaveCategoria(c);
                Assert.IsNotNull(clave, c.ToString());
                Assert.IsTrue(claves.Add(clave), $"Clave repetida: {clave}");
                CollectionAssert.Contains(ClavesTexto.Todas, clave, c.ToString());
                Assert.AreEqual(esperados[c], TextosEs(clave), c.ToString());
            }
            Assert.AreEqual(22, claves.Count);
        }

        [Test]
        public void LosPuntosVanDespuesDeLosLugaresAunqueEstenMasArriba()
        {
            var mapa = new MapaFalso();
            var lugar = LugarAbajo(mapa);
            En(mapa, Punto("grocery", "supermarket", "Mercadona", 36.70, 1), 500f, 100f);
            var p = PantallaConPuntos(mapa, lugar);
            FotogramaPantalla(p);

            var anuncioLugar = p.Capa.Marcadores.Single().Anuncio;
            Assert.IsTrue(p.Capa.Marcadores.Single().Visible);
            CollectionAssert.AreEqual(new[] { TextosEs(ClavesTexto.PreciosControl), TextosEs(ClavesTexto.TemaCambiar), anuncioLugar, "Mercadona, supermercado" },
                p.Accesibilidad.Recorrido(), "Botones, el lugar (abajo) y después el punto (arriba).");
            CollectionAssert.AreEqual(new[] { (anuncioLugar, AccesibilidadPantalla.GrupoLugares), ("Mercadona, supermercado", AccesibilidadPantalla.GrupoPuntos) },
                p.Accesibilidad.RecorridoDeMarcas());
        }

        [Test]
        public void CadaGrupoDeArribaAbajoYDeIzquierdaADerecha()
        {
            var acc = new AccesibilidadPantalla(new VisualElement());
            acc.Marcador(new VisualElement(), () => "punto-abajo", () => true, () => new Vector2(0f, 900f), AccesibilidadPantalla.GrupoPuntos);
            acc.Marcador(new VisualElement(), () => "lugar-abajo", () => true, () => new Vector2(0f, 800f));
            acc.Marcador(new VisualElement(), () => "punto-derecha", () => true, () => new Vector2(50f, 10f), AccesibilidadPantalla.GrupoPuntos);
            acc.Marcador(new VisualElement(), () => "punto-izquierda", () => true, () => new Vector2(10f, 10f), AccesibilidadPantalla.GrupoPuntos);
            acc.Marcador(new VisualElement(), () => "lugar-arriba", () => true, () => new Vector2(0f, 500f));
            CollectionAssert.AreEqual(new[] { "lugar-arriba", "lugar-abajo", "punto-izquierda", "punto-derecha", "punto-abajo" }, acc.Recorrido());
        }

        [Test]
        public void ConElLectorLaJerarquiaTieneLosPuntosDetrasYComoTexto()
        {
            var mapa = new MapaFalso();
            var lugar = LugarAbajo(mapa);
            En(mapa, Punto("grocery", "supermarket", "Mercadona", 36.70, 1), 500f, 100f);
            var p = PantallaConPuntos(mapa, lugar, lector: true);
            FotogramaPantalla(p);

            var nodos = NodosActivos();
            var etiquetas = nodos.Select(n => n.label).ToList();
            CollectionAssert.AreEqual(p.Accesibilidad.Recorrido(), etiquetas, "La jerarquía real sigue el recorrido.");
            int iLugar = etiquetas.IndexOf(p.Capa.Marcadores.Single().Anuncio);
            int iPunto = etiquetas.IndexOf("Mercadona, supermercado");
            Assert.GreaterOrEqual(iLugar, 0);
            Assert.Greater(iPunto, iLugar, "El punto va después del lugar.");
            Assert.AreEqual(etiquetas.Count - 1, iPunto);
            Assert.AreEqual(UnityEngine.Accessibility.AccessibilityRole.StaticText, nodos[iPunto].role, "Texto, sin acción.");
        }

        [Test]
        public void UnSoloFotogramaDeLaPantallaDaElNodoDelPunto()
        {
            var mapa = new MapaFalso();
            var lugar = LugarApp("bar-cachon", "Bar Cachón", SitioCachon);
            mapa.proyecciones[lugar.Posicion] = new PuntoPantalla(0f, 0f, -5f); // detrás de la cámara: no se ve
            var mercadona = Punto("grocery", "supermarket", "Mercadona", 36.70, 1);
            mapa.puntos.Add(mercadona);
            mapa.proyecciones[mercadona.Posicion] = new PuntoPantalla(5f, 6f, 100f); // cerca de la esquina: dentro con cualquier pantalla
            var p = PantallaConPuntos(mapa, lugar, lector: true);
            p.ActualizarCapas(2f);
            Assert.IsTrue(p.CapaPuntos.Vistas.Single().Visible);
            Assert.That(NodosActivos().Any(n => n.label == "Mercadona, supermercado"),
                "TalkBack se actualiza después de colocar los puntos, en el mismo fotograma.");

            mapa.distancia = 2000f; // lejos: ningún punto
            p.ActualizarCapas(2f);
            Assert.IsFalse(NodosActivos().Any(n => n.label == "Mercadona, supermercado"));
        }

        [Test]
        public void LosPuntosOcultosNoTienenEntrada()
        {
            var mapa = new MapaFalso();
            var lugar = LugarAbajo(mapa);
            En(mapa, PuntoEn("Bar Cachón", AlNorte(SitioCachon, 20), id: 1), 100f, 300f);           // repite el lugar visible
            En(mapa, PuntoEn("Fuera", new Coordenada(36.60, -6.35), id: 2), -500f, 500f);             // fuera de pantalla
            En(mapa, PuntoEn("Grande", new Coordenada(36.61, -6.35), rank: 1, id: 3), 600f, 700f);
            En(mapa, PuntoEn("Pisado", new Coordenada(36.62, -6.35), rank: 9, id: 4), 605f, 702f);   // pisa a Grande
            En(mapa, Punto("pharmacy", null, "", 36.63, 5), 300f, 1200f);                              // sin nombre
            var p = PantallaConPuntos(mapa, lugar);
            FotogramaPantalla(p);

            var anuncioLugar = p.Capa.Marcadores.Single().Anuncio;
            CollectionAssert.AreEqual(new[] { anuncioLugar, "Grande, bar", "farmacia" },
                p.Accesibilidad.RecorridoDeMarcas().Select(m => m.etiqueta), "Sin repetidos, pisados ni fuera de pantalla.");
            Assert.AreEqual(1, p.CapaPuntos.OcultosPorRepetido);
            Assert.AreEqual(1, p.CapaPuntos.OcultosPorSolape);

            mapa.distancia = 2000f; // lejos: ningún punto
            FotogramaPantalla(p);
            CollectionAssert.AreEqual(new[] { anuncioLugar }, p.Accesibilidad.RecorridoDeMarcas().Select(m => m.etiqueta));
        }

        [Test]
        public void AlLiberarseLaTeselaSuEntradaDesaparece()
        {
            var mapa = new MapaFalso();
            var lugar = LugarAbajo(mapa);
            var a = En(mapa, PuntoEn("Uno", new Coordenada(36.60, -6.35), id: 1), 100f, 200f);
            En(mapa, PuntoEn("Dos", new Coordenada(36.61, -6.35), id: 2), 100f, 600f);
            var p = PantallaConPuntos(mapa, lugar, lector: true);
            FotogramaPantalla(p);
            Assert.AreEqual(3, p.Accesibilidad.Marcas, "El lugar y los dos puntos.");
            Assert.That(NodosActivos().Any(n => n.label == "Uno, bar"));

            mapa.puntos.Remove(a);
            mapa.Cambiar(); // la tesela de "Uno" se libera: su vista se destruye
            Assert.AreEqual(2, p.Accesibilidad.Marcas, "La marca se da de baja con la vista.");
            Assert.IsFalse(NodosActivos().Any(n => n.label == "Uno, bar"), "Su nodo se quita ya, sin esperar al siguiente fotograma.");
            CollectionAssert.DoesNotContain(p.Accesibilidad.Recorrido(), "Uno, bar");
            FotogramaPantalla(p);
            CollectionAssert.AreEqual(p.Accesibilidad.Recorrido(), NodosActivos().Select(n => n.label));

            // Una tesela nueva: su punto se da de alta (aviso de vista creada)
            En(mapa, PuntoEn("Tres", new Coordenada(36.62, -6.35), id: 3), 100f, 900f);
            mapa.Cambiar();
            Assert.AreEqual(3, p.Accesibilidad.Marcas);
            FotogramaPantalla(p);
            CollectionAssert.AreEqual(new[] { p.Capa.Marcadores.Single().Anuncio, "Dos, bar", "Tres, bar" },
                p.Accesibilidad.RecorridoDeMarcas().Select(m => m.etiqueta));
            Assert.That(NodosActivos().Any(n => n.label == "Tres, bar"));
        }

        [Test]
        public void ConElMenuAbiertoNingunPuntoEnElRecorrido()
        {
            var mapa = new MapaFalso();
            var lugar = LugarAbajo(mapa);
            En(mapa, Punto("grocery", "supermarket", "Mercadona", 36.70, 1), 500f, 100f);
            var p = PantallaConPuntos(mapa, lugar, lector: true);
            FotogramaPantalla(p);
            Assert.Contains("Mercadona, supermercado", p.Accesibilidad.Recorrido());

            p.AlternarMenu();
            FotogramaPantalla(p);
            Assert.IsTrue(p.Accesibilidad.Modal);
            CollectionAssert.DoesNotContain(p.Accesibilidad.Recorrido(), "Mercadona, supermercado");
            Assert.IsEmpty(p.Accesibilidad.RecorridoDeMarcas());
            Assert.IsFalse(NodosActivos().Any(n => n.role == UnityEngine.Accessibility.AccessibilityRole.StaticText), "Ni lugares ni puntos.");

            p.CerrarMenu();
            FotogramaPantalla(p);
            Assert.That(NodosActivos().Any(n => n.label == "Mercadona, supermercado"), "Al cerrar el menú vuelve el punto.");
        }

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
