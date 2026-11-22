namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de la pantalla del móvil (historia 2.7, parte 2b; FR20): que no se apague sola mientras se navega.
    /// </summary>
    public interface IPantallaSistema
    {
        /// <summary>True: la pantalla no se apaga sola; false: vuelve a lo que diga el sistema.</summary>
        void MantenerEncendida(bool encendida);
    }
}
