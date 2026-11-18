using System;
using System.Threading;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Adaptador de IUbicacion con el GPS (Input.location) y la brújula (Input.compass) de Unity, y el
    /// permiso de ubicación de Android. Solo enciende el GPS con permiso y con la app delante (NFR-3):
    /// Iniciar y Detener los llama ServicioUbicacion al abrir, pausar y reanudar la app.
    /// Lo crea el Arranque como componente (necesita Update para leer las lecturas nuevas).
    /// </summary>
    public class UbicacionUnity : MonoBehaviour, IUbicacion
    {
        /// <summary>Precisión pedida al GPS y distancia mínima entre lecturas (m).</summary>
        const float PrecisionMetros = 10f, DistanciaMinimaMetros = 2f;
        /// <summary>Cambio de rumbo (grados) por debajo del cual no se avisa: la brújula tiembla.</summary>
        const float UmbralRumbo = 1f;

        EstadoUbicacion estado = EstadoUbicacion.SinPermiso;
        Coordenada? posicion;
        float? rumbo;
        bool quiereFuncionar; // Iniciar sin Detener después: la app está delante
        bool encendido;       // Input.location y la brújula están encendidos
        double ultimaLectura;

        // Respuesta del permiso: puede llegar desde el hilo de Java; se aplica en Update
        int respuestaPendiente; // 0 nada, 1 concedido, 2 denegado (se lee y se pone a 0 con Interlocked)

        public EstadoUbicacion Estado => estado;
        public Coordenada? Posicion => posicion;
        public float? Rumbo => rumbo;
        public event Action Cambiado;

        void Awake()
        {
            if (TienePermiso()) estado = EstadoUbicacion.Buscando;
        }

        public void PedirPermiso()
        {
            if (TienePermiso()) { AlConceder(); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            var respuestas = new PermissionCallbacks();
            respuestas.PermissionGranted += _ => Interlocked.Exchange(ref respuestaPendiente, 1);
            respuestas.PermissionDenied += _ => Interlocked.CompareExchange(ref respuestaPendiente, 2, 0);
            // Desde Android 12 hay que pedir la precisa y la aproximada juntas (si no, el sistema ignora la petición)
            Permission.RequestUserPermissions(new[] { Permission.FineLocation, Permission.CoarseLocation }, respuestas);
#else
            // Fuera de Android no hay diálogo: vale si el sistema tiene la ubicación activada
            Debug.Log("PriceMap: sin permisos de Android; la ubicación del sistema está " + (Input.location.isEnabledByUser ? "activada." : "desactivada."));
            AlDenegar();
#endif
        }

        public void Iniciar()
        {
            quiereFuncionar = true;
            if (!TienePermiso())
            {
                // Se quitó el permiso desde Ajustes con la app en segundo plano
                if (estado == EstadoUbicacion.Buscando || estado == EstadoUbicacion.Activa) Cambiar(EstadoUbicacion.SinPermiso, null, null);
                return;
            }
            if (estado == EstadoUbicacion.SinPermiso || estado == EstadoUbicacion.Denegado) Cambiar(EstadoUbicacion.Buscando, null, null);
            Encender();
        }

        public void Detener()
        {
            quiereFuncionar = false;
            Apagar();
        }

        static bool TienePermiso()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.FineLocation)
                   || Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
#else
            return Input.location.isEnabledByUser;
#endif
        }

        void AlConceder()
        {
            if (estado != EstadoUbicacion.Activa) Cambiar(EstadoUbicacion.Buscando, null, null);
            // Con el diálogo del permiso la app pierde el foco: si está en pausa, se enciende al volver (Iniciar)
            if (quiereFuncionar) Encender();
        }

        void AlDenegar()
        {
            Apagar();
            Cambiar(EstadoUbicacion.Denegado, null, null);
        }

        void Encender()
        {
            if (encendido) return;
            encendido = true;
            Input.location.Start(PrecisionMetros, DistanciaMinimaMetros);
            Input.compass.enabled = true;
            Debug.Log("PriceMap: ubicación y brújula encendidas.");
        }

        void Apagar()
        {
            if (!encendido) return;
            encendido = false;
            Input.location.Stop();
            Input.compass.enabled = false;
            Debug.Log("PriceMap: ubicación y brújula apagadas.");
        }

        void Update()
        {
            int respuesta = Interlocked.Exchange(ref respuestaPendiente, 0);
            if (respuesta != 0)
            {
                if (respuesta == 1 || TienePermiso()) AlConceder(); else AlDenegar();
            }
            if (!encendido) return;

            var nuevoEstado = estado;
            var nuevaPosicion = posicion;
            if (Input.location.status == LocationServiceStatus.Running)
            {
                var lectura = Input.location.lastData;
                if (lectura.timestamp > 0 && lectura.timestamp != ultimaLectura)
                {
                    ultimaLectura = lectura.timestamp;
                    nuevaPosicion = new Coordenada(lectura.latitude, lectura.longitude);
                    nuevoEstado = EstadoUbicacion.Activa;
                }
            }
            // Sin brújula (o sin lecturas aún) el timestamp sigue en 0: solo el círculo
            float? nuevoRumbo = Input.compass.enabled && Input.compass.timestamp > 0 ? Normalizar(Input.compass.trueHeading) : (float?)null;
            if (nuevoRumbo.HasValue && rumbo.HasValue && Mathf.Abs(Mathf.DeltaAngle(nuevoRumbo.Value, rumbo.Value)) < UmbralRumbo)
                nuevoRumbo = rumbo;
            Cambiar(nuevoEstado, nuevaPosicion, nuevoRumbo);
        }

        static float Normalizar(float grados) => Mathf.Repeat(grados, 360f);

        void Cambiar(EstadoUbicacion nuevoEstado, Coordenada? nuevaPosicion, float? nuevoRumbo)
        {
            if (nuevoEstado == estado && nuevaPosicion == posicion && nuevoRumbo == rumbo) return;
            bool cambioEstado = nuevoEstado != estado;
            estado = nuevoEstado;
            posicion = nuevaPosicion;
            rumbo = nuevoRumbo;
            if (cambioEstado) Debug.Log($"PriceMap: ubicación {estado}.");
            Cambiado?.Invoke();
        }

        void OnDestroy() => Apagar();
    }
}
