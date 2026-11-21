---
title: 'Historia 4.7: ser el primero en dar un precio'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-5-decir-otro-precio-con-la-regla.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En un lugar sin precio (p. ej. ChemaBarber) no hay nada que confirmar y el primer cliente no puede aportar (FR27).

**Approach:** Lugar sin ningún precio: HasIdo → `EligeConcepto` → `OtroPrecio` (regla de la 4.5). Conceptos sugeridos por tipo de lugar o uno nuevo con su unidad; `conceptoId` nuevo (slug); la recompensa marca "¡Pionero!".

## Boundaries & Constraints

**Always:**
- **Sugerencias** (`ConceptosSugeridos`, Dominio, tabla por tipo): peluquería/barbería → "Corte de pelo", "Arreglo de barba"; clínica dental → "Limpieza dental", "Empaste"; fisioterapia → "Sesión de fisioterapia"; gimnasio → "Cuota mensual", "Entrada de un día"; ocio → "Entrada"; por defecto → ninguna. Hasta 4 chips.
- **Concepto propio:** "Otro" abre nombre (obligatorio, 3–40 caracteres) y unidad opcional de lista cerrada ("sesión", "persona", "hora", "mes", "unidad", ninguna).
- **Id:** `slug(nombre)` (minúsculas, sin tildes, guiones); si choca con uno existente del lugar, se usa el existente. Cumple AD-11 (texto, nunca se reutiliza con otro significado).
- **Nombre y unidad:** el aporte público solo admite los campos de AD-17 → se guardan en `lugares/<id>/conceptos/<conceptoId>` (`nombre`, `unidad`, `creado`), en el mismo lote, solo si no existe. Reglas: solo crear, campos exactos, nombre 3–40, exige el mismo lote del aporte.
- **Pionero:** lugar sin ningún precio (ni web, ni declarado, ni aportes) al abrir la pregunta → evento `AporteCreado(tipo: pionero)`; el agradecimiento muestra "¡Pionero!" (recompensa completa en 4.11).
- La ficha (1.8) muestra los conceptos de `lugares/<id>/conceptos` junto a los empaquetados.

**Never:** sin editar ni borrar conceptos creados por clientes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Sin precio | ChemaBarber | HasIdo → EligeConcepto con "Corte de pelo" |
| Sugerido | "Corte de pelo" + 12 € | Concepto `corte-de-pelo` creado y aporte 1200 |
| Otro | "Degradado", unidad ninguna | Concepto `degradado` |
| Nombre corto | "ab" | Error junto al campo |
| Choque | "Corte de pelo" ya creado por otro | Usa el existente; no lo vuelve a crear |
| Pionero | Primer aporte del lugar | "¡Pionero!" |
| Segundo cliente | Ya hay un aporte | Flujo normal de la 4.4, sin pionero |
| Regla emulador | Concepto sin aporte en el lote | Denegado |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/ConceptosSugeridos.cs`, `Dominio/Slug.cs`, `UI/PasoEligeConcepto.cs`.
- `Dominio/FlujoPregunta.cs`, `Aplicacion/ServicioAportes.cs` (lote con concepto), `Adaptadores/Firebase/AportesFirestore.cs`, `IRepositorioLugares`/`ServicioLugares` (conceptos de clientes).
- `firebase/firestore.rules` (`conceptos`) y pruebas. Tests: `PruebasPionero.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Sugerencias, slug y pruebas.
- [ ] Paso de elegir concepto y "Otro".
- [ ] Lote con concepto nuevo y reglas.
- [ ] Pionero y conceptos de clientes en la ficha.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "pionero".
