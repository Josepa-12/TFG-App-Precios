using System;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Marcador de un lugar encima del mapa (como el prototipo, styles.css · .pin): cuadrado con el color
    /// de su plantilla, borde blanco, logo del tipo de negocio, punta abajo e insignia € si tiene precio;
    /// sin precio, más pequeño y atenuado. El elemento mide 0×0 y está justo en la coordenada: la punta
    /// cae ahí y el resto cuelga hacia arriba (PantallaMapa.uss). No captura toques: el mapa sigue
    /// moviéndose al arrastrar encima (la ficha llega en la historia 1.8).
    /// </summary>
    public class MarcadorLugar : VisualElement
    {
        public Lugar Lugar { get; }
        public bool TienePrecio { get; }
        public string Glifo { get; }
        /// <summary>Lo que lee TalkBack ("Clínica Areas, clínica privada, desde 20 €").</summary>
        public string Anuncio { get; }

        /// <summary>Cuerpo cuadrado (su marco es el que se da a TalkBack).</summary>
        public VisualElement Cuerpo { get; }
        public VisualElement Punta { get; }
        /// <summary>Insignia €; null si el lugar no tiene precio.</summary>
        public VisualElement Insignia { get; }

        /// <summary>Si se ve ahora (delante de la cámara y dentro de la pantalla con margen).</summary>
        public bool Visible { get; private set; }
        /// <summary>Punto de la coordenada en dp del panel (donde cae la punta).</summary>
        public Vector2 Posicion { get; private set; }
        /// <summary>Distancia a la cámara: los más cercanos se pintan encima.</summary>
        public float Profundidad { get; private set; }
        /// <summary>Orden de llegada: desempata la ordenación por profundidad.</summary>
        internal int Indice { get; }

        readonly VisualElement logo;
        bool colocado;

        public MarcadorLugar(Lugar lugar, ResumenLugar resumen, int indice = 0, Func<string, string> textos = null)
        {
            Lugar = lugar ?? throw new ArgumentNullException(nameof(lugar));
            TienePrecio = resumen != null && resumen.TienePrecio && !lugar.Publico;
            Glifo = IconosLugar.GlifoDe(lugar.Tipo);
            Anuncio = TextoAnuncio(lugar, TienePrecio ? resumen : ResumenLugar.SinPrecio, textos ?? Textos.T);
            Indice = indice;
            name = "marcador-" + lugar.Id;

            AddToClassList("marcador");
            AddToClassList(ClasePlantilla(lugar.Plantilla));
            if (!TienePrecio) AddToClassList("marcador--sin-precio");
            pickingMode = PickingMode.Ignore;
            usageHints = UsageHints.DynamicTransform; // se mueve cada fotograma: solo cambia su transformación
            style.display = DisplayStyle.None;      // hasta la primera proyección

            var pin = Nuevo("marcador__pin");
            Add(pin);
            Cuerpo = Nuevo("marcador__cuerpo");
            pin.Add(Cuerpo);
            logo = Nuevo("marcador__logo");
            logo.generateVisualContent += DibujarLogo;
            Cuerpo.Add(logo);
            Punta = Nuevo("marcador__punta");
            Punta.generateVisualContent += DibujarPunta;
            pin.Add(Punta);
            if (TienePrecio)
            {
                // El € se dibuja con vectores: con la fuente al 200 % un texto se saldría de la insignia
                Insignia = Nuevo("marcador__insignia");
                Insignia.generateVisualContent += DibujarEuro;
                Cuerpo.Add(Insignia);
            }
        }

        static VisualElement Nuevo(string clase)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(clase);
            // Los colores llegan con el estilo: volver a dibujar cuando cambie la geometría
            e.RegisterCallback<GeometryChangedEvent>(_ => e.MarkDirtyRepaint());
            return e;
        }

        public static string ClasePlantilla(Plantilla p) => p switch
        {
            Plantilla.Hosteleria => "marcador--hosteleria",
            Plantilla.Ocio => "marcador--ocio",
            _ => "marcador--servicio",
        };

        /// <summary>
        /// "Nombre, tipo, desde 20 €" o "Nombre, tipo, aún no tiene precio". El tipo va en minúsculas y
        /// el precio con el formato es-ES de la UI (AD-10).
        /// </summary>
        public static string TextoAnuncio(Lugar lugar, ResumenLugar resumen, Func<string, string> textos)
        {
            // En castellano las minúsculas no dependen de la cultura (á, é, ñ...): invariante, igual en todo móvil
            string tipo = lugar.Tipo.ToLowerInvariant();
            var precio = resumen?.PrecioAnunciado;
            return precio == null
                ? Textos.Formatear(textos(ClavesTexto.MarcadorSinPrecio), lugar.Nombre, tipo)
                : Textos.Formatear(textos(ClavesTexto.MarcadorConPrecio), lugar.Nombre, tipo, FormatoPrecio.Euros(precio.Value.MinCent));
        }

        /// <summary>Muestra u oculta el marcador; display solo se toca si cambia.</summary>
        internal void Mostrar(bool ver)
        {
            if (ver == Visible) return;
            Visible = ver;
            style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Lleva el marcador a su punto (dp del panel) moviendo solo su transformación.</summary>
        internal void Colocar(Vector2 dp, float profundidad)
        {
            Profundidad = profundidad;
            if (colocado && dp == Posicion) return;
            colocado = true;
            Posicion = dp;
            style.translate = new Translate(dp.x, dp.y);
        }

        /// <summary>Vuelve a dibujar los vectores (el color del € cambia con el tema).</summary>
        public void Repintar()
        {
            logo.MarkDirtyRepaint();
            Punta.MarkDirtyRepaint();
            Insignia?.MarkDirtyRepaint();
        }

        void DibujarLogo(MeshGenerationContext mgc) =>
            IconosLugar.Dibujar(mgc.painter2D, Glifo, logo.contentRect, logo.resolvedStyle.color);

        // Triángulo blanco que apunta a la coordenada
        void DibujarPunta(MeshGenerationContext mgc)
        {
            var r = Punta.contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            p.fillColor = Punta.resolvedStyle.color;
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMin));
            p.LineTo(new Vector2(r.center.x, r.yMax));
            p.ClosePath();
            p.Fill();
        }

        // € con trazos: una C abierta a la derecha y dos barras
        void DibujarEuro(MeshGenerationContext mgc)
        {
            var r = Insignia.contentRect;
            float lado = Mathf.Min(r.width, r.height);
            if (lado <= 0f) return;
            var c = r.center;
            float radio = lado * 0.32f;
            var p = mgc.painter2D;
            p.strokeColor = Insignia.resolvedStyle.color;
            p.lineWidth = Mathf.Max(1f, lado * 0.12f);
            p.lineCap = LineCap.Round;
            var centroC = c + new Vector2(lado * 0.08f, 0f);
            p.BeginPath();
            p.Arc(centroC, radio, -50f, 50f, ArcDirection.CounterClockwise);
            p.Stroke();
            float x0 = centroC.x - radio - lado * 0.16f, x1 = centroC.x + lado * 0.04f;
            float dy = radio * 0.32f;
            p.BeginPath();
            p.MoveTo(new Vector2(x0, c.y - dy)); p.LineTo(new Vector2(x1, c.y - dy));
            p.MoveTo(new Vector2(x0, c.y + dy)); p.LineTo(new Vector2(x1, c.y + dy));
            p.Stroke();
        }
    }
}
