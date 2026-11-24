using System.Collections.Generic;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Escucha mover, soltar y cancelar de un dedo desde que baja en una hoja, vaya donde vaya. Sin captura los eventos
    /// entran por la raíz del panel; con el puntero capturado Unity 6.3 solo se los da al elemento que lo captura (ni a la
    /// raíz ni a sus padres). Lo puede capturar la hoja al arrastrar o, dentro de ella, un botón, la lista o el texto del
    /// campo: por eso se escucha en toda la cadena, de donde bajó el dedo a la raíz, y cada evento se atiende una vez.
    /// </summary>
    internal sealed class EscuchaGesto
    {
        readonly EventCallback<PointerMoveEvent> mover;
        readonly EventCallback<PointerUpEvent> soltar;
        readonly EventCallback<PointerCancelEvent> cancelar;
        readonly List<VisualElement> cadena = new List<VisualElement>();
        IPanel panel;

        public EscuchaGesto(EventCallback<PointerMoveEvent> mover, EventCallback<PointerUpEvent> soltar, EventCallback<PointerCancelEvent> cancelar)
        {
            this.mover = mover;
            this.soltar = soltar;
            this.cancelar = cancelar;
        }

        /// <summary>Empieza a escuchar; hoja: la que seguirá el gesto, dentro de la que bajó el dedo.</summary>
        /// <returns>False fuera de un panel: no hay nada que escuchar.</returns>
        public bool Empezar(VisualElement hoja, VisualElement objetivo)
        {
            Terminar();
            panel = hoja.panel;
            if (panel == null) return false;
            for (var e = objetivo != null && hoja.Contains(objetivo) ? objetivo : hoja; e != null; e = e.hierarchy.parent) cadena.Add(e);
            foreach (var e in cadena)
            {
                e.RegisterCallback<PointerMoveEvent>(AlMover, TrickleDown.TrickleDown);
                e.RegisterCallback<PointerUpEvent>(AlSoltar, TrickleDown.TrickleDown);
                e.RegisterCallback<PointerCancelEvent>(AlCancelar, TrickleDown.TrickleDown);
            }
            return true;
        }

        public void Terminar()
        {
            foreach (var e in cadena)
            {
                e.UnregisterCallback<PointerMoveEvent>(AlMover, TrickleDown.TrickleDown);
                e.UnregisterCallback<PointerUpEvent>(AlSoltar, TrickleDown.TrickleDown);
                e.UnregisterCallback<PointerCancelEvent>(AlCancelar, TrickleDown.TrickleDown);
            }
            cadena.Clear();
            panel = null;
        }

        void AlMover(PointerMoveEvent e) { if (Toca(e.currentTarget, e.pointerId)) mover(e); }
        void AlSoltar(PointerUpEvent e) { if (Toca(e.currentTarget, e.pointerId)) soltar(e); }
        void AlCancelar(PointerCancelEvent e) { if (Toca(e.currentTarget, e.pointerId)) cancelar(e); }

        // Una sola vez por evento: en quien tiene el puntero o, si no lo tiene nadie, en la raíz
        bool Toca(IEventHandler actual, int puntero) =>
            panel != null && actual == (panel.GetCapturingElement(puntero) ?? panel.visualTree);
    }
}
