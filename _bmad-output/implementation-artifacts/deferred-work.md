- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-abrir-la-app-y-ver-el-mapa-sobre-la-nueva-estructura.md`
  summary: Las teselas del mapa se cargan alrededor de un origen fijo (MapaVectorial.latitud/longitud); centrar el mapa lejos de ese origen muestra el suelo vacío, y la vista inicial está repetida en PantallaMapa.VistaInicial.
  evidence: MapaVectorial.Start carga 3×3 teselas alrededor del origen y Centrar solo mueve la cámara. Encaja en la historia 1.2 (gestos y caché) o en la 1.7 (centrar en mi posición), que necesitan cargar teselas según la vista.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-2-mapa-con-la-paleta-el-tema-claro-u-oscuro-y-los-gestos.md`
  summary: Segunda parte de la historia 1.2: inclinar el mapa con dos dedos (arrastre vertical), cargar y liberar teselas alrededor de la vista con la caché existente, y comprobar los 30 fps y el uso sin conexión en el móvil.
  evidence: El usuario pidió dividir la historia 1.2 el 17/11/2026 por tamaño (unos 2.100 tokens). Esta parte va justo después de la base de interfaz y el tema, y antes de cerrar la historia 1.2. Absorbe el pendiente de teselas de la historia 1.1.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-2b-gestos-y-teselas-alrededor-de-la-vista.md`
  summary: Extraer de CamaraMapa.Tactil el paso que aplica el gesto (inclinación, zoom y giro) a un método probable y probarlo (signo de la inclinación, sin zoom ni giro al inclinar).
  evidence: Tactil lee Input.GetTouch directamente. Hoy solo se prueba el clasificador puro y el resto se comprueba en el móvil.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Segunda parte de la historia 1.4. Los marcadores de los lugares sobre el mapa. La UI pinta cada lugar en una capa de UI Toolkit y lo coloca con `IMapa.Proyectar`, en `LateUpdate` después de la cámara (desvío < 1 px). Cada marcador lleva el icono vectorial de su tipo (glifos de `prototipo/lugares.js` dibujados con `Painter2D`), el color de su plantilla y la insignia € (sin precio, atenuado a 32 dp y opacidad 0,7). TalkBack lo anuncia: "Clínica Areas, clínica privada, desde 20 €". También se retiran los 4 marcadores fijos de `Principal.unity`.
  evidence: El usuario pidió dividir la historia 1.4 el 17/11/2026 por tamaño (unos 2.800 tokens). Esta parte va justo después de los datos base (parte 1) y usa su `ServicioLugares` y `ResumenLugar`.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Probar que la raíz de composición carga los lugares (hoy "PriceMap: 124 lugares cargados" solo se comprueba a mano en la build de PC).
  evidence: Revisión de la 1.4 (parte 1), Verification Gap. `PruebasArranque` solo prueba `Componer`. En la parte 2, `ServicioLugares` llegará a `Componer` y se podrá comprobar ahí.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Pruebas automáticas del script `exportar-datos-unity.js` (tarifas de un lugar que no existe, ids repetidos, plantillas desconocidas).
  evidence: Revisión de la 1.4 (parte 1). No hay arnés de pruebas de Node y los casos de error solo se comprobaron a mano con una copia aislada. Conviene hacerlo cuando `WEB_PRICES` tenga más negocios.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4b-marcadores-de-los-lugares-en-el-mapa.md`
  summary: Probar con TalkBack en un Android real que los marcadores se anuncian y que sus nodos aparecen y desaparecen al mover el mapa. También comprobar a ojo, al arrastrar, que los marcadores no van un fotograma por detrás.
  evidence: Revisión de la 1.4 (parte 2), Verification Gap. Con el lector apagado, `AnadirMarcas` y `ActualizarMarcadores` no se ejecutan. No hay pruebas PlayMode del orden Update/LateUpdate. La captura de PC solo mide con la cámara quieta (0,5 px).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-filtro-precios-en-el-mapa.md`
  summary: Segunda parte de la historia 1.5: el menú "Precios" con TalkBack y la captura de comprobación.
    - Con el menú abierto, las 5 opciones son botones de TalkBack y la elegida lleva el estado "seleccionado" (`AccessibilityState.Selected` o valor "seleccionado").
    - Al elegir se anuncia "Precios en el mapa: Hostelería".
    - Al cerrarse el menú, el foco vuelve a la píldora.
    - El botón atrás de Android (`KeyCode.Escape`) cierra el menú sin cambiar el filtro.
    - Captura "filtro" de PC con el menú abierto y Hostelería elegida.
  evidence: El usuario pidió dividir la historia 1.5 el 17/11/2026 por tamaño (unos 2.700 tokens). La parte 1 deja la píldora, el menú básico, el filtro y la preferencia.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-filtro-precios-en-el-mapa.md`
  summary: Probar con toques reales (en un panel o en el móvil) que la píldora abre el menú, que una opción filtra y que tocar fuera lo cierra (los `Clickable` del velo, la píldora y las opciones).
  evidence: Revisión de la 1.5 (parte 1), Verification Gap. Las pruebas EditMode llaman a `AlternarMenu`, `CerrarMenu` y `Pulsar` directamente, porque no hay panel para enviar eventos de puntero. Hasta ahora solo lo comprobó un clic accidental en la captura de PC.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5b-menu-precios-con-talkback-y-boton-atras.md`
  summary: Comprobar en un Android real que el botón atrás cierra el menú "Precios" y que, con el menú cerrado, no saca de la app. También comprobar que TalkBack lee el anuncio y lleva el foco a la píldora al elegir.
  evidence: Revisión de la 1.5 (parte 2), Verification Gap. Las pruebas llaman a `Atras()` directamente, y `Update` con `KeyCode.Escape` no se prueba porque en EditMode no hay forma de simular el `Input` antiguo. `Input.backButtonLeavesApp` se queda en su valor por defecto (false).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-puntos-de-interes-de-referencia-en-el-mapa.md`
  summary: |
    Segunda parte de la historia 1.6:
    - **Repetidos:** ocultar el punto de interés que repite un lugar de la app con marcador visible (mismo nombre normalizado a menos de 80 m, id del elemento MVT, como `applyPoiDedup` en `prototipo/app.js:639-660`). Si el filtro oculta el lugar, el punto vuelve a verse.
    - **Solapes:** ocultar los puntos que se pisan entre sí. Gana el de menor `rank`.
    - **TalkBack:** el usuario lo decidió el 17/11/2026. Lee los puntos visibles después de los lugares de la app, con el formato "Mercadona, supermercado"; los sanitarios públicos, como "centro sanitario público".
    - **Captura:** captura de PC con un repetido.
  evidence: El usuario pidió dividir la historia 1.6 el 17/11/2026 por tamaño (unos 2.300 tokens). La parte 1 deja la lectura de la capa `poi`, las categorías y el dibujo.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-6b-repetidos-solapes-y-talkback-de-los-puntos.md`
  summary: |
    Tercera parte de la historia 1.6: TalkBack lee los puntos de interés visibles (el usuario lo decidió el 17/11/2026).
    - **Qué dice:** "Mercadona, supermercado". Sin nombre, solo la categoría; si es sanitario público, "Centro de Salud X, centro sanitario público".
    - **Categorías:** los nombres salen de `POI_CATS.one` en minúsculas, por Localization, salvo super, que es "supermercado".
    - **Orden:** primero los botones, luego los lugares de la app y después los puntos, cada grupo de arriba abajo.
    - **Cuándo no se leen:** los puntos ocultos (lejos, fuera de pantalla, repetidos o pisados) no tienen nodo, y con el menú "Precios" abierto no se leen.
    - **Cambios necesarios:** `AccesibilidadPantalla` necesita grupos de marcas (lugares antes que puntos) y `QuitarMarcador`, porque las vistas de puntos se crean y destruyen con las teselas.
  evidence: El usuario pidió el 17/11/2026 separar la parte 2 de la historia 1.6 (unos 2.100 tokens). La parte 2 deja los repetidos y los solapes.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-6b-repetidos-solapes-y-talkback-de-los-puntos.md`
  summary: Probar con un panel de UI Toolkit dispuesto, o en el móvil, que la caja de solape de un punto usa la medida real de su nombre (y no la estimada), y que los puntos no parpadean al moverse el mapa.
  evidence: Revisión de la 1.6 (parte 2), Verification Gap. Las pruebas EditMode no tienen disposición y siempre usan la estimación. La medida real solo se comprueba en la captura de PC (0 pares solapados).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-6c-talkback-de-los-puntos-de-interes.md`
  summary: Prueba EditMode que compare cada valor de la tabla es-ES de Localization con `PrepararInterfaz.TextosEs[clave]`, para todas las claves de `ClavesTexto.Todas` (por ejemplo, `LaTablaTieneLosTextosDelGenerador` en `PruebasTextos`).
  evidence: Revisión de la 1.6 (parte 3), Verification Gap. Las pruebas usan `TextosEs` (`Textos.Forzados`) y `PruebasTextos` solo comprueba que el valor existe y no está vacío. Si la tabla se desvía del generador, ninguna prueba falla. Afecta a los textos de las historias 1.4 a 1.6.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-lugares-reales-de-sanlucar-con-su-logo-en-el-mapa.md`
  summary: Ignorar en git `unity/PriceMap/Assets/AddressableAssetsData/link.xml`, que cada build regenera.
  evidence: Se borró en `58e4bbb` y volvió a aparecer tras la build de PC de la verificación de la 1.4 (parte 1).
