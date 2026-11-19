using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Sistema
{
    /// <summary>
    /// Lee si Android tiene quitadas las animaciones ("Quitar animaciones" pone Settings.Global.ANIMATOR_DURATION_SCALE
    /// a 0). En el editor, en otras plataformas o si falla la lectura: false (animaciones normales).
    /// </summary>
    public class AccesibilidadSistemaAndroid : IAccesibilidadSistema
    {
        const string EscalaAnimaciones = "animator_duration_scale"; // Settings.Global.ANIMATOR_DURATION_SCALE

        public bool AnimacionesQuitadas
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
                    using var resolver = actividad.Call<AndroidJavaObject>("getContentResolver");
                    using var ajustes = new AndroidJavaClass("android.provider.Settings$Global");
                    return ajustes.CallStatic<float>("getFloat", resolver, EscalaAnimaciones, 1f) == 0f;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("PriceMap: no se pudo leer la escala de animaciones del sistema: " + e.Message);
                    return false;
                }
#else
                return false;
#endif
            }
        }
    }
}
