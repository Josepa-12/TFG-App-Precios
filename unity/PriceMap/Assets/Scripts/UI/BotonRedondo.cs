using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    public enum Icono { Sol, Luna, Mas, Menos, Navegar, Cerrar }

    /// <summary>
    /// Botón redondo de 48 dp con fondo de cristal (DESIGN.md · round-button) y un icono dibujado
    /// con vectores, nunca emojis (UX-DR23). El icono usa el color (USS color) del botón.
    /// En columna (tema, ubicación y zoom; prototipo .ctrl-stack) es un hueco de 48 × 48 dp sin fondo ni borde
    /// propios: el cristal, el borde y la sombra los pone la columna, y el icono va en --color-accent-text.
    /// Sol, Luna y Navegar son los iconos del prototipo (viewBox de 24 con trazo 2) dibujados a 22 dp.
    /// </summary>
    public class BotonRedondo : VisualElement
    {
        Icono icono;
        readonly Action alPulsar;

        public Icono Icono
        {
            get => icono;
            set { icono = value; MarkDirtyRepaint(); }
        }

        /// <summary>Lado de los iconos del prototipo (Sol, Luna, Navegar), en dp.</summary>
        public const float TamanoIcono = 22f;
        public const string ClaseColumna = "boton-redondo--columna";
        /// <summary>Segundo hueco de una columna: línea de 1 dp en --color-border encima.</summary>
        public const string ClaseSeparado = "boton-redondo--separado";

        /// <param name="enColumna">Hueco de la columna de controles, sin fondo ni borde propios.</param>
        public BotonRedondo(Icono icono, Action alPulsar, bool enColumna = false)
        {
            this.icono = icono;
            this.alPulsar = alPulsar;
            AddToClassList("boton-redondo");
            if (enColumna) AddToClassList(ClaseColumna);
            focusable = true;
            pickingMode = PickingMode.Position;
            this.AddManipulator(new Clickable(alPulsar));
            RegisterCallback<NavigationSubmitEvent>(_ => alPulsar()); // teclado, mando o navegación
            generateVisualContent += Dibujar;
            // El color cambia con el tema: volver a dibujar el icono cuando cambie el estilo
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        /// <summary>Lo que hace el botón al tocarlo (también lo usan las pruebas y TalkBack).</summary>
        public void Pulsar() => alPulsar?.Invoke();

        /// <summary>True si es un hueco de la columna de controles.</summary>
        public bool EnColumna => ClassListContains(ClaseColumna);

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
                case Icono.Navegar: Navegar(p, c); break;
                case Icono.Mas: Linea(p, c + new Vector2(-7, 0), c + new Vector2(7, 0)); Linea(p, c + new Vector2(0, -7), c + new Vector2(0, 7)); break;
                case Icono.Menos: Linea(p, c + new Vector2(-7, 0), c + new Vector2(7, 0)); break;
                // Aspa de cerrar (ficha del lugar, historia 1.8)
                case Icono.Cerrar: Linea(p, c + new Vector2(-6, -6), c + new Vector2(6, 6)); Linea(p, c + new Vector2(-6, 6), c + new Vector2(6, -6)); break;
            }
        }

        static void Linea(Painter2D p, Vector2 a, Vector2 b)
        {
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(b);
            p.Stroke();
        }

        // Punto del viewBox de 24 del prototipo (centro en 12, 12) llevado a un icono de 22 dp centrado en c
        static Vector2 V(Vector2 c, float x, float y) => c + new Vector2(x - 12f, y - 12f) * (TamanoIcono / 24f);

        static void Trazo(Painter2D p) => p.lineWidth = 2f * TamanoIcono / 24f;

        // Sol del prototipo: círculo de radio 4 y ocho rayos de 8 a 10
        static void Sol(Painter2D p, Vector2 c)
        {
            Trazo(p);
            float s = TamanoIcono / 24f;
            p.BeginPath();
            p.Arc(c, 4f * s, 0f, 360f);
            p.ClosePath();
            p.Stroke();
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Linea(p, c + d * 8f * s, c + d * 10f * s);
            }
        }

        // Luna del prototipo ("M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"): el primer arco no llega entre sus dos puntos
        // (distan 12,73), así que SVG lo agranda a una semicircunferencia de centro (16,5; 7,5) que pasa por (12, 12);
        // el segundo es el círculo de radio 9 centrado en el icono, de (21, 12) a (12, 3) por abajo y la izquierda.
        static void Luna(Painter2D p, Vector2 c)
        {
            Trazo(p);
            float s = TamanoIcono / 24f;
            p.BeginPath();
            p.MoveTo(V(c, 12f, 3f));
            p.Arc(V(c, 16.5f, 7.5f), 4.5f * Mathf.Sqrt(2f) * s, -135f, 45f, ArcDirection.CounterClockwise);
            p.Arc(c, 9f * s, 0f, 270f, ArcDirection.Clockwise);
            p.ClosePath();
            p.Stroke();
        }

        // Flecha de navegación del prototipo (polygon 3,11 22,2 13,21 11,13)
        static void Navegar(Painter2D p, Vector2 c)
        {
            Trazo(p);
            p.BeginPath();
            p.MoveTo(V(c, 3f, 11f));
            p.LineTo(V(c, 22f, 2f));
            p.LineTo(V(c, 13f, 21f));
            p.LineTo(V(c, 11f, 13f));
            p.ClosePath();
            p.Stroke();
        }
    }
}
