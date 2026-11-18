---
title: 'Historia 2.5: ir a un punto libre o a una ubicación compartida'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 2'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Solo se puede ir a lugares de la app. El usuario quiere ir a un punto cualquiera del mapa o a una ubicación que le mandaron (FR16, FR17).

**Approach:** Una "ficha de punto libre" (misma hoja que la ficha del lugar, sin precios) con la dirección por geocodificación inversa de Photon. Se abre con una pulsación larga en el mapa, desde un enlace compartido con la app (intent de Android) o pegando el enlace o unas coordenadas en el buscador. Un analizador puro reconoce los formatos.

## Boundaries & Constraints

**Always:**
- **Pulsación larga:** 500 ms sin moverse más de 6 dp. `IMapa` lanza `PulsacionLarga(PuntoPantalla)` y convierte a coordenada con un método nuevo `Desproyectar`. Se coloca una chincheta en el punto.
- **Ficha de punto libre:** título con la dirección ("Calle Ancha, 10") o, mientras carga o sin conexión, las coordenadas "36,7781, -6,3515"; subtítulo "Punto en el mapa". Misma fila de acciones (Ir llega en la 2.6). Se cierra como la ficha del lugar.
- **Geocodificación inversa:** `IGeocodificador.Inverso(Coordenada)` (Photon `/reverse`).
- **Analizador de enlaces** (`LectorUbicacionCompartida`, Dominio, puro): reconoce
  - Google Maps `?q=lat,lng`, `@lat,lng,zoom`, `!3d<lat>!4d<lng>`, `ll=`/`query=`.
  - Apple Maps `ll=lat,lng` y `q=`.
  - `geo:lat,lng` (con `?q=lat,lng(nombre)`).
  - Coordenadas sueltas "36.7781, -6.3515" o "36,7781 -6,3515" (decimales con coma o punto).
  - Valida rangos (lat ±90, lon ±180).
- **Enlaces cortos** (`maps.app.goo.gl`, `goo.gl/maps`): se resuelven siguiendo la redirección con una petición HEAD/GET sin seguir automáticamente, leyendo `Location`, hasta 5 saltos; luego se analiza la URL final. Por un puerto `IResolutorEnlaces`.
- **Compartir con la app:** intent-filter `ACTION_SEND` (text/plain) y `ACTION_VIEW` con esquema `geo` en el manifiesto; Unity lee el intent al arrancar y al volver (`onNewIntent` vía plugin o `Application.absoluteURL`/`deepLinkActivated`).
- **Pegar en el buscador:** si el texto es un enlace o coordenadas reconocibles, el primer resultado es "Ir a este punto".
- **No reconocido:** mensaje "No reconozco esta ubicación. Prueba a copiar el enlace completo." (toast de 3 s y anuncio de TalkBack) y no abre nada.
- Los puntos libres no se guardan en historial (FR17).

**Never:** sin guardar como favorito (3.6), sin API de Google.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Pulsación larga | 600 ms quieto en el mapa | Chincheta y ficha con la dirección |
| Pulsación con arrastre | 600 ms moviéndose 20 dp | El mapa se mueve, sin ficha |
| Google `?q=` | `https://maps.google.com/?q=36.778,-6.351` | Ficha en 36.778, -6.351 |
| Google `@` | `.../@36.778,-6.351,17z` | Ficha en ese punto |
| Google `!3d!4d` | `...!3d36.778!4d-6.351` | Ficha en ese punto |
| Apple | `https://maps.apple.com/?ll=36.778,-6.351` | Ficha |
| geo | `geo:36.778,-6.351?q=36.778,-6.351(Casa)` | Ficha |
| Coordenadas con coma | "36,778 -6,351" | Ficha |
| Enlace corto | `https://maps.app.goo.gl/abc` → redirección a `@36.77,-6.35` | Ficha |
| Fuera de rango | "95, 200" | Mensaje de no reconocido |
| Sin conexión | Pulsación larga | Ficha con coordenadas como título |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/LectorUbicacionCompartida.cs`, `Aplicacion/IResolutorEnlaces.cs`, `Aplicacion/ServicioDestino.cs` (destino actual: lugar o punto libre), `UI/FichaPunto.cs` (o modo de `FichaLugar`).
- `Aplicacion/IMapa.cs`: `PulsacionLarga`, `Desproyectar`; `Vectorial/CamaraMapa.cs`, `MapaVectorial.cs`, `MapaFalso`.
- `Adaptadores/Busqueda/`: `GeocodificadorPhoton.Inverso`, `ResolutorEnlacesWeb`.
- Manifiesto: `Editor/PermisosUbicacionAndroid.cs` (intent-filters). Lectura del intent en `Adaptadores/Sistema/`.
- Tests: `PruebasUbicacionCompartida.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `LectorUbicacionCompartida` con cada formato y sus pruebas.
- [ ] Pulsación larga y `Desproyectar` en el mapa.
- [ ] Geocodificación inversa y resolución de enlaces cortos.
- [ ] Ficha de punto libre, intents de Android y pegado en el buscador.
- [ ] Mensaje de no reconocido; captura "punto".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada formato de la matriz se reconoce o se rechaza como dice.
- Dada la build de Android, cuando se comparte una ubicación de WhatsApp, entonces PriceMap aparece en "Compartir con" (comprobación manual).

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "punto".

**Manual checks:**
- Compartir desde WhatsApp y Google Maps en el móvil (incluido enlace corto).
