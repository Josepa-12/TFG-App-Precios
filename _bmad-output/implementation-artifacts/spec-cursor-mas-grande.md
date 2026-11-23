---
title: 'Cursor de la posición más grande (punto y flecha)'
type: 'feature'
created: '2026-11-22'
status: 'done'
route: 'dispatch'
baseline_commit: '61690ed245326963b8bdab61737b640cf1059758'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En el móvil, el círculo de la posición (18 dp) y la flecha de la navegación en coche (26 dp) se ven muy pequeños (prueba del usuario en el Redmi, 22/11/2026).

**Approach:** Agrandar el punto, la flecha y el cono de dirección cambiando sus tokens de tamaño y, si la opción elegida lo pide, dibujar la flecha sobre un disco, sin tocar cómo se colocan ni cómo giran.

## Boundaries & Constraints

**Always:**
- Decidido (usuario, opción B de la maqueta https://claude.ai/artifact/Tuzx6rS5uDYvfvYRjE3BkN): sin ruta y a pie, punto de 24 dp (borde blanco 4 dp) y cono de 100 dp; navegando en coche, flecha de 30 dp en el color de la flecha, sin borde, dentro de un disco blanco de 48 dp con sombra suave (la sombra apenas desplazada hacia abajo), como Apple Maps. El disco no gira; la flecha sí. Atenuado sin señal: el disco y la flecha juntos.
- El centro del punto y de la flecha sigue exactamente en la coordenada; el giro y la atenuación sin señal (`OpacidadSinSenal`) siguen igual.
- El borde blanco del punto (y el disco de la flecha) dejan el cursor visible sobre el mapa claro y el oscuro.
- `MargenDp` (margen fuera de pantalla en que aún se coloca) llega al menos a la mitad del cono nuevo.
- Lo que encuadra o sigue la cámara (zona de encuadre, punto de seguir) no cambia.

**Never:** cambiar colores, el comportamiento del cono, la chincheta ni los marcadores de los lugares.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Sin ruta | Posición conocida | Punto y cono con el tamaño nuevo, centrados en la coordenada |
| Navegando en coche | Rumbo conocido | Flecha con el tamaño nuevo, girada como hoy |
| Sin señal | 10 s sin lecturas | El cursor grande, atenuado como hoy |
| Borde de la pantalla | Coordenada a menos de medio cono del borde | Se sigue colocando (sin cortarse de golpe) |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `UI/Estilos/Tokens.uss` l. 142 (`--radio-posicion-punto` 9px), 162-164 (`--posicion-punto` 18px, `--posicion-cono` 80px, `--posicion-flecha` 26px) -- los tamaños.
- `UI/PantallaMapa.uss` l. 368-411 (`.marcador-posicion*`: borde del punto 3px, cono, flecha) -- estilos.
- `Scripts/UI/MarcadorPosicion.cs` (201 l.) -- `MargenDp` 40 (l. 20), `BordeFlecha` 2,5 (l. 26), `DibujarFlecha`/`DibujarCono` con Painter2D; el disco de la opción B se dibujaría aquí o como elemento con borde redondeado.
- `Tests/EditMode/PruebasFlechaYSenal.cs`, `PruebasUbicacion.cs` -- revisar pruebas que miden tamaños o el margen.
- `Scripts/Arranque/CapturaPruebas.cs` -- capturas `navegacion-flecha`, `navegacion-flecha-oscuro` y alguna sin ruta con el punto: se rehacen.

## Tasks & Acceptance

**Execution:**
- [x] `UI/Estilos/Tokens.uss`, `UI/PantallaMapa.uss` -- tamaños y bordes nuevos.
- [x] `Scripts/UI/MarcadorPosicion.cs` -- margen y, en B, el disco de la flecha.
- [x] `Tests/EditMode` -- pruebas de tamaño, centrado y margen.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dadas las capturas de PC, cuando se comparan con la maqueta elegida, entonces el punto y la flecha tienen sus tamaños.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Revisión 1 (23/11/2026). Edge Case Hunter: sin hallazgos.

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | (gap) Nada comprueba que el disco se esconde al dejar la flecha | gap | Solo se mira el disco al encenderse | patch: prueba flecha → sin flecha |
| 2 | `Flecha =new` | low | Errata del cambio | patch |
| 3 | `MargenDp` 50 copiado a mano del token del cono | low | Las pruebas comparan cada uno con un literal | patch: prueba `MargenDp == cono / 2` |
| 4 | La prueba del margen no mira el 50 exacto ni la derecha/abajo | low | Solo izquierda y arriba | patch |
| 5 | «El disco no gira» siempre se cumple | low | Nadie toca `Disco.style.rotate` | patch: con rumbo ≠ 0, la flecha gira y el disco no |
| 6 | Sin señal, la opacidad va por hijo: la flecha se ve más clara sobre el disco | low | El spec pide atenuar los dos juntos; se ve en la captura `sin-senal` | rechazado |
| 7 | Disco blanco en el tema oscuro; color prestado de `--color-posicion-borde` | false | Maqueta B aprobada en claro y oscuro con disco blanco | rechazado |
| 8 | Sin prueba de que la flecha girada cabe en el disco | low | 30·√2 ≈ 42 < 48; insignificante | rechazado |
| 9 | Ayudantes de lectura de tokens duplicados | low | Solo orden de pruebas | rechazado |
| 10 | Memoria del TFG, `CLAUDE.md` y capturas | low | Archivos de contexto; capturas al final | defer: al cerrar |
| 11 | Comentarios que aún hablan del borde blanco de la flecha | low | `MarcadorPosicion` y USS | patch: comentarios |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- Las capturas de PC las hace el orquestador al final (no lanzar `-captura`).
