---
title: 'Historia 4.6: tramos de gasto en hostelería'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-4-confirmar-lo-que-pague.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En bares y restaurantes no hay un precio único que confirmar; proponer un número sesga la respuesta (FR26).

**Approach:** En lugares de plantilla Hostelería, el paso del precio es `Tramo`: cinco opciones de gasto por persona sin ningún precio propuesto. Se guarda como rango `minCent`/`maxCent` con el mismo lote.

## Boundaries & Constraints

**Always:**
- **Tramos** (`TramosHosteleria`, Dominio, en `ParametrosNegocio`): menos de 10 € [1, 1000], 10–20 [1000, 2000], 20–30 [2000, 3000], 30–50 [3000, 5000], más de 50 [5000, null].
  - "Menos de 10 €" se guarda con `minCent: 1` (las reglas exigen > 0) y `maxCent: 1000`.
  - "Más de 50 €" con `maxCent: null` (la regla de la 4.1 lo permite solo si `minCent == 5000`).
- **Flujo:** para Hostelería, `FlujoPregunta` va HasIdo → Tramo (sin QueUsaste ni TeCosto). El aporte usa el concepto de gasto por persona del lugar (`conceptoId` del concepto de forma rango; si no lo hay, `gasto/persona`).
- **UI:** título "¿Cuánto gastaste por persona?"; cinco botones de 48 dp en columna con el texto del tramo; ninguno preseleccionado; sin mostrar el precio actual.
- **TalkBack:** grupo de opciones; cada una "De 10 a 20 euros por persona".
- Evento `AporteCreado(tipo: tramo)`; en la 4.11 cuenta como "confirmar" si el tramo contiene el rango mostrado y como "distinto" si no.

**Never:** sin regla deslizable en hostelería, sin precio sugerido.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Hostelería | Bar con precio 10–20 € | HasIdo → Tramo; no se muestra "10–20 €" |
| Elegir 10–20 | Toque | `forma: rango`, `minCent: 1000`, `maxCent: 2000` |
| Menos de 10 | Toque | `minCent: 1`, `maxCent: 1000` |
| Más de 50 | Toque | `minCent: 5000`, `maxCent: null` |
| Regla emulador | `maxCent: null` con `minCent: 3000` | Denegado |
| No hostelería | Clínica | Flujo de la 4.4 |
| Sin concepto rango | Bar sin conceptos | `conceptoId: gasto/persona` |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/TramoGasto.cs`, `UI/PasoTramo.cs`.
- `Dominio/ParametrosNegocio.cs` (tramos), `FlujoPregunta.cs`, `firebase/firestore.rules` (tramo abierto) y pruebas.
- Tests: `PruebasFlujoPregunta.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Tramos en el Dominio y en el flujo.
- [ ] Paso de UI con TalkBack.
- [ ] Ajuste de reglas para el tramo abierto y su prueba.
- [ ] Pruebas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "tramos".
