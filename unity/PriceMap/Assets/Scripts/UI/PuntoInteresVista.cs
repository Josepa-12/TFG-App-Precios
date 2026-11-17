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
            EscalaFuente.Aplicar(this, AccessibilitySettings.fontScale);
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

        void DibujarGlifo(MeshGenerationContext mgc) =>
            IconosLugar.Dibujar(mgc.painter2D, Glifo, glifo.contentRect, glifo.resolvedStyle.color);
    }
}
