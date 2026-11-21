---
title: 'Historia 8.1: reclamar y verificar mi local'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 8'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Un dueño no puede demostrar que el local es suyo (FR43, FR10).

**Approach:** Script del administrador siembra `contactosLugares/<id>` con el correo público de cada lugar. En la ficha, "¿Es tu negocio?" (o "Reclamar este local" con cuenta de negocio) crea `propiedad/<lugarId>`: verificada si el correo verificado de la cuenta coincide con el contacto; si no, pendiente de aprobación manual en la consola de Firebase.

## Boundaries & Constraints

**Always:**
- **Sembrado** (`firebase/sembrar/`, Node + Admin SDK): lee los datos base (`unity/PriceMap/Assets/Datos/Resources/*.json`), extrae `email` (de OSM o de la web: el campo que ya tengan los datos o una tabla `contactos.json` a mano) y escribe `contactosLugares/<id> = {email}` en minúsculas. Idempotente. Reglas: solo `get` de un documento (no `list`) con sesión; escritura denegada a la app.
- **Correo verificado:** la cuenta necesita `email_verified` (Google lo da; enlace de correo también). Para cambiar a otro correo, "Reenviar enlace" envía verificación al correo nuevo (`SendEmailVerificationBeforeUpdatingEmail`); si caduca, se pide otro (UX-DR16).
- **Propiedad** `propiedad/{lugarId}`: `{uid, estado: 'verificada'|'pendiente', creado}`.
  - Crear `estado: verificada` solo si `request.auth.token.email_verified == true`, `request.auth.token.email.lower() == get(contactosLugares/lugarId).data.email` y el usuario es negocio.
  - Crear `estado: pendiente` siempre que no exista propiedad (un solo reclamante a la vez).
  - El dueño pendiente puede pasar a verificada (update) si ahora cumple la condición. Aprobación manual solo por el administrador (consola).
- **Cuenta no negocio:** "¿Es tu negocio?" explica que hace falta cuenta de negocio y ofrece cerrar sesión y crear otra (el tipo no cambia, 3.4).
- **Mensajes:** verificada → "¡Listo! Ya puedes gestionar Clínica Areas." Correo distinto → "Tu correo no coincide con el del local (c***@clinicaareas.es). Usa ese correo o espera la revisión manual." Sin correo público → "Revisaremos tu solicitud a mano en unos días."
- Con propiedad verificada, la ficha no muestra "¿Es tu negocio?".

**Never:** sin verificación por SMS ni por carta, sin panel web de administración.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Coincide | Negocio con `info@clinicaareas.es` verificado | `propiedad` verificada |
| Mayúsculas | `Info@ClinicaAreas.es` | Verificada |
| No coincide | Otro correo | Pendiente + mensaje |
| Sin contacto | Lugar sin correo público | Pendiente + mensaje manual |
| Ya reclamado | Propiedad de otro | "Este local ya está reclamado" |
| Cliente | "¿Es tu negocio?" | Explicación de cuenta de negocio |
| Regla falsa | Crear verificada con correo distinto | Denegado |
| Regla list | `list contactosLugares` | Denegado |
| Sembrado dos veces | Ejecutar el script otra vez | Mismos documentos |

</frozen-after-approval>

## Open Questions

1. **De dónde salen los correos** (los datos base pueden no tenerlos). A) Tabla `firebase/sembrar/contactos.json` a mano con los conocidos (recomendado). B) Extraerlos automáticamente de OSM (`contact:email`/`email`) en el exportador de datos.

## Code Map

- Nuevos: `firebase/sembrar/` (`package.json`, `sembrar.js`, `contactos.json`, `LEEME.md`), `Dominio/Propiedad.cs`, `Aplicacion/INegocio.cs`, `Aplicacion/ServicioNegocio.cs`, `Adaptadores/Firebase/NegocioFirestore.cs`, `UI/HojaReclamar.cs`.
- `Aplicacion/ServicioCuentas.cs` (correo verificado, cambiar correo), `UI/FilaAcciones.cs`.
- `firebase/firestore.rules` y pruebas. Tests: `PruebasNegocio.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Script de sembrado.
- [ ] Reglas de `contactosLugares` y `propiedad` con pruebas.
- [ ] Servicio y hoja de reclamar con sus mensajes.
- [ ] "¿Es tu negocio?" en la ficha.

**Acceptance Criteria:**
- Dada la suite de reglas y la EditMode, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `npm test` de reglas; pruebas EditMode por lotes; sembrado contra el emulador.
