---
title: 'Historia 3.2: mis datos en Firebase, protegidos y disponibles sin conexión'
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

**Problem:** La app no tiene servidor. Las épicas 3–9 necesitan Firebase (Auth y Firestore) con persistencia sin conexión y reglas que protejan los datos de cada usuario (AD-2, AD-5, AD-6, AD-7, AD-8, NFR-8).

**Approach:** Firebase Unity SDK (Auth, Firestore) tras el adaptador `PriceMap.Adaptadores.Firebase`, con persistencia activada. `firebase/firestore.rules` versionado con la regla base de `usuarios/<uid>`, probado con emulador y arnés de Node.

## Boundaries & Constraints

**Always:**
- **Requisitos previos del usuario** (el agente no puede): proyecto de Firebase en plan Spark; app Android `com.pricemap.app` con la SHA-1 del keystore de depuración/publicación; activar Auth (Google y correo con enlace) y Firestore; `google-services.json` en `unity/PriceMap/Assets/`. El agente prepara lo demás y documenta los pasos en `firebase/LEEME.md`.
- **SDK:** `com.google.firebase.app`, `.auth`, `.firestore` 14.1.0 (o la última compatible con 6000.3), `.tgz` locales en `unity/PriceMap/Packages/` referenciados por ruta en `manifest.json`. Dependencias Android con EDM4U.
- **Ensamblado** `PriceMap.Adaptadores.Firebase` → Firebase, Aplicación, Dominio. Ningún otro referencia Firebase (prueba que recorre los `.asmdef`).
- **Inicio:** `FirebaseApp.CheckAndFixDependenciesAsync` en el Arranque; si falla (p. ej. PC sin `google-services.json`), sigue sin cuenta ni servidor, con log `PriceMap:`.
- **Persistencia:** `FirebaseFirestore.Settings.PersistenceEnabled = true`, caché ilimitada razonable (100 MB). Único almacén local de datos dinámicos (AD-8).
- **Reglas base** (`firebase/firestore.rules`): todo denegado por defecto; `usuarios/{uid}` y subcolecciones: lectura/escritura solo si `request.auth.uid == uid`. Las demás colecciones las abren sus historias.
- **Pruebas de reglas:** `firebase/pruebas/` con `@firebase/rules-unit-testing` y emulador (`firebase emulators:exec`): propio permitido, ajeno denegado, sin sesión denegado.
- **Configuración:** `firebase/firebase.json` y `.firebaserc` con marcador `PRICEMAP_PROJECT_ID` que rellena el usuario.
- `google-services.json` fuera de git si el usuario lo prefiere (preguntar al revisar); `.gitignore` preparado.

**Never:** sin Cloud Functions, Storage ni plan Blaze (AD-7); nada fuera del adaptador usa Firebase.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Arranque con Firebase | `google-services.json` válido | Firebase listo; log "PriceMap: Firebase listo" |
| Arranque sin configuración | PC o sin json | La app arranca sin servidor; log de aviso; nada falla |
| Leer lo propio | uid A lee `usuarios/A` | Permitido (emulador) |
| Leer lo ajeno | uid A lee `usuarios/B` | Denegado |
| Escribir lo ajeno | uid A escribe `usuarios/B/favoritos/x` | Denegado |
| Sin sesión | Lee `usuarios/A` | Denegado |
| Otra colección | Escribe `cualquiera/x` | Denegado |
| Asmdef | UI referencia Firebase | La prueba de capas falla |

</frozen-after-approval>

## Open Questions

1. **Requisitos previos** (sin proyecto de Firebase la historia se bloquea). A) El agente deja todo preparado (adaptador que compila con el SDK, reglas, pruebas con emulador) y el usuario solo añade `google-services.json` (recomendado). B) Esperar a que el usuario cree el proyecto.
2. **¿Emulador disponible?** Necesita Node y Java. Sin Java, las pruebas de reglas van a `deferred-work`.

## Code Map

- Nuevos: `Scripts/Adaptadores/Firebase/` (asmdef, `InicioFirebase.cs`, `AlmacenFirestore.cs` base con `Resultado<T>`).
- `Packages/manifest.json` (paquetes locales), `Assets/Plugins/Android/` (dependencias EDM4U).
- `Arranque/Arranque.cs`: inicio asíncrono de Firebase sin bloquear el mapa.
- `firebase/`: `firestore.rules`, `firebase.json`, `.firebaserc`, `pruebas/` (package.json, reglas.test.js), `LEEME.md`.
- Tests: `PruebasCapas.cs` (ningún asmdef fuera del adaptador referencia Firebase).

## Tasks & Acceptance

**Execution:**
- [ ] `firebase/` con reglas base, configuración y pruebas de emulador.
- [ ] Instalar el SDK y EDM4U; adaptador `Adaptadores/Firebase` con persistencia.
- [ ] Arranque tolerante a la falta de configuración.
- [ ] Prueba de capas.
- [ ] `firebase/LEEME.md` con los pasos del usuario.

**Acceptance Criteria:**
- Dada la suite de reglas con el emulador, cuando se ejecuta, entonces cada fila de reglas de la matriz pasa.
- Dada la suite EditMode y la build de PC sin `google-services.json`, cuando se ejecutan, entonces pasan y la app arranca.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `cd firebase/pruebas && npm test` (emulador).
- Pruebas EditMode por lotes; `CompilarApk` y build de PC.

**Manual checks:**
- Con `google-services.json`, el log del móvil dice "Firebase listo".
