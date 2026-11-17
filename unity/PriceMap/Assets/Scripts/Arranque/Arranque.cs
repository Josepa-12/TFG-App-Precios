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
            if (mapa == null) mapa = FindFirstObjectByType<MapaVectorial>();
            if (pantallaMapa == null) pantallaMapa = FindFirstObjectByType<PantallaMapa>();

            // "mapa != null" usa la comparación de Unity (objeto destruido = nulo) antes de pasarlo como interfaz
            if (!Componer(mapa != null ? mapa : null, pantallaMapa)) enabled = false;
        }

        /// <summary>
        /// Conecta las piezas. Si falta alguna, lo registra y devuelve false sin lanzar excepciones.
        /// </summary>
        public static bool Componer(IMapa mapa, PantallaMapa pantallaMapa)
        {
            if (mapa == null || pantallaMapa == null)
            {
                var faltan = new System.Collections.Generic.List<string>();
                if (mapa == null) faltan.Add("el mapa (MapaVectorial)");
                if (pantallaMapa == null) faltan.Add("la PantallaMapa");
                Debug.LogError("PriceMap: falta " + string.Join(" y ", faltan) + " en la escena; la app no puede arrancar.");
                return false;
            }

            pantallaMapa.Inicializar(mapa);
            Debug.Log("PriceMap: app compuesta (mapa → pantalla del mapa).");
            return true;
        }
    }
}
