---
title: 'Historia 1.1: Abrir la app y ver el mapa sobre la nueva estructura del proyecto'
type: 'refactor'
created: '2026-11-17'
status: 'done'
baseline_commit: 'dfe1c1e53b058efdb2879ab368a33e310067a36d'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `unity/PrecioLocal` es una prueba técnica: todo el código está en un único ensamblado, conviven tres escenas (Mapa, MapaCesium y MapaVectorial) y depende de Cesium. Con esta base no se pueden aplicar las reglas de capas de la arquitectura (AD-1, AD-2) ni generar una APK limpia.

**Approach:** Reorganizar el proyecto en las capas hexagonales con `.asmdef`. El mapa vectorial pasa a `PriceMap.Vectorial` sin cambiar su comportamiento y se expone por un puerto `IMapa` mínimo. Se quitan Cesium y las escenas A y B. Una escena principal con un único componente `Arranque` compone la app. Se deja configurada la compilación para Android (APK, minSdk 26, solo vertical).

## Boundaries & Constraints

**Always:** Dominio con `noEngineReferences: true` y sin referencias a otros ensamblados. UI solo referencia Aplicación y Dominio. Arranque es el único que crea y conecta piezas, sin singletons ni localizadores. Al mover ficheros se conservan sus `.meta`, para que la escena no pierda referencias. Código en castellano, sin tildes en los identificadores y con un tipo público por fichero.

**Decisiones del usuario (17/11/2026):** la app se llama **PriceMap**, así que `productName = PriceMap` y el identificador Android es `com.pricemap.app`. El mapa se hace con datos de OpenStreetMap, sin Cesium: el mapa vectorial ya los usa a través de las teselas de OpenFreeMap. Todo el proyecto pasa a llamarse PriceMap: la carpeta `unity/PrecioLocal` → `unity/PriceMap`, los namespaces y ensamblados `PrecioLocal.*` → `PriceMap.*`, los shaders `PriceMap/*`, el menú del editor `PriceMap` y el prefijo de los logs `PriceMap:`. Los documentos de planificación vigentes ya están actualizados. La especificación se mantiene completa aunque supera los 1.600 tokens.

**Never:** No se cambia lo que hace el mapa vectorial: teselas, caché, paleta, sombras, tecla T, atribución y marcadores de ejemplo siguen igual. Sin UI Toolkit, Localization ni Firebase, que llegan en historias posteriores. No se toca `prototipo/`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Arranque normal | Escena principal en Play o en la APK | Arranque crea el mapa, lo entrega a la UI y la UI centra en la Plaza del Cabildo; se ve Sanlúcar con atribución | N/A |
| Falta una pieza | Escena sin `MapaVectorial` o sin `PantallaMapa` | Log `PriceMap:` con la pieza que falta y no lanza excepción | Arranque se desactiva |
| Dependencia prohibida | Un `.asmdef` referencia una capa no permitida | La prueba EditMode de capas falla nombrando el ensamblado | N/A |

</frozen-after-approval>

## Code Map

- `Assets/Scripts/Vectorial/{LectorMVT,Triangulador,MapaVectorial}.cs`: mapa vectorial (opción C). Se reutiliza tal cual. `MapaVectorial` usa el shader `PriceMap/CesiumEdificiosPaleta` en `CrearMateriales()`.
- `Assets/Scripts/Geo.cs`, `CamaraMapa.cs`, `Marcador.cs`: namespace `PriceMap` y usan `Vector3`, así que pertenecen al adaptador del mapa (AD-13). `CamaraMapa.CentrarEn(lat, lon)` ya existe.
- `Assets/Scripts/MapaTeselas.cs`: es de la escena A, pero en sus líneas 162-179 están `TemaMapa` y `PaletaMapa`, que usa `MapaVectorial`. Hay que conservarlos.
- `Assets/Scripts/TemaCesium.cs`, `Assets/Editor/PrepararEscenaCesium.cs`, `Assets/CesiumSettings/`, `Assets/Scenes/{Mapa,MapaCesium}.unity`, `Assets/Shaders/{TeselaPaleta.shader,PaletaOSM.cginc}`, `Assets/Shaders/Resources/CesiumTerrenoPaleta.shader`, `Assets/Materiales/{Tesela,Suelo}.mat`: exclusivos de A y B. Se borran.
- `Assets/Shaders/Resources/CesiumEdificiosPaleta.shader`: lo usa el mapa vectorial. Se renombra, no se borra.
- `Assets/Editor/PrepararEscena.cs`: escena A, pero `PrepararEscenaVectorial` usa sus `Sitios`, `CrearPosicion` y `CrearSitio` (y los materiales `Blanco` y `Marcador *`).
- `Packages/manifest.json`: hoy Cesium trae como dependencias Input System (1.20.1) y Test Framework (1.6.0). `ProjectSettings`: Input "Both", minSdk 25, sin identificador, rotación libre.

