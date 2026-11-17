---
title: 'Historia 1.4 (parte 1): datos base de los lugares de Sanlúcar y su resumen de precio'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: '6bbcc82e9ccbf6f509e97d44c5100409ffb464ac'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los 121 lugares reales del prototipo, los 3 añadidos a mano y las tarifas web de Clínica Areas no existen en Unity. Los marcadores (1.4, parte 2), el filtro (1.5), los POI (1.6) y la ficha (1.8) necesitan esos datos y saber si cada lugar tiene precio.

**Approach:**
- Un script de Node exporta los datos del prototipo a JSON empaquetado en `Assets/Datos/`.
- Un adaptador lo lee y lo convierte en `Lugar` del Dominio, con sus conceptos y sus `FuentesPrecio` de la 1.3.
- `ServicioLugares` (Aplicación) los carga y da, para cada lugar, un resumen calculado al leer: si tiene precio y el precio que se anuncia.

## Boundaries & Constraints

**Always:**
- Solo precios de la web oficial del negocio, cada uno con su página y su fecha de consulta (FR11, AD-11). Importes en céntimos `int`.
- Ids con slug: el prototipo no tiene ids numéricos de OSM, así que todos los ids son slugs (`bar-cachon`, `clinica-areas`). Los conceptos son `area/concepto` (`fisioterapia/sesion-de-fisioterapia`).
- El Dominio no usa UnityEngine, y "ahora" llega como parámetro (AD-12). Nada de lo calculado se guarda (AD-3).

**Decisiones (17/11/2026):**
- **Precio que se anuncia:** el importe más bajo de los conceptos que tienen precio, con la forma "desde". Clínica Areas → desde 2000 (20 €, Sesión AlterG). Si ningún concepto tiene importe, no hay precio que anunciar.
- **Precios con bono:** no se usan. Superinductiva, Entrenamiento personal y Entrenamiento grupal se quitan, y si el negocio los quiere, los añade él. Los conceptos "consultar" (`null`) se quedan, porque la web los ofrece aunque no dé el importe.
- **Sanidad pública:** el `Lugar` lleva `Publico`. Un lugar público nunca tiene precio ni conceptos con precio: el resumen lo trata como sin precio y la carga descarta sus conceptos. Los datos actuales no traen ninguno; en el mapa saldrán en la 1.6 como POI.
- Esta parte no toca el mapa ni la UI. Los marcadores, los iconos, TalkBack y la retirada de los marcadores fijos de la escena son de la parte 2.

**Never:** No inventar precios ni migrar `PLACE_CONCEPTS` (son sugerencias). Sin Newtonsoft (se usa `JsonUtility`). Sin formatear euros (es de la Presentación).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Migración | `REAL_PLACES` + `EXTRA_PLACES` + `WEB_PRICES` | 124 lugares con id único, nombre, tipo (`cat`), plantilla, lat/lon (el prototipo da [lon, lat]) y dirección si la hay | El script termina con error si hay ids repetidos, una plantilla desconocida o coordenadas fuera de Sanlúcar |
| Tarifas | Clínica Areas | 11 conceptos en 4 áreas, con url y fecha 13/11/2026: 8 con importe y 3 "consultar" (`null`) | N/A |
| Con bono | `['Superinductiva','sesión',15,{desde,nota:'con bono'}]` | El concepto no se exporta | N/A |
| Formas | 40 desde / 90 / `null` | Desde 4000 / Fijo 9000 / A consultar | N/A |
| Resumen | Clínica Areas, ahora 17/11/2026 | Tiene precio, desde 2000 | N/A |
| Sin precio | Bar Cachón; o un lugar con solo "consultar" | No tiene precio | N/A |
| Público | Lugar con `Publico` y una tarifa | No tiene precio y se queda sin conceptos | N/A |
| JSON dañado o ausente | Fichero ilegible o que no existe | Lista vacía y `Resultado` con error | Log `PriceMap:` en el adaptador; la app no se cae |

</frozen-after-approval>

## Code Map

