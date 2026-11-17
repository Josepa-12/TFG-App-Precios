---
status: final
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-TFG-App-Precios-2026-11-16/prd.md
  - _bmad-output/planning-artifacts/prds/prd-TFG-App-Precios-2026-11-16/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/EXPERIENCE.md
---

# TFG-App-Precios (PriceMap) - Desglose en épicas

## Visión general

Este documento recoge el desglose completo en épicas e historias de PriceMap, la app final para Android en Unity. Descompone en historias implementables los requisitos del PRD, las decisiones de la arquitectura y los requisitos del diseño de UX.

## Inventario de requisitos

### Requisitos funcionales

FR1: El usuario ve un mapa real en 2.5D (calles, agua, zonas verdes, edificios en volumen, nombres de calles) con los colores de la paleta del tema activo, sin iconos ajenos; lo mueve, amplía, gira e inclina con uno y dos dedos; lo ya visto se muestra sin conexión.
FR2: El usuario elige en el desplegable "Precios" qué lugares con precios ver: Ninguno (por defecto, el control dice "Ver precios"), Todos, Servicios locales, Hostelería u Ocio; con Ninguno solo se ve el lugar abierto o el destino.
FR3: Cada lugar con precios se muestra con el logo de su tipo y el color de su plantilla, insignia € si tiene precio (más discreto si no), etiqueta "%" si tiene oferta activa, y se mantiene en su coordenada (desvío < 1 px) al mover, girar e inclinar.
FR4: El mapa muestra puntos de interés reales de OpenStreetMap con icono y nombre, distinguibles de los lugares con precios; se ocultan paradas de autobús y oficinas.
FR5: El mapa muestra la posición real del usuario (círculo y, con brújula, cono de orientación) y un control para centrarla.
FR6: El usuario cambia entre tema claro (paleta B) y oscuro (paleta D); mapa e interfaz cambian a la vez.
FR7: Al tocar un lugar se abre su ficha con nombre, tipo, dirección, conceptos y precios, y bajo la cabecera coche / a pie con tiempo estimado y botón Ir, visibles sin desplazar.
FR8: La ficha muestra cada precio según su forma (fijo, rango por persona en hostelería, "desde X €"), agrupa conceptos por área y admite "A consultar"; en ocio, espacios con precio total y actividades por participante con precio por persona.
FR9: Cada precio muestra su origen (web del negocio con enlace y fecha, declarado por el negocio o según N clientes) y un punto de frescura con resumen.
FR10: Desde la ficha se puede marcar favorito, activar "Avísame si cambia" / "cuando tenga precio", añadir al plan, avisar de un precio incorrecto e ir; en lugares sin cuenta de negocio aparece "¿Es tu negocio?".
FR11: La app incluye los lugares reales de la ciudad desde OpenStreetMap sin rellenar precios por defecto; los centros sanitarios públicos aparecen sin precios.
FR12: Quien activó "Avísame si cambia" recibe un aviso cuando el precio mostrado de ese lugar cambia o pasa a tener precio; se comprueba al abrir la app y al volver a ella, con notificación local.
FR13: La hoja del buscador sigue al dedo, se queda donde se suelta, se abre o cierra con un lanzamiento rápido y permite buscar a media altura; antes de escribir muestra favoritos, categorías y lugares frecuentes.
FR14: Al escribir, el buscador encuentra lugares de la app, puntos de interés y direcciones, priorizando los cercanos.
FR15: "Encuentra cerca": al elegir una categoría, el mapa y una lista por distancia muestran solo esa categoría, primero los lugares con precios.
FR16: El usuario abre con la app una ubicación compartida por WhatsApp, Google Maps o Apple Maps (incluidos enlaces cortos), un enlace geo: o coordenadas, y navega hasta ella.
FR17: Manteniendo pulsado el mapa se marca un punto con su dirección; direcciones y puntos se guardan como favoritos con nombre; las direcciones navegadas no se guardan en el historial ni generan pregunta de llegada.
FR18: La app calcula la ruta más corta por calles reales, respetando en coche el sentido de circulación, empezando y acabando en el punto de calle más cercano.
FR19: Durante la ruta la app sigue la posición real, muestra la siguiente maniobra en castellano y el tiempo estimado, y recalcula si la posición se aleja más de 40 m durante más de 5 s.
FR20: En coche la posición es una flecha con el rumbo; a pie, un círculo con cono que sigue la brújula.
FR21: Si están activadas, las maniobras se leen en voz alta en castellano.
FR22: Al llegar (≤ 30 m) se muestra "Has llegado" con "Terminar ruta" (cierre automático al minuto); una ruta iniciada con la app hacia un lugar con precios crea una visita verificada cuando el tiempo restante llega a ≤ 5 min, aunque luego se termine antes o se cierre la app; con más de 5 min restantes no hay visita; como máximo una por ruta.
FR23: Una hora después de crearse una visita verificada, el cliente recibe una notificación que abre la pregunta de llegada aunque la app esté cerrada; si hay ruta en curso u otra pregunta abierta, la visita pasa a pendientes.
FR24: La pregunta sigue "¿Has ido?" → "¿Qué usaste?" (si hay varios conceptos) → "¿Te costó X €?" → valoración opcional → recompensa, con "Atrás" y "Más tarde" en cada paso.
FR25: Si el precio no coincide, el cliente lo indica con una regla de 1 € por marca, escribiéndolo o eligiendo uno ya aportado; acepta céntimos, sin tope, no acepta 0 ni negativos.
FR26: En hostelería el cliente elige un tramo de gasto por persona (< 10, 10-20, 20-30, 30-50, > 50 €) en lugar de confirmar un precio propuesto.
FR27: En un lugar sin precio, el primer cliente aporta el precio inicial eligiendo un concepto sugerido o escribiendo otro con su unidad, y recibe la recompensa de pionero.
FR28: En hostelería y ocio el cliente puede valorar 3 criterios de 1 a 5 estrellas, siempre opcional.
FR29: "Más tarde" guarda la visita como pendiente; el botón € muestra un punto rojo, retoma la pregunta y deja elegir entre varias; sin pendientes está desactivado.
FR30: En clínicas, fisioterapia, psicología y dentistas el aporte se guarda en la parte pública sin el identificador del cliente; solo cuenta para sus recompensas y reputación.
FR31: El precio mostrado se calcula con los aportes recientes: mediana (fijo), percentiles 25-75 (rango), percentil bajo 10-20 (desde).
FR32: Con al menos 3 aportes en los últimos 6 meses, el precio de los clientes sustituye al declarado y el origen pasa a "según N clientes".
FR33: Cada precio guarda su última confirmación y se clasifica en al día / podría haber cambiado / antiguo (hostelería < 30, 30-60, > 60 días; servicio local y ocio < 3, 3-6, > 6 meses).
FR34: Un cliente puede avisar de que un precio cambió, el servicio ya no se ofrece o el negocio cerró; cada aviso pesa según la reputación (×1, ×1,5, ×2).
FR35: Con 3 puntos de aviso el precio pasa a "en revisión", se marca en la ficha y la pregunta de llegada pide confirmarlo; se cierra cuando un cliente lo confirma o aporta otro.
FR36: Aportes, avisos, precios declarados y ofertas se guardan en un servidor y los ven todos los usuarios en menos de 1 minuto.
FR37: El usuario inicia sesión con Google (principal) o con correo, sin contraseñas propias de la app.
FR38: Sin cuenta se puede ver el mapa y las fichas, buscar y navegar; para aportar, ganar recompensas o guardar favoritos se pide crear cuenta.
FR39: Al registrarse se elige Cliente o Negocio; un negocio no recibe preguntas, no tiene monedas ni tienda y no aporta como cliente.
FR40: El progreso del cliente (monedas, XP, aportes, favoritos, plan, cosméticos, ajustes) se guarda en su cuenta y se recupera en otro dispositivo.
FR41: La primera vez se muestra el aviso de privacidad y se pide consentimiento explícito para ubicación y notificaciones.
FR42: El usuario puede cerrar sesión y borrar su cuenta y sus datos personales.
FR43: Un negocio reclama su lugar y se verifica confirmando por enlace el correo de su cuenta, que debe coincidir con el correo público del establecimiento (OpenStreetMap o su web); sin correo público, lo aprueba manualmente el administrador.
FR44: Si el lugar no existe, el negocio lo da de alta con nombre, tipo (por plantilla), ubicación (dirección o posición actual) y precios.
FR45: El negocio declara y edita los conceptos y precios de su lugar, que se muestran como "declarado por el negocio" hasta que manden los clientes.
FR46: El negocio publica ofertas con texto, precio opcional positivo y fecha de fin; desaparecen al caducar o con 3 avisos de "ya no está vigente".
FR47: El perfil del negocio muestra el estado de cada precio, los avisos recibidos y los precios en revisión.
FR48: Recompensas: confirmar +10 monedas/+15 XP; precio distinto +15/+20; pionero +25/+30; valorar +5/+10; confirmar precio "podría haber cambiado" +5 extra y "antiguo" +10 extra.
FR49: Cada 150 XP se sube de nivel; el avatar muestra el nivel con la XP como anillo.
FR50: El cliente compra con monedas y equipa cosméticos de posición (clásica, dorada, neón) y de ruta (clásica, discontinua, dorada), visibles al momento en el mapa.
FR51: El cliente gana insignias por hitos (primer precio, explorador, 10 y 50 aportes, racha de 7 días, crítico); las no ganadas se ven bloqueadas.
FR52: La reputación sube con los aportes (Nueva, Fiable a 5, Experta a 20) y se muestra en el perfil.
FR53: Llegada, pregunta y monedas tienen sonido, silenciable desde la burbuja del altavoz.
FR54: El cliente añade lugares al plan desde la ficha, los ordena, elige personas (1 por defecto) y qué usará en cada parada.
FR55: El plan calcula el gasto total (hostelería rango × personas, entradas y servicios × personas, pistas y aparcamientos una vez) e indica paradas sin precio y "desde".
FR56: "Empezar ruta" del plan guía parada a parada; al terminar una se abre la siguiente.
FR57: El cliente ve sus visitas pendientes, aportes, favoritos, avisos activos e insignias.
FR58: Ajustes en secciones: Apariencia, Navegación y voz, Notificaciones, Privacidad y ubicación, Accesibilidad, Idioma y Ayuda.
FR59: La app registra de forma anónima, con consentimiento, los eventos para las métricas (visitas, preguntas respondidas o aplazadas, aportes, confirmaciones, frescura, uso de la tienda) y los exporta por participante con identificador anónimo.
FR60: Un concepto puede tener variantes (horario, día, tipo de cliente) con su precio; la ficha las muestra y destaca la vigente; "¿Qué usaste?" las incluye; el negocio las declara; reglas y frescura se aplican por variante.

