---
title: 'Abrir la app centrada en la posición'
type: 'feature'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
baseline_commit: 'aa9827a83225bdb5f54da1b38e95c42288bf43b0'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Al abrir la app, el mapa se queda en la vista inicial (Plaza del Cabildo) y hay que tocar el botón de ubicación para ver el cursor (petición del usuario, 23/11/2026: «que se abra con el cursor centrado directamente»).

**Approach:** Con la primera posición del GPS tras abrir la app, el mapa se centra en ella una sola vez, como el botón de ubicación (mismo zoom y giro). No se centra si antes el usuario ha movido el mapa, ha usado el zoom o ha abierto una ficha. Sin permiso o sin señal, la vista inicial de siempre. La posición no se guarda en el móvil.

</frozen-after-approval>

## Implementation Notes

- `PantallaMapa.CentrarAlAbrir` (primer paso de `ActualizarCapas`, cada fotograma). `AlGestoUsuario`, `Acercar`/`Alejar` y `PrepararFicha` lo anulan. `GestoUsuario` se escucha ahora siempre (antes solo con navegación).
- «Una sola vez» se entiende como una por apertura: durante 10 s desde la primera posición (`ParametrosNegocio.CentradoAlAbrirSegundos`) también centran las siguientes, porque la primera del móvil puede ser antigua o poco precisa (revisión, hallazgo 4).
- La primera posición tras conceder el permiso también centra. Volver a la app desde recientes no vuelve a centrar. Fuera de Sanlúcar también se centra en el usuario.

## Review Triage Log

Revisión 1 (23/11/2026), Blind Hunter (única capa del camino oneshot).

| # | Hallazgo | Veredicto | Evidencia | Ruta |
|---|----------|-----------|-----------|------|
| 1 | Cuatro pruebas antiguas fallan (`ActivaLejos`) | high | 1170/1174 en el lote | patch: la ayuda simula el gesto |
| 2 | Con un dedo quieto en el mapa, el arrastre deshace el centrado | low | Pide el dedo puesto en el fotograma de la primera posición; el plazo de 10 s lo repite con la siguiente | rechazado |
| 3 | Un toque en curso puede caer en otro sitio | low | Mismo fotograma; raro | rechazado |
| 4 | La primera lectura puede ser antigua o imprecisa y gasta el centrado | medium | `UbicacionGps` pasa a Activa con cualquier lectura | patch: 10 s |
| 5 | Buscador, categorías o menú no lo anulan | low | La lista «cerca» se mide desde el usuario: centrar ayuda | rechazado |
| 6 | Volver desde recientes no centra | low | La petición habla de abrir la app; se avisa al usuario | rechazado |
| 7 | Fuera de Sanlúcar | false | Es lo pedido; el botón de ubicación ya lo hacía | rechazado |
| 8 | La captura de PC no lo muestra | low | Su ubicación falsa está en la vista inicial; lo cubren las pruebas | rechazado |
| 9 | Huecos de pruebas | low | Añadida la posición ya activa y el gesto posterior; el resto son caminos comunes | patch |
| 10 | La anulación va en cuatro sitios | low | Una asignación por sitio | rechazado |
| 11 | Comentario de `Acercar`/`Alejar` y fechas | low | Cierto | patch |
| 12 | Memoria y `CLAUDE.md` | low | Archivos de contexto | al cerrar |
