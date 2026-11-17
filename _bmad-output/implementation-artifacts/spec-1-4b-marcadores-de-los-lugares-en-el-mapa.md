---
title: 'Historia 1.4 (parte 2): marcadores de los lugares con su logo en el mapa'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: '04017e921fdd9049b7c6ef7297249c854e1d5b54'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La app ya carga los 124 lugares y sabe si cada uno tiene precio (parte 1), pero el mapa no los muestra. Solo están los 4 marcadores de negocio puestos a mano en la escena, sin logo, sin precio y sin TalkBack.

**Approach:** `IMapa` da la posición en pantalla de una coordenada. La UI dibuja cada lugar en una capa de UI Toolkit, debajo de los botones:
- Cada marcador lleva el logo vectorial de su tipo, el color de su plantilla y la insignia € si tiene precio; sin precio, se ve atenuado.
- El marcador se recoloca cada fotograma, después de la cámara.
- TalkBack lo anuncia.
- Se retiran los 4 marcadores fijos.

## Boundaries & Constraints

**Always:**
- **Marcador (como el prototipo, `styles.css:67-78, 519-521`):**
  - Cuadrado de 42 dp, radio `--radio-md`, borde blanco de 2,5 px y una **punta** blanca abajo de 8 dp. **La coordenada cae en la punta.**
  - Logo blanco de 22 dp.
  - Insignia € de 18 dp en `--color-reward`, arriba a la derecha.
  - Sin precio: 32 dp, punta de 6 dp, logo de 17 dp, opacidad 0,7 y sin €.
- **Colores fijos, iguales en los dos temas (decisión del 17/11/2026):** servicio `#2F5BEA`, hostelería `#E07B12` y ocio `#1F9D6B`. Van en tokens nuevos `--color-pin-*` que no cambian con el tema, para que el logo blanco cumpla el contraste.
- **Desvío:** la posición sale del estado de la cámara en el mismo fotograma, con un desvío menor de 1 px.
- **Logo:** sale del tipo con la tabla del prototipo. Un tipo que no está en la tabla usa el icono genérico `pin`. Iconos vectoriales con `Painter2D`, nunca emojis.
- **TalkBack:**
  - Con precio: "Clínica Areas, clínica privada, desde 20 €".
  - Sin precio: "Bar Cachón, bar, aún no tiene precio".
  - El tipo va en minúsculas (es-ES).
  - Euros en es-ES, formateados por la UI (AD-10): 2000 → "20 €" y 2250 → "22,50 €".
  - Recorrido: primero los botones y después los marcadores visibles, de arriba abajo y de izquierda a derecha.
- `Camera`, `Vector2` y `Vector3` no salen de Vectorial ni de UI. Dominio y Aplicación siguen sin UnityEngine.

**Never:**
- Sin filtro "Precios": por ahora se ven todos los lugares, y el filtro llega en la 1.5.
- Sin tocar el marcador: no abre nada, porque la ficha es de la 1.8. Los marcadores no capturan toques y el mapa se sigue moviendo al arrastrar sobre ellos.
- Sin agrupar marcadores cercanos.
- No se toca el marcador "Tu posición" (1.7).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Con precio | Clínica Areas | Marcador azul de 42 dp con logo `cross` e insignia € | N/A |
| Sin precio | Bar Cachón | Marcador naranja de 32 dp con logo `martini`, opacidad 0,7, sin € | N/A |
| Público | `Lugar` con `publico: true` (sintético) | Sin €, atenuado | N/A |
| Tipo desconocido | `tipo` que no está en la tabla | Logo `pin` | Log `PriceMap:` de aviso, una vez por tipo |
| Proyección | Lugar delante de la cámara | La punta está a menos de 1 px del punto proyectado | N/A |
| Fuera de vista | Detrás de la cámara o fuera de la pantalla (con margen de un marcador) | Oculto y sin nodo de TalkBack | N/A |
| Tema | Pasar de claro a oscuro | El marcador mantiene sus colores | N/A |
| Toque | Arrastrar empezando sobre un marcador | El mapa se mueve (`TapaPunto` da false) | N/A |
| Sin lugares | La carga falló (lista vacía) | Capa vacía y la app funciona | N/A |

</frozen-after-approval>

## Code Map

