---
title: 'Historia 4.4: confirmar lo que pagué'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-1-reglas-que-protegen-los-aportes-y-el-anonimato.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-3-la-pregunta-llega-una-hora-despues-o-la-retomo-con-el-boton.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La hoja de pregunta (4.3) está vacía. El cliente debe poder decir en pocos toques si fue y si el precio era el de la app (FR24, FR30, FR60).

**Approach:** Máquina de pasos pura (`FlujoPregunta`, Dominio): "¿Has ido?" → "¿Qué usaste?" (solo con varios conceptos o variantes) → "¿Te costó X €?". "Sí" crea la confirmación en un lote atómico (aporte público, `aporteHecho`, copia privada) vía `ServicioAportes` y el puerto `IAportes`. Termina con agradecimiento (la recompensa es la 4.11).

## Boundaries & Constraints

**Always:**
- **Pasos** (`PasoPregunta`): `HasIdo`, `QueUsaste`, `TeCosto`, `Gracias`. Las ramas de 4.5–4.8 (otro precio, tramos, pionero, valoración) serán pasos nuevos de la misma máquina.
  - "¿Has ido?": Sí / No. "No" descarta la visita (`estado: descartada`) y cierra con "Gracias por decírnoslo".
  - "¿Qué usaste?": conceptos del lugar con su precio; los que tienen variantes se despliegan ("Sesión · con bono"). Un solo concepto sin variantes → se salta.
  - "¿Te costó X €?": X = precio mostrado (fijo: "90 €"; "desde 40 €" → "¿Te costó 40 €?"). "Sí" confirma; "No, fueron…" → paso `OtroPrecio` (4.5; pregunta 1).
- **Confirmación = aporte** con el valor mostrado (`importeCent`, forma del concepto), en un lote: `lugares/<id>/aportes/<aid>`, `usuarios/<uid>/aportes/<aid>` (+ `visitaId`) y `visitas/<vid>.aporteHecho = true`, `estado: respondida` (reglas 4.1).
- **"Atrás"** vuelve al paso anterior; en el primero = "Más tarde". **"Más tarde"** deja la visita en pendientes. Atrás de Android = "Atrás".
- **Eventos:** `PreguntaRespondida`, `PreguntaAplazada`, `AporteCreado(tipo: confirmacion)` (para 4.11 y 6.2).
- **TalkBack:** título de cada paso como primer foco; botones con nombre; lista de conceptos con "seleccionado".
- **Errores:** lote rechazado por reglas (visita caducada) → "Esta visita ya no admite respuestas" y se descarta; sin conexión se encola (lo completa la 4.9).

**Never:** sin regla de precio (4.5), tramos (4.6), pionero (4.7), valoración (4.8) ni monedas (4.11).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Un concepto | Lugar con 1 concepto | HasIdo → TeCosto |
| Varios conceptos | Clínica Areas | HasIdo → QueUsaste → TeCosto |
| Variantes | Concepto con "con bono" | Variantes listadas en QueUsaste |
| No fui | "No" en HasIdo | Visita descartada; "Gracias por decírnoslo" |
| Confirmar | "Sí" en TeCosto (40 €) | Lote con `importeCent: 4000`; Gracias |
| Atrás | En TeCosto | Vuelve a QueUsaste |
| Atrás primer paso | En HasIdo | Pendiente |
| Más tarde | Cualquier paso | Pendiente; hoja cerrada |
| Visita caducada | Lote rechazado | Mensaje y visita descartada |

</frozen-after-approval>

## Open Questions

1. **"No, fueron…" antes de la 4.5.** A) Hacer 4.4 y 4.5 seguidas sin publicar entre medias, así el botón ya lleva a la regla (recomendado). B) Ocultar el botón hasta la 4.5.

## Code Map

- Nuevos: `Dominio/FlujoPregunta.cs`, `PasoPregunta.cs`, `Aporte.cs`; `Aplicacion/IAportes.cs`, `ServicioAportes.cs`; `Adaptadores/Firebase/AportesFirestore.cs` (lote `WriteBatch`); `UI/HojaPregunta.cs` (completa) y una clase por paso si conviene.
- `Aplicacion/ServicioVisitas.cs` (respondida/descartada/pendiente).
- Tests: `PruebasFlujoPregunta.cs`, `PruebasAportes.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `FlujoPregunta` y pruebas de cada secuencia.
- [ ] `IAportes` con el lote atómico y adaptador.
- [ ] Hoja con los pasos, Atrás, Más tarde, TalkBack.
- [ ] Eventos para recompensa y evaluación.
- [ ] Captura "pregunta".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.
- Dado el emulador, cuando se ejecuta el lote real del adaptador (prueba de reglas con los mismos campos), entonces se acepta.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "pregunta".
