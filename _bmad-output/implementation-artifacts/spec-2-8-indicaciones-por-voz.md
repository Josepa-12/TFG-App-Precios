---
title: 'Historia 2.8: indicaciones por voz'
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

**Problem:** El conductor tiene que mirar el móvil para saber la siguiente maniobra (FR21).

**Approach:** Un puerto `IVoz` con un adaptador que usa el `TextToSpeech` de Android por JNI (`PriceMap.Adaptadores.Voz`). `ServicioNavegacion` decide cuándo hablar con una regla pura: aviso previo y aviso en el momento. Un ajuste "Indicaciones por voz" (activado por defecto) en `IPreferencias`; la sección de Ajustes llega en la 3.5, aquí se pone un botón de altavoz en el panel de navegación.

## Boundaries & Constraints

**Always:**
- **Cuándo hablar** (`ReglaVoz`, Dominio, pura): por maniobra, dos avisos como máximo:
  - Previo: coche a 300 m ("En 300 metros, gira a la derecha por Calle Ancha"); a pie a 50 m.
  - En el momento: coche a 40 m, a pie a 15 m ("Gira a la derecha").
  - Si la maniobra está más cerca que el umbral previo al empezar, solo el del momento. Nunca repite un aviso ya dicho. Tras un recálculo, empieza de cero con las maniobras nuevas.
  - Distancias redondeadas: 50 m en coche, 10 m a pie.
- **Adaptador:** `TextToSpeech` con `Locale("es","ES")`, cola `QUEUE_FLUSH` para no acumular; `AudioAttributes` de navegación (`USAGE_ASSISTANCE_NAVIGATION_GUIDANCE`) con foco de audio transitorio y atenuación de otras apps. Funciona con la pantalla apagada mientras el servicio de la 2.1 mantiene vivo el proceso (el TTS lo dispara el plugin Java cuando Unity está pausado: el servicio nativo recibe la lista de avisos con sus coordenadas y los lanza él).
- **Silencio:** con el modo silencio o vibración del móvil (`AudioManager.getRingerMode() != NORMAL`) no habla. Con la voz desactivada tampoco. La maniobra se sigue viendo.
- **Botón altavoz** en el panel de navegación (48 dp): alterna la voz; TalkBack "Indicaciones por voz, activadas".
- Si TalkBack está activo, la voz de navegación sigue sonando (son canales distintos).
- En PC y editor, `VozNula`.

**Never:** sin voces descargadas ni servicios en la nube.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Previo coche | Maniobra a 310 → 290 m | "En 300 metros, gira a la derecha por Calle Ancha" una vez |
| Momento coche | 41 → 39 m | "Gira a la derecha" una vez |
| Cerca al empezar | Primera maniobra a 100 m en coche | Solo el aviso del momento |
| A pie | Maniobra a 52 → 48 m | Aviso previo "En 50 metros…" |
| Voz desactivada | Ajuste apagado | No habla; panel igual |
| Modo silencio | Ringer en vibración | No habla |
| Recálculo | Ruta nueva | Avisos de las maniobras nuevas desde cero |
| Pantalla apagada | Unity pausado, servicio activo | El servicio nativo dice el aviso |

</frozen-after-approval>

## Open Questions

1. **Voz con la pantalla apagada.** Mover los avisos al servicio nativo complica el plugin. A) Hacerlo (recomendado: lo pide el criterio). B) Solo con la app delante y dejarlo en `deferred-work`.

## Code Map

- Nuevos: `Dominio/ReglaVoz.cs`, `Aplicacion/IVoz.cs`, `Adaptadores/Voz/` (asmdef, `VozAndroid.cs`, `VozNula.cs`).
- `Plugins/Android/ServicioUbicacionRuta.java` (2.1): lista de avisos y `TextToSpeech` nativo.
- `Aplicacion/ServicioNavegacion.cs` (2.7), `UI/PanelManiobra.cs` (altavoz), `BotonRedondo.cs` (icono altavoz), textos.
- Tests: `PruebasVoz.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaVoz` y sus pruebas.
- [ ] `IVoz`, adaptador Android y nulo.
- [ ] Avisos nativos con la pantalla apagada.
- [ ] Botón altavoz y preferencia.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con una voz falsa que registra lo dicho.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `CompilarApk`.

**Manual checks:**
- En coche o andando: avisos a tiempo, con la pantalla apagada y respetando el modo silencio.
