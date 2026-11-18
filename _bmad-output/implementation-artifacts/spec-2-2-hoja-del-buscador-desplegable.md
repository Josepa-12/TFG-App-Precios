---
title: 'Historia 2.2: hoja del buscador desplegable'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 2'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/EXPERIENCE.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No hay forma de buscar: falta la cápsula inferior y la hoja del buscador (FR13, UX-DR4, UX-DR5).

**Approach:** Cápsula de cristal abajo con el campo "Buscar en Sanlúcar" y el avatar. Al tocarla o arrastrarla sube una hoja que sigue al dedo, con la física de soltar de UX-DR5. Antes de escribir muestra las categorías y "Encuentra cerca" (favoritos y frecuentes llegan con la épica 3). La búsqueda real es de la 2.3.

## Boundaries & Constraints

**Always:**
- **Cápsula** (DESIGN `components.capsule`): altura mínima 56 dp, radio `pill`, cristal con la opacidad mínima que da 4,5:1 sobre el mapa; con "reducir transparencias" del sistema (si se puede leer) o en su defecto siempre en el tema oscuro, fondo opaco. Avatar de 40 dp a la derecha, sin acción hasta la 3.5 (genérico, sin cuenta).
- **Física de la hoja** (pura, en `FisicaHoja`, probada):
  - Sigue al dedo y se queda donde se suelta.
  - Lanzamiento de más de 0,6 px/ms (en dp/ms reales): abre o cierra del todo según el sentido.
  - Soltada por debajo del 15 % de la altura útil se pliega; por encima del 88 %, se abre.
  - Menos de 6 px de movimiento es toque (enfoca el campo y saca el teclado); más es arrastre (no saca el teclado).
  - Con la lista desplazada arriba del todo, arrastrar la lista hacia abajo mueve la hoja.
- **Contenido inicial:** fila de categorías (chips con icono vectorial: restaurantes, bares, supermercados, farmacias, gasolineras, gimnasios, salud, cultura) y entrada "Encuentra cerca". Las categorías aún no hacen nada (2.4).
- **TalkBack:** tocar la cápsula abre la hoja del todo; la hoja ofrece las acciones "Expandir" y "Contraer" (acciones personalizadas del nodo o dos botones visibles solo para el lector). Al abrir, el foco va al campo de búsqueda. Modal mientras está abierta.
- **Reducir animaciones:** sin transición al cambiar de altura (UX-DR19). La preferencia se lee del sistema (`Settings.Global.ANIMATOR_DURATION_SCALE == 0`) por un puerto.
- **Atrás:** cierra la hoja (pliega) antes que cualquier otra cosa.
- Convive con la píldora "Precios", la ficha y los botones redondos: la hoja abierta tapa los botones de abajo; la ficha y la hoja no están abiertas a la vez (abrir una cierra la otra).

**Never:** sin búsqueda real (2.3), sin favoritos ni frecuentes (épica 3), sin perfil al tocar el avatar (3.5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Arrastrar y soltar | Arrastre hasta 50 %, velocidad 0,1 px/ms | Queda al 50 % |
| Lanzar arriba | Velocidad 0,9 px/ms hacia arriba | Abierta del todo |
| Soltar baja | Suelta al 10 % | Plegada |
| Soltar alta | Suelta al 92 % | Abierta |
| Toque | Movimiento 3 px | Foco al campo y teclado |
| Arrastre corto | Movimiento 10 px | Arrastre, sin teclado |
| TalkBack | Toque en la cápsula | Abierta del todo, foco al campo, acciones Expandir/Contraer |
| Reducir animaciones | Activo | Cambios de altura instantáneos |
| Atrás | Hoja abierta | Se pliega |

</frozen-after-approval>

## Open Questions

1. **Altura "media".** UX-DR5 solo da los umbrales. A) La hoja se queda donde se suelte (sin alturas fijas intermedias), como dice el criterio (recomendado). B) Imán a 50 %.

## Code Map

- `Assets/UI/PantallaMapa.uxml/.uss`, `Estilos/Tokens.uss`: cápsula y hoja; tokens de cristal.
- Nuevos `Scripts/UI/CapsulaBuscador.cs`, `HojaBuscador.cs`, `FisicaHoja.cs` (pura).
- `Scripts/Aplicacion/`: puerto `IAccesibilidadSistema` (reducir animaciones/transparencias) y adaptador en `Adaptadores/Sistema`.
- `Scripts/UI/PantallaMapa.cs` (`Atras()`, capas modales), `AccesibilidadPantalla.cs` (acciones Expandir/Contraer, modal), `IconosLugar.cs` (glifos de categorías), textos.
- Referencia: `prototipo/app.js` (búsqueda `sheet`/`drag`), maqueta `key-mapa-inicio.html`.
- `CapturaPruebas.cs`: captura "buscador" con la hoja al 50 %.

## Tasks & Acceptance

**Execution:**
- [ ] `FisicaHoja` pura y su prueba (todas las filas de física).
- [ ] Cápsula, hoja y contenido inicial en UXML/USS.
- [ ] Gestos de arrastre, toque y lanzamiento con `PointerMove`/`PointerUp`.
- [ ] TalkBack, atrás y reducir animaciones.
- [ ] Captura "buscador" y `Tests/EditMode/PruebasBuscador.cs`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces la física cumple cada fila de la matriz.
- Dada la build de PC, cuando se abre `buscador.png`, entonces se ven la cápsula y la hoja a media altura con las categorías.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes y captura de PC; limpieza de `link.xml` y `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: arrastre, lanzamiento y teclado; TalkBack con Expandir/Contraer.
