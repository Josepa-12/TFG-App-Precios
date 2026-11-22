---
title: 'Hoja del buscador con tres paradas: cerrada, mitad y completa'
type: 'feature'
created: '2026-11-22'
status: 'done'
route: 'dispatch'
baseline_commit: '00d326a9b7cb60cac34397bb22ce65dec00081e1'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La hoja del buscador se queda exactamente donde se suelta entre el 15 % y el 88 % de su recorrido (decisión del 19/11/2026, ahora cambiada por el usuario): queda a medias en cualquier altura.

**Approach:** Al soltarla, la hoja va siempre a una de tres paradas: **cerrada** (cápsula, fracción 0), **mitad** (fracción 0,5 del recorrido) o **completa** (fracción 1). Sustituye a `DestinoHoja.Quedar`.

## Boundaries & Constraints

**Always:**
- Soltar sin lanzamiento: va a la parada más cercana a la fracción en que se suelta (cortes en 0,25 y 0,75).
- Decidido (usuario): un lanzamiento (> 0,6 dp/ms) lleva a la **parada siguiente en ese sentido** (cerrada → mitad → completa hacia arriba; al revés hacia abajo), como Apple Maps; si ya está en el extremo, se queda.
- Tocar la cápsula sigue abriéndola completa con el teclado; un arrastre nunca saca el teclado (como hoy).
- Al ir a cerrada se quita el foco del campo (como hoy); en mitad y completa se conserva lo escrito y los resultados.
- El desplazamiento de la lista y el gesto desde la lista siguen las reglas de hoy (`ListaMueveHoja`), con «abierta del todo» = completa.
- Atrás pliega la hoja (como hoy, desde cualquier parada).
- El paso a la parada se anima como hoy cualquier apertura o pliegue; sin animación si el sistema las quita.

**Never:** cambiar la cápsula, la lista, «Encuentra cerca» ni la hoja de la ruta; añadir paradas nuevas fuera de estas tres.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Soltar cerca de arriba | Fracción 0,8, sin lanzamiento | Completa |
| Soltar en medio | Fracción 0,4 o 0,6 | Mitad |
| Soltar cerca de abajo | Fracción 0,2 | Cerrada, sin foco en el campo |
| Toque en la cápsula | Plegada | Completa con el teclado |
| Atrás | En mitad | Cerrada |
| Lanzamiento arriba | Desde cerrada (parada de partida) | Mitad; otro lanzamiento, completa |
| Lanzamiento abajo | Desde completa | Mitad; desde mitad, cerrada |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `Scripts/UI/FisicaHoja.cs` (193 l.) -- `enum DestinoHoja { Plegada, Abierta, Quedar }` (l. 6) → `Plegada, Mitad, Abierta`; `Destino(k, v)` (~l. 177: `Lanzamiento`, `PlegarDebajo` 0,15, `AbrirEncima` 0,88) y `Soltar` (~l. 148-165: pone `Fraccion` según el destino) son la regla; el comentario de la clase cuenta la decisión del 19/11 y hay que reescribirlo. Constante nueva para la mitad (0,5).
- `Scripts/UI/HojaBuscador.cs` (764 l.) -- `Soltar` (~l. 617-629: toque → enfoca; arrastre a `Plegada` → quita el foco), `Aplicar`, `Top` (~l. 354), `Mezcla` (~l. 357, mezcla cápsula→hoja hasta 0,15: no cambia), `Abrir`/`Plegar` (~l. 395/412).
- `Tests/EditMode/PruebasBuscador.cs` -- l. 62, 132, 164 esperan `DestinoHoja.Quedar`: pasan a la parada que toca; pruebas nuevas por fila de la matriz.
- `Scripts/Arranque/CapturaPruebas.cs` -- si alguna captura suelta la hoja a medias, que quede en una parada; captura `buscador-mitad` (claro y oscuro).

## Tasks & Acceptance

**Execution:**
- [x] `Scripts/UI/FisicaHoja.cs` -- tres paradas en `Destino`/`Soltar`, constante de la mitad, comentario -- la regla nueva, pura.
- [x] `Scripts/UI/HojaBuscador.cs` -- tratar `Mitad` (foco y aplicar) -- comportamiento de la matriz.
- [x] `Tests/EditMode/PruebasBuscador.cs` -- una prueba por fila de la matriz y por el lanzamiento; actualizar las de `Quedar`.
- [x] `Scripts/Arranque/CapturaPruebas.cs` -- captura `buscador-mitad` y `buscador-mitad-oscuro`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas y ninguna usa `Quedar`.
- Dada la build de PC con `-captura`, cuando se hace `buscador-mitad`, entonces la hoja está a la mitad del recorrido.

## Implementation Notes

