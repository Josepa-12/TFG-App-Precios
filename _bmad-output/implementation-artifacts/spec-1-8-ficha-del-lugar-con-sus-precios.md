---
title: 'Historia 1.8: ficha del lugar con sus precios'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los marcadores no se pueden tocar. No hay forma de ver los precios de un lugar, su origen ni lo recientes que son, que es el objetivo de la épica 1 (FR-7 a FR-10, FR-60).

**Approach:**
- Tocar un marcador de lugar abre una ficha desde abajo (UI Toolkit). La ficha tiene:
  - **Cabecera:** nombre, tipo con el color de su plantilla y dirección.
  - **Fila de acciones:** reservada bajo la cabecera.
  - **Conceptos:** agrupados por área, cada uno con su precio, su origen y su punto de frescura, calculados con el Dominio de la 1.3.
- El mapa centra el lugar en la parte que no tapa la ficha.
- Se cierra con el botón atrás, al tocar el mapa o con su botón de cerrar. Al cerrarse, el foco vuelve al marcador.

## Boundaries & Constraints

**Always:**
- **Tocar un marcador:** los marcadores siguen sin capturar toques (`Ignore`), para que el mapa se pueda arrastrar empezando encima de uno.
  - El toque se detecta al soltar el dedo, si se ha movido menos de 6 dp (`EXPERIENCE.md:89`).
  - Gana el marcador visible más cercano al punto tocado, si la distancia es menor de 24 dp.
  - Tocar un punto de interés no abre nada: la ficha de destino es de la épica 2.
- **Ficha:**
  - Superficie opaca, esquinas superiores de 24 dp, altura máxima del 72 % de la pantalla y desplazamiento interno (DESIGN `components.sheet`).
  - Cabecera: nombre en `title`; tipo con una muestra del color de su plantilla (`--color-pin-*`) y la plantilla ("Clínica privada · Servicio local"); dirección si la hay.
  - Botón de cerrar de 48 dp.
- **Fila de acciones:** elemento `fila-acciones` bajo la cabecera, vacío y sin altura hasta que otras épicas lo rellenen (FR-10). Su orden en el árbol es fijo.
- **Conceptos:**
  - Agrupados por `Area` y en el orden del JSON.
  - Precio con `FormatoPrecio`: fijo "90 €", "desde 40 €" y, en hostelería, rango "10–20 € por persona".
  - "Consultar" cuando la web no da importe.
  - Origen: "Publicado en su web · 13/11/2026" (forma del criterio de aceptación y de la maqueta).
  - Punto de frescura con su color (`fresh-*`) y su texto: "Al día", "Podría haber cambiado" o "Antiguo".
  - Unidad si la hay ("sesión").
- **Variantes (FR-60):** debajo de su concepto, con su condición y su precio. La vigente (`ReglaVariantes.Vigente`, hora local) se destaca. Se implementan y se prueban con datos sintéticos (ver la pregunta 1).
- **Sin precio:** "Aún no tiene precio" (UX-DR16), sin inventar nada.
- **Centrado:** el lugar queda centrado en la zona del mapa que no tapa la ficha.
- **TalkBack:**
  - Al abrir, el foco va al título.
  - La ficha es modal: el recorrido solo incluye la ficha, como el menú "Precios".
  - Al cerrar (atrás, cerrar o tocar el mapa), el foco vuelve al nodo del marcador.
- **Datos:** "ahora" lo da el Arranque (AD-12). Nada se guarda (AD-3). Los textos van por Localization.

**Never:**
- No hay ficha de destino para los puntos de interés ni acciones reales (Ir, favorito, avisar…): son de las épicas 2, 3, 7, 8 y 9.
- Sin arrastrar la ficha entre alturas (ver la pregunta 2).
- Sin estados de carga ni de "sin conexión": los datos van dentro de la app.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Tocar marcador | Toque (< 6 dp de movimiento) sobre Clínica Areas | Se abre su ficha y el mapa centra el lugar sobre la ficha | N/A |
| Arrastrar | El dedo empieza sobre un marcador y se mueve 30 dp | El mapa se mueve y no se abre nada | N/A |
| Clínica Areas | Su ficha | Áreas Traumatología, Fisioterapia, Suelo pélvico y Readaptación. "Sesión de fisioterapia · desde 40 €", "Infiltraciones · Consultar", origen "Publicado en su web · 13/11/2026" y frescura "Al día" (ahora 18/11/2026) | N/A |
| Sin precio | ChemaBarber | "Aún no tiene precio" | N/A |
| Rango hostelería | Concepto sintético [1000, 2000] | "10–20 € por persona" | N/A |
| Variante vigente | Concepto sintético con la variante "Con luz" 20–24 h; ahora 21:00 | Variante bajo su concepto, destacada | N/A |
| Cerrar con atrás | Ficha abierta y `Escape` | Se cierra y el foco vuelve al marcador | N/A |
| Tocar el mapa | Ficha abierta y toque fuera de ella | Se cierra; el mapa no se mueve con ese toque | N/A |
| Menú "Precios" abierto | Toque en un marcador | Solo se cierra el menú, sin abrir la ficha | N/A |

</frozen-after-approval>

