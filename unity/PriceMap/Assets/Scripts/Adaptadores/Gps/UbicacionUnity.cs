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
    /// fotograma. Iniciar y Detener los llama ServicioUbicacion al abrir, pausar y reanudar la app (NFR-3).
    /// </summary>
    public class UbicacionUnity : MonoBehaviour, IUbicacion
    {
        UbicacionGps gps;

        UbicacionGps Gps => gps ??= new UbicacionGps(new FuenteUbicacionUnity());

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

        void Update() => Gps.Actualizar();

        void OnDestroy() => gps?.Detener();
    }
}
