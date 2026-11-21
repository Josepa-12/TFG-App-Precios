---
title: 'Historia 5.4: mi reputación'
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

**Problem:** Todos los clientes pesan igual; la 7.1 necesita la reputación para el peso de los avisos (FR52, AD-14).

**Approach:** `ReglaReputacion` (Dominio): Nueva (< 5 aportes), Fiable (5–19), Experta (≥ 20). Se guarda en `usuarios/<uid>.reputacion` junto al contador `aportes`. El perfil la muestra junto al nivel.

## Boundaries & Constraints

**Always:**
- **Umbrales** en `ParametrosNegocio`: Fiable 5, Experta 20.
- **Contador:** `usuarios/<uid>.aportes: Increment(1)` en el mismo lote que el aporte (4.9). Regla: solo +1 y solo si el lote crea una copia privada de aporte nueva.
- **Reputación:** `reputacion` ('nueva'|'fiable'|'experta'), la actualiza `ServicioProgreso` al cruzar un umbral. Regla: debe coincidir con `aportes` resultante (comparaciones simples con 5 y 20); así la 7.1 valida el peso del aviso contra `reputacion`.
- **Perfil:** junto al nivel, etiqueta "Fiable" con icono (escudo) y texto; al tocar: "Tus avisos cuentan ×1,5. Llegarás a Experta con 20 aportes (te faltan 8)."
- **Recompensa:** al subir, "¡Ahora eres Fiable!".
- **Cuentas anteriores:** las reglas no cuentan documentos; el contador no se reconstruye (ver pregunta 1).

**Never:** sin bajar de reputación (no hay regla para ello).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Nueva | 4 aportes | Nueva |
| Fiable | Quinto aporte | `reputacion: fiable`; "¡Ahora eres Fiable!" |
| Experta | Vigésimo aporte | Experta |
| Perfil | Fiable con 12 aportes | "Fiable · te faltan 8 para Experta" |
| Regla incoherente | `reputacion: experta` con 6 aportes | Denegado |
| Regla contador | `aportes: Increment(5)` | Denegado |

</frozen-after-approval>

## Open Questions

1. **Cuentas con aportes previos.** A) No habrá: 4.x y 5.4 se construyen seguidas antes de las pruebas con usuarios; el contador nace en 0 al crear la cuenta (3.4) (recomendado). B) Migración manual por el administrador.

## Code Map

- Nuevos: `Dominio/Reputacion.cs`, `Dominio/ReglaReputacion.cs`, `UI/EtiquetaReputacion.cs`.
- `Aplicacion/ServicioProgreso.cs`, `ServicioAportes.cs` (incremento en el lote), adaptadores Firestore.
- `UI/PantallaPerfil.cs`, `UI/PantallaRecompensa.cs`; `Adaptadores/Firebase/PerfilFirestore.cs` (3.4: `aportes: 0`).
- `firebase/firestore.rules` y pruebas. Tests: `PruebasReputacion.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Regla y pruebas.
- [ ] Contador en el lote y reputación guardada; reglas.
- [ ] Perfil y recompensa.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas.
