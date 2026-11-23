using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Rutas con Valhalla (servidor de FOSSGIS, historia 2.6): la más rápida en coche (<c>auto</c>, respetando el
    /// sentido de circulación; parte 5a de «Cómo llegar») o la más corta (<c>shortest</c>) a pie (<c>pedestrian</c>),
    /// con las maniobras en castellano. Valhalla empieza y acaba en el punto de calle más cercano. Gratis y sin clave.
    /// Nada se guarda.
    /// </summary>
    public sealed class RutasValhalla : IRutas
    {
        public const string Url = "https://valhalla1.openstreetmap.de/route";

        static readonly TimeSpan Plazo = TimeSpan.FromSeconds(ParametrosNegocio.RutaAlternativasPlazoSegundos);

        readonly Func<string, CancellationToken, Task<Resultado<string>>> pedir;
        readonly Func<TimeSpan, CancellationToken, Task> esperar;

        public RutasValhalla() : this((cuerpo, ct) => PeticionRutas.Pedir("Valhalla", Url, cuerpo, ct), Task.Delay) { }

        /// <param name="pedir">Manda el cuerpo JSON; las pruebas pasan uno que no sale a la red.</param>
        /// <param name="esperar">Espera cancelable (en la app, Task.Delay).</param>
        internal RutasValhalla(Func<string, CancellationToken, Task<Resultado<string>>> pedir, Func<TimeSpan, CancellationToken, Task> esperar)
        {
            this.pedir = pedir;
            this.esperar = esperar;
        }

        public static string Costing(ModoViaje modo) => modo == ModoViaje.APie ? "pedestrian" : "auto";

        /// <summary>
        /// Cuerpo JSON de la petición: los dos puntos (origen con 5 decimales, destino con 6), el costing con
        /// <c>shortest</c> (false en coche, que da la más rápida; true a pie y cuando se pide la más corta),
        /// <c>alternates</c> si se quieren alternativas, <c>language: es-ES</c>, <c>directions_type: instructions</c> (con
        /// "maneuvers" Valhalla no da el texto de cada maniobra; comprobado el 20/11/2026) y kilómetros.
        /// </summary>
        public static string Cuerpo(Coordenada origen, Coordenada destino, ModoViaje modo, int alternativas = 0, bool laMasCorta = false)
        {
            string c = Costing(modo);
            string corta = laMasCorta || modo == ModoViaje.APie ? "true" : "false";
            return "{\"locations\":["
                   + "{\"lat\":" + PeticionRutas.Origen(origen.Latitud) + ",\"lon\":" + PeticionRutas.Origen(origen.Longitud) + "},"
                   + "{\"lat\":" + PeticionRutas.Destino(destino.Latitud) + ",\"lon\":" + PeticionRutas.Destino(destino.Longitud) + "}],"
                   + "\"costing\":\"" + c + "\","
                   + "\"costing_options\":{\"" + c + "\":{\"shortest\":" + corta + "}},"
                   + (alternativas > 0 ? "\"alternates\":" + alternativas + "," : "")
                   + "\"language\":\"es-ES\","
                   + "\"directions_type\":\"instructions\","
                   + "\"units\":\"kilometers\"}";
        }

        public Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct) =>
            Una(Cuerpo(origen, destino, modo), ct);

        async Task<Resultado<Ruta>> Una(string cuerpo, CancellationToken ct)
        {
            var r = await pedir(cuerpo, ct);
            var ruta = r.Ok ? LecturaValhalla.Leer(r.Valor) : Resultado<Ruta>.Fallo(r.Error);
            if (!ruta.Ok) UnityEngine.Debug.LogWarning("PriceMap: Valhalla sin ruta: " + ruta.Error);
            return ruta;
        }

        /// <summary>
        /// Una petición con dos alternativas y, en coche, si salen menos de tres rutas distintas, otra detrás (nunca a la
        /// vez) con la más corta. La segunda es un extra: si falla o no llega dentro del plazo (contado desde el
        /// principio, menor que el límite del servicio), valen las de la primera.
        /// </summary>
        public async Task<Resultado<IReadOnlyList<Ruta>>> Alternativas(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var fin = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                var plazo = modo == ModoViaje.Coche ? esperar(Plazo, fin.Token) : null;
                var r = await pedir(Cuerpo(origen, destino, modo, ParametrosNegocio.RutaAlternativasMaximo - 1), ct);
                ct.ThrowIfCancellationRequested();
                var leidas = r.Ok ? LecturaValhalla.LeerTodas(r.Valor) : Resultado<IReadOnlyList<Ruta>>.Fallo(r.Error);
                if (!leidas.Ok)
                {
                    UnityEngine.Debug.LogWarning("PriceMap: Valhalla sin rutas: " + leidas.Error);
                    return leidas;
                }

                var rutas = ReglaAlternativas.Juntar(leidas.Valor);
                if (plazo != null && !plazo.IsCompleted && rutas.Count < ParametrosNegocio.RutaAlternativasMaximo)
                {
                    var corta = await LaMasCorta(Cuerpo(origen, destino, modo, laMasCorta: true), plazo, fin.Token, ct);
                    if (corta != null) rutas = ReglaAlternativas.Juntar(rutas.Append(corta));
                }
                return Resultado<IReadOnlyList<Ruta>>.Exito(rutas);
            }
            finally
            {
                fin.Cancel(); // se para la espera y, si venció el plazo, la segunda petición
            }
        }

        /// <summary>La segunda petición; null si falla o vence el plazo. Si cancela quien llama, OperationCanceledException.</summary>
        async Task<Ruta> LaMasCorta(string cuerpo, Task plazo, CancellationToken ctPeticion, CancellationToken ct)
        {
            var peticion = Una(cuerpo, ctPeticion);
            // Si se deja de esperar (plazo o cancelación), su excepción no queda sin observar
            _ = peticion.ContinueWith(t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            var primera = await Task.WhenAny(peticion, plazo);
            ct.ThrowIfCancellationRequested();
            if (primera != peticion) return null;
            try
            {
                var r = await peticion;
                return r.Ok ? r.Valor : null;
            }
            catch (Exception e)
            {
                ct.ThrowIfCancellationRequested();
                UnityEngine.Debug.LogWarning("PriceMap: Valhalla sin la ruta más corta: " + e.Message);
                return null;
            }
        }
    }
}
