/* PrecioLocal — lugares, direcciones y rutas (TFG)
 *  - Categorías de puntos de interés (POI) reales de OpenStreetMap, dibujadas con iconos propios
 *    integrados en el mapa (capa de símbolos de MapLibre), al estilo de Mapas de Apple.
 *  - Búsqueda de direcciones (Photon, de komoot, sobre datos de OpenStreetMap).
 *  - Lectura de ubicaciones compartidas: enlaces de WhatsApp / Google Maps / Apple Maps, "geo:" o coordenadas.
 *  - Rutas por calles reales (OSRM; servidores públicos de demostración) con alternativa sin conexión.
 * Se carga antes de app.js. Las funciones usan USER_POS, distM, bearing y SPEED de app.js en tiempo de ejecución.
 */

// ---------- Glifos (trazos estilo Lucide, viewBox 24) ----------
const GLYPH = {
  utensils: '<path d="M3 2v7c0 1.1.9 2 2 2h4a2 2 0 0 0 2-2V2M7 2v20M21 15V2a5 5 0 0 0-5 5v6c0 1.1.9 2 2 2h3Zm0 0v7"/>',
  burger: '<path d="M4 11h16a8 8 0 0 0-16 0ZM3 15h18M5 15v1a3 3 0 0 0 3 3h8a3 3 0 0 0 3-3v-1"/>',
  coffee: '<path d="M17 8h1a4 4 0 1 1 0 8h-1M3 8h14v9a4 4 0 0 1-4 4H7a4 4 0 0 1-4-4ZM6 2v2M10 2v2M14 2v2"/>',
  martini: '<path d="M8 22h8M12 11v11M19 3l-7 8-7-8Z"/>',
  basket: '<path d="m15 11-1 9M19 11l-4-7M2 11h20M3.5 11l1.6 7.4a2 2 0 0 0 2 1.6h9.8a2 2 0 0 0 2-1.6l1.7-7.4M4.5 15.5h15M5 11l4-7M9 11l1 9"/>',
  bag: '<path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4ZM3 6h18M16 10a4 4 0 0 1-8 0"/>',
  fuel: '<path d="M3 22h12M4 9h10M14 22V4a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v18M14 13h2a2 2 0 0 1 2 2v2a2 2 0 0 0 4 0V9.8a2 2 0 0 0-.6-1.4L18 5"/>',
  zap: '<path d="M13 2 3 14h9l-1 8 10-12h-9l1-8Z"/>',
  parking: '<path d="M9 17V7h4a3 3 0 0 1 0 6H9"/>',
  bed: '<path d="M2 4v16M2 8h18a2 2 0 0 1 2 2v10M2 17h20M6 8v9"/>',
  pill: '<path d="m10.5 20.5 10-10a4.95 4.95 0 1 0-7-7l-10 10a4.95 4.95 0 1 0 7 7ZM8.5 8.5l7 7"/>',
  cross: '<path d="M9 3h6v6h6v6h-6v6H9v-6H3V9h6Z"/>',
  dumbbell: '<path d="M6.5 6.5v11M17.5 6.5v11M3 9.5v5M21 9.5v5M6.5 12h11"/>',
  tree: '<path d="M12 22v-7M17 8A5 5 0 0 0 7 8a4 4 0 0 0-1 7.9h12A4 4 0 0 0 17 8Z"/>',
  ticket: '<path d="M3 9a3 3 0 0 0 0 6v3a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1v-3a3 3 0 0 0 0-6V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1ZM13 5v2M13 17v2M13 11v2"/>',
  bank: '<path d="M3 21h18M5 21V10M19 21V10M9 21V10M15 21V10M2 10l10-7 10 7Z"/>',
  scissors: '<circle cx="6" cy="6" r="3"/><path d="M8.1 8.1 12 12M20 4 8.1 15.9"/><circle cx="6" cy="18" r="3"/><path d="M14.8 14.8 20 20"/>',
  wrench: '<path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.8-3.8a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9Z"/>',
  bus: '<rect x="4" y="3" width="16" height="16" rx="2"/><path d="M4 11h16M8 19v2M16 19v2M8 15h.01M16 15h.01"/>',
  school: '<path d="M22 10 12 5 2 10l10 5 10-5ZM6 12v5c3 2 9 2 12 0v-5"/>',
  landmark: '<path d="M3 22h18M6 18v-7M10 18v-7M14 18v-7M18 18v-7M12 2l8 5H4Z"/>',
  church: '<path d="M12 2v5M10 4h4M6 22V12l6-5 6 5v10M10 22v-5h4v5"/>',
  activity: '<path d="M22 12h-4l-3 9L9 3l-3 9H2"/>',
  brain: '<path d="M12 5a3 3 0 1 0-6 .1 4 4 0 0 0-2.5 5.8 4 4 0 0 0 .6 6.6A4 4 0 1 0 12 18ZM12 5a3 3 0 1 1 6 .1 4 4 0 0 1 2.5 5.8 4 4 0 0 1-.6 6.6A4 4 0 1 1 12 18ZM12 5v13"/>',
  ball: '<circle cx="12" cy="12" r="10"/><path d="m12 7 4 3-1.5 5h-5L8 10Z"/>',
  key: '<circle cx="7.5" cy="15.5" r="5.5"/><path d="m21 2-9.6 9.6M15.5 7.5l3 3L22 7l-3-3"/>',
  pin: '<path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/>',
  link: '<path d="M10 13a5 5 0 0 0 7.5.5l3-3a5 5 0 0 0-7-7l-1.7 1.7M14 11a5 5 0 0 0-7.5-.5l-3 3a5 5 0 0 0 7 7l1.7-1.7"/>',
  home: '<path d="m3 10 9-7 9 7v10a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2ZM9 22V12h6v10"/>',
};

