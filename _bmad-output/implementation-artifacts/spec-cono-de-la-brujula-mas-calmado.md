---
title: 'El cono de la brújula se mueve menos'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El cono de la posición sigue a la brújula al instante y se mueve con un solo grado de cambio: tiembla todo el tiempo (petición del usuario, 24/11/2026: «se mueve demasiado», «menos sensible»).

**Approach:** Filtrar la brújula antes de mostrarla: el rumbo se queda quieto hasta que el móvil gira unos 10° (opción elegida por el usuario entre 5°, 10° y 20°) y entonces gira suave, sin saltos, en alrededor de medio segundo. El dibujo del cono no cambia. Vale también para la cámara al navegar a pie, que usa el mismo rumbo.

</frozen-after-approval>

## Implementation Notes

- `Dominio/FiltroBrujula.cs` (nuevo, puro): suavizado del temblor (constante 0,15 s); el rumbo mostrado no se mueve hasta que el suavizado se aparta 10° y entonces se acerca a él (0,17 s) hasta quedar a 0,5°. Constantes en `ParametrosNegocio`.
- `Adaptadores/Gps/UbicacionGps.cs`: `LeerBrujula` pasa la brújula por el filtro con un reloj inyectable; desaparece `UmbralRumbo` (1°). Sin rumbo antes, la primera lectura sale tal cual.
- El dibujo del cono y el umbral de 2° de la cámara a pie no cambian.
- Se dio por terminado el giro a 0,5° y no a 1°: con 1° quedaba a 1,05° del final.
- Verificación: 1802/1802 pruebas EditMode; APK generada. Falta la prueba en el móvil.

## Review Triage Log

Revisión blind-hunter (24/11/2026):

- Primera lectura imposible (NaN) → cono al norte: **low**, arreglado (`LeerBrujula` la descarta y conserva el rumbo, o ninguno) con prueba.
- Vuelta de la pausa sin reiniciar el filtro: **low**, rechazado; con más de 10° se coloca al momento (probado) y con menos se queda quieto, que es lo pedido.
- Zona quieta descentrada (se para en un extremo del balanceo): **low**; real, pero solo se vuelve a disparar con balanceos de más de unos ±8° a 1 Hz. Añadidas pruebas de balanceo tras un giro a 1, 2 y 5 Hz.
- Giros lentos a pasos de 10°: **false**; es el comportamiento elegido («quieto hasta 10° y entonces gira suave»).
- Cámara a pie más retrasada y hasta 10° desviada: **false**; la Intent lo incluye y las pruebas de la cámara siguen pasando.
- «Reducir animaciones»: **false**; el filtro sigue a un sensor, como la cámara; sin él volvería el temblor.
- `(float)` puede dar 360: **low**, arreglado (`Mathf.Repeat` después).
- `Cambiado` en cada fotograma durante un giro: **false**; antes avisaba en cada fotograma con 1° de temblor, ahora solo al girar.
- Reloj hacia atrás contado dos veces: **false**; `realtimeSinceStartupAsDouble` no retrocede.
- «No se pasa» solo miraba el final: **low**, arreglado (máximo de todos los fotogramas).
- Independencia de los fotogramas sin probar: **low**, arreglado (5, 15, 30 y 120 fps) y comentario rebajado.
- Prueba de temblor fácil: **low**, arreglado (balanceos a 1, 2 y 5 Hz).
- Sin pruebas directas de `FiltroBrujula`: **low**, rechazado; se prueba entero a través de `UbicacionGps`.
- Spec sin notas y documentación sin actualizar: **low**, arreglado (notas, memoria, CLAUDE.md y comentario de `IUbicacion.Rumbo`).
- Sin plan de prueba en el móvil: **low**; se prueba en el Redmi en esta misma sesión.
