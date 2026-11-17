using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Todo lo que se sabe del precio de un concepto o de una variante. Los aportes de clientes
    /// (historia 4.10) y los avisos (historia 7.2) se añadirán aquí.
    /// </summary>
    public sealed class FuentesPrecio
    {
        static readonly IReadOnlyList<Declaracion> Ninguna = Array.Empty<Declaracion>();

        public Plantilla Plantilla { get; }
        public PrecioWeb Web { get; }
        public IReadOnlyList<Declaracion> Declaraciones { get; }

        public FuentesPrecio(Plantilla plantilla, PrecioWeb web = null, IReadOnlyList<Declaracion> declaraciones = null)
        {
            Plantilla = plantilla;
            Web = web;
            Declaraciones = declaraciones ?? Ninguna;
        }
    }
}
