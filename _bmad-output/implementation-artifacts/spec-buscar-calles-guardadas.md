---
title: 'Buscar en las calles guardadas del mapa'
type: 'feature'
created: '2026-11-23'
status: 'done'
baseline_commit: '9ca00c9ece4a46e85dceb0054f9b180abe6201b6'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Photon no encuentra calles de Sanlúcar que sí están en el mapa («calle fariñas», «calle ancha 10»), y sin conexión el buscador no da ninguna calle (prueba del usuario, 23/11/2026).

**Approach:** El buscador busca también en los nombres de calle de las teselas que el mapa ya tiene guardadas en el móvil, sin salir a la red, y enseña esas calles como direcciones normales (misma fila, misma ficha y mismo Ir).

## Boundaries & Constraints

**Always:**
- Decidido (usuario, 23/11/2026): salen siempre al escribir, al momento y también sin conexión, debajo de los lugares y los puntos y encima de las direcciones de Photon.
- Decidido (usuario, 23/11/2026): con número («calle fariñas 12») sale la calle sin el número y lleva al medio de la calle.
- Se comparan los nombres con la regla del buscador (sin tildes, por principio de palabra, varias palabras): «farinas», «c farinas» y «ancha» encuentran.
- Cada calle sale una vez por localidad: sus tramos y teselas se juntan. Tramos del mismo nombre en localidades distintas (Sanlúcar, Chipiona) son filas distintas, con su localidad debajo, como las de Photon.
- La calle lleva a un punto de la propia calle (el medio de su trazado), nunca fuera de ella.
- Leer y preparar las calles no bloquea la pantalla (fuera del hilo principal); una tesela ilegible se salta con un aviso en el registro. Las teselas que se guarden con la app abierta cuentan en las búsquedas siguientes.
- Mínimo 3 caracteres normalizados, como las direcciones. TalkBack cuenta y anuncia estas filas como las demás.
- Sin repetidas: si Photon da la misma calle (mismo nombre normalizado, a menos de 1 km), queda una sola fila.

**Never:** guardar búsquedas (FR17); descargar teselas para buscar; rutas de barco, vías de tren ni calles sin nombre en la lista; inventar números de portal; cambiar cómo pregunta la app a Photon.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Photon no la da | «calle fariñas» | Fila «Calle Fariñas», «Sanlúcar de Barrameda», distancia; abre su ficha e Ir |
| Sin conexión | «ancha», Photon falla | Las calles guardadas, y el aviso de conexión de hoy |
| Repetida | Photon también da «Calle Ancha» | Una sola fila |
| Con número en Photon | Photon da «Calle Ancha 10» | Esa fila y la de la calle |
| Número que Photon no da | «calle fariñas 12» | Fila «Calle Fariñas», al medio de la calle |
| Abreviatura | «c/ fariñas», «avda calzada» | Las encuentra como «calle» y «avenida» |
| Texto corto | «fa» | Ninguna calle guardada |
| Sin teselas | Primera apertura sin red | Como hoy |
| Texto cambia | La búsqueda vieja acaba tarde | No se pinta |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- Capa `transportation_name` de las teselas (14/x/y.pbf en `persistentDataPath/vectorial`): atributos `name` y `class`; 59 teselas en el PC, unos 1.270 nombres, «Calle Fariñas» en dos. Clases fuera: `ferry`, `rail`/`transit`, `raceway`. Capa `place`: `city/town/village` con nombre (Sanlúcar, Chipiona) para la localidad de la más cercana.
- `Scripts/Vectorial/RutasCallesGuardadas.cs`: patrón a seguir (carpeta leída en el hilo principal, `LectorMVT.Leer`, `ACoordenada`, `Task.Run`, teselas ilegibles). Las geometrías se recortan con margen: un tramo puede repetirse en dos teselas.
- `Scripts/Aplicacion/IGeocodificador.cs` (`Direccion`), `BuscadorDirecciones` (`BastaParaPedir`, `MaximoDirecciones`, espera y cancelación), `ResultadoBusqueda.DeDireccion`, `Dominio/ReglaBusqueda`.
- `Scripts/UI/PantallaMapa.cs`: `Buscar`, `PedirDirecciones`, `AlCambiarDirecciones`, `PintarResultados`, `Juntar`, `BuscarComoDireccion`; `HojaBuscador.MostrarResultados` (locales, direcciones, aviso).
- `Scripts/Arranque/Arranque.cs` (`Start`, junto a `callesGuardadas`) y `Componer`.
- Pruebas: `PruebasDirecciones`, `PruebasRutaAproximada` (constructor de teselas MVT de prueba). Captura `busqueda-direccion` en `CapturaPruebas`.

