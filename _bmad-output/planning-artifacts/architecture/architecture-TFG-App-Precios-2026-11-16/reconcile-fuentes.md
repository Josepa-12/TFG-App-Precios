# Conciliación de fuentes con la arquitectura

## PRD (prd.md + addendum.md)
- **Cubierto:** las 12 áreas de funciones tienen sitio y reglas en el mapa de capacidades; NFR-1 (rendimiento del mapa: adaptador Vectorial existente), NFR-2 (AD-8), NFR-6 (AD-10), NFR-7 (AD-7), NFR-8 (AD-5, AD-6).
- **Hueco resuelto:** NFR-3 (GPS solo con navegación) no aparecía → convención "GPS y batería".
- **Divergencia con el PRD (requiere actualizar la fuente):**
  - FR-43 dice "código enviado al teléfono o al correo"; con Spark solo es viable el correo (y aprobación manual si no hay correo público).
  - FR-12 implica una notificación cuando cambia el precio; sin servidor solo se comprueba al abrir la app o al volver a primer plano.
  - El anexo proponía FCM: ya no se usa.

## UX (DESIGN.md + EXPERIENCE.md)
- **Cubierto:** la UI observa estado (AD-15); tokens de DESIGN.md como única fuente de estilo (convención "Tema y estilo"); IMapa cubre marcadores, ruta, posición y toques.
- **Hueco resuelto:** el suelo de accesibilidad (TalkBack, alternativas sin arrastre) no tenía reflejo técnico → convención "Accesibilidad".
- **Sin hueco:** estados sin conexión y GPS sin señal encajan con AD-8 y AD-9.
