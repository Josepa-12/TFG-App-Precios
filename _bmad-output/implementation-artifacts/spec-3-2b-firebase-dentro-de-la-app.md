---
title: 'Historia 3.2 (parte 2): Firebase dentro de la app'
type: 'feature'
created: '2026-11-24'
status: 'done'
baseline_commit: '062d5dbcc35c70ff08aebd7c25ede118185aeac4'
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
- [x] SDK, EDM4U, script de descarga y `.gitignore`.
- [x] `IServidor`, `Adaptadores/Firebase` y `Arranque`.
- [x] Pruebas: capas, inicio (falla, tarda, sin Firebase), nada secreto en git.

**Acceptance Criteria:**
- Dada la suite EditMode, la APK y la build de PC, cuando se ejecutan, entonces pasan y la app arranca sin servidor en PC.

## Implementation Notes

- SDK 14.1.0 y EDM4U 1.2.190. Los demás ensamblados llevan `overrideReferences: true`, porque las `.dll` de Firebase se referencian solas.
- El SDK trae Analytics: `PrivacidadFirebaseAndroid` lo desactiva y quita sus permisos de publicidad.
- Verificado (24/11/2026): 1601/1601 pruebas; APK con `libFirebaseCpp*` y sin permisos de publicidad; PC arranca con el log de aviso y sin excepciones. Sin probar en un móvil.
- El diff que se dio primero a los revisores solo tenía los archivos nuevos; después revisaron los modificados («-2» en la tabla).

## Spec Change Log

## Review Triage Log

