---
title: 'Hoja de la ruta que solo informa y se despliega con «Finalizar ruta»'
type: 'feature'
created: '2026-11-22'
status: 'done'
route: 'dispatch'
baseline_commit: 'd598150cd5b01b430a52a5dbf8b4e68f20c77d9d'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Navegando, tocar en cualquier parte de la tarjeta de abajo (llegada, minutos, km) reabre la ficha del destino y esconde las tarjetas: en el móvil parece que la ruta se cancela. El usuario quiere que la tarjeta solo informe.

**Approach:** La tarjeta de abajo pasa a ser una hoja con dos posiciones, como Apple Maps (captura del usuario): **plegada**, solo informa (asa + llegada/min/km); **desplegada**, debajo sale la fila del destino y un botón rojo ancho **«Finalizar ruta»** que termina la ruta. Se despliega o pliega tocando la hoja o arrastrándola.

## Boundaries & Constraints

**Always:**
- Plegada, ningún toque en la hoja termina la ruta ni abre la ficha; solo despliega.
- Desplegada: tocar la parte de arriba, arrastrar hacia abajo o el botón atrás la pliegan. Atrás con la hoja plegada hace lo de hoy (quita la ruta).
- Decidido (usuario): la hoja plegada **no lleva X**; solo se termina con «Finalizar ruta» desplegada (o atrás).
- Decidido (usuario): la fila del destino (icono + nombre) **solo informa**: tocarla no abre la ficha. Desde la hoja ya no se reabre la ficha (`ReabrirFichaRuta` se queda, sin llamarla la hoja).
- «Finalizar ruta» = lo que hace hoy la X (`QuitarRuta`). Sin navegación (calculando o con fallo) la hoja funciona igual y el botón dice «Quitar ruta».
- Al desplegarse o plegarse, el encuadre y el seguimiento de la cámara usan la altura nueva de la hoja (como ya hace `AlMedirTarjetaRuta`).
- TalkBack: la hoja se anuncia con su texto de siempre y la acción «Desplegar»/«Plegar»; desplegada, «Finalizar ruta» es un botón propio. Animación quitada si el sistema las quita.
- Tema claro y oscuro con los tokens de siempre; el botón con fondo `--color-danger` y texto `--color-surface` (AA en los dos temas).

