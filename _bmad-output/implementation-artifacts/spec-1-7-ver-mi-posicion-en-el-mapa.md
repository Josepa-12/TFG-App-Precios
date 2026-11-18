---
title: 'Historia 1.7 (parte 1): ver mi posición en el mapa'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: 'aa123b045c86e8f0bf76586582edb014adb47e4e'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El mapa no sabe dónde está el usuario. Solo hay un marcador fijo, "Tu posición (simulada)", en la Plaza del Cabildo (FR-5, UX-DR6).

**Approach:** Un puerto `IUbicacion` con su adaptador de GPS y brújula. La posición se dibuja como un círculo azul con un cono de orientación, y un botón de ubicación centra el mapa en el usuario. El permiso se pide al abrir la app, después de una explicación breve. La parte 2 (`deferred-work`) añade el segundo toque para orientar al norte, lo que lee TalkBack según el estado, el aviso de denegado con "Abrir Ajustes" y la captura de pruebas.

## Boundaries & Constraints

**Always:**
- **Permiso (decidido el 18/11/2026):** se pide la primera vez que se abre la app. Antes sale una ventana modal con el texto *"PriceMap usa tu ubicación solo mientras tienes la app abierta, para mostrarte dónde estás en el mapa. No la guarda ni la comparte."* y los botones "Permitir" y "Ahora no". TalkBack la trata como modal, igual que el menú "Precios".
  - "Permitir" lanza el permiso de Android.
  - Que la explicación ya salió se guarda en `IPreferencias`, y no vuelve a salir al abrir la app.
  - Con "Ahora no", tocar el botón de ubicación vuelve a mostrar la explicación. Si Android ya lo denegó, tocarlo no hace nada (el aviso es de la parte 2).
- **GPS solo con la app delante (NFR-3):** con `OnApplicationPause(true)` u `OnApplicationFocus(false)` se detienen la ubicación y la brújula, y se reanudan al volver.
- **Arquitectura:** Dominio y Aplicación no ven `Input.location` ni `Permission`. `IUbicacion` da el estado (`SinPermiso`, `Denegado`, `Buscando`, `Activa`), la `Coordenada`, el rumbo en grados o "sin brújula", y avisa de los cambios. El adaptador va en un ensamblado nuevo, `PriceMap.Adaptadores.Gps`. El Arranque compone el adaptador y `ServicioUbicacion`.
- **Marcador:** círculo `#2F5BEA` con borde blanco, por encima de los lugares y de los puntos de interés, colocado con `IMapa.Proyectar`. Con brújula lleva un cono translúcido hacia el rumbo respecto al norte real, también con el mapa girado (rumbo − `IMapa.Giro`). Se retira el marcador 3D "Tu posición (simulada)".
- **Botón:** un `BotonRedondo` con un icono nuevo de diana dibujado con `Painter2D`, en la pila derecha, debajo del de tema. Con posición, centra el mapa en el usuario. Sin posición todavía, no hace nada. Su texto para TalkBack es "Centrar en mi posición" (los demás estados son de la parte 2).
- Los textos van por Localization.

**Never:** sin GPS en segundo plano, sin ruta, sin círculo de precisión, sin seguir al usuario automáticamente y sin orientar al norte (parte 2). La bienvenida completa es de la historia 3.1.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Primera apertura | Sin preferencia guardada | Sale la explicación; queda guardado que salió |
| Siguientes aperturas | Explicación ya mostrada | No sale |
| "Ahora no" y tocar el botón | `SinPermiso` | Vuelve a salir la explicación |
| Con permiso | 36.78, -6.35; rumbo 90°; mapa sin girar | Círculo en esa posición y cono hacia el este |
| Mapa girado | Rumbo 90°, giro 40° | Cono a 50° en pantalla |
| Sin brújula | Posición sin rumbo | Solo el círculo |
| Buscando | Permiso concedido, sin primera posición | Sin círculo; tocar el botón no hace nada |
| Tocar con posición | `Activa` | El mapa se centra en el usuario |
| Denegado | `Denegado` | Mapa en `VistaInicial`, sin círculo; tocar no hace nada |
| Segundo plano / volver | Pausa / reanudación | Se detienen / se reanudan ubicación y brújula |

</frozen-after-approval>

## Code Map

