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

**Approach:** Puerto `IVoz` con adaptador `TextToSpeech` de Android por JNI (`PriceMap.Adaptadores.Voz`). `ServicioNavegacion` decide cuándo hablar con una regla pura (aviso previo y en el momento). Ajuste "Indicaciones por voz" (activado por defecto) en `IPreferencias`; Ajustes llega en la 3.5, aquí basta un botón de altavoz en el panel.

## Boundaries & Constraints

**Always:**
- **Cuándo hablar** (`ReglaVoz`, Dominio, pura), máx. dos avisos por maniobra:
  - Previo: coche a 300 m ("En 300 metros, gira a la derecha por Calle Ancha"); a pie a 50 m.
  - En el momento: coche a 40 m, a pie a 15 m ("Gira a la derecha").
  - Maniobra ya más cerca que el umbral previo al empezar → solo el del momento. Nunca repite un aviso. Tras recálculo, de cero con las maniobras nuevas.
  - Distancias redondeadas a 50 m (coche) y 10 m (a pie).
- **Adaptador:** `Locale("es","ES")`, `QUEUE_FLUSH`; `AudioAttributes` `USAGE_ASSISTANCE_NAVIGATION_GUIDANCE`, foco de audio transitorio con atenuación de otras apps. Con la pantalla apagada (Unity pausado, proceso vivo por el servicio de la 2.1) el servicio nativo recibe la lista de avisos con sus coordenadas y los lanza él.
- **Silencio:** no habla con `AudioManager.getRingerMode() != NORMAL` (silencio o vibración) ni con la voz desactivada; la maniobra se sigue viendo.
- **Botón altavoz** (48 dp) en el panel: alterna la voz; TalkBack "Indicaciones por voz, activadas".
- Con TalkBack activo la voz de navegación sigue sonando (canales distintos).
- PC y editor: `VozNula`.

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

1. **Voz con la pantalla apagada** (avisos en el servicio nativo complican el plugin). A) Hacerlo (recomendado: lo pide el criterio). B) Solo con la app delante y dejarlo en `deferred-work`.

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
