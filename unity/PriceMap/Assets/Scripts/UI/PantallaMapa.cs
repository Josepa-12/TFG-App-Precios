using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Pantalla principal: el mapa a pantalla completa con los controles flotando encima.
    /// Recibe el mapa y el servicio de tema desde el Arranque (AD-2) y solo observa estado
    /// y envía comandos (AD-15). Las siguientes historias le añaden sus controles.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PantallaMapa : MonoBehaviour
    {
        /// <summary>Plaza del Cabildo, Sanlúcar de Barrameda: vista inicial del mapa.</summary>
        public static readonly Coordenada VistaInicial = new Coordenada(36.7779, -6.3528);

        IMapa mapa;
        ServicioTema tema;
        ServicioFiltro filtro;
        ServicioUbicacion ubicacion;
        VisualElement raiz, zonaSegura, zonaSeguraPrecios, zoom, velo;
        PildoraPrecios pildora;
        MenuPrecios menu;
        BotonRedondo botonTema, botonUbicacion, botonAcercar, botonAlejar;
        MarcadorPosicion marcadorPosicion;
        ExplicacionUbicacion explicacion;
        AvisoUbicacion aviso;
        FichaLugar ficha;
        DateTime ahora;              // "ahora" del Arranque (AD-12): con él se calculan los precios de la ficha
        float ultimaEscala;          // última escala px → dp con disposición (para medir si el usuario está centrado)
        string claveBotonUbicacion;  // etiqueta de TalkBack del botón de ubicación en el último fotograma
        Label atribucion;
        AccesibilidadPantalla accesibilidad;
        CapaMarcadores capa;
        CapaPuntosInteres capaPuntos;
        Rect zonaSeguraAplicada;
        float anchoAplicado;
        bool porNavegacion; // la última activación del filtro llegó por teclado o mando (NavigationSubmitEvent)
        bool temaYaAnunciado;// el primer tema (al arrancar) no se anuncia: solo los cambios del usuario

        /// <param name="lugares">Lugares que se dibujan como marcadores (vacío si la carga falló).</param>
        /// <param name="ahora">Momento con el que se calcula el resumen del precio (AD-12: lo da el Arranque).</param>
        /// <param name="filtro">Filtro "Precios"; sin él no hay píldora y se ven todos los lugares.</param>
        /// <param name="ubicacion">Posición del usuario; sin ella no hay botón de ubicación ni círculo.</param>
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora, ServicioFiltro filtro,
            ServicioUbicacion ubicacion = null)
        {
            var documento = GetComponent<UIDocument>();
            var raizDocumento = documento != null ? documento.rootVisualElement : null;
            if (raizDocumento != null) raizDocumento.pickingMode = PickingMode.Ignore; // la raíz no tapa el mapa
            Inicializar(mapa, tema, lugares, ahora, raizDocumento, filtro, ubicacion);
        }

        /// <summary>Igual, con el árbol de la interfaz dado (las pruebas lo construyen sin panel).</summary>
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora, VisualElement raizDocumento,
            ServicioFiltro filtro = null, ServicioUbicacion ubicacion = null)
        {
            this.mapa = mapa;
            this.tema = tema;
            this.filtro = filtro;
            this.ubicacion = ubicacion;
            this.ahora = ahora;
            this.mapa.Centrar(VistaInicial);

            raiz = raizDocumento?.Q("raiz");
            if (raiz == null) return; // sin interfaz (p. ej. en pruebas): el mapa funciona igual

            zonaSegura = raiz.Q("zona-segura");
            zonaSeguraPrecios = raiz.Q("zona-segura-precios");
            zoom = raiz.Q("zoom");
            atribucion = raiz.Q<Label>("atribucion");
            atribucion.text = Textos.T(ClavesTexto.MapaAtribucion);

            botonTema = new BotonRedondo(Icono.Luna, () => this.tema.Alternar()) { name = "boton-tema" };
            raiz.Q("hueco-tema").Add(botonTema);
            var huecoUbicacion = raiz.Q("hueco-ubicacion");
            if (this.ubicacion != null && huecoUbicacion != null)
            {
                botonUbicacion = new BotonRedondo(Icono.Diana, TocarUbicacion) { name = "boton-ubicacion" };
                huecoUbicacion.Add(botonUbicacion);
            }
            botonAcercar = new BotonRedondo(Icono.Mas, () => this.mapa.Acercar()) { name = "boton-acercar" };
            botonAlejar = new BotonRedondo(Icono.Menos, () => this.mapa.Alejar()) { name = "boton-alejar" };
            zoom.Add(botonAcercar);
            zoom.Add(botonAlejar);

            accesibilidad = new AccesibilidadPantalla(raiz);
            if (this.filtro != null) CrearFiltro(); // la píldora va arriba a la izquierda: primera en TalkBack
            accesibilidad.Boton(botonTema, () => Textos.T(ClavesTexto.TemaCambiar), () => this.tema.Alternar(), ValorTema);
            if (botonUbicacion != null)
                accesibilidad.Boton(botonUbicacion, () => Textos.T(ClaveBotonUbicacion()), TocarUbicacion);
            accesibilidad.Boton(botonAcercar, () => Textos.T(ClavesTexto.MapaAcercar), () => this.mapa.Acercar(), visible: () => AccesibilidadPantalla.LectorActivo);
            accesibilidad.Boton(botonAlejar, () => Textos.T(ClavesTexto.MapaAlejar), () => this.mapa.Alejar(), visible: () => AccesibilidadPantalla.LectorActivo);

            // Puntos de interés de OpenStreetMap, por debajo de los marcadores de lugar
            var contenedorPuntos = raiz.Q("capa-puntos");
            if (contenedorPuntos != null)
            {
                capaPuntos = new CapaPuntosInteres(contenedorPuntos, this.mapa);
                // TalkBack: cada punto es una entrada de texto que se da de alta y de baja con su vista
                capaPuntos.VistaCreada += RegistrarPunto;
                capaPuntos.VistaQuitada += QuitarPunto;
                foreach (var v in capaPuntos.Vistas) RegistrarPunto(v); // las creadas por el constructor
                this.mapa.PuntosInteresCambiados += AlCambiarPuntos;
            }

            // Marcadores de los lugares, debajo de los botones
            capa = new CapaMarcadores(raiz.Q("capa-marcadores"), this.mapa, ConResumen(lugares, ahora));
            foreach (var m in capa.Marcadores)
            {
                var marcador = m;
                // TalkBack: el doble toque en el marcador abre su ficha (historia 1.8, parte 3)
                accesibilidad.Marcador(marcador.Cuerpo, () => marcador.Anuncio, () => marcador.Visible, () => marcador.Posicion,
                    alActivar: () => AbrirFicha(marcador.Lugar));
            }

            // Ficha del lugar (historia 1.8): se abre al tocar un marcador, encima de los controles del mapa
            ficha = new FichaLugar(CerrarFicha);
            (raiz.Q("hueco-ficha") ?? raiz).Add(ficha);
            // Con la fuente grande: la ficha se dispone al abrirla, entonces se le aplica la escala
            ficha.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, AccessibilitySettings.fontScale));
            // Abierta sin medida (primer fotograma), el lugar se centró con el 72 %: al disponerse, con su borde real
            ficha.Hoja.RegisterCallback<GeometryChangedEvent>(_ => RecentrarFichaPendiente());
            this.mapa.Toque += AlTocarMapa;

            // Posición del usuario, encima de lugares y puntos, y la explicación del permiso (modal)
            if (this.ubicacion != null) CrearUbicacion();

            if (this.filtro != null)
            {
                this.filtro.Cambiado += AlCambiarFiltro;
                AlCambiarFiltro(this.filtro.Actual); // por si ya se inició; el Arranque lo inicia justo después
            }

            this.tema.Cambiado += AlCambiarTema;
            AssistiveSupport.screenReaderStatusChanged += AlCambiarLector;
            AccessibilitySettings.fontScaleChanged += AlCambiarEscalaFuente;
            raiz.RegisterCallback<GeometryChangedEvent>(_ => AjustarDisposicion());

            AlCambiarLector(AccesibilidadPantalla.LectorActivo);
            // El tema inicial llega con ServicioTema.Iniciar (lo llama el Arranque justo después)

            // Primera apertura sin permiso: la explicación sale antes de pedirlo, una sola vez
            if (this.ubicacion != null && this.ubicacion.DebeExplicarAlAbrir) AbrirExplicacion();
        }

        /// <summary>Círculo de la posición y ventana de la explicación del permiso, con su recorrido de TalkBack.</summary>
        void CrearUbicacion()
        {
            var capaPosicion = raiz.Q("capa-posicion");
            if (capaPosicion != null)
            {
                marcadorPosicion = new MarcadorPosicion();
                capaPosicion.Add(marcadorPosicion);
            }

            explicacion = new ExplicacionUbicacion(PermitirUbicacion, AhoraNoUbicacion);
            (raiz.Q("hueco-explicacion") ?? raiz).Add(explicacion);
            // Con la fuente grande: la ventana se dispone al abrirla, entonces se le aplica la escala
            explicacion.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, AccessibilitySettings.fontScale));
            // TalkBack: con la ventana abierta el recorrido es modal (texto, "Ahora no" y "Permitir")
            accesibilidad.Texto(explicacion.Texto, () => explicacion.Texto.text, () => ExplicacionAbierta, enModal: true);
            accesibilidad.Boton(explicacion.AhoraNo, () => explicacion.AhoraNo.text, AhoraNoUbicacion, visible: () => ExplicacionAbierta, enModal: true);
            accesibilidad.Boton(explicacion.Permitir, () => explicacion.Permitir.text, PermitirUbicacion, visible: () => ExplicacionAbierta, enModal: true);

            // Aviso de ubicación desactivada (denegado o apagada): misma ventana, mismo hueco y mismo recorrido modal
            aviso = new AvisoUbicacion(AbrirAjustesUbicacion, CerrarAviso);
            (raiz.Q("hueco-explicacion") ?? raiz).Add(aviso);
            aviso.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, AccessibilitySettings.fontScale));
            accesibilidad.Texto(aviso.Texto, () => aviso.Texto.text, () => AvisoAbierto, enModal: true);
            accesibilidad.Boton(aviso.Cerrar, () => aviso.Cerrar.text, CerrarAviso, visible: () => AvisoAbierto, enModal: true);
            accesibilidad.Boton(aviso.AbrirAjustes, () => aviso.AbrirAjustes.text, AbrirAjustesUbicacion, visible: () => AvisoAbierto, enModal: true);
        }

        /// <summary>True con la explicación del permiso de ubicación a la vista.</summary>
        public bool ExplicacionAbierta => explicacion != null && explicacion.Abierta;

        /// <summary>True con el aviso de ubicación desactivada a la vista.</summary>
        public bool AvisoAbierto => aviso != null && aviso.Abierta;

        /// <summary>True con alguna ventana modal de la ubicación (explicación o aviso) a la vista.</summary>
        public bool ModalUbicacionAbierta => ExplicacionAbierta || AvisoAbierto;

        /// <summary>
        /// Lo que hace el botón de ubicación (ServicioUbicacion.AlTocarBoton): sin permiso vuelve a salir la
        /// explicación; denegado o apagada, el aviso; buscando, nada; con posición, centra el mapa en el usuario o,
        /// si ya está centrado y el mapa girado, lo orienta al norte.
        /// </summary>
        public void TocarUbicacion()
        {
            if (ubicacion == null || ModalUbicacionAbierta) return;
            switch (AccionUbicacionActual())
            {
                case AccionUbicacion.Explicar: AbrirExplicacion(); break;
                case AccionUbicacion.Avisar: AbrirAviso(); break;
                case AccionUbicacion.Centrar: mapa.Centrar(ubicacion.Posicion.Value); break;
                case AccionUbicacion.OrientarNorte: mapa.OrientarNorte(); break;
            }
        }

        /// <summary>Lo que haría ahora el botón de ubicación, con la distancia en pantalla al centro y el giro del mapa.</summary>
        public AccionUbicacion AccionUbicacionActual() =>
            ubicacion == null ? AccionUbicacion.Nada : ubicacion.AlTocarBoton(DistanciaAlCentroDp(), mapa.Giro);

        /// <summary>
        /// Distancia (dp) en pantalla entre la posición del usuario y el centro de la vista (IMapa.Centro), con la
        /// escala de este fotograma; infinito sin posición o si alguno de los dos queda detrás de la cámara.
        /// </summary>
        public float DistanciaAlCentroDp()
        {
            var posicion = ubicacion?.Posicion;
            if (!posicion.HasValue) return float.PositiveInfinity;
            var usuario = mapa.Proyectar(posicion.Value);
            var centro = mapa.Proyectar(mapa.Centro);
            if (!usuario.Delante || !centro.Delante) return float.PositiveInfinity;
            float escala = EscalaDp > 0f ? EscalaDp : ultimaEscala > 0f ? ultimaEscala : 1f;
            return Vector2.Distance(new Vector2(usuario.X, usuario.Y), new Vector2(centro.X, centro.Y)) * escala;
        }

        /// <summary>Clave de la etiqueta de TalkBack del botón de ubicación según lo que hará al tocarlo.</summary>
        public string ClaveBotonUbicacion() => ClaveBotonUbicacion(AccionUbicacionActual());

        /// <summary>"Orientar al norte", "Buscando tu posición", "Ubicación desactivada" o "Centrar en mi posición".</summary>
        public static string ClaveBotonUbicacion(AccionUbicacion accion)
        {
            switch (accion)
            {
                case AccionUbicacion.OrientarNorte: return ClavesTexto.UbicacionNorte;
                case AccionUbicacion.Nada: return ClavesTexto.UbicacionBuscando;
                case AccionUbicacion.Avisar: return ClavesTexto.UbicacionDesactivada;
                default: return ClavesTexto.UbicacionCentrar; // centrar, y sin permiso (la explicación)
            }
        }

        /// <summary>Muestra el aviso de ubicación desactivada con el texto del estado (denegado o apagada).</summary>
        void AbrirAviso()
        {
            if (aviso == null || aviso.Abierta) return;
            if (MenuAbierto) Cerrar(enfocar: false, porNavegacion: false);
            CerrarFicha(); // la ventana no queda encima de la ficha ni la ficha debajo al cerrarla (recorrido modal)
            aviso.Abrir(ubicacion.Estado);
            accesibilidad.Modal = true; // reconstruye: solo el texto, "Cerrar" y "Abrir Ajustes"
            accesibilidad.PedirFoco(aviso.Texto);
        }

        /// <summary>"Abrir Ajustes": cierra el aviso y abre los ajustes del sistema (por el puerto, nunca con JNI desde la UI).</summary>
        public void AbrirAjustesUbicacion()
        {
            CerrarAviso();
            ubicacion?.AbrirAjustes();
        }

        /// <summary>"Cerrar" (o el botón atrás): cierra el aviso y el foco vuelve al botón de ubicación.</summary>
        public void CerrarAviso()
        {
            if (!AvisoAbierto) return;
            aviso.Mostrar(false);
            accesibilidad.Modal = false; // reconstruye: vuelven los botones y los marcadores
            if (botonUbicacion != null) accesibilidad.PedirFoco(botonUbicacion);
        }

        /// <summary>Muestra la explicación (modal) y guarda que ya salió.</summary>
        void AbrirExplicacion()
        {
            if (explicacion == null || explicacion.Abierta) return;
            if (MenuAbierto) Cerrar(enfocar: false, porNavegacion: false);
            CerrarFicha(); // la ventana no queda encima de la ficha ni la ficha debajo al cerrarla (recorrido modal)
            explicacion.Mostrar(true);
            ubicacion.MarcarExplicacionMostrada();
            accesibilidad.Modal = true; // reconstruye: solo el texto y los dos botones
            accesibilidad.PedirFoco(explicacion.Texto);
        }

        /// <summary>"Permitir": cierra la explicación y lanza el permiso de Android.</summary>
        public void PermitirUbicacion()
        {
            CerrarExplicacion();
            ubicacion?.Permitir();
        }

        /// <summary>"Ahora no" (o el botón atrás): cierra la explicación sin pedir el permiso.</summary>
        public void AhoraNoUbicacion() => CerrarExplicacion();

        void CerrarExplicacion()
        {
            if (!ExplicacionAbierta) return;
            explicacion.Mostrar(false);
            accesibilidad.Modal = false; // reconstruye: vuelven los botones y los marcadores
            if (botonUbicacion != null) accesibilidad.PedirFoco(botonUbicacion);
        }

        /// <summary>True con la ficha de un lugar a la vista.</summary>
        public bool FichaAbierta => ficha != null && ficha.Abierta;

        // El mapa avisa de un toque (CamaraMapa: menos de 6 dp de movimiento y fuera de la interfaz)
        void AlTocarMapa(PuntoPantalla punto) => TocarMapa(punto, EscalaDp > 0f ? EscalaDp : ultimaEscala);

        /// <summary>
        /// Toque en el mapa (px de pantalla, origen arriba a la izquierda) con la escala px → dp dada. Con el menú
        /// "Precios" abierto solo lo cierra y con una ventana de ubicación no hace nada; con la ficha abierta la
        /// cierra. Si no, abre la ficha del marcador visible más cercano a menos de 24 dp (los puntos de interés no
        /// abren nada: su ficha es de la épica 2). Devuelve true si ha abierto una ficha.
        /// </summary>
        public bool TocarMapa(PuntoPantalla punto, float escala)
        {
            if (ModalUbicacionAbierta) return false;
            if (MenuAbierto) { CerrarMenu(); return false; }
            if (FichaAbierta) { CerrarFicha(); return false; }
            if (capa == null || escala <= 0f || float.IsNaN(punto.X) || float.IsNaN(punto.Y)) return false;
            var marcador = capa.MasCercano(new Vector2(punto.X * escala, punto.Y * escala));
            if (marcador == null) return false;
            AbrirFicha(marcador.Lugar);
            return true;
        }

        /// <summary>Abre la ficha del lugar con sus precios calculados con el "ahora" del Arranque.</summary>
        public void AbrirFicha(Lugar lugar)
        {
            if (ficha == null || lugar == null || ModalUbicacionAbierta) return;
            if (MenuAbierto) Cerrar(enfocar: false, porNavegacion: false);
            bool yaAbierta = FichaAbierta;
            ficha.Abrir(lugar, ahora);
            CentrarEnFicha(lugar);
            // De una ficha a otra la hoja tiene medida, pero la del lugar anterior: al disponerse, se recentra
            if (yaAbierta) centrarPendiente = lugar;

            // TalkBack: la ficha es modal (solo sus nodos) y el foco va al título; al cerrarla vuelve a este marcador
            marcadorFicha = MarcadorDe(lugar);
            DeclararFichaEnTalkBack();
            if (accesibilidad.Modal) accesibilidad.Reconstruir(); // de una ficha a otra: los nodos de la nueva
            else accesibilidad.Modal = true;
            accesibilidad.PedirFoco(ficha.Titulo);
        }

        MarcadorLugar marcadorFicha; // marcador del lugar de la ficha abierta: al cerrarla, el foco vuelve a él

        MarcadorLugar MarcadorDe(Lugar lugar)
        {
            if (capa != null) foreach (var m in capa.Marcadores) if (m.Lugar == lugar) return m;
            return null;
        }

        /// <summary>
        /// Da de baja los nodos de la ficha anterior y declara los de esta, en el orden de lectura (FichaLugar.TextosTalkBack),
        /// con "Cerrar ficha" al final. Los del contenido desplazan la ficha hasta ellos al recibir el foco de TalkBack.
        /// </summary>
        void DeclararFichaEnTalkBack()
        {
            accesibilidad.QuitarControles(ficha);
            foreach (var (elemento, texto) in ficha.TextosTalkBack())
            {
                var e = elemento;
                accesibilidad.Texto(e, () => texto, () => FichaAbierta, enModal: true, grupo: ficha,
                    alEnfocar: () => ficha.DesplazarHasta(e));
            }
            accesibilidad.Boton(ficha.BotonCerrar, () => Textos.T(ClavesTexto.FichaCerrar), CerrarFicha,
                visible: () => FichaAbierta, enModal: true, grupo: ficha);
        }

        /// <summary>Alto máximo de la hoja (--ficha-alto-max): con él se centra mientras la hoja no tiene medida.</summary>
        public const float AltoMaximoFicha = 0.72f;

        Lugar centrarPendiente; // lugar centrado con el 72 %, a recentrar con la medida real de la hoja

        /// <summary>
        /// Deja el lugar en el centro horizontal y a media altura de la zona del mapa que la ficha no tapa (de arriba
        /// al borde superior de la hoja), al instante y sin tocar zoom, giro ni inclinación (historia 1.8, parte 2).
        /// </summary>
        void CentrarEnFicha(Lugar lugar)
        {
            float? borde = BordeHojaPx();
            mapa.Centrar(lugar.Posicion, PuntoCentradoFicha(Screen.width, Screen.height, borde));
            centrarPendiente = borde.HasValue ? null : lugar;
        }

        void RecentrarFichaPendiente()
        {
            var lugar = centrarPendiente;
            if (lugar == null || !FichaAbierta || ficha.Lugar != lugar) return;
            if (!BordeHojaPx().HasValue) return; // aún sin medida: se espera al siguiente cambio
            CentrarEnFicha(lugar);
        }

        /// <summary>Borde superior de la hoja en px de pantalla (origen arriba); null si aún no tiene medida.</summary>
        float? BordeHojaPx()
        {
            float escala = EscalaDp;
            if (ficha == null || escala <= 0f) return null;
            var hoja = ficha.Hoja.worldBound;
            if (float.IsNaN(hoja.yMin) || float.IsNaN(hoja.height) || hoja.height <= 0f) return null;
            return hoja.yMin / escala;
        }

        /// <summary>
        /// Punto de pantalla (px, origen arriba a la izquierda) donde queda el lugar con la ficha abierta: el centro
        /// horizontal y la mitad de la zona libre sobre la hoja. Sin medida de la hoja, se supone el 72 % del alto.
        /// </summary>
        public static PuntoPantalla PuntoCentradoFicha(float anchoPx, float altoPx, float? bordeHojaPx)
        {
            float borde = bordeHojaPx ?? altoPx * (1f - AltoMaximoFicha);
            return new PuntoPantalla(anchoPx / 2f, borde / 2f, 1f);
        }

        /// <summary>Cierra la ficha (botón atrás, su botón de cerrar o un toque en el mapa fuera de ella).</summary>
        public void CerrarFicha()
        {
            centrarPendiente = null;
            if (!FichaAbierta) return;
            ficha.Mostrar(false);
            // TalkBack: fuera los nodos de la ficha, vuelve el mapa y el foco al marcador que la abrió, si se ve
            var marcador = marcadorFicha;
            marcadorFicha = null;
            accesibilidad.QuitarControles(ficha);
            accesibilidad.Modal = false;
            accesibilidad.PedirFoco(marcador != null && marcador.Visible ? marcador.Cuerpo : null);
        }

        /// <summary>Píldora "Precios", su menú y el velo que lo cierra al tocar fuera (FR-2, UX-DR6).</summary>
        void CrearFiltro()
        {
            pildora = new PildoraPrecios(AlternarMenu);
            menu = new MenuPrecios(Elegir);
            var hueco = raiz.Q("hueco-precios");
            hueco.Add(pildora);
            hueco.Add(menu);
            // Se recuerda si la píldora o una opción se activan por teclado o mando: solo entonces (o con el lector)
            // se mueve también el foco de teclado, para que un toque no deje el anillo de foco a la vista
            hueco.RegisterCallback<NavigationSubmitEvent>(_ => porNavegacion = true, TrickleDown.TrickleDown);
            hueco.RegisterCallback<PointerDownEvent>(_ => porNavegacion = false, TrickleDown.TrickleDown);
            velo = raiz.Q("velo-menu");
            velo.pickingMode = PickingMode.Position;
            // Al soltar: el gesto empezó con el menú abierto, así que el mapa no lo ha recibido (TapaPunto)
            velo.AddManipulator(new Clickable(CerrarMenu));
            // Con la fuente grande: el menú se dispone al abrirlo, entonces se le aplica la escala
            menu.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, AccessibilitySettings.fontScale));

            // TalkBack: la píldora es un botón con la opción actual como valor; con el menú abierto el recorrido
            // es modal (píldora, que lo cierra, y las 5 opciones), igual que el velo a la vista
            // (con la explicación de la ubicación o la ficha abiertas, la píldora sale del recorrido modal)
            accesibilidad.Boton(pildora, () => Textos.T(ClavesTexto.PreciosControl), AlternarMenu, ValorFiltro,
                visible: () => !ModalUbicacionAbierta && !FichaAbierta, enModal: true);
            foreach (var o in menu.Opciones)
            {
                var opcion = o;
                accesibilidad.Boton(opcion, () => opcion.Texto.text, opcion.Pulsar,
                    visible: () => MenuAbierto, seleccionado: () => opcion.Elegida, enModal: true);
            }
        }

        /// <summary>Lo que hace una opción del menú: cierra, elige el filtro, devuelve el foco a la píldora y lo anuncia.</summary>
        void Elegir(FiltroPrecios f)
        {
            bool navegacion = ConsumirNavegacion();
            Cerrar(enfocar: false, navegacion);
            filtro.Elegir(f); // reconstruye la jerarquía (AlCambiarFiltro): el foco se pide después
            // El anuncio va tras mover el foco a la píldora, en el mismo pase: TalkBack no lo corta al leerla
            accesibilidad.PedirFoco(pildora, navegacion, AnuncioFiltro(f));
        }

        // Si la última activación de la píldora o de una opción llegó por teclado o mando; se lee una sola vez
        bool ConsumirNavegacion()
        {
            bool n = porNavegacion;
            porNavegacion = false;
            return n;
        }

        /// <summary>Anuncio de TalkBack al elegir: "Precios en el mapa: Hostelería".</summary>
        public static string AnuncioFiltro(FiltroPrecios f) => Textos.T(ClavesTexto.PreciosAnuncio, Textos.T(TextosFiltro.ClaveOpcion(f)));

        /// <summary>Valor de la píldora para TalkBack: la opción elegida ("Hostelería").</summary>
        public string ValorFiltro() => Textos.T(TextosFiltro.ClaveOpcion(filtro.Actual));

        void AlCambiarFiltro(FiltroPrecios nuevo)
        {
            if (capa != null) capa.Filtro = nuevo;
            if (pildora == null) return;
            pildora.Mostrar(nuevo);
            menu.Marcar(nuevo);
            accesibilidad?.Reconstruir(); // el valor de la píldora ha cambiado
        }

        /// <summary>True con el menú "Precios" abierto.</summary>
        public bool MenuAbierto => menu != null && menu.Abierto;

        /// <summary>Abre el menú si está cerrado y lo cierra si está abierto (lo que hace la píldora).</summary>
        public void AlternarMenu() => AlternarMenu(ConsumirNavegacion());

        /// <param name="porNavegacion">Si se llegó por teclado o mando: también se mueve el foco de teclado.</param>
        internal void AlternarMenu(bool porNavegacion)
        {
            if (menu == null) return;
            if (menu.Abierto) Cerrar(enfocar: true, porNavegacion);
            else
            {
                // Con la ficha abierta (píldora activada por teclado o TalkBack) el menú no puede quedar debajo
                CerrarFicha();
                menu.Marcar(filtro.Actual);
                menu.Mostrar(true);
                velo.EnableInClassList("abierto", true);
                accesibilidad.Modal = true; // reconstruye: solo la píldora y las opciones
                // El foco de TalkBack (y el de teclado, con lector o por navegación) va a la opción elegida
                foreach (var o in menu.Opciones)
                    if (o.Elegida) accesibilidad.PedirFoco(o, porNavegacion);
            }
        }

        /// <summary>Cierra el menú sin cambiar el filtro (al tocar fuera) y devuelve el foco a la píldora.</summary>
        public void CerrarMenu() => Cerrar(enfocar: true, ConsumirNavegacion());

        void Cerrar(bool enfocar, bool porNavegacion)
        {
            if (menu == null || !menu.Abierto) return;
            menu.Mostrar(false);
            velo.EnableInClassList("abierto", false);
            accesibilidad.Modal = false; // reconstruye: vuelven los botones de tema y zoom y los marcadores
            if (enfocar) accesibilidad.PedirFoco(pildora, porNavegacion);
        }

        /// <summary>
        /// Botón atrás de Android (KeyCode.Escape): con la explicación de la ubicación abierta es "Ahora no"; con la
        /// ficha del lugar abierta la cierra; con el menú abierto lo cierra sin cambiar el filtro. Si no, no hace nada (la app no sale:
        /// Input.backButtonLeavesApp sigue en false). Devuelve true si lo ha gestionado.
        /// </summary>
        public bool Atras()
        {
            if (ExplicacionAbierta) { AhoraNoUbicacion(); return true; }
            if (AvisoAbierto) { CerrarAviso(); return true; }
            if (FichaAbierta) { CerrarFicha(); return true; }
            if (!MenuAbierto) return false;
            ConsumirNavegacion();
            Cerrar(enfocar: true, porNavegacion: true); // atrás es una tecla: el foco de teclado vuelve a la píldora
            return true;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Atras();
        }

        static IEnumerable<(Lugar, ResumenLugar)> ConResumen(ServicioLugares lugares, DateTime ahora)
        {
            if (lugares == null) yield break;
            foreach (var l in lugares.Lugares) yield return (l, lugares.Resumen(l.Id, ahora).Valor);
        }

        void OnDestroy()
        {
            if (tema != null) tema.Cambiado -= AlCambiarTema;
            if (filtro != null) filtro.Cambiado -= AlCambiarFiltro;
            if (capaPuntos != null && mapa != null) mapa.PuntosInteresCambiados -= AlCambiarPuntos;
            if (ficha != null && mapa != null) mapa.Toque -= AlTocarMapa;
            AssistiveSupport.screenReaderStatusChanged -= AlCambiarLector;
            AccessibilitySettings.fontScaleChanged -= AlCambiarEscalaFuente;
            if (raiz != null) EscalaFuente.Olvidar(raiz);
            accesibilidad?.Desactivar();
        }

        /// <summary>
        /// True si el punto de pantalla (origen abajo a la izquierda, como Input) cae sobre un control:
        /// así el mapa no se mueve al pulsar un botón. Con el menú "Precios" abierto, todo el mapa
        /// cuenta como tapado: ese toque solo cierra el menú. Con la explicación de la ubicación, también. Con la
        /// ficha del lugar abierta, igual: un toque fuera de la hoja la cierra (su velo) y no mueve el mapa.
        /// </summary>
        public bool TapaPunto(Vector2 pantalla)
        {
            if (MenuAbierto || ModalUbicacionAbierta || FichaAbierta) return true;
            if (raiz?.panel == null) return false;
            var p = RuntimePanelUtils.ScreenToPanel(raiz.panel, new Vector2(pantalla.x, Screen.height - pantalla.y));
            return EsControl(raiz, raiz.panel.Pick(p));
        }

        /// <summary>Solo los controles de esta pantalla cuentan, nunca los contenedores ni la raíz.</summary>
        public static bool EsControl(VisualElement raiz, VisualElement tocado) =>
            tocado != null && tocado != raiz && raiz.Contains(tocado) && tocado.pickingMode == PickingMode.Position;

        /// <summary>Para las pruebas: tema que muestra la interfaz y botón de tema.</summary>
        public bool InterfazOscura => raiz != null && raiz.ClassListContains("tema-oscuro");
        public BotonRedondo BotonTema => botonTema;
        /// <summary>Botón de ubicación, círculo de la posición y explicación del permiso; null sin interfaz o sin ubicación.</summary>
        public BotonRedondo BotonUbicacion => botonUbicacion;
        public MarcadorPosicion MarcadorPosicion => marcadorPosicion;
        public ExplicacionUbicacion Explicacion => explicacion;
        public AvisoUbicacion Aviso => aviso;
        /// <summary>Ficha del lugar; null sin interfaz.</summary>
        public FichaLugar Ficha => ficha;
        /// <summary>Píldora, menú y velo del filtro "Precios"; null sin interfaz o sin filtro.</summary>
        public PildoraPrecios Pildora => pildora;
        public MenuPrecios Menu => menu;
        public VisualElement Velo => velo;
        /// <summary>Capa de marcadores; null sin interfaz.</summary>
        public CapaMarcadores Capa => capa;
        /// <summary>Capa de puntos de interés; null sin interfaz.</summary>
        public CapaPuntosInteres CapaPuntos => capaPuntos;

        // El mapa ha cargado o liberado una tesela: se crean o quitan las vistas de sus puntos
        void AlCambiarPuntos() => capaPuntos?.Sincronizar();

        /// <summary>
        /// Da de alta un punto en TalkBack: texto "Mercadona, supermercado" (sin acción), en el grupo de los puntos,
        /// que va detrás de los lugares. Solo tiene nodo mientras se ve; su marco es un cuadrado de 48 dp centrado
        /// en el círculo (PuntoInteresVista.ZonaTactil), para que se pueda encontrar al explorar tocando.
        /// </summary>
        void RegistrarPunto(PuntoInteresVista v) =>
            accesibilidad?.Marcador(v.Icono, () => TextosPuntos.Anuncio(v), () => v.Visible, () => v.Posicion,
                AccesibilidadPantalla.GrupoPuntos, () => v.ZonaTactil);

        // La vista se ha destruido (tesela liberada): su entrada de TalkBack desaparece con ella
        void QuitarPunto(PuntoInteresVista v) => accesibilidad?.QuitarMarcador(v.Icono);
        public AccesibilidadPantalla Accesibilidad => accesibilidad;

        /// <summary>Píxeles de pantalla → dp del panel; 0 mientras no hay disposición.</summary>
        public float EscalaDp
        {
            get
            {
                float ancho = raiz != null ? raiz.layout.width : float.NaN;
                return Screen.width > 0 && !float.IsNaN(ancho) && ancho > 0f ? ancho / Screen.width : 0f;
            }
        }

        string ValorTema() => Textos.T(tema.Actual == Tema.Oscuro ? ClavesTexto.TemaOscuro : ClavesTexto.TemaClaro);

        void AlCambiarTema(Tema nuevo)
        {
            if (raiz == null) return;
            raiz.EnableInClassList("tema-oscuro", nuevo == Tema.Oscuro);
            // El icono muestra el tema al que se pasa al pulsar
            botonTema.Icono = nuevo == Tema.Oscuro ? Icono.Sol : Icono.Luna;
            RepintarIconos();
            accesibilidad.Reconstruir();
            if (temaYaAnunciado) AccesibilidadPantalla.Anunciar(ValorTema());
            temaYaAnunciado = true;
        }

        void AlCambiarLector(bool activo)
        {
            if (zoom == null) return;
            // Sin TalkBack los gestos bastan; con TalkBack, botones de acercar y alejar (UX-DR17)
            zoom.EnableInClassList("visible", activo);
            accesibilidad.Reconstruir();
        }

        void AlCambiarEscalaFuente(float escala)
        {
            if (raiz != null) EscalaFuente.Aplicar(raiz, escala);
            capaPuntos?.OlvidarMedidas(); // los nombres cambian de tamaño: las cajas de los solapes también
        }

        void RepintarIconos()
        {
            // El color nuevo del tema se resuelve en el siguiente pase de estilos
            raiz.schedule.Execute(() =>
            {
                botonTema.MarkDirtyRepaint(); botonUbicacion?.MarkDirtyRepaint(); botonAcercar.MarkDirtyRepaint(); botonAlejar.MarkDirtyRepaint();
                capa?.Repintar(); pildora?.Repintar(); menu?.Repintar(); marcadorPosicion?.Repintar();
                ficha?.BotonCerrar.MarkDirtyRepaint();
            });
        }

        /// <summary>Respeta las zonas seguras (barra de estado y de gestos) y aplica la escala de fuente.</summary>
        void AjustarDisposicion()
        {
            EscalaFuente.Aplicar(raiz, AccessibilitySettings.fontScale);

            var segura = Screen.safeArea;
            float ancho = raiz.layout.width;
            if (Screen.width <= 0 || float.IsNaN(ancho) || ancho <= 0f) return;
            if (segura == zonaSeguraAplicada && Mathf.Approximately(ancho, anchoAplicado)) return;
            zonaSeguraAplicada = segura;
            anchoAplicado = ancho;
            float escala = ancho / Screen.width; // píxeles de pantalla → dp del panel
            // La píldora va en su propia zona segura, encima del velo del menú: mismos márgenes
            foreach (var zona in new[] { zonaSegura, zonaSeguraPrecios })
            {
                if (zona == null) continue;
                zona.style.left = segura.xMin * escala;
                zona.style.right = (Screen.width - segura.xMax) * escala;
                zona.style.top = (Screen.height - segura.yMax) * escala;
                zona.style.bottom = segura.yMin * escala;
            }
            // La hoja de la ficha llega al borde inferior; su contenido termina encima de la barra de gestos
            ficha?.PonerMargenInferior(segura.yMin * escala);
        }

        readonly List<Lugar> lugaresVisibles = new List<Lugar>();

        /// <summary>Lugares cuyo marcador se ve ahora (tras CapaMarcadores.Actualizar); se reutiliza la lista.</summary>
        List<Lugar> LugaresVisibles()
        {
            lugaresVisibles.Clear();
            if (capa != null) foreach (var m in capa.Marcadores) if (m.Visible) lugaresVisibles.Add(m.Lugar);
            return lugaresVisibles;
        }

        // La cámara se mueve en Update (CamaraMapa, orden -100): aquí ya está quieta en este fotograma
        void LateUpdate() => ActualizarCapas(EscalaDp);

        /// <summary>
        /// Lo que se hace cada fotograma: recoloca puntos de interés y marcadores con la escala dada
        /// (píxeles de pantalla → dp; 0 mientras no hay disposición) y mantiene TalkBack al día.
        /// </summary>
        internal void ActualizarCapas(float escala)
        {
            // Primero los marcadores: los puntos que repiten un lugar visible ahora se ocultan (FR-4)
            if (capa != null && escala > 0f) capa.Actualizar(escala);
            if (capaPuntos != null && escala > 0f) capaPuntos.Actualizar(escala, LugaresVisibles());
            // La posición del usuario: círculo en su coordenada y cono hacia el rumbo (rumbo − giro del mapa)
            if (marcadorPosicion != null && escala > 0f) marcadorPosicion.Actualizar(mapa, ubicacion.Posicion, ubicacion.Rumbo, escala);
            if (escala > 0f) ultimaEscala = escala;
            // La etiqueta del botón de ubicación sigue a lo que hará al tocarlo: si cambia, se actualiza en su nodo,
            // sin rehacer la jerarquía (TalkBack no pierde el foco aunque la distancia oscile alrededor de 8 dp)
            if (botonUbicacion != null)
            {
                var clave = ClaveBotonUbicacion();
                if (clave != claveBotonUbicacion)
                {
                    claveBotonUbicacion = clave;
                    accesibilidad?.RefrescarEtiqueta(botonUbicacion);
                }
            }
            // El aviso abierto sigue al estado: si ya no toca avisar se cierra; si cambia el motivo, cambia el texto
            if (AvisoAbierto)
            {
                if (AccionUbicacionActual() != AccionUbicacion.Avisar) CerrarAviso();
                else if (aviso.Motivo != ubicacion.Estado)
                {
                    aviso.Abrir(ubicacion.Estado);
                    accesibilidad?.RefrescarEtiqueta(aviso.Texto);
                }
            }
            // Con TalkBack: rehace los nodos de marcadores y puntos si cambian los visibles o su orden
            // (después de las dos capas, para que los puntos ocultos en este fotograma no tengan nodo)
            accesibilidad?.ActualizarMarcadores();
            // Botones y marcadores se mueven (zona segura, escala de fuente, mapa): TalkBack necesita sus marcos al día
            if (accesibilidad != null && AccesibilidadPantalla.LectorActivo) AssistiveSupport.activeHierarchy?.RefreshNodeFrames();
        }
    }
}
