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
        VisualElement raiz, zonaSegura, zoom;
        BotonRedondo botonTema, botonAcercar, botonAlejar;
        Label atribucion;
        AccesibilidadPantalla accesibilidad;
        CapaMarcadores capa;
        Rect zonaSeguraAplicada;
        float anchoAplicado;
        bool temaYaAnunciado; // el primer tema (al arrancar) no se anuncia: solo los cambios del usuario

        /// <param name="lugares">Lugares que se dibujan como marcadores (vacío si la carga falló).</param>
        /// <param name="ahora">Momento con el que se calcula el resumen del precio (AD-12: lo da el Arranque).</param>
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora)
        {
            var documento = GetComponent<UIDocument>();
            var raizDocumento = documento != null ? documento.rootVisualElement : null;
            if (raizDocumento != null) raizDocumento.pickingMode = PickingMode.Ignore; // la raíz no tapa el mapa
            Inicializar(mapa, tema, lugares, ahora, raizDocumento);
        }

        /// <summary>Igual, con el árbol de la interfaz dado (las pruebas lo construyen sin panel).</summary>
        public void Inicializar(IMapa mapa, ServicioTema tema, ServicioLugares lugares, DateTime ahora, VisualElement raizDocumento)
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

            // Marcadores de los lugares, debajo de los botones (primer hijo de la raíz)
            capa = new CapaMarcadores(raiz.Q("capa-marcadores"), this.mapa, ConResumen(lugares, ahora));
            foreach (var m in capa.Marcadores)
            {
                var marcador = m;
                accesibilidad.Marcador(marcador.Cuerpo, () => marcador.Anuncio, () => marcador.Visible, () => marcador.Posicion);
            }

            this.tema.Cambiado += AlCambiarTema;
            AssistiveSupport.screenReaderStatusChanged += AlCambiarLector;
            AccessibilitySettings.fontScaleChanged += AlCambiarEscalaFuente;
            raiz.RegisterCallback<GeometryChangedEvent>(_ => AjustarDisposicion());

            AlCambiarLector(AccesibilidadPantalla.LectorActivo);
            // El tema inicial llega con ServicioTema.Iniciar (lo llama el Arranque justo después)
        }

        static IEnumerable<(Lugar, ResumenLugar)> ConResumen(ServicioLugares lugares, DateTime ahora)
        {
            if (lugares == null) yield break;
            foreach (var l in lugares.Lugares) yield return (l, lugares.Resumen(l.Id, ahora).Valor);
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
        /// <summary>Capa de marcadores; null sin interfaz.</summary>
        public CapaMarcadores Capa => capa;
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

        void AlCambiarEscalaFuente(float escala) { if (raiz != null) EscalaFuente.Aplicar(raiz, escala); }

        void RepintarIconos()
        {
            // El color nuevo del tema se resuelve en el siguiente pase de estilos
            raiz.schedule.Execute(() => { botonTema.MarkDirtyRepaint(); botonAcercar.MarkDirtyRepaint(); botonAlejar.MarkDirtyRepaint(); capa?.Repintar(); });
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

        // La cámara se mueve en Update (CamaraMapa, orden -100): aquí ya está quieta en este fotograma
        void LateUpdate()
        {
            if (capa != null)
            {
                float escala = EscalaDp;
                if (escala > 0f) capa.Actualizar(escala);
                // Con TalkBack: rehace los nodos de los marcadores si cambian los visibles o su orden
                accesibilidad.ActualizarMarcadores();
            }
            // Botones y marcadores se mueven (zona segura, escala de fuente, mapa): TalkBack necesita sus marcos al día
            if (accesibilidad != null && AccesibilidadPantalla.LectorActivo) AssistiveSupport.activeHierarchy?.RefreshNodeFrames();
        }
    }
}
