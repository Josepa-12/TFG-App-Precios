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
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Ficha del lugar (historia 1.8, partes 1 y 2): una prueba por fila de la matriz de cada especificación, con la
    /// pantalla real (PantallaMapa.uxml), los lugares reales (las variantes, con lugares sintéticos) y un mapa falso
    /// que solo proyecta los lugares dados y apunta dónde se le pide centrar.
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
            public readonly List<(Coordenada destino, PuntoPantalla enPantalla)> centradosEn = new List<(Coordenada, PuntoPantalla)>();
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) => centradosEn.Add((destino, enPantalla));
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
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
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
            Crear(Ahora);
        }

        /// <summary>Crea la pantalla (sustituye a la anterior) con ese "ahora" y, si se da, la ubicación.</summary>
        void Crear(DateTime ahora, Func<IPreferencias, ServicioUbicacion> ubicacion = null)
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), ahora, arbol, filtro, ubicacion?.Invoke(prefs));
            tema.Iniciar();
            filtro.Iniciar();
            filtro.Elegir(FiltroPrecios.Todos); // con Ninguno (por defecto) no se ve ningún marcador
        }

        /// <summary>Ubicación fija en un estado (sin GPS): solo para abrir el aviso o la explicación.</summary>
        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; }
            public Coordenada? Posicion => null;
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

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
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

            // Sin los conceptos sin importe ("Consultar"), borrados de los datos el 18/11/2026
            Assert.IsNull(ficha.Fila("traumatologia/infiltraciones"));
            Assert.IsNull(ficha.Fila("traumatologia/factores-de-crecimiento"));
            Assert.IsNull(ficha.Fila("fisioterapia/recuperacion-con-frio-aquilo-phisiolab"));
            Assert.IsTrue(ficha.Filas.All(f => f.Precio.text != "Consultar"));

            // Agrupados por área y en el orden de los datos: los 8 conceptos, sin "Aún no tiene precio"
            Assert.AreEqual(8, ficha.Filas.Count);
            Assert.AreEqual("traumatologia/consulta-de-traumatologia", ficha.Filas[0].Concepto.Id);
            Assert.AreEqual("readaptacion-y-entrenamiento/sesion-alterg", ficha.Filas[7].Concepto.Id);
            Assert.IsNull(ficha.SinPrecio);
        }

        /// <summary>Lugar sintético con un concepto sin importe: "Consultar" se sigue probando sin datos reales.</summary>
        static Lugar ConConsultar() => new Lugar("clinica-prueba", "Clínica de prueba", "Clínica privada", Plantilla.ServicioLocal,
            new Coordenada(36.78, -6.35), conceptos: new[]
            {
                new Concepto("traumatologia/infiltraciones", "Traumatología", "Infiltraciones", "servicio",
                    new FuentesPrecio(Plantilla.ServicioLocal, new PrecioWeb(null, "https://ejemplo.es/tarifas", new DateTime(2026, 11, 13)))),
            });

        [Test]
        public void UnConceptoSinImporteDiceConsultarSinFrescuraNiUnidad()
        {
            pantalla.AbrirFicha(ConConsultar());
            var infiltraciones = pantalla.Ficha.Fila("traumatologia/infiltraciones");
            Assert.AreEqual("Consultar", infiltraciones.Precio.text);
            Assert.AreEqual("Publicado en su web · 13/11/2026", infiltraciones.Origen.text);
            Assert.IsNull(infiltraciones.Frescura, "Sin importe no hay punto de frescura.");
            Assert.IsNull(infiltraciones.Unidad);
        }

        [Test]
        public void LaFilaDeAccionesVaVaciaBajoLaCabecera()
        {
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            var hoja = pantalla.Ficha.Hoja;
            var acciones = hoja.Q("fila-acciones");
            Assert.IsNotNull(acciones);
            // Historia 2.6: la fila lleva los modos e Ir, ocultos sin servicio de rutas (PruebasRutas)
            Assert.AreEqual(1, acciones.childCount);
            Assert.AreSame(pantalla.Ficha.Acciones, acciones[0]);
            Assert.IsFalse(pantalla.Ficha.AccionesVisibles, "Sin servicio de rutas no se ve nada (FR-10).");
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
        public void ConDosMarcadoresSolapadosSinPrecioGanaElMasCercanoALaCamara()
        {
            var lejos = LugarId("chemabarber");
            var cerca = LugarId("fisiosanlucar");
            mapa.puntos[lejos.Posicion] = new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 500f);
            mapa.puntos[cerca.Posicion] = new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 200f);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.IsFalse(pantalla.Capa.Marcadores.Single(m => m.Lugar == lejos).Visible, "De dos que se pisan solo se ve uno.");
            Assert.IsTrue(Tocar(Punta), "En la punta se toca el que se ve.");
            Assert.AreEqual("fisiosanlucar", pantalla.Ficha.Lugar.Id, "Entre dos sin precio, gana el más cercano a la cámara.");
        }

        /// <summary>Fila "Con precio gana": un lugar sin precio más cercano que pisa a Clínica Areas se oculta.</summary>
        [Test]
        public void ConDosMarcadoresSolapadosGanaElQueTienePrecio()
        {
            var conPrecio = LugarId("clinica-areas");
            var sinPrecio = LugarId("fisiosanlucar");
            mapa.puntos[conPrecio.Posicion] = new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 500f);
            mapa.puntos[sinPrecio.Posicion] = new PuntoPantalla((Punta.x + 10f) / Escala, Punta.y / Escala, 200f);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.IsTrue(pantalla.Capa.Marcadores.Single(m => m.Lugar == conPrecio).Visible);
            Assert.IsFalse(pantalla.Capa.Marcadores.Single(m => m.Lugar == sinPrecio).Visible, "Aunque esté más cerca de la cámara.");
            Assert.IsTrue(Tocar(Punta));
            Assert.AreEqual("clinica-areas", pantalla.Ficha.Lugar.Id);
        }

        /// <summary>
        /// Con su ficha abierta, el marcador del lugar no se oculta aunque lo pise otro con precio ni bajo un control;
        /// al cerrarla vuelven las reglas normales.
        /// </summary>
        [Test]
        public void ElMarcadorDeLaFichaAbiertaNuncaSeOculta()
        {
            var conPrecio = LugarId("clinica-areas");
            var chema = LugarId("chemabarber");
            mapa.puntos[conPrecio.Posicion] = new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 200f);
            mapa.puntos[chema.Posicion] = new PuntoPantalla((Punta.x + 10f) / Escala, Punta.y / Escala, 500f);
            var marcadorChema = pantalla.Capa.Marcadores.Single(m => m.Lugar == chema);
            var marcadorAreas = pantalla.Capa.Marcadores.Single(m => m.Lugar == conPrecio);
            var control = new List<Rect> { new Rect(Punta.x - 10f, Punta.y - 40f, 40f, 30f) }; // un control encima de los dos
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.IsFalse(marcadorChema.Visible, "Sin ficha gana el que tiene precio.");

            pantalla.AbrirFicha(chema);
            Assert.AreSame(marcadorChema, pantalla.Capa.Fijado);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx, control);
            Assert.IsTrue(marcadorChema.Visible, "El de la ficha abierta se ve, bajo el control y pisando al que tiene precio.");
            Assert.IsFalse(marcadorAreas.Visible);

            pantalla.CerrarFicha();
            Assert.IsNull(pantalla.Capa.Fijado);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx, control);
            Assert.IsFalse(marcadorChema.Visible, "Cerrada la ficha, el control lo oculta.");
        }

        /// <summary>Fila "TalkBack": con el lector, un marcador oculto por solape no está en el recorrido.</summary>
        [Test]
        public void UnMarcadorOcultoPorSolapeNoEstaEnElRecorrido()
        {
            var conPrecio = LugarId("clinica-areas");
            var sinPrecio = LugarId("fisiosanlucar");
            mapa.puntos[conPrecio.Posicion] = new PuntoPantalla(Punta.x / Escala, Punta.y / Escala, 500f);
            mapa.puntos[sinPrecio.Posicion] = new PuntoPantalla((Punta.x + 10f) / Escala, Punta.y / Escala, 200f);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            ConLector();
            Acc.ActualizarMarcadores();
            var visible = pantalla.Capa.Marcadores.Single(m => m.Lugar == conPrecio);
            var oculto = pantalla.Capa.Marcadores.Single(m => m.Lugar == sinPrecio);
            Assert.IsFalse(oculto.Visible);
            var recorrido = Acc.Recorrido();
            CollectionAssert.Contains(recorrido, visible.Anuncio);
            CollectionAssert.DoesNotContain(recorrido, oculto.Anuncio, "El oculto por solape no sale.");
            CollectionAssert.DoesNotContain(AssistiveSupport.activeHierarchy.rootNodes.Select(n => n.label).ToList(), oculto.Anuncio);
        }

        // ── Variantes y centrado (historia 1.8, parte 2) ──

        /// <summary>"Ahora" del Arranque (UTC) que en el móvil son esas horas locales del 18/11/2026.</summary>
        static DateTime UtcDeLocal(int hora) => new DateTime(2026, 11, 18, hora, 0, 0, DateTimeKind.Local).ToUniversalTime();

        static readonly DateTime FechaWeb = new DateTime(2026, 11, 13);

        static FuentesPrecio Web(ValorPrecio valor) =>
            new FuentesPrecio(Plantilla.Ocio, new PrecioWeb(valor, "https://ejemplo.es/tarifas", FechaWeb));

        /// <summary>Lugar sintético con un concepto de 20 € y sus variantes (no se tocan los datos reales).</summary>
        static Lugar Padel(params Variante[] variantes) =>
            new Lugar("padel-prueba", "Pádel de prueba", "Club de pádel", Plantilla.Ocio, new Coordenada(36.78, -6.35),
                conceptos: new[] { new Concepto("pista", "", "Pista de pádel", "hora", Web(ValorPrecio.Fijo(2000)), variantes) });

        static Variante Noche(TimeSpan desde, TimeSpan hasta, int cent = 2400) =>
            new Variante("pista@noche", CondicionVariante.Horario(desde, hasta), Web(ValorPrecio.Fijo(cent)));

        FilaConcepto AbrirPadel(int horaLocal, params Variante[] variantes)
        {
            pantalla.Ficha.Abrir(Padel(variantes), UtcDeLocal(horaLocal));
            return pantalla.Ficha.Filas.Single();
        }

        [Test]
        public void LaVarianteVigenteVaBajoSuConceptoConElChipAhora()
        {
            var fila = AbrirPadel(21, Noche(new TimeSpan(20, 0, 0), TimeSpan.Zero));
            Assert.AreEqual("Pista de pádel", fila.Nombre.text);
            Assert.AreEqual(FormatoPrecio.Euros(2000), fila.Precio.text);
            var variante = fila.Variantes.Single();
            Assert.IsTrue(fila.Contains(variante), "La variante va dentro de la fila de su concepto.");
            Assert.AreEqual("De 20:00 a 24:00 · " + FormatoPrecio.Euros(2400), variante.Texto);
            Assert.IsTrue(variante.Vigente);
            Assert.IsNotNull(variante.Chip);
            Assert.AreEqual("Ahora", variante.Chip.text);
            Assert.AreEqual(0, variante.IndexOf(variante.Chip), "El chip va delante de la condición.");
            Assert.IsTrue(variante.ClassListContains("ficha__variante--vigente"));
        }

        [Test]
        public void LaVarianteNoVigenteNoLlevaElChip()
        {
            var variante = AbrirPadel(10, Noche(new TimeSpan(20, 0, 0), TimeSpan.Zero)).Variantes.Single();
            Assert.AreEqual("De 20:00 a 24:00 · " + FormatoPrecio.Euros(2400), variante.Texto);
            Assert.IsFalse(variante.Vigente);
            Assert.IsNull(variante.Chip);
        }

        [Test]
        public void UnaFranjaQueCruzaLaMedianocheEstaVigenteALaUna()
        {
            var variante = AbrirPadel(1, Noche(new TimeSpan(22, 0, 0), new TimeSpan(2, 0, 0))).Variantes.Single();
            Assert.AreEqual("De 22:00 a 02:00 · " + FormatoPrecio.Euros(2400), variante.Texto);
            Assert.IsTrue(variante.Vigente);
            Assert.IsNotNull(variante.Chip);
        }

        [Test]
        public void LosDiasSeguidosSeJuntan()
        {
            var c = CondicionVariante.EnDias(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
            Assert.AreEqual("Lun a vie", FilaConcepto.TextoCondicion(c, T));
            // El orden de los datos no importa: la semana empieza en lunes
            var desordenados = CondicionVariante.EnDias(DayOfWeek.Sunday, DayOfWeek.Friday, DayOfWeek.Saturday);
            Assert.AreEqual("Vie a dom", FilaConcepto.TextoCondicion(desordenados, T));
        }

        [Test]
        public void LosDiasSueltosVanEnLista()
        {
            var c = CondicionVariante.EnDias(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Friday);
            Assert.AreEqual("Lun, mar y vie", FilaConcepto.TextoCondicion(c, T));
            Assert.AreEqual("Sáb y dom", FilaConcepto.TextoCondicion(CondicionVariante.EnDias(DayOfWeek.Saturday, DayOfWeek.Sunday), T));
            Assert.AreEqual("Mié", FilaConcepto.TextoCondicion(CondicionVariante.EnDias(DayOfWeek.Wednesday), T));
        }

        [Test]
        public void LaVarianteDeClienteNuncaSeDestaca()
        {
            var bono = new Variante("pista@con-bono", CondicionVariante.DeCliente("con-bono"), Web(ValorPrecio.Desde(1500)));
            foreach (var hora in new[] { 1, 10, 21 })
            {
                var variante = AbrirPadel(hora, bono).Variantes.Single();
                Assert.AreEqual("Con bono · desde " + FormatoPrecio.Euros(1500), variante.Texto);
                Assert.IsFalse(variante.Vigente, $"A las {hora}:00 tampoco.");
                Assert.IsNull(variante.Chip);
            }
            Assert.AreEqual("Estudiante", FilaConcepto.TextoCondicion(CondicionVariante.DeCliente("estudiante"), T));
        }

        [Test]
        public void ComoMuchoUnaVarianteVaDestacada()
        {
            // A las 21:00 se cumplen las dos franjas: solo la primera lleva el chip
            var tarde = new Variante("pista@tarde", CondicionVariante.Horario(new TimeSpan(18, 0, 0), new TimeSpan(22, 0, 0)), Web(ValorPrecio.Fijo(2200)));
            var fila = AbrirPadel(21, tarde, Noche(new TimeSpan(20, 0, 0), TimeSpan.Zero));
            Assert.AreEqual(2, fila.Variantes.Count);
            Assert.AreEqual(1, fila.Variantes.Count(v => v.Vigente));
            Assert.IsTrue(fila.Variantes[0].Vigente);
        }

        [Test]
        public void LaVigenteSeEligeSoloEntreLasVariantesQueSeVen()
        {
            // Las dos se cumplen a las 21:00, pero la primera no tiene nada que mostrar: el chip va en la segunda
            var oculta = new Variante("pista@sin-precio", CondicionVariante.Horario(new TimeSpan(18, 0, 0), new TimeSpan(23, 0, 0)),
                new FuentesPrecio(Plantilla.Ocio));
            var fila = AbrirPadel(21, oculta, Noche(new TimeSpan(20, 0, 0), TimeSpan.Zero));
            Assert.AreEqual(1, fila.Variantes.Count);
            Assert.AreEqual("pista@noche", fila.Variantes[0].Variante.Id);
            Assert.IsTrue(fila.Variantes[0].Vigente);
            Assert.IsNotNull(fila.Variantes[0].Chip);
        }

        [Test]
        public void LaVigenteUsaLaHoraLocalDelMovil()
        {
            var utc = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
            var desfase = TimeZoneInfo.Local.GetUtcOffset(utc);
            Assume.That(desfase != TimeSpan.Zero, "Con la máquina en UTC la conversión no cambia nada.");
            // Franja [hora local, hora local + |desfase|): contiene la hora local, nunca la hora UTC
            var local = (utc + desfase).TimeOfDay;
            var hasta = TimeSpan.FromTicks((local + desfase.Duration()).Ticks % TimeSpan.TicksPerDay);
            var franja = new Variante("pista@franja", CondicionVariante.Horario(local, hasta), Web(ValorPrecio.Fijo(2400)));
            pantalla.Ficha.Abrir(Padel(franja), utc);
            var variante = pantalla.Ficha.Filas.Single().Variantes.Single();
            Assert.IsTrue(variante.Vigente, $"A las {utc:HH:mm} UTC son las {local} en el móvil: dentro de la franja.");
            Assert.IsNotNull(variante.Chip);
        }

        [Test]
        public void AlAbrirLaFichaElLugarSeCentraEncimaDeLaHoja()
        {
            var areas = LugarId("clinica-areas");
            pantalla.AbrirFicha(areas);
            var (destino, enPantalla) = mapa.centradosEn.Single();
            Assert.AreEqual(areas.Posicion, destino);
            // En EditMode la hoja aún no tiene medida: se supone el 72 % del alto
            Assert.AreEqual(Screen.width / 2f, enPantalla.X, 1e-3f);
            Assert.AreEqual(Screen.height * (1f - 0.72f) / 2f, enPantalla.Y, 1e-3f);

            // Con la hoja medida: centro horizontal y mitad de la zona que no tapa
            var p = PantallaMapa.PuntoCentradoFicha(1080f, 1920f, 1200f);
            Assert.AreEqual(540f, p.X, 1e-3f);
            Assert.AreEqual(600f, p.Y, 1e-3f);
        }

        [Test]
        public void AbrirElMenuConLaFichaAbiertaLaCierra()
        {
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            pantalla.AlternarMenu(); // la píldora activada por teclado o TalkBack
            Assert.IsTrue(pantalla.MenuAbierto);
            Assert.IsFalse(pantalla.FichaAbierta, "El menú no queda debajo de la ficha.");
        }

        // ── TalkBack de la ficha (historia 1.8, parte 3) ──

        AccesibilidadPantalla Acc => pantalla.Accesibilidad;

        /// <summary>Enciende el lector forzado y rehace la jerarquía; si el editor no la activa, la prueba se ignora.</summary>
        void ConLector()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Acc.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
        }

        static List<AccessibilityNode> Nodos() => AssistiveSupport.activeHierarchy.rootNodes.ToList();
        static List<string> Etiquetas() => Nodos().Select(n => n.label).ToList();
        static AccessibilityNode Nodo(string etiqueta) => Nodos().Single(n => n.label == etiqueta);

        /// <summary>
        /// Lo que hace el doble toque de TalkBack: lanza el evento invoked del nodo. El evento no se puede lanzar
        /// desde fuera, así que se lee su delegado por reflexión.
        /// </summary>
        static void Activar(AccessibilityNode nodo)
        {
            var accion = Delegado<Func<bool>>(nodo, "invoked");
            Assert.IsNotNull(accion, $"El nodo \"{nodo.label}\" no tiene acción.");
            Assert.IsTrue(accion());
        }

        /// <summary>Lo que hace TalkBack al poner el foco en el nodo: lanza su evento focusChanged (por reflexión).</summary>
        static void LanzarFoco(AccessibilityNode nodo) =>
            Delegado<Action<AccessibilityNode, bool>>(nodo, "focusChanged")?.Invoke(nodo, true);

        /// <summary>Delegado del evento de ese nombre en el nodo (null sin suscriptores).</summary>
        static T Delegado<T>(AccessibilityNode nodo, string evento) where T : Delegate
        {
            const System.Reflection.BindingFlags Campos = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var campo = typeof(AccessibilityNode).GetFields(Campos)
                .FirstOrDefault(f => typeof(T).IsAssignableFrom(f.FieldType) && f.Name.IndexOf(evento, StringComparison.OrdinalIgnoreCase) >= 0);
            if (campo == null) Assert.Inconclusive($"Esta versión de Unity no deja leer el evento {evento} del nodo.");
            return campo.GetValue(nodo) as T;
        }

        /// <summary>Recorrido esperado de la ficha abierta: sus textos en orden y "Cerrar ficha" al final.</summary>
        List<string> RecorridoDeLaFicha() =>
            pantalla.Ficha.TextosTalkBack().Select(t => t.texto).Append(T(ClavesTexto.FichaCerrar)).ToList();

        [Test]
        public void ConTalkBackElDobleToqueEnUnMarcadorAbreSuFicha()
        {
            var m = Colocar("clinica-areas", Punta);
            ConLector();
            var nodo = Nodo(m.Anuncio);
            Assert.AreEqual(AccessibilityRole.Button, nodo.role, "El marcador de un lugar tiene acción.");
            Activar(nodo);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreEqual("clinica-areas", pantalla.Ficha.Lugar.Id);
            Assert.IsTrue(Acc.Modal);
            Assert.AreSame(pantalla.Ficha.Titulo, Acc.Enfocado, "El foco va al título.");
            Assert.AreSame(Nodo("Clínica Areas"), Acc.UltimoNodoEnfocado, "TalkBack recibe el nodo del título.");
        }

        [Test]
        public void ElFocoDeTalkBackEnUnConceptoDesplazaLaFicha()
        {
            ConLector();
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            var ficha = pantalla.Ficha;
            LanzarFoco(Nodo("Clínica Areas"));
            Assert.AreEqual(0, ficha.Desplazamientos, "El título está fuera del contenido: no se desplaza.");

            var fila = ficha.Fila("readaptacion-y-entrenamiento/sesion-alterg");
            LanzarFoco(Nodo(fila.Anuncio));
            Assert.AreEqual(1, ficha.Desplazamientos);
            Assert.AreSame(fila, ficha.UltimoDesplazado, "Se desplaza hasta el concepto enfocado.");
        }

        [Test]
        public void ConLaFichaAbiertaElAvisoDeUbicacionLaCierraYEsElRecorrido()
        {
            Crear(Ahora, prefs => new ServicioUbicacion(new UbicacionFalsa { Estado = EstadoUbicacion.Denegado }, prefs));
            ConLector();
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            pantalla.TocarUbicacion(); // denegado: abre el aviso
            Assert.IsTrue(pantalla.AvisoAbierto);
            Assert.IsFalse(pantalla.FichaAbierta, "El aviso cierra la ficha.");
            Assert.IsTrue(Acc.Modal);
            var aviso = pantalla.Aviso;
            var esperado = new[] { aviso.Texto.text, aviso.Cerrar.text, aviso.AbrirAjustes.text };
            CollectionAssert.AreEqual(esperado, Etiquetas(), "El recorrido es el del aviso.");
            CollectionAssert.AreEqual(esperado, Acc.Recorrido());
            Assert.AreSame(aviso.Texto, Acc.Enfocado);

            pantalla.CerrarAviso();
            Assert.IsFalse(Acc.Modal);
            Assert.IsFalse(pantalla.FichaAbierta);
            CollectionAssert.DoesNotContain(Etiquetas(), "Clínica Areas");
        }

        [Test]
        public void ConLaFichaAbiertaLaExplicacionDeUbicacionLaCierra()
        {
            Crear(Ahora, prefs =>
            {
                prefs.Guardar(ServicioUbicacion.ClaveExplicacion, "1"); // ya salió al abrir: no sale sola
                return new ServicioUbicacion(new UbicacionFalsa { Estado = EstadoUbicacion.SinPermiso }, prefs);
            });
            ConLector();
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            pantalla.TocarUbicacion(); // sin permiso: vuelve a salir la explicación
            Assert.IsTrue(pantalla.ExplicacionAbierta);
            Assert.IsFalse(pantalla.FichaAbierta);
            var e = pantalla.Explicacion;
            CollectionAssert.AreEqual(new[] { e.Texto.text, e.AhoraNo.text, e.Permitir.text }, Etiquetas());
        }

        [Test]
        public void ConLaFichaAbiertaElRecorridoSoloTieneLaFicha()
        {
            var m = Colocar("clinica-areas", Punta);
            ConLector();
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            var ficha = pantalla.Ficha;

            var esperado = new List<string> { "Clínica Areas", "Clínica privada · Servicio local", "C/ Santo Domingo, 21-23" };
            string area = null;
            foreach (var f in ficha.Filas)
            {
                if (f.Concepto.Area != area) { area = f.Concepto.Area; esperado.Add(area); }
                esperado.Add(f.Anuncio);
                esperado.AddRange(f.Variantes.Select(v => v.Anuncio));
            }
            esperado.Add("Cerrar ficha");
            CollectionAssert.AreEqual(esperado, Etiquetas(), "Título, tipo, dirección, áreas y conceptos en orden; \"Cerrar ficha\" al final.");
            CollectionAssert.AreEqual(esperado, Acc.Recorrido());
            Assert.AreEqual(AccessibilityRole.Button, Nodo("Cerrar ficha").role);
            Assert.AreEqual(AccessibilityRole.StaticText, Nodo("Clínica Areas").role);

            var etiquetas = Etiquetas();
            CollectionAssert.DoesNotContain(etiquetas, m.Anuncio, "Ningún marcador.");
            foreach (var clave in new[] { ClavesTexto.TemaCambiar, ClavesTexto.PreciosControl, ClavesTexto.MapaAcercar, ClavesTexto.MapaAlejar })
                CollectionAssert.DoesNotContain(etiquetas, T(clave), "Ningún control del mapa.");
            Assert.IsEmpty(Acc.RecorridoDeMarcas());
        }

        [Test]
        public void LaFraseDeUnConceptoDiceNombrePrecioUnidadOrigenYFrescura()
        {
            ConLector();
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            var frase = "Sesión de fisioterapia, desde 40" + Nbsp + "€ por sesión. Publicado en su web el 13/11/2026. Al día";
            Assert.AreEqual(frase, pantalla.Ficha.Fila("fisioterapia/sesion-de-fisioterapia").Anuncio);
            Assert.AreEqual(AccessibilityRole.StaticText, Nodo(frase).role);
        }

        [Test]
        public void LaFraseDeConsultarNoTieneUnidadNiFrescura()
        {
            pantalla.AbrirFicha(ConConsultar());
            var fila = pantalla.Ficha.Fila("traumatologia/infiltraciones");
            Assert.AreEqual("Infiltraciones", fila.Nombre.text);
            Assert.AreEqual("Infiltraciones, Consultar. Publicado en su web el 13/11/2026", fila.Anuncio);
        }

        [Test]
        public void LaVarianteVigenteSeLeeTrasSuConceptoConAhora()
        {
            Crear(UtcDeLocal(21)); // la pantalla calcula la ficha con este "ahora": las 21:00 en el móvil
            ConLector();
            pantalla.AbrirFicha(Padel(Noche(new TimeSpan(20, 0, 0), TimeSpan.Zero)));
            var fila = pantalla.Ficha.Filas.Single();
            var frase = "Ahora: de 20:00 a 24:00, " + FormatoPrecio.Euros(2400);
            Assert.AreEqual(frase, fila.Variantes.Single().Anuncio);
            var etiquetas = Etiquetas();
            int i = etiquetas.IndexOf(fila.Anuncio);
            Assert.GreaterOrEqual(i, 0, "El concepto tiene su nodo.");
            Assert.AreEqual(frase, etiquetas[i + 1], "El nodo de la variante va justo después del de su concepto.");

            var noVigente = AbrirPadel(10, Noche(new TimeSpan(20, 0, 0), TimeSpan.Zero)).Variantes.Single();
            Assert.AreEqual("De 20:00 a 24:00, " + FormatoPrecio.Euros(2400), noVigente.Anuncio);
        }

        [Test]
        public void SinPrecioSeLeeAunNoTienePrecioYCerrarFicha()
        {
            ConLector();
            pantalla.AbrirFicha(LugarId("chemabarber"));
            var etiquetas = Etiquetas();
            Assert.AreEqual("ChemaBarber", etiquetas.First());
            CollectionAssert.AreEqual(new[] { "Aún no tiene precio", "Cerrar ficha" }, etiquetas.Skip(etiquetas.Count - 2));
            CollectionAssert.AreEqual(RecorridoDeLaFicha(), etiquetas);
        }

        [Test]
        public void CerrarFichaConTalkBackDevuelveElFocoAlMarcador()
        {
            var m = Colocar("clinica-areas", Punta);
            ConLector();
            Activar(Nodo(m.Anuncio));
            Activar(Nodo("Cerrar ficha"));
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsFalse(Acc.Modal);
            Assert.AreSame(m.Cuerpo, Acc.Enfocado, "El foco vuelve al marcador.");
            Assert.AreSame(Nodo(m.Anuncio), Acc.UltimoNodoEnfocado, "TalkBack recibe el nodo del marcador.");
            Assert.AreSame(Acc.NodoDe(m.Cuerpo), Nodo(m.Anuncio), "El marcador vuelve al recorrido con su nodo.");
            CollectionAssert.DoesNotContain(Etiquetas(), "Clínica Areas");
        }

        [Test]
        public void AtrasCierraLaFichaYDevuelveElFocoAlMarcador()
        {
            var m = Colocar("clinica-areas", Punta);
            ConLector();
            Activar(Nodo(m.Anuncio));
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsFalse(Acc.Modal);
            Assert.AreSame(m.Cuerpo, Acc.Enfocado);
            Assert.AreSame(Nodo(m.Anuncio), Acc.UltimoNodoEnfocado, "TalkBack recibe el nodo del marcador.");
            CollectionAssert.Contains(Etiquetas(), T(ClavesTexto.TemaCambiar), "Vuelven los controles del mapa.");
        }

        [Test]
        public void SiElMarcadorYaNoSeVeNoSeFuerzaElFoco()
        {
            ConLector();
            pantalla.AbrirFicha(LugarId("clinica-areas")); // sin colocar: su marcador no se ve
            Assert.AreSame(pantalla.Ficha.Titulo, Acc.Enfocado);
            pantalla.CerrarFicha();
            Assert.IsNull(Acc.Enfocado);
        }

        [Test]
        public void AbrirOtraFichaSoloDejaLosNodosDeLaNueva()
        {
            Colocar("clinica-areas", Punta);
            var chema = Colocar("chemabarber", Punta + new Vector2(0f, 150f));
            ConLector();
            int controles = Acc.Controles;
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            pantalla.AbrirFicha(LugarId("chemabarber"));
            Assert.IsTrue(Acc.Modal);
            Assert.AreSame(pantalla.Ficha.Titulo, Acc.Enfocado);
            CollectionAssert.AreEqual(RecorridoDeLaFicha(), Etiquetas(), "Solo los nodos de ChemaBarber.");
            CollectionAssert.DoesNotContain(Etiquetas(), "Clínica Areas");
            Assert.AreEqual(controles + RecorridoDeLaFicha().Count, Acc.Controles, "Sin controles de la ficha anterior.");

            pantalla.CerrarFicha();
            Assert.AreSame(chema.Cuerpo, Acc.Enfocado, "El foco va al marcador de la nueva.");
            Assert.AreEqual(controles, Acc.Controles, "Al cerrar no queda ningún control de la ficha.");
        }

        [Test]
        public void SinLectorAbrirYCerrarFuncionanSinJerarquia()
        {
            AccesibilidadPantalla.LectorForzado = () => false;
            var m = Colocar("clinica-areas", Punta);
            pantalla.AbrirFicha(LugarId("clinica-areas"));
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsTrue(Acc.Modal);
            Assert.AreSame(pantalla.Ficha.Titulo, Acc.Enfocado);
            CollectionAssert.AreEqual(RecorridoDeLaFicha(), Acc.Recorrido());
            Assert.IsNull(AssistiveSupport.activeHierarchy, "No se activa ninguna jerarquía.");

            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsFalse(Acc.Modal);
            Assert.AreSame(m.Cuerpo, Acc.Enfocado);
            Assert.IsNull(AssistiveSupport.activeHierarchy);
        }
    }
}