- source_spec: `_bmad-output/implementation-artifacts/spec-1-7-ver-mi-posicion-en-el-mapa.md`
  summary: Historia 1.7 (parte 2). El segundo toque del botón de ubicación, ya centrado, orienta el mapa al norte (`IMapa.OrientarNorte()`, giro 0); si el usuario mueve el mapa, el siguiente toque vuelve a centrar. TalkBack lee el estado del botón ("Centrar en mi posición", "Orientar al norte", "Buscando tu posición", "Ubicación desactivada"). Con el permiso denegado, al tocar el botón sale el aviso "La ubicación está desactivada. Puedes activarla en Ajustes > Aplicaciones > PriceMap > Permisos." con un botón "Abrir Ajustes" que lleva a los ajustes de la app. Captura de PC "posicion" con una ubicación simulada (círculo y cono).
  evidence: El usuario pidió el 18/11/2026 partir la 1.7 (unos 2.600 tokens). La parte 1 deja la ubicación, el permiso con su explicación, el marcador y el centrado.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-7-ver-mi-posicion-en-el-mapa.md`
  summary: En la parte 2 de la 1.7, distinguir "ubicación del móvil apagada" (permiso concedido, pero `Input.location.isEnabledByUser` es false o el estado es `Failed`) de "buscando", para que el botón y TalkBack no digan "Buscando tu posición" sin fin y se indique que hay que activar la ubicación.
  evidence: Revisión de la 1.7 (parte 1), hallazgo 3 (medium). `UbicacionUnity.Update` solo actúa con `LocationServiceStatus.Running`, así que el estado se queda en `Buscando`. En la parte 1 se ve igual que buscando (sin círculo y el botón no hace nada), pero el aviso de la parte 2 necesita saberlo.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-7-ver-mi-posicion-en-el-mapa.md`
  summary: Separar `UbicacionUnity` de `Input.location`, `Input.compass` y `Permission` detrás de una interfaz pequeña, para probar en EditMode sus transiciones (denegado, permiso retirado, orden de las respuestas, umbral de 1° del rumbo).
  evidence: Revisión de la 1.7 (parte 1), hallazgos 10 (Blind Hunter y Verification Gap). El adaptador real no se ejecuta en ninguna prueba ni en la captura, que usa una ubicación falsa. Hoy sus errores solo aparecerían probando en un móvil.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-ficha-del-lugar-con-sus-precios.md`
  summary: |
    Historia 1.8 (parte 2):
    - **Variantes (FR-60):** debajo de su concepto, con su condición y su precio; la vigente (`ReglaVariantes.Vigente`, hora local) se destaca. Se prueban con datos sintéticos (no hay precios con bono en los datos: decisión del 17/11, mantenida el 18/11).
    - **Centrado:** al abrir la ficha, el lugar queda centrado en la zona del mapa que no tapa la ficha.
    - **TalkBack:** al abrir, el foco va al título; la ficha es modal (el recorrido solo incluye la ficha, como el menú "Precios"); al cerrar (atrás, X o tocar el mapa), el foco vuelve al nodo del marcador.
    - Prueba con TalkBack forzado: el foco va al título y vuelve al marcador.
  evidence: El usuario pidió el 18/11/2026 partir la 1.8 (unos 2.600 tokens). La parte 1 deja el toque en los marcadores, la ficha con cabecera, conceptos, precios, origen, frescura, "sin precio", fila de acciones y cerrar.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-ficha-del-lugar-con-sus-precios.md`
  summary: Probar las ramas de `Input` de `CamaraMapa` (`ToqueTactil`/`ToqueRaton`: empezar sobre la interfaz cancela, segundo dedo anula, solo dedos en el móvil) sacando su lógica de `Input` detrás de algo que se pueda simular en EditMode.
  evidence: Revisión de la 1.8 (parte 1), hallazgo 4 (Verification Gap). `DetectorToque` está probado, pero cómo lo alimenta `CamaraMapa` no; invertir el `if (gestoSobreInterfaz)` no rompería ninguna prueba.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-ficha-del-lugar-con-sus-precios.md`
  summary: Comparar en `PruebasTextos` el valor de cada clave de `Textos_es-ES.asset` con `PrepararInterfaz.TextosEs`, no solo que exista.
  evidence: Revisión de la 1.8 (parte 1), hallazgo 10 (Verification Gap). Las pruebas de la ficha usan `TextosEs`; un `{1}` perdido en `ficha.precio.rango` del asset pasaría todas las pruebas.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-ficha-del-lugar-con-sus-precios.md`
  summary: Ver funcionando el desplazamiento interno de la ficha cuando no cabe (sin barras, ScrollView sin tema por defecto). Sin verificar; si falla, sería medium.
  evidence: Revisión de la 1.8 (parte 1), hallazgo 11 (Blind Hunter). En la captura Clínica Areas cabe en el 68 % de la pantalla; lo resolvería un lugar sintético con muchos conceptos en la captura o probarlo en un móvil.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8b-variantes-y-centrado-de-la-ficha.md`
  summary: |
    Historia 1.8 (parte 3), TalkBack de la ficha:
    - Tocar dos veces el nodo de un marcador abre su ficha. Hoy `AccesibilidadPantalla.Marcador` no tiene acción.
    - Con la ficha abierta, `Modal`: el recorrido solo tiene título, tipo, dirección, un nodo por concepto (con sus variantes) y la X ("Cerrar ficha", clave `FichaCerrar` ya creada).
    - Al abrir, el foco va al título. Al cerrar (atrás, X o tocar fuera), vuelve al nodo del marcador. `Enfocar` hoy solo busca en `nodosControles`, no en `nodosMarcas`.
    - La ficha se desplaza hasta el nodo con el foco.
    - Pruebas con TalkBack forzado (patrón de `PruebasFiltro.cs:68-75, 519`).
  evidence: El usuario pidió el 18/11/2026 partir la parte 2 de la 1.8, que medía unos 2.600 tokens. La parte 2 se queda con las variantes y el centrado.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8b-variantes-y-centrado-de-la-ficha.md`
  summary: Probar el recentrado de la ficha cuando la hoja recibe su medida real (`RecentrarFichaPendiente`) y al pasar de una ficha a otra. Hoy solo lo comprueba la captura de PC; se podría añadir un caso A→B a la captura o montar un arnés de PlayMode con panel.
  evidence: Revisión de la 1.8 (parte 2), hallazgo 5 (Verification Gap). En EditMode no hay layout de UI Toolkit; quitar el `RegisterCallback` o la línea `if (yaAbierta)` no rompe ninguna prueba EditMode.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8c-talkback-de-la-ficha.md`
  summary: Probar en un móvil con TalkBack la ficha. Tocar dos veces un marcador tiene que abrirla y deslizar tiene que recorrer solo la ficha y desplazarla hasta el nodo con el foco. Hay que ver si TalkBack llega a los nodos que quedan fuera de la parte visible o dibuja su marco sobre el mapa, y comprobar que atrás devuelve el foco al marcador. Sin verificar; si falla, sería medium.
  evidence: Revisión de la 1.8 (parte 3), hallazgo 6 (Blind Hunter). `frameGetter` da el marco sin recortar al `ScrollView`; en EditMode no hay disposición ni TalkBack real.
- source_spec: `_bmad-output/implementation-artifacts/spec-mapa-sin-solapes-con-precios.md`
  summary: Dos lugares reales comparten coordenada. Con la regla de solapes, uno queda oculto a cualquier zoom y no se puede tocar ni lo anuncia TalkBack. Hay que corregir la coordenada en los datos o decidir cómo mostrar dos lugares en el mismo punto (agrupar o desplazar).
  evidence: Revisión del arreglo de solapes, hallazgo 8. `PruebasFiltro` espera ver 123 de 124 lugares por esa pareja.
- source_spec: `_bmad-output/implementation-artifacts/spec-mapa-sin-solapes-con-precios.md`
  summary: Probar en EditMode, con un panel dispuesto, las zonas reales de los controles (`ZonasControles`) y que las filas de la ficha no se encogen. Hoy solo lo comprueba la captura de PC.
  evidence: Revisión del arreglo de solapes, hallazgo 9 (Verification Gap). Quitar `Ocupar(pildora)` o los `flex-shrink: 0` no rompe ninguna prueba EditMode.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-gps-activo-con-la-pantalla-apagada-durante-una-ruta.md`
  summary: Probar en un Android real el servicio de ruta. Al iniciar la ruta tiene que salir la notificación fija "Navegando a …" y, con la pantalla apagada 2 min, al volver tienen que llegar las posiciones en orden; al terminar, la notificación desaparece. En Android 13 o superior, comprobar la petición de `POST_NOTIFICATIONS` y el aviso si se deniega. Hasta la 2.7 no hay botón que llame a `IniciarRuta`: hará falta esa historia o un disparador temporal.
  evidence: Decisión del autor del 19/11/2026 (spec 2.1). En EditMode solo se prueba la lógica con un servicio falso; el plugin Java solo se ha compilado en la APK.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-gps-activo-con-la-pantalla-apagada-durante-una-ruta.md`
  summary: El puerto de ubicación solo entrega la coordenada de las posiciones de la ruta; se pierden el rumbo de la marcha, la velocidad, la precisión y la hora que ya lee LecturaRuta.
  evidence: UbicacionGps.RecogerRuta usa solo lectura.Posicion e IUbicacion no tiene hora; la navegación (2.7) y la visita verificada (4.2) necesitan distinguir una posición antigua de la cola de una actual.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-gps-activo-con-la-pantalla-apagada-durante-una-ruta.md`
  summary: La notificación de navegación usa el icono de la app a todo color como icono pequeño; en la barra de estado se vería como una mancha blanca. Hace falta un icono monocromo.
  evidence: ServicioUbicacionRuta.notificacion usa setSmallIcon(getApplicationInfo().icon). Comprobarlo junto con la prueba en un Android real.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-hoja-del-buscador-desplegable.md`
  summary: Historia 2.2 (parte 2): TalkBack de la hoja del buscador y ajustes del sistema. Con TalkBack, tocar la cápsula abre la hoja del todo y pone el foco en el campo; la hoja ofrece las acciones "Expandir" y "Contraer" (dos botones visibles solo para el lector, porque AccesibilidadPantalla no tiene acciones personalizadas); y la hoja es modal mientras está abierta. Un puerto `IAccesibilidadSistema` con adaptador para Android: con "reducir animaciones" (ANIMATOR_DURATION_SCALE == 0) la altura cambia sin transición, y con "reducir transparencias" el fondo de la cápsula es opaco.
  evidence: El usuario partió la 2.2 el 19/11/2026 porque su spec pasaba de 1.600 tokens.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-hoja-del-buscador-desplegable.md`
  summary: Probar en el móvil los gestos reales de la hoja del buscador. Arrastrar la lista frente a arrastrar la hoja, tocar el avatar, soltar fuera de la hoja y comprobar que el teclado sale al tocar.
  evidence: En EditMode no hay panel que reparta eventos de puntero, así que las pruebas llaman a Bajar/Mover/Soltar directamente y no pasan por los manejadores de HojaBuscador.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-hoja-del-buscador-desplegable.md`
  summary: Decidir en la 2.3 si plegar la hoja del buscador (con atrás, al abrir una ficha o al arrastrarla abajo) borra lo escrito en el campo, como hace ahora.
  evidence: HojaBuscador.Plegar y la suelta plegada vacían el campo; el spec de la 2.2 no lo dice y en la parte 1 todavía no hay búsqueda.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-hoja-del-buscador-desplegable.md`
  summary: Añadir a PruebasContraste los pares nuevos del buscador: el texto de ayuda del campo sobre la cápsula y sobre la hoja (claro y oscuro), el texto de los chips y de "Encuentra cerca", y los iconos blancos sobre los colores de categoría.
  evidence: El diff de la 2.2 añade --color-capsula y --color-buscador-campo y ninguna prueba mide su contraste.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2b-talkback-y-reducir-animaciones-del-buscador.md`
  summary: Hacer que TalkBack anuncie el campo del buscador como campo de búsqueda (AccessibilityRole.SearchField) y lea lo que se ha escrito. Hoy se declara como botón porque AccesibilidadPantalla solo sabe crear botones y textos.
  evidence: El nodo del campo es un Button con la etiqueta fija «Buscar en Sanlúcar». Tiene más sentido hacerlo en la 2.3, cuando haya búsqueda real.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2b-talkback-y-reducir-animaciones-del-buscador.md`
  summary: Aplicar «quitar animaciones» del sistema también a los movimientos de cámara del mapa (centrar en la ficha o en mi posición), no solo a la hoja del buscador (UX-DR19).
  evidence: IAccesibilidadSistema solo lo consulta la hoja; las animaciones de CamaraMapa son anteriores a esta historia y no miran el ajuste.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2b-talkback-y-reducir-animaciones-del-buscador.md`
  summary: Probar en un móvil con TalkBack la hoja del buscador (abrir desde la cápsula, recorrido, Expandir/Contraer y botón atrás) y el ajuste de Android «quitar animaciones».
  evidence: En EditMode, TalkBack se simula con LectorForzado y la escala de animaciones con un puerto falso.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-buscar-lugares-puntos-de-interes-y-direcciones.md`
  summary: Historia 2.3 (parte 2), direcciones con Photon. Un puerto IGeocodificador con su adaptador PriceMap.Adaptadores.Busqueda (GeocodificadorPhoton con UnityWebRequest y LecturaPhoton para leer el GeoJSON). La URL es https://photon.komoot.io/api/?q=&lat=&lon=&limit=8&lang=default, sin bbox (decisión del 19/11: todo el mundo, con prioridad por cercanía) y con User-Agent "PriceMap/1.0". Espera 350 ms tras la última tecla, con un mínimo de 3 caracteres, y cancela la petición anterior. Las direcciones salen después de los lugares y de los puntos; el botón «Buscar como dirección» va junto a «No hay resultados»; sin conexión se avisa «Las direcciones necesitan conexión». Los errores van como Resultado<T>, y las direcciones no se guardan (FR17). Matriz: dirección "calle ancha 10", sin conexión y tecleo rápido (una sola petición).
  evidence: El usuario partió la 2.3 el 19/11/2026 porque su spec pasaba de 1.600 tokens.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-buscar-lugares-puntos-de-interes-y-direcciones.md`
  summary: Comprobar en el móvil que arrastrar la lista de resultados no elige un resultado sin querer (el Clickable de FilaResultado dentro del ScrollView).
  evidence: Sin verificar (maybe-false, medium si es cierto). Se resuelve probando en un móvil real a desplazar la lista y soltar sobre una fila.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-buscar-lugares-puntos-de-interes-y-direcciones.md`
  summary: Comprobar con TalkBack real que escribir deprisa no encadena anuncios "N resultados" ni corta el eco del teclado.
  evidence: Sin verificar (maybe-false, medium si es cierto). Solo se anuncia si cambian los resultados, pero el foco se pide a cada tecla con el lector activo; se resuelve en el móvil.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-buscar-lugares-puntos-de-interes-y-direcciones.md`
  summary: Añadir a PruebasContraste los iconos de los resultados (glifo sobre los fondos --color-poi-* y accent sobre surface-2) y el texto muted en oscuro.
  evidence: Sin verificar (maybe-false, medium si es cierto). Se resuelve midiendo el contraste de esos pares de tokens en claro y oscuro.
