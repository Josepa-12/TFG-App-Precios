using System;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Punto de interés de OpenStreetMap encima del mapa (como el prototipo, lugares.js · addPoiLayer):
    /// círculo de 24 dp con el color de su categoría, borde blanco y glifo blanco, y el nombre a la
    /// derecha con un halo del color de fondo. Distinto del marcador de lugar (cuadrado con punta).
    /// El elemento mide 0×0 y está justo en la coordenada: el círculo queda centrado en ella
    /// (PantallaMapa.uss). No captura toques.
    /// </summary>
    public class PuntoInteresVista : VisualElement
    {
        public PuntoInteres Punto { get; }
        public CategoriaPoi Categoria { get; }
        public string Glifo { get; }
        /// <summary>Centro sanitario público: está en el mapa para orientarse, nunca tendrá precio.</summary>
        public bool SanidadPublica { get; }

        /// <summary>Círculo de color con el glifo.</summary>
        public VisualElement Icono { get; }
        /// <summary>Nombre a la derecha; null si el punto no tiene nombre (solo se ve el icono).</summary>
        public Label Nombre { get; }

        /// <summary>Si se ve ahora (cámara cerca y dentro de la pantalla con margen).</summary>
        public bool Visible { get; private set; }
        /// <summary>Punto de la coordenada en dp del panel (centro del círculo).</summary>
        public Vector2 Posicion { get; private set; }

        readonly VisualElement glifo;
        bool colocado;

        /// <param name="categoria">Categoría ya decidida por ReglaPuntosInteres (no Ninguna).</param>
        public PuntoInteresVista(PuntoInteres punto, CategoriaPoi categoria)
        {
            Punto = punto ?? throw new ArgumentNullException(nameof(punto));
            if (categoria == CategoriaPoi.Ninguna) throw new ArgumentException("Un punto sin categoría no se dibuja.", nameof(categoria));
            Categoria = categoria;
            Glifo = IconosLugar.GlifoDe(categoria);
            SanidadPublica = ReglaPuntosInteres.EsSanidadPublica(categoria, punto.Nombre, punto.Clase, punto.Subclase);
            NombreNormalizado = ReglaPuntosInteres.NombreNormalizado(punto.Nombre);
            name = punto.Id != 0 ? "poi-" + punto.Id : "poi-sin-id"; // sin id, no se repite "poi-0"

            AddToClassList("poi");
            AddToClassList(ClaseCategoria(categoria));
            if (SanidadPublica) AddToClassList("poi--sanidad-publica");
            pickingMode = PickingMode.Ignore;
            usageHints = UsageHints.DynamicTransform; // se mueve cada fotograma: solo cambia su transformación
            style.display = DisplayStyle.None;      // hasta la primera proyección

            Icono = new VisualElement { pickingMode = PickingMode.Ignore };
            Icono.AddToClassList("poi__icono");
            Add(Icono);
            glifo = new VisualElement { pickingMode = PickingMode.Ignore };
            glifo.AddToClassList("poi__glifo");
            glifo.generateVisualContent += DibujarGlifo;
            glifo.RegisterCallback<GeometryChangedEvent>(_ => glifo.MarkDirtyRepaint());
            Icono.Add(glifo);

            if (!string.IsNullOrEmpty(punto.Nombre))
            {
                Nombre = new Label(punto.Nombre) { pickingMode = PickingMode.Ignore };
                Nombre.AddToClassList("poi__nombre");
                Nombre.AddToClassList("t-caption");
                // Al resolverse el estilo por primera vez, el tamaño pasa de dp a sp (escala de fuente del sistema)
                Nombre.RegisterCallback<GeometryChangedEvent>(PrimeraDisposicion);
                Add(Nombre);
            }
        }

        void PrimeraDisposicion(GeometryChangedEvent _)
        {
            if (Nombre.resolvedStyle.fontSize <= 0f) return;
            Nombre.UnregisterCallback<GeometryChangedEvent>(PrimeraDisposicion);
            EscalaFuente.Aplicar(this, EscalaFuente.Sistema);
            escalaAplicada = true;
        }

        /// <summary>Clase USS de la categoría ("poi--super"): da el color del círculo y del nombre.</summary>
        public static string ClaseCategoria(CategoriaPoi c) => "poi--" + c.ToString().ToLowerInvariant();

        /// <summary>Muestra u oculta el punto; display solo se toca si cambia.</summary>
        internal void Mostrar(bool ver)
        {
            if (ver == Visible) return;
            Visible = ver;
            style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Lleva el punto a su sitio (dp del panel) moviendo solo su transformación.</summary>
        internal void Colocar(Vector2 dp)
        {
            if (colocado && dp == Posicion) return;
            colocado = true;
            Posicion = dp;
            style.translate = new Translate(dp.x, dp.y);
        }

        // ───────── Caja para los solapes (CapaPuntosInteres) ─────────

        /// <summary>Diámetro del círculo (dp), como --poi-icono de Tokens.uss.</summary>
        public const float IconoDp = 24f;
        /// <summary>Lado mínimo (dp) de la zona de TalkBack del punto (Accessibility Floor: 48 dp).</summary>
        public const float ZonaTactilDp = 48f;

        /// <summary>
        /// Zona de TalkBack (dp del panel): un cuadrado de al menos ZonaTactilDp centrado en el círculo. Sin
        /// disposición, se centra en Posicion (la coordenada, que es el centro del círculo).
        /// </summary>
        public Rect ZonaTactil
        {
            get
            {
                var b = Icono.worldBound;
                bool valido = !float.IsNaN(b.x) && !float.IsNaN(b.y) && b.width > 0f && b.height > 0f;
                var centro = valido ? b.center : Posicion;
                float ancho = Mathf.Max(ZonaTactilDp, valido ? b.width : 0f);
                float alto = Mathf.Max(ZonaTactilDp, valido ? b.height : 0f);
                return new Rect(centro.x - ancho / 2f, centro.y - alto / 2f, ancho, alto);
            }
        }
        /// <summary>Distancia del centro del círculo al borde izquierdo del nombre (dp), como left de .poi__nombre.</summary>
        public const float NombreIzquierdaDp = 14f;
        /// <summary>Ancho máximo del nombre (dp), como --poi-nombre-ancho: más largo, pasa a otra línea.</summary>
        public const float NombreAnchoMaxDp = 96f;
        /// <summary>Tamaño de letra (dp) con el que se estima el nombre si aún no tiene estilo (t-caption).</summary>
        const float LetraPorDefectoDp = 12f;

        /// <summary>Dónde caería en esta actualización (dp), mientras la capa decide los solapes.</summary>
        internal Vector2 Candidata;

        /// <summary>Nombre normalizado para los repetidos (ReglaPuntosInteres), calculado una sola vez.</summary>
        public string NombreNormalizado { get; }

        /// <summary>Última medida real del nombre; (0, 0) si aún no se ha dispuesto o se ha olvidado.</summary>
        internal Vector2 MedidaRecordada { get; set; }

        /// <summary>Olvida la medida recordada (al cambiar la escala de fuente): hasta la próxima disposición se estima.</summary>
        internal void OlvidarMedida() => MedidaRecordada = Vector2.zero;

        bool escalaAplicada; // la escala de fuente del sistema ya está en el tamaño de letra del nombre

        /// <summary>
        /// Ancho y alto del nombre (dp): los de su disposición si ya la tiene (se recuerdan, porque al
        /// ocultarse la disposición queda a cero) o, si nunca se ha dispuesto, una estimación por el número
        /// de letras. (0, 0) sin nombre.
        /// </summary>
        public Vector2 MedidaNombre
        {
            get
            {
                if (Nombre == null) return Vector2.zero;
                var l = Nombre.layout;
                if (Visible && l.width > 0f && l.height > 0f && !float.IsNaN(l.width) && !float.IsNaN(l.height))
                    MedidaRecordada = new Vector2(l.width, l.height);
                return MedidaRecordada != Vector2.zero ? MedidaRecordada : EstimarNombre();
            }
        }

        /// <summary>
        /// Estimación del nombre sin disposición, a lo alto: ~0,65 em por letra (con la escala de fuente del
        /// sistema si aún no se ha aplicado), en líneas de como mucho NombreAnchoMaxDp.
        /// </summary>
        internal Vector2 EstimarNombre()
        {
            float letra = Nombre.resolvedStyle.fontSize;
            if (!(letra > 0f) || float.IsInfinity(letra)) letra = LetraPorDefectoDp;
            if (!escalaAplicada)
            {
                float escala = EscalaFuente.Sistema;
                if (escala > 0f && !float.IsNaN(escala) && !float.IsInfinity(escala)) letra *= escala;
            }
            float ancho = Nombre.text.Length * letra * 0.65f;
            int lineas = Mathf.Max(1, Mathf.CeilToInt(ancho / NombreAnchoMaxDp));
            return new Vector2(Mathf.Min(ancho, NombreAnchoMaxDp), lineas * letra * 1.3f);
        }

        /// <summary>
        /// Caja que ocupa el punto (dp del panel) con el círculo centrado en centro: el círculo de IconoDp
        /// y, a su derecha y centrado en vertical, el nombre (MedidaNombre).
        /// </summary>
        public Rect Caja(Vector2 centro)
        {
            const float radio = IconoDp / 2f;
            var nombre = MedidaNombre;
            float derecha = nombre.x > 0f ? Mathf.Max(radio, NombreIzquierdaDp + nombre.x) : radio;
            float medioAlto = Mathf.Max(radio, nombre.y / 2f);
            return Rect.MinMaxRect(centro.x - radio, centro.y - medioAlto, centro.x + derecha, centro.y + medioAlto);
        }

        void DibujarGlifo(MeshGenerationContext mgc) =>
            IconosLugar.Dibujar(mgc.painter2D, Glifo, glifo.contentRect, glifo.resolvedStyle.color);
    }
}
