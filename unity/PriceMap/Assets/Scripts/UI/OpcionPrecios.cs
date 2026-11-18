using System;
using PriceMap.Dominio;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Una opción del menú "Precios": punto de color y nombre (sin contador, decisión del 17/11/2026).
    /// La elegida se marca solo con el fondo surface-2, sin check (prototipo .cat-menu button.on); TalkBack la
    /// sigue leyendo como seleccionada.
    /// </summary>
    public class OpcionPrecios : VisualElement
    {
        readonly Action<FiltroPrecios> alElegir;

        public FiltroPrecios Filtro { get; }
        public Label Texto { get; }
        public bool Elegida { get; private set; }

        public OpcionPrecios(FiltroPrecios filtro, string texto, Action<FiltroPrecios> alElegir)
        {
            Filtro = filtro;
            name = "opcion-precios-" + ReglaFiltro.AClave(filtro);
            AddToClassList("opcion-precios");
            focusable = true;
            pickingMode = PickingMode.Position;
            this.alElegir = alElegir;
            this.AddManipulator(new Clickable(Pulsar));
            RegisterCallback<NavigationSubmitEvent>(_ => Pulsar());

            Add(new PuntoPrecios(filtro));
            Texto = new Label(texto) { pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("opcion-precios__texto");
            Texto.AddToClassList("t-label");
            Add(Texto);
        }

        /// <summary>Lo mismo que tocarla: elige su filtro.</summary>
        public void Pulsar() => alElegir?.Invoke(Filtro);

        public void Marcar(bool elegida)
        {
            Elegida = elegida;
            EnableInClassList("opcion-precios--elegida", elegida);
        }

        public void Repintar()
        {
            foreach (var hijo in Children()) hijo.MarkDirtyRepaint();
        }
    }
}
