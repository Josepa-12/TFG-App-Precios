using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Dónde cae la coordenada en la pantalla con la cámara de este fotograma (px, origen arriba a
        /// la izquierda). Sirve para colocar encima del mapa lo que dibuja la UI (marcadores).
        /// </summary>
        PuntoPantalla Proyectar(Coordenada punto);

        /// <summary>
        /// Puntos de interés de OpenStreetMap de las teselas cargadas ahora mismo, sin clasificar
        /// (la UI aplica ReglaPuntosInteres). Cambia al cargar o liberar teselas.
        /// </summary>
        IReadOnlyList<PuntoInteres> PuntosInteres { get; }

        /// <summary>Se lanza cuando cambia PuntosInteres (se carga o se libera una tesela).</summary>
        event Action PuntosInteresCambiados;

        /// <summary>Distancia de la cámara a su punto de mira, en metros (infinito si no hay cámara).</summary>
        float DistanciaCamara { get; }
    }
}
