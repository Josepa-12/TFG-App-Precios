using System;
using System.Threading.Tasks;
using PriceMap.Adaptadores.Busqueda;
using PriceMap.Adaptadores.Datos;
using PriceMap.Adaptadores.Gps;
using PriceMap.Adaptadores.Sistema;
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

        void Start()
        {
            Application.targetFrameRate = 60; // Android limita a 30 por defecto (NFR-1 pide ≥ 30)

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
            var direcciones = new BuscadorDirecciones(new GeocodificadorPhoton(), Task.Delay);
            if (!Componer(mapa != null ? mapa : null, pantallaMapa, tema, lugares, filtro, DateTime.UtcNow, ubicacion,
                    accesibilidadSistema, direcciones: direcciones)) { enabled = false; return; }

            mapa.EntradaBloqueada = pantallaMapa.TapaPunto;
            CapturaPruebas.SiSePide(gameObject, tema, filtro, preferencias, mapa, pantallaMapa);
        }

        // GPS solo con la app delante (NFR-3): se apagan ubicación y brújula en segundo plano y se encienden al volver
        // Al volver delante (OnApplicationFocus, que llega también tras la pausa), la pantalla relee los ajustes de
        // accesibilidad que pudieron cambiar en Ajustes: una sola lectura por vuelta
        void OnApplicationPause(bool enPausa) => AlPausar(ubicacion, enPausa);

        void OnApplicationFocus(bool conFoco)
        {
            AlCambiarFoco(ubicacion, conFoco);
            if (conFoco && enabled && pantallaMapa != null) pantallaMapa.LeerAccesibilidadSistema();
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
        public static bool Componer(IMapa mapa, PantallaMapa pantallaMapa, ServicioTema tema, ServicioLugares lugares,
            ServicioFiltro filtro, DateTime ahora, ServicioUbicacion ubicacion = null, IAccesibilidadSistema accesibilidadSistema = null,
            ServicioBusqueda busqueda = null, BuscadorDirecciones direcciones = null, ServicioCerca cerca = null)
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
            pantallaMapa.Inicializar(mapa, tema, lugares, ahora, filtro, ubicacion, accesibilidadSistema, busqueda, direcciones, cerca);
            tema.Iniciar();
            filtro.Iniciar(); // filtro "Precios" guardado; Ninguno la primera vez (FR-2)
            ubicacion?.Iniciar(); // GPS y brújula, si ya hay permiso
            Debug.Log($"PriceMap: app compuesta (mapa → pantalla del mapa), tema {tema.Actual}, filtro {filtro.Actual}" +
                      (ubicacion != null ? $", ubicación {ubicacion.Estado}." : "."));
            return true;
        }
    }
}
