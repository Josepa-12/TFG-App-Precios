---
title: 'Ajuste visual: controles del mapa como el prototipo'
type: 'bugfix'
created: '2026-11-18'
status: 'done'
baseline_commit: '04405c73d8000adbc4c9a2548d2cb98558f1a2d4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La píldora "Precios", su desplegable y los botones de tema y ubicación (historias 1.2, 1.5 y 1.7) no se ven como en el prototipo. Además, la captura de PC simula una pantalla de 900 dp de ancho: los controles parecen diminutos y no se pueden comparar con el prototipo.

**Approach:** Igualar esos controles al prototipo (`prototipo/styles.css`), según la maqueta que el usuario aprobó el 18/11/2026 (https://claude.ai/artifact/HXVbUfKb75jGH9dowSZQUE), y hacer que la captura de PC tenga el ancho de un móvil (412 dp).

## Boundaries & Constraints

**Always:**
- **Desplegable** (prototipo `.cat-menu`, `styles.css:141-148`):
  - 220 dp de ancho, `padding` 6 dp, esquinas de 18 dp, fondo `surface`, **sin borde** y con sombra suave.
  - Título "MOSTRAR PRECIOS EN EL MAPA": en mayúsculas (el texto de Localization no cambia; se pasa a mayúsculas al mostrarlo), 11 px, peso 800, espaciado 0,05 em, color `muted`, `padding` 8/12/4 dp.
  - Opciones: 48 dp de alto como mínimo (no baja a 44), esquinas de 12 dp, peso 700, 14 px.
  - La elegida se marca solo con el fondo `surface-2`, **sin check**. TalkBack mantiene el estado "seleccionado".
- **Píldora** (`.pill.glass`): fondo `glass`, borde de 1 dp en `glass-edge` (casi invisible) y la misma sombra suave.
- **Tema y ubicación** (`.ctrl-stack`, `styles.css:152-154`):
  - Una sola columna de cristal arriba a la derecha (16 dp desde arriba y 12 dp desde la derecha), con esquinas de 16 dp, fondo `glass`, borde `glass-edge` y sombra.
  - Dentro, dos huecos de 48 × 48 dp separados por una línea de 1 dp en `border`, con iconos de 22 dp en color de texto de acento (`#2F5BEA` en claro, `#8FB0FF` en oscuro).
  - Cada hueco conserva su comportamiento, sus estados, su etiqueta de TalkBack y su foco visible.
  - El icono de ubicación pasa de la diana a la flecha de navegación del prototipo (`polygon 3,11 22,2 13,21 11,13`). Sus estados (buscando, desactivado, al norte) siguen marcándose como ahora.
- **Botones de zoom** (solo con TalkBack): usan el mismo estilo de columna, debajo.
- **Sombra:** UI Toolkit no tiene `box-shadow`. Se dibuja una sombra suave equivalente, parecida a `0 4px 16px` con un 16 % de opacidad en claro y un 45 % en oscuro, con la técnica que permita Unity 6000.3: el filtro de USS si existe o un dibujo propio. La misma para los tres controles.
- **Captura de PC:** solo en modo `-captura`, el panel mide 412 dp de ancho (`PlayerSettings.defaultScreenWidth` ya lo pretende). En el móvil la escala sigue siendo 1 px = 1 dp × dpi/160.
- **Contraste:** el de los textos y los iconos sobre cristal sigue llegando a 4,5:1 (`PruebasContraste`).

**Never:** sin cambiar los tamaños en dp del móvil; sin cambiar el comportamiento de los controles; sin añadir el altavoz, el botón € ni la cápsula del buscador (historias 2.8, 4.3 y 2.2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Menú abierto | Hostelería elegida | Título en mayúsculas; Hostelería con fondo `surface-2` y sin check; ninguna opción con borde | N/A |
| Menú sin borde | Estilo resuelto del menú | Borde 0, ancho 220 dp, esquinas 18 dp, sombra presente | N/A |
| Columna | Pantalla normal | Un contenedor con los botones de tema y ubicación, 48 × 96 dp, separador entre ellos | N/A |
| Icono de ubicación | Botón de ubicación | Dibuja la flecha; los estados siguen cambiando su aspecto | N/A |
| TalkBack | Lector forzado | Mismo recorrido y etiquetas que antes; la opción elegida, "seleccionada" | N/A |
| Captura | `-captura` | Panel de 412 dp de ancho (en el log) | N/A |

</frozen-after-approval>

## Code Map

- `Assets/UI/PantallaMapa.uss`:
  - `.boton-redondo` (l.47-66), `.pildora-precios` (l.78-117), `.menu-precios` (l.132-147) y `.opcion-precios` (l.149-186, check en l.176-186).
  - Tokens en `Assets/UI/Estilos/Tokens.uss`: `--color-glass`, `--color-control-edge`, `--radio-*`, `--zona-tactil-min`; los nuevos (`--color-glass-edge`, sombra) van aquí.
- `Assets/Scripts/UI/BotonRedondo.cs`: `Icono` (l.7) y `Dibujar`. Añadir `Icono.Navegar` (flecha) y una variante "en columna" sin fondo ni borde propios.
- `Assets/Scripts/UI/PantallaMapa.cs`:
  - Creación de los botones: l.84 (`Icono.Diana` → `Icono.Navegar`) y l.94-98. Hay que meter tema y ubicación en un contenedor `columna-controles` (y zoom en otro), sin cambiar su orden de TalkBack ni `TapaPunto`/`EsControl`.
- `Assets/Scripts/UI/MenuPrecios.cs` y `OpcionPrecios.cs`: quitar el check y poner el título en mayúsculas al mostrarlo.
- `Assets/UI/PantallaMapa.uxml`: hueco de la columna, si conviene.
- `Assets/Scripts/Arranque/CapturaPruebas.cs` (+ `Arranque`): en modo `-captura`, panel de 412 dp de ancho, p. ej. ajustando el `PanelSettings` (`referenceDpi` = `Screen.dpi` × 412 / `Screen.width`) o con escala de pantalla. `PantallaMapa.EscalaDp` (l.618) se calcula con `raiz.layout.width / Screen.width`, así que se adapta solo. Comprobar que las capturas de centrado y toque siguen bien.
- **Pruebas que cambian a propósito:**
  - `PruebasFiltro` exige el check en la elegida (matriz de la 1.5: "Hostelería con check").
  - Las pruebas que leen `.boton-redondo` o `Icono.Diana` (`PruebasPantallaMapa`, `PruebasUbicacion`) también cambian.
- Referencia: `prototipo/styles.css:130-154` y `prototipo/index.html:21-37`.

## Tasks & Acceptance

**Execution:**
- [x] `Tokens.uss` y `PantallaMapa.uss`: cristal, sombra, menú, opciones, píldora y columna.
- [x] `BotonRedondo.cs`, `PantallaMapa.cs` y `PantallaMapa.uxml`: columna de controles y flecha de ubicación.
- [x] `MenuPrecios.cs` y `OpcionPrecios.cs`: título en mayúsculas y sin check.
- [x] `CapturaPruebas.cs`: panel de 412 dp en modo captura.
- [x] `Tests/EditMode`: una prueba por fila de la matriz y ajustar las que esperaban el diseño anterior.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abren `claro.png`, `oscuro.png` y `filtro.png`, entonces la píldora, el menú y la columna se ven como la propuesta de la maqueta, a escala de un móvil de 412 dp, y el log no tiene errores.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | La sombra y el borde de cristal de `SombraSuave` no se comprueban al dibujar: con un nombre de propiedad mal escrito no saldrían y ninguna prueba fallaría (VG, BH) | medium | Las pruebas solo miran la clase y el texto del USS; en EditMode no se resuelven estilos. | patch |
| 2 | Las clases de esquina y separador de la columna no se comprueban en los botones (VG) | low | Ninguna prueba mira `ClaseSeparado` ni `--arriba`/`--abajo` en los botones reales. | patch |
| 3 | La prueba del orden de TalkBack no lleva botón de ubicación (BH) | low | Monta la pantalla sin servicio de ubicación; no ve tema → ubicación → acercar. | patch |
| 4 | `-captura` cambia el `PanelSettings` compartido (ECH) | low | En el editor el cambio quedaría en el asset; se corrige con una copia. | patch |
| 5 | `.t-caption` perdió el espacio tras `{` en `Tokens.uss` (BH) | low | Cambio de formato sin querer. | patch |
| 6 | Falta un caso "al norte" en la prueba de estados del botón (ECH) | low | La etiqueta del estado norte no se comprueba con la nueva columna. | patch |
| 7 | El borde `glass-edge` no llega a 3:1 (WCAG 1.4.11) (BH) | low | Es el borde aprobado del prototipo; el control se reconoce por su icono y su sombra. | rechazado |
| 8 | La fila "los estados siguen cambiando su aspecto" no se cumple (BH) | false | Los estados del botón de ubicación nunca cambiaron su dibujo, solo su etiqueta (1.7b); no hay regresión. | rechazado |
| 9 | TalkBack leería el título en mayúsculas (BH) | false | El título del menú no está en el recorrido de TalkBack (`MenuPrecios.cs:33`, sin registro en `AccesibilidadPantalla`). | rechazado |
| 10 | `SombraSuave` crea arrays en cada repintado (BH) | low | Solo repinta al cambiar la geometría o el tema, no cada fotograma. | rechazado |
| 11 | La sombra de una columna cae en el hueco de la otra; el anillo de foco mueve el icono 0,5 dp; los iconos de zoom miden 14 dp (BH, ECH) | low | Detalles que solo se ven con TalkBack activo o con teclado; no se ven en las capturas. | rechazado |
| 12 | Opciones con peso 800 en vez de 700 (ECH) | low | El proyecto no tiene Nunito Sans 700; queda anotado como desvío. | rechazado |
| 13 | La prueba de la captura no comprueba el alto; null de `PanelSettings`; borde oculto al pulsar (BH, ECH) | low | Sin efecto visible o solo en pruebas. | rechazado |
| 14 | Spec, `CLAUDE.md`, memoria y sprint sin actualizar (BH) | low | Se ponen al día al cerrar. | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC (`-buildTarget Win64`) y `PriceMap.exe -captura <carpeta>`: capturas y log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.