| Capa | Hallazgo | Veredicto | Prueba | Destino |
|---|---|---|---|---|
| blind | DelMovil no cuenta las escrituras pendientes | medium | `Leido` solo miraba `IsFromCache`; la foto local tras escribir lleva `HasPendingWrites`. Con él desaparecen también los avisos repetidos iguales | patch |
| blind | Excepción dentro del aviso de `Listen` | medium | `Leido` podía lanzar dentro del SDK y `alFallar` no se llamaba; las del consumidor sí son suyas | patch |
| blind | `Escuchar` avisa antes de devolver si falla al momento | low | Es así en el hilo principal; se documenta en `IAlmacen` | patch |
| blind | Valores prometidos que Firestore rechaza (listas en listas, HoraServidor en listas, mapas con tipo) | medium | `AFirestore` los dejaba pasar o los tomaba por lista | patch |
| blind | `DeFirestore` convierte int/float en null | false | El SDK solo devuelve long y double; `escrito` no es lo que devuelve Firestore | reject |
| blind | HoraServidor sin prueba | medium | Ninguna la cubría; desfase de AD-12 no es de esta historia | patch |
| blind | La copia del móvil sobrevive a cerrar sesión | medium | No hay cuentas todavía; va con la 3.3 y la de borrar cuenta | defer |
| blind | No hay borrar, y MergeAll junta mapas | medium | Lo segundo no estaba documentado (patch); borrar va con quien lo use (defer) | patch + defer |
| blind | Sin reintento ni límite de tiempo al iniciar | low | `Iniciando` puede no acabar, según el contrato; al volver a abrir se reintenta; arreglarlo añade estados | reject |
| blind | Firebase empieza antes de la bienvenida | maybe-false | Analytics está desactivado y nada usa Auth ni Firestore: no consta que salga nada del móvil; el aviso sí tendrá que nombrar Firebase al usarlo (medium sin verificar) | defer |
| blind | Analytics desactivado pero incluido | low | `firebase_analytics_collection_deactivated` lo apaga del todo; lo exige la dependencia de Auth | reject |
| blind | El manifiesto final no se comprueba | low | Mirado con aapt en esta APK: sin permisos de publicidad; una lista cerrada añade complejidad | reject |
| blind | IL2CPP sin `link.xml` propio; `AccesoFirestore` no se ha ejecutado en APK | maybe-false | Hace falta un móvil y una historia que lo use (medium sin verificar) | defer |
| blind | Registro del motivo completo y aviso tras parar | low | Solo en el registro del móvil | reject |
| blind | Rutas con `.`, `..`, `__x__` | low | El SDK las rechaza y llega un código; `Escuchar` con ruta mala no tenía prueba (añadida) | reject |
| blind | Los escaneos de secretos no ven lo no añadido; sin `.git` fallan | medium | `git grep`/`ls-files` solo miran lo seguido y el repositorio es público | patch |
| blind | Pruebas que leen el código | medium | Firestore real necesita el emulador | defer |
| blind | Script solo de Windows, sin límite de tiempo | low | Límite añadido; el resto, no: se trabaja en Windows y `mavenLocal` lo pone EDM4U | patch |
| blind | Memoria y CLAUDE.md sin actualizar | low | Se hace al cerrar la historia | reject |
| blind-2 | `overrideReferences` ausente se daba por bueno | medium | El valor por defecto de la prueba no era el de Unity | patch |
| blind-2 | Scripts sin asmdef; Red permitida; asmdef ajenos | medium | Lo primero, cierto; Red es regla común de los adaptadores; un asmdef ajeno fallaría con mensaje claro | patch |
| blind-2 | `google-services-desktop.json` en la APK | low | La misma configuración ya va en los recursos | reject |
| blind-2 | Configuración vieja si se quita el json | low | Poco probable y con un solo proyecto | reject |
| blind-2 | Llamada a EDM4U frágil | low | Falla la compilación con error; dos APK seguidas bien | reject |
| blind-2 | `ResolveSync(true)` reescribe archivos seguidos | low | Dos compilaciones seguidas no los cambian; se dice en LEEME que se suben | reject |
| blind-2 | Plantillas Gradle fijadas a 6000.3.28f1 | low | Dicho en LEEME | patch |
| blind-2 | LEEME dice que la prueba comprueba la huella | low | No era cierto: corregido | patch |
| blind-2 | `StreamingAssets.meta` ignorado | low | Hoy no hay nada más en la carpeta | reject |
| blind-2 | La build de PC lleva Firebase | low | Es solo de pruebas | reject |
| blind-2 | Clave de API sin limitar ni documentar | medium | Va en cada APK; recomendación en LEEME | patch |
| blind-2 | «Sin ella, la app compila y funciona» sin probar en Android | maybe-false | Cubierto con un inicio falso; falta un móvil (low) | reject |
| blind-2 | `servidor` sin usar y aviso como warning | false | La matriz pide log de aviso, y el Code Map que nada dependa de él aún | reject |
| edge | DelMovil sin escrituras pendientes | medium | Igual que blind | patch |
| edge | Avisos idénticos con MetadataChanges.Include | low | Con las pendientes en DelMovil ya no son iguales | patch |
| edge | `Escuchar` no traduce lo que lanza al empezar | medium | `Leer` y `Escribir` sí lo hacían | patch |
| edge | MergeAll junta mapas | medium | Documentado en `IAlmacen`; el spec pide merge | patch |
| edge | Mapas con tipo | medium | Vacíos se guardaban como lista | patch |
| edge | HoraServidor o lista en lista | medium | Igual que blind | patch |
| edge | CheckAndFix puede mostrar un diálogo al abrir | low | Lo pide la parte congelada; solo con los servicios de Google antiguos o apagados | reject |
| edge | Suscriptor de `Cambiado` que lanza | low | Unity lo registra; aún no hay suscriptores | reject |
| edge | Script sin límite de tiempo | low | `-TimeoutSec 600` | patch |
| edge | git acaba en 128 | low | Se ignora como sin git | patch |
| edge | Clave en un archivo sin añadir | medium | `--untracked` | patch |
| edge-2 | `overrideReferences` por defecto | medium | Igual que blind-2 | patch |
| edge-2 | Scripts sin asmdef | medium | Igual que blind-2 | patch |
| edge-2 | androidlib viejo | low | Igual que blind-2 | reject |
| gap | `overrideReferences` ausente | medium | Prueba con JSON sin la clave | patch |
| gap | `Leer`/`Escribir` en el hilo principal | medium | Prueba añadida | patch |
| gap | HoraServidor | medium | Prueba añadida | patch |
| gap | Traducción de los errores del SDK | medium | Prueba por reflexión con el enum real | patch |
| gap | Firestore solo mirado por texto | medium | Emulador, con la primera historia que lo use | defer |
| gap | Rama Android de `CrearServidor` | medium | Decisión separada y probada | patch |
| gap | Secretos sin añadir a git | medium | `--untracked` y `--others` | patch |
| gap | Huella del script sin prueba | low | Probado a mano: con una huella falsa no instala nada | reject |
| gap | Mapas juntados sin documentar | medium | Igual que edge | patch |
| gap | Mapas con tipo | medium | Igual que edge | patch |
| gap-2 | Scripts sin asmdef ni asmref | medium | Prueba `TodosLosScriptsTienenSuEnsamblado` | patch |
| gap-2 | APK sin bibliotecas de Firebase | medium | `CompilarApk` falla si no las lleva | patch |

## Verification

**Commands:**
- Pruebas EditMode, `CompilarApk` (`-buildTarget Android`), `CompilarPcPruebas` (`-buildTarget Win64`) y captura; borrar `link.xml` de Addressables y deshacer los `addressables_content_state.bin`.

**Manual checks:**
- El log del móvil dice «PriceMap: Firebase listo»; sin conexión, la app abre igual.
