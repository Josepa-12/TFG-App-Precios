---
title: 'Historia 8.4: publicar ofertas'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 8'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-7-1-avisar-de-que-un-precio-esta-mal.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los negocios no pueden anunciar ofertas, y el marcador no tiene la etiqueta "%" (FR46, FR3, UX-DR7, NFR-6).

**Approach:** El dueño verificado publica ofertas (texto, precio opcional, fecha de fin) en `lugares/<id>/ofertas`. El Dominio decide la vigencia (no caducada y < 3 avisos `oferta_caducada` posteriores). Bloque "Ofertas" en la ficha y etiqueta "%" en la esquina del marcador.

## Boundaries & Constraints

**Always:**
- **Oferta** `ofertas/{autoId}`: `{texto (5–120), precioCent? (> 0), fin (fecha, ≥ hoy, ≤ hoy + 90 días), creado}`. Reglas: dueño verificado; `precioCent` entero > 0 si existe (negativo o 0 rechazado: NFR-6); inmutable salvo borrar por el dueño.
- **Vigencia** (`ReglaOferta`, Dominio, pura): vigente si `ahora < fin` (fin del día local) y los avisos `oferta_caducada` con `conceptoId` = `oferta:<ofertaId>` creados después de la oferta son < 3 (cuenta de avisos, no peso; FR46 habla de 3 avisos).
- **Avisar caducada:** cada oferta en la ficha tiene "¿Ya no está vigente?", que crea un aviso de la 7.1 con `tipo: oferta_caducada` y `conceptoId: oferta:<id>` (peso como en la 7.1, aunque aquí se cuentan avisos).
- **Ficha:** bloque "Ofertas" bajo la fila de acciones: texto, precio ("Por 9,90 €") y "Hasta el 30/11".
- **Marcador:** etiqueta "%" de 16 dp en la esquina superior derecha (color de acento con contraste; UX-DR7) si hay alguna oferta vigente. TalkBack del marcador añade "con oferta".
- **Panel:** publicar y borrar ofertas propias (lista en el panel de la 8.5; el formulario se añade aquí).

**Never:** sin ofertas de clientes, sin notificaciones de ofertas.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Publicar | "2x1 en cañas", sin precio, fin 30/11 | En la ficha y "%" en el marcador |
| Con precio | 9,90 € | "Por 9,90 €" |
| Precio negativo | -5 | Error junto al campo; regla denegada |
| Fin pasado | Fin ayer | Error |
| Caduca | `ahora` > fin | Desaparece de ficha y marcador |
| 3 avisos | 3 `oferta_caducada` | Desaparece |
| 2 avisos | 2 | Sigue |
| Avisar | "¿Ya no está vigente?" | Aviso creado; toast |
| Otro negocio | Publicar en local ajeno | Denegado |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/Oferta.cs`, `Dominio/ReglaOferta.cs`, `Adaptadores/Firebase/OfertasFirestore.cs`, `UI/BloqueOfertas.cs`, `UI/FormularioOferta.cs`.
- `Dominio/ResumenLugar.cs`/`ReglaResumen.cs` (tiene oferta), `UI/MarcadorLugar.cs` (etiqueta), `UI/AccesibilidadPantalla.cs`.
- `Aplicacion/ServicioNegocio.cs`, `ServicioLugares.cs` (listener de ofertas), `ServicioAvisos.cs` (7.1).
- `firebase/firestore.rules` y pruebas. Tests: `PruebasOfertas.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaOferta` y pruebas.
- [ ] Adaptador, listener y resumen con oferta.
- [ ] Reglas y pruebas.
- [ ] Bloque en la ficha, etiqueta "%" y formulario; captura "oferta".

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abre `oferta.png`, entonces se ve la etiqueta "%" en el marcador.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "oferta".
