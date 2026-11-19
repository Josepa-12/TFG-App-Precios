---
title: 'Historia 2.4 (parte 2): mapa filtrado por categoría'
type: 'feature'
created: '2026-11-19'
status: 'done'
baseline_commit: '23260458e50185a0a29ec22f82097d83af1008b0'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Al elegir una categoría (parte 1) solo cambia la lista; el mapa sigue enseñando todo (FR15: «el mapa y una lista por distancia muestran solo esa categoría»).

**Approach:** Mientras hay una categoría activa, el mapa enseña solo sus marcadores y puntos, y la píldora de arriba lo indica y permite quitarla.

**Decisiones del 19/11/2026:**
- La píldora «Ver precios» se convierte en «Salud ✕», con el icono de la categoría; tocarla quita la categoría.
- «Encuentra cerca» (todas) no filtra el mapa ni cambia la píldora.

## Boundaries & Constraints

**Always:**
- **Qué se ve:** con la categoría C activa, marcadores de los lugares con `ReglaCerca.Incluye(C, lugar)` y puntos de interés con `ReglaCerca.Incluye(C, categoriaPoi)`. El resto se oculta. Se ignora el filtro «Precios», y en el mapa no hay radio de 3 km. Las reglas de solapes y repetidos de la 1.5/1.6 siguen igual.
- **Cuándo:** la categoría está activa desde que se toca su chip hasta que se quita: X de la cabecera, botón atrás con la lista, escribir en el campo o tocar la píldora. Sigue activa si la hoja se pliega (arrastrándola o al tocar un punto de la lista).
- **Píldora con categoría:** icono de la categoría (el mismo glifo y color del chip), su nombre y una ✕ dibujada (no emoji) en lugar de la flecha.
  - Tocarla quita la categoría: vuelve «Precios» con el filtro guardado (que no cambia ni se pierde) y, si la hoja enseña la lista, vuelven las categorías.
  - No abre el menú de precios.
- **TalkBack:** la píldora con categoría es un botón «Quitar categoría Salud». Al quitarla se anuncia el filtro que vuelve, como al elegir un filtro.
- **Tema:** claro y oscuro, como la píldora de hoy.

