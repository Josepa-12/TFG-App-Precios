// Pruebas de firestore.rules con el emulador (npm test). Nunca usan el proyecto real.
'use strict';

const { test, before, beforeEach, after } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const {
  initializeTestEnvironment,
  assertFails,
  assertSucceeds,
} = require('@firebase/rules-unit-testing');

const PROYECTO = 'demo-pricemap';

// Rutas bajo usuarios/<uid>. Más de un nombre: la regla vale para todas, no solo para los favoritos.
const SUBCOLECCIONES = ['favoritos', 'ajustes', 'favoritos/x/notas'];
const OTRAS_COLECCIONES = ['cualquiera', 'lugares'];

let entorno;

// Base de datos vista por cada sesión.
const de = (uid) => entorno.authenticatedContext(uid).firestore();
const sinSesion = () => entorno.unauthenticatedContext().firestore();

before(async () => {
  const emulador = process.env.FIRESTORE_EMULATOR_HOST;
  if (!emulador) throw new Error('Falta el emulador de Firestore: ejecuta las pruebas con «npm test».');
  const { hostname, port } = new URL(`http://${emulador}`);

  entorno = await initializeTestEnvironment({
    projectId: PROYECTO,
    firestore: {
      host: hostname,
      port: Number(port),
      rules: readFileSync(join(__dirname, '..', 'firestore.rules'), 'utf8'),
    },
  });
});

// Documentos de partida, con «x» en cada colección. Los «nuevo» de las pruebas no existen.
beforeEach(async () => {
  await entorno.clearFirestore();
  await entorno.withSecurityRulesDisabled(async (contexto) => {
    const db = contexto.firestore();
    for (const uid of ['A', 'B']) {
      await db.doc(`usuarios/${uid}`).set({ nombre: uid });
      for (const sub of SUBCOLECCIONES) await db.doc(`usuarios/${uid}/${sub}/x`).set({ nombre: 'Casa' });
    }
    for (const col of OTRAS_COLECCIONES) {
      await db.doc(`${col}/x`).set({ dato: 1 });
      await db.doc(`${col}/x/aportes/y`).set({ dato: 1 });
      await db.doc(`${col}/x/usuarios/A`).set({ dato: 1 });
    }
  });
});

after(async () => {
  if (entorno) await entorno.cleanup();
});

// Ninguna operación sobre un documento que existe, ni listar su colección, ni crear otro al lado.
async function todoDenegado(db, ruta) {
  const coleccion = ruta.slice(0, ruta.lastIndexOf('/'));
  await assertFails(db.doc(ruta).get());
  await assertFails(db.collection(coleccion).get());
  await assertFails(db.doc(`${coleccion}/nuevo`).set({ nombre: 'Intruso' }));
  await assertFails(db.doc(ruta).set({ nombre: 'Intruso' }));
  await assertFails(db.doc(ruta).set({ nombre: 'Intruso' }, { merge: true }));
  await assertFails(db.doc(ruta).update({ nombre: 'Intruso' }));
  await assertFails(db.doc(ruta).delete());
}

// --- El documento del usuario ---

test('leer lo propio: permitido', async () => {
  await assertSucceeds(de('A').doc('usuarios/A').get());
});

test('escribir lo propio: permitido', async () => {
  await assertSucceeds(de('A').doc('usuarios/A').set({ nombre: 'Ana' }, { merge: true }));
  await assertSucceeds(de('A').doc('usuarios/A').update({ nombre: 'Ana María' }));
});

test('crear lo propio por primera vez: permitido', async () => {
  await assertSucceeds(de('nuevo').doc('usuarios/nuevo').set({ nombre: 'Nuevo' }));
  await assertSucceeds(de('nuevo').doc('usuarios/nuevo/favoritos/x').set({ nombre: 'Casa' }));
});

test('borrar lo propio: permitido', async () => {
  await assertSucceeds(de('A').doc('usuarios/A').delete());
});

test('leer, escribir y borrar lo ajeno: denegado', async () => {
  await todoDenegado(de('A'), 'usuarios/B');
});

test('sin sesión, leer, escribir y borrar: denegado', async () => {
  await todoDenegado(sinSesion(), 'usuarios/A');
});

test('listar usuarios: denegado', async () => {
  await assertFails(de('A').collection('usuarios').get());
  await assertFails(sinSesion().collection('usuarios').get());
});

test('lote con lo propio y lo ajeno: denegado entero', async () => {
  const db = de('A');
  const lote = db.batch();
  lote.set(db.doc('usuarios/A'), { nombre: 'Cambiado' });
  lote.set(db.doc('usuarios/B/favoritos/x'), { nombre: 'Cambiado' });
  await assertFails(lote.commit());

  await entorno.withSecurityRulesDisabled(async (contexto) => {
    const guardado = await contexto.firestore().doc('usuarios/A').get();
    assert.equal(guardado.data().nombre, 'A');
  });
});

// --- Sus subcolecciones ---

for (const sub of SUBCOLECCIONES) {
  test(`${sub}, lo propio: permitido`, async () => {
    const coleccion = de('A').collection(`usuarios/A/${sub}`);
    await assertSucceeds(coleccion.get());
    await assertSucceeds(coleccion.doc('x').get());
    await assertSucceeds(coleccion.doc('nuevo').set({ nombre: 'Trabajo' }));
    await assertSucceeds(coleccion.doc('x').update({ nombre: 'Trabajo' }));
    await assertSucceeds(coleccion.doc('x').delete());
  });

  test(`${sub}, lo ajeno: denegado`, async () => {
    await todoDenegado(de('A'), `usuarios/B/${sub}/x`);
  });

  test(`${sub}, sin sesión: denegado`, async () => {
    await todoDenegado(sinSesion(), `usuarios/A/${sub}/x`);
  });

  test(`${sub}, buscar en los de todos (grupo de colecciones): denegado`, async () => {
    const nombre = sub.split('/').pop();
    await assertFails(de('A').collectionGroup(nombre).get());
    await assertFails(sinSesion().collectionGroup(nombre).get());
  });
}

// --- Lo demás ---

for (const col of OTRAS_COLECCIONES) {
  test(`otra colección (${col}): denegado`, async () => {
    for (const db of [de('A'), sinSesion()]) {
      await todoDenegado(db, `${col}/x`);
      await todoDenegado(db, `${col}/x/aportes/y`);
      await todoDenegado(db, `${col}/x/usuarios/A`);
      await assertFails(db.doc(`${col}/A`).set({ dato: 2 }));
    }
  });
}
