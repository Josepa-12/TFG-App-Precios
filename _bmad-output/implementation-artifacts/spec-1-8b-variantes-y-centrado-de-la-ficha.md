---
title: 'Historia 1.8 (parte 2): variantes y centrado de la ficha'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '580abd3608f7ac61a53048e4d92bd63924d2a0ea'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La ficha de la parte 1 (`580abd3`) no muestra las variantes de precio (FR-60) y puede tapar el lugar que se está mirando.

**Approach:** Cada concepto lista debajo sus variantes, con su condición y su precio, y la que se cumple ahora va destacada. Al abrir la ficha, el lugar queda centrado en la parte del mapa que la ficha no tapa.

## Boundaries & Constraints

**Always:**
- **Decisión del 18/11/2026:** la 1.8 se parte en tres. TalkBack de la ficha va en la parte 3 (`deferred-work`).
- **Variantes:** van debajo de su concepto, cada una con su condición y su precio. El precio sale de `CalculadoraPrecio.Calcular(v.Fuentes, ahora)` y se formatea con `FormatoPrecio.Precio`. Textos de condición:
  - horario: "De 20:00 a 24:00";
  - días: "Lun, mar y vie", o "Lun a vie" si son seguidos;
  - cliente: "Con bono" para `con-bono`; cualquier otro tipo, su texto con la primera letra en mayúscula.
- **Vigente:** `ReglaVariantes.Vigente(concepto.Variantes, horaLocal)`. `horaLocal` es el "ahora" del Arranque, que llega en UTC, pasado a la hora local del móvil. Como mucho hay una vigente. **Decisión del 18/11:** se destaca con un chip "Ahora" delante de la condición (estilo `.now` de la maqueta).
- **Datos:** no se tocan `lugares.json` ni el repositorio (decisión del 17/11, mantenida el 18/11). Las variantes se prueban con lugares sintéticos.
- **Centrado:** al abrir, el lugar queda en el centro horizontal y a media altura de la zona del mapa que la ficha no tapa (desde arriba hasta el borde superior de la hoja). Si la hoja aún no tiene medida, se toma el 72 % del alto. Es instantáneo, como `Centrar`, y no cambia el zoom, el giro ni la inclinación.
- Los textos nuevos van por Localization (`ClavesTexto` + `PrepararInterfaz.TextosEs`).

**Never:** sin TalkBack de la ficha (parte 3); sin variantes reales en los datos; sin animar el centrado; sin cambiar el resto de la ficha de la parte 1.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Vigente | Concepto sintético "Pista de pádel · 20 €" con la variante "De 20:00 a 24:00 · 24 €"; hora local 21:00 | Variante bajo su concepto, con el chip "Ahora" | N/A |
| No vigente | Lo mismo a las 10:00 | Variante sin el chip | N/A |
| Medianoche | "De 22:00 a 02:00"; 01:00 | Vigente | N/A |
| Días | `EnDias(lunes a viernes)` | "Lun a vie" | N/A |
| Días sueltos | `EnDias(lunes, martes, viernes)` | "Lun, mar y vie" | N/A |
| Cliente | `DeCliente("con-bono")` · desde 15 € | "Con bono · desde 15 €", nunca destacada | N/A |
| Centrado | Abrir la ficha de Clínica Areas | `Proyectar(lugar)` cae en (ancho/2, top de la hoja/2), con ±2 px | N/A |

</frozen-after-approval>

## Code Map

- **Dominio** (no cambia):
  - `Variante` (`Id`, `Condicion`, `Fuentes`).
  - `CondicionVariante`: `Horario`, `EnDias`, `DeCliente`, `SeCumple`. `Desde`/`Hasta` son `TimeSpan`, `Dias` es `DayOfWeek[]` y lleva `TipoCliente`.
  - `ReglaVariantes.Vigente(IReadOnlyList<Variante>, DateTime)` devuelve la primera que se cumple, o null.
  - `Concepto.Variantes` nunca es null.
- `Scripts/UI/FilaConcepto.cs`: variantes debajo (nueva `FilaVariante` si conviene) y el texto de condición en un método estático que se pueda probar.
- `Scripts/UI/FichaLugar.cs`: `Abrir(lugar, ahora)` recibe UTC y calcula la vigente con `ahora.ToLocalTime()`. `Hoja` ya está expuesta.
- **Centrado:**
  - `Scripts/Aplicacion/IMapa.cs`: nuevo `void Centrar(Coordenada c, PuntoPantalla enPantalla)`, que deja `c` en ese punto (px, origen arriba a la izquierda).
  - `Scripts/Vectorial/CamaraMapa.cs`: nuevo `CentrarEn(lat, lon, Vector2 objetivo)`. Hace `CentrarEn`, después `PuntoSuelo` en el objetivo y en el centro de pantalla (Y desde abajo), `pivote += centro - objetivo` y `Aplicar()`. `PuntoSuelo` (l.113) es privado: reutilizarlo. Así funciona también con el mapa inclinado.
  - También hay que actualizar `MapaVectorial.Centrar` (l.189) y los seis `MapaFalso` de `Tests/EditMode`.
