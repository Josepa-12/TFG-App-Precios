---
title: 'Historia 1.3: Calcular el precio, su origen, su frescura y sus variantes'
type: 'feature'
created: '2026-11-17'
status: 'done'
baseline_commit: '58e4bbb488b90795427c8c33a6fcc060a769f051'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La ficha (historia 1.8) y los marcadores (historia 1.4) necesitan saber, para cada concepto y variante, qué precio mostrar, de dónde sale, lo reciente que es y qué variante está vigente. Si cada pantalla lo calcula a su manera, mostrarán precios distintos para el mismo concepto (AD-3).

**Approach:** Añadir al Dominio, en C# puro, una calculadora de precio que, a partir de las fuentes de un concepto o variante y de "ahora" pasado como parámetro, devuelve:
- el precio mostrado según su forma: fijo, rango por persona, "desde" o "A consultar";
- su origen;
- su frescura según la plantilla.

Una regla aparte indica qué variante está vigente en un instante. Todos los umbrales viven en `ParametrosNegocio` y cada regla tiene su prueba EditMode.

## Boundaries & Constraints

**Always:**
- El Dominio no referencia UnityEngine.
- Importes en céntimos (`int`). Los rangos son `[minCent, maxCent]`, con `maxCent` nulo si el rango es abierto.
- Ninguna función lee el reloj: "ahora" llega como parámetro (AD-12).
- Nada se guarda: todo se calcula al leer (AD-3).
- Ningún precio sale sin origen (NFR-6).
- Los umbrales y parámetros solo están en `ParametrosNegocio` (AD-14).

**Decisiones (17/11/2026, alcance por historias):**
- Esta historia cubre los orígenes *web del negocio* y *declarado por el negocio*, y el caso *sin precio*. Manda la declaración más reciente sobre la web.
- El origen *según N clientes* (mediana o percentiles con 3 aportes en 6 meses) es de la historia 4.10.
- El estado *en revisión* es de la 7.2.

**Frescura:** la última confirmación es la fecha de consulta de la web o la de la declaración más reciente.
- Hostelería: al día < 30 días, podría haber cambiado de 30 a 60 días y antiguo > 60 días.
- Servicio local y ocio: al día < 3 meses, podría haber cambiado de 3 a 6 meses y antiguo > 6 meses (meses de calendario).
- Los límites (30 y 60 días, 3 y 6 meses) cuentan como "podría haber cambiado".
- "A consultar" y "sin precio" no tienen frescura.

**Variantes:** cada variante tiene su propio precio y su frescura, y una condición:
- de horario, con franja de horas que puede cruzar la medianoche;
- de día de la semana;
- de tipo de cliente (socio, estudiante, con bono).

Solo las de horario y de día pueden estar vigentes; las de tipo de cliente nunca, porque no dependen del momento. Si coinciden varias, gana la primera de la lista.

**Never:** Sin UnityEngine, sin Firestore, sin UI y sin formatear texto: eso es de la Presentación (AD-10).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Web, desde | Fisioterapia: web "desde 4000", consulta 13/11/2026, ahora 17/11/2026 | Forma Desde, 4000, origen Web (url, 13/11/2026), Al día | N/A |
| Web, consultar | Infiltraciones: web sin importe | "A consultar", origen Web, sin frescura | N/A |
| Sin precio | ChemaBarber: sin web ni declaraciones | Sin precio, sin frescura | N/A |
| Manda la declaración | Web de mayo y declaración de octubre | Origen Declarado, frescura desde octubre | Declaraciones con importe ≤ 0 se ignoran |
| Frescura hostelería | Confirmado hace 29 / 30 / 60 / 61 días | Al día / Podría / Podría / Antiguo | N/A |
| Frescura servicio y ocio | Hace 3 meses menos 1 día / 3 meses / 6 meses / 6 meses y 1 día | Al día / Podría / Podría / Antiguo | N/A |
| Fecha futura | Confirmación posterior a "ahora" (reloj desfasado) | Al día | N/A |
| Rango hostelería | Declarado [1000, 2000] | Forma Rango 1000-2000; con `maxCent` nulo, rango abierto | min > max: se ignora la declaración |
| Variante vigente | "Con luz" 20:00-24:00 y "Fin de semana" sáb-dom; sábado 21:00 | Vigente "Con luz" (primera que coincide) | Sin coincidencias: ninguna |
| Horario de noche | Franja 22:00-06:00; 02:00 | Vigente | N/A |

