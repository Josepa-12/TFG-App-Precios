---
title: 'Historia 6.1: ver mi actividad'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 6'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El cliente no ve lo que tiene pendiente ni lo que ha aportado (FR57).

**Approach:** Sección "Tu actividad" del perfil con cuatro bloques: visitas pendientes, mis aportes (copia privada), favoritos e insignias. Solo lectura de servicios existentes; la UI no ve Firestore (AD-15). Los avisos activos (7.3) se añaden luego como quinto bloque.

## Boundaries & Constraints

**Always:**
- **Pendientes:** lugar, fecha ("hoy 17:30", "ayer") y botón "Responder" que abre su pregunta (4.3).
- **Mis aportes:** lugar, concepto, valor formateado ("40 €", "10–20 € por persona"), fecha y tipo (confirmación, otro precio, pionero). Más recientes primero; páginas de 20.
- **Favoritos:** los de la 3.6; tocar abre la ficha.
- **Insignias:** resumen "2 de 6" con acceso a la sección de la 5.3.
- **Vacío** (UX-DR16, UX-DR20): sin aportes, "Aún no has aportado precios. Navega a un sitio con la app y te preguntaremos al llegar." Cada bloque vacío con su texto corto.
- **Sin conexión:** lo que tenga la caché de Firestore.
- TalkBack: cada bloque con encabezado; cada fila con su texto completo.
- Negocio: no ve esta sección (ve su panel, 8.5).

**Never:** sin borrar aportes (AD-4), sin estadísticas comparadas con otros.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Con datos | 1 pendiente, 3 aportes, 2 favoritos, 1 insignia | Cuatro bloques con su contenido |
| Vacío | Cuenta nueva | Texto de "Aún no has aportado precios…" |
| Responder | Toque en pendiente | Abre su pregunta |
| Paginación | 45 aportes | 20 y "Ver más" |
| Sin conexión | Modo avión | Datos de caché |
| Tramo | Aporte [1000, 2000] | "10–20 € por persona" |

</frozen-after-approval>

## Code Map

- Nuevos: `UI/PantallaActividad.cs` (+ uxml/uss).
- `Aplicacion/ServicioAportes.cs` (mis aportes paginados), `ServicioVisitas.cs`, `ServicioLugares.cs` (favoritos), `ServicioProgreso.cs` (insignias).
- `Adaptadores/Firebase/AportesFirestore.cs` (consulta de la copia privada ordenada).
- `UI/PantallaPerfil.cs` (entrada). Tests: `PruebasActividad.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Consultas en los servicios.
- [ ] Pantalla con bloques, vacíos y paginación.
- [ ] TalkBack; capturas "actividad" y "actividad-vacia".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; capturas de PC.