### Requisitos no funcionales

NFR1: El mapa se dibuja a ≥ 30 fps en un Android de gama media de 2021 con 4 GB de RAM y la zona visible carga en < 3 s con 4G.
NFR2: Con la zona en caché, mapa, fichas vistas y favoritos funcionan sin conexión; los aportes sin conexión se envían al recuperarla.
NFR3: Durante una ruta, el GPS sigue funcionando en segundo plano con una notificación visible de navegación (para que la visita verificada se cree con el móvil en el bolsillo); sin ruta activa, nunca usa el GPS en segundo plano.
NFR4: Contraste ≥ 4,5:1 (claro y oscuro), zonas táctiles ≥ 48 dp, respeto a "reducir animaciones" y TalkBack en mapa, ficha, buscador, pregunta de llegada y perfil.
NFR5: Textos en castellano; estructura preparada para añadir idiomas.
NFR6: Ningún precio sin origen; validación de precios > 0 y ofertas sin precio negativo.
NFR7: Servicios gratuitos o con capa gratuita suficiente para el TFG.
NFR8: El servidor solo permite a un cliente crear aportes, avisos y visitas a su nombre y a un negocio editar su propio lugar verificado; nadie modifica progreso, monedas o reputación ajenos; reglas comprobadas en el servidor.

### Requisitos adicionales (arquitectura)

- **Proyecto base existente (brownfield), no plantilla nueva:** `unity/PriceMap` (Unity 6000.3.28f1, render integrado) con el mapa vectorial de la opción C (`PriceMap.Vectorial`, `Geo`, `CamaraMapa`, `Marcador`). La primera historia reorganiza ese proyecto en capas con `.asmdef` y retira Cesium y las escenas de prueba A y B.
- AD-1: capas hexagonales impuestas con `.asmdef` (Dominio sin UnityEngine; UI solo ve Aplicación y Dominio).
- AD-2: una sola raíz de composición `PriceMap.Arranque`; sin singletons ni localizadores para lógica.
- AD-3: el precio mostrado, su origen, la frescura y "en revisión" se calculan en el Dominio al leer; nunca se guardan.
- AD-4: aportes, avisos y visitas son inmutables (solo crear).
- AD-5: un único escritor por entidad (tabla de dueños: datos base empaquetados, `lugares`, `propiedad`, `declaraciones`, `ofertas`, `aportes`, `avisos`, `usuarios/<uid>/visitas`, `usuarios/<uid>/aportes`, `usuarios/<uid>`, `contactosLugares`, `evaluacion`).
- AD-6: `firebase/firestore.rules` versionado: aportes y avisos públicos sin autor; visita `<lugarId>_<AAAAMMDD>` privada; aporte en lote con la visita (`getAfter`) y copia privada; peso del aviso según reputación; negocio sin aportes; verificación contra `contactosLugares`.
- AD-7: Firebase en plan Spark (Auth Google y correo + Firestore); sin Cloud Functions, push de servidor ni SMS.
- AD-8: caché de Firestore como único almacén local; caché propia solo para teselas.
- AD-9: `ServicioVisitas` es el único que crea visitas, programa la notificación de +1 h y gestiona pendientes.
- AD-10: importes en céntimos enteros; tramos `[minCent, maxCent]`.
- AD-11: ids en texto (`osm:<tipo>:<id>`, slug manual, `alta:<autoId>`, `concepto@variante`); nunca se reutilizan.
- AD-12: `creado = ServerTimestamp`; el Dominio recibe "ahora" como parámetro (hora del servidor estimada).
- AD-13: el mapa solo a través del puerto `IMapa`; lat/lon `double` fuera del adaptador.
- AD-14: parámetros de negocio solo en `ParametrosNegocio`.
- AD-15: la UI observa estado de los servicios; nunca consulta Firestore.
- AD-16: registro de evaluación solo por `IRegistroEventos`, anónimo y con consentimiento.
- AD-17: forma fija del aporte (`lugarId`, `conceptoId`, `forma`, `importeCent` o `minCent`/`maxCent`, `creado`) y del aviso (`lugarId`, `conceptoId`, `tipo`, `peso`, `creado`).
- AD-18: `usuarios/<uid>` solo con actualizaciones por campo e incrementos atómicos; cada servicio, sus campos.
- AD-19: GPS en segundo plano solo durante una ruta, con un servicio en primer plano nativo de Android (plugin en `Assets/Plugins/Android/`, tipo `location`, notificación fija); se arranca al pulsar Ir y se para al terminar; sin `ACCESS_BACKGROUND_LOCATION`.
- Convenciones: código en castellano sin tildes en identificadores; `Resultado<T>` entre capas; `async`/`Task`; textos con el paquete Localization (`es-ES`); `AccessibilityHierarchy` por pantalla; GPS en segundo plano solo durante una ruta (AD-19); pruebas EditMode del Dominio.
- Stack: Firebase Unity SDK 14.1.0 (Auth, Firestore), Mobile Notifications 2.3.2, Localization, Unity Test Framework, Android minSdk 26 / targetSdk 35.
- Servicios externos: OpenFreeMap (teselas), Valhalla FOSSGIS (`shortest`) con OSRM de reserva, Photon (direcciones).
- Entorno: un proyecto Firebase; reglas con Firebase CLI; script de sembrado `firebase/sembrar/` para `contactosLugares`; APK sin Google Play.
- Datos iniciales: migrar los 121 lugares de `prototipo/datos-lugares.js` y las tarifas de `prototipo/datos-precios-web.js` a los datos base empaquetados.

### Requisitos de diseño UX

UX-DR1: Tokens de color de DESIGN.md (paletas B y D con sus pares `-dark`, colores de plantilla, frescura y mapa) traducidos a variables USS; ningún color suelto en el código.
UX-DR2: Tipografía Varela Round (títulos) y Nunito Sans (texto) con la escala de DESIGN.md en sp; con la fuente al 200 % nada se corta.
UX-DR3: Escala de espaciado 4/8/12/16/24/32 y radios `xs` a `pill`; alturas mínimas (no fijas) en botones, píldoras y cápsula.
UX-DR4: Cápsula inferior de cristal con buscador y avatar; opacidad mínima para 4,5:1 y fondo opaco con "reducir transparencias".
UX-DR5: Hoja del buscador con física de soltar (0,6 px/ms, 15 %, 88 %, 6 px) y acciones "Expandir"/"Contraer".
UX-DR6: Píldora "Precios" con menú de 5 opciones; botones redondos (tema, ubicación con segundo toque al norte, altavoz, € con punto rojo).
UX-DR7: Marcador de lugar (42 px, color de plantilla, logo, insignia €, etiqueta "%", sin precio atenuado).
UX-DR8: Posición del usuario (círculo, cono a pie, flecha en coche) y línea de ruta, con sus cosméticos.
UX-DR9: Ficha con cabecera, fila de acciones (coche / a pie, Ir) visible sin desplazar, conceptos con precio, origen, frescura y variante vigente destacada.
UX-DR10: Regla de precio (1 € por marca, número tocable, control ajustable con TalkBack) y tramos de hostelería.
UX-DR11: Pantalla de recompensa (moneda "+N", "¡Pionero!", barra de XP, "Volver al mapa" / "Ir a la tienda").
UX-DR12: Tarjetas de cosmético (equipado / comprar / equipar, "Te faltan N monedas"), insignias bloqueadas y avatar con anillo de XP.
UX-DR13: Tarjeta del plan con paradas ordenables por arrastre o "Subir"/"Bajar", personas y gasto estimado.
UX-DR14: Hoja de cuenta (Google, correo, "Ahora no") que vuelve a la acción que la pidió.
UX-DR15: Toast de 3 s anunciado como región en vivo; nunca única vía para información imprescindible.
UX-DR16: Estados de EXPERIENCE.md · State Patterns (primer arranque, permisos denegados, cargando, sin conexión, sin precio, en revisión, sin resultados, GPS sin señal, ficha cargando, perfil vacío, enlace caducado, correo que no coincide, notificaciones denegadas, aporte sin conexión, monedas insuficientes, borrar cuenta, error de inicio de sesión).
UX-DR17: TalkBack en mapa, ficha, buscador, pregunta y perfil (nombre, rol y estado; marcadores anunciados con nombre, tipo y precio); botones de zoom y lista "Encuentra cerca" como alternativa al mapa con TalkBack.
UX-DR18: Zonas táctiles ≥ 48 dp; foco que va al título al abrir una hoja y vuelve al cerrarla; nada solo por color.
UX-DR19: "Reducir animaciones" quita transiciones de hojas, animación de la moneda y movimiento suave de cámara.
UX-DR20: Microcopia según Voice and Tone (tuteo, frases cortas, origen del precio siempre).
UX-DR21: Sonidos cortos (llegada, pregunta, monedas) silenciables, con modo silencio respetado y siempre acompañados de señal visible y anuncio.
UX-DR22: Solo vertical; zonas seguras; tema inicial según el sistema; botón atrás de Android cierra la capa superior.
UX-DR23: Iconos vectoriales dibujados, nunca emojis; logos por tipo de negocio.
UX-DR24: Las 4 maquetas de `mockups/` y las capturas del prototipo citadas por pantalla como referencia visual (los documentos mandan).

