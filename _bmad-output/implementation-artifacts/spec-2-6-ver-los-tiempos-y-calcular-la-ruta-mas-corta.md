---
title: 'Historia 2.6 (parte 1): rutas y tiempos en la ficha'
type: 'feature'
created: '2026-11-20'
status: 'done'
baseline_commit: '6ab1279767d21109c6bd8fd496f52caf42a7f377'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La fila de acciones de la ficha está vacía (1.8) y la app no sabe calcular rutas (FR7, FR10, FR18).

**Approach:** Un puerto de rutas con Valhalla (FOSSGIS, `shortest`) y OSRM de reserva. La fila de acciones de la ficha (lugar y punto libre) muestra coche (por defecto) y a pie con sus minutos y el botón Ir. Dibujar la ruta es la parte 2.

**Decisiones del 20/11/2026:**
- La 2.6 se parte en dos. Parte 1 (esta): servicio de rutas y fila de la ficha. Parte 2: Ir dibuja la ruta (accent de 6 dp con borde), la encuadra, y sin conexión enseña una recta semitransparente con el aviso «Sin conexión: ruta aproximada».
- Sin posición conocida, los dos modos enseñan «—» e Ir sigue activo: hace lo mismo que el botón de ubicación (explicación del permiso o aviso de ubicación).
- La parte 1 se deja entera (unos 2.000 tokens): partirla dejaría una parte sin nada visible.
- OSRM de reserva en `routing.openstreetmap.de` (`routed-car`, `routed-foot`): el servidor de demostración de OSRM solo tiene coche.

## Boundaries & Constraints

**Always:**
- **Modelo** (Dominio, sin Unity): `ModoViaje` (Coche, APie); `Ruta` (puntos `Coordenada`, metros, segundos, `Maniobra`: tipo, texto, coordenada, metros); `Polilinea.Decodificar(cadena, precision)` (5 y 6).
- **Valhalla:** `POST https://valhalla1.openstreetmap.de/route`, `costing` `auto` o `pedestrian` con `costing_options.<c>.shortest: true`, `language: es-ES`, `directions_type: maneuvers`; polilínea de precisión 6; maniobras con su texto.
- **OSRM:** `GET https://routing.openstreetmap.de/routed-{car|foot}/route/v1/{driving|foot}/lon,lat;lon,lat?alternatives=3&steps=true&overview=full&geometries=polyline6`; se queda la alternativa de menor distancia. Maniobras sin texto (tipo y modificador; los textos, en la 2.7).
- **Reserva:** si Valhalla falla (error, HTTP o 8 s), OSRM; si también falla, el modo queda sin tiempo.
- **Privacidad:** el origen va con 5 decimales (≈1 m), el destino con 6. Nada se guarda; caché solo en memoria.
- **Tiempos:** al abrir la ficha con posición `Activa` (o al pasar a `Activa` con ella abierta), se piden coche y a pie en paralelo. Mientras: «…»; luego «6 min» (minutos redondeados hacia arriba, «1 min» como mínimo); fallo: «—». Caché por (origen redondeado a 20 m, destino, modo) durante 2 min. Cerrar la ficha cancela las peticiones; una respuesta tardía no pinta nada.
- **Fila** (DESIGN.md y maqueta `key-ficha-lugar`): dos botones de modo (icono vectorial + nombre, minutos debajo; 48 dp de alto, borde de control; el elegido con borde y texto accent) y Ir (`button-primary`, 52 dp, `flex 1.2`). Coche elegido al abrir. Siempre a la vista, sin desplazar.
- **Ir con posición:** en esta parte solo deja el modo y la ruta elegidos listos para la parte 2 (sin dibujo).
- **TalkBack:** «Coche, 6 minutos, seleccionado», «A pie, 15 minutos», «Ir a Clínica Areas» (punto libre: «Ir a este punto»). Sin tiempo: «Coche, sin tiempo, seleccionado».
- Parámetros (8 s, 20 m, 2 min) en `ParametrosNegocio`. Textos en Localization.

