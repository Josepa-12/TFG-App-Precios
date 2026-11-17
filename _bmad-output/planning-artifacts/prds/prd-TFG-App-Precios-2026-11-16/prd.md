---
title: PRD — PriceMap (app final)
status: final
created: 2026-11-16
updated: 2026-11-16
---

# PRD: PriceMap
*Nombre provisional — confirmar.*

## 0. Propósito del documento

Este PRD define la **app móvil final** de PriceMap que se construye para el TFG (entrega: 11/12/2026). Es la fuente de requisitos para crear las épicas e historias (`bmad-create-epics-and-stories`) y, si se hacen, la arquitectura y el diseño UX.

Parte de un **prototipo web navegable ya construido y validado** (`prototipo/index.html`), descrito en el documento *"PriceMap — Todo lo realizado en el prototipo web"* y en `docs/memoria-tfg.md` (cap. 4). Este PRD no repite el prototipo: lo convierte en requisitos para la app real. Lo que el prototipo **simula** (inicio con Google, verificación del negocio, GPS, pregunta una hora después con la app cerrada, guardado en la nube) aquí debe funcionar de verdad.

Estructura: el vocabulario se fija en el Glosario (§3) y se usa igual en todo el documento; las funciones se agrupan en §4 con requisitos (FR) numerados de forma global; las suposiciones confirmadas se recogen en §11. Las decisiones técnicas están en `addendum.md`.

**Decisiones que condicionan todo el documento:**
- **Plataforma:** app nativa para Android construida con Unity; iPhone queda para la v2.
- **Alcance:** el MVP incluye todas las funciones de §4, sin priorizar ni aplazar ninguna.
- **Gamificación:** es, junto a los precios, la novedad de la app, así que la usan todos los participantes de las pruebas; no hay grupo sin ella.

## 1. Visión

Antes de ir a un sitio casi nunca se sabe cuánto va a costar: una sesión de fisioterapia, una pista de pádel o una cena. Los mapas habituales dicen cómo llegar, el horario y las reseñas, pero no el precio, o solo un "€€" vago. Para saberlo hay que llamar, buscar en la web del negocio o ir y descubrirlo allí.

PriceMap es una app de mapa y navegación en la que el dato principal de cada sitio es **cuánto cuesta**. Los precios se mantienen entre todos: cuando alguien llega a un sitio navegando con la app, una hora después se le pregunta, con un toque, si el precio mostrado es correcto o cuánto pagó. Las aportaciones se premian con monedas, niveles e insignias, y las monedas se canjean por cosméticos que se ven en el propio mapa.

El poder está en los clientes: el negocio puede declarar sus precios, pero si los clientes que han ido pagan otra cosa, manda lo que pagan los clientes. Y ningún precio se inventa: cada precio dice de dónde sale.

## 2. Usuario objetivo

### 2.1 Trabajos que resuelve (JTBD)

- **Funcional:** saber cuánto me va a costar un servicio local antes de ir, y llegar hasta allí.
- **Funcional:** comparar sitios del mismo tipo cerca de mí por precio.
- **Funcional:** calcular cuánto nos va a costar una tarde con varias paradas para un grupo.
- **Emocional:** ir tranquilo, sin la incomodidad de preguntar el precio o de llevarme una sorpresa al pagar.
- **Social:** ayudar a mis vecinos con lo que yo ya sé y que se me reconozca (nivel, insignias).
- **Para el negocio:** mostrar mis precios reales y mis ofertas a quien está buscando cerca.

### 2.2 Fuera del público de la v1

- Quien compara precios de productos de catálogo (supermercados, gasolineras): ya hay comparadores.
- Quien busca alojamiento: el precio depende de la fecha y lo cubren otras plataformas.
- Quien necesita servicios a presupuesto (reformas): no hay un precio comparable.
- Usuarios de iPhone: la v1 es solo Android (ver §8.2).

### 2.3 Recorridos de usuario clave

- **UJ-1. Lucía descubre cuánto cuesta el fisio antes de pedir cita.**
  Lucía, 24 años, tiene una contractura y no sabe cuánto cobran los fisios de Sanlúcar. Abre la app sin cuenta; el mapa sale limpio, sin precios. En el desplegable "Precios" elige *Servicios locales* y ve los logos de las clínicas cerca; las que tienen precio llevan la insignia €. Toca Clínica Areas y la ficha le muestra "Sesión de fisioterapia · desde 40 €", con el origen "publicado en su web" y la fecha, y un punto verde de frescura. Compara con FisioSanlúcar y pulsa **Ir**. **Caso límite:** si una clínica no tiene precio, la ficha lo dice y le ofrece "Avísame cuando tenga precio".