### Mapa de cobertura de requisitos

FR1: Épica 1 - Mapa real en 2.5D con la paleta y caché
FR2: Épica 1 - Filtro "Precios"
FR3: Épica 1 - Marcadores de lugares con precios (la etiqueta "%" de oferta llega en la Épica 8)
FR4: Épica 1 - Puntos de interés de OpenStreetMap
FR5: Épica 1 - Posición del usuario y centrar
FR6: Épica 1 - Tema claro y oscuro
FR7: Épica 1 - Ficha del lugar con fila de acciones (Ir se activa en la Épica 2)
FR8: Épica 1 - Precios por plantilla
FR9: Épica 1 - Origen y frescura visibles
FR10: Épica 1 - Estructura de acciones de la ficha; cada acción llega con su épica (Ir: 2, favorito: 3, avisar y avísame: 7, ¿Es tu negocio?: 8, añadir al plan: 9)
FR11: Épica 1 - Lugares reales sin precios inventados
FR12: Épica 7 - "Avísame si cambia" al abrir la app
FR13: Épica 2 - Hoja del buscador desplegable
FR14: Épica 2 - Búsqueda de lugares y direcciones
FR15: Épica 2 - "Encuentra cerca"
FR16: Épica 2 - Destino desde ubicación compartida
FR17: Épica 2 - Punto libre en el mapa (guardar como favorito: Épica 3)
FR18: Épica 2 - Ruta más corta por calles reales
FR19: Épica 2 - Navegación giro a giro y recálculo
FR20: Épica 2 - Flecha en coche y cono a pie
FR21: Épica 2 - Indicaciones por voz
FR22: Épica 2 - Llegada y "Terminar ruta" (creación de la visita verificada: Épica 4)
FR23: Épica 4 - Pregunta una hora después con la app cerrada
FR24: Épica 4 - Flujo de la pregunta
FR25: Épica 4 - Regla de precio
FR26: Épica 4 - Tramos de hostelería
FR27: Épica 4 - Primer precio (pionero)
FR28: Épica 4 - Valoración opcional
FR29: Épica 4 - Pendientes y botón €
FR30: Épica 4 - Aportes anónimos
FR31: Épica 4 - Cálculo del precio con los aportes
FR32: Épica 4 - Mandan los clientes (3 aportes en 6 meses)
FR33: Épica 1 - Frescura de cada precio
FR34: Épica 7 - Avisos de precio incorrecto
FR35: Épica 7 - Estado "en revisión"
FR36: Épica 4 - Datos compartidos en menos de 1 minuto
FR37: Épica 3 - Inicio de sesión con Google o correo
FR38: Épica 3 - Uso sin cuenta
FR39: Épica 3 - Tipo de cuenta cliente o negocio
FR40: Épica 3 - Progreso en la nube
FR41: Épica 3 - Aviso de privacidad y consentimientos
FR42: Épica 3 - Cerrar sesión y borrar cuenta
FR43: Épica 8 - Reclamar y verificar un lugar por correo
FR44: Épica 8 - Alta de un lugar nuevo
FR45: Épica 8 - Declarar y editar precios
FR46: Épica 8 - Ofertas
FR47: Épica 8 - Panel del negocio
FR48: Épica 4 - Recompensas por aportar (incluidos los extra por frescura)
FR49: Épica 4 - Niveles y anillo de XP
FR50: Épica 5 - Tienda de cosméticos
FR51: Épica 5 - Insignias
FR52: Épica 5 - Reputación
FR53: Épica 5 - Sonidos de recompensa
FR54: Épica 9 - Crear y ordenar un plan
FR55: Épica 9 - Gasto estimado del plan
FR56: Épica 9 - Ruta del plan
FR57: Épica 6 - Actividad del cliente
FR58: Épica 3 - Ajustes agrupados (cada épica añade su sección)
FR59: Épica 6 - Registro anónimo para la evaluación
FR60: Épica 1 - Mostrar variantes (en "¿Qué usaste?": Épica 4; declarar variantes: Épica 8)

## Lista de épicas

Orden de construcción aprobado en la mesa redonda del 17/11/2026: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9. Las épicas 1 a 6 son lo mínimo para las pruebas con usuarios (SUS). La accesibilidad, los estados de error y sin conexión, los textos localizables y las pruebas del Dominio van dentro de cada historia, no en una épica aparte.

### Épica 1: Explorar Sanlúcar y ver sus precios en el mapa
Cualquier usuario, sin cuenta y sin conexión tras la primera carga, ve el mapa real de Sanlúcar con la paleta del proyecto, los lugares con su logo, el filtro "Precios", su posición y la ficha de cada lugar con los precios conocidos (los publicados en la web del negocio), su origen, su frescura y sus variantes. Incluye la base técnica: proyecto en capas, raíz de composición, retirada de Cesium y lugares base empaquetados.
**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR8, FR9, FR10 (estructura), FR11, FR33, FR60 (mostrar)

### Épica 2: Buscar y llegar a cualquier sitio
El usuario busca lugares, categorías y direcciones, abre ubicaciones compartidas, marca puntos en el mapa y navega giro a giro por la ruta más corta, con flecha o cono, voz y GPS activo con la pantalla apagada, hasta "Has llegado".
**FRs covered:** FR13, FR14, FR15, FR16, FR17, FR18, FR19, FR20, FR21, FR22 (llegada), FR10 (Ir)

### Épica 3: Crear tu cuenta y guardar tu progreso
El usuario acepta la privacidad, entra con Google o con correo (o sigue sin cuenta), elige cliente o negocio, guarda favoritos y progreso en la nube, y gestiona su cuenta desde el esqueleto del perfil y los ajustes, que las siguientes épicas amplían.
**FRs covered:** FR37, FR38, FR39, FR40, FR41, FR42, FR58, FR10 (favorito), FR17 (guardar favorito)

### Épica 4: Aportar el precio al llegar
El cliente que navega hasta un lugar con precios recibe, una hora después, la pregunta de llegada; aporta o confirma el precio (regla, tramos, pionero, variantes, valoración), lo deja pendiente si quiere, y gana monedas, XP y nivel. Los aportes son anónimos, protegidos por las reglas de Firestore, y en cuanto hay 3 manda lo que pagan los clientes.
**FRs covered:** FR22 (visita verificada), FR23, FR24, FR25, FR26, FR27, FR28, FR29, FR30, FR31, FR32, FR36, FR48, FR49, FR60 (en "¿Qué usaste?")

### Épica 5: Tienda, insignias y reputación
El cliente gasta sus monedas en cosméticos que se ven en el mapa, gana insignias, sube de reputación y oye la recompensa.
**FRs covered:** FR50, FR51, FR52, FR53

### Épica 6: Tu actividad y la evaluación del TFG
El cliente consulta su actividad completa y, con su consentimiento, la app registra de forma anónima los datos que necesitan las pruebas con usuarios y las métricas de la memoria.
**FRs covered:** FR57, FR59

### Épica 7: Precios fiables y al día
El cliente avisa de un precio incorrecto (con peso según su reputación), ve qué precios están "en revisión" y la pregunta de llegada le pide confirmarlos, y recibe un aviso cuando cambia el precio de un lugar que vigila.
**FRs covered:** FR12, FR34, FR35, FR10 (avisar, avísame)

### Épica 8: La cuenta de negocio
El dueño reclama y verifica su local por correo (o con aprobación manual), da de alta uno nuevo, declara precios y variantes, publica ofertas visibles en la ficha y en el marcador, y consulta su panel.
**FRs covered:** FR43, FR44, FR45, FR46, FR47, FR3 (etiqueta "%"), FR10 (¿Es tu negocio?), FR60 (declarar variantes)

### Épica 9: Plan de tarde
El cliente monta un plan con varias paradas y personas, ve el gasto estimado y lo recorre parada a parada.
**FRs covered:** FR54, FR55, FR56, FR10 (añadir al plan)

## Épica 1: Explorar Sanlúcar y ver sus precios en el mapa

Cualquier usuario, sin cuenta y sin conexión tras la primera carga, ve el mapa real de Sanlúcar con la paleta del proyecto, los lugares con su logo, el filtro "Precios", su posición y la ficha de cada lugar con los precios conocidos, su origen, su frescura y sus variantes.

### Historia 1.1: Abrir la app y ver el mapa sobre la nueva estructura del proyecto

Como usuario,
quiero abrir la app en mi Android y ver el mapa de Sanlúcar,
para que la app arranque sobre una base ordenada en la que crecerán todas las funciones.

**Criterios de aceptación:**

**Dado** el proyecto existente `unity/PriceMap`
**Cuando** se reorganiza según la arquitectura
**Entonces** existen los ensamblados `PriceMap.Dominio` (con `noEngineReferences: true`), `PriceMap.Aplicacion`, `PriceMap.Adaptadores.*`, `PriceMap.Vectorial`, `PriceMap.UI`, `PriceMap.Arranque` y uno de pruebas EditMode del Dominio, con las referencias del diagrama de AD-1 y ninguna otra
**Y** el código del mapa vectorial existente (`LectorMVT`, `Triangulador`, `MapaVectorial`, `Geo`, `CamaraMapa`, `Marcador`) queda en sus capas sin cambiar su comportamiento

**Dado** el proyecto reorganizado
**Cuando** se compila
**Entonces** el paquete Cesium, las escenas `Mapa` y `MapaCesium` y sus scripts y shaders exclusivos ya no están, y no hay errores de compilación