// ---------- Categorías de puntos de interés ----------
// near: aparece en "Encuentra cerca". tpl: plantilla de precio a la que podría pasar (ver docs/estudio-puntos-interes.md).
const POI_CATS = {
  comer:      { label: 'Restaurantes',          one: 'Restaurante',        color: '#F07D1A', glyph: 'utensils', near: true },
  rapida:     { label: 'Comida rápida',         one: 'Comida rápida',      color: '#E5541F', glyph: 'burger',   near: true },
  cafe:       { label: 'Cafeterías y dulces',   one: 'Cafetería',          color: '#9B6A3C', glyph: 'coffee',   near: true },
  bares:      { label: 'Bares',                 one: 'Bar',                color: '#D6336C', glyph: 'martini',  near: true },
  super:      { label: 'Supermercados',         one: 'Alimentación',       color: '#D99A00', glyph: 'basket',   near: true },
  tiendas:    { label: 'Tiendas',               one: 'Tienda',             color: '#C9821A', glyph: 'bag',      near: true },
  gasolina:   { label: 'Gasolineras',           one: 'Gasolinera',         color: '#2F7DE1', glyph: 'fuel',     near: true },
  carga:      { label: 'Carga eléctrica',       one: 'Punto de carga',     color: '#16A36A', glyph: 'zap',      near: true },
  parking:    { label: 'Aparcamientos',         one: 'Aparcamiento',       color: '#3B63D9', glyph: 'parking',  near: true },
  hoteles:    { label: 'Hoteles',               one: 'Alojamiento',        color: '#7A5AF0', glyph: 'bed',      near: true },
  farmacia:   { label: 'Farmacias',             one: 'Farmacia',           color: '#E5484D', glyph: 'pill',     near: true },
  salud:      { label: 'Salud',                 one: 'Salud',              color: '#C93434', glyph: 'cross',    near: true },
  deporte:    { label: 'Gimnasios y deporte',   one: 'Deporte',            color: '#0E9FB0', glyph: 'dumbbell', near: true },
  parques:    { label: 'Parques y paseos',      one: 'Parque',             color: '#2E9E5B', glyph: 'tree',     near: true },
  cultura:    { label: 'Cultura y turismo',     one: 'Cultura',            color: '#A33FD1', glyph: 'ticket',   near: true },
  bancos:     { label: 'Bancos y cajeros',      one: 'Banco',              color: '#4D7C6B', glyph: 'bank',     near: true },
  belleza:    { label: 'Peluquerías y belleza', one: 'Peluquería',         color: '#E0609A', glyph: 'scissors', near: true },
  talleres:   { label: 'Talleres',              one: 'Taller',             color: '#6B7280', glyph: 'wrench',   near: true },
  transporte: { label: 'Transporte',            one: 'Transporte',         color: '#4B5B7C', glyph: 'bus',      near: true },
  educacion:  { label: 'Educación',             one: 'Centro educativo',   color: '#8C6E4E', glyph: 'school',   near: false },
  publico:    { label: 'Servicios públicos',    one: 'Servicio público',   color: '#64748B', glyph: 'landmark', near: false },
  culto:      { label: 'Lugares de culto',      one: 'Lugar de culto',     color: '#7C7C8A', glyph: 'church',   near: false },
};
// La subclase manda sobre la clase (p. ej. class "fuel" + subclass "charging_station" → carga eléctrica).
const SUB_CAT = {
  charging_station: 'carga', nightclub: 'bares', food_court: 'rapida',
  supermarket: 'super', marketplace: 'super', greengrocer: 'super', butcher: 'super', frozen_food: 'super', convenience: 'super',
  bakery: 'cafe', ice_cream: 'cafe', hairdresser: 'belleza', beauty: 'belleza', veterinary: 'salud',
  car_repair: 'talleres', car_parts: 'talleres', bus_station: 'transporte', taxi: 'transporte',
  post_office: 'publico', books: 'tiendas', library: 'cultura', visitor_centre: 'cultura', laundry: 'tiendas',
  guest_house: 'hoteles', hotel: 'hoteles', mall: 'tiendas',
};
const CLASS_CAT = {
  restaurant: 'comer', fast_food: 'rapida', cafe: 'cafe', bakery: 'cafe', ice_cream: 'cafe', bar: 'bares', beer: 'bares',
  grocery: 'super', butcher: 'super', shop: 'tiendas', clothing_store: 'tiendas', alcohol_shop: 'tiendas', laundry: 'tiendas',
  fuel: 'gasolina', parking: 'parking', bicycle_parking: 'parking', lodging: 'hoteles', campsite: 'hoteles',
  pharmacy: 'farmacia', hospital: 'salud', doctors: 'salud', dentist: 'salud', veterinary: 'salud',
  sports_centre: 'deporte', pitch: 'deporte', stadium: 'deporte', swimming_pool: 'deporte', golf: 'deporte',
  athletics: 'deporte', sailing: 'deporte', fitness: 'deporte', fitness_centre: 'deporte',
  park: 'parques', garden: 'parques', playground: 'parques',
  museum: 'cultura', art_gallery: 'cultura', attraction: 'cultura', castle: 'cultura', monument: 'cultura',
  cinema: 'cultura', theatre: 'cultura', library: 'cultura', information: 'cultura',
  bank: 'bancos', atm: 'bancos', hairdresser: 'belleza', car: 'talleres',
  ferry_terminal: 'transporte', harbor: 'transporte', railway: 'transporte', bus: 'transporte',
  school: 'educacion', college: 'educacion', university: 'educacion', kindergarten: 'educacion',
  town_hall: 'publico', police: 'publico', post: 'publico', fire_station: 'publico', place_of_worship: 'culto',
};
const HIDDEN_SUB = ['bus_stop', 'office']; // demasiados (128 paradas en Sanlúcar) o sin interés para la app
const poiCatOf = p => (HIDDEN_SUB.includes(p.subclass) ? '' : (SUB_CAT[p.subclass] || CLASS_CAT[p.class] || ''));

