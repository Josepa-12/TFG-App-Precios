/* PrecioLocal — prototipo navegable (TFG)
 * Datos inventados. Mapa real: OpenFreeMap + MapLibre, recoloreado con las paletas B (claro) y D (oscuro).
 * Interfaz principal inspirada en Mapas de Apple (dock de cristal con buscador + avatar) y Waze (botón amarillo).
 */

// ---------- Datos de ejemplo (establecimientos inventados en Sanlúcar de Barrameda) ----------
const USER_POS = [-6.3528, 36.7779]; // Plaza del Cabildo (posición simulada)
const HOME = { center: [-6.3515, 36.7790], zoom: 14.9, pitch: 50, bearing: -15 }; // vista inicial

const TPL = {
  servicio:   { label: 'Servicio local', menu: 'Servicios locales', ratings: false },
  hosteleria: { label: 'Hostelería',     menu: 'Hostelería',        ratings: ['Calidad', 'Servicio', 'Ambiente'] },
  ocio:       { label: 'Ocio',           menu: 'Ocio',              ratings: ['Instalaciones', 'Atención', 'Calidad-precio'] },
};

const PLACES = [
  { id: 'fisio', tpl: 'servicio', cat: 'Fisioterapia', name: 'Fisio Guadalquivir', addr: 'Calle Ancha', pos: [-6.3561, 36.7766],
    items: [{ n: 'Sesión (60 min)', p: 35, u: 'sesión' }, { n: 'Primera visita', p: 45, u: 'sesión' }],
    src: { by: 'usuarios', n: 8, months: 3 } },
  { id: 'psico', tpl: 'servicio', cat: 'Psicología', name: 'Consulta Psicología Atlántica', addr: 'Calle San Juan', pos: [-6.3497, 36.7761], sensitive: true,
    items: [{ n: 'Sesión individual', p: 50, u: 'sesión' }],
    src: { by: 'negocio' } },
  { id: 'barber', tpl: 'servicio', cat: 'Barbería', name: 'Barbería El Faro', addr: 'Calle Bolsa', pos: [-6.3546, 36.7793],
    items: [{ n: 'Corte', p: 12, u: 'servicio' }, { n: 'Corte + barba', p: 18, u: 'servicio' }],
    src: { by: 'usuarios', n: 14, months: 2 } },
  { id: 'marea', tpl: 'hosteleria', cat: 'Restaurante', name: 'Casa Marea', addr: 'Bajo de Guía', pos: [-6.3478, 36.7860],
    range: [20, 30], ratings: [4.5, 4.2, 4.7], nRatings: 23, src: { by: 'usuarios', n: 31, months: 3 } },
  { id: 'azotea', tpl: 'hosteleria', cat: 'Bar de copas', name: 'Azotea Poniente', addr: 'Calle Real', pos: [-6.3506, 36.7801],
    range: [7, 12], ratings: [4.0, 3.6, 4.6], nRatings: 11, src: { by: 'usuarios', n: 12, months: 3 } },
  { id: 'marisma', tpl: 'ocio', cat: 'Polideportivo', name: 'Centro Deportivo Marisma', addr: 'Avda. del Puerto', pos: [-6.3445, 36.7748],
    items: [
      { n: 'Pista fútbol 7 A', p: 12, u: 'h', note: 'precio de la pista entera' },
      { n: 'Pista fútbol 7 B', p: 10, u: 'h', note: 'precio de la pista entera' },
      { n: 'Pista pádel 1', p: 8, u: 'h' },
      { n: 'Pista pádel 1', v: 'con luz', p: 10, u: 'h' },
    ],
    ratings: [3.9, 4.3, 4.4], nRatings: 17, src: { by: 'usuarios', n: 6, months: 3 } },
  { id: 'enigma', tpl: 'ocio', cat: 'Escape room', name: 'Enigma del Galeón', addr: 'Calle Trasbolsa', pos: [-6.3582, 36.7742],
    items: [{ n: 'Sala "El Galeón"', p: 15, u: 'persona' }, { n: 'Sala "Doñana"', p: 17, u: 'persona' }],
    ratings: [4.6, 4.8, 4.1], nRatings: 9, src: { by: 'negocio' } },
];
const TYPES = [['Restaurantes', 'Restaurante'], ['Bares de copas', 'Bar de copas'], ['Fisioterapia', 'Fisioterapia'], ['Psicología', 'Psicología'], ['Barberías', 'Barbería'], ['Pistas', 'Polideportivo'], ['Escape rooms', 'Escape room']];

const SHOP = [
  { id: 'flecha-azul',   kind: 'flecha', name: 'Posición clásica',  cost: 0 },
  { id: 'flecha-dorada', kind: 'flecha', name: 'Posición dorada',   cost: 150 },
  { id: 'flecha-neon',   kind: 'flecha', name: 'Posición neón',     cost: 200 },
  { id: 'ruta-solida',   kind: 'ruta',   name: 'Ruta clásica',      cost: 0 },
  { id: 'ruta-discont',  kind: 'ruta',   name: 'Ruta discontinua',  cost: 100 },
  { id: 'ruta-dorada',   kind: 'ruta',   name: 'Ruta dorada',       cost: 250 },
];

const XP_PER_LEVEL = 150;
const REWARD = { confirm: { c: 10, xp: 15 }, newPrice: { c: 15, xp: 20 }, rating: { c: 5, xp: 10 } };
const SPEED = { pie: 80, coche: 350 }; // metros por minuto para la ETA

// ---------- Estado ----------
const S = {
  coins: 240, xp: 340, contributions: 12, confirmed: 9,
  owned: ['flecha-azul', 'ruta-solida'], equipped: { flecha: 'flecha-azul', ruta: 'ruta-solida' },
  filter: 'todos', place: null, navigating: null, flow: null, themePref: 'auto', page: null,
  pending: [{ id: 'barber', when: 'ayer' }],            // visitas con "Más tarde" sin responder
  favs: ['marea', 'fisio', 'marisma'],
  alerts: ['marea'],                                   // "Avísame si cambia"
  visits: [{ id: 'barber', when: 'ayer', n: 3 }, { id: 'azotea', when: 'hace 5 días', n: 4 }, { id: 'enigma', when: 'el mes pasado', n: 1 }],
  history: [
    { id: 'azotea', what: 'Gasto 7–12 €', when: 'hace 5 días' },
    { id: 'fisio', what: 'Confirmó 35 €', when: 'hace 2 semanas' },
    { id: 'marisma', what: 'Pista pádel 1 · 8 €', when: 'hace 3 semanas' },
  ],
  settings: { buildings: true, reduceMotion: false, voice: true, mode: 'pie', notifPending: true, notifAlerts: true, anonAll: false },
};
const level = xp => Math.floor(xp / XP_PER_LEVEL) + 1;

