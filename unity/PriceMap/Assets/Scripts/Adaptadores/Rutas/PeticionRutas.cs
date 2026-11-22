using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Adaptadores.Red;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Petición HTTP común a Valhalla y OSRM (historia 2.6), como GeocodificadorPhoton.Pedir: agente propio, tiempo
    /// máximo y cancelación que aborta. Va por ClienteHttp (en Android, la red del sistema: FOSSGIS solo acepta
    /// TLS 1.3). Se llama desde el hilo principal.
    /// </summary>
    internal static class PeticionRutas
    {
        public const string AgenteUsuario = "PriceMap/1.0";
        /// <summary>Segundos de la petición; el servicio corta antes (8 s, ParametrosNegocio) y pasa a la reserva.</summary>
        public const int TiempoMaximo = 10;

        /// <summary>
        /// El origen (la posición del usuario) con 5 decimales: ≈1 m, lo que pide una ruta que empieza donde está el
        /// usuario. Va a los servidores de FOSSGIS y no se guarda nada.
        /// </summary>
        public static string Origen(double grados) => grados.ToString("0.#####", CultureInfo.InvariantCulture);

        /// <summary>El destino (un lugar o un punto elegido a mano) con 6 decimales.</summary>
        public static string Destino(double grados) => grados.ToString("0.######", CultureInfo.InvariantCulture);

        /// <summary>
        /// GET (sin cuerpo) o POST con el cuerpo JSON: el texto de la respuesta o el fallo (sin conexión, HTTP).
        /// Cancelar aborta la petición y termina en OperationCanceledException.
        /// </summary>
        public static async Task<Resultado<string>> Pedir(string servicio, string url, string cuerpoJson, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var peticion = cuerpoJson == null
                    ? PeticionHttp.Get(url, AgenteUsuario)
                    : PeticionHttp.PostJson(url, cuerpoJson, AgenteUsuario);
                peticion.SegundosMaximo = TiempoMaximo;
                var resp = await ClienteHttp.Pedir(peticion, ct);
                if (!resp.Ok) return Resultado<string>.Fallo($"{servicio} no responde {resp.Describir()}");
                return Resultado<string>.Exito(resp.Texto);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                return Resultado<string>.Fallo($"Fallo al pedir a {servicio}: " + (string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message));
            }
        }

        /// <summary>Punto de la línea en esa posición (acotada a la línea); el respaldo si la línea está vacía.</summary>
        public static Coordenada Punto(System.Collections.Generic.IReadOnlyList<Coordenada> puntos, int i, Coordenada respaldo)
        {
            if (puntos == null || puntos.Count == 0) return respaldo;
            return puntos[Math.Max(0, Math.Min(puntos.Count - 1, i))];
        }
    }
}
