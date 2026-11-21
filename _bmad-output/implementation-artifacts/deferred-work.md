- source_spec: `_bmad-output/implementation-artifacts/spec-1-2b-gestos-y-teselas-alrededor-de-la-vista.md`
  summary: Sacar la entrada de `CamaraMapa` de `Input` estático a algo simulable en EditMode y probar: el paso que aplica el gesto (`Tactil`: signo de la inclinación, sin zoom ni giro al inclinar) y las ramas de `ToqueTactil`/`ToqueRaton` (empezar sobre la interfaz cancela, segundo dedo anula, solo dedos en el móvil).
  evidence: Revisiones 1.2b y 1.8 (parte 1, hallazgo 4). Solo se prueban el clasificador y `DetectorToque`; invertir `if (gestoSobreInterfaz)` no rompe ninguna prueba.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Probar la rama real del Arranque (no la de captura) con una fábrica estática: carga de lugares («PriceMap: 124 lugares cargados»), Valhalla principal + OSRM de reserva con espera real, Photon y el resolutor de enlaces.
  evidence: `PruebasArranque` pasa a `Componer` un repositorio vacío; ninguna prueba construye la rama que no es de captura (revisiones 1.4 y 2.6; maybe-false, medium si es cierto).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Pruebas automáticas de `exportar-datos-unity.js` (tarifas de un lugar inexistente, ids repetidos, plantillas desconocidas).
  evidence: Revisión 1.4 (parte 1). No hay arnés de Node; se comprobó a mano. Conviene cuando `WEB_PRICES` tenga más negocios.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Ignorar en git `unity/PriceMap/Assets/AddressableAssetsData/link.xml`, que cada build regenera.
  evidence: Se borró en `58e4bbb` y vuelve tras cada build de PC; hoy no está en `.gitignore`.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4b-marcadores-de-los-lugares-en-el-mapa.md`
  summary: En Android real con TalkBack, los marcadores se anuncian y sus nodos aparecen/desaparecen al mover el mapa; a ojo, al arrastrar, no van un fotograma por detrás.
  evidence: Revisión 1.4 (parte 2). Con el lector apagado `AnadirMarcas`/`ActualizarMarcadores` no corren; sin pruebas PlayMode de Update/LateUpdate; la captura mide con la cámara quieta (0,5 px).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-filtro-precios-en-el-mapa.md`
  summary: Con toques reales (panel o móvil): la píldora abre el menú, una opción filtra y tocar fuera cierra (`Clickable` del velo, la píldora y las opciones).
  evidence: Revisión 1.5 (parte 1). Las pruebas llaman a `AlternarMenu`/`CerrarMenu`/`Pulsar` directamente.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5b-menu-precios-con-talkback-y-boton-atras.md`
  summary: En Android real: atrás cierra el menú «Precios» y, cerrado, no saca de la app; TalkBack lee el anuncio y lleva el foco a la píldora al elegir.
  evidence: Revisión 1.5 (parte 2). Las pruebas llaman a `Atras()`; `KeyCode.Escape` no se simula en EditMode; `Input.backButtonLeavesApp` = false por defecto.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-6b-repetidos-solapes-y-talkback-de-los-puntos.md`
  summary: Con panel dispuesto o en el móvil: la caja de solape de un punto usa la medida real de su nombre (no la estimada) y los puntos no parpadean al mover el mapa.
  evidence: Revisión 1.6 (parte 2). EditMode no tiene disposición; solo lo comprueba la captura (0 pares solapados).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-6c-talkback-de-los-puntos-de-interes.md`
  summary: En `PruebasTextos`, comparar el valor de cada clave de `ClavesTexto.Todas` en `Textos_es-ES.asset` con `PrepararInterfaz.TextosEs[clave]` (p. ej. `LaTablaTieneLosTextosDelGenerador`), no solo que exista.
  evidence: Revisiones 1.6 (parte 3) y 1.8 (parte 1, hallazgo 10). Hoy solo `ContainsKey`; las pruebas usan `TextosEs` (`Textos.Forzados`), así que un `{1}` perdido en `ficha.precio.rango` pasaría todo.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-ficha-del-lugar-con-sus-precios.md`
  summary: Ver funcionando el desplazamiento interno de la ficha cuando no cabe (sin barras, ScrollView sin tema por defecto). Sin verificar; si falla, medium.
  evidence: Revisión 1.8 (parte 1, hallazgo 11). Clínica Areas cabe en el 68 %; hace falta un lugar sintético con muchos conceptos en la captura o el móvil.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8b-variantes-y-centrado-de-la-ficha.md`
  summary: Probar el recentrado de la ficha al recibir la hoja su medida real (`RecentrarFichaPendiente`) y al pasar de una ficha a otra (caso A→B en la captura o arnés PlayMode con panel).
  evidence: Revisión 1.8 (parte 2, hallazgo 5). Quitar el `RegisterCallback` o `if (yaAbierta)` no rompe ninguna prueba EditMode.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8c-talkback-de-la-ficha.md`
  summary: En móvil con TalkBack: doble toque en un marcador abre la ficha; deslizar recorre solo la ficha y la desplaza hasta el foco; ver si llega a nodos fuera de lo visible o dibuja el marco sobre el mapa; atrás devuelve el foco al marcador. Si falla, medium.
  evidence: Revisión 1.8 (parte 3, hallazgo 6). `frameGetter` da el marco sin recortar al `ScrollView`.