**Never:** cambiar ni guardar el filtro «Precios» por una categoría; filtrar el mapa con «Encuentra cerca»; TalkBack de la lista (otra parte).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Salud | Filtro «Hostelería» y chip Salud | Solo marcadores y puntos de salud; píldora «Salud ✕» |
| Plegar | Salud y se pliega la hoja | El mapa sigue con salud |
| Quitar con la píldora | Toque en «Salud ✕» | Vuelve «Hostelería» y su mapa; la lista vuelve a categorías |
| Quitar con la X | X de la cabecera | Igual que arriba |
| Escribir | Salud y se teclea | Vuelve el filtro y la búsqueda |
| Encuentra cerca | Toque en «Encuentra cerca» | Mapa y píldora sin cambios |
| Supermercados | Chip Super | Ningún marcador de la app; solo puntos de super |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts/UI`.
- `CapaMarcadores.cs`: `Filtro` (l.38-42) e `incluido` (l.87). Añadir una `Categoria` (`CategoriaPoi?`) que, cuando no es null, sustituye al filtro: `ReglaCerca.Incluye`. Hay que marcar el cambio, como `filtroCambiado`.
- `CapaPuntosInteres.cs`: en `Actualizar` (l.103-133), con `Categoria` puesta, se ocultan (`Mostrar(false)`) los puntos cuya categoría (`ReglaPuntosInteres.Categoria`, como en l.67) no entra. Esto va antes de los repetidos y los solapes.
- `PildoraPrecios.cs`: un modo categoría, `MostrarCategoria(CategoriaPoi c, string nombre)`, y la vuelta con `Mostrar(filtro)`.
  - El icono, con `IconosLugar.GlifoDe(c)` y la clase de color del chip (`buscador__chip-icono--<nombre>`, en `PantallaMapa.uss`).
  - La ✕, con `IconosLugar.Borrar`, en lugar de `flecha`.
  - Clase `pildora-precios--categoria`.
- `PantallaMapa.cs`:
  - `ElegirCategoria`/`QuitarCategoria` (l.647-663) y `PintarCerca`: con chip (no null), poner `capa.Categoria`, `capaPuntos.Categoria` y la píldora en modo categoría; al quitar, volver a `filtro.Actual`. «Encuentra cerca» (null) no toca nada de esto.
  - La acción de la píldora (la que abre el menú): con categoría activa llama a `QuitarCategoria`.
  - TalkBack de la píldora: su etiqueta, según el modo; el anuncio, el de `AlCambiarFiltro` (l.1009).
  - `QuitarCategoria` ya funciona con la hoja plegada: la lista se rehace al desplegar.
- Textos: «Quitar categoría {0}» (TalkBack), en `ClavesTexto`, `Editor/PrepararInterfaz.cs` y luego `PrepararInterfaz.Asegurar` por lotes.
- `Arranque/CapturaPruebas.cs`: en el paso «cerca», pliega la hoja con Salud y guarda `cerca-mapa.png` (mapa filtrado y píldora «Salud ✕»).
- Pruebas: `Tests/EditMode/PruebasCerca.cs` (pantalla, capas y píldora), una por fila de la matriz. Los falsos ya existen: mapa con puntos, lugares reales y filtro.

## Tasks & Acceptance

**Execution:**
- [x] `CapaMarcadores.cs`, `CapaPuntosInteres.cs`: `Categoria`.
- [x] `PildoraPrecios.cs` + `PantallaMapa.uss`: modo categoría con icono y ✕.
- [x] `PantallaMapa.cs`: activar y quitar, píldora y TalkBack.
- [x] Textos, Localization y captura `cerca-mapa`.
- [x] `PruebasCerca.cs`: filas de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abre `cerca-mapa.png`, entonces solo se ven marcadores y puntos de salud y la píldora dice «Salud ✕»; el registro no tiene errores.

## Spec Change Log

## Review Triage Log

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | Ninguna prueba activa el nodo TalkBack de la píldora (acción real) | low | Las pruebas llaman a `PulsarPildora()` directamente; si `accesibilidad.Boton` volviera a `AlternarMenu`, nada fallaría | patch |
| 2 | No se comprueban `label`/`value` del nodo de la píldora con y sin categoría | low | `Recorrido()` lee etiquetas en vivo y nunca el valor; el valor vacío con categoría no está probado | patch (con 1) |
| 3 | Atrás con la hoja plegada no quita la categoría | false | La intención enumera cuándo se quita: «botón atrás con la lista»; plegada solo la píldora | rechazado |
| 4 | Con zoom lejano el mapa de una categoría sale vacío | false | `DistanciaMaxima` es la regla de la 1.6, que el spec mantiene («Las reglas… siguen igual») | rechazado |
| 5 | `CapaPuntosInteres.Categoria` no marca el cambio | false | `capaPuntos.Actualizar` se llama en cada frame (`PantallaMapa.cs:1360`) sin depender de una marca | rechazado |
| 6 | `PulsarPildora` pide el foco para la píldora con la hoja abierta (fuera del recorrido) | low | Solo con teclado físico y la hoja a media altura; arreglarlo añade una guarda | rechazado |
| 7 | No se anuncia nada al activar la categoría | false | El spec solo pide anunciar al quitarla; el TalkBack de la lista es otra parte | rechazado |
| 8 | `TocarUnPunto…` no comprueba el mapa pintado | low | Solo mira `CategoriaMapa` y la píldora | patch |
| 9 | Sin prueba de cambio de tema en modo categoría | low | `Repintar` solo marca repintado; poco probable que falle | rechazado |
| 10 | `IconoCategoria[0]` frágil | low | Índice de hijo en dos sitios; un campo, como `flecha` | patch |
| 11 | Textos fijos «Salud» en las pruebas | low | En esta suite el texto es el de es-ES; se cambiaría junto con la tabla | rechazado |
| 12 | La captura no comprueba el mapa tras «atrás» y usa `pildora` sin comprobar null | low | Tras `Atras()` no se mira `CategoriaMapa`; l.73 haría `NullReferenceException` si la píldora faltara | patch |
| 13 | Sin prueba de cambiar el filtro con categoría activa | low | Con categoría la píldora no abre el menú; no se llega por la UI | rechazado |
| 14 | CLAUDE.md sin la nueva superficie pública | low | CLAUDE.md ya está modificado en el árbol; ficheros de contexto de agente | rechazado |
| 15 | Píldora pulsada (teclado) con la ficha abierta: la categoría cambia detrás de la ficha | low | `AlternarMenu` cierra la ficha por ese mismo caso (l.1076-1077); `PulsarPildora` no | patch |
| 16 | Categoría con el menú «Precios» abierto | false | Con el menú abierto la hoja está plegada y el velo tapa los chips; el recorrido TalkBack es modal | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- Build de PC y captura; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: elegir una categoría, plegar, tocar «Salud ✕».