- `Scripts/UI/PantallaMapa.cs`: `AbrirFicha` (l.316-321) centra después de `ficha.Abrir`. Si la hoja aún no tiene `worldBound`, centra con el 72 % y vuelve a centrar en el primer `GeometryChangedEvent` de la hoja. Pasar de dp a px con `EscalaDp`.
- **Textos:** `ficha.ahora` y las condiciones: "De {0} a {1}", "{0} a {1}", "{0} y {1}", los días abreviados y "Con bono".
- **Maqueta:** `_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/mockups/key-ficha-lugar.html:50-51`. `.variant` es 13 px, color muted y el precio en color texto. `.now` es un chip con fondo `surface2`, texto `accent` y peso 800.
- `Assets/UI/PantallaMapa.uss`: estilos de la variante y del chip.
- `Scripts/Arranque/CapturaPruebas.cs`: la captura "ficha" ya abre Clínica Areas.

## Tasks & Acceptance

**Execution:**
- [x] `FilaConcepto.cs` (+ `FilaVariante.cs`), `ClavesTexto.cs`, `PrepararInterfaz.cs` y `PantallaMapa.uss`: variantes con condición, precio y la vigente destacada. Regenerar la tabla con `PrepararInterfaz.Asegurar` por lotes.
- [x] `IMapa.cs`, `CamaraMapa.cs`, `MapaVectorial.cs` y los `MapaFalso`: centrar en un punto de pantalla.
- [x] `PantallaMapa.cs` y `FichaLugar.cs`: centrar al abrir y usar la hora local para la vigente.
- [x] `CapturaPruebas.cs`: la captura "ficha" comprueba el centrado y una nueva captura "variantes" abre un lugar sintético con `AbrirFicha`.
- [x] `Tests/EditMode/PruebasFicha.cs`: una prueba por fila de la matriz. El centrado se prueba con `CamaraMapa` real en `PruebasMapaVectorial.cs`, y con un `MapaFalso` se prueba que `AbrirFicha` llama a `Centrar` con el punto correcto.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas, incluida una prueba por fila de la matriz.
- Dada la build de PC, cuando se abren `ficha.png` y `variantes.png`, entonces Clínica Areas se ve encima de la hoja, la variante vigente va destacada y el log no tiene errores.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | Con la inclinación mínima (20°), el punto objetivo (14 % desde arriba) queda sobre el horizonte; `PuntoSuelo` falla y el lugar se queda en el centro, debajo de la hoja (ECH, BH) | medium | `inclinacionMin = 20` desde la horizontal. Con el campo de visión de la cámara, el rayo al 14 % sube por encima del horizonte. `CentrarEn` hace `return` sin avisar. | patch |
| 2 | El chip "Ahora" se elige entre todas las variantes, también las ocultas: si la primera que se cumple no se muestra, no hay chip (ECH, BH) | low | `FichaLugar` llama a `Vigente(c.Variantes)` y `FilaConcepto` filtra después. Se corrige moviendo la elección a las variantes mostradas. | patch |
| 3 | Quitar `ToLocalTime()` no rompe ninguna prueba (VG) | medium | Las horas de las pruebas caen lejos de los bordes de las franjas, así que UTC y la hora local dan lo mismo. | patch |
| 4 | Ocultar variantes sin importe ni "Consultar" no tiene prueba (VG, BH) | low | Todas las variantes de prueba tienen precio fijo o "desde". | patch |
| 5 | No hay prueba del recentrado tras la medida real de la hoja ni al pasar de una ficha a otra (VG, BH) | medium | UI Toolkit no hace el layout en EditMode; hoy solo lo comprueba la captura de PC. | defer |
| 6 | `centrarPendiente` puede quedarse puesto y recentrar más tarde (VG, ECH, BH) | low | Con la ficha abierta, el mapa no se puede mover (`TapaPunto` da true y el velo tapa los botones), y al cerrar se borra: recentrar sobre el mismo lugar no deshace nada del usuario. | rechazado |
| 7 | Cerrar y reabrir en el mismo fotograma usa la medida vieja (ECH) | low | Ningún camino reabre en el mismo fotograma: el toque que cierra no abre. | rechazado |
| 8 | Concepto sin precio propio pero con variantes con precio desaparece (BH) | low | No hay variantes en los datos; el Dominio calcula el precio del concepto con sus propias fuentes. | rechazado |
| 9 | `TipoCliente.Trim()` sin comprobar null (BH) | false | `DeCliente` exige un tipo no vacío y el constructor pone `""` si es null. | rechazado |
| 10 | Horario de todo el día ("De 10:00 a 10:00"), días que dan la vuelta a la semana y otros tipos de cliente sin traducir (ECH, BH) | low | No hay variantes en los datos; arreglarlo añade ramas y claves. | rechazado |
| 11 | `Screen.width/height` frente a `cam.pixelWidth/Height` (BH) | false | La cámara del mapa ocupa toda la pantalla y el panel empieza en el origen; la zona segura solo mueve los controles. | rechazado |
| 12 | Las variantes no muestran su unidad, su origen ni su frescura (BH) | low | El spec pide solo condición y precio, como la maqueta. | rechazado |
| 13 | `--color-ahora-texto` copia valores en vez de usar `var()` (BH) | low | Lo vigila la prueba de contraste; cambiarlo no aporta nada visible. | rechazado |
| 14 | El separador "·" y el formato de las horas no van por Localization (ECH, BH) | low | El "·" ya se usa como literal en la parte 1; es un signo, no un texto. | rechazado |
| 15 | Spec y `CLAUDE.md` desactualizados (siete `MapaFalso`, faltaba `PruebasFicha`, recuentos de líneas) (BH) | low | `CLAUDE.md` se pone al día a mano al cerrar la historia; el spec no se cambia. | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC (`-buildTarget Win64`) y `PriceMap.exe -captura <carpeta>`: `ficha.png`, `variantes.png` y log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.
