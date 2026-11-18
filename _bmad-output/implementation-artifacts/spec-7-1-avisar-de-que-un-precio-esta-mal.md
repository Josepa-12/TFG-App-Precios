---
title: 'Historia 7.1: avisar de que un precio está mal'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 7'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-5-4-mi-reputacion.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Si un precio está mal, nadie puede avisar (FR34, FR10).

**Approach:** En la ficha, "¿Algún precio está mal?" (fila de acciones o al pie de los conceptos) abre una hoja: elegir el concepto (o "todo el negocio"), y el motivo: "El precio ha cambiado", "Ya no se ofrece este servicio" o "El negocio ha cerrado". Se crea un aviso anónimo en `lugares/<id>/avisos` con peso según la reputación (×1, ×1,5, ×2). Las reglas validan el peso contra `usuarios/<uid>.reputacion`.

## Boundaries & Constraints

**Always:**
- **Aviso** (AD-17): exactamente `lugarId`, `conceptoId` (o `*` para "el negocio ha cerrado"), `tipo` ('precio_cambiado'|'no_se_ofrece'|'cerrado'|'oferta_caducada' para la 8.4), `peso` (1|1.5|2), `creado` (= `request.time`). Sin autor.
- **Peso** (`ReglaPeso`, Dominio): Nueva 1, Fiable 1,5, Experta 2 (`ParametrosNegocio`).
- **Reglas:**
  - Solo clientes (`esCliente()`).
  - `peso` coincide con `get(usuarios/uid).reputacion`.
  - Un aviso por usuario, concepto y día: copia privada `usuarios/<uid>/avisos/<lugarId>_<conceptoId>_<AAAAMMDD>` en el mismo lote (`getAfter`), que no puede existir antes. Mismo `avisoId` en público y privado (patrón de la 4.1).
  - Inmutable (AD-4).
- **Sin cuenta:** `ExigirCuenta` (3.3). **Negocio:** no ve la acción.
- **Confirmación:** toast "Gracias, lo revisaremos con otros avisos" y anuncio.
- **Sin conexión:** cola de Firestore.
- TalkBack: hoja modal con foco al título; opciones como botones de radio.

**Never:** sin texto libre en el aviso, sin estado "en revisión" (7.2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Avisar | Fiable, "Sesión" → precio cambiado | Aviso con `peso: 1.5` |
| Cerrado | "El negocio ha cerrado" | `conceptoId: *` |
| Sin cuenta | Toque | Hoja de cuenta y vuelta a la acción |
| Negocio | Cuenta de negocio | Sin acción |
| Segundo aviso | Mismo concepto el mismo día | Mensaje "Ya avisaste hoy de este precio" |
| Regla peso falso | Nueva con `peso: 2` | Denegado |
| Regla autor | Campo `uid` | Denegado |
| Regla modificar | Update | Denegado |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/Aviso.cs`, `Dominio/TipoAviso.cs`, `Dominio/ReglaPeso.cs`, `Aplicacion/IAvisos.cs`, `Aplicacion/ServicioAvisos.cs`, `Adaptadores/Firebase/AvisosFirestore.cs`, `UI/HojaAvisar.cs`.
- `UI/FichaLugar.cs` / `FilaAcciones.cs`.
- `firebase/firestore.rules` y pruebas. Tests: `PruebasAvisos.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Modelo y peso con pruebas.
- [ ] Puerto, servicio y adaptador con lote.
- [ ] Reglas y pruebas.
- [ ] Hoja de avisar en la ficha; captura "avisar".

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "avisar".
