using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Ruta aproximada sin conexión (historia 2.6, parte 2b): IRutas que no sale a la red. Lee las teselas que el mapa ya
    /// guardó en disco (persistentDataPath/vectorial/14/x/y.pbf) del rectángulo origen–destino más una tesela de margen
    /// (como mucho 5 × 5), toma las calles de la capa transportation que valen para el modo (GrafoCalles.Admite), pasa
    /// sus vértices a lat/lon con la fórmula de las teselas y calcula el camino más corto con GrafoCalles. Nunca descarga
    /// nada. Todo corre fuera del hilo principal y se puede cancelar. Sin calles que unan los dos puntos, Fallo.
    /// </summary>
    public sealed class RutasCallesGuardadas : IRutas
    {
        /// <summary>Zoom de las teselas guardadas por defecto (el del mapa, MapaVectorial.zoom).</summary>
        public const int Zoom = 14;
        /// <summary>Rejilla de cada tesela: las claves de los vértices se cuentan en ella (4096, la de OpenMapTiles).</summary>
        public const int Extension = 4096;

        readonly string carpeta;
        readonly int zoom;

        /// <param name="carpetaDatos">Application.persistentDataPath, leído en el hilo principal (Unity no deja leerlo fuera).</param>
        /// <param name="zoom">Zoom con el que el mapa guarda las teselas (MapaVectorial.zoom): carpeta, teselas y rejilla.</param>
        public RutasCallesGuardadas(string carpetaDatos, int zoom = Zoom)
        {
            this.zoom = zoom;
            carpeta = Path.Combine(carpetaDatos ?? "", "vectorial", zoom.ToString());
        }

        /// <summary>Zoom de las teselas que lee.</summary>
        public int ZoomTeselas => zoom;

        public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct) =>
            Task.Run(() => CalcularAhora(origen, destino, modo, ct), ct);

        /// <summary>Lo que hace Calcular, en el hilo que llama (las pruebas lo usan directamente).</summary>
        public Resultado<Ruta> CalcularAhora(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var zona = Teselas(origen, destino, zoom);
            if (zona == null) return Resultado<Ruta>.Fallo("El destino está demasiado lejos para la ruta aproximada.");
            var (x0, y0, x1, y1) = zona.Value;
            var grafo = new GrafoCalles();
            int leidas = 0;
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    ct.ThrowIfCancellationRequested();
                    string ruta = Path.Combine(carpeta, x.ToString(), y + ".pbf");
                    if (!File.Exists(ruta)) continue;
                    try
                    {
                        AnadirTesela(grafo, File.ReadAllBytes(ruta), x, y, modo, zoom);
                        leidas++;
                    }
                    catch (Exception e) when (!(e is OperationCanceledException))
                    {
                        Debug.LogWarning($"PriceMap: tesela guardada {zoom}/{x}/{y} ilegible para la ruta aproximada: {e.Message}");
                    }
                }
            if (leidas == 0) return Resultado<Ruta>.Fallo("No hay calles guardadas en esta zona.");
            var r = grafo.Calcular(origen, destino, modo, ct);
            return r != null
                ? Resultado<Ruta>.Exito(r)
                : Resultado<Ruta>.Fallo("No hay calles guardadas que unan el origen y el destino.");
        }

        /// <summary>
        /// Añade al grafo las calles de una tesela (bytes ya descomprimidos) que valen para el modo, con las claves
        /// globales (x · 4096 + px, y · 4096 + py al zoom de las teselas, 14 por defecto).
        /// </summary>
        public static void AnadirTesela(GrafoCalles grafo, byte[] datos, int tx, int ty, ModoViaje modo, int zoom = Zoom)
        {
            var capas = LectorMVT.Leer(datos);
            if (!capas.TryGetValue("transportation", out var calles)) return;
            double escala = calles.extension > 0 ? (double)Extension / calles.extension : 1.0;
            var puntos = new List<PuntoCalle>();
            foreach (var e in calles.elementos)
            {
                if (e.tipo != TipoGeometria.Linea || !GrafoCalles.Admite(e.Texto("class"), modo)) continue;
                foreach (var parte in e.partes)
                {
                    if (parte.Count < 2) continue;
                    puntos.Clear();
                    foreach (var p in parte)
                    {
                        long gx = (long)tx * Extension + (long)Math.Round(p.x * escala);
                        long gy = (long)ty * Extension + (long)Math.Round(p.y * escala);
                        puntos.Add(new PuntoCalle(Clave(gx, gy), ACoordenada(gx, gy, zoom)));
                    }
                    grafo.AnadirCalle(puntos);
                }
            }
        }

        /// <summary>Clave del vértice: sus dos enteros globales juntos.</summary>
        public static long Clave(long gx, long gy) => (gx << 32) ^ (gy & 0xFFFFFFFFL);

        /// <summary>Lado del mundo en la rejilla global (4096 · 2^zoom).</summary>
        static double Mundo(int zoom) => (double)Extension * (1 << zoom);

        /// <summary>Punto de la rejilla global (4096 por tesela al zoom dado) a lat/lon (Web Mercator inversa).</summary>
        public static Coordenada ACoordenada(double gx, double gy, int zoom = Zoom)
        {
            double mundo = Mundo(zoom);
            double lon = gx / mundo * 360.0 - 180.0;
            double n = Math.PI * (1 - 2 * gy / mundo);
            double lat = Math.Atan(Math.Sinh(n)) * 180.0 / Math.PI;
            return new Coordenada(lat, lon);
        }

        /// <summary>Tesela (al zoom dado, 14 por defecto) que contiene la coordenada.</summary>
        public static (int x, int y) Tesela(Coordenada c, int zoom = Zoom)
        {
            int n = 1 << zoom;
            double latRad = c.Latitud * Math.PI / 180.0;
            int x = (int)Math.Floor((c.Longitud + 180.0) / 360.0 * n);
            int y = (int)Math.Floor((1 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2 * n);
            return (x, y);
        }

        /// <summary>
        /// Teselas que se leen: el rectángulo de las del origen y el destino más una de margen por cada lado, recortado a
        /// 5 por lado (se quita margen por igual); null si el origen y el destino ya están a más de 5 teselas.
        /// </summary>
        public static (int x0, int y0, int x1, int y1)? Teselas(Coordenada origen, Coordenada destino, int zoom = Zoom)
        {
            var a = Tesela(origen, zoom);
            var b = Tesela(destino, zoom);
            var x = Lado(Math.Min(a.x, b.x), Math.Max(a.x, b.x));
            var y = Lado(Math.Min(a.y, b.y), Math.Max(a.y, b.y));
            if (x == null || y == null) return null;
            return (x.Value.desde, y.Value.desde, x.Value.hasta, y.Value.hasta);
        }

        static (int desde, int hasta)? Lado(int min, int max)
        {
            int maximo = ParametrosNegocio.RutaAproximadaTeselasLado;
            if (max - min + 1 > maximo) return null;
            int desde = min - 1, hasta = max + 1;
            while (hasta - desde + 1 > maximo)
            {
                if (hasta - max >= min - desde) hasta--;
                else desde++;
            }
            return (desde, hasta);
        }
    }
}
