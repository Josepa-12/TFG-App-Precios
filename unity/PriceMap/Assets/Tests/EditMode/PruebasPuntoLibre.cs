using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Busqueda;
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
    /// Punto libre (historia 2.5, parte 1): la pulsación larga (DetectorToque), el puerto (MapaVectorial:
    /// PulsacionLarga y Desproyectar), la geocodificación inversa de Photon (URL y lectura) y la ficha del punto en la
    /// pantalla real, una prueba por fila de la matriz. Nada sale a la red: el geocodificador es falso.
    /// </summary>
    public class PruebasPuntoLibre
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        const float Escala = 0.5f;           // px de pantalla → dp
        const float AnchoPx = 1080f, AltoPx = 1920f;
        const float Dpi = 160f;              // 1 px = 1 dp
        const float Fotograma = 1f / 60f;

        static readonly Coordenada CalleAncha = new Coordenada(36.77814, -6.35148);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        static readonly Direccion DireccionCalleAncha =
            new Direccion("Calle Ancha 10", "Sanlúcar de Barrameda", CalleAncha, "Calle Ancha", "10");

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        // ───────────────────────── Falsos ─────────────────────────

        /// <summary>Mapa falso: proyecta y desproyecta solo los puntos dados; la pulsación larga la lanza la prueba.</summary>
        class MapaFalso : IMapa
        {
            public readonly Dictionary<Coordenada, PuntoPantalla> puntos = new Dictionary<Coordenada, PuntoPantalla>();
            public readonly Dictionary<(float, float), Coordenada> suelo = new Dictionary<(float, float), Coordenada>();
            public Action<PuntoPantalla> pulsacion;
            public readonly List<(Coordenada destino, PuntoPantalla enPantalla)> centradosEn = new List<(Coordenada, PuntoPantalla)>();
            public void Centrar(Coordenada destino) { }
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
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add => pulsacion += value; remove => pulsacion -= value; }
            public Coordenada? Desproyectar(PuntoPantalla punto) => suelo.TryGetValue((punto.X, punto.Y), out var c) ? c : (Coordenada?)null;
        }

        class Inversa
        {
            public Coordenada Punto;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<Direccion>> Tcs;
            public void Responder(Direccion d) => Tcs.TrySetResult(Resultado<Direccion>.Exito(d));
            public void Fallar() => Tcs.TrySetResult(Resultado<Direccion>.Fallo("sin conexión"));
        }

        /// <summary>Geocodificador falso: cada geocodificación inversa queda pendiente hasta que la prueba responde.</summary>
        class GeoFalso : IGeocodificador
        {
            /// <summary>No atiende a la cancelación: su respuesta puede llegar tarde.</summary>
            public bool IgnorarCancelacion;
            public readonly List<Inversa> Inversas = new List<Inversa>();

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct) =>
                Task.FromResult(Resultado<IReadOnlyList<Direccion>>.Fallo("no se usa", Array.Empty<Direccion>()));

            public Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct)
            {
                var i = new Inversa { Punto = punto, Ct = ct, Tcs = new TaskCompletionSource<Resultado<Direccion>>() };
                Inversas.Add(i);
                if (!IgnorarCancelacion) ct.Register(() => i.Tcs.TrySetCanceled());
                return i.Tcs.Task;
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

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        GameObject objeto;
        PantallaMapa pantalla;
        MapaFalso mapa;
        GeoFalso geo;
        SynchronizationContext contextoAntes;

        FichaLugar Ficha => pantalla.Ficha;
        AccesibilidadPantalla Acc => pantalla.Accesibilidad;

        [SetUp]
        public void Preparar()
        {
            // Sin contexto de sincronización, la continuación corre en cuanto la prueba responde la petición falsa
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            Crear(new GeoFalso());
        }

        /// <summary>Ubicación fija en un estado (sin GPS): solo para abrir el aviso de ubicación.</summary>
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

        void Crear(GeoFalso nuevo, Func<IPreferencias, ServicioUbicacion> ubicacion = null)
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            mapa = new MapaFalso();
            geo = nuevo;
            pantalla.Inicializar(mapa, tema, Lugares(), Ahora, arbol, filtro, ubicacion?.Invoke(prefs), geocodificador: geo);
            tema.Iniciar();
            filtro.Iniciar();
            filtro.Elegir(FiltroPrecios.Todos);
        }

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        /// <summary>Pone la coordenada en ese punto (dp): Proyectar la lleva ahí y Desproyectar la devuelve.</summary>
        PuntoPantalla Situar(Coordenada c, Vector2 dp)
        {
            var px = new PuntoPantalla(dp.x / Escala, dp.y / Escala, 300f);
            mapa.puntos[c] = px;
            mapa.suelo[(px.X, px.Y)] = c;
            return px;
        }

        /// <summary>Pulsación larga del mapa (como la lanza MapaVectorial) en el punto donde está la coordenada.</summary>
        void PulsarLargo(Coordenada c, Vector2 dp) => mapa.pulsacion(Situar(c, dp));

        /// <summary>Lo que hace LateUpdate con la chincheta, con el tamaño de pantalla de las pruebas.</summary>
        void Recolocar() => pantalla.Chincheta.Actualizar(mapa, pantalla.PuntoAbierto, Escala, AnchoPx, AltoPx);

        static readonly Vector2 Donde = new Vector2(200f, 300f);

        /// <summary>Mantiene el dedo quieto (o moviéndose hasta "movimiento" dp) durante esos segundos, fotograma a fotograma.</summary>
        static bool Mantener(DetectorToque d, float segundos, float movimiento = 0f)
        {
            bool larga = false;
            int n = Mathf.CeilToInt(segundos / Fotograma);
            for (int i = 1; i <= n; i++)
            {
                d.Mover(new Vector2(100f + movimiento * i / n, 100f));
                larga |= d.Avanzar(Fotograma, Dpi);
            }
            return larga;
        }

        // ───────────────────────── Matriz: gestos ─────────────────────────

        [Test]
        public void PulsacionLarga_600msQuietoAbreElPuntoConLaChinchetaYLuegoLaDireccion()
        {
            var d = new DetectorToque();
            d.Empezar(new Vector2(100f, 100f));
            Assert.IsFalse(Mantener(d, 0.45f), "A los 450 ms aún no.");
            Assert.IsTrue(d.Avanzar(0.15f, Dpi), "A los 600 ms (cumplidos los 500) se dispara, sin soltar.");
            Assert.IsTrue(d.ArrastreBloqueado, "Desde entonces el dedo ya no mueve el mapa.");
            Assert.IsFalse(Mantener(d, 0.1f), "Una sola vez por gesto.");
            Assert.IsFalse(d.Soltar(new Vector2(100f, 100f), Dpi), "Al soltar no hay toque.");

            PulsarLargo(CalleAncha, Donde);
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsNull(Ficha.Lugar);
            Assert.AreEqual(CalleAncha, Ficha.Punto);
            Assert.AreEqual(CalleAncha, pantalla.PuntoAbierto);
            Assert.AreEqual("36,7781, -6,3515", Ficha.Titulo.text, "Mientras carga: coordenadas con 4 decimales y coma decimal.");
            Assert.AreEqual("Punto en el mapa", Ficha.Tipo.text);
            Assert.AreEqual("Buscando la dirección…", Ficha.Direccion.text);
            Assert.AreEqual(DisplayStyle.Flex, Ficha.Direccion.style.display.value);
            Assert.AreEqual(DisplayStyle.None, Ficha.Muestra.style.display.value, "Sin muestra de plantilla.");
            Assert.AreEqual(DisplayStyle.Flex, Ficha.GotaPunto.style.display.value, "En su sitio, la gotita de la maqueta.");
            Assert.AreSame(Ficha.Tipo.parent, Ficha.GotaPunto.parent, "La gotita va en la línea del subtítulo…");
            Assert.Less(Ficha.GotaPunto.parent.IndexOf(Ficha.GotaPunto), Ficha.Tipo.parent.IndexOf(Ficha.Tipo), "…delante de él.");
            Assert.AreEqual(0, Ficha.Filas.Count, "Sin precios.");
            Assert.IsNull(Ficha.SinPrecio, "Ni \"Aún no tiene precio\".");
            Assert.IsTrue(Ficha.ClassListContains(FichaLugar.ClasePunto));

            Recolocar();
            Assert.IsTrue(pantalla.Chincheta.Visible, "La chincheta está en el punto mientras la ficha está abierta.");
            Assert.AreEqual(Donde, pantalla.Chincheta.Posicion);
            Assert.AreEqual(PickingMode.Ignore, pantalla.Chincheta.pickingMode, "No captura toques.");

            var peticion = geo.Inversas.Single();
            Assert.AreEqual(CalleAncha, peticion.Punto);
            peticion.Responder(DireccionCalleAncha);
            Assert.AreEqual("Calle Ancha, 10", Ficha.Titulo.text, "Calle y número con coma.");
            Assert.AreEqual("Sanlúcar de Barrameda", Ficha.Direccion.text, "Debajo, la localidad.");
            Assert.IsFalse(Ficha.BuscandoDireccion);
            Assert.AreEqual("Punto en el mapa", Ficha.Tipo.text);
        }

        [Test]
        public void ConArrastre_600msMoviendose20DpNoHayPulsacionLarga()
        {
            var d = new DetectorToque();
            d.Empezar(new Vector2(100f, 100f));
            Assert.IsFalse(Mantener(d, 0.6f, movimiento: 20f), "Moviéndose 20 dp es un arrastre.");
            Assert.IsFalse(d.ArrastreBloqueado, "El mapa sigue moviéndose con el dedo.");
            Assert.IsFalse(d.Soltar(new Vector2(120f, 100f), Dpi), "Tampoco es un toque.");

            // A 480 dpi, 6 dp son 18 px: 10 px de temblor aún cuentan como quieto
            d.Empezar(new Vector2(100f, 100f));
            d.Mover(new Vector2(110f, 100f));
            Assert.IsTrue(d.Avanzar(0.6f, 480f));
        }

        [Test]
        public void ToqueCorto_SoltarALos200msEsUnToqueNormal()
        {
            var d = new DetectorToque();
            d.Empezar(new Vector2(100f, 100f));
            Assert.IsFalse(Mantener(d, 0.2f));
            Assert.IsTrue(d.Soltar(new Vector2(101f, 100f), Dpi), "Toque normal (2.x).");
            Assert.IsFalse(d.Avanzar(1f, Dpi), "Soltado, el tiempo ya no cuenta.");
            Assert.IsFalse(d.PulsacionLargaDada);

            // El siguiente gesto empieza de cero
            d.Empezar(new Vector2(100f, 100f));
            Assert.IsFalse(Mantener(d, 0.3f), "El tiempo del gesto anterior no se suma.");
        }

        [Test]
        public void SobreUnMarcador_DaElPuntoLibreYNoLaFichaDelLugar()
        {
            var areas = Lugares().Lugares.Single(l => l.Id == "clinica-areas");
            Situar(areas.Posicion, Donde);
            pantalla.Capa.Actualizar(Escala, AnchoPx, AltoPx);
            Assert.IsTrue(pantalla.Capa.Marcadores.Single(m => m.Lugar == areas).Visible, "El marcador se ve justo donde se pulsa.");

            Assert.IsTrue(pantalla.PulsarLargoMapa(mapa.puntos[areas.Posicion]));
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsNull(Ficha.Lugar, "No es la ficha de Clínica Areas.");
            Assert.AreEqual(areas.Posicion, Ficha.Punto, "Punto libre en ese sitio.");
            Assert.AreEqual("Punto en el mapa", Ficha.Tipo.text);
            Assert.IsNull(pantalla.Capa.Fijado, "Ningún marcador queda fijado.");
        }

        [Test]
        public void DosDedos_UnSegundoDedoAntesDe500msLaAnula()
        {
            var d = new DetectorToque();
            d.Empezar(new Vector2(100f, 100f));
            Assert.IsFalse(Mantener(d, 0.3f));
            d.Anular(); // segundo dedo
            Assert.IsFalse(Mantener(d, 0.5f), "Gesto de dos dedos, sin ficha.");
            Assert.IsFalse(d.ArrastreBloqueado);
            Assert.IsFalse(d.Soltar(new Vector2(100f, 100f), Dpi));

            // Si ya se dio, el segundo dedo libera el mapa para el gesto de dos dedos
            d.Empezar(new Vector2(100f, 100f));
            Assert.IsTrue(Mantener(d, 0.6f));
            d.Anular();
            Assert.IsFalse(d.ArrastreBloqueado);
        }

        [Test]
        public void PorEncimaDelHorizonteNoHayPunto()
        {
            Assert.IsFalse(pantalla.PulsarLargoMapa(new PuntoPantalla(10f, 10f, 1f)), "Desproyectar da null: nada.");
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsEmpty(geo.Inversas);
        }

        // ───────────────────────── Matriz: respuestas ─────────────────────────

        [Test]
        public void SinConexion_ElTituloSonLasCoordenadas()
        {
            PulsarLargo(CalleAncha, Donde);
            geo.Inversas.Single().Fallar();
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreEqual("36,7781, -6,3515", Ficha.Titulo.text);
            Assert.IsFalse(Ficha.BuscandoDireccion, "Sin \"Buscando la dirección…\".");
            Assert.AreEqual(DisplayStyle.None, Ficha.Direccion.style.display.value);
        }

        [Test]
        public void SinCalle_ElTituloSonLasCoordenadas()
        {
            PulsarLargo(CalleAncha, Donde);
            geo.Inversas.Single().Responder(new Direccion("Pinar de la Algaida", "Sanlúcar de Barrameda", CalleAncha));
            Assert.AreEqual("36,7781, -6,3515", Ficha.Titulo.text);
            Assert.AreEqual(DisplayStyle.None, Ficha.Direccion.style.display.value, "Sin calle no se pone la localidad sola.");

            // Sin nada cerca (Photon no da ningún rasgo), igual
            PulsarLargo(Cabildo, Donde + new Vector2(0f, 40f));
            geo.Inversas.Last().Responder(null);
            Assert.AreEqual("36,7779, -6,3528", Ficha.Titulo.text);
        }

        [Test]
        public void ConCalleSinNumeroYSinLocalidad()
        {
            PulsarLargo(CalleAncha, Donde);
            geo.Inversas.Single().Responder(new Direccion("Calle Ancha", "", CalleAncha, "Calle Ancha"));
            Assert.AreEqual("Calle Ancha", Ficha.Titulo.text);
            Assert.AreEqual(DisplayStyle.None, Ficha.Direccion.style.display.value);
        }

        [Test]
        public void RespuestaTardia_LaFichaEnsenaLaDireccionDeB()
        {
            Crear(new GeoFalso { IgnorarCancelacion = true });
            PulsarLargo(CalleAncha, Donde);                              // A
            PulsarLargo(Cabildo, Donde + new Vector2(0f, 60f));          // B, antes de que llegue A
            Assert.AreEqual(2, geo.Inversas.Count);
            Assert.IsTrue(geo.Inversas[0].Ct.IsCancellationRequested, "La petición de A se cancela.");

            // A llega tarde: no cambia la ficha de B
            geo.Inversas[0].Responder(DireccionCalleAncha);
            Assert.AreEqual(Cabildo, Ficha.Punto);
            Assert.AreEqual("36,7779, -6,3528", Ficha.Titulo.text);
            Assert.IsTrue(Ficha.BuscandoDireccion, "Sigue esperando la de B.");

            geo.Inversas[1].Responder(new Direccion("Plaza del Cabildo 1", "Sanlúcar de Barrameda", Cabildo, "Plaza del Cabildo", "1"));
            Assert.AreEqual("Plaza del Cabildo, 1", Ficha.Titulo.text);
        }

        // ───────────────────────── Matriz: cerrar ─────────────────────────

        void AbrirYComprobarQueSeVe()
        {
            PulsarLargo(CalleAncha, Donde);
            Recolocar();
            Assert.IsTrue(pantalla.Chincheta.Visible);
        }

        void ComprobarCerrada()
        {
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsNull(pantalla.PuntoAbierto);
            Assert.IsFalse(pantalla.Chincheta.Visible, "La chincheta desaparece al cerrar.");
            Recolocar();
            Assert.IsFalse(pantalla.Chincheta.Visible, "Y no vuelve en el siguiente fotograma.");
        }

        [Test]
        public void Cerrar_LaXQuitaFichaYChincheta()
        {
            AbrirYComprobarQueSeVe();
            Ficha.BotonCerrar.Pulsar();
            ComprobarCerrada();
        }

        [Test]
        public void Cerrar_AtrasQuitaFichaYChincheta()
        {
            AbrirYComprobarQueSeVe();
            Assert.IsTrue(pantalla.Atras());
            ComprobarCerrada();
        }

        [Test]
        public void Cerrar_ToqueFueraQuitaFichaYChincheta()
        {
            AbrirYComprobarQueSeVe();
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)), "Con la ficha abierta el gesto no mueve el mapa.");
            Ficha.TocarFuera();
            ComprobarCerrada();
        }

        [Test]
        public void UnaRespuestaTrasCerrarNoCambiaNada()
        {
            Crear(new GeoFalso { IgnorarCancelacion = true });
            PulsarLargo(CalleAncha, Donde);
            pantalla.CerrarFicha();
            Assert.IsTrue(geo.Inversas.Single().Ct.IsCancellationRequested);
            geo.Inversas.Single().Responder(DireccionCalleAncha);
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.AreNotEqual("Calle Ancha, 10", Ficha.Titulo.text);
        }

        [Test]
        public void ElPuntoSeCentraEncimaDeLaHojaComoUnLugar()
        {
            PulsarLargo(CalleAncha, Donde);
            var (destino, enPantalla) = mapa.centradosEn.Single();
            Assert.AreEqual(CalleAncha, destino);
            var esperado = PantallaMapa.PuntoCentradoFicha(Screen.width, Screen.height, null);
            Assert.AreEqual(esperado.X, enPantalla.X, 1e-3f);
            Assert.AreEqual(esperado.Y, enPantalla.Y, 1e-3f);
        }

        [Test]
        public void DelPuntoALaFichaDeUnLugarSeVaLaChincheta()
        {
            Crear(new GeoFalso { IgnorarCancelacion = true });
            AbrirYComprobarQueSeVe();
            var areas = Lugares().Lugares.Single(l => l.Id == "clinica-areas");
            pantalla.AbrirFicha(areas);
            Assert.AreSame(areas, Ficha.Lugar);
            Assert.IsNull(Ficha.Punto);
            Assert.IsFalse(pantalla.Chincheta.Visible);
            Assert.AreEqual(DisplayStyle.Flex, Ficha.Muestra.style.display.value, "La muestra vuelve con un lugar.");
            Assert.AreEqual(DisplayStyle.None, Ficha.GotaPunto.style.display.value, "Y la gotita se oculta.");
            Assert.IsFalse(Ficha.ClassListContains(FichaLugar.ClasePunto));
            geo.Inversas.Single().Responder(DireccionCalleAncha);
            Assert.AreEqual("Clínica Areas", Ficha.Titulo.text, "La dirección tardía del punto no pisa al lugar.");
        }

        [Test]
        public void ConElMenuOUnaVentanaDeUbicacionNoSeAbre()
        {
            pantalla.AlternarMenu();
            Assert.IsTrue(pantalla.MenuAbierto);
            Situar(CalleAncha, Donde);
            Assert.IsFalse(pantalla.PulsarLargoMapa(mapa.puntos[CalleAncha]));
            Assert.IsFalse(pantalla.MenuAbierto, "Solo cierra el menú, como un toque.");
            Assert.IsFalse(pantalla.FichaAbierta);

            // Con el aviso de ubicación abierto no hace nada
            Crear(new GeoFalso(), prefs => new ServicioUbicacion(new UbicacionFalsa { Estado = EstadoUbicacion.Denegado }, prefs));
            pantalla.TocarUbicacion(); // denegado: abre el aviso
            Assert.IsTrue(pantalla.ModalUbicacionAbierta);
            Situar(CalleAncha, Donde);
            Assert.IsFalse(pantalla.PulsarLargoMapa(mapa.puntos[CalleAncha]));
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(pantalla.AvisoAbierto, "El aviso sigue abierto.");
            Assert.IsEmpty(geo.Inversas);
        }

        [Test]
        public void CadaFotogramaActualizarCapasColocaLaChinchetaYAlCerrarLaOculta()
        {
            // ActualizarCapas usa Screen.width/height: el punto cae en el centro de la pantalla del editor
            var centro = new PuntoPantalla(Screen.width / 2f, Screen.height / 2f, 300f);
            mapa.puntos[CalleAncha] = centro;
            mapa.suelo[(centro.X, centro.Y)] = CalleAncha;
            Assert.IsTrue(pantalla.PulsarLargoMapa(centro));
            pantalla.ActualizarCapas(Escala);
            Assert.IsTrue(pantalla.Chincheta.Visible);
            Assert.AreEqual(new Vector2(Screen.width / 2f * Escala, Screen.height / 2f * Escala), pantalla.Chincheta.Posicion);

            pantalla.CerrarFicha();
            pantalla.ActualizarCapas(Escala);
            Assert.IsFalse(pantalla.Chincheta.Visible, "Sin ficha del punto, el fotograma no la vuelve a poner.");
        }

        /// <summary>Componer entrega el geocodificador a una pantalla con interfaz de verdad (como PruebasArranque).</summary>
        [Test]
        public void ElArranqueLeDaElGeocodificadorALaPantalla()
        {
            var conInterfaz = new GameObject("Pantalla con interfaz");
            try
            {
                conInterfaz.SetActive(false);
                var documento = conInterfaz.AddComponent<UIDocument>();
                documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
                documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
                var conUi = conInterfaz.AddComponent<PantallaMapa>();
                conInterfaz.SetActive(true); // UIDocument (ExecuteAlways) crea su árbol también en el editor
                Assert.IsNotNull(documento.rootVisualElement?.Q("raiz"), "El UIDocument no ha creado el árbol de la interfaz.");

                var prefs = new PreferenciasFalsas();
                var otroMapa = new MapaFalso();
                var otroGeo = new GeoFalso();
                Assert.IsTrue(Arranque.Arranque.Componer(otroMapa, conUi, new ServicioTema(prefs, new SistemaClaro()), Lugares(),
                    new ServicioFiltro(prefs), Ahora, geocodificador: otroGeo));
                var px = new PuntoPantalla(100f, 200f, 300f);
                otroMapa.suelo[(px.X, px.Y)] = CalleAncha;
                otroMapa.pulsacion(px); // la pulsación larga llega por el puerto, como en la app
                Assert.IsTrue(conUi.FichaAbierta);
                Assert.AreEqual(CalleAncha, otroGeo.Inversas.Single().Punto, "La pantalla pregunta la dirección a ese geocodificador.");
            }
            finally { UnityEngine.Object.DestroyImmediate(conInterfaz); }
        }

        [Test]
        public void SinGeocodificadorSoloCoordenadasYSinBuscando()
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = new GameObject("Pantalla sin geocodificador");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            mapa = new MapaFalso();
            pantalla.Inicializar(mapa, new ServicioTema(prefs, new SistemaClaro()), Lugares(), Ahora, arbol, new ServicioFiltro(prefs));
            PulsarLargo(CalleAncha, Donde);
            Assert.AreEqual("36,7781, -6,3515", Ficha.Titulo.text);
            Assert.IsFalse(Ficha.BuscandoDireccion);
            Assert.AreEqual(DisplayStyle.None, Ficha.Direccion.style.display.value);
        }

        [Test]
        public void LaChinchetaVaEncimaDeLaPosicionYDebajoDeLosControles()
        {
            var raiz = pantalla.Chincheta.parent.parent;
            Assert.AreEqual("capa-chincheta", pantalla.Chincheta.parent.name);
            Assert.Greater(raiz.IndexOf(pantalla.Chincheta.parent), raiz.IndexOf(raiz.Q("capa-posicion")));
            Assert.Greater(raiz.IndexOf(pantalla.Chincheta.parent), raiz.IndexOf(raiz.Q("capa-marcadores")));
            Assert.Less(raiz.IndexOf(pantalla.Chincheta.parent), raiz.IndexOf(raiz.Q("zona-segura")), "Los botones siguen encima.");
        }

        // ───────────────────────── TalkBack ─────────────────────────

        [Test]
        public void ConTalkBackLaFichaDelPuntoEsModalConTituloSubtituloYCerrar()
        {
            AccesibilidadPantalla.LectorForzado = () => false; // sin jerarquía: el recorrido se calcula igual
            PulsarLargo(CalleAncha, Donde);
            Assert.IsTrue(Acc.Modal);
            Assert.AreSame(Ficha.Titulo, Acc.Enfocado, "El foco va al título.");
            CollectionAssert.AreEqual(new[] { "36,7781, -6,3515", "Punto en el mapa", "Cerrar ficha" }, Acc.Recorrido(),
                "\"Buscando la dirección…\" no se lee.");

            geo.Inversas.Single().Responder(DireccionCalleAncha);
            CollectionAssert.AreEqual(new[] { "Calle Ancha, 10, Sanlúcar de Barrameda", "Punto en el mapa", "Cerrar ficha" }, Acc.Recorrido());
            Assert.AreSame(Ficha.Titulo, Acc.Enfocado, "El foco no se mueve.");

            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(Acc.Modal);
            Assert.IsNull(Acc.Enfocado, "No hay marcador al que volver.");
        }

        [Test]
        public void AlLlegarLaDireccionSeRehaceElTituloEnSuNodoSinMoverElFoco()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Acc.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            PulsarLargo(CalleAncha, Donde);
            var nodos = AssistiveSupport.activeHierarchy.rootNodes.ToList();
            CollectionAssert.AreEqual(new[] { "36,7781, -6,3515", "Punto en el mapa", "Cerrar ficha" }, nodos.Select(n => n.label).ToList());
            var titulo = nodos[0];
            var jerarquia = AssistiveSupport.activeHierarchy;

            geo.Inversas.Single().Responder(DireccionCalleAncha);
            Assert.AreSame(jerarquia, AssistiveSupport.activeHierarchy, "No se rehace la jerarquía.");
            Assert.AreEqual("Calle Ancha, 10, Sanlúcar de Barrameda", titulo.label, "El mismo nodo lleva el título nuevo.");
            Assert.AreSame(Ficha.Titulo, Acc.Enfocado);
        }

        // ───────────────────────── Formato ─────────────────────────

        [Test]
        public void LasCoordenadasVanConCuatroDecimalesYComaDecimal()
        {
            Assert.AreEqual("36,7781, -6,3515", FichaLugar.TextoCoordenadas(new Coordenada(36.77814, -6.35148), T));
            Assert.AreEqual("36,7000, -6,0000", FichaLugar.TextoCoordenadas(new Coordenada(36.7, -6.0), T));
            Assert.AreEqual("-33,8688, 151,2093", FichaLugar.TextoCoordenadas(new Coordenada(-33.86882, 151.20929), T));
        }

        [Test]
        public void ElTituloDeLaDireccionJuntaCalleYNumeroConComa()
        {
            Assert.AreEqual("Calle Ancha, 10", FichaLugar.TituloDireccion(DireccionCalleAncha, T));
            Assert.AreEqual("Calle Ancha", FichaLugar.TituloDireccion(new Direccion("x", "", CalleAncha, "Calle Ancha"), T));
            Assert.IsNull(FichaLugar.TituloDireccion(new Direccion("Bar Juan", "", CalleAncha), T), "Sin calle, null.");
            Assert.IsNull(FichaLugar.TituloDireccion(null, T));
        }

        // ───────────────────────── Photon /reverse ─────────────────────────

        [Test]
        public void LaUrlInversaLlevaLatLonYLimit1()
        {
            Assert.AreEqual("https://photon.komoot.io/reverse?lat=36.77814&lon=-6.35148&limit=1&lang=default",
                GeocodificadorPhoton.UrlInversa(CalleAncha));
        }

        [Test]
        public void LeeLaRespuestaInversaConCalleNumeroYLocalidad()
        {
            const string json = @"{""type"":""FeatureCollection"",""features"":[
 {""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[-6.35148,36.77814]},
  ""properties"":{""osm_key"":""place"",""street"":""Calle Ancha"",""housenumber"":""10"",""city"":""Sanlúcar de Barrameda""}}]}";
            var r = LecturaPhoton.LeerInverso(json);
            Assert.IsTrue(r.Ok, r.Error);
            Assert.AreEqual("Calle Ancha", r.Valor.Calle);
            Assert.AreEqual("10", r.Valor.Numero);
            Assert.AreEqual("Sanlúcar de Barrameda", r.Valor.Localidad);
            Assert.AreEqual("Calle Ancha 10", r.Valor.Nombre, "El nombre del buscador no cambia (sin coma).");
            Assert.AreEqual("Calle Ancha, 10", FichaLugar.TituloDireccion(r.Valor, T));
        }

        [Test]
        public void EncimaDeUnaCalleLaCalleEsSuNombre()
        {
            const string json = @"{""features"":[{""geometry"":{""coordinates"":[-6.35,36.77]},
  ""properties"":{""osm_key"":""highway"",""name"":""Calle Ancha"",""city"":""Sanlúcar de Barrameda""}}]}";
            var r = LecturaPhoton.LeerInverso(json);
            Assert.IsTrue(r.Ok, r.Error);
            Assert.AreEqual("Calle Ancha", r.Valor.Calle);
            Assert.AreEqual("", r.Valor.Numero);

            // Un sitio con nombre que no es una calle ni está en una: sin calle
            const string sinCalle = @"{""features"":[{""geometry"":{""coordinates"":[-6.35,36.77]},
  ""properties"":{""osm_key"":""natural"",""name"":""Pinar de la Algaida""}}]}";
            Assert.AreEqual("", LecturaPhoton.LeerInverso(sinCalle).Valor.Calle);
        }

        [Test]
        public void SinRasgosLaInversaDaNullYUnaRespuestaRotaEsUnFallo()
        {
            var vacia = LecturaPhoton.LeerInverso("{\"type\":\"FeatureCollection\",\"features\":[]}");
            Assert.IsTrue(vacia.Ok);
            Assert.IsNull(vacia.Valor);
            Assert.IsFalse(LecturaPhoton.LeerInverso("<html>502 Bad Gateway</html>").Ok);
        }

        // ───────────────────────── Puerto real (MapaVectorial) ─────────────────────────

        [Test]
        public void DesproyectarEsLaInversaDeProyectarYPorEncimaDelHorizonteDaNull()
        {
            double latAntes = Geo.LatitudOrigen, lonAntes = Geo.LongitudOrigen;
            Geo.LatitudOrigen = 36.7779; Geo.LongitudOrigen = -6.3528;
            var objCamara = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = objCamara.AddComponent<CamaraMapa>();
            var objMapa = new GameObject("Mapa de prueba");
            try
            {
                IMapa real = objMapa.AddComponent<MapaVectorial>();
                camara.giro = 35f; camara.inclinacion = 50f; camara.distancia = 900f;
                real.Centrar(Cabildo);
                var p = real.Proyectar(CalleAncha);
                Assert.IsTrue(p.Delante);
                var c = real.Desproyectar(p);
                Assert.IsTrue(c.HasValue);
                Assert.Less(c.Value.DistanciaMetros(CalleAncha), 0.5, $"{c.Value.Latitud}, {c.Value.Longitud}");

                // Muy poco inclinado, lo más alto de la pantalla es cielo
                camara.inclinacion = 20f;
                real.Centrar(Cabildo);
                var cam = objCamara.GetComponent<Camera>();
                Assert.IsNull(real.Desproyectar(new PuntoPantalla(cam.pixelWidth / 2f, Screen.height - cam.pixelHeight + 1f, 1f)));

                // La pulsación larga de la cámara llega por el puerto con la Y desde arriba
                var recibidos = new List<PuntoPantalla>();
                real.PulsacionLarga += recibidos.Add;
                camara.SimularPulsacionLarga(new Vector2(120f, 30f));
                Assert.AreEqual(1, recibidos.Count);
                Assert.AreEqual(120f, recibidos[0].X, 1e-3f);
                Assert.AreEqual(Screen.height - 30f, recibidos[0].Y, 1e-3f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objMapa);
                UnityEngine.Object.DestroyImmediate(objCamara);
                Geo.LatitudOrigen = latAntes; Geo.LongitudOrigen = lonAntes;
            }
        }
    }
}
