---
title: 'Historia 1.6 (parte 1): puntos de interés en el mapa'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: 'c4ca12fbfee02ccad319b7e9db62740788910ec9'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El mapa solo muestra los lugares de la app. Faltan los sitios reales que sirven para orientarse (supermercados, farmacias, gasolineras, bancos…) y los centros sanitarios públicos, que la 1.4 dejó para aquí (FR-4).

**Approach:**
- El mapa vectorial ya descarga la capa `poi` de OpenFreeMap. Se leen sus puntos y se ofrecen por `IMapa`.
- Una regla del Dominio, portada del prototipo, decide la categoría de cada punto y cuáles se ocultan.
- La UI los dibuja en una capa propia, por debajo de los marcadores de lugar, con un círculo de color, su icono y el nombre.

## Boundaries & Constraints

**Always:**
- **Categorías como el prototipo** (`prototipo/lugares.js:45-94`):
  - `SUB_CAT` manda sobre `CLASS_CAT`.
  - Sin categoría, el punto no se dibuja.
  - Las paradas de autobús (`bus_stop`) y las oficinas (`office`) nunca se dibujan.
- **Sanidad pública:** un punto de categoría `salud` cuyo nombre cumple la regex `PUBLIC_HEALTH` (consultorio, centro de salud, urgencias, hospital, ambulatorio, chare) queda marcado como "Centro sanitario público" y nunca tendrá precio.
- **Aspecto, distinto del marcador de lugar:**
  - Círculo de 24 dp con el color de la categoría (`POI_CATS`, como tokens `--color-poi-*`), borde blanco y glifo blanco vectorial.
  - Nombre a la derecha (`name:es` o `name`) en 12 sp, con el color de la categoría oscurecido en el tema claro y aclarado en el oscuro, y un halo del color de fondo del tema.
  - Los marcadores de lugar siempre quedan por encima.
- **Cuándo se ven:** solo con la cámara a 1200 m o menos (como el zoom 15 del prototipo; al arrancar, a 900 m, ya se ven), solo los de las teselas cargadas y solo los que están dentro de la pantalla.
- **Arquitectura:**
  - `Vector*` y `Camera` no salen de Vectorial ni de UI.
  - El Dominio recibe lat/lon, `class`, `subclass`, nombre y `rank`, y decide.

**Never:**
- **No entra en esta parte, va a la parte 2:**
  - Ocultar el punto que repite un lugar de la app visible.
  - Ocultar los puntos que se pisan entre sí (por `rank`).
  - Leerlos con TalkBack. Ya decidido: sí se leen, después de los lugares.
- Sin tocar un punto: no abre nada (es de la 1.8 y de la épica 2), y los puntos no capturan toques.
- Sin filtro propio de puntos, buscador ni lista "Encuentra cerca".
- No se cambian los marcadores de lugar ni el filtro "Precios".

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Supermercado | `class=grocery, subclass=supermarket, name=Mercadona` | Categoría super, glifo `basket`, nombre "Mercadona" | N/A |
| Subclase manda | `class=fuel, subclass=charging_station` | Categoría carga (`zap`) | N/A |
| Ocultos | `subclass=bus_stop` / `office` | No se dibujan | N/A |
| Sin categoría | `class=xyz` | No se dibuja | N/A |
| Sanidad pública | `class=hospital, name=Centro de Salud Barrio Bajo` | Salud, marcado como sanitario público | N/A |
| Sin nombre | `name` vacío | Solo el icono | N/A |
| `name:es` | `name=X, name:es=Y` | Se ve "Y" | N/A |
| Lejos | Cámara a 2000 m | Ningún punto | N/A |
| Fuera de pantalla | Punto proyectado fuera, más allá del margen | Oculto | N/A |
| Tesela liberada | Se libera la tesela que contiene el punto | El punto desaparece | N/A |

</frozen-after-approval>

## Code Map

