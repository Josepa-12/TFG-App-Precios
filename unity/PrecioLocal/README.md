# PrecioLocal en Unity — prueba técnica del mapa

Proyecto de Unity 6.3 (6000.3.28f1, render integrado) para la prueba técnica del mapa real en 2.5D.

## Abrirlo
Unity Hub → **Add / Añadir** → carpeta `unity/PrecioLocal`, o abrir con el editor 6000.3.28f1. La escena `Assets/Scenes/Mapa.unity` se abre sola la primera vez. Pulsa **Play** para que se cargue el mapa.

## Qué hay en la escena
| Objeto | Qué hace |
|---|---|
| **Mapa** (`MapaTeselas.cs`) | Descarga las teselas de OpenStreetMap alrededor de la Plaza del Cabildo (zoom 16, 9×9 teselas, unos 4,4 km) y las coloca en el suelo a escala real (1 unidad = 1 metro, norte = +Z). Las guarda en caché en el disco para no descargarlas otra vez. |
| **Main Camera** (`CamaraMapa.cs`) | Cámara 2.5D como en el prototipo web (inclinación 50°, giro −15°). Ratón: arrastrar = mover, rueda = zoom, botón derecho = girar/inclinar. Teclado: WASD/flechas, Q/E girar, R/F inclinar. Táctil: un dedo mueve, dos dedos zoom y giro. |
| **Marcadores** (`Marcador.cs`) | Tu posición simulada y 4 sitios reales del prototipo (Clínica Areas, Casa Ballén, Padel La Jara, ChemaBarber), colocados por latitud/longitud con los colores de las plantillas. |
| **Suelo (referencia)** | Plano para ver la escena en el editor antes de pulsar Play. |

`Geo.cs` convierte latitud/longitud ↔ metros de la escena (Web Mercator).

**Paleta del mapa:** el shader `Assets/Shaders/TeselaPaleta.shader` recolorea las teselas de OpenStreetMap con los colores del prototipo web (fondo, agua, zonas verdes, calles, edificios y texto). En el componente **Mapa** se elige el tema (Original, Claro u Oscuro); en Play, la tecla **T** cambia de tema.

Para regenerar la escena desde cero: menú **PrecioLocal ▸ Preparar escena del mapa**.

## Escena en 3D con Cesium (`Assets/Scenes/MapaCesium.unity`)
Sanlúcar sobre el globo real de **Cesium for Unity**, con:
- el relieve de Cesium World Terrain;
- el mapa de OpenStreetMap encima;
- los edificios en 3D de Cesium OSM Buildings.

Todo va pintado con la paleta del proyecto y usa la misma cámara y los mismos marcadores que la escena "Mapa".

1. Crea una cuenta gratuita en https://ion.cesium.com/signup.
2. En Unity: **Cesium ▸ Cesium ▸ Connect to Cesium ion**. Si al crear el token da error, copia el *Default Token* de https://ion.cesium.com/tokens y pégalo en **Token ▸ Use an existing token**.
3. Pulsa **Play**. La tecla **T** cambia de tema: Claro, Oscuro u Original.

El token se guarda en `Assets/CesiumSettings`, que está en `.gitignore` y no se sube al repositorio. Para regenerar la escena: menú **PrecioLocal ▸ Preparar escena con Cesium (3D)**.

## Siguientes pasos de la prueba técnica
1. **Estilo propio:** hecho de forma aproximada con el shader de recoloreado (opción A). Un estilo 100 % propio (grosor de calles, qué etiquetas salen) exige teselas vectoriales.
2. **Edificios en 2.5D:** extruir los edificios de la capa `building` de las teselas vectoriales.
3. **GPS real:** `Input.location` en el móvil (Android: compilar con el módulo ya instalado; iPhone: requiere Mac).
4. **Decidir** entre seguir en Unity o con la versión web (ver la memoria, 5.1).

Mapa: © colaboradores de OpenStreetMap (ODbL). Uso de teselas según la política de tile.openstreetmap.org: pocas descargas, caché y atribución visible.
