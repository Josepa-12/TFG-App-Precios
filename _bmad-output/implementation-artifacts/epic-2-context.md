# Epic 2 Context: Buscar y llegar a cualquier sitio

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Convertir el mapa de la Épica 1 en una herramienta para llegar a cualquier sitio: el usuario busca lugares, puntos de interés, categorías y direcciones desde la hoja del buscador, abre ubicaciones compartidas o marca un punto en el mapa, y navega giro a giro por la ruta más corta (flecha en coche, cono a pie, voz opcional) con el GPS activo aunque apague la pantalla, hasta "Has llegado". Importa porque la ruta iniciada con la app es la base de la visita verificada y de la pregunta de llegada de la Épica 4, que es el núcleo del aporte de precios.

## Stories

- Story 2.1: GPS activo con la pantalla apagada durante una ruta
- Story 2.2: Hoja del buscador desplegable
- Story 2.3: Buscar lugares, puntos de interés y direcciones
- Story 2.4: Encuentra cerca por categorías
- Story 2.5: Ir a un punto libre o a una ubicación compartida
- Story 2.6: Ver los tiempos y calcular la ruta más corta
- Story 2.7: Navegar giro a giro
- Story 2.8: Indicaciones por voz
- Story 2.9: Llegar y terminar la ruta

## Requirements & Constraints

- **Hoja del buscador:** sigue al dedo y se queda donde se suelta; lanzamiento > 0,6 px/ms abre o cierra del todo; < 15 % se pliega, > 88 % se abre; > 6 px es arrastre (no saca el teclado). A media altura se puede escribir y arrastrar la lista mueve la hoja; abierta del todo, la lista se desplaza. Antes de escribir: favoritos, categorías y frecuentes; en el primer uso solo categorías y "Encuentra cerca".
- **Búsqueda:** lugares de la app, puntos de interés y direcciones, priorizando los cercanos. Sin resultados: "No hay resultados" + buscar como dirección. Sin conexión: lugares de los datos base y aviso de que las direcciones necesitan conexión.
- **Encuentra cerca:** al elegir categoría, el mapa muestra solo esa categoría y una lista por distancia, primero lugares con precios y luego puntos de interés, sin duplicar los que ya están en la app.
- **Punto libre y enlaces:** pulsación larga en el mapa abre ficha de punto libre con dirección por geocodificación inversa. Se aceptan enlaces de Google Maps (`?q=`, `@lat,lng`, `!3d…!4d…`), Apple Maps (`ll=`), `geo:`, coordenadas sueltas y enlaces cortos (`maps.app.goo.gl`, resueltos siguiendo la redirección), compartidos a la app o pegados en el buscador. Enlace no reconocido: mensaje claro y no abre nada. Direcciones y puntos navegados no van al historial ni generan pregunta de llegada.
- **Ruta:** la más corta por calles reales, respetando en coche el sentido de circulación; empieza y acaba en el punto de calle más cercano. Sin conexión: ruta aproximada con aviso. La ficha muestra coche (por defecto) y a pie con minutos y botón Ir, visibles sin desplazar.
- **Navegación:** sigue la posición real, siguiente maniobra en castellano y tiempo restante; recalcula si se aleja > 40 m durante > 5 s, sin borrar la ruta anterior hasta tener la nueva; se recorta lo ya recorrido. GPS sin señal: última posición atenuada, aviso "Buscando señal GPS", no recalcula ni crea/descarta visita.
- **Voz:** si está activada, maniobras leídas en castellano también con la pantalla apagada; con voz desactivada o móvil en silencio, nada suena y la maniobra se sigue viendo.
- **Llegada:** a ≤ 30 m, "Has llegado" con "Terminar ruta"; cierre automático al minuto, anunciado por TalkBack ("La ruta se cerrará en un minuto"); al terminar se para el GPS en segundo plano y desaparece la notificación.
- **GPS en segundo plano:** solo durante una ruta, con notificación fija "Navegando a <destino> · N min"; nunca sin ruta. En Android 13+ se pide permiso de notificaciones antes; si se deniega, la navegación sigue en primer plano y se explica la limitación.
- **Accesibilidad (en cada historia):** contraste ≥ 4,5:1, zonas táctiles ≥ 48 dp, TalkBack en buscador y navegación, "reducir animaciones" respetado, textos localizables en `es-ES`.

