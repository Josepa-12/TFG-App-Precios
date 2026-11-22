using System;
using PriceMap.Dominio;

namespace PriceMap.UI
{
    /// <summary>Qué hace la cámara durante la navegación (historia 2.7, parte 2b).</summary>
    public enum EstadoSeguimiento
    {
        /// <summary>Sin navegación: la cámara es del usuario, como siempre.</summary>
        Apagado,
        /// <summary>Recién pulsado Ir: se ve la ruta entera; a los 2 s pasa a Siguiendo.</summary>
        Esperando,
        /// <summary>La cámara sigue al usuario, inclinada y con el rumbo arriba.</summary>
        Siguiendo,
        /// <summary>El usuario ha movido el mapa: no se vuelve a centrar sola («Volver a centrar»).</summary>
        Suelto,
    }

    /// <summary>
    /// Estados de la cámara al navegar (historia 2.7, parte 2b), sin Unity: Empezar (Esperando, o Siguiendo al instante
    /// con «reducir animaciones») → a los CamaraEsperaSeguirSegundos, Siguiendo → un gesto del usuario, Suelto →
    /// «Volver a centrar», Siguiendo otra vez. Terminar lo apaga. Reloj inyectable.
    /// </summary>
    public sealed class SeguimientoCamara
    {
        public static readonly TimeSpan Espera = TimeSpan.FromSeconds(ParametrosNegocio.CamaraEsperaSeguirSegundos);

        readonly Func<DateTime> reloj;
        DateTime desde;

        /// <param name="reloj">Hora actual (por defecto, DateTime.UtcNow).</param>
        public SeguimientoCamara(Func<DateTime> reloj = null)
        {
            this.reloj = reloj ?? (() => DateTime.UtcNow);
        }

        public EstadoSeguimiento Estado { get; private set; } = EstadoSeguimiento.Apagado;

        /// <summary>La cámara tiene que seguir al usuario ahora.</summary>
        public bool Siguiendo => Estado == EstadoSeguimiento.Siguiendo;

        /// <summary>El usuario la ha soltado: el botón de ubicación es «Volver a centrar».</summary>
        public bool Suelto => Estado == EstadoSeguimiento.Suelto;

        /// <summary>Empieza la navegación: espera 2 s viendo la ruta entera o, sin espera, sigue ya.</summary>
        public void Empezar(bool sinEspera)
        {
            desde = reloj();
            Estado = sinEspera ? EstadoSeguimiento.Siguiendo : EstadoSeguimiento.Esperando;
        }

        /// <summary>Pasa de Esperando a Siguiendo al cumplirse los 2 s; true justo cuando pasa.</summary>
        public bool Avanzar()
        {
            if (Estado != EstadoSeguimiento.Esperando || reloj() - desde < Espera) return false;
            Estado = EstadoSeguimiento.Siguiendo;
            return true;
        }

        /// <summary>Un gesto del usuario (o los botones de zoom): deja de seguir. True si cambia algo.</summary>
        public bool Soltar()
        {
            if (Estado != EstadoSeguimiento.Esperando && Estado != EstadoSeguimiento.Siguiendo) return false;
            Estado = EstadoSeguimiento.Suelto;
            return true;
        }

        /// <summary>«Volver a centrar» (el botón de ubicación navegando): sigue otra vez. Sin navegación, nada.</summary>
        public void Recentrar()
        {
            if (Estado != EstadoSeguimiento.Apagado) Estado = EstadoSeguimiento.Siguiendo;
        }

        /// <summary>Termina la navegación: la cámara vuelve a ser del usuario.</summary>
        public void Terminar() => Estado = EstadoSeguimiento.Apagado;
    }
}
