---
title: 'Historia 1.5 (parte 1): filtro "Precios" con píldora y menú'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: 'b4f4d2585d3337d63ca3b1ced55b2fade285e48a'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Hoy el mapa muestra siempre los 124 marcadores, y no hay forma de dejarlo limpio ni de ver solo un tipo de lugar (FR-2, UX-DR6).

**Approach:**
- Una píldora "Precios" arriba a la izquierda abre un menú con Ninguno, Todos, Servicios locales, Hostelería y Ocio.
- La opción elegida filtra los marcadores por plantilla y se guarda en el móvil.
- La primera vez es "Ninguno": la píldora dice "Ver precios" y no hay marcadores.
- La regla va en el Dominio y el estado en un servicio de la Aplicación, como el tema.

## Boundaries & Constraints

**Always:**
- **Píldora:**
  - Cristal (`--color-glass`, borde `--color-control-edge`) y radio `pill`, como `.boton-redondo`.
  - Al menos 48 dp de alto. Con la fuente al 200 % crece sin cortar el texto (`.t-label`).
  - Va arriba a la izquierda, dentro de la zona segura (16 dp arriba, 12 dp a la izquierda).
  - Lleva un punto de color y una flecha hacia abajo, dibujados con `Painter2D`.
- **Texto de la píldora:** "Ver precios" (Ninguno, con el texto atenuado), "Todos los precios", "Servicios locales", "Hostelería" u "Ocio".
- **Punto de color:** aro hueco con Ninguno, `--color-pin-*` de la plantilla con una plantilla, y acento con Todos.
- **Menú desplegable**, debajo de la píldora:
  - Fondo `surface` y radio `lg`.
  - Título "Mostrar precios en el mapa".
  - 5 opciones de al menos 48 dp, **solo con su nombre, sin contador (decisión del 17/11/2026)**. Cada opción lleva su punto de color, y la elegida se marca con fondo `surface-2` y un check.
  - Se cierra al elegir y al tocar fuera, también sobre el mapa. Ese toque no mueve el mapa.
  - Sin animación.
- **TalkBack (mínimo de esta parte):** la píldora es un botón "Precios en el mapa", con la opción actual como valor.
- **Preferencia:** la elección se guarda con `IPreferencias` (clave `filtro-precios`). Un valor desconocido o ausente cuenta como Ninguno.
- **Arquitectura:**
  - La regla (qué plantillas entran en cada opción) va en el Dominio, sin UnityEngine.
  - Textos con Localization y colores de `Tokens.uss`.
  - Sin filtro (`null`), `CapaMarcadores` muestra todos los lugares, como hasta ahora.

**Never:**
- **No entra en esta parte, va a la parte 2:**
  - Opciones del menú en TalkBack con estado "seleccionado", anuncio al cambiar y foco de vuelta a la píldora.
  - Botón atrás de Android.
  - Captura "filtro".
- Sin la excepción de FR-2 "salvo el lugar abierto o el destino actual": no existen hasta las historias 1.8 y 2.x.
- Sin filtrar por "solo con precio".
- No se tocan los POI (1.6).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Primer arranque | Sin preferencia guardada | Píldora "Ver precios" y 0 marcadores | N/A |
| Todos | Elegir Todos | Píldora "Todos los precios" y los 124 marcadores | N/A |
| Una plantilla | Elegir Hostelería | Solo los marcadores de hostelería (89) | N/A |
| Se recuerda | Elegir Ocio y volver a arrancar | Píldora "Ocio" y solo los marcadores de ocio | N/A |
| Valor dañado | Preferencia "xyz" | Como Ninguno | N/A |
| Cerrar sin elegir | Menú abierto y tocar el mapa | Se cierra, el filtro no cambia y el mapa no se mueve con ese toque | N/A |
| Marca | Abrir el menú con Hostelería elegida | Hostelería con check y fondo `surface-2` | N/A |
| Filtro + fuera de vista | Hostelería, y un bar fuera de la pantalla | Oculto (las dos condiciones a la vez) | N/A |

</frozen-after-approval>

## Code Map

