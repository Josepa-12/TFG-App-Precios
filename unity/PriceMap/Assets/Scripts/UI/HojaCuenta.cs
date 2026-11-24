using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Entrar en la cuenta (historia 3.3; maqueta del 24/11/2026): hoja desde abajo sobre un velo o, tras la bienvenida, a
    /// pantalla completa con el logo. «Continuar con Google» (el botón oficial blanco, en los dos temas) y «Ahora no»; con un
    /// error, su aviso y «Reintentar» en lugar del texto y de Google. El texto se desplaza si no cabe. Enseña el estado de
    /// ServicioCuentas y le manda lo que se pulsa; la pone, la sigue y la quita PantallaMapa.
    /// </summary>
    public class HojaCuenta : VisualElement
    {
        public const string ClaseCompleta = "cuenta--completa";

        readonly ServicioCuentas servicio;
        readonly Func<string, string> textos;

        /// <summary>A pantalla completa («Crea tu cuenta») o en hoja («Entra en PriceMap»).</summary>
        public bool Completa { get; }
        public VisualElement Tarjeta { get; }
        public ScrollView Pagina { get; }
        public Label Titulo { get; }
        public Label Para { get; }
        /// <summary>El error, en lugar de Para.</summary>
        public Label Aviso { get; }
        public Button Google { get; }
        public Label TextoGoogle { get; }
        /// <summary>Con un error, en lugar de Google.</summary>
        public Button Reintentar { get; }
        public Button AhoraNo { get; }

        public bool AvisoVisible { get; private set; }

        /// <summary>El botón de entrar que se ve: Google o Reintentar.</summary>
        public Button Entrar => AvisoVisible ? Reintentar : Google;

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public HojaCuenta(ServicioCuentas servicio, Func<string, string> textos = null)
        {
            this.servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            this.textos = textos ?? (c => Textos.T(c));
            Completa = servicio.Entrada == EntradaCuenta.Completa;
            name = "cuenta";
            AddToClassList("cuenta");
            EnableInClassList(ClaseCompleta, Completa);
            pickingMode = PickingMode.Position; // el velo tapa todo lo de debajo
            RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(this, EscalaFuente.Sistema));

            Tarjeta = PantallaBienvenida.Nuevo("cuenta-tarjeta", "cuenta__tarjeta");
            Tarjeta.pickingMode = PickingMode.Position;
            Tarjeta.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(this, EscalaFuente.Sistema));
            Add(Tarjeta);
            if (!Completa) Tarjeta.Add(PantallaBienvenida.Nuevo("cuenta-asa", "cuenta__asa"));

            Pagina = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "cuenta-pagina",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            Pagina.AddToClassList("cuenta__pagina");
            Tarjeta.Add(Pagina);
            if (Completa)
            {
                var logo = PantallaBienvenida.Nuevo("cuenta-logo", "bienvenida__logo");
                logo.Add(PantallaBienvenida.Nuevo("cuenta-pin", "bienvenida__pin"));
                logo.Add(PantallaBienvenida.Nuevo("cuenta-nombre", "bienvenida__nombre"));
                Pagina.Add(logo);
            }
            Titulo = Texto("cuenta-titulo", "cuenta__titulo", Completa ? "t-display" : "t-title",
                this.textos(Completa ? ClavesTexto.CuentaTituloCompleta : ClavesTexto.CuentaTitulo));
            Para = Texto("cuenta-para", "cuenta__para", "t-body", this.textos(ClavesTexto.CuentaPara));
            Aviso = Texto("cuenta-aviso", "cuenta__aviso", "t-body", "");

            var pie = PantallaBienvenida.Nuevo("cuenta-pie", "cuenta__pie");
            Tarjeta.Add(pie);
            Google = new Button(PulsarEntrar) { name = "cuenta-google", text = "", focusable = true };
            Google.AddToClassList("cuenta__google");
            Semicirculo.Mantener(Google);
            Google.Add(new IconoGoogle());
            TextoGoogle = new Label { name = "cuenta-google-texto", pickingMode = PickingMode.Ignore };
            TextoGoogle.AddToClassList("cuenta__google-texto");
            TextoGoogle.AddToClassList("t-google");
            Google.Add(TextoGoogle);
            pie.Add(Google);
            Reintentar = PantallaBienvenida.NuevoBoton("cuenta-reintentar", "", PulsarEntrar, principal: true);
            Boton(Reintentar);
            pie.Add(Reintentar);
            AhoraNo = new Button(servicio.AhoraNo)
                { name = "cuenta-ahora-no", text = this.textos(ClavesTexto.UbicacionAhoraNo), focusable = true };
            AhoraNo.AddToClassList("bienvenida__enlace");
            AhoraNo.AddToClassList("cuenta__ahora-no");
            AhoraNo.AddToClassList("t-heading");
            pie.Add(AhoraNo);

            // Con la fuente grande: lo que sale con un error se dispone entonces
            foreach (var e in new VisualElement[] { Aviso, Reintentar })
                e.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(this, EscalaFuente.Sistema));
            Actualizar();
        }

        Label Texto(string nombre, string clase, string tipografia, string texto)
        {
            var l = new Label(texto) { name = nombre, pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            Pagina.Add(l);
            return l;
        }

        static void Boton(Button b)
        {
            b.AddToClassList("cuenta__boton");
            b.RemoveFromClassList("t-label");
            b.AddToClassList("t-heading");
        }

        /// <summary>«Continuar con Google» o «Reintentar».</summary>
        public async void PulsarEntrar() => await servicio.EntrarConGoogle();

        /// <summary>Enseña el estado del servicio: el error (si lo hay) y los textos de los botones.</summary>
        public void Actualizar()
        {
            AvisoVisible = servicio.Error != null;
            if (AvisoVisible) Aviso.text = textos(ClaveError(servicio.Error));
            Para.style.display = PantallaBienvenida.Ver(!AvisoVisible);
            Aviso.style.display = PantallaBienvenida.Ver(AvisoVisible);
            Google.style.display = PantallaBienvenida.Ver(!AvisoVisible);
            Reintentar.style.display = PantallaBienvenida.Ver(AvisoVisible);
            TextoGoogle.text = textos(servicio.Entrando ? ClavesTexto.CuentaEntrando : ClavesTexto.CuentaGoogle);
            Reintentar.text = textos(servicio.Entrando ? ClavesTexto.CuentaEntrando : ClavesTexto.CuentaReintentar);
        }

        /// <summary>Clave del texto de un código de ErroresCuenta.</summary>
        public static string ClaveError(string codigo)
        {
            switch (codigo)
            {
                case ErroresCuenta.SinConexion: return ClavesTexto.CuentaErrorConexion;
                case ErroresCuenta.NoDisponible: return ClavesTexto.CuentaErrorServidor;
                default: return ClavesTexto.CuentaError;
            }
        }

        /// <summary>Lo que recorre TalkBack, en orden: texto (sin acción) o botón.</summary>
        public IEnumerable<PantallaBienvenida.ControlLector> Controles()
        {
            yield return Control(Titulo, () => Titulo.text);
            yield return Control(Para, () => Para.text, visible: () => !AvisoVisible);
            yield return Control(Aviso, () => Aviso.text, visible: () => AvisoVisible);
            yield return Control(Google, () => TextoGoogle.text, PulsarEntrar, () => !AvisoVisible);
            yield return Control(Reintentar, () => Reintentar.text, PulsarEntrar, () => AvisoVisible);
            yield return Control(AhoraNo, () => AhoraNo.text, servicio.AhoraNo);
        }

        static PantallaBienvenida.ControlLector Control(VisualElement e, Func<string> etiqueta, Action accion = null, Func<bool> visible = null) =>
            new PantallaBienvenida.ControlLector { Elemento = e, Etiqueta = etiqueta, Accion = accion, Visible = visible ?? (() => true) };

        /// <summary>Deja a la vista ese texto (foco de TalkBack en uno que hay que desplazar).</summary>
        public void DesplazarHasta(VisualElement elemento)
        {
            if (elemento == null || Pagina.panel == null || !Pagina.contentContainer.Contains(elemento)) return;
            Pagina.ScrollTo(elemento);
        }

        /// <summary>
        /// Barra de estado, barra de gestos y bordes (dp), con Screen.safeArea (PantallaMapa). La hoja llega al borde inferior
        /// y su contenido termina encima de la barra de gestos.
        /// </summary>
        public void PonerZonaSegura(float izquierda, float arriba, float derecha, float abajo)
        {
            style.paddingTop = Mathf.Max(0f, arriba);
            style.paddingLeft = Mathf.Max(0f, izquierda);
            style.paddingRight = Mathf.Max(0f, derecha);
            Tarjeta.style.paddingBottom = Mathf.Max(0f, abajo);
        }
    }
}
