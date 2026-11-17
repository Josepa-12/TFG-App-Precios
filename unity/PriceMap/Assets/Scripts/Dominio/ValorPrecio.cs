using System;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Importe de un precio en céntimos (AD-10): fijo o "desde" con un importe; rango con mínimo y máximo
    /// (máximo nulo = rango abierto, "Más de 50 €"). Siempre mayor que 0 (NFR-6).
    /// </summary>
    public readonly struct ValorPrecio : IEquatable<ValorPrecio>
    {
        public FormaPrecio Forma { get; }
        /// <summary>Importe de un precio fijo o "desde"; en un rango, su mínimo.</summary>
        public int ImporteCent { get; }
        /// <summary>Máximo de un rango (null = abierto). Nulo en fijo y "desde".</summary>
        public int? MaxCent { get; }

        ValorPrecio(FormaPrecio forma, int importeCent, int? maxCent)
        {
            Forma = forma;
            ImporteCent = importeCent;
            MaxCent = maxCent;
        }

        public static ValorPrecio Fijo(int cent) => new ValorPrecio(FormaPrecio.Fijo, cent, null);
        public static ValorPrecio Desde(int cent) => new ValorPrecio(FormaPrecio.Desde, cent, null);
        public static ValorPrecio Rango(int minCent, int? maxCent) => new ValorPrecio(FormaPrecio.Rango, minCent, maxCent);

        public int MinCent => ImporteCent;

        /// <summary>Importes positivos y, en un rango cerrado, mínimo ≤ máximo.</summary>
        public bool EsValido => ImporteCent > 0 && (Forma != FormaPrecio.Rango || MaxCent == null || MaxCent >= ImporteCent);

        public bool Equals(ValorPrecio o) => Forma == o.Forma && ImporteCent == o.ImporteCent && MaxCent == o.MaxCent;
        public override bool Equals(object obj) => obj is ValorPrecio o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(Forma, ImporteCent, MaxCent);
        public override string ToString() => Forma == FormaPrecio.Rango ? $"{Forma} {ImporteCent}-{MaxCent?.ToString() ?? "…"}" : $"{Forma} {ImporteCent}";
    }
}
