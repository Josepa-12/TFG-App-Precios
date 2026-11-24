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
- source_spec: `_bmad-output/implementation-artifacts/spec-2-7-navegar-giro-a-giro.md`
  summary: Historia 2.7 (parte 2): la cámara sigue al usuario (inclinación 45°, arriba el rumbo de la marcha en coche y el de la brújula a pie; a los 2 s de pulsar Ir, al instante con «reducir animaciones»), deja de seguir si se mueve el mapa y sale «Volver a centrar»; flecha `#2F5BEA` con borde blanco orientada al rumbo GPS (velocidad > 1 m/s; si no, el último) en coche y, por pedido del usuario del 21/11, también a pie (concretar: solo navegando, o también fuera de ruta); sin señal 10 s, posición atenuada y «Buscando señal GPS» sin recálculo (UX-DR16); `Screen.sleepTimeout = NeverSleep` durante la navegación. Matriz: coche rumbo 90° a 5 m/s → flecha al este; 12 s sin posición → atenuada; arrastre → «Volver a centrar». Necesita que `IUbicacion` entregue el rumbo de la marcha y la velocidad (pendiente de la 2.1) o calcularlos de dos posiciones.
  evidence: El usuario partió la 2.7 el 21/11/2026 (total unos 2.800 tokens): primero el seguimiento y después la cámara y la posición.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-7-navegar-giro-a-giro.md`
  summary: En la llegada (2.9), la tarjeta de arriba y TalkBack dicen «En 130 m, ha llegado a su destino» (texto de Valhalla en pasado); usar una forma futura («Su destino está a 130 m») o el texto `verbal_pre_transition_instruction` de Valhalla.
  evidence: Revisión de la 2.7 parte 1 (blind, low). La prueba `Pantalla_PasarManiobra_SaleLaSiguienteYTalkBackLaAnunciaUnaVez` fija ese texto; la llegada queda fuera de la 2.7 por el intent.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-7-navegar-giro-a-giro.md`
  summary: `PantallaMapa.OnDestroy` termina la ruta con `MotivoFinRuta.Manual`; cuando `ServicioVisitas` (épica 4) escuche `RutaTerminada`, cerrar la app no debe contar como que el usuario terminó la ruta (otro motivo o sin evento).
  evidence: Revisión de la 2.7 parte 1 (blind, low). Hoy nadie escucha `RutaTerminada`; `OnDestroy` llama a `navegacion.Terminar()` con el motivo por defecto.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-7b-camara-que-sigue.md`
  summary: Historia 2.7 (parte 2c): flecha de posición (`#2F5BEA`/`#4C7DFF`, borde blanco, gira respecto al norte; rumbo de la marcha de `ReglaRumbo` en coche y brújula a pie) y 10 s sin posición → flecha atenuada (0,4), aviso «Buscando señal GPS» (anuncio al aparecer y «Señal GPS recuperada»), sin recálculo y con el contador de desvío a cero. Preguntas abiertas: flecha a pie solo navegando (recomendado) o siempre; aviso en pastilla fija bajo la tarjeta de arriba (recomendado) o en lugar de la distancia. Capturas `navegacion-flecha` y `sin-senal`.
  evidence: El usuario partió la 2.7 parte 2 el 22/11/2026 (unos 2.600 tokens): primero la cámara que sigue, «Volver a centrar» y la pantalla encendida (spec 2.7b); después la flecha y la falta de señal.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-7b-camara-que-sigue.md`
  summary: Siguiendo en coche, la cámara llega al punto en ≈ 0,6 s y espera la posición siguiente (≈ 1 s): comprobar en el móvil si se nota el «avanza y para»; si se nota, extrapolar la posición con la velocidad o ligar la transición al intervalo del GPS.
  evidence: Revisión de la 2.7b (blind, maybe-false; medium si es cierto). τ = 0,2 s en `CamaraMapa.AvanzarSeguimiento`; en PC no se puede juzgar.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-7b-camara-que-sigue.md`
  summary: Comprobar en el móvil que la pantalla no se apaga durante la ruta (`Arranque.Start` pasa `PantallaSistemaUnity` a `Componer`; sin prueba automática).
  evidence: Revisión de la 2.7b (verification gap, low). Quitar ese argumento no rompe ninguna prueba; necesita escena y Android.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-8-indicaciones-por-voz.md`
  summary: Historia 2.8 (parte 2): indicaciones por voz con la pantalla apagada (en curso: spec-2-8b, Unity sin pausar navegando).
  evidence: El usuario partió la 2.8 el 22/11/2026 (unos 2.300 tokens): primero la voz con la app delante; la pantalla apagada obliga a tocar el servicio de Android.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-8-indicaciones-por-voz.md`
  summary: Prueba propia de que `AccesibilidadPantalla.RefrescarEtiqueta` relee el valor del nodo (cambiar el modo de voz con la fila cerrada y mirar el valor de TalkBack).
  evidence: Revisión de la 2.8 (verification-gap). Hoy todo cambio de modo desde la UI reconstruye el recorrido y la prueba pasa aunque se quite esa línea.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-8-indicaciones-por-voz.md`
  summary: Probar `PuenteVoz` en un móvil real: castellano, foco de audio con atenuación, calla en silencio/vibración, frase pedida antes de que el motor esté listo.
  evidence: Revisión de la 2.8: en EditMode solo se lee el código Java; no hay pruebas de Android instrumentadas.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-8b-voz-con-la-pantalla-apagada.md`
  summary: Probar en el móvil `ActividadPrincipal` (Unity sin pausar navegando con el móvil bloqueado, vuelta, ruta terminada detrás) y el `WAKE_LOCK` del servicio; en EditMode solo se lee el código Java.
  evidence: Revisión de la 2.8b (verification-gap). Si Unity se pausa igualmente, pasar al respaldo decidido: la línea de la ruta al servicio de Android.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-8b-voz-con-la-pantalla-apagada.md`
  summary: Prueba que fije que `UbicacionUnity` y `Arranque` van en el mismo GameObject (destino de `AlPosicionRuta` y `AlSegundoPlano`).
  evidence: Revisión de la 2.8b (verification-gap): si se separan, `AlSegundoPlano` no llega y en segundo plano se dibuja a 60 fps sin que falle ninguna prueba.

- source_spec: `_bmad-output/implementation-artifacts/spec-red-android-tls13.md`
  summary: En Android 8 y 9 (minSdk 26) `HttpURLConnection` no tiene TLS 1.3, así que Valhalla y OSRM seguirán fallando por SSL en esos móviles (la ruta sale aproximada).
  evidence: TLS 1.3 viene activado desde Android 10; arreglarlo pediría Conscrypt en la APK o subir el minSdk a 29 (decisión del usuario).
- source_spec: `_bmad-output/implementation-artifacts/spec-red-android-tls13.md`
  summary: La ruta de Android de `ClienteHttp`/`PuenteRed` solo tiene pruebas de lectura del código; falta probarla en el móvil (ruta, búsqueda, enlace corto, modo avión, cancelar).
  evidence: EditMode no ejecuta JNI ni Java; queda para la prueba en el Redmi con `adb logcat`.
- source_spec: `_bmad-output/implementation-artifacts/spec-hoja-navegacion-finalizar-ruta.md`
  summary: Probar en el móvil el gesto de la hoja de la ruta (tocar, arrastrar arriba/abajo, pulsar «Finalizar ruta», arrastrar desde el botón) y TalkBack «Desplegar»/«Plegar».
  evidence: En EditMode los eventos de puntero se envían con el destino puesto a mano y la disposición forzada por reflexión.
- source_spec: `_bmad-output/implementation-artifacts/spec-ir-a-una-direccion-buscada.md`
  summary: Un resultado de dirección de Photon que es un lugar de la app (mismo nombre y sitio) abre una ficha de punto libre sin precios.
  evidence: Revisión (edge case). `ServicioBusqueda` quita los puntos repetidos con `ReglaPuntosInteres.EsRepetido`, pero las direcciones de Photon no se comparan; el lugar sale encima en la lista.
- source_spec: `_bmad-output/implementation-artifacts/spec-abrir-en-la-ultima-ubicacion-conocida.md`
  summary: Probar en el móvil que la app se abre ya en la última ubicación del sistema (precisa y solo aproximada).
  evidence: En EditMode solo se lee el código Java; nada ejecuta `getLastKnownLocation`.
- source_spec: `_bmad-output/implementation-artifacts/spec-varias-rutas-la-mas-rapida.md`
  summary: Probar en el móvil las alternativas reales (Valhalla y OSRM): ninguna prueba ejecuta los constructores sin parámetros de los dos adaptadores.
  evidence: Revisión de la 5a (verification-gap): cambiar de sitio la URL y el cuerpo no rompería ninguna prueba; ya pasaba antes.
- source_spec: `_bmad-output/implementation-artifacts/spec-como-llegar-elegir-ruta.md`
  summary: Al cerrar «Cómo llegar» con la X, recuperar también el zoom que había antes de encuadrar las rutas.
  evidence: Revisión de la 5b (ciega). `IMapa.Encuadrar` cambia la distancia desde la 2.6b y no hay cómo reponerla; la ficha vuelve centrada, pero con el mapa alejado.
- source_spec: `_bmad-output/implementation-artifacts/spec-como-llegar-elegir-ruta.md`
  summary: Probar «Cómo llegar» en un móvil (alternativas reales, desplazar las tarjetas, TalkBack, paso de Iniciar a navegar) y ver si las etiquetas de minutos se pisan o si bloquear el móvil justo tras «Iniciar» molesta.
  evidence: En el PC solo hay rutas falsas y sin TalkBack; si se bloquea en los 0,9 s de acercamiento, la navegación empieza al volver.
- source_spec: `_bmad-output/implementation-artifacts/spec-buscar-calles-guardadas.md`
  summary: Probar con un panel que las calles y direcciones que llegan tarde no mueven el desplazamiento de la lista del buscador, y probar la búsqueda de calles en el móvil (tiempo de la primera).
  evidence: Revisión (verification gap). Ninguna prueba mira `Panel.scrollOffset`; el tiempo solo se ha medido en el PC (20 ms la primera búsqueda).
- source_spec: `_bmad-output/implementation-artifacts/spec-2-9-llegar-y-terminar-la-ruta.md`
  summary: Probar con la pantalla en un panel que los marcadores y puntos no se dibujan debajo de las tarjetas de la ruta ni de «Has llegado» (`ZonasOcupadas`).
  evidence: Revisión de la 2.9 (verification gap). Ninguna prueba mira `ZonasOcupadas`; quitar `Ocupar` de esas tarjetas no rompe nada.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-9-llegar-y-terminar-la-ruta.md`
  summary: En el móvil: llegada real, cierre al minuto (también bloqueado) y que la notificación desaparezca.
  evidence: En el PC la posición es falsa y no hay notificación; si Android pausara Unity, el minuto se cerraría al volver.

