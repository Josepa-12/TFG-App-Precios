namespace PriceMap.Dominio
{
    /// <summary>Qué tema usar al arrancar y cómo se guarda la elección.</summary>
    public static class ReglaTema
    {
        const string ClaveClaro = "claro";
        const string ClaveOscuro = "oscuro";

        /// <summary>
        /// Manda la elección guardada; si no hay ninguna (o no se reconoce), el tema del sistema (UX-DR22).
        /// </summary>
        public static Tema Inicial(string guardado, bool sistemaOscuro)
        {
            if (guardado == ClaveOscuro) return Tema.Oscuro;
            if (guardado == ClaveClaro) return Tema.Claro;
            return sistemaOscuro ? Tema.Oscuro : Tema.Claro;
        }

        /// <summary>Texto con el que se guarda el tema en las preferencias.</summary>
        public static string AClave(Tema tema) => tema == Tema.Oscuro ? ClaveOscuro : ClaveClaro;

        public static Tema Contrario(Tema tema) => tema == Tema.Oscuro ? Tema.Claro : Tema.Oscuro;
    }
}