**Dado** un único componente `Arranque` en la escena principal
**Cuando** la app arranca
**Entonces** ese componente crea los servicios y adaptadores y se los entrega a la UI, sin singletons ni localizadores de servicios (AD-2)
**Y** la app compila para Android (minSdk 26), se instala como APK en un móvil real, solo en vertical, y muestra el mapa vectorial de Sanlúcar con la atribución de OpenFreeMap y OpenStreetMap

### Historia 1.2: Mapa con la paleta, el tema claro u oscuro y los gestos

Como usuario,
quiero un mapa con los colores de la app que pueda mover, ampliar, girar e inclinar,
para explorar la ciudad cómodamente de día y de noche.

**Criterios de aceptación:**

**Dado** el mapa abierto
**Cuando** uso un dedo, dos dedos para ampliar y girar, y arrastre vertical con dos dedos
**Entonces** el mapa se mueve, amplía, gira e inclina (FR1) a 30 fps o más en un Android de gama media (NFR-1)

**Dado** el primer arranque
**Cuando** el tema del sistema es oscuro
**Entonces** la app arranca con la paleta D; si es claro, con la paleta B (UX-DR22)
**Y** el botón redondo de tema alterna entre claro y oscuro y cambia a la vez el mapa y la interfaz (FR6)

**Dado** los tokens de DESIGN.md
**Cuando** se construye la interfaz
**Entonces** colores, tipografías (Varela Round y Nunito Sans en sp), espaciados y radios salen de variables USS, sin colores sueltos en el código (UX-DR1 a UX-DR3)
**Y** la composición de cada pantalla sigue las maquetas de `ux-designs/.../mockups/` y las capturas del prototipo citadas por pantalla en EXPERIENCE.md, y si discrepan mandan DESIGN.md y EXPERIENCE.md (UX-DR24)

**Dado** que he visto una zona del mapa
**Cuando** abro la app sin conexión
**Entonces** esa zona se dibuja desde la caché de teselas, y una zona nunca vista muestra el color de fondo del mapa sin pantalla en blanco (FR1, NFR-2)

**Dado** TalkBack activo
**Cuando** estoy en el mapa
**Entonces** aparecen botones de acercar y alejar con su nombre, y todos los controles del mapa tienen nombre, rol y estado y zona táctil de al menos 48 dp (UX-DR17, UX-DR18)

### Historia 1.3: Calcular el precio, su origen, su frescura y sus variantes

Como usuario,
quiero que cada precio se calcule siempre igual y diga de dónde sale y lo reciente que es,
para poder fiarme de lo que veo.

**Criterios de aceptación:**

**Dado** el Dominio en C# puro (sin UnityEngine)
**Cuando** recibe los datos de un concepto (precio de la web, declaraciones, aportes, avisos) y el instante "ahora" como parámetro
**Entonces** devuelve el precio mostrado según su forma (fijo, rango por persona, "desde"), su origen (web del negocio con página y fecha, declarado por el negocio o según N clientes) y su frescura (AD-3, AD-12, FR8, FR9)
**Y** en esta épica, sin aportes ni declaraciones, el origen es la web del negocio o "sin precio"

**Dado** la tabla de frescura del PRD
**Cuando** se calcula
**Entonces** hostelería está "al día" a menos de 30 días, "podría haber cambiado" de 30 a 60 y es "antiguo" a más de 60; servicio local y ocio, a menos de 3 meses, de 3 a 6 y a más de 6 (FR33)

**Dado** un concepto con variantes (por ejemplo `superinductiva@con-bono`)
**Cuando** se calcula
**Entonces** cada variante tiene su propio precio y su frescura, y la función indica qué variante está vigente en un instante dado (FR60)

**Dado** los parámetros de negocio
**Cuando** se usan
**Entonces** todos vienen de `ParametrosNegocio` (AD-14) y los importes van en céntimos (AD-10)
**Y** cada regla tiene su prueba EditMode, y todas pasan

### Historia 1.4: Lugares reales de Sanlúcar con su logo en el mapa

Como usuario,
quiero ver en el mapa los lugares reales con el logo de su tipo de negocio,
para reconocer de un vistazo qué es cada sitio.

**Criterios de aceptación:**

**Dado** los 121 lugares de `prototipo/datos-lugares.js`, Clínica Areas y FisioSanlúcar, ChemaBarber y las tarifas de `prototipo/datos-precios-web.js`
**Cuando** se migran a los datos base empaquetados (JSON en `Assets/Datos/`)
**Entonces** cada lugar tiene id estable (`osm:<tipo>:<id>` o slug, como `clinica-areas` y `chemabarber`), nombre, tipo, plantilla, coordenadas y conceptos, sin ningún precio que no esté en la web oficial (FR11, AD-11)
**Y** las tarifas web conservan su página y su fecha de consulta, y los importes van en céntimos (AD-10)

**Dado** los datos base cargados
**Cuando** se muestran en el mapa a través del puerto `IMapa` (AD-13)
**Entonces** cada lugar con precios aparece con el logo de su tipo, el color de su plantilla, insignia € si tiene precio y aspecto atenuado si no (FR3, UX-DR7, UX-DR23)
**Y** el marcador se mantiene en su coordenada con menos de 1 px de desvío al mover, girar e inclinar

**Dado** los centros sanitarios públicos
**Cuando** se muestran
**Entonces** aparecen sin precios ni insignia € (FR11)

**Dado** TalkBack activo
**Cuando** el foco llega a un marcador
**Entonces** se anuncia con nombre, tipo y precio si lo tiene ("Clínica Areas, fisioterapia, desde 40 €")

### Historia 1.5: Filtro "Precios" en el mapa

Como usuario,
quiero elegir qué lugares con precios ver,
para tener el mapa limpio o centrarme en lo que busco.

**Criterios de aceptación:**

**Dado** el primer arranque
**Cuando** miro el mapa
**Entonces** la píldora de arriba a la izquierda dice "Ver precios" y no hay marcadores de precio (FR2)

**Dado** la píldora "Precios"
**Cuando** la toco
**Entonces** se abre un menú con Ninguno, Todos, Servicios locales, Hostelería y Ocio, con la opción elegida marcada, y el mapa muestra solo esos lugares (FR2, UX-DR6)
**Y** el menú se maneja con TalkBack y la elección se mantiene al volver a abrir la app

### Historia 1.6: Puntos de interés de referencia en el mapa

Como usuario,
quiero ver supermercados, farmacias, gasolineras y otros sitios reales en el mapa,
para orientarme aunque no tengan precios.

**Criterios de aceptación:**

**Dado** la capa `poi` de las teselas de OpenFreeMap
**Cuando** acerco el mapa
**Entonces** aparecen los puntos de interés con icono vectorial y nombre, distinguibles de los marcadores de lugares con precios (FR4, UX-DR23)
**Y** las paradas de autobús y las oficinas no se muestran

**Dado** un punto de interés que ya es un lugar de la app
**Cuando** se dibujan ambos
**Entonces** solo aparece el marcador del lugar de la app

### Historia 1.7: Ver mi posición en el mapa

Como usuario,
quiero ver dónde estoy y centrar el mapa en mi posición,
para saber qué tengo cerca.

**Criterios de aceptación:**

**Dado** que concedo el permiso de ubicación tras una explicación breve
**Cuando** la app está delante
**Entonces** el mapa muestra mi posición como un círculo y, si el móvil tiene brújula, un cono de orientación (FR5)
**Y** el botón de ubicación centra el mapa en mí y, con un segundo toque, lo orienta al norte (UX-DR6)

**Dado** que deniego el permiso
**Cuando** abro el mapa
**Entonces** el mapa se centra en Sanlúcar y el botón de ubicación explica cómo activarlo (UX-DR16)

**Dado** la app en segundo plano y sin ruta
**Cuando** pasa el tiempo
**Entonces** la app no usa el GPS (NFR-3)

### Historia 1.8: Ficha del lugar con sus precios

Como usuario,
quiero tocar un lugar y ver sus precios con su origen y su frescura,
para saber cuánto me va a costar antes de ir.

**Criterios de aceptación:**

**Dado** un marcador
**Cuando** lo toco
**Entonces** sube la ficha con nombre, tipo (con el color de su plantilla) y dirección, y el mapa centra el lugar en la zona no tapada por la ficha (FR7, UX-DR9)

**Dado** la ficha de Clínica Areas
**Cuando** la abro
**Entonces** veo los conceptos agrupados por área, con "desde 40 €" en la sesión de fisioterapia, "Consultar" donde la web no da importe, el origen "Publicado en su web · 13/11/2026" y el punto de frescura con su texto (FR8, FR9, UX-DR20)
**Y** la variante "Superinductiva · con bono · desde 15 €" aparece bajo su concepto, destacada si es la vigente (FR60)

**Dado** un lugar sin precio, como ChemaBarber
**Cuando** abro su ficha
**Entonces** dice "Aún no tiene precio" sin inventar ninguno (UX-DR16)

**Dado** la ficha abierta
**Cuando** miro su estructura
**Entonces** tiene reservada la fila de acciones bajo la cabecera, que las siguientes épicas rellenan (FR10)

**Dado** TalkBack activo
**Cuando** abro la ficha
**Entonces** el foco va a su título y, al cerrarla con el botón atrás de Android, vuelve al marcador (UX-DR18, UX-DR22)

## Épica 2: Buscar y llegar a cualquier sitio

El usuario busca lugares, categorías y direcciones, abre ubicaciones compartidas, marca puntos en el mapa y navega giro a giro por la ruta más corta, con flecha o cono, voz y GPS activo con la pantalla apagada, hasta "Has llegado".

### Historia 2.1: GPS activo con la pantalla apagada durante una ruta

Como usuario que navega con el móvil en el bolsillo,
quiero que la app siga sabiendo dónde estoy aunque apague la pantalla,
para que la navegación y la llegada funcionen sin tener el móvil en la mano.

