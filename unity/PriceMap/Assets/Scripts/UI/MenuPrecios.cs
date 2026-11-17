using System;
using System.Collections.Generic;
using PriceMap.Dominio;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Menú desplegable del filtro "Precios", debajo de la píldora: título y las 5 opciones con su punto
    /// de color; la elegida, con fondo surface-2 y check. Sin animación. Lo abre y cierra PantallaMapa.
    /// </summary>
    public class MenuPrecios : VisualElement
    {
        readonly List<OpcionPrecios> opciones = new List<OpcionPrecios>();

        public IReadOnlyList<OpcionPrecios> Opciones => opciones;
        public Label Titulo { get; }
        public bool Abierto { get; private set; }

        /// <param name="alElegir">Lo que pasa al tocar una opción (elegir el filtro y cerrar).</param>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public MenuPrecios(Action<FiltroPrecios> alElegir, Func<string, string> textos = null)
        {
            textos = textos ?? (c => Textos.T(c));
            name = "menu-precios";
            AddToClassList("menu-precios");
            // El fondo del menú tapa el mapa: tocar entre opciones no lo mueve ni cierra el menú
            pickingMode = PickingMode.Position;

            Titulo = new Label(textos(ClavesTexto.PreciosTitulo)) { pickingMode = PickingMode.Ignore };
            Titulo.AddToClassList("menu-precios__titulo");
            Titulo.AddToClassList("t-meta");
            Add(Titulo);
            foreach (var f in ReglaFiltro.Opciones)
            {
                var o = new OpcionPrecios(f, textos(TextosFiltro.ClaveOpcion(f)), alElegir);
                opciones.Add(o);
                Add(o);
            }
            Mostrar(false);
        }

        public void Mostrar(bool abierto)
        {
            Abierto = abierto;
            style.display = abierto ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Marca la opción elegida.</summary>
        public void Marcar(FiltroPrecios filtro)
        {
            foreach (var o in opciones) o.Marcar(o.Filtro == filtro);
        }

        public void Repintar()
        {
            foreach (var o in opciones) o.Repintar();
        }
    }
}
