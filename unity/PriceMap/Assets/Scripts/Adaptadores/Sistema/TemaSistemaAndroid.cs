using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Sistema
{
    /// <summary>
    /// Lee si Android está en modo oscuro (Configuration.uiMode). En el editor y en otras plataformas, claro.
    /// </summary>
    public class TemaSistemaAndroid : ITemaSistema
    {
        const int UiModeNightMask = 0x30; // Configuration.UI_MODE_NIGHT_MASK
        const int UiModeNightYes = 0x20;  // Configuration.UI_MODE_NIGHT_YES

        public bool EsOscuro
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
                    using var recursos = actividad.Call<AndroidJavaObject>("getResources");
                    using var configuracion = recursos.Call<AndroidJavaObject>("getConfiguration");
                    return (configuracion.Get<int>("uiMode") & UiModeNightMask) == UiModeNightYes;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("PriceMap: no se pudo leer el modo oscuro del sistema: " + e.Message);
                    return false;
                }
#else
                return false;
#endif
            }
        }
    }
}
