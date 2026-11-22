namespace PriceMap.Dominio
{
    /// <summary>Qué dice la voz al navegar (historia 2.8): todas las indicaciones (por defecto), solo las importantes o nada.</summary>
    public enum ModoVoz
    {
        /// <summary>La siguiente al pasar una maniobra, el aviso intermedio (300 m en coche, 50 m a pie) y el del momento.</summary>
        Todo,
        /// <summary>Solo la siguiente al pasar una maniobra y el aviso del momento.</summary>
        Importante,
        /// <summary>Calla.</summary>
        Nada,
    }
}
