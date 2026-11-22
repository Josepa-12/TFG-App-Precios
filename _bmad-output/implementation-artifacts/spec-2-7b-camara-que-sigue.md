---
title: 'Historia 2.7 (parte 2b): la cámara sigue al usuario'
type: 'feature'
created: '2026-11-22'
status: 'done'
baseline_commit: '723f40f36e882ec893d0e408ed2098fcc8126e45'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Al navegar (2.7 parte 1) la cámara se queda quieta con la ruta entera: el usuario se sale de la vista y tiene que mover el mapa a mano, y la pantalla se apaga sola (FR20).

**Approach:** Durante la navegación la cámara sigue al usuario inclinada y con su rumbo arriba, hasta que él mueve el mapa; «Volver a centrar» la recupera. La pantalla no se apaga mientras se navega.

**Decisiones:**
- (21/11) Al pulsar Ir se ve la ruta entera (encuadre de la 2.6b) y a los 2 s la cámara pasa a seguir; con «reducir animaciones», al instante y sin transición.
- (21/11) Arriba, el rumbo de la marcha en coche y el de la brújula a pie.
- (22/11) La parte 2 se parte: esta (2.7b) es la cámara, «Volver a centrar» y la pantalla encendida; la flecha y «Buscando señal GPS» son la 2.7c.
- (22/11) «Volver a centrar» es el botón redondo de ubicación (abajo a la izquierda durante la ruta), sin texto: mientras la cámara sigue, su icono de siempre; al soltarla, un icono nuevo de recentrar y la etiqueta de TalkBack «Volver a centrar». Pulsarlo navegando siempre vuelve a seguir.
- (22/11) En coche, navegando y a ≤ 40 m de la ruta, el punto de posición va sobre la línea (el punto proyectado de la parte 1), no donde dice el GPS; fuera de ruta, la posición real. A pie, siempre la real. La cámara sigue al punto que se ve.

## Boundaries & Constraints

**Always:**
- **Seguir:** inclinación 45°, la posición en el tercio de abajo de la zona libre entre las tarjetas, distancia de cámara 350 m en coche y 180 m a pie (`ParametrosNegocio`). Transición suave (≈ 0,6 s) salvo con «reducir animaciones»; después, cada posición nueva recoloca la cámara sin saltos bruscos.
- **Soltar:** un arrastre, giro, inclinación o pellizco del usuario (no un toque) y los botones de acercar/alejar dejan de seguir y el botón de ubicación pasa a «Volver a centrar»; pulsarlo vuelve a seguir. No se vuelve a centrar solo.
- **Rumbo de la marcha** (regla pura en Dominio, la reutilizará la flecha de la 2.7c): rumbo entre dos posiciones a ≥ 3 m; velocidad con su distancia y tiempo (reloj inyectable); con ≤ 1 m/s se queda el último. A pie, la brújula (`IUbicacion.Rumbo`); sin brújula, el de la marcha. Sin rumbo todavía, el de la ruta en el punto proyectado.
- **Pantalla encendida** mientras se navega (`Screen.sleepTimeout = NeverSleep`); al terminar, el valor del sistema. Por un puerto de Aplicación.
- **Al terminar** (X, atrás, otra ruta): deja de seguir, la cámara se queda donde está sin inclinación forzada y el botón de ubicación vuelve a su icono y su comportamiento de siempre.
- **TalkBack:** el botón (≥ 48 dp) se llama «Volver a centrar» al soltar y «Siguiendo tu posición» mientras sigue.
- Umbrales en `ParametrosNegocio`; textos en Localization.

