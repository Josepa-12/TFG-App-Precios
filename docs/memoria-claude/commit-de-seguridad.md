---
name: commit-de-seguridad
description: "hacer commit en cuanto una historia esté implementada y revisada, antes de la revisión del usuario, por si se corta el uso"
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-20T15:44:18.520Z
---

Cuando una historia está implementada y ha pasado la revisión de código de BMAD, hacer el commit ya, sin esperar a la revisión del usuario. El usuario la revisa después; si pide cambios, van en otro commit o se rehace ese.

**Why:** el 20/11/2026 el usuario quiso un commit "justo antes de quedarte sin uso" para no perder trabajo; Claude no ve cuánto uso le queda, así que se acordó este punto fijo.

**How to apply:** matiza [[sin-commits-sin-revision]]: el commit de seguridad es la excepción tras implementar y revisar; avisar al usuario de que está hecho y pendiente de su revisión. No hacer push.
