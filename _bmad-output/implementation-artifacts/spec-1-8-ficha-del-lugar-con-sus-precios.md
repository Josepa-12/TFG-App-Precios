---
title: 'Historia 1.8 (parte 1): ficha del lugar con sus precios'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '86431486aba9355e2ab010a2984923933e496431'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los marcadores no se pueden tocar. No hay forma de ver los precios de un lugar, su origen ni lo recientes que son, que es el objetivo de la épica 1 (FR-7 a FR-10).

**Approach:** Tocar un marcador de lugar abre una ficha desde abajo (UI Toolkit) con cabecera (nombre, tipo con el color de su plantilla y dirección), la fila de acciones reservada y los conceptos agrupados por área, cada uno con su precio, su origen y su punto de frescura, calculados con el Dominio de la 1.3. Se cierra con el botón atrás, con su botón de cerrar o tocando el mapa. La parte 2 (`deferred-work`) añade las variantes destacadas, el centrado del lugar en la zona libre y TalkBack (foco al título, modal, vuelta al marcador).

## Boundaries & Constraints

**Always:**
- **Decisiones del 18/11/2026:** se mantiene la decisión del 17/11 de no tener precios con bono en los datos (sin "Superinductiva · con bono"); la ficha tiene altura fija, sin arrastre entre alturas; la historia se parte en dos.
- **Tocar un marcador:** los marcadores siguen sin capturar toques (`Ignore`), para que el mapa se pueda arrastrar empezando encima de uno.
  - El toque se detecta al soltar el dedo, si se ha movido menos de 6 dp (`EXPERIENCE.md:89`).
  - Gana el marcador visible más cercano al punto tocado, si la distancia es menor de 24 dp.
  - Tocar un punto de interés no abre nada: la ficha de destino es de la épica 2.
  - Con el menú "Precios" o una ventana de ubicación abiertos, el toque solo los cierra (o nada), sin abrir la ficha.
- **Ficha:**
  - Superficie opaca, esquinas superiores de 24 dp, altura máxima del 72 % de la pantalla y desplazamiento interno si no cabe (DESIGN `components.sheet`).
  - Cabecera: nombre en `title`; tipo con una muestra del color de su plantilla (`--color-pin-*`) y la plantilla ("Clínica privada · Servicio local"); dirección si la hay.
  - Botón de cerrar de 48 dp.
- **Fila de acciones:** elemento `fila-acciones` bajo la cabecera, vacío y sin altura hasta que otras épicas lo rellenen (FR-10). Su orden en el árbol es fijo.
- **Conceptos:**
  - Agrupados por `Area` y en el orden del JSON.
  - Precio con `FormatoPrecio`: fijo "90 €", "desde 40 €" y, en hostelería, rango "10–20 € por persona".
  - "Consultar" cuando la web no da importe.
  - Origen: "Publicado en su web · 13/11/2026".
  - Punto de frescura con su color (`fresh-*`) y su texto: "Al día", "Podría haber cambiado" o "Antiguo".
  - Unidad si la hay ("sesión").
- **Sin precio:** "Aún no tiene precio" (UX-DR16), sin inventar nada.
- **Cerrar:** botón atrás (`Escape`), botón de cerrar o un toque en el mapa fuera de la ficha; ese toque no mueve el mapa.
- **Datos:** "ahora" lo da el Arranque (AD-12). Nada se guarda (AD-3). Los textos van por Localization.