- `Scripts/Aplicacion/`: `IMapa.cs` (sin giro: se añade `float Giro`), `IPreferencias.cs` (para guardar que la explicación salió). Puertos nuevos: `IUbicacion`, `EstadoUbicacion` y `ServicioUbicacion`. `ARCHITECTURE-SPINE.md:25,31,200,263` prevé `PriceMap.Adaptadores.Gps`.
- `Scripts/Adaptadores/Sistema/` (`.asmdef` y `PreferenciasLocales.cs`): de modelo para el asmdef nuevo `Adaptadores/Gps`.
- `Scripts/Vectorial/CamaraMapa.cs`: `giro` (`:21`, 0 = norte), que `MapaVectorial` expone como `Giro`. Hay que actualizar los `MapaFalso` de las pruebas.
- `Scripts/Vectorial/Marcador.cs` (`TipoMarcador.Posicion`) y `Editor/PrepararEscenaPrincipal.cs:90-99`: se retira "Tu posición (simulada)" y la escena se regenera por lotes.
- `Scripts/UI/`:
  - `BotonRedondo.cs`: `enum Icono` y sus dibujos; se añade la diana.
  - `CapaMarcadores.cs` y `MarcadorLugar.cs`: patrón de proyección para la nueva `MarcadorPosicion`.
  - `MenuPrecios.cs`: patrón de modal con TalkBack para `ExplicacionUbicacion`.
  - `PantallaMapa.cs`: `VistaInicial` (`:20`); el botón de tema se monta en `:67-68`.
  - `ClavesTexto.cs` y `Editor/PrepararInterfaz.cs` (`TextosEs`, que se vuelca por lotes).
- `UI/PantallaMapa.uxml:14` (`hueco-tema`): se añade `hueco-ubicacion` debajo.
- `Scripts/Arranque/Arranque.cs`: compone la ubicación. `CapturaPruebas.cs`: necesita una `IUbicacion` falsa para que la captura no pida permisos.
- `prototipo/app.js:464, 561-595`: referencia del indicador y de la brújula (rumbo = 360 − alpha).

## Tasks & Acceptance

**Execution:**
- [x] `Aplicacion/IUbicacion.cs`, `EstadoUbicacion.cs` y `ServicioUbicacion.cs` (estado, posición, rumbo, eventos, pausa y reanudación).
- [x] `Adaptadores/Gps/` (asmdef y `UbicacionUnity.cs`): permiso de Android, `Input.location` e `Input.compass`.
- [x] `IMapa.Giro` en `MapaVectorial` y en los `MapaFalso`.
- [x] `UI/MarcadorPosicion.cs`, icono y botón de ubicación, `UI/ExplicacionUbicacion.cs`, `PantallaMapa.uxml/.uss/.cs` y textos.
- [x] `Arranque.cs`, la `IUbicacion` falsa de `CapturaPruebas.cs` y la escena sin el marcador simulado.
- [x] `Tests/EditMode/PruebasUbicacion.cs`: cada fila de la matriz, con una `IUbicacion` falsa.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas las pruebas y ninguna lee el `Input.location` real.
- Dada la build de PC, cuando se abre la captura "mapa", entonces aparece el botón de ubicación en la pila derecha y el log no tiene errores.

## Spec Change Log

## Review Triage Log

