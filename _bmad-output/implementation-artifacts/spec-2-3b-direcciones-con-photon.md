---
title: 'Historia 2.3 (parte 2): direcciones con Photon'
type: 'feature'
created: '2026-11-19'
status: 'done'
baseline_commit: '24c449eea2bc36c217bf494ff8c05afa05666087'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El buscador (2.3, parte 1) solo encuentra lugares de la app y puntos de interés. Faltan las direcciones (FR14, UX-DR16).

**Approach:** Un puerto `IGeocodificador` (Aplicación) con su adaptador Photon (nuevo asmdef `PriceMap.Adaptadores.Busqueda`). Al teclear, tras una espera, las direcciones se añaden debajo de los resultados locales.

**Decisiones del 19/11/2026:** TalkBack de las direcciones va en otra parte (en `deferred-work`). «Buscar como dirección» busca ya y sirve de reintento. Tocar una dirección centra el mapa y pliega la hoja.

## Boundaries & Constraints

**Always:**
- **Petición:** `https://photon.komoot.io/api/?q=<texto>&lat=<lat>&lon=<lon>&limit=8&lang=default`. Sin bbox: todo el mundo, con prioridad por cercanía. User-Agent `PriceMap/1.0`. Lat/lon es el punto de cercanía de la parte 1.
- **Cuándo:** 350 ms después de la última tecla, con al menos 3 caracteres normalizados. Cada tecla cancela la espera y la petición anteriores. Nunca se pinta la respuesta de un texto viejo.
- **Orden:** lugares, puntos y después direcciones (8 como mucho). Fila: icono `pin`, la dirección arriba y debajo «Dirección · <localidad> · a 1,2 km».
- **«Buscar como dirección»:** botón junto a «No hay resultados». Sale cuando no hay resultados locales y aún no hay direcciones: en la espera, con menos de 3 caracteres o tras un fallo. Al tocarlo pide ya, sin espera ni mínimo.
- **Tocar una dirección:** centra el mapa en ella y pliega la hoja, como un punto. Su ficha llega en la 2.5.
- **Sin conexión o fallo:** los locales siguen y se ve «Las direcciones necesitan conexión». Los errores van como `Resultado<T>`, sin excepciones hacia la UI.
- **FR17:** las direcciones no se guardan.

