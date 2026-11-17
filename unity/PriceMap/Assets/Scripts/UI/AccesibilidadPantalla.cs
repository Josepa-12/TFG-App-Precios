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

        readonly List<Control> controles = new List<Control>();
        readonly VisualElement raiz;
        AccessibilityHierarchy jerarquia;

        public AccesibilidadPantalla(VisualElement raiz) => this.raiz = raiz;

        public static bool LectorActivo => AssistiveSupport.isScreenReaderEnabled;

        /// <summary>Declara un botón. Etiqueta y valor se leen cada vez que se reconstruye.</summary>
        public void Boton(VisualElement elemento, Func<string> etiqueta, Action accion, Func<string> valor = null, Func<bool> visible = null) =>
            controles.Add(new Control { elemento = elemento, etiqueta = etiqueta, valor = valor, accion = accion, visible = visible });

        /// <summary>Construye la jerarquía con los controles visibles y la activa si el lector está encendido.</summary>
        public void Reconstruir()
        {
            if (!LectorActivo) { if (jerarquia != null && AssistiveSupport.activeHierarchy == jerarquia) AssistiveSupport.activeHierarchy = null; return; }
            jerarquia = new AccessibilityHierarchy();
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
            AssistiveSupport.activeHierarchy = jerarquia;
        }

        /// <summary>Retira la jerarquía de esta pantalla de TalkBack (al destruirla).</summary>
        public void Desactivar()
        {
            if (jerarquia != null && AssistiveSupport.activeHierarchy == jerarquia) AssistiveSupport.activeHierarchy = null;
            jerarquia = null;
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
