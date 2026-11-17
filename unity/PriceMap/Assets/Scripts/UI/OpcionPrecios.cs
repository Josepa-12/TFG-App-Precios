using System;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Una opción del menú "Precios": punto de color y nombre (sin contador, decisión del 17/11/2026).
    /// La elegida lleva fondo surface-2 y un check.
    /// </summary>
    public class OpcionPrecios : VisualElement
    {
        readonly VisualElement check;
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
            Texto.AddToClassList("t-body");
            Add(Texto);
            check = new VisualElement { pickingMode = PickingMode.Ignore };
            check.AddToClassList("opcion-precios__check");
            check.generateVisualContent += DibujarCheck;
            check.RegisterCallback<GeometryChangedEvent>(_ => check.MarkDirtyRepaint());
            Add(check);
        }

        /// <summary>El check es visible solo en la elegida (sin cambiar el ancho de la fila).</summary>
        public VisualElement Check => check;

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

        void DibujarCheck(MeshGenerationContext mgc)
        {
            var r = check.contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            p.strokeColor = check.resolvedStyle.color;
            p.lineWidth = 2.5f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin + r.width * 0.12f, r.yMin + r.height * 0.52f));
            p.LineTo(new Vector2(r.xMin + r.width * 0.40f, r.yMin + r.height * 0.80f));
            p.LineTo(new Vector2(r.xMin + r.width * 0.88f, r.yMin + r.height * 0.22f));
            p.Stroke();
        }
    }
}
