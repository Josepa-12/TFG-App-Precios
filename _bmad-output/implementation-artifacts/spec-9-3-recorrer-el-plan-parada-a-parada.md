---
title: 'Historia 9.3: recorrer el plan parada a parada'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 9'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-2-7-navegar-giro-a-giro.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-2-la-visita-queda-verificada-al-acercarme.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Con el plan hecho, el usuario tiene que buscar cada parada a mano (FR56).

**Approach:** "Empezar ruta" en la tarjeta del plan inicia la navegación (2.7) hacia la primera parada. Al terminar la ruta de una parada (llegada o "Terminar"), se ofrece la siguiente con "Ir a la siguiente: Bar Pepe" y se abre su ruta. Cada parada con precios crea su visita como cualquier ruta (4.2), porque usa el mismo `ServicioNavegacion`.

## Boundaries & Constraints

**Always:**
- **Estado del recorrido** (`RecorridoPlan`, Aplicación): parada actual, paradas hechas; se guarda en `IPreferencias` para retomarlo si se cierra la app ("Continuar el plan: parada 2 de 4").
- **Entre paradas:** al terminar la ruta de una parada, tarjeta "Parada 1 hecha" con "Ir a la siguiente: <nombre>" (botón principal) y "Terminar el plan". Sin pulsar nada, se queda ahí (no arranca sola la siguiente).
- **Saltar:** en la tarjeta del plan durante el recorrido, "Saltar esta parada".
- **Fin:** tras la última, "¡Plan terminado!" y el plan se conserva (el usuario lo vacía si quiere).
- **Modo de viaje:** el elegido en la fila de acciones (2.6) para todo el recorrido; se puede cambiar entre paradas.
- **Visitas:** sin código especial; la integración se comprueba con una prueba que encadena navegación falsa y `ServicioVisitas`.
- **Sin conexión** (UX-DR16): al abrir el plan se ve el último gasto calculado guardado (9.2) y "Empezar ruta" usa la ruta aproximada de la 2.6 con su aviso.
- TalkBack: la tarjeta entre paradas toma el foco y se anuncia.

**Never:** sin optimizar el orden automáticamente (lo decide el usuario).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Empezar | Plan de 3 paradas | Navegación a la 1 |
| Llegar | "Has llegado" y terminar | Tarjeta "Parada 1 hecha · Ir a la siguiente: Bar Pepe" |
| Siguiente | Toque | Navegación a la 2 |
| Saltar | "Saltar esta parada" en la 2 | Navegación a la 3 |
| Última | Termina la 3 | "¡Plan terminado!" |
| Cerrar app | Cerrada entre paradas | Al abrir: "Continuar el plan: parada 2 de 3" |
| Visita | Parada con precios, restante ≤ 5 min | Visita creada |
| Punto sin precio | Parada sin plantilla de precio | Sin visita |
| Sin conexión | Abrir plan | Último total y ruta aproximada |

</frozen-after-approval>

## Code Map

- Nuevos: `Aplicacion/RecorridoPlan.cs`, `UI/TarjetaEntreParadas.cs`.
- `Aplicacion/ServicioPlan.cs`, `ServicioNavegacion.cs` (`RutaTerminada`), `ServicioVisitas.cs` (sin cambios; prueba de integración).
- `UI/TarjetaPlan.cs` ("Empezar ruta", "Saltar"), `UI/TarjetaLlegada.cs` (2.9).
- Tests: `PruebasRecorridoPlan.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `RecorridoPlan` con persistencia y pruebas.
- [ ] Tarjeta entre paradas, saltar y fin.
- [ ] Prueba de integración con visitas.
- [ ] Sin conexión y captura "recorrido".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "recorrido".

**Manual checks:**
- Recorrido real de dos paradas por Sanlúcar.
