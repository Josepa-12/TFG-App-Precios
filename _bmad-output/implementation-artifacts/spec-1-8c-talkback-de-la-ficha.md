---
title: 'Historia 1.8 (parte 3): TalkBack de la ficha'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '6bd287f0ebd63d209bb091af5cecee545d82147a'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Con TalkBack, la ficha del lugar (partes 1 y 2, `580abd3` y `6bd287f`) no existe. Tocar dos veces un marcador no la abre, su contenido no se puede leer y solo se cierra con el botón atrás. Además, los controles del mapa siguen en el recorrido aunque estén tapados.

**Approach:** Tocar dos veces el nodo de un marcador abre su ficha. Mientras está abierta, la ficha es modal: el recorrido solo tiene sus nodos y el foco va al título. Al cerrarla, el foco vuelve al marcador, como con el menú "Precios" y las ventanas de ubicación.

## Boundaries & Constraints

**Always:**
- **Abrir:** activar el nodo de un marcador de lugar (doble toque de TalkBack, `invoked`) llama a `AbrirFicha(lugar)`. Los puntos de interés siguen sin acción, porque su ficha es de la épica 2.
- **Modal:** con la ficha abierta, `Modal = true`. El recorrido, en este orden:
  1. el título (nombre del lugar);
  2. el tipo ("Clínica privada · Servicio local");
  3. la dirección, si la hay;
  4. por cada área, su encabezado;
  5. un nodo por concepto, con una frase: nombre, precio, unidad si la hay, origen y frescura ("Sesión de fisioterapia, desde 40 € por sesión. Publicado en su web el 13/11/2026. Al día"). Las variantes van después, una por nodo ("Ahora: de 20:00 a 24:00, 24 €");
  6. "Aún no tiene precio", si no hay conceptos;
  7. el botón "Cerrar ficha" al final.
- **Foco:**
  - Al abrir, va al título.
  - Al cerrar (atrás, X o tocar fuera), `Modal = false` y el foco vuelve al nodo del marcador que la abrió, si sigue visible. Si no, no se fuerza ningún foco.
  - Abrir otra ficha con la primera abierta (un toque en el mapa) mantiene como destino el marcador de la nueva.
- **Desplazamiento:** cuando TalkBack pone el foco en un nodo de la ficha que queda fuera de la parte visible del `ScrollView`, la ficha se desplaza hasta él.
- **Nodos por apertura:** los nodos de la ficha cambian con cada lugar. Al cerrar o abrir otra, los de la anterior desaparecen del recorrido, sin dejar nodos que apunten a elementos que ya no existen.
- Los textos nuevos van por Localization (`ClavesTexto` + `PrepararInterfaz.TextosEs`). `ClavesTexto.FichaCerrar` ya existe.

**Never:** sin cambios visuales en la ficha; sin cambiar el recorrido del mapa ni el de los demás modales; sin anunciar nada más al abrir (el foco en el título ya lo lee).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Abrir con TalkBack | Lector forzado; `invoked` en el nodo del marcador de Clínica Areas | Ficha abierta; `Modal` true; `Enfocado` es el título | N/A |
| Recorrido | Ficha de Clínica Areas abierta | Título, tipo, dirección, áreas y conceptos en orden, "Cerrar ficha" al final; ningún marcador ni control del mapa | N/A |
| Frase del concepto | "Sesión de fisioterapia" | "Sesión de fisioterapia, desde 40 € por sesión. Publicado en su web el 13/11/2026. Al día" | N/A |
| Consultar | "Infiltraciones" | Frase con "Consultar", sin unidad ni frescura | N/A |
| Variante vigente | Lugar sintético de pádel a las 21:00 | Nodo "Ahora: de 20:00 a 24:00, 24 €" tras su concepto | N/A |
| Sin precio | ChemaBarber | "Aún no tiene precio" y "Cerrar ficha" | N/A |
| Cerrar con la X | `invoked` en "Cerrar ficha" | Ficha cerrada; `Modal` false; `Enfocado` es el cuerpo del marcador | N/A |
| Cerrar con atrás | `Atras()` | Lo mismo | N/A |
| Otra ficha | Abrir Clínica Areas y después ChemaBarber sin cerrar | El recorrido solo tiene nodos de ChemaBarber; al cerrar, el foco va a su marcador | N/A |
| Sin lector | `LectorForzado` false | Abrir y cerrar funcionan; no se activa ninguna jerarquía | N/A |

</frozen-after-approval>

## Code Map

