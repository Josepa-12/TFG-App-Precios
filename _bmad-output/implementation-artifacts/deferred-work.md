- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-abrir-la-app-y-ver-el-mapa-sobre-la-nueva-estructura.md`
  summary: Las teselas del mapa se cargan alrededor de un origen fijo (MapaVectorial.latitud/longitud); centrar el mapa lejos de ese origen muestra el suelo vacío, y la vista inicial está repetida en PantallaMapa.VistaInicial.
  evidence: MapaVectorial.Start carga 3×3 teselas alrededor del origen y Centrar solo mueve la cámara. Encaja en la historia 1.2 (gestos y caché) o en la 1.7 (centrar en mi posición), que necesitan cargar teselas según la vista.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-2-mapa-con-la-paleta-el-tema-claro-u-oscuro-y-los-gestos.md`
  summary: Segunda parte de la historia 1.2: inclinar el mapa con dos dedos (arrastre vertical), cargar y liberar teselas alrededor de la vista con la caché existente, y comprobar los 30 fps y el uso sin conexión en el móvil.
  evidence: El usuario pidió dividir la historia 1.2 el 17/11/2026 por tamaño (unos 2.100 tokens). Esta parte va justo después de la base de interfaz y el tema, y antes de cerrar la historia 1.2. Absorbe el pendiente de teselas de la historia 1.1.
