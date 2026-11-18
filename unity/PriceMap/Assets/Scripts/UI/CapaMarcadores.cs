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
    /// están detrás de la cámara o fuera de la pantalla, y de dos que se pisan deja solo uno (el que tiene precio
    /// y, entre iguales, el más cercano).
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
        /// Filtro "Precios": solo se ven los lugares de las plantillas que incluye (y además en vista), tengan o no
        /// precio. Sin filtro (null), se ven todos.
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
        public bool Actualizar(float escalaDp, IReadOnlyList<Rect> ocupadas = null) =>
            Actualizar(escalaDp, Screen.width, Screen.height, ocupadas);

        /// <summary>
        /// Recoloca los marcadores. escalaDp convierte píxeles de pantalla en dp del panel. Solo se ven los
        /// lugares de las plantillas del filtro. De los que caben en pantalla, en el orden de Prioridad (el de la
        /// ficha abierta, los que tienen precio, el que ya se veía, el más cercano), se oculta el que pisa (ZonaDp) a
        /// uno ya mostrado o a una de las zonas ocupadas (los controles, en dp del panel): nunca se ven dos que se
        /// pisan. El fijado (Fijado) no se oculta por solapes.
        /// Devuelve true si ha cambiado el conjunto de marcadores visibles (para TalkBack).
        /// </summary>
        public bool Actualizar(float escalaDp, float anchoPx, float altoPx, IReadOnlyList<Rect> ocupadas = null)
        {
            if (escalaDp <= 0f || float.IsNaN(escalaDp) || float.IsInfinity(escalaDp)) return false;
            float ancho = anchoPx * escalaDp, alto = altoPx * escalaDp;
            // Un cambio de filtro cuenta como cambio aunque no cambie ningún visible (TalkBack lo rehace)
            bool cambio = filtroCambiado;
            filtroCambiado = false;
            OcultosPorSolape = 0;
            candidatos.Clear();
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
                if (!ver)
                {
                    if (m.Visible) { m.Mostrar(false); cambio = true; }
                    continue;
                }
                m.Colocar(dp, p.Profundidad); // ZonaDp ya es la de este fotograma
                candidatos.Add(m);
            }

            // Solapes: por prioridad (Prioridad); uno que pisa a otro ya mostrado o un control se oculta. El de la ficha
            // abierta va primero y no lo ocultan ni los controles
            prioridad.Fijado = Fijado;
            candidatos.Sort(prioridad);
            zonasVisibles.Clear();
            foreach (var m in candidatos)
            {
                var zona = m.ZonaDp;
                bool pisa = m != Fijado && (Pisa(zona, zonasVisibles) || (ocupadas != null && Pisa(zona, ocupadas)));
                if (pisa) OcultosPorSolape++;
                else zonasVisibles.Add(zona);
                if (m.Visible == pisa) { m.Mostrar(!pisa); cambio = true; }
            }
            candidatos.Clear();
            if (!EnOrden()) contenedor.Sort(Comparar);
            return cambio;
        }

        /// <summary>True si la zona pisa alguna de las dadas (las vacías, sin medida, no cuentan).</summary>
        internal static bool Pisa(Rect zona, IReadOnlyList<Rect> otras)
        {
            for (int i = 0; i < otras.Count; i++)
            {
                var o = otras[i];
                if (!(o.width > 0f) || !(o.height > 0f)) continue;
                if (zona.Overlaps(o)) return true;
            }
            return false;
        }

        readonly List<MarcadorLugar> candidatos = new List<MarcadorLugar>();
        readonly List<Rect> zonasVisibles = new List<Rect>();
        readonly Prioridad prioridad = new Prioridad(); // un solo comparador: ordenar no asigna memoria

        /// <summary>
        /// Marcador del lugar de la ficha abierta (lo pone PantallaMapa): nunca se oculta por solapes ni por controles
        /// y gana a cualquier otro que lo pise. Null sin ficha.
        /// </summary>
        public MarcadorLugar Fijado { get; set; }

        /// <summary>
        /// Orden de los solapes: el fijado; después los que tienen precio; entre iguales, el que ya se veía en el
        /// fotograma anterior (histéresis: al girar no se alternan dos que se pisan); después los más cercanos a la
        /// cámara y, a igual distancia, por orden de llegada (estable entre fotogramas).
        /// </summary>
        sealed class Prioridad : IComparer<MarcadorLugar>
        {
            public MarcadorLugar Fijado;

            public int Compare(MarcadorLugar a, MarcadorLugar b)
            {
                if (ReferenceEquals(a, b)) return 0;
                if (a == Fijado) return -1;
                if (b == Fijado) return 1;
                if (a.TienePrecio != b.TienePrecio) return a.TienePrecio ? -1 : 1;
                // Visible aún es el del fotograma anterior: las bajas y altas de este se aplican después de ordenar
                if (a.Visible != b.Visible) return a.Visible ? -1 : 1;
                int c = a.Profundidad.CompareTo(b.Profundidad);
                return c != 0 ? c : a.Indice.CompareTo(b.Indice);
            }
        }

        /// <summary>Zonas (dp del panel, ZonaDp) de los marcadores que se ven tras la última actualización.</summary>
        public IReadOnlyList<Rect> ZonasVisibles => zonasVisibles;

        /// <summary>Marcadores en pantalla ocultados en la última actualización por pisar a otro que gana o un control.</summary>
        public int OcultosPorSolape { get; private set; }

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

        /// <summary>Distancia máxima (dp) entre el toque y un marcador para que lo abra (historia 1.8).</summary>
        public const float RadioToqueDp = 24f;

        /// <summary>
        /// Marcador visible más cercano a un punto del panel (dp), si está a menos de radioDp; si no, null.
        /// La distancia se mide al cuerpo y la punta dibujados; a igual distancia (p. ej. encima de dos que se
        /// solapan) gana el que se pinta encima, el más cercano a la cámara.
        /// </summary>
        public MarcadorLugar MasCercano(Vector2 puntoDp, float radioDp = RadioToqueDp)
        {
            MarcadorLugar mejor = null;
            float mejorDistancia = float.PositiveInfinity;
            foreach (var m in marcadores)
            {
                if (!m.Visible) continue;
                float d = m.DistanciaDp(puntoDp);
                if (!(d < radioDp)) continue;
                bool gana = mejor == null || d < mejorDistancia - 0.001f
                            || (d <= mejorDistancia + 0.001f && m.Profundidad < mejor.Profundidad);
                if (!gana) continue;
                mejor = m;
                mejorDistancia = d;
            }
            return mejor;
        }

        /// <summary>Vuelve a dibujar los vectores (al cambiar de tema cambia el color del €).</summary>
        public void Repintar()
        {
            foreach (var m in marcadores) m.Repintar();
        }
    }
}
