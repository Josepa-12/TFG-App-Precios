---
title: 'Historia 1.7 (parte 2): orientar al norte, estados del botón y aviso de ubicación desactivada'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '29c8e76d184e666025e664bef2f492ed6f1b1cf6'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-7-ver-mi-posicion-en-el-mapa.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La parte 1 deja el círculo, el cono y el centrado, pero el botón de ubicación siempre dice "Centrar en mi posición", no orienta al norte (UX-DR6), con el permiso denegado no hace nada, y con la ubicación del móvil apagada se queda en "Buscando" para siempre (hallazgo 3 de la revisión de la parte 1).

**Approach:** El botón pasa a tener estados. Primer toque con posición: centra. Segundo toque, ya centrado: orienta al norte (`IMapa.OrientarNorte()`). Si el usuario mueve el mapa, el siguiente toque vuelve a centrar. Con el permiso denegado o la ubicación del móvil apagada, al tocarlo sale un aviso con "Abrir Ajustes". TalkBack lee el estado. `UbicacionUnity` se separa de `Input.location` tras una interfaz pequeña para poder probarlo.

## Boundaries & Constraints

**Always:**
- **Estados nuevos de `EstadoUbicacion`:** se añade `Apagada` (permiso concedido, pero `Input.location.isEnabledByUser` es false o el servicio da `Failed`). Al volver a encenderse la ubicación, pasa a `Buscando` al volver a la app.
- **Acción del botón** (`AccionUbicacion`, puro y probado):
  - `SinPermiso` → explicación de la parte 1.
  - `Denegado` o `Apagada` → aviso de ubicación desactivada.
  - `Buscando` → nada.
  - `Activa` y mapa no centrado en el usuario → centrar.
  - `Activa`, centrado y con giro ≠ 0 → orientar al norte (giro 0, sin cambiar zoom ni inclinación).
  - `Activa`, centrado y con giro 0 → vuelve a centrar, sin efecto visible (decidido el 18/11/2026: sin modo brújula).
  - "Centrado" = la posición del usuario cae a menos de 8 dp del centro de la pantalla. Cualquier arrastre lo rompe.
- **TalkBack (etiqueta del botón según la acción):** "Centrar en mi posición", "Orientar al norte", "Buscando tu posición", "Ubicación desactivada".
- **Aviso:** ventana modal (mismo patrón que `ExplicacionUbicacion`):
  - Denegado: *"La ubicación está desactivada. Puedes activarla en Ajustes > Aplicaciones > PriceMap > Permisos."*
  - Apagada: *"La ubicación del móvil está apagada. Actívala en los ajustes rápidos del móvil."*
  - Botones "Abrir Ajustes" (denegado: ajustes de la app con `android.settings.APPLICATION_DETAILS_SETTINGS`; apagada: `android.settings.LOCATION_SOURCE_SETTINGS`) y "Cerrar". El botón atrás la cierra.
  - Abrir los ajustes va por un puerto (`ISistemaAjustes` o un método nuevo de `IUbicacion`), nunca desde la UI con JNI.
- **Separación del adaptador:** `UbicacionUnity` usa una interfaz interna `IFuenteUbicacion` (posición, rumbo, `isEnabledByUser`, estado del servicio, permiso) con la implementación real `FuenteUbicacionUnity`. Las pruebas usan una fuente falsa.
- **Captura de PC "posicion":** ubicación simulada en la Plaza del Cabildo con rumbo 45°, mapa centrado; se ven el círculo y el cono.
- Textos por Localization.

**Never:** sin seguir al usuario automáticamente, sin modo brújula (girar el mapa con el móvil) y sin GPS en segundo plano.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Primer toque | `Activa`, mapa lejos del usuario | Centra; etiqueta pasa a "Orientar al norte" si giro ≠ 0 |
| Segundo toque | `Activa`, centrado, giro 40° | Giro 0; etiqueta "Centrar en mi posición" |
| Centrado y al norte | `Activa`, centrado, giro 0 | Vuelve a centrar (no hace nada visible) |
| Arrastre tras centrar | Usuario mueve el mapa 50 dp | Siguiente toque centra de nuevo |
| Denegado | `Denegado` y toque | Aviso de permiso con "Abrir Ajustes" |
| Ubicación apagada | Permiso concedido, `isEnabledByUser` false | Estado `Apagada`; toque → aviso de ubicación apagada |
| Servicio `Failed` | Permiso concedido, estado `Failed` | Estado `Apagada` |
| Se enciende al volver | `Apagada`, vuelve a la app con la ubicación encendida | `Buscando` y luego `Activa` |
| Atrás con aviso | Aviso abierto y `Escape` | Se cierra; foco al botón de ubicación |
| Buscando | `Buscando` | TalkBack: "Buscando tu posición"; toque no hace nada |

</frozen-after-approval>

## Code Map

