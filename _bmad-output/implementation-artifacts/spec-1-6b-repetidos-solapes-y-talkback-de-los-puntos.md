---
title: 'Historia 1.6 (parte 2): puntos de interés sin repetidos ni solapes'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: '144a9a48777a0d5082b602a301eda1f2bfad7d7d'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tras la parte 1, en el centro de Sanlúcar los puntos de interés se amontonan y sus nombres se pisan. Además, un sitio que ya es lugar de la app sale dos veces: con su marcador y con su círculo (criterio 2 de la historia 1.6).

**Approach:**
- Una regla del Dominio decide si un punto repite un lugar de la app que se está viendo (mismo nombre normalizado a menos de 80 m, como `applyPoiDedup` del prototipo); si es así, se oculta.
- En cada fotograma, de los puntos que caben en pantalla se muestran primero los de menor `rank`. Los que pisan a uno ya mostrado se ocultan enteros.

## Boundaries & Constraints

**Always:**
- **Repetido** (lo decide la historia):
  - Mismo nombre normalizado (minúsculas, sin tildes, sin signos y sin espacios repetidos) y a menos de 80 m de un lugar cuyo **marcador está visible ahora**.
  - Si el filtro "Precios" oculta ese lugar, o se sale de la pantalla, el punto vuelve a verse.
  - Otra sucursal con el mismo nombre a más de 80 m sí se ve.
  - La distancia la calcula el Dominio (haversine), sin Unity.
- **Solapes:**
  - Solo entre puntos de interés.
  - La caja de cada punto es el círculo de 24 dp más su nombre: el ancho del nombre ya dispuesto o, si aún no tiene disposición, una estimación.
  - Orden: `rank` ascendente y, si hay empate, el id.
  - Un punto que pisa a otro ya mostrado se oculta entero (icono y nombre).
  - Los marcadores de lugar no participan: siempre se ven y quedan por encima.
- **Orden de actualización:** en cada fotograma, primero los marcadores de lugar (para saber cuáles se ven) y después los puntos.

