---
title: 'Historia 2.2 (parte 1): hoja del buscador desplegable'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '71e2e2bfc532f52e319f64e4004fa92e4f6f20b2'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No hay forma de buscar: falta la cápsula inferior y la hoja del buscador (FR13, UX-DR4, UX-DR5).

**Approach:** Cápsula de cristal abajo con el campo "Buscar en Sanlúcar" y el avatar. Al tocarla o arrastrarla sube una hoja que sigue al dedo, con la física de soltar de UX-DR5. Antes de escribir muestra las categorías y "Encuentra cerca". La búsqueda real es de la 2.3. **Decisión del 19/11/2026:** la historia se parte en dos. Esta parte 1 trae la cápsula, la hoja y sus gestos. TalkBack y lo que se lee del sistema (reducir animaciones y transparencias) van en la parte 2.

## Boundaries & Constraints

**Always:**
- **Cápsula:** altura mínima de 56 dp, radio `pill`, cristal (`--color-glass`) y fondo opaco en el tema oscuro. A la derecha, un avatar genérico de 40 dp que aún no hace nada.
- **Física de la hoja**, pura en `FisicaHoja` y con pruebas:
  - Sigue al dedo.
  - Si se lanza a más de 0,6 dp/ms, se abre o se cierra del todo según el sentido.
  - Si se suelta por debajo del 15 % de la altura útil, se pliega; por encima del 88 %, se abre del todo.
  - Entre esos dos límites se queda exactamente donde se suelta, sin imán a ninguna altura (decisión del 19/11/2026).
  - Un movimiento de menos de 6 dp es un toque: enfoca el campo y saca el teclado. Uno mayor es un arrastre y no saca el teclado.
  - Con la lista arriba del todo, arrastrarla hacia abajo mueve la hoja.
- **Contenido inicial:** chips de categorías con icono vectorial (restaurantes, bares, supermercados, farmacias, gasolineras, gimnasios, salud y cultura) y la entrada "Encuentra cerca". Aún no hacen nada (2.4).
- **Atrás:** pliega la hoja antes que cualquier otra cosa.
- **Convivencia:**
  - La hoja abierta tapa los botones de abajo.
  - La ficha y la hoja nunca están abiertas a la vez: abrir una cierra la otra.
  - La cápsula se oculta mientras la ficha está abierta.