## Open Questions

1. **Variante "con bono" del criterio de aceptación.** La historia pide ver "Superinductiva · con bono · desde 15 €", pero el 17/11 decidiste quitar de los datos los precios con bono.
   - **A) Mantener tu decisión.** La ficha sabe mostrar variantes y se prueba con datos de ejemplo, pero ningún lugar real tiene variantes todavía. Recomendado.
   - **B) Recuperar solo esa.** Superinductiva vuelve como concepto con la variante "con bono · desde 15 €".
2. **¿La ficha se puede arrastrar?** El diseño no define alturas para la ficha.
   - **A) Altura fija** (hasta el 72 % de la pantalla), con desplazamiento dentro si no cabe; se cierra con la X, con atrás o tocando el mapa. Recomendado: más sencillo y accesible.
   - **B) Dos alturas** (media y completa) que se cambian arrastrando, con "Expandir/Contraer" para TalkBack.
3. **Tamaño.** Este spec es largo (unos 2.600 tokens). Propongo partirlo:
   - **Parte 1:** tocar un marcador y ver la ficha con cabecera, conceptos, precios, origen, frescura, "sin precio", fila de acciones y cerrar.
   - **Parte 2:** variantes destacadas, centrado sobre la zona libre y TalkBack (foco al título, modal, vuelta al marcador).

## Code Map

- `Assets/Scripts/Dominio/`:
  - Modelo: `Lugar` (Conceptos, Direccion, Plantilla, Tipo), `Concepto` (Area, Nombre, Unidad, Fuentes, Variantes) y `Variante` (Id, Condicion, Fuentes; `CondicionVariante.SeCumple`).
  - Cálculo: `CalculadoraPrecio.Calcular(FuentesPrecio, DateTime) → PrecioMostrado` (Valor, EsConsultar, Origen, Frescura, TienePrecio) y `ReglaVariantes.Vigente(variantes, ahoraLocal)`.
  - Tipos: `ValorPrecio` (Forma, ImporteCent, MinCent, MaxCent), `Origen` (Tipo, Url, FechaWeb) y `EstadoFrescura`.
- `Assets/Scripts/UI/FormatoPrecio.cs:25` (`Euros`): se añaden "desde" y el rango "por persona".
- `Assets/Scripts/UI/MarcadorLugar.cs:53` y `CapaMarcadores.cs:51`: `Ignore`; `Posicion`, `Visible` y `Lugar`. `PruebasMarcadores.cs:215-220` exige `Ignore` y `TapaPunto` false, y eso se mantiene.
- `Assets/Scripts/Vectorial/CamaraMapa.cs:50,63`:
  - Hoy no distingue un toque de un arrastre.
  - Hay que añadir el evento `Tocado(Vector2 pantalla)` cuando el dedo se suelta con menos de 6 dp de movimiento.
  - `IMapa` lo expone como `event Action<PuntoPantalla> Toque`, y hay que actualizar los `MapaFalso`.
- `Assets/Scripts/UI/PantallaMapa.cs`:
  - Patrón de capa modal con velo y `Atras()` (menú "Precios", 1.5).
  - `TapaPunto`/`EsControl`.
  - `AccesibilidadPantalla`: `Modal`, `PedirFoco` y los nodos de marcas, para devolver el foco al marcador.
- `Assets/UI/PantallaMapa.uxml/.uss` y `Estilos/Tokens.uss`: se añaden la hoja (`ficha`) y los tokens `--color-fresh-*` (DESIGN `:33-35`, oscuro `:57-59`).
- Referencias:
  - `_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/mockups/key-ficha-lugar.html:69` (maqueta) y `prototipo/app.js:905-1041` (`openDest`, `drawPlace`).
  - Prototipo: `srcHTML` en `app.js:236-243`, y `FRESH`/`freshDot` en `prototipo/extras.js:17-57`.
- `Assets/Scripts/Arranque/CapturaPruebas.cs`: captura "ficha" con la ficha de Clínica Areas abierta.

## Tasks & Acceptance

**Execution:**
- [ ] `CamaraMapa.cs`, `IMapa`, `MapaVectorial` y los `MapaFalso`: evento de toque.
- [ ] `UI/FichaLugar.cs` (más `FilaConcepto` y `FilaVariante` si conviene), `FormatoPrecio` y textos: la ficha completa.
- [ ] `PantallaMapa.cs/.uxml/.uss` y `Tokens.uss`: abrir con un toque, cerrar (atrás, X, mapa), centrado en la zona libre y modo modal de TalkBack con el foco.
- [ ] `CapturaPruebas.cs`: captura "ficha".
- [ ] `Tests/EditMode/PruebasFicha.cs`: cada fila de la matriz.

**Acceptance Criteria:**
- Dada la build de PC, cuando se abre `ficha.png`, entonces se ve la ficha de Clínica Areas con sus áreas, "desde 40 €", "Consultar", el origen y el punto de frescura.
- Dado TalkBack forzado en las pruebas, cuando se abre y se cierra la ficha, entonces el foco va al título y vuelve al marcador.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC y `PriceMap.exe -captura <carpeta>` (sin tocar otras ventanas): `ficha.png` y log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.
