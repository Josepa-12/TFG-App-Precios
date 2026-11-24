---
title: 'Historia 3.3 (parte 2): entrar con correo'
type: 'feature'
created: '2026-11-24'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-3-3-entrar-con-google-o-con-correo-o-seguir-sin-cuenta.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Quien no usa Google no puede crear cuenta (FR37).

**Approach:** «Con correo» en la hoja de cuenta: crear cuenta, entrar y contraseña olvidada con Firebase Auth, tras `ICuentas`.

## Boundaries & Constraints

**Always:**
- **Decidido (usuario, 24/11/2026): correo y contraseña de Firebase** en vez del enlace mágico (el plan gratuito envía 5 enlaces al día y el SDK de Unity no los trae). La contraseña la guarda Firebase: PriceMap no la guarda ni la registra. FR37 se matiza así en `prd.md` y `epics.md`.
- **Maqueta:** https://claude.ai/artifact/XLja29b3nU8htXJvbJsZLq.
- Crear cuenta (contraseña de 8 o más), entrar y «He olvidado la contraseña» (correo de Firebase en español; la respuesta no dice si el correo existe).
- Al entrar sigue con la acción pendiente, como con Google. Atrás vuelve a la hoja; el correo escrito se conserva, la contraseña no.
- Errores junto al campo y anunciados por TalkBack; el teclado no tapa el botón; 200 % sin cortes.

**Never:** verificar el correo; cambiar la contraseña dentro de la app; enlazar correo y Google a mano.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Crear | Correo y 8 caracteres | Sesión iniciada |
| Correo no válido | «ana@» | Error junto al campo, sin petición |
| Contraseña corta | 5 caracteres al crear | Error junto al campo, sin petición |
| Correo ya usado | Crear | «Ya hay una cuenta con ese correo» y «Entrar» |
| No coinciden | Entrar | «El correo o la contraseña no coinciden.» |
| Olvidada | Correo válido | Mismo mensaje exista o no |
| Demasiados intentos | Entrar | «Demasiados intentos. Espera un rato.» |
| Sin conexión | Cualquiera | «Necesitas conexión para entrar» y reintento |
| Doble toque | Petición en curso | Una sola petición |

</frozen-after-approval>

## Code Map

- `ICuentas` (`CrearConCorreo`, `EntrarConCorreo`, `EnviarContrasena`), `CuentasFirebase`/`IAccesoAuth` (códigos de `AuthError`; el de credenciales incorrectas es único por la protección de enumeración de Firebase; `LanguageCode = "es"`), `CuentasFalsas`.
- UI: `FormularioCuenta` (campos como el `TextField` de `HojaBuscador.cs:193`, `isPasswordField`, teclado de correo) dentro de `HojaCuenta`; validación pura del correo en Aplicacion.

## Tasks & Acceptance

**Execution:**
- [ ] Puerto, servicio y adaptador con pruebas.
- [ ] Formularios, errores, TalkBack y textos.
- [ ] Capturas `cuenta-correo(-oscuro)`, `cuenta-correo-200`; FR37.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con cuentas falsas.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode, `CompilarApk`, `CompilarPcPruebas` y captura de PC.

**Manual checks:**
- Crear, entrar y recuperar la contraseña en un móvil, con el proveedor «Correo electrónico/contraseña» activado en Firebase.
