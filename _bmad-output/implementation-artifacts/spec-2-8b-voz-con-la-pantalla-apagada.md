---
title: 'Historia 2.8 (parte 2): la navegación sigue con el móvil bloqueado'
type: 'feature'
created: '2026-11-22'
status: 'done'
baseline_commit: '0f2d735ed8f10d160982d984eb67fc238202d36e'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Con el móvil bloqueado (o la app detrás) Android pausa Unity y con él toda la navegación: la voz de la 2.8 calla, no se recalcula y, al volver, las posiciones guardadas se procesan de golpe y pueden disparar avisos viejos (FR21).

**Approach:** Navegando, Unity no se pausa: la app hace exactamente lo mismo bloqueada que delante (distancias por la ruta, recálculos, avisos de voz a tiempo real), solo sin dibujar y a pocos fotogramas por segundo.

**Decisiones (22/11/2026, del usuario):**
- Bloqueado debe hacer lo mismo que la app delante, a tiempo real. Se descarta mandar a Android una lista de avisos.
- Se intenta que Unity no se pause; si en el móvil no funciona, se pasa a mandar la línea de la ruta al servicio de Android (sin recálculo bloqueado).

## Boundaries & Constraints

**Always:**
- **Pantalla de Android:** se pasa de GameActivity a Activity (`androidApplicationEntry`) con una actividad propia `com.pricemap.app.ActividadPrincipal`, copia de la plantilla `UnityPlayerActivity` de Unity 6000.3. Si C# ha dicho que hay ruta (`seguirEnSegundoPlano`), en `onPause`/`onStop` no se pausa el reproductor de Unity y se avisa a C# («segundo plano»); al volver, «delante». Sin ruta, todo como siempre (`OnApplicationPause` y GPS apagado, NFR-3).
- **En segundo plano navegando:** 5 fotogramas por segundo y la cámara del mapa sin dibujar; al volver, lo de siempre. La navegación, la voz y la notificación siguen igual que delante.
- **Procesador despierto:** el servicio de ruta (2.1) tiene un `PARTIAL_WAKE_LOCK` mientras está en marcha (permiso `WAKE_LOCK`); lo suelta al parar.
- **Respaldo:** si aun así Unity se pausa y vuelve con posiciones guardadas, la voz no habla durante 1 s al volver (solo da por dicho lo que ya no toca), así no dice avisos viejos.
- Al terminar la ruta (botón, notificación o llegada), se vuelve al comportamiento normal y se suelta el procesador.
- Compartir con la app (2.5c), permisos y la notificación siguen funcionando con la actividad nueva.

