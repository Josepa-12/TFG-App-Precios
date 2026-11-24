using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Entrar en la cuenta (historia 3.3; maqueta del 24/11/2026): hoja desde abajo sobre un velo o, tras la bienvenida, a
    /// pantalla completa con el logo. «Continuar con Google» (el botón oficial blanco, en los dos temas), «Con correo» y «Ahora
    /// no»; con un error, su aviso y «Reintentar» en lugar del texto y de Google. El texto se desplaza si no cabe. Con el
    /// correo, FormularioCuenta en lugar de los botones, a pantalla completa. Enseña el estado de ServicioCuentas y le manda lo
    /// que se pulsa; la pone, la sigue y la quita PantallaMapa.
    /// </summary>
    public class HojaCuenta : VisualElement
    {
        public const string ClaseCompleta = "cuenta--completa";
        public const string ClaseFormulario = "cuenta--formulario";

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
        public Button ConCorreo { get; }
        public Button AhoraNo { get; }
        /// <summary>Crear cuenta, entrar con correo u otra contraseña; a la vista en lugar de la tarjeta.</summary>
        public FormularioCuenta Formulario { get; }

        public bool AvisoVisible { get; private set; }

        /// <summary>El botón de entrar que se ve: Google, Reintentar o el del formulario.</summary>
        public Button Entrar => EnFormulario ? Formulario.Principal : AvisoVisible ? Reintentar : Google;

        bool EnFormulario => servicio.Paso != PasoCuenta.Botones;

        /// <summary>El título de lo que se ve.</summary>
        public Label TituloVisto => EnFormulario ? Formulario.Titulo : Titulo;

        /// <summary>El error o aviso a la vista; null si no hay ninguno.</summary>
        public string Mensaje => EnFormulario ? Formulario.Mensaje?.text : AvisoVisible ? Aviso.text : null;

        /// <summary>Lo que dice el botón mientras se espera.</summary>
        public string TextoEntrando => textos(FormularioCuenta.ClaveEntrando(servicio.Paso));

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
            ConCorreo = PantallaBienvenida.NuevoBoton("cuenta-con-correo", this.textos(ClavesTexto.CuentaConCorreo), PulsarConCorreo, principal: false);
            Boton(ConCorreo);
            pie.Add(ConCorreo);
            AhoraNo = new Button(servicio.AhoraNo)
                { name = "cuenta-ahora-no", text = this.textos(ClavesTexto.UbicacionAhoraNo), focusable = true };
            AhoraNo.AddToClassList("bienvenida__enlace");
            AhoraNo.AddToClassList("cuenta__ahora-no");
            AhoraNo.AddToClassList("t-heading");
            pie.Add(AhoraNo);

            Formulario = new FormularioCuenta(servicio, this.textos);
            Add(Formulario);

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

        internal static void Boton(Button b)
        {
            b.AddToClassList("cuenta__boton");
            b.RemoveFromClassList("t-label");
            b.AddToClassList("t-heading");
        }

        /// <summary>«Continuar con Google» o «Reintentar».</summary>
        public async void PulsarEntrar() => await servicio.EntrarConGoogle();

        public void PulsarConCorreo() => servicio.IrA(PasoCuenta.Crear);

        /// <summary>Enseña el estado del servicio: los botones o el formulario, el error (si lo hay) y los textos.</summary>
        public void Actualizar()
        {
            EnableInClassList(ClaseFormulario, EnFormulario);
            Tarjeta.style.display = PantallaBienvenida.Ver(!EnFormulario);
            Formulario.style.display = PantallaBienvenida.Ver(EnFormulario);
            Formulario.Actualizar();
            if (EnFormulario) return;
            AvisoVisible = servicio.Error != null;
            if (AvisoVisible) Aviso.text = textos(ClaveError(servicio.Error));
            Para.style.display = PantallaBienvenida.Ver(!AvisoVisible);
            Aviso.style.display = PantallaBienvenida.Ver(AvisoVisible);
            Google.style.display = PantallaBienvenida.Ver(!AvisoVisible);
            Reintentar.style.display = PantallaBienvenida.Ver(AvisoVisible);
            TextoGoogle.text = textos(servicio.Entrando ? ClavesTexto.CuentaEntrando : ClavesTexto.CuentaGoogle);
            Reintentar.text = textos(servicio.Entrando ? ClavesTexto.CuentaEntrando : ClavesTexto.CuentaReintentar);
        }

        /// <summary>Clave del texto de un código de ErroresCuenta en ese paso.</summary>
        public static string ClaveError(string codigo, PasoCuenta paso = PasoCuenta.Botones)
        {
            switch (codigo)
            {
                case ErroresCuenta.NoDisponible when paso != PasoCuenta.Botones: return ClavesTexto.CuentaErrorServidorCorreo;
                case ErroresCuenta.ContrasenaVacia: return ClavesTexto.CuentaErrorContrasenaVacia;
                case ErroresCuenta.SinConexion: return ClavesTexto.CuentaErrorConexion;
                case ErroresCuenta.NoDisponible: return ClavesTexto.CuentaErrorServidor;
                case ErroresCuenta.CorreoUsado: return ClavesTexto.CuentaErrorCorreoUsado;
                case ErroresCuenta.CorreoNoValido: return ClavesTexto.CuentaErrorCorreo;
                case ErroresCuenta.ContrasenaDebil: return ClavesTexto.CuentaErrorContrasena;
                case ErroresCuenta.NoCoinciden: return ClavesTexto.CuentaErrorNoCoinciden;
                case ErroresCuenta.DemasiadosIntentos: return ClavesTexto.CuentaErrorIntentos;
                default:
                    return paso == PasoCuenta.Crear ? ClavesTexto.CuentaErrorCrear
                        : paso == PasoCuenta.Olvidada ? ClavesTexto.CuentaErrorEnviar : ClavesTexto.CuentaError;
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
            yield return Control(ConCorreo, () => ConCorreo.text, PulsarConCorreo);
            yield return Control(AhoraNo, () => AhoraNo.text, servicio.AhoraNo);
            foreach (var c in Formulario.Controles()) yield return c;
        }

        PantallaBienvenida.ControlLector Control(VisualElement e, Func<string> etiqueta, Action accion = null, Func<bool> visible = null) =>
            new PantallaBienvenida.ControlLector
            {
                Elemento = e, Etiqueta = etiqueta, Accion = accion, Visible = () => !EnFormulario && (visible == null || visible()),
            };

        /// <summary>Deja a la vista ese texto (foco de TalkBack en uno que hay que desplazar).</summary>
        public void DesplazarHasta(VisualElement elemento)
        {
            if (EnFormulario) { Formulario.DesplazarHasta(elemento); return; }
            if (elemento == null || Pagina.panel == null || !Pagina.contentContainer.Contains(elemento)) return;
            Pagina.ScrollTo(elemento);
        }

        /// <summary>Vuelve a dibujar los iconos (su color llega con el estilo del tema).</summary>
        public void Repintar() => Formulario.Repintar();

        /// <summary>
        /// Barra de estado, barra de gestos y bordes (dp), con Screen.safeArea (PantallaMapa). La hoja llega al borde inferior
        /// y su contenido termina encima de la barra de gestos.
        /// </summary>
        public void PonerZonaSegura(float izquierda, float arriba, float derecha, float abajo)
        {
            style.paddingTop = Mathf.Max(0f, arriba);
            style.paddingLeft = Mathf.Max(0f, izquierda);
            style.paddingRight = Mathf.Max(0f, derecha);
            Tarjeta.style.paddingBottom = Formulario.style.paddingBottom = Mathf.Max(0f, abajo);
        }
    }
}
