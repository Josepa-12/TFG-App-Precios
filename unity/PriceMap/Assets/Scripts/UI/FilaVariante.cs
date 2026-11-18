using System;
using PriceMap.Dominio;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Variante de un concepto en la ficha (historia 1.8, parte 2; FR-60; maqueta key-ficha-lugar.html · .variant):
    /// condición y precio ("De 20:00 a 24:00 · 24 €") debajo del nombre del concepto. La vigente lleva delante el
    /// chip "Ahora" (.now). El precio sale del PrecioMostrado que calcula el Dominio para la variante; no captura toques.
    /// </summary>
    public class FilaVariante : VisualElement
    {
        public Variante Variante { get; }
        public PrecioMostrado PrecioMostrado { get; }
        /// <summary>True si es la variante que se cumple ahora (ReglaVariantes.Vigente).</summary>
        public bool Vigente { get; }

        /// <summary>Chip "Ahora"; null si no es la vigente.</summary>
        public Label Chip { get; }
        public Label Condicion { get; }
        public Label Precio { get; }

        /// <summary>"Con bono · desde 15 €" (sin el chip).</summary>
        public string Texto => Condicion.text + " · " + Precio.text;

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public FilaVariante(Variante variante, PrecioMostrado precio, bool vigente, Func<string, string> textos = null)
        {
            Variante = variante ?? throw new ArgumentNullException(nameof(variante));
            PrecioMostrado = precio ?? throw new ArgumentNullException(nameof(precio));
            Vigente = vigente;
            textos ??= Textos.T;
            name = "variante-" + variante.Id;
            AddToClassList("ficha__variante");
            EnableInClassList("ficha__variante--vigente", vigente);
            pickingMode = PickingMode.Ignore;

            if (vigente)
            {
                Chip = Etiqueta(textos(ClavesTexto.FichaAhora), "ficha__variante-ahora", "t-meta-fuerte");
                Add(Chip);
            }
            Condicion = Etiqueta(FilaConcepto.TextoCondicion(variante.Condicion, textos), "ficha__variante-condicion", "t-meta");
            Add(Condicion);
            Add(Etiqueta("·", "ficha__variante-separador", "t-meta"));
            Precio = Etiqueta(FilaConcepto.TextoPrecio(precio, textos), "ficha__variante-precio", "t-meta-fuerte");
            Add(Precio);
        }

        static Label Etiqueta(string texto, string clase, string tipografia)
        {
            var l = new Label(texto) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            return l;
        }
    }
}