**Never:** flecha y sin señal (2.7c), voz (2.8), llegada (2.9), cambiar el seguimiento de la parte 1, tocar el servicio de Android, ampliar `IUbicacion`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Empezar | Ir en coche | Ruta entera; a los 2 s, 45°, 350 m, siguiendo | Reducir animaciones: al instante |
| Coche rumbo | Dos posiciones hacia el este a 5 m/s | Giro de cámara 90° (el este arriba) | — |
| Parado | 0,5 m/s tras ir al este | Se queda en 90° | — |
| A pie | Brújula a 200° | Giro 200°, 180 m | Sin brújula: el de la marcha |
| Coche en ruta | GPS a 8 m de la línea | Punto y cámara sobre la línea | A 45 m (fuera de ruta): la posición real |
| A pie en ruta | GPS a 8 m de la línea | Punto en la posición real | — |
| Arrastre | El usuario mueve el mapa | Deja de seguir; el botón de ubicación pasa a «Volver a centrar» | Un toque no suelta |
| Recentrar | Pulsa el botón | Sigue otra vez; el botón vuelve a su icono | — |
| Terminar | X o atrás | No sigue; botón de siempre; pantalla con su valor | — |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts`.
- `Vectorial/CamaraMapa.cs` — estado `pivote/distancia/inclinacion/giro` (l.20-29; `inclinacion` desde la horizontal: 85 cenital, rango 20-85), `Aplicar` (l.143), `CentrarEn(lat,lon,objetivo)` (l.235-260), `Encuadrar` (l.273). Hoy todo es al instante: añadir la transición (interpolar en `Update`) y avisar del gesto del usuario desde `Tactil/Raton/Teclado` (l.171-223), no desde el toque de `DetectorToque`.
- `Aplicacion/IMapa.cs` — añadir `Seguir(posicion, giro, inclinacion, distancia, enPantalla, animado)` y el evento `GestoUsuario` **como miembros por defecto de la interfaz**, para no tocar los 17 `MapaFalso` de las pruebas (solo el de `PruebasNavegacion`, l.290). `Vectorial/MapaVectorial.cs` (l.186-263) los pasa a la cámara.
- Nuevo `Dominio/ReglaRumbo.cs`. `IUbicacion` no da velocidad ni rumbo de la marcha (Unity no los da en primer plano); no ampliar el puerto. El servicio que la usa recibe las posiciones de `ServicioUbicacion.Cambiado`.
- `Aplicacion/IAccesibilidadSistema.AnimacionesQuitadas` — se lee en `PantallaMapa.LeerAccesibilidadSistema` (l.1083); usarlo para la transición y los 2 s.
- `UI/PantallaMapa.cs` — `EmpezarNavegacion` (l.1614), `TerminarNavegacion` (l.1624), `AlNavegar` (l.1642), `ColocarControlesRuta` (l.1837), columnas y botón de ubicación (l.134-154; `TocarUbicacion` l.302 y `ClaveBotonUbicacion` l.337: navegando, ramas propias antes de `ServicioUbicacion.AlTocarBoton`), `UI/BotonRedondo.cs` (iconos Sol/Luna/Mas/Menos/Navegar/Cerrar, l.7: añadir `Recentrar`, dibujado como los demás). Seguimiento como clase pura nueva `UI/SeguimientoCamara.cs` (esperando 2 s → siguiendo → suelto), con reloj inyectable.
- Punto ajustado: `PantallaMapa.cs` l.2386 pasa la posición a `MarcadorPosicion.Actualizar`; en coche, navegando y sin `EstadoNavegacion.FueraDeRuta`, pasar `navegacion.Estado.Proyectado`. Los puntos de interés y marcadores ya son UI proyectada (`CapaPuntosInteres` l.126): con la cámara inclinada siguen derechos, no tocarlos.
- Nuevo puerto `Aplicacion/IPantallaSistema.cs` (`MantenerEncendida(bool)`) y adaptador `Adaptadores/Sistema/PantallaSistemaUnity.cs`; en `Arranque/Arranque.cs` (`Componer`, parámetro opcional).
- `Dominio/ParametrosNegocio.cs`, `UI/ClavesTexto.cs`, `Editor/PrepararInterfaz.TextosEs` y tabla es-ES, `UI/PantallaMapa.uss`, `Arranque/CapturaPruebas.cs` (capturas `navegacion-sigue` y `navegacion-sigue-oscuro`, y `volver-centrar`).
- Pruebas: nuevo `Tests/EditMode/PruebasCamaraNavegacion.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `Dominio/ReglaRumbo.cs` + `ParametrosNegocio` -- rumbo de la marcha y velocidad -- pura, con pruebas.
- [x] `IMapa`, `CamaraMapa`, `MapaVectorial` -- seguir con transición y evento de gesto -- miembros por defecto; pruebas de la cámara.
- [x] `SeguimientoCamara`, `PantallaMapa`, USS -- 2 s, seguir, soltar, «Volver a centrar», TalkBack -- pruebas de la matriz en pantalla.
- [x] `IPantallaSistema` + adaptador + `Arranque`; textos; capturas; memoria del TFG; CLAUDE.md.

