# PriceMap — mapa rápido (leer esto antes de abrir archivos)

App Android (Unity 6000.3.28f1, UI Toolkit, Localization) que muestra precios de negocios de Sanlúcar sobre un mapa vectorial OSM propio (sin Cesium). TFG. Rama de trabajo: `unity`. Todo en español (código, textos, commits).

## Reglas de ahorro de tokens
- No releer specs/planning enteros: usar Grep con rango. Specs cerrados (1.1–1.6) no hace falta abrirlos.
- Archivos grandes, leer solo la parte necesaria: `MapaVectorial.cs` (860 l.), `PantallaMapa.cs` (2253), `IconosLugar.cs` (405), `CapturaPruebas.cs` (1372), `AccesibilidadPantalla.cs` (464).
- Nunca leer `.meta`, `.unity`, `.asset` salvo que la tarea los toque.

## Capas (Assets/Scripts, un asmdef por carpeta)
- **Dominio** (sin dependencias): `Lugar`, `Concepto`, `Variante`/`CondicionVariante`/`TipoCondicion`, `Declaracion`, `PrecioWeb`, `FuentesPrecio`, `ValorPrecio`, `FormaPrecio`, `Origen`/`TipoOrigen`, `Frescura`/`EstadoFrescura`, `PrecioMostrado`, `ResumenLugar`, `Coordenada`, `Tema`, `Plantilla`, `FiltroPrecios`, `CategoriaPoi`, `ParametrosNegocio`. Lógica pura: `CalculadoraPrecio`, `ReglaVariantes`, `ReglaResumen`, `ReglaFiltro`, `ReglaTema`, `ReglaPuntosInteres`, `ReglaBusqueda` (buscador, 2.3), `ReglaCerca` (categorías de «Encuentra cerca», 2.4), `LectorUbicacionCompartida` (enlaces de mapas y coordenadas pegados, 2.5b; `TextoCompartido` para lo compartido, 2.5c). Rutas (2.6): `ModoViaje`, `Ruta` (`Minutos`), `Maniobra`+`TipoManiobra`, `Polilinea` (precisión 5/6); `Ruta.Aproximada` y `GrafoCalles`+`PuntoCalle` (ruta sin conexión por las calles guardadas: unión de extremos a 5 m, A*, 300 m, 2.6c).
- **Aplicacion** (→Dominio): puertos `IMapa`, `IUbicacion` (con `IniciarRuta/ActualizarNotificacion/TerminarRuta`), `IRepositorioLugares`, `ITemaSistema`, `IAccesibilidadSistema` (animaciones quitadas), `IPreferencias`; servicios `ServicioLugares`, `ServicioFiltro`, `ServicioTema`, `ServicioUbicacion` (+`EstadoUbicacion`, `AccionUbicacion`), `ServicioBusqueda` (+`ResultadoBusqueda`, lugares y puntos, 2.3), `IGeocodificador` (`Buscar` e `Inverso`, 2.5)+`Direccion` y `BuscadorDirecciones` (espera 350 ms, cancelación, 2.3b), `ServicioCerca` (lista por categoría, 3 km, 50, 2.4), `IResolutorEnlaces` (enlaces cortos, 2.5b), `IEntradaCompartida` (`Tomar`, lo compartido con la app, 2.5c), `IRutas` y `ServicioRutas` (coche y a pie a la vez, reserva tras fallo u 8 s, caché 2 min/20 m en memoria, 2.6; tercer `IRutas local` si fallan los dos, sin caché, `ConRutaAproximada`, 2.6c); `Resultado<T>`, `PuntoPantalla`, `RectPantalla` (zona de encuadre en px, 2.6b), `PuntoInteres`. `IMapa` también dibuja y quita la ruta y la encuadra (`MostrarRuta/QuitarRuta/Encuadrar`, 2.6b).
- **Adaptadores**: `Datos/RepositorioLugaresJson` (datos en `Assets/Datos/Resources`), `Sistema/TemaSistemaAndroid`, `Sistema/AccesibilidadSistemaAndroid`, `Sistema/PreferenciasLocales`, `Sistema/EntradaCompartidaAndroid` (JNI, 2.5c), `Gps/UbicacionUnity` (componente fino → `UbicacionGps` sobre `IFuenteUbicacion`; real: `FuenteUbicacionUnity`). Ruta (2.1): `UbicacionGps` usa `IServicioRuta` (real `ServicioRutaAndroid`, solo Android, JNI) y `LecturaRuta` (cola en texto). Direcciones (2.3b): `Busqueda/GeocodificadorPhoton` (UnityWebRequest) y `LecturaPhoton` (GeoJSON, pura); `Busqueda/ResolutorEnlacesWeb` (sigue `Location`, 5 saltos, 2.5b). Rutas (2.6): `Rutas/RutasValhalla` (POST, `shortest`, `directions_type: instructions` para tener los textos), `Rutas/RutasOsrm` (reserva, la alternativa más corta), lectores puros `LecturaValhalla`/`LecturaOsrm`, `PeticionRutas`.
- **Plugin Android** (`Assets/Plugins/Android`, paquete `com.pricemap.ruta`): `ServicioUbicacionRuta` (servicio en primer plano `location`) y `PuenteUbicacionRuta` (`iniciar/actualizar/parar/vaciarCola`); `com.pricemap.compartir.PuenteCompartir` (`tomar`: lee y consume el intent SEND/VIEW, 2.5c).
- **Vectorial** (→Aplicacion, Dominio): `MapaVectorial` (implementa IMapa; línea de ruta con borde, colas 1960/1970, 9 m o 6 dp, 2.6b; `MostrarRuta(puntos, aproximada)` con colores suaves, 2.6c), `RutasCallesGuardadas` (IRutas sin red: `.pbf` guardados, 5×5 teselas, hilo aparte, 2.6c), `CamaraMapa` (`Encuadrar`, 2.6b; +`DetectorToque`: toque < 6 dp → `IMapa.Toque`; 500 ms quieto → `IMapa.PulsacionLarga`, 2.5), `GestoDosDedos`, `PlanTeselas`, `LectorMVT`, `Triangulador`, `Geo`, `PaletaMapa`, `TemaMapa`.
- **UI** (→Aplicacion, Dominio, Localization): `PantallaMapa` (MonoBehaviour raíz), `AccesibilidadPantalla` (TalkBack; `RecuperarFoco` tras rehacer el recorrido), `CapaMarcadores`+`MarcadorLugar`, `CapaPuntosInteres`+`PuntoInteresVista`, `PildoraPrecios`/`MenuPrecios`/`OpcionPrecios`/`PuntoPrecios` (filtro), `BotonRedondo` (iconos Sol/Luna/Mas/Menos/Navegar/Cerrar; en columna de cristal), `SombraSuave` (sombra dibujada), `FichaLugar`+`FilaConcepto`+`FilaVariante`+`FilaAcciones`/`BotonModo` (modos con minutos e Ir, 2.6) (ficha del lugar, 1.8; modal en TalkBack; también ficha de punto libre con `AbrirPunto`, 2.5), `MarcadorPosicion`, `ChinchetaPunto` (punto libre, 2.5), `TarjetaManiobra`+`IconoManiobra` y `TarjetaRuta` (tarjetas de la ruta arriba y abajo, 2.6b; Ir/quitar/reabrir en `PantallaMapa`; «≈» si es aproximada, 2.6c), `Toast` (3 s, reutilizable, 2.6c), `ExplicacionUbicacion`+`AvisoUbicacion` (modal reutilizada), `HojaBuscador`+`FisicaHoja` (cápsula y hoja del buscador, 2.2; física pura)+`FilaResultado` (resultados, X, 2.3; «Ir a este punto» con `TipoResultado.Coordenada`, 2.5b), `FormatoDistancia`, `IconosLugar`, `FormatoPrecio`, `EscalaFuente`, textos: `Textos`, `ClavesTexto`, `TextosFiltro`, `TextosPuntos`. UXML/USS en `Assets/UI` (`PantallaMapa.uxml/.uss`, `Estilos/Tokens.uss`).
- **Arranque** (→todo): `Arranque` (composición), `CapturaPruebas` (modo `-captura` de PC).
- **Editor** (`PriceMap.EditorTools.*`): `ConfigurarAndroid` (`CompilarApk`, `CompilarPcPruebas`), `PermisosUbicacionAndroid`, `CompartirAndroid` (filtros SEND text/plain y VIEW geo:, 2.5c), `PrepararEscenaPrincipal`, `PrepararInterfaz`.
- Textos traducibles: `Assets/Localizacion/Textos_es-ES.asset` + `Textos Shared Data.asset`.

