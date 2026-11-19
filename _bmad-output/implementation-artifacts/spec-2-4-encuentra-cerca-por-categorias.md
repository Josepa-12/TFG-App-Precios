---
title: 'Historia 2.4 (parte 1): lista «Encuentra cerca» por categorías'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '168f61a87692456ac0af8424618d394dbcdf9867'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Las categorías de la hoja del buscador (2.2) y «Encuentra cerca» no hacen nada. El usuario quiere ver lo que hay cerca de un tipo, también sin mirar el mapa (FR15, UX-DR17).

**Approach:** Al tocar una categoría, la hoja enseña una lista por distancia de esa categoría: primero los lugares con precio y después el resto, sin repetidos. «Encuentra cerca» enseña la misma lista con todas las categorías. Una regla pura del Dominio decide qué tipos de lugar y qué categorías de POI entran en cada categoría.

**Decisiones del 19/11/2026:**
- La historia se parte en dos. El filtro del mapa y el chip «Restaurantes ✕» de la píldora van en la parte 2 (`deferred-work`).
- «Encuentra cerca» = lista de todo lo cercano, como alternativa al mapa con TalkBack.
- El veterinario entra en Salud.
- Escribir en el campo quita la categoría y vuelve a la búsqueda de la 2.3.
- El TalkBack de la lista también va aparte (`deferred-work`).
- Cabecera con título y X; el botón atrás vuelve a las categorías; como mucho 50 filas y lugares a 3 km.

## Boundaries & Constraints

**Always:**
- **Categorías** (las 8 de la hoja: `HojaBuscador.Categorias`, valores de `CategoriaPoi`). Una regla pura `ReglaCerca` (Dominio) dice qué entra en cada una, con pruebas de la tabla:
  - **Comer:** restaurantes, tabernas, comida rápida, cafeterías y heladerías. POI: Comer, Rápida y Café.
  - **Bares:** bares, pubs y discotecas. POI: Bares.
  - **Deporte:** centros deportivos, piscinas, pistas, campos y clubes. POI: Deporte.
  - **Salud:** clínicas, dentistas, fisioterapia y veterinario. POI: Salud, incluidos los centros públicos.
  - **Cultura:** cine, teatro, museos, monumentos y visitas turísticas. POI: Cultura.
  - **Supermercados, Farmacias, Gasolineras:** solo POI de su categoría.
- **Lista:** filas como las de la 2.3: icono, nombre, tipo, distancia y precio resumido si lo tiene.
  - **Orden:** primero los lugares con precio por distancia; después el resto (lugares sin precio y POI) por distancia.
  - Sin los POI que repiten un lugar (regla de la 1.6). Como mucho 50 filas.
- **Alcance:** lugares de la app a 3 km o menos; POI solo de las teselas cargadas. La distancia se mide desde el usuario o, sin posición, desde el centro del mapa.
- **Cabecera de la lista:** el título («Comer cerca», o «Cerca» para «Encuentra cerca»), con «desde el centro del mapa» si no hay posición, y una X («Quitar categoría») que vuelve a las categorías.
  - El botón atrás también vuelve a las categorías antes de plegar la hoja.
  - Escribir en el campo quita la categoría.
- **Tocar una fila:** un lugar abre su ficha; un POI centra el mapa y pliega la hoja (su ficha llega en la 2.5).
- **Vacío:** «No hay <categoría> cerca», por ejemplo «No hay gasolineras cerca» (UX-DR16).

