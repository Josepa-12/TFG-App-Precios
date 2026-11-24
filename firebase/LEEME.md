# Firebase de PriceMap

PriceMap no tiene servidor propio: usa Firebase en el plan gratuito (Spark), solo **Authentication** (Google, y correo y contraseña) y **Cloud Firestore**. Las reglas de esta carpeta son lo que protege los datos.

## Qué hay

- `firestore.rules`: reglas de seguridad. Todo está denegado salvo `usuarios/<uid>` y sus subcolecciones, que solo lee y escribe su dueño. Cada historia que necesite otra colección la abre aquí, con sus pruebas. Los permisos se suman: cuando lleguen `visitas` y `aportes` (épica 4), la regla de `usuarios` se sustituye por subcolecciones con nombre, porque añadir otra al lado no la recorta.
- `firebase.json`: dónde están las reglas y cómo arranca el emulador.
- `.firebaserc`: el proyecto de Firebase (`pricemap-98f78`).
- `pruebas/`: pruebas de las reglas con el emulador de Firestore.
- `descargar-sdk.ps1`: descarga el SDK de Firebase para Unity.

## Descargar el SDK de Unity

La app usa el Firebase Unity SDK 14.1.0 (Auth y Firestore) y EDM4U 1.2.190 (sus dependencias de Android). Son unos 80 MB en archivos `.tgz` y no se suben. **Antes de abrir el proyecto de Unity por primera vez**, en la raíz del repositorio:

```
powershell -NoProfile -ExecutionPolicy Bypass -File firebase/descargar-sdk.ps1
```

Los deja en `unity/PriceMap/Packages/Firebase` (allí los busca `Packages/manifest.json`) y comprueba su huella SHA-256. Sin ellos, Unity no abre el proyecto: dice `com.google.firebase.app: The file […\Packages\Firebase\…tgz…] cannot be found` (y lo mismo con los otros tres). Para cambiar de versión hay que cambiar a la vez `manifest.json` y la versión y la huella del script (`PruebasFirebase` compara las versiones; la huella la comprueba el script al bajarlos).

Al compilar la APK, EDM4U escribe las dependencias de Android en `Assets/Plugins/Android/mainTemplate.gradle` y `settingsTemplate.gradle`, y copia las bibliotecas del SDK a `Assets/GeneratedLocalRepo` (no se sube). Esos dos `.gradle` sí se suben; al cambiar de versión de Unity hay que volver a crearlos (Player Settings → Publishing Settings). Los ajustes de EDM4U, sin ventanas, están en `ProjectSettings/GvhProjectSettings.xml`. El SDK trae también Analytics: `PrivacidadFirebaseAndroid` lo deja desactivado y le quita los permisos de publicidad.

## Probar las reglas

Hace falta Node (24.12 o posterior) y Java 21 o posterior.

```
cd firebase/pruebas
npm ci
npm test
```

- La primera vez se descarga el emulador, así que hace falta internet.
- Las pruebas usan un proyecto ficticio (`demo-pricemap`): no hay que iniciar sesión y nunca tocan el proyecto real.
- Los avisos `PERMISSION_DENIED` son normales: son las escrituras que deben denegarse. Lo que cuenta es el resumen final: `fail 0` y, además, `pass` igual a `tests` y mayor que 1 (con `fail 0` a secas puede que no se haya ejecutado ninguna).
- El emulador usa el puerto 8080. Si está ocupado, cierra lo que lo use o cambia el puerto en `firebase.json`.

## Publicar las reglas

Cambiar `firestore.rules` no cambia nada en Firebase hasta que se publica. Antes, pasa las pruebas. Después, de una de estas dos formas:

- **En la consola:** [console.firebase.google.com](https://console.firebase.google.com) → proyecto `pricemap-98f78` → Firestore → Reglas → pegar el contenido de `firestore.rules` → Publicar.
- **Con la línea de comandos**, desde `firebase/pruebas` (después de `npm ci`):

```
npx firebase login
npx firebase deploy --config ../firebase.json --project pricemap-98f78 --only firestore:rules
```

## `google-services.json` va aparte

El repositorio es público, así que la configuración de la app no se sube (está en `.gitignore`). Para montar el proyecto, descárgala de la consola (Configuración del proyecto → Tus apps → app Android `com.pricemap.app`) y déjala en `unity/PriceMap/Assets/google-services.json`. Lo que se genera a partir de ella tampoco se sube. Sin ella, la app compila y funciona, pero sin cuenta ni servidor.

La clave de API que lleva el archivo también va dentro de cada APK: no es secreta, y lo que protege los datos son las reglas. Aun así, conviene limitarla en Google Cloud (APIs y servicios → Credenciales) a la app `com.pricemap.app` con su huella SHA-1.

Si se firma la app con otra clave (otro ordenador u otro almacén de claves), hay que añadir su huella SHA-1 en esa misma página y volver a descargar el archivo, o la entrada con Google no funcionará.

## Entrar con Google o con correo

En la consola de Firebase, Authentication → Método de acceso, el proveedor **Google** debe estar activado, y en la app Android debe estar la huella SHA-1 con la que se firma la APK. Si falta la huella, Google rechaza la entrada y la app dice «No se pudo iniciar sesión». Si la clave de API está limitada y falla con «Requests from this Android client application are blocked», hay que quitar ese límite.

Para entrar con correo, en esa misma página debe estar activado **Correo electrónico/contraseña** (sin el enlace por correo); si no, la app dice que no se pudo entrar o crear la cuenta. Las contraseñas las guarda Firebase. Conviene dejar activada la protección contra la enumeración de correos (Authentication → Configuración), para que no se sepa qué correos tienen cuenta. Si se pone una política de contraseñas más estricta que 8 caracteres, hay que cambiar el texto de la app.