// Expresión de estilo equivalente a poiCatOf, para que MapLibre calcule la categoría dentro del mapa
function matchExpr(input, obj, fallback) {
  const groups = {};
  for (const [k, v] of Object.entries(obj)) (groups[v] ||= []).push(k);
  const e = ['match', input];
  for (const [cat, keys] of Object.entries(groups)) e.push(keys, cat);
  e.push(fallback);
  return e;
}
const CAT_EXPR = ['match', ['get', 'subclass'], HIDDEN_SUB, '', matchExpr(['get', 'subclass'], SUB_CAT, matchExpr(['get', 'class'], CLASS_CAT, ''))];

// Tipos de negocio con precios (los de datos-lugares.js): categoría de mapa e icono del marcador
const PLACE_POICAT = {
  Restaurante: 'comer', Taberna: 'comer', 'Comida rápida': 'rapida', 'Cafetería': 'cafe', 'Heladería': 'cafe', Bar: 'bares', Pub: 'bares', Discoteca: 'bares',
  'Centro deportivo': 'deporte', 'Campo de fútbol': 'deporte', 'Pista de pádel': 'deporte', 'Pista de tenis': 'deporte', 'Recinto deportivo': 'deporte',
  Piscina: 'deporte', 'Club de golf': 'deporte', 'Club náutico': 'deporte',
  Cine: 'cultura', Teatro: 'cultura', Museo: 'cultura', 'Visita turística': 'cultura', Monumento: 'cultura', Aparcamiento: 'parking',
  'Peluquería': 'belleza', 'Centro de belleza': 'belleza', 'Clínica dental': 'salud', 'Clínica médica': 'salud', Veterinario: 'salud',
  Taller: 'talleres', 'Lavado de coches': 'talleres', 'Lavandería': 'tiendas', Autoescuela: 'educacion',
};
const PLACE_GLYPH = { 'Campo de fútbol': 'ball', 'Pista de pádel': 'ball', 'Pista de tenis': 'ball', 'Recinto deportivo': 'ball', Monumento: 'landmark', Discoteca: 'martini' };
// Conceptos que se sugieren para cada tipo de negocio (servicio local y ocio). [nombre, unidad]
// Ocio: alquiler de espacios con precio TOTAL (por hora); actividades y entradas POR PERSONA.
const PLACE_CONCEPTS = {
  'Peluquería': [['Corte de pelo', 'servicio'], ['Corte + barba', 'servicio'], ['Tinte', 'servicio']],
  'Centro de belleza': [['Manicura', 'servicio'], ['Depilación piernas', 'servicio'], ['Limpieza facial', 'servicio']],
  'Clínica dental': [['Revisión', 'servicio'], ['Limpieza dental', 'servicio'], ['Empaste', 'servicio']],
  'Clínica médica': [['Consulta general', 'sesión'], ['Consulta con especialista', 'sesión']],
  Veterinario: [['Consulta', 'servicio'], ['Vacuna', 'servicio']],
  Taller: [['Cambio de aceite', 'servicio'], ['Revisión pre-ITV', 'servicio'], ['Mano de obra', 'h']],
  'Lavado de coches': [['Lavado exterior', 'servicio'], ['Lavado completo', 'servicio']],
  'Lavandería': [['Lavadora', 'servicio'], ['Secadora', 'servicio']],
  Autoescuela: [['Clase práctica', 'sesión'], ['Matrícula', 'servicio']],
  'Centro deportivo': [['Pista de pádel', 'h'], ['Pista polideportiva', 'h'], ['Entrada a la piscina', 'persona']],
  'Campo de fútbol': [['Campo completo', 'h'], ['Medio campo', 'h']],
  'Pista de pádel': [['Pista', 'h'], ['Pista con luz', 'h']],
  'Pista de tenis': [['Pista', 'h'], ['Pista con luz', 'h']],
  'Recinto deportivo': [['Pista', 'h'], ['Entrada', 'persona']],
  Piscina: [['Entrada adulto', 'persona'], ['Entrada infantil', 'persona']],
  'Club de golf': [['Green fee 18 hoyos', 'persona'], ['Clase', 'sesión']],
  'Club náutico': [['Curso de vela', 'persona'], ['Alquiler de kayak', 'h']],
  Cine: [['Entrada general', 'persona'], ['Día del espectador', 'persona']],
  Teatro: [['Entrada general', 'persona']],
  Museo: [['Entrada general', 'persona'], ['Entrada reducida', 'persona']],
  'Visita turística': [['Entrada general', 'persona'], ['Visita guiada', 'persona']],
  Monumento: [['Entrada general', 'persona'], ['Visita guiada', 'persona']],
  Aparcamiento: [['Primera hora', 'h'], ['Día completo', 'día']],
};

