using System.Collections.Generic;
using UnityEngine;

namespace PrecioLocal.Vectorial
{
    /// <summary>
    /// Convierte un polígono 2D (con huecos, p. ej. los patios de una manzana) en triángulos por
    /// "recorte de orejas" (ear clipping). Los huecos se unen al contorno exterior con un puente
    /// desde su punto más a la derecha, como hace la librería earcut de Mapbox (versión simplificada).
    /// </summary>
    public static class Triangulador
    {
        /// <summary>Añade los puntos a <paramref name="puntos"/> y los triángulos (índices) a <paramref name="tris"/>.</summary>
        public static void Triangular(List<Vector2> exterior, List<List<Vector2>> huecos, List<Vector2> puntos, List<int> tris)
        {
            var anillo = Limpio(exterior);
            if (anillo.Count < 3) return;
            if (AreaFirmada(anillo) < 0) anillo.Reverse();          // exterior en sentido antihorario

            if (huecos != null && huecos.Count > 0)
            {
                var hs = new List<List<Vector2>>();
                foreach (var h in huecos)
                {
                    var c = Limpio(h);
                    if (c.Count < 3) continue;
                    if (AreaFirmada(c) > 0) c.Reverse();              // huecos en sentido horario
                    hs.Add(c);
                }
                hs.Sort((p, q) => MaxX(q).CompareTo(MaxX(p)));
                foreach (var h in hs) Puentear(anillo, h);
            }

            int b = puntos.Count;
            puntos.AddRange(anillo);
            Recortar(anillo, b, tris);
        }

        static List<Vector2> Limpio(List<Vector2> p)
        {
            var r = new List<Vector2>(p.Count);
            foreach (var v in p) if (r.Count == 0 || (r[r.Count - 1] - v).sqrMagnitude > 1e-6f) r.Add(v);
            if (r.Count > 1 && (r[0] - r[r.Count - 1]).sqrMagnitude < 1e-6f) r.RemoveAt(r.Count - 1);
            return r;
        }

        static float MaxX(List<Vector2> p) { float m = float.MinValue; foreach (var v in p) m = Mathf.Max(m, v.x); return m; }

        public static float AreaFirmada(List<Vector2> p)
        {
            float s = 0;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += p[j].x * p[i].y - p[i].x * p[j].y;
            return s / 2;
        }

        // Une el hueco al contorno: rayo hacia +x desde el punto más a la derecha del hueco
        static void Puentear(List<Vector2> anillo, List<Vector2> hueco)
        {
            int m = 0;
            for (int i = 1; i < hueco.Count; i++) if (hueco[i].x > hueco[m].x) m = i;
            Vector2 M = hueco[m];

            int mejor = -1; float mejorX = float.MaxValue;
            for (int i = 0, j = anillo.Count - 1; i < anillo.Count; j = i++)
            {
                Vector2 a = anillo[j], c = anillo[i];
                if ((a.y - M.y) * (c.y - M.y) > 0 || Mathf.Approximately(a.y, c.y)) continue;
                float xi = a.x + (M.y - a.y) * (c.x - a.x) / (c.y - a.y);
                if (xi >= M.x && xi < mejorX) { mejorX = xi; mejor = a.x > c.x ? j : i; }
            }
            if (mejor < 0) // sin cruce: el vértice más cercano
            {
                float d = float.MaxValue;
                for (int i = 0; i < anillo.Count; i++) { float di = (anillo[i] - M).sqrMagnitude; if (di < d) { d = di; mejor = i; } }
            }

            var nuevo = new List<Vector2>(anillo.Count + hueco.Count + 2);
            for (int i = 0; i <= mejor; i++) nuevo.Add(anillo[i]);
            for (int k = 0; k <= hueco.Count; k++) nuevo.Add(hueco[(m + k) % hueco.Count]);
            nuevo.Add(anillo[mejor]);
            for (int i = mejor + 1; i < anillo.Count; i++) nuevo.Add(anillo[i]);
            anillo.Clear(); anillo.AddRange(nuevo);
        }

        static void Recortar(List<Vector2> p, int b, List<int> tris)
        {
            int n = p.Count;
            var sig = new int[n]; var ant = new int[n];
            for (int i = 0; i < n; i++) { sig[i] = (i + 1) % n; ant[i] = (i + n - 1) % n; }
            int restantes = n, actual = 0, fallos = 0;
            while (restantes > 3)
            {
                int a = ant[actual], c = sig[actual];
                bool oreja = EsOreja(p, a, actual, c, sig);
                if (oreja || ++fallos > restantes) // si se atasca (polígono degenerado), recorta igualmente
                {
                    tris.Add(b + a); tris.Add(b + actual); tris.Add(b + c);
                    sig[a] = c; ant[c] = a; restantes--; fallos = 0;
                    actual = c;
                }
                else actual = c;
            }
            tris.Add(b + ant[actual]); tris.Add(b + actual); tris.Add(b + sig[actual]);
        }

        static bool EsOreja(List<Vector2> p, int a, int b, int c, int[] sig)
        {
            Vector2 A = p[a], B = p[b], C = p[c];
            if (Cruz(A, B, C) <= 0) return false;                     // vértice cóncavo o alineado
            for (int i = sig[c]; i != a; i = sig[i])
            {
                Vector2 P = p[i];
                if (P == A || P == B || P == C) continue;
                if (Cruz(A, B, P) >= 0 && Cruz(B, C, P) >= 0 && Cruz(C, A, P) >= 0) return false;
            }
            return true;
        }

        static float Cruz(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }
}
