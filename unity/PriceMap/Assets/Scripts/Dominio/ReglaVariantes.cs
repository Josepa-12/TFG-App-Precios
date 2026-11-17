using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>Qué variante de un concepto está vigente ahora (FR-60), para destacarla en la ficha.</summary>
    public static class ReglaVariantes
    {
        /// <summary>
        /// La primera variante (con condición) que se cumple en este instante, o null si ninguna.
        /// <paramref name="ahoraLocal"/> es la hora local del lugar (España), no UTC: la convierte la Aplicación.
        /// Las de tipo de cliente nunca están vigentes.
        /// </summary>
        public static Variante Vigente(IReadOnlyList<Variante> variantes, System.DateTime ahoraLocal)
        {
            if (variantes == null) return null;
            foreach (var v in variantes)
                if (v?.Condicion != null && v.Condicion.SeCumple(ahoraLocal)) return v;
            return null;
        }
    }
}