- `Scripts/Aplicacion/`:
  - `EstadoUbicacion.cs`: se añade `Apagada`. `AccionUbicacion.cs` (hoy `Nada`/`Explicar`/`Centrar`): se añaden `OrientarNorte` y `Avisar`.
  - `ServicioUbicacion.AlTocarBoton()` (`:74`): pasa a `AlTocarBoton(float distanciaCentroDp = ∞, float giro = 0)`, decisión pura con umbral de 8 dp; `AbrirAjustes()` delega en el puerto.
  - `IUbicacion.cs`: `AbrirAjustes()`. `IMapa.cs`: `Coordenada Centro { get; }` (para saber si el usuario está centrado: distancia en pantalla entre `Proyectar(Posicion)` y `Proyectar(Centro)`) y `OrientarNorte()`.
- `Scripts/Vectorial/CamaraMapa.cs:147-156`: `OrientarNorte()` (`giro = 0; Aplicar()`), instantáneo como `CentrarEn`. `MapaVectorial.cs:186-240`: `Centro` = `Geo.AGeo(pivote)` y `OrientarNorte`.
- `Scripts/Adaptadores/Gps/UbicacionUnity.cs`: la lógica pasa a `UbicacionGps` (clase pura que implementa `IUbicacion`, con `Actualizar()` por fotograma e `Interlocked` para las respuestas del permiso) sobre `IFuenteUbicacion` (permiso, `isEnabledByUser`, estado del servicio, lectura, brújula, encender/apagar, abrir ajustes). `FuenteUbicacionUnity` envuelve `Input.location`, `Input.compass`, `Permission` y los Intents de Android. `UbicacionUnity` queda como componente fino que delega (el Arranque no cambia, `Arranque.cs:41`). `AssemblyInfo.cs` con `InternalsVisibleTo` y referencia desde el asmdef de pruebas.
- `Scripts/UI/ExplicacionUbicacion.cs`: se parametriza (nombre, claves del texto y de los botones, `PonerTexto`) para reutilizarla como aviso; los estilos `explicacion-ubicacion*` se comparten.
- `Scripts/UI/PantallaMapa.cs`: `TocarUbicacion` (`:162`), `CrearUbicacion` (`:136`), `Atras` (`:306`), `TapaPunto` (`:342`), etiqueta del botón (`:89-90`, se recalcula en `ActualizarCapas` y se reconstruye TalkBack si cambia).
- Textos: `ClavesTexto.cs:66-85` (claves y `Todas`), `Editor/PrepararInterfaz.cs:29-77` (`TextosEs`); la tabla se regenera con `PrepararInterfaz.Asegurar` por lotes.
- `Scripts/Arranque/CapturaPruebas.cs:47-56` (`UbicacionCaptura`: rumbo 45° y `AbrirAjustes`) y `Start` (`:71-110`): captura "posicion" con el mapa centrado en el usuario.
- Pruebas: los seis `MapaFalso` (`PruebasArranque:18`, `PruebasFiltro:29`, `PruebasMarcadores:30`, `PruebasPantallaMapa:18`, `PruebasPuntosInteres:26`, `PruebasUbicacion:50`) ganan `Centro` y `OrientarNorte`. `PruebasUbicacion.cs:381-393` (denegado no hace nada) cambia: ahora sale el aviso.

## Tasks & Acceptance

**Execution:**
- [x] `EstadoUbicacion.Apagada`, `AccionUbicacion` con centrar/norte/aviso y su prueba.
- [x] `IMapa.OrientarNorte()` en `MapaVectorial` y `MapaFalso`.
- [x] `IFuenteUbicacion` + `FuenteUbicacionUnity`; `UbicacionUnity` probado con fuente falsa (denegado, permiso retirado, apagada, `Failed`, umbral de 1° del rumbo).
- [x] `UI/AvisoUbicacion.cs`, abrir ajustes por el puerto, etiquetas de TalkBack y textos.
- [x] `CapturaPruebas.cs`: captura "posicion".
- [x] `Tests/EditMode/PruebasUbicacion.cs`: cada fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas y `UbicacionUnity` se prueba sin `Input.location` real.
- Dada la build de PC, cuando se abre `posicion.png`, entonces se ven el círculo y el cono centrados.

## Spec Change Log

## Review Triage Log

