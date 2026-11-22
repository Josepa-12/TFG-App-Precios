using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Posición del usuario encima del mapa (prototipo: app.js · updateMe): círculo azul con borde blanco y,
    /// con brújula, un cono translúcido hacia donde mira el móvil. Va por encima de los lugares y de los
    /// puntos de interés. Como los marcadores, mide 0×0 y está justo en la coordenada (se mueve con
    /// translate); no captura toques.
    ///
    /// Historia 2.7, parte 2c: navegando en coche es una flecha (sin cono) con el rumbo de la marcha, girada respecto
    /// al norte (rumbo − giro del mapa: con la cámara siguiendo, hacia arriba); sin señal del GPS, todo al 40 %.
    /// </summary>
    public class MarcadorPosicion : VisualElement
    {
        /// <summary>Margen fuera de la pantalla (dp) en el que aún se coloca: lo que mide el cono.</summary>
        public const float MargenDp = 40f;
        /// <summary>Apertura total del cono (grados).</summary>
        const float AperturaCono = 60f;
        /// <summary>Opacidad de la posición sin señal del GPS (se queda en su último sitio).</summary>
        public const float OpacidadSinSenal = 0.4f;
        /// <summary>Grosor del borde blanco de la flecha (dp).</summary>
        const float BordeFlecha = 2.5f;

        public VisualElement Punto { get; }
        public VisualElement Cono { get; }
        /// <summary>Flecha de la navegación en coche (dibujada hacia arriba; la gira style.rotate).</summary>
        public VisualElement Flecha { get; }

        /// <summary>Si el círculo se ve ahora (hay posición, delante de la cámara y dentro de la pantalla).</summary>
        public bool Visible { get; private set; }
        /// <summary>Si el cono se ve (visible y con brújula).</summary>
        public bool ConoVisible { get; private set; }
        /// <summary>Ángulo del cono en pantalla (grados, sentido horario desde arriba): rumbo − giro del mapa.</summary>
        public float AnguloCono { get; private set; }
        /// <summary>Si la flecha se ve (visible y en modo flecha): entonces no se ven ni el círculo ni el cono.</summary>
        public bool FlechaVisible { get; private set; }
        /// <summary>Ángulo de la flecha en pantalla (grados, sentido horario desde arriba): rumbo − giro del mapa.</summary>
        public float AnguloFlecha { get; private set; }
        /// <summary>Atenuado (sin señal del GPS): opacidad 0,4.</summary>
        public bool Atenuado { get; private set; }
        /// <summary>Punto de la coordenada en dp del panel.</summary>
        public Vector2 Posicion { get; private set; }

        bool colocado;

        public MarcadorPosicion()
        {
            name = "marcador-posicion";
            AddToClassList("marcador-posicion");
            pickingMode = PickingMode.Ignore;
            usageHints = UsageHints.DynamicTransform; // se mueve cada fotograma
            style.display = DisplayStyle.None;

            Cono = new VisualElement { name = "marcador-posicion-cono", pickingMode = PickingMode.Ignore };
            Cono.AddToClassList("marcador-posicion__cono");
            Cono.generateVisualContent += DibujarCono;
            Cono.RegisterCallback<GeometryChangedEvent>(_ => Cono.MarkDirtyRepaint());
            Cono.style.display = DisplayStyle.None;
            Add(Cono);

            Punto = new VisualElement { name = "marcador-posicion-punto", pickingMode = PickingMode.Ignore };
            Punto.AddToClassList("marcador-posicion__punto");
            Add(Punto);

            Flecha = new VisualElement { name = "marcador-posicion-flecha", pickingMode = PickingMode.Ignore };
            Flecha.AddToClassList("marcador-posicion__flecha");
            Flecha.generateVisualContent += DibujarFlecha;
            Flecha.RegisterCallback<GeometryChangedEvent>(_ => Flecha.MarkDirtyRepaint());
            Flecha.style.display = DisplayStyle.None;
            Add(Flecha);
        }

        /// <summary>Ángulo en pantalla de un rumbo real con el mapa girado: (rumbo − giro) en [0, 360).</summary>
        public static float AnguloEnPantalla(float rumbo, float giro) => Mathf.Repeat(rumbo - giro, 360f);

        /// <summary>Recoloca el marcador con el tamaño de la pantalla actual.</summary>
        public void Actualizar(IMapa mapa, Coordenada? posicion, float? rumbo, float escalaDp,
            bool flecha = false, float? rumboFlecha = null, bool atenuado = false) =>
            Actualizar(mapa, posicion, rumbo, escalaDp, Screen.width, Screen.height, flecha, rumboFlecha, atenuado);

        /// <summary>
        /// Proyecta la posición con la cámara de este fotograma y orienta el cono. Sin posición, detrás de la
        /// cámara o fuera de la pantalla (con margen), se oculta. escalaDp convierte píxeles de pantalla en dp.
        /// Con flecha (navegando en coche), la flecha hacia rumboFlecha (sin él, al norte) en lugar del círculo y el cono;
        /// atenuado (sin señal), todo al 40 %.
        /// </summary>
        public void Actualizar(IMapa mapa, Coordenada? posicion, float? rumbo, float escalaDp, float anchoPx, float altoPx,
            bool flecha = false, float? rumboFlecha = null, bool atenuado = false)
        {
            bool ver = false;
            if (mapa != null && posicion.HasValue && escalaDp > 0f && !float.IsNaN(escalaDp) && !float.IsInfinity(escalaDp))
            {
                var p = mapa.Proyectar(posicion.Value);
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

            if (atenuado != Atenuado)
            {
                Atenuado = atenuado;
                style.opacity = atenuado ? new StyleFloat(OpacidadSinSenal) : new StyleFloat(StyleKeyword.Null);
            }

            bool conFlecha = ver && flecha;
            if (conFlecha)
            {
                float angulo = AnguloEnPantalla(rumboFlecha ?? 0f, mapa.Giro);
                if (!FlechaVisible || !Mathf.Approximately(angulo, AnguloFlecha))
                {
                    AnguloFlecha = angulo;
                    Flecha.style.rotate = new Rotate(new Angle(angulo, AngleUnit.Degree));
                }
            }
            if (conFlecha != FlechaVisible)
            {
                FlechaVisible = conFlecha;
                Flecha.style.display = conFlecha ? DisplayStyle.Flex : DisplayStyle.None;
                Punto.style.display = conFlecha ? DisplayStyle.None : DisplayStyle.Flex;
            }

            bool cono = ver && !flecha && rumbo.HasValue;
            if (cono)
            {
                float angulo = AnguloEnPantalla(rumbo.Value, mapa.Giro);
                if (!ConoVisible || !Mathf.Approximately(angulo, AnguloCono))
                {
                    AnguloCono = angulo;
                    Cono.style.rotate = new Rotate(new Angle(angulo, AngleUnit.Degree));
                }
            }
            if (cono != ConoVisible)
            {
                ConoVisible = cono;
                Cono.style.display = cono ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Vuelve a dibujar el cono (su color llega con el estilo).</summary>
        public void Repintar()
        {
            Cono.MarkDirtyRepaint();
            Flecha.MarkDirtyRepaint();
        }

        // Sector circular desde el centro del elemento hacia arriba (0° = arriba); el giro lo pone style.rotate
        void DibujarCono(MeshGenerationContext mgc)
        {
            var r = Cono.contentRect;
            float radio = Mathf.Min(r.width, r.height) * 0.5f;
            if (radio <= 0f) return;
            var c = r.center;
            var p = mgc.painter2D;
            p.fillColor = Cono.resolvedStyle.color;
            // Painter2D mide los ángulos desde +X en sentido horario (Y hacia abajo): arriba = -90°
            p.BeginPath();
            p.MoveTo(c);
            p.Arc(c, radio, -90f - AperturaCono / 2f, -90f + AperturaCono / 2f);
            p.ClosePath();
            p.Fill();
        }

        // Flecha de navegación hacia arriba (punta arriba, muesca abajo) que llena el elemento: relleno con su color
        // (--color-flecha) y borde con el del marcador (--color-posicion-borde)
        void DibujarFlecha(MeshGenerationContext mgc)
        {
            var r = Flecha.contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            Vector2 P(float x, float y) => new Vector2(r.x + r.width * x, r.y + r.height * y);
            var p = mgc.painter2D;
            p.fillColor = Flecha.resolvedStyle.color;
            p.strokeColor = resolvedStyle.color;
            p.lineWidth = BordeFlecha;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(P(0.5f, 0.08f));
            p.LineTo(P(0.88f, 0.9f));
            p.LineTo(P(0.5f, 0.72f));
            p.LineTo(P(0.12f, 0.9f));
            p.ClosePath();
            p.Fill();
            p.Stroke();
        }
    }
}