- `Scripts/Aplicacion/IMapa.cs`: se añade `PuntoPantalla Proyectar(Coordenada)`. `PuntoPantalla` es un struct nuevo en Aplicación con `X`, `Y` en px de pantalla (origen arriba a la izquierda) y `Delante` (z > 0). Los bordes y el margen los decide la UI.
- `Scripts/Vectorial/MapaVectorial.cs:172-191` (puerto): implementa la proyección con `Geo.ALocal(lat, lon)` (`Geo.cs:25`) y la cámara de `CamaraMapa`.
- `Scripts/Vectorial/CamaraMapa.cs:43-69`: hoy lee la entrada y mueve la cámara en `LateUpdate`. **Pasa a `Update` con `[DefaultExecutionOrder(-100)]`**. Así la cámara está quieta antes de que nadie proyecte y UI Toolkit (que actualiza los paneles antes de los `LateUpdate`) recibe la posición en el mismo fotograma.
- `Scripts/UI/PantallaMapa.cs`:
  - `Inicializar(mapa, tema[, raiz])` → `Inicializar(mapa, tema, lugares, ahora[, raiz])`. Lo llama `PruebasPantallaMapa.cs:49`.
  - `TapaPunto`/`EsControl` (`:84-96`).
  - La escala de px a dp está en `AjustarDisposicion` (`:138-143`).
- `Scripts/UI/EscalaFuente.cs:18`: escala todos los `TextElement`. El € se dibuja con `Painter2D` para que no se desborde con la fuente al 200 %.
- `Scripts/UI/AccesibilidadPantalla.cs:32-50`: una sola `activeHierarchy`. Se añade `Marcador(elemento, etiqueta, visible)`, sin acción y con rol `StaticText`, a la misma jerarquía. Cuando cambia el conjunto de marcadores visibles, se añaden o quitan sus nodos sin rehacer los de los botones.
- `Scripts/UI/BotonRedondo.cs`: estilo de los iconos con `Painter2D`.
- `UI/PantallaMapa.uxml/.uss`: `raiz` > `zona-segura` > `pila-derecha`, con todo en `picking-mode="Ignore"`. La capa va como primer hijo de `raiz`, también con `Ignore`, igual que todos sus hijos.
- `UI/Estilos/Tokens.uss`: `--color-reward` y `--color-reward-ink` (`:17-18`), y `--radio-md` (`:44`). Se añaden `--color-pin-servicio|hosteleria|ocio|borde`, sin redefinirlos en `.tema-oscuro`.
- `Scripts/UI/Textos.cs` (`T(clave)`) y `ClavesTexto.cs` (constantes y `Todas`, comprobadas por `PruebasTextos`). Las entradas van en `Editor/PrepararInterfaz.cs:29` (`TextosEs`) y se vuelcan con `AsegurarTextos()`.
- `prototipo/lugares.js:11-40` (`GLYPH`), `:45-68` (`POI_CATS`: categoría → glifo) y `:108-117` (`PLACE_POICAT`: tipo → categoría; `PLACE_GLYPH`: excepciones).
- `Editor/PrepararEscenaPrincipal.cs:14, 23-29, 63-65, 110-131`: quitar `Sitios`, `CrearSitio` y los materiales que ya no se usan, y regenerar `Scenes/Principal.unity`.
- `Scripts/Arranque/Arranque.cs:44`: `Componer` añade los lugares. Se llama 5 veces desde `PruebasArranque.cs:54-91`. El "ahora" lo da el Arranque (`DateTime.UtcNow`, AD-12).
- `Scripts/Arranque/CapturaPruebas.cs:46-62`: patrón de las capturas.

## Tasks & Acceptance

**Execution:**
- [x] `Aplicacion/PuntoPantalla.cs`, `IMapa.cs`, `Vectorial/MapaVectorial.cs` y los `MapaFalso` de las pruebas: añadir `Proyectar`.
- [x] `Vectorial/CamaraMapa.cs`: entrada y movimiento en `Update` con `[DefaultExecutionOrder(-100)]`.
- [x] `UI/IconosLugar.cs`:
  - Tabla de tipo → glifo.
  - Trazos de los glifos que hacen falta, más `pin`.
  - Intérprete de trazos para `Painter2D`: M/L/H/V/C/A/Z en absoluto y relativo, lineto implícito tras M, números compactos (`2-2`, `.6`), banderas de arco, `circle` y `rect` con `rx`.
- [x] `UI/FormatoPrecio.cs`: euros es-ES desde céntimos.
- [x] `UI/MarcadorLugar.cs` y `UI/CapaMarcadores.cs`:
  - Cada marcador es un elemento absoluto en 0,0 con `usageHints = DynamicTransform`, y se mueve con `style.translate`. `display` solo se cambia cuando cambia la visibilidad.
  - `Actualizar(float escalaDp)`: la escala se inyecta para las pruebas.
  - Los más cercanos se pintan encima: se ordenan por profundidad solo cuando el orden cambia.
