using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de rutas (historia 2.6): la ruta más corta por calles reales entre dos puntos, en coche o a pie.
    /// Adaptadores: Valhalla (principal) y OSRM (reserva). Los fallos (sin conexión, HTTP, respuesta rota) vuelven
    /// como Resultado.Fallo; cancelar termina en OperationCanceledException. Nada se guarda (FR17).
    /// </summary>
    public interface IRutas
    {
        Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct);
    }
}
