---
title: 'Historia 8.3: declarar mis precios y variantes'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 8'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-8-1-reclamar-y-verificar-mi-local.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El dueño verificado no puede publicar sus precios (FR45, FR60, NFR-6).

**Approach:** Editor de precios en el panel del negocio: conceptos (agrupados por área) con forma (fijo, desde, rango por persona, "a consultar"), importe, unidad y variantes (horario, día, tipo de cliente). Un documento por concepto en `lugares/<id>/declaraciones/<conceptoId>`. El Dominio ya trata `Declaracion` como fuente (1.3): "Declarado por el negocio" hasta que manden los clientes (4.10).

## Boundaries & Constraints

**Always:**
- **Documento** `declaraciones/{conceptoId}`: `{area, nombre, unidad, forma, importeCent | minCent+maxCent | consultar: true, variantes: [{id, condicion: {tipo, desde, hasta, dias, tipoCliente}, forma, importeCent…}], actualizado}`.
  - `conceptoId` = slug del nombre (AD-11); renombrar crea uno nuevo y borra la declaración vieja (deja de mostrarse; los aportes viejos siguen con su id).
- **Validación** (`ValidarDeclaracion`, Dominio, pura): importes > 0 (NFR-6), `maxCent > minCent`, nombre 3–60, condición coherente (`desde` < `hasta` en horas; días no vacíos). Errores junto a su campo sin borrar lo demás.
- **Condiciones** (FR60): reutilizan `CondicionVariante`/`TipoCondicion` (1.3): franja horaria, días de la semana, tipo de cliente (texto corto, p. ej. "Jubilados").
- **Reglas:** escribir en `declaraciones` solo si `propiedad/<lugarId>.uid == request.auth.uid` y `estado == 'verificada'`; campos y tipos validados (importes enteros > 0); `actualizado == request.time`.
- **Ficha:** declaraciones leídas con listener, entran en `FuentesPrecio`; origen "Declarado por el negocio · 18/11/2026"; si también hay precios web, manda lo declarado (decisión de la 1.3, comprobar `CalculadoraPrecio`).
- **Editor:** añadir, editar, borrar concepto (con confirmación); añadir variantes dentro del concepto; guardar por concepto. TalkBack en todos los campos.

**Never:** sin importar precios desde la web automáticamente, sin editar locales no verificados.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Declarar fijo | "Corte de pelo", 12 € | Documento `corte-de-pelo`; ficha "12 € · Declarado por el negocio" |
| Rango | Menú 15–25 € por persona | Rango en la ficha |
| Variante | "Con luz" 20–24 h, 24 € | Variante bajo el concepto, destacada en su horario |
| Precio 0 | 0 € | Error junto al campo; lo demás intacto |
| Rango invertido | 25–15 | Error |
| Horas mal | Desde 22 hasta 20 | Error |
| No verificado | Propiedad pendiente | Editor bloqueado (8.5 explica); regla denegada |
| Otro negocio | Declarar en local ajeno | Denegado |
| Mandan clientes | 3 aportes recientes | La ficha muestra "Según 3 clientes" |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/ValidarDeclaracion.cs`, `UI/EditorPrecios.cs`, `UI/EditorConcepto.cs`, `UI/EditorVariante.cs`, `Adaptadores/Firebase/DeclaracionesFirestore.cs`.
- `Dominio/Declaracion.cs`, `CondicionVariante.cs`, `CalculadoraPrecio.cs` (prioridad web/declarado), `Aplicacion/ServicioNegocio.cs`, `ServicioLugares.cs` (listener).
- `firebase/firestore.rules` y pruebas. Tests: `PruebasDeclaraciones.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Validación y pruebas.
- [ ] Adaptador, listener y mezcla en `FuentesPrecio`.
- [ ] Reglas y pruebas.
- [ ] Editor con conceptos y variantes; captura "editor".

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "editor".
