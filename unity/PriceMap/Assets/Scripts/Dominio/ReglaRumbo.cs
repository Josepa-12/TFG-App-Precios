using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Rumbo de la marcha (historia 2.7, parte 2b; lo reutilizará la flecha de la 2.7c). Unity no da ni velocidad ni rumbo
    /// del GPS, así que se sacan de las posiciones: se guarda la última posición de referencia y, cuando la nueva está a
    /// RumboDistanciaMinimaMetros o más, la velocidad es esa distancia entre el tiempo que ha pasado (reloj inyectable)
    /// y, si supera RumboVelocidadMinima, el rumbo es el de la referencia a la nueva. Parado (≤ 1 m/s) se queda el último.
    /// Con menos distancia, la referencia no cambia: los pasos cortos se suman hasta los 3 m. No es seguro entre hilos.
    /// </summary>
    public sealed class ReglaRumbo
    {
        readonly Func<DateTime> reloj;
        Coordenada? referencia;
        DateTime horaReferencia;

        /// <param name="reloj">Hora actual (por defecto, DateTime.UtcNow).</param>
        public ReglaRumbo(Func<DateTime> reloj = null)
        {
            this.reloj = reloj ?? (() => DateTime.UtcNow);
        }

        /// <summary>Último rumbo de la marcha en grados (0 = norte, 90 = este, sentido horario); null si aún no hay.</summary>
        public double? Rumbo { get; private set; }

        /// <summary>Última velocidad medida (m/s); null si aún no hay.</summary>
        public double? Velocidad { get; private set; }

        /// <summary>Olvida las posiciones, el rumbo y la velocidad (al empezar una ruta).</summary>
        public void Reiniciar()
        {
            referencia = null;
            Rumbo = null;
            Velocidad = null;
        }

        /// <summary>Posición nueva del GPS.</summary>
        public void Nueva(Coordenada posicion)
        {
            var ahora = reloj();
            if (!referencia.HasValue)
            {
                referencia = posicion;
                horaReferencia = ahora;
                return;
            }
            double metros = referencia.Value.DistanciaMetros(posicion);
            if (metros < ParametrosNegocio.RumboDistanciaMinimaMetros) return;
            double segundos = (ahora - horaReferencia).TotalSeconds;
            // Sin tiempo entre las dos (misma hora o reloj hacia atrás), la velocidad no se puede medir: se queda la
            // anterior y la posición cuenta como en marcha (se toma el rumbo)
            if (segundos > 0) Velocidad = metros / segundos;
            if (segundos <= 0 || Velocidad.Value > ParametrosNegocio.RumboVelocidadMinima) Rumbo = RumboEntre(referencia.Value, posicion);
            referencia = posicion;
            horaReferencia = ahora;
        }

        /// <summary>Rumbo inicial (grados en [0, 360), 0 = norte, sentido horario) de a hacia b por la esfera.</summary>
        public static double RumboEntre(Coordenada a, Coordenada b)
        {
            double f1 = a.Latitud * Math.PI / 180.0, f2 = b.Latitud * Math.PI / 180.0;
            double dl = (b.Longitud - a.Longitud) * Math.PI / 180.0;
            double y = Math.Sin(dl) * Math.Cos(f2);
            double x = Math.Cos(f1) * Math.Sin(f2) - Math.Sin(f1) * Math.Cos(f2) * Math.Cos(dl);
            double grados = Math.Atan2(y, x) * 180.0 / Math.PI;
            return (grados % 360.0 + 360.0) % 360.0;
        }

        /// <summary>
        /// Rumbo de la ruta en el tramo dado (o el primero que siga con longitud, si ese no la tiene); null si la línea no
        /// tiene ningún tramo con longitud desde ahí.
        /// </summary>
        public static double? RumboRuta(IReadOnlyList<Coordenada> puntos, int tramo)
        {
            if (puntos == null) return null;
            for (int i = Math.Max(0, tramo); i + 1 < puntos.Count; i++)
                if (puntos[i].DistanciaMetros(puntos[i + 1]) > 0.01) return RumboEntre(puntos[i], puntos[i + 1]);
            return null;
        }

        /// <summary>
        /// Rumbo que va arriba al navegar: a pie, la brújula; en coche (o a pie sin brújula), el de la marcha; sin él
        /// todavía, el de la ruta. Null si no hay ninguno.
        /// </summary>
        public static double? Elegir(ModoViaje modo, double? brujula, double? marcha, double? ruta)
        {
            if (modo == ModoViaje.APie && brujula.HasValue) return brujula;
            return marcha ?? ruta;
        }
    }
}
