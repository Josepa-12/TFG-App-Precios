using System;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Ventana modal que explica para qué se usa la ubicación antes de pedir el permiso de Android, con
    /// "Permitir" y "Ahora no". Es a la vez el velo que cubre toda la pantalla (mapa y botones): mientras
    /// está abierta, nada más responde. La abre y cierra PantallaMapa; TalkBack la trata como modal.
    /// </summary>
    public class ExplicacionUbicacion : VisualElement
    {
        public VisualElement Tarjeta { get; }
        public Label Texto { get; }
        public Button Permitir { get; }
        public Button AhoraNo { get; }
        public bool Abierta { get; private set; }

        /// <param name="alPermitir">"Permitir": cierra y lanza el permiso de Android.</param>
        /// <param name="alAhoraNo">"Ahora no": solo cierra.</param>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public ExplicacionUbicacion(Action alPermitir, Action alAhoraNo, Func<string, string> textos = null)
        {
            textos = textos ?? (c => Textos.T(c));
            name = "explicacion-ubicacion";
            AddToClassList("explicacion-ubicacion");
            pickingMode = PickingMode.Position; // el velo tapa el mapa y los botones

            Tarjeta = new VisualElement { name = "explicacion-ubicacion-tarjeta", pickingMode = PickingMode.Position };
            Tarjeta.AddToClassList("explicacion-ubicacion__tarjeta");
            Add(Tarjeta);

            Texto = new Label(textos(ClavesTexto.UbicacionExplicacion)) { name = "explicacion-ubicacion-texto", pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("explicacion-ubicacion__texto");
            Texto.AddToClassList("t-body");
            Tarjeta.Add(Texto);

            var botones = new VisualElement { pickingMode = PickingMode.Ignore };
            botones.AddToClassList("explicacion-ubicacion__botones");
            Tarjeta.Add(botones);

            AhoraNo = Boton("explicacion-ubicacion-ahora-no", textos(ClavesTexto.UbicacionAhoraNo), alAhoraNo, "explicacion-ubicacion__boton--secundario");
            Permitir = Boton("explicacion-ubicacion-permitir", textos(ClavesTexto.UbicacionPermitir), alPermitir, "explicacion-ubicacion__boton--principal");
            botones.Add(AhoraNo);
            botones.Add(Permitir);

            Mostrar(false);
        }

        static Button Boton(string nombre, string texto, Action alPulsar, string clase)
        {
            // Button ya responde al toque y a NavigationSubmitEvent (teclado, mando)
            var b = new Button(alPulsar) { name = nombre, text = texto, focusable = true };
            b.AddToClassList("explicacion-ubicacion__boton");
            b.AddToClassList(clase);
            b.AddToClassList("t-label");
            return b;
        }

        public void Mostrar(bool abierta)
        {
            Abierta = abierta;
            style.display = abierta ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
