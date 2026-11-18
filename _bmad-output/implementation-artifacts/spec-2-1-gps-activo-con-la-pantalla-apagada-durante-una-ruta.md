---
title: 'Historia 2.1: GPS activo con la pantalla apagada durante una ruta'
type: 'feature'
created: '2026-11-18'
status: 'draft'
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

## Open Questions

1. **Prueba en móvil.** El servicio solo se puede comprobar en un Android real. A) Dejar las pruebas manuales en `deferred-work` (recomendado). B) Bloquear la historia hasta probarlo.

## Code Map

- `Assets/Plugins/Android/` (nuevo): `ServicioUbicacionRuta.java` y `PuenteUbicacionRuta.java` (métodos estáticos `iniciar`, `actualizar`, `parar`, `vaciarCola`).
- `Scripts/Editor/PermisosUbicacionAndroid.cs`: añadir permisos y el `<service>` al manifiesto.
- `Scripts/Aplicacion/IUbicacion.cs`, `ServicioUbicacion.cs`: ruta activa, no apagar al pausar.
- `Scripts/Adaptadores/Gps/UbicacionUnity.cs` (+ `IFuenteUbicacion` de la 1.7b): fuente nativa durante la ruta.
- Tests: `PruebasUbicacion.cs` (pausa con/sin ruta, cola), `PruebasPermisosUbicacionAndroid.cs` (manifiesto).

## Tasks & Acceptance

**Execution:**
- [ ] Plugin Java con servicio, notificación y cola.
- [ ] Manifiesto por post-proceso y su prueba.
- [ ] `IUbicacion.IniciarRuta/ActualizarNotificacion/TerminarRuta` en adaptador, servicio y falsos.
- [ ] Permiso `POST_NOTIFICATIONS` y aviso si se deniega.
- [ ] Pruebas EditMode de la lógica de pausa y de la cola (fuente falsa).

**Acceptance Criteria:**
- Dada la build de Android, cuando se compila, entonces el manifiesto tiene el servicio con tipo `location` y no tiene `ACCESS_BACKGROUND_LOCATION`.
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes: todas pasan.
- `CompilarApk` por lotes: APK generado; revisar el manifiesto fusionado.

**Manual checks:**
- En un Android real: notificación fija, posiciones con la pantalla apagada 2 min, la notificación desaparece al parar.
