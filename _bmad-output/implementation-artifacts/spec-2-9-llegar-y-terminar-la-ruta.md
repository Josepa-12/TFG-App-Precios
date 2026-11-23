---
title: 'Historia 2.9: llegar y terminar la ruta'
type: 'feature'
created: '2026-11-18'
status: 'ready-for-dev'
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
- [ ] Llegada, minuto y motivos en Dominio/Aplicacion con reloj inyectado; voz y notificación.
- [ ] «Has llegado» en la pantalla (B), atrás, TalkBack, textos, estilos y contraste.
- [ ] Una prueba por fila de la matriz; capturas `llegada(-oscuro)`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dadas las capturas `llegada(-oscuro)`, cuando se comparan con la maqueta B, entonces coinciden.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- `CompilarPcPruebas` (con `-buildTarget Win64`) y `PriceMap.exe -captura`; borrar `link.xml` y deshacer `Windows/addressables_content_state.bin`.

**Manual checks:**
- En el móvil: llegada real, cierre al minuto (también bloqueado) y notificación que desaparece.