**Never:** filtro del mapa ni chip en la píldora (parte 2); TalkBack de la lista (otra parte): los nodos de chips y filas no se tocan, salvo que ninguno apunte a elementos que ya no se ven; servicios externos (Overpass, etc.); nada guardado.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Salud | Categoría Salud | Clínicas, fisio, dentistas, veterinario y centros públicos; primero los de precio |
| Supermercados | Super con teselas cargadas | Solo POI de super, por distancia |
| Repetido | POI y lugar con el mismo nombre a < 80 m | Una sola fila (el lugar) |
| Sin posición | Ubicación `Buscando` | Distancias desde el centro y «desde el centro del mapa» |
| Lejos | Lugar a 3,5 km | No sale |
| Encuentra cerca | Toque en «Encuentra cerca» | Todas las categorías, primero los de precio |
| Vacío | Gasolineras sin POI cargados | «No hay gasolineras cerca» |
| Quitar | X o botón atrás | Vuelven las categorías |
| Escribir | Categoría activa y se teclea "bar" | Búsqueda de la 2.3; la categoría se va |
| Toque | Fila de un lugar | Ficha abierta |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts`.
- **Dominio** — nuevo `ReglaCerca.cs`:
  - `Incluye(CategoriaPoi chip, Lugar lugar)` (por `lugar.Tipo`, con la tabla de la intención) e `Incluye(CategoriaPoi chip, CategoriaPoi poi)`.
  - Tipos de lugar de los datos: `Assets/Datos/Resources/*.json`. Hay una tabla parecida en `UI/IconosLugar.cs:29-39` (`GlifoPorTipo`); no se reutiliza, porque UI no es Dominio.
  - `CategoriaPoi.cs` tiene los valores de POI; `ReglaPuntosInteres.Categoria` (l.69), `EsRepetido` (l.126) y `EsSanidadPublica`.
- **Aplicacion** — nuevo `ServicioCerca.cs`:
  - `Listar(CategoriaPoi? chip, Coordenada cerca)` → `IReadOnlyList<ResultadoBusqueda>`; null es «Encuentra cerca».
  - Reutiliza `ResultadoBusqueda.DeLugar/DePunto` (`Puntuacion` 0) y las fuentes de `ServicioBusqueda.cs` (`lugares.Lugares`, `Resumen(id, ahora)`, `mapa.PuntosInteres`, `EsRepetido`), como hace `ServicioBusqueda.Buscar`.
  - Constantes: 3.000 m y 50.
- **UI**:
  - `HojaBuscador.cs`:
    - Los chips (`chips`, l.204-219) y `Cerca` (l.221) ganan `Clickable` y eventos (`CategoriaElegida(CategoriaPoi?)`).
    - La cabecera de la lista va encima de `Resultados`, con título, «desde el centro del mapa» y la X.
    - Las filas se pintan con `MostrarResultados` (l.415) o un hermano `MostrarCerca(titulo, resultados, vacio)`. «No hay …» reutiliza `SinResultados` con otro texto, sin el botón «Buscar como dirección».
  - `PantallaMapa.cs`:
    - Estado `categoriaActiva`. `Buscar` (l.435) la quita si hay texto.
    - `Atras()` la quita antes de plegar.
    - El toque reutiliza `ElegirResultado`.
    - TalkBack queda para otra parte. Que `DeclararResultadosEnTalkBack` no deje nodos de filas que ya no existen; que los chips y «Encuentra cerca» (`Texto`, l.403-408) sigan ocultos en el recorrido mientras se ve la lista.
    - `PuntoCercania()` (l.426) da la distancia. El texto «desde el centro del mapa» sale cuando `ubicacion?.Posicion` es null.
- **Arranque** — `Componer` crea `ServicioCerca` y lo pasa a `Inicializar` (opcional, como `busqueda`).
- **Textos** — `ClavesTexto`, `Editor/PrepararInterfaz.cs` (`TextosEs`) y después `PrepararInterfaz.Asegurar` por lotes: «{0} cerca», «Cerca», «desde el centro del mapa», «No hay {0} cerca» (con el nombre de la categoría en minúsculas y en plural) y «Quitar categoría».
- **Captura** — `CapturaPruebas`: un paso «cerca» tras la búsqueda; toca Salud y guarda `cerca.png`.
- **Pruebas** — nuevo `Tests/EditMode/PruebasCerca.cs`, con los falsos de `PruebasBusqueda` (mapa con puntos y lugares reales).

## Tasks & Acceptance

**Execution:**
- [x] `Dominio/ReglaCerca.cs` y pruebas de la tabla.
- [x] `Aplicacion/ServicioCerca.cs` (alcance, orden, repetidos, máximo) y pruebas.
- [x] `UI/HojaBuscador.cs`, `UI/PantallaMapa.cs`, `UI/PantallaMapa.uss`: chips y «Encuentra cerca» que se pueden tocar, cabecera, lista, vacío, X, atrás y escribir.
- [x] `Arranque/Arranque.cs`, textos, Localization y captura «cerca».
- [x] `Tests/EditMode/PruebasCerca.cs`: una prueba por fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `cerca.png`, entonces se ve la lista de Salud con Clínica Areas y su precio entre los primeros, y el registro no tiene errores.

## Spec Change Log

## Review Triage Log

Revisión del 19/11 (blind, edge-case, verification-gap).

| # | Hallazgo | Veredicto | Ruta | Evidencia |
|---|----------|-----------|------|-----------|
| 1 | «No hay salud cerca» / «No hay cultura cerca» al pasar a minúsculas el nombre del chip (blind, vg) | medium | patch | Cada chip tiene su clave («sitios de salud», «sitios de cultura», «sitios de deporte»…). Prueba `CadaCategoriaTieneSuTextoDeListaVacia`. |
| 2 | Un punto repetido de un lugar que no entra en la lista desaparece y no sale ninguno (blind, edge) | medium | patch | `EsRepetido` comparaba con todos los lugares; ahora solo con los de la categoría. Prueba `UnPuntoQueRepiteUnLugarDeOtraCategoriaSigueSaliendo`. |
| 3 | Rehacer la lista al volver a desplegar, sin probar (vg) | medium | patch | `AlVolverADesplegarSigueLaLista` comprueba ya las distancias desde el centro nuevo. |
| 4 | La prueba de TalkBack lee las reglas de visibilidad y no la jerarquía real (vg) | medium | patch | Nueva `ConTalkBackLaJerarquiaRealNoTieneLosChipsConLaLista` sobre `AssistiveSupport.activeHierarchy`. |
| 5 | Ninguna prueba recorre los tipos de los datos reales (blind) | low | patch | `CadaTipoDeLosDatosRealesTieneCategoriaOEstaExcluidoAPropósito`; se añade el caso «Pista de tenis». |
| 6 | Con la hoja abierta, la lista no se rehace al cargar puntos o al llegar la posición (blind, edge) | maybe-false (medium si es cierto) | defer | Se rehace al desplegar; rehacerla en caliente movería el scroll y el foco. Hay que ver en el móvil si se nota. |
| 7 | «Gimnasios cerca» enseña golf, piscinas y el club náutico; «Restaurantes» incluye cafeterías (blind) | low | defer | Las etiquetas de los chips son de la 2.2; renombrar el chip es decisión del usuario. |
| 8 | Arrastrar la hoja empezando en un chip puede elegir la categoría (blind) | maybe-false (medium si es cierto) | defer | Depende de si el `Clickable` se cancela con el arrastre; se ve en el móvil. |
| 9 | Toques reales en chips, «Encuentra cerca» y la X sin probar (vg) | maybe-false (medium si es cierto) | defer | En EditMode no hay panel para enviar eventos; el patrón del repo prueba los métodos públicos. |
| 10 | Los POI no tienen límite de 3 km (blind) | false | — | El spec lo dice así: 3 km para lugares, teselas cargadas para POI. |
| 11 | Solo espacios en el campo con una categoría (edge, vg) | low | rechazado | Camino raro y sin daño. |
| 12 | `ElegirCategoria` pública con texto en el campo; categoría fuera de los chips (edge) | false | — | La UI solo la llama desde los chips con las categorías visibles. |
| 13 | Puntos con Id 0 repetidos; orden con empates y `CompareOrdinal` con tildes (blind, edge) | low | rechazado | Mismo criterio que la 2.3; casos raros. |
| 14 | Sin estado `:active` en chips y X (blind) | low | rechazado | Cosmético. |
| 15 | Documentos sin actualizar (blind) | — | — | Se actualizan al cerrar. |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura «cerca»; después, limpiar `link.xml` (y su `.meta`) y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: tocar categorías, la lista y el botón atrás.
