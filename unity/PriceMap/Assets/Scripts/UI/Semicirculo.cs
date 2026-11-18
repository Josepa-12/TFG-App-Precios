using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Extremos semicirculares (prototipo .pill: border-radius = la mitad del alto). En CSS un radio enorme (999px)
    /// se reduce por igual en todas las esquinas y da una píldora; UI Toolkit no lo hace y dibuja un óvalo. Para los
    /// controles cuyo alto cambia (crecen con la fuente grande), el radio se pone en cada cambio de tamaño a la mitad
    /// del lado menor.
    /// </summary>
    public static class Semicirculo
    {
        /// <summary>Radio que deja los extremos de ese tamaño en semicírculo: la mitad del lado menor.</summary>
        public static float Radio(Vector2 tamano) => Mathf.Max(0f, Mathf.Min(tamano.x, tamano.y) / 2f);

        /// <summary>Mantiene el radio de las cuatro esquinas a la mitad del lado menor del elemento.</summary>
        public static void Mantener(VisualElement elemento) =>
            elemento.RegisterCallback<GeometryChangedEvent>(e => Aplicar(elemento, e.newRect.size));

        /// <summary>Pone el radio para ese tamaño (las pruebas lo llaman sin panel).</summary>
        public static void Aplicar(VisualElement elemento, Vector2 tamano)
        {
            if (float.IsNaN(tamano.x) || float.IsNaN(tamano.y)) return;
            var r = new StyleLength(Radio(tamano));
            elemento.style.borderTopLeftRadius = r;
            elemento.style.borderTopRightRadius = r;
            elemento.style.borderBottomLeftRadius = r;
            elemento.style.borderBottomRightRadius = r;
        }
    }
}
