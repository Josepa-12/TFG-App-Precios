using System;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Píldora "Precios" arriba a la izquierda (DESIGN.md · prices-pill): cristal, punto de color de la
    /// opción elegida, su texto y una flecha hacia abajo. Al pulsarla abre o cierra el menú.
    /// Con Ninguno dice "Ver precios" con el texto atenuado.
    /// Con una categoría de "Encuentra cerca" activa (historia 2.4, parte 2) enseña el icono de la categoría (el del
    /// chip), su nombre y una ✕ dibujada en lugar de la flecha; pulsarla entonces quita la categoría (PantallaMapa).
    /// </summary>
    public class PildoraPrecios : VisualElement
    {
        public const string ClaseCategoria = "pildora-precios--categoria";

        readonly Func<string, string> textos;
        readonly VisualElement flecha;
        readonly VisualElement glifo;
        string glifoCategoria = IconosLugar.Generico;
        string claseColor;

        public PuntoPrecios Punto { get; }
        public Label Texto { get; }
        public FiltroPrecios Filtro { get; private set; }
        /// <summary>Icono de la categoría (círculo del color del chip con su glifo); oculto sin categoría.</summary>
        public VisualElement IconoCategoria { get; }
        /// <summary>La ✕ que sustituye a la flecha con categoría; oculta sin ella.</summary>
        public VisualElement Quitar { get; }
        /// <summary>Categoría que enseña la píldora; null en modo "Precios".</summary>
        public CategoriaPoi? Categoria { get; private set; }

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public PildoraPrecios(Action alPulsar, Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "pildora-precios";
            AddToClassList("pildora-precios");
            focusable = true;
            pickingMode = PickingMode.Position;
            this.AddManipulator(new Clickable(alPulsar));
            RegisterCallback<NavigationSubmitEvent>(_ => alPulsar()); // teclado, mando o navegación
            SombraSuave.Poner(this); // cristal con borde glass-edge (USS) y la sombra suave de los controles
            Semicirculo.Mantener(this); // extremos semicirculares también si crece con la fuente grande

            Punto = new PuntoPrecios(FiltroPrecios.Ninguno);
            Add(Punto);
            // Icono de la categoría: las mismas clases que el del chip (tamaño, círculo y color) y su glifo
            IconoCategoria = new VisualElement { name = "pildora-precios-icono", pickingMode = PickingMode.Ignore };
            IconoCategoria.AddToClassList("buscador__chip-icono");
            IconoCategoria.AddToClassList("pildora-precios__icono");
            glifo = new VisualElement { name = "pildora-precios-glifo", pickingMode = PickingMode.Ignore };
            glifo.AddToClassList("buscador__chip-glifo");
            glifo.generateVisualContent += mgc => IconosLugar.Dibujar(mgc.painter2D, glifoCategoria, glifo.contentRect, glifo.resolvedStyle.color);
            glifo.RegisterCallback<GeometryChangedEvent>(_ => glifo.MarkDirtyRepaint());
            IconoCategoria.Add(glifo);
            Add(IconoCategoria);
            Texto = new Label { name = "pildora-precios-texto", pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("pildora-precios__texto");
            Texto.AddToClassList("t-label");
            Add(Texto);
            flecha = new VisualElement { pickingMode = PickingMode.Ignore };
            flecha.AddToClassList("pildora-precios__flecha");
            flecha.generateVisualContent += DibujarFlecha;
            flecha.RegisterCallback<GeometryChangedEvent>(_ => flecha.MarkDirtyRepaint());
            Add(flecha);
            Quitar = new VisualElement { name = "pildora-precios-quitar", pickingMode = PickingMode.Ignore };
            Quitar.AddToClassList("pildora-precios__quitar");
            Quitar.generateVisualContent += mgc => IconosLugar.Dibujar(mgc.painter2D, IconosLugar.Borrar, Quitar.contentRect, Quitar.resolvedStyle.color);
            Quitar.RegisterCallback<GeometryChangedEvent>(_ => Quitar.MarkDirtyRepaint());
            Add(Quitar);

            Mostrar(FiltroPrecios.Ninguno);
        }

        /// <summary>Pone el texto y el punto de la opción elegida (y sale del modo categoría).</summary>
        public void Mostrar(FiltroPrecios filtro)
        {
            Filtro = filtro;
            Categoria = null;
            Texto.text = textos(TextosFiltro.ClavePildora(filtro));
            Punto.Filtro = filtro;
            EnableInClassList("pildora-precios--ninguno", filtro == FiltroPrecios.Ninguno);
            Modo(categoria: false);
        }

        /// <summary>Modo categoría: icono del chip, su nombre y la ✕. El filtro guardado (Filtro) no cambia.</summary>
        public void MostrarCategoria(CategoriaPoi categoria, string nombre)
        {
            Categoria = categoria;
            Texto.text = nombre;
            glifoCategoria = IconosLugar.GlifoDe(categoria);
            if (claseColor != null) IconoCategoria.RemoveFromClassList(claseColor);
            claseColor = "buscador__chip-icono--" + HojaBuscador.Nombre(categoria);
            IconoCategoria.AddToClassList(claseColor);
            EnableInClassList("pildora-precios--ninguno", false);
            Modo(categoria: true);
            glifo.MarkDirtyRepaint();
        }

        void Modo(bool categoria)
        {
            EnableInClassList(ClaseCategoria, categoria);
            Punto.style.display = categoria ? DisplayStyle.None : DisplayStyle.Flex;
            flecha.style.display = categoria ? DisplayStyle.None : DisplayStyle.Flex;
            IconoCategoria.style.display = categoria ? DisplayStyle.Flex : DisplayStyle.None;
            Quitar.style.display = categoria ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Vuelve a dibujar el punto, la flecha, el icono y la ✕ (al cambiar de tema).</summary>
        public void Repintar()
        {
            Punto.MarkDirtyRepaint();
            flecha.MarkDirtyRepaint();
            glifo.MarkDirtyRepaint();
            Quitar.MarkDirtyRepaint();
        }

        void DibujarFlecha(MeshGenerationContext mgc)
        {
            var r = flecha.contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            p.strokeColor = flecha.resolvedStyle.color;
            p.lineWidth = 2f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            // Chevron hacia abajo dentro del rectángulo, con 1 dp de margen para el grosor de la línea
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin + 1f, r.yMin + 1f));
            p.LineTo(new Vector2(r.center.x, r.yMax - 1f));
            p.LineTo(new Vector2(r.xMax - 1f, r.yMin + 1f));
            p.Stroke();
        }
    }
}
