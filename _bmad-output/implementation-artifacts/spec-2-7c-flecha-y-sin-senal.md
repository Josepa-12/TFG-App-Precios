---
title: 'Historia 2.7 (parte 2c): flecha en coche y «Buscando señal GPS»'
type: 'feature'
created: '2026-11-22'
status: 'done'
baseline_commit: 'cfce1002a85cba9673e886cec1cab05b5f18cf82'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Navegando en coche la posición sigue siendo el círculo con el cono de la brújula, que no dice hacia dónde vas; y si se pierde el GPS no se nota nada (FR20, UX-DR8, UX-DR16).

**Approach:** En una ruta en coche, la posición es una flecha con el rumbo de la marcha. Navegando, tras 10 s sin lecturas del GPS, la posición se atenúa y sale una pastilla pequeña «Buscando señal GPS» bajo la tarjeta de arriba; no se recalcula hasta que vuelve.

**Decisiones (22/11/2026, del usuario):**
- La flecha solo sale navegando en coche. A pie y sin ruta, el círculo con el cono de siempre. Esto sustituye lo del 21/11 («la flecha también a pie»).
- El aviso va bajo la tarjeta de arriba, pequeño, sin ocupar mucho espacio.

## Boundaries & Constraints

**Always:**
- **Flecha:** relleno `#2F5BEA` (oscuro `#4C7DFF`) con borde blanco, unos 26 dp, en el sitio del círculo (el punto que se ve de la 2.7b) y sin cono. Apunta al rumbo de la marcha de `ReglaRumbo` (2.7b) y, sin él todavía, al de la ruta. Gira respecto al norte (rumbo − giro del mapa): con la cámara siguiendo, hacia arriba.
- **Sin señal** (coche y a pie, solo navegando): más de 10 s sin una lectura nueva del GPS (también cuenta una lectura con la misma coordenada: un coche parado no está sin señal). Entonces la posición (flecha o círculo) se queda en su último sitio con opacidad 0,4, sale la pastilla y no se recalcula (el contador de desvío vuelve a cero). Con la primera lectura nueva, todo como antes y la pastilla se va.
- **Pastilla:** una línea, texto pequeño, centrada bajo la tarjeta de arriba, estilo del toast pero más baja y sin desaparecer sola. Con la ficha abierta no se ve.
- **TalkBack:** anuncia «Buscando señal GPS» al aparecer y «Señal GPS recuperada» al irse, una vez cada uno.
- Al terminar la ruta: círculo y cono de siempre, sin atenuar, sin pastilla.
- Umbral de 10 s en `ParametrosNegocio`; textos en Localization; contraste ≥ 4,5:1 en los dos temas.