**Never:** TalkBack de las direcciones (otra parte); geocodificación inversa ni ficha de punto libre (2.5); caché ni historial; otra API.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Dirección | "calle ancha 10", con red | Una petición a los 350 ms; "Calle Ancha 10 …" tras los locales | N/A |
| Tecleo rápido | "cal"…"calle" en < 350 ms | Una sola petición, con "calle" | Esperas anteriores canceladas |
| Corto | "ca" | Sin petición; el botón sale si no hay locales | N/A |
| Botón | "ca" y tocar el botón | Petición ya con "ca" | N/A |
| Sin conexión | "calle ancha", sin red | Locales, aviso y botón (reintento) | `Resultado.Fallo` |
| Tardía | Llega "call" con el campo en "calle" | Se descarta | N/A |
| Borrar | X con una petición en curso | Se cancela; vuelven las categorías | N/A |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts`.
- **Aplicacion**:
  - Nuevos `IGeocodificador.cs`: `Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct)`. Y `Direccion` con `Nombre`, `Localidad` y `Posicion`.
  - `ResultadoBusqueda.cs`: `TipoResultado.Direccion` y `DeDireccion(direccion, metros)`.
  - Nuevo `BuscadorDirecciones.cs`:
    - `Pedir(consulta, cerca)`, `PedirYa(consulta, cerca)` y `Cancelar()`.
    - Espera inyectable `Func<TimeSpan, CancellationToken, Task>`, para que las pruebas no esperen.
    - Evento `Cambiado(consulta, Resultado<IReadOnlyList<ResultadoBusqueda>>)`.
    - `ServicioBusqueda` no cambia.
- **Adaptadores/Busqueda** (nuevo asmdef, refs Aplicacion y Dominio; `AssemblyInfo` con `InternalsVisibleTo("PriceMap.Tests.EditMode")`, como `Adaptadores/Gps`). Añadirlo a las referencias del asmdef de pruebas.
  - `LecturaPhoton.cs` (pura): GeoJSON → `Direccion` con `JsonUtility`.
    - `coordinates` viene como [lon, lat].
    - Nombre: `name` o `street` + `housenumber`. Localidad: `city`, `district` o `county`.
    - Fuera los registros sin nombre ni calle.
  - `GeocodificadorPhoton.cs`: `UnityWebRequest.Get` con User-Agent, `await` de la operación (Unity 6) y `ct.Register(req.Abort)`. Cualquier fallo → `Resultado.Fallo`.
- **UI**:
  - `PantallaMapa.Buscar` (l.420): llama también a `direcciones.Pedir`. Con `Cambiado` del texto actual repinta locales + direcciones; reutiliza `ultimosResultados` (l.409-436) para no romper el anuncio de la parte 1. `ElegirResultado` (l.476): Dirección = Punto. `BorrarBusqueda` (l.485) cancela.
  - `HojaBuscador.MostrarResultados` (l.371): direcciones, Label de aviso bajo la lista y botón (≥ 48 dp) junto a `SinResultados` (l.219), con un evento.
  - `FilaResultado` (`GlifoDe` l.77, `TipoDe` l.84): rama Dirección.
- **Arranque** — `Start`/`Componer` (l.49, 86-104): `new BuscadorDirecciones(new GeocodificadorPhoton(), Task.Delay)` a `Inicializar` (opcional, como `busqueda`).
- **Textos** — `ClavesTexto` (l.144-179), `Editor/PrepararInterfaz.cs` (l.132) y Localization, como en la parte 1: «Dirección», «Las direcciones necesitan conexión» y «Buscar como dirección».
- **Captura** — `CapturaPruebas.Busqueda()` (l.750): escribe "calle ancha 10" y espera. Si hay direcciones, guarda "busqueda-direccion". Sin red, solo lo registra, sin error.

## Tasks & Acceptance

**Execution:**
- [x] `Aplicacion/IGeocodificador.cs`, `Direccion`, `ResultadoBusqueda.cs`, `BuscadorDirecciones.cs` -- puerto, tipo y espera/cancelación.
- [x] `Adaptadores/Busqueda/*` y asmdef de pruebas -- `LecturaPhoton` y `GeocodificadorPhoton`.
- [x] `UI/HojaBuscador.cs`, `UI/FilaResultado.cs`, `UI/PantallaMapa.cs`, `UI/PantallaMapa.uss` -- filas, aviso, botón y toque.
- [x] `Arranque/Arranque.cs`, textos, Localization y `Arranque/CapturaPruebas.cs`.
- [x] `Tests/EditMode/PruebasDirecciones.cs` -- `LecturaPhoton` con un JSON de ejemplo; `BuscadorDirecciones` y la hoja con un geocodificador y una espera falsos, una prueba por fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas y ninguna sale a la red.
- Dada la build de PC con red, cuando se busca "calle ancha 10", entonces el registro no tiene errores y aparece al menos una dirección.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Revisión del 19/11 (blind, edge-case, verification-gap).

| # | Hallazgo | Veredicto | Ruta | Evidencia |
|---|----------|-----------|------|-----------|
| 1 | Al volver a desplegar, las direcciones guardadas conservan la distancia vieja mientras los locales se recalculan (edge, vg) | medium | patch | `PedirDirecciones` salía sin tocar `direccionesActuales`; ahora las recalcula con `PuntoCercania()`. Prueba `AlVolverADesplegarLasDistanciasSeMidenDesdeElPuntoDeAhora`. |
| 2 | Photon repite tramos de una calle (blind, edge) | medium | patch | La captura mostraba dos "Calle Ancha 10 · Marbella"; `LecturaPhoton` quita repetidos por nombre y localidad. Prueba `LosTramosRepetidosDeUnaCalleSalenUnaVez`. |
| 3 | Se manda la posición exacta (6 decimales) a un tercero (blind) | medium | patch | `Url` usa 3 decimales (~100 m), que bastan para ordenar. |
| 4 | Una excepción al pintar las direcciones se pierde en una tarea descartada (blind) | low | patch | `AlCambiarDirecciones` la registra con `Debug.LogException`. |
| 5 | `Componer` podría dejar de pasar las direcciones sin que falle nada (vg) | medium | patch | Prueba `ElArranqueLeDaLasDireccionesALaPantalla`. |
| 6 | Volver a pedir tras un fallo al desplegar, sin probar (vg) | medium | patch | Prueba `AlVolverADesplegarTrasUnFalloSePideOtraVez`. |
| 7 | No se prueba que la lista no salte al llegar las direcciones (vg) | maybe-false (medium si es cierto) | defer | Sin panel dispuesto no hay `scrollOffset` en EditMode; se ve en el móvil. |
| 8 | `JsonUtility` podría dar `features` vacío en vez de null (blind) | false | — | `UnaRespuestaRotaEsUnFallo` con `{"message":"error"}` pasa (538/538). |
| 9 | 429/5xx se anuncia como "necesitan conexión" (blind, edge) | false | — | El spec pide ese aviso para "sin conexión o fallo". |
| 10 | Botón y aviso fuera de TalkBack (edge) | false | — | Excluido por la intención: parte 3 en `deferred-work`. |
| 11 | Tocar "Buscar como dirección" con una petición en curso la reinicia (edge) | false | — | Es lo pedido: "pide ya". |
| 12 | `req.Abort` desde otro hilo (edge) | false | — | Solo se cancela desde la UI, en el hilo principal. |
| 13 | Diferencias solo de espacios o mayúsculas repiten la petición o descartan la respuesta (edge) | low | rechazado | Una petición de más; nada visible. |
| 14 | Dirección que repite un lugar local (edge) | low | rechazado | Raro y sin daño: dos filas del mismo sitio. |
| 15 | Se rehacen todas las filas al llegar las direcciones (blind) | low | rechazado | Mismo patrón que la parte 1; la escala se aplica por fila. |
| 16 | Tipo de Photon, localidad con `state`/`country`, distancias lejanas sin límite (blind) | low | rechazado | El spec fija los campos; "a 9.400 km" se lee bien. |
| 17 | Clave de detalle anidada y "a" solo en direcciones (blind) | low | rechazado | Texto traducible; diferencia menor. |
| 18 | Ningún marcador en el mapa al elegir una dirección (blind) | low | rechazado | La ficha de punto libre llega en la 2.5. |
| 19 | La captura cambia el tema y no lo restaura (blind, edge) | low | rechazado | Es el último paso de la captura. |
| 20 | Botón perdido tras una respuesta vacía de `PedirYa` corto (edge) | low | rechazado | Repetir daría lo mismo. |
| 21 | `DeDireccion` sin comprobar null; `Direccion` en el archivo del puerto (blind) | low | rechazado | Solo se llama con direcciones ya filtradas. |
| 22 | CLAUDE.md, memoria y sprint sin actualizar (blind) | — | — | Se actualizan al cerrar la historia. |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura; después, limpiar `link.xml` y `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: una dirección y el modo avión.
