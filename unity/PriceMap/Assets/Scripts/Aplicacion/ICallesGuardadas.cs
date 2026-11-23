using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de las calles guardadas (spec buscar-calles-guardadas): busca en los nombres de calle de las teselas que
    /// el mapa ya tiene en el móvil. Nunca sale a la red, no bloquea el hilo que llama y no guarda nada (FR17).
    /// </summary>
    public interface ICallesGuardadas
    {
        /// <param name="consulta">Texto tal como lo ha escrito el usuario.</param>
        /// <param name="cerca">Punto de cercanía (el usuario o el centro del mapa).</param>
        /// <returns>Las calles que coinciden (ReglaCalles.Buscar), cada una con un punto de la propia calle.</returns>
        Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct);
    }
}
