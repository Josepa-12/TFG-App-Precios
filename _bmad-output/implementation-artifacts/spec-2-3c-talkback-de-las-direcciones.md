---
title: 'Historia 2.3 (parte 3): TalkBack de las direcciones'
type: 'feature'
created: '2026-11-19'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Las direcciones de la parte 2 se ven, pero TalkBack no las recorre. Tampoco recorre «Buscar como dirección» ni anuncia que han llegado direcciones o que falta conexión.

**Approach:** En `PantallaMapa`, al declarar el buscador y los resultados en TalkBack:
- **Fila de dirección:** es un botón del recorrido, igual que las locales, con el anuncio «Calle Ancha 10, Dirección, Sanlúcar de Barrameda, a 1,2 km». Sin localidad: «Calle Ancha 10, Dirección, a 1,2 km». Usa una clave de texto nueva.
- **«Buscar como dirección»:** es un botón del recorrido mientras se ve; hace lo mismo que tocarlo.
- **Aviso «Las direcciones necesitan conexión»:** es un texto del recorrido, detrás de las filas, como en pantalla. Se anuncia una vez, cuando aparece; mientras siga visible no se repite.
- **Anuncio «N resultados»:** cuenta locales y direcciones. Se anuncia si cambia la lista, también cuando llegan las direcciones.
- **Foco al llegar las direcciones:** la respuesta llega después y no es una acción del usuario, así que el foco de TalkBack no se mueve; solo se anuncia. Al teclear sigue como en la parte 1: el foco se queda en el campo.

</frozen-after-approval>

## Implementation Notes

- **Clave nueva:** `buscador.direccion.anuncio` = «{0}, {1}, {2}, a {3}». Sin localidad se usa la del resultado local («{0}, {1}, a {2}»). Tablas regeneradas con `PrepararInterfaz.Asegurar`.
- **`PantallaMapa`:**
  - «Buscar como dirección» es un botón del recorrido detrás de «No hay resultados». El aviso es un texto del grupo de resultados, detrás de las filas, y las filas de dirección ya no se saltan.
  - «N resultados» cuenta locales y direcciones (`Juntar`); `MismosResultados` también compara la `Direccion`.
  - El aviso se anuncia una vez (`avisoAnunciado`). Al ocultarse se rearma, y también al pulsar el botón, para que un reintento fallido se oiga.
  - Al llegar las direcciones (`AlCambiarDirecciones`) y al pulsar el botón no se mueve el foco: solo se anuncia. Si el botón desaparece con la respuesta (`focoAlCampoPendiente`), el foco va al campo.
- **Sorpresa:** al llegar las direcciones, `HojaBuscador.MostrarResultados` rehacía todas las filas, también las locales, y la fila con el foco desaparecía. Arreglo:
  - Si los locales son la misma lista (`localesPintados`), sus filas se conservan y solo se cambian las de dirección.
  - `AccesibilidadPantalla` guarda en `Enfocado` el nodo que TalkBack enfoca (`focusChanged`), y el nuevo `RecuperarFoco()` lo devuelve tras `Reconstruir`, sin anunciar.
- **Pruebas:** 6 nuevas en `PruebasDirecciones` sustituyen a la de la parte 2 que comprobaba que las direcciones no estaban en TalkBack. Pasan 543 de 543.


## Review Triage Log

Revisión del 19/11 (blind hunter; vía oneshot).

| # | Hallazgo | Veredicto | Ruta | Evidencia |
|---|----------|-----------|------|-----------|
| 1 | Si el foco estaba en «No hay resultados» y llegan direcciones, se queda en nada | medium | patch | `RecuperarFoco` devolvía false y nadie lo atendía; ahora el foco va al campo. Prueba `SiElNodoConElFocoDesapareceElFocoVaAlCampo`. |
| 2 | Sin conexión, el aviso parpadea y se vuelve a anunciar tras cada pausa al teclear | medium | patch | `PedirDirecciones` ponía `direccionesFallaron = false`; ahora solo lo quita una respuesta buena. Prueba `SinConexionElAvisoNoSeRepiteAlSeguirEscribiendo` con espera real. |
| 3 | Poner `Enfocado` desde `focusChanged` puede cancelar un `PedirFoco` pendiente y su anuncio | medium | patch | Campo aparte, `ultimoFoco`, para `RecuperarFoco`; `Enfocado` solo cambia con `PedirFoco`. |
| 4 | `RecuperarFoco` sin `ContainsNode` y con `NodoDe` doble | low | patch | Corrección directa. Los marcadores quedan fuera: aquí solo se usa en la hoja. |
| 5 | La prueba "el foco no se mueve" no comprobaba el nodo enviado a TalkBack ni que la fila se conserve | low | patch | Se añaden `UltimoNodoEnfocado` y `AreSame` de la fila. |
| 6 | Conservar las filas locales depende de que la lista sea el mismo objeto | low | rechazado | `Buscar` siempre crea una lista nueva; documentado en `MostrarResultados`. |
| 7 | Separador ". " fijo en el anuncio combinado | low | rechazado | Puntuación para el lector; la app solo está en es-ES. |
| 8 | Sin localidad se reutiliza la clave del resultado local | low | rechazado | Mismo formato «nombre, tipo, a distancia». |
| 9 | `Juntar` cuenta los datos y no lo dibujado | false | — | `AnadirFila` solo salta null y las listas no traen null. |
| 10 | `focoAlCampoPendiente` puede durar más que su petición | false | — | `Buscar` lo limpia a cada cambio de texto (también la X), y con la hoja plegada no se mueve el foco. |
| 11 | Faltan la memoria, el sprint y CLAUDE.md | — | — | Se actualizan al cerrar; `Direcciones` ya está en la lista de pruebas de CLAUDE.md. |
