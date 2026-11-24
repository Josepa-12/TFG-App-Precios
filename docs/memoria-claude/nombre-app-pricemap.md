---
name: nombre-app-pricemap
description: "La app final se llama PriceMap (no PrecioLocal); paquete Android com.pricemap.app; mapa con OpenStreetMap, sin Cesium"
metadata:
  node_type: memory
  type: project
  modified: 2026-11-17T10:17:58.673Z
---

El 17/11/2026 el usuario decidió que la app se llama **PriceMap**. "PrecioLocal" queda solo como nombre interno de namespaces y ensamblados (`PrecioLocal.*`, fijados por la arquitectura). Identificador Android: `com.pricemap.app`.

El mapa se hace con OpenStreetMap (teselas vectoriales de OpenFreeMap) y Cesium se retira.

**Why:** lo pidió el usuario en la historia 1.1.

**How to apply:** en los textos visibles, el `productName`, la APK y Firebase usa PriceMap. No renombres los namespaces sin preguntar. Relacionado: [[estado-planificacion-app-final]].
