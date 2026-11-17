using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto del mapa (AD-13): lo único que el resto de la app puede pedirle al mapa.
    /// Crece con cada historia (marcadores, posición, ruta...).
    /// </summary>
    public interface IMapa
    {
        /// <summary>Centra la vista en una coordenada real.</summary>
        void Centrar(Coordenada destino);

        /// <summary>Pinta el mapa con la paleta del tema (B claro, D oscuro).</summary>
        void AplicarTema(Tema tema);

        /// <summary>Acerca la vista un paso (botones de zoom, alternativa a los gestos).</summary>
        void Acercar();

        /// <summary>Aleja la vista un paso.</summary>
        void Alejar();
    }
}
