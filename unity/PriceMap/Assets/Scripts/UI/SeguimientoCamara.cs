using System;
using PriceMap.Dominio;

namespace PriceMap.UI
{
    /// <summary>Qué hace la cámara durante la navegación (historia 2.7, parte 2b).</summary>
    public enum EstadoSeguimiento
    {
        /// <summary>Sin navegación: la cámara es del usuario, como siempre.</summary>
        Apagado,
        /// <summary>La cámara sigue al usuario, inclinada y con el rumbo arriba.</summary>
        Siguiendo,
        /// <summary>El usuario ha movido el mapa: no se vuelve a centrar sola («Volver a centrar»).</summary>
        Suelto,
    }

    /// <summary>
    /// Estados de la cámara al navegar (historia 2.7, parte 2b; «Iniciar», parte 5b de «Cómo llegar»), sin Unity: Empezar
    /// (Siguiendo, y Acercando mientras la cámara llega al usuario; con «reducir animaciones» llega al momento) → un gesto
    /// del usuario, Suelto (sin dejar de contar lo que falta para llegar) → «Volver a centrar», Siguiendo otra vez.
    /// Terminar lo apaga. Reloj inyectable.
    /// </summary>
    public sealed class SeguimientoCamara
    {
        public static readonly TimeSpan Acercamiento = TimeSpan.FromSeconds(ParametrosNegocio.CamaraAcercarSegundos);

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

        /// <summary>«Iniciar» pulsado y la cámara aún de camino al usuario (aunque la haya soltado): la navegación no ha empezado.</summary>
        public bool Acercando { get; private set; }

        /// <summary>«Iniciar»: la cámara va al usuario; al momento (sin animaciones), ya ha llegado.</summary>
        public void Empezar(bool alMomento)
        {
            desde = reloj();
            Estado = EstadoSeguimiento.Siguiendo;
            Acercando = !alMomento;
        }

        /// <summary>True, una sola vez, al cumplirse el tiempo en que la cámara llega (o si el reloj salta atrás).</summary>
        public bool Llegar()
        {
            if (!Acercando) return false;
            var pasado = reloj() - desde;
            if (pasado >= TimeSpan.Zero && pasado < Acercamiento) return false;
            Acercando = false;
            return true;
        }

        /// <summary>Un gesto del usuario (o los botones de zoom): deja de seguir. True si cambia algo.</summary>
        public bool Soltar()
        {
            if (Estado != EstadoSeguimiento.Siguiendo) return false;
            Estado = EstadoSeguimiento.Suelto;
            return true;
        }

        /// <summary>«Volver a centrar» (el botón de ubicación navegando): sigue otra vez. Sin navegación, nada.</summary>
        public void Recentrar()
        {
            if (Estado != EstadoSeguimiento.Apagado) Estado = EstadoSeguimiento.Siguiendo;
        }

        /// <summary>Termina la navegación: la cámara vuelve a ser del usuario.</summary>
        public void Terminar()
        {
            Estado = EstadoSeguimiento.Apagado;
            Acercando = false;
        }
    }
}