## Pruebas (Assets/Tests/EditMode, `Pruebas<Tema>.cs`)
Arranque, Buscador, Busqueda, Capas, Cerca (2.4: lista y mapa por categoría), Compartir (2.5c: manifiesto, texto y pantalla), Direcciones, Contraste, Coordenada, EscenaPrincipal, Ficha, Filtro, GestosYTeselas, Lugares, MapaVectorial, Marcadores, PantallaMapa, PermisosUbicacionAndroid, Precio, PuntoLibre (2.5), PuntosInteres, Rutas (2.6: polilínea, lectores, servicio y fila de la ficha; 2.6b: Ir dibuja la ruta, tarjetas, encuadre y línea), RutaAproximada (2.6c: grafo, teselas MVT de prueba, servicio, pantalla y toast), UbicacionCompartida (2.5b: analizador y enlaces pegados), Tema, Textos, Ubicacion.

## Comandos (con el editor de Unity CERRADO)
```
U="C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe"
"$U" -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults <scratch>/r.xml -logFile <scratch>/t.log
"$U" -batchmode -quit -buildTarget Android -projectPath unity/PriceMap -executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarApk -logFile <scratch>/b.log
```
Tras build de PC: borrar `Assets/AddressableAssetsData/link.xml` (y su `.meta`) y deshacer `Windows/addressables_content_state.bin`.

