using System;
using System.Collections.Generic;
using System.Threading;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>Un trozo de calle de una tesela: su nombre y sus puntos en orden.</summary>
    public sealed class TramoCalle
    {
        public string Nombre { get; }
        public IReadOnlyList<Coordenada> Puntos { get; }

        public TramoCalle(string nombre, IReadOnlyList<Coordenada> puntos)
        {
            Nombre = nombre ?? "";
            Puntos = puntos ?? Array.Empty<Coordenada>();
        }
    }

    /// <summary>Una localidad del mapa (ciudad o pueblo) y dónde está su rótulo.</summary>
    public readonly struct LocalidadMapa
    {
        public string Nombre { get; }
        public Coordenada Posicion { get; }

        public LocalidadMapa(string nombre, Coordenada posicion)
        {
            Nombre = nombre ?? "";
            Posicion = posicion;
        }
    }

    /// <summary>
    /// Una calle de una localidad, con todos sus tramos juntos. Su punto medio (TrazadoCalle) se calcula la primera vez
    /// que se pide; no se debe pedir a la vez desde dos hilos.
    /// </summary>
    public sealed class CalleGuardada
    {
        readonly List<IReadOnlyList<Coordenada>> tramos = new List<IReadOnlyList<Coordenada>>();
        Direccion direccion;

        public string Nombre { get; }
        public string Localidad { get; }

        internal CalleGuardada(string nombre, string localidad)
        {
            Nombre = nombre;
            Localidad = localidad;
        }

        internal void Anadir(IReadOnlyList<Coordenada> tramo) => tramos.Add(tramo);

        /// <summary>La calle como una dirección sin número, en el medio de su trazado.</summary>
        public Direccion Direccion =>
            direccion ??= new Direccion(Nombre, Localidad, TrazadoCalle.PuntoMedio(tramos), calle: Nombre);
    }

    /// <summary>
    /// Reglas del buscador sobre las calles guardadas (spec buscar-calles-guardadas), sin Unity: juntar los tramos en
    /// calles, qué calles coinciden con lo escrito (la regla del buscador, además con la abreviatura del tipo de vía
    /// desplegada y sin el número del portal), su orden y las repetidas de Photon.
    /// </summary>
    public static class ReglaCalles
    {
        /// <summary>Calles que se muestran como mucho.</summary>
        public const int MaximoCalles = 8;
        /// <summary>Una dirección de Photon con el mismo nombre a menos de estos metros es la misma calle.</summary>
        public const double MetrosMismaCalle = 1000.0;

        // Abreviaturas del tipo de vía (ya normalizadas: «c/» es «c», «avda.» es «avda»)
        static readonly Dictionary<string, string> Abreviaturas = new Dictionary<string, string>
        {
            ["c"] = "calle", ["cl"] = "calle",
            ["av"] = "avenida", ["avd"] = "avenida", ["avda"] = "avenida",
            ["pl"] = "plaza", ["pza"] = "plaza", ["plza"] = "plaza",
            ["p"] = "paseo", ["pº"] = "paseo", ["po"] = "paseo", ["pso"] = "paseo",
            ["ctra"] = "carretera", ["crta"] = "carretera",
            ["cno"] = "camino", ["cmno"] = "camino",
            ["trv"] = "travesia", ["tva"] = "travesia", ["trva"] = "travesia",
            ["gta"] = "glorieta", ["rda"] = "ronda", ["cjon"] = "callejon", ["cllon"] = "callejon",
            ["urb"] = "urbanizacion", ["bda"] = "barriada",
        };

        // Tipos de vía: quitado el número, uno solo («calle 12» → «calle») no dice qué calle es
        static readonly HashSet<string> TiposVia = new HashSet<string>(Abreviaturas.Values)
        {
            "autovia", "autopista", "acceso", "bajada", "cuesta", "pasaje", "puente", "senda", "sendero", "vereda",
        };

        // Artículos y preposiciones: tampoco dicen qué calle es
        static readonly HashSet<string> Particulas = new HashSet<string>
        {
            "a", "al", "con", "de", "del", "e", "el", "en", "la", "las", "los", "por", "y",
        };

        // Delante del portal («nº 12», «portal 3»)
        static readonly HashSet<string> PalabrasNumero = new HashSet<string> { "n", "nº", "no", "num", "numero", "º", "portal" };
        // Y las que, sueltas al final, son un número a medio escribir
        static readonly HashSet<string> NumeroAMedias = new HashSet<string> { "nº", "num", "numero" };

        // Piso y puerta: solo cuentan detrás del portal
        static readonly HashSet<string> PalabrasPiso = new HashSet<string>
        {
            "bis", "bajo", "bajos", "bj", "atico", "entresuelo", "planta", "piso", "puerta", "pta", "escalera", "esc",
            "bloque", "blq", "izq", "izqda", "izda", "izquierda", "dcha", "der", "derecha", "duplicado", "local",
        };

        /// <summary>
        /// Formas de la consulta que se comparan con los nombres, normalizadas: lo escrito, con la abreviatura de la
        /// primera palabra desplegada («c farinas» → «calle farinas») y las dos sin el número del portal del final. Vacía
        /// si lo escrito no llega al mínimo de las direcciones (3 caracteres normalizados).
        /// </summary>
        public static IReadOnlyList<string> Formas(string consulta)
        {
            var formas = new List<string>(4);
            if (!BuscadorDirecciones.BastaParaPedir(consulta)) return formas;
            var palabras = ReglaBusqueda.Palabras(ReglaBusqueda.Normalizar(consulta));
            AnadirForma(formas, palabras, palabras.Length);
            int n = SinNumero(palabras);
            if (n < palabras.Length && !(n == 1 && EsTipoVia(palabras[0]))
                && string.Join(" ", palabras, 0, n).Length >= BuscadorDirecciones.MinimoCaracteres)
                AnadirForma(formas, palabras, n);
            return formas;
        }

        static void AnadirForma(List<string> formas, string[] palabras, int n)
        {
            string forma = string.Join(" ", palabras, 0, n);
            formas.Add(forma);
            if (Abreviaturas.TryGetValue(palabras[0], out var entera))
                formas.Add(entera + forma.Substring(palabras[0].Length));
        }

        static bool EsTipoVia(string palabra) => TiposVia.Contains(palabra) || Abreviaturas.ContainsKey(palabra);

        /// <summary>
        /// Cuántas palabras quedan al quitar del final el portal, con su piso y su puerta («12», «12 b», «nº12», «12 1º
        /// izda», «portal 3», «s n», «nº» a medio escribir); todas si no lo hay. Nunca deja la consulta vacía.
        /// </summary>
        static int SinNumero(string[] p)
        {
            int n = p.Length;
            if (n > 2 && p[n - 2] == "s" && p[n - 1] == "n") return n - 2; // «s/n»
            if (n > 1 && NumeroAMedias.Contains(p[n - 1])) n--;
            // El número más a la izquierda tras el que no hay nada más que números, pisos y puertas
            int total = n;
            for (int i = total - 1; i >= 1; i--)
            {
                if (EsNumero(p[i])) n = i;
                else if (!PalabrasPiso.Contains(p[i]) && !PalabrasNumero.Contains(p[i]) && !EsLetra(p[i])) break;
            }
            // Y lo que lleva delante («nº», «portal»), salvo que sea lo único que dice qué calle es
            while (n < total && n > 1 && PalabrasNumero.Contains(p[n - 1]) && !(n == 2 && EsTipoVia(p[0]))) n--;
            return n;
        }

        static bool EsLetra(string palabra) => palabra.Length == 1 && char.IsLetter(palabra[0]);

        // Cifras con «nº» o parecido delante y una letra o «bis» detrás como mucho: «12», «nº12», «12b», «2º», «12bis»
        static bool EsNumero(string palabra)
        {
            int desde = 0;
            while (desde < palabra.Length && !EsCifra(palabra[desde])) desde++;
            int hasta = desde;
            while (hasta < palabra.Length && EsCifra(palabra[hasta])) hasta++;
            if (hasta == desde || (desde > 0 && !PalabrasNumero.Contains(palabra.Substring(0, desde)))) return false;
            string resto = palabra.Substring(hasta);
            return resto.Length == 0 || resto == "bis" || EsLetra(resto);
        }

        static bool EsCifra(char c) => c >= '0' && c <= '9';

        /// <summary>
        /// Puntuación (ReglaBusqueda) de la calle con la mejor de las formas; NoCoincide si ninguna. Si el nombre solo no
        /// coincide, las palabras que le faltan pueden ser de la localidad («fariñas sanlucar»): entonces, Contiene.
        /// </summary>
        public static int Puntuar(IReadOnlyList<string> formas, string nombre, string localidad)
        {
            int mejor = ReglaBusqueda.NoCoincide;
            foreach (var f in formas)
                mejor = Math.Max(mejor, ReglaBusqueda.PuntuarNormalizada(f, ReglaBusqueda.Palabras(f), nombre));
            if (mejor != ReglaBusqueda.NoCoincide || string.IsNullOrEmpty(localidad)) return mejor;
            string[] deNombre = null;
            foreach (var f in formas)
            {
                var palabras = ReglaBusqueda.Palabras(f);
                if (palabras.Length < 2) continue;
                deNombre ??= ReglaBusqueda.Palabras(ReglaBusqueda.Normalizar(nombre));
                if (CoincideConLocalidad(palabras, deNombre, localidad)) return ReglaBusqueda.Contiene;
            }
            return mejor;
        }

        // Cada palabra es el principio de una del nombre o de la localidad (los números del portal no cuentan) y al menos
        // una dice qué calle es: no es un tipo de vía ni una partícula, y coincide con el nombre
        static bool CoincideConLocalidad(string[] palabras, string[] nombre, string localidad)
        {
            bool dice = false;
            foreach (var p in palabras)
                if (!EsTipoVia(p) && !Particulas.Contains(p) && Array.Exists(nombre, t => Empieza(t, p) && !EsTipoVia(t) && !Particulas.Contains(t)))
                {
                    dice = true;
                    break;
                }
            if (!dice) return false;
            var deLocalidad = ReglaBusqueda.Palabras(ReglaBusqueda.Normalizar(localidad));
            for (int i = 0; i < palabras.Length; i++)
            {
                string p = palabras[i];
                if (!EsDelNumero(palabras, i) && !Array.Exists(nombre, t => Empieza(t, p)) && !Array.Exists(deLocalidad, t => Empieza(t, p)))
                    return false;
            }
            return true;
        }

        static bool Empieza(string palabra, string principio) => palabra.StartsWith(principio, StringComparison.Ordinal);

        // Un número, o lo que lleva al lado («nº», «bajo», «izda»)
        static bool EsDelNumero(string[] p, int i) =>
            EsNumero(p[i]) || NumeroAMedias.Contains(p[i])
            || ((PalabrasNumero.Contains(p[i]) || PalabrasPiso.Contains(p[i]) || EsLetra(p[i]))
                && ((i > 0 && EsNumero(p[i - 1])) || (i + 1 < p.Length && EsNumero(p[i + 1]))));

        /// <summary>
        /// Junta los tramos: una calle por nombre (normalizado) y localidad. La localidad de cada tramo es la más cercana
        /// a su mitad (vacía si no hay ninguna). Fuera los tramos sin nombre o con menos de dos puntos.
        /// </summary>
        public static IReadOnlyList<CalleGuardada> Juntar(IEnumerable<TramoCalle> tramos, IReadOnlyList<LocalidadMapa> localidades)
        {
            var calles = new List<CalleGuardada>();
            var porClave = new Dictionary<(string, string), CalleGuardada>();
            var normalizados = new Dictionary<string, string>();
            foreach (var t in tramos)
            {
                if (t == null || t.Puntos.Count < 2) continue;
                if (!normalizados.TryGetValue(t.Nombre, out var nombre))
                    normalizados[t.Nombre] = nombre = ReglaBusqueda.Normalizar(t.Nombre);
                if (nombre.Length == 0) continue;
                string localidad = Localidad(t.Puntos, localidades);
                if (!porClave.TryGetValue((nombre, localidad), out var calle))
                {
                    porClave[(nombre, localidad)] = calle = new CalleGuardada(t.Nombre.Trim(), localidad);
                    calles.Add(calle);
                }
                calle.Anadir(t.Puntos);
            }
            return calles;
        }

        static string Localidad(IReadOnlyList<Coordenada> puntos, IReadOnlyList<LocalidadMapa> localidades)
        {
            if (localidades == null || localidades.Count == 0) return "";
            var a = puntos[(puntos.Count - 1) / 2];
            var b = puntos[puntos.Count / 2];
            var mitad = new Coordenada((a.Latitud + b.Latitud) / 2, (a.Longitud + b.Longitud) / 2);
            string cercana = "";
            double metros = double.PositiveInfinity;
            foreach (var l in localidades)
            {
                double d = mitad.DistanciaMetros(l.Posicion);
                if (d >= metros) continue;
                metros = d;
                cercana = l.Nombre;
            }
            return cercana;
        }

        /// <summary>
        /// Las calles que coinciden, como direcciones: por puntuación, después por distancia a <paramref name="cerca"/> y
        /// después por nombre; como mucho MaximoCalles.
        /// </summary>
        public static IReadOnlyList<Direccion> Buscar(IReadOnlyList<CalleGuardada> calles, string consulta, Coordenada cerca,
            CancellationToken ct = default)
        {
            var formas = Formas(consulta);
            if (formas.Count == 0 || calles == null) return Array.Empty<Direccion>();
            var lista = new List<(int puntos, double metros, Direccion direccion)>();
            foreach (var c in calles)
            {
                ct.ThrowIfCancellationRequested();
                int p = Puntuar(formas, c.Nombre, c.Localidad);
                if (p != ReglaBusqueda.NoCoincide) lista.Add((p, cerca.DistanciaMetros(c.Direccion.Posicion), c.Direccion));
            }
            lista.Sort((a, b) =>
            {
                int d = b.puntos.CompareTo(a.puntos);
                if (d == 0) d = a.metros.CompareTo(b.metros);
                if (d == 0) d = string.CompareOrdinal(a.direccion.Nombre, b.direccion.Nombre);
                return d;
            });
            var r = new List<Direccion>(Math.Min(MaximoCalles, lista.Count));
            foreach (var x in lista) { if (r.Count >= MaximoCalles) break; r.Add(x.direccion); }
            return r.AsReadOnly();
        }

        /// <summary>
        /// Las direcciones de Photon sin las calles que ya salen guardadas (mismo nombre normalizado a menos de 1 km); la
        /// misma lista si no sobra ninguna.
        /// </summary>
        public static IReadOnlyList<ResultadoBusqueda> SinRepetidas(IReadOnlyList<ResultadoBusqueda> calles,
            IReadOnlyList<ResultadoBusqueda> direcciones)
        {
            if (calles == null || calles.Count == 0 || direcciones == null || direcciones.Count == 0) return direcciones;
            var r = new List<ResultadoBusqueda>(direcciones.Count);
            foreach (var d in direcciones)
            {
                string nombre = ReglaBusqueda.Normalizar(d.Nombre);
                bool repetida = false;
                foreach (var c in calles)
                    if (c.Posicion.DistanciaMetros(d.Posicion) < MetrosMismaCalle && ReglaBusqueda.Normalizar(c.Nombre) == nombre)
                    {
                        repetida = true;
                        break;
                    }
                if (!repetida) r.Add(d);
            }
            return r.Count == direcciones.Count ? direcciones : r.AsReadOnly();
        }
    }
}