## Technical Decisions

- **Capas hexagonales (asmdef):** Dominio puro, Aplicación con servicios y puertos, adaptadores por tecnología: `Rutas`, `Busqueda`, `Gps`, `Voz`, `Datos`; el mapa vectorial es el adaptador de `IMapa`. La UI solo ve Aplicación y Dominio y observa estado de los servicios. Composición solo en `Arranque`; sin singletons.
- **Mapa solo por `IMapa`** (tema, marcadores, ruta, posición, centrar, toque y pulsación larga); lat/lon en `double` fuera del adaptador, `Vector3` solo dentro.
- **Servicios externos:** rutas con Valhalla (FOSSGIS, `shortest`) y OSRM de reserva (quedarse con la más corta de sus alternativas); direcciones y geocodificación inversa con Photon (komoot); voz con un plugin del TextToSpeech de Android; enlaces compartidos con intent filters de Android para `geo:` y Google Maps.
- **Lógica a portar del prototipo** a C# (Dominio, con pruebas EditMode): parseo de enlaces y traducción de maniobras de Valhalla al castellano.
- **GPS en segundo plano (AD-19):** servicio en primer plano nativo (`Assets/Plugins/Android/`, tipo `location`, `FOREGROUND_SERVICE_LOCATION`); solo el adaptador de GPS lo arranca (al pulsar Ir, con la app delante) y lo para, a petición del servicio de navegación; sin `ACCESS_BACKGROUND_LOCATION`; sus posiciones llegan por el mismo puerto de GPS que en primer plano.
- **Visitas (AD-9):** la navegación no crea visitas; solo emite eventos (`RutaIniciada`, `TiempoRestanteCambiado`) que consumirá `ServicioVisitas` en la Épica 4.
- **Umbrales** (40 m, 5 s, 30 m, 1 min, física de la hoja) en `ParametrosNegocio`, no dispersos.
- **Convenciones:** código en castellano sin tildes en identificadores, `Resultado<T>` entre capas, `async`/`Task`, textos con Localization, Android minSdk 26 / targetSdk 35. Servicios gratuitos para el TFG.

## UX & Interaction Patterns

- Todo nace del mapa, sin pestañas; las hojas se apilan como mucho dos niveles (ficha sobre buscador); el botón atrás de Android cierra la capa superior.
- Cápsula de cristal inferior con buscador y avatar: tocar el campo abre con teclado, arrastrar abre sin teclado. Con TalkBack o sin arrastrar, tocar la cápsula abre del todo y la hoja ofrece "Expandir"/"Contraer". Foco al título al abrir una hoja y de vuelta al cerrar.
- Con TalkBack: botones de acercar/alejar y la lista de "Encuentra cerca" como alternativa al mapa; cada elemento se anuncia con nombre, tipo, distancia y precio, y abre la ficha sin tocar el mapa.
- Posición: círculo fuera de ruta, flecha con rumbo en coche, cono a pie si hay brújula; gira respecto al norte aunque el mapa esté girado. La línea de ruta usa el estilo de ruta del usuario (cosméticos en la Épica 5).
- "Reducir animaciones" quita transiciones de hojas y el movimiento suave de cámara. Iconos vectoriales, nunca emojis. Botón primario único por pantalla (Ir).
- Referencias visuales: capturas del prototipo de buscador (`05`, `06*`), punto libre (`06g`, `06h`) y navegación (`10*`, `11a`).

## Cross-Story Dependencies

- 2.1 (servicio de GPS) es la base de 2.7 y 2.9 (arranque con la notificación de navegación y parada al terminar).
- 2.2 (hoja) contiene 2.3 (búsqueda) y 2.4 (Encuentra cerca); 2.3 también acepta enlaces pegados de 2.5.
- 2.5 y la ficha de la Épica 1 llevan a 2.6 (tiempos e Ir); 2.6 → 2.7 → 2.8 y 2.9.
- Con la Épica 1: usa `IMapa`, la ficha (activa su botón Ir), lugares base y puntos de interés.
- Con épicas posteriores: favoritos de direcciones y puntos (Épica 3); visita verificada a ≤ 5 min y pregunta de llegada (Épica 4, vía eventos de navegación); cosméticos de posición y ruta (Épica 5); ruta parada a parada del plan (Épica 9).
