---
title: 'Historia 2.2 (parte 2): TalkBack y reducir animaciones del buscador'
type: 'feature'
created: '2026-11-19'
status: 'done'
baseline_commit: 'ade3a599d48c3d6c549c236d0a1d0d1e5ed14671'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La hoja del buscador de la parte 1 (commit `ade3a59`) no existe para TalkBack, y su transición de 0,32 s no respeta el ajuste del sistema "quitar animaciones" (UX-DR19).

**Approach:** Declarar la cápsula y la hoja en `AccesibilidadPantalla`. Para quien usa TalkBack, tocar la cápsula abre la hoja del todo, lleva el foco al campo y deja la hoja modal, con "Expandir" y "Contraer" como botones que solo ve el lector. Un puerto nuevo, `IAccesibilidadSistema`, lee del sistema si están quitadas las animaciones, y en ese caso la hoja cambia de altura sin transición.

## Boundaries & Constraints

**Always:**
- **Cápsula en TalkBack:** un botón «Buscar en Sanlúcar» que abre la hoja del todo, lleva el foco al campo y pone la pantalla en modo modal. Se lee después de los controles del mapa. El avatar no aparece en el recorrido hasta que haga algo (3.5).
- **Hoja abierta, o desplegada a cualquier altura:** es modal. El recorrido tiene, por este orden, el campo, «Expandir» y «Contraer» (botones que solo ve el lector), los 8 chips y «Encuentra cerca». No incluye ningún marcador ni control del mapa.
- **«Contraer»** pliega la hoja, quita el modo modal y devuelve el foco a la cápsula. **«Expandir»** abre la hoja del todo. **Atrás** hace lo mismo que «Contraer».
- **Quitar animaciones:** si la escala de animaciones del sistema es 0 (`Settings.Global.ANIMATOR_DURATION_SCALE`), la hoja cambia de altura sin transición. Se lee al arrancar y cada vez que la app vuelve a primer plano.
  - Lo lee un puerto `IAccesibilidadSistema` (Aplicacion), con un adaptador para Android que sigue el patrón de `TemaSistemaAndroid`. Fuera de Android, y si hay algún error, devuelve false.
- **Decisiones del 19/11/2026:**
  - Hasta la 2.4, los chips y «Encuentra cerca» se declaran como texto, no como botones, porque todavía no hacen nada.
  - «Expandir» solo está en el recorrido cuando la hoja no está abierta del todo. «Contraer» está siempre que la hoja está desplegada.
- **Reducir transparencias:** Android no ofrece ese ajuste a las apps. Por eso la cápsula sigue como en la parte 1: opaca en el tema oscuro y de cristal al 0,90 en el claro (decisión del 19/11/2026).

**Never:** nada de la búsqueda (2.3), ni que los chips hagan algo (2.4), ni acciones personalizadas nuevas en `AccesibilidadPantalla` (se usan botones solo para el lector, como el zoom).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| TalkBack abre | Lector activo y se activa la cápsula | Abierta del todo, modal, foco en el campo |
| Recorrido abierta | Lector activo, hoja abierta | Campo, Contraer (y Expandir si aplica), 8 chips, Encuentra cerca; sin marcadores |
| Contraer | Se activa «Contraer» | Plegada, no modal, foco en la cápsula |
| Atrás | Lector activo, hoja abierta | Igual que Contraer |
| Sin lector | Lector apagado | Ni «Expandir» ni «Contraer» en el recorrido |
| Quitar animaciones | Escala 0 | La hoja no tiene transición |
| Animaciones normales | Escala 1 | Transición de 0,32 s |
| Vuelve a primer plano | La escala cambió en Ajustes | Se aplica la nueva |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- **`Scripts/UI/PantallaMapa.cs`** — `CrearBuscador()` (~l.350) declara los nodos con `accesibilidad.Boton/Texto(..., enModal: true, visible: ...)` (firma en `AccesibilidadPantalla.cs:112-132`). Modelos que seguir:
  - la ficha: `DeclararFichaEnTalkBack` (~l.445);
  - el aviso: l.199-201;
  - el zoom, botón solo para el lector: l.106, con `visible: () => AccesibilidadPantalla.LectorActivo`.
