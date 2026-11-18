// Uso: node prototipo/herramientas/exportar-datos-unity.js
// Exporta los lugares reales del prototipo (datos-lugares.js), los añadidos a mano y las tarifas
// de la web oficial de cada negocio (datos-precios-web.js) al JSON que empaqueta la app de Unity:
// unity/PriceMap/Assets/Datos/Resources/lugares.json (historia 1.4).
//
// Reglas (spec de la historia 1.4):
// - Solo precios de la web oficial, cada uno con su página y su fecha de consulta.
// - Importes en céntimos enteros. Los conceptos sin importe ("consultar") no se exportan (18/11/2026).
// - Los precios "con bono" no se usan: el concepto no se exporta.
// - PLACE_CONCEPTS (sugerencias del prototipo) no se migra.
// Termina con error si hay ids repetidos, una plantilla desconocida o coordenadas fuera de Sanlúcar.
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const raiz = path.resolve(__dirname, '..', '..');
const proto = path.join(raiz, 'prototipo');
const salida = path.join(raiz, 'unity', 'PriceMap', 'Assets', 'Datos', 'Resources', 'lugares.json');

// Caja de Sanlúcar de Barrameda (la misma que comprueban las pruebas de Unity)
const SANLUCAR = { latMin: 36.70, latMax: 36.85, lonMin: -6.45, lonMax: -6.25 };
const PLANTILLAS = { servicio: 'servicio', hosteleria: 'hosteleria', ocio: 'ocio' };

const errores = [];
const fallo = m => errores.push(m);

// Los ficheros del prototipo son JS con `const`: se ejecutan en un contexto aislado y se sacan las constantes
const ctx = {};
vm.createContext(ctx);
const codigo = ['datos-lugares.js', 'datos-precios-web.js']
  .map(f => fs.readFileSync(path.join(proto, f), 'utf8')).join('\n;\n');
vm.runInContext(codigo + '\n;this.__d = { REAL_PLACES, EXTRA_PLACES, WEB_PRICES };', ctx);
const { REAL_PLACES, EXTRA_PLACES, WEB_PRICES } = ctx.__d;

const slug = s => s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');

// "13/11/2026" → "2026-11-13"
function fechaIso(f, donde) {
  const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(f || '');
  if (!m) { fallo(`${donde}: fecha de consulta no válida (${f})`); return ''; }
  return `${m[3]}-${m[2]}-${m[1]}`;
}

function conceptos(id) {
  const web = WEB_PRICES[id];
  if (!web) return { lista: [], conBono: 0 };
  if (!web.url) fallo(`${id}: las tarifas no tienen página`);
  const fecha = fechaIso(web.fecha, id);
  const lista = [];
  let conBono = 0;
  for (const [area, items] of Object.entries(web.grupos)) {
    for (const [nombre, unidad, precio, opciones = {}] of items) {
      if (/bono/i.test(opciones.nota || '')) { conBono++; continue; } // precio con bono: no se usa
      const cid = `${slug(area)}/${slug(nombre)}`;
      if (lista.some(c => c.id === cid)) fallo(`${id}: concepto repetido ${cid}`);
      // Sin importe ("consultar") no se exporta: decisión del 18/11/2026, la ficha solo muestra precios publicados
      if (precio === null || precio === undefined) continue;
      if (typeof precio !== 'number' || !(precio > 0)) fallo(`${id}/${cid}: importe no válido (${precio})`);
      const forma = opciones.desde ? 'desde' : 'fijo';
      const cent = Math.round(precio * 100);
      lista.push({ id: cid, area, nombre, unidad: unidad || '', forma, cent, url: web.url, fecha });
    }
  }
  return { lista, conBono };
}

const lugares = [];
let totalConceptos = 0, totalBono = 0;
for (const p of [...REAL_PLACES, ...EXTRA_PLACES]) {
  if (!p.id) { fallo(`lugar sin id: ${p.name}`); continue; }
  if (lugares.some(l => l.id === p.id)) fallo(`id repetido: ${p.id}`);
  if (!PLANTILLAS[p.tpl]) fallo(`${p.id}: plantilla desconocida (${p.tpl})`);
  const [lon, lat] = p.pos || []; // el prototipo da [lon, lat]
  if (!(lat >= SANLUCAR.latMin && lat <= SANLUCAR.latMax && lon >= SANLUCAR.lonMin && lon <= SANLUCAR.lonMax))
    fallo(`${p.id}: coordenadas fuera de Sanlúcar (${p.pos})`);
  const publico = p.publico === true;
  const { lista, conBono } = conceptos(p.id);
  totalBono += conBono;
  const cs = publico ? [] : lista; // la sanidad pública nunca tiene precio
  totalConceptos += cs.length;
  lugares.push({
    id: p.id, nombre: p.name, tipo: p.cat || '', plantilla: PLANTILLAS[p.tpl],
    lat, lon, direccion: p.addr || '', publico, conceptos: cs,
  });
}
for (const id of Object.keys(WEB_PRICES))
  if (!lugares.some(l => l.id === id)) fallo(`tarifas de un lugar que no existe: ${id}`);

if (errores.length) {
  console.error('No se exporta nada:\n  ' + errores.join('\n  '));
  process.exit(1);
}

const json = {
  generado: 'prototipo/herramientas/exportar-datos-unity.js',
  fuente: 'Lugares: © colaboradores de OpenStreetMap (ODbL). Precios: web oficial de cada negocio.',
  lugares,
};
fs.mkdirSync(path.dirname(salida), { recursive: true });
fs.writeFileSync(salida, JSON.stringify(json, null, 1) + '\n');
console.log(`${lugares.length} lugares, ${totalConceptos} conceptos (${totalBono} con bono descartados) → ${path.relative(raiz, salida)}`);
