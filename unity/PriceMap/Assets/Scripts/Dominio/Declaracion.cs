using System;

namespace PriceMap.Dominio
{
    /// <summary>Precio declarado por el negocio desde su cuenta, con la fecha (del servidor) en que lo hizo.</summary>
    public sealed class Declaracion
    {
        public ValorPrecio Valor { get; }
        public DateTime Creado { get; }

        public Declaracion(ValorPrecio valor, DateTime creado)
        {
            Valor = valor;
            Creado = creado;
        }
    }
}