**Never:** cosméticos de la flecha (épica 5), voz (2.8), llegada (2.9), tocar el servicio de Android, cambiar la cámara de la 2.7b.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Coche | Navegando, rumbo de la marcha 90° | Flecha hacia el este (arriba con la cámara siguiendo); sin cono | Sin rumbo aún: el de la ruta |
| A pie | Navegando a pie | Círculo con cono | — |
| Sin ruta | Sin navegar | Círculo con cono | — |
| Sin señal | 12 s sin lecturas | Posición a 0,4 y pastilla; TalkBack lo anuncia; sin recálculo | — |
| Parado | 60 s con lecturas de la misma coordenada | Sin pastilla | — |
| Vuelve | Lectura nueva tras la pastilla | Normal; «Señal GPS recuperada»; el desvío cuenta desde cero | — |
| Terminar | X con la pastilla | Círculo normal, sin pastilla | — |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts`.
- `Aplicacion/IUbicacion.cs` — añadir un miembro por defecto con la hora o un contador de la última lectura del GPS (las falsas de las pruebas no cambian). `Adaptadores/Gps/UbicacionGps.cs`: cada lectura nueva de Unity (`marca` nueva, l.265-271) o de la cola del servicio (`RecogerRuta`, l.175) lo actualiza aunque la coordenada no cambie (`Cambiar`, l.282, sale si nada cambia).
- `Aplicacion/ServicioNavegacion.cs` — `SinSenal` y evento `SenalCambiada`; comprobación con su reloj desde `PantallaMapa` (cada fotograma, `ActualizarCapas`); con señal perdida, `desvioDesde = null` y no recalcular.
- `UI/MarcadorPosicion.cs` — modo flecha (Painter2D como el cono, l.110-124) y atenuado; `AnguloEnPantalla` (l.56). Colores y tamaños en `UI/Estilos/Tokens.uss` (l.48-50, 140, 160-161) y `UI/PantallaMapa.uss` (l.357-398).
- `UI/PantallaMapa.cs` — l.~2564 `marcadorPosicion.Actualizar(mapa, PosicionMostrada, …)`: en coche navegando, flecha con `RumboCamara`/`rumboMarcha`; pastilla nueva bajo `tarjetaManiobra` (como `ColocarToast`, `UI/Toast.cs`), TalkBack con `AccesibilidadPantalla.Anunciar`.
- `Dominio/ParametrosNegocio.cs`, `UI/ClavesTexto.cs`, `Editor/PrepararInterfaz.TextosEs` y tabla es-ES; `Arranque/CapturaPruebas.cs` (capturas `navegacion-flecha`, `navegacion-flecha-oscuro`, `sin-senal`).
- Pruebas: nuevo `Tests/EditMode/PruebasFlechaYSenal.cs`; `Tests/EditMode/PruebasContraste.cs` para la pastilla.

## Tasks & Acceptance

**Execution:**
- [x] `IUbicacion` + `UbicacionGps` -- última lectura -- pruebas con la fuente falsa.
- [x] `ServicioNavegacion` + `ParametrosNegocio` -- sin señal, sin recálculo -- pruebas con reloj falso.
- [x] `MarcadorPosicion`, `PantallaMapa`, USS -- flecha, atenuado, pastilla, TalkBack -- pruebas de la matriz en pantalla y contraste.
- [x] Textos; capturas; memoria del TFG; CLAUDE.md.

**Acceptance Criteria:**
- Given la suite EditMode, when se ejecuta sin red, then pasan todas.
- Given las capturas `navegacion-flecha` (claro y oscuro) y `sin-senal`, when se abren, then se ve la flecha hacia arriba sin cono, y la flecha atenuada con la pastilla pequeña bajo la tarjeta de arriba.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Revisión del 22/11/2026 (blind, edge-case, verification-gap; diff del código desde `cfce100`).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | `UbicacionUnity` (el adaptador de la app) no reenvía `Lecturas`: vale 0 y un coche parado > 10 s sale «sin señal» | high | `UbicacionUnity.cs` reenvía todo menos `Lecturas`; las pruebas y la captura usan sus propias ubicaciones | patch (+ prueba por reflexión de que declara todo `IUbicacion`) |
| 2 | Al volver de segundo plano, el tiempo en pausa cuenta y sale «Buscando señal GPS» si el GPS tarda | medium | `ultimaLectura` con el reloj de pared; `LateUpdate` no corre en pausa | patch (un salto de más de 1 s entre comprobaciones reinicia la cuenta) |
| 3 | Un recálculo en marcha al perder la señal se aplica o avisa de fallo (toast en un túnel); el reintento salta al volver la señal sin que el desvío cuente desde cero | medium | `Recalcular` no miraba `SinSenal` tras el `await`; al despertar no miraba `desvioDesde` | patch |
| 4 | `Pantalla_ElToastVaBajoLaPastilla` pasaría con el toast encima de la pastilla | low | Solo compara los `top` (12 > 8) | patch (prueba) |
| 5 | Falta la fila «sin señal a pie» (círculo atenuado) | low | Ninguna prueba de pantalla a pie sin señal | patch (prueba) |
| 6 | Sin captura `sin-senal` en oscuro | low | Regla del proyecto: claro y oscuro | patch |
| 7 | La flecha apunta al norte sin rumbo | false | La ruta siempre tiene tramos: `RumboCamara` no es null navegando | rechazado |
| 8 | El toast no se recoloca si la pastilla se mueve o al terminar | low | El toast dura 3 s; la tarjeta cambia de alto rara vez con él a la vista | rechazado |
| 9 | Aviso de TalkBack con la ficha (modal) abierta | low | Raro; el aviso sigue siendo cierto | rechazado |
| 10 | Con la ubicación apagada a mitad de ruta sale «Buscando señal GPS» | low | Raro; el arreglo añade una rama por estado | rechazado |
| 11 | Las posiciones de la cola cuentan como señal aunque lleguen tarde | false | Son lecturas reales de la pantalla apagada | rechazado |
| 12 | La flecha no gira sobre el punto exacto | low | La diferencia es de 1–2 dp | rechazado |
| 13 | `style.top` de la pastilla se escribe cada fotograma | low | Sin efecto visible | rechazado |
| 14 | La pastilla no está en el recorrido de TalkBack | low | El spec pide anunciarla al aparecer y al irse; se hace | rechazado |
| 15 | La prueba del `top` de la pastilla no puede fallar en EditMode | low | Sin maquetación en EditMode; la captura lo comprueba | rechazado |
| 16 | Textos, memoria y CLAUDE.md no aparecen en el diff | false | El diff de revisión excluye `.asset` y documentos; están cambiados | rechazado |

Segunda revisión del 22/11/2026 (mismas tres capas, código tras los arreglos de la primera).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 17 | Sin el servicio de ruta (notificaciones denegadas, o no arranca), el GPS de Unity con 2 m de distancia mínima no da lecturas parado: «Buscando señal GPS» en un semáforo | medium | `FuenteUbicacionUnity` pedía `Start(10, 2)`; `UbicacionGps` solo cuenta una `marca` nueva | patch (distancia mínima 0, como el servicio) |
| 18 | Al volver la señal con el toast a la vista, ninguna prueba comprueba que el toast sube | low | `Pantalla_ElToastVaBajoLaPastilla` solo cubre la pastilla al aparecer | patch (prueba) |
| 19 | El comentario de `desvioAntes` solo habla de la señal; también cambia al volver a la ruta y desviarse otra vez | low | Al salir, `Seguir` lanza el siguiente recálculo con el desvío ya contado | patch (comentario) |
| 20 | Recálculo en marcha: se pierde y vuelve la señal antes de la respuesta, y la ruta se aplica | low | La ruta se calcula desde un punto de segundos antes y se proyecta la posición actual; el arreglo añade estado | rechazado |
| 21 | Con la ubicación apagada o sin permiso sale «Buscando señal GPS» | low | carried: fila 10 | rechazado |
| 22 | Un parón de más de 1 s con la app delante reinicia la cuenta | low | Solo retrasa el aviso lo que dure el parón; es el arreglo de la fila 2 | rechazado |
| 23 | `IUbicacion.Lecturas => 0` por defecto esconde un adaptador que lo olvide | low | El único adaptador real lo reenvía y la prueba por reflexión lo protege | rechazado |
| 24 | El toast no se recoloca si la pastilla se mueve con la tarjeta o al terminar | low | carried: fila 8 | rechazado |
| 25 | Aviso de TalkBack con la ficha abierta (pastilla oculta) | low | carried: fila 9 | rechazado |
| 26 | La flecha apunta al norte sin `RumboCamara` | false | carried: fila 7 | rechazado |
| 27 | En segundo plano no se detecta la falta de señal | low | No hay pastilla que ver; al volver la cuenta empieza de cero (fila 2) | rechazado |
| 28 | La pastilla no está en el recorrido de TalkBack | low | carried: fila 14 | rechazado |
| 29 | El contraste de la flecha atenuada no se mide | false | La opacidad 0,4 la pide el spec; el contraste se pide para la pastilla | rechazado |
| 30 | La pastilla con `nowrap` puede salirse con fuentes muy grandes | low | Texto corto («Buscando señal GPS»); raro | rechazado |
| 31 | Parado fuera de ruta tras volver la señal no se recalcula hasta moverse | false | Igual que antes de la historia: `Seguir` solo corre con una posición nueva | rechazado |
| 32 | La captura `NavegacionFlecha` no deja la cámara como estaba si sale por error | low | Solo en la captura de PC y por un camino de error | rechazado |
| 33 | `CLAUDE.md` no lista `sin-senal-oscuro` y `deferred-work` sigue con la flecha pendiente | low | Documentos, no código | se corrige al cerrar la historia |

Al verificar, 4 pruebas del servicio de la primera revisión fallaban: su ayudante `Ir` avanzaba el reloj sin `Comprobar` y el arreglo de la fila 2 lo tomaba por una vuelta de segundo plano. `Ir` ahora hace también un fotograma, como la app. Suite: 1013/1013.

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- Build de PC y capturas; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: en coche la flecha gira con la marcha; en un túnel o un garaje sale la pastilla y se va al salir.
