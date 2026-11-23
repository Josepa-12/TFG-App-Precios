---
title: 'Historia 3.2 (parte 1): reglas de seguridad de Firestore'
type: 'feature'
created: '2026-11-24'
status: 'done'
baseline_commit: '621109aed8c58a7ac5b3869d11379fd66ddcc1dc'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La app no tiene servidor. Antes de guardar nada en Firestore hacen falta reglas que protejan los datos de cada usuario (AD-5, AD-6, AD-7, NFR-8), versionadas y probadas.

**Approach:** Carpeta `firebase/` con `firestore.rules` (regla base de `usuarios/<uid>`), su configuración, pruebas con el emulador y un `LEEME.md` para el usuario.

## Boundaries & Constraints

**Always:**
- Decidido (usuario, 24/11/2026): la 3.2 va en dos partes; esta es la 1 (reglas); la 2 (`spec-3-2b`) mete el SDK en la app.
- Decidido (24/11/2026; el repositorio es público): `google-services.json` fuera de git, y también lo que se genera de él (`google-services-desktop.json`, `google-services.xml`).
- Requisitos previos hechos (usuario, 24/11/2026): proyecto `pricemap-98f78` (Spark), app con SHA-1, Auth con Google y enlace por correo, Firestore Standard `(default)` en modo producción (comprobado: existe y deniega) y el json en su sitio. Node y Java 21 están instalados: las reglas se prueban con el emulador.
- **Reglas base** (`firebase/firestore.rules`): todo denegado por defecto; `usuarios/{uid}` y subcolecciones: lectura/escritura solo si `request.auth.uid == uid`. Las demás colecciones las abren sus historias.
- **Pruebas de reglas:** `firebase/pruebas/` con `@firebase/rules-unit-testing` y emulador (`firebase emulators:exec`): propio permitido, ajeno denegado, sin sesión denegado.
- **Configuración:** `firebase/firebase.json` y `.firebaserc` con el proyecto `pricemap-98f78`.

**Never:** Cloud Functions, Storage ni plan Blaze (AD-7); iniciar sesión en Firebase ni desplegar (lo hace el usuario); tocar el proyecto de Unity.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Leer lo propio | uid A lee `usuarios/A` | Permitido (emulador) |
| Leer lo ajeno | uid A lee `usuarios/B` | Denegado |
| Escribir lo ajeno | uid A escribe `usuarios/B/favoritos/x` | Denegado |
| Sin sesión | Lee `usuarios/A` | Denegado |
| Otra colección | Escribe `cualquiera/x` | Denegado |
| Escribir lo propio | uid A escribe `usuarios/A` y `usuarios/A/favoritos/x` | Permitido |
| Listar usuarios | uid A lista `usuarios` | Denegado |

</frozen-after-approval>

## Code Map

- `firebase/firestore.rules`, `firebase/firebase.json` (reglas y emulador de Firestore), `firebase/.firebaserc` (`pricemap-98f78`), `firebase/LEEME.md`.
- `firebase/pruebas/`: `package.json` con versiones fijas de `firebase-tools` y `@firebase/rules-unit-testing`, `package-lock.json` y `reglas.test.js` (el ejecutor de pruebas de Node, sin más dependencias). `npm test` arranca el emulador con un proyecto `demo-…`: nunca el real ni la red de Firebase. `node_modules` ya está ignorado.
- `.gitignore` (raíz): el json de Firebase ya está; añadir lo generado (`google-services-desktop.json*`, `Assets/Plugins/Android/FirebaseApp.androidlib/`) y los registros del emulador (`*-debug.log`, `.firebase/`).
- `LEEME.md` (para quien monte el proyecto): qué hay, cómo probar las reglas y cómo publicarlas (pegarlas en la consola, Firestore → Reglas, o `firebase deploy --only firestore:rules`), y que `google-services.json` va aparte.

## Tasks & Acceptance

**Execution:**
- [x] `firebase/`: reglas, configuración y `LEEME.md`.
- [x] `firebase/pruebas/`: una prueba por fila de la matriz, y más si una regla lo pide.
- [x] `.gitignore`.

**Acceptance Criteria:**
- Dado el emulador, cuando se ejecuta `npm test`, entonces pasan todas, y una regla rota a propósito (abrir `usuarios`) las hace fallar.
- Dado `git status`, cuando se acaba, entonces no hay nada secreto ni generado por subir.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| Hallazgo | Veredicto | Motivo | Destino |
|---|---|---|---|
| BH1 la regla recursiva no se puede estrechar después | medium | Firestore suma los `allow`: AD-6 (visitas) exigirá sustituirla. La pide el spec; se avisa en el fichero y queda para la épica 4 | patch |
| BH2/EC7 sin límite de campos ni tamaño | medium | Cada historia fijará los campos de lo suyo (AD-6, AD-18); hoy no hay ninguno que validar | defer |
| BH3/EC3 clave de administrador sin ignorar | medium | `git check-ignore` no la cubre y el repositorio es público | patch |
| BH4/EC2 copias de `google-services.json` | medium | Solo se ignora una ruta exacta | patch |
| BH5 patrones sueltos | low | `*-debug.log` vale para todo el repositorio | patch |
| BH6 `firebase` sin fijar | low | Entra como dependencia de otra | patch |
| BH7 dos ficheros a la vez se pisarían | low | Comparten proyecto y `clearFirestore` | patch |
| BH8 «fail 0» sin pruebas | low | Se nombra el fichero en el script | patch |
| BH9 comentario falso | low | Hay rutas sin sembrar | patch |
| BH10/EC5/EC6/VG1/VG2 denegaciones sin probar | medium | Ocho reglas rotas a propósito pasaban 13/13 | patch |
| BH11/VG5 lo publicado puede no ser lo del repositorio | medium | Aquí no se inicia sesión: se comprueba al usar Firebase desde la app (3.2b) | defer |
| EC1 sin `location` en `firebase.json` | low | La base ya existe (comprobado) y solo importaría al crearla | reject |
| EC4 sin límite de tiempo | low | Se añade al script | patch |
| VG3 solo se prueba `favoritos` | medium | Falta prueba con otra subcolección | patch |
| VG4 fuera del camino normal | medium | Se añade a los comandos de `CLAUDE.md` al cerrar | patch |

## Verification

**Commands:**
- `cd firebase/pruebas && npm ci && npm test`.

**Manual checks:**
- El usuario publica las reglas en la consola.
