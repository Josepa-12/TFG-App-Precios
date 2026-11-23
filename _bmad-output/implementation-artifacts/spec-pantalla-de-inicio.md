---
title: 'Pantalla de inicio animada, sin el logo de Unity'
type: 'feature'
created: '2026-11-23'
status: 'done'
baseline_commit: '77fbe84fe2bcb2b077b2c415342b6236ea8d060d'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Al abrir la app sale el logo de Unity sobre fondo oscuro, después un momento en negro y el mapa aún sin centrar ni cargar.

**Approach:** Pantalla de inicio propia con el logo del usuario (maqueta, opción A: https://claude.ai/artifact/YGrDHZfz2mTUzEghV4TzPb), animada desde el primer instante. Mientras se ve, la app busca la ubicación y carga el mapa alrededor; al irse, el pin se encoge hasta el punto azul del usuario y el mapa se acerca por detrás.

## Boundaries & Constraints

**Always:**
- Decidido (usuario, 23/11/2026): fuera el logo de Unity (también en PC). Sale cada vez que se abre la app desde cero, no al volver de segundo plano.
- Decidido (usuario, 23/11/2026): animación A. El pin cae, rebota y flota con su sombra y ondas en el suelo mientras carga; sale «PriceMap» debajo; al acabar, el pin se encoge hasta la posición del usuario, el fondo se desvanece y el mapa se acerca (menos de 1 s). Proporciones de la imagen original (852 × 1846), centradas, en cualquier pantalla.
- Decidido (usuario, 23/11/2026): en oscuro, fondo azul noche y «Price» en blanco. Manda el tema de la app (el guardado o, sin él, el del sistema).
- Decidido (usuario, 23/11/2026): se queda hasta tener la posición, centrada, y las teselas de la vista; como mucho **4 s** desde que Unity está listo. Nunca antes de que el pin haya caído (1 s). Si no llega la posición, se entra en la última conocida y se centra sola después, como hoy.
- Sin permiso de ubicación, denegado o apagada, o con algo compartido que abre otro sitio: no se espera a la posición, solo al mapa. El pin se encoge entonces en el centro de la pantalla.
- Sin fotograma negro ni blanco entre medias: por eso la pantalla es **nativa de Android** (encima de Unity desde `onCreate`; el motor tarda 2–3 s y no puede animar). El arranque del sistema (Android 12+) y el fondo de la ventana van del mismo color, sin icono.
- «Quitar animaciones» del sistema: logo quieto y fundido corto, sin caída, flotar, ondas ni zoom del mapa.
- Mientras se ve no se puede tocar el mapa; TalkBack dice «PriceMap, cargando» y, al irse, sigue como hoy. La explicación del permiso (1.7) sale al irse la pantalla, no debajo.
- Seguro: si Unity no avisa, la pantalla se va sola a los 15 s. Se libera al irse (vistas e imágenes).

**Never:** cambiar `CentrarAlAbrir` para lo demás; pedir permisos desde la pantalla de inicio; la bienvenida (3.1).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Con GPS | Posición y teselas a los 2 s | Se va; el pin acaba en el punto azul |
| Rápido | Todo listo a los 0,2 s | Espera a que el pin caiga (1 s) |
| GPS lento | Sin posición a los 4 s | Se va; última conocida; se centra al llegar |
| Mapa lento | Posición sí, teselas no a los 4 s | Se va igual |
| Sin permiso o apagada | — | No espera posición; después, la explicación de hoy |
| Compartido | Abierta con un `geo:` | No espera posición; abre su ficha |
| Tema | Guardado oscuro, sistema claro | Pantalla oscura |
| Sin animaciones | Ajuste del sistema | Logo quieto, fundido corto |
| Vuelve de segundo plano | App viva | No sale |
| Bloqueo a mitad | Se apaga la pantalla con el inicio puesto | Al volver, sigue o se va; nunca se queda |
| Unity no avisa | 15 s | Se va sola |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap`.
- Capas ya recortadas en `docs/diseno/inicio` (`pin`, `anillo`, `sombra`, `nombre-claro`, `nombre-oscuro`, ×3 de la imagen; medidas en `separar_capas.py`): van a `res/drawable-nodpi` del proyecto Gradle (copiar a `Assets/Plugins/Android` no admite recursos sueltos: usar el post-proceso o un `.androidlib`). Anillo, sombra y ondas valen también dibujados.
- `Assets/Plugins/Android/ActividadPrincipal.java` (`onCreate`: `setContentView(mUnityPlayer.getFrameLayout())`; aquí se pone encima la vista nueva). Clase nueva `com.pricemap.inicio.*` (vista y `PuenteInicio` con `estado/irse(x, y)`), JNI como `PuenteVoz`/`VozAndroid`. Las animaciones, con `ViewPropertyAnimator`/`AnimatorSet`: el hilo principal se bloquea a ratos al cargar Unity.
- Tema guardado: PlayerPrefs de Unity, `SharedPreferences` `com.pricemap.app.v2.playerprefs`, clave `PriceMap.tema` (valor de `ReglaTema.AClave`; PlayerPrefs codifica los textos en URL).
- `Assets/Editor/ActividadAndroid.cs` (patrón de post-proceso Gradle): colores del arranque (`values`, `values-night`, `values-v31`: hoy `staticSplashScreenBackgroundColor` #1F1F20, icono transparente y `windowBackground` negro). `ConfigurarAndroid.Configurar`: `PlayerSettings.SplashScreen.show = false` y `showUnityLogo = false`.
- `Assets/Scripts/Aplicacion`: puerto `IPantallaInicio` y servicio puro con reloj inyectado que decide cuándo irse (posición, teselas, 4 s, 1 s). Adaptador en `Adaptadores/Sistema` (Android; nulo en PC, editor y captura).
- `Scripts/UI/PantallaMapa.cs` (`CentrarAlAbrir`, `Proyectar` la posición, explicación al abrir en la línea 308, `AbrirExplicacion`), `Scripts/Vectorial/MapaVectorial.cs` (`TeselasPendientes`, `ActualizarTeselas`; `IMapa` necesita saber si la vista está cargada y acercarse desde 1,22, con miembros por defecto), `CamaraMapa`, `Arranque.Start`.
- Pruebas: las de Java leen el fichero (`PruebasSegundoPlano`, `PruebasVoz`) y las de manifiesto, `PruebasPermisosUbicacionAndroid`.

## Tasks & Acceptance

**Execution:**
- [x] Vista nativa, recursos, colores y `ActividadPrincipal`.
- [x] Puerto, servicio y adaptador; `IMapa` (vista cargada, acercarse); `PantallaMapa`/`Arranque`.
- [x] Quitar el logo de Unity (proyecto y `ConfigurarAndroid`).
- [x] Una prueba por fila de la matriz que no sea solo del móvil; Java y recursos leídos.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas, y la APK se compila con los recursos dentro.
- Dada la captura de PC, cuando se ejecuta, entonces todo sale como antes (allí no hay pantalla de inicio).

## Implementation Notes

- Reanudada el 23/11/2026: ya hay en el árbol, sin commit, `IPantallaInicio.cs`, `ServicioInicio.cs` y `PantallaInicioAndroid.cs` de un intento cortado (usan `ParametrosNegocio.InicioPinSegundos/InicioMaximoSegundos`, que aún no existen). Revisarlos y continuar desde ahí. Los cambios de `spec-3-1` y `separar_capas.py` son ajenos: no tocarlos.

## Spec Change Log

## Review Triage Log

| Hallazgo | Veredicto | Motivo | Destino |
|---|---|---|---|
| BH1/EC1 `onDestroy` quita la pantalla de otra actividad | low | `quitar()` sin la guarda `instancia == this`; corrección directa | patch |
| BH2/EC9 «sin animaciones» distinto en Java y C# | low | Solo en ahorro de batería de Android 8 (el ajuste vale > 0): fundido corto con zoom; arreglarlo añade superficie al puerto | reject |
| BH3 sin animaciones, mínimo de 1 s | false | El spec dice «nunca antes de 1 s», sin excepción | reject |
| BH4 tesela fallida = 4 s | false | El spec: «como mucho 4 s» y «Mapa lento: se va igual»; es lo pedido | reject |
| BH5 muestreo 5×5 con el mapa inclinado | false | Solo se usa al abrir, con 50°, 45° de campo y 900 m: entre dos filas hay menos que una tesela (1,96 km) | reject |
| BH6 `MargenVista` copiado a mano | low | Se deriva de `InicioAcercarDesde` | patch |
| BH7/EC8/VG-otros contrato «cualquier otro movimiento» | low | `CentrarEn(lat, lon)` no lo acaba (y es mejor así con el GPS tardío): se corrige el comentario | patch |
| BH8 `Seguir` animado salta | false | En los 0,8 s de la salida la pantalla nativa tapa los toques: no puede empezar una ruta | reject |
| BH9/EC6 «centrada» no se comprueba | low | Sin toques solo pasaría tras un bloqueo de más de 10 s, y entonces ya manda el límite de 4 s; el pin va al punto azul proyectado | reject |
| BH10/EC7 destino proyectado ya a 1,22 | low | Fuera del centro el pin se queda corto; basta proyectar antes | patch |
| BH11 excepción en `Arranque.Start` | false | Para eso está el seguro de 15 s del spec | reject |
| BH12/EC5 teclado físico | low | Un teclado físico en los primeros segundos; arreglarlo añade guardas | reject |
| BH13/VG-otros nodo obligatorio que falta solo avisa | low | Saldría el gris de Unity sin enterarse; lanzar `BuildFailedException` | patch |
| BH14 fondo de la ventana viejo tras cambiar el tema | low | Tapado por Unity; no se ve en el uso normal | reject |
| BH15 cambio de tamaño durante el inicio | low | Vertical fija; solo plegables en los primeros segundos | reject |
| BH16 aceleración gráfica permanente | low | Necesaria para animar; queda en la lista de pruebas en el móvil | reject |
| BH17 texto de TalkBack fuera de Localization | low | Se necesita antes de que Unity exista; la app solo está en español | reject |
| BH18/VG10 Java comprobado leyendo el código | medium | No hay pruebas instrumentadas en el repositorio | defer |
| BH19 objetos de prueba sin borrar si falla | low | Corrección directa en TearDown | patch |
| BH20 documentación incompleta | low | Se completa al cerrar; la línea larga del Javadoc se parte | patch |
| EC2 el listener de pre-dibujo no se quita en `liberar()` | low | Fuga si la pantalla se va antes de dibujarse; corrección directa | patch |
| EC3 `vista = null` antes de `liberar()` | medium | Otro hilo puede devolver el foco de TalkBack con Unity aún oculta; basta cambiar el orden | patch |
| EC4 dedo apoyado al quitarse la vista | low | El arrastre pasa al mapa ya a la vista, como si se arrastrase; arreglarlo añade estado | reject |
| VG1 `Componer` pasa `inicio` | medium | Falta prueba | patch |
| VG2 los 4 s con otro reloj que el del pin | medium | Falta prueba | patch |
| VG3 `MapaVectorial.AcercarDesde` por la interfaz | medium | Falta prueba | patch |
| VG4 `CamaraMapa.Update` avanza el acercamiento | medium | Falta prueba | patch |
| VG5 se centra antes de decidir | medium | Falta prueba | patch |
| VG6 enlace corto sin resolver (`abrirSolo`) | medium | Falta prueba | patch |
| VG7 atrás con la pantalla de inicio | medium | Falta prueba | patch |
| VG8 foco de TalkBack al terminar | medium | Falta prueba | patch |
| VG9 margen de la vista | medium | Falta prueba | patch |

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md), `CompilarApk`, `CompilarPcPruebas` y captura; borrar `link.xml` y deshacer los `addressables_content_state.bin`.

**Manual checks:**
- En el móvil: sin negro ni Unity, animación fluida, pin hasta el punto azul, oscuro y sin animaciones.
