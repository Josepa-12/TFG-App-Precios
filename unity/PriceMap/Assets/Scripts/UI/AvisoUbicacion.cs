using System;
using PriceMap.Aplicacion;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Aviso de ubicación desactivada (historia 1.7, parte 2): la misma ventana modal que la explicación del
    /// permiso, con "Abrir Ajustes" y "Cerrar". Con el permiso denegado explica cómo activarlo en los ajustes de
    /// la app; con la ubicación del móvil apagada, que se encienda en los ajustes rápidos. También avisa de que una
    /// ruta se pausa al apagar la pantalla si se denegó el permiso de notificaciones (historia 2.1).
    /// </summary>
    public class AvisoUbicacion : ExplicacionUbicacion
    {
        /// <param name="alAbrirAjustes">"Abrir Ajustes": cierra y abre los ajustes del sistema (por el puerto IUbicacion).</param>
        /// <param name="alCerrar">"Cerrar" (o el botón atrás): solo cierra.</param>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public AvisoUbicacion(Action alAbrirAjustes, Action alCerrar, Func<string, string> textos = null)
            : base("aviso-ubicacion", ClavesTexto.UbicacionAvisoDenegado,
                ("abrir-ajustes", ClavesTexto.UbicacionAbrirAjustes, alAbrirAjustes), ("cerrar", ClavesTexto.UbicacionCerrar, alCerrar), textos)
        {
        }

        public Button AbrirAjustes => Principal;
        public Button Cerrar => Secundario;

        /// <summary>Estado con el que se abrió por última vez (Denegado o Apagada).</summary>
        public EstadoUbicacion Motivo { get; private set; } = EstadoUbicacion.Denegado;

        /// <summary>True si está abierto por el permiso de notificaciones denegado (no por el estado de la ubicación).</summary>
        public bool SinNotificaciones { get; private set; }

        /// <summary>Pone el texto que toca para ese estado y abre la ventana.</summary>
        public void Abrir(EstadoUbicacion estado)
        {
            SinNotificaciones = false;
            Motivo = estado;
            PonerTexto(ClaveTexto(estado));
            Mostrar(true);
        }

        /// <summary>
        /// Abre el aviso de ruta sin GPS en segundo plano: "Sin permiso de notificaciones, la navegación se pausa al
        /// apagar la pantalla." "Abrir Ajustes" lleva a los de la app, donde se activan las notificaciones.
        /// </summary>
        public void AbrirSinNotificaciones()
        {
            SinNotificaciones = true;
            PonerTexto(ClavesTexto.UbicacionAvisoNotificaciones);
            Mostrar(true);
        }

        /// <summary>Clave del texto del aviso: ubicación del móvil apagada o, si no, permiso denegado.</summary>
        public static string ClaveTexto(EstadoUbicacion estado) =>
            estado == EstadoUbicacion.Apagada ? ClavesTexto.UbicacionAvisoApagada : ClavesTexto.UbicacionAvisoDenegado;
    }
}
