---
title: 'Mapa y ficha sin solapes'
type: 'bugfix'
created: '2026-11-18'
status: 'done'
baseline_commit: '05a4e8f327addef53a08774e29bde41e1de2b40a'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Con la captura a tamaño de móvil (412 dp), la ficha de Clínica Areas sale con las filas montadas unas sobre otras: el nombre del concepto pisa el origen y la frescura, y la unidad queda debajo del precio. En el mapa, el punto de interés "Plaza de los Milagros" queda debajo del marcador de la clínica, la Taberna Santo Domingo se monta sobre él y los nombres del mapa se leen a través de los controles.

**Approach:** **Decisiones del usuario (18/11/2026):**
- La ficha se dispone sin solapes a cualquier ancho.
- Se borran de los datos los conceptos de Clínica Areas sin importe ("Consultar").
- En el mapa nada se pinta encima de un marcador ni debajo de los controles.
- Los lugares sin precio siguen en el mapa: una primera versión los ocultaba por un malentendido y se deshace.

## Boundaries & Constraints

**Always:**
- **Ficha sin solapes:**
  - Cada fila de concepto (nombre, variantes, origen, frescura, precio y unidad) ocupa su propio alto: ningún texto de una fila pisa otro texto de su fila ni de otra, ni los encabezados de área, a 412 dp y a 360 dp de ancho, ni con la fuente al 130 %.
  - Hay que encontrar y corregir la causa en `FichaLugar`/`FilaConcepto`/`PantallaMapa.uss`, no ocultarla.
- **Datos de Clínica Areas:** se quitan de `lugares.json` sus conceptos sin importe (`traumatologia/infiltraciones`, `traumatologia/factores-de-crecimiento` y el de "Recuperación con frío (Aquilo / Phisiolab)"). "Consultar" se sigue probando con datos sintéticos.
- **Lugares sin precio:** se ven como antes (1.5), pequeños y atenuados. Se deshace el filtro "solo con precio" de la primera versión.
- **Marcadores entre sí:** de los visibles, primero los que tienen precio y, entre iguales, del más cercano a la cámara al más lejano. Uno cuya zona dibujada (`MarcadorLugar.ZonaDp`) pisa la de otro ya mostrado se oculta. En el mismo fotograma no se ven dos marcadores que se pisan.
- **Puntos de interés:** las zonas de los marcadores visibles cuentan como ocupadas antes de colocar los puntos (`CapaPuntosInteres.Actualizar`). Un punto cuya caja (icono y nombre) pisa un marcador se oculta, igual que cuando pisa otro punto.
- **Debajo de los controles (decisión del 18/11/2026):** la píldora, el menú "Precios" abierto y las columnas de botones cuentan como zonas ocupadas, con sus rectángulos en dp del panel. Un marcador o un punto de interés que las pise se oculta. Como Unity no difumina lo de detrás, el cristal (`--color-glass`) sube a 0,90 de opacidad en claro y en oscuro: los nombres de calle del mapa, que no se pueden ocultar, apenas se leen debajo. La prueba de contraste se mantiene.
- **TalkBack:** el recorrido solo tiene los marcadores visibles, como ahora. Un marcador oculto por solape no sale.
- **Toque:** `CapaMarcadores.MasCercano` solo mira los visibles (ya lo hace).
- **Rendimiento:** sin asignaciones por fotograma nuevas (reutilizar listas, como `cajasMostradas`).

