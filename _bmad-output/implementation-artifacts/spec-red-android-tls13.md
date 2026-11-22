---
title: 'Arreglo: peticiones HTTPS con la red de Android (TLS 1.3)'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
baseline_commit: 'ae350189de2d0890c40f2991ce17a35a66f2464b'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En el móvil (Redmi 10 2022, Android 13) no salen rutas ni direcciones. El registro dice «Valhalla/OSRM no responde (ConnectionError): Unable to complete SSL connection»: los servidores de FOSSGIS (`valhalla1.openstreetmap.de`, `routing.openstreetmap.de`) solo aceptan TLS 1.3 y `UnityWebRequest` en Android solo habla TLS 1.2. Photon sí acepta TLS 1.2, pero su búsqueda no se ha llegado a probar en el móvil; se pasa también a la red de Android para que nada dependa de la versión de TLS de cada servidor. En el PC nunca se vio: las capturas usan rutas falsas.

**Approach:** En Android, las peticiones de rutas, direcciones (Photon, también la inversa) y enlaces cortos las hace la red del sistema (`HttpURLConnection`, TLS 1.3 desde Android 10) con un puente Java; en el PC y el editor, `UnityWebRequest` como ahora. Cada fallo de red queda en el registro.

## Boundaries & Constraints

**Always:**
- Un solo cliente para los tres adaptadores (`PeticionRutas`, `GeocodificadorPhoton`, `ResolutorEnlacesWeb`): método (GET/POST), URL, cuerpo JSON, cabeceras (`User-Agent`, `Content-Type`), tiempo máximo y cancelación; devuelve código HTTP, texto y la cabecera `Location`. Mismos tiempos y mismos mensajes de fallo que hoy.
- Enlaces cortos: sin seguir redirecciones solo (como hoy, los saltos los cuenta `ResolutorEnlacesWeb`).
- Java: la petición en un hilo aparte, nunca en el de la interfaz ni en el de Unity; C# espera el resultado fotograma a fotograma, sin bloquear. Cancelar corta la conexión.
- Registro: cada petición fallida deja una línea `PriceMap: <servicio> …` con el error (como las de diagnóstico ya añadidas en `RutasValhalla`, `RutasOsrm` y `GeocodificadorPhoton.Buscar`).
- Las teselas del mapa (`MapaVectorial`, corrutinas) no cambian.

**Never:** cambiar de servidor; desactivar la comprobación de certificados; tocar la lógica de rutas o del buscador.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Rutas | Móvil con Internet, ficha de un negocio | Minutos de coche y a pie (Valhalla) |
| Direcciones | Buscar «calle ancha» | Filas de Photon con distancia |
| Enlace corto | Pegar un enlace `maps.app.goo.gl` | Se sigue la redirección y se abre el punto |
| Sin red | Modo avión | Mismos fallos que hoy (aproximada, aviso del buscador) y una línea en el registro |
| Cancelar | Teclear otra letra con Photon en camino | La petición vieja se corta; solo se pinta la última |
| PC | Editor y build de PC | Igual que hoy (`UnityWebRequest`) |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `Scripts/Adaptadores/Rutas/PeticionRutas.cs:36-67` (`Pedir`, POST/GET con `UnityWebRequest`, `await req.SendWebRequest()`), `Scripts/Adaptadores/Busqueda/GeocodificadorPhoton.cs:69-93` (`Pedir`, GET) y `Scripts/Adaptadores/Busqueda/ResolutorEnlacesWeb.cs:~50-80` (GET sin redirecciones, lee `Location`): pasan a usar el cliente común.
- Nuevo cliente en un sitio que vean `Rutas` y `Busqueda` (p. ej. asmdef nuevo `PriceMap.Adaptadores.Red`, referenciado por los dos y por `PriceMap.Arranque` si hace falta): `ClienteHttp.Pedir(...)` → en `#if UNITY_ANDROID && !UNITY_EDITOR`, el puente; si no, `UnityWebRequest`.
- Nuevo `Plugins/Android/PuenteRed.java` (`com.pricemap.red`): `int pedir(metodo, url, cuerpo, cabeceras, msMaximo, seguirRedirecciones)` en un `ExecutorService`; `estado(id)`, `codigo(id)`, `texto(id)`, `location(id)`, `error(id)`, `cancelar(id)` (desconecta), `liberar(id)`. JNI desde C# como `Adaptadores/Gps/ServicioRutaAndroid.cs:66-77` (`AndroidJavaClass` perezosa, try/catch).
- Espera fotograma a fotograma: `await Awaitable.NextFrameAsync(ct)` hasta que `estado(id)` ≠ en curso.
- Pruebas: las de los lectores y adaptadores siguen igual; nueva `Tests/EditMode/PruebasRed.cs` (el cliente en el editor con `UnityWebRequest`, el Java leído: hilo aparte, sin seguir redirecciones en enlaces, cancelar desconecta, sin tocar certificados).

## Tasks & Acceptance

**Execution:**
- [x] `PuenteRed.java` + `ClienteHttp` (asmdef) -- petición nativa y espera sin bloquear.
- [x] `PeticionRutas`, `GeocodificadorPhoton`, `ResolutorEnlacesWeb` -- usar el cliente; registro de fallos.
- [x] `PruebasRed.cs` -- pruebas.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dado `CompilarApk` e instalado por `adb` en el móvil, cuando se abre la ficha de un negocio y se busca «calle ancha», entonces el registro no tiene «Unable to complete SSL connection» y salen minutos y direcciones.

