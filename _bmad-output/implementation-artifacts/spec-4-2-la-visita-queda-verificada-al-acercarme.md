---
title: 'Historia 4.2: la visita queda verificada al acercarme'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-2-7-navegar-giro-a-giro.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-1-reglas-que-protegen-los-aportes-y-el-anonimato.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La navegación no deja constancia de que el cliente fue a un lugar, y sin visita no se puede aportar (FR22, AD-9).

**Approach:** `ServicioVisitas` (Aplicación), único que crea visitas, escucha `RutaIniciada`, `TiempoRestanteCambiado` y `RutaTerminada` de `ServicioNavegacion`. Ruta a un lugar con plantilla de precio y ≤ 5 min restantes → visita verificada en `usuarios/<uid>/visitas/<lugarId>_<AAAAMMDD>` por el puerto `IVisitas`. Máx. una por ruta.

## Boundaries & Constraints

**Always:**
- **`ReglaVisita`** (Dominio, pura): crea si el destino es lugar con `Plantilla` de precio (no centro sanitario público, ni punto libre ni dirección), cliente con sesión, restante ≤ 300 s (`ParametrosNegocio`) y aún no creada en esta ruta.
- **Persistencia:** escritura en la cola de Firestore (si la app se cierra justo después, se envía al volver). Además, `ServicioVisitas` guarda en `IPreferencias` la visita pendiente por si el proceso muere antes de que Firestore la guarde en disco, y la reintenta al arrancar.
- **Fecha del id:** día local de "ahora" (AD-12). Si ya existe la del día (otra ruta), no se crea otra y no es error.
- Creada, ni terminar la ruta ni cerrar la app la deshace. Terminar antes de 5 min restantes no crea nada.
- **Negocio o sin cuenta:** sin visita y sin aviso.
- **Evento de salida:** `VisitaCreada(visita)` para la 4.3 y la 6.2 (`visita_verificada`).
- `ServicioNavegacion` no conoce `ServicioVisitas`; los conecta el Arranque (AD-9).

**Never:** sin notificación ni pregunta (4.3); ninguna otra clase crea o descarta visitas.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Acercarse | Ruta a Clínica Areas, restante 301 → 299 s | Visita `clinica-areas_20261118` creada una vez |
| Sigue bajando | 299 → 120 s | No crea otra |
| Terminar antes con visita | Termina a 200 s | La visita sigue |
| Terminar sin visita | Termina a 400 s | Sin visita |
| Punto libre | Ruta a una dirección | Sin visita |
| Público | Centro de salud | Sin visita |
| Negocio | Cuenta de negocio | Sin visita |
| Sin cuenta | Sin sesión | Sin visita |
| Ya existe | Segunda ruta el mismo día | No se crea; sin error |
| Proceso muere | Visita en preferencias sin confirmar | Se reintenta al arrancar |
| Ruta empezada a < 5 min | Restante inicial 180 s | Visita creada al empezar |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/ReglaVisita.cs`, `Dominio/Visita.cs`, `Aplicacion/IVisitas.cs`, `Aplicacion/ServicioVisitas.cs`, `Adaptadores/Firebase/VisitasFirestore.cs`.
- `Aplicacion/ServicioNavegacion.cs` (eventos, 2.7), `ParametrosNegocio.cs` (300 s).
- `Arranque/Arranque.cs`: conectar eventos.
- Tests: `PruebasVisitas.cs` con `IVisitas` falsa y navegación simulada.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaVisita` y pruebas.
- [ ] `IVisitas` + adaptador Firestore.
- [ ] `ServicioVisitas` con reintento al arrancar.
- [ ] Conexión en el Arranque y pruebas de cada fila.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes.

**Manual checks:**
- Ruta real a un lugar con precio: aparece la visita en la consola de Firebase.