- **UJ-2. Andrés aporta el primer precio de ChemaBarber una hora después de cortarse el pelo.**
  Andrés, 31 años, tiene cuenta con Google. ChemaBarber aún no tiene precio en la app. Navega a pie hasta allí con la app; el cono de la brújula le orienta. Al llegar pulsa "Terminar ruta". Una hora después, con la app cerrada, le llega una notificación: "¿Has ido a ChemaBarber?". Responde sí → "¿Qué te hiciste?" y elige el concepto sugerido "Corte de pelo" → marca lo que pagó en la regla. Es el primero en aportarlo: gana 25 monedas y 30 XP con "¡Pionero!", y su anillo de nivel avanza. Desde ese momento, la ficha de ChemaBarber muestra el precio "según 1 cliente". **Caso límite:** si estaba conduciendo cuando tocaba preguntar, la visita queda en pendientes y la retoma con el botón €.

- **UJ-3. Carmen, dueña de un bar, publica sus precios y una oferta.**
  Carmen crea una cuenta de negocio con Google, busca su bar en el mapa y lo reclama; confirma el enlace que le llega al correo del local y queda verificado. Declara el gasto medio por persona y publica "Tapa + caña 3 € hasta el domingo". Su marcador muestra una pequeña "%". Semanas después, su perfil le dice que tres clientes han confirmado un rango algo distinto y que ahora se muestra el de los clientes. **Caso límite:** si su bar no aparece en el mapa, lo da de alta a mano con nombre, tipo, ubicación y precios.

- **UJ-4. Pablo planea la tarde del sábado para cuatro amigos.**
  Pablo añade al plan una pista de pádel, unas tapas y un bar de copas. Pone 4 personas. La app le da el gasto estimado: pádel una vez para el grupo, tapas y copas en rango por persona, y le avisa de que el bar de copas aún no tiene precio. Pulsa "Empezar ruta" y la app le lleva parada a parada.

- **UJ-5. Rosa avisa de que un precio ya no es correcto.**
  Rosa, usuaria "Fiable", ve que la peluquería de su barrio sigue mostrando 10 € cuando ya cobra 12 €. Pulsa "¿Algún precio está mal?" → "El precio ha cambiado". Con su aviso y el de otro vecino, el precio pasa a "en revisión" y la app pide al siguiente cliente que lo confirme.

## 3. Glosario

- **Usuario** — Cualquier persona que usa la app, con o sin cuenta. Si tiene cuenta, es un *Cliente* o un *Negocio*.
- **Lugar** — Sitio del mapa con ficha propia. Puede tener precios o no. Viene de OpenStreetMap o lo da de alta un negocio.
- **Lugar con precios** — Lugar de una de las tres plantillas que admite precios.
- **Punto de interés** — Comercio o servicio real de OpenStreetMap que se muestra en el mapa solo como referencia (supermercado, farmacia, banco…), sin precios.
- **Plantilla** — Tipo de precio de un lugar: *Servicio local*, *Hostelería* u *Ocio*. Fija sus conceptos, forma de precio, unidad y si admite valoración.
- **Concepto** — Cada cosa que cobra un lugar (una sesión, una pista, el gasto por persona). Un lugar tiene uno o varios.
- **Precio** — Valor de un concepto. Forma: *fijo*, *rango* o *desde*. Tiene un origen y una frescura.
- **Variante** — Condición que cambia el precio de un mismo concepto: horario (p. ej. pista con luz, noche), día (laborable, fin de semana) o tipo de cliente (socio, estudiante). Un concepto tiene un precio base y cero o más variantes, cada una con su propio precio.
- **Origen del precio** — De dónde sale el precio mostrado: *web del negocio*, *declarado por el negocio* o *según N clientes*.
- **Aporte** — Precio que un cliente dice haber pagado tras una visita verificada.
- **Confirmación** — Aporte que coincide con el precio mostrado.
- **Visita verificada** — Ruta hacia un lugar con precios iniciada con la app en la que el tiempo restante llegó a 5 minutos o menos. Es la única vía para aportar.
- **Visita pendiente** — Visita verificada cuya pregunta se ha dejado para más tarde.
- **Pregunta de llegada** — Secuencia "¿Has ido?" → "¿Qué usaste?" → "¿Te costó X?" → valoración → recompensa.
- **Frescura** — Antigüedad de la última confirmación de un precio: *al día*, *podría haber cambiado* o *antiguo*.
- **Aviso de precio incorrecto** — Informe de un cliente de que un precio cambió, el servicio ya no se ofrece o el negocio cerró.
- **En revisión** — Estado de un precio con suficientes avisos ponderados; la pregunta de llegada pide confirmarlo.
- **Reputación** — Nivel de fiabilidad del cliente (*Nueva*, *Fiable*, *Experta*) que pondera sus avisos.
- **Cliente** — Cuenta que descubre, aporta y gana recompensas.
- **Negocio** — Cuenta que gestiona los precios y ofertas de un único lugar propio.
- **Oferta** — Promoción temporal publicada por un negocio, con fecha de fin.
- **Plan** — Lista ordenada de paradas (lugares) con número de personas y gasto estimado.
- **Monedas / XP / Nivel** — Recompensas: las monedas se gastan en la tienda; la XP sube el nivel (150 XP por nivel).
- **Cosmético** — Aspecto canjeable del marcador de posición o de la línea de la ruta.
- **Insignia** — Logro por hitos de aportación.

