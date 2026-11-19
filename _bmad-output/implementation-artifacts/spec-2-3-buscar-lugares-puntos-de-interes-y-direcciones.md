---
title: 'Historia 2.3 (parte 1): buscar lugares y puntos de interés'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '1f0461777b87e7a933e638e15288dc0d1588e3b2'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La hoja del buscador (2.2) todavía no busca. El usuario necesita encontrar lugares de la app y puntos de interés (FR14).

**Approach:** Un `ServicioBusqueda` (Aplicación) busca en los lugares de la app (`ServicioLugares`) y en los puntos de interés de las teselas cargadas (`IMapa.PuntosInteres`) y los ordena por coincidencia y cercanía. Los resultados se ven en la hoja. Tocar un lugar abre su ficha; tocar un punto de interés centra el mapa en él (la ficha de punto libre llega en la 2.5).

**Decisión del 19/11/2026:** la historia se parte en dos. Las direcciones con Photon van en la parte 2.

## Boundaries & Constraints

**Always:**
- **Normalización** (pura, en Dominio `ReglaBusqueda`): minúsculas, sin tildes y sin signos.
  - Coincide si cada palabra de la consulta es el principio de alguna palabra del nombre (o de la dirección del lugar).
  - Puntuación: nombre exacto > empieza igual > contiene.
- **Orden:** primero los lugares de la app y luego los puntos de interés. Dentro de cada grupo, por puntuación y después por distancia a la posición del usuario (o al centro del mapa si no la hay). Como mucho 20 resultados. Se busca a cada tecla, porque todo es local.
- Se quitan los puntos de interés que repiten un lugar de la app (misma regla de repetidos que la 1.6), y también los de categoría `Ninguna`.
- **Fila de resultado:** icono del tipo; el nombre; una segunda línea con el tipo y la distancia («350 m», «1,2 km»); y el precio resumido si lo tiene.
- **Sin resultados:** «No hay resultados».
- **Decisión del 19/11/2026:** plegar la hoja (atrás, abrir una ficha o arrastrarla abajo) no borra el texto ni los resultados. Al volver a abrirla siguen ahí. Una «X» en el campo, visible cuando hay texto, lo vacía y vuelve a mostrar las categorías.
- **TalkBack:**
  - Cada resultado es un botón: «Nombre, tipo, a 350 m, desde 20 €».
  - Al cambiar los resultados se anuncia «N resultados» (o «No hay resultados»).
  - La X es un botón «Borrar búsqueda».