- `Assets/Scripts/Dominio/Plantilla.cs` (`ServicioLocal`, `Hosteleria`, `Ocio`) y `ReglaTema.cs` (estilo de regla estática). Aquí van `FiltroPrecios` y `ReglaFiltro` (`Incluye`, `AClave`, `DesdeClave`).
- `Assets/Scripts/Aplicacion/ServicioTema.cs:11-40`: patrón para `ServicioFiltro` (`ClavePreferencia`, `Actual`, `Cambiado`, `Iniciar()` y `Elegir()`). `IPreferencias.cs:7-9` tiene `Leer`, `Guardar` y `Borrar`.
- `Assets/Scripts/Arranque/Arranque.cs:27-60`: crear `ServicioFiltro`, pasarlo a `Componer` y a `PantallaMapa.Inicializar`, y llamar a `Iniciar()`. `PruebasArranque.cs` llama a `Componer` varias veces.
- `Assets/Scripts/UI/CapaMarcadores.cs:61-70`: `Actualizar` recalcula `ver` en cada fotograma, así que el filtro va dentro de esa condición. Cuando el filtro cambia, `Actualizar` tiene que devolver `cambio` para TalkBack.
- `Assets/UI/PantallaMapa.uxml:10-16`: `zona-segura` solo tiene `pila-derecha` y `atribucion`. Hay que añadir `hueco-precios` arriba a la izquierda. El menú va en la misma `zona-segura`, encima de la capa de marcadores.
- `Assets/UI/PantallaMapa.uss:42-61` (`.boton-redondo`): estilo de cristal que hay que imitar.
- `Assets/Scripts/UI/BotonRedondo.cs`: patrón de control con `Clickable`, `NavigationSubmitEvent` e iconos con `Painter2D`. Sirve para `PildoraPrecios.cs` y `MenuPrecios.cs`.
- `Assets/Scripts/UI/PantallaMapa.cs`:
  - `TapaPunto`/`EsControl` (`:84-96`). Con el menú abierto, todo el mapa cuenta como tapado: un toque fuera cierra el menú y el mapa no lo recibe.
  - `AlCambiarTema`/`RepintarIconos`: repintar el punto y la flecha.
- `Assets/Scripts/UI/AccesibilidadPantalla.cs:44`: `Boton(..., valor)`.
- `Assets/Scripts/UI/ClavesTexto.cs` y `Editor/PrepararInterfaz.cs:29` (`TextosEs`): textos nuevos. `PruebasTextos` comprueba que existen en la tabla.
- `Assets/UI/Estilos/Tokens.uss`: `--color-glass`, `--color-surface`, `--color-surface-2`, `--radio-pill`, `--radio-lg`, `--zona-tactil-min`, `--color-pin-*` y el acento.
- `Assets/Scripts/Arranque/CapturaPruebas.cs`: la captura "marcadores" de la 1.4 necesita marcadores visibles. Con Ninguno por defecto daría error, así que tiene que elegir Todos antes y restaurar la preferencia al acabar.
- `Tests/EditMode/PruebasMarcadores.cs` (1.4) construye `CapaMarcadores` sin filtro, y esas pruebas tienen que seguir pasando.

## Tasks & Acceptance

**Execution:**
- [x] `Dominio/FiltroPrecios.cs` y `Dominio/ReglaFiltro.cs`: opciones, inclusión por plantilla y paso de clave a filtro y de filtro a clave (Ninguno si la clave no se reconoce).
- [x] `Aplicacion/ServicioFiltro.cs`: `Actual`, `Cambiado`, `Iniciar()` y `Elegir()`, con la preferencia `filtro-precios`.
- [x] `UI/CapaMarcadores.cs`: filtro dentro de la condición de visibilidad.
- [x] `UI/PildoraPrecios.cs`, `UI/MenuPrecios.cs` y `PantallaMapa.uxml/.uss`: píldora, menú, abrir y cerrar, marca y repintado al cambiar de tema.
- [x] `UI/PantallaMapa.cs`:
  - Conectar con `ServicioFiltro` y aplicar el filtro a la capa.
  - Botón de TalkBack de la píldora, con su valor.
  - Bloquear el mapa con el menú abierto.
- [x] `ClavesTexto.cs` y `PrepararInterfaz.TextosEs`: "Precios en el mapa", "Ver precios", "Todos los precios", las 5 opciones y el título del menú.
- [x] `Arranque.cs`, `PruebasArranque.cs` y `CapturaPruebas.cs`: composición, y elegir Todos en la captura "marcadores".
- [x] `Tests/EditMode/PruebasFiltro.cs`: regla, servicio y cada fila de la matriz con la pantalla instanciada y un `MapaFalso`.

**Acceptance Criteria:**
- Dada la build de PC sin preferencia guardada, cuando se mira la captura `claro.png`, entonces la píldora dice "Ver precios" y no hay marcadores.
- Dada la misma ejecución, cuando se mira `marcadores.png`, entonces se ven los marcadores (la captura ha elegido Todos) y después se restaura la preferencia.

## Implementation Notes

- Implementado por un subagente.
  - Pasan 168/168 pruebas EditMode, 25 de ellas en `PruebasFiltro`.
  - Captura de PC: `claro.png` muestra "Ver precios" con el aro hueco y 0 marcadores, y `marcadores.png` muestra "Todos los precios" con marcadores.
  - Un clic accidental en la primera ejecución abrió el menú de verdad: Todos salía marcado con el check y el menú se cerró al tocar fuera.