**Criterios de aceptación:**

**Dado** un plugin nativo de Android en `Assets/Plugins/Android/` con un servicio en primer plano de tipo `location` y el permiso `FOREGROUND_SERVICE_LOCATION`
**Cuando** el adaptador de GPS lo arranca con la app delante
**Entonces** aparece una notificación fija de navegación y llegan posiciones al puerto de GPS aunque la pantalla esté bloqueada o haya otra app abierta (AD-19, NFR-3)
**Y** la app no pide `ACCESS_BACKGROUND_LOCATION`

**Dado** el servicio en marcha
**Cuando** el adaptador lo para
**Entonces** la notificación desaparece y no llegan más posiciones en segundo plano

**Dado** cualquier otro módulo
**Cuando** intenta usar la ubicación en segundo plano sin ruta
**Entonces** no puede: solo el adaptador de GPS arranca y para el servicio
**Y** en Android 13 o superior se pide el permiso de notificaciones antes de arrancarlo; si se deniega, la navegación sigue en primer plano y se explica la limitación

### Historia 2.2: Hoja del buscador desplegable

Como usuario,
quiero abrir el buscador desde la cápsula y dejarlo a la altura que me venga bien,
para buscar sin perder de vista el mapa.

**Criterios de aceptación:**

**Dado** la cápsula de cristal con el buscador y el avatar (UX-DR4)
**Cuando** arrastro la hoja
**Entonces** sigue al dedo y se queda donde la suelto; un lanzamiento de más de 0,6 px/ms la abre o cierra del todo; soltada por debajo del 15 % se pliega y por encima del 88 % se abre; un movimiento de más de 6 px es arrastre y no saca el teclado (FR13, UX-DR5)

**Dado** la hoja a media altura
**Cuando** escribo
**Entonces** puedo buscar sin abrirla del todo, y arrastrar la lista mueve la hoja

**Dado** TalkBack activo o sin poder arrastrar
**Cuando** toco la cápsula
**Entonces** la hoja se abre del todo y ofrece las acciones "Expandir" y "Contraer" (UX-DR5, UX-DR17)

**Dado** el primer uso, sin favoritos ni lugares frecuentes
**Cuando** abro la hoja
**Entonces** muestra solo las categorías y "Encuentra cerca" (UX-DR16)
**Y** con "reducir animaciones" activo la hoja cambia de altura sin transición (UX-DR19)

### Historia 2.3: Buscar lugares, puntos de interés y direcciones

Como usuario,
quiero escribir un nombre o una dirección y encontrarlo,
para ir a cualquier sitio, tenga precio o no.

**Criterios de aceptación:**

**Dado** la hoja del buscador
**Cuando** escribo
**Entonces** aparecen lugares de la app, puntos de interés y direcciones de Photon, priorizando los cercanos (FR14)

**Dado** una búsqueda sin resultados
**Cuando** termino de escribir
**Entonces** veo "No hay resultados" y la opción de buscarlo como dirección (UX-DR16)

**Dado** que no hay conexión
**Cuando** busco
**Entonces** encuentro los lugares de la app (datos base) y un aviso de que las direcciones necesitan conexión

### Historia 2.4: Encuentra cerca por categorías

Como usuario,
quiero elegir una categoría y ver lo que hay cerca ordenado por distancia,
para encontrar rápido lo que necesito, también sin mirar el mapa.

**Criterios de aceptación:**

**Dado** la lista de categorías (restaurantes, gasolineras, supermercados, farmacias, bares, gimnasios, salud, cultura...)
**Cuando** elijo una
**Entonces** el mapa muestra solo esa categoría y debajo aparece una lista por distancia, primero los lugares con precios y después los puntos de interés, sin repetir los que ya están en la app (FR15)

**Dado** TalkBack activo
**Cuando** uso la lista
**Entonces** cada elemento se anuncia con nombre, tipo, distancia y precio si lo tiene, y al tocarlo se abre su ficha sin tocar el mapa (UX-DR17)

### Historia 2.5: Ir a un punto libre o a una ubicación compartida

Como usuario,
quiero marcar un punto en el mapa o abrir una ubicación que me han mandado por WhatsApp,
para ir a casa de un amigo o a cualquier sitio que no está en la app.

**Criterios de aceptación:**

**Dado** el mapa
**Cuando** mantengo pulsado un punto
**Entonces** se abre la ficha de punto libre con la dirección obtenida por geocodificación inversa (FR17)

**Dado** un enlace de Google Maps (`?q=`, `@lat,lng`, `!3d…!4d…`), de Apple Maps (`ll=`), un enlace `geo:`, unas coordenadas o un enlace corto (`maps.app.goo.gl`)
**Cuando** lo comparto con la app o lo pego en el buscador
**Entonces** la app abre la ficha de ese punto; los enlaces cortos se resuelven siguiendo la redirección (FR16)

**Dado** un enlace que no se reconoce
**Cuando** lo pego
**Entonces** la app lo dice con un mensaje claro y no abre nada

### Historia 2.6: Ver los tiempos y calcular la ruta más corta

Como usuario,
quiero ver en la ficha cuánto tardo en coche o a pie y pulsar Ir,
para elegir cómo ir y tener la ruta más corta sin ir en contra dirección.

**Criterios de aceptación:**

**Dado** la ficha de un lugar o de un punto libre
**Cuando** se abre con mi posición conocida
**Entonces** la fila de acciones bajo la cabecera muestra coche (por defecto) y a pie con sus minutos y el botón Ir, visibles sin desplazar (FR7, FR10, UX-DR9)

**Dado** que pulso Ir
**Cuando** se calcula la ruta
**Entonces** se pide a Valhalla (FOSSGIS) con `shortest`, respetando en coche el sentido de circulación; si no responde, a OSRM quedándose con la más corta de sus alternativas (FR18)
**Y** la ruta empieza y acaba en el punto de calle más cercano y se dibuja en el mapa con el estilo de ruta del usuario (UX-DR8)

**Dado** que no hay conexión
**Cuando** pulso Ir
**Entonces** se muestra una ruta aproximada con un aviso (UX-DR16)

### Historia 2.7: Navegar giro a giro

Como conductor o peatón,
quiero que la app me guíe con la siguiente maniobra y mi posición,
para llegar sin perderme.

**Criterios de aceptación:**

**Dado** una ruta en curso
**Cuando** me muevo
**Entonces** la app sigue mi posición real, muestra la siguiente maniobra en castellano y el tiempo restante, y arranca el servicio de GPS en segundo plano de la Historia 2.1 con la notificación "Navegando a <destino> · N min" (FR19, AD-19)

**Dado** que me alejo más de 40 m de la ruta durante más de 5 s
**Cuando** pasa
**Entonces** la ruta se recalcula sin borrar la anterior hasta tener la nueva (FR19)

**Dado** el modo coche
**Cuando** navego
**Entonces** mi posición es una flecha con el rumbo; a pie, un círculo con cono que sigue la brújula (FR20, UX-DR8)

**Dado** que se pierde la señal del GPS
**Cuando** ocurre
**Entonces** se mantiene la última posición atenuada con el aviso "Buscando señal GPS" y no se recalcula hasta recuperarla (UX-DR16)

### Historia 2.8: Indicaciones por voz

Como conductor,
quiero oír las maniobras,
para no tener que mirar el móvil.

**Criterios de aceptación:**

**Dado** "Indicaciones por voz" activado
**Cuando** se acerca una maniobra
**Entonces** se lee en voz alta en castellano con la voz del móvil (TextToSpeech de Android), también con la pantalla apagada (FR21)

**Dado** la voz desactivada o el móvil en silencio
**Cuando** se acerca una maniobra
**Entonces** no suena nada y la maniobra se sigue viendo en pantalla

### Historia 2.9: Llegar y terminar la ruta

Como usuario,
quiero saber que he llegado y cerrar la ruta,
para volver al mapa.

**Criterios de aceptación:**

**Dado** una ruta en curso
**Cuando** estoy a 30 m o menos del destino
**Entonces** aparece "Has llegado" con "Terminar ruta"; si no lo pulso, la ruta se cierra sola al minuto y TalkBack anuncia "La ruta se cerrará en un minuto" (FR22)

**Dado** que termino la ruta, a mano o sola
**Cuando** se cierra
**Entonces** el servicio de GPS en segundo plano se para y desaparece la notificación de navegación (AD-19)
**Y** las direcciones y puntos libres a los que he navegado no se guardan en el historial (FR17)

## Épica 3: Crear tu cuenta y guardar tu progreso

El usuario acepta la privacidad, entra con Google o con correo (o sigue sin cuenta), elige cliente o negocio, guarda favoritos y progreso en la nube, y gestiona su cuenta desde el esqueleto del perfil y los ajustes.

### Historia 3.1: Bienvenida, privacidad y consentimientos

Como usuario nuevo,
quiero saber qué datos usa la app y decidir qué permito,
para usarla con confianza.

**Criterios de aceptación:**

**Dado** el primer arranque
**Cuando** abro la app
**Entonces** antes del mapa veo la bienvenida con el aviso de privacidad y pido o rechazo explícitamente los permisos de ubicación y de notificaciones (FR41, UX-DR16)

**Dado** que rechazo un permiso
**Cuando** sigo
**Entonces** la app funciona con lo que permite y Ajustes explica cómo activarlo después

**Dado** que ya pasé la bienvenida
**Cuando** vuelvo a abrir la app
**Entonces** va directa al mapa

### Historia 3.2: Mis datos en Firebase, protegidos y disponibles sin conexión

Como usuario,
quiero que mis datos se guarden de forma segura y que la app funcione sin conexión,
para no perder nada y que nadie toque lo mío.

**Criterios de aceptación:**