- `Scripts/UI/AccesibilidadPantalla.cs`:
  - `Marcador(...)` (l.128) recibe un `Action alActivar` opcional. En `AnadirMarcas`, el nodo de la marca hace `nodo.invoked += …` (como los botones en `Reconstruir`, l.273-274) y su rol pasa a `Button` cuando tiene acción.
  - `Enfocar` (l.210) hoy solo busca en `nodosControles` y tiene que buscar también en `nodosMarcas` por `marca.elemento`.
  - Hace falta dar de baja controles dinámicos, por ejemplo `QuitarControles(VisualElement contenedor)` o un grupo de controles que se sustituye entero, y reconstruir.
  - `Modal` (l.85), `Texto` (l.119), `Boton` (l.107) y `PedirFoco` (l.201) se reutilizan.
  - Para el desplazamiento: el evento de foco de `AccessibilityNode` (`focusChanged`, si existe en Unity 6000.3) llama a un `Action<VisualElement>`. Si no existe, se anota en Implementation Notes y se deja para la prueba en el móvil.
- `Scripts/UI/PantallaMapa.cs`:
  - Marcadores: l.117, pasar `() => AbrirFicha(marcador.Lugar)`.
  - `AbrirFicha`/`CerrarFicha`: registrar los nodos de la ficha con `enModal: true`, `Modal = true` + `PedirFoco(ficha.Titulo)`; al cerrar, darlos de baja, `Modal = false` + `PedirFoco(marcador.Cuerpo)`.
  - Patrón a copiar: las ventanas de ubicación (l.163-173, 242-243 y 258-259) y el menú (`AlternarMenu`/`Cerrar`).
  - `CapaMarcadores.Marcadores` para encontrar el marcador de un lugar.
- `Scripts/UI/FichaLugar.cs`, `FilaConcepto.cs` y `FilaVariante.cs`: frase de TalkBack (`Anuncio`) de cada fila, construida con `Textos.Formatear` y claves nuevas. `FichaLugar.Contenido` (ScrollView) tiene `ScrollTo(VisualElement)`.
- **Pruebas:** patrón con TalkBack forzado en `PruebasFiltro.cs:68-75` (TearDown) y `:519`:
  - `LectorForzado = () => true`;
  - `Assert.Ignore` si `AssistiveSupport.activeHierarchy` es null;
  - `activeHierarchy.rootNodes` (label, role, `invoked`), `Recorrido()` y `Enfocado`.

  En `PruebasFicha.cs` ya hay `AbrirPadel` y `MapaFalso`.

## Tasks & Acceptance

**Execution:**
- [x] `AccesibilidadPantalla.cs`: acción en las marcas, foco a marcas y alta/baja de los nodos de la ficha.
- [x] `FilaConcepto.cs`, `FilaVariante.cs`, `ClavesTexto.cs` y `PrepararInterfaz.cs`: frases de TalkBack. Regenerar la tabla con `PrepararInterfaz.Asegurar` por lotes.
- [x] `PantallaMapa.cs` y `FichaLugar.cs`: abrir desde el nodo, ficha modal, foco al título y vuelta al marcador, y desplazamiento al foco.
- [x] `Tests/EditMode/PruebasFicha.cs`: una prueba por fila de la matriz, con el lector forzado.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas, incluida una prueba por fila de la matriz.
- Dada la build de PC, cuando se ejecuta `-captura`, entonces el log no tiene errores y las capturas existentes no cambian.

## Implementation Notes

- `AccessibilityNode.focusChanged` (`Action<AccessibilityNode, bool>`) existe en 6000.3: los controles aceptan `alEnfocar`, y los nodos del contenido llaman a `FichaLugar.DesplazarHasta` (`ScrollView.ScrollTo`). Una prueba lanza `focusChanged` por reflexión y comprueba que solo se desplaza con un nodo del contenido. Sin panel, `ScrollTo` no se llama, porque lanza una NullReferenceException. Que se desplace de verdad queda para el móvil.
- Con la ficha abierta, abrir el aviso o la explicación de ubicación cierra antes la ficha, como hace el menú.
- `UltimoNodoEnfocado` (interno) guarda el nodo que recibe `SendLayoutChanged`. Las pruebas lo usan para comprobar el foco en el título y en el marcador.
- Alta y baja: `Boton`/`Texto` aceptan un `grupo`; `QuitarControles(grupo)` quita sus nodos de la jerarquía activa y los da de baja. La ficha se declara entera (cabecera, contenido y "Cerrar ficha") en cada apertura con `grupo: ficha`.
- El orden sale de `FichaLugar.TextosTalkBack()` (título, tipo, dirección y los hijos del contenido en orden). Las frases: `FilaConcepto.Anuncio` y `FilaVariante.Anuncio`, con 6 claves nuevas `ficha.anuncio.*`. `ficha.cerrar` pasa de "Cerrar" a "Cerrar ficha" (solo se usaba para TalkBack: la X es un icono).
- La píldora "Precios" (`enModal`) se oculta también con la ficha abierta, para que el recorrido no tenga controles del mapa.
- `Enfocar` busca también en los nodos de los marcadores (`NodoDe`).
- Los marcadores de lugar pasan a tener el rol `Button`. Dos aserciones de `PruebasFiltro` los reconocían por `StaticText` y ahora los reconocen por su etiqueta.
- En las pruebas, el doble toque se simula leyendo por reflexión el delegado de `invoked` del nodo. Si una versión de Unity lo cambia, la prueba sale como Inconclusive.

## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | Si con la ficha abierta se abre el aviso o la explicación de ubicación, `Modal` ya es true y no se reconstruye: faltan sus nodos, y al cerrarlo el mapa vuelve debajo de la ficha (ECH, VG, BH) | medium | `AbrirAviso`/`AbrirExplicacion` no miran `FichaAbierta`; el setter de `Modal` sale si el valor no cambia. Se llega con teclado o mando (`TocarUbicacion`). | patch |
| 2 | La vuelta del foco al marcador y el foco en el título solo se comprueban con `Enfocado`, que se pone antes de buscar el nodo (VG) | medium | Si se vuelve a buscar solo en `nodosControles`, no se manda `SendLayoutChanged` al marcador y ninguna prueba falla. | patch |
| 3 | El desplazamiento al foco (`focusChanged` → `DesplazarHasta`) no tiene ninguna prueba (VG, BH) | medium | Sin layout no se ve el desplazamiento, pero la suscripción y la condición de `DesplazarHasta` sí se pueden probar, igual que `invoked`. | patch |
| 4 | "Ahora" está en dos claves: el chip y `ficha.anuncio.variante.ahora` (BH) | low | Una traducción podría cambiar solo una; se corrige formando la frase con `FichaAhora`. | patch |
| 5 | Dos pruebas de frases no miran la jerarquía real (variante tras su concepto) (BH) | low | `LaVarianteVigenteSeLeeTrasSuConceptoConAhora` comprueba `TextosTalkBack()` y no los nodos activos. | patch |
| 6 | Los nodos fuera de la parte visible pueden no recibir foco o dibujar su marco sobre el mapa (BH) | maybe-false | Depende de cómo trate TalkBack un marco que se sale del `ScrollView`; solo se ve en el móvil. | defer |
| 7 | "Publicado en su web el" sin fecha (BH, ECH) | false | `Origen.DeWeb` exige siempre una fecha. | rechazado |
| 8 | Las áreas no tienen rol de encabezado (BH) | low | El spec no lo pide; añadir el rol amplía `Texto`. Mejora para más adelante. | rechazado |
| 9 | "Otra ficha con un toque en el mapa" no se puede dar: el toque cierra la ficha (BH, ECH) | low | Es redacción del spec; el código cubre la llamada directa a `AbrirFicha`, que es la que existe. | rechazado |
| 10 | La prueba del recorrido no comprueba el botón de ubicación (BH) | false | Compara la lista entera con `CollectionAssert.AreEqual`, así que ya lo excluye. | rechazado |
| 11 | "Consultar" se entiende peor oído; fecha leída como cifras (BH) | low | La matriz pide "Consultar"; TalkBack lee "13/11/2026" como fecha. | rechazado |
| 12 | "vIP" al pasar a minúscula; nombre vacío; área vacía en la prueba (ECH) | low | Ningún dato real lo tiene. | rechazado |
| 13 | `MarcadorDe` compara referencias de `Lugar` (ECH) | false | Los marcadores y la ficha reciben las mismas instancias del repositorio. | rechazado |
| 14 | Reconstruir la jerarquía dentro del `invoked` del nodo (ECH) | low | Es el mismo patrón que la píldora "Precios" (1.5) al abrir su menú. | rechazado |
| 15 | `sprint-status`, `deferred-work` y `CLAUDE.md` desfasados (BH) | low | Se ponen al día al cerrar la historia. | rechazado |

## Verification

Resultado (18/11/2026): EditMode 352/352 (11 nuevas en `PruebasFicha`, 0 ignoradas). Build de PC correcta. Con `-captura`, el log no tiene errores y las 9 capturas salen idénticas byte a byte a las de la build anterior. Después se borró `link.xml` (y su `.meta`) y se deshizo `addressables_content_state.bin`.

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC (`-buildTarget Win64`) y `PriceMap.exe -captura <carpeta>`: log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En un móvil con TalkBack (pendiente en `deferred-work`): tocar dos veces un marcador abre la ficha, deslizar recorre solo la ficha y la desplaza, y atrás devuelve el foco al marcador.
