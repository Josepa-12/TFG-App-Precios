# Epic 3 Context: Crear tu cuenta y guardar tu progreso

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Dar identidad al usuario sin obligarle a tenerla: acepta la privacidad y decide sus permisos, entra con Google o con correo (o sigue sin cuenta), elige si es cliente o negocio, guarda favoritos y progreso en la nube y gestiona su cuenta desde el esqueleto del perfil y los ajustes. Importa porque introduce Firebase (cuentas, datos y reglas de seguridad) y la separación cliente/negocio, sobre las que se apoyan los aportes de precios, la gamificación y la cuenta de negocio.

## Stories

- Story 3.1: Bienvenida, privacidad y consentimientos
- Story 3.2: Mis datos en Firebase, protegidos y disponibles sin conexión
- Story 3.3: Entrar con Google o con correo, o seguir sin cuenta
- Story 3.4: Elegir cuenta de cliente o de negocio
- Story 3.5: Perfil y ajustes
- Story 3.6: Favoritos y progreso en la nube

## Requirements & Constraints

- **Bienvenida:** solo en el primer arranque y antes del mapa; aviso de privacidad y consentimiento explícito (aceptar o rechazar) de ubicación y de notificaciones. Rechazar no bloquea nada: la app funciona con lo permitido y Ajustes explica cómo activarlo después. Pasada una vez, la app va directa al mapa.
- **Sin cuenta:** mapa, fichas, búsqueda y navegación funcionan siempre. Aportar, recompensas y favoritos piden cuenta; tras entrar se vuelve a la acción que la pidió.
- **Inicio de sesión:** Google como opción principal, o correo; la app no gestiona contraseñas propias. Si falla: mensaje claro con reintento y la app sigue usable sin cuenta.
- **Tipo de cuenta:** se elige Cliente o Negocio en el primer inicio de sesión y se guarda en la cuenta. Una cuenta de negocio no recibe preguntas de llegada, no tiene monedas ni tienda y no puede crear visitas, aportes ni avisos; lo impide el servidor, no solo la app.
- **Cerrar sesión / borrar cuenta:** cerrar sesión (con confirmación) devuelve al uso sin cuenta. Borrar cuenta se confirma en dos pasos que explican qué se borra, y elimina la cuenta y los datos personales de Firebase (RGPD: acceso y borrado).
- **Progreso:** favoritos y ajustes (y lo que añadan las épicas siguientes) se recuperan al entrar en otro móvil.
- **Favoritos:** de lugares y de puntos libres o direcciones, estos con nombre propio ("Casa de mamá"); aparecen en la hoja del buscador y funcionan sin conexión. Las direcciones a las que solo se navega no se guardan en historial.
- **Privacidad:** datos mínimos; ningún seguimiento en segundo plano fuera de una ruta iniciada por el usuario; nunca datos personales en los registros técnicos.
- **Seguridad:** cada usuario solo lee y escribe su documento y sus subcolecciones; nadie modifica progreso, monedas o reputación ajenos. Se prueba con el emulador de Firestore.
- **Coste:** solo servicios gratuitos o de capa gratuita.
- **Accesibilidad (cada historia):** contraste ≥ 4,5:1 en claro y oscuro, zonas táctiles ≥ 48 dp, TalkBack, fuente del sistema sin cortes, "reducir animaciones" respetado, textos localizables en `es-ES`.

## Technical Decisions

