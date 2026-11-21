using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Aviso breve (toast; DESIGN.md · Toast, UX-DR15): fondo text, texto bg, 12 dp de radio, centrado arriba; dura 3 s
    /// y se va sin animación. TalkBack lo anuncia al mostrarse (lo pide quien lo muestra, con AccesibilidadPantalla.
    /// Anunciar) y nunca es la única vía de algo imprescindible. No se toca: los toques pasan al mapa. Reutilizable (la
    /// historia 2.6, parte 2b, lo estrena bajo la tarjeta de la ruta; la 3.3 lo usa también). Colocar fija su borde de
    /// arriba en el elemento que lo contiene.
    /// </summary>
    public class Toast : VisualElement
    {
        /// <summary>Lo que dura a la vista (ms): 3 s.</summary>
        public const long DuracionMs = 3000;

        public Label Texto { get; }

        IVisualElementScheduledItem ocultar;

        public Toast()
        {
            name = "toast";
            AddToClassList("toast");
            pickingMode = PickingMode.Ignore;
            Texto = new Label { name = "toast-texto", pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("toast__texto");
            Texto.AddToClassList("t-label");
            Add(Texto);
            style.display = DisplayStyle.None;
        }

        /// <summary>Se ve ahora.</summary>
        public bool Visible => style.display.value == DisplayStyle.Flex;

        /// <summary>Lo enseña con ese texto durante 3 s (otro toast antes de tiempo lo sustituye y vuelve a contar).</summary>
        public void Mostrar(string texto)
        {
            Texto.text = texto ?? "";
            style.display = DisplayStyle.Flex;
            ocultar?.Pause();
            ocultar = schedule.Execute(Ocultar).StartingIn(DuracionMs);
        }

        /// <summary>Lo quita al momento.</summary>
        public void Ocultar()
        {
            ocultar?.Pause();
            ocultar = null;
            style.display = DisplayStyle.None;
        }

        /// <summary>Borde de arriba (dp) dentro de su contenedor.</summary>
        public void Colocar(float arribaDp) => style.top = arribaDp;
    }
}
