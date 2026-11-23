using System;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Lo que UbicacionGps necesita del móvil: permiso, interruptor de ubicación, servicio de ubicación, brújula y
    /// ajustes del sistema. La implementación real (FuenteUbicacionUnity) envuelve Input.location, Input.compass,
    /// Permission y los Intents de Android; las pruebas usan una fuente falsa.
    /// </summary>
    internal interface IFuenteUbicacion
    {
        /// <summary>Permiso de ubicación (precisa o aproximada) concedido.</summary>
        bool TienePermiso { get; }

        /// <summary>La ubicación del móvil está encendida (Input.location.isEnabledByUser).</summary>
        bool UbicacionEncendida { get; }

        /// <summary>Estado del servicio de ubicación (Input.location.status).</summary>
        LocationServiceStatus EstadoServicio { get; }

        /// <summary>Última lectura del GPS y su marca de tiempo (0 si aún no hay ninguna).</summary>
        (Coordenada posicion, double marca) UltimaLectura { get; }

        /// <summary>Última ubicación que recuerda el sistema (la más reciente de sus proveedores); null si no hay.</summary>
        Coordenada? UltimaConocida => null;

        /// <summary>Rumbo de la brújula respecto al norte real, sin normalizar; null sin brújula o sin lecturas.</summary>
        float? Rumbo { get; }

        /// <summary>Android aconseja explicar el permiso (shouldShowRequestPermissionRationale); fuera de Android, false.</summary>
        bool ExplicaPermiso => false;

        /// <summary>
        /// Lanza la petición de permiso del sistema. La respuesta (true concedido, false denegado) puede llegar
        /// desde otro hilo, y más de una vez (precisa y aproximada).
        /// </summary>
        void PedirPermiso(Action<bool> respuesta);

        /// <summary>Enciende el GPS y la brújula.</summary>
        void Encender();

        /// <summary>Apaga el GPS y la brújula.</summary>
        void Apagar();

        /// <summary>Abre la pantalla de ajustes del sistema con esa acción de Intent (UbicacionGps.AjustesApp o AjustesUbicacion).</summary>
        void AbrirAjustes(string accion);
    }
}