- source_spec: `spec-pantalla-de-inicio.md`
  summary: Probar en el móvil la parte nativa de la pantalla de inicio (en el repositorio no hay pruebas instrumentadas de Android).
  evidence: Las pruebas solo leen el Java. Comprobar que sale, `irse` la quita y después se puede tocar el mapa; que se va sola a los 15 s; tema guardado oscuro con el sistema claro (y en Android 8–11 el arranque con el del sistema); sin fotograma negro; pin hasta el punto azul; sin animaciones; TalkBack; y que `hardwareAccelerated` no afecta al mapa ni al teclado.

- source_spec: `spec-3-1-bienvenida-privacidad-y-consentimientos.md`
  summary: Poder volver a leer el aviso de privacidad desde Ajustes · Privacidad (3.5) y completarlo con lo que pide el RGPD.
  evidence: Tras «Empezar» la bienvenida no vuelve a salir y nada más abre el aviso. Le faltan derechos (acceso, borrado, reclamar a la AEPD), base legal, plazos y fecha; el contenido lo decide el usuario.

- source_spec: `spec-3-1-bienvenida-privacidad-y-consentimientos.md`
  summary: Probar la bienvenida en un móvil (instalación limpia en Android 13+ y anterior, y actualización).
  evidence: Diálogos reales de permisos, denegar dos veces, cerrar el diálogo sin responder, TalkBack, fundido de la pantalla de inicio y copiar el correo (Unity no tiene menú de copiar en Android).

