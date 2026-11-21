---
title: 'Historia 3.3: entrar con Google o con correo, o seguir sin cuenta'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 3'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-3-2-mis-datos-en-firebase-protegidos-y-disponibles-sin-conexion.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No se puede iniciar sesión, y las acciones que necesitan cuenta no pueden pedirla (FR37, FR38, UX-DR14).

**Approach:** Puerto `ICuentas` (adaptador Firebase Auth) y `ServicioCuentas`. "Hoja de cuenta" con "Continuar con Google", "Con correo" (enlace mágico) y "Ahora no". `ServicioCuentas.ExigirCuenta(accion)` abre la hoja y repite la acción al entrar. Toast reutilizable (UX-DR15).

## Boundaries & Constraints

**Always:**
- **Google:** Credential Manager (`GetGoogleIdOption`) por un plugin Java pequeño que devuelve el `idToken`; luego `GoogleAuthProvider.GetCredential(idToken, null)` y `SignInWithCredentialAsync`. `webClientId` de `google-services.json` (`oauth_client` tipo 3).
- **Correo sin contraseña:** pide el correo y envía enlace (`SendSignInLinkToEmail`, `ActionCodeSettings` con `handleCodeInApp = true`, dominio gratuito de Firebase Hosting `<proyecto>.firebaseapp.com`). El enlace abre la app (intent-filter del dominio) y completa `SignInWithEmailLinkAsync`. Correo pendiente guardado en `IPreferencias`. La app nunca gestiona contraseñas.
- **Sin cuenta:** "Ahora no" cierra la hoja; mapa, fichas, búsqueda y navegación funcionan (FR38).
- **`ExigirCuenta(Action alEntrar)`:** con sesión ejecuta ya; sin sesión abre la hoja y ejecuta al entrar; si se cancela, nada. Una pendiente a la vez.
- **Estado observable:** `ServicioCuentas.Usuario` (uid, nombre, correo, foto) y evento `SesionCambiada`; la UI no ve Firebase.
- **Errores:** sin conexión; cancelado; enlace caducado ("El enlace ha caducado. Te enviamos otro." con botón); genérico "No se pudo iniciar sesión. Inténtalo de nuevo." con reintento. La app sigue usable sin cuenta (UX-DR16).
- **Toast** (`UI/Toast.cs`): 3 s, abajo sobre la cápsula, región en vivo para TalkBack; nunca única vía de información imprescindible (UX-DR15).
- **Hoja de cuenta:** estética de hoja; foco al título; atrás = "Ahora no".
- PC/editor: `CuentasFalsas` para capturas y pruebas.

**Never:** sin contraseñas, sin Facebook/Apple, sin elegir tipo de cuenta (3.4).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Google | Elige cuenta | Sesión iniciada; toast "Hola, Ana" |
| Google cancelado | Cierra el selector | Hoja sigue abierta, sin error |
| Correo | Escribe correo válido | "Te hemos enviado un enlace a a***@gmail.com" |
| Correo inválido | "ana@" | Error junto al campo |
| Abrir enlace | Enlace válido | Sesión iniciada; acción pendiente ejecutada |
| Enlace caducado | Enlace viejo | Mensaje y botón "Enviar otro" |
| Ahora no | Toque | Hoja cerrada; acción pendiente descartada |
| Exigir con sesión | `ExigirCuenta` con usuario | Ejecuta directamente |
| Sin conexión | Google | "Necesitas conexión para entrar" y reintento |

</frozen-after-approval>

## Open Questions

1. **Correo: enlace o contraseña** (FR37: "sin contraseñas propias"). A) Enlace mágico (recomendado). B) Correo y contraseña de Firebase (más sencillo, pero contradice FR37).
2. **Tamaño.** A) Partir (recomendado): parte 1 = puerto, servicio, Google, hoja, toast y `ExigirCuenta`; parte 2 = correo con enlace y errores. B) Una sola.

## Code Map

- Nuevos: `Aplicacion/ICuentas.cs`, `ServicioCuentas.cs`, `UsuarioCuenta.cs`; `Adaptadores/Firebase/CuentasFirebase.cs`; `Plugins/Android/InicioGoogle.java`; `UI/HojaCuenta.cs`, `UI/Toast.cs`.
- Manifiesto: intent-filter del dominio de enlaces (`Editor/PermisosUbicacionAndroid.cs` o post-proceso nuevo).
- `Arranque/Arranque.cs`, `CapturaPruebas.cs` (captura "cuenta"), textos.
- Tests: `PruebasCuentas.cs` con `ICuentas` falsa.

## Tasks & Acceptance

**Execution:**
- [ ] Puerto, servicio y `ExigirCuenta` con pruebas.
- [ ] Adaptador Firebase + plugin de Google.
- [ ] Correo con enlace y manejo del deep link.
- [ ] Hoja de cuenta, toast, errores y TalkBack.
- [ ] Captura "cuenta".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con cuentas falsas.
- Dado un móvil con la app configurada, cuando entro con Google, entonces la sesión se mantiene al reabrir (manual).

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "cuenta"; `CompilarApk`.

**Manual checks:**
- Google y correo en un móvil real con el proyecto de Firebase del usuario.
