namespace PriceMap.Aplicacion
{
    /// <summary>Lo que hace el botón de ubicación según el estado (ServicioUbicacion.AlTocarBoton).</summary>
    public enum AccionUbicacion
    {
        /// <summary>Buscando (o sin posición todavía).</summary>
        Nada,
        /// <summary>Sin permiso: vuelve a salir la explicación.</summary>
        Explicar,
        /// <summary>Con posición: el mapa se centra en el usuario (también si ya está centrado y al norte).</summary>
        Centrar,
        /// <summary>Con posición, ya centrado y con el mapa girado: el giro vuelve a 0 (norte arriba).</summary>
        OrientarNorte,
        /// <summary>Permiso denegado o ubicación del móvil apagada: aviso con "Abrir Ajustes".</summary>
        Avisar,
    }
}