## Tasks & Acceptance

**Execution:**
- [x] `Scripts/Aplicacion` -- puerto de calles guardadas y regla pura de juntar/ordenar/quitar repetidas y quitar número y abreviaturas -- sin Unity, probable.
- [x] `Scripts/Vectorial` -- adaptador que lee e indexa las teselas guardadas (en otro hilo, al día con las nuevas) -- sin red.
- [x] `BuscadorDirecciones`/`PantallaMapa`/`Arranque` -- enseñarlas según lo decidido (también «No hay resultados» y «Buscar como dirección» con solo calles), con TalkBack.
- [x] `Tests/EditMode` -- una prueba por fila de la matriz y captura de PC con una calle guardada.

**Acceptance Criteria:**
- Given las teselas de Sanlúcar guardadas en el PC, when se busca «calle farinas» sin red, then sale «Calle Fariñas» de Sanlúcar y su punto está en esa calle.
- Given la suite EditMode, when se ejecuta, then pasan todas.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Revisión del 23/11/2026 (BH = ciega, EC = casos límite, VG = huecos de verificación).

| Hallazgo | Veredicto | Ruta y prueba |
|----------|-----------|---------------|
| BH1, VG, EC13: «No hay resultados» mientras se buscan las calles | medium | patch |
| BH2, VG: las filas de calles que siguen se rehacen | medium | patch: se reutiliza el resultado visible |
| BH3: un cambio de orden destruye filas que siguen | low | patch: se mueve la fila |
| BH10, VG, EC9: segmentos repetidos en el trazado | medium | patch: una arista por pareja |
| BH11, EC2, EC3, EC4: números sin quitar («nº12», «12 bajo», «nº») | medium | patch |
| BH13, EC5: con la localidad escrita no sale la calle | medium | patch: casa también con la localidad |
| EC12: longitud dañada en la tesela, bucle sin fin con el candado | medium | patch: se rechaza |
| BH15, EC10: comentarios que dicen de más («no se descifran», «aldeas») | low | patch: comentarios; las pedanías van con su pueblo, como decía el Code Map |
| VG1: vuelta al hilo principal sin probar | medium | patch: prueba |
| VG2: zoom distinto de 14 sin probar | medium | patch: prueba |
| VG3: solo se prueba «town» | medium | patch: prueba |
| VG4, BH17: tesela que no se puede abrir, borrada o de otra extensión, sin probar | medium | patch: pruebas |
| VG5: el desplazamiento de la lista al llegar calles, sin probar | low | defer: pide una hoja con panel; las direcciones tienen el mismo hueco |
| BH4, BH5, BH6, BH16, EC11: coste, memoria e índice por teselas | low | rechazado: 59 teselas en 20 ms la primera vez y 2–7 ms después, en otro hilo |
| BH7: error al listar la carpeta | low | rechazado: raro, y la búsqueda siguiente lo repite |
| BH8, EC6: localidad lejana si falta la tesela del rótulo | low | rechazado: el rótulo de Sanlúcar está en las nueve teselas del casco |
| BH9, EC7: dos calles del mismo nombre en una localidad | false | El spec lo decide: una fila por localidad |
| BH12: «calle de 12» deja «calle de» | low | rechazado: texto poco probable |
| BH14, EC8: la regla de 1 km de las repetidas | false | El spec la fija así |
| EC1: calle con número en el nombre y además portal | low | rechazado: poco probable, y el nombre completo sin portal sí la encuentra |
| EC14, EC15: calles que llegan después de Photon (foco, filas que bajan) | low | rechazado: las calles tardan milisegundos y Photon espera 350 ms |
| BH17: criterio con teselas reales solo en este PC | low | rechazado: las teselas de prueba cubren la lógica |

Sin intent_gap ni bad_spec.

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` (con `-buildTarget Win64`) y `PriceMap.exe -captura`; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.