**Never:** sin búsqueda real (2.3), sin favoritos ni frecuentes (épica 3), sin perfil al tocar el avatar (3.5), sin TalkBack de la hoja ni lectura de los ajustes del sistema (parte 2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Arrastrar y soltar | Arrastre hasta 50 %, velocidad 0,1 dp/ms | Queda al 50 % |
| Lanzar arriba | Velocidad 0,9 dp/ms hacia arriba | Abierta del todo |
| Lanzar abajo | Velocidad 0,9 dp/ms hacia abajo desde el 70 % | Plegada |
| Soltar baja | Suelta al 10 % | Plegada |
| Soltar alta | Suelta al 92 % | Abierta |
| Toque | Movimiento 3 dp | Abierta del todo, foco al campo y teclado |
| Arrastre corto | Movimiento 10 dp | Arrastre, sin teclado |
| Atrás | Hoja abierta | Se pliega |
| Ficha | Ficha abierta y se toca la cápsula | Se cierra la ficha y se abre la hoja |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- **`Scripts/UI/FisicaHoja.cs`** (nuevo, puro): usa las constantes de `prototipo/app.js:722` (plegada 76, hueco arriba 64, lanzamiento 0,6, 0,15, 0,88, toque 6).
  - La velocidad se muestrea cada más de 4 ms y es 0 si el dedo se paró más de 90 ms antes de soltar (`app.js:747`).
  - La lista mueve la hoja (`app.js:782`): siempre si no está abierta del todo; si lo está, solo con el desplazamiento a 0 y arrastrando hacia abajo.
- **`Scripts/UI/HojaBuscador.cs`** (nuevo): es la cápsula y la hoja a la vez. Con `k` la fracción abierta, la forma se mezcla con `s = min(1, k/0.15)` (`app.js:737`):
  - Los márgenes pasan de 12/12/20 a 0 y las esquinas de `--radio-pill` a `--radio-sheet` arriba y 0 abajo.
  - Abierta, el fondo es el opaco de superficie.
  - El tirador mide 36×3, o 40×5 abierta.
  - La transición dura 0,32 s (la parte 2 la quitará con reducir animaciones). La sombra se pone con `SombraSuave.Poner`.
  - Chips de 40 de alto mínimo con borde de 1,5. Iconos con `IconosLugar.Dibujar` y `GlifoDe(CategoriaPoi.Comer/Bares/Super/Farmacia/Gasolina/Deporte/Salud/Cultura)` (l.124 y l.73); colores `--color-poi-<cat>` de `Tokens.uss` (l.57-101).
  - La lupa y el avatar son glifos nuevos en `IconosLugar.Trazos` (l.43).
- **`UI/PantallaMapa.uxml`**: `hueco-buscador` después de `hueco-ficha` (l.36). Estilos en `PantallaMapa.uss`; los tokens nuevos van en `Estilos/Tokens.uss`, en claro y en oscuro (desde ~l.155).
- **`Scripts/UI/PantallaMapa.cs`**:
  - `Inicializar` (l.61) crea la hoja.
  - `Atras()` (l.568) comprueba la hoja después del aviso y antes que la ficha.
  - `AbrirFicha` (l.358) pliega la hoja y oculta la cápsula; `CerrarFicha` (l.447) la vuelve a mostrar. Abrir la hoja cierra la ficha y el menú (`Cerrar` l.554).
  - `TapaPunto` (l.608): la hoja tapa el mapa.
  - `ZonasControles` (l.737): `Ocupar(cápsula)`.
  - `AjustarDisposicion` (l.705): margen inferior de la zona segura.
  - Los gestos van con `PointerDown`/`PointerMove`/`PointerUp`, capturando el puntero.
- **Textos**: `ClavesTexto` y `PrepararInterfaz.TextosEs` (l.29). Hacen falta "Buscar en Sanlúcar", "Encuentra cerca" y los 8 nombres de categoría.
- **Captura**: `Arranque/CapturaPruebas.cs`, con un `Buscador()` en la lista de `Start` (l.151-156) que deja la hoja al 50 % y guarda `Capturar("buscador")`. Modelo: `Ficha()` (~l.456).
- **Pruebas**: nuevo `Tests/EditMode/PruebasBuscador.cs`, con el patrón `Crear`/`Limpiar` de `PruebasFicha.cs` (l.80-122).
- **No tocar:** `AccesibilidadPantalla` (parte 2) ni la lógica de marcadores, puntos y filtro.

## Tasks & Acceptance

**Execution:**
- [x] `FisicaHoja.cs` + pruebas de cada fila de física de la matriz.
- [x] `HojaBuscador.cs`, UXML/USS/tokens y glifos — cápsula, hoja y contenido inicial.
- [x] `PantallaMapa.cs` — gestos, atrás, exclusión con la ficha, zonas ocupadas.
- [x] Textos, captura "buscador" y `PruebasBuscador.cs` (gestos, atrás, ficha).

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `buscador.png`, entonces se ven la hoja a media altura con las categorías, en claro y oscuro, sin solapes con los controles.

## Implementation Notes

- **Gestos y teclado:** `HojaBuscador` escucha `PointerDown/Move/Up/Cancel` en la fase de bajada (TrickleDown). Al pasar de 6 dp captura el puntero, quita el foco al campo y para la propagación. Así el `TextField` no recibe el soltar y no saca el teclado. Con un toque el soltar sí le llega, y el propio `TextField` saca el teclado. Un toque fuera del campo (lupa, relleno, tirador) abre la hoja y enfoca el campo, pero puede que no saque el teclado: hay que comprobarlo en el móvil.
- **Estado comprobable:** las pruebas manejan el gesto con `Bajar/Mover/Soltar(punto, ms)` y `Medir(alto)`, sin panel. `TecladoPedido` indica que se ha pedido el foco y el teclado.
- **Forma:** `Aplicar()` pone desde el código `top`, márgenes, esquinas y relleno. A partir de s = 1 se añade la clase `buscador--abierta` (fondo `surface`, tirador de 40×5 y borde de acento en el campo). Mientras se arrastra se añade `buscador--arrastrando`, sin transición.
- **Capas:** `hueco-buscador` va entre `hueco-ficha` y `hueco-explicacion`, y `PruebasFiltro` se ha ajustado a ese orden. La atribución sube por encima de la cápsula plegada.
- **`TapaPunto`:** la hoja tapa el mapa entero mientras se arrastra y cuando está abierta del todo. A media altura, solo lo que cubre.
- **Captura:** `buscador.png` (claro) y `buscador-oscuro.png`, con comprobaciones de posición, de que se ven los 8 chips y "Encuentra cerca" y de que la hoja no pisa la píldora, la columna ni la atribución.

## Spec Change Log

## Review Triage Log

| # | Hallazgo | Veredicto | Ruta | Evidencia |
|---|----------|-----------|------|-----------|
| 1 | Si el dedo sale de la cápsula antes de los 6 dp o se suelta fuera sin captura, `AlSoltar` no llega y la hoja deja de responder | high | patch | Mover, soltar y cancelar solo se escuchaban en la hoja. Ahora se escuchan en la raíz del panel mientras dura el gesto, y un gesto colgado se reinicia al bajar otro dedo |
| 2 | La primera velocidad del arrastre divide el recorrido previo al umbral por un instante: un arrastre lento cuenta como lanzamiento; un lanzamiento de un solo movimiento daba 0 | medium | patch | `Mover` reiniciaba `msMuestra` al pasar el umbral. Ahora la primera muestra abarca desde que bajó el dedo. La prueba que fijaba −2 dp/ms se corrigió a −1 y hay dos pruebas nuevas |
| 3 | Un movimiento horizontal en el campo (mover el cursor) se toma como arrastre y cierra el teclado | medium | patch | Al pasar el umbral, si el gesto es más horizontal que vertical se ignora |
| 4 | Con la hoja a media altura, abrir el menú «Precios» lo deja debajo de la hoja | medium | patch | `AlternarMenu` ahora pliega la hoja |
| 5 | `Mostrar(false)` en mitad de un gesto deja la física pulsada | low | patch | Ahora cancela la física; hay prueba |
| 6 | Nada comprueba en la captura que no haya marcadores bajo la cápsula sin depender de `ZonasControles` | gap | patch | `SolapesConMarcadores` añade la cápsula medida aparte. Captura: 0 bajo un control |
| 7 | El comentario de `FisicaHoja` dice "sin Unity" y usa `Vector2`/`Mathf` | low | patch | Comentario corregido |
| 8 | Los manejadores de puntero (lista contra hoja, avatar) no se prueban con eventos reales | gap | defer | En EditMode no hay panel que reparta eventos; se comprueba en el móvil |
| 9 | Plegar borra lo escrito en el campo | medium | defer | El spec no lo dice; hay que decidirlo en la 2.3, cuando la búsqueda exista |
| 10 | El contraste del texto y de los chips nuevos no está en `PruebasContraste` | medium | defer | Usa los tokens de superficie y de texto ya medidos, pero los pares nuevos no tienen prueba |
| 11 | La fila de la matriz «Ficha abierta y se toca la cápsula» no puede darse, porque la cápsula se oculta con la ficha | low | rechazado | La prueba comprueba el enlace `Desplegando` → `CerrarFicha`; arreglarlo sería editar el spec congelado |
| 12 | Tocar el tirador con la hoja abierta vuelve a sacar el teclado | low | rechazado | Uso poco habitual; cambiarlo añade un caso especial |
| 13 | Tocar durante la transición de 0,32 s hace saltar la hoja | low | rechazado | Cosmético y raro; el arreglo exige leer el estilo resuelto |
| 14 | Panel bajo o apaisado: la hoja casi no se abre | false | rechazado | La app es solo vertical |
| 15 | Al ocultar la cápsula, la atribución sigue subida; el chip tiene un fondo por defecto de otra categoría; no hay capturas plegada ni abierta | low | rechazado | La ficha tapa la atribución; todos los chips llevan su clase; `claro.png` y `oscuro.png` ya muestran la cápsula plegada |
| 16 | Falta `Textos Shared Data.asset` en el diff; falta la prueba de la atribución; `TapaPunto` a media altura sin prueba | false | rechazado | Fallo al preparar el diff; la captura comprueba la atribución; `TapaPunto` a media altura usa `Pick`, que necesita panel |
| 17 | CLAUDE.md y la memoria del TFG sin actualizar; fecha `created` cambiada; estado del sprint | low | patch | Se actualizan al cerrar; `created` vuelve al 18/11 |

## Verification

**Commands:**
- Pruebas EditMode por lotes — todas pasan.
- Build de PC y `PriceMap.exe -captura` — `buscador.png` correcta; luego borrar `link.xml` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: arrastre, lanzamiento y teclado.