- **Modo modal y foco:**
  - Al desplegar la hoja (`AlDesplegarBuscador` ~l.377, o su apertura por TalkBack) se pone `accesibilidad.Modal = true` y `PedirFoco(buscador.Campo)`. Al plegarla (`PlegarBuscador` ~l.384, `Atras` ~l.630) se pone `Modal = false` y `PedirFoco(cápsula)`.
  - El modo modal tiene que seguir a la hoja aunque se pliegue por gesto (`HojaBuscador.Soltar` con destino plegado). Se comprueba en `ActualizarCapas` (~l.880), como se hace con el aviso.
  - No pisar el modo modal de la ficha, el menú o la explicación: abrir la hoja ya los cierra.
- **`Scripts/UI/HojaBuscador.cs`** — se le añade una clase USS que quita la transición (p. ej. `buscador--sin-transicion`, `transition-duration: 0s`, como `.buscador--arrastrando` en `PantallaMapa.uss:878`) y un método para activarla. También hacen falta los elementos de «Expandir» y «Contraer»: pueden ser `VisualElement` invisibles a la vista, con un tamaño para el marco del nodo.
- **Puerto** — nuevo `Scripts/Aplicacion/IAccesibilidadSistema.cs` (`bool AnimacionesQuitadas { get; }`). El adaptador nuevo es `Scripts/Adaptadores/Sistema/AccesibilidadSistemaAndroid.cs`:
  - `#if UNITY_ANDROID && !UNITY_EDITOR`;
  - `AndroidJavaClass("android.provider.Settings$Global").CallStatic<float>("getFloat", contentResolver, "animator_duration_scale", 1f)`;
  - `try/catch` con `Debug.LogWarning`, como `TemaSistemaAndroid`.
- **Conexión** — `Arranque.cs` (l.31; `Componer` l.74; `OnApplicationFocus`/`Pause` l.53-55): crea el adaptador y lo pasa a `PantallaMapa.Inicializar` (l.53 y 63) como parámetro opcional (null = animaciones normales). La pantalla lo vuelve a leer al reanudarse (`AlCambiarFoco`).
- **Textos** — `ClavesTexto` y `PrepararInterfaz.TextosEs`: «Expandir» y «Contraer». La etiqueta de la cápsula reutiliza `BuscadorCampo`.
- **Pruebas** — `Tests/EditMode/PruebasBuscador.cs`, ampliado con:
  - `LectorForzado`, `pantalla.Accesibilidad.Recorrido()` y `.Enfocado`, como en `PruebasFicha` (TalkBack, ~l.655), y `Limpiar` (l.281) que ya los resetea;
  - un `IAccesibilidadSistema` falso.
- **No tocar:** `FisicaHoja` ni los gestos.

## Tasks & Acceptance

**Execution:**
- [x] `IAccesibilidadSistema` + `AccesibilidadSistemaAndroid` + `Arranque` — leer la escala de animaciones.
- [x] `HojaBuscador` + USS — sin transición; elementos de Expandir/Contraer.
- [x] `PantallaMapa` — nodos de TalkBack, modal, foco, Atrás/Contraer, relectura al volver.
- [x] Textos y pruebas de cada fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se hace la captura, entonces el buscador sigue igual que en la parte 1 (sin lector).

## Implementation Notes

- El modo modal sigue a la hoja con `PantallaMapa.SeguirModalBuscador`, llamado al desplegar (`AlDesplegarBuscador`), al plegar (`PlegarBuscador`) y en cada `ActualizarCapas` (gestos). Si la hoja se pliega porque se abre la ficha, el menú o una ventana de ubicación (`PlegarBuscadorPorOtraVentana`), el modo modal pasa a ser el de esa ventana; las ventanas lo ponen con `ModalNuevo` (rehace el recorrido si ya era modal).
- Abrir la explicación o un aviso de ubicación pliega ahora la hoja (antes no se trataba ese caso), como ya hacían la ficha y el menú.
- La píldora «Precios» sale del recorrido con la hoja desplegada (era `enModal`).
- Tras «Expandir», el foco pasa a «Contraer» (el nodo de «Expandir» desaparece). El campo es un botón cuyo doble toque le da el foco (teclado).
- «Expandir» y «Contraer» son dos `VisualElement` invisibles en las mitades de la zona del tirador (`.buscador__solo-lector`).
- El Arranque relee el ajuste en `OnApplicationFocus(true)` y `OnApplicationPause(false)` (`PantallaMapa.LeerAccesibilidadSistema`).
- Las pruebas referencian ahora `PriceMap.Adaptadores.Sistema`. `PruebasFiltro` y `PruebasPuntosInteres` cuentan la cápsula en el recorrido.

