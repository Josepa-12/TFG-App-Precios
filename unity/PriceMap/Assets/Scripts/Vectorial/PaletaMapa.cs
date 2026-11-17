using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>Paletas del mapa, copiadas del prototipo web (prototipo/app.js, colores light/dark).</summary>
    public struct PaletaMapa
    {
        public Color fondo, agua, verde, calle, edificio, etiqueta;
        public Color fachada; // lado en sombra de los edificios
        public Color sombra;  // multiplicador de las sombras proyectadas (azulado, como la paleta)
        public Color borde, calleMenor; // borde de las calles y calles secundarias

        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public static PaletaMapa De(TemaMapa t) => t == TemaMapa.Oscuro
            ? new PaletaMapa { fondo = H("#141B30"), agua = H("#0A0F1F"), verde = H("#18213A"), calle = H("#2A3554"), edificio = H("#2A3A63"), fachada = H("#1A2442"), etiqueta = H("#9AA6C4"), sombra = H("#8A90A8"), borde = H("#0F1424"), calleMenor = H("#222C48") }
            : t == TemaMapa.Claro
            ? new PaletaMapa { fondo = H("#E9EDF7"), agua = H("#BFD0F4"), verde = H("#DCE6EE"), calle = H("#FFFFFF"), edificio = H("#D3DCF2"), fachada = H("#97A6D0"), etiqueta = H("#4B5878"), sombra = H("#A7B1D4"), borde = H("#C9D4EE"), calleMenor = H("#F7F9FE") }
            : new PaletaMapa { fondo = H("#F2EFE9"), agua = H("#AAD3DF"), verde = H("#C8FACC"), calle = H("#FFFFFF"), edificio = H("#D9D0C9"), fachada = H("#A89A8E"), etiqueta = H("#13235B"), sombra = H("#B8B8C0"), borde = H("#C8BFB4"), calleMenor = H("#FFFFFF") };
    }
}
