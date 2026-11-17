using System.Collections.Generic;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Convierte los tamaños de letra de los tokens (dp) en sp: los multiplica por la escala de fuente
    /// del sistema (AccessibilitySettings.fontScale). Con la fuente al 200 % los contenedores crecen
    /// o el texto pasa a varias líneas; nada tiene alto fijo.
    /// </summary>
    public static class EscalaFuente
    {
        static readonly Dictionary<TextElement, float> tamanoBase = new Dictionary<TextElement, float>();

        public static void Aplicar(VisualElement raiz, float escala)
        {
            if (escala <= 0f) escala = 1f;
            raiz.Query<TextElement>().ForEach(t =>
            {
                if (!tamanoBase.TryGetValue(t, out float baseDp))
                {
                    baseDp = t.resolvedStyle.fontSize;
                    if (baseDp <= 0f) return; // aún sin estilo resuelto: se aplicará en la siguiente pasada
                    tamanoBase[t] = baseDp;
                }
                t.style.fontSize = baseDp * escala;
            });
        }

        /// <summary>Olvida los tamaños guardados (al destruir la pantalla).</summary>
        public static void Olvidar(VisualElement raiz) => raiz.Query<TextElement>().ForEach(t => tamanoBase.Remove(t));
    }
}
