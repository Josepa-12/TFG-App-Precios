---
title: 'Historia 1.5 (parte 2): menú "Precios" con TalkBack y botón atrás'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: '99c94c204e01d5f5f5fb808e8c0697651953a8dc'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El menú "Precios" (parte 1) solo se puede usar a la vista. Con TalkBack se abre, pero sus opciones no existen para el lector. Además, el botón atrás de Android no lo cierra. La historia 1.5 pide que el menú "se maneja con TalkBack" (FR-2, Accessibility Floor).

**Approach:**
- Con el menú abierto, TalkBack solo recorre la píldora y las 5 opciones, como el velo hace a la vista. La opción elegida se anuncia como seleccionada.
- Al elegir se anuncia "Precios en el mapa: Hostelería".
- Al abrir, el foco va a la opción elegida. Al cerrar, vuelve a la píldora.
- El botón atrás cierra el menú sin cambiar nada.
- Una captura de PC con el menú abierto lo deja comprobado.

## Boundaries & Constraints

**Always:**
- **Menú modal también para TalkBack:** con el menú abierto, la jerarquía solo tiene la píldora (que lo cierra) y las 5 opciones, en orden. Los botones de tema y zoom y los marcadores salen de ella mientras dure. Al cerrar vuelven.
- **Cada opción es un botón con su nombre:**
  - La elegida lleva el estado seleccionado (`AccessibilityState.Selected`, si existe en Unity 6.3; si no, el valor "seleccionado").
  - Activarla hace lo mismo que tocarla.
- **Anuncio:** al elegir se anuncia "Precios en el mapa: {opción}" con `AccesibilidadPantalla.Anunciar`. Con el lector apagado no se anuncia nada.
- **Foco:**
  - Al abrir, el foco de TalkBack va a la opción elegida y el del teclado o mando también (`Focus()`).
  - Al cerrar (por elegir, tocar fuera o atrás), los dos vuelven a la píldora.
  - Para TalkBack se usa `notificationDispatcher.SendLayoutChanged(nodo)`.
- **Botón atrás:**
  - Con el menú abierto, `KeyCode.Escape` (el atrás de Android) lo cierra sin cambiar el filtro, y la app no sale.
  - Con el menú cerrado, atrás no hace nada nuevo en esta historia.
- Los textos van por Localization. La clave del anuncio es nueva (`precios.anuncio` = "Precios en el mapa: {0}").

**Never:**
- No se cambia el aspecto del menú ni de la píldora (parte 1).
- No se añade una opción "Cerrar": se cierra con la píldora, con atrás o eligiendo.
- El botón atrás no gestiona nada más que este menú (no hay más capas todavía).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Abrir con TalkBack | Lector activo y Hostelería elegida; activar la píldora | Recorrido: píldora y Ninguno, Todos, Servicios locales, Hostelería (seleccionada) y Ocio. El foco va a Hostelería | N/A |
| Elegir con TalkBack | Activar "Ocio" | Filtro Ocio, menú cerrado, anuncio "Precios en el mapa: Ocio" y foco en la píldora. El recorrido vuelve a tener botones y marcadores | N/A |
| Cerrar con la píldora | Activar la píldora con el menú abierto | Menú cerrado, filtro sin cambios, foco en la píldora | N/A |
| Atrás con el menú | Menú abierto y `Escape` | Menú cerrado, filtro sin cambios, foco en la píldora | N/A |
| Atrás sin menú | Menú cerrado y `Escape` | No pasa nada (ni se cierra la app ni cambia el filtro) | N/A |
| Lector apagado | Elegir una opción | Sin anuncio, y el filtro cambia igual | N/A |

</frozen-after-approval>

## Code Map

- `Assets/Scripts/UI/PantallaMapa.cs`:
  - `CrearFiltro()` (`:102-118`) registra la píldora con `accesibilidad.Boton(...)`.
  - `AlternarMenu()` (`:136`) y `CerrarMenu()` (`:149`) abren y cierran el menú.
  - `LateUpdate()` (`:264`) es donde va la tecla atrás, o un `Update` nuevo.
  - `AlCambiarFiltro` llama a `Reconstruir()`.
