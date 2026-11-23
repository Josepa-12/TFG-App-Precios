using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Sistema
{
    /// <summary>
    /// Pantalla de inicio nativa (plugin <c>com.pricemap.inicio.PuenteInicio</c>; la pone ActividadPrincipal encima de
    /// Unity al abrir la app). En el editor y en otras plataformas no hay (Segundos null). Si falla la llamada, como si no
    /// la hubiera: la nativa se va sola a los 15 s.
    /// </summary>
    public class PantallaInicioAndroid : IPantallaInicio
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        const string ClasePuente = "com.pricemap.inicio.PuenteInicio";
        AndroidJavaClass puente;
        AndroidJavaClass Puente => puente ??= new AndroidJavaClass(ClasePuente);

        public double? Segundos
        {
            get
            {
                try
                {
                    int ms = Puente.CallStatic<int>("estado"); // -1: no hay; si no, ms desde que el pin empezó a caer
                    return ms < 0 ? (double?)null : ms / 1000.0;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("PriceMap: no se pudo leer la pantalla de inicio: " + e.Message);
                    return null;
                }
            }
        }

        public void Irse(PuntoPantalla? destino)
        {
            var (x, y) = Fraccion(destino, Screen.width, Screen.height);
            try { Puente.CallStatic("irse", x, y); }
            catch (System.Exception e) { Debug.LogWarning("PriceMap: no se pudo quitar la pantalla de inicio: " + e.Message); }
        }
#else
        public double? Segundos => null;
        public void Irse(PuntoPantalla? destino) { }
#endif

        /// <summary>
        /// El punto como fracción de la pantalla (0–1 desde arriba a la izquierda): así vale aunque Unity dibuje con otra
        /// resolución. Sin punto, detrás de la cámara o fuera de la pantalla, el centro.
        /// </summary>
        internal static (float x, float y) Fraccion(PuntoPantalla? punto, float ancho, float alto)
        {
            if (!punto.HasValue || !punto.Value.Delante || !(ancho > 0f) || !(alto > 0f)) return (0.5f, 0.5f);
            float x = punto.Value.X / ancho, y = punto.Value.Y / alto;
            return x >= 0f && x <= 1f && y >= 0f && y <= 1f ? (x, y) : (0.5f, 0.5f); // NaN tampoco pasa
        }
    }
}
