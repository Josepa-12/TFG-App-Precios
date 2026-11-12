/* PrecioLocal — prototipo navegable (TFG)
 * Datos inventados. Mapa real: OpenFreeMap + MapLibre, recoloreado con las paletas B (claro) y D (oscuro).
 */

// ---------- Datos de ejemplo (establecimientos inventados en Sanlúcar de Barrameda) ----------
const USER_POS = [-6.3528, 36.7779]; // Plaza del Cabildo (posición simulada)
const HOME = { center: [-6.3515, 36.7790], zoom: 14.9, pitch: 50, bearing: -15 }; // vista inicial

const TPL = {
  servicio:   { label: 'Servicio local', ratings: false },
  hosteleria: { label: 'Hostelería',     ratings: ['Calidad', 'Servicio', 'Ambiente'] },
  ocio:       { label: 'Ocio',           ratings: ['Instalaciones', 'Atención', 'Calidad-precio'] },
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

// ---------- Estado ----------
const S = {
  coins: 240, xp: 340, contributions: 12, confirmed: 9,
  owned: ['flecha-azul', 'ruta-solida'], equipped: { flecha: 'flecha-azul', ruta: 'ruta-solida' },
  filter: 'todos', place: null, navigating: null, flow: null, themePref: 'auto',
};
const level = xp => Math.floor(xp / XP_PER_LEVEL) + 1;

// ---------- Utilidades ----------
const $ = s => document.querySelector(s);
const $$ = s => [...document.querySelectorAll(s)];
const store = {
  get(k) { try { return localStorage.getItem(k); } catch { return null; } },
  set(k, v) { try { localStorage.setItem(k, v); } catch { /* sin almacenamiento */ } },
};
const ICON = {
  x: '<svg class="ic" viewBox="0 0 24 24"><path d="M18 6 6 18M6 6l12 12"/></svg>',
  users: '<svg class="ic sm" viewBox="0 0 24 24"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>',
  building: '<svg class="ic sm" viewBox="0 0 24 24"><path d="M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18Z"/><path d="M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2"/></svg>',
  shield: '<svg class="ic sm" viewBox="0 0 24 24"><path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"/></svg>',
  nav: '<svg class="ic" viewBox="0 0 24 24"><polygon points="3 11 22 2 13 21 11 13 3 11"/></svg>',
  star: '<svg viewBox="0 0 24 24"><path d="M11.525 2.295a.53.53 0 0 1 .95 0l2.31 4.679a2.123 2.123 0 0 0 1.595 1.16l5.166.756a.53.53 0 0 1 .294.904l-3.736 3.638a2.123 2.123 0 0 0-.611 1.878l.882 5.14a.53.53 0 0 1-.771.56l-4.618-2.428a2.122 2.122 0 0 0-1.973 0L6.396 21.01a.53.53 0 0 1-.77-.56l.881-5.139a2.122 2.122 0 0 0-.611-1.879L2.16 9.795a.53.53 0 0 1 .294-.906l5.165-.755a2.122 2.122 0 0 0 1.597-1.16z"/></svg>',
  minus: '<svg class="ic" viewBox="0 0 24 24"><path d="M5 12h14"/></svg>',
  plus: '<svg class="ic" viewBox="0 0 24 24"><path d="M5 12h14M12 5v14"/></svg>',
  check: '<svg class="ic" viewBox="0 0 24 24"><path d="M20 6 9 17l-5-5"/></svg>',
  award: '<svg class="ic" viewBox="0 0 24 24"><circle cx="12" cy="8" r="6"/><path d="M15.477 12.89 17 22l-5-3-5 3 1.523-9.11"/></svg>',
  flag: '<svg class="ic" viewBox="0 0 24 24"><path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1zM4 22v-7"/></svg>',
  lock: '<svg class="ic" viewBox="0 0 24 24"><rect width="18" height="11" x="3" y="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>',
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
  clearTimeout(toastT); toastT = setTimeout(() => (t.hidden = true), 2200);
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
        if (map.getPaintProperty(id, 'fill-pattern')) map.setPaintProperty(id, 'fill-pattern', undefined);
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
    center: HOME.center, zoom: HOME.zoom, pitch: HOME.pitch, bearing: HOME.bearing, attributionControl: { compact: true },
  });
  map.on('error', e => console.warn('Mapa:', e.error?.message));
  map.on('load', () => {
    addBuildings(); addRouteLayers(); styleMap(currentTheme());
    window.__mapReady = true;
  });

  // Marcador del usuario
  const me = document.createElement('div');
  me.className = 'me';
  userMarker = new maplibregl.Marker({ element: me }).setLngLat(USER_POS).addTo(map);
  applyCosmetics();

  // Marcadores de precio
  for (const p of PLACES) {
    const el = document.createElement('button');
    el.className = `pin ${p.tpl}`;
    el.dataset.place = p.id;
    el.textContent = pinLabel(p);
    el.setAttribute('aria-label', `${p.name}: ${pinLabel(p)}`);
    el.addEventListener('click', e => { e.stopPropagation(); openPlace(p.id); });
    markers[p.id] = new maplibregl.Marker({ element: el, anchor: 'bottom' }).setLngLat(p.pos).addTo(map);
    el.setAttribute('aria-label', `${p.name}: ${pinLabel(p)}`); // MapLibre lo sobrescribe con "Map marker"
  }
}

