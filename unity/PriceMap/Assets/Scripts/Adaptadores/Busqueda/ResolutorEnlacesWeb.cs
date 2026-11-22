using System;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Adaptadores.Red;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Busqueda
{
    /// <summary>
    /// Enlaces cortos de Google Maps (historia 2.5, parte 2): pide el enlace sin seguir redirecciones
    /// (ClienteHttp con <c>SeguirRedirecciones = false</c>) y lee la cabecera <c>Location</c>, hasta 5 saltos. Para en cuanto la URL deja de ser
    /// un enlace corto (no se llega a pedir la página de Google, que en Europa manda al aviso de cookies) y la devuelve.
    /// Cualquier fallo (sin conexión, HTTP sin Location, demasiados saltos) vuelve como Resultado.Fallo; cancelar
    /// aborta la petición. Nada se guarda (FR17). Se llama desde el hilo principal.
    /// </summary>
    public sealed class ResolutorEnlacesWeb : IResolutorEnlaces
    {
        /// <summary>Saltos de redirección que se siguen como mucho.</summary>
        public const int MaximoSaltos = 5;

        // Un salto: destino de la redirección, null si no la hay, o fallo. Las pruebas ponen uno falso (sin red)
        readonly Func<string, CancellationToken, Task<Resultado<string>>> siguiente;

        public ResolutorEnlacesWeb() : this(Siguiente) { }

        internal ResolutorEnlacesWeb(Func<string, CancellationToken, Task<Resultado<string>>> siguiente) =>
            this.siguiente = siguiente ?? throw new ArgumentNullException(nameof(siguiente));

        public async Task<Resultado<string>> Resolver(string url, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(url)) return Resultado<string>.Fallo("Enlace vacío.");
            string actual = url.Trim();
            for (int salto = 0; salto < MaximoSaltos; salto++)
            {
                actual = ConHttps(actual); // Android bloquea http en claro
                var r = await siguiente(actual, ct);
                if (!r.Ok) return r;
                if (r.Valor == null) return Resultado<string>.Exito(actual); // sin redirección: esta es la final
                actual = r.Valor;
                if (!LectorUbicacionCompartida.EsEnlaceCorto(actual)) return Resultado<string>.Exito(actual);
            }
            return Fallo($"Más de {MaximoSaltos} redirecciones.");
        }

        /// <summary>
        /// Un salto: la URL a la que redirige (relativa o no) o null si la respuesta no es una redirección.
        /// </summary>
        static async Task<Resultado<string>> Siguiente(string url, CancellationToken ct)
        {
            try
            {
                var peticion = PeticionHttp.Get(url, GeocodificadorPhoton.AgenteUsuario);
                peticion.SegundosMaximo = GeocodificadorPhoton.TiempoMaximo;
                peticion.SeguirRedirecciones = false;
                var resp = await ClienteHttp.Pedir(peticion, ct);
                long codigo = resp.Codigo;
                // Una redirección sin seguir llega en el PC como error ("redirect limit exceeded") y en Android como
                // Success: por eso se mira antes el código
                if (codigo >= 300 && codigo < 400)
                {
                    string destino = resp.Location;
                    if (string.IsNullOrWhiteSpace(destino)) return Fallo($"Redirección {codigo} sin Location.");
                    return Resultado<string>.Exito(Absoluta(url, destino.Trim()));
                }
                if (!resp.Ok) return Fallo("El enlace no responde " + resp.Describir());
                return Resultado<string>.Exito(null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                return Fallo("Fallo al resolver el enlace: " + (string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message));
            }
        }

        /// <summary>El fallo de un salto, también en el registro.</summary>
        static Resultado<string> Fallo(string error)
        {
            UnityEngine.Debug.LogWarning("PriceMap: enlace corto sin resolver: " + error);
            return Resultado<string>.Fallo(error);
        }

        /// <summary>Un enlace corto con <c>http://</c> se pide con <c>https://</c> (Android no deja http en claro).</summary>
        internal static string ConHttps(string url) =>
            url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && LectorUbicacionCompartida.EsEnlaceCorto(url)
                ? "https://" + url.Substring("http://".Length)
                : url;

        /// <summary>El destino de Location como URL absoluta (puede venir relativo a la pedida).</summary>
        public static string Absoluta(string pedida, string destino)
        {
            // "/maps/…" en Mono (Android) también es una URI absoluta (file://): solo vale como absoluta con http(s)
            if (destino.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || destino.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return destino;
            if (Uri.TryCreate(pedida, UriKind.Absolute, out var baseUri) && Uri.TryCreate(baseUri, destino, out var rel))
                return rel.AbsoluteUri;
            return destino;
        }
    }
}
