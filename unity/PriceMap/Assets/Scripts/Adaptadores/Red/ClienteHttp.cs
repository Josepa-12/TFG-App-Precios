using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if !(UNITY_ANDROID && !UNITY_EDITOR)
using UnityEngine.Networking;
#endif

namespace PriceMap.Adaptadores.Red
{
    /// <summary>Lo que se pide: método, URL, cuerpo JSON (null = sin cuerpo), cabeceras, tiempo máximo y redirecciones.</summary>
    public sealed class PeticionHttp
    {
        public string Metodo = "GET";
        public string Url;
        public string CuerpoJson;
        public readonly Dictionary<string, string> Cabeceras = new Dictionary<string, string>();
        /// <summary>Segundos de la petición entera (como el timeout de UnityWebRequest).</summary>
        public int SegundosMaximo = 10;
        /// <summary>false para los enlaces cortos: los saltos los cuenta ResolutorEnlacesWeb.</summary>
        public bool SeguirRedirecciones = true;

        public static PeticionHttp Get(string url, string agente) => new PeticionHttp { Url = url }.Con("User-Agent", agente);

        /// <summary>POST con el cuerpo en JSON (Content-Type: application/json).</summary>
        public static PeticionHttp PostJson(string url, string cuerpoJson, string agente) =>
            new PeticionHttp { Metodo = "POST", Url = url, CuerpoJson = cuerpoJson }
                .Con("Content-Type", "application/json").Con("User-Agent", agente);

        public PeticionHttp Con(string cabecera, string valor)
        {
            if (!string.IsNullOrEmpty(valor)) Cabeceras[cabecera] = valor;
            return this;
        }
    }

    /// <summary>
    /// La respuesta. <see cref="Resultado"/> lleva los nombres de UnityWebRequest.Result (Success, ConnectionError,
    /// ProtocolError…) en las dos plataformas, para que los mensajes de fallo sean los de siempre.
    /// </summary>
    public readonly struct RespuestaHttp
    {
        public const string Exito = "Success";
        public const string ErrorConexion = "ConnectionError";
        public const string ErrorProtocolo = "ProtocolError";

        public readonly string Resultado;
        public readonly long Codigo;
        public readonly string Texto;
        /// <summary>La cabecera Location (vacía si no hay).</summary>
        public readonly string Location;
        public readonly string Error;

        public RespuestaHttp(string resultado, long codigo, string texto, string location, string error)
        {
            Resultado = resultado ?? ErrorConexion;
            Codigo = codigo;
            Texto = texto ?? "";
            Location = location ?? "";
            Error = error ?? "";
        }

        public bool Ok => Resultado == Exito;

        /// <summary>«ConnectionError: …» o «ProtocolError, HTTP 404: …», lo que va entre paréntesis y tras los dos puntos en los fallos.</summary>
        public string Describir() => $"({Resultado}{(Codigo > 0 ? ", HTTP " + Codigo : "")}): {Error}";
    }

    /// <summary>
    /// Cliente HTTP común a rutas, direcciones y enlaces cortos (arreglo «red Android TLS 1.3»). En Android la petición
    /// la hace la red del sistema (HttpURLConnection, con TLS 1.3 desde Android 10) por el puente
    /// com.pricemap.red.PuenteRed, en un hilo aparte, y aquí se espera fotograma a fotograma sin bloquear; en el PC y
    /// en el editor, UnityWebRequest como antes. Se llama desde el hilo principal. Cancelar corta la conexión y termina
    /// en OperationCanceledException. Los fallos de red vuelven en la respuesta (no como excepción); lo que lance de
    /// más (JNI, URL mal formada en el PC) lo recogen los adaptadores como hasta ahora.
    /// </summary>
    public static class ClienteHttp
    {
        /// <summary>true si las peticiones van por la red de Android (solo en el móvil).</summary>
        public const bool RedAndroid =
#if UNITY_ANDROID && !UNITY_EDITOR
            true;
#else
            false;
#endif

        /// <summary>Las cabeceras en líneas «Nombre: valor» (como las lee PuenteRed).</summary>
        internal static string CabecerasEnTexto(IReadOnlyDictionary<string, string> cabeceras)
        {
            var sb = new StringBuilder();
            foreach (var c in cabeceras)
            {
                if (string.IsNullOrWhiteSpace(c.Key) || c.Value == null) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(c.Key.Trim()).Append(": ").Append(c.Value.Replace('\n', ' ').Replace('\r', ' '));
            }
            return sb.ToString();
        }

        public static Task<RespuestaHttp> Pedir(PeticionHttp p, CancellationToken ct)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            ct.ThrowIfCancellationRequested();
#if UNITY_ANDROID && !UNITY_EDITOR
            return PedirAndroid(p, ct);
#else
            return PedirUnity(p, ct);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        const string ClasePuente = "com.pricemap.red.PuenteRed";
        // Los mismos números que PuenteRed
        const int EnCurso = 0, Hecha = 1, Protocolo = 2, Conexion = 3, Cancelada = 4;

        static AndroidJavaClass puente;
        static AndroidJavaClass Puente => puente ??= new AndroidJavaClass(ClasePuente);

        static async Task<RespuestaHttp> PedirAndroid(PeticionHttp p, CancellationToken ct)
        {
            // Sin null por JNI (la firma no se encontraría): cuerpo vacío = sin cuerpo
            int id = Puente.CallStatic<int>("pedir", p.Metodo ?? "GET", p.Url ?? "", p.CuerpoJson ?? "",
                CabecerasEnTexto(p.Cabeceras), Math.Max(1, p.SegundosMaximo) * 1000, p.SeguirRedirecciones);
            bool cortar = true;
            try
            {
                int estado;
                while ((estado = Puente.CallStatic<int>("estado", id)) == EnCurso)
                    await Awaitable.NextFrameAsync(ct); // lanza OperationCanceledException al cancelar
                cortar = false;
                ct.ThrowIfCancellationRequested();
                if (estado == Cancelada) throw new OperationCanceledException(ct);
                string resultado = estado == Hecha ? RespuestaHttp.Exito
                    : estado == Protocolo ? RespuestaHttp.ErrorProtocolo : RespuestaHttp.ErrorConexion;
                string error = estado == Hecha ? "" : Puente.CallStatic<string>("error", id);
                if (estado != Hecha && string.IsNullOrEmpty(error)) error = "petición sin respuesta";
                return new RespuestaHttp(resultado, Puente.CallStatic<int>("codigo", id), Puente.CallStatic<string>("texto", id),
                    Puente.CallStatic<string>("location", id), error);
            }
            finally
            {
                try
                {
                    if (cortar) Puente.CallStatic("cancelar", id);
                    Puente.CallStatic("liberar", id);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("PriceMap: red de Android, no se pudo cerrar la petición: " + e.Message);
                }
            }
        }
#else
        static async Task<RespuestaHttp> PedirUnity(PeticionHttp p, CancellationToken ct)
        {
            using var req = new UnityWebRequest(p.Url, p.Metodo ?? UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };
            if (p.CuerpoJson != null) req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(p.CuerpoJson));
            foreach (var c in p.Cabeceras) req.SetRequestHeader(c.Key, c.Value);
            req.timeout = Math.Max(1, p.SegundosMaximo);
            if (!p.SeguirRedirecciones) req.redirectLimit = 0;
            using (ct.Register(req.Abort))
                await req.SendWebRequest();
            ct.ThrowIfCancellationRequested();
            return new RespuestaHttp(req.result.ToString(), req.responseCode, req.downloadHandler?.text,
                req.GetResponseHeader("Location"), req.error);
        }
#endif
    }
}
