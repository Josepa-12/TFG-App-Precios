using System;
using PriceMap.Adaptadores.Datos;
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

        void Start()
        {
            Application.targetFrameRate = 60; // Android limita a 30 por defecto (NFR-1 pide ≥ 30)

            if (mapa == null) mapa = FindFirstObjectByType<MapaVectorial>();
            if (pantallaMapa == null) pantallaMapa = FindFirstObjectByType<PantallaMapa>();

            var preferencias = new PreferenciasLocales();
            var tema = new ServicioTema(preferencias, new TemaSistemaAndroid());

            // Lugares base empaquetados (historia 1.4). Si fallan, el adaptador ya lo ha registrado y
            // la app sigue con el mapa sin marcadores.
            var lugares = new ServicioLugares(new RepositorioLugaresJson());
            if (lugares.Cargar().Ok) Debug.Log($"PriceMap: {lugares.Lugares.Count} lugares cargados.");

            // "mapa != null" usa la comparación de Unity (objeto destruido = nulo) antes de pasarlo como interfaz.
            // "Ahora" lo da el Arranque (AD-12): con él se calcula el resumen del precio de cada lugar.
            if (!Componer(mapa != null ? mapa : null, pantallaMapa, tema, lugares, DateTime.UtcNow)) { enabled = false; return; }

            mapa.EntradaBloqueada = pantallaMapa.TapaPunto;
            CapturaPruebas.SiSePide(gameObject, tema, preferencias, mapa, pantallaMapa);
        }

        /// <summary>
        /// Conecta las piezas. Si falta alguna, lo registra y devuelve false sin lanzar excepciones.
        /// </summary>
        public static bool Componer(IMapa mapa, PantallaMapa pantallaMapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora)
        {
            if (mapa == null || pantallaMapa == null || tema == null || lugares == null)
            {
                var faltan = new System.Collections.Generic.List<string>();
                if (mapa == null) faltan.Add("el mapa (MapaVectorial)");
                if (pantallaMapa == null) faltan.Add("la PantallaMapa");
                if (tema == null) faltan.Add("el ServicioTema");
                if (lugares == null) faltan.Add("el ServicioLugares");
                Debug.LogError("PriceMap: falta " + string.Join(" y ", faltan) + " en la escena; la app no puede arrancar.");
                return false;
            }

            tema.Cambiado += mapa.AplicarTema;   // el mapa y la interfaz cambian a la vez (FR-6)
            pantallaMapa.Inicializar(mapa, tema, lugares, ahora);
            tema.Iniciar();
            Debug.Log($"PriceMap: app compuesta (mapa → pantalla del mapa), tema {tema.Actual}.");
            return true;
        }
    }
}
