using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Estado del tema de la app (AD-15): el mapa y la interfaz lo observan y cambian a la vez.
    /// </summary>
    public class ServicioTema
    {
        public const string ClavePreferencia = "tema";

        readonly IPreferencias preferencias;
        readonly ITemaSistema sistema;

        public Tema Actual { get; private set; }

        /// <summary>Se lanza al iniciar y en cada cambio, con el tema nuevo.</summary>
        public event Action<Tema> Cambiado;

        public ServicioTema(IPreferencias preferencias, ITemaSistema sistema)
        {
            this.preferencias = preferencias ?? throw new ArgumentNullException(nameof(preferencias));
            this.sistema = sistema ?? throw new ArgumentNullException(nameof(sistema));
        }

        /// <summary>Calcula el tema inicial (guardado o del sistema) y lo anuncia.</summary>
        public void Iniciar()
        {
            Actual = ReglaTema.Inicial(preferencias.Leer(ClavePreferencia), sistema.EsOscuro);
            Cambiado?.Invoke(Actual);
        }

        /// <summary>Pasa al otro tema, guarda la elección y lo anuncia.</summary>
        public void Alternar()
        {
            Actual = ReglaTema.Contrario(Actual);
            preferencias.Guardar(ClavePreferencia, ReglaTema.AClave(Actual));
            Cambiado?.Invoke(Actual);
        }
    }
}
