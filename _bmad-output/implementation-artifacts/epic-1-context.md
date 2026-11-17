# Epic 1 Context: Explorar Sanlúcar y ver sus precios en el mapa

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Cualquier usuario, sin cuenta y sin conexión tras la primera carga, abre la app en Android y ve el mapa real de Sanlúcar en 2.5D con la paleta del proyecto, los lugares con precios con su logo, el filtro "Precios", su posición y la ficha de cada lugar con los precios publicados en la web del negocio, su origen, su frescura y sus variantes. La épica también pone la base técnica sobre la que crecen las demás: el proyecto Unity existente reorganizado en capas hexagonales, una raíz de composición, la retirada de Cesium y de las escenas de prueba, y los lugares base empaquetados.

## Stories

- Story 1.1: Abrir la app y ver el mapa sobre la nueva estructura del proyecto
- Story 1.2: Mapa con la paleta, el tema claro u oscuro y los gestos
- Story 1.3: Calcular el precio, su origen, su frescura y sus variantes
- Story 1.4: Lugares reales de Sanlúcar con su logo en el mapa
- Story 1.5: Filtro "Precios" en el mapa
- Story 1.6: Puntos de interés de referencia en el mapa
- Story 1.7: Ver mi posición en el mapa
- Story 1.8: Ficha del lugar con sus precios

## Requirements & Constraints

- Mapa real (calles, agua, zonas verdes, edificios en volumen, nombres de calles) con la paleta del tema activo; mover, ampliar, girar e inclinar con uno y dos dedos; ≥ 30 fps en un Android de gama media de 2021 (4 GB RAM); zona visible en < 3 s con 4G.
- Lo ya visto se dibuja sin conexión desde la caché de teselas; una zona nunca vista muestra el color de fondo, nunca una pantalla en blanco.
- Tema claro (paleta B) y oscuro (paleta D); el inicial sigue al sistema; mapa e interfaz cambian a la vez.
- Filtro "Precios": Ninguno (por defecto, el control dice "Ver precios"), Todos, Servicios locales, Hostelería, Ocio; la elección se recuerda.
- Marcadores con logo del tipo, color de plantilla, insignia € si tiene precio (atenuado si no); fijos en su coordenada (< 1 px de desvío) al mover, girar e inclinar. Centros sanitarios públicos sin precio.
- Puntos de interés de OpenStreetMap con icono y nombre, distinguibles de los lugares con precio; sin paradas de autobús ni oficinas; si un POI ya es lugar de la app, solo se ve el lugar.
- Posición del usuario (círculo y cono con brújula) y botón para centrar (segundo toque orienta al norte). Sin ruta y en segundo plano, nunca se usa el GPS.
- Ficha: nombre, tipo, dirección, conceptos agrupados por área; precio fijo, rango por persona o "desde"; "Consultar" cuando la web no da importe; origen con página y fecha; punto de frescura; variantes con la vigente destacada; "Aún no tiene precio" sin inventar nada. Fila de acciones reservada bajo la cabecera.
- Ningún precio sin origen ni inventado: solo los de la web oficial del negocio.
- Accesibilidad: contraste ≥ 4,5:1, zonas táctiles ≥ 48 dp, TalkBack en mapa y ficha, botones de zoom como alternativa a los gestos, "reducir animaciones" respetado.
- Textos en castellano, preparados para otros idiomas. Solo vertical. APK sin Google Play, Android minSdk 26 / targetSdk 35.

## Technical Decisions

