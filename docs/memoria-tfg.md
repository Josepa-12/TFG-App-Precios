# Memoria del TFG — borrador de referencia

> **Qué es este documento.** Es un borrador que Claude va completando conforme avanza el proyecto. Recoge lo que se ha decidido y por qué, para que al final puedas leerlo y redactar **tu propia memoria con tus palabras**. Antes de reutilizar texto, consulta con el tutor la política de uso de IA.
>
> **Estado de cada apartado:** **[Redactado]** contenido estable · **[Parcial]** falta información · **[Pendiente]** aún no se ha llegado ahí.
>
> **Última actualización:** 24/11/2026

---

## Datos generales

- **Título provisional:** App GPS colaborativa de precios de servicios locales
- **Grado:** Diseño, animación y programación de videojuegos y entornos interactivos 2D y 3D
- **Entrega:** 11 de diciembre de 2026
- **Tutor:** *(completar)*

---

## Resumen / Abstract **[Parcial]**

*(Se redacta al final. Borrador inicial:)*

Este trabajo presenta el diseño y el desarrollo de un prototipo de aplicación móvil de mapa y navegación cuya función principal es mostrar el **coste** de los establecimientos y servicios locales antes de acudir a ellos. Los precios se mantienen de forma **colaborativa**: cuando el usuario llega a un destino usando la navegación de la app, se le pregunta con un solo toque si el precio mostrado sigue siendo correcto. Para incentivar esas aportaciones se integra un sistema de **gamificación** (puntos, niveles y monedas canjeables por cosméticos del mapa). El prototipo se valida con usuarios mediante el cuestionario SUS.

**Palabras clave:** gamificación, crowdsourcing, geolocalización, precios, mapas, aplicación móvil.

---

## 1. Introducción **[Parcial]**

### 1.1 Motivación y problema
Antes de ir a un sitio, normalmente no se sabe cuánto va a costar: una sesión de fisioterapia, una pista de pádel o una cena. Los mapas y navegadores habituales indican cómo llegar, horarios y reseñas, pero rara vez el precio, y cuando lo hacen es poco preciso (por ejemplo, un indicador genérico de "€€"). Para conocer el precio hay que llamar, buscar en la web del negocio o simplemente ir y descubrirlo allí.

### 1.2 Objetivos
**Objetivo general:** diseñar y desarrollar un prototipo funcional de aplicación de mapa y navegación que muestre el precio de establecimientos locales y lo mantenga actualizado gracias a las aportaciones de los usuarios.

**Objetivos específicos:**
1. Definir un **modelo de precios unificado** válido para distintos tipos de negocio (ver cap. 4).
2. Integrar la recogida de precios en el momento de **llegada al destino**, con una interacción de un toque.
3. Diseñar **reglas de actualización** que hagan el precio fiable frente a reportes erróneos.
4. Diseñar e implementar un sistema de **gamificación** que incentive las aportaciones.
5. **Validar** la usabilidad del prototipo con usuarios reales (SUS).

### 1.3 Pregunta de investigación
*(Propuesta, pendiente de confirmar con el tutor.)* ¿Puede la gamificación integrada en la navegación conseguir que los usuarios mantengan actualizados los precios de servicios locales?

### 1.4 Estructura del documento
*(Completar al final.)*

---

## 2. Estado del arte **[Pendiente]**

Apartados previstos (hay que documentarlos con fuentes reales y fechas de consulta):

- **2.1 Mapas y navegadores:** Google Maps, Apple Maps, Waze. Qué información de precio ofrecen y cuáles son sus limitaciones.
- **2.2 Plataformas de reseñas y comparadores:** TripAdvisor, comparadores por sector (por ejemplo, de carburantes). Qué cubren y qué no.
- **2.3 Crowdsourcing de información local:** Waze como ejemplo de datos aportados por los usuarios en tiempo real.
- **2.4 Gamificación:** definición y elementos de juego en contextos no lúdicos (referencia de partida: Deterding et al., 2011), y apps que usan puntos, niveles o recompensas para conseguir aportaciones.
- **2.5 Conclusión:** el hueco que cubre este proyecto, el **precio** como dato principal, mantenido por la comunidad e integrado en la navegación.

---

## 3. Metodología y planificación **[Parcial]**

### 3.1 Planificación
| Semana | Fechas | Trabajo |
|---|---|---|
| 1 | 11–17 nov | Tema al tutor (**enviado**), instalación de Unity, prueba técnica del mapa. Decisión sobre el mapa el 16 nov |
| 2 | 18–24 nov | Núcleo: establecimientos, plantillas de categoría, pregunta al llegar, reglas de precio, GPS simulado |
| 3 | 25 nov–1 dic | Gamificación, interfaz, compilación para móvil |
| 4 | 2–8 dic | Pruebas con usuarios (SUS) y cierre de la memoria |
| — | 9–11 dic | Margen y entrega |

*(Al final: comparar lo planificado con lo real y comentar las desviaciones.)*

### 3.2 Herramientas
- Motor / framework: *(pendiente de la decisión sobre el mapa, ver 5.1)*
- Mapa: *(pendiente)*. Requisito: mapa real, 2.5D y con estilo editable
- Asistente de IA: Claude Code, usado para el análisis, la documentación y la programación *(detallar según la política del tutor)*
- Metodología de desarrollo asistido por IA: **BMAD Method** v6.12.4 (agentes de análisis, producto, arquitectura, UX y desarrollo)
- Diseño de interfaz: skill **UI/UX Pro Max** (guías de estilo, paletas, tipografías y accesibilidad)
- Pruebas de interfaz: **Playwright** (automatización del navegador vía MCP)
- Control de versiones: **Git** + repositorio público en **GitHub**: https://github.com/Josepa-12/TFG-App-Precios

### 3.3 Limitaciones del entorno
- Desarrollo en Windows y móvil personal iPhone: desde Windows no se pueden compilar apps de iOS. *(Anotar qué solución se adopta: Android prestado, Mac, versión web o GPS simulado.)*

---

## 4. Diseño **[Parcial]**

### 4.1 Concepto
Una app de mapa y navegación en la que el dato principal de cada establecimiento es **cuánto cuesta**. Las reseñas son secundarias y solo existen en algunas categorías. El poder está en los clientes: el negocio puede declarar un precio inicial, pero si los usuarios reportan otro, prevalece el de los usuarios.

### 4.2 Estudio de tipos de negocio **[Redactado]**
Se analizaron unos 15 sectores (salud, cuidado personal, hostelería, deporte, ocio, educación, reparaciones, mascotas, aparcamiento, alojamiento, comercio, etc.) según **cómo cobran**, y no según a qué se dedican. Todos se pueden describir con cuatro dimensiones:

1. **Número de conceptos** que cobran: uno o varios.
2. **Forma del precio:** fijo, rango o "desde".
3. **Unidad:** persona, sesión, hora, entrada…
4. **Variante:** horario, día, socio…

**Sectores excluidos y por qué:**
- **Comercio con catálogo** (supermercados, gasolineras): tienen cientos de productos y ya existen comparadores.
- **Alojamiento:** el precio depende de la fecha y ya lo cubren otras plataformas.
- **Servicios a presupuesto** (reformas): no hay un precio comparable.

Estudio completo: [estudio-tipos-negocio.md](estudio-tipos-negocio.md) *(candidato a anexo)*.

### 4.3 Modelo de precios unificado **[Redactado]**
> Un establecimiento es una lista de conceptos, y cada concepto tiene un precio.

Las categorías no se programan por separado. Cada una es una **plantilla** que fija los valores por defecto:

| Plantilla | Conceptos | Precio | Unidad | Valoración |
|---|---|---|---|---|
| **Servicios locales** (fisio, psicología, peluquería…) | Normalmente 1 | Fijo | Sesión / servicio | **No** |
| **Hostelería** (restaurantes, bares de copas) | 1: gasto por persona | **Solo rango**, nunca fijo | Persona | **Sí** |
| **Ocio / deporte** | Varios (uno por pista, sala, entrada…) | Fijo | Ver regla abajo | **Sí** |

**Regla de la unidad en ocio:**
- **Alquiler de un espacio** (pistas, salas): precio **total**. Una pista de 12 € se muestra como 12 €, no como 2 € por persona.
- **Actividades por participante** (escape room, karting, bolera…): precio **por persona**.

**Justificación de diseño para defender en la memoria:** añadir un sector nuevo consiste en añadir una plantilla, no en escribir código. El sistema escala aunque el prototipo solo implemente tres plantillas.

### 4.4 Valoración **[Redactado]**
Solo en hostelería y ocio. Son **3 criterios por categoría**, de 1 a 5 estrellas, y la valoración es **opcional** para no romper el flujo de un toque.

| Categoría | Criterios |
|---|---|
| Hostelería | Calidad (comida/bebida) · Servicio · Ambiente |
| Ocio / deporte | Instalaciones · Atención · Calidad-precio |

Los servicios locales no llevan valoración. Además de simplificar la app, se evita pedir opiniones sobre servicios sensibles como la psicología.

### 4.5 Flujo de llegada al destino **[Parcial]**
La app pregunta **solo cuando el usuario ha navegado hasta el sitio con ella**, sin seguimiento en segundo plano.

| Caso | Flujo |
|---|---|
| 1 concepto, precio fijo | "¿Has ido?" → "¿Te costó **X €**?" → Sí / No, fueron… |
| Hostelería (rango) | "¿Has ido?" → "¿Cuánto gastaste por persona?" → tramos (< 10 · 10–20 · 20–30 · 30–50 · > 50) |
| Varios conceptos | "¿Has ido?" → "¿Qué usaste?" → "¿Te costó **X €**?" → Sí / No |
| Hostelería y ocio | + valoración opcional |

Decisión a justificar: en hostelería se usan tramos en lugar de mostrar un precio, para **reducir el sesgo de confirmación** (que el usuario conteste "sí" a lo que se le propone).

**"Más tarde" (decidido 12/11/2026):** la visita queda guardada como **pendiente**. En el mapa, el botón circular amarillo **€** muestra un punto rojo pequeño, discreto a propósito. Al pulsarlo se retoman las preguntas; si hay varias pendientes, primero se elige cuál. El botón **solo funciona si hay alguna pendiente**: sin pendientes aparece apagado. Así no se pueden aportar precios sin una visita verificada. *(Pendiente: decidir si las pendientes caducan pasado un tiempo.)*

**Introducir un precio distinto (decidido 12/11/2026):** sustituye a los botones de +1/−1, que eran lentos con precios altos.
- Una **regla deslizable** de 1 € por marca, que con el dedo permite saltos grandes.
- El número se puede **pulsar para escribirlo** con el teclado numérico.
- Botones con los precios que ya han dicho otros usuarios.

### 4.6 Reglas de cálculo del precio **[Parcial]**
| Forma | Precio mostrado |
|---|---|
| Fijo | Mediana de los reportes recientes |
| Rango | Del percentil 25 al percentil 75 de los reportes (el 50 % central) |
| Desde | Valor bajo habitual (percentil 10–20) |

Además:
- Hace falta un mínimo de reportes coincidentes antes de sustituir el precio declarado por el negocio.
- Solo cuentan los reportes de una ventana de tiempo reciente.
- Se muestra de dónde sale el dato: "declarado por el negocio" o "según N usuarios en los últimos X meses".

*(Pendiente: fijar los valores concretos de N y X y justificarlos con la simulación.)*

### 4.7 Gamificación **[Parcial]**
Decidido: puntos, niveles y monedas canjeables por **cosméticos del mapa/GPS**. *(Pendiente: cuántos puntos da cada acción, curva de niveles, lista de cosméticos y medidas contra las aportaciones falsas para ganar puntos.)*

### 4.8 Mapa **[Parcial]**
Requisitos decididos:
- **Mapa real**, no creado a mano.
- **Vista 2.5D**: cámara inclinada y edificios extruidos.
- **Estilo editable por el autor** sobre el mapa base.

*(Pendiente: tecnología elegida y resultado de la prueba técnica, ver 5.1.)*

### 4.9 Privacidad **[Parcial]**
Principios propuestos:
- Consentimiento explícito.
- Datos mínimos.
- Sin seguimiento en segundo plano.
- Aportaciones no vinculadas a la identidad en categorías sensibles (psicología).

### 4.10 Identidad visual **[Redactado]**
Se generó un sistema de diseño de partida con la skill UI/UX Pro Max. Para una app de consumo y mapas recomendó:
- un estilo vivo y con bloques de color;
- alto contraste;
- tipografía amable.

Se compararon **cuatro paletas** aplicadas a la pantalla del mapa (ver [diseno/paletas.png](diseno/paletas.png)):
- A: la paleta inicial del autor, en azul y gris;
- B: azul y amarillo;
- C: verde local;
- D: navegación nocturna.

| Modo | Paleta | Fondo | Acento | Recompensa | Texto |
|---|---|---|---|---|---|
| Claro | **B · Azul y amarillo** | #F3F5FB | #2F5BEA | #FFC93C | #13235B |
| Oscuro | **D · Navegación nocturna** | #0F1424 | #4C7DFF | #FFD166 | #E6ECFA |

**Por qué se descartó la A:** no tenía un color de recompensa, y las monedas, que son el núcleo de la gamificación, no destacaban.

**Por qué B y D:** comparten el azul como color de acción y el amarillo como recompensa, así que la app se reconoce igual en los dos modos.

**Decisiones de diseño:**
- El **amarillo** se reserva para monedas, logros y el rango de precios de la hostelería.
- El **azul** se usa para las acciones, el resto de precios y la ruta.
- El modo oscuro reduce el deslumbramiento al usar el GPS de noche.

**Tipografía:** Varela Round para títulos y Nunito Sans para el texto. Las dos son redondeadas y cercanas.

**Accesibilidad:**
- Contraste de 4,5:1 como mínimo en el texto. En el modo oscuro, el blanco sobre el acento (3,7:1) solo se usa en texto en negrita y de tamaño grande.
- Zonas táctiles de 44 px como mínimo.
- Iconos SVG en lugar de emojis.
- Se respeta la preferencia `prefers-reduced-motion`.

### 4.11 Prototipo navegable **[Redactado]**
Prototipo web interactivo, situado en **Sanlúcar de Barrameda** y con establecimientos inventados. Código en [prototipo/](../prototipo/) y 24 capturas en [diseno/capturas/](diseno/capturas/). *(La lista de pantallas de abajo describe la primera versión; los cambios de la segunda iteración se detallan al final del apartado.)*

- **Mapa real en 2.5D** con MapLibre GL y teselas vectoriales de OpenFreeMap (OpenStreetMap), con cámara inclinada y edificios extruidos. El estilo se **recolorea por código** con las paletas B y D. Además, se ocultan los negocios reales del mapa base para que solo aparezcan los precios de la app. Esto demuestra el requisito de un mapa real con estilo editable.
- **Pantallas:**
  - bienvenida con el aviso de privacidad;
  - mapa con precios, filtros y buscador;
  - fichas de las tres plantillas;
  - navegación con GPS simulado;
  - preguntas al llegar (con flujos distintos según la plantilla);
  - valoración;
  - recompensa;
  - tienda de cosméticos;
  - perfil con selector de tema.
- **Gamificación del prototipo:**
  - confirmar un precio: +10 monedas y +15 XP;
  - aportar un precio nuevo: +15 monedas y +20 XP;
  - valorar: +5 monedas y +10 XP;
  - cada nivel son 150 XP.
  - Los cosméticos (marcador de posición y estilo de la ruta) **se ven en el mapa** en cuanto se equipan. *(Valores provisionales, pendientes de equilibrar con la simulación.)*
- **Pruebas automáticas con Playwright:** un script recorre todo el flujo en Microsoft Edge y genera las capturas. En las pruebas aparecieron fallos que se corrigieron:
  - la tecla Escape no cerraba el buscador;
  - MapLibre sobrescribía el texto accesible de los marcadores;
  - el sentido del giro era incorrecto al ir hacia el sur;
  - aparecían tramas rayadas del mapa base en el modo oscuro.

**Segunda iteración (12/11/2026), tras la revisión del autor:**
- **Bocetos previos:** antes de rediseñar se compararon tres propuestas de pantalla principal (ver [diseno/bocetos/bocetos-interfaz.png](diseno/bocetos/bocetos-interfaz.png)):
  - A: inspirada en Mapas de Apple;
  - B: inspirada en Waze;
  - C: un híbrido de las dos.

  Se eligió la **A**, con un botón de precios pequeño y circular tomado de Waze.
- **Pantalla del GPS limpia:**
  - se quita la barra inferior de pestañas;
  - abajo queda una **cápsula de cristal** con el buscador y el **avatar**;
  - el avatar muestra el nivel, con la **experiencia como anillo** alrededor;
  - los filtros pasan a un **desplegable** ("Todos ▾") que indica cuántos sitios hay de cada tipo.
- **Buscador que se despliega hacia arriba:**
  - antes de escribir muestra favoritos, búsqueda por tipo y lugares frecuentes o visitados;
  - al escribir funciona como un buscador normal.
- **Gesto de arrastre del buscador**, como en Mapas de Apple:
  - la hoja **sigue al dedo** y **se queda a la altura donde se suelta**;
  - las excepciones son tres:
    - un **lanzamiento rápido** (más de 0,6 px/ms) la abre del todo hacia arriba o la cierra hacia abajo;
    - si se suelta **muy cerca de abajo** (menos del 15 % del recorrido), se pliega;
    - si se suelta **muy cerca de arriba** (más del 88 %), se abre del todo;
  - si el dedo se para antes de soltar, no cuenta como lanzamiento;
  - **se puede buscar a media altura**, sin abrir la hoja del todo;
  - se puede arrastrar desde el asa, desde la fila del buscador y **desde la propia lista**. Con la hoja a media altura, arrastrar la lista mueve la hoja; con la hoja abierta del todo, la lista se desplaza con normalidad y la hoja solo baja si se tira hacia abajo estando la lista arriba del todo;
  - si se abre arrastrando, no aparece el teclado; solo aparece al tocar el campo de texto;
  - técnicamente, en la lista se usan eventos *touch*: así se decide en cada gesto si se mueve la hoja o se deja el desplazamiento nativo de la lista. En el asa se usan eventos *pointer*, que sirven para el dedo y para el ratón. Mientras se arrastra se desactiva la transición CSS y, al soltar, la misma transición completa el movimiento;
  - se ha probado con gestos táctiles simulados (Playwright y el protocolo de Chrome, CDP).
  - **Fallo corregido (13/11/2026):** tras el primer deslizamiento no se podía volver a deslizar sin tocar antes el buscador.
    - *Causa:* con la hoja abierta, el código ignoraba los arrastres que empezaban sobre el campo de texto, que ocupa casi toda la fila. Solo respondía el asa, de 24 px.
    - *Solución:* el arrastre puede empezar en cualquier punto de la barra. Se distingue por el movimiento: más de 6 px es un deslizamiento, y entonces se quita el foco y no sale el teclado; si no hay movimiento, es un toque para escribir.
    - Se añadió una prueba automática que encadena varios arrastres seguidos, con ratón y con dedo, sin tocar el campo entre medias.
- **Coche o a pie:**
  - se elige **en la ficha del sitio**, antes de pulsar "Ir", con el tiempo estimado de cada opción;
  - el **coche es la opción por defecto**;
  - ya no está en los ajustes de navegación.
- **Burbuja de sonido:** es igual que la de precios y está **encima de ella**. Silencia o activa los sonidos de la app (llegada, pregunta y monedas), que se generan con Web Audio, sin archivos. También aparece durante la navegación.
- **La pregunta "¿has ido?" se retrasa:**
  - ya no sale al llegar, sino **1 hora después** (`ASK_DELAY_MS`), para que el usuario tenga tiempo de **recibir el servicio** antes de valorar el precio. En una primera versión se probó con 1 minuto;
  - si al cumplirse la hora hay otra pregunta abierta o una **ruta en curso**, la visita pasa a pendientes (botón €) en lugar de interrumpir. Así no se distrae a quien conduce;
  - *limitación del prototipo:* el temporizador solo funciona con la página abierta. En la app real tendría que ser una **notificación programada**, porque la app puede estar cerrada.
- **La ruta no desaparece al llegar:**
  - se muestra "Has llegado" con el botón **"He finalizado"**;
  - si no se pulsa, la ruta se cierra sola **1 minuto después de llegar** (`ARRIVE_END_MS`).
- **Indicador de posición según el modo** (como en Mapas de Apple y Waze):
  - **en coche**, el círculo se convierte en una **flecha** que apunta hacia donde vas;
  - **a pie**, se mantiene el **círculo con un cono translúcido** que indica hacia dónde mira el móvil;
  - el rumbo se obtiene de la **brújula del dispositivo**:
    - en iOS, con `webkitCompassHeading`, que exige pedir permiso tras un toque del usuario;
    - en Android, con el evento `deviceorientationabsolute`, donde el rumbo es 360° − alpha;
    - en ambos casos se corrige la orientación de la pantalla;
  - si el dispositivo no tiene brújula, como un ordenador, el cono sigue la dirección de la ruta;
  - fuera de la navegación, el cono aparece solo si hay datos de brújula;
  - el marcador gira con `rotationAlignment: 'map'`, así que el rumbo es respecto al norte aunque el mapa esté girado;
  - los cosméticos de la tienda (dorado, neón) se aplican al círculo, al cono y a la flecha;
  - *requisito:* la brújula del navegador solo funciona con **HTTPS**, así que para probarla en el móvil hay que publicar el prototipo (por ejemplo, en GitHub Pages).
- **Barra del buscador fina y simétrica:**
  - cerrada, deja el **mismo hueco (7 px) por encima y por debajo** del campo de búsqueda;
  - el asa va superpuesta en el borde, así que no añade altura;
  - al abrirse, el asa recupera su espacio.
- **Perfil:** se abre desde el avatar y reúne la actividad (pendientes, aportaciones, favoritos, avisos, insignias), la **tienda** y unos **ajustes agrupados por secciones**, tomando Discord como referencia:
  - Apariencia;
  - Navegación y voz (a pie / en coche, que cambia la ETA);
  - Notificaciones;
  - Privacidad y ubicación;
  - Accesibilidad (reducir animaciones);
  - Idioma;
  - Ayuda sobre cómo se calculan los precios.
- **Nueva función, "Avísame si cambia":** desde la ficha de un sitio se activa un aviso para cuando los clientes confirmen un precio distinto.
- **Fallo corregido en la simulación de la ruta:** a veces la animación se paraba sin llegar al destino.
  - *Causa:* la marca de tiempo que recibe `requestAnimationFrame` puede ser anterior al instante en que se guardaba el inicio. El progreso salía negativo y se pedía la posición `-1` de la ruta.
  - *Solución:* el inicio se toma del primer fotograma y el progreso se limita a [0, 1]. Además, "Salir" ahora cancela la simulación.
