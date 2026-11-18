---
title: 'Historia 6.2: registro anónimo para la evaluación'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 6'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Las métricas de la memoria (SM-2, SM-3, SM-4) necesitan datos de uso reales, registrados solo con consentimiento y sin identificar a nadie (FR59, AD-16).

**Approach:** Un puerto `IRegistroEventos` con adaptador de Firestore que escribe en `evaluacion/<idAnonimo>/eventos`. El consentimiento se pide en la bienvenida (3.1) y se puede cambiar en Ajustes > Privacidad. Los servicios publican eventos de dominio y un único `ServicioEvaluacion` los traduce a la lista cerrada.

## Boundaries & Constraints

**Always:**
- **Id anónimo:** GUID por instalación en `IPreferencias` (`evaluacion.id`), sin relación con el uid; se genera al aceptar. Retirar el consentimiento deja de registrar (no borra lo ya enviado; ver la pregunta 1).
- **Consentimiento:** casilla en el paso de permisos de la bienvenida (3.1) "Participar en la evaluación del TFG (anónimo)", desmarcada por defecto, con texto explicativo; y el mismo interruptor en Ajustes > Privacidad y ubicación. Sin consentimiento, `RegistroNulo`.
- **Eventos** (lista cerrada, `snake_case`): `visita_verificada`, `pregunta_respondida`, `pregunta_aplazada`, `aporte_creado`, `confirmacion`, `compra_tienda`.
  - Campos: `nombre`, `creado` (`ServerTimestamp`) y datos mínimos sin ubicación exacta ni uid: `plantilla` (para todos los que tienen lugar), `frescura` del precio mostrado en `confirmacion` y `aporte_creado` (para SM-4), `tipoAporte` en `aporte_creado`, `cosmeticoId` en `compra_tienda`. Nunca `lugarId` (permitiría reidentificar con la visita) — ver la pregunta 2.
- **Fuente única:** `ServicioEvaluacion` se suscribe a `VisitaCreada`, `PreguntaRespondida`, `PreguntaAplazada`, `AporteCreado`, `CompraTienda`; ningún otro módulo llama a `IRegistroEventos`.
- **Reglas:** `evaluacion/{id}/eventos/{e}`: solo crear, con `nombre` en la lista, campos permitidos por evento y `creado == request.time`; lectura, update y delete denegados desde la app. No exige sesión (también participan usuarios sin cuenta).
- Sin conexión: cola de Firestore.

**Never:** sin uid, sin coordenadas, sin `lugarId`, sin leer eventos desde la app.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Sin consentimiento | Visita creada | Nada registrado |
| Con consentimiento | Visita creada | `visita_verificada` con `plantilla` |
| Aplazar | "Más tarde" | `pregunta_aplazada` |
| Confirmar | Confirmación de precio antiguo | `confirmacion` con `frescura: antiguo` y `aporte_creado` |
| Compra | Dorada | `compra_tienda` con `cosmeticoId` |
| Retirar | Desactivar en Ajustes | Deja de registrar |
| Regla nombre | `nombre: otro` | Denegado |
| Regla leer | Leer eventos | Denegado |
| Regla campo | Evento con `uid` | Denegado |

</frozen-after-approval>

## Open Questions

1. **Retirar el consentimiento.** A) Deja de registrar; lo enviado sigue (anónimo e irreconocible) y se explica en el texto (recomendado). B) Borrar lo enviado: la app no puede (no lee ni borra), lo haría el administrador con el id que el usuario le diera.
2. **¿`lugarId` en los eventos?** A) No (recomendado, más privado; SM-2 a SM-4 no lo necesitan). B) Sí, para analizar por lugar.

## Code Map

- Nuevos: `Aplicacion/IRegistroEventos.cs`, `Aplicacion/EventoEvaluacion.cs`, `Aplicacion/ServicioEvaluacion.cs`, `Adaptadores/Firebase/RegistroFirestore.cs`, `Aplicacion/RegistroNulo.cs`.
- `UI/PantallaBienvenida.cs` (casilla), `UI/PantallaAjustes.cs` (interruptor).
- `Arranque/Arranque.cs`: suscripciones.
- `firebase/firestore.rules` y pruebas. Tests: `PruebasEvaluacion.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Puerto, servicio y adaptador.
- [ ] Consentimiento en bienvenida y Ajustes.
- [ ] Reglas y pruebas.
- [ ] Pruebas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas.
