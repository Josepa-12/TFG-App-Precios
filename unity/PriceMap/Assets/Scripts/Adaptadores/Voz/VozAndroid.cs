#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Voz
{
    /// <summary>
    /// Voz real (historia 2.8): llama por JNI al plugin de Assets/Plugins/Android (com.pricemap.voz.PuenteVoz), que usa el
    /// TextToSpeech de Android en es-ES con el uso de audio de navegación, baja el volumen de las otras apps mientras habla
    /// y calla con el móvil en silencio o en vibración. Solo se compila en Android.
    /// </summary>
    public sealed class VozAndroid : IVoz
    {
        const string ClasePuente = "com.pricemap.voz.PuenteVoz";

        AndroidJavaClass puente;
        AndroidJavaObject actividad;

        AndroidJavaClass Puente => puente ??= new AndroidJavaClass(ClasePuente);

        AndroidJavaObject Actividad
        {
            get
            {
                if (actividad != null) return actividad;
                using var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                return actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }

        public void Hablar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;
            try { Puente.CallStatic("hablar", Actividad, texto); }
            catch (Exception e) { Debug.LogWarning("PriceMap: no se pudo decir la indicación: " + e.Message); }
        }

        public void Parar()
        {
            try { Puente.CallStatic("parar"); }
            catch (Exception e) { Debug.LogWarning("PriceMap: no se pudo callar la voz: " + e.Message); }
        }
    }
}
#endif
