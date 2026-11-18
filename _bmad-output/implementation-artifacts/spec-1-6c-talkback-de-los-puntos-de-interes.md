---
title: 'Historia 1.6 (parte 3): TalkBack lee los puntos de interés'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: '317951479e847df73ae3b83930337a1678f8b3f1'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los puntos de interés (partes 1 y 2) solo existen a la vista. Una persona que usa TalkBack no sabe que hay una farmacia o un supermercado cerca. El usuario decidió el 17/11/2026 que el lector los lea.

**Approach:** Cada punto de interés visible es una entrada de texto para TalkBack ("Mercadona, supermercado"). Se leen después de los lugares de la app, cada grupo de arriba abajo, y aparecen y desaparecen con lo que se ve.

## Boundaries & Constraints

**Always:**
- **Qué dice TalkBack de cada punto:**
  - Con nombre: "{nombre}, {categoría}" (por ejemplo, "Mercadona, supermercado").
  - Sin nombre: solo la categoría.
  - Sanitario público: "{nombre}, centro sanitario público".
- **Nombres de categoría:** los de `POI_CATS.one` del prototipo, en minúsculas:
  - restaurante, comida rápida, cafetería, bar, **supermercado** (en el prototipo `super` era "Alimentación"), tienda, gasolinera, punto de carga;
  - aparcamiento, alojamiento, farmacia, salud, deporte, parque, cultura, banco;
  - peluquería, taller, transporte, centro educativo, servicio público, lugar de culto.
  - Van por Localization, con una clave por categoría, la de "centro sanitario público" y la plantilla "{0}, {1}".
- **Rol:** texto (StaticText), sin acción, porque tocar un punto no hace nada hasta la 1.8.
- **Orden del recorrido:** primero los botones; después los lugares de la app, de arriba abajo y de izquierda a derecha (como ahora); por último los puntos, en el mismo orden. Un punto nunca va antes que un lugar, aunque esté más arriba.
- **Cuándo no se leen:** los puntos que no se ven no tienen nodo: lejos, fuera de pantalla, repetidos o pisados. Con el menú "Precios" abierto (modo modal) tampoco se leen.
- **Altas y bajas:** las vistas de puntos se crean y se destruyen con las teselas, así que sus entradas de TalkBack se dan de alta y de baja con ellas. Ninguna entrada queda apuntando a una vista destruida.

**Never:**
- No se cambia el aspecto ni la lógica de visibilidad de los puntos (partes 1 y 2).
- No se cambia cómo se leen los lugares de la app ni los botones.
- Sin acción al activar un punto.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Punto con nombre | Supermercado "Mercadona" visible | Entrada "Mercadona, supermercado" | N/A |
| Sin nombre | Farmacia sin nombre visible | "farmacia" | N/A |
| Sanidad pública | "Centro de Salud Barrio Bajo", sanitario público | "Centro de Salud Barrio Bajo, centro sanitario público" | N/A |
| Orden | Lugar visible abajo y punto visible arriba | Botones, el lugar y después el punto | N/A |
| Ocultos | Punto repetido, pisado o fuera de pantalla | Sin entrada | N/A |
| Tesela liberada | Se destruye la vista de un punto | Su entrada desaparece; el recorrido no la incluye | N/A |
| Menú abierto | Modo modal | Ningún punto en el recorrido | N/A |

</frozen-after-approval>

## Code Map

- `Scripts/UI/AccesibilidadPantalla.cs`:
  - Hoy hay `class Marca` (`:25-31`) y `Marcador(elemento, etiqueta, visible, posicion)` (`:95`).
  - `MarcasVisibles()` (`:152-168`) ordena todas las marcas por posición, sin grupo.
  - `Recorrido()`/`RecorridoConEstado()`, `Reconstruir()` y `ActualizarMarcadores()` (`:203`) ya rehacen los nodos cuando cambia el conjunto o el orden.
  - Se añade un **grupo** a `Marca` (0 = lugares, 1 = puntos), y el orden pasa a ser primero por grupo y luego por posición. También se añade `QuitarMarcador(elemento)`.
  - `LectorForzado` (interno) permite probar la jerarquía real.
- `Scripts/UI/CapaPuntosInteres.cs`:
  - `Sincronizar()` (`:50-82`) crea vistas (`new PuntoInteresVista`) y las destruye (`RemoveFromHierarchy`).
  - Se añaden avisos o callbacks de vista creada y vista quitada, para que la pantalla dé de alta y de baja sus entradas sin que la capa conozca la accesibilidad.
- `Scripts/UI/PuntoInteresVista.cs`: `Punto` (con `Nombre`), `Categoria`, `SanidadPublica`, `Visible` y `Posicion`. Su texto para TalkBack se construye en la UI con los textos de Localization, igual que hace `MarcadorLugar.Anuncio`.
- `Scripts/UI/PantallaMapa.cs`:
  - Registra los marcadores en `:93`.
  - `AlCambiarPuntos` (`:274`) llama a `Sincronizar`: ahí se dan de alta y de baja los puntos en la accesibilidad.
  - `ActualizarCapas` llama a `accesibilidad.ActualizarMarcadores()`.
- `Scripts/Dominio/CategoriaPoi.cs`: hay 22 categorías y `Ninguna`.
- `Scripts/UI/ClavesTexto.cs`, `Editor/PrepararInterfaz.cs:29` (`TextosEs`) y las tablas: se vuelcan con `PrepararInterfaz.Asegurar` por lotes. `PruebasTextos` comprueba las claves.
- `Tests/EditMode/PruebasPuntosInteres.cs` y `PruebasFiltro.cs`: patrón de pantalla con `MapaFalso`, `LectorForzado` y `Textos.Forzados`.

