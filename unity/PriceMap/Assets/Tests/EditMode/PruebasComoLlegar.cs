using System;
using System.Collections.Generic;
using System.Linq;
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
    /// «Cómo llegar» (parte 5b): elegir entre varias rutas e Iniciar, en la pantalla real con servicios falsos, una prueba
    /// por fila de la matriz; y las reglas puras (parte propia, toque en una línea), las líneas del mapa y la hoja en un panel.
    /// </summary>
    public class PruebasComoLlegar
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
        static readonly DateTime HoraLocal = new DateTime(2026, 11, 23, 22, 47, 0, DateTimeKind.Local);
        static readonly Coordenada Origen = new Coordenada(36.7779, -6.3528);
        const float Escala = 0.5f; // dp por px: 24 dp = 48 px

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        /// <summary>Coordenada a esos metros al norte y al este del origen.</summary>
        static Coordenada M(double norte, double este) => new Coordenada(Origen.Latitud + norte / 111195.0,
            Origen.Longitud + este / (111195.0 * Math.Cos(Origen.Latitud * Math.PI / 180.0)));

        static Ruta R(double metros, double segundos, bool aproximada, params (double n, double e)[] puntos) =>
            new Ruta(puntos.Select(p => M(p.n, p.e)).ToArray(), metros, segundos, new[]
            {
                new Maniobra(TipoManiobra.Salida, "Sal hacia el norte por Calle Ancha.", M(puntos[0].n, puntos[0].e), 300),
                new Maniobra(TipoManiobra.Derecha, "Gira a la derecha por Calle Santo Domingo.", M(puntos[puntos.Length / 2].n, puntos[puntos.Length / 2].e), 300),
                new Maniobra(TipoManiobra.Llegada, "Has llegado a tu destino.", M(puntos.Last().n, puntos.Last().e), 0),
            }, aproximada);

        // Comparten los primeros 100 m al norte y el final; cada una tiene su parte propia
        static Ruta Rapida => R(2800, 420, false, (0, 0), (100, 0), (100, 300), (400, 300));
        static Ruta Corta => R(2400, 425, false, (0, 0), (100, 0), (400, 0), (400, 300));
        static Ruta Larga => R(3500, 540, false, (0, 0), (100, 0), (100, -300), (700, -300), (700, 300), (400, 300));
        static Ruta DeAPie => R(2100, 2040, false, (0, 0), (100, 0), (400, 0), (400, 300));

        // ───────────────────────── Falsos ─────────────────────────

        class Peticion
        {
            public ModoViaje Modo;
            public Coordenada Origen, Destino;
            public CancellationToken Ct;
            public readonly TaskCompletionSource<Resultado<IReadOnlyList<Ruta>>> Tcs = new TaskCompletionSource<Resultado<IReadOnlyList<Ruta>>>();
            public void Responder(params Ruta[] rutas) => Tcs.TrySetResult(Resultado<IReadOnlyList<Ruta>>.Exito(rutas));
            public void Fallar() => Tcs.TrySetResult(Resultado<IReadOnlyList<Ruta>>.Fallo("sin conexión"));
        }

        /// <summary>Los tiempos de la ficha contestan al momento (7 y 34 min); las alternativas, cuando la prueba responde.</summary>
        class RutasFalsas : IRutas
        {
            public bool SinTiempos;
            /// <summary>Los tiempos, cuando la prueba responde (null: al momento).</summary>
            public Dictionary<ModoViaje, TaskCompletionSource<Resultado<Ruta>>> Tiempos;
            public readonly List<Peticion> Peticiones = new List<Peticion>();

            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct) =>
                Tiempos != null ? (Tiempos[modo] = new TaskCompletionSource<Resultado<Ruta>>()).Task :
                Task.FromResult(SinTiempos ? Resultado<Ruta>.Fallo("sin conexión") : Resultado<Ruta>.Exito(modo == ModoViaje.Coche ? Rapida : DeAPie));

            public Task<Resultado<IReadOnlyList<Ruta>>> Alternativas(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                var p = new Peticion { Modo = modo, Origen = origen, Destino = destino, Ct = ct };
                Peticiones.Add(p);
                return p.Tcs.Task; // sin atender a la cancelación: sirve para las respuestas tardías
            }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class VozFalsa : IVoz
        {
            public readonly List<string> Dicho = new List<string>();
            public void Hablar(string texto) => Dicho.Add(texto);
            public void Parar() { }
        }

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.Activa;
            public Coordenada? Posicion { get; set; }
            public float? Rumbo => null;
            public event Action Cambiado;
            public void PedirPermiso() { }
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
            public bool RutaActiva { get; private set; }
            public bool RutaSoloPrimerPlano => false;
            public int Terminadas;
            public Resultado<bool> IniciarRuta(string texto) { RutaActiva = true; return Resultado<bool>.Exito(true); }
            public void ActualizarNotificacion(string texto) { }
            public void TerminarRuta() { RutaActiva = false; Terminadas++; }
            public void Mover(Coordenada c) { Posicion = c; Cambiado?.Invoke(); }
        }

        /// <summary>Mapa plano: 1 px por metro, el origen en (500, 1000).</summary>
        class MapaFalso : IMapa
        {
            public void Centrar(Coordenada destino) { }
            public int centrados;
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) => centrados++;
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada c)
            {
                double norte = (c.Latitud - Origen.Latitud) * 111195.0;
                double este = (c.Longitud - Origen.Longitud) * 111195.0 * Math.Cos(Origen.Latitud * Math.PI / 180.0);
                return new PuntoPantalla((float)(500 + este), (float)(1000 - norte), 300f);
            }
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => Origen;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque;
            public event Action<PuntoPantalla> PulsacionLarga;
            public Coordenada? Desproyectar(PuntoPantalla punto) => Origen;
            public IReadOnlyList<Coordenada> linea;
            public bool aproximada;
            public IReadOnlyList<IReadOnlyList<Coordenada>> alternativas = Array.Empty<IReadOnlyList<Coordenada>>();
            /// <summary>Lo último recibido tal cual: null = dejar de elegir; vacía = una sola ruta; Nada = no ha llegado nada.</summary>
            public static readonly IReadOnlyList<IReadOnlyList<Coordenada>> Nada = new IReadOnlyList<Coordenada>[0];
            public IReadOnlyList<IReadOnlyList<Coordenada>> ultimas = Nada;
            public readonly List<(IReadOnlyList<Coordenada> puntos, RectPantalla libre)> encuadres = new List<(IReadOnlyList<Coordenada>, RectPantalla)>();
            public int seguidos, dejados;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { linea = puntos; this.aproximada = aproximada; }
            public void QuitarRuta() => linea = null;
            public void MostrarAlternativas(IReadOnlyList<IReadOnlyList<Coordenada>> lineas)
            {
                ultimas = lineas;
                alternativas = lineas ?? Array.Empty<IReadOnlyList<Coordenada>>();
            }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) => encuadres.Add((puntos, libre));
            public void Seguir(Coordenada posicion, float giro, float inclinacion, float distancia, PuntoPantalla enPantalla, bool animado) => seguidos++;
            public void DejarDeSeguir() => dejados++;
            public event Action GestoUsuario;
            public void Gesto() => GestoUsuario?.Invoke();
            public void Tocar(float x, float y) => Toque?.Invoke(new PuntoPantalla(x, y, 1f));
            public void PulsarLargo(float x, float y) => PulsacionLarga?.Invoke(new PuntoPantalla(x, y, 1f));
        }

        // ───────────────────────── Preparación ─────────────────────────

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        static Lugar Areas => Lugares().Lugares.First(l => l.Id == "clinica-areas");
        static Lugar Otro => Lugares().Lugares.First(l => l.Id != "clinica-areas");

        SynchronizationContext contextoAntes;
        RutasFalsas servicio;
        UbicacionFalsa gps;
        ServicioRutas rutas;
        ServicioNavegacion nav;
        VozFalsa voz;
        ServicioTema tema;
        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        VisualElement raiz;
        DateTime reloj;
        readonly List<string> anuncios = new List<string>();

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones corren al responder
            Textos.Forzados = T;
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            anuncios.Clear();
            reloj = Ahora;
            servicio = new RutasFalsas();
        }

        void Anotar(string t) => anuncios.Add(t);

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = null;
            Textos.Forzados = null;
            AccesibilidadPantalla.AnuncioPedido -= Anotar;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        static Task Nunca(TimeSpan t, CancellationToken ct) => new TaskCompletionSource<bool>().Task;

        void Crear()
        {
            gps = new UbicacionFalsa { Posicion = Origen };
            var prefs = new PreferenciasFalsas();
            var ubicacion = new ServicioUbicacion(gps, prefs);
            rutas = new ServicioRutas(servicio, null, Nunca, () => reloj);
            nav = new ServicioNavegacion(ubicacion, rutas, Nunca, () => reloj);
            voz = new VozFalsa();
            var servicioVoz = new ServicioVoz(nav, voz, prefs);
            servicioVoz.Elegir(ModoVoz.Todo);
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            raiz = arbol.Q("raiz");
            tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, ubicacion, rutas: rutas, navegacion: nav, voz: servicioVoz);
            pantalla.RelojCamara = () => reloj;
            pantalla.RelojLocal = () => HoraLocal;
            tema.Iniciar();
            filtro.Iniciar();
        }

        HojaComoLlegar Hoja => pantalla.ComoLlegar;
        FilaAcciones Fila => pantalla.Ficha.Acciones;
        Peticion Ultima => servicio.Peticiones.Last();

        static bool Visible(VisualElement e) => e.style.display.value == DisplayStyle.Flex;

        /// <summary>Ficha de Clínica Areas e Ir: la hoja, calculando.</summary>
        void Ir(ModoViaje modo = ModoViaje.Coche)
        {
            if (pantalla == null || objeto == null) Crear();
            pantalla.AbrirFicha(Areas);
            Fila.Tocar(modo);
            Fila.PulsarIr();
        }

        /// <summary>La hoja con las tres rutas en coche.</summary>
        void IrConTres()
        {
            Ir();
            Ultima.Responder(Rapida, Corta, Larga);
        }

        static void Igual(Ruta esperada, IReadOnlyList<Coordenada> linea) => CollectionAssert.AreEqual(esperada.Puntos, linea);

        // ───────────────────────── Ir ─────────────────────────

        [Test]
        public void Ir_CalculandoYLuegoLasTarjetasConTodasEncuadradas()
        {
            Ir();
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.IsTrue(Visible(Hoja));
            Assert.AreEqual("Cómo llegar", Hoja.Titulo.text);
            Assert.AreEqual("a Clínica Areas", Hoja.Destino.text);
            Assert.AreEqual("Calculando…", Hoja.Estado.text);
            Assert.IsTrue(Visible(Hoja.Estado));
            Assert.AreEqual(0, Hoja.Rutas);
            Assert.IsTrue(Hoja.Coche.Elegido);
            Assert.AreEqual("7 min", Hoja.Coche.Tiempo.text, "Los de la ficha.");
            Assert.AreEqual("34 min", Hoja.APie.Tiempo.text);
            Assert.IsNull(mapa.linea);
            Assert.IsEmpty(mapa.encuadres);
            Assert.IsFalse(pantalla.RutaActiva, "No empieza.");
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual(Origen, Ultima.Origen);
            Assert.AreEqual(Areas.Posicion, Ultima.Destino);

            Ultima.Responder(Rapida, Corta, Larga);
            Assert.AreEqual(3, Hoja.Rutas);
            Assert.IsFalse(Visible(Hoja.Estado));
            CollectionAssert.AreEqual(new[] { "7 min", "8 min", "9 min" }, Hoja.Tarjetas.Select(t => t.Minutos.text));
            CollectionAssert.AreEqual(new[]
            {
                "Llegada 22:54 · 2,8 km · Más rápida", "Llegada 22:54 · 2,4 km", "Llegada 22:56 · 3,5 km",
            }, Hoja.Tarjetas.Select(t => t.Detalle.text));
            Assert.IsTrue(Hoja.Tarjetas.All(t => t.Visible && t.TextoIniciar.text == "Iniciar"));
            CollectionAssert.AreEqual(new[] { true, false, false }, Hoja.Tarjetas.Select(t => t.Elegida), "La más rápida, elegida.");

            Igual(Rapida, mapa.linea);
            Assert.AreEqual(2, mapa.alternativas.Count);
            Igual(Corta, mapa.alternativas[0]);
            Igual(Larga, mapa.alternativas[1]);
            var (puntos, libre) = mapa.encuadres.Last();
            foreach (var r in new[] { Rapida, Corta, Larga }) CollectionAssert.IsSubsetOf(r.Puntos, puntos.ToArray());
            Assert.IsTrue(puntos.Contains(Areas.Posicion), "Y el destino, aunque esté apartado de la calle.");
            Assert.IsTrue(puntos.Contains(gps.Posicion.Value), "Y el usuario.");
            Assert.IsTrue(libre.Valido);
            Assert.Less(libre.Abajo, Screen.height * (1f - HojaComoLlegar.AltoMaximo) + 1f, "Encima de la hoja.");
            CollectionAssert.AreEqual(new[] { "7 min", "8 min", "9 min" }, pantalla.MinutosRuta.Etiquetas.Select(e => e.Texto.text));
            Assert.IsFalse(nav.Activa);
        }

        [Test]
        public void Ir_TapaLaCapsulaLaPildoraLosBotonesYLaFicha()
        {
            IrConTres();
            Assert.IsFalse(pantalla.Buscador.Visible);
            Assert.IsTrue(raiz.ClassListContains(PantallaMapa.ClaseEligiendo));
            Assert.IsFalse(Visible(pantalla.Ficha), "La ficha sigue abierta, tapada.");
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.TapaPunto(new Vector2(10, 10)), "El mapa se puede mover y tocar.");
            Fila.PulsarIr();
            Assert.AreEqual(1, servicio.Peticiones.Count, "Con la hoja abierta, Ir no hace nada.");
        }

        [Test]
        public void Ir_ElDestinoSeVeAunqueElFiltroNoLoIncluya()
        {
            Crear(); // filtro "Precios" por defecto: Ninguno
            pantalla.AbrirFicha(Areas);
            var marcador = pantalla.Capa.Marcadores.First(m => m.Lugar == Areas);
            pantalla.Capa.Actualizar(Escala, 4000f, 4000f);
            Assert.IsFalse(marcador.Visible);
            Fila.PulsarIr();
            pantalla.Capa.Actualizar(Escala, 4000f, 4000f);
            Assert.IsTrue(marcador.Visible);
            Assert.AreEqual(1, pantalla.Capa.Marcadores.Count(m => m.Visible), "Solo el destino.");
            pantalla.CerrarComoLlegar();
            pantalla.Capa.Actualizar(Escala, 4000f, 4000f);
            Assert.IsFalse(marcador.Visible);
        }

        [Test]
        public void Ir_LosMinutosDeLaFichaQueLleganDespues()
        {
            servicio.SinTiempos = true;
            Ir();
            Assert.AreEqual("…", Hoja.Coche.Tiempo.text, "Se está calculando.");
            Assert.AreEqual("—", Hoja.APie.Tiempo.text);
            Ultima.Responder(Larga);
            Assert.AreEqual("9 min", Hoja.Coche.Tiempo.text, "Los de la primera.");
        }

        void ConTiemposAMano() => servicio.Tiempos = new Dictionary<ModoViaje, TaskCompletionSource<Resultado<Ruta>>>();

        [Test]
        public void Ir_ElTiempoQueLlegaConLaHojaAbierta_EnSuModo()
        {
            ConTiemposAMano();
            Ir();
            Assert.AreEqual("…", Hoja.Coche.Tiempo.text);
            Assert.AreEqual("…", Hoja.APie.Tiempo.text);
            servicio.Tiempos[ModoViaje.APie].SetResult(Resultado<Ruta>.Exito(DeAPie));
            Assert.AreEqual("34 min", Hoja.APie.Tiempo.text);
            Ultima.Responder(Larga);
            Assert.AreEqual("9 min", Hoja.Coche.Tiempo.text);
            servicio.Tiempos[ModoViaje.Coche].SetResult(Resultado<Ruta>.Exito(Rapida));
            Assert.AreEqual("9 min", Hoja.Coche.Tiempo.text, "Los de la primera tarjeta, no los de la ficha.");
        }

        [Test]
        public void Ir_ElTiempoQueFallaTarde_NoQuitaLosMinutosYaVistos()
        {
            ConTiemposAMano();
            Ir();
            var coche = servicio.Tiempos[ModoViaje.Coche];
            Ultima.Responder(Larga);
            Hoja.APie.Pulsar();
            coche.SetResult(Resultado<Ruta>.Fallo("sin conexión"));
            Assert.AreEqual("9 min", Hoja.Coche.Tiempo.text);
        }

        [Test]
        public void CambiarDeModo_AMedias_NoSeQuedaCalculandoYSeAnunciaCalculando()
        {
            servicio.SinTiempos = true;
            Ir();
            Assert.AreEqual("…", Hoja.Coche.Tiempo.text);
            Hoja.APie.Pulsar();
            Assert.AreEqual("—", Hoja.Coche.Tiempo.text, "Su petición se ha cancelado: lo que dice la ficha.");
            Assert.AreEqual("…", Hoja.APie.Tiempo.text);
            Assert.AreEqual(Textos.Formatear(T(ClavesTexto.FichaModoElegido),
                Textos.Formatear(T(ClavesTexto.FichaAnuncioCalculando), "A pie")), anuncios.Last());
        }

        [Test]
        public void CambiarDeModo_SinPosicion_FalloSinPedir()
        {
            IrConTres();
            int peticiones = servicio.Peticiones.Count;
            gps.Posicion = null;
            Hoja.APie.Pulsar();
            Assert.AreEqual("No se ha podido calcular la ruta", Hoja.Estado.text);
            Assert.AreEqual(0, Hoja.Rutas);
            Assert.AreEqual("—", Hoja.APie.Tiempo.text);
            Assert.IsNull(mapa.linea);
            Assert.AreEqual(peticiones, servicio.Peticiones.Count);
        }

        [Test]
        public void Encuadre_SeRepiteAlMedirLaHojaSalvoSiElUsuarioHaMovidoElMapa()
        {
            var medir = typeof(PantallaMapa).GetMethod("AlMedirComoLlegar",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            IrConTres();
            int n = mapa.encuadres.Count;
            medir.Invoke(pantalla, null);
            Assert.AreEqual(n + 1, mapa.encuadres.Count);
            mapa.Gesto();
            medir.Invoke(pantalla, null);
            Assert.AreEqual(n + 1, mapa.encuadres.Count, "El mapa es del usuario.");
            Hoja.APie.Pulsar();
            Ultima.Responder(DeAPie);
            Assert.AreEqual(n + 2, mapa.encuadres.Count);
            medir.Invoke(pantalla, null);
            Assert.AreEqual(n + 3, mapa.encuadres.Count, "Con las rutas nuevas, otra vez.");
        }

        [Test]
        public void Ir_ConLaCamaraAunDeCamino_LaNavegacionEmpiezaDebajo()
        {
            IrConTres();
            pantalla.IniciarRuta(0);
            Assert.IsTrue(pantalla.AcercandoCamara);
            pantalla.AbrirFicha(Otro);
            Fila.PulsarIr();
            Ultima.Responder(Larga, Corta);
            pantalla.LlegaLaCamara();
            Assert.IsTrue(nav.Activa);
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.IsFalse(pantalla.TarjetasRutaVisibles);
            Igual(Larga, mapa.linea);
            Hoja.BotonCerrar.Pulsar();
            Assert.AreEqual(Rapida.Puntos.Last(), mapa.linea.Last(), "Vuelve la suya.");
            pantalla.CerrarFicha();
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
        }

        // ───────────────────────── Elegir ─────────────────────────

        [Test]
        public void Elegir_ConLaTarjeta_ResaltadaEnLosDosSitiosSinEmpezar()
        {
            IrConTres();
            int encuadres = mapa.encuadres.Count;
            Hoja.Tarjetas[1].Tocar();
            CollectionAssert.AreEqual(new[] { false, true, false }, Hoja.Tarjetas.Select(t => t.Elegida));
            Igual(Corta, mapa.linea);
            Igual(Rapida, mapa.alternativas[0]);
            Igual(Larga, mapa.alternativas[1]);
            CollectionAssert.AreEqual(new[] { false, true, false },
                pantalla.MinutosRuta.Etiquetas.Select(e => e.ClassListContains(CapaMinutosRuta.ClaseElegida)));
            Assert.AreSame(pantalla.MinutosRuta.Etiquetas[1], pantalla.MinutosRuta.Etiquetas[1].parent.Children().Last(), "Encima.");
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.IsFalse(nav.Activa);
            Assert.AreEqual(encuadres, mapa.encuadres.Count, "El mapa no se mueve.");
            Assert.AreEqual("8 minutos, llegada 22:54, 2,4 km, seleccionada", anuncios.Last());
        }

        [Test]
        public void Elegir_ConSuLinea_A24dp()
        {
            IrConTres();
            // La larga va por x = 200 (300 m al oeste): 40 px = 20 dp a su izquierda
            Assert.IsFalse(pantalla.TocarMapa(new PuntoPantalla(160f, 600f, 1f), Escala));
            Assert.AreEqual(2, Hoja.Elegida);
            Igual(Larga, mapa.linea);
            Assert.IsFalse(nav.Activa);
            // A 60 px = 30 dp de todas: nada
            pantalla.TocarMapa(new PuntoPantalla(440f, 700f, 1f), Escala);
            Assert.AreEqual(2, Hoja.Elegida);
        }

        [Test]
        public void Elegir_EnElTramoComun_SigueLaElegida()
        {
            IrConTres();
            Hoja.Tarjetas[1].Tocar();
            pantalla.TocarMapa(new PuntoPantalla(501f, 950f, 1f), Escala); // los 100 m de las tres
            Assert.AreEqual(1, Hoja.Elegida);
        }

        [Test]
        public void ToqueFueraDeLasLineas_NiCierraNiAbreFichas()
        {
            IrConTres();
            pantalla.ActualizarCapas(Escala); // marcadores colocados
            foreach (var m in pantalla.Capa.Marcadores.Where(m => m.Visible).ToList())
                pantalla.TocarMapa(new PuntoPantalla(m.Posicion.x / Escala, m.Posicion.y / Escala, 1f), Escala);
            mapa.Tocar(5f, 5f);
            mapa.PulsarLargo(5f, 5f);
            Assert.IsFalse(pantalla.PulsarLargoMapa(new PuntoPantalla(5f, 5f, 1f)));
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.AreSame(Areas, pantalla.Ficha.Lugar);
        }

        // ───────────────────────── Iniciar ─────────────────────────

        [Test]
        public void Iniciar_LaSegunda_PrimeroLaCamaraYLuegoTarjetasYVoz()
        {
            IrConTres();
            int seguidos = mapa.seguidos, encuadres = mapa.encuadres.Count;
            Hoja.Tarjetas[1].PulsarIniciar();
            Assert.IsFalse(pantalla.EligiendoRuta);
            Assert.IsFalse(pantalla.FichaAbierta);
            Igual(Corta, mapa.linea);
            Assert.IsNull(mapa.ultimas, "Se deja de elegir: la línea vuelve debajo de los edificios.");
            Assert.IsEmpty(pantalla.MinutosRuta.Etiquetas);
            Assert.IsFalse(raiz.ClassListContains(PantallaMapa.ClaseEligiendo));
            Assert.Greater(mapa.seguidos, seguidos, "La cámara va al cursor.");
            Assert.AreEqual(encuadres, mapa.encuadres.Count);
            Assert.IsTrue(pantalla.AcercandoCamara);
            Assert.IsFalse(Visible(pantalla.TarjetaManiobra));
            Assert.IsFalse(Visible(pantalla.TarjetaRuta));
            Assert.IsFalse(pantalla.BotonVozVisible);
            Assert.IsFalse(pantalla.Buscador.Visible, "Ni la cápsula.");
            Assert.IsEmpty(voz.Dicho);
            Assert.IsFalse(gps.RutaActiva);

            reloj = reloj.AddSeconds(0.9);
            pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(pantalla.Navegando);
            CollectionAssert.AreEqual(Corta.Puntos, nav.Ruta.Puntos);
            Assert.AreEqual(Areas.Posicion, nav.PuntoDestino);
            Assert.AreEqual(ModoViaje.Coche, nav.Modo);
            Assert.IsTrue(Visible(pantalla.TarjetaManiobra));
            Assert.IsTrue(Visible(pantalla.TarjetaRuta));
            Assert.AreEqual("8", pantalla.TarjetaRuta.Minutos.text);
            Assert.IsNotEmpty(voz.Dicho, "Y la voz.");
            Assert.AreSame(pantalla.TarjetaManiobra, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void Iniciar_SinPosicionYa_AlMomento()
        {
            IrConTres();
            gps.Posicion = null;
            Hoja.Tarjetas[0].PulsarIniciar();
            Assert.IsTrue(pantalla.Navegando, "No hay cursor al que ir.");
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
        }

        [Test]
        public void UnaRuta_UnaTarjetaSinMasRapida()
        {
            Ir();
            Ultima.Responder(Rapida);
            Assert.AreEqual(1, Hoja.Rutas);
            Assert.AreEqual("Llegada 22:54 · 2,8 km", Hoja.Tarjetas[0].Detalle.text);
            Assert.AreEqual("7 minutos, llegada 22:54, 2,8 km", Hoja.Tarjetas[0].Anuncio);
            Assert.IsFalse(Hoja.Tarjetas[1].Visible);
            Assert.IsFalse(Hoja.Tarjetas[2].Visible);
            Assert.IsNotNull(mapa.ultimas, "Se está eligiendo: encima de los edificios.");
            Assert.AreNotSame(MapaFalso.Nada, mapa.ultimas);
            Assert.IsEmpty(mapa.ultimas);
        }

        [Test]
        public void UnMinuto_EnSingular()
        {
            Ir();
            Ultima.Responder(R(90, 40, false, (0, 0), (90, 0)));
            Assert.AreEqual("1 min", Hoja.Tarjetas[0].Minutos.text);
            Assert.AreEqual("1 minuto, llegada 22:47, 90 m", Hoja.Tarjetas[0].Anuncio);
        }

        // ───────────────────────── Modo, sin red, fallo, cerrar, tardías ─────────────────────────

        [Test]
        public void CambiarDeModo_FueraLasDeCocheYLasDeAPie()
        {
            IrConTres();
            Hoja.APie.Pulsar();
            Assert.IsTrue(Hoja.APie.Elegido);
            Assert.AreEqual("Calculando…", Hoja.Estado.text);
            Assert.AreEqual(0, Hoja.Rutas);
            Assert.IsNull(mapa.linea);
            Assert.IsEmpty(mapa.alternativas);
            Assert.IsEmpty(pantalla.MinutosRuta.Etiquetas);
            Assert.AreEqual(ModoViaje.APie, Ultima.Modo);
            Ultima.Responder(DeAPie);
            Assert.AreEqual("34 min", Hoja.Tarjetas[0].Minutos.text);
            Igual(DeAPie, mapa.linea);

            Hoja.Coche.Pulsar(); // las de coche, de la caché
            Assert.AreEqual(3, Hoja.Rutas);
            Igual(Rapida, mapa.linea);
            Assert.AreEqual(2, servicio.Peticiones.Count);

            Hoja.APie.Pulsar();
            pantalla.Iniciar();
            Assert.AreEqual(ModoViaje.APie, nav.Modo);
        }

        [Test]
        public void SinRed_LaAproximadaYIniciarConElToast()
        {
            Ir();
            Ultima.Responder(R(1200, 240, true, (0, 0), (300, 0)));
            Assert.AreEqual("≈ 4 min", Hoja.Tarjetas[0].Minutos.text.Replace("≈ ", "≈ "));
            Assert.IsTrue(mapa.aproximada);
            Assert.IsFalse(pantalla.Toast.Visible);
            pantalla.Iniciar();
            Assert.IsTrue(pantalla.Toast.Visible);
            Assert.AreEqual("Sin conexión: ruta aproximada", pantalla.Toast.Texto.text);
        }

        [Test]
        public void Fallo_TextoSinTarjetasNiLineasYLaX()
        {
            Ir();
            Ultima.Fallar();
            Assert.AreEqual("No se ha podido calcular la ruta", Hoja.Estado.text);
            Assert.IsFalse(Visible(Hoja.DetalleEstado));
            Assert.AreEqual(0, Hoja.Rutas);
            Assert.AreEqual("—", Hoja.Coche.Tiempo.text);
            Assert.IsNull(mapa.linea);
            Assert.Contains("No se ha podido calcular la ruta", anuncios);
            pantalla.TocarMapa(new PuntoPantalla(500f, 950f, 1f), Escala);
            Assert.IsTrue(pantalla.EligiendoRuta);
            Hoja.BotonCerrar.Pulsar();
            Assert.IsFalse(pantalla.EligiendoRuta);
            Assert.IsTrue(pantalla.FichaAbierta);
        }

        [Test]
        public void Fallo_RutasSinPuntos()
        {
            Ir();
            Ultima.Responder(R(0, 60, false, (0, 0)));
            Assert.AreEqual("No se ha podido calcular la ruta", Hoja.Estado.text);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Cerrar_SinLineasYVuelveLaFichaConSuModo(bool conAtras)
        {
            IrConTres();
            Hoja.APie.Pulsar();
            Ultima.Responder(DeAPie);
            int centrados = mapa.centrados;
            if (conAtras) Assert.IsTrue(pantalla.Atras()); else Hoja.BotonCerrar.Pulsar();
            Assert.IsFalse(pantalla.EligiendoRuta);
            Assert.IsFalse(Visible(Hoja));
            Assert.IsNull(mapa.linea);
            Assert.IsNull(mapa.ultimas);
            Assert.IsEmpty(pantalla.MinutosRuta.Etiquetas);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsTrue(Visible(pantalla.Ficha));
            Assert.AreSame(Areas, pantalla.Ficha.Lugar);
            Assert.AreEqual(ModoViaje.APie, Fila.Elegido);
            Assert.IsFalse(raiz.ClassListContains(PantallaMapa.ClaseEligiendo));
            Assert.Greater(mapa.centrados, centrados, "El lugar, otra vez en la zona libre.");
            Assert.AreSame(Fila.Ir, pantalla.Accesibilidad.Enfocado);
            Assert.IsTrue(pantalla.Accesibilidad.Modal);

            Assert.IsTrue(pantalla.Atras(), "Y atrás otra vez cierra la ficha.");
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(pantalla.Buscador.Visible);
        }

        [Test]
        public void RespuestaTardia_CerradaUOtroModo_NoCambiaNada()
        {
            Ir();
            var tarde = Ultima;
            pantalla.CerrarComoLlegar();
            Assert.IsTrue(tarde.Ct.IsCancellationRequested);
            tarde.Responder(Rapida);
            Assert.IsNull(mapa.linea);
            Assert.IsFalse(pantalla.EligiendoRuta);
            Assert.IsNull(pantalla.OpcionesRuta);

            Fila.PulsarIr();
            var deCoche = Ultima;
            Hoja.APie.Pulsar();
            deCoche.Responder(Rapida, Corta);
            Assert.AreEqual(0, Hoja.Rutas);
            Assert.AreEqual("Calculando…", Hoja.Estado.text);
            Ultima.Responder(DeAPie);
            Igual(DeAPie, mapa.linea);
        }

        [Test]
        public void ConOtraNavegacion_NoLaTerminaYAlCerrarVuelveSuLinea()
        {
            IrConTres();
            pantalla.Iniciar();
            gps.Mover(M(50, 0));
            var restante = mapa.linea;
            pantalla.AbrirFicha(Otro);
            Fila.PulsarIr();
            Assert.IsTrue(pantalla.Navegando, "Abrir «Cómo llegar» no la termina.");
            Assert.IsNull(mapa.linea);
            Ultima.Responder(Larga, Corta);
            Igual(Larga, mapa.linea);
            Assert.IsFalse(pantalla.TarjetasRutaVisibles);
            gps.Mover(M(60, 0));
            Igual(Larga, mapa.linea); // avanzar no pinta la línea de la navegación encima
            mapa.Gesto();
            Assert.AreEqual(EstadoSeguimiento.Siguiendo, pantalla.EstadoCamara, "Mover el mapa aquí no suelta la cámara.");

            Hoja.BotonCerrar.Pulsar();
            Assert.IsTrue(nav.Activa);
            Assert.AreEqual(0, gps.Terminadas);
            Assert.AreEqual(Rapida.Puntos.Last(), mapa.linea.Last(), "Vuelve su línea, desde donde va.");
            Assert.Less(mapa.linea[0].DistanciaMetros(M(60, 0)), 1.0);
            Assert.AreNotSame(restante, mapa.linea);
            pantalla.CerrarFicha();
            Assert.IsTrue(pantalla.TarjetasRutaVisibles);
            Assert.AreEqual("Clínica Areas", pantalla.TarjetaRuta.DestinoNombre.text);

            pantalla.AbrirFicha(Otro);
            Fila.PulsarIr(); // de la caché
            pantalla.IniciarRuta(1);
            Assert.AreEqual(1, gps.Terminadas, "«Iniciar» termina la de antes.");
            Assert.IsFalse(nav.Activa);
            pantalla.LlegaLaCamara();
            CollectionAssert.AreEqual(Corta.Puntos, nav.Ruta.Puntos);
            Assert.AreEqual(Otro.Nombre, nav.Destino);
        }

        [Test]
        public void Compartir_CierraLaHojaYLaFicha()
        {
            IrConTres();
            pantalla.RecibirCompartido("farmacia de guardia");
            Assert.IsFalse(pantalla.EligiendoRuta);
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsNull(mapa.linea);
            Assert.IsEmpty(mapa.alternativas);
            Assert.IsTrue(pantalla.Buscador.Abierta);
        }

        [Test]
        public void PuntoDelMapa_SuNombreYVuelveConSusDatos()
        {
            Crear();
            pantalla.ElegirResultado(ResultadoBusqueda.DeDireccion(
                new Direccion("Calle Ancha 10", "Sanlúcar de Barrameda", M(400, 300), "Calle Ancha", "10"), 500));
            Fila.PulsarIr();
            Assert.AreEqual("a Calle Ancha, 10", Hoja.Destino.text);
            Assert.AreEqual(M(400, 300), pantalla.PuntoChincheta, "La chincheta del destino sigue.");
            pantalla.CerrarComoLlegar();
            Assert.AreEqual("Calle Ancha, 10", pantalla.Ficha.Titulo.text);
            Assert.AreEqual("Sanlúcar de Barrameda", pantalla.Ficha.Direccion.text);
        }

        [Test]
        public void Tema_LaHojaYLasLineasSiguenConLosColoresNuevos()
        {
            IrConTres();
            Hoja.Tarjetas[1].Tocar();
            tema.Alternar();
            Assert.IsTrue(pantalla.InterfazOscura);
            Assert.IsTrue(pantalla.EligiendoRuta);
            Assert.AreEqual(1, Hoja.Elegida);
            Igual(Corta, mapa.linea);
            Assert.AreEqual(2, mapa.alternativas.Count);
        }

        // ───────────────────────── TalkBack ─────────────────────────

        [Test]
        public void TalkBack_HojaModalTarjetasSeleccionadasEIniciarAparte()
        {
            Crear();
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            Ir();
            var acc = pantalla.Accesibilidad;
            Assert.IsTrue(acc.Modal);
            Assert.AreSame(Hoja.Titulo, acc.Enfocado);
            CollectionAssert.AreEqual(new[] { "Cómo llegar", "a Clínica Areas", "Coche, 7 minutos", "A pie, 34 minutos", "Calculando…", "Cerrar" },
                acc.Recorrido());

            Ultima.Responder(Rapida, Corta);
            Assert.AreEqual("7 minutos, llegada 22:54, 2,8 km, más rápida", anuncios.Last());
            Assert.AreSame(Hoja.Titulo, acc.Enfocado, "Llegar las rutas no mueve el foco.");
            CollectionAssert.AreEqual(new[]
            {
                ("Cómo llegar", false), ("a Clínica Areas", false), ("Coche, 7 minutos", true), ("A pie, 34 minutos", false),
                ("7 minutos, llegada 22:54, 2,8 km, más rápida", true), ("Iniciar, 7 minutos, llegada 22:54, 2,8 km, más rápida", false),
                ("8 minutos, llegada 22:54, 2,4 km", false), ("Iniciar, 8 minutos, llegada 22:54, 2,4 km", false), ("Cerrar", false),
            }, acc.RecorridoConEstado());
            Hoja.Tarjetas[1].Tocar();
            Assert.Contains(("8 minutos, llegada 22:54, 2,4 km", true), acc.RecorridoConEstado());

            pantalla.CerrarComoLlegar();
            Assert.AreSame(Fila.Ir, acc.Enfocado);
            Assert.Contains("Ir a Clínica Areas", acc.Recorrido());
            Assert.IsFalse(acc.Recorrido().Contains("Cómo llegar"));
        }

        [Test]
        public void TalkBack_ConElLectorEncendido_ElFocoVaYVuelveYLasTarjetasSonBotones()
        {
            Crear();
            AccesibilidadPantalla.LectorForzado = () => true;
            IrConTres();
            var acc = pantalla.Accesibilidad;
            Assert.IsNotNull(acc.NodoDe(Hoja.Titulo));
            var nodo = acc.NodoDe(Hoja.Tarjetas[0]);
            Assert.AreEqual(AccessibilityRole.Button, nodo.role);
            Assert.IsTrue((nodo.state & AccessibilityState.Selected) != 0);
            Hoja.Tarjetas[2].Tocar();
            Assert.IsTrue((acc.NodoDe(Hoja.Tarjetas[0]).state & AccessibilityState.Selected) == 0);
            Assert.IsTrue((acc.NodoDe(Hoja.Tarjetas[2]).state & AccessibilityState.Selected) != 0);
            Assert.IsNull(acc.NodoDe(Fila.Ir), "La ficha tapada no está.");
            pantalla.CerrarComoLlegar();
            Assert.AreSame(acc.NodoDe(Fila.Ir), acc.UltimoNodoEnfocado);

            Fila.PulsarIr();
            pantalla.Iniciar();
            Assert.IsNotNull(acc.NodoDe(pantalla.TarjetaManiobra), "Al llegar la cámara entran las tarjetas.");
            Assert.AreSame(acc.NodoDe(pantalla.TarjetaManiobra), acc.UltimoNodoEnfocado);
        }

        [Test]
        public void LosTextosEstanEnLaTabla()
        {
            foreach (var clave in ClavesTexto.Todas.Where(c => c.StartsWith("comollegar.")))
                Assert.IsTrue(PrepararInterfaz.TextosEs.ContainsKey(clave), clave);
            Assert.AreEqual(13, ClavesTexto.Todas.Count(c => c.StartsWith("comollegar.")));
        }

        // ───────────────────────── Reglas puras ─────────────────────────

        [Test]
        public void ParteRuta_ElMedioDeLoQueNoComparte()
        {
            var tres = new[] { Rapida, Corta, Larga };
            // Rápida: propia de (100, 20) a (400, 280): su medio, cerca de la esquina (100, 300)
            var a = ReglaParteRuta.PuntoMedio(tres, 0).Value;
            Assert.Less(a.DistanciaMetros(M(110, 300)), 40);
            var b = ReglaParteRuta.PuntoMedio(tres, 1).Value;
            Assert.Less(b.DistanciaMetros(M(400, 0)), 40);
            var c = ReglaParteRuta.PuntoMedio(tres, 2).Value;
            Assert.Less(c.DistanciaMetros(M(700, -300)), 160);
            foreach (var p in new[] { a, b, c }) Assert.Greater(p.DistanciaMetros(M(50, 0)), 100, "Nunca en el tramo común.");
        }

        [Test]
        public void ParteRuta_SolaOSinPartePropia_ElMedioDeLaLinea()
        {
            var recta = R(400, 60, false, (0, 0), (400, 0));
            Assert.Less(ReglaParteRuta.PuntoMedio(new[] { recta }, 0).Value.DistanciaMetros(M(200, 0)), 15);
            Assert.Less(ReglaParteRuta.PuntoMedio(new[] { recta, recta }, 1).Value.DistanciaMetros(M(200, 0)), 15);
        }

        [Test]
        public void ParteRuta_CasosLimite()
        {
            Assert.IsNull(ReglaParteRuta.PuntoMedio(null, 0));
            Assert.IsNull(ReglaParteRuta.PuntoMedio(new[] { new Ruta(null, 0, 0, null) }, 0));
            Assert.IsNull(ReglaParteRuta.PuntoMedio(new[] { Rapida }, 1));
            Assert.AreEqual(M(0, 0), ReglaParteRuta.PuntoMedio(new[] { R(0, 0, false, (0, 0), (0, 0)) }, 0));
        }

        static PuntoPantalla P(float x, float y, float z = 1f) => new PuntoPantalla(x, y, z);

        [Test]
        public void ToqueRuta_LaMasCercanaElEmpateYElRadio()
        {
            var lineas = new IReadOnlyList<PuntoPantalla>[]
            {
                new[] { P(0, 0), P(100, 0) },
                new[] { P(0, 0), P(50, 0), P(50, 100) },
            };
            Assert.AreEqual(1, ReglaToqueRuta.MasCercana(P(60, 80), lineas, 0, 24, 2), "A 10 px de la segunda.");
            Assert.AreEqual(0, ReglaToqueRuta.MasCercana(P(95, 5), lineas, 1, 24, 2));
            Assert.AreEqual(1, ReglaToqueRuta.MasCercana(P(20, 3), lineas, 1, 24, 2), "Tramo común: sigue la elegida.");
            Assert.AreEqual(0, ReglaToqueRuta.MasCercana(P(20, 3), lineas, 0, 24, 2));
            Assert.AreEqual(1, ReglaToqueRuta.MasCercana(P(74, 90), lineas, 0, 24, 2), "A 24 justos.");
            Assert.AreEqual(-1, ReglaToqueRuta.MasCercana(P(74.5f, 90), lineas, 0, 24, 2));
            Assert.AreEqual(-1, ReglaToqueRuta.MasCercana(P(0, 0), null, 0, 24, 2));
            Assert.AreEqual(-1, ReglaToqueRuta.MasCercana(P(float.NaN, 0), lineas, 0, 24, 2));
        }

        [Test]
        public void ToqueRuta_LosTramosDetrasDeLaCamaraNoCuentan()
        {
            var lineas = new IReadOnlyList<PuntoPantalla>[] { new[] { P(0, 0), P(100, 0, -1f), P(200, 0), P(300, 0) } };
            Assert.AreEqual(-1, ReglaToqueRuta.MasCercana(P(50, 0), lineas, 0, 24, 2));
            Assert.AreEqual(0, ReglaToqueRuta.MasCercana(P(250, 0), lineas, 0, 24, 2));
        }

        [Test]
        public void LosParametrosEstanEnParametrosNegocio()
        {
            Assert.AreEqual(24f, ParametrosNegocio.RutaToqueDp);
            Assert.Less(ParametrosNegocio.RutaAlternativaAncho, 1f, "Más finas.");
        }

        // ───────────────────────── Etiquetas y mapa real ─────────────────────────

        [Test]
        public void Etiquetas_EnElPuntoPropioYFueraConLaHoja()
        {
            IrConTres();
            pantalla.ActualizarCapas(Escala);
            var tres = new[] { Rapida, Corta, Larga };
            for (int i = 0; i < 3; i++)
            {
                var e = pantalla.MinutosRuta.Etiquetas[i];
                Assert.IsTrue(e.Visible);
                var p = mapa.Proyectar(ReglaParteRuta.PuntoMedio(tres, i).Value);
                Assert.AreEqual(p.X * Escala, e.Posicion.x, 0.1f);
                Assert.AreEqual(p.Y * Escala, e.Posicion.y, 0.1f);
            }
            pantalla.CerrarComoLlegar();
            Assert.IsEmpty(raiz.Q("capa-minutos-ruta").Children());
        }

        [Test]
        public void MapaVectorial_AlternativasDebajoMasFinasYConElTema()
        {
            double latAntes = Geo.LatitudOrigen, lonAntes = Geo.LongitudOrigen;
            Geo.LatitudOrigen = Origen.Latitud; Geo.LongitudOrigen = Origen.Longitud;
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = objCamara.AddComponent<CamaraMapa>();
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                var real = objMapa.AddComponent<MapaVectorial>();
                real.sombras = false; // Update no toca QualitySettings
                camara.distancia = 300f;
                IMapa puerto = real;
                puerto.MostrarRuta(Rapida.Puntos);
                puerto.MostrarAlternativas(new[] { Corta.Puntos, Larga.Puntos });
                Assert.IsNotNull(real.ObjetoAlternativas);
                Assert.AreSame(objMapa.transform, real.ObjetoAlternativas.transform.parent);
                Assert.AreEqual(2, real.ObjetoAlternativas.transform.childCount, "Borde y relleno.");
                Assert.Less(real.MaterialAlternativa.renderQueue, real.MaterialBordeRuta.renderQueue, "Debajo de la elegida.");
                Assert.Greater(real.MaterialBordeAlternativa.renderQueue, 2000, "Eligiendo, encima de los edificios.");
                var claro = PaletaMapa.De(TemaMapa.Claro);
                Assert.AreEqual(claro.rutaAlternativa, real.MaterialAlternativa.color);
                // Más finas: la línea de (0, 0) a (100, 0) va de −ancho/2 a +ancho/2 en este
                float ancho = 2f * real.ObjetoAlternativas.GetComponentsInChildren<MeshFilter>()[0].sharedMesh.vertices
                    .Where(v => v.z > 20f && v.z < 80f || v.z <= 0f).Select(v => Mathf.Abs(v.x)).Where(x => x < 20f).DefaultIfEmpty(0f).Max();
                Assert.AreEqual(9f * ParametrosNegocio.RutaAlternativaAncho, ancho, 0.1f);

                puerto.AplicarTema(Tema.Oscuro);
                Fotograma(real);
                var oscuro = PaletaMapa.De(TemaMapa.Oscuro);
                Assert.AreEqual(oscuro.rutaAlternativa, real.MaterialAlternativa.color, "Con el tema, sus colores.");
                Assert.AreEqual(oscuro.rutaAlternativaBorde, real.MaterialBordeAlternativa.color);

                var antes = real.ObjetoAlternativas;
                camara.distancia = 4000f;
                Fotograma(real);
                Assert.AreNotSame(antes, real.ObjetoAlternativas, "Al alejar, se rehacen más anchas.");
                puerto.QuitarRuta();
                Assert.IsNotNull(real.ObjetoAlternativas);
                puerto.MostrarAlternativas(new IReadOnlyList<Coordenada>[0]);
                Assert.IsNull(real.ObjetoAlternativas);
                Assert.Greater(real.MaterialRuta.renderQueue, 2000, "Con una sola ruta, también encima.");
                puerto.MostrarAlternativas(null);
                Assert.AreEqual(1960, real.MaterialBordeRuta.renderQueue, "Al dejar de elegir, debajo de los edificios.");
                Assert.AreEqual(1970, real.MaterialRuta.renderQueue);
                Assert.Greater(real.MaterialBordeAlternativa.renderQueue, 1950, "Y siempre encima de las calles.");

                // La primera ruta de la sesión: las alternativas llegan antes de que haya materiales
                var nuevo = new GameObject("Mapa nuevo") { transform = { parent = objMapa.transform } }.AddComponent<MapaVectorial>();
                nuevo.sombras = false;
                ((IMapa)nuevo).MostrarAlternativas(new IReadOnlyList<Coordenada>[0]);
                ((IMapa)nuevo).MostrarRuta(Rapida.Puntos);
                Assert.Greater(nuevo.MaterialBordeRuta.renderQueue, 2000);
                Assert.Greater(nuevo.MaterialRuta.renderQueue, 2000);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                UnityEngine.Object.DestroyImmediate(objCamara);
                Geo.LatitudOrigen = latAntes; Geo.LongitudOrigen = lonAntes;
            }
        }

        static void Fotograma(MapaVectorial m) =>
            typeof(MapaVectorial).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(m, null);

        // ───────────────────────── La hoja en un panel ─────────────────────────

        static void Disponer(VisualElement e)
        {
            var validar = e.panel.GetType().GetMethod("ValidateLayout",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            validar.Invoke(e.panel, null);
        }

        [TestCase(1f)]
        [TestCase(2f)]
        public void HojaEnUnPanel_ZonasDe48dpYConElTextoGrandeLaListaSeDesplaza(float fuente)
        {
            var objetoPanel = new GameObject("Hoja en un panel");
            try
            {
                objetoPanel.SetActive(false);
                var documento = objetoPanel.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                objetoPanel.SetActive(true);
                var raizPanel = documento.rootVisualElement.Q("raiz");
                var marco = new VisualElement();
                marco.style.position = Position.Absolute;
                marco.style.width = 412f;
                marco.style.height = 800f;
                raizPanel.Add(marco);
                var hoja = new HojaComoLlegar(T);
                marco.Add(hoja);
                hoja.Abrir("un destino con un nombre bastante largo que ocupa dos líneas", ModoViaje.Coche);
                hoja.PonerTiempo(ModoViaje.Coche, EstadoTiempo.Minutos, 7);
                hoja.PonerTiempo(ModoViaje.APie, EstadoTiempo.Minutos, 34);
                hoja.MostrarRutas(new[] { Rapida, Corta, Larga }, 0, HoraLocal);
                Disponer(hoja);
                EscalaFuente.Aplicar(hoja, fuente);
                Disponer(hoja);
                Disponer(hoja);
                Assert.AreEqual(marco.worldBound.yMax, hoja.worldBound.yMax, 2f, "Anclada abajo.");
                Assert.LessOrEqual(hoja.worldBound.height, 800f * HojaComoLlegar.AltoMaximo + 1f);
                foreach (var t in hoja.Tarjetas)
                {
                    // El panel de las pruebas redondea a su píxel (96 dpi, 1,67 dp): 48 dp menos uno
                    Assert.GreaterOrEqual(t.Iniciar.worldBound.height, 46f);
                    Assert.GreaterOrEqual(t.Iniciar.worldBound.width, 46f);
                    Assert.LessOrEqual(t.Iniciar.worldBound.xMax, t.worldBound.xMax, "«Iniciar» cabe en la tarjeta.");
                    Assert.GreaterOrEqual(t.Iniciar.worldBound.xMin, t.Detalle.worldBound.xMax, "Y no pisa el detalle.");
                    Assert.LessOrEqual(t.Minutos.worldBound.yMax, t.Detalle.worldBound.yMin + 1f);
                    Assert.GreaterOrEqual(t.worldBound.yMax, t.Detalle.worldBound.yMax, "El texto cabe en la tarjeta.");
                }
                foreach (var m in new[] { hoja.Coche, hoja.APie }) Assert.GreaterOrEqual(m.worldBound.height, 46f);
                Assert.GreaterOrEqual(hoja.BotonCerrar.worldBound.width, 46f);
                float contenido = hoja.Lista.contentContainer.layout.height, vista = hoja.Lista.contentViewport.layout.height;
                if (fuente > 1f)
                {
                    Assert.Greater(contenido, vista + 10f, "No cabe: se desplaza.");
                    hoja.DesplazarHasta(2);
                    Assert.Greater(hoja.Lista.scrollOffset.y, 0f);
                }
                else Assert.LessOrEqual(contenido, vista + 1f, "Cabe entera.");
            }
            finally { UnityEngine.Object.DestroyImmediate(objetoPanel); }
        }

        [Test]
        public void EtiquetasEnUnPanel_ElToqueEnLosMinutosEligeLaSuyaYLaElegidaGana()
        {
            var objetoPanel = new GameObject("Etiquetas en un panel");
            try
            {
                objetoPanel.SetActive(false);
                var documento = objetoPanel.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                objetoPanel.SetActive(true);
                var contenedor = documento.rootVisualElement.Q("capa-minutos-ruta");
                var capa = new CapaMinutosRuta(contenedor, new MapaFalso());
                capa.Mostrar(new[] { Rapida, Corta, Corta }, 0, r => r.Minutos + " min"); // las dos cortas, en el mismo sitio
                capa.Actualizar(Escala);
                Disponer(contenedor);
                Disponer(contenedor);
                var primera = capa.Etiquetas[0].Texto.worldBound;
                Assert.Greater(primera.width, 0f);
                Assert.AreEqual(0, capa.EtiquetaEn(primera.center));
                Assert.AreEqual(-1, capa.EtiquetaEn(new Vector2(primera.center.x, primera.yMax + 3f)));
                Assert.AreEqual(-1, capa.EtiquetaEn(new Vector2(primera.xMax + 3f, primera.center.y)));
                var repetida = capa.Etiquetas[1].Texto.worldBound.center;
                Assert.AreEqual(1, capa.EtiquetaEn(repetida));
                capa.Elegir(2);
                Assert.AreEqual(2, capa.EtiquetaEn(repetida), "Pisadas, la elegida.");
            }
            finally { UnityEngine.Object.DestroyImmediate(objetoPanel); }
        }
    }
}
