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

// Lugares REALES de Sanlúcar que pueden tener precios (datos-lugares.js, generado desde OpenStreetMap).
// Ninguno trae precios: los DECLARA EL NEGOCIO (opcional) o los APORTA EL PRIMER CLIENTE al llegar con el GPS;
// después los clientes los confirman o los corrigen y el precio mostrado se ajusta a lo que pagan.
const MIN_REPORTS = 3; // aportes de clientes necesarios para que manden sobre el precio declarado (provisional)
const median = a => { const s = [...a].sort((x, y) => x - y), m = s.length >> 1; return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2; };
const pct = (a, q) => { const s = [...a].sort((x, y) => x - y); return s[Math.min(s.length - 1, Math.max(0, Math.round((s.length - 1) * q)))]; };
// Concepto con precio: "declared" lo pone el negocio (o su web oficial); "reports" son los precios que pagaron los clientes.
// g: grupo (p. ej. "Fisioterapia"); desde: la web dice "desde X €"; nota: "con bono"; ask: el negocio lo da "a consultar".
function mkItem(n, u, extra = {}) {
  return {
    n, u, declared: null, reports: [], g: null, desde: false, nota: '', ask: false,
    at: null, flags: 0, review: false, // at: última confirmación (frescura); flags/review: avisos de precio incorrecto
    ...extra,
    get p() { // precio que se muestra
      if (this.reports.length >= MIN_REPORTS || this.declared == null) return this.reports.length ? Math.round(median(this.reports) * 100) / 100 : null;
      return this.declared;
    },
  };
}
const SENSITIVE_CATS = ['Clínica médica', 'Clínica privada', 'Fisioterapia', 'Clínica dental']; // salud (no veterinarios): aportaciones siempre anónimas
// Tipo de negocio → plantilla de precios (para dar de alta negocios a mano)
const HOSTELERIA_CATS = ['Restaurante', 'Taberna', 'Comida rápida', 'Cafetería', 'Heladería', 'Bar', 'Pub', 'Discoteca'];
const OCIO_POICATS = ['deporte', 'cultura', 'parking'];
const tplOfCat = c => HOSTELERIA_CATS.includes(c) ? 'hosteleria' : OCIO_POICATS.includes(PLACE_POICAT[c]) ? 'ocio' : 'servicio';

const dmy = t => { const [d, m, y] = t.split('/').map(Number); return new Date(y, m - 1, d, 12).getTime(); };
function buildPlace(r) {
  const web = WEB_PRICES[r.id];
  let items = [];
  if (r.tpl !== 'hosteleria') {
    items = web
      ? Object.entries(web.grupos).flatMap(([g, list]) => list.map(([n, u, v, o = {}]) => mkItem(n, u, { g, declared: v, desde: !!o.desde, nota: o.nota || '', ask: v == null, at: v == null ? null : dmy(web.fecha) })))
      : (PLACE_CONCEPTS[r.cat] || [['Precio', 'servicio']]).map(([n, u]) => mkItem(n, u));
  }
  return {
    ...r,
    poiCat: PLACE_POICAT[r.cat] || 'tiendas',          // categoría de mapa (lugares.js) para "Encuentra cerca"
    sensitive: SENSITIVE_CATS.includes(r.cat),         // aportes anónimos en categorías sensibles
    items,
    web: web ? { url: web.url, fecha: web.fecha } : null, // precios iniciales publicados en la web del negocio
    declaredRange: null, rangeReports: [],            // hostelería: rango por persona
    rangeAt: null, rangeFlags: 0, rangeReview: false, // frescura y avisos del rango
    offers: [], flagLog: [],                          // ofertas del negocio y avisos de clientes
    // Rango mostrado: siempre dos números finitos. Si el percentil superior cae en el tramo abierto
    // ("Más de 50 €", guardado con hi = null), el máximo es el inicio de ese tramo y rangeOpen = true.
    get range() {
      if (this.rangeReports.length >= MIN_REPORTS || !this.declaredRange) {
        if (!this.rangeReports.length) return null;
        const top = pct(this.rangeReports.map(b => b[1] ?? Infinity), 0.75);
        const hi = isFinite(top) ? top : Math.max(...this.rangeReports.filter(b => b[1] == null).map(b => b[0]));
        return [pct(this.rangeReports.map(b => b[0]), 0.25), hi];
      }
      return this.declaredRange;
    },
    get rangeOpen() { // el máximo del rango es "o más" (tramo abierto): no se inventa tope
      if (!this.rangeReports.length || (this.rangeReports.length < MIN_REPORTS && this.declaredRange)) return false;
      return !isFinite(pct(this.rangeReports.map(b => b[1] ?? Infinity), 0.75));
    },
    ratings: null, nRatings: 0, owner: false,
  };
}
const PLACES = [...REAL_PLACES, ...EXTRA_PLACES].map(buildPlace);
const idByName = n => PLACES.find(p => p.name === n)?.id;

const SHOP = [
  { id: 'flecha-azul',   kind: 'flecha', name: 'Posición clásica',  cost: 0 },
  { id: 'flecha-dorada', kind: 'flecha', name: 'Posición dorada',   cost: 150 },
  { id: 'flecha-neon',   kind: 'flecha', name: 'Posición neón',     cost: 200 },
  { id: 'ruta-solida',   kind: 'ruta',   name: 'Ruta clásica',      cost: 0 },
  { id: 'ruta-discont',  kind: 'ruta',   name: 'Ruta discontinua',  cost: 100 },
  { id: 'ruta-dorada',   kind: 'ruta',   name: 'Ruta dorada',       cost: 250 },
];

const XP_PER_LEVEL = 150;
// first: aportar el PRIMER precio de un sitio (recompensa extra para animar a los pioneros)
const REWARD = { confirm: { c: 10, xp: 15 }, newPrice: { c: 15, xp: 20 }, first: { c: 25, xp: 30 }, rating: { c: 5, xp: 10 } };
const SPEED = { pie: 80, coche: 350 }; // metros por minuto para la ETA

// ---------- Estado ----------
const S = {
  coins: 240, xp: 340, contributions: 12, confirmed: 9,
  owned: ['flecha-azul', 'ruta-solida'], equipped: { flecha: 'flecha-azul', ruta: 'ruta-solida' },
  filter: 'ninguno', place: null, navigating: null, flow: null, themePref: 'auto', page: null,
  pending: [{ id: idByName('Gabriel Bornes Peluquero'), when: 'ayer' }].filter(x => x.id), // visitas con "Más tarde" sin responder
  favs: ['Casa Ballén', 'Padel La Jara', 'Multicines Al-Ándalus'].map(idByName).filter(Boolean), // favoritos (ids de PLACES)
  spots: [],                                           // favoritos sin precios: direcciones, lugares del mapa, ubicaciones
  dest: null,                                          // destino abierto en la ficha: precio, lugar del mapa o dirección
  poiCat: null,                                        // categoría de "Encuentra cerca" activa
  alerts: ['Casa Ballén'].map(idByName).filter(Boolean), // "Avísame si cambia / cuando tenga precio"
  visits: [['Gabriel Bornes Peluquero', 'ayer', 3], ['Castillo de Santiago', 'hace 5 días', 1], ['El Esquinazo', 'el mes pasado', 2]]
    .map(([n, when, k]) => ({ id: idByName(n), when, n: k })).filter(v => v.id),
  history: [],                                         // aportaciones de esta sesión (los negocios reales empiezan sin precios)
  travelMode: 'coche',                                 // se elige en la ficha; el coche es la opción principal
  settings: { buildings: true, reduceMotion: false, voice: true, sound: true, notifPending: true, notifAlerts: true, anonAll: false },
};
// Tiempo desde que llegas hasta que la app pregunta "¿has ido?": 1 hora, para que dé tiempo a recibir el servicio.
const ASK_DELAY_MS = 60 * 60 * 1000;
// Al llegar, la ruta sigue en pantalla hasta pulsar "Terminar ruta" o hasta que pase este tiempo.
const ARRIVE_END_MS = 60 * 1000;
const askDelay = () => window.__askDelayMs ?? ASK_DELAY_MS; // las capturas automáticas lo acortan
const fmtDelay = ms => ms >= 3600000 ? `${+(ms / 3600000).toFixed(1)} h` : ms >= 60000 ? `${Math.round(ms / 60000)} min` : `${Math.round(ms / 1000)} s`; // espacio no separable: "4 s" no se parte
const level = xp => Math.floor(xp / XP_PER_LEVEL) + 1;
// "Reducir animaciones" (ajuste de la app o preferencia del sistema): la cámara del mapa salta sin animar
const reducedMotion = () => !!S.settings.reduceMotion || matchMedia('(prefers-reduced-motion: reduce)').matches;
const motionMs = ms => (reducedMotion() ? 0 : ms);

// ---------- Utilidades ----------
const $ = s => document.querySelector(s);
const $$ = s => [...document.querySelectorAll(s)];
const store = {
  get(k) { try { return localStorage.getItem(k); } catch { return null; } },
  set(k, v) { try { localStorage.setItem(k, v); } catch { /* sin almacenamiento */ } },
};
// Iconos decorativos: aria-hidden (el texto o la etiqueta del control ya dicen qué es)
const svg = (d, cls = 'ic') => `<svg class="${cls}" viewBox="0 0 24 24" aria-hidden="true">${d}</svg>`;
const ICON = {
  x: svg('<path d="M18 6 6 18M6 6l12 12"/>'),
  users: svg('<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/>', 'ic sm'),
  building: svg('<path d="M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18Z"/><path d="M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2"/>', 'ic sm'),
  shield: svg('<path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"/>'),
  nav: svg('<polygon points="3 11 22 2 13 21 11 13 3 11"/>'),
  star: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M11.525 2.295a.53.53 0 0 1 .95 0l2.31 4.679a2.123 2.123 0 0 0 1.595 1.16l5.166.756a.53.53 0 0 1 .294.904l-3.736 3.638a2.123 2.123 0 0 0-.611 1.878l.882 5.14a.53.53 0 0 1-.771.56l-4.618-2.428a2.122 2.122 0 0 0-1.973 0L6.396 21.01a.53.53 0 0 1-.77-.56l.881-5.139a2.122 2.122 0 0 0-.611-1.879L2.16 9.795a.53.53 0 0 1 .294-.906l5.165-.755a2.122 2.122 0 0 0 1.597-1.16z"/></svg>',
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
  web: svg('<circle cx="12" cy="12" r="10"/><path d="M2 12h20M12 2a15 15 0 0 1 0 20 15 15 0 0 1 0-20"/>', 'ic sm'),
  info: svg('<circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/>'),
  out: svg('<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9"/>'),
  trash: svg('<path d="M3 6h18M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/>'),
  car: svg('<path d="M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9C18.7 10.6 16 10 16 10s-1.3-1.4-2.2-2.3c-.5-.4-1.1-.7-1.8-.7H5c-.6 0-1.1.4-1.4.9l-1.4 2.9A3.7 3.7 0 0 0 2 12v4c0 .6.4 1 1 1h2"/><circle cx="7" cy="17" r="2"/><path d="M9 17h6"/><circle cx="17" cy="17" r="2"/>'),
  walk: svg('<circle cx="13" cy="4" r="2"/><path d="m7 21 3-5 2 1 1 4M10 16l1-6 3 2 3 1M11 10 8 12l-1 3"/>'),
  vol: svg('<path d="M11 4.7a.7.7 0 0 0-1.2-.5L6.4 7.6A1.4 1.4 0 0 1 5.4 8H3a1 1 0 0 0-1 1v6a1 1 0 0 0 1 1h2.4a1.4 1.4 0 0 1 1 .4l3.4 3.4a.7.7 0 0 0 1.2-.5z"/><path d="M16 9a5 5 0 0 1 0 6M19.4 18.4a9 9 0 0 0 0-12.8"/>'),
  volx: svg('<path d="M11 4.7a.7.7 0 0 0-1.2-.5L6.4 7.6A1.4 1.4 0 0 1 5.4 8H3a1 1 0 0 0-1 1v6a1 1 0 0 0 1 1h2.4a1.4 1.4 0 0 1 1 .4l3.4 3.4a.7.7 0 0 0 1.2-.5z"/><path d="m22 9-6 6M16 9l6 6"/>'),
};

// ---------- Sonidos (Web Audio, sin archivos) ----------
let actx;
const SFX = {
  arrive: [[660, .16, 0], [880, .16, .14], [1100, .3, .28]],
  ask:    [[880, .14, 0], [660, .2, .12]],
  coin:   [[988, .1, 0], [1319, .26, .08]],
};
function sfx(name) {
  if (!S.settings.sound) return;
  try {
    actx ||= new (window.AudioContext || window.webkitAudioContext)();
    const t = actx.currentTime;
    for (const [f, d, at] of SFX[name]) {
      const o = actx.createOscillator(), g = actx.createGain();
      o.type = 'sine'; o.frequency.value = f;
      g.gain.setValueAtTime(0.0001, t + at);
      g.gain.exponentialRampToValueAtTime(0.14, t + at + 0.01);
      g.gain.exponentialRampToValueAtTime(0.0001, t + at + d);
      o.connect(g).connect(actx.destination); o.start(t + at); o.stop(t + at + d + 0.02);
    }
  } catch { /* sin audio */ }
}
function updateSound() {
  const on = S.settings.sound;
  $$('.sound-btn').forEach(b => {
    b.innerHTML = on ? ICON.vol : ICON.volx;
    b.classList.toggle('off', !on);
    b.setAttribute('aria-pressed', on);
    b.setAttribute('aria-label', on ? 'Silenciar la app' : 'Activar el sonido de la app');
  });
}
const UNIT = { 'sesión': '/sesión', h: '/h', persona: '/persona', servicio: '', 'día': '/día' };
const unitText = { 'sesión': 'por sesión', h: 'por hora', persona: 'por persona', servicio: '', 'día': 'por día' };
const GUESS = { 'sesión': 30, h: 10, persona: 10, servicio: 15, 'día': 10 }; // valor inicial de la regla si aún no hay precio

function distM(a, b) {
  const R = 6371000, toR = d => d * Math.PI / 180;
  const dLat = toR(b[1] - a[1]), dLon = toR(b[0] - a[0]);
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(toR(a[1])) * Math.cos(toR(b[1])) * Math.sin(dLon / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(h));
}
const fmtDist = m => m < 1000 ? `${Math.round(m / 10) * 10} m` : `${(m / 1000).toFixed(1).replace('.', ',')} km`;
const bearing = (a, b) => Math.atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI;
const placeById = id => PLACES.find(p => p.id === id);
const tplIcon = p => glyphSVG(PLACE_GLYPH[p.cat] || POI_CATS[p.poiCat]?.glyph); // logo del tipo de negocio (glyphSVG ya lo oculta a lectores)
const priced = p => p.tpl === 'hosteleria' ? !!p.range : p.items.some(i => i.p != null);
const nReports = p => p.tpl === 'hosteleria' ? p.rangeReports.length : p.items.reduce((a, i) => a + i.reports.length, 0);
const isDeclared = p => p.tpl === 'hosteleria' ? !!p.declaredRange : p.items.some(i => i.declared != null);
const fmtEur = v => `${(Number.isInteger(v) ? String(v) : v.toFixed(2)).replace('.', ',')} €`;
// Icono de un sitio de la app: mismo color que su marcador (servicio azul, hostelería naranja, ocio verde)
const placeIconHTML = p => `<i class="poi-ic tpl-${p.tpl}" aria-hidden="true">${tplIcon(p)}</i>`;
const placeDest = p => ({ kind: 'place', id: p.id, key: p.id, name: p.name, pos: p.pos });
// ¿Este punto del mapa es uno de los negocios de la app? (mismo nombre y a menos de 80 m)
const placeForSpot = s => PLACES.find(p => normTxt(p.name) === normTxt(s.name) && distM(p.pos, s.pos) < 80);

// Rango por persona en texto. Tramo abierto: "Más de 50 €" (largo) / "50 €+" (corto), o "20–50+ €"
function fmtRange(p, short = false) {
  const [lo, hi] = p.range;
  if (!p.rangeOpen) return `${lo}–${hi} €`;
  return lo >= hi ? (short ? `${hi} €+` : `Más de ${hi} €`) : `${lo}–${hi}+ €`;
}
function pinLabel(p) {
  if (!priced(p)) return 'Sin precio';
  if (p.tpl === 'hosteleria') return fmtRange(p, true);
  const ps = p.items.filter(i => i.p != null).map(i => i.p);
  return ps.length === 1 ? fmtEur(ps[0]) : `desde ${fmtEur(Math.min(...ps))}`;
}
// De dónde sale el precio: declarado por el negocio, aportado por clientes o ambos
function srcHTML(p) {
  // Mismo patrón en los cuatro casos: "origen · estado de la confirmación por clientes"
  const n = nReports(p), dec = isDeclared(p), cl = n === 1 ? '1 cliente' : `${n} clientes`;
  const conf = n ? ` · ${n} de ${MIN_REPORTS} clientes ${n === 1 ? 'lo ha' : 'lo han'} confirmado` : ' · pendiente de confirmar por clientes';
  if (dec && n < MIN_REPORTS && p.web) return `<div class="src declared">${ICON.web}<span>Publicado en la web del negocio <span style="white-space:nowrap">(<a href="${p.web.url}" target="_blank" rel="noopener">${p.web.url.replace(/^https?:\/\//, '')}</a>)</span> · consultado el ${p.web.fecha}${n ? conf : ''}</span></div>`;
  if (dec && n < MIN_REPORTS) return `<div class="src declared">${ICON.building}<span>Declarado por el negocio${conf}</span></div>`;
  if (n >= MIN_REPORTS) return `<div class="src">${ICON.users}<span>Según lo que pagaron ${cl}</span></div>`;
  return `<div class="src">${ICON.users}<span>Aportado por ${cl} · faltan ${MIN_REPORTS - n} más para confirmarlo</span></div>`;
}

let toastT;
function toast(msg) {
  // El elemento queda siempre en el DOM (role=status anuncia cada texto nuevo); se muestra/oculta con .show
  const t = $('#toast'); t.hidden = false; t.textContent = msg; t.classList.add('show');
  clearTimeout(toastT); toastT = setTimeout(() => t.classList.remove('show'), msg.length > 60 ? 6000 : 4500);
}
let notifT;
function notify(title, body, placeId) {
  const n = $('#notif');
  n.innerHTML = `<span class="n-ic">${ICON.bell}</span><div><b>${title}</b><span>${body}</span></div>`;
  n.dataset.place = placeId || ''; n.hidden = false;
  clearTimeout(notifT); notifT = setTimeout(() => (n.hidden = true), 6000);
}