- **Proyecto brownfield:** `unity/PriceMap` (Unity 6000.3.28f1, render integrado) con el mapa vectorial existente (`LectorMVT`, `Triangulador`, `MapaVectorial`, `Geo`, `CamaraMapa`, `Marcador`), teselas MVT de OpenFreeMap (zoom 14) y caché propia en `persistentDataPath/vectorial`.
- **Capas con `.asmdef`:** `PriceMap.Dominio` (C# puro, `noEngineReferences: true`, no referencia nada del proyecto) ← `PriceMap.Aplicacion` (servicios y puertos) ← adaptadores `PriceMap.Adaptadores.*` y `PriceMap.Vectorial` (implementan puertos) ; `PriceMap.UI` solo ve Aplicación y Dominio ; `PriceMap.Arranque` ve todo. Pruebas EditMode del Dominio en `Assets/Tests/EditMode/`.
- **Raíz de composición única** (`Arranque`): crea adaptadores y servicios y los entrega por constructor/inicialización. Sin singletons ni localizadores para lógica.
- **Mapa solo por el puerto `IMapa`** (tema, marcadores, ruta, posición, centrar, toque y pulsación larga). Dominio y Aplicación usan lat/lon `double`; `Vector3` solo dentro del adaptador del mapa.
- **Precio derivado:** el Dominio calcula precio mostrado, origen y frescura al leer, por concepto y variante; nunca se guarda. Recibe "ahora" como parámetro.
- **Importes en céntimos enteros** (`int`); solo la Presentación formatea en es-ES.
- **Ids estables en texto:** `osm:<tipo>:<id>` o slug (`clinica-areas`, `chemabarber`); conceptos y variantes con slug (`fisioterapia/sesion`, `superinductiva@con-bono`).
- **Parámetros de negocio** (umbrales de frescura, etc.) solo en `ParametrosNegocio` del Dominio.
- **Frescura:** hostelería al día < 30 días, podría haber cambiado 30-60, antiguo > 60; servicio local y ocio < 3 meses, 3-6, > 6.
- **Datos base empaquetados** en `Assets/Datos/` (JSON), migrados de `prototipo/datos-lugares.js` (121 lugares) y `prototipo/datos-precios-web.js` (tarifas con página y fecha).
- **UI:** UI Toolkit; la UI se suscribe a estado/eventos de los servicios y envía comandos, no lee ficheros; colores y medidas solo desde variables USS con los tokens de DESIGN.md; textos con el paquete Localization (`es-ES`); `AccessibilityHierarchy` por pantalla.
- **Convenciones:** código en castellano, sin tildes en identificadores; un tipo público por fichero; carpetas = namespaces bajo `Assets/Scripts/`; `Resultado<T>` entre capas; `async`/`Task`; `Debug.Log` con prefijo `PriceMap:` solo en adaptadores y arranque.

## UX & Interaction Patterns

- Tokens de DESIGN.md (paletas B y D, colores de plantilla, frescura, mapa), tipografía Varela Round (títulos) y Nunito Sans (texto) en sp, espaciado 4/8/12/16/24/32, radios `xs` a `pill`; con fuente al 200 % nada se corta.
- Píldora "Precios" arriba a la izquierda con menú de 5 opciones; botones redondos (tema, ubicación).
- Marcador de 42 px con color de plantilla, logo, insignia €; sin precio, atenuado. Iconos vectoriales, nunca emojis.
- Ficha que sube desde abajo; el mapa centra el lugar en la zona no tapada; el foco va al título al abrirla y vuelve al marcador al cerrarla con el botón atrás.
- Las maquetas de `ux-designs/.../mockups/` y las capturas del prototipo son referencia visual; si discrepan, mandan DESIGN.md y EXPERIENCE.md.

## Cross-Story Dependencies

- 1.1 es la base de todas: estructura de ensamblados, `Arranque`, puerto `IMapa` y build de Android.
- 1.3 (Dominio del precio) la usan 1.4 (insignia €, anuncio de TalkBack) y 1.8 (ficha).
- 1.4 (datos base y marcadores) la usan 1.5 (filtro), 1.6 (deduplicar POI) y 1.8 (ficha).
- La fila de acciones de la ficha la rellenan las épicas 2 (Ir), 3 (favorito), 7 (avisar, avísame), 8 (¿Es tu negocio?, etiqueta "%") y 9 (plan).
