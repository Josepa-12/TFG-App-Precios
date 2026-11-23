namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de la pantalla de inicio (spec pantalla-de-inicio): la dibuja el sistema por encima de Unity desde que se
    /// abre la app, con el pin que cae. La app solo le pregunta si sigue y le dice cuándo irse.
    /// </summary>
    public interface IPantallaInicio
    {
        /// <summary>
        /// Segundos desde que el pin empezó a caer (0 si aún no ha podido); null si no hay pantalla de inicio o ya se ha
        /// ido del todo (PC, editor, captura o tras su animación de salida).
        /// </summary>
        double? Segundos { get; }

        /// <summary>
        /// Se va: el pin se encoge hasta ese punto (px, origen arriba a la izquierda, como IMapa.Proyectar) mientras el
        /// fondo se desvanece. Null: el centro de la pantalla. Sigue a la vista menos de 1 s (Segundos dice cuándo acaba).
        /// </summary>
        void Irse(PuntoPantalla? destino);
    }
}