- 31 capturas regeneradas con Playwright, sin errores en la consola.

### 4.11b Prototipo, tercera iteración: puntos de interés y GPS a cualquier destino **[Redactado]**
*(Tercera iteración, 13/11/2026. Estudio completo en [estudio-puntos-interes.md](estudio-puntos-interes.md).)*

**Logos en lugar de precios en el mapa.**
- Los establecimientos con precios se ven con el **logo de su tipo de negocio**: tijeras en la barbería, cubiertos en el restaurante, cerebro en psicología…
- Llevan una pequeña **insignia "€"** que indica que tienen precios.
- El color sigue la plantilla: azul para servicio local, naranja para hostelería y verde para ocio.
- El precio se ve al abrir la ficha y en las listas.

**Comercios y servicios reales integrados en el mapa.**
- Se muestran los puntos de interés reales de OpenStreetMap: supermercados, farmacias, gasolineras, gimnasios, bancos…
- Van como una capa de símbolos **dibujada dentro del propio mapa**: un círculo de color con el icono y el nombre al lado, como en Mapas de Apple.
- No son marcadores flotantes, así que se distinguen de los establecimientos con precios.
- **Censo de Sanlúcar:** 481 puntos en 98 tipos distintos, agrupados en 22 categorías. Las paradas de autobús (128) y las oficinas se ocultan para no saturar el mapa.
- Los iconos se generan por código (SVG → imagen del mapa), sin archivos de imagen.

**"Encuentra cerca".**
- En el buscador hay una lista de categorías como la de Mapas de Apple: restaurantes, gasolineras, supermercados, parques, aparcamientos, hoteles, comida rápida, farmacias, bares, gimnasios, salud, cultura…
- Al elegir una, el mapa muestra solo esa categoría.
- Debajo aparece la lista ordenada por distancia: primero los establecimientos con precios y luego los del mapa.

**GPS a cualquier destino, no solo a sitios con precios:**
- **Direcciones escritas**, como la casa de un amigo, con el geocodificador **Photon** (komoot, datos de OSM). La búsqueda prioriza los resultados cercanos.
- **Ubicaciones compartidas por WhatsApp:** WhatsApp comparte un enlace de Google Maps con las coordenadas. Se reconocen enlaces de Google Maps (`?q=`, `@lat,lng`, `!3d…!4d…`), de Apple Maps (`ll=`), `geo:` y coordenadas sueltas, pegándolos en el buscador o con el botón "Abrir una ubicación compartida".
  - *Límite:* los enlaces cortos (`maps.app.goo.gl`) solo se pueden resolver desde un servidor o desde la app nativa.
  - En la app real se registraría la app para abrir estos enlaces: "Compartir → PrecioLocal" en iOS y *intent filters* en Android.
  - En el prototipo se simula con la URL `index.html?ll=LAT,LNG&name=…`.
- **Mantener pulsado el mapa** marca un punto cualquiera, con su dirección obtenida por geocodificación inversa.
- **Favoritos sin precio:** las direcciones y los lugares se pueden guardar con el corazón ("Casa de mamá").

**Rutas por calles reales.**
- Se sustituye la ruta en "L" por rutas reales de **OSRM**: el servidor público de demostración para ir en coche y el de FOSSGIS para ir a pie.
- Las indicaciones son giro a giro y en castellano ("Gira a la derecha por Calle Carmen Viejo", "En la rotonda, toma la 2.ª salida"), y el tiempo estimado es real.
- Si no hay conexión, se usa la ruta aproximada anterior.
- La simulación de la llegada recorre la geometría real y su duración depende de la longitud de la ruta.

**Privacidad.**
- Las direcciones y ubicaciones a las que se navega **no se guardan en el historial** ni generan la pregunta del precio, que solo se hace en establecimientos con precios.
- *Advertencia para la app real:* los servicios públicos de búsqueda y rutas reciben el texto buscado y las coordenadas. Una versión en producción debería usar **servidores propios** de Photon y OSRM, o calcular las rutas en el móvil. Los servidores de demostración, además, no permiten un uso intensivo.

**Perfil.** La tienda pasa a estar **encima de "Tu actividad"**, para que se vea antes.

**Cuarta iteración (13/11/2026): pantalla limpia y lugares reales.**
- **Precios ocultos por defecto.** Los marcadores de precio ya no aparecen al abrir la app. El desplegable de arriba a la izquierda pasa a llamarse **"Precios"** y tiene las opciones *Ninguno* (por defecto), *Todos*, *Servicios locales*, *Hostelería* y *Ocio*. El sitio abierto en la ficha, o al que se navega, se ve siempre.
- **121 lugares reales** de Sanlúcar sustituyen a los 7 inventados (ver [estudio-puntos-interes.md](estudio-puntos-interes.md), apartado 5b).
- **Ningún precio inventado.** Todos los lugares empiezan vacíos, por dos vías:
  - **Modo negocio:** el dueño declara su precio inicial y se muestra como "declarado por el negocio". En la app real habría que verificar al dueño, por ejemplo con un código enviado al establecimiento; en el prototipo se simula.
  - **Primer cliente:** quien llega con el GPS aporta el primer precio ("¡Pionero!", recompensa extra). Elige un concepto sugerido o escribe "otra cosa" con su unidad.
- **El precio fluctúa con los clientes.** Cada concepto guarda lo declarado y los aportes de los clientes por separado. Con 3 aportes o más (`MIN_REPORTS`), el precio mostrado pasa a ser la **mediana de lo que pagan los clientes**, o el **rango central** en hostelería, aunque el negocio haya declarado otro. Esto pone en práctica la decisión de que "el poder está en los clientes", y la ficha indica siempre de dónde sale el dato.
- Tocar un negocio real en el mapa abre su ficha de precios. En "Encuentra cerca" no se repiten los que ya están en la app.

### 4.11c Prototipo, quinta iteración: cuentas de cliente y de negocio **[Redactado]**
**Dos tipos de cuenta** (13/11/2026), en `prototipo/cuentas.js`:
- **Cliente:** descubre precios, aporta lo que paga al llegar con el GPS y gana monedas y niveles. El perfil muestra su nombre, su nivel, la tienda y su actividad.
- **Negocio:**
  1. busca su establecimiento entre los lugares del mapa y lo vincula, tras una verificación;
  2. si no aparece, **lo da de alta a mano**: nombre, tipo de negocio (agrupado por plantilla de precio), dirección (búsqueda con Photon o el centro del mapa) y precios;
  3. el negocio aparece en el mapa con el logo de su tipo, y su perfil muestra el estado de sus precios: declarados o confirmados por N clientes.

  Las cuentas de negocio no tienen monedas ni tienda, no reciben la pregunta "¿has ido?" y solo gestionan su propio negocio. Así se evita que un negocio aporte precios de la competencia.

**Inicio de sesión.**
- La opción principal es **"Continuar con Google"**, y la alternativa, el correo. También se puede entrar sin cuenta para mirar.
- En el prototipo es **simulado**. En la app real se usaría Google Identity Services o Firebase Authentication con el proveedor Google: la app recibe el nombre y el correo verificados, y no gestiona contraseñas.
- La sesión del prototipo se guarda solo en el navegador (`localStorage`).

**Verificación del negocio** (pendiente para la app real): un código enviado al teléfono o al correo del establecimiento, o la comprobación del CIF. En el prototipo se simula con un botón.

**Origen del precio en la ficha.** Hay tres casos:
- *publicado en la web del negocio*, con enlace y fecha de consulta;
- *declarado por el negocio*;
- *según N clientes*.

Cuando el negocio revisa y publica sus precios desde su cuenta, dejan de mostrarse como "de su web" y pasan a "declarados por el negocio". En todos los casos, con 3 aportes de clientes manda lo que pagan ellos.

**Clínicas privadas y centros públicos** (ver [estudio-puntos-interes.md](estudio-puntos-interes.md), 5c):
- se añaden Clínica Areas, con sus tarifas oficiales agrupadas por área ("desde X €", "con bono", "Consultar"), y FisioSanlúcar;
- los centros sanitarios públicos se ven en el mapa solo para saber dónde están, sin precios.

**Sexta iteración (16/11/2026): frescura, ofertas, plan de tarde y avisos de precio incorrecto** (`prototipo/extras.js`).
- **Frescura del precio.** Cada concepto guarda cuándo se confirmó por última vez: aporte o confirmación de un cliente, declaración del negocio o fecha de consulta de su web. La ficha pone un punto de color junto a cada precio y un resumen ("Al día · confirmado hace 3 días"). Los umbrales dependen del tipo de negocio, porque la hostelería cambia antes:

  | Plantilla | Al día | Podría haber cambiado | Antiguo |
  |---|---|---|---|
  | Hostelería | < 30 días | 30-60 días | > 60 días |
  | Servicios locales y ocio | < 3 meses | 3-6 meses | > 6 meses |

  Confirmar un precio que "podría haber cambiado" da +5 monedas extra, y uno antiguo, +10. Así la gamificación empuja a mantener vivos los datos, y la frescura media sirve como indicador para la pregunta de investigación.
- **Ofertas del negocio.**
  - La cuenta de negocio publica ofertas con texto, precio opcional y fecha de fin.
  - Se ven en la ficha y con una etiqueta "%" muy pequeña en la esquina inferior del marcador, para no recargar el mapa.
  - Desaparecen al caducar o cuando 3 clientes avisan de que "ya no está".
- **Plan de tarde.**
  - Desde la ficha se añaden paradas. La tarjeta del plan permite ordenarlas, elegir el número de personas y qué se va a usar en cada sitio.
  - Calcula el gasto estimado total: hostelería, rango × personas; entradas y servicios, × personas; pistas y aparcamientos, una vez para el grupo. También indica qué paradas aún no tienen precio.
  - "Empezar ruta" guía parada a parada: al pulsar "He finalizado" se abre la siguiente.
- **"¿Algún precio está mal?"**
  - Cualquier cliente puede avisar de que un precio ha cambiado, de que ya no se ofrece el servicio o de que el negocio ha cerrado.
  - Cada aviso pesa según la **reputación** del usuario, que sube con sus aportaciones: Nueva ×1, Fiable ×1,5 (5 aportes) y Experta ×2 (20 aportes).
  - Con 3 puntos de aviso, el precio queda **"en revisión"**: se marca en la ficha y la pregunta de llegada pide a los siguientes clientes que lo confirmen. Cuando un cliente lo confirma o aporta otro, la revisión se cierra.
  - El negocio ve en su perfil los avisos recibidos y qué precios están en revisión.
- Para las capturas se simula el paso del tiempo (`proto.ageDays`) y los avisos de otros usuarios (`proto.simReports`). No se inventa ningún precio.

### 4.11e Revisión del prototipo: auditoría multiagente y correcciones (16/11/2026) **[Redactado]**
**Método.** Antes de seguir añadiendo funciones se auditó el prototipo con cuatro agentes especializados, que trabajaron en paralelo y probaron el comportamiento real con Playwright, no solo leyendo el código:
- **Funcionalidad:** flujos completos, validaciones, persistencia y casos límite. 20 hallazgos.
- **UI/UX:** jerarquía, consistencia y tamaños de pantalla, incluidos portátiles. 18 hallazgos.
- **Accesibilidad**, con WCAG 2.2 como referencia: teclado, lector de pantalla, contraste y objetivos táctiles. 16 hallazgos.
- **Claridad y contenido:** textos, terminología y acciones ambiguas. 25 hallazgos.

Un agente coordinador unificó los 79 hallazgos en 44 tareas sin duplicados, priorizadas de P0 a P3 y con dependencias. La implementación también se repartió entre agentes, en oleadas, para que nunca hubiera dos editando la misma zona de código:
1. **Oleada 1:** flujo de llegada, cuentas y guardado, plan y avisos, y estilos.
2. **Oleada 2:** ficha, mapa y buscador; perfil, ayuda y textos.
3. **Oleada 3:** foco, teclado y semántica.
4. **Oleada 4:** validación y capturas.

Todo se hizo en la rama `revision`, sin integrarlo en `main` hasta la revisión del autor.

**Problemas más importantes detectados y corregidos:**
- **Sin salida:**
  - a partir del paso 2 del flujo de llegada no había ni "Atrás" ni "Más tarde";
  - en "¿Es tu negocio?" con sesión de cliente no había forma de volver.
- **Integridad del dato:**
  - la regla recortaba a 100 € lo que se escribía, aceptaba 0 € y redondeaba los céntimos (22,50 € contaba como precio distinto);
  - un error de validación en el modo negocio borraba los precios;
  - el tramo "Más de 50 €" inventaba un tope de 80 €.
- **Promesa incumplida:** la app decía "tus monedas y aportaciones quedan guardadas" y no se guardaba nada. Ahora el progreso se guarda por cuenta en el navegador.
- **Interfaz:**
  - los avisos emergentes tapaban y bloqueaban el botón "Ir";
  - el marco del móvil se cortaba en portátiles de 1366×768 (lo que vería el tribunal);
  - en fichas largas "Ir" quedaba 900 px más abajo. Ahora va en una fila bajo la cabecera, como en Mapas de Apple.
- **Accesibilidad (P0):**
  - los diálogos no recibían el foco, no lo retenían y no se cerraban con Escape;
  - además había problemas de contraste en modo oscuro, estados de los controles que el lector de pantalla no anunciaba y campos sin etiqueta.
- **Claridad:**
  - "avisos de precio" significaba dos cosas: vigilar un sitio y denunciar un error;
  - la ayuda decía algo falso sobre cómo cambia el precio;
  - el total del plan de tarde parecía completo aunque faltaran precios.

**Decisiones tomadas en la revisión:**

| Tema | Decisión |
|---|---|
| Guardado | El progreso del cliente se guarda en el navegador |
| Botón "Ir" | Fila de acciones bajo la cabecera de la ficha |
| Asa de la ficha | Se elimina, porque no se podía arrastrar |
| Marcadores | Siguen mostrando el logo del tipo de negocio; los que no tienen precio se ven más discretos |
| Plan de tarde | Empieza con 1 persona |
| Alta de negocio | Usa la ubicación actual |
| Filtro de precios | Con "Ninguno", la píldora dice "Ver precios" |
| Color de cada sitio | El mismo que su marcador (por plantilla) en todas las pantallas |
| Tramo de hostelería | "Más de 50 €" queda abierto |
| Aportaciones anónimas | Clínicas, fisioterapia y dentistas |
| Cuentas de negocio | No aportan precios como cliente |
| Indicaciones por voz | Se implementan con la síntesis de voz del navegador |

**Validación.** Se repitieron las pruebas de los cuatro auditores y el recorrido completo de capturas en 360×740, 390×844, 1366×768 y 844×390, en claro y oscuro. El resultado se detalla en el capítulo 6.

**Valor para el TFG:** la auditoría documenta la calidad del prototipo antes de las pruebas con usuarios (SUS) y sirve como evidencia del proceso iterativo de diseño.

**Rutas y marcadores (16/11/2026).**
- **Ruta más corta y sin ir en contra dirección.** OSRM calcula la ruta más rápida y su servidor público no permite pedir la más corta. Por eso las rutas se piden primero a **Valhalla** (servidor público de FOSSGIS) con la opción `shortest`, que minimiza la distancia y respeta los sentidos de circulación de OpenStreetMap. A pie se puede ir por cualquier acera. Si Valhalla no responde, se usa OSRM y se elige la más corta de sus alternativas.
- La ruta empieza y acaba en el punto de la calle más cercano. Antes se unía con líneas rectas a la posición exacta del usuario y del destino, y esos tramos cruzaban manzanas.
- Valhalla devuelve las maniobras con números de tipo. Se traducen al mismo formato de pasos que OSRM, así que las indicaciones en castellano y los iconos de giro no cambian.
- **Error corregido en los marcadores de precio.** Salían en posiciones erróneas y parecían seguir a la cámara: unos 5.100 px de desvío medido con Playwright. La causa era que el estilo propio del marcador (`position: relative`, una transición y una escala en `transform`) pisaba el `position: absolute` y el `transform` con los que MapLibre lo coloca. Ahora el marcador visible va dentro de un contenedor que solo toca MapLibre, y el desvío es menor de 1 px al mover, girar e inclinar el mapa. Lo mismo se corrigió en el indicador de posición del usuario.

### 4.12 Alcance del prototipo **[Parcial]**
**Dentro:**
- Las tres plantillas, con un establecimiento de ejemplo de cada una, incluido un polideportivo con varias pistas.
- Pregunta al llegar.
- Reglas de precio.
- Valoración.
- Gamificación básica.

**Fuera**, como trabajo futuro (cap. 8):
- Avisos tipo Waze.
- Android Auto / CarPlay.
- Motor de rutas propio.
- Moderación avanzada.
- Panel completo para negocios.
- Sectores excluidos en 4.2.

---

## 5. Desarrollo **[Pendiente]**

- **5.1 Elección de tecnología y prueba técnica del mapa:** opciones valoradas (Unity + Mapbox SDK frente a una app fuera de Unity con Mapbox GL o MapLibre), criterios y resultado. *Primer dato:* el prototipo web (4.11) demuestra que con MapLibre + OpenFreeMap se consigue un mapa real en 2.5D, con estilo propio, sin cuenta y sin coste.
  - *Segundo dato (16/11/2026):* se crea el proyecto de Unity `unity/PrecioLocal` (Unity 6.3 LTS, render integrado) con una escena "Mapa" generada por código:
    - teselas reales de OpenStreetMap a escala 1 unidad = 1 m, proyección Web Mercator (`Geo.cs`), con caché en disco y atribución;
    - cámara 2.5D con ratón, teclado y táctil;
    - marcadores colocados por latitud y longitud (tu posición y 4 sitios reales).

    No se ha usado el Mapbox SDK, sin desarrollo activo y con cuenta obligatoria: se carga el mapa directamente.

    *Resultado (16/11/2026):* al pulsar Play el mapa real de Sanlúcar se descarga y se muestra correctamente en el editor, con los marcadores en su sitio. Queda demostrado que Unity puede mostrar un mapa real sin SDK de terceros.

    *Estilo propio, opción A (16/11/2026):* para el estilo se valoraron cuatro caminos:
    - **A.** Teselas de imagen recoloreadas.
    - **B.** Cesium for Unity con edificios de OSM.
    - **C.** Teselas vectoriales con código propio.
    - **D.** Versión web.

    Se empieza por la A. Primero se probaron los mapas base de CARTO, de estilo claro, pero ahora exigen clave de API. Por eso se mantienen las teselas de OpenStreetMap y se recolorean con un shader propio (`TeselaPaleta.shader`). Ese shader traduce cada color del estilo OSM al color equivalente de la paleta del prototipo web, por luminosidad y por tono: suelo, edificios, agua, zonas verdes, calles y texto. Admite tema claro, oscuro y original.

    Ventajas: sin proveedor nuevo, sin clave y sin coste. Límites:
    - el mapa sigue plano;
    - no se puede cambiar el grosor de las calles ni qué etiquetas aparecen;
    - las etiquetas siguen siendo las de OSM.

    *Opción B, Cesium for Unity (16/11/2026):* se elige la B para tener la ciudad en 3D.
    - Se instala Cesium for Unity 1.26.1. Es gratuito y de código abierto, compatible con Unity 6, con el render integrado y con Android e iOS.
    - Necesita una cuenta gratuita de Cesium ion.
    - Se genera una segunda escena, `MapaCesium`, con:
      - el origen geográfico en la Plaza del Cabildo;
      - el relieve de Cesium World Terrain con las teselas de OpenStreetMap encima;
      - los edificios en 3D de Cesium OSM Buildings;
      - los mismos marcadores, colocados con `CesiumGlobeAnchor`.
    - Se mantiene la cámara 2.5D propia y la escena anterior, para comparar.
    - Cesium añade el paquete Input System. Por eso el proyecto pasa a usar los dos sistemas de entrada ("Both") y la cámara sigue funcionando.
    - El token de Cesium ion queda fuera del repositorio (`.gitignore`).
    - **Paleta en 3D.** Con los materiales por defecto de Cesium, los edificios blancos sobre el mapa de OSM no se distinguían. Se crean dos materiales propios:
      - **Terreno:** recolorea con la paleta las teselas OSM que Cesium coloca encima, usando la misma función que la opción A, ahora compartida en `PaletaOSM.cginc`.
      - **Edificios:** tejado con el color de edificio de la paleta y fachadas más oscuras según la luz.
      - Los colores son globales y los aplica `TemaCesium.cs`, que permite cambiar entre tema claro, oscuro y original con la tecla T.
    - **Problema encontrado (5.8):** el suelo salía entero del color del agua.
      - *Diagnóstico:* se guardó la imagen que Cesium aplicaba al terreno y era mar liso.
      - *Causa:* en las plantillas de URL de Cesium, `{y}` cuenta las filas desde el sur, mientras que el esquema XYZ de OpenStreetMap las cuenta desde el norte. Se pedían las teselas de la latitud opuesta, en el Atlántico Sur.
      - *Solución:* usar `{reverseY}`.
    - *Resultado:* Sanlúcar en 3D con la paleta del proyecto, con calles y sus nombres, edificios con volumen y marcadores en su sitio.
    - **Mapa más limpio y sombras (16/11/2026):**
      - *Iconos:* el shader del terreno quita los iconos de puntos de interés de OSM y sus nombres. En el estilo OSM son de colores vivos y oscuros, así que se detectan por saturación y luminosidad y se rellenan con la media de los píxeles de alrededor. Se pueden mostrar u ocultar con la tecla I.
        - *Límite:* los iconos negros, como iglesias y mezquitas, no se pueden separar del texto de las calles por color y se mantienen.
      - *Sombras:*
        - Los edificios proyectan sombra sobre el suelo, con un tono azulado de la paleta y un sol de tarde desde el suroeste.
        - La distancia de sombra sigue al zoom de la cámara: la del proyecto, 70 m, no cubría la vista del mapa, que se mira desde unos 900 m.
        - Los edificios no reciben sombras. A esa distancia la precisión no da y aparecían rayas por "acné de sombra".

    *Opción C, teselas vectoriales de OpenFreeMap en Unity (16/11/2026):* se prueba a dibujar el mapa en Unity con los mismos datos que el prototipo web.
    - **Datos:** teselas vectoriales de OpenFreeMap, formato Mapbox Vector Tile y esquema OpenMapTiles, a zoom 14 (unos 2 km por tesela).
    - **Código propio, sin librerías externas:**
      - lector del formato, que es protobuf (`LectorMVT.cs`);
      - triangulador por recorte de orejas con soporte de patios interiores (`Triangulador.cs`);
      - constructor del mapa (`MapaVectorial.cs`).
    - **Qué se dibuja:**
      - calles como cintas con borde, con anchura según el tipo de vía;
      - agua y zonas verdes;
      - edificios extruidos con su altura (`render_height`) y con sombra;
      - nombres de calle tumbados sobre el tramo recto más largo, a 7 m de alto. Se probó a darles tamaño constante en pantalla, levantarlos hacia la cámara y añadirles negrita con halo, pero se descartó y se volvió a esta versión.
    - **Paleta:** las capas usan los colores exactos de la paleta, sin recolorear imágenes, y no aparece ningún icono que no se quiera.
    - **Ventajas frente a Cesium:**
      - sin cuenta, sin clave y sin logo obligatorio: basta la atribución en texto;
      - mismos datos que el prototipo web;
      - caché en disco para usarlo sin conexión;
      - cada tesela se construye en milisegundos.
    - **Límites de esta prueba:**
      - el suelo es plano, sin relieve;
      - no hay más detalle que el del zoom 14;
      - los nombres siguen un solo tramo recto y no la curva de la calle.

    Pendiente para completar la comparación:
    - estilo propio, que en Unity exige teselas vectoriales;
    - edificios extruidos;
    - GPS real en el móvil.