- **Firebase en plan Spark:** solo Authentication (Google y correo) y Cloud Firestore; sin Cloud Functions, push de servidor ni SMS. Un único proyecto de Firebase para el TFG. Firebase Unity SDK 14.1.0 es la versión prevista, pero las fuentes se contradicen: fijarla y anotarla al instalar desde el registro oficial.
- **Aislamiento:** Firebase vive solo en el adaptador de Firebase (`Adaptadores/Firebase`), detrás de puertos de Aplicación; `DefaultInstance` solo aparece ahí. La UI nunca lo ve: observa estado y eventos de los servicios y envía comandos. Composición solo en `Arranque`, sin singletons.
- **Servicio:** la lógica de cuentas vive en `ServicioCuentas`. Puertos `I<Algo>`, adaptadores `<Tecnologia><Puerto>`, `Resultado<T>` con código de error (la UI lo traduce a texto), `async`/`Task` con vuelta al hilo principal solo en Presentación.
- **Sin conexión:** la persistencia de Firestore activada es la única caché de datos del cliente; las escrituras se encolan y la UI las muestra al momento. No crear otra base local (la única caché propia es la de teselas).
- **Documento `usuarios/<uid>`:** lo escribe solo el propio usuario y siempre por campo (merge); prohibido sobrescribirlo entero. Dueños de campos: `ServicioCuentas` (ajustes y tipo de cuenta), `ServicioLugares` (favoritos y avisos activados); `ServicioProgreso` y `ServicioPlan` llegarán en épicas posteriores, con incremento atómico para monedas y XP.
- **Borrado:** aportes, avisos y visitas son inmutables desde la app; el borrado de cuenta es la única excepción. Los aportes y avisos públicos no llevan autor.
- **Reglas:** `firebase/firestore.rules` versionado, desplegado con Firebase CLI y probado con emulador; es la frontera de seguridad. La restricción de negocio exige que las reglas puedan conocer el tipo de cuenta.
- **Datos:** colecciones en plural y minúsculas, campos `camelCase` en castellano, ids estables en texto como clave (`osm:<tipo>:<id>`, slug, `alta:<autoId>`), `creado = ServerTimestamp` en todo documento creado. El Dominio recibe "ahora" como parámetro; Aplicación le pasa la hora del servidor estimada o, si nunca hubo conexión, la local.
- **Distribución:** APK instalada a mano o Firebase App Distribution, sin Google Play; Android minSdk 26 / targetSdk 35.

## UX & Interaction Patterns

- **Hoja de cuenta:** "Continuar con Google" (primario), "Con correo" (secundario) y "Ahora no" (texto). Se llega desde la bienvenida, el perfil sin cuenta o una acción que exige cuenta. Un solo botón primario por pantalla.
- **Perfil:** se abre tocando el avatar de la cápsula (círculo de 40 px; en épicas posteriores muestra el nivel con anillo de XP). De momento, nombre y acceso a Ajustes, preparado para añadir tienda y actividad. Estado vacío: "Aún no has aportado precios. Navega a un sitio con la app y te preguntaremos al llegar."
- **Ajustes:** Apariencia, Navegación y voz, Notificaciones, Privacidad y ubicación, Accesibilidad, Idioma y Ayuda (cómo se calculan los precios), más cerrar sesión y borrar cuenta. "Reducir animaciones" propio, además del del sistema, quita transiciones de hojas y movimiento suave de cámara. El tema inicial sigue al sistema y se cambia con su botón o en Ajustes.
- **Permiso denegado:** el mapa se centra en Sanlúcar y el botón de ubicación explica cómo activarla.
- **Buscador:** sin favoritos ni frecuentes, solo categorías y "Encuentra cerca".
- **Toast:** 3 s, anunciado por TalkBack como región en vivo, sin tapar Ir ni la fila de acciones; nada imprescindible va solo ahí.
- **Capas:** todo nace del mapa, sin pestañas; máximo dos hojas apiladas; atrás de Android cierra la superior. Foco al título al abrir y de vuelta a su origen al cerrar.
- **Estilo y tono:** colores y medidas solo de tokens; iconos vectoriales, nunca emojis; solo vertical. Tuteo, frases cortas, sin culpar al usuario ("No hay conexión. Te mostramos lo último que guardaste."). Prohibidos anuncios y notificaciones para volver a la app.
- **Referencias:** capturas del prototipo `01-bienvenida`, `01b-crear-cuenta`, `19-perfil`, `21-ajustes-apariencia-oscuro`, `27-cuenta-negocio` y maqueta `key-perfil-tienda`; mandan los documentos de diseño.

## Cross-Story Dependencies

- 3.2 (Firebase, reglas, puertos) es la base de 3.3 y, por ella, de 3.4, 3.5 y 3.6. Requisito externo: proyecto de Firebase y `google-services.json`.
- 3.1 no depende de Firebase y enlaza con la hoja de cuenta de 3.3; los permisos rechazados se explican en los Ajustes de 3.5.
- 3.4 amplía las reglas de 3.2; las reglas completas de visitas y aportes llegan en la Épica 4 y deben mantener el veto a negocios.
- 3.6 usa el "volver a la acción" de 3.3, la ficha (Épica 1), y la ficha de punto libre y la hoja del buscador (Épica 2).
- Épicas posteriores: aportes y visitas (4), monedas, tienda e insignias en el perfil (5), actividad y consentimiento del registro anónimo (6), "Avísame si cambia" (7), verificación y panel de negocio (8), plan (9).
