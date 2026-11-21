---
title: 'Historia 2.6 (parte 2b): ruta aproximada sin conexión'
type: 'feature'
created: '2026-11-21'
status: 'done'
baseline_commit: '0baf3774f78246b524cfc0bad878439b1ff4bdd4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Sin conexión (fallan Valhalla y OSRM), Ir solo dice «No se ha podido calcular la ruta» (2.6b). El usuario no tiene ninguna ruta (FR18, UX-DR16).

**Approach:** Calcular en el móvil una ruta aproximada por las calles de las teselas guardadas (grafo de la capa `transportation` de las MVT en caché, sin sentidos únicos) y enseñarla como ruta aproximada: línea en un azul más suave, tarjeta de arriba «Ruta aproximada por las calles guardadas», valores con «≈» y un toast «Sin conexión: ruta aproximada».

**Decisiones del 20/11/2026:** opción A (por las calles guardadas) porque la recta atraviesa edificios; maqueta v2 aprobada.

**Decisiones del 21/11/2026** (maqueta aprobada: https://claude.ai/artifact/WmVDqyEMoSxAq4So3vge5J):
- **Sin calles que unan** origen y destino (o a más de 300 m de una calle): «No se ha podido calcular la ruta» y «—», como hoy, más el toast «Sin conexión: no hay calles guardadas para esta ruta».
- **Ficha sin conexión:** los minutos salen de la ruta aproximada, «≈ 4 min» en coche y a pie (TalkBack: «unos 4 minutos»).
- **Colores de la línea aproximada:** claro relleno `#8FA3E6`, borde `#5F78C9`; oscuro relleno `#6F86C9`, borde `#44599A`.
- El spec se deja entero (unos 2.400 tokens).

## Boundaries & Constraints

**Always:**
- **Cuándo:** solo si fallan el principal y la reserva (`ServicioRutas.Calcular`). La ruta aproximada no se guarda en la caché (al volver la red, la siguiente petición va a los servicios).
- **Grafo** (Dominio, puro, `double`): nodos por coordenada exacta de la tesela (enteros globales `x·4096 + px` al zoom 14: los vértices compartidos entre teselas vecinas coinciden), aristas por cada tramo. Un extremo suelto a ≤ 5 m de otro tramo se une a él (cruces que la generalización deja sin vértice). Camino más corto por metros (A*).
- **Calles por modo:** coche, todas salvo `path`, `track`, `raceway`, `busway`, `bus_guideway`, `ferry`, `rail`, `transit`, `aerialway`; a pie, todas salvo `motorway`, `trunk` y las de transporte.
- **Origen y destino:** al punto más cercano de un tramo a ≤ 300 m; si no hay, no hay ruta.
- **Teselas:** las guardadas en disco (`persistentDataPath/vectorial/14/x/y.pbf`) del rectángulo origen–destino más una tesela de margen, como mucho 5×5. Lectura y cálculo fuera del hilo principal; cancelable.
- **Tiempo:** metros ÷ velocidad (coche 30 km/h, a pie 5 km/h); sin maniobras.
- **Ruta** (`Dominio/Ruta`): marca `Aproximada`.
- **Línea:** la de la 2.6b (ancho, borde, colas, zoom) con los colores suaves de la ruta aproximada, en claro y oscuro.
- **Tarjetas:** arriba «Ruta aproximada por las calles guardadas»; abajo «≈ 13:34», «≈ 4» y «≈ 1,2». TalkBack: «Ruta aproximada. Llegada hacia las 13:34, unos 4 minutos, unos 1,2 km».
- **Toast** (DESIGN.md: fondo `text`, texto `bg`, 12 px de radio, centrado, debajo de la tarjeta de arriba): «Sin conexión: ruta aproximada», 3 s, sin animación, anunciado por TalkBack. Reutilizable (la 3.3 lo usa).
- Velocidades, 5 m, 300 m y 5×5 en `ParametrosNegocio`; textos en Localization; colores en tokens y `PaletaMapa`, con contraste comprobado.

**Never:** sentidos únicos o giros prohibidos; descargar teselas para la ruta; reintentar solo cuando vuelve la red, recalcular o navegar (2.7); recta del origen al destino.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Sin conexión | Los dos caen, calles guardadas | Línea suave por las calles; arriba «Ruta aproximada…»; abajo con «≈»; toast 3 s | N/A |
| Con conexión | Valhalla responde | Como la 2.6b, sin toast | N/A |
| Cruce sin vértice | Extremo a 3 m de otra calle | El camino pasa por ese cruce | N/A |
| A pie / coche | Tramo `path` | A pie lo usa; en coche no | N/A |
| Lejos de una calle | Destino a 400 m de cualquier tramo | «No se ha podido calcular la ruta», «—» | Toast «Sin conexión: no hay calles guardadas para esta ruta» |
| Sin unir | Teselas sin camino entre los dos | Igual que la fila anterior | Mismo toast |
| Ficha sin red | Abrir la ficha, los dos caen | «≈ 4 min» en coche y a pie | Sin camino: «—» |
| Vuelve la red | Ir otra vez | Ruta de Valhalla (no estaba en la caché) | N/A |
| Tema | Oscuro, ruta aproximada | Colores suaves del oscuro | N/A |
| Cancelar | Quitar ruta mientras calcula | No se dibuja nada | Cancelación |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts`.
- `Dominio/Ruta.cs`: añadir `Aproximada` (constructor con valor por defecto `false`).
- Nuevo `Dominio/GrafoCalles.cs` (nodos, aristas, unión de extremos sueltos, proyección al tramo más cercano, A*) y `Dominio/ParametrosNegocio.cs` (velocidades, 5 m, 300 m, 5×5; junto a las de `Ruta*`, l.19-37).
- `Aplicacion/ServicioRutas.cs`: `Calcular` (l.~118) pide a un tercer `IRutas local` (parámetro opcional del constructor) si fallan los dos; `Guardar` no se llama con `Aproximada`. `PedirTiempos` usa `Calcular`, así que la ficha recibe la aproximada sin más cambios en el servicio.
- Nuevo `Vectorial/RutasCallesGuardadas.cs : IRutas`: lee los `.pbf` con `LectorMVT.Leer` (capa `transportation`, `e.Texto("class")`, `e.partes` en coordenadas de tesela, `extension` 4096), pasa a lat/lon con la fórmula de teselas (no `Geo.ALocal`, que es local a Unity) y construye el `GrafoCalles`. Guardar `Application.persistentDataPath` en el constructor (no se lee fuera del hilo principal). Clases de calle: `MapaVectorial.cs` l.573-588.
- `Arranque/Arranque.cs` l.61-62: pasar `new RutasCallesGuardadas(...)` a la rama real; la captura usa uno falso.
- `Aplicacion/IMapa.cs` l.85: `MostrarRuta(puntos, bool aproximada)`; los 15 `MapaFalso` de `Tests/EditMode` (lista en `spec-2-6b`, Code Map).
- `Vectorial/MapaVectorial.cs`: `MostrarRuta` (l.308), `ColorearRuta`, `PaletaMapa` (colores suaves por tema).
- `UI/TarjetaManiobra.cs` (texto de ruta aproximada), `UI/TarjetaRuta.cs` (`MostrarRuta` l.103: «≈» y anuncio), `UI/FilaAcciones.cs` (`PonerMinutos` l.90 y el anuncio l.102-105: «≈» y «unos N minutos» con `Aproximada`; quien la llama en `PantallaMapa` pasa la marca).
- Nuevo `UI/Toast.cs`; `UI/PantallaMapa.cs` `DibujarRuta` (l.~1527: toast si `Aproximada`, `MostrarRuta(…, aproximada)`), `FalloRuta` (l.1543). Estilos en `Assets/UI/PantallaMapa.uss` y `Estilos/Tokens.uss`; claves en `UI/ClavesTexto.cs` + `Editor/PrepararInterfaz.TextosEs` + tabla es-ES.
- `Arranque/CapturaPruebas.cs`: captura «ruta-aproximada» (claro) y «ruta-aproximada-oscuro» con rutas falsas que fallan y teselas de Sanlúcar ya en caché.
- Pruebas: nuevo `Tests/EditMode/PruebasRutaAproximada.cs` (grafo sintético y teselas MVT de prueba; servicio; pantalla), y pares nuevos en `PruebasContraste`.

## Tasks & Acceptance

**Execution:**
- [x] `Dominio/GrafoCalles.cs`, `Ruta.Aproximada`, `ParametrosNegocio` -- grafo y A* con pruebas de la matriz (cruce sin vértice, modos, lejos, sin unir).
- [x] `Vectorial/RutasCallesGuardadas.cs` -- lectura de teselas guardadas, conversión y cancelación, con pruebas sobre un `.pbf` de prueba.
- [x] `Aplicacion/ServicioRutas.cs` + `Arranque` -- tercer servicio, sin caché para la aproximada.
- [x] `IMapa` + falsos + `MapaVectorial` -- estilo suave por tema.
- [x] `UI/Toast.cs`, tarjetas, `PantallaMapa`, textos y tokens -- con TalkBack y pruebas de la matriz.
- [x] Capturas «ruta-aproximada» y «ruta-aproximada-oscuro»; memoria del TFG; CLAUDE.md.

**Acceptance Criteria:**
- Given la suite EditMode, when se ejecuta sin red, then pasan todas.
- Given las capturas «ruta-aproximada», when se abren, then la línea suave sigue las calles, las tarjetas llevan «≈» y el toast se ve bajo la tarjeta de arriba.
- Given TalkBack forzado, when llega la ruta aproximada, then se anuncia el toast y el foco va a la tarjeta de arriba.

## Implementation Notes

- La captura usa el `RutasCallesGuardadas` real (no uno falso) para que la línea siga las calles de las teselas ya en disco; las rutas falsas fallan solo durante «ruta-aproximada» y `ServicioRutas.VaciarCache()` evita que salga la ruta guardada de la captura «ruta».
- Contraste de la línea aproximada: con los colores aprobados, en claro el relleno `#8FA3E6` no llega a 3:1 sobre las calles blancas (≈ 2,5:1) y lo marca el borde `#5F78C9` (≥ 3,5:1); en oscuro el borde no llega y lo marca el relleno (≥ 3,4:1). La prueba pide ≥ 3:1 a uno de los dos.
- Un fallo de `Calcular` con servicio local quiere decir que tampoco hubo calles (`ServicioRutas.ConRutaAproximada`): ahí la pantalla saca el toast «no hay calles guardadas». TalkBack de 1 minuto: «alrededor de 1 minuto».

## Spec Change Log

## Review Triage Log

Revisión del 21/11/2026 (blind, edge-case, verification-gap; diff desde `0baf377`).

| Hallazgo | Veredicto | Ruta | Evidencia |
|---|---|---|---|
| Toast sobre la ficha reabierta: se enseña y se anuncia dentro de la modal y queda colocado contra una tarjeta oculta; el ya visible sigue encima al reabrir (blind + edge + gap) | medium | patch | `DibujarRuta`/`FalloRuta` llaman a `MostrarToast` fuera de `if (TarjetasRutaVisibles)`; `ReabrirFichaRuta` no lo oculta |
| El lector usa zoom 14 fijo y el mapa guarda en `MapaVectorial.zoom` (Range 12-14) (edge + gap) | medium | patch | `RutasCallesGuardadas.Zoom = 14`; `CargarTesela` escribe en `vectorial/{zoom}`; ninguna prueba los ata |
| `Admite` deja pasar las clases `*_construction` (blind + edge) | low | patch | no están en `SinCoche`/`SinPie`/`Transporte`; arreglo de una línea |
| `UnirExtremosSueltos` no mira la cancelación; la prueba de cancelar no llega al bucle ni a A* (gap + edge + afirmación) | low | patch | `Teselas_Cancelar`/`Grafo_Cancelar` cancelan antes de empezar; pasar `ct` es directo |
| Sin prueba de la posición del toast bajo la tarjeta (gap) | medium | patch | ninguna prueba lee `style.top`; solo la captura |
| Textos de 1 minuto de TalkBack sin comprobar (blind) | low | patch | solo `LosTextosEstanEnLaTabla`; añadir la aserción es directo |
| El toast no se oculta solo a los 3 s sin prueba (gap + blind) | low | rechazado | el planificador de UI Toolkit no corre sin panel en EditMode; haría falta PlayMode o un reloj inyectado |
| El Arranque podría perder el servicio local sin que falle ninguna prueba (gap) | medium | defer | el servicio se crea en `Start`, fuera de `Componer`; mismo hueco que Valhalla/OSRM (2.6) |
| Hasta 16–24 s de «Calculando» sin red si la red se cuelga en vez de fallar (blind + edge) | medium | defer | `ConLimite` 8 s para principal, reserva y local en serie; arreglarlo pide detectar la red |
| El peor caso (5×5 teselas) en un móvil lento puede pasar de los 8 s (blind + edge) | maybe-false | defer | solo medido en PC (563 y 371 m); se resuelve midiendo en el móvil |
| «Sin conexión» cuando hay red pero caen Valhalla y OSRM (blind + edge) | low | rechazado | el spec (frozen) iguala «fallan los dos» con sin conexión; raro y el arreglo añade una comprobación de red |
| Toast «no hay calles guardadas» también por tiempo agotado o > 5 teselas (blind + edge) | low | rechazado | sin red y tan lejos el mensaje sigue siendo cierto en lo práctico; distinguir la causa añade superficie |
| Proyección al tramo aislado más cercano en vez de uno conectado (blind + edge) | low | rechazado | los tramos aislados vienen solo del recorte (los cose la unión a 5 m); probar N candidatos añade complejidad |
| La distancia del punto real a la calle no cuenta (blind) | false | rechazado | la épica fija que la ruta empieza y acaba en el punto de calle más cercano, como Valhalla |
| Se leen las mismas teselas para coche, a pie e Ir (blind) | low | rechazado | eficiencia; una caché de teselas leídas añade estado |
| Extremo suelto unido a su propia calle o a un puente (blind) | low | rechazado | raro y sin daño visible (atajo de < 5 m); filtrar `brunnel` añade ramas |
| Tesela leída mientras el mapa la escribe (blind) | low | rechazado | la lectura ilegible se captura y se salta; carrera muy improbable |
| Origen y destino en el mismo nodo: ruta de 0 m (edge) | low | rechazado | destino a pocos metros: «≈ 1 min» es correcto en la práctica |
| Origen y destino a 5 teselas sin margen (edge) | low | rechazado | lo fija el spec (5×5 como mucho) |
| Code Map dice «captura con uno falso» y el nombre «2b» frente a «2.6c» (blind) | low | rechazado | el arreglo es editar el spec; la nota de implementación ya explica el cambio |

## Design Notes

Uniones entre teselas: el mismo vértice OSM tiene la misma coordenada entera global en las dos teselas (misma rejilla de 4096 por tesela), así que basta con la clave entera; los extremos que el recorte crea en el margen de la tesela quedan sueltos y la unión a ≤ 5 m los cose con la vecina.

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- Build de PC y capturas «ruta-aproximada» y «ruta-aproximada-oscuro»; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil, en modo avión tras haber visto la zona: ruta a pie y en coche por Sanlúcar.
