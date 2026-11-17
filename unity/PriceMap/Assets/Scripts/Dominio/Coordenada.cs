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

        /// <summary>Radio medio de la Tierra (m) para la distancia haversine (el mismo que distM del prototipo).</summary>
        public const double RadioTierraMetros = 6371000.0;

        /// <summary>Distancia en metros sobre la esfera (haversine), como distM de prototipo/app.js.</summary>
        public double DistanciaMetros(Coordenada otra)
        {
            const double aRadianes = Math.PI / 180.0;
            double dLat = (otra.Latitud - Latitud) * aRadianes;
            double dLon = (otra.Longitud - Longitud) * aRadianes;
            double sLat = Math.Sin(dLat / 2), sLon = Math.Sin(dLon / 2);
            double h = sLat * sLat + Math.Cos(Latitud * aRadianes) * Math.Cos(otra.Latitud * aRadianes) * sLon * sLon;
            return 2 * RadioTierraMetros * Math.Asin(Math.Sqrt(Math.Min(1.0, h)));
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
