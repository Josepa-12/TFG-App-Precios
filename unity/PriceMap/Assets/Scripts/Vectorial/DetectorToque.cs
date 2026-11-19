using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Distingue un toque de un arrastre (historia 1.8, EXPERIENCE.md:89): es un toque si al soltar el dedo
    /// se ha movido menos de 6 dp desde donde empezó (contando lo más lejos que llegó), con un solo dedo o el
    /// botón izquierdo. Un segundo dedo o el botón derecho lo anulan. Sin estado de Unity: se prueba en EditMode.
    ///
    /// Pulsación larga (historia 2.5): el mismo dedo quieto (menos de 6 dp) 500 ms. Se dispara una sola vez, al
    /// cumplirse el tiempo (Avanzar, cada fotograma), sin esperar a soltar; después, Soltar ya no da toque.
    /// </summary>
    public class DetectorToque
    {
        /// <summary>Movimiento máximo (dp) para que cuente como toque.</summary>
        public const float UmbralDp = 6f;

        /// <summary>Tiempo quieto (s) que hace falta para la pulsación larga.</summary>
        public const float PulsacionLargaSegundos = 0.5f;

        Vector2 inicio;
        float mayor, transcurrido;
        bool activo, anulado, larga;

        /// <summary>True entre Empezar y Soltar (o Cancelar).</summary>
        public bool Activo => activo;

        /// <summary>True si este gesto ya ha dado la pulsación larga (hasta el siguiente Empezar).</summary>
        public bool PulsacionLargaDada => larga;

        /// <summary>
        /// La pulsación larga ya se ha dado y el dedo sigue abajo: el mapa no debe moverse con él. Un segundo dedo
        /// (Anular) lo libera para el gesto de dos dedos.
        /// </summary>
        public bool ArrastreBloqueado => activo && larga && !anulado;

        /// <summary>El dedo (o el botón izquierdo) baja en ese punto de pantalla (px).</summary>
        public void Empezar(Vector2 px)
        {
            inicio = px;
            mayor = 0f;
            transcurrido = 0f;
            activo = true;
            anulado = false;
            larga = false;
        }

        /// <summary>El dedo se mueve: se recuerda lo más lejos que ha llegado del inicio.</summary>
        public void Mover(Vector2 px)
        {
            if (activo) mayor = Mathf.Max(mayor, Vector2.Distance(inicio, px));
        }

        /// <summary>Un segundo dedo o el botón derecho: este gesto ya no es un toque ni una pulsación larga.</summary>
        public void Anular()
        {
            if (activo) anulado = true;
        }

        /// <summary>El gesto se interrumpe (el sistema cancela el toque o empezó sobre la interfaz).</summary>
        public void Cancelar() => activo = false;

        /// <summary>
        /// Pasa el tiempo con el dedo abajo (cada fotograma). Devuelve true una sola vez: cuando llega a 500 ms sin
        /// anular y sin haberse movido 6 dp o más.
        /// </summary>
        /// <param name="segundos">Tiempo desde la llamada anterior (Time.unscaledDeltaTime).</param>
        /// <param name="dpi">Puntos por pulgada de la pantalla (Screen.dpi); 0 o menos = 160 (1 px = 1 dp).</param>
        public bool Avanzar(float segundos, float dpi)
        {
            if (!activo || anulado || larga) return false;
            if (segundos > 0f) transcurrido += segundos;
            if (transcurrido < PulsacionLargaSegundos || mayor >= UmbralPx(dpi)) return false;
            larga = true;
            return true;
        }

        /// <summary>
        /// El dedo se levanta en ese punto: true si ha sido un toque (menos de 6 dp de movimiento y sin pulsación
        /// larga ya dada).
        /// </summary>
        /// <param name="dpi">Puntos por pulgada de la pantalla (Screen.dpi); 0 o menos = 160 (1 px = 1 dp).</param>
        public bool Soltar(Vector2 px, float dpi)
        {
            if (!activo) return false;
            Mover(px);
            activo = false;
            return !anulado && !larga && mayor < UmbralPx(dpi);
        }

        /// <summary>6 dp en píxeles de pantalla: 1 dp = dpi / 160 px (como el PanelSettings de la interfaz).</summary>
        public static float UmbralPx(float dpi) => UmbralDp * (dpi > 0f ? dpi : 160f) / 160f;
    }
}