- `prototipo/datos-lugares.js` (`REAL_PLACES`, 121) y `prototipo/datos-precios-web.js` (`EXTRA_PLACES`, 3, con `addr` y `about`; `WEB_PRICES` por id del lugar). Ambos son JS con `const`. Los ítems web son `[nombre, unidad, precio|null, {desde, nota}]` y `tpl` es `servicio|hosteleria|ocio`.
- `prototipo/herramientas/generar-lugares.js`: estilo de los scripts de Node que ya existen.
- `unity/PriceMap/Assets/Scripts/Dominio/` (de la 1.3), que se reutiliza sin cambiar firmas:
  - `FuentesPrecio(Plantilla, PrecioWeb, declaraciones)`
  - `PrecioWeb(ValorPrecio?, url, fecha)`
  - `ValorPrecio.Fijo/Desde/Rango`
  - `CalculadoraPrecio.Calcular`, que devuelve `PrecioMostrado` (`TienePrecio`, `Valor`)
  - `Variante`
  - `Coordenada`
- `Assets/Scripts/Adaptadores/Datos/`: el asmdef ya existe y está vacío (referencia Aplicación y Dominio). `JsonUtility` ya se usa en `Vectorial/MapaVectorial.cs:115`.
- `Assets/Scripts/Aplicacion/ServicioTema.cs`: estilo de servicio de la Aplicación. Busca si ya existe un `Resultado<T>`; si no existe, créalo en Aplicación.
- `Assets/Scripts/Arranque/Arranque.cs`: crea el repositorio y el servicio y llama a `Cargar()`. Aún no se lo pasa a la UI.
- `Assets/Tests/EditMode/`: NUnit y `PruebasCapas` (el Dominio no usa UnityEngine). El asmdef de pruebas tiene que referenciar `PriceMap.Adaptadores.Datos`.

## Tasks & Acceptance

**Execution:**
- [x] `prototipo/herramientas/exportar-datos-unity.js`: carga los dos ficheros con `vm` y escribe `unity/PriceMap/Assets/Datos/Resources/lugares.json`. Valida, pasa a céntimos (`Math.round(x*100)`), descarta los conceptos con bono y escribe un resumen ("124 lugares, 11 conceptos").
- [x] `Dominio/Lugar.cs`, `Concepto.cs`, `ResumenLugar.cs` y `ReglaResumen.cs`: `ReglaResumen.Resumir(Lugar, DateTime ahora)` usa `CalculadoraPrecio`.
- [x] `Aplicacion/IRepositorioLugares.cs` y `Aplicacion/ServicioLugares.cs`: `Cargar()`, `Lugares` y `Resumen(id, ahora)`.
- [x] `Adaptadores/Datos/RepositorioLugaresJson.cs`, con DTOs `[Serializable]` internos: lee `Resources.Load<TextAsset>("lugares")` y tiene un constructor con el texto para las pruebas.
- [x] `Arranque/Arranque.cs`: componer el repositorio y el servicio, y registrar en el log el número de lugares cargados.
- [x] `Tests/EditMode/PruebasLugares.cs`: cada fila de la matriz. Comprueba también la integridad del JSON real: 124 lugares, ids únicos, ningún concepto fuera de `clinica-areas`, todos los precios con url y fecha, y lat/lon dentro de Sanlúcar.

**Acceptance Criteria:**
- Dado el JSON generado, cuando se compara con el prototipo, entonces cada importe coincide en céntimos y los tres conceptos con bono no aparecen.
- Dado que se arranca la app (build de PC), cuando se lee el log, entonces aparece "PriceMap: 124 lugares cargados".

## Implementation Notes

- Implementado por un subagente. Pasan 110/110 pruebas EditMode, 23 de ellas de `PruebasLugares`. El script exporta "124 lugares, 11 conceptos (3 con bono descartados)" y al volver a ejecutarlo el JSON sale igual. Con un id repetido o una plantilla desconocida, sale con código 1 y no escribe nada (comprobado con una copia aislada). En la build de PC el log dice "PriceMap: 124 lugares cargados.".
- `Resultado<T>` no existía y se ha creado en Aplicación (`Ok`, `Valor`, `Error`). Un fallo puede llevar un valor sustituto (una lista vacía).
- La caja de Sanlúcar es lat 36,70-36,85 y lon −6,45 a −6,25. La comparten el script y las pruebas.
- Se borra `Adaptadores/Datos/AssemblyInfo.cs`, que solo estaba para que el ensamblado compilara hasta esta historia.
- Si un lugar del JSON está mal, falla todo el fichero (lista vacía) y no solo ese lugar. El script valida antes, así que solo pasaría si se edita el JSON a mano.
- El texto `about` del prototipo no se migra porque el spec no lo pide. La ficha (1.8) puede necesitarlo.

