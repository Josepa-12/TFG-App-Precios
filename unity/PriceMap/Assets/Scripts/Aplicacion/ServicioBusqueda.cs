using System;
using System.Collections.Generic;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Buscador (historia 2.3, parte 1; FR14): busca en los lugares de la app y en los puntos de interés de las
    /// teselas cargadas (IMapa.PuntosInteres). Primero los lugares y después los puntos; en cada grupo, por
    /// puntuación (ReglaBusqueda) y después por distancia al punto de cercanía. Como mucho MaximoResultados.
    /// Fuera los puntos sin categoría y los que repiten un lugar de la app (misma regla que la 1.6). Todo es local:
    /// se busca a cada tecla y no se guarda nada (FR17).
    /// </summary>
    public class ServicioBusqueda
    {
        public const int MaximoResultados = 20;

        static readonly IReadOnlyList<ResultadoBusqueda> Ninguno = Array.Empty<ResultadoBusqueda>();

        readonly ServicioLugares lugares;
        readonly IMapa mapa;
        readonly DateTime ahora;

        /// <param name="lugares">Lugares de la app (y su precio).</param>
        /// <param name="mapa">De él salen los puntos de interés de las teselas cargadas; null, solo lugares.</param>
        /// <param name="ahora">Momento con el que se calcula el precio anunciado (AD-12: lo da el Arranque).</param>
        public ServicioBusqueda(ServicioLugares lugares, IMapa mapa, DateTime ahora)
        {
            this.lugares = lugares ?? throw new ArgumentNullException(nameof(lugares));
            this.mapa = mapa;
            this.ahora = ahora;
        }

        /// <summary>
        /// Resultados para la consulta, ordenados, con la distancia a <paramref name="cerca"/> (la posición del
        /// usuario o, si no la hay, el centro del mapa). Vacío si la consulta no tiene letras ni números.
        /// </summary>
        public IReadOnlyList<ResultadoBusqueda> Buscar(string consulta, Coordenada cerca)
        {
            string q = ReglaBusqueda.Normalizar(consulta);
            var palabras = ReglaBusqueda.Palabras(q);
            if (palabras.Length == 0) return Ninguno;

            var deLugares = new List<ResultadoBusqueda>();
            foreach (var l in lugares.Lugares)
            {
                int p = ReglaBusqueda.PuntuarNormalizada(q, palabras, l.Nombre, l.Direccion);
                if (p == ReglaBusqueda.NoCoincide) continue;
                var precio = lugares.Resumen(l.Id, ahora).Valor?.PrecioAnunciado;
                deLugares.Add(ResultadoBusqueda.DeLugar(l, cerca.DistanciaMetros(l.Posicion), p, precio));
            }

            var dePuntos = new List<ResultadoBusqueda>();
            var puntos = mapa?.PuntosInteres;
            if (puntos != null)
            {
                var vistos = new HashSet<ulong>(); // un mismo punto puede llegar en dos teselas
                foreach (var punto in puntos)
                {
                    if (punto == null || string.IsNullOrEmpty(punto.Nombre)) continue;
                    var categoria = ReglaPuntosInteres.Categoria(punto.Clase, punto.Subclase);
                    if (categoria == CategoriaPoi.Ninguna) continue;
                    int p = ReglaBusqueda.PuntuarNormalizada(q, palabras, punto.Nombre);
                    if (p == ReglaBusqueda.NoCoincide) continue;
                    if (punto.Id != 0 && !vistos.Add(punto.Id)) continue;
                    if (ReglaPuntosInteres.EsRepetido(punto.Nombre, punto.Posicion, lugares.Lugares)) continue;
                    bool publica = ReglaPuntosInteres.EsSanidadPublica(categoria, punto.Nombre, punto.Clase, punto.Subclase);
                    dePuntos.Add(ResultadoBusqueda.DePunto(punto, categoria, publica, cerca.DistanciaMetros(punto.Posicion), p));
                }
            }

            deLugares.Sort(Comparar);
            dePuntos.Sort(Comparar);
            var r = new List<ResultadoBusqueda>(Math.Min(MaximoResultados, deLugares.Count + dePuntos.Count));
            foreach (var x in deLugares) { if (r.Count >= MaximoResultados) break; r.Add(x); }
            foreach (var x in dePuntos) { if (r.Count >= MaximoResultados) break; r.Add(x); }
            return r.AsReadOnly();
        }

        /// <summary>Más puntuación primero; a igual puntuación, más cerca primero; después, por nombre (orden estable).</summary>
        static int Comparar(ResultadoBusqueda a, ResultadoBusqueda b)
        {
            int c = b.Puntuacion.CompareTo(a.Puntuacion);
            if (c == 0) c = a.Metros.CompareTo(b.Metros);
            if (c == 0) c = string.CompareOrdinal(a.Nombre, b.Nombre);
            return c;
        }
    }
}
