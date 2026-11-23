using System;
using System.Threading.Tasks;
using PriceMap.Adaptadores.Busqueda;
using PriceMap.Adaptadores.Datos;
using PriceMap.Adaptadores.Gps;
using PriceMap.Adaptadores.Rutas;
using PriceMap.Adaptadores.Sistema;
using PriceMap.Adaptadores.Voz;
using PriceMap.Aplicacion;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEngine;

namespace PriceMap.Arranque
{
    /// <summary>
    /// Raíz de composición (AD-2): el único sitio que crea o localiza adaptadores y servicios
    /// y se los entrega a la UI. Debe haber uno solo, en la escena principal.
    /// </summary>
    public class Arranque : MonoBehaviour
    {
        [SerializeField] MapaVectorial mapa;
        [SerializeField] PantallaMapa pantallaMapa;

        ServicioUbicacion ubicacion;
        ServicioVoz voz;

        /// <summary>Fotogramas por segundo con la app delante: Android limita a 30 por defecto (NFR-1 pide ≥ 30).</summary>
        public const int FotogramasDelante = 60;
        /// <summary>Fotogramas por segundo en segundo plano navegando (historia 2.8, parte 2): sin dibujar, solo la lógica.</summary>
        public const int FotogramasSegundoPlano = 5;

        void Start()
        {
            Application.targetFrameRate = FotogramasDelante;

            if (mapa == null) mapa = FindFirstObjectByType<MapaVectorial>();
            if (pantallaMapa == null) pantallaMapa = FindFirstObjectByType<PantallaMapa>();

            var preferencias = new PreferenciasLocales();
            var tema = new ServicioTema(preferencias, new TemaSistemaAndroid());
            var filtro = new ServicioFiltro(preferencias);

            // Lugares base empaquetados (historia 1.4). Si fallan, el adaptador ya lo ha registrado y
            // la app sigue con el mapa sin marcadores.
            var lugares = new ServicioLugares(new RepositorioLugaresJson());
            if (lugares.Cargar().Ok) Debug.Log($"PriceMap: {lugares.Lugares.Count} lugares cargados.");

            // Posición del usuario (historia 1.7): GPS y brújula del móvil. La captura de pruebas usa una
            // ubicación falsa para no pedir permisos ni depender del GPS del PC.
            IUbicacion fuenteUbicacion = CapturaPruebas.SePide() ? CapturaPruebas.UbicacionFalsa() : gameObject.AddComponent<UbicacionUnity>();
            ubicacion = new ServicioUbicacion(fuenteUbicacion, preferencias);

            // "mapa != null" usa la comparación de Unity (objeto destruido = nulo) antes de pasarlo como interfaz.
            // "Ahora" lo da el Arranque (AD-12): con él se calcula el resumen del precio de cada lugar.
            // "Quitar animaciones" del sistema (UX-DR19): la hoja del buscador cambia de altura sin transición
            var accesibilidadSistema = new AccesibilidadSistemaAndroid();
            // El buscador (historia 2.3) lo crea Componer con el mapa ya comprobado y el mismo "ahora"
            // Direcciones (historia 2.3, parte 2): Photon, 350 ms después de la última tecla
            // Dirección del punto libre (historia 2.5): el mismo Photon, con /reverse
            var photon = new GeocodificadorPhoton();
            var direcciones = new BuscadorDirecciones(photon, Task.Delay);
            // Enlaces cortos pegados en el buscador (historia 2.5, parte 2): se siguen sus redirecciones
            var resolutor = new ResolutorEnlacesWeb();
            // Tiempos e Ir de la ficha (historia 2.6): Valhalla y, si falla o tarda 8 s, OSRM. La captura de pruebas
            // usa rutas falsas para no depender de la red. Sin conexión (los dos caen), la ruta aproximada por las calles
            // de las teselas guardadas (parte 2b); la captura usa la misma, con las teselas de Sanlúcar ya en disco
            var callesGuardadas = new RutasCallesGuardadas(Application.persistentDataPath,
                mapa != null ? mapa.zoom : RutasCallesGuardadas.Zoom); // el zoom con el que el mapa guarda las teselas
            // Calles del buscador (spec buscar-calles-guardadas): los nombres de esas mismas teselas, sin red
            var calles = new BuscadorCalles(new BusquedaCallesGuardadas(Application.persistentDataPath, callesGuardadas.ZoomTeselas));
            var rutas = CapturaPruebas.SePide()
                ? new ServicioRutas(CapturaPruebas.RutasFalsas(), null, Task.Delay, local: callesGuardadas)
                : new ServicioRutas(new RutasValhalla(), new RutasOsrm(), Task.Delay, local: callesGuardadas);
            // Seguir la ruta tras Ir (historia 2.7, parte 1): recálculo con las mismas rutas y la notificación en es-ES
            var navegacion = new ServicioNavegacion(ubicacion, rutas, Task.Delay,
                textoNotificacion: (destino, minutos) => Textos.T(ClavesTexto.NotificacionNavegando, destino, minutos),
                textoLlegada: destino => Textos.T(ClavesTexto.LlegadaA, destino));
            // Indicaciones por voz (historia 2.8): el TextToSpeech de Android; en el PC, el editor y la captura, callada
            voz = new ServicioVoz(navegacion, CrearVoz(), preferencias, textoLlegada: destino => Textos.T(ClavesTexto.LlegadaA, destino));
            if (!Componer(mapa != null ? mapa : null, pantallaMapa, tema, lugares, filtro, DateTime.UtcNow, ubicacion,
                    accesibilidadSistema, direcciones: direcciones, geocodificador: photon, resolutorEnlaces: resolutor, rutas: rutas,
                    navegacion: navegacion, pantallaSistema: new PantallaSistemaUnity(), voz: voz, calles: calles))
            { enabled = false; return; }

            mapa.EntradaBloqueada = pantallaMapa.TapaPunto;
            CapturaPruebas.SiSePide(gameObject, tema, filtro, preferencias, mapa, pantallaMapa);

            // Ubicaciones y textos compartidos con la app (historia 2.5, parte 3): al arrancar y al volver delante
            compartida = new EntradaCompartidaAndroid();
            PasarCompartido(compartida, pantallaMapa);
        }

