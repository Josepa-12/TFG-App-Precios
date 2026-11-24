---
title: 'Historia 3.3 (parte 2): entrar con correo'
type: 'feature'
created: '2026-11-24'
status: 'done'
baseline_commit: 'e5ac7221b52c84ef271e044a0c5b5920f6d18e47'
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
- **Maqueta:** https://claude.ai/artifact/XLja29b3nU8htXJvbJsZLq (aprobada; copia en `docs/diseno/cuenta/maqueta.html`): «Con correo» (secundario) bajo Google, en la hoja y en la pantalla completa; los formularios, a pantalla completa.
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
| Correo ya usado | Crear | «Ya hay una cuenta con ese correo. Entra o, si la creaste con Google, usa Google.» y «Entrar» |
| No coinciden | Entrar | «El correo o la contraseña no coinciden.» |
| Olvidada | Correo válido | Mismo mensaje exista o no |
| Demasiados intentos | Entrar | «Demasiados intentos. Espera un rato.» |
| Sin conexión | Cualquiera | «Necesitas conexión para entrar» y reintento |
| Doble toque | Petición en curso | Una sola petición |
| Atrás o «Ahora no» | Petición en curso | Cancelada y sin sesión |
| Servidor no disponible | Sin Firebase | Mensaje; atrás funciona |
| Cambiar de Crear a Entrar | Correo escrito | El correo se conserva; la contraseña se vacía |

</frozen-after-approval>

## Code Map

- `ICuentas`: `CrearConCorreo`, `EntrarConCorreo`, `EnviarContrasena` (con `CancellationToken`). `ErroresCuenta` nuevos: correo usado, no coinciden, correo no válido, contraseña débil, demasiados intentos. `ServicioCuentas`: pasos de la entrada (botones, crear, entrar, olvidada), misma petición única, espera al servidor y `Cerrar` que Google; validación pura (correo con «@» y punto en el dominio, sin espacios a los lados; 8 caracteres al crear). La contraseña no se guarda en ningún campo del servicio.
- `CuentasFirebase`: las tres pasan por `EntrarEnFirebase` (de una en una; una cancelada hace `Salir` y no se publica). `IAccesoAuth`/`AccesoAuth`: `CreateUserWithEmailAndPasswordAsync`, `SignInWithEmailAndPasswordAsync`, `SendPasswordResetEmailAsync`, `LanguageCode = "es"`. Códigos (`AuthError`): `WrongPassword`, `UserNotFound` e `InvalidCredential` → no coinciden; `EmailAlreadyInUse`; `InvalidEmail`/`MissingEmail`; `WeakPassword`; `TooManyRequests`/`QuotaExceeded`. Al enviar la contraseña, `UserNotFound` cuenta como enviado. Crear cancelada: la cuenta ya existe en Firebase; se sale sin sesión. `CuentasFalsas` con las tres.
- UI: `FormularioCuenta` dentro de `HojaCuenta` (hoy `cuenta__*` en `PantallaMapa.uss` l. 2620; `HojaCuenta.Controles`, `PantallaMapa.ActualizarCuenta`/`Atras`). Campos como el `TextField` de `HojaBuscador.cs:193`; contraseña con `isPasswordField` y «Ver»/«Ocultar» (48 dp); teclado de correo. Intro en el último campo envía. Con un campo enfocado, los botones van justo debajo de los campos y el contenido se desplaza, para que el teclado no los tape. Al salir del formulario se vacía el campo de la contraseña.
- Los textos nuevos, en `Textos_es-ES.asset`; FR-37 en `prd.md` (l. 258) y `epics.md` (l. 58 y 679). Memoria, CLAUDE.md y `sprint-status` los cierra quien revisa.

## Tasks & Acceptance

**Execution:**
- [x] Puerto, servicio y adaptador con pruebas.
- [x] Formularios, errores, TalkBack y textos.
- [x] Capturas `cuenta-correo(-oscuro)`, `cuenta-correo-200`; FR37.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con cuentas falsas.

## Spec Change Log

## Review Triage Log

| Capa | Hallazgo | Veredicto | Prueba | Destino |
|---|---|---|---|---|
| blind | El error queda fuera de la vista al cerrarse el teclado | medium | Se saltaba el desplazamiento y luego se ponía a cero | patch |
| blind | Cancelar el enlace llama a Salir | medium | No abre sesión y podía cerrar la guardada | patch |
| blind | Crear y Entrar se podían cambiar sin que fallara nada | medium | El doble no anotaba la operación | patch |
| blind | El error no se quita al corregir el campo | low | Se actualiza al volver a enviar; quitarlo antes pide estado nuevo | reject |
| blind | Mensajes de «iniciar sesión» al crear y al pedir el enlace | medium | Cierto, y la contraseña vacía decía que no coincide | patch |
| blind | Aviso de privacidad sin la contraseña | medium | Frase nueva | patch |
| blind | 8.1 pide correo verificado | medium | Las cuentas de correo no se verifican; va con la 8.1 | defer |
| blind | epics y epic-3-context a medias | low | Corregidos | patch |
| blind | El campo con error pierde el foco | medium | Misma especificidad | patch |
| blind | Correo usado con Google, sin atajo | low | El mensaje lo dice y atrás lleva a Google | reject |
| blind | Alto del teclado supuesto | maybe-false | Hay que verlo en el móvil (medium sin verificar) | defer |
| blind | Intro aplazado y Blur antes de limpiar | medium | Podía enviar en otro paso | patch |
| blind | Campos como botón en TalkBack | medium | Igual que el buscador; a ver en el móvil | defer |
| blind | LEEME incompleto | low | Completado | patch |
| blind | Un error local a la vez | low | Poco frecuente | reject |
| blind | Reescalado cuadrático | low | Menos de veinte textos | reject |
| edge | dedoAbajo se queda puesto | high | El botón o la página capturan el puntero: el hueco del teclado no se iba | patch |
| edge | Done no envía tras arrastrar | high | Misma causa | patch |
| edge | Error fuera de la vista | medium | Igual que blind | patch |
| edge | Cancelar el enlace | medium | Igual que blind | patch |
| edge | UserDisabled | low | Solo si el administrador la desactiva | reject |
| gap | Crear y Entrar | medium | `CuentasFalsas.Ultima` | patch |
| gap | Errores de Google | medium | Prueba añadida | patch |
| gap | Teclado por el foco | medium | Pruebas con panel; lo nativo, en el móvil | patch + defer |
| gap | Intro sin Intro | medium | `KeyDownEvent` de verdad | patch |
| gap | TalkBack desplaza | medium | Prueba añadida | patch |
| gap | Valor del correo | medium | Sin refresco a mano | patch |
| gap | Destruir vacía la contraseña | medium | Prueba añadida | patch |
| gap | Llamadas reales a Firebase | medium | En el móvil | defer |
| gap | Cancelar el enlace | medium | Igual que blind | patch |
| gap | Mensajes | medium | Igual que blind | patch |

## Implementation Notes

- Verificado (24/11/2026): 1787/1787 pruebas, APK, PC y captura sin errores. Sin probar en un móvil (teclado de Android y Firebase de verdad).
- Los formularios no llevan «Ahora no», como los dibujos de la maqueta: atrás vuelve a la hoja.

## Verification

**Commands:**
- Pruebas EditMode, `CompilarApk`, `CompilarPcPruebas` y captura de PC.

**Manual checks:**
- Crear, entrar y recuperar la contraseña en un móvil, con el proveedor «Correo electrónico/contraseña» activado en Firebase.