**Never:** dibujar la ruta, encuadrar o la ruta aproximada (parte 2); navegación giro a giro y textos de maniobras de OSRM (2.7); claves de API de pago; API de Google.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Abrir ficha con posición | `Activa`, Valhalla responde | «…» y luego coche y a pie con minutos; coche elegido |
| Punto libre | Ficha de punto libre con posición | La misma fila; Ir dice «Ir a este punto» |
| Sin posición | `Buscando`/`SinPermiso`/`Apagada` | «—» en los dos; Ir hace lo del botón de ubicación |
| Llega la posición | Pasa a `Activa` con la ficha abierta | Se piden y salen los minutos |
| Valhalla cae | Fallo o 8 s | OSRM, la alternativa más corta |
| Los dos caen | Sin red | «—» en ese modo |
| Cambiar a pie | Toque en «A pie» | A pie elegido; TalkBack lo dice |
| Caché | Reabrir la ficha antes de 2 min sin moverse 20 m | Sin peticiones nuevas |
| Cerrar pronto | Cerrar la ficha antes de la respuesta | Peticiones canceladas; nada se pinta |
| Polilínea | Cadena polyline6 conocida | Coordenadas esperadas |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts` salvo otra indicación.
- Dominio nuevo: `ModoViaje`, `Ruta`, `Maniobra`, `TipoManiobra`, `Polilinea`; parámetros en `ParametrosNegocio.cs` (hoy solo frescura).
- Aplicación nueva: `IRutas` (`Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje, CancellationToken)`) y `ServicioRutas` (tiempos de los dos modos, caché, reserva con dos `IRutas`). Ojo: en `ServicioUbicacion` (l.77-89) `RutaActiva`/`IniciarRuta` son el GPS en segundo plano de la 2.1; no reutilizar esos nombres.
- Adaptador nuevo `Adaptadores/Rutas/` con su asmdef (Aplicacion, Dominio) y `AssemblyInfo` con `InternalsVisibleTo` de pruebas, como `Adaptadores/Busqueda`: `RutasValhalla`, `RutasOsrm` (petición como `GeocodificadorPhoton.Pedir`, `AgenteUsuario`; POST para Valhalla) y lectores puros `LecturaValhalla`, `LecturaOsrm` (como `LecturaPhoton`), probados con JSON en línea.
- `Arranque/Arranque.cs`: crea los dos adaptadores y `ServicioRutas`, y lo pasa por `Componer` → `PantallaMapa.Inicializar` (opcional, como `geocodificador`).
- `UI/FichaLugar.cs`: `FilaAcciones` ya existe vacía (l.42, l.111); se rellena con el nuevo `UI/FilaAcciones.cs`. `Abrir` (l.134) y `AbrirPunto` (l.209). Estilos en `Assets/UI/PantallaMapa.uss` (`.ficha__acciones` l.762, ya `flex-shrink: 0`); tokens en `Estilos/Tokens.uss`. Iconos coche/a pie dibujados como los de `BotonRedondo` (sin emojis).
- `UI/PantallaMapa.cs`: pide los tiempos en `AbrirFicha` (l.1169) y `AbrirPunto` (l.1235) y al pasar la posición a `Activa` (lee `ubicacion.Posicion` cada fotograma, l.1775); cancela en `CerrarFicha` (l.1308). Ir sin posición → `TocarUbicacion` (l.256). TalkBack: botones en `DeclararFichaEnTalkBack` (l.1317-1330) con `accesibilidad.Boton(..., grupo: ficha)`.
- `Arranque/CapturaPruebas.cs`: la captura `ficha` (l.466-507) ya abre Clínica Areas; con un `IRutas` falso, la fila sale con minutos.
- Pruebas: `Tests/EditMode/PruebasRutas.cs` (polilínea, lectores, URL y cuerpo, servicio con `IRutas` falsos) y fila en la pantalla real (como `PruebasFicha.cs`).

## Tasks & Acceptance

**Execution:**
- [x] Modelo, `Polilinea` y parámetros, con pruebas.
- [x] `IRutas`, adaptadores Valhalla y OSRM con sus lectores, y pruebas con JSON grabado.
- [x] `ServicioRutas`: dos modos, reserva, caché y cancelación, con pruebas.
- [x] `FilaAcciones` en la ficha, textos y TalkBack; Arranque; pruebas de la matriz.
- [x] Captura `ficha` con la fila; memoria del TFG.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta sin red, entonces pasan todas.
- Dada la captura `ficha`, cuando se abre, entonces la fila enseña coche (elegido) y a pie con minutos e Ir, sin tapar la cabecera ni los conceptos.

## Implementation Notes

- **Caché:** en vez de redondear el origen a una rejilla de 20 m, una ruta guardada vale si el origen nuevo está a ≤ 20 m del suyo (mismo destino y modo, < 2 min). Así cumple «sin moverse 20 m» también junto al borde de una celda. Solo se guardan las rutas buenas.
- **8 s:** el límite lo pone `ServicioRutas` (espera inyectada, `Task.Delay` en la app) sobre cada servicio, también sobre OSRM; los adaptadores llevan además el `timeout` de 10 s de `UnityWebRequest`.
- **Sin servicio de rutas** (parámetro opcional de `Componer`/`Inicializar`, como `geocodificador`), la fila existe pero oculta y fuera de TalkBack; por eso las pruebas antiguas de la ficha no cambian su recorrido. `PruebasFicha.LaFilaDeAccionesVaVaciaBajoLaCabecera` se adaptó: la fila tiene ahora un hijo (oculto).
- **TalkBack:** la etiqueta es «Coche, 6 minutos» y el «seleccionado» lo pone el estado `Selected` del nodo (como el menú «Precios»). `AccesibilidadPantalla.RefrescarEtiqueta` relee también ese estado, para cambiar de modo sin rehacer la jerarquía. Al cambiar de modo se anuncia «A pie, 15 minutos, seleccionado». Mientras se calcula: «Coche, calculando el tiempo».
- **Ir con posición** guarda en `ServicioRutas` `ModoElegido`, `DestinoElegido` y `RutaElegida` (si aún no ha llegado, se pone al llegar); `Cancelar` (cerrar la ficha o abrir otra) lo olvida. La parte 2 la dibuja.
- **Textos:** 13 claves nuevas en `ClavesTexto`, `PrepararInterfaz.TextosEs` y las tablas de Localization (añadidas a mano a los `.asset`).
- **Captura:** `CapturaPruebas.RutasFalsas()` (4 min en coche, 12 a pie) y `ComprobarAcciones` en la captura `ficha` (modos ≥ 48 dp, Ir ≥ 52 dp, minutos, coche elegido, sin tapar cabecera ni conceptos).
## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | `ComprobarAcciones` entre el resumen de `Ficha()` y el método (blind, edge, verification-gap) | low | Dos `<summary>` en un método y ninguno en el otro | patch |
| 2 | Lo elegido con Ir sobrevive a cerrar la ficha (blind, edge) | medium | `Cancelar` no tocaba `ModoElegido`/`DestinoElegido`/`RutaElegida` | patch + pruebas |
| 3 | Ir antes de que llegue la ruta deja `RutaElegida` en null para siempre (blind, edge) | medium | `Tiempo` solo rellenaba `ultimas` | patch + prueba |
| 4 | Modos e Ir del punto libre en TalkBack sin prueba (verification-gap) | medium | La rama final de `DeclararFichaEnTalkBack` no la recorría ninguna prueba | patch: prueba del recorrido |
| 5 | Error de `AlCalcularTiempo` perdido en una tarea sin esperar (blind, edge) | low | Sin try/catch | patch: `Debug.LogException` |
| 6 | `RefrescarEtiqueta` borra otras banderas del nodo (blind, edge) | low | Asignaba `state` entero | patch: solo el bit `Selected` |
| 7 | El comentario y la memoria dicen «sin mandar la posición exacta» con 5 decimales (blind) | low | 5 decimales ≈ 1 m | patch: redactado tal cual |
| 8 | Los minutos no se refrescan al moverse con la ficha abierta, ni tras «—» al volver la red (blind, edge) | low | El spec pide al abrir o al pasar a `Activa`; Ir de la parte 2 debe recalcular | defer (parte 2) |
| 9 | El montaje de Valhalla + OSRM del Arranque no tiene prueba (verification-gap) | maybe-false (medium si es cierto) | Igual que Photon y el resolutor; pide una costura de fábrica | defer |
| 10 | Reserva hasta 16 s (blind) | false | El spec fija 8 s por servicio | rechazado |
| 11 | Ir no da respuesta en esta parte (blind) | false | Lo fija el spec (parte 1 sin dibujo) | rechazado |
| 12 | El 400 de Valhalla («sin camino») pierde el cuerpo y pasa a OSRM (blind) | low | Pide OSRM de más y el registro es genérico | rechazado |
| 13 | Política de uso de FOSSGIS y agente sin contacto (blind) | low | Agente identifica la app; caché de 2 min | rechazado |
| 14 | Foco tapa el borde del modo elegido; Ir sin `:active` (blind) | low | Cosmético | rechazado |
| 15 | TalkBack podría leer dos veces el cambio de modo (blind) | low | Depende de Android; se ve en el móvil | rechazado |
| 16 | `LecturaValhalla` usa `PeticionRutas.Punto` (blind) | low | Sin daño concreto | rechazado |
| 17 | Faltan pruebas de lectura raras (varios tramos, millas, tarea nula) y de contexto en cola (blind) | low | Rutas de dos puntos: un tramo | rechazado |
| 18 | Ruta descartada si se cancela justo al llegar (blind) | low | Solo cuesta una petición | rechazado |
| 19 | `esperar` nulo, OSRM sin distancia, forma vacía, desbordes de la polilínea (edge) | low | Entradas que los servidores no dan | rechazado |
| 20 | Ficha abierta sin destino pide cada fotograma (edge) | false | `FichaLugar.Posicion` nunca es null con la ficha abierta | rechazado |
| 21 | Ir sin servicio de ubicación no hace nada (edge) | low | En la app siempre hay servicio | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura `ficha`; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil con datos: minutos reales en coche y a pie desde Sanlúcar.
