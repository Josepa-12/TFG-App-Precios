using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Chincheta del punto libre (historia 2.5; maqueta del 19/11/2026): gota con la punta en la coordenada, borde y
    /// centro de otro color (claro: azul marino con borde y centro blancos; oscuro: blanca con borde fino y centro azul
    /// marino) y una sombra pequeña en el suelo. Se ve mientras la ficha del punto está abierta. Como MarcadorPosicion,
    /// mide 0×0 y está justo en la coordenada (se mueve con translate); no captura toques.
    /// </summary>
    public class ChinchetaPunto : VisualElement
    {
        /// <summary>Margen fuera de la pantalla (dp) en el que aún se coloca: lo que mide la gota.</summary>
        public const float MargenDp = 48f;

        static readonly CustomStyleProperty<float> PropiedadBorde = new CustomStyleProperty<float>("--chincheta-borde");

        public VisualElement Gota { get; }
        public VisualElement Sombra { get; }

        /// <summary>Si se ve ahora (hay punto, delante de la cámara y dentro de la pantalla).</summary>
        public bool Visible { get; private set; }
        /// <summary>Punto de la coordenada (la punta de la gota) en dp del panel.</summary>
        public Vector2 Posicion { get; private set; }

        float borde = 3f; // grosor del borde en unidades de la maqueta (34 × 44); el oscuro lo pone más fino
        bool colocado;

        public ChinchetaPunto()
        {
            name = "chincheta-punto";
            AddToClassList("chincheta");
            pickingMode = PickingMode.Ignore;
            usageHints = UsageHints.DynamicTransform; // se mueve con el mapa
            style.display = DisplayStyle.None;

            Sombra = new VisualElement { name = "chincheta-sombra", pickingMode = PickingMode.Ignore };
            Sombra.AddToClassList("chincheta__sombra");
            Add(Sombra);

            Gota = new VisualElement { name = "chincheta-gota", pickingMode = PickingMode.Ignore };
            Gota.AddToClassList("chincheta__gota");
            Gota.generateVisualContent += DibujarGota;
            Gota.RegisterCallback<GeometryChangedEvent>(_ => Gota.MarkDirtyRepaint());
            Add(Gota);

            RegisterCallback<CustomStyleResolvedEvent>(_ =>
            {
                if (customStyle.TryGetValue(PropiedadBorde, out var b) && b > 0f) borde = b;
                Gota.MarkDirtyRepaint();
            });
        }

        /// <summary>Recoloca la chincheta con el tamaño de la pantalla actual.</summary>
        public void Actualizar(IMapa mapa, Coordenada? punto, float escalaDp) =>
            Actualizar(mapa, punto, escalaDp, Screen.width, Screen.height);

        /// <summary>
        /// Proyecta el punto con la cámara de este fotograma. Sin punto, detrás de la cámara o fuera de la pantalla
        /// (con margen), se oculta. escalaDp convierte píxeles de pantalla en dp.
        /// </summary>
        public void Actualizar(IMapa mapa, Coordenada? punto, float escalaDp, float anchoPx, float altoPx)
        {
            bool ver = false;
            if (mapa != null && punto.HasValue && escalaDp > 0f && !float.IsNaN(escalaDp) && !float.IsInfinity(escalaDp))
            {
                var p = mapa.Proyectar(punto.Value);
                var dp = new Vector2(p.X * escalaDp, p.Y * escalaDp);
                float ancho = anchoPx * escalaDp, alto = altoPx * escalaDp;
                ver = p.Delante && !float.IsNaN(dp.x) && !float.IsNaN(dp.y)
                      && dp.x >= -MargenDp && dp.x <= ancho + MargenDp && dp.y >= -MargenDp && dp.y <= alto + MargenDp;
                if (ver && (!colocado || dp != Posicion))
                {
                    colocado = true;
                    Posicion = dp;
                    style.translate = new Translate(dp.x, dp.y);
                }
            }
            if (ver != Visible)
            {
                Visible = ver;
                style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Vuelve a dibujar la gota (sus colores llegan con el estilo del tema).</summary>
        public void Repintar() => Gota.MarkDirtyRepaint();

        // Gota de la maqueta (viewBox 34 × 44, punta abajo en el centro), escalada al tamaño del elemento. El cuerpo
        // usa el color de la gota; el borde y el centro, el de la chincheta
        void DibujarGota(MeshGenerationContext mgc) =>
            Dibujar(mgc, Gota.contentRect, Gota.resolvedStyle.color, resolvedStyle.color, borde);

        /// <summary>
        /// Dibuja la gota de la maqueta en el rectángulo: cuerpo de un color y, del otro, el borde (de ese grosor en
        /// unidades de la maqueta; 0 = sin borde) y el centro. También la gotita de la ficha del punto.
        /// </summary>
        public static void Dibujar(MeshGenerationContext mgc, Rect r, Color cuerpo, Color detalle, float borde)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            float sx = r.width / 34f, sy = r.height / 44f;
            Vector2 P(float x, float y) => new Vector2(r.x + x * sx, r.y + y * sy);
            var p = mgc.painter2D;
            p.BeginPath();
            p.MoveTo(P(17f, 1.5f));
            p.BezierCurveTo(P(8.4f, 1.5f), P(1.5f, 8.3f), P(1.5f, 16.8f));
            p.BezierCurveTo(P(1.5f, 28.1f), P(14.9f, 41.2f), P(15.9f, 42.2f));
            p.BezierCurveTo(P(16.5f, 42.8f), P(17.5f, 42.8f), P(18.1f, 42.2f));
            p.BezierCurveTo(P(19.1f, 41.2f), P(32.5f, 28.1f), P(32.5f, 16.8f));
            p.BezierCurveTo(P(32.5f, 8.3f), P(25.6f, 1.5f), P(17f, 1.5f));
            p.ClosePath();
            p.fillColor = cuerpo;
            p.Fill();
            if (borde > 0f)
            {
                p.strokeColor = detalle;
                p.lineWidth = borde * sx;
                p.lineJoin = LineJoin.Round;
                p.Stroke();
            }

            p.BeginPath();
            p.Arc(P(17f, 16.5f), 5.2f * sx, 0f, 360f);
            p.ClosePath();
            p.fillColor = detalle;
            p.Fill();
        }
    }
}