- `Assets/Scripts/UI/AccesibilidadPantalla.cs`:
  - `Boton(elemento, etiqueta, accion, valor, visible)` (`:44`).
  - `Reconstruir()` (`:88`): rehace la jerarquía con los controles visibles y luego los marcadores.
  - `Recorrido()`: el orden, que usan las pruebas.
  - `Anunciar(texto)` (`:152`).
  - **Falta:**
    - Un modo modal: un `Func<bool>` o un grupo activo, para que con el menú abierto solo se incluyan sus controles y no los marcadores.
    - El estado seleccionado en un botón.
    - Una forma de pedir el foco de un control, con `SendLayoutChanged(nodo)`.
- `Assets/Scripts/UI/MenuPrecios.cs` (`Opciones`, `Abierto`, `Mostrar`, `Marcar`) y `OpcionPrecios.cs` (`Filtro`, `Texto`, `Elegida`, `Pulsar()`).
- `Assets/Scripts/UI/PildoraPrecios.cs`: es enfocable (`focusable` y `NavigationSubmitEvent`).
- `Assets/Scripts/UI/ClavesTexto.cs` y `Editor/PrepararInterfaz.cs:29` (`TextosEs`): clave nueva. Volcarla con `PrepararInterfaz.Asegurar` por lotes, como en la parte 1.
- `Assets/Scripts/Arranque/CapturaPruebas.cs:45-80`:
  - Añadir la captura "filtro": elegir Hostelería, abrir el menú, capturar, cerrar y restaurar el filtro y la preferencia, como hace `MarcadoresConTodos` (`:118`).
  - Registrar en el log el recorrido de TalkBack con el menú abierto, con `Recorrido()`.
- `Assets/Tests/EditMode/PruebasFiltro.cs`: patrón de pantalla instanciada con `MapaFalso` y preferencias en memoria. Con el lector apagado, `Reconstruir` no construye nodos, así que las pruebas usan `Recorrido()` y comprueban el estado y el foco por métodos que no dependan del lector.

## Tasks & Acceptance

**Execution:**
- [x] `UI/AccesibilidadPantalla.cs`: grupo modal, estado seleccionado y petición de foco, reflejados también en `Recorrido()`. Que cada entrada indique si está seleccionada.
- [x] `UI/PantallaMapa.cs`:
  - Registrar las 5 opciones.
  - Activar el modo modal al abrir y quitarlo al cerrar, llamando a `Reconstruir()`.
  - Foco al abrir y al cerrar.
  - Anuncio al elegir.
  - `Escape` cierra el menú.
- [x] `ClavesTexto.cs`, `PrepararInterfaz.TextosEs` y las tablas: `precios.anuncio`.
- [x] `Arranque/CapturaPruebas.cs`: captura "filtro" y recorrido en el log, restaurando el estado al acabar.
- [x] `Tests/EditMode/PruebasFiltro.cs`: cada fila de la matriz. El atrás se simula llamando al método que llama `Update`, sin teclado real.

**Acceptance Criteria:**
- Dada la build de PC, cuando se abre `filtro.png`, entonces se ve la píldora "Hostelería" con el menú abierto y Hostelería marcada.
- Dado el log de esa ejecución, cuando se lee el recorrido de TalkBack con el menú abierto, entonces son la píldora y las 5 opciones, con Hostelería seleccionada, y nada más.

## Implementation Notes

- Implementado por un subagente.
  - Pasan 176/176 pruebas EditMode.
  - En la captura de PC, `filtro.png` muestra la píldora "Hostelería" con el menú abierto y Hostelería marcada.
  - Recorrido en el log: "Precios en el mapa | Ninguno | Todos | Servicios locales | Hostelería (seleccionada) | Ocio".
  - Anuncio en el log: "Precios en el mapa: Hostelería".
