using System;
using System.Threading;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Lógica de IUbicacion sobre una IFuenteUbicacion: permiso, estados (SinPermiso, Denegado, Buscando, Activa,
    /// Apagada), lecturas del GPS y de la brújula. Solo enciende el GPS con permiso, con la ubicación del móvil
    /// encendida y con la app delante (NFR-3). UbicacionUnity la crea con la fuente real y llama a Actualizar en
    /// cada fotograma; las pruebas la usan con una fuente falsa.
    /// </summary>
    internal class UbicacionGps : IUbicacion
    {
        /// <summary>Intent de los ajustes de la app (Aplicaciones &gt; PriceMap): permiso denegado.</summary>
        public const string AjustesApp = "android.settings.APPLICATION_DETAILS_SETTINGS";
        /// <summary>Intent de los ajustes de ubicación del móvil: ubicación apagada.</summary>
        public const string AjustesUbicacion = "android.settings.LOCATION_SOURCE_SETTINGS";
        /// <summary>Cambio de rumbo (grados) por debajo del cual no se avisa: la brújula tiembla.</summary>
        public const float UmbralRumbo = 1f;

        readonly IFuenteUbicacion fuente;
        EstadoUbicacion estado = EstadoUbicacion.SinPermiso;
        Coordenada? posicion;
        float? rumbo;
        bool quiereFuncionar; // Iniciar sin Detener después: la app está delante
        bool encendido;       // GPS y brújula encendidos
        double ultimaLectura;

        // Respuesta del permiso: puede llegar desde el hilo de Java; se aplica en Actualizar
        int respuestaPendiente; // 0 nada, 1 concedido, 2 denegado (se lee y se pone a 0 con Interlocked)

        public UbicacionGps(IFuenteUbicacion fuente)
        {
            this.fuente = fuente ?? throw new ArgumentNullException(nameof(fuente));
            if (fuente.TienePermiso) estado = EstadoUbicacion.Buscando;
        }

        public EstadoUbicacion Estado => estado;
        public Coordenada? Posicion => posicion;
        public float? Rumbo => rumbo;
        public event Action Cambiado;

        /// <summary>True con el GPS y la brújula encendidos (para las pruebas).</summary>
        internal bool Encendido => encendido;

        public void PedirPermiso()
        {
            if (fuente.TienePermiso) { AlConceder(); return; }
            fuente.PedirPermiso(concedido =>
            {
                if (concedido) Interlocked.Exchange(ref respuestaPendiente, 1);
                else Interlocked.CompareExchange(ref respuestaPendiente, 2, 0); // un "concedido" no se pisa
            });
        }

        public void Iniciar()
        {
            quiereFuncionar = true;
            if (!fuente.TienePermiso)
            {
                // Se quitó el permiso desde Ajustes con la app en segundo plano
                if (estado == EstadoUbicacion.Buscando || estado == EstadoUbicacion.Activa || estado == EstadoUbicacion.Apagada)
                    Cambiar(EstadoUbicacion.SinPermiso, null, null);
                return;
            }
            ArrancarConPermiso();
        }

        public void Detener()
        {
            quiereFuncionar = false;
            Apagar();
        }

        public void AbrirAjustes() => fuente.AbrirAjustes(estado == EstadoUbicacion.Apagada ? AjustesUbicacion : AjustesApp);

        void AlConceder() => ArrancarConPermiso();

        void AlDenegar()
        {
            Apagar();
            Cambiar(EstadoUbicacion.Denegado, null, null);
        }

        /// <summary>
        /// Con permiso: si la ubicación del móvil está apagada, Apagada; si no, Buscando (o sigue Activa con la
        /// última posición) y, con la app delante, enciende el GPS. Con el diálogo del permiso la app pierde el
        /// foco: si está en pausa, se enciende al volver (Iniciar).
        /// </summary>
        void ArrancarConPermiso()
        {
            if (!fuente.UbicacionEncendida)
            {
                Apagar();
                Cambiar(EstadoUbicacion.Apagada, null, null);
                return;
            }
            if (estado != EstadoUbicacion.Activa) Cambiar(EstadoUbicacion.Buscando, null, null);
            if (quiereFuncionar) Encender();
        }

        void Encender()
        {
            if (encendido) return;
            encendido = true;
            fuente.Encender();
        }

        void Apagar()
        {
            if (!encendido) return;
            encendido = false;
            fuente.Apagar();
        }

        /// <summary>Una vez por fotograma: aplica la respuesta del permiso y lee GPS y brújula.</summary>
        public void Actualizar()
        {
            int respuesta = Interlocked.Exchange(ref respuestaPendiente, 0);
            if (respuesta != 0)
            {
                if (respuesta == 1 || fuente.TienePermiso) AlConceder(); else AlDenegar();
            }
            if (!encendido) return;

            // Se apagó la ubicación del móvil con la app abierta, o el servicio no arranca
            if (!fuente.UbicacionEncendida || fuente.EstadoServicio == LocationServiceStatus.Failed)
            {
                Apagar();
                Cambiar(EstadoUbicacion.Apagada, null, null);
                return;
            }

            var nuevoEstado = estado;
            var nuevaPosicion = posicion;
            if (fuente.EstadoServicio == LocationServiceStatus.Running)
            {
                var (lectura, marca) = fuente.UltimaLectura;
                if (marca > 0 && marca != ultimaLectura)
                {
                    ultimaLectura = marca;
                    nuevaPosicion = lectura;
                    nuevoEstado = EstadoUbicacion.Activa;
                }
            }
            // Sin brújula (o sin lecturas aún): solo el círculo
            float? nuevoRumbo = fuente.Rumbo.HasValue ? Normalizar(fuente.Rumbo.Value) : (float?)null;
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
    }
}
