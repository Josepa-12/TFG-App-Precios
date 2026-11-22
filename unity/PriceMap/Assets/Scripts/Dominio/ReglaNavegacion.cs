using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Seguir una ruta (historia 2.7, parte 1; FR19), lógica pura. Se crea con la ruta (calcula una vez los metros
    /// acumulados de la línea y dónde cae cada maniobra) y Seguir proyecta cada posición del GPS al tramo más cercano,
    /// con una proyección equirrectangular local en double (precisa de sobra a la escala de una calle).
    ///
    /// - Restante: la parte de la línea desde el punto proyectado; los metros que se enseñan son los de la ruta en esa
    ///   proporción (así al empezar coinciden con los de la ficha) y el tiempo, duración × (restante / total).
    /// - Maniobra siguiente: la primera con texto cuya posición en la ruta (la de su punto, proyectado a su distancia en
    ///   la línea al crear la regla) no ha quedado por detrás del punto proyectado. Las maniobras sin texto (OSRM) no
    ///   cuentan: esa ruta no tiene maniobra siguiente.
    /// - Ruido: el avance nunca baja. Se busca desde NavegacionMargenAtrasMetros por detrás del último avance y, si la
    ///   posición cae más atrás, se queda en el último punto (lo de detrás del margen no cuenta ni para la distancia). Un tramo más de NavegacionSaltoMetros por delante (p. ej. la
    ///   vuelta por la misma calle) solo gana si está NavegacionEmpateMetros más cerca que el mejor de los cercanos.
    /// </summary>
    public sealed class ReglaNavegacion
    {
        const double GradosARadianes = Math.PI / 180.0;
        const double MetrosPorGrado = Coordenada.RadioTierraMetros * GradosARadianes;
        /// <summary>Peso (m por m) de lo lejos que cae una maniobra de donde la esperan sus metros, al proyectarla.</summary>
        const double PesoEsperada = 0.05;

        readonly double[] acumuladas;
        readonly int[] conTexto;          // índices de Ruta.Maniobras con texto, en orden
        readonly double[] posicionesTexto; // su posición en la línea (m)

        public Ruta Ruta { get; }

        /// <summary>Metros de la línea (suma de sus tramos).</summary>
        public double Total { get; }

        public ReglaNavegacion(Ruta ruta)
        {
            Ruta = ruta ?? throw new ArgumentNullException(nameof(ruta));
            var p = ruta.Puntos;
            acumuladas = new double[Math.Max(1, p.Count)];
            for (int i = 1; i < p.Count; i++) acumuladas[i] = acumuladas[i - 1] + p[i - 1].DistanciaMetros(p[i]);
            Total = acumuladas[acumuladas.Length - 1];

            // Posición de cada maniobra en la línea: hacia delante desde la anterior, cerca de donde la esperan sus metros
            var indices = new List<int>();
            var posiciones = new List<double>();
            double sumaMetros = 0;
            foreach (var m in ruta.Maniobras) sumaMetros += m?.Metros ?? 0;
            double escala = sumaMetros > 0 ? Total / sumaMetros : 0;
            double esperada = 0, ultima = 0;
            int tramo = 0;
            for (int i = 0; i < ruta.Maniobras.Count; i++)
            {
                var m = ruta.Maniobras[i];
                if (m == null) continue;
                if (!string.IsNullOrWhiteSpace(m.Texto) && p.Count >= 2)
                {
                    var c = MejorPara(m.Posicion, tramo, escala > 0 ? esperada * escala : (double?)null);
                    double posicion = Math.Max(ultima, c.avance);
                    tramo = Math.Max(tramo, c.tramo);
                    ultima = posicion;
                    indices.Add(i);
                    posiciones.Add(posicion);
                }
                esperada += m.Metros;
            }
            conTexto = indices.ToArray();
            posicionesTexto = posiciones.ToArray();
        }

        /// <summary>Posición en la línea (m) de la maniobra de Ruta.Maniobras con ese índice; null si no tiene texto.</summary>
        public double? PosicionManiobra(int indice)
        {
            int k = Array.IndexOf(conTexto, indice);
            return k < 0 ? (double?)null : posicionesTexto[k];
        }

        /// <summary>
        /// Estado con esta posición. anterior es el de la posición de antes en esta ruta (null la primera vez): el avance
        /// no baja de el suyo.
        /// </summary>
        public EstadoNavegacion Seguir(Coordenada posicion, EstadoNavegacion anterior = null)
        {
            var p = Ruta.Puntos;
            if (p.Count == 0)
                return new EstadoNavegacion(posicion, 0, 0, 0, Ruta.Metros, Ruta.Segundos, -1, null, 0, null, 0);
            if (p.Count == 1)
                return Estado(p[0], 0, 0, posicion.DistanciaMetros(p[0]));

            double base_ = anterior != null ? Math.Max(0, Math.Min(anterior.Avance, Total)) : 0;
            double minimo = base_ - ParametrosNegocio.NavegacionMargenAtrasMetros;
            Candidato cerca = Candidato.Ninguno, lejos = Candidato.Ninguno;
            for (int i = 0; i + 1 < p.Count; i++)
            {
                if (acumuladas[i + 1] < minimo) continue; // tramo entero por detrás del margen
                var c = Proyectar(posicion, i, minimo);
                if (c.avance > base_ + ParametrosNegocio.NavegacionSaltoMetros)
                {
                    if (c.distancia < lejos.distancia) lejos = c;
                }
                else if (c.distancia < cerca.distancia) cerca = c;
            }
            var mejor = cerca;
            if (lejos.Valido && (!cerca.Valido || lejos.distancia + ParametrosNegocio.NavegacionEmpateMetros < cerca.distancia))
                mejor = lejos;
            if (!mejor.Valido) return Estado(p[p.Count - 1], p.Count - 2, Total, posicion.DistanciaMetros(p[p.Count - 1]));

            // Por detrás del último avance (ruido): se queda donde estaba
            if (anterior != null && mejor.avance < base_)
            {
                var (punto, tramo) = PuntoEn(base_);
                return Estado(punto, tramo, base_, mejor.distancia);
            }
            return Estado(mejor.punto, mejor.tramo, mejor.avance, mejor.distancia);
        }

        /// <summary>Lo que queda de línea: el punto proyectado y los puntos de la ruta por delante de él.</summary>
        public IReadOnlyList<Coordenada> PuntosRestantes(EstadoNavegacion estado)
        {
            var p = Ruta.Puntos;
            if (estado == null || p.Count < 2) return p;
            var lista = new List<Coordenada>(p.Count - estado.Tramo) { estado.Proyectado };
            for (int i = Math.Max(0, estado.Tramo + 1); i < p.Count; i++)
                if (p[i] != lista[lista.Count - 1]) lista.Add(p[i]);
            if (lista.Count < 2) lista.Add(p[p.Count - 1]); // al final: una línea de un punto no se dibuja
            return lista;
        }

        EstadoNavegacion Estado(Coordenada proyectado, int tramo, double avance, double distancia)
        {
            double fraccion = Total > 0 ? Math.Max(0, Total - avance) / Total : 0;
            double metros = Ruta.Metros > 0 ? Ruta.Metros * fraccion : Math.Max(0, Total - avance);
            double segundos = Ruta.Segundos * fraccion;
            int k = 0;
            while (k < posicionesTexto.Length && posicionesTexto[k] < avance) k++;
            Maniobra siguiente = null, despues = null;
            double aSiguiente = 0, aDespues = 0;
            int indice = -1;
            if (k < conTexto.Length)
            {
                indice = conTexto[k];
                siguiente = Ruta.Maniobras[indice];
                aSiguiente = Math.Max(0, posicionesTexto[k] - avance);
                if (k + 1 < conTexto.Length)
                {
                    despues = Ruta.Maniobras[conTexto[k + 1]];
                    aDespues = Math.Max(0, posicionesTexto[k + 1] - avance);
                }
            }
            return new EstadoNavegacion(proyectado, tramo, avance, distancia, metros, segundos, indice, siguiente, aSiguiente,
                despues, aDespues);
        }

        struct Candidato
        {
            public int tramo;
            public double avance, distancia;
            public Coordenada punto;
            public bool Valido => !double.IsPositiveInfinity(distancia);
            public static Candidato Ninguno => new Candidato { distancia = double.PositiveInfinity };
        }

        /// <summary>
        /// El punto del tramo i más cercano a la posición (equirrectangular local centrada en ella), sin bajar de los
        /// metros mínimos de la línea (lo de más atrás del margen no cuenta).
        /// </summary>
        Candidato Proyectar(Coordenada posicion, int i, double minimo = double.NegativeInfinity)
        {
            var a = Ruta.Puntos[i];
            var b = Ruta.Puntos[i + 1];
            double cos = Math.Cos(posicion.Latitud * GradosARadianes);
            double ax = (a.Longitud - posicion.Longitud) * cos * MetrosPorGrado, ay = (a.Latitud - posicion.Latitud) * MetrosPorGrado;
            double bx = (b.Longitud - posicion.Longitud) * cos * MetrosPorGrado, by = (b.Latitud - posicion.Latitud) * MetrosPorGrado;
            double dx = bx - ax, dy = by - ay;
            double l2 = dx * dx + dy * dy;
            double t = l2 > 0 ? Math.Max(0, Math.Min(1, -(ax * dx + ay * dy) / l2)) : 0;
            double largo = acumuladas[i + 1] - acumuladas[i];
            if (minimo > acumuladas[i] && largo > 0) t = Math.Max(t, Math.Min(1, (minimo - acumuladas[i]) / largo));
            double px = ax + t * dx, py = ay + t * dy;
            return new Candidato
            {
                tramo = i,
                avance = acumuladas[i] + t * (acumuladas[i + 1] - acumuladas[i]),
                distancia = Math.Sqrt(px * px + py * py),
                punto = new Coordenada(a.Latitud + t * (b.Latitud - a.Latitud), a.Longitud + t * (b.Longitud - a.Longitud)),
            };
        }

        /// <summary>Tramo más cercano a una maniobra desde el tramo dado, cerca de donde la esperan sus metros.</summary>
        Candidato MejorPara(Coordenada punto, int desde, double? esperada)
        {
            var mejor = Candidato.Ninguno;
            double coste = double.PositiveInfinity;
            for (int i = Math.Max(0, desde); i + 1 < Ruta.Puntos.Count; i++)
            {
                var c = Proyectar(punto, i);
                double k = c.distancia + (esperada.HasValue ? PesoEsperada * Math.Abs(c.avance - esperada.Value) : 0);
                if (k < coste) { coste = k; mejor = c; }
            }
            return mejor;
        }

        /// <summary>El punto de la línea a esos metros del principio y su tramo.</summary>
        (Coordenada punto, int tramo) PuntoEn(double metros)
        {
            var p = Ruta.Puntos;
            for (int i = 0; i + 1 < p.Count; i++)
            {
                if (acumuladas[i + 1] < metros && i + 2 < p.Count) continue;
                double largo = acumuladas[i + 1] - acumuladas[i];
                double t = largo > 0 ? Math.Max(0, Math.Min(1, (metros - acumuladas[i]) / largo)) : 0;
                var a = p[i];
                var b = p[i + 1];
                return (new Coordenada(a.Latitud + t * (b.Latitud - a.Latitud), a.Longitud + t * (b.Longitud - a.Longitud)), i);
            }
            return (p[p.Count - 1], Math.Max(0, p.Count - 2));
        }
    }
}
