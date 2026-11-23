---
title: '«Cómo llegar»: elegir entre varias rutas e Iniciar (parte 5b)'
type: 'feature'
created: '2026-11-22'
status: 'ready-for-dev'
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
- `ServicioRutas` da las alternativas ordenadas (parte 5a, `spec-varias-rutas-la-mas-rapida`).
- `Scripts/UI/PantallaMapa.cs`: `Ir`, `PedirRuta`, `DibujarRuta`, `FalloRuta`, `EmpezarNavegacion` (hoy `seguimiento.Empezar` con 2 s: desde «Iniciar», sin espera), `SeguirCamara`/`PuntoSeguir` (piden `Navegando` y tarjetas visibles; antes de empezar no lo están), `ActualizarCapas` (cada fotograma, para el plazo con `RelojCamara`), `EncuadrarRuta`/`ZonaEncuadre`, `TocarMapa`, `PulsarLargo…`, `Atras`, `AlCambiarTema`, `CerrarFicha`, `ReabrirFichaRuta` (reabre la ficha con su modo, también lo elegido en el buscador, `conocidoRuta`), `CentrarAlAbrir`. Un solo estado «eligiendo ruta» que tape cápsula, píldora y tarjetas como la ficha.
- `Scripts/Aplicacion/IMapa.cs` y `Scripts/Vectorial/MapaVectorial.cs` (`MostrarRuta`, `ConstruirRuta`, `ColorearRuta`, colas 1960/1970, `PaletaMapa`): líneas de alternativas (miembros por defecto para los dobles) debajo de la elegida.
- Toque en una línea: proyectar sus puntos (`Proyectar`) y medir a los tramos en dp; regla pura. Etiquetas de minutos: capa como la de marcadores, en el punto medio de la parte propia de cada ruta.
- UI nueva `Scripts/UI/HojaComoLlegar.cs` + USS + textos (`ClavesTexto`, `PrepararInterfaz.TextosEs`, `Assets/Localizacion`); reutilizar `BotonModo`, `TarjetaRuta.HoraLlegada`, `FormatoDistancia`, tokens, contraste AA en claro y oscuro; con el texto del sistema grande la lista se desplaza.
- Las pruebas y capturas que pulsan Ir y esperan la navegación al momento pasan por «Iniciar» con una ayuda común; `CapturaPruebas.EsperarRuta`.

## Tasks & Acceptance

**Execution:**
- [ ] Mapa: líneas de alternativas, etiquetas y elegir por toque.
- [ ] `HojaComoLlegar` + `PantallaMapa` (Ir, elegir, modo, Iniciar con la cámara primero, X/atrás, TalkBack).
- [ ] Textos, estilos y contraste.
- [ ] Pruebas por fila de la matriz; capturas `como-llegar(-oscuro)` con 3 rutas falsas.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dadas las capturas `como-llegar(-oscuro)`, cuando se comparan con la maqueta A, entonces coinciden.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` (con `-buildTarget Win64`) y `PriceMap.exe -captura`; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.