**Dado** un proyecto de Firebase en plan Spark con Authentication (Google y correo) y Firestore
**Cuando** se integra con el Firebase Unity SDK en el adaptador de Firebase
**Entonces** la persistencia sin conexión de Firestore está activada y ninguna otra capa usa Firebase directamente (AD-2, AD-7, AD-8)

**Dado** `firebase/firestore.rules` versionado y desplegado con Firebase CLI
**Cuando** se prueban con el emulador de Firestore
**Entonces** cada usuario solo lee y escribe su documento `usuarios/<uid>` y sus subcolecciones, y nadie puede tocar las de otro (AD-5, AD-6, NFR-8)

### Historia 3.3: Entrar con Google o con correo, o seguir sin cuenta

Como usuario,
quiero entrar con mi cuenta de Google o con mi correo, o seguir mirando sin cuenta,
para empezar rápido y sin contraseñas nuevas.

**Criterios de aceptación:**

**Dado** la hoja de cuenta
**Cuando** elijo "Continuar con Google" o "Con correo"
**Entonces** inicio sesión con Firebase Authentication sin que la app gestione contraseñas propias (FR37, UX-DR14)

**Dado** "Ahora no"
**Cuando** sigo sin cuenta
**Entonces** puedo ver el mapa y las fichas, buscar y navegar (FR38)

**Dado** una acción que necesita cuenta (aportar, favoritos, recompensas)
**Cuando** la toco sin cuenta
**Entonces** se abre la hoja de cuenta y, tras entrar, vuelvo a esa acción (FR38, UX-DR14)

**Dado** un error al iniciar sesión
**Cuando** ocurre
**Entonces** veo un mensaje claro con reintento y la app sigue usable sin cuenta (UX-DR16)
**Y** los avisos breves (toast) duran 3 s y TalkBack los anuncia (UX-DR15)

### Historia 3.4: Elegir cuenta de cliente o de negocio

Como usuario que se registra,
quiero elegir si soy cliente o negocio,
para usar la parte de la app que me corresponde.

**Criterios de aceptación:**

**Dado** el primer inicio de sesión
**Cuando** elijo Cliente o Negocio
**Entonces** el tipo se guarda en mi cuenta (FR39)

**Dado** una cuenta de negocio
**Cuando** uso la app
**Entonces** no recibo preguntas de llegada, no tengo monedas ni tienda, y las reglas de Firestore me impiden crear visitas, aportes o avisos (FR39, AD-6)

### Historia 3.5: Perfil y ajustes

Como usuario con cuenta,
quiero un perfil con mis ajustes,
para gestionar mi cuenta y cómo funciona la app.

**Criterios de aceptación:**

**Dado** el avatar de la cápsula
**Cuando** lo toco
**Entonces** se abre el perfil con mi nombre y un acceso a Ajustes, preparado para que las siguientes épicas añadan sus secciones (tienda, actividad)

**Dado** Ajustes
**Cuando** lo abro
**Entonces** veo las secciones Apariencia, Navegación y voz, Notificaciones, Privacidad y ubicación, Accesibilidad, Idioma y Ayuda (FR58)
**Y** "Reducir animaciones" en Accesibilidad quita las transiciones de hojas y el movimiento suave de la cámara (UX-DR19)

**Dado** "Cerrar sesión"
**Cuando** lo confirmo
**Entonces** vuelvo al uso sin cuenta (FR42)

**Dado** "Borrar cuenta"
**Cuando** lo confirmo en dos pasos que explican qué se borra
**Entonces** se borran mi cuenta y mis datos personales de Firebase (FR42, UX-DR16)

### Historia 3.6: Favoritos y progreso en la nube

Como cliente,
quiero guardar mis sitios favoritos y recuperar mi progreso en otro móvil,
para no perder nada.

**Criterios de aceptación:**

**Dado** la ficha de un lugar o de un punto libre
**Cuando** pulso el corazón
**Entonces** se guarda como favorito, con un nombre propio si es un punto libre ("Casa de mamá"), y aparece en la hoja del buscador (FR10, FR17)

**Dado** mi progreso (favoritos, ajustes y lo que añadan las siguientes épicas)
**Cuando** entro con mi cuenta en otro móvil
**Entonces** lo recupero (FR40)
**Y** cada servicio escribe solo sus campos de `usuarios/<uid>` con actualizaciones por campo, nunca sobrescribiendo el documento entero (AD-18)

## Épica 4: Aportar el precio al llegar

El cliente que navega hasta un lugar con precios recibe, una hora después, la pregunta de llegada; aporta o confirma el precio, lo deja pendiente si quiere, y gana monedas, XP y nivel. Los aportes son anónimos y, en cuanto hay 3, manda lo que pagan los clientes.

### Historia 4.1: Reglas que protegen los aportes y el anonimato

Como cliente,
quiero que nadie pueda saber qué he aportado ni hacer trampas con los precios,
para confiar en la app y en sus datos.

**Criterios de aceptación:**

**Dado** `firebase/firestore.rules`
**Cuando** se amplían con las colecciones de visitas y aportes
**Entonces** la visita solo se crea en `usuarios/<uid>/visitas/<lugarId>_<AAAAMMDD>` (como mucho una por lugar y día), y un aporte público en `lugares/<id>/aportes` solo se crea si existe esa visita propia, de menos de 24 h y con `aporteHecho == false`, marcándola en el mismo lote (`getAfter`) junto con la copia privada en `usuarios/<uid>/aportes` (AD-6)
**Y** el aporte público tiene exactamente `lugarId`, `conceptoId`, `forma`, `importeCent` o `minCent`/`maxCent` y `creado`, sin ningún dato del autor; cualquier otro campo se rechaza (AD-17, FR30)

**Dado** los aportes ya creados
**Cuando** alguien intenta modificarlos o borrarlos
**Entonces** las reglas lo rechazan (AD-4)

**Dado** una cuenta de negocio
**Cuando** intenta crear una visita o un aporte
**Entonces** las reglas lo rechazan (FR39)
**Y** cada uno de estos casos tiene su prueba con el emulador de Firestore, y todas pasan

### Historia 4.2: La visita queda verificada al acercarme

Como cliente que navega con la app,
quiero que mi visita cuente aunque no llegue a pulsar nada,
para poder aportar el precio después.

**Criterios de aceptación:**

**Dado** una ruta iniciada con la app hacia un lugar con precios
**Cuando** el tiempo restante estimado llega a 5 minutos o menos
**Entonces** `ServicioVisitas` crea la visita verificada, aunque luego termine la ruta antes de llegar o cierre la app (FR22, AD-9)

**Dado** una ruta terminada con más de 5 minutos restantes
**Cuando** la cierro
**Entonces** no se crea visita (FR22)

**Dado** una ruta hacia un punto libre, una dirección o un lugar sin plantilla de precio
**Cuando** me acerco
**Entonces** no se crea visita
**Y** ningún otro módulo crea ni descarta visitas: la navegación solo envía eventos a `ServicioVisitas` (AD-9)

### Historia 4.3: La pregunta llega una hora después, o la retomo con el botón €

Como cliente,
quiero que la app me pregunte cuando ya he recibido el servicio, o retomarlo cuando me venga bien,
para no tener que contestar en el momento.

**Criterios de aceptación:**

**Dado** una visita verificada
**Cuando** se crea
**Entonces** se programa una notificación local para una hora después (Mobile Notifications) que, al tocarla, abre la pregunta de esa visita aunque la app estuviera cerrada (FR23, AD-9)

**Dado** que a esa hora hay una ruta en curso u otra pregunta abierta
**Cuando** llega el momento
**Entonces** la visita pasa a pendientes sin interrumpir (FR23)

**Dado** visitas pendientes
**Cuando** miro el mapa
**Entonces** el botón € muestra un punto rojo; con una pendiente abre su pregunta y con varias me deja elegir; sin pendientes está desactivado (FR29, UX-DR6)

**Dado** las notificaciones denegadas
**Cuando** se crea una visita
**Entonces** va directa a pendientes (UX-DR16)

### Historia 4.4: Confirmar lo que pagué

Como cliente,
quiero contestar en pocos toques si fui y si el precio era el que decía la app,
para aportar sin esfuerzo.

**Criterios de aceptación:**

**Dado** la pregunta de llegada de un lugar con precio
**Cuando** la abro
**Entonces** sigue "¿Has ido?" → "¿Qué usaste?" (solo si hay varios conceptos, e incluye sus variantes) → "¿Te costó X €?" (FR24, FR60)

**Dado** que respondo "Sí" al precio
**Cuando** sigo
**Entonces** se guarda la confirmación en un mismo lote: el aporte público sin autor, la marca `aporteHecho` de la visita y la copia privada (AD-6, AD-17, FR30), y la pregunta termina con un mensaje de agradecimiento

**Dado** cualquier paso
**Cuando** pulso "Atrás" o "Más tarde"
**Entonces** vuelvo al paso anterior o la visita queda pendiente (FR24, FR29)
**Y** todos los pasos funcionan con TalkBack y con el botón atrás de Android (UX-DR17, UX-DR22)

### Historia 4.5: Decir otro precio con la regla

Como cliente,
quiero indicar lo que pagué si no coincide,
para corregir el precio de forma rápida.

**Criterios de aceptación:**

**Dado** que respondo "No, fueron…"
**Cuando** aparece la regla de precio
**Entonces** puedo deslizarla de 1 € en 1 €, tocar el número para escribirlo con decimales o elegir uno de los precios ya aportados por otros (FR25, UX-DR10)

**Dado** un importe
**Cuando** lo envío
**Entonces** se aceptan céntimos y no hay tope, pero no se acepta 0 ni negativos (FR25, NFR-6)

**Dado** TalkBack activo
**Cuando** uso la regla
**Entonces** es un control ajustable que sube o baja 1 € por gesto (UX-DR10)

