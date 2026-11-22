namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de la voz (historia 2.8): lee una frase en castellano con la voz del sistema. En Android, el TextToSpeech
    /// (PriceMap.Adaptadores.Voz.VozAndroid), que calla con el móvil en silencio o en vibración; en el PC y el editor, nada.
    /// </summary>
    public interface IVoz
    {
        /// <summary>Dice la frase; corta la que estuviera sonando.</summary>
        void Hablar(string texto);

        /// <summary>Calla lo que esté diciendo.</summary>
        void Parar();
    }
}
