using System;

namespace PriceMap.Aplicacion
{
    /// <summary>En qué punto está el servidor (cuentas y datos en la nube, historia 3.2).</summary>
    public enum EstadoServidor
    {
        /// <summary>Aún se está preparando; puede no llegar a estar.</summary>
        Iniciando,
        Listo,
        /// <summary>No lo hay (PC, editor, sin configuración o sin los servicios de Google): la app sigue sin cuenta.</summary>
        NoDisponible,
    }

    /// <summary>
    /// Puerto del servidor (historia 3.2): si se puede contar con él. La app arranca sin esperarlo y funciona sin él. Listo
    /// no quiere decir que haya conexión: sin ella, se leen y guardan los datos del móvil.
    /// </summary>
    public interface IServidor
    {
        EstadoServidor Estado { get; }

        /// <summary>El estado ha cambiado (en el hilo principal).</summary>
        event Action Cambiado;
    }
}
