using System;
using System.Collections.Generic;
using System.Linq;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Las rutas entre las que se elige (parte 5a de «Cómo llegar»): de menos a más tiempo, sin repetidas y tres como
    /// mucho (ParametrosNegocio). La primera es la más rápida, la de siempre (ficha, Ir y recálculo).
    /// </summary>
    public static class ReglaAlternativas
    {
        /// <summary>
        /// Junta las rutas (las null se saltan). De dos repetidas queda la más rápida. Si sobran, quedan las más rápidas
        /// y, de última, la más corta en metros, que siempre sale entre las opciones.
        /// </summary>
        public static IReadOnlyList<Ruta> Juntar(IEnumerable<Ruta> rutas)
        {
            var lista = new List<Ruta>();
            if (rutas == null) return lista.AsReadOnly();
            foreach (var r in rutas.Where(r => r != null).OrderBy(r => r.Segundos).ThenBy(r => r.Metros))
                if (!lista.Any(x => SonLaMisma(x, r))) lista.Add(r);

            const int maximo = ParametrosNegocio.RutaAlternativasMaximo;
            if (lista.Count > maximo)
            {
                var corta = lista.OrderBy(r => r.Metros).First();
                int sitio = Math.Min(lista.IndexOf(corta), maximo - 1);
                lista[sitio] = corta; // si no estaba entre las primeras, ocupa el último sitio
                lista.RemoveRange(maximo, lista.Count - maximo);
            }
            return lista.AsReadOnly();
        }

        /// <summary>Los mismos metros y los mismos segundos, con el margen (de la mayor de las dos medidas).</summary>
        public static bool SonLaMisma(Ruta a, Ruta b) => Parecidos(a.Metros, b.Metros) && Parecidos(a.Segundos, b.Segundos);

        static bool Parecidos(double a, double b) => Math.Abs(a - b) <= Math.Max(a, b) * ParametrosNegocio.RutaAlternativasMargen;
    }
}