## Tasks & Acceptance

**Execution:**
- [x] `UI/AccesibilidadPantalla.cs`: grupo de marcas, orden por grupo y `QuitarMarcador`.
- [x] `UI/CapaPuntosInteres.cs`: avisos de vista creada y quitada.
- [x] `UI/PantallaMapa.cs` y un `UI/TextosPuntos.cs` (texto de cada punto para TalkBack): alta y baja de entradas.
- [x] Textos: 22 categorías, sanidad pública y la plantilla en `ClavesTexto`, `TextosEs` y las tablas.
- [x] `CapturaPruebas.cs`: en la captura "puntos", registrar en el log el recorrido de TalkBack (cuántos lugares y cuántos puntos, y los tres primeros puntos).
- [x] `Tests/EditMode/PruebasPuntosInteres.cs`: cada fila de la matriz, con `Recorrido()` y con la jerarquía real (`LectorForzado`) para el orden y las altas y bajas.

**Acceptance Criteria:**
- Dado el log de la captura "puntos", cuando se lee el recorrido, entonces todos los lugares van antes que cualquier punto y los puntos se leen como "nombre, categoría".
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas las pruebas, también las de la 1.4 y la 1.5 sobre el recorrido.

## Implementation Notes

- `AccesibilidadPantalla`: constantes `GrupoLugares` (0) y `GrupoPuntos` (1), parámetro `grupo` en `Marcador`, `QuitarMarcador` (quita la marca y, si tenía nodo, lo saca ya de la jerarquía activa) y `RecorridoDeMarcas()` (etiqueta y grupo, para la captura).
- `PantallaMapa.ActualizarCapas`: `ActualizarMarcadores()` pasa a ir después de colocar los puntos; antes iba entre las dos capas y un punto recién oculto o mostrado habría tardado un fotograma más en tener o perder su nodo.
- El nodo de cada punto usa como marco el círculo (`Icono`), no la vista, que mide 0×0.
- Las vistas que crea el constructor de la capa se dan de alta recorriendo `Vistas` justo después de suscribirse a los avisos.
- Captura (17/11): 53 entradas, 23 lugares y 28 puntos; primeros puntos "deporte", "parque", "C.P. Vista Alegre, centro educativo"; sin errores.
- Tras los parches de la revisión (18/11):
  - Pasan 255/255 pruebas EditMode. La captura de PC sale igual y no da errores.
  - Si solo cambia el orden, los nodos se rehacen como mucho una vez por segundo (`Reloj` inyectable). Un cambio en el conjunto se rehace al momento.
  - Al quitar puntos se envía un solo `SendLayoutChanged` por tanda.
  - La zona táctil de cada punto para TalkBack mide 48 dp.
  - La primera captura se quedó en pausa porque la ventana perdió el foco (`runInBackground: 0`). Al repetirla, terminó entera.

## Spec Change Log

## Review Triage Log

Primera pasada (18/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | BH, EC | Al mover el mapa, el orden de los puntos cambia casi en cada fotograma, se rehacen todos los nodos y el foco de TalkBack salta | medium | Ahora que todos los puntos son marcas, `MismoOrden` falla continuamente → patch: si solo cambia el orden, rehacer como mucho una vez por segundo; si cambia el conjunto, enseguida |
| 2 | EC | Liberar una tesela envía un `SendLayoutChanged` por cada punto quitado | medium | Inunda TalkBack → patch: un solo aviso por tanda |
| 3 | BH | El marco de TalkBack de un punto mide 24 dp, por debajo de los 48 dp del Accessibility Floor | medium | Con exploración táctil apenas se puede tocar → patch: marco de al menos 48 dp centrado en el icono |
| 4 | BH, EC | `TextoAnuncio` con una categoría vacía devuelve "Nombre, " | low | Arreglo directo → patch |
| 5 | BH, EC | La captura monta la etiqueta esperada a mano con ", " | low | Arreglo directo → patch: usar `TextosPuntos.Anuncio` |
| 6 | VG | Nada compara la tabla de Localization con los textos del generador | low | Gap ya verificado, disposición defer: afecta a toda la suite → deferred |
| 7 | BH, EC | Las etiquetas de los nodos no se refrescan si cambia el texto | low | `GetTable` es síncrono y en la v1 solo hay es-ES, así que el texto no cambia en la sesión → rechazado |
| 8 | BH | Nombres de categoría como "salud", "deporte" o "peluquería" | low | Los fija el spec congelado (`POI_CATS.one` del prototipo); cambiarlos sería editar el spec → rechazado, se comenta al usuario |
| 9 | BH | Eventos `VistaCreada` y `VistaQuitada` sin desuscribir | low | La capa y la pantalla viven lo mismo → rechazado |
| 10 | BH | Quitar el nodo enfocado deja el foco en un sitio indefinido | low | Ocurre solo al liberar teselas lejanas; se verá en el móvil → rechazado |
| 11 | EC | Captura con el menú abierto | low | La captura nunca abre el menú en esa fase → rechazado |
| 12 | BH | Faltan pruebas de empate de posición y de `capa == null` | low | Caminos triviales → rechazado |
| 13 | BH | Coste por fotograma de `MarcasVisibles` | low | Lo reduce el arreglo #1 → rechazado |
| 14 | BH | Faltan los assets de textos en el diff | false | Se excluyeron del diff que se revisaba; `PruebasTextos` comprueba las claves en la tabla |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC y `PriceMap.exe -captura <carpeta>`: el recorrido en el log y ningún error. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.
