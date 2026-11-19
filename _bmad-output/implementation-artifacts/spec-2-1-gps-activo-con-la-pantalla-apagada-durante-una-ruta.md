---
title: 'Historia 2.1: GPS activo con la pantalla apagada durante una ruta'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '22d5431abc20154250f4fc65ad27aeb9dfbb8055'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 2'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Con la pantalla apagada o con otra app delante, Unity se pausa y el GPS deja de llegar. Sin eso no hay navegación con el móvil en el bolsillo ni visita verificada (NFR-3, AD-19).

**Approach:** Un plugin nativo de Android (Java) en `Assets/Plugins/Android/` con un servicio en primer plano de tipo `location` y una notificación fija. El adaptador de GPS es el único que lo arranca y lo para, y recoge las posiciones que el servicio acumula mientras Unity está pausado. Esta historia deja el mecanismo listo; la 2.7 lo arranca al pulsar Ir.

## Boundaries & Constraints

**Always:**
- **Servicio nativo** `ServicioUbicacionRuta` (Java, `foregroundServiceType="location"`): usa `LocationManager` (GPS + red, cada 1 s, 0 m) sin Google Play Services.
  - Notificación fija de canal "Navegación" (importancia baja, sin sonido) con el texto que le pase Unity ("Navegando a <destino> · N min") y que se puede actualizar.
  - Guarda las posiciones en una cola en memoria (lat, lon, rumbo, velocidad, precisión, hora). Unity la vacía al volver o en cada `Update` vía JNI.
  - También envía cada posición con `UnityPlayer.UnitySendMessage` cuando Unity está activo.
- **Manifiesto:** `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_LOCATION` y `POST_NOTIFICATIONS`; el `<service>` con `foregroundServiceType="location"` y `exported="false"`. Nunca `ACCESS_BACKGROUND_LOCATION`. Se añade en el post-proceso de Gradle existente (`PermisosUbicacionAndroid`).
- **Puerto:** `IUbicacion` gana `IniciarRuta(string textoNotificacion)`, `ActualizarNotificacion(string)` y `TerminarRuta()`. Durante una ruta, `Detener()` (pausa de la app) no apaga el GPS.
- **Android 13+:** antes de arrancar se pide `POST_NOTIFICATIONS`. Si se deniega, la ruta sigue solo con la app delante y se muestra un aviso: *"Sin permiso de notificaciones, la navegación se pausa al apagar la pantalla."*
- **Solo el adaptador de GPS** toca el plugin (`AndroidJavaClass` dentro de `PriceMap.Adaptadores.Gps`).
- En el editor y en PC, `IniciarRuta` simplemente mantiene la ubicación de Unity (sin plugin).
- **Decisión del 19/11/2026:** las comprobaciones en un Android real (notificación fija, 2 min con la pantalla apagada y parada) quedan en `deferred-work`. La historia se cierra con la lógica probada en EditMode y el APK compilado con el manifiesto correcto.

**Never:** sin GPS en segundo plano sin ruta, sin `ACCESS_BACKGROUND_LOCATION`, sin Google Play Services.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Arrancar | `IniciarRuta("Navegando a Clínica Areas · 6 min")` con permisos | Notificación fija; posiciones llegan al puerto |
| Pantalla apagada | Ruta en marcha, Unity pausado 2 min | Al volver, el puerto recibe las posiciones acumuladas en orden |
| Parar | `TerminarRuta()` | Notificación fuera; no llegan más posiciones en segundo plano |
| Pausa sin ruta | `Detener()` sin ruta | GPS apagado (como en 1.7) |
| Pausa con ruta | `Detener()` con ruta | GPS sigue (servicio) |
| Notificaciones denegadas | Android 13, `POST_NOTIFICATIONS` denegado | Ruta solo en primer plano y aviso |
| Sin permiso de ubicación | `Denegado` | `IniciarRuta` devuelve error y no arranca nada |

</frozen-after-approval>

## Code Map

- `Assets/Plugins/Android/` (nuevo): `ServicioUbicacionRuta.java` y `PuenteUbicacionRuta.java`, con los métodos estáticos `iniciar`, `actualizar`, `parar` y `vaciarCola`.
- `Assets/Editor/PermisosUbicacionAndroid.cs`: añadir los permisos y el `<service>` al manifiesto, en el post-proceso de Gradle que ya existe.
- `Assets/Scripts/Aplicacion/IUbicacion.cs` (l.10-30: `Estado`, `Cambiado`, `Iniciar`, `Detener`) y `ServicioUbicacion.cs`:
  - La pausa de la app llega por `ServicioUbicacion` (l.57-62), que llama a `ubicacion.Detener()`.
  - Con una ruta activa, no apaga.
  - La llaman `Arranque.OnApplicationPause`/`OnApplicationFocus` (`Arranque.cs:53-61`).
- `Assets/Scripts/Adaptadores/Gps/`:
  - La lógica está en `UbicacionGps` (`Detener`, l.72), sobre `IFuenteUbicacion`; la fuente real es `FuenteUbicacionUnity`.
  - `UbicacionUnity` es el componente fino.
  - La fuente nativa de la ruta va detrás de una interfaz pequeña (p. ej. `IServicioRuta`), para probar en EditMode la cola y la pausa con un falso. Su implementación con `AndroidJavaClass` solo se compila en Android.
- **Falsos de `IUbicacion`** que hay que actualizar: `CapturaPruebas.cs:57` y los de `Tests/EditMode`.
- **Aviso por notificaciones denegadas:** reutilizar `AvisoUbicacion`. El texto va por Localization (`ClavesTexto` + `PrepararInterfaz.TextosEs`).
- **Pruebas:** `PruebasUbicacion.cs` (pausa con y sin ruta, cola) y `PruebasPermisosUbicacionAndroid.cs` (manifiesto).

