---
title: 'Historia 2.6: ver los tiempos y calcular la ruta más corta'
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

**Problem:** La fila de acciones de la ficha está vacía (1.8) y no hay rutas (FR7, FR10, FR18).

**Approach:** La fila de acciones muestra coche (por defecto) y a pie con sus minutos y el botón Ir. Un puerto `IRutas` con dos adaptadores: Valhalla FOSSGIS (`shortest`) y OSRM de reserva (la más corta de sus alternativas). La ruta se dibuja en el mapa por `IMapa.MostrarRuta`. La navegación giro a giro es la 2.7: aquí, Ir calcula y dibuja la ruta y deja el modo "vista previa de ruta".

## Boundaries & Constraints

**Always:**
- **Modelo** (Dominio): `ModoViaje` (Coche, APie), `Ruta` (lista de `Coordenada`, distancia en metros, duración en segundos, lista de `Maniobra` con tipo, texto, coordenada y distancia), sin tipos de Unity.
- **Valhalla:** `POST https://valhalla1.openstreetmap.de/route` con `costing` `auto` o `pedestrian`, `costing_options.<c>.shortest: true`, `language: es-ES`, `directions_type: maneuvers`. Decodifica la polilínea (precisión 6).
- **OSRM de reserva:** `https://router.project-osrm.org/route/v1/{driving|foot}/…?alternatives=3&steps=true&overview=full&geometries=polyline6`; se elige la de menor distancia. Si OSRM no da texto en castellano, las maniobras se redactan con una tabla propia (`TextosManiobra`).
- **Tiempos de la fila:** al abrir la ficha con posición `Activa`, se piden las dos rutas (coche y a pie) en paralelo y se muestran "6 min" y "15 min". Mientras: "…". Sin posición: la fila muestra solo Ir desactivado con "Activa la ubicación para ir". Caché por (origen redondeado a 20 m, destino, modo) durante 2 min.
- **Punto de calle:** origen y destino se pasan tal cual; el enrutador ya ajusta al punto de calle más cercano (`radius`/`snapping`), y la línea dibujada empieza y acaba en esos puntos.
- **Sentido de circulación:** lo respeta `auto` de Valhalla y `driving` de OSRM; prueba con respuesta grabada.
- **Dibujo:** `IMapa.MostrarRuta(IReadOnlyList<Coordenada>, EstiloRuta)` y `QuitarRuta()`. Línea de 6 dp con el color `--color-route` (clásica). El mapa encuadra la ruta entera.
- **Sin conexión:** "ruta aproximada": línea recta discontinua del origen al destino, con distancia en línea recta y el aviso "Sin conexión: ruta aproximada" (UX-DR16).
- **TalkBack:** "Coche, 6 minutos, seleccionado", "A pie, 15 minutos", "Ir a Clínica Areas".
- Peticiones con `Resultado<T>`, tiempo máximo 8 s por servicio, `User-Agent` genérico.

**Never:** sin navegación giro a giro (2.7), sin GPS en segundo plano (2.7), sin claves de API de pago.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Abrir ficha con posición | `Activa` | Coche y a pie con minutos; coche seleccionado |
| Sin posición | `Buscando` | Ir desactivado con su texto |
| Ir en coche | Valhalla responde | Línea dibujada y encuadrada |
| Valhalla cae | Timeout | OSRM, la más corta de las alternativas |
| Ambos caen / sin conexión | Sin red | Línea recta discontinua y aviso |
| Sentido único | Respuesta grabada con calle de sentido único | La ruta no la recorre al revés |
| Cambiar a pie | Toque en "A pie" | Seleccionado; Ir usa `pedestrian` |
| Polilínea | Cadena polyline6 conocida | Coordenadas esperadas |

</frozen-after-approval>

## Open Questions

1. **Tamaño.** Es una historia grande. A) Partir: parte 1 = puerto, adaptadores, polilínea y fila de tiempos; parte 2 = dibujo de la ruta, encuadre, sin conexión y TalkBack (recomendado). B) Una sola.

## Code Map

- Nuevos Dominio: `ModoViaje.cs`, `Ruta.cs`, `Maniobra.cs`, `TipoManiobra.cs`, `Polilinea.cs` (decodificar).
- Nuevos Aplicación: `IRutas.cs`, `ServicioRutas.cs` (tiempos, reserva, caché).
- Nuevo adaptador `Adaptadores/Rutas/` (asmdef): `RutasValhalla.cs`, `RutasOsrm.cs`, `RutasConReserva.cs`.
- `Aplicacion/IMapa.cs`: `MostrarRuta`, `QuitarRuta`, `Encuadrar`; `Vectorial/` (nuevo `LineaRuta.cs` con malla), `MapaFalso`.
- `UI/FichaLugar.cs` (`fila-acciones` de la 1.8), nuevo `UI/FilaAcciones.cs`, textos.
- Tests: `PruebasRutas.cs` con JSON grabados en `Tests/EditMode/Datos/`.

## Tasks & Acceptance

**Execution:**
- [ ] Modelo, polilínea y pruebas.
- [ ] Adaptadores Valhalla y OSRM (parseo con respuestas grabadas) y reserva.
- [ ] `ServicioRutas` con tiempos de los dos modos y caché.
- [ ] Fila de acciones con modos, minutos e Ir; TalkBack.
- [ ] Dibujo y encuadre de la ruta; ruta aproximada sin conexión.
- [ ] Captura "ruta".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas sin red.
- Dada la build de PC con una ruta grabada, cuando se abre `ruta.png`, entonces se ve la línea de la ruta y la fila con los dos modos.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "ruta" (con `IRutas` falsa).

**Manual checks:**
- En el móvil con datos: tiempos reales y ruta en coche por calles de sentido único.
