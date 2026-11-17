using System;
using System.Globalization;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Punto geográfico real (WGS84) en grados, con doble precisión. Dominio y Aplicación
    /// usan siempre este tipo; las coordenadas de Unity solo existen dentro del adaptador del mapa (AD-13).
    /// </summary>
    public readonly struct Coordenada : IEquatable<Coordenada>
    {
        public double Latitud { get; }
        public double Longitud { get; }

        public Coordenada(double latitud, double longitud)
        {
            Latitud = latitud;
            Longitud = longitud;
        }

        public bool Equals(Coordenada otra) => Latitud.Equals(otra.Latitud) && Longitud.Equals(otra.Longitud);
        public override bool Equals(object obj) => obj is Coordenada otra && Equals(otra);
        public override int GetHashCode() => HashCode.Combine(Latitud, Longitud);
        public static bool operator ==(Coordenada a, Coordenada b) => a.Equals(b);
        public static bool operator !=(Coordenada a, Coordenada b) => !a.Equals(b);

        /// <summary>"36.777900, -6.352800": punto decimal sea cual sea el idioma del móvil.</summary>
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0:F6}, {1:F6}", Latitud, Longitud);
    }
}
