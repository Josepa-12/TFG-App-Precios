using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Ruta calculada (historia 2.6): la línea por las calles (puntos en orden), su longitud, su duración y sus
    /// maniobras. Nada se guarda (FR17): vive en memoria mientras sirve.
    /// </summary>
    public sealed class Ruta
    {
        public IReadOnlyList<Coordenada> Puntos { get; }
        public double Metros { get; }
        public double Segundos { get; }
        public IReadOnlyList<Maniobra> Maniobras { get; }

        public Ruta(IReadOnlyList<Coordenada> puntos, double metros, double segundos, IReadOnlyList<Maniobra> maniobras)
        {
            Puntos = puntos ?? Array.Empty<Coordenada>();
            Metros = metros < 0 || double.IsNaN(metros) ? 0 : metros;
            Segundos = segundos < 0 || double.IsNaN(segundos) ? 0 : segundos;
            Maniobras = maniobras ?? Array.Empty<Maniobra>();
        }

        /// <summary>Minutos que se enseñan: redondeados hacia arriba y como mínimo 1 ("1 min" aunque sean 10 s).</summary>
        public int Minutos => MinutosDe(Segundos);

        /// <summary>Segundos a minutos de la ficha: hacia arriba, como mínimo 1.</summary>
        public static int MinutosDe(double segundos)
        {
            if (double.IsNaN(segundos) || segundos <= 60) return 1;
            return (int)Math.Min(int.MaxValue, Math.Ceiling(segundos / 60.0));
        }
    }
}