**Never:** sin agrupar marcadores en círculos con número; sin desplazar marcadores de su sitio; sin tocar las etiquetas de calles del mapa vectorial; sin cambiar el filtro Ninguno (sigue sin lugares).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Ficha a 412 dp | Ficha de Clínica Areas en la captura | Ninguna caja de texto pisa a otra | N/A |
| Ficha estrecha y fuente grande | 360 dp y fuente al 130 % | Ninguna caja de texto pisa a otra | N/A |
| Datos de Clínica Areas | Su ficha | Sin "Infiltraciones", "Factores de crecimiento" ni "Recuperación con frío"; el resto, igual | N/A |
| Sin precio visible | Filtro Todos; un lugar sin precio que no pisa nada | Se ve, pequeño y atenuado | N/A |
| Con precio gana | Taberna Santo Domingo (sin precio) pisa Clínica Areas | Se ve Clínica Areas; la taberna se oculta hasta acercar | N/A |
| Dos marcadores con precio | Dos lugares con precio cuyas zonas se pisan | Se ve solo el más cercano a la cámara; al acercar hasta que no se pisen, los dos | N/A |
| Punto bajo marcador | Clínica Areas visible y "Plaza de los Milagros" pisando su zona | El punto se oculta; cuenta en `OcultosPorSolape` | N/A |
| Punto libre | Punto que no pisa ningún marcador ni punto | Se ve | N/A |
| Bajo un control | Un punto de interés o un marcador cuya caja pisa la píldora o la columna | Se oculta | N/A |
| Cristal | `--color-glass` en claro y en oscuro | Opacidad 0,90 | N/A |
| TalkBack | Lector forzado y un marcador oculto por solape | No está en el recorrido | N/A |

</frozen-after-approval>

## Code Map

- `Assets/Scripts/UI/CapaMarcadores.cs`:
  - `Actualizar` (l.69-91) decide `ver` con `ReglaFiltro.Incluye` y la proyección.
  - Hay que añadir `&& m.TienePrecio` cuando el filtro no es Ninguno, y una segunda pasada de solapes: ordenar los visibles por `Profundidad` y ocultar los que pisan una zona ya aceptada. El valor de retorno ("cambió el conjunto visible") debe tener en cuenta esas bajas.
  - `MasCercano` (l.~128) ya ignora los no visibles.
- `Assets/Scripts/UI/MarcadorLugar.cs`: `TienePrecio` (l.44, ya es false para públicos), `ZonaDp`, `Profundidad`, `Mostrar`.
- `Assets/Scripts/UI/CapaPuntosInteres.cs`:
  - `Actualizar` (l.102-145). Antes del bucle de solapes, sembrar `cajasMostradas` con las zonas de los marcadores visibles.
  - Hace falta un parámetro nuevo (p. ej. `IReadOnlyList<Rect> ocupadas`) o leerlas de `CapaMarcadores`.
  - Actualizar el resumen de la l.100 ("Los marcadores de lugar no cuentan para los solapes").
- Controles: `PildoraPrecios`, `MenuPrecios` y las columnas `columna-controles`/zoom de `PantallaMapa`. Sus `worldBound` dan las zonas ocupadas (vacías mientras no tienen medida). Tokens en `Assets/UI/Estilos/Tokens.uss` (`--color-glass`).
- `Assets/Scripts/UI/PantallaMapa.cs`: `ActualizarCapas` (l.~702) llama primero a la capa de marcadores y después a la de puntos con los lugares visibles. Hay que pasarle también las zonas.
- **Pruebas que cambian a propósito:**
  - `PruebasMarcadores` y `PruebasFiltro`: las que esperan ver marcadores "sin precio" con un filtro activo.
  - `PruebasPuntosInteres`: las que esperan un punto encima de un marcador.
  - La captura `marcadores` de `CapturaPruebas` cuenta los "sin precio" visibles: ahora tienen que ser 0. Añadir allí la comprobación "ningún punto ni marcador visible pisa un marcador".

## Tasks & Acceptance

