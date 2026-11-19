---
title: 'Historia 2.4 (parte 3): TalkBack de la lista «Encuentra cerca»'
type: 'feature'
created: '2026-11-19'
status: 'done'
baseline_commit: 'b078d183665fcc7f67693c2ec99d413d9305d76d'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Con TalkBack, los chips y «Encuentra cerca» se leen como texto y no se pueden activar, y la lista de una categoría no entra en el recorrido: quien usa lector no puede usar «Encuentra cerca».

**Approach:** Chips, «Encuentra cerca», filas de la lista y la X de la cabecera pasan a ser botones. Al abrir la lista se anuncia cuántos hay y el foco va al título, como en la búsqueda (2.3).

**Decisión del 19/11/2026:** al quitar la categoría con la hoja abierta (X o botón atrás), el foco va al chip de la categoría que estaba elegida («Encuentra cerca» si era esa).

## Boundaries & Constraints

**Always:**
- **Chips y «Encuentra cerca»:** botones con su nombre («Salud», «Encuentra cerca»); activarlos hace lo mismo que tocarlos.
- **Cabecera:** el título es un texto («Salud cerca» y, sin posición, «Salud cerca, desde el centro del mapa»). La X es el botón «Quitar categoría».
- **Filas:** cada una es un botón con el mismo anuncio que en la búsqueda (`FilaResultado.Anuncio`, p. ej. «Nombre, tipo, a 350 m, desde 20 €»). Activar una fila de lugar abre la ficha con el foco en su título; una de punto hace lo mismo que tocarla.
- **Al abrir la lista** (chip, «Encuentra cerca» o desplegar la hoja con la lista): se anuncia «<Título>, N resultados» («1 resultado») o, vacía, el texto vacío de la categoría («No hay sitios de salud cerca»). El foco va al título.
- **Plegada:** las filas y la cabecera no están en el recorrido, como las de la búsqueda.
- Sin lector, nada cambia.

