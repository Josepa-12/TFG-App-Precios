---
title: 'Historia 2.5 (parte 2): pegar un enlace o unas coordenadas en el buscador'
type: 'feature'
created: '2026-11-20'
status: 'done'
baseline_commit: '019959de128d6cd0551828cc0b4b54e661810748'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Si a alguien le mandan una ubicación (un enlace de Google Maps por WhatsApp, unas coordenadas), no puede abrirla en PriceMap (FR17).

**Approach:** Al pegar en el campo del buscador un enlace de mapas reconocible o unas coordenadas, el primer resultado es «Ir a este punto», que abre la ficha de punto libre de la parte 1. Un analizador puro reconoce los formatos y los enlaces cortos se resuelven siguiendo sus redirecciones.

**Decisiones del 20/11/2026:**
- El mensaje de no reconocido va dentro de la hoja, en el sitio de «No hay resultados», y se queda mientras el texto siga ahí (no hay toast).
- Mientras se resuelve un enlace corto se ve «Comprobando el enlace…» en ese mismo sitio; al llegar, lo sustituye «Ir a este punto» (o el mensaje de no reconocido).
- Maqueta aprobada: https://claude.ai/artifact/1uonskQkovff6YTTnktWFj. Icono de la fila: la chincheta en negativo (círculo azul marino con la gota blanca; en oscuro, círculo blanco y gota azul marino).

## Boundaries & Constraints

**Always:**
- **Analizador** (`LectorUbicacionCompartida`, Dominio, puro), sobre el texto tal cual:
  - Google Maps `?q=lat,lng`, `@lat,lng,zoom`, `!3d<lat>!4d<lng>` (gana sobre `@` si están los dos), `ll=`/`query=`.
  - Apple Maps `ll=lat,lng` y `q=lat,lng`.
  - `geo:lat,lng` (con `?q=lat,lng(nombre)`, que gana).
  - Coordenadas sueltas: «36.7781, -6.3515», «36.7781 -6.3515», «36,7781 -6,3515», «36,7781; -6,3515».
  - Valida rangos (lat ±90, lon ±180). El nombre del enlace se ignora: la ficha pone la dirección de Photon.
- **Enlaces cortos** (`maps.app.goo.gl`, `goo.gl/maps`): puerto `IResolutorEnlaces` que sigue las redirecciones leyendo `Location` (hasta 5 saltos) y analiza la URL final. Se cancela si cambia el texto; una respuesta tardía no pinta nada.
- **Resultado «Ir a este punto»:** primera fila, con las coordenadas («36,7781, -6,3515») de subtítulo y el glifo de la chincheta. Tocarla (o activarla con TalkBack) abre la ficha de punto libre en esa coordenada.
- **Con un enlace o coordenadas reconocidos** no se buscan lugares, puntos ni direcciones.
- **No reconocido:** si el texto parece un enlace (`http`, `geo:`, `maps.`) y no se reconoce, o el enlace corto falla, se ve y se anuncia «No reconozco esta ubicación. Prueba a copiar el enlace completo.». Lo demás se busca como siempre.
- TalkBack: la fila es un botón «Ir a este punto, 36,7781, -6,3515», y se anuncia «1 resultado» como en la búsqueda.

