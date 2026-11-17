# Revisión editorial (estructura + redacción) — DESIGN.md y EXPERIENCE.md

**Lectura del propósito:** estos documentos existen para que quien construya la app en Unity (persona o agente) sepa cómo se ve y cómo funciona cada pantalla de PrecioLocal.

**Modelo de estructura:** Referencia (consulta por apartados, esquema fijo de BMAD).

**Recuento:** DESIGN.md 1.521 palabras; EXPERIENCE.md 2.866 palabras.

| # | Pasada | Texto original | Texto revisado | Cambios |
|---|---|---|---|---|
| U1 | structure | EXPERIENCE.md § Foundation: "Tema claro por defecto; oscuro en Ajustes." | QUESTION → resuelto: "El tema inicial sigue al del sistema; se cambia con el botón de tema o en Ajustes." | Contradecía § Responsive & Platform ("modo oscuro del sistema como valor inicial") |
| U2 | structure | EXPERIENCE.md § State Patterns, fila "Recompensa" | CUT | Repite la fila de Component Patterns; no es un estado (−12 palabras) |
| U3 | structure | EXPERIENCE.md § Component Patterns, fila "Plan" | Renombrar a "Botón Plan" | Se confundía con "Tarjeta del plan" |
| U4 | structure | EXPERIENCE.md § IA: tres tablas seguidas | MOVE: maquetas y capturas bajo un subtítulo "Referencias visuales" | Separa la arquitectura de las referencias |
| U5 | structure | DESIGN.md § Do's and Don'ts, dos filas sobre el amarillo | MERGE en una: "Amarillo solo para recompensas y el rango de hostelería, siempre como fondo con `reward-ink`" | Misma regla en dos filas (−10 palabras) |
| U6 | prose | "Sin arrastrar: tocar la cápsula la abre del todo, y expone las acciones de accesibilidad" | "Sin arrastrar: tocar la cápsula la abre del todo, y la hoja ofrece las acciones de accesibilidad" | Sujeto ambiguo |
| U7 | prose | "ubicación centra y, segundo toque, orienta al norte" | "ubicación centra el mapa y, con un segundo toque, lo orienta al norte" | Frase elíptica |
| U8 | prose | "no recalcula ni crea ni descarta la visita" | "no recalcula la ruta ni crea o descarta la visita" | Objeto de "recalcula" ausente |
| U9 | prose | "Zonas táctiles de 48 dp como mínimo, el mínimo de Android" | "Zonas táctiles de al menos 48 dp, el mínimo de Android" | Repetición de "mínimo" |
| U10 | prose | "nunca de comparador frío" | "nunca como un comparador frío" | Preposición |
| U11 | prose | "Siempre acompañados de texto, nunca solo color." | "Siempre van con un texto, nunca solo con color." | Concordancia y claridad |

**Resumen:** 11 recomendaciones (5 de estructura, 6 de redacción); cambio neto de unas −15 palabras (−0,3 %). La U1 corrige una contradicción real entre secciones. Ninguna sacrifica comprensión.