## Spec Change Log

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | VG | No se prueba la rama del adaptador cuando falta el recurso: la prueba usa un repositorio falso | medium | Sin el `if (asset == null)`, `asset.text` lanza fuera del `try` y tumba `Arranque.Start` → patch: constructor `internal` con el nombre del recurso y prueba con un recurso que no existe |
| 2 | VG | Una fecha mal escrita o una forma desconocida no tienen prueba | medium | Los 6 casos de JSON dañado no las cubren → patch: dos `TestCase` más |
| 3 | BH | Si la carga falla, `Arranque` escribe "0 lugares cargados" como si hubiera ido bien | low | Ignora el `Resultado` → patch: escribir el log solo si sale bien |
| 4 | BH, EC, VG | `ServicioLugares.Lugares` muestra nulos y duplicados que `Buscar` descarta | low | `Lugares = r.Valor` sin filtrar → patch: lista filtrada |
| 5 | EC | `default(Resultado<T>).Error` es null y no "" | low | Propiedad automática → patch: campo con `?? ""` |
| 6 | EC | `Lugar` y `Concepto` guardan la lista de quien los crea | low | Si esa lista cambia después, cambia el objeto del Dominio → patch: copia |
| 7 | BH | Caracteres combinantes invisibles en la expresión regular, parámetro `plantilla` sin usar y un valor por defecto que nunca se aplica | low | → patch: `̀-ͯ` y quitar el código muerto |
| 8 | BH, VG | `AddressableAssetsData/link.xml` aparece en el diff | low | Se borró en `58e4bbb` y lo regeneró la build de PC de la verificación → borrado; ignorarlo en git va a deferred |
| 9 | VG | Ninguna prueba automática del log de `Arranque` | low | Gap ya verificado, disposición defer → deferred (parte 2, cuando el servicio llegue a `Componer`) |
| 10 | VG, BH | El script no tiene pruebas (tarifas sin lugar y demás) | low | No hay arnés de Node → deferred |
| 11 | BH | Los datos no llevan logo | false | El logo sale del `tipo` en la parte 2; el spec separa los datos del marcador |
| 12 | BH, EC | El adaptador acepta lat/lon ausentes, que quedan en (0, 0) | low | Solo pasa si se edita el JSON a mano (el script valida) y el arreglo añade guardas → rechazado |
| 13 | BH, EC | El adaptador acepta `cent` ≤ 0 | low | `CalculadoraPrecio` ya lo trata como "consultar" (`!EsValido`); solo pasa con un JSON editado a mano → rechazado |
| 14 | BH, EC | `web.grupos` ausente hace que el script lance un TypeError | false | El script falla de forma visible y no exporta: es el comportamiento correcto |
| 15 | BH | Un lugar público con tarifas pierde sus conceptos sin aviso | low | El spec lo pide así y los datos no traen ninguno → rechazado |
| 16 | BH, EC | Un slug vacío, `opciones` nulo, importes que redondean a 0, Infinity, un `tpl` heredado, `pos` en texto | low | Datos que el prototipo no tiene; cada uno añadiría una guarda → rechazados |
| 17 | BH | El JSON no lleva versión de esquema ni fecha | low | Ningún consumidor lo necesita aún → rechazado |
| 18 | BH | Falta `lugares.json` en el diff | false | Se excluyó del diff que se revisaba por tamaño; está en disco y va en el commit |
| 19 | BH | Las pruebas fijan 124 lugares y "solo `clinica-areas`" | false | Lo pide el spec (matriz y tareas); cambiarlo sería editar el spec |
| 20 | EC | `ReglaResumen` no tiene en cuenta las variantes | false | El spec decide "el más bajo de los conceptos", y el usuario descartó los precios con bono |
| 21 | EC | Un repositorio que lance excepción tumba el `Arranque` | low | El único adaptador real no lanza → rechazado |
| 22 | EC | Conceptos con id repetido dentro de un lugar | low | El script ya lo valida → rechazado |
| 23 | BH | Falta actualizar `docs/memoria-tfg.md` | false | No es un fallo del código; se actualiza al cerrar la historia |

## Verification

**Commands:**
- `node prototipo/herramientas/exportar-datos-unity.js` -- termina con código 0 y dice "124 lugares".
- `"C:/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe" -batchmode -projectPath unity/PriceMap -runTests -testPlatform EditMode -testResults <scratch>/resultados.xml -logFile <scratch>/tests.log` -- todas pasan (con el editor cerrado).
