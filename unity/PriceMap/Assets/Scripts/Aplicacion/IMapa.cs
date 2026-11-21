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

        /// <summary>
        /// Mueve la vista para que la coordenada quede en ese punto de la pantalla (px, origen arriba a la
        /// izquierda, como Proyectar), al instante y sin cambiar el zoom, el giro ni la inclinación. Lo usa la
        /// ficha para dejar el lugar en la zona del mapa que no tapa (historia 1.8, parte 2).
        /// </summary>
        void Centrar(Coordenada destino, PuntoPantalla enPantalla);

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

        /// <summary>
        /// Giro del mapa en grados: rumbo hacia el que mira la cámara (0 = norte arriba, en sentido horario).
        /// Un rumbo real se ve en pantalla girado (rumbo − Giro).
        /// </summary>
        float Giro { get; }

        /// <summary>Coordenada real del centro de la vista (el punto al que mira la cámara).</summary>
        Coordenada Centro { get; }

        /// <summary>Pone el giro a 0 (norte arriba) sin cambiar el zoom ni la inclinación, al instante como Centrar.</summary>
        void OrientarNorte();

        /// <summary>
        /// Toque sobre el mapa (el dedo se suelta tras moverse menos de 6 dp), en px con origen arriba a la
        /// izquierda, como Proyectar. No llega si el gesto empezó sobre un control de la interfaz.
        /// </summary>
        event Action<PuntoPantalla> Toque;

        /// <summary>
        /// Pulsación larga sobre el mapa (historia 2.5): un dedo quieto 500 ms (menos de 6 dp), en px con origen
        /// arriba a la izquierda, como Toque. Se lanza sin esperar a soltar; ese gesto ya no da Toque. Tampoco llega
        /// si el gesto empezó sobre un control de la interfaz ni con dos dedos.
        /// </summary>
        event Action<PuntoPantalla> PulsacionLarga;

        /// <summary>
        /// Coordenada real del suelo bajo ese punto de pantalla (px, origen arriba a la izquierda, como Proyectar);
        /// null si cae por encima del horizonte o no hay cámara.
        /// </summary>
        Coordenada? Desproyectar(PuntoPantalla punto);

        /// <summary>
        /// Dibuja la línea de la ruta (historia 2.6, parte 2a) por esos puntos, en orden, cubriendo la calle: por encima
        /// de las calles y por debajo de los edificios y de lo que dibuja la UI. Sustituye a la anterior. La aproximada
        /// (sin conexión, parte 2b) va en los colores más suaves de la ruta aproximada de su tema.
        /// </summary>
        void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false);

        /// <summary>Quita la línea de la ruta, si la hay.</summary>
        void QuitarRuta();

        /// <summary>
        /// Mueve y acerca la vista al instante para que todos los puntos quepan en ese rectángulo de la pantalla (px,
        /// origen arriba a la izquierda), sin cambiar el giro ni la inclinación y sin pasar los límites de la cámara.
        /// </summary>
        void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre);
    }
}
