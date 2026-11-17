# PriceMap en Unity

App Android de PriceMap hecha con Unity 6.3 (6000.3.28f1, render integrado). El mapa real de Sanlúcar se dibuja a partir de las teselas vectoriales de OpenStreetMap que sirve OpenFreeMap, sin Cesium ni proveedores con cuenta.

## Abrirlo
En Unity Hub, **Add / Añadir** → carpeta `unity/PriceMap`, con el editor 6000.3.28f1. La escena `Assets/Scenes/Principal.unity` se abre sola la primera vez. Pulsa **Play** para cargar el mapa.

Para volver a generar la escena desde cero: menú **PriceMap ▸ Preparar escena principal**.

## Estructura (arquitectura hexagonal, AD-1)
Cada capa es un ensamblado (`.asmdef`). La prueba `PruebasCapas` falla si una capa referencia otra que no le corresponde.

| Carpeta | Ensamblado | Puede usar |
|---|---|---|
| `Assets/Scripts/Dominio` | `PriceMap.Dominio` (C# puro, sin Unity) | nada |
| `Assets/Scripts/Aplicacion` | `PriceMap.Aplicacion` (servicios y puertos, p. ej. `IMapa`) | Dominio |
| `Assets/Scripts/Adaptadores/*` | `PriceMap.Adaptadores.*` | Aplicación, Dominio |
| `Assets/Scripts/Vectorial` | `PriceMap.Vectorial` (el mapa: `MapaVectorial`, `LectorMVT`, `Triangulador`, `Geo`, `CamaraMapa`, `Marcador`) | Aplicación, Dominio |
| `Assets/Scripts/UI` | `PriceMap.UI` (pantallas) | Aplicación, Dominio |
| `Assets/Scripts/Arranque` | `PriceMap.Arranque` (raíz de composición) | todas |
| `Assets/Tests/EditMode` | `PriceMap.Tests.EditMode` | — |

El componente **Arranque** de la escena es el único que conecta las piezas: entrega el mapa (por el puerto `IMapa`) a la `PantallaMapa`.

## Controles del mapa
- Táctil: un dedo mueve; dos dedos amplían y giran.
- Ratón: arrastrar mueve, la rueda amplía y el botón derecho gira e inclina.
- Teclado: WASD o flechas mueven, Q/E giran, R/F inclinan y **T** cambia de tema (Claro, Oscuro u Original).

Las teselas se guardan en `persistentDataPath/vectorial`, así que lo ya visto se dibuja sin conexión.

## Pruebas
Desde **Window ▸ General ▸ Test Runner ▸ EditMode**, o por línea de comandos:
```
Unity.exe -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults resultados.xml
```

## APK de Android
Menú **PriceMap ▸ Compilar APK**, o por línea de comandos:
```
Unity.exe -batchmode -quit -buildTarget Android -projectPath unity/PriceMap -executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarApk
```
Genera `Builds/PriceMap.apk` (`com.pricemap.app`, minSdk 26, targetSdk 35, IL2CPP ARM64, solo vertical). Para instalarla, cópiala al móvil y ábrela, permitiendo instalar apps de origen desconocido. Está firmada con la clave de depuración de Unity, que vale para pruebas pero no para publicar. Para compilar hace falta el módulo Android Build Support del editor, con su SDK, NDK y OpenJDK.

Atribución: OpenFreeMap © OpenMapTiles · Datos © colaboradores de OpenStreetMap.
