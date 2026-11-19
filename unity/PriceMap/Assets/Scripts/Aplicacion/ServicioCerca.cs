using System;
using System.Collections.Generic;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// "Encuentra cerca" (historia 2.4, parte 1; FR15, UX-DR17): lo que hay cerca de una categoría de la hoja del
    /// buscador (ReglaCerca), o de todas con "Encuentra cerca". Primero los lugares de la app con precio, por
    /// distancia; después el resto (lugares sin precio y puntos de interés), también por distancia. Lugares a
    /// RadioMetros o menos; puntos de interés de las teselas cargadas, sin los que repiten un lugar (regla de la 1.6).
    /// Como mucho MaximoResultados. Todo es local y no se guarda nada.
    /// </summary>
    public class ServicioCerca
    {
        /// <summary>Distancia máxima (m) de los lugares de la app.</summary>
        public const double RadioMetros = 3000.0;
        /// <summary>Filas como mucho.</summary>
        public const int MaximoResultados = 50;

        readonly ServicioLugares lugares;
        readonly IMapa mapa;
        readonly DateTime ahora;

        /// <param name="lugares">Lugares de la app (y su precio).</param>
        /// <param name="mapa">De él salen los puntos de interés de las teselas cargadas; null, solo lugares.</param>
        /// <param name="ahora">Momento con el que se calcula el precio anunciado (AD-12: lo da el Arranque).</param>
        public ServicioCerca(ServicioLugares lugares, IMapa mapa, DateTime ahora)
        {
            this.lugares = lugares ?? throw new ArgumentNullException(nameof(lugares));
            this.mapa = mapa;
            this.ahora = ahora;
        }

        /// <summary>
        /// Lista de la categoría <paramref name="chip"/> (null: "Encuentra cerca", todas), con la distancia a
        /// <paramref name="cerca"/> (la posición del usuario o, si no la hay, el centro del mapa).
        /// </summary>
        public IReadOnlyList<ResultadoBusqueda> Listar(CategoriaPoi? chip, Coordenada cerca)
        {
            var conPrecio = new List<ResultadoBusqueda>();
            var resto = new List<ResultadoBusqueda>();
            foreach (var l in lugares.Lugares)
            {
                if (!ReglaCerca.Incluye(chip, l)) continue;
                double metros = cerca.DistanciaMetros(l.Posicion);
                if (metros > RadioMetros) continue;
                var precio = lugares.Resumen(l.Id, ahora).Valor?.PrecioAnunciado;
                (precio.HasValue ? conPrecio : resto).Add(ResultadoBusqueda.DeLugar(l, metros, 0, precio));
            }

            var puntos = mapa?.PuntosInteres;
            if (puntos != null)
            {
                // Un punto solo se quita por repetir un lugar de esta misma categoría: si no, se iría sin que su lugar
                // salga en la lista (p. ej. un lugar de un tipo sin categoría)
                var deLaCategoria = new List<Lugar>();
                foreach (var l in lugares.Lugares) if (ReglaCerca.Incluye(chip, l)) deLaCategoria.Add(l);
                var vistos = new HashSet<ulong>(); // un mismo punto puede llegar en dos teselas
                foreach (var punto in puntos)
                {
                    if (punto == null || string.IsNullOrEmpty(punto.Nombre)) continue;
                    var categoria = ReglaPuntosInteres.Categoria(punto.Clase, punto.Subclase);
                    if (!ReglaCerca.Incluye(chip, categoria)) continue;
                    if (punto.Id != 0 && !vistos.Add(punto.Id)) continue;
                    if (ReglaPuntosInteres.EsRepetido(punto.Nombre, punto.Posicion, deLaCategoria)) continue;
                    bool publica = ReglaPuntosInteres.EsSanidadPublica(categoria, punto.Nombre, punto.Clase, punto.Subclase);
                    resto.Add(ResultadoBusqueda.DePunto(punto, categoria, publica, cerca.DistanciaMetros(punto.Posicion), 0));
                }
            }

            conPrecio.Sort(Comparar);
            resto.Sort(Comparar);
            var r = new List<ResultadoBusqueda>(Math.Min(MaximoResultados, conPrecio.Count + resto.Count));
            foreach (var x in conPrecio) { if (r.Count >= MaximoResultados) break; r.Add(x); }
            foreach (var x in resto) { if (r.Count >= MaximoResultados) break; r.Add(x); }
            return r.AsReadOnly();
        }

        /// <summary>Más cerca primero; a igual distancia, por nombre (orden estable).</summary>
        static int Comparar(ResultadoBusqueda a, ResultadoBusqueda b)
        {
            int c = a.Metros.CompareTo(b.Metros);
            if (c == 0) c = string.CompareOrdinal(a.Nombre, b.Nombre);
            return c;
        }
    }
}
