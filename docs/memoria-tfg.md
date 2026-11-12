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

### 4.7 Gamificación **[Pendiente]**
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

### 4.10 Alcance del prototipo **[Parcial]**
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

- **5.1 Elección de tecnología y prueba técnica del mapa:** opciones valoradas (Unity + Mapbox SDK frente a una app fuera de Unity con Mapbox GL o MapLibre), criterios y resultado.
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