## Tasks & Acceptance

**Execution:**
- [x] `unity/PrecioLocal` → `unity/PriceMap` (`git mv`; `Library/` se mueve con la carpeta). Renombrar todo `PrecioLocal` del código, de los shaders y de los menús a `PriceMap`.
- [x] `Packages/manifest.json`: quitar `com.cesium.unity` y su `scopedRegistries`; añadir `com.unity.inputsystem` 1.20.1 y `com.unity.test-framework` 1.6.0. Así no se pierden al irse Cesium.
- [x] Borrar los ficheros exclusivos de A y B del Code Map (con sus `.meta`).
- [x] `Assets/Shaders/Resources/CesiumEdificiosPaleta.shader` → `EdificiosPaleta.shader` (`git mv`), con `Shader "PriceMap/EdificiosPaleta"`. Actualizar `Shader.Find` y los comentarios que hablan de Cesium.
- [x] Mover con `git mv` `Geo.cs`, `CamaraMapa.cs` y `Marcador.cs` a `Assets/Scripts/Vectorial/` (namespace `PriceMap.Vectorial`). Sacar `TemaMapa.cs` y `PaletaMapa.cs` de `MapaTeselas.cs`. Quitar de `Marcador` lo referido a Cesium.
- [x] `Assets/Scripts/Dominio/`: `Coordenada.cs` (struct lat/lon `double`, inmutable) y `PriceMap.Dominio.asmdef`.
- [x] `Assets/Scripts/Aplicacion/`: puerto `IMapa.cs` (`void Centrar(Coordenada destino)`) y `.asmdef` → Dominio.
- [x] `Assets/Scripts/Vectorial/PriceMap.Vectorial.asmdef` → Aplicación y Dominio. `MapaVectorial` implementa `IMapa` y delega en `CamaraMapa.CentrarEn`.
- [x] `Assets/Scripts/Adaptadores/Datos/PriceMap.Adaptadores.Datos.asmdef` → Aplicación y Dominio. Queda vacío hasta la historia 1.4.
- [x] `Assets/Scripts/UI/`: `PantallaMapa.cs` (MonoBehaviour con `Inicializar(IMapa)` que centra en la Plaza del Cabildo, 36.7779 / -6.3528) y `.asmdef` → Aplicación y Dominio.
- [x] `Assets/Scripts/Arranque/`: `Arranque.cs` (busca el `MapaVectorial` y la `PantallaMapa` de la escena; si falta alguno, registra un log y se desactiva) y `.asmdef` → todos.
- [x] `Assets/Editor/`: `PrepararEscenaPrincipal.cs` sustituye a `PrepararEscena`, `PrepararEscenaVectorial` y `AbrirEscenaMapa`. Genera `Assets/Scenes/Principal.unity` (luz, cámara, mapa, marcadores de ejemplo, `Arranque`, `PantallaMapa`) como única escena del build. `ConfigurarAndroid.cs` aplica `productName` PriceMap, el identificador `com.pricemap.app`, minSdk 26, targetSdk 35, IL2CPP, ARM64 y solo vertical, y tiene un método de compilación por lotes `CompilarApk` que genera `Builds/PriceMap.apk`. `Editor.asmdef` solo para el editor. Borrar `MapaVectorial.unity`.
- [x] `Assets/Tests/EditMode/`: `.asmdef` de pruebas → Dominio. `PruebasCapas.cs` lee cada `.asmdef` de `Assets/Scripts` y comprueba las referencias permitidas de AD-1 (y que Dominio tiene `noEngineReferences`). `PruebasCoordenada.cs` comprueba la igualdad y el `ToString`.

**Acceptance Criteria:**
- Dado el proyecto, cuando Unity compila en modo por lotes, entonces no hay errores ni referencias a `CesiumForUnity`.
- Dado `Principal.unity` en Play, cuando carga, entonces se ve el mapa vectorial de Sanlúcar con atribución, igual que la antigua escena MapaVectorial.
- Dada la build de Android, cuando se ejecuta `CompilarApk`, entonces se genera una APK en vertical, con minSdk 26 y targetSdk 35, que se instala en un móvil real.

## Design Notes

`IMapa` empieza con un solo método a propósito. Cada historia le añade lo que necesita (tema, marcadores, posición…), para no diseñar por adelantado. Los marcadores de ejemplo de la escena se mantienen hasta que la historia 1.4 los sustituya por los datos reales.

## Verification

**Commands:**
- `"C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe" -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults <scratch>/resultados.xml -logFile <scratch>/tests.log`: todas las pruebas pasan.
- `... -batchmode -quit -projectPath unity/PriceMap -executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarApk -logFile <scratch>/build.log`: termina con código 0 y existe `Builds/PriceMap.apk`.

**Manual checks:**
- El usuario instala la APK en su móvil y ve el mapa en vertical.
