---
title: 'Historia 2.5 (parte 1): mantener pulsado el mapa abre un punto libre'
type: 'feature'
created: '2026-11-19'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Solo se puede ir a lugares de la app; el usuario quiere elegir un punto cualquiera del mapa (FR16). Las partes 2 (pegar enlaces) y 3 (compartir con la app) abrirán esta misma ficha.

**Approach:** Mantener pulsado el mapa pone una chincheta y abre una «ficha de punto libre» (la hoja de la ficha del lugar, sin precios) con la dirección por geocodificación inversa de Photon.

**Decisión del 19/11/2026:** mantener pulsado da siempre un punto libre, también encima de un marcador o de un punto de interés (como Google Maps); la ficha del sitio sigue abriéndose con un toque.

**Maqueta aprobada (19/11/2026):** https://claude.ai/artifact/H63tSuEdd2K5fGMv6RFvtS. Chincheta en forma de gota azul marino (`--color-text` claro) con borde blanco y centro blanco; en oscuro, blanca con centro azul marino y borde fino oscuro. La ficha lleva bajo el subtítulo «Buscando la dirección…» solo mientras espera a Photon.

## Boundaries & Constraints

**Always:**
- **Pulsación larga:** un dedo (o el botón izquierdo en PC) quieto 500 ms, sin moverse más de 6 dp. Se dispara al cumplirse el tiempo, sin esperar a soltar, y al soltar no hay toque. Un segundo dedo la anula.
- **Chincheta** en el punto mientras la ficha está abierta; desaparece al cerrarla.
- **Ficha de punto libre:** título con la dirección («Calle Ancha, 10») o, mientras carga, sin conexión o sin calle, las coordenadas («36,7781, -6,3515», 4 decimales y coma decimal). Subtítulo «Punto en el mapa»; debajo, la localidad si Photon la da. Sin precios ni muestra. Se abre, centra y cierra como la ficha del lugar (X, atrás, toque fuera).
- **Geocodificación inversa:** `IGeocodificador.Inverso(Coordenada, ct)` con Photon `/reverse`. Una respuesta tardía de un punto anterior no cambia la ficha.
- **TalkBack:** la ficha es modal como la del lugar: título, subtítulo y «Cerrar». Al llegar la dirección se rehace el título sin mover el foco.
- No se guarda nada (FR17).

**Never:** el botón «Ir» (2.6), pegar enlaces (parte 2), intents de Android (parte 3), favoritos (3.6), API de Google.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Pulsación larga | 600 ms quieto en el mapa | Chincheta y ficha; título con la dirección al llegar |
| Con arrastre | 600 ms moviéndose 20 dp | El mapa se mueve, sin ficha |
| Toque corto | Suelta a los 200 ms | Toque normal (2.x), sin ficha de punto |
| Sobre un marcador | 600 ms quieto encima de un bar | Punto libre en ese sitio, no la ficha del bar |
| Dos dedos | Segundo dedo antes de 500 ms | Gesto de dos dedos, sin ficha |
| Sin conexión | Photon falla | Título con las coordenadas |
| Respuesta tardía | Punto A y luego B antes de que llegue A | La ficha enseña la dirección de B |
| Cerrar | X, atrás o toque fuera | Ficha y chincheta desaparecen |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts` salvo otra indicación.
- `Vectorial/DetectorToque.cs`: lógica pura de toque (umbral 6 dp, `Empezar`/`Mover`/`Anular`/`Soltar`). Añadir el tiempo: un método por fotograma que dispare una vez a los 500 ms si no está anulado y no ha pasado del umbral; `Soltar` no da toque después.
- `Vectorial/CamaraMapa.cs`: `ToqueTactil` (l.74-85) y `ToqueRaton` (l.87-97) llaman al detector cada fotograma; evento `PulsacionLarga(Vector2)`. Al dispararse, que el arrastre no siga moviendo el mapa. `PuntoSuelo` (l.113-118) es el rayo contra el suelo.
- `Aplicacion/IMapa.cs`: `event Action<PuntoPantalla> PulsacionLarga` y `Coordenada? Desproyectar(PuntoPantalla)` (null si cae sobre el horizonte). `Vectorial/MapaVectorial.cs`: los implementa como `Toque` (l.268, l.311) y con `Geo.AGeo` (`Geo.cs` l.33).
- Los 11 `MapaFalso` de `Tests/EditMode` (Arranque, Buscador, Busqueda, Cerca, Direcciones, Ficha, Filtro, Marcadores, PantallaMapa, PuntosInteres, Ubicacion): añadir los miembros nuevos.
- `Aplicacion/IGeocodificador.cs` + `Adaptadores/Busqueda/GeocodificadorPhoton.cs`: `Inverso` con `https://photon.komoot.io/reverse?lat=..&lon=..&limit=1`, mismo GeoJSON (`LecturaPhoton.Leer`). Mismo trato de cancelación y fallos que `Buscar`. El título «Calle, número» se compone aparte (hoy `Nombre` sale sin coma).
- `UI/FichaLugar.cs`: modo punto libre que reutiliza `Velo`, `Hoja`, `Titulo`, `Tipo` (subtítulo), `Direccion` (localidad), `BotonCerrar`; oculta `Muestra` y vacía `Contenido`. `TextosTalkBack` (l.179).
- `UI/PantallaMapa.cs`: suscribirse a `PulsacionLarga`; `AbrirPunto(Coordenada)` junto a `AbrirFicha` (l.934-957); generalizar `CentrarEnFicha`/`RecentrarFichaPendiente` a coordenada; `CerrarFicha` quita la chincheta; con un modal de ubicación o el menú, como `TocarMapa` (l.921-931).
- Chincheta: nuevo `UI/ChinchetaPunto.cs`, colocada como `MarcadorPosicion.Actualizar` (l.59-90) desde `ActualizarCapas` (l.1415), en su propia capa. Aspecto según la maqueta aprobada.
- Textos: «Punto en el mapa» y el formato de coordenadas en `ClavesTexto`, `Editor/PrepararInterfaz.cs`, `PrepararInterfaz.Asegurar` por lotes.
- `Arranque/CapturaPruebas.cs`: paso «punto» como `Ficha()` (l.465), tolerando la falta de red.
- Pruebas: `Tests/EditMode/PruebasPuntoLibre.cs` (nueva) y las del detector en `PruebasFicha.cs` (~l.149-235).

## Tasks & Acceptance

**Execution:**
- [ ] `DetectorToque` + `CamaraMapa` + `IMapa`/`MapaVectorial` (+ falsos): pulsación larga y `Desproyectar`.
- [ ] `IGeocodificador.Inverso` en Photon.
- [ ] Ficha de punto libre, chincheta y `PantallaMapa`.
- [ ] Textos, Localization y captura «punto».
- [ ] `PruebasPuntoLibre.cs`: filas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `punto.png`, entonces se ven la chincheta y la ficha con «Punto en el mapa»; el registro no tiene errores.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: mantener pulsado, arrastrar sin querer, dos dedos, sin datos.
