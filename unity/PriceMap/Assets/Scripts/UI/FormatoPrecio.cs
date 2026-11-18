using System;
using System.Globalization;
using PriceMap.Dominio;

namespace PriceMap.UI
{
    /// <summary>
    /// Importes en euros con el formato de es-ES (AD-10: el Dominio da céntimos enteros y solo la
    /// Presentación los formatea). 2000 → "20 €", 2250 → "22,50 €", 123400 → "1234 €", 1234500 → "12.345 €".
    /// En es-ES solo se agrupan los miles a partir de 5 cifras (agrupación mínima 2, CLDR y RAE), y el
    /// espacio antes de € es de no separación para que nunca se parta la línea.
    /// </summary>
    public static class FormatoPrecio
    {
        const string EspacioDuro = " ";

        // Separadores fijos de es-ES: no dependen de los datos de cultura de la plataforma (IL2CPP)
        static readonly NumberFormatInfo ConMiles = new NumberFormatInfo
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            NumberGroupSizes = new[] { 3 },
            NegativeSign = "-",
        };

        /// <summary>Euros sin decimales si son exactos; si no, con dos decimales.</summary>
        public static string Euros(int centimos) => Numero(centimos) + EspacioDuro + "€";

        /// <summary>Solo la cifra, sin "€" (el mínimo de un rango: "10–20 €").</summary>
        public static string Numero(int centimos)
        {
            decimal euros = centimos / 100m;
            // Agrupación mínima 2: con 4 cifras enteras o menos, sin punto de miles ("F" no agrupa)
            string tipo = System.Math.Abs(centimos / 100) >= 10000 ? "N" : "F";
            return euros.ToString(tipo + (centimos % 100 == 0 ? "0" : "2"), ConMiles);
        }

        /// <summary>
        /// Precio de la ficha (historia 1.8): fijo "90 €", "desde 40 €" y, en hostelería, rango
        /// "10–20 € por persona" (abierto: "más de 50 € por persona"). Los textos van por Localization.
        /// </summary>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public static string Precio(ValorPrecio valor, Func<string, string> textos = null)
        {
            textos ??= Textos.T;
            switch (valor.Forma)
            {
                case FormaPrecio.Desde:
                    return Textos.Formatear(textos(ClavesTexto.FichaPrecioDesde), Euros(valor.ImporteCent));
                case FormaPrecio.Rango:
                    return valor.MaxCent == null
                        ? Textos.Formatear(textos(ClavesTexto.FichaPrecioRangoAbierto), Euros(valor.MinCent))
                        : Textos.Formatear(textos(ClavesTexto.FichaPrecioRango), Numero(valor.MinCent), Euros(valor.MaxCent.Value));
                default:
                    return Euros(valor.ImporteCent);
            }
        }
    }
}