**Never:**
- **No entra en esta parte, va a la parte 3:** que TalkBack lea los puntos.
- No se quitan los marcadores de lugar por solaparse.
- No se cambia el aspecto de los puntos.
- Sin tocar un punto (1.8 y épica 2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Repetido visible | Punto "Bar Cachón" a 20 m del marcador visible de Bar Cachón (filtro Todos) | Solo se ve el marcador | N/A |
| Lugar oculto por el filtro | El mismo, con el filtro en Ninguno | Se ve el punto | N/A |
| Otra sucursal | "Mercadona" a 300 m de un lugar "Mercadona" visible | Se ven los dos | N/A |
| Tildes y signos | Punto "BAR CACHÓN!" y lugar "Bar Cachón" a 10 m | Repetido: se oculta | N/A |
| Solape | Dos puntos con las cajas cruzadas, `rank` 3 y 10 | Se ve el de `rank` 3 | N/A |
| Sin solape | Dos puntos separados | Se ven los dos | N/A |
| Empate de `rank` | Cruzados y con el mismo `rank` | Se ve el de menor id | N/A |

</frozen-after-approval>

## Code Map

- `Scripts/Dominio/ReglaPuntosInteres.cs` (parte 1): añadir `NombreNormalizado(string)` y `EsRepetido(nombre, posición, lugaresVisibles)`, con un umbral de 80 m como constante documentada. `Coordenada` (Dominio) no tiene distancia: se añade `DistanciaMetros` (haversine).
- `Scripts/UI/CapaPuntosInteres.cs`: `Actualizar(escalaDp, anchoPx, altoPx)` (`:87`) proyecta y decide qué se ve. Ahí se añaden el filtro de repetidos (recibe los lugares visibles) y los solapes por `rank`. También `Vistas` (`:35`) y `Visibles` (`:106`).
- `Scripts/UI/PuntoInteresVista.cs`: `Punto` (`Rank`, `Id`, `Nombre`, `Posicion`), `Icono`, `Nombre` (Label), `Visible`, `Posicion`, `Mostrar` y `Colocar` (`:86-94`).
- `Scripts/UI/CapaMarcadores.cs`: `Marcadores[i].Visible` y `.Lugar` (`Nombre`, `Posicion`) dan los lugares visibles.
- `Scripts/UI/PantallaMapa.cs` `ActualizarCapas` (`:347`): hoy actualiza los puntos antes que los marcadores. Hay que invertir el orden y pasar los lugares visibles.
- `prototipo/app.js:639-660` (`applyPoiDedup`, `normTxt`): referencia.
- `Scripts/Arranque/CapturaPruebas.cs`, captura "puntos": registrar en el log cuántos puntos se ocultan por repetidos y cuántos por solapes, y cuántos pares de puntos visibles se solapan (debe ser 0).

## Tasks & Acceptance

**Execution:**
- [x] `Dominio`: nombre normalizado, distancia y regla de repetido.
- [x] `UI/CapaPuntosInteres.cs`: repetidos y solapes por `rank` en `Actualizar`.
- [x] `UI/PantallaMapa.cs`: orden de actualización y lugares visibles.
- [x] `CapturaPruebas.cs`: recuentos y comprobación de solapes en el log.
- [x] `Tests/EditMode/PruebasPuntosInteres.cs`: cada fila de la matriz.

**Acceptance Criteria:**
- Dada la build de PC, cuando se abre `puntos.png`, entonces los nombres de los puntos no se pisan y ningún sitio sale a la vez como lugar y como punto.
- Dado el log de la captura, cuando se lee, entonces hay 0 pares de puntos visibles que se solapan y algún punto oculto por solape.

## Implementation Notes

- Implementado por un subagente.
  - Pasan 232/232 pruebas EditMode.
  - En la captura de PC del centro, de 67 puntos con categoría se ven 28: 21 se ocultan por repetidos y 18 por solapes. Hay 0 pares solapados, ningún punto visible repite un lugar visible y el log no tiene errores.
  - `puntos.png` se ve limpio. Algún nombre queda medio tapado por un marcador de lugar, lo que el spec permite (los marcadores no participan).
- La caja del nombre usa la medida real cuando ya tiene disposición y la guarda para cuando está oculto. Mientras no tiene disposición, usa una estimación.
- Se adaptó `LejosNoSeVeNingunPunto`: tenía dos puntos en el mismo píxel, que ahora se solapan por diseño.
- Tras los parches de la revisión:
  - Los parches se escribieron con el editor abierto (lo pidió el usuario) y se verificaron al cerrarlo: 237/237 pruebas EditMode y la build de PC correcta.
  - Captura con 46 puntos visibles, 29 ocultos por repetidos y 25 por solapes. No hay ningún par solapado, ningún punto repite un lugar y el log no tiene errores.
  - Los nombres normalizados se calculan una vez; el orden por importancia es un delegado estático.
  - La estimación del nombre es de 0,65 em por la escala de fuente, y las medidas guardadas se olvidan cuando cambia la escala.
- En el sprint, la 1.6 sigue en `in-progress` hasta la parte 3 (TalkBack de los puntos).

## Spec Change Log

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | BH, EC | `EsRepetido` normaliza nombres en cada fotograma, con cadenas, `StringBuilder` y enumeradores nuevos: basura que el móvil tiene que recoger | medium | Se ejecuta por punto y por fotograma, y en el centro siempre hay un lugar a menos de 80 m → patch: normalizar una vez y reutilizar |
| 2 | VG | La prueba de empate de `rank` pasa aunque se quite la comparación por id | medium | El orden de los nombres coincide con el de los ids → patch: invertirlos |
| 3 | BH, EC | La medida guardada del nombre no se olvida al cambiar la escala de fuente, y la estimación inicial se queda corta | low | Un punto puede quedar oculto para siempre o solaparse un fotograma → patch: olvidar las medidas al cambiar la escala y una estimación conservadora |
| 4 | BH | Faltan casos: cascada, exactamente 80 m y lugar sin nombre | low | Pruebas baratas → patch |
| 5 | BH | Las medidas de la caja (24, 14, 96 dp) están duplicadas en el USS | low | Un cambio de estilo rompería los solapes sin avisar → patch: prueba que compara los dos |
| 6 | BH, EC | La captura solo avisa (warning) si no hay ningún solape, que es un criterio de aceptación | low | Arreglo directo → patch: `LogError` |
| 7 | VG | La medida real del nombre no se usa en ninguna prueba | low | Gap ya verificado, disposición defer: hace falta un panel con disposición → deferred |
| 8 | BH | Prototipo: comparar con `name:es` antes que con `name` | false | `PuntoInteres.Nombre` ya es `name:es` o `name` (parte 1) |
| 9 | EC | `FormD` no separa ß, æ, ø ni ł | low | En Sanlúcar no aparecen → rechazado |
| 10 | BH | Comprobación de la captura frente a la regla de la capa (tolerancia de 0,25 dp) | low | Es una herramienta manual; la regla tiene sus pruebas → rechazado |
| 11 | BH | `LugaresVisibles` se construye aunque estemos lejos | low | Coste mínimo, y lo cubre el arreglo de #1 → rechazado |
| 12 | BH | Faltan los registros del proyecto | false | Se completan al cerrar |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC y `PriceMap.exe -captura <carpeta>`: `puntos.png` limpio y el log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.
