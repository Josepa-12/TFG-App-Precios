---
title: 'Historia 2.9: llegar y terminar la ruta'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '63b4f1ff6df574e0a0d8850647ce08cea9539eb0'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La navegación no detecta la llegada: al llegar al destino sigue como si nada hasta que se finaliza a mano (FR22, AD-19). Además, el botón atrás corta la ruta sin querer.

**Approach:** Al llegar sale «Has llegado» (maqueta, opción B: https://claude.ai/artifact/G24QJ9QrB8u83kDUaajJUs): tarjeta arriba, en el sitio de la de la maniobra, y hoja abajo solo con el botón «Finalizar ruta» y el aviso de que se cierra sola al minuto. Atrás deja de terminar la ruta.

## Boundaries & Constraints

**Always:**
- Decidido (usuario, 23/11/2026): se llega a 30 m o menos, en línea recta, del destino o del final de la línea de la ruta. Una vez por ruta; solo con posición del GPS (sin señal no se llega). Tras llegar no hay recálculo, ni maniobras, ni vuelta a «navegando».
- Decidido (usuario, 23/11/2026): la voz dice una vez «Has llegado a <destino>» en «todo» y en «lo importante»; en «nada», no.
- Decidido (usuario, 23/11/2026): con una ruta iniciada, atrás nunca la termina: cierra lo que haya encima (ficha, «Cómo llegar», fila de voz, hoja desplegada) y, si no hay nada, no hace nada y la app no sale. A mano solo se termina con «Finalizar ruta». En «Has llegado», igual.
- Pantalla: arriba, marca de hecho, «Has llegado» y el nombre del destino; abajo, botón azul «Finalizar ruta» (no el rojo) y «La ruta se cerrará en 1 minuto». La hoja no se despliega. La línea de la ruta se quita; la cámara se queda siguiendo y el botón de voz desaparece.
- Cierre automático a los 60 s de llegar (reloj inyectado; también con el móvil bloqueado). Con una ficha o «Cómo llegar» abiertas no se cierran: al cerrarlas sale «Has llegado» o, pasado el minuto, el mapa normal. «Iniciar» otra ruta termina la que había.
- Terminar (botón, solo o al iniciar otra) para el GPS de fondo y su notificación, quita la ruta y deja el mapa normal; `RutaTerminada` con motivo `Llegada` (botón tras llegar), `Automatico` o `Manual` (antes de llegar o por otra ruta). La notificación dice «Has llegado a <destino>» ese minuto.
- TalkBack: anuncia «Has llegado a <destino>. La ruta se cerrará en un minuto.» y el foco va a «Finalizar ruta». Contraste AA en claro y oscuro; texto al 200 % sin cortes.
- Nada se guarda (FR17).

**Never:** crear visitas (4.2); sonido de llegada (5.5); preguntar «¿Terminar la ruta?»; cambiar los textos de las maniobras de Valhalla.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Llegar | Posición a 25 m del destino | «Has llegado», voz y anuncio, una sola vez |
| Casi | A 35 m del destino y del final de la línea | Sigue navegando |
| Destino apartado | Destino a 80 m de la calle; posición a 20 m del final de la línea | «Has llegado» |
| Ya allí | Iniciar a 10 m del destino | «Has llegado» al empezar |
| Alejarse | Tras llegar, a 60 m | Sigue «Has llegado», sin recálculo |
| Botón | «Finalizar ruta» tras llegar | GPS de ruta parado, mapa normal, motivo `Llegada` |
| Automático | 60 s tras llegar | Igual, motivo `Automatico` |
| Antes de llegar | Hoja desplegada, «Finalizar ruta» | Como hoy, motivo `Manual` |
| Atrás en ruta | `Escape`, hoja plegada o desplegada | Nada, o solo se pliega; la ruta sigue |
| Ficha encima | Llega con una ficha abierta | La ficha sigue; al cerrarla, «Has llegado» o el mapa |
| Recálculo en camino | Llega con un recálculo pedido | Se cancela: su ruta no se dibuja |
| Sin señal | Sin lecturas del GPS | No se llega |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `Scripts/Aplicacion/ServicioNavegacion.cs`: `Empezar`, `Seguir` (posición), `Comprobar` (cada fotograma, también bloqueado a 5 fps: aquí el minuto), `Recalcular` (sesión cancelable), `Terminar(motivo)`, `textoNotificacion`. `MotivoFinRuta` (añadir `Llegada` y `Automatico`). Nuevo: estado «llegado» y su evento (lo usará la épica 4).
- `Scripts/Dominio/ReglaNavegacion.cs`, `ParametrosNegocio` (30 m, 60 s), `ReglaVoz` y `Aplicacion/ServicioVoz` (`AlActualizar`; calla al terminar).
- `Scripts/UI/PantallaMapa.cs`: `AlNavegar`, `ActualizarVistaRuta`, `AlTerminarNavegacion`, `QuitarRuta` (la llama el botón), `Atras` (quitar `if (rutaActiva) QuitarRuta`, también con la cámara acercándose), `ColocarControlesRuta`, `ActualizarBotonVoz`, `PastillaSenal`, `TocarMapa`. `TarjetasRutaVisibles` es la condición de las dos tarjetas.
- `Scripts/UI/TarjetaManiobra.cs` y `TarjetaRuta.cs` (gesto propio, `Finalizar`, `Desplegar`): decidir si se reutilizan o hay tarjeta propia. Textos: `ClavesTexto`, `PrepararInterfaz.TextosEs`, `Assets/Localizacion` (`Textos_es-ES` y `Shared Data`).
- `Plugins/Android/ServicioUbicacionRuta.java`: `onTaskRemoved` ya para el servicio (línea 139); solo comprobarlo.
- Pruebas que hoy terminan con atrás: `PruebasNavegacion` 972, `PruebasRutas` 1492/1530/2109, `PruebasCamaraNavegacion` 872/958. Cambian por decisión del usuario.

## Tasks & Acceptance

**Execution:**
- [x] Llegada, minuto y motivos en Dominio/Aplicacion con reloj inyectado; voz y notificación.
- [x] «Has llegado» en la pantalla (B), atrás, TalkBack, textos, estilos y contraste.
- [x] Una prueba por fila de la matriz; capturas `llegada(-oscuro)`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dadas las capturas `llegada(-oscuro)`, cuando se comparan con la maqueta B, entonces coinciden.

## Implementation Notes

## Spec Change Log

## Review Triage Log

Revisión del 23/11/2026 (BH = ciega, EC = casos límite, VG = huecos de verificación).

| Hallazgo | Veredicto | Ruta y prueba |
|----------|-----------|---------------|
| EC1: un recálculo de la caché que llega deja la notificación en «Navegando a…» | medium | patch |
| BH8, EC4: un fallo del texto de la voz deja a la pantalla sin la llegada | low | patch: dentro del try |
| BH1: la cámara puede girar al llegar fuera de la ruta | medium | patch: el rumbo solo se reinicia si iba por la línea |
| BH2, BH3: el anuncio se gasta con una ventana delante | low | patch: la comprobación va en EnfocarLlegada |
| BH5, BH6, EC5, EC6, VG otros: motivos al compartir o cerrar la app tras llegar | low | patch: Finalizar en OnDestroy y comentario de Llegada |
| EC11: OnDestroy no se desuscribe de la hoja | low | patch |
| VG1: «ninguna maniobra tras llegar» sin proteger | medium | patch: prueba |
| VG2: «Finalizar ruta» nunca se pulsa de verdad | medium | patch: prueba |
| VG3: el toast al llegar, prueba vacía | medium | patch: prueba |
| VG4: margen de la barra de gestos sin probar | medium | patch: prueba |
| BH16: la prueba de cancelar no mira la cancelación | low | patch: prueba |
| BH12: «1 minuto» no va unido al parámetro | low | patch: prueba |
| VG5: los marcadores no pisan las tarjetas de llegada, sin probar | medium | defer: pide la pantalla entera en un panel, como las tarjetas de la ruta |
| EC2: final de la línea cerca y destino lejos | false | Decisión del usuario: también se llega al acabar la línea |
| EC3: nombre provisional de un punto cuya dirección llega tras Iniciar | low | rechazado: entre Ir, elegir e Iniciar pasan segundos; la notificación ya lo hacía y arreglarlo añade superficie |
| BH7, EC7: terminar desde fuera de la pantalla | low | rechazado: nadie lo hace |
| BH9, EC9: salto del reloj hacia delante | low | rechazado: raro, y solo cierra antes «Has llegado» |
| BH10, EC8: el minuto solo corre con la app despierta | low | rechazado: navegando Unity no se pausa (2.8b); en pausa se cierra al volver. Comprobarlo en el móvil |
| BH11, EC10: «1 minuto» fijo aunque se vea tarde | low | rechazado: raro y lo decide el spec |
| BH4: el cierre automático no se anuncia | low | rechazado: se avisa al llegar |
| BH13: no hay tiempo restante 0 al llegar | low | rechazado: la épica 4 tiene HaLlegado |
| BH14: destino sin nombre | false | Los destinos siempre llevan nombre (lugar, dirección o coordenadas) |
| BH15: contraste del círculo con la tarjeta | low | rechazado: adorno; la marca y los textos cumplen |
| BH17: constante repetida y dobles copiados | low | rechazado: como el resto de las pruebas |
| BH18: documentación | low | se hace al cerrar |

Sin intent_gap ni bad_spec.

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` (con `-buildTarget Win64`) y `PriceMap.exe -captura`; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.

**Manual checks:**
- En el móvil: llegada real, cierre al minuto (también bloqueado) y notificación que desaparece.