## 4. Funciones

### 4.1 Mapa y descubrimiento
**Descripción:** Pantalla principal a pantalla completa con el mapa real de la ciudad en 2.5D (cámara inclinada, edificios en volumen) y la paleta del proyecto (B en claro, D en oscuro). Por defecto no muestra precios, para que quede limpia. Realiza UJ-1.

#### FR-1: Mapa real en 2.5D con la paleta propia
El usuario ve un mapa real de su zona con calles, agua, zonas verdes, edificios en volumen y nombres de calles, en los colores de la paleta.
- Los colores de cada capa son los de la paleta del tema activo, sin iconos ajenos a la app.
- Se puede mover, hacer zoom, girar e inclinar con gestos de uno y dos dedos.
- Los datos del mapa ya vistos se guardan en caché y se muestran sin conexión.

#### FR-2: Filtro de precios
El usuario elige en el desplegable "Precios" qué lugares con precios ver: *Ninguno* (por defecto), *Todos*, *Servicios locales*, *Hostelería* u *Ocio*.
- Con *Ninguno*, el control dice "Ver precios" y no hay marcadores de precio salvo el lugar abierto o el destino actual.

#### FR-3: Marcadores de lugares con precios
Cada lugar con precios se muestra con el logo de su tipo y el color de su plantilla (azul servicio local, naranja hostelería, verde ocio).
- Lleva insignia € si tiene precio; sin precio se ve más discreto.
- Lleva una etiqueta "%" mínima si tiene una oferta activa.
- El marcador se mantiene en su coordenada al mover, girar e inclinar el mapa (desvío < 1 px).

#### FR-4: Puntos de interés de referencia
El mapa muestra puntos de interés reales de OpenStreetMap (supermercados, farmacias, gasolineras, bancos…) con icono y nombre, distinguibles de los lugares con precios. Se ocultan las paradas de autobús y las oficinas.

#### FR-5: Posición del usuario
El mapa muestra la posición real del usuario. Fuera de la navegación es un círculo y, si el dispositivo tiene brújula, lleva un cono de orientación. Un control centra el mapa en la posición.

#### FR-6: Tema claro y oscuro
El usuario cambia entre tema claro (paleta B) y oscuro (paleta D); el mapa y la interfaz cambian a la vez.

### 4.2 Ficha del lugar y modelo de precios
**Descripción:** Un lugar es una lista de conceptos con su precio. Cada plantilla fija los valores por defecto, de modo que añadir un sector es añadir una plantilla. Realiza UJ-1.

| Plantilla | Conceptos | Forma | Unidad | Valoración |
|---|---|---|---|---|
| Servicio local | Normalmente 1 | Fijo | Sesión o servicio | No |
| Hostelería | 1: gasto por persona | Rango | Persona | Sí: calidad, servicio, ambiente |
| Ocio | Varios | Fijo | Espacio total o por persona | Sí: instalaciones, atención, calidad-precio |

#### FR-7: Ficha del lugar
Al tocar un lugar se abre su ficha con nombre, tipo, dirección, conceptos y precios, y justo bajo la cabecera la elección coche / a pie con su tiempo estimado y el botón **Ir**, visibles sin desplazar.

