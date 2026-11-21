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
    /// Buscador (historia 2.3, parte 1): la regla de coincidencia (ReglaBusqueda), el servicio que mezcla lugares y
    /// puntos de interés (ServicioBusqueda, una prueba por fila de la matriz) y la lista de la hoja en la pantalla real.
    /// </summary>
    public class PruebasBusqueda
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        const string Nbsp = " ";

        // ───────────────────────── ReglaBusqueda ─────────────────────────

        [Test]
        public void NormalizaSinTildesNiSignosNiMayusculas()
        {
            Assert.AreEqual("clinica areas", ReglaBusqueda.Normalizar("¡Clínica  ÁREAS!"));
            Assert.AreEqual("", ReglaBusqueda.Normalizar("  ¿? "));
        }

        [TestCase("Bar Cachón", "bar cachon", ReglaBusqueda.Exacto)]
        [TestCase("Bar Cachón", "bar ca", ReglaBusqueda.EmpiezaIgual)]
        [TestCase("Bar Cachón", "cach", ReglaBusqueda.Contiene)]
        [TestCase("Bar Cachón", "cachon bar", ReglaBusqueda.Contiene)]
        [TestCase("Bar Cachón", "achon", ReglaBusqueda.NoCoincide)]
        [TestCase("Bar Cachón", "bar pepe", ReglaBusqueda.NoCoincide)]
        [TestCase("José Pérez", "jose", ReglaBusqueda.EmpiezaIgual)]
        [TestCase("Bar Cachón", "", ReglaBusqueda.NoCoincide)]
        public void Puntua(string nombre, string consulta, int esperada) =>
            Assert.AreEqual(esperada, ReglaBusqueda.Puntuar(consulta, nombre));

        [Test]
        public void TambienCoincideConLaDireccion()
        {
            Assert.AreEqual(ReglaBusqueda.Contiene, ReglaBusqueda.Puntuar("calle ancha", "Bar Pepe", "Calle Ancha, 10"));
            Assert.AreEqual(ReglaBusqueda.NoCoincide, ReglaBusqueda.Puntuar("calle ancha", "Bar Pepe", "Calle Real, 3"));
        }

        [TestCase(0.0, "0" + Nbsp + "m")]
        [TestCase(344.0, "340" + Nbsp + "m")]
        [TestCase(345.0, "350" + Nbsp + "m")]
        [TestCase(994.0, "990" + Nbsp + "m")]
        [TestCase(996.0, "1" + Nbsp + "km")]
        [TestCase(1234.0, "1,2" + Nbsp + "km")]
        [TestCase(12560.0, "12,6" + Nbsp + "km")]
        public void FormateaLaDistancia(double metros, string esperado) => Assert.AreEqual(esperado, FormatoDistancia.Texto(metros));

        // ───────────────────────── ServicioBusqueda ─────────────────────────

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

        static Lugar Sitio(string id, string nombre, Coordenada pos, string direccion = null) =>
            new Lugar(id, nombre, "Bar", Plantilla.Hosteleria, pos, direccion);

        /// <summary>Coordenada a tantos metros al norte de otra.</summary>
        static Coordenada AlNorte(Coordenada c, double metros) => new Coordenada(c.Latitud + metros / 111195.0, c.Longitud);

        static ulong siguienteId = 1;
        static PuntoInteres Punto(string nombre, string clase, string subclase, Coordenada pos) =>
            new PuntoInteres(siguienteId++, nombre, clase, subclase, 1, pos);

        static Lugar Real(string id) => Reales().Buscar(id) ?? throw new AssertionException($"Falta el lugar {id}.");

        [Test]
        public void ClinicaArEncuentraClinicaAreasLaPrimera()
        {
            var r = new ServicioBusqueda(Reales(), new MapaFalso(), Ahora).Buscar("clinica ar", Cabildo);
            Assert.IsNotEmpty(r);
            Assert.AreEqual("clinica-areas", r[0].Lugar?.Id);
            Assert.AreEqual(TipoResultado.Lugar, r[0].Tipo);
            Assert.AreEqual(ReglaBusqueda.EmpiezaIgual, r[0].Puntuacion);
        }

        [Test]
        public void SinTildesEncuentraJose()
        {
            var r = new ServicioBusqueda(Reales(), new MapaFalso(), Ahora).Buscar("jose", Cabildo);
            Assert.IsTrue(r.Any(x => x.Nombre.StartsWith("José", StringComparison.Ordinal) || x.Nombre.StartsWith("Jose", StringComparison.Ordinal)),
                "Encuentra \"Jose…\" / \"José…\".");
            var s = new ServicioBusqueda(Sinteticos(Sitio("a", "José Pérez", Cabildo)), null, Ahora).Buscar("jose", Cabildo);
            Assert.AreEqual("José Pérez", s.Single().Nombre);
        }

        [Test]
        public void EncuentraUnPuntoDeInteresConSuCategoria()
        {
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Mercadona", "grocery", "supermarket", AlNorte(Cabildo, 300)));
            var r = new ServicioBusqueda(Reales(), mapa, Ahora).Buscar("mercadona", Cabildo);
            var m = r.Single(x => x.Tipo == TipoResultado.Punto);
            Assert.AreEqual("Mercadona", m.Nombre);
            Assert.AreEqual(CategoriaPoi.Super, m.Categoria);
            Assert.AreSame(mapa.puntos[0], m.Punto);
            Assert.AreEqual(300.0, m.Metros, 1.0);
            Assert.IsNull(m.Precio);
        }

        [Test]
        public void UnPuntoQueRepiteUnLugarCercaNoSale()
        {
            var areas = Real("clinica-areas");
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Clínica Areas", "doctors", null, AlNorte(areas.Posicion, 40)));
            mapa.puntos.Add(Punto("CLINICA AREAS", "doctors", null, AlNorte(areas.Posicion, 500))); // otra sucursal
            var r = new ServicioBusqueda(Reales(), mapa, Ahora).Buscar("clinica areas", Cabildo);
            Assert.AreEqual(1, r.Count(x => x.Tipo == TipoResultado.Lugar && x.Lugar == areas));
            var puntos = r.Where(x => x.Tipo == TipoResultado.Punto).ToList();
            Assert.AreEqual(1, puntos.Count, "Solo el lugar; el de 500 m es otra sucursal.");
            Assert.AreSame(mapa.puntos[1], puntos[0].Punto);
        }

        [Test]
        public void SinCategoriaNiNombreNoSale()
        {
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Parada Mercadona", "bus", "bus_stop", Cabildo)); // oculto: categoría Ninguna
            mapa.puntos.Add(Punto("Oficina Mercadona", "office", null, Cabildo));   // sin categoría
            mapa.puntos.Add(Punto("", "grocery", "supermarket", Cabildo));
            Assert.IsEmpty(new ServicioBusqueda(Sinteticos(), mapa, Ahora).Buscar("mercadona", Cabildo));
        }

        [Test]
        public void SinResultadosConZzzz() =>
            Assert.IsEmpty(new ServicioBusqueda(Reales(), new MapaFalso(), Ahora).Buscar("zzzz", Cabildo));

        [Test]
        public void ConsultaSinLetrasNoBusca() =>
            Assert.IsEmpty(new ServicioBusqueda(Reales(), new MapaFalso(), Ahora).Buscar(" ¿? ", Cabildo));

        [Test]
        public void ComoMuchoVeinte()
        {
            var mapa = new MapaFalso();
            for (int i = 0; i < 30; i++) mapa.puntos.Add(Punto($"Bar Prueba {i}", "bar", null, AlNorte(Cabildo, 2000 + i * 10)));
            var r = new ServicioBusqueda(Sinteticos(), mapa, Ahora).Buscar("bar prueba", Cabildo);
            Assert.AreEqual(ServicioBusqueda.MaximoResultados, r.Count);
            Assert.AreEqual("Bar Prueba 0", r[0].Nombre, "Los más cercanos.");
        }

        [Test]
        public void PrimeroLosLugaresYDentroPorPuntuacionYDistancia()
        {
            var cerca = AlNorte(Cabildo, 100);
            var lejos = AlNorte(Cabildo, 900);
            var lugares = Sinteticos(
                Sitio("a", "Taberna El Pino", cerca),       // contiene "pino"
                Sitio("b", "Pino", lejos),                  // exacto
                Sitio("c", "Pino Verde", lejos),            // empieza igual
                Sitio("d", "Casa del Pino", lejos));        // contiene, más lejos que "a"
            var mapa = new MapaFalso();
            mapa.puntos.Add(Punto("Pino", "bar", null, cerca)); // exacto, pero es un punto: después de los lugares
            var r = new ServicioBusqueda(lugares, mapa, Ahora).Buscar("pino", Cabildo);
            CollectionAssert.AreEqual(new[] { "b", "c", "a", "d" }, r.Take(4).Select(x => x.Lugar.Id).ToArray());
            Assert.AreEqual(TipoResultado.Punto, r[4].Tipo);
            Assert.AreEqual(5, r.Count);
        }

        [Test]
        public void ElPrecioEsElAnunciadoDelLugar()
        {
            var r = new ServicioBusqueda(Reales(), new MapaFalso(), Ahora).Buscar("clinica areas", Cabildo)[0];
            Assert.AreEqual(Reales().Resumen("clinica-areas", Ahora).Valor.PrecioAnunciado, r.Precio);
        }

        [Test]
        public void UnPuntoRepetidoEnDosTeselasSaleUnaVez()
        {
            var mapa = new MapaFalso();
            mapa.puntos.Add(new PuntoInteres(77, "Mercadona", "grocery", "supermarket", 1, Cabildo));
            mapa.puntos.Add(new PuntoInteres(77, "Mercadona", "grocery", "supermarket", 1, Cabildo));
            Assert.AreEqual(1, new ServicioBusqueda(Sinteticos(), mapa, Ahora).Buscar("mercadona", Cabildo).Count);
        }

        // ───────────────────────── Hoja y pantalla ─────────────────────────

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        GameObject objeto;
        PantallaMapa pantalla;
        HojaBuscador hoja;
        MapaFalso mapaPantalla;
        readonly List<string> anuncios = new List<string>();

        void Oir(string texto) => anuncios.Add(texto);

        [SetUp]
        public void Crear()
        {
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapaPantalla = new MapaFalso();
            mapaPantalla.puntos.Add(Punto("Mercadona", "grocery", "supermarket", AlNorte(Cabildo, 300)));
            pantalla.Inicializar(mapaPantalla, tema, Reales(), Ahora, arbol, filtro,
                busqueda: new ServicioBusqueda(Reales(), mapaPantalla, Ahora));
            tema.Iniciar();
            filtro.Iniciar();
            hoja = pantalla.Buscador;
            hoja.Medir(800f);
            anuncios.Clear();
            AccesibilidadPantalla.AnuncioPedido += Oir;
        }

        [TearDown]
        public void Limpiar()
        {
            AccesibilidadPantalla.AnuncioPedido -= Oir;
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
        }

        static bool Oculto(VisualElement e) => e.style.display == DisplayStyle.None;

        void AbrirYEscribir(string texto)
        {
            hoja.Abrir();
            pantalla.ActualizarCapas(0f); // la pantalla sigue a la hoja: modal de TalkBack
            hoja.Escribir(texto);
        }

        [Test]
        public void SinTextoSeVenLasCategoriasYNoLaX()
        {
            Assert.IsFalse(hoja.MostrandoResultados);
            Assert.IsFalse(Oculto(hoja.FilaChips));
            Assert.IsFalse(Oculto(hoja.Cerca));
            Assert.IsTrue(Oculto(hoja.Resultados));
            Assert.IsTrue(Oculto(hoja.SinResultados));
            Assert.IsTrue(Oculto(hoja.Borrar));
            Assert.IsTrue(hoja.CajaCampo.Contains(hoja.Borrar), "La X va dentro del campo.");
        }

        [Test]
        public void AlEscribirSeVenLosResultadosEnLugarDeLasCategorias()
        {
            AbrirYEscribir("clinica ar");
            Assert.IsTrue(hoja.MostrandoResultados);
            Assert.IsTrue(Oculto(hoja.FilaChips));
            Assert.IsTrue(Oculto(hoja.Cerca));
            Assert.IsFalse(Oculto(hoja.Resultados));
            Assert.IsFalse(Oculto(hoja.Borrar));
            Assert.IsNotEmpty(hoja.Filas);
            var primera = hoja.Filas[0];
            Assert.AreEqual("Clínica Areas", primera.Nombre.text);
            Assert.IsTrue(hoja.Panel.contentContainer.Contains(primera));
            var areas = Real("clinica-areas");
            string distancia = FormatoDistancia.Texto(Cabildo.DistanciaMetros(areas.Posicion));
            Assert.AreEqual($"{areas.Tipo} · {distancia}", primera.Detalle.text);
            Assert.IsTrue(primera.Icono.ClassListContains("buscador__resultado-icono--lugar"));
        }

        [Test]
        public void LaFilaDeUnPuntoLlevaSuCategoria()
        {
            AbrirYEscribir("mercadona");
            var fila = hoja.Filas.Single(f => f.Resultado.Tipo == TipoResultado.Punto);
            Assert.AreEqual("Supermercado · 300" + Nbsp + "m", fila.Detalle.text);
            Assert.IsTrue(fila.Icono.ClassListContains("buscador__resultado-icono--super"));
            Assert.IsNull(fila.Precio);
            Assert.AreEqual("Mercadona, supermercado, a 300" + Nbsp + "m", fila.Anuncio);
        }

        [Test]
        public void ElPrecioResumidoSaleEnLaFilaYEnTalkBack()
        {
            var conPrecio = Reales().Lugares.First(l => Reales().Resumen(l.Id, Ahora).Valor.TienePrecio);
            AbrirYEscribir(conPrecio.Nombre);
            var fila = hoja.Filas.First(f => f.Resultado.Lugar == conPrecio);
            string precio = FormatoPrecio.Precio(Reales().Resumen(conPrecio.Id, Ahora).Valor.PrecioAnunciado.Value, T);
            Assert.AreEqual(precio, fila.Precio?.text);
            string distancia = FormatoDistancia.Texto(Cabildo.DistanciaMetros(conPrecio.Posicion));
            Assert.AreEqual($"{conPrecio.Nombre}, {conPrecio.Tipo}, a {distancia}, {precio}", fila.Anuncio);
        }

        [Test]
        public void SinResultadosSaleElAviso()
        {
            AbrirYEscribir("zzzz");
            Assert.IsEmpty(hoja.Filas);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.IsFalse(Oculto(hoja.SinResultados));
            Assert.AreEqual("No hay resultados", hoja.SinResultados.text);
            Assert.IsTrue(Oculto(hoja.FilaChips));
        }

        [Test]
        public void PlegarYVolverAAbrirMantieneLaBusqueda()
        {
            AbrirYEscribir("clinica");
            int filas = hoja.Filas.Count;
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(hoja.Abierta);
            Assert.AreEqual("clinica", hoja.Campo.value);
            Assert.AreEqual(filas, hoja.Filas.Count);
            hoja.Abrir();
            Assert.AreEqual("clinica", hoja.Campo.value);
            Assert.IsTrue(hoja.MostrandoResultados);
            Assert.AreEqual(filas, hoja.Filas.Count);
        }

        [Test]
        public void AlVolverADesplegarLaBusquedaSeRehaceConLaNuevaCercania()
        {
            mapaPantalla.centro = Cabildo;
            AbrirYEscribir("mercadona");
            double antes = hoja.Filas[0].Resultado.Metros;
            Assert.IsTrue(pantalla.Atras());
            mapaPantalla.centro = AlNorte(Cabildo, 2000); // el mapa se ha movido con la hoja plegada
            pantalla.AbrirBuscadorConLector(); // despliega (Desplegando)
            Assert.AreEqual("mercadona", hoja.Campo.value);
            Assert.Greater(Math.Abs(hoja.Filas[0].Resultado.Metros - antes), 1.0, "Las distancias se miden desde el nuevo centro.");
        }

        [Test]
        public void PlegarloArrastrandoTampocoBorraElTexto()
        {
            AbrirYEscribir("clinica");
            hoja.Bajar(new Vector2(150f, hoja.TopAbierta + 5f), 0.0);
            for (int i = 1; i <= 20; i++) hoja.Mover(new Vector2(150f, hoja.TopAbierta + 5f + i * 40f), i * 10.0);
            hoja.Soltar(205.0);
            Assert.IsFalse(hoja.Abierta);
            Assert.AreEqual("clinica", hoja.Campo.value);
            Assert.IsTrue(hoja.MostrandoResultados);
        }

        [Test]
        public void LaXVaciaElCampoYVuelvenLasCategorias()
        {
            AbrirYEscribir("clinica");
            pantalla.BorrarBusqueda();
            Assert.AreEqual("", hoja.Campo.value);
            Assert.IsFalse(hoja.MostrandoResultados);
            Assert.IsEmpty(hoja.Filas);
            Assert.IsFalse(Oculto(hoja.FilaChips));
            Assert.IsTrue(Oculto(hoja.Borrar));
        }

        [Test]
        public void TocarUnLugarCentraElMapaYAbreSuFicha()
        {
            AbrirYEscribir("clinica areas");
            var fila = hoja.Filas[0];
            pantalla.ElegirResultado(fila.Resultado);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreSame(fila.Resultado.Lugar, pantalla.Ficha.Lugar);
            Assert.AreEqual(fila.Resultado.Posicion, mapaPantalla.centrados.Last());
            Assert.IsFalse(hoja.Abierta);
            Assert.AreEqual("clinica areas", hoja.Campo.value, "Abrir una ficha no borra la búsqueda.");
        }

        [Test]
        public void TocarUnPuntoCentraElMapaYPliegaLaHoja()
        {
            AbrirYEscribir("mercadona");
            var punto = hoja.Filas.Single(f => f.Resultado.Tipo == TipoResultado.Punto).Resultado;
            pantalla.ElegirResultado(punto);
            Assert.AreEqual(punto.Posicion, mapaPantalla.centrados.Last());
            Assert.IsFalse(hoja.Abierta);
            Assert.IsFalse(pantalla.FichaAbierta);
        }

        [Test]
        public void LaDistanciaEsAlUsuarioOAlCentroDelMapa()
        {
            mapaPantalla.centro = AlNorte(Cabildo, 1000);
            Assert.AreEqual(mapaPantalla.centro, pantalla.PuntoCercania(), "Sin ubicación: el centro del mapa.");
        }

        [Test]
        public void ConTalkBackLosResultadosSonBotonesYSeAnuncianCuantosHay()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            AbrirYEscribir("mercadona");
            var recorrido = pantalla.Accesibilidad.Recorrido();
            CollectionAssert.Contains(recorrido, "Borrar búsqueda");
            Assert.AreEqual(recorrido.IndexOf(T(ClavesTexto.BuscadorCampo)) + 1, recorrido.IndexOf("Borrar búsqueda"), "La X va detrás del campo.");
            CollectionAssert.DoesNotContain(recorrido, T(ClavesTexto.BuscadorCerca), "Sin categorías mientras se busca.");
            foreach (var f in hoja.Filas) CollectionAssert.Contains(recorrido, f.Anuncio);
            Assert.AreEqual(recorrido.Count - 1, recorrido.IndexOf(hoja.Filas.Last().Anuncio), "Las filas van al final.");
            Assert.AreEqual(PantallaMapa.AnuncioResultados(hoja.Filas.Count), anuncios.Last());

            anuncios.Clear();
            hoja.Escribir("mercadon"); // mismos resultados: no se repite el anuncio
            Assert.IsEmpty(anuncios);

            hoja.Escribir("zzzz");
            Assert.AreEqual("No hay resultados", anuncios.Last());
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), "No hay resultados");
        }

        [Test]
        public void LosAnunciosDeCuantosHay()
        {
            Assert.AreEqual("No hay resultados", PantallaMapa.AnuncioResultados(0));
            Assert.AreEqual("1 resultado", PantallaMapa.AnuncioResultados(1));
            Assert.AreEqual("12 resultados", PantallaMapa.AnuncioResultados(12));
        }

        [Test]
        public void ConTalkBackActivarUnaFilaHaceLoMismoQueTocarla()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            AbrirYEscribir("clinica areas");
            var nodo = pantalla.Accesibilidad.NodoDe(hoja.Filas[0]);
            Assert.IsNotNull(nodo, "La fila tiene su nodo en la jerarquía.");
            Assert.AreEqual(AccessibilityRole.Button, nodo.role);
            Assert.AreEqual(hoja.Filas[0].Anuncio, nodo.label);
            // El doble toque lanza el evento invoked del nodo: no se puede lanzar desde fuera, se lee por reflexión
            const System.Reflection.BindingFlags Campos = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var campo = typeof(AccessibilityNode).GetFields(Campos)
                .FirstOrDefault(f => typeof(Func<bool>).IsAssignableFrom(f.FieldType) && f.Name.IndexOf("invoked", StringComparison.OrdinalIgnoreCase) >= 0);
            if (campo == null) Assert.Inconclusive("Esta versión de Unity no deja leer el evento invoked del nodo.");
            var accion = campo.GetValue(nodo) as Func<bool>;
            Assert.IsNotNull(accion);
            Assert.IsTrue(accion());
            Assert.IsTrue(pantalla.FichaAbierta);
        }

        [Test]
        public void PlegadaLasFilasNoEstanEnElRecorrido()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            AbrirYEscribir("clinica");
            pantalla.PlegarBuscador();
            var recorrido = pantalla.Accesibilidad.Recorrido();
            foreach (var f in hoja.Filas) CollectionAssert.DoesNotContain(recorrido, f.Anuncio);
            CollectionAssert.DoesNotContain(recorrido, "Borrar búsqueda");
        }

        [Test]
        public void ElArranqueLeDaElBuscadorALaPantalla()
        {
            var otro = new GameObject("Otra pantalla");
            try
            {
                var p = otro.AddComponent<PantallaMapa>();
                var prefs = new PreferenciasFalsas();
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), p, new ServicioTema(prefs, new SistemaClaro()), Reales(),
                    new ServicioFiltro(prefs), Ahora));
                Assert.IsNotNull(p.Busqueda);
            }
            finally { UnityEngine.Object.DestroyImmediate(otro); }
        }
    }
}
