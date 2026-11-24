---
name: sin-commits-sin-revision
description: "No commit (ni push) hasta que el usuario revise el código; trabajo de auditoría en la rama \"revision\""
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-16T14:11:52.279Z
---

No hacer commits ni push hasta que el usuario haya revisado todo el código y lo autorice expresamente. Desde el 16/11/2026 el trabajo de revisión/auditoría va en la rama `revision` (creada desde main en 309bde8).

**Why:** el usuario quiere revisar el código antes de que se registre nada en git; lo pidió explícitamente al lanzar la auditoría multiagente del prototipo.

**How to apply:** trabajar en la rama `revision`; al terminar cambios, dejarlos sin commit y avisar. Solo commit/push/copia al escritorio cuando el usuario lo pida tras revisar. Ver también [[memoria-tfg-viva]].
