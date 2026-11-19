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
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => puntos;
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => centro;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
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

        [SetUp]
        public void Crear()
        {
            Textos.Forzados = T;
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapaPantalla = new MapaFalso();
            mapaPantalla.puntos.Add(Punto("Centro de Salud Barrio Bajo", "doctors", null, AlNorte(Cabildo, 200)));
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
        public void TocarUnPuntoCentraElMapaYPliegaLaHoja()
        {
            AbrirYElegir(CategoriaPoi.Salud);
            var punto = hoja.Filas.First(f => f.Resultado.Tipo == TipoResultado.Punto).Resultado;
            pantalla.ElegirResultado(punto);
            Assert.AreEqual(punto.Posicion, mapaPantalla.centrados.Last());
            Assert.IsFalse(hoja.Abierta);
            Assert.IsFalse(pantalla.FichaAbierta);
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
            foreach (var f in filasBusqueda) CollectionAssert.DoesNotContain(recorrido, f.Anuncio);
            CollectionAssert.Contains(recorrido, T(ClavesTexto.BuscadorCampo));
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