- `Scripts/Vectorial/LectorMVT.cs`: `Leer` (`:60`) ya devuelve la capa `poi`. Los puntos llegan en `partes[0][0]` (`:163-182`) y las propiedades con `Texto(...)`/`Numero("rank", 99)` (`:26-28`). **Hay que guardar el id del elemento:** `LeerElemento` (`:132-161`) se salta el campo 1. La parte 2 lo usará para quitar repetidos.
- `Scripts/Vectorial/MapaVectorial.cs`:
  - `Construir` (`:278-352`) recorre las capas. Hay que añadir `poi`: pasar cada punto a lat/lon (lo inverso de `Geo`/`AMundo`, `:282`) y guardarlo en `TeselaCargada` (`:52`).
  - `cargadas` (`:53`), `CargarTesela` (`:227`) y `Liberar` (`:149`): al cargar o liberar, se recompone la lista y se lanza el evento.
- `Scripts/Aplicacion/IMapa.cs`: se añaden `IReadOnlyList<PuntoInteres> PuntosInteres`, `event Action PuntosInteresCambiados` y `float DistanciaCamara`. `PuntoInteres` (id, nombre, clase, subclase, rank, Coordenada) va sin Unity. Los `MapaFalso` de las pruebas (`PruebasArranque`, `PruebasPantallaMapa`, `PruebasMarcadores`, `PruebasFiltro`) implementan los miembros nuevos.
- `Scripts/Dominio/`: `CategoriaPoi` y `ReglaPuntosInteres` (categoría desde clase y subclase, ocultos y sanidad pública), portadas de `prototipo/lugares.js:45-94, 196-202`.
- `Scripts/UI/IconosLugar.cs`: glifos e intérprete (`Dibujar`, `:82`). Faltan `basket`, `pill`, `fuel`, `bank`, `zap`, `bed`, `tree`, `bus`, `church` y `school`, que se copian de `lugares.js:11-41`.
- `Scripts/UI/CapaMarcadores.cs` (`:69-119`): patrón de proyección y visibilidad (margen, `Delante`, `translate`, `DynamicTransform`) para la nueva `CapaPuntosInteres`.
- `UI/PantallaMapa.uxml/.uss` y `Estilos/Tokens.uss`: `capa-puntos` antes de `capa-marcadores` y tokens `--color-poi-*`. El halo se hace con `-unity-text-outline` en el color de fondo del tema.
- `Scripts/UI/PantallaMapa.cs`: crea la capa, se suscribe a `PuntosInteresCambiados` y la actualiza en `LateUpdate`.
- `Scripts/Arranque/CapturaPruebas.cs`: captura "puntos" en el centro de Sanlúcar, con el número de puntos visibles por categoría en el log.

## Tasks & Acceptance

**Execution:**
- [x] `LectorMVT.cs`: guardar el id del elemento.
- [x] `MapaVectorial.cs` e `IMapa.cs`: puntos de la capa `poi` por tesela, lista agregada, evento y distancia de la cámara.
- [x] `Dominio/CategoriaPoi.cs` y `Dominio/ReglaPuntosInteres.cs`: categorías, ocultos y sanidad pública.
- [x] `UI/IconosLugar.cs`: glifos que faltan.
- [x] `UI/PuntoInteresVista.cs` y `UI/CapaPuntosInteres.cs`: círculo, nombre con halo, umbral de distancia y visibilidad.
- [x] `PantallaMapa.uxml/.uss/.cs` y `Tokens.uss`: capa, estilos, tokens y actualización.
- [x] `CapturaPruebas.cs`: captura "puntos".
- [x] `Tests/EditMode/PruebasPuntosInteres.cs`:
  - Cada fila de la matriz.
  - Lectura del id en `LectorMVT`.
  - Una tesela real de la caché, si existe, debe dar puntos con categoría.

**Acceptance Criteria:**
- Dada la build de PC, cuando se abre `puntos.png`, entonces se ven puntos de interés redondos con icono y nombre, distintos de los marcadores de lugar.
- Dado el log de la captura, cuando se lee, entonces hay más de 0 puntos visibles y ninguno es una parada de autobús ni una oficina.

## Implementation Notes

- Implementado por un subagente.
  - Pasan 203/203 pruebas EditMode.
  - La tesela real del centro de Sanlúcar da 291 puntos, 242 de ellos con categoría.
  - Captura de PC: 855 puntos en las teselas cargadas, 628 con categoría y 67 visibles a 900 m. Ninguno es una parada ni una oficina, y hay 1 sanitario público.
  - En `puntos.png` se ven los círculos con su icono y su nombre, por debajo de los marcadores. En el centro se amontonan, porque los solapes son de la parte 2.
