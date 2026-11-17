# Revisión con la lista de un buen spine

## Veredicto
Fija los puntos de divergencia principales (capas, dueño de cada dato, precio derivado, visita, importes, ids, mapa) y respeta el código existente. Falla en el anonimato y la forma de los datos (ver revisión adversaria) y deja callada una parte del entorno operativo.

## Hallazgos
- **[high] Entorno operativo incompleto.** Despliegue y entornos están, pero no hay nada sobre copias de seguridad de Firestore, monitorización de errores ni cómo se siembran los datos base y `contactosLugares`. *Fix:* sembrado con un script del administrador versionado en `firebase/`; copias y monitorización a Deferred con su motivo.
- **[medium] AD-12 vago.** "Compara con la hora del servidor de la última sincronización" no dice qué hora usar sin conexión. *Fix:* el dominio recibe "ahora" como parámetro; Aplicación usa la hora del servidor estimada (hora local + desfase medido en la última lectura) y, sin datos, la hora local.
- **[medium] Ciclo de vida de los datos empaquetados.** Si una versión nueva de la app cambia un lugar base, sus aportes en Firestore siguen ligados por id (AD-11), pero no se dice qué pasa si un lugar desaparece. *Fix:* los ids de lugares base no se reutilizan ni se borran; un lugar retirado se marca como cerrado.
- **[low] Ratificación del código existente.** Correcta: mantiene `PrecioLocal.Vectorial`, `Geo` y los nombres en castellano.