### Historia 4.6: Tramos de gasto en hostelería

Como cliente de un bar o restaurante,
quiero elegir cuánto gasté por persona entre unos tramos,
para aportar sin que la app me sugiera un número.

**Criterios de aceptación:**

**Dado** la pregunta en un lugar de hostelería
**Cuando** llego al paso del precio
**Entonces** elijo un tramo por persona: menos de 10, 10-20, 20-30, 30-50 o más de 50 €, sin que se proponga ningún precio (FR26)
**Y** el tramo se guarda como `minCent`/`maxCent`, con `maxCent` nulo en "Más de 50 €" (AD-10, AD-17)

### Historia 4.7: Ser el primero en dar un precio

Como cliente que llega a un sitio sin precio, como ChemaBarber,
quiero aportar el primer precio,
para que los demás lo sepan antes de ir.

**Criterios de aceptación:**

**Dado** la pregunta en un lugar sin precio
**Cuando** llego al paso del precio
**Entonces** elijo un concepto sugerido por su tipo (por ejemplo, "Corte de pelo") o escribo otro con su unidad, e indico lo que pagué con la regla (FR27)

**Dado** que envío el primer precio
**Cuando** se guarda
**Entonces** la recompensa muestra "¡Pionero!" (FR27, UX-DR11)

### Historia 4.8: Valorar si quiero

Como cliente de hostelería u ocio,
quiero poder valorar el sitio sin que sea obligatorio,
para dar mi opinión solo cuando me apetezca.

**Criterios de aceptación:**

**Dado** un lugar de hostelería
**Cuando** llego a la valoración
**Entonces** puedo puntuar calidad, servicio y ambiente de 1 a 5 estrellas, o saltarla (FR28)

**Dado** un lugar de ocio
**Cuando** llego a la valoración
**Entonces** puedo puntuar instalaciones, atención y calidad-precio, o saltarla (FR28)

**Dado** un servicio local
**Cuando** termino de aportar
**Entonces** no se pide valoración
**Y** las estrellas se manejan con TalkBack (UX-DR17)

### Historia 4.9: Mi aporte llega aunque no tenga conexión, y lo ven todos

Como cliente,
quiero que mi aporte llegue a los demás aunque no tenga conexión en ese momento,
para no perder lo que aporto.

**Criterios de aceptación:**

**Dado** cualquier aporte de las historias 4.4 a 4.7
**Cuando** se guarda
**Entonces** usa el mismo lote de la Historia 4.4 y nunca incluye datos del autor en la parte pública (AD-6, AD-17, FR30)

**Dado** que no hay conexión
**Cuando** envío
**Entonces** el aporte queda en la cola de Firestore, la recompensa se muestra y se confirma al sincronizar, con un aviso breve (AD-8, UX-DR15, UX-DR16)

**Dado** otro usuario con la ficha de ese lugar
**Cuando** se sincroniza
**Entonces** ve el aporte en menos de 1 minuto (FR36)

### Historia 4.10: Mandan los clientes

Como usuario,
quiero que el precio mostrado sea el que pagan de verdad los clientes cuando hay suficientes aportes,
para fiarme más de él que de lo declarado.

**Criterios de aceptación:**

**Dado** los aportes de un concepto o variante
**Cuando** el Dominio calcula el precio
**Entonces** usa la mediana si es fijo, del percentil 25 al 75 si es rango y un percentil bajo (10-20) si es "desde" (FR31)

**Dado** al menos 3 aportes en los últimos 6 meses
**Cuando** se calcula
**Entonces** el precio de los clientes sustituye al de la web o al declarado, y la ficha muestra "según N clientes" (FR32, AD-3)
**Y** el precio nunca se guarda en Firestore, y cada regla tiene su prueba EditMode (AD-3, AD-14)

### Historia 4.11: Ganar monedas, XP y subir de nivel

Como cliente,
quiero recibir monedas y experiencia por aportar,
para sentir que mi ayuda cuenta.

**Criterios de aceptación:**

**Dado** que termino una pregunta
**Cuando** se muestra la recompensa
**Entonces** gano: confirmar +10 monedas y +15 XP; precio distinto +15 y +20; pionero +25 y +30; valorar +5 y +10; y +5 o +10 monedas extra si confirmé un precio "podría haber cambiado" o "antiguo" (FR48, AD-14)
**Y** monedas y XP se suman con incremento atómico en mi documento (AD-18)

**Dado** el final de la pregunta
**Cuando** termina
**Entonces** el mensaje de agradecimiento de la Historia 4.4 pasa a ser la pantalla de recompensa, con la moneda "+N", la barra de XP y el botón "Volver al mapa" (UX-DR11)

**Dado** que acumulo 150 XP
**Cuando** subo de nivel
**Entonces** el avatar muestra el nuevo nivel con el anillo de XP reiniciado (FR49, UX-DR12)
**Y** con "reducir animaciones" la moneda y el anillo no se animan (UX-DR19)

## Épica 5: Tienda, insignias y reputación

El cliente gasta sus monedas en cosméticos que se ven en el mapa, gana insignias, sube de reputación y oye la recompensa.

### Historia 5.1: Comprar y equipar cosméticos en la tienda

Como cliente,
quiero gastar mis monedas en cosméticos,
para personalizar mi mapa.

**Criterios de aceptación:**

**Dado** la nueva sección Tienda del perfil y el nuevo botón "Ir a la tienda" en la pantalla de recompensa
**Cuando** la abro
**Entonces** veo los cosméticos de posición (clásica, dorada 150, neón 200) y de ruta (clásica, discontinua 100, dorada 250) con su estado: equipado, comprar o equipar (FR50, UX-DR12)

**Dado** monedas suficientes
**Cuando** compro
**Entonces** se descuentan con incremento atómico y el cosmético pasa a ser mío (AD-18)

**Dado** monedas insuficientes
**Cuando** miro un cosmético
**Entonces** el botón está desactivado con "Te faltan N monedas" (UX-DR16)

### Historia 5.2: Ver mis cosméticos en el mapa

Como cliente,
quiero ver al momento el cosmético que equipo,
para notar el premio.

**Criterios de aceptación:**

**Dado** un cosmético de posición equipado
**Cuando** vuelvo al mapa
**Entonces** el círculo, el cono y la flecha de mi posición lo usan (FR50, UX-DR8)

**Dado** un cosmético de ruta equipado
**Cuando** navego
**Entonces** la línea de la ruta lo usa (FR50, UX-DR8)

### Historia 5.3: Ganar insignias

Como cliente,
quiero ganar insignias por mis hitos,
para tener metas.

**Criterios de aceptación:**

**Dado** mis aportes
**Cuando** alcanzo un hito (primer precio, explorador, 10 aportes, 50 aportes, racha de 7 días, crítico)
**Entonces** gano la insignia y se guarda en mi progreso (FR51)

**Dado** la sección de insignias del perfil
**Cuando** la abro
**Entonces** las ganadas se ven con su icono y las demás bloqueadas con candado; al tocarlas veo cómo se ganan (FR51, UX-DR12)

### Historia 5.4: Mi reputación

Como cliente,
quiero subir de reputación con mis aportes,
para que mis avisos cuenten más.

**Criterios de aceptación:**

**Dado** mis aportes
**Cuando** llego a 5 o a 20
**Entonces** mi reputación pasa de Nueva a Fiable o a Experta y se guarda en mi progreso (FR52, AD-14)

**Dado** mi perfil
**Cuando** lo abro
**Entonces** veo mi reputación junto a mi nivel (FR52)

### Historia 5.5: Sonidos de recompensa

Como cliente,
quiero oír la llegada, la pregunta y las monedas,
para que la app se sienta viva, y poder silenciarla.

**Criterios de aceptación:**

**Dado** la llegada, la aparición de la pregunta o las monedas ganadas
**Cuando** ocurren
**Entonces** suena un sonido corto y suave, acompañado siempre de un cambio visible y de un anuncio de TalkBack (FR53, UX-DR21)

**Dado** la burbuja del altavoz encima del botón € o el modo silencio del móvil
**Cuando** silencio
**Entonces** no suena nada (FR53, UX-DR6)

## Épica 6: Tu actividad y la evaluación del TFG

El cliente consulta su actividad completa y, con su consentimiento, la app registra de forma anónima los datos que necesitan las pruebas con usuarios y las métricas de la memoria.

### Historia 6.1: Ver mi actividad

Como cliente,
quiero ver lo que tengo pendiente y lo que he aportado,
para seguir mi contribución.

**Criterios de aceptación:**

**Dado** la sección "Tu actividad" del perfil
**Cuando** la abro
**Entonces** veo mis visitas pendientes, mis aportes (desde mi copia privada), mis favoritos y mis insignias (FR57)

**Dado** que aún no he aportado nada
**Cuando** abro la actividad
**Entonces** veo "Aún no has aportado precios. Navega a un sitio con la app y te preguntaremos al llegar." (UX-DR16, UX-DR20)

### Historia 6.2: Registro anónimo para la evaluación

Como participante en las pruebas del TFG,
quiero que mis datos de uso se registren solo si lo acepto y sin identificarme,
para ayudar a la investigación sin perder privacidad.

**Criterios de aceptación:**

**Dado** la pantalla de consentimientos y el ajuste de Privacidad
**Cuando** acepto participar en la evaluación
**Entonces** la app genera un identificador anónimo por instalación y empieza a registrar eventos; si no acepto, no registra nada (FR59, AD-16)

**Dado** el consentimiento dado
**Cuando** ocurre una visita verificada, una pregunta respondida o aplazada, un aporte, una confirmación o una compra en la tienda
**Entonces** se registra por el puerto `IRegistroEventos` con su nombre de la lista cerrada (`visita_verificada`, `pregunta_respondida`, `pregunta_aplazada`, `aporte_creado`, `confirmacion`, `compra_tienda`), sin uid ni ubicación exacta, en `evaluacion/<idAnonimo>/eventos` (AD-16)
**Y** las reglas de Firestore solo permiten crear esos eventos, nunca leerlos ni modificarlos desde la app

