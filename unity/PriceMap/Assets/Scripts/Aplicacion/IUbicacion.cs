using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de la ubicación del usuario: GPS y brújula del móvil. Dominio y Aplicación no ven
    /// Input.location ni los permisos de Android; eso queda en el adaptador (PriceMap.Adaptadores.Gps).
    /// </summary>
    public interface IUbicacion
    {
        EstadoUbicacion Estado { get; }

        /// <summary>Última posición conocida; null si aún no hay ninguna.</summary>
        Coordenada? Posicion { get; }

        /// <summary>Rumbo del móvil en grados respecto al norte real (0 = norte, 90 = este); null sin brújula.</summary>
        float? Rumbo { get; }

        /// <summary>Se lanza cuando cambia el estado, la posición o el rumbo.</summary>
        event Action Cambiado;

        /// <summary>Lanza la petición de permiso del sistema (Android). Si ya está concedido, empieza a buscar.</summary>
        void PedirPermiso();

        /// <summary>Enciende ubicación y brújula si hay permiso (al abrir la app y al volver a ella).</summary>
        void Iniciar();

        /// <summary>Apaga ubicación y brújula (la app pasa a segundo plano: NFR-3).</summary>
        void Detener();

        /// <summary>
        /// Abre los ajustes del sistema donde se arregla el estado actual: con el permiso denegado, los de la app
        /// (Aplicaciones &gt; PriceMap &gt; Permisos); con la ubicación del móvil apagada, los de ubicación.
        /// </summary>
        void AbrirAjustes();
    }
}
