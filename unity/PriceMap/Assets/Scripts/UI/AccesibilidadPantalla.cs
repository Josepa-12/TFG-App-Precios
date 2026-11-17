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
            public Func<bool> visible;
            public Action accion;
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

        public AccesibilidadPantalla(VisualElement raiz) => this.raiz = raiz;

        public static bool LectorActivo => AssistiveSupport.isScreenReaderEnabled;

        /// <summary>Declara un botón. Etiqueta y valor se leen cada vez que se reconstruye.</summary>
        public void Boton(VisualElement elemento, Func<string> etiqueta, Action accion, Func<string> valor = null, Func<bool> visible = null) =>
            controles.Add(new Control { elemento = elemento, etiqueta = etiqueta, valor = valor, accion = accion, visible = visible });

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
            foreach (var c in controles) if (c.visible == null || c.visible()) r.Add(c.etiqueta());
            foreach (var m in MarcasVisibles()) r.Add(m.etiqueta());
            return r;
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
            foreach (var c in controles)
            {
                if (c.visible != null && !c.visible()) continue;
                var nodo = jerarquia.AddNode(c.etiqueta());
                nodo.role = AccessibilityRole.Button;
                if (c.valor != null) nodo.value = c.valor();
                var elemento = c.elemento;
                nodo.frameGetter = () => MarcoEnPantalla(elemento);
                var accion = c.accion;
                nodo.invoked += () => { accion(); return true; };
            }
            AnadirMarcas(MarcasVisibles());
            AssistiveSupport.activeHierarchy = jerarquia;
        }

        /// <summary>
        /// Si han cambiado los marcadores visibles o su orden de recorrido (al mover o girar el mapa),
        /// quita sus nodos y añade los nuevos detrás de los botones, sin rehacer los de los botones.
        /// </summary>
        public void ActualizarMarcadores()
        {
            if (!LectorActivo || jerarquia == null || AssistiveSupport.activeHierarchy != jerarquia) return;
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
        }

        /// <summary>Lee en voz alta un aviso (por ejemplo, el nuevo tema).</summary>
        public static void Anunciar(string texto)
        {
            if (LectorActivo) AssistiveSupport.notificationDispatcher?.SendAnnouncement(texto);
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
