using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Posición del usuario en el mapa (FR-5): la UI observa estado, posición y rumbo y le envía comandos
    /// (permitir, tocar el botón). Recuerda si la explicación del permiso ya salió y apaga el GPS y la
    /// brújula con la app en segundo plano (NFR-3).
    /// </summary>
    public class ServicioUbicacion
    {
        /// <summary>Preferencia que guarda que la explicación del permiso ya salió una vez.</summary>
        public const string ClaveExplicacion = "ubicacion-explicacion-mostrada";

        readonly IUbicacion ubicacion;
        readonly IPreferencias preferencias;
        bool iniciada;

        public ServicioUbicacion(IUbicacion ubicacion, IPreferencias preferencias)
        {
            this.ubicacion = ubicacion ?? throw new ArgumentNullException(nameof(ubicacion));
            this.preferencias = preferencias ?? throw new ArgumentNullException(nameof(preferencias));
            this.ubicacion.Cambiado += () => Cambiado?.Invoke();
        }

        public EstadoUbicacion Estado => ubicacion.Estado;

        /// <summary>Posición del usuario; solo en Activa (si no, null).</summary>
        public Coordenada? Posicion => ubicacion.Estado == EstadoUbicacion.Activa ? ubicacion.Posicion : null;

        /// <summary>Rumbo respecto al norte real; null sin brújula o sin posición.</summary>
        public float? Rumbo => Posicion.HasValue ? ubicacion.Rumbo : null;

        /// <summary>Se lanza cuando cambia el estado, la posición o el rumbo.</summary>
        public event Action Cambiado;

        /// <summary>True con la app en segundo plano (ubicación y brújula apagadas).</summary>
        public bool Pausada { get; private set; }

        /// <summary>Si la explicación tiene que salir al abrir la app: sin permiso y sin haber salido antes.</summary>
        public bool DebeExplicarAlAbrir => Estado == EstadoUbicacion.SinPermiso && preferencias.Leer(ClaveExplicacion) == null;

        /// <summary>Guarda que la explicación ya salió: no vuelve a salir al abrir la app.</summary>
        public void MarcarExplicacionMostrada() => preferencias.Guardar(ClaveExplicacion, "1");

        /// <summary>Al abrir la app: enciende ubicación y brújula si ya hay permiso.</summary>
        public void Iniciar()
        {
            iniciada = true;
            if (!Pausada) ubicacion.Iniciar();
        }

        /// <summary>"Permitir" en la explicación: lanza el permiso de Android.</summary>
        public void Permitir() => ubicacion.PedirPermiso();

        /// <summary>La app pasa a segundo plano (OnApplicationPause(true) u OnApplicationFocus(false)).</summary>
        public void Pausar()
        {
            if (Pausada) return;
            Pausada = true;
            ubicacion.Detener();
        }

        /// <summary>La app vuelve a primer plano: se reanudan ubicación y brújula.</summary>
        public void Reanudar()
        {
            if (!Pausada) return;
            Pausada = false;
            if (iniciada) ubicacion.Iniciar();
        }

        /// <summary>Lo que hace el botón de ubicación en el estado actual.</summary>
        public AccionUbicacion AlTocarBoton()
        {
            if (Estado == EstadoUbicacion.SinPermiso) return AccionUbicacion.Explicar;
            return Posicion.HasValue ? AccionUbicacion.Centrar : AccionUbicacion.Nada;
        }
    }
}