#### FR-8: Precios por plantilla
La ficha muestra cada precio según su forma: fijo, rango por persona (hostelería) o "desde X €"; los conceptos se agrupan por área si son muchos y admiten "A consultar".
- En ocio, el alquiler de un espacio muestra el precio total, y las actividades por participante muestran el precio por persona.

#### FR-9: Origen y frescura visibles
Cada precio muestra su origen (*web del negocio* con enlace y fecha, *declarado por el negocio* o *según N clientes*) y un punto de frescura con un resumen ("Al día · confirmado hace 3 días").

#### FR-10: Acciones de la ficha
Desde la ficha el usuario puede marcar como favorito, activar "Avísame si cambia" (o "cuando tenga precio"), añadir al plan, avisar de un precio incorrecto e ir. En lugares sin cuenta de negocio aparece "¿Es tu negocio?".

#### FR-11: Lugares reales sin precios inventados
La app incluye los lugares reales de la ciudad desde OpenStreetMap. Un lugar sin precio lo muestra como tal; ningún precio se rellena por defecto. Los centros sanitarios públicos aparecen sin precios.

#### FR-12: Notificación de cambio de precio
Quien activó "Avísame si cambia" recibe un aviso cuando el precio mostrado de ese lugar cambia o pasa a tener precio. La comprobación se hace al abrir la app y al volver a ella; el aviso es una notificación local (sin servidor, ver arquitectura AD-7).

#### FR-60: Variantes del precio
Un concepto puede tener variantes por horario, día o tipo de cliente (socio, estudiante), cada una con su precio.
- La ficha muestra el precio base y, debajo, cada variante con su condición ("Con luz: 16 €", "Fin de semana: 14 €", "Socios: 10 €").
- Si una variante coincide con el momento actual (por ejemplo, es fin de semana), la ficha la destaca.
- En la pregunta de llegada, "¿Qué usaste?" incluye las variantes del concepto, y el aporte cuenta para la variante elegida.
- El negocio declara variantes al editar sus precios (FR-45), y las reglas del precio (FR-31, FR-32) y la frescura (FR-33) se aplican a cada variante por separado.

### 4.3 Búsqueda y destinos
**Descripción:** El buscador está en una cápsula inferior que se despliega hacia arriba arrastrándola. El GPS lleva a cualquier sitio, no solo a lugares con precios.

#### FR-13: Buscador desplegable
La hoja del buscador sigue al dedo, se queda donde se suelta, se abre o cierra del todo con un lanzamiento rápido y permite buscar a media altura. Antes de escribir muestra favoritos, categorías y lugares frecuentes.

#### FR-14: Búsqueda de lugares y direcciones
Al escribir, el buscador encuentra lugares de la app, puntos de interés y direcciones, priorizando los resultados cercanos.

#### FR-15: Encuentra cerca
El usuario elige una categoría (restaurantes, farmacias, gimnasios…) y ve en el mapa y en una lista por distancia solo esa categoría, primero los lugares con precios.

#### FR-16: Destino desde ubicación compartida
El usuario puede abrir con la app una ubicación compartida por WhatsApp, Google Maps o Apple Maps (incluidos enlaces cortos), un enlace `geo:` o unas coordenadas, y navegar hasta ese punto.

#### FR-17: Destino libre y favoritos sin precio
Manteniendo pulsado el mapa se marca un punto con su dirección. Direcciones y puntos se pueden guardar como favoritos con nombre ("Casa de mamá").
- Las direcciones a las que se navega no se guardan en el historial ni generan pregunta de llegada.

### 4.4 Navegación GPS
**Descripción:** Navegación real giro a giro con el GPS del móvil. Realiza UJ-2 y UJ-4.

#### FR-18: Cálculo de ruta
La app calcula la ruta más corta por calles reales; en coche respeta el sentido de circulación. Empieza y acaba en el punto de calle más cercano.

#### FR-19: Navegación giro a giro
Durante la ruta la app sigue la posición real, muestra la siguiente maniobra en castellano y el tiempo estimado, y recalcula la ruta si la posición se aleja más de 40 m de ella durante más de 5 s.

#### FR-20: Indicador según el modo
En coche, la posición es una flecha que apunta al rumbo; a pie, un círculo con cono que sigue la brújula.

#### FR-21: Indicaciones por voz
Si están activadas, las maniobras se leen en voz alta en castellano.

