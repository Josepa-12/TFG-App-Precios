using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Ubicaciones compartidas (historia 2.5, parte 2; FR17): reconoce un enlace de mapas o unas coordenadas sueltas en
    /// el texto pegado en el buscador y devuelve su coordenada. Lógica pura, sin red: los enlaces cortos
    /// (<c>maps.app.goo.gl</c>, <c>goo.gl/maps</c>) se resuelven fuera (IResolutorEnlaces) y su URL final se lee aquí.
    /// Formatos:
    /// <list type="bullet">
    /// <item>Google Maps: <c>!3d&lt;lat&gt;!4d&lt;lng&gt;</c> (gana a todo), <c>?q=</c>, <c>ll=</c>, <c>query=</c> y <c>@lat,lng,zoom</c>.</item>
    /// <item>Apple Maps: <c>ll=lat,lng</c> y <c>q=lat,lng</c>.</item>
    /// <item><c>geo:lat,lng</c>; con <c>?q=lat,lng(nombre)</c>, gana el <c>q</c>. <c>geo:0,0</c> sin q legible no es un punto.</item>
    /// <item>Coordenadas sueltas: «36.7781, -6.3515», «36.7781 -6.3515», «36,7781 -6,3515», «36,7781; -6,3515»
    /// (las dos con dos decimales al menos, para no confundir una búsqueda como «10 20» o «1,5 2,5»).</item>
    /// <item>Enlaces de mapas pegados sin <c>https://</c> (<c>maps.app.goo.gl/…</c>, <c>www.google.com/maps/…</c>).</item>
    /// </list>
    /// Valida los rangos (latitud ±90, longitud ±180). El nombre que traiga el enlace se ignora: la ficha pone la
    /// dirección de Photon. No usa ReglaBusqueda.Normalizar (quita signos y puntos).
    /// </summary>
    public static class LectorUbicacionCompartida
    {
        const RegexOptions Opciones = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        // Número con punto decimal (en las URL la coma separa latitud y longitud)
        const string NumeroPunto = @"[+-]?\d{1,3}(?:\.\d+)?";

        // Con esquema, o de mapas sin él («maps.app.goo.gl/…», «www.google.com/maps/…», «maps.apple.com/…»)
        static readonly Regex Enlace = new Regex(
            @"(?:https?://|geo:)\S+|(?<![\w./])(?:maps\.app\.goo\.gl/|goo\.gl/maps|(?:www\.)?google\.[a-z]{2,3}(?:\.[a-z]{2})?/maps"
            + @"|maps\.google\.[a-z]{2,3}(?:\.[a-z]{2})?|maps\.apple\.com)\S*", Opciones);
        static readonly Regex Datos3d4d = new Regex(@"!3d(" + NumeroPunto + @")!4d(" + NumeroPunto + ")", Opciones);
        static readonly Regex Arroba = new Regex(@"@(" + NumeroPunto + @"),\s*(" + NumeroPunto + @")(?![\d.])", Opciones);
        static readonly Regex Parametro = new Regex(@"[?&](q|ll|query)=([^&#]*)", Opciones);
        static readonly Regex ParDecimal = new Regex(@"^\s*(" + NumeroPunto + @")\s*[,;\s]\s*(" + NumeroPunto + @")\s*(?:\(.*\))?\s*$", Opciones);
        static readonly Regex Geo = new Regex(@"^geo:(" + NumeroPunto + @"),(" + NumeroPunto + @")(?:[,;?#]|$)", Opciones);
        // Sueltas: punto o coma decimal (dos decimales al menos: «1,5 2,5» no es un sitio); separadas por coma, punto
        // y coma o espacios
        static readonly Regex Sueltas = new Regex(
            @"^\s*([+-]?\d{1,3}[.,]\d{2,})\s*(?:[,;]\s*|\s+)([+-]?\d{1,3}[.,]\d{2,})\s*$", Opciones);

        /// <summary>
        /// La coordenada del enlace o de las coordenadas sueltas; null si el texto no es ninguna de las dos cosas o se
        /// sale de rango.
        /// </summary>
        public static Coordenada? Leer(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            string enlace = ExtraerEnlace(texto);
            return enlace != null ? LeerEnlace(enlace) : LeerSueltas(texto);
        }

        /// <summary>
        /// El texto parece un enlace (lleva <c>http</c>, <c>geo:</c> o <c>maps.</c>): si no se reconoce, se avisa en
        /// vez de buscarlo como un nombre.
        /// </summary>
        public static bool PareceEnlace(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return texto.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0
                || texto.IndexOf("geo:", StringComparison.OrdinalIgnoreCase) >= 0
                || texto.IndexOf("maps.", StringComparison.OrdinalIgnoreCase) >= 0
                || EsEnlaceCorto(texto);
        }

        /// <summary>
        /// El primer enlace del texto es un enlace corto de Google Maps (servidor <c>maps.app.goo.gl</c>, o
        /// <c>goo.gl</c> con ruta <c>/maps…</c>): hay que resolverlo. Mira el servidor, no el texto suelto.
        /// </summary>
        public static bool EsEnlaceCorto(string texto)
        {
            string enlace = ExtraerEnlace(texto);
            if (enlace == null || !Uri.TryCreate(enlace, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
            string servidor = uri.Host.ToLowerInvariant();
            return servidor == "maps.app.goo.gl"
                || (servidor == "goo.gl" && uri.AbsolutePath.StartsWith("/maps", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// El primer enlace del texto (<c>http(s)://…</c>, <c>geo:…</c> o uno de mapas sin esquema, hasta el primer
        /// espacio); null si no hay. Al de mapas sin esquema se le pone <c>https://</c>. Al compartir desde Google
        /// Maps, el enlace suele ir detrás del nombre del sitio.
        /// </summary>
        public static string ExtraerEnlace(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return null;
            var m = Enlace.Match(texto);
            if (!m.Success) return null;
            bool conEsquema = m.Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || m.Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || m.Value.StartsWith("geo:", StringComparison.OrdinalIgnoreCase);
            return conEsquema ? m.Value : "https://" + m.Value;
        }

        /// <summary>Largo máximo del texto compartido que va al campo del buscador (sin enlace).</summary>
        public const int LargoMaximoCompartido = 200;

        static readonly Regex Espacios = new Regex(@"\s+", RegexOptions.CultureInvariant);

        /// <summary>
        /// Lo que va al campo del buscador cuando otra app comparte un texto con PriceMap (historia 2.5, parte 3): si
        /// lleva un enlace, solo el enlace (Google Maps lo pone detrás del nombre y la dirección del sitio); si no, el
        /// texto en una línea (saltos y espacios seguidos → un espacio), recortado a 200 caracteres. Null si está vacío.
        /// </summary>
        public static string TextoCompartido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            string enlace = ExtraerEnlace(texto);
            if (enlace != null) return enlace;
            string linea = Espacios.Replace(texto, " ").Trim();
            if (linea.Length > LargoMaximoCompartido)
            {
                int largo = char.IsHighSurrogate(linea[LargoMaximoCompartido - 1]) ? LargoMaximoCompartido - 1 : LargoMaximoCompartido;
                linea = linea.Substring(0, largo).TrimEnd(); // sin partir un emoji
            }
            return linea.Length > 0 ? linea : null;
        }

        static Coordenada? LeerEnlace(string enlace)
        {
            string url = Descodificar(enlace);

            if (url.StartsWith("geo:", StringComparison.OrdinalIgnoreCase))
            {
                var q = LeerParametros(url, soloQ: true);
                if (q.HasValue) return q;
                var g = Geo.Match(url);
                if (!g.Success) return null;
                var c = Crear(g.Groups[1].Value, g.Groups[2].Value, CultureInfo.InvariantCulture);
                // geo:0,0?q=Calle Ancha: el 0,0 solo dice "sin coordenadas"
                return c.HasValue && (c.Value.Latitud != 0 || c.Value.Longitud != 0) ? c : null;
            }

            var d = Datos3d4d.Match(url);
            if (d.Success) return Crear(d.Groups[1].Value, d.Groups[2].Value, CultureInfo.InvariantCulture);

            var p = LeerParametros(url, soloQ: false);
            if (p.HasValue) return p;

            var a = Arroba.Match(url);
            if (a.Success) return Crear(a.Groups[1].Value, a.Groups[2].Value, CultureInfo.InvariantCulture);
            return null;
        }

        // q=, ll= o query= con "lat,lng" (el primero que lo sea). En geo: solo q
        static Coordenada? LeerParametros(string url, bool soloQ)
        {
            foreach (Match m in Parametro.Matches(url))
            {
                if (soloQ && !string.Equals(m.Groups[1].Value, "q", StringComparison.OrdinalIgnoreCase)) continue;
                var par = ParDecimal.Match(m.Groups[2].Value.Replace('+', ' '));
                if (!par.Success) continue;
                var c = Crear(par.Groups[1].Value, par.Groups[2].Value, CultureInfo.InvariantCulture);
                if (c.HasValue) return c;
            }
            return null;
        }

        static Coordenada? LeerSueltas(string texto)
        {
            var m = Sueltas.Match(texto);
            if (!m.Success) return null;
            return Crear(m.Groups[1].Value.Replace(',', '.'), m.Groups[2].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        static Coordenada? Crear(string lat, string lon, IFormatProvider formato)
        {
            const NumberStyles estilo = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            if (!double.TryParse(lat, estilo, formato, out double la) || !double.TryParse(lon, estilo, formato, out double lo)) return null;
            if (double.IsNaN(la) || double.IsNaN(lo) || Math.Abs(la) > 90.0 || Math.Abs(lo) > 180.0) return null;
            return new Coordenada(la, lo);
        }

        // %2C → ","; si el enlace trae un % roto, se lee tal cual
        static string Descodificar(string url)
        {
            try
            {
                return Uri.UnescapeDataString(url);
            }
            catch (UriFormatException)
            {
                return url;
            }
        }
    }
}
