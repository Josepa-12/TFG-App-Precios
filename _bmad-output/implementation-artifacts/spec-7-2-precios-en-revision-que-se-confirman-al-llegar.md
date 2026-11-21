---
title: 'Historia 7.2: precios en revisión que se confirman al llegar'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 7'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-7-1-avisar-de-que-un-precio-esta-mal.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los avisos no tienen efecto visible y nadie confirma el precio dudoso (FR35, AD-3).

**Approach:** El Dominio calcula "en revisión" al leer (avisos del concepto posteriores al último aporte con peso ≥ 3). La ficha muestra "En revisión" con explicación. La pregunta de llegada pide confirmar ese concepto primero. Confirmar o aportar otro cierra la revisión (el aporte nuevo deja los avisos anteriores fuera del cálculo).

## Boundaries & Constraints

**Always:**
- **Regla** (`ReglaRevision`, Dominio, pura): por concepto (o variante), `suma(peso de avisos con creado > fecha del último aporte del concepto)` ≥ 3 (`ParametrosNegocio`) → en revisión. Avisos `*` (cerrado) cuentan para todos los conceptos del lugar; `oferta_caducada` no cuenta aquí. Sin aportes, cuentan todos los avisos de los últimos 6 meses.
- **Nunca se guarda** (AD-3): avisos leídos con listeners como los aportes (4.9) y pasados al Dominio.
- **Ficha:** etiqueta "En revisión" (color de aviso + icono + texto, no solo color) junto al precio; al tocarla: "Varios clientes dicen que este precio ha cambiado. Si vas, confírmalo y ganarás monedas." Si es por "cerrado", la cabecera muestra "Puede que haya cerrado".
- **Pregunta de llegada** (`FlujoPregunta`): si algún concepto está en revisión, `QueUsaste` lo propone el primero con la etiqueta "En revisión" y `TeCosto` dice "¿Sigue costando X €?". Confirmar = aporte con el mismo valor (cierra la revisión); otro precio = aporte nuevo.
- **Cálculo** con pruebas EditMode (AD-14).
- El marcador del mapa no cambia (no está en DESIGN).

**Never:** sin ocultar el precio en revisión (se sigue mostrando, con la etiqueta).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| 2 avisos ×1 | Tras el último aporte | No en revisión |
| 3 avisos ×1 | Tras el último aporte | En revisión |
| 2 avisos ×1,5 | Fiables | En revisión (3) |
| Avisos viejos | 3 avisos antes del último aporte | No en revisión |
| Cerrado | 2 avisos `*` de Expertas (4) | Todos los conceptos en revisión; "Puede que haya cerrado" |
| Pregunta | Concepto en revisión | Primero en QueUsaste; "¿Sigue costando 40 €?" |
| Confirmar | Sí | Aporte nuevo; ya no en revisión |
| Sin aportes | 3 avisos en 6 meses | En revisión |
| Oferta caducada | 3 avisos `oferta_caducada` | No afecta a los precios |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/ReglaRevision.cs`.
- `Dominio/CalculadoraPrecio.cs` / `PrecioMostrado.cs` (`EnRevision`, `PosibleCierre`), `FuentesPrecio.cs` (avisos).
- `Aplicacion/IRepositorioAportes.cs` (o nuevo `IRepositorioAvisos`) con listeners; `ServicioLugares.cs`.
- `Dominio/FlujoPregunta.cs`; `UI/FichaLugar.cs`, `UI/HojaPregunta.cs`.
- Tests: `PruebasRevision.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaRevision` y pruebas de la matriz.
- [ ] Avisos como fuente y listeners.
- [ ] Etiqueta en la ficha y explicación.
- [ ] Orden y texto en la pregunta.
- [ ] Captura "revision".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "revision".