**Never:** sin variantes en la ficha, sin centrar el lugar y sin el modo modal de TalkBack (parte 2); sin ficha de destino para los puntos de interés ni acciones reales (Ir, favorito, avisar…: épicas 2, 3, 7, 8 y 9); sin arrastrar la ficha; sin estados de carga ni de "sin conexión".

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Tocar marcador | Toque (< 6 dp de movimiento) sobre Clínica Areas | Se abre su ficha | N/A |
| Toque cerca | Toque a 20 dp del marcador | Se abre su ficha | N/A |
| Toque lejos | Toque a 30 dp del marcador más cercano | No se abre nada | N/A |
| Arrastrar | El dedo empieza sobre un marcador y se mueve 30 dp | El mapa se mueve y no se abre nada | N/A |
| Clínica Areas | Su ficha | Áreas Traumatología, Fisioterapia, Suelo pélvico y Readaptación. "Sesión de fisioterapia · desde 40 €", "Infiltraciones · Consultar", origen "Publicado en su web · 13/11/2026" y frescura "Al día" (ahora 18/11/2026) | N/A |
| Sin precio | ChemaBarber | "Aún no tiene precio" | N/A |
| Rango hostelería | Concepto sintético [1000, 2000] | "10–20 € por persona" | N/A |
| Cerrar con atrás | Ficha abierta y `Escape` | Se cierra | N/A |
| Cerrar con la X | Toque en cerrar | Se cierra | N/A |
| Tocar el mapa | Ficha abierta y toque fuera de ella | Se cierra; el mapa no se mueve con ese toque | N/A |
| Menú "Precios" abierto | Toque en un marcador | Solo se cierra el menú, sin abrir la ficha | N/A |

</frozen-after-approval>

## Code Map

- `Assets/Scripts/Dominio/`:
  - Modelo: `Lugar` (Conceptos, Direccion, Plantilla, Tipo), `Concepto` (Area, Nombre, Unidad, Fuentes, Variantes).
  - Cálculo: `CalculadoraPrecio.Calcular(FuentesPrecio, DateTime) → PrecioMostrado` (Valor, EsConsultar, Origen, Frescura, TienePrecio).
  - Tipos: `ValorPrecio` (Forma, ImporteCent, MinCent, MaxCent), `Origen` (Tipo, Url, FechaWeb) y `EstadoFrescura`.
- `Assets/Scripts/UI/FormatoPrecio.cs:25` (`Euros`): se añaden "desde" y el rango "por persona".
- `Assets/Scripts/UI/MarcadorLugar.cs:53` y `CapaMarcadores.cs:51`: `Ignore`; `Posicion`, `Visible` y `Lugar`. `PruebasMarcadores.cs:215-220` exige `Ignore` y `TapaPunto` false, y eso se mantiene.
- `Assets/Scripts/Vectorial/CamaraMapa.cs:50,63`:
  - Hoy no distingue un toque de un arrastre.
  - Hay que añadir el evento `Tocado(Vector2 pantalla)` cuando el dedo se suelta con menos de 6 dp de movimiento (con ratón en PC, igual).
  - `IMapa` lo expone como `event Action<PuntoPantalla> Toque` (origen arriba a la izquierda, como `Proyectar`), y hay que actualizar los seis `MapaFalso`.
- `Assets/Scripts/UI/PantallaMapa.cs`:
  - Patrón de capa con velo y `Atras()` (menú "Precios", 1.5; ventanas de ubicación, 1.7). `Atras()` cierra la ficha antes que nada salvo las ventanas de ubicación.
  - `TapaPunto`/`EsControl`: con la ficha abierta, un toque fuera de ella la cierra y no mueve el mapa.
  - `EscalaDp` para pasar de px a dp (6 dp y 24 dp).
- `Assets/UI/PantallaMapa.uxml/.uss` y `Estilos/Tokens.uss`: se añaden la hoja (`ficha`) y los tokens `--color-fresh-*` (DESIGN `:33-35`, oscuro `:57-59`).
- Referencias:
  - `_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/mockups/key-ficha-lugar.html:69` (maqueta) y `prototipo/app.js:905-1041` (`openDest`, `drawPlace`).
  - Prototipo: `srcHTML` en `app.js:236-243`, y `FRESH`/`freshDot` en `prototipo/extras.js:17-57`.
