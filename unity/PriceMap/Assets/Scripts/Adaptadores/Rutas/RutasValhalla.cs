using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Rutas con Valhalla (servidor de FOSSGIS, historia 2.6): la más corta (<c>shortest</c>) en coche
    /// (<c>auto</c>, respetando el sentido de circulación) o a pie (<c>pedestrian</c>), con las maniobras en
    /// castellano. Valhalla empieza y acaba en el punto de calle más cercano. Gratis y sin clave. Nada se guarda.
    /// </summary>
    public sealed class RutasValhalla : IRutas
    {
        public const string Url = "https://valhalla1.openstreetmap.de/route";

        public static string Costing(ModoViaje modo) => modo == ModoViaje.APie ? "pedestrian" : "auto";

        /// <summary>
        /// Cuerpo JSON de la petición: los dos puntos (origen con 5 decimales, destino con 6), el costing con
        /// <c>shortest: true</c>, <c>language: es-ES</c>, <c>directions_type: instructions</c> (con "maneuvers" Valhalla no da el texto de cada maniobra; comprobado el 20/11/2026) y kilómetros.
        /// </summary>
        public static string Cuerpo(Coordenada origen, Coordenada destino, ModoViaje modo)
        {
            string c = Costing(modo);
            return "{\"locations\":["
                   + "{\"lat\":" + PeticionRutas.Origen(origen.Latitud) + ",\"lon\":" + PeticionRutas.Origen(origen.Longitud) + "},"
                   + "{\"lat\":" + PeticionRutas.Destino(destino.Latitud) + ",\"lon\":" + PeticionRutas.Destino(destino.Longitud) + "}],"
                   + "\"costing\":\"" + c + "\","
                   + "\"costing_options\":{\"" + c + "\":{\"shortest\":true}},"
                   + "\"language\":\"es-ES\","
                   + "\"directions_type\":\"instructions\","
                   + "\"units\":\"kilometers\"}";
        }

        public async Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            var r = await PeticionRutas.Pedir("Valhalla", Url, Cuerpo(origen, destino, modo), ct);
            if (!r.Ok) return Resultado<Ruta>.Fallo(r.Error);
            return LecturaValhalla.Leer(r.Valor);
        }
    }
}
