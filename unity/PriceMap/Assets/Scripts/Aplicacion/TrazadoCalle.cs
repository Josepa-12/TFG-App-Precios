using System;
using System.Collections.Generic;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Punto medio del trazado de una calle (spec buscar-calles-guardadas). Los tramos se cosen por sus vértices comunes
    /// y por los extremos sueltos que quedan a 5 m (el corte entre dos teselas); si la calle queda en varios trozos, vale
    /// el más largo. El medio es el de su recorrido más largo, siempre sobre un tramo de la calle.
    /// </summary>
    public static class TrazadoCalle
    {
        /// <summary>Distancia máxima (m) a la que se cose un extremo suelto.</summary>
        public const double MetrosUnion = 5.0;

        readonly struct Arista
        {
            public readonly int Destino;
            public readonly double Metros;
            public readonly bool Union; // cosido: no es un trozo de calle

            public Arista(int destino, double metros, bool union)
            {
                Destino = destino;
                Metros = metros;
                Union = union;
            }
        }

        /// <summary>El punto medio; sin ningún punto, default.</summary>
        public static Coordenada PuntoMedio(IReadOnlyList<IReadOnlyList<Coordenada>> tramos)
        {
            var nodos = new List<Coordenada>();
            var indices = new Dictionary<Coordenada, int>();
            var aristas = new List<List<Arista>>();

            int Nodo(Coordenada c)
            {
                if (indices.TryGetValue(c, out int i)) return i;
                indices[c] = i = nodos.Count;
                nodos.Add(c);
                aristas.Add(new List<Arista>());
                return i;
            }

            void Unir(int a, int b, bool union)
            {
                if (a == b) return;
                foreach (var x in aristas[a]) if (x.Destino == b) return; // un trozo repetido no es otra arista
                double m = nodos[a].DistanciaMetros(nodos[b]);
                aristas[a].Add(new Arista(b, m, union));
                aristas[b].Add(new Arista(a, m, union));
            }

            if (tramos != null)
                foreach (var t in tramos)
                    for (int i = 0; i < t.Count; i++)
                    {
                        int n = Nodo(t[i]);
                        if (i > 0) Unir(Nodo(t[i - 1]), n, union: false);
                    }
            if (nodos.Count == 0) return default;

            // Extremos sueltos (una sola arista) a 5 m de otro vértice: se cosen
            var sueltos = new List<int>();
            for (int i = 0; i < nodos.Count; i++) if (aristas[i].Count == 1) sueltos.Add(i);
            double gradosLat = MetrosUnion / 111000.0;
            foreach (int s in sueltos)
                for (int i = 0; i < nodos.Count; i++)
                    if (i != s && Math.Abs(nodos[i].Latitud - nodos[s].Latitud) <= gradosLat
                        && nodos[i].DistanciaMetros(nodos[s]) <= MetrosUnion)
                        Unir(s, i, union: true);

            int lejos = MasLejano(aristas, TrozoMasLargo(aristas), out _, out _); // un extremo del recorrido más largo
            int fin = MasLejano(aristas, lejos, out var metros, out var previo);  // y el otro
            return Mitad(nodos, aristas, fin, metros, previo);
        }

        // Un vértice del trozo con más metros de calle
        static int TrozoMasLargo(List<List<Arista>> aristas)
        {
            var visto = new bool[aristas.Count];
            var pila = new Stack<int>();
            int mejor = 0;
            double mejorMetros = -1;
            for (int i = 0; i < aristas.Count; i++)
            {
                if (visto[i]) continue;
                double total = 0;
                visto[i] = true;
                pila.Push(i);
                while (pila.Count > 0)
                    foreach (var a in aristas[pila.Pop()])
                    {
                        if (!a.Union) total += a.Metros;
                        if (visto[a.Destino]) continue;
                        visto[a.Destino] = true;
                        pila.Push(a.Destino);
                    }
                if (total <= mejorMetros) continue;
                mejorMetros = total;
                mejor = i;
            }
            return mejor;
        }

        // Dijkstra: el vértice más lejano del trozo, con los metros y el vértice anterior de cada uno
        static int MasLejano(List<List<Arista>> aristas, int origen, out double[] metros, out int[] previo)
        {
            metros = new double[aristas.Count];
            previo = new int[aristas.Count];
            for (int i = 0; i < metros.Length; i++)
            {
                metros[i] = double.PositiveInfinity;
                previo[i] = -1;
            }
            metros[origen] = 0;
            var cola = new SortedSet<(double metros, int nodo)> { (0, origen) };
            int lejano = origen;
            while (cola.Count > 0)
            {
                var primero = cola.Min;
                cola.Remove(primero);
                lejano = primero.nodo; // salen por orden de distancia: el último es el más lejano
                foreach (var a in aristas[lejano])
                {
                    double d = primero.metros + a.Metros;
                    if (d >= metros[a.Destino]) continue;
                    if (!double.IsPositiveInfinity(metros[a.Destino])) cola.Remove((metros[a.Destino], a.Destino));
                    metros[a.Destino] = d;
                    previo[a.Destino] = lejano;
                    cola.Add((d, a.Destino));
                }
            }
            return lejano;
        }

        static Coordenada Mitad(List<Coordenada> nodos, List<List<Arista>> aristas, int fin, double[] metros, int[] previo)
        {
            double mitad = metros[fin] / 2;
            int n = fin;
            while (previo[n] >= 0 && metros[previo[n]] > mitad) n = previo[n];
            int p = previo[n];
            if (p < 0) return nodos[n];
            foreach (var a in aristas[p])
                if (a.Union && a.Destino == n) return nodos[n]; // el cosido no es calle; su extremo, sí
            double largo = metros[n] - metros[p];
            double f = largo > 0 ? (mitad - metros[p]) / largo : 0;
            var x = nodos[p];
            var y = nodos[n];
            return new Coordenada(x.Latitud + (y.Latitud - x.Latitud) * f, x.Longitud + (y.Longitud - x.Longitud) * f);
        }
    }
}
