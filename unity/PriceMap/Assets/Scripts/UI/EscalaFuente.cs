using System;
using System.Collections.Generic;
using UnityEngine.Accessibility;
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

        /// <summary>
        /// Solo para la captura de PC (CapturaPruebas, en otro ensamblado: por eso es pública), que prueba la fuente al
        /// 130 %. La app nunca la pone: null, y manda AccessibilitySettings.fontScale.
        /// </summary>
        public static Func<float> Forzada { get; set; }

        /// <summary>Escala de fuente que se aplica: la forzada o la del sistema (AccessibilitySettings.fontScale).</summary>
        public static float Sistema => Forzada != null ? Forzada() : AccessibilitySettings.fontScale;

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
