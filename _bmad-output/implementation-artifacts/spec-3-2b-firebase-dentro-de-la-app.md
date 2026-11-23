---
title: 'Historia 3.2 (parte 2): Firebase dentro de la app'
type: 'feature'
created: '2026-11-24'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La app no puede usar Firebase: faltan el SDK (Auth y Firestore) y su inicio con persistencia sin conexión (AD-2, AD-8).

**Approach:** Firebase Unity SDK tras el adaptador `PriceMap.Adaptadores.Firebase`, con persistencia activada y un arranque que no depende de él.

## Boundaries & Constraints

**Always:**
- Decidido (usuario, 24/11/2026): la 3.2 va en dos partes; esta es la 2; las reglas están en la 1.
- Decidido (24/11/2026; el repositorio es público): `google-services.json` fuera de git, y también lo que se genera de él (`google-services-desktop.json`, `google-services.xml`).
- **SDK:** `com.google.firebase.app`, `.auth`, `.firestore` 14.1.0 (o la última compatible con 6000.3), `.tgz` locales en `unity/PriceMap/Packages/` referenciados por ruta en `manifest.json`. Dependencias Android con EDM4U.
- **Ensamblado** `PriceMap.Adaptadores.Firebase` → Firebase, Aplicación, Dominio. Ningún otro referencia Firebase (prueba que recorre los `.asmdef`).
- **Inicio:** `FirebaseApp.CheckAndFixDependenciesAsync` en el Arranque; si falla (p. ej. PC sin `google-services.json`), sigue sin cuenta ni servidor, con log `PriceMap:`.
- **Persistencia:** `FirebaseFirestore.Settings.PersistenceEnabled = true`, caché ilimitada razonable (100 MB). Único almacén local de datos dinámicos (AD-8).

**Never:** sin Cloud Functions, Storage ni plan Blaze (AD-7); nada fuera del adaptador usa Firebase.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Arranque con Firebase | `google-services.json` válido | Firebase listo; log "PriceMap: Firebase listo" |
| Arranque sin configuración | PC o sin json | La app arranca sin servidor; log de aviso; nada falla |
| Asmdef | UI referencia Firebase | La prueba de capas falla |

</frozen-after-approval>

## Code Map

- SDK: 14.1.0 existe (`https://dl.google.com/games/registry/unity/com.google.firebase.<app|auth|firestore>/…-14.1.0.tgz`; EDM4U, `com.google.external-dependency-manager`, hasta 1.2.190). Los `.tgz` pesan unos 80 MB: no van a git, sino a `unity/PriceMap/Packages/Firebase/` con un script que los baja y comprueba su SHA-256 (`firebase/descargar-sdk.ps1`), citado en `LEEME.md`. Se verifica que sin ellos el mensaje es claro. Descargas nuevas, cada una en su carpeta, sin ejecutar nada de dentro.
- EDM4U por lotes: sin ventanas (ajustes del resolvedor guardados en `ProjectSettings`), plantillas Gradle si hacen falta; minSdk 26, targetSdk 35, ARM64, IL2CPP. Respetar los post-procesos de `Assets/Editor` (`ActividadAndroid`, `PantallaInicioRecursos`, `allowBackup=false`) y que `PruebasCapas` (recorre los `.asmdef` de `Assets`) siga valiendo. `link.xml` propio si IL2CPP recorta algo.
- `Scripts/Adaptadores/Firebase/` (asmdef `PriceMap.Adaptadores.Firebase`): `InicioFirebase` (puerto `IServidor` en Aplicacion: estado Iniciando/Listo/NoDisponible, `Cambiado`) y `AlmacenFirestore` (leer, escribir por campo con merge y escuchar un documento, `Resultado<T>`; los errores de Firebase, a códigos propios). Solo se inicia en Android; en editor, PC y captura, `NoDisponible` sin tocar Firebase. El hilo: como los demás adaptadores, vuelve al principal.
- `Arranque/Arranque.cs`: inicio sin esperar; nada de la app depende de él todavía. `CapturaPruebas.SePide()` → no disponible.
- Docs: versión del SDK fijada en ARCHITECTURE-SPINE (tabla, línea 208).

## Tasks & Acceptance

**Execution:**
- [ ] SDK, EDM4U, script de descarga y `.gitignore`.
- [ ] `IServidor`, `Adaptadores/Firebase` y `Arranque`.
- [ ] Pruebas: capas, inicio (falla, tarda, sin Firebase), nada secreto en git.

**Acceptance Criteria:**
- Dada la suite EditMode, la APK y la build de PC, cuando se ejecutan, entonces pasan y la app arranca sin servidor en PC.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode, `CompilarApk` (`-buildTarget Android`), `CompilarPcPruebas` (`-buildTarget Win64`) y captura; borrar `link.xml` de Addressables y deshacer los `addressables_content_state.bin`.

**Manual checks:**
- El log del móvil dice «PriceMap: Firebase listo»; sin conexión, la app abre igual.