const glyphSVG = (name, cls = 'ic') => `<svg class="${cls}" viewBox="0 0 24 24">${GLYPH[name] || GLYPH.pin}</svg>`;
const poiIconHTML = cat => {
  const c = POI_CATS[cat];
  return c ? `<i class="poi-ic" style="background:${c.color}">${glyphSVG(c.glyph)}</i>` : `<i class="poi-ic addr">${glyphSVG('pin')}</i>`;
};

// Aclara u oscurece un color (para que el texto de las etiquetas se lea en cada modo)
function shade(hex, f) {
  const n = parseInt(hex.slice(1), 16), ch = [n >> 16, (n >> 8) & 255, n & 255];
  const out = ch.map(v => Math.round(f < 0 ? v * (1 + f) : v + (255 - v) * f));
  return '#' + out.map(v => v.toString(16).padStart(2, '0')).join('');
}
const poiTextColor = theme => ['match', CAT_EXPR, ...Object.entries(POI_CATS).flatMap(([k, c]) => [k, shade(c.color, theme === 'dark' ? 0.35 : -0.3)]), '#666'];

// ---------- Capa de puntos de interés integrada en el mapa ----------
async function loadPoiIcons(map) {
  const jobs = Object.entries(POI_CATS).map(([k, c]) => new Promise(res => {
    if (map.hasImage(`poi-${k}`)) return res();
    const s = `<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 48 48">
      <circle cx="24" cy="24" r="21" fill="${c.color}" stroke="#fff" stroke-width="3"/>
      <g transform="translate(12 12)" fill="none" stroke="#fff" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">${GLYPH[c.glyph]}</g></svg>`;
    const img = new Image(48, 48);
    img.onload = () => { if (!map.hasImage(`poi-${k}`)) map.addImage(`poi-${k}`, img, { pixelRatio: 2 }); res(); };
    img.onerror = res;
    img.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(s);
  }));
  await Promise.all(jobs);
}
function addPoiLayer(map, src, theme) {
  // misma fuente que el resto de etiquetas del mapa, en redonda (la primera que aparece es la cursiva de los ríos)
  const fonts = map.getStyle().layers.map(l => l.layout?.['text-font']).filter(f => Array.isArray(f) && typeof f[0] === 'string');
  const font = fonts.find(f => /Regular/.test(f[0])) || fonts[0] || ['Noto Sans Regular'];
  map.addLayer({
    id: 'poi-app', type: 'symbol', source: src, 'source-layer': 'poi', minzoom: 15,
    filter: ['!=', CAT_EXPR, ''],
    layout: {
      'icon-image': ['concat', 'poi-', CAT_EXPR], 'icon-size': 1, 'icon-padding': 2,
      'text-field': ['coalesce', ['get', 'name:es'], ['get', 'name']], 'text-font': font, 'text-size': 12,
      'text-anchor': 'left', 'text-justify': 'left', 'text-offset': [1.15, 0], 'text-max-width': 8,
      'text-optional': true, 'symbol-sort-key': ['get', 'rank'],
    },
    paint: { 'text-color': poiTextColor(theme), 'text-halo-color': theme === 'dark' ? '#0F1424' : '#FFFFFF', 'text-halo-width': 1.5 },
  });
}
function setPoiLayerCat(map, cat) {
  if (!map?.getLayer('poi-app')) return;
  map.setFilter('poi-app', cat ? ['==', CAT_EXPR, cat] : ['!=', CAT_EXPR, '']);
}
function poiToSpot(f) {
  const p = f.properties, cat = poiCatOf(p), pos = f.geometry.coordinates.slice(0, 2);
  const name = p['name:es'] || p.name || POI_CATS[cat]?.one || 'Lugar';
  return { kind: 'poi', key: `poi:${name}:${pos[0].toFixed(5)},${pos[1].toFixed(5)}`, name, pos, cat, sub: POI_CATS[cat]?.one || '' };
}
// Puntos de interés de una categoría en las teselas cargadas, ordenados por distancia (sin repetidos)
function poisNear(map, src, cat, limit = 40) {
  if (!map || !src) return [];
  const seen = new Set(), out = [];
  for (const f of map.querySourceFeatures(src, { sourceLayer: 'poi' })) {
    if (f.geometry.type !== 'Point') continue;
    const c = poiCatOf(f.properties); if (!c || (cat && c !== cat)) continue;
    const s = poiToSpot(f); if (seen.has(s.key) || !f.properties.name) continue;
    seen.add(s.key); out.push(s);
  }
  return out.sort((a, b) => distM(USER_POS, a.pos) - distM(USER_POS, b.pos)).slice(0, limit);
}
function poisByName(map, src, q, limit = 6) {
  if (!map || !src || q.length < 2) return [];
  const n = normTxt(q);
  return poisNear(map, src, null, 400).filter(s => normTxt(s.name).includes(n)).slice(0, limit);
}
const normTxt = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();

