---
title: 'Historia 1.2 (parte 2): Inclinar con dos dedos y teselas alrededor de la vista'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: '49f9605c0b9a85fa6b2e36804480a0e7bcc7a5e2'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El mapa no se puede inclinar con los dedos. Además, solo carga las 3×3 teselas que rodean la Plaza del Cabildo: al moverse fuera de esa zona aparece el suelo vacío. Y si la app arranca sin conexión ni dirección de teselas guardada, no dibuja nada, aunque las teselas estén en la caché.

**Approach:**
- Añadir a `CamaraMapa` la inclinación con un arrastre vertical de dos dedos, decidido por una función pura que distingue entre inclinar y ampliar o girar.
- Hacer que `MapaVectorial` cargue las teselas alrededor del centro de la vista. Se piden a la red solo si no están ya en la caché de disco, y se liberan las que quedan lejos.
- Cerrar la historia 1.2 con la comprobación en el móvil de los 30 fps y del uso sin conexión.

## Boundaries & Constraints

**Always:**
- Se reutilizan la caché en `persistentDataPath/vectorial` y la construcción de mallas que ya existen.
- Al liberar una tesela se destruyen sus mallas, para no dejar memoria ocupada.
- Las funciones de decisión (gesto y teselas que cargar o liberar) son puras y tienen pruebas EditMode.
- Un gesto que empieza sobre un control de la interfaz sigue sin mover el mapa.

**Decisiones (17/11/2026):**
- La inclinación sigue la convención de Google Maps: subir los dos dedos inclina el mapa (más 3D) y bajarlos lo pone más cenital, dentro de los límites actuales (20° a 85°).
- Se cargan las teselas a distancia 1 del centro (3×3), o a distancia 2 si la cámara está alejada (más de 2.000 m). Se liberan las que pasan de distancia 3.
- Una tesela que falla, por no haber red ni caché, no se marca como cargada: se vuelve a intentar cuando cambia el centro de la vista.

**Never:** Sin teselas de más zoom que 14, sin cambiar el estilo del mapa y sin tocar la interfaz de la parte 1.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Inclinar | Dos dedos que se mueven en vertical en el mismo sentido, sin separarse ni girar | Cambia la inclinación (subir = más inclinado), sin zoom ni giro | Se respeta el límite de 20° a 85° |
| Ampliar o girar | Dos dedos que se separan, se juntan o giran | Zoom y giro como hasta ahora, sin inclinar | N/A |
| Gesto ambiguo | Movimiento vertical pequeño (< 2 px por fotograma) o dedos en sentidos opuestos | No es inclinación | N/A |
| Moverse por el mapa | El centro de la vista pasa a otra tesela | Se cargan las de alrededor que faltan, de la más cercana a la más lejana, y se liberan las lejanas | N/A |
| Sin conexión, zona vista | Teselas en la caché y sin red | Se dibujan desde la caché aunque no se pueda obtener la dirección de las teselas | N/A |
| Sin conexión, zona nueva | Sin red ni caché | Se ve el color de fondo del mapa, sin pantalla en blanco | Se vuelve a intentar al cambiar de tesela |

</frozen-after-approval>

## Code Map

- `unity/PriceMap/Assets/Scripts/Vectorial/CamaraMapa.cs`: en `Tactil()`, con dos dedos se calculan la distancia y el ángulo entre ellos (zoom y giro). `inclinacion` va de 20 a 85, y los límites se aplican en `Aplicar()`. Los gestos que empiezan sobre la interfaz ya se bloquean con `gestoSobreInterfaz`.
- `unity/PriceMap/Assets/Scripts/Vectorial/MapaVectorial.cs`:
  - `Start` obtiene la plantilla (de la red, o la guardada en `plantilla.txt`) y, si no la consigue, sale sin dibujar nada. Carga 3×3 teselas fijas.
  - `CargarTesela` lee la caché o descarga, y `Construir` crea un `GameObject` por tesela, colgado del mapa.
  - `Nombres` usa `nombresColocados` (un diccionario global) para no repetir el nombre de una calle a menos de 300 m.