</frozen-after-approval>

## Code Map

- `unity/PriceMap/Assets/Scripts/Dominio/`: ya están `Coordenada`, `Tema` y `ReglaTema` (estilo de las reglas puras y estáticas). El `.asmdef` tiene `noEngineReferences: true`.
- `unity/PriceMap/Assets/Tests/EditMode/`: pruebas NUnit con `[TestCase]` (ver `PruebasTema.cs`). El ensamblado de pruebas ya referencia `PriceMap.Dominio`.
- `prototipo/extras.js:16-24` (`FRESH`, `freshState`) y `prototipo/app.js:24-33` (`mkItem`, `declared` y `ask`): lógica de referencia. El prototipo usaba 90/180 días; aquí se usan meses de calendario.
- `prototipo/datos-precios-web.js:30-60`: forma de los datos de la web (url, fecha y, por concepto, importe o `null` = consultar, `desde` y `nota`). Sirve de ejemplo para las pruebas; la migración de datos es de la historia 1.4.

## Tasks & Acceptance

**Execution:**
- [x] `Dominio/`, un tipo por fichero:
  - `Plantilla` (ServicioLocal, Hosteleria, Ocio)
  - `FormaPrecio` (Fijo, Rango, Desde)
  - `TipoOrigen` (Web, Declarado, Clientes, SinPrecio)
  - `EstadoFrescura` (AlDia, PodriaHaberCambiado, Antiguo)
  - `ValorPrecio` (forma, `importeCent` o `minCent`/`maxCent`, validación > 0)
  - `PrecioWeb` (valor o "consultar", url, fecha de consulta)
  - `Declaracion` (valor, creado)
  - `FuentesPrecio` (plantilla, web y declaraciones)
  - `PrecioMostrado` (valor, `EsConsultar`, origen, frescura y última confirmación)
  - `Origen`
  - `Frescura`
- [x] `Dominio/ParametrosNegocio.cs`: umbrales de frescura por plantilla (30/60 días; 3/6 meses).
- [x] `Dominio/CalculadoraPrecio.cs`: `Calcular(FuentesPrecio, DateTime ahora)` y `Frescura(Plantilla, DateTime ultima, DateTime ahora)`.
- [x] `Dominio/Variante.cs`, `CondicionVariante.cs` (horario, días o tipo de cliente) y `ReglaVariantes.cs` con `Vigente(IReadOnlyList<Variante>, DateTime ahoraLocal)`.
- [x] `Tests/EditMode/PruebasPrecio.cs`: cada fila de la matriz y los límites exactos de frescura en las dos plantillas.

**Acceptance Criteria:**
- Dado el ensamblado Dominio, cuando se compila, entonces no referencia UnityEngine (lo comprueba `PruebasCapas`).
- Dada cualquier entrada válida, cuando se calcula, entonces el resultado tiene siempre un origen (Web, Declarado o SinPrecio) y, si hay importe, es mayor que 0.

## Design Notes

`FuentesPrecio` ya prevé aportes, para que la historia 4.10 añada el origen *Clientes* sin cambiar la firma de `Calcular`, pero en esta historia no se rellenan. La vigencia usa la hora local del lugar (España), que le pasa la Aplicación. La frescura usa la hora del servidor estimada (AD-12). Son dos parámetros distintos.

## Verification

**Commands:**
- Pruebas EditMode por lotes: todas pasan, incluidas las nuevas.

## Implementation Notes