**Execution:**
- [x] `CapaMarcadores.cs`: solapes entre marcadores (con precio primero); "solo con precio" deshecho.
- [x] `PantallaMapa.uss`: la ficha sin solapes (causa: el contenido del ScrollView encogía); comprobación en la captura a 412 y 360 dp con fuente al 130 %.
- [x] `lugares.json` (y el exportador): sin los conceptos "Consultar" de Clínica Areas.
- [x] `CapaPuntosInteres.cs` y `PantallaMapa.cs`: las zonas de los marcadores cuentan como ocupadas.
- [x] `CapturaPruebas.cs`: comprobación de solapes con marcadores.
- [x] `Tests/EditMode`: una prueba por fila de la matriz y ajustar las que esperaban el comportamiento anterior.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `marcadores.png`, entonces junto a Clínica Areas no hay nada encima de su marcador, y el log informa de 0 pares que se pisan y no tiene errores.

## Implementation Notes

- **Ficha (causa del solape):** el proyecto no carga el tema por defecto de Unity, así que el `content-container` del `ScrollView` de la ficha tenía `flex-shrink: 1` y encogía hasta el alto del viewport (en la captura, 436 dp de filas en 436 dp de viewport, cuando necesitan 831). Con él encogían cada `FilaConcepto` y cada encabezado de área: las cajas quedaban más bajas que su texto y los textos se dibujaban fuera, encima de la fila siguiente (la unidad debajo del precio, el nombre sobre el origen). Corrección en `PantallaMapa.uss`: `flex-shrink: 0` en el contenedor y en `.ficha__area`, `.ficha__concepto`, `.ficha__sin-precio` y los hijos de las columnas de la fila. No era cosa de `EscalaFuente` ni de `position`.
- **Comprobación de la ficha en la captura** (`CapturaPruebas.SolapesFicha`): para cada texto de la hoja, `Debug.LogError` si su caja pisa la de otro (medio dp de tolerancia) o si `MeasureTextSize` (con su ancho + 1 dp) pide más alto del que tiene. Se ejecuta a 412 dp, a 360 dp con la fuente al 130 % (captura nueva `ficha-estrecha.png`) y en `variantes`. Sin la corrección da 4 pares y 44 textos desbordados; con ella, 0. Para forzar el 130 % se añadió `EscalaFuente.Forzada`/`EscalaFuente.Sistema` (la app sigue usando `AccessibilitySettings.fontScale`).
- **Datos:** `exportar-datos-unity.js` ya no exporta los conceptos sin importe y `lugares.json` se regeneró (solo desaparecen los 3 de Clínica Areas; quedan 8). "Consultar" se prueba con un lugar sintético (`PruebasFicha.ConConsultar`).
- `CapaMarcadores.Actualizar(..., ocupadas)`: primera pasada (filtro + pantalla, con o sin precio) y segunda en orden: con precio primero, después por cercanía y, a igual distancia, por `Indice`. Expone `ZonasVisibles` y `OcultosPorSolape`. `Pisa` (interno, compartido con la capa de puntos) ignora rectángulos sin medida (NaN o vacíos).
- `CapaPuntosInteres.Actualizar(..., lugaresVisibles, ocupadas)`: un punto que pisa una zona ocupada se oculta y cuenta en `OcultosPorSolape`.
- `PantallaMapa.ZonasControles()` rellena una lista reutilizada con los `worldBound` de la píldora, el menú (solo abierto), `columna-controles` y `zoom` (solo con clase `visible`); sin panel o sin medida no añade nada. Tras los marcadores se le añaden sus `ZonasVisibles` y se pasa a los puntos.
- `--color-glass`: 0,90 en claro y oscuro; la prueba de contraste sigue pasando.
- Pruebas: la `MapaFalso` de `PruebasFiltro` reparte cada coordenada en una rejilla (antes todo caía en el mismo punto y ahora se pisaría). Dos lugares reales comparten coordenada, así que con Todos o sin filtro se ven 123 de 124.
- **Marcador de la ficha abierta:** `CapaMarcadores.Fijado` (lo pone `PantallaMapa.AbrirFicha` y lo quita `CerrarFicha`) va primero en el orden y no se oculta ni por solapes ni por controles; la captura `ficha` da error si el de Clínica Areas no se ve (centrado, queda junto a la píldora).
- **Histéresis:** el orden de solapes (`Prioridad`, un `IComparer` reutilizado) pone, a igual precio, primero al que ya se veía en el fotograma anterior: al girar no se alternan dos que se pisan.

## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | Con la ficha abierta, su marcador queda bajo la píldora y se oculta (BH, VG, ECH) | medium | Se ve en `ficha.png`: el lugar que describe la ficha desaparece del mapa. | patch |
| 2 | Parpadeo: entre marcadores de igual prioridad, el ganador se elige por `Profundidad` en cada fotograma, sin histéresis (BH, ECH) | medium | Al girar, dos marcadores a profundidad parecida se alternan, y cada cambio rehace el recorrido de TalkBack. | patch |
| 3 | `ActualizarCapas` (zonas de los marcadores → puntos) no se prueba por la pantalla (VG) | medium | Las pruebas pasan las zonas con sus propias funciones; quitar el `AddRange` no rompe nada. | patch |
| 4 | La comprobación de la captura no usa las mismas zonas que la app (falta el zoom y el menú; cadena de `parent`) (BH, ECH) | low | Se corrige leyendo `ZonasControles()`. | patch |
| 5 | Asignaciones por fotograma en `Sort(Comparison)` y `AddRange` (ECH) | low | En Mono, `Sort(Comparison)` crea un comparador por llamada; el spec pide sin asignaciones nuevas. | patch |
| 6 | `EscalaFuente.Forzada` es pública (BH) | low | Solo la usan las pruebas y la captura; pasa a `internal`. | patch |
| 7 | La pasada estrecha no avisa si el ancho no cambió; comentario con el orden al revés; comentarios de `Tokens.uss` desfasados (BH) | low | Correcciones directas. | patch |
| 8 | Dos lugares con la misma coordenada: uno queda oculto a cualquier zoom y sale de TalkBack (BH, ECH) | medium | Antes se dibujaban uno sobre otro y TalkBack anunciaba los dos. Es un problema de los datos; la solución (desplazar o agrupar) va contra el "Never" del spec. | defer |
| 9 | `ZonasControles` con disposición real y el arreglo de la ficha solo se comprueban en la captura de PC (VG, BH) | medium | EditMode no tiene layout; la captura lo comprueba. | defer |
| 10 | La hoja de la ficha no cuenta como zona ocupada (ECH) | low | Es opaca y, con la ficha abierta, TalkBack es modal: no se ve ni se recorre lo de debajo. | rechazado |
| 11 | Primer fotograma del menú sin medida; punto OSM del lugar oculto por solape (ECH) | low | Un fotograma; y el punto cae en la misma zona ocupada, así que también se oculta. | rechazado |
| 12 | El punto de posición y la atribución no cuentan como zona (BH) | low | Fuera del alcance del spec. | rechazado |
| 13 | El Code Map del spec sigue con la primera versión; el exportador no cuenta lo descartado; `lugares.json` sin hunks; huecos de `SolapesFicha` (BH, ECH) | low | Cambiar el spec no se corrige en revisión; el resto, sin efecto visible. | rechazado |
| 14 | La corrutina interrumpida no restaura la fuente (ECH) | low | Solo en la captura de PC, que no se interrumpe. | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC (`-buildTarget Win64`) y `PriceMap.exe -captura <carpeta>`: `marcadores.png` y log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.

**Resultados (18/11/2026, segunda versión):** 380/380 pruebas EditMode. Captura: "marcadores sin precio visibles 7; ocultos por solape 1; pares que se pisan 0; bajo un control 0"; ficha a 412 dp, a 360 dp con fuente al 130 % y variantes: 0 pares que se pisan y 0 textos desbordados; centrado de las dos fichas con 0,0 px de desvío; ningún mensaje de `Debug.LogError` en el log. `link.xml` borrado y `addressables_content_state.bin` restaurado.
