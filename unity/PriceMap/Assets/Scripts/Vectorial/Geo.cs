using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Conversión entre coordenadas geográficas (latitud/longitud, WGS84) y la escena de Unity.
    /// Se usa la proyección Web Mercator (la de OpenStreetMap). 1 unidad de Unity = 1 metro real
    /// alrededor del origen. Eje +X = este, eje +Z = norte, eje +Y = arriba.
    /// </summary>
    public static class Geo
    {
        public const double RadioTierra = 6378137.0; // metros (WGS84)

        // Origen de la escena: Plaza del Cabildo, Sanlúcar de Barrameda (posición simulada del usuario en el prototipo web)
        public static double LatitudOrigen = 36.7779;
        public static double LongitudOrigen = -6.3528;

        static double MercX(double lon) => RadioTierra * lon * Mathf.Deg2Rad;
        static double MercY(double lat) => RadioTierra * System.Math.Log(System.Math.Tan(System.Math.PI / 4 + lat * Mathf.Deg2Rad / 2));

        /// <summary>Factor de escala de Mercator en el origen (metros reales por metro de Mercator).</summary>
        public static double Escala => System.Math.Cos(LatitudOrigen * Mathf.Deg2Rad);

        /// <summary>Latitud/longitud → posición en la escena (metros respecto al origen).</summary>
        public static Vector3 ALocal(double lat, double lon, float altura = 0f)
        {
            double x = (MercX(lon) - MercX(LongitudOrigen)) * Escala;
            double z = (MercY(lat) - MercY(LatitudOrigen)) * Escala;
            return new Vector3((float)x, altura, (float)z);
        }

        /// <summary>Posición en la escena → latitud/longitud.</summary>
        public static Vector2d AGeo(Vector3 local)
        {
            double mx = local.x / Escala + MercX(LongitudOrigen);
            double my = local.z / Escala + MercY(LatitudOrigen);
            double lon = mx / RadioTierra * Mathf.Rad2Deg;
            double lat = (2 * System.Math.Atan(System.Math.Exp(my / RadioTierra)) - System.Math.PI / 2) * Mathf.Rad2Deg;
            return new Vector2d(lat, lon);
        }

        // ---------- Teselas (esquema XYZ de OpenStreetMap) ----------
        public static double TamTeselaMercator(int zoom) => 2 * System.Math.PI * RadioTierra / (1 << zoom);

        public static Vector2Int Tesela(double lat, double lon, int zoom)
        {
            int n = 1 << zoom;
            int x = (int)System.Math.Floor((lon + 180.0) / 360.0 * n);
            double latRad = lat * Mathf.Deg2Rad;
            int y = (int)System.Math.Floor((1 - System.Math.Log(System.Math.Tan(latRad) + 1 / System.Math.Cos(latRad)) / System.Math.PI) / 2 * n);
            return new Vector2Int(x, y);
        }

        /// <summary>Centro de la tesela (x, y, zoom) en la escena, y su lado en metros de la escena.</summary>
        public static Vector3 CentroTesela(int x, int y, int zoom, out float lado)
        {
            double t = TamTeselaMercator(zoom);
            double mx = -System.Math.PI * RadioTierra + (x + 0.5) * t;
            double my = System.Math.PI * RadioTierra - (y + 0.5) * t;
            lado = (float)(t * Escala);
            return new Vector3((float)((mx - MercX(LongitudOrigen)) * Escala), 0f, (float)((my - MercY(LatitudOrigen)) * Escala));
        }
    }

    /// <summary>Par latitud/longitud en doble precisión.</summary>
    [System.Serializable]
    public struct Vector2d
    {
        public double lat, lon;
        public Vector2d(double lat, double lon) { this.lat = lat; this.lon = lon; }
        public override string ToString() => $"{lat:F6}, {lon:F6}";
    }
}