// ---------- Utilidades ----------
const $ = s => document.querySelector(s);
const $$ = s => [...document.querySelectorAll(s)];
const store = {
  get(k) { try { return localStorage.getItem(k); } catch { return null; } },
  set(k, v) { try { localStorage.setItem(k, v); } catch { /* sin almacenamiento */ } },
};
const svg = (d, cls = 'ic') => `<svg class="${cls}" viewBox="0 0 24 24">${d}</svg>`;
const ICON = {
  x: svg('<path d="M18 6 6 18M6 6l12 12"/>'),
  users: svg('<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/>', 'ic sm'),
  building: svg('<path d="M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18Z"/><path d="M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2"/>', 'ic sm'),
  shield: svg('<path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"/>'),
  nav: svg('<polygon points="3 11 22 2 13 21 11 13 3 11"/>'),
  star: '<svg viewBox="0 0 24 24"><path d="M11.525 2.295a.53.53 0 0 1 .95 0l2.31 4.679a2.123 2.123 0 0 0 1.595 1.16l5.166.756a.53.53 0 0 1 .294.904l-3.736 3.638a2.123 2.123 0 0 0-.611 1.878l.882 5.14a.53.53 0 0 1-.771.56l-4.618-2.428a2.122 2.122 0 0 0-1.973 0L6.396 21.01a.53.53 0 0 1-.77-.56l.881-5.139a2.122 2.122 0 0 0-.611-1.879L2.16 9.795a.53.53 0 0 1 .294-.906l5.165-.755a2.122 2.122 0 0 0 1.597-1.16z"/></svg>',
  check: svg('<path d="M20 6 9 17l-5-5"/>'),
  award: svg('<circle cx="12" cy="8" r="6"/><path d="M15.477 12.89 17 22l-5-3-5 3 1.523-9.11"/>'),
  lock: svg('<rect width="18" height="11" x="3" y="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>'),
  heart: svg('<path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/>'),
  bell: svg('<path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10.3 21a1.94 1.94 0 0 0 3.4 0"/>'),
  clock: svg('<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>', 'ic sm'),
  pin: svg('<path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/>', 'ic sm'),
  back: svg('<path d="m15 18-6-6 6-6"/>'),
  chev: svg('<path d="m9 18 6-6-6-6"/>', 'ic chev'),
  food: svg('<path d="M3 2v7c0 1.1.9 2 2 2h4a2 2 0 0 0 2-2V2M7 2v20M21 15V2a5 5 0 0 0-5 5v6c0 1.1.9 2 2 2h3Zm0 0v7"/>'),
  ball: svg('<circle cx="12" cy="12" r="10"/><path d="m12 7 4 3-1.5 5h-5L8 10Z"/>'),
  hand: svg('<path d="M11 12h2a2 2 0 1 0 0-4h-3c-.6 0-1.1.2-1.4.6L3 14"/><path d="m7 18 1.6-1.4c.3-.4.8-.6 1.4-.6h4c1.1 0 2.1-.4 2.8-1.2l4.6-4.4a2 2 0 0 0-2.75-2.91l-4.2 3.9"/><path d="m2 13 6 6"/>'),
  pending: svg('<circle cx="12" cy="12" r="10"/><path d="M12 8v4M12 16h.01"/>'),
  list: svg('<path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/>'),
  store: svg('<path d="m2 7 4.4-4.4A2 2 0 0 1 7.8 2h8.4a2 2 0 0 1 1.4.6L22 7M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8M2 7h20v3a2 2 0 0 1-4 0 2 2 0 0 1-4 0 2 2 0 0 1-4 0 2 2 0 0 1-4 0 2 2 0 0 1-4 0Z"/>'),
  palette: svg('<circle cx="13.5" cy="6.5" r="1.5"/><circle cx="17.5" cy="10.5" r="1.5"/><circle cx="8.5" cy="7.5" r="1.5"/><circle cx="6.5" cy="12.5" r="1.5"/><path d="M12 2a10 10 0 0 0 0 20c.93 0 1.5-.75 1.5-1.66 0-.43-.18-.82-.44-1.12-.3-.3-.45-.67-.45-1.12a1.64 1.64 0 0 1 1.67-1.67h2A5.56 5.56 0 0 0 22 10.92C22 6 17.5 2 12 2Z"/>'),
  route: svg('<circle cx="6" cy="19" r="3"/><path d="M9 19h8.5a3.5 3.5 0 0 0 0-7h-11a3.5 3.5 0 0 1 0-7H15"/><circle cx="18" cy="5" r="3"/>'),
  access: svg('<circle cx="16" cy="4" r="1"/><path d="m18 19 1-7-6 1M5 8l3-3 5.5 3-2.36 3.5M4.24 14.5a5 5 0 0 0 6.88 6M13.76 17.5a5 5 0 0 0-6.88-6"/>'),
  globe: svg('<circle cx="12" cy="12" r="10"/><path d="M2 12h20M12 2a15 15 0 0 1 0 20 15 15 0 0 1 0-20"/>'),
  help: svg('<circle cx="12" cy="12" r="10"/><path d="M9.1 9a3 3 0 0 1 5.8 1c0 2-3 3-3 3M12 17h.01"/>'),
  info: svg('<circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/>'),
  out: svg('<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9"/>'),
  trash: svg('<path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>'),
};
const UNIT = { 'sesión': '/sesión', h: '/h', persona: '/persona', servicio: '' };
const unitText = { 'sesión': 'por sesión', h: 'por hora', persona: 'por persona', servicio: '' };

function distM(a, b) {
  const R = 6371000, toR = d => d * Math.PI / 180;
  const dLat = toR(b[1] - a[1]), dLon = toR(b[0] - a[0]);
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(toR(a[1])) * Math.cos(toR(b[1])) * Math.sin(dLon / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(h));
}
const fmtDist = m => m < 1000 ? `${Math.round(m / 10) * 10} m` : `${(m / 1000).toFixed(1).replace('.', ',')} km`;
const bearing = (a, b) => Math.atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI;
const placeById = id => PLACES.find(p => p.id === id);
const tplIcon = p => p.tpl === 'hosteleria' ? ICON.food : p.tpl === 'ocio' ? ICON.ball : ICON.hand;

function pinLabel(p) {
  if (p.tpl === 'hosteleria') return `${p.range[0]}–${p.range[1]} €`;
  if (p.items.length === 1 || p.tpl === 'servicio') return `${p.items[0].p} €`;
  return `desde ${Math.min(...p.items.map(i => i.p))} €`;
}
function srcHTML(src) {
  return src.by === 'usuarios'
    ? `<div class="src">${ICON.users} Según ${src.n} usuarios · últimos ${src.months} meses</div>`
    : `<div class="src declared">${ICON.building} Declarado por el negocio · pendiente de confirmar por clientes</div>`;
}

let toastT;
function toast(msg) {
  const t = $('#toast'); t.textContent = msg; t.hidden = false;
  clearTimeout(toastT); toastT = setTimeout(() => (t.hidden = true), 2400);
}
let notifT;
function notify(title, body, placeId) {
  const n = $('#notif');
  n.innerHTML = `<span class="n-ic">${ICON.bell}</span><div><b>${title}</b><span>${body}</span></div>`;
  n.dataset.place = placeId || ''; n.hidden = false;
  clearTimeout(notifT); notifT = setTimeout(() => (n.hidden = true), 5000);
}

// ---------- Tema ----------
const MAP_THEME = {
  light: { bg: '#E9EDF7', water: '#BFD0F4', green: '#DCE6EE', road: '#FFFFFF', roadMinor: '#F7F9FE', casing: '#C9D4EE', building: '#D3DCF2', label: '#4B5878', halo: '#FFFFFF' },
  dark:  { bg: '#141B30', water: '#0A0F1F', green: '#18213A', road: '#2A3554', roadMinor: '#222C48', casing: '#0F1424', building: '#2A3A63', label: '#9AA6C4', halo: '#0F1424' },
};
function currentTheme() {
  if (S.themePref !== 'auto') return S.themePref;
  return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}
function applyTheme() {
  const t = currentTheme();
  document.documentElement.dataset.theme = t;
  if (map && map.isStyleLoaded()) styleMap(t);
}

// ---------- Mapa ----------
let map, userMarker;
const markers = {};

function styleMap(t) {
  const c = MAP_THEME[t];
  for (const l of map.getStyle().layers) {
    const sl = l['source-layer'], id = l.id;
    try {
      if (id === 'edificios-3d' || id.startsWith('ruta')) continue;
      if (l.type === 'fill' && l.paint && l.paint['fill-pattern']) { map.setLayoutProperty(id, 'visibility', 'none'); continue; } // tramas rayadas
      if (l.type === 'background') map.setPaintProperty(id, 'background-color', c.bg);
      else if (sl === 'water') {
        if (l.type === 'fill') map.setPaintProperty(id, 'fill-color', c.water);
        if (l.type === 'line') map.setPaintProperty(id, 'line-color', c.water);
      } else if (sl === 'waterway' && l.type === 'line') map.setPaintProperty(id, 'line-color', c.water);
      else if ((sl === 'landcover' || sl === 'landuse' || sl === 'park') && l.type === 'fill') {
        map.setPaintProperty(id, 'fill-color', c.green);
      } else if (sl === 'transportation' && l.type === 'line') {
        const minor = /minor|service|track|path|pedestrian/.test(id);
        map.setPaintProperty(id, 'line-color', id.includes('casing') ? c.casing : (minor ? c.roadMinor : c.road));
      } else if (sl === 'building') map.setLayoutProperty(id, 'visibility', 'none');
      else if (sl === 'poi') map.setLayoutProperty(id, 'visibility', 'none'); // ocultar negocios reales
      else if (l.type === 'symbol') {
        map.setPaintProperty(id, 'text-color', c.label);
        map.setPaintProperty(id, 'text-halo-color', c.halo);
      }
    } catch { /* capa sin esa propiedad */ }
  }
  if (map.getLayer('edificios-3d')) map.setPaintProperty('edificios-3d', 'fill-extrusion-color', c.building);
  styleRoute();
}

