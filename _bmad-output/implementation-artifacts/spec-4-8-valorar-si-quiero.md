---
title: 'Historia 4.8: valorar si quiero'
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

**Problem:** En hostelería y ocio el cliente puede querer opinar, pero nunca obligado (FR28).

**Approach:** Tras el aporte, en lugares de Hostelería y Ocio, un paso opcional `Valorar` con tres criterios de 1 a 5 estrellas y "Saltar". La valoración es anónima, se guarda en `lugares/<id>/valoraciones` en un lote con la marca de la visita.

## Boundaries & Constraints

**Always:**
- **Criterios** (`CriteriosValoracion`, Dominio): Hostelería → calidad, servicio, ambiente; Ocio → instalaciones, atención, calidad-precio; Servicio local → ninguno (el paso no aparece).
- **UI:** tres filas con cinco estrellas vectoriales de 48 dp por zona táctil (dibujadas con `Painter2D`, no emoji); "Enviar valoración" activo cuando hay al menos un criterio puntuado; "Saltar" siempre.
- **Guardado:** `lugares/<id>/valoraciones/<vid>` con exactamente `lugarId`, `criterios` (mapa criterio → 1..5, solo los puntuados) y `creado`; sin autor. En lote con `visitas/<vid>.valorada = true` (reglas: visita propia, < 24 h, `aporteHecho == true` y `valorada == false`). Id aleatorio, sin relación con el uid.
- **Saltar** no escribe nada. Evento `Valorado` solo si se envía (recompensa +5/+10 en la 4.11).
- **TalkBack:** cada criterio es un control ajustable "Calidad, 3 de 5 estrellas" (subir/bajar 1); además cada estrella es tocable.
- La ficha no muestra valoraciones todavía (no hay FR para mostrarlas).

**Never:** sin comentarios de texto, sin valoración obligatoria.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Hostelería | Tras el tramo | Paso con calidad, servicio y ambiente |
| Ocio | Tras el aporte | Instalaciones, atención y calidad-precio |
| Servicio local | Clínica | Sin paso de valoración |
| Saltar | Toque | Nada guardado; fin |
| Enviar parcial | Solo calidad 4 | `criterios: {calidad: 4}` |
| Sin estrellas | Nada elegido | "Enviar" desactivado |
| TalkBack | Gesto subir en servicio | 4 de 5 estrellas |
| Regla doble | Segunda valoración de la visita | Denegado (emulador) |
| Regla valor 6 | `calidad: 6` | Denegado |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/CriteriosValoracion.cs`, `Dominio/Valoracion.cs`, `UI/Estrellas.cs`, `UI/PasoValorar.cs`.
- `Dominio/FlujoPregunta.cs` (paso tras el aporte), `Aplicacion/ServicioAportes.cs`, `IAportes.cs`, adaptador Firestore.
- `firebase/firestore.rules` (`valoraciones`, `visitas.valorada`) y pruebas. Tests: `PruebasValoracion.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Criterios y flujo con pruebas.
- [ ] Control de estrellas con TalkBack.
- [ ] Lote y reglas de valoraciones.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "valorar".
