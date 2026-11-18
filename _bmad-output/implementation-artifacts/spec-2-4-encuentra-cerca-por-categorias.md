---
title: 'Historia 2.4: encuentra cerca por categorías'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 2'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Las categorías de la hoja del buscador (2.2) no hacen nada. El usuario quiere ver lo que hay cerca de un tipo, también sin mirar el mapa (FR15, UX-DR17).

**Approach:** Al elegir una categoría, el mapa muestra solo los lugares y puntos de interés de esa categoría y la hoja enseña una lista por distancia: primero los lugares con precio y después los puntos de interés, sin repetidos. Una regla pura del Dominio decide qué tipos de lugar y qué categorías de POI entran en cada categoría.

## Boundaries & Constraints

**Always:**
- **Categorías** (`CategoriaCerca`, Dominio): restaurantes, bares, supermercados, farmacias, gasolineras, gimnasios, salud, cultura. Cada una mapea tipos de `Lugar` y valores de `CategoriaPoi`. La tabla vive en una regla pura (`ReglaCerca`) y se prueba.
- **Filtro del mapa:** mientras hay una categoría activa, se ignora el filtro "Precios" y solo se ven los marcadores y puntos de esa categoría. La píldora muestra un chip "Restaurantes ✕" para quitarla (✕ dibujado, no emoji). Quitarla devuelve el filtro anterior.
- **Lista:** cada fila con icono, nombre, tipo, distancia y precio resumido si lo tiene. Ordenada: lugares con precio por distancia, luego el resto por distancia. Sin repetir los POI que ya son lugares (regla de la 1.6).
- **Distancia** desde la posición del usuario o, sin posición, desde el centro del mapa; con un texto "desde el centro del mapa".
- **Alcance:** puntos de interés solo de las teselas cargadas; lugares, todos los de la app en 3 km.
- **TalkBack:** cada fila es un botón "Nombre, tipo, a 350 m, desde 20 €". Al tocar se abre su ficha (lugar) o se centra el punto (POI, ficha de punto libre en la 2.5) sin tocar el mapa.
- **Vacío:** "No hay <categoría> cerca" (UX-DR16).

**Never:** sin consultar servicios externos para categorías (Overpass, etc.).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Elegir salud | Categoría "salud" | Mapa: clínicas, fisios, dentistas y centros públicos; lista con primero los de precio |
| Supermercados | Categoría "supermercados" con teselas cargadas | Solo POI de super, por distancia |
| Repetido | POI y lugar con el mismo nombre a < 80 m | Una sola fila (el lugar) |
| Sin posición | Estado `Buscando` | Distancias desde el centro del mapa y su texto |
| Quitar | Toque en ✕ del chip | Vuelve el filtro "Precios" anterior |
| Vacío | Categoría sin resultados | "No hay gasolineras cerca" |
| TalkBack | Toque en una fila de lugar | Ficha abierta, foco en su título |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/CategoriaCerca.cs`, `Dominio/ReglaCerca.cs`, `Aplicacion/ServicioCerca.cs` (o ampliación de `ServicioFiltro`).
- `Scripts/UI/HojaBuscador.cs`, `PildoraPrecios.cs` (chip de categoría), `CapaMarcadores.cs` y `CapaPuntosInteres.cs` (filtrado), `AccesibilidadPantalla.cs`.
- `Scripts/Dominio/ReglaPuntosInteres.cs` y `CategoriaPoi.cs`: categorías de POI existentes.
- Tests: `PruebasCerca.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `CategoriaCerca` + `ReglaCerca` y pruebas de la tabla.
- [ ] Servicio con filtro de categoría que se superpone al filtro "Precios".
- [ ] Lista por distancia en la hoja y chip en la píldora.
- [ ] TalkBack y estado vacío; captura "cerca".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abre `cerca.png`, entonces se ven solo los lugares de salud y la lista ordenada.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "cerca".