**Never:** intents de Android (parte 3); guardar nada (FR17); API de Google.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Google `?q=` | `https://maps.google.com/?q=36.778,-6.351` | «Ir a este punto» en 36.778, -6.351 |
| Google `@` | `…/@36.778,-6.351,17z` | Ese punto |
| Google `!3d!4d` | `…/@36.7,-6.3,17z/data=!3d36.778!4d-6.351` | 36.778, -6.351 |
| Apple | `https://maps.apple.com/?ll=36.778,-6.351` | Ese punto |
| geo | `geo:0,0?q=36.778,-6.351(Casa)` | 36.778, -6.351 |
| Coma decimal | «36,778 -6,351» | Ese punto |
| Enlace corto | `https://maps.app.goo.gl/abc` → `…/@36.77,-6.35,17z` | Ese punto, tras resolverlo |
| Fuera de rango | «95, 200» | No es una coordenada: búsqueda normal |
| Enlace raro | `https://maps.google.com/?cid=123` | Mensaje de no reconocido |
| Corto sin red | Enlace corto y falla la red | Mensaje de no reconocido |
| Tocar la fila | «Ir a este punto» | Ficha de punto libre en esa coordenada |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts` salvo otra indicación.
- Nuevo `Dominio/LectorUbicacionCompartida.cs`: `Leer(string) → Coordenada?` y `PareceEnlace(string)`, `EsEnlaceCorto(string)`. `ReglaBusqueda.Normalizar` destroza signos: no usarlo. `Coordenada` (l.20-38) no valida rangos.
- `Aplicacion/ResultadoBusqueda.cs`: `TipoResultado` (l.9-14) + `Coordenada`, factoría `DeCoordenada` (l.59-70).
- Nuevo `Aplicacion/IResolutorEnlaces.cs` (`Task<Resultado<string>> Resolver(string url, CancellationToken)`) y `Adaptadores/Busqueda/ResolutorEnlacesWeb.cs`: como `GeocodificadorPhoton.Pedir` (l.69-93) con `redirectLimit = 0` y `GetResponseHeader("Location")`.
- `UI/PantallaMapa.cs`: en `Buscar` (l.464-481), antes de buscar, el analizador; con coordenada, `localesActuales = [DeCoordenada]` y sin `PedirDirecciones`. Enlace corto: petición con su CTS, como `PedirDireccionPunto` (l.1052+). `Juntar`/`MismosResultados` (l.591-612) con el tipo nuevo. `ElegirResultado` (l.650-656) → `AbrirPunto(resultado.Posicion)` (l.1018).
- `UI/HojaBuscador.cs`: `MostrarResultados` (l.472-514) y `SinResultados` para el mensaje. `UI/FilaResultado.cs`: ramas en `GlifoDe` (l.83-86, glifo de la chincheta, `ChinchetaPunto.Dibujar`), clase del icono (l.39-41), `DetalleDe` (l.92-99, `FichaLugar.TextoCoordenadas`), `TipoDe` (l.105-111) y `Anuncio` (l.68-76).
- `Arranque/Arranque.cs`: crea `ResolutorEnlacesWeb` y lo pasa por `Componer` (l.95-98) → `PantallaMapa.Inicializar` (l.71/83), opcional como `geocodificador`.
- Textos: «Ir a este punto», el mensaje y «Comprobando el enlace…» en `ClavesTexto`, `Editor/PrepararInterfaz.cs` y `PrepararInterfaz.Asegurar` por lotes.
- `Arranque/CapturaPruebas.cs`: paso «enlace» que escribe un enlace de Google `@` y captura la fila.
- Pruebas: `Tests/EditMode/PruebasUbicacionCompartida.cs` (analizador: cada fila de la matriz y casos raros) y pantalla con un resolutor falso (como `GeoFalso`/`EsperaFalsa` de `PruebasDirecciones.cs` l.33-76).

## Tasks & Acceptance

**Execution:**
- [x] `LectorUbicacionCompartida` y sus pruebas.
- [x] `IResolutorEnlaces` + `ResolutorEnlacesWeb` + Arranque.
- [x] Resultado `Coordenada`, `FilaResultado`, `PantallaMapa` (buscar, elegir, mensaje) y TalkBack.
- [x] Textos, Localization y captura «enlace».
- [x] Pruebas de pantalla: filas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `enlace.png`, entonces la primera fila es «Ir a este punto» con sus coordenadas; el registro no tiene errores.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | Ninguna prueba comprueba que `Componer` lleve el resolutor a la pantalla (verification-gap) | medium | `PruebasArranque` nunca pasaba `resolutorEnlaces`; las pruebas de pantalla llamaban a `Inicializar` directamente | patch: `ComponerEntregaElResolutorDeEnlacesAlBuscador` |
| 2 | El bucle de saltos de `ResolutorEnlacesWeb` no tiene pruebas (verification-gap, blind) | medium | Solo se probaba `Absoluta`; `Siguiente` era estático con red | patch: constructor `internal` con el salto + 5 pruebas sin red |
| 3 | Pasar de una coordenada a otra no tiene prueba de anuncio (verification-gap) | low | Las pruebas de TalkBack solo cubrían 0→1 y 0→0 | patch: `TalkBack_OtraCoordenadaSeAnunciaOtraVez` |
| 4 | Enlaces de mapas sin `https://` no se extraen; un corto sin esquema se pedía con el texto entero (verification-gap, blind, edge) | medium | `Enlace` solo casaba `https?://`/`geo:`; `ExtraerEnlace(...) ?? texto.Trim()` mandaba «Bar maps.app.goo.gl/x» | patch: `Enlace` acepta dominios de mapas sin esquema y les pone `https://` |
| 5 | `EsEnlaceCorto` miraba el texto suelto, no el servidor (blind) | low | `IndexOf` en todo el texto: «https://ejemplo.com/x maps.app.goo.gl» pedía ejemplo.com | patch: comprueba el servidor del primer enlace |
| 6 | Un corto con `http://` falla en Android (blind, edge) | low | Unity bloquea http en claro por defecto | patch: `ConHttps` antes de cada salto |
| 7 | La URL resuelta va al registro en la versión de publicación (blind) | low | `Debug.Log("… URL final " + r.Valor)` sin condición | patch: la URL solo con `Debug.isDebugBuild` |
| 8 | Aserción con `||` que pasa siempre (blind) | low | `BuscarDireccionVisible || geo.Busquedas.Contains("zzzz")` | patch: `CollectionAssert.Contains(geo.Busquedas, "zzzz")` |
| 9 | «1,5 2,5» se toma por coordenadas (edge) | low | `Sueltas` aceptaba un decimal | patch: dos decimales al menos |
| 10 | Con «cerca» activa y el campo vacío, el enlace pendiente no se olvida (edge) | low | `Buscar("")` volvía antes de `OlvidarEnlace` | patch: `OlvidarEnlace()` en esa rama |
| 11 | Cortos que resuelven a URL sin coordenadas (`?q=Nombre&ftid=`, `/maps/place/Nombre/data=…1s0x…`) o con `/maps/search/lat,+lng` (blind, edge) | maybe-false (medium si es cierto) | Depende de lo que devuelva Google hoy; sin red no se comprueba | defer |
| 12 | Coordenadas en grados/minutos/segundos o con etiqueta delante (blind) | low | Formatos fuera de los fijados en el spec; añadirlos es otra rama de análisis | rechazado |
| 13 | Enlaces de OSM, `dir/?destination=`, `center=`, `!2d!3d` (blind, edge) | low | Poco frecuentes al compartir; añaden ramas | rechazado |
| 14 | El mensaje aconseja «copiar el enlace completo» ante una web cualquiera (blind) | low | El texto y la regla `http` los fija el spec | rechazado (cambiaría el spec) |
| 15 | Etiqueta TalkBack con comas decimales difícil de oír (blind) | low | La etiqueta la fija el spec | rechazado (cambiaría el spec) |
| 16 | Sin tiempo máximo total al resolver (5 × tiempo por salto) (blind) | low | Lo normal son 1–2 saltos; añadir un plazo global es más lógica | rechazado |
| 17 | La distancia de `DeCoordenada` no se enseña (blind) | false | No causa ningún fallo; el campo es parte del resultado común | rechazado |
| 18 | `Assert.Ignore`/`Inconclusive` en la prueba del doble toque (blind) | false | Misma pauta que todas las suites de TalkBack: limitación del editor | rechazado |
| 19 | Sprint y pendientes sin actualizar (blind) | false | Se actualizan al cerrar la historia (paso 5) | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: copiar un enlace de Google Maps (también corto, de «Compartir») y pegarlo en el buscador.
