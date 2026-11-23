using System;
using System.Collections.Generic;
using System.Linq;
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
    /// "Encuentra cerca" (historia 2.4, parte 1): la tabla de ReglaCerca, el servicio que hace la lista
    /// (ServicioCerca) y la lista en la hoja de la pantalla real. Una prueba por fila de la matriz de la historia.
    /// </summary>
    public class PruebasCerca
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);

        // ───────────────────────── Falsos (como en PruebasBusqueda) ─────────────────────────

        class MapaFalso : IMapa
        {
            public readonly List<PuntoInteres> puntos = new List<PuntoInteres>();
            public readonly List<Coordenada> centrados = new List<Coordenada>();
            public Coordenada centro = Cabildo;
            public void Centrar(Coordenada destino) => centrados.Add(destino);
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) => centrados.Add(destino);
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            // Con rejilla (historia 2.4, parte 2), cada coordenada distinta cae en su casilla, lejos de las demás: sin
            // solapes, se ve todo lo que entra en el filtro o la categoría
            public bool rejilla;
            readonly Dictionary<Coordenada, int> casillas = new Dictionary<Coordenada, int>();
            public PuntoPantalla Proyectar(Coordenada punto)
            {
                if (!rejilla) return PuntoPantalla.Ninguno;
                if (!casillas.TryGetValue(punto, out int i)) casillas[punto] = i = casillas.Count;
                return new PuntoPantalla(300f + (i % 40) * 600f, 300f + (i / 40) * 400f, 100f + i);
            }
            public IReadOnlyList<PuntoInteres> PuntosInteres => puntos;
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => centro;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event System.Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
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

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.Buscando;
            public Coordenada? Posicion { get; set; }
            public float? Rumbo { get; set; }
            public event Action Cambiado { add { } remove { } }
            public void PedirPermiso() { }
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
            public bool RutaActiva => false;
            public bool RutaSoloPrimerPlano => false;
            public Resultado<bool> IniciarRuta(string textoNotificacion) => Resultado<bool>.Exito(true);
            public void ActualizarNotificacion(string textoNotificacion) { }
            public void TerminarRuta() { }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        static ServicioLugares reales;
        static ServicioLugares Reales()
        {
            if (reales != null) return reales;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return reales = l;
        }

        static ServicioLugares Sinteticos(params Lugar[] lugares)
        {
            var s = new ServicioLugares(new Repositorio(lugares));
            Assert.IsTrue(s.Cargar().Ok);
            return s;
        }

        static Lugar Sitio(string id, string nombre, string tipo, Coordenada pos) =>
            new Lugar(id, nombre, tipo, Plantilla.ServicioLocal, pos, null);

        static Coordenada AlNorte(Coordenada c, double metros) => new Coordenada(c.Latitud + metros / 111195.0, c.Longitud);

        static ulong siguienteId = 1;
        static PuntoInteres Punto(string nombre, string clase, string subclase, Coordenada pos) =>
            new PuntoInteres(siguienteId++, nombre, clase, subclase, 1, pos);

        static Lugar Real(string id) => Reales().Buscar(id) ?? throw new AssertionException($"Falta el lugar {id}.");

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        /// <summary>Primero todos los de precio (por distancia) y después el resto (por distancia).</summary>
        static void AssertOrden(IReadOnlyList<ResultadoBusqueda> r)
        {
            int primeraSinPrecio = r.ToList().FindIndex(x => x.Precio == null);
            if (primeraSinPrecio < 0) primeraSinPrecio = r.Count;
            Assert.IsTrue(r.Skip(primeraSinPrecio).All(x => x.Precio == null), "Los de precio van delante.");
            for (int i = 1; i < r.Count; i++)
                if (i != primeraSinPrecio)
                    Assert.LessOrEqual(r[i - 1].Metros, r[i].Metros + 1e-6, $"Por distancia dentro de cada grupo ({r[i - 1]} y {r[i]}).");
        }

        // ───────────────────────── ReglaCerca ─────────────────────────

        [TestCase(CategoriaPoi.Comer, "Restaurante")]
        [TestCase(CategoriaPoi.Comer, "Taberna")]
        [TestCase(CategoriaPoi.Comer, "Comida rápida")]
        [TestCase(CategoriaPoi.Comer, "Cafetería")]
        [TestCase(CategoriaPoi.Comer, "Heladería")]
        [TestCase(CategoriaPoi.Bares, "Bar")]
        [TestCase(CategoriaPoi.Bares, "Pub")]
        [TestCase(CategoriaPoi.Bares, "Discoteca")]
        [TestCase(CategoriaPoi.Deporte, "Centro deportivo")]
        [TestCase(CategoriaPoi.Deporte, "Recinto deportivo")]
        [TestCase(CategoriaPoi.Deporte, "Piscina")]
        [TestCase(CategoriaPoi.Deporte, "Pista de pádel")]
        [TestCase(CategoriaPoi.Deporte, "Campo de fútbol")]
        [TestCase(CategoriaPoi.Deporte, "Club de golf")]
        [TestCase(CategoriaPoi.Deporte, "Club náutico")]
        [TestCase(CategoriaPoi.Salud, "Clínica dental")]
        [TestCase(CategoriaPoi.Salud, "Clínica médica")]
        [TestCase(CategoriaPoi.Salud, "Clínica privada")]
        [TestCase(CategoriaPoi.Salud, "Fisioterapia")]
        [TestCase(CategoriaPoi.Salud, "Veterinario")]
        [TestCase(CategoriaPoi.Deporte, "Pista de tenis")]
        [TestCase(CategoriaPoi.Cultura, "Cine")]
        [TestCase(CategoriaPoi.Cultura, "Teatro")]
        [TestCase(CategoriaPoi.Cultura, "Museo")]
        [TestCase(CategoriaPoi.Cultura, "Monumento")]
        [TestCase(CategoriaPoi.Cultura, "Visita turística")]
        public void CadaTipoDeLugarEntraEnSuCategoriaYSoloEnElla(CategoriaPoi chip, string tipo)
        {
            var lugar = Sitio("x", "X", tipo, Cabildo);
            foreach (var c in HojaBuscador.Categorias)
                Assert.AreEqual(c == chip, ReglaCerca.Incluye(c, lugar), $"{tipo} en {c}.");
            Assert.IsTrue(ReglaCerca.Incluye(null, lugar), "Encuentra cerca: todas las categorías.");
        }

        [TestCase("Peluquería")]
        [TestCase("Aparcamiento")]
        [TestCase("Taller")]
        [TestCase("Lavandería")]
        [TestCase("")]
        public void LosTiposSinCategoriaNoEntran(string tipo)
        {
            var lugar = Sitio("x", "X", tipo, Cabildo);
            foreach (var c in HojaBuscador.Categorias) Assert.IsFalse(ReglaCerca.Incluye(c, lugar), $"{tipo} en {c}.");
            Assert.IsFalse(ReglaCerca.Incluye(null, lugar));
        }

        [TestCase(CategoriaPoi.Comer, CategoriaPoi.Comer)]
        [TestCase(CategoriaPoi.Comer, CategoriaPoi.Rapida)]
        [TestCase(CategoriaPoi.Comer, CategoriaPoi.Cafe)]
        [TestCase(CategoriaPoi.Bares, CategoriaPoi.Bares)]
        [TestCase(CategoriaPoi.Super, CategoriaPoi.Super)]
        [TestCase(CategoriaPoi.Farmacia, CategoriaPoi.Farmacia)]
        [TestCase(CategoriaPoi.Gasolina, CategoriaPoi.Gasolina)]
        [TestCase(CategoriaPoi.Deporte, CategoriaPoi.Deporte)]
        [TestCase(CategoriaPoi.Salud, CategoriaPoi.Salud)]
        [TestCase(CategoriaPoi.Cultura, CategoriaPoi.Cultura)]
        public void CadaCategoriaDePuntoEntraEnSuChipYSoloEnEl(CategoriaPoi chip, CategoriaPoi poi)
        {
            foreach (var c in HojaBuscador.Categorias)
                Assert.AreEqual(c == chip, ReglaCerca.Incluye(c, poi), $"{poi} en {c}.");
            Assert.IsTrue(ReglaCerca.Incluye(null, poi));
        }

        [TestCase(CategoriaPoi.Parques)]
        [TestCase(CategoriaPoi.Tiendas)]
        [TestCase(CategoriaPoi.Hoteles)]
        [TestCase(CategoriaPoi.Belleza)]
        [TestCase(CategoriaPoi.Ninguna)]
        public void LasOtrasCategoriasDePuntoNoEntran(CategoriaPoi poi)
        {
            foreach (var c in HojaBuscador.Categorias) Assert.IsFalse(ReglaCerca.Incluye(c, poi));
            Assert.IsFalse(ReglaCerca.Incluye(null, poi));
        }

        [Test]
        public void SupermercadosFarmaciasYGasolinerasNoTienenLugares()
        {
            foreach (var l in Reales().Lugares)
            {
                Assert.IsFalse(ReglaCerca.Incluye(CategoriaPoi.Super, l));
                Assert.IsFalse(ReglaCerca.Incluye(CategoriaPoi.Farmacia, l));
                Assert.IsFalse(ReglaCerca.Incluye(CategoriaPoi.Gasolina, l));
            }
        }

        [Test]
        public void ElVeterinarioEntraEnSalud()
        {
            Assert.AreEqual(CategoriaPoi.Salud, ReglaPuntosInteres.Categoria("veterinary", null));
            Assert.IsTrue(ReglaCerca.Incluye(CategoriaPoi.Salud, ReglaPuntosInteres.Categoria("veterinary", null)));
            Assert.IsTrue(ReglaCerca.Incluye(CategoriaPoi.Salud, Real("clinica-veterinaria-reina-del-mar")));
        }

        [Test]
        public void LasCategoriasDeLaReglaSonLasDeLaHoja() =>
            CollectionAssert.AreEqual(HojaBuscador.Categorias, ReglaCerca.Categorias);

        // ───────────────────────── ServicioCerca (matriz) ─────────────────────────

        [Test]
        public void SaludTraeClinicasFisioDentistasVeterinarioYCentrosPublicosPrimeroLosDePrecio()
        {
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Centro de Salud Barrio Bajo", "doctors", null, AlNorte(Cabildo, 200)));
            mapa.puntos.Add(Punto("Clínica Veterinaria Bonanza", "veterinary", null, AlNorte(Cabildo, 250)));
            mapa.puntos.Add(Punto("Dentista Pérez", "dentist", null, AlNorte(Cabildo, 150)));
            mapa.puntos.Add(Punto("Mercadona", "grocery", "supermarket", AlNorte(Cabildo, 100))); // no es salud
            var r = new ServicioCerca(Reales(), mapa, Ahora).Listar(CategoriaPoi.Salud, Cabildo);

            AssertOrden(r);
            Assert.IsNotNull(r[0].Precio, "Primero un lugar con precio.");
            var tipos = r.Where(x => x.Tipo == TipoResultado.Lugar).Select(x => x.Lugar.Tipo).Distinct().ToList();
            foreach (var t in new[] { "Clínica dental", "Clínica privada", "Fisioterapia" }) CollectionAssert.Contains(tipos, t);
            Assert.IsTrue(r.All(x => x.Tipo != TipoResultado.Lugar || ReglaCerca.Incluye(CategoriaPoi.Salud, x.Lugar)));
            var publico = r.Single(x => x.Nombre == "Centro de Salud Barrio Bajo");
            Assert.IsTrue(publico.SanidadPublica, "Los centros públicos también salen.");
            Assert.IsTrue(r.Any(x => x.Nombre == "Clínica Veterinaria Bonanza"));
            Assert.IsTrue(r.Any(x => x.Nombre == "Dentista Pérez"));
            Assert.IsFalse(r.Any(x => x.Nombre == "Mercadona"));
            int areas = r.ToList().FindIndex(x => x.Lugar?.Id == "clinica-areas");
            Assert.That(areas, Is.InRange(0, 4), "Clínica Areas, con precio, entre los primeros.");
            Assert.IsNotNull(r[areas].Precio);
            Assert.AreEqual(Reales().Resumen("clinica-areas", Ahora).Valor.PrecioAnunciado, r[areas].Precio);
            Assert.IsTrue(r.All(x => x.Puntuacion == 0));
        }

        [Test]
        public void SupermercadosSoloPuntosDeSuperPorDistancia()
        {
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Mercadona", "grocery", "supermarket", AlNorte(Cabildo, 700)));
            mapa.puntos.Add(Punto("Dia", "grocery", "supermarket", AlNorte(Cabildo, 200)));
            mapa.puntos.Add(Punto("Frutería Paco", "shop", "greengrocer", AlNorte(Cabildo, 400)));
            mapa.puntos.Add(Punto("Casa Bigote", "restaurant", null, AlNorte(Cabildo, 50)));
            var r = new ServicioCerca(Reales(), mapa, Ahora).Listar(CategoriaPoi.Super, Cabildo);
            CollectionAssert.AreEqual(new[] { "Dia", "Frutería Paco", "Mercadona" }, r.Select(x => x.Nombre).ToArray());
            Assert.IsTrue(r.All(x => x.Tipo == TipoResultado.Punto && x.Categoria == CategoriaPoi.Super));
        }

        [Test]
        public void UnPuntoQueRepiteUnLugarCercaSaleUnaVez()
        {
            var areas = Real("clinica-areas");
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Clínica Areas", "doctors", null, AlNorte(areas.Posicion, 40)));
            var r = new ServicioCerca(Reales(), mapa, Ahora).Listar(CategoriaPoi.Salud, Cabildo);
            Assert.AreEqual(1, r.Count(x => x.Nombre == "Clínica Areas"));
            Assert.AreEqual(TipoResultado.Lugar, r.Single(x => x.Nombre == "Clínica Areas").Tipo);
        }

        // Tipos de los datos que a propósito no están en ninguna categoría
        static readonly string[] TiposSinCategoria = { "Peluquería", "Barbería", "Centro de belleza", "Aparcamiento", "Taller", "Lavandería" };

        [Test]
        public void CadaTipoDeLosDatosRealesTieneCategoriaOEstaExcluidoAPropósito()
        {
            foreach (var tipo in Reales().Lugares.Select(l => l.Tipo).Distinct())
            {
                bool entra = ReglaCerca.Incluye(null, Sitio("x", "X", tipo, Cabildo));
                Assert.IsTrue(entra != TiposSinCategoria.Contains(tipo),
                    $"\"{tipo}\": o entra en una categoría o está en TiposSinCategoria (y no las dos cosas).");
            }
        }

        [Test]
        public void UnPuntoQueRepiteUnLugarDeOtraCategoriaSigueSaliendo()
        {
            // Un lugar sin categoría con el mismo nombre que un punto de Salud: el punto no se pierde
            var lugares = Sinteticos(Sitio("x", "Centro Vital", "Peluquería", AlNorte(Cabildo, 100)));
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Centro Vital", "doctors", null, AlNorte(Cabildo, 110)));
            var r = new ServicioCerca(lugares, mapa, Ahora).Listar(CategoriaPoi.Salud, Cabildo);
            Assert.AreEqual(TipoResultado.Punto, r.Single().Tipo);
        }

        [Test]
        public void CadaCategoriaTieneSuTextoDeListaVacia()
        {
            foreach (var c in HojaBuscador.Categorias) Assert.IsNotNull(HojaBuscador.ClaveNombreCerca(c), c.ToString());
            string Vacio(CategoriaPoi c) => Textos.Formatear(T(ClavesTexto.BuscadorCercaVacio), T(HojaBuscador.ClaveNombreCerca(c)));
            Assert.AreEqual("No hay gasolineras cerca", Vacio(CategoriaPoi.Gasolina));
            Assert.AreEqual("No hay sitios de salud cerca", Vacio(CategoriaPoi.Salud));
            Assert.AreEqual("No hay sitios de cultura cerca", Vacio(CategoriaPoi.Cultura));
        }

        [Test]
        public void UnPuntoEnDosTeselasSaleUnaVez()
        {
            var mapa = new MapaFalso();
            mapa.puntos.Add(new PuntoInteres(77, "Repsol", "fuel", null, 1, Cabildo));
            mapa.puntos.Add(new PuntoInteres(77, "Repsol", "fuel", null, 1, Cabildo));
            Assert.AreEqual(1, new ServicioCerca(Sinteticos(), mapa, Ahora).Listar(CategoriaPoi.Gasolina, Cabildo).Count);
        }

        [Test]
        public void SinPosicionLaDistanciaEsDesdeElPuntoDado()
        {
            var centro = AlNorte(Cabildo, 1000);
            var lugares = Sinteticos(Sitio("a", "Bar A", "Bar", AlNorte(Cabildo, 1300)));
            var r = new ServicioCerca(lugares, null, Ahora).Listar(CategoriaPoi.Bares, centro);
            Assert.AreEqual(300.0, r.Single().Metros, 1.0);
        }

        [Test]
        public void UnLugarAMasDeTresKilometrosNoSale()
        {
            var lugares = Sinteticos(
                Sitio("lejos", "Bar Lejos", "Bar", AlNorte(Cabildo, 3500)),
                Sitio("cerca", "Bar Cerca", "Bar", AlNorte(Cabildo, 2900)));
            var r = new ServicioCerca(lugares, new MapaFalso(), Ahora).Listar(CategoriaPoi.Bares, Cabildo);
            CollectionAssert.AreEqual(new[] { "cerca" }, r.Select(x => x.Lugar.Id).ToArray());
            Assert.AreEqual(3000.0, ServicioCerca.RadioMetros);
        }

        [Test]
        public void EncuentraCercaTraeTodasLasCategoriasPrimeroLosDePrecio()
        {
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Repsol", "fuel", null, AlNorte(Cabildo, 100)));
            mapa.puntos.Add(Punto("Farmacia Central", "pharmacy", null, AlNorte(Cabildo, 120)));
            mapa.puntos.Add(Punto("Parque de la Victoria", "park", null, AlNorte(Cabildo, 90))); // no es de ningún chip
            var r = new ServicioCerca(Reales(), mapa, Ahora).Listar(null, Cabildo);
            AssertOrden(r);
            Assert.IsNotNull(r[0].Precio);
            Assert.AreEqual(ServicioCerca.MaximoResultados, r.Count, "Hay más de 50 sitios cerca del Cabildo: como mucho 50.");
            var categorias = r.Where(x => x.Tipo == TipoResultado.Lugar).Select(x => ReglaCerca.CategoriaDeTipo(x.Lugar.Tipo)).Distinct().Count();
            Assert.GreaterOrEqual(categorias, 2, "Lugares de varias categorías.");
            Assert.IsFalse(r.Any(x => x.Nombre == "Parque de la Victoria"));
        }

        [Test]
        public void ComoMuchoCincuentaLosMasCercanos()
        {
            var mapa = new MapaFalso();
            for (int i = 0; i < 70; i++) mapa.puntos.Add(Punto($"Bar {i:00}", "bar", null, AlNorte(Cabildo, 10 + i * 10)));
            var r = new ServicioCerca(Sinteticos(), mapa, Ahora).Listar(CategoriaPoi.Bares, Cabildo);
            Assert.AreEqual(50, r.Count);
            Assert.AreEqual("Bar 00", r[0].Nombre);
            Assert.AreEqual("Bar 49", r[49].Nombre);
        }

        [Test]
        public void GasolinerasSinPuntosCargadosSaleVacia() =>
            Assert.IsEmpty(new ServicioCerca(Reales(), new MapaFalso(), Ahora).Listar(CategoriaPoi.Gasolina, Cabildo));

        // ───────────────────────── Hoja y pantalla ─────────────────────────

        GameObject objeto;
        PantallaMapa pantalla;
        HojaBuscador hoja;
        MapaFalso mapaPantalla;
        UbicacionFalsa gps;
        ServicioFiltro filtroPantalla;

        [SetUp]
        public void Crear()
        {
            Textos.Forzados = T;
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = filtroPantalla = new ServicioFiltro(prefs);
            mapaPantalla = new MapaFalso();
            mapaPantalla.puntos.Add(Punto("Centro de Salud Barrio Bajo", "doctors", null, AlNorte(Cabildo, 200)));
            mapaPantalla.puntos.Add(Punto("Restaurante de Prueba", "restaurant", null, AlNorte(Cabildo, 260)));
            mapaPantalla.puntos.Add(Punto("Supermercado de Prueba", "grocery", "supermarket", AlNorte(Cabildo, 320)));
            gps = new UbicacionFalsa();
            pantalla.Inicializar(mapaPantalla, tema, Reales(), Ahora, arbol, filtro,
                ubicacion: new ServicioUbicacion(gps, prefs),
                busqueda: new ServicioBusqueda(Reales(), mapaPantalla, Ahora),
                cerca: new ServicioCerca(Reales(), mapaPantalla, Ahora));
            tema.Iniciar();
            filtro.Iniciar();
            hoja = pantalla.Buscador;
            hoja.Medir(800f);
        }

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
        }

        static bool Oculto(VisualElement e) => e.style.display == DisplayStyle.None;

        void AbrirYElegir(CategoriaPoi? categoria)
        {
            hoja.Abrir();
            pantalla.ActualizarCapas(0f);
            hoja.ElegirCategoria(categoria); // lo que hace el toque en el chip (Clickable)
        }

        [Test]
        public void TocarUnChipEnsenaSuListaConCabecera()
        {
            AbrirYElegir(CategoriaPoi.Salud);
            Assert.IsTrue(pantalla.CercaActiva);
            Assert.AreEqual(CategoriaPoi.Salud, pantalla.CategoriaActiva);
            Assert.IsTrue(hoja.MostrandoCerca);
            Assert.IsFalse(hoja.MostrandoResultados);
            Assert.IsTrue(Oculto(hoja.FilaChips));
            Assert.IsTrue(Oculto(hoja.Cerca));
            Assert.IsFalse(Oculto(hoja.CabeceraCerca));
            Assert.IsFalse(Oculto(hoja.Resultados));
            Assert.AreEqual("Salud cerca", hoja.TituloCerca.text);
            Assert.AreEqual("Quitar categoría", hoja.QuitarCategoria.tooltip);
            Assert.IsNotEmpty(hoja.Filas);
            Assert.IsNotNull(hoja.Filas[0].Precio, "Primero los de precio, con su precio en la fila.");
            Assert.IsTrue(hoja.Filas.Any(f => f.Resultado.Nombre == "Centro de Salud Barrio Bajo"));
            Assert.IsTrue(hoja.Panel.contentContainer.Contains(hoja.CabeceraCerca));
            Assert.Less(hoja.Panel.contentContainer.IndexOf(hoja.CabeceraCerca), hoja.Panel.contentContainer.IndexOf(hoja.Resultados),
                "La cabecera va encima de la lista.");
        }

        [Test]
        public void SinPosicionSeMideDesdeElCentroYLoDice()
        {
            gps.Estado = EstadoUbicacion.Buscando;
            mapaPantalla.centro = AlNorte(Cabildo, 500);
            AbrirYElegir(CategoriaPoi.Salud);
            Assert.IsFalse(Oculto(hoja.DesdeCentro));
            Assert.AreEqual("desde el centro del mapa", hoja.DesdeCentro.text);
            var fila = hoja.Filas.First();
            Assert.AreEqual(mapaPantalla.centro.DistanciaMetros(fila.Resultado.Posicion), fila.Resultado.Metros, 0.5);
        }

        [Test]
        public void ConPosicionSeMideDesdeElUsuario()
        {
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Cabildo;
            mapaPantalla.centro = AlNorte(Cabildo, 2000);
            AbrirYElegir(CategoriaPoi.Salud);
            Assert.IsTrue(Oculto(hoja.DesdeCentro));
            var fila = hoja.Filas.First();
            Assert.AreEqual(Cabildo.DistanciaMetros(fila.Resultado.Posicion), fila.Resultado.Metros, 0.5);
        }

        [Test]
        public void EncuentraCercaEnsenaTodoConElTituloCerca()
        {
            AbrirYElegir(null);
            Assert.IsTrue(pantalla.CercaActiva);
            Assert.IsNull(pantalla.CategoriaActiva);
            Assert.AreEqual("Cerca", hoja.TituloCerca.text);
            Assert.AreEqual(ServicioCerca.MaximoResultados, hoja.Filas.Count);
            Assert.IsNotNull(hoja.Filas[0].Precio);
        }

        [Test]
        public void GasolinerasSinPuntosDiceNoHayGasolinerasCerca()
        {
            AbrirYElegir(CategoriaPoi.Gasolina);
            Assert.IsEmpty(hoja.Filas);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.IsFalse(Oculto(hoja.SinResultados));
            Assert.AreEqual("No hay gasolineras cerca", hoja.SinResultados.text);
            Assert.IsFalse(hoja.BuscarDireccionVisible, "Sin \"Buscar como dirección\".");
            Assert.IsTrue(Oculto(hoja.BuscarDireccion));
            Assert.AreEqual("Gasolineras cerca", hoja.TituloCerca.text);
        }

        [Test]
        public void LaXDeLaCabeceraVuelveALasCategorias()
        {
            AbrirYElegir(CategoriaPoi.Salud);
            hoja.PulsarQuitarCategoria();
            Assert.IsFalse(pantalla.CercaActiva);
            Assert.IsTrue(hoja.MostrandoCategorias);
            Assert.IsFalse(Oculto(hoja.FilaChips));
            Assert.IsFalse(Oculto(hoja.Cerca));
            Assert.IsTrue(Oculto(hoja.CabeceraCerca));
            Assert.IsEmpty(hoja.Filas);
            Assert.IsTrue(hoja.Abierta);
        }

        [Test]
        public void AtrasVuelveALasCategoriasYDespuesPliega()
        {
            AbrirYElegir(CategoriaPoi.Gasolina);
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.CercaActiva);
            Assert.IsTrue(hoja.MostrandoCategorias);
            Assert.IsTrue(hoja.Abierta, "Primero vuelven las categorías; la hoja sigue abierta.");
            Assert.AreEqual("No hay resultados", hoja.SinResultados.text, "El texto de la búsqueda vuelve.");
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(hoja.Abierta);
        }

        [Test]
        public void EscribirQuitaLaCategoriaYBusca()
        {
            AbrirYElegir(CategoriaPoi.Salud);
            hoja.Escribir("bar");
            Assert.IsFalse(pantalla.CercaActiva);
            Assert.IsFalse(hoja.MostrandoCerca);
            Assert.IsTrue(hoja.MostrandoResultados);
            Assert.IsTrue(Oculto(hoja.CabeceraCerca));
            Assert.IsNotEmpty(hoja.Filas);
            Assert.IsTrue(hoja.Filas.All(f => f.Resultado.Puntuacion > 0), "Son resultados de la búsqueda.");
            pantalla.BorrarBusqueda();
            Assert.IsTrue(hoja.MostrandoCategorias, "Al vaciar el campo vuelven las categorías, no la lista.");
        }

        [Test]
        public void TocarUnLugarAbreSuFicha()
        {
            AbrirYElegir(CategoriaPoi.Salud);
            var fila = hoja.Filas.First(f => f.Resultado.Tipo == TipoResultado.Lugar);
            pantalla.ElegirResultado(fila.Resultado);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreSame(fila.Resultado.Lugar, pantalla.Ficha.Lugar);
            Assert.IsFalse(hoja.Abierta);
        }

        [Test]
        public void TocarUnPuntoAbreSuFichaConSuCategoria()
        {
            AbrirYElegir(CategoriaPoi.Salud);
            var punto = hoja.Filas.First(f => f.Resultado.Tipo == TipoResultado.Punto).Resultado;
            pantalla.ElegirResultado(punto);
            Assert.AreEqual(punto.Posicion, mapaPantalla.centrados.Last());
            Assert.IsFalse(hoja.Abierta);
            Assert.AreEqual(punto.Posicion, pantalla.PuntoAbierto);
            Assert.AreEqual(punto.Nombre, pantalla.Ficha.Titulo.text);
            Assert.AreEqual(FilaResultado.Mayuscula(FilaResultado.TipoDe(punto, T)), pantalla.Ficha.Tipo.text);
            pantalla.CerrarFicha();
            Assert.IsTrue(pantalla.CercaActiva, "La lista sigue al cerrar la ficha.");
            Assert.IsNotEmpty(hoja.Filas);
        }

        [Test]
        public void AlVolverADesplegarSigueLaLista()
        {
            AbrirYElegir(CategoriaPoi.Salud);
            double antes = hoja.Filas[0].Resultado.Metros;
            pantalla.PlegarBuscador();
            mapaPantalla.centro = AlNorte(mapaPantalla.centro, 1000); // el mapa se ha movido con la hoja plegada
            pantalla.AbrirBuscadorConLector();
            Assert.IsTrue(hoja.MostrandoCerca);
            Assert.AreEqual("Salud cerca", hoja.TituloCerca.text);
            var primero = hoja.Filas[0].Resultado;
            Assert.AreEqual(mapaPantalla.centro.DistanciaMetros(primero.Posicion), primero.Metros, 1.0,
                "La lista se rehace: distancias desde el centro de ahora.");
            Assert.AreNotEqual(antes, primero.Metros);
        }

        [Test]
        public void ConTalkBackLaJerarquiaRealNoTieneLosChipsConLaLista()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            pantalla.Accesibilidad.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            pantalla.AbrirBuscadorConLector();
            var antes = AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList();
            CollectionAssert.Contains(antes, T(ClavesTexto.BuscadorCerca), "Con las categorías, \"Encuentra cerca\" está.");
            hoja.ElegirCategoria(CategoriaPoi.Salud);
            var despues = AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList();
            foreach (var c in HojaBuscador.Categorias) CollectionAssert.DoesNotContain(despues, T(HojaBuscador.ClaveCategoria(c)));
            CollectionAssert.DoesNotContain(despues, T(ClavesTexto.BuscadorCerca));
        }

        [Test]
        public void ConTalkBackLosChipsSalenDelRecorridoYNoQuedanFilasViejas()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            hoja.Abrir();
            pantalla.ActualizarCapas(0f);
            hoja.Escribir("clinica");
            var filasBusqueda = hoja.Filas.ToList();
            Assert.IsNotEmpty(filasBusqueda);
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), filasBusqueda[0].Anuncio);
            pantalla.BorrarBusqueda();
            hoja.ElegirCategoria(CategoriaPoi.Salud);
            var recorrido = pantalla.Accesibilidad.Recorrido();
            foreach (var c in HojaBuscador.Categorias) CollectionAssert.DoesNotContain(recorrido, T(HojaBuscador.ClaveCategoria(c)));
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorCerca));
            // Las filas de Salud ya están en el recorrido (2.4, parte 3) y algunas repiten texto con las de la búsqueda:
            // de las viejas no queda ninguna (solo hay una entrada por fila nueva)
            var actuales = hoja.Filas.Select(f => f.Anuncio).ToList();
            foreach (var f in filasBusqueda)
                Assert.AreEqual(actuales.Count(a => a == f.Anuncio), recorrido.Count(e => e == f.Anuncio), f.Anuncio);
            CollectionAssert.Contains(recorrido, T(ClavesTexto.BuscadorCampo));
        }

        // ───────────────────────── TalkBack de la lista (historia 2.4, parte 3) ─────────────────────────

        /// <summary>Lo que se pide anunciar mientras se hace eso.</summary>
        static List<string> Oir(Action accion)
        {
            var oidos = new List<string>();
            Action<string> oir = oidos.Add;
            AccesibilidadPantalla.AnuncioPedido += oir;
            try { accion(); }
            finally { AccesibilidadPantalla.AnuncioPedido -= oir; }
            return oidos;
        }

        static string Nombre(CategoriaPoi c) => T(HojaBuscador.ClaveCategoria(c));

        void ConPosicion()
        {
            gps.Estado = EstadoUbicacion.Activa;
            gps.Posicion = Cabildo;
        }

        /// <summary>Con posición, lector activo, jerarquía real y la hoja abierta con TalkBack (con las categorías).</summary>
        void AbrirConJerarquiaReal()
        {
            ConPosicion();
            AccesibilidadPantalla.LectorForzado = () => true;
            pantalla.Accesibilidad.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            pantalla.AbrirBuscadorConLector();
        }

        [Test]
        public void ConTalkBackLosChipsYEncuentraCercaSonBotones()
        {
            AbrirConJerarquiaReal();
            foreach (var c in HojaBuscador.Categorias)
                Assert.AreEqual(AccessibilityRole.Button, NodoTalkBack(Nombre(c)).role, Nombre(c));
            Assert.AreEqual(AccessibilityRole.Button, NodoTalkBack(T(ClavesTexto.BuscadorCerca)).role);
        }

        [Test]
        public void ElDobleToqueEnUnChipEnsenaSuListaAnunciaYEnfocaElTitulo()
        {
            AbrirConJerarquiaReal();
            var oidos = Oir(() => ActivarTalkBack(NodoTalkBack(Nombre(CategoriaPoi.Salud))));
            Assert.IsTrue(pantalla.CercaActiva);
            Assert.AreEqual(CategoriaPoi.Salud, pantalla.CategoriaActiva);
            Assert.Greater(hoja.Filas.Count, 1);
            string esperado = "Salud cerca, " + PantallaMapa.AnuncioResultados(hoja.Filas.Count);
            StringAssert.EndsWith(" resultados", esperado);
            CollectionAssert.Contains(oidos, esperado);
            Assert.AreSame(hoja.TituloCerca, pantalla.Accesibilidad.Enfocado, "El foco va al título.");
            var titulo = NodoTalkBack("Salud cerca");
            Assert.AreEqual(AccessibilityRole.StaticText, titulo.role, "El título es un texto.");
            Assert.AreEqual(AccessibilityRole.Button, NodoTalkBack(T(ClavesTexto.BuscadorQuitarCategoria)).role);
        }

        [Test]
        public void ElDobleToqueEnEncuentraCercaEnsenaTodoYAnuncia()
        {
            AbrirConJerarquiaReal();
            var oidos = Oir(() => ActivarTalkBack(NodoTalkBack(T(ClavesTexto.BuscadorCerca))));
            Assert.IsTrue(pantalla.CercaActiva);
            Assert.IsNull(pantalla.CategoriaActiva);
            CollectionAssert.Contains(oidos, "Cerca, " + PantallaMapa.AnuncioResultados(hoja.Filas.Count));
            Assert.AreSame(hoja.TituloCerca, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ConTalkBackElChipAnunciaCuantosHayYEnfocaElTitulo()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var oidos = Oir(() => AbrirYElegir(CategoriaPoi.Salud));
            CollectionAssert.Contains(oidos, "Salud cerca, " + PantallaMapa.AnuncioResultados(hoja.Filas.Count));
            Assert.AreSame(hoja.TituloCerca, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ConTalkBackUnaListaVaciaAnunciaSuTextoVacio()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            ConPosicion();
            var oidos = Oir(() => AbrirYElegir(CategoriaPoi.Gasolina));
            CollectionAssert.Contains(oidos, "No hay gasolineras cerca");
            Assert.IsFalse(oidos.Any(o => o.StartsWith("Gasolineras cerca")), "Sin \"N resultados\".");
            Assert.AreSame(hoja.TituloCerca, pantalla.Accesibilidad.Enfocado);
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.AreEqual(1, recorrido.Count(e => e == "No hay gasolineras cerca"), "El texto vacío, una vez.");
            Assert.Less(recorrido.IndexOf("Gasolineras cerca"), recorrido.IndexOf("No hay gasolineras cerca"),
                "El texto vacío va detrás de la cabecera.");
        }

        [Test]
        public void ConUnSoloResultadoSeAnunciaUnResultado() =>
            Assert.AreEqual("Salud cerca, " + T(ClavesTexto.BuscadorUnResultado),
                Textos.Formatear(T(ClavesTexto.BuscadorCercaJuntar), "Salud cerca", PantallaMapa.AnuncioResultados(1)));

        [Test]
        public void ConTalkBackSinPosicionElTituloDiceDesdeElCentroDelMapa()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            gps.Estado = EstadoUbicacion.Buscando;
            AbrirYElegir(CategoriaPoi.Salud);
            var recorrido = pantalla.Accesibilidad.Recorrido();
            CollectionAssert.Contains(recorrido, "Salud cerca, desde el centro del mapa");
            CollectionAssert.DoesNotContain(recorrido, "Salud cerca");
        }

        [Test]
        public void ConTalkBackConPosicionElTituloEsSoloElTitulo()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            ConPosicion();
            AbrirYElegir(CategoriaPoi.Salud);
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), "Salud cerca");
        }

        [Test]
        public void ConTalkBackLaCabeceraYLasFilasVanEnOrdenDetrasDelCampo()
        {
            ConPosicion();
            AccesibilidadPantalla.LectorForzado = () => true;
            AbrirYElegir(CategoriaPoi.Salud);
            var recorrido = pantalla.Accesibilidad.Recorrido();
            int campo = recorrido.IndexOf(T(ClavesTexto.BuscadorCampo));
            int titulo = recorrido.IndexOf("Salud cerca");
            int quitar = recorrido.IndexOf(T(ClavesTexto.BuscadorQuitarCategoria));
            int fila = recorrido.IndexOf(hoja.Filas[0].Anuncio);
            Assert.That(campo >= 0 && campo < titulo && titulo < quitar && quitar < fila, string.Join(" | ", recorrido));
            foreach (var f in hoja.Filas) CollectionAssert.Contains(recorrido, f.Anuncio);
        }

        [Test]
        public void ElDobleToqueEnUnaFilaDeLugarAbreLaFichaConElFocoEnSuTitulo()
        {
            AbrirConJerarquiaReal();
            ActivarTalkBack(NodoTalkBack(Nombre(CategoriaPoi.Salud)));
            var fila = hoja.Filas.First(f => f.Resultado.Tipo == TipoResultado.Lugar);
            ActivarTalkBack(AssistiveSupport.activeHierarchy.rootNodes.First(n => n.label == fila.Anuncio));
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreSame(fila.Resultado.Lugar, pantalla.Ficha.Lugar);
            Assert.AreSame(pantalla.Ficha.Titulo, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ElDobleToqueEnUnaFilaDePuntoHaceLoMismoQueTocarla()
        {
            AbrirConJerarquiaReal();
            ActivarTalkBack(NodoTalkBack(Nombre(CategoriaPoi.Salud)));
            var fila = hoja.Filas.First(f => f.Resultado.Tipo == TipoResultado.Punto);
            ActivarTalkBack(AssistiveSupport.activeHierarchy.rootNodes.First(n => n.label == fila.Anuncio));
            Assert.AreEqual(fila.Resultado.Posicion, mapaPantalla.centrados.Last());
            Assert.IsFalse(hoja.Abierta);
            Assert.AreEqual(fila.Resultado.Posicion, pantalla.PuntoAbierto);
            Assert.AreSame(pantalla.Ficha.Titulo, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ElDobleToqueEnQuitarCategoriaVuelveALosChipsConElFocoEnElDeSalud()
        {
            AbrirConJerarquiaReal();
            ActivarTalkBack(NodoTalkBack(Nombre(CategoriaPoi.Salud)));
            ActivarTalkBack(NodoTalkBack(T(ClavesTexto.BuscadorQuitarCategoria)));
            Assert.IsFalse(pantalla.CercaActiva);
            Assert.IsTrue(hoja.MostrandoCategorias);
            int i = Array.IndexOf(HojaBuscador.Categorias, CategoriaPoi.Salud);
            Assert.AreSame(hoja.Chips[i], pantalla.Accesibilidad.Enfocado);
            CollectionAssert.Contains(AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList(), Nombre(CategoriaPoi.Salud));
        }

        [Test]
        public void ConTalkBackAtrasConEncuentraCercaLlevaElFocoAEncuentraCerca()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            AbrirYElegir(null);
            Assert.IsTrue(pantalla.Atras());
            Assert.IsTrue(hoja.MostrandoCategorias);
            Assert.AreSame(hoja.Cerca, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ConTalkBackLaPildoraSigueLlevandoElFocoALaPildora()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            AbrirYElegir(CategoriaPoi.Salud);
            pantalla.PlegarBuscador();
            pantalla.PulsarPildora();
            Assert.AreSame(pantalla.Pildora, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void PlegadaNiFilasNiCabeceraEnElRecorrido()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            ConPosicion();
            AbrirYElegir(CategoriaPoi.Salud);
            var filas = hoja.Filas.Select(f => f.Anuncio).ToList();
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), "Salud cerca");
            pantalla.PlegarBuscador();
            var recorrido = pantalla.Accesibilidad.Recorrido();
            CollectionAssert.DoesNotContain(recorrido, "Salud cerca");
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorQuitarCategoria));
            foreach (var f in filas) CollectionAssert.DoesNotContain(recorrido, f);
        }

        [Test]
        public void ConTalkBackAlVolverADesplegarLaListaSeAnunciaYElFocoVaAlTitulo()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            AbrirYElegir(CategoriaPoi.Salud);
            pantalla.PlegarBuscador();
            var oidos = Oir(pantalla.AbrirBuscadorConLector);
            CollectionAssert.Contains(oidos, "Salud cerca, " + PantallaMapa.AnuncioResultados(hoja.Filas.Count));
            Assert.AreEqual(1, oidos.Count(o => o.StartsWith("Salud cerca")), "Se anuncia una vez.");
            Assert.AreSame(hoja.TituloCerca, pantalla.Accesibilidad.Enfocado);
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), hoja.Filas[0].Anuncio);
        }

        [Test]
        public void ConTalkBackArrastrarLaHojaAbiertaConLaListaNoAnunciaYElFocoSigueEnLaFila()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            ConPosicion();
            AbrirYElegir(CategoriaPoi.Salud);
            var vieja = hoja.Filas[0];
            var sitio = vieja.Resultado;
            pantalla.Accesibilidad.PedirFoco(vieja);
            var oidos = Oir(() =>
            {
                // Arrastre lento hacia abajo de 40 dp (más de 10): la hoja sigue abierta
                var p = new Vector2(150f, hoja.TopAbierta + 20f);
                hoja.Bajar(p, 0.0);
                for (int i = 1; i <= 10; i++) hoja.Mover(p + new Vector2(0f, 4f * i), 100.0 * i);
                hoja.Soltar(1500.0);
                pantalla.ActualizarCapas(0f);
            });
            Assert.IsTrue(hoja.Abierta, "No se ha plegado.");
            Assert.IsTrue(hoja.MostrandoCerca);
            Assert.AreNotSame(vieja, hoja.Filas[0], "Al desplegar, la lista se rehace (PintarCerca).");
            Assert.AreSame(sitio.Lugar, hoja.Filas[0].Resultado.Lugar);
            Assert.AreSame(sitio.Punto, hoja.Filas[0].Resultado.Punto);
            Assert.AreSame(hoja.Filas[0], pantalla.Accesibilidad.Enfocado, "El foco sigue en la fila (la nueva del mismo sitio).");
            Assert.IsFalse(oidos.Any(o => o.StartsWith("Salud cerca")), string.Join(" | ", oidos));
        }

        [Test]
        public void SinLectorElegirUnChipNoAnunciaNiMueveElFoco()
        {
            var oidos = Oir(() => AbrirYElegir(CategoriaPoi.Salud));
            Assert.IsFalse(oidos.Any(o => o.StartsWith("Salud cerca")));
            Assert.AreNotSame(hoja.TituloCerca, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void LaClaveParaJuntarEstaEnLaTablaYEnLocalization()
        {
            Assert.AreEqual("{0}, {1}", T(ClavesTexto.BuscadorCercaJuntar));
            CollectionAssert.Contains(ClavesTexto.Todas, ClavesTexto.BuscadorCercaJuntar);
        }

        // ───────────────────────── Mapa filtrado por categoría (historia 2.4, parte 2) ─────────────────────────

        /// <summary>Coloca marcadores y puntos en la rejilla (sin solapes) en una pantalla enorme.</summary>
        void PintarMapa()
        {
            mapaPantalla.rejilla = true;
            pantalla.Capa.Actualizar(1f, 100000f, 100000f);
            pantalla.CapaPuntos.Actualizar(1f, 100000f, 100000f);
        }

        List<MarcadorLugar> MarcadoresVisibles() => pantalla.Capa.Marcadores.Where(m => m.Visible).ToList();
        List<string> PuntosVisibles() => pantalla.CapaPuntos.Vistas.Where(v => v.Visible).Select(v => v.Punto.Nombre).ToList();

        string TextoFiltro(FiltroPrecios f) => T(TextosFiltro.ClavePildora(f));

        void AssertPildoraConSalud()
        {
            var p = pantalla.Pildora;
            Assert.IsTrue(p.ClassListContains(PildoraPrecios.ClaseCategoria));
            Assert.AreEqual(CategoriaPoi.Salud, p.Categoria);
            Assert.AreEqual("Salud", p.Texto.text);
            Assert.IsTrue(p.IconoCategoria.ClassListContains("buscador__chip-icono"), "El círculo del icono del chip.");
            Assert.IsTrue(p.IconoCategoria.ClassListContains("buscador__chip-icono--salud"), "El color del chip de Salud.");
            Assert.IsFalse(Oculto(p.IconoCategoria));
            Assert.IsFalse(Oculto(p.Quitar), "La ✕ en lugar de la flecha.");
            Assert.IsTrue(Oculto(p.Punto), "Sin el punto del filtro.");
        }

        /// <summary>Vuelve el filtro guardado (Hostelería) en el mapa y en la píldora; la categoría ya no está.</summary>
        void AssertVuelveHosteleria()
        {
            Assert.AreEqual(FiltroPrecios.Hosteleria, filtroPantalla.Actual, "El filtro guardado no cambia.");
            Assert.IsNull(pantalla.CategoriaMapa);
            Assert.IsNull(pantalla.Capa.Categoria);
            Assert.IsNull(pantalla.CapaPuntos.Categoria);
            var p = pantalla.Pildora;
            Assert.IsFalse(p.ClassListContains(PildoraPrecios.ClaseCategoria));
            Assert.IsNull(p.Categoria);
            Assert.AreEqual(TextoFiltro(FiltroPrecios.Hosteleria), p.Texto.text);
            Assert.IsTrue(Oculto(p.IconoCategoria));
            Assert.IsTrue(Oculto(p.Quitar));
            Assert.IsFalse(Oculto(p.Punto));
            PintarMapa();
            var visibles = MarcadoresVisibles();
            Assert.IsNotEmpty(visibles);
            Assert.IsTrue(visibles.All(m => m.Lugar.Plantilla == Plantilla.Hosteleria), "El mapa de Hostelería.");
            CollectionAssert.AreEquivalent(new[] { "Centro de Salud Barrio Bajo", "Restaurante de Prueba", "Supermercado de Prueba" }, PuntosVisibles());
        }

        void ElegirSaludConHosteleria()
        {
            filtroPantalla.Elegir(FiltroPrecios.Hosteleria);
            AbrirYElegir(CategoriaPoi.Salud);
        }

        [Test]
        public void SaludEnsenaSoloSaludEnElMapaYLaPildoraSaludX()
        {
            ElegirSaludConHosteleria();
            Assert.AreEqual(CategoriaPoi.Salud, pantalla.CategoriaMapa);
            PintarMapa();
            var visibles = MarcadoresVisibles();
            Assert.IsNotEmpty(visibles);
            Assert.IsTrue(visibles.All(m => ReglaCerca.Incluye(CategoriaPoi.Salud, m.Lugar)), "Solo marcadores de salud.");
            Assert.IsTrue(visibles.Any(m => m.Lugar.Plantilla != Plantilla.Hosteleria), "El filtro \"Precios\" no cuenta.");
            Assert.IsTrue(visibles.Any(m => m.Lugar.Id == "clinica-areas"));
            int todosSalud = Reales().Lugares.Count(l => ReglaCerca.Incluye(CategoriaPoi.Salud, l));
            Assert.AreEqual(todosSalud, pantalla.Capa.Marcadores.Count(m => ReglaCerca.Incluye(CategoriaPoi.Salud, m.Lugar)));
            Assert.IsTrue(pantalla.Capa.Marcadores.Where(m => !ReglaCerca.Incluye(CategoriaPoi.Salud, m.Lugar)).All(m => !m.Visible));
            CollectionAssert.AreEquivalent(new[] { "Centro de Salud Barrio Bajo" }, PuntosVisibles());
            AssertPildoraConSalud();
            Assert.AreEqual(FiltroPrecios.Hosteleria, filtroPantalla.Actual, "El filtro guardado no cambia.");
            Assert.AreEqual(FiltroPrecios.Hosteleria, pantalla.Capa.Filtro);
        }

        [Test]
        public void AlPlegarElMapaSigueConSalud()
        {
            ElegirSaludConHosteleria();
            pantalla.PlegarBuscador();
            Assert.IsFalse(hoja.Abierta);
            Assert.IsTrue(pantalla.CercaActiva);
            Assert.AreEqual(CategoriaPoi.Salud, pantalla.CategoriaMapa);
            PintarMapa();
            Assert.IsTrue(MarcadoresVisibles().All(m => ReglaCerca.Incluye(CategoriaPoi.Salud, m.Lugar)));
            CollectionAssert.AreEquivalent(new[] { "Centro de Salud Barrio Bajo" }, PuntosVisibles());
            AssertPildoraConSalud();
        }

        [Test]
        public void TocarUnPuntoDeLaListaPliegaYElMapaSigueConSalud()
        {
            ElegirSaludConHosteleria();
            pantalla.ElegirResultado(hoja.Filas.First(f => f.Resultado.Tipo == TipoResultado.Punto).Resultado);
            Assert.IsFalse(hoja.Abierta);
            Assert.AreEqual(CategoriaPoi.Salud, pantalla.CategoriaMapa);
            PintarMapa();
            Assert.IsTrue(MarcadoresVisibles().All(m => ReglaCerca.Incluye(CategoriaPoi.Salud, m.Lugar)));
            CollectionAssert.AreEquivalent(new[] { "Centro de Salud Barrio Bajo" }, PuntosVisibles());
            AssertPildoraConSalud();
        }

        [Test]
        public void LaPildoraConLaFichaAbiertaLaCierraAntesDeQuitarLaCategoria()
        {
            ElegirSaludConHosteleria();
            pantalla.PlegarBuscador();
            pantalla.AbrirFicha(Reales().Lugares.First(l => l.Id == "clinica-areas"));
            Assert.IsTrue(pantalla.FichaAbierta);
            pantalla.PulsarPildora();
            Assert.IsFalse(pantalla.FichaAbierta, "No se cambia el mapa detrás de la ficha.");
            AssertVuelveHosteleria();
        }

        [Test]
        public void ElDobleToqueDeTalkBackEnLaPildoraQuitaLaCategoria()
        {
            ElegirSaludConHosteleria();
            pantalla.PlegarBuscador();
            AccesibilidadPantalla.LectorForzado = () => true;
            pantalla.Accesibilidad.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            var nodo = NodoTalkBack("Quitar categoría Salud");
            Assert.IsTrue(string.IsNullOrEmpty(nodo.value), "Con categoría la píldora no tiene valor.");
            ActivarTalkBack(nodo);
            Assert.IsFalse(pantalla.CercaActiva);
            Assert.IsFalse(pantalla.MenuAbierto, "No abre el menú de precios.");
            AssertVuelveHosteleria();
            var etiquetas = AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList();
            CollectionAssert.DoesNotContain(etiquetas, "Quitar categoría Salud");
            Assert.AreEqual(T(TextosFiltro.ClaveOpcion(FiltroPrecios.Hosteleria)), NodoTalkBack(T(ClavesTexto.PreciosControl)).value);
        }

        static AccessibilityNode NodoTalkBack(string etiqueta) =>
            AssistiveSupport.activeHierarchy.rootNodes.Single(n => n.label == etiqueta);

        /// <summary>El doble toque de TalkBack: lanza el evento invoked del nodo (por reflexión, como en PruebasFicha).</summary>
        static void ActivarTalkBack(AccessibilityNode nodo)
        {
            const System.Reflection.BindingFlags Campos = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var campo = typeof(AccessibilityNode).GetFields(Campos)
                .FirstOrDefault(f => typeof(Func<bool>).IsAssignableFrom(f.FieldType) && f.Name.IndexOf("invoked", StringComparison.OrdinalIgnoreCase) >= 0);
            if (campo == null) Assert.Inconclusive("Esta versión de Unity no deja leer el evento invoked del nodo.");
            var accion = campo.GetValue(nodo) as Func<bool>;
            Assert.IsNotNull(accion, $"El nodo \"{nodo.label}\" no tiene acción.");
            Assert.IsTrue(accion());
        }

        [Test]
        public void LaPildoraQuitaLaCategoriaYVuelveElFiltroSinAbrirElMenu()
        {
            ElegirSaludConHosteleria();
            pantalla.PulsarPildora();
            Assert.IsFalse(pantalla.CercaActiva);
            Assert.IsFalse(pantalla.MenuAbierto, "No abre el menú de precios.");
            Assert.IsTrue(hoja.MostrandoCategorias, "La lista vuelve a las categorías.");
            AssertVuelveHosteleria();
            pantalla.PulsarPildora();
            Assert.IsTrue(pantalla.MenuAbierto, "Sin categoría, la píldora vuelve a abrir el menú.");
        }

        [Test]
        public void LaPildoraConLaHojaPlegadaQuitaLaCategoriaYAlDesplegarSalenLasCategorias()
        {
            ElegirSaludConHosteleria();
            pantalla.PlegarBuscador();
            pantalla.PulsarPildora();
            AssertVuelveHosteleria();
            pantalla.AbrirBuscadorConLector();
            Assert.IsTrue(hoja.MostrandoCategorias);
            Assert.IsFalse(hoja.MostrandoCerca);
        }

        [Test]
        public void LaXDeLaCabeceraQuitaLaCategoriaDelMapa()
        {
            ElegirSaludConHosteleria();
            hoja.PulsarQuitarCategoria();
            Assert.IsTrue(hoja.MostrandoCategorias);
            AssertVuelveHosteleria();
        }

        [Test]
        public void AtrasConLaListaQuitaLaCategoriaDelMapa()
        {
            ElegirSaludConHosteleria();
            Assert.IsTrue(pantalla.Atras());
            AssertVuelveHosteleria();
        }

        [Test]
        public void EscribirVuelveElFiltroYLaBusqueda()
        {
            ElegirSaludConHosteleria();
            hoja.Escribir("bar");
            Assert.IsTrue(hoja.MostrandoResultados);
            AssertVuelveHosteleria();
        }

        [Test]
        public void EncuentraCercaNoCambiaNiElMapaNiLaPildora()
        {
            filtroPantalla.Elegir(FiltroPrecios.Hosteleria);
            PintarMapa();
            var antes = MarcadoresVisibles();
            var puntosAntes = PuntosVisibles();
            AbrirYElegir(null);
            Assert.IsTrue(pantalla.CercaActiva);
            Assert.IsNull(pantalla.CategoriaMapa);
            Assert.IsNull(pantalla.Capa.Categoria);
            Assert.IsNull(pantalla.CapaPuntos.Categoria);
            Assert.IsFalse(pantalla.Pildora.ClassListContains(PildoraPrecios.ClaseCategoria));
            Assert.AreEqual(TextoFiltro(FiltroPrecios.Hosteleria), pantalla.Pildora.Texto.text);
            PintarMapa();
            CollectionAssert.AreEquivalent(antes, MarcadoresVisibles());
            CollectionAssert.AreEquivalent(puntosAntes, PuntosVisibles());
        }

        [Test]
        public void SupermercadosSinMarcadoresSoloPuntosDeSuper()
        {
            filtroPantalla.Elegir(FiltroPrecios.Todos);
            AbrirYElegir(CategoriaPoi.Super);
            PintarMapa();
            Assert.IsEmpty(MarcadoresVisibles(), "Ningún marcador de la app.");
            CollectionAssert.AreEquivalent(new[] { "Supermercado de Prueba" }, PuntosVisibles());
            Assert.AreEqual(T(ClavesTexto.BuscadorSuper), pantalla.Pildora.Texto.text);
            Assert.IsTrue(pantalla.Pildora.IconoCategoria.ClassListContains("buscador__chip-icono--super"));
            Assert.IsFalse(pantalla.Pildora.IconoCategoria.ClassListContains("buscador__chip-icono--salud"));
        }

        [Test]
        public void CambiarDeChipCambiaElIconoDeLaPildora()
        {
            AbrirYElegir(CategoriaPoi.Super);
            hoja.PulsarQuitarCategoria();
            hoja.ElegirCategoria(CategoriaPoi.Salud);
            AssertPildoraConSalud();
            Assert.IsFalse(pantalla.Pildora.IconoCategoria.ClassListContains("buscador__chip-icono--super"));
        }

        [Test]
        public void ConTalkBackLaPildoraEsQuitarCategoriaYAnunciaElFiltroQueVuelve()
        {
            ElegirSaludConHosteleria();
            pantalla.PlegarBuscador();
            var recorrido = pantalla.Accesibilidad.Recorrido();
            CollectionAssert.Contains(recorrido, "Quitar categoría Salud");
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.PreciosControl));
            Assert.AreEqual("Quitar categoría Salud", pantalla.EtiquetaPildora());
            var oidos = new List<string>();
            Action<string> oir = oidos.Add;
            AccesibilidadPantalla.AnuncioPedido += oir;
            try { pantalla.PulsarPildora(); }
            finally { AccesibilidadPantalla.AnuncioPedido -= oir; }
            CollectionAssert.Contains(oidos, PantallaMapa.AnuncioFiltro(FiltroPrecios.Hosteleria));
            Assert.AreEqual("Precios en el mapa: Hostelería", PantallaMapa.AnuncioFiltro(FiltroPrecios.Hosteleria));
            recorrido = pantalla.Accesibilidad.Recorrido();
            CollectionAssert.Contains(recorrido, T(ClavesTexto.PreciosControl));
            CollectionAssert.DoesNotContain(recorrido, "Quitar categoría Salud");
        }

        [Test]
        public void ElTextoQuitarCategoriaEstaEnLaTablaYEnLocalization()
        {
            Assert.AreEqual("Quitar categoría {0}", T(ClavesTexto.PildoraQuitarCategoria));
            CollectionAssert.Contains(ClavesTexto.Todas, ClavesTexto.PildoraQuitarCategoria);
        }

        [Test]
        public void ElArranqueLeDaElServicioCercaALaPantalla()
        {
            var otro = new GameObject("Otra pantalla");
            try
            {
                var p = otro.AddComponent<PantallaMapa>();
                var prefs = new PreferenciasFalsas();
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), p, new ServicioTema(prefs, new SistemaClaro()), Reales(),
                    new ServicioFiltro(prefs), Ahora));
                Assert.IsNotNull(p.Cerca);
            }
            finally { UnityEngine.Object.DestroyImmediate(otro); }
        }
    }
}
