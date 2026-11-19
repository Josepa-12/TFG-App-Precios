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

        public Direccion(string nombre, string localidad, Coordenada posicion)
        {
            Nombre = nombre ?? "";
            Localidad = localidad ?? "";
            Posicion = posicion;
        }

        public override string ToString() => string.IsNullOrEmpty(Localidad) ? Nombre : $"{Nombre}, {Localidad}";
    }

    /// <summary>
    /// Puerto del buscador de direcciones (historia 2.3, parte 2; FR14). El adaptador real es Photon (komoot).
    /// Cualquier fallo (sin conexión, respuesta rota) vuelve como Resultado.Fallo, nunca como excepción; si se
    /// cancela con <c>ct</c>, la tarea puede acabar cancelada (OperationCanceledException) y nadie pinta nada.
    /// </summary>
    public interface IGeocodificador
    {
        /// <param name="consulta">Texto tal como lo ha escrito el usuario.</param>
        /// <param name="cerca">Punto de cercanía (el usuario o el centro del mapa): las direcciones cercanas primero.</param>
        Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct);
    }
}
