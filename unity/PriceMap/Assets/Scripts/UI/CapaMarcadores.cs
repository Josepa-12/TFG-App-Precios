using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Capa de UI Toolkit con un marcador por lugar, debajo de los botones. Cada fotograma (LateUpdate,
    /// con la cámara ya quieta) pide al mapa dónde cae cada lugar y mueve su marcador; oculta los que
    /// están detrás de la cámara o fuera de la pantalla y pinta encima los más cercanos.
    /// </summary>
    public class CapaMarcadores
    {
        /// <summary>
        /// Margen fuera de la pantalla (dp) en el que un marcador aún se coloca: lo que mide uno. Vale a
        /// los lados y abajo; arriba no, porque el marcador cuelga hacia arriba de su punto.
        /// </summary>
        public const float MargenDp = 50f;

        readonly IMapa mapa;
        readonly VisualElement contenedor;
        readonly List<MarcadorLugar> marcadores = new List<MarcadorLugar>();

        FiltroPrecios? filtro;
        bool filtroCambiado;

        public IReadOnlyList<MarcadorLugar> Marcadores => marcadores;
        public VisualElement Contenedor => contenedor;

        /// <summary>
        /// Filtro "Precios": solo se ven los lugares de las plantillas que incluye (y además en vista).
        /// Sin filtro (null), se ven todos.
        /// </summary>
        public FiltroPrecios? Filtro
        {
            get => filtro;
            set { if (filtro != value) { filtro = value; filtroCambiado = true; } }
        }

        /// <param name="contenedor">Elemento de la capa (primer hijo de la raíz, sin capturar toques).</param>
        /// <param name="lugares">Cada lugar con el resumen de su precio, calculado al crear la capa.</param>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public CapaMarcadores(VisualElement contenedor, IMapa mapa, IEnumerable<(Lugar lugar, ResumenLugar resumen)> lugares,
            Func<string, string> textos = null)
        {
            this.contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));
            this.mapa = mapa ?? throw new ArgumentNullException(nameof(mapa));
            contenedor.pickingMode = PickingMode.Ignore;
            if (lugares == null) return;
            foreach (var (lugar, resumen) in lugares)
            {
                if (lugar == null) continue;
                var m = new MarcadorLugar(lugar, resumen, marcadores.Count, textos);
                marcadores.Add(m);
                contenedor.Add(m);
            }
        }

        /// <summary>Recoloca los marcadores con el tamaño de la pantalla actual.</summary>
        public bool Actualizar(float escalaDp) => Actualizar(escalaDp, Screen.width, Screen.height);

        /// <summary>
        /// Recoloca los marcadores. escalaDp convierte píxeles de pantalla en dp del panel.
        /// Devuelve true si ha cambiado el conjunto de marcadores visibles (para TalkBack).
        /// </summary>
        public bool Actualizar(float escalaDp, float anchoPx, float altoPx)
        {
            if (escalaDp <= 0f || float.IsNaN(escalaDp) || float.IsInfinity(escalaDp)) return false;
            float ancho = anchoPx * escalaDp, alto = altoPx * escalaDp;
            // Un cambio de filtro cuenta como cambio aunque no cambie ningún visible (TalkBack lo rehace)
            bool cambio = filtroCambiado;
            filtroCambiado = false;
            foreach (var m in marcadores)
            {
                // Fuera del filtro no hace falta proyectarlo: se oculta igual
                bool incluido = filtro == null || ReglaFiltro.Incluye(filtro.Value, m.Lugar.Plantilla);
                var p = incluido ? mapa.Proyectar(m.Lugar.Posicion) : PuntoPantalla.Ninguno;
                var dp = new Vector2(p.X * escalaDp, p.Y * escalaDp);
                bool ver = incluido && p.Delante && !float.IsNaN(dp.x) && !float.IsNaN(dp.y)
                           && dp.x >= -MargenDp && dp.x <= ancho + MargenDp
                           // El marcador cuelga hacia arriba de su punto: por encima del borde superior ya no asoma
                           && dp.y >= 0f && dp.y <= alto + MargenDp;
                if (ver != m.Visible) { m.Mostrar(ver); cambio = true; }
                if (ver) m.Colocar(dp, p.Profundidad);
            }
            if (!EnOrden()) contenedor.Sort(Comparar);
            return cambio;
        }

        /// <summary>True si los visibles ya van del más lejano al más cercano (el último se pinta encima).</summary>
        bool EnOrden()
        {
            // Los ocultos pueden quedar entre los visibles: no se pintan, así que no obligan a reordenar
            float anterior = float.PositiveInfinity;
            foreach (var hijo in contenedor.Children())
            {
                if (!(hijo is MarcadorLugar m) || !m.Visible) continue;
                if (m.Profundidad > anterior) return false;
                anterior = m.Profundidad;
            }
            return true;
        }

        /// <summary>Más lejanos primero; los ocultos, al principio; a igual distancia, por orden de llegada.</summary>
        static int Comparar(VisualElement a, VisualElement b)
        {
            var ma = a as MarcadorLugar; var mb = b as MarcadorLugar;
            if (ma == null || mb == null) return (ma == null ? 0 : 1) - (mb == null ? 0 : 1);
            if (ma.Visible != mb.Visible) return ma.Visible ? 1 : -1;
            if (ma.Visible)
            {
                int c = mb.Profundidad.CompareTo(ma.Profundidad);
                if (c != 0) return c;
            }
            return ma.Indice.CompareTo(mb.Indice);
        }

        /// <summary>Vuelve a dibujar los vectores (al cambiar de tema cambia el color del €).</summary>
        public void Repintar()
        {
            foreach (var m in marcadores) m.Repintar();
        }
    }
}
