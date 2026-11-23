---
title: 'Ir a una dirección buscada: ficha con minutos e Ir'
type: 'feature'
created: '2026-11-22'
status: 'done'
baseline_commit: '07c2137bd8be1c87b85903e7c877a3905d7eb3f9'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Al elegir una dirección en el buscador («calle ancha»), el mapa se centra en ella y la hoja se pliega, pero no hay forma de ir allí (prueba del usuario en el móvil, 22/11/2026).

**Approach:** Elegir una dirección abre la ficha de punto libre (historia 2.5) en su coordenada, ya con la dirección de Photon de título y la localidad debajo, con los minutos en coche y a pie e «Ir», como al mantener pulsado el mapa.

## Boundaries & Constraints

**Always:**
- Decidido (usuario): los puntos del mapa de OpenStreetMap que salen en los resultados (`TipoResultado` de punto de interés: bancos, farmacias…) también abren la ficha de punto libre: título con su nombre, debajo su categoría (texto de la categoría como en el resultado), minutos e Ir, sin petición a `/reverse`.
- La ficha se abre con la dirección del resultado: no se pide otra vez a Photon (`/reverse`) ni sale «Buscando la dirección…».
- Título y localidad con las mismas reglas que `FichaLugar.PonerDireccionPunto` (calle con número si lo hay; sin calle, el nombre del resultado).
- Decidido (usuario, 23/11/2026): si el resultado tiene nombre propio además de calle («Hotel Guadalquivir», en «Avenida Calzada, 20»), el título es el nombre y debajo van calle, número y localidad («Avenida Calzada, 20, Sanlúcar de Barrameda»); también es el nombre de la ruta. Una calle buscada abre su ficha con Ir a esa calle.
- Lo demás como la ficha de punto libre de hoy: chincheta, centrado, minutos, Ir, cerrar con atrás/X/mapa, TalkBack modal.
- El texto del buscador y sus resultados se conservan al cerrar la ficha (como al abrir un lugar desde el buscador).

**Never:** cambiar los resultados de lugares ni «Ir a este punto» (coordenadas pegadas); cambiar cómo busca Photon.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Dirección con calle | Elegir «Calle Ancha, 10, Sanlúcar» | Ficha: «Calle Ancha, 10» / «Sanlúcar de Barrameda», minutos e Ir; sin petición a `/reverse` |
| Dirección sin calle | Elegir un resultado solo con nombre (p. ej. una plaza) | Título con el nombre del resultado |
| Sitio con nombre y calle | Elegir «Hotel Guadalquivir» | Título «Hotel Guadalquivir»; debajo «Avenida Calzada, 20, Sanlúcar de Barrameda»; la ruta lleva el nombre |
| Ir | En esa ficha, Ir | Ruta al punto, como desde un punto libre |
| Punto del mapa | Elegir «Caixabank» (banco de OSM) | Ficha: «Caixabank» / su categoría, minutos e Ir |
| Sin red | Minutos sin conexión | Igual que el punto libre (ruta aproximada) |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `Scripts/UI/PantallaMapa.cs` 963-970 (`ElegirResultado`: `Lugar` → `AbrirFicha`, `Coordenada` → `AbrirPunto`; el resto solo `mapa.Centrar` + `PlegarBuscador`), 1339-1362 (`AbrirPunto`: `ficha.AbrirPunto(punto, buscando)`, `PedirTiempos`, centrado, chincheta, TalkBack y `PedirDireccionPunto`). Hace falta una variante de `AbrirPunto` con la dirección ya conocida (sin petición).
- `Scripts/UI/FichaLugar.cs` 219-262 (`AbrirPunto`, `PonerDireccionPunto`, `TituloDireccion`, `PonerLineaDireccion`).
- `Scripts/Aplicacion/ResultadoBusqueda.cs` (`Tipo`, `Nombre`, `Posicion`, `Direccion`, `Punto`, `Categoria`).
- La ruta a un punto libre ya existe (`puntoRuta`, `nombreRuta`, fila del destino de la hoja de la ruta con `PonerDestinoRuta`): el nombre de la fila debe ser la dirección.
- Pruebas: `Tests/EditMode/PruebasBusqueda.cs`/`PruebasDirecciones.cs`/`PruebasPuntoLibre.cs` (resultados y punto libre).