// ---------- Accesibilidad: ventanas modales y foco ----------
// Ventanas que tapan el mapa, de la más alta a la más baja. Mientras una está visible, el resto de la app
// queda inerte (atributo inert: ni foco, ni clic, ni lector de pantalla) y el tabulador no sale de ella.
const MODALS = ['#flowOverlay', '#screen-cuenta', '#screen-onboarding', '#screen-perfil'];
const topModal = () => MODALS.map(s => $(s)).find(el => el && !el.hidden) || null;
function syncModal() {
  const top = topModal();
  // Las regiones vivas (aviso y notificación) nunca se inertizan: deben poder anunciarse
  for (const el of $('#app').children) if (!el.matches('#toast, .notif-live')) el.inert = !!top && el !== top;
}
const isShown = el => !!el && el.isConnected && !el.closest('[hidden], [inert]') && el.getClientRects().length > 0;
const focusables = box => [...box.querySelectorAll('a[href], button, input, select, textarea, [tabindex]')]
  .filter(el => el.tabIndex >= 0 && !el.disabled && isShown(el) && getComputedStyle(el).visibility !== 'hidden');
// Trampa de foco: Tab y Mayús+Tab dan la vuelta dentro de la ventana abierta
function trapTab(e) {
  if (e.key !== 'Tab') return;
  const top = topModal(); if (!top) return;
  const box = top.id === 'flowOverlay' ? $('#flowCard') : top, list = focusables(box), a = document.activeElement;
  if (!list.length) { e.preventDefault(); return; }
  if (!box.contains(a)) { e.preventDefault(); (e.shiftKey ? list[list.length - 1] : list[0]).focus(); return; }
  // a puede ser un título con tabindex="-1" (no está en la lista): se mira qué controles hay antes y después
  const before = list.some(x => x !== a && (x.compareDocumentPosition(a) & Node.DOCUMENT_POSITION_FOLLOWING));
  const after = list.some(x => x !== a && (a.compareDocumentPosition(x) & Node.DOCUMENT_POSITION_FOLLOWING));
  if (e.shiftKey && !before) { e.preventDefault(); list[list.length - 1].focus(); }
  else if (!e.shiftKey && !after) { e.preventDefault(); list[0].focus(); }
}
// Selector del "mismo" control tras volver a pintar (por sus atributos data-*), para no dejar el foco en <body>
function ctlSig(el) {
  if (!el || el === document.body) return null;
  const data = [...el.attributes].filter(x => x.name.startsWith('data-')).map(x => `[${x.name}="${CSS.escape(x.value)}"]`).join('');
  if (data) return el.tagName.toLowerCase() + data;
  if (el.id) return `#${CSS.escape(el.id)}`;
  return null;
}
// Primer selector que exista, esté visible y no esté desactivado
const findCtl = (box, sels) => sels.filter(Boolean).map(s => box.querySelector(s)).find(t => t && !t.disabled && isShown(t)) || null;
// Vuelve a pintar box y, si el foco estaba dentro, lo lleva al control equivalente (o al título)
function keepFocus(box, draw, fallback = 'h2') {
  const a = document.activeElement, had = !!a && box.contains(a), sig = had && ctlSig(a);
  draw();
  if (had && !box.contains(document.activeElement)) (findCtl(box, [sig]) || box.querySelector(fallback))?.focus();
}
// Lleva el foco a el si sigue en pantalla; si no, al primero de los alternativos que lo esté
function focusBack(el, ...alts) {
  const t = [el, ...alts.map(s => (typeof s === 'string' ? $(s) : s))].find(isShown);
  t?.focus({ preventScroll: true });
  return t;
}
// Errores junto al campo (modo negocio, ofertas, "Otra cosa"): texto bajo el campo, aria-invalid,
// aria-describedby y foco al primer campo con error, además del aviso. errs: [[campo, mensaje], …]
function fieldErrors(box, errs) {
  box.querySelectorAll('.field-err').forEach(p => p.remove());
  box.querySelectorAll('[aria-invalid]').forEach(el => { el.removeAttribute('aria-invalid'); if (el.getAttribute('aria-describedby')?.endsWith('Err')) el.removeAttribute('aria-describedby'); });
  for (const [el, msg] of errs) {
    if (!el) continue;
    const p = document.createElement('p');
    p.className = 'acc-err field-err'; p.id = `${el.id || el.name}Err`; p.textContent = msg;
    (el.closest('.owner-field') || el.closest('.acc-row') || el.closest('.acc-field') || el).after(p);
    el.setAttribute('aria-invalid', 'true'); el.setAttribute('aria-describedby', p.id);
  }
  if (errs.length) { errs[0][0]?.focus(); toast(errs[0][1]); }
  return errs.length > 0;
}
// ¿El foco se ha perdido (en <body> o en algo oculto/inerte)?
const focusLost = () => { const a = document.activeElement; return !a || a === document.body || !isShown(a); };

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
      if (id === 'edificios-3d' || id === 'poi-app' || id.startsWith('ruta')) continue;
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
  if (map.getLayer('poi-app')) {
    map.setPaintProperty('poi-app', 'text-color', poiTextColor(t));
    map.setPaintProperty('poi-app', 'text-halo-color', c.halo);
  }
  styleRoute();
}
let VSRC = null; // fuente vectorial del mapa base (OpenFreeMap)