- Implementado directamente en la sesión. Son 17 tipos en el Dominio (sin UnityEngine) y pasan las 89 pruebas EditMode, 33 de ellas sobre el precio.
- La función de frescura se llama `CalculadoraPrecio.Estado(plantilla, ultima, ahora)`, no `Frescura`, para no coincidir con el nombre del tipo `Frescura`.
- `FuentesPrecio` solo tiene web y declaraciones. Los aportes (4.10) y los avisos (7.2) se añadirán cuando se necesiten, en lugar de dejar ahora campos vacíos.
- Las fechas `Local` se pasan a UTC antes de comparar. Las `Unspecified` se dejan tal cual. La vigencia de las variantes usa la hora local del lugar, que le pasa la Aplicación.

## Review Triage Log

Primera pasada (17/11/2026). Revisores: Blind Hunter (BH), Edge Case Hunter (EC) y Verification Gap (VG).

| # | Origen | Hallazgo | Veredicto | Evidencia / ruta |
|---|---|---|---|---|
| 1 | BH | `default(Origen)` parecía "de la web" sin url | medium | `TipoOrigen.Web` era el 0 → patch: `SinPrecio = 0` y prueba |
| 2 | BH, EC | `DateTime.Kind` mezclado (UTC del servidor y hora local) desplaza los límites | medium | → patch: `Comparable()` pasa a UTC las fechas `Local` en `Calcular` y `Estado`, con prueba |
| 3 | EC | Una fecha extrema (MaxValue) desborda `AddDays`/`AddMonths` | low | → patch: si la confirmación es posterior a "ahora", sale "al día" antes de sumar |
| 4 | BH, EC | Con dos declaraciones de la misma fecha gana la primera de la lista | low | → patch: con empate gana la última, con prueba |
| 5 | BH | No se mostraba que una declaración antigua mande sobre una web reciente | low | Es la regla del spec → patch: prueba que lo deja explícito |
| 6 | VG | `Calcular` no se probaba con hostelería ni con el estado de una declaración | medium | → patch: 45 días en hostelería = "podría", y declaración de hace 7 meses = "antiguo" |
| 7 | VG, BH | La franja de día (desde < hasta) y "todo el día" sin probar; la documentación de `Horario` estaba mal | medium | → patch: pruebas 09:00-14:00 (límites), 20:00-00:00 a las 23:59 y 00:00, y todo el día; documentación corregida |
| 8 | BH, EC | Las fábricas aceptan entradas inválidas (`EnDias(null)`/vacío, horas fuera de 0-24 h, cliente vacío, web sin url) | low | → patch: validación con excepción y prueba |
| 9 | BH | `PrecioMostrado` se podía construir con estados contradictorios | low | → patch: el constructor es `internal`; solo lo crea la calculadora |
| 10 | BH, VG | La comprobación `Enum.IsDefined` siempre era verdadera | low | → patch: la prueba comprueba el origen esperado de cada caso |
| 11 | BH | Frescura al instante y no por día de calendario | low | Coherente con "días" de 24 h; la UI mostrará el mismo cálculo → rechazado |
| 12 | BH | `AddMonths` en fin de mes (31 ago + 6 = 28 feb) | low | Comportamiento de meses de calendario, documentado en el código → rechazado |
| 13 | BH | La forma no se comprueba con la plantilla (rango solo en hostelería) | low | Lo garantizan los datos (historia 1.4) → rechazado |
| 14 | BH | Una variante solo admite una condición | low | Fuera del alcance del FR-60 en la v1 → rechazado |
| 15 | BH | `HashCode.Combine` podría no compilar | false | El proyecto compila con .NET Standard 2.1 (ya se usa en `Coordenada`) |
| 16 | BH | `ValorPrecio.ToString` muestra céntimos | false | Formatear en euros es de la Presentación (AD-10) |
| 17 | EC | Plantilla fuera del enum | low | Solo con un cast forzado → rechazado |
| 18 | EC | `ReglaVariantes` con hora UTC | low | Documentado: la Aplicación pasa la hora local del lugar → rechazado |
| 19 | EC | Claim: el método se llama `Estado` y no `Frescura` | low | Nombre documentado en Implementation Notes; arreglarlo sería editar el spec → rechazado |

Tras los parches: 89/89 pruebas EditMode.
