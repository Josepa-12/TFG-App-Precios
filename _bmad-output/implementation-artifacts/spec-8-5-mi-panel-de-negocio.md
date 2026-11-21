---
title: 'Historia 8.5: mi panel de negocio'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 8'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El dueño no ve el estado de sus precios ni lo que dicen los clientes (FR47, UX-DR16).

**Approach:** "Mi negocio" en el perfil de cuenta de negocio (entrada reservada en la 3.5) abre el panel: local, precios con su estado, avisos recibidos y ofertas, con accesos al editor (8.3) y a publicar ofertas (8.4). Sin verificar, explica qué falta y no deja editar.

## Boundaries & Constraints

**Always:**
- **Cabecera:** nombre, tipo y dirección del local; estado de la propiedad ("Verificado" / "Pendiente de verificar").
- **Precios:** por concepto, el precio mostrado (calculado por el Dominio como en la ficha) y su estado: "Declarado por ti", "Según 4 clientes", "Publicado en su web", "En revisión" (con el número de puntos) o "Sin precio". Frescura con su punto.
- **Avisos recibidos:** agrupados por concepto con tipo y fecha ("El precio ha cambiado · hace 2 días"), sin datos de quién avisó. Últimos 6 meses.
- **Ofertas:** vigentes y caducadas, con "Borrar".
- **Acciones:** "Editar precios" (8.3) y "Publicar oferta" (8.4).
- **Sin verificar** (UX-DR16): "Para gestionar tu local, confirma el enlace que enviamos a tu correo o espera la revisión manual." con "Reenviar enlace" (8.1). Sin acciones de edición.
- **Sin local:** "Aún no tienes un local. Reclama el tuyo desde su ficha o dalo de alta." con "Dar de alta" (8.2).
- Solo lectura de servicios (AD-15); la UI no ve Firestore.
- TalkBack: secciones con encabezado; cada precio leído completo.

**Never:** sin estadísticas de visitas (privadas de los clientes), sin responder a avisos.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Verificado | Local con 3 conceptos | Lista con estados y acciones |
| Mandan clientes | Concepto con 4 aportes | "Según 4 clientes" |
| En revisión | 3 puntos de avisos | "En revisión (3 puntos)" |
| Avisos | 2 avisos | Lista sin autor |
| Pendiente | Propiedad pendiente | Explicación y "Reenviar enlace"; sin editar |
| Sin local | Negocio sin propiedad | Explicación y "Dar de alta" |
| Cliente | Cuenta de cliente | No ve "Mi negocio" |

</frozen-after-approval>

## Code Map

- Nuevos: `UI/PantallaPanelNegocio.cs` (+ uxml/uss).
- `Aplicacion/ServicioNegocio.cs` (estado del local, avisos, ofertas), `ServicioLugares.cs`, `Dominio/CalculadoraPrecio.cs`, `ReglaRevision.cs` (7.2).
- `UI/PantallaPerfil.cs` (entrada). Tests: `PruebasPanelNegocio.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Estado del panel en el servicio con pruebas.
- [ ] Pantalla con secciones, estados vacíos y accesos.
- [ ] Capturas "panel" y "panel-pendiente".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; capturas de PC.