**Never:** tocar la tarjeta de maniobra de arriba; añadir «Añadir parada», «Compartir llegada» o «Informar de una incidencia»; cambiar el flujo de Ir (es el cambio 5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Tocar plegada | Navegando, hoja plegada | Se despliega; la ruta sigue, la ficha no se abre |
| Finalizar | Hoja desplegada, «Finalizar ruta» | Ruta terminada, vuelve la cápsula del buscador |
| Plegar | Desplegada, toque arriba / arrastre abajo / atrás | Plegada; la ruta sigue |
| Atrás plegada | Navegando, hoja plegada | Quita la ruta (como hoy) |
| Arrastre | Arrastrar hacia arriba > 6 dp | Se despliega; hacia abajo, se pliega |
| Sin navegación | Calculando o fallo | Igual; el botón dice «Quitar ruta» |
| Fin de ruta | Llegada o recálculo con la hoja desplegada | Se cierra plegada con la ruta; la siguiente empieza plegada |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `Scripts/UI/TarjetaRuta.cs` (164 l.) -- hoy: asa, `Cuerpo` con `Clickable(Tocar)` → evento `Tocada`, columnas llegada/min/km, `Quitar` (BotonRedondo X) → `QuitarPulsado`; `MostrarCalculando/MostrarFallo/MostrarRestante`, `Anuncio`, `PonerMargenInferior`. Aquí van el estado desplegada, la fila del destino (icono de `IconosLugar` + nombre) y el botón «Finalizar ruta»; la X (`Quitar`) desaparece (revisar quién usa `Quitar`/`QuitarPulsado`, p. ej. TalkBack ~1725-1733).
- `Scripts/UI/PantallaMapa.cs` -- `CrearTarjetasRuta` ~2100 (`Tocada += ReabrirFichaRuta`, `QuitarPulsado += QuitarRuta`), `DeclararTarjetaRutaEnTalkBack` ~2116, `AlMedirTarjetaRuta` ~2125, `QuitarRuta` 2041, `ReabrirFichaRuta` 2063 (se conserva para la ficha), `Atras` 2427 (plegar antes que quitar la ruta), `lugarRuta`/`puntoRuta` para la fila del destino.
- `Scripts/UI/FisicaHoja.cs` -- solo el umbral de toque (`UmbralToque` 6 dp) como referencia; no se cambia (el cambio 2 la toca).
- `UI/PantallaMapa.uss` 1746-1820 (`.tarjeta-ruta*`), `UI/Estilos/Tokens.uss` (token del rojo).
- `Scripts/UI/ClavesTexto.cs` (`RutaTerminar` 308, `RutaQuitar` 278) y `Assets/Localizacion/Textos_es-ES.asset` + `Textos Shared Data.asset`: textos nuevos «Finalizar ruta», «Desplegar», «Plegar».
- `Scripts/Arranque/CapturaPruebas.cs` ~800-830 (captura `ruta`: hoy `TarjetaRuta.Tocar()` reabre la ficha) -- añadir captura `ruta-desplegada` (claro y oscuro).
- Pruebas que cambian de expectativa: `Tests/EditMode/PruebasRutas.cs` 1089-1144 (`Abajo.Tocar()` reabría la ficha). `ReabrirFichaRuta()` directo en `PruebasNavegacion`, `PruebasCamaraNavegacion`, `PruebasFlechaYSenal`, `PruebasRutaAproximada`: siguen valiendo.

## Tasks & Acceptance

**Execution:**
- [x] `Scripts/UI/TarjetaRuta.cs` -- estado plegada/desplegada, toque y arrastre, fila del destino, botón «Finalizar ruta» -- la hoja nueva.
- [x] `Scripts/UI/PantallaMapa.cs` -- conectar eventos, atrás, TalkBack y encuadre -- comportamiento de la matriz.
- [x] `UI/PantallaMapa.uss`, `UI/Estilos/Tokens.uss`, textos -- aspecto de la maqueta aprobada.
- [x] `Tests/EditMode/PruebasRutas.cs` (y nueva sección si hace falta) -- una prueba por fila de la matriz y contraste del rojo en `PruebasContraste`.
- [x] `Scripts/Arranque/CapturaPruebas.cs` -- captura `ruta-desplegada` y `ruta-desplegada-oscuro`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC con `-captura`, cuando se hacen las capturas, entonces `ruta-desplegada` y `ruta-desplegada-oscuro` coinciden con la maqueta.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Revisión 1 (22/11/2026).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | (gap) El gesto real (`AlBajar/AlMover/AlSoltar`) no lo prueba nada; las pruebas llaman a `Tocar/Soltar` (blind, verification) | gap | Sin `PointerDownEvent` en las pruebas | patch: pruebas con eventos de puntero |
| 2 | Los pliegues de la app (ruta nueva, recálculo, fin, `QuitarRuta`) también rehacen TalkBack y llevan el foco a la hoja (blind; edge 2) | medium | `AlDesplegarHojaRuta` en cada cambio; un recálculo conduciendo mueve el foco | patch: solo los del usuario (toque, arrastre, atrás) |
| 3 | La ficha ya no se reabre desde la pantalla | false | Decisión del usuario: la fila del destino solo informa | rechazado |
| 4 | Punto libre: si la dirección llega tras Ir, la fila sigue con «Punto en el mapa» (blind, edge, verification) | low | `PonerDestinoRuta` solo en `Ir()` | patch: actualizar al llegar la dirección + prueba con y sin dirección |
| 5 | Clave `RutaTerminar` sin uso (blind, edge) | low | Todos los usos pasaron a `RutaFinalizar` | patch: quitarla |
| 6 | `tarjeta-ruta--desplegada` sin regla USS | low | Marca de estado; sin daño | rechazado |
| 7 | Sin respuesta visual al pulsar la cabecera ni «Finalizar ruta» (blind, edge) | low | Se borró `.tarjeta-ruta__cuerpo:active` sin sustituto | patch: `:active` |
| 8 | Sin transición al desplegar | false | El spec solo pide respetar «quitar animaciones» si las hay; no pide animación | rechazado |
| 9 | `ClaseIconoDestino` en `PantallaMapa` | low | Solo orden del código | rechazado |
| 10 | «, » del anuncio de TalkBack sin clave | low | Como los otros anuncios compuestos | rechazado |
| 11 | La prueba de textos no mira los `.asset` de Localization | low | Ya en `deferred-work` (comparar la tabla con `TextosEs`) | rechazado |
| 12 | `CLAUDE.md` y la memoria del TFG sin actualizar | low | Archivos de contexto | defer: se actualizan al cerrar |
| 13 | Un segundo dedo cancela el gesto del primero | low | Raro; el reinicio existe para gestos colgados | rechazado |
| 14 | `ReabrirFichaRuta` no pliega la hoja | false | La pantalla ya no la llama | rechazado |
| 15 | Un recálculo pliega la hoja con el dedo sobre «Finalizar»; al soltar, `Tocar()` la vuelve a abrir | low | `AlSoltar` solo ignora el desplegable si está desplegada | patch: un toque que empezó dentro del desplegable nunca alterna |
| 16 | `Tipo` vacío daría « · en coche» | false | Ningún lugar de los datos tiene `tipo` vacío | rechazado |
| 17 | Hoja oculta con el dedo abajo: al soltar se despliega | low | Raro; añade guarda | rechazado |
| 18 | Sin `DetachFromPanelEvent` | low | La hoja no sale del panel | rechazado |
| 19 | `EncimaDelDesplegable` con tamaño 0 en el mismo fotograma | low | Insignificante | rechazado |
| 20 | (gap) El botón de ubicación sobre la hoja desplegada solo lo mira la captura | low | Lo comprueba `ComprobarHojaDesplegada`; en EditMode no hay disposición real | rechazado |

## Design Notes

Maqueta aprobada (22/11/2026): https://claude.ai/artifact/RYFuCWVTRWqj3BPiMKLdpA. Plegada: asa + tres columnas (valor `t-price`, rótulo `t-meta`), sin X. Desplegada, debajo y con 16 dp entre bloques: fila del destino en `--color-surface-2`, radio `--radio-md`, círculo de 40 dp del color de su plantilla con el icono de `IconosLugar`, nombre en negrita y debajo «categoría · en coche/a pie» en `--color-muted` (punto libre: su dirección); botón de 52 dp de alto, ancho entero, radio 26 dp, fondo `--color-danger`, texto `--color-surface` en `t-heading`.

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` y `PriceMap.exe -captura` -- capturas nuevas; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.
