---
title: 'Historia 5.5: sonidos de recompensa'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 5'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La llegada, la pregunta y las monedas son silenciosas (FR53, UX-DR21).

**Approach:** Un puerto `ISonidos` con tres sonidos cortos (llegada, pregunta, monedas) generados por código (síntesis simple, sin ficheros con licencia dudosa). Una burbuja de altavoz encima del botón € los silencia. El modo silencio del móvil siempre se respeta. Cada sonido va acompañado de un cambio visible y un anuncio.

## Boundaries & Constraints

**Always:**
- **Sonidos** (`SintesisSonidos`, genera `AudioClip` en memoria al arrancar): llegada = dos notas ascendentes (≈ 300 ms); pregunta = una nota suave (≈ 200 ms); monedas = tres notas rápidas brillantes (≈ 350 ms). Volumen moderado, envolvente sin clics.
- **Cuándo:** llegada (2.9 "Has llegado"), aparición de la pregunta (4.3/4.4 al abrir la hoja), monedas (4.11 en la pantalla de recompensa).
- **Silencio:**
  - Burbuja del altavoz (botón pequeño de 48 dp de zona táctil sobre el botón €): alterna sonidos; se guarda en `IPreferencias` (y en la nube con los ajustes, 3.6). TalkBack: "Sonidos, activados".
  - Modo silencio o vibración del móvil (`AudioManager.getRingerMode()` por JNI): no suena.
  - Ajuste equivalente en Ajustes > Notificaciones o Apariencia ("Sonidos de la app").
- **Siempre con señal visible y anuncio:** las tres situaciones ya tienen su cambio visible y anuncio de TalkBack en sus historias; aquí se comprueba que el sonido nunca es la única señal.
- En PC y editor: `SonidosNulos` salvo en la captura.

**Never:** sin música de fondo, sin vibración (no está pedida).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Llegada | "Has llegado" | Suena llegada |
| Pregunta | Se abre la hoja | Suena pregunta |
| Monedas | Pantalla de recompensa | Suena monedas |
| Burbuja apagada | Silenciado | No suena nada |
| Modo silencio | Ringer en vibración | No suena |
| Ajuste | Desactivado en Ajustes | No suena; la burbuja lo refleja |
| Clips | Generar | Duración y sin valores fuera de [-1, 1] |

</frozen-after-approval>

## Code Map

- Nuevos: `Aplicacion/ISonidos.cs`, `Adaptadores/Sistema/SonidosUnity.cs` (+ `SintesisSonidos`, `AudioSource`), `UI/BurbujaAltavoz.cs`.
- `Aplicacion/ServicioNavegacion.cs`, `ServicioVisitas.cs`, `ServicioProgreso.cs` (eventos que disparan sonidos, conectados en el Arranque).
- `UI/BotonPendientes.cs`, `UI/PantallaAjustes.cs`. Tests: `PruebasSonidos.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Síntesis de los tres clips y prueba.
- [ ] Puerto, adaptador y conexión con los eventos.
- [ ] Burbuja, ajuste y modo silencio.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con sonidos falsos.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes.

**Manual checks:**
- En el móvil, con y sin modo silencio.
