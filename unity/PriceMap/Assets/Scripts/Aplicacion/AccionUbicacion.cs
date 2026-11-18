namespace PriceMap.Aplicacion
{
    /// <summary>Lo que hace el botón de ubicación según el estado (ServicioUbicacion.AlTocarBoton).</summary>
    public enum AccionUbicacion
    {
        /// <summary>Buscando, denegado (el aviso es de la parte 2) o sin posición.</summary>
        Nada,
        /// <summary>Sin permiso: vuelve a salir la explicación.</summary>
        Explicar,
        /// <summary>Con posición: el mapa se centra en el usuario.</summary>
        Centrar,
    }
}
