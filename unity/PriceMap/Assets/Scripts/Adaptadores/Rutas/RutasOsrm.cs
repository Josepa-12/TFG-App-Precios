using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Rutas de reserva con OSRM (servidor de FOSSGIS en routing.openstreetmap.de, historia 2.6): el de
    /// demostración de OSRM solo tiene coche. Pide hasta 3 alternativas: Calcular se queda con la de menos tiempo
    /// (LecturaOsrm) y Alternativas las da por tiempo (ReglaAlternativas). Las maniobras llegan sin texto (tipo y modificador); sus textos son de la historia 2.7.
    /// </summary>
    public sealed class RutasOsrm : IRutas
    {
        public const string Servidor = "https://routing.openstreetmap.de/";

        readonly Func<string, CancellationToken, Task<Resultado<string>>> pedir;

        public RutasOsrm() : this((url, ct) => PeticionRutas.Pedir("OSRM", url, null, ct)) { }

        /// <param name="pedir">Pide la URL; las pruebas pasan uno que no sale a la red.</param>
        internal RutasOsrm(Func<string, CancellationToken, Task<Resultado<string>>> pedir) => this.pedir = pedir;

        /// <summary>
        /// URL de la petición: <c>routed-car/route/v1/driving</c> o <c>routed-foot/route/v1/foot</c>, los puntos como
        /// lon,lat (origen con 5 decimales, destino con 6), 3 alternativas, pasos, línea completa en polyline6.
        /// </summary>
        public static string Url(Coordenada origen, Coordenada destino, ModoViaje modo)
        {
            string perfil = modo == ModoViaje.APie ? "routed-foot/route/v1/foot/" : "routed-car/route/v1/driving/";
            return Servidor + perfil
                   + PeticionRutas.Origen(origen.Longitud) + "," + PeticionRutas.Origen(origen.Latitud) + ";"
                   + PeticionRutas.Destino(destino.Longitud) + "," + PeticionRutas.Destino(destino.Latitud)
                   + "?alternatives=3&steps=true&overview=full&geometries=polyline6";
        }

        public async Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            var r = await pedir(Url(origen, destino, modo), ct);
            var ruta = r.Ok ? LecturaOsrm.Leer(r.Valor) : Resultado<Ruta>.Fallo(r.Error);
            if (!ruta.Ok) UnityEngine.Debug.LogWarning("PriceMap: OSRM sin ruta: " + ruta.Error);
            return ruta;
        }

        public async Task<Resultado<IReadOnlyList<Ruta>>> Alternativas(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            var r = await pedir(Url(origen, destino, modo), ct);
            var rutas = r.Ok ? LecturaOsrm.LeerTodas(r.Valor) : Resultado<IReadOnlyList<Ruta>>.Fallo(r.Error);
            if (rutas.Ok) return Resultado<IReadOnlyList<Ruta>>.Exito(ReglaAlternativas.Juntar(rutas.Valor));
            UnityEngine.Debug.LogWarning("PriceMap: OSRM sin rutas: " + rutas.Error);
            return rutas;
        }
    }
}
