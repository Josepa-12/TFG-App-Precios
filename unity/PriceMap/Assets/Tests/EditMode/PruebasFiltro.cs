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
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Filtro "Precios" del mapa (historia 1.5, parte 1): la regla del Dominio, el servicio y una prueba
    /// por fila de la matriz de la especificación con la pantalla real y un mapa falso.
    /// </summary>
    public class PruebasFiltro
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        const float Escala = 0.5f;
        const float AnchoPx = 1080f, AltoPx = 1920f;

        /// <summary>Proyecta todo al centro de la pantalla salvo las coordenadas que se digan.</summary>
        class MapaFalso : IMapa
        {
            public readonly Dictionary<Coordenada, PuntoPantalla> puntos = new Dictionary<Coordenada, PuntoPantalla>();
            public PuntoPantalla porDefecto = new PuntoPantalla(500f, 900f, 300f);
            public void Centrar(Coordenada destino) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => puntos.TryGetValue(punto, out var p) ? p : porDefecto;
        }

        class PreferenciasFalsas : IPreferencias
        {
            public readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        readonly List<GameObject> objetos = new List<GameObject>();

        [TearDown]
        public void Limpiar()
        {
            foreach (var o in objetos) UnityEngine.Object.DestroyImmediate(o);
            objetos.Clear();
        }

        /// <summary>Pantalla con la interfaz real, compuesta como en el Arranque (Inicializar y luego Iniciar).</summary>
        (PantallaMapa pantalla, VisualElement arbol, ServicioFiltro filtro, MapaFalso mapa) Pantalla(PreferenciasFalsas prefs)
        {
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            var pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro);
            tema.Iniciar();
            filtro.Iniciar();
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            return (pantalla, arbol, filtro, mapa);
        }

        static int Visibles(PantallaMapa p) => p.Capa.Marcadores.Count(m => m.Visible);
        static IEnumerable<Plantilla> PlantillasVisibles(PantallaMapa p) => p.Capa.Marcadores.Where(m => m.Visible).Select(m => m.Lugar.Plantilla).Distinct();

        // ── Regla del Dominio ──

        [TestCase(FiltroPrecios.Ninguno, false, false, false)]
        [TestCase(FiltroPrecios.Todos, true, true, true)]
        [TestCase(FiltroPrecios.ServiciosLocales, true, false, false)]
        [TestCase(FiltroPrecios.Hosteleria, false, true, false)]
        [TestCase(FiltroPrecios.Ocio, false, false, true)]
        public void CadaOpcionIncluyeSusPlantillas(FiltroPrecios filtro, bool servicio, bool hosteleria, bool ocio)
        {
            Assert.AreEqual(servicio, ReglaFiltro.Incluye(filtro, Plantilla.ServicioLocal));
            Assert.AreEqual(hosteleria, ReglaFiltro.Incluye(filtro, Plantilla.Hosteleria));
            Assert.AreEqual(ocio, ReglaFiltro.Incluye(filtro, Plantilla.Ocio));
        }

        [Test]
        public void CadaOpcionSeGuardaYSeRecuperaConSuClave()
        {
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(FiltroPrecios)), ReglaFiltro.Opciones);
            foreach (var f in ReglaFiltro.Opciones) Assert.AreEqual(f, ReglaFiltro.DesdeClave(ReglaFiltro.AClave(f)));
            Assert.AreEqual(ReglaFiltro.Opciones.Length, ReglaFiltro.Opciones.Select(ReglaFiltro.AClave).Distinct().Count());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("xyz")]
        [TestCase("Hosteleria")]
        public void UnaClaveDesconocidaOAusenteEsNinguno(string clave) =>
            Assert.AreEqual(FiltroPrecios.Ninguno, ReglaFiltro.DesdeClave(clave));

        [Test]
        public void ElMenuTieneLasCincoOpcionesEnOrden() =>
            CollectionAssert.AreEqual(new[] { FiltroPrecios.Ninguno, FiltroPrecios.Todos, FiltroPrecios.ServiciosLocales, FiltroPrecios.Hosteleria, FiltroPrecios.Ocio },
                ReglaFiltro.Opciones);

        // ── Servicio ──

        [Test]
        public void ElServicioIniciaEnNingunoSinGuardarNadaYLoAnuncia()
        {
            var prefs = new PreferenciasFalsas();
            var servicio = new ServicioFiltro(prefs);
            var recibidos = new List<FiltroPrecios>();
            servicio.Cambiado += recibidos.Add;
            servicio.Iniciar();
            Assert.AreEqual(FiltroPrecios.Ninguno, servicio.Actual);
            CollectionAssert.AreEqual(new[] { FiltroPrecios.Ninguno }, recibidos);
            Assert.IsEmpty(prefs.datos);
        }

        [Test]
        public void ElegirGuardaYAnuncia()
        {
            var prefs = new PreferenciasFalsas();
            var servicio = new ServicioFiltro(prefs);
            servicio.Iniciar();
            var recibidos = new List<FiltroPrecios>();
            servicio.Cambiado += recibidos.Add;
            servicio.Elegir(FiltroPrecios.Hosteleria);
            Assert.AreEqual(FiltroPrecios.Hosteleria, servicio.Actual);
            Assert.AreEqual("hosteleria", prefs.Leer(ServicioFiltro.ClavePreferencia));
            CollectionAssert.AreEqual(new[] { FiltroPrecios.Hosteleria }, recibidos);
        }

        // ── Matriz ──

        [Test]
        public void PrimerArranqueVerPreciosYCeroMarcadores()
        {
            var (p, arbol, filtro, _) = Pantalla(new PreferenciasFalsas());
            Assert.AreEqual(FiltroPrecios.Ninguno, filtro.Actual);
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosVer), p.Pildora.Texto.text);
            Assert.IsTrue(p.Pildora.ClassListContains("pildora-precios--ninguno"), "Con Ninguno el texto va atenuado.");
            Assert.AreEqual(FiltroPrecios.Ninguno, p.Pildora.Punto.Filtro, "Aro hueco.");
            Assert.IsTrue(p.Pildora.Punto.ClassListContains("punto-precios--ninguno"));
            Assert.AreEqual(0, Visibles(p));
            Assert.AreEqual(arbol.Q("hueco-precios"), p.Pildora.parent, "La píldora va arriba a la izquierda.");
            Assert.IsFalse(p.MenuAbierto);
        }

        [Test]
        public void TodosMuestraLos124Marcadores()
        {
            var (p, _, filtro, _) = Pantalla(new PreferenciasFalsas());
            filtro.Elegir(FiltroPrecios.Todos);
            Assert.IsTrue(p.Capa.Actualizar(Escala, AnchoPx, AltoPx));
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosTodos), p.Pildora.Texto.text);
            Assert.IsFalse(p.Pildora.ClassListContains("pildora-precios--ninguno"));
            Assert.IsTrue(p.Pildora.Punto.ClassListContains("punto-precios--todos"), "Con Todos, el punto es del acento.");
            Assert.AreEqual(124, Lugares().Lugares.Count);
            Assert.AreEqual(124, Visibles(p));
        }

        [Test]
        public void HosteleriaMuestraSoloLos89DeHosteleria()
        {
            var (p, _, _, _) = Pantalla(new PreferenciasFalsas());
            p.AlternarMenu();
            p.Menu.Opciones.Single(o => o.Filtro == FiltroPrecios.Hosteleria).Pulsar();
            p.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.IsFalse(p.MenuAbierto, "Se cierra al elegir.");
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosOpcionHosteleria), p.Pildora.Texto.text);
            Assert.IsTrue(p.Pildora.Punto.ClassListContains("punto-precios--hosteleria"));
            Assert.AreEqual(89, Visibles(p));
            CollectionAssert.AreEqual(new[] { Plantilla.Hosteleria }, PlantillasVisibles(p));
        }

        [Test]
        public void LaEleccionSeRecuerdaAlVolverAArrancar()
        {
            var prefs = new PreferenciasFalsas();
            Pantalla(prefs).filtro.Elegir(FiltroPrecios.Ocio);

            var (p, _, filtro, _) = Pantalla(prefs);
            Assert.AreEqual(FiltroPrecios.Ocio, filtro.Actual);
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosOpcionOcio), p.Pildora.Texto.text);
            Assert.AreEqual(Lugares().Lugares.Count(l => l.Plantilla == Plantilla.Ocio), Visibles(p));
            CollectionAssert.AreEqual(new[] { Plantilla.Ocio }, PlantillasVisibles(p));
        }

        [Test]
        public void UnValorDanadoCuentaComoNinguno()
        {
            var prefs = new PreferenciasFalsas();
            prefs.Guardar(ServicioFiltro.ClavePreferencia, "xyz");
            var (p, _, filtro, _) = Pantalla(prefs);
            Assert.AreEqual(FiltroPrecios.Ninguno, filtro.Actual);
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosVer), p.Pildora.Texto.text);
            Assert.AreEqual(0, Visibles(p));
        }

        [Test]
        public void TocarFueraCierraElMenuSinCambiarElFiltroNiMoverElMapa()
        {
            var (p, arbol, filtro, _) = Pantalla(new PreferenciasFalsas());
            filtro.Elegir(FiltroPrecios.Ocio);
            var raiz = arbol.Q("raiz");
            Assert.IsFalse(p.TapaPunto(new Vector2(200f, 200f)), "Con el menú cerrado el mapa recibe los toques.");
            Assert.AreEqual(DisplayStyle.None, p.Menu.style.display.value);

            p.AlternarMenu();
            Assert.IsTrue(p.MenuAbierto);
            Assert.AreEqual(DisplayStyle.Flex, p.Menu.style.display.value);
            Assert.AreEqual(p.Pildora.parent, p.Menu.parent, "El menú va debajo de la píldora.");
            Assert.IsTrue(p.Velo.ClassListContains("abierto"), "El velo cubre la pantalla con el menú abierto.");
            Assert.IsTrue(PantallaMapa.EsControl(raiz, p.Velo), "El velo captura el toque: no llega al mapa.");
            Assert.AreEqual(raiz, p.Velo.parent);
            Assert.IsTrue(raiz.IndexOf(p.Velo) < raiz.IndexOf(arbol.Q("zona-segura-precios")), "El velo va debajo de la píldora y del menú.");
            Assert.IsTrue(arbol.Q("zona-segura-precios").Contains(p.Menu));
            Assert.IsTrue(raiz.IndexOf(raiz.Q("capa-marcadores")) < raiz.IndexOf(p.Velo), "El velo va encima de los marcadores.");
            Assert.IsTrue(p.TapaPunto(new Vector2(200f, 200f)), "Con el menú abierto, todo el mapa cuenta como tapado.");

            p.CerrarMenu(); // lo que hace el velo al tocarlo
            Assert.IsFalse(p.MenuAbierto);
            Assert.IsFalse(p.Velo.ClassListContains("abierto"));
            Assert.AreEqual(FiltroPrecios.Ocio, filtro.Actual, "Cerrar sin elegir no cambia el filtro.");

            // La píldora alterna: abrir y volver a pulsarla cierra
            p.AlternarMenu();
            p.AlternarMenu();
            Assert.IsFalse(p.MenuAbierto);
        }

        /// <summary>
        /// Con el menú abierto, el velo tapa los botones de tema y zoom: un toque sobre ellos solo cierra el menú.
        /// En el pick de UI Toolkit gana el último hermano pintado, así que el velo debe ir detrás (en el árbol)
        /// de todo contenedor de controles salvo el de la píldora.
        /// </summary>
        [Test]
        public void ConElMenuAbiertoLosBotonesDeTemaYZoomQuedanBajoElVelo()
        {
            var (p, arbol, _, _) = Pantalla(new PreferenciasFalsas());
            var raiz = arbol.Q("raiz");
            p.AlternarMenu();
            int velo = raiz.IndexOf(p.Velo);
            foreach (var control in new VisualElement[] { p.BotonTema, arbol.Q("zoom"), arbol.Q("atribucion") })
            {
                var capa = control;
                while (capa.parent != raiz) capa = capa.parent;
                Assert.Less(raiz.IndexOf(capa), velo, $"{control.name} queda encima del velo: tocarlo haría algo más que cerrar el menú.");
            }
            VisualElement precios = p.Pildora;
            while (precios.parent != raiz) precios = precios.parent;
            Assert.Greater(raiz.IndexOf(precios), velo, "La píldora y el menú quedan encima del velo.");
            Assert.AreEqual(raiz.childCount - 1, raiz.IndexOf(precios), "Nada más va encima de la píldora y el menú.");
        }

        static readonly Dictionary<FiltroPrecios, string> ClaveEsperada = new Dictionary<FiltroPrecios, string>
        {
            [FiltroPrecios.Ninguno] = ClavesTexto.PreciosOpcionNinguno,
            [FiltroPrecios.Todos] = ClavesTexto.PreciosOpcionTodos,
            [FiltroPrecios.ServiciosLocales] = ClavesTexto.PreciosOpcionServicios,
            [FiltroPrecios.Hosteleria] = ClavesTexto.PreciosOpcionHosteleria,
            [FiltroPrecios.Ocio] = ClavesTexto.PreciosOpcionOcio,
        };

        [Test]
        public void LaOpcionElegidaLlevaCheckYFondo()
        {
            var (p, _, filtro, _) = Pantalla(new PreferenciasFalsas());
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosOpcionNinguno), p.ValorFiltro(), "Valor de TalkBack de la píldora.");
            filtro.Elegir(FiltroPrecios.Hosteleria);
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosOpcionHosteleria), p.ValorFiltro(), "El valor de TalkBack cambia al elegir.");
            p.AlternarMenu();
            Assert.AreEqual(5, p.Menu.Opciones.Count);
            foreach (var o in p.Menu.Opciones)
            {
                bool es = o.Filtro == FiltroPrecios.Hosteleria;
                Assert.AreEqual(es, o.Elegida, o.name);
                Assert.AreEqual(es, o.ClassListContains("opcion-precios--elegida"), o.name);
                Assert.AreEqual(Textos.T(ClaveEsperada[o.Filtro]), o.Texto.text, "Solo el nombre, sin contador.");
            }
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosTitulo), p.Menu.Titulo.text);
        }

        [Test]
        public void ConFiltroYFueraDeVistaSeOculta()
        {
            var bar = new Lugar("bar-fuera", "Bar fuera", "Bar", Plantilla.Hosteleria, new Coordenada(36.70, -6.35));
            var barDentro = new Lugar("bar-dentro", "Bar dentro", "Bar", Plantilla.Hosteleria, new Coordenada(36.71, -6.35));
            var clinica = new Lugar("clinica", "Clínica", "Clínica privada", Plantilla.ServicioLocal, new Coordenada(36.72, -6.35));
            var mapa = new MapaFalso();
            mapa.puntos[bar.Posicion] = new PuntoPantalla(AnchoPx + 400f, 500f, 100f); // 200 dp fuera de la pantalla
            var capa = new CapaMarcadores(new VisualElement(), mapa,
                new[] { bar, barDentro, clinica }.Select(l => (l, ReglaResumen.Resumir(l, Ahora))), c => PrepararInterfaz.TextosEs[c]);

            capa.Actualizar(Escala, AnchoPx, AltoPx);
            CollectionAssert.AreEqual(new[] { false, true, true }, capa.Marcadores.Select(m => m.Visible), "Sin filtro, solo cuenta la vista.");

            capa.Filtro = FiltroPrecios.Hosteleria;
            Assert.IsTrue(capa.Actualizar(Escala, AnchoPx, AltoPx));
            CollectionAssert.AreEqual(new[] { false, true, false }, capa.Marcadores.Select(m => m.Visible), "Las dos condiciones a la vez.");

            capa.Filtro = null;
            capa.Actualizar(Escala, AnchoPx, AltoPx);
            CollectionAssert.AreEqual(new[] { false, true, true }, capa.Marcadores.Select(m => m.Visible));
        }

        [Test]
        public void UnCambioDeFiltroCuentaComoCambioAunqueNoCambieNingunVisible()
        {
            var bar = new Lugar("bar", "Bar", "Bar", Plantilla.Hosteleria, new Coordenada(36.70, -6.35));
            var mapa = new MapaFalso { porDefecto = PuntoPantalla.Ninguno }; // todo detrás de la cámara
            var capa = new CapaMarcadores(new VisualElement(), mapa, new[] { (bar, ReglaResumen.Resumir(bar, Ahora)) }, c => PrepararInterfaz.TextosEs[c]);
            Assert.IsFalse(capa.Actualizar(Escala, AnchoPx, AltoPx));
            capa.Filtro = FiltroPrecios.Ocio;
            Assert.IsTrue(capa.Actualizar(Escala, AnchoPx, AltoPx));
            Assert.IsFalse(capa.Actualizar(Escala, AnchoPx, AltoPx));
        }

        [Test]
        public void TalkBackLeeLaPildoraComoPrimerBoton()
        {
            var (p, _, _, _) = Pantalla(new PreferenciasFalsas());
            Assert.AreEqual(Textos.T(ClavesTexto.PreciosControl), p.Accesibilidad.Recorrido()[0]);
        }

        [Test]
        public void LosControlesDelFiltroSonTactilesYNadaMasTapaElMapa()
        {
            var (p, arbol, _, _) = Pantalla(new PreferenciasFalsas());
            var raiz = arbol.Q("raiz");
            Assert.IsTrue(PantallaMapa.EsControl(raiz, p.Pildora));
            Assert.IsFalse(PantallaMapa.EsControl(raiz, arbol.Q("hueco-precios")), "El hueco no tapa el mapa.");
            Assert.AreEqual(PickingMode.Ignore, p.Pildora.Texto.pickingMode);
            foreach (var o in p.Menu.Opciones) Assert.IsTrue(PantallaMapa.EsControl(raiz, o));
        }

        [Test]
        public void SinServicioDeFiltroNoHayPildoraYSeVenTodos()
        {
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            var pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            pantalla.Inicializar(new MapaFalso(), new ServicioTema(new PreferenciasFalsas(), new SistemaClaro()), Lugares(), Ahora, arbol);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.IsNull(pantalla.Pildora);
            Assert.IsNull(pantalla.Capa.Filtro);
            Assert.AreEqual(124, Visibles(pantalla));
        }
    }
}
