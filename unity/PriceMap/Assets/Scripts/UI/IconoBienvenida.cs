using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    public enum GlifoBienvenida { Mapa, Personas, Navegar, Pin, Flecha, Persona, Campana, Hecho, Atras }

    /// <summary>
    /// Iconos de la bienvenida (historia 3.1; maqueta A del 23/11/2026), dibujados con vectores en su caja (viewBox de 24)
    /// con el color del elemento (USS color).
    /// </summary>
    public class IconoBienvenida : VisualElement
    {
        public GlifoBienvenida Glifo { get; }

        public IconoBienvenida(GlifoBienvenida glifo)
        {
            Glifo = glifo;
            name = "bienvenida-icono-" + glifo.ToString().ToLowerInvariant();
            AddToClassList("bienvenida__glifo");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Dibujar;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        void Dibujar(MeshGenerationContext mgc)
        {
            var r = contentRect;
            float s = Mathf.Min(r.width, r.height) / 24f;
            if (float.IsNaN(s) || s <= 0f) return;
            var c = r.center;
            Vector2 V(float x, float y) => c + new Vector2(x - 12f, y - 12f) * s;
            var p = mgc.painter2D;
            p.strokeColor = resolvedStyle.color;
            p.lineWidth = (Glifo == GlifoBienvenida.Hecho ? 3f : 2f) * s;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            void Linea(bool cerrada, params Vector2[] puntos)
            {
                p.BeginPath();
                p.MoveTo(puntos[0]);
                for (int i = 1; i < puntos.Length; i++) p.LineTo(puntos[i]);
                if (cerrada) p.ClosePath();
                p.Stroke();
            }
            void Circulo(float x, float y, float radio)
            {
                p.BeginPath();
                p.Arc(V(x, y), radio * s, 0f, 360f);
                p.ClosePath();
                p.Stroke();
            }

            switch (Glifo)
            {
                case GlifoBienvenida.Mapa:
                    Linea(true, V(3, 6), V(9, 4), V(15, 6), V(21, 4), V(21, 18), V(15, 20), V(9, 18), V(3, 20));
                    Linea(false, V(9, 4), V(9, 18));
                    Linea(false, V(15, 6), V(15, 20));
                    break;
                case GlifoBienvenida.Personas:
                    Circulo(9, 8, 3.5f);
                    p.BeginPath();
                    p.MoveTo(V(2.5f, 20));
                    p.BezierCurveTo(V(2.5f, 16), V(5.5f, 14), V(9, 14));
                    p.BezierCurveTo(V(12.5f, 14), V(15.5f, 16), V(15.5f, 20));
                    p.Stroke();
                    // La segunda persona asoma por detrás: media cabeza (arco de radio 3,5 entre (16; 5) y (16; 11,5)) y el hombro
                    p.BeginPath();
                    p.Arc(V(16f - Mathf.Sqrt(3.5f * 3.5f - 3.25f * 3.25f), 8.25f), 3.5f * s, -68.2f, 68.2f, ArcDirection.Clockwise);
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(V(18.5f, 14.5f));
                    p.BezierCurveTo(V(20.5f, 15.3f), V(21.5f, 17.2f), V(21.5f, 20));
                    p.Stroke();
                    break;
                case GlifoBienvenida.Navegar:
                    Linea(true, V(21, 3), V(3, 10.5f), V(11, 13), V(13.5f, 21));
                    break;
                case GlifoBienvenida.Pin:
                    p.BeginPath();
                    p.MoveTo(V(12, 22));
                    p.BezierCurveTo(V(12, 22), V(19, 15.5f), V(19, 10));
                    p.Arc(V(12, 10), 7f * s, 0f, -180f, ArcDirection.CounterClockwise);
                    p.BezierCurveTo(V(5, 15.5f), V(12, 22), V(12, 22));
                    p.ClosePath();
                    p.Stroke();
                    Circulo(12, 10, 2.5f);
                    break;
                case GlifoBienvenida.Flecha:
                    Linea(false, V(4, 12), V(20, 12));
                    Linea(false, V(14, 6), V(20, 12), V(14, 18));
                    break;
                case GlifoBienvenida.Persona:
                    Circulo(12, 8, 4);
                    p.BeginPath();
                    p.MoveTo(V(4, 21));
                    p.BezierCurveTo(V(4, 16.5f), V(7.5f, 14), V(12, 14));
                    p.BezierCurveTo(V(16.5f, 14), V(20, 16.5f), V(20, 21));
                    p.Stroke();
                    break;
                case GlifoBienvenida.Campana:
                    p.BeginPath();
                    p.MoveTo(V(6, 16));
                    p.LineTo(V(6, 11));
                    p.Arc(V(12, 11), 6f * s, 180f, 360f, ArcDirection.Clockwise);
                    p.LineTo(V(18, 16));
                    p.LineTo(V(20, 18));
                    p.LineTo(V(4, 18));
                    p.ClosePath();
                    p.Stroke();
                    Linea(false, V(10, 21), V(14, 21));
                    break;
                case GlifoBienvenida.Hecho:
                    Linea(false, V(5, 13), V(10, 18), V(19, 7));
                    break;
                case GlifoBienvenida.Atras:
                    Linea(false, V(15, 5), V(8, 12), V(15, 19));
                    break;
            }
        }
    }
}