        IEntradaCompartida compartida;

        /// <summary>La voz del móvil (Android) o la que no suena (PC, editor y captura de pruebas).</summary>
        static IVoz CrearVoz()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!CapturaPruebas.SePide()) return new VozAndroid();
#endif
            return new VozNula();
        }

        /// <summary>Lo compartido con la app y aún no leído va al buscador de la pantalla. Sin nada, nada.</summary>
        internal static void PasarCompartido(IEntradaCompartida entrada, PantallaMapa pantalla)
        {
            if (entrada == null || pantalla == null) return;
            string texto = entrada.Tomar();
            if (texto != null) pantalla.RecibirCompartido(texto);
        }

        // GPS solo con la app delante (NFR-3): se apagan ubicación y brújula en segundo plano y se encienden al volver
        // Al volver delante (OnApplicationFocus, que llega también tras la pausa), la pantalla relee los ajustes de
        // accesibilidad que pudieron cambiar en Ajustes: una sola lectura por vuelta
        void OnApplicationPause(bool enPausa)
        {
            AlPausar(ubicacion, enPausa);
            AlVolverDePausa(voz, CamaraDelMapa(), enPausa);
            // Con pantalla partida puede llegar onNewIntent sin cambio de foco: también al salir de la pausa
            AlVolverDelante(compartida, pantallaMapa, delante: !enPausa, activo: enabled);
        }

        void OnApplicationFocus(bool conFoco)
        {
            AlCambiarFoco(ubicacion, conFoco);
            if (conFoco && enabled && pantallaMapa != null) pantallaMapa.LeerAccesibilidadSistema();
            AlVolverDelante(compartida, pantallaMapa, delante: conFoco, activo: enabled);
        }

        /// <summary>
        /// Lo manda ActividadPrincipal (Android, UnitySendMessage) al ir a segundo plano navegando ("1": Unity sigue sin
        /// pausarse) y al volver o terminar la ruta ("0"). Historia 2.8, parte 2.
        /// </summary>
        public void AlSegundoPlano(string valor) => AplicarSegundoPlano(valor == "1", CamaraDelMapa());

        static Camera CamaraDelMapa()
        {
            var camara = FindFirstObjectByType<CamaraMapa>();
            return camara != null ? camara.GetComponent<Camera>() : null;
        }

        /// <summary>
        /// Lo que hace OnApplicationPause para la voz y el dibujo (historia 2.8, parte 2). Navegando Unity no se pausa
        /// (ActividadPrincipal): si llega aquí, el sistema lo pausó igualmente o no había ruta. Al salir de la pausa, el
        /// ritmo normal y la cámara (sin depender del aviso «0», que pudo quedarse en cola) y la voz calla 1 s mientras se
        /// procesan las posiciones guardadas. Al entrar en pausa, nada.
        /// </summary>
        internal static void AlVolverDePausa(ServicioVoz voz, Camera camara, bool enPausa)
        {
            if (enPausa) return;
            AplicarSegundoPlano(false, camara);
            voz?.Reanudar();
        }

        /// <summary>
        /// En segundo plano navegando: 5 fotogramas por segundo y la cámara del mapa sin dibujar (la navegación, la voz y
        /// la notificación siguen igual). Al volver: el ritmo normal y la cámara otra vez. Sin cámara, solo el ritmo.
        /// </summary>
        internal static void AplicarSegundoPlano(bool segundoPlano, Camera camara)
        {
            Application.targetFrameRate = segundoPlano ? FotogramasSegundoPlano : FotogramasDelante;
            if (camara != null) camara.enabled = !segundoPlano;
        }

        /// <summary>
        /// Con la app delante y el Arranque activo, lo compartido con la app ya abierta (onNewIntent) va al buscador.
        /// </summary>
        internal static void AlVolverDelante(IEntradaCompartida entrada, PantallaMapa pantalla, bool delante, bool activo)
        {
            if (!delante || !activo) return;
            PasarCompartido(entrada, pantalla);
        }

        /// <summary>Lo que hace OnApplicationPause: en pausa se detiene; al salir de ella se reanuda.</summary>
        internal static void AlPausar(ServicioUbicacion ubicacion, bool enPausa) => AplicarPrimerPlano(ubicacion, delante: !enPausa);

        /// <summary>Lo que hace OnApplicationFocus: sin foco se detiene; con foco se reanuda.</summary>
        internal static void AlCambiarFoco(ServicioUbicacion ubicacion, bool conFoco) => AplicarPrimerPlano(ubicacion, delante: conFoco);

        /// <summary>Con la app delante se reanudan ubicación y brújula; si no, se detienen. Sin servicio, nada.</summary>
        internal static void AplicarPrimerPlano(ServicioUbicacion ubicacion, bool delante)
        {
            if (ubicacion == null) return;
            if (delante) ubicacion.Reanudar(); else ubicacion.Pausar();
        }

        /// <summary>
        /// Conecta las piezas. Si falta alguna, lo registra y devuelve false sin lanzar excepciones.
        /// </summary>
        /// <param name="ubicacion">Posición del usuario; sin ella (null) no hay botón de ubicación ni círculo.</param>
        /// <param name="accesibilidadSistema">Ajustes de accesibilidad del sistema; sin él, animaciones normales.</param>
        /// <param name="busqueda">Buscador (historia 2.3); si no se da, se crea con los lugares, el mapa y "ahora".</param>
        /// <param name="direcciones">Direcciones (historia 2.3, parte 2); sin él (null), el buscador no busca direcciones.</param>
        /// <param name="cerca">"Encuentra cerca" (historia 2.4); si no se da, se crea con los lugares, el mapa y "ahora".</param>
        /// <param name="geocodificador">Dirección del punto libre (historia 2.5); sin él, la ficha del punto solo da coordenadas.</param>
        /// <param name="resolutorEnlaces">Enlaces cortos del buscador (historia 2.5, parte 2); sin él, no se reconocen.</param>
        /// <param name="rutas">Tiempos e Ir de la ficha (historia 2.6); sin él, la fila de acciones queda vacía.</param>
        /// <param name="navegacion">Seguir la ruta tras Ir (historia 2.7); sin él, la ruta se dibuja pero no se sigue.</param>
        /// <param name="pantallaSistema">Pantalla encendida mientras se navega (historia 2.7, parte 2b); sin él, se apaga sola.</param>
        /// <param name="voz">Indicaciones por voz (historia 2.8); sin él, no hay botón de voz.</param>
        /// <param name="calles">Calles guardadas del buscador (spec buscar-calles-guardadas); sin él, no se buscan.</param>
        public static bool Componer(IMapa mapa, PantallaMapa pantallaMapa, ServicioTema tema, ServicioLugares lugares,
            ServicioFiltro filtro, DateTime ahora, ServicioUbicacion ubicacion = null, IAccesibilidadSistema accesibilidadSistema = null,
            ServicioBusqueda busqueda = null, BuscadorDirecciones direcciones = null, ServicioCerca cerca = null,
            IGeocodificador geocodificador = null, IResolutorEnlaces resolutorEnlaces = null, ServicioRutas rutas = null,
            ServicioNavegacion navegacion = null, IPantallaSistema pantallaSistema = null, ServicioVoz voz = null,
            BuscadorCalles calles = null)
        {
            if (mapa == null || pantallaMapa == null || tema == null || lugares == null || filtro == null)
            {
                var faltan = new System.Collections.Generic.List<string>();
                if (mapa == null) faltan.Add("el mapa (MapaVectorial)");
                if (pantallaMapa == null) faltan.Add("la PantallaMapa");
                if (tema == null) faltan.Add("el ServicioTema");
                if (lugares == null) faltan.Add("el ServicioLugares");
                if (filtro == null) faltan.Add("el ServicioFiltro");
                Debug.LogError("PriceMap: falta " + string.Join(" y ", faltan) + " en la escena; la app no puede arrancar.");
                return false;
            }

            tema.Cambiado += mapa.AplicarTema;   // el mapa y la interfaz cambian a la vez (FR-6)
            // Buscador de lugares y puntos de interés (historia 2.3): todo local, con el mismo "ahora" que los precios
            busqueda ??= new ServicioBusqueda(lugares, mapa, ahora);
            // "Encuentra cerca" (historia 2.4): también local, con las mismas fuentes que el buscador
            cerca ??= new ServicioCerca(lugares, mapa, ahora);
            // La pantalla no se apaga mientras se navega (historia 2.7, parte 2b; FR20)
            if (navegacion != null && pantallaSistema != null) _ = new PantallaEncendida(navegacion, pantallaSistema);
            pantallaMapa.Inicializar(mapa, tema, lugares, ahora, filtro, ubicacion, accesibilidadSistema, busqueda, direcciones, cerca, geocodificador,
                resolutorEnlaces, rutas, navegacion, voz, calles);
            tema.Iniciar();
            filtro.Iniciar(); // filtro "Precios" guardado; Ninguno la primera vez (FR-2)
            ubicacion?.Iniciar(); // GPS y brújula, si ya hay permiso
            Debug.Log($"PriceMap: app compuesta (mapa → pantalla del mapa), tema {tema.Actual}, filtro {filtro.Actual}" +
                      (ubicacion != null ? $", ubicación {ubicacion.Estado}." : "."));
            return true;
        }
    }
}
