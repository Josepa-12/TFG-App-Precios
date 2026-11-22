using System;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// La pantalla no se apaga mientras se navega (historia 2.7, parte 2b; FR20): al empezar la ruta (RutaIniciada) la
    /// mantiene encendida y al terminarla (RutaTerminada, por cualquier motivo) la devuelve al valor del sistema.
    /// </summary>
    public sealed class PantallaEncendida
    {
        readonly IPantallaSistema pantalla;

        public PantallaEncendida(ServicioNavegacion navegacion, IPantallaSistema pantalla)
        {
            if (navegacion == null) throw new ArgumentNullException(nameof(navegacion));
            this.pantalla = pantalla ?? throw new ArgumentNullException(nameof(pantalla));
            navegacion.RutaIniciada += _ => this.pantalla.MantenerEncendida(true);
            navegacion.RutaTerminada += _ => this.pantalla.MantenerEncendida(false);
        }
    }
}
