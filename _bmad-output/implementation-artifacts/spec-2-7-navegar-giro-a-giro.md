---
title: 'Historia 2.7: navegar giro a giro'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 2'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-2-1-gps-activo-con-la-pantalla-apagada-durante-una-ruta.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-2-6-ver-los-tiempos-y-calcular-la-ruta-mas-corta.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Ir dibuja la ruta (2.6) pero no guía: falta siguiente maniobra, tiempo restante y recálculo al desviarse (FR19, FR20).

**Approach:** `ServicioNavegacion` (Aplicación) recibe posiciones de `IUbicacion`, las proyecta sobre la ruta, calcula siguiente maniobra, distancia a ella y tiempo restante, y decide recalcular. Arranca el GPS de ruta (2.1). UI: panel de maniobra arriba, tiempo abajo.

## Boundaries & Constraints

**Always:**
- **Seguimiento** (`ReglaNavegacion`, Dominio, pura):
  - Proyección al segmento más cercano de la ruta, en metros (equivalente de `Geo` en `double` en el Dominio: equirrectangular local).
  - Distancia restante = resto de ruta desde el punto proyectado; tiempo restante = duración total × (restante / total).
  - Siguiente maniobra = primera por delante del punto proyectado.
  - Desvío: > 40 m de la ruta durante > 5 s seguidos → recalcular (`ParametrosNegocio`).
- **Recálculo:** ruta nueva desde la posición actual; la vieja se ve hasta tener la nueva; uno solo a la vez; si falla, reintento a los 10 s.
- **Panel de maniobra:** icono vectorial (recto, izquierda, derecha, ligeramente, rotonda con salida, llegar), texto ("Gira a la derecha por Calle Ancha") y distancia ("en 120 m"). Abajo: tiempo restante, distancia, hora estimada de llegada y botón "Terminar".
- **Cámara:** sigue al usuario, arriba el rumbo de la marcha (coche) o de la brújula (a pie), inclinación 45°. Si el usuario mueve el mapa, deja de seguir y sale "Volver a centrar".
- **Posición:** coche = flecha `#2F5BEA` con borde blanco orientada al rumbo GPS (si velocidad > 1 m/s; si no, el último); a pie = círculo con cono de la 1.7 (FR20, UX-DR8).
- **GPS en segundo plano:** al empezar, `IUbicacion.IniciarRuta("Navegando a <destino> · N min")`; notificación actualizada al cambiar los minutos; se para al terminar (2.9).
- **Sin señal GPS** (10 s sin posición nueva): posición atenuada, aviso "Buscando señal GPS", sin recálculo hasta recuperarla (UX-DR16).
- **TalkBack:** panel de maniobra = región en vivo, anuncia cada maniobra nueva; "Terminar ruta" es botón.
- **Eventos para la épica 4** (AD-9): `RutaIniciada(destino)`, `TiempoRestanteCambiado(segundos)`, `RutaTerminada(motivo)`.
- Pantalla encendida con la app delante (`Screen.sleepTimeout = NeverSleep`), restaurada al terminar.

**Never:** sin voz (2.8), sin llegada (2.9), sin crear visitas (4.2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Empezar | Ir con ruta de 6 min | Panel de maniobra, tiempo "6 min", notificación con "6 min" |
| Avanzar | Posición a mitad de la ruta | Tiempo ≈ mitad; siguiente maniobra correcta |
| Desvío corto | 45 m de la ruta durante 3 s | No recalcula |
| Desvío largo | 45 m durante 6 s | Recalcula; la ruta vieja sigue hasta la nueva |
| Recálculo falla | Sin red | Reintento a los 10 s, aviso discreto |
| Coche | Modo coche, rumbo 90°, 5 m/s | Flecha al este |
| A pie | Modo a pie | Círculo y cono |
| Sin señal | 12 s sin posición | Posición atenuada y "Buscando señal GPS" |
| Mover el mapa | Arrastre | Deja de seguir; "Volver a centrar" |

</frozen-after-approval>

## Open Questions

1. **Tamaño.** A) Partir (recomendado): parte 1 = `ReglaNavegacion`, servicio, recálculo y eventos; parte 2 = panel, cámara, flecha y sin señal. B) Una sola.
2. **Flecha a pie** (pedido del usuario el 21/11/2026: «cuando esté andando quiero la flecha en vez del círculo»). Cambia «a pie = círculo con cono» de Boundaries y la fila «A pie» de la matriz. Falta concretar: A) solo navegando a pie, flecha orientada al rumbo de la marcha y, por debajo de 1 m/s, a la brújula (recomendado). B) También fuera de ruta, siempre que el usuario se mueva (cambia la 1.7).

## Code Map

- Nuevos: `Dominio/ReglaNavegacion.cs`, `Dominio/EstadoNavegacion.cs`, `Aplicacion/ServicioNavegacion.cs`, `UI/PanelManiobra.cs`, `UI/IconosManiobra.cs`.
- `Aplicacion/IMapa.cs`: `Seguir(Coordenada, float rumbo, bool activo)` o equivalente; `Vectorial/CamaraMapa.cs`.
- `UI/MarcadorPosicion.cs`: modo flecha.
- `Aplicacion/IUbicacion.cs` (2.1), `ServicioRutas` (2.6), `ParametrosNegocio` (40 m, 5 s, 10 s).
- Tests: `PruebasNavegacion.cs` con rutas sintéticas.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaNavegacion` y pruebas de proyección, restante, maniobra y desvío.
- [ ] `ServicioNavegacion` con recálculo, eventos y notificación.
- [ ] Panel de maniobra, barra inferior, "Volver a centrar".
- [ ] Flecha en coche; sin señal.
- [ ] Captura "navegacion" con ruta y posición simuladas.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con ubicación y rutas falsas.
- Dada la build de PC, cuando se abre `navegacion.png`, entonces se ven el panel, la flecha y la ruta.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "navegacion".

**Manual checks:**
- Ruta real a pie y en coche por Sanlúcar con la pantalla apagada.
