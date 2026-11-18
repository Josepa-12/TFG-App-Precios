using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Ficha del lugar (historia 1.8, parte 1): una prueba por fila de la matriz de la especificación, con la
    /// pantalla real (PantallaMapa.uxml), los lugares reales y un mapa falso que solo proyecta los lugares dados.
    /// </summary>
    public class PruebasFicha
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        const float Escala = 0.5f;           // px de pantalla → dp
        const float AnchoPx = 1080f, AltoPx = 1920f;
        const string Nbsp = " ";       // espacio duro antes de €

        /// <summary>Solo se ven los lugares con punto propio; el resto queda detrás de la cámara.</summary>
        class MapaFalso : IMapa
        {
            public readonly Dictionary<Coordenada, PuntoPantalla> puntos = new Dictionary<Coordenada, PuntoPantalla>();
            public Action<PuntoPantalla> toque;
            public void Centrar(Coordenada destino) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => puntos.TryGetValue(punto, out var p) ? p : PuntoPantalla.Ninguno;
            public readonly List<PuntoInteres> puntosInteres = new List<PuntoInteres>();
            public IReadOnlyList<PuntoInteres> PuntosInteres => puntosInteres;
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add => toque += value; remove => toque -= value; }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
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

        static Lugar LugarId(string id) => Lugares().Lugares.Single(l => l.Id == id);
        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        ServicioFiltro filtro;

        [SetUp]
        public void Preparar()
        {
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro);
            tema.Iniciar();
            filtro.Iniciar();
            filtro.Elegir(FiltroPrecios.Todos); // con Ninguno (por defecto) no se ve ningún marcador
        }

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
        }

        /// <summary>Pone el lugar con la punta en ese punto (dp) y recoloca los marcadores. Devuelve su marcador.</summary>
        MarcadorLugar Colocar(string id, Vector2 puntaDp)
        {
            var lugar = LugarId(id);
            mapa.puntos[lugar.Posicion] = new PuntoPantalla(puntaDp.x / Escala, puntaDp.y / Escala, 300f);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            var m = pantalla.Capa.Marcadores.Single(x => x.Lugar.Id == id);
            Assert.IsTrue(m.Visible, $"{id} debería verse.");
            return m;
        }

        /// <summary>Toque del mapa (como lo lanza MapaVectorial: px con origen arriba a la izquierda) en un punto en dp.</summary>
        bool Tocar(Vector2 dp) => pantalla.TocarMapa(new PuntoPantalla(dp.x / Escala, dp.y / Escala, 1f), Escala);

        static readonly Vector2 Punta = new Vector2(270f, 600f);

        // ── Tocar un marcador ──

        [Test]
        public void TocarUnMarcadorAbreSuFicha()
        {
            // Menos de 6 dp de movimiento entre bajar y soltar el dedo es un toque (a 160 dpi, 1 px = 1 dp)
            var detector = new DetectorToque();
            detector.Empezar(new Vector2(100f, 100f));
            detector.Mover(new Vector2(103f, 102f));
            Assert.IsTrue(detector.Soltar(new Vector2(104f, 101f), 160f));

            var m = Colocar("clinica-areas", Punta);
            Assert.AreEqual(PickingMode.Ignore, m.pickingMode, "El marcador sigue sin capturar toques.");
            Assert.IsTrue(Tocar(m.ZonaDp.center), "Tocar encima del cuerpo abre la ficha.");
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreEqual("clinica-areas", pantalla.Ficha.Lugar.Id);
        }

        [Test]
        public void ElToqueDelMapaLlegaPorElPuerto()
        {
            // Sin disposición (EditMode) la escala sale del último fotograma; luego se recoloca con 1080×1920
            pantalla.ActualizarCapas(Escala);
            Colocar("clinica-areas", Punta);
            mapa.toque(new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 1f));
            Assert.IsTrue(pantalla.FichaAbierta, "El evento Toque de IMapa abre la ficha.");
        }

        [Test]
        public void UnToqueA20DpDelMarcadorAbreSuFicha()
        {
            var m = Colocar("clinica-areas", Punta);
            Assert.IsTrue(Tocar(new Vector2(m.ZonaDp.xMax + 20f, Punta.y)));
            Assert.AreEqual("clinica-areas", pantalla.Ficha.Lugar.Id);
        }

        [Test]
        public void UnToqueA30DpDelMarcadorNoAbreNada()
        {
            var m = Colocar("clinica-areas", Punta);
            Assert.IsFalse(Tocar(new Vector2(m.ZonaDp.xMax + 30f, Punta.y)));
            Assert.IsFalse(Tocar(new Vector2(Punta.x, Punta.y + 30f)));
            Assert.IsFalse(pantalla.FichaAbierta);
        }

        [Test]
        public void GanaElMarcadorMasCercano()
        {
            var areas = Colocar("clinica-areas", Punta);
            Colocar("chemabarber", Punta + new Vector2(60f, 0f));
            Assert.IsTrue(Tocar(new Vector2(areas.ZonaDp.xMax + 8f, Punta.y)));
            Assert.AreEqual("clinica-areas", pantalla.Ficha.Lugar.Id);
        }

        [Test]
        public void ArrastrarSobreUnMarcadorMueveElMapaYNoAbreNada()
        {
            var m = Colocar("clinica-areas", Punta);
            // El dedo empieza encima del marcador y se mueve 30 dp: es un arrastre, no hay toque
            var detector = new DetectorToque();
            detector.Empezar(m.ZonaDp.center);
            detector.Mover(m.ZonaDp.center + new Vector2(30f, 0f));
            Assert.IsFalse(detector.Soltar(m.ZonaDp.center + new Vector2(30f, 0f), 160f));
            // Volver al sitio antes de soltar tampoco es un toque: cuenta lo más lejos que llegó
            detector.Empezar(m.ZonaDp.center);
            detector.Mover(m.ZonaDp.center + new Vector2(30f, 0f));
            Assert.IsFalse(detector.Soltar(m.ZonaDp.center, 160f));
            // El mapa recibe el arrastre: nada de la capa de marcadores lo tapa
            var raiz = pantalla.Capa.Contenedor.parent;
            Assert.IsFalse(pantalla.TapaPunto(new Vector2(m.ZonaDp.center.x / Escala, AltoPx - m.ZonaDp.center.y / Escala)));
            Assert.IsFalse(PantallaMapa.EsControl(raiz, m.Cuerpo));
            Assert.IsFalse(pantalla.FichaAbierta);
        }

        [Test]
        public void LosSeisDpDependenDeLaDensidad()
        {
            Assert.AreEqual(6f, DetectorToque.UmbralPx(160f), 1e-4f);
            Assert.AreEqual(18f, DetectorToque.UmbralPx(480f), 1e-4f);
            var detector = new DetectorToque();
            detector.Empezar(Vector2.zero);
            Assert.IsTrue(detector.Soltar(new Vector2(10f, 0f), 480f), "10 px a 480 dpi son 3,3 dp: toque.");
        }

        // ── Contenido ──

        [Test]
        public void LaFichaDeClinicaAreasTieneSusAreasPreciosOrigenYFrescura()
        {
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            var ficha = pantalla.Ficha;

            Assert.AreEqual("Clínica Areas", ficha.Titulo.text);
            Assert.AreEqual("Clínica privada · Servicio local", ficha.Tipo.text);
            Assert.IsTrue(ficha.Muestra.ClassListContains("ficha__muestra--servicio"));
            Assert.AreEqual("C/ Santo Domingo, 21-23", ficha.Direccion.text);
            CollectionAssert.AreEqual(new[] { "Traumatología", "Fisioterapia", "Suelo pélvico", "Readaptación y entrenamiento" },
                ficha.Areas.Select(a => a.text));

            var sesion = ficha.Fila("fisioterapia/sesion-de-fisioterapia");
            Assert.AreEqual("Sesión de fisioterapia", sesion.Nombre.text);
            Assert.AreEqual("desde 40" + Nbsp + "€", sesion.Precio.text);
            Assert.AreEqual("/sesión", sesion.Unidad.text);
            Assert.AreEqual("Publicado en su web · 13/11/2026", sesion.Origen.text);
            Assert.AreEqual("Al día", sesion.Frescura.text);
            Assert.IsTrue(sesion.PuntoFrescura.ClassListContains("ficha__frescura-punto--al-dia"));

            Assert.AreEqual("90" + Nbsp + "€", ficha.Fila("traumatologia/consulta-de-traumatologia").Precio.text);
            Assert.IsNull(ficha.Fila("traumatologia/tratamiento-ecoguiado").Unidad, "La unidad \"servicio\" no se muestra.");

            var infiltraciones = ficha.Fila("traumatologia/infiltraciones");
            Assert.AreEqual("Consultar", infiltraciones.Precio.text);
            Assert.AreEqual("Publicado en su web · 13/11/2026", infiltraciones.Origen.text);
            Assert.IsNull(infiltraciones.Frescura, "Sin importe no hay punto de frescura.");
            Assert.IsNull(infiltraciones.Unidad);

            // Agrupados por área y en el orden de los datos: los 11 conceptos, sin "Aún no tiene precio"
            Assert.AreEqual(11, ficha.Filas.Count);
            Assert.AreEqual("traumatologia/consulta-de-traumatologia", ficha.Filas[0].Concepto.Id);
            Assert.AreEqual("readaptacion-y-entrenamiento/sesion-alterg", ficha.Filas[10].Concepto.Id);
            Assert.IsNull(ficha.SinPrecio);
        }

        [Test]
        public void LaFilaDeAccionesVaVaciaBajoLaCabecera()
        {
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            var hoja = pantalla.Ficha.Hoja;
            var acciones = hoja.Q("fila-acciones");
            Assert.IsNotNull(acciones);
            Assert.AreEqual(0, acciones.childCount, "Vacía hasta que otras épicas la rellenen (FR-10).");
            Assert.AreEqual(hoja.IndexOf(hoja.Q("ficha-cabecera")) + 1, hoja.IndexOf(acciones), "Justo bajo la cabecera.");
            Assert.Less(hoja.IndexOf(acciones), hoja.IndexOf(pantalla.Ficha.Contenido));
        }

        [Test]
        public void UnLugarSinPrecioDiceAunNoTienePrecio()
        {
            pantalla.AbrirFicha(LugarId("chemabarber"));
            var ficha = pantalla.Ficha;
            Assert.AreEqual("ChemaBarber", ficha.Titulo.text);
            Assert.IsNotNull(ficha.SinPrecio);
            Assert.AreEqual("Aún no tiene precio", ficha.SinPrecio.text);
            Assert.AreEqual(0, ficha.Filas.Count);
            Assert.AreEqual(0, ficha.Areas.Count);
        }

        [Test]
        public void ElRangoDeHosteleriaEsPorPersona()
        {
            Assert.AreEqual("10–20" + Nbsp + "€ por persona", FormatoPrecio.Precio(ValorPrecio.Rango(1000, 2000), T));
            Assert.AreEqual("más de 50" + Nbsp + "€ por persona", FormatoPrecio.Precio(ValorPrecio.Rango(5000, null), T));

            // Concepto sintético en un bar: la ficha muestra el rango, el origen y la frescura de hostelería
            var web = new PrecioWeb(ValorPrecio.Rango(1000, 2000), "https://ejemplo.es/carta", new DateTime(2026, 9, 1));
            var bar = new Lugar("bar-prueba", "Bar de prueba", "Bar", Plantilla.Hosteleria, new Coordenada(36.78, -6.35),
                conceptos: new[] { new Concepto("comer/menu", "", "Comer", "persona", new FuentesPrecio(Plantilla.Hosteleria, web)) });
            pantalla.AbrirFicha(bar);
            var fila = pantalla.Ficha.Filas.Single();
            Assert.AreEqual("10–20" + Nbsp + "€ por persona", fila.Precio.text);
            Assert.AreEqual("Publicado en su web · 01/09/2026", fila.Origen.text);
            Assert.AreEqual("Antiguo", fila.Frescura.text, "78 días en hostelería: antiguo.");
            Assert.IsTrue(fila.PuntoFrescura.ClassListContains("ficha__frescura-punto--antiguo"));
            Assert.IsNull(fila.Unidad, "En un rango el texto ya dice \"por persona\": sin unidad repetida.");
            Assert.IsTrue(pantalla.Ficha.Muestra.ClassListContains("ficha__muestra--hosteleria"));
            Assert.AreEqual("Bar · Hostelería", pantalla.Ficha.Tipo.text);
            Assert.AreEqual(DisplayStyle.None, pantalla.Ficha.Direccion.style.display.value, "Sin dirección no se ve la línea.");
            Assert.AreEqual(0, pantalla.Ficha.Areas.Count, "Sin área no hay encabezado.");
        }

        [Test]
        public void LaFrescuraTieneTresTextos()
        {
            Assert.AreEqual("Al día", T(FilaConcepto.ClaveFrescura(EstadoFrescura.AlDia)));
            Assert.AreEqual("Podría haber cambiado", T(FilaConcepto.ClaveFrescura(EstadoFrescura.PodriaHaberCambiado)));
            Assert.AreEqual("Antiguo", T(FilaConcepto.ClaveFrescura(EstadoFrescura.Antiguo)));
            Assert.AreEqual("ficha__frescura-punto--podria", FilaConcepto.ClaseFrescura(EstadoFrescura.PodriaHaberCambiado));
        }

        // ── Cerrar ──

        [Test]
        public void ElBotonAtrasCierraLaFicha()
        {
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.Atras(), "Sin ficha ni menú, atrás no hace nada.");
        }

        [Test]
        public void LaXCierraLaFicha()
        {
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            var x = pantalla.Ficha.BotonCerrar;
            Assert.AreEqual(Icono.Cerrar, x.Icono);
            Assert.AreEqual(PickingMode.Position, x.pickingMode);
            x.Pulsar();
            Assert.IsFalse(pantalla.FichaAbierta);
        }

        [Test]
        public void TocarElMapaFueraDeLaFichaLaCierraSinMoverlo()
        {
            var m = Colocar("clinica-areas", Punta);
            pantalla.AbrirFicha(LugarId("chemabarber"));
            // Con la ficha abierta todo el mapa cuenta como tapado: el gesto no lo mueve
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, AltoPx - 10f)));
            Assert.AreEqual(PickingMode.Position, pantalla.Ficha.Velo.pickingMode);
            pantalla.Ficha.TocarFuera(); // lo que hace el velo al soltar
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.TapaPunto(new Vector2(10f, AltoPx - 10f)), "Cerrada, el mapa vuelve a moverse.");

            // Y si el toque llegara al mapa (camino del puerto), tampoco abre otra ficha: solo cierra
            pantalla.AbrirFicha(LugarId("chemabarber"));
            Assert.IsFalse(Tocar(m.ZonaDp.center));
            Assert.IsFalse(pantalla.FichaAbierta);
        }

        [Test]
        public void ConElMenuAbiertoElToqueSoloLoCierra()
        {
            var m = Colocar("clinica-areas", Punta);
            pantalla.AlternarMenu();
            Assert.IsTrue(pantalla.MenuAbierto);
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)), "Con el menú abierto el gesto no llega al mapa.");
            Assert.IsFalse(Tocar(m.ZonaDp.center));
            Assert.IsFalse(pantalla.MenuAbierto, "El toque cierra el menú.");
            Assert.IsFalse(pantalla.FichaAbierta, "Y no abre la ficha.");
        }

        [Test]
        public void TocarUnPuntoDeInteresNoAbreNada()
        {
            // Un supermercado de OpenStreetMap en (60, 60) dp, lejos de todo marcador de lugar
            Colocar("clinica-areas", Punta);
            var donde = new Vector2(60f, 60f);
            var poi = new PuntoInteres(1, "Súper de prueba", "grocery", "supermarket", 1, new Coordenada(36.70, -6.30));
            mapa.puntosInteres.Add(poi);
            mapa.puntos[poi.Posicion] = new PuntoPantalla(donde.x / Escala, donde.y / Escala, 300f);
            pantalla.CapaPuntos.Sincronizar();
            pantalla.CapaPuntos.Actualizar(Escala, AnchoPx, AltoPx);
            var vista = pantalla.CapaPuntos.Vistas.Single(v => v.Punto == poi);
            Assert.IsTrue(vista.Visible, "El punto de interés se ve justo donde se toca.");

            Assert.IsFalse(Tocar(donde));
            Assert.IsFalse(pantalla.FichaAbierta, "Tocar un punto de interés no abre nada (su ficha es de la épica 2).");
        }

        [Test]
        public void ConDosMarcadoresSolapadosGanaElMasCercanoALaCamara()
        {
            var lejos = LugarId("clinica-areas");
            var cerca = LugarId("fisiosanlucar");
            mapa.puntos[lejos.Posicion] = new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 500f);
            mapa.puntos[cerca.Posicion] = new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 200f);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.IsTrue(Tocar(Punta), "En la punta, la distancia a los dos es 0.");
            Assert.AreEqual("fisiosanlucar", pantalla.Ficha.Lugar.Id, "Gana el que se pinta encima (menor profundidad).");
        }

        [Test]
        public void AbrirElMenuConLaFichaAbiertaLaCierra()
        {
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            pantalla.AlternarMenu(); // la píldora activada por teclado o TalkBack
            Assert.IsTrue(pantalla.MenuAbierto);
            Assert.IsFalse(pantalla.FichaAbierta, "El menú no queda debajo de la ficha.");
        }
    }
}
