namespace PriceMap.Aplicacion
{
    /// <summary>Por qué termina una ruta (evento RutaTerminada, AD-9; la llegada, historia 2.9).</summary>
    public enum MotivoFinRuta
    {
        /// <summary>Antes de llegar, el usuario la termina («Finalizar ruta»); o, en cualquier momento, inicia otra ruta.</summary>
        Manual,
        /// <summary>
        /// Ya en el destino («Has llegado»), se termina antes del minuto: «Finalizar ruta», o una ubicación compartida con
        /// la app o cerrarla.
        /// </summary>
        Llegada,
        /// <summary>Ya en el destino, se cierra sola al minuto (ParametrosNegocio.LlegadaCierreSegundos).</summary>
        Automatico,
    }
}
