---
title: 'Historia 2.9: llegar y terminar la ruta'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 2'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-2-7-navegar-giro-a-giro.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La navegación no detecta la llegada ni libera el GPS al acabar (FR22, AD-19).

**Approach:** `ServicioNavegacion` detecta la llegada y pasa a "Has llegado" (tarjeta con "Terminar ruta", cierre automático al minuto). Terminar (a mano, sola o con "Terminar" en ruta) para el GPS en segundo plano, quita la ruta y vuelve al mapa.

## Boundaries & Constraints

**Always:**
- **Llegada** (`ReglaNavegacion`): distancia en línea recta al destino ≤ 30 m (`ParametrosNegocio`); una vez por ruta.
- **Tarjeta:** título "Has llegado a <destino>", botón "Terminar ruta", texto "La ruta se cerrará en 1 minuto". TalkBack (región en vivo): "Has llegado a <destino>. La ruta se cerrará en un minuto."; foco a "Terminar ruta".
- **Cierre automático** a los 60 s; reloj inyectado ("ahora" del Arranque) para probarlo.
- **Terminar** (motivo `Llegada`, `Manual` o `Automatico`): `IUbicacion.TerminarRuta()` (fuera notificación), `IMapa.QuitarRuta()`, cámara normal (sin inclinación de navegación), `Screen.sleepTimeout` restaurado, evento `RutaTerminada(motivo)`.
- **Botón atrás** navegando: pregunta "¿Terminar la ruta?" (Sí/No); en "Has llegado": termina.
- **Sin historial** (FR17): direcciones y puntos libres navegados no se guardan; lugares tampoco aquí (los "frecuentes" de la épica 3 los calcula su servicio).
- App cerrada en ruta → el servicio nativo se para (`onTaskRemoved` → `stopSelf`).

**Never:** sin crear visitas (4.2), sin sonido de llegada (5.5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Llegar | Posición a 25 m del destino | Tarjeta "Has llegado" y anuncio |
| Casi | 35 m | Sigue navegando |
| Terminar a mano | Toque en "Terminar ruta" | GPS de ruta parado, ruta quitada, mapa normal |
| Automático | 60 s tras llegar | Igual que terminar a mano, motivo `Automatico` |
| Terminar en ruta | "Terminar" antes de llegar | Igual, motivo `Manual` |
| Atrás en ruta | `Escape` | Pregunta de confirmación |
| App cerrada | Quitar la app de recientes | Servicio parado, notificación fuera |
| Punto libre | Navegación a una dirección | Nada se guarda tras terminar |

</frozen-after-approval>

## Code Map

- `Dominio/ReglaNavegacion.cs` (llegada), `ParametrosNegocio.cs` (30 m, 60 s).
- `Aplicacion/ServicioNavegacion.cs`: estados `Navegando`/`Llegado`/`Terminada`, temporizador, evento.
- Nuevo `UI/TarjetaLlegada.cs`; `UI/PantallaMapa.cs` (`Atras()`), confirmación modal con el patrón de `ExplicacionUbicacion`.
- `Plugins/Android/ServicioUbicacionRuta.java`: `onTaskRemoved`.
- Tests: `PruebasNavegacion.cs` (llegada y cierre).

## Tasks & Acceptance

**Execution:**
- [ ] Llegada y cierre automático en Dominio/servicio con reloj inyectado.
- [ ] Tarjeta "Has llegado", anuncio y foco.
- [ ] Terminar por todos sus caminos y confirmación con atrás.
- [ ] `onTaskRemoved` en el plugin.
- [ ] Captura "llegada".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abre `llegada.png`, entonces se ve la tarjeta "Has llegado".

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "llegada".

**Manual checks:**
- En el móvil: llegada real, cierre al minuto y notificación que desaparece.
