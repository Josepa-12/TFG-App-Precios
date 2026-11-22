---
title: 'Historia 2.7 (parte 1): seguir la ruta'
type: 'feature'
created: '2026-11-18'
status: 'done'
baseline_commit: '205b852ba6b6d7a5f25b93fe89153784babda220'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Ir dibuja la ruta (2.6) pero no guía: no avanza la maniobra, no baja el tiempo, no se recalcula al desviarse y el GPS de fondo (2.1) no se enciende (FR19, FR20).

**Approach:** `ServicioNavegacion` (Aplicación) empieza al pulsar Ir: recibe las posiciones de `ServicioUbicacion`, las proyecta sobre la ruta con `ReglaNavegacion` (Dominio, pura), saca la maniobra siguiente, la distancia a ella y lo que queda, recalcula si te desvías y mantiene el GPS de fondo con su notificación. Las tarjetas de la 2.6b se actualizan solas.

**Decisiones del 21/11/2026:**
- La 2.7 se parte en dos (total unos 2.800 tokens). **Parte 1** (esta): seguimiento, recálculo, GPS de fondo, eventos y tarjetas vivas. **Parte 2**: cámara que sigue (inclinación 45°, rumbo arriba), «Volver a centrar», flecha (también a pie, pregunta pendiente), «Buscando señal GPS» y pantalla siempre encendida.
- **Al pulsar Ir** la navegación empieza ya; se ve la ruta entera (encuadre de la 2.6b) y, en la parte 2, la cámara pasa a seguirte a los 2 s (al instante con «reducir animaciones»).

## Boundaries & Constraints

**Always:**
- **`ReglaNavegacion`** (equirrectangular local en `double`): proyección al segmento más cercano; restante = resto de la ruta desde el punto proyectado; tiempo restante = duración × (restante / total); siguiente maniobra = la primera con posición por delante del punto proyectado (posición de cada maniobra proyectada a su distancia en la ruta); distancia a ella. El avance nunca vuelve atrás por el ruido del GPS (se proyecta desde el último tramo, con margen).
- **Desvío:** > 40 m de la ruta durante > 5 s seguidos → recalcular (`ParametrosNegocio`; reloj inyectable).
- **Recálculo:** ruta nueva desde la posición actual con `ServicioRutas.Calcular` (sin conexión, la aproximada); la vieja se ve hasta tener la nueva; uno solo a la vez; si falla, reintento a los 10 s y toast «No se ha podido recalcular la ruta».
- **Lo recorrido se recorta:** la línea empieza en el punto proyectado.
- **Tarjeta de arriba:** icono y texto de la maniobra siguiente (textos de Valhalla, en usted), su distancia («En 120 m», con `FormatoDistancia`) y la de después. Ruta sin maniobras (OSRM o aproximada): el texto de la 2.6b y la distancia que queda.
- **Tarjeta de abajo:** llegada (ahora + restante), minutos y distancia que quedan, con «≈» si es aproximada; la X pasa a ser «Terminar ruta» (mismo icono).
- **GPS de fondo:** al empezar, `IniciarRuta("Navegando a <destino> · N min")`; `ActualizarNotificacion` cuando cambian los minutos; `TerminarRuta` al terminar (X, atrás o quitar la ruta).
- **Eventos (AD-9):** `RutaIniciada(destino)`, `TiempoRestanteCambiado(segundos)` (al cambiar el minuto), `RutaTerminada(motivo)` con motivo `Manual` (la 2.9 añade la llegada).
- **TalkBack:** la tarjeta de arriba anuncia cada maniobra nueva (solo al cambiar de maniobra, no cada metro); X «Terminar ruta».
- Umbrales en `ParametrosNegocio`; textos en Localization.

