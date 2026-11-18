---
title: 'Historia 4.1: reglas que protegen los aportes y el anonimato'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Antes de dejar aportar, el servidor tiene que garantizar que solo aporta quien visitó el sitio, una vez, sin dejar rastro del autor en la parte pública y sin poder modificar nada después (AD-4, AD-6, AD-17, FR30, FR39).

**Approach:** Ampliar `firebase/firestore.rules` con visitas, aportes públicos y copia privada, y probar cada caso con el emulador. Solo reglas y pruebas: la app las usa desde la 4.2.

## Boundaries & Constraints

**Always:**
- **Visita** `usuarios/{uid}/visitas/{visitaId}`:
  - `visitaId` = `<lugarId>_<AAAAMMDD>` (validado con regex y con `lugarId` del documento).
  - Campos exactos: `lugarId`, `creado` (= `request.time`), `aporteHecho` (= false al crear), `estado` ('nueva'|'pendiente'|'respondida'|'descartada').
  - Solo la crea un cliente (`esCliente()` de la 3.4). Solo se puede actualizar `aporteHecho` (false → true, únicamente en el lote del aporte) y `estado`.
- **Aporte público** `lugares/{lugarId}/aportes/{aporteId}`:
  - Campos exactos: `lugarId`, `conceptoId`, `forma` ('fijo'|'rango'|'desde'), `importeCent` (int > 0) o `minCent` (int > 0) y `maxCent` (int > `minCent` o null solo en el tramo abierto), `creado` (= `request.time`). Cualquier otro campo se rechaza; ningún dato del autor.
  - Solo se crea en un lote con la copia privada del mismo `aporteId`. La regla del público usa el uid de quien escribe (`request.auth.uid`) sin guardarlo: comprueba con `getAfter` que existe `usuarios/{uid}/aportes/{aporteId}` con los mismos `lugarId`, `conceptoId` y valores (ver la pregunta 1).
- **Copia privada y visita:** la regla de la copia privada lee su `visitaId` y comprueba con `get`/`getAfter` que esa visita es del mismo usuario y lugar, se creó hace < 24 h, tenía `aporteHecho == false` antes del lote y pasa a `true` en el mismo lote.
- **Copia privada** `usuarios/{uid}/aportes/{aporteId}`: los mismos campos más `visitaId`; solo el dueño; solo crear.
- **Inmutables:** update y delete denegados en aportes públicos y privados (AD-4).
- **Lectura:** aportes públicos legibles por cualquiera (incluso sin sesión); privados solo el dueño.
- **Pruebas** en `firebase/pruebas/aportes.test.js`, una por caso de la matriz.

**Never:** sin campos de autor en lo público, sin Cloud Functions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Crear visita | Cliente, id `clinica-areas_20261118` | Permitido |
| Visita id mal | `clinica-areas_2026` | Denegado |
| Segunda visita | Mismo lugar y día | Denegado (ya existe) |
| Negocio crea visita | `tipo: negocio` | Denegado |
| Aporte en lote | Visita < 24 h, `aporteHecho` false→true, copia privada | Permitido |
| Aporte sin visita | Lote sin visita | Denegado |
| Visita vieja | Visita de hace 25 h | Denegado |
| Segundo aporte | Visita con `aporteHecho` true | Denegado |
| Campo extra | Aporte con `uid` | Denegado |
| Importe 0 | `importeCent: 0` | Denegado |
| Modificar aporte | Update | Denegado |
| Borrar aporte | Delete | Denegado |
| Leer público | Sin sesión | Permitido |

</frozen-after-approval>

## Open Questions

1. **Cómo ligar el aporte público con la visita sin uid.** La propuesta usa el mismo `aporteId` en público y privado; la regla del público mira la copia privada del que escribe (`request.auth.uid`). El id es aleatorio, así que no revela al autor. A) Así (recomendado). B) Guardar un hash del uid en el público (contradice AD-17).

## Code Map

- `firebase/firestore.rules`: funciones `esCliente`, `visitaValida`, `aporteValido`, `camposExactos`.
- `firebase/pruebas/aportes.test.js`, `visitas.test.js`.
- Sin cambios en Unity.

## Tasks & Acceptance

**Execution:**
- [ ] Reglas de visitas, aportes públicos y privados.
- [ ] Pruebas de emulador de cada fila.

**Acceptance Criteria:**
- Dada la suite de reglas con el emulador, cuando se ejecuta, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `cd firebase/pruebas && npm test`.