- source_spec: `_bmad-output/implementation-artifacts/spec-mapa-sin-solapes-con-precios.md`
  summary: Dos lugares reales comparten coordenada: uno queda oculto a cualquier zoom (ni toque ni TalkBack). Corregir el dato o decidir cómo mostrarlos (agrupar o desplazar).
  evidence: Revisión de solapes, hallazgo 8. `PruebasFiltro` espera 123 de 124 lugares por esa pareja.
- source_spec: `_bmad-output/implementation-artifacts/spec-mapa-sin-solapes-con-precios.md`
  summary: Probar en EditMode con panel dispuesto las zonas reales de los controles (`ZonasControles`) y que las filas de la ficha no se encogen.
  evidence: Revisión de solapes, hallazgo 9. Quitar `Ocupar(pildora)` o los `flex-shrink: 0` no rompe ninguna prueba.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-gps-activo-con-la-pantalla-apagada-durante-una-ruta.md`
  summary: En Android real: al iniciar la ruta sale la notificación fija «Navegando a …»; con la pantalla apagada 2 min, al volver llegan las posiciones en orden; al terminar desaparece. En Android 13+, la petición de `POST_NOTIFICATIONS` y el aviso si se deniega. Hasta la 2.7 nada llama a `IniciarRuta` (hace falta esa historia o un disparador temporal).
  evidence: Decisión del 19/11/2026 (spec 2.1). EditMode solo prueba con un servicio falso; el plugin Java solo se ha compilado.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-gps-activo-con-la-pantalla-apagada-durante-una-ruta.md`
  summary: `IUbicacion` solo entrega la coordenada de las posiciones de la ruta: se pierden rumbo de la marcha, velocidad, precisión y hora, que ya lee `LecturaRuta`.
  evidence: `UbicacionGps.RecogerRuta` usa solo `lectura.Posicion` e `IUbicacion` no tiene hora; la 2.7 (flecha con velocidad > 1 m/s) y la 4.2 necesitan distinguir una posición antigua de la cola de una actual.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-gps-activo-con-la-pantalla-apagada-durante-una-ruta.md`
  summary: La notificación de navegación usa el icono de la app a color como icono pequeño (mancha blanca en la barra); hace falta uno monocromo.
  evidence: `ServicioUbicacionRuta.notificacion` usa `setSmallIcon(getApplicationInfo().icon)`. Comprobar con la prueba en Android real.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-hoja-del-buscador-desplegable.md`
  summary: En el móvil, gestos reales de la hoja: arrastrar la lista frente a la hoja, tocar el avatar, soltar fuera y que el teclado salga al tocar.
  evidence: Sin panel en EditMode; las pruebas llaman a Bajar/Mover/Soltar sin pasar por los manejadores de `HojaBuscador`.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-hoja-del-buscador-desplegable.md`
  summary: Añadir a `PruebasContraste` los pares del buscador sin medir: texto de ayuda sobre `--color-capsula` y `--color-buscador-campo` (claro y oscuro), texto de los chips y de «Encuentra cerca», iconos blancos sobre colores de categoría, y en los resultados accent sobre surface-2 y muted en oscuro (revisar qué cubre ya la comprobación de `poi-*-texto`).
  evidence: Diffs de 2.2 y 2.3 (maybe-false, medium si es cierto). Ninguna prueba nombra esos tokens.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2b-talkback-y-reducir-animaciones-del-buscador.md`
  summary: Que TalkBack anuncie el campo del buscador como campo de búsqueda (`AccessibilityRole.SearchField`) y lea lo escrito.
  evidence: Hoy es un Button con la etiqueta fija «Buscar en Sanlúcar»; `AccesibilidadPantalla` solo crea botones y textos.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2b-talkback-y-reducir-animaciones-del-buscador.md`
  summary: Aplicar «quitar animaciones» del sistema también a los movimientos de cámara (centrar en la ficha o en mi posición, y el encuadre de la ruta), no solo a la hoja (UX-DR19).
  evidence: `IAccesibilidadSistema` solo lo consulta la hoja; `CamaraMapa` no mira el ajuste.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2b-talkback-y-reducir-animaciones-del-buscador.md`
  summary: En móvil con TalkBack, la hoja del buscador (abrir desde la cápsula, recorrido, Expandir/Contraer, atrás) y el ajuste de Android «quitar animaciones».
  evidence: En EditMode, TalkBack se simula con `LectorForzado` y la escala con un puerto falso.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-buscar-lugares-puntos-de-interes-y-direcciones.md`
  summary: En el móvil, que arrastrar la lista de resultados no elija una fila sin querer (`Clickable` de `FilaResultado` dentro del ScrollView).
  evidence: Sin verificar (maybe-false, medium si es cierto).
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-buscar-lugares-puntos-de-interes-y-direcciones.md`
  summary: Con TalkBack real, que escribir deprisa no encadene anuncios «N resultados» ni corte el eco del teclado.
  evidence: Sin verificar (maybe-false, medium si es cierto). Solo se anuncia si cambian los resultados, pero el foco se pide a cada tecla con el lector activo.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3b-direcciones-con-photon.md`
  summary: En el móvil, que la lista no salte arriba al llegar las direcciones (`MostrarResultados` con arriba falso).
  evidence: Sin verificar (maybe-false, medium si es cierto). EditMode no lee el `scrollOffset`.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3b-direcciones-con-photon.md`
  summary: Photon no encuentra «calle ancha 10» en Sanlúcar (da Barrameda y calles de otras ciudades); estudiar más peso a la cercanía (`location_bias_scale`, `zoom`) o un bbox suave.
  evidence: Captura del 19/11/2026: los 7 resultados son de Marbella, Motril, etc. Se decidió sin bbox; medir con varias consultas reales.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-4-encuentra-cerca-por-categorias.md`
  summary: Rehacer la lista «Encuentra cerca» con la hoja abierta cuando cargan puntos nuevos o llega la primera posición (hoy solo al desplegar), sin mover el scroll ni el foco.
  evidence: Sin verificar (maybe-false, medium si es cierto). Se ve abriendo una categoría nada más arrancar.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-4-encuentra-cerca-por-categorias.md`
  summary: «Gimnasios» lista también piscinas, golf, pistas y el club náutico, y «Restaurantes» incluye cafeterías y heladerías; decidir si renombrar («Deporte», «Comer»).
  evidence: Etiquetas de la 2.2 frente a la tabla de categorías del 19/11/2026.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-4-encuentra-cerca-por-categorias.md`
  summary: En el móvil, que tocar chips, «Encuentra cerca» y la X funcione y que un arrastre que empieza en un chip no elija la categoría.
  evidence: Sin verificar (maybe-false, medium si es cierto).
- source_spec: `_bmad-output/implementation-artifacts/spec-2-5-ir-a-un-punto-libre-o-a-una-ubicacion-compartida.md`
  summary: Comprobar en Sanlúcar si Photon `/reverse` da calles lejanas al pulsar en el río, el pinar o la playa, y si hace falta `radius` o un límite de distancia antes de usar la calle como título.
  evidence: Sin verificar (maybe-false, medium si es cierto). El adaptador acepta la primera respuesta sin mirar su distancia.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-5-ir-a-un-punto-libre-o-a-una-ubicacion-compartida.md`
  summary: Vibración corta al dispararse la pulsación larga (como Android), y probar el gesto real en el móvil (dedo quieto, temblor, dos dedos, ratón en PC).
  evidence: La entrada de `CamaraMapa` lee `Input` estático (ver el primer pendiente); la vibración no estaba en el spec.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-5b-pegar-enlaces-o-coordenadas.md`
  summary: Ver en el móvil a qué URL llevan hoy los enlaces cortos de «Compartir» de Google Maps (sitio con nombre y chincheta). Si la primera URL larga no trae coordenadas (`?q=Nombre&ftid=…`, `/maps/place/Nombre/data=…!1s0x…`) o las trae como `/maps/search/36.778,+-6.351`, añadir esa forma al analizador o buscar el nombre con Photon.
  evidence: Sin verificar (maybe-false, medium si es cierto). El resolutor para en la primera URL no corta; el analizador lee `@`, `!3d!4d`, `q=`/`ll=`/`query=`; la URL final sale en el registro con `Debug.isDebugBuild`.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-5c-compartir-con-pricemap.md`
  summary: En el móvil, `PuenteCompartir.tomar`: lo compartido se aplica una vez y no vuelve al volver a la app, al abrirla desde recientes ni tras matar el proceso y volver con el icono (la actividad recreada podría traer el intent original). Si vuelve, marcar el intent como leído de otra forma.
  evidence: Sin verificar (maybe-false, medium si es cierto). `setIntent(new Intent())` no sobrevive a la recreación de la actividad.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-5c-compartir-con-pricemap.md`
  summary: Que `geo:0,0?q=Calle Ancha 3` (contactos y calendario) busque esa dirección en vez de dar «No reconozco esta ubicación».
  evidence: Hoy se trata como enlace sin coordenadas (prueba «geo sin coordenadas»); es una decisión de producto.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-6-ver-los-tiempos-y-calcular-la-ruta-mas-corta.md`
  summary: La fila de modos podría volver a pedir los minutos al moverse el usuario o al volver la red tras «—».
  evidence: Hoy solo se piden al abrir la ficha o al pasar la posición a `Activa` (Ir ya calcula desde la posición actual, con caché de 20 m).
