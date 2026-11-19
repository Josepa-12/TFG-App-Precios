using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine; // await de AsyncOperation (Unity 6: AsyncOperationAwaitableExtensions)
using UnityEngine.Networking;

namespace PriceMap.Adaptadores.Busqueda
{
    /// <summary>
    /// Direcciones con Photon (komoot), historia 2.3, parte 2. Sin bbox: todo el mundo, con prioridad por cercanía al
    /// punto dado. Como mucho 8 resultados, en el idioma de cada sitio (<c>lang=default</c>). Cualquier fallo (sin
    /// conexión, HTTP, respuesta rota) vuelve como Resultado.Fallo; cancelar aborta la petición. Nada se guarda (FR17).
    /// Se llama desde el hilo principal (UnityWebRequest lo exige).
    /// </summary>
    public sealed class GeocodificadorPhoton : IGeocodificador
    {
        public const string Api = "https://photon.komoot.io/api/";
        public const string AgenteUsuario = "PriceMap/1.0";
        /// <summary>Segundos que se espera la respuesta antes de darla por fallida.</summary>
        public const int TiempoMaximo = 10;

        /// <summary>
        /// URL de la petición: q, lat, lon, limit=8 y lang=default. La cercanía va con 3 decimales (unos 100 m): basta
        /// para ordenar y no manda a un tercero la posición exacta del usuario.
        /// </summary>
        public static string Url(string consulta, Coordenada cerca) =>
            Api + "?q=" + Uri.EscapeDataString((consulta ?? "").Trim())
                + "&lat=" + cerca.Latitud.ToString("0.###", CultureInfo.InvariantCulture)
                + "&lon=" + cerca.Longitud.ToString("0.###", CultureInfo.InvariantCulture)
                + "&limit=" + BuscadorDirecciones.MaximoDirecciones.ToString(CultureInfo.InvariantCulture)
                + "&lang=default";

        public async Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct)
        {
            var vacia = (IReadOnlyList<Direccion>)Array.Empty<Direccion>();
            ct.ThrowIfCancellationRequested();
            try
            {
                using var req = UnityWebRequest.Get(Url(consulta, cerca));
                req.SetRequestHeader("User-Agent", AgenteUsuario);
                req.timeout = TiempoMaximo;
                using (ct.Register(req.Abort))
                    await req.SendWebRequest();
                ct.ThrowIfCancellationRequested();
                if (req.result != UnityWebRequest.Result.Success)
                    return Resultado<IReadOnlyList<Direccion>>.Fallo(
                        $"Photon no responde ({req.result}{(req.responseCode > 0 ? ", HTTP " + req.responseCode : "")}): {req.error}", vacia);
                return LecturaPhoton.Leer(req.downloadHandler.text);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                return Resultado<IReadOnlyList<Direccion>>.Fallo("Fallo al pedir direcciones a Photon: " + e.Message, vacia);
            }
        }
    }
}
