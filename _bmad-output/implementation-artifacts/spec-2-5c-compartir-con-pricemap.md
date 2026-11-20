---
title: 'Historia 2.5 (parte 3): compartir una ubicación con PriceMap'
type: 'feature'
created: '2026-11-20'
status: 'done'
baseline_commit: '647366d9a6dd04f9c79a2d72bd5aa0a7224807f8'
route: 'dispatch'
review_loop_iteration: 0
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Si alguien recibe una ubicación en WhatsApp o la ve en Google Maps, tiene que copiarla y pegarla en el buscador; PriceMap no sale en «Compartir» ni abre los enlaces `geo:` (FR17).

**Approach:** PriceMap se registra en Android para recibir texto compartido (`ACTION_SEND`, `text/plain`) y enlaces `geo:` (`ACTION_VIEW`). Al arrancar o al volver a la app, lee lo recibido y lo pasa al buscador como si se hubiera pegado (parte 2), con toda su lógica: analizador, enlace corto, mensaje de no reconocido.

**Decisiones del 20/11/2026:**
- Una ubicación compartida que se reconoce abre sola la ficha de punto libre (al momento, o al resolverse el enlace corto), como si se tocara «Ir a este punto». Si no se reconoce, el mensaje queda en la hoja. Si el usuario cambia el texto antes, ya no se abre.
- No se registran los enlaces `https` de Google Maps en «Abrir con» (desde Android 12 no se ofrecerían a PriceMap).
- El spec se deja entero (unos 2.000 tokens).

## Boundaries & Constraints

**Always:**
- **Manifiesto** (al generar Gradle): en la actividad de Unity, filtros `SEND` + `text/plain` (categoría `DEFAULT`) y `VIEW` + esquema `geo` (`DEFAULT`, `BROWSABLE`). Sin duplicarlos si ya están.
- **Lectura** (plugin, JNI): el texto de `EXTRA_TEXT` (SEND) o la URI (`VIEW`) del intent actual de la actividad. Se consume al leerlo (no se repite al volver) y se ignora si el intent viene del historial (`FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY`). Se lee al arrancar y al volver delante.
- **En la app**: si el texto lleva un enlace, al campo va solo el enlace; si no, el texto en una línea (saltos → espacios), recortado a 200 caracteres. Se cierra la ficha abierta y se abre la hoja del buscador con ese texto (con TalkBack, como `AbrirBuscadorConLector`).
- Si la explicación o el aviso de ubicación están abiertos, se espera a que se cierren.
- Texto que no es ubicación: se busca como un texto escrito.
- Nada se guarda (FR17).