function addBuildings() {
  const style = map.getStyle();
  const src = Object.keys(style.sources).find(k => style.sources[k].type === 'vector');
  const firstSymbol = style.layers.find(l => l.type === 'symbol')?.id;
  map.addLayer({
    id: 'edificios-3d', type: 'fill-extrusion', source: src, 'source-layer': 'building', minzoom: 14,
    layout: { visibility: S.settings.buildings ? 'visible' : 'none' },
    paint: {
      'fill-extrusion-color': MAP_THEME[currentTheme()].building,
      'fill-extrusion-height': ['coalesce', ['get', 'render_height'], 8],
      'fill-extrusion-base': ['coalesce', ['get', 'render_min_height'], 0],
      'fill-extrusion-opacity': 0.9,
    },
  }, firstSymbol);
}

function addRouteLayers() {
  map.addSource('ruta', { type: 'geojson', data: { type: 'FeatureCollection', features: [] } });
  map.addLayer({ id: 'ruta-borde', type: 'line', source: 'ruta', layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-color': '#FFFFFF', 'line-width': 11 } });
  map.addLayer({ id: 'ruta-linea', type: 'line', source: 'ruta', layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-color': '#2F5BEA', 'line-width': 7 } });
}
function styleRoute() {
  if (!map.getLayer('ruta-linea')) return;
  const t = currentTheme(), r = S.equipped.ruta;
  const color = r === 'ruta-dorada' ? '#FFC93C' : (t === 'dark' ? '#4C7DFF' : '#2F5BEA');
  map.setPaintProperty('ruta-linea', 'line-color', color);
  map.setPaintProperty('ruta-linea', 'line-dasharray', r === 'ruta-discont' ? [1.2, 1.2] : [1, 0]);
  map.setPaintProperty('ruta-borde', 'line-color', t === 'dark' ? '#0F1424' : '#FFFFFF');
}

function initMap() {
  if (!window.maplibregl) { $('#mapError').hidden = false; return; }
  map = new maplibregl.Map({
    container: 'map', style: 'https://tiles.openfreemap.org/styles/liberty',
    center: HOME.center, zoom: HOME.zoom, pitch: HOME.pitch, bearing: HOME.bearing, attributionControl: false,
  });
  map.addControl(new maplibregl.AttributionControl({ compact: true }), 'bottom-left');
  map.on('error', e => console.warn('Mapa:', e.error?.message));
  map.on('load', () => {
    addBuildings(); addRouteLayers(); styleMap(currentTheme());
    // La atribución compacta empieza desplegada y tapa el botón €: se pliega (sigue accesible con la "i")
    $$('.maplibregl-ctrl-attrib').forEach(a => { a.classList.remove('maplibregl-compact-show'); a.removeAttribute('open'); });
    window.__mapReady = true;
  });
  map.on('click', () => { closeCatMenu(); if ($('#dock').classList.contains('open')) closeSearch(); });

  const me = document.createElement('div');
  me.className = 'me';
  userMarker = new maplibregl.Marker({ element: me }).setLngLat(USER_POS).addTo(map);
  applyCosmetics();

  for (const p of PLACES) {
    const el = document.createElement('button');
    el.className = `pin ${p.tpl}`;
    el.dataset.place = p.id;
    el.textContent = pinLabel(p);
    el.addEventListener('click', e => { e.stopPropagation(); openPlace(p.id); });
    markers[p.id] = new maplibregl.Marker({ element: el, anchor: 'bottom' }).setLngLat(p.pos).addTo(map);
    el.setAttribute('aria-label', `${p.name}: ${pinLabel(p)}`); // MapLibre lo sobrescribe con "Map marker"
  }
}

function applyCosmetics() {
  const el = userMarker?.getElement(); if (!el) return;
  el.className = `me ${S.equipped.flecha}`;
  if (map?.isStyleLoaded()) styleRoute();
}

// ---------- Desplegable de categorías ----------
function renderCatMenu() {
  const opts = [['todos', 'Todos', PLACES.length], ...Object.keys(TPL).map(k => [k, TPL[k].menu, PLACES.filter(p => p.tpl === k).length])];
  $('#catMenu').innerHTML = opts.map(([k, l, n]) =>
    `<button role="option" aria-selected="${S.filter === k}" class="${S.filter === k ? 'on' : ''}" data-cat="${k}"><span class="dot-cat ${k}"></span>${l}<small>${n}</small></button>`).join('');
}
function openCatMenu() { renderCatMenu(); $('#catMenu').hidden = false; $('#catBtn').setAttribute('aria-expanded', 'true'); }
function closeCatMenu() { $('#catMenu').hidden = true; $('#catBtn').setAttribute('aria-expanded', 'false'); }
function setFilter(k) {
  S.filter = k;
  for (const p of PLACES) markers[p.id]?.getElement().toggleAttribute('hidden', !(k === 'todos' || p.tpl === k));
  $('#catLabel').textContent = k === 'todos' ? 'Todos' : TPL[k].menu;
  $('#catDot').className = `dot-cat ${k}`;
  closeCatMenu();
}

// ---------- Buscador (se despliega hacia arriba) ----------
function openSearch() {
  closePlace(); closeCatMenu();
  $('#dock').classList.add('open'); $('#mapUI').classList.add('searching');
  $('#avatarBtn').hidden = true; $('#searchCancel').hidden = false; $('#searchPanel').hidden = false;
  renderSearch();
}
function closeSearch() {
  const si = $('#searchInput'); si.value = ''; si.blur();
  $('#dock').classList.remove('open'); $('#mapUI').classList.remove('searching');
  $('#avatarBtn').hidden = false; $('#searchCancel').hidden = true; $('#searchPanel').hidden = true;
}
function resultRow(p, sub, icon = ICON.pin) {
  return `<button class="result" data-place="${p.id}"><i class="r-ic">${icon}</i><span>${p.name}<small>${sub}</small></span><b>${pinLabel(p)}</b></button>`;
}
function renderSearch() {
  const q = $('#searchInput').value.trim(), box = $('#searchPanel');
  if (q) { // buscador normal
    const norm = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
    const list = PLACES.filter(p => norm(`${p.name} ${p.cat} ${TPL[p.tpl].label}`).includes(norm(q)));
    box.innerHTML = list.length ? list.map(p => resultRow(p, `${p.cat} · ${fmtDist(distM(USER_POS, p.pos))}`)).join('')
      : '<p class="empty-note">Sin resultados</p>';
    return;
  }
  const favs = S.favs.map(placeById);
  box.innerHTML = `
    <div class="sp-title">Favoritos <small>${favs.length}</small></div>
    <div class="favs">${favs.map(p => `<button class="fav" data-place="${p.id}"><i class="${p.tpl}">${tplIcon(p)}</i>${p.name}</button>`).join('') || '<p class="empty-note">Marca sitios con el corazón para verlos aquí.</p>'}</div>
    <div class="sp-title">Buscar por tipo</div>
    <div class="types">${TYPES.map(([l, q]) => `<button data-type="${q}">${l}</button>`).join('')}</div>
    <div class="sp-title">Frecuentes y visitados</div>
    ${S.visits.length ? S.visits.map(v => resultRow(placeById(v.id), `Visitado ${v.when}${v.n > 1 ? ` · ${v.n} veces` : ''}`, ICON.clock)).join('') : '<p class="empty-note">Aún no has visitado ningún sitio.</p>'}`;
}

// ---------- Ficha del establecimiento ----------
function openPlace(id) {
  const p = placeById(id); S.place = id;
  closeCatMenu(); if ($('#dock').classList.contains('open')) closeSearch();
  $('#app').classList.add('place-open');
  $$('.pin').forEach(el => el.classList.toggle('selected', el.dataset.place === id));
  renderPlace();
  $('#placeSheet').hidden = false; $('#placeSheet').scrollTop = 0;
  map?.easeTo({ center: [p.pos[0], p.pos[1] - 0.0012], duration: 500 });
}
function renderPlace() {
  const p = placeById(S.place); if (!p) return;
  const d = fmtDist(distM(USER_POS, p.pos));
  let price = '';
  if (p.tpl === 'hosteleria') {
    price = `<div class="range"><strong>${p.range[0]}–${p.range[1]} €</strong><span>por persona</span></div>
      <p class="hint">Rango calculado con el 50 % central de lo que gastan los clientes.</p>`;
  } else {
    price = `<div class="rows">${p.items.map(i => `
      <div class="row"><div>${i.n}${i.v ? ` · ${i.v}` : ''}${i.note ? `<small>${i.note}</small>` : ''}</div>
      <b>${i.p} €<span>${UNIT[i.u]}</span></b></div>`).join('')}</div>`;
  }
  let ratings = '';
  if (TPL[p.tpl].ratings) {
    const avg = (p.ratings.reduce((a, b) => a + b, 0) / p.ratings.length).toFixed(1).replace('.', ',');
    ratings = `<div class="ratings"><h3>Valoración ${avg} <small>${p.nRatings} valoraciones</small></h3>
      ${TPL[p.tpl].ratings.map((c, i) => `<div class="rating"><span>${c}</span><div class="bar"><i style="width:${p.ratings[i] / 5 * 100}%"></i></div><b>${p.ratings[i].toFixed(1).replace('.', ',')}</b></div>`).join('')}</div>`;
  }
  const note = p.tpl === 'servicio'
    ? `<div class="note">${ICON.shield}<span>Servicio sin reseñas: solo se muestra el precio.${p.sensitive ? ' Las aportaciones en esta categoría son anónimas.' : ''}</span></div>` : '';
  const fav = S.favs.includes(p.id), alert = S.alerts.includes(p.id);
  const sh = $('#placeSheet');
  sh.innerHTML = `<div class="grab"></div>
    <div class="sheet-head"><div><div class="cat">${TPL[p.tpl].label} · ${p.cat}</div><h2>${p.name}</h2>
    <div class="meta">${d} · ${p.addr}</div></div>
    <div class="head-btns"><button class="fav-btn ${fav ? 'on' : ''}" data-act="fav" aria-pressed="${fav}" aria-label="${fav ? 'Quitar de favoritos' : 'Añadir a favoritos'}">${ICON.heart}</button>
    <button class="close" data-act="close" aria-label="Cerrar">${ICON.x}</button></div></div>
    ${price}${srcHTML(p.src)}
    <button class="alert-row" data-act="alert">${ICON.bell}<span>Avísame si cambia el precio<small>Te avisamos cuando los clientes reporten un precio distinto</small></span><i class="switch" role="switch" aria-checked="${alert}"></i></button>
    ${ratings}${note}
    <button class="btn primary" data-act="go">${ICON.nav} Ir aquí</button>`;
}
function closePlace() {
  $('#placeSheet').hidden = true; S.place = null;
  $('#app').classList.remove('place-open');
  $$('.pin').forEach(el => el.classList.remove('selected'));
}
function toggleList(list, id) { const i = list.indexOf(id); if (i >= 0) list.splice(i, 1); else list.push(id); return i < 0; }

// ---------- Navegación (GPS simulado) ----------
function routeCoords(a, b) {
  const mid = [a[0], b[1]]; // ruta en "L" simplificada (sin motor de rutas)
  const pts = [];
  const seg = (p, q, n) => { for (let i = 0; i < n; i++) pts.push([p[0] + (q[0] - p[0]) * i / n, p[1] + (q[1] - p[1]) * i / n]); };
  seg(a, mid, 40); seg(mid, b, 40); pts.push(b);
  return pts;
}
function startNav() {
  const p = placeById(S.place);
  const coords = routeCoords(USER_POS, p.pos);
  S.navigating = { id: p.id, coords };
  closePlace();
  $('#mapUI').hidden = true; $('#navUI').hidden = false; $('#navSimulate').disabled = false;
  const mid = [USER_POS[0], p.pos[1]];
  const total = distM(USER_POS, mid) + distM(mid, p.pos);
  // Sentido del giro: producto vectorial entre el primer y el segundo tramo
  const cross = (mid[0] - USER_POS[0]) * (p.pos[1] - mid[1]) - (mid[1] - USER_POS[1]) * (p.pos[0] - mid[0]);
  const right = cross < 0;
  S.navigating.turn = right ? 'derecha' : 'izquierda';
  setNavBanner(fmtDist(distM(USER_POS, mid)), `Luego gira a la ${S.navigating.turn}`, right ? 'right' : 'left');
  $('#navEta').textContent = `${Math.max(1, Math.round(total / SPEED[S.settings.mode]))} min · ${fmtDist(total)}`;
  $('#navTo').textContent = `Hacia ${p.name}`;
  markers[p.id].getElement().classList.add('dest');
  map?.getSource('ruta')?.setData({ type: 'Feature', geometry: { type: 'LineString', coordinates: coords } });
  map?.easeTo({ center: USER_POS, zoom: 17, pitch: 62, bearing: bearing(USER_POS, coords[40]), duration: 900 });
}
function setNavBanner(dist, instr, icon) {
  $('#navDist').textContent = dist;
  $('#navInstr').textContent = instr;
  $('#navIconTurn').style.display = icon === 'arrive' ? 'none' : '';
  $('#navIconTurn').style.transform = icon === 'left' ? 'scaleX(-1)' : '';
  $('#navIconArrive').style.display = icon === 'arrive' ? '' : 'none';
}
function stopNav() {
  if (!S.navigating) return;
  markers[S.navigating.id]?.getElement().classList.remove('dest');
  S.navigating = null;
  map?.getSource('ruta')?.setData({ type: 'FeatureCollection', features: [] });
  userMarker.setLngLat(USER_POS);
  $('#navUI').hidden = true; $('#mapUI').hidden = false;
  map?.easeTo({ ...HOME, duration: 900 });
}
function simulateArrival(fast) {
  const nav = S.navigating; if (!nav) return;
  $('#navSimulate').disabled = true;
  // t0 se toma del primer fotograma: la marca de tiempo de requestAnimationFrame puede ser
  // anterior a performance.now() y daba un índice negativo (cs[-1] → error y animación parada).
  const dur = fast ? 50 : 3500, cs = nav.coords;
  let t0 = null;
  const step = now => {
    if (S.navigating !== nav) { $('#navSimulate').disabled = false; return; } // se pulsó "Salir"
    if (t0 === null) t0 = now;
    const k = Math.max(0, Math.min(1, (now - t0) / dur));
    const idx = Math.min(cs.length - 1, Math.floor(k * (cs.length - 1)));
    userMarker.setLngLat(cs[idx]);
    map?.jumpTo({ center: cs[idx], bearing: bearing(cs[idx], cs[Math.min(cs.length - 1, idx + 5)]) || map.getBearing() });
    const half = Math.floor(cs.length / 2);
    if (idx < half) setNavBanner(fmtDist(distM(cs[idx], cs[half])), `Luego gira a la ${nav.turn}`, nav.turn === 'derecha' ? 'right' : 'left');
    else setNavBanner(fmtDist(distM(cs[idx], cs[cs.length - 1])), 'Sigue recto hasta el destino', 'straight');
    if (k < 1) requestAnimationFrame(step);
    else { $('#navSimulate').disabled = false; setNavBanner('Has llegado', placeById(nav.id).name, 'arrive'); addVisit(nav.id); startFlow(nav.id); }
  };
  requestAnimationFrame(step);
}
function addVisit(id) {
  const v = S.visits.find(x => x.id === id);
  if (v) { v.n++; v.when = 'hoy'; S.visits.splice(S.visits.indexOf(v), 1); S.visits.unshift(v); }
  else S.visits.unshift({ id, when: 'hoy', n: 1 });
}

// ---------- Precios pendientes (botón amarillo) ----------
function updatePriceFab() {
  const n = S.pending.length, b = $('#priceFab');
  b.classList.toggle('empty', n === 0);
  b.setAttribute('aria-disabled', n === 0);
  b.setAttribute('aria-label', n ? `Precios pendientes: ${n}` : 'No tienes precios pendientes');
  $('#priceDot').hidden = n === 0 || !S.settings.notifPending;
}
function openPending() {
  if (!S.pending.length) return toast('No tienes visitas pendientes de responder');
  if (S.pending.length === 1) return startFlow(S.pending[0].id, true);
  S.flow = { step: 'pick' }; renderFlow();
}
function removePending(id) { S.pending = S.pending.filter(x => x.id !== id); updatePriceFab(); }

// ---------- Preguntas al llegar ----------
function startFlow(id, fromPending = false) {
  S.flow = { id, fromPending, step: 'visited', item: null, value: null, rating: [0, 0, 0], earned: { c: 0, xp: 0 }, changed: false };
  renderFlow();
}
function endFlow() { S.flow = null; $('#flowOverlay').hidden = true; stopNav(); updateHUD(); }

function rulerHTML(value, max) {
  const STEP = 12, nums = [];
  for (let v = 0; v <= max; v += 5) nums.push(`<span class="ruler-num" style="left:${v * STEP}px">${v}</span>`);
  return `<div class="pv"><input id="priceInput" type="number" inputmode="numeric" min="0" max="${max}" value="${value}" aria-label="Precio en euros"><span>€</span></div>
    <div class="pv-hint">Toca el número para escribirlo o desliza la regla</div>
    <div class="ruler-wrap"><div class="ruler" id="ruler" aria-hidden="true"><div class="ruler-track" id="rulerTrack">
      <div class="ruler-inner" style="position:absolute;top:0;bottom:0;width:${max * STEP + 2}px">${nums.join('')}<div class="ruler-ticks" style="left:0;width:${max * STEP + 2}px"></div></div>
    </div></div><div class="needle"></div></div>`;
}
function setupRuler(max) {
  const STEP = 12, f = S.flow, r = $('#ruler'), track = $('#rulerTrack'), inner = track.firstElementChild, input = $('#priceInput');
  const pad = r.clientWidth / 2;
  track.style.width = `${max * STEP + pad * 2}px`; inner.style.left = `${pad}px`;
  const setVal = (v, moveRuler) => {
    f.value = Math.max(0, Math.min(max, Math.round(v)));
    if (document.activeElement !== input) input.value = f.value;
    $$('.sugs button').forEach(b => b.classList.toggle('on', +b.dataset.sug === f.value));
    if (moveRuler) r.scrollLeft = f.value * STEP;
  };
  r.scrollLeft = f.value * STEP;
  r.addEventListener('scroll', () => setVal(r.scrollLeft / STEP, false));
  input.addEventListener('input', () => { if (input.value !== '') setVal(+input.value, true); });
  input.addEventListener('blur', () => { input.value = f.value; });
  r.addEventListener('wheel', e => { e.preventDefault(); r.scrollLeft += (e.deltaY || e.deltaX); }, { passive: false });
  // Arrastre con ratón (en el móvil el dedo desplaza la regla de forma nativa)
  let drag = null;
  r.addEventListener('pointerdown', e => { if (e.pointerType !== 'mouse') return; drag = { x: e.clientX, s: r.scrollLeft }; r.classList.add('drag'); r.setPointerCapture(e.pointerId); });
  r.addEventListener('pointermove', e => { if (drag) r.scrollLeft = drag.s - (e.clientX - drag.x); });
  const end = () => { if (!drag) return; drag = null; r.classList.remove('drag'); r.scrollTo({ left: f.value * STEP, behavior: 'smooth' }); };
  r.addEventListener('pointerup', end); r.addEventListener('pointercancel', end);
  $$('.sugs button').forEach(b => b.addEventListener('click', () => setVal(+b.dataset.sug, true)));
  setVal(f.value, false);
}

function renderFlow() {
  const f = S.flow, card = $('#flowCard');
  $('#flowOverlay').hidden = false;
  if (f.step === 'pick') {
    card.innerHTML = `<h2>Precios pendientes</h2><p>Elige la visita que quieres responder.</p>
      <div class="options">${S.pending.map(x => { const p = placeById(x.id); return `<button class="option" data-pend="${p.id}"><span>${p.name}</span><small>${x.when}</small></button>`; }).join('')}</div>
      <div class="flow-actions"><button class="btn ghost" data-flow="close">Cerrar</button></div>`;
    return;
  }
  const p = placeById(f.id);
  const hasRating = !!TPL[p.tpl].ratings;
  const steps = p.tpl === 'hosteleria' ? ['visited', 'range', 'rate', 'reward']
    : ['visited', ...(p.items.length > 1 ? ['choose'] : []), 'confirm', ...(hasRating ? ['rate'] : []), 'reward'];
  const n = steps.indexOf(f.step === 'input' ? 'confirm' : f.step) + 1;
  const stepTag = f.step === 'reward' ? '' : `<div class="flow-step">Paso ${n} de ${steps.length - 1}</div>`;
  const privacy = p.sensitive ? `<div class="privacy-pill">${ICON.shield} Aportación anónima</div>` : '';
  let html = '';

  if (f.step === 'visited') {
    const pend = S.pending.find(x => x.id === p.id);
    html = f.fromPending
      ? `${stepTag}<h2>¿Fuiste a ${p.name}?</h2><p>Lo dejaste pendiente ${pend ? pend.when : ''}. Cuéntanos el precio y gana monedas.</p>`
      : `${stepTag}<h2>Has llegado a ${p.name}</h2><p>¿Has entrado finalmente? Tu respuesta ayuda a mantener los precios al día.</p>`;
    html += `<div class="flow-actions"><button class="btn primary" data-flow="yes">Sí, he ido</button>
      <button class="btn secondary" data-flow="later">Más tarde</button><button class="btn ghost" data-flow="no">No he ido</button></div>`;
  } else if (f.step === 'choose') {
    html = `${stepTag}<h2>¿Qué usaste?</h2><p>Elige el servicio para confirmar su precio.</p>
      <div class="options">${p.items.map((it, i) => `<button class="option" data-item="${i}"><span>${it.n}${it.v ? ` · ${it.v}` : ''}</span><small>${it.p} €${UNIT[it.u]}</small></button>`).join('')}</div>`;
  } else if (f.step === 'confirm') {
    const it = p.items[f.item ?? 0];
    html = `${stepTag}${privacy}<h2>¿Te costó esto?</h2><p>${it.n}${it.v ? ` · ${it.v}` : ''}${it.note ? ` (${it.note})` : ''}</p>
      <div class="big-price">${it.p} € <span>${unitText[it.u]}</span></div>
      <div class="flow-actions two"><button class="btn secondary" data-flow="diff">No, fue otro</button><button class="btn primary" data-flow="same">${ICON.check} Sí</button></div>`;
  } else if (f.step === 'input') {
    const it = p.items[f.item ?? 0];
    const max = Math.max(100, Math.ceil(it.p * 3 / 10) * 10);
    const sugs = [...new Set([it.p - 5, it.p, it.p + 5, it.p + 10].filter(v => v > 0))]; // precios que han dicho otros (simulado)
    html = `${stepTag}${privacy}<h2>¿Cuánto te costó?</h2><p>${it.n} · ${unitText[it.u] || 'precio'}</p>
      ${rulerHTML(f.value, max)}
      <div class="sugs-label">Precios que han dicho otros</div>
      <div class="sugs">${sugs.map(v => `<button data-sug="${v}">${v} €</button>`).join('')}</div>
      <div class="flow-actions"><button class="btn primary" data-flow="sendPrice">Enviar precio</button></div>`;
    card.innerHTML = html;
    return setupRuler(max);
  } else if (f.step === 'range') {
    const b = [['Menos de 10 €', 5], ['10–20 €', 15], ['20–30 €', 25], ['30–50 €', 40], ['Más de 50 €', 60]];
    html = `${stepTag}<h2>¿Cuánto gastaste por persona?</h2><p>Aproximado, con bebida incluida.</p>
      <div class="options">${b.map(([l, v]) => `<button class="option" data-bucket="${v}" data-label="${l}"><span>${l}</span></button>`).join('')}</div>`;
  } else if (f.step === 'rate') {
    const crit = TPL[p.tpl].ratings;
    html = `${stepTag}<h2>¿Qué tal fue?</h2><p>Opcional · +${REWARD.rating.c} monedas</p>
      ${crit.map((c, ci) => `<div class="stars-row"><span>${c}</span><div class="stars">${[1, 2, 3, 4, 5].map(s =>
        `<button class="star ${f.rating[ci] >= s ? 'on' : ''}" data-crit="${ci}" data-star="${s}" aria-label="${c}: ${s} de 5">${ICON.star}</button>`).join('')}</div></div>`).join('')}
      <div class="flow-actions two"><button class="btn ghost" data-flow="skip">Saltar</button><button class="btn primary" data-flow="sendRate" ${f.rating.some(r => r === 0) ? 'disabled' : ''}>Enviar</button></div>`;
  } else if (f.step === 'reward') {
    const before = S.xp - f.earned.xp, lvBefore = level(before), lv = level(S.xp);
    const pct = (S.xp % XP_PER_LEVEL) / XP_PER_LEVEL * 100;
    const msg = f.changed
      ? 'Hemos registrado tu precio. El precio mostrado cambia cuando varios clientes coinciden.'
      : 'Has confirmado el precio. ¡Gracias por mantenerlo al día!';
    html = `<div class="reward-burst"><div class="reward-coin">+${f.earned.c}</div><h2>¡Gracias!</h2></div>
      <p style="text-align:center">${msg}</p>
      ${lv > lvBefore ? `<div class="levelup">¡Subes a nivel ${lv}!</div>` : ''}
      <div class="xp"><div class="xp-head"><span>Nivel ${lv}</span><span>+${f.earned.xp} XP · ${S.xp % XP_PER_LEVEL}/${XP_PER_LEVEL}</span></div>
      <div class="xp-bar"><i style="width:${pct}%"></i></div></div>
      <div class="flow-actions"><button class="btn primary" data-flow="done">Volver al mapa</button><button class="btn secondary" data-flow="shop">Ir a la tienda</button></div>`;
  }
  card.innerHTML = html;
}

function grant(r) { S.coins += r.c; S.xp += r.xp; S.flow.earned.c += r.c; S.flow.earned.xp += r.xp; }

function onFlowClick(e) {
  const f = S.flow; if (!f) return;
  const b = e.target.closest('button'); if (!b) return;
  if (f.step === 'pick') {
    if (b.dataset.pend) startFlow(b.dataset.pend, true);
    else if (b.dataset.flow === 'close') endFlow();
    return;
  }
  const p = placeById(f.id), act = b.dataset.flow;
  const hasRating = !!TPL[p.tpl].ratings;
  const afterPrice = what => {
    S.contributions++; removePending(p.id);
    S.history.unshift({ id: p.id, what, when: 'hoy' });
    f.step = hasRating ? 'rate' : 'reward';
  };

  if (act === 'yes') f.step = p.tpl === 'hosteleria' ? 'range' : (p.items.length > 1 ? 'choose' : 'confirm');
  else if (act === 'later') {
    if (!S.pending.some(x => x.id === p.id)) S.pending.unshift({ id: p.id, when: 'hoy' });
    updatePriceFab(); toast('Guardado en pendientes: lo tienes en el botón €');
    return endFlow();
  } else if (act === 'no') { removePending(p.id); toast('Entendido'); return endFlow(); }
  else if (b.dataset.item) { f.item = +b.dataset.item; f.step = 'confirm'; }
  else if (act === 'same') { const it = p.items[f.item ?? 0]; grant(REWARD.confirm); S.confirmed++; afterPrice(`Confirmó ${it.p} €`); }
  else if (act === 'diff') { f.value = p.items[f.item ?? 0].p; f.step = 'input'; }
  else if (b.dataset.sug) return; // lo gestiona la regla
  else if (act === 'sendPrice') {
    const it = p.items[f.item ?? 0];
    f.changed = f.value !== it.p; grant(f.changed ? REWARD.newPrice : REWARD.confirm);
    afterPrice(`${it.n} · ${f.value} €`);
  }
  else if (b.dataset.bucket) { const v = +b.dataset.bucket; f.changed = v < p.range[0] || v > p.range[1]; grant(REWARD.confirm); afterPrice(`Gasto ${b.dataset.label}`); }
  else if (b.dataset.star) { f.rating[+b.dataset.crit] = +b.dataset.star; }
  else if (act === 'sendRate') { grant(REWARD.rating); f.step = 'reward'; }
  else if (act === 'skip') f.step = 'reward';
  else if (act === 'done') return endFlow();
  else if (act === 'shop') { endFlow(); return openProfile('tienda'); }
  renderFlow();
}

// ---------- Perfil (tienda y ajustes dentro, agrupados como en Discord) ----------
const PAGES = {
  main: 'Perfil', tienda: 'Tienda', pendientes: 'Precios pendientes', aportaciones: 'Mis aportaciones', insignias: 'Insignias',
  avisos: 'Avisos de precio', favoritos: 'Favoritos', apariencia: 'Apariencia', navegacion: 'Navegación y voz', notificaciones: 'Notificaciones',
  privacidad: 'Privacidad y ubicación', accesibilidad: 'Accesibilidad', idioma: 'Idioma y región', ayuda: 'Cómo funcionan los precios', acerca: 'Acerca de',
};
function openProfile(page = 'main') { closePlace(); closeCatMenu(); S.page = page; $('#screen-perfil').hidden = false; renderProfile(); $('#screen-perfil').scrollTop = 0; }
function closeProfile() { S.page = null; $('#screen-perfil').hidden = true; }

const it = (icon, label, { go, val, count, sub, cls = '', attr = '' } = {}) =>
  `<button class="it ${cls}" ${go ? `data-page="${go}"` : ''} ${attr}>${icon}<span class="lbl">${label}${sub ? `<small>${sub}</small>` : ''}</span>${count ? `<span class="count">${count}</span>` : ''}${val ? `<span class="val">${val}</span>` : ''}${go ? ICON.chev : ''}</button>`;
const sw = (key, label, sub) => `<button class="it" data-set="${key}"><span class="lbl">${label}${sub ? `<small>${sub}</small>` : ''}</span><i class="switch" role="switch" aria-checked="${!!S.settings[key]}"></i></button>`;
const grp = (title, body) => `<div class="grp">${title ? `<h3>${title}</h3>` : ''}<div class="grp-box">${body}</div></div>`;
const seg = (key, opts) => `<div class="segmented">${opts.map(([v, l]) => `<button class="${(key === 'theme' ? S.themePref : S.settings[key]) === v ? 'on' : ''}" data-seg="${key}" data-v="${v}">${l}</button>`).join('')}</div>`;

function ringSVG(size, r, sw) {
  const c = 2 * Math.PI * r, pct = (S.xp % XP_PER_LEVEL) / XP_PER_LEVEL;
  return `<svg viewBox="0 0 ${size} ${size}"><circle class="ring-bg" cx="${size / 2}" cy="${size / 2}" r="${r}" style="stroke-width:${sw}"/><circle class="ring-xp" cx="${size / 2}" cy="${size / 2}" r="${r}" style="stroke-width:${sw}" stroke-dasharray="${c * pct} ${c}"/></svg>`;
}

function renderProfile() {
  const pg = S.page, el = $('#screen-perfil');
  const lv = level(S.xp), xpIn = S.xp % XP_PER_LEVEL;
  let body = '';
  if (pg === 'main') {
    body = `<div class="phead"><div class="big-ring">${ringSVG(84, 39, 6)}<span class="face">${lv}</span></div>
        <div style="flex:1;min-width:0"><h2>Nivel ${lv}</h2><p>Colaborador local · ${xpIn}/${XP_PER_LEVEL} XP</p>
        <div class="xp-bar"><i style="width:${xpIn / XP_PER_LEVEL * 100}%"></i></div>
        <div class="coins"><i class="coin"></i>${S.coins}<span>· ${S.contributions} aportes</span></div></div></div>`
      + grp('Tu actividad',
        it(ICON.pending, 'Precios pendientes', { go: 'pendientes', count: S.pending.length || '' })
        + it(ICON.list, 'Mis aportaciones', { go: 'aportaciones', val: S.contributions })
        + it(ICON.heart, 'Favoritos', { go: 'favoritos', val: S.favs.length })
        + it(ICON.bell, 'Avisos de precio', { go: 'avisos', val: S.alerts.length })
        + it(ICON.award, 'Insignias', { go: 'insignias' }))
      + grp('Tienda', it(ICON.store, 'Tienda de cosméticos', { go: 'tienda', sub: 'Marcador de posición y estilo de la ruta' }))
      + grp('Ajustes de la app',
        it(ICON.palette, 'Apariencia', { go: 'apariencia' })
        + it(ICON.route, 'Navegación y voz', { go: 'navegacion' })
        + it(ICON.bell, 'Notificaciones', { go: 'notificaciones' })
        + it(ICON.shield, 'Privacidad y ubicación', { go: 'privacidad' })
        + it(ICON.access, 'Accesibilidad', { go: 'accesibilidad' })
        + it(ICON.globe, 'Idioma y región', { go: 'idioma', val: 'Español' }))
      + grp('Soporte',
        it(ICON.help, 'Cómo funcionan los precios', { go: 'ayuda' })
        + it(ICON.info, 'Acerca de', { go: 'acerca' }))
      + grp('', it(ICON.out, 'Cerrar sesión', { cls: 'danger', attr: 'data-act="logout"' }));
  } else if (pg === 'tienda') {
    const sec = k => SHOP.filter(i => i.kind === k).map(item => {
      const owned = S.owned.includes(item.id), eq = S.equipped[item.kind] === item.id;
      const btn = eq ? `<button class="btn secondary" disabled>Equipado</button>`
        : owned ? `<button class="btn secondary" data-equip="${item.id}">Equipar</button>`
        : `<button class="btn reward" data-buy="${item.id}" ${S.coins < item.cost ? 'disabled' : ''}><span class="coin"></span>${item.cost}</button>`;
      return `<div class="item ${eq ? 'equipped' : ''}"><div class="preview">${previewOf(item)}</div><h3>${item.name}</h3>${btn}</div>`;
    }).join('');
    body = `<p class="lead">Gasta tus monedas en cosméticos para tu GPS.</p>
      <div class="wallet"><span>Tus monedas</span><strong><span class="coin"></span>${S.coins}</strong></div>
      <h2 class="section-title">Marcador de posición</h2><div class="shop-grid">${sec('flecha')}</div>
      <h2 class="section-title">Estilo de la ruta</h2><div class="shop-grid">${sec('ruta')}</div>`;
  } else if (pg === 'pendientes') {
    body = `<p class="lead">Visitas en las que pulsaste "Más tarde". También las tienes en el botón amarillo € del mapa.</p>`
      + (S.pending.length ? grp('', S.pending.map(x => { const p = placeById(x.id); return it(tplIcon(p), p.name, { sub: `Visitado ${x.when}`, val: 'Responder', attr: `data-pend="${p.id}"` }); }).join(''))
        : `<div class="text-card">No tienes nada pendiente. ¡Todo al día!</div>`);
  } else if (pg === 'aportaciones') {
    body = grp(`${S.contributions} aportaciones · ${S.confirmed} precios confirmados`, S.history.map(h => { const p = placeById(h.id); return it(tplIcon(p), p.name, { sub: `${h.what} · ${h.when}` }); }).join(''));
  } else if (pg === 'favoritos') {
    body = S.favs.length ? grp('', S.favs.map(id => { const p = placeById(id); return `<button class="it" data-place="${p.id}">${tplIcon(p)}<span class="lbl">${p.name}<small>${p.cat}</small></span><span class="price">${pinLabel(p)}</span></button>`; }).join(''))
      : `<div class="text-card">Marca sitios con el corazón de su ficha.</div>`;
  } else if (pg === 'avisos') {
    body = `<p class="lead">Te avisamos cuando los clientes confirmen un precio distinto en estos sitios. Se activan desde la ficha con "Avísame si cambia".</p>`
      + (S.alerts.length ? grp('Sitios vigilados', S.alerts.map(id => { const p = placeById(id); return `<button class="it" data-unalert="${p.id}">${tplIcon(p)}<span class="lbl">${p.name}<small>Ahora: ${pinLabel(p)}</small></span><i class="switch" role="switch" aria-checked="true"></i></button>`; }).join(''))
        : `<div class="text-card">No vigilas ningún sitio todavía.</div>`)
      + (S.alerts.length ? `<div class="grp"><button class="btn secondary" data-act="demoAlert">Ver un ejemplo de aviso</button></div>` : '');
  } else if (pg === 'insignias') {
    const badges = [['Primer precio', true], ['Explorador', true], ['10 aportes', true], ['Racha 7 días', false], ['Crítico', false], ['50 aportes', false]];
    body = `<div class="badges">${badges.map(([n, ok]) => `<div class="badge ${ok ? '' : 'locked'}"><i>${ok ? ICON.award : ICON.lock}</i>${n}</div>`).join('')}</div>`;
  } else if (pg === 'apariencia') {
    body = grp('', `<div class="seg-label">Modo</div>${seg('theme', [['auto', 'Automático'], ['light', 'Claro'], ['dark', 'Oscuro']])}`
      + sw('buildings', 'Edificios en 3D', 'Muestra el mapa en 2.5D con edificios extruidos'));
  } else if (pg === 'navegacion') {
    body = grp('', `<div class="seg-label">Cómo te desplazas</div>${seg('mode', [['pie', 'A pie'], ['coche', 'En coche']])}`
      + sw('voice', 'Indicaciones por voz', 'Lee los giros en voz alta durante la ruta'));
  } else if (pg === 'notificaciones') {
    body = grp('', sw('notifPending', 'Recordar precios pendientes', 'Punto rojo en el botón € del mapa')
      + sw('notifAlerts', 'Cambios de precio', 'Avisos de los sitios que vigilas'));
  } else if (pg === 'privacidad') {
    body = `<div class="text-card"><b>Ubicación:</b> solo se usa mientras navegas con la app. Nunca en segundo plano.</div>
      <div class="text-card"><b>Categorías sensibles</b> (p. ej. psicología): tus aportaciones son siempre anónimas.</div>`
      + grp('', sw('anonAll', 'Aportar siempre de forma anónima', 'En todas las categorías, no solo en las sensibles')
        + it(ICON.trash, 'Borrar historial de visitas', { cls: 'danger', attr: 'data-act="clearVisits"' }));
  } else if (pg === 'accesibilidad') {
    body = grp('', sw('reduceMotion', 'Reducir animaciones', 'Quita los movimientos de la interfaz'));
  } else if (pg === 'idioma') {
    body = grp('', it(ICON.globe, 'Idioma', { val: 'Español' }) + it(ICON.route, 'Unidades', { val: 'km · €' }));
  } else if (pg === 'ayuda') {
    body = `<div class="text-card"><b>Servicios locales</b> (fisio, peluquería…): un precio por servicio. Mostramos la mediana de lo que reportan los clientes en los últimos meses.</div>
      <div class="text-card"><b>Hostelería:</b> siempre un rango por persona, con el 50 % central de lo que gasta la gente.</div>
      <div class="text-card"><b>Ocio:</b> un precio por instalación o actividad. Alquiler de pistas, precio total; actividades como escape room, por persona.</div>
      <div class="text-card"><b>Origen del dato:</b> cada precio indica si lo declaró el negocio o cuántos usuarios lo han confirmado. El precio solo cambia cuando varios clientes coinciden.</div>`;
  } else if (pg === 'acerca') {
    body = `<div class="text-card"><b>PrecioLocal</b> (nombre provisional) · prototipo del Trabajo de Fin de Grado.<br>Mapa: © OpenStreetMap, OpenFreeMap, MapLibre. Los establecimientos son inventados.</div>`;
  }
  const back = pg === 'main' ? 'close' : 'main';
  el.innerHTML = `<div class="topnav"><button class="back" data-back="${back}" aria-label="${pg === 'main' ? 'Volver al mapa' : 'Volver al perfil'}">${ICON.back}</button><h1>${PAGES[pg]}</h1></div>${body}`;
}
function previewOf(item) {
  if (item.kind === 'flecha') {
    const col = item.id === 'flecha-dorada' ? '#FFC93C' : item.id === 'flecha-neon' ? '#22D3EE' : 'var(--accent)';
    return `<span class="swatch-dot" style="background:${col}"></span>`;
  }
  return `<span class="route-prev ${item.id === 'ruta-discont' ? 'dashed' : ''} ${item.id === 'ruta-dorada' ? 'gold' : ''}"></span>`;
}

function onProfileClick(e) {
  const b = e.target.closest('button'); if (!b) return;
  const d = b.dataset;
  if (d.back) return d.back === 'close' ? closeProfile() : openProfile('main');
  if (d.page) return openProfile(d.page);
  if (d.place) { closeProfile(); return openPlace(d.place); }
  if (d.pend) { closeProfile(); return startFlow(d.pend, true); }
  if (d.unalert) { toggleList(S.alerts, d.unalert); toast('Aviso desactivado'); return renderProfile(); }
  if (d.set) {
    S.settings[d.set] = !S.settings[d.set];
    if (d.set === 'buildings' && map?.getLayer('edificios-3d')) map.setLayoutProperty('edificios-3d', 'visibility', S.settings.buildings ? 'visible' : 'none');
    if (d.set === 'reduceMotion') document.documentElement.classList.toggle('reduce-motion', S.settings.reduceMotion);
    if (d.set === 'notifPending') updatePriceFab();
    return renderProfile();
  }
  if (d.seg) {
    if (d.seg === 'theme') { S.themePref = d.v; store.set('pl-theme', d.v); applyTheme(); }
    else S.settings[d.seg] = d.v;
    return renderProfile();
  }
  if (d.buy) {
    const item = SHOP.find(i => i.id === d.buy);
    if (S.coins < item.cost) return;
    S.coins -= item.cost; S.owned.push(item.id); S.equipped[item.kind] = item.id; toast(`Comprado y equipado: ${item.name}`);
    applyCosmetics(); updateHUD(); return renderProfile();
  }
  if (d.equip) {
    const item = SHOP.find(i => i.id === d.equip); S.equipped[item.kind] = item.id; toast(`Equipado: ${item.name}`);
    applyCosmetics(); return renderProfile();
  }
  if (d.act === 'logout') return toast('En el prototipo no hay cuenta de usuario');
  if (d.act === 'clearVisits') { S.visits = []; return toast('Historial de visitas borrado'); }
  if (d.act === 'demoAlert') {
    const p = placeById(S.alerts[0]);
    const now = p.tpl === 'hosteleria' ? `${p.range[0] + 2}–${p.range[1] + 2} €` : `${p.items[0].p + 5} €`;
    closeProfile();
    return notify(`Cambio de precio en ${p.name}`, `${pinLabel(p)} → ${now} según los últimos clientes`, p.id);
  }
}

// ---------- HUD ----------
function updateHUD() {
  const r = 20, c = 2 * Math.PI * r, pct = (S.xp % XP_PER_LEVEL) / XP_PER_LEVEL;
  $('#ringXp').setAttribute('stroke-dasharray', `${c * pct} ${c}`);
  $('#avatarLv').textContent = level(S.xp);
  $('#avatarBtn').setAttribute('aria-label', `Abrir perfil · nivel ${level(S.xp)}, ${S.xp % XP_PER_LEVEL} de ${XP_PER_LEVEL} XP`);
  updatePriceFab();
  if (S.page) renderProfile();
}

// ---------- Eventos ----------
function bind() {
  $('#onbStart').addEventListener('click', () => { $('#screen-onboarding').hidden = true; store.set('pl-onb', '1'); });
  $('#catBtn').addEventListener('click', e => { e.stopPropagation(); $('#catMenu').hidden ? openCatMenu() : closeCatMenu(); });
  $('#catMenu').addEventListener('click', e => { const b = e.target.closest('[data-cat]'); if (b) setFilter(b.dataset.cat); });
  $('#themeFab').addEventListener('click', () => { S.themePref = currentTheme() === 'dark' ? 'light' : 'dark'; store.set('pl-theme', S.themePref); applyTheme(); });
  $('#locateFab').addEventListener('click', () => map?.easeTo({ center: USER_POS, zoom: 16.5, duration: 600 }));
  $('#priceFab').addEventListener('click', openPending);
  $('#avatarBtn').addEventListener('click', () => openProfile('main'));
  $('#placeSheet').addEventListener('click', e => {
    const a = e.target.closest('[data-act]')?.dataset.act;
    if (a === 'close') closePlace();
    else if (a === 'go') startNav();
    else if (a === 'fav') { toast(toggleList(S.favs, S.place) ? 'Añadido a favoritos' : 'Quitado de favoritos'); renderPlace(); }
    else if (a === 'alert') { toast(toggleList(S.alerts, S.place) ? 'Te avisaremos si cambia el precio' : 'Aviso desactivado'); renderPlace(); }
  });
  $('#navExit').addEventListener('click', stopNav);
  $('#navSimulate').addEventListener('click', () => simulateArrival(false));
  $('#flowCard').addEventListener('click', onFlowClick);
  $('#screen-perfil').addEventListener('click', onProfileClick);
  $('#notif').addEventListener('click', () => { $('#notif').hidden = true; const id = $('#notif').dataset.place; if (id) openPlace(id); });

  const si = $('#searchInput');
  si.addEventListener('focus', () => { if (!$('#dock').classList.contains('open')) openSearch(); });
  si.addEventListener('input', renderSearch);
  si.addEventListener('keydown', e => { if (e.key === 'Escape') closeSearch(); });
  $('#searchCancel').addEventListener('click', closeSearch);
  $('#searchPanel').addEventListener('click', e => {
    const b = e.target.closest('button'); if (!b) return;
    if (b.dataset.place) { closeSearch(); openPlace(b.dataset.place); }
    else if (b.dataset.type) { si.value = b.dataset.type; renderSearch(); }
  });
  document.addEventListener('click', e => { if (!e.target.closest('.cat-dd')) closeCatMenu(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') { closeCatMenu(); if (S.page) closeProfile(); } });
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', applyTheme);
}

// ---------- Inicio ----------
S.themePref = store.get('pl-theme') || 'auto';
if (store.get('pl-onb')) $('#screen-onboarding').hidden = true;
applyTheme();
bind();
initMap();
setFilter('todos');
updateHUD();

// Ganchos para las capturas automáticas (Playwright)
window.proto = { openPlace, startNav, simulateArrival, openProfile, openSearch, openPending, setTheme: v => { S.themePref = v; applyTheme(); }, S };