#### FR-22: Llegada y visita verificada
Al llegar (posición a 30 m o menos del lugar) se muestra "Has llegado" con "Terminar ruta"; si no se pulsa, la ruta se cierra sola al minuto.
- Una ruta hacia un lugar con precios iniciada con la app crea una visita verificada en cuanto el tiempo restante estimado llega a 5 minutos o menos.
- La visita se mantiene aunque después el usuario termine la ruta antes de llegar o cierre la app.
- Una ruta terminada con más de 5 minutos restantes no crea visita.
- Cada ruta crea como máximo una visita verificada.

### 4.5 Pregunta de llegada y aportes
**Descripción:** Es el núcleo colaborativo de la app: solo pregunta a quien llegó navegando con la app, una hora después, para que haya recibido el servicio. Realiza UJ-2.

#### FR-23: Pregunta una hora después, con la app cerrada
Una hora después de crearse una visita verificada (FR-22), el cliente recibe una notificación que abre la pregunta de llegada, aunque la app esté cerrada.
- Si en ese momento hay una ruta en curso u otra pregunta abierta, la visita pasa a pendientes sin interrumpir.

#### FR-24: Flujo de la pregunta
La pregunta sigue: "¿Has ido?" → "¿Qué usaste?" (solo con varios conceptos) → "¿Te costó X €?" → valoración opcional (hostelería y ocio) → recompensa. Cada paso tiene "Atrás" y "Más tarde".

#### FR-25: Introducir otro precio
Si el precio no coincide, el cliente lo indica con una regla deslizable de 1 € por marca, escribiéndolo o eligiendo uno ya aportado por otros. Acepta céntimos y no tiene tope; no acepta 0 ni negativos.

#### FR-26: Tramos en hostelería
En hostelería no se propone un precio: el cliente elige un tramo de gasto por persona (menos de 10, 10-20, 20-30, 30-50, más de 50 €), para reducir el sesgo de confirmación.

#### FR-27: Primer precio ("Pionero")
En un lugar sin precio, el primer cliente que llega aporta el precio inicial eligiendo un concepto sugerido o escribiendo otro con su unidad, y recibe la recompensa de pionero.

#### FR-28: Valoración opcional
En hostelería y ocio el cliente puede valorar 3 criterios de 1 a 5 estrellas. Nunca es obligatoria.

#### FR-29: Visitas pendientes y botón €
"Más tarde" guarda la visita como pendiente. El botón € muestra un punto rojo si hay pendientes, retoma la pregunta y, si hay varias, deja elegir. Sin pendientes está desactivado: no se puede aportar sin visita verificada.

#### FR-30: Aportes anónimos en categorías sensibles
En clínicas, fisioterapia, psicología y dentistas el aporte se guarda en la parte pública sin el identificador del cliente; solo cuenta para sus recompensas y su reputación, y nadie más puede saber quién lo hizo.

### 4.6 Reglas del precio y fiabilidad
**Descripción:** Cómo se decide el precio mostrado y cómo se mantiene vivo. Realiza UJ-3 y UJ-5.

#### FR-31: Cálculo del precio mostrado
El precio mostrado se calcula a partir de los aportes recientes: mediana si es fijo, del percentil 25 al 75 si es rango y percentil bajo (10-20) si es "desde".

#### FR-32: Mandan los clientes
Con al menos 3 aportes en los últimos 6 meses, el precio de los clientes sustituye al declarado por el negocio, y el origen pasa a "según N clientes".

#### FR-33: Frescura
Cada precio guarda la fecha de su última confirmación y se clasifica según esta tabla:

| Plantilla | Al día | Podría haber cambiado | Antiguo |
|---|---|---|---|
| Hostelería | < 30 días | 30-60 días | > 60 días |
| Servicio local y ocio | < 3 meses | 3-6 meses | > 6 meses |

#### FR-34: Avisos de precio incorrecto
Un cliente puede avisar de que un precio cambió, el servicio ya no se ofrece o el negocio cerró. Cada aviso pesa según la reputación: Nueva ×1, Fiable ×1,5 (5 aportes), Experta ×2 (20 aportes).

#### FR-35: Estado en revisión
Con 3 puntos de aviso el precio pasa a "en revisión", se marca en la ficha y la pregunta de llegada pide a los siguientes clientes que lo confirmen; se cierra cuando uno lo confirma o aporta otro.

