using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Una dirección encontrada por el geocodificador (historia 2.3, parte 2): lo que se lee ("Calle Ancha 10"), su
    /// localidad ("Sanlúcar de Barrameda"; puede ir vacía) y dónde está. Nada se guarda (FR17).
    /// </summary>
    public sealed class Direccion
    {
        public string Nombre { get; }
        public string Localidad { get; }
        public Coordenada Posicion { get; }
        /// <summary>Calle sola ("Calle Ancha"; vacía si el sitio no está en una calle): la usa la ficha de punto libre.</summary>
        public string Calle { get; }
        /// <summary>Número del portal ("10"; vacío si no lo hay).</summary>
        public string Numero { get; }

        public Direccion(string nombre, string localidad, Coordenada posicion, string calle = null, string numero = null)
        {
            Nombre = nombre ?? "";
            Localidad = localidad ?? "";
            Posicion = posicion;
            Calle = calle ?? "";
            Numero = numero ?? "";
        }

        public override string ToString() => string.IsNullOrEmpty(Localidad) ? Nombre : $"{Nombre}, {Localidad}";
    }

    /// <summary>
    /// Puerto del buscador de direcciones (historia 2.3, parte 2; FR14) y de la geocodificación inversa del punto
    /// libre (historia 2.5; FR16). El adaptador real es Photon (komoot).
    /// Cualquier fallo (sin conexión, respuesta rota) vuelve como Resultado.Fallo, nunca como excepción; si se
    /// cancela con <c>ct</c>, la tarea puede acabar cancelada (OperationCanceledException) y nadie pinta nada.
    /// </summary>
    public interface IGeocodificador
    {
        /// <param name="consulta">Texto tal como lo ha escrito el usuario.</param>
        /// <param name="cerca">Punto de cercanía (el usuario o el centro del mapa): las direcciones cercanas primero.</param>
        Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct);

        /// <summary>
        /// Geocodificación inversa (historia 2.5): la dirección más cercana a ese punto, o null si no hay ninguna.
        /// Mismo trato de fallos y cancelación que Buscar.
        /// </summary>
        Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct);
    }
}
