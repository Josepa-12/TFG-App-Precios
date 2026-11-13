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
- **5.2 Arquitectura:** estructura del proyecto y módulos.
- **5.3 Modelo de datos:** implementación del modelo de 4.3.
- **5.4 Lógica de precios:** implementación de 4.6 y simulación.
- **5.5 Gamificación**
- **5.6 Interfaz**
- **5.7 Compilación y despliegue en móvil**
- **5.8 Problemas encontrados y soluciones**

---

## 6. Pruebas y validación **[Pendiente]**

Plan previsto:
- **Participantes:** entre 5 y 20 usuarios. Idealmente, un grupo usa la app con gamificación y otro sin ella.
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
