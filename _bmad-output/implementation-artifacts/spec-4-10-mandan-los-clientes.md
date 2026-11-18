---
title: 'Historia 4.10: mandan los clientes'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-3-calcular-el-precio-su-origen-su-frescura-y-sus-variantes.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `CalculadoraPrecio` (1.3) solo conoce la web y lo declarado; los aportes de los clientes no cambian el precio mostrado (FR31, FR32, AD-3).

**Approach:** Ampliar el Dominio: con al menos 3 aportes en los últimos 6 meses de un concepto o variante, el precio de los clientes sustituye al de la web o al declarado, con origen "según N clientes". El valor sale de estadísticos según la forma. Todo puro, con pruebas EditMode, y nunca se guarda.

## Boundaries & Constraints

**Always:**
- **Ventana:** aportes con `creado` en los últimos 183 días respecto a "ahora" (`ParametrosNegocio.VentanaAportesDias`).
- **Umbral:** ≥ 3 aportes en la ventana (`ParametrosNegocio.MinAportesClientes`).
- **Estadísticos** (`Estadisticos`, Dominio, puro, sobre céntimos enteros):
  - Fijo: mediana de `importeCent` (con número par, media de los dos centrales redondeada al céntimo).
  - Rango: percentil 25 de `minCent` y percentil 75 de `maxCent` (tramos abiertos: `maxCent` null cuenta como `minCent` para el percentil 75 y, si el resultado es un tramo abierto, se muestra "más de 50 €").
  - Desde: percentil 15 de `importeCent` (en el rango 10–20 que pide FR31).
  - Percentiles con interpolación lineal (método "R-7"), redondeo al céntimo.
- **Formatos de redondeo para mostrar:** los de `FormatoPrecio` (1.8); el Dominio da céntimos.
- **Origen:** `TipoOrigen.Clientes` con `N` y la fecha del aporte más reciente; la ficha: "Según 4 clientes · 12/11/2026".
- **Frescura:** con el aporte más reciente como última confirmación (FR33).
- **Variantes:** se aplica por variante con sus aportes (`conceptoId` = `concepto@variante`).
- **Menos de 3:** manda la web o lo declarado como hoy; los aportes no cambian ni el precio ni la frescura de la fuente oficial (ver la pregunta 1).
- **Nunca se guarda** el precio calculado (prueba de que el adaptador no escribe campos de precio en `lugares`).

**Never:** sin medias ponderadas por reputación (no está en los requisitos).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| 2 aportes | Web 40 €; aportes 45, 50 | 40 € "Publicado en su web" |
| 3 aportes fijo | 45, 50, 60 | 50 € "Según 3 clientes" |
| 4 aportes fijo | 40, 45, 50, 60 | 47,50 € |
| Rango | Tramos [10–20]×3, [20–30]×1 | P25 de min = 10 €, P75 de max = 22,50 € → "10–22,50 € por persona" |
| Desde | 20, 25, 30, 40 | P15 = 22,25 € → "desde 22,25 €" |
| Antiguos | 3 aportes de hace 7 meses | No mandan |
| Variante | 3 aportes `superinductiva@con-bono` | Solo cambia esa variante |
| Frescura | Último aporte hace 10 días (hostelería) | "Al día" |

</frozen-after-approval>

## Open Questions

1. **Con 1 o 2 aportes, ¿cuentan para la frescura?** A) No: mientras no manden los clientes, la frescura es la de la fuente oficial (recomendado: sencillo y coherente con "origen"). B) Sí: un aporte que coincide renueva la fecha.
2. **Redondeo de rangos.** A) Al céntimo, como sale del cálculo (recomendado: exacto y probado). B) Al euro más cercano para que se lea mejor ("10–23 €").

## Code Map

- Nuevos: `Dominio/Estadisticos.cs`, `Dominio/AporteCliente.cs` (o reutilizar `Aporte` de la 4.4).
- `Dominio/CalculadoraPrecio.cs`, `FuentesPrecio.cs`, `Origen.cs`/`TipoOrigen.cs`, `ParametrosNegocio.cs`.
- `UI/FormatoPrecio.cs` y textos de origen ("Según N clientes").
- Tests: `PruebasPrecio.cs` (ampliar) y `PruebasEstadisticos.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `Estadisticos` (mediana, percentiles R-7) y pruebas.
- [ ] `CalculadoraPrecio` con aportes, ventana y umbral.
- [ ] Origen "según N clientes" en la ficha.
- [ ] Pruebas de cada fila.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con valores exactos en céntimos.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes.