**Never:** filtros `VIEW` de `https` de Google Maps; API de Google; actividad propia de Android; cambios en el analizador de la parte 2.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Compartir desde Google Maps | SEND «Bar Pepe\nCalle Ancha 3\nhttps://maps.app.goo.gl/abc» | Campo con `https://maps.app.goo.gl/abc` y «Comprobando el enlace…»; al resolverse, se abre la ficha del punto | Falla la red: mensaje de no reconocido en la hoja |
| Cambia el texto | Enlace corto resolviéndose y el usuario escribe | No se abre ninguna ficha | N/A |
| Coordenadas compartidas | SEND «36.7781, -6.3515» | Ficha de punto libre en 36.7781, -6.3515 | N/A |
| geo: | VIEW `geo:36.778,-6.351?q=36.778,-6.351(Casa)` | Ficha de punto libre en 36.778, -6.351 | N/A |
| No reconocido | SEND `https://maps.google.com/?cid=123` | Hoja con el mensaje de no reconocido; no se abre nada | N/A |
| Texto cualquiera | SEND «Bar Pepe» | Búsqueda normal de «Bar Pepe» | N/A |
| Con la app abierta | Llega con la ficha de un lugar abierta | Se cierra la ficha y se abre la hoja con el texto | N/A |
| Primer arranque | Llega con la explicación de ubicación a la vista | Al cerrarla, se abre la hoja con el texto | N/A |
| Volver sin compartir | La app vuelve delante (intent ya leído o del historial) | Nada cambia | N/A |
| Vacío | SEND sin texto | Nada | N/A |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets` salvo otra indicación.
- Actividad: `UnityPlayerGameActivity` (`androidApplicationEntry: 2`), `launchMode="singleTask"`; su `onNewIntent` ya hace `setIntent(intent)` (visto en `Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary`), así que `getIntent()` siempre es el último. No hace falta actividad propia.
- `Editor/PermisosUbicacionAndroid.cs`: patrón `IPostGenerateGradleAndroidProject` + XML del manifiesto de `unityLibrary` (l.33-71). Nuevo `Editor/CompartirAndroid.cs`, igual, que añade los filtros a la `activity` con `intent-filter` `MAIN`/`LAUNCHER`. Pruebas como `Tests/EditMode/PruebasPermisosUbicacionAndroid.cs` (manifiesto mínimo en temporal, l.19-37), con una `activity` lanzadora.
- Plugin: nuevo `Plugins/Android/PuenteCompartir.java` (paquete `com.pricemap.compartir`), `static String tomar(Activity)`: lee y consume (`setIntent(new Intent())`). Como `PuenteUbicacionRuta.java`.
- Puerto nuevo `Scripts/Aplicacion/IEntradaCompartida.cs` (`string Tomar()`, null si no hay). Adaptador `Scripts/Adaptadores/Sistema/EntradaCompartidaAndroid.cs` (JNI solo en Android; fuera, null), como `TemaSistemaAndroid`.
- Texto para el campo: función pura en Dominio, junto a `LectorUbicacionCompartida` (`ExtraerEnlace`, l.74+): `TextoCompartido(string) → string` (enlace, o una línea de 200 como mucho; null si vacío).
- `Scripts/Arranque/Arranque.cs`: crea `EntradaCompartidaAndroid`; en `Start` (tras `Componer`) y en `OnApplicationFocus(true)` (l.70-74), `Tomar()` → `pantallaMapa.RecibirCompartido(texto)`.
- `Scripts/UI/PantallaMapa.cs`: `RecibirCompartido(string)`: pendiente si `ModalUbicacionAbierta` (l.249), aplicado en `Update` (l.1453) al cerrarse. Aplicar: `CerrarFicha()` (l.1308), abrir la hoja (`buscador.Abrir()` o `AbrirBuscadorConLector()` l.973), poner el texto en el campo y `Buscar` (l.469). Abrir sola: marca para elegir la fila `Coordenada` en cuanto exista (en `LeerUbicacionCompartida` l.510 o `ResolverEnlace` l.553) con `ElegirResultado` (l.796); la marca se pierde si cambia el texto.
- `Scripts/Arranque/CapturaPruebas.cs`: paso «compartido» que llama a `RecibirCompartido` con un `geo:`.
- Pruebas: `Tests/EditMode/PruebasCompartir.cs` (pantalla real con resolutor falso, como `PruebasUbicacionCompartida.cs` l.199-227; `TextoCompartido`; manifiesto).

## Tasks & Acceptance

**Execution:**
- [x] `Editor/CompartirAndroid.cs` + pruebas de manifiesto -- filtros sin duplicar.
- [x] `Plugins/Android/PuenteCompartir.java`, `IEntradaCompartida`, `EntradaCompartidaAndroid` -- lectura y consumo del intent.
- [x] `TextoCompartido` en Dominio + pruebas.
- [x] `PantallaMapa.RecibirCompartido` y `Arranque` -- cada fila de la matriz con pruebas.
- [x] Captura «compartido» y memoria del TFG.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la APK, cuando se compila, entonces el manifiesto final tiene los dos filtros y la build no da errores.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| # | Hallazgo (capa) | Veredicto | Evidencia | Ruta |
|---|---|---|---|---|
| 1 | El enlace corto resuelto tarde sustituye la ficha que el usuario abrió entretanto (edge) | medium | `AbrirSiToca` solo miraba el texto y la lista «cerca» | patch: si `FichaAbierta`, no se abre; prueba `SiSeAbreOtraFichaMientrasSeResuelve_SeQuedaEsa` |
| 2 | Ninguna prueba hace correr el `Update` que aplica lo pendiente (verification-gap) | medium | Las pruebas `PrimerArranque_*` llamaban a `AplicarCompartidoPendiente` a mano | patch: las pruebas corren el `Update` real (reflexión; `SendMessage` no va en EditMode) |
| 3 | El paso al volver delante no tiene prueba; con pantalla partida puede no haber cambio de foco (verification-gap, edge, blind) | medium | Nada ejecutaba `OnApplicationFocus`; `OnApplicationPause(false)` no leía | patch: `AlVolverDelante` estático, llamado desde foco y pausa, con prueba |
| 4 | Comentario de `AplicarCompartidoPendiente` al revés (blind) | low | Decía «con la hoja ya abierta» dentro de `if (!yaAbierta)` | patch: reescrito |
| 5 | La lectura en Java no se prueba; tras matar el proceso, la actividad recreada podría traer otra vez el intent (verification-gap, blind) | maybe-false (medium si es cierto) | Sin pruebas instrumentadas; solo se ve en el móvil | defer |
| 6 | `geo:0,0?q=dirección` (contactos, calendario) da «no reconocido» en vez de buscar la dirección (blind) | medium | Comportamiento de la parte 2 (`geo sin coordenadas`), no de esta | defer |
| 7 | Carrera entre `getIntent`/`setIntent` y un `onNewIntent` simultáneo (blind, edge) | low | Dos compartidos en milisegundos; arreglarlo pide hilo de UI y cerrojo | rechazado |
| 8 | Un texto con un enlace que no es de mapas manda solo el enlace (blind) | low | Lo fija el spec («al campo va solo el enlace») | rechazado (cambiaría el spec) |
| 9 | Varios enlaces: no se elige el de mapas (edge) | low | Raro al compartir; pide recorrer todos los enlaces | rechazado |
| 10 | Enlace sin límite de largo (blind, edge) | low | El campo desplaza el texto; un guard más | rechazado |
| 11 | El menú de precios no se cierra (blind) | false | `AbrirBuscadorConLector` → `AlDesplegarBuscador` cierra ficha y menú | rechazado |
| 12 | Doble petición de direcciones por el segundo `Buscar` (blind) | false | `PedirDirecciones` no repite con el mismo texto ya pedido | rechazado |
| 13 | El aviso de ubicación sin prueba propia (blind) | low | Usa la misma `ModalUbicacionAbierta` que la explicación, probada | rechazado |
| 14 | El filtro SEND existente con `text/*` se duplicaría (blind, edge) | low | El manifiesto de Unity no trae filtros SEND | rechazado |
| 15 | Sprint y pendientes sin actualizar (blind) | false | Se hace al cerrar la historia | rechazado |
| 16 | El recorte a 200 puede partir emojis compuestos (blind) | low | Cosmético y raro | rechazado |
| 17 | La ventana de ubicación abierta mientras se resuelve deja sin abrir la ficha (edge) | low | Solo si el usuario toca el botón de ubicación entretanto: acción suya | rechazado |
| 18 | Tras «Permitir», el aviso de denegado cierra la ficha compartida (edge) | false | El aviso solo se abre al tocar el botón de ubicación (`TocarUbicacion`) | rechazado |
| 19 | En frío, la hoja se abre antes de medirse (edge) | false | La hoja guarda la fracción y `Medir` la recalcula | rechazado |
| 20 | SEND con el texto solo en `ClipData` (edge) | low | Android copia `EXTRA_TEXT` a `ClipData`, no al revés; muy raro | rechazado |
| 21 | La prueba de categoría no llega al borrado de la marca (verification-gap) | false | `ElegirCategoria` no cancela la petición: la respuesta llega a esa rama | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md): todas en verde.
- APK por lotes (CLAUDE.md) y comprobar los filtros en el `AndroidManifest.xml` generado.
- Build de PC y captura «compartido»; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: compartir desde Google Maps (un sitio y una chincheta) y desde WhatsApp (ubicación y enlace); abrir un `geo:`; volver a la app sin compartir (no se repite).
