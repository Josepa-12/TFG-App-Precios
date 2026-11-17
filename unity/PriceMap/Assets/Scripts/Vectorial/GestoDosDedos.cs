using UnityEngine;

namespace PriceMap.Vectorial
{
    public enum TipoGesto { Nada, Inclinar, ZoomGiro }

    /// <summary>
    /// Decide qué hace un gesto de dos dedos en un fotograma (EXPERIENCE.md · gestos):
    /// arrastre vertical de los dos dedos en el mismo sentido = inclinar; separar, juntar o girar = zoom y giro.
    /// Función pura para poder probarla sin pantalla táctil.
    /// </summary>
    public static class GestoDosDedos
    {
        /// <summary>Movimiento vertical mínimo de cada dedo por fotograma para contar como inclinación (px).</summary>
        public const float MinimoVertical = 2f;

        /// <summary>
        /// Clasifica el gesto. <paramref name="dy"/> es el desplazamiento vertical medio (positivo = hacia arriba),
        /// solo válido si el resultado es <see cref="TipoGesto.Inclinar"/>.
        /// </summary>
        public static TipoGesto Clasificar(Vector2 deltaA, Vector2 deltaB, float distAntes, float distAhora,
                                           float angAntes, float angAhora, out float dy)
        {
            dy = (deltaA.y + deltaB.y) / 2f;
            float cambioDist = Mathf.Abs(distAhora - distAntes);
            float cambioAng = Mathf.Abs(Mathf.DeltaAngle(angAntes, angAhora));

            bool verticalA = Mathf.Abs(deltaA.y) >= MinimoVertical && Mathf.Abs(deltaA.y) > Mathf.Abs(deltaA.x) * 1.5f;
            bool verticalB = Mathf.Abs(deltaB.y) >= MinimoVertical && Mathf.Abs(deltaB.y) > Mathf.Abs(deltaB.x) * 1.5f;
            bool mismoSentido = Mathf.Sign(deltaA.y) == Mathf.Sign(deltaB.y);
            // Los dedos se mueven juntos: casi no cambian ni su separación ni su ángulo
            bool juntos = cambioDist < Mathf.Max(3f, Mathf.Abs(dy) * 0.5f) && cambioAng < 5f;

            if (verticalA && verticalB && mismoSentido && juntos) return TipoGesto.Inclinar;
            if (cambioDist > 0.01f || cambioAng > 0.01f) return TipoGesto.ZoomGiro;
            return TipoGesto.Nada;
        }
    }
}
