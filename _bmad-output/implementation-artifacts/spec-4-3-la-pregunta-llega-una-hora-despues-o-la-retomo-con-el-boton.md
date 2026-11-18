---
title: 'Historia 4.3: la pregunta llega una hora después, o la retomo con el botón €'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-2-la-visita-queda-verificada-al-acercarme.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La visita existe pero nadie pregunta por ella (FR23, FR29).

**Approach:** `ServicioVisitas` programa una notificación local a +1 h con el id de la visita (Mobile Notifications). Al tocarla se abre la pregunta de esa visita, aunque la app estuviera cerrada. Si a esa hora hay una ruta o otra pregunta abierta, la visita pasa a pendientes. Un botón € redondo en el mapa con punto rojo retoma las pendientes. La pregunta en sí es la 4.4: aquí se abre una hoja "Pregunta" con el nombre del lugar y "Más tarde".

## Boundaries & Constraints

**Always:**
- **Paquete** `com.unity.mobile.notifications` (2.3.2 o la compatible); adaptador `PriceMap.Adaptadores.Notificaciones` con el puerto `INotificaciones` (ampliando el de la 3.1): `Programar(id, titulo, texto, cuando, datos)`, `Cancelar(id)`, evento `NotificacionAbierta(datos)`, y `PermisoConcedido`.
- **Texto:** "¿Qué tal en Clínica Areas?" / "Cuéntanos cuánto pagaste y gana monedas." Canal "Preguntas" con importancia por defecto.
- **Al tocarla:** si la app estaba cerrada, se lee la intención inicial (`GetLastNotificationIntent`) tras el arranque; `ServicioVisitas` recibe `NotificacionAbierta(visitaId)` y abre la pregunta salvo que haya ruta o pregunta abierta (entonces pendiente).
- **Hora llegada sin tocarla:** las visitas cuyo `creado + 1 h` ya pasó y siguen `nueva` pasan a `pendiente` al abrir o volver a la app.
- **Notificaciones denegadas:** la visita va directa a pendientes, sin programar nada (UX-DR16).
- **Botón €** (UX-DR6): `BotonRedondo` con icono € en la pila derecha; punto rojo si hay pendientes (además del color, con número para no depender del color); desactivado sin pendientes, con TalkBack "Preguntas pendientes, 2" o "Sin preguntas pendientes, desactivado". Solo para clientes con sesión.
- **Una pendiente:** abre su pregunta. **Varias:** lista para elegir (nombre, fecha "hoy 17:30"). Pendientes caducan a las 24 h de la visita (regla de la 4.1) y desaparecen.
- **Estado observable:** `ServicioVisitas.Pendientes`, `PreguntaAbierta`, eventos.
- Al responder o descartar, se cancela la notificación si aún no ha saltado.

**Never:** sin el flujo de pasos (4.4), sin sonido (5.5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Visita creada | Permiso concedido | Notificación programada a +1 h con el id |
| Tocar notificación | App cerrada | Arranca y abre la pregunta de esa visita |
| Ruta en curso | Toque con ruta activa | Visita a pendientes, sin abrir |
| Otra pregunta abierta | Toque | Pendientes |
| Sin tocar | Abre la app a +2 h | Visita en pendientes; punto rojo |
| Denegadas | Sin permiso | Directa a pendientes |
| Una pendiente | Toque en € | Abre su pregunta |
| Varias | Toque en € con 2 | Lista para elegir |
| Ninguna | € | Desactivado |
| Caducada | Pendiente de hace 25 h | Desaparece |
| Negocio | Cuenta de negocio | Sin botón € |

</frozen-after-approval>

## Code Map

- Nuevos: `Adaptadores/Notificaciones/` (asmdef, `NotificacionesAndroid.cs`, `NotificacionesNulas.cs`), `UI/BotonPendientes.cs`, `UI/ListaPendientes.cs`, `UI/HojaPregunta.cs` (esqueleto).
- `Aplicacion/INotificaciones.cs` (3.1), `ServicioVisitas.cs` (4.2), `IVisitas.cs` (estado).
- `UI/BotonRedondo.cs` (icono €, punto con número), `UI/PantallaMapa.cs`.
- `Packages/manifest.json`. Tests: `PruebasPreguntas.cs` con reloj y notificaciones falsas.

## Tasks & Acceptance

**Execution:**
- [ ] Paquete y adaptador de notificaciones.
- [ ] Programar, abrir y pasar a pendientes en `ServicioVisitas`.
- [ ] Botón € con punto y lista de pendientes.
- [ ] Esqueleto de hoja de pregunta con "Más tarde".
- [ ] Pruebas de la matriz; captura "pendientes".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abre `pendientes.png`, entonces se ve el botón € con el punto y el número.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC; `CompilarApk`.

**Manual checks:**
- Con el tiempo reducido a 1 min en una build de depuración: notificación con la app cerrada que abre la pregunta.
