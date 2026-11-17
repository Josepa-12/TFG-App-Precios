using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// El módulo de accesibilidad de Unity 6 no se rellena solo desde UI Toolkit: cada pantalla
    /// declara sus controles con nombre, rol y estado para TalkBack (Accessibility Floor de EXPERIENCE.md).
    /// Recorrido: primero los botones y después los marcadores visibles, de arriba abajo y de izquierda a derecha.
    /// </summary>
    public class AccesibilidadPantalla
    {
        class Control
        {
            public VisualElement elemento;
            public Func<string> etiqueta, valor;
            public Func<bool> visible, seleccionado;
            public Action accion;
            public bool enModal;
        }

        class Marca
        {
            public VisualElement elemento;
            public Func<string> etiqueta;
            public Func<bool> visible;
            public Func<Vector2> posicion;
        }

        readonly List<Control> controles = new List<Control>();
        readonly List<Marca> marcas = new List<Marca>();
        readonly VisualElement raiz;
        AccessibilityHierarchy jerarquia;
        // Nodos de los marcadores en la jerarquía activa, en el orden del recorrido
        readonly List<(Marca marca, AccessibilityNode nodo)> nodosMarcas = new List<(Marca, AccessibilityNode)>();
        // Nodos de los controles en la jerarquía activa, para poder llevarles el foco
        readonly Dictionary<VisualElement, AccessibilityNode> nodosControles = new Dictionary<VisualElement, AccessibilityNode>();

        public AccesibilidadPantalla(VisualElement raiz) => this.raiz = raiz;

        /// <summary>Solo para las pruebas: si no es null, sustituye al estado real del lector de pantalla.</summary>
        internal static Func<bool> LectorForzado;

        public static bool LectorActivo => LectorForzado?.Invoke() ?? AssistiveSupport.isScreenReaderEnabled;

        /// <summary>Se lanza con cada aviso que se pide, esté o no el lector encendido (antes de mirarlo).</summary>
        public static event Action<string> AnuncioPedido;

        /// <summary>Se lanza con cada aviso que llega de verdad al lector (nunca con el lector apagado).</summary>
        public static event Action<string> Anunciado;

        bool modal;

        /// <summary>
        /// Modo modal (un menú abierto): el recorrido solo tiene los controles declarados con enModal,
        /// y ningún marcador. Al cambiarlo se reconstruye la jerarquía.
        /// </summary>
        public bool Modal
        {
            get => modal;
            set
            {
                if (modal == value) return;
                modal = value;
                Reconstruir();
            }
        }

        /// <summary>Último control al que se pidió llevar el foco (también sin lector, para las pruebas).</summary>
        public VisualElement Enfocado { get; private set; }

        /// <summary>Si la última petición de foco movió también el de teclado o mando (Focus()).</summary>
        internal bool FocoDeTeclado { get; private set; }

        /// <summary>
        /// Declara un botón. Etiqueta, valor y estado se leen cada vez que se reconstruye.
        /// </summary>
        /// <param name="seleccionado">Si devuelve true, el nodo lleva el estado Selected (la opción elegida de un menú).</param>
        /// <param name="enModal">Si el control sigue en el recorrido con el modo modal activo.</param>
        public void Boton(VisualElement elemento, Func<string> etiqueta, Action accion, Func<string> valor = null, Func<bool> visible = null,
            Func<bool> seleccionado = null, bool enModal = false) =>
            controles.Add(new Control
            {
                elemento = elemento, etiqueta = etiqueta, valor = valor, accion = accion, visible = visible,
                seleccionado = seleccionado, enModal = enModal,
            });

        /// <summary>
        /// Declara un marcador del mapa: texto sin acción (rol StaticText). Solo tiene nodo mientras es visible.
        /// La posición (dp del panel) ordena el recorrido; si no se da, se usa la del elemento.
        /// </summary>
        public void Marcador(VisualElement elemento, Func<string> etiqueta, Func<bool> visible, Func<Vector2> posicion = null) =>
            marcas.Add(new Marca { elemento = elemento, etiqueta = etiqueta, visible = visible, posicion = posicion });

        /// <summary>Etiquetas en el orden en que las recorre TalkBack (también sin lector, para las pruebas).</summary>
        public List<string> Recorrido()
        {
            var r = new List<string>();
            foreach (var (etiqueta, _) in RecorridoConEstado()) r.Add(etiqueta);
            return r;
        }

        /// <summary>El mismo recorrido, con si cada entrada lleva el estado seleccionado.</summary>
        public List<(string etiqueta, bool seleccionada)> RecorridoConEstado()
        {
            var r = new List<(string, bool)>();
            foreach (var c in ControlesEnRecorrido()) r.Add((c.etiqueta(), Seleccionado(c)));
            if (!Modal) foreach (var m in MarcasVisibles()) r.Add((m.etiqueta(), false));
            return r;
        }

        IEnumerable<Control> ControlesEnRecorrido()
        {
            foreach (var c in controles)
            {
                if (Modal && !c.enModal) continue;
                if (c.visible != null && !c.visible()) continue;
                yield return c;
            }
        }

        static bool Seleccionado(Control c) => c.seleccionado != null && c.seleccionado();

        /// <summary>
        /// Lleva el foco de TalkBack (SendLayoutChanged con su nodo) al control y, con el lector activo o si se
        /// llegó por teclado o mando, también el de teclado (Focus()): tras un toque no se ve el anillo de foco.
        /// Después se pide el anuncio, si lo hay, para que TalkBack no lo corte al leer el control enfocado.
        /// Con panel, se hace en el siguiente pase: un menú recién mostrado aún no tiene estilo ni marco resueltos,
        /// y el nodo se busca entonces en la jerarquía que esté activa.
        /// </summary>
        public void PedirFoco(VisualElement elemento, bool porNavegacion = false, string anuncio = null)
        {
            Enfocado = elemento;
            bool teclado = FocoDeTeclado = elemento != null && (porNavegacion || LectorActivo);
            if (elemento == null) { if (anuncio != null) Anunciar(anuncio); return; }
            if (raiz?.panel == null) { Enfocar(elemento, teclado, anuncio); return; }
            raiz.schedule.Execute(() => { if (Enfocado == elemento) Enfocar(elemento, teclado, anuncio); });
        }

        void Enfocar(VisualElement elemento, bool teclado, string anuncio)
        {
            if (teclado) elemento.Focus();
            if (LectorActivo && jerarquia != null && AssistiveSupport.activeHierarchy == jerarquia
                && nodosControles.TryGetValue(elemento, out var nodo) && jerarquia.ContainsNode(nodo))
                AssistiveSupport.notificationDispatcher?.SendLayoutChanged(nodo);
            if (anuncio != null) Anunciar(anuncio);
        }

        List<Marca> MarcasVisibles()
        {
            var visibles = new List<Marca>();
            foreach (var m in marcas) if (m.visible == null || m.visible()) visibles.Add(m);
            // De arriba abajo y, en la misma altura, de izquierda a derecha (orden estable)
            var orden = new Dictionary<Marca, int>();
            for (int i = 0; i < visibles.Count; i++) orden[visibles[i]] = i;
            visibles.Sort((a, b) =>
            {
                Vector2 pa = Posicion(a), pb = Posicion(b);
                int c = pa.y.CompareTo(pb.y);
                if (c == 0) c = pa.x.CompareTo(pb.x);
                return c != 0 ? c : orden[a].CompareTo(orden[b]);
            });
            return visibles;
        }

        static Vector2 Posicion(Marca m)
        {
            if (m.posicion != null) return m.posicion();
            var b = m.elemento.worldBound;
            return float.IsNaN(b.x) ? Vector2.zero : b.center;
        }

        /// <summary>Construye la jerarquía con los controles visibles y la activa si el lector está encendido.</summary>
        public void Reconstruir()
        {
            if (!LectorActivo) { Desactivar(); return; }
            jerarquia = new AccessibilityHierarchy();
            nodosMarcas.Clear();
            nodosControles.Clear();
            foreach (var c in ControlesEnRecorrido())
            {
                var nodo = jerarquia.AddNode(c.etiqueta());
                nodo.role = AccessibilityRole.Button;
                if (c.valor != null) nodo.value = c.valor();
                if (Seleccionado(c)) nodo.state = AccessibilityState.Selected;
                nodosControles[c.elemento] = nodo;
                var elemento = c.elemento;
                nodo.frameGetter = () => MarcoEnPantalla(elemento);
                var accion = c.accion;
                nodo.invoked += () => { accion(); return true; };
            }
            if (!Modal) AnadirMarcas(MarcasVisibles()); // con un menú abierto, el mapa no está en el recorrido
            AssistiveSupport.activeHierarchy = jerarquia;
        }

        /// <summary>
        /// Si han cambiado los marcadores visibles o su orden de recorrido (al mover o girar el mapa),
        /// quita sus nodos y añade los nuevos detrás de los botones, sin rehacer los de los botones.
        /// </summary>
        public void ActualizarMarcadores()
        {
            if (!LectorActivo || Modal || jerarquia == null || AssistiveSupport.activeHierarchy != jerarquia) return;
            var visibles = MarcasVisibles();
            if (MismoOrden(visibles)) return;
            foreach (var (_, nodo) in nodosMarcas) if (jerarquia.ContainsNode(nodo)) jerarquia.RemoveNode(nodo, true);
            nodosMarcas.Clear();
            AnadirMarcas(visibles);
            AssistiveSupport.notificationDispatcher?.SendLayoutChanged(null);
        }

        // Los mismos marcadores y en el mismo orden (de arriba abajo y de izquierda a derecha)
        bool MismoOrden(List<Marca> visibles)
        {
            if (visibles.Count != nodosMarcas.Count) return false;
            for (int i = 0; i < visibles.Count; i++) if (visibles[i] != nodosMarcas[i].marca) return false;
            return true;
        }

        void AnadirMarcas(List<Marca> visibles)
        {
            foreach (var m in visibles)
            {
                var nodo = jerarquia.AddNode(m.etiqueta());
                nodo.role = AccessibilityRole.StaticText;
                var elemento = m.elemento;
                nodo.frameGetter = () => MarcoEnPantalla(elemento);
                nodosMarcas.Add((m, nodo));
            }
        }

        /// <summary>Retira la jerarquía de esta pantalla de TalkBack (al destruirla).</summary>
        public void Desactivar()
        {
            if (jerarquia != null && AssistiveSupport.activeHierarchy == jerarquia) AssistiveSupport.activeHierarchy = null;
            jerarquia = null;
            nodosMarcas.Clear();
            nodosControles.Clear();
        }

        /// <summary>Lee en voz alta un aviso (por ejemplo, el nuevo tema).</summary>
        public static void Anunciar(string texto)
        {
            AnuncioPedido?.Invoke(texto);
            if (!LectorActivo) return;
            AssistiveSupport.notificationDispatcher?.SendAnnouncement(texto);
            Anunciado?.Invoke(texto);
        }

        /// <summary>Rectángulo del elemento en píxeles de pantalla, con el origen arriba a la izquierda.</summary>
        Rect MarcoEnPantalla(VisualElement e)
        {
            float anchoPanel = raiz.panel?.visualTree.layout.width ?? 0f;
            float escala = anchoPanel > 0f ? Screen.width / anchoPanel : 1f;
            var b = e.worldBound;
            return new Rect(b.x * escala, b.y * escala, b.width * escala, b.height * escala);
        }
    }
}