#### FR-36: Datos compartidos entre usuarios
Los aportes, avisos, precios declarados y ofertas se guardan en un servidor y los ven todos los usuarios en menos de 1 minuto.

### 4.7 Cuentas e identidad
**Descripción:** Dos tipos de cuenta con papeles separados, para que ningún negocio aporte precios de la competencia. Realiza UJ-2 y UJ-3.

#### FR-37: Inicio de sesión real
El usuario inicia sesión con Google (opción principal) o con correo; la app no gestiona contraseñas propias. También puede usar la app sin cuenta para mirar.

#### FR-38: Uso sin cuenta
Sin cuenta se puede ver el mapa y las fichas, buscar y navegar; para aportar, ganar recompensas o guardar favoritos se pide crear cuenta.

#### FR-39: Elección del tipo de cuenta
Al registrarse el usuario elige Cliente o Negocio. Una cuenta de negocio no recibe preguntas de llegada, no tiene monedas ni tienda y no aporta precios como cliente.

#### FR-40: Progreso en la nube
El progreso del cliente (monedas, XP, aportes, favoritos, plan, cosméticos, ajustes) se guarda en su cuenta y se recupera en otro dispositivo.

#### FR-41: Aviso de privacidad y consentimiento
La primera vez se muestra el aviso de privacidad y se pide consentimiento explícito para la ubicación y las notificaciones.

#### FR-42: Cerrar sesión y borrar cuenta
El usuario puede cerrar sesión y borrar su cuenta y sus datos personales.

### 4.8 Gestión del negocio
**Descripción:** El negocio reclama su lugar, declara precios y publica ofertas. Realiza UJ-3.

#### FR-43: Reclamar y verificar un lugar
Una cuenta de negocio busca su lugar en el mapa y lo reclama; la propiedad se verifica antes de poder editar: el negocio confirma por enlace el correo de su cuenta y este debe coincidir con el correo público del establecimiento (OpenStreetMap o su web). Si el establecimiento no tiene correo público, lo aprueba manualmente el administrador.

#### FR-44: Alta de un lugar nuevo
Si el lugar no existe, el negocio lo da de alta con nombre, tipo (agrupado por plantilla), ubicación (dirección o posición actual) y precios.

#### FR-45: Declarar y editar precios
El negocio declara y edita los conceptos y precios de su lugar; se muestran como "declarado por el negocio" hasta que manden los clientes (FR-32).

#### FR-46: Ofertas
El negocio publica ofertas con texto, precio opcional (positivo) y fecha de fin. Desaparecen al caducar o cuando 3 clientes avisan de que ya no está vigente.

#### FR-47: Panel del negocio
El perfil del negocio muestra el estado de cada precio (declarado o confirmado por N clientes), los avisos recibidos y los precios en revisión.

### 4.9 Gamificación
**Descripción:** Incentiva las aportaciones sin ensuciar los datos. Realiza UJ-2.

#### FR-48: Recompensas por aportar
Confirmar da +10 monedas y +15 XP; aportar un precio distinto, +15 y +20; ser pionero, +25 y +30; valorar, +5 y +10. Confirmar un precio que podría haber cambiado da +5 monedas extra, y uno antiguo, +10. Son los valores del prototipo y se pueden ajustar tras las pruebas.

#### FR-49: Niveles
Cada 150 XP se sube de nivel. El avatar muestra el nivel con la XP como anillo.

#### FR-50: Tienda de cosméticos
El cliente compra con monedas y equipa cosméticos del marcador de posición (clásica, dorada, neón) y de la ruta (clásica, discontinua, dorada), que se ven al momento en el mapa.

#### FR-51: Insignias
El cliente gana insignias por hitos (primer precio, explorador, 10 y 50 aportes, racha de 7 días, crítico); las no ganadas se ven bloqueadas.

#### FR-52: Reputación
La reputación sube con los aportes (Nueva, Fiable a 5, Experta a 20) y se muestra en el perfil.

#### FR-53: Sonidos de recompensa
La llegada, la pregunta y las monedas tienen sonido, que se puede silenciar desde la burbuja del altavoz.

### 4.10 Plan de tarde
**Descripción:** Planear una salida con varias paradas y su gasto. Realiza UJ-4.

#### FR-54: Crear y ordenar un plan
El cliente añade lugares al plan desde la ficha, los ordena, elige el número de personas (1 por defecto) y qué usará en cada parada.

