# TFG — App GPS colaborativa de precios de servicios locales

Responde siempre en español.

## Contexto
- Estudiante del grado de diseño, animación y programación de videojuegos y entornos interactivos 2D y 3D.
- **Entrega: 11 de diciembre de 2026.**
- Calendario propuesto (11/11/2026):
  - 11-17 nov: tema al tutor (ENVIADO), instalar Unity, prueba técnica del mapa. El 16 nov se decide el mapa: 3D si funciona; si no, mapa 2D plano.
  - 18-24 nov: núcleo (establecimientos, 3 categorías, llegada → pregunta del precio, reglas de precio, GPS simulado).
  - 25 nov-1 dic: gamificación (puntos, niveles, monedas, 2-3 cosméticos), interfaz, compilación Android.
  - 2-8 dic: pruebas con usuarios (SUS) y cierre de la memoria (redactada en paralelo desde la semana 2).
  - 9-11 dic: margen y entrega.
- Ha usado Unity antes, pero recuerda poco: explicar los pasos de Unity con claridad.

## Tema
App móvil de mapa/GPS colaborativo cuya idea principal es el **COSTE** de establecimientos y servicios locales (la reseña es secundaria). Problema: no se sabe cuánto cuesta un servicio antes de ir, y Google Maps no lo resuelve.

## Decisiones tomadas por el estudiante
- Mapa interactivo que funcione como GPS, alternativa a los GPS normales.
- **Mapa real (no creado a mano), en 2.5D** (inclinado, con edificios extruidos), con **estilo editable por el estudiante** sobre el mapa base.
- Sistema de puntos, niveles y monedas para cosméticos del GPS, para incentivar aportaciones.
- La app pregunta por el servicio cuando el usuario usa el GPS para ir a un sitio (sin seguimiento en segundo plano).
- Cualquier tipo de establecimiento.
- Tres modelos de precio (decidido 12/11/2026):
  - **Servicios locales** (fisioterapia, psicología, peluquería, etc.): unificados, **un único precio** por servicio. **Sin reseñas.**
  - **Restaurantes y bares de copas:** **solo rango de precio** por persona (ej. 20-30 €), nunca precios fijos. **Con valoración.**
  - **Establecimientos de ocio:** **varios precios por establecimiento**, uno por elemento/instalación (ej. polideportivo: pista de fútbol A = 12 €, pista B = 10 €). Alquiler de espacios: **precio total** (pista = 12 €, no 2 €/persona). Actividades como escape room: **precio por persona**. **Con valoración.**
- Valoración (decidido): 3 criterios por categoría, de 1 a 5 estrellas.
  - Hostelería: calidad (comida/bebida), servicio, ambiente.
  - Ocio / deporte: instalaciones, atención, calidad-precio.
- Identidad visual (12/11/2026): paleta B (azul #2F5BEA + amarillo #FFC93C, fondo #F3F5FB, texto #13235B) en modo claro y paleta D (fondo #0F1424, acento #4C7DFF, amarillo #FFD166, texto #E6ECFA) en modo oscuro. Tipografía Varela Round + Nunito Sans. Ver docs/diseno/.
- Prototipo navegable web en `prototipo/`, situado en Sanlúcar de Barrameda, con mapa real 2.5D (MapLibre + OpenFreeMap) y establecimientos inventados.
- Modelo unificado: establecimiento → lista de conceptos con precio (ver [docs/estudio-tipos-negocio.md](docs/estudio-tipos-negocio.md)).
- El negocio puede declarar un precio inicial (opcional). El poder está en los clientes: si reportan un precio distinto, la app actualiza el precio mostrado.
- Pregunta de un solo toque: "¿el coste fue X?" / "¿sigue costando X?".
- Los servicios locales no llevan reseñas, solo coste.
- Idea de avisos tipo Waze (controles, radares, accidentes) con aviso de uso por copiloto. Fuera del alcance del prototipo.

## Propuestas de Claude (NO confirmadas — tratar como sugerencias)
- Pregunta de investigación: ¿puede la gamificación integrada en la navegación conseguir que los usuarios mantengan actualizados los precios de servicios locales?
- Plantillas por categoría: tipo de precio, criterios de valoración y módulos activos (precio, valoración, comentario).
- Reglas de actualización del precio: mínimo de reportes coincidentes, mediana de reportes recientes, solo visitas verificadas, mostrar el origen del dato (declarado por el negocio / según N usuarios en los últimos X meses).
- Preguntar primero "¿has ido finalmente?" y después el coste. Considerar sesgo de confirmación al mostrar el precio en la pregunta.
- Privacidad: consentimiento explícito, datos mínimos, aportes no vinculados a la identidad en categorías sensibles (psicología).
- Prototipo con un ejemplo de cada modelo de precio: un servicio local (fisio/psicología), un restaurante o bar de copas, un polideportivo con varias pistas.
- Fuera de alcance: avisos tipo Waze, Android Auto/CarPlay, motor de rutas propio, moderación avanzada, panel de negocio completo.
- Validación: pruebas con 5-20 usuarios, idealmente con y sin gamificación, cuestionario SUS.
- Criterios de valoración (2–3 por categoría):
  - Hostelería: calidad (comida/bebida), servicio, ambiente.
  - Ocio / deporte: instalaciones, atención, calidad-precio.

## Tecnología
- Requisito confirmado (12/11/2026): mapa real en 2.5D con estilo editable → descarta el mapa simulado/creado a mano.
- Encaje natural: mapas vectoriales con estilo propio (Mapbox Studio o, como alternativa abierta, MapLibre + Maputnik), con cámara inclinada y edificios extruidos para el 2.5D.
- ~~Camino A: Unity con mapa simulado~~ (descartado por el requisito de mapa real).
- **Camino B:** app con mapa real fuera de Unity (framework móvil/web). Más cómodo con Claude Code, menos alineado con el grado.
- Mapa real en Unity: Mapbox Maps SDK for Unity (estilos de Mapbox Studio; v2 sin desarrollo activo; en Unity 6 hay que ajustar un script). Alternativa: Cesium for Unity (gratis, globos 3D, sin navegación).
- Prueba técnica recomendada (2-3 días): mapa con estilo propio, posición GPS y marcador en un establecimiento de prueba, en un móvil real.

## Equipo del estudiante
- PC con Windows. Unity Hub 3.22.6 instalado (11/11/2026); el editor aún no está instalado.
- Móvil: **iPhone**. Desde Windows no se pueden compilar apps iOS (hace falta un Mac con Xcode). Opciones pendientes de decidir: Mac prestado o de la universidad, Android prestado para las pruebas, o desarrollar con GPS simulado en el editor.

## Estado
Fase de definición del concepto y alcance. Sin código ni prototipo todavía. Hay un resumen de 5 líneas para el tutor.

## Pendiente
1. ~~Enviar al tutor el tema~~ (enviado). Esperar aprobación. Temas a tratar: gamificación como eje, Unity vs nativo, política de uso de IA, requisitos de pruebas con usuarios, peso desarrollo/memoria.
2. Elegir tecnología para el mapa real 2.5D con estilo editable y hacer la prueba técnica.
3. ~~Fisioterapia: precio o valoración~~ → solo precio (decidido).
4. ~~Valoración y criterios~~ (decidido).
5. Decidir si se incluye un modo "negocio" mínimo.
6. Escribir en C# las reglas de actualización de precios y el sistema de monedas/niveles, con una simulación para obtener resultados.
7. Preparar el anteproyecto o el correo al tutor.
