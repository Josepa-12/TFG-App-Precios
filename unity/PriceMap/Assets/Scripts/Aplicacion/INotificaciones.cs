using System;

namespace PriceMap.Aplicacion
{
    /// <summary>Permiso para mostrar notificaciones (historia 3.1).</summary>
    public enum EstadoNotificaciones
    {
        /// <summary>Aún no se ha concedido (ni se ha denegado en esta sesión).</summary>
        SinPermiso,
        Concedido,
        /// <summary>Android lo ha denegado en esta sesión.</summary>
        Denegado,
    }

    /// <summary>
    /// Puerto del permiso de notificaciones (historia 3.1): Android 13 o superior lo pide; antes, en el editor y en PC
    /// está concedido.
    /// </summary>
    public interface INotificaciones
    {
        /// <summary>Se lee al momento: también si se ha cambiado en los Ajustes del móvil.</summary>
        EstadoNotificaciones Estado { get; }

        /// <summary>Android aún puede preguntar; false tras denegarlo de forma definitiva (solo quedan los Ajustes del móvil).</summary>
        bool PuedePedir => true;

        /// <summary>Ha llegado la respuesta a PedirPermiso (en el hilo principal).</summary>
        event Action Cambiado;

        /// <summary>Lanza la petición de permiso de Android. Si ya está concedido, solo avisa con Cambiado.</summary>
        void PedirPermiso();
    }
}
