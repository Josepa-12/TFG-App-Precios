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
    /// La geocodificación inversa del punto libre (historia 2.5) usa <c>/reverse</c> con el mismo trato.
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
            var r = await Pedir(Url(consulta, cerca), ct);
            if (!r.Ok) return Resultado<IReadOnlyList<Direccion>>.Fallo(r.Error, vacia);
            return LecturaPhoton.Leer(r.Valor);
        }

        public const string ApiInversa = "https://photon.komoot.io/reverse";

        /// <summary>
        /// URL de la geocodificación inversa (historia 2.5): lat, lon, limit=1 y lang=default (como Buscar: en el
        /// idioma de cada sitio). El punto va con 6 decimales (unos
        /// 10 cm): es un sitio del mapa elegido a mano, no la posición del usuario.
        /// </summary>
        public static string UrlInversa(Coordenada punto) =>
            ApiInversa + "?lat=" + punto.Latitud.ToString("0.######", CultureInfo.InvariantCulture)
                + "&lon=" + punto.Longitud.ToString("0.######", CultureInfo.InvariantCulture)
                + "&limit=1&lang=default";

        public async Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct)
        {
            var r = await Pedir(UrlInversa(punto), ct);
            if (!r.Ok) return Resultado<Direccion>.Fallo(r.Error);
            return LecturaPhoton.LeerInverso(r.Valor);
        }

        /// <summary>
        /// GET con el agente y el tiempo máximo: el texto de la respuesta o el fallo (sin conexión, HTTP). Cancelar
        /// aborta la petición y termina en OperationCanceledException.
        /// </summary>
        static async Task<Resultado<string>> Pedir(string url, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var req = UnityWebRequest.Get(url);
                req.SetRequestHeader("User-Agent", AgenteUsuario);
                req.timeout = TiempoMaximo;
                using (ct.Register(req.Abort))
                    await req.SendWebRequest();
                ct.ThrowIfCancellationRequested();
                if (req.result != UnityWebRequest.Result.Success)
                    return Resultado<string>.Fallo(
                        $"Photon no responde ({req.result}{(req.responseCode > 0 ? ", HTTP " + req.responseCode : "")}): {req.error}");
                return Resultado<string>.Exito(req.downloadHandler.text);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                return Resultado<string>.Fallo("Fallo al pedir a Photon: " + (string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message));
            }
        }
    }
}