- `PantallaMapa.PonerDestinoRuta` (1754): con punto libre usa `ficha.Titulo` y `ficha.Direccion`; con un punto del mapa el detalle debe ser su categoría. `ReabrirFichaRuta` (≈2140) llama a `AbrirPunto(puntoRuta)`: con la dirección conocida hay que reabrir con ella (sin `/reverse`), así que se recuerda lo mostrado junto a `puntoRuta`; `QuitarRuta` lo borra.
- Elecciones propias (no visibles como duda): en la ficha de un punto del mapa la categoría (con mayúscula, la de `FilaResultado.TipoDe`, «Centro sanitario público» en la sanidad pública) sustituye a «Punto en el mapa» en `Tipo`, con la gotita; sin línea de dirección. Si el texto de la categoría es vacío, se queda «Punto en el mapa». `FichaLugar.PonerDireccionPunto` descarta la localidad cuando no hay calle: con dirección conocida la localidad sale siempre que exista y no repita el título.
- La lista «Encuentra cerca» usa el mismo `ElegirResultado`: sus puntos también abren la ficha. Tres pruebas fijan lo antiguo («CentraElMapaYPliegaLaHoja» en `PruebasBusqueda` 429, `PruebasCerca` 559 y `PruebasDirecciones` 559) y pasan a la conducta nueva.

## Tasks & Acceptance

**Execution:**
- [x] `Scripts/UI/PantallaMapa.cs`, `Scripts/UI/FichaLugar.cs` -- abrir la ficha de punto con la dirección conocida desde `ElegirResultado`.
- [x] `Tests/EditMode` -- una prueba por fila de la matriz (con geocodificador falso que cuenta las llamadas a `Inverso`).
- [x] `Scripts/Arranque/CapturaPruebas.cs` -- captura `direccion-ficha` y `direccion-ficha-oscuro`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.

## Implementation Notes

## Spec Change Log

- 23/11/2026: hallazgo 1 de la revisión (nombre propio con calle). El usuario decide el título (nombre) y la línea de debajo (calle, número y localidad); se añade al bloque congelado y a la matriz. Se conserva el resto del código (KEEP: `Conocido`, `conocidoRuta`, sin `/reverse`).

## Review Triage Log

Revisión 1 (23/11/2026). BH = Blind Hunter, EC = Edge Case Hunter, VG = Verification Gap.

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | (BH, EC, VG) Un resultado con nombre y calle pierde su nombre en la ficha y la ruta | medium | `FichaLugar.AbrirPunto`: calle antes que nombre; caso real en Photon | intent_gap: decidido por el usuario (frozen); el arreglo cabe en el diff, sin rehacer el código |
| 2 | (VG) Ninguna prueba lo fija | gap | Nombre y calle coinciden en todas | patch: prueba, con Ir |
| 3 | (BH) TalkBack dice «Ir a este punto» | false | La intención dice TalkBack como la ficha de punto libre | rechazado |
| 4 | (BH, EC) Al cerrar la ficha, TalkBack no vuelve al buscador | medium | `CerrarFicha` acaba en `PedirFoco(null)`; antes `PlegarBuscador` lo llevaba a la cápsula | patch |
| 5 | (BH) Reglas distintas de `PonerDireccionPunto` | false | Decidido en el Code Map; el camino de `/reverse` no cambia | rechazado |
| 6 | (BH) Un punto libre en la misma coordenada reescribe la fila del destino | low | Pide igualdad exacta de coordenadas | rechazado |
| 7 | (BH) La prueba «no hereda» no cubre `conocidoRuta = null` en `QuitarRuta` | low | Sin efecto visible: `Ir` siempre lo reasigna y reabrir pide ruta activa | rechazado |
| 8 | (BH) `ElegirResultado` y `AbrirPunto(ResultadoBusqueda)` admiten cualquier tipo | low | Solo hay cuatro tipos y los otros dos salen antes | rechazado |
| 9 | (BH) La captura depende de Photon | low | El resultado elegido es fijo | rechazado |
| 10 | (BH) Sin captura de un punto del mapa | low | Solo se registra en el log | patch: captura `punto-ficha` |
| 11 | (BH) Comprobación de categoría circular en `PruebasCerca` | low | `PruebasBusqueda` y `PruebasRutas` usan el texto literal | rechazado |
| 12 | (BH) Fila del destino sin localidad, sin prueba | low | `PonerDestinoRuta` | patch: en la prueba de la localidad vacía |
| 13 | (BH) Sin prueba de Ir sin GPS, punto sin red y nombre vacío | low | Caminos comunes ya probados con el punto libre | rechazado |
| 14 | (BH) Comentario de `TocarMapa` anticuado | low | «su ficha es de la épica 2» | patch |
| 15 | (BH) Memoria y `CLAUDE.md` | low | Archivos de contexto | defer: al cerrar |
| 16 | (EC) Un resultado de Photon que es un lugar de la app abre una ficha sin precios | low | Las direcciones no se comparan con los lugares; ya pasaba antes (solo centraba) y el lugar sale encima en la lista | defer |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` (con `-buildTarget Win64`) y `PriceMap.exe -captura` -- capturas nuevas; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.
