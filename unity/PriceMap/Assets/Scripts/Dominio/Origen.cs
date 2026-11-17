using System;

namespace PriceMap.Dominio
{
    /// <summary>De dónde sale un precio mostrado: tipo, página y fecha de la web o número de clientes.</summary>
    public readonly struct Origen
    {
        public TipoOrigen Tipo { get; }
        /// <summary>Página de la web del negocio (solo si Tipo = Web).</summary>
        public string Url { get; }
        /// <summary>Fecha de consulta de la web (solo si Tipo = Web).</summary>
        public DateTime? FechaWeb { get; }
        /// <summary>Número de clientes (solo si Tipo = Clientes).</summary>
        public int NClientes { get; }

        Origen(TipoOrigen tipo, string url, DateTime? fechaWeb, int nClientes)
        {
            Tipo = tipo;
            Url = url;
            FechaWeb = fechaWeb;
            NClientes = nClientes;
        }

        public static Origen DeWeb(string url, DateTime fecha) => new Origen(TipoOrigen.Web, url, fecha, 0);
        public static Origen Declarado() => new Origen(TipoOrigen.Declarado, null, null, 0);
        public static Origen DeClientes(int n) => new Origen(TipoOrigen.Clientes, null, null, n);
        public static Origen SinPrecio() => new Origen(TipoOrigen.SinPrecio, null, null, 0);
    }
}
