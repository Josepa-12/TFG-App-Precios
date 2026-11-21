---
title: 'Historia 9.1: montar un plan de tarde'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 9'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No se puede organizar una salida con varios sitios (FR54, FR10, UX-DR13).

**Approach:** "Añadir al plan" en la fila de acciones de la ficha añade el lugar como parada. Botón "Plan" en el mapa abre la tarjeta del plan (paradas ordenables, quitar, personas, qué se usará). `ServicioPlan` es dueño de `usuarios/<uid>.plan` (AD-18).

## Boundaries & Constraints

**Always:**
- **Modelo** (`Plan`, Dominio): `personas` (1–20, 1 por defecto), lista ordenada de `Parada` (`lugarId`, lista opcional de `conceptoId` elegidos). Un plan por usuario; máximo 8 paradas; un lugar no se repite.
- **Guardado:** `usuarios/<uid>.plan` como mapa completo con actualización por campo (`Update("plan", …)`, AD-18). Sin cuenta: `ExigirCuenta`. Negocio: sin plan.
- **Ficha:** "Añadir al plan" / "En tu plan" (seleccionado; tocar lo quita). Toast "Añadido al plan · Ver plan".
- **Botón "Plan"** en el mapa (pila izquierda bajo la píldora), solo con paradas, con su número.
- **Tarjeta del plan** (UX-DR13): hoja "Tu plan"; selector de personas (− / +, 48 dp); paradas con número, nombre, tipo y precio resumido; asa para arrastrar; "Subir"/"Bajar" visibles en cada fila (alternativa sin arrastre, las que usa TalkBack); "Quitar"; "¿Qué usarás?" despliega los conceptos del lugar con casillas. "Vaciar plan" con confirmación.
- **Mapa:** con la tarjeta abierta, cada parada muestra su número encima del marcador.
- **Sin conexión:** cambios en la cola de Firestore.
- TalkBack: "Parada 2 de 4, Bar Pepe" con acciones Subir/Bajar/Quitar.

**Never:** sin gasto (9.2), sin ruta (9.3), sin varios planes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Añadir | "Añadir al plan" en Bar Pepe | Parada 1; aparece botón "Plan · 1" |
| Repetido | Añadir otra vez | Botón ya en "En tu plan"; tocar lo quita |
| Máximo | Novena parada | "El plan admite 8 paradas" |
| Subir | "Subir" en la parada 3 | Pasa a 2 |
| Arrastrar | Parada 1 al final | Orden actualizado |
| Personas | + tres veces | 4 personas |
| Mínimo | − con 1 | Desactivado |
| Qué usarás | Marca "Menú" | Guardado en la parada |
| Sin cuenta | Añadir | Hoja de cuenta y vuelta |
| Vaciar | Confirmar | Plan vacío; botón "Plan" desaparece |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/Plan.cs`, `Dominio/Parada.cs`, `Dominio/ReglaPlan.cs` (añadir, mover, límites), `Aplicacion/IPlan.cs`, `Aplicacion/ServicioPlan.cs`, `Adaptadores/Firebase/PlanFirestore.cs`, `UI/TarjetaPlan.cs`, `UI/BotonPlan.cs`.
- `UI/FilaAcciones.cs`, `UI/CapaMarcadores.cs` (números), `UI/PantallaMapa.cs`.
- `firebase/firestore.rules` (forma del plan) y pruebas. Tests: `PruebasPlan.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Modelo y `ReglaPlan` con pruebas.
- [ ] Servicio y adaptador; reglas.
- [ ] Botón en la ficha y botón "Plan".
- [ ] Tarjeta con orden, personas y conceptos; TalkBack; captura "plan".

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "plan".
