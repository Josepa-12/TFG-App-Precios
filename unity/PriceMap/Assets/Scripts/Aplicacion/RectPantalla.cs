namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Rectángulo de la pantalla en píxeles, con el origen arriba a la izquierda (como PuntoPantalla): la zona libre
    /// del mapa donde se encuadra la ruta (historia 2.6, parte 2a), entre las tarjetas y con los márgenes de los lados.
    /// </summary>
    public readonly struct RectPantalla
    {
        public float Izquierda { get; }
        public float Arriba { get; }
        public float Derecha { get; }
        public float Abajo { get; }

        public float Ancho => Derecha - Izquierda;
        public float Alto => Abajo - Arriba;
        /// <summary>True si tiene ancho y alto positivos (sin NaN).</summary>
        public bool Valido => Ancho > 0f && Alto > 0f;

        public RectPantalla(float izquierda, float arriba, float derecha, float abajo)
        {
            Izquierda = izquierda;
            Arriba = arriba;
            Derecha = derecha;
            Abajo = abajo;
        }

        public override string ToString() => $"({Izquierda:F0}, {Arriba:F0}) – ({Derecha:F0}, {Abajo:F0})";
    }
}
