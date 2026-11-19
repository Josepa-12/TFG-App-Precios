# PriceMap — mapa rápido (leer esto antes de abrir archivos)

App Android (Unity 6000.3.28f1, UI Toolkit, Localization) que muestra precios de negocios de Sanlúcar sobre un mapa vectorial OSM propio (sin Cesium). TFG. Rama de trabajo: `unity`. Todo en español (código, textos, commits).

## Reglas de ahorro de tokens
- No releer specs/planning enteros: usar Grep con rango. Specs cerrados (1.1–1.6) no hace falta abrirlos.
- Archivos grandes, leer solo la parte necesaria: `MapaVectorial.cs` (698 l.), `PantallaMapa.cs` (744), `IconosLugar.cs` (405), `CapturaPruebas.cs` (555), `AccesibilidadPantalla.cs` (431).
- Nunca leer `.meta`, `.unity`, `.asset` salvo que la tarea los toque.

## Capas (Assets/Scripts, un asmdef por carpeta)
- **Dominio** (sin dependencias): `Lugar`, `Concepto`, `Variante`/`CondicionVariante`/`TipoCondicion`, `Declaracion`, `PrecioWeb`, `FuentesPrecio`, `ValorPrecio`, `FormaPrecio`, `Origen`/`TipoOrigen`, `Frescura`/`EstadoFrescura`, `PrecioMostrado`, `ResumenLugar`, `Coordenada`, `Tema`, `Plantilla`, `FiltroPrecios`, `CategoriaPoi`, `ParametrosNegocio`. Lógica pura: `CalculadoraPrecio`, `ReglaVariantes`, `ReglaResumen`, `ReglaFiltro`, `ReglaTema`, `ReglaPuntosInteres`.
- **Aplicacion** (→Dominio): puertos `IMapa`, `IUbicacion` (con `IniciarRuta/ActualizarNotificacion/TerminarRuta`), `IRepositorioLugares`, `ITemaSistema`, `IAccesibilidadSistema` (animaciones quitadas), `IPreferencias`; servicios `ServicioLugares`, `ServicioFiltro`, `ServicioTema`, `ServicioUbicacion` (+`EstadoUbicacion`, `AccionUbicacion`); `Resultado<T>`, `PuntoPantalla`, `PuntoInteres`.
- **Adaptadores**: `Datos/RepositorioLugaresJson` (datos en `Assets/Datos/Resources`), `Sistema/TemaSistemaAndroid`, `Sistema/AccesibilidadSistemaAndroid`, `Sistema/PreferenciasLocales`, `Gps/UbicacionUnity` (componente fino → `UbicacionGps` sobre `IFuenteUbicacion`; real: `FuenteUbicacionUnity`). Ruta (2.1): `UbicacionGps` usa `IServicioRuta` (real `ServicioRutaAndroid`, solo Android, JNI) y `LecturaRuta` (cola en texto).
- **Plugin Android** (`Assets/Plugins/Android`, paquete `com.pricemap.ruta`): `ServicioUbicacionRuta` (servicio en primer plano `location`) y `PuenteUbicacionRuta` (`iniciar/actualizar/parar/vaciarCola`).
- **Vectorial** (→Aplicacion, Dominio): `MapaVectorial` (implementa IMapa), `CamaraMapa` (+`DetectorToque`: toque < 6 dp → `IMapa.Toque`), `GestoDosDedos`, `PlanTeselas`, `LectorMVT`, `Triangulador`, `Geo`, `PaletaMapa`, `TemaMapa`.
- **UI** (→Aplicacion, Dominio, Localization): `PantallaMapa` (MonoBehaviour raíz), `AccesibilidadPantalla` (TalkBack), `CapaMarcadores`+`MarcadorLugar`, `CapaPuntosInteres`+`PuntoInteresVista`, `PildoraPrecios`/`MenuPrecios`/`OpcionPrecios`/`PuntoPrecios` (filtro), `BotonRedondo` (iconos Sol/Luna/Mas/Menos/Navegar/Cerrar; en columna de cristal), `SombraSuave` (sombra dibujada), `FichaLugar`+`FilaConcepto`+`FilaVariante` (ficha del lugar, 1.8; modal en TalkBack), `MarcadorPosicion`, `ExplicacionUbicacion`+`AvisoUbicacion` (modal reutilizada), `HojaBuscador`+`FisicaHoja` (cápsula y hoja del buscador, 2.2; física pura), `IconosLugar`, `FormatoPrecio`, `EscalaFuente`, textos: `Textos`, `ClavesTexto`, `TextosFiltro`, `TextosPuntos`. UXML/USS en `Assets/UI` (`PantallaMapa.uxml/.uss`, `Estilos/Tokens.uss`).
- **Arranque** (→todo): `Arranque` (composición), `CapturaPruebas` (modo `-captura` de PC).
- **Editor** (`PriceMap.EditorTools.*`): `ConfigurarAndroid` (`CompilarApk`, `CompilarPcPruebas`), `PermisosUbicacionAndroid`, `PrepararEscenaPrincipal`, `PrepararInterfaz`.
- Textos traducibles: `Assets/Localizacion/Textos_es-ES.asset` + `Textos Shared Data.asset`.

## Pruebas (Assets/Tests/EditMode, `Pruebas<Tema>.cs`)
Arranque, Buscador, Capas, Contraste, Coordenada, EscenaPrincipal, Ficha, Filtro, GestosYTeselas, Lugares, MapaVectorial, Marcadores, PantallaMapa, PermisosUbicacionAndroid, Precio, PuntosInteres, Tema, Textos, Ubicacion.

## Comandos (con el editor de Unity CERRADO)
```
U="C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe"
"$U" -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults <scratch>/r.xml -logFile <scratch>/t.log
"$U" -batchmode -quit -buildTarget Android -projectPath unity/PriceMap -executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarApk -logFile <scratch>/b.log
```
Tras build de PC: borrar `Assets/AddressableAssetsData/link.xml` y deshacer `Windows/addressables_content_state.bin`.

## Proceso
- BMAD: planificación en `_bmad-output/planning-artifacts/` (epics.md, prd, architecture, ux); historias en `_bmad-output/implementation-artifacts/spec-*.md`; estado en `sprint-status.yaml`; pendientes en `deferred-work.md`.
- Memoria del TFG: `docs/memoria-tfg.md` (actualizar tras cada avance).
- No commitear sin revisión del usuario.
