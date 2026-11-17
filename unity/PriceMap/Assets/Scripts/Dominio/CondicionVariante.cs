using System;
using System.Collections.Generic;
using System.Linq;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Condición de una variante: franja horaria (puede cruzar la medianoche: 22:00-06:00),
    /// días de la semana o tipo de cliente (socio, estudiante, con bono). Las de tipo de cliente
    /// no dependen del momento, así que nunca están "vigentes".
    /// </summary>
    public sealed class CondicionVariante
    {
        public TipoCondicion Tipo { get; }
        public TimeSpan Desde { get; }
        public TimeSpan Hasta { get; }
        public IReadOnlyCollection<DayOfWeek> Dias { get; }
        /// <summary>Tipo de cliente (p. ej. "con-bono"); vacío en horario y día.</summary>
        public string TipoCliente { get; }

        CondicionVariante(TipoCondicion tipo, TimeSpan desde, TimeSpan hasta, IReadOnlyCollection<DayOfWeek> dias, string tipoCliente)
        {
            Tipo = tipo;
            Desde = desde;
            Hasta = hasta;
            Dias = dias ?? Array.Empty<DayOfWeek>();
            TipoCliente = tipoCliente ?? "";
        }

        /// <summary>
        /// Franja [desde, hasta) dentro del día (0:00 a 24:00). Si hasta &lt; desde, cruza la medianoche
        /// (22:00-06:00; 20:00-00:00 = hasta medianoche). Si son iguales, todo el día.
        /// </summary>
        public static CondicionVariante Horario(TimeSpan desde, TimeSpan hasta)
        {
            if (desde < TimeSpan.Zero || desde >= TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(desde));
            if (hasta < TimeSpan.Zero || hasta >= TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(hasta));
            return new CondicionVariante(TipoCondicion.Horario, desde, hasta, null, null);
        }

        public static CondicionVariante EnDias(params DayOfWeek[] dias)
        {
            if (dias == null || dias.Length == 0) throw new ArgumentException("Una variante por días necesita al menos un día.", nameof(dias));
            return new CondicionVariante(TipoCondicion.Dia, TimeSpan.Zero, TimeSpan.Zero, dias.Distinct().ToArray(), null);
        }

        public static CondicionVariante DeCliente(string tipoCliente)
        {
            if (string.IsNullOrWhiteSpace(tipoCliente)) throw new ArgumentException("Falta el tipo de cliente.", nameof(tipoCliente));
            return new CondicionVariante(TipoCondicion.TipoCliente, TimeSpan.Zero, TimeSpan.Zero, null, tipoCliente);
        }

        /// <summary>¿Se cumple en este instante (hora local del lugar)?</summary>
        public bool SeCumple(DateTime ahoraLocal)
        {
            switch (Tipo)
            {
                case TipoCondicion.Horario:
                    var h = ahoraLocal.TimeOfDay;
                    if (Desde < Hasta) return h >= Desde && h < Hasta;
                    if (Desde == Hasta) return true;                // todo el día
                    return h >= Desde || h < Hasta;                 // cruza la medianoche
                case TipoCondicion.Dia:
                    return Dias.Contains(ahoraLocal.DayOfWeek);
                default:
                    return false;                                   // tipo de cliente: no depende del momento
            }
        }
    }
}
