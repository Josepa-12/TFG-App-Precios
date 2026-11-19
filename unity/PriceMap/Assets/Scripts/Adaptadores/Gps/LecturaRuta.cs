using System.Collections.Generic;
using System.Globalization;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Una posición del servicio de ruta (historia 2.1): coordenada, rumbo de la marcha, velocidad, precisión y hora.
    /// Llega de Java como texto, una por línea: "lat;lon;rumbo;velocidad;precision;horaMs" con punto decimal y los
    /// campos opcionales vacíos si el GPS no los da.
    /// </summary>
    internal readonly struct LecturaRuta
    {
        public Coordenada Posicion { get; }
        /// <summary>Rumbo de la marcha en grados respecto al norte (no el de la brújula); null si el GPS no lo da.</summary>
        public float? RumboMarcha { get; }
        /// <summary>Velocidad en m/s; null si el GPS no la da.</summary>
        public float? Velocidad { get; }
        /// <summary>Radio de precisión en metros; null si el GPS no lo da.</summary>
        public float? Precision { get; }
        /// <summary>Hora de la lectura en milisegundos desde 1970 (UTC).</summary>
        public long HoraMs { get; }

        public LecturaRuta(Coordenada posicion, float? rumboMarcha, float? velocidad, float? precision, long horaMs)
        {
            Posicion = posicion;
            RumboMarcha = rumboMarcha;
            Velocidad = velocidad;
            Precision = precision;
            HoraMs = horaMs;
        }

        /// <summary>Lee la cola del servicio en orden; las líneas dañadas se saltan.</summary>
        public static List<LecturaRuta> Leer(string texto)
        {
            var lecturas = new List<LecturaRuta>();
            if (string.IsNullOrEmpty(texto)) return lecturas;
            foreach (var linea in texto.Split('\n'))
            {
                var c = linea.Trim().Split(';');
                if (c.Length != 6) continue;
                if (!double.TryParse(c[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) continue;
                if (!double.TryParse(c[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) continue;
                if (!long.TryParse(c[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hora)) continue;
                if (lat < -90 || lat > 90 || lon < -180 || lon > 180) continue;
                lecturas.Add(new LecturaRuta(new Coordenada(lat, lon), Opcional(c[2]), Opcional(c[3]), Opcional(c[4]), hora));
            }
            return lecturas;
        }

        static float? Opcional(string campo) =>
            float.TryParse(campo, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (float?)null;
    }
}