// ---------- Direcciones (Photon) ----------
function photonToSpot(f, kind = 'addr') {
  const p = f.properties, pos = f.geometry.coordinates;
  const street = [p.street, p.housenumber].filter(Boolean).join(' ');
  // con número de portal, el título es la dirección (lo que el usuario buscó); si no, el nombre del sitio
  const name = (p.housenumber && street) || p.name || street || p.city || p.county || 'Dirección';
  const extra = name === street ? p.name : street;
  const country = /^(España|Spain)$/i.test(p.country || '') ? '' : p.country;
  const sub = [extra, p.postcode, p.city || p.county, country].filter(Boolean).join(', ');
  return { kind, key: `${kind}:${pos[0].toFixed(5)},${pos[1].toFixed(5)}`, name, sub, pos };
}
async function searchAddresses(q, signal) {
  const u = `https://photon.komoot.io/api/?q=${encodeURIComponent(q)}&lat=${USER_POS[1]}&lon=${USER_POS[0]}&limit=6`;
  const r = await fetch(u, { signal });
  if (!r.ok) throw new Error('photon ' + r.status);
  return (await r.json()).features.map(f => photonToSpot(f));
}
async function reverseGeocode(pos) {
  const r = await fetch(`https://photon.komoot.io/reverse?lon=${pos[0]}&lat=${pos[1]}&limit=1`);
  const f = (await r.json()).features?.[0];
  return f ? photonToSpot(f) : null;
}