- `DestinoHoja { Plegada, Mitad, Abierta }`; `FisicaHoja.FraccionMitad` 0,5, `CorteCerrada` 0,25, `CorteCompleta` 0,75 (en el corte exacto, mitad); `AbrirEncima` desaparece; `PlegarDebajo` pasa a `MezclaHasta` (0,15, solo la mezcla cápsula→hoja de `HojaBuscador.Mezcla`). `FraccionDe(parada)` da la fracción de cada parada. A 0,6 dp/ms justos no es lanzamiento.
- Lanzamiento: `Destino(k, v)` va a la primera parada estrictamente más allá de la fracción actual k en ese sentido (arriba: mitad si k < 0,5, si no completa; abajo: mitad si k > 0,5, si no cerrada). Desde una parada coincide con «la siguiente»; si se cambia de sentido a medio arrastre (de la mitad bajada al 30 % y lanzada arriba) va a la mitad, sin saltársela.
- `FisicaHoja.Cancelar` durante un arrastre (p. ej. `Mostrar(false)` con el dedo abajo, o un gesto nuevo encima de uno sin soltar en `AlBajar`, que ahora también llama a `Aplicar`) lleva la hoja a la parada más cercana, para que nunca quede a medias. El `PointerCancel` del sistema en arrastre pasa por `Soltar`.
- `HojaBuscador.Soltar` ya trataba bien la mitad (el arrastre quita el foco al empezar; en las tres paradas se conservan lo escrito y los resultados); solo cambia el comentario. La animación sigue siendo la transición USS de 0,32 s (quitada con «reducir animaciones»).
- Captura nueva `BuscadorMitad` (tras `Buscador`): gesto real hasta el 40 %, soltar despacio → mitad; lanzamiento arriba → completa; atrás → cerrada.

## Spec Change Log

## Review Triage Log

Revisión 1 (22/11/2026).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | El lanzamiento decide por la parada de partida, no por el dedo: de mitad bajar a 0,3 y lanzar arriba va a completa (blind, edge, verification) | medium | `Destino(k, v, inicio)` usa `inicio` | patch: la siguiente parada más allá de `k` en el sentido del lanzamiento |
| 2 | `inicio` que no es parada (tras `Poner`) | low | Desaparece con el 1 | patch (con el 1) |
| 3 | `NingunaSueltaDejaLaHojaFueraDeLasTresParadas` no puede fallar; el bucle de coma flotante no llega a 1 | low | `FraccionDe` siempre da una parada | patch: probar `Soltar` con fracciones enteras de una tabla |
| 4 | Sin pruebas en los cortes exactos 0,25/0,75 ni con v = ±0,6 | low | La matriz prueba 0,24/0,26/0,74/0,76 | patch |
| 5 | `AlCancelar` llama a `Soltar` (un cancelado rápido cuenta como lanzamiento); el comentario de `Cancelar` no lo cuenta | low | Comportamiento aceptable; el comentario confunde | patch: comentario |
| 6 | `Soltar` sin arrastre devuelve un destino distinto de la fracción | low | Solo tras `Poner` de la captura | rechazado |
| 7 | La captura `buscador-mitad` repite `buscador` | low | Comprueba además el gesto y el lanzamiento | rechazado |
| 8 | En la mitad, arrastrar la lista mueve la hoja (no se desplaza) | false | Como Apple Maps: desde la mitad se sube a completa para desplazar | rechazado |
| 9 | TalkBack no llega a la mitad | low | «Expandir» a completa y «Contraer» a cerrada bastan | rechazado |
| 10 | Documentos (memoria del TFG, planificación) siguen con «se queda donde se suelta» | low | Archivos de contexto | defer: memoria y CLAUDE.md al cerrar |
| 11 | El comentario de `FisicaHoja` cita el prototipo; `PlegarDebajo` mal nombrada | low | Solo `Mezcla` la usa | patch: comentario y nombre |
| 12 | (gap) `Mostrar(false)` a medio arrastre: ninguna prueba mira la fracción final | gap | La de 40 dp ya cae en cerrada | patch: prueba a ~0,4 → mitad y `Top` |
| 13 | `AlBajar` con un gesto colgado llama a `Cancelar` sin `Aplicar`: la hoja salta bajo el dedo | low | `Cancelar` ahora mueve la fracción | patch: `Aplicar` tras `Cancelar` |
| 14 | El comentario de `HojaBuscador.Soltar` dice que mitad y completa conservan el foco | low | Un arrastre siempre quita el foco | patch: comentario |
| 15 | Captura: sin guarda de hoja sin medir; `worldBound` frente a `TopPlegada` | low | Solo en la captura | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` (con `-buildTarget Win64`) y `PriceMap.exe -captura` -- capturas nuevas; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.
