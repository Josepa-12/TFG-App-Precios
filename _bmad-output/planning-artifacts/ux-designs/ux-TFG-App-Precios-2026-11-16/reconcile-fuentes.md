# Conciliación de fuentes con DESIGN.md y EXPERIENCE.md

Fuentes: PRD final y anexo, `prototipo/` (styles.css, app.js, README), memoria 4.10-4.11, capturas `docs/diseno/capturas/`.

## Cubierto
- Paletas B y D con sus valores exactos de `styles.css`, incluidos los ajustes de contraste de la revisión (texto oscuro sobre el acento oscuro, bordes de control ≥ 3:1).
- Paleta del mapa de `PaletaMapa` (Unity).
- Tipografías, radios (14 px dominante), botones de 52 px, píldoras de 44 px, marcadores de 42 px.
- Hoja arrastrable con sus umbrales (0,6 px/ms, 15 %, 88 %, 6 px).
- Fila Ir bajo la cabecera, color por plantilla, "Ver precios", plan con 1 persona, "Más de 50 €" abierto (decisiones de la auditoría, memoria 4.11e).
- Los cinco recorridos del PRD (UJ-1 a UJ-5) con sus casos de fallo.

## Huecos encontrados y resueltos
1. **Sonidos** (FR-53, prototipo con Web Audio): no estaban en EXPERIENCE.md → añadidos en Interaction Primitives.
2. **Naranja de hostelería en oscuro**: el prototipo no lo definía → `tpl-hosteleria-dark` marcado como [ASSUMPTION].
3. **Zonas táctiles**: el prototipo usaba 44 px (web); en Android el mínimo es 48 dp → [ASSUMPTION] en Accessibility Floor.
4. **Horizontal**: el prototipo tenía un fallo pendiente en 844×390 → la v1 bloquea la orientación vertical ([ASSUMPTION]).

## Sin hueco
- Las capturas sirven de referencia de composición; los documentos mandan en caso de conflicto.
