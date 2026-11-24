using System;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>Sin servidor (PC, editor y captura de pruebas): nunca toca Firebase.</summary>
    public sealed class ServidorNulo : IServidor
    {
        public EstadoServidor Estado => EstadoServidor.NoDisponible;
        public event Action Cambiado { add { } remove { } }
    }
}
