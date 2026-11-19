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
    /// Recorrido: primero los botones y después las marcas visibles por grupos (los lugares de la app y luego los
    /// puntos de interés), cada grupo de arriba abajo y de izquierda a derecha.
    /// </summary>
    public class AccesibilidadPantalla
    {
        /// <summary>Grupo de los marcadores de los lugares de la app: van primero.</summary>
        public const int GrupoLugares = 0;
        /// <summary>Grupo de los puntos de interés de OpenStreetMap: siempre después de los lugares.</summary>
        public const int GrupoPuntos = 1;

        class Control
        {
            public VisualElement elemento;
            public Func<string> etiqueta, valor;
            public Func<bool> visible, seleccionado;
            public Action accion;
            public bool enModal;
            public bool esTexto; // texto sin acción (rol StaticText), p. ej. el de una ventana modal
            public object grupo; // controles dinámicos que se dan de baja juntos (QuitarControles), p. ej. los de la ficha
            public Action alEnfocar; // cuando TalkBack pone el foco en su nodo (p. ej. desplazar la ficha hasta él)
        }

        class Marca
        {
            public VisualElement elemento;
            public Func<string> etiqueta;
            public Func<bool> visible;
            public Func<Vector2> posicion;
            public int grupo;
            public Func<Rect> marco; // zona de TalkBack (dp del panel); null: el worldBound del elemento
            public Action accion;    // doble toque de TalkBack (rol Button); null: texto sin acción
        }

        /// <summary>
        /// Con los mismos marcadores visibles y solo otro orden (al mover el mapa), los nodos se rehacen como mucho
        /// una vez cada este tiempo (s): rehacerlos resitúa el foco de TalkBack.
        /// </summary>
        public const float IntervaloReordenar = 1f;

        /// <summary>Reloj en segundos con el que se espacian las reordenaciones; las pruebas lo sustituyen.</summary>
        internal Func<float> Reloj = () => Time.realtimeSinceStartup;

        /// <summary>Veces que se han rehecho los nodos de los marcadores en ActualizarMarcadores (para las pruebas).</summary>
        internal int ReconstruccionesMarcas { get; private set; }

        float ultimaReordenacion = float.NegativeInfinity;
        bool avisoPendiente; // se quitaron nodos (QuitarMarcador): un solo SendLayoutChanged por tanda

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
        /// <param name="grupo">Si no es null, QuitarControles(grupo) lo da de baja junto con los demás de su grupo.</param>
        /// <param name="alEnfocar">Lo que se hace cuando TalkBack pone el foco en su nodo.</param>
        public void Boton(VisualElement elemento, Func<string> etiqueta, Action accion, Func<string> valor = null, Func<bool> visible = null,
            Func<bool> seleccionado = null, bool enModal = false, object grupo = null, Action alEnfocar = null) =>
            controles.Add(new Control
            {
                elemento = elemento, etiqueta = etiqueta, valor = valor, accion = accion, visible = visible,
                seleccionado = seleccionado, enModal = enModal, grupo = grupo, alEnfocar = alEnfocar,
            });

        /// <summary>
        /// Declara un texto entre los controles (rol StaticText, sin acción), en su orden de declaración:
        /// p. ej. el texto de una ventana modal, antes de sus botones.
        /// </summary>
        /// <param name="grupo">Si no es null, QuitarControles(grupo) lo da de baja junto con los demás de su grupo.</param>
        /// <param name="alEnfocar">Lo que se hace cuando TalkBack pone el foco en su nodo (p. ej. desplazar hasta él).</param>
        public void Texto(VisualElement elemento, Func<string> etiqueta, Func<bool> visible = null, bool enModal = false,
            object grupo = null, Action alEnfocar = null) =>
            controles.Add(new Control
            {
                elemento = elemento, etiqueta = etiqueta, visible = visible, enModal = enModal, esTexto = true,
                grupo = grupo, alEnfocar = alEnfocar,
            });

        /// <summary>
        /// Da de baja los controles de ese grupo (p. ej. los nodos de la ficha del lugar anterior). Los que tenían
        /// nodo en la jerarquía activa se quitan ya, para que ninguna entrada de TalkBack apunte a un elemento que
        /// ya no existe. No reconstruye: quien llama decide cuándo (Modal o Reconstruir).
        /// </summary>
        public void QuitarControles(object grupo)
        {
            if (grupo == null) return;
            foreach (var c in controles)
            {
                if (c.grupo != grupo || !nodosControles.TryGetValue(c.elemento, out var nodo)) continue;
                if (jerarquia != null && jerarquia.ContainsNode(nodo)) jerarquia.RemoveNode(nodo, true);
                nodosControles.Remove(c.elemento);
            }
            controles.RemoveAll(c => c.grupo == grupo);
        }

        /// <summary>Número de controles declarados (visibles o no), para las pruebas.</summary>
        internal int Controles => controles.Count;

        /// <summary>
        /// Declara un marcador del mapa. Solo tiene nodo mientras es visible. Sin acción es un texto (rol
        /// StaticText); con ella, un botón que el doble toque de TalkBack activa (p. ej. abrir la ficha del lugar).
        /// El grupo ordena primero (GrupoLugares antes que GrupoPuntos) y, dentro de él, la posición (dp del panel);
        /// si no se da la posición, se usa la del elemento. marco (dp del panel) sustituye al worldBound del elemento
        /// como zona de TalkBack (p. ej. para llegar a 48 dp).
        /// </summary>
        public void Marcador(VisualElement elemento, Func<string> etiqueta, Func<bool> visible, Func<Vector2> posicion = null,
            int grupo = GrupoLugares, Func<Rect> marco = null, Action alActivar = null) =>
            marcas.Add(new Marca
            {
                elemento = elemento, etiqueta = etiqueta, visible = visible, posicion = posicion, grupo = grupo, marco = marco,
                accion = alActivar,
            });

        /// <summary>
        /// Da de baja las marcas de ese elemento (su vista se ha destruido). Si tenía nodo en la jerarquía activa,
        /// se quita ya, para que ninguna entrada de TalkBack apunte a una vista que ya no existe. El aviso a TalkBack
        /// se manda una sola vez para toda la tanda, en el siguiente ActualizarMarcadores.
        /// </summary>
        public void QuitarMarcador(VisualElement elemento)
        {
            if (elemento == null) return;
            marcas.RemoveAll(m => m.elemento == elemento);
            for (int i = nodosMarcas.Count - 1; i >= 0; i--)
            {
                if (nodosMarcas[i].marca.elemento != elemento) continue;
                var nodo = nodosMarcas[i].nodo;
                if (jerarquia != null && jerarquia.ContainsNode(nodo)) jerarquia.RemoveNode(nodo, true);
                nodosMarcas.RemoveAt(i);
                avisoPendiente = true;
            }
        }

        /// <summary>Número de marcas declaradas (visibles o no), para las pruebas.</summary>
        internal int Marcas => marcas.Count;

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

        /// <summary>
        /// Solo las marcas del recorrido (sin los botones), en orden, con su grupo (GrupoLugares o GrupoPuntos).
        /// Vacío en modo modal.
        /// </summary>
        public List<(string etiqueta, int grupo)> RecorridoDeMarcas()
        {
            var r = new List<(string, int)>();
            if (!Modal) foreach (var m in MarcasVisibles()) r.Add((m.etiqueta(), m.grupo));
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
            ultimoFoco = elemento;
            bool teclado = FocoDeTeclado = elemento != null && (porNavegacion || LectorActivo);
            if (elemento == null) { if (anuncio != null) Anunciar(anuncio); return; }
            if (raiz?.panel == null) { Enfocar(elemento, teclado, anuncio); return; }
            raiz.schedule.Execute(() => { if (Enfocado == elemento) Enfocar(elemento, teclado, anuncio); });
        }

        void Enfocar(VisualElement elemento, bool teclado, string anuncio)
        {
            if (teclado) elemento.Focus();
            if (LectorActivo && jerarquia != null && AssistiveSupport.activeHierarchy == jerarquia)
            {
                var nodo = NodoDe(elemento);
                if (nodo != null && jerarquia.ContainsNode(nodo))
                {
                    UltimoNodoEnfocado = nodo;
                    AssistiveSupport.notificationDispatcher?.SendLayoutChanged(nodo);
                }
            }
            if (anuncio != null) Anunciar(anuncio);
        }

        /// <summary>
        /// Tras rehacer la jerarquía sin que el usuario haya hecho nada (p. ej. llegan las direcciones del buscador):
        /// devuelve el foco de TalkBack al elemento donde estaba, si sigue en el recorrido, sin anunciarlo de nuevo y
        /// sin foco de teclado. Devuelve si lo ha hecho.
        /// </summary>
        public bool RecuperarFoco()
        {
            var nodo = NodoDe(ultimoFoco);
            if (nodo == null || jerarquia == null || !jerarquia.ContainsNode(nodo)) return false;
            if (LectorActivo && AssistiveSupport.activeHierarchy == jerarquia)
            {
                UltimoNodoEnfocado = nodo;
                AssistiveSupport.notificationDispatcher?.SendLayoutChanged(nodo);
            }
            return true;
        }

        // Último foco, pedido (PedirFoco) o puesto por el usuario con TalkBack (focusChanged). Aparte de Enfocado, para
        // que un foco que mueva el sistema no cancele un PedirFoco pendiente ni su anuncio.
        VisualElement ultimoFoco;

        /// <summary>Último foco (pedido o puesto por el usuario con TalkBack): el que devolvería RecuperarFoco.</summary>
        public VisualElement UltimoFoco => ultimoFoco;

        /// <summary>Último nodo al que Enfocar llevó el foco de TalkBack (SendLayoutChanged), para las pruebas.</summary>
        internal AccessibilityNode UltimoNodoEnfocado { get; private set; }

        /// <summary>Nodo del elemento en la jerarquía activa: un control o un marcador (p. ej. al cerrar la ficha).</summary>
        internal AccessibilityNode NodoDe(VisualElement elemento)
        {
            if (elemento == null) return null;
            if (nodosControles.TryGetValue(elemento, out var nodo)) return nodo;
            foreach (var (marca, n) in nodosMarcas) if (marca.elemento == elemento) return n;
            return null;
        }

        List<Marca> MarcasVisibles()
        {
            var visibles = new List<Marca>();
            foreach (var m in marcas) if (m.visible == null || m.visible()) visibles.Add(m);
            // Primero los lugares y después los puntos; en cada grupo, de arriba abajo y, en la misma altura,
            // de izquierda a derecha (orden estable)
            var orden = new Dictionary<Marca, int>();
            for (int i = 0; i < visibles.Count; i++) orden[visibles[i]] = i;
            visibles.Sort((a, b) =>
            {
                int c = a.grupo.CompareTo(b.grupo);
                if (c != 0) return c;
                Vector2 pa = Posicion(a), pb = Posicion(b);
                c = pa.y.CompareTo(pb.y);
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

        /// <summary>
        /// Vuelve a leer la etiqueta de ese control y la pone en su nodo ya existente, sin rehacer la jerarquía ni
        /// mover el foco de TalkBack. Si el control no tiene nodo (sin lector, oculto o en modal), no hace nada.
        /// </summary>
        internal void RefrescarEtiqueta(VisualElement elemento)
        {
            if (elemento == null || !nodosControles.TryGetValue(elemento, out var nodo)) return;
            var control = controles.Find(c => c.elemento == elemento);
            if (control != null) nodo.label = control.etiqueta();
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
                nodo.role = c.esTexto ? AccessibilityRole.StaticText : AccessibilityRole.Button;
                if (c.valor != null) nodo.value = c.valor();
                if (Seleccionado(c)) nodo.state = AccessibilityState.Selected;
                nodosControles[c.elemento] = nodo;
                var elemento = c.elemento;
                nodo.frameGetter = () => MarcoEnPantalla(elemento);
                var accion = c.accion;
                if (accion != null) nodo.invoked += () => { accion(); return true; };
                var alEnfocar = c.alEnfocar;
                nodo.focusChanged += (_, enfocado) =>
                {
                    if (!enfocado) return;
                    ultimoFoco = elemento; // donde está el usuario: RecuperarFoco lo devuelve tras otra reconstrucción
                    alEnfocar?.Invoke();
                };
            }
            if (!Modal) AnadirMarcas(MarcasVisibles()); // con un menú abierto, el mapa no está en el recorrido
            AssistiveSupport.activeHierarchy = jerarquia;
            avisoPendiente = false; // jerarquía nueva: TalkBack ya la recibe entera
        }

        /// <summary>
        /// Si han cambiado los marcadores visibles, quita sus nodos y añade los nuevos detrás de los botones, sin
        /// rehacer los de los botones. Si solo ha cambiado el orden (al mover o girar el mapa), lo mismo, pero como
        /// mucho una vez cada IntervaloReordenar. Manda un solo aviso a TalkBack, también por las bajas de QuitarMarcador.
        /// </summary>
        public void ActualizarMarcadores()
        {
            if (!LectorActivo || Modal || jerarquia == null || AssistiveSupport.activeHierarchy != jerarquia) return;
            var visibles = MarcasVisibles();
            bool rehacer = false;
            if (!MismoOrden(visibles))
            {
                float ahora = Reloj();
                if (!MismoConjunto(visibles)) rehacer = true;
                else if (ahora - ultimaReordenacion >= IntervaloReordenar) { rehacer = true; ultimaReordenacion = ahora; }
            }
            if (rehacer)
            {
                foreach (var (_, nodo) in nodosMarcas) if (jerarquia.ContainsNode(nodo)) jerarquia.RemoveNode(nodo, true);
                nodosMarcas.Clear();
                AnadirMarcas(visibles);
                ReconstruccionesMarcas++;
            }
            if (rehacer || avisoPendiente)
            {
                avisoPendiente = false;
                AssistiveSupport.notificationDispatcher?.SendLayoutChanged(null);
            }
        }

        // Los mismos marcadores, en cualquier orden
        bool MismoConjunto(List<Marca> visibles)
        {
            if (visibles.Count != nodosMarcas.Count) return false;
            var actuales = new HashSet<Marca>();
            foreach (var (m, _) in nodosMarcas) actuales.Add(m);
            foreach (var m in visibles) if (!actuales.Contains(m)) return false;
            return true;
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
                var accion = m.accion;
                nodo.role = accion != null ? AccessibilityRole.Button : AccessibilityRole.StaticText;
                if (accion != null) nodo.invoked += () => { accion(); return true; };
                var elemento = m.elemento;
                var marco = m.marco;
                nodo.frameGetter = marco != null ? () => MarcoEnPantalla(marco()) : (Func<Rect>)(() => MarcoEnPantalla(elemento));
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
            avisoPendiente = false;
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
        Rect MarcoEnPantalla(VisualElement e) => MarcoEnPantalla(e.worldBound);

        /// <summary>Rectángulo en dp del panel pasado a píxeles de pantalla.</summary>
        Rect MarcoEnPantalla(Rect b)
        {
            float anchoPanel = raiz?.panel?.visualTree.layout.width ?? 0f;
            float escala = anchoPanel > 0f ? Screen.width / anchoPanel : 1f;
            return new Rect(b.x * escala, b.y * escala, b.width * escala, b.height * escala);
        }
    }
}
