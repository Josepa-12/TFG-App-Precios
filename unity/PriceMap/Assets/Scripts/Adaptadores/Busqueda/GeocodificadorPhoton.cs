using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Adaptadores.Red;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Busqueda
{
    /// <summary>
    /// Direcciones con Photon (komoot), historia 2.3, parte 2. Sin bbox: todo el mundo, con prioridad por cercanía al
    /// punto dado. Como mucho 8 resultados, en el idioma de cada sitio (<c>lang=default</c>). Cualquier fallo (sin
    /// conexión, HTTP, respuesta rota) vuelve como Resultado.Fallo; cancelar aborta la petición. Nada se guarda (FR17).
    /// La geocodificación inversa del punto libre (historia 2.5) usa <c>/reverse</c> con el mismo trato.
    /// Va por ClienteHttp (en Android, la red del sistema). Se llama desde el hilo principal.
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
            var lista = r.Ok ? LecturaPhoton.Leer(r.Valor) : Resultado<IReadOnlyList<Direccion>>.Fallo(r.Error, vacia);
            UnityEngine.Debug.Log(lista.Ok
                ? $"PriceMap: Photon, {lista.Valor.Count} direcciones para la búsqueda."
                : "PriceMap: Photon sin direcciones: " + lista.Error);
            return lista;
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
            var direccion = r.Ok ? LecturaPhoton.LeerInverso(r.Valor) : Resultado<Direccion>.Fallo(r.Error);
            if (!direccion.Ok) UnityEngine.Debug.LogWarning("PriceMap: Photon sin dirección del punto: " + direccion.Error);
            return direccion;
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
                var peticion = PeticionHttp.Get(url, AgenteUsuario);
                peticion.SegundosMaximo = TiempoMaximo;
                var resp = await ClienteHttp.Pedir(peticion, ct);
                if (!resp.Ok) return Resultado<string>.Fallo("Photon no responde " + resp.Describir());
                return Resultado<string>.Exito(resp.Texto);
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
