---
name: ritmo-implementacion-seguida
description: "Cómo implementar las historias pendientes seguidas (decidido el 18/11/2026) — preguntas abiertas una a una, parar al final de cada épica, subagentes sí, commit por historia"
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-18T11:50:52.125Z
---

El 18/11/2026, al pedir implementar seguidas todas las historias pendientes (1.7b y épicas 2–9), el usuario decidió:
- **Preguntas abiertas de cada spec:** revisarlas una a una al llegar a cada historia (no aplicar la recomendada sin preguntar).
- **Paradas:** dar por aprobado el spec una vez resueltas sus preguntas, implementar y revisar sin parar; avisar y parar al cerrar cada épica.
- **Subagentes:** permitidos para investigar y para las revisiones de bmad-build.
- **Commits:** un commit por historia terminada y revisada (la revisión de bmad-build), sin esperar al final de la épica.

**Why:** quiere agilidad, pero decidir él cada pregunta de diseño.

**How to apply:** en cada historia, preguntar solo sus Open Questions (con AskUserQuestion, opción recomendada primero), luego seguir hasta el commit. Esto matiza [[sin-commits-sin-revision]] para esta tanda. Ver [[estado-planificacion-app-final]].