**Acceptance Criteria:**
- Given la suite EditMode, when se ejecuta sin red, then pasan todas.
- Given la captura `navegacion-sigue` (claro y oscuro), when se abre, then la cámara está inclinada, la posición en el tercio de abajo sobre la línea, la ruta delante y los puntos de interés derechos.
- Given la captura `volver-centrar`, when se abre, then tras mover el mapa el botón de ubicación lleva el icono de recentrar.

## Implementation Notes

- Transición: `CamaraMapa` persigue el objetivo con suavizado exponencial (τ = 0,6 s / 3: al 95 % en 0,6 s), el giro con `LerpAngle`; vale para el paso inicial y para cada posición nueva. Sin animar, al instante.
- Gesto: se compara el estado de la cámara antes y después de leer la entrada; el pivote movido por un dedo o el botón izquierdo cuenta solo pasado `DetectorToque.UmbralPx` desde que bajó (un toque no suelta). `IMapa` también gana `DejarDeSeguir` (miembro por defecto).
- Altura: `CamaraSeguirAlturaZona = 0,75` (dentro del tercio de abajo de la zona libre). Un gesto en los 2 s de espera también suelta. Con la ficha abierta no se sigue (`PrepararFicha` llama a `DejarDeSeguir`); al cerrarla, `EncuadrarOSeguir`. Con la cámara siguiendo o suelta, el re-encuadre al medirse las tarjetas no se hace.
- Pantalla encendida: `Aplicacion/PantallaEncendida` (RutaIniciada → true, RutaTerminada → false), creada en `Arranque.Componer` con el parámetro opcional `pantallaSistema`.
- USS: sin cambios (el botón ya mide 48 dp).

## Spec Change Log

## Review Triage Log

