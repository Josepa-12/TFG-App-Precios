using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Rutas de reserva con OSRM (servidor de FOSSGIS en routing.openstreetmap.de, historia 2.6): el de
    /// demostración de OSRM solo tiene coche. Pide hasta 3 alternativas y se queda con la de menor distancia
    /// (LecturaOsrm). Las maniobras llegan sin texto (tipo y modificador); sus textos son de la historia 2.7.
    /// </summary>
    public sealed class RutasOsrm : IRutas
    {
        public const string Servidor = "https://routing.openstreetmap.de/";

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
            var r = await PeticionRutas.Pedir("OSRM", Url(origen, destino, modo), null, ct);
            if (!r.Ok) return Resultado<Ruta>.Fallo(r.Error);
            return LecturaOsrm.Leer(r.Valor);
        }
    }
}