- `AccessibilityState.Selected` existe en Unity 6.3. El foco se pide en el siguiente pase, cuando el menú ya tiene su disposición.
- En las pruebas EditMode, `precios.anuncio` devuelve la clave en lugar del texto, probablemente por el catálogo de Addressables en caché. Por eso la prueba lo comprueba con `PrepararInterfaz.TextosEs`. En la build sí sale el texto.
- Tras los parches de la revisión:
  - Pasan 179/179 pruebas EditMode.
  - En la captura de PC, `filtro.png` ya no muestra el borde de foco tras un toque, y el recorrido y el anuncio del log son correctos.
  - Hay ganchos solo para las pruebas: `AccesibilidadPantalla.LectorForzado`, `AnuncioPedido` y `Textos.Forzados`, todos `internal` y visibles para las pruebas con `UI/AssemblyInfo.cs`. Con el lector forzado se prueba la jerarquía real.
  - El foco del teclado o mando solo se mueve con el lector activo o al navegar (`NavigationSubmitEvent` o atrás). El anuncio se envía después del cambio de foco.

## Spec Change Log

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG). El de casos límite se cortó por el límite de uso y se reanudó.

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | EC | `Focus()` al abrir y al cerrar deja el borde de foco en la opción y la píldora tras un toque | medium | Incumple "no se cambia el aspecto" (Never) y se ve en `filtro.png` → patch: mover el foco solo con lector o al navegar con teclado o mando |
| 2 | BH, EC | El anuncio se envía antes de mover el foco y TalkBack puede cortarlo | medium | `PedirFoco` va al siguiente pase y `Anunciar` es inmediato → patch: anunciar después del cambio de foco |
| 3 | BH, VG | El anuncio al elegir no se prueba: el evento solo salta con lector, y una aserción se compara consigo misma | medium | Quitar `Anunciar` no rompería ninguna prueba → patch: evento de petición antes de mirar el lector, y una prueba con el texto exacto |
| 4 | BH, VG | La jerarquía real en modo modal (estado Selected, sin marcadores, `ActualizarMarcadores`) no se prueba | medium | Con el lector apagado, `Reconstruir` sale enseguida → patch: forzar el lector en pruebas (o deferred si no se puede en EditMode) |
| 5 | BH | `Modal` obliga a llamar luego a `Reconstruir` | low | Fácil olvidarlo → patch: que reconstruya solo |
| 6 | VG | El botón atrás (`Update` + `Escape`) no se prueba | low | Gap ya verificado, disposición defer → deferred (móvil real) |
| 7 | BH | Sin opción elegida, el foco no se mueve | low | `Actual` siempre es una de las 5 opciones → rechazado |
| 8 | BH | Elegir la opción ya elegida no tiene prueba | low | `Elegir` cierra, enfoca y anuncia siempre, cambie o no el filtro → rechazado |
| 9 | BH | Las etiquetas de las opciones salen de `Texto.text` y no de `Textos.T` | low | Es el mismo texto (sale de la tabla) → rechazado |
| 10 | BH | El botón atrás se gestiona en la pantalla y no de forma global; `backButtonLeavesApp` | low | Por ahora solo hay una capa (lo dice el spec) → rechazado |
| 11 | BH, EC | Captura: restauración si la corrutina se detiene, y comprobación del anuncio demasiado estrecha | low | Es una herramienta manual → rechazado |
| 12 | EC | La plantilla del anuncio sin `{0}` lanza `FormatException` | low | La tabla es nuestra → rechazado |
| 13 | BH | No se prueba tocar fuera con foco | low | `Atras` y el velo usan el mismo `CerrarMenu`, y está probado → rechazado |
| 14 | BH | Faltan assets en el diff y la memoria del TFG | false | Se excluyeron del diff que se revisaba; la memoria se actualiza al cerrar |

## Verification

**Commands:**
- `"C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe" -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults <scratch>/resultados.xml -logFile <scratch>/tests.log` -- todas pasan (con el editor cerrado).
- `-executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarPcPruebas` y luego `Builds/PC/PriceMap.exe -captura <carpeta>` -- `filtro.png`, el recorrido en el log y ningún error. Después, borrar `Assets/AddressableAssetsData/link.xml` y deshacer `Windows/addressables_content_state.bin`.