function applyFilter() {
  for (const p of PLACES) {
    const show = S.filter === 'todos' || p.tpl === S.filter;
    markers[p.id]?.getElement().toggleAttribute('hidden', !show);
  }
  $$('.chip').forEach(c => c.classList.toggle('active', c.dataset.filter === S.filter));
}

function applyCosmetics() {
  const el = userMarker?.getElement(); if (!el) return;
  el.className = `me ${S.equipped.flecha}`;
  if (map?.isStyleLoaded()) styleRoute();
}

// ---------- Ficha del establecimiento ----------
function placeById(id) { return PLACES.find(p => p.id === id); }

function openPlace(id) {
  const p = placeById(id); S.place = id;
  $$('.pin').forEach(el => el.classList.toggle('selected', el.dataset.place === id));
  $('#searchResults').hidden = true;
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

  const sh = $('#placeSheet');
  sh.innerHTML = `<div class="grab"></div>
    <div class="sheet-head"><div><div class="cat">${TPL[p.tpl].label} · ${p.cat}</div><h2>${p.name}</h2>
    <div class="meta">${d} · ${p.addr}</div></div>
    <button class="close" data-act="close" aria-label="Cerrar">${ICON.x}</button></div>
    ${price}${srcHTML(p.src)}${ratings}${note}
    <button class="btn primary" data-act="go">${ICON.nav} Ir aquí</button>`;
  sh.hidden = false;
  sh.scrollTop = 0;
  map?.easeTo({ center: [p.pos[0], p.pos[1] - 0.0012], duration: 500 });
}
function closePlace() {
  $('#placeSheet').hidden = true; S.place = null;
  $$('.pin').forEach(el => el.classList.remove('selected'));
}

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
  $('#placeSheet').hidden = true; $('#mapUI').hidden = true; $('#tabbar').hidden = true;
  $('#navUI').hidden = false;
  const total = distM(USER_POS, [USER_POS[0], p.pos[1]]) + distM([USER_POS[0], p.pos[1]], p.pos);
  // Sentido del giro: producto vectorial entre el primer y el segundo tramo
  const mid = [USER_POS[0], p.pos[1]];
  const cross = (mid[0] - USER_POS[0]) * (p.pos[1] - mid[1]) - (mid[1] - USER_POS[1]) * (p.pos[0] - mid[0]);
  const right = cross < 0;
  S.navigating.turn = right ? 'derecha' : 'izquierda';
  setNavBanner(fmtDist(distM(USER_POS, mid)), `Luego gira a la ${S.navigating.turn}`, right ? 'right' : 'left');
  $('#navEta').textContent = `${Math.max(1, Math.round(total / 80))} min · ${fmtDist(total)}`;
  $('#navTo').textContent = `Hacia ${p.name}`;
  markers[p.id].getElement().classList.add('dest');
  if (map?.getSource('ruta')) map.getSource('ruta').setData({ type: 'Feature', geometry: { type: 'LineString', coordinates: coords } });
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
  if (map?.getSource('ruta')) map.getSource('ruta').setData({ type: 'FeatureCollection', features: [] });
  userMarker.setLngLat(USER_POS);
  $('#navUI').hidden = true; $('#mapUI').hidden = false; $('#tabbar').hidden = false;
  map?.easeTo({ ...HOME, duration: 900 });
}
function simulateArrival(fast) {
  const nav = S.navigating; if (!nav) return;
  $('#navSimulate').disabled = true;
  const dur = fast ? 50 : 3500, t0 = performance.now(), cs = nav.coords;
  const step = now => {
    const k = Math.min(1, (now - t0) / dur);
    const idx = Math.min(cs.length - 1, Math.floor(k * (cs.length - 1)));
    userMarker.setLngLat(cs[idx]);
    map?.jumpTo({ center: cs[idx], bearing: bearing(cs[idx], cs[Math.min(cs.length - 1, idx + 5)]) || map.getBearing() });
    const half = Math.floor(cs.length / 2);
    if (idx < half) setNavBanner(fmtDist(distM(cs[idx], cs[half])), `Luego gira a la ${nav.turn}`, nav.turn === 'derecha' ? 'right' : 'left');
    else setNavBanner(fmtDist(distM(cs[idx], cs[cs.length - 1])), 'Sigue recto hasta el destino', 'straight');
    if (k < 1) requestAnimationFrame(step);
    else { $('#navSimulate').disabled = false; setNavBanner('Has llegado', placeById(nav.id).name, 'arrive'); startFlow(nav.id); }
  };
  requestAnimationFrame(step);
}

