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
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Ignorar en git `unity/PriceMap/Assets/AddressableAssetsData/link.xml`, que cada build regenera.
  evidence: Se borró en `58e4bbb` y volvió a aparecer tras la build de PC de la verificación de la 1.4 (parte 1).