- [x] `UI/AccesibilidadPantalla.cs`: añadir `Marcador(...)` y la actualización de los nodos de los marcadores visibles.
- [x] `UI/PantallaMapa.cs`: crear la capa con los lugares y su resumen, y llamar a `Actualizar` en `LateUpdate`.
- [x] `UI/PantallaMapa.uss` y `Tokens.uss`: estilos del marcador y tokens `--color-pin-*`.
- [x] `Textos.T(clave, params object[])` (con `string.Format`), las claves `marcador.anuncio.conprecio` = "{0}, {1}, desde {2}" y `marcador.anuncio.sinprecio` = "{0}, {1}, aún no tiene precio", y sus entradas en `TextosEs`.
- [x] `Arranque.cs` y `PruebasArranque.cs`: `Componer` con los lugares y "ahora".
- [x] `CapturaPruebas.cs`:
  - Captura "marcadores" centrada en Clínica Areas.
  - Registrar en el log el mayor desvío entre la punta de cada marcador visible (`worldBound` tras el layout) y su proyección.
- [x] `PrepararEscenaPrincipal.cs` y `Principal.unity`: quitar los sitios y regenerar la escena por lotes. `PruebasEscenaPrincipal` comprueba que el único `Marcador` es "Tu posición".
- [x] `Tests/EditMode/PruebasMarcadores.cs`:
  - Cada fila de la matriz con un `MapaFalso` que proyecta a puntos conocidos.
  - Todos los tipos del JSON real tienen un glifo propio.
  - Todos los glifos se interpretan sin error.
  - Euros, textos de TalkBack y orden de recorrido.

**Acceptance Criteria:**
- Dada la build de PC, cuando se abre la captura "marcadores", entonces se ven los lugares con su logo y su color, y Clínica Areas lleva la insignia €.
- Dada la captura de PC, cuando se lee el log, entonces el mayor desvío entre punta y proyección es menor de 1 px.

## Implementation Notes

Solo Clínica Areas tiene precios publicados en su web. FisioSanlúcar y ChemaBarber no los publican, así que los otros 123 marcadores salen atenuados. Es lo esperado.

- Implementado por un subagente.
  - Pasan 135/135 pruebas EditMode.
  - En la captura de PC, el mayor desvío entre punta y proyección es de 0,500 px y el log no tiene errores.
  - Clínica Areas lleva la cruz azul y la insignia €, y el resto sale atenuado con su logo.
- Cosas que se apartan un poco del spec:
  - `PuntoPantalla` lleva `Profundidad` (para ordenar), y `Delante` sale de ella.
  - El intérprete de trazos también lee `S`/`s`, porque el glifo `pin` lo usa.
  - `AccesibilidadPantalla.Marcador` recibe la posición para ordenar el recorrido.
  - `CapaMarcadores.Actualizar` tiene una variante que recibe el tamaño de pantalla, para las pruebas.
  - `Componer` da error si falta `ServicioLugares`.
  - Sin precio, el marcador usa `--radio-sm`.
- Cambios de assets:
  - Se regenera `Principal.unity` y se borran los materiales "Marcador Servicio/Hostelería/Ocio".
  - `LocalizationSettings.asset` pasa de `en` a `es-ES` al volcar los textos, que es el idioma correcto.
  - Se ha deshecho `addressables_content_state.bin`, que había cambiado la build.
- **Contraste:** el logo blanco sobre el naranja de hostelería `#E07B12` da 2,997:1, justo por debajo del 3:1 que piden los iconos. Servicio da unos 5,6:1 y ocio 3,45:1. Es el color que eligió el usuario el 17/11, así que se le comenta.
- TalkBack no se ha probado en un móvil real. Solo está probado el orden del recorrido.
- Tras los parches de la revisión:
  - Pasan 141/141 pruebas EditMode.
  - En la captura de PC hay 7 marcadores visibles, Clínica Areas lleva €, el desvío es de 0,500 px y no hay errores.
  - El naranja del marcador de hostelería pasa a `#D6740F` (3,3:1), por decisión del usuario.
  - Para que el orden de TalkBack se mantenga al mover el mapa, `PantallaMapa` llama a `ActualizarMarcadores()` en cada `LateUpdate`. Con el lector apagado sale al momento.
  - Los helpers de contraste de `PruebasContraste` pasan a `internal static` para reutilizarlos.

