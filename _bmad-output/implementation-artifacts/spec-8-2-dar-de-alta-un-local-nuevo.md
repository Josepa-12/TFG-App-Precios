---
title: 'Historia 8.2: dar de alta un local nuevo'
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

**Problem:** Un negocio que no está en los datos base no puede aparecer en el mapa (FR44, AD-11).

**Approach:** Formulario de alta para cuentas de negocio. Crea `lugares/alta:<autoId>` y `propiedad/alta:<autoId>` pendiente en un lote. `ServicioLugares` mezcla los dados de alta con los empaquetados; salen en el mapa con el logo de su tipo.

## Boundaries & Constraints

**Always:**
- **Formulario:**
  - Nombre: 3–60 caracteres.
  - Tipo: tipos existentes en `IconosLugar`, agrupados por plantilla (Servicio local, Hostelería, Ocio). Sin tipos nuevos.
  - Ubicación: buscador de dirección (2.3) o "Usar mi posición"; minimapa con chincheta arrastrable.
  - Dirección (texto, de Photon o escrita).
  - Correo del local (validación de formato).
  - Errores junto a cada campo; nada se borra al fallar (NFR-6 en espíritu).
- **Documento** `lugares/alta:<autoId>`: `{nombre, tipo, plantilla, lat, lon, direccion, email, dueno: uid, creado}`. Reglas: solo negocios, `dueno == uid`, campos exactos, `plantilla` coherente con `tipo` (lista en reglas), coordenadas dentro del área de Sanlúcar ampliada, máximo un alta por cuenta (`usuarios/<uid>.altas` incrementado en el lote, ≤ 1; ver pregunta 1).
- **Propiedad:** en el mismo lote, `propiedad/alta:<autoId>` pendiente (8.1). Sin contacto sembrado y con el correo escrito por el propio dueño no prueba nada: pendiente hasta aprobación manual.
- **Mapa:** `IRepositorioLugares` gana una fuente Firestore (listener de `lugares` con id `alta:*`) mezclada con los JSON; visibles para todos con el logo de su tipo y sin precio; en la ficha, "Pendiente de verificar" mientras la propiedad esté pendiente.
- **Sin conexión:** el alta se encola y ya se ve en el mapa del dueño.

**Never:** sin editar ni borrar lugares de los datos base, sin fotos.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Alta | Negocio, "Bar Pepe", bar, posición actual, correo | `lugares/alta:…` y propiedad pendiente; marcador en el mapa |
| Nombre corto | "Ba" | Error junto al campo |
| Correo mal | "pepe@" | Error junto al campo |
| Fuera de zona | Coordenadas de Madrid | "La ubicación tiene que estar en Sanlúcar" |
| Segundo alta | Misma cuenta | "Ya diste de alta un local" |
| Cliente | Cuenta de cliente | Sin acceso al alta |
| Ficha | Lugar de alta pendiente | "Pendiente de verificar" |
| Regla tipo-plantilla | `tipo: bar`, `plantilla: ocio` | Denegado |

</frozen-after-approval>

## Open Questions

1. **¿Cuántos locales por cuenta de negocio?** A) Uno (recomendado para el TFG: simple y evita abusos). B) Varios.

## Code Map

- Nuevos: `UI/PantallaAlta.cs` (+ uxml/uss), `Adaptadores/Firebase/LugaresFirestore.cs`.
- `Aplicacion/ServicioNegocio.cs` (alta), `IRepositorioLugares.cs`/`ServicioLugares.cs` (mezcla), `Adaptadores/Datos/RepositorioLugaresJson.cs`.
- `UI/PantallaPerfil.cs` (entrada "Dar de alta mi local" para negocios sin local).
- `firebase/firestore.rules` y pruebas. Tests: `PruebasAlta.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Validación del formulario (pura) y pruebas.
- [ ] Lote de alta y reglas.
- [ ] Mezcla de lugares de alta en repositorio y mapa.
- [ ] Pantalla de alta y etiqueta en la ficha.

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "alta".
