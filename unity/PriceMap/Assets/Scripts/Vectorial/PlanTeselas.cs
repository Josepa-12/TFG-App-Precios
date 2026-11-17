using System.Collections.Generic;
using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Qué teselas cargar y cuáles liberar según el centro de la vista. Distancia en teselas
    /// (la mayor de las dos diferencias, x o y). Función pura para probarla sin red ni escena.
    /// </summary>
    public static class PlanTeselas
    {
        public readonly struct Plan
        {
            /// <summary>Teselas que faltan, de la más cercana al centro a la más lejana.</summary>
            public readonly List<Vector2Int> aCargar;
            /// <summary>Teselas cargadas que han quedado demasiado lejos.</summary>
            public readonly List<Vector2Int> aLiberar;

            public Plan(List<Vector2Int> aCargar, List<Vector2Int> aLiberar)
            {
                this.aCargar = aCargar;
                this.aLiberar = aLiberar;
            }
        }

        public static int Distancia(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        public static Plan Calcular(Vector2Int centro, ICollection<Vector2Int> cargadas, ICollection<Vector2Int> pendientes,
                                    int radioCarga, int radioLiberar)
        {
            var aCargar = new List<Vector2Int>();
            for (int dy = -radioCarga; dy <= radioCarga; dy++)
                for (int dx = -radioCarga; dx <= radioCarga; dx++)
                {
                    var t = new Vector2Int(centro.x + dx, centro.y + dy);
                    if (!cargadas.Contains(t) && !pendientes.Contains(t)) aCargar.Add(t);
                }
            aCargar.Sort((a, b) => (a - centro).sqrMagnitude.CompareTo((b - centro).sqrMagnitude));

            var aLiberar = new List<Vector2Int>();
            foreach (var t in cargadas)
                if (Distancia(t, centro) > radioLiberar) aLiberar.Add(t);

            return new Plan(aCargar, aLiberar);
        }
    }
}
