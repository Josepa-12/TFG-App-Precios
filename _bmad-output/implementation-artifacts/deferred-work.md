- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-abrir-la-app-y-ver-el-mapa-sobre-la-nueva-estructura.md`
  summary: Las teselas del mapa se cargan alrededor de un origen fijo (MapaVectorial.latitud/longitud); centrar el mapa lejos de ese origen muestra el suelo vacío, y la vista inicial está repetida en PantallaMapa.VistaInicial.
  evidence: MapaVectorial.Start carga 3×3 teselas alrededor del origen y Centrar solo mueve la cámara. Encaja en la historia 1.2 (gestos y caché) o en la 1.7 (centrar en mi posición), que necesitan cargar teselas según la vista.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-2-mapa-con-la-paleta-el-tema-claro-u-oscuro-y-los-gestos.md`
  summary: Segunda parte de la historia 1.2: inclinar el mapa con dos dedos (arrastre vertical), cargar y liberar teselas alrededor de la vista con la caché existente, y comprobar los 30 fps y el uso sin conexión en el móvil.
  evidence: El usuario pidió dividir la historia 1.2 el 17/11/2026 por tamaño (unos 2.100 tokens). Esta parte va justo después de la base de interfaz y el tema, y antes de cerrar la historia 1.2. Absorbe el pendiente de teselas de la historia 1.1.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-2b-gestos-y-teselas-alrededor-de-la-vista.md`
  summary: Extraer de CamaraMapa.Tactil el paso que aplica el gesto (inclinación, zoom y giro) a un método probable y probarlo (signo de la inclinación, sin zoom ni giro al inclinar).
  evidence: Tactil lee Input.GetTouch directamente. Hoy solo se prueba el clasificador puro y el resto se comprueba en el móvil.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Segunda parte de la historia 1.4. Los marcadores de los lugares sobre el mapa. La UI pinta cada lugar en una capa de UI Toolkit y lo coloca con `IMapa.Proyectar`, en `LateUpdate` después de la cámara (desvío < 1 px). Cada marcador lleva el icono vectorial de su tipo (glifos de `prototipo/lugares.js` dibujados con `Painter2D`), el color de su plantilla y la insignia € (sin precio, atenuado a 32 dp y opacidad 0,7). TalkBack lo anuncia: "Clínica Areas, clínica privada, desde 20 €". También se retiran los 4 marcadores fijos de `Principal.unity`.
  evidence: El usuario pidió dividir la historia 1.4 el 17/11/2026 por tamaño (unos 2.800 tokens). Esta parte va justo después de los datos base (parte 1) y usa su `ServicioLugares` y `ResumenLugar`.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Probar que la raíz de composición carga los lugares (hoy "PriceMap: 124 lugares cargados" solo se comprueba a mano en la build de PC).
  evidence: Revisión de la 1.4 (parte 1), Verification Gap. `PruebasArranque` solo prueba `Componer`. En la parte 2, `ServicioLugares` llegará a `Componer` y se podrá comprobar ahí.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Pruebas automáticas del script `exportar-datos-unity.js` (tarifas de un lugar que no existe, ids repetidos, plantillas desconocidas).
  evidence: Revisión de la 1.4 (parte 1). No hay arnés de pruebas de Node y los casos de error solo se comprobaron a mano con una copia aislada. Conviene hacerlo cuando `WEB_PRICES` tenga más negocios.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4b-marcadores-de-los-lugares-en-el-mapa.md`
  summary: Probar con TalkBack en un Android real que los marcadores se anuncian y que sus nodos aparecen y desaparecen al mover el mapa. También comprobar a ojo, al arrastrar, que los marcadores no van un fotograma por detrás.
  evidence: Revisión de la 1.4 (parte 2), Verification Gap. Con el lector apagado, `AnadirMarcas` y `ActualizarMarcadores` no se ejecutan. No hay pruebas PlayMode del orden Update/LateUpdate. La captura de PC solo mide con la cámara quieta (0,5 px).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-filtro-precios-en-el-mapa.md`
  summary: Segunda parte de la historia 1.5: el menú "Precios" con TalkBack y la captura de comprobación.
    - Con el menú abierto, las 5 opciones son botones de TalkBack y la elegida lleva el estado "seleccionado" (`AccessibilityState.Selected` o valor "seleccionado").
    - Al elegir se anuncia "Precios en el mapa: Hostelería".
    - Al cerrarse el menú, el foco vuelve a la píldora.
    - El botón atrás de Android (`KeyCode.Escape`) cierra el menú sin cambiar el filtro.
    - Captura "filtro" de PC con el menú abierto y Hostelería elegida.
  evidence: El usuario pidió dividir la historia 1.5 el 17/11/2026 por tamaño (unos 2.700 tokens). La parte 1 deja la píldora, el menú básico, el filtro y la preferencia.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-filtro-precios-en-el-mapa.md`
  summary: Probar con toques reales (en un panel o en el móvil) que la píldora abre el menú, que una opción filtra y que tocar fuera lo cierra (los `Clickable` del velo, la píldora y las opciones).
  evidence: Revisión de la 1.5 (parte 1), Verification Gap. Las pruebas EditMode llaman a `AlternarMenu`, `CerrarMenu` y `Pulsar` directamente, porque no hay panel para enviar eventos de puntero. Hasta ahora solo lo comprobó un clic accidental en la captura de PC.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5b-menu-precios-con-talkback-y-boton-atras.md`
  summary: Comprobar en un Android real que el botón atrás cierra el menú "Precios" y que, con el menú cerrado, no saca de la app. También comprobar que TalkBack lee el anuncio y lleva el foco a la píldora al elegir.
  evidence: Revisión de la 1.5 (parte 2), Verification Gap. Las pruebas llaman a `Atras()` directamente, y `Update` con `KeyCode.Escape` no se prueba porque en EditMode no hay forma de simular el `Input` antiguo. `Input.backButtonLeavesApp` se queda en su valor por defecto (false).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-puntos-de-interes-de-referencia-en-el-mapa.md`
  summary: |
    Segunda parte de la historia 1.6:
    - **Repetidos:** ocultar el punto de interés que repite un lugar de la app con marcador visible (mismo nombre normalizado a menos de 80 m, id del elemento MVT, como `applyPoiDedup` en `prototipo/app.js:639-660`). Si el filtro oculta el lugar, el punto vuelve a verse.
    - **Solapes:** ocultar los puntos que se pisan entre sí. Gana el de menor `rank`.
    - **TalkBack:** el usuario lo decidió el 17/11/2026. Lee los puntos visibles después de los lugares de la app, con el formato "Mercadona, supermercado"; los sanitarios públicos, como "centro sanitario público".
    - **Captura:** captura de PC con un repetido.
  evidence: El usuario pidió dividir la historia 1.6 el 17/11/2026 por tamaño (unos 2.300 tokens). La parte 1 deja la lectura de la capa `poi`, las categorías y el dibujo.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Ignorar en git `unity/PriceMap/Assets/AddressableAssetsData/link.xml`, que cada build regenera.
  evidence: Se borró en `58e4bbb` y volvió a aparecer tras la build de PC de la verificación de la 1.4 (parte 1).