// ---------- Ubicaciones compartidas (WhatsApp, Google Maps, Apple Maps, geo:, coordenadas) ----------
// WhatsApp comparte la ubicación como enlace de Google Maps (…maps.google.com/?q=LAT,LNG…).
function parseLocation(text) {
  if (!text) return null;
  const t = decodeURIComponent(String(text)).replace(/\s+/g, ' ');
  const ok = (lat, lng) => Math.abs(lat) <= 90 && Math.abs(lng) <= 180 ? { pos: [lng, lat] } : null;
  let m = t.match(/!3d(-?\d+\.\d+)!4d(-?\d+\.\d+)/);                       // Google Maps (enlace largo)
  if (m) return ok(+m[1], +m[2]);
  m = t.match(/(-?\d{1,2}\.\d{3,})\s*,\s*(-?\d{1,3}\.\d{3,})/);           // q=, @, ll=, geo:, "36.77, -6.35"
  if (m) return ok(+m[1], +m[2]);
  return null;
}
// Los enlaces cortos (maps.app.goo.gl) solo se pueden resolver desde un servidor o desde la app nativa.
const isShortMapLink = t => /maps\.app\.goo\.gl|goo\.gl\/maps|maps\.apple\/p\//i.test(t || '');

// ---------- Rutas por calles reales (OSRM) ----------
const ROUTERS = {
  coche: 'https://router.project-osrm.org/route/v1/driving/',            // demo pública de OSRM (solo coche)
  pie: 'https://routing.openstreetmap.de/routed-foot/route/v1/driving/', // servidor de FOSSGIS para ir a pie
};
const routeCache = new Map();
function cumDist(coords) { const c = [0]; for (let i = 1; i < coords.length; i++) c.push(c[i - 1] + distM(coords[i - 1], coords[i])); return c; }
function nearestIdx(coords, p) { let bi = 0, bd = Infinity; coords.forEach((c, i) => { const d = distM(c, p); if (d < bd) { bd = d; bi = i; } }); return bi; }
function fallbackRoute(a, b, mode) { // ruta en "L" si no hay conexión con el servidor de rutas
  const mid = [a[0], b[1]], coords = [a, mid, b], cum = cumDist(coords);
  const cross = (mid[0] - a[0]) * (b[1] - mid[1]) - (mid[1] - a[1]) * (b[0] - mid[0]);
  return { coords, cum, total: cum[2], duration: cum[2] / SPEED[mode] * 60, real: false,
    steps: [{ at: cum[1], type: 'turn', modifier: cross < 0 ? 'right' : 'left', name: '' }, { at: cum[2], type: 'arrive', name: '' }] };
}
async function getRoute(a, b, mode) {
  const key = `${a.join()}|${b.join()}|${mode}`;
  if (routeCache.has(key)) return routeCache.get(key);
  const p = (async () => {
    try {
      const ctl = new AbortController(), to = setTimeout(() => ctl.abort(), 7000);
      const url = `${ROUTERS[mode]}${a[0]},${a[1]};${b[0]},${b[1]}?overview=full&geometries=geojson&steps=true`;
      const j = await (await fetch(url, { signal: ctl.signal })).json();
      clearTimeout(to);
      const r = j.routes?.[0]; if (!r) throw new Error(j.code || 'sin ruta');
      const coords = [a, ...r.geometry.coordinates, b], cum = cumDist(coords);
      const steps = r.legs[0].steps.map(s => ({
        at: cum[nearestIdx(coords, s.maneuver.location)], type: s.maneuver.type, modifier: s.maneuver.modifier, exit: s.maneuver.exit, name: s.name,
      }));
      steps[steps.length - 1].at = cum[cum.length - 1];
      return { coords, cum, total: cum[cum.length - 1], duration: r.duration, steps, real: true };
    } catch (e) {
      console.warn('Ruta aproximada (sin servidor de rutas):', e.message);
      routeCache.delete(key); // se reintentará la próxima vez
      return fallbackRoute(a, b, mode);
    }
  })();
  p.then(v => { if (v.real) p.done = v; }); // para consultar la ruta ya resuelta sin esperar
  routeCache.set(key, p);
  return p;
}
const cachedRoute = (a, b, mode) => routeCache.get(`${a.join()}|${b.join()}|${mode}`);
function positionAt(r, d) {
  const { coords, cum } = r;
  if (d <= 0) return coords[0];
  for (let i = 1; i < coords.length; i++) {
    if (cum[i] >= d) { const f = (d - cum[i - 1]) / ((cum[i] - cum[i - 1]) || 1); return [coords[i - 1][0] + (coords[i][0] - coords[i - 1][0]) * f, coords[i - 1][1] + (coords[i][1] - coords[i - 1][1]) * f]; }
  }
  return coords[coords.length - 1];
}
function stepText(s) {
  const on = s.name ? ` por ${s.name}` : '';
  const side = m => (m || '').includes('left') ? 'izquierda' : 'derecha';
  switch (s.type) {
    case 'arrive': return 'Llegarás a tu destino';
    case 'roundabout': case 'rotary': case 'roundabout turn': case 'exit roundabout': case 'exit rotary':
      return `En la rotonda, toma la ${s.exit || 1}.ª salida${on}`;
    case 'merge': return `Incorpórate${on}`;
    case 'fork': return `Mantente a la ${side(s.modifier)}${on}`;
  }
  const m = s.modifier || 'straight';
  if (m === 'straight') return `Sigue recto${on}`;
  if (m === 'uturn') return `Cambia de sentido${on}`;
  if (m.startsWith('slight')) return `Gira ligeramente a la ${side(m)}${on}`;
  if (m.startsWith('sharp')) return `Gira bruscamente a la ${side(m)}${on}`;
  return `Gira a la ${side(m)}${on}`;
}
function stepIcon(s) {
  if (s.type === 'arrive') return 'arrive';
  if (/roundabout|rotary/.test(s.type)) return 'round';
  const m = s.modifier || '';
  if (m === 'uturn' || m.includes('left')) return 'left';
  if (m.includes('right')) return 'right';
  return 'straight';
}
const lerpAngle = (a, b, t) => { const d = ((b - a + 540) % 360) - 180; return a + d * t; };