**Never:** nada de direcciones ni de Photon (parte 2); sin historial de búsquedas (FR17); sin autocompletar de Google.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Lugar | "clinica ar" | Clínica Areas el primero |
| Sin tildes | "jose" | Encuentra "José …" |
| Punto de interés | "mercadona" con la tesela cargada | Mercadona con categoría supermercado |
| Repetido | Punto de interés con el nombre de un lugar a < 80 m | Solo el lugar |
| Sin resultados | "zzzz" | «No hay resultados» |
| Toque en lugar | Resultado Clínica Areas | Mapa centrado y ficha abierta |
| Toque en punto | Resultado Mercadona | Mapa centrado en él y hoja plegada |
| Plegar y volver | "clinica", atrás y abrir | Sigue "clinica" con sus resultados |
| Borrar | Toque en la X | Campo vacío y categorías |
| Máximo | Consulta que coincide con más de 20 | 20 resultados |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts`.
- **Dominio** — nuevo `ReglaBusqueda.cs`:
  - `Normalizar` reutiliza `ReglaPuntosInteres.NombreNormalizado` (l.99-119).
  - `Puntuar(consulta, nombre, direccion)` devuelve 0 si no coincide, 3 exacto, 2 empieza igual, 1 contiene.
  - La distancia es `Coordenada.DistanciaMetros` (`Coordenada.cs:25`).
- **Aplicacion**:
  - Nuevo `ResultadoBusqueda.cs`: tipo (Lugar/Punto), nombre, `Posicion`, `Lugar` o `PuntoInteres`, `CategoriaPoi`, metros y `ValorPrecio?`.
  - Nuevo `ServicioBusqueda.cs`: `Buscar(string consulta, Coordenada cerca)` → `IReadOnlyList<ResultadoBusqueda>`.
  - Fuentes: `ServicioLugares.Lugares` (l.19) y `.Resumen(id, ahora).Valor.PrecioAnunciado` (l.48); `IMapa.PuntosInteres` (`IMapa.cs:42`) con `ReglaPuntosInteres.Categoria` (l.69) y `EsRepetido` (l.126).
  - El punto de cercanía es `ServicioUbicacion.Posicion` o, si no hay, `IMapa.Centro` (l.57).
- **UI** — `HojaBuscador.cs`:
  - Lista de resultados dentro del `Panel` (l.144-151). Los chips y `Cerca` (l.153-178) se ocultan mientras hay texto.
  - Botón X en `CajaCampo`.
  - Quitar el vaciado del campo en `Plegar()` (l.294) y en la suelta plegada (l.355).
  - En cada fila: el icono con `IconosLugar.GlifoDe(lugar.Tipo)` (l.122) o `GlifoDe(CategoriaPoi)` (l.82); el tipo con `lugar.Tipo` o `TextosPuntos.ClaveCategoria` (l.13); el precio con `FormatoPrecio.Precio` (l.43).
  - La distancia, con un formateador nuevo (`FormatoPrecio` o `FormatoDistancia`): «350 m» redondeado a 10 m y «1,2 km» a partir de 1.000 m.
- **UI** — `PantallaMapa.cs`:
  - El cambio de valor del `Campo` llama a `ServicioBusqueda.Buscar` y pinta la lista.
  - Tocar un lugar llama a `AbrirFicha(lugar)` (l.530), que ya centra y pliega. Tocar un punto llama a `mapa.Centrar(pos)` y pliega.
  - TalkBack: las filas son botones en `DeclararBuscadorEnTalkBack` (l.380), con un grupo que se da de baja al rehacer la lista, como la ficha. Los anuncios van con `AccesibilidadPantalla.Anunciar`.
  - `Inicializar` (l.57 y 67) gana un `ServicioBusqueda` opcional.
- **Arranque** — `Arranque.cs`: `Start` (l.23-53) y `Componer` (l.84) crean `ServicioBusqueda`.
- **Captura** — `CapturaPruebas`: un `Busqueda()` después de `Buscador()` (l.157) que escribe "clinica" y guarda "busqueda" (en claro y en oscuro).
- **Textos** — «No hay resultados», «N resultados» (plural), «Borrar búsqueda» y la plantilla de TalkBack de la fila.
- **Pruebas** — nuevo `Tests/EditMode/PruebasBusqueda.cs` (regla, servicio con un mapa falso y la UI de la hoja), con el patrón `Crear`/`Limpiar` de `PruebasBuscador`.
- **Proyección de los puntos:** la 2.5 los convertirá en ficha; ahora solo centran.

## Tasks & Acceptance

**Execution:**
- [x] `ReglaBusqueda` y pruebas.
- [x] `ResultadoBusqueda` y `ServicioBusqueda` (mezcla, orden, repetidos, máximo) y pruebas.
- [x] `HojaBuscador` + `PantallaMapa`: lista, «No hay resultados», X, mantener el texto, toque en resultado, TalkBack.
- [x] `Arranque`, textos y captura "busqueda".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se busca "clinica", entonces el log no tiene errores y la captura "busqueda" muestra los resultados.

## Spec Change Log

## Review Triage Log

Revisión del 19/11 (tres capas: blind, edge-case, verification-gap).

| # | Hallazgo | Veredicto | Ruta | Evidencia |
|---|----------|-----------|------|-----------|
| 1 | Al volver a desplegar, los resultados guardados no se rehacen: distancias y puntos viejos (blind, edge) | medium | patch | `AlDesplegarBuscador` no buscaba; ahora rehace `Buscar(Campo.value)` si hay búsqueda. Prueba `AlVolverADesplegarLaBusquedaSeRehaceConLaNuevaCercania`. |
| 2 | La X en la cápsula plegada también abre la hoja con el teclado (blind, vg) | medium | patch | `AlBajar` solo excluía `Avatar`; ahora también `Borrar`. |
| 3 | Filas nuevas sin escala de fuente si la lista mide lo mismo (vg) | medium | patch | `GeometryChangedEvent` no se propaga y solo se escuchaba en `Resultados`; ahora cada fila se escala al medirse. |
| 4 | Ninguna prueba pasa por el aviso real del campo (vg) | medium | patch | Todo usaba `Escribir` (`SetValueWithoutNotify`); la captura ahora pone `Campo.value`. X y filas con toque real: en el móvil. |
| 5 | La rama "posición del usuario" de `PuntoCercania` sin probar (blind, vg) | medium | patch | Prueba nueva `ConPosicionLaBusquedaMideDesdeElUsuarioYNoDesdeElCentro` en `PruebasUbicacion`. |
| 6 | El anuncio "N resultados" se pierde si los resultados cambian sin la hoja modal (edge) | low | patch | `ultimosResultados` se actualizaba sin anunciar; ahora solo cuenta como anunciado con la hoja modal. |
| 7 | La comprobación de la captura "plegar mantiene la búsqueda" no podía fallar (edge) | low | patch | `Filas` es la lista viva; se guarda `filasAntes`. |
| 8 | Arrastrar la lista puede elegir un resultado sin querer (blind) | maybe-false (medium si es cierto) | defer | Depende de si el `Clickable` se cancela al desplazar; se ve en el móvil. |
| 9 | TalkBack anuncia y reenfoca a cada tecla (blind) | maybe-false (medium si es cierto) | defer | Solo anuncia si cambian los resultados, pero falta probarlo con TalkBack real. |
| 10 | Sin pruebas de contraste para los iconos nuevos (blind) | maybe-false (medium si es cierto) | defer | Habría que medir glifo/fondo de los 22 colores en claro y oscuro. |
| 11 | Los resultados no siguen el filtro de precios (blind) | false | — | La intención no lo pide y tocar el lugar abre su ficha y lo centra igual. |
| 12 | Elegir un punto no lo marca en el mapa (blind) | low | rechazado | La ficha del punto llega en la 2.5 (Design Notes). |
| 13 | `Contiene` es "empieza palabra", no subcadena (blind) | low | rechazado | Elección documentada en `ReglaBusqueda`; nadie busca a mitad de palabra. |
| 14 | `FormatoDistancia` fuera de Localization y sin infinito (blind, edge) | false / low | rechazado | La distancia entre dos coordenadas nunca es infinita; la app solo tiene es-ES. |
| 15 | El precio usa el `ahora` del arranque (blind) | low | rechazado | Mismo criterio que marcadores y ficha (AD-12). |
| 16 | Puntos con Id 0 repetidos en dos teselas (edge) | maybe-false (low) | rechazado | Solo sería una fila de más; no hay pruebas de que pase. |
| 17 | `Buscar(texto)` con un texto distinto del campo (edge) | false | — | Solo se llama con `Campo.value` (`TextoCambiado` y al desplegar). |
| 18 | Change log del spec vacío y parte 2 sin línea propia en el sprint (blind) | low | rechazado | El arreglo es editar el spec; la parte 2 está en `deferred-work`. |

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "busqueda"; limpieza de `link.xml` y `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: teclear, la lista, tocar un resultado y TalkBack.
