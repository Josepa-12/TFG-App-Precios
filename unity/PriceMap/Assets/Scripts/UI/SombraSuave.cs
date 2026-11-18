using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Sombra suave de los controles flotantes (prototipo --shadow: 0 4px 16px). UI Toolkit no tiene box-shadow y los
    /// filtros de USS de Unity 6000.3 no traen drop-shadow, así que se dibuja una malla propia en el contenido del
    /// control: un anillo alrededor de su rectángulo redondeado cuya opacidad, en cada vértice, sale de la distancia
    /// al mismo rectángulo desplazado 4 dp hacia abajo, con una caída gaussiana (σ = 8 dp, la mitad del desenfoque
    /// de CSS). Como en CSS, la sombra no se pinta debajo del control: el cristal translúcido no se oscurece.
    /// El color (con su opacidad) sale de la propiedad --sombra del control (clase .sombra-suave → --color-sombra).
    /// Con <c>conBorde</c> dibuja además un borde de 1 dp por dentro con --borde-cristal, sin ocupar sitio en la
    /// disposición (la columna de controles mide así 48 × 96 dp con sus dos huecos de 48 × 48).
    /// </summary>
    public sealed class SombraSuave
    {
        public const string Clase = "sombra-suave";
        public const float DesplazamientoY = 4f;
        public const float Desenfoque = 16f;
        const float Sigma = Desenfoque / 2f;

        static readonly CustomStyleProperty<Color> PropSombra = new CustomStyleProperty<Color>("--sombra");
        static readonly CustomStyleProperty<Color> PropBorde = new CustomStyleProperty<Color>("--borde-cristal");
        // Contornos del anillo: distancia (dp) al borde del control; más juntos cerca, donde la sombra cambia más
        static readonly float[] Contornos = { 0f, 1f, 2f, 3f, 4f, 6f, 8f, 10f, 12f, 15f, 18f, 22f, 26f };
        const int TramosEsquina = 8;

        static readonly ConditionalWeakTable<VisualElement, SombraSuave> puestas = new ConditionalWeakTable<VisualElement, SombraSuave>();

        readonly VisualElement elemento;
        readonly bool conBorde;

        /// <summary>Color de la sombra resuelto del tema (transparente hasta que se resuelve el estilo).</summary>
        public Color Color { get; private set; } = Color.clear;
        public Color Borde { get; private set; } = Color.clear;

        SombraSuave(VisualElement elemento, bool conBorde)
        {
            this.elemento = elemento;
            this.conBorde = conBorde;
            elemento.AddToClassList(Clase);
            puestas.Add(elemento, this);
            elemento.generateVisualContent += Dibujar;
            elemento.RegisterCallback<GeometryChangedEvent>(_ => elemento.MarkDirtyRepaint());
            // Al cambiar de tema cambian --color-sombra y --color-glass-edge: se vuelven a leer y se repinta
            elemento.RegisterCallback<CustomStyleResolvedEvent>(_ => LeerColores());
        }

        /// <summary>Pone la sombra al control (y, si se pide, el borde de cristal de 1 dp por dentro).</summary>
        public static SombraSuave Poner(VisualElement elemento, bool conBorde = false) => new SombraSuave(elemento, conBorde);

        /// <summary>La sombra puesta a ese control, o null (la captura de PC comprueba con ella los colores resueltos).</summary>
        public static SombraSuave De(VisualElement elemento) =>
            elemento != null && puestas.TryGetValue(elemento, out var sombra) ? sombra : null;

        /// <summary>True si dibuja también el borde de cristal.</summary>
        public bool ConBorde => conBorde;

        /// <summary>True si el control lleva la sombra suave.</summary>
        public static bool Tiene(VisualElement elemento) => elemento != null && elemento.ClassListContains(Clase);

        void LeerColores()
        {
            var estilo = elemento.customStyle;
            Color = estilo.TryGetValue(PropSombra, out var s) ? s : Color.clear;
            Borde = estilo.TryGetValue(PropBorde, out var b) ? b : Color.clear;
            elemento.MarkDirtyRepaint();
        }

        void Dibujar(MeshGenerationContext mgc)
        {
            float w = elemento.layout.width, h = elemento.layout.height;
            if (float.IsNaN(w) || float.IsNaN(h) || w <= 0f || h <= 0f) return;
            var rect = new Rect(0f, 0f, w, h);
            float radio = Radio(elemento.resolvedStyle.borderTopLeftRadius, w, h);
            if (Color.a > 0f) Anillo(mgc, rect, radio, Color);
            if (conBorde && Borde.a > 0f)
            {
                var p = mgc.painter2D;
                p.strokeColor = Borde;
                p.lineWidth = 1f;
                RectanguloRedondeado(p, new Rect(0.5f, 0.5f, w - 1f, h - 1f), Mathf.Max(0f, radio - 0.5f));
                p.Stroke();
            }
        }

        static float Radio(float pedido, float w, float h) => Mathf.Clamp(pedido, 0f, Mathf.Min(w, h) / 2f);

        /// <summary>
        /// Opacidad de la sombra en el punto p (coordenadas del control) para un control con ese rectángulo y radio,
        /// con color de opacidad <paramref name="alfa"/>: la de un rectángulo redondeado desplazado 4 dp hacia abajo y
        /// desenfocado con σ = 8 dp. Dentro del control no se usa (la malla solo cubre el anillo de fuera).
        /// </summary>
        internal static float Alfa(Vector2 p, Rect control, float radio, float alfa)
        {
            var centro = control.center + new Vector2(0f, DesplazamientoY);
            float d = DistanciaRectanguloRedondeado(p - centro, control.size / 2f, radio);
            return alfa * 0.5f * Erfc(d / (Sigma * Mathf.Sqrt(2f)));
        }

        /// <summary>Distancia con signo de p (relativo al centro) a un rectángulo redondeado de semitamaño b y radio r.</summary>
        static float DistanciaRectanguloRedondeado(Vector2 p, Vector2 b, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b + new Vector2(r, r);
            var fuera = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return fuera.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        // Función de error complementaria (Abramowitz y Stegun 7.1.26, error < 1,5e-7)
        static float Erfc(float x)
        {
            float z = Mathf.Abs(x);
            float t = 1f / (1f + 0.3275911f * z);
            float y = t * (0.254829592f + t * (-0.284496736f + t * (1.421413741f + t * (-1.453152027f + t * 1.061405429f)))) * Mathf.Exp(-z * z);
            return x >= 0f ? y : 2f - y;
        }

        /// <summary>Contorno del control ampliado <paramref name="e"/> dp, en sentido horario empezando por la esquina superior izquierda.</summary>
        static void Contorno(Rect r, float radio, float e, Vector2[] destino)
        {
            var centros = new[]
            {
                new Vector2(r.xMin + radio, r.yMin + radio), new Vector2(r.xMax - radio, r.yMin + radio),
                new Vector2(r.xMax - radio, r.yMax - radio), new Vector2(r.xMin + radio, r.yMax - radio),
            };
            int i = 0;
            for (int esquina = 0; esquina < 4; esquina++)
            {
                float desde = 180f + 90f * esquina; // con y hacia abajo, el ángulo crece en sentido horario
                for (int k = 0; k <= TramosEsquina; k++)
                {
                    float a = (desde + 90f * k / TramosEsquina) * Mathf.Deg2Rad;
                    destino[i++] = centros[esquina] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radio + e);
                }
            }
        }

        static void Anillo(MeshGenerationContext mgc, Rect rect, float radio, Color color)
        {
            int porContorno = 4 * (TramosEsquina + 1);
            int contornos = Contornos.Length;
            var vertices = new Vertex[porContorno * contornos];
            var puntos = new Vector2[porContorno];
            for (int c = 0; c < contornos; c++)
            {
                Contorno(rect, radio, Contornos[c], puntos);
                for (int i = 0; i < porContorno; i++)
                {
                    var tinte = color;
                    tinte.a = Alfa(puntos[i], rect, radio, color.a);
                    vertices[c * porContorno + i] = new Vertex { position = new Vector3(puntos[i].x, puntos[i].y, Vertex.nearZ), tint = tinte };
                }
            }

            var indices = new ushort[(contornos - 1) * porContorno * 6];
            int n = 0;
            for (int c = 0; c < contornos - 1; c++)
                for (int i = 0; i < porContorno; i++)
                {
                    int j = (i + 1) % porContorno;
                    ushort dentroI = (ushort)(c * porContorno + i), dentroJ = (ushort)(c * porContorno + j);
                    ushort fueraI = (ushort)((c + 1) * porContorno + i), fueraJ = (ushort)((c + 1) * porContorno + j);
                    // Triángulos en sentido horario (lo que pide MeshGenerationContext)
                    indices[n++] = dentroI; indices[n++] = fueraI; indices[n++] = fueraJ;
                    indices[n++] = dentroI; indices[n++] = fueraJ; indices[n++] = dentroJ;
                }

            var malla = mgc.Allocate(vertices.Length, indices.Length);
            malla.SetAllVertices(vertices);
            malla.SetAllIndices(indices);
        }

        static void RectanguloRedondeado(Painter2D p, Rect r, float radio)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin + radio, r.yMin));
            p.LineTo(new Vector2(r.xMax - radio, r.yMin));
            p.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + radio), radio);
            p.LineTo(new Vector2(r.xMax, r.yMax - radio));
            p.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - radio, r.yMax), radio);
            p.LineTo(new Vector2(r.xMin + radio, r.yMax));
            p.ArcTo(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - radio), radio);
            p.LineTo(new Vector2(r.xMin, r.yMin + radio));
            p.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + radio, r.yMin), radio);
            p.ClosePath();
        }
    }
}
