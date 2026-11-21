---
title: 'Historia 6.3: exportar los datos de la evaluación'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 6'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-2-registro-anonimo-para-la-evaluacion.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los eventos de `evaluacion/` no se leen desde la app; el autor necesita sacarlos para las métricas de la memoria (FR59).

**Approach:** Script Node del administrador en `firebase/exportar/` con Admin SDK y clave de cuenta de servicio (la descarga el usuario; no se versiona). Lee `evaluacion/*/eventos` y genera un CSV por participante y un resumen con SM-2, SM-3 y SM-4. Cálculo en módulo puro con pruebas.

## Boundaries & Constraints

**Always:**
- **Uso:** `node exportar.js --clave <ruta.json> --salida <carpeta> [--desde AAAA-MM-DD] [--hasta AAAA-MM-DD]`. También contra el emulador (`FIRESTORE_EMULATOR_HOST`).
- **CSV por participante:** `participante-<id8>.csv` con `fecha_iso,nombre,plantilla,frescura,tipoAporte,cosmeticoId` (UTF-8 con BOM para Excel, separador `;`).
- **Resumen** `resumen.csv` y `resumen.md`:
  - SM-2 tasa de aporte = `aporte_creado` / `visita_verificada` (global y por participante).
  - SM-3 uso de la tienda = participantes con ≥ 1 `compra_tienda` / participantes, y compras por participante.
  - SM-4 proporción "al día" = `confirmacion` + `aporte_creado` con `frescura: al-dia` / total con frescura.
  - Nº de participantes, eventos por tipo y rango de fechas.
- **Módulo puro** `metricas.js` (entrada: lista de eventos; salida: métricas) con pruebas `node --test`.
- **Seguridad:** la clave nunca se sube (`.gitignore`: `firebase/exportar/*.json` salvo `package.json`); el LEEME explica cómo descargarla.
- Sin conexión a nada más que Firestore.

**Never:** sin panel web, sin exportar datos de usuarios (`usuarios/`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Dos participantes | A: 4 visitas, 3 aportes; B: 2 visitas, 0 aportes | SM-2 global 3/6 = 50 %; A 75 %, B 0 % |
| Tienda | A compra 1; B nada | SM-3 = 50 % |
| Frescura | 6 confirmaciones al día de 8 | SM-4 = 75 % |
| Sin visitas | Participante solo con compras | SM-2 "n/d" para él |
| Filtro fechas | `--desde` | Solo eventos desde ese día |
| Sin clave | Sin `--clave` ni emulador | Error claro y salida 1 |

</frozen-after-approval>

## Code Map

- Nuevos: `firebase/exportar/package.json`, `exportar.js`, `metricas.js`, `metricas.test.js`, `LEEME.md`.
- `.gitignore`.
- `docs/memoria-tfg.md`: sección de método de evaluación con cómo se calculan las métricas.

## Tasks & Acceptance

**Execution:**
- [ ] `metricas.js` y pruebas de la matriz.
- [ ] `exportar.js` con Admin SDK y emulador.
- [ ] CSV y resumen; LEEME y `.gitignore`.

**Acceptance Criteria:**
- Dada `node --test`, cuando se ejecuta, entonces cada fila de la matriz pasa.
- Dado el emulador con eventos sembrados, cuando se exporta, entonces se generan los CSV y el resumen.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `cd firebase/exportar && npm test`.
