#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Android;

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Servicio de ruta real (historia 2.1, AD-19): llama por JNI al plugin de Assets/Plugins/Android
    /// (com.pricemap.ruta.PuenteUbicacionRuta). Es el único sitio de la app que arranca o para el GPS en segundo
    /// plano, y solo lo usa UbicacionGps durante una ruta. Solo se compila en Android.
    /// </summary>
    internal class ServicioRutaAndroid : IServicioRuta
    {
        const string ClasePuente = "com.pricemap.ruta.PuenteUbicacionRuta";
        /// <summary>La actividad de la app (historia 2.8, parte 2): navegando no pausa Unity en segundo plano.</summary>
        const string ClaseActividad = "com.pricemap.app.ActividadPrincipal";
        const string PermisoNotificaciones = "android.permission.POST_NOTIFICATIONS";
        const int Android13 = 33;

        readonly string objetoUnity;
        AndroidJavaClass puente;

        /// <param name="objetoUnity">GameObject con UbicacionUnity: recibe AlPosicionRuta por UnitySendMessage.</param>
        public ServicioRutaAndroid(string objetoUnity) => this.objetoUnity = objetoUnity;

        AndroidJavaClass Puente => puente ??= new AndroidJavaClass(ClasePuente);

        public bool NecesitaPermisoNotificaciones
        {
            get
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                return version.GetStatic<int>("SDK_INT") >= Android13 && !Permission.HasUserAuthorizedPermission(PermisoNotificaciones);
            }
        }

        public void PedirPermisoNotificaciones(Action<bool> respuesta)
        {
            var respuestas = new PermissionCallbacks();
            respuestas.PermissionGranted += _ => respuesta(true);
            respuestas.PermissionDenied += _ => respuesta(false);
            respuestas.PermissionRequestDismissed += _ => respuesta(false); // diálogo cerrado sin responder
            Permission.RequestUserPermission(PermisoNotificaciones, respuestas);
        }

        public void Iniciar(string textoNotificacion)
        {
            Llamar("iniciar", textoNotificacion, objetoUnity);
            Seguir(true);
        }

        public void ActualizarTexto(string textoNotificacion) => Llamar("actualizar", textoNotificacion);

        public void Parar()
        {
            Seguir(false); // primero: así, si la app está detrás, la actividad pausa Unity como siempre
            Llamar("parar");
        }

        /// <summary>
        /// Con ruta (true), la actividad no pausa Unity al ir a segundo plano y le avisa con AlSegundoPlano (al mismo
        /// GameObject que AlPosicionRuta, el del Arranque); sin ruta (false), todo como siempre.
        /// </summary>
        void Seguir(bool activo)
        {
            try
            {
                using var actividad = new AndroidJavaClass(ClaseActividad);
                actividad.CallStatic("seguir", activo, objetoUnity);
            }
            catch (Exception e)
            {
                Debug.LogWarning("PriceMap: la actividad no puede seguir en segundo plano: " + e.Message);
            }
        }

        public bool EnMarcha
        {
            get
            {
                try { return Puente.CallStatic<bool>("enMarcha"); }
                catch (Exception e) { Debug.LogWarning("PriceMap: no se pudo saber si el servicio de ruta sigue: " + e.Message); return false; }
            }
        }

        public string VaciarCola()
        {
            try { return Puente.CallStatic<string>("vaciarCola") ?? ""; }
            catch (Exception e) { Debug.LogWarning("PriceMap: no se pudo leer la cola del servicio de ruta: " + e.Message); return ""; }
        }

        void Llamar(string metodo, params object[] argumentos)
        {
            try
            {
                Puente.CallStatic(metodo, argumentos);
                Debug.Log("PriceMap: servicio de ruta (" + metodo + ").");
            }
            catch (Exception e)
            {
                Debug.LogWarning("PriceMap: fallo en el servicio de ruta (" + metodo + "): " + e.Message);
            }
        }
    }
}
#endif
