using UnityEngine;

namespace PriceMap.UI
{
    /// <summary>El teclado de Android de un campo enfocado.</summary>
    internal static class TecladoAndroid
    {
        /// <summary>
        /// Android, por defecto, se queda el primer toque fuera del teclado (solo lo cierra) y, mientras está abierto,
        /// Unity da la app por perdida de foco: deja de dibujar y apaga el GPS (UnityPlayer.handleFocus). Pasando los
        /// toques, el toque llega a la app y esta sigue a la vista y en marcha. Se llama antes de que salga el teclado.
        /// </summary>
        public static void PasarToques(bool pasar)
        {
            Pasados = pasar;
#if UNITY_ANDROID && !UNITY_EDITOR
            TouchScreenKeyboard.Android.consumesOutsideTouches = !pasar;
#endif
        }

        /// <summary>Lo último pedido (para las pruebas).</summary>
        internal static bool Pasados { get; private set; }
    }
}