- `unity/PriceMap/Assets/Scripts/Vectorial/Geo.cs`: `Tesela(lat, lon, zoom)` y `AGeo(Vector3)` convierten entre coordenadas reales y la escena. El pivote de la cámara está en coordenadas de la escena.
- `unity/PriceMap/Assets/Tests/EditMode/PruebasMapaVectorial.cs`: tiene una fixture con cámara y mapa sin `Start`.

## Tasks & Acceptance

**Execution:**
- [x] `Vectorial/GestoDosDedos.cs`: función pura `Clasificar(deltaA, deltaB, distAntes, distAhora, angAntes, angAhora)` que devuelve `Inclinar(dy)`, `ZoomGiro` o `Nada`.
- [x] `Vectorial/CamaraMapa.cs`: usa `GestoDosDedos`. `Inclinar` cambia `inclinacion` en `−dy × velocidad` y no aplica zoom ni giro en ese fotograma.
- [x] `Vectorial/PlanTeselas.cs`: función pura `Calcular(centro, cargadas, pendientes, radioCarga, radioLiberar)` que devuelve las teselas a cargar (ordenadas por distancia) y las teselas a liberar.
- [x] `Vectorial/MapaVectorial.cs`:
  - en `Update`, si cambia la tesela del pivote o el radio, aplica `PlanTeselas`;
  - una corrutina carga la cola de una en una;
  - un diccionario de teselas cargadas guarda su raíz, sus mallas y los nombres colocados en cada tesela;
  - al liberar una tesela se destruyen sus mallas y se quitan sus nombres de `nombresColocados`;
  - la plantilla se vuelve a pedir solo si hay que descargar y aún no se tiene;
  - las teselas en caché se cargan aunque no haya plantilla.
- [x] `Tests/EditMode/`: `PruebasGestoDosDedos` (las 3 filas de la matriz sobre gestos) y `PruebasPlanTeselas`:
  - al empezar carga 3×3 ordenadas por distancia;
  - al moverse una tesela, solo pide las 3 nuevas;
  - libera las que están a más de 3;
  - lo que está pendiente no se vuelve a pedir;
  - con radio 2 carga 5×5.

**Acceptance Criteria:**
- Dada la APK en un Android de gama media, cuando muevo, amplío, giro e inclino, entonces va a 30 fps o más (lo comprueba el usuario en el móvil).
- Dado que me alejo varios kilómetros arrastrando el mapa, cuando vuelvo, entonces el número de teselas cargadas nunca pasa de 7×7.
- Dado el ejecutable de Windows con `-captura`, cuando arranca, entonces se ve el mapa completo alrededor de la vista inicial, igual que en la parte 1.

## Verification

**Commands:**
- Pruebas EditMode por lotes: todas pasan.
- `ConfigurarAndroid.CompilarApk` y `CompilarPcPruebas`, y después el ejecutable con `-captura`: código 0 y capturas con el mapa completo.

**Manual checks:**
- En el móvil: inclinar con dos dedos, alejarse más de 6 km y ver que se carga el mapa, activar el modo avión en una zona ya vista y en una nueva, y comprobar los 30 fps (las opciones de desarrollador de Android pueden mostrar el uso de la GPU o se puede usar un medidor externo).
## Implementation Notes

