---
title: 'Historia 7.3: avísame si cambia'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 7'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El usuario tiene que mirar la ficha cada día para saber si un precio cambia (FR12, FR10, FR57).

**Approach:** Botón "Avísame si cambia" (o "Avísame cuando tenga precio") en la ficha. Guarda en `usuarios/<uid>/vigilados/<lugarId>` una huella del precio mostrado. Al abrir la app o volver a ella, `ServicioVigilancia` recalcula y, si la huella cambió, lanza una notificación local que abre la ficha. Lista en la ficha y en "Tu actividad" para desactivar.

## Boundaries & Constraints

**Always:**
- **Huella** (`HuellaPrecio`, Dominio, pura): texto estable con los precios mostrados de todos los conceptos y variantes del lugar (id + forma + céntimos), sin origen ni frescura. "Sin precio" = huella vacía.
- **Guardado:** `{lugarId, huella, nombre, creado}`; por `ServicioLugares` (dueño de los avisos activados según AD-18). Sin cuenta: `ExigirCuenta`.
- **Botón:** "Avísame si cambia" (con precio) o "Avísame cuando tenga precio" (sin precio); activado: "Te avisaremos" con estado seleccionado; tocar otra vez lo desactiva.
- **Comprobación:** al arrancar y en `OnApplicationFocus(true)`, como mucho una vez cada 15 min; espera a tener los aportes del servidor (lectura `GetSnapshotAsync(Source.Server)` de los lugares vigilados; sin conexión no comprueba). Compara huellas; si cambió:
  - Notificación local inmediata "Cambió el precio en Clínica Areas" / "Clínica Areas ya tiene precio"; al tocarla abre la ficha.
  - Actualiza la huella guardada (para no repetir).
- **Permisos:** sin permiso de notificaciones, en lugar de notificación se muestra un toast al volver y un punto en "Tu actividad".
- **Lista:** en la ficha (estado del botón) y en "Tu actividad" un bloque "Avisos activos" con nombre y botón "Desactivar".
- **Reglas:** `usuarios/{uid}/vigilados/{lugarId}`: solo el dueño; campos exactos.
- AD-7: sin push del servidor; solo comprobación al abrir o volver (Deferred de la arquitectura).

**Never:** sin comprobaciones en segundo plano.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Activar | Clínica Areas con precio | Guardado con huella; botón "Te avisaremos" |
| Sin precio | ChemaBarber | Botón "Avísame cuando tenga precio" |
| Cambia | Al volver, mandan los clientes con otro valor | Notificación "Cambió el precio…"; huella actualizada |
| Tiene precio | ChemaBarber recibe su primer aporte | "ChemaBarber ya tiene precio" |
| Igual | Solo cambia la frescura | Sin notificación |
| Frecuencia | Volver dos veces en 5 min | Una comprobación |
| Sin conexión | Al volver | No comprueba |
| Sin permiso | Notificaciones denegadas | Toast y punto en actividad |
| Desactivar | Desde actividad | Documento borrado; botón normal en la ficha |
| Tocar notificación | App cerrada | Abre la ficha del lugar |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/HuellaPrecio.cs`, `Aplicacion/ServicioVigilancia.cs`, `Adaptadores/Firebase/VigiladosFirestore.cs`, `UI/BotonAvisame.cs`.
- `Aplicacion/INotificaciones.cs` (4.3; notificación inmediata con datos de lugar), `ServicioLugares.cs`.
- `UI/FilaAcciones.cs`, `UI/PantallaActividad.cs`, `Arranque/Arranque.cs` (foco).
- `firebase/firestore.rules` y pruebas. Tests: `PruebasVigilancia.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Huella y pruebas.
- [ ] Guardar y quitar vigilados; reglas.
- [ ] Comprobación al abrir y volver con notificación.
- [ ] Botón en la ficha y bloque en actividad.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas.

**Manual checks:**
- Dos móviles: uno vigila, el otro aporta tres veces; el primero recibe la notificación al volver.
