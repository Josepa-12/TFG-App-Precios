using System;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Precio publicado en la web oficial del negocio, con la página y la fecha en que se consultó.
    /// Sin valor = la web nombra el servicio pero no da el importe ("A consultar").
    /// </summary>
    public sealed class PrecioWeb
    {
        public ValorPrecio? Valor { get; }
        public string Url { get; }
        public DateTime FechaConsulta { get; }

        public PrecioWeb(ValorPrecio? valor, string url, DateTime fechaConsulta)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("El precio de la web necesita su página (NFR-6).", nameof(url));
            Valor = valor;
            Url = url;
            FechaConsulta = fechaConsulta;
        }

        public bool EsConsultar => Valor == null;
    }
}