| # | Fuente | Hallazgo | Veredicto | Evidencia | Ruta |
|---|--------|----------|-----------|-----------|------|
| 1 | blind | Faltan en el diff los dos `.asset` de Localization: no se sabe si están las 7 claves | false | El diff se generó sin `.asset`; `Textos Shared Data.asset` tiene las 7 claves y `Textos_es-ES.asset` los textos | rechazado |
| 2 | blind | Se editó el bloque congelado tras la aprobación | false | La línea se añadió al responder el usuario la pregunta abierta, antes de aprobar, como pide el paso 2 | rechazado |
| 3 | blind + edge | "Cualquier arrastre lo rompe": un arrastre de menos de 8 dp deja el mapa "centrado" y el toque orienta al norte | low | La intención define centrado como < 8 dp; un arrastre así es imperceptible (menor que el umbral de toque de 6 dp más 2). Distinguirlo exige seguir los arrastres en la cámara | rechazado |
| 4 | blind + edge | Se queda en `Apagada` si se enciende la ubicación desde la cortina sin pausar la app | false | Bajar la cortina lanza `OnApplicationFocus(false)` en Android (fila 6 de la revisión de la parte 1): al volver, `Iniciar` pasa a `Buscando` | rechazado |
| 5 | blind | Un `Failed` pasajero no se reintenta | low | Solo se reintenta al volver a la app; `Failed` es raro y el usuario sale a Ajustes con el aviso | rechazado |
| 6 | blind + edge | `Failed` con la ubicación encendida muestra "ubicación apagada" y abre ajustes que ya están bien | low | La intención congelada manda `Failed` → `Apagada`; es un caso raro | rechazado |
| 7 | blind + edge | El aviso abierto no reacciona a cambios de estado (texto viejo; "Abrir Ajustes" va por el estado actual, no por el texto) | low | Al volver de la cortina con la ubicación encendida, el aviso de "apagada" sigue abierto y "Abrir Ajustes" abre los de la app | patch |
| 8 | blind + edge | Al cambiar la etiqueta del botón se rehace toda la jerarquía: TalkBack pierde el foco tras tocar el botón y, cerca de 8 dp, la jerarquía se rehace una y otra vez; en el primer fotograma la etiqueta no se refresca | medium | `Reconstruir` crea una `AccessibilityHierarchy` nueva sin devolver el foco (`AccesibilidadPantalla.cs:247-268`); `primera` salta el refresco | patch (actualizar la etiqueta del nodo en el sitio) |
| 9 | blind | Sin permiso, TalkBack dice "Centrar en mi posición" y el toque abre la explicación | low | La intención solo da cuatro etiquetas; la explicación es el paso previo a centrar | rechazado |
| 10 | blind | Denegado siempre manda a Ajustes, sin volver a pedir el permiso en la app | false | Es lo que pide la intención (Denegado → aviso con "Abrir Ajustes") | rechazado |
| 11 | blind | El texto del aviso de denegado es ambiguo y la ruta de menús varía por fabricante | false | Texto literal de la intención congelada | rechazado |
| 12 | blind | Falta la prueba de recuperación Denegado → concedido en Ajustes → `Buscando` → `Activa` | low | Camino para el que existe el aviso, sin prueba | patch (prueba) |
| 13 | blind + edge | `UbicacionUnity` (delegación, eventos, `OnDestroy`) y `FuenteUbicacionUnity` sin pruebas; sin alternativa si el Intent no existe | low | Componente fino que solo delega; los Intents de ajustes existen en todos los Android soportados | rechazado |
| 14 | blind | Bordes del giro alrededor de 0 (359,6°, −0,4°) sin probar | low | `AlNorte` usa `Math.Min(resto, 360 − resto)`, justo donde fallaría | patch (prueba) |
| 15 | blind | `DistanciaAlCentroDp` usa 1 px = 1 dp sin escala | low | Solo antes del primer fotograma con disposición, cuando aún no se puede tocar el botón | rechazado |
| 16 | blind | La captura no muestra el aviso ni el mapa tras orientar al norte | low | El aviso se prueba en EditMode; el norte se comprueba en el log de la captura | rechazado |
| 17 | blind | Ficheros de proceso desfasados (estado, sprint, `deferred-work`, `CLAUDE.md`) | low | El estado se actualiza en el paso 5; la 1.7 sigue en `review` hasta cerrar; `deferred-work` no se edita | rechazado |
| 18 | gap | `Centro` y `OrientarNorte` del `MapaVectorial` real no tienen prueba | low | Solo los prueban los `MapaFalso`; un `Centro` mal calculado pasaría la suite | patch (prueba) |
| 19 | edge | La captura mide con `EscalaDp` 0 sin disposición | false | Se mide tras 8 s y varias capturas: hay disposición | rechazado |
| 20 | edge | Giro NaN o infinito → siempre "Orientar al norte" | false | `CamaraMapa.giro` solo cambia con sumas finitas | rechazado |
| 21 | edge | En PC la denegación espera un fotograma y puede convertirse en concedido | false | Si `TienePermiso`, `PedirPermiso` concede antes de preguntar; si no, `Actualizar` deniega | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC y `PriceMap.exe -captura <carpeta>`: `posicion.png` y log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En un Android real: segundo toque al norte, aviso con "Abrir Ajustes" que lleva a la pantalla correcta, y estado "apagada" con la ubicación del móvil desactivada.
