---
title: 'Historia 4.9: mi aporte llega aunque no tenga conexión, y lo ven todos'
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

**Problem:** Sin conexión el lote del aporte no se valida en el servidor, y los demás no ven aportes nuevos porque la ficha solo lee datos empaquetados (FR36, AD-8, NFR-2).

**Approach:** Todos los aportes (4.4–4.7) usan un único método de lote. Sin conexión queda en la cola de Firestore y la recompensa sale "pendiente de enviar"; al sincronizar, toast de confirmación o de rechazo. `ServicioLugares` escucha en tiempo real los aportes de lugares visibles o abiertos: la ficha se actualiza en < 1 min.

## Boundaries & Constraints

**Always:**
- **Un solo camino:** `IAportes.CrearEnLote(Aporte, Visita, ConceptoNuevo?, Valoracion?)`; ninguna otra ruta escribe aportes. Prueba que recorre los usos.
- **Sin conexión:** no se espera `WriteBatch.CommitAsync` para mostrar la pantalla final; el servicio guarda el estado "enviando" y lo resuelve al terminar el `Task` (Firestore lo completa al reconectar, también tras reiniciar gracias a la persistencia).
  - Mientras: recompensa con "Se enviará cuando tengas conexión" (UX-DR16).
  - Al completarse: toast "Tu precio ya está publicado" (UX-DR15).
  - Rechazo del servidor (visita caducada): toast "No se pudo publicar tu precio: la visita ha caducado"; sin monedas (la 4.11 suma tras la confirmación).
- **Reinicio sin conexión:** el `Task` se pierde; al arrancar, `ServicioAportes` mira con un listener de `usuarios/<uid>/aportes` y `SnapshotMetadata.HasPendingWrites` cuáles siguen pendientes y espera a que dejen de estarlo.
- **Tiempo real:** `IRepositorioAportes.Escuchar(lugarId)`, listener de `lugares/<id>/aportes` (solo últimos 6 meses) para la ficha abierta y los lugares visibles, máximo 30 listeners activos; los aportes se mezclan con las fuentes del Dominio (`FuentesPrecio`) y el precio se recalcula al leer (AD-3). Regla de cálculo: 4.10.
- **Anonimato:** la parte pública nunca incluye datos del autor (prueba con el lote real).

**Never:** sin cola propia fuera de Firestore (AD-8), sin guardar el precio calculado.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Con conexión | Aporte enviado | Recompensa normal; toast de publicado |
| Sin conexión | Aporte | Recompensa con "Se enviará…"; nada perdido |
| Reconectar | Lote pendiente | Toast "Tu precio ya está publicado" |
| Rechazo | Visita caducada al sincronizar | Toast de error; sin monedas |
| Reinicio offline | Cerrar y abrir sin red | Sigue pendiente; se confirma al reconectar |
| Otro usuario | Ficha abierta del lugar | Ve el aporte en < 1 min |
| Lugar fuera de vista | Más de 30 lugares visibles | Solo 30 listeners (los más cercanos al centro) |

</frozen-after-approval>

## Code Map

- `Aplicacion/IAportes.cs`, `ServicioAportes.cs` (estado de envío), nuevo `Aplicacion/IRepositorioAportes.cs`.
- `Adaptadores/Firebase/AportesFirestore.cs` (lote único, `HasPendingWrites`, listeners).
- `Aplicacion/ServicioLugares.cs` y `Dominio/FuentesPrecio.cs` (aportes como fuente).
- `UI/HojaPregunta.cs` (texto pendiente), `UI/Toast.cs`, `UI/FichaLugar.cs` (refresco).
- Tests: `PruebasAportes.cs` con adaptador falso que simula pendiente, éxito y rechazo.

## Tasks & Acceptance

**Execution:**
- [ ] Lote único y prueba de que no hay otra ruta.
- [ ] Estados de envío, toasts y reinicio sin conexión.
- [ ] Listeners de aportes y mezcla en `FuentesPrecio`.
- [ ] Pruebas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes.

**Manual checks:**
- Modo avión, aportar, cerrar y abrir, quitar el modo avión; otro móvil con la ficha abierta ve el cambio.
