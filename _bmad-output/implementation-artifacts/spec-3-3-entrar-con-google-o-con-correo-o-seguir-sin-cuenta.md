---
title: 'Historia 3.3 (parte 1): entrar con Google o seguir sin cuenta'
type: 'feature'
created: '2026-11-24'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** No se puede iniciar sesión, y las acciones que necesitan cuenta no pueden pedirla (FR37, FR38, UX-DR14).

**Approach:** Puerto `ICuentas` (adaptador Firebase Auth) y `ServicioCuentas`. Hoja de cuenta con «Continuar con Google» y «Ahora no», y la misma entrada a pantalla completa una vez tras la bienvenida. `ServicioCuentas.ExigirCuenta(accion)` abre la hoja y sigue con la acción al entrar.

## Boundaries & Constraints

**Always:**
- **Decidido (usuario, 24/11/2026):** la 3.3 va en dos partes; el correo es la 2 (`spec-3-3b`). Hasta entonces no hay botón «Con correo».
- **Decidido (usuario, 24/11/2026): dónde se abre.** El avatar de la cápsula sin sesión abre la hoja; con sesión muestra la inicial y, al tocarlo, el toast «Has entrado como …» (el perfil es la 3.5). Tras terminar la bienvenida sale una vez la pantalla completa «Crea tu cuenta»; después, nunca más sola.
- **Maqueta:** https://claude.ai/artifact/XLja29b3nU8htXJvbJsZLq (aprobada; copia local en `docs/diseno/cuenta/maqueta.html`).
- **Decidido (usuario, 24/11/2026):** el botón de Google es el oficial blanco en los dos temas (fondo #FFFFFF, borde #747775, texto #1F1F1F, «G» en sus cuatro colores), fuera de la paleta.
- **Google:** Credential Manager (`GetSignInWithGoogleOption`, `default_web_client_id`) en un plugin Java que devuelve el `idToken`; luego `GoogleAuthProvider.GetCredential` y `SignInWithCredentialAsync`.
- **Sin cuenta:** «Ahora no» y atrás cierran; mapa, fichas, búsqueda y navegación funcionan (FR38).
- **`ExigirCuenta(Action alEntrar)`:** con sesión ejecuta ya; sin sesión abre la hoja y ejecuta al entrar; si se cancela, nada. Una pendiente a la vez.
- **Estado observable:** `ServicioCuentas.Usuario` (uid, nombre, correo) y `SesionCambiada`; la UI no ve Firebase. La sesión sigue al reabrir.
- **Errores:** sin conexión, cancelado (sin mensaje), servidor no disponible y genérico, con reintento; la app sigue usable sin cuenta (UX-DR16).
- **Privacidad:** correo, nombre y token nunca van al registro. El aviso de la bienvenida dice que Google (Firebase) guarda la cuenta si se crea.
- **TalkBack y 200 %:** modal; foco al título y de vuelta al origen; errores anunciados.
- PC, editor y captura: `CuentasFalsas`.

**Never:** correo (parte 2); Facebook ni Apple; elegir tipo de cuenta (3.4); perfil, cerrar sesión y borrar cuenta (3.5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Google | Elige cuenta | Sesión; toast «Hola, Ana» |
| Google cancelado | Cierra el selector | La hoja sigue, sin error |
| Ahora no | Toque o atrás | Cerrada; acción pendiente descartada |
| Exigir con sesión | `ExigirCuenta` | Ejecuta directamente |
| Exigir sin sesión | Entra | Cerrada y acción ejecutada una vez |
| Sin conexión | Google | «Necesitas conexión para entrar» y reintento |
| Otro fallo | Google | «No se pudo iniciar sesión. Inténtalo de nuevo.» |
| Doble toque | Petición en curso | Una sola petición |
| Tras la bienvenida | Se termina | Pantalla completa una vez; al reabrir, no |
| Con sesión ya | Se termina la bienvenida | No sale |
| Servidor no disponible | Sin Firebase | Mensaje; «Ahora no» funciona |
| Sin nombre | Cuenta sin nombre | Inicial y toast con el correo |

</frozen-after-approval>

## Code Map

- **Aplicacion (nuevos):** `ICuentas` (`Usuario`, `Cambiado`, `EntrarConGoogle` → `Resultado<T>`), `UsuarioCuenta`, `ErroresCuenta`, `ServicioCuentas` (espera a `IServidor` Listo; preferencia `cuenta.ofrecida`). Patrón de `ServicioBienvenida`.
- **Adaptadores/Firebase/`CuentasFirebase`** sobre `IAccesoAuth`, como `IAccesoFirestore`/`AccesoFirestore` (pruebas con uno falso). Vuelta al hilo principal como `AlmacenFirestore`. `CuentasFalsas` junto a `ServidorNulo`.
- **Java:** `Plugins/Android/PuenteCuenta.java` (`com.pricemap.cuenta`, patrón `PuenteRed`: pedir, estado y cancelar por sondeo; ventana en el hilo principal). Dependencias por EDM4U en un `*Dependencies.xml` de `Assets/Editor`: `androidx.credentials:credentials` y `credentials-play-services-auth`, `com.google.android.libraries.identity.googleid`. `default_web_client_id` está en `FirebaseApp.androidlib/res/values/google-services.xml`; sin él, no disponible. Ojo: `ActividadPrincipal` pasa a pausa con el selector abierto.
- **UI:** `HojaCuenta` (botones `explicacion-ubicacion__boton`; pantalla completa con el logo de `PantallaBienvenida`). En `PantallaMapa`: hueco como `hueco-bienvenida` (l. 343), modal como `ModalUbicacionAbierta` (l. 506), atrás (l. 3186), tras `AlTerminarBienvenida` (l. 399), `MostrarToast` (l. 2155; hoy solo se coloca bajo la tarjeta de ruta). Avatar: `HojaBuscador.Avatar` (l. 207, hoy no hace nada). Lo compartido (2.5c) espera también a que se cierre.
- `Arranque/Arranque.cs` (l. 58 y `CrearServidor`), `CapturaPruebas.cs` (`Bienvenida()`, l. 304), textos en `Textos_es-ES.asset`, `docs/diseno/bienvenida/textos.md`.
- Pruebas: `PruebasCuentas.cs`; manifiesto y Java leído como en `PruebasVoz`.

## Tasks & Acceptance

**Execution:**
- [ ] `ICuentas`, `ServicioCuentas` y `ExigirCuenta` con pruebas.
- [ ] `CuentasFirebase`, `PuenteCuenta.java` y dependencias.
- [ ] Hoja, pantalla tras la bienvenida, avatar y toast.
- [ ] TalkBack, 200 %, textos y capturas `cuenta(-oscuro)`, `cuenta-bienvenida(-oscuro)`, `cuenta-200`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con cuentas falsas.
- Dada la APK, cuando se compila, entonces lleva el plugin y no pide permisos nuevos.
- Dado un móvil con la app configurada, cuando entro con Google, entonces la sesión sigue al reabrir (manual).

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode, `CompilarApk`, `CompilarPcPruebas` y captura de PC.

**Manual checks:**
- Google en un móvil, con el proveedor activado en la consola de Firebase. Si sale «Requests from this Android client application are blocked», quitar el límite de la clave de API.
