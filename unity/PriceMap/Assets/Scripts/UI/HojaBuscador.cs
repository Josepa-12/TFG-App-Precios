using System;
using System.Collections.Generic;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Buscador (historia 2.2, parte 1; FR13, UX-DR4 y UX-DR5; prototipo .dock): la cápsula de cristal de abajo con el
    /// campo "Buscar en Sanlúcar" y un avatar genérico de 40 dp, que al tocarla o arrastrarla se despliega en una hoja
    /// que sigue al dedo (FisicaHoja). La cápsula y la hoja son el mismo elemento: con la fracción abierta k, la forma
    /// se mezcla con s = min(1, k / 0,15) (márgenes de 12/12/20 a 0, esquinas de píldora a 24 arriba y 0 abajo, fondo
    /// de la cápsula a surface opaco). Antes de escribir muestra los chips de categorías y "Encuentra cerca", que aún
    /// no hacen nada (historia 2.4); la búsqueda es de la 2.3.
    ///
    /// Un movimiento de menos de 6 dp fuera de la lista es un toque: abre la hoja del todo y enfoca el campo (sale el
    /// teclado). Uno mayor es un arrastre: captura el puntero (el campo no recibe el soltar y no saca el teclado).
    /// La abren, pliegan y ocultan PantallaMapa y sus gestos; nada se guarda.
    /// </summary>
    public class HojaBuscador : VisualElement
    {
        /// <summary>Categorías de los chips, en el orden en que se ven (los nombres van en ClavesTexto.Buscador*).</summary>
        public static readonly CategoriaPoi[] Categorias =
        {
            CategoriaPoi.Comer, CategoriaPoi.Bares, CategoriaPoi.Super, CategoriaPoi.Farmacia,
            CategoriaPoi.Gasolina, CategoriaPoi.Deporte, CategoriaPoi.Salud, CategoriaPoi.Cultura,
        };

        /// <summary>Márgenes de la cápsula plegada (izquierda y derecha, abajo) en dp; abierta, 0.</summary>
        public const float MargenLado = 12f, MargenAbajo = 20f;
        /// <summary>Alto mínimo de la cápsula (DESIGN components.capsule): 5 + campo de 46 + 5.</summary>
        public const float AltoCapsulaMin = 56f;
        const float Relleno = 5f;            // plegada, el mismo hueco por encima y por debajo del campo
        const float RellenoArriba = 24f;     // abierta, el tirador tiene su propio hueco
        const float RellenoLado = 14f;       // abierta (prototipo .dock.open)
        /// <summary>Radio de las esquinas superiores de la hoja abierta (--radio-sheet).</summary>
        public const float RadioHoja = 24f;
        /// <summary>Duración de la transición al soltar, abrir o plegar (USS .buscador).</summary>
        public const float Transicion = 0.32f;

        public const string ClaseAbierta = "buscador--abierta";
        public const string ClaseArrastrando = "buscador--arrastrando";
        /// <summary>"Quitar animaciones" del sistema (UX-DR19): la hoja cambia de altura sin transición.</summary>
        public const string ClaseSinTransicion = "buscador--sin-transicion";

        readonly Func<string, string> textos;
        readonly FisicaHoja fisica = new FisicaHoja();
        readonly List<VisualElement> chips = new List<VisualElement>();
        readonly List<VisualElement> glifos = new List<VisualElement>();

        float altoPanel = float.NaN;     // alto del hueco (dp); NaN hasta la primera disposición
        float seguraArriba, seguraAbajo; // zonas seguras (dp) que pone PantallaMapa
        int puntero = -1;                // dedo del gesto en curso

        /// <summary>
        /// El usuario empieza a desplegar el buscador (toque o arrastre): PantallaMapa cierra la ficha y el menú
        /// "Precios" (nunca están abiertos a la vez).
        /// </summary>
        public event Action Desplegando;

        public VisualElement AsaZona { get; }
        public VisualElement Asa { get; }
        public VisualElement Interior { get; }
        public VisualElement Linea { get; }
        /// <summary>Caja del campo (fondo redondeado con la lupa y el texto).</summary>
        public VisualElement CajaCampo { get; }
        public TextField Campo { get; }
        public VisualElement Lupa { get; }
        public VisualElement Avatar { get; }
        public ScrollView Panel { get; }
        /// <summary>Chips de categorías en el orden de Categorias.</summary>
        public IReadOnlyList<VisualElement> Chips => chips;
        /// <summary>Entrada "Encuentra cerca".</summary>
        public VisualElement Cerca { get; }
        /// <summary>
        /// "Expandir" y "Contraer" de TalkBack (historia 2.2, parte 2): no se ven ni recogen toques; solo dan el marco
        /// de su nodo (las dos mitades de la zona del tirador).
        /// </summary>
        public VisualElement Expandir { get; }
        public VisualElement Contraer { get; }

        /// <summary>Sin transición al cambiar de altura (animaciones quitadas en el sistema).</summary>
        public bool SinTransicion
        {
            get => ClassListContains(ClaseSinTransicion);
            set => EnableInClassList(ClaseSinTransicion, value);
        }

        /// <summary>Física de la hoja (fracción abierta y gesto en curso).</summary>
        public FisicaHoja Fisica => fisica;
        /// <summary>Fracción abierta: 0 plegada (cápsula), 1 abierta del todo.</summary>
        public float Fraccion => fisica.Fraccion;
        /// <summary>Desplegada (más que la cápsula) o con un dedo arrastrándola.</summary>
        public bool Abierta => fisica.Fraccion > 0f || fisica.Arrastrando;
        public bool AbiertaDelTodo => fisica.AbiertaDelTodo;
        public bool Arrastrando => fisica.Arrastrando;
        /// <summary>Se ha pedido el foco del campo (y con él el teclado) con un toque; se quita al arrastrar o plegar.</summary>
        public bool TecladoPedido { get; private set; }
        /// <summary>La cápsula se ve (se oculta mientras la ficha está abierta).</summary>
        public bool Visible { get; private set; } = true;

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public HojaBuscador(Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "buscador";
            AddToClassList("buscador");
            pickingMode = PickingMode.Position;

            AsaZona = Nuevo("buscador-asa-zona", "buscador__asa-zona");
            Asa = Nuevo("buscador-asa", "buscador__asa");
            AsaZona.Add(Asa);
            Expandir = Nuevo("buscador-expandir", "buscador__solo-lector");
            Expandir.AddToClassList("buscador__expandir");
            Contraer = Nuevo("buscador-contraer", "buscador__solo-lector");
            Contraer.AddToClassList("buscador__contraer");
            AsaZona.Add(Expandir);
            AsaZona.Add(Contraer);

            Interior = Nuevo("buscador-interior", "buscador__interior");
            Add(Interior);
            Add(AsaZona); // encima del interior: plegada va sobre el borde superior

            Linea = Nuevo("buscador-linea", "buscador__linea");
            Interior.Add(Linea);
            CajaCampo = Nuevo("buscador-caja-campo", "buscador__campo");
            Linea.Add(CajaCampo);
            Lupa = Glifo(IconosLugar.Lupa, "buscador-lupa", "buscador__lupa");
            CajaCampo.Add(Lupa);
            Campo = new TextField { name = "buscador-campo" };
            Campo.AddToClassList("buscador__texto");
            Campo.AddToClassList("t-body");
            Campo.textEdition.placeholder = this.textos(ClavesTexto.BuscadorCampo);
            CajaCampo.Add(Campo);

            // Avatar genérico: captura el toque (no despliega la hoja) y aún no hace nada (perfil, historia 3.5)
            Avatar = new VisualElement { name = "buscador-avatar", pickingMode = PickingMode.Position };
            Avatar.AddToClassList("buscador__avatar");
            Avatar.Add(Glifo(IconosLugar.Avatar, "buscador-avatar-glifo", "buscador__avatar-glifo"));
            Linea.Add(Avatar);

            // Sin barras de desplazamiento (no hay tema por defecto de Unity que las dibuje): se arrastra con el dedo
            Panel = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "buscador-panel",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            Panel.AddToClassList("buscador__panel");
            Interior.Add(Panel);

            var filaChips = Nuevo("buscador-chips", "buscador__chips");
            Panel.Add(filaChips);
            foreach (var categoria in Categorias)
            {
                var chip = new VisualElement { name = "buscador-chip", pickingMode = PickingMode.Position };
                chip.AddToClassList("buscador__chip");
                var icono = Nuevo("buscador-chip-icono", "buscador__chip-icono");
                icono.AddToClassList("buscador__chip-icono--" + Nombre(categoria));
                icono.Add(Glifo(IconosLugar.GlifoDe(categoria), "buscador-chip-glifo", "buscador__chip-glifo"));
                chip.Add(icono);
                var texto = new Label(this.textos(ClaveCategoria(categoria))) { name = "buscador-chip-texto", pickingMode = PickingMode.Ignore };
                texto.AddToClassList("buscador__chip-texto");
                texto.AddToClassList("t-meta-fuerte");
                chip.Add(texto);
                filaChips.Add(chip);
                chips.Add(chip);
            }

            Cerca = new VisualElement { name = "buscador-cerca", pickingMode = PickingMode.Position };
            Cerca.AddToClassList("buscador__cerca");
            Cerca.Add(Glifo(IconosLugar.Generico, "buscador-cerca-glifo", "buscador__cerca-glifo"));
            var textoCerca = new Label(this.textos(ClavesTexto.BuscadorCerca)) { name = "buscador-cerca-texto", pickingMode = PickingMode.Ignore };
            textoCerca.AddToClassList("buscador__cerca-texto");
            textoCerca.AddToClassList("t-label");
            Cerca.Add(textoCerca);
            Panel.Add(Cerca);

            // Gestos: el dedo baja en la hoja (fase de bajada, antes que el campo y la lista). Mover, soltar y cancelar
            // se escuchan en la raíz del panel mientras dura el gesto: el dedo puede salir de la cápsula (56 dp) antes
            // de los 6 dp del arrastre y aun así la hoja lo sigue y se entera de que se suelta
            RegisterCallback<PointerDownEvent>(AlBajar, TrickleDown.TrickleDown);
            // Con la fuente grande la línea crece: la cápsula plegada la sigue
            Linea.RegisterCallback<GeometryChangedEvent>(_ => Recalcular());

            Aplicar();
        }

        // ───────────────────────── Medidas ─────────────────────────

        /// <summary>Alto (dp) del hueco donde va la hoja; lo da PantallaMapa al disponerse.</summary>
        public void Medir(float alto)
        {
            altoPanel = alto;
            Recalcular();
        }

        /// <summary>Zonas seguras de arriba (barra de estado) y abajo (barra de gestos), en dp.</summary>
        public void PonerZonaSegura(float arriba, float abajo)
        {
            seguraArriba = Mathf.Max(0f, arriba);
            seguraAbajo = Mathf.Max(0f, abajo);
            Recalcular();
        }

        bool Medida => !float.IsNaN(altoPanel) && altoPanel > 0f;

        /// <summary>Alto de la cápsula plegada: 56 dp, o más si la línea del campo crece con la fuente.</summary>
        public float AltoCapsula
        {
            get
            {
                float linea = Linea.layout.height;
                return float.IsNaN(linea) ? AltoCapsulaMin : Mathf.Max(AltoCapsulaMin, linea + 2f * Relleno);
            }
        }

        /// <summary>Lo que ocupa abajo la cápsula plegada con su margen y la zona segura (dp).</summary>
        public float ReservaInferior => AltoCapsula + MargenAbajo + seguraAbajo;

        /// <summary>Borde superior (dp) de la cápsula plegada y de la hoja abierta del todo.</summary>
        public float TopPlegada => altoPanel - ReservaInferior;
        public float TopAbierta => seguraArriba + FisicaHoja.HuecoArriba;

        void Recalcular()
        {
            if (Medida) fisica.PonerAltoUtil(TopPlegada - TopAbierta);
            Aplicar();
        }

        /// <summary>Borde superior (dp) de la hoja con la fracción actual; NaN sin medida.</summary>
        public float Top => Medida ? TopPlegada - fisica.Fraccion * fisica.AltoUtil : float.NaN;

        /// <summary>Mezcla de cápsula (0) a hoja (1): s = min(1, k / 0,15).</summary>
        public static float Mezcla(float fraccion) => Mathf.Clamp01(fraccion / FisicaHoja.PlegarDebajo);

        /// <summary>Pone la forma de la fracción actual: posición, márgenes, esquinas, relleno y clases.</summary>
        void Aplicar()
        {
            float k = fisica.Fraccion, s = Mezcla(k);
            float capsula = AltoCapsula;
            style.left = MargenLado * (1f - s);
            style.right = MargenLado * (1f - s);
            style.bottom = (MargenAbajo + seguraAbajo) * (1f - s);
            if (Medida)
            {
                style.top = Top;
                style.height = StyleKeyword.Null;
            }
            else
            {
                // Antes de la primera disposición: la cápsula, anclada abajo
                style.top = StyleKeyword.Null;
                style.height = capsula;
            }
            float radioPildora = capsula / 2f;
            float arriba = Mathf.Lerp(radioPildora, RadioHoja, s), abajo = Mathf.Lerp(radioPildora, 0f, s);
            style.borderTopLeftRadius = arriba;
            style.borderTopRightRadius = arriba;
            style.borderBottomLeftRadius = abajo;
            style.borderBottomRightRadius = abajo;
            style.paddingTop = Mathf.Lerp(Relleno, RellenoArriba, s);
            style.paddingBottom = Mathf.Lerp(Relleno, seguraAbajo, s);
            style.paddingLeft = Mathf.Lerp(Relleno, RellenoLado, s);
            style.paddingRight = Mathf.Lerp(Relleno, RellenoLado, s);
            EnableInClassList(ClaseAbierta, s >= 1f);
            EnableInClassList(ClaseArrastrando, fisica.Arrastrando);
        }

        // ───────────────────────── Abrir y plegar ─────────────────────────

        /// <summary>Abre la hoja del todo (sin gesto).</summary>
        public void Abrir()
        {
            fisica.Poner(1f);
            Aplicar();
        }

        /// <summary>Deja la hoja en esa fracción (sin gesto; la captura la deja a media altura).</summary>
        public void Poner(float fraccion)
        {
            fisica.Poner(fraccion);
            Aplicar();
        }

        /// <summary>Pliega la hoja en la cápsula: quita el foco del campo (y el teclado) y lo vacía.</summary>
        public void Plegar()
        {
            SoltarPuntero();
            fisica.Cancelar();
            fisica.Poner(0f);
            QuitarFoco();
            Campo.SetValueWithoutNotify(string.Empty);
            Aplicar();
        }

        /// <summary>Muestra u oculta la cápsula (oculta mientras la ficha está abierta).</summary>
        public void Mostrar(bool visible)
        {
            Visible = visible;
            if (!visible)
            {
                SoltarPuntero();
                fisica.Cancelar(); // un gesto a medias no deja la hoja sin responder
                Aplicar();
            }
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void EnfocarCampo()
        {
            TecladoPedido = true;
            // En el móvil, si el toque fue en el campo, el teclado lo saca el propio TextField al soltar
            if (Campo.panel != null) Campo.Focus();
        }

        void QuitarFoco()
        {
            TecladoPedido = false;
            if (Campo.panel != null) Campo.Blur();
        }

        // ───────────────────────── Gestos ─────────────────────────

        /// <summary>Baja el dedo en ese punto (dp del panel); <paramref name="enLista"/> si fue en la lista de la hoja.</summary>
        public void Bajar(Vector2 punto, double ms, bool enLista = false) =>
            fisica.Bajar(punto, ms, enLista, Panel.scrollOffset.y);

        /// <summary>Mueve el dedo; al pasar a arrastre quita el foco del campo y avisa de que se despliega.</summary>
        public void Mover(Vector2 punto, double ms)
        {
            bool antes = fisica.Arrastrando;
            if (!fisica.Mover(punto, ms)) return;
            if (!antes)
            {
                QuitarFoco(); // un arrastre nunca saca el teclado
                Desplegando?.Invoke();
            }
            Aplicar();
        }

        /// <summary>Suelta el dedo: toque (abre del todo y enfoca el campo) o arrastre (destino de FisicaHoja).</summary>
        public FisicaHoja.Suelta Soltar(double ms)
        {
            var r = fisica.Soltar(ms);
            if (r.EsToque)
            {
                Desplegando?.Invoke();
                EnfocarCampo();
            }
            else if (r.FueArrastre && r.Destino == DestinoHoja.Plegada)
            {
                QuitarFoco();
                Campo.SetValueWithoutNotify(string.Empty);
            }
            Aplicar();
            return r;
        }

        void AlBajar(PointerDownEvent e)
        {
            if (!Visible) return;
            if (fisica.Pulsado && e.pointerId == puntero) return;
            if (e.pointerType == UnityEngine.UIElements.PointerType.mouse && e.button != 0) return;
            var objetivo = e.target as VisualElement;
            if (Dentro(objetivo, Avatar)) return; // el avatar no despliega ni arrastra
            // Un gesto anterior que no llegó a soltarse (p. ej. otro dedo) no bloquea la hoja: empieza uno nuevo
            if (fisica.Pulsado) { SoltarPuntero(); fisica.Cancelar(); }
            puntero = e.pointerId;
            raizGesto = panel?.visualTree;
            if (raizGesto != null)
            {
                raizGesto.RegisterCallback<PointerMoveEvent>(AlMover, TrickleDown.TrickleDown);
                raizGesto.RegisterCallback<PointerUpEvent>(AlSoltar, TrickleDown.TrickleDown);
                raizGesto.RegisterCallback<PointerCancelEvent>(AlCancelar, TrickleDown.TrickleDown);
            }
            Bajar(e.position, e.timestamp, Dentro(objetivo, Panel));
        }

        VisualElement raizGesto; // raíz del panel que escucha el gesto en curso

        void AlMover(PointerMoveEvent e)
        {
            if (e.pointerId != puntero || !fisica.Pulsado) return;
            bool antes = fisica.Arrastrando;
            Mover(e.position, e.timestamp);
            if (!fisica.Arrastrando) return;
            // Desde aquí el gesto es de la hoja: ni el campo (no saca el teclado) ni la lista reciben más
            if (!antes) this.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        void AlSoltar(PointerUpEvent e)
        {
            if (e.pointerId != puntero) return;
            bool arrastre = fisica.Arrastrando;
            SoltarPuntero();
            Soltar(e.timestamp);
            if (arrastre) e.StopPropagation();
        }

        void AlCancelar(PointerCancelEvent e)
        {
            if (e.pointerId != puntero) return;
            bool arrastre = fisica.Arrastrando;
            SoltarPuntero();
            if (arrastre) Soltar(e.timestamp); // la hoja va a su destino, como al soltar
            else fisica.Cancelar();
        }

        void SoltarPuntero()
        {
            if (puntero >= 0 && this.HasPointerCapture(puntero)) this.ReleasePointer(puntero);
            puntero = -1;
            if (raizGesto != null)
            {
                raizGesto.UnregisterCallback<PointerMoveEvent>(AlMover, TrickleDown.TrickleDown);
                raizGesto.UnregisterCallback<PointerUpEvent>(AlSoltar, TrickleDown.TrickleDown);
                raizGesto.UnregisterCallback<PointerCancelEvent>(AlCancelar, TrickleDown.TrickleDown);
                raizGesto = null;
            }
        }

        static bool Dentro(VisualElement e, VisualElement contenedor)
        {
            for (; e != null; e = e.parent) if (e == contenedor) return true;
            return false;
        }

        // ───────────────────────── Dibujo ─────────────────────────

        /// <summary>Vuelve a dibujar los glifos (el color cambia con el tema).</summary>
        public void Repintar()
        {
            foreach (var g in glifos) g.MarkDirtyRepaint();
        }

        VisualElement Glifo(string glifo, string nombre, string clase)
        {
            var e = Nuevo(nombre, clase);
            e.generateVisualContent += mgc => IconosLugar.Dibujar(mgc.painter2D, glifo, e.contentRect, e.resolvedStyle.color);
            e.RegisterCallback<GeometryChangedEvent>(_ => e.MarkDirtyRepaint());
            glifos.Add(e);
            return e;
        }

        /// <summary>Clave del nombre del chip de cada categoría ("Restaurantes").</summary>
        public static string ClaveCategoria(CategoriaPoi categoria)
        {
            switch (categoria)
            {
                case CategoriaPoi.Comer: return ClavesTexto.BuscadorComer;
                case CategoriaPoi.Bares: return ClavesTexto.BuscadorBares;
                case CategoriaPoi.Super: return ClavesTexto.BuscadorSuper;
                case CategoriaPoi.Farmacia: return ClavesTexto.BuscadorFarmacia;
                case CategoriaPoi.Gasolina: return ClavesTexto.BuscadorGasolina;
                case CategoriaPoi.Deporte: return ClavesTexto.BuscadorDeporte;
                case CategoriaPoi.Salud: return ClavesTexto.BuscadorSalud;
                case CategoriaPoi.Cultura: return ClavesTexto.BuscadorCultura;
                default: return null;
            }
        }

        /// <summary>Sufijo de la clase del color del icono (--color-poi-&lt;categoría&gt;).</summary>
        public static string Nombre(CategoriaPoi categoria) => categoria.ToString().ToLowerInvariant();

        static VisualElement Nuevo(string nombre, string clase)
        {
            var e = new VisualElement { name = nombre, pickingMode = PickingMode.Ignore };
            e.AddToClassList(clase);
            return e;
        }
    }
}
