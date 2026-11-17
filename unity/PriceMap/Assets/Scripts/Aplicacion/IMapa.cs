using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto del mapa (AD-13): lo único que el resto de la app puede pedirle al mapa.
    /// Empieza con lo mínimo; cada historia añade lo que necesite (tema, marcadores, posición, ruta...).
    /// </summary>
    public interface IMapa
    {
        /// <summary>Centra la vista en una coordenada real.</summary>
        void Centrar(Coordenada destino);
    }
}