- El halo de 1,5 px se comía las letras de 12 sp, así que se dejó en 0,5 px.
- `PruebasMarcadores.ArrastrarSobreUnMarcadorMueveElMapa` se adaptó: la capa de puntos va ahora en el índice 0 y la de marcadores en el 1.
- Tras los parches de la revisión:
  - Pasan 214/214 pruebas EditMode y el log de la captura no tiene errores.
  - Se oscurecen los colores de nombre de comer (`#A35512`) y súper (`#8B6300`) en el tema claro, para llegar a ≥ 4,5:1.
  - La sanidad pública excluye a los veterinarios y compara palabras completas.
  - `ConstruirDesde` (bytes → tesela → puntos) y `ActualizarCapas` quedan probados.
- En el sprint, la 1.6 sigue en `in-progress` hasta la parte 2 (repetidos, solapes y TalkBack).

## Spec Change Log

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | BH, EC | Los veterinarios (categoría salud) llamados "Hospital Veterinario" o "Urgencias…" se marcan como sanitarios públicos, y la regex no comprueba palabras completas | medium | `veterinary` va a Salud en `SUB_CAT`/`CLASS_CAT` → patch: excluir veterinarios y usar `\b…\b`, con pruebas |
| 2 | BH | No se comprueba el contraste del color de los nombres; el de supermercado da 4,30:1 en claro | medium | Por debajo del 4,5:1 que pide el texto (accesibilidad) → patch: ajustar los tokens que fallan y probar los dos temas |
| 3 | VG, BH | La ruta real de la tesela a `PuntosInteres` (`Construir` + `AGeo` + `RecomponerPuntos` en `CargarTesela`) no tiene prueba | medium | Las pruebas usan `SimularCargada` y un conversor falso → patch: método interno `ConstruirDesde` y prueba con la coordenada esperada |
| 4 | VG | Nada comprueba que `LateUpdate` coloca y muestra los puntos | medium | Las pruebas llaman a `Actualizar` directamente → patch: método interno y prueba de pantalla |
| 5 | BH, EC | `RecomponerPuntos` se ejecuta y lanza el evento con cada tesela, aunque no tenga puntos | low | Arreglo directo → patch |
| 6 | BH, EC | Los puntos sin id se llaman todos `poi-0` | low | Arreglo directo → patch |
| 7 | BH | Sin solapes por `rank`, sin quitar repetidos y sin TalkBack | false | Es la parte 2, ya apuntada en deferred-work (el spec lo deja fuera: Never) |
| 8 | BH | La sanidad pública no tiene estilo visual propio | low | El spec solo pide marcarla; se verá en TalkBack (parte 2) y en la ficha (1.8) → rechazado |
| 9 | BH | Se proyectan todos los puntos en cada fotograma y se recompone una vez por cada tesela liberada | low | Mismo patrón que los marcadores; se medirá en el móvil → rechazado |
| 10 | BH | La comprobación de "prohibidos" de la captura nunca puede fallar | low | Es una herramienta manual y la regla tiene sus pruebas → rechazado |
| 11 | BH | `DistanciaCamara` en el puerto | low | Decisión de diseño aceptable → rechazado |
| 12 | BH | No se prueba que `OnDestroy` se desuscribe | low | Mismo patrón que el tema → rechazado |
| 13 | EC | Un elemento MultiPoint solo guarda el primer punto | low | Los POI de OpenMapTiles son puntos sueltos → rechazado |
| 14 | EC | La captura no restaura el estado si la corrutina se detiene | low | Herramienta manual → rechazado |
| 15 | BH | Falta la memoria del TFG y no aparecen sprint y deferred en el diff | false | Se actualizan al cerrar; están fuera del diff que se revisaba |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC (`PriceMap.EditorTools.ConfigurarAndroid.CompilarPcPruebas`) y `PriceMap.exe -captura <carpeta>`: `puntos.png` y el log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.