- **5.2 Arquitectura:** estructura del proyecto y módulos.
  - *Historia 1.1 (17/11/2026): nueva estructura del proyecto.* Tras la planificación con BMAD (sprint con 9 épicas y 53 historias en `_bmad-output/implementation-artifacts/sprint-status.yaml`), la primera historia convierte la prueba técnica en la base de la app:
    - **Nombre:** la app pasa a llamarse **PriceMap**. El proyecto queda en `unity/PriceMap` y el identificador Android es `com.pricemap.app`.
    - **Mapa:** se queda solo la opción C, el mapa vectorial con datos de OpenStreetMap servidos por OpenFreeMap. Se retiran Cesium y las escenas de prueba A (teselas de imagen) y B (Cesium). El shader de los edificios, que llevaba "Cesium" en el nombre, pasa a llamarse `EdificiosPaleta`.
    - **Capas:** el código se divide con `.asmdef` en Dominio (C# puro, sin Unity), Aplicación (servicios y puertos), Adaptadores, Vectorial (el mapa), UI y Arranque. Una prueba automática lee los `.asmdef` y falla si alguna capa usa otra que no le corresponde.
    - **Composición:** un único componente `Arranque` en la escena `Principal` conecta las piezas y entrega el mapa a la UI a través del puerto `IMapa`, sin singletons.
    - **Android:** la APK se compila por línea de comandos (`ConfigurarAndroid.CompilarApk`): minSdk 26, targetSdk 35, IL2CPP ARM64 y solo vertical, con un tamaño de unos 17 MB.
    - **Pruebas:** 9 pruebas EditMode (capas, coordenadas y arranque) que pasan en el Test Runner por lotes.
  - *Historia 1.2, parte 1 (17/11/2026): base de interfaz y tema.* La historia se dividió en dos por tamaño. La primera parte añade sobre el mapa la primera capa de interfaz con UI Toolkit:
    - **Tokens:** los de DESIGN.md (paletas B y D, espaciados, radios y tipografías) están en `Tokens.uss`. Una prueba automática calcula el contraste WCAG de los textos y los bordes en los dos temas, también sobre el cristal de los botones encima del mapa.
    - **Fuentes:** Varela Round y Nunito Sans, con licencia OFL. Las variantes 400, 600 y 800 de Nunito Sans se generan con fonttools a partir de la fuente variable. Los tamaños se multiplican por la escala de fuente del sistema (sp).
    - **Tema:** un `ServicioTema` en la capa de aplicación, que observan a la vez el mapa y la interfaz. El primer arranque sigue el tema del sistema (modo oscuro de Android leído por JNI) y después se recuerda la elección.
    - **Textos:** pasan por el paquete Localization (tabla `Textos`, `es-ES`), que viaja en la APK como contenido de Addressables.
    - **Accesibilidad:** cada control se declara en la jerarquía de accesibilidad de Unity 6 para TalkBack. Con el lector activo aparecen los botones de acercar y alejar.
    - **Comprobación visual sin móvil:** un ejecutable de Windows lanzado con `-captura` guarda una captura en cada tema y registra la posición de cada elemento. Así se encontró que, sin el tema por defecto de Unity, la raíz de la interfaz medía 0 de alto.
  - *Historia 1.2, parte 2 (17/11/2026): gestos y teselas alrededor de la vista.*
    - **Inclinar:** el mapa se inclina arrastrando dos dedos en vertical, como en Google Maps (subir = más 3D). Una función pura decide en cada fotograma si el gesto es inclinar o ampliar y girar, según cuánto cambian la separación y el ángulo entre los dedos.
    - **Carga de teselas:** se cargan las 3×3 de alrededor del centro de la vista (5×5 con la cámara alejada) y se liberan, mallas incluidas, las que quedan a más de 3 teselas. Así nunca hay más de 7×7 en memoria.
    - **Sin conexión:** las teselas en caché se dibujan aunque no se pueda pedir la dirección de las teselas. Una tesela que falla se vuelve a pedir cuando cambia la vista.
    - **Comprobación:** en el ejecutable de Windows, un recorrido de 5,5 km hacia el norte (desembocadura del Guadalquivir) deja 15 teselas cargadas.
- **5.4 Lógica de precios:**
  - *Historia 1.3 (17/11/2026): precio, origen, frescura y variantes en el Dominio.* Reglas en C# puro, sin Unity, que comparten todas las pantallas y que nunca se guardan (AD-3):
    - **Precio mostrado:** fijo, rango por persona (con máximo abierto en "Más de 50 €"), "desde" o "A consultar", siempre en céntimos.
    - **Origen:** manda la declaración del negocio más reciente; si no hay, la web oficial, con su página y la fecha de consulta; si tampoco, "sin precio". Ningún precio sale sin origen, y los importes iguales o menores que 0 se descartan.
    - **Frescura:** se mide desde la última confirmación. En hostelería, al día por debajo de 30 días y antiguo por encima de 60. En servicios y ocio, por debajo de 3 meses y por encima de 6 (meses de calendario). Los límites cuentan como "podría haber cambiado". Los umbrales están solo en `ParametrosNegocio`.
    - **Variantes:** cada una tiene su precio y su frescura. Las de horario (también las que cruzan la medianoche) y las de día pueden estar vigentes; las de tipo de cliente nunca, porque no dependen del momento.
    - **Pruebas:** cada regla tiene sus pruebas EditMode, con los límites exactos de la frescura.
  - *Historia 1.4, parte 1 (17/11/2026): datos base de los lugares.*
    - **Exportación:** un script de Node (`prototipo/herramientas/exportar-datos-unity.js`) pasa los lugares del prototipo a un JSON que va dentro de la app (`Assets/Datos/Resources/lugares.json`). Son los 121 lugares de OpenStreetMap más Clínica Areas, FisioSanlúcar y ChemaBarber. Cada lugar tiene un id estable, nombre, tipo, plantilla y coordenadas. Si encuentra ids repetidos, plantillas desconocidas o coordenadas fuera de Sanlúcar, el script no exporta nada.
    - **Solo precios de la web oficial:** de Clínica Areas quedan 11 conceptos en 4 áreas, con su página y fecha de consulta y los importes en céntimos: 8 con importe y 3 "A consultar". Los tres precios que la web solo da con bono se quitan; si el negocio los quiere, los añade él.
    - **Resumen para el mapa:** el Dominio calcula al leer si un lugar tiene precio y cuál anuncia: el importe más bajo, con la forma "desde" (Clínica Areas: desde 20 €). Un centro público nunca tiene precio.
    - **Carga:** la app lee el JSON al arrancar. Si el fichero falta o está dañado, el mapa sale sin lugares y la app no se cae.
    - **Pruebas:** 113 pruebas EditMode. Comprueban el JSON real (124 lugares, cada importe igual al de la web) y los casos de fichero dañado.
  - *Historia 1.4, parte 2 (17/11/2026): marcadores de los lugares en el mapa.*
    - **Cómo se dibujan:** son una capa de la interfaz (UI Toolkit) encima del mapa 3D, no objetos 3D. Cada fotograma el mapa dice dónde cae en la pantalla cada coordenada (`IMapa.Proyectar`) y el marcador se mueve ahí. Así conserva su tamaño al hacer zoom, usa los estilos del proyecto y TalkBack puede leerlo.
    - **Sin retraso:** la cámara se mueve en `Update`, antes que nada, y los marcadores se colocan en `LateUpdate`. En la build de PC la punta queda a 0,5 px de su coordenada (el límite es 1 px).
    - **Aspecto, como el prototipo:** cuadrado redondeado con punta, cuya punta marca el sitio exacto. Lleva el color de su plantilla, el logo vectorial de su tipo de negocio (14 glifos del prototipo dibujados con un pequeño intérprete de trazos SVG) e insignia € si tiene precio. Sin precio, es más pequeño y translúcido.
    - **Colores y contraste:** los colores del marcador no cambian con el tema, para que el logo blanco siempre contraste. El naranja de hostelería se oscureció a `#D6740F` porque el original daba 2,997:1, por debajo del 3:1 que piden los iconos.
    - **TalkBack:** lee "Clínica Areas, clínica privada, desde 20 €" o "Bar Cachón, bar, aún no tiene precio". Primero recorre los botones y después los marcadores visibles, de arriba abajo. Los que quedan fuera de la pantalla no tienen nodo.
    - **Pruebas (1.4):** 141 pruebas EditMode. Cubren colores, logos, insignia, ocultación fuera de vista, orden de pintado, formato de euros en castellano ("1234 €", "22,50 €"), textos de TalkBack, contraste y la proyección del mapa real.
  - *Historia 1.5, parte 1 (17/11/2026): filtro "Precios".*
    - **Qué hace:** una píldora arriba a la izquierda abre un menú con Ninguno, Todos, Servicios locales, Hostelería y Ocio. El mapa muestra solo los lugares de esa plantilla. La primera vez es Ninguno: la píldora dice "Ver precios" y el mapa sale limpio (FR-2). La elección se guarda en el móvil.
    - **Cómo está hecho:** la regla (qué plantillas entran en cada opción) está en el Dominio y el estado en un `ServicioFiltro`, igual que el tema. La capa de marcadores combina el filtro con "está en la pantalla" en cada fotograma.
    - **Cerrar el menú:** un velo invisible a pantalla completa queda por encima del mapa y de los demás botones. Cualquier toque fuera solo cierra el menú: no mueve el mapa ni pulsa otro botón.
    - **Decisiones:** el menú no lleva el contador de sitios del prototipo, para que quede más limpio. La historia se partió en dos; TalkBack dentro del menú y el botón atrás van en la parte 2.
    - **Pruebas:** 170 pruebas EditMode. Cubren la regla, el servicio, cada caso (primer arranque, Todos, una plantilla, que se recuerde, valor dañado, cerrar sin elegir, marca de la opción) y la composición real al arrancar.
  - *Historia 1.5, parte 2 (17/11/2026): menú "Precios" con TalkBack y botón atrás.*
    - **Menú modal también para el lector:** con el menú abierto, TalkBack solo recorre la píldora y las 5 opciones. Los botones y los marcadores salen de la jerarquía mientras está abierto, igual que el velo los tapa a la vista. La opción elegida se marca como seleccionada (`AccessibilityState.Selected`).
    - **Foco y anuncio:** al abrir el menú, el foco va a la opción elegida; al cerrarlo, vuelve a la píldora. Al elegir se anuncia "Precios en el mapa: Hostelería", después de mover el foco, para que TalkBack no lo corte. El foco del teclado solo se mueve con el lector activo o al navegar, así que con el dedo no aparece el borde de foco.
    - **Botón atrás de Android:** cierra el menú sin cambiar el filtro. Con el menú cerrado no hace nada.
    - **Pruebas:** 179 pruebas EditMode. Para probar la jerarquía real de TalkBack sin un lector se añadió un gancho interno que lo simula. Queda comprobar en un móvil real el botón atrás y el orden de anuncio y foco.
  - *Historia 1.6, parte 1 (17/11/2026): puntos de interés de OpenStreetMap.*
    - **De dónde salen:** de la capa `poi` de las mismas teselas de OpenFreeMap que ya dibujan el mapa. No hace falta otra descarga ni otro servicio. Cada tesela guarda sus puntos (con su id de OpenStreetMap) y el mapa los ofrece por el puerto `IMapa`.
    - **Qué se dibuja:** una regla del Dominio, portada del prototipo, da a cada punto una de 22 categorías (supermercado, farmacia, gasolinera, banco…) y descarta las paradas de autobús y las oficinas. Los centros de salud, hospitales y ambulatorios públicos quedan marcados como sanidad pública; se excluyen los veterinarios y se comparan palabras completas.
    - **Cómo se ve:** un círculo de color con su icono vectorial y el nombre al lado, por debajo de los marcadores de lugar (que son cuadrados con punta). Aparecen con la cámara a 1,2 km o menos. El color de cada nombre cumple el contraste de 4,5:1 en los dos temas.
    - **Comprobación:** en la build de PC, a 900 m sobre el centro se ven 67 puntos de 16 categorías. Pasan 214 pruebas EditMode, incluida la ruta real de bytes de la tesela a coordenada (con menos de 3 m de error).
    - **Pendiente (parte 2):** ocultar el punto que repite un lugar de la app, evitar que los nombres se pisen y que TalkBack los lea.
  - *Historia 1.6, parte 2 (17/11/2026): puntos sin repetidos ni solapes.*
    - **Repetidos:** una regla del Dominio, portada de `applyPoiDedup` del prototipo, oculta el punto que repite un lugar de la app cuyo marcador se está viendo. Se considera repetido si tiene el mismo nombre (sin mayúsculas, tildes ni signos) y está a menos de 80 m (distancia haversine). Si el filtro "Precios" oculta el lugar, el punto vuelve a verse; otra sucursal más lejos se ve siempre.
    - **Solapes:** en cada fotograma, los puntos que caben en pantalla se ordenan por importancia (`rank` de OpenStreetMap y, si empatan, el id). Un punto cuya caja (icono más nombre) pisa a otro ya mostrado se oculta entero. Los marcadores de lugar no participan y siempre se ven.
    - **Rendimiento:** los nombres normalizados se calculan una sola vez, para no generar basura en memoria en cada fotograma en el móvil.
    - **Comprobación:** en la build de PC se ven 46 puntos y se ocultan 29 por repetidos y 25 por solapes, con 0 pares solapados. Pasan 237 pruebas EditMode.
    - **Pendiente (parte 3):** que TalkBack lea los puntos visibles después de los lugares.
  - *Historia 1.6, parte 3 (18/11/2026): TalkBack lee los puntos de interés.*
    - **Qué dice:** cada punto visible es un texto para el lector, del tipo "Mercadona, supermercado". Si no tiene nombre, solo la categoría. Si es un centro sanitario público, "…, centro sanitario público". Las 22 categorías salen del prototipo y se traducen con Localization.
    - **Orden:** primero los botones, luego los lugares de la app y al final los puntos, cada grupo de arriba abajo. Los puntos ocultos (lejos, repetidos o pisados) no se leen. Con el menú "Precios" abierto, tampoco.
    - **Cuidado con el lector:** al mover el mapa, el orden de los puntos cambia casi en cada fotograma. Para que el foco de TalkBack no salte sin parar, la lista solo se rehace al momento si cambia qué puntos se ven; si solo cambia el orden, como mucho una vez por segundo. Al descargar una zona se avisa al lector una sola vez. La zona táctil de cada punto mide 48 dp, el mínimo de accesibilidad, aunque el círculo mida 24.
    - **Comprobación:** pasan 255 pruebas EditMode. En la captura de PC, TalkBack lee 53 entradas: los botones, 23 lugares y 28 puntos, en ese orden.
    - **Incidencia:** el ejecutable de pruebas se detiene si pierde el foco, porque el proyecto no permite seguir en segundo plano (`runInBackground` desactivado). Las capturas deben lanzarse sin tocar otras ventanas.
  - *Historia 1.7, parte 1 (18/11/2026): ver mi posición en el mapa (pendiente de revisión).*
    - **Arquitectura:** un puerto nuevo, `IUbicacion` (estado `SinPermiso`, `Denegado`, `Buscando` o `Activa`, la coordenada, el rumbo o "sin brújula" y un aviso de cambios), y su servicio, `ServicioUbicacion`. El adaptador `UbicacionUnity` vive en un ensamblado nuevo, `PriceMap.Adaptadores.Gps`, y es el único que toca `Input.location`, `Input.compass` y los permisos de Android. Dominio y Aplicación no los ven; la prueba de capas lo vigila.
    - **Permiso:** la primera vez que se abre la app sale una ventana modal con la explicación y los botones "Permitir" y "Ahora no". Que ya salió se guarda en las preferencias. Con "Ahora no", tocar el botón de ubicación la vuelve a mostrar. Desde Android 12 hay que pedir la ubicación precisa y la aproximada juntas, así que al generar el proyecto de Android se añaden las dos al manifiesto y se le dice a Unity que no pida permisos por su cuenta al arrancar.
    - **GPS solo con la app delante (NFR-3):** al pasar a segundo plano o perder el foco se apagan el GPS y la brújula, y se encienden al volver. Como el diálogo del permiso también quita el foco a la app, el GPS se enciende al volver de él y no antes.
    - **Cómo se ve:** un círculo azul (#2F5BEA) con borde blanco, por encima de los lugares y de los puntos. Con brújula, lleva un cono translúcido hacia donde mira el móvil, respecto al norte real; si el mapa está girado, el cono se gira con él (rumbo menos giro del mapa). El botón nuevo, con un icono de diana, va debajo del de tema y centra el mapa en el usuario. Sin posición no hace nada. Se retira el marcador 3D fijo "Tu posición (simulada)" y sus materiales.
    - **Comprobación:** 25 pruebas EditMode nuevas, todas con una ubicación falsa: una por cada fila de la matriz de la spec, el camino real de cada fotograma, el papel de cada nodo de TalkBack en la explicación, qué hace el Arranque con cada aviso de pausa o de foco, y el manifiesto de Android (los dos permisos una sola vez, aunque se genere dos veces). La captura de PC usa una ubicación fija en la Plaza del Cabildo con el móvil mirando al este: el botón sale en la pila derecha, debajo del de tema, y el cono aparece a 105° (90° menos el giro inicial del mapa, −15°). El log no tiene errores. La suite completa pasa (280 de 280). La revisión con tres revisores dio 25 hallazgos: se corrigieron 5 (una respuesta del permiso que podía perderse y cuatro pruebas que faltaban), se aplazaron 2 (avisar cuando la ubicación del móvil está apagada, en la parte 2, y probar el adaptador real del GPS) y el resto se descartó tras comprobarlo.
    - **Pendiente:** probarlo en un Android real (explicación, permiso, círculo con cono, centrado y que no salga el icono del GPS en segundo plano). La parte 2 añade orientar al norte con el segundo toque, lo que lee TalkBack según el estado y el aviso de denegado con "Abrir Ajustes".
  - *Historia 1.7, parte 2 (18/11/2026): orientar al norte, estados del botón y aviso de ubicación desactivada.*
    - **Botón con estados:** lo que hace el botón lo decide `ServicioUbicacion.AlTocarBoton` (lógica pura) con la distancia en pantalla entre el usuario y el centro de la vista y el giro del mapa. Lejos del usuario, centra. Ya centrado (a menos de 8 dp) y con el mapa girado, lo orienta al norte (giro 0, sin tocar zoom ni inclinación). Centrado y al norte, vuelve a centrar sin efecto visible: no hay modo brújula. Si el usuario arrastra el mapa, el siguiente toque vuelve a centrar.
    - **TalkBack:** la etiqueta del botón dice lo que hará: "Centrar en mi posición", "Orientar al norte", "Buscando tu posición" o "Ubicación desactivada". Se recalcula en cada fotograma y, si cambia, se rehace la jerarquía del lector.
    - **Ubicación apagada:** un estado nuevo, `Apagada`, para cuando hay permiso pero la ubicación del móvil está apagada o el servicio falla. Antes se quedaba en "Buscando" para siempre (hallazgo de la revisión de la parte 1). Al volver a la app con la ubicación encendida pasa a "Buscando" y luego a "Activa".
    - **Aviso:** con el permiso denegado o la ubicación apagada, tocar el botón abre una ventana modal (la misma de la explicación, reutilizada) con el texto de cada caso, "Abrir Ajustes" y "Cerrar". "Abrir Ajustes" va por el puerto `IUbicacion` (nunca desde la UI): con el permiso denegado abre los ajustes de la app, y con la ubicación apagada, los de ubicación. El botón atrás la cierra y el foco vuelve al botón.
    - **Adaptador probado:** la lógica del GPS pasa a `UbicacionGps`, que trabaja sobre una interfaz pequeña (`IFuenteUbicacion`: permiso, interruptor de ubicación, servicio, lecturas, brújula y ajustes). La fuente real (`FuenteUbicacionUnity`) envuelve `Input.location`, `Input.compass`, los permisos y los Intents de Android, y `UbicacionUnity` queda como un componente fino. Así las transiciones se prueban sin GPS real.
    - **Comprobación:** pasan 303 pruebas EditMode (23 nuevas: cada fila de la matriz, las etiquetas de TalkBack, el aviso en los dos casos y el adaptador con una fuente falsa, incluidos el permiso retirado, la ubicación apagada, el servicio `Failed`, el orden de las respuestas del permiso y el umbral de 1° del rumbo). En la captura de PC "posicion", con la ubicación simulada en la Plaza del Cabildo y rumbo 45°, el mapa se aleja, el botón centra (círculo a 0 dp del centro, cono a 60°) y el segundo toque deja el giro en 0°. El log no tiene errores y la APK compila.
    - **Revisión:** tres revisores dieron 21 hallazgos. Se corrigieron 5: la etiqueta del botón ahora se cambia en el propio nodo de TalkBack (rehacer la jerarquía hacía perder el foco tras tocar el botón), el aviso abierto se cierra o cambia de texto si cambia el estado, y tres pruebas que faltaban (centro y norte del mapa real, recuperación tras conceder el permiso en Ajustes y los bordes del giro). El resto se descartó tras comprobarlo. La suite pasa (308 de 308) y la captura no tiene errores.
    - **Pendiente:** probar en un Android real el segundo toque, que "Abrir Ajustes" lleve a la pantalla correcta y el estado "apagada" con la ubicación del móvil desactivada.
  - *Historia 1.8, parte 1 (18/11/2026): ficha del lugar con sus precios (commit `580abd3`).*
    - **Tocar un marcador:** los marcadores siguen sin capturar toques, para poder arrastrar el mapa empezando encima de uno. La cámara distingue un toque de un arrastre (`DetectorToque`: menos de 6 dp de movimiento entre bajar y soltar el dedo, contando lo más lejos que llegó; un segundo dedo lo anula) y lo avisa por el puerto `IMapa` (evento `Toque`). La pantalla abre la ficha del marcador visible más cercano si está a menos de 24 dp de lo dibujado (cuerpo y punta). Los puntos de interés no abren nada (su ficha es de la épica 2). Con el menú "Precios" abierto, el toque solo lo cierra.
    - **Ficha:** hoja opaca que sube desde abajo, con esquinas superiores de 24 dp, como mucho el 72 % del alto y desplazamiento interno. Cabecera con el nombre, el tipo con una muestra del color de su plantilla ("Clínica privada · Servicio local") y la dirección; botón de cerrar de 48 dp; la fila de acciones reservada bajo la cabecera, vacía; y los conceptos agrupados por área en el orden de los datos. Cada concepto muestra su precio ("90 €", "desde 40 €", "10–20 € por persona" en hostelería o "Consultar"), su unidad ("/sesión"), el origen ("Publicado en su web · 13/11/2026") y el punto de frescura con su color y su texto. Todo lo calcula el Dominio de la 1.3 con el "ahora" del Arranque y nada se guarda. Sin precios: "Aún no tiene precio".
    - **Cerrar:** botón atrás, la X o un toque en el mapa fuera de la hoja. Con la ficha abierta, ese toque no mueve el mapa.
    - **Comprobación:** pasan 329 pruebas EditMode (20 de la ficha, una o más por fila de la matriz; una del camino del toque desde la cámara real hasta el puerto; y una que ata la zona del toque de los marcadores a sus medidas en los USS). En la captura de PC "ficha", un toque simulado en la punta del marcador de Clínica Areas (llamando directamente a la pantalla, sin pasar por la cámara) abre su ficha con sus cuatro áreas, "desde 40 €", "Consultar", el origen y "Al día"; el botón atrás la cierra. El log no tiene errores.
    - **Pendiente:** probar en un Android real el toque y el desplazamiento de la ficha. La parte 2 se partió en dos a petición del usuario: variantes y centrado (parte 2) y TalkBack de la ficha (parte 3).
  - *Historia 1.8, parte 2 (18/11/2026): variantes y centrado de la ficha (commit `6bd287f`).*
    - **Variantes (FR-60):** debajo del nombre de cada concepto, con su condición y su precio: "De 20:00 a 24:00 · 24 €", "Lun a vie · 18 €", "Con bono · desde 15 €". La que se cumple ahora (`ReglaVariantes.Vigente`, con el "ahora" del Arranque pasado a la hora local del móvil) lleva delante un chip "Ahora", decisión del usuario para que no dependa solo del color. En tema oscuro, el texto del chip usa un azul más claro (`--color-ahora-texto`) para llegar al contraste 4,5:1. No hay variantes en los datos reales (decisión del 17/11), así que se prueban con lugares sintéticos.
    - **Centrado:** al abrir la ficha, el lugar queda en el centro horizontal y a media altura de la zona del mapa que la hoja no tapa. El puerto `IMapa` gana `Centrar(coordenada, punto de pantalla)`: la cámara centra la coordenada y luego desplaza el pivote lo que separa, en el suelo, el punto pedido del centro de la vista, así que es exacto también con el mapa girado e inclinado. Si la hoja aún no tiene medida, se usa el 72 % del alto y se recentra cuando se dispone.
    - **Comprobación:** pasan 341 pruebas EditMode (una por fila de la matriz; la cámara real girada 35° e inclinada 50°, y también con la inclinación mínima, donde el lugar se acerca al centro hasta que su punto toca el suelo; la vigente según la hora local del móvil y elegida solo entre las variantes que se ven). En las capturas de PC, Clínica Areas y el lugar sintético de pádel caen en el punto esperado (0 px de desvío) y `variantes.png` muestra el chip "Ahora". El log no tiene errores.
  - *Historia 1.8, parte 3 (18/11/2026): TalkBack de la ficha (commit `04405c7`).*
    - **Abrir:** el nodo de cada marcador de lugar pasa a ser un botón: el doble toque de TalkBack abre su ficha. Los puntos de interés siguen siendo texto sin acción.
    - **Ficha modal:** con la ficha abierta, el recorrido solo tiene sus nodos y el foco va al título. El orden es: nombre, tipo, dirección, cada área con sus conceptos y "Cerrar ficha" al final. Cada concepto se lee en una frase ("Sesión de fisioterapia, desde 40 € por sesión. Publicado en su web el 13/11/2026. Al día") y cada variante, en su propio nodo detrás ("Ahora: de 20:00 a 24:00, 24 €"). La píldora "Precios" también sale del recorrido.
    - **Nodos por apertura:** `AccesibilidadPantalla` gana grupos de controles que se dan de baja juntos (`QuitarControles`). Al cerrar la ficha o abrir otra, los nodos de la anterior se quitan de la jerarquía, así que ninguno apunta a un elemento que ya no existe.
    - **Cerrar:** con atrás, la X o un toque fuera, el mapa vuelve al recorrido y el foco vuelve al marcador que abrió la ficha, si sigue visible. Si no, no se fuerza ningún foco.
    - **Desplazamiento:** Unity 6000.3 tiene el evento `AccessibilityNode.focusChanged`. Cuando TalkBack enfoca un nodo del contenido, la ficha se desplaza hasta él (`ScrollView.ScrollTo`).
    - **Comprobación:** pasan 355 pruebas EditMode (14 nuevas, entre ellas el foco real en el título y de vuelta en el marcador, el desplazamiento al enfocar un concepto y el aviso de ubicación con la ficha abierta, que ahora la cierra; una o más por fila de la matriz, con el lector forzado y el doble toque simulado lanzando el evento `invoked` del nodo). Dos pruebas del filtro reconocían los marcadores por su rol de texto y ahora lo hacen por su etiqueta. Las 9 capturas de PC salen idénticas byte a byte a las de antes del cambio, y el log no tiene errores.
    - **Pendiente:** probar en un móvil con TalkBack el doble toque, el recorrido con el desplazamiento y la vuelta del foco al marcador.
  - *Ajuste visual de los controles del mapa (18/11/2026, pendiente de revisión).*
    - **Motivo:** el usuario vio en el editor que la píldora "Precios", su desplegable y los botones de tema y ubicación no eran como en el prototipo y parecían demasiado pequeños. Lo segundo era un efecto de la vista de PC: con 96 dpi, la ventana de 540 px equivalía a una pantalla de 900 dp de ancho, más del doble que un móvil. Antes de tocar nada se le enseñó una maqueta con el antes y la propuesta, que aprobó.
    - **Cambios:**
      - El desplegable no tiene borde y lleva sombra, mide 220 dp de ancho y el título va en mayúsculas pequeñas. La opción elegida se marca solo con el fondo, sin check (TalkBack sigue diciendo "seleccionado").
      - La píldora es de cristal y también lleva sombra.
      - Tema y ubicación van en una sola columna de cristal de 48 × 96 dp con separador, y la diana pasa a ser la flecha de navegación del prototipo.
      - Como UI Toolkit no tiene `box-shadow`, la sombra la dibuja una pieza propia (`SombraSuave`).
      - El cristal claro sube a 0,76 de opacidad para que el icono azul llegue a 4,5:1.
      - La captura de PC escala el panel a 412 dp de ancho, como un móvil.
    - **Comprobación:** pasan 369 pruebas EditMode. La captura comprueba en los dos temas que la sombra y el borde se resuelven con color. El log no tiene errores.
  - *Mapa y ficha sin solapes (18/11/2026, pendiente de revisión).*
    - **Motivo:** en la captura a tamaño de móvil (412 dp), la ficha de Clínica Areas salía con las filas montadas unas sobre otras: el nombre del concepto pisaba el origen y la frescura, y la unidad quedaba debajo del precio. En el mapa, el punto de interés "Plaza de los Milagros" quedaba debajo del marcador de la clínica, la Taberna Santo Domingo se montaba sobre él y los nombres del mapa se leían a través de los controles.
    - **Decisiones del autor:** la ficha se dispone sin solapes a cualquier ancho; se borran de los datos los conceptos de Clínica Areas sin importe ("Consultar"); en el mapa nada se pinta encima de un marcador ni debajo de los controles; los lugares sin precio siguen en el mapa, pequeños y atenuados. Una primera versión los ocultaba por un malentendido y se deshizo.
    - **Causa del solape de la ficha:** la app no carga el tema por defecto de Unity. Sin él, el contenedor del contenido de la lista desplazable (`ScrollView`) podía encoger (`flex-shrink: 1`) y se ajustaba al alto visible: 436 dp para unas filas que necesitan 831. Con él encogía cada fila y cada encabezado, y como el texto no encoge, se dibujaba fuera de su caja, encima de la fila siguiente. Se corrige en `PantallaMapa.uss` con `flex-shrink: 0` en el contenedor y en las filas, que ahora miden lo que su texto y se desplazan.
    - **Cambios en el mapa:**
      - `CapaMarcadores`: de los marcadores visibles, primero los que tienen precio y, entre iguales, del más cercano a la cámara al más lejano, se oculta el que pisa a uno ya mostrado o a un control. Lo oculto reaparece al acercar el zoom, cuando ya no se pisan.
      - `CapaPuntosInteres`: las zonas de los marcadores visibles y de los controles cuentan como ocupadas antes de colocar los puntos; un punto que las pisa se oculta, igual que si pisa a otro punto.
      - La píldora, el menú abierto y las columnas de botones son zonas ocupadas (sus rectángulos en dp del panel). Como Unity no difumina lo de detrás, el cristal sube a 0,90 de opacidad en los dos temas: los nombres de calle, que no se pueden ocultar, apenas se leen debajo.
      - Sin agrupar marcadores ni moverlos de su sitio, y sin asignaciones nuevas por fotograma (listas reutilizadas).
      - Tras la revisión: el marcador del lugar con la ficha abierta nunca se oculta, aunque quede bajo la píldora; y, a igual prioridad, gana el que ya se veía en el fotograma anterior, para que al girar el mapa no parpadeen ni se rehaga el recorrido de TalkBack.
    - **Comprobación final:** pasan 384 pruebas EditMode. La captura de PC da 0 pares de marcadores que se pisan, 0 puntos sobre un marcador, nada bajo un control y 0 textos de la ficha que se pisan, a 412 dp y a 360 dp con la letra al 130 %; el log no tiene errores. Pendiente (`deferred-work`): dos lugares de los datos comparten coordenada y uno queda siempre oculto.
    - **Datos:** el exportador del prototipo deja de exportar los conceptos sin importe; Clínica Areas pasa de 11 a 8 conceptos. "Consultar" se sigue probando con un lugar sintético.
    - **Comprobación:** pasan 380 pruebas EditMode (una o más por fila de la matriz). La captura de PC comprueba ahora que ningún texto de la ficha pisa a otro ni necesita más alto del que tiene, a 412 dp, a 360 dp con la fuente al 130 % (captura nueva `ficha-estrecha.png`) y en la de variantes. Sin la corrección daba 4 pares que se pisan y 44 textos desbordados; con ella, 0. En `marcadores.png` nada pisa al marcador de Clínica Areas (0 pares, 0 bajo un control, 7 lugares sin precio visibles). El log no tiene errores.
    - **Pendiente:** con la ficha abierta, el marcador de Clínica Areas queda centrado justo debajo de la píldora y se oculta por la regla de los controles; falta decidir si el marcador de la ficha abierta es una excepción.
  - *Historia 2.1: GPS con la pantalla apagada durante una ruta (19/11/2026, revisada).*
    - **Motivo:** con la pantalla apagada o con otra app delante, Unity se pausa y deja de recibir el GPS. Sin eso no hay navegación con el móvil en el bolsillo ni visita verificada (NFR-3, AD-19).
    - **Solución:** un plugin nativo de Android en Java (`Assets/Plugins/Android/`) con un servicio en primer plano de tipo `location` (`ServicioUbicacionRuta`) y una notificación fija del canal "Navegación", de importancia baja y sin sonido. Lee el GPS y la red con `LocationManager` cada segundo, sin Google Play Services, y guarda cada posición en una cola en memoria (como mucho una hora). Unity la vacía en cada fotograma y al volver de la pausa, y el plugin le avisa de cada posición con `UnitySendMessage` si la app está delante.
    - **Puerto:** `IUbicacion` gana `IniciarRuta`, `ActualizarNotificacion` y `TerminarRuta` (y el estado `RutaActiva` y `RutaSoloPrimerPlano`). Solo el adaptador de GPS toca el plugin, detrás de una interfaz pequeña (`IServicioRuta`) que en las pruebas es falsa y cuya versión con JNI solo se compila en Android. Sin ruta, la pausa apaga el GPS como en la 1.7; con ruta, el servicio sigue. Las posiciones acumuladas llegan al puerto en orden, una a una. En el editor y en PC no hay servicio: la ruta mantiene la ubicación de Unity.
    - **Permisos:** el post-proceso de Gradle añade `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_LOCATION` y `POST_NOTIFICATIONS` y el `<service>` no exportado; nunca `ACCESS_BACKGROUND_LOCATION` (si apareciera, se quita). En Android 13 o superior se pide el permiso de notificaciones al iniciar la ruta; si se deniega, la ruta sigue solo con la app delante y sale el aviso "Sin permiso de notificaciones, la navegación se pausa al apagar la pantalla.", una vez por ruta, en la misma ventana que el aviso de ubicación.
    - **Revisión:** tres revisores automáticos (a ciegas, casos límite y huecos de pruebas) encontraron fallos que se corrigieron:
      - Si el servicio no llegaba a arrancar o Android lo paraba, la posición se quedaba congelada. Ahora, si el servicio no está en marcha, se siguen usando las lecturas de Unity.
      - El servicio pide el GPS y la red por separado, aunque estén apagados. Así funciona también con solo la ubicación aproximada.
      - Mientras haya una lectura reciente del GPS, se descartan las de la red, que son menos precisas, para que el punto no salte.
      - Con la ubicación del móvil apagada, las posiciones antiguas de la cola se descartan.
      - Si el usuario da el permiso de notificaciones desde Ajustes durante la ruta, el servicio arranca al volver a la app.
      - Si se cierra el diálogo del permiso sin contestar, cuenta como denegado.
      - El aviso de notificaciones ya no tapa el de ubicación y se cierra cuando deja de ser cierto.
      - Quedan pendientes dos cosas: dar al puerto la hora y la velocidad de cada posición (para las historias 2.7 y 4.2) y un icono monocromo para la notificación.
    - **Comprobación:** pasan 400 pruebas EditMode (pausa con y sin ruta, cola en orden tras 2 minutos, parada, notificaciones denegadas y concedidas después, sin permiso de ubicación al empezar o a mitad de ruta, servicio caído, lectura de la cola y manifiesto). La APK compila y su manifiesto tiene el servicio con tipo `location` y no tiene `ACCESS_BACKGROUND_LOCATION`. La prueba en un móvil real queda pendiente (`deferred-work`). Nadie arranca aún la ruta: lo hará el botón Ir en la historia 2.7.
  - *Historia 2.2 (parte 1): hoja del buscador desplegable (19/11/2026, revisada).*
    - **Motivo:** no había forma de buscar (FR13, UX-DR4, UX-DR5).
    - **Decisiones del autor:** la hoja se queda donde se suelta, sin imán a media altura; la historia se parte en dos y TalkBack y los ajustes del sistema (reducir animaciones y transparencias) van en la parte 2. Antes de implementar se enseñó una maqueta.
    - **Solución:** una cápsula de cristal abajo con «Buscar en Sanlúcar» y un avatar genérico, que es el mismo elemento que la hoja (`HojaBuscador`). Al subir, pasa de forma de píldora a hoja con las esquinas de arriba redondeadas, mezclando ambas formas en el primer 15 % del recorrido.
      - La física es lógica pura en `FisicaHoja`, con los valores del prototipo: hueco de 64 dp arriba, lanzamiento a 0,6 dp/ms, se pliega por debajo del 15 % y se abre por encima del 88 %, toque por debajo de 6 dp.
      - Muestra los chips de las 8 categorías con los iconos y colores de los puntos de interés, y «Encuentra cerca» (llegan en la 2.4).
      - El botón atrás pliega la hoja. La ficha y la hoja nunca están abiertas a la vez, y la cápsula se oculta con la ficha.
    - **Revisión:** se corrigió un gesto que podía quedarse colgado si el dedo salía de la cápsula antes de empezar el arrastre: ahora se escucha en la raíz del panel. También se arreglaron:
      - una velocidad inicial inflada que convertía un arrastre lento en un lanzamiento;
      - los gestos horizontales en el campo, que movían la hoja;
      - el menú «Precios», que quedaba debajo de la hoja.
    - **Comprobación:** pasan 439 pruebas EditMode (cada fila de la física, gestos, botón atrás, ficha, menú y forma). En la captura de PC no queda ningún marcador bajo la cápsula y a media altura se ven los 8 chips y «Encuentra cerca». Quedan para el móvil los gestos reales y el teclado.
  - *Historia 2.2 (parte 2): TalkBack y quitar animaciones del buscador (19/11/2026, revisada).*
    - **Motivo:** la hoja no existía para TalkBack y su transición de 0,32 s no respetaba el ajuste «Quitar animaciones» del sistema (UX-DR19).
    - **Decisiones del autor:** hasta la 2.4 los chips y «Encuentra cerca» se leen como texto, no como botones; «Expandir» solo aparece si la hoja no está abierta del todo. Android no ofrece a las apps el ajuste de reducir transparencias, así que la cápsula sigue como en la parte 1.
    - **Solución:** con TalkBack, la cápsula es un botón «Buscar en Sanlúcar» detrás de los controles del mapa; activarla abre la hoja del todo y lleva el foco al campo. Desplegada a cualquier altura (también con el dedo), la hoja es modal: campo, «Expandir», «Contraer» (botones que solo ve el lector, como el zoom), los chips y «Encuentra cerca», sin marcadores ni controles del mapa. «Contraer» y atrás la pliegan y devuelven el foco a la cápsula. Si se abre la ficha, el menú o una ventana de ubicación, la hoja se pliega y manda su modo modal.
      - Un puerto nuevo, `IAccesibilidadSistema`, dice si están quitadas las animaciones; su adaptador de Android lee `Settings.Global.ANIMATOR_DURATION_SCALE` (fuera de Android o si falla, no). Se lee al arrancar y al volver a primer plano, y la hoja cambia entonces de altura sin transición.
    - **Revisión:** se añadieron pruebas sobre la jerarquía real que lee TalkBack (no solo sobre el recorrido calculado), sobre abrir una ventana de ubicación con la hoja desplegada y sobre el paso del ajuste desde el arranque. Además:
      - al desplegar la hoja con el dedo, el foco de TalkBack va al campo y no se queda en un nodo que ya no está;
      - el ajuste se lee una sola vez al volver a la app.
      Quedan pendientes anunciar el campo como campo de búsqueda (con la 2.3) y aplicar «quitar animaciones» también a los movimientos de cámara del mapa.
    - **Comprobación:** pasan 459 pruebas EditMode (una por fila de la matriz: abrir con TalkBack, recorrido abierta y a media altura, Expandir, Contraer, atrás, sin lector, plegar con el dedo, ficha, menú y aviso de ubicación encima, animaciones quitadas y normales y su relectura). La captura de PC sale igual que en la parte 1 y la APK compila. Queda probarlo en un móvil con TalkBack.
  - *Historia 2.3 (parte 1): buscar lugares y puntos de interés (19/11/2026).*
    - **Motivo:** la hoja del buscador aún no buscaba (FR14).
    - **Decisiones del autor:** la historia se parte en dos y las direcciones con Photon van en la parte 2. Plegar la hoja (atrás, abrir una ficha o arrastrarla abajo) no borra el texto ni los resultados; una «X» en el campo lo vacía.
    - **Solución:** la regla pura `ReglaBusqueda` (Dominio) normaliza como los repetidos de la 1.6 (minúsculas, sin tildes ni signos) y da una puntuación: nombre exacto, empieza igual o cada palabra de la consulta es el principio de alguna palabra del nombre o de la dirección. `ServicioBusqueda` (Aplicación) busca en los lugares de la app y en los puntos de interés de las teselas cargadas; quita los puntos sin categoría y los que repiten un lugar a menos de 80 m; pone primero los lugares y después los puntos, cada grupo por puntuación y distancia (al usuario o, sin posición, al centro del mapa), con un máximo de 20. Se busca a cada tecla porque todo es local y nada se guarda (FR17).
      - Cada fila (`FilaResultado`) lleva el icono del tipo, el nombre, el tipo y la distancia («350 m», «1,2 km», con el nuevo `FormatoDistancia`) y el precio resumido si lo tiene. Sin resultados sale «No hay resultados».
      - Tocar un lugar abre su ficha, que centra el mapa y pliega la hoja; tocar un punto centra el mapa en él y pliega la hoja (su ficha llega en la 2.5); desde el 23/11/2026 abre su ficha de punto libre.
      - TalkBack: cada resultado es un botón («Clínica Areas, Clínica privada, a 300 m, desde 20 €»), la X es «Borrar búsqueda» y al cambiar los resultados se anuncia «N resultados» o «No hay resultados».
    - **Comprobación:** pasan 503 pruebas EditMode (44 nuevas: regla, servicio con un mapa falso, una por fila de la matriz, y la hoja con TalkBack). En la captura de PC «clinica» da 5 lugares con Clínica Areas y su precio, en claro y en oscuro, y el registro no tiene errores. Queda probar en el móvil el teclado, la lista y TalkBack.
    - **Revisión:** tres revisores automáticos. Se corrigió que la búsqueda guardada no se rehiciera al volver a desplegar la hoja (distancias viejas), que tocar la X en la cápsula plegada también abriera la hoja, que las filas nuevas pudieran quedarse sin la escala de letra del sistema y que se perdiera el anuncio de TalkBack; se añadieron pruebas de la distancia desde la posición del usuario y de rehacer la búsqueda. Quedan para el móvil el arrastre de la lista, los anuncios al teclear deprisa y el contraste de los iconos nuevos. Pasan 505 pruebas.
  - *Historia 2.3 (parte 2): direcciones con Photon (19/11/2026).*
    - **Motivo:** el buscador debía encontrar también direcciones (FR14) y ofrecer «Buscar como dirección» si no hay resultados (UX-DR16).
    - **Decisiones del autor:** el TalkBack de las direcciones va en una parte 3. Sin límite de zona: todo el mundo, con prioridad por cercanía. «Buscar como dirección» busca al momento y sirve de reintento tras un fallo. Tocar una dirección centra el mapa y pliega la hoja (su ficha llega en la 2.5). Desde el 23/11/2026 abre su ficha de punto libre con Ir.
    - **Solución:** un puerto nuevo, `IGeocodificador` (Aplicación), con su adaptador `GeocodificadorPhoton` en un ensamblado propio (`Adaptadores/Busqueda`) y la lectura pura del GeoJSON en `LecturaPhoton`. `BuscadorDirecciones` espera 350 ms desde la última tecla y pide con al menos 3 caracteres. Cada tecla cancela la petición anterior y nunca se pinta la respuesta de un texto viejo; la espera se inyecta para que las pruebas no esperen ni salgan a la red. Las direcciones (como mucho 8) van detrás de los lugares y los puntos, con el icono de pin y «Dirección · localidad · a 1,2 km». Sin conexión o si Photon falla, los resultados locales siguen y sale «Las direcciones necesitan conexión». Nada se guarda (FR17).
    - **Revisión:** tres revisores. Se corrigieron las distancias viejas de las direcciones al volver a desplegar la hoja y los tramos repetidos de una misma calle. También se redondea a unos 100 m la posición que se manda a Photon, por privacidad. Se añadieron pruebas del arranque y del reintento tras un fallo.
    - **Comprobación:** pasan 538 pruebas EditMode. En la captura de PC, «calle ancha 10» da 7 direcciones y el registro no tiene errores. Pero Photon no encuentra la Calle Ancha de Sanlúcar con número: devuelve calles con ese nombre de otras ciudades. Queda estudiar si dar más peso a la cercanía, y probar en el móvil el modo avión.
  - *Historia 2.3 (parte 3): TalkBack de las direcciones (19/11/2026).*
    - **Solución:** cada dirección es un botón del recorrido («Calle Ancha 10, Dirección, Sanlúcar de Barrameda, a 1,2 km»), y también lo es «Buscar como dirección». El aviso de conexión es un texto al final del recorrido que se anuncia una vez, al aparecer. «N resultados» cuenta lugares, puntos y direcciones. Las direcciones llegan solas, sin que el usuario haga nada, así que no mueven el foco: solo se anuncian. Para eso la hoja conserva las filas locales y `AccesibilidadPantalla.RecuperarFoco` devuelve el foco al nodo donde estaba el usuario en la jerarquía nueva. Si ese nodo ya no existe, el foco va al campo.
    - **Revisión:** un revisor. Se corrigió que sin conexión el aviso parpadeara y se repitiera tras cada pausa al teclear, y que el foco se quedara en nada al desaparecer «No hay resultados». El foco que mueve el lector se guarda aparte del pedido por la app. Pasan 545 pruebas. Queda probarlo con TalkBack en el móvil.
  - *Historia 2.4 (parte 1): lista «Encuentra cerca» por categorías (19/11/2026).*
    - **Motivo:** las categorías de la hoja del buscador y «Encuentra cerca» no hacían nada (FR15). Además, la lista por distancia es la alternativa al mapa para quien usa TalkBack (UX-DR17).
    - **Decisiones del autor:**
      - La historia se parte en tres: lista, TalkBack de la lista y filtro del mapa con el chip de la píldora.
      - «Encuentra cerca» enseña todo lo cercano.
      - La tabla de cada categoría la decidió el autor; el veterinario entra en Salud.
      - Escribir quita la categoría.
      - La lista lleva una cabecera con título y X, y el botón atrás vuelve a las categorías.
      - Como mucho 50 filas y lugares a 3 km o menos.
    - **Solución:**
      - La regla pura `ReglaCerca` (Dominio) dice qué tipos de lugar y qué categorías de punto de interés entran en cada chip.
      - `ServicioCerca` (Aplicación) junta los lugares a 3 km o menos y los puntos de las teselas cargadas. Pone primero los que tienen precio, todo por distancia (al usuario o al centro del mapa), y quita los puntos que repiten un lugar de la misma categoría.
      - En la hoja, los chips y «Encuentra cerca» ya se pueden tocar. La lista reutiliza las filas de la 2.3 y, si está vacía, dice «No hay gasolineras cerca» o «No hay sitios de salud cerca».
    - **Revisión:** tres revisores. Se corrigió el texto de la lista vacía (salía «No hay salud cerca») y que un punto desapareciera por parecerse a un lugar de otra categoría. Se añadieron pruebas de los tipos de los datos reales, de la jerarquía real de TalkBack y de rehacer la lista al desplegar la hoja.
    - **Comprobación:** pasan 620 pruebas. En la captura de PC, Salud enseña primero Clínica Areas con «desde 20 €» y después dentistas, fisioterapia y centros públicos por distancia.
    - **Pendiente:**
      - Probar en el móvil los toques y el arrastre.
      - Decidir si el chip «Gimnasios» pasa a llamarse «Deporte», porque también enseña golf y piscinas.
  - *Historia 2.4 (parte 2): mapa filtrado por categoría (19/11/2026).*
    - **Motivo:** al elegir una categoría solo cambiaba la lista; el mapa seguía enseñándolo todo (FR15).
    - **Decisiones del autor:**
      - La píldora «Precios» se convierte en «Salud ✕» con el icono de la categoría; tocarla quita la categoría.
      - «Encuentra cerca» (todas) no filtra el mapa ni cambia la píldora.
    - **Solución:**
      - `CapaMarcadores` y `CapaPuntosInteres` tienen una `Categoria` que, mientras está puesta, sustituye al filtro «Precios» con `ReglaCerca.Incluye`. En el mapa no hay radio de 3 km y las reglas de solapes y repetidos siguen igual.
      - La categoría sigue activa al plegar la hoja y se quita con la X de la cabecera, el botón atrás con la lista, al escribir o con la píldora. El filtro «Precios» guardado no cambia nunca.
      - `PildoraPrecios` tiene un modo categoría: el icono del chip (mismo glifo y color), su nombre y una ✕ dibujada en lugar de la flecha. En ese modo no abre el menú.
      - TalkBack: la píldora es el botón «Quitar categoría Salud»; al quitarla se anuncia el filtro que vuelve («Precios en el mapa: Hostelería»).
    - **Revisión:** tres revisores automáticos encontraron 16 posibles problemas y se arreglaron 6. Si la píldora se pulsa con la ficha abierta, ahora primero cierra la ficha. Hay pruebas nuevas para el doble toque de TalkBack (etiqueta y valor del nodo) y la captura hace más comprobaciones. Los otros 10 se descartaron: no se cumplían o quedaban fuera de lo pedido. Por ejemplo, el botón atrás con la hoja plegada no quita la categoría porque así se decidió.
    - **Comprobación:** pasan 635 pruebas. En la captura de PC `cerca-mapa`, con la hoja plegada solo se ven los marcadores de salud y la píldora dice «Salud ✕». El registro no tiene errores.
    - **Pendiente:** probar en el móvil a elegir una categoría, plegar la hoja y tocar «Salud ✕».
  - *Historia 2.4 (parte 3): TalkBack de la lista «Encuentra cerca» (19/11/2026).*
    - **Motivo:** con TalkBack, los chips se leían pero no se podían activar, y la lista de una categoría no entraba en el recorrido. Quien usa lector de pantalla no podía usar «Encuentra cerca».
    - **Decisión del autor:** al quitar la categoría, el foco vuelve al chip que estaba elegido, para no tener que recorrer otra vez los chips.
    - **Solución:**
      - Los chips y «Encuentra cerca» son botones.
      - Al abrir la lista se anuncia «Salud cerca, N resultados» (o «No hay … cerca») y el foco va al título. Sin GPS, el título dice además «desde el centro del mapa».
      - Cada fila es un botón con el mismo texto que en la búsqueda; una fila de lugar abre la ficha con el foco en su título. La X es el botón «Quitar categoría».
      - Con la hoja plegada, la lista no está en el recorrido.
    - **Revisión:** tres revisores automáticos encontraron 15 posibles problemas. Se añadió una prueba de arrastrar la hoja abierta con la lista, y gracias a ella apareció un fallo real: al arrastrar, las filas se rehacen y TalkBack perdía la fila en la que estaba. Ahora el foco vuelve a la fila del mismo sitio. El resto se descartó por no cumplirse o quedar fuera de lo pedido.
    - **Comprobación:** pasan 654 pruebas; la build de PC y la captura terminan sin errores.
    - **Pendiente:** probar en el móvil con TalkBack (elegir Salud, recorrer la lista, abrir una ficha y quitar la categoría). Al abrir la lista, TalkBack lee el título y después el anuncio, que también empieza por el título: comprobar si se hace pesado.
  - *Historia 2.5 (parte 1): mantener pulsado el mapa abre un punto libre (19/11/2026).*
    - **Motivo:** solo se podía ir a lugares de la app; hay que poder elegir cualquier punto del mapa (FR16). Las partes 2 (pegar enlaces) y 3 (compartir con la app) abrirán esta misma ficha.
    - **Decisiones del autor:** mantener pulsado da siempre un punto libre, también encima de un marcador o de un punto de interés, como Google Maps; la ficha del sitio sigue abriéndose con un toque. Chincheta azul marino con borde y centro blancos (blanca con centro azul marino en oscuro), aprobada en maqueta: la roja se confundía con farmacias y centros de salud.
    - **Solución:**
      - `DetectorToque` cuenta el tiempo: con el dedo quieto (menos de 6 dp) 500 ms lanza la pulsación larga sin esperar a soltar; desde ahí el dedo ya no mueve el mapa y al soltar no hay toque. Un segundo dedo la anula. `IMapa` gana `PulsacionLarga` y `Desproyectar` (el rayo de la cámara contra el suelo; nada por encima del horizonte).
      - La ficha del punto es la misma hoja de la ficha del lugar, sin muestra ni precios: título con las coordenadas («36,7781, -6,3515») hasta que llega la dirección («Calle Ancha, 10»), subtítulo «Punto en el mapa» y, debajo, «Buscando la dirección…» o la localidad. Se centra y se cierra igual que la del lugar, y la chincheta se va con ella.
      - La dirección sale de la geocodificación inversa de Photon (`/reverse`). Cada punto nuevo cancela la petición anterior, y una respuesta tardía no cambia la ficha. Sin conexión o sin calle se quedan las coordenadas. No se guarda nada (FR17).
      - TalkBack: la ficha es modal con título, subtítulo y «Cerrar ficha»; al llegar la dirección se cambia la etiqueta del título en su nodo, sin mover el foco.
    - **Revisión:** tres revisores automáticos encontraron 21 posibles problemas y se arreglaron 7:
      - la gotita de la maqueta junto a «Punto en el mapa»;
      - un fallo real: un segundo dedo tras la pulsación larga movía el mapa bajo la ficha;
      - el idioma en la petición a Photon y la liberación de cada petición;
      - pruebas que faltaban (el arranque pasa Photon a la pantalla y la chincheta sigue al mapa) o que no comprobaban nada.
      
      Quedan para el móvil: si Photon da calles lejanas al pulsar en el río o el pinar, y una vibración al soltar la chincheta.
    - **Comprobación:** pasan 684 pruebas (30 nuevas, una por fila de la matriz y las de Photon y del puerto real). En la captura de PC `punto`, la chincheta y la ficha salen con «Punto en el mapa» y la dirección real de Photon («Calle Carmen Viejo»); el registro no tiene errores.
    - **Pendiente:** probar en el móvil a mantener pulsado, arrastrar sin querer, usar dos dedos y quitar los datos.
  - *Historia 2.5 (parte 2): pegar un enlace o unas coordenadas en el buscador (20/11/2026).*
    - **Motivo:** si a alguien le mandan una ubicación (un enlace de Google Maps por WhatsApp, unas coordenadas), no podía abrirla en PriceMap (FR17).
    - **Decisiones del autor:** el mensaje de enlace no reconocido va dentro de la hoja, en el sitio de «No hay resultados», y se queda mientras siga el texto (sin aviso flotante). Mientras se resuelve un enlace corto se lee «Comprobando el enlace…» en ese mismo sitio. Icono de la fila: la chincheta en negativo, aprobado en maqueta.
    - **Solución:**
      - Un analizador puro del dominio (`LectorUbicacionCompartida`) reconoce enlaces de Google Maps (`?q=`, `@lat,lng`, `!3d…!4d…`, que gana, `ll=`, `query=`), Apple Maps (`ll=`, `q=`), `geo:` (con `?q=`, que gana) y coordenadas sueltas con punto o coma decimal, y comprueba los rangos. Las sueltas tienen que llevar decimales, para no confundir una búsqueda como «10 20».
      - Los enlaces cortos (`maps.app.goo.gl`, `goo.gl/maps`) se resuelven con un puerto nuevo (`IResolutorEnlaces`); el adaptador web lee la cabecera `Location` sin seguirla sola, hasta 5 saltos, y para en cuanto la URL deja de ser corta (así no llega a la página de Google, que en Europa manda al aviso de cookies). Cambiar el texto cancela la petición y una respuesta tardía no pinta nada.
      - Con un enlace o coordenadas reconocidos, la única fila es «Ir a este punto» con las coordenadas de subtítulo, y no se buscan lugares ni direcciones. Tocarla abre la ficha de punto libre de la parte 1. Si parece un enlace y no se reconoce (o el corto falla), se lee «No reconozco esta ubicación. Prueba a copiar el enlace completo.».
      - TalkBack: la fila es un botón «Ir a este punto, 36,7781, -6,3515» y se anuncia «1 resultado»; el mensaje de no reconocido y «Comprobando el enlace…» también se anuncian.
    - **Revisión (tres revisores en paralelo):** se arreglaron diez cosas.
      - Los enlaces de mapas pegados sin `https://` (`maps.app.goo.gl/…`, `www.google.com/maps/…`) se reconocen y se les pone `https://`.
      - El enlace corto se reconoce por su servidor, no por aparecer en el texto. Uno con `http://` se pide con `https://`, porque Android no deja http en claro.
      - «1,5 2,5» ya no se toma por coordenadas: hacen falta dos decimales.
      - La URL compartida solo se escribe en el registro en las builds de desarrollo.
      - Con la lista «cerca» abierta y el campo vacío, el enlace pendiente se olvida.
      - Pruebas nuevas: el bucle de saltos del resolutor (sin red, con un salto falso), el paso del resolutor desde el arranque hasta el buscador y el anuncio de TalkBack al cambiar de coordenada. También se corrigió una prueba que pasaba siempre.
      - Se descartaron otros formatos (grados, minutos y segundos, OpenStreetMap, rutas), que no los pide el spec.
    - **Comprobación:** pasan 771 pruebas (las nuevas cubren cada fila de la matriz, los casos raros del analizador, el resolutor y la pantalla con un resolutor falso). En la captura de PC `enlace`, la única fila es «Ir a este punto» con «36,7781, -6,3515»; el registro no tiene errores.
    - **Pendiente:** probar en el móvil a pegar un enlace de Google Maps, también el corto de «Compartir». Hay que ver a qué URL lleva hoy el corto: si es una URL sin coordenadas (solo el nombre del sitio), habrá que leerla de otra forma. También hay que dejar sitio a la «X» del campo cuando el texto es largo.
  - *Historia 2.5 (parte 3): compartir una ubicación con PriceMap (20/11/2026).*
    - **Motivo:** quien recibía una ubicación en WhatsApp o la veía en Google Maps tenía que copiarla y pegarla en el buscador; PriceMap no salía en «Compartir» ni abría los enlaces `geo:` (FR17).
    - **Decisiones del autor:** una ubicación compartida que se reconoce abre sola la ficha de punto libre (al momento o al resolverse el enlace corto), como si se tocara «Ir a este punto»; si el usuario cambia el texto antes, ya no se abre. Si no se reconoce, el mensaje queda en la hoja. No se registran los enlaces `https` de Google Maps en «Abrir con»: desde Android 12 el sistema no se los ofrecería a PriceMap.
    - **Solución:**
      - Al generar el proyecto de Android (`CompartirAndroid`), la actividad de Unity gana dos filtros: «Compartir» con texto (`SEND` + `text/plain`) y «Abrir con» para `geo:` (`VIEW`). Sin duplicarlos y sin actividad propia: la de Unity ya es `singleTask` y guarda el último intent.
      - Un plugin Java mínimo (`PuenteCompartir.tomar`) lee el texto compartido o la URI `geo:` del intent y lo consume, para que al volver a la app no se repita; un intent que viene de la lista de recientes no cuenta. Lo llama un puerto nuevo (`IEntradaCompartida`) desde el arranque y al volver la app delante.
      - Al campo va solo el enlace si lo hay (Google Maps lo pone detrás del nombre y la dirección del sitio) o el texto en una línea, de 200 caracteres como mucho. Después pasa por todo lo de la parte 2: analizador, enlace corto y mensaje de no reconocido. Un texto cualquiera se busca como uno escrito.
      - Se cierra la ficha abierta y se abre la hoja del buscador del todo (con TalkBack, modal y con el foco en el campo, y se anuncia lo que se ve). Si está a la vista la explicación o el aviso de ubicación (primer arranque), se espera a que se cierren. No se guarda nada.
    - **Revisión (tres revisores en paralelo):** cuatro arreglos.
      - Si el enlace corto se resuelve cuando el usuario ya ha abierto otra ficha, se queda la suya.
      - Lo compartido también se lee al salir de la pausa (pantalla partida), con un método que ahora tiene prueba.
      - Las pruebas del primer arranque hacen correr el `Update` real, que es el que aplica lo pendiente.
      - Se corrigió un comentario.
      - Quedan para después: comprobar en el móvil que lo compartido no vuelve tras matar el proceso, y que un `geo:0,0?q=dirección` busque la dirección.
    - **Comprobación:** pasan 797 pruebas (26 nuevas: los filtros del manifiesto, el texto para el campo y cada fila de la matriz en la pantalla real con un resolutor falso). La APK compila y su manifiesto final lleva los dos filtros. En la captura de PC `compartido`, un `geo:` que llega con la hoja plegada deja el enlace en el campo y abre solo la ficha del punto, con la dirección real de Photon («Calle Carmen Viejo»).
    - **Pendiente:** probar en el móvil a compartir desde Google Maps (un sitio y una chincheta) y desde WhatsApp (ubicación y enlace), abrir un `geo:` y volver a la app sin compartir.
  - *Historia 2.6 (parte 1): rutas y tiempos en la ficha (20/11/2026).*
    - **Motivo:** la fila de acciones de la ficha estaba vacía desde la 1.8 y la app no sabía calcular rutas (FR7, FR10, FR18).
    - **Decisiones del autor:** la 2.6 se parte en dos: esta parte calcula las rutas y llena la fila; dibujar la ruta, encuadrarla y la recta «Sin conexión: ruta aproximada» son la parte 2. Sin posición, los dos modos enseñan «—» e Ir hace lo mismo que el botón de ubicación. OSRM de reserva en `routing.openstreetmap.de`, porque el servidor de demostración de OSRM solo tiene coche.
    - **Solución:**
      - Dominio: `ModoViaje` (coche, a pie), `Ruta` (línea, metros, segundos y maniobras), `Maniobra` con un `TipoManiobra` común a los dos servicios, y `Polilinea.Decodificar` (precisión 5 y 6). Los minutos se redondean hacia arriba, con 1 como mínimo. Los umbrales (8 s, 20 m, 2 min) van en `ParametrosNegocio`.
      - Un puerto nuevo (`IRutas`) con dos adaptadores en `Adaptadores/Rutas`. Valhalla (FOSSGIS) recibe un POST con `shortest: true`, `language: es-ES` y `directions_type: maneuvers`, y da las maniobras con su texto. OSRM recibe un GET con 3 alternativas y se queda con la de menor distancia; sus maniobras van sin texto (tipo y modificador), que se escribirá en la 2.7. Los dos lectores son puros y se prueban con JSON en línea. Privacidad: el origen va con 5 decimales (≈1 m, lo que pide una ruta que empieza donde está el usuario) y el destino con 6 a los servidores de FOSSGIS, y no se guarda nada.
      - `ServicioRutas` pide coche y a pie a la vez. Si Valhalla falla o tarda 8 s, pregunta a OSRM; si también falla, ese modo queda sin tiempo. Las rutas buenas se guardan solo en memoria durante 2 min y valen mientras el usuario no se mueva más de 20 m (mismo destino y modo). Cada petición nueva, o cerrar la ficha, cancela las anteriores, y una respuesta tardía no pinta nada.
      - La fila (`FilaAcciones`, maqueta `key-ficha-lugar`): coche y a pie con su icono vectorial, su nombre y los minutos debajo («…» mientras se calculan, «6 min», «—» sin tiempo), 48 dp y borde de control; el elegido con borde y texto de acento; e Ir como botón primario de 52 dp. Coche elegido al abrir. Va bajo la cabecera, fuera del contenido que se desplaza. La misma fila sale en la ficha de punto libre.
      - Los tiempos se piden al abrir la ficha con la posición activa, o en cuanto llega la posición con la ficha abierta. Ir con posición deja el modo, el destino y su ruta listos para la parte 2.
      - TalkBack: entre la cabecera y los conceptos, «Coche, 6 minutos» (con estado seleccionado), «A pie, 15 minutos» e «Ir a Clínica Areas» («Ir a este punto» en un punto libre); sin tiempo, «Coche, sin tiempo». Al cambiar de modo se anuncia «A pie, 15 minutos, seleccionado», y los minutos y el estado cambian en el mismo nodo, sin rehacer la jerarquía.
    - **Revisión (tres revisores en paralelo):** siete arreglos. Lo elegido con Ir se olvida al cerrar la ficha y se completa si la ruta llega después de pulsar; hay prueba de TalkBack de la fila en el punto libre. Los errores de la llegada de tiempos se registran y el refresco de TalkBack solo toca el estado «seleccionado». El texto sobre la privacidad del origen queda como es: 5 decimales, unos 1 m. Para la parte 2 queda que Ir recalcule desde la posición actual.
    - **Comprobación:** pasan 846 pruebas (49 nuevas: polilínea, URL y cuerpo, los dos lectores, el servicio con servicios falsos —dos modos, reserva, 8 s, caché y cancelación— y cada fila de la matriz en la pantalla real). En la captura de PC `ficha` (rutas falsas, sin red), la fila enseña «Coche · 4 min» elegido, «A pie · 12 min» e Ir, entre la cabecera y los conceptos, sin taparlos; también a 360 dp con la fuente al 130 % y en la ficha de punto libre. El registro no tiene errores.
    - **Pendiente:** probar en el móvil con datos que salen minutos reales en coche y a pie desde Sanlúcar.
  - *Historia 2.6 (parte 2a): Ir dibuja la ruta (20/11/2026).*
    - **Motivo:** tras la parte 1, Ir no hacía nada visible: el usuario no veía por dónde ir (FR7, FR18, UX-DR8).
    - **Decisiones del autor (maqueta v2 aprobada, inspirada en una captura de navegación):** la línea cubre la calle entera, por su centro (unos 9 m con su borde, nunca menos de 6 dp en pantalla), con relleno de acento y un borde algo más oscuro. Arriba, una tarjeta oscura (clara en el tema oscuro) con la maniobra y la siguiente; abajo, la llegada, los minutos y la distancia con una X para quitar la ruta. Mientras hay ruta se ocultan la píldora, el botón de tema y la cápsula, y el botón de ubicación baja a la izquierda. La parte 2 se vuelve a partir: la 2a (esta) con conexión; la 2b, la ruta aproximada sin conexión.
    - **Solución:**
      - `IMapa` gana `MostrarRuta`, `QuitarRuta` y `Encuadrar` (con un `RectPantalla` en px). `MapaVectorial` dibuja la línea con la misma cinta de las calles (uniones y extremos redondos), en un objeto propio que no se libera con las teselas: borde en la cola 1960 y relleno en la 1970, encima de las calles y debajo de los edificios. El ancho es el mayor entre 9 m y lo que ocupan 6 dp en el centro de la vista (distancia, campo de visión y dpi); se rehace si cambia más de un 10 % al hacer zoom. Los colores siguen al tema (`PaletaMapa`).
      - `CamaraMapa.Encuadrar`: el pivote va al centro de la ruta, una búsqueda binaria da la distancia más corta con la que la caja de los puntos proyectados cabe en la zona libre (dentro de los límites 60–4000 m), y un desplazamiento lleva esa caja al centro de la zona; se repite tres veces por la perspectiva. Sin giro ni inclinación nuevos, al instante.
      - `PantallaMapa`: Ir cierra la ficha, enseña «Calculando la ruta…» y «…» y pide la ruta del modo elegido desde la posición actual (`ServicioRutas.Calcular`, con la caché de 20 m). Al llegar, la línea, el encuadre entre las tarjetas (16 dp de margen, 32 dp a los lados; medidas en `ParametrosNegocio`) y las tarjetas; si falla, «No se ha podido calcular la ruta» y «—». Tocar la tarjeta de abajo reabre la ficha con el modo de la ruta: las tarjetas se ocultan y la línea sigue; al cerrarla vuelven y la ruta se encuadra otra vez. La X o atrás quitan la ruta. La hora de llegada es la local de ahora más la duración.
      - `TarjetaManiobra` (flechas vectoriales por tipo de maniobra) y `TarjetaRuta` (tres columnas y la X de 48 dp), con tokens nuevos en `Tokens.uss`.
      - Con una ruta a un punto libre, su chincheta se queda en el destino al cerrar la ficha y se va al quitar la ruta. Lo que se comparte con la app mientras hay ruta quita la ruta y abre la hoja del buscador (si no, se perdía detrás de la cápsula oculta).
      - TalkBack: la tarjeta de arriba es lo primero del recorrido, como texto («Conduzca hacia el noroeste por Calle Zarate. Después, gire a la derecha hacia Calle Regina»), y recibe el foco al pulsar Ir y al llegar la ruta. La de abajo es un botón («Llegada 13:33, 4 minutos, 560 m») que abre la ficha, seguido de «Quitar ruta». Sin píldora ni botón de tema en el recorrido mientras hay ruta.
    - **Problema encontrado:** con `directions_type: maneuvers`, Valhalla no devuelve el texto de las maniobras (comprobado con una petición real), así que la tarjeta habría dicho siempre «Ruta a…». Se pide `instructions`, que trae el texto en castellano (en forma de usted: «Gire a la derecha hacia Calle Regina»).
    - **Comprobación:** pasan 869 pruebas (28 nuevas: cada fila de la matriz en la pantalla real con servicios falsos, la zona de encuadre, el ancho de la línea, los textos de las tarjetas, la línea real de `MapaVectorial` con sus colas y colores y cómo se recolorea y se rehace cada fotograma, el encuadre real de la cámara y del puerto, y el contraste de las tarjetas y de la línea en los dos temas; se borran las tres de `ElegirParaIr`, que la pantalla ya no usaba). En las capturas de PC `ruta` (claro, coche) y `ruta-oscuro` (a pie), con la forma y los textos de una ruta real de Valhalla guardados en la captura, la ruta cabe entera entre las dos tarjetas y el registro no tiene errores.
    - **Pendiente:** probar en el móvil una ruta real en coche por calles de sentido único y a pie; que la línea cubra la calle al acercar y se vea al alejar.
  - *Historia 2.6 (parte 2b): ruta aproximada sin conexión (21/11/2026).*
    - **Motivo:** sin conexión (fallan Valhalla y OSRM), Ir solo decía «No se ha podido calcular la ruta»: el usuario se quedaba sin ruta (FR18, UX-DR16).
    - **Decisiones del autor (maqueta aprobada):** la ruta aproximada va por las calles de las teselas que el mapa ya guardó (opción A), no en línea recta, porque la recta atraviesa edificios. Se dibuja igual que la normal pero en un azul más suave (claro `#8FA3E6` con borde `#5F78C9`; oscuro `#6F86C9` con borde `#44599A`), la tarjeta de arriba dice «Ruta aproximada por las calles guardadas», los valores de abajo llevan «≈» y sale un toast «Sin conexión: ruta aproximada». Si no hay calles que unan los dos puntos (o están a más de 300 m de una calle), lo de siempre más el toast «Sin conexión: no hay calles guardadas para esta ruta». La ficha sin red da también los minutos aproximados («≈ 4 min»; TalkBack, «unos 4 minutos»).
    - **Solución:**
      - `GrafoCalles` (Dominio, puro): un nodo por vértice con la coordenada entera global de la tesela (x · 4096 + px al zoom 14), que es la misma en las teselas vecinas para el mismo vértice de OSM; una arista por tramo, sin sentidos únicos. Los extremos sueltos a ≤ 5 m de otro tramo se unen a él (cruces que la generalización deja sin vértice y los cortes del margen de cada tesela). Origen y destino van al punto más cercano de un tramo a ≤ 300 m y el camino más corto por metros sale de A* (con un índice de celdas de 50 m). Tiempo a 30 km/h en coche y 5 km/h a pie. En coche no se usan `path`, `track`, `raceway`, `busway` ni `bus_guideway`; a pie, ni `motorway` ni `trunk`; ninguno, barcos, trenes ni teleféricos.
      - `RutasCallesGuardadas` (adaptador del mapa vectorial, un `IRutas` más): lee del disco las teselas del rectángulo origen–destino más una de margen (como mucho 5 × 5), pasa los vértices a lat/lon con la fórmula de las teselas y construye el grafo, todo fuera del hilo principal y cancelable. Nunca descarga nada.
      - `ServicioRutas` recibe ese servicio como tercero: solo se le pregunta si fallan los dos, y su ruta no va a la caché (al volver la red, la siguiente petición va a Valhalla). `Ruta` gana la marca `Aproximada`.
      - `IMapa.MostrarRuta` gana `aproximada` (colores suaves de `PaletaMapa`); `TarjetaManiobra`, `TarjetaRuta` y la fila de la ficha enseñan «≈» y TalkBack dice «Ruta aproximada. Llegada hacia las 13:34, unos 4 minutos, unos 1,2 km». `Toast` nuevo y reutilizable (fondo `text`, texto `bg`, 12 dp de radio, centrado bajo la tarjeta de arriba, 3 s, sin animación), anunciado por TalkBack después de llevar el foco a la tarjeta de arriba.
    - **Comprobación:** pasan 897 pruebas (28 nuevas: el grafo con cada fila de la matriz —cruce sin vértice, modos, lejos, sin unir, tiempo—, teselas MVT de prueba escritas en una carpeta temporal —dos teselas vecinas, path a pie y no en coche, sin teselas, cancelación, rectángulo de 5 × 5—, el servicio sin caché para la aproximada, la pantalla real con línea suave, «≈», toast y TalkBack, la cancelación, los colores reales de `MapaVectorial` y el contraste de la línea y del toast en los dos temas). En las capturas de PC `ruta-aproximada` (claro, coche: 563 m por 14 puntos) y `ruta-aproximada-oscuro` (a pie: 371 m), con las rutas falsas fallando y las teselas de Sanlúcar ya en disco, la línea suave sigue las calles, las tarjetas llevan «≈» y el toast se ve bajo la de arriba; el registro no tiene errores.
    - **Pendiente:** probar en el móvil en modo avión, tras haber visto la zona, una ruta a pie y otra en coche por Sanlúcar.
  - *Historia 2.7 (parte 1): seguir la ruta (21/11/2026).*
    - **Motivo:** Ir dibujaba la ruta pero no guiaba: la maniobra no avanzaba, el tiempo no bajaba, no se recalculaba al desviarse y el GPS de fondo de la 2.1 no se encendía (FR19, FR20).
    - **Decisiones del autor:** la 2.7 se parte en dos. Esta parte sigue la ruta, recalcula, enciende el GPS de fondo, lanza los eventos y pone las tarjetas al día; la cámara que sigue, «Volver a centrar», la flecha, «Buscando señal GPS» y la pantalla encendida son la parte 2. La navegación empieza al pulsar Ir, sin otro botón, y se sigue viendo la ruta entera.
    - **Solución:**
      - `ReglaNavegacion` (Dominio, pura): proyecta cada posición al tramo más cercano con una proyección equirrectangular local en `double`. Lo que queda es la línea desde el punto proyectado; el tiempo, duración × (lo que queda / total), y los metros, los de la ruta en esa proporción (así al empezar coinciden con la ficha). La maniobra siguiente es la primera con texto que no ha quedado atrás; cada maniobra se coloca en la línea al empezar, cerca de donde la esperan sus metros (así una vuelta por la misma calle no la confunde). El avance nunca baja: se busca desde 30 m por detrás y, si el GPS cae más atrás, se queda donde estaba; un tramo más de 100 m por delante solo gana si está 15 m más cerca (`ParametrosNegocio`).
      - `ServicioNavegacion` (Aplicación): empieza con la ruta ya llegada, enciende el GPS de fondo con «Navegando a Clínica Areas · 6 min» y cambia el texto al cambiar el minuto. Si el usuario pasa más de 5 s seguidos a más de 40 m de la ruta, recalcula desde su posición con `ServicioRutas.Calcular` (sin conexión, la aproximada); la ruta vieja se ve hasta tener la nueva, solo hay un recálculo a la vez y, si falla, toast «No se ha podido recalcular la ruta» y otro intento a los 10 s si sigue fuera. Lanza `RutaIniciada`, `TiempoRestanteCambiado` y `RutaTerminada(Manual)` para la épica 4 (AD-9); no crea visitas. Reloj y espera inyectables, como `ServicioRutas`.
      - Pantalla: la línea empieza en el punto proyectado; la tarjeta de arriba lleva encima del texto la distancia a la maniobra («En 120 m»; sin ella en la maniobra misma) y, sin maniobras (OSRM o aproximada), «Quedan 1,2 km»; la de abajo, la llegada, los minutos y la distancia que quedan («≈» si es aproximada). La X pasa a ser «Terminar ruta» para TalkBack, que anuncia cada maniobra nueva una sola vez («En 70 m, gire a la izquierda hacia Calle Santa Ana…»), no a cada metro. La X, atrás u otra ruta desde la ficha terminan la navegación (fuera el GPS de fondo y la notificación).
    - **Comprobación:** pasan 936 pruebas (35 nuevas: la regla con rutas sintéticas —empezar, a mitad, pasar la maniobra, ruido, desvío, vuelta por la misma calle, sin maniobras—, el servicio con GPS, rutas y reloj falsos —notificación, minutos, desvío corto y largo, uno a la vez, fallo y reintento, sin conexión, terminar— y la pantalla real, una prueba por fila de la matriz). En la captura de PC `navegacion` (ubicación falsa a mitad de Calle Regina, 4 m fuera de la línea) la línea empieza en el punto proyectado y la tarjeta dice «En 70 m · Gire a la izquierda hacia Calle Santa Ana»; el registro no tiene errores.
    - **Revisión de código (22/11/2026):** tres revisores encontraron dos fallos reales, ya arreglados. (1) Si la ruta empezaba a más de 40 m del usuario (lejos de una calle, o la aproximada, que une calles hasta a 300 m), se recalculaba cada 5 s sin fin y daba la misma ruta; ahora no se recalcula a menos de 40 m de donde se pidió la ruta. (2) Si el usuario volvía a la ruta mientras se pedía el recálculo, la respuesta sustituía igual a la buena; ahora se descarta. Además, los minutos se redondean al segundo en un solo sitio (`Ruta.MinutosDe`), para que la ficha y las tarjetas den lo mismo con segundos decimales. Hay 6 pruebas nuevas (942 en total, todas en verde) y la captura `navegacion` sale igual. Dos detalles quedan para más adelante: el texto de llegada (2.9) y el motivo de fin al cerrar la app (épica 4).
    - **Pendiente:** en el móvil, una ruta real a pie con la pantalla apagada: al volver, la maniobra y los minutos al día y la notificación cambiando de minuto.
  - *Historia 2.7 (parte 2b): la cámara sigue al usuario (22/11/2026).* **[Parcial]**
    - **Motivo:** al navegar la cámara se quedaba quieta con la ruta entera: el usuario se salía de la vista y tenía que mover el mapa a mano, y la pantalla se apagaba sola (FR20).
    - **Decisiones del autor:** al pulsar Ir se ve la ruta entera y a los 2 s la cámara pasa a seguir (con «reducir animaciones», al instante y sin transición). Arriba va el rumbo de la marcha en coche y el de la brújula a pie. «Volver a centrar» es el mismo botón redondo de ubicación, sin texto, con un icono de mira al soltar la cámara. En coche y a 40 m o menos de la ruta, el punto de posición va sobre la línea; a pie, siempre la posición real. La parte 2 se parte: la flecha y «Buscando señal GPS» quedan para la 2.7c.
    - **Solución:**
      - `ReglaRumbo` (Dominio, pura, la reutilizará la flecha): como Unity no da la velocidad ni el rumbo del GPS, se sacan de las posiciones. Se guarda una posición de referencia; cuando la nueva está a 3 m o más, la velocidad es esa distancia entre el tiempo pasado y, si pasa de 1 m/s, el rumbo es el de la referencia a la nueva (parado se queda el último). Elige el rumbo de arriba: a pie la brújula; si no, el de la marcha; sin él todavía, el del tramo de la ruta.
      - Cámara: `IMapa` gana `Seguir`, `DejarDeSeguir` y el evento `GestoUsuario` como miembros por defecto de la interfaz (los mapas falsos de las pruebas no cambian). `CamaraMapa` persigue el objetivo en cada fotograma con un suavizado exponencial (al 95 % en 0,6 s), el giro por el lado más corto; así la transición del principio y cada posición nueva salen sin saltos. Un arrastre (pasado el umbral de un toque), los dos dedos, el botón derecho, la rueda o el teclado la sueltan y avisan.
      - Pantalla: `SeguimientoCamara` (esperando 2 s → siguiendo → suelto, reloj inyectable). Siguiendo, la posición va centrada en el tercio de abajo de la zona libre entre las tarjetas, inclinada 45° y a 350 m en coche o 180 m a pie (`ParametrosNegocio`). Un gesto o los botones de zoom la sueltan; el botón de ubicación pasa al icono de mira y TalkBack lo llama «Volver a centrar» (siguiendo, «Siguiendo tu posición»). Al terminar (X, atrás u otra ruta) la cámara se queda donde está y el botón vuelve a lo de siempre. Con la ficha reabierta no sigue; al cerrarla, vuelve.
      - Pantalla encendida: puerto `IPantallaSistema` con su adaptador (`Screen.sleepTimeout`), conectado en el Arranque a los eventos de la navegación: `NeverSleep` al empezar y el valor del sistema al terminar.
    - **Comprobación:** pasan 985 pruebas (35 nuevas: la regla del rumbo, el seguimiento, la cámara real —transición, al instante, gesto, toque, por el puerto— y la pantalla con una prueba por fila de la matriz, más la pantalla encendida y el Arranque). En las capturas de PC `navegacion-sigue` (claro y oscuro) la cámara está a 45° y 350 m, con el rumbo de la marcha (47°) arriba, el punto sobre la línea en el tercio de abajo, la ruta delante y los puntos de interés derechos; en `volver-centrar`, tras mover el mapa, el botón lleva la mira. El registro no tiene errores.
    - **Revisión de código (22/11/2026):** tres revisores; tres fallos reales arreglados. (1) En coche parado, el ruido del GPS daba rumbos al azar y el mapa giraba en un semáforo: ahora el rumbo sale del punto sobre la línea, que nunca retrocede. (2) A pie, la brújula solo giraba el mapa al llegar una posición nueva y, parado, nunca: ahora se recoloca en cuanto la brújula se mueve 2° o más. (3) La velocidad podía quedar infinita con dos posiciones a la misma hora. Además, la decisión de si el usuario ha movido el mapa pasa a una función pura con pruebas, y hay pruebas de la ficha abierta navegando, del punto sobre la línea y de «reducir animaciones» releído (985 en total, todas en verde). Para el móvil quedan dos comprobaciones: si la cámara «avanza y se para» entre posiciones, y que la pantalla no se apague.
    - **Pendiente:** revisión de código; en el móvil, una ruta en coche y otra a pie (la cámara gira con la marcha o con la brújula, arrastrar la suelta y la pantalla no se apaga).
  - *Historia 2.7 (parte 2c): flecha en coche y «Buscando señal GPS» (22/11/2026).* **[Parcial]**
    - **Motivo:** navegando en coche la posición seguía siendo el círculo con el cono de la brújula, que no dice hacia dónde se va; y si se perdía el GPS (un túnel, un garaje) no se notaba nada (FR20, UX-DR8, UX-DR16).
    - **Decisiones del autor:** la flecha sale solo navegando en coche; a pie y sin ruta, el círculo con el cono. El aviso de señal es una pastilla pequeña bajo la tarjeta de arriba.
    - **Solución:**
      - Lecturas del GPS: `IUbicacion` gana `Lecturas` (miembro por defecto, 0: las ubicaciones falsas de las pruebas no cambian), un contador que sube con cada lectura nueva de Unity o de la cola del servicio de ruta aunque repita la coordenada y sin avisar por `Cambiado`. Así un coche parado en un semáforo no cuenta como «sin señal».
      - `ServicioNavegacion.Comprobar` (la pantalla lo llama en cada fotograma, con el reloj inyectado): más de 10 s (`ParametrosNegocio.NavegacionSinSenalSegundos`) sin lecturas ni posiciones nuevas → `SinSenal` y `SenalCambiada`; el contador del desvío vuelve a cero y no se recalcula (tampoco el reintento pendiente). La primera lectura nueva lo deshace y el desvío cuenta desde ahí. Al terminar la ruta se quita sin avisar.
      - `MarcadorPosicion`: modo flecha dibujado con Painter2D (relleno `#2F5BEA`, en oscuro `#4C7DFF`, borde blanco, 26 dp; desde el 23/11/2026, sin borde, de 30 dp y sobre un disco blanco de 48 dp) en el sitio del círculo y sin cono; apunta al rumbo de la marcha de `ReglaRumbo` (sin él, al de la ruta) menos el giro del mapa, así que con la cámara siguiendo mira hacia arriba. Sin señal, todo al 40 % en su último sitio.
      - Pantalla: `PastillaSenal` con los colores del toast, texto de 12 sp y menos relleno, centrada 8 dp bajo la tarjeta de arriba y sin irse sola; con la ficha abierta no se ve y, si sale un toast a la vez, va debajo de ella. TalkBack anuncia «Buscando señal GPS» y «Señal GPS recuperada» una vez cada uno.
    - **Comprobación:** tras dos revisiones de código pasan 1013 pruebas (28 nuevas: las lecturas del adaptador con fuentes falsas, la señal del servicio —10 s justos, 12 s, parado 60 s, vuelve, sin recálculo, reintento, terminar—, la pantalla con una prueba por fila de la matriz y el contraste de la flecha y la pastilla en los dos temas). En las capturas de PC `navegacion-flecha` (claro y oscuro) la flecha mira hacia arriba sin cono sobre la línea; en `sin-senal`, a los 10 s sin lecturas de la ubicación falsa, la flecha está atenuada (0,4) y la pastilla «Buscando señal GPS» sale bajo la tarjeta de arriba; al volver las lecturas se va. El registro no tiene errores.
    - **Pendiente:** en el móvil, que la flecha gire con la marcha y la pastilla salga en un túnel o un garaje y se vaya al salir.
  - *Historia 2.8 (parte 1): indicaciones por voz con la app delante (22/11/2026).* **[Parcial]**
    - **Motivo:** quien conduce tenía que mirar el móvil para saber la siguiente maniobra (FR21).
    - **Decisiones del autor:** tres modos: «todo» (al pasar una maniobra dice la siguiente con su distancia, otra vez a 300 m en coche o 50 m a pie, y en el momento), «lo importante» (sin el aviso de 300 m) y «nada». Se eligen con un botón en la columna de cristal que abre una fila de tres iconos sin texto (volumen, volumen con «!», volumen tachado). La voz con la pantalla apagada queda para la parte 2.
    - **Solución:**
      - `ReglaVoz` (Dominio, pura) decide qué decir con cada estado de la navegación: nunca repite un aviso, calla la salida, la llegada y las rutas sin textos (OSRM, aproximada), redondea a 50 m en coche y 10 m a pie y dice los kilómetros con un decimal. El aviso del momento usa el texto corto de Valhalla (`verbal_succinct_transition_instruction`, en `Maniobra.TextoCorto`).
      - `ServicioVoz` (Aplicación) escucha a `ServicioNavegacion`, guarda el modo en `IPreferencias` y habla por el puerto `IVoz`. Fuera de la ruta calla, porque la distancia se mide sobre la ruta vieja; tras el recálculo empieza de cero.
      - Android: `PuenteVoz.java` usa el TextToSpeech del sistema en castellano de España, con el uso de audio de navegación y el foco transitorio que baja el volumen de las otras apps; en silencio o vibración no habla. `VozAndroid` lo llama por JNI; en PC, `VozNula`. El manifiesto declara el motor de voz (`<queries>`), sin lo que Android 11+ no lo encuentra.
      - Pantalla: botón de voz encima de «Volver a centrar», solo navegando, con el icono del modo; la fila se cierra al elegir, al tocar el mapa o con atrás. TalkBack lee «Indicaciones por voz, solo las importantes» y anuncia el modo elegido.
    - **Comprobación:** tras la revisión de código (7 arreglos) pasan 1065 pruebas (52 de voz: una por fila de la matriz con una voz falsa, redondeo, JSON de Valhalla, recálculo, modo guardado, fuera de la ruta, voz que falla, composición, manifiesto, pantalla y TalkBack) y el APK se genera sin errores. En las capturas de PC `voz` (claro y oscuro) se ve la fila de tres iconos abierta junto al botón.
    - **Pendiente:** en el móvil, que los avisos lleguen a tiempo, en castellano, y que calle en vibración; la voz con la pantalla apagada (parte 2).
  - *Historia 2.8 (parte 2): la navegación sigue con el móvil bloqueado (22/11/2026).* **[Parcial]**
    - **Motivo:** con el móvil bloqueado Android pausa Unity y con él toda la navegación: la voz callaba, no se recalculaba y, al volver, las posiciones guardadas podían disparar avisos viejos (FR21).
    - **Decisiones del autor:** bloqueado debe hacer lo mismo que delante, a tiempo real; se descartó mandar al servicio una lista de avisos. Si en el móvil Unity se pausara igualmente, el respaldo es mandar la línea de la ruta al servicio de Android.
    - **Solución:**
      - La GameActivity de Unity pausa el motor desde su parte nativa, así que se pasó a la entrada Activity con una actividad propia (`ActividadPrincipal`, copia de la plantilla de Unity 6000.3): navegando no pausa Unity al ir a segundo plano y solo avisa a C# cuando la app deja de verse (`onStop`); sin ruta, todo como siempre. Si la ruta acaba con la app detrás, hace entonces la pausa saltada; si Android destruye la actividad o para el servicio, todo vuelve a lo normal.
      - El servicio de ruta mantiene el procesador despierto (`PARTIAL_WAKE_LOCK`) mientras escucha el GPS.
      - En segundo plano la app va a 5 fotogramas por segundo con la cámara del mapa apagada; al volver, 60 y la cámara.
      - Respaldo: si el sistema pausa Unity igualmente, al volver la voz no habla durante 1 s desde el primer fotograma (solo da por dicho lo que ya no toca).
    - **Comprobación:** tras la revisión de código (8 arreglos) pasan las pruebas EditMode y el APK se genera con `ActividadPrincipal` como lanzadora, los filtros de compartir y `WAKE_LOCK`. La parte Java solo se comprueba leyendo el código.
    - **Pendiente:** probar en el móvil que, bloqueado, los avisos llegan a su hora y se recalcula.
  - *Pantalla de inicio animada (23/11/2026).* **[Parcial]**
    - **Motivo:** al abrir se veía el logo de Unity, después un momento en negro y el mapa aún sin centrar ni cargar.
    - **Decisiones del autor:** sin logo de Unity; el pin cae, rebota y flota mientras carga (animación A) y, al acabar, se encoge hasta la posición del usuario mientras el mapa se acerca; también en oscuro. Se queda hasta tener la posición y el mapa de la vista: como mucho 4 s y nunca menos de 1 s. Sin permiso, con la ubicación apagada o con algo compartido, no espera a la posición.
    - **Solución:**
      - Vista nativa de Android (`PantallaInicio`, `PuenteInicio`) encima de Unity desde `onCreate`: el motor tarda 2–3 s en arrancar y no puede animar. El arranque del sistema y el fondo de la ventana llevan el mismo color.
      - C# solo decide cuándo se va (`ServicioInicio`, puro y con reloj inyectado) y hacia dónde. Si no avisa, la vista se quita sola a los 15 s.
      - `MapaVectorial.VistaCargada` mira si están construidas las teselas que se ven y `CamaraMapa.AcercarDesde` acerca el mapa sin cambiar su distancia.
    - **Comprobación:** tras la revisión de código (18 arreglos, 9 de ellos pruebas que faltaban) pasan 1488 pruebas y el APK se genera con las imágenes y los colores dentro. La parte Java solo se comprueba leyendo el código.
    - **Pendiente:** en el móvil, que no haya fotograma negro, que la animación sea fluida, el pin hasta el punto azul, oscuro, sin animaciones y TalkBack. En Android 8–11 el color del arranque sigue al sistema y no al tema guardado.
  - *Historia 3.1: bienvenida, privacidad y consentimientos (24/11/2026).* **[Parcial]**
    - **Motivo:** la app solo explicaba la ubicación (y con una frase que ya no era verdad) y pedía el permiso al abrir; faltaban el aviso de privacidad y el consentimiento explícito (FR41).
    - **Decisiones del autor:** tres pasos (qué es, privacidad, permisos) a pantalla completa y solo la primera vez, con el logo de la pantalla de inicio; aviso completo dentro de la app con el responsable; «Empezar» siempre activo y lo no contestado cuenta como «Ahora no».
    - **Solución:**
      - `ServicioBienvenida` (Aplicación, pura): pasos, aviso y consentimientos; guarda `bienvenida.vista` lo último, así que cerrarla a mitad la repite. Puerto `INotificaciones` (`NotificacionesAndroid`: pide `POST_NOTIFICATIONS` desde Android 13).
      - `PantallaBienvenida` va puesta debajo de la pantalla de inicio, que esa vez no espera ni a la posición ni al mapa. Mientras está, nada más responde y lo compartido con la app espera. La explicación de la 1.7 queda solo en el botón de ubicación.
      - Los textos se corrigieron para decir qué sale del móvil: búsquedas y puntos marcados (Photon), rutas con la posición exacta (Valhalla u OSRM), el mapa (OpenFreeMap) y los enlaces cortos (Google).
      - La copia de seguridad de Android se desactiva (`allowBackup=false`): si no, al reinstalar se daría por visto el aviso.
    - **Comprobación:** tras la revisión de código (12 arreglos) pasan 1540 pruebas, el APK se genera y las capturas de PC muestran los tres pasos y el aviso en claro, oscuro y al 200 % sin cortes.
    - **Pendiente:** en el móvil, instalación limpia en Android 13+ y anterior; volver a leer el aviso desde Ajustes (3.5) y completarlo con los derechos del RGPD.
  - *Historia 3.2 (parte 1): reglas de seguridad de Firestore (24/11/2026).* **[Parcial]**
    - **Motivo:** antes de guardar nada en la nube hace falta que cada usuario solo pueda leer y escribir lo suyo (NFR-8).
    - **Solución:** proyecto de Firebase en el plan gratuito y `firebase/firestore.rules` en el repositorio: todo denegado salvo `usuarios/<uid>` y sus subcolecciones, solo para su dueño. El `google-services.json` queda fuera de git, porque el repositorio es público.
    - **Comprobación:** 22 pruebas con el emulador de Firestore (lo propio, lo ajeno, sin sesión, otras colecciones y lotes mezclados). La revisión rompió las reglas a propósito de varias formas: ocho pasaban las primeras pruebas, así que se ampliaron hasta que todas fallan.
    - **Pendiente:** publicar las reglas en la consola; en la épica 4, sustituir la regla general por subcolecciones con nombre (las visitas y los aportes no se pueden modificar).
  - *Historia 3.2 (parte 2): Firebase dentro de la app (24/11/2026).* **[Parcial]**
    - **Motivo:** la app necesita el SDK de Firebase (cuentas y Firestore) sin depender de él para arrancar ni llenar de Firebase el resto del código.
    - **Solución:** puertos `IServidor` (iniciando, listo o no disponible) e `IAlmacen` (leer, escribir por campos y escuchar un documento, con códigos de error propios); el adaptador `PriceMap.Adaptadores.Firebase` es el único que ve el SDK (14.1.0) y una prueba lo vigila. Firestore guarda una copia en el móvil de hasta 100 MB para usarla sin conexión. Los paquetes (80 MB) no van al repositorio: los baja un script que comprueba su SHA-256. El SDK trae Analytics: se desactiva y se le quitan los permisos de publicidad.
    - **Comprobación:** tras la revisión de código (16 arreglos) pasan 1601 pruebas, la APK lleva las bibliotecas de Firebase y no pide permisos de publicidad, y en PC la app arranca sin servidor.
    - **Pendiente:** verlo en el móvil («Firebase listo», y abrir sin conexión) y probar Firestore real con el emulador (3.3).
- **5.3 Modelo de datos:** implementación del modelo de 4.3.
- **5.4 Lógica de precios:** implementación de 4.6 y simulación.
- **5.5 Gamificación**
- **5.6 Interfaz**
- **5.7 Compilación y despliegue en móvil**
- **5.8 Problemas encontrados y soluciones**

---

## 6. Pruebas y validación **[Parcial]**

### 6.0 Validación técnica tras la auditoría (16/11/2026)
Antes de las pruebas con usuarios, un agente de validación repitió las pruebas automatizadas de los cuatro auditores (funcionalidad, UI/UX, accesibilidad y claridad) y las de cada oleada de corrección. También regeneró y revisó una a una las 69 capturas del recorrido completo, en 360×740, 390×844, 1366×768 y 844×390, en claro y oscuro.

**Resultado.**
- **Hallazgos resueltos:** todos salvo uno parcial. Algunos botones (más y menos del número de personas, botones de la tienda) cumplen el mínimo de 24 px de WCAG 2.2 AA, pero no llegan a los 44 px recomendados.
- **Contraste:** 0 incumplimientos en 17 pantallas, en claro y oscuro.
- **Teclado y lector de pantalla:** 0 fallos en las pruebas de foco y teclado.
- **Consola:** sin errores en todo el recorrido.

**Regresiones encontradas y corregidas durante la validación (9):**
- La pregunta de llegada podía abrirse sobre la pantalla de cuenta tras cerrar sesión.
- Se publicaban ofertas con precio negativo.
- Había un contraste insuficiente en modo oscuro.
- Había detalles de maquetación.

**Pendiente:** en horizontal (844×390), el aviso emergente tapa unos segundos el título de la tarjeta de pregunta.

### Pruebas con usuarios (pendiente)

Plan previsto:
- **Participantes:** entre 5 y 20 usuarios. Todos usan la app con gamificación: es, junto a los precios, la novedad de la app, así que no se hace un grupo sin ella (decidido 16/11/2026). Su efecto se mide con la tasa de aporte, el uso de la tienda y un cuestionario final sobre si las recompensas animaron a responder.
- **Instrumento:** cuestionario SUS (System Usability Scale; Brooke, 1996).
- **Datos que se recogen:** puntuación SUS, número de aportaciones y observaciones.

*(Pendiente: guion de la prueba, tareas y resultados.)*

---

## 7. Resultados **[Pendiente]**

---

## 8. Conclusiones y trabajo futuro **[Pendiente]**

Ideas de trabajo futuro ya identificadas:
- Avisos tipo Waze (controles, radares, accidentes), con aviso de uso por copiloto.
- Integración con Android Auto y CarPlay.
- Panel para negocios.
- Moderación avanzada.
- Ampliar a más plantillas de categoría.

---

## Bibliografía **[Pendiente]**

*(Verificar cada referencia antes de usarla.)*
- Brooke, J. (1996). SUS: A "quick and dirty" usability scale. En *Usability Evaluation in Industry*. Taylor & Francis.
- Deterding, S., Dixon, D., Khaled, R. y Nacke, L. (2011). From game design elements to gamefulness: defining "gamification". *Proceedings of the 15th International Academic MindTrek Conference*.

---

## Anexo: registro de decisiones

| Fecha | Decisión | Motivo |
|---|---|---|
| 11/11/2026 | El precio es el dato principal; las reseñas son secundarias | Es el problema que no resuelven los mapas actuales |
| 11/11/2026 | La pregunta se hace al llegar con el GPS de la app, sin seguimiento en segundo plano | Privacidad y simplicidad |
| 11/11/2026 | Gamificación con puntos, niveles, monedas y cosméticos | Incentivar aportaciones; encaja con el grado |
| 12/11/2026 | Tema enviado al tutor | Requisito para empezar |
| 12/11/2026 | Mapa real, 2.5D y con estilo editable | Mayor realismo y control estético del autor |
| 12/11/2026 | Modelo unificado: establecimiento → conceptos con precio, con tres plantillas | Escalable, sin código distinto para cada sector |
| 12/11/2026 | Servicios locales: un precio y sin reseñas | Simplicidad; se evitan opiniones sobre servicios sensibles |
| 12/11/2026 | Hostelería: solo rango por persona, con valoración | El gasto varía según lo que se pida |
| 12/11/2026 | Ocio: precio total en espacios y por persona en actividades, con valoración | Así cobran realmente estos negocios |
| 12/11/2026 | 3 criterios de valoración por categoría | Valorar rápido sin perder información útil |
| 12/11/2026 | BMAD Method como metodología y repositorio público en GitHub | Trabajo estructurado con IA y trazabilidad del desarrollo |
| 12/11/2026 | Skill UI/UX Pro Max y Playwright | Guías de diseño y pruebas automáticas de la interfaz |
| 12/11/2026 | Paleta B (azul y amarillo) en modo claro y D (navegación nocturna) en modo oscuro | Color de recompensa propio para la gamificación; mismo lenguaje de color en los dos modos |
| 12/11/2026 | Prototipo navegable web en Sanlúcar de Barrameda con mapa real 2.5D | Validar el concepto y el flujo antes de elegir la tecnología final |
| 12/11/2026 | Interfaz principal estilo Mapas de Apple (propuesta A) con botón € circular pequeño | Pantalla del GPS limpia; el botón de Waze se reduce para que no moleste |
| 12/11/2026 | El botón € solo funciona con visitas pendientes ("Más tarde"), avisando con un punto rojo | Solo se aportan precios de visitas verificadas; recordatorio discreto |
| 12/11/2026 | Regla deslizable + escribir el número para introducir el precio | Los botones de +1/−1 eran lentos con precios altos |
| 12/11/2026 | Perfil desde un avatar con anillo de XP; tienda y ajustes dentro del perfil | Quitar la barra de pestañas y dejar el mapa despejado |
| 12/11/2026 | Categorías en un desplegable y buscador que se despliega hacia arriba (favoritos, tipos, visitados) | Menos elementos fijos en pantalla y acceso rápido a los sitios habituales |
| 12/11/2026 | Función "Avísame si cambia" en los sitios | Refuerza la idea de precios al día y da un motivo para volver a la app |
| 12/11/2026 | El buscador se queda a la altura donde se suelta (con lanzamiento y extremos) y se puede arrastrar desde la lista | Comportamiento esperado por quien usa Mapas de Apple; se puede buscar sin tapar todo el mapa |
| 12/11/2026 | Coche o a pie se elige en la ficha, con el coche por defecto | Se decide al elegir destino, no en los ajustes |
| 12/11/2026 | Burbuja de sonido encima de la de precios | Silenciar rápido sin entrar en los ajustes |
| 12/11/2026 | La pregunta "¿has ido?" sale 1 minuto después de llegar (provisional, se ampliará) | Dar tiempo a entrar; no interrumpir una ruta en curso |
| 12/11/2026 | La pregunta pasa a salir 1 hora después de llegar | Que dé tiempo a recibir el servicio antes de preguntar el precio |
| 12/11/2026 | La ruta sigue al llegar hasta pulsar "He finalizado" o pasar 1 minuto | No cortar la guía de golpe al llegar |
| 12/11/2026 | Flecha de rumbo en coche; círculo con cono de brújula a pie | Lenguaje visual habitual de los GPS; a pie importa hacia dónde miras |
| 12/11/2026 | Barra del buscador fina y simétrica | Más espacio para el mapa y un aspecto más limpio |
| 13/11/2026 | Tienda encima de "Tu actividad" en el perfil | Dar más visibilidad a la parte de recompensa |
| 13/11/2026 | Logo del tipo de negocio (con insignia €) en lugar del precio en el mapa | Mapa más limpio y reconocible; el precio se consulta en la ficha |
| 13/11/2026 | Puntos de interés reales de OSM integrados en el mapa (22 categorías) y "Encuentra cerca" | La app sirve como GPS completo y no solo para los sitios con precios |
| 13/11/2026 | GPS a cualquier destino: direcciones, ubicaciones de WhatsApp y puntos marcados en el mapa | Uso diario como GPS ("ir a casa de un amigo") |
| 13/11/2026 | Rutas reales con OSRM (servidores públicos) con alternativa sin conexión | Indicaciones y tiempos reales sin desarrollar un motor de rutas propio |
| 13/11/2026 | Marcadores de precio ocultos por defecto; desplegable "Precios" con opción "Ninguno" | Pantalla del GPS limpia; los precios se muestran solo si el usuario los pide |
| 13/11/2026 | 121 lugares reales de Sanlúcar (OSM) en lugar de los 7 inventados; se incluyen instalaciones públicas que cobran | Prototipo realista, sin sitios falsos encima de los reales |
| 13/11/2026 | Sin precios inventados: los declara el negocio o los aporta el primer cliente; con 3 aportes manda lo que pagan los clientes | No atribuir datos falsos a negocios reales (el repositorio es público) y dar veracidad al precio |
| 13/11/2026 | Precios iniciales de internet solo si los publica el propio negocio en su web oficial (con enlace y fecha); se descartan los agregadores | Partir de datos fiables y comprobables; los agregadores suelen estar desfasados o ligados a una aseguradora |
| 13/11/2026 | Nuevos tipos "Clínica privada" y "Fisioterapia"; se añaden Clínica Areas (con sus tarifas oficiales) y FisioSanlúcar | Definir mejor la salud privada, que sí tiene precio |
| 13/11/2026 | Los centros sanitarios públicos aparecen en el mapa solo con su ubicación, sin precios | Son útiles como destino del GPS, pero no se paga en el momento |
| 13/11/2026 | Dos tipos de cuenta: cliente y negocio. El negocio vincula su local o lo da de alta a mano y declara sus precios | Que los negocios pongan su precio inicial y que puedan aparecer los que faltan en el mapa |
| 13/11/2026 | Inicio de sesión con Google como opción principal (simulado en el prototipo; Firebase Authentication en la app real) | Registro rápido, sin contraseñas, y correo verificado |
| 16/11/2026 | Rutas más cortas respetando el sentido de las calles: Valhalla con `shortest` (OSRM, la más corta de sus alternativas, como respaldo); la ruta empieza y acaba en la calle | El usuario quiere la ruta más corta y nunca en contra dirección; OSRM solo optimiza el tiempo y añadía tramos rectos fuera de las calles |
| 16/11/2026 | Frescura del precio con umbrales según el tipo de negocio (hostelería: 30/60 días; servicios y ocio: 3/6 meses) y monedas extra por confirmar precios viejos | Mostrar cuánto de fiable es un dato e incentivar que se mantenga al día |
| 16/11/2026 | Ofertas del negocio con fecha de fin, en la ficha y con una etiqueta muy pequeña en una esquina del marcador; se retiran con 3 avisos de "ya no está" | Dar valor a la cuenta de negocio sin recargar el mapa |
| 16/11/2026 | Plan de tarde desde la ficha: paradas, personas, gasto estimado y ruta parada a parada | Usar los precios para planificar, no solo para consultar |
| 16/11/2026 | Avisos de precio incorrecto con peso según la reputación; con 3 puntos el precio queda "en revisión" y se pide confirmarlo | Filtrar datos falsos o desfasados sin ocultar información |
| 16/11/2026 | Auditoría con cuatro agentes (funcionalidad, UI/UX, accesibilidad y claridad) y corrección de las 44 tareas resultantes en la rama `revision`, sin commits hasta la revisión del autor | Asegurar la calidad del prototipo antes de las pruebas con usuarios y documentar el proceso |
| 16/11/2026 | Decisiones de la revisión (ver 4.11e): progreso guardado en el navegador, "Ir" bajo la cabecera, sin asa, plan con 1 persona, "Ver precios", color por plantilla, "Más de 50 €" abierto, anonimato también en dentistas, cuentas de negocio sin aportar como cliente, voz implementada | Resolver las decisiones abiertas de la auditoría con la opción recomendada |
| 16/11/2026 | Se añade ChemaBarber (tipo nuevo "Barbería"), sin precio porque no lo publica | Ubicación facilitada por el autor; mismo criterio de no inventar precios |
| 16/11/2026 | Al abrir un sitio, la cámara lo centra en la parte del mapa que no tapa la ficha (desplazamiento en píxeles) | Con el mapa inclinado, el desplazamiento fijo en grados dejaba el marcador debajo de la ficha |
| 16/11/2026 | Corregido: los marcadores de precio salían desplazados y se movían con la cámara | El CSS del marcador (`position: relative`, transición y escala de `transform`) pisaba la posición que calcula MapLibre; ahora el marcador va dentro de un contenedor que solo mueve MapLibre |
| 16/11/2026 | PRD de la app final con BMAD (`_bmad-output/planning-artifacts/prds/`): Android con Unity, mapa vectorial de OpenFreeMap (opción C), 60 requisitos funcionales y 8 no funcionales | Fijar qué debe hacer la app real antes de crear épicas e historias |
| 16/11/2026 | El MVP incluye todas las funciones del prototipo, sin priorizar ni aplazar ninguna (incluida la voz) | El autor considera que hay tiempo suficiente con la ayuda de la IA |
| 16/11/2026 | Visita verificada: ruta iniciada con la app en la que faltan 5 minutos o menos para llegar; la pregunta llega una hora después, aunque la app esté cerrada | Contar la visita aunque no se pulse "Terminar ruta" ni se llegue al punto exacto |
| 16/11/2026 | Variantes del precio (horario, día, tipo de cliente) en la app final | Recoger la cuarta dimensión del estudio de tipos de negocio, que el prototipo no tenía |
| 16/11/2026 | Diseño de UX de la app final con BMAD (`_bmad-output/planning-artifacts/ux-designs/`): DESIGN.md (colores, tipografía, componentes) y EXPERIENCE.md (pantallas, estados, accesibilidad, recorridos), con 4 maquetas de pantallas clave | Fijar cómo se ve y cómo funciona la app antes de la arquitectura y las historias |
| 16/11/2026 | Accesibilidad sin gestos obligatorios: zoom con botones y lista "Encuentra cerca" con TalkBack, hoja con Expandir/Contraer, regla ajustable, plan con Subir/Bajar; zonas táctiles de 48 dp; solo vertical en la v1 | La revisión de accesibilidad vio que casi todo dependía de gestos |
| 16/11/2026 | Arquitectura de la app final con BMAD (`_bmad-output/planning-artifacts/architecture/`): hexagonal ligero (núcleo de reglas en C# puro + adaptadores), 18 decisiones de arquitectura | Que las partes construidas por separado encajen y que las reglas del precio se puedan probar sin Unity |
| 16/11/2026 | Firebase en plan gratuito (Spark), sin código en servidor: el precio lo calcula cada móvil a partir de los aportes; verificación del negocio por correo del local o aprobación manual; "Avísame si cambia" al abrir la app | Coste cero y sin tarjeta (NFR-7); el plan gratuito no permite funciones en servidor ni SMS |
| 16/11/2026 | Aportes y avisos públicos sin ningún dato del autor; las reglas de Firestore exigen una visita verificada propia (una por lugar y día) | Anonimato total y control de trampas sin servidor |
| 17/11/2026 | Épicas e historias con BMAD (`_bmad-output/planning-artifacts/epics.md`): 9 épicas y 53 historias con criterios Dado/Cuando/Entonces; orden 1 explorar → 2 llegar → 3 cuenta → 4 aportar → 5 tienda → 6 evaluación → 7 precios fiables → 8 negocio → 9 plan, revisado en una mesa redonda de agentes | Las épicas 1 a 6 son lo mínimo para las pruebas con usuarios; la frescura va en la épica 1 porque la ficha la necesita |
| 17/11/2026 | GPS en segundo plano solo durante una ruta, con notificación fija de navegación (servicio en primer plano de Android); nunca sin ruta | Que la visita verificada se cree con el móvil en el bolsillo y la pantalla apagada, sin seguir al usuario fuera de la navegación |
| 16/11/2026 | Lugares base empaquetados en la app; Firestore solo para lo que cambia; caché de Firestore para el uso sin conexión | Arranque rápido, uso sin conexión desde el primer día y menos cuota |
| 17/11/2026 | La app se llama PriceMap (`com.pricemap.app`); el mapa se hace solo con OpenStreetMap (OpenFreeMap) y se retira Cesium | Decisión del autor al empezar la implementación (historia 1.1) |
| 17/11/2026 | Proyecto de Unity organizado en capas con `.asmdef` y una prueba automática que impide dependencias prohibidas | Que la arquitectura hexagonal se cumpla sola, no por disciplina |
| 17/11/2026 | Solo dos temas (claro y oscuro); el primero sigue al sistema y la elección se guarda en el móvil (en la cuenta desde la Épica 3) | FR-6 y UX-DR22; el tema "Original" de OSM era solo de la prueba técnica |
| 17/11/2026 | Historia 1.2 dividida: primero la base de interfaz y el tema; después los gestos de inclinación y la carga de teselas alrededor de la vista | Especificación demasiado grande para implementarla de una vez con fiabilidad |
| 17/11/2026 | Historia 1.4 dividida: primero los datos base de los lugares y su resumen de precio; después los marcadores con logo y TalkBack | Mismo motivo que la 1.2 |
| 17/11/2026 | El marcador anuncia el precio más bajo del lugar ("desde"), sin contar precios con bono. Los conceptos que la web solo da con bono no se migran: si el negocio los quiere, los añade él | Un precio que solo vale para quien tiene bono no representa al lugar |
| 17/11/2026 | Los centros sanitarios públicos nunca tienen precio (regla del Dominio) y se ven en el mapa como puntos de interés (historia 1.6) | No se paga en el momento; los datos del prototipo ya los excluían |
| 17/11/2026 | Marcadores con punta, como en el prototipo, y con colores fijos en los dos temas (naranja de hostelería oscurecido a `#D6740F`) | Que se vea el punto exacto del lugar y que el logo blanco cumpla el contraste mínimo de 3:1 |
| 17/11/2026 | Filtro "Precios": su elección se guarda en el móvil y el menú no lleva contador de sitios. Historia 1.5 dividida (TalkBack del menú y botón atrás en la parte 2) | FR-2 pide recordar la elección; el autor prefiere un menú limpio y especificaciones más cortas |
| 16/11/2026 | Los recorridos de ejemplo no atribuyen precios inventados a negocios reales: en UJ-2, Andrés es el pionero que aporta el primer precio de ChemaBarber, que sigue en la app con su nombre | Mismo criterio que el prototipo: no inventar precios de negocios reales |
| 18/11/2026 | Ubicación: el permiso se pide al abrir la app por primera vez, después de una explicación breve ("solo mientras tienes la app abierta… no la guarda ni la comparte"); si se deniega, el botón de ubicación explica cómo activarlo y lleva a los Ajustes. Historia 1.7 dividida (orientar al norte, TalkBack del botón y aviso de denegado en la parte 2) | Decisión del autor; explicar para qué se usa antes de pedirla y no volver a pedirla en cada arranque |
| 18/11/2026 | En el mapa nada se pinta encima de un marcador ni debajo de los controles (entre marcadores que se pisan gana el que tiene precio y, si no, el más cercano); los lugares sin precio siguen viéndose; el cristal de los controles sube a 0,90 de opacidad | En la zona de Clínica Areas se montaban marcadores y puntos y se veía mal; sin agrupar ni mover marcadores |
| 18/11/2026 | Se borran de los datos los conceptos sin importe ("Consultar") de Clínica Areas | Decisión del autor: la ficha solo muestra precios publicados |
| 19/11/2026 | Historia 2.1: la comprobación en un Android real (notificación fija, 2 min con la pantalla apagada y parada) queda pendiente; la historia se cierra con la lógica probada en EditMode y la APK compilada con el manifiesto correcto | Decisión del autor |
| 18/11/2026 | Especificaciones de todas las historias pendientes escritas por adelantado (1.7 parte 2 y épicas 2 a 9, 46 ficheros `spec-*.md` en `_bmad-output/implementation-artifacts/`), en borrador y con las decisiones abiertas y una opción recomendada en cada una | Agilizar la implementación: el autor revisa las decisiones abiertas de una vez en lugar de historia a historia |
| 20/11/2026 | Historia 2.6 partida en dos: la parte 1 calcula las rutas (Valhalla `shortest`, OSRM de `routing.openstreetmap.de` de reserva) y llena la fila de la ficha con coche, a pie e Ir; dibujar la ruta es la parte 2. Sin posición, «—» e Ir hace lo del botón de ubicación | Cada parte deja algo visible; el servidor de demostración de OSRM solo tiene coche |
| 20/11/2026 | Historia 2.6 parte 2a: Ir cierra la ficha y dibuja la ruta cubriendo la calle (9 m, nunca menos de 6 dp), la encuadra entre una tarjeta de maniobras arriba y una de llegada, minutos y distancia abajo; sin píldora, tema ni cápsula mientras hay ruta. La parte sin conexión pasa a la 2b. Valhalla se pide con `directions_type: instructions` | Maqueta v2 aprobada; con `maneuvers` Valhalla no da el texto de las maniobras |
| 21/11/2026 | Historia 2.6 parte 2b: sin conexión, ruta aproximada por las calles de las teselas guardadas (grafo con A*, sin sentidos únicos), línea en un azul más suave, «≈» en los valores y toast «Sin conexión: ruta aproximada»; sin calles que unan, el fallo de siempre con el toast «Sin conexión: no hay calles guardadas para esta ruta»; la ficha sin red también da «≈ N min» | La recta atraviesa edificios; maqueta aprobada el 21/11/2026 |
| 21/11/2026 | Historia 2.7 partida en dos: la parte 1 sigue la ruta (maniobra siguiente con «En N m», tiempo y distancia que quedan, línea recortada), recalcula tras 5 s a más de 40 m (la vieja se ve hasta la nueva; si falla, toast y reintento a los 10 s), enciende el GPS de fondo con su notificación y lanza los eventos para las visitas; la cámara que sigue, la flecha y «Buscando señal GPS» van a la parte 2. La navegación empieza al pulsar Ir | Spec de unos 2.800 tokens; cada parte deja algo que se puede probar |
| 22/11/2026 | Historia 2.7 parte 2 partida otra vez: la 2.7b es la cámara que sigue (45°, rumbo de la marcha en coche y brújula a pie, a los 2 s de Ir o al instante con «reducir animaciones»), soltarla al mover el mapa y la pantalla encendida; la flecha y «Buscando señal GPS» van a la 2.7c. «Volver a centrar» es el botón redondo de ubicación, que cambia de icono al soltar la cámara | Spec de unos 2.600 tokens; el sitio del botón lo eligió el usuario entre una píldora con texto y el botón redondo |
| 22/11/2026 | Historia 2.7b: navegando en coche y a 40 m o menos de la ruta, el punto de posición (y la cámara) van sobre la línea de la ruta y no donde dice el GPS; a pie, siempre la posición real | El GPS tiene 5–15 m de error y el punto bailaba sobre aceras y edificios; a pie se va por aceras y plazas, no por el centro de la calle |
| 22/11/2026 | Historia 2.7b (implementación): la cámara persigue su objetivo con un suavizado exponencial (al 95 % en 0,6 s) en vez de una animación de duración fija; la posición va al 75 % de la zona libre entre las tarjetas (dentro del tercio de abajo); un gesto durante los 2 s de espera también suelta la cámara; con la ficha reabierta la cámara no sigue | Con posiciones cada segundo, una animación fija de 0,6 s arranca y para en cada una; el suavizado vale para la transición y para seguir sin saltos. El 75 % deja ver la ruta delante sin pegar el punto a la tarjeta de abajo |
| 22/11/2026 | Historia 2.7c: la flecha de posición sale solo navegando en coche (a pie y sin ruta, el círculo con el cono); sustituye lo del 21/11 («también a pie»). Sin lecturas del GPS 10 s navegando, la posición se atenúa y sale una pastilla pequeña «Buscando señal GPS» bajo la tarjeta de arriba | Decisión del usuario; el aviso no debe ocupar mucho sitio |
| 22/11/2026 | Historia 2.8b: con el móvil bloqueado la navegación debe hacer lo mismo que delante; navegando, Unity no se pausa (actividad Activity propia en vez de GameActivity, 5 fps sin dibujar, procesador despierto con `WAKE_LOCK`). Si en el móvil no funciona, respaldo: la línea de la ruta al servicio de Android | Decisión del usuario: «tiempo real, no debe cambiar si el móvil está bloqueado»; GameActivity pausa Unity desde su parte nativa sin forma de evitarlo |
| 22/11/2026 | Historia 2.8 (diseño y revisión): el selector de voz es una fila de tres iconos sin texto (volumen, volumen con «!», volumen tachado) en vez de un menú con nombres; fuera de la ruta la voz calla hasta el recálculo | Decisión del usuario para que ocupe menos; la distancia fuera de la ruta se mide sobre la ruta vieja y diría giros que ya no tocan |
| 22/11/2026 | Historia 2.8: se parte en dos (con la app delante; con la pantalla apagada, 2.8b). Tres modos de voz: «todo» (siguiente al pasar una maniobra, aviso a 300 m/50 m y en el momento), «lo importante» (sin el de 300 m) y «nada»; se eligen con un botón en la columna de cristal que abre un menú de tres opciones | Decisión del usuario: poder elegir cuánto habla la app |
| 22/11/2026 | Historia 2.7c (revisión): el GPS de Unity pide lecturas sin distancia mínima (antes 2 m), como el servicio de ruta | Sin el servicio (notificaciones denegadas), un coche parado en un semáforo no recibía lecturas y salía «Buscando señal GPS» |
| 22/11/2026 | Historia 2.7c (implementación): la señal se mide con un contador de lecturas del GPS en `IUbicacion` (miembro por defecto) y no con los cambios de posición; sin señal también se cancela el reintento pendiente de un recálculo; si sale un toast con la pastilla a la vista, va debajo de ella | Un coche parado da lecturas con la misma coordenada y no debe salir el aviso; recalcular con la última posición vieja daría una ruta equivocada; así no se tapan |
| 22/11/2026 | Icono de la app: el logo del pin azul con la moneda del euro sobre un mapa (dado por el usuario). En Android 8+ va como icono adaptativo: el dibujo entero, sin sus esquinas redondeadas, en la capa de fondo dentro de la zona visible (72 de 108 dp) y la capa delantera vacía; el clásico (y el del PC) es el logo tal cual | Decisión del usuario; cada móvil recorta el icono con su forma (círculo, squircle de MIUI…) y así el pin sale entero en todas |
| 22/11/2026 | Arreglo de red: en Android las rutas (Valhalla, OSRM), las direcciones (Photon) y los enlaces cortos van por la red del sistema (`HttpURLConnection`, puente Java en un hilo aparte) y no por `UnityWebRequest`; en el PC y el editor, igual que antes. Probado en el Redmi 10: ruta real y direcciones, sin errores SSL. Android 8–9 sigue sin TLS 1.3 (aplazado) | Los servidores de FOSSGIS solo aceptan TLS 1.3 y la red de Unity en Android solo habla TLS 1.2: en el móvil no salían rutas |
| 22/11/2026 | Navegando, la hoja de abajo solo informa (llegada, minutos, km) y no lleva X; se despliega tocándola o arrastrándola y entonces enseña el destino (solo informa, no reabre la ficha) y un botón rojo «Finalizar ruta» («Quitar ruta» sin navegación). Atrás la pliega antes de quitar la ruta | Decisión del usuario tras probar en el móvil (modelo: Apple Maps): tocar la tarjeta reabría la ficha y parecía que la ruta se cancelaba |
| 22/11/2026 | La hoja del buscador ya no se queda donde se suelta (sustituye a la decisión del 19/11): va siempre a cerrada, mitad o completa; soltando despacio, a la más cercana (cortes en 25 % y 75 %); un lanzamiento, a la parada siguiente desde donde está, como Apple Maps | Decisión del usuario tras probar en el móvil |
| 23/11/2026 | El cursor de la posición es más grande (sustituye a los 18 dp del círculo y los 26 dp de la flecha): a pie y sin ruta, círculo de 24 dp con borde de 4 dp y cono de 100 dp; en coche, flecha de 30 dp sin borde sobre un disco blanco de 48 dp con sombra suave, que no gira y se atenúa con ella sin señal | Decisión del usuario tras probar en el móvil: los dos se veían muy pequeños (modelo: Apple Maps; opción B de la maqueta) |
| 23/11/2026 | Elegir en el buscador una dirección o un punto del mapa de OpenStreetMap abre la ficha de punto libre con sus minutos e Ir, ya con sus datos y sin preguntar a Photon. Una calle: «Calle Ancha, 10» y la localidad. Un sitio con nombre propio y calle: el nombre y debajo calle, número y localidad. Un punto del mapa: su nombre y su categoría en el sitio de «Punto en el mapa». Sustituye a lo del 19/11 (solo centraba el mapa) | Decisión del usuario tras probar en el móvil: buscaba una calle y no podía ir a ella |
| 23/11/2026 | Al abrir la app el mapa se centra en la posición del usuario (la primera del GPS y las de los 10 s siguientes), salvo que antes mueva el mapa, use el zoom o abra una ficha. Sin permiso o sin señal, la vista inicial (Plaza del Cabildo). La última posición no se guarda en el móvil | Petición del usuario; la primera lectura del móvil puede ser antigua o poco precisa, por eso los 10 s |
| 23/11/2026 | Al abrir, la vista empieza en la última ubicación que recuerda Android (la app no guarda ninguna) en vez de en la Plaza del Cabildo; el GPS la afina | Decisión del usuario: que se abra directamente donde está |
| 23/11/2026 | Navegando en coche sin señal se atenúa solo el disco (al 70 %) y la flecha va en el azul oscuro en los dos temas; sustituye a «el disco y la flecha juntos al 40 %» | Decisión del usuario: en oscuro la flecha no se distinguía; con el disco al 40 % el contraste era 1,0:1 y así ronda 3:1 |
| 23/11/2026 | «Cómo llegar»: la ruta elegida por defecto es la más rápida (antes, la más corta), también en los minutos de la ficha y en el recálculo. La historia se parte en dos (5a, pedir varias rutas; 5b, la hoja) | Decisión del usuario: de Sanlúcar a Chipiona la más corta son 9,7 km en 47 min y otra de 10,3 km, 22 min |
| 23/11/2026 | Parte 5a de «Cómo llegar» (implementación): el servicio de rutas da hasta tres rutas por tiempo y sin repetidas (margen del 2 %). Valhalla, con dos alternativas y, en coche, si hay menos de tres distintas, una segunda petición detrás con la más corta (tope de 7 s; si falla, valen las primeras); OSRM y la aproximada, de reserva. A pie sigue la más corta | Con `shortest: false` Valhalla suele dar una sola ruta entre Sanlúcar y Chipiona; así la más corta sigue saliendo sin peticiones a la vez a FOSSGIS |
| 23/11/2026 | Parte 5b de «Cómo llegar» (implementación): «Ir» abre la hoja con hasta tres rutas, coche o a pie y un «Iniciar» en cada una; se elige con la tarjeta o tocando su línea (24 dp) y el mapa las encuadra todas con sus minutos. Al iniciar, la cámara va al cursor (0,9 s; sin animaciones, al momento) y después salen las tarjetas y la voz. La ficha sigue abierta y tapada debajo de la hoja. Mientras se elige, las líneas van por encima de los edificios y el destino se ve aunque el filtro no lo incluya | En la captura, con la vista inclinada, los edificios tapaban casi toda la ruta elegida; con las líneas encima se ven enteras, como en la maqueta A |
| 23/11/2026 | Parte 5b de «Cómo llegar» (revisión de código): 16 arreglos (9 de ellos, pruebas que faltaban) y 1258 pruebas en verde. Cada «Iniciar» se lee en TalkBack con su ruta, un modo dejado a medias no se queda en «…», el cursor ya es la flecha mientras la cámara se acerca y el encuadre incluye al usuario y al destino | La revisión encontró que tres botones «Iniciar» iguales no dicen a quien usa TalkBack qué ruta empieza, y que el doble del mapa no distinguía «dejar de elegir» de «una sola ruta» |
| 23/11/2026 | El buscador busca también en los nombres de calle de las teselas guardadas del mapa, sin red: salen al escribir, debajo de los lugares y puntos y encima de las direcciones de Photon, una fila por calle y localidad. Con número («calle fariñas 12») sale la calle sin él y lleva al medio de su trazado; se entienden «c/», «avda» y otras abreviaturas. Si Photon da la misma calle a menos de 1 km, queda la guardada | Photon no encuentra calles de Sanlúcar que sí están en el mapa y sin conexión no había ninguna. El punto se calcula cosiendo los tramos de las teselas, para que nunca caiga fuera de la calle. Las 59 teselas del PC se leen en unos 20 ms, en otro hilo |
| 23/11/2026 | Calles guardadas (revisión de código): 13 arreglos y 1379 pruebas en verde. «No hay resultados» ya no parpadea mientras se busca, se quitan más formas de portal («nº12», «12 bajo») y vale escribir también la localidad («fariñas sanlúcar») | La búsqueda va en otro hilo: con la lista vacía se anunciaba «No hay resultados» un momento antes de la calle |
| 23/11/2026 | Historia 2.9 (decisiones del usuario): «Has llegado» en la tarjeta de arriba y solo el botón «Finalizar ruta» abajo (maqueta B); se llega a 30 m del destino o del final de la línea; la voz dice «Has llegado a…»; el botón atrás nunca termina la ruta, solo «Finalizar ruta» | Evitar cortar la ruta sin querer y que un destino apartado de la calle también termine |
| 23/11/2026 | Historia 2.9 implementada y revisada (11 arreglos, 1445 pruebas en verde, APK bien): al llegar se quitan la línea y las maniobras, sale «Has llegado», la voz lo dice una vez y la ruta se cierra sola al minuto. El servicio de navegación distingue por qué termina la ruta (a mano, al llegar o sola), que usará la épica 4. Con ella quedan hechas todas las historias de la épica 2, a falta de la prueba en el móvil | La revisión encontró que ninguna prueba protegía que la voz callase las maniobras tras llegar, y que un recálculo guardado podía dejar la notificación en «Navegando a…» |
| 23/11/2026 | Pantalla de inicio (decisiones del usuario): fuera el logo de Unity; animación A (el pin cae, rebota y flota; al acabar se encoge hasta la posición del usuario y el mapa se acerca), también en oscuro; se queda hasta tener la posición y el mapa de la vista, como mucho 4 s y nunca menos de 1 s; sin permiso, apagada o con algo compartido no espera a la posición | Al abrir se veía el logo de Unity, un momento en negro y el mapa sin centrar ni cargar |
| 23/11/2026 | Pantalla de inicio implementada (1477 pruebas en verde, APK bien; falta la prueba en el móvil): es una vista nativa de Android encima de Unity desde `onCreate`, porque el motor tarda 2–3 s y no puede animar; el arranque del sistema y el fondo de la ventana van del mismo color y sin icono. C# solo decide cuándo se va (`ServicioInicio`) y hacia dónde | La actividad pasa a dibujarse con aceleración gráfica (Unity la quitaba) para que la animación sea fluida. En Android 8–11 el color del arranque sigue al sistema, no al tema guardado: no hay forma de cambiarlo antes de abrir |
| 23/11/2026 | Pantalla de inicio (revisión de código): 18 arreglos (9 de ellos pruebas que faltaban), 1488 pruebas en verde, APK y captura de PC bien | Entre ellos: TalkBack podía perder el foco al irse la pantalla, el destino del pin se calculaba con el mapa ya alejado y, si Unity cambia sus recursos, ahora la compilación falla en vez de volver el gris de Unity sin avisar |
| 24/11/2026 | Historia 3.1 (decisiones del usuario): bienvenida de tres pasos (maqueta A), aviso completo dentro de la app con nombre y correo del responsable, «Empezar» siempre activo | Consentimiento explícito sin obligar a contestar: no se pide ningún permiso que no se acepte |
| 24/11/2026 | Historia 3.1 implementada y revisada (12 arreglos, 1540 pruebas en verde, APK y capturas bien) | La revisión encontró que con toques rápidos se podía pasar la bienvenida sin leerla, que el aviso no decía que los puntos marcados en el mapa se envían a Photon y que la copia de seguridad de Android podía dar el aviso por visto al reinstalar |
| 24/11/2026 | Historia 3.2 partida en dos (decisión del usuario): reglas de seguridad y Firebase dentro de la app. La parte 1, implementada y revisada (12 arreglos, 22 pruebas de reglas en verde) | Son entregas independientes; las reglas son la frontera de seguridad y se prueban con el emulador sin tocar el proyecto real |
| 24/11/2026 | Historia 3.2 parte 2 implementada y revisada (16 arreglos, 1601 pruebas en verde, APK y captura de PC bien): Firebase Unity SDK 14.1.0 detrás de un adaptador, inicio sin esperar, paquetes fuera de git con script y SHA-256, Analytics desactivado | La app debe funcionar igual sin servidor, y el repositorio público no puede llevar la configuración. La revisión vio que un dato con cambios sin enviar se daba por confirmado y que la prueba de capas dejaba pasar un ensamblado sin `overrideReferences` |
