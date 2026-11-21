---
title: 'Historia 3.6: favoritos y progreso en la nube'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 3'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No se pueden guardar favoritos y los ajustes se pierden al cambiar de móvil (FR10, FR17, FR40).

**Approach:** Corazón en la fila de acciones de la ficha (lugar y punto libre). Favoritos en `usuarios/<uid>/favoritos/<id>`, visibles en la hoja del buscador. Ajustes sincronizados en `usuarios/<uid>.ajustes` por campo; cada servicio escribe solo sus campos (AD-18).

## Boundaries & Constraints

**Always:**
- **Favorito de lugar:** id = id del lugar; `{tipo: 'lugar', lugarId, creado}`.
- **Favorito de punto libre:** al pulsar se pide nombre ("Casa de mamá", obligatorio, máx. 40 caracteres); id `punto:<autoId>`; `{tipo: 'punto', nombre, lat, lon, direccion, creado}`.
- **Sin cuenta:** el corazón usa `ExigirCuenta` (3.3) y guarda al entrar.
- **Quitar:** tocar el corazón lleno, sin confirmación; toast "Quitado de favoritos" con "Deshacer" 3 s.
- **Hoja del buscador:** sección "Favoritos" antes de las categorías (icono, nombre, distancia); tocar abre la ficha; sin favoritos, no aparece.
- **Ajustes en la nube:** `ServicioCuentas` (dueño de `ajustes`, AD-18) sube cada cambio con `Update("ajustes.<clave>", valor)`; al entrar en otro móvil, la nube manda sobre lo local.
- **Observación:** `ServicioLugares` expone `Favoritos` y `FavoritosCambiados` desde un listener de Firestore (funciona sin conexión gracias a la persistencia).
- **Reglas:** `usuarios/{uid}/favoritos/{id}`: solo el dueño; campos permitidos según `tipo`; `nombre` ≤ 40.
- **Sin conexión:** añadir y quitar funcionan y se sincronizan después (cola de Firestore).
- TalkBack: "Guardar en favoritos, botón" / "Quitar de favoritos, botón, seleccionado".

**Never:** sin carpetas de favoritos, sin compartirlos, sin corazón en los marcadores del mapa (no está en DESIGN).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Guardar lugar | Corazón en Clínica Areas con cuenta | `favoritos/clinica-areas`; aparece en la hoja |
| Guardar punto | Corazón en punto libre + "Casa de mamá" | `favoritos/punto:…` con nombre y coordenadas |
| Nombre vacío | "" | No deja guardar |
| Sin cuenta | Corazón | Hoja de cuenta; al entrar se guarda |
| Quitar | Corazón lleno | Se quita; toast con Deshacer |
| Deshacer | Toque en Deshacer | Vuelve a guardarse |
| Otro móvil | Entrar con la misma cuenta | Favoritos y ajustes recuperados |
| Sin conexión | Guardar | Visible ya; se sube al reconectar |
| Regla campo extra | Favorito con `foo` | Denegado (emulador) |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/Favorito.cs`, `Aplicacion/IFavoritos.cs`, `Adaptadores/Firebase/FavoritosFirestore.cs`, `UI/BotonFavorito.cs`, `UI/DialogoNombre.cs`.
- `Aplicacion/ServicioLugares.cs` (favoritos), `ServicioCuentas.cs` / `ServicioAjustes.cs` (ajustes en la nube).
- `UI/FilaAcciones.cs` (2.6), `UI/HojaBuscador.cs`, `UI/Toast.cs` (acción Deshacer).
- `firebase/firestore.rules` y pruebas. Tests: `PruebasFavoritos.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Modelo, puerto, adaptador y servicio de favoritos.
- [ ] Corazón en la ficha, diálogo de nombre y Deshacer.
- [ ] Sección de favoritos en la hoja.
- [ ] Ajustes en la nube por campo.
- [ ] Reglas y pruebas.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "favoritos".

**Manual checks:**
- Dos móviles (o desinstalar y reinstalar) con la misma cuenta.
