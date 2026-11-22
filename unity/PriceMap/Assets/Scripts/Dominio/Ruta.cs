using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Ruta calculada (historia 2.6): la línea por las calles (puntos en orden), su longitud, su duración y sus
    /// maniobras. Nada se guarda (FR17): vive en memoria mientras sirve. Aproximada (parte 2b): calculada en el móvil
    /// por las calles guardadas, sin conexión, sin sentidos únicos ni maniobras.
    /// </summary>
    public sealed class Ruta
    {
        public IReadOnlyList<Coordenada> Puntos { get; }
        public double Metros { get; }
        public double Segundos { get; }
        public IReadOnlyList<Maniobra> Maniobras { get; }
        /// <summary>Ruta aproximada por las calles guardadas (sin conexión, historia 2.6, parte 2b).</summary>
        public bool Aproximada { get; }

        public Ruta(IReadOnlyList<Coordenada> puntos, double metros, double segundos, IReadOnlyList<Maniobra> maniobras,
            bool aproximada = false)
        {
            Aproximada = aproximada;
            Puntos = puntos ?? Array.Empty<Coordenada>();
            Metros = metros < 0 || double.IsNaN(metros) ? 0 : metros;
            Segundos = segundos < 0 || double.IsNaN(segundos) ? 0 : segundos;
            Maniobras = maniobras ?? Array.Empty<Maniobra>();
        }

        /// <summary>Minutos que se enseñan: redondeados hacia arriba y como mínimo 1 ("1 min" aunque sean 10 s).</summary>
        public int Minutos => MinutosDe(Segundos);

        /// <summary>
        /// Segundos a minutos de la ficha: al segundo (360,3 s y 180,0001 s son 6 y 3 min en la ficha y en las tarjetas),
        /// hacia arriba, como mínimo 1.
        /// </summary>
        public static int MinutosDe(double segundos)
        {
            if (double.IsNaN(segundos)) return 1;
            segundos = Math.Round(segundos);
            if (segundos <= 60) return 1;
            return (int)Math.Min(int.MaxValue, Math.Ceiling(segundos / 60.0));
        }
    }
}
