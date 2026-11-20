using System.Threading;
using System.Threading.Tasks;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto que resuelve un enlace corto de mapas (<c>maps.app.goo.gl/…</c>, <c>goo.gl/maps/…</c>) siguiendo sus
    /// redirecciones (historia 2.5, parte 2; FR17). Devuelve la URL final, que se lee con LectorUbicacionCompartida.
    /// Cualquier fallo (sin conexión, HTTP, demasiados saltos) vuelve como Resultado.Fallo, nunca como excepción; si se
    /// cancela con <c>ct</c>, la tarea puede acabar cancelada (OperationCanceledException) y nadie pinta nada.
    /// </summary>
    public interface IResolutorEnlaces
    {
        Task<Resultado<string>> Resolver(string url, CancellationToken ct);
    }
}
