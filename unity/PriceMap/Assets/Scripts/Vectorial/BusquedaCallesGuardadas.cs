using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Calles guardadas (spec buscar-calles-guardadas): ICallesGuardadas sobre las teselas que el mapa ya guardó en disco
    /// (persistentDataPath/vectorial/14/x/y.pbf). Los nombres salen de la capa transportation_name y las localidades
    /// (city, town y village) de la capa place. Cada tesela se lee una vez: en cada búsqueda solo se leen las nuevas
    /// o cambiadas. Nunca descarga nada y todo corre fuera del hilo principal. Una tesela ilegible se salta con un aviso.
    /// </summary>
    public sealed class BusquedaCallesGuardadas : ICallesGuardadas
    {
        const string CapaNombres = "transportation_name", CapaLocalidades = "place";
        static readonly HashSet<string> Capas = new HashSet<string> { CapaNombres, CapaLocalidades };
        // Con nombre, pero no son calles
        static readonly HashSet<string> ClasesFuera = new HashSet<string> { "ferry", "rail", "transit", "raceway", "aerialway" };
        static readonly HashSet<string> ClasesLocalidad = new HashSet<string> { "city", "town", "village" };
        const int Extension = RutasCallesGuardadas.Extension;

        class TeselaLeida
        {
            public long Bytes;
            public DateTime Cambiada;
            public readonly List<TramoCalle> Tramos = new List<TramoCalle>();
            public readonly List<LocalidadMapa> Localidades = new List<LocalidadMapa>();
        }

        readonly string carpeta;
        readonly int zoom;
        // Todo lo de debajo, con el candado: las búsquedas van de una en una
        readonly object candado = new object();
        readonly SortedDictionary<string, TeselaLeida> teselas = new SortedDictionary<string, TeselaLeida>(StringComparer.Ordinal);
        readonly HashSet<string> avisadas = new HashSet<string>();
        IReadOnlyList<CalleGuardada> calles; // null: hay que volver a juntarlas

        /// <param name="carpetaDatos">Application.persistentDataPath, leído en el hilo principal (Unity no deja leerlo fuera).</param>
        /// <param name="zoom">Zoom con el que el mapa guarda las teselas (MapaVectorial.zoom).</param>
        public BusquedaCallesGuardadas(string carpetaDatos, int zoom = RutasCallesGuardadas.Zoom)
        {
            this.zoom = zoom;
            carpeta = Path.Combine(carpetaDatos ?? "", "vectorial", zoom.ToString(CultureInfo.InvariantCulture));
        }

        public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct) =>
            Task.Run(() => BuscarAhora(consulta, cerca, ct), ct);

        /// <summary>Lo que hace Buscar, en el hilo que llama (las pruebas lo usan directamente).</summary>
        public Resultado<IReadOnlyList<Direccion>> BuscarAhora(string consulta, Coordenada cerca, CancellationToken ct)
        {
            lock (candado)
            {
                ct.ThrowIfCancellationRequested(); // mientras esperaba ha podido llegar otra tecla
                Actualizar(ct);
                return Resultado<IReadOnlyList<Direccion>>.Exito(ReglaCalles.Buscar(calles, consulta, cerca, ct));
            }
        }

        // Lee las teselas nuevas o cambiadas, olvida las borradas y, si algo ha cambiado, vuelve a juntar las calles
        void Actualizar(CancellationToken ct)
        {
            var vistas = new HashSet<string>();
            if (Directory.Exists(carpeta))
                foreach (var fichero in new DirectoryInfo(carpeta).GetFiles("*.pbf", SearchOption.AllDirectories))
                {
                    if (!int.TryParse(fichero.Directory?.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int x)
                        || !int.TryParse(Path.GetFileNameWithoutExtension(fichero.Name), NumberStyles.None, CultureInfo.InvariantCulture, out int y))
                        continue;
                    ct.ThrowIfCancellationRequested(); // lo ya leído se queda para la siguiente
                    LeerSiHaCambiado(fichero, x, y);
                    vistas.Add(fichero.FullName);
                }
            List<string> borradas = null;
            foreach (var ruta in teselas.Keys)
                if (!vistas.Contains(ruta)) (borradas ??= new List<string>()).Add(ruta);
            if (borradas != null)
            {
                foreach (var ruta in borradas) teselas.Remove(ruta);
                calles = null;
            }
            if (calles != null) return;
            var tramos = new List<TramoCalle>();
            var localidades = new List<LocalidadMapa>();
            foreach (var t in teselas.Values)
            {
                tramos.AddRange(t.Tramos);
                localidades.AddRange(t.Localidades);
            }
            calles = ReglaCalles.Juntar(tramos, localidades);
        }

        void LeerSiHaCambiado(FileInfo fichero, int x, int y)
        {
            string ruta = fichero.FullName;
            TeselaLeida nueva = null;
            try
            {
                // Tamaño y fecha de antes de leer: si el mapa la estaba escribiendo, la próxima vez habrán cambiado
                nueva = new TeselaLeida { Bytes = fichero.Length, Cambiada = fichero.LastWriteTimeUtc };
                if (teselas.TryGetValue(ruta, out var t) && t.Bytes == nueva.Bytes && t.Cambiada == nueva.Cambiada) return;
                Leer(File.ReadAllBytes(ruta), x, y, zoom, nueva.Tramos, nueva.Localidades);
                avisadas.Remove(ruta);
            }
            catch (Exception e)
            {
                if (avisadas.Add(ruta))
                    Debug.LogWarning($"PriceMap: tesela guardada {zoom}/{x}/{y} ilegible para buscar calles: {e.Message}");
                if (nueva == null || e is IOException || e is UnauthorizedAccessException) return; // no se pudo abrir: otra vez será
                nueva.Tramos.Clear(); // dañada: no se vuelve a leer mientras no cambie
                nueva.Localidades.Clear();
            }
            teselas[ruta] = nueva;
            calles = null;
        }

        /// <summary>
        /// Calles con nombre y localidades de una tesela (bytes ya descomprimidos). Las calles se recortan al borde de la
        /// tesela: lo que sobresale en el margen está también en la vecina.
        /// </summary>
        public static void Leer(byte[] datos, int tx, int ty, int zoom, List<TramoCalle> tramos, List<LocalidadMapa> localidades)
        {
            var capas = LectorMVT.Leer(datos, Capas);
            if (capas.TryGetValue(CapaNombres, out var nombres))
            {
                double escala = Escala(nombres);
                foreach (var e in nombres.elementos)
                {
                    string nombre = e.Texto("name"), clase = e.Texto("class");
                    if (e.tipo != TipoGeometria.Linea || string.IsNullOrWhiteSpace(nombre)
                        || (clase != null && ClasesFuera.Contains(clase))) continue;
                    foreach (var parte in e.partes)
                        foreach (var trozo in Recortar(parte, escala))
                        {
                            var puntos = new Coordenada[trozo.Count];
                            for (int i = 0; i < puntos.Length; i++) puntos[i] = Punto(tx, ty, trozo[i].x, trozo[i].y, zoom);
                            tramos.Add(new TramoCalle(nombre, puntos));
                        }
                }
            }
            if (capas.TryGetValue(CapaLocalidades, out var lugares))
            {
                double escala = Escala(lugares);
                foreach (var e in lugares.elementos)
                {
                    string nombre = e.Texto("name"), clase = e.Texto("class");
                    if (e.tipo != TipoGeometria.Punto || string.IsNullOrWhiteSpace(nombre) || clase == null
                        || !ClasesLocalidad.Contains(clase) || e.partes.Count == 0 || e.partes[0].Count == 0) continue;
                    var p = e.partes[0][0];
                    localidades.Add(new LocalidadMapa(nombre.Trim(), Punto(tx, ty, p.x * escala, p.y * escala, zoom)));
                }
            }
        }

        static double Escala(CapaMVT capa) => capa.extension > 0 ? (double)Extension / capa.extension : 1.0;

        static Coordenada Punto(int tx, int ty, double x, double y, int zoom) =>
            RutasCallesGuardadas.ACoordenada((double)tx * Extension + x, (double)ty * Extension + y, zoom);

        // Los trozos de la línea que caen dentro de la tesela (0–4096), con el corte en el borde
        static List<List<(double x, double y)>> Recortar(List<Vector2> linea, double escala)
        {
            var trozos = new List<List<(double x, double y)>>();
            List<(double x, double y)> trozo = null;
            for (int i = 1; i < linea.Count; i++)
            {
                double ax = linea[i - 1].x * escala, ay = linea[i - 1].y * escala;
                double dx = linea[i].x * escala - ax, dy = linea[i].y * escala - ay;
                if (dx == 0 && dy == 0) continue; // un vértice repetido no parte la línea
                double desde = 0, hasta = 1;
                if (!Corte(-dx, ax, ref desde, ref hasta) || !Corte(dx, Extension - ax, ref desde, ref hasta)
                    || !Corte(-dy, ay, ref desde, ref hasta) || !Corte(dy, Extension - ay, ref desde, ref hasta) || hasta <= desde)
                {
                    trozo = null;
                    continue;
                }
                if (trozo == null || desde > 0)
                {
                    trozo = new List<(double x, double y)> { desde > 0 ? (ax + dx * desde, ay + dy * desde) : (ax, ay) };
                    trozos.Add(trozo);
                }
                trozo.Add(hasta < 1 ? (ax + dx * hasta, ay + dy * hasta) : (ax + dx, ay + dy));
                if (hasta < 1) trozo = null;
            }
            return trozos;
        }

        // Un borde del recorte de Liang–Barsky; falso si el segmento queda entero fuera
        static bool Corte(double p, double q, ref double desde, ref double hasta)
        {
            if (p == 0) return q >= 0;
            double r = q / p;
            if (p < 0)
            {
                if (r > hasta) return false;
                if (r > desde) desde = r;
            }
            else
            {
                if (r < desde) return false;
                if (r < hasta) hasta = r;
            }
            return true;
        }
    }
}