**Never:** cámara, flecha, sin señal o pantalla encendida (parte 2); voz (2.8); llegada (2.9); crear visitas (4.2).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Empezar | Ir con ruta de 6 min | Tarjetas con la primera maniobra y «6 min»; notificación «Navegando a … · 6 min»; `RutaIniciada` |
| Avanzar | Posición a mitad de la ruta | ≈ 3 min; maniobra siguiente correcta y su distancia; línea recortada |
| Pasar maniobra | Posición tras la primera maniobra | Sale la siguiente; TalkBack la anuncia una vez |
| Ruido | Posición 10 m por detrás de la anterior | No vuelve a la maniobra anterior |
| Desvío corto | 45 m de la ruta 3 s | No recalcula |
| Desvío largo | 45 m 6 s | Recalcula desde la posición; la vieja sigue hasta la nueva |
| Recálculo falla | Los servicios caen y no hay calles | Toast; reintento a los 10 s |
| Sin maniobras | Ruta aproximada | «Ruta aproximada…» con la distancia que queda; «≈» abajo |
| Minuto cambia | 6 → 5 min | Notificación «· 5 min»; `TiempoRestanteCambiado` |
| Terminar | X o atrás | Sin ruta; `TerminarRuta`; `RutaTerminada(Manual)` |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets/Scripts`.
- Nuevos `Dominio/ReglaNavegacion.cs` (+ `EstadoNavegacion`: punto proyectado, índice de tramo, restante en m y s, maniobra siguiente y de después con su distancia, distancia a la ruta) y `Aplicacion/ServicioNavegacion.cs` (eventos, recálculo, notificación; reloj y espera inyectables como `ServicioRutas`).
- `Dominio/Ruta.cs`, `Dominio/Maniobra.cs` (`Posicion`, `Metros` del tramo; no trae su distancia acumulada: calcularla al empezar).
- `Aplicacion/ServicioUbicacion.cs`: `Posicion`, `Cambiado`, `IniciarRuta/ActualizarNotificacion/TerminarRuta` (l.83-89). Las posiciones de la cola del servicio de Android llegan en orden por `Cambiado` (`UbicacionGps.RecogerRuta`).
- `Aplicacion/ServicioRutas.cs`: `Calcular` para el recálculo.
- `UI/PantallaMapa.cs`: `Ir()` (l.1467), `DibujarRuta`, `FalloRuta`, `QuitarRuta` (l.1604), `TarjetasRutaVisibles` (l.1644), `MostrarToast`; recibe `ServicioNavegacion` por `Componer` y redibuja con `IMapa.MostrarRuta(restantes, aproximada)`.
- `UI/TarjetaManiobra.cs` (`Poner`, l.101: añadir la distancia «En 120 m»; `Anuncio`), `UI/TarjetaRuta.cs` (restante, X «Terminar ruta»), `UI/FormatoDistancia.cs`, `UI/ClavesTexto.cs` + `Editor/PrepararInterfaz.TextosEs` + tabla es-ES.
- `Arranque/Arranque.cs` (`Componer`, l.138: nuevo parámetro opcional), `Arranque/CapturaPruebas.cs` (captura «navegacion»: ubicación falsa a mitad de la ruta de Clínica Areas).
- `Dominio/ParametrosNegocio.cs`: 40 m, 5 s, 10 s.
- Pruebas: nuevo `Tests/EditMode/PruebasNavegacion.cs` (regla con rutas sintéticas; servicio con ubicación, rutas y reloj falsos; pantalla).

## Tasks & Acceptance

**Execution:**
- [x] `ReglaNavegacion` + pruebas (proyección, restante, maniobra, ruido, desvío).
- [x] `ServicioNavegacion` + pruebas (empezar, recálculo, reintento, notificación, eventos, terminar).
- [x] Tarjetas vivas, línea recortada, X «Terminar ruta», TalkBack; pruebas de la matriz en pantalla.
- [x] `Arranque`, textos, captura «navegacion»; memoria del TFG; CLAUDE.md.

**Acceptance Criteria:**
- Given la suite EditMode, when se ejecuta sin red, then pasan todas.
- Given la captura «navegacion», when se abre, then la línea empieza en la posición y la tarjeta de arriba da la maniobra siguiente con su distancia.

## Spec Change Log

## Review Triage Log

Revisión del 22/11/2026 (blind, edge-case, verification-gap; diff del código desde `205b852`).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | Recalcula sin fin si la ruta empieza a más de 40 m (lejos de una calle; la aproximada une hasta 300 m; la caché de 20 m devuelve la misma) | medium | `Seguir` recalcula a los 5 s fuera de ruta sin mirar desde dónde se pidió; la ruta nueva sigue lejos y se repite cada 5 s, con anuncio de TalkBack | patch (`origenRuta`: no se recalcula a menos de 40 m de donde se pidió la ruta) |
| 2 | Vuelve a la ruta mientras se pide el recálculo y la respuesta la sustituye igual | medium | El camino de éxito de `Recalcular` no miraba `Estado.FueraDeRuta` (el de reintento sí) | patch |
| 3 | Minutos distintos en ficha y tarjeta con segundos decimales (360,3 s: 7 frente a 6) | low | `Ruta.Minutos` no redondeaba al segundo y `TarjetaRuta`/`EstadoNavegacion` sí | patch (redondeo dentro de `Ruta.MinutosDe`) |
| 4 | Sin prueba del anuncio de TalkBack tras recalcular | low | Ninguna prueba mira `anuncios` tras `Responder` del recálculo | patch (prueba) |
| 5 | Sin prueba del toast «ruta aproximada» al recalcular sin conexión | low | Solo la tabla de textos nombra `ToastRutaAproximada` | patch (prueba) |
| 6 | Sin prueba de que `Componer` pase la navegación a la pantalla | low | Ninguna llamada a `Componer` en pruebas pasa `navegacion:` | patch (prueba) |
| 7 | Sin prueba de que `OnDestroy` termine la ruta | low | El TearDown destruye sin comprobar `Terminadas` | patch (prueba, por reflexión: el editor no llama a `OnDestroy`) |
| 8 | Texto de llegada «En 130 m, ha llegado a su destino» | low | Es el texto de Valhalla; la llegada es de la 2.9 (fuera por el intent) | defer |
| 9 | `OnDestroy` termina con motivo `Manual` (las visitas de la épica 4 lo leerían como fin del usuario) | low | Real cuando exista `ServicioVisitas`; hoy nadie escucha | defer |
| 10 | El reloj del desvío no usa la hora de cada lectura de la cola | low | Con la pantalla apagada la app está en pausa; solo retrasa 5 s el recálculo al volver; el arreglo pide llevar la hora por `Cambiado` | rechazado |
| 11 | El avance sube con el ruido estando parado | low | Lo fija el spec (el avance no baja); el salto es del tamaño del ruido; el arreglo añade histéresis | rechazado |
| 12 | Llegada y minutos congelados estando parado | low | Fórmula del spec (duración × fracción); el GPS real varía y refresca | rechazado |
| 13 | El recálculo dispara `TiempoCalculado` de la ficha | false | `ServicioRutas.Calcular` no lo dispara (solo `Tiempo`) | rechazado |
| 14 | Recálculo síncrono desde la caché da dos Actualizado | low | Inocuo (el segundo no redibuja) y solo pasaba en el caso 1, ya arreglado | rechazado |
| 15 | Fallo silencioso de `IniciarRuta` | low | El spec no pide aviso; se sigue con la app delante | rechazado |
| 16 | `ServicioNavegacion` no se desuscribe de `Cambiado`; `Destino`/`Modo` quedan | low | Vive lo mismo que `ServicioUbicacion` (Arranque); sin daño | rechazado |
| 17 | Redibujo de la línea a cada lectura / N veces al vaciar la cola | low | Una vez por segundo, línea pequeña; la cola solo al volver | rechazado |
| 18 | La etiqueta de TalkBack no lleva los metros | low | Decisión: no refrescar la etiqueta a cada metro; el anuncio sí los lleva | rechazado |
| 19 | Orden de limpieza en la captura «navegacion» | false | `Mover` y `QuitarRuta` van seguidos; el desvío necesita 5 s | rechazado |
| 20 | Pruebas que faltan (punto sin nombre, Terminar dentro de RutaCambiada, minutos por recálculo, Inicializar dos veces) | low | El título del punto nunca está vacío; los demás no son caminos de uso | rechazado |
| 21 | Texto por defecto de la notificación repetido | low | Solo lo usan las pruebas; la app pasa el de Localization | rechazado |
| 22 | Parado a más de 40 m con la misma coordenada no recalcula | low | El GPS real no repite la coordenada exacta; con el caso 1 tampoco debe | rechazado |
| 23 | Una excepción en un suscriptor deja `Recalculando` a true | false | Los tres manejadores de `PantallaMapa` tienen try/catch | rechazado |
| 24 | El cambio de maniobra con la ficha abierta no se anuncia | low | Al cerrar la ficha el foco vuelve a la tarjeta, que lee su etiqueta | rechazado |
| 25 | `PuntosRestantes` con estado de otra ruta | false | `Regla` y `Estado` se cambian juntos en el servicio | rechazado |
| 26 | Salto a un tramo lejano estando fuera de ruta | false | Es lo buscado: si está claramente más cerca de lo de delante, va por ahí | rechazado |
| 27 | Ruta de 2+ puntos con `Total` 0 | low | Ruta degenerada que ningún servicio da | rechazado |
| 28 | «Se ve la ruta entera» frente a la línea recortada | false | El spec habla del encuadre de la 2.6b, no de la línea | rechazado |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- Build de PC y captura «navegacion»; después, borrar `link.xml` y su `.meta` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: ruta real a pie con la pantalla apagada; al volver, la maniobra y los minutos están al día y la notificación cambia de minuto.
