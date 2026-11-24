---
name: tokens-en-pregunta-tamano
description: "Al preguntar si se parte un spec, decir en la misma pregunta cuántos tokens mide el total"
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-21T12:53:27.329Z
---

Cuando pregunte si partir una historia o spec por tamaño, poner en la misma pregunta cuántos tokens consume el total (y, si se puede, cada parte).

**Why:** el usuario lo pidió el 21/11/2026 al partir la 2.7: quiere decidir viendo el tamaño real, no solo «es grande».

**How to apply:** en la pregunta de «Tamaño» de cada spec (paso 2 de `bmad-build`), escribir «el total mide unos N tokens; cada parte unos M». Ver [[ritmo-implementacion-seguida]].
