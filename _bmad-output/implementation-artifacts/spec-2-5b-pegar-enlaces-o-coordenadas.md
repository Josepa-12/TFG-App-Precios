---
title: 'Historia 2.5 (parte 2): pegar un enlace o unas coordenadas en el buscador'
type: 'feature'
created: '2026-11-20'
status: 'draft'
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
- [ ] `LectorUbicacionCompartida` y sus pruebas.
- [ ] `IResolutorEnlaces` + `ResolutorEnlacesWeb` + Arranque.
- [ ] Resultado `Coordenada`, `FilaResultado`, `PantallaMapa` (buscar, elegir, mensaje) y TalkBack.
- [ ] Textos, Localization y captura «enlace».
- [ ] Pruebas de pantalla: filas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `enlace.png`, entonces la primera fila es «Ir a este punto» con sus coordenadas; el registro no tiene errores.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: copiar un enlace de Google Maps (también corto, de «Compartir») y pegarlo en el buscador.
