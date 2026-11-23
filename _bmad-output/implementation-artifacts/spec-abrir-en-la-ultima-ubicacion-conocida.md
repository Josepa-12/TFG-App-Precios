---
title: 'Abrir ya en la última ubicación del sistema'
type: 'feature'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
baseline_commit: '2f97e86c7fb037c2f6d62774cdc754e90036a27d'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Al abrir la app se ve un momento la Plaza del Cabildo hasta que llega el GPS; el usuario quiere que se abra directamente donde está (23/11/2026).

**Approach:** La vista inicial es la última ubicación que recuerda Android (la más reciente de sus proveedores), si hay permiso y la ubicación está encendida; al llegar el GPS se afina como hoy (10 s). La app no guarda ninguna posición. Sin ella, la vista inicial de siempre.

</frozen-after-approval>

## Implementation Notes

- `PuenteUbicacionRuta.ultimaConocida` (Java, `getLastKnownLocation`, sin encender el GPS) → `IFuenteUbicacion`/`IUbicacion.UltimaConocida` (miembros por defecto) → `PantallaMapa.Inicializar`.
- No cambia `Estado` ni `Posicion`: el cursor sale con la primera lectura del GPS.
- Vale aunque sea antigua o poco precisa: solo mueve la vista y el GPS la afina.

## Review Triage Log

Revisión 1 (23/11/2026), Blind Hunter.

- low: sin límite de antigüedad ni distancia — es lo pedido (abrir donde está); un límite añadiría reglas; rechazado.
- low: elige la más reciente aunque otra sea más precisa — solo la vista, el GPS la afina; rechazado.
- low: fallos de Java sin registro — patch (`Log.w`).
- low: la primera vez (al conceder el permiso) no la usa — solo pasa una vez y el GPS centra; rechazado.
- low: cadena con miembros por defecto y Java leído como texto — no hay pruebas de Android; queda la prueba en el móvil (`deferred-work`).