## Spec Change Log

## Review Triage Log

| # | Hallazgo | Veredicto | Ruta | Evidencia |
|---|----------|-----------|------|-----------|
| 1 | Las pruebas de «Expandir» y del menú sobre la hoja miran el recorrido calculado, no la jerarquía real: si faltara un `Reconstruir()`, no lo notarían | gap | patch | Dos pruebas nuevas sobre `AssistiveSupport.activeHierarchy` (se ejecutan; ninguna se salta) |
| 2 | Ninguna prueba abre una ventana de ubicación con la hoja desplegada | gap | patch | Prueba nueva en `PruebasUbicacion`: el aviso pliega la hoja, queda solo él en el recorrido y al cerrarlo no vuelve el modo modal |
| 3 | No se prueba que `Componer` pase el puerto a la pantalla | gap | patch | Prueba nueva en `PruebasArranque` (más un accesor `internal` para comprobarlo) |
| 4 | Desplegada con el dedo y con TalkBack, el foco se quedaba en un nodo del mapa que ya no está | medium | patch | `SeguirModalBuscador` lleva el foco al campo; hay prueba |
| 5 | Al volver a la app se leía el ajuste dos veces (pausa y foco) | low | patch | Ahora solo en `OnApplicationFocus`, que también llega tras la pausa |
| 6 | La lista de pruebas de CLAUDE.md tenía `Ficha` repetida | low | patch | Corregido |
| 7 | Con TalkBack, abrir la hoja da el foco al `TextField` y sale el teclado | low | rechazado | El spec pide el foco en el campo; es lo mismo que pasa con un toque sin lector (parte 1) |
| 8 | El campo se declara como botón y no como campo de búsqueda | medium | defer | `AccesibilidadPantalla` solo crea botones y textos; dar otro rol amplía su API (con la búsqueda real, 2.3) |
| 9 | «Quitar animaciones» solo afecta a la hoja; los movimientos de cámara del mapa siguen animados | medium | defer | Ya existía antes de esta historia; el spec limita la parte 2 a la hoja |
| 10 | La cápsula y el campo tienen la misma etiqueta | false | rechazado | Nunca están a la vez en el recorrido |
| 11 | Sin lector, el modo modal se reconstruye al desplegar | low | rechazado | Una vez por apertura (está protegido por `modalBuscador`), no en cada fotograma |
| 12 | `PlegarBuscador` puede reconstruir dos veces | low | rechazado | Solo si se pliega en el mismo fotograma en que se abrió; sin efecto visible |
| 13 | «Expandir» y «Contraer» están en las dos mitades del tirador | low | rechazado | Hace falta un marco para el nodo; el sitio es razonable |
| 14 | `AbrirFicha` no usa `ModalNuevo` | low | rechazado | Hace lo mismo; no causa ningún daño |
| 15 | Ventana de ubicación abierta y la hoja se despliega o se activa debajo | false | rechazado | La explicación y el aviso van encima de la hoja, con velo, y su recorrido modal no incluye la cápsula |
| 16 | La ventana se cierra con la hoja abierta debajo | false | rechazado | Abrir la ventana ya pliega la hoja |
| 17 | El lector se enciende con la hoja abierta y «Contraer» no aparece | low | rechazado | `AlCambiarLector` ya reconstruye la jerarquía |
| 18 | Pruebas de USS con expresiones regulares; nombre engañoso de una prueba; caso con el puerto null | low | rechazado | Frágiles pero correctas; el caso null es el mismo que «animaciones normales» |

## Verification

**Commands:**
- Pruebas EditMode por lotes — todas pasan.
- Build de PC y `PriceMap.exe -captura` — sin errores; luego borrar `link.xml` y deshacer `addressables_content_state.bin`.
- `CompilarApk` — compila (el adaptador solo existe en Android).

**Manual checks:**
- En el móvil, con TalkBack: abrir, recorrido, Expandir/Contraer, atrás; con "quitar animaciones", sin transición.
