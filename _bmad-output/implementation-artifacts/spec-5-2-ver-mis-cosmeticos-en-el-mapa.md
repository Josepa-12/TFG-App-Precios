---
title: 'Historia 5.2: ver mis cosméticos en el mapa'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 5'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-5-1-comprar-y-equipar-cosmeticos-en-la-tienda.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Equipar un cosmético no cambia nada en el mapa (FR50, UX-DR8).

**Approach:** `MarcadorPosicion` (círculo, cono y flecha) y la línea de ruta leen el estilo equipado de `ServicioProgreso.Equipados` y cambian al momento. Los estilos se definen como tokens de DESIGN.md.

## Boundaries & Constraints

**Always:**
- **Estilos de posición** (`EstiloPosicion`, en la UI, valores desde USS):
  - Clásica: círculo `#2F5BEA` con borde blanco y cono translúcido (1.7).
  - Dorada: relleno degradado dorado, borde blanco, cono dorado.
  - Neón: relleno cian intenso con halo exterior (anillo translúcido de 8 dp) y cono cian.
  - La flecha de navegación (2.7) usa los mismos colores.
- **Estilos de ruta** (`EstiloRuta`, pasado a `IMapa.MostrarRuta`):
  - Clásica: línea continua `--color-route`.
  - Discontinua: guiones de 12 dp con huecos de 8 dp.
  - Dorada: línea dorada con borde oscuro de 2 dp.
  - Implementados en la malla de `LineaRuta` (2.6) con UV a lo largo para los guiones.
- **Cambio al momento:** al equipar, el marcador y la ruta activa se repintan sin reiniciar la navegación.
- **Contraste:** cada estilo con contraste suficiente sobre el mapa claro y oscuro (prueba en `PruebasContraste` contra los colores de fondo del mapa, ≥ 3:1 para elementos gráficos).
- Sin cuenta o negocio: siempre clásicos.
- Los tokens nuevos se añaden a `Tokens.uss` y a DESIGN.md si faltan (avisando en el spec si se cambia DESIGN).

**Never:** sin animaciones de los cosméticos con "reducir animaciones" (el halo neón no pulsa).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Posición dorada | Equipada | Círculo y cono dorados |
| Neón en coche | Navegando en coche | Flecha cian con halo |
| Ruta discontinua | Navegando | Línea a guiones |
| Equipar en ruta | Cambia a dorada durante la navegación | La línea cambia sin recalcular |
| Sin cuenta | — | Clásicos |
| Contraste | Dorada sobre fondo claro | ≥ 3:1 |

</frozen-after-approval>

## Code Map

- `UI/MarcadorPosicion.cs` (estilos), nuevo `UI/EstiloPosicion.cs`.
- `Dominio/` o `Aplicacion/`: `EstiloRuta` (enum por id de catálogo); `Aplicacion/IMapa.cs` (`MostrarRuta` con estilo); `Vectorial/LineaRuta.cs` (guiones, borde).
- `Assets/UI/Estilos/Tokens.uss`; `Tests/EditMode/PruebasContraste.cs`.
- `CapturaPruebas.cs`: capturas "cosmetico-dorada" y "ruta-discontinua".

## Tasks & Acceptance

**Execution:**
- [ ] Estilos de posición y su lectura desde el servicio.
- [ ] Estilos de ruta en la malla.
- [ ] Repintado al equipar y pruebas de contraste.
- [ ] Capturas.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan las pruebas de contraste y de cambio de estilo.
- Dada la build de PC, cuando se abren las capturas, entonces se ven los estilos dorado y discontinuo.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; capturas de PC.