- source_spec: `_bmad-output/implementation-artifacts/spec-2-6b-dibujar-la-ruta.md`
  summary: Historia 2.6 (parte 2b): sin conexión (fallan Valhalla y OSRM), ruta aproximada calculada en el móvil por las calles de las teselas guardadas (grafo de la capa de calles de las MVT en caché, sin sentidos únicos), dibujada en un azul más suave, tarjeta de arriba «Ruta aproximada por las calles guardadas», valores con «≈», y un toast nuevo «Sin conexión: ruta aproximada» (DESIGN.md: arriba y centrado, fondo `text`, 3 s, anunciado por TalkBack). Si no hay calles guardadas que unan origen y destino, decidir qué se enseña.
  evidence: El usuario eligió el 20/11/2026 la opción A (ruta por las calles guardadas) porque la recta atraviesa edificios; maqueta v2 aprobada (https://claude.ai/artifact/KnJ3R4DVbfBPPiQSVvwBTM).
- source_spec: `_bmad-output/implementation-artifacts/spec-2-6c-ruta-aproximada-sin-conexion.md`
  summary: Probar que la composición real (rama que no es de captura) da a la pantalla un `ServicioRutas` con `ConRutaAproximada` (servicio local de calles guardadas).
  evidence: Revisión de la 2.6c (verification gap). El servicio se crea en `Arranque.Start`, fuera de `Componer`; quitar `local: callesGuardadas` no rompe ninguna prueba. Mismo hueco que Valhalla/OSRM (pendiente de la 1.4/2.6).
- source_spec: `_bmad-output/implementation-artifacts/spec-2-6c-ruta-aproximada-sin-conexion.md`
  summary: Sin red, con una conexión que se cuelga en vez de fallar, «Calculando la ruta…» puede durar 16–24 s (8 s de Valhalla, 8 de OSRM y 8 de la local en serie); estudiar ir antes a la local con errores de red o `Application.internetReachability`.
  evidence: Revisión de la 2.6c (blind + edge, medium). `ServicioRutas.Calcular` usa `ConLimite` con 8 s en los tres; en modo avión los fallos suelen ser inmediatos.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-6c-ruta-aproximada-sin-conexion.md`
  summary: Medir en un móvil el peor caso de la ruta aproximada (5×5 teselas, unión de extremos y A*) frente al límite de 8 s; si no cabe, límite propio para la local o límite al tamaño del grafo.
  evidence: Sin verificar (maybe-false, medium si es cierto). Solo se ha medido en PC (563 m y 371 m); pasado el límite saldría el toast «no hay calles guardadas».
