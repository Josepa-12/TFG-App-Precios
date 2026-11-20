namespace PriceMap.Dominio
{
    /// <summary>
    /// Qué hay que hacer en una maniobra de la ruta (historia 2.6), común a Valhalla (su número de tipo) y a OSRM (su
    /// tipo y su modificador). Los textos de las maniobras de OSRM se escriben con esto en la historia 2.7.
    /// </summary>
    public enum TipoManiobra
    {
        /// <summary>Otra cosa (transbordador, ascensor...), sin traducción propia.</summary>
        Otra,
        Salida,
        Llegada,
        Recto,
        LigeroDerecha,
        Derecha,
        CerradoDerecha,
        CambioSentido,
        CerradoIzquierda,
        Izquierda,
        LigeroIzquierda,
        Incorporacion,
        Rotonda,
        SalidaRotonda,
    }
}
