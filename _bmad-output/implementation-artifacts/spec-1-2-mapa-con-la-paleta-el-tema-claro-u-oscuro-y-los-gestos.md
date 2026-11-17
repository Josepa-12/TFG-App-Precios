---
title: 'Historia 1.2 (parte 1): Base de interfaz y tema claro u oscuro'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: 'e691736da5a7599c7817109a5378fde8253529fb'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La app no tiene interfaz real. El tema se cambia con una tecla de pruebas y la atribución se dibuja con `OnGUI`. No hay tokens de DESIGN.md, ni fuentes, ni textos localizables, ni TalkBack.

**Approach:** Crear con UI Toolkit la primera capa de interfaz sobre el mapa:
- **Botón de tema:** botón redondo arriba a la derecha que alterna entre claro (paleta B) y oscuro (paleta D) y cambia a la vez el mapa y la interfaz. El primer arranque sigue el tema del sistema y la elección se recuerda.
- **Base de estilo:** tokens USS, fuentes Varela Round y Nunito Sans escaladas como sp, y textos con el paquete Localization.
- **Accesibilidad:** con TalkBack aparecen los botones de acercar y alejar, con nombre, rol y estado, y zona táctil de 48 dp.

## Boundaries & Constraints

**Always:**
- La UI solo ve Aplicación y Dominio. El tema es un estado de `ServicioTema` (Aplicación) que observan la UI y el mapa (AD-15).
- El mapa solo se toca a través de `IMapa` (AD-13).
- Colores, medidas y tipografías solo en USS, con variables de DESIGN.md. Ningún color suelto en C#, salvo `PaletaMapa`, que es la paleta del mapa.
- 1 px del panel = 1 dp (PanelSettings de tamaño físico constante a 160 dpi). Los textos se escalan con `AccessibilitySettings.fontScale` (sp).
- Todos los textos visibles y las etiquetas de TalkBack salen de una tabla de Localization `es-ES`.
- Iconos vectoriales (Painter2D), nunca emojis. Se respetan las zonas seguras.

**Decisiones (17/11/2026):**
- La app solo tiene tema claro y oscuro. Se quitan el tema "Original" (colores de OSM) y la tecla T.
- La elección de tema se guarda en el móvil, en preferencias locales. En la Épica 3 pasará también a la cuenta.
- El usuario dividió la historia 1.2. La parte 2 (inclinar con dos dedos, teselas alrededor de la vista, 30 fps y uso sin conexión) está en `deferred-work.md` y se hace a continuación.

**Never:** La píldora "Precios", la cápsula, los botones de ubicación, € y altavoz y "reducir animaciones" son de otras historias. Los gestos y las teselas, de la parte 2.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Primer arranque, sistema oscuro | Sin tema guardado y sistema oscuro | Paleta D en el mapa y en la UI | N/A |
| Primer arranque, sistema claro | Sin tema guardado y sistema claro | Paleta B | N/A |
| Tema guardado | Guardado "oscuro" y sistema claro | Paleta D: manda lo guardado | Un valor guardado desconocido se trata como si no hubiera ninguno |
| Alternar | Tocar el botón de tema | El mapa y la UI cambian a la vez, se guarda la elección y TalkBack anuncia el nuevo estado | N/A |
| TalkBack activo | Lector de pantalla encendido | Aparecen "Acercar" y "Alejar"; cada botón tiene nombre, rol y estado | Al apagarlo, se ocultan |

</frozen-after-approval>

## Code Map

- `unity/PriceMap/Assets/Scripts/Vectorial/MapaVectorial.cs`:
  - `Update` tiene la tecla T y `OnGUI` dibuja la atribución y el tema. Los dos se quitan.
  - `AplicarTema()` pinta los materiales y los globales `_PL_*`; se reutiliza.
  - Implementa `IMapa.Centrar`.
- `unity/PriceMap/Assets/Scripts/Vectorial/CamaraMapa.cs`: `distancia` va de 60 a 4000. Hay que añadir `Acercar` y `Alejar` (×0,6 y ×1/0,6).
- `unity/PriceMap/Assets/Scripts/Vectorial/{TemaMapa,PaletaMapa}.cs`: quitar `Original`.
- `unity/PriceMap/Assets/Scripts/Aplicacion/IMapa.cs` solo tiene `Centrar`. `UI/PantallaMapa.cs` recibe `IMapa`, y `Arranque/Arranque.cs` lo compone con `Componer`.
- `unity/PriceMap/Assets/Editor/PrepararEscenaPrincipal.cs` genera la escena y `ConfigurarAndroid.cs` compila la APK.
- `unity/PriceMap/Assets/Tests/EditMode/`: las pruebas de capas recorren todos los `.asmdef`, y el nuevo adaptador tiene que cumplirlas. `PruebasEscenaPrincipal` comprueba cómo está conectada la escena.
- APIs de Unity 6.3 comprobadas en `UnityEngine.AccessibilityModule`:
  - `AssistiveSupport.isScreenReaderEnabled`, `screenReaderStatusChanged` y `activeHierarchy`;
  - `AccessibilityHierarchy.AddNode`;
  - `AccessibilityNode`: `label`, `role`, `state`, `frameGetter` e `invoked`;
  - `AccessibilitySettings.fontScale` y `fontScaleChanged`.

