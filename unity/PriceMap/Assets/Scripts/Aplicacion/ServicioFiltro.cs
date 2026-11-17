using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Estado del filtro "Precios" del mapa (FR-2): la UI lo observa y le envía la opción elegida,
    /// que se recuerda en el móvil.
    /// </summary>
    public class ServicioFiltro
    {
        public const string ClavePreferencia = "filtro-precios";

        readonly IPreferencias preferencias;

        public FiltroPrecios Actual { get; private set; } = FiltroPrecios.Ninguno;

        /// <summary>Se lanza al iniciar y en cada elección, con el filtro nuevo.</summary>
        public event Action<FiltroPrecios> Cambiado;

        public ServicioFiltro(IPreferencias preferencias)
        {
            this.preferencias = preferencias ?? throw new ArgumentNullException(nameof(preferencias));
        }

        /// <summary>Lee el filtro guardado (Ninguno si no hay o no se reconoce) y lo anuncia.</summary>
        public void Iniciar()
        {
            Actual = ReglaFiltro.DesdeClave(preferencias.Leer(ClavePreferencia));
            Cambiado?.Invoke(Actual);
        }

        /// <summary>Cambia el filtro, guarda la elección y la anuncia.</summary>
        public void Elegir(FiltroPrecios filtro)
        {
            Actual = filtro;
            preferencias.Guardar(ClavePreferencia, ReglaFiltro.AClave(filtro));
            Cambiado?.Invoke(Actual);
        }
    }
}
