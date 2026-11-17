using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Capa de UI Toolkit con los puntos de interés de OpenStreetMap (FR-4), por debajo de los marcadores
    /// de lugar. Tiene una vista por cada punto con categoría de las teselas cargadas (Sincronizar, al
    /// cambiar la lista del mapa) y cada fotograma (LateUpdate) coloca las que se ven: solo con la cámara
    /// cerca y dentro de la pantalla. Los que no tienen categoría, las paradas de autobús y las oficinas
    /// no se dibujan (ReglaPuntosInteres).
    /// </summary>
    public class CapaPuntosInteres
    {
        /// <summary>
        /// Con la cámara más lejos que esto (m) no se ve ningún punto: equivale al zoom 15 del prototipo
        /// (al arrancar la cámara está a 900 m, así que ya se ven).
        /// </summary>
        public const float DistanciaMaxima = 1200f;

        /// <summary>Margen fuera de la pantalla (dp) en el que un punto aún se coloca: lo que mide el círculo.</summary>
        public const float MargenDp = 24f;

        readonly IMapa mapa;
        readonly VisualElement contenedor;
        readonly Dictionary<PuntoInteres, PuntoInteresVista> vistas = new Dictionary<PuntoInteres, PuntoInteresVista>();
        readonly List<PuntoInteresVista> orden = new List<PuntoInteresVista>();

        public VisualElement Contenedor => contenedor;
        /// <summary>Vistas de los puntos con categoría, en el orden en que se pintan.</summary>
        public IReadOnlyList<PuntoInteresVista> Vistas => orden;

        /// <param name="contenedor">Elemento de la capa (antes de capa-marcadores, sin capturar toques).</param>
        public CapaPuntosInteres(VisualElement contenedor, IMapa mapa)
        {
            this.contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));
            this.mapa = mapa ?? throw new ArgumentNullException(nameof(mapa));
            contenedor.pickingMode = PickingMode.Ignore;
            Sincronizar();
        }

        /// <summary>
        /// Rehace las vistas con la lista actual del mapa: crea las de los puntos nuevos y quita las de los
        /// puntos de teselas liberadas. Devuelve true si algo ha cambiado.
        /// </summary>
        public bool Sincronizar()
        {
            var puntos = mapa.PuntosInteres ?? (IReadOnlyList<PuntoInteres>)Array.Empty<PuntoInteres>();
            var actuales = new HashSet<PuntoInteres>();
            bool cambio = false;
            foreach (var p in puntos)
            {
                if (p == null || !actuales.Add(p) || vistas.ContainsKey(p)) continue;
                var categoria = ReglaPuntosInteres.Categoria(p.Clase, p.Subclase);
                if (categoria == CategoriaPoi.Ninguna) continue; // sin categoría, bus_stop u office: no se dibuja
                var v = new PuntoInteresVista(p, categoria);
                vistas[p] = v;
                orden.Add(v);
                contenedor.Add(v);
                cambio = true;
            }
            for (int i = orden.Count - 1; i >= 0; i--)
            {
                var v = orden[i];
                if (actuales.Contains(v.Punto)) continue;
                // Tesela liberada: el punto desaparece
                vistas.Remove(v.Punto);
                orden.RemoveAt(i);
                EscalaFuente.Olvidar(v);
                v.RemoveFromHierarchy();
                cambio = true;
            }
            return cambio;
        }

        /// <summary>Recoloca los puntos con el tamaño de la pantalla actual.</summary>
        public void Actualizar(float escalaDp) => Actualizar(escalaDp, Screen.width, Screen.height);

        /// <summary>
        /// Recoloca los puntos. escalaDp convierte píxeles de pantalla en dp del panel. Con la cámara
        /// más lejos que DistanciaMaxima se ocultan todos sin proyectarlos.
        /// </summary>
        public void Actualizar(float escalaDp, float anchoPx, float altoPx)
        {
            if (escalaDp <= 0f || float.IsNaN(escalaDp) || float.IsInfinity(escalaDp)) return;
            float ancho = anchoPx * escalaDp, alto = altoPx * escalaDp;
            bool cerca = mapa.DistanciaCamara <= DistanciaMaxima;
            foreach (var v in orden)
            {
                if (!cerca) { v.Mostrar(false); continue; }
                var p = mapa.Proyectar(v.Punto.Posicion);
                var dp = new Vector2(p.X * escalaDp, p.Y * escalaDp);
                bool ver = p.Delante && !float.IsNaN(dp.x) && !float.IsNaN(dp.y)
                           && dp.x >= -MargenDp && dp.x <= ancho + MargenDp
                           && dp.y >= -MargenDp && dp.y <= alto + MargenDp;
                v.Mostrar(ver);
                if (ver) v.Colocar(dp);
            }
        }

        /// <summary>Número de puntos que se ven ahora.</summary>
        public int Visibles
        {
            get { int n = 0; foreach (var v in orden) if (v.Visible) n++; return n; }
        }
    }
}