- Textos: `ClavesTexto.cs` (claves y `Todas`) y `Editor/PrepararInterfaz.cs` (`TextosEs`); la tabla se regenera con `PrepararInterfaz.Asegurar` por lotes.
- `Assets/Scripts/Arranque/CapturaPruebas.cs`: captura "ficha" con la ficha de Clínica Areas abierta.
- Build de PC: lanzar con `-buildTarget Win64` (si la última build fue Android, el cambio de plataforma dentro del proceso falla).

## Tasks & Acceptance

**Execution:**
- [x] `CamaraMapa.cs`, `IMapa`, `MapaVectorial` y los `MapaFalso`: evento de toque.
- [x] `UI/FichaLugar.cs` (más `FilaConcepto` si conviene), `FormatoPrecio` y textos: cabecera, fila de acciones, conceptos con precio, origen, frescura y unidad, y "Aún no tiene precio".
- [x] `PantallaMapa.cs/.uxml/.uss` y `Tokens.uss`: abrir con un toque y cerrar (atrás, X, mapa).
- [x] `CapturaPruebas.cs`: captura "ficha".
- [x] `Tests/EditMode/PruebasFicha.cs`: cada fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas, incluida una prueba por fila de la matriz.
- Dada la build de PC, cuando se abre `ficha.png`, entonces se ve la ficha de Clínica Areas con sus áreas, "desde 40 €", "Consultar", el origen y el punto de frescura, y el log no tiene errores.

## Implementation Notes

- **Distancia del toque al marcador:** se mide al rectángulo dibujado (cuerpo + punta, `MarcadorLugar.ZonaDp`), no a la punta: tocar el cuerpo da 0. A igual distancia gana el más cercano a la cámara (el que se pinta encima). Lados en dp copiados de los tokens (42/32 y punta 8/6).
- **Cerrar tocando el mapa:** con la ficha abierta `TapaPunto` da true en toda la pantalla (el mapa no se mueve) y un velo transparente bajo la hoja la cierra al soltar. `PantallaMapa.TocarMapa` también cierra si llegara un toque por el puerto.
- **Capas:** `hueco-ficha` va encima de la píldora y debajo de las ventanas de ubicación (se ajustó `PruebasFiltro.ConElMenuAbiertoLosBotonesDeTemaYZoomQuedanBajoElVelo`).
- **Unidad:** "/sesión" como el prototipo (`app.js · UNIT`); la unidad "servicio" no se muestra. Sin unidad en "Consultar".
- **ScrollView sin tema por defecto:** barras ocultas (se arrastra con el dedo) y reglas mínimas de recorte en `PantallaMapa.uss`. En la captura el contenido de Clínica Areas cabe (68 % del alto), así que el desplazamiento no se ve en `ficha.png`.
- **TalkBack:** la ficha aún no está en la jerarquía de accesibilidad (parte 2).

## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | Rango con unidad duplicada: "10–20 € por persona" y "/persona" (BH, ECH) | low | `FilaConcepto` añade la unidad si no es "servicio"; el concepto sintético del bar usa `persona`. | patch |
| 2 | Con la ficha abierta, la píldora sigue activable (teclado/TalkBack) y el menú se abre debajo (ECH) | medium | `AlternarMenu` no mira `FichaAbierta`; los nodos de TalkBack de la píldora siguen activos en la parte 1. | patch |
| 3 | El comentario de la captura dice "mismo camino que CamaraMapa" pero llama a `TocarMapa` (BH) | low | `CapturaPruebas.Ficha` llama a `pantalla.TocarMapa` directamente. | patch |
| 4 | Camino CamaraMapa → MapaVectorial (giro de Y) → IMapa.Toque sin probar (VG, BH) | medium | Ninguna prueba sube `Tocado` en una `CamaraMapa` real; `ElToqueDelMapaLlegaPorElPuerto` usa un `MapaFalso`. | patch (parte MapaVectorial); defer (ramas de `Input`) |
| 5 | Desempate de marcadores solapados por profundidad sin probar (VG, BH) | medium | `GanaElMarcadorMasCercano` usa distancias distintas y la misma profundidad. | patch |
| 6 | `LadoDp`/`PuntaDp` copiados de los USS sin prueba que los ate (VG, BH) | low | `PruebasMarcadores` ya lee los USS para otros tokens; nada compara 42/32 y 8/6. | patch |
| 7 | Unidad "servicio" oculta sin prueba (VG) | low | Solo se comprueba `Unidad == null` en una fila "Consultar". | patch |
| 8 | Clases de color "podría"/"antiguo" del punto de frescura sin prueba (VG) | low | Solo se comprueba `--al-dia`. | patch |
| 9 | `TocarUnPuntoDeInteresNoAbreNada` no pone ningún punto de interés (BH) | low | El `MapaFalso` no tiene puntos; repite la prueba de 30 dp. | patch |
| 10 | Pruebas con `TextosEs` y no con la tabla real (VG) | medium | `PruebasTextos` solo comprueba que la clave existe; hueco anterior a esta historia (ya anotado en `deferred-work`). | defer |
| 11 | Desplazamiento interno de la ficha sin ver funcionando (BH) | maybe-false | En la captura cabe todo; hace falta un lugar con más conceptos o una prueba en el móvil. | defer |
| 12 | "por persona" en rangos de cualquier plantilla (BH, ECH) | low | No hay rangos en `lugares.json`; las variantes y los rangos llegan con más datos. Arreglarlo añade un parámetro. | rechazado |
| 13 | Rango con mínimo igual al máximo: "20–20 €" (BH, ECH) | low | No hay rangos en los datos; arreglarlo añade una rama. | rechazado |
| 14 | Arrastrar sobre el velo cierra la ficha (BH, ECH) | low | Es el mismo patrón que el velo del menú "Precios" (1.5); el mapa no se mueve. Arreglarlo añade lógica. | rechazado |
| 15 | Ratón en un PC con pantalla táctil no abre la ficha (BH, ECH) | low | La build de PC es solo para capturas; es raro en el uso normal y el arreglo toca la lógica del ratón simulado en Android. | rechazado |
| 16 | La rueda del ratón sobre la ficha hace zoom en el mapa (ECH) | low | Solo en PC; igual con el menú "Precios" desde la 1.5. | rechazado |
| 17 | `FichaCerrar` no se usa; la X no tiene etiqueta de TalkBack (BH) | low | Clave preparada para la parte 2 (TalkBack). | rechazado |
| 18 | TalkBack inutilizable con la ficha abierta (BH) | low | El usuario decidió partir la historia: el modo modal de TalkBack es la parte 2. El caso de la píldora va en el n.º 2. | rechazado |
| 19 | Grupo sin área tras uno con área; tipo vacío; lugar público (ECH) | low | Ningún lugar de `lugares.json` tiene `publico: true`, área vacía ni tipo vacío. | rechazado |
| 20 | "1 clientes" y "Publicado en su web · " sin fecha (BH) | false / low | `Origen.DeWeb` exige siempre fecha; el origen "clientes" no llega hasta épicas posteriores. | rechazado |
| 21 | `Abrir` con la ficha ya abierta no reaplica la escala de fuente (ECH) | false | `TocarMapa` cierra la ficha si está abierta; nadie llama a `AbrirFicha` con ella abierta. | rechazado |
| 22 | `MapaVectorial` no se desuscribe de la cámara (BH) | low | Cámara y mapa viven en la misma escena y se destruyen juntos. | rechazado |
| 23 | La escala de fuente se aplica a toda la raíz en cada cambio de geometría de la ficha (BH) | maybe-false / low | `EscalaFuente.Aplicar` parte de los tamaños base guardados, así que no se acumula. | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC (`-buildTarget Win64`) y `PriceMap.exe -captura <carpeta>`: `ficha.png` y log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.
