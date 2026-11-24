using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Firebase;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Entrar con Google o seguir sin cuenta (historia 3.3, parte 1): cada fila de la matriz con cuentas falsas y la pantalla
    /// del mapa real, TalkBack, los textos, el contraste, el adaptador de Firebase Auth con uno falso, el plugin de Android y
    /// la composición.
    /// </summary>
    public class PruebasCuentas
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Usuario = new Coordenada(36.7790, -6.3540);
        static readonly Coordenada Sur = new Coordenada(36.7772, -6.3540);
        static readonly Lugar BarPepe = new Lugar("bar-pepe", "Bar Pepe", "Bar", Plantilla.Hosteleria, Usuario, "Calle Ancha, 3");
        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        // ───────────────────────── Dobles ─────────────────────────

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class ServidorFalso : IServidor
        {
            public EstadoServidor Estado { get; set; } = EstadoServidor.Listo;
            public event Action Cambiado;
            public void Poner(EstadoServidor estado) { Estado = estado; Cambiado?.Invoke(); }
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado => Posicion.HasValue ? EstadoUbicacion.Activa : EstadoUbicacion.SinPermiso;
            public Coordenada? Posicion { get; private set; }
            public float? Rumbo => null;
            public event Action Cambiado;
            public void Mover(Coordenada c) { Posicion = c; Cambiado?.Invoke(); }
            public bool PuedePedirPermiso => true;
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

        /// <summary>Una sola ruta, del origen al destino, al momento.</summary>
        class RutasFalsas : IRutas
        {
            public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
            {
                double metros = origen.DistanciaMetros(destino);
                return Task.FromResult(Resultado<Ruta>.Exito(new Ruta(new[] { origen, destino }, metros, 120, new[]
                {
                    new Maniobra(TipoManiobra.Salida, "Diríjase al norte.", origen, metros),
                    new Maniobra(TipoManiobra.Llegada, "Ha llegado.", destino, 0),
                })));
            }
        }

        class ResolutorFalso : IResolutorEnlaces
        {
            public readonly TaskCompletionSource<Resultado<string>> respuesta = new TaskCompletionSource<Resultado<string>>();
            public Task<Resultado<string>> Resolver(string url, CancellationToken ct) => respuesta.Task;
        }

        class NotificacionesFalsas : INotificaciones
        {
            public EstadoNotificaciones Estado => EstadoNotificaciones.SinPermiso;
            public bool PuedePedir => true;
            public event Action Cambiado { add { } remove { } }
            public void PedirPermiso() { }
        }

        class MapaFalso : IMapa
        {
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => new PuntoPantalla(100f, 100f, 900f);
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => Usuario;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => Usuario;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
        }

        class Repositorio : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(new[] { BarPepe });
        }

        // ───────────────────────── Montaje ─────────────────────────

        readonly List<GameObject> objetos = new List<GameObject>();
        readonly List<string> anuncios = new List<string>();
        SynchronizationContext contextoAntes;
        PreferenciasFalsas prefs;
        CuentasFalsas google;
        ServidorFalso servidor;
        TaskCompletionSource<bool> espera; // la del servidor
        ServicioCuentas cuentas;
        ServicioBienvenida bienvenida;
        PantallaMapa pantalla;
        VisualElement arbol;
        UbicacionFalsa gps;
        ResolutorFalso resolutor;

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones, al momento
            Textos.Forzados = T;
            prefs = new PreferenciasFalsas();
            google = new CuentasFalsas();
            servidor = new ServidorFalso();
            espera = new TaskCompletionSource<bool>();
            CrearServicio();
            anuncios.Clear();
            AccesibilidadPantalla.AnuncioPedido += Anotar;
        }

        void Anotar(string texto) => anuncios.Add(texto);

        void CrearServicio() => cuentas = new ServicioCuentas(google, servidor, prefs, (_, ct) =>
        {
            ct.Register(() => espera.TrySetCanceled());
            return espera.Task;
        });

        [TearDown]
        public void Limpiar()
        {
            AccesibilidadPantalla.AnuncioPedido -= Anotar;
            foreach (var o in objetos) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            objetos.Clear();
            Textos.Forzados = null;
            EscalaFuente.Forzada = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        /// <summary>Abre la app (como Componer). Con la bienvenida sin ver, sale.</summary>
        /// <param name="conRutas">Con posición (200 m al sur del bar) y rutas, para «Cómo llegar» y navegar.</param>
        void Abrir(bool bienvenidaVista = true, bool conRutas = false)
        {
            if (bienvenidaVista) prefs.Guardar(ServicioBienvenida.ClaveVista, "1");
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            pantalla = objeto.AddComponent<PantallaMapa>();
            arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var lugares = new ServicioLugares(new Repositorio());
            lugares.Cargar();
            var mapa = new MapaFalso();
            gps = new UbicacionFalsa();
            if (conRutas) gps.Mover(Sur);
            var ubicacion = new ServicioUbicacion(gps, prefs);
            Task Nunca(TimeSpan t, CancellationToken ct) => new TaskCompletionSource<bool>().Task;
            var rutas = conRutas ? new ServicioRutas(new RutasFalsas(), null, Nunca) : null;
            resolutor = new ResolutorFalso();
            bienvenida = new ServicioBienvenida(prefs, ubicacion, new NotificacionesFalsas());
            pantalla.Inicializar(mapa, tema, lugares, Ahora, arbol, filtro, ubicacion,
                busqueda: new ServicioBusqueda(lugares, mapa, Ahora), resolutorEnlaces: resolutor, rutas: rutas,
                navegacion: rutas != null ? new ServicioNavegacion(ubicacion, rutas, Nunca) : null, bienvenida: bienvenida, cuentas: cuentas);
            tema.Iniciar();
            filtro.Iniciar();
            ubicacion.Iniciar();
            pantalla.Buscador.Medir(800f);
        }

        void TerminarBienvenida()
        {
            Assert.IsTrue(pantalla.BienvenidaAbierta);
            bienvenida.Siguiente();
            bienvenida.Siguiente();
            bienvenida.Terminar();
        }

        /// <summary>Lo que hace Unity al destruirla (en EditMode no llama a OnDestroy).</summary>
        void Destruir() => typeof(PantallaMapa).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic).Invoke(pantalla, null);

        void ContestarFallo(string error) =>
            google.Responder = _ => Task.FromResult(Resultado<UsuarioCuenta>.Fallo(error));

        TaskCompletionSource<Resultado<UsuarioCuenta>> ContestarDespues()
        {
            var respuesta = new TaskCompletionSource<Resultado<UsuarioCuenta>>();
            google.Responder = _ => respuesta.Task;
            return respuesta;
        }

        static Resultado<UsuarioCuenta> Ok(UsuarioCuenta u) => Resultado<UsuarioCuenta>.Exito(u);

        List<string> Recorrido() => pantalla.Accesibilidad.Recorrido();

        static bool SeVe(VisualElement e) => e.style.display.value == DisplayStyle.Flex;

        // ───────────────────────── Matriz ─────────────────────────

        [Test]
        public void Google_EligeCuenta_SesionToastYLetra()
        {
            Abrir();
            Assert.IsFalse(SeVe(pantalla.Buscador.Inicial));
            pantalla.TocarAvatar();
            Assert.IsTrue(pantalla.CuentaAbierta);
            Assert.IsFalse(pantalla.Cuenta.Completa);
            Assert.AreEqual("Entra en PriceMap", pantalla.Cuenta.Titulo.text);
            int sesiones = 0;
            cuentas.SesionCambiada += () => sesiones++;
            pantalla.Cuenta.PulsarEntrar();
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.AreSame(CuentasFalsas.Ana, cuentas.Usuario);
            Assert.AreEqual(1, sesiones);
            Assert.IsTrue(pantalla.Toast.Visible);
            Assert.AreEqual("Hola, Ana", pantalla.Toast.Texto.text);
            Assert.AreEqual(1, anuncios.Count(a => a == "Hola, Ana"));
            Assert.IsTrue(SeVe(pantalla.Buscador.Inicial));
            Assert.AreEqual("A", pantalla.Buscador.Inicial.text);
            Assert.IsTrue(pantalla.Buscador.Avatar.ClassListContains(HojaBuscador.ClaseAvatarSesion));
        }

        [Test]
        public void ConSesion_ElAvatarDiceComoQuienYNoAbreNada()
        {
            google.Poner(CuentasFalsas.Ana);
            Abrir();
            Assert.AreEqual("A", pantalla.Buscador.Inicial.text, "La letra, ya al abrir.");
            pantalla.TocarAvatar();
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.AreEqual("Has entrado como ana@example.com", pantalla.Toast.Texto.text);
            Assert.IsTrue(pantalla.Toast.Visible);
            Assert.AreEqual(PantallaMapa.AbajoSupuestoControlesDp + 12f, pantalla.Toast.style.top.value.value, 1e-3f, "Sin ruta, bajo los controles.");
            google.Poner(null);
            Assert.IsFalse(SeVe(pantalla.Buscador.Inicial));
            Assert.IsFalse(pantalla.Buscador.Avatar.ClassListContains(HojaBuscador.ClaseAvatarSesion));
        }

        [Test]
        public void GoogleCancelado_LaHojaSigueSinError()
        {
            Abrir();
            pantalla.TocarAvatar();
            ContestarFallo(ErroresCuenta.Cancelado);
            pantalla.Cuenta.PulsarEntrar();
            Assert.IsTrue(pantalla.CuentaAbierta);
            Assert.IsNull(cuentas.Error);
            Assert.IsFalse(cuentas.Entrando);
            Assert.IsTrue(SeVe(pantalla.Cuenta.Google) && SeVe(pantalla.Cuenta.Para));
            Assert.IsFalse(SeVe(pantalla.Cuenta.Aviso) || SeVe(pantalla.Cuenta.Reintentar));
            Assert.IsFalse(pantalla.Toast.Visible);
            Assert.IsNull(cuentas.Usuario);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AhoraNoYAtras_CierranYLaAccionPendienteSeDescarta(bool conAtras)
        {
            Abrir();
            int hechas = 0;
            cuentas.ExigirCuenta(() => hechas++);
            Assert.IsTrue(pantalla.CuentaAbierta);
            if (conAtras) Assert.IsTrue(pantalla.Atras());
            else cuentas.AhoraNo();
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.IsEmpty(arbol.Q("hueco-cuenta").Children());
            Assert.IsFalse(pantalla.Atras());
            pantalla.TocarAvatar();
            pantalla.Cuenta.PulsarEntrar();
            Assert.IsNotNull(cuentas.Usuario);
            Assert.AreEqual(0, hechas, "La de antes ya no.");
        }

        [Test]
        public void ExigirConSesion_EjecutaDirectamente()
        {
            google.Poner(CuentasFalsas.Ana);
            Abrir();
            int hechas = 0;
            cuentas.ExigirCuenta(() => hechas++);
            Assert.AreEqual(1, hechas);
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.Throws<ArgumentNullException>(() => cuentas.ExigirCuenta(null));
        }

        [Test]
        public void ExigirSinSesion_AlEntrarSeCierraYSeHaceUnaVez()
        {
            Abrir();
            int hechas = 0;
            bool cerrada = false;
            cuentas.ExigirCuenta(() => { hechas++; cerrada = !pantalla.CuentaAbierta; });
            ContestarFallo(ErroresCuenta.Desconocido);
            pantalla.Cuenta.PulsarEntrar();
            Assert.AreEqual(0, hechas, "Con un fallo sigue pendiente.");
            google.Responder = _ => Task.FromResult(Ok(CuentasFalsas.Ana));
            pantalla.Cuenta.PulsarEntrar();
            Assert.AreEqual(1, hechas);
            Assert.IsTrue(cerrada, "Ya cerrada.");
            google.Poner(null);
            google.Poner(CuentasFalsas.Ana);
            Assert.AreEqual(1, hechas, "Una sola vez.");
        }

        [Test]
        public void ExigirDosVeces_UnaPendienteALaVez()
        {
            var hechas = new List<string>();
            cuentas.ExigirCuenta(() => hechas.Add("a"));
            cuentas.ExigirCuenta(() => hechas.Add("b"));
            cuentas.EntrarConGoogle().Wait();
            CollectionAssert.AreEqual(new[] { "b" }, hechas);
        }

        [TestCase(ErroresCuenta.SinConexion, "Necesitas conexión para entrar.")]
        [TestCase(ErroresCuenta.Desconocido, "No se pudo iniciar sesión. Inténtalo de nuevo.")]
        [TestCase(ErroresCuenta.NoDisponible, "Ahora no se puede iniciar sesión en este móvil. Puedes seguir sin cuenta.")]
        public void ConUnFallo_SuMensajeYReintentar(string error, string texto)
        {
            Abrir();
            pantalla.TocarAvatar();
            ContestarFallo(error);
            pantalla.Cuenta.PulsarEntrar();
            var c = pantalla.Cuenta;
            Assert.IsTrue(SeVe(c.Aviso) && SeVe(c.Reintentar) && !SeVe(c.Google) && !SeVe(c.Para));
            Assert.AreEqual(texto, c.Aviso.text);
            Assert.AreEqual("Reintentar", c.Reintentar.text);
            CollectionAssert.Contains(anuncios, texto);
            Assert.AreSame(c.Reintentar, pantalla.Accesibilidad.Enfocado);

            // El mismo fallo otra vez se vuelve a decir; mientras se reintenta, el mensaje sigue
            anuncios.Clear();
            var respuesta = ContestarDespues();
            c.PulsarEntrar();
            Assert.IsTrue(SeVe(c.Aviso));
            Assert.AreEqual("Entrando…", c.Reintentar.text);
            respuesta.SetResult(Resultado<UsuarioCuenta>.Fallo(error));
            CollectionAssert.Contains(anuncios, texto);

            google.Responder = _ => Task.FromResult(Ok(CuentasFalsas.Ana));
            c.PulsarEntrar();
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.AreEqual("Hola, Ana", pantalla.Toast.Texto.text);
        }

        [Test]
        public void ReintentarYCancelar_SeVaElErrorYVuelveGoogle()
        {
            Abrir();
            pantalla.TocarAvatar();
            ContestarFallo(ErroresCuenta.SinConexion);
            pantalla.Cuenta.PulsarEntrar();
            ContestarFallo(ErroresCuenta.Cancelado);
            pantalla.Cuenta.PulsarEntrar();
            Assert.IsTrue(SeVe(pantalla.Cuenta.Google) && !SeVe(pantalla.Cuenta.Aviso));
            Assert.AreSame(pantalla.Cuenta.Google, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void UnaExcepcionDelAdaptador_EsElFalloGenerico()
        {
            cuentas.Abrir();
            google.Responder = _ => throw new InvalidOperationException("x");
            cuentas.EntrarConGoogle().Wait();
            Assert.AreEqual(ErroresCuenta.Desconocido, cuentas.Error);
            Assert.IsFalse(cuentas.Entrando);
        }

        [Test]
        public void DobleToque_UnaSolaPeticion()
        {
            Abrir();
            pantalla.TocarAvatar();
            var respuesta = ContestarDespues();
            pantalla.Cuenta.PulsarEntrar();
            Assert.IsTrue(cuentas.Entrando);
            Assert.AreEqual("Entrando…", pantalla.Cuenta.TextoGoogle.text);
            pantalla.Cuenta.PulsarEntrar();
            Assert.AreEqual(1, google.Peticiones);
            respuesta.SetResult(Ok(CuentasFalsas.Ana));
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.AreEqual(1, google.Peticiones);
        }

        [Test]
        public void AhoraNoConLaPeticionEnCurso_SeCancelaYNoEntra()
        {
            Abrir();
            pantalla.TocarAvatar();
            var respuesta = ContestarDespues();
            var ct = default(CancellationToken);
            var responder = google.Responder;
            google.Responder = t => { ct = t; return responder(t); };
            pantalla.Cuenta.PulsarEntrar();
            cuentas.AhoraNo();
            Assert.IsTrue(ct.IsCancellationRequested);
            Assert.IsFalse(pantalla.CuentaAbierta || cuentas.Entrando);
            pantalla.TocarAvatar();
            respuesta.SetResult(Ok(CuentasFalsas.Ana));
            Assert.IsNull(cuentas.Usuario);
            Assert.IsTrue(pantalla.CuentaAbierta, "La nueva no se entera.");
            Assert.IsFalse(pantalla.Toast.Visible);
        }

        [Test]
        public void TrasLaBienvenida_PantallaCompletaUnaVezYAlReabrirNo()
        {
            Abrir(bienvenidaVista: false);
            Assert.IsFalse(pantalla.CuentaAbierta);
            TerminarBienvenida();
            Assert.IsFalse(pantalla.BienvenidaAbierta);
            Assert.IsTrue(pantalla.CuentaAbierta);
            Assert.IsTrue(pantalla.Cuenta.Completa);
            Assert.IsTrue(pantalla.Cuenta.ClassListContains(HojaCuenta.ClaseCompleta));
            Assert.AreEqual("Crea tu cuenta", pantalla.Cuenta.Titulo.text);
            Assert.IsNotNull(pantalla.Cuenta.Q(className: "bienvenida__pin"), "Con el logo de la bienvenida.");
            Assert.AreEqual("1", prefs.Leer("cuenta.ofrecida"));
            cuentas.AhoraNo();
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.AreSame(pantalla.BotonUbicacion, pantalla.Accesibilidad.Enfocado);
            cuentas.Ofrecer();
            Assert.IsFalse(pantalla.CuentaAbierta);

            CrearServicio();
            Abrir();
            Assert.IsFalse(pantalla.CuentaAbierta);
        }

        [Test]
        public void CerradaLaAppConLaPantallaDelante_TampocoVuelve()
        {
            Abrir(bienvenidaVista: false);
            TerminarBienvenida();
            Destruir();
            Assert.AreEqual(EntradaCuenta.Cerrada, cuentas.Entrada);
            CrearServicio();
            Abrir();
            Assert.IsFalse(pantalla.CuentaAbierta);
        }

        [Test]
        public void LaBienvenidaYaVistaDeAntes_NoLaOfrece()
        {
            Abrir();
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.IsNull(prefs.Leer(ServicioCuentas.ClaveOfrecida));
        }

        [Test]
        public void ConSesionYa_TrasLaBienvenidaNoSale()
        {
            google.Poner(CuentasFalsas.Ana);
            Abrir(bienvenidaVista: false);
            TerminarBienvenida();
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.IsFalse(pantalla.Toast.Visible);
        }

        [Test]
        public void LaSesionGuardadaLlegaConLaPantallaDelante_SeCierraSinSaludar()
        {
            servidor.Estado = EstadoServidor.Iniciando;
            Abrir(bienvenidaVista: false);
            TerminarBienvenida();
            Assert.IsTrue(pantalla.CuentaAbierta);
            google.Poner(CuentasFalsas.Ana);
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.IsFalse(pantalla.Toast.Visible);
            Assert.AreEqual("A", pantalla.Buscador.Inicial.text);
        }

        [Test]
        public void SinFirebase_MensajeYAhoraNoFunciona()
        {
            servidor.Estado = EstadoServidor.NoDisponible;
            Abrir();
            pantalla.TocarAvatar();
            pantalla.Cuenta.PulsarEntrar();
            Assert.AreEqual(T(ClavesTexto.CuentaErrorServidor), pantalla.Cuenta.Aviso.text);
            Assert.AreEqual(0, google.Peticiones);
            cuentas.AhoraNo();
            Assert.IsFalse(pantalla.CuentaAbierta);
            pantalla.AbrirFicha(BarPepe);
            Assert.IsTrue(pantalla.FichaAbierta, "La app sigue sin cuenta.");
        }

        [Test]
        public void ElServidorSePrepara_SeEsperaYSigue()
        {
            servidor.Estado = EstadoServidor.Iniciando;
            cuentas.Abrir();
            var entrar = cuentas.EntrarConGoogle();
            Assert.IsTrue(cuentas.Entrando);
            Assert.AreEqual(0, google.Peticiones);
            servidor.Poner(EstadoServidor.Listo);
            Assert.IsTrue(entrar.Wait(5000));
            Assert.AreEqual(1, google.Peticiones);
            Assert.AreSame(CuentasFalsas.Ana, cuentas.Usuario);
        }

        [Test]
        public void ElServidorSePreparaYHabiaSesion_EntraSinGoogle()
        {
            servidor.Estado = EstadoServidor.Iniciando;
            int hechas = 0;
            cuentas.ExigirCuenta(() => hechas++);
            var entrar = cuentas.EntrarConGoogle();
            google.Poner(CuentasFalsas.Ana);
            servidor.Poner(EstadoServidor.Listo);
            Assert.IsTrue(entrar.Wait(5000));
            Assert.AreEqual(0, google.Peticiones);
            Assert.AreEqual(EntradaCuenta.Cerrada, cuentas.Entrada);
            Assert.AreEqual(1, hechas);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ElServidorNoLlega_NoDisponible(bool tarda)
        {
            servidor.Estado = EstadoServidor.Iniciando;
            cuentas.Abrir();
            var entrar = cuentas.EntrarConGoogle();
            if (tarda) espera.SetResult(true);
            else servidor.Poner(EstadoServidor.NoDisponible);
            Assert.IsTrue(entrar.Wait(5000));
            Assert.AreEqual(ErroresCuenta.NoDisponible, cuentas.Error);
            Assert.AreEqual(0, google.Peticiones);
            Assert.AreEqual(10, ServicioCuentas.EsperaServidor.TotalSeconds);
        }

        [Test]
        public void AhoraNoEsperandoAlServidor_SueltaLaEspera()
        {
            servidor.Estado = EstadoServidor.Iniciando;
            cuentas.Abrir();
            var entrar = cuentas.EntrarConGoogle();
            cuentas.AhoraNo();
            Assert.IsTrue(entrar.Wait(5000));
            servidor.Poner(EstadoServidor.Listo);
            Assert.AreEqual(0, google.Peticiones);
            Assert.IsNull(cuentas.Error);
        }

        [Test]
        public void SinNombre_InicialYToastConElCorreo()
        {
            Abrir();
            google.Responder = _ => Task.FromResult(Ok(new UsuarioCuenta("u", " ", "zoe@example.com")));
            pantalla.TocarAvatar();
            pantalla.Cuenta.PulsarEntrar();
            Assert.AreEqual("Hola, zoe@example.com", pantalla.Toast.Texto.text);
            Assert.AreEqual("Z", pantalla.Buscador.Inicial.text);
            pantalla.TocarAvatar();
            Assert.AreEqual("Has entrado como zoe@example.com", pantalla.Toast.Texto.text);
        }

        [TestCase("ana maría", "a@b.es", "ana", "A", "a@b.es")]
        [TestCase("  Íñigo ", null, "Íñigo", "Í", "Íñigo")]
        [TestCase(null, "x@b.es", "x@b.es", "X", "x@b.es")]
        [TestCase("", "", "", null, "")]
        public void Usuario_SaludoInicialEIdentidad(string nombre, string correo, string saludo, string inicial, string identidad)
        {
            var u = new UsuarioCuenta("u", nombre, correo);
            Assert.AreEqual((saludo, inicial, identidad), (u.Saludo, u.Inicial, u.Identidad));
            Assert.Throws<ArgumentException>(() => new UsuarioCuenta(" ", nombre, correo));
            StringAssert.DoesNotContain("b.es", u.ToString(), "Escrito en el registro por error no dice nada.");
        }

        // ───────────────────────── Con la pantalla del mapa ─────────────────────────

        [Test]
        public void ConLaHoja_NadaDeDebajoResponde()
        {
            Abrir();
            pantalla.TocarAvatar();
            Assert.IsTrue(pantalla.ModalUbicacionAbierta);
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)));
            Assert.AreEqual(PickingMode.Position, pantalla.Cuenta.pickingMode);
            pantalla.AbrirFicha(BarPepe);
            Assert.IsFalse(pantalla.FichaAbierta);
            pantalla.TocarUbicacion();
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            var raiz = arbol.Q("raiz");
            Assert.Greater(raiz.IndexOf(arbol.Q("hueco-cuenta")), raiz.IndexOf(arbol.Q("hueco-explicacion")));
            Assert.Greater(raiz.IndexOf(arbol.Q("zona-segura-toast")), raiz.IndexOf(arbol.Q("hueco-buscador")), "El toast, encima del buscador.");
            Assert.AreSame(arbol.Q("zona-segura-toast"), pantalla.Toast.parent);
        }

        [Test]
        public void LoCompartidoEsperaAQueSeCierre()
        {
            Abrir(bienvenidaVista: false);
            pantalla.RecibirCompartido("geo:36.77,-6.35");
            TerminarBienvenida();
            Assert.IsFalse(pantalla.AplicarCompartidoPendiente());
            Assert.IsTrue(pantalla.CompartidoPendiente);
            cuentas.AhoraNo();
            Assert.IsTrue(pantalla.AplicarCompartidoPendiente());
            Assert.AreEqual(new Coordenada(36.77, -6.35), pantalla.PuntoAbierto);
        }

        [Test]
        public void DesdeUnaFicha_LaFichaSigueDebajoYVuelve()
        {
            Abrir();
            pantalla.AbrirFicha(BarPepe);
            var boton = pantalla.Ficha.BotonCerrar;
            pantalla.Accesibilidad.PedirFoco(boton);
            cuentas.ExigirCuenta(() => { });
            Assert.IsTrue(pantalla.FichaAbierta && pantalla.CuentaAbierta);
            CollectionAssert.DoesNotContain(Recorrido(), T(ClavesTexto.FichaCerrar), "TalkBack no llega a la ficha.");
            Assert.IsTrue(pantalla.Atras());
            Assert.IsTrue(pantalla.FichaAbierta, "Atrás cierra la de encima.");
            Assert.IsTrue(pantalla.Accesibilidad.Modal, "La de la ficha.");
            CollectionAssert.Contains(Recorrido(), T(ClavesTexto.FichaCerrar));
            Assert.AreSame(boton, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ConElBuscadorDesplegado_SePliegaYAlCerrarVuelveLaCapsula()
        {
            Abrir();
            pantalla.AbrirBuscadorConLector();
            CollectionAssert.Contains(Recorrido(), T(ClavesTexto.CuentaAvatar), "El avatar, también en la hoja.");
            pantalla.TocarAvatar();
            Assert.IsFalse(pantalla.BuscadorAbierto);
            pantalla.ActualizarCapas(1f);
            Assert.IsTrue(pantalla.Accesibilidad.Modal);
            cuentas.AhoraNo();
            pantalla.ActualizarCapas(1f);
            Assert.IsFalse(pantalla.Accesibilidad.Modal);
            Assert.AreSame(pantalla.Buscador.ZonaAvatar, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void ElToastSeQuitaAlAbrirUnaFichaYAlDesplegarElBuscador()
        {
            google.Poner(CuentasFalsas.Ana);
            Abrir();
            pantalla.TocarAvatar();
            Assert.IsTrue(pantalla.Toast.Visible);
            pantalla.AbrirFicha(BarPepe);
            Assert.IsFalse(pantalla.Toast.Visible);
            pantalla.CerrarFicha();
            pantalla.TocarAvatar();
            Assert.IsTrue(pantalla.Toast.Visible);
            pantalla.AbrirBuscadorConLector();
            Assert.IsFalse(pantalla.Toast.Visible);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnEnlaceCortoQueSeResuelveConLaCuentaAbierta_SeAbreAlCerrarla(bool entrando)
        {
            Abrir();
            pantalla.RecibirCompartido("https://maps.app.goo.gl/abc");
            cuentas.Abrir();
            Assert.IsTrue(pantalla.CuentaAbierta);
            resolutor.respuesta.SetResult(Resultado<string>.Exito("https://www.google.com/maps/place/x/@36.77,-6.35,17z/data=!3d36.77!4d-6.35"));
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(pantalla.CuentaAbierta);
            if (entrando) pantalla.Cuenta.PulsarEntrar();
            else cuentas.AhoraNo();
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreEqual(new Coordenada(36.77, -6.35), pantalla.PuntoAbierto);
            Assert.AreSame(pantalla.Ficha.Titulo, pantalla.Accesibilidad.Enfocado);
            Assert.IsTrue(pantalla.Accesibilidad.Modal);
        }

        [Test]
        public void ConElMenuDePrecios_ElAvatarLoCierraYAlVolverEsElMapa()
        {
            Abrir();
            var delMapa = Recorrido();
            pantalla.AlternarMenu();
            pantalla.TocarAvatar();
            Assert.IsFalse(pantalla.MenuAbierto);
            Assert.IsTrue(pantalla.CuentaAbierta);
            cuentas.AhoraNo();
            CollectionAssert.AreEqual(delMapa, Recorrido());
        }

        [Test]
        public void ElAvatarNoEstaEnElRecorridoDeOtraVentanaYVuelve()
        {
            Abrir(bienvenidaVista: false);
            CollectionAssert.DoesNotContain(Recorrido(), "Tu cuenta", "Bienvenida.");
            TerminarBienvenida();
            CollectionAssert.DoesNotContain(Recorrido(), "Tu cuenta", "La propia cuenta.");
            cuentas.AhoraNo();
            CollectionAssert.Contains(Recorrido(), "Tu cuenta");
            pantalla.TocarUbicacion();
            Assert.IsTrue(pantalla.ExplicacionAbierta);
            CollectionAssert.DoesNotContain(Recorrido(), "Tu cuenta", "Explicación de la ubicación.");
            pantalla.AhoraNoUbicacion();
            CollectionAssert.Contains(Recorrido(), "Tu cuenta");
            pantalla.AlternarMenu();
            CollectionAssert.DoesNotContain(Recorrido(), "Tu cuenta", "Menú Precios.");
            pantalla.CerrarMenu();
            CollectionAssert.Contains(Recorrido(), "Tu cuenta");
        }

        [Test]
        public void SobreComoLlegar_AtrasVuelveAElla()
        {
            Abrir(conRutas: true);
            pantalla.AbrirFicha(BarPepe);
            pantalla.Ficha.Acciones.PulsarIr();
            Assert.IsTrue(pantalla.EligiendoRuta);
            var antes = Recorrido();
            var foco = pantalla.Accesibilidad.Enfocado;
            cuentas.ExigirCuenta(() => { });
            CollectionAssert.AreEqual(new[] { "Entra en PriceMap", T(ClavesTexto.CuentaPara), "Continuar con Google", "Ahora no" }, Recorrido());
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.IsTrue(pantalla.EligiendoRuta);
            CollectionAssert.AreEqual(antes, Recorrido());
            Assert.AreSame(foco, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void AlLlegarConLaCuentaDelante_SeAnunciaYAlCerrarlaElFocoVaAFinalizar()
        {
            Abrir(conRutas: true);
            pantalla.AbrirFicha(BarPepe);
            pantalla.Ficha.Acciones.PulsarIr();
            pantalla.Iniciar();
            Assert.IsTrue(pantalla.Navegando);
            cuentas.ExigirCuenta(() => { });
            anuncios.Clear();
            gps.Mover(Usuario);
            Assert.IsTrue(pantalla.LlegadaVisible);
            Assert.IsTrue(pantalla.CuentaAbierta);
            Assert.AreEqual(1, anuncios.Count(a => a.StartsWith("Has llegado")));
            CollectionAssert.DoesNotContain(Recorrido(), T(ClavesTexto.RutaFinalizar));
            cuentas.AhoraNo();
            Assert.AreSame(pantalla.HojaLlegada.Finalizar, pantalla.Accesibilidad.Enfocado);
            CollectionAssert.Contains(Recorrido(), T(ClavesTexto.RutaFinalizar));
            Assert.AreEqual(1, anuncios.Count(a => a.StartsWith("Has llegado")), "Una sola vez.");
        }

        [Test]
        public void SinServicio_ElAvatarNoHaceNada()
        {
            cuentas = null;
            Abrir();
            pantalla.TocarAvatar();
            pantalla.Buscador.PulsarAvatar();
            Assert.IsFalse(pantalla.CuentaAbierta);
            CollectionAssert.DoesNotContain(Recorrido(), T(ClavesTexto.CuentaAvatar));
        }

        [Test]
        public void AlDestruirLaPantallaYaNoEscucha()
        {
            Abrir();
            Destruir();
            cuentas.Abrir();
            Assert.IsFalse(pantalla.CuentaAbierta);
            cuentas.EntrarConGoogle().Wait();
            Assert.IsNotNull(cuentas.Usuario);
            Assert.IsFalse(pantalla.Toast.Visible);
        }

        // ───────────────────────── TalkBack ─────────────────────────

        [Test]
        public void TalkBack_ModalConElTituloPrimeroYDeVueltaAlAvatar()
        {
            Abrir();
            CollectionAssert.Contains(Recorrido(), "Tu cuenta");
            pantalla.TocarAvatar();
            var acc = pantalla.Accesibilidad;
            Assert.IsTrue(acc.Modal);
            Assert.AreSame(pantalla.Cuenta.Titulo, acc.Enfocado);
            CollectionAssert.AreEqual(new[] { "Entra en PriceMap", T(ClavesTexto.CuentaPara), "Continuar con Google", "Ahora no" }, Recorrido());
            ContestarFallo(ErroresCuenta.SinConexion);
            pantalla.Cuenta.PulsarEntrar();
            CollectionAssert.AreEqual(new[] { "Entra en PriceMap", "Necesitas conexión para entrar.", "Reintentar", "Ahora no" }, Recorrido());
            cuentas.AhoraNo();
            Assert.IsFalse(acc.Modal);
            Assert.IsNull(acc.Exclusivo);
            Assert.AreSame(pantalla.Buscador.ZonaAvatar, acc.Enfocado);
            CollectionAssert.Contains(Recorrido(), T(ClavesTexto.TemaCambiar));
        }

        [Test]
        public void TalkBack_LosNodosYElDobleToque()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Abrir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            Assert.AreEqual("Sin sesión", Nodo(pantalla.Buscador.ZonaAvatar).value);
            Activar(pantalla.Buscador.ZonaAvatar);
            Assert.IsTrue(pantalla.CuentaAbierta);
            foreach (var n in AssistiveSupport.activeHierarchy.rootNodes)
                Assert.AreEqual(n.label == "Continuar con Google" || n.label == "Ahora no" ? AccessibilityRole.Button : AccessibilityRole.StaticText, n.role, n.label);
            Activar(pantalla.Cuenta.AhoraNo);
            Assert.IsFalse(pantalla.CuentaAbierta);
            Activar(pantalla.Buscador.ZonaAvatar);
            Activar(pantalla.Cuenta.Google);
            Assert.IsNotNull(cuentas.Usuario);
            Assert.AreEqual("ana@example.com", Nodo(pantalla.Buscador.ZonaAvatar).value);
        }

        [Test]
        public void TalkBack_AlEmpezarAEntrar_LoDiceYCambiaElBoton()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Abrir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            pantalla.TocarAvatar();
            var respuesta = ContestarDespues();
            pantalla.Cuenta.PulsarEntrar();
            Assert.AreEqual("Entrando…", Nodo(pantalla.Cuenta.Google).label);
            Assert.AreEqual(1, anuncios.Count(a => a == "Entrando…"));
            respuesta.SetResult(Resultado<UsuarioCuenta>.Fallo(ErroresCuenta.SinConexion));
            Assert.AreEqual("Reintentar", Nodo(pantalla.Cuenta.Reintentar).label);
            Assert.AreEqual(1, anuncios.Count(a => a == "Entrando…"), "Solo al empezar.");
            respuesta = ContestarDespues();
            pantalla.Cuenta.PulsarEntrar();
            Assert.AreEqual("Entrando…", Nodo(pantalla.Cuenta.Reintentar).label);
            Assert.AreEqual(2, anuncios.Count(a => a == "Entrando…"));
            respuesta.SetResult(Resultado<UsuarioCuenta>.Fallo(ErroresCuenta.Cancelado));
            Assert.AreEqual("Continuar con Google", Nodo(pantalla.Cuenta.Google).label);
        }

        [Test]
        public void TalkBack_LaSesionGuardadaLlegaDespues_ElAvatarLaDice()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            Abrir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            Assert.AreEqual("Sin sesión", Nodo(pantalla.Buscador.ZonaAvatar).value);
            google.Poner(CuentasFalsas.Ana);
            Assert.AreEqual("ana@example.com", Nodo(pantalla.Buscador.ZonaAvatar).value);
            google.Poner(null);
            Assert.AreEqual("Sin sesión", Nodo(pantalla.Buscador.ZonaAvatar).value);
        }

        [Test]
        public void TalkBack_ElSaludoVaConElFocoYNoAntes()
        {
            ComponerConPanel();
            pantalla.TocarAvatar();
            anuncios.Clear();
            pantalla.Cuenta.PulsarEntrar();
            Assert.IsTrue(pantalla.Toast.Visible);
            CollectionAssert.DoesNotContain(anuncios, "Hola, Ana", "Con panel el foco se mueve en el siguiente pase: el saludo, con él.");
            Assert.AreSame(pantalla.Buscador.ZonaAvatar, pantalla.Accesibilidad.Enfocado);
        }

        AccessibilityNode Nodo(VisualElement elemento)
        {
            var nodo = pantalla.Accesibilidad.NodoDe(elemento);
            Assert.IsNotNull(nodo, elemento.name + " no está en el recorrido.");
            return nodo;
        }

        /// <summary>El doble toque de TalkBack (como en PruebasBienvenida): el evento no se puede lanzar desde fuera.</summary>
        void Activar(VisualElement elemento)
        {
            const System.Reflection.BindingFlags Campos = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var campo = typeof(AccessibilityNode).GetFields(Campos).FirstOrDefault(f =>
                typeof(Func<bool>).IsAssignableFrom(f.FieldType) && f.Name.IndexOf("invoked", StringComparison.OrdinalIgnoreCase) >= 0);
            if (campo == null) Assert.Inconclusive("Esta versión de Unity no deja leer el evento invoked del nodo.");
            Assert.IsTrue(((Func<bool>)campo.GetValue(Nodo(elemento)))());
        }

        static void Tocar(VisualElement elemento, Clickable toque)
        {
            Assert.AreSame(elemento, toque.target, elemento.name);
            var invocar = typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            using var e = PointerUpEvent.GetPooled();
            invocar.Invoke(toque, new object[] { e });
        }

        [Test]
        public void ConElDedo_CadaBotonHaceLoSuyo()
        {
            Abrir();
            Tocar(pantalla.Buscador.Avatar, pantalla.Buscador.ToqueAvatar);
            Assert.IsTrue(pantalla.CuentaAbierta);
            Assert.IsFalse(pantalla.BuscadorAbierto, "El toque del avatar no despliega la hoja.");
            ContestarFallo(ErroresCuenta.Desconocido);
            Tocar(pantalla.Cuenta.Google, pantalla.Cuenta.Google.clickable);
            Assert.AreEqual(1, google.Peticiones);
            Tocar(pantalla.Cuenta.Reintentar, pantalla.Cuenta.Reintentar.clickable);
            Assert.AreEqual(2, google.Peticiones);
            Tocar(pantalla.Cuenta.AhoraNo, pantalla.Cuenta.AhoraNo.clickable);
            Assert.IsFalse(pantalla.CuentaAbierta);
        }

        // ───────────────────────── Textos y aspecto ─────────────────────────

        static string Regla(string selector, string fichero = "Assets/UI/PantallaMapa.uss")
        {
            string uss = File.ReadAllText(fichero).Replace("\r\n", "\n");
            var m = Regex.Match(uss, @"(^|\n)" + selector + @"\s*\{(?<c>[^}]*)\}");
            Assert.IsTrue(m.Success, "Sin regla " + selector);
            return m.Groups["c"].Value;
        }

        [Test]
        public void ElAvisoDeLaBienvenidaDiceQuienGuardaLaCuenta()
        {
            StringAssert.Contains("Si creas una cuenta, Google (Firebase) guarda tu nombre, tu correo y tu foto.", T(ClavesTexto.AvisoGuarda));
            var claves = ClavesTexto.Todas.Where(c => c.StartsWith("cuenta.")).ToList();
            Assert.AreEqual(13, claves.Count);
            foreach (var c in claves) Assert.IsNotEmpty(T(c), c);
        }

        [Test]
        public void ElBotonDeGoogleEsElOficialBlancoEnLosDosTemas()
        {
            var claro = PruebasContraste.Tema(false);
            var oscuro = PruebasContraste.Tema(true);
            foreach (var (nombre, hex) in new[] { ("google-fondo", "FFFFFF"), ("google-borde", "747775"), ("google-texto", "1F1F1F") })
            {
                Assert.AreEqual(hex, ColorUtility.ToHtmlStringRGB(claro[nombre]), nombre);
                Assert.AreEqual(claro[nombre], oscuro[nombre], nombre + " es igual en oscuro.");
            }
            StringAssert.Contains("color: var(--color-google-texto);", Regla(@"\.cuenta__google-texto"));
            StringAssert.Contains("background-color: var(--color-google-fondo);", Regla(@"\.cuenta__google:active", "Assets/UI/PantallaMapa.uss"));
            CollectionAssert.AreEqual(new[] { "EA4335", "4285F4", "FBBC05", "34A853" },
                IconoGoogle.Trozos.Select(t => ColorUtility.ToHtmlStringRGB(t.color)));
            Assert.IsTrue(IconoGoogle.Ordenes.All(o => o.Count > 3));
            cuentas.Abrir();
            var hoja = new HojaCuenta(cuentas);
            Assert.IsNotNull(hoja.Google.Q<IconoGoogle>());
            Assert.AreEqual("Continuar con Google", hoja.TextoGoogle.text);
            Assert.IsNull(hoja.Query<Button>().ToList().FirstOrDefault(b => b.text.Contains("correo")), "El correo es la parte 2.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CumpleElContraste(bool oscuro)
        {
            var c = PruebasContraste.Tema(oscuro);
            float Contraste(string a, string b) => PruebasContraste.Contraste(c[a], c[b]);
            foreach (var fondo in new[] { "surface", "bg" })
            {
                Assert.GreaterOrEqual(Contraste("text", fondo), 4.5f, "título sobre " + fondo);
                Assert.GreaterOrEqual(Contraste("muted", fondo), 4.5f, "texto sobre " + fondo);
                Assert.GreaterOrEqual(Contraste("accent-text", fondo), 4.5f, "«Ahora no» sobre " + fondo);
                if (!oscuro) Assert.GreaterOrEqual(Contraste("google-borde", fondo), 3f, "borde del botón de Google sobre " + fondo);
                else Assert.GreaterOrEqual(Contraste("google-fondo", fondo), 3f, "botón de Google sobre " + fondo);
            }
            Assert.GreaterOrEqual(Contraste("text", "accent-soft"), 4.5f, "aviso del error");
            Assert.GreaterOrEqual(Contraste("google-texto", "google-fondo"), 4.5f);
            Assert.GreaterOrEqual(Contraste("google-texto", "google-pulsado"), 4.5f);
            Assert.GreaterOrEqual(Contraste("accent-ink", "accent"), 4.5f, "letra del avatar y «Reintentar»");
        }

        [Test]
        public void Al200_ElTextoSeDesplazaYLosBotonesCrecen()
        {
            cuentas.Abrir();
            var hoja = new HojaCuenta(cuentas);
            Assert.IsInstanceOf<ScrollView>(hoja.Pagina);
            Assert.IsFalse(hoja.Pagina.Contains(hoja.Google), "Los botones se ven siempre.");
            StringAssert.Contains("min-height: var(--bienvenida-boton-alto);", Regla(@"\.cuenta__google:active"));
            StringAssert.Contains("min-height: var(--bienvenida-boton-alto);", Regla(@"\.explicacion-ubicacion__boton\.cuenta__boton"));
            StringAssert.Contains("max-height: 100%;", Regla(@"\.cuenta__tarjeta"));
            foreach (var l in new[] { @"\.cuenta__aviso", @"\.cuenta__google-texto" }) StringAssert.Contains("white-space: normal;", Regla(l));
            hoja.PonerZonaSegura(1f, 30f, 2f, 20f);
            Assert.AreEqual(30f, hoja.style.paddingTop.value.value);
            Assert.AreEqual(20f, hoja.Tarjeta.style.paddingBottom.value.value);
            StringAssert.Contains("left: -6px;", Regla(@"\.buscador__avatar-zona"), "48 dp: 40 del círculo, 4 por lado.");
        }

        // ───────────────────────── Firebase Auth ─────────────────────────

        class AuthFalso : IAccesoAuth
        {
            public UsuarioCuenta guardado;
            public Func<string, Task<UsuarioCuenta>> alEntrar;
            public Action alCambiar;
            /// <summary>Firebase ya ha entrado y avisa (StateChanged), antes de que siga quien espera a Entrar.</summary>
            public void EntrarYAvisar() { guardado = CuentasFalsas.Ana; alCambiar(); }
            public int escuchas, entradas, salidas;
            public string token;
            public UsuarioCuenta Usuario => guardado;
            public void Escuchar(Action alCambiar) { escuchas++; this.alCambiar = alCambiar; }
            public Task<UsuarioCuenta> Entrar(string idToken)
            {
                entradas++;
                token = idToken;
                return alEntrar != null ? alEntrar(idToken) : Task.FromResult(guardado = CuentasFalsas.Ana);
            }
            public void Salir() { salidas++; guardado = null; alCambiar?.Invoke(); }
        }

        class SelectorFalso : ISelectorGoogle
        {
            public Func<CancellationToken, Task<RespuestaGoogle>> contestar = _ => Task.FromResult(new RespuestaGoogle(EstadoGoogle.Hecha, "token-secreto"));
            public int peticiones;
            public Task<RespuestaGoogle> Pedir(CancellationToken ct) { peticiones++; return contestar(ct); }
        }

        class HiloFalso : SynchronizationContext
        {
            readonly Queue<Action> pendientes = new Queue<Action>();
            public override void Post(SendOrPostCallback d, object state) => pendientes.Enqueue(() => d(state));
            public void Vaciar() { while (pendientes.Count > 0) pendientes.Dequeue()(); }
        }

        AuthFalso auth;
        SelectorFalso selector;
        bool conexion;

        CuentasFirebase Firebase()
        {
            auth = auth ?? new AuthFalso();
            selector = new SelectorFalso();
            conexion = true;
            return new CuentasFirebase(servidor, auth, selector, () => conexion);
        }

        [TearDown]
        public void OlvidarAuth() => auth = null;

        [Test]
        public void Firebase_Entra_UsuarioYAviso()
        {
            var f = Firebase();
            int avisos = 0;
            f.Cambiado += () => avisos++;
            Assert.AreEqual(1, auth.escuchas);
            var r = f.EntrarConGoogle(CancellationToken.None).Result;
            Assert.IsTrue(r.Ok);
            Assert.AreEqual("token-secreto", auth.token);
            Assert.AreSame(CuentasFalsas.Ana, f.Usuario);
            auth.alCambiar(); // el aviso del SDK llega después
            Assert.AreEqual(1, avisos, "El mismo usuario no se avisa dos veces.");
            auth.guardado = null;
            auth.alCambiar();
            Assert.IsNull(f.Usuario);
            Assert.AreEqual(2, avisos);
        }

        [Test]
        public void Firebase_AlEstarListoLeeLaSesionGuardada()
        {
            servidor.Estado = EstadoServidor.Iniciando;
            auth = new AuthFalso { guardado = CuentasFalsas.Ana };
            var f = Firebase();
            Assert.AreEqual(0, auth.escuchas, "No se toca Firebase hasta que está listo.");
            Assert.IsNull(f.Usuario);
            int avisos = 0;
            f.Cambiado += () => avisos++;
            servidor.Poner(EstadoServidor.Listo);
            servidor.Poner(EstadoServidor.Listo);
            Assert.AreSame(CuentasFalsas.Ana, f.Usuario);
            Assert.AreEqual((1, 1), (avisos, auth.escuchas));
        }

        [TestCase(EstadoServidor.Iniciando)]
        [TestCase(EstadoServidor.NoDisponible)]
        public void Firebase_SinServidor_NoDisponibleSinPedirNada(EstadoServidor estado)
        {
            servidor.Estado = estado;
            var f = Firebase();
            Assert.AreEqual(ErroresCuenta.NoDisponible, f.EntrarConGoogle(CancellationToken.None).Result.Error);
            Assert.AreEqual((0, 0, 0), (selector.peticiones, auth.entradas, auth.escuchas));
        }

        [Test]
        public void Firebase_SinConexion_NoAbreElSelector()
        {
            var f = Firebase();
            conexion = false;
            Assert.AreEqual(ErroresCuenta.SinConexion, f.EntrarConGoogle(CancellationToken.None).Result.Error);
            Assert.AreEqual(0, selector.peticiones);
        }

        [Test]
        public void Firebase_LoQueContestaGoogle()
        {
            var f = Firebase();
            selector.contestar = _ => Task.FromResult(new RespuestaGoogle(EstadoGoogle.Cancelada));
            Assert.AreEqual(ErroresCuenta.Cancelado, f.EntrarConGoogle(CancellationToken.None).Result.Error);

            selector.contestar = _ => Task.FromResult(new RespuestaGoogle(EstadoGoogle.NoDisponible, motivo: "sin default_web_client_id"));
            LogAssert.Expect(LogType.Warning, "PriceMap: Google no ha dado la cuenta (cuenta-no-disponible, sin default_web_client_id).");
            Assert.AreEqual(ErroresCuenta.NoDisponible, f.EntrarConGoogle(CancellationToken.None).Result.Error);

            selector.contestar = _ => Task.FromResult(new RespuestaGoogle(EstadoGoogle.Fallo, motivo: "X"));
            LogAssert.Expect(LogType.Warning, "PriceMap: Google no ha dado la cuenta (cuenta-desconocido, X).");
            Assert.AreEqual(ErroresCuenta.Desconocido, f.EntrarConGoogle(CancellationToken.None).Result.Error);

            selector.contestar = _ => { conexion = false; return Task.FromResult(new RespuestaGoogle(EstadoGoogle.Fallo, motivo: "X")); };
            LogAssert.Expect(LogType.Warning, "PriceMap: Google no ha dado la cuenta (cuenta-sin-conexion, X).");
            Assert.AreEqual(ErroresCuenta.SinConexion, f.EntrarConGoogle(CancellationToken.None).Result.Error, "Se ha ido mientras tanto.");
            Assert.AreEqual(0, auth.entradas);
        }

        [Test]
        public void Firebase_LosErroresNoLlevanDatosAlRegistro()
        {
            var f = Firebase();
            auth.alEntrar = t => Task.FromException<UsuarioCuenta>(new FalloAuth("NetworkRequestFailed", new Exception(t + " ana@example.com")));
            LogAssert.Expect(LogType.Warning, "PriceMap: no se pudo entrar en la cuenta (cuenta-sin-conexion, NetworkRequestFailed).");
            Assert.AreEqual(ErroresCuenta.SinConexion, f.EntrarConGoogle(CancellationToken.None).Result.Error);
            auth.alEntrar = t => Task.FromException<UsuarioCuenta>(new Exception(t + " ana@example.com"));
            LogAssert.Expect(LogType.Warning, "PriceMap: no se pudo entrar en la cuenta (cuenta-desconocido, Exception).");
            Assert.AreEqual(ErroresCuenta.Desconocido, f.EntrarConGoogle(CancellationToken.None).Result.Error);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Firebase_CanceladaConElSelectorOConFirebaseContestando_SinSesion()
        {
            var f = Firebase();
            using (var cts = new CancellationTokenSource())
            {
                selector.contestar = _ => { cts.Cancel(); throw new OperationCanceledException(); };
                Assert.AreEqual(ErroresCuenta.Cancelado, f.EntrarConGoogle(cts.Token).Result.Error);
                Assert.AreEqual(0, auth.entradas);
            }
            using (var cts = new CancellationTokenSource())
            {
                selector.contestar = _ => Task.FromResult(new RespuestaGoogle(EstadoGoogle.Hecha, "t"));
                auth.alEntrar = _ => { cts.Cancel(); return Task.FromResult(auth.guardado = CuentasFalsas.Ana); };
                Assert.AreEqual(ErroresCuenta.Cancelado, f.EntrarConGoogle(cts.Token).Result.Error);
                Assert.AreEqual(1, auth.salidas, "Dijo «Ahora no»: no se queda dentro.");
                Assert.IsNull(f.Usuario);
            }
        }

        [Test]
        public void Firebase_ElAvisoLlegaAntesQueLaRespuesta_LaSesionSePublicaUnaVezAlAcabar()
        {
            var f = Firebase();
            int avisos = 0;
            f.Cambiado += () => avisos++;
            var lenta = new TaskCompletionSource<UsuarioCuenta>();
            auth.alEntrar = _ => lenta.Task;
            var entrar = f.EntrarConGoogle(CancellationToken.None);
            auth.EntrarYAvisar();
            Assert.IsNull(f.Usuario, "Aún no ha acabado.");
            lenta.SetResult(CuentasFalsas.Ana);
            Assert.IsTrue(entrar.Result.Ok);
            Assert.AreSame(CuentasFalsas.Ana, f.Usuario);
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Firebase_FallaAlEntrar_YLaSiguientePuede()
        {
            var f = Firebase();
            auth.alEntrar = _ => Task.FromException<UsuarioCuenta>(new FalloAuth("Failure"));
            LogAssert.Expect(LogType.Warning, new Regex("cuenta-desconocido, Failure"));
            Assert.IsFalse(f.EntrarConGoogle(CancellationToken.None).Result.Ok);
            auth.alEntrar = null;
            Assert.IsTrue(f.EntrarConGoogle(CancellationToken.None).Result.Ok);
        }

        [Test]
        public void Firebase_UnaCanceladaQueTarda_SaleAntesDeQueEntreLaSiguiente()
        {
            var f = Firebase();
            var vistos = new List<UsuarioCuenta>();
            f.Cambiado += () => vistos.Add(f.Usuario);
            var lenta = new TaskCompletionSource<UsuarioCuenta>();
            auth.alEntrar = _ => lenta.Task;
            using (var cts = new CancellationTokenSource())
            {
                var primera = f.EntrarConGoogle(cts.Token);
                cts.Cancel();
                auth.alEntrar = null;
                var segunda = f.EntrarConGoogle(CancellationToken.None);
                Assert.AreEqual(1, auth.entradas, "La segunda espera.");
                auth.EntrarYAvisar();
                lenta.SetResult(CuentasFalsas.Ana);
                Assert.AreEqual(ErroresCuenta.Cancelado, primera.Result.Error);
                Assert.IsTrue(segunda.Result.Ok);
                Assert.AreEqual((2, 1), (auth.entradas, auth.salidas));
                CollectionAssert.AreEqual(new[] { CuentasFalsas.Ana }, vistos, "La de la cancelada no se ve.");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Firebase_DosCanceladas_LaPrimeraAcabaTarde_SinSesion(bool enElSelector)
        {
            var f = Firebase();
            int avisos = 0;
            f.Cambiado += () => avisos++;
            var lenta = new TaskCompletionSource<UsuarioCuenta>();
            auth.alEntrar = _ => lenta.Task;
            using (var cts1 = new CancellationTokenSource())
            using (var cts2 = new CancellationTokenSource())
            {
                var primera = f.EntrarConGoogle(cts1.Token);
                cts1.Cancel();
                var elegir = new TaskCompletionSource<RespuestaGoogle>();
                if (enElSelector) selector.contestar = _ => elegir.Task;
                var segunda = f.EntrarConGoogle(cts2.Token);
                cts2.Cancel();
                elegir.SetCanceled();
                auth.EntrarYAvisar();
                lenta.SetResult(CuentasFalsas.Ana);
                Assert.AreEqual(ErroresCuenta.Cancelado, primera.Result.Error);
                Assert.AreEqual(ErroresCuenta.Cancelado, segunda.Result.Error);
                Assert.IsNull(f.Usuario);
                Assert.IsNull(auth.guardado);
                Assert.AreEqual((1, 1, 0), (auth.entradas, auth.salidas, avisos));
            }
        }

        [Test]
        public void Firebase_ConElServicio_CanceladaYReabiertaParaUnaAccion_LaAccionNoSeHace()
        {
            var f = Firebase();
            cuentas = new ServicioCuentas(f, servidor, prefs, (_, __) => espera.Task);
            int sesiones = 0, hechas = 0;
            cuentas.SesionCambiada += () => sesiones++;
            var lenta = new TaskCompletionSource<UsuarioCuenta>();
            auth.alEntrar = _ => lenta.Task;
            cuentas.Abrir();
            var entrar = cuentas.EntrarConGoogle();
            cuentas.AhoraNo();
            cuentas.ExigirCuenta(() => hechas++);
            auth.EntrarYAvisar();
            lenta.SetResult(CuentasFalsas.Ana);
            Assert.IsTrue(entrar.Wait(5000));
            Assert.IsNull(cuentas.Usuario);
            Assert.AreEqual((0, 0, 1), (hechas, sesiones, auth.salidas));
            Assert.AreEqual(EntradaCuenta.Hoja, cuentas.Entrada, "La nueva sigue abierta.");
            auth.alEntrar = null;
            cuentas.EntrarConGoogle().Wait();
            Assert.AreEqual((1, 1), (hechas, sesiones));
        }

        // ───────────────────────── Selector de Google ─────────────────────────

        class PuenteFalso : IPuenteCuenta
        {
            public readonly Queue<int> estados = new Queue<int>();
            public readonly List<string> llamadas = new List<string>();
            public int Pedir() { llamadas.Add("pedir"); return 7; }
            public int Estado(int id) { Assert.AreEqual(7, id); return estados.Dequeue(); }
            public string Token(int id) { Assert.AreEqual(7, id); return "token"; }
            public string Motivo(int id) { Assert.AreEqual(7, id); return "motivo"; }
            public void Cancelar(int id) { Assert.AreEqual(7, id); llamadas.Add("cancelar"); }
            public void Liberar(int id) { Assert.AreEqual(7, id); llamadas.Add("liberar"); }
        }

        static Task UnFotograma(CancellationToken ct) => ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask;

        // Los estados de PuenteCuenta.java (Plugin_… comprueba que son los mismos números)
        [TestCase(1, EstadoGoogle.Hecha, "token", null)]
        [TestCase(2, EstadoGoogle.Cancelada, null, null)]
        [TestCase(3, EstadoGoogle.Fallo, null, "motivo")]
        [TestCase(4, EstadoGoogle.NoDisponible, null, "motivo")]
        [TestCase(-1, EstadoGoogle.Fallo, null, "motivo")]
        public void Selector_CadaEstadoDelPuente_YAlAcabarSoloLibera(int estado, object esperado, string token, string motivo)
        {
            var puente = new PuenteFalso();
            foreach (int e in new[] { 0, 0, estado }) puente.estados.Enqueue(e);
            int fotogramas = 0;
            var r = new SelectorGoogleAndroid(puente, ct => { fotogramas++; return UnFotograma(ct); }).Pedir(CancellationToken.None).Result;
            Assert.AreEqual((esperado, token, motivo), ((object)r.Estado, r.IdToken, r.Motivo));
            Assert.AreEqual(2, fotogramas);
            CollectionAssert.AreEqual(new[] { "pedir", "liberar" }, puente.llamadas);
        }

        [Test]
        public void Selector_AlCancelar_CancelaYLibera()
        {
            var puente = new PuenteFalso();
            for (int i = 0; i < 5; i++) puente.estados.Enqueue(0);
            using (var cts = new CancellationTokenSource())
            {
                int fotogramas = 0;
                var pedir = new SelectorGoogleAndroid(puente, ct =>
                {
                    if (++fotogramas == 2) cts.Cancel();
                    return UnFotograma(ct);
                }).Pedir(cts.Token);
                Assert.IsTrue(pedir.IsCanceled);
            }
            CollectionAssert.AreEqual(new[] { "pedir", "cancelar", "liberar" }, puente.llamadas);
        }

        [Test]
        public void Selector_SinAndroid_NoDisponible() =>
            Assert.AreEqual(EstadoGoogle.NoDisponible, new SelectorGoogleAndroid().Pedir(CancellationToken.None).Result.Estado);

        [Test]
        public void Firebase_LosAvisosDeOtroHiloVuelvenAlPrincipal()
        {
            var hilo = new HiloFalso();
            SynchronizationContext.SetSynchronizationContext(hilo);
            var f = Firebase();
            SynchronizationContext.SetSynchronizationContext(null); // otro hilo
            auth.guardado = CuentasFalsas.Ana;
            auth.alCambiar();
            Assert.IsNull(f.Usuario);
            hilo.Vaciar();
            Assert.IsNotNull(f.Usuario);
        }

        // Por reflexión: las pruebas tampoco referencian Firebase
        static Type Tipo(string ensamblado, string nombre) =>
            typeof(CuentasFirebase).Assembly.GetReferencedAssemblies().Where(a => a.Name == ensamblado)
                .Select(System.Reflection.Assembly.Load).Single().GetType(nombre, throwOnError: true);

        [TestCase("NetworkRequestFailed", ErroresCuenta.SinConexion)]
        [TestCase("Cancelled", ErroresCuenta.Cancelado)]
        [TestCase("InvalidCredential", ErroresCuenta.Desconocido)]
        [TestCase("OperationNotAllowed", ErroresCuenta.Desconocido)]
        public void Acceso_LosErroresDelSdk_SueltosOEnvueltos_LleganConSuCodigo(string nombre, string propio)
        {
            int codigo = (int)Enum.Parse(Tipo("Firebase.Auth", "Firebase.Auth.AuthError"), nombre);
            var delSdk = (Exception)Activator.CreateInstance(Tipo("Firebase.App", "Firebase.FirebaseException"), codigo, "x");
            foreach (var e in new[] { delSdk, new AggregateException(new AggregateException(delSdk)) })
            {
                var traducido = AccesoAuth.Traducido(e);
                Assert.AreEqual(nombre, ((FalloAuth)traducido).Codigo);
                Assert.AreEqual(propio, CodigosAuth.De(traducido));
            }
            var otro = new InvalidOperationException();
            Assert.AreSame(otro, AccesoAuth.Traducido(otro));
            Assert.AreEqual(ErroresCuenta.Cancelado, CodigosAuth.De(new OperationCanceledException()));
        }

        [Test]
        public void SoloElAdaptadorTocaFirebaseAuth()
        {
            var con = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f).Contains("FirebaseAuth.DefaultInstance")).Select(Path.GetFileName);
            CollectionAssert.AreEqual(new[] { "AccesoAuth.cs" }, con);
        }

        // ───────────────────────── Android ─────────────────────────

        [Test]
        public void Plugin_CredentialManagerEnElHiloDeLaInterfazYSinRegistro()
        {
            string java = File.ReadAllText("Assets/Plugins/Android/PuenteCuenta.java");
            StringAssert.Contains("package com.pricemap.cuenta;", java);
            StringAssert.Contains("new GetSignInWithGoogleOption.Builder(cliente)", java);
            StringAssert.Contains("\"default_web_client_id\"", java);
            StringAssert.Contains("actividad.runOnUiThread(", java);
            StringAssert.Contains("GoogleIdTokenCredential.createFrom(", java);
            StringAssert.Contains("p.senal.cancel()", java);
            foreach (var m in new[] { "int pedir()", "int estado(int id)", "String token(int id)", "String motivo(int id)", "void cancelar(int id)", "void liberar(int id)" })
                StringAssert.Contains("public static " + m, java);
            StringAssert.DoesNotContain("android.util.Log", java, "Ni la cuenta ni el token van al registro.");
            string cs = File.ReadAllText("Assets/Scripts/Adaptadores/Firebase/SelectorGoogleAndroid.cs");
            string jni = File.ReadAllText("Assets/Scripts/Adaptadores/Firebase/PuenteCuentaAndroid.cs");
            StringAssert.Contains("\"com.pricemap.cuenta.PuenteCuenta\"", jni);
            StringAssert.Contains("public static final int FALLO = 3;", java);
            StringAssert.Contains("public static final int DESCONOCIDA = -1;", java);
            StringAssert.Contains("if (!mensaje.startsWith(\"[16]\") || mensaje.contains(\"cancelled by user\")) estado = CANCELADA;", java,
                "«[16] Cancelled by user.» es del usuario; los demás «[16]», no.");
            foreach (var (nombre, constante) in new[] { ("EnCurso", "EN_CURSO"), ("Hecha", "HECHA"), ("Cancelada", "CANCELADA"), ("NoDisponible", "NO_DISPONIBLE") })
                Assert.AreEqual(Regex.Match(java, constante + @" = (\d)").Groups[1].Value, Regex.Match(cs, nombre + @" = (\d)").Groups[1].Value, nombre);
            foreach (var metodo in new[] { "pedir", "estado", "token", "motivo", "cancelar", "liberar" }) StringAssert.Contains("\"" + metodo + "\"", jni);
        }

        [Test]
        public void Dependencias_LasDelXmlEstanEnLaPlantillaDeGradle()
        {
            var xml = File.ReadAllText("Assets/Editor/CuentaDependencies.xml");
            var gradle = File.ReadAllText("Assets/Plugins/Android/mainTemplate.gradle");
            var paquetes = Regex.Matches(xml, "spec=\"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            CollectionAssert.AreEqual(new[] { "androidx.credentials:credentials", "androidx.credentials:credentials-play-services-auth",
                "com.google.android.libraries.identity.googleid:googleid" }, paquetes.Select(p => p.Substring(0, p.LastIndexOf(':'))));
            foreach (var p in paquetes) StringAssert.Contains($"implementation '{p}'", gradle);
        }

        // ───────────────────────── Composición ─────────────────────────

        void ComponerConPanel()
        {
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PrepararInterfaz.RutaPanelSettings);
            documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PrepararInterfaz.RutaPantallaMapa);
            pantalla = objeto.AddComponent<PantallaMapa>();
            objeto.SetActive(true);
            Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), pantalla, new ServicioTema(prefs, new SistemaClaro()),
                new ServicioLugares(new Repositorio()), new ServicioFiltro(prefs), Ahora, cuentas: cuentas));
        }

        [Test]
        public void Componer_PasaLasCuentasALaPantalla()
        {
            ComponerConPanel();
            Assert.AreSame(cuentas, pantalla.Cuentas);
        }

        [Test]
        public void Arranque_SinFirebaseLasFalsasYConElLasDeVerdad()
        {
            var falsas = Arranque.Arranque.CrearCuentas(new ServidorNulo(), prefs);
            falsas.Abrir();
            falsas.EntrarConGoogle().Wait();
            Assert.AreSame(CuentasFalsas.Ana, falsas.Usuario, "Sin esperar a ningún servidor.");

            servidor.Estado = EstadoServidor.NoDisponible;
            var deVerdad = Arranque.Arranque.CrearCuentas(servidor, prefs);
            deVerdad.Abrir();
            deVerdad.EntrarConGoogle().Wait();
            Assert.AreEqual(ErroresCuenta.NoDisponible, deVerdad.Error);

            string codigo = File.ReadAllText("Assets/Scripts/Arranque/Arranque.cs");
            StringAssert.Contains("CapturaPruebas.SePide() ? CapturaPruebas.CuentasFalsas() : CrearCuentas(servidor, preferencias)", codigo);
            StringAssert.Contains("cuentas: cuentas, bienvenida: bienvenida))", codigo);
        }
    }
}
