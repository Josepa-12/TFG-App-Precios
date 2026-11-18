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

**Problem:** Ir dibuja la ruta (2.6) pero no guía. El usuario necesita la siguiente maniobra, el tiempo restante y el recálculo si se desvía (FR19, FR20).

**Approach:** `ServicioNavegacion` (Aplicación) recibe posiciones de `IUbicacion`, proyecta la posición sobre la ruta, calcula la siguiente maniobra, la distancia a ella y el tiempo restante, y decide recalcular. Arranca el GPS de ruta de la 2.1 con la notificación "Navegando a <destino> · N min". La UI muestra un panel de maniobra arriba y el tiempo abajo; la posición es flecha en coche y círculo con cono a pie.

## Boundaries & Constraints

**Always:**
- **Seguimiento** (Dominio, puro, `ReglaNavegacion`):
  - Proyección de la posición al segmento más cercano de la ruta (metros, con `Geo` equivalente en `double` en el Dominio: equirrectangular local).
  - Distancia restante = resto de la ruta desde el punto proyectado; tiempo restante = duración total × (restante / total).
  - Siguiente maniobra = primera cuya posición en la ruta está por delante del punto proyectado.
  - Desvío: distancia a la ruta > 40 m durante > 5 s seguidos → recalcular (`ParametrosNegocio`).
- **Recálculo:** pide una ruta nueva desde la posición actual; la anterior se sigue mostrando hasta tener la nueva. Mientras se recalcula, no se lanza otro. Si falla, se reintenta a los 10 s.
- **Panel de maniobra:** icono vectorial del giro (recto, izquierda, derecha, ligeramente, rotonda con salida, llegar), texto en castellano ("Gira a la derecha por Calle Ancha") y distancia ("en 120 m"). Abajo: tiempo restante, distancia y hora estimada de llegada, y botón "Terminar".
- **Cámara:** sigue al usuario con el rumbo de la marcha arriba (coche) o el de la brújula (a pie), inclinación 45°. Si el usuario mueve el mapa, deja de seguir y aparece "Volver a centrar".
- **Posición:** en coche, flecha `#2F5BEA` con borde blanco orientada al rumbo GPS (si velocidad > 1 m/s; si no, el último); a pie, círculo con cono de la 1.7 (FR20, UX-DR8).
- **GPS en segundo plano:** al empezar, `IUbicacion.IniciarRuta("Navegando a <destino> · N min")`; la notificación se actualiza cuando cambian los minutos. Al terminar (2.9) se para.
- **Sin señal GPS** (sin posición nueva en 10 s): posición atenuada, aviso "Buscando señal GPS" y sin recálculo hasta recuperarla (UX-DR16).
- **TalkBack:** el panel de maniobra es región en vivo y anuncia cada maniobra nueva; "Terminar ruta" es botón.
- **Eventos para la épica 4:** `ServicioNavegacion` publica `RutaIniciada(destino)`, `TiempoRestanteCambiado(segundos)` y `RutaTerminada(motivo)` (AD-9).
- Pantalla encendida durante la navegación con la app delante (`Screen.sleepTimeout = NeverSleep`), y se restaura al terminar.

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

1. **Tamaño.** A) Partir: parte 1 = `ReglaNavegacion`, servicio, recálculo y eventos; parte 2 = panel, cámara, flecha y sin señal (recomendado). B) Una sola.

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
- [ ] Captura "navegacion" con una ruta y posición simuladas.

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
