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
- Interfaz principal (12/11/2026), estilo Mapas de Apple (propuesta A de [docs/diseno/bocetos/](docs/diseno/bocetos/)):
  - **Barra inferior:** cápsula de cristal con el buscador y el avatar. El avatar muestra el nivel y lleva la XP como anillo alrededor. Al pulsarlo se abre el perfil.
  - **Sin pestañas:** la tienda y los ajustes están dentro del perfil. Los ajustes van agrupados por secciones, tomando Discord como referencia.
  - **Categorías:** en un desplegable.
  - **Buscador:** se despliega hacia arriba y muestra favoritos, búsqueda por tipo y lugares frecuentes o visitados. Al escribir funciona como un buscador normal.
  - **Botón € pequeño y circular:** solo funciona si hay visitas pendientes (las que se dejaron en "Más tarde"). Avisa con un punto rojo pequeño.
- Para introducir un precio distinto se usa una regla deslizable y se puede escribir el número. Sustituye a los botones de +1/−1.
- Función "Avísame si cambia" en la ficha de cada sitio.
- El buscador se queda a la altura donde se suelta:
  - un lanzamiento rápido lo abre o lo cierra del todo;
  - cerca de abajo se pliega y cerca de arriba se abre del todo;
  - se puede buscar a media altura;
  - se arrastra también desde la lista.
- Coche o a pie se elige en la ficha del sitio (coche por defecto), no en los ajustes.
- Burbuja de sonido (silenciar o activar) encima de la burbuja de precios.
- La pregunta "¿has ido?" sale **1 hora después de llegar**, para dar tiempo a recibir el servicio. Si hay una ruta en curso, pasa a pendientes. En la app real será una notificación programada.
- Al llegar, la ruta sigue en pantalla hasta pulsar "He finalizado" o hasta que pase 1 minuto.
- Indicador de posición:
  - **en coche**, una flecha de rumbo;
  - **a pie**, un círculo con un cono translúcido que sigue la brújula del móvil. La brújula necesita HTTPS en el móvil.
- Barra del buscador fina, con el mismo hueco arriba y abajo.
- Cambios del 13/11/2026 (ver [docs/estudio-puntos-interes.md](docs/estudio-puntos-interes.md)):
  - **Mapa:** los establecimientos con precios se muestran con el **logo de su tipo de negocio** y una insignia €, no con el precio.
  - **Puntos de interés:** los reales de OpenStreetMap (supermercados, gimnasios, farmacias, gasolineras…) van integrados en el mapa con icono y nombre, como en Mapas de Apple. Son 22 categorías.
  - **"Encuentra cerca":** lista de categorías en el buscador (las de Mapas de Apple y más).
  - **GPS a cualquier destino:**
    - direcciones escritas (Photon);
    - ubicaciones compartidas por WhatsApp o Google Maps (enlace o coordenadas);
    - puntos marcados manteniendo pulsado el mapa;
    - favoritos sin precio ("Casa de mamá").
  - **Rutas:** por calles reales (OSRM), con indicaciones giro a giro. Las direcciones no se guardan en el historial.
  - **Perfil:** la tienda va encima de "Tu actividad".
- **Marcadores de precio ocultos por defecto.** El desplegable "Precios" (arriba a la izquierda) tiene las opciones Ninguno, Todos, Servicios locales, Hostelería y Ocio.
- **121 lugares reales** de Sanlúcar con plantilla de precio (89 de hostelería, 21 de ocio y 11 servicios locales), en lugar de los 7 inventados. Ver [docs/estudio-puntos-interes.md](docs/estudio-puntos-interes.md) (5b).
- **Sin precios inventados:**
  - el **negocio** puede declarar su precio inicial;
  - los **clientes** aportan lo que pagan (el primero, con recompensa extra);
  - con **3 aportes**, manda lo que pagan los clientes.

  Se incluyen las instalaciones públicas que cobran y se excluye la sanidad pública.
- Cambios del 13/11/2026, segunda parte (ver [docs/estudio-puntos-interes.md](docs/estudio-puntos-interes.md), 5c):
  - **Precios iniciales de internet:** solo los que publica el propio negocio en su web oficial, con enlace y fecha. Nada de agregadores. Por ahora: tarifas de Clínica Areas.
  - **Clínicas privadas:** tipos nuevos "Clínica privada" y "Fisioterapia". Se añaden Clínica Areas (no es solo fisioterapia: traumatología, suelo pélvico, readaptación, entrenamiento, obesidad) y FisioSanlúcar.
  - **Centros sanitarios públicos** (el Hospital Virgen del Camino es público): salen en el mapa solo con su ubicación, sin precios.
  - **Dos tipos de cuenta:**
    - **cliente:** aporta precios y gana monedas;
    - **negocio:** vincula su local o lo da de alta a mano si no aparece, y declara sus precios iniciales. No tiene monedas ni recibe la pregunta "¿has ido?".
  - **Inicio de sesión:** "Continuar con Google" como opción principal. En el prototipo es simulado; en la app real, Firebase Authentication o Google Identity Services. La verificación del negocio queda pendiente para la app real.
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
Concepto definido. Hay un prototipo web navegable (segunda iteración, 12/11/2026). Falta elegir la tecnología final del mapa y hacer la prueba técnica.

## Pendiente
1. ~~Enviar al tutor el tema~~ (enviado). Esperar aprobación. Temas a tratar: gamificación como eje, Unity vs nativo, política de uso de IA, requisitos de pruebas con usuarios, peso desarrollo/memoria.
2. Elegir tecnología para el mapa real 2.5D con estilo editable y hacer la prueba técnica.
3. ~~Fisioterapia: precio o valoración~~ → solo precio (decidido).
4. ~~Valoración y criterios~~ (decidido).
5. ~~Decidir si se incluye un modo "negocio" mínimo~~ → cuenta de negocio en el prototipo (13/11/2026). Pendiente: cómo verificar al dueño en la app real.
6. Escribir en C# las reglas de actualización de precios y el sistema de monedas/niveles, con una simulación para obtener resultados.
7. Preparar el anteproyecto o el correo al tutor.
8. Decidir si las visitas pendientes ("Más tarde") caducan y cuándo.
9. Puntos de interés (ver el estudio):
   - qué categorías abren precios después de las tres plantillas actuales;
   - si se importan los precios oficiales de las gasolineras;
   - si se crea la categoría "Bodegas y enoturismo".
