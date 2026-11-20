using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Polilínea codificada de Google (historia 2.6): la línea de la ruta que dan Valhalla y OSRM. Precisión 5
    /// (polyline) o 6 (polyline6): cada valor es la diferencia con el anterior multiplicada por 10^precisión, en
    /// zigzag y en trozos de 5 bits con 63 sumado. Latitud primero y después longitud.
    /// </summary>
    public static class Polilinea
    {
        /// <summary>
        /// Las coordenadas de la cadena; vacía si la cadena es nula o vacía. Lanza FormatException si la cadena está
        /// rota (un carácter fuera de rango o un valor cortado a medias).
        /// </summary>
        public static IReadOnlyList<Coordenada> Decodificar(string cadena, int precision)
        {
            if (precision < 0 || precision > 10) throw new ArgumentOutOfRangeException(nameof(precision));
            var puntos = new List<Coordenada>();
            if (string.IsNullOrEmpty(cadena)) return puntos;
            double factor = Math.Pow(10, precision);
            int i = 0;
            long lat = 0, lon = 0;
            while (i < cadena.Length)
            {
                lat += Valor(cadena, ref i);
                lon += Valor(cadena, ref i);
                puntos.Add(new Coordenada(lat / factor, lon / factor));
            }
            return puntos;
        }

        static long Valor(string cadena, ref int i)
        {
            long resultado = 0;
            int desplazamiento = 0;
            while (true)
            {
                if (i >= cadena.Length) throw new FormatException("Polilínea cortada a medias.");
                int b = cadena[i++] - 63;
                if (b < 0 || b > 63) throw new FormatException($"Carácter no válido en la polilínea: '{cadena[i - 1]}'.");
                if (desplazamiento > 60) throw new FormatException("Valor demasiado largo en la polilínea.");
                resultado |= (long)(b & 0x1F) << desplazamiento;
                desplazamiento += 5;
                if (b < 0x20) break;
            }
            return (resultado & 1) != 0 ? ~(resultado >> 1) : resultado >> 1;
        }
    }
}
