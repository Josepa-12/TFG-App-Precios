---
name: captura-al-cerrar-historia
description: "Al terminar cada historia, enseñar al usuario capturas de lo hecho en una página Artifact como la de \"Controles del mapa\""
metadata:
  node_type: memory
  type: feedback
  modified: 2026-11-18T17:33:06.613Z
---

Cada vez que se termine una historia (o una parte), mandar al usuario una página Artifact con capturas de lo que se ha hecho en esa historia, al estilo de la comparación "Controles del mapa" (https://claude.ai/artifact/HXVbUfKb75jGH9dowSZQUE): claro y oscuro, a tamaño de móvil, con una lista corta de qué ha cambiado.

**Why:** el usuario (18/11/2026) quiere ver cómo va la app al cerrar cada historia; las capturas de PC a 900 dp de ancho le hicieron pensar que los controles eran demasiado pequeños.

**How to apply:** usar las capturas reales de la build de PC (`PriceMap.exe -captura`, ya a tamaño móvil tras el ajuste visual de los controles) de la historia, incrustadas como data URI o subidas como archivos del artifact; antes de implementar un cambio visual, enseñar una maqueta de la propuesta (pidió verla antes). Relacionado: [[estado-planificacion-app-final]], [[ritmo-implementacion-seguida]].
