namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Dónde cae una coordenada real en la pantalla en este fotograma: X e Y en píxeles de pantalla
    /// (origen arriba a la izquierda) y si está delante de la cámara. Los bordes y el margen los decide la UI.
    /// </summary>
    public readonly struct PuntoPantalla
    {
        public float X { get; }
        public float Y { get; }
        /// <summary>Distancia a la cámara a lo largo de su eje (z de la vista); ≤ 0 si está detrás.</summary>
        public float Profundidad { get; }
        /// <summary>True si el punto está delante de la cámara (z &gt; 0).</summary>
        public bool Delante => Profundidad > 0f;

        public PuntoPantalla(float x, float y, float profundidad)
        {
            X = x;
            Y = y;
            Profundidad = profundidad;
        }

        /// <summary>Para cuando no hay cámara: nunca se ve.</summary>
        public static readonly PuntoPantalla Ninguno = new PuntoPantalla(0f, 0f, -1f);

        public override string ToString() => $"({X:F1}, {Y:F1}) z={Profundidad:F1}";
    }
}
