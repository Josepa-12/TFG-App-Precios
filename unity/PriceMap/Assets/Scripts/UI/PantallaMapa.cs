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
        VisualElement raiz, zonaSegura, zonaSeguraPrecios, zoom, velo;
        PildoraPrecios pildora;
        MenuPrecios menu;
        BotonRedondo botonTema, botonAcercar, botonAlejar;
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
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora, ServicioFiltro filtro)
        {
            var documento = GetComponent<UIDocument>();
            var raizDocumento = documento != null ? documento.rootVisualElement : null;
            if (raizDocumento != null) raizDocumento.pickingMode = PickingMode.Ignore; // la raíz no tapa el mapa
            Inicializar(mapa, tema, lugares, ahora, raizDocumento, filtro);
        }

        /// <summary>Igual, con el árbol de la interfaz dado (las pruebas lo construyen sin panel).</summary>
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora, VisualElement raizDocumento,
            ServicioFiltro filtro = null)
        {
            this.mapa = mapa;
            this.tema = tema;
            this.filtro = filtro;
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
            botonAcercar = new BotonRedondo(Icono.Mas, () => this.mapa.Acercar()) { name = "boton-acercar" };
            botonAlejar = new BotonRedondo(Icono.Menos, () => this.mapa.Alejar()) { name = "boton-alejar" };
            zoom.Add(botonAcercar);
            zoom.Add(botonAlejar);

            accesibilidad = new AccesibilidadPantalla(raiz);
            if (this.filtro != null) CrearFiltro(); // la píldora va arriba a la izquierda: primera en TalkBack
            accesibilidad.Boton(botonTema, () => Textos.T(ClavesTexto.TemaCambiar), () => this.tema.Alternar(), ValorTema);
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
                accesibilidad.Marcador(marcador.Cuerpo, () => marcador.Anuncio, () => marcador.Visible, () => marcador.Posicion);
            }

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
            accesibilidad.Boton(pildora, () => Textos.T(ClavesTexto.PreciosControl), AlternarMenu, ValorFiltro, enModal: true);
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
        /// Botón atrás de Android (KeyCode.Escape): con el menú abierto lo cierra sin cambiar el filtro.
        /// Con el menú cerrado no hace nada (la app no sale: Input.backButtonLeavesApp sigue en false).
        /// Devuelve true si lo ha gestionado.
        /// </summary>
        public bool Atras()
        {
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
            AssistiveSupport.screenReaderStatusChanged -= AlCambiarLector;
            AccessibilitySettings.fontScaleChanged -= AlCambiarEscalaFuente;
            if (raiz != null) EscalaFuente.Olvidar(raiz);
            accesibilidad?.Desactivar();
        }

        /// <summary>
        /// True si el punto de pantalla (origen abajo a la izquierda, como Input) cae sobre un control:
        /// así el mapa no se mueve al pulsar un botón. Con el menú "Precios" abierto, todo el mapa
        /// cuenta como tapado: ese toque solo cierra el menú.
        /// </summary>
        public bool TapaPunto(Vector2 pantalla)
        {
            if (MenuAbierto) return true;
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
            raiz.schedule.Execute(() => { botonTema.MarkDirtyRepaint(); botonAcercar.MarkDirtyRepaint(); botonAlejar.MarkDirtyRepaint(); capa?.Repintar(); pildora?.Repintar(); menu?.Repintar(); });
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
            // Con TalkBack: rehace los nodos de marcadores y puntos si cambian los visibles o su orden
            // (después de las dos capas, para que los puntos ocultos en este fotograma no tengan nodo)
            accesibilidad?.ActualizarMarcadores();
            // Botones y marcadores se mueven (zona segura, escala de fuente, mapa): TalkBack necesita sus marcos al día
            if (accesibilidad != null && AccesibilidadPantalla.LectorActivo) AssistiveSupport.activeHierarchy?.RefreshNodeFrames();
        }
    }
}
