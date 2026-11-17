# Memoria del TFG — borrador de referencia

> **Qué es este documento.** Es un borrador que Claude va completando conforme avanza el proyecto. Recoge lo que se ha decidido y por qué, para que al final puedas leerlo y redactar **tu propia memoria con tus palabras**. Antes de reutilizar texto, consulta con el tutor la política de uso de IA.
>
> **Estado de cada apartado:** **[Redactado]** contenido estable · **[Parcial]** falta información · **[Pendiente]** aún no se ha llegado ahí.
>
> **Última actualización:** 12/11/2026

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
| 16/11/2026 | Los recorridos de ejemplo no atribuyen precios inventados a negocios reales: en UJ-2, Andrés es el pionero que aporta el primer precio de ChemaBarber, que sigue en la app con su nombre | Mismo criterio que el prototipo: no inventar precios de negocios reales |
