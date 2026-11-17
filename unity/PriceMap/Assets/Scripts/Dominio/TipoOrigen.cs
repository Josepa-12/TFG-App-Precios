namespace PriceMap.Dominio
{
    /// <summary>De dónde sale el precio mostrado (NFR-6: ningún precio sin origen).</summary>
    public enum TipoOrigen
    {
        /// <summary>El concepto todavía no tiene precio (y valor por defecto: nada sin rellenar parece "de la web").</summary>
        SinPrecio = 0,
        /// <summary>Publicado en la web oficial del negocio.</summary>
        Web,
        /// <summary>Declarado por el negocio desde su cuenta.</summary>
        Declarado,
        /// <summary>Según lo que pagaron N clientes (historia 4.10).</summary>
        Clientes,
    }
}