#### FR-55: Gasto estimado
El plan calcula el gasto total: hostelería como rango × personas, entradas y servicios × personas, pistas y aparcamientos una vez por grupo; indica las paradas sin precio y las de precio "desde".

#### FR-56: Ruta del plan
"Empezar ruta" guía parada a parada: al terminar una, se abre la siguiente.

### 4.11 Perfil y ajustes
**Descripción:** El perfil se abre desde el avatar y reúne la tienda, la actividad y los ajustes agrupados.

#### FR-57: Actividad
El cliente ve sus visitas pendientes, aportes, favoritos, avisos activos e insignias.

#### FR-58: Ajustes agrupados
Ajustes en secciones: Apariencia, Navegación y voz, Notificaciones, Privacidad y ubicación, Accesibilidad, Idioma y Ayuda (cómo se calculan los precios).

### 4.12 Evaluación del TFG
**Descripción:** Datos necesarios para medir las métricas de §9 durante las pruebas con usuarios.

#### FR-59: Registro anónimo para la evaluación
La app registra de forma anónima, con consentimiento, los eventos necesarios para las métricas de §9: visitas verificadas, preguntas respondidas o aplazadas, aportes, confirmaciones, frescura de los precios y uso de la tienda. Los datos se pueden exportar por participante con un identificador anónimo.

## 5. Requisitos no funcionales transversales

- **NFR-1 Rendimiento del mapa:** el mapa se dibuja a ≥ 30 fps en un Android de gama media de 2021 con 4 GB de RAM y la zona visible carga en < 3 s con conexión 4G.
- **NFR-2 Sin conexión:** con la zona en caché, el mapa, las fichas ya vistas y los favoritos funcionan sin conexión; los aportes hechos sin conexión se envían al recuperarla.
- **NFR-3 GPS y batería:** durante una ruta, el GPS sigue funcionando en segundo plano (pantalla bloqueada o con otra app abierta) con una notificación visible de navegación, para que la visita verificada (FR-22) se cree aunque el móvil vaya en el bolsillo. Sin ruta activa, la app nunca usa el GPS en segundo plano.
- **NFR-4 Accesibilidad:** contraste ≥ 4,5:1 en texto (tema claro y oscuro), zonas táctiles ≥ 44 px, respeto a "reducir animaciones" y compatibilidad con TalkBack en el mapa, la ficha, el buscador, la pregunta de llegada y el perfil.
- **NFR-5 Idioma:** textos en castellano; la estructura permite añadir idiomas.
- **NFR-6 Fiabilidad del dato:** ningún precio se muestra sin origen; la validación impide precios menores o iguales a 0 y ofertas con precio negativo.
- **NFR-7 Coste:** la v1 usa servicios gratuitos o con capa gratuita suficiente para el TFG.
- **NFR-8 Seguridad de los datos compartidos:** el servidor solo acepta que un cliente cree aportes, avisos y visitas a su nombre, y que un negocio edite los precios y ofertas de su propio lugar verificado; nadie puede modificar el progreso, las monedas o la reputación de otro usuario. Las reglas se comprueban en el servidor, no solo en la app.

## 6. Restricciones y salvaguardas

### 6.1 Privacidad
- Consentimiento explícito, datos mínimos y ningún seguimiento en segundo plano fuera de una ruta iniciada por el usuario (NFR-3).
- La pregunta de llegada, las direcciones privadas y el anonimato de los aportes siguen FR-17, FR-22 y FR-30.
- Cumplimiento del RGPD: derecho de acceso y borrado (FR-42).

### 6.2 Antifraude
- Solo se aporta tras una visita verificada; un negocio no aporta como cliente.
- Un cliente puede hacer como mucho un aporte por lugar cada 24 h.

### 6.3 Servicios externos
- Los servicios públicos de mapas, rutas y búsqueda tienen políticas de uso moderado; la app usa caché y no hace peticiones masivas.
- Atribución visible de OpenStreetMap y OpenFreeMap en el mapa.

### 6.4 Aspecto y tono
- **Referencia de interfaz:** Mapas de Apple. El mapa ocupa toda la pantalla y los controles flotan encima: cápsula inferior de cristal con el buscador y el avatar, desplegable "Precios" arriba a la izquierda, tema y ubicación arriba a la derecha, botón € pequeño y circular tomado de Waze.
- **Color:** paleta B (azul y amarillo) en claro y D (navegación nocturna) en oscuro. El amarillo se reserva para monedas, logros y el rango de la hostelería; el azul, para acciones, precios y la ruta.
- **Tipografía:** Varela Round para títulos y Nunito Sans para el texto; las dos redondeadas y cercanas.
- **Iconos:** dibujados (SVG o equivalente), nunca emojis.
- **Tono de los textos:** cercano, breve y en castellano llano; la app explica siempre de dónde sale cada precio y nunca culpa al usuario.