**Never:** duplicar la navegación en Java; cambiar la lógica de la ruta o de la voz; dejar Unity sin pausar sin ruta.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Bloquear | Navegando, se bloquea el móvil | Unity sigue: avisos de voz a su hora, minutos de la notificación al día |
| Desvío | Bloqueado, se equivoca de calle | Recalcula y dice la siguiente de la ruta nueva |
| Sin ruta | Se bloquea sin navegar | Se pausa como siempre; GPS apagado |
| Volver | Desbloquea | Dibuja otra vez al ritmo normal; nada repetido |
| Pausa forzada | El sistema pausa Unity igualmente | Al volver no dice avisos viejos (1 s de silencio) |
| Terminar | X de la notificación con el móvil bloqueado | La ruta acaba, se suelta el procesador, la próxima vez se pausa |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap`.
- `Assets/Editor/ConfigurarAndroid.cs:27-41` — `PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity`. Plantilla: `C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Data/PlaybackEngines/AndroidPlayer/Source/com/unity3d/player/UnityPlayerActivity.java` (`onPause`/`onStop` llaman a `super` y luego a `mUnityPlayer.onPause()/onStop()`; en GameActivity la pausa la hace la parte nativa y no se puede evitar).
- Nuevo `Assets/Plugins/Android/ActividadPrincipal.java` (`com.pricemap.app`) — copia de la plantilla; `static volatile boolean seguirEnSegundoPlano` + `public static void seguir(boolean)`; en segundo plano con ruta, sin `mUnityPlayer.onPause/onStop` y `UnityPlayer.UnitySendMessage(objeto, "AlSegundoPlano", "1"/"0")`.
- Nuevo `Assets/Editor/ActividadAndroid.cs` (`IPostGenerateGradleAndroidProject`, como `CompartirAndroid.cs:25-38`, que busca la actividad lanzadora por su intent-filter MAIN/LAUNCHER) — `android:name` de la actividad lanzadora a `com.pricemap.app.ActividadPrincipal`; quitar del manifiesto lo que sea solo de GameActivity si lo hay.
- `Assets/Plugins/Android/ServicioUbicacionRuta.java` — `PowerManager.PARTIAL_WAKE_LOCK` al arrancar, `release` en `onDestroy`; `PermisosUbicacionAndroid.cs` (Editor) añade `android.permission.WAKE_LOCK`.
- `Assets/Scripts/Adaptadores/Gps/ServicioRutaAndroid.cs` (JNI) — al iniciar/parar la ruta, `ActividadPrincipal.seguir(true/false)`.
- `Assets/Scripts/Arranque/Arranque.cs:104-139` — método `AlSegundoPlano(string)` (UnitySendMessage) → `Application.targetFrameRate` 5 y `PantallaMapa` sin dibujar el mapa (cámara desactivada), y al revés; `OnApplicationPause(false)` tras una pausa real → `ServicioVoz.Reanudar()`.
- `Assets/Scripts/Aplicacion/ServicioVoz.cs` — `Reanudar()`: 1 s (reloj inyectado `Func<DateTime>`) en que `Actualizado` solo llama a `ReglaVoz.Sincronizar(estado)`; `Dominio/ReglaVoz.cs` — `Sincronizar`: la maniobra actual como avisada a su distancia de ahora, sin frase.
- Pruebas: `Assets/Tests/EditMode/PruebasVoz.cs` (Reanudar/Sincronizar, fila Pausa forzada) y una nueva `PruebasSegundoPlano.cs` (AlSegundoPlano: fotogramas y mapa; manifiesto con la actividad y `WAKE_LOCK`; la lógica Java, leyendo el código como en la 2.8).

## Tasks & Acceptance

**Execution:**
- [x] `ActividadPrincipal.java`, `ActividadAndroid.cs`, `ConfigurarAndroid.cs` -- Unity sin pausar navegando.
- [x] `ServicioUbicacionRuta.java`, `PermisosUbicacionAndroid.cs`, `ServicioRutaAndroid.cs` -- procesador despierto y aviso a la actividad.
- [x] `Arranque.cs`, `PantallaMapa` -- 5 fps y sin dibujar en segundo plano.
- [x] `ReglaVoz.Sincronizar`, `ServicioVoz.Reanudar` -- respaldo de 1 s -- pruebas.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dado `CompilarApk`, cuando se compila, entonces el APK se genera y su manifiesto lanza `ActividadPrincipal` con los filtros de compartir y `WAKE_LOCK`.

## Implementation Notes

- `ActividadPrincipal.seguir(boolean, String objeto)`: lleva también el GameObject (el del Arranque, el mismo de `AlPosicionRuta`). Solo se salta la pausa si además `PuenteUbicacionRuta.enMarcha()` (sin servicio vivo, Unity se pausa). Al volver no se llama a `onResume/onStart` de Unity si se saltó la pausa. `seguir(false)` con la app detrás hace en ese momento la pausa saltada (`pausarLoSaltado`).
- El apagado de la cámara lo hace `Arranque.AplicarSegundoPlano` (busca `CamaraMapa`); `PantallaMapa` no cambia. Constantes `Arranque.FotogramasDelante` (60) y `FotogramasSegundoPlano` (5); `ParametrosNegocio.VozSilencioAlVolverSegundos` (1).
- `ReglaVoz.Sincronizar` = `Avisar(estado, Todo)` sin frase. `ServicioVoz` recibe un reloj opcional.
- `ProjectSettings.asset`: `androidApplicationEntry: 1` (Activity). `ActividadAndroid` cambia `android:name` de la lanzadora y quita `android.app.lib_name`.
- La notificación aún no tiene botón X (fila «Terminar» de la matriz): se cumple al terminar desde la app o al llegar; el botón queda para la 2.9.
- Riesgo: un recálculo o una ruta nueva en el segundo de silencio no dicen su primera maniobra.

## Spec Change Log

## Review Triage Log

Revisión del 22/11/2026 (blind, edge-case, verification-gap; diff del código desde `0f2d735`, sin `.asset` ni `.meta`).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | Con pantalla partida o un diálogo encima, `onPause` manda «segundo plano» y el mapa deja de dibujarse a la vista | medium | `onPause` llega aunque la app siga visible; `avisar(true)` estaba en `onPause` | patch («1» solo en `onStop`) |
| 2 | Si Android destruye la actividad navegando, el servicio sigue con GPS, notificación y procesador despierto | medium | `onDestroy` no paraba el servicio; el `WAKE_LOCK` es nuevo | patch |
| 3 | Si el sistema para el servicio con la app detrás, Unity se queda sin pausar | medium | `siguiendo()` solo se mira en `onPause/onStop` | patch (`seguir(false)` en `onDestroy` del servicio) |
| 4 | Tras una pausa larga el primer fotograma llega después de 1 s y se dicen avisos viejos | medium | La ventana empezaba en `OnApplicationPause(false)`; las posiciones guardadas se procesan en el primer fotograma | patch (la ventana empieza en el primer `Actualizado`; se limpia al empezar ruta) |
| 5 | Ruta terminada con la app detrás: volver a 60 fps depende de un solo mensaje | low | `pausarLoSaltado` manda «0» justo antes de pausar | patch (`OnApplicationPause(false)` pone lo de delante) |
| 6 | `seguir(false)` y luego `seguir(true)` antes de ejecutarse: pausa en plena ruta | low | `pausarLoSaltado` va en `runOnUiThread` | patch |
| 7 | El procesador se queda despierto si `escuchar()` no consigue proveedor | low | `despertar()` sin mirar el resultado | patch |
| 8 | Sin prueba: `OnApplicationPause(false)` llama a `Reanudar` | low | `PruebasVoz` llama a `Reanudar` directamente | patch (función estática + prueba) |
| 9 | La parte Java solo se comprueba leyendo el código | low | EditMode no corre Android; falta la prueba en el móvil | defer |
| 10 | Nada fija que `UbicacionUnity` y `Arranque` compartan GameObject (destino de `AlSegundoPlano`) | low | Hoy `Arranque` añade `UbicacionUnity` a su propio objeto | defer |
| 11 | El GPS y la brújula de Unity siguen en segundo plano | false | Al bloquear llega `OnApplicationFocus(false)` → `ServicioUbicacion.Pausar` → `Detener` | rechazado |
| 12 | El panel de UI Toolkit sigue dibujando a 5 fps; las teselas se siguen cargando | low | El spec pide la cámara del mapa sin dibujar; coste pequeño a 5 fps | rechazado |
| 13 | Una posición real a menos de 40 m en el segundo de silencio no dice el «momento» | low | Solo en el camino de respaldo | rechazado |
| 14 | Prueba del manifiesto con uno de GameActivity; `callbackOrder` sin comprobar | low | El APK generado lanza `ActividadPrincipal` con los filtros (comprobado al compilar) | rechazado |
| 15 | Plantilla copiada que cambiará al actualizar Unity; `WAKE_LOCK` sin tiempo límite | low | La cabecera dice la versión; una ruta puede durar lo que quiera | rechazado |
| 16 | `Parar` durante el cierre de la app manda pausas; la prueba busca cualquier `CamaraMapa` de la escena | low | Sin efecto visible | rechazado |
| 17 | Fila «Terminar»: la notificación no tiene botón X | low | La X de la notificación no existe aún (2.9); la fila se cumple al terminar desde la app o al llegar | rechazado |
| 18 | CLAUDE.md sin las piezas nuevas | low | Documentos | se corrige al cerrar |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarApk` -- sin errores; después, deshacer `Android/addressables_content_state.bin`.

**Manual checks (imprescindible, en el móvil):**
- Navegando, bloquear el móvil: los avisos de voz llegan a su hora y, si te equivocas, recalcula. Sin ruta, al bloquear se pausa. Si Unity se pausa igualmente, avisar: se pasa al respaldo (línea de la ruta en Android).
