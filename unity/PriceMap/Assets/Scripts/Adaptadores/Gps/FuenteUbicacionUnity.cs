using System;
using PriceMap.Dominio;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Fuente real de la ubicación: Input.location (GPS), Input.compass (brújula), el permiso de Android
    /// (Permission) y los Intents de los ajustes del sistema. Fuera de Android no hay diálogo de permiso: vale
    /// si el sistema tiene la ubicación activada, y los ajustes no se abren.
    /// </summary>
    internal class FuenteUbicacionUnity : IFuenteUbicacion
    {
        /// <summary>Precisión pedida al GPS y distancia mínima entre lecturas (m).</summary>
        const float PrecisionMetros = 10f, DistanciaMinimaMetros = 2f;
        const int FlagActivityNewTask = 0x10000000; // Intent.FLAG_ACTIVITY_NEW_TASK

        public bool TienePermiso
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.FineLocation)
                       || Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
#else
                return Input.location.isEnabledByUser;
#endif
            }
        }

        public bool UbicacionEncendida => Input.location.isEnabledByUser;

        public LocationServiceStatus EstadoServicio => Input.location.status;

        public (Coordenada posicion, double marca) UltimaLectura
        {
            get
            {
                var l = Input.location.lastData;
                return (new Coordenada(l.latitude, l.longitude), l.timestamp);
            }
        }

        // Sin brújula (o sin lecturas aún) el timestamp sigue en 0
        public float? Rumbo => Input.compass.enabled && Input.compass.timestamp > 0 ? Input.compass.trueHeading : (float?)null;

        public void PedirPermiso(Action<bool> respuesta)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var respuestas = new PermissionCallbacks();
            respuestas.PermissionGranted += _ => respuesta(true);
            respuestas.PermissionDenied += _ => respuesta(false);
            // Desde Android 12 hay que pedir la precisa y la aproximada juntas (si no, el sistema ignora la petición)
            Permission.RequestUserPermissions(new[] { Permission.FineLocation, Permission.CoarseLocation }, respuestas);
#else
            Debug.Log("PriceMap: sin permisos de Android; la ubicación del sistema está " + (Input.location.isEnabledByUser ? "activada." : "desactivada."));
            respuesta(false);
#endif
        }

        public void Encender()
        {
            Input.location.Start(PrecisionMetros, DistanciaMinimaMetros);
            Input.compass.enabled = true;
            Debug.Log("PriceMap: ubicación y brújula encendidas.");
        }

        public void Apagar()
        {
            Input.location.Stop();
            Input.compass.enabled = false;
            Debug.Log("PriceMap: ubicación y brújula apagadas.");
        }

        public void AbrirAjustes(string accion)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = new AndroidJavaObject("android.content.Intent", accion);
                if (accion == UbicacionGps.AjustesApp)
                {
                    // Pantalla de esta app: "package:com.pricemap.app"
                    using var claseUri = new AndroidJavaClass("android.net.Uri");
                    using var uri = claseUri.CallStatic<AndroidJavaObject>("parse", "package:" + actividad.Call<string>("getPackageName"));
                    intent.Call<AndroidJavaObject>("setData", uri)?.Dispose();
                }
                intent.Call<AndroidJavaObject>("addFlags", FlagActivityNewTask)?.Dispose();
                actividad.Call("startActivity", intent);
                Debug.Log("PriceMap: ajustes abiertos (" + accion + ").");
            }
            catch (Exception e)
            {
                Debug.LogWarning("PriceMap: no se pudieron abrir los ajustes (" + accion + "): " + e.Message);
            }
#else
            Debug.Log("PriceMap: fuera de Android no se abren los ajustes (" + accion + ").");
#endif
        }
    }
}