## Tasks & Acceptance

**Execution:**
- [x] `Scripts/Dominio/`:
  - `Tema.cs` con `Claro` y `Oscuro`.
  - `ReglaTema.cs` con `Inicial(string guardado, bool sistemaOscuro)` y `AClave(Tema)`.
- [x] `Scripts/Aplicacion/`:
  - Puertos `IPreferencias` (leer y guardar texto) e `ITemaSistema` (`EsOscuro`).
  - `ServicioTema` con `Actual`, `event Action<Tema> Cambiado`, `Iniciar()` y `Alternar()`.
  - `IMapa` gana `AplicarTema(Tema)`, `Acercar()` y `Alejar()`.
- [x] `Scripts/Adaptadores/Sistema/` (`PriceMap.Adaptadores.Sistema` → Aplicación y Dominio):
  - `PreferenciasLocales`, sobre PlayerPrefs.
  - `TemaSistemaAndroid`, que lee `Configuration.uiMode` por JNI y en el editor devuelve claro.
- [x] `Scripts/Vectorial/`:
  - `MapaVectorial` implementa los métodos nuevos (`Tema` → `TemaMapa`) y deja de tener la tecla T y `OnGUI`.
  - `CamaraMapa` gana `Acercar` y `Alejar`.
  - `TemaMapa` y `PaletaMapa` quedan solo con Claro y Oscuro.
- [x] `Assets/UI/`:
  - `Estilos/Tokens.uss`: variables de DESIGN.md en claro, con `.tema-oscuro` para los pares `-dark` y clases de tipografía `t-*`.
  - `PantallaMapa.uxml` y `PantallaMapa.uss`, siguiendo `mockups/key-mapa-inicio.html`:
    - botón de tema arriba a la derecha, a 16 dp del borde superior por debajo de la zona segura, de 48 dp y fondo `glass`;
    - pila de zoom debajo;
    - atribución abajo a la derecha (`caption`).
  - `Fuentes/`: Varela Round y Nunito Sans 400/600/800, con sus `OFL.txt` y los `FontAsset` dinámicos. Las instancias estáticas de Nunito Sans se generan con fonttools desde la fuente variable de google/fonts.
  - `PanelSettings.asset`.
- [x] `Scripts/UI/`:
  - `PantallaMapa` (UIDocument) se suscribe a `ServicioTema` y pone o quita `.tema-oscuro`.
  - `BotonRedondo` dibuja los iconos sol, luna, más y menos.
  - `EscalaFuente` aplica `fontScale` a los textos.
  - `AccesibilidadPantalla` construye la `AccessibilityHierarchy`, con `frameGetter` sacado de `worldBound` y pasado a coordenadas de pantalla.
  - Zonas seguras con `Screen.safeArea`.
- [x] Localization:
  - `com.unity.localization` 1.5.14 en el manifiesto.
  - Script del editor que crea la locale `es-ES` y la tabla `Textos` con `tema.claro`, `tema.oscuro`, `tema.cambiar`, `mapa.acercar`, `mapa.alejar` y `mapa.atribucion`.
  - La UI lee los textos con `LocalizedString`.
- [x] `Scripts/Arranque/Arranque.cs`:
  - Crea `PreferenciasLocales`, `TemaSistemaAndroid` y `ServicioTema`.
  - Conecta el servicio con el mapa (`AplicarTema`) y con la UI.
  - Fija `Application.targetFrameRate = 60`.
- [x] `Editor/`:
  - `PrepararEscenaPrincipal` añade el UIDocument con PanelSettings y `PantallaMapa.uxml`.
  - `ConfigurarAndroid.CompilarApk` construye el contenido de Addressables antes que la APK.
- [x] `Tests/EditMode/`:
  - `PruebasReglaTema` cubre las filas de la matriz.
  - `PruebasServicioTema`, con dobles, cubre alternar, guardar, el evento y un valor guardado desconocido.
  - Prueba de que la tabla `Textos` tiene todas las claves en `es-ES`.
  - `PruebasEscenaPrincipal` también comprueba el UIDocument.

**Acceptance Criteria:**
- Dado el tema oscuro, cuando miro cualquier control, entonces su texto y sus bordes cumplen 4,5:1 y 3:1 sobre su fondo, con los pares `-dark`.
- Dada la fuente del sistema al 200 %, cuando veo la atribución, entonces no se corta.
- Dada la APK, cuando arranca, entonces se ve el mapa con el botón de tema y la atribución en el tema que corresponde, sin `OnGUI`.

## Design Notes

El tema vive en Aplicación y no en la UI porque lo usan dos adaptadores, el mapa y la UI, y en la Épica 3 se sincronizará con la cuenta. `ReglaTema` es una función pura para poder probar el arranque sin Android.

## Verification

**Commands:**
- `Unity.exe -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode ...`: todas pasan.
- `Unity.exe -batchmode -quit -buildTarget Android -projectPath unity/PriceMap -executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarApk`: termina con código 0 y genera la APK.

**Manual checks:**
- En el móvil: alternar el tema, cerrar y abrir la app y ver que lo recuerda, activar TalkBack y usar acercar y alejar, y poner la fuente al 200 %.
