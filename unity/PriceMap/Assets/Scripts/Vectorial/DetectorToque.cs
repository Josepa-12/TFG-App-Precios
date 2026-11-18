using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Distingue un toque de un arrastre (historia 1.8, EXPERIENCE.md:89): es un toque si al soltar el dedo
    /// se ha movido menos de 6 dp desde donde empezó (contando lo más lejos que llegó), con un solo dedo o el
    /// botón izquierdo. Un segundo dedo o el botón derecho lo anulan. Sin estado de Unity: se prueba en EditMode.
    /// </summary>
    public class DetectorToque
    {
        /// <summary>Movimiento máximo (dp) para que cuente como toque.</summary>
        public const float UmbralDp = 6f;

        Vector2 inicio;
        float mayor;
        bool activo, anulado;

        /// <summary>True entre Empezar y Soltar (o Cancelar).</summary>
        public bool Activo => activo;

        /// <summary>El dedo (o el botón izquierdo) baja en ese punto de pantalla (px).</summary>
        public void Empezar(Vector2 px)
        {
            inicio = px;
            mayor = 0f;
            activo = true;
            anulado = false;
        }

        /// <summary>El dedo se mueve: se recuerda lo más lejos que ha llegado del inicio.</summary>
        public void Mover(Vector2 px)
        {
            if (activo) mayor = Mathf.Max(mayor, Vector2.Distance(inicio, px));
        }

        /// <summary>Un segundo dedo o el botón derecho: este gesto ya no es un toque.</summary>
        public void Anular()
        {
            if (activo) anulado = true;
        }

        /// <summary>El gesto se interrumpe (el sistema cancela el toque o empezó sobre la interfaz).</summary>
        public void Cancelar() => activo = false;

        /// <summary>El dedo se levanta en ese punto: true si ha sido un toque (menos de 6 dp de movimiento).</summary>
        /// <param name="dpi">Puntos por pulgada de la pantalla (Screen.dpi); 0 o menos = 160 (1 px = 1 dp).</param>
        public bool Soltar(Vector2 px, float dpi)
        {
            if (!activo) return false;
            Mover(px);
            activo = false;
            return !anulado && mayor < UmbralPx(dpi);
        }

        /// <summary>6 dp en píxeles de pantalla: 1 dp = dpi / 160 px (como el PanelSettings de la interfaz).</summary>
        public static float UmbralPx(float dpi) => UmbralDp * (dpi > 0f ? dpi : 160f) / 160f;
    }
}
