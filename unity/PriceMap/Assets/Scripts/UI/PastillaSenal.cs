using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Pastilla "Buscando señal GPS" (historia 2.7, parte 2c; UX-DR16): una línea de texto pequeño, centrada bajo la
    /// tarjeta de arriba, con el estilo del toast (fondo text, texto bg) pero más baja y sin irse sola: está mientras la
    /// navegación no tiene señal. TalkBack lo anuncia quien la muestra (AccesibilidadPantalla.Anunciar). No se toca: los
    /// toques pasan al mapa. Colocar fija su borde de arriba en el elemento que la contiene.
    /// </summary>
    public class PastillaSenal : VisualElement
    {
        /// <summary>Alto supuesto (dp) mientras no tiene medida (texto de 12 sp y 4 dp de relleno arriba y abajo).</summary>
        public const float AltoSupuestoDp = 26f;

        public Label Texto { get; }

        public PastillaSenal()
        {
            name = "pastilla-senal";
            AddToClassList("pastilla-senal");
            pickingMode = PickingMode.Ignore;
            Texto = new Label { name = "pastilla-senal-texto", pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("pastilla-senal__texto");
            Texto.AddToClassList("t-caption");
            Add(Texto);
            style.display = DisplayStyle.None;
        }

        /// <summary>Se ve ahora.</summary>
        public bool Visible => style.display.value == DisplayStyle.Flex;

        /// <summary>La enseña con ese texto hasta que se oculte.</summary>
        public void Mostrar(string texto)
        {
            Texto.text = texto ?? "";
            style.display = DisplayStyle.Flex;
        }

        public void Ocultar() => style.display = DisplayStyle.None;

        /// <summary>Borde de arriba (dp) dentro de su contenedor.</summary>
        public void Colocar(float arribaDp) => style.top = arribaDp;

        /// <summary>Borde de abajo (dp) dentro de su contenedor: medido o, sin medida, con el alto supuesto.</summary>
        public float Abajo
        {
            get
            {
                var l = Texto.layout;
                float alto = !float.IsNaN(l.height) && l.height > 0f ? l.height : AltoSupuestoDp;
                return style.top.value.value + alto;
            }
        }
    }
}
