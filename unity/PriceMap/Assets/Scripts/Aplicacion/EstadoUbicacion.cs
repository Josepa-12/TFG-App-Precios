namespace PriceMap.Aplicacion
{
    /// <summary>Estado de la ubicación del usuario (FR-5).</summary>
    public enum EstadoUbicacion
    {
        /// <summary>Aún no se ha concedido el permiso (ni se ha denegado en esta sesión).</summary>
        SinPermiso,
        /// <summary>Android ha denegado el permiso.</summary>
        Denegado,
        /// <summary>Permiso concedido, esperando la primera posición.</summary>
        Buscando,
        /// <summary>Hay posición.</summary>
        Activa,
    }
}
