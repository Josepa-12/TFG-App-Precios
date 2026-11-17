using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    public enum Icono { Sol, Luna, Mas, Menos }

    /// <summary>
    /// Botón redondo de 48 dp con fondo de cristal (DESIGN.md · round-button) y un icono dibujado
    /// con vectores, nunca emojis (UX-DR23). El icono usa el color de texto del tema.
    /// </summary>
    public class BotonRedondo : VisualElement
    {
        Icono icono;

        public Icono Icono
        {
            get => icono;
            set { icono = value; MarkDirtyRepaint(); }
        }

        public BotonRedondo(Icono icono, Action alPulsar)
        {
            this.icono = icono;
            AddToClassList("boton-redondo");
            focusable = true;
            pickingMode = PickingMode.Position;
            this.AddManipulator(new Clickable(alPulsar));
            RegisterCallback<NavigationSubmitEvent>(_ => alPulsar()); // teclado, mando o navegación
            generateVisualContent += Dibujar;
            // El color cambia con el tema: volver a dibujar el icono cuando cambie el estilo
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        void Dibujar(MeshGenerationContext mgc)
        {
            var p = mgc.painter2D;
            var r = contentRect;
            var c = r.center;
            var color = resolvedStyle.color;
            p.strokeColor = color;
            p.fillColor = color;
            p.lineWidth = 2f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            switch (icono)
            {
                case Icono.Sol: Sol(p, c); break;
                case Icono.Luna: Luna(p, c); break;
                case Icono.Mas: Linea(p, c + new Vector2(-7, 0), c + new Vector2(7, 0)); Linea(p, c + new Vector2(0, -7), c + new Vector2(0, 7)); break;
                case Icono.Menos: Linea(p, c + new Vector2(-7, 0), c + new Vector2(7, 0)); break;
            }
        }

        static void Linea(Painter2D p, Vector2 a, Vector2 b)
        {
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(b);
            p.Stroke();
        }

        static void Sol(Painter2D p, Vector2 c)
        {
            p.BeginPath();
            p.Arc(c, 4.5f, 0f, 360f);
            p.Fill();
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Linea(p, c + d * 7.5f, c + d * 10f);
            }
        }

        // Luna creciente: arco exterior por la izquierda y arco interior desplazado a la derecha
        static void Luna(Painter2D p, Vector2 c)
        {
            const float rExt = 8f, rInt = 6.5f;
            var cInt = c + new Vector2(4.5f, -2.5f);
            // Puntos donde se cortan los dos círculos
            var (a, b) = Cortes(c, rExt, cInt, rInt);
            float angA = Grados(a - c), angB = Grados(b - c);
            float angAi = Grados(a - cInt), angBi = Grados(b - cInt);
            p.BeginPath();
            p.Arc(c, rExt, angA, angB, ArcDirection.CounterClockwise);
            p.Arc(cInt, rInt, angBi, angAi, ArcDirection.Clockwise);
            p.ClosePath();
            p.Fill();
        }

        static float Grados(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        static (Vector2, Vector2) Cortes(Vector2 c0, float r0, Vector2 c1, float r1)
        {
            var d = c1 - c0;
            float dist = d.magnitude;
            float a = (r0 * r0 - r1 * r1 + dist * dist) / (2f * dist);
            float h = Mathf.Sqrt(Mathf.Max(0f, r0 * r0 - a * a));
            var m = c0 + d * (a / dist);
            var n = new Vector2(-d.y, d.x) / dist * h;
            return (m + n, m - n);
        }
    }
}