- Implementado directamente en la sesión. Pasan las 54 pruebas EditMode y la APK se compila sin errores.
- **Recorrido en el ejecutable de Windows:** el mapa sale con 9 teselas a 900 m, 15 a 5,5 km al norte (desembocadura del Guadalquivir, agua) y 15 al volver. En una ejecución, la ventana recibió entrada de escritorio que alejó la cámara y se cargó el anillo extra (25 y 35 teselas), siempre por debajo de 49.
- **`MapaVectorial` para las pruebas:** expone `ActualizarTeselas`, `TeselasPendientes` y `SimularCargada` como `internal` (`InternalsVisibleTo` en `Vectorial/AssemblyInfo.cs`). `Liberar` usa `DestroyImmediate` fuera de Play.
- **Pruebas:** quedan en `PruebasGestosYTeselas.cs`, con dos clases: `PruebasGestoDosDedos` y `PruebasPlanTeselas`.

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | BH, EC, VG | Si una tesela falla y la vista no se mueve, nunca se reintenta, y el límite de 5 s de la plantilla hace fallar toda la cola de golpe | medium | → patch: tras un fallo, `reintentarEn` fuerza a planificar de nuevo a los 5 s aunque la vista siga quieta |
| 2 | BH, EC, VG | Una tesela que termina de cargarse cuando la vista ya se ha ido lejos se queda en memoria (pueden superarse las 7×7) | medium | → patch: se libera al terminar si está a más de `RadioLiberarEfectivo` |
| 3 | BH, EC, VG | `etiquetas` se llena de `TextMesh` destruidos | low | → patch: `Liberar` quita las etiquetas de la tesela |
| 4 | EC | Una excepción al leer o construir una tesela mata la corrutina para el resto de la sesión | medium | → patch: `try/catch` en la lectura de caché y en `Construir` |
| 5 | BH, EC | `radioLiberar` por debajo del radio de carga hace que las teselas se carguen y liberen sin parar | low | → patch: `RadioLiberarEfectivo = max(radioLiberar, radio + 2)` |
| 6 | BH | El gesto se vuelve a clasificar en cada fotograma y puede saltar de inclinar a zoom a mitad del movimiento | medium | → patch: el primer movimiento claro fija el modo hasta que se levantan los dedos |
| 7 | EC | Un dedo que se levanta con delta puede producir un paso falso | low | → patch: los fotogramas de `Ended`/`Canceled` no aplican el gesto |
| 8 | BH | La inclinación no tiene límite | false | `Aplicar()` limita `inclinacion` entre 20 y 85 en cada fotograma |
| 9 | VG | `MapaVectorial` (carga, liberación y anillo extra) solo se probaba con la función pura | medium | → patch: `ElMapaCargaAlrededorDeLaVistaYLiberaLoLejano` en `PruebasMapaVectorial` |
| 10 | VG | `CamaraMapa.Tactil` (signo de la inclinación, sin zoom al inclinar) sin probar | medium | → defer: lee `Input.GetTouch` directamente y habría que extraer el paso de aplicar. Se comprueba en el móvil, como dice la verificación |
| 11 | BH, EC | `CapturaPruebas` solo registra y no comprueba nada | low | → patch: `LogError` si hay más de 49 teselas, y la vuelta usa el origen del mapa |
| 12 | EC | Claim: las clases de prueba no se llaman como dice el spec | low | → patch: hay dos clases, `PruebasGestoDosDedos` y `PruebasPlanTeselas` |
| 13 | BH | Al liberar una tesela vecina, una calle puede quedarse sin nombre | low | Caso raro y arreglarlo exige reconstruir etiquetas → rechazado |
| 14 | EC | La cola conserva el orden antiguo al moverse | low | Solo afecta al orden de llegada, no al resultado → rechazado |
| 15 | EC | Sin histéresis en el anillo extra a 2.000 m | low | Planificar es barato y las teselas ya cargadas no se tocan → rechazado |
| 16 | EC | Umbrales en píxeles sin escalar por dpi | low | Son pequeños, se afinarán en el móvil → rechazado |
| 17 | BH | Faltan las pruebas en los bordes de los umbrales | low | Se afinarán con datos del móvil → rechazado |
| 18 | BH | Faltan la memoria y el spec en el diff | false | La memoria se actualizó (`docs/memoria-tfg.md`, 5.2) y el spec está en `implementation-artifacts`, fuera del diff de código |

Tras los parches: 54/54 pruebas, recorrido correcto en el ejecutable de Windows y APK compilada.
