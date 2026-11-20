using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Sistema
{
    /// <summary>
    /// Lee lo compartido con la app del intent de la actividad de Unity (plugin <c>com.pricemap.compartir.PuenteCompartir</c>,
    /// historia 2.5, parte 3). En el editor y en otras plataformas no hay nada (null).
    /// </summary>
    public class EntradaCompartidaAndroid : IEntradaCompartida
    {
        public string Tomar()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
                if (actividad == null) return null;
                using var puente = new AndroidJavaClass("com.pricemap.compartir.PuenteCompartir");
                return puente.CallStatic<string>("tomar", actividad);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("PriceMap: no se pudo leer lo compartido con la app: " + e.Message);
                return null;
            }
#else
            return null;
#endif
        }
    }
}
