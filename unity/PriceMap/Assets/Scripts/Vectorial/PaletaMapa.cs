using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>Paletas del mapa (tokens map-* de DESIGN.md, iguales a las del prototipo web).</summary>
    public struct PaletaMapa
    {
        public Color fondo, agua, verde, calle, edificio, etiqueta;
        public Color fachada; // lado en sombra de los edificios
        public Color sombra;  // multiplicador de las sombras proyectadas (azulado, como la paleta)
        public Color borde, calleMenor; // borde de las calles y calles secundarias
        public Color ruta, rutaBorde;   // línea de la ruta (accent) y su borde, algo más oscuro (historia 2.6, parte 2a)
        public Color rutaAproximada, rutaAproximadaBorde; // ruta aproximada sin conexión, más suave (parte 2b, 21/11/2026)
        public Color rutaAlternativa, rutaAlternativaBorde; // rutas no elegidas en «Cómo llegar», más claras (maqueta del 22/11/2026)

        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public static PaletaMapa De(TemaMapa t) => t == TemaMapa.Oscuro
            ? new PaletaMapa { fondo = H("#141B30"), agua = H("#0A0F1F"), verde = H("#18213A"), calle = H("#2A3554"), edificio = H("#2A3A63"), fachada = H("#1A2442"), etiqueta = H("#9AA6C4"), sombra = H("#8A90A8"), borde = H("#0F1424"), calleMenor = H("#222C48"), ruta = H("#4C7DFF"), rutaBorde = H("#2F5BEA"), rutaAproximada = H("#6F86C9"), rutaAproximadaBorde = H("#44599A"), rutaAlternativa = H("#6F86C9"), rutaAlternativaBorde = H("#44599A") }
            : new PaletaMapa { fondo = H("#E9EDF7"), agua = H("#BFD0F4"), verde = H("#DCE6EE"), calle = H("#FFFFFF"), edificio = H("#D3DCF2"), fachada = H("#97A6D0"), etiqueta = H("#4B5878"), sombra = H("#A7B1D4"), borde = H("#C9D4EE"), calleMenor = H("#F7F9FE"), ruta = H("#2F5BEA"), rutaBorde = H("#1E3FB0"), rutaAproximada = H("#8FA3E6"), rutaAproximadaBorde = H("#5F78C9"), rutaAlternativa = H("#8FA6E8"), rutaAlternativaBorde = H("#5F78C9") };
    }
}
