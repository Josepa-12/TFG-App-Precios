using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Punto de color de una opción del filtro "Precios", dibujado con Painter2D: aro hueco con Ninguno,
    /// color de la plantilla (--color-pin-*) con una plantilla y acento con Todos. El color llega por la
    /// clase USS (PantallaMapa.uss · .punto-precios--*), así que cambia con el tema.
    /// </summary>
    public class PuntoPrecios : VisualElement
    {
        FiltroPrecios filtro;

        public FiltroPrecios Filtro
        {
            get => filtro;
            set
            {
                RemoveFromClassList(Clase(filtro));
                filtro = value;
                AddToClassList(Clase(filtro));
                MarkDirtyRepaint();
            }
        }

        public PuntoPrecios(FiltroPrecios filtro)
        {
            AddToClassList("punto-precios");
            pickingMode = PickingMode.Ignore;
            this.filtro = filtro;
            AddToClassList(Clase(filtro));
            generateVisualContent += Dibujar;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        /// <summary>Clase USS que da el color del punto.</summary>
        public static string Clase(FiltroPrecios filtro)
        {
            switch (filtro)
            {
                case FiltroPrecios.Todos: return "punto-precios--todos";
                case FiltroPrecios.ServiciosLocales: return "punto-precios--servicio";
                case FiltroPrecios.Hosteleria: return "punto-precios--hosteleria";
                case FiltroPrecios.Ocio: return "punto-precios--ocio";
                default: return "punto-precios--ninguno";
            }
        }

        void Dibujar(MeshGenerationContext mgc)
        {
            var r = contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            var color = resolvedStyle.color;
            float radio = Mathf.Min(r.width, r.height) / 2f;
            p.BeginPath();
            if (filtro == FiltroPrecios.Ninguno)
            {
                // Aro hueco: sin precios en el mapa
                p.strokeColor = color;
                p.lineWidth = 2f;
                p.Arc(r.center, radio - 1f, 0f, 360f);
                p.Stroke();
            }
            else
            {
                p.fillColor = color;
                p.Arc(r.center, radio, 0f, 360f);
                p.Fill();
            }
        }
    }
}
