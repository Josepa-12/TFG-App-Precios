using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Dónde va la etiqueta de minutos de cada ruta de «Cómo llegar» (parte 5b): en el punto medio de su parte propia, el
    /// tramo seguido más largo que no comparte con las otras (a más de RutaParteComunMetros de ellas). Sin parte propia, o
    /// con una sola ruta, en el punto medio de la línea. Lógica pura.
    /// </summary>
    public static class ReglaParteRuta
    {
        /// <summary>Metros entre las muestras de la línea y cuántas como mucho.</summary>
        const double Paso = 20.0;
        const int MuestrasMaximo = 400;
        const double GradosARadianes = Math.PI / 180.0;
        const double MetrosPorGrado = Coordenada.RadioTierraMetros * GradosARadianes;

        /// <summary>Punto medio de la parte propia de la ruta con ese índice; null si no tiene puntos.</summary>
        public static Coordenada? PuntoMedio(IReadOnlyList<Ruta> rutas, int indice)
        {
            if (rutas == null || indice < 0 || indice >= rutas.Count) return null;
            var puntos = rutas[indice].Puntos;
            if (puntos.Count == 0) return null;
            var muestras = Muestras(puntos);
            int mejorDesde = 0, mejorLargo = 0, desde = 0;
            for (int i = 0; i <= muestras.Count; i++)
            {
                if (i < muestras.Count && EsPropia(muestras[i], rutas, indice)) continue;
                if (i - desde > mejorLargo) { mejorDesde = desde; mejorLargo = i - desde; }
                desde = i + 1;
            }
            return mejorLargo > 0 ? muestras[mejorDesde + (mejorLargo - 1) / 2] : muestras[(muestras.Count - 1) / 2];
        }

        static bool EsPropia(Coordenada punto, IReadOnlyList<Ruta> rutas, int indice)
        {
            for (int r = 0; r < rutas.Count; r++)
                if (r != indice && CercaDe(punto, rutas[r].Puntos)) return false;
            return true;
        }

        // El punto está a RutaParteComunMetros o menos de la línea (equirrectangular local centrada en él)
        static bool CercaDe(Coordenada punto, IReadOnlyList<Coordenada> linea)
        {
            const double margen = ParametrosNegocio.RutaParteComunMetros;
            double cos = Math.Cos(punto.Latitud * GradosARadianes);
            double ax = 0, ay = 0;
            for (int i = 0; i < linea.Count; i++)
            {
                double bx = (linea[i].Longitud - punto.Longitud) * cos * MetrosPorGrado;
                double by = (linea[i].Latitud - punto.Latitud) * MetrosPorGrado;
                if (i == 0) { ax = bx; ay = by; }
                // Los dos extremos al mismo lado y lejos: el tramo no puede estar cerca
                bool lejos = (ax > margen && bx > margen) || (ax < -margen && bx < -margen) ||
                             (ay > margen && by > margen) || (ay < -margen && by < -margen);
                if (!lejos)
                {
                    double dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy;
                    double t = l2 > 0 ? Math.Max(0, Math.Min(1, -(ax * dx + ay * dy) / l2)) : 0;
                    double px = ax + t * dx, py = ay + t * dy;
                    if (px * px + py * py <= margen * margen) return true;
                }
                ax = bx;
                ay = by;
            }
            return false;
        }

        // Puntos repartidos por igual a lo largo de la línea (al menos uno)
        static List<Coordenada> Muestras(IReadOnlyList<Coordenada> puntos)
        {
            double total = 0;
            for (int i = 1; i < puntos.Count; i++) total += puntos[i - 1].DistanciaMetros(puntos[i]);
            if (total <= 0) return new List<Coordenada> { puntos[0] };
            int n = (int)Math.Max(1, Math.Min(MuestrasMaximo, Math.Ceiling(total / Paso)));
            var r = new List<Coordenada>(n);
            double recorrido = 0;
            for (int i = 1; i < puntos.Count && r.Count < n; i++)
            {
                double largo = puntos[i - 1].DistanciaMetros(puntos[i]);
                while (r.Count < n)
                {
                    double falta = (r.Count + 0.5) * total / n - recorrido;
                    if (falta > largo) break;
                    double t = largo > 0 ? falta / largo : 0;
                    r.Add(new Coordenada(puntos[i - 1].Latitud + t * (puntos[i].Latitud - puntos[i - 1].Latitud),
                        puntos[i - 1].Longitud + t * (puntos[i].Longitud - puntos[i - 1].Longitud)));
                }
                recorrido += largo;
            }
            return r;
        }
    }
}
