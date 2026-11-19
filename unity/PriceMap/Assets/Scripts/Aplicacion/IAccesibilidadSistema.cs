namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de los ajustes de accesibilidad del sistema operativo que la app no recibe por eventos (historia 2.2,
    /// parte 2; UX-DR19). Se lee al arrancar y cada vez que la app vuelve a primer plano.
    /// </summary>
    public interface IAccesibilidadSistema
    {
        /// <summary>True si el usuario ha quitado las animaciones del sistema (escala de animaciones 0).</summary>
        bool AnimacionesQuitadas { get; }
    }
}
