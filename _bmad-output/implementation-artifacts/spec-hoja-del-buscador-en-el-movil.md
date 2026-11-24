---
title: 'La hoja del buscador responde bien en el móvil'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En el móvil (prueba del usuario, 24/11/2026) la hoja del buscador va lenta e imprecisa; tras arrastrarla, el siguiente arrastre no la mueve hasta que se le da un toque; y al tocar para escribir no se abre hasta que se cierra el teclado.

**Approach:** Arreglar las tres causas, comprobadas: (1) la disposición entra en bucle porque el alto de la cápsula sigue al redondeo a píxel de la línea del campo; (2) con el dedo capturado, Unity 6.3 ya no pasa los eventos por la raíz del panel, que es donde la hoja escucha (también la hoja de la ruta); (3) el campo se enfoca al bajar el dedo y el teclado de Android quita el foco a la app, que deja de dibujar y apaga el GPS. La hoja sigue al dedo en cada arrastre y, al tocar, se abre a la vez que sale el teclado. El aspecto y las paradas no cambian.

</frozen-after-approval>

## Implementation Notes

- Causas comprobadas: 489 errores «Layout update is struggling» en el registro del Redmi y reproducidos en un panel de 1080 × 2400 px a 2,75 (la línea oscila entre 45,82 y 46,18 dp); reparto de eventos con captura en el código de Unity 6000.3 (`EventDispatchUtilities`: con el puntero capturado se salta la bajada por los padres); con el teclado, `OnApplicationFocus(false)` y 2,5 s sin fotogramas, y `UnityPlayer.handleFocus` no avisa de la pérdida de foco si `consumesOutsideTouches` es false.
- `UI/EscuchaGesto.cs` (nuevo): escucha en toda la cadena, del elemento tocado a la raíz, y atiende cada evento una vez (en quien captura o en la raíz). Lo usan `HojaBuscador` y `TarjetaRuta` (mismo fallo).
- `HojaBuscador`: `MedirLinea` ignora cambios de menos de 1,5 píxeles; con el dedo, el campo no se enfoca al bajar (`focusController.IgnoreEvent`) sino al soltar un toque; un `PointerDown` repetido empieza un gesto nuevo.
- `UI/TecladoAndroid.cs` (nuevo, sacado de `FormularioCuenta`): `PasarToques` al enfocar y al desenfocar.
- Pruebas: `PruebasGestosEnPanel` (panel del tamaño del móvil y eventos repartidos por el panel, sin destino). Con el código anterior fallan todas; 1817/1817 con el nuevo. APK generada. Falta verlo en el móvil.

## Review Triage Log

Revisión blind-hunter (24/11/2026):

- Atrás en el teclado (`Canceled`) devuelve el texto a como estaba al enfocar: **maybe-false** (medium sin verificar); ya pasaba antes. Se mira en el Redmi; en `deferred-work`.
- Tocar el campo ya enfocado o el tirador con el teclado abierto, según el momento: **maybe-false** (low); solo se ve con un teclado de verdad. Se mira en el Redmi.
- El bucle vuelve si un píxel es 1 dp o más: **medium**, arreglado (umbral de 1,5 píxeles) con pruebas a 1, 1,5, 2,75 y 3,5.
- `AltoCapsula` con efecto al leerla: **low**, arreglado (solo `MedirLinea` guarda el alto). Que dependa de dónde se midió (0,4 dp): rechazado, no se ve.
- Prueba frágil por la velocidad al soltar: **low**, arreglado.
- Un elemento de fuera de la cadena que capture el puntero: **low**, rechazado; nada lo hace y el siguiente toque lo recupera.
- `HojaBuscador` ignora el resultado de `Empezar`: **false**; `AlBajar` solo llega dentro de un panel.
- El toque fuera del teclado llega al mapa y la hoja no deja sitio al teclado: **low**; lo primero es lo esperado y lo segundo ya era así; en `deferred-work`.
- `PasarToques` sin dueño: **low**, rechazado; un formulario y el buscador no se enfocan a la vez.
- Caminos sin prueba: **low**, añadidos el cancelado en pleno arrastre, el dedo que no llega a soltarse y la fila que desaparece.
- Documentación sin cerrar: **low**, hecho.
- `Vigilar(TextField)` para campos futuros: **low**, rechazado por ahora (dos usos); anotado en CLAUDE.md.
