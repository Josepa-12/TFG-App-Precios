using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de rutas (historia 2.6): la ruta por calles reales entre dos puntos, en coche (la más rápida, parte 5a de
    /// «Cómo llegar») o a pie. Adaptadores: Valhalla (principal) y OSRM (reserva). Los fallos (sin conexión, HTTP,
    /// respuesta rota) vuelven como Resultado.Fallo; cancelar termina en OperationCanceledException. Nada se guarda (FR17).
    /// </summary>
    public interface IRutas
    {
        Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct);

        /// <summary>
        /// Las rutas entre las que elegir, como las deja ReglaAlternativas: por tiempo, sin repetidas y tres como mucho.
        /// Nunca una lista vacía con éxito. Por defecto, la de Calcular sola.
        /// </summary>
        async Task<Resultado<IReadOnlyList<Ruta>>> Alternativas(Coordenada origen, Coordenada destino, ModoViaje modo, CancellationToken ct)
        {
            var r = await Calcular(origen, destino, modo, ct);
            if (!r.Ok) return Resultado<IReadOnlyList<Ruta>>.Fallo(r.Error);
            return r.Valor != null
                ? Resultado<IReadOnlyList<Ruta>>.Exito(new[] { r.Valor })
                : Resultado<IReadOnlyList<Ruta>>.Fallo("El servicio de rutas no ha dado ninguna ruta.");
        }
    }
}
