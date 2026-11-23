using System;
using System.Threading;
using PriceMap.Aplicacion;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.Android;
#endif

namespace PriceMap.Adaptadores.Sistema
{
    /// <summary>
    /// Permiso de notificaciones (historia 3.1): POST_NOTIFICATIONS desde Android 13 (ya en el manifiesto,
    /// PermisosUbicacionAndroid). En Android anterior, en el editor y en PC está concedido sin diálogo. La respuesta puede
    /// llegar desde otro hilo: se aplica en el que creó este objeto (el principal).
    /// </summary>
    public class NotificacionesAndroid : INotificaciones
    {
        /// <summary>Primera versión de Android (API) que pide permiso para las notificaciones.</summary>
        internal const int Android13 = 33;

        readonly IPermisoNotificaciones permiso;
        readonly SynchronizationContext hilo = SynchronizationContext.Current;
        readonly PreguntaPermiso pregunta = new PreguntaPermiso();
        bool denegado, explicaba;

        public NotificacionesAndroid() : this(PermisoReal()) { }

        internal NotificacionesAndroid(IPermisoNotificaciones permiso) =>
            this.permiso = permiso ?? throw new ArgumentNullException(nameof(permiso));

        public EstadoNotificaciones Estado =>
            Concedido ? EstadoNotificaciones.Concedido : denegado ? EstadoNotificaciones.Denegado : EstadoNotificaciones.SinPermiso;

        public bool PuedePedir => pregunta.PuedePedir;
        public event Action Cambiado;

        bool Concedido => permiso.Version < Android13 || permiso.Concedido;

        public void PedirPermiso()
        {
            if (Concedido) { Cambiado?.Invoke(); return; }
            explicaba = permiso.Explica;
            permiso.Pedir(respuesta =>
            {
                if (hilo != null) hilo.Post(_ => Responder(respuesta), null);
                else Responder(respuesta);
            });
        }

        // null: diálogo cerrado sin responder (queda como estaba)
        void Responder(bool? concedido)
        {
            if (concedido == true) denegado = false;
            else if (concedido == false)
            {
                denegado = true;
                pregunta.Denegar(explicaba, permiso.Explica);
            }
            Cambiado?.Invoke();
        }

        static IPermisoNotificaciones PermisoReal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new PermisoAndroid();
#else
            return new PermisoConcedido();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        class PermisoAndroid : IPermisoNotificaciones
        {
            const string Permiso = "android.permission.POST_NOTIFICATIONS";
            int version;

            public int Version
            {
                get
                {
                    if (version != 0) return version;
                    using var clase = new AndroidJavaClass("android.os.Build$VERSION");
                    return version = clase.GetStatic<int>("SDK_INT");
                }
            }

            public bool Concedido => Permission.HasUserAuthorizedPermission(Permiso);
            public bool Explica => Permission.ShouldShowRequestPermissionRationale(Permiso);

            public void Pedir(Action<bool?> respuesta)
            {
                var respuestas = new PermissionCallbacks();
                respuestas.PermissionGranted += _ => respuesta(true);
                respuestas.PermissionDenied += _ => respuesta(false);
                respuestas.PermissionRequestDismissed += _ => respuesta(null);
                Permission.RequestUserPermission(Permiso, respuestas);
            }
        }
#else
        class PermisoConcedido : IPermisoNotificaciones
        {
            public int Version => Android13;
            public bool Concedido => true;
            public bool Explica => false;
            public void Pedir(Action<bool?> respuesta) => respuesta(true);
        }
#endif
    }

    /// <summary>Lo que NotificacionesAndroid necesita del móvil; las pruebas usan uno falso.</summary>
    internal interface IPermisoNotificaciones
    {
        /// <summary>Versión de Android (API).</summary>
        int Version { get; }
        bool Concedido { get; }
        /// <summary>Android aconseja explicar el permiso (shouldShowRequestPermissionRationale).</summary>
        bool Explica { get; }
        /// <summary>Lanza el diálogo; la respuesta (true, false o null si se cierra sin responder) puede llegar desde otro hilo.</summary>
        void Pedir(Action<bool?> respuesta);
    }
}
