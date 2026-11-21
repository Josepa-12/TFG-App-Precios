using System;
using System.Collections.Generic;
using System.Threading;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Vértice de una calle guardada: su clave entera (la coordenada global de la tesela, igual en las teselas vecinas
    /// para el mismo vértice de OSM) y su posición real.
    /// </summary>
    public readonly struct PuntoCalle
    {
        public long Clave { get; }
        public Coordenada Posicion { get; }

        public PuntoCalle(long clave, Coordenada posicion)
        {
            Clave = clave;
            Posicion = posicion;
        }
    }

    /// <summary>
    /// Grafo de las calles guardadas para la ruta aproximada sin conexión (historia 2.6, parte 2b). Nodos por clave
    /// exacta (los vértices compartidos entre tramos y teselas coinciden), una arista por tramo, sin sentidos únicos ni
    /// giros prohibidos. Un extremo suelto a ≤ 5 m de otro tramo se une a él (cruces que la generalización deja sin
    /// vértice, y los cortes en el margen de cada tesela). El origen y el destino van al punto más cercano de un tramo a
    /// ≤ 300 m, y el camino más corto por metros sale de A*. Las distancias se miden en un plano local en metros
    /// (equirrectangular alrededor del primer punto), de sobra para unos pocos kilómetros. Puro, sin Unity.
    /// </summary>
    public sealed class GrafoCalles
    {
        const double Grados = Math.PI / 180.0;
        /// <summary>Lado (m) de las celdas del índice de tramos.</summary>
        const double Celda = 50.0;

        struct Arista
        {
            public int A, B;
            public bool Viva;
        }

        readonly double metrosUnion;
        readonly double metrosCalle;
        readonly List<double> xs = new List<double>(), ys = new List<double>();
        readonly List<Coordenada> posiciones = new List<Coordenada>();
        readonly Dictionary<long, int> porClave = new Dictionary<long, int>();
        readonly List<Arista> aristas = new List<Arista>();
        readonly HashSet<long> pares = new HashSet<long>();
        readonly Dictionary<long, List<int>> celdas = new Dictionary<long, List<int>>();
        double cosRef = 1.0;
        bool conReferencia, unido;
        int vivas;

        /// <param name="metrosUnion">Distancia a la que un extremo suelto se une a otro tramo (por defecto, 5 m).</param>
        /// <param name="metrosCalle">Distancia máxima del origen o del destino a un tramo (por defecto, 300 m).</param>
        public GrafoCalles(double metrosUnion = ParametrosNegocio.RutaAproximadaUnionMetros,
            double metrosCalle = ParametrosNegocio.RutaAproximadaCalleMetros)
        {
            this.metrosUnion = metrosUnion;
            this.metrosCalle = metrosCalle;
        }

        public int Nodos => posiciones.Count;
        public int Aristas => vivas;

        /// <summary>Transporte (vías, barcos, teleféricos): ninguno de los dos modos va por ahí.</summary>
        static readonly HashSet<string> Transporte = new HashSet<string> { "ferry", "rail", "transit", "aerialway" };
        /// <summary>Clases por las que no va el coche, además del transporte.</summary>
        static readonly HashSet<string> SinCoche = new HashSet<string> { "path", "track", "raceway", "busway", "bus_guideway" };
        /// <summary>Clases por las que no se va a pie, además del transporte.</summary>
        static readonly HashSet<string> SinPie = new HashSet<string> { "motorway", "trunk" };

        /// <summary>
        /// Si una calle de esa clase (atributo class de la capa transportation de OpenMapTiles) vale para ese modo: en
        /// coche, todas salvo path, track, raceway, busway, bus_guideway y el transporte; a pie, todas salvo motorway,
        /// trunk y el transporte. Las que están en obras (*_construction) y sin clase, no valen.
        /// </summary>
        public static bool Admite(string clase, ModoViaje modo)
        {
            if (string.IsNullOrEmpty(clase) || Transporte.Contains(clase)) return false;
            if (clase.EndsWith("_construction", StringComparison.Ordinal)) return false; // aún sin abrir
            return modo == ModoViaje.APie ? !SinPie.Contains(clase) : !SinCoche.Contains(clase);
        }

        /// <summary>Velocidad (m/s) con la que se calcula el tiempo de la ruta aproximada.</summary>
        public static double VelocidadMetrosSegundo(ModoViaje modo) =>
            (modo == ModoViaje.APie ? ParametrosNegocio.RutaAproximadaPieKmh : ParametrosNegocio.RutaAproximadaCocheKmh) / 3.6;

        /// <summary>Añade una calle: una arista por cada tramo entre dos vértices seguidos (los repetidos no se duplican).</summary>
        public void AnadirCalle(IReadOnlyList<PuntoCalle> puntos)
        {
            if (puntos == null) return;
            unido = false;
            int anterior = -1;
            foreach (var p in puntos)
            {
                int n = Nodo(p);
                if (anterior >= 0) AnadirArista(anterior, n);
                anterior = n;
            }
        }

        int Nodo(PuntoCalle p)
        {
            if (porClave.TryGetValue(p.Clave, out int n)) return n;
            n = NodoNuevo(p.Posicion);
            porClave[p.Clave] = n;
            return n;
        }

        int NodoNuevo(Coordenada c)
        {
            if (!conReferencia)
            {
                conReferencia = true;
                cosRef = Math.Cos(c.Latitud * Grados);
            }
            var (x, y) = Plano(c);
            xs.Add(x);
            ys.Add(y);
            posiciones.Add(c);
            return posiciones.Count - 1;
        }

        (double x, double y) Plano(Coordenada c) =>
            (c.Longitud * Grados * Coordenada.RadioTierraMetros * cosRef, c.Latitud * Grados * Coordenada.RadioTierraMetros);

        void AnadirArista(int a, int b)
        {
            if (a == b) return;
            long par = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (!pares.Add(par)) return;
            int id = aristas.Count;
            aristas.Add(new Arista { A = a, B = b, Viva = true });
            vivas++;
            long cx0 = CeldaDe(Math.Min(xs[a], xs[b])), cx1 = CeldaDe(Math.Max(xs[a], xs[b]));
            long cy0 = CeldaDe(Math.Min(ys[a], ys[b])), cy1 = CeldaDe(Math.Max(ys[a], ys[b]));
            for (long cx = cx0; cx <= cx1; cx++)
                for (long cy = cy0; cy <= cy1; cy++)
                {
                    long k = ClaveCelda(cx, cy);
                    if (!celdas.TryGetValue(k, out var lista)) celdas[k] = lista = new List<int>();
                    lista.Add(id);
                }
        }

        void Matar(int id)
        {
            var e = aristas[id];
            if (!e.Viva) return;
            e.Viva = false;
            aristas[id] = e;
            vivas--;
            long par = e.A < e.B ? ((long)e.A << 32) | (uint)e.B : ((long)e.B << 32) | (uint)e.A;
            pares.Remove(par);
        }

        static long CeldaDe(double v) => (long)Math.Floor(v / Celda);
        static long ClaveCelda(long cx, long cy) => (cx << 32) ^ (cy & 0xFFFFFFFFL);

        /// <summary>
        /// El tramo vivo más cercano al punto (x, y) a ≤ radio m, sin contar los que tocan el nodo excluido: el id de la
        /// arista, la posición en ella (0 en A, 1 en B) y la distancia. -1 si no hay ninguno.
        /// </summary>
        (int arista, double t, double distancia) Cercano(double x, double y, double radio, int excluido)
        {
            int mejor = -1;
            double mejorT = 0, mejorD = double.MaxValue;
            long cx0 = CeldaDe(x - radio), cx1 = CeldaDe(x + radio), cy0 = CeldaDe(y - radio), cy1 = CeldaDe(y + radio);
            for (long cx = cx0; cx <= cx1; cx++)
                for (long cy = cy0; cy <= cy1; cy++)
                {
                    if (!celdas.TryGetValue(ClaveCelda(cx, cy), out var lista)) continue;
                    foreach (int id in lista)
                    {
                        var e = aristas[id];
                        if (!e.Viva || e.A == excluido || e.B == excluido) continue;
                        double ax = xs[e.A], ay = ys[e.A], dx = xs[e.B] - ax, dy = ys[e.B] - ay;
                        double l2 = dx * dx + dy * dy;
                        double t = l2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * dx + (y - ay) * dy) / l2)) : 0;
                        double px = ax + t * dx - x, py = ay + t * dy - y;
                        double d = Math.Sqrt(px * px + py * py);
                        if (d < mejorD || (d == mejorD && id < mejor))
                        {
                            mejor = id;
                            mejorT = t;
                            mejorD = d;
                        }
                    }
                }
            return mejorD <= radio ? (mejor, mejorT, mejorD) : (-1, 0, 0);
        }

        /// <summary>Parte la arista en t y devuelve el nodo de ese punto (uno de sus extremos si cae a menos de 1 cm).</summary>
        int Partir(int id, double t)
        {
            var e = aristas[id];
            double largo = Largo(e.A, e.B);
            if (t * largo < 0.01) return e.A;
            if ((1 - t) * largo < 0.01) return e.B;
            var a = posiciones[e.A];
            var b = posiciones[e.B];
            int p = NodoNuevo(new Coordenada(a.Latitud + (b.Latitud - a.Latitud) * t, a.Longitud + (b.Longitud - a.Longitud) * t));
            Matar(id);
            AnadirArista(e.A, p);
            AnadirArista(p, e.B);
            return p;
        }

        double Largo(int a, int b)
        {
            double dx = xs[b] - xs[a], dy = ys[b] - ys[a];
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Une cada extremo suelto (un nodo con un solo tramo) al tramo más cercano de otra calle a ≤ 5 m, partiéndolo
        /// si hace falta. Calcular lo hace solo la primera vez (con su cancelación, mirada cada 1024 nodos).
        /// </summary>
        public void UnirExtremosSueltos() => UnirExtremosSueltos(CancellationToken.None);

        public void UnirExtremosSueltos(CancellationToken ct)
        {
            unido = true;
            var grado = new int[posiciones.Count];
            foreach (var e in aristas)
                if (e.Viva) { grado[e.A]++; grado[e.B]++; }
            for (int n = 0; n < grado.Length; n++)
            {
                if ((n & 1023) == 0) ct.ThrowIfCancellationRequested();
                if (grado[n] != 1) continue;
                var (id, t, _) = Cercano(xs[n], ys[n], metrosUnion, n);
                if (id < 0) continue;
                AnadirArista(n, Partir(id, t));
            }
        }

        /// <summary>
        /// La ruta aproximada más corta por metros entre los puntos de calle más cercanos al origen y al destino (a ≤ 300
        /// m), con el tiempo a la velocidad del modo y sin maniobras; null si alguno está lejos de toda calle o no hay
        /// camino que los una. Cancelar termina en OperationCanceledException.
        /// </summary>
        public Ruta Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!unido) UnirExtremosSueltos(ct);
            if (vivas == 0) return null;
            int o = Proyectar(origen);
            if (o < 0) return null;
            int d = Proyectar(destino);
            if (d < 0) return null;
            var camino = AEstrella(o, d, ct);
            if (camino == null) return null;
            if (camino.Count == 1) camino.Add(camino[0]);
            var puntos = new List<Coordenada>(camino.Count);
            double metros = 0;
            for (int i = 0; i < camino.Count; i++)
            {
                puntos.Add(posiciones[camino[i]]);
                if (i > 0) metros += Largo(camino[i - 1], camino[i]);
            }
            return new Ruta(puntos, metros, metros / VelocidadMetrosSegundo(modo), Array.Empty<Maniobra>(), aproximada: true);
        }

        /// <summary>Nodo del punto de calle más cercano a la coordenada (partiendo el tramo); -1 si no hay a ≤ 300 m.</summary>
        int Proyectar(Coordenada c)
        {
            var (x, y) = Plano(c);
            var (id, t, _) = Cercano(x, y, metrosCalle, -1);
            return id < 0 ? -1 : Partir(id, t);
        }

        List<int> AEstrella(int o, int d, CancellationToken ct)
        {
            int n = posiciones.Count;
            var vecinos = new List<int>[n];
            foreach (var e in aristas)
            {
                if (!e.Viva) continue;
                (vecinos[e.A] ??= new List<int>()).Add(e.B);
                (vecinos[e.B] ??= new List<int>()).Add(e.A);
            }
            var g = new double[n];
            var previo = new int[n];
            var cerrado = new bool[n];
            for (int i = 0; i < n; i++) { g[i] = double.PositiveInfinity; previo[i] = -1; }
            g[o] = 0;
            var abiertos = new Monticulo();
            abiertos.Meter(Largo(o, d), o);
            int pasos = 0;
            while (abiertos.Cuantos > 0)
            {
                if ((++pasos & 1023) == 0) ct.ThrowIfCancellationRequested();
                int u = abiertos.Sacar();
                if (cerrado[u]) continue;
                if (u == d) break;
                cerrado[u] = true;
                var vs = vecinos[u];
                if (vs == null) continue;
                foreach (int v in vs)
                {
                    if (cerrado[v]) continue;
                    double nuevo = g[u] + Largo(u, v);
                    if (nuevo >= g[v]) continue;
                    g[v] = nuevo;
                    previo[v] = u;
                    abiertos.Meter(nuevo + Largo(v, d), v);
                }
            }
            if (double.IsPositiveInfinity(g[d])) return null;
            var camino = new List<int>();
            for (int v = d; v >= 0; v = previo[v]) camino.Add(v);
            camino.Reverse();
            return camino;
        }

        /// <summary>Montículo binario de mínimos (prioridad, nodo); los repetidos se descartan al sacarlos.</summary>
        sealed class Monticulo
        {
            readonly List<(double p, int n)> datos = new List<(double, int)>();
            public int Cuantos => datos.Count;

            public void Meter(double prioridad, int nodo)
            {
                datos.Add((prioridad, nodo));
                int i = datos.Count - 1;
                while (i > 0)
                {
                    int padre = (i - 1) / 2;
                    if (datos[padre].p <= datos[i].p) break;
                    (datos[padre], datos[i]) = (datos[i], datos[padre]);
                    i = padre;
                }
            }

            public int Sacar()
            {
                int r = datos[0].n;
                int ultimo = datos.Count - 1;
                datos[0] = datos[ultimo];
                datos.RemoveAt(ultimo);
                int i = 0;
                while (true)
                {
                    int a = 2 * i + 1, b = a + 1, m = i;
                    if (a < datos.Count && datos[a].p < datos[m].p) m = a;
                    if (b < datos.Count && datos[b].p < datos[m].p) m = b;
                    if (m == i) break;
                    (datos[m], datos[i]) = (datos[i], datos[m]);
                    i = m;
                }
                return r;
            }
        }
    }
}