## 7. No objetivos

- No es un comparador de productos de catálogo ni de alojamiento.
- No es una plataforma de reseñas: la valoración es secundaria y opcional.
- No reserva citas ni cobra pagos.
- No hace avisos de tráfico tipo Waze ni funciona en Android Auto / CarPlay.
- No tiene motor de rutas propio en la v1.
- No hay moderación manual avanzada ni panel web para negocios.

## 8. Alcance del MVP

### 8.1 Dentro
- App Android con mapa vectorial en la paleta, centrada en Sanlúcar de Barrameda.
- Las tres plantillas con los lugares reales de la ciudad, con variantes del precio.
- Cuentas de cliente y negocio con Google, verificación del negocio, progreso en la nube.
- Navegación GPS real, pregunta de llegada con notificación, aportes y reglas del precio.
- Frescura, avisos de precio incorrecto, ofertas, plan de tarde.
- Gamificación: monedas, niveles, tienda, insignias, reputación.
- Indicaciones por voz.
- Perfil, ajustes, tema claro y oscuro, accesibilidad básica con TalkBack.
- Registro anónimo para la evaluación.

### 8.2 Fuera del MVP
- **iPhone (iOS):** no se puede compilar desde Windows. v2.
- **Otras ciudades:** la arquitectura no lo impide, pero los datos y pruebas son de Sanlúcar. v2.
- **Relieve del terreno:** Sanlúcar es casi plana. v2.

## 9. Métricas de éxito

La gamificación es, junto a los precios, la novedad de la app, así que todos los participantes la usan: no hay grupo sin gamificación. Los datos salen del registro anónimo (FR-59) y de un cuestionario final.

**Primarias**
- **SM-1 Usabilidad:** puntuación SUS media ≥ 68 con 5-20 participantes. Valida FR-1 a FR-30.
- **SM-2 Tasa de aporte:** ≥ 50 % de las visitas verificadas acaban en un aporte o confirmación (incluidas las retomadas desde pendientes). Valida FR-22 a FR-29, FR-48.

**Secundarias**
- **SM-3 Efecto percibido de la gamificación:** ≥ 70 % de los participantes declara en el cuestionario final que las recompensas (monedas, nivel, tienda) le animaron a responder la pregunta de llegada; se contrasta con el uso real de la tienda y de las confirmaciones de precios antiguos. Valida FR-48 a FR-52. Responde a la pregunta de investigación.
- **SM-4 Frescura media:** ≥ 70 % de los precios mostrados "al día" al final de la prueba. Valida FR-33, FR-48.
- **SM-5 Cobertura:** % de lugares con al menos un precio al final de la prueba.

**Contramétricas (no optimizar)**
- **SM-C1 Número bruto de aportes:** no se premia aportar más por aportar; contrarresta SM-2 (riesgo de aportes falsos para ganar monedas).
- **SM-C2 Tiempo en la app:** no es un objetivo; la app debe dar el precio y apartarse. Contrarresta SM-3.

## 10. Preguntas abiertas

1. ¿Las visitas pendientes caducan? ¿A los cuántos días?
2. ¿Cuál es la curva de niveles definitiva y el precio de cada cosmético tras las pruebas?
3. ¿Se mantiene el nombre "PriceMap"?

## 11. Suposiciones confirmadas

Todas confirmadas por el autor el 16/11/2026:
- §4.6 FR-32 — 3 aportes en los últimos 6 meses para que manden los clientes.
- §4.6 FR-36 — los datos compartidos se actualizan en menos de 1 minuto.
- §4.8 FR-43 — la verificación del negocio es por correo del local (enlace) o aprobación manual; sin SMS, por usar el plan gratuito de Firebase (cambiado el 16/11/2026 al hacer la arquitectura).
- §4.9 FR-48 — los valores de recompensa son los del prototipo.
- §5 NFR-1 — móvil de referencia: Android de gama media de 2021 con 4 GB de RAM.
- §6.2 — un aporte por cliente y lugar cada 24 h.
