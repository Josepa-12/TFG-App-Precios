using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Voz
{
    /// <summary>Voz que no suena (historia 2.8): la del PC, el editor y la captura de pruebas.</summary>
    public sealed class VozNula : IVoz
    {
        public void Hablar(string texto) { }
        public void Parar() { }
    }
}