## Proceso
- BMAD: planificación en `_bmad-output/planning-artifacts/` (epics.md, prd, architecture, ux); historias en `_bmad-output/implementation-artifacts/spec-*.md`; estado en `sprint-status.yaml`; pendientes en `deferred-work.md`.
- Memoria del TFG: `docs/memoria-tfg.md` (actualizar tras cada avance).
- No commitear sin revisión del usuario.

## Forma de trabajar (preferencias del dueño del proyecto)
- **Historias:** una a una con `bmad-build`. Las Open Questions de cada spec se preguntan una a una (opción recomendada primero); resueltas, se implementa y revisa sin parar. Parar y avisar al cerrar cada épica. Subagentes permitidos (implementación y revisiones). Los specs de más de ~1.600 tokens se suelen partir (2.6 → 2.6a, 2.6b…).
- **BMAD completo:** no saltar ningún paso de un flujo, aunque se pida ir rápido (rápido = menos preguntas, no menos pasos).
- **Commits:** commit de seguridad en cuanto una historia está implementada y ha pasado la revisión de código de BMAD; el usuario lo revisa después (si pide cambios, otro commit). Un commit por historia o parte. Push a `origin/unity` solo cuando lo pida; `main` no se toca.
- **Cambios visuales:** enseñar antes una maqueta (página Artifact); al cerrar cada historia, una página con las capturas reales de la build de PC (claro y oscuro, tamaño móvil) y una lista corta de cambios.
- **Al explicar:** opciones con palabras sencillas, en español.
- **Memoria del TFG:** actualizar `docs/memoria-tfg.md` (capítulo, marcas [Redactado]/[Parcial]/[Pendiente], fecha y anexo de decisiones) tras cada decisión o avance.
- **Nombre:** la app es **PriceMap** (`com.pricemap.app`); `PrecioLocal.*` solo en namespaces internos, no renombrar sin preguntar. Mapa OSM/OpenFreeMap, sin Cesium.

## Trucos del entorno
- Unity por lotes solo con el editor cerrado; las clases del editor con su namespace completo (`PriceMap.EditorTools.ConfigurarAndroid.CompilarPcPruebas`).
- La captura de PC (`PriceMap.exe -captura`) se queda en pausa si la ventana pierde el foco: lanzarla con `timeout` y repetir si se cuelga.
- Pruebas que llamen a `MapaVectorial.Update` deben poner `sombras = false` (si no, escriben `shadowDistance` en `ProjectSettings/QualitySettings.asset`).

## Estado (21/11/2026)
- Hechas: épica 1 entera; 2.1–2.5; 2.6 partes 1 y 2a (`560617c`); 2.6 parte 2b (spec `spec-2-6c`, ruta aproximada sin conexión, 897/897 pruebas, sin commit ni revisión aún). Sprint 2-6 en `review`.
- **Siguiente:** revisión de código de la 2.6c y commit; luego 2.7–2.9.
- Decidido: los textos de maniobra de Valhalla se aceptan en forma de usted («Gire a la derecha…»).
- Pendientes abiertos: pruebas en un móvil real (TalkBack, arrastre, rutas reales por calles de sentido único); Photon no encuentra «calle ancha 10». Épicas 3+ necesitan el proyecto de Firebase y `google-services.json` (spec 3.2).
