# Revisión de calidad del PRD — PrecioLocal (app final)

## Veredicto general
El PRD tiene una tesis clara (el precio como dato principal, mantenido por clientes con visita verificada y empujado por la gamificación), recorridos con protagonistas que sí condicionan los requisitos y un glosario sólido. El mayor riesgo es de alcance: 58 FR en Unity con unas 3 semanas hasta las pruebas con usuarios y sin priorización dentro del MVP. Faltan además reglas concretas en los puntos que deciden la "visita verificada" y un requisito para el grupo sin gamificación que exige SM-3.

## Preparación para decidir — adecuada
Las decisiones de plataforma (Android con Unity), mapa (opción C, en el anexo) y regla "mandan los clientes" están tomadas y razonadas. Pero el PRD no reconoce la tensión entre el MVP de §8.1, que incluye todo el prototipo, y el calendario: hoy es 16/11, la semana del 2-8/12 es para pruebas con usuarios y la entrega es el 11/12.

### Hallazgos
- **[critical]** Alcance sin priorizar frente al plazo (§8.1) — El MVP mete los 11 bloques de funciones sin orden. Con ~3 semanas de desarrollo en Unity no es creíble hacerlo todo, y las historias no sabrán qué sacrificar. *Fix:* priorizar cada bloque (Imprescindible / Deseable / Si da tiempo) y añadir un `[NOTE FOR PM]` sobre el riesgo de plazo.

## Sustancia frente a relleno — fuerte
Cinco protagonistas con nombre y contexto, cada uno ligado a requisitos. La visión es específica del producto. Los NFR tienen umbrales (30 fps, 3 s, 4,5:1, 44 px).

### Hallazgos
- **[low]** Precio de ejemplo en UJ-1 (§2.3) — "desde 35 €" puede no coincidir con la tarifa real de Clínica Areas. *Fix:* marcarlo como ilustrativo.

## Coherencia estratégica — adecuada
SM-3 valida la pregunta de investigación (¿la gamificación hace que se mantengan los precios?), pero ningún FR permite tener un grupo sin gamificación.

### Hallazgos
- **[high]** Falta el modo sin gamificación (§4.9, SM-3) — Sin él no se puede medir SM-3. *Fix:* nuevo FR: la app puede funcionar con la gamificación desactivada para un grupo de prueba (sin monedas, niveles, tienda ni insignias; la pregunta de llegada igual).
- **[medium]** Datos de la prueba (§9) — SM-2, SM-3 y SM-4 necesitan registrar visitas, aportes y frescura por participante. *Fix:* FR de registro anónimo de eventos para la evaluación.

## Claridad de "hecho" — escasa en puntos clave
Casi todos los FR son comprobables, pero los que deciden qué cuenta como visita verificada y cuándo se recalcula no tienen umbral.

### Hallazgos
- **[high]** Llegada sin umbral (FR-22, FR-23) — "Al llegar" no dice a qué distancia; y no queda claro si terminar la ruta antes de llegar crea visita. *Fix:* llegada = posición a ≤ 30 m del lugar; solo crea visita verificada si se llegó con la navegación activa; terminar antes no la crea.
- **[medium]** Desvío sin umbral (FR-19) — *Fix:* recalcular si la posición se aleja > 40 m de la ruta durante > 5 s.
- **[medium]** "No vinculado visiblemente" (FR-30) — *Fix:* el aporte se guarda sin el identificador del cliente en la parte pública; solo cuenta para sus recompensas.
- **[low]** "Pantallas principales" en NFR-4 — *Fix:* enumerarlas (mapa, ficha, buscador, pregunta de llegada, perfil).

## Honestidad del alcance — fuerte
No objetivos claros, fuera del MVP razonado, suposiciones marcadas e indexadas.

### Hallazgos
- **[low]** Las 6 suposiciones ya están confirmadas por el usuario. *Fix:* quitar las etiquetas y pasar el índice a "Confirmadas".

## Utilidad para lo siguiente (épicas e historias) — adecuada
IDs FR-1 a FR-58 contiguos, UJ-1 a UJ-5 con protagonista, glosario usable.

### Hallazgos
- **[medium]** "Usuario" sin definir (§3) — Los FR usan "el usuario" para cualquiera, con o sin cuenta, y "cliente" para la cuenta. *Fix:* añadir "Usuario" al glosario.
- **[high]** Falta la seguridad de los datos compartidos (§5) — Con datos en un servidor, nada impide que un cliente escriba aportes ajenos o que un negocio edite otro lugar. *Fix:* NFR-8 de reglas de acceso en el servidor.

## Encaje de forma — fuerte
App de consumo con recorridos con protagonista: la forma es la adecuada.

## Notas mecánicas
- IDs de FR contiguos (1-58), sin duplicados. SM-1 a SM-5 y SM-C1, SM-C2 correctos.
- Índice de suposiciones coincide con las etiquetas en línea (6 de 6).
- §0 remite a §11 para las suposiciones: correcto.
