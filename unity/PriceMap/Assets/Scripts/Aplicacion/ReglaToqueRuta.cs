using System;
using System.Collections.Generic;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Qué ruta de «Cómo llegar» elige un toque en el mapa (parte 5b), con sus líneas ya proyectadas en la pantalla.
    /// Lógica pura.
    /// </summary>
    public static class ReglaToqueRuta
    {
        /// <summary>
        /// Índice de la línea más cercana al toque, a ese radio o menos; −1 si ninguna. Si la elegida está, con el margen
        /// de empate, igual de cerca (un tramo común), gana ella.
        /// </summary>
        public static int MasCercana(PuntoPantalla toque, IReadOnlyList<IReadOnlyList<PuntoPantalla>> lineas, int elegida,
            float radioPx, float empatePx)
        {
            int mejor = -1;
            float distancia = float.PositiveInfinity, deLaElegida = float.PositiveInfinity;
            for (int i = 0; lineas != null && i < lineas.Count; i++)
            {
                float d = Distancia(toque, lineas[i]);
                if (i == elegida) deLaElegida = d;
                if (d < distancia) { distancia = d; mejor = i; }
            }
            if (!(distancia <= radioPx)) return -1;
            return deLaElegida <= radioPx && deLaElegida - distancia <= empatePx ? elegida : mejor;
        }

        /// <summary>Píxeles del punto a la línea; los tramos con un extremo detrás de la cámara no cuentan.</summary>
        public static float Distancia(PuntoPantalla punto, IReadOnlyList<PuntoPantalla> linea)
        {
            float mejor = float.PositiveInfinity;
            for (int i = 1; linea != null && i < linea.Count; i++)
            {
                var a = linea[i - 1];
                var b = linea[i];
                if (!a.Delante || !b.Delante) continue;
                float dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
                float t = l2 > 0f ? Math.Max(0f, Math.Min(1f, ((punto.X - a.X) * dx + (punto.Y - a.Y) * dy) / l2)) : 0f;
                float px = a.X + t * dx - punto.X, py = a.Y + t * dy - punto.Y;
                float d = (float)Math.Sqrt(px * px + py * py);
                if (d < mejor) mejor = d;
            }
            return mejor;
        }
    }
}
