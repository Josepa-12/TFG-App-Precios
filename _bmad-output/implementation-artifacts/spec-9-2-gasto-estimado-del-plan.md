---
title: 'Historia 9.2: gasto estimado del plan'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 9'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9-1-montar-un-plan-de-tarde.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El plan no dice cuánto va a costar la tarde (FR55, AD-10).

**Approach:** `CalculadoraPlan` (Dominio, pura) suma en céntimos el precio mostrado de lo que se usará en cada parada según plantilla y forma. Total como rango (mín.–máx.) con avisos de paradas sin precio y de precios "desde", abajo en la tarjeta.

## Boundaries & Constraints

**Always:**
- **Por parada:**
  - Conceptos elegidos o, sin elección, el primero del lugar (o el de forma rango en hostelería).
  - Hostelería (rango por persona): [min × personas, max × personas]; tramo abierto (máx. null) → máximo abierto y total marcado "o más".
  - Por persona (entradas, servicios, unidad "persona"/"sesión"): importe × personas en mín. y máx.
  - Por grupo (unidad "pista", "hora de pista", "aparcamiento" o concepto marcado en los datos como `porGrupo`): importe una vez.
  - "Desde X": suma X a mín. y máx. y marca la parada "desde" (el total es mínimo).
  - "Consultar" o sin precio: no suma; parada marcada "sin precio".
- **Total:** "Entre 45 y 80 €" o "45 €" si mín = máx; avisos "No incluye 1 parada sin precio" y "Algunos precios son 'desde'" (FR55). Formato es-ES de `FormatoPrecio`.
- **Variantes:** la vigente a la hora actual (no se planifica la hora en esta versión).
- **Precio usado:** el de la ficha (web, declarado o clientes), calculado al leer; nunca se guarda (AD-3), salvo el último total mostrado para el modo sin conexión de la 9.3 (local, en `IPreferencias`).
- **"Por grupo":** tabla de unidades en `ParametrosNegocio` (ver pregunta 1).
- Prueba EditMode de cada fila (AD-14).

**Never:** sin propinas ni transporte en el gasto.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Hostelería | 10–20 €/persona, 4 personas | 40–80 € |
| Entradas | 8 € por persona, 4 | 32 € |
| Pista | 20 € por hora de pista, 4 | 20 € |
| Mezcla | Las tres anteriores | Entre 92 y 132 € |
| Sin precio | Una parada sin precio | Total sin ella + aviso |
| Desde | Desde 15 € por persona, 2 | +30 € y aviso "desde" |
| Tramo abierto | Más de 50 €, 2 | "Desde 100 € o más" |
| Consultar | "Consultar" | No suma; aviso sin precio |
| Variante | Pista "con luz" vigente 24 € | Usa 24 € |

</frozen-after-approval>

## Open Questions

1. **¿Cómo se sabe que un concepto es "por grupo"?** A) Por su unidad (lista en `ParametrosNegocio`: "pista", "hora de pista", "aparcamiento", "reserva") (recomendado). B) Campo nuevo `porGrupo` en los datos y en las declaraciones.

## Code Map

- Nuevos: `Dominio/CalculadoraPlan.cs`, `Dominio/GastoPlan.cs`.
- `Dominio/CalculadoraPrecio.cs`, `ReglaVariantes.cs`, `ParametrosNegocio.cs`; `UI/TarjetaPlan.cs` (total y avisos); `UI/FormatoPrecio.cs`.
- Tests: `PruebasGastoPlan.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `CalculadoraPlan` y pruebas de la matriz.
- [ ] Total y avisos en la tarjeta; guardado local del último total.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con céntimos exactos.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "plan" con el total.
