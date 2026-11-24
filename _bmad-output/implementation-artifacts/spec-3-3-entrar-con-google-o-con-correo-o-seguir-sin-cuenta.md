---
title: 'Historia 3.3 (parte 1): entrar con Google o seguir sin cuenta'
type: 'feature'
created: '2026-11-24'
status: 'done'
baseline_commit: 'dc10301bb1e2309607c60260166385431c059184'
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
- [x] `ICuentas`, `ServicioCuentas` y `ExigirCuenta` con pruebas.
- [x] `CuentasFirebase`, `PuenteCuenta.java` y dependencias.
- [x] Hoja, pantalla tras la bienvenida, avatar y toast.
- [x] TalkBack, 200 %, textos y capturas `cuenta(-oscuro)`, `cuenta-bienvenida(-oscuro)`, `cuenta-200`.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con cuentas falsas.
- Dada la APK, cuando se compila, entonces lleva el plugin y no pide permisos nuevos.
- Dado un móvil con la app configurada, cuando entro con Google, entonces la sesión sigue al reabrir (manual).

## Spec Change Log

## Review Triage Log

| Capa | Hallazgo | Veredicto | Prueba | Destino |
|---|---|---|---|---|
| blind | Acción pendiente con una sesión cancelada | high | `StateChanged` llega antes que la continuación y `AlCambiarSesion` la ejecutaba | patch |
| blind | Dos cancelaciones podían dejar sesión | high | `esta != ultima` se saltaba `Salir` aunque la segunda también se cancelara | patch |
| blind | El avatar enseña un momento una sesión revocada | medium | Mismo aviso intermedio | patch |
| blind | Las pruebas no veían esas carreras | medium | `AuthFalso` no avisaba antes de acabar | patch |
| blind | «Reintentar» no sirve con NoDisponible | low | La matriz pide mensaje y la intención, reintento; caso raro y reabrir lo arregla | reject |
| blind | Fallos permanentes con mensaje de reintentar | low | Sin cuenta de Google, tras añadirla sí sirve; los demás son raros | reject |
| blind | Sin límite de tiempo en el selector o Firebase | low | «Ahora no» corta siempre; el SDK tiene el suyo | reject |
| blind | Cuenta sin nombre ni correo | low | Google siempre da el correo | reject |
| blind | El aviso no llega a quien ya pasó la bienvenida | medium | Ya estaba en pendientes poder leerlo desde Ajustes (3.5) | defer |
| blind | Aviso incompleto (salir, borrar, sesión en el móvil) | medium | Salir y borrar son la 3.5; va con lo del RGPD pendiente | defer |
| blind | La hoja parece arrastrable y no lo es | medium | El asa está en la maqueta aprobada; arrastrar es un gesto nuevo | defer |
| blind | TalkBack no dice que se está entrando | medium | Solo cambiaba la etiqueta | patch |
| blind | Excepción de la UI contada como fallo al entrar | low | Solo con un fallo propio de la UI; la acción pendiente se hace fuera del try | reject |
| blind | `EscucharSiListo` no se recupera | low | Listo solo llega una vez; no hay cuándo reintentar | reject |
| blind | Cobertura: avatar al 200 % y oscuro, «Cómo llegar», llegada | medium | Sin prueba ni captura | patch |
| blind | Memoria y CLAUDE.md; requisitos de Firebase | low | Al cerrar; LEEME lo explica | reject |
| edge | Dos cancelaciones podían dejar sesión | high | Igual que blind | patch |
| edge | Aviso antes de la continuación | high | Igual que blind | patch |
| edge | «[16] Cancelled by user.» | medium | Cerrar el selector enseñaba un error | patch |
| edge | NoCredentialException | low | Igual que blind | reject |
| edge | Sin nombre ni correo | low | Igual que blind | reject |
| edge | Enlace corto compartido con la cuenta abierta | medium | Se perdía `abrirSolo` | patch |
| edge | ExigirCuenta con la explicación abierta | false | El avatar no abre con una modal y nadie más llama aún a ExigirCuenta | reject |
| edge | Marcas con Modal falso y Exclusivo | false | El aviso no puede estar abierto debajo: mismo motivo | reject |
| edge | «Hola» cortado por el foco | medium | El foco va en la pasada siguiente | patch |
| edge | Entrada ya abierta al crear la pantalla | false | El servicio se crea cerrado y solo lo abre la pantalla | reject |
| edge | Toast encima de la ficha | medium | Antes quedaba debajo | patch |
| edge | Acción pendiente con sesión cancelada (claim) | high | Igual que blind | patch |
| gap | Avatar en el recorrido de otras modales | medium | Pruebas añadidas | patch |
| gap | Valor del avatar con la sesión guardada | medium | Prueba añadida | patch |
| gap | «Entrando…» en el nodo | medium | Prueba añadida | patch |
| gap | Menú Precios abierto | medium | Prueba añadida | patch |
| gap | Sitio del toast sin ruta | medium | La captura lo comprueba | patch |
| gap | Selector de Google sin ejecutar | medium | `IPuenteCuenta` con uno falso; el Java, en el móvil | patch + defer |
| gap | Zona segura del toast y de la hoja | low | `Screen.safeArea` no se puede fijar en EditMode; como las demás zonas | defer |
| gap | Sesión tras cancelar | high | Igual que blind | patch |
| gap | Acción pendiente | high | Igual que blind | patch |
| gap | «Hola» cortado | medium | Igual que edge | patch |
| gap | Toast encima | medium | Igual que edge | patch |

## Implementation Notes

- Verificado (24/11/2026): 1687/1687 pruebas, APK con `PuenteCuenta` y sin permisos nuevos, PC y captura sin errores. Google de verdad, sin probar en un móvil.
- El botón de Google va en Nunito Sans (no hay Roboto en el proyecto).

## Verification

**Commands:**
- Pruebas EditMode, `CompilarApk`, `CompilarPcPruebas` y captura de PC.

**Manual checks:**
- Google en un móvil, con el proveedor activado en la consola de Firebase. Si sale «Requests from this Android client application are blocked», quitar el límite de la clave de API.
