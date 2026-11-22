namespace PriceMap.Dominio
{
    /// <summary>
    /// Dónde va el usuario en la ruta (historia 2.7, parte 1; lo calcula ReglaNavegacion): el punto de la ruta más
    /// cercano a su posición, lo que queda, la maniobra siguiente y la de después con su distancia, y lo lejos que está
    /// de la ruta. Inmutable.
    /// </summary>
    public sealed class EstadoNavegacion
    {
        /// <summary>Punto de la ruta en el que se proyecta la posición: la línea recortada empieza aquí.</summary>
        public Coordenada Proyectado { get; }
        /// <summary>Tramo (de Puntos[Tramo] a Puntos[Tramo + 1]) en el que cae el punto proyectado.</summary>
        public int Tramo { get; }
        /// <summary>Metros recorridos por la línea desde el principio hasta el punto proyectado; nunca baja.</summary>
        public double Avance { get; }
        /// <summary>Metros entre la posición y la ruta.</summary>
        public double DistanciaRuta { get; }
        /// <summary>Metros que quedan (los de la ruta en proporción a lo que queda de línea).</summary>
        public double MetrosRestantes { get; }
        /// <summary>Segundos que quedan: duración × (restante / total).</summary>
        public double SegundosRestantes { get; }
        /// <summary>Índice en Ruta.Maniobras de la maniobra siguiente; -1 si no hay (ruta sin maniobras con texto).</summary>
        public int IndiceSiguiente { get; }
        /// <summary>Maniobra siguiente (la primera con texto que no ha quedado atrás); null si no hay.</summary>
        public Maniobra Siguiente { get; }
        /// <summary>Metros por la ruta hasta la maniobra siguiente (0 sin ella).</summary>
        public double MetrosASiguiente { get; }
        /// <summary>La de después de la siguiente; null si no hay.</summary>
        public Maniobra Despues { get; }
        /// <summary>Metros por la ruta hasta la de después (0 sin ella).</summary>
        public double MetrosADespues { get; }

        public EstadoNavegacion(Coordenada proyectado, int tramo, double avance, double distanciaRuta, double metrosRestantes,
            double segundosRestantes, int indiceSiguiente, Maniobra siguiente, double metrosASiguiente, Maniobra despues,
            double metrosADespues)
        {
            Proyectado = proyectado;
            Tramo = tramo;
            Avance = avance;
            DistanciaRuta = distanciaRuta;
            MetrosRestantes = metrosRestantes;
            SegundosRestantes = segundosRestantes;
            IndiceSiguiente = indiceSiguiente;
            Siguiente = siguiente;
            MetrosASiguiente = metrosASiguiente;
            Despues = despues;
            MetrosADespues = metrosADespues;
        }

        /// <summary>Minutos que quedan, como los de la ficha (hacia arriba, como mínimo 1), al segundo (Ruta.MinutosDe).</summary>
        public int MinutosRestantes => Ruta.MinutosDe(SegundosRestantes);

        /// <summary>Está a más de NavegacionDesvioMetros de la ruta.</summary>
        public bool FueraDeRuta => DistanciaRuta > ParametrosNegocio.NavegacionDesvioMetros;
    }
}