// ---------- Preguntas al llegar ----------
function startFlow(id) { S.flow = { id, step: 'visited', item: null, value: null, rating: [0, 0, 0], earned: { c: 0, xp: 0 }, changed: false }; renderFlow(); }
function endFlow() { S.flow = null; $('#flowOverlay').hidden = true; stopNav(); updateHUD(); }

function renderFlow() {
  const f = S.flow, p = placeById(f.id), card = $('#flowCard');
  $('#flowOverlay').hidden = false;
  const hasRating = !!TPL[p.tpl].ratings;
  const steps = p.tpl === 'hosteleria' ? ['visited', 'range', 'rate', 'reward']
    : [...['visited'], ...(p.items.length > 1 ? ['choose'] : []), 'confirm', ...(hasRating ? ['rate'] : []), 'reward'];
  const n = steps.indexOf(f.step === 'input' ? 'confirm' : f.step) + 1;
  const stepTag = f.step === 'reward' ? '' : `<div class="flow-step">Paso ${n} de ${steps.length - 1}</div>`;
  const privacy = p.sensitive ? `<div class="privacy-pill">${ICON.shield} Aportación anónima</div>` : '';
  let html = '';

  if (f.step === 'visited') {
    html = `${stepTag}<h2>Has llegado a ${p.name}</h2><p>¿Has entrado finalmente? Tu respuesta ayuda a mantener los precios al día.</p>
      <div class="flow-actions"><button class="btn primary" data-flow="yes">Sí, he ido</button>
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
    html = `${stepTag}${privacy}<h2>¿Cuánto te costó?</h2><p>${it.n} · ${unitText[it.u] || 'precio'}</p>
      <div class="stepper"><button data-step="-1" aria-label="Restar 1 €">${ICON.minus}</button><output id="stepVal">${f.value} €</output><button data-step="1" aria-label="Sumar 1 €">${ICON.plus}</button></div>
      <div class="flow-actions"><button class="btn primary" data-flow="sendPrice">Enviar precio</button></div>`;
  } else if (f.step === 'range') {
    const b = [['Menos de 10 €', 5], ['10–20 €', 15], ['20–30 €', 25], ['30–50 €', 40], ['Más de 50 €', 60]];
    html = `${stepTag}<h2>¿Cuánto gastaste por persona?</h2><p>Aproximado, con bebida incluida.</p>
      <div class="options">${b.map(([l, v]) => `<button class="option" data-bucket="${v}"><span>${l}</span></button>`).join('')}</div>`;
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
  const p = placeById(f.id), b = e.target.closest('button'); if (!b) return;
  const act = b.dataset.flow;
  const hasRating = !!TPL[p.tpl].ratings;
  const afterPrice = () => { S.contributions++; f.step = hasRating ? 'rate' : 'reward'; };

  if (act === 'yes') f.step = p.tpl === 'hosteleria' ? 'range' : (p.items.length > 1 ? 'choose' : 'confirm');
  else if (act === 'no' || act === 'later') { toast(act === 'later' ? 'Te lo preguntaremos más tarde' : 'Entendido'); return endFlow(); }
  else if (b.dataset.item) { f.item = +b.dataset.item; f.step = 'confirm'; }
  else if (act === 'same') { grant(REWARD.confirm); S.confirmed++; afterPrice(); }
  else if (act === 'diff') { f.value = p.items[f.item ?? 0].p; f.step = 'input'; }
  else if (b.dataset.step) { f.value = Math.max(0, f.value + +b.dataset.step); $('#stepVal').textContent = `${f.value} €`; return; }
  else if (act === 'sendPrice') { f.changed = f.value !== p.items[f.item ?? 0].p; grant(f.changed ? REWARD.newPrice : REWARD.confirm); afterPrice(); }
  else if (b.dataset.bucket) { const v = +b.dataset.bucket; f.changed = v < p.range[0] || v > p.range[1]; grant(REWARD.confirm); afterPrice(); }
  else if (b.dataset.star) { f.rating[+b.dataset.crit] = +b.dataset.star; }
  else if (act === 'sendRate') { grant(REWARD.rating); f.step = 'reward'; }
  else if (act === 'skip') f.step = 'reward';
  else if (act === 'done') return endFlow();
  else if (act === 'shop') { endFlow(); return go('tienda'); }
  renderFlow();
}

// ---------- Tienda y perfil ----------
function previewOf(it) {
  if (it.kind === 'flecha') {
    const col = it.id === 'flecha-dorada' ? '#FFC93C' : it.id === 'flecha-neon' ? '#22D3EE' : 'var(--accent)';
    return `<span class="swatch-dot" style="background:${col}"></span>`;
  }
  return `<span class="route-prev ${it.id === 'ruta-discont' ? 'dashed' : ''} ${it.id === 'ruta-dorada' ? 'gold' : ''}"></span>`;
}
function renderShop() {
  const sec = k => SHOP.filter(i => i.kind === k).map(it => {
    const owned = S.owned.includes(it.id), eq = S.equipped[it.kind] === it.id;
    const btn = eq ? `<button class="btn secondary" disabled>Equipado</button>`
      : owned ? `<button class="btn secondary" data-equip="${it.id}">Equipar</button>`
      : `<button class="btn reward" data-buy="${it.id}" ${S.coins < it.cost ? 'disabled' : ''}><span class="coin"></span>${it.cost}</button>`;
    return `<div class="item ${eq ? 'equipped' : ''}"><div class="preview">${previewOf(it)}</div><h3>${it.name}</h3>${btn}</div>`;
  }).join('');
  $('#screen-tienda').innerHTML = `<h1>Tienda</h1><p class="lead">Gasta tus monedas en cosméticos para tu GPS.</p>
    <div class="wallet"><span>Tus monedas</span><strong><span class="coin"></span>${S.coins}</strong></div>
    <h2 class="section-title">Marcador de posición</h2><div class="shop-grid">${sec('flecha')}</div>
    <h2 class="section-title">Estilo de la ruta</h2><div class="shop-grid">${sec('ruta')}</div>`;
}
function renderProfile() {
  const lv = level(S.xp), pct = (S.xp % XP_PER_LEVEL) / XP_PER_LEVEL * 100;
  const badges = [['Primer precio', true], ['Explorador', true], ['10 aportes', true], ['Racha 7 días', false]];
  const seg = v => `<button class="${S.themePref === v ? 'on' : ''}" data-theme-pref="${v}">${{ auto: 'Automático', light: 'Claro', dark: 'Oscuro' }[v]}</button>`;
  $('#screen-perfil').innerHTML = `<div class="profile-head"><div class="avatar">TÚ</div><div><h1>Tu perfil</h1><p class="lead" style="margin:0">Nivel ${lv} · Colaborador local</p></div></div>
    <div class="card"><div class="xp-head"><span>Nivel ${lv}</span><span>${S.xp % XP_PER_LEVEL}/${XP_PER_LEVEL} XP</span></div><div class="xp-bar"><i style="width:${pct}%"></i></div></div>
    <div class="card stats"><div><strong>${S.contributions}</strong><span>Aportaciones</span></div><div><strong>${S.confirmed}</strong><span>Precios confirmados</span></div><div><strong>${S.coins}</strong><span>Monedas</span></div></div>
    <div class="card"><h2 class="section-title" style="margin-top:0">Insignias</h2><div class="badges">${badges.map(([n, ok]) => `<div class="badge ${ok ? '' : 'locked'}"><i>${ok ? ICON.award : ICON.lock}</i>${n}</div>`).join('')}</div></div>
    <div class="card"><div class="setting-label">Apariencia</div><div class="segmented">${seg('auto')}${seg('light')}${seg('dark')}</div></div>
    <div class="card"><div class="small-note">${ICON.shield}<span>Tu ubicación solo se usa mientras navegas. Las aportaciones en categorías sensibles (p. ej. psicología) son anónimas.</span></div></div>`;
}

function updateHUD() {
  $('#coinsTop').textContent = S.coins;
  $('#lvlTop').textContent = `Nv ${level(S.xp)}`;
  if (!$('#screen-tienda').hidden) renderShop();
  if (!$('#screen-perfil').hidden) renderProfile();
}

function go(screen) {
  closePlace();
  $('#screen-tienda').hidden = screen !== 'tienda';
  $('#screen-perfil').hidden = screen !== 'perfil';
  $('#mapUI').hidden = screen !== 'map';
  if (screen === 'tienda') renderShop();
  if (screen === 'perfil') renderProfile();
  $$('.tab').forEach(t => t.classList.toggle('active', t.dataset.go === screen));
}

// ---------- Búsqueda ----------
function renderResults(q) {
  const box = $('#searchResults');
  const norm = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
  const list = PLACES.filter(p => !q || norm(p.name + ' ' + p.cat).includes(norm(q)));
  box.innerHTML = list.length ? list.map(p => `<button class="result" data-place="${p.id}"><span>${p.name}<small>${p.cat} · ${fmtDist(distM(USER_POS, p.pos))}</small></span><b>${pinLabel(p)}</b></button>`).join('')
    : '<p style="padding:12px;color:var(--muted)">Sin resultados</p>';
  box.hidden = false;
}

// ---------- Eventos ----------
function bind() {
  $('#onbStart').addEventListener('click', () => { $('#screen-onboarding').hidden = true; store.set('pl-onb', '1'); });
  $$('.chip').forEach(c => c.addEventListener('click', () => { S.filter = c.dataset.filter; applyFilter(); }));
  $$('[data-go]').forEach(b => b.addEventListener('click', () => go(b.dataset.go)));
  $('#themeFab').addEventListener('click', () => { S.themePref = currentTheme() === 'dark' ? 'light' : 'dark'; store.set('pl-theme', S.themePref); applyTheme(); });
  $('#locateFab').addEventListener('click', () => map?.easeTo({ center: USER_POS, zoom: 16.5, duration: 600 }));
  $('#placeSheet').addEventListener('click', e => {
    const a = e.target.closest('[data-act]')?.dataset.act;
    if (a === 'close') closePlace(); else if (a === 'go') startNav();
  });
  $('#navExit').addEventListener('click', stopNav);
  $('#navSimulate').addEventListener('click', () => simulateArrival(false));
  $('#flowCard').addEventListener('click', onFlowClick);
  const si = $('#searchInput');
  si.addEventListener('focus', () => renderResults(si.value));
  si.addEventListener('input', () => renderResults(si.value));
  si.addEventListener('keydown', e => { if (e.key === 'Escape') { si.value = ''; si.blur(); $('#searchResults').hidden = true; } });
  $('#searchResults').addEventListener('click', e => { const b = e.target.closest('[data-place]'); if (b) { si.value = ''; si.blur(); openPlace(b.dataset.place); } });
  document.addEventListener('click', e => { if (!e.target.closest('.search, .search-results')) $('#searchResults').hidden = true; });
  $('#screen-tienda').addEventListener('click', e => {
    const b = e.target.closest('button'); if (!b) return;
    if (b.dataset.buy) {
      const it = SHOP.find(i => i.id === b.dataset.buy);
      if (S.coins < it.cost) return;
      S.coins -= it.cost; S.owned.push(it.id); S.equipped[it.kind] = it.id; toast(`Comprado y equipado: ${it.name}`);
    } else if (b.dataset.equip) {
      const it = SHOP.find(i => i.id === b.dataset.equip); S.equipped[it.kind] = it.id; toast(`Equipado: ${it.name}`);
    } else return;
    applyCosmetics(); updateHUD(); renderShop();
  });
  $('#screen-perfil').addEventListener('click', e => {
    const v = e.target.closest('[data-theme-pref]')?.dataset.themePref; if (!v) return;
    S.themePref = v; store.set('pl-theme', v); applyTheme(); renderProfile();
  });
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', applyTheme);
}

// ---------- Inicio ----------
S.themePref = store.get('pl-theme') || 'auto';
if (store.get('pl-onb')) $('#screen-onboarding').hidden = true;
applyTheme();
bind();
initMap();
updateHUD();

// Ganchos para las capturas automáticas (Playwright)
window.proto = { openPlace, startNav, simulateArrival, go, setTheme: v => { S.themePref = v; applyTheme(); }, S };
