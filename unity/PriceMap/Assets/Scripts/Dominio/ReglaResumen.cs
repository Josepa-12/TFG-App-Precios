using System;

namespace PriceMap.Dominio
{
    /// <summary>Resumen del precio de un lugar para el mapa (historia 1.4).</summary>
    public static class ReglaResumen
    {
        /// <summary>
        /// El precio que se anuncia es el importe más bajo de los conceptos que tienen precio, con la
        /// forma "desde" (Clínica Areas → desde 20 €). Los "A consultar" no cuentan. Un lugar público
        /// nunca tiene precio. "Ahora" llega como parámetro (AD-12).
        /// </summary>
        public static ResumenLugar Resumir(Lugar lugar, DateTime ahora)
        {
            if (lugar == null) throw new ArgumentNullException(nameof(lugar));
            if (lugar.Publico) return ResumenLugar.SinPrecio;

            int? minimo = null;
            foreach (var c in lugar.Conceptos)
            {
                if (c == null) continue;
                var p = CalculadoraPrecio.Calcular(c.Fuentes, ahora);
                if (!p.TienePrecio) continue;
                var importe = p.Valor.Value.MinCent;
                if (minimo == null || importe < minimo) minimo = importe;
            }
            return minimo == null ? ResumenLugar.SinPrecio : new ResumenLugar(ValorPrecio.Desde(minimo.Value));
        }
    }
}
