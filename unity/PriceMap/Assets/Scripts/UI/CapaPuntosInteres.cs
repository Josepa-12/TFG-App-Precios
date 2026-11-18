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

        /// <summary>
        /// Se lanza al crear la vista de un punto (en Sincronizar). Las que crea el constructor ya están en Vistas
        /// antes de que nadie pueda suscribirse: quien escuche debe dar de alta también esas.
        /// </summary>
        public event Action<PuntoInteresVista> VistaCreada;
        /// <summary>Se lanza al quitar la vista de un punto (su tesela se ha liberado), ya fuera del árbol.</summary>
        public event Action<PuntoInteresVista> VistaQuitada;

        /// <param name="contenedor">Elemento de la capa (antes de capa-marcadores, sin capturar toques).</param>
        public CapaPuntosInteres(VisualElement contenedor, IMapa mapa)
        {
            this.contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));
            this.mapa = mapa ?? throw new ArgumentNullException(nameof(mapa));
            contenedor.pickingMode = PickingMode.Ignore;
            nombreLugar = NombreLugar; // un solo delegado para todos los fotogramas
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
                VistaCreada?.Invoke(v);
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
                VistaQuitada?.Invoke(v);
            }
            return cambio;
        }

        /// <summary>Recoloca los puntos con el tamaño de la pantalla actual.</summary>
        public void Actualizar(float escalaDp, IReadOnlyList<Lugar> lugaresVisibles = null, IReadOnlyList<Rect> ocupadas = null) =>
            Actualizar(escalaDp, Screen.width, Screen.height, lugaresVisibles, ocupadas);

        /// <summary>
        /// Recoloca los puntos. escalaDp convierte píxeles de pantalla en dp del panel. Con la cámara
        /// más lejos que DistanciaMaxima se ocultan todos sin proyectarlos. De los que caben en pantalla:
        /// se ocultan los que repiten un lugar de lugaresVisibles (cuyo marcador se ve ahora,
        /// ReglaPuntosInteres.EsRepetido) y, por orden de rank (y de id si empatan), los que pisan a uno ya
        /// mostrado o una zona ocupada (icono y nombre, PuntoInteresVista.Caja). Las zonas ocupadas (dp del
        /// panel) son las de los marcadores visibles y los controles: nada se pinta encima de un marcador.
        /// </summary>
        public void Actualizar(float escalaDp, float anchoPx, float altoPx, IReadOnlyList<Lugar> lugaresVisibles = null,
            IReadOnlyList<Rect> ocupadas = null)
        {
            if (escalaDp <= 0f || float.IsNaN(escalaDp) || float.IsInfinity(escalaDp)) return;
            float ancho = anchoPx * escalaDp, alto = altoPx * escalaDp;
            bool cerca = mapa.DistanciaCamara <= DistanciaMaxima;
            OcultosPorRepetido = 0;
            OcultosPorSolape = 0;
            candidatos.Clear();
            foreach (var v in orden)
            {
                if (!cerca) { v.Mostrar(false); continue; }
                var p = mapa.Proyectar(v.Punto.Posicion);
                var dp = new Vector2(p.X * escalaDp, p.Y * escalaDp);
                bool enPantalla = p.Delante && !float.IsNaN(dp.x) && !float.IsNaN(dp.y)
                                  && dp.x >= -MargenDp && dp.x <= ancho + MargenDp
                                  && dp.y >= -MargenDp && dp.y <= alto + MargenDp;
                if (!enPantalla) { v.Mostrar(false); continue; }
                if (lugaresVisibles != null && lugaresVisibles.Count > 0
                    && ReglaPuntosInteres.EsRepetidoNormalizado(v.NombreNormalizado, v.Punto.Posicion, lugaresVisibles, nombreLugar))
                {
                    v.Mostrar(false); // ya sale como lugar de la app: solo se ve su marcador
                    OcultosPorRepetido++;
                    continue;
                }
                v.Candidata = dp;
                candidatos.Add(v);
            }

            // Solapes: primero los más importantes; uno que pisa a otro ya mostrado se oculta entero
            candidatos.Sort(PorImportancia);
            cajasMostradas.Clear();
            foreach (var v in candidatos)
            {
                var caja = v.Caja(v.Candidata);
                // Los marcadores y los controles cuentan como ya mostrados
                bool pisa = CapaMarcadores.Pisa(caja, cajasMostradas) || (ocupadas != null && CapaMarcadores.Pisa(caja, ocupadas));
                if (pisa) { v.Mostrar(false); OcultosPorSolape++; continue; }
                cajasMostradas.Add(caja);
                v.Mostrar(true);
                v.Colocar(v.Candidata);
            }
            candidatos.Clear();
        }

        readonly List<PuntoInteresVista> candidatos = new List<PuntoInteresVista>();
        readonly List<Rect> cajasMostradas = new List<Rect>();
        static readonly Comparison<PuntoInteresVista> PorImportancia = CompararImportancia;

        // Nombre normalizado de cada lugar, calculado una vez (los lugares no cambian de nombre)
        readonly Dictionary<Lugar, string> nombresLugares = new Dictionary<Lugar, string>();
        Func<Lugar, string> nombreLugar;

        string NombreLugar(Lugar l)
        {
            if (!nombresLugares.TryGetValue(l, out var n)) nombresLugares[l] = n = ReglaPuntosInteres.NombreNormalizado(l.Nombre);
            return n;
        }

        /// <summary>Olvida las medidas de los nombres (al cambiar la escala de fuente): se vuelven a estimar o medir.</summary>
        public void OlvidarMedidas()
        {
            foreach (var v in orden) v.OlvidarMedida();
        }

        /// <summary>Menor rank primero; si empatan, menor id; después, nombre y posición (orden estable entre fotogramas).</summary>
        internal static int CompararImportancia(PuntoInteresVista a, PuntoInteresVista b)
        {
            int c = a.Punto.Rank.CompareTo(b.Punto.Rank);
            if (c != 0) return c;
            c = a.Punto.Id.CompareTo(b.Punto.Id);
            if (c != 0) return c;
            c = string.CompareOrdinal(a.Punto.Nombre, b.Punto.Nombre);
            if (c != 0) return c;
            c = a.Punto.Posicion.Latitud.CompareTo(b.Punto.Posicion.Latitud);
            return c != 0 ? c : a.Punto.Posicion.Longitud.CompareTo(b.Punto.Posicion.Longitud);
        }

        /// <summary>Puntos en pantalla ocultados en la última actualización por repetir un lugar visible.</summary>
        public int OcultosPorRepetido { get; private set; }
        /// <summary>Puntos en pantalla ocultados en la última actualización por pisar a otro más importante.</summary>
        public int OcultosPorSolape { get; private set; }

        /// <summary>Número de puntos que se ven ahora.</summary>
        public int Visibles
        {
            get { int n = 0; foreach (var v in orden) if (v.Visible) n++; return n; }
        }
    }
}
