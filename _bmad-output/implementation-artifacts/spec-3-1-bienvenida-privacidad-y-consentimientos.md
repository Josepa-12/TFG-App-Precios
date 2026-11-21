---
title: 'Historia 3.1: bienvenida, privacidad y consentimientos'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 3'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/EXPERIENCE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La 1.7 solo explica la ubicación. Faltan bienvenida, aviso de privacidad y consentimiento explícito de ubicación y notificaciones (FR41, UX-DR16).

**Approach:** Bienvenida a pantalla completa antes del mapa, solo en el primer arranque, en tres pasos: qué es PriceMap, privacidad, permisos. Sustituye a la `ExplicacionUbicacion` del primer arranque (1.7), que queda solo para el botón de ubicación.

## Boundaries & Constraints

**Always:**
- **Pasos** ("Siguiente", puntos de progreso; atrás = paso anterior):
  1. "Precios reales de Sanlúcar, en el mapa" y una frase sobre aportar al llegar.
  2. Privacidad: ubicación solo con la app abierta y durante una ruta; aportes anónimos; cuenta opcional; enlace "Leer el aviso completo" con el texto dentro de la app (sin web).
  3. Permisos: tarjetas de ubicación y de notificaciones, cada una con explicación y "Permitir"/"Ahora no". Después, "Empezar".
- Hueco en el diseño del paso 3 para el consentimiento a la evaluación del TFG (6.2), sin casilla todavía.
- **Notificaciones:** Android 13+ pide `POST_NOTIFICATIONS`; anteriores = concedido. Por puerto (`IPermisos`, o ampliar `IUbicacion` + nuevo `INotificaciones`), nunca desde la UI.
- **Guardado:** `IPreferencias` guarda `bienvenida.vista = 1` y las elecciones; luego mapa directo. Cerrar a mitad → la bienvenida vuelve a salir.
- **Rechazo:** la app funciona con lo permitido; "Ahora no" muestra "Puedes activarlo después en Ajustes" (Ajustes real = 3.5).
- **Accesibilidad:** título de cada paso como primer foco, modal, letra al 200 % sin cortes (desplazamiento), 48 dp.
- Textos por Localization, tono de Voice and Tone (tuteo, frases cortas).

**Never:** sin cuenta en la bienvenida (3.3), sin consentimiento de evaluación todavía (6.2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Primer arranque | Sin `bienvenida.vista` | Bienvenida antes del mapa |
| Siguiente arranque | `bienvenida.vista = 1` | Mapa directo |
| Cerrar a mitad | Sale en el paso 2 | Vuelve a salir la bienvenida |
| Permitir ubicación | Toque | Diálogo de Android; tarjeta marca "Permitido" |
| Ahora no | Toque en ubicación | Texto sobre Ajustes; la app sigue |
| Android 12 | Notificaciones | Se dan por concedidas sin diálogo |
| Atrás en paso 2 | `Escape` | Paso 1 |
| Atrás en paso 1 | `Escape` | Nada (no sale de la app) |
| Usuario de la 1.7 | Ya vio la explicación de ubicación | Ve la bienvenida una vez; ubicación ya aparece como "Permitido" si lo estaba |

</frozen-after-approval>

## Code Map

- Nuevos: `UI/PantallaBienvenida.cs` + `Assets/UI/PantallaBienvenida.uxml/.uss`; `Aplicacion/INotificaciones.cs` (solo permiso; programar llega en la 4.3); `Aplicacion/ServicioBienvenida.cs`.
- `Adaptadores/Sistema/`: permiso de notificaciones.
- `UI/PantallaMapa.cs` y `UI/ExplicacionUbicacion.cs`: sin explicación en el primer arranque.
- `Arranque/Arranque.cs`: bienvenida o mapa. `CapturaPruebas.cs`: capturas "bienvenida-1..3".
- Tests: `PruebasBienvenida.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Servicio con pasos, guardado y elecciones; pruebas.
- [ ] Pantalla con los tres pasos, TalkBack y atrás.
- [ ] Permiso de notificaciones por puerto.
- [ ] Retirar la explicación del primer arranque de la 1.7.
- [ ] Capturas y pruebas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abren las capturas de la bienvenida, entonces los tres pasos se ven sin cortes.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; capturas de PC.

**Manual checks:**
- Instalación limpia en Android 13+ y en uno anterior.