- Piezas nuevas:
  - `PildoraPrecios`, `MenuPrecios`, `OpcionPrecios`, `PuntoPrecios` y `TextosFiltro`.
  - `velo-menu`, un velo invisible a pantalla completa que cierra el menú al tocarlo.
  - `Inicializar` recibe `ServicioFiltro` como último parámetro opcional. Sin él no hay píldora y se ven todos los lugares, para que las pruebas de la 1.4 sigan valiendo.
- Tras los parches de la revisión:
  - Pasan 170/170 pruebas EditMode.
  - El orden de la interfaz es: marcadores, `zona-segura` (tema, zoom, atribución), `velo-menu` y `zona-segura-precios` (píldora y menú). Con el menú abierto, tocar tema o zoom solo cierra el menú.
  - Captura de PC: arranque con Ninguno y 0 marcadores; "marcadores" con Todos (7 visibles, 0,5 px); al acabar, la preferencia y el filtro vuelven a como estaban.
- En el sprint, la 1.5 sigue en `in-progress` hasta la parte 2 (TalkBack del menú, botón atrás y captura "filtro").

## Spec Change Log

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | BH | Con el menú abierto, los botones de tema y zoom quedan por encima del velo: hacen su acción y el menú sigue abierto | medium | `pila-derecha` va después de `velo-menu` en el UXML (comprobado). Incumple "se cierra al tocar fuera" → patch y prueba |
| 2 | VG | Ninguna prueba comprueba que `Componer` entrega el filtro a una pantalla con interfaz | medium | En la prueba de arranque la pantalla no tiene UIDocument y `Inicializar` sale antes → patch: prueba con el árbol |
| 3 | VG | La prueba de textos del menú compara la tabla de textos consigo misma | medium | Un texto cambiado no fallaría → patch: claves esperadas fijas y valor de TalkBack de la píldora |
| 4 | BH, EC, VG | La captura restaura la preferencia guardada pero no el filtro en memoria | low | `filtroAntes` solo se escribe en el log → patch |
| 5 | VG | Los toques reales sobre el velo, la píldora y las opciones (`Clickable`) no tienen prueba | low | Gap ya verificado, disposición defer: sin panel en EditMode → deferred |
| 6 | BH, EC | TalkBack no recorre las opciones del menú y el botón atrás no lo cierra | false | Es de la parte 2, ya apuntada en deferred-work (el spec lo deja fuera: Never) |
| 7 | BH | El filtro no se ve hasta el siguiente `Actualizar` | false | `PantallaMapa.LateUpdate` llama a `Actualizar` en cada fotograma, no solo al mover la cámara |
| 8 | BH | La escala de fuente del menú podría entrar en bucle | false | `EscalaFuente.Aplicar` pone `base × escala`: con la misma escala no cambia la geometría |
| 9 | BH | Hay dos valores por defecto: `null` (todos) y Ninguno | false | Lo decide el spec, para no romper las pruebas de la 1.4 |
| 10 | BH | Faltan los assets de textos en el diff | false | Se excluyeron del diff que se revisaba; las claves las comprueba `PruebasTextos` |
| 11 | BH | Con `:focus` el borde pasa a 2 px y el contenido se mueve | low | Mismo patrón que `.boton-redondo`, ya aceptado en la 1.2 → rechazado |
| 12 | BH, EC | Un valor de `FiltroPrecios` fuera del enum en `Elegir`, y valores nuevos que caen en Ninguno | low | Solo pasa con un cast forzado → rechazado |
| 13 | EC | Si al UXML le falta `hueco-precios`, `Inicializar` falla | low | El UXML es nuestro; fallaría de forma visible → rechazado |
| 14 | EC | Aro con radio negativo si mide menos de 2 px | low | Siempre mide 10 dp o más → rechazado |
| 15 | EC | Con filtro y sin interfaz, nadie se suscribe a `Cambiado` | low | Sin interfaz no hay nada que filtrar → rechazado |
| 16 | EC | Si la corrutina de captura se detiene, la preferencia "todos" queda guardada | low | Es una herramienta de prueba manual que siempre termina con `Quit` → rechazado |
| 17 | BH | `AlCambiarFiltro` se ejecuta dos veces al arrancar, faltan `<summary>`, 124 y 89 están fijos, y no hay prueba de `OnDestroy` | low | Cosméticos, o los fija el spec → rechazado |

## Verification

**Commands:**
- `"C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe" -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults <scratch>/resultados.xml -logFile <scratch>/tests.log` -- todas pasan (con el editor cerrado).
- `-executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarPcPruebas` y luego `Builds/PC/PriceMap.exe -captura <carpeta>` -- `claro.png` (Ver precios, sin marcadores) y `marcadores.png`, sin errores en el log. Después, borrar `Assets/AddressableAssetsData/link.xml` y deshacer `Windows/addressables_content_state.bin`.
