# Conciliación de fuentes con el PRD

Fuentes cotejadas: documento "PrecioLocal — Todo lo realizado en el prototipo web", `docs/memoria-tfg.md` (cap. 4 y 6), `prototipo/README.md`, `docs/estudio-tipos-negocio.md`, `docs/estudio-puntos-interes.md`.

## Cubierto
Todas las funciones del prototipo tienen su FR: mapa y filtro (FR-1 a FR-6), fichas y plantillas (FR-7 a FR-12), buscador y destinos (FR-13 a FR-17), navegación (FR-18 a FR-22), pregunta de llegada (FR-23 a FR-30), reglas, frescura y avisos (FR-31 a FR-36), cuentas (FR-37 a FR-42), negocio y ofertas (FR-43 a FR-47), gamificación (FR-48 a FR-53), plan (FR-54 a FR-56), perfil (FR-57, FR-58). Privacidad, accesibilidad y no objetivos coinciden con la memoria (4.9, 4.10, 4.12).

## Huecos encontrados
1. **Aspecto y tono (cualitativo, se perdía):** la identidad visual de la memoria (4.10) y del prototipo no estaba en el PRD: estilo de Mapas de Apple con controles flotantes y cápsula de cristal, tipografías Varela Round y Nunito Sans, el amarillo reservado para recompensas y el rango de hostelería, el azul para acciones y ruta, iconos SVG sin emojis y un tono cercano en los textos. → Se añade §6.4 "Aspecto y tono".
2. **Variante del precio:** el estudio de tipos de negocio describe cuatro dimensiones (conceptos, forma, unidad y **variante**: horario, día, socio). El PRD recoge las tres primeras; la variante no aparece y el prototipo tampoco la implementa. → Se añade como pregunta abierta.
3. **Límite de los enlaces cortos:** el prototipo no resolvía `maps.app.goo.gl`; el PRD exige resolverlos (FR-16) y el anexo explica cómo. Sin hueco.
