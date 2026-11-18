using System;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Ventana modal que explica para qué se usa la ubicación antes de pedir el permiso de Android, con
    /// "Permitir" y "Ahora no". Es a la vez el velo que cubre toda la pantalla (mapa y botones): mientras
    /// está abierta, nada más responde. La abre y cierra PantallaMapa; TalkBack la trata como modal.
    /// Con otro nombre, otras claves de texto y otros botones sirve también de aviso (AvisoUbicacion); los
    /// estilos explicacion-ubicacion* se comparten.
    /// </summary>
    public class ExplicacionUbicacion : VisualElement
    {
        readonly Func<string, string> textos;

        public VisualElement Tarjeta { get; }
        public Label Texto { get; }
        /// <summary>Botón principal (a la derecha, con el color de acento) y secundario (solo cierra).</summary>
        public Button Principal { get; }
        public Button Secundario { get; }
        /// <summary>"Permitir" y "Ahora no" en la explicación del permiso.</summary>
        public Button Permitir => Principal;
        public Button AhoraNo => Secundario;
        public bool Abierta { get; private set; }

        /// <param name="alPermitir">"Permitir": cierra y lanza el permiso de Android.</param>
        /// <param name="alAhoraNo">"Ahora no": solo cierra.</param>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public ExplicacionUbicacion(Action alPermitir, Action alAhoraNo, Func<string, string> textos = null)
            : this("explicacion-ubicacion", ClavesTexto.UbicacionExplicacion,
                ("permitir", ClavesTexto.UbicacionPermitir, alPermitir), ("ahora-no", ClavesTexto.UbicacionAhoraNo, alAhoraNo), textos)
        {
        }

        /// <param name="nombre">Nombre del elemento; sus partes se llaman nombre-texto, nombre-tarjeta y nombre-sufijo.</param>
        /// <param name="claveTexto">Clave del texto de la ventana.</param>
        /// <param name="principal">Sufijo del nombre, clave del texto y acción del botón principal.</param>
        /// <param name="secundario">Lo mismo para el botón secundario.</param>
        protected ExplicacionUbicacion(string nombre, string claveTexto, (string sufijo, string clave, Action accion) principal,
            (string sufijo, string clave, Action accion) secundario, Func<string, string> textos)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = nombre;
            AddToClassList("explicacion-ubicacion");
            pickingMode = PickingMode.Position; // el velo tapa el mapa y los botones

            Tarjeta = new VisualElement { name = nombre + "-tarjeta", pickingMode = PickingMode.Position };
            Tarjeta.AddToClassList("explicacion-ubicacion__tarjeta");
            Add(Tarjeta);

            Texto = new Label(this.textos(claveTexto)) { name = nombre + "-texto", pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("explicacion-ubicacion__texto");
            Texto.AddToClassList("t-body");
            Tarjeta.Add(Texto);

            var botones = new VisualElement { pickingMode = PickingMode.Ignore };
            botones.AddToClassList("explicacion-ubicacion__botones");
            Tarjeta.Add(botones);

            Secundario = Boton(nombre + "-" + secundario.sufijo, this.textos(secundario.clave), secundario.accion, "explicacion-ubicacion__boton--secundario");
            Principal = Boton(nombre + "-" + principal.sufijo, this.textos(principal.clave), principal.accion, "explicacion-ubicacion__boton--principal");
            botones.Add(Secundario);
            botones.Add(Principal);

            Mostrar(false);
        }

        static Button Boton(string nombre, string texto, Action alPulsar, string clase)
        {
            // Button ya responde al toque y a NavigationSubmitEvent (teclado, mando)
            var b = new Button(alPulsar) { name = nombre, text = texto, focusable = true };
            b.AddToClassList("explicacion-ubicacion__boton");
            b.AddToClassList(clase);
            b.AddToClassList("t-label");
            Semicirculo.Mantener(b); // píldora, no óvalo, también con dos líneas de texto
            return b;
        }

        /// <summary>Cambia el texto de la ventana por el de esa clave.</summary>
        public void PonerTexto(string clave) => Texto.text = textos(clave);

        public void Mostrar(bool abierta)
        {
            Abierta = abierta;
            style.display = abierta ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
