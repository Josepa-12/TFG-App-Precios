---
title: 'Historia 2.6 (parte 2a): Ir dibuja la ruta'
type: 'feature'
created: '2026-11-20'
status: 'done'
baseline_commit: '15b4b0d17352d48611f4bdc13711e598b85ab8f3'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Ir no hace nada visible (2.6, parte 1): el usuario no ve por dónde ir (FR7, FR18, UX-DR8).

**Approach:** Ir cierra la ficha, dibuja la ruta del modo elegido cubriendo la calle, la encuadra y enseña dos tarjetas: arriba la maniobra y la siguiente, abajo la llegada, los minutos y la distancia.

**Decisiones del 20/11/2026** (maqueta v2 aprobada: https://claude.ai/artifact/KnJ3R4DVbfBPPiQSVvwBTM, inspirada en una captura de navegación que pasó el usuario):
- **Línea:** cubre la calle entera, por su centro: unos 9 m de ancho (una calle de Sanlúcar con su borde), nunca menos de 6 dp en pantalla; relleno accent y borde accent más oscuro (claro `#1E3FB0`, oscuro `#2F5BEA`), uniones y extremos redondos.
- **Tarjeta de acciones arriba:** primera maniobra en grande y la siguiente debajo, con su flecha (textos de Valhalla en castellano). Fondo `text` al 96 % con texto blanco en claro; en oscuro, fondo claro (`#E6ECFA`) y texto `bg`. Si la ruta viene de OSRM (sin textos hasta la 2.7): «Ruta a Clínica Areas».
- **Tarjeta de ruta abajo:** «Llegada 13:34», «4 min», «1,2 km» (o «950 m») en tres columnas y la X «Quitar ruta» (48 dp). Tocarla reabre la ficha.
- Mientras hay ruta se ocultan la píldora de precios, los botones de la columna (tema) y la cápsula del buscador; queda el botón de ubicación abajo a la izquierda, encima de la tarjeta.
- La 2.6 parte 2 se parte: **2a** (esta) con conexión; **2b** sin conexión (ruta aproximada por las calles guardadas, opción A, y toast). La 2a se deja entera (unos 2.100 tokens).

## Boundaries & Constraints

**Always:**
- **Ir con posición:** pide la ruta del modo elegido desde la posición **actual** (`ServicioRutas.Calcular`; la caché vale si no se ha movido 20 m). Mientras llega: arriba «Calculando la ruta…», abajo «…», sin línea. Al llegar: línea, encuadre y tarjetas.
- **Si falla** (en esta parte): arriba «No se ha podido calcular la ruta», abajo «—», sin línea. La 2b lo cambiará por la ruta aproximada.
- **Línea:** por encima de calles y por debajo de edificios y marcadores; se rehace cuando el ancho mínimo en pantalla lo pide al hacer zoom; sigue al tema.
- **Encuadre:** la ruta entera entre la tarjeta de arriba y la de abajo (16 dp de margen) y 32 dp a los lados, sin pasar los límites de la cámara. Al instante.
- **Llegada:** hora local de ahora + la duración, «HH:mm».
- **Quitar ruta** (X o atrás): fuera línea y tarjetas; vuelven la píldora, los botones y la cápsula.
- **Reabrir:** con la ficha abierta (desde la tarjeta) se ocultan las tarjetas y la línea sigue; cerrarla (X, atrás, mapa) vuelve a las tarjetas. Otro modo en la ficha e Ir: ruta nueva de ese modo.
- **TalkBack:** tarjeta de arriba como texto («Sal hacia el norte por Calle Ancha. Después, gira a la derecha en Calle Santo Domingo»); la de abajo como botón «Llegada 13:34, 4 minutos, 1,2 km» que abre la ficha, y «Quitar ruta». Al dibujarse la ruta, el foco va a la tarjeta de arriba.
- Medidas (9 m, 6 dp, márgenes) en `ParametrosNegocio` o tokens; textos en Localization.

**Never:** sin conexión / ruta aproximada / toast (2b); navegación, recortar lo recorrido, recalcular o cambiar de maniobra al andar (2.7); cosméticos de ruta (Épica 5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Ir con ruta | Coche, Valhalla responde | Ficha cerrada; línea; encuadre; arriba maniobra y siguiente; abajo «Llegada · 4 min · 1,2 km» |
| Ir antes de la ruta | La ruta llega después | «Calculando la ruta…» y «…»; al llegar, línea y tarjetas |
| Ruta de OSRM | Valhalla cae | Arriba «Ruta a Clínica Areas» |
| Falla | Los dos caen | «No se ha podido calcular la ruta», «—», sin línea |
| Quitar ruta | X o atrás | Sin línea ni tarjetas; vuelven píldora, botones y cápsula |
| Reabrir | Tocar la tarjeta de abajo | Ficha abierta, tarjetas ocultas, línea sigue; al cerrar, tarjetas |
| Otro modo | Ficha reabierta, «A pie», Ir | Ruta a pie con sus tarjetas |
| Se ha movido | > 20 m desde los minutos | La ruta sale de la posición actual |
| Zoom | Alejar mucho | La línea no baja de 6 dp |
| Tema | Oscuro con ruta | Línea, borde y tarjeta de arriba con los colores oscuros |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts` salvo otra indicación.
- `Aplicacion/IMapa.cs`: `MostrarRuta(IReadOnlyList<Coordenada>)`, `QuitarRuta()`, `Encuadrar(IReadOnlyList<Coordenada>, rect libre en px)`. 15 `MapaFalso : IMapa` en `Tests/EditMode/Pruebas*.cs` (Arranque, Buscador, Busqueda, Cerca, Compartir, Direcciones, Ficha, Filtro, Marcadores, PantallaMapa, PuntoLibre, PuntosInteres, Rutas, Ubicacion, UbicacionCompartida) ganan los miembros.
- `Vectorial/MapaVectorial.cs`: `Linea` (l.533) solo acepta `ElementoMVT`: sacar una versión con `IList<Vector3>` (puntos con `Geo.ALocal`); `Disco` (l.556) para uniones. `Pieza` (l.599); el objeto de ruta cuelga del `transform` (no de una tesela). Materiales en `CrearMateriales` (l.660): colas 1960 (borde) y 1970 (relleno), ZWrite 0, ZTest Always; colores en `AplicarTema` (l.692) y `PaletaMapa`. Ancho = max(9 m, 6 dp × metros por píxel en el centro, de `CamaraMapa.distancia`, FOV y `Screen.dpi`); rehacer si cambia más de un 10 %.
- `Vectorial/CamaraMapa.cs`: no hay encuadre: `Encuadrar` (pivote al centro de los puntos; distancia que mete las esquinas proyectadas en el rect, acotada a 60–4000; desplazamiento como `CentrarEn(lat,lon,objetivo)` l.242).
- `Dominio/Maniobra.cs` (texto de Valhalla) y `Dominio/Ruta.cs` (`Metros`, `Segundos`, `Minutos`).
- `UI/PantallaMapa.cs`: `Ir()` (parte 1) y `CerrarFicha` (l.1495; hoy cancela las rutas y olvida lo elegido: con ruta dibujada no debe quitarla). Píldora, columna de controles y cápsula ya se ocultan con otras ventanas (ver `PlegarBuscadorPorOtraVentana`, `buscador.Visible`). Atrás en `Atras`. TalkBack con `accesibilidad.Boton/Texto` y `PedirFoco`. Reloj inyectable (por defecto `DateTime.Now`) para la llegada.
- Nuevos `UI/TarjetaManiobra.cs` y `UI/TarjetaRuta.cs` (iconos dibujados como en `BotonRedondo`/`FilaAcciones`); estilos en `Assets/UI/PantallaMapa.uss` con tokens de `Estilos/Tokens.uss`.
- `Arranque/CapturaPruebas.cs`: capturas «ruta» (claro, coche) y «ruta-oscuro» (a pie) con `IRutas` falsos que den una ruta por calles de Sanlúcar y maniobras con texto.
- Pruebas: `Tests/EditMode/PruebasRutas.cs` (pantalla: cada fila de la matriz) y funciones puras para `Encuadrar` y el ancho en metros.

## Tasks & Acceptance

**Execution:**
- [x] `IMapa` + 15 falsos.
- [x] Línea en `MapaVectorial` (ancho, borde, tema, zoom) y `CamaraMapa.Encuadrar`, con pruebas de sus cálculos.
- [x] `TarjetaManiobra` y `TarjetaRuta` con textos y TalkBack.
- [x] `PantallaMapa`: Ir, calculando, fallo, quitar, reabrir, atrás, otro modo; ocultar y devolver controles; pruebas de la matriz.
- [x] Capturas «ruta» y «ruta-oscuro»; memoria del TFG.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta sin red, entonces pasan todas.
- Dadas las capturas «ruta» y «ruta-oscuro», cuando se abren, entonces la línea cubre las calles y cabe entera entre las dos tarjetas, como en la maqueta.

## Implementation Notes

- Valhalla se pide ahora con `directions_type: instructions`: con `maneuvers` (parte 1) no devuelve el texto de las maniobras (comprobado con una petición real el 20/11/2026) y la tarjeta diría siempre «Ruta a…». Sus textos van en forma de usted («Gire a la derecha hacia Calle Regina»).
- `ServicioRutas.ElegirParaIr` (parte 1) se ha borrado con sus pruebas: Ir pide la ruta con `Calcular` desde la posición actual (la caché de 20 m evita una petición nueva). Las pruebas de la parte 1 que suponían que Ir dejaba la ficha abierta se han sustituido.
- Las tarjetas van debajo del velo del menú «Precios» en el árbol (con ruta no hay píldora ni menú).

## Spec Change Log

## Review Triage Log

Revisión del 20/11/2026 (blind, edge-case, verification-gap; diff desde `3bd917b`, ya que `0617b8e`/`3bd917b` no son de esta parte).

| Hallazgo | Veredicto | Ruta | Evidencia |
|---|---|---|---|
| Compartir texto con ruta activa: la cápsula está oculta y `AbrirBuscadorConLector` sale | medium | patch | `AplicarCompartidoPendiente` no quita la ruta y `AbrirBuscadorConLector` exige `buscador.Visible` |
| Ruta a un punto libre: `CerrarFicha` quita la chincheta y el destino queda sin marcador | medium | patch | `CerrarFicha` llama a `chincheta?.Actualizar(mapa, null, 1f)` y `Ir` cierra la ficha |
| `PuntoLibre_IrYReabrir…` no comprueba que se ocultan las tarjetas | medium | patch | gap verificado: solo `Reabrir_LaFicha…` (por `AbrirFicha`) lo afirma |
| Colocación de ubicación/atribución con ruta y su vuelta, sin prueba (blind + gap) | medium | patch | gap verificado: ninguna prueba lee `pila-derecha.style` |
| `MapaVectorial.Update` (tema y zoom de la ruta) sin prueba | medium | patch | gap verificado: la prueba usa `ColorearRutaAhora` y `MostrarRuta` directos |
| `MapaVectorial.Encuadrar` (inversión de Y) sin prueba | medium | patch | gap verificado: solo se prueba `CamaraMapa.Encuadrar` |
| Contraste de los tokens nuevos y de la línea sin comprobar (NFR-4) | low | patch | `PruebasContraste` no los incluye; añadir es directo |
| `SetGlobalColor` de fachada y sombra unidos en una línea | low | patch | formato accidental en `AplicarTema` |
| `ElegirParaIr`/`*Elegido` muerto, con pruebas y CLAUDE.md que lo citan | low | patch | la pantalla ya no lo llama; borrarlo es directo |
| Encuadre al cambiar de tamaño una tarjeta deshace el paseo del usuario (blind + edge) | low | rechazado | los textos no cambian con la ruta dibujada; solo área segura u orientación, raro, y el arreglo añade guardas |
| `nombreRuta` puede ser el texto provisional del punto | low | rechazado | solo con OSRM e Ir durante la búsqueda inversa; raro |
| Llegada y maniobra no se actualizan | low | rechazado | navegación y avance de maniobra son de la 2.7 (Never) |
| Textos de Valhalla en usted y pruebas en tú | low | rechazado | los textos de prueba no cambian el comportamiento; se avisa al usuario |
| Materiales sin destruir / `Shader.Find` nulo | false | rechazado | el mapa vive toda la app; el mismo shader ya lo usan los demás materiales |
| Asa en la tarjeta sin arrastre | low | rechazado | viene de la maqueta aprobada |
| Sin reintento tras el fallo | false | rechazado | el comportamiento ante el fallo lo fija el spec (la 2b lo cambia) |
| «km» fijo, números mágicos, ruido en el asset de Localization | low | rechazado | cosméticos, no afectan al uso |
| Atribución elevada con `buscador == null` | false | rechazado | en la app siempre hay buscador |
| Zona de encuadre inválida en pantallas estrechas | false | rechazado | inalcanzable: ancho de pantalla ≫ 64 dp |
| Primer encuadre sin el borde seguro de arriba | low | rechazado | se rehace al medirse las tarjetas en el mismo fotograma de disposición |
| `HoraLlegada` con segundos infinitos | false | rechazado | Valhalla y OSRM dan segundos finitos |
| Excepción en `DibujarRuta` deja estado a medias | maybe-false | rechazado | no se ha visto que `MostrarRuta` lance; sería low |
| Línea por debajo de 6 dp lejos del centro o con < 10 % de cambio | low | rechazado | diseño fijado en el Code Map (centro de la vista y 10 %) |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y capturas «ruta» y «ruta-oscuro»; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: ruta real en coche por calles de sentido único y a pie; la línea cubre la calle al acercar y se ve al alejar.