## Tasks & Acceptance

**Execution:**
- [x] Plugin Java con servicio, notificación y cola.
- [x] Manifiesto por post-proceso y su prueba.
- [x] `IUbicacion.IniciarRuta/ActualizarNotificacion/TerminarRuta` en adaptador, servicio y falsos.
- [x] Permiso `POST_NOTIFICATIONS` y aviso si se deniega.
- [x] Pruebas EditMode de la lógica de pausa y de la cola (fuente falsa).

**Acceptance Criteria:**
- Dada la build de Android, cuando se compila, entonces el manifiesto tiene el servicio con tipo `location` y no tiene `ACCESS_BACKGROUND_LOCATION`.
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.

## Spec Change Log

## Review Triage Log

| # | Hallazgo | Veredicto | Ruta | Evidencia |
|---|----------|-----------|------|-----------|
| 1 | `servicioActivo` se pone a true sin saber si el servicio arrancó o sigue vivo; se ignoran las lecturas de Unity y la posición se congela | high | patch | `startForeground` puede fallar (`stopSelf`) y el sistema puede pararlo; nadie lo avisa. Arreglo: `IServicioRuta.EnMarcha` (`enMarcha()` en Java) y las lecturas de Unity siguen si no está en marcha |
| 2 | Proveedor apagado al arrancar o solo ubicación aproximada: el servicio no escucha nunca | medium | patch | `escuchar()` solo pedía los proveedores encendidos y en un único `try`. Ahora pide cada uno por separado aunque esté apagado |
| 3 | Las posiciones del GPS y de la red se mezclan y el punto salta | medium | patch | Dos proveedores cada 1 s. Ahora se tiran las de la red si hay una del GPS de menos de 5 s |
| 4 | Con la ubicación del móvil apagada, `RecogerRuta` vuelve a poner Activa con posiciones viejas | medium | patch | `RecogerRuta` corre antes de la comprobación de `Apagada`. Ahora tira la cola si el estado no es Activa o Buscando |
| 5 | Si se dan las notificaciones desde Ajustes durante la ruta, el servicio no arranca nunca | medium | patch | Nada volvía a mirar el permiso. `Iniciar` lo hace ahora |
| 6 | Si el diálogo del permiso se cierra sin respuesta, la ruta se queda esperando | medium | patch | Faltaba `PermissionRequestDismissed` |
| 7 | El aviso de notificaciones tapa el de ubicación y sigue abierto al acabar la ruta | low | patch | `PantallaMapa` espera a que no haya otra ventana y lo cierra cuando `RutaSoloPrimerPlano` pasa a false |
| 8 | `parar()` sin actividad no para el servicio; texto viejo si `actualizar` llega antes de `onStartCommand` | low | patch | Correcciones directas en el puente y en el servicio |
| 9 | Falta una prueba de quitar el permiso de ubicación a mitad de ruta | gap | patch | Añadida `QuitarElPermisoDeUbicacionDuranteLaRutaLaTermina` (y una prueba para cada arreglo de arriba) |
| 10 | El comentario de una prueba dice "desde la notificación", pero la notificación no tiene botón de terminar | low | patch | Comentario corregido; el spec no pide ese botón |
| 11 | `RecogerRuta` solo entrega la coordenada (no el rumbo de la marcha, la velocidad ni la hora) | medium | defer | El puerto no tiene esos campos; los necesitan la 2.7 y la 4.2 |
| 12 | El icono pequeño de la notificación es el de la app a todo color (en la barra se vería una mancha blanca) | low | defer | Comprobarlo en el móvil real junto con el resto de la prueba pendiente |
| 13 | Recoger 3.600 posiciones dispara 3.600 `Cambiado` en un fotograma | low | rechazado | El spec pide entregarlas en orden; aún no hay ningún consumidor que pese |
| 14 | El GPS de Unity sigue encendido mientras va el servicio | false | rechazado | Hace falta para la brújula y, tras el arreglo 1, como respaldo |
| 15 | Una respuesta tardía del permiso pasa a la ruta siguiente | low | rechazado | Muy improbable, y el arreglo (contador de peticiones) añade complejidad |
| 16 | El nombre del canal "Navegación" no pasa por Localization; NaN en la cola; excepciones de JNI | low | rechazado | La app solo está en español; Android no da NaN; `Llamar` ya captura las excepciones |
| 17 | "Abrir Ajustes" del aviso de notificaciones abre los de ubicación si esta está apagada | low | rechazado | Caso raro; arreglarlo supone ampliar el puerto |
| 18 | Una librería podría añadir `ACCESS_BACKGROUND_LOCATION` al manifiesto fusionado | false | rechazado | Comprobado en el manifiesto fusionado de la APK: no está |
| 19 | Faltan `Textos Shared Data.asset` y los `.meta` en el diff; el sprint dice in-progress | false | rechazado | Fallo al preparar el diff, no del cambio; el sprint se actualiza al cerrar |
| 20 | Faltan pruebas de duplicados del `<service>` y de un manifiesto sin `<application>` | low | rechazado | El post-proceso ya comprueba que existe antes de añadir; caso imposible en Unity |

## Verification

**Commands:**
- Pruebas EditMode por lotes: todas pasan.
- `CompilarApk` por lotes: APK generado; revisar el manifiesto fusionado.

**Manual checks:**
- En un Android real: notificación fija, posiciones con la pantalla apagada 2 min, la notificación desaparece al parar.
