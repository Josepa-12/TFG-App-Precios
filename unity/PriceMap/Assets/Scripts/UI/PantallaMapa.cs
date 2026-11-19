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
        ServicioBusqueda busqueda;
        ServicioCerca servicioCerca;
        BuscadorDirecciones direcciones;
        VisualElement raiz, zonaSegura, zonaSeguraPrecios, zoom, velo, columnaControles;
        PildoraPrecios pildora;
        MenuPrecios menu;
        BotonRedondo botonTema, botonUbicacion, botonAcercar, botonAlejar;
        MarcadorPosicion marcadorPosicion;
        ExplicacionUbicacion explicacion;
        AvisoUbicacion aviso;
        FichaLugar ficha;
        HojaBuscador buscador;
        VisualElement huecoBuscador;
        IAccesibilidadSistema accesibilidadSistema; // "quitar animaciones" del sistema; null = animaciones normales
        bool modalBuscador;       // la hoja del buscador desplegada ha puesto el modo modal de TalkBack
        bool expandirEnRecorrido; // "Expandir" estaba en el recorrido al rehacerlo por última vez
        DateTime ahora;             // "ahora" del Arranque (AD-12): con él se calculan los precios de la ficha
        float ultimaEscala;          // última escala px → dp con disposición (para medir si el usuario está centrado)
        string claveBotonUbicacion;  // etiqueta de TalkBack del botón de ubicación en el último fotograma
        bool avisoNotificacionesVisto; // ya salió el aviso de ruta sin notificaciones (vuelve a salir en otra ruta)
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
        /// <param name="accesibilidadSistema">Ajustes de accesibilidad del sistema; sin él, animaciones normales.</param>
        /// <param name="busqueda">Buscador de lugares y puntos de interés (historia 2.3); sin él, el campo no encuentra nada.</param>
        /// <param name="direcciones">Direcciones (historia 2.3, parte 2); sin él, el buscador no busca direcciones.</param>
        /// <param name="cerca">"Encuentra cerca" (historia 2.4); sin él, las listas de las categorías salen vacías.</param>
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora, ServicioFiltro filtro,
            ServicioUbicacion ubicacion = null, IAccesibilidadSistema accesibilidadSistema = null, ServicioBusqueda busqueda = null,
            BuscadorDirecciones direcciones = null, ServicioCerca cerca = null)
        {
            var documento = GetComponent<UIDocument>();
            var raizDocumento = documento != null ? documento.rootVisualElement : null;
            if (raizDocumento != null) raizDocumento.pickingMode = PickingMode.Ignore; // la raíz no tapa el mapa
            Inicializar(mapa, tema, lugares, ahora, raizDocumento, filtro, ubicacion, accesibilidadSistema, busqueda, direcciones, cerca);
        }

        /// <summary>Igual, con el árbol de la interfaz dado (las pruebas lo construyen sin panel).</summary>
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora, VisualElement raizDocumento,
            ServicioFiltro filtro = null, ServicioUbicacion ubicacion = null, IAccesibilidadSistema accesibilidadSistema = null,
            ServicioBusqueda busqueda = null, BuscadorDirecciones direcciones = null, ServicioCerca cerca = null)
        {
            this.mapa = mapa;
            this.tema = tema;
            this.filtro = filtro;
            this.ubicacion = ubicacion;
            this.busqueda = busqueda;
            this.servicioCerca = cerca;
            this.direcciones = direcciones;
            this.accesibilidadSistema = accesibilidadSistema;
            this.ahora = ahora;
            this.mapa.Centrar(VistaInicial);

            raiz = raizDocumento?.Q("raiz");
            if (raiz == null) return; // sin interfaz (p. ej. en pruebas): el mapa funciona igual

            zonaSegura = raiz.Q("zona-segura");
            zonaSeguraPrecios = raiz.Q("zona-segura-precios");
            zoom = raiz.Q("zoom");
            columnaControles = raiz.Q("columna-controles");
            atribucion = raiz.Q<Label>("atribucion");
            atribucion.text = Textos.T(ClavesTexto.MapaAtribucion);

            // Columna de cristal arriba a la derecha (prototipo .ctrl-stack): tema y, debajo, ubicación, separados por
            // una línea; el cristal, el borde y la sombra los pone la columna, no cada botón
            botonTema = new BotonRedondo(Icono.Luna, () => this.tema.Alternar(), enColumna: true) { name = "boton-tema" };
            raiz.Q("hueco-tema").Add(botonTema);
            var huecoUbicacion = raiz.Q("hueco-ubicacion");
            if (this.ubicacion != null && huecoUbicacion != null)
            {
                botonUbicacion = new BotonRedondo(Icono.Navegar, TocarUbicacion, enColumna: true) { name = "boton-ubicacion" };
                huecoUbicacion.Add(botonUbicacion);
            }
            Columna(botonTema, botonUbicacion);
            botonAcercar = new BotonRedondo(Icono.Mas, () => this.mapa.Acercar(), enColumna: true) { name = "boton-acercar" };
            botonAlejar = new BotonRedondo(Icono.Menos, () => this.mapa.Alejar(), enColumna: true) { name = "boton-alejar" };
            zoom.Add(botonAcercar);
            zoom.Add(botonAlejar);
            Columna(botonAcercar, botonAlejar);
            foreach (var columna in raiz.Query(className: "columna-controles").ToList()) SombraSuave.Poner(columna, conBorde: true);

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
            ficha.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, EscalaFuente.Sistema));
            // Abierta sin medida (primer fotograma), el lugar se centró con el 72 %: al disponerse, con su borde real
            ficha.Hoja.RegisterCallback<GeometryChangedEvent>(_ => RecentrarFichaPendiente());
            this.mapa.Toque += AlTocarMapa;

            // Buscador (historia 2.2): cápsula abajo que se despliega en hoja, encima de los controles del mapa
            CrearBuscador();

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

        /// <summary>
        /// Esquinas y separador de los huecos de una columna de controles: el de arriba lleva las esquinas superiores
        /// de la columna, el de abajo (si lo hay) las inferiores y una línea encima; así el fondo al pulsar y el
        /// borde de foco siguen la forma de la columna.
        /// </summary>
        static void Columna(BotonRedondo arriba, BotonRedondo abajo)
        {
            arriba.AddToClassList("boton-redondo--arriba");
            (abajo ?? arriba).AddToClassList("boton-redondo--abajo");
            abajo?.AddToClassList(BotonRedondo.ClaseSeparado);
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
            explicacion.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, EscalaFuente.Sistema));
            // TalkBack: con la ventana abierta el recorrido es modal (texto, "Ahora no" y "Permitir")
            accesibilidad.Texto(explicacion.Texto, () => explicacion.Texto.text, () => ExplicacionAbierta, enModal: true);
            accesibilidad.Boton(explicacion.AhoraNo, () => explicacion.AhoraNo.text, AhoraNoUbicacion, visible: () => ExplicacionAbierta, enModal: true);
            accesibilidad.Boton(explicacion.Permitir, () => explicacion.Permitir.text, PermitirUbicacion, visible: () => ExplicacionAbierta, enModal: true);

            // Aviso de ubicación desactivada (denegado o apagada): misma ventana, mismo hueco y mismo recorrido modal
            aviso = new AvisoUbicacion(AbrirAjustesUbicacion, CerrarAviso);
            (raiz.Q("hueco-explicacion") ?? raiz).Add(aviso);
            aviso.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, EscalaFuente.Sistema));
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
            PlegarBuscadorPorOtraVentana();
            aviso.Abrir(ubicacion.Estado);
            ModalNuevo(); // reconstruye: solo el texto, "Cerrar" y "Abrir Ajustes"
            accesibilidad.PedirFoco(aviso.Texto);
        }

        /// <summary>
        /// Aviso de ruta sin GPS en segundo plano (historia 2.1): se denegó el permiso de notificaciones y la
        /// navegación se pausa al apagar la pantalla. Misma ventana que el aviso de ubicación; si ya estaba abierto,
        /// cambia el texto.
        /// </summary>
        void AbrirAvisoNotificaciones()
        {
            if (aviso == null) return;
            bool yaAbierto = aviso.Abierta;
            if (MenuAbierto) Cerrar(enfocar: false, porNavegacion: false);
            CerrarFicha();
            PlegarBuscadorPorOtraVentana();
            aviso.AbrirSinNotificaciones();
            if (yaAbierto) accesibilidad.RefrescarEtiqueta(aviso.Texto);
            else ModalNuevo(); // reconstruye: solo el texto, "Cerrar" y "Abrir Ajustes"
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
            PlegarBuscadorPorOtraVentana();
            explicacion.Mostrar(true);
            ubicacion.MarcarExplicacionMostrada();
            ModalNuevo(); // reconstruye: solo el texto y los dos botones
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

        /// <summary>True con la hoja del buscador desplegada (más que la cápsula) o arrastrándose.</summary>
        public bool BuscadorAbierto => buscador != null && buscador.Abierta;

        /// <summary>
        /// Cápsula y hoja del buscador. Desplegarla cierra la ficha y el menú "Precios"; su hueco da el alto que recorre
        /// la hoja. La atribución queda por encima de la cápsula plegada (que crece con la fuente: se sigue su línea).
        /// La escala de fuente le llega con la del resto de la pantalla (AjustarDisposicion): sus textos existen desde
        /// el principio, así que no hace falta aplicarla en cada cambio de su geometría (cada fotograma al arrastrar).
        /// </summary>
        void CrearBuscador()
        {
            huecoBuscador = raiz.Q("hueco-buscador");
            if (huecoBuscador == null) return;
            buscador = new HojaBuscador();
            huecoBuscador.Add(buscador);
            SombraSuave.Poner(buscador);
            buscador.Desplegando += AlDesplegarBuscador;
            buscador.TextoCambiado += Buscar;
            buscador.ResultadoElegido += ElegirResultado;
            buscador.BuscarComoDireccionPedido += BuscarComoDireccion;
            buscador.CategoriaElegida += ElegirCategoria;
            buscador.CategoriaQuitada += QuitarCategoria;
            if (direcciones != null) direcciones.Cambiado += AlCambiarDirecciones;
            huecoBuscador.RegisterCallback<GeometryChangedEvent>(_ => MedirBuscador());
            buscador.Linea.RegisterCallback<GeometryChangedEvent>(_ => ColocarAtribucion());
            LeerAccesibilidadSistema();
            DeclararBuscadorEnTalkBack();
        }

        /// <summary>
        /// TalkBack (historia 2.2, parte 2). Plegada, la cápsula es un botón "Buscar en Sanlúcar" detrás de los
        /// controles del mapa (el avatar no está: aún no hace nada). Desplegada a cualquier altura, la hoja es modal:
        /// el campo, "Expandir" (si no está abierta del todo) y "Contraer" (botones que solo ve el lector), los 8 chips
        /// y "Encuentra cerca" (botones, historia 2.4). Ni marcadores ni controles del mapa.
        /// </summary>
        void DeclararBuscadorEnTalkBack()
        {
            accesibilidad.Boton(buscador, () => Textos.T(ClavesTexto.BuscadorCampo), AbrirBuscadorConLector,
                visible: () => buscador.Visible && !BuscadorAbierto);
            Func<bool> enHoja = () => BuscadorAbierto && !ModalUbicacionAbierta;
            accesibilidad.Boton(buscador.Campo, () => Textos.T(ClavesTexto.BuscadorCampo), () => buscador.Campo.Focus(),
                visible: enHoja, enModal: true);
            accesibilidad.Boton(buscador.Borrar, () => Textos.T(ClavesTexto.BuscadorBorrar), BorrarBusqueda,
                visible: () => enHoja() && buscador.HayTexto, enModal: true);
            accesibilidad.Boton(buscador.Expandir, () => Textos.T(ClavesTexto.BuscadorExpandir), ExpandirBuscador,
                visible: () => enHoja() && ExpandirEnRecorrido, enModal: true);
            accesibilidad.Boton(buscador.Contraer, () => Textos.T(ClavesTexto.BuscadorContraer), PlegarBuscador,
                visible: () => enHoja() && AccesibilidadPantalla.LectorActivo, enModal: true);
            // Chips y "Encuentra cerca" (historia 2.4, parte 3): botones que hacen lo mismo que el toque
            for (int i = 0; i < buscador.Chips.Count; i++)
            {
                var chip = buscador.Chips[i];
                CategoriaPoi? categoria = HojaBuscador.Categorias[i];
                accesibilidad.Boton(chip, () => chip.Q<Label>().text, () => buscador.ElegirCategoria(categoria),
                    visible: () => enHoja() && buscador.MostrandoCategorias, enModal: true);
            }
            accesibilidad.Boton(buscador.Cerca, () => buscador.Cerca.Q<Label>().text, () => buscador.ElegirCategoria(null),
                visible: () => enHoja() && buscador.MostrandoCategorias, enModal: true);
            // Con la lista "cerca", su texto vacío va detrás de la cabecera (se declara en PintarCerca)
            accesibilidad.Texto(buscador.SinResultados, () => buscador.SinResultados.text,
                () => enHoja() && buscador.SinResultadosVisible && !buscador.MostrandoCerca, enModal: true);
            accesibilidad.Boton(buscador.BuscarDireccion, () => Textos.T(ClavesTexto.BuscadorBuscarDireccion), BuscarComoDireccion,
                visible: () => enHoja() && buscador.BuscarDireccionVisible, enModal: true);
            // Las filas de los resultados se declaran al pintarlas (DeclararResultadosEnTalkBack), detrás de todo esto
        }

        // ───────────────────────── Búsqueda (historia 2.3) ─────────────────────────

        IReadOnlyList<ResultadoBusqueda> ultimosResultados; // lo último que se anunció (null sin búsqueda)
        IReadOnlyList<ResultadoBusqueda> localesActuales;   // lugares y puntos del texto actual (null sin búsqueda)
        // Direcciones (historia 2.3, parte 2): texto del campo para el que se pidieron, su respuesta (null sin
        // respuesta buena: en la espera, con poco texto o tras un fallo) y si la última respuesta fue un fallo
        string consultaDirecciones;
        IReadOnlyList<ResultadoBusqueda> direccionesActuales;
        bool direccionesFallaron;

        /// <summary>
        /// Punto de cercanía de la búsqueda: la posición del usuario o, si no la hay, el centro del mapa.
        /// </summary>
        public Coordenada PuntoCercania() => ubicacion?.Posicion ?? mapa.Centro;

        /// <summary>
        /// Busca el texto del campo (a cada tecla: lo local, al momento) y pinta los resultados en la hoja; las
        /// direcciones se piden aparte (BuscadorDirecciones: tras la espera) y se añaden debajo al llegar. TalkBack:
        /// cada fila es un botón y, si la lista cambia, se anuncia "N resultados" o "No hay resultados".
        /// </summary>
        public void Buscar(string texto)
        {
            if (buscador == null) return;
            if (cercaActiva)
            {
                // Escribir quita la categoría (historia 2.4): vuelve la búsqueda de la 2.3
                if (string.IsNullOrEmpty(texto)) { PintarCerca(); return; }
                cercaActiva = false;
                categoriaActiva = null;
                AplicarCategoriaAlMapa(); // vuelven el filtro "Precios" y su píldora
            }
            IReadOnlyList<ResultadoBusqueda> resultados = null;
            if (busqueda != null && buscador.EnBusqueda) resultados = busqueda.Buscar(texto, PuntoCercania());
            localesActuales = resultados;
            focoAlCampoPendiente = false; // al teclear, el foco ya está en el campo
            PedirDirecciones(texto);
            PintarResultados(arriba: true);
        }

        /// <summary>
        /// Al cambiar el texto: cancela las direcciones anteriores y pide las del texto nuevo (tras la espera y con el
        /// mínimo de caracteres). Si el texto es el mismo (al volver a desplegar la hoja) y ya hay respuesta o está en
        /// camino, se deja como está; tras un fallo se vuelve a pedir.
        /// </summary>
        void PedirDirecciones(string texto)
        {
            if (direcciones == null) return;
            if (!buscador.EnBusqueda)
            {
                direcciones.Cancelar();
                consultaDirecciones = null;
                direccionesActuales = null;
                direccionesFallaron = false;
                return;
            }
            if (texto == consultaDirecciones && !direccionesFallaron && (direcciones.Pendiente || direccionesActuales != null))
            {
                // Mismo texto (al volver a desplegar): no se pide otra vez, pero las distancias se miden desde el punto de ahora
                if (direccionesActuales != null) direccionesActuales = ConDistancia(direccionesActuales, PuntoCercania());
                return;
            }
            consultaDirecciones = texto;
            direccionesActuales = null;
            // direccionesFallaron se queda: sin conexión, el aviso no parpadea (ni se vuelve a anunciar) a cada tecla;
            // lo quita la próxima respuesta buena
            direcciones.Pedir(texto, PuntoCercania());
        }

        /// <summary>
        /// "Buscar como dirección" (junto a "No hay resultados"): pide las direcciones del texto ya, sin espera ni
        /// mínimo. Tras un fallo sirve de reintento.
        /// </summary>
        public void BuscarComoDireccion()
        {
            if (buscador == null || direcciones == null || !buscador.EnBusqueda || ModalUbicacionAbierta) return;
            string texto = buscador.Campo.value;
            consultaDirecciones = texto;
            direccionesActuales = null;
            direccionesFallaron = false;
            avisoAnunciado = false;      // un reintento que vuelve a fallar se anuncia otra vez
            focoAlCampoPendiente = true; // TalkBack: si el botón desaparece al llegar la respuesta, el foco va al campo
            direcciones.PedirYa(texto, PuntoCercania()); // con un geocodificador inmediato, la respuesta ya ha llegado
            PintarResultados(arriba: false, moverFoco: false);
        }

        // Respuesta del geocodificador: solo vale si es del texto que hay ahora en el campo
        void AlCambiarDirecciones(string consulta, Resultado<IReadOnlyList<ResultadoBusqueda>> r)
        {
            if (buscador == null || consulta != consultaDirecciones || consulta != buscador.Campo.value) return;
            try
            {
                direccionesFallaron = !r.Ok;
                direccionesActuales = r.Ok ? r.Valor ?? Array.Empty<ResultadoBusqueda>() : null;
                if (!r.Ok) Debug.Log("PriceMap: direcciones no disponibles (" + r.Error + ").");
                PintarResultados(arriba: false, moverFoco: false); // llegan solas: el foco de TalkBack no se mueve
            }
            catch (Exception e)
            {
                Debug.LogException(e); // llega desde una tarea que nadie espera: sin esto el error se perdería
            }
        }

        static IReadOnlyList<ResultadoBusqueda> ConDistancia(IReadOnlyList<ResultadoBusqueda> lista, Coordenada cerca)
        {
            var r = new List<ResultadoBusqueda>(lista.Count);
            foreach (var x in lista) r.Add(ResultadoBusqueda.DeDireccion(x.Direccion, cerca.DistanciaMetros(x.Posicion)));
            return r.AsReadOnly();
        }

        bool avisoAnunciado;        // "Las direcciones necesitan conexión" ya anunciado mientras sigue visible
        bool focoAlCampoPendiente;  // "Buscar como dirección" pulsado: si el botón desaparece, el foco vuelve al campo

        /// <summary>
        /// Pinta locales + direcciones, el aviso de conexión y "Buscar como dirección" (si aún no hay direcciones
        /// buenas). TalkBack (parte 3): "N resultados" cuenta locales y direcciones y se anuncia si cambia la lista; el
        /// aviso de conexión se anuncia una vez, al aparecer.
        /// </summary>
        /// <param name="moverFoco">Falso cuando llegan las direcciones: no es una acción del usuario y el foco de
        /// TalkBack no se mueve (solo se anuncia).</param>
        void PintarResultados(bool arriba, bool moverFoco = true)
        {
            bool ofrecer = direcciones != null && direccionesActuales == null;
            buscador.MostrarResultados(localesActuales, direccionesActuales, direccionesFallaron, ofrecer, arriba);

            string anuncio = null;
            if (!buscador.MostrandoResultados) ultimosResultados = null;
            else if (BuscadorAbierto && modalBuscador) // solo cuenta como anunciado si de verdad se puede anunciar
            {
                var actuales = Juntar(localesActuales, direccionesActuales);
                if (ultimosResultados == null || !MismosResultados(ultimosResultados, actuales)) anuncio = AnuncioResultados(actuales.Count);
                ultimosResultados = actuales;
                if (buscador.AvisoDireccionesVisible && !avisoAnunciado)
                {
                    string aviso = Textos.T(ClavesTexto.BuscadorDireccionesSinConexion);
                    anuncio = anuncio == null ? aviso : anuncio + ". " + aviso;
                    avisoAnunciado = true;
                }
            }
            if (!buscador.AvisoDireccionesVisible) avisoAnunciado = false;
            if (focoAlCampoPendiente && !buscador.BuscarDireccionVisible)
            {
                focoAlCampoPendiente = false;
                moverFoco = true; // el botón con el foco ya no está: el foco va al campo
            }
            DeclararResultadosEnTalkBack(anuncio, moverFoco);
        }

        static IReadOnlyList<ResultadoBusqueda> Juntar(IReadOnlyList<ResultadoBusqueda> locales, IReadOnlyList<ResultadoBusqueda> direcciones)
        {
            if (direcciones == null || direcciones.Count == 0) return locales ?? Array.Empty<ResultadoBusqueda>();
            var r = new List<ResultadoBusqueda>();
            if (locales != null) r.AddRange(locales);
            r.AddRange(direcciones);
            return r;
        }

        /// <summary>"No hay resultados", "1 resultado" o "N resultados".</summary>
        public static string AnuncioResultados(int n) =>
            n == 0 ? Textos.T(ClavesTexto.BuscadorSinResultados)
            : n == 1 ? Textos.T(ClavesTexto.BuscadorUnResultado)
            : Textos.T(ClavesTexto.BuscadorResultados, n.ToString(System.Globalization.CultureInfo.InvariantCulture));

        static bool MismosResultados(IReadOnlyList<ResultadoBusqueda> a, IReadOnlyList<ResultadoBusqueda> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i].Lugar != b[i].Lugar || a[i].Punto != b[i].Punto || a[i].Direccion != b[i].Direccion) return false;
            return true;
        }

        /// <summary>
        /// TalkBack: da de baja las filas anteriores y declara las nuevas como botones ("Clínica Areas, Clínica privada,
        /// a 350 m, desde 20 €"). Con la hoja desplegada (modal) rehace el recorrido, deja el foco en el campo y anuncia.
        /// </summary>
        void DeclararResultadosEnTalkBack(string anuncio, bool moverFoco = true)
        {
            if (accesibilidad == null) return;
            accesibilidad.QuitarControles(buscador.Resultados);
            Func<bool> enHoja = () => BuscadorAbierto && !ModalUbicacionAbierta;
            foreach (var f in buscador.Filas)
            {
                var fila = f;
                accesibilidad.Boton(fila, () => fila.Anuncio, () => ElegirResultado(fila.Resultado),
                    visible: enHoja, enModal: true, grupo: buscador.Resultados);
            }
            // El aviso de conexión va debajo de la lista, como en pantalla
            accesibilidad.Texto(buscador.AvisoDirecciones, () => buscador.AvisoDirecciones.text,
                () => enHoja() && buscador.AvisoDireccionesVisible, enModal: true, grupo: buscador.Resultados);
            if (!BuscadorAbierto || !modalBuscador) return; // plegada: entran en el recorrido al desplegarla
            accesibilidad.Reconstruir();
            if (!moverFoco)
            {
                // La jerarquía es nueva: el foco vuelve a donde estaba el usuario. Si ese nodo ya no está (p. ej. "No hay
                // resultados" al llegar las direcciones), va al campo para no quedarse en nada
                if (accesibilidad.RecuperarFoco()) { if (anuncio != null) AccesibilidadPantalla.Anunciar(anuncio); }
                else if (anuncio != null) accesibilidad.PedirFoco(buscador.Campo, anuncio: anuncio);
                else if (AccesibilidadPantalla.LectorActivo) accesibilidad.PedirFoco(buscador.Campo);
            }
            else if (anuncio != null) accesibilidad.PedirFoco(buscador.Campo, anuncio: anuncio);
            else if (AccesibilidadPantalla.LectorActivo) accesibilidad.PedirFoco(buscador.Campo);
        }

        /// <summary>
        /// Toque en un resultado: un lugar abre su ficha (que centra el mapa y pliega la hoja); un punto de interés
        /// centra el mapa en él y pliega la hoja (su ficha llega en la 2.5); una dirección, igual que un punto.
        /// </summary>
        public void ElegirResultado(ResultadoBusqueda resultado)
        {
            if (resultado == null || buscador == null || ModalUbicacionAbierta) return;
            if (resultado.Tipo == TipoResultado.Lugar) { AbrirFicha(resultado.Lugar); return; }
            mapa.Centrar(resultado.Posicion);
            PlegarBuscador();
        }

        // ───────────────────────── Encuentra cerca (historia 2.4, parte 1) ─────────────────────────

        bool cercaActiva;               // se ve (o se verá al desplegar) la lista "cerca" de una categoría
        CategoriaPoi? categoriaActiva;  // su categoría; null con "Encuentra cerca" (todas)

        /// <summary>Hay una categoría (o "Encuentra cerca") elegida: la hoja enseña su lista.</summary>
        public bool CercaActiva => cercaActiva;
        /// <summary>Categoría elegida; null con "Encuentra cerca" o sin categoría.</summary>
        public CategoriaPoi? CategoriaActiva => categoriaActiva;

        /// <summary>
        /// Toque en un chip (su categoría) o en "Encuentra cerca" (null): la hoja enseña la lista de lo cercano de esa
        /// categoría (ServicioCerca), con su cabecera.
        /// </summary>
        public void ElegirCategoria(CategoriaPoi? categoria)
        {
            if (buscador == null || ModalUbicacionAbierta) return;
            direcciones?.Cancelar(); // con la categoría no se buscan direcciones
            cercaActiva = true;
            categoriaActiva = categoria;
            AplicarCategoriaAlMapa();
            PintarCerca();
        }

        /// <summary>
        /// La X de la cabecera, el botón atrás o la píldora con la categoría: quita la categoría y vuelven las categorías
        /// (o la búsqueda), y en el mapa, el filtro "Precios" guardado. TalkBack con la hoja desplegada: el foco va al
        /// chip de la categoría quitada ("Encuentra cerca" si era esa); con la píldora, PulsarPildora lo lleva a ella.
        /// </summary>
        public void QuitarCategoria()
        {
            if (buscador == null || !cercaActiva) return;
            var quitada = categoriaActiva;
            cercaActiva = false;
            categoriaActiva = null;
            AplicarCategoriaAlMapa();
            Buscar(buscador.Campo.value);
            if (accesibilidad == null || !AccesibilidadPantalla.LectorActivo || !BuscadorAbierto || !modalBuscador) return;
            int i = quitada is CategoriaPoi c ? Array.IndexOf(HojaBuscador.Categorias, c) : -1;
            accesibilidad.PedirFoco(i >= 0 && i < buscador.Chips.Count ? buscador.Chips[i] : buscador.Cerca);
        }

        CategoriaPoi? categoriaMapa; // la que filtra el mapa ahora (null: el filtro "Precios")

        /// <summary>Categoría que filtra el mapa y enseña la píldora; null con el filtro "Precios" (y con "Encuentra cerca").</summary>
        public CategoriaPoi? CategoriaMapa => categoriaMapa;

        /// <summary>
        /// Historia 2.4, parte 2: con un chip activo el mapa enseña solo sus marcadores y puntos y la píldora dice
        /// "Salud ✕"; "Encuentra cerca" (null) no filtra el mapa. Sin categoría vuelve el filtro "Precios" guardado,
        /// que nunca se cambia por una categoría.
        /// </summary>
        void AplicarCategoriaAlMapa()
        {
            var nueva = cercaActiva ? categoriaActiva : null;
            if (nueva == categoriaMapa) return;
            categoriaMapa = nueva;
            if (capa != null) capa.Categoria = nueva;
            if (capaPuntos != null) capaPuntos.Categoria = nueva;
            if (pildora == null) return;
            if (nueva is CategoriaPoi c) pildora.MostrarCategoria(c, NombreCategoria(c));
            else pildora.Mostrar(filtro.Actual);
            accesibilidad?.Reconstruir(); // la etiqueta de la píldora ha cambiado
        }

        static string NombreCategoria(CategoriaPoi c) => Textos.T(HojaBuscador.ClaveCategoria(c));

        /// <summary>Lo que hace la píldora: con una categoría la quita; si no, abre o cierra el menú "Precios".</summary>
        public void PulsarPildora()
        {
            if (categoriaMapa == null) { AlternarMenu(); return; }
            bool navegacion = ConsumirNavegacion();
            CerrarFicha(); // por teclado con la ficha abierta: no se cambia el mapa detrás de ella (como AlternarMenu)
            QuitarCategoria();
            // Como al elegir un filtro: el foco vuelve a la píldora y se anuncia el filtro que vuelve
            accesibilidad?.PedirFoco(pildora, navegacion, AnuncioFiltro(filtro.Actual));
        }

        /// <summary>
        /// Calcula y pinta la lista de la categoría activa con la distancia desde el usuario o, sin posición, desde el
        /// centro del mapa (y entonces lo dice la cabecera). TalkBack (historia 2.4, parte 3): el título (texto), la X
        /// ("Quitar categoría"), el texto vacío y las filas (botones, como los de la búsqueda) entran en el recorrido
        /// con la hoja desplegada. Con <paramref name="enfocar"/>, el foco va al título y se anuncia "Salud cerca,
        /// N resultados" (o el texto vacío); si no, vuelve a donde estaba.
        /// </summary>
        void PintarCerca(bool enfocar = true)
        {
            var lista = servicioCerca?.Listar(categoriaActiva, PuntoCercania()) ?? Array.Empty<ResultadoBusqueda>();
            string titulo, vacio;
            if (categoriaActiva is CategoriaPoi c)
            {
                string nombre = Textos.T(HojaBuscador.ClaveCategoria(c));
                titulo = Textos.T(ClavesTexto.BuscadorCercaCategoria, nombre);
                vacio = Textos.T(ClavesTexto.BuscadorCercaVacio, Textos.T(HojaBuscador.ClaveNombreCerca(c)));
            }
            else
            {
                titulo = Textos.T(ClavesTexto.BuscadorCercaTitulo);
                vacio = Textos.T(ClavesTexto.BuscadorSinResultados);
            }
            // Las filas se rehacen (elementos nuevos): si el foco estaba en una, irá a la nueva del mismo sitio
            var filaEnfocada = (accesibilidad?.UltimoFoco as FilaResultado)?.Resultado;
            buscador.MostrarCerca(titulo, ubicacion?.Posicion == null, lista, vacio);
            localesActuales = null;
            ultimosResultados = null;

            if (accesibilidad == null) return;
            accesibilidad.QuitarControles(buscador.Resultados);
            Func<bool> enHoja = () => BuscadorAbierto && !ModalUbicacionAbierta && buscador.MostrandoCerca;
            accesibilidad.Texto(buscador.TituloCerca, EtiquetaTituloCerca, enHoja, enModal: true, grupo: buscador.Resultados);
            accesibilidad.Boton(buscador.QuitarCategoria, () => Textos.T(ClavesTexto.BuscadorQuitarCategoria),
                buscador.PulsarQuitarCategoria, visible: enHoja, enModal: true, grupo: buscador.Resultados);
            accesibilidad.Texto(buscador.FilaSinResultados, () => buscador.SinResultados.text,
                () => enHoja() && buscador.SinResultadosVisible, enModal: true, grupo: buscador.Resultados);
            foreach (var f in buscador.Filas)
            {
                var fila = f;
                accesibilidad.Boton(fila, () => fila.Anuncio, () => ElegirResultado(fila.Resultado),
                    visible: enHoja, enModal: true, grupo: buscador.Resultados);
            }
            int n = buscador.Filas.Count;
            string anuncio = n == 0 ? vacio : Textos.T(ClavesTexto.BuscadorCercaJuntar, titulo, AnuncioResultados(n));
            if (!BuscadorAbierto || !modalBuscador) return; // plegada: entran en el recorrido al desplegarla
            accesibilidad.Reconstruir(); // los chips salen del recorrido; entran la cabecera y las filas
            if (!AccesibilidadPantalla.LectorActivo) return; // sin lector, nada cambia
            if (enfocar) { accesibilidad.PedirFoco(buscador.TituloCerca, anuncio: anuncio); return; }
            var misma = MismaFila(filaEnfocada);
            if (misma != null) accesibilidad.PedirFoco(misma);
            else if (!accesibilidad.RecuperarFoco()) accesibilidad.PedirFoco(buscador.TituloCerca);
        }

        // La fila de ahora del mismo lugar o punto que ese resultado; null si ya no está
        FilaResultado MismaFila(ResultadoBusqueda r)
        {
            if (r == null) return null;
            foreach (var f in buscador.Filas)
                if (f.Resultado.Lugar == r.Lugar && f.Resultado.Punto == r.Punto && f.Resultado.Direccion == r.Direccion) return f;
            return null;
        }

        /// <summary>Título de la lista "cerca" para TalkBack: "Salud cerca" o, sin posición, "Salud cerca, desde el centro del mapa".</summary>
        string EtiquetaTituloCerca() => buscador.DesdeCentro.style.display == DisplayStyle.None
            ? buscador.TituloCerca.text
            : Textos.T(ClavesTexto.BuscadorCercaJuntar, buscador.TituloCerca.text, buscador.DesdeCentro.text);

        /// <summary>La X del campo ("Borrar búsqueda" en TalkBack): lo vacía, vuelven las categorías y el foco va al campo.</summary>
        public void BorrarBusqueda()
        {
            if (buscador == null) return;
            direcciones?.Cancelar(); // una petición en curso no llega a pintarse
            buscador.BorrarTexto();
            accesibilidad?.PedirFoco(buscador.Campo);
        }

        // "Expandir" solo está en el recorrido con el lector y la hoja desplegada sin abrir del todo
        bool ExpandirEnRecorrido => AccesibilidadPantalla.LectorActivo && BuscadorAbierto && !buscador.AbiertaDelTodo;

        /// <summary>
        /// Lee del sistema si están quitadas las animaciones (UX-DR19): entonces la hoja cambia de altura sin
        /// transición. Al arrancar y cada vez que la app vuelve a primer plano (lo llama el Arranque).
        /// </summary>
        public void LeerAccesibilidadSistema()
        {
            if (buscador == null) return;
            buscador.SinTransicion = accesibilidadSistema != null && accesibilidadSistema.AnimacionesQuitadas;
        }

        /// <summary>La cápsula activada con TalkBack: abre la hoja del todo, modal, y lleva el foco al campo.</summary>
        public void AbrirBuscadorConLector()
        {
            if (buscador == null || !buscador.Visible) return;
            AlDesplegarBuscador(); // cierra la ficha y el menú
            buscador.Abrir();
            SeguirModalBuscador(enfocarCapsula: false); // con la lista "cerca" y el lector, ya enfoca su título
            if (!cercaActiva || !AccesibilidadPantalla.LectorActivo) accesibilidad.PedirFoco(buscador.Campo);
        }

        /// <summary>"Expandir" de TalkBack: abre la hoja del todo; "Expandir" sale del recorrido y el foco pasa a "Contraer".</summary>
        public void ExpandirBuscador()
        {
            if (buscador == null || !BuscadorAbierto) return;
            buscador.Abrir();
            SeguirModalBuscador(enfocarCapsula: false);
            accesibilidad.PedirFoco(buscador.Contraer);
        }

        /// <summary>
        /// El modo modal sigue a la hoja: al desplegarse (por gesto o por TalkBack) se pone y al plegarse (también por
        /// gesto) se quita y el foco vuelve a la cápsula, salvo que otra ventana (ficha, menú, ubicación) se haya
        /// abierto: entonces manda ella. Si "Expandir" entra o sale del recorrido, se rehace.
        /// </summary>
        void SeguirModalBuscador(bool enfocarCapsula)
        {
            if (buscador == null || accesibilidad == null) return;
            bool abierta = BuscadorAbierto;
            // (también si otra ventana quitó el modo modal con la hoja aún desplegada)
            if (abierta && (!modalBuscador || !accesibilidad.Modal))
            {
                modalBuscador = true;
                expandirEnRecorrido = ExpandirEnRecorrido;
                ModalNuevo(); // reconstruye: solo los nodos de la hoja
                // Con la lista "cerca": se rehace, el foco va a su título y se anuncia (historia 2.4, parte 3)
                if (cercaActiva) PintarCerca();
                // Desplegada con el dedo: el foco de TalkBack no puede quedarse en un nodo que ya no está
                else if (AccesibilidadPantalla.LectorActivo) accesibilidad.PedirFoco(buscador.Campo);
            }
            else if (!abierta && modalBuscador)
            {
                modalBuscador = false;
                if (MenuAbierto || FichaAbierta || ModalUbicacionAbierta) return;
                accesibilidad.Modal = false; // reconstruye: vuelven los controles del mapa, la cápsula y los marcadores
                if (enfocarCapsula) accesibilidad.PedirFoco(buscador);
            }
            else if (abierta && ExpandirEnRecorrido != expandirEnRecorrido)
            {
                expandirEnRecorrido = !expandirEnRecorrido;
                accesibilidad.Reconstruir();
            }
        }

        // Pliega la hoja porque se abre otra ventana modal (ficha, menú, ubicación): el modo modal pasa a ser el suyo
        void PlegarBuscadorPorOtraVentana()
        {
            buscador?.Plegar();
            modalBuscador = false;
        }

        // Pone el modo modal de una ventana nueva; si ya lo estaba (otra ventana o la hoja), rehace el recorrido
        void ModalNuevo()
        {
            if (accesibilidad.Modal) accesibilidad.Reconstruir();
            else accesibilidad.Modal = true;
        }

        void MedirBuscador()
        {
            if (buscador == null) return;
            buscador.Medir(huecoBuscador.layout.height);
            ColocarAtribucion();
        }

        // La atribución (abajo a la derecha, en la zona segura) va encima de la cápsula plegada, no debajo
        void ColocarAtribucion()
        {
            if (buscador == null || atribucion == null) return;
            atribucion.style.bottom = buscador.AltoCapsula + HojaBuscador.MargenAbajo + 8f;
        }

        // El usuario empieza a desplegar el buscador: la ficha y el menú nunca están abiertos a la vez que la hoja
        void AlDesplegarBuscador()
        {
            if (MenuAbierto) Cerrar(enfocar: false, porNavegacion: false);
            CerrarFicha();
            bool eraModal = modalBuscador;
            SeguirModalBuscador(enfocarCapsula: false); // TalkBack: la hoja desplegada es modal
            bool repintada = !eraModal && modalBuscador && cercaActiva; // SeguirModalBuscador ya rehízo la lista "cerca"
            // La búsqueda guardada al plegar se rehace: el usuario, el mapa o los puntos cargados pueden haber cambiado
            if (buscador != null && cercaActiva) { if (!repintada) PintarCerca(enfocar: false); } // la lista "cerca", igual
            else if (buscador != null && buscador.EnBusqueda) Buscar(buscador.Campo.value);
        }

        /// <summary>
        /// Pliega la hoja del buscador en la cápsula (botón atrás o "Contraer" de TalkBack): quita el modo modal y el
        /// foco vuelve a la cápsula.
        /// </summary>
        public void PlegarBuscador()
        {
            SeguirModalBuscador(enfocarCapsula: false); // desplegada en este mismo fotograma: aún no era modal
            buscador?.Plegar();
            SeguirModalBuscador(enfocarCapsula: true);
        }

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
            // La hoja del buscador se pliega y la cápsula se oculta mientras la ficha está abierta
            if (buscador != null)
            {
                PlegarBuscadorPorOtraVentana();
                buscador.Mostrar(false);
            }
            bool yaAbierta = FichaAbierta;
            ficha.Abrir(lugar, ahora);
            CentrarEnFicha(lugar);
            // De una ficha a otra la hoja tiene medida, pero la del lugar anterior: al disponerse, se recentra
            if (yaAbierta) centrarPendiente = lugar;

            // TalkBack: la ficha es modal (solo sus nodos) y el foco va al título; al cerrarla vuelve a este marcador
            marcadorFicha = MarcadorDe(lugar);
            if (capa != null) capa.Fijado = marcadorFicha; // su marcador no lo oculta ni la píldora ni otro que lo pise
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
            buscador?.Mostrar(true); // vuelve la cápsula
            // TalkBack: fuera los nodos de la ficha, vuelve el mapa y el foco al marcador que la abrió, si se ve
            var marcador = marcadorFicha;
            marcadorFicha = null;
            if (capa != null) capa.Fijado = null;
            accesibilidad.QuitarControles(ficha);
            accesibilidad.Modal = false;
            accesibilidad.PedirFoco(marcador != null && marcador.Visible ? marcador.Cuerpo : null);
        }

        /// <summary>Píldora "Precios", su menú y el velo que lo cierra al tocar fuera (FR-2, UX-DR6).</summary>
        void CrearFiltro()
        {
            pildora = new PildoraPrecios(PulsarPildora);
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
            menu.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(raiz, EscalaFuente.Sistema));

            // TalkBack: la píldora es un botón con la opción actual como valor; con el menú abierto el recorrido
            // es modal (píldora, que lo cierra, y las 5 opciones), igual que el velo a la vista
            // (con la explicación de la ubicación o la ficha abiertas, la píldora sale del recorrido modal)
            // Con una categoría (historia 2.4, parte 2) es el botón "Quitar categoría Salud", sin valor
            accesibilidad.Boton(pildora, EtiquetaPildora, PulsarPildora, () => categoriaMapa == null ? ValorFiltro() : "",
                visible: () => !ModalUbicacionAbierta && !FichaAbierta && !BuscadorAbierto, enModal: true);
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

        /// <summary>Etiqueta de la píldora en TalkBack: "Precios en el mapa" o, con una categoría, "Quitar categoría Salud".</summary>
        public string EtiquetaPildora() => categoriaMapa is CategoriaPoi c
            ? Textos.T(ClavesTexto.PildoraQuitarCategoria, NombreCategoria(c))
            : Textos.T(ClavesTexto.PreciosControl);

        void AlCambiarFiltro(FiltroPrecios nuevo)
        {
            if (capa != null) capa.Filtro = nuevo;
            if (pildora == null) return;
            if (categoriaMapa == null) pildora.Mostrar(nuevo); // con categoría la píldora la sigue enseñando
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
                PlegarBuscadorPorOtraVentana(); // a media altura la hoja taparía el menú
                menu.Marcar(filtro.Actual);
                menu.Mostrar(true);
                velo.EnableInClassList("abierto", true);
                ModalNuevo(); // reconstruye: solo la píldora y las opciones
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
        /// hoja del buscador desplegada la pliega; con la ficha del lugar abierta la cierra; con el menú abierto lo
        /// cierra sin cambiar el filtro. Si no, no hace nada (la app no sale: Input.backButtonLeavesApp sigue en
        /// false). Devuelve true si lo ha gestionado.
        /// </summary>
        public bool Atras()
        {
            if (ExplicacionAbierta) { AhoraNoUbicacion(); return true; }
            if (AvisoAbierto) { CerrarAviso(); return true; }
            if (BuscadorAbierto && cercaActiva) { QuitarCategoria(); return true; } // primero vuelven las categorías
            if (BuscadorAbierto) { PlegarBuscador(); return true; }
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
            if (buscador != null)
            {
                buscador.Desplegando -= AlDesplegarBuscador;
                buscador.TextoCambiado -= Buscar;
                buscador.ResultadoElegido -= ElegirResultado;
                buscador.BuscarComoDireccionPedido -= BuscarComoDireccion;
                buscador.CategoriaElegida -= ElegirCategoria;
                buscador.CategoriaQuitada -= QuitarCategoria;
            }
            if (direcciones != null)
            {
                direcciones.Cambiado -= AlCambiarDirecciones;
                direcciones.Cancelar();
            }
            AssistiveSupport.screenReaderStatusChanged -= AlCambiarLector;
            AccessibilitySettings.fontScaleChanged -= AlCambiarEscalaFuente;
            if (raiz != null) EscalaFuente.Olvidar(raiz);
            accesibilidad?.Desactivar();
        }

        /// <summary>
        /// True si el punto de pantalla (origen abajo a la izquierda, como Input) cae sobre un control:
        /// así el mapa no se mueve al pulsar un botón. Con el menú "Precios" abierto, todo el mapa
        /// cuenta como tapado: ese toque solo cierra el menú. Con la explicación de la ubicación, también. Con la
        /// ficha del lugar abierta, igual: un toque fuera de la hoja la cierra (su velo) y no mueve el mapa. La hoja del
        /// buscador tapa el mapa: mientras se arrastra y abierta del todo, entero; a media altura, lo que cubre (Pick).
        /// </summary>
        public bool TapaPunto(Vector2 pantalla)
        {
            if (MenuAbierto || ModalUbicacionAbierta || FichaAbierta) return true;
            if (buscador != null && buscador.Visible && (buscador.Arrastrando || buscador.AbiertaDelTodo)) return true;
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
        /// <summary>Cápsula y hoja del buscador; null sin interfaz.</summary>
        public HojaBuscador Buscador => buscador;
        /// <summary>Ajustes de accesibilidad del sistema que recibió al iniciarse (para las pruebas del arranque).</summary>
        internal IAccesibilidadSistema AccesibilidadSistema => accesibilidadSistema;
        /// <summary>Buscador que recibió al iniciarse (para las pruebas y la captura); null sin él.</summary>
        public ServicioBusqueda Busqueda => busqueda;
        /// <summary>"Encuentra cerca" que recibió al iniciarse (historia 2.4); null sin él.</summary>
        public ServicioCerca Cerca => servicioCerca;
        /// <summary>Buscador de direcciones que recibió al iniciarse (historia 2.3, parte 2); null sin él.</summary>
        public BuscadorDirecciones Direcciones => direcciones;
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
                buscador?.Repintar();
            });
        }

        /// <summary>Respeta las zonas seguras (barra de estado y de gestos) y aplica la escala de fuente.</summary>
        void AjustarDisposicion()
        {
            EscalaFuente.Aplicar(raiz, EscalaFuente.Sistema);

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
            // El buscador está fuera de la zona segura: plegado va encima de la barra de gestos y abierto del todo,
            // 64 dp por debajo de la barra de estado
            buscador?.PonerZonaSegura((Screen.height - segura.yMax) * escala, segura.yMin * escala);
            ColocarAtribucion();
        }

        readonly List<Lugar> lugaresVisibles = new List<Lugar>();
        readonly List<Rect> zonasOcupadas = new List<Rect>();

        /// <summary>
        /// Zonas (dp del panel) donde no se pinta ni marcador ni punto de interés: la píldora, el menú "Precios"
        /// abierto y las columnas de botones. Sin medida (antes de la primera disposición) no cuentan.
        /// Se reutiliza la lista; tras ActualizarCapas lleva además las zonas de los marcadores visibles.
        /// </summary>
        public IReadOnlyList<Rect> ZonasControles()
        {
            zonasOcupadas.Clear();
            if (raiz == null) return zonasOcupadas;
            Ocupar(pildora);
            if (MenuAbierto) Ocupar(menu);
            Ocupar(columnaControles); // tema y ubicación
            if (zoom != null && zoom.ClassListContains("visible")) Ocupar(zoom);
            Ocupar(buscador); // la cápsula (o lo que cubre la hoja desplegada)
            return zonasOcupadas;
        }

        void Ocupar(VisualElement e)
        {
            if (e == null || e.panel == null || e.resolvedStyle.display == DisplayStyle.None) return;
            var r = e.worldBound;
            if (float.IsNaN(r.width) || float.IsNaN(r.height) || r.width <= 0f || r.height <= 0f) return;
            zonasOcupadas.Add(r);
        }

        /// <summary>Para las pruebas y la captura: zonas ocupadas en la última actualización (controles y marcadores).</summary>
        public IReadOnlyList<Rect> ZonasOcupadas => zonasOcupadas;

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
            // Primero los marcadores (sin pisar los controles); después los puntos, que no pisan ni controles ni
            // marcadores visibles y se ocultan si repiten un lugar visible (FR-4)
            if (escala > 0f) ZonasControles();
            if (capa != null && escala > 0f) capa.Actualizar(escala, zonasOcupadas);
            if (capaPuntos != null && escala > 0f)
            {
                if (capa != null)
                {
                    var zonas = capa.ZonasVisibles; // con un bucle: AddRange sobre la interfaz asignaría memoria
                    for (int i = 0; i < zonas.Count; i++) zonasOcupadas.Add(zonas[i]);
                }
                capaPuntos.Actualizar(escala, LugaresVisibles(), zonasOcupadas);
            }
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
            // Ruta sin notificaciones (permiso denegado): el aviso sale una vez por ruta, cuando no hay otra ventana
            if (ubicacion != null)
            {
                if (!ubicacion.RutaSoloPrimerPlano)
                {
                    avisoNotificacionesVisto = false;
                    // Terminó la ruta o se dio el permiso: el aviso ya no es verdad
                    if (AvisoAbierto && aviso.SinNotificaciones) CerrarAviso();
                }
                // Espera a que se cierre otra ventana (no tapa el aviso de ubicación)
                else if (!avisoNotificacionesVisto && !ExplicacionAbierta && !AvisoAbierto)
                {
                    avisoNotificacionesVisto = true;
                    AbrirAvisoNotificaciones();
                }
            }
            // El aviso abierto sigue al estado: si ya no toca avisar se cierra; si cambia el motivo, cambia el texto
            // (el de las notificaciones no depende del estado de la ubicación)
            if (AvisoAbierto && !aviso.SinNotificaciones)
            {
                if (AccionUbicacionActual() != AccionUbicacion.Avisar) CerrarAviso();
                else if (aviso.Motivo != ubicacion.Estado)
                {
                    aviso.Abrir(ubicacion.Estado);
                    accesibilidad?.RefrescarEtiqueta(aviso.Texto);
                }
            }
            // La hoja del buscador también se despliega y se pliega con el dedo: el modo modal de TalkBack la sigue
            SeguirModalBuscador(enfocarCapsula: true);
            // Con TalkBack: rehace los nodos de marcadores y puntos si cambian los visibles o su orden
            // (después de las dos capas, para que los puntos ocultos en este fotograma no tengan nodo)
            accesibilidad?.ActualizarMarcadores();
            // Botones y marcadores se mueven (zona segura, escala de fuente, mapa): TalkBack necesita sus marcos al día
            if (accesibilidad != null && AccesibilidadPantalla.LectorActivo) AssistiveSupport.activeHierarchy?.RefreshNodeFrames();
        }
    }
}
