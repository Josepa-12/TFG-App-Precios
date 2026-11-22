---
title: 'Historia 2.8 (parte 1): indicaciones por voz con la app delante'
type: 'feature'
created: '2026-11-22'
status: 'done'
baseline_commit: 'b0eeb9559acd0ea7368a7704ee9cd84392f8e8f4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El conductor tiene que mirar el móvil para saber la siguiente maniobra (FR21).

**Approach:** Puerto `IVoz` con el `TextToSpeech` de Android (`PriceMap.Adaptadores.Voz`). Una regla pura decide qué decir y cuándo, según el modo de voz: todo, lo importante o nada. El modo se elige con un botón de altavoz en la columna de cristal y se guarda en `IPreferencias`.

**Decisiones (22/11/2026, del usuario):**
- Se parte en dos: esta parte habla con la app delante; la voz con la pantalla apagada (servicio de Android) es la 2.8b.
- Tres modos. **Lo importante:** al terminar una maniobra (y al empezar la ruta) dice la siguiente con su distancia, y justo antes de hacerla la repite. **Todo:** lo mismo más un aviso a 300 m en coche (50 m a pie) si la maniobra estaba lejos. **Nada:** calla.
- Botón redondo en la columna de cristal, encima de «Volver a centrar»; al tocarlo, una fila de cristal con tres iconos de 48 dp a su derecha (sin texto, para que ocupe poco): volumen (todo), volumen con «!» (solo alertas: lo importante) y volumen tachado (nada). El elegido, con fondo de acento; el botón muestra el icono del modo. Maqueta: https://claude.ai/artifact/8KkwKf5FxWqpGiznd2PPVL.

## Boundaries & Constraints

**Always:**
- **Cuándo hablar** (`ReglaVoz`, Dominio, pura); nunca repite un aviso; tras un recálculo, de cero con las maniobras nuevas:
  - **Siguiente:** al pasar una maniobra o empezar la ruta: «En 800 metros, gire a la derecha por Calle Ancha.» Si ya está a menos del umbral del momento, solo el del momento.
  - **Intermedio** (solo «todo»): al cruzar 300 m en coche o 50 m a pie, si el aviso «siguiente» se dijo a más de 1,5 veces esa distancia.
  - **Momento:** a 40 m en coche, 15 m a pie: «Gire a la derecha.»
  - Distancias redondeadas a 50 m (coche) y 10 m (a pie); de 1 km en adelante, «1,5 kilómetros».
  - `Salida` y `Llegada` no se dicen (la llegada es la 2.9). Sin textos (OSRM, ruta aproximada), calla.
- **Voz:** `Locale("es","ES")`, `QUEUE_FLUSH`; `AudioAttributes` `USAGE_ASSISTANCE_NAVIGATION_GUIDANCE`, foco de audio transitorio con atenuación de otras apps. No habla con el móvil en silencio o vibración. Con TalkBack activo también suena.
- **Modo** guardado (por defecto «todo»). Nombres para TalkBack: «Todas las indicaciones», «Solo las importantes», «Sin voz»; el botón: «Indicaciones por voz, solo las importantes»; al elegir, lo anuncia. La fila se cierra al elegir, al tocar el mapa o con atrás.
- El botón solo se ve navegando. PC y editor: `VozNula`.

