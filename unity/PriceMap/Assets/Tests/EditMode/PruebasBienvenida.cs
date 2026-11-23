using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using PriceMap.Adaptadores.Gps;
using PriceMap.Adaptadores.Sistema;
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
    /// Bienvenida, privacidad y consentimientos (historia 3.1): cada fila de la matriz con la pantalla del mapa y la interfaz
    /// reales y los permisos falsos, TalkBack, los textos, el contraste, el adaptador de las notificaciones y la composición.
    /// </summary>
    public class PruebasBienvenida
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Usuario = new Coordenada(36.7790, -6.3540);
        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        // ───────────────────────── Dobles ─────────────────────────

        class PreferenciasFalsas : IPreferencias
        {
            public readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public readonly List<string> orden = new List<string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) { datos[clave] = valor; orden.Add(clave); }
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class UbicacionFalsa : IUbicacion
        {
            public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.SinPermiso;
            public Coordenada? Posicion { get; set; }
            public float? Rumbo => null;
            public event Action Cambiado;
            public bool PuedePedirPermiso { get; set; } = true;
            public int Permisos, Ajustes;
            public void PedirPermiso() => Permisos++;
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() => Ajustes++;
            public bool RutaActiva => false;
            public bool RutaSoloPrimerPlano { get; set; }
            public Resultado<bool> IniciarRuta(string textoNotificacion) => Resultado<bool>.Exito(false);
            public void ActualizarNotificacion(string textoNotificacion) { }
            public void TerminarRuta() { }
            public void Poner(EstadoUbicacion estado) { Estado = estado; Cambiado?.Invoke(); }
        }

        class NotificacionesFalsas : INotificaciones
        {
            public EstadoNotificaciones Estado { get; set; }
            public bool PuedePedir { get; set; } = true;
            public event Action Cambiado;
            public int Permisos;
            public void PedirPermiso() => Permisos++;
            public void Poner(EstadoNotificaciones estado) { Estado = estado; Cambiado?.Invoke(); }
        }

        class PantallaInicioFalsa : IPantallaInicio
        {
            public double? Segundos { get; set; } = 0;
            public readonly List<PuntoPantalla?> Idas = new List<PuntoPantalla?>();
            public void Irse(PuntoPantalla? destino) => Idas.Add(destino);
        }

        class MapaFalso : IMapa
        {
            public int Acercamientos;
            public bool VistaCargada => false;
            public void AcercarDesde(float factor, float segundos) => Acercamientos++;
            public readonly List<Coordenada> Centrados = new List<Coordenada>();
            public void Centrar(Coordenada destino) => Centrados.Add(destino);
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) => Centrados.Add(destino);
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

        static readonly Lugar BarPepe = new Lugar("bar-pepe", "Bar Pepe", "Bar", Plantilla.Hosteleria, Usuario, "Calle Ancha, 3");

        class Repositorio : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(Array.Empty<Lugar>());
        }

        // ───────────────────────── Montaje ─────────────────────────

        readonly List<GameObject> objetos = new List<GameObject>();
        SynchronizationContext contextoAntes;
        PreferenciasFalsas prefs;
        UbicacionFalsa gps;
        NotificacionesFalsas notificaciones;
        MapaFalso mapa;
        PantallaMapa pantalla;
        VisualElement arbol;
        ServicioBienvenida servicio;
        float reloj;

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            Textos.Forzados = T;
            prefs = new PreferenciasFalsas();
            gps = new UbicacionFalsa();
            notificaciones = new NotificacionesFalsas();
            mapa = new MapaFalso();
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (var o in objetos) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            objetos.Clear();
            Textos.Forzados = null;
            EscalaFuente.Forzada = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        /// <summary>Abre la app (como Componer), con las mismas preferencias y permisos de antes.</summary>
        PantallaBienvenida Abrir(ServicioInicio inicio = null)
        {
            var objeto = new GameObject("Pantalla de prueba");
            objetos.Add(objeto);
            pantalla = objeto.AddComponent<PantallaMapa>();
            arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var lugares = new ServicioLugares(new Repositorio());
            lugares.Cargar();
            var ubicacion = new ServicioUbicacion(gps, prefs);
            servicio = new ServicioBienvenida(prefs, ubicacion, notificaciones);
            pantalla.Inicializar(mapa, tema, lugares, Ahora, arbol, filtro, ubicacion,
                busqueda: new ServicioBusqueda(lugares, mapa, Ahora), inicio: inicio, bienvenida: servicio);
            tema.Iniciar();
            filtro.Iniciar();
            ubicacion.Iniciar();
            pantalla.Buscador.Medir(800f);
            // Cada vez que se mira el reloj ha pasado un segundo: el botón principal siempre responde
            if (pantalla.Bienvenida != null) pantalla.Bienvenida.Reloj = () => reloj += 1f;
            return pantalla.Bienvenida;
        }

        PantallaBienvenida AbrirEnPermisos()
        {
            var b = Abrir();
            servicio.Siguiente();
            servicio.Siguiente();
            return b;
        }

        List<string> Recorrido() => pantalla.Accesibilidad.Recorrido();

        static bool SeVe(VisualElement e) => e.style.display.value == DisplayStyle.Flex;

        // ───────────────────────── Matriz ─────────────────────────

        [Test]
        public void PrimerArranque_LaBienvenidaSaleAntesDelMapaYEnElPrimerPaso()
        {
            var b = Abrir();
            Assert.IsNotNull(b);
            Assert.IsTrue(pantalla.BienvenidaAbierta);
            Assert.IsFalse(pantalla.ExplicacionAbierta, "La de la 1.7 ya no sale al abrir.");
            Assert.AreEqual(0, servicio.Paso);
            Assert.AreEqual(T(ClavesTexto.BienvenidaTituloPrecios), b.Titulo.text);
            Assert.AreEqual("Siguiente", b.Principal.text);
            Assert.AreEqual(Visibility.Hidden, b.BotonAtras.style.visibility.value);
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)), "El mapa no se mueve.");
            Assert.AreEqual(PickingMode.Position, b.pickingMode);
            var raiz = arbol.Q("raiz");
            Assert.AreSame(raiz.Children().Last(), b.parent, "En el último hueco: encima de todo.");
            Assert.AreEqual("hueco-bienvenida", b.parent.name);
            Assert.AreEqual(0, gps.Permisos + notificaciones.Permisos, "No se pide nada hasta «Permitir».");
            Assert.IsEmpty(prefs.orden.Where(c => c.StartsWith("bienvenida.")), "Nada guardado aún.");
        }

        [Test]
        public void SiguienteArranque_MapaDirecto()
        {
            prefs.Guardar("bienvenida.vista", "1");
            Assert.IsNull(Abrir());
            Assert.IsFalse(pantalla.BienvenidaAbierta);
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            Assert.IsFalse(pantalla.Accesibilidad.Modal);
            Assert.IsEmpty(arbol.Q("hueco-bienvenida").Children());
            Assert.IsFalse(pantalla.ModalUbicacionAbierta);
        }

        [Test]
        public void CerrarAMitad_VuelveASalirDesdeElPrincipio()
        {
            Abrir();
            servicio.Siguiente();
            servicio.AhoraNoUbicacion();
            Assert.AreEqual(1, servicio.Paso);
            UnityEngine.Object.DestroyImmediate(pantalla.gameObject);
            Assert.IsNotNull(Abrir());
            Assert.AreEqual(0, servicio.Paso);
            Assert.IsNull(prefs.Leer("bienvenida.vista"));
            Assert.IsNull(prefs.Leer("bienvenida.ubicacion"));
        }

        [Test]
        public void PermitirUbicacion_LanzaElDialogoYLaTarjetaMarcaPermitido()
        {
            var b = AbrirEnPermisos();
            Assert.IsTrue(SeVe(b.Ubicacion.Permitir) && SeVe(b.Ubicacion.AhoraNo));
            Assert.IsFalse(SeVe(b.Ubicacion.FilaPermitido) || SeVe(b.Ubicacion.Despues));
            servicio.PermitirUbicacion();
            Assert.AreEqual(1, gps.Permisos);
            Assert.AreEqual(Consentimiento.SinContestar, servicio.Ubicacion, "Con el diálogo delante aún no se sabe.");
            gps.Poner(EstadoUbicacion.Buscando);
            Assert.AreEqual(Consentimiento.Permitido, servicio.Ubicacion);
            Assert.IsTrue(SeVe(b.Ubicacion.FilaPermitido));
            Assert.AreEqual("Permitido", b.Ubicacion.Permitido.text);
            Assert.IsFalse(SeVe(b.Ubicacion.Permitir) || SeVe(b.Ubicacion.AhoraNo) || SeVe(b.Ubicacion.Despues));
            servicio.PermitirUbicacion();
            Assert.AreEqual(1, gps.Permisos, "Ya está.");
            Assert.AreEqual(Consentimiento.SinContestar, servicio.Notificaciones, "La otra, como estaba.");
        }

        [Test]
        public void AhoraNo_SaleElTextoDeAjustesYLaAppSigue()
        {
            var b = AbrirEnPermisos();
            servicio.AhoraNoUbicacion();
            Assert.IsTrue(SeVe(b.Ubicacion.Despues));
            Assert.AreEqual("Puedes activarlo después en Ajustes.", b.Ubicacion.Despues.text);
            Assert.IsTrue(SeVe(b.Ubicacion.Permitir), "Android aún puede preguntar.");
            Assert.IsFalse(SeVe(b.Ubicacion.AhoraNo));
            Assert.AreEqual(0, gps.Permisos);

            b.PulsarPrincipal(); // «Empezar»
            Assert.IsFalse(pantalla.BienvenidaAbierta);
            Assert.AreEqual("1", prefs.Leer("bienvenida.vista"));
            Assert.AreEqual("ahora-no", prefs.Leer("bienvenida.ubicacion"));
            Assert.AreEqual("ahora-no", prefs.Leer("bienvenida.notificaciones"), "Lo no contestado cuenta como «Ahora no».");
            Assert.IsFalse(pantalla.ExplicacionAbierta, "No sale la de la 1.7.");
            pantalla.TocarUbicacion();
            Assert.IsTrue(pantalla.ExplicacionAbierta, "El botón de ubicación sigue con ella.");
        }

        class PermisoFalso : IPermisoNotificaciones
        {
            public int Version { get; set; } = 33;
            public bool Concedido { get; set; }
            public bool Explica { get; set; }
            public Action<bool?> Respuesta;
            public int Peticiones;
            public void Pedir(Action<bool?> respuesta) { Peticiones++; Respuesta = respuesta; }
        }

        [TestCase(26, true)]
        [TestCase(32, true)]
        [TestCase(33, false)]
        [TestCase(35, false)]
        public void Android12_LasNotificacionesSeDanPorConcedidasSinDialogo(int version, bool concedido)
        {
            var permiso = new PermisoFalso { Version = version };
            var n = new NotificacionesAndroid(permiso);
            int avisos = 0;
            n.Cambiado += () => avisos++;
            Assert.AreEqual(concedido ? EstadoNotificaciones.Concedido : EstadoNotificaciones.SinPermiso, n.Estado);
            n.PedirPermiso();
            Assert.AreEqual(concedido ? 0 : 1, permiso.Peticiones);
            Assert.AreEqual(concedido ? 1 : 0, avisos);

            var s = new ServicioBienvenida(prefs, new ServicioUbicacion(gps, prefs), n);
            Assert.AreEqual(concedido ? Consentimiento.Permitido : Consentimiento.SinContestar, s.Notificaciones);
            s.Terminar();
            Assert.AreEqual(concedido ? "permitido" : "ahora-no", prefs.Leer("bienvenida.notificaciones"));
        }

        [Test]
        public void AtrasEnElPaso2_VuelveAl1()
        {
            var b = Abrir();
            b.PulsarPrincipal();
            Assert.AreEqual(1, servicio.Paso);
            Assert.AreEqual(Visibility.Visible, b.BotonAtras.style.visibility.value);
            pantalla.TeclaAtras();
            Assert.AreEqual(0, servicio.Paso);
            Assert.AreEqual(T(ClavesTexto.BienvenidaTituloPrecios), b.Titulo.text);
            b.PulsarPrincipal();
            b.PulsarAtras(); // «‹»
            Assert.AreEqual(0, servicio.Paso);
        }

        [Test]
        public void AtrasEnElPaso1_NoHaceNadaNiSaleDeLaApp()
        {
            Abrir();
            Assert.IsTrue(pantalla.Atras(), "Gastado aquí.");
            Assert.IsTrue(pantalla.BienvenidaAbierta);
            Assert.AreEqual(0, servicio.Paso);
            Assert.IsFalse(Input.backButtonLeavesApp);
        }

        [TestCase(EstadoUbicacion.Buscando)]
        [TestCase(EstadoUbicacion.Activa)]
        [TestCase(EstadoUbicacion.Apagada)]
        public void UsuarioDeLa17_LaVeUnaVezConLaUbicacionYaPermitida(EstadoUbicacion estado)
        {
            gps.Estado = estado;
            var b = AbrirEnPermisos();
            Assert.IsNotNull(b);
            Assert.IsTrue(SeVe(b.Ubicacion.FilaPermitido), "También con la ubicación del móvil apagada.");
            b.PulsarPrincipal();
            Assert.AreEqual("permitido", prefs.Leer("bienvenida.ubicacion"), "Ya estaba permitida.");
            Assert.IsNull(Abrir(), "Una sola vez.");
        }

        [Test]
        public void UsuarioDeLa17_SinPermisoLaVeSinContestar()
        {
            var b = AbrirEnPermisos();
            Assert.IsTrue(SeVe(b.Ubicacion.Permitir) && SeVe(b.Ubicacion.AhoraNo));
        }

        // ───────────────────────── Pasos y aviso ─────────────────────────

        [Test]
        public void LosTresPasosYElAviso_UnaPaginaALaVezConSuTituloSusPuntosYSuBoton()
        {
            var b = Abrir();
            void Comprobar(int pagina, string titulo, string boton, bool enlace)
            {
                for (int i = 0; i < b.Paginas.Count; i++) Assert.AreEqual(i == pagina, SeVe(b.Paginas[i]), $"Página {i} en la {pagina}.");
                Assert.AreEqual(titulo, b.Titulo.text);
                Assert.IsTrue(b.Paginas[pagina].Contains(b.Titulo), "El título se desplaza con su página.");
                Assert.AreEqual(boton != null, SeVe(b.Principal));
                if (boton != null) Assert.AreEqual(boton, b.Principal.text);
                Assert.AreEqual(enlace, SeVe(b.LeerAviso));
                Assert.AreEqual(boton != null, SeVe(b.Puntos[0].parent), "Los puntos no salen en el aviso.");
                if (boton != null)
                    for (int i = 0; i < 3; i++) Assert.AreEqual(i == pagina, b.Puntos[i].ClassListContains(PantallaBienvenida.ClasePuntoActual));
            }
            Comprobar(0, "Precios reales de Sanlúcar, en el mapa", "Siguiente", false);
            servicio.AbrirAviso();
            Assert.IsFalse(servicio.AvisoAbierto, "Solo desde la privacidad.");
            b.PulsarPrincipal();
            Comprobar(1, "Tu privacidad", "Siguiente", true);
            servicio.AbrirAviso();
            Comprobar(PantallaBienvenida.PaginaAviso, "Aviso de privacidad", null, false);
            servicio.Siguiente();
            Assert.AreEqual(1, servicio.Paso, "Con el aviso abierto no se avanza.");
            Assert.IsTrue(pantalla.Atras());
            Comprobar(1, "Tu privacidad", "Siguiente", true);
            b.PulsarPrincipal();
            Comprobar(2, "Tú decides", "Empezar", false);
            servicio.Siguiente();
            Assert.AreEqual(2, servicio.Paso);
            Assert.IsTrue(b.Principal.enabledSelf, "«Empezar», siempre activo.");
        }

        [Test]
        public void ElAvisoLlevaAlResponsableYSuCorreoSePuedenSeleccionar()
        {
            var b = Abrir();
            Assert.AreEqual("Jose Antonio Segovia Sarmiento, autor de este Trabajo de Fin de Grado.", b.Responsable.text);
            Assert.AreEqual("jcoete37@gmail.com", b.Correo.text);
            foreach (var l in new[] { b.Responsable, b.Correo })
            {
                Assert.IsTrue(l.selection.isSelectable);
                Assert.AreEqual(PickingMode.Position, l.pickingMode);
                Assert.IsTrue(b.Paginas[PantallaBienvenida.PaginaAviso].Contains(l));
            }
        }

        [Test]
        public void ElHuecoDeLaEvaluacionEstaVacioEnElPaso3()
        {
            var b = Abrir();
            Assert.AreEqual("hueco-evaluacion", b.HuecoEvaluacion.name);
            Assert.IsTrue(b.Paginas[2].Contains(b.HuecoEvaluacion));
            Assert.AreEqual(0, b.HuecoEvaluacion.childCount, "Sin casilla hasta la 6.2.");
        }

        [Test]
        public void ElPrimerPasoLlevaElLogoDeLaPantallaDeInicioTambienEnOscuro()
        {
            var b = Abrir();
            Assert.IsNotNull(b.Paginas[0].Q("bienvenida-pin"));
            Assert.IsNotNull(b.Paginas[0].Q("bienvenida-nombre"));
            foreach (var imagen in new[] { "pin.png", "nombre-claro.png", "nombre-oscuro.png" })
                CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(PantallaInicioRecursos.CarpetaImagenes, imagen)),
                    File.ReadAllBytes("Assets/UI/Imagenes/" + imagen), imagen);
            var pin = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Imagenes/pin.png");
            Assert.AreEqual(960f / 1170f, (float)pin.width / pin.height, 0.01f, "Sin deformar al importarla.");
            StringAssert.Contains("url(\"Imagenes/nombre-claro.png\")", Regla(@"\.bienvenida__nombre"));
            StringAssert.Contains("url(\"Imagenes/nombre-oscuro.png\")", Regla(@"\.pm-raiz\.tema-oscuro \.bienvenida__nombre"));
        }

        // ───────────────────────── Consentimientos ─────────────────────────

        [Test]
        public void DenegarEnAndroid_SaleElTextoYPermitirSigueMientrasPuedaPreguntar()
        {
            var b = AbrirEnPermisos();
            servicio.PermitirUbicacion();
            gps.Poner(EstadoUbicacion.Denegado);
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Ubicacion);
            Assert.IsTrue(SeVe(b.Ubicacion.Despues) && SeVe(b.Ubicacion.Permitir));
            servicio.PermitirUbicacion();
            Assert.AreEqual(2, gps.Permisos);
            gps.PuedePedirPermiso = false; // «no volver a preguntar»
            gps.Poner(EstadoUbicacion.Denegado);
            Assert.AreEqual(Consentimiento.Bloqueado, servicio.Ubicacion);
            Assert.IsTrue(SeVe(b.Ubicacion.Despues));
            Assert.IsFalse(SeVe(b.Ubicacion.Permitir) || SeVe(b.Ubicacion.AhoraNo));
            b.PulsarPrincipal();
            Assert.AreEqual("ahora-no", prefs.Leer("bienvenida.ubicacion"));
        }

        [Test]
        public void Notificaciones_PermitirDenegarYBloquear()
        {
            var b = AbrirEnPermisos();
            servicio.AhoraNoNotificaciones();
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Notificaciones);
            servicio.PermitirNotificaciones();
            Assert.AreEqual(1, notificaciones.Permisos);
            notificaciones.Poner(EstadoNotificaciones.Denegado);
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Notificaciones);
            notificaciones.PuedePedir = false;
            notificaciones.Poner(EstadoNotificaciones.Denegado);
            Assert.AreEqual(Consentimiento.Bloqueado, servicio.Notificaciones);
            Assert.IsFalse(SeVe(b.Notificaciones.Permitir));
            notificaciones.Poner(EstadoNotificaciones.Concedido);
            Assert.IsTrue(SeVe(b.Notificaciones.FilaPermitido));
            b.PulsarPrincipal();
            Assert.AreEqual("permitido", prefs.Leer("bienvenida.notificaciones"));
        }

        [Test]
        public void AlVolverDeAjustes_LaTarjetaMarcaPermitido()
        {
            var b = AbrirEnPermisos();
            servicio.AhoraNoUbicacion();
            servicio.AhoraNoNotificaciones();
            gps.Estado = EstadoUbicacion.Buscando;             // activados en los Ajustes del móvil, sin avisar
            notificaciones.Estado = EstadoNotificaciones.Concedido;
            pantalla.LeerAccesibilidadSistema();               // la app vuelve delante (Arranque.OnApplicationFocus)
            Assert.IsTrue(SeVe(b.Ubicacion.FilaPermitido));
            Assert.IsTrue(SeVe(b.Notificaciones.FilaPermitido));
        }

        [Test]
        public void Terminar_GuardaLaVistaLoUltimoYNoVuelveAEscuchar()
        {
            AbrirEnPermisos();
            int terminada = 0, cambios = 0;
            servicio.Terminada += () => terminada++;
            servicio.ConsentimientoCambiado += () => cambios++;
            servicio.Terminar();
            servicio.Terminar();
            Assert.AreEqual(1, terminada);
            Assert.AreEqual("bienvenida.vista", prefs.orden.Last(), "Cortado antes, vuelve a salir.");
            gps.Poner(EstadoUbicacion.Buscando);
            notificaciones.Poner(EstadoNotificaciones.Concedido);
            servicio.Refrescar();
            Assert.AreEqual(0, cambios);
            Assert.IsFalse(servicio.Atras());
        }

        [Test]
        public void AlDestruirLaPantallaYaNoEscucha()
        {
            AbrirEnPermisos();
            UnityEngine.Object.DestroyImmediate(pantalla.gameObject);
            Assert.DoesNotThrow(() => { gps.Poner(EstadoUbicacion.Buscando); servicio.Atras(); servicio.Terminar(); });
        }

        [Test]
        public void AhoraNoYPermitirCerrandoElDialogo_SigueElAhoraNo()
        {
            AbrirEnPermisos();
            servicio.AhoraNoUbicacion();
            servicio.PermitirUbicacion();
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Ubicacion, "Con el diálogo delante.");
            gps.Poner(EstadoUbicacion.SinPermiso); // cerrado sin responder
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Ubicacion);
            servicio.AhoraNoNotificaciones();
            servicio.PermitirNotificaciones();
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Notificaciones);
            notificaciones.Poner(EstadoNotificaciones.SinPermiso);
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Notificaciones);
            Assert.AreEqual((1, 1), (gps.Permisos, notificaciones.Permisos));
            gps.Poner(EstadoUbicacion.Buscando);
            Assert.AreEqual(Consentimiento.Permitido, servicio.Ubicacion);
        }

        [Test]
        public void PulsacionesSeguidas_ElBotonPrincipalEsperaTrasCambiarDePagina()
        {
            var b = Abrir();
            float t = 100f;
            b.Reloj = () => t;
            b.PulsarPrincipal();
            Assert.AreEqual(1, servicio.Paso, "En la primera página responde al momento.");
            b.PulsarPrincipal();
            t += 0.39f;
            b.PulsarPrincipal();
            Assert.AreEqual(1, servicio.Paso, "Toques seguidos: no se salta la privacidad.");
            t += 0.02f;
            b.PulsarPrincipal();
            Assert.AreEqual(2, servicio.Paso);
            b.PulsarPrincipal();
            Assert.IsTrue(servicio.Pendiente, "Ni los permisos.");
            t += 0.3f;
            servicio.AhoraNoUbicacion(); // cambiar una tarjeta no es cambiar de página
            t += 0.11f;
            b.PulsarPrincipal();
            Assert.IsFalse(servicio.Pendiente);
        }

        // ───────────────────────── Con la pantalla del mapa ─────────────────────────

        [Test]
        public void ConLaBienvenida_NiFichasNiBotonesNiAvisos()
        {
            var b = Abrir();
            pantalla.AbrirFicha(BarPepe);
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsFalse(pantalla.PulsarLargoMapa(new PuntoPantalla(10f, 10f, 1f)));
            pantalla.TocarUbicacion();
            Assert.IsFalse(pantalla.ExplicacionAbierta);
            gps.RutaSoloPrimerPlano = true;
            pantalla.ActualizarCapas(1f);
            Assert.IsFalse(pantalla.AvisoAbierto, "El aviso de las notificaciones espera.");
            servicio.Terminar();
            pantalla.ActualizarCapas(1f);
            Assert.IsTrue(pantalla.AvisoAbierto);
        }

        [Test]
        public void LoCompartidoEsperaAQueAcabe()
        {
            AbrirEnPermisos();
            pantalla.RecibirCompartido("geo:36.77,-6.35");
            Assert.IsFalse(pantalla.AplicarCompartidoPendiente());
            Assert.IsTrue(pantalla.CompartidoPendiente);
            Assert.IsFalse(pantalla.FichaAbierta);
            servicio.Terminar();
            Assert.IsTrue(pantalla.AplicarCompartidoPendiente()); // lo hace Update
            Assert.AreEqual(new Coordenada(36.77, -6.35), pantalla.PuntoAbierto);
        }

        [Test]
        public void LaPantallaDeInicioNoEsperaNiALaPosicionNiAlMapaYElMapaNoSeAcerca()
        {
            double reloj = 100;
            var nativa = new PantallaInicioFalsa();
            gps.Estado = EstadoUbicacion.Buscando; // usuario de la 1.7: hay permiso, pero aún no posición
            var b = Abrir(new ServicioInicio(nativa, () => reloj));
            Assert.IsNotNull(b, "Ya puesta debajo: al fundirse se ve ella.");
            Assert.IsTrue(pantalla.InicioALaVista);
            void Fotograma(double s) { reloj += s; if (nativa.Segundos.HasValue) nativa.Segundos += s; pantalla.ActualizarCapas(1f); }
            Fotograma(0.9);
            Assert.IsEmpty(nativa.Idas, "El pin aún cae.");
            pantalla.TeclaAtras();
            Fotograma(0.2);
            CollectionAssert.AreEqual(new PuntoPantalla?[] { null }, nativa.Idas);
            Assert.AreEqual(0, mapa.Acercamientos);
            nativa.Segundos = null;
            Fotograma(0.5);
            Assert.IsFalse(pantalla.InicioALaVista);
            Assert.IsTrue(pantalla.BienvenidaAbierta);
            Assert.AreSame(b.Titulo, pantalla.Accesibilidad.Enfocado);
        }

        [Test]
        public void YaVista_LaPantallaDeInicioEsperaComoSiempre()
        {
            prefs.Guardar("bienvenida.vista", "1");
            double reloj = 100;
            var nativa = new PantallaInicioFalsa();
            Abrir(new ServicioInicio(nativa, () => reloj));
            reloj += 2; nativa.Segundos = 2;
            pantalla.ActualizarCapas(1f);
            Assert.IsEmpty(nativa.Idas, "Espera al mapa.");
        }

        [Test]
        public void ElCentradoAlAbrirNoCambia()
        {
            Abrir();
            gps.Posicion = Usuario;
            gps.Poner(EstadoUbicacion.Activa);
            pantalla.ActualizarCapas(1f);
            Assert.AreEqual(Usuario, mapa.Centrados.Last(), "Debajo de la bienvenida.");
        }

        // ───────────────────────── TalkBack ─────────────────────────

        [Test]
        public void TalkBack_ModalConElTituloDelPasoDePrimeroYSoloLoSuyo()
        {
            var b = Abrir();
            var acc = pantalla.Accesibilidad;
            Assert.IsTrue(acc.Modal);
            Assert.AreSame(b.Titulo, acc.Enfocado);
            CollectionAssert.AreEqual(new[]
            {
                T(ClavesTexto.BienvenidaTituloPrecios), T(ClavesTexto.BienvenidaMira), T(ClavesTexto.BienvenidaNegocios),
                T(ClavesTexto.BienvenidaGuia), "Paso 1 de 3", "Siguiente",
            }, Recorrido());

            b.PulsarPrincipal();
            Assert.AreSame(b.Titulo, acc.Enfocado);
            CollectionAssert.AreEqual(new[]
            {
                T(ClavesTexto.BienvenidaTituloPrivacidad), T(ClavesTexto.BienvenidaPrivacidadUbicacion), T(ClavesTexto.BienvenidaPrivacidadEnvios),
                T(ClavesTexto.BienvenidaPrivacidadAportes), T(ClavesTexto.BienvenidaPrivacidadCuenta), "Leer el aviso completo",
                "Paso 2 de 3", "Siguiente", "Atrás",
            }, Recorrido());

            servicio.AbrirAviso();
            Assert.AreSame(b.Titulo, acc.Enfocado);
            CollectionAssert.AreEqual(new[]
            {
                T(ClavesTexto.AvisoTitulo), T(ClavesTexto.AvisoDatosTitulo), T(ClavesTexto.AvisoDatos), T(ClavesTexto.AvisoSaleTitulo),
                T(ClavesTexto.AvisoSale), T(ClavesTexto.AvisoGuardaTitulo), T(ClavesTexto.AvisoGuarda), T(ClavesTexto.AvisoResponsableTitulo),
                T(ClavesTexto.AvisoResponsable), T(ClavesTexto.AvisoCorreo), "Atrás",
            }, Recorrido());
            servicio.Atras();
            Assert.AreSame(b.LeerAviso, acc.Enfocado, "Al cerrar el aviso, al enlace que lo abrió.");

            b.PulsarPrincipal();
            Assert.AreSame(b.Titulo, acc.Enfocado);
            CollectionAssert.AreEqual(new[]
            {
                "Tú decides", "Ubicación, Para verte en el mapa y guiarte.", "Ahora no, Ubicación", "Permitir, Ubicación",
                "Notificaciones, Para que la ruta siga con la pantalla apagada.", "Ahora no, Notificaciones", "Permitir, Notificaciones",
                T(ClavesTexto.BienvenidaCambiar), "Paso 3 de 3", "Empezar", "Atrás",
            }, Recorrido());

            gps.Poner(EstadoUbicacion.Buscando);
            servicio.AhoraNoNotificaciones();
            CollectionAssert.AreEqual(new[]
            {
                "Tú decides", "Ubicación, Para verte en el mapa y guiarte.", "Permitido, Ubicación",
                "Notificaciones, Para que la ruta siga con la pantalla apagada.", T(ClavesTexto.BienvenidaDespues), "Permitir, Notificaciones",
                T(ClavesTexto.BienvenidaCambiar), "Paso 3 de 3", "Empezar", "Atrás",
            }, Recorrido());
        }

        [Test]
        public void TalkBack_AlContestarElFocoVaALaRespuestaDeSuTarjeta()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var b = AbrirEnPermisos();
            var acc = pantalla.Accesibilidad;
            acc.PedirFoco(b.Ubicacion.AhoraNo);
            servicio.AhoraNoUbicacion();
            Assert.AreSame(b.Ubicacion.Despues, acc.UltimoFoco);
            acc.PedirFoco(b.Ubicacion.Permitir);
            servicio.PermitirUbicacion();
            gps.Poner(EstadoUbicacion.Buscando);
            Assert.AreSame(b.Ubicacion.FilaPermitido, acc.UltimoFoco);
            acc.PedirFoco(b.Titulo);
            servicio.AhoraNoNotificaciones();
            Assert.AreSame(b.Titulo, acc.UltimoFoco, "Fuera de la tarjeta, el foco no se mueve.");
            Assert.AreSame(acc.NodoDe(b.Titulo), acc.UltimoNodoEnfocado, "Y TalkBack lo recupera en el recorrido rehecho.");
        }

        [Test]
        public void TalkBack_LosNodosSonTextosOBotonesYElFocoDesplazaLaPagina()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var b = AbrirEnPermisos();
            var jerarquia = AssistiveSupport.activeHierarchy;
            if (jerarquia == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            CollectionAssert.AreEqual(Recorrido(), jerarquia.rootNodes.Select(n => n.label));
            foreach (var n in jerarquia.rootNodes)
            {
                bool boton = n.label.StartsWith("Permitir,") || n.label.StartsWith("Ahora no,") || n.label == "Empezar" || n.label == "Atrás";
                Assert.AreEqual(boton ? AccessibilityRole.Button : AccessibilityRole.StaticText, n.role, n.label);
            }
            LanzarFoco(b.Notificaciones.Permitir);
            Assert.AreSame(b.Notificaciones.Permitir, b.UltimoDesplazado);
            LanzarFoco(b.Principal);
            Assert.AreSame(b.Notificaciones.Permitir, b.UltimoDesplazado, "El pie no se desplaza.");
            Assert.AreSame(b.Principal, pantalla.Accesibilidad.UltimoFoco);
            LanzarFoco(b.Titulo);
            Assert.AreSame(b.Titulo, b.UltimoDesplazado);
        }

        [Test]
        public void TalkBack_ElDobleToqueEnCadaBotonHaceLoSuyo()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var b = Abrir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            Activar(b.Principal);
            Assert.AreEqual(1, servicio.Paso);
            Activar(b.BotonAtras);
            Assert.AreEqual(0, servicio.Paso);
            Activar(b.Principal);
            Activar(b.LeerAviso);
            Assert.IsTrue(servicio.AvisoAbierto);
            Activar(b.BotonAtras);
            Assert.IsFalse(servicio.AvisoAbierto);
            Activar(b.Principal);
            Assert.AreEqual(2, servicio.Paso);

            Activar(b.Ubicacion.Permitir);
            Assert.AreEqual((1, 0), (gps.Permisos, notificaciones.Permisos));
            Activar(b.Notificaciones.Permitir);
            Assert.AreEqual((1, 1), (gps.Permisos, notificaciones.Permisos));
            Activar(b.Ubicacion.AhoraNo);
            Assert.AreEqual((Consentimiento.Rechazado, Consentimiento.SinContestar), (servicio.Ubicacion, servicio.Notificaciones));
            Activar(b.Notificaciones.AhoraNo);
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Notificaciones);
            Assert.AreEqual((1, 1), (gps.Permisos, notificaciones.Permisos), "«Ahora no» no pide nada.");

            Activar(b.Principal);
            Assert.IsFalse(pantalla.BienvenidaAbierta);
            Assert.AreEqual("1", prefs.Leer("bienvenida.vista"));
        }

        AccessibilityNode Nodo(VisualElement elemento)
        {
            var nodo = pantalla.Accesibilidad.NodoDe(elemento);
            Assert.IsNotNull(nodo, elemento.name + " no está en el recorrido.");
            return nodo;
        }

        /// <summary>El doble toque de TalkBack (como en PruebasFicha): el evento no se puede lanzar desde fuera.</summary>
        void Activar(VisualElement elemento)
        {
            var accion = Delegado<Func<bool>>(Nodo(elemento), "invoked");
            Assert.IsNotNull(accion, elemento.name + " no tiene acción.");
            Assert.IsTrue(accion());
        }

        /// <summary>Lo que hace TalkBack al poner el foco en el nodo.</summary>
        void LanzarFoco(VisualElement elemento)
        {
            var nodo = Nodo(elemento);
            var alEnfocar = Delegado<Action<AccessibilityNode, bool>>(nodo, "focusChanged");
            Assert.IsNotNull(alEnfocar, elemento.name + " no escucha el foco.");
            alEnfocar(nodo, true);
        }

        static TDelegado Delegado<TDelegado>(AccessibilityNode nodo, string evento) where TDelegado : Delegate
        {
            const System.Reflection.BindingFlags Campos = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var campo = typeof(AccessibilityNode).GetFields(Campos).FirstOrDefault(f =>
                typeof(TDelegado).IsAssignableFrom(f.FieldType) && f.Name.IndexOf(evento, StringComparison.OrdinalIgnoreCase) >= 0);
            if (campo == null) Assert.Inconclusive($"Esta versión de Unity no deja leer el evento {evento} del nodo.");
            return campo.GetValue(nodo) as TDelegado;
        }

        [Test]
        public void Empezar_VuelveElMapaYElFocoVaAlBotonDeUbicacion()
        {
            var b = AbrirEnPermisos();
            b.PulsarPrincipal();
            Assert.IsNull(pantalla.Bienvenida);
            Assert.IsNull(b.parent);
            Assert.IsFalse(pantalla.Accesibilidad.Modal);
            Assert.AreSame(pantalla.BotonUbicacion, pantalla.Accesibilidad.Enfocado);
            CollectionAssert.Contains(Recorrido(), T(ClavesTexto.TemaCambiar));
            CollectionAssert.DoesNotContain(Recorrido(), "Empezar");
            Assert.IsFalse(pantalla.Atras(), "Atrás ya no es de la bienvenida.");
        }

        // ───────────────────────── Textos y aspecto ─────────────────────────

        [Test]
        public void LosTextosSonLosDeTextosMd()
        {
            string doc = File.ReadAllText(Path.Combine(Application.dataPath, "..", "..", "..", "docs", "diseno", "bienvenida", "textos.md"));
            var propios = new[] { ClavesTexto.BienvenidaAtras, ClavesTexto.BienvenidaPasoDe, ClavesTexto.BienvenidaAnuncioPermiso }; // solo TalkBack
            var claves = ClavesTexto.Todas.Where(c => c.StartsWith("bienvenida.") || c.StartsWith("aviso.")).ToList();
            Assert.AreEqual(33, claves.Count);
            foreach (var clave in claves.Except(propios)) StringAssert.Contains(T(clave), doc, clave);
            StringAssert.Contains(T(ClavesTexto.UbicacionExplicacion), doc, "La de la 1.7 también dice la verdad.");
        }

        static string Regla(string selector)
        {
            string uss = File.ReadAllText("Assets/UI/PantallaMapa.uss").Replace("\r\n", "\n");
            var m = Regex.Match(uss, @"(^|\n)" + selector + @"\s*\{(?<c>[^}]*)\}");
            Assert.IsTrue(m.Success, "Sin regla " + selector);
            return m.Groups["c"].Value;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CumpleElContraste(bool oscuro)
        {
            var c = PruebasContraste.Tema(oscuro);
            float Contraste(string a, string b) => PruebasContraste.Contraste(c[a], c[b]);
            Assert.AreEqual(1f, c["bg"].a, "Opaca: no se ve el mapa.");
            Assert.GreaterOrEqual(Contraste("text", "bg"), 4.5f, "textos de los pasos");
            Assert.GreaterOrEqual(Contraste("muted", "bg"), 4.5f, "párrafos del aviso y pie");
            Assert.GreaterOrEqual(Contraste("accent-text", "bg"), 4.5f, "enlace del aviso");
            Assert.GreaterOrEqual(Contraste("accent-text", "accent-soft"), 3f, "iconos en su cuadro");
            Assert.GreaterOrEqual(Contraste("accent-ink", "accent"), 4.5f, "botón principal");
            Assert.GreaterOrEqual(Contraste("text", "surface"), 4.5f, "textos de las tarjetas");
            Assert.GreaterOrEqual(Contraste("muted", "surface"), 4.5f, "«Puedes activarlo después»");
            Assert.GreaterOrEqual(Contraste("success", "surface"), 4.5f, "«Permitido»");
            Assert.GreaterOrEqual(Contraste("control-edge", "surface"), 3f, "borde de «Ahora no»");
            Assert.GreaterOrEqual(Contraste("control-edge", "bg"), 3f, "puntos de progreso");
            Assert.GreaterOrEqual(Contraste("accent", "bg"), 3f, "punto actual");
            StringAssert.Contains("background-color: var(--color-bg);", Regla(@"\.bienvenida"));
            StringAssert.Contains("color: var(--color-success);", Regla(@"\.bienvenida__permitido-texto"));
        }

        [Test]
        public void ConElTextoGrandeSeDesplazaYLosBotonesVanEnColumna()
        {
            var b = Abrir();
            Assert.IsFalse(b.ClassListContains(PantallaBienvenida.ClaseGrande));
            b.PonerEscala(2f);
            Assert.IsTrue(b.ClassListContains(PantallaBienvenida.ClaseGrande));
            StringAssert.Contains("flex-direction: column;", Regla(@"\.bienvenida--grande \.bienvenida__botones"));
            b.PonerEscala(1f);
            Assert.IsFalse(b.ClassListContains(PantallaBienvenida.ClaseGrande));
            EscalaFuente.Forzada = () => 2f;
            Assert.IsTrue(Abrir().ClassListContains(PantallaBienvenida.ClaseGrande), "Abierta ya con el texto al 200 %.");
            foreach (var p in b.Paginas) Assert.IsInstanceOf<ScrollView>(p);
            foreach (var clase in new[] { @"\.bienvenida__enlace:active", @"\.explicacion-ubicacion__boton" })
                StringAssert.Contains("min-height: var(--zona-tactil-min);", Regla(clase));
            StringAssert.Contains("height: var(--zona-tactil-min);", Regla(@"\.bienvenida__atras"));
        }

        [Test]
        public void LaZonaSeguraVaEnSusMargenes()
        {
            var b = Abrir();
            b.PonerZonaSegura(1f, 30f, 2f, 20f);
            Assert.AreEqual(30f, b.style.paddingTop.value.value);
            Assert.AreEqual(20f, b.style.paddingBottom.value.value);
            Assert.AreEqual(1f, b.style.paddingLeft.value.value);
            Assert.AreEqual(2f, b.style.paddingRight.value.value);
        }

        // ───────────────────────── Permisos de Android ─────────────────────────

        class HiloFalso : SynchronizationContext
        {
            public readonly Queue<Action> Cola = new Queue<Action>();
            public override void Post(SendOrPostCallback d, object estado) => Cola.Enqueue(() => d(estado));
        }

        [Test]
        public void Notificaciones_LaRespuestaDeOtroHiloSeAplicaEnElPrincipal()
        {
            var hilo = new HiloFalso();
            SynchronizationContext.SetSynchronizationContext(hilo);
            var permiso = new PermisoFalso();
            var n = new NotificacionesAndroid(permiso);
            SynchronizationContext.SetSynchronizationContext(null);
            int avisos = 0;
            n.Cambiado += () => avisos++;
            n.PedirPermiso();
            permiso.Explica = true;
            var otro = new Thread(() => permiso.Respuesta(false));
            otro.Start();
            otro.Join();
            Assert.AreEqual(0, avisos);
            Assert.AreEqual(EstadoNotificaciones.SinPermiso, n.Estado, "Aún no.");
            hilo.Cola.Dequeue()();
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(EstadoNotificaciones.Denegado, n.Estado);
            Assert.IsTrue(n.PuedePedir);
        }

        [Test]
        public void Notificaciones_CerrarElDialogoNoEsDenegarYDosVecesDenegadoSeBloquea()
        {
            var permiso = new PermisoFalso();
            var n = new NotificacionesAndroid(permiso);
            n.PedirPermiso();
            permiso.Respuesta(null);
            Assert.AreEqual(EstadoNotificaciones.SinPermiso, n.Estado);
            n.PedirPermiso();
            permiso.Explica = true;
            permiso.Respuesta(false);
            Assert.AreEqual(EstadoNotificaciones.Denegado, n.Estado);
            Assert.IsTrue(n.PuedePedir);
            n.PedirPermiso();
            permiso.Explica = false;
            permiso.Respuesta(false);
            Assert.IsFalse(n.PuedePedir);
            permiso.Concedido = true; // en los Ajustes del móvil
            Assert.AreEqual(EstadoNotificaciones.Concedido, n.Estado);
        }

        [Test]
        public void Notificaciones_EnElEditorEstanConcedidas()
        {
            var n = new NotificacionesAndroid();
            Assert.AreEqual(EstadoNotificaciones.Concedido, n.Estado);
        }

        [Test]
        public void PreguntaPermiso_DudosaSoloUnaVez()
        {
            var p = new PreguntaPermiso();
            Assert.IsTrue(p.PuedePedir);
            p.Denegar(explicaba: false, explica: false);
            Assert.IsTrue(p.PuedePedir, "Puede ser el diálogo cerrado sin responder.");
            p.Denegar(explicaba: false, explica: false);
            Assert.IsFalse(p.PuedePedir, "Ya estaba bloqueado.");
            p.Denegar(explicaba: false, explica: true);
            Assert.IsTrue(p.PuedePedir);
            p.Denegar(explicaba: true, explica: false);
            Assert.IsFalse(p.PuedePedir, "Denegado del todo.");
        }

        class FuenteFalsa : IFuenteUbicacion
        {
            public bool TienePermiso { get; set; }
            public bool UbicacionEncendida => true;
            public LocationServiceStatus EstadoServicio => LocationServiceStatus.Stopped;
            public (Coordenada posicion, double marca) UltimaLectura => default;
            public float? Rumbo => null;
            public bool ExplicaPermiso { get; set; }
            public Action<bool> Respuesta;
            public void PedirPermiso(Action<bool> r) => Respuesta = r;
            public void Encender() { }
            public void Apagar() { }
            public void AbrirAjustes(string accion) { }
        }

        [Test]
        public void Gps_DosDenegacionesYYaNoPuedePreguntarYAvisa()
        {
            var fuente = new FuenteFalsa();
            var g = new UbicacionGps(fuente);
            int avisos = 0;
            g.Cambiado += () => avisos++;
            Assert.IsTrue(g.PuedePedirPermiso);
            g.PedirPermiso();
            fuente.ExplicaPermiso = true;
            fuente.Respuesta(false);
            g.Actualizar();
            fuente.Respuesta(false); // la precisa y la aproximada, en otro fotograma
            g.Actualizar();
            Assert.AreEqual(EstadoUbicacion.Denegado, g.Estado);
            Assert.IsTrue(g.PuedePedirPermiso);
            avisos = 0;
            g.PedirPermiso();
            fuente.ExplicaPermiso = false;
            fuente.Respuesta(false);
            g.Actualizar();
            Assert.IsFalse(g.PuedePedirPermiso);
            Assert.AreEqual(1, avisos, "El estado no cambia, pero se avisa.");
        }

        [Test]
        public void ElPermisoDeNotificacionesEstaEnElManifiestoYLoPideSoloElAdaptador()
        {
            StringAssert.Contains("\"android.permission.POST_NOTIFICATIONS\"", File.ReadAllText("Assets/Editor/PermisosUbicacionAndroid.cs"));
            foreach (var f in Directory.GetFiles("Assets/Scripts/UI", "*.cs"))
                StringAssert.DoesNotContain("UnityEngine.Android", File.ReadAllText(f), f);
        }

        // ───────────────────────── Composición ─────────────────────────

        /// <summary>Componer con un UIDocument de verdad (con panel), como Arranque.Start.</summary>
        PantallaBienvenida ComponerConPanel()
        {
            var objeto = new GameObject("Pantalla con interfaz");
            objetos.Add(objeto);
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PrepararInterfaz.RutaPanelSettings);
            documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PrepararInterfaz.RutaPantallaMapa);
            pantalla = objeto.AddComponent<PantallaMapa>();
            objeto.SetActive(true);
            Assert.IsNotNull(documento.rootVisualElement?.Q("raiz"), "El UIDocument no ha creado el árbol de la interfaz.");
            var lugares = new ServicioLugares(new Repositorio());
            lugares.Cargar();
            var ubicacion = new ServicioUbicacion(gps, prefs);
            servicio = new ServicioBienvenida(prefs, ubicacion, notificaciones);
            Assert.IsTrue(Arranque.Arranque.Componer(mapa, pantalla, new ServicioTema(prefs, new SistemaClaro()), lugares,
                new ServicioFiltro(prefs), Ahora, ubicacion, bienvenida: servicio));
            Assert.IsTrue(pantalla.BienvenidaAbierta);
            pantalla.Bienvenida.Reloj = () => reloj += 1f;
            return pantalla.Bienvenida;
        }

        [Test]
        public void Componer_PasaLaBienvenidaALaPantalla()
        {
            var b = ComponerConPanel();
            Assert.IsNotNull(b.panel);
            Assert.AreEqual("hueco-bienvenida", b.parent.name);
        }

        static void Enviar(VisualElement elemento)
        {
            Assert.IsNotNull(elemento.panel, elemento.name);
            using var e = NavigationSubmitEvent.GetPooled();
            e.target = elemento;
            elemento.SendEvent(e);
        }

        [Test]
        public void ConTecladoOMando_CadaBotonHaceLoSuyo()
        {
            var b = ComponerConPanel();
            Enviar(b.Principal);
            Assert.AreEqual(1, servicio.Paso);
            Enviar(b.BotonAtras);
            Assert.AreEqual(0, servicio.Paso);
            Enviar(b.Principal);
            Enviar(b.LeerAviso);
            Assert.IsTrue(servicio.AvisoAbierto);
            Enviar(b.BotonAtras);
            Assert.IsFalse(servicio.AvisoAbierto);
            Enviar(b.Principal);
            Assert.AreEqual(2, servicio.Paso);
            Enviar(b.Ubicacion.Permitir);
            Assert.AreEqual((1, 0), (gps.Permisos, notificaciones.Permisos));
            Enviar(b.Notificaciones.Permitir);
            Assert.AreEqual((1, 1), (gps.Permisos, notificaciones.Permisos));
            Enviar(b.Ubicacion.AhoraNo);
            Assert.AreEqual((Consentimiento.Rechazado, Consentimiento.SinContestar), (servicio.Ubicacion, servicio.Notificaciones));
            Enviar(b.Notificaciones.AhoraNo);
            Assert.AreEqual(Consentimiento.Rechazado, servicio.Notificaciones);
            Enviar(b.Principal);
            Assert.IsFalse(pantalla.BienvenidaAbierta);
        }

        /// <summary>
        /// Un toque: el manipulador Clickable del elemento, con el evento que lanza al soltar. (Sin disposición en EditMode,
        /// un PointerUp no cae dentro de ningún elemento.)
        /// </summary>
        static void Tocar(VisualElement elemento, Clickable toque)
        {
            Assert.AreSame(elemento, toque.target, elemento.name);
            var invocar = typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(invocar);
            using var e = PointerUpEvent.GetPooled();
            invocar.Invoke(toque, new object[] { e });
        }

        [Test]
        public void ConElDedo_CadaBotonHaceLoSuyo()
        {
            var b = ComponerConPanel();
            Tocar(b.Principal, b.Principal.clickable);
            Assert.AreEqual(1, servicio.Paso);
            Tocar(b.BotonAtras, b.ToqueAtras);
            Assert.AreEqual(0, servicio.Paso);
            Tocar(b.Principal, b.Principal.clickable);
            Tocar(b.LeerAviso, b.LeerAviso.clickable);
            Assert.IsTrue(servicio.AvisoAbierto);
            Tocar(b.BotonAtras, b.ToqueAtras);
            Tocar(b.Principal, b.Principal.clickable);
            Assert.AreEqual(2, servicio.Paso);
            Tocar(b.Ubicacion.Permitir, b.Ubicacion.Permitir.clickable);
            Tocar(b.Notificaciones.AhoraNo, b.Notificaciones.AhoraNo.clickable);
            Tocar(b.Notificaciones.Permitir, b.Notificaciones.Permitir.clickable);
            Assert.AreEqual((1, 1), (gps.Permisos, notificaciones.Permisos));
            Tocar(b.Ubicacion.AhoraNo, b.Ubicacion.AhoraNo.clickable);
            Assert.AreEqual((Consentimiento.Rechazado, Consentimiento.Rechazado), (servicio.Ubicacion, servicio.Notificaciones));
            Tocar(b.Principal, b.Principal.clickable);
            Assert.IsFalse(pantalla.BienvenidaAbierta);
        }

        [Test]
        public void Arranque_LaComponeSalvoEnLaCaptura()
        {
            string codigo = File.ReadAllText("Assets/Scripts/Arranque/Arranque.cs");
            StringAssert.Contains("CapturaPruebas.SePide() ? null : new ServicioBienvenida(preferencias, ubicacion, new NotificacionesAndroid())", codigo);
            StringAssert.Contains("bienvenida: bienvenida))", codigo);
        }
    }
}
