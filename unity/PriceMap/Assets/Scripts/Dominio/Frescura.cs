using System;

namespace PriceMap.Dominio
{
    /// <summary>Estado de frescura de un precio y la fecha de su última confirmación.</summary>
    public readonly struct Frescura
    {
        public EstadoFrescura Estado { get; }
        public DateTime UltimaConfirmacion { get; }

        public Frescura(EstadoFrescura estado, DateTime ultimaConfirmacion)
        {
            Estado = estado;
            UltimaConfirmacion = ultimaConfirmacion;
        }
    }
}
