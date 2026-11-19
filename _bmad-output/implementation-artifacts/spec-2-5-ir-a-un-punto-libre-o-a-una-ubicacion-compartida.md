---
title: 'Historia 2.5 (parte 1): mantener pulsado el mapa abre un punto libre'
type: 'feature'
created: '2026-11-19'
status: 'done'
baseline_commit: '26af338a60bc8d677bd5def850c5f204ff5becd5'
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
- [x] `DetectorToque` + `CamaraMapa` + `IMapa`/`MapaVectorial` (+ falsos): pulsación larga y `Desproyectar`.
- [x] `IGeocodificador.Inverso` en Photon.
- [x] Ficha de punto libre, chincheta y `PantallaMapa`.
- [x] Textos, Localization y captura «punto».
- [x] `PruebasPuntoLibre.cs`: filas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `punto.png`, entonces se ven la chincheta y la ficha con «Punto en el mapa»; el registro no tiene errores.

## Implementation Notes

- `DetectorToque.Avanzar(segundos, dpi)` cuenta el tiempo por fotograma (`Time.unscaledDeltaTime`) desde el fotograma siguiente al de bajar el dedo; `ArrastreBloqueado` para el arrastre de un dedo (y del botón izquierdo) tras dispararse; un segundo dedo lo libera.
- `Direccion` gana `Calle` y `Numero` (opcionales); `LecturaPhoton` toma la calle de `street` o, si el rasgo es una calle (`osm_key=highway`), de `name`. Sin calle → coordenadas. El `Nombre` del buscador no cambia.
- `GeocodificadorPhoton`: `Buscar` e `Inverso` comparten la petición (`Pedir`); URL inversa con 6 decimales.
- TalkBack: el nodo del título lee también la localidad cuando llega («Calle Ancha, 10, Sanlúcar de Barrameda»), para no perderla sin añadir un cuarto nodo; se refresca con `RefrescarEtiqueta`.
- En el punto, la muestra de la plantilla se oculta y en su sitio va la gotita de la maqueta (`FichaLugar.GotaPunto`, 12×14, misma gota y tokens que la chincheta, sin borde); con un lugar, al revés.
- Tras la pulsación larga, `CamaraMapa` marca el gesto como empezado sobre la interfaz: ni un segundo dedo ni el ratón mueven el mapa hasta soltar.
- Chincheta dibujada con Painter2D (gota 34×44 de la maqueta); colores en `Tokens.uss` (`--color-chincheta*`), grosor del borde en `--chincheta-borde` (3 claro, 2 oscuro).
- Textos nuevos: `punto.tipo`, `punto.coordenadas`, `punto.callenumero`, `punto.buscando`, `punto.anuncio`.

## Spec Change Log

## Review Triage Log

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | Falta la gotita junto a «Punto en el mapa» de la maqueta aprobada | low | La captura `punto.png` enseña el subtítulo sin icono; la maqueta lo tiene | patch |
| 2 | Tras la pulsación larga, un segundo dedo desbloquea el arrastre y mueve el mapa bajo la ficha | low | `Anular` pone `anulado` y `ArrastreBloqueado` pasa a false; `gestoSobreInterfaz` solo se mira en `Began` con un dedo (`CamaraMapa.cs:67-69`) | patch |
| 3 | `UrlInversa` sin `lang=default`, a diferencia de `Buscar` y de su propio comentario | low | La URL solo lleva lat, lon y limit | patch |
| 4 | Los `CancellationTokenSource` del punto no se liberan | low | `CancelarDireccionPunto` y `PedirDireccionPunto` los sueltan sin `Dispose` | patch |
| 5 | Ninguna prueba comprueba que el Arranque pasa el geocodificador a la pantalla | low | Solo `PruebasPuntoLibre` lo inyecta en `Inicializar`; quitarlo de `Componer` no rompe nada | patch |
| 6 | `ActualizarCapas` con la chincheta no se prueba (sigue al mapa y se oculta al cerrar) | low | Las pruebas copian la línea en `Recolocar()` | patch |
| 7 | Pruebas engañosas: `ConArrastre…` comprueba la ficha con un detector suelto; `ConElMenuOUnaVentana…` no prueba la ventana de ubicación | low | El `Assert.IsFalse(FichaAbierta)` no puede fallar; falta el caso `ModalUbicacionAbierta` | patch |
| 8 | Photon `/reverse` puede dar una calle lejana (río, pinar, mar) | maybe-false (medium si es cierto) | No hay radio ni comprobación de distancia; hay que medir en Sanlúcar | defer |
| 9 | Sin vibración al dispararse la pulsación larga | low | No lo pide el spec; en Android es lo habitual | defer |
| 10 | Entrada real de `CamaraMapa` (dedos y ratón) sin prueba automática | low | `Input` estático no se puede falsear en EditMode | defer (prueba en el móvil) |
| 11 | Recentrado con la medida real de la hoja sin prueba | false | La captura `punto` lo comprueba: desvío 0,0 px | rechazado |
| 12 | Título de coordenadas ambiguo («36,7781, -6,3515») | false | Es el formato que fija la intención | rechazado |
| 13 | TalkBack no puede crear un punto libre | low | Un gesto de mantener pulsado no existe con TalkBack; la parte 2 (pegar coordenadas) da la alternativa | rechazado |
| 14 | Sin calle se pierde el nombre del sitio y la localidad | low | La intención fija coordenadas sin calle | rechazado |
| 15 | Sin mensaje cuando falla Photon | low | La intención fija que se queden las coordenadas | rechazado |
| 16 | Texto de error genérico cambiado sin prueba | low | Ni la UI ni las pruebas lo usan | rechazado |
| 17 | Pulsaciones repetidas lanzan varias peticiones | low | Cada una cancela la anterior; uso normal de una persona | rechazado |
| 18 | `RecentrarFichaPendiente` compara por coordenada | low | Solo coincide si un punto cae exactamente en la coordenada de un lugar | rechazado |
| 19 | Un tirón de más de 500 ms convierte un toque en pulsación larga | false | `Avanzar` solo corre con el dedo aún abajo (`Moved`/`Stationary`): el dedo ha estado ese tiempo | rechazado |
| 20 | Pulsar junto al horizonte da un punto muy lejano | low | Es el suelo que hay bajo el dedo; caso raro | rechazado |
| 21 | Ficheros del diff ausentes (assets, CLAUDE.md) | false | Excluidos a propósito del diff de revisión; están modificados en el árbol | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: mantener pulsado, arrastrar sin querer, dos dedos, sin datos.
