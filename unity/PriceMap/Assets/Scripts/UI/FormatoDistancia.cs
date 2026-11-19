using System;
using System.Globalization;

namespace PriceMap.UI
{
    /// <summary>
    /// Distancias con el formato de es-ES (historia 2.3): por debajo de 1.000 m, en metros redondeados a 10
    /// ("350 m"); desde ahí, en kilómetros con un decimal si no es exacto ("1,2 km", "3 km"). El espacio antes de la
    /// unidad es de no separación, como el del euro, para que nunca se parta la línea.
    /// </summary>
    public static class FormatoDistancia
    {
        const string EspacioDuro = " ";

        static readonly NumberFormatInfo Es = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

        public static string Texto(double metros)
        {
            if (double.IsNaN(metros) || metros < 0) metros = 0;
            double redondeados = Math.Round(metros / 10.0, MidpointRounding.AwayFromZero) * 10.0;
            if (redondeados < 1000.0) return redondeados.ToString("0", Es) + EspacioDuro + "m";
            double km = Math.Round(metros / 1000.0, 1, MidpointRounding.AwayFromZero);
            return km.ToString("0.#", Es) + EspacioDuro + "km";
        }
    }
}
