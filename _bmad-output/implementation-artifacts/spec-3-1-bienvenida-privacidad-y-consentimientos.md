---
title: 'Historia 3.1: bienvenida, privacidad y consentimientos'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: 'a575955348c29362900ca384e4df4d935b9fab09'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/diseno/bienvenida/textos.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La 1.7 solo explica la ubicación. Faltan bienvenida, aviso de privacidad y consentimiento explícito de ubicación y notificaciones (FR41, UX-DR16).

**Approach:** Bienvenida a pantalla completa antes del mapa, solo en el primer arranque, en tres pasos: qué es PriceMap, privacidad, permisos. Sustituye a la `ExplicacionUbicacion` del primer arranque (1.7), que queda solo para el botón de ubicación.

## Boundaries & Constraints

**Always:**
- Decidido (usuario, 23/11/2026): responsable en el aviso completo, «Jose Antonio Segovia Sarmiento» y «jcoete37@gmail.com» (texto seleccionable, sin abrir otra app).
- Decidido (usuario, 23/11/2026): opción A de la maqueta (https://claude.ai/artifact/6TqprV1FLDLC2Ms6AGGv9r), tres pasos, con el logo de la pantalla de inicio (`docs/diseno/inicio`: pin y «PriceMap») en el primero, también en oscuro. Sale al irse la pantalla de inicio (`spec-pantalla-de-inicio`).
- Decidido (usuario, 23/11/2026): «Empezar», siempre activo; lo no contestado cuenta como «Ahora no». El spec, entero (sin partir).
- Los textos de privacidad dicen la verdad de la app: durante una ruta la ubicación sigue con la pantalla apagada; el texto buscado y los puntos de una ruta van a Photon, Valhalla y OSRM, y el mapa se descarga de OpenFreeMap. La explicación de la 1.7 («No la guarda ni la comparte») se corrige igual.
- **Pasos** ("Siguiente", puntos de progreso; atrás = paso anterior):
  1. "Precios reales de Sanlúcar, en el mapa" y una frase sobre aportar al llegar.
  2. Privacidad: ubicación solo con la app abierta y durante una ruta; aportes anónimos; cuenta opcional; enlace "Leer el aviso completo" con el texto dentro de la app (sin web).
  3. Permisos: tarjetas de ubicación y de notificaciones, cada una con explicación y "Permitir"/"Ahora no". Después, "Empezar".
- Hueco en el diseño del paso 3 para el consentimiento a la evaluación del TFG (6.2), sin casilla todavía.
- **Notificaciones:** Android 13+ pide `POST_NOTIFICATIONS`; anteriores = concedido. Por puerto (`IPermisos`, o ampliar `IUbicacion` + nuevo `INotificaciones`), nunca desde la UI.
- **Guardado:** `IPreferencias` guarda `bienvenida.vista = 1` y las elecciones; luego mapa directo. Cerrar a mitad → la bienvenida vuelve a salir.
- **Rechazo:** la app funciona con lo permitido; "Ahora no" muestra "Puedes activarlo después en Ajustes" (Ajustes real = 3.5).
- **Accesibilidad:** título de cada paso como primer foco, modal, letra al 200 % sin cortes (desplazamiento), 48 dp.
- Textos por Localization, tono de Voice and Tone (tuteo, frases cortas).

**Never:** sin cuenta en la bienvenida (3.3), sin consentimiento de evaluación todavía (6.2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Primer arranque | Sin `bienvenida.vista` | Bienvenida antes del mapa |
| Siguiente arranque | `bienvenida.vista = 1` | Mapa directo |
| Cerrar a mitad | Sale en el paso 2 | Vuelve a salir la bienvenida |
| Permitir ubicación | Toque | Diálogo de Android; tarjeta marca "Permitido" |
| Ahora no | Toque en ubicación | Texto sobre Ajustes; la app sigue |
| Android 12 | Notificaciones | Se dan por concedidas sin diálogo |
| Atrás en paso 2 | `Escape` | Paso 1 |
| Atrás en paso 1 | `Escape` | Nada (no sale de la app) |
| Usuario de la 1.7 | Ya vio la explicación de ubicación | Ve la bienvenida una vez; ubicación ya aparece como "Permitido" si lo estaba |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- Textos exactos en `docs/diseno/bienvenida/textos.md`. `ClavesTexto`, `Editor/PrepararInterfaz.TextosEs` y las tablas de `Localizacion`, como las demás.
- `Aplicacion`: `INotificaciones` (estado sin permiso/concedido/denegado, `Cambiado`, `PedirPermiso`) y `ServicioBienvenida` (puro: paso 0–2, aviso abierto, elecciones, `Pendiente`, `Terminar`; preferencias `bienvenida.vista`, `bienvenida.ubicacion`, `bienvenida.notificaciones`). Al terminar, `ServicioUbicacion.MarcarExplicacionMostrada`.
- Adaptador `Adaptadores/Sistema/NotificacionesAndroid` (patrón de `ServicioRutaAndroid.PedirPermisoNotificaciones`; la respuesta puede llegar de otro hilo; editor y PC, concedido). El permiso ya está en el manifiesto.
- `UI/PantallaBienvenida.cs` (elemento, sin MonoBehaviour; `uxml`/`uss` propios o en código, como las demás hojas), en un hueco nuevo de `PantallaMapa.uxml` encima de todo, opaco, con zona segura (`AjustarDisposicion`) y `ScrollView`. Colores y letra, de `Tokens.uss`; botones como `.explicacion-ubicacion__boton`; iconos con `Painter2D`. Logo: copiar `pin`, `nombre-claro` y `nombre-oscuro` de `docs/diseno/inicio` a `UI/Imagenes`. Hueco vacío `hueco-evaluacion` en el paso 3.
- `UI/PantallaMapa.cs`: ya puesta debajo de la pantalla de inicio (al fundirse se ve ella, no el mapa); con ella pendiente, la de inicio no espera ni a la posición ni al mapa (`ComprobarInicio`) y el mapa no se acerca. `ExplicarAlAbrir`, `ExplicacionPorSalir` y `DebeExplicarAlAbrir` se quitan (el botón de ubicación sigue con la explicación). Con ella: `TapaPunto`, `Atras`, `ModalUbicacionAbierta`/avisos y `AplicarCompartidoPendiente` esperan; TalkBack con `ModalNuevo` y todo lo suyo `enModal` (lo demás fuera del recorrido), foco al título en cada paso y al botón de ubicación al acabar.
- Tarjetas: «Permitido» con marca si el estado es concedido (también al volver de Ajustes); tras «Ahora no» o denegar, el texto de Ajustes; si Android aún puede preguntar, «Permitir» sigue. Ubicación apagada = permitido.
- `Arranque.Componer` (parámetro opcional, como `inicio`) y `CapturaPruebas`: `bienvenida-1..3`, `bienvenida-aviso` (claro y oscuro) y `bienvenida-200`; las demás capturas, sin bienvenida.
- No tocar: `CentrarAlAbrir`, el permiso que pide la ruta (`UbicacionGps`).

## Tasks & Acceptance

**Execution:**
- [x] `Aplicacion` y adaptador: servicio, puerto y permiso de notificaciones.
- [x] `UI` y textos: pantalla, aviso, TalkBack, atrás y 200 %; texto de la 1.7.
- [x] `PantallaMapa`, `Arranque`: montar, esperar y quitar la explicación del primer arranque.
- [x] `CapturaPruebas` y `Tests/EditMode/PruebasBienvenida.cs`: una prueba por fila de la matriz, contraste y composición; las de la 1.7, al día.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la build de PC, cuando se abren las capturas de la bienvenida, entonces los tres pasos y el aviso se ven sin cortes, también al 200 %.

## Implementation Notes

- `Consentimiento` (sin contestar, permitido, rechazado, bloqueado) es el estado de cada tarjeta. «Android aún puede preguntar»: `IUbicacion.PuedePedirPermiso` / `INotificaciones.PuedePedir`, con `PreguntaPermiso` (shouldShowRequestPermissionRationale antes y después; false-false se da por posible una vez).
- Se guarda `permitido` o `ahora-no` al pulsar «Empezar», y `bienvenida.vista` lo último.
- `ModalUbicacionAbierta` incluye la bienvenida: así esperan fichas, avisos, lo compartido y los gestos del mapa.
- Con la fuente al 140 % o más (`bienvenida--grande`), el icono va encima del texto y los botones en columna.
- Textos añadidos solo para TalkBack: «Atrás», «Paso {0} de {1}» y «Permitir, Ubicación».

## Spec Change Log

## Review Triage Log

| Hallazgo | Veredicto | Motivo | Destino |
|---|---|---|---|
| BH1/EC8 el aviso no dice que el punto marcado en el mapa va a Photon ni que la posición exacta va a las rutas | medium | `GeocodificadorPhoton` /reverse manda 6 decimales; «los textos dicen la verdad» solo admite esta lectura | patch |
| BH2 contenido RGPD (derechos, base legal, plazos, fecha) | low | El contenido del aviso lo aprobó el usuario en la maqueta; se le avisa | defer |
| BH3 el aviso no se puede volver a leer | medium | Cierto hasta que exista Ajustes · Privacidad (3.5) | defer |
| BH4 el mapa se carga debajo antes del aviso | low | Solo descarga teselas; la posición no sale del móvil. Retenerlo añade estado | reject |
| BH5/EC1 toques rápidos saltan el consentimiento | medium | «Siguiente» y «Empezar» son el mismo botón sin pausa | patch |
| BH6 «Permitir» borra el «Ahora no» antes de la respuesta | low | Basta no borrarlo: concedido gana siempre | patch |
| BH7/EC4 cerrar dos veces el diálogo de ubicación cuenta como denegar | maybe-false | Faltaría ver en un móvil qué llama Unity al cerrarlo sin suscribirse a Dismissed; como mucho, low | reject |
| BH8 el permiso de la ruta, aparte | false | El spec manda no tocarlo | reject |
| BH9 elecciones guardadas sin uso | false | Lo pide el spec; las usará 3.5/6.2 | reject |
| BH10/EC7 la copia de Android restaura `bienvenida.vista` | medium | El manifiesto no pone `allowBackup` (por defecto, sí): reinstalar saltaría el aviso, y el aviso dice que los datos se quedan en el móvil | patch |
| BH11 «Permitido» falso en notificaciones | false | En Android 13+, apagarlas en Ajustes retira el permiso; antes, la matriz dice concedidas | reject |
| BH12 tarjeta bloqueada sin botón | low | Texto decidido; los Ajustes son la 3.5 | reject |
| BH13/EC2 dos peticiones a la vez | low | El diálogo tapa la app; un doble toque se corrige solo con la respuesta real | reject |
| EC3 «Empezar» con una petición en marcha | false | El diálogo de Android tapa el botón | reject |
| BH14 `pidiendo` sigue tras conceder | low | Corrección de una línea | patch |
| BH15 estado tras destruir | false | Destruida, nadie llama a `Atras` | reject |
| BH16 otras ventanas al abrir | false | Solo se abre al inicializar, sin nada abierto | reject |
| BH17 permisos leídos en `LeerAccesibilidadSistema` | low | Nombre impreciso, sin efecto; cambiarlo añade superficie | reject |
| BH18 no se ve que hay más debajo | low | Al 200 % la tarjeta se ve cortada por el pie; falta probar otros altos | reject |
| BH19 orden de «Atrás» en TalkBack | low | El título es el primero (decidido) y el gesto atrás del sistema sale siempre | reject |
| BH20/VG2 la prueba del foco no pone el foco | medium | Falta prueba | patch |
| BH21 pruebas que leen el código | low | Como las demás del repositorio | reject |
| BH22 documentación | low | Se hace al cerrar | reject |
| EC5 «Preguntar siempre» en Ajustes sin reinicio | low | Raro; arreglarlo añade un método | reject |
| EC6 teclado físico por debajo | low | Igual que las ventanas de antes; teclado físico en el primer arranque | reject |
| EC9 `ClaveExplicacion` ya no se lee | low | Se escribe y nadie la lee: se quita | patch |
| VG1 los botones no se pulsan en ninguna prueba | medium | Falta prueba | patch |
| VG3 la escala de fuente del sistema | medium | Falta prueba y la captura la salta | patch |
| VG4 la tabla de textos no se compara | medium | Falta prueba | patch |
| VG5 `RecuperarFoco` tras una respuesta | medium | Falta prueba | patch |
| VG-otros comentario de `IrAPagina` | low | Dice «con los botones de verdad» | patch |

## Verification

**Commands:**
- Pruebas EditMode, `CompilarApk` (`-buildTarget Android`), `CompilarPcPruebas` (`-buildTarget Win64`) y captura; borrar `link.xml` y deshacer los `addressables_content_state.bin`.

**Manual checks:**
- Instalación limpia en Android 13+ y en uno anterior; actualización desde la versión de antes.
