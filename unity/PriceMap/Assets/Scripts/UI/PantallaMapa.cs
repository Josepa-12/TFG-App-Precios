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
        VisualElement raiz, zonaSegura, zoom;
        BotonRedondo botonTema, botonAcercar, botonAlejar;
        Label atribucion;
        AccesibilidadPantalla accesibilidad;
        Rect zonaSeguraAplicada;
        float anchoAplicado;
        bool temaYaAnunciado; // el primer tema (al arrancar) no se anuncia: solo los cambios del usuario

        public void Inicializar(IMapa mapa, ServicioTema tema)
        {
            var documento = GetComponent<UIDocument>();
            var raizDocumento = documento != null ? documento.rootVisualElement : null;
            if (raizDocumento != null) raizDocumento.pickingMode = PickingMode.Ignore; // la raíz no tapa el mapa
            Inicializar(mapa, tema, raizDocumento);
        }

        /// <summary>Igual, con el árbol de la interfaz dado (las pruebas lo construyen sin panel).</summary>
        public void Inicializar(IMapa mapa, ServicioTema tema, VisualElement raizDocumento)
        {
            this.mapa = mapa;
            this.tema = tema;
            this.mapa.Centrar(VistaInicial);

            raiz = raizDocumento?.Q("raiz");
            if (raiz == null) return; // sin interfaz (p. ej. en pruebas): el mapa funciona igual

            zonaSegura = raiz.Q("zona-segura");
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
            accesibilidad.Boton(botonTema, () => Textos.T(ClavesTexto.TemaCambiar), () => this.tema.Alternar(), ValorTema);
            accesibilidad.Boton(botonAcercar, () => Textos.T(ClavesTexto.MapaAcercar), () => this.mapa.Acercar(), visible: () => AccesibilidadPantalla.LectorActivo);
            accesibilidad.Boton(botonAlejar, () => Textos.T(ClavesTexto.MapaAlejar), () => this.mapa.Alejar(), visible: () => AccesibilidadPantalla.LectorActivo);

            this.tema.Cambiado += AlCambiarTema;
            AssistiveSupport.screenReaderStatusChanged += AlCambiarLector;
            AccessibilitySettings.fontScaleChanged += AlCambiarEscalaFuente;
            raiz.RegisterCallback<GeometryChangedEvent>(_ => AjustarDisposicion());

            AlCambiarLector(AccesibilidadPantalla.LectorActivo);
            // El tema inicial llega con ServicioTema.Iniciar (lo llama el Arranque justo después)
        }

        void OnDestroy()
        {
            if (tema != null) tema.Cambiado -= AlCambiarTema;
            AssistiveSupport.screenReaderStatusChanged -= AlCambiarLector;
            AccessibilitySettings.fontScaleChanged -= AlCambiarEscalaFuente;
            if (raiz != null) EscalaFuente.Olvidar(raiz);
            accesibilidad?.Desactivar();
        }

        /// <summary>
        /// True si el punto de pantalla (origen abajo a la izquierda, como Input) cae sobre un control:
        /// así el mapa no se mueve al pulsar un botón.
        /// </summary>
        public bool TapaPunto(Vector2 pantalla)
        {
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

        void AlCambiarEscalaFuente(float escala) { if (raiz != null) EscalaFuente.Aplicar(raiz, escala); }

        void RepintarIconos()
        {
            // El color nuevo del tema se resuelve en el siguiente pase de estilos
            raiz.schedule.Execute(() => { botonTema.MarkDirtyRepaint(); botonAcercar.MarkDirtyRepaint(); botonAlejar.MarkDirtyRepaint(); });
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
            zonaSegura.style.left = segura.xMin * escala;
            zonaSegura.style.right = (Screen.width - segura.xMax) * escala;
            zonaSegura.style.top = (Screen.height - segura.yMax) * escala;
            zonaSegura.style.bottom = segura.yMin * escala;
        }

        void Update()
        {
            // Los botones pueden moverse (zona segura, escala de fuente): TalkBack necesita sus marcos al día
            if (accesibilidad != null && AccesibilidadPantalla.LectorActivo) AssistiveSupport.activeHierarchy?.RefreshNodeFrames();
        }
    }
}
