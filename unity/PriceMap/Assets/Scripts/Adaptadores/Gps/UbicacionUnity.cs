using System;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Adaptador de IUbicacion con el GPS, la brújula y el permiso de ubicación de Android. Es un componente fino:
    /// la lógica está en UbicacionGps (probada con una fuente falsa) y el acceso a Unity y Android en
    /// FuenteUbicacionUnity. Lo crea el Arranque como componente para llamar a UbicacionGps.Actualizar en cada
    /// fotograma. Iniciar y Detener los llama ServicioUbicacion al abrir, pausar y reanudar la app (NFR-3). En
    /// Android le da a UbicacionGps el servicio de ruta nativo (ServicioRutaAndroid, AD-19), que avisa de cada
    /// posición con AlPosicionRuta; en el editor y en PC no hay servicio.
    /// </summary>
    public class UbicacionUnity : MonoBehaviour, IUbicacion
    {
        UbicacionGps gps;

        UbicacionGps Gps => gps ??= new UbicacionGps(new FuenteUbicacionUnity(), CrearServicioRuta());

        IServicioRuta CrearServicioRuta()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new ServicioRutaAndroid(gameObject.name);
#else
            return null;
#endif
        }

        void Awake() => _ = Gps;

        public EstadoUbicacion Estado => Gps.Estado;
        public Coordenada? Posicion => Gps.Posicion;
        public float? Rumbo => Gps.Rumbo;

        public event Action Cambiado
        {
            add => Gps.Cambiado += value;
            remove => Gps.Cambiado -= value;
        }

        public void PedirPermiso() => Gps.PedirPermiso();
        public void Iniciar() => Gps.Iniciar();
        public void Detener() => Gps.Detener();
        public void AbrirAjustes() => Gps.AbrirAjustes();
        public bool RutaActiva => Gps.RutaActiva;
        public bool RutaSoloPrimerPlano => Gps.RutaSoloPrimerPlano;
        public Resultado<bool> IniciarRuta(string textoNotificacion) => Gps.IniciarRuta(textoNotificacion);
        public void ActualizarNotificacion(string textoNotificacion) => Gps.ActualizarNotificacion(textoNotificacion);
        public void TerminarRuta() => Gps.TerminarRuta();

        /// <summary>Lo llama el plugin de Android (UnitySendMessage) con cada posición del servicio: se recoge la cola.</summary>
        public void AlPosicionRuta(string _) => Gps.RecogerRuta();

        void Update() => Gps.Actualizar();

        void OnDestroy()
        {
            gps?.TerminarRuta();
            gps?.Detener();
        }
    }
}
