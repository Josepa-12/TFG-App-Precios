---
title: 'Varias rutas y la más rápida primero (parte 5a de «Cómo llegar»)'
type: 'feature'
created: '2026-11-23'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La app pide siempre la ruta más corta en distancia y solo una. De Sanlúcar a Chipiona en coche sale una de 9,7 km en 47 min cuando hay otra de 10,3 km en 22 min (Valhalla real, 23/11/2026). «Cómo llegar» (parte 5b) necesita hasta tres rutas para elegir.

**Approach:** El servicio de rutas da hasta tres rutas distintas ordenadas de menos a más tiempo, y la ruta de siempre (minutos de la ficha, Ir y recálculo) pasa a ser la más rápida. Sin cambios en la interfaz.

## Boundaries & Constraints

**Always:**
- Decidido (usuario, 23/11/2026): la más rápida es la primera y la que usan la ficha, Ir y el recálculo; la más corta sigue saliendo entre las opciones. Sustituye a «la más corta» de la 2.6.
- En coche: más rápida (`shortest: false`). A pie: como hoy (`shortest: true`), porque andando apenas se diferencian.
- Alternativas con Valhalla: una petición con `alternates: 2`; en coche, si da menos de tres, otra detrás (no a la vez) con `shortest: true` para añadir la más corta. Si esa segunda falla, valen las que hay.
- Se juntan sin repetidas (mismos metros y segundos con un margen del 2 %), en orden de tiempo, tres como mucho.
- Si Valhalla falla o tarda más de 8 s, OSRM con sus alternativas; si fallan los dos, la aproximada de las calles guardadas, sola. El límite de 8 s es para el total de cada servicio; si vence durante la segunda petición de Valhalla, valen las de la primera.
- Caché de las alternativas con las mismas reglas que hoy (2 min, 20 m, modo y destino; la aproximada no se guarda). Al llegar, la primera queda también como la ruta guardada de `Calcular`, para que la ficha y la lista no se contradigan; si `Calcular` ya la tenía guardada, la lista no obliga a cambiarla.

**Never:** guardar rutas en el móvil (FR17); cambiar de servidor; tocar la interfaz o la navegación; peticiones en paralelo de más a FOSSGIS.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Coche con tres | Valhalla da viaje y 2 alternativas | 3 rutas por tiempo; una sola petición |
| Coche con una | Valhalla da solo el viaje | Segunda petición `shortest`; 2 rutas por tiempo |
| La corta es la misma | La segunda repite la primera | 1 ruta |
| Segunda falla | Error o sin respuesta a tiempo en la segunda | Las de la primera, éxito |
| A pie | Valhalla da viaje y alternativas | Por tiempo; nunca segunda petición |
| Reserva | Valhalla falla | Las de OSRM por tiempo, 3 como mucho |
| Sin red | Fallan los dos | La aproximada sola |
| Alternativa rota | Una alternativa con línea ilegible | Se salta; las demás valen |
| Ruta de siempre | `Calcular` en coche | La más rápida (Valhalla `shortest: false`; OSRM, la de menos tiempo) |
| Caché | Segunda petición de alternativas antes de 2 min | Sin red, la misma lista |
| Cancelar | Se cancela durante cualquiera de las dos | `OperationCanceledException` |

</frozen-after-approval>

## Code Map

Rutas bajo `unity/PriceMap/Assets`.
- `Scripts/Aplicacion/IRutas.cs` -- método nuevo con implementación por defecto (los dobles de las pruebas y `RutasCallesGuardadas` no cambian): `Alternativas(...)` → `Resultado<IReadOnlyList<Ruta>>` con la de `Calcular` sola.
- `Scripts/Aplicacion/ServicioRutas.cs` -- `Calcular` (caché → principal → reserva → local), `ConLimite`, `Guardada`; hace falta la versión de listas sin duplicar la lógica del límite (hacerla genérica). Para que el límite de 8 s no tire las de la primera petición, la segunda de Valhalla la pide el propio adaptador con un plazo menor o el adaptador devuelve lo que tiene al cancelarse por tiempo (nunca si cancela el llamante).
- `Scripts/Adaptadores/Rutas/RutasValhalla.cs` -- `Cuerpo` (costing, `shortest`); añadir `alternates`. `LecturaValhalla.cs` -- `Leer` lee `trip`; las alternativas llegan en `alternates[].trip` con la misma forma: sacar la lectura de un `Viaje`.
- `Scripts/Adaptadores/Rutas/RutasOsrm.cs` (ya pide `alternatives=3`) y `LecturaOsrm.cs` (hoy se queda con la de menor distancia).
- `Scripts/Dominio/Ruta.cs` -- `Segundos`, `Metros`, `Aproximada`. La regla de juntar y ordenar, pura, en Dominio.
- Comprobado el 23/11/2026 con Valhalla real (Sanlúcar–Chipiona): coche `shortest: false` da solo el viaje (10,36 km, 1319 s) aunque se pidan 2 alternativas; `shortest: true` da 9,70 km/2825 s, 10,10/2746 y 10,35/1348. OSRM da una.
- Pruebas: `Tests/EditMode/PruebasRutas.cs` (lectores, cuerpo, servicio); las que fijan `shortest` en coche o «la más corta» de OSRM pasan a la conducta nueva. Buscar también «más corta» en comentarios.

## Tasks & Acceptance

**Execution:**
- [ ] `Scripts/Dominio` -- regla pura de ordenar, quitar repetidas y limitar.
- [ ] `Scripts/Aplicacion/IRutas.cs`, `ServicioRutas.cs` -- alternativas con reserva, local, límite y caché.
- [ ] `Scripts/Adaptadores/Rutas` -- Valhalla y OSRM con alternativas; `Calcular` en coche, la más rápida.
- [ ] `Tests/EditMode` -- una prueba por fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.
- Dada la captura de PC, cuando se ejecuta, entonces no hay errores en el registro.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes (CLAUDE.md) -- todas en verde.
- La build y la captura de PC las hace el orquestador al final.