### Historia 6.3: Exportar los datos de la evaluación

Como autor del TFG,
quiero exportar los eventos por participante,
para calcular las métricas de la memoria.

**Criterios de aceptación:**

**Dado** los eventos en `evaluacion/`
**Cuando** ejecuto el script de exportación del administrador en `firebase/`
**Entonces** obtengo un CSV por participante anónimo con sus eventos y fechas, y un resumen con la tasa de aporte sobre visitas verificadas (SM-2), el uso de la tienda (SM-3) y la proporción de precios "al día" (SM-4) (FR59)

## Épica 7: Precios fiables y al día

El cliente avisa de un precio incorrecto, ve qué precios están "en revisión" y la pregunta de llegada le pide confirmarlos, y recibe un aviso cuando cambia el precio de un lugar que vigila.

### Historia 7.1: Avisar de que un precio está mal

Como cliente,
quiero avisar de que un precio ha cambiado o de que el sitio ya no ofrece algo,
para que el dato no engañe a nadie.

**Criterios de aceptación:**

**Dado** la ficha de un lugar con cuenta de cliente
**Cuando** pulso "¿Algún precio está mal?"
**Entonces** elijo entre "el precio ha cambiado", "ya no se ofrece este servicio" o "el negocio ha cerrado" y se crea un aviso sin autor con `lugarId`, `conceptoId`, `tipo`, `peso` y `creado` (FR34, FR10, AD-17)

**Dado** mi reputación
**Cuando** se crea el aviso
**Entonces** su peso es ×1 (Nueva), ×1,5 (Fiable) o ×2 (Experta), y las reglas de Firestore rechazan un peso que no coincida con la reputación de mi progreso (FR34, AD-6)

### Historia 7.2: Precios en revisión que se confirman al llegar

Como usuario,
quiero saber cuándo un precio está en duda y ayudar a confirmarlo,
para que se corrija pronto.

**Criterios de aceptación:**

**Dado** los avisos de un concepto
**Cuando** suman 3 puntos de peso posteriores al último aporte
**Entonces** el Dominio marca el precio como "en revisión" y la ficha muestra la etiqueta "En revisión" con una explicación breve (FR35, AD-3, UX-DR16)

**Dado** un precio en revisión
**Cuando** un cliente con visita verificada llega a la pregunta
**Entonces** se le pide confirmar ese precio; al confirmarlo o aportar otro, la revisión se cierra (FR35)
**Y** el cálculo tiene su prueba EditMode (AD-14)

### Historia 7.3: Avísame si cambia

Como usuario,
quiero que la app me avise cuando cambie el precio de un sitio que me interesa,
para no tener que mirarlo cada día.

**Criterios de aceptación:**

**Dado** la ficha de un lugar
**Cuando** pulso "Avísame si cambia" (o "Avísame cuando tenga precio")
**Entonces** el lugar se guarda en mis avisos activos con el precio que veo en ese momento (FR12, FR10)

**Dado** avisos activos
**Cuando** abro la app o vuelvo a ella y el precio mostrado de ese lugar ha cambiado o ya tiene precio
**Entonces** recibo una notificación local que abre su ficha (FR12, AD-7)

**Dado** mis avisos activos
**Cuando** abro su ficha o la sección "Tu actividad" del perfil
**Entonces** los veo listados y puedo desactivar cada aviso (FR57)

## Épica 8: La cuenta de negocio

El dueño reclama y verifica su local por correo (o con aprobación manual), da de alta uno nuevo, declara precios y variantes, publica ofertas visibles en la ficha y en el marcador, y consulta su panel.

### Historia 8.1: Reclamar y verificar mi local

Como dueño de un negocio,
quiero demostrar que el local es mío,
para poder gestionar sus precios.

**Criterios de aceptación:**

**Dado** el script de sembrado `firebase/sembrar/`
**Cuando** lo ejecuta el administrador
**Entonces** crea `contactosLugares/<id>` con el correo público de cada lugar base que lo tenga (de OpenStreetMap o de su web), de solo lectura para la app (AD-5)

**Dado** la ficha de un lugar sin dueño
**Cuando** la abro con cuenta de negocio, o pulso "¿Es tu negocio?" con otra cuenta
**Entonces** puedo reclamarlo (FR10, FR43)

**Dado** una reclamación
**Cuando** confirmo por enlace el correo de mi cuenta y coincide con `contactosLugares/<id>.email`
**Entonces** las reglas marcan la propiedad como verificada (FR43, AD-6)

**Dado** que el correo no coincide o el lugar no tiene correo público
**Cuando** reclamo
**Entonces** queda pendiente de aprobación manual del administrador en la consola de Firebase, con "Reenviar enlace" si cambio el correo; si el enlace caduca, puedo pedir otro (FR43, UX-DR16)

### Historia 8.2: Dar de alta un local nuevo

Como dueño de un negocio que no aparece en el mapa,
quiero añadir mi local,
para que los clientes lo encuentren.

**Criterios de aceptación:**

**Dado** mi cuenta de negocio y un local que no está en la app
**Cuando** lo doy de alta con nombre, tipo (agrupado por plantilla), ubicación (dirección o mi posición actual) y correo del local
**Entonces** se crea `lugares/alta:<autoId>` como su dueño y aparece en el mapa con el logo de su tipo (FR44, AD-11)
**Y** queda pendiente de verificación como en la Historia 8.1

### Historia 8.3: Declarar mis precios y variantes

Como dueño verificado,
quiero declarar los precios de mi local, con sus variantes,
para que los clientes sepan cuánto cuesta.

**Criterios de aceptación:**

**Dado** mi local verificado
**Cuando** declaro o edito conceptos, precios y variantes (horario, día, tipo de cliente)
**Entonces** se guardan en `lugares/<id>/declaraciones` y la ficha los muestra como "declarado por el negocio" hasta que haya 3 aportes de clientes en 6 meses (FR45, FR60, FR32)

**Dado** un precio menor o igual a 0
**Cuando** intento guardarlo
**Entonces** se rechaza junto al campo sin borrar lo demás (NFR-6)

**Dado** un local no verificado o de otro negocio
**Cuando** intento declarar precios
**Entonces** las reglas de Firestore lo rechazan (AD-6)

### Historia 8.4: Publicar ofertas

Como dueño verificado,
quiero publicar ofertas con fecha de fin,
para atraer clientes.

**Criterios de aceptación:**

**Dado** mi local verificado
**Cuando** publico una oferta con texto, precio opcional y fecha de fin
**Entonces** aparece en la ficha y el marcador muestra una etiqueta "%" mínima en su esquina (FR46, FR3, UX-DR7)

**Dado** una oferta con precio negativo
**Cuando** intento publicarla
**Entonces** se rechaza (NFR-6)

**Dado** una oferta publicada
**Cuando** llega su fecha de fin o 3 clientes avisan de que ya no está vigente (aviso de tipo `oferta_caducada`)
**Entonces** deja de mostrarse (FR46)

### Historia 8.5: Mi panel de negocio

Como dueño verificado,
quiero ver el estado de mis precios y los avisos de los clientes,
para mantener mi información al día.

**Criterios de aceptación:**

**Dado** el perfil de mi cuenta de negocio
**Cuando** lo abro
**Entonces** veo mi local, cada precio con su estado (declarado o confirmado por N clientes), los avisos recibidos y los precios en revisión (FR47)

**Dado** un local sin verificar
**Cuando** abro el panel
**Entonces** explica que debo confirmar el enlace del correo o esperar la aprobación manual, y no puedo editar (UX-DR16)

## Épica 9: Plan de tarde

El cliente monta un plan con varias paradas y personas, ve el gasto estimado y lo recorre parada a parada.

### Historia 9.1: Montar un plan de tarde

Como cliente,
quiero añadir varios sitios a un plan y ordenarlos,
para organizar una salida con amigos.

**Criterios de aceptación:**

**Dado** la ficha de un lugar
**Cuando** pulso "Añadir al plan"
**Entonces** se añade como parada y aparece el botón "Plan" en el mapa (FR54, FR10)

**Dado** la tarjeta del plan
**Cuando** la abro
**Entonces** puedo ordenar las paradas arrastrando o con "Subir" y "Bajar", quitar paradas, elegir el número de personas (1 por defecto) y qué usaré en cada parada (FR54, UX-DR13)
**Y** el plan se guarda en mi progreso por campo (AD-18)

### Historia 9.2: Gasto estimado del plan

Como cliente,
quiero saber cuánto nos va a costar la tarde,
para decidir antes de salir.

**Criterios de aceptación:**

**Dado** un plan con paradas y personas
**Cuando** se calcula en el Dominio
**Entonces** suma la hostelería como rango × personas, las entradas y servicios × personas, y pistas y aparcamientos una vez por grupo, en céntimos (FR55, AD-10)

**Dado** paradas sin precio o con precio "desde"
**Cuando** veo el total
**Entonces** el plan las indica para que el total no parezca completo (FR55)
**Y** el cálculo tiene su prueba EditMode

### Historia 9.3: Recorrer el plan parada a parada

Como cliente,
quiero que la app me lleve de una parada a la siguiente,
para no tener que buscar cada sitio.

**Criterios de aceptación:**

**Dado** un plan
**Cuando** pulso "Empezar ruta"
**Entonces** navega a la primera parada, y al terminar la ruta de una parada se abre la de la siguiente (FR56)
**Y** cada parada con precios crea su visita verificada como cualquier ruta (FR22, AD-9)

**Dado** que no hay conexión
**Cuando** abro el plan
**Entonces** veo el último gasto calculado y la ruta aproximada (UX-DR16)
