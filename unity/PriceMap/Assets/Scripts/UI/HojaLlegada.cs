using System;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Hoja de "Has llegado" abajo (historia 2.9; maqueta B del 23/11/2026), en el sitio de la hoja de la ruta: solo el
    /// botón ancho "Finalizar ruta", en el acento (no en el rojo de antes de llegar), y "La ruta se cerrará en 1 minuto".
    /// No se despliega. El botón crece con la fuente.
    /// </summary>
    public class HojaLlegada : VisualElement
    {
        /// <summary>Botón "Finalizar ruta".</summary>
        public VisualElement Finalizar { get; }
        public Label TextoFinalizar { get; }
        /// <summary>"La ruta se cerrará en 1 minuto".</summary>
        public Label Aviso { get; }

        /// <summary>"Finalizar ruta" se ha pulsado.</summary>
        public event Action FinalizarPulsado;

        public HojaLlegada(Func<string, string> textos = null)
        {
            textos = textos ?? (c => Textos.T(c));
            name = "hoja-llegada";
            AddToClassList("hoja-llegada");
            pickingMode = PickingMode.Position; // arrastrar sobre la hoja no mueve el mapa

            Finalizar = new VisualElement { name = "hoja-llegada-finalizar", focusable = true, pickingMode = PickingMode.Position };
            Finalizar.AddToClassList("hoja-llegada__finalizar");
            Finalizar.AddManipulator(new Clickable(PulsarFinalizar));
            Finalizar.RegisterCallback<NavigationSubmitEvent>(_ => PulsarFinalizar());
            Add(Finalizar);
            TextoFinalizar = new Label(textos(ClavesTexto.RutaFinalizar)) { name = "hoja-llegada-finalizar-texto", pickingMode = PickingMode.Ignore };
            TextoFinalizar.AddToClassList("hoja-llegada__finalizar-texto");
            TextoFinalizar.AddToClassList("t-heading");
            Finalizar.Add(TextoFinalizar);

            Aviso = new Label(textos(ClavesTexto.LlegadaCierre)) { name = "hoja-llegada-aviso", pickingMode = PickingMode.Ignore };
            Aviso.AddToClassList("hoja-llegada__aviso");
            Aviso.AddToClassList("t-llegada");
            Add(Aviso);
            style.display = DisplayStyle.None;
        }

        public void PulsarFinalizar() => FinalizarPulsado?.Invoke();

        /// <summary>Relleno de abajo: el suyo más la barra de gestos (dp), como la hoja de la ruta.</summary>
        public void PonerMargenInferior(float dp) => style.paddingBottom = RellenoAbajo + (dp > 0f ? dp : 0f);

        /// <summary>Relleno de abajo propio (dp), el de su estilo.</summary>
        public const float RellenoAbajo = 20f;
    }
}