function addBuildings() {
  const style = map.getStyle();
  const src = VSRC = Object.keys(style.sources).find(k => style.sources[k].type === 'vector');
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

function addRouteLayers() { // por debajo de los iconos de los puntos de interés
  const before = map.getLayer('poi-app') ? 'poi-app' : undefined;
  map.addSource('ruta', { type: 'geojson', data: { type: 'FeatureCollection', features: [] } });
  map.addLayer({ id: 'ruta-borde', type: 'line', source: 'ruta', layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-color': '#FFFFFF', 'line-width': 11 } }, before);
  map.addLayer({ id: 'ruta-linea', type: 'line', source: 'ruta', layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-color': '#2F5BEA', 'line-width': 7 } }, before);
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
    container: 'map',
    center: HOME.center, zoom: HOME.zoom, pitch: HOME.pitch, bearing: HOME.bearing, attributionControl: false,
    locale: { // textos accesibles de MapLibre en español
      'Map.Title': 'Mapa', 'Marker.Title': 'Marcador del mapa', 'Popup.Close': 'Cerrar',
      'AttributionControl.ToggleAttribution': 'Mostrar u ocultar atribución', 'AttributionControl.MapFeedback': 'Sugerir cambios en el mapa',
      'NavigationControl.ZoomIn': 'Acercar', 'NavigationControl.ZoomOut': 'Alejar', 'NavigationControl.ResetBearing': 'Orientar al norte',
      'GeolocateControl.FindMyLocation': 'Buscar mi ubicación', 'GeolocateControl.LocationNotAvailable': 'Ubicación no disponible',
      'FullscreenControl.Enter': 'Pantalla completa', 'FullscreenControl.Exit': 'Salir de pantalla completa',
      'LogoControl.Title': 'Logotipo de MapLibre', 'ScaleControl.Meters': 'm', 'ScaleControl.Kilometers': 'km',
      'CooperativeGesturesHandler.WindowsHelpText': 'Usa Ctrl + rueda para hacer zoom', 'CooperativeGesturesHandler.MacHelpText': 'Usa ⌘ + rueda para hacer zoom',
      'CooperativeGesturesHandler.MobileHelpText': 'Usa dos dedos para mover el mapa',
    },
  });
  // Estilo base (OpenFreeMap liberty). Sus filtros de escudos de carretera comparan ["get","ref_length"] con un
  // número y las vías sin ese dato daban avisos "Expected value to be of type number, but found null" en consola:
  // se cambia por ["coalesce", …, 99] (sin dato = sin escudo, el mismo resultado sin avisos).
  const fixNull = x => Array.isArray(x)
    ? (x[0] === 'get' && x[1] === 'ref_length' && x.length === 2 ? ['coalesce', x, 99] : x.map(fixNull)) : x;
  map.setStyle('https://tiles.openfreemap.org/styles/liberty', {
    transformStyle: (_, next) => ({ ...next, layers: next.layers.map(l => (l.filter ? { ...l, filter: fixNull(l.filter) } : l)) }),
  });
  map.addControl(new maplibregl.AttributionControl({ compact: true, customAttribution: 'Rutas: OSRM · FOSSGIS · Búsqueda: Photon (komoot)' }), 'bottom-left');
  map.on('error', e => console.warn('Mapa:', e.error?.message));
  map.on('load', async () => {
    addBuildings();
    await loadPoiIcons(map);                       // iconos de categoría dibujados en el propio mapa
    addPoiLayer(map, VSRC, currentTheme());
    addRouteLayers(); styleMap(currentTheme());
    // La atribución compacta empieza desplegada y tapa el botón €: se pliega (sigue accesible con la "i")
    $$('.maplibregl-ctrl-attrib').forEach(a => { a.classList.remove('maplibregl-compact-show'); a.removeAttribute('open'); });
    window.__mapReady = true;
    openIncomingLink(location.search + location.hash); // ¿se abrió la app desde una ubicación compartida?
  });
  map.on('click', () => { closeCatMenu(); if ($('#dock').classList.contains('open') && !S.poiCat) closeSearch(); });
  // Puntos de interés del mapa: al tocarlos se abre su ficha (sin precios) y se puede ir con el GPS
  map.on('click', 'poi-app', e => {
    const f = e.features?.[0]; if (!f || S.navigating) return;
    const s = poiToSpot(f), p = placeForSpot(s);
    p ? openPlace(p.id) : openDest(s); // si es un negocio con precios de la app, se abre su ficha de precios
  });
  map.on('mouseenter', 'poi-app', () => { map.getCanvas().style.cursor = 'pointer'; });
  map.on('mouseleave', 'poi-app', () => { map.getCanvas().style.cursor = ''; });
  // Mantener pulsado el mapa (o clic derecho) marca una ubicación cualquiera para ir hasta ella
  let lp = null;
  const cancelLp = () => { clearTimeout(lp); lp = null; };
  map.on('contextmenu', e => dropPin(e.lngLat));
  map.on('touchstart', e => { cancelLp(); if (e.originalEvent.touches.length === 1) { const ll = e.lngLat; lp = setTimeout(() => dropPin(ll), 550); } });
  map.on('touchmove', cancelLp); map.on('touchend', cancelLp); map.on('movestart', cancelLp);

  const me = document.createElement('div');
  me.className = 'me';
  me.innerHTML = '<div class="me-cone"></div><div class="me-dot"></div><svg class="me-arrow" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2.5 19.5 20.5 12 16.6 4.5 20.5Z"/></svg>';
  // rotationAlignment 'map': la rotación es un rumbo respecto al norte (0° = norte), aunque el mapa gire
  userMarker = new maplibregl.Marker({ element: me, rotationAlignment: 'map', pitchAlignment: 'viewport' }).setLngLat(USER_POS).addTo(map);
  applyCosmetics();
  updateMe();

  // Establecimientos con precios: logo del tipo de negocio (no el precio) con una insignia "€"
  for (const p of PLACES) addPlaceMarker(p);
  markersReady = true;
  applyMarkerVisibility();
  map.on('moveend', updatePinTabs); // pins fuera de la vista: no tabulables
  map.on('idle', applyPoiDedup);    // teselas nuevas: se recalcula qué iconos del mapa base repiten un sitio de la app
}
let markersReady = false;
function addPlaceMarker(p) {
  if (markers[p.id]) return;
  // El contenedor lo posiciona MapLibre (con transform); el .pin va dentro para que sus estilos
  // (escala al seleccionarlo, sombra…) no pisen esa posición y el marcador no se mueva con la cámara.
  const wrap = document.createElement('div');
  wrap.className = 'pin-wrap';
  const el = document.createElement('button');
  el.className = `pin ${p.tpl}${priced(p) ? '' : ' empty'}`;
  el.dataset.place = p.id;
  el.innerHTML = `${glyphSVG(PLACE_GLYPH[p.cat] || POI_CATS[p.poiCat]?.glyph, 'ic pin-glyph')}<i class="pin-eur" aria-hidden="true">€</i>`;
  el.addEventListener('click', e => { e.stopPropagation(); openPlace(p.id); });
  el.setAttribute('aria-label', `${p.name} (${p.cat}): ${pinLabel(p)}`);
  wrap.appendChild(el);
  // anchor 'bottom': la punta del marcador (parte baja del contenedor) cae justo en la coordenada del sitio
  markers[p.id] = new maplibregl.Marker({ element: wrap, anchor: 'bottom' }).setLngLat(p.pos).addTo(map);
  window.updateOfferTag?.(p); // extras.js puede cargarse después que los marcadores
  wrap.removeAttribute('aria-label'); wrap.removeAttribute('role'); // el botón de dentro ya lo describe
}
const pinOf = id => markers[id]?.getElement().querySelector('.pin');
// Negocio dado de alta a mano desde una cuenta de negocio
function addPlace(p) {
  PLACES.push(p);
  if (markersReady) { addPlaceMarker(p); applyMarkerVisibility(); }
}

// Marcador rojo del destino cuando no es un establecimiento con precios (dirección, lugar del mapa…)
let destMarker = null;
function showDestMarker(pos) {
  if (!map) return;
  if (!destMarker) {
    const el = document.createElement('div');
    el.className = 'dest-pin';
    el.innerHTML = glyphSVG('pin');
    destMarker = new maplibregl.Marker({ element: el, anchor: 'bottom' });
  }
  destMarker.setLngLat(pos).addTo(map);
}
function hideDestMarker() { destMarker?.remove(); }

function dropPin(ll) {
  if (S.navigating || S.flow || !map) return;
  const pos = [ll.lng, ll.lat];
  // El título será la dirección en cuanto llegue (fillAddress); mientras, "Buscando la dirección…"
  const spot = { kind: 'pin', key: `pin:${pos[0].toFixed(5)},${pos[1].toFixed(5)}`, name: 'Ubicación marcada', sub: '', resolving: true, pos };
  openDest(spot);
  fillAddress(spot);
}
function fillAddress(spot) { // completa la dirección de una ubicación (geocodificación inversa)
  reverseGeocode(spot.pos)
    .then(a => {
      const coords = `${spot.pos[1].toFixed(5)}, ${spot.pos[0].toFixed(5)}`;
      if (spot.kind === 'pin') { spot.name = a?.name || a?.sub || coords; spot.sub = a?.name ? a.sub || '' : ''; } // la dirección pasa a ser el título
      else spot.sub = a ? [a.name, a.sub].filter(Boolean).join(', ') : coords;
    })
    .catch(() => { const coords = `${spot.pos[1].toFixed(5)}, ${spot.pos[0].toFixed(5)}`; if (spot.kind === 'pin') spot.name = coords; else spot.sub = coords; })
    .finally(() => { delete spot.resolving; if (S.dest === spot) renderPlace(); });
}
// Ubicación compartida al abrir la app: ?ll=LAT,LNG&name=… · ?q=<enlace de WhatsApp/Google Maps> · #geo:LAT,LNG
function openIncomingLink(text) {
  if (!text || text.length < 3) return;
  const params = new URLSearchParams(location.search);
  const loc = parseLocation(params.get('ll') || params.get('q') || text);
  if (!loc) return;
  openShared(loc.pos, params.get('name'));
}
function openShared(pos, name) {
  const spot = { kind: 'shared', key: `shared:${pos[0].toFixed(5)},${pos[1].toFixed(5)}`, name: name || 'Ubicación compartida', sub: 'Buscando la dirección…', pos };
  openDest(spot);
  fillAddress(spot);
  return spot;
}

function applyCosmetics() {
  const el = userMarker?.getElement(); if (!el) return;
  el.dataset.skin = S.equipped.flecha;
  if (map?.isStyleLoaded()) styleRoute();
}

// ---------- Indicador de posición: círculo, cono (brújula) o flecha (coche) ----------
// - Navegando en coche: flecha que apunta hacia donde vas (rumbo de la ruta).
// - Navegando a pie: círculo con un cono translúcido hacia donde mira el móvil (brújula);
//   si el dispositivo no tiene brújula (ordenador), el cono sigue la dirección de la ruta.
// - Sin navegar: círculo, con cono si hay datos de brújula (como en Mapas de Apple).
function updateMe() {
  const el = userMarker?.getElement(); if (!el) return;
  const nav = S.navigating;
  let mode = 'dot', rot = 0;
  if (nav && nav.mode === 'coche') { mode = 'arrow'; rot = nav.heading; }
  else if (nav) { mode = 'cone'; rot = S.compass ?? nav.heading; }
  else if (S.compass != null) { mode = 'cone'; rot = S.compass; }
  el.dataset.mode = mode;
  userMarker.setRotation(rot || 0);
}

// Brújula del móvil. iOS da el rumbo en webkitCompassHeading (pide permiso con un toque del usuario);
// Android da "deviceorientationabsolute" con alpha (sentido antihorario) → rumbo = 360 − alpha.
// Requiere HTTPS (o localhost) en el móvil.
function onOrientation(e) {
  let h = null;
  if (typeof e.webkitCompassHeading === 'number') h = e.webkitCompassHeading;
  else if (e.absolute && typeof e.alpha === 'number') h = 360 - e.alpha;
  if (h == null) return;
  h += screen.orientation?.angle || 0; // móvil en horizontal
  S.compass = (h + 360) % 360;
  updateMe();
}
let compassOn = false;
function startCompass() { // llamar desde un toque del usuario (requisito de iOS)
  if (compassOn) return;
  const listen = () => {
    compassOn = true;
    window.addEventListener('deviceorientationabsolute', onOrientation);
    window.addEventListener('deviceorientation', onOrientation);
  };
  const DOE = window.DeviceOrientationEvent;
  if (DOE && typeof DOE.requestPermission === 'function') DOE.requestPermission().then(r => { if (r === 'granted') listen(); }).catch(() => {});
  else listen();
}

// ---------- Desplegable de categorías ----------
// Desplegable "Precios": por defecto no se muestra ningún marcador de precio (pantalla limpia);
// solo aparecen si el usuario lo elige aquí.
function renderCatMenu() {
  // Contadores reales: cuántos sitios hay y cuántos tienen ya precio
  const count = list => { const m = list.filter(priced).length; return `${list.length} ${list.length === 1 ? 'sitio' : 'sitios'} · ${m} con precio`; };
  const opts = [['ninguno', 'Ninguno', ''], ['todos', 'Todos', count(PLACES)], ...Object.keys(TPL).map(k => [k, TPL[k].menu, count(PLACES.filter(p => p.tpl === k))])];
  // Lista de opciones (role=listbox): el título ya es el nombre de la lista; las opciones se recorren con
  // las flechas (tabindex=-1, el foco lo mueve el script)
  $('#catMenu').innerHTML = `<div class="menu-title" aria-hidden="true">Mostrar precios en el mapa</div>` + opts.map(([k, l, n]) =>
    `<button role="option" tabindex="-1" aria-selected="${S.filter === k}" class="${S.filter === k ? 'on' : ''}" data-cat="${k}"><span class="dot-cat ${k}"></span><span class="cat-opt">${l}${n ? `<small>${n}</small>` : ''}</span></button>`).join('');
}
// focus: llevar el foco a la opción elegida (al abrir con teclado)
function openCatMenu(focus = false) {
  renderCatMenu(); $('#catMenu').hidden = false; $('#catBtn').setAttribute('aria-expanded', 'true');
  if (focus) ($('#catMenu [aria-selected="true"]') || $('#catMenu [role="option"]'))?.focus();
}
function closeCatMenu(refocus = false) {
  const was = !$('#catMenu').hidden, inside = $('#catMenu').contains(document.activeElement);
  $('#catMenu').hidden = true; $('#catBtn').setAttribute('aria-expanded', 'false');
  if (was && (refocus || inside)) $('#catBtn').focus({ preventScroll: true }); // el foco no se queda en una opción oculta
}
// Teclado en la lista: flechas, Inicio/Fin, Escape (Intro y Espacio ya pulsan el botón de la opción)
function onCatMenuKey(e) {
  const opts = $$('#catMenu [role="option"]'), i = opts.indexOf(document.activeElement);
  const go = j => { e.preventDefault(); opts[(j + opts.length) % opts.length].focus(); };
  if (e.key === 'ArrowDown') go(i + 1);
  else if (e.key === 'ArrowUp') go(i < 0 ? opts.length - 1 : i - 1);
  else if (e.key === 'Home') go(0);
  else if (e.key === 'End') go(opts.length - 1);
  else if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); closeCatMenu(true); }
  else if (e.key === 'Tab') closeCatMenu();
}
function applyMarkerVisibility() {
  for (const p of PLACES) {
    const chosen = S.filter !== 'ninguno' && (S.filter === 'todos' || p.tpl === S.filter) && (!S.poiCat || p.poiCat === S.poiCat);
    // el sitio abierto en la ficha o al que se navega se ve siempre, aunque los precios estén ocultos
    const show = chosen || p.id === S.place || p.id === S.navigating?.id;
    markers[p.id]?.getElement().toggleAttribute('hidden', !show);
  }
  applyPoiDedup(); updatePinTabs();
}
// Con los marcadores de la app a la vista, se oculta el icono del mapa base del mismo sitio
// (mismo nombre normalizado y a menos de 80 m) para que no salga dos veces. Se combina con el filtro
// de categoría de lugares.js (setPoiLayerCat), que se vuelve a leer cada vez.
// Se excluyen por id de elemento (no por nombre) para no ocultar otra sucursal con el mismo nombre.
const DEDUP_TAG = -424242; // id ficticio que marca la condición añadida aquí
function applyPoiDedup() {
  if (!map?.getLayer('poi-app') || !VSRC) return;
  const shown = PLACES.filter(p => markers[p.id] && !markers[p.id].getElement().hasAttribute('hidden'));
  const names = new Set(); // ids de los elementos repetidos
  if (shown.length) for (const f of map.querySourceFeatures(VSRC, { sourceLayer: 'poi' })) {
    const pr = f.properties || {}, nm = pr['name:es'] || pr.name;
    if (!nm || f.id == null || f.geometry.type !== 'Point' || names.has(f.id)) continue;
    const n = normTxt(nm), pos = f.geometry.coordinates;
    if (shown.some(p => normTxt(p.name) === n && distM(p.pos, pos) < 80)) names.add(f.id);
  }
  const cur = map.getFilter('poi-app'); let base = cur;
  if (Array.isArray(cur) && cur[0] === 'all' && cur.length === 3 && JSON.stringify(cur[2]).includes(String(DEDUP_TAG))) base = cur[1];
  const want = names.size
    ? ['all', base, ['!', ['in', ['id'], ['literal', [DEDUP_TAG, ...[...names].sort((a, b) => a - b)]]]]]
    : base;
  if (JSON.stringify(want) !== JSON.stringify(cur)) map.setFilter('poi-app', want);
}
// Marcadores fuera de la parte visible del mapa: fuera del orden de tabulación
function updatePinTabs() {
  if (!map) return;
  const c = map.getContainer(), W = c.clientWidth, H = c.clientHeight;
  for (const p of PLACES) {
    const el = pinOf(p.id); if (!el) continue;
    const pt = map.project(p.pos);
    el.tabIndex = pt.x >= 0 && pt.y >= 0 && pt.x <= W && pt.y <= H ? 0 : -1;
  }
}
function setFilter(k) {
  S.filter = k;
  applyMarkerVisibility();
  $('#catLabel').textContent = k === 'ninguno' ? 'Ver precios' : k === 'todos' ? 'Todos los precios' : TPL[k].menu;
  $('#catBtn').setAttribute('aria-label', k === 'ninguno' ? 'Ver precios en el mapa' : `Precios en el mapa: ${$('#catLabel').textContent}`);
  $('#catDot').className = `dot-cat ${k}`;
  $('#catBtn').classList.toggle('off', k === 'ninguno');
  closeCatMenu();
}
// "Encuentra cerca": muestra en el mapa solo esa categoría y la lista de sitios cercanos a media altura
function setPoiCat(cat) {
  S.poiCat = cat;
  setPoiLayerCat(map, cat);
  applyMarkerVisibility();
  if (cat) {
    $('#searchInput').value = '';
    const h = $('#app').getBoundingClientRect().height;
    openSearch(Math.round(h * 0.5));
    map?.easeTo({ center: USER_POS, zoom: 15.6, duration: motionMs(600) });
    // las teselas de alrededor tardan un momento en cargarse: se vuelve a pintar la lista al terminar
    map?.once('idle', () => { if (S.poiCat === cat && $('#dock').classList.contains('open')) renderSearch(); });
  }
}

// ---------- Buscador (se despliega hacia arriba) ----------
let panelT;
// top = null → abierto del todo; top = número → se queda a esa altura (posición intermedia)
function openSearch(top = null) {
  clearTimeout(panelT);
  closePlace(); closeCatMenu();
  const dock = $('#dock');
  dock.classList.add('open'); $('#mapUI').classList.add('searching');
  dock.style.top = top == null ? '' : `${top}px`;
  $('#avatarBtn').hidden = true; $('#searchCancel').hidden = false; $('#searchPanel').hidden = false;
  renderSearch();
}
function closeSearch() {
  if (S.poiCat) { S.poiCat = null; setPoiLayerCat(map, null); applyMarkerVisibility(); }
  const si = $('#searchInput'); si.value = ''; si.blur();
  $('#dock').removeAttribute('style');
  $('#dock').classList.remove('open'); $('#mapUI').classList.remove('searching');
  $('#avatarBtn').hidden = false; $('#searchCancel').hidden = true;
  // el contenido se oculta al terminar de bajar, para que no desaparezca a mitad de la animación
  clearTimeout(panelT); panelT = setTimeout(() => { if (!$('#dock').classList.contains('open')) $('#searchPanel').hidden = true; }, 340);
}

// Arrastre del buscador (como en Mapas de Apple):
//  - la hoja sigue al dedo y se QUEDA donde la sueltas;
//  - lanzamiento rápido hacia arriba → se abre del todo; rápido hacia abajo → se cierra;
//  - si la sueltas muy cerca de abajo se pliega, y muy cerca de arriba se abre del todo;
//  - se puede arrastrar desde el asa, desde la fila del buscador y desde la propia lista.
const SHEET = { OPEN_TOP: 64, CLOSED_H: 76, FLICK: 0.6, CLOSE_BELOW: 0.15, OPEN_ABOVE: 0.88 };
function setupDockDrag() {
  const dock = $('#dock'), app = $('#app'), panel = $('#searchPanel'), ui = $('#mapUI');
  const isFull = () => dock.classList.contains('open') && !dock.style.top;
  let d = null, noClickUntil = 0;

  function begin(y) {
    const ar = app.getBoundingClientRect();
    d = { y0: y, top0: dock.getBoundingClientRect().top - ar.top, closedTop: ar.height - SHEET.CLOSED_H, top: 0, k: 0, v: 0, py: y, pt: performance.now() };
    dock.classList.add('dragging'); clearTimeout(panelT);
    if (!dock.classList.contains('open')) { closePlace(); closeCatMenu(); renderSearch(); panel.hidden = false; }
  }
  function move(y) {
    const top = d.top = Math.min(d.closedTop, Math.max(SHEET.OPEN_TOP, d.top0 + (y - d.y0)));
    const k = d.k = (d.closedTop - top) / (d.closedTop - SHEET.OPEN_TOP);
    const s = Math.min(1, k / 0.15); // pasa enseguida de "cápsula" a "hoja" de ancho completo
    const r = 28 - 2 * s, rb = 28 * (1 - s);
    Object.assign(dock.style, { top: `${top}px`, left: `${10 * (1 - s)}px`, right: `${10 * (1 - s)}px`, bottom: `${14 * (1 - s)}px`, borderRadius: `${r}px ${r}px ${rb}px ${rb}px` });
    ui.classList.toggle('searching', k > 0.1);
    const now = performance.now();
    if (now - d.pt > 4) { d.v = (y - d.py) / (now - d.pt); d.py = y; d.pt = now; }
  }
  function end() {
    if (!d) return;
    const { k, top } = d;
    const v = performance.now() - d.pt > 90 ? 0 : d.v; // si el dedo se paró antes de soltar, no es un lanzamiento
    d = null;
    noClickUntil = performance.now() + 120; // que soltar no cuente como pulsar un resultado
    dock.classList.remove('dragging'); dock.removeAttribute('style'); // la transición CSS termina el movimiento
    const to = v < -SHEET.FLICK ? 'full' : v > SHEET.FLICK ? 'close'
      : k < SHEET.CLOSE_BELOW ? 'close' : k > SHEET.OPEN_ABOVE ? 'full' : 'partial';
    if (to === 'close') closeSearch();
    else openSearch(to === 'full' ? null : top);
  }

  // Asa y fila del buscador (dedo y ratón). El arrastre puede empezar en cualquier punto de la barra,
  // también sobre el campo de texto: si el dedo se mueve más de 6 px es un arrastre (se quita el foco
  // y no sale el teclado); si no se mueve, es un toque normal para escribir.
  // (Antes, con la hoja abierta se ignoraba el arrastre sobre el campo y solo respondía el asa:
  //  tras el primer deslizamiento no se podía volver a deslizar.)
  let p = null;
  dock.addEventListener('pointerdown', e => {
    if (e.button > 0 || e.target.closest('#avatarBtn, #searchCancel, #searchPanel')) return;
    p = { id: e.pointerId, y: e.clientY, on: false };
  });
  dock.addEventListener('pointermove', e => {
    if (!p || e.pointerId !== p.id) return;
    if (!p.on) {
      if (Math.abs(e.clientY - p.y) < 6) return;
      p.on = true; dock.setPointerCapture(e.pointerId);
      $('#searchInput').blur(); getSelection()?.removeAllRanges();
      begin(p.y);
    }
    move(e.clientY);
  });
  const pEnd = () => { if (p?.on) end(); p = null; };
  dock.addEventListener('pointerup', pEnd); dock.addEventListener('pointercancel', pEnd);

  // Lista con el dedo: si la hoja no está abierta del todo, arrastrar mueve la hoja;
  // si está abierta, la lista se desplaza normal y solo baja la hoja al tirar hacia abajo estando arriba del todo.
  const decide = dy => !isFull() || (panel.scrollTop <= 0 && dy > 0);
  let t = null;
  panel.addEventListener('touchstart', e => { t = { y: e.touches[0].clientY, decided: false, drag: false }; }, { passive: true });
  panel.addEventListener('touchmove', e => {
    if (!t) return;
    const y = e.touches[0].clientY;
    if (!t.decided) { if (Math.abs(y - t.y) < 6) return; t.decided = true; t.drag = decide(y - t.y); if (t.drag) { $('#searchInput').blur(); begin(t.y); } }
    if (t.drag) { e.preventDefault(); move(y); }
  }, { passive: false });
  const tEnd = () => { if (t?.drag) end(); t = null; };
  panel.addEventListener('touchend', tEnd); panel.addEventListener('touchcancel', tEnd);

  // Lista con ratón (para probar en el ordenador)
  let m = null;
  panel.addEventListener('pointerdown', e => { if (e.pointerType === 'mouse' && e.button === 0) m = { id: e.pointerId, y: e.clientY, decided: false, drag: false }; });
  panel.addEventListener('pointermove', e => {
    if (!m || e.pointerId !== m.id) return;
    if (!m.decided) { if (Math.abs(e.clientY - m.y) < 6) return; m.decided = true; m.drag = decide(e.clientY - m.y); if (m.drag) { panel.setPointerCapture(e.pointerId); begin(m.y); } }
    if (m.drag) move(e.clientY);
  });
  const mEnd = () => { if (m?.drag) end(); m = null; };
  panel.addEventListener('pointerup', mEnd); panel.addEventListener('pointercancel', mEnd);
  panel.addEventListener('click', e => { if (performance.now() < noClickUntil) { e.stopPropagation(); e.preventDefault(); } }, true);
}
// Filas de resultados. Los destinos sin precio se guardan en RESULTS para recuperarlos al pulsar.
const RESULTS = new Map();
function resultRow(p, sub, icon) {
  return `<button class="result" data-place="${p.id}">${icon || placeIconHTML(p)}<span>${p.name}<small>${sub}</small></span><b${priced(p) ? '' : ' class="no-price"'}>${pinLabel(p)}</b></button>`;
}
// Búsqueda por servicio: el primer concepto cuyo nombre contiene el texto (n ya normalizado)
const itemMatch = (p, n) => p.tpl === 'hosteleria' ? null : p.items.find(i => normTxt(i.n).includes(n) && (i.p != null || i.ask)) || p.items.find(i => normTxt(i.n).includes(n));
function itemPriceText(p, i) {
  if (i.p == null) return i.ask ? 'a consultar' : 'sin precio';
  return `${i.desde && i.p === i.declared ? 'desde ' : ''}${fmtEur(i.p)}`;
}
function spotRow(s, sub) {
  RESULTS.set(s.key, s);
  return `<button class="result" data-key="${s.key}">${poiIconHTML(s.cat)}<span>${s.name}<small>${sub ?? [s.sub, fmtDist(distM(USER_POS, s.pos))].filter(Boolean).join(' · ')}</small></span></button>`;
}
let addrCtl = null, addrT = null;
function renderSearch() {
  const q = $('#searchInput').value.trim(), box = $('#searchPanel');
  if (!q && S.poiCat) return renderNear(S.poiCat);
  if (q) { // buscador normal: ubicación compartida, sitios con precio, categorías, lugares del mapa y direcciones
    const n = normTxt(q);
    const loc = parseLocation(q);
    // Por nombre y tipo, y también por servicio (nombres de los conceptos) y por la descripción del negocio
    const byName = p => normTxt(`${p.name} ${p.cat}`).includes(n);
    const places = PLACES.filter(p => byName(p) || itemMatch(p, n) || (p.about && normTxt(p.about).includes(n)))
      .sort((a, b) => (priced(b) - priced(a)) || (byName(b) - byName(a)) || distM(USER_POS, a.pos) - distM(USER_POS, b.pos)).slice(0, 8);
    const placeSub = p => {
      const i = !byName(p) && itemMatch(p, n), d = fmtDist(distM(USER_POS, p.pos));
      return i ? `${i.n}${i.p != null || i.ask ? ` · ${itemPriceText(p, i)}` : ''} · ${d}` : `${p.cat} · ${d}`;
    };
    const cats = Object.entries(POI_CATS).filter(([, c]) => normTxt(`${c.label} ${c.one}`).includes(n));
    const pois = poisByName(map, VSRC, q).filter(s => !placeForSpot(s));
    let html = '';
    if (loc) {
      const s = { kind: 'shared', key: `shared:${loc.pos[0].toFixed(5)},${loc.pos[1].toFixed(5)}`, name: 'Ubicación compartida', sub: `${loc.pos[1].toFixed(5)}, ${loc.pos[0].toFixed(5)}`, pos: loc.pos };
      html += `<h2 class="sp-title">Ubicación compartida</h2>${spotRow(s)}`;
    } else if (isShortMapLink(q)) {
      html += `<div class="note">${ICON.info}<span>Los enlaces cortos de Google Maps (maps.app.goo.gl) no se pueden abrir desde el navegador. En la app instalada se abrirían con "Compartir → PrecioLocal". Prueba con el enlace largo o con las coordenadas.</span></div>`;
    }
    if (places.length) html += `<h2 class="sp-title">En PrecioLocal</h2>${places.map(p => resultRow(p, placeSub(p))).join('')}`;
    if (cats.length) html += `<h2 class="sp-title">Encuentra cerca</h2>${cats.map(([k, c]) => `<button class="result" data-near="${k}">${poiIconHTML(k)}<span>${c.label}<small>Ver los más cercanos</small></span></button>`).join('')}`;
    if (pois.length) html += `<h2 class="sp-title">Lugares</h2>${pois.map(s => spotRow(s)).join('')}`;
    if (!loc) html += `<h2 class="sp-title">Direcciones</h2><div id="addrResults"><p class="empty-note">Buscando direcciones…</p></div>`;
    box.innerHTML = html;
    if (!loc) searchAddr(q);
    return;
  }
  const favs = [...S.favs.map(id => ({ place: placeById(id) })), ...S.spots.map(s => ({ spot: s }))];
  box.innerHTML = `
    <button class="sp-action" data-act="paste">${glyphSVG('link')}<span>Abrir una ubicación compartida<small>Pega un enlace de WhatsApp o Google Maps, o unas coordenadas</small></span></button>
    <h2 class="sp-title">Favoritos <small>${favs.length}</small></h2>
    <div class="favs">${favs.map(f => f.place
      ? `<button class="fav" data-place="${f.place.id}"><i class="tpl-${f.place.tpl}" aria-hidden="true">${tplIcon(f.place)}</i>${f.place.name}</button>`
      : (RESULTS.set(f.spot.key, f.spot), `<button class="fav" data-key="${f.spot.key}"><i class="spot">${glyphSVG(f.spot.kind === 'poi' ? POI_CATS[f.spot.cat]?.glyph : 'home')}</i>${f.spot.name}</button>`)).join('')
      || '<p class="empty-note">Marca sitios con el corazón para verlos aquí.</p>'}</div>
    <h2 class="sp-title">Encuentra cerca</h2>
    <div class="near-list">${Object.entries(POI_CATS).filter(([, c]) => c.near).map(([k, c]) => `<button data-near="${k}">${poiIconHTML(k)}<span>${c.label}</span></button>`).join('')}</div>
    <h2 class="sp-title">Frecuentes y visitados</h2>
    ${S.visits.length ? S.visits.map(v => resultRow(placeById(v.id), `Visitado ${v.when}${v.n > 1 ? ` · ${v.n} veces` : ''}`)).join('') : '<p class="empty-note">Aún no has visitado ningún sitio.</p>'}`;
}
// Lista de "Encuentra cerca": primero los sitios con precios de esa categoría, luego los del mapa
function renderNear(cat) {
  const c = POI_CATS[cat];
  const places = PLACES.filter(p => p.poiCat === cat);
  const pois = poisNear(map, VSRC, cat).filter(s => !placeForSpot(s)); // sin repetir los que ya están en la app
  $('#searchPanel').innerHTML = `
    <div class="near-head"><button class="back-mini" data-clear-cat aria-label="Volver">${ICON.back}</button>${poiIconHTML(cat)}<h2 tabindex="-1">${c.label}</h2><small>${places.length + pois.length}</small></div>
    ${places.length ? `<h3 class="sp-title">En PrecioLocal</h3>${places.sort((a, b) => distM(USER_POS, a.pos) - distM(USER_POS, b.pos)).map(p => resultRow(p, `${p.cat} · ${fmtDist(distM(USER_POS, p.pos))}`)).join('')}` : ''}
    <h3 class="sp-title">Cerca de ti</h3>
    ${pois.map(s => spotRow(s)).join('') || '<p class="empty-note">No hay ninguno en la zona cargada del mapa. Acerca o mueve el mapa.</p>'}`;
}
// Direcciones con Photon (con espera entre teclas para no saturar el servicio)
function searchAddr(q) {
  clearTimeout(addrT); addrCtl?.abort();
  addrT = setTimeout(async () => {
    addrCtl = new AbortController();
    try {
      const list = await searchAddresses(q, addrCtl.signal);
      if ($('#searchInput').value.trim() !== q) return;
      const box = $('#addrResults'); if (!box) return;
      box.innerHTML = list.map(s => spotRow(s)).join('') || '<p class="empty-note">No se ha encontrado ninguna dirección.</p>';
    } catch (e) {
      if (e.name === 'AbortError') return;
      const box = $('#addrResults'); if (box) box.innerHTML = '<p class="empty-note">No se pueden buscar direcciones sin conexión.</p>';
    }
  }, 350);
}
async function pasteLocation() {
  let text = '';
  try { text = await navigator.clipboard.readText(); } catch { /* sin permiso para leer el portapapeles */ }
  const loc = parseLocation(text);
  if (loc) { closeSearch(); return openShared(loc.pos); }
  const si = $('#searchInput');
  if (!$('#dock').classList.contains('open')) openSearch();
  si.focus();
  toast(isShortMapLink(text) ? 'Ese enlace corto no se puede abrir aquí: usa el enlace largo' : 'Pega el enlace o las coordenadas en el buscador');
}

// ---------- Ficha del establecimiento ----------
function openPlace(id) { openDest(placeDest(placeById(id))); }
// Abre la ficha de cualquier destino: establecimiento con precios, lugar del mapa, dirección,
// ubicación compartida o ubicación marcada en el mapa. Desde la ficha se puede ir con el GPS.
let sheetOpener = null; // control que abrió la ficha (pin, fila de resultados…): recibe el foco al cerrarla
function openDest(d) {
  if (S.navigating) return;
  const a = document.activeElement, wasOpen = !$('#placeSheet').hidden;
  if (!wasOpen) sheetOpener = a && a !== document.body && !$('#placeSheet').contains(a) ? a : null;
  S.dest = d; S.place = d.kind === 'place' ? d.id : null;
  S.travelMode = 'coche'; // el coche es la opción principal; se puede cambiar a pie en la ficha
  closeCatMenu(); if ($('#dock').classList.contains('open')) closeSearch();
  $('#app').classList.add('place-open');
  $$('.pin').forEach(el => el.classList.toggle('selected', el.dataset.place === S.place));
  applyMarkerVisibility();
  if (d.kind === 'place') hideDestMarker(); else showDestMarker(d.pos);
  renderPlace();
  $('#placeSheet').hidden = false; $('#placeSheet').scrollTop = 0;
  // El foco va al título de la ficha (no es modal: el resto de la app sigue disponible). Si hay una
  // ventana encima (p. ej. el modo negocio se abre justo después), esa ventana se queda con el foco.
  if (!topModal()) $('#sheetTitle')?.focus({ preventScroll: true });
  // El sitio queda centrado en la parte del mapa que no tapa la ficha (desplazamiento en píxeles,
  // válido con el mapa inclinado o girado)
  if (map) {
    const h = map.getContainer().clientHeight, sheet = Math.min($('#placeSheet').offsetHeight, h * 0.62);
    map.easeTo({ center: d.pos, offset: [0, -sheet / 2], zoom: Math.max(map.getZoom(), 15.5), duration: motionMs(500) });
  }
  // se piden ya las rutas en coche y a pie para mostrar los tiempos reales en la ficha
  for (const m of ['coche', 'pie']) getRoute(USER_POS, d.pos, m).then(() => { if (S.dest === d && !$('#placeSheet').hidden) renderPlace(); });
}
// Cerrar la ficha con Escape o con la X: el foco vuelve al pin o fila que la abrió (o al buscador)
function closePlaceKb() {
  const id = S.place;
  closePlace();
  focusBack(sheetOpener, id && pinOf(id), '#searchInput', '#mapTitle');
  sheetOpener = null;
}
function modePickHTML(d) {
  // Fila de acciones bajo la cabecera (como en Mapas de Apple): coche / a pie con sus minutos y el botón "Ir"
  return `<div class="go-row"><div class="mode-pick" role="radiogroup" aria-label="Cómo vas">
      ${[['coche', 'En coche', ICON.car], ['pie', 'A pie', ICON.walk]].map(([m, l, ic]) =>
        `<button role="radio" aria-checked="${S.travelMode === m}" aria-label="${l}, ${etaMin(d, m)} min" class="${S.travelMode === m ? 'on' : ''}" data-mode="${m}">${ic}<span>${etaMin(d, m)} min<small>${l}</small></span></button>`).join('')}
    </div>
    <button class="btn primary" data-act="go">${ICON.nav} ${S.travelMode === 'coche' ? 'Ir en coche' : 'Ir a pie'}</button></div>`;
}
const isSaved = d => d.kind === 'place' ? S.favs.includes(d.id) : S.spots.some(s => s.key === d.key);
function favBtnHTML(d) {
  const fav = isSaved(d);
  return `<button class="fav-btn ${fav ? 'on' : ''}" data-act="fav" aria-pressed="${fav}" aria-label="${fav ? 'Quitar de favoritos' : 'Añadir a favoritos'}">${ICON.heart}</button>`;
}
// Ficha de un destino sin precios (dirección, lugar del mapa, ubicación compartida o marcada)
function renderSpot(d) {
  const c = POI_CATS[d.cat];
  const kindLabel = d.kind === 'poi' ? c?.one : d.kind === 'shared' ? 'Ubicación compartida' : d.kind === 'pin' ? 'Ubicación marcada' : 'Dirección';
  const note = d.pub
    ? `<div class="note">${ICON.info}<span>Centro sanitario público: la app solo indica dónde está y cómo llegar. Aquí no se muestran precios.</span></div>`
    : d.kind === 'poi'
    ? `<div class="note">${ICON.info}<span>Aún no hay precios de este sitio en PrecioLocal. Puedes ir igualmente con el GPS.</span></div>`
    : `<div class="note">${ICON.shield}<span>Las direcciones y ubicaciones a las que vas no se guardan en tu historial ni se usan para pedirte precios.</span></div>`;
  const title = d.resolving ? 'Buscando la dirección…' : d.name;
  const sh = $('#placeSheet');
  sh.innerHTML = `
    <div class="sheet-head"><div class="spot-head">${poiIconHTML(d.cat)}<div><div class="cat">${kindLabel}</div><h2>${title}</h2>
    <div class="meta">${fmtDist(distM(USER_POS, d.pos))}${d.sub && d.sub !== c?.one ? ` · ${d.sub}` : ''}</div></div></div>
    <div class="head-btns">${favBtnHTML(d)}<button class="close" data-act="close" aria-label="Cerrar">${ICON.x}</button></div></div>
    ${planNextHTML(d)}${modePickHTML(d)}${note}${planBtnHTML(d)}`;
  sheetA11y(sh);
}
// Iconos decorativos de la ficha ocultos a los lectores de pantalla (el texto de al lado ya lo dice)
// El título (h2) da nombre a la ficha (aria-labelledby="sheetTitle") y recibe el foco al abrirla
function sheetA11y(sh) {
  sh.querySelectorAll('svg').forEach(s => s.setAttribute('aria-hidden', 'true'));
  const h = sh.querySelector('h2'); if (h) { h.id = 'sheetTitle'; h.tabIndex = -1; }
}
// Al volver a pintar la ficha (favorito, avísame, coche/a pie, plan…) el foco sigue en el mismo control
function renderPlace() { keepFocus($('#placeSheet'), drawPlace, '#sheetTitle'); }
function drawPlace() {
  const d = S.dest; if (!d) return;
  if (d.kind !== 'place') return renderSpot(d);
  const p = placeById(d.id);
  const dist = fmtDist(distM(USER_POS, p.pos));
  let price = '';
  if (!priced(p)) {
    // Negocio real sin precios: lo declara el dueño o lo aporta el primer cliente que vaya con el GPS
    price = `<div class="empty-price"><b>Aún no hay precios</b>
      <span>Ve con el GPS de la app y, al rato de llegar, te preguntaremos cuánto pagaste: serás el primero en aportarlo${p.tpl === 'hosteleria' ? '' : ` (${p.items.map(i => i.n.toLowerCase()).join(', ')}…)`}.</span></div>`;
  } else if (p.tpl === 'hosteleria') {
    price = `<div class="range"><strong>${fmtRange(p)}</strong><span>por persona</span>${freshDot(p, p.rangeAt)}${p.rangeReview ? reviewChip : ''}</div>
      <p class="hint">${nReports(p) >= MIN_REPORTS ? 'Rango calculado con el 50 % central de lo que gastan los clientes.' : 'Rango provisional: se ajusta con lo que gastan los clientes.'}</p>`;
  } else {
    // Servicios con precio y, si el negocio lo indica, los "a consultar". Agrupados por área si los hay.
    const shown = p.items.filter(i => i.p != null || i.ask);
    const row = i => {
      const fromWeb = i.desde && i.p === i.declared; // "desde" solo mientras se muestra el precio publicado
      const small = [i.u === 'h' && p.tpl === 'ocio' ? 'precio total del espacio' : '', i.p === i.declared ? i.nota : ''].filter(Boolean).join(' · ');
      return `<li class="row"><div>${i.n}${i.review ? reviewChip : ''}${small ? `<small>${small}</small>` : ''}</div>
        ${i.p == null ? '<b class="ask">A consultar</b>' : `<b>${freshDot(p, i.at)}${fromWeb ? '<em>desde</em> ' : ''}${fmtEur(i.p)}<span>${UNIT[i.u]}</span></b>`}</li>`;
    };
    // Grupos como encabezados y conceptos como lista (lectores de pantalla), con el mismo aspecto
    const groups = [...new Set(shown.map(i => i.g))];
    price = groups.length > 1 || groups[0]
      ? groups.map(g => `<h3 class="rows-group">${g || 'Otros'}</h3><ul class="rows">${shown.filter(i => i.g === g).map(row).join('')}</ul>`).join('')
      : `<ul class="rows">${shown.map(row).join('')}</ul>`;
  }
  const acc = S.account, mine = acc?.type === 'negocio' && acc.business === p.id;
  const owner = mine
    ? `<button class="owner-row" data-act="owner">${ICON.building}<span>Editar los precios de tu negocio<small>Los clientes los confirmarán o corregirán con lo que paguen</small></span>${ICON.chev}</button>`
    : acc?.type === 'negocio' ? '' // una cuenta de negocio solo gestiona su propio negocio
    : `<button class="owner-row" data-act="claim">${ICON.building}<span>¿Es tu negocio? Crea tu cuenta de negocio<small>Declara tus precios iniciales; los clientes los confirmarán con lo que paguen</small></span>${ICON.chev}</button>`;
  let ratings = '';
  if (TPL[p.tpl].ratings && p.ratings) { // sin valoraciones todavía: no se muestra el bloque
    ratings = (() => {
      const avg = (p.ratings.reduce((a, b) => a + b, 0) / p.ratings.length).toFixed(1).replace('.', ',');
      return `<div class="ratings"><h3>Valoración ${avg} <small>${p.nRatings} ${p.nRatings === 1 ? 'valoración' : 'valoraciones'}</small></h3>
      ${TPL[p.tpl].ratings.map((c, i) => `<div class="rating"><span>${c}</span><div class="bar"><i style="width:${p.ratings[i] / 5 * 100}%"></i></div><b>${p.ratings[i].toFixed(1).replace('.', ',')}</b></div>`).join('')}</div>`;
    })();
  }
  const note = p.tpl === 'servicio'
    ? `<div class="note">${ICON.shield}<span>En servicios locales mostramos solo precios, sin reseñas.${p.sensitive ? ' Las aportaciones en esta categoría son anónimas.' : ''}</span></div>` : '';
  const alert = S.alerts.includes(p.id);
  const sh = $('#placeSheet');
  // Cabecera → fila "Ir" (visible sin desplazar) → descripción, ofertas y precios
  sh.innerHTML = `
    <div class="sheet-head"><div class="spot-head">${placeIconHTML(p)}<div><div class="cat">${p.cat}</div><h2>${p.name}</h2>
    <div class="meta">${dist}${p.addr ? ` · ${p.addr}` : ''}</div></div></div>
    <div class="head-btns">${favBtnHTML(d)}<button class="close" data-act="close" aria-label="Cerrar">${ICON.x}</button></div></div>
    ${planNextHTML(d)}
    ${modePickHTML(d)}
    ${p.about ? `<p class="about">${p.about}</p>` : ''}
    ${offersHTML(p, mine)}
    ${price}${priced(p) ? srcHTML(p) + freshHTML(p) : ''}${mine ? '' : reportLinkHTML(p)}
    <button class="alert-row" data-act="alert" role="switch" aria-checked="${alert}">${ICON.bell}<span>${priced(p) ? 'Avísame si cambia el precio' : 'Avísame cuando tenga precio'}<small>${priced(p) ? 'Te avisamos cuando los clientes paguen un precio distinto' : 'Te avisamos cuando el negocio o un cliente lo aporte'}</small></span><i class="switch" aria-hidden="true" aria-checked="${alert}"></i></button>
    ${ratings}${note}
    ${planBtnHTML(d)}
    ${owner}`;
  sheetA11y(sh);
}
// Minutos estimados: con la ruta real si ya está calculada; si no, aproximación por distancia
function etaMin(d, m) {
  const r = cachedRoute(USER_POS, d.pos, m);
  if (r && r.done) return Math.max(1, Math.round(r.done.duration / 60));
  return Math.max(1, Math.round(distM(USER_POS, d.pos) * 1.3 / SPEED[m]));
}
function closePlace() {
  $('#placeSheet').hidden = true; S.place = null;
  $('#app').classList.remove('place-open');
  $$('.pin').forEach(el => el.classList.remove('selected'));
  if (!S.navigating) { S.dest = null; hideDestMarker(); }
  applyMarkerVisibility();
}
function toggleList(list, id) { const i = list.indexOf(id); if (i >= 0) list.splice(i, 1); else list.push(id); return i < 0; }

// ---------- Navegación (GPS simulado) ----------
// Ruta por calles reales (OSRM, en lugares.js). Vale para cualquier destino: un establecimiento con precios,
// un lugar del mapa, una dirección, una ubicación compartida por WhatsApp o un punto marcado en el mapa.
async function startNav() {
  const d = S.dest; if (!d || S.navigating) return;
  const mode = S.travelMode;
  const nav = S.navigating = { dest: d, id: d.kind === 'place' ? d.id : null, mode, heading: bearing(USER_POS, d.pos), arrived: false, endT: null, route: null };
  if (mode === 'pie') startCompass(); // a pie el cono sigue la brújula del móvil
  $('#placeSheet').hidden = true; $('#app').classList.remove('place-open');
  $$('.pin').forEach(el => el.classList.remove('selected'));
  $('#mapUI').hidden = true; $('#navUI').hidden = false;
  $('#navSimulate').hidden = false; $('#navSimulate').disabled = true; $('#navFinish').hidden = true; $('#navExit').hidden = false;
  $('#app').classList.add('navigating');
  if (nav.id) pinOf(nav.id)?.classList.add('dest');
  applyMarkerVisibility();
  setNavBanner('…', 'Calculando la ruta', 'straight');
  $('#navEta').textContent = '…';
  $('#navTo').textContent = `${planNavText(d)}Hacia ${d.name}`;
  updatePlanChip();
  updateMe();
  if (focusLost()) $('#navExit').focus({ preventScroll: true }); // la ficha se ha ocultado: el foco pasa a la navegación

  const r = await getRoute(USER_POS, d.pos, mode);
  if (S.navigating !== nav) return; // se canceló mientras se calculaba
  nav.route = r;
  nav.heading = bearing(r.coords[0], positionAt(r, 25));
  updateMe();
  map?.getSource('ruta')?.setData({ type: 'Feature', geometry: { type: 'LineString', coordinates: r.coords } });
  updateNavBanner(nav, 0);
  $('#navEta').textContent = `${Math.max(1, Math.round(r.duration / 60))} min ${mode === 'coche' ? 'en coche' : 'a pie'} · ${fmtDist(r.total)}`;
  $('#navSimulate').disabled = false;
  if (!r.real) toast('Sin conexión con el servidor de rutas: ruta aproximada');
  map?.easeTo({ center: USER_POS, zoom: 17, pitch: 62, bearing: nav.heading, duration: motionMs(900) });
}
function updateNavBanner(nav, dist) {
  const s = nav.route.steps.find(x => x.at > dist + 2 && x.type !== 'depart');
  if (s) setNavBanner(fmtDist(Math.max(0, s.at - dist)), stepText(s), stepIcon(s));
}
// Indicaciones por voz: se lee cada instrucción NUEVA (no a cada fotograma) y la llegada.
// Elección de diseño: "Sonidos de la app" desactivado (o la burbuja del altavoz) silencia TODA la app,
// también la voz; "Indicaciones por voz" solo controla la voz.
function speak(text) {
  if (!('speechSynthesis' in window) || !S.settings.voice || !S.settings.sound) return;
  try {
    speechSynthesis.cancel(); // una indicación nueva sustituye a la anterior
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'es-ES';
    speechSynthesis.speak(u);
  } catch { /* sin síntesis de voz */ }
}
function stopSpeech() { if ('speechSynthesis' in window) try { speechSynthesis.cancel(); } catch { /* nada */ } }
function setNavBanner(dist, instr, icon) {
  $('#navDist').textContent = dist;
  $('#navInstr').textContent = instr;
  // Al llegar, simulateArrival pinta ('Has llegado', nombre del destino); el resto son pasos de la ruta
  const nav = S.navigating, said = dist === 'Has llegado' ? `Has llegado a ${instr}` : instr;
  if (nav && instr === 'Calculando la ruta') nav.lastSaid = null;
  else if (nav && nav.lastSaid !== said) { nav.lastSaid = said; speak(said); }
  const show = { navIconTurn: icon === 'left' || icon === 'right', navIconStraight: icon === 'straight', navIconRound: icon === 'round', navIconArrive: icon === 'arrive' };
  for (const [id, on] of Object.entries(show)) $(`#${id}`).style.display = on ? '' : 'none';
  $('#navIconTurn').style.transform = icon === 'left' ? 'scaleX(-1)' : '';
}
function stopNav() {
  if (!S.navigating) return;
  const was = S.navigating;
  clearTimeout(S.navigating.endT);
  stopSpeech();
  if (S.navigating.id) pinOf(S.navigating.id)?.classList.remove('dest');
  S.navigating = null;
  S.dest = null; hideDestMarker();
  applyMarkerVisibility();
  updateMe();
  map?.getSource('ruta')?.setData({ type: 'FeatureCollection', features: [] });
  userMarker.setLngLat(USER_POS);
  $('#navUI').hidden = true; $('#mapUI').hidden = false;
  $('#app').classList.remove('navigating');
  if (focusLost()) focusBack($('#searchInput'), '#mapTitle'); // al salir de la ruta el foco no se queda en un botón oculto
  map?.easeTo({ ...HOME, duration: motionMs(900) });
  updatePlanChip();
  planAfterNav(was); // plan de tarde: abre la siguiente parada
}
function simulateArrival(fast) {
  const nav = S.navigating; if (!nav || !nav.route) return;
  $('#navSimulate').disabled = true;
  // t0 se toma del primer fotograma: la marca de tiempo de requestAnimationFrame puede ser
  // anterior a performance.now() y daba un progreso negativo.
  const r = nav.route;
  const dur = fast ? 50 : Math.min(14000, Math.max(4000, r.total * 5)); // la animación dura según la longitud de la ruta
  let t0 = null;
  const step = now => {
    if (S.navigating !== nav) { $('#navSimulate').disabled = false; return; } // se pulsó "Salir"
    if (t0 === null) t0 = now;
    const k = Math.max(0, Math.min(1, (now - t0) / dur));
    const dist = k * r.total, pos = positionAt(r, dist), ahead = positionAt(r, Math.min(r.total, dist + 20));
    if (distM(pos, ahead) > 1) nav.heading = lerpAngle(nav.heading, bearing(pos, ahead), fast ? 1 : 0.18); // giro suave
    userMarker.setLngLat(pos);
    updateMe();
    map?.jumpTo({ center: pos, bearing: nav.heading });
    updateNavBanner(nav, dist);
    if (k < 1) requestAnimationFrame(step);
    else {
      $('#navSimulate').disabled = false; setNavBanner('Has llegado', nav.dest.name, 'arrive');
      sfx('arrive');
      // Solo se pregunta el precio en establecimientos con precios. Las direcciones (casa de un amigo,
      // ubicaciones compartidas…) no se guardan en el historial ni generan preguntas.
      // ¿Se va a programar la pregunta del precio? (mismas condiciones que scheduleAsk)
      const willAsk = !!nav.id && !isBizAccount() && !askScheduled.has(nav.id) && !S.pending.some(x => x.id === nav.id);
      if (nav.id) { addVisit(nav.id); scheduleAsk(nav.id); }
      else toast(`Has llegado a ${nav.dest.name}`);
      // La ruta sigue en pantalla hasta pulsar "Terminar ruta" o hasta que pase ARRIVE_END_MS.
      // Una sola acción de cierre: "Salir" se oculta al llegar.
      nav.arrived = true;
      $('#navSimulate').hidden = true; $('#navFinish').hidden = false; $('#navExit').hidden = true;
      const endMs = window.__arriveEndMs ?? ARRIVE_END_MS;
      $('#navEta').textContent = 'Has llegado';
      $('#navTo').textContent = willAsk ? `Te preguntaremos el precio dentro de ${fmtDelay(askDelay())}`
        : nav.id && !isBizAccount() ? 'Ya tienes pendiente el precio de este sitio'
        : `Ruta terminada · se cierra sola en ${fmtDelay(endMs)}`;
      $('#navFinish').focus();
      nav.endT = setTimeout(() => { if (S.navigating === nav) stopNav(); }, endMs);
    }
  };
  requestAnimationFrame(step);
}

// La pregunta "¿has ido?" no sale al llegar: se lanza pasado ASK_DELAY_MS (1 min por ahora),
// para dar tiempo a entrar en el sitio. Si en ese momento ya hay otra pregunta abierta, queda pendiente.
const askScheduled = new Set(); // sitios con una pregunta ya programada (no se duplica)
const ARRIVAL_STEPS = ['visited', 'choose', 'other', 'confirm', 'input', 'range', 'rate'];
const isBizAccount = () => S.account?.type === 'negocio';
function scheduleAsk(id) {
  // Las cuentas de negocio no aportan precios como clientes (evita que un negocio puntúe a la competencia)
  if (isBizAccount()) return toast('Has llegado');
  // Una sola pregunta por sitio: si ya hay una programada, abierta o pendiente, no se programa otra
  if (askScheduled.has(id) || S.pending.some(x => x.id === id) || (S.flow?.id === id && ARRIVAL_STEPS.includes(S.flow.step)))
    return toast('Has llegado · ya tienes pendiente el precio de este sitio');
  const ms = askDelay(), who = S.account?.email ?? null;
  askScheduled.add(id);
  toast(`Has llegado · te preguntaremos por el precio en ${fmtDelay(ms)}`);
  setTimeout(() => {
    askScheduled.delete(id);
    if (isBizAccount()) return; // la sesión pudo cambiar a una cuenta de negocio mientras tanto
    if (who && S.account?.email !== who) return; // la visita era de una sesión que ya se cerró
    // nunca interrumpir otra pregunta, una ruta en curso (seguridad al conducir) ni la pantalla de cuenta
    if (S.flow || S.navigating || !$('#screen-cuenta').hidden) {
      if (!S.pending.some(x => x.id === id)) S.pending.unshift({ id, when: 'hoy' });
      return updatePriceFab();
    }
    sfx('ask'); startFlow(id);
  }, ms);
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
  if (isBizAccount()) return toast('Las cuentas de negocio no aportan precios de clientes');
  if (!S.pending.length) return toast('Aquí guardaremos los precios que dejes para «Más tarde»');
  if (S.pending.length === 1) return startFlow(S.pending[0].id, true);
  S.flow = { step: 'pick' }; renderFlow();
}
function removePending(id) { S.pending = S.pending.filter(x => x.id !== id); updatePriceFab(); }

// ---------- Preguntas al llegar ----------
function startFlow(id, fromPending = false) {
  if (isBizAccount()) return toast('Las cuentas de negocio no aportan precios de clientes');
  // hist: pasos anteriores (para "‹ Atrás"); commit: aporte preparado que solo se registra al terminar
  S.flow = { id, fromPending, step: 'visited', item: null, value: null, rating: [0, 0, 0], earned: { c: 0, xp: 0 }, changed: false, hist: [], commit: null };
  renderFlow();
}
// "Más tarde": la visita queda en pendientes y no se registra nada
function flowLater() {
  const id = S.flow.id;
  if (!S.pending.some(x => x.id === id)) S.pending.unshift({ id, when: 'hoy' });
  updatePriceFab(); toast('Guardado en pendientes: lo tienes en el botón €');
  endFlow();
}
// Escape: en las preguntas al llegar equivale a "Más tarde"; en el resto de ventanas, cerrar
function flowEscape() {
  if (!S.flow) return;
  if (ARRIVAL_STEPS.includes(S.flow.step) && S.flow.id && !EXTRA_STEPS[S.flow.step]) flowLater(); else endFlow();
}
function endFlow() {
  S.flow = null; $('#flowOverlay').hidden = true; syncModal(); updateHUD();
  // El foco vuelve al control que abrió la ventana. Se espera a que termine lo que venga detrás
  // (p. ej. volver a pintar la ficha o abrir el perfil): solo se mueve si el foco se ha quedado perdido.
  const op = flowOpener; flowOpener = null;
  setTimeout(() => {
    if (S.flow || !focusLost()) return;
    const sh = $('#placeSheet');
    focusBack(op?.el, op?.sig && op.inSheet && findCtl(sh, [op.sig]), op?.inSheet && '#sheetTitle', '#priceFab', '#searchInput', '#navExit', '#mapTitle');
  }, 0);
}

const MAX_TYPED = 9999; // lo escrito a mano manda sobre el máximo de la regla
const fmtNum = v => String(v).replace('.', ',');
const roundHalf = v => Math.round(v * 2) / 2; // precios en pasos de 0,5 €
function rulerHTML(value, max) {
  const STEP = 12, nums = [];
  for (let v = 0; v <= max; v += 5) nums.push(`<span class="ruler-num" style="left:${v * STEP}px">${v}</span>`);
  return `<div class="pv"><input id="priceInput" type="text" inputmode="decimal" autocomplete="off" value="${fmtNum(value)}" aria-label="Precio en euros"><span>€</span></div>
    <div class="pv-hint">Toca el número para escribirlo o desliza la regla</div>
    <div class="ruler-wrap"><div class="ruler" id="ruler" aria-hidden="true"><div class="ruler-track" id="rulerTrack">
      <div class="ruler-inner" style="position:absolute;top:0;bottom:0;width:${max * STEP + 2}px">${nums.join('')}<div class="ruler-ticks" style="left:0;width:${max * STEP + 2}px"></div></div>
    </div></div><div class="needle"></div></div>`;
}
function setupRuler(max) {
  const STEP = 12, f = S.flow, r = $('#ruler'), track = $('#rulerTrack'), inner = track.firstElementChild, input = $('#priceInput');
  const pad = r.clientWidth / 2;
  track.style.width = `${max * STEP + pad * 2}px`; inner.style.left = `${pad}px`;
  // expect: posición que ha puesto la app (no el usuario); su evento scroll no debe recortar lo escrito
  let expect = null;
  const fit = () => { input.style.width = `${Math.max(2, input.value.length) + 0.4}ch`; };
  const show = () => { input.value = fmtNum(f.value); fit(); };
  const moveTo = v => { r.scrollLeft = Math.min(v, max) * STEP; expect = r.scrollLeft; };
  const setVal = (v, moveRuler, typed = false) => {
    f.value = Math.max(0, Math.min(typed ? MAX_TYPED : max, roundHalf(v)));
    if (document.activeElement !== input) show();
    $$('.sugs button').forEach(b => { b.classList.toggle('on', +b.dataset.sug === f.value); b.setAttribute('aria-pressed', +b.dataset.sug === f.value); });
    if (moveRuler) moveTo(f.value);
  };
  moveTo(f.value);
  r.addEventListener('scroll', () => {
    if (expect != null && Math.abs(r.scrollLeft - expect) < 1) return;
    expect = null; setVal(r.scrollLeft / STEP, false);
  });
  input.addEventListener('input', () => {
    fit(); const v = parseFloat(input.value.replace(',', '.'));
    if (isFinite(v)) setVal(v, true, true);
  });
  input.addEventListener('blur', show);
  r.addEventListener('wheel', e => { e.preventDefault(); r.scrollLeft += (e.deltaY || e.deltaX); }, { passive: false });
  // Arrastre con ratón (en el móvil el dedo desplaza la regla de forma nativa)
  let drag = null;
  r.addEventListener('pointerdown', e => { if (e.pointerType !== 'mouse') return; drag = { x: e.clientX, s: r.scrollLeft }; r.classList.add('drag'); r.setPointerCapture(e.pointerId); });
  r.addEventListener('pointermove', e => { if (drag) r.scrollLeft = drag.s - (e.clientX - drag.x); });
  const end = () => { if (!drag) return; drag = null; r.classList.remove('drag'); r.scrollTo({ left: f.value * STEP, behavior: reducedMotion() ? 'auto' : 'smooth' }); };
  r.addEventListener('pointerup', end); r.addEventListener('pointercancel', end);
  $$('.sugs button').forEach(b => b.addEventListener('click', () => setVal(+b.dataset.sug, true, true)));
  setVal(f.value, false, true);
}

// Tramos de gasto en hostelería: [etiqueta, desde, hasta]; el último no tiene tope (hasta = null)
const BUCKETS = [['Menos de 10 €', 0, 10], ['10–20 €', 10, 20], ['20–30 €', 20, 30], ['30–50 €', 30, 50], ['Más de 50 €', 50, null]];
// Concepto en curso del flujo: uno de la lista o el nuevo de "Otra cosa" (aún no añadido a p.items)
const flowItem = (p, f) => f.item === 'new' ? f.newItem : p.items[f.item];

// Tarjeta de flujo (ventana modal). Tras pintar cada paso:
//  - el h2 del paso da nombre al diálogo (aria-labelledby="flowTitle");
//  - al abrir o al cambiar de paso, el foco va a ese h2 ("Otra cosa": al campo de texto);
//  - si se vuelve a pintar el mismo paso (estrellas, personas del plan…), el foco sigue en el mismo control.
let flowOpener = null, flowKey = '', flowFocusNext = null;
const focusFlowNext = (...sels) => { flowFocusNext = sels; }; // control preferido tras el próximo repintado
function renderFlow() {
  const ov = $('#flowOverlay'), card = $('#flowCard'), opening = ov.hidden, a = document.activeElement;
  if (opening) {
    const inSheet = $('#placeSheet').contains(a);
    flowOpener = a && a !== document.body && !ov.contains(a) ? { el: a, sig: ctlSig(a), inSheet } : null;
  }
  const sig = !opening && card.contains(a) ? ctlSig(a) : null, want = flowFocusNext; flowFocusNext = null;
  drawFlow();
  if (!S.flow) return;
  const h = card.querySelector('h2');
  if (h) { h.id = 'flowTitle'; h.tabIndex = -1; }
  card.querySelectorAll('svg').forEach(x => x.setAttribute('aria-hidden', 'true'));
  syncModal();
  const key = `${S.flow.step}|${h?.textContent || ''}`, same = !opening && key === flowKey;
  flowKey = key;
  let t = same ? findCtl(card, [...(want || []), sig]) : null;
  if (!t && !same && S.flow.step === 'other') t = $('#otherName');
  (t || h || card).focus({ preventScroll: !t });
}
function drawFlow() {
  const f = S.flow, card = $('#flowCard');
  $('#flowOverlay').hidden = false;
  if (f.step === 'pick') {
    card.innerHTML = `<h2>Precios pendientes</h2><p>Elige la visita que quieres responder.</p>
      <div class="options">${S.pending.map(x => { const p = placeById(x.id); return `<button class="option" data-pend="${p.id}"><span>${p.name}</span><small>${x.when}</small></button>`; }).join('')}</div>
      <div class="flow-actions"><button class="btn ghost" data-flow="close">Cerrar</button></div>`;
    return;
  }
  if (EXTRA_STEPS[f.step]) return EXTRA_STEPS[f.step](card); // ofertas, plan de tarde, avisos (extras.js)
  const p = placeById(f.id);
  if (f.step === 'owner') return renderOwner(p, card);
  const hasRating = !!TPL[p.tpl].ratings;
  const steps = p.tpl === 'hosteleria' ? ['visited', 'range', 'rate', 'reward']
    : ['visited', 'choose', 'confirm', ...(hasRating ? ['rate'] : []), 'reward'];
  const n = steps.indexOf(f.step === 'input' || f.step === 'other' ? 'confirm' : f.step) + 1;
  // Salida en cada paso: "‹ Atrás" (paso anterior) y "Más tarde" (queda en pendientes, no se registra nada)
  const nav = f.step === 'visited' ? '' : `<div class="flow-nav"><button type="button" data-flow="back">‹ Atrás</button><button type="button" data-flow="later">Más tarde</button></div>`;
  const stepTag = f.step === 'reward' ? '' : `${nav}<div class="flow-step">Paso ${n} de ${steps.length - 1}</div>`;
  const privacy = p.sensitive ? `<div class="privacy-pill">${ICON.shield} Aportación anónima</div>` : '';
  let html = '';

  if (f.step === 'visited') {
    const pend = S.pending.find(x => x.id === p.id);
    html = f.fromPending
      ? `${stepTag}<h2>¿Has ido a ${p.name}?</h2><p>Lo dejaste pendiente ${pend ? pend.when : ''}. Cuéntanos el precio y gana monedas.</p>`
      : `${stepTag}<h2>¿Has ido a ${p.name}?</h2><p>${priced(p) ? 'Tu respuesta ayuda a mantener los precios al día.' : 'Aún nadie ha aportado su precio: puedes ser el primero (+' + REWARD.first.c + ' monedas).'}</p>`;
    html += `<div class="flow-actions"><button class="btn primary" data-flow="yes">Sí, he ido</button>
      <button class="btn secondary" data-flow="later">Más tarde</button><button class="btn ghost" data-flow="no">No he ido</button></div>`;
  } else if (f.step === 'choose') {
    // Conceptos con precio (para confirmarlo) y conceptos aún sin precio (para aportarlo el primero)
    const priceTxt = it => it.p != null ? `${it.desde && it.p === it.declared ? 'desde ' : ''}${fmtEur(it.p)}${UNIT[it.u]}` : it.ask ? 'A consultar' : 'sin precio aún';
    html = `${stepTag}<h2>¿Qué servicio pagaste en ${p.name}?</h2><p>Elige el servicio o la actividad.</p>
      <div class="options">${p.items.map((it, i) => `<button class="option" data-item="${i}"><span>${it.n}</span><small>${priceTxt(it)}</small></button>`).join('')}
      <button class="option" data-flow="other"><span>Otra cosa</span><small>escríbela</small></button></div>`;
  } else if (f.step === 'other') {
    html = `${stepTag}<h2>¿Qué pagaste en ${p.name}?</h2><p>Escribe el servicio o la actividad que pagaste.</p>
      <input class="text-in" id="otherName" maxlength="40" placeholder="Por ejemplo: alisado, clase de surf…" autocomplete="off" aria-label="Qué pagaste (servicio o actividad)" value="${(f.otherName || '').replace(/"/g, '&quot;')}">
      <div class="unit-pick" role="group" aria-label="Cómo se cobra">${['servicio', 'persona', 'h', 'sesión'].map(u => `<button data-unit="${u}" class="${f.unit === u ? 'on' : ''}" aria-pressed="${f.unit === u}">${{ servicio: 'Precio único', persona: 'Por persona', h: 'Por hora', 'sesión': 'Por sesión' }[u]}</button>`).join('')}</div>
      <div class="flow-actions"><button class="btn primary" data-flow="otherOk">Continuar</button></div>`;
  } else if (f.step === 'confirm') {
    const it = flowItem(p, f);
    html = `${stepTag}${privacy}<h2>¿Te costó esto en ${p.name}?</h2><p>${it.n}${it.u === 'h' && p.tpl === 'ocio' ? ' (precio total del espacio)' : ''}</p>
      <div class="big-price">${fmtEur(it.p)} <span>${unitText[it.u]}</span></div>
      ${it.review ? `<div class="note">${ICON.pending}<span>Varios usuarios avisan de que este precio podría haber cambiado: tu respuesta lo aclara.</span></div>` : REWARD_STALE[freshState(p, it.at)] ? `<div class="note">${ICON.clock}<span>Precio confirmado ${ageText(it.at)}: +${REWARD_STALE[freshState(p, it.at)].c} monedas extra por confirmarlo.</span></div>` : ''}
      <div class="flow-actions two"><button class="btn secondary" data-flow="diff">No, pagué otro precio</button><button class="btn primary" data-flow="same">${ICON.check} Sí, pagué eso</button></div>`;
  } else if (f.step === 'input') {
    const it = flowItem(p, f);
    const base = it.p ?? GUESS[it.u] ?? 15;
    const max = Math.max(100, Math.ceil(base * 3 / 10) * 10);
    const others = [...new Set(it.reports.concat(it.declared != null ? [it.declared] : []))].sort((a, b) => a - b).slice(0, 4);
    html = `${stepTag}${privacy}<h2>${it.p == null ? `¿Cuánto pagaste en ${p.name}? Serás el primero` : `¿Cuánto te costó en ${p.name}?`}</h2><p>${it.n} · ${unitText[it.u] || 'precio del servicio'}</p>
      ${rulerHTML(f.value, max)}
      ${others.length ? `<div class="sugs-label">Precios aportados hasta ahora</div><div class="sugs" role="group" aria-label="Precios aportados hasta ahora">${others.map(v => `<button data-sug="${v}" aria-pressed="false">${fmtEur(v)}</button>`).join('')}</div>` : ''}
      <div class="flow-actions"><button class="btn primary" data-flow="sendPrice">Enviar precio</button></div>`;
    card.innerHTML = html;
    return setupRuler(max);
  } else if (f.step === 'range') {
    html = `${stepTag}<h2>¿Cuánto gastaste por persona en ${p.name}?</h2><p>Aproximado, con bebida incluida.${p.range ? '' : ' Aún nadie lo ha aportado: serás el primero.'}</p>
      ${p.rangeReview ? `<div class="note">${ICON.pending}<span>Varios usuarios avisan de que este rango podría haber cambiado: tu respuesta lo aclara.</span></div>` : ''}
      <div class="options">${BUCKETS.map(([l], i) => `<button class="option" data-bucket="${i}"><span>${l}</span></button>`).join('')}</div>`;
  } else if (f.step === 'rate') {
    const crit = TPL[p.tpl].ratings;
    html = `${stepTag}<h2>¿Qué tal fue en ${p.name}?</h2><p>Opcional · +${REWARD.rating.c} monedas</p>
      ${crit.map((c, ci) => `<div class="stars-row"><span id="crit${ci}">${c}</span><div class="stars" role="radiogroup" aria-labelledby="crit${ci}">${[1, 2, 3, 4, 5].map(s =>
        // Grupo de opciones por criterio: una sola estrella tabulable (la elegida o la primera); flechas para cambiar
        `<button class="star ${f.rating[ci] >= s ? 'on' : ''}" role="radio" aria-checked="${f.rating[ci] === s}" tabindex="${(f.rating[ci] || 1) === s ? 0 : -1}" data-crit="${ci}" data-star="${s}" aria-label="${s} de 5">${ICON.star}</button>`).join('')}</div></div>`).join('')}
      <div class="flow-actions two"><button class="btn ghost" data-flow="skip">Saltar</button><button class="btn primary" data-flow="sendRate" ${f.rating.some(r => r === 0) ? 'disabled' : ''}>Enviar</button></div>`;
  } else if (f.step === 'reward') {
    const before = S.xp - f.earned.xp, lvBefore = level(before), lv = level(S.xp);
    const pctXp = (S.xp % XP_PER_LEVEL) / XP_PER_LEVEL * 100;
    const msg = f.first ? '¡Eres el primero en aportar este precio! Ya aparece en su ficha y en el mapa (actívalo en «Ver precios»); se irá ajustando con lo que paguen otros clientes.'
      : f.changed && f.declaredStays ? `Hemos registrado tu precio. Por ahora se muestra el que declaró el negocio; cambiará cuando ${MIN_REPORTS} clientes aporten lo que pagaron.`
      : f.changed ? 'Hemos registrado tu precio. El precio mostrado se calcula con lo que pagan los clientes y se ajusta con cada aporte.'
      : 'Has confirmado el precio. ¡Gracias por mantenerlo al día!';
    html = `<div class="reward-burst"><div class="reward-coin">+${f.earned.c}</div><h2>${f.first ? '¡Pionero!' : '¡Gracias!'}</h2></div>
      <p style="text-align:center">${msg}</p>
      ${f.bonus ? `<p class="bonus">Incluye +${f.bonus} monedas extra por actualizar un precio antiguo</p>` : ''}
      ${lv > lvBefore ? `<div class="levelup">¡Subes a nivel ${lv}!</div>` : ''}
      <div class="xp"><div class="xp-head"><span>Nivel ${lv}</span><span>+${f.earned.xp} XP · ${S.xp % XP_PER_LEVEL}/${XP_PER_LEVEL}</span></div>
      <div class="xp-bar"><i style="width:${pctXp}%"></i></div></div>
      <div class="flow-actions"><button class="btn primary" data-flow="done">Volver al mapa</button><button class="btn secondary" data-flow="shop">Ir a la tienda</button></div>`;
  }
  card.innerHTML = html;
  if (f.step === 'reward') sfx('coin');
}

// ---------- Modo negocio: el dueño declara su precio inicial ----------
// Se muestra como "Declarado por el negocio" hasta que MIN_REPORTS clientes aportan lo que pagaron;
// a partir de ahí manda lo que pagan los clientes (mediana / rango central).
function openOwner(id) { S.flow = { id, step: 'owner' }; renderFlow(); }
function renderOwner(p, card) {
  // Cada campo lleva nombre accesible: el del servicio (aria-labelledby) o una etiqueta propia (aria-label)
  const field = (name, val, ph = '€', lab = '') => `<input class="num-in" type="number" inputmode="decimal" min="0" step="0.5" name="${name}" id="own-${name}" value="${val ?? ''}" placeholder="${ph}" ${lab}>`;
  const body = p.tpl === 'hosteleria'
    ? `<div class="owner-field"><span id="own-range-l">Gasto medio por persona</span><div class="range-in">${field('lo', p.declaredRange?.[0], 'desde', 'aria-label="Gasto medio por persona: desde (€)"')}<b aria-hidden="true">–</b>${field('hi', p.declaredRange?.[1], 'hasta', 'aria-label="Gasto medio por persona: hasta (€)"')}<b aria-hidden="true">€</b></div></div>`
    : p.items.map((it, i) => `<div class="owner-field"><span id="own-l${i}">${it.n}<small>${unitText[it.u] || 'precio único'}${it.desde ? ' · se muestra «desde»' : ''}${it.nota ? ` · ${it.nota}` : ''}${it.review ? ' · <b class="rev-t">en revisión</b>' : ''}</small></span>${field('i' + i, it.declared, it.ask ? 'A consultar' : '€', `aria-labelledby="own-l${i}"`)}</div>`).join('')
      + (p.items.some(it => it.ask) ? `<p class="hint">Si lo dejas vacío, se mostrará «A consultar».</p>` : '')
      + `<div class="owner-field"><input class="text-in" name="newName" id="own-newName" placeholder="Añadir otro servicio…" maxlength="40" aria-label="Añadir otro servicio: nombre">${field('newPrice', '', '€', 'aria-label="Añadir otro servicio: precio (€)"')}</div>`;
  card.innerHTML = `<div class="flow-step">Modo negocio</div><h2>Precios de ${p.name}</h2>
    <p>Declara tus precios. Se mostrarán como «declarados por el negocio»; cuando ${MIN_REPORTS} clientes aporten lo que pagaron, se mostrará lo que pagan ellos.</p>
    <form id="ownerForm">${body}</form>
    ${p.web ? `<div class="note">${ICON.info}<span>Hemos rellenado los precios publicados en tu web (${p.web.fecha}). Revísalos: al publicar, pasarán a mostrarse como declarados por el negocio.</span></div>` : ''}
    <div class="flow-actions two"><button class="btn ghost" data-flow="close">Cancelar</button><button class="btn primary" data-flow="ownerSave">Publicar precios</button></div>`;
}
function saveOwner(p) {
  // Primero se valida todo; solo si no hay errores se modifica el lugar (si no, queda intacto)
  const fd = new FormData($('#ownerForm')), form = $('#ownerForm');
  const raw = k => String(fd.get(k) ?? '').trim().replace(',', '.');
  // Campo vacío → null; número inválido o negativo → NaN (error)
  const num = k => { const s = raw(k); if (s === '' && !form.elements[k]?.validity?.badInput) return null; const v = Number(s); return s !== '' && isFinite(v) && v >= 0 ? v : NaN; };
  // El error se muestra bajo el campo (con aria-invalid y aria-describedby) y el foco va a ese campo
  const fail = (k, msg) => (fieldErrors($('#flowCard'), [[form.elements[k], msg]]), false);
  const badMsg = label => `${label}: escribe un precio válido (0 € o más)`;
  if (p.tpl === 'hosteleria') {
    const lo = num('lo'), hi = num('hi');
    if (Number.isNaN(lo)) return fail('lo', badMsg('Gasto mínimo'));
    if (Number.isNaN(hi)) return fail('hi', badMsg('Gasto máximo'));
    if (lo == null || hi == null) return fail(lo == null ? 'lo' : 'hi', 'Indica el gasto mínimo y máximo por persona');
    if (hi < lo) return fail('hi', 'El gasto máximo debe ser igual o mayor que el mínimo');
    p.declaredRange = [lo, hi]; p.rangeAt = Date.now();
  } else {
    const vals = p.items.map((_, i) => num('i' + i));
    const badAt = vals.findIndex(v => Number.isNaN(v));
    if (badAt >= 0) return fail('i' + badAt, badMsg(`«${p.items[badAt].n}»`));
    const nn = String(fd.get('newName') || '').trim(), np = num('newPrice');
    if (Number.isNaN(np)) return fail('newPrice', badMsg(nn ? `«${nn}»` : 'Nuevo servicio'));
    if (nn && np == null) return fail('newPrice', `Pon el precio de «${nn}» o borra su nombre`);
    if (!nn && np != null) return fail('newName', 'Escribe el nombre del nuevo servicio o borra su precio');
    if (nn && p.items.some(it => normTxt(it.n) === normTxt(nn))) return fail('newName', `«${nn}» ya está en la lista: pon su precio arriba`);
    if (!vals.some(v => v != null) && np == null) return fail(p.items.length ? 'i0' : 'newPrice', 'Pon al menos un precio');
    p.items.forEach((it, i) => {
      const v = vals[i];
      if (v !== it.declared) { it.desde = false; it.nota = ''; } // el negocio fija un precio concreto
      it.declared = v; it.ask = v == null && it.ask; // vacío en un servicio "a consultar": sigue "A consultar"
      if (v != null) it.at = Date.now();
    });
    if (nn) { const it = mkItem(nn, 'servicio', { g: p.items.some(i => i.g) ? 'Otros' : null }); it.declared = np; it.at = Date.now(); p.items.push(it); }
  }
  p.owner = true;
  p.web = null; // a partir de ahora es el negocio quien declara el precio desde su cuenta
  saveBusiness(p);
  return true;
}

function grant(r) { S.coins += r.c; S.xp += r.xp; S.flow.earned.c += r.c; S.flow.earned.xp += r.xp; }

// Doble clic/toque: durante ~350 ms tras cambiar de paso se ignoran los clics repetidos en el mismo sitio
// (p. ej. un doble clic en "Sí" no se salta la pantalla de recompensa)
let flowLock = { until: 0, x: 0, y: 0 };
function onFlowClick(e) {
  if (!S.flow || !e.target.closest('button')) return;
  if (Date.now() < flowLock.until && (e.detail > 1 || Math.hypot(e.clientX - flowLock.x, e.clientY - flowLock.y) < 40)) return;
  const before = S.flow, step = before.step;
  handleFlowClick(e);
  if (S.flow && (S.flow !== before || S.flow.step !== step)) flowLock = { until: Date.now() + 350, x: e.clientX, y: e.clientY };
}
function handleFlowClick(e) {
  const f = S.flow; if (!f) return;
  const b = e.target.closest('button'); if (!b) return;
  if (f.step === 'pick') {
    if (b.dataset.pend) startFlow(b.dataset.pend, true);
    else if (b.dataset.flow === 'close') endFlow();
    return;
  }
  if (EXTRA_STEPS[f.step]) return onExtraFlowClick(e, b);
  const p = placeById(f.id), act = b.dataset.flow;
  if (f.step === 'owner') {
    e.preventDefault();
    if (act === 'close') return endFlow();
    if (act === 'ownerSave' && saveOwner(p)) {
      endFlow(); toast('Precios publicados como declarados por el negocio');
      refreshPlace(p);
      if (S.place !== p.id) openPlace(p.id); // el dueño ve cómo queda su ficha
    }
    return;
  }
  const hasRating = !!TPL[p.tpl].ratings;
  // El aporte se prepara al enviar el precio y se registra al salir de la valoración (o directamente si no hay
  // valoración): así "‹ Atrás" y "Más tarde" en la valoración no dejan nada registrado.
  const runCommit = () => { const c = f.commit; f.commit = null; if (c) c(); };
  const afterPrice = (what, apply) => {
    f.commit = () => {
      apply(); S.contributions++; removePending(p.id);
      S.history.unshift({ id: p.id, what, when: 'hoy' });
      refreshPlace(p);
    };
    if (hasRating) f.step = 'rate'; else { runCommit(); f.step = 'reward'; }
  };
  const stale = at => { const st = freshState(p, at); if (REWARD_STALE[st]) { grant(REWARD_STALE[st]); f.bonus = REWARD_STALE[st].c; } };
  const prev = f.step;

  f.hist ||= [];
  if (act === 'back') { f.step = f.hist.pop() || 'visited'; f.commit = null; f.bonus = 0; return renderFlow(); }
  if (act === 'yes') f.step = p.tpl === 'hosteleria' ? 'range' : 'choose';
  else if (act === 'later') return flowLater();
  else if (act === 'no') { removePending(p.id); toast('Vale, no te preguntaremos por esta visita'); return endFlow(); }
  else if (b.dataset.item) {
    f.item = +b.dataset.item; const it = p.items[f.item];
    // Precio "desde" (web del negocio): no se confirma tal cual, se pregunta cuánto costó
    if (it.p != null && !(it.desde && it.p === it.declared)) f.step = 'confirm';
    else { f.value = it.p != null ? roundHalf(it.p) : GUESS[it.u] ?? 15; f.step = 'input'; }
  }
  else if (act === 'other') { f.unit = f.unit || 'servicio'; f.step = 'other'; }
  else if (b.dataset.unit) { f.unit = b.dataset.unit; $$('.unit-pick button').forEach(x => { x.classList.toggle('on', x === b); x.setAttribute('aria-pressed', x === b); }); return; }
  else if (act === 'otherOk') {
    const name = $('#otherName').value.trim(); if (fieldErrors($('#flowCard'), name ? [] : [[$('#otherName'), 'Escribe qué pagaste']])) return;
    f.otherName = name;
    const i = p.items.findIndex(it => normTxt(it.n) === normTxt(name)); // ya existe: se usa ese concepto
    if (i >= 0) {
      f.item = i; const it = p.items[i];
      if (it.p != null && !(it.desde && it.p === it.declared)) f.step = 'confirm';
      else { f.value = it.p != null ? roundHalf(it.p) : GUESS[it.u] ?? 15; f.step = 'input'; }
    } else { // concepto nuevo: solo se añade a la lista al registrar el precio
      f.item = 'new'; f.newItem = mkItem(name, f.unit); f.value = GUESS[f.unit] ?? 15; f.step = 'input';
    }
  }
  else if (act === 'same') {
    const it = flowItem(p, f);
    f.first = false; f.changed = false;
    afterPrice(`Confirmó ${fmtEur(it.p)}`, () => {
      const at = it.at; it.reports.push(it.p); clearFlags(p, it);
      grant(REWARD.confirm); stale(at); S.confirmed++;
    });
  }
  else if (act === 'diff') { f.value = roundHalf(flowItem(p, f).p); f.step = 'input'; }
  else if (b.dataset.sug) return; // lo gestiona la regla
  else if (act === 'sendPrice') {
    const it = flowItem(p, f), before = it.p, value = f.value, isNew = f.item === 'new';
    if (!(value > 0)) return toast('El precio debe ser mayor que 0 €');
    f.first = before == null; f.changed = !f.first && Math.abs(value - before) >= 0.25; // la regla va de 0,5 en 0,5 €
    f.declaredStays = it.declared != null && it.reports.length + 1 < MIN_REPORTS;
    afterPrice(`${it.n} · ${fmtEur(value)}`, () => {
      if (isNew) { p.items.push(it); f.item = p.items.length - 1; }
      const at = it.at; it.reports.push(value); clearFlags(p, it);
      if (!f.first) stale(at);
      grant(f.first ? REWARD.first : f.changed ? REWARD.newPrice : REWARD.confirm);
    });
  }
  else if (b.dataset.bucket) {
    const [label, lo, hi] = BUCKETS[+b.dataset.bucket], before = p.range, open = p.rangeOpen;
    f.first = !before;
    f.changed = !!before && ((hi ?? Infinity) <= before[0] || (lo >= before[1] && !open));
    f.declaredStays = !!p.declaredRange && p.rangeReports.length + 1 < MIN_REPORTS;
    afterPrice(`Gasto ${label}`, () => {
      const at = p.rangeAt; p.rangeReports.push([lo, hi]); clearFlags(p);
      if (!f.first) stale(at);
      grant(f.first ? REWARD.first : REWARD.confirm);
    });
  }
  else if (b.dataset.star) { f.rating[+b.dataset.crit] = +b.dataset.star; }
  else if (act === 'sendRate') {
    runCommit();
    // media acumulada de cada criterio
    p.ratings = p.ratings ? p.ratings.map((r, i) => (r * p.nRatings + f.rating[i]) / (p.nRatings + 1)) : [...f.rating];
    p.nRatings++;
    grant(REWARD.rating); f.step = 'reward';
  }
  else if (act === 'skip') { runCommit(); f.step = 'reward'; }
  else if (act === 'done') return endFlow();
  else if (act === 'shop') { endFlow(); return openProfile('tienda'); }
  if (f.step !== prev && f.step !== 'reward') f.hist.push(prev);
  renderFlow();
}
// Tras un cambio de precio: actualizar la etiqueta accesible del marcador y la ficha si está abierta
function refreshPlace(p) {
  pinOf(p.id)?.setAttribute('aria-label', `${p.name} (${p.cat}): ${pinLabel(p)}`);
  pinOf(p.id)?.classList.toggle('empty', !priced(p));
  updateOfferTag(p); // después de fijar la etiqueta: añade " · oferta activa" si hace falta
  if (S.place === p.id && !$('#placeSheet').hidden) renderPlace();
}

// ---------- Perfil (tienda y ajustes dentro, agrupados como en Discord) ----------
const PAGES = {
  main: 'Perfil', tienda: 'Tienda', pendientes: 'Precios pendientes', aportaciones: 'Mis aportaciones', insignias: 'Insignias',
  avisos: 'Sitios vigilados', reputacion: 'Reputación', bizAvisos: 'Avisos de clientes', favoritos: 'Favoritos', apariencia: 'Apariencia', navegacion: 'Navegación y voz', notificaciones: 'Notificaciones',
  privacidad: 'Privacidad y ubicación', accesibilidad: 'Accesibilidad', idioma: 'Idioma y región', ayuda: 'Cómo funcionan los precios', acerca: 'Acerca de',
};
// Al abrir el perfil o cambiar de página, el foco va al título (h1 con tabindex="-1")
function openProfile(page = 'main') {
  closePlace(); closeCatMenu(); S.page = page; $('#screen-perfil').hidden = false; syncModal(); renderProfile(); $('#screen-perfil').scrollTop = 0;
  $('#screen-perfil h1')?.focus({ preventScroll: true });
}
// Al cerrar el perfil el foco vuelve al avatar (si después se abre otra cosa, esa cosa se queda con el foco)
function closeProfile() {
  S.page = null; $('#screen-perfil').hidden = true; syncModal();
  if (focusLost()) focusBack($('#avatarBtn'), '#searchInput', '#mapTitle');
}

// Fila del perfil. Sin go/attr (no hace nada) se pinta como <div> no interactivo con el mismo aspecto.
const it = (icon, label, { go, val, count, sub, cls = '', attr = '' } = {}) => {
  const tag = go || attr ? 'button' : 'div';
  return `<${tag} class="it ${cls}" ${go ? `data-page="${go}"` : ''} ${attr}>${icon}<span class="lbl">${label}${sub ? `<small>${sub}</small>` : ''}</span>${count ? `<span class="count">${count}</span>` : ''}${val ? `<span class="val">${val}</span>` : ''}${go ? ICON.chev : ''}</${tag}>`;
};
// Interruptor: el propio botón es el switch (el dibujo interior es decorativo)
const sw = (key, label, sub) => `<button class="it" role="switch" aria-checked="${!!S.settings[key]}" data-set="${key}"><span class="lbl">${label}${sub ? `<small>${sub}</small>` : ''}</span><i class="switch" aria-hidden="true" aria-checked="${!!S.settings[key]}"></i></button>`;
const grp = (title, body) => `<div class="grp">${title ? `<h3>${title}</h3>` : ''}<div class="grp-box">${body}</div></div>`;
const seg = (key, opts) => `<div class="segmented">${opts.map(([v, l]) => { const on = (key === 'theme' ? S.themePref : S.settings[key]) === v; return `<button class="${on ? 'on' : ''}" aria-pressed="${on}" data-seg="${key}" data-v="${v}">${l}</button>`; }).join('')}</div>`;
// Hace cuánto (avisos de clientes)
const agoTxt = t => { const m = Math.round((Date.now() - t) / 60000); return m < 1 ? 'hace un momento' : m < 60 ? `hace ${m} min` : m < 1440 ? `hace ${Math.round(m / 60)} h` : ageText(t); };
const REASON_TXT = { changed: 'Ahora cuesta otra cantidad', gone: 'Ya no ofrecen este servicio', closed: 'El negocio ha cerrado' };

function ringSVG(size, r, sw) {
  const c = 2 * Math.PI * r, pct = (S.xp % XP_PER_LEVEL) / XP_PER_LEVEL;
  return `<svg viewBox="0 0 ${size} ${size}"><circle class="ring-bg" cx="${size / 2}" cy="${size / 2}" r="${r}" style="stroke-width:${sw}"/><circle class="ring-xp" cx="${size / 2}" cy="${size / 2}" r="${r}" style="stroke-width:${sw}" stroke-dasharray="${c * pct} ${c}"/></svg>`;
}

function renderProfile() {
  const pg = S.page, el = $('#screen-perfil');
  const lv = level(S.xp), xpIn = S.xp % XP_PER_LEVEL;
  let body = '';
  const acc = S.account, via = acc ? (acc.method === 'google' ? 'Google' : 'correo') : '';
  if (pg === 'main' && acc?.type === 'negocio') {
    // Cuenta de negocio: sin monedas ni tienda; gestiona los precios de su negocio
    const p = placeById(acc.business);
    const st = !p ? 'Aún no has vinculado tu negocio'
      : !priced(p) ? 'Sin precios publicados'
      : nReports(p) >= MIN_REPORTS ? `Manda lo que pagan los clientes (${nReports(p)} aportaciones)`
      : `Precios declarados · ${nReports(p)} de ${MIN_REPORTS} aportaciones de clientes para confirmarlos`;
    body = `<div class="phead"><div class="biz-face">${p ? tplIcon(p) : ICON.building}</div>
        <div style="flex:1;min-width:0"><h2>${p ? p.name : acc.name}</h2><p>Cuenta de negocio · ${acc.name} (${via})</p>
        ${p ? `<div class="biz-state">${st}</div>` : ''}</div></div>`
      + grp('Tu negocio', p
        ? it(ICON.list, 'Editar precios', { sub: p.tpl === 'hosteleria' ? 'Gasto medio por persona' : `${p.items.length} servicios`, attr: 'data-act="ownerEdit"' })
          + it(ICON.store, 'Ofertas', { sub: activeOffers(p).length ? `${activeOffers(p).length} activa${activeOffers(p).length > 1 ? 's' : ''}` : 'Publica una oferta con fecha de fin', attr: 'data-act="offersEdit"' })
          + it(ICON.pending, 'Avisos de clientes', { sub: inReview(p) ? 'Hay precios en revisión: actualízalos o espera a que los clientes los confirmen' : 'Ningún precio en revisión', count: p.flagLog.length || '', go: 'bizAvisos' })
          + it(ICON.pin, 'Ver la ficha en el mapa', { attr: `data-place="${p.id}"` })
          + it(ICON.users, 'Cómo lo ven los clientes', { attr: `data-place="${p.id}"`, sub: 'Abre tu ficha tal como la ven ellos' })
        : it(ICON.building, 'Vincular o dar de alta mi negocio', { attr: 'data-act="linkBiz"' }))
      + grp('Ajustes de la app',
        it(ICON.palette, 'Apariencia', { go: 'apariencia' })
        + it(ICON.bell, 'Notificaciones', { go: 'notificaciones' })
        + it(ICON.shield, 'Privacidad y ubicación', { go: 'privacidad' })
        + it(ICON.globe, 'Idioma y región', { go: 'idioma', val: 'Español' }))
      + grp('Soporte', it(ICON.help, 'Cómo funcionan los precios', { go: 'ayuda' }) + it(ICON.info, 'Acerca de', { go: 'acerca' }))
      + grp('', it(ICON.out, 'Cerrar sesión', { cls: 'danger', attr: 'data-act="logout"' }));
  } else if (pg === 'main') {
    body = `<div class="phead"><div class="big-ring">${ringSVG(84, 39, 6)}<span class="face">${lv}</span></div>
        <div style="flex:1;min-width:0"><h2>${acc ? acc.name : `Nivel ${lv}`}</h2><p>${acc ? `Nivel ${lv} · ` : 'Colaborador local · '}${xpIn}/${XP_PER_LEVEL} XP</p>
        <div class="xp-bar"><i style="width:${xpIn / XP_PER_LEVEL * 100}%"></i></div>
        <div class="coins"><i class="coin"></i>${S.coins}<span>· ${S.contributions} aportaciones</span></div></div></div>`
      + (acc ? '' : grp('Cuenta', it(ICON.users, 'Crear cuenta o iniciar sesión', { sub: 'Guarda tus monedas y aportaciones · también para negocios', attr: 'data-act="login"' })))
      + grp('Tienda', it(ICON.store, 'Tienda de cosméticos', { go: 'tienda', sub: 'Marcador de posición y estilo de la ruta' }))
      + grp('Tu actividad',
        it(ICON.pending, 'Precios pendientes', { go: 'pendientes', count: S.pending.length || '' })
        + it(ICON.list, 'Mis aportaciones', { go: 'aportaciones', val: S.contributions })
        + it(ICON.heart, 'Favoritos', { go: 'favoritos', val: S.favs.length + S.spots.length })
        + it(ICON.bell, 'Sitios vigilados', { go: 'avisos', val: S.alerts.length, sub: 'Te avisamos si cambia su precio' })
        + it(ICON.users, 'Reputación', { go: 'reputacion', val: reputation().name, sub: `Tus avisos de error cuentan ×${fmtW(reputation().w)}` })
        + it(ICON.award, 'Insignias', { go: 'insignias' }))
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
      + (acc ? grp(`Cuenta de cliente · ${via}`, it(ICON.out, 'Cerrar sesión', { cls: 'danger', attr: 'data-act="logout"' })) : '');
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
    body = `<p class="lead">Precios que dejaste para «Más tarde». También los tienes en el botón amarillo € del mapa.</p>`
      + (S.pending.length ? grp('', S.pending.map(x => { const p = placeById(x.id); return it(tplIcon(p), p.name, { sub: `Visitado ${x.when}`, val: 'Responder', attr: `data-pend="${p.id}"` }); }).join(''))
        : `<div class="text-card">No tienes nada pendiente. ¡Todo al día!</div>`);
  } else if (pg === 'aportaciones') {
    body = grp(`${S.contributions} aportaciones · ${S.confirmed} precios confirmados`, S.history.map(h => { const p = placeById(h.id); return it(tplIcon(p), p.name, { sub: `${h.what} · ${h.when}` }); }).join(''));
  } else if (pg === 'favoritos') {
    body = (S.favs.length ? grp('Con precios', S.favs.map(id => { const p = placeById(id); return `<button class="it" data-place="${p.id}">${tplIcon(p)}<span class="lbl">${p.name}<small>${p.cat}</small></span><span class="price">${pinLabel(p)}</span></button>`; }).join('')) : '')
      + (S.spots.length ? grp('Lugares y direcciones', S.spots.map(s => `<button class="it" data-key="${s.key}">${glyphSVG(s.kind === 'poi' ? POI_CATS[s.cat]?.glyph : 'home')}<span class="lbl">${s.name}<small>${s.sub || ''}</small></span></button>`).join('')) : '')
      || `<div class="text-card">Marca sitios con el corazón de su ficha. También puedes guardar direcciones, como la casa de un amigo.</div>`;
  } else if (pg === 'avisos') {
    body = `<p class="lead">Te avisamos cuando los clientes confirmen un precio distinto en estos sitios. Se activan desde la ficha con "Avísame si cambia".</p>`
      + (S.alerts.length ? grp('Sitios vigilados', S.alerts.map(id => { const p = placeById(id); return `<button class="it" role="switch" aria-checked="true" data-unalert="${p.id}">${tplIcon(p)}<span class="lbl">${p.name}<small>Ahora: ${pinLabel(p)}</small></span><i class="switch" aria-hidden="true" aria-checked="true"></i></button>`; }).join(''))
        : `<div class="text-card">No vigilas ningún sitio todavía.</div>`)
      + (S.alerts.length ? `<div class="grp"><button class="btn secondary" data-act="demoAlert">Ver un ejemplo de aviso</button></div>` : '');
  } else if (pg === 'insignias') {
    const badges = [['Primer precio', true], ['Explorador', true], ['10 aportaciones', true], ['Racha 7 días', false], ['Crítico', false], ['50 aportaciones', false]];
    body = `<ul class="badges" role="list">${badges.map(([n, ok]) => `<li class="badge ${ok ? '' : 'locked'}"><i aria-hidden="true">${ok ? ICON.award : ICON.lock}</i>${n}${ok ? '' : '<span class="sr-only"> (bloqueada)</span>'}</li>`).join('')}</ul>`;
  } else if (pg === 'reputacion') {
    const rep = reputation();
    body = `<p class="lead">Tu reputación decide cuánto pesan tus avisos de error en un precio.</p>`
      + grp('', it(ICON.users, `Tu nivel: ${rep.name}`, { sub: `Tus avisos de error cuentan ×${fmtW(rep.w)}${rep.next ? ` · te faltan ${rep.next} aportaciones para subir` : ''}` })
        + it(ICON.award, 'Nueva', { val: '×1', sub: 'Al empezar' })
        + it(ICON.award, 'Fiable', { val: '×1,5', sub: 'Desde 5 aportaciones' })
        + it(ICON.award, 'Experta', { val: '×2', sub: 'Desde 20 aportaciones' }))
      + `<div class="text-card">Cuando los avisos de un precio suman ${REVIEW_AT} puntos, queda <b>En revisión</b> y pedimos a los siguientes clientes que lo confirmen.</div>`
      + grp('', it(ICON.help, 'Cómo funcionan los precios', { go: 'ayuda' }));
  } else if (pg === 'bizAvisos') {
    const p = placeById(S.account?.business);
    if (!p) body = `<div class="text-card">Aún no has vinculado tu negocio.</div>`;
    else {
      const hos = p.tpl === 'hosteleria', what = i => (hos ? 'Gasto por persona' : p.items[i]?.n || 'Precio');
      const rev = hos ? (p.rangeReview ? ['Gasto por persona'] : []) : p.items.filter(x => x.review).map(x => x.n);
      body = `<p class="lead">Avisos de error que han enviado tus clientes. Cuando suman ${REVIEW_AT} puntos, el precio queda en revisión hasta que lo actualices o lo confirmen otros clientes.</p>`
        + grp('En revisión', rev.length ? rev.map(n => it(ICON.pending, n, { sub: 'Pendiente de actualizar o confirmar' })).join('') : it(ICON.check, 'Ningún precio en revisión'))
        + grp('Avisos recibidos', p.flagLog.length
          ? p.flagLog.map(f => it(ICON.pending, what(f.item), { sub: `${REASON_TXT[f.reason] || f.reason} · ${agoTxt(f.when)}` })).join('')
          : it(ICON.check, 'Aún no has recibido avisos'))
        + `<div class="grp"><button class="btn primary" data-act="ownerEdit">Actualizar precios</button></div>`;
    }
  } else if (pg === 'apariencia') {
    body = grp('', `<div class="seg-label">Modo</div>${seg('theme', [['auto', 'Automático'], ['light', 'Claro'], ['dark', 'Oscuro']])}`
      + sw('buildings', 'Edificios en 3D', 'Muestra el mapa en 2.5D con edificios extruidos'));
  } else if (pg === 'navegacion') {
    body = grp('', sw('sound', 'Sonidos de la app', 'También con la burbuja del altavoz en el mapa')
      + sw('voice', 'Indicaciones por voz', 'Lee los giros en voz alta durante la ruta'))
      + `<div class="text-card" style="margin-top:10px">Si vas en coche o a pie se elige en la ficha de cada sitio, antes de pulsar "Ir". Por defecto, en coche.</div>`;
  } else if (pg === 'notificaciones') {
    body = grp('', sw('notifPending', 'Recordar precios pendientes', 'Punto rojo en el botón € del mapa')
      + sw('notifAlerts', 'Cambios de precio', 'Avisos de los sitios que vigilas'));
  } else if (pg === 'privacidad') {
    body = `<div class="text-card"><b>Ubicación:</b> solo se usa mientras navegas con la app. Nunca en segundo plano.</div>
      <div class="text-card"><b>Clínicas, fisioterapia y dentistas:</b> tus aportaciones son siempre anónimas.</div>`
      + grp('', sw('anonAll', 'Aportar siempre de forma anónima', 'En todas las categorías, no solo en las sensibles')
        + it(ICON.trash, 'Borrar historial de visitas', { cls: 'danger', attr: 'data-act="clearVisits"' }));
  } else if (pg === 'accesibilidad') {
    body = grp('', sw('reduceMotion', 'Reducir animaciones', 'Quita los movimientos de la interfaz'));
  } else if (pg === 'idioma') {
    body = grp('', it(ICON.globe, 'Idioma', { val: 'Español' }) + it(ICON.route, 'Unidades', { val: 'km · €' }));
  } else if (pg === 'ayuda') {
    const [hg, hs] = FRESH.hosteleria, [sg, ss] = FRESH.servicio;
    body = `<div class="text-card"><b>¿De dónde salen los precios?</b> Los negocios empiezan sin precio, salvo los que publican sus tarifas en su web oficial (se indica la página y la fecha). El dueño puede declarar el suyo desde su cuenta de negocio, y los clientes aportan lo que pagaron al llegar con el GPS.</div>
      <div class="text-card"><b>¿Qué precio se muestra?</b> Si el negocio declaró su precio, se muestra el suyo hasta que ${MIN_REPORTS} o más clientes aportan lo que pagaron; desde entonces manda el precio típico que pagan los clientes. Si el negocio no declaró nada, se muestra lo que pagan los clientes desde la primera aportación.</div>
      <div class="text-card"><b>Servicios locales</b> (peluquería, dentista, taller…): un precio por servicio, el precio típico que pagan los clientes.</div>
      <div class="text-card"><b>Hostelería:</b> siempre un rango por persona, con lo que gasta la mayoría de la gente.</div>
      <div class="text-card"><b>Ocio:</b> un precio por instalación o actividad. Alquiler de pistas, precio total; actividades como escape room, por persona.</div>
      <div class="text-card"><b>¿Está al día?</b> Cada precio lleva un punto: al día, podría haber cambiado o antiguo. En hostelería pasa a "podría haber cambiado" a los ${hg} días y a antiguo a los ${hs}; en servicios y ocio, a los ${sg / 30} y ${ss / 30} meses. Si confirmas un precio viejo ganas monedas extra.</div>
      <div class="text-card"><b>Avisos de error y "En revisión":</b> si un precio está mal, avísalo desde la ficha. Cada aviso suma según tu reputación: Nueva ×1, Fiable ×1,5 (desde 5 aportaciones) y Experta ×2 (desde 20). Con ${REVIEW_AT} puntos el precio queda "En revisión" y pedimos a los siguientes clientes que lo confirmen.</div>
      <div class="text-card"><b>Ofertas:</b> las publica el propio negocio con una fecha de fin. Si ${OFFER_GONE} clientes avisan de que ya no está, se retira.</div>
      <div class="text-card"><b>Anonimato:</b> en clínicas, fisioterapia y dentistas tus aportaciones son siempre anónimas. En el resto puedes elegirlo en Privacidad.</div>
      <div class="text-card"><b>Origen del dato:</b> cada precio indica si lo declaró el negocio, si sale de su web o cuántos clientes lo han aportado.</div>`;
  } else if (pg === 'acerca') {
    body = `<div class="text-card"><b>PrecioLocal</b> (nombre provisional) · prototipo del Trabajo de Fin de Grado.<br>Mapa: © OpenStreetMap, OpenFreeMap, MapLibre. Los lugares son reales (OpenStreetMap, licencia ODbL); ningún precio es inventado: empiezan vacíos o con las tarifas que el propio negocio publica en su web.</div>`;
  }
  const back = pg === 'main' ? 'close' : 'main';
  // Iconos SVG del perfil: decorativos (el texto de cada fila ya da el nombre)
  body = body.replace(/<svg(?![^>]*aria-hidden)/g, '<svg aria-hidden="true"');
  el.innerHTML = `<div class="topnav"><button class="back" data-back="${back}" aria-label="${pg === 'main' ? 'Volver al mapa' : 'Volver al perfil'}">${ICON.back}</button><h1 tabindex="-1">${PAGES[pg]}</h1></div>${body}`;
}
function previewOf(item) {
  if (item.kind === 'flecha') {
    const col = item.id === 'flecha-dorada' ? '#FFC93C' : item.id === 'flecha-neon' ? '#22D3EE' : 'var(--accent)';
    return `<span class="swatch-dot" style="background:${col}"></span>`;
  }
  return `<span class="route-prev ${item.id === 'ruta-discont' ? 'dashed' : ''} ${item.id === 'ruta-dorada' ? 'gold' : ''}"></span>`;
}

// Selector del control equivalente tras volver a pintar el perfil (para no dejar el foco en el body)
function profileCtlSel(d) {
  if (d.set) return `[data-set="${d.set}"]`;
  if (d.seg) return `[data-seg="${d.seg}"][data-v="${d.v}"]`;
  if (d.page) return `[data-page="${d.page}"]`;
  if (d.equip || d.buy) return `[data-equip="${d.equip || d.buy}"], [data-buy="${d.buy || d.equip}"]`;
  return null;
}
function rerenderProfile(d) {
  renderProfile();
  const el = $('#screen-perfil'), sel = profileCtlSel(d), t = sel && el.querySelector(sel);
  (t && !t.disabled ? t : el.querySelector('h1'))?.focus({ preventScroll: true });
}
function onProfileClick(e) {
  const b = e.target.closest('button'); if (!b) return;
  const d = b.dataset;
  if (d.back) return d.back === 'close' ? closeProfile() : openProfile('main');
  if (d.page) return openProfile(d.page);
  if (d.place) { closeProfile(); return openPlace(d.place); }
  if (d.key) { const s = S.spots.find(x => x.key === d.key); closeProfile(); return s && openDest(s); }
  if (d.pend) { closeProfile(); return startFlow(d.pend, true); }
  if (d.unalert) { toggleList(S.alerts, d.unalert); toast('Aviso desactivado'); return rerenderProfile(d); }
  if (d.set) {
    S.settings[d.set] = !S.settings[d.set];
    if (d.set === 'buildings' && map?.getLayer('edificios-3d')) map.setLayoutProperty('edificios-3d', 'visibility', S.settings.buildings ? 'visible' : 'none');
    if (d.set === 'reduceMotion') document.documentElement.classList.toggle('reduce-motion', S.settings.reduceMotion);
    if (d.set === 'notifPending') updatePriceFab();
    if (d.set === 'sound') updateSound();
    if ((d.set === 'voice' || d.set === 'sound') && !(S.settings.voice && S.settings.sound)) stopSpeech();
    return rerenderProfile(d);
  }
  if (d.seg) {
    if (d.seg === 'theme') { S.themePref = d.v; store.set('pl-theme', d.v); applyTheme(); }
    else S.settings[d.seg] = d.v;
    return rerenderProfile(d);
  }
  if (d.buy) {
    const item = SHOP.find(i => i.id === d.buy);
    if (S.coins < item.cost) return;
    S.coins -= item.cost; S.owned.push(item.id); S.equipped[item.kind] = item.id; toast(`Comprado y equipado: ${item.name}`);
    applyCosmetics(); updateHUD(); return rerenderProfile(d);
  }
  if (d.equip) {
    const item = SHOP.find(i => i.id === d.equip); S.equipped[item.kind] = item.id; toast(`Equipado: ${item.name}`);
    applyCosmetics(); return rerenderProfile(d);
  }
  if (d.act === 'logout') { closeProfile(); return logout(); }
  if (d.act === 'login') { closeProfile(); return openAccount(); }
  if (d.act === 'linkBiz') { closeProfile(); return openAccount({ type: 'negocio', step: 'business' }); }
  if (d.act === 'ownerEdit') { closeProfile(); return openOwner(S.account.business); }
  if (d.act === 'offersEdit') { closeProfile(); return openOffers(S.account.business); }
  if (d.act === 'clearVisits') { S.visits = []; return toast('Historial de visitas borrado'); }
  if (d.act === 'demoAlert') {
    // Solo con datos reales de la sesión: no se inventan cambios de precio de negocios reales
    const p = S.alerts.map(placeById).find(x => x && priced(x));
    if (!p) return toast('Aún no hay precios en los sitios que vigilas: el aviso llegará cuando alguien los aporte');
    closeProfile();
    const precio = p.tpl === 'hosteleria' ? `${fmtRange(p)} por persona` : pinLabel(p);
    return notify(`Ya hay precio en ${p.name}`, `${precio} · ${isDeclared(p) && nReports(p) < MIN_REPORTS ? 'declarado por el negocio' : 'aportado por clientes'}`, p.id);
  }
}

// ---------- HUD ----------
function updateHUD() {
  const r = 20, c = 2 * Math.PI * r, pct = (S.xp % XP_PER_LEVEL) / XP_PER_LEVEL;
  $('#ringXp').setAttribute('stroke-dasharray', `${c * pct} ${c}`);
  const biz = S.account?.type === 'negocio';
  $('#avatarBtn').classList.toggle('biz', biz);
  if (biz) { $('#avatarLv').innerHTML = ICON.store; $('#avatarBtn').setAttribute('aria-label', 'Abrir el perfil de tu negocio'); }
  else { $('#avatarLv').textContent = level(S.xp); $('#avatarBtn').setAttribute('aria-label', `Abrir perfil · nivel ${level(S.xp)}, ${S.xp % XP_PER_LEVEL} de ${XP_PER_LEVEL} XP`); }
  updatePriceFab();
  if (S.page) renderProfile();
}

// ---------- Eventos ----------
function bind() {
  $('#onbStart').addEventListener('click', () => {
    $('#screen-onboarding').hidden = true; syncModal(); store.set('pl-onb', '1'); startCompass(); askAccountIfNeeded();
    if (!topModal()) $('#mapTitle').focus({ preventScroll: true }); // sin cuenta que pedir: al mapa (el siguiente Tab va a "Ver precios")
  });
  $('#locateFab').addEventListener('click', startCompass); // también al centrar en tu posición (toque del usuario)
  // e.detail === 0: pulsado con teclado (Intro/Espacio) → el foco entra en la lista
  $('#catBtn').addEventListener('click', e => { e.stopPropagation(); $('#catMenu').hidden ? openCatMenu(e.detail === 0) : closeCatMenu(); });
  $('#catBtn').addEventListener('keydown', e => {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') { e.preventDefault(); openCatMenu(true); if (e.key === 'ArrowUp') $$('#catMenu [role="option"]').at(-1).focus(); }
  });
  $('#catMenu').addEventListener('keydown', onCatMenuKey);
  $('#catMenu').addEventListener('click', e => { const b = e.target.closest('[data-cat]'); if (b) setFilter(b.dataset.cat); });
  $('#themeFab').addEventListener('click', () => { S.themePref = currentTheme() === 'dark' ? 'light' : 'dark'; store.set('pl-theme', S.themePref); applyTheme(); });
  $('#locateFab').addEventListener('click', () => map?.easeTo({ center: USER_POS, zoom: 16.5, duration: motionMs(600) }));
  $('#priceFab').addEventListener('click', openPending);
  $('#avatarBtn').addEventListener('click', () => openProfile('main'));
  $('#placeSheet').addEventListener('click', e => {
    const a = e.target.closest('[data-act]')?.dataset.act;
    if (a === 'close') closePlaceKb();
    else if (a === 'go') startNav();
    else if (a === 'fav') {
      const d = S.dest; let added;
      if (d.kind === 'place') added = toggleList(S.favs, d.id);
      else { const i = S.spots.findIndex(s => s.key === d.key); added = i < 0; if (added) S.spots.push(d); else S.spots.splice(i, 1); }
      toast(added ? 'Añadido a favoritos' : 'Quitado de favoritos'); renderPlace();
    }
    else if (a === 'alert') { toast(toggleList(S.alerts, S.place) ? (priced(placeById(S.place)) ? 'Te avisaremos si cambia el precio' : 'Te avisaremos cuando tenga precio') : 'Aviso desactivado'); renderPlace(); }
    else if (a === 'owner') openOwner(S.place);
    else if (a === 'claim') openAccount({ type: 'negocio', claim: S.place });
    else if (a) onExtraAct(a, e.target.closest('[data-act]')); // plan, avisos y ofertas (extras.js)
    const m = e.target.closest('[data-mode]')?.dataset.mode;
    if (m) { S.travelMode = m; renderPlace(); }
  });
  $$('.sound-btn').forEach(b => b.addEventListener('click', () => {
    S.settings.sound = !S.settings.sound; updateSound();
    if (!S.settings.sound) stopSpeech(); // silenciar la app también calla la voz
    toast(S.settings.sound ? 'Sonido activado' : 'App silenciada');
    if (S.page) renderProfile();
  }));
  $('#navExit').addEventListener('click', stopNav);
  $('#navFinish').addEventListener('click', stopNav);
  $('#navSimulate').addEventListener('click', () => simulateArrival(false));
  $('#flowCard').addEventListener('click', onFlowClick);
  // Estrellas (grupo de opciones): flechas izquierda/derecha (y arriba/abajo), Inicio y Fin cambian la nota
  $('#flowCard').addEventListener('keydown', e => {
    const b = e.target.closest('.star'); if (!b || !S.flow || S.flow.step !== 'rate') return;
    const ci = +b.dataset.crit, cur = S.flow.rating[ci] || +b.dataset.star;
    const to = { ArrowRight: cur + 1, ArrowUp: cur + 1, ArrowLeft: cur - 1, ArrowDown: cur - 1, Home: 1, End: 5 }[e.key];
    if (to == null) return;
    e.preventDefault();
    S.flow.rating[ci] = (to + 4) % 5 + 1; // da la vuelta: 5 → 1 y 1 → 5
    focusFlowNext(`[data-crit="${ci}"][data-star="${S.flow.rating[ci]}"]`); renderFlow();
  });
  $('#screen-perfil').addEventListener('click', onProfileClick);
  $('#notif').addEventListener('click', () => { $('#notif').hidden = true; const id = $('#notif').dataset.place; if (id) openPlace(id); });

  const si = $('#searchInput');
  // El buscador NO se abre solo con recibir el foco (tabular por él no cambia la vista): se abre al tocarlo,
  // al escribir o con Intro / flecha abajo
  const isOpen = () => $('#dock').classList.contains('open');
  $('#dock .search').addEventListener('click', () => { if (!isOpen()) { openSearch(); si.focus(); } });
  si.addEventListener('input', () => (isOpen() ? renderSearch() : openSearch()));
  si.addEventListener('keydown', e => {
    if (e.key === 'Escape' && isOpen()) { e.stopPropagation(); closeSearch(); si.focus({ preventScroll: true }); }
    else if ((e.key === 'Enter' || e.key === 'ArrowDown') && !isOpen()) { e.preventDefault(); openSearch(); }
  });
  // Cancelar con teclado: el foco vuelve al campo (con el dedo no, para que no salga el teclado)
  $('#searchCancel').addEventListener('click', e => { closeSearch(); if (e.detail === 0) si.focus({ preventScroll: true }); });
  setupDockDrag();
  $('#searchPanel').addEventListener('click', e => {
    const b = e.target.closest('button'); if (!b) return;
    const ds = b.dataset;
    if (ds.place) { closeSearch(); openPlace(ds.place); }
    else if (ds.key) { const s = RESULTS.get(ds.key); closeSearch(); if (s) { openDest(s); if (s.kind === 'shared') fillAddress(s); } }
    else if (ds.near) { setPoiCat(ds.near); if (focusLost()) $('#searchPanel .near-head h2')?.focus({ preventScroll: true }); }
    else if ('clearCat' in ds) { const k = S.poiCat; setPoiCat(null); renderSearch(); if (focusLost()) focusBack(findCtl($('#searchPanel'), [`[data-near="${k}"]`]), '#searchInput'); }
    else if (ds.act === 'paste') pasteLocation();
  });
  document.addEventListener('click', e => { if (!e.target.closest('.cat-dd')) closeCatMenu(); });
  document.addEventListener('keydown', trapTab);
  document.addEventListener('keydown', e => {
    if (e.key !== 'Escape') return;
    closeCatMenu();
    if (S.page && topModal() === $('#screen-perfil')) return closeProfile();
    // Ficha abierta y ninguna ventana encima: Escape la cierra y el foco vuelve a donde estaba
    if (!topModal() && !$('#placeSheet').hidden && !S.navigating) { e.preventDefault(); closePlaceKb(); }
  });
  // Escape con una pregunta abierta: "Más tarde" en las preguntas al llegar; cerrar en el resto
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && S.flow && !$('#flowOverlay').hidden) { e.preventDefault(); flowEscape(); } });
  $('#flowCard').addEventListener('submit', e => e.preventDefault()); // Intro en el formulario del negocio no recarga la página
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', applyTheme);
}

// ---------- Inicio ----------
S.themePref = store.get('pl-theme') || 'auto';
if (store.get('pl-onb')) $('#screen-onboarding').hidden = true;
applyTheme();
bind();
syncModal();
if (!$('#screen-onboarding').hidden) $('#onbTitle').focus({ preventScroll: true }); // bienvenida: el siguiente Tab cae en "Empezar"
initMap();
setFilter('ninguno'); // pantalla limpia: los precios se muestran solo si se eligen en el desplegable
updateSound();
updateHUD();

// Ganchos para las capturas automáticas (Playwright)
window.proto = { PLACES, startFlow, openOwner, openPlace, openDest, openShared, dropPin, setPoiCat, startNav, simulateArrival, openProfile, openSearch, openPending, setTheme: v => { S.themePref = v; applyTheme(); }, S };
