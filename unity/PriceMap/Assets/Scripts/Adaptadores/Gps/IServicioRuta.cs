using System;

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Lo que UbicacionGps necesita del servicio nativo de ruta (historia 2.1, AD-19): un servicio en primer plano
    /// de Android con notificación fija que sigue leyendo el GPS con Unity en pausa y guarda las posiciones en una
    /// cola. La implementación real (ServicioRutaAndroid) solo se compila en Android; las pruebas usan una falsa.
    /// En el editor y en PC no hay servicio (null) y la ruta sigue con la ubicación de Unity.
    /// </summary>
    internal interface IServicioRuta
    {
        /// <summary>Android 13 o superior sin el permiso POST_NOTIFICATIONS: hay que pedirlo antes de arrancar.</summary>
        bool NecesitaPermisoNotificaciones { get; }

        /// <summary>Pide POST_NOTIFICATIONS. La respuesta (true concedido) puede llegar desde el hilo de Java.</summary>
        void PedirPermisoNotificaciones(Action<bool> respuesta);

        /// <summary>Arranca el servicio con la notificación fija ("Navegando a &lt;destino&gt; · N min").</summary>
        void Iniciar(string textoNotificacion);

        /// <summary>Cambia el texto de la notificación.</summary>
        void ActualizarTexto(string textoNotificacion);

        /// <summary>Para el servicio: quita la notificación y vacía la cola sin entregarla.</summary>
        void Parar();

        /// <summary>
        /// True si el servicio está de verdad en marcha (en Android: arrancado en primer plano y vivo). Puede ser
        /// false después de Iniciar si no pudo arrancar o si el sistema lo paró.
        /// </summary>
        bool EnMarcha { get; }

        /// <summary>Devuelve y borra la cola en el formato de LecturaRuta.Leer (cadena vacía si no hay nada).</summary>
        string VaciarCola();
    }
}
