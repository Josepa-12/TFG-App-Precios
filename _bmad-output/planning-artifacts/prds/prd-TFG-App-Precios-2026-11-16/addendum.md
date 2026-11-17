# Anexo técnico del PRD — PriceMap

Decisiones y contexto técnico que no son requisitos de producto, para arquitectura y desarrollo.

## Plataforma y motor
- **Unity 6.3 LTS** (6000.3.28f1), render integrado, compilando para **Android**. iOS descartado en la v1 porque no se puede compilar desde Windows.
- Proyecto existente: `unity/PriceMap` (rama `unity`).

## Mapa (prueba técnica 5.1)
- Elegida la **opción C**: teselas vectoriales de **OpenFreeMap** (formato Mapbox Vector Tile, esquema OpenMapTiles, zoom 14), leídas y dibujadas con código propio: `LectorMVT.cs`, `Triangulador.cs`, `MapaVectorial.cs`, `PlanoPaleta.shader`.
- Motivos frente a las opciones A (teselas de imagen recoloreadas) y B (Cesium for Unity): colores exactos de la paleta, ningún icono ajeno, edificios con patios, sin cuenta ni logo obligatorio, mismos datos que el prototipo web.
- Pendiente: zoom > 14 con sobremuestreo, nombres que sigan la curva de la calle, puntos de interés desde la capa `poi`.
- Atribución: "OpenFreeMap © OpenMapTiles · Datos © colaboradores de OpenStreetMap".

## Servicios
- **Rutas:** Valhalla (servidor público de FOSSGIS) con `shortest`, OSRM como reserva. En producción, servidor propio.
- **Búsqueda de direcciones:** Photon (komoot). En producción, servidor propio.
- **Cuentas:** Firebase Authentication con proveedor Google (sustituye la simulación del prototipo).
- **Datos compartidos:** [propuesta] Firebase Firestore (aportes, avisos, precios declarados, ofertas, progreso).
- **Notificaciones:** paquete Unity Mobile Notifications (notificaciones locales) para la pregunta una hora después con la app cerrada y para "Avísame si cambia", que se comprueba al abrir la app. Sin FCM: el proyecto usa el plan gratuito Spark, sin código en servidor (ver la arquitectura).
- **Voz:** plugin con el TextToSpeech de Android.
- **Enlaces compartidos:** intent filters de Android para `geo:` y enlaces de Google Maps; los cortos (`maps.app.goo.gl`) se resuelven siguiendo la redirección.

## Lógica portable del prototipo
- La lógica de `prototipo/app.js`, `extras.js`, `cuentas.js` y `lugares.js` (reglas de precio, frescura, reputación, plan, recompensas, parseo de enlaces, traducción de maniobras de Valhalla) se traduce a C#.
- Los 121 lugares de `datos-lugares.js` y las tarifas de `datos-precios-web.js` se migran como datos iniciales.

## Interfaz
- UI Toolkit de Unity (hojas de estilo tipo CSS). El desenfoque de "cristal" de la cápsula es la parte más costosa; puede simplificarse a un fondo translúcido.
- Accesibilidad: módulo de accesibilidad de Unity 6 con TalkBack.