**Never:** cambiar la lista, su orden o el mapa; rehacer la lista cuando llega el GPS (otra entrada de `deferred-work`); textos nuevos en pantalla.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Chip | TalkBack, doble toque en «Salud» | Lista de Salud; anuncio «Salud cerca, N resultados»; foco en el título |
| Vacía | Categoría sin resultados | Anuncio «No hay … cerca»; foco en el título |
| Sin GPS | Sin posición | El título se lee «… cerca, desde el centro del mapa» |
| Fila de lugar | Doble toque en una fila de lugar | Ficha abierta con el foco en su título |
| Quitar | Doble toque en «Quitar categoría» con Salud | Vuelven los chips; foco en el chip «Salud» |
| Plegar | Hoja plegada con la lista | Ni filas ni cabecera en el recorrido |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts/UI` salvo otra indicación.
- `PantallaMapa.cs`:
  - `DeclararBuscadorEnTalkBack` (l.395-418): los chips y «Encuentra cerca» son `accesibilidad.Texto` (l.408-413). Pasan a `Boton(..., accion: () => buscador.ElegirCategoria(c), visible igual, enModal: true)`.
  - `PintarCerca` (l.712-736): hoy solo da de baja `QuitarControles(buscador.Resultados)`. Declarar aquí, en el grupo `buscador.Resultados` y con `visible: enHoja`, el título (`buscador.TituloCerca`, `Texto`), la X (`buscador.QuitarCategoria`, `Boton` → `QuitarCategoria`) y las filas, como `DeclararResultadosEnTalkBack` (l.594-620). Después, `Reconstruir()` y `PedirFoco(buscador.TituloCerca, anuncio: …)`.
  - Anuncio: reutilizar `AnuncioResultados(n)` (l.577) para «N resultados», con la clave nueva «{0}, {1}» si hace falta, y el texto vacío de la categoría con 0.
  - `QuitarCategoria` (l.662): con la hoja abierta y el lector, foco al chip de la categoría quitada (`buscador.Chips`, o `buscador.Cerca` si era null). Con la píldora (2.4b) el foco sigue yendo a la píldora. `ElegirResultado` → `AbrirFicha` ya enfoca `ficha.Titulo` (l.905).
- `HojaBuscador.cs`: `TituloCerca` (l.133), `DesdeCentro` (l.135), `QuitarCategoria` (l.137), `Chips` (l.93), `Cerca` (l.97), `Filas` (l.117), `MostrarCerca` (l.522, reutiliza `FilaResultado`). No cambiar la pantalla.
- `ClavesTexto.cs` (+ `Todas`) y `Editor/PrepararInterfaz.cs`: solo si hace falta una clave para «{0}, {1}». Luego `PrepararInterfaz.Asegurar` por lotes.
- `Tests/EditMode/PruebasCerca.cs`: una prueba por fila de la matriz. Reutilizar `AbrirYElegir`, `NodoTalkBack`, `ActivarTalkBack`, `LectorForzado` y los oyentes de `AnuncioPedido` (como en `PruebasBusqueda.cs` l.248-271). La prueba de la fila de lugar comprueba `Accesibilidad.Enfocado == Ficha.Titulo`.

## Tasks & Acceptance

**Execution:**
- [x] `PantallaMapa.cs`: chips y «Encuentra cerca» como botones; cabecera y filas en `PintarCerca`; anuncio y foco; foco al quitar.
- [x] Textos (si hace falta la clave) y Localization.
- [x] `PruebasCerca.cs`: filas de la matriz; ajustar las pruebas que esperaban chips como texto.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se lanza la captura, entonces el registro no tiene errores.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | Arrastrar la hoja abierta con la lista y TalkBack (`PintarCerca(enfocar: false)` + `RecuperarFoco`) no tiene prueba | low | Ninguna prueba arrastra la hoja con una categoría; si el arrastre robara el foco nada fallaría | patch |
| 2 | `ElDobleToqueEnEncuentraCerca…` supone exactamente `MaximoResultados` filas | low | Depende de los datos; basta comparar con `hoja.Filas.Count` | patch |
| 3 | Al entrar se oye «Salud cerca» (foco) y luego «Salud cerca, N resultados» | low | Es lo que fija la intención congelada (anuncio «<Título>, N resultados» y foco al título); cambiarlo es cambiar el spec | rechazado (se comenta al usuario) |
| 4 | El anuncio sin posición no dice «desde el centro del mapa» | low | El título enfocado ya lo lee; el spec fija el anuncio | rechazado |
| 5 | Volver de otra ventana con la hoja abierta reenfoca el título / repinta dos veces | low | Solo si una ventana modal se cierra con la hoja aún desplegada: ficha y menú pliegan la hoja al abrirse | rechazado |
| 6 | `Buscar("")` con la lista activa roba el foco | false | `Buscar` solo se llama desde `QuitarCategoria` (cerca ya en false), el cambio del campo (escribir quita la categoría) y `AlDesplegarBuscador` con `EnBusqueda` | rechazado |
| 7 | Doble petición de foco con la píldora | false | Con la hoja abierta la píldora no está en el recorrido; el segundo `PedirFoco` cambia `Enfocado` y el primero no se ejecuta | rechazado |
| 8 | Prueba de un resultado solo formatea el texto | low | Construir datos con 1 resultado añade complejidad; la rama `n == 0` sí se prueba | rechazado |
| 9 | Textos en español fijos en las pruebas | low | Mismo criterio que en la 2.4b | rechazado |
| 10 | La prueba antigua reescrita prueba menos | low | Sigue comprobando una entrada por fila actual; los nodos viejos ya no existen | rechazado |
| 11 | Memoria del TFG, CLAUDE.md y sprint sin actualizar | low | Se actualizan al cerrar la historia (paso 5) | rechazado |
| 12 | Clave genérica «{0}, {1}» | low | La app solo está en español; traducir no está previsto | rechazado |
| 13 | `AbrirBuscadorConLector` con la hoja ya modal no enfoca el título | false | La cápsula solo está en el recorrido con la hoja plegada | rechazado |
| 14 | `PlegarBuscador` desplegada en el mismo fotograma anuncia la lista | low | Caso de un fotograma; el foco a la cápsula sustituye al del título y su anuncio | rechazado |
| 15 | `ExpandirBuscador` con la hoja no modal pierde el anuncio | false | «Expandir» solo está en el recorrido modal: la hoja ya es modal | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil con TalkBack: elegir Salud, recorrer la lista, abrir una ficha y quitar la categoría.
