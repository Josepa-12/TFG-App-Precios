using System;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Píldora "Precios" arriba a la izquierda (DESIGN.md · prices-pill): cristal, punto de color de la
    /// opción elegida, su texto y una flecha hacia abajo. Al pulsarla abre o cierra el menú.
    /// Con Ninguno dice "Ver precios" con el texto atenuado.
    /// </summary>
    public class PildoraPrecios : VisualElement
    {
        readonly Func<string, string> textos;
        readonly VisualElement flecha;

        public PuntoPrecios Punto { get; }
        public Label Texto { get; }
        public FiltroPrecios Filtro { get; private set; }

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public PildoraPrecios(Action alPulsar, Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "pildora-precios";
            AddToClassList("pildora-precios");
            focusable = true;
            pickingMode = PickingMode.Position;
            this.AddManipulator(new Clickable(alPulsar));
            RegisterCallback<NavigationSubmitEvent>(_ => alPulsar()); // teclado, mando o navegación
            SombraSuave.Poner(this); // cristal con borde glass-edge (USS) y la sombra suave de los controles
            Semicirculo.Mantener(this); // extremos semicirculares también si crece con la fuente grande

            Punto = new PuntoPrecios(FiltroPrecios.Ninguno);
            Add(Punto);
            Texto = new Label { name = "pildora-precios-texto", pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("pildora-precios__texto");
            Texto.AddToClassList("t-label");
            Add(Texto);
            flecha = new VisualElement { pickingMode = PickingMode.Ignore };
            flecha.AddToClassList("pildora-precios__flecha");
            flecha.generateVisualContent += DibujarFlecha;
            flecha.RegisterCallback<GeometryChangedEvent>(_ => flecha.MarkDirtyRepaint());
            Add(flecha);

            Mostrar(FiltroPrecios.Ninguno);
        }

        /// <summary>Pone el texto y el punto de la opción elegida.</summary>
        public void Mostrar(FiltroPrecios filtro)
        {
            Filtro = filtro;
            Texto.text = textos(TextosFiltro.ClavePildora(filtro));
            Punto.Filtro = filtro;
            EnableInClassList("pildora-precios--ninguno", filtro == FiltroPrecios.Ninguno);
        }

        /// <summary>Vuelve a dibujar el punto y la flecha (al cambiar de tema).</summary>
        public void Repintar()
        {
            Punto.MarkDirtyRepaint();
            flecha.MarkDirtyRepaint();
        }

        void DibujarFlecha(MeshGenerationContext mgc)
        {
            var r = flecha.contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            p.strokeColor = flecha.resolvedStyle.color;
            p.lineWidth = 2f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            // Chevron hacia abajo dentro del rectángulo, con 1 dp de margen para el grosor de la línea
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin + 1f, r.yMin + 1f));
            p.LineTo(new Vector2(r.center.x, r.yMax - 1f));
            p.LineTo(new Vector2(r.xMax - 1f, r.yMin + 1f));
            p.Stroke();
        }
    }
}