**Never:** voces descargadas o en la nube; voz con la pantalla apagada (2.8b); llegada (2.9); Ajustes (3.5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Empezar | Coche, primera maniobra a 820 m | «En 800 metros, gire a la derecha por Calle Ancha.» |
| Intermedio | «Todo», 310 → 290 m | «En 300 metros, …» una vez |
| Importante | «Lo importante», 310 → 290 m | Nada |
| Momento | 41 → 39 m | «Gire a la derecha.» una vez |
| Cerca | Siguiente maniobra a 30 m al pasar la anterior | Solo «Gire a la derecha.» |
| A pie | «Todo», 52 → 48 m | «En 50 metros, …» |
| Nada | Modo «Sin voz» | No habla; el mapa igual |
| Silencio | Móvil en vibración | No habla |
| Recálculo | Ruta nueva | Dice la siguiente de la ruta nueva |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `Scripts/Dominio/Maniobra.cs` — añadir `TextoCorto` (por defecto `Texto`). `Scripts/Adaptadores/Rutas/LecturaValhalla.cs:21,56` solo lee `instruction`: leer también `verbal_succinct_transition_instruction`. `ReglaNavegacion` solo cuenta maniobras con `Texto`: OSRM y la aproximada no dan siguiente.
- `Scripts/Dominio/EstadoNavegacion.cs` — `IndiceSiguiente`, `Siguiente`, `MetrosASiguiente`: entrada de `ReglaVoz`; «pasar una maniobra» = cambia `IndiceSiguiente`.
- `Scripts/Aplicacion/ServicioNavegacion.cs` — sin cambios; eventos `RutaIniciada`, `Actualizado` (tras cada `Seguir`), `RutaCambiada` (recálculo), `RutaTerminada`; `Modo`, `Estado`. Nuevo `ServicioVoz` (Aplicación) se suscribe como `Aplicacion/PantallaEncendida.cs:17-18`.
- `Scripts/Aplicacion/IPreferencias.cs` — solo cadenas: clave `"voz"` = `todo|importante|nada` (sin valor = todo), como `ServicioTema.ClavePreferencia`.
- `Scripts/Arranque/Arranque.cs:67-68,144-168` — componer `ServicioVoz` con `VozAndroid` o `VozNula` (`#if UNITY_ANDROID && !UNITY_EDITOR`, como `Adaptadores/Gps/UbicacionUnity.cs:22-25`) y pasarlo a `PantallaMapa`.
- Nuevo `Scripts/Adaptadores/Voz/` (asmdef `PriceMap.Adaptadores.Voz` → Aplicacion, Dominio; añadir en `PriceMap.Arranque`): `VozAndroid` (JNI, `AndroidJavaClass` perezosa y try/catch como `Adaptadores/Gps/ServicioRutaAndroid.cs:66-77`), `VozNula`.
- Nuevo `Plugins/Android/PuenteVoz.java` (`com.pricemap.voz`): `hablar(texto)`, `parar()`; TTS, foco (soltarlo con `UtteranceProgressListener`), `getRingerMode`. La 2.8b lo reutilizará desde el servicio.
- UI: `Scripts/UI/BotonRedondo.cs:7,67-78` (iconos `Voz`, `VozImportante`, `VozNada` con Painter2D); `Scripts/UI/PantallaMapa.cs` (`Columna(...)` l.265, `ColocarControlesRuta` l.2063-2084); fila de iconos nueva (cristal como la columna; cierre como `MenuPrecios`); TalkBack `AccesibilidadPantalla.Boton(..., valor)` como el tema (`PantallaMapa.cs:169,2475,2485`).
- Textos: `Scripts/UI/ClavesTexto.cs` (+`Todas`), `Editor/PrepararInterfaz.cs` `TextosEs`, tabla es-ES. Captura `voz` (fila abierta, claro y oscuro) en `Scripts/Arranque/CapturaPruebas.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `Maniobra` + `LecturaValhalla` -- texto corto -- prueba con JSON.
- [x] `Dominio/ReglaVoz.cs` + `ModoVoz` -- la matriz -- pruebas.
- [x] `Aplicacion/IVoz.cs`, `ServicioVoz.cs` -- navegación, modo guardado -- pruebas con voz falsa.
- [x] `Adaptadores/Voz/*`, `PuenteVoz.java`, `Arranque`.
- [x] Botón, menú, iconos, TalkBack, textos, captura `voz` -- `Tests/EditMode/PruebasVoz.cs`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con una voz falsa que apunta lo dicho.
- Dado `CompilarApk`, cuando se compila, entonces el APK se genera sin errores.

## Implementation Notes

- Al empezar, la maniobra siguiente de `EstadoNavegacion` suele ser la `Salida` (aún sin pasar): `ReglaVoz` usa entonces `Despues`; al dejar atrás la salida no se repite el aviso.
- Lo que el modo no deja decir (Nada, o el intermedio con Importante) no cuenta como dicho: si se cambia de modo a medio camino se dice lo que aún toque.
- Kilómetros a la décima desde los metros reales (1530 m → «1,5 kilómetros»), no desde los ya redondeados a 50.
- Añadido `Editor/MotorVozAndroid.cs`: `<queries>` con `android.intent.action.TTS_SERVICE` en el manifiesto (desde Android 11, sin eso el TTS no ve el motor). No estaba en el Code Map.
- `AccesibilidadPantalla.RefrescarEtiqueta` también relee el valor del nodo (el modo del botón de voz).
- `hueco-voz` va antes de `hueco-tema` en la columna (con ruta el tema no se ve; las pruebas exigen ubicación justo después de tema).

## Spec Change Log

## Review Triage Log

Revisión del 22/11/2026 (blind, edge-case, verification-gap; diff del código desde `b0eeb95`, sin `.asset` ni `.meta`).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | Fuera de la ruta la voz dice maniobras de la ruta vieja (distancia medida desde la proyección) | medium | `EstadoNavegacion.FueraDeRuta` = más de 40 m; `ServicioVoz.AlActualizar` no lo mira | patch (+ prueba) |
| 2 | Con «Sin voz», el aviso «siguiente» se pierde al volver a otro modo a mitad de camino | low | `Avisar` actualiza `objetivo` antes de salir por `Nada`; el comentario promete lo contrario | patch (+ prueba) |
| 3 | Si el motor de voz no arranca, se pierde la conexión sin `shutdown()` y cada frase crea otra | low | `alIniciar` pone `tts = null` sin cerrarlo | patch |
| 4 | La frase pendiente del arranque suena aunque el móvil se haya puesto en vibración | low | `alIniciar` llama a `decir` sin mirar `getRingerMode` | patch |
| 5 | Sin prueba: terminar la ruta con la fila abierta la cierra (si no, atrás y el toque se pierden) | low | Ninguna prueba abre la fila y termina | patch (prueba) |
| 6 | Sin prueba: `Componer` pasa la voz a la pantalla (sin ella no hay botón) | medium | Todas las llamadas a `Componer` de las pruebas omiten `voz` | patch (prueba) |
| 7 | Sin prueba: una voz que falla no para la navegación | low | `VozFalsa` nunca lanza | patch (prueba) |
| 8 | `RefrescarEtiqueta` relee el valor sin prueba propia | low | Hoy todo cambio de modo reconstruye el recorrido | defer |
| 9 | `PuenteVoz` solo se comprueba leyendo el código Java | low | En EditMode no corre Android; pendiente de probar en el móvil | defer |
| 10 | Una maniobra vista a 45 m: el aviso largo se corta con el del momento (`QUEUE_FLUSH`) | low | Se oye el del momento, que dice lo esencial; el arreglo cambia la regla | rechazado |
| 11 | El aviso del momento no tiene en cuenta la velocidad | low | Umbrales fijos que pide el spec | rechazado |
| 12 | Se dice `instruction` y no los textos `verbal_*` de Valhalla; minúscula sobre nombres propios | false | Decisión del spec (Design Notes); las instrucciones empiezan por verbo | rechazado |
| 13 | El resultado del foco de audio no se mira; puede hablar durante una llamada | low | Raro; el arreglo añade un estado nuevo | rechazado |
| 14 | Sin voz en castellano, la app no avisa al usuario | low | Solo en el log; el arreglo añade superficie a `IVoz` | rechazado |
| 15 | TalkBack puede leer el modo dos veces (foco + anuncio) | low | El spec pide anunciarlo; sin comprobar en el móvil | rechazado |
| 16 | La voz habla con el botón oculto (ficha abierta navegando) | low | Momentáneo; la ruta sigue | rechazado |
| 17 | `ServicioVoz` no se desuscribe; `catch` sin registro; `VozAndroid` no libera objetos JNI | low | Vive lo que la app | rechazado |
| 18 | La fila no se cierra al arrastrar el mapa | low | Se cierra con un toque o atrás | rechazado |
| 19 | Los textos `voz.*` no se ven en el diff | false | El diff excluye `.asset`; están las 7 claves en `Textos Shared Data.asset` | rechazado |
| 20 | `MotorVozAndroid` falla con un manifiesto vacío | low | El manifiesto de Unity siempre tiene raíz | rechazado |
| 21 | La captura no deja la cámara como estaba si sale por error | low | Solo la captura de PC y por un camino de error | rechazado |
| 22 | `Reiniciar` en `RutaCambiada` es redundante; `SePide()` en Android, código muerto | low | Sin efecto visible | rechazado |

## Design Notes

Textos en usted, de Valhalla (decidido el 20/11): «En 800 metros, » + `Texto` con la primera letra en minúscula; momento = `TextoCorto`.

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarApk` -- sin errores. Build de PC y captura `voz`; después, la limpieza de CLAUDE.md.

**Manual checks:**
- En el móvil: avisos a tiempo y en castellano; en vibración, calla.
