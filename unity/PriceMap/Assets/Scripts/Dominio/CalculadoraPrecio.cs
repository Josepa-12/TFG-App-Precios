using System;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Calcula lo que se muestra de un precio (AD-3): siempre igual para todos y nunca guardado.
    /// "Ahora" llega como parámetro (AD-12).
    /// </summary>
    public static class CalculadoraPrecio
    {
        /// <summary>
        /// Las fechas del servidor llegan en UTC y "ahora" puede venir en hora local: se comparan en UTC.
        /// Las fechas sin zona (Unspecified) se toman tal cual, como en las pruebas.
        /// </summary>
        static DateTime Comparable(DateTime t) => t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : t;

        /// <summary>
        /// Manda la declaración válida más reciente del negocio; si no hay, la web oficial;
        /// si tampoco, "sin precio". (El origen "según N clientes" llega en la historia 4.10.)
        /// </summary>
        public static PrecioMostrado Calcular(FuentesPrecio fuentes, DateTime ahora)
        {
            if (fuentes == null) throw new ArgumentNullException(nameof(fuentes));

            Declaracion ultima = null;
            // Con la misma fecha gana la que va después en la lista (la última que llegó)
            foreach (var d in fuentes.Declaraciones)
                if (d != null && d.Valor.EsValido && (ultima == null || Comparable(d.Creado) >= Comparable(ultima.Creado))) ultima = d;
            if (ultima != null)
                return new PrecioMostrado(ultima.Valor, false, Origen.Declarado(),
                    new Frescura(Estado(fuentes.Plantilla, ultima.Creado, ahora), ultima.Creado));

            var web = fuentes.Web;
            if (web != null)
            {
                // Sin importe (o con un importe imposible, ≤ 0): el servicio existe, pero hay que consultarlo
                if (web.EsConsultar || !web.Valor.Value.EsValido)
                    return new PrecioMostrado(null, true, Origen.DeWeb(web.Url, web.FechaConsulta), null);
                return new PrecioMostrado(web.Valor, false, Origen.DeWeb(web.Url, web.FechaConsulta),
                    new Frescura(Estado(fuentes.Plantilla, web.FechaConsulta, ahora), web.FechaConsulta));
            }

            return new PrecioMostrado(null, false, Origen.SinPrecio(), null);
        }

        /// <summary>
        /// Frescura según la plantilla (FR-33). Hostelería: &lt; 30 días al día, &gt; 60 antiguo.
        /// Servicio local y ocio: &lt; 3 meses al día, &gt; 6 antiguo (meses de calendario).
        /// Una fecha posterior a "ahora" (reloj desfasado) cuenta como al día.
        /// </summary>
        public static EstadoFrescura Estado(Plantilla plantilla, DateTime ultimaConfirmacion, DateTime ahora)
        {
            ultimaConfirmacion = Comparable(ultimaConfirmacion);
            ahora = Comparable(ahora);
            if (ultimaConfirmacion >= ahora) return EstadoFrescura.AlDia; // futura (reloj desfasado); evita desbordar fechas extremas
            DateTime limiteAlDia, limiteAntiguo;
            if (plantilla == Plantilla.Hosteleria)
            {
                limiteAlDia = ultimaConfirmacion.AddDays(ParametrosNegocio.FrescuraHosteleriaDiasAlDia);
                limiteAntiguo = ultimaConfirmacion.AddDays(ParametrosNegocio.FrescuraHosteleriaDiasAntiguo);
            }
            else
            {
                limiteAlDia = ultimaConfirmacion.AddMonths(ParametrosNegocio.FrescuraOtrosMesesAlDia);
                limiteAntiguo = ultimaConfirmacion.AddMonths(ParametrosNegocio.FrescuraOtrosMesesAntiguo);
            }
            if (ahora < limiteAlDia) return EstadoFrescura.AlDia;
            // Nota: los meses son de calendario (AddMonths): del 31 de agosto, 6 meses llegan al 28 de febrero.
            if (ahora <= limiteAntiguo) return EstadoFrescura.PodriaHaberCambiado;
            return EstadoFrescura.Antiguo;
        }
    }
}
