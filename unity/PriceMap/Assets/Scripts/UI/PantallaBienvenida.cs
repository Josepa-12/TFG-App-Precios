using System;
using System.Collections.Generic;
using System.Linq;
using PriceMap.Aplicacion;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Bienvenida del primer arranque (historia 3.1; maqueta A del 23/11/2026): a pantalla completa y opaca, encima de todo.
    /// Tres pasos (qué es PriceMap con el logo, la privacidad y los permisos) y el aviso de privacidad completo; arriba «‹»,
    /// abajo los puntos de progreso y el botón principal. El contenido de cada página se desplaza si no cabe (texto grande).
    /// Enseña el estado de ServicioBienvenida y le manda lo que se pulsa; la pone, la sigue y la quita PantallaMapa.
    /// </summary>
    public class PantallaBienvenida : VisualElement
    {
        /// <summary>Con el texto del sistema así de grande o más, los iconos van encima del texto y los botones, en columna.</summary>
        public const float EscalaGrande = 1.4f;
        public const string ClaseGrande = "bienvenida--grande";
        public const string ClasePuntoActual = "bienvenida__punto--actual";

        /// <summary>
        /// Segundos tras cambiar de página en los que el botón principal no responde: «Siguiente» y «Empezar» son el mismo
        /// botón, y dos toques seguidos se saltarían un paso.
        /// </summary>
        public const float EsperaPrincipal = 0.4f;

        /// <summary>Reloj en segundos; las pruebas lo sustituyen.</summary>
        internal Func<float> Reloj = () => Time.realtimeSinceStartup;

        /// <summary>Índice de la página del aviso, detrás de los tres pasos.</summary>
        public const int PaginaAviso = ServicioBienvenida.Pasos;

        /// <summary>Lo que recorre TalkBack, en orden: texto (sin acción) o botón, y en qué página está.</summary>
        public class ControlLector
        {
            public VisualElement Elemento;
            public Func<string> Etiqueta;
            /// <summary>Valor del nodo (lo escrito en un campo); null si no tiene.</summary>
            public Func<string> Valor;
            public Action Accion;
            public Func<bool> Visible;
        }

        readonly ServicioBienvenida servicio;
        readonly Func<string, string> textos;
        readonly ScrollView[] paginas = new ScrollView[ServicioBienvenida.Pasos + 1];
        readonly Label[] titulos = new Label[ServicioBienvenida.Pasos + 1];
        readonly VisualElement[] puntos = new VisualElement[ServicioBienvenida.Pasos];
        readonly List<ControlLector> controles = new List<ControlLector>();
        readonly VisualElement filaPuntos;
        int paginaVista = -1;                             // la que se enseñó por última vez
        float cambioPagina = float.NegativeInfinity;      // cuándo se cambió a ella (la primera no cuenta)

        /// <summary>«‹»: vuelve atrás (no se ve en el primer paso).</summary>
        public VisualElement BotonAtras { get; }
        /// <summary>El toque de «‹», para las pruebas.</summary>
        internal Clickable ToqueAtras { get; }
        /// <summary>«Siguiente» o, en el último paso, «Empezar».</summary>
        public Button Principal { get; }
        /// <summary>«Leer el aviso completo», en el paso de la privacidad.</summary>
        public Button LeerAviso { get; }
        public TarjetaPermiso Ubicacion { get; }
        public TarjetaPermiso Notificaciones { get; }
        /// <summary>Sitio del consentimiento a la evaluación del TFG (historia 6.2): vacío de momento.</summary>
        public VisualElement HuecoEvaluacion { get; }
        /// <summary>Nombre y correo del responsable: su texto se puede seleccionar.</summary>
        public Label Responsable { get; }
        public Label Correo { get; }
        public IReadOnlyList<ScrollView> Paginas => paginas;
        public IReadOnlyList<VisualElement> Puntos => puntos;
        public IReadOnlyList<ControlLector> Controles => controles;

        /// <summary>Página a la vista: el paso (0 a 2) o PaginaAviso.</summary>
        public int Pagina => servicio.AvisoAbierto ? PaginaAviso : servicio.Paso;

        /// <summary>Título de la página a la vista: el primer foco de TalkBack.</summary>
        public Label Titulo => titulos[Pagina];

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public PantallaBienvenida(ServicioBienvenida servicio, Func<string, string> textos = null)
        {
            this.servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            this.textos = textos ?? (c => Textos.T(c));
            name = "bienvenida";
            AddToClassList("bienvenida");
            pickingMode = PickingMode.Position; // nada de lo de debajo responde
            RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(this, EscalaFuente.Sistema));

            var cabecera = Nuevo("bienvenida-cabecera", "bienvenida__cabecera");
            Add(cabecera);
            BotonAtras = new VisualElement { name = "bienvenida-atras", focusable = true, pickingMode = PickingMode.Position };
            BotonAtras.AddToClassList("bienvenida__atras");
            BotonAtras.Add(new IconoBienvenida(GlifoBienvenida.Atras));
            BotonAtras.AddManipulator(ToqueAtras = new Clickable(PulsarAtras));
            BotonAtras.RegisterCallback<NavigationSubmitEvent>(_ => PulsarAtras());
            cabecera.Add(BotonAtras);

            for (int i = 0; i < paginas.Length; i++)
            {
                // Sin barras de desplazamiento, como la ficha: se arrastra con el dedo
                paginas[i] = new ScrollView(ScrollViewMode.Vertical)
                {
                    name = i == PaginaAviso ? "bienvenida-aviso" : "bienvenida-paso-" + (i + 1),
                    horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                    verticalScrollerVisibility = ScrollerVisibility.Hidden,
                };
                paginas[i].AddToClassList("bienvenida__pagina");
                // Con la fuente grande: cada página se dispone al enseñarla; entonces se le aplica la escala
                var pagina = paginas[i];
                pagina.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(pagina, EscalaFuente.Sistema));
                Add(pagina);
            }

            // Paso 1: el logo (el de la pantalla de inicio), el título centrado y tres filas
            var logo = Nuevo("bienvenida-logo", "bienvenida__logo");
            logo.Add(Nuevo("bienvenida-pin", "bienvenida__pin"));
            logo.Add(Nuevo("bienvenida-nombre", "bienvenida__nombre"));
            paginas[0].Add(logo);
            PonerTitulo(0, ClavesTexto.BienvenidaTituloPrecios).AddToClassList("bienvenida__titulo--centrado");
            Fila(0, GlifoBienvenida.Mapa, ClavesTexto.BienvenidaMira);
            Fila(0, GlifoBienvenida.Personas, ClavesTexto.BienvenidaNegocios);
            Fila(0, GlifoBienvenida.Navegar, ClavesTexto.BienvenidaGuia);

            // Paso 2: la privacidad
            PonerTitulo(1, ClavesTexto.BienvenidaTituloPrivacidad);
            Fila(1, GlifoBienvenida.Pin, ClavesTexto.BienvenidaPrivacidadUbicacion);
            Fila(1, GlifoBienvenida.Flecha, ClavesTexto.BienvenidaPrivacidadEnvios);
            Fila(1, GlifoBienvenida.Personas, ClavesTexto.BienvenidaPrivacidadAportes);
            Fila(1, GlifoBienvenida.Persona, ClavesTexto.BienvenidaPrivacidadCuenta);

            // Paso 3: los permisos
            PonerTitulo(2, ClavesTexto.BienvenidaTituloPermisos);
            Ubicacion = Tarjeta("ubicacion", GlifoBienvenida.Pin, ClavesTexto.BienvenidaUbicacion, ClavesTexto.BienvenidaUbicacionPara,
                servicio.PermitirUbicacion, servicio.AhoraNoUbicacion);
            Notificaciones = Tarjeta("notificaciones", GlifoBienvenida.Campana, ClavesTexto.BienvenidaNotificaciones,
                ClavesTexto.BienvenidaNotificacionesPara, servicio.PermitirNotificaciones, servicio.AhoraNoNotificaciones);
            HuecoEvaluacion = Nuevo("hueco-evaluacion", "bienvenida__hueco-evaluacion");
            paginas[2].Add(HuecoEvaluacion);
            var cambiar = Texto("bienvenida-cambiar", "bienvenida__cambiar", "t-meta", ClavesTexto.BienvenidaCambiar);
            paginas[2].Add(cambiar);
            Declarar(2, cambiar);

            // El aviso completo
            PonerTitulo(PaginaAviso, ClavesTexto.AvisoTitulo);
            Apartado(ClavesTexto.AvisoDatosTitulo, ClavesTexto.AvisoDatos);
            Apartado(ClavesTexto.AvisoSaleTitulo, ClavesTexto.AvisoSale);
            Apartado(ClavesTexto.AvisoGuardaTitulo, ClavesTexto.AvisoGuarda);
            Responsable = Apartado(ClavesTexto.AvisoResponsableTitulo, ClavesTexto.AvisoResponsable);
            Responsable.AddToClassList("bienvenida__responsable");
            Correo = Texto("bienvenida-correo", "bienvenida__parrafo", "t-body", ClavesTexto.AvisoCorreo);
            paginas[PaginaAviso].Add(Correo);
            Declarar(PaginaAviso, Correo);
            Seleccionable(Responsable);
            Seleccionable(Correo);

            var pie = Nuevo("bienvenida-pie", "bienvenida__pie");
            Add(pie);
            LeerAviso = new Button(servicio.AbrirAviso) { name = "bienvenida-leer-aviso", focusable = true };
            LeerAviso.text = "<u>" + this.textos(ClavesTexto.BienvenidaLeerAviso) + "</u>";
            LeerAviso.AddToClassList("bienvenida__enlace");
            LeerAviso.AddToClassList("t-heading");
            pie.Add(LeerAviso);
            Declarar(() => Pagina == ServicioBienvenida.PasoPrivacidad, LeerAviso, () => this.textos(ClavesTexto.BienvenidaLeerAviso), servicio.AbrirAviso);
            filaPuntos = Nuevo("bienvenida-puntos", "bienvenida__puntos");
            pie.Add(filaPuntos);
            for (int i = 0; i < puntos.Length; i++) filaPuntos.Add(puntos[i] = Nuevo("bienvenida-punto-" + (i + 1), "bienvenida__punto"));
            Declarar(() => Pagina != PaginaAviso, filaPuntos,
                () => Textos.Formatear(this.textos(ClavesTexto.BienvenidaPasoDe), servicio.Paso + 1, ServicioBienvenida.Pasos));
            Principal = NuevoBoton("bienvenida-principal", "", PulsarPrincipal, principal: true);
            Principal.AddToClassList("bienvenida__principal");
            Principal.RemoveFromClassList("t-label");
            Principal.AddToClassList("t-heading");
            pie.Add(Principal);
            Declarar(() => Pagina != PaginaAviso, Principal, () => Principal.text, PulsarPrincipal);
            Declarar(() => Pagina != 0, BotonAtras, () => this.textos(ClavesTexto.BienvenidaAtras), PulsarAtras);

            Actualizar();
        }

        // ───────────────────────── Piezas ─────────────────────────

        internal static VisualElement Nuevo(string nombre, string clase)
        {
            var e = new VisualElement { name = nombre, pickingMode = PickingMode.Ignore };
            e.AddToClassList(clase);
            return e;
        }

        Label Texto(string nombre, string clase, string tipografia, string clave)
        {
            var l = new Label(textos(clave)) { name = nombre, pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            return l;
        }

        /// <summary>Botón como los de la ventana de la ubicación (explicacion-ubicacion__boton).</summary>
        internal static Button NuevoBoton(string nombre, string texto, Action alPulsar, bool principal)
        {
            var b = new Button(alPulsar) { name = nombre, text = texto, focusable = true };
            b.AddToClassList("explicacion-ubicacion__boton");
            b.AddToClassList(principal ? "explicacion-ubicacion__boton--principal" : "explicacion-ubicacion__boton--secundario");
            b.AddToClassList("t-label");
            Semicirculo.Mantener(b);
            return b;
        }

        Label PonerTitulo(int pagina, string clave)
        {
            var l = Texto(paginas[pagina].name + "-titulo", "bienvenida__titulo", "t-display", clave);
            titulos[pagina] = l;
            paginas[pagina].Add(l);
            Declarar(pagina, l);
            return l;
        }

        void Fila(int pagina, GlifoBienvenida glifo, string clave)
        {
            var fila = Nuevo("bienvenida-fila", "bienvenida__fila");
            var icono = Nuevo("bienvenida-icono", "bienvenida__icono");
            icono.Add(new IconoBienvenida(glifo));
            fila.Add(icono);
            var texto = Texto("bienvenida-fila-texto", "bienvenida__fila-texto", "t-body", clave);
            fila.Add(texto);
            paginas[pagina].Add(fila);
            Declarar(pagina, texto);
        }

        TarjetaPermiso Tarjeta(string nombre, GlifoBienvenida glifo, string claveTitulo, string clavePara, Action alPermitir, Action alAhoraNo)
        {
            var t = new TarjetaPermiso("bienvenida-" + nombre, glifo, textos(claveTitulo), textos(clavePara), alPermitir, alAhoraNo, textos);
            paginas[ServicioBienvenida.PasoPermisos].Add(t);
            string Anuncio(string que) => Textos.Formatear(textos(ClavesTexto.BienvenidaAnuncioPermiso), que, t.Titulo.text);
            bool EnPaso() => Pagina == ServicioBienvenida.PasoPermisos;
            Declarar(EnPaso, t.Cabecera, () => Textos.Formatear(textos(ClavesTexto.BienvenidaAnuncioPermiso), t.Titulo.text, t.Para.text));
            Declarar(() => EnPaso() && t.PermitidoVisible, t.FilaPermitido, () => Anuncio(t.Permitido.text));
            Declarar(() => EnPaso() && t.DespuesVisible, t.Despues, () => t.Despues.text);
            Declarar(() => EnPaso() && t.AhoraNoVisible, t.AhoraNo, () => Anuncio(t.AhoraNo.text), alAhoraNo);
            Declarar(() => EnPaso() && t.PermitirVisible, t.Permitir, () => Anuncio(t.Permitir.text), alPermitir);
            return t;
        }

        Label Apartado(string claveTitulo, string claveTexto)
        {
            var pagina = paginas[PaginaAviso];
            var titulo = Texto("bienvenida-apartado", "bienvenida__apartado", "t-heading", claveTitulo);
            pagina.Add(titulo);
            Declarar(PaginaAviso, titulo);
            var parrafo = Texto("bienvenida-parrafo", "bienvenida__parrafo", "t-body", claveTexto);
            pagina.Add(parrafo);
            Declarar(PaginaAviso, parrafo);
            return parrafo;
        }

        static void Seleccionable(Label l)
        {
            l.pickingMode = PickingMode.Position;
            l.selection.isSelectable = true;
        }

        void Declarar(int pagina, Label texto) => Declarar(() => Pagina == pagina, texto, () => texto.text);

        void Declarar(Func<bool> visible, VisualElement elemento, Func<string> etiqueta, Action accion = null) =>
            controles.Add(new ControlLector { Elemento = elemento, Etiqueta = etiqueta, Accion = accion, Visible = visible });

        // ───────────────────────── Uso ─────────────────────────

        /// <summary>«Siguiente» o «Empezar»; recién cambiada la página (EsperaPrincipal), nada.</summary>
        public void PulsarPrincipal()
        {
            if (Reloj() - cambioPagina < EsperaPrincipal) return;
            if (servicio.Paso == ServicioBienvenida.PasoPermisos) servicio.Terminar();
            else servicio.Siguiente();
        }

        public void PulsarAtras() => servicio.Atras();

        /// <summary>Enseña la página, los puntos, los botones y las tarjetas como está el servicio.</summary>
        public void Actualizar()
        {
            int pagina = Pagina;
            if (paginaVista >= 0 && pagina != paginaVista) cambioPagina = Reloj();
            paginaVista = pagina;
            for (int i = 0; i < paginas.Length; i++)
            {
                bool antes = paginas[i].style.display.value == DisplayStyle.Flex;
                paginas[i].style.display = Ver(i == pagina);
                if (i == pagina && !antes) paginas[i].scrollOffset = Vector2.zero;
            }
            for (int i = 0; i < puntos.Length; i++) puntos[i].EnableInClassList(ClasePuntoActual, i == servicio.Paso);
            BotonAtras.style.visibility = pagina == 0 ? Visibility.Hidden : Visibility.Visible;
            LeerAviso.style.display = Ver(pagina == ServicioBienvenida.PasoPrivacidad);
            filaPuntos.style.display = Ver(pagina != PaginaAviso);
            Principal.style.display = Ver(pagina != PaginaAviso);
            Principal.text = textos(servicio.Paso == ServicioBienvenida.PasoPermisos ? ClavesTexto.BienvenidaEmpezar : ClavesTexto.BienvenidaSiguiente);
            Ubicacion.Poner(servicio.Ubicacion);
            Notificaciones.Poner(servicio.Notificaciones);
        }

        internal static DisplayStyle Ver(bool visible) => visible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>Deja a la vista ese elemento de la página (foco de TalkBack en un texto que hay que desplazar).</summary>
        public void DesplazarHasta(VisualElement elemento)
        {
            var pagina = paginas[Pagina];
            if (elemento == null || !pagina.contentContainer.Contains(elemento)) return;
            UltimoDesplazado = elemento;
            if (pagina.panel != null) pagina.ScrollTo(elemento); // sin panel (pruebas), ScrollTo falla
        }

        /// <summary>Último elemento pedido a DesplazarHasta (para las pruebas).</summary>
        internal VisualElement UltimoDesplazado { get; private set; }

        /// <summary>Barra de estado, barra de gestos y bordes (dp), con Screen.safeArea (PantallaMapa).</summary>
        public void PonerZonaSegura(float izquierda, float arriba, float derecha, float abajo)
        {
            style.paddingLeft = Mathf.Max(0f, izquierda);
            style.paddingTop = Mathf.Max(0f, arriba);
            style.paddingRight = Mathf.Max(0f, derecha);
            style.paddingBottom = Mathf.Max(0f, abajo);
        }

        /// <summary>Escala de fuente del sistema: con texto grande, el icono va encima y los botones en columna.</summary>
        public void PonerEscala(float escala) => EnableInClassList(ClaseGrande, escala >= EscalaGrande);

        /// <summary>Vuelve a dibujar los iconos (su color llega con el estilo del tema).</summary>
        public void Repintar() => this.Query<IconoBienvenida>().ForEach(i => i.MarkDirtyRepaint());

        /// <summary>Tarjeta que contiene ese elemento; null si no está en ninguna.</summary>
        public TarjetaPermiso TarjetaDe(VisualElement elemento) =>
            elemento == null ? null : new[] { Ubicacion, Notificaciones }.FirstOrDefault(t => t.Contains(elemento));
    }

    /// <summary>
    /// Tarjeta de un permiso en la bienvenida: icono, título y para qué es; debajo, «Ahora no» y «Permitir», «✓ Permitido» o
    /// «Puedes activarlo después en Ajustes» (con «Permitir» si Android aún puede preguntar).
    /// </summary>
    public class TarjetaPermiso : VisualElement
    {
        /// <summary>Icono, título y para qué es (una sola entrada de TalkBack).</summary>
        public VisualElement Cabecera { get; }
        public Label Titulo { get; }
        public Label Para { get; }
        public VisualElement FilaPermitido { get; }
        public Label Permitido { get; }
        public Label Despues { get; }
        public Button AhoraNo { get; }
        public Button Permitir { get; }

        public Consentimiento Estado { get; private set; }
        public bool PermitidoVisible => Estado == Consentimiento.Permitido;
        public bool DespuesVisible => Estado == Consentimiento.Rechazado || Estado == Consentimiento.Bloqueado;
        public bool AhoraNoVisible => Estado == Consentimiento.SinContestar;
        public bool PermitirVisible => Estado == Consentimiento.SinContestar || Estado == Consentimiento.Rechazado;

        readonly VisualElement botones;

        public TarjetaPermiso(string nombre, GlifoBienvenida glifo, string titulo, string para, Action alPermitir, Action alAhoraNo,
            Func<string, string> textos)
        {
            name = nombre;
            AddToClassList("bienvenida__tarjeta");
            pickingMode = PickingMode.Ignore;

            Cabecera = PantallaBienvenida.Nuevo(nombre + "-cabecera", "bienvenida__fila");
            Cabecera.AddToClassList("bienvenida__fila--tarjeta");
            Add(Cabecera);
            var icono = PantallaBienvenida.Nuevo(nombre + "-icono", "bienvenida__icono");
            icono.Add(new IconoBienvenida(glifo));
            Cabecera.Add(icono);
            var columna = PantallaBienvenida.Nuevo(nombre + "-textos", "bienvenida__tarjeta-textos");
            Cabecera.Add(columna);
            Titulo = Texto(nombre + "-titulo", "bienvenida__tarjeta-titulo", "t-heading", titulo);
            columna.Add(Titulo);
            Para = Texto(nombre + "-para", "bienvenida__tarjeta-para", "t-body", para);
            columna.Add(Para);

            FilaPermitido = PantallaBienvenida.Nuevo(nombre + "-permitido", "bienvenida__permitido");
            FilaPermitido.Add(new IconoBienvenida(GlifoBienvenida.Hecho));
            Permitido = Texto(nombre + "-permitido-texto", "bienvenida__permitido-texto", "t-heading", textos(ClavesTexto.BienvenidaPermitido));
            FilaPermitido.Add(Permitido);
            Add(FilaPermitido);

            Despues = Texto(nombre + "-despues", "bienvenida__despues", "t-meta", textos(ClavesTexto.BienvenidaDespues));
            Add(Despues);

            botones = PantallaBienvenida.Nuevo(nombre + "-botones", "bienvenida__botones");
            Add(botones);
            AhoraNo = PantallaBienvenida.NuevoBoton(nombre + "-ahora-no", textos(ClavesTexto.UbicacionAhoraNo), alAhoraNo, principal: false);
            AhoraNo.AddToClassList("bienvenida__boton");
            AhoraNo.AddToClassList("bienvenida__boton--primero");
            botones.Add(AhoraNo);
            Permitir = PantallaBienvenida.NuevoBoton(nombre + "-permitir", textos(ClavesTexto.UbicacionPermitir), alPermitir, principal: true);
            Permitir.AddToClassList("bienvenida__boton");
            botones.Add(Permitir);
        }

        static Label Texto(string nombre, string clase, string tipografia, string texto)
        {
            var l = new Label(texto) { name = nombre, pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            return l;
        }

        /// <summary>Lo primero que hay que leer de su estado (foco de TalkBack tras contestar).</summary>
        public VisualElement Respuesta => PermitidoVisible ? FilaPermitido : DespuesVisible ? (VisualElement)Despues : Cabecera;

        public void Poner(Consentimiento estado)
        {
            Estado = estado;
            FilaPermitido.style.display = PantallaBienvenida.Ver(PermitidoVisible);
            Despues.style.display = PantallaBienvenida.Ver(DespuesVisible);
            AhoraNo.style.display = PantallaBienvenida.Ver(AhoraNoVisible);
            Permitir.style.display = PantallaBienvenida.Ver(PermitirVisible);
            botones.style.display = PantallaBienvenida.Ver(AhoraNoVisible || PermitirVisible);
        }
    }
}
