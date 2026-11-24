using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// La cuenta con correo (historia 3.3, parte 2; maqueta del 24/11/2026), a pantalla completa dentro de HojaCuenta: crear
    /// cuenta, entrar o pedir otra contraseña, según el paso de ServicioCuentas. Los errores van junto a su campo y los demás,
    /// en un aviso sobre los botones. Con un campo enfocado los botones van justo debajo y queda sitio para el teclado. El
    /// correo se conserva entre pasos; la contraseña se vacía al cambiar.
    /// </summary>
    public class FormularioCuenta : VisualElement
    {
        public const string ClaseTeclado = "cuenta-form--teclado";
        public const string ClaseMal = "cuenta-form__caja--mal";
        public const string ClaseFoco = "cuenta-form__caja--foco";

        /// <summary>Parte de la pantalla que se da por tapada por el teclado (y su línea de texto en Android).</summary>
        public const float FraccionTeclado = 0.5f;

        /// <summary>Lo que se espera (ms) a ver si otro campo coge el foco antes de devolver los botones abajo.</summary>
        const long EsperaSinTeclado = 400;
        const long EsperaDisposicion = 50;

        readonly ServicioCuentas servicio;
        readonly Func<string, string> textos;
        readonly VisualElement cabecera;
        readonly VisualElement grupoCorreo, grupoContrasena;
        PasoCuenta pasoVisto = PasoCuenta.Botones;
        TextField enfocado;                 // el campo con el foco; null si ninguno
        TouchScreenKeyboard tecladoAbierto; // el suyo en el móvil
        bool dedoAbajo;

        /// <summary>Para las pruebas: hay un dedo puesto, y lo último que se ha pedido dejar a la vista.</summary>
        internal bool DedoAbajo => dedoAbajo;
        internal VisualElement UltimoDesplazado { get; private set; }

        public Label Titulo { get; }
        public VisualElement BotonAtras { get; }
        /// <summary>El toque de «‹», para las pruebas.</summary>
        internal Clickable ToqueAtras { get; }
        public ScrollView Pagina { get; }
        /// <summary>Qué va a pasar («He olvidado la contraseña»).</summary>
        public Label Para { get; }
        public VisualElement CajaCorreo { get; }
        public TextField Correo { get; }
        public Label ErrorCorreo { get; }
        /// <summary>«Entrar», bajo el error de un correo que ya tiene cuenta.</summary>
        public Button EntrarConEse { get; }
        public VisualElement CajaContrasena { get; }
        public TextField Contrasena { get; }
        /// <summary>«Ver» u «Ocultar».</summary>
        public Button Ver { get; }
        public Label Ayuda { get; }
        public Label ErrorContrasena { get; }
        public Button Olvidada { get; }
        /// <summary>Un error que no es de ningún campo o, enviado el enlace, lo que hay que hacer.</summary>
        public Label Aviso { get; }
        public VisualElement Hueco { get; }
        public Button Principal { get; }
        /// <summary>«Ya tengo cuenta», «Crear cuenta» o «Volver a entrar».</summary>
        public Button Secundario { get; }

        /// <summary>Error o aviso a la vista, para anunciarlo; null si no hay.</summary>
        public Label Mensaje { get; private set; }

        /// <summary>Con los botones debajo de los campos y sitio para el teclado.</summary>
        public bool ConTeclado { get; private set; }

        /// <summary>Ha cambiado lo que TalkBack dice de ese control (el correo escrito, «Ver»).</summary>
        public event Action<VisualElement> EtiquetaCambiada;

        public FormularioCuenta(ServicioCuentas servicio, Func<string, string> textos)
        {
            this.servicio = servicio;
            this.textos = textos;
            name = "cuenta-form";
            AddToClassList("cuenta-form");

            cabecera = PantallaBienvenida.Nuevo("cuenta-form-cabecera", "cuenta-form__cabecera");
            Add(cabecera);
            BotonAtras = new VisualElement { name = "cuenta-form-atras", focusable = true, pickingMode = PickingMode.Position };
            BotonAtras.AddToClassList("bienvenida__atras");
            BotonAtras.Add(new IconoBienvenida(GlifoBienvenida.Atras));
            BotonAtras.AddManipulator(ToqueAtras = new Clickable(servicio.Atras));
            BotonAtras.RegisterCallback<NavigationSubmitEvent>(_ => servicio.Atras());
            cabecera.Add(BotonAtras);
            Titulo = Texto(cabecera, "cuenta-form-titulo", "cuenta-form__titulo", "t-title");

            Pagina = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "cuenta-form-pagina",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            Pagina.AddToClassList("cuenta-form__pagina");
            Add(Pagina);
            Para = Texto(Pagina, "cuenta-form-para", "cuenta-form__para", "t-body", ClavesTexto.CuentaOlvidadaPara);

            grupoCorreo = Grupo("cuenta-form-correo", ClavesTexto.CuentaCorreo);
            CajaCorreo = Caja(grupoCorreo, Correo = new TextField { name = "cuenta-correo", keyboardType = TouchScreenKeyboardType.EmailAddress });
            ErrorCorreo = Texto(grupoCorreo, "cuenta-error-correo", "cuenta-form__error", "t-label");
            EntrarConEse = Enlace("cuenta-entrar-con-ese", ClavesTexto.CuentaEntrar, () => servicio.IrA(PasoCuenta.Entrar));
            grupoCorreo.Add(EntrarConEse);

            grupoContrasena = Grupo("cuenta-form-contrasena", ClavesTexto.CuentaContrasena);
            CajaContrasena = Caja(grupoContrasena, Contrasena = new TextField { name = "cuenta-contrasena", isPasswordField = true, maskChar = '•' });
            Ver = Enlace("cuenta-ver", ClavesTexto.CuentaVer, AlternarVer);
            Ver.AddToClassList("cuenta-form__ver");
            CajaContrasena.Add(Ver);
            Ayuda = Texto(grupoContrasena, "cuenta-ayuda", "cuenta-form__ayuda", "t-meta", ClavesTexto.CuentaContrasenaAyuda);
            ErrorContrasena = Texto(grupoContrasena, "cuenta-error-contrasena", "cuenta-form__error", "t-label");
            Olvidada = Enlace("cuenta-olvidada", ClavesTexto.CuentaOlvidada, () => servicio.IrA(PasoCuenta.Olvidada));
            Pagina.Add(Olvidada);

            Aviso = Texto(Pagina, "cuenta-form-aviso", "cuenta__aviso", "t-body");
            Hueco = PantallaBienvenida.Nuevo("cuenta-form-hueco", "cuenta-form__hueco");
            Pagina.Add(Hueco);
            Principal = PantallaBienvenida.NuevoBoton("cuenta-principal", "", Enviar, principal: true);
            HojaCuenta.Boton(Principal);
            Pagina.Add(Principal);
            Secundario = new Button(PulsarSecundario) { name = "cuenta-secundario", focusable = true };
            Secundario.AddToClassList("bienvenida__enlace");
            Secundario.AddToClassList("t-heading");
            Pagina.Add(Secundario);

            Correo.RegisterValueChangedCallback(_ => EtiquetaCambiada?.Invoke(Correo));
            RegisterCallback<PointerDownEvent>(_ => dedoAbajo = true, TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(_ => SoltarDedo(), TrickleDown.TrickleDown);
            RegisterCallback<PointerCancelEvent>(_ => SoltarDedo(), TrickleDown.TrickleDown);
            RegisterCallback<PointerCaptureOutEvent>(_ => SoltarDedo(), TrickleDown.TrickleDown); // lo tenía un botón o la página
            RegisterCallback<GeometryChangedEvent>(_ => DejarSitioAlTeclado());
            Pagina.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => DesplazarAlCampo());
            // Con la fuente grande: lo que sale después se dispone entonces
            this.Query<TextElement>().ForEach(t => t.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(this, EscalaFuente.Sistema)));
            Actualizar();
        }

        // ───────────────────────── Piezas ─────────────────────────

        Label Texto(VisualElement padre, string nombre, string clase, string tipografia, string clave = null)
        {
            var l = new Label(clave != null ? textos(clave) : "") { name = nombre, pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            padre.Add(l);
            return l;
        }

        Button Enlace(string nombre, string clave, Action alPulsar)
        {
            var b = new Button(alPulsar) { name = nombre, text = textos(clave), focusable = true };
            b.AddToClassList("bienvenida__enlace");
            b.AddToClassList("cuenta-form__enlace");
            b.AddToClassList("t-label");
            return b;
        }

        VisualElement Grupo(string nombre, string claveEtiqueta)
        {
            var g = PantallaBienvenida.Nuevo(nombre, "cuenta-form__grupo");
            Pagina.Add(g);
            Texto(g, nombre + "-etiqueta", "cuenta-form__etiqueta", "t-label", claveEtiqueta);
            return g;
        }

        VisualElement Caja(VisualElement grupo, TextField campo)
        {
            var caja = PantallaBienvenida.Nuevo(campo.name + "-caja", "cuenta-form__caja");
            grupo.Add(caja);
            campo.AddToClassList("cuenta-form__texto");
            campo.AddToClassList("t-body");
            campo.autoCorrection = false;
            caja.Add(campo);
            // Antes que el propio campo, que es quien abre y cierra el teclado
            campo.RegisterCallback<FocusInEvent>(_ => AlEnfocar(campo), TrickleDown.TrickleDown);
            campo.RegisterCallback<FocusOutEvent>(e => AlDesenfocar(campo, e.relatedTarget == null), TrickleDown.TrickleDown);
            campo.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter) return;
                e.StopPropagation();
                Intro(campo);
            }, TrickleDown.TrickleDown);
            return caja;
        }

        // ───────────────────────── Uso ─────────────────────────

        bool HayContrasena => servicio.Paso == PasoCuenta.Crear || servicio.Paso == PasoCuenta.Entrar;

        /// <summary>El botón principal (o Intro en el último campo): crea la cuenta, entra o pide el enlace.</summary>
        public async void Enviar()
        {
            switch (servicio.Paso)
            {
                case PasoCuenta.Crear: await servicio.CrearConCorreo(Correo.value, Contrasena.value); break;
                case PasoCuenta.Entrar: await servicio.EntrarConCorreo(Correo.value, Contrasena.value); break;
                case PasoCuenta.Olvidada: await servicio.EnviarContrasena(Correo.value); break;
            }
        }

        public void PulsarSecundario() => servicio.IrA(servicio.Paso == PasoCuenta.Entrar ? PasoCuenta.Crear : PasoCuenta.Entrar);

        /// <summary>«Ver» u «Ocultar» la contraseña.</summary>
        public void AlternarVer() => PonerVer(Contrasena.isPasswordField);

        void PonerVer(bool ver)
        {
            Contrasena.isPasswordField = !ver;
            Ver.text = textos(ver ? ClavesTexto.CuentaOcultar : ClavesTexto.CuentaVer);
            EtiquetaCambiada?.Invoke(Ver);
        }

        /// <summary>Intro en ese campo (también el del teclado del móvil): pasa al siguiente o, en el último, envía.</summary>
        internal void Intro(TextField campo)
        {
            if (campo == Correo && HayContrasena) Enfocar(Contrasena);
            else Enviar();
        }

        static void Enfocar(VisualElement e)
        {
            if (e.panel != null) e.Focus(); // sin panel (pruebas), nada
        }

        /// <summary>Suelta el teclado y vacía la contraseña, otra vez oculta (cambio de paso o cierre).</summary>
        public void VaciarContrasena()
        {
            tecladoAbierto = null; // antes de soltar el foco: cerrar el teclado no es un Intro
            enfocado = null;
            dedoAbajo = false;
            foreach (var c in new[] { Correo, Contrasena })
            {
                c.parent.RemoveFromClassList(ClaseFoco);
                if (c.panel != null) c.Blur();
            }
            Contrasena.SetValueWithoutNotify("");
            PonerVer(false);
            TecladoAndroid.PasarToques(false);
            PonerTeclado(false);
        }

        /// <summary>Enseña el paso del servicio, sus errores y los textos de los botones.</summary>
        public void Actualizar()
        {
            var paso = servicio.Paso;
            if (paso != pasoVisto)
            {
                pasoVisto = paso;
                Mensaje = null;
                VaciarContrasena();
                Pagina.scrollOffset = Vector2.zero;
            }
            if (paso == PasoCuenta.Botones) { Mensaje = null; return; }

            bool crear = paso == PasoCuenta.Crear, entrar = paso == PasoCuenta.Entrar, olvidada = paso == PasoCuenta.Olvidada;
            Titulo.text = textos(crear ? ClavesTexto.CuentaCrear : entrar ? ClavesTexto.CuentaEntrar : ClavesTexto.CuentaNuevaContrasena);
            PonerVisible(Para, olvidada && !servicio.Enviado);
            PonerVisible(grupoContrasena, !olvidada);
            PonerVisible(Olvidada, entrar);
            Secundario.text = textos(crear ? ClavesTexto.CuentaYaTengo : entrar ? ClavesTexto.CuentaCrear : ClavesTexto.CuentaVolverEntrar);

            string error = servicio.Error;
            bool deCorreo = error == ErroresCuenta.CorreoUsado || error == ErroresCuenta.CorreoNoValido;
            bool deContrasena = !olvidada && (error == ErroresCuenta.ContrasenaDebil || error == ErroresCuenta.ContrasenaVacia || error == ErroresCuenta.NoCoinciden);
            string texto = error != null ? textos(HojaCuenta.ClaveError(error, paso)) : "";
            ErrorCorreo.text = deCorreo ? texto : "";
            ErrorContrasena.text = deContrasena ? texto : "";
            bool otro = error != null && !deCorreo && !deContrasena;
            Aviso.text = otro ? texto : servicio.Enviado ? textos(ClavesTexto.CuentaEnviado) : "";
            PonerVisible(ErrorCorreo, deCorreo);
            PonerVisible(EntrarConEse, crear && error == ErroresCuenta.CorreoUsado);
            PonerVisible(ErrorContrasena, deContrasena);
            PonerVisible(Ayuda, crear && !deContrasena);
            PonerVisible(Aviso, otro || servicio.Enviado);
            CajaCorreo.EnableInClassList(ClaseMal, deCorreo);
            CajaContrasena.EnableInClassList(ClaseMal, deContrasena);
            Mensaje = deCorreo ? ErrorCorreo : deContrasena ? ErrorContrasena : otro || servicio.Enviado ? Aviso : null;

            Principal.text = textos(servicio.Entrando ? ClaveEntrando(paso)
                : otro && error != ErroresCuenta.DemasiadosIntentos ? ClavesTexto.CuentaReintentar
                : crear ? ClavesTexto.CuentaCrear
                : entrar ? ClavesTexto.CuentaEntrar
                : servicio.Enviado ? ClavesTexto.CuentaEnviarOtro : ClavesTexto.CuentaEnviar);
            if (!servicio.Entrando) EnsenarMensaje();
        }

        /// <summary>Clave del texto mientras se espera en ese paso.</summary>
        public static string ClaveEntrando(PasoCuenta paso) =>
            paso == PasoCuenta.Crear ? ClavesTexto.CuentaCreando : paso == PasoCuenta.Olvidada ? ClavesTexto.CuentaEnviando : ClavesTexto.CuentaEntrando;

        static void PonerVisible(VisualElement e, bool visible) => e.style.display = PantallaBienvenida.Ver(visible);

        /// <summary>Lo que recorre TalkBack, en orden; la contraseña nunca se dice.</summary>
        public IEnumerable<PantallaBienvenida.ControlLector> Controles()
        {
            yield return Control(Titulo, () => Titulo.text);
            yield return Control(Para, () => Para.text);
            yield return Control(Correo, () => textos(ClavesTexto.CuentaCorreo), () => Enfocar(Correo), valor: () => Correo.value);
            yield return Control(ErrorCorreo, () => ErrorCorreo.text);
            yield return Control(EntrarConEse, () => EntrarConEse.text, () => servicio.IrA(PasoCuenta.Entrar));
            yield return Control(Contrasena, () => textos(ClavesTexto.CuentaContrasena), () => Enfocar(Contrasena), grupoContrasena);
            yield return Control(Ver, () => textos(Contrasena.isPasswordField ? ClavesTexto.CuentaVerLector : ClavesTexto.CuentaOcultarLector), AlternarVer, grupoContrasena);
            yield return Control(Ayuda, () => Ayuda.text, visible: grupoContrasena);
            yield return Control(ErrorContrasena, () => ErrorContrasena.text, visible: grupoContrasena);
            yield return Control(Olvidada, () => Olvidada.text, () => servicio.IrA(PasoCuenta.Olvidada));
            yield return Control(Aviso, () => Aviso.text);
            yield return Control(Principal, () => Principal.text, Enviar);
            yield return Control(Secundario, () => Secundario.text, PulsarSecundario);
            yield return Control(BotonAtras, () => textos(ClavesTexto.BienvenidaAtras), servicio.Atras);
        }

        PantallaBienvenida.ControlLector Control(VisualElement e, Func<string> etiqueta, Action accion = null, VisualElement visible = null,
            Func<string> valor = null) => new PantallaBienvenida.ControlLector
        {
            Elemento = e, Etiqueta = etiqueta, Accion = accion, Valor = valor,
            Visible = () => servicio.Paso != PasoCuenta.Botones && SeVe(e) && (visible == null || SeVe(visible)),
        };

        static bool SeVe(VisualElement e) => e.style.display.value != DisplayStyle.None;

        /// <summary>Deja a la vista ese elemento (foco de TalkBack, un error).</summary>
        public void DesplazarHasta(VisualElement elemento)
        {
            if (elemento == null || !Pagina.contentContainer.Contains(elemento)) return;
            UltimoDesplazado = elemento;
            if (Pagina.panel != null) Pagina.ScrollTo(elemento); // sin panel (pruebas), ScrollTo falla
        }

        // Con el teclado no: manda el campo (DesplazarAlCampo). Se espera a que la página se haya dispuesto
        void EnsenarMensaje()
        {
            if (Mensaje == null || ConTeclado) return;
            if (panel == null) { DesplazarHasta(Mensaje); return; }
            schedule.Execute(() => { if (!ConTeclado) DesplazarHasta(Mensaje); }).ExecuteLater(EsperaDisposicion);
        }

        /// <summary>Vuelve a dibujar el icono de atrás (su color llega con el estilo del tema).</summary>
        public void Repintar() => this.Query<IconoBienvenida>().ForEach(i => i.MarkDirtyRepaint());

        // ───────────────────────── Teclado ─────────────────────────

        void AlEnfocar(TextField campo)
        {
            enfocado = campo;
            tecladoAbierto = null;
            campo.parent.AddToClassList(ClaseFoco);
            TecladoAndroid.PasarToques(true);
            PonerTeclado(true);
            if (panel != null) schedule.Execute(() => { if (enfocado == campo) tecladoAbierto = campo.touchScreenKeyboard; });
        }

        void AlDesenfocar(TextField campo, bool aNadie)
        {
            campo.parent.RemoveFromClassList(ClaseFoco);
            if (enfocado != campo) return;
            // Intro en el teclado del móvil: se cierra solo («Done»). Con un toque fuera es LostFocus y con atrás, Canceled
            bool intro = aNadie && !dedoAbajo && tecladoAbierto != null && tecladoAbierto.status == TouchScreenKeyboard.Status.Done;
            enfocado = null;
            tecladoAbierto = null;
            TecladoAndroid.PasarToques(false);
            if (panel == null) { PonerTeclado(false); return; }
            if (intro)
            {
                var paso = servicio.Paso;
                schedule.Execute(() => IntroSiSigue(campo, paso));
            }
            schedule.Execute(QuitarTeclado).ExecuteLater(EsperaSinTeclado);
        }

        /// <summary>El Intro del teclado del móvil llega un momento después: solo vale en el mismo paso y aún abierto.</summary>
        internal void IntroSiSigue(TextField campo, PasoCuenta paso)
        {
            if (panel != null && servicio.Paso == paso) Intro(campo);
        }

        void SoltarDedo()
        {
            if (!dedoAbajo) return;
            dedoAbajo = false;
            if (ConTeclado && enfocado == null) schedule.Execute(QuitarTeclado).ExecuteLater(EsperaSinTeclado);
        }

        // Con un dedo puesto no: el botón se iría de debajo antes de soltarlo
        internal void QuitarTeclado()
        {
            if (enfocado == null && !dedoAbajo) PonerTeclado(false);
        }

        /// <summary>Con el teclado: los botones justo debajo de los campos y sitio para desplazar lo que tapa.</summary>
        public void PonerTeclado(bool conTeclado)
        {
            if (ConTeclado == conTeclado) return;
            ConTeclado = conTeclado;
            EnableInClassList(ClaseTeclado, conTeclado);
            DejarSitioAlTeclado();
            if (conTeclado) return;
            Pagina.scrollOffset = Vector2.zero;
            EnsenarMensaje();
        }

        void DejarSitioAlTeclado()
        {
            float alto = layout.height;
            Pagina.contentContainer.style.paddingBottom = ConTeclado && alto > 0f ? new StyleLength(alto * FraccionTeclado) : new StyleLength(StyleKeyword.Null);
            DesplazarAlCampo();
        }

        void DesplazarAlCampo()
        {
            if (!ConTeclado || Pagina.panel == null) return;
            var grupo = enfocado == Contrasena ? grupoContrasena : grupoCorreo;
            float visto = layout.height * (1f - FraccionTeclado) - cabecera.layout.height;
            float y = Desplazamiento(grupo.layout.yMin, Principal.layout.yMax, visto);
            if (!float.IsNaN(y)) Pagina.scrollOffset = new Vector2(0f, y);
        }

        /// <summary>Lo que se desplaza para ver el botón en ese alto sin que se vaya por arriba el campo.</summary>
        public static float Desplazamiento(float arribaCampo, float abajoBoton, float visto) =>
            Mathf.Max(0f, Mathf.Min(arribaCampo, abajoBoton - visto));
    }
}