| # | Fuente | Hallazgo | Veredicto | Evidencia | Ruta |
|---|--------|----------|-----------|-----------|------|
| 1 | blind | `respuestaPendiente` se lee y se pone a 0 en dos pasos: una respuesta del hilo de Java entre medias se pierde | low | `UbicacionUnity.Update`: lectura y escritura separadas; con Fine denegado y Coarse concedido llegan dos respuestas seguidas | patch (`Interlocked.Exchange`) |
| 2 | blind | No se escucha `PermissionDeniedAndDontAskAgain` | false | En Unity 6000.3, sin suscriptor a ese evento, Unity lanza `PermissionDenied`, que sí se escucha y pasa a `Denegado` | rechazado |
| 3 | blind + edge | Con permiso pero la ubicación del móvil apagada (o `Failed`), el estado se queda en `Buscando` para siempre | medium | `Update` solo actúa con `Running`. En la parte 1 se ve igual que buscando (sin círculo y el botón no hace nada, como dice la matriz). En la parte 2, TalkBack diría "Buscando tu posición" sin fin | defer (parte 2) |
| 4 | blind | Tras volver de segundo plano se ve la posición antigua hasta la primera lectura nueva; se ignora la precisión | low | Es la última posición conocida y dura segundos. La intención excluye la precisión ("sin círculo de precisión") | rechazado |
| 5 | blind | Brújula: no se mira `headingAccuracy` ni la orientación de la pantalla | low | La app está fija en vertical (`defaultScreenOrientation: 0`, sin autorrotar). Filtrar la precisión añade ramas | rechazado |
| 6 | blind | `OnApplicationFocus(false)` pausa también con la cortina de notificaciones | false | La intención congelada pide pausar con `OnApplicationFocus(false)` | rechazado |
| 7 | blind | TalkBack dice siempre "Centrar en mi posición" | false | La intención deja los demás estados para la parte 2 | rechazado |
| 8 | blind | La UI no escucha `ServicioUbicacion.Cambiado` y la lambda no se desuscribe | false | La pantalla lee la posición en cada fotograma, igual que los marcadores. El servicio vive lo mismo que la app. No hay daño concreto | rechazado |
| 9 | blind | Bucle de disposición por `GeometryChangedEvent` → `EscalaFuente.Aplicar` | false | `Aplicar` es idempotente (tamaño base × escala) y el menú usa el mismo patrón (`PantallaMapa.cs:218`) | rechazado |
| 10 | blind + gap | `UbicacionUnity` (permisos, estados, umbral de rumbo) no tiene pruebas | medium | Lee `Input.location`, `Input.compass` y `Permission` directamente. Probarlo exige separarlo detrás de una interfaz | defer |
| 11 | blind + gap | `PermisosUbicacionAndroid` no tiene pruebas | low | La transformación del XML es determinista y nada la comprueba. Si falta la aproximada, Android 12 ignora la petición | patch (prueba) |
| 12 | blind | `SkipPermissionsDialog` podría estar obsoleto | low | Sin efecto si Unity lo ignora. No se comprobó que esté obsoleto | rechazado |
| 13 | blind | No se prueba abrir la explicación con el menú "Precios" abierto | false | No se puede llegar: con el menú abierto, su velo tapa el botón de ubicación y TalkBack lo saca del recorrido | rechazado |
| 14 | blind | Tocar fuera de la ventana no hace nada | low | Elección de diseño (ventana que obliga a elegir). Cambiarla añade comportamiento | rechazado |
| 15 | blind | La escena regenerada llena el diff | false | El spec pide regenerarla por lotes y `PruebasEscenaPrincipal` pasa | rechazado |
| 16 | gap | `ActualizarCapas` → `MarcadorPosicion.Actualizar` no se prueba por la ruta real | low | Todas las pruebas llaman al marcador directamente | patch (prueba) |
| 17 | gap | `OnApplicationPause` y `OnApplicationFocus` → `Pausar`/`Reanudar` no se prueban | low | Las pruebas llaman al servicio directamente. Invertir la condición no rompe ninguna | patch (función pequeña y prueba) |
| 18 | gap | El rol `StaticText` y la ausencia de acción del texto de la explicación no se prueban | low | `Recorrido()` solo devuelve etiquetas | patch (prueba) |
| 19 | edge | `Iniciar` con el permiso retirado y el GPS encendido | false | `Iniciar` solo se llama al arrancar o tras `Detener` (`encendido` = false). Además, Android reinicia la app al retirar un permiso | rechazado |
| 20 | edge | `PermissionRequestDismissed` no se escucha | false | Cerrar el diálogo sin elegir deja `SinPermiso`, que es correcto: el botón vuelve a mostrar la explicación | rechazado |
| 21 | edge | Con teclado o mando, el foco detrás de la ventana puede activar el tema o la píldora | low | En un móvil sin teclado no pasa. TalkBack ya es modal | rechazado |
| 22 | edge | `botonUbicacion` null al cerrar la explicación | false | `hueco-ubicacion` está en el UXML y la explicación solo existe con ubicación | rechazado |
| 23 | edge | Manifiesto con `SkipPermissionsDialog=false` ya presente | low | El manifiesto que genera Unity no lo trae | rechazado |
| 24 | edge | Manifiesto mal formado → excepción | low | La exportación de Gradle falla igual, con el mensaje de la excepción | rechazado |
| 25 | edge | `mapa` null en `CapturaPruebas.Ubicacion()` | false | `SiSePide` solo se llama si `Componer` tuvo éxito con un mapa válido | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC y `PriceMap.exe -captura <carpeta>`: log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En un Android real: la explicación al abrir por primera vez, el permiso, el círculo con el cono y el centrado. En segundo plano no debe salir el icono de GPS.