## Implementation Notes

- Diagnóstico ya hecho (sin commit): `RutasValhalla`/`RutasOsrm` registran el fallo; `GeocodificadorPhoton.Buscar` registra el número de direcciones o el fallo; `MapaVectorial.CrearSuelo` ya no usa `CreatePrimitive` (en Android la física se poda y daba «Can't add component because class 'MeshCollider' doesn't exist!»).

## Spec Change Log

## Review Triage Log

Revisión 1 (22/11/2026). Ruta: patch salvo donde se indica.

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | El tiempo máximo y cancelar solo llaman a `disconnect()`: si el hilo está en el DNS o aún sin socket, `estado` sigue EN_CURSO y C# espera de más (edge 1, 2, 4 y claim; blind 1) | medium | `reloj` solo pone `agotada` y desconecta; `estado` cambia solo al salir el hilo | patch: el reloj y `cancelar` fijan el estado final al momento; `terminar` solo desde EN_CURSO |
| 2 | `catch (Exception)` no recoge un `Error` (OOM): estado EN_CURSO para siempre | low | `PuenteRed.java:139` | patch: `Throwable` |
| 3 | `getResponseCode()` = -1 sale como Success | low | Raro y el arreglo añade una rama | rechazado |
| 4 | 3xx no seguido con `seguirRedirecciones=true` sale como Success vacío (edge, blind, verification) | low | Todas las URL son https; añade rama | rechazado |
| 5 | `IOException` al leer `getErrorStream` pasa a ConnectionError | low | Raro; añade guarda | rechazado |
| 6 | `RejectedExecutionException` deja la petición en el mapa | false | `newCachedThreadPool` sin `shutdown` nunca rechaza | rechazado |
| 7 | Desbordamiento de `SegundosMaximo * 1000` | false | Los adaptadores usan 8–10 s | rechazado |
| 8 | Claves de cabecera con salto de línea o «:» | false | Las claves son constantes del código | rechazado |
| 9 | POST con cuerpo `""` distinto en Android y PC | false | Ningún adaptador lo usa (`null` o JSON) | rechazado |
| 10 | «Más de 5 redirecciones» no deja línea en el registro | low | `ResolutorEnlacesWeb` devuelve `Resultado.Fallo` sin el ayudante `Fallo` | patch |
| 11 | `Log.w` de Java escribe la URL entera (búsqueda y coordenadas) en logcat | medium | `PuenteRed.java:145`; FR17 «nada se guarda» | patch: solo método y host |
| 12 | `Debug.Log` en cada búsqueda con éxito | low | Diagnóstico pedido en Implementation Notes para la prueba en el móvil | rechazado |
| 13 | Android 8–9 no tienen TLS 1.3 en `HttpURLConnection` (minSdk 26) | medium | Limitación anterior al arreglo; el arreglo cubre Android 10+ | defer |
| 14 | `reloj` sin `setRemoveOnCancelPolicy`: tareas canceladas retienen la `Peticion` hasta el límite | low | `ScheduledThreadPoolExecutor` por defecto | patch: una línea |
| 15 | Comentario de `ResolutorEnlacesWeb` solo describe el PC (en Android el 3xx es Success) | low | Línea del comentario | patch: comentario |
| 16 | Las teselas siguen con `UnityWebRequest` | false | El intent lo excluye («Las teselas del mapa no cambian») | rechazado |
| 17 | El permiso INTERNET depende de la detección automática | false | Hoy está (MapaVectorial usa `UnityEngine.Networking`) | rechazado |
| 18 | Sin regla ProGuard para `PuenteRed` | false | Minify apagado; igual que los otros puentes | rechazado |
| 19 | `CrearSuelo` mezclado en el arreglo | false | Recogido en Implementation Notes | rechazado |
| 20 | Puerto libre con `TcpListener` puede robarse; la prueba de cancelar no cierra la respuesta | low | Raro; añade complejidad | rechazado |
| 21 | Rama muerta en `PruebasCapas.CapasPermitidas` | low | La nueva línea tapa `|| StartsWith("PriceMap.Adaptadores.")` | patch: borrarla |
| 22 | (gap) `ResolutorEnlacesWeb.Siguiente` real sin prueba | gap | Verification-gap, verificado | patch: prueba con el servidor local (302, 200, 404) |
| 23 | (gap) `PeticionRutas.Pedir` sin prueba (POST, cabeceras, mensaje de fallo) | gap | Verification-gap, verificado | patch: prueba con el servidor local |
| 24 | (gap) `CrearSuelo` sin prueba de malla y sin collider | gap | Verification-gap, verificado | patch |
| 25 | (gap) Sin prueba de que `SegundosMaximo` corta un servidor mudo | gap | Blind | patch: prueba de 1 s en el editor |
| 26 | (gap) La ruta de Android solo con pruebas de texto | gap | EditMode no ejecuta JNI | defer (prueba en el móvil) |
| 27 | La prueba del 302 no fija `Resultado` | low | Las plataformas difieren y `ResolutorEnlacesWeb` mira el código | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarApk`, `adb install -r`, `adb logcat Unity:V *:S` mientras se pide una ruta y se busca una dirección -- sin errores de SSL; deshacer `Android/addressables_content_state.bin` y borrar `link.xml`.
