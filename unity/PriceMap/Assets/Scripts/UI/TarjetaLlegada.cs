using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// "Has llegado" arriba (historia 2.9; maqueta B del 23/11/2026), en el sitio y con los colores de la tarjeta de
    /// acciones: la marca de hecho en un círculo verde, "Has llegado" y el nombre del destino. TalkBack la lee como texto
    /// (Anuncio: "Has llegado a Clínica Areas").
    /// </summary>
    public class TarjetaLlegada : VisualElement
    {
        readonly Func<string, string> textos;

        /// <summary>Círculo con la marca de hecho.</summary>
        public VisualElement Marca { get; }
        public Label Titulo { get; }
        public Label Destino { get; }

        /// <summary>Lo que lee TalkBack: "Has llegado a Clínica Areas".</summary>
        public string Anuncio { get; private set; } = "";

        public TarjetaLlegada(Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "tarjeta-llegada";
            AddToClassList("tarjeta-maniobra"); // el mismo sitio y los mismos colores
            AddToClassList("tarjeta-llegada");
            pickingMode = PickingMode.Position; // arrastrar sobre la tarjeta no mueve el mapa

            Marca = new VisualElement { name = "tarjeta-llegada-marca", pickingMode = PickingMode.Ignore };
            Marca.AddToClassList("tarjeta-llegada__marca");
            Marca.generateVisualContent += DibujarMarca;
            Marca.RegisterCallback<GeometryChangedEvent>(_ => Marca.MarkDirtyRepaint());
            Marca.RegisterCallback<CustomStyleResolvedEvent>(_ => Marca.MarkDirtyRepaint());
            Add(Marca);
            var columna = new VisualElement { name = "tarjeta-llegada-textos", pickingMode = PickingMode.Ignore };
            columna.AddToClassList("tarjeta-llegada__textos");
            Add(columna);
            Titulo = new Label(this.textos(ClavesTexto.LlegadaTitulo)) { name = "tarjeta-llegada-titulo", pickingMode = PickingMode.Ignore };
            Titulo.AddToClassList("tarjeta-llegada__titulo");
            Titulo.AddToClassList("t-maniobra");
            columna.Add(Titulo);
            Destino = new Label { name = "tarjeta-llegada-destino", pickingMode = PickingMode.Ignore };
            Destino.AddToClassList("tarjeta-llegada__destino");
            Destino.AddToClassList("t-llegada");
            columna.Add(Destino);
            style.display = DisplayStyle.None;
        }

        /// <summary>El destino al que se ha llegado.</summary>
        public void Poner(string destino)
        {
            Destino.text = destino ?? "";
            Destino.style.display = string.IsNullOrEmpty(destino) ? DisplayStyle.None : DisplayStyle.Flex;
            Anuncio = Textos.Formatear(textos(ClavesTexto.LlegadaA), Destino.text);
        }

        /// <summary>Vuelve a dibujar la marca (el color cambia con el tema).</summary>
        public void Repintar() => Marca.MarkDirtyRepaint();

        // Maqueta: "M5 13l5 5 9-11" en 24 × 24 con trazo 3, en el color (USS color) de la marca y al 54 % del círculo
        void DibujarMarca(MeshGenerationContext mgc)
        {
            var r = Marca.contentRect;
            if (float.IsNaN(r.width) || r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            float k = Mathf.Min(r.width, r.height) * 0.54f / 24f;
            Vector2 V(float x, float y) => r.center + new Vector2(x - 12f, y - 12f) * k;
            p.strokeColor = Marca.resolvedStyle.color;
            p.lineWidth = 3f * k;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(V(5f, 13f));
            p.LineTo(V(10f, 18f));
            p.LineTo(V(19f, 7f));
            p.Stroke();
        }
    }
}
