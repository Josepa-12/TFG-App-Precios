---
title: 'Historia 5.3: ganar insignias'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 5'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No hay metas más allá del nivel (FR51, UX-DR12).

**Approach:** `ReglaInsignias` (Dominio, pura) decide las insignias ganadas desde las estadísticas del cliente (aportes, lugares distintos, días con aporte, valoraciones). `ServicioProgreso` las añade a `usuarios/<uid>.insignias` tras cada aporte confirmado. Sección "Insignias" en el perfil (ganadas y bloqueadas).

## Boundaries & Constraints

**Always:**
- **Insignias** (ids y condición, en `ParametrosNegocio`):
  - `primer-precio`: 1 aporte.
  - `explorador`: aportes en 5 lugares distintos.
  - `10-aportes`, `50-aportes`.
  - `racha-7`: aportes en 7 días naturales consecutivos (hora local).
  - `critico`: 10 valoraciones.
- **Estadísticas:** desde la copia privada `usuarios/<uid>/aportes` y las valoraciones propias (la 4.8 marca `valorada` en la visita; el contador `usuarios/<uid>.valoraciones` se incrementa en el mismo lote). La regla recibe la lista y "ahora".
- **Guardado:** `insignias: ArrayUnion(id)` por `ServicioProgreso`. Reglas: solo añadir ids de la lista cerrada; nunca quitar. (Si se merece lo comprueba el cliente: ver pregunta 1.)
- **Al ganar:** en la recompensa, "¡Nueva insignia: Explorador!" con su icono; TalkBack lo anuncia.
- **Perfil:** rejilla de 6 insignias con icono vectorial; ganadas a color con fecha; no ganadas en gris con candado y texto "Bloqueada" (no solo color). Al tocar, hoja con nombre, cómo se gana y progreso ("3 de 5 lugares").
- Negocio o sin cuenta: sin insignias.

**Never:** sin insignias secretas, sin monedas por insignia (no está en FR48).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Primer aporte | 1 aporte | `primer-precio` |
| Explorador | Aportes en 5 lugares | `explorador` |
| Mismo lugar | 5 aportes en 1 lugar | Sin explorador |
| Racha | Aportes del 1 al 7 | `racha-7` |
| Racha rota | Día 4 sin aporte | Sin racha |
| Crítico | 10 valoraciones | `critico` |
| Ya ganada | Gana otra vez la condición | No se duplica |
| Perfil | 2 ganadas | 2 a color, 4 con candado |
| Detalle | Toque en Explorador bloqueada | "Aporta en 5 sitios distintos · 3 de 5" |
| Regla id falso | `ArrayUnion('trampa')` | Denegado |

</frozen-after-approval>

## Open Questions

1. **¿Validar en el servidor que la insignia se merece?** Sin Cloud Functions las reglas no pueden contar aportes. A) Solo lista cerrada en reglas; la condición la comprueba el cliente (recomendado para el TFG). B) Contadores en `usuarios/<uid>` validados en cada lote (más reglas, más riesgo de errores).

## Code Map

- Nuevos: `Dominio/Insignia.cs`, `Dominio/ReglaInsignias.cs`, `UI/SeccionInsignias.cs`, `UI/IconosInsignia.cs`.
- `Aplicacion/ServicioProgreso.cs`, `Adaptadores/Firebase/ProgresoFirestore.cs`, `UI/PantallaRecompensa.cs`, `UI/PantallaPerfil.cs`.
- `firebase/firestore.rules` y pruebas. Tests: `PruebasInsignias.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaInsignias` y pruebas.
- [ ] Guardado y aviso en la recompensa.
- [ ] Sección del perfil y detalle con progreso.
- [ ] Reglas; captura "insignias".

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "insignias".
