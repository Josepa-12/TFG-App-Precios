# Revisión de los documentos de UX — TFG-App-Precios

## Veredicto general
Los dos documentos sirven ya de contrato: los colores salen exactos del prototipo, cada recorrido tiene clímax y caso de fallo, y los estados cubren lo esencial. Los riesgos para quien los use después son una sección obligatoria ausente (Inspiración y antipatrones, cuando hay tres productos de referencia), los títulos de los recorridos que no coinciden literalmente con el PRD y dos estados de navegación que faltan.

## 1. Cobertura de recorridos — fuerte
Los cinco UJ del PRD tienen protagonista con nombre, pasos numerados, clímax y caso de fallo.
### Hallazgos
- **[medium]** Los títulos de los recorridos no son literales del PRD (EXPERIENCE.md § Key Flows; p. ej. "Andrés confirma lo que pagó en la barbería" frente a "Andrés confirma el precio una hora después de cortarse el pelo"). *Fix:* usar los títulos exactos del PRD.

## 2. Completitud de tokens — adecuada
Todos los colores tienen valor hexadecimal y par oscuro; el contraste está declarado.
### Hallazgos
- **[medium]** La tipografía va en px, pero en Android el texto debe escalar en sp (DESIGN.md § typography). *Fix:* indicar que los tamaños se leen como sp y escalan con la fuente del sistema.
- **[low]** El token de componente `pin` se llama "Marcador de lugar" en la prosa (DESIGN.md § Components). *Fix:* renombrar el token a `marcador`.

## 3. Cobertura de componentes — adecuada
### Hallazgos
- **[low]** "Regla deslizable de precio" (DESIGN.md) y "Regla de precio" (EXPERIENCE.md) son el mismo componente con dos nombres. *Fix:* unificar como "Regla de precio".
- **[low]** "Botón secundario" no tiene fila de comportamiento. *Fix:* añadirlo junto al primario.

## 4. Cobertura de estados — adecuada
### Hallazgos
- **[high]** Falta el estado de **GPS sin señal** durante la navegación (EXPERIENCE.md § State Patterns): es el fallo más habitual en un GPS. *Fix:* mantener la última posición, aviso "Buscando señal GPS" y no crear ni descartar la visita mientras tanto.
- **[medium]** Falta **ficha cargando** (datos del servidor) y **perfil vacío** (sin aportes ni insignias). *Fix:* añadir ambos estados.
- **[low]** Buscador sin favoritos ni frecuentes. *Fix:* mostrar solo categorías y "Encuentra cerca".

## 5. Referencias visuales — escasa
No hay `mockups/`, `wireframes/` ni `imports/`; las capturas se citan como carpeta, sin decir cuál ilustra qué.
### Hallazgos
- **[medium]** Referencia poco específica a `docs/diseno/capturas/` (EXPERIENCE.md § IA). *Fix:* citar las capturas clave por pantalla (p. ej. `02-mapa-claro.png` → Mapa) o generar las maquetas de pantallas clave en el cierre.

## 6. Exceso y sobreespecificación — adecuada
### Hallazgos
- **[low]** Algunos tamaños se repiten en el frontmatter y en la prosa de Components (DESIGN.md). *Fix:* en la prosa, remitir al token.

## 7. Disciplina de herencia — adecuada
Las rutas de `sources` resuelven y las referencias de tokens de EXPERIENCE.md existen en DESIGN.md.
### Hallazgos
- Ver hallazgo de §1 (títulos de UJ) y §3 (nombre de la regla).

## 8. Encaje de forma — adecuada
DESIGN.md en el orden canónico; EXPERIENCE.md con todas las secciones obligatorias y Responsive & Platform.
### Hallazgos
- **[high]** Falta **Inspiration & Anti-patterns**, que es obligatoria cuando las fuentes nombran productos de referencia: Mapas de Apple (interfaz), Waze (botón €) y Discord (ajustes agrupados), y los descartes (bocetos B y C, paleta A, pestañas inferiores). *Fix:* añadir la sección con qué se toma de cada uno y qué se rechaza.

## Notas mecánicas
- Frontmatter completo en ambos documentos (name, status, created, updated, sources).
- Sin diagramas Mermaid.
