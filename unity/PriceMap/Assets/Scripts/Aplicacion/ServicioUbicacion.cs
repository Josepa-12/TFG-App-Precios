using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Posición del usuario en el mapa (FR-5): la UI observa estado, posición y rumbo y le envía comandos
    /// (permitir, tocar el botón). Apaga el GPS y la brújula con la app en segundo plano (NFR-3).
    /// </summary>
    public class ServicioUbicacion
    {
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

        /// <summary>Última ubicación que recuerda el sistema (IUbicacion.UltimaConocida), para la vista inicial.</summary>
        public Coordenada? UltimaConocida => ubicacion.UltimaConocida;

        /// <summary>Rumbo respecto al norte real; null sin brújula o sin posición.</summary>
        public float? Rumbo => Posicion.HasValue ? ubicacion.Rumbo : null;

        /// <summary>Se lanza cuando cambia el estado, la posición o el rumbo.</summary>
        public event Action Cambiado;

        /// <summary>Lecturas del GPS que han llegado, también las que repiten la coordenada (IUbicacion.Lecturas).</summary>
        public int Lecturas => ubicacion.Lecturas;

        /// <summary>True con la app en segundo plano (ubicación y brújula apagadas).</summary>
        public bool Pausada { get; private set; }

        /// <summary>Android aún puede preguntar por el permiso (IUbicacion.PuedePedirPermiso).</summary>
        public bool PuedePedirPermiso => ubicacion.PuedePedirPermiso;

        /// <summary>Al abrir la app: enciende ubicación y brújula si ya hay permiso.</summary>
        public void Iniciar()
        {
            iniciada = true;
            if (!Pausada) ubicacion.Iniciar();
        }

        /// <summary>"Permitir" en la explicación: lanza el permiso de Android.</summary>
        public void Permitir() => ubicacion.PedirPermiso();

        /// <summary>
        /// La app pasa a segundo plano (OnApplicationPause(true) u OnApplicationFocus(false)). Sin ruta se apagan
        /// ubicación y brújula; con una ruta en marcha el adaptador mantiene el GPS (AD-19).
        /// </summary>
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

        /// <summary>Hay una ruta en marcha: el GPS sigue con la app en segundo plano (AD-19).</summary>
        public bool RutaActiva => ubicacion.RutaActiva;

        /// <summary>Ruta en marcha que se pausa al apagar la pantalla (permiso de notificaciones denegado).</summary>
        public bool RutaSoloPrimerPlano => ubicacion.RutaSoloPrimerPlano;

        /// <summary>Empieza una ruta con GPS en segundo plano y notificación fija (IUbicacion.IniciarRuta).</summary>
        public Resultado<bool> IniciarRuta(string textoNotificacion) => ubicacion.IniciarRuta(textoNotificacion);

        /// <summary>Cambia el texto de la notificación de la ruta.</summary>
        public void ActualizarNotificacion(string textoNotificacion) => ubicacion.ActualizarNotificacion(textoNotificacion);

        /// <summary>Termina la ruta: fuera la notificación y el GPS en segundo plano.</summary>
        public void TerminarRuta() => ubicacion.TerminarRuta();

        /// <summary>Distancia (dp) entre el usuario y el centro de la pantalla por debajo de la cual está "centrado".</summary>
        public const float UmbralCentradoDp = 8f;

        /// <summary>Giro (grados) por debajo del cual el mapa ya mira al norte (360° cuenta como 0°).</summary>
        public const float UmbralGiroGrados = 0.5f;

        /// <summary>
        /// Lo que hace el botón de ubicación en el estado actual. Sin permiso: la explicación; denegado o apagada:
        /// el aviso; buscando: nada. Con posición: si el usuario no está centrado, centrar; si lo está y el mapa
        /// está girado, orientar al norte; centrado y al norte, volver a centrar (no se ve nada: sin modo brújula).
        /// </summary>
        /// <param name="distanciaCentroDp">Distancia en pantalla (dp) entre el usuario y el centro de la vista; infinito si no se sabe.</param>
        /// <param name="giro">Giro del mapa en grados (IMapa.Giro).</param>
        public AccionUbicacion AlTocarBoton(float distanciaCentroDp = float.PositiveInfinity, float giro = 0f)
        {
            switch (Estado)
            {
                case EstadoUbicacion.SinPermiso: return AccionUbicacion.Explicar;
                case EstadoUbicacion.Denegado:
                case EstadoUbicacion.Apagada: return AccionUbicacion.Avisar;
            }
            if (!Posicion.HasValue) return AccionUbicacion.Nada;
            bool centrado = distanciaCentroDp < UmbralCentradoDp;
            return centrado && !AlNorte(giro) ? AccionUbicacion.OrientarNorte : AccionUbicacion.Centrar;
        }

        /// <summary>True si el giro (en grados, cualquier vuelta) es 0 con la tolerancia UmbralGiroGrados.</summary>
        public static bool AlNorte(float giro)
        {
            double resto = ((giro % 360.0) + 360.0) % 360.0; // [0, 360)
            return Math.Min(resto, 360.0 - resto) < UmbralGiroGrados;
        }

        /// <summary>"Abrir Ajustes" en el aviso: los de la app (denegado) o los de ubicación (apagada).</summary>
        public void AbrirAjustes() => ubicacion.AbrirAjustes();
    }
}