- source_spec: `spec-3-2-mis-datos-en-firebase-protegidos-y-disponibles-sin-conexion.md`
  summary: Meter el SDK de Firebase en la app (parte 2 de la 3.2; `spec-3-2b-firebase-dentro-de-la-app.md`).
  evidence: El spec entero medía 1.946 tokens y el usuario eligió partirlo.

- source_spec: `spec-3-2-mis-datos-en-firebase-protegidos-y-disponibles-sin-conexion.md`
  summary: Al abrir visitas y aportes (épica 4), sustituir la regla recursiva de `usuarios/<uid>` por subcolecciones con nombre y validar campos y tamaños.
  evidence: Firestore suma los `allow`: con `{resto=**}` no se puede estrechar `visitas` (AD-6), y una cuenta puede escribir cualquier campo o gastar la cuota.

- source_spec: `spec-3-2-mis-datos-en-firebase-protegidos-y-disponibles-sin-conexion.md`
  summary: En la 3.2b, comprobar desde la app que las reglas publicadas en `pricemap-98f78` son las del repositorio.
  evidence: Las pruebas usan el emulador; el usuario las publica a mano y nada compara lo publicado.

- source_spec: `spec-3-2b-firebase-dentro-de-la-app.md`
  summary: Comprobar Firebase en un móvil y Firestore de verdad con el emulador (dos escrituras parciales, `DelMovil`, escuchar, IL2CPP).
  evidence: Las pruebas usan un Firestore falso y `AccesoFirestore` nunca se ha ejecutado en la APK (recorte de IL2CPP, medium sin verificar); en el móvil falta ver «PriceMap: Firebase listo» y que sin conexión abre. Encaja en la 3.3.

- source_spec: `spec-3-2b-firebase-dentro-de-la-app.md`
  summary: Vaciar la copia de Firestore al cerrar sesión o cambiar de cuenta, y poder borrar documentos y campos.
  evidence: `IAlmacen` solo lee, escribe y escucha; lo necesitan la 3.3 (sesión), favoritos y borrar la cuenta.

- source_spec: `spec-3-2b-firebase-dentro-de-la-app.md`
  summary: Nombrar Firebase en «Qué sale del móvil» cuando la app empiece a usarlo (3.3).
  evidence: Firebase se inicia antes de la bienvenida; Analytics está apagado y no consta que salga nada (por confirmar con una captura de red), pero el aviso no lo menciona (medium sin verificar).

- source_spec: `spec-3-3-entrar-con-google-o-con-correo-o-seguir-sin-cuenta.md`
  summary: Entrar con correo y contraseña (parte 2 de la 3.3; `spec-3-3b-entrar-con-correo.md`).
  evidence: La spec entera medía unos 1.950 tokens y el usuario eligió partirla; el enlace mágico se descartó por el límite de 5 al día.