## Spec Change Log

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | VG | Ninguna prueba hace `PantallaMapa.Inicializar` con lugares cargados (`ConResumen` y el registro en TalkBack) | medium | Todas pasan null o una carga fallida → patch: prueba con el JSON real |
| 2 | VG, BH | `MapaVectorial.Proyectar` real (Y invertida, profundidad) sin prueba | medium | Solo la usan los mapas falsos → patch: prueba en `PruebasMapaVectorial` |
| 3 | VG, EC | Límite vertical de visibilidad sin probar. Un punto por encima del borde de arriba (de 0 a −50 dp) cuenta como visible aunque el marcador entero queda fuera | low | El marcador cuelga hacia arriba desde su punto → patch: `dp.y >= 0` arriba y pruebas de los dos bordes |
| 4 | BH, EC | Al mover el mapa, el orden de TalkBack no se actualiza si siguen visibles los mismos marcadores | low | `MismoConjunto` solo compara el conjunto → patch: rehacer los nodos también cuando cambia el orden |
| 5 | BH | El formato de euros no es el de es-ES: "1.234 €" debería ser "1234 €" (agrupación mínima de 2) y el espacio antes de € debería ser fijo | low | CLDR y RAE → patch: agrupar a partir de 5 cifras y U+00A0 |
| 6 | BH, EC, VG | `Proyectar` invierte la Y con `pixelRect.yMax` y la UI compara con `Screen.height` | low | Hoy dan lo mismo (la cámara ocupa toda la pantalla) → patch: `Screen.height` |
| 7 | EC | `TipoMarcador.Servicio/Hosteleria/Ocio` ya no se usan | low | Solo queda "Tu posición" → patch: borrarlos |
| 8 | VG | Los nodos de TalkBack de los marcadores solo se crean con el lector activo, así que ninguna prueba recorre ese código | low | Gap ya verificado, disposición defer → deferred (prueba en el móvil) |
| 9 | VG | El orden de ejecución (cámara en `Update` y marcadores en `LateUpdate`) solo se comprueba en la captura | low | No hay pruebas PlayMode → deferred |
| 10 | BH | El texto de TalkBack se construye una vez y, como Localization carga de forma asíncrona, podría salir la clave | false | `GetTable` es síncrono (espera a que cargue) y las claves están en las tablas (`Textos Shared Data.asset:43,47`) |
| 11 | BH | `TextoAnuncio` falla si `Tipo` es null | false | `Lugar` guarda `tipo ?? ""`, así que nunca es null |
| 12 | BH | `Opaco` es código muerto que volvería a crear los materiales | false | Lo usa "Tu posición" (`PrepararEscenaPrincipal.cs:97`) y solo crea el material de posición |
| 13 | BH | Faltan los assets en el diff | false | Se excluyeron del diff que se revisaba por tamaño. Las claves están en las tablas |
| 14 | BH | No se agrupan los marcadores que se solapan | false | El spec lo deja fuera (Never) |
| 15 | BH | El resumen del precio no se recalcula durante la sesión y `.Valor` se usa sin mirar `Ok` | low | Los datos base no cambian en la sesión y `Resumen` solo falla con un id que no existe → rechazado |
| 16 | BH | Coste por fotograma: 124 proyecciones y reordenar al girar | low | `Camara()` se busca una vez y luego se reutiliza; 0,5 px y fluido en PC → rechazado (se medirá en el móvil con los 30 fps de la 1.2) |
| 17 | BH | `null` en `Inicializar` se acepta sin aviso y la prueba usa `UtcNow` | low | Solo afecta a las pruebas → rechazado |
| 18 | BH | `CapturaPruebas` depende de los datos actuales (`clinica-areas`, 124) | low | Es una herramienta de prueba manual → rechazado |
| 19 | EC | Sin `CamaraMapa`, `Camara()` busca en la escena 124 veces por fotograma | low | La escena siempre tiene cámara → rechazado |
| 20 | EC | Una plantilla de texto mal escrita lanza `FormatException` | low | La tabla es nuestra y la prueba los anuncios → rechazado |
| 21 | EC | Si falta `capa-marcadores` en el UXML, `Inicializar` falla | low | El UXML es nuestro; fallaría de forma visible → rechazado |
| 22 | EC | Zoom desde un callback después de `LateUpdate`: un fotograma de retraso | maybe-false | Depende de cuándo despache Unity los eventos de UI y de TalkBack. Se vería en el móvil; si es real sería low → rechazado |
| 23 | Triage | El logo blanco sobre el naranja `#E07B12` da 2,997:1, por debajo de 3:1 | medium | El usuario decide el 17/11 oscurecer el naranja del marcador → patch: `--color-pin-hosteleria` a `#D6740F` (3,3:1) y prueba de contraste de los tres colores del marcador |

## Verification

**Commands:**
- `"C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe" -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults <scratch>/resultados.xml -logFile <scratch>/tests.log` -- todas pasan (con el editor cerrado).
- `-executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarPcPruebas` y luego `Builds/PC/PriceMap.exe -captura <carpeta>` -- se genera `marcadores.png`, el log no tiene errores y el desvío es menor de 1 px. Después, borrar `Assets/AddressableAssetsData/link.xml`, que regenera la build.
