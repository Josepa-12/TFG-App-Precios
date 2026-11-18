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

**Problem:** La navegación no sabe cuándo se ha llegado ni libera el GPS al acabar (FR22, AD-19).

**Approach:** `ServicioNavegacion` detecta la llegada (≤ 30 m del destino) y pasa al estado "Has llegado": tarjeta con "Terminar ruta" y cierre automático al minuto. Terminar (a mano, sola o con "Terminar" durante la ruta) para el servicio de GPS en segundo plano, quita la ruta y vuelve al mapa.

## Boundaries & Constraints

**Always:**
- **Llegada** (`ReglaNavegacion`): distancia en línea recta de la posición al destino ≤ 30 m (`ParametrosNegocio`). Solo una vez por ruta.
- **Tarjeta "Has llegado"**: título "Has llegado a <destino>", botón "Terminar ruta" y texto "La ruta se cerrará en 1 minuto". TalkBack anuncia "Has llegado a <destino>. La ruta se cerrará en un minuto." como región en vivo. El foco va a "Terminar ruta".
- **Cierre automático:** a los 60 s de llegar; un reloj inyectado ("ahora" del Arranque) para probarlo.
- **Terminar** (cualquier motivo: `Llegada`, `Manual`, `Automatico`): `IUbicacion.TerminarRuta()` (notificación fuera), `IMapa.QuitarRuta()`, cámara a vista normal (sin inclinación de navegación), `Screen.sleepTimeout` restaurado, evento `RutaTerminada(motivo)`.
- **Botón atrás** durante la navegación: pregunta "¿Terminar la ruta?" (Sí/No). Con "Has llegado": termina.
- **Sin historial:** las direcciones y puntos libres navegados no se guardan en ningún sitio (FR17). Los lugares tampoco tienen historial en esta historia (los "frecuentes" de la épica 3 los calcula su servicio).
- Si la app se cierra durante la ruta, el servicio nativo también se para (`onTaskRemoved` → `stopSelf`).

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
- Nuevo `UI/TarjetaLlegada.cs`; `UI/PantallaMapa.cs` (`Atras()`), confirmación modal reutilizando el patrón de `ExplicacionUbicacion`.
- `Plugins/Android/ServicioUbicacionRuta.java`: `onTaskRemoved`.
- Tests: `PruebasNavegacion.cs` (llegada y cierre).

## Tasks & Acceptance

**Execution:**
- [ ] Llegada y cierre automático en el Dominio/servicio con reloj inyectado.
- [ ] Tarjeta "Has llegado", anuncio y foco.
- [ ] Terminar en todos sus caminos y confirmación con atrás.
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
