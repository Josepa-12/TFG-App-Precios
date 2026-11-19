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

        /// <summary>
        /// La app pasa a segundo plano: apaga ubicación y brújula (NFR-3). Durante una ruta no apaga el GPS: lo
        /// mantiene el servicio en primer plano de Android (AD-19).
        /// </summary>
        void Detener();

        /// <summary>Hay una ruta en marcha (entre IniciarRuta y TerminarRuta).</summary>
        bool RutaActiva { get; }

        /// <summary>
        /// Ruta en marcha sin GPS en segundo plano: en Android 13 o superior se denegó el permiso de notificaciones,
        /// así que la navegación se pausa al apagar la pantalla. La UI lo explica con un aviso. Avisa por Cambiado.
        /// </summary>
        bool RutaSoloPrimerPlano { get; }

        /// <summary>
        /// Empieza una ruta (AD-19): con la app delante arranca el GPS en segundo plano con una notificación fija
        /// con este texto ("Navegando a &lt;destino&gt; · N min"). Las posiciones llegan por Posicion y Cambiado,
        /// también las que se tomaron con la pantalla apagada, en orden, al volver a la app. Sin permiso de
        /// ubicación devuelve un fallo y no arranca nada. El valor es true si el GPS en segundo plano ya está en
        /// marcha; false si de momento solo funciona con la app delante (esperando el permiso de notificaciones,
        /// o en el editor y en PC, donde no hay servicio).
        /// </summary>
        Resultado<bool> IniciarRuta(string textoNotificacion);

        /// <summary>Cambia el texto de la notificación de la ruta (p. ej. el tiempo restante). Sin ruta, nada.</summary>
        void ActualizarNotificacion(string textoNotificacion);

        /// <summary>Termina la ruta: quita la notificación y no llegan más posiciones en segundo plano.</summary>
        void TerminarRuta();

        /// <summary>
        /// Abre los ajustes del sistema donde se arregla el estado actual: con el permiso denegado, los de la app
        /// (Aplicaciones &gt; PriceMap &gt; Permisos); con la ubicación del móvil apagada, los de ubicación.
        /// </summary>
        void AbrirAjustes();
    }
}