Revisión del 22/11/2026 (blind, edge-case, verification-gap; diff del código desde `723f40f`).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | En coche parado, el ruido del GPS (≥ 3 m en 1 s) da rumbos al azar y la cámara gira en un semáforo | medium | `AlNavegar` alimentaba `ReglaRumbo` con la posición real; 5 m en 1 s pasan los dos umbrales | patch (en coche y en ruta, con el punto proyectado, que no retrocede) |
| 2 | A pie, la brújula solo gira la cámara al llegar una posición; parado no gira nunca | medium | `SeguirCamara` solo se llamaba desde `AlNavegar`; `ServicioNavegacion` descarta las posiciones repetidas | patch (recolocar en el fotograma si la brújula se mueve ≥ 2°) |
| 3 | `ReglaRumbo.Velocidad` puede quedar en `PositiveInfinity` (misma hora o reloj hacia atrás) | low | `segundos > 0 ? … : PositiveInfinity` en `Nueva`; valor público para la 2.7c | patch |
| 4 | La detección real del gesto en `CamaraMapa.Update` no la prueba nada | low | Las pruebas usan `SimularGesto`; quitar la línea del `Gesto()` no rompe ninguna | patch (función pura `EsGesto` + pruebas) |
| 5 | Sin pruebas de ficha abierta navegando (deja de seguir, al cerrar vuelve; suelta, no recoloca) | low | Ninguna prueba cierra una ficha siguiendo ni mira `dejados` | patch (pruebas) |
| 6 | Sin prueba de que el marcador recibe `PosicionMostrada` | low | La prueba de coche en ruta mira la propiedad y la cámara, no el marcador | patch (prueba) |
| 7 | Sin prueba de «reducir animaciones» releído al volver a la app | low | La prueba existente lo pone antes de `Inicializar` | patch (prueba) |
| 8 | Siguiendo en coche, la cámara avanza y se para entre posiciones (τ 0,2 s, posiciones cada ~1 s) | maybe-false | Depende de cómo se note en el móvil; si se nota, medium | defer |
| 9 | El paso de `PantallaSistemaUnity` en `Arranque.Start` no tiene prueba | low | Solo se puede probar con escena y Android; lo cubre la comprobación manual en el móvil | defer |
| 10 | Un cambio de distancia/inclinación/giro sin entrada cuenta como gesto | false | La cámara no tiene inercia ni animación dentro de la lectura de la entrada; `AvanzarSeguimiento` corre después de comparar | rechazado |
| 11 | `arrastreLargo` no se borra al levantar el dedo | low | El pivote solo cambia con un arrastre, que empieza con `EmpezarArrastre` | rechazado |
| 12 | Dedo quieto en el mapa (bajo el umbral) congela la cámara sin soltarla | low | `Arrastrar` fija el suelo bajo el dedo mientras se toca; dura lo que dura el toque | rechazado |
| 13 | `PosicionMostrada` enseña el último punto proyectado sin GPS | low | Sin señal es la 2.7c (última posición atenuada) | rechazado |
| 14 | Navegando, el botón de ubicación ya no abre la explicación ni los ajustes | low | Perder el permiso a mitad de ruta es raro; el arreglo añade ramas | rechazado |
| 15 | «Siguiendo tu posición» en los 2 s de espera o con la ficha abierta | low | 2 s; con la ficha (modal) el botón no está en el recorrido | rechazado |
| 16 | Al terminar, la cámara se queda inclinada y girada | false | Es lo del spec: «se queda donde está sin inclinación forzada» | rechazado |
| 17 | Sin prueba de cambio de tamaño de pantalla siguiendo; la prueba del punto no mira las tarjetas | low | En EditMode las tarjetas no tienen tamaño; la captura lo comprueba | rechazado |
| 18 | `PantallaEncendida` no se desuscribe ni mira el estado inicial | low | Vive lo que la app y se crea antes de cualquier ruta | rechazado |
| 19 | La tabla de Localization no aparece en el diff | false | `ubicacion.recentrar`/`ubicacion.siguiendo` están en `Textos Shared Data.asset` y en `Textos_es-ES.asset` (l.732, 736) | rechazado |
| 20 | El punto del icono `Recentrar` sin color de relleno | false | `BotonRedondo` pone `fillColor` (l.62) antes de dibujar | rechazado |
| 21 | Coordenadas de pantalla con `Screen.height` frente a `pixelHeight` | false | La misma convención que `Encuadrar` (2.6b); la cámara ocupa la pantalla | rechazado |
| 22 | `LosParametrosEstanEnParametrosNegocio` no prueba comportamiento | low | Es la prueba de siempre de cada historia | rechazado |
| 23 | Reloj hacia atrás en `ReglaRumbo` | low | Lo cubre el arreglo 3 | rechazado (incluido en 3) |
| 24 | Zona libre degenerada en `PuntoSeguir` | false | `ZonaEncuadre` ya devuelve la pantalla entera si la zona no es válida (l.2069) | rechazado |
| 25 | Un `Centrar` del buscador navegando pelearía con el seguimiento | false | Con ruta no se ven ni la cápsula ni el buscador (2.6b) | rechazado |
| 26 | Navegando no se puede orientar al norte | low | Con rumbo arriba es lo buscado; al terminar vuelve | rechazado |
| 27 | La tarea dice USS y no cambia ningún USS | low | El botón ya medía 48 dp; no hacía falta | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- Build de PC y capturas; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: ruta en coche y a pie; la cámara gira con la marcha o la brújula, arrastrar la suelta y la pantalla no se apaga.
