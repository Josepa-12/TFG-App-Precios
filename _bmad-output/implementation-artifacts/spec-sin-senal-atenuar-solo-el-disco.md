---
title: 'Sin señal, atenuar solo el disco de la flecha'
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

**Problem:** En el tema oscuro y sin señal GPS, la flecha se distingue poco dentro del disco atenuado (los dos al 40 %).

**Approach:** Navegando en coche sin señal se atenúa solo el disco; la flecha se ve entera (decisión del usuario, 23/11/2026; sustituye a «el disco y la flecha juntos» del spec del cursor). A pie y sin ruta, el círculo y el cono se atenúan como hasta ahora.

</frozen-after-approval>

## Implementation Notes

- `MarcadorPosicion.Atenuar`: la opacidad va en el marcador entero sin flecha y en `Disco` con ella.
- Revisión: al 40 %, en oscuro el disco compuesto sobre la calle y la flecha `#4C7DFF` daban 1,0:1. Disco al 70 % (`OpacidadDiscoSinSenal`) y flecha en `--color-flecha-sin-senal` (`#2F5BEA` en los dos temas): unos 3:1.

## Review Triage Log

Revisión 1 (23/11/2026), Blind Hunter.

- medium: contraste 1,0:1 en oscuro — comprobado con los colores; patch (70 % y azul oscuro).
- low: `CapturaPruebas` daba por buena la opacidad del marcador — patch.
- low: sin prueba de pasar de flecha a círculo sin señal — patch.
- low: comentarios antiguos («atenuada») — patch.
- low: memoria y `CLAUDE.md` — al cerrar.
