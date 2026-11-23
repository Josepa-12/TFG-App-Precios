# Firebase de PriceMap

PriceMap no tiene servidor propio: usa Firebase en el plan gratuito (Spark), solo **Authentication** (Google y enlace por correo) y **Cloud Firestore**. Las reglas de esta carpeta son lo que protege los datos.

## Qué hay

- `firestore.rules`: reglas de seguridad. Todo está denegado salvo `usuarios/<uid>` y sus subcolecciones, que solo lee y escribe su dueño. Cada historia que necesite otra colección la abre aquí, con sus pruebas. Los permisos se suman: cuando lleguen `visitas` y `aportes` (épica 4), la regla de `usuarios` se sustituye por subcolecciones con nombre, porque añadir otra al lado no la recorta.
- `firebase.json`: dónde están las reglas y cómo arranca el emulador.
- `.firebaserc`: el proyecto de Firebase (`pricemap-98f78`).
- `pruebas/`: pruebas de las reglas con el emulador de Firestore.

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

El repositorio es público, así que la configuración de la app no se sube (está en `.gitignore`). Para montar el proyecto, descárgala de la consola (Configuración del proyecto → Tus apps → app Android `com.pricemap.app`) y déjala en `unity/PriceMap/Assets/google-services.json`. Lo que se genera a partir de ella tampoco se sube.

Si se firma la app con otra clave (otro ordenador u otro almacén de claves), hay que añadir su huella SHA-1 en esa misma página y volver a descargar el archivo, o la entrada con Google no funcionará.
