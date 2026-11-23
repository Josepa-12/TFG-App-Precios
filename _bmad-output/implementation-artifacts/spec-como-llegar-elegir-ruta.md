---
title: '«Cómo llegar»: elegir entre varias rutas e Iniciar (parte 5b)'
type: 'feature'
created: '2026-11-22'
status: 'done'
baseline_commit: '35290b0efde85fa792851c132a529c1eb39986fb'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Hoy «Ir» en la ficha empieza a navegar al momento por una sola ruta: la voz y la tarjeta de la maniobra salen antes de que la cámara se acerque al cursor, y no se puede elegir otra ruta (prueba del usuario, 22/11/2026).

**Approach:** «Ir» abre la hoja **«Cómo llegar»** (maqueta, opción A: https://claude.ai/artifact/GooW4qUWFu8cp3PmwfMR38): el mapa encuadra **todas las rutas enteras** y la hoja lista hasta 3, cada una con su botón **«Iniciar»**. Al pulsarlo, la cámara se acerca al cursor y, cuando ha llegado, empieza la navegación de hoy.

## Boundaries & Constraints

**Always:**
- Decidido (usuario): «Iniciar» en cada tarjeta; tocar una tarjeta o una línea del mapa la elige (resaltada en los dos sitios) sin empezar. La más rápida sale la primera y elegida (23/11/2026).
- Mapa: la elegida en azul fuerte encima; las otras más claras y finas; en cada línea una etiqueta con sus minutos. Todas encuadradas entre el borde de arriba y la hoja.
- Decidido (usuario): al pulsar «Iniciar», primero la cámara se acerca y sigue al cursor (como «Volver a centrar»); la tarjeta de la maniobra, la hoja de la ruta y la voz salen cuando ha llegado (menos de 1 s; sin animaciones, al momento).
- Hoja: «Cómo llegar», «a <destino>», X (vuelve la ficha del destino); debajo, coche / a pie con sus minutos (cambiar de modo pide sus rutas); cada tarjeta: minutos, «Llegada HH:MM · km» y «Más rápida» en la primera si hay más de una.
- Atrás = la X. TalkBack: hoja modal; cada tarjeta se lee «7 minutos, llegada 22:54, 2,8 km, más rápida» (seleccionada) y su «Iniciar» aparte; al cerrar, el foco vuelve a Ir de la ficha.
- Sin posición, Ir hace lo de hoy (permiso o aviso). Con una navegación en marcha, abrir «Cómo llegar» no la termina; la termina «Iniciar»; con X o atrás vuelve su línea.
- El recálculo, la voz y la hoja de la ruta no cambian.

**Never:** guardar rutas (FR17); elegir otra ruta navegando; que un toque en el mapa fuera de las líneas cierre la hoja o abra una ficha.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Ir | Ficha con posición | Hoja con «Calculando…» y luego 1–3 tarjetas; todas encuadradas |
| Elegir | Tocar la 2.ª tarjeta o su línea (24 dp) | Pasa a resaltada; no empieza |
| Iniciar | «Iniciar» de la 2.ª | Cámara al cursor; luego tarjetas y voz de esa ruta |
| Iniciar y gesto | El usuario mueve el mapa antes de llegar la cámara | Empieza igual, con la cámara suelta |
| Una ruta | Sin alternativas | Una tarjeta sin «Más rápida» |
| Cambiar de modo | Tocar «A pie» | Fuera las de coche, «Calculando…», las de a pie |
| Sin red | Solo aproximada | Una tarjeta con «≈»; Iniciar con el toast de hoy |
| Fallo | Sin rutas | Texto del fallo, sin tarjetas ni líneas; X |
| Cerrar | X o atrás | Sin líneas; vuelve la ficha con su modo |
| Respuesta tardía | Se cierra o cambia de modo antes de llegar | No cambia nada |
| Giro o tema | Cambia el tema con la hoja abierta | Líneas y hoja con los colores nuevos |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `ServicioRutas.Alternativas` da las rutas ordenadas (parte 5a). Al llegar, los minutos del modo elegido en la hoja son los de la primera (la ficha puede decir unos segundos de diferencia).
- `Scripts/UI/PantallaMapa.cs`: `Ir`, `PedirRuta`, `DibujarRuta`, `FalloRuta`, `EmpezarNavegacion` (hoy `seguimiento.Empezar` con 2 s: desde «Iniciar», sin espera), `SeguirCamara`/`PuntoSeguir` (piden `Navegando` y tarjetas visibles; antes de empezar no lo están), `ActualizarCapas` (cada fotograma, para el plazo con `RelojCamara`), `EncuadrarRuta`/`ZonaEncuadre`, `TocarMapa`, `PulsarLargo…`, `Atras`, `AlCambiarTema`, `CerrarFicha`, `ReabrirFichaRuta` (reabre la ficha con su modo, también lo elegido en el buscador, `conocidoRuta`), `CentrarAlAbrir`. Un solo estado «eligiendo ruta» que tape cápsula, píldora y tarjetas como la ficha.
- `Scripts/Aplicacion/IMapa.cs` y `Scripts/Vectorial/MapaVectorial.cs` (`MostrarRuta`, `ConstruirRuta`, `ColorearRuta`, colas 1960/1970, `PaletaMapa`): líneas de alternativas (miembros por defecto para los dobles) debajo de la elegida.
- Toque en una línea: proyectar sus puntos (`Proyectar`) y medir a los tramos en dp; regla pura. Etiquetas de minutos: capa como la de marcadores, en el punto medio de la parte propia de cada ruta.
- UI nueva `Scripts/UI/HojaComoLlegar.cs` + USS + textos (`ClavesTexto`, `PrepararInterfaz.TextosEs`, `Assets/Localizacion`); reutilizar `BotonModo`, `TarjetaRuta.HoraLlegada`, `FormatoDistancia`, tokens, contraste AA en claro y oscuro; con el texto del sistema grande la lista se desplaza.
- Las pruebas y capturas que pulsan Ir y esperan la navegación al momento pasan por «Iniciar» con una ayuda común; `CapturaPruebas.EsperarRuta`.

## Tasks & Acceptance

**Execution:**
- [x] Mapa: líneas de alternativas, etiquetas y elegir por toque.
- [x] `HojaComoLlegar` + `PantallaMapa` (Ir, elegir, modo, Iniciar con la cámara primero, X/atrás, TalkBack).
- [x] Textos, estilos y contraste.
- [x] Pruebas por fila de la matriz; capturas `como-llegar(-oscuro)` con 3 rutas falsas.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dadas las capturas `como-llegar(-oscuro)`, cuando se comparan con la maqueta A, entonces coinciden.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Revisión del 23/11/2026 (tres capas; BH = ciega, EC = casos límite, VG = huecos de verificación).

| # | Hallazgo | Veredicto | Ruta y prueba |
|---|----------|-----------|---------------|
| VG1 | El doble del mapa no distingue `null` (dejar de elegir) de lista vacía (una ruta) | medium | patch: el doble guarda el valor tal cual y se comprueba en Iniciar, cerrar y una ruta |
| VG2 | Colas subidas sin probar en el orden de la app (alternativas antes de los materiales) | medium | patch: segundo mapa nuevo en la prueba |
| VG3 | Los minutos de la ficha que llegan con la hoja abierta no se prueban | medium | patch: tiempos a mano y dos pruebas |
| VG4 | La dirección inversa que llega con la hoja abierta no se prueba | medium | patch: prueba en PruebasRutas |
| VG5 | Rumbo y distancia al acercarse se comprueban con los valores del respaldo | medium | patch: giro 77 del mapa y comprobación antes de llegar |
| VG6, BH14 | `EtiquetaEn` sin probar | medium | patch: capa en un panel real (dentro, fuera, pisadas) |
| VG7, BH14 | `encuadreSuelto` sin probar | medium | patch: medir, gesto, cambio de modo |
| VG8 | Perder la posición con la hoja abierta no se prueba | medium | patch: prueba |
| BH14 | Ir con la cámara aún de camino, sin probar | low | patch: prueba (la navegación empieza debajo y la línea es la de la hoja) |
| VG9, BH15, EC10 | `MostrarFallo` de las dos tarjetas y `RutaAnuncioFallo` sin uso; el comentario de la X | low | patch: borrados, con su texto de la tabla |
| EC1 | El modo dejado a medias se queda en «…» | medium | patch: vuelve a lo que dice la ficha |
| EC2 | Un tiempo de la ficha que falla tarde pisa con «—» los minutos ya vistos | low | patch: una condición más |
| BH9a, EC9 | Los «Iniciar» se leen todos igual en TalkBack | medium | patch: «Iniciar, 7 minutos, llegada…» |
| BH9c | Al cambiar de modo se anuncia «sin tiempo» cuando se está calculando | low | patch: «…» antes del anuncio |
| BH8 | El cursor no es flecha hasta que llega la cámara | low | patch: flecha también al acercarse |
| BH12 | El encuadre deja fuera al usuario o al destino si están apartados de la calle | low | patch: sus puntos entran en el encuadre |
| BH11 | Al cerrar con X no vuelve el zoom | low | defer: encuadrar cambia la distancia desde la 2.6b y `IMapa` no tiene cómo reponerla |
| BH1, EC5 | Rutas y horas sin refrescar con la hoja abierta mucho rato | low | rechazado: poco habitual, la navegación recalcula y enseña la hora real; pide estado nuevo |
| BH2, EC3 | Sin reintento tras un fallo | low | rechazado: la matriz fija «texto del fallo y X»; X e Ir reintenta |
| BH3, EC4 | Posición perdida y recuperada con la hoja abierta: no se pide sola | low | rechazado: raro, y X e Ir lo resuelve |
| BH4, EC6 | Bloquear en los 0,9 s tras Iniciar: no empieza hasta volver | low | rechazado: ventana de 0,9 s; avisado al usuario como límite |
| BH5, EC7 | Toast de ruta aproximada perdido si se abre una ficha en los 0,9 s | low | rechazado: raro, y la tarjeta ya dice «Ruta aproximada» |
| BH6 | La cámara se recoloca un poco al medirse las tarjetas | low | rechazado: transición suave de unos dp; las capturas no lo enseñan |
| BH7, EC11 | La llegada es por tiempo (0,9 s) | false | El spec pide «menos de 1 s» y, con un gesto, «empieza igual»: un plazo fijo es lo que lo garantiza |
| BH9b, d | No se anuncia cuántas rutas hay; 0,9 s sin foco | low | rechazado: el recorrido enseña las tarjetas y el foco llega con la de la maniobra |
| BH10 | Los minutos de la ficha no se actualizan con los de la hoja | low | rechazado: el Code Map lo acepta («unos segundos de diferencia») |
| BH13, EC8 | Etiquetas de minutos que se pisan | low | rechazado: van en la parte propia de cada ruta; la pisada se elige por su línea o su tarjeta |
| BH16 | Las rutas falsas de la captura repiten las maniobras de la primera | low | rechazado: solo datos de la captura |

Sin intent_gap ni bad_spec. 16 arreglos, 1 diferido.

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` (con `-buildTarget Win64`) y `PriceMap.exe -captura`; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.
