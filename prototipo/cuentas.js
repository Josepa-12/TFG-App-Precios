/* PrecioLocal — cuentas de usuario (prototipo)
 *
 * Dos tipos de cuenta:
 *  - CLIENTE: descubre precios, aporta lo que paga al llegar con el GPS y gana monedas y niveles.
 *  - NEGOCIO: vincula su establecimiento (o lo da de alta a mano si no aparece en el mapa) y declara
 *    sus precios iniciales. Los clientes los confirman o los corrigen con lo que pagan.
 *
 * Inicio de sesión con Google SIMULADO. En la app real se usaría Google Identity Services o
 * Firebase Authentication (proveedor Google): la app recibe el nombre y el correo verificados.
 * La sesión del prototipo se guarda solo en este navegador (localStorage).
 */

const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
const unstore = k => { try { localStorage.removeItem(k); } catch { /* sin almacenamiento */ } };
const GOOGLE_G = '<svg class="g-logo" viewBox="0 0 48 48" aria-hidden="true"><path fill="#FFC107" d="M43.6 20.5H42V20H24v8h11.3C33.7 32.7 29.2 36 24 36c-6.6 0-12-5.4-12-12s5.4-12 12-12c3.1 0 5.8 1.2 7.9 3l5.7-5.7C34 6.1 29.3 4 24 4 13 4 4 13 4 24s9 20 20 20 20-9 20-20c0-1.3-.1-2.4-.4-3.5z"/><path fill="#FF3D00" d="m6.3 14.7 6.6 4.8C14.7 15.1 19 12 24 12c3.1 0 5.8 1.2 7.9 3l5.7-5.7C34 6.1 29.3 4 24 4 16.3 4 9.7 8.3 6.3 14.7z"/><path fill="#4CAF50" d="M24 44c5.2 0 9.9-2 13.4-5.2l-6.2-5.2C29.2 35.1 26.7 36 24 36c-5.2 0-9.6-3.3-11.3-8l-6.5 5C9.5 39.6 16.2 44 24 44z"/><path fill="#1976D2" d="M43.6 20.5H42V20H24v8h11.3c-.8 2.2-2.2 4.2-4.1 5.6l6.2 5.2C37 39.2 44 34 44 24c0-1.3-.1-2.4-.4-3.5z"/></svg>';

// ---------- Sesión y datos del negocio (persisten en este navegador) ----------
// Claves de localStorage:
//  pl-account           sesión abierta
//  pl-negocio:<correo>  negocio de cada cuenta de negocio y lo que ha declarado (al volver a entrar, lo recupera)
//  pl-negocios-alta     todos los negocios dados de alta a mano (siguen en el mapa aunque se vincule otro)
//  pl-estado:<correo>   progreso de cada cuenta (pl-estado:invitado sin cuenta): monedas, favoritos, plan, ajustes…
//  pl-precios           lo que han aportado los clientes en cada lugar (dato público, común a todas las cuentas)
const BIZ_PREFIX = 'pl-negocio:', ALTA_KEY = 'pl-negocios-alta', STATE_PREFIX = 'pl-estado:', PRICES_KEY = 'pl-precios';
const mailKey = email => String(email || '').trim().toLowerCase();
const bizKey = email => BIZ_PREFIX + (mailKey(email) || 'sin-cuenta');
const readJSON = k => { try { return JSON.parse(store.get(k) || 'null'); } catch { return null; } };
const storeKeys = () => { try { return Object.keys(localStorage); } catch { return []; } };

function applyBusiness(biz) {
  if (!biz?.id) return null;
  let p = placeById(biz.id);
  if (!p && biz.custom) { p = buildPlace({ id: biz.id, tpl: biz.tpl, cat: biz.cat, name: biz.name, addr: biz.addr, pos: biz.pos, custom: true }); addPlace(p); }
  if (!p) return null;
  if (biz.custom && biz.addr) p.addr = biz.addr;
  if (biz.items) p.items = biz.items.map(i => mkItem(i.n, i.u, i));
  p.declaredRange = biz.declaredRange || null;
  p.rangeAt = biz.rangeAt ?? null;
  p.offers = biz.offers || [];
  p.web = biz.web ?? null;
  p.owner = !!biz.owner;
  return p;
}
function rememberAlta(data) {
  const list = (readJSON(ALTA_KEY) || []).filter(b => b.id !== data.id);
  list.push(data);
  store.set(ALTA_KEY, JSON.stringify(list));
}
function loadAccount() {
  S.account = readJSON('pl-account');
  // Migración: antes había un único negocio guardado en 'pl-negocio'
  const old = readJSON('pl-negocio');
  if (old) {
    const email = S.account?.type === 'negocio' && S.account.business === old.id ? S.account.email : '';
    store.set(bizKey(email), JSON.stringify(old));
    if (old.custom) rememberAlta(old);
    unstore('pl-negocio');
  }
  // Primero los negocios dados de alta a mano; después lo que declaró cada cuenta de negocio
  for (const b of readJSON(ALTA_KEY) || []) applyBusiness(b);
  for (const k of storeKeys()) if (k.startsWith(BIZ_PREFIX)) applyBusiness(readJSON(k));
  if (S.account?.business && !placeById(S.account.business)) S.account.business = null;
}
const bizData = p => ({
  id: p.id, custom: !!p.custom, tpl: p.tpl, cat: p.cat, name: p.name, addr: p.addr, pos: p.pos,
  owner: p.owner, web: p.web, declaredRange: p.declaredRange, rangeAt: p.rangeAt, offers: p.offers,
  items: p.items.map(({ n, u, declared, g, desde, nota, ask, at }) => ({ n, u, declared, g, desde, nota, ask, at })),
});
// Guarda lo que el negocio ha declarado (y, si lo dio de alta a mano, el propio negocio) en la cuenta que lo gestiona
function saveBusiness(p) {
  const data = bizData(p);
  store.set(bizKey(S.account?.email), JSON.stringify(data));
  if (p.custom) rememberAlta(data);
}
function saveAccount() { if (S.account) store.set('pl-account', JSON.stringify(S.account)); else unstore('pl-account'); }

// ---------- Progreso del cliente (se guarda por cuenta) y aportes de precio (comunes) ----------
const PERSONAL = ['coins', 'xp', 'contributions', 'confirmed', 'owned', 'equipped', 'pending', 'favs', 'spots', 'alerts', 'visits', 'history', 'settings'];
const clone = v => JSON.parse(JSON.stringify(v ?? null)); // sin funciones ni referencias compartidas
const stateKey = () => STATE_PREFIX + (mailKey(S.account?.email) || 'invitado');
function personalSnapshot() {
  const o = {};
  for (const k of PERSONAL) o[k] = S[k];
  o.plan = { stops: S.plan?.stops || [], people: S.plan?.people ?? 2 };
  o.reported = S.reported || [];
  o.reportedOffers = S.reportedOffers || [];
  return clone(o);
}
const DEFAULT_STATE = personalSnapshot(); // estado de ejemplo con el que arranca el prototipo

// Datos que aportan los clientes en un lugar
const hasClientData = p => p.rangeReports.length || p.nRatings || p.flagLog?.length || p.rangeFlags || p.rangeReview || p.rangeOpen != null
  || (p.offers || []).some(o => o.gone) || p.items.some(i => i.reports.length || i.flags || i.review);
function placeSnapshot(p) {
  const gone = {};
  for (const o of p.offers || []) if (o.gone) gone[o.id] = o.gone;
  const d = {
    items: p.items.map(i => ({ n: i.n, u: i.u, reports: i.reports, at: i.at, flags: i.flags, review: i.review })),
    rangeReports: p.rangeReports, rangeAt: p.rangeAt, rangeFlags: p.rangeFlags, rangeReview: p.rangeReview,
    ratings: p.ratings, nRatings: p.nRatings, flagLog: p.flagLog || [], gone,
  };
  if (p.rangeOpen != null) d.rangeOpen = p.rangeOpen;
  return d;
}
const maxAt = (a, b) => Math.max(a ?? 0, b ?? 0) || null;
function applyPlaceData(p, d) {
  for (const si of d.items || []) {
    let it = p.items.find(i => i.n === si.n);
    if (!it) { it = mkItem(si.n, si.u || 'servicio'); p.items.push(it); } // concepto nuevo ("Otra cosa")
    it.reports = [...(si.reports || [])];
    it.at = maxAt(it.at, si.at); it.flags = si.flags || 0; it.review = !!si.review;
  }
  p.rangeReports = clone(d.rangeReports || []);
  p.rangeAt = maxAt(p.rangeAt, d.rangeAt);
  p.rangeFlags = d.rangeFlags || 0; p.rangeReview = !!d.rangeReview;
  p.ratings = d.ratings ? [...d.ratings] : null; p.nRatings = d.nRatings || 0;
  p.flagLog = clone(d.flagLog || []);
  for (const o of p.offers || []) if (d.gone?.[o.id]) o.gone = Math.max(o.gone || 0, d.gone[o.id]);
  if (d.rangeOpen != null) p.rangeOpen = d.rangeOpen;
}
function loadPrices() {
  const all = readJSON(PRICES_KEY) || {}, changed = [];
  for (const [id, d] of Object.entries(all)) { const p = placeById(id); if (p && d) { applyPlaceData(p, d); changed.push(p); } }
  return changed;
}

let lastSaved = '';
function saveClientState() {
  const personal = JSON.stringify(personalSnapshot()), prices = {};
  for (const p of PLACES) if (hasClientData(p)) prices[p.id] = placeSnapshot(p);
  const key = stateKey(), pricesTxt = JSON.stringify(prices), all = key + '|' + personal + '|' + pricesTxt;
  if (all === lastSaved) return;
  store.set(key, personal);
  store.set(PRICES_KEY, pricesTxt);
  lastSaved = all;
}
function applyPersonal(st) {
  const ok = id => !!placeById(id);
  for (const k of PERSONAL) if (st[k] !== undefined) S[k] = clone(st[k]);
  S.settings = { ...DEFAULT_STATE.settings, ...(st.settings || {}) };
  S.equipped = { ...DEFAULT_STATE.equipped, ...(st.equipped || {}) };
  for (const k of ['owned', 'spots']) if (!Array.isArray(S[k])) S[k] = clone(DEFAULT_STATE[k]);
  S.favs = (S.favs || []).filter(ok); S.alerts = (S.alerts || []).filter(ok);
  for (const k of ['pending', 'visits', 'history']) S[k] = (S[k] || []).filter(x => ok(x?.id));
  if (S.plan) {
    S.plan.stops = (st.plan?.stops || []).filter(s => s?.d && (s.d.kind !== 'place' || ok(s.d.id)));
    S.plan.people = st.plan?.people || 2;
    S.plan.active = null;
  }
  S.reported = [...(st.reported || [])];
  S.reportedOffers = [...(st.reportedOffers || [])];
}
// Carga el progreso de la cuenta abierta (o del invitado) y repinta lo que depende de él
function loadClientState(changed = []) {
  const st = readJSON(stateKey()) || (S.account ? null : DEFAULT_STATE);
  if (st) applyPersonal(st);
  document.documentElement.classList.toggle('reduce-motion', !!S.settings.reduceMotion);
  if (typeof map !== 'undefined' && map?.getLayer?.('edificios-3d')) map.setLayoutProperty('edificios-3d', 'visibility', S.settings.buildings ? 'visible' : 'none');
  updateHUD(); updatePriceFab(); updateSound(); applyCosmetics();
  if (typeof updatePlanChip === 'function') updatePlanChip();
  for (const p of changed) refreshPlace(p);
  if (S.place && !$('#placeSheet').hidden) renderPlace();
}
// Cambia de cuenta (o cierra sesión con acc = null) guardando antes el progreso de la que estaba abierta.
// Una cuenta nueva sin progreso guardado se queda con lo que llevabas como invitado.
function switchAccount(acc) {
  saveClientState();
  const fromGuest = !S.account, own = acc ? readJSON(STATE_PREFIX + mailKey(acc.email)) : null;
  S.account = acc; saveAccount();
  if (!acc || own) loadClientState();
  else if (!fromGuest) { applyPersonal(DEFAULT_STATE); loadClientState(); }
  saveClientState();
}

// ---------- Pantalla de cuenta ----------
let accOpener = null; // control desde el que se abrió la cuenta: recibe el foco al cerrarla
function openAccount(opts = {}) {
  const a = document.activeElement;
  accOpener = a && a !== document.body && !$('#screen-cuenta').contains(a) ? a : null;
  closePlace(); closeCatMenu(); if (S.page) closeProfile();
  const type = opts.type || 'cliente';
  S.acc = { step: opts.step || (S.account && opts.type === 'negocio' ? 'business' : 'type'), type, claim: opts.claim || null, method: 'google', q: '', found: [], pick: null };
  // Desde "¿Es tu negocio?" con una cuenta de cliente abierta: hay que entrar con una cuenta de negocio
  if (opts.claim && S.account?.type !== 'negocio') S.acc.step = 'type';
  $('#screen-cuenta').hidden = false; syncModal();
  renderAccount();
}
// Al cerrar, el foco vuelve al control de origen o al avatar (si después se abre otra cosa, esa se lo queda)
function closeAccount() {
  S.acc = null; $('#screen-cuenta').hidden = true; syncModal();
  if (focusLost()) focusBack(accOpener, '#avatarBtn', '#searchInput', '#mapTitle');
  accOpener = null;
}
// Salir sin cambiar nada: si venía de "¿Es tu negocio?", vuelve a la ficha de ese lugar
function cancelAccount() {
  const back = S.acc?.claim;
  closeAccount();
  if (back && placeById(back)) openPlace(back);
}
function skipAccount() { store.set('pl-guest', '1'); closeAccount(); toast('Puedes crear tu cuenta cuando quieras desde el perfil'); }
function askAccountIfNeeded() { if (!S.account && !store.get('pl-guest')) openAccount(); }

function finishLogin() {
  saveAccount(); saveClientState(); closeAccount(); updateHUD();
  if (S.place) renderPlace();
}
function logout() {
  const was = S.account?.name;
  switchAccount(null); unstore('pl-guest'); updateHUD();
  toast(was ? `Sesión cerrada (${was})` : 'Sesión cerrada');
  openAccount();
}

const BIZ_TYPES = () => {
  const by = { servicio: [], hosteleria: [], ocio: [] };
  for (const c of Object.keys(PLACE_POICAT).sort((a, b) => a.localeCompare(b, 'es'))) by[tplOfCat(c)].push(c);
  return by;
};

function renderAccount() {
  const a = S.acc, el = $('#screen-cuenta');
  // Barra superior solo con "Volver" (sin h1 vacío: el título de cada paso es el h1/h2 de debajo)
  const back = to => `<div class="topnav"><button class="back" data-a="${to}" aria-label="Volver">${ICON.back}</button></div>`;
  const claimP = a.claim ? placeById(a.claim) : null;
  let html = '';

  // Con una sesión de cliente abierta, entrar como negocio la cierra: se avisa antes
  const warnClient = () => S.account?.type === 'cliente' && a.type === 'negocio'
    ? `<p class="acc-warn" role="note">${ICON.info}<span>Se cerrará tu sesión de cliente (${esc(S.account.name)}). Tus monedas y aportaciones quedan guardadas en esa cuenta.</span></p>` : '';
  const err = id => `<p class="acc-err" id="${id}Err" aria-live="polite"></p>`;

  if (a.step === 'type') {
    const card = (t, title, sub, icon) => `<button class="acc-type ${a.type === t ? 'on' : ''}" data-type="${t}" aria-pressed="${a.type === t}">
      <span class="acc-ic">${icon}</span><span><b>${title}</b><small>${sub}</small></span><i class="radio"></i></button>`;
    // Con sesión abierta siempre hay forma de salir (Volver y Cancelar), sin cerrar la sesión
    html = (S.account ? back('close') : '') + `<div class="acc-head"><div class="logo"><svg viewBox="0 0 64 64" aria-hidden="true"><path d="M32 4C20 4 11 13 11 25c0 15 21 35 21 35s21-20 21-35C53 13 44 4 32 4Z" fill="var(--accent)"/><circle cx="32" cy="25" r="12" fill="var(--reward)"/><text x="32" y="31" text-anchor="middle" font-size="16" font-weight="800" fill="var(--reward-ink)" font-family="Nunito Sans">€</text></svg></div>
        <h1>${claimP ? `¿Es tuyo ${esc(claimP.name)}?` : 'Crea tu cuenta o inicia sesión'}</h1>
        <p>${claimP ? 'Entra con una cuenta de negocio para declarar sus precios.' : 'Elige cómo vas a usar PrecioLocal. Si ya tienes cuenta, entra igual.'}</p></div>
      <div class="acc-types">
        ${card('cliente', 'Soy cliente', 'Descubre precios, aporta lo que pagas y gana monedas', ICON.users)}
        ${card('negocio', 'Tengo un negocio', 'Declara tus precios iniciales y da de alta tu negocio si no aparece', ICON.store)}
      </div>
      ${warnClient()}
      <button class="btn google" data-a="google">${GOOGLE_G}Continuar con Google</button>
      <button class="btn secondary" data-a="email">Continuar con correo electrónico</button>
      ${S.account ? `<button class="btn ghost" data-a="close">Cancelar</button>` : `<button class="btn ghost" data-a="skip">Ahora no, solo quiero mirar</button>`}
      <p class="acc-legal">${ICON.shield}<span>Solo guardamos tu nombre y tu correo. Tus aportaciones en clínicas, fisioterapia y dentistas son siempre anónimas.</span></p>`;
  } else if (a.step === 'login') {
    const g = a.method === 'google';
    html = back('toType') + `<div class="acc-sim">
        <div class="acc-sim-tag">${g ? GOOGLE_G + 'Simulación de «Continuar con Google»' : 'Simulación de registro con correo'}</div>
        <h2>${a.type === 'negocio' ? 'Cuenta de negocio' : 'Cuenta de cliente'}</h2>
        <p>${g ? 'En la app real se abre la ventana de Google, eliges tu cuenta y la app recibe tu nombre y tu correo verificados (Firebase Authentication). Aquí lo simulamos con datos de prueba.' : 'Te enviaríamos un enlace para confirmar el correo. Aquí lo simulamos.'}</p>
        <form id="accForm" novalidate>
          <div class="acc-field"><label for="accName">Nombre</label><input class="text-in" id="accName" name="name" maxlength="40" value="${a.type === 'negocio' ? 'Responsable del negocio' : 'Usuario de prueba'}" autocomplete="off">${err('accName')}</div>
          <div class="acc-field"><label for="accEmail">Correo</label><input class="text-in" id="accEmail" name="email" type="email" maxlength="60" value="${a.type === 'negocio' ? 'negocio.prueba@gmail.com' : 'usuario.prueba@gmail.com'}" autocomplete="off">${err('accEmail')}</div>
        </form>
        ${warnClient()}
        <button class="btn primary" data-a="loginOk">Continuar</button></div>`;
  } else if (a.step === 'business') {
    html = back(S.account ? 'close' : 'toType') + `<div class="acc-head left"><h1>¿Cuál es tu negocio?</h1>
        <p>Búscalo entre los lugares del mapa. Si no aparece, puedes darlo de alta tú.</p></div>
      <label class="search acc-search">${svg('<circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>')}<input id="bizQ" type="search" placeholder="Nombre del negocio" aria-label="Nombre de tu negocio" autocomplete="off" value="${esc(a.q)}"></label>
      <div class="acc-list" id="bizList">${bizListHTML(a.q)}</div>
      <button class="btn secondary" data-a="new">${ICON.building} Mi negocio no aparece · Darlo de alta</button>`;
  } else if (a.step === 'verify') {
    const p = placeById(a.pick);
    html = back('business') + `<div class="acc-head left"><h1>Verifica que es tuyo</h1><p>Vas a gestionar los precios de este negocio.</p></div>
      <div class="acc-place">${placeIconHTML(p)}<div><b>${esc(p.name)}</b><small>${esc(p.cat)}${p.addr ? ' · ' + esc(p.addr) : ''}</small></div></div>
      <div class="note">${ICON.shield}<span>Para evitar que alguien se haga pasar por un negocio, en la app real lo verificaríamos con un código enviado al teléfono o al correo del establecimiento, o con el CIF. En el prototipo se simula.</span></div>
      ${p.web ? `<div class="note">${ICON.info}<span>Ya tiene los precios publicados en tu web (${esc(p.web.fecha)}). Podrás revisarlos y corregirlos.</span></div>` : ''}
      <button class="btn primary" data-a="verifyOk">${ICON.check} Verificar y poner mis precios</button>`;
  } else if (a.step === 'new') {
    const t = BIZ_TYPES(), opt = cs => cs.map(c => `<option value="${esc(c)}">${esc(c)}</option>`).join('');
    html = back('business') + `<div class="acc-head left"><h1>Da de alta tu negocio</h1><p>Aparecerá en el mapa con el logo de su tipo. Después pondrás tus precios.</p></div>
      <form id="newBiz" novalidate>
        <div class="acc-field"><label for="newName">Nombre del negocio</label><input class="text-in" id="newName" name="name" maxlength="50" placeholder="Por ejemplo: Fisioterapia Bajo de Guía" autocomplete="off">${err('newName')}</div>
        <div class="acc-field"><label for="newCat">Tipo de negocio</label><select class="text-in" id="newCat" name="cat" required>
          <option value="" selected>Elige el tipo…</option>
          <optgroup label="Servicios locales (un precio por servicio)">${opt(t.servicio)}</optgroup>
          <optgroup label="Hostelería (rango por persona)">${opt(t.hosteleria)}</optgroup>
          <optgroup label="Ocio (precio por instalación o actividad)">${opt(t.ocio)}</optgroup></select>${err('newCat')}</div>
        <div class="acc-field"><label for="newAddr">Dirección</label><div class="acc-row"><input class="text-in" id="newAddr" maxlength="80" placeholder="Calle y número" autocomplete="off"><button type="button" class="btn secondary small" data-a="addrSearch">Buscar</button></div>${err('newAddr')}</div>
      </form>
      <div class="acc-list" id="addrList">${addrListHTML()}</div>
      <button class="btn primary" data-a="create">Dar de alta y poner precios</button>`;
  }
  // Foco: al título del paso; si se vuelve a pintar el mismo paso (p. ej. elegir cliente/negocio), al mismo control
  const act = document.activeElement, sig = el.contains(act) && el._step === a.step ? ctlSig(act) : null;
  el.innerHTML = html;
  el.scrollTop = 0;
  el.querySelectorAll('svg').forEach(x => x.setAttribute('aria-hidden', 'true'));
  const title = el.querySelector('h1, .acc-sim h2');
  if (title) { title.id = 'accTitle'; title.tabIndex = -1; el.setAttribute('aria-labelledby', 'accTitle'); el.removeAttribute('aria-label'); }
  (findCtl(el, [sig]) || title)?.focus({ preventScroll: true });
  el._step = a.step;
  if (a.step === 'business') {
    const q = $('#bizQ');
    q.addEventListener('input', () => { a.q = q.value; $('#bizList').innerHTML = bizListHTML(a.q); });
    setTimeout(() => q.focus(), 50);
  }
  if (a.step === 'new') $('#newAddr').addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); searchBizAddress(); } });
}

function bizListHTML(q) {
  const n = normTxt(q.trim());
  if (n.length < 2) return `<p class="acc-hint">Escribe al menos dos letras.</p>`;
  const list = PLACES.filter(p => normTxt(p.name).includes(n)).slice(0, 8);
  if (!list.length) return `<p class="acc-hint">No encontramos «${esc(q)}». Puedes darlo de alta abajo.</p>`;
  return list.map(p => `<button class="it" data-pick="${p.id}">${placeIconHTML(p)}<span class="lbl">${esc(p.name)}<small>${esc(p.cat)} · ${fmtDist(distM(USER_POS, p.pos))}</small></span>${ICON.chev}</button>`).join('');
}
function addrListHTML() {
  const a = S.acc;
  const rows = a.found.map((s, i) => `<button class="it ${a.pick === i ? 'on' : ''}" data-addr="${i}">${ICON.pin}<span class="lbl">${esc(s.name)}<small>${esc(s.sub || '')}</small></span>${a.pick === i ? ICON.check : ''}</button>`);
  rows.push(`<button class="it ${a.pick === 'here' ? 'on' : ''}" data-addr="here">${svg('<circle cx="12" cy="12" r="3"/><path d="M12 2v4M12 18v4M2 12h4M18 12h4"/>', 'ic sm')}<span class="lbl">Usar mi ubicación actual<small>Estás ahora en el local</small></span>${a.pick === 'here' ? ICON.check : ''}</button>`);
  return rows.join('');
}
// Si el usuario escribió el número de portal y el resultado no lo trae, se conserva
function addrWithNumber(name, q) {
  const num = String(q || '').match(/\b(\d{1,4}[a-z]?)\b/i)?.[1];
  if (!num || /\d/.test(name)) return name; // el resultado ya trae su número de portal
  return `${name}, ${num}`;
}
// Quita resultados repetidos: mismo nombre y detalle, o a menos de 15 m de otro
function dedupeSpots(list) {
  const key = s => normTxt(`${s.name}|${s.sub || ''}`), out = [];
  for (const s of list) if (!out.some(o => key(o) === key(s) || distM(o.pos, s.pos) < 15)) out.push(s);
  return out;
}
async function searchBizAddress() {
  const q = $('#newAddr').value.trim(); if (!q) return showErrors([[$('#newAddr'), 'Escribe la dirección']]);
  showErrors([]);
  $('#addrList').innerHTML = `<p class="acc-hint">Buscando…</p>`;
  try { S.acc.found = dedupeSpots(await searchAddresses(q + ', Sanlúcar de Barrameda')).slice(0, 4).map(s => ({ ...s, name: addrWithNumber(s.name, q) })); }
  catch { S.acc.found = []; toast('Sin conexión: usa tu ubicación actual'); }
  if (!S.acc) return; // se cerró la pantalla mientras buscaba
  S.acc.q = q;
  S.acc.pick = S.acc.found.length ? 0 : 'here';
  $('#addrList').innerHTML = addrListHTML();
}

// Muestra los errores junto a cada campo (texto bajo el campo, aria-invalid y aria-describedby), avisa y
// lleva el foco al primero. errs: [[campo, mensaje], …]. Devuelve true si hay errores.
function showErrors(errs) {
  const scr = $('#screen-cuenta');
  scr.querySelectorAll('[aria-invalid]').forEach(el => { el.removeAttribute('aria-invalid'); el.removeAttribute('aria-describedby'); });
  scr.querySelectorAll('.acc-err').forEach(el => { el.textContent = ''; });
  for (const [el, msg] of errs) {
    const e = document.getElementById(el.id + 'Err');
    if (e) { e.textContent = msg; el.setAttribute('aria-describedby', e.id); }
    el.setAttribute('aria-invalid', 'true');
  }
  if (errs.length) { errs[0][0].focus(); toast(errs[0][1]); }
  return errs.length > 0;
}

function createBusiness() {
  const fd = new FormData($('#newBiz')), a = S.acc;
  const name = String(fd.get('name') || '').trim(), cat = String(fd.get('cat') || '');
  const errs = [];
  if (!name) errs.push([$('#newName'), 'Escribe el nombre del negocio']);
  if (!cat) errs.push([$('#newCat'), 'Elige el tipo de negocio']);
  if (a.pick == null) errs.push([$('#newAddr'), 'Busca la dirección o usa tu ubicación actual']);
  if (showErrors(errs)) return;
  const typed = $('#newAddr').value.trim();
  const spot = a.pick === 'here' ? null : a.found[a.pick];
  const pos = spot ? spot.pos : [...USER_POS];
  const addr = spot ? spot.name : typed;
  const slug = normTxt(name).replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 40);
  const p = buildPlace({ id: `mi-${slug}-${Date.now().toString(36)}`, tpl: tplOfCat(cat), cat, name, addr, pos, custom: true });
  p.owner = true;
  addPlace(p);
  S.account.business = p.id;
  saveBusiness(p);
  finishLogin();
  toast('Negocio dado de alta: ahora pon tus precios');
  openPlace(p.id); // la ficha muestra el negocio con su dirección
  openOwner(p.id);
  // Sin dirección escrita: se busca la de tu ubicación actual
  if (!addr) reverseGeocode(pos).then(s => {
    if (!s || p.addr || S.account?.business !== p.id) return;
    p.addr = s.name; saveBusiness(p); refreshPlace(p);
  }).catch(() => { /* sin conexión: queda sin dirección */ });
}

function onAccountClick(e) {
  const b = e.target.closest('button'); if (!b) return;
  const a = S.acc, d = b.dataset;
  if (d.type) { a.type = d.type; return renderAccount(); }
  if (d.pick) { a.pick = d.pick; a.step = 'verify'; return renderAccount(); }
  if (d.addr != null) {
    a.pick = d.addr === 'here' ? 'here' : +d.addr; $('#addrList').innerHTML = addrListHTML();
    if ($('#newAddr')?.getAttribute('aria-invalid')) { $('#newAddr').removeAttribute('aria-invalid'); $('#newAddr').removeAttribute('aria-describedby'); $('#newAddrErr').textContent = ''; }
    return;
  }
  switch (d.a) {
    case 'google': case 'email': a.method = d.a; a.step = 'login'; return renderAccount();
    case 'toType': a.step = 'type'; return renderAccount();
    case 'skip': return skipAccount();
    case 'close': return cancelAccount();
    case 'loginOk': {
      const fd = new FormData($('#accForm'));
      const name = String(fd.get('name') || '').trim(), email = String(fd.get('email') || '').trim();
      const errs = [];
      if (!name) errs.push([$('#accName'), 'Escribe tu nombre']);
      if (!/^\S+@\S+\.\S+$/.test(email)) errs.push([$('#accEmail'), 'Escribe un correo válido, por ejemplo nombre@gmail.com']);
      if (showErrors(errs)) return;
      // Una cuenta de negocio que ya existía recupera su negocio al volver a entrar con el mismo correo
      const prev = a.type === 'negocio' ? readJSON(bizKey(email))?.id : null;
      switchAccount({ type: a.type, name, email, method: a.method, business: prev && placeById(prev) ? prev : null });
      if (a.type === 'cliente') { finishLogin(); return toast(`Hola, ${name}: tus monedas y aportaciones quedan guardadas`); }
      if (a.claim) { a.pick = a.claim; a.step = 'verify'; return renderAccount(); }
      if (S.account.business) {
        const p = placeById(S.account.business);
        finishLogin(); toast(`Hola, ${name}: has vuelto a ${p.name}`);
        return openPlace(p.id);
      }
      a.step = 'business';
      return renderAccount();
    }
    case 'new': a.step = 'new'; a.found = []; a.pick = null; return renderAccount();
    case 'verifyOk': {
      const p = placeById(a.pick);
      S.account.business = p.id; saveBusiness(p); finishLogin();
      toast(`Negocio verificado: ${p.name}`);
      return openOwner(p.id);
    }
    case 'addrSearch': return searchBizAddress();
    case 'create': return createBusiness();
  }
}

// ---------- Inicio ----------
$('#screen-cuenta').addEventListener('click', onAccountClick);
$('#screen-cuenta').addEventListener('submit', e => e.preventDefault());
// Escape: sale de la pantalla si hay sesión o ya eligió mirar sin cuenta; si no, equivale a "Ahora no"
document.addEventListener('keydown', e => {
  if (e.key !== 'Escape' || $('#screen-cuenta').hidden || !S.acc) return;
  if (S.account || store.get('pl-guest')) cancelAccount(); else skipAccount();
});
loadAccount();
loadClientState(loadPrices());
saveClientState();
if (store.get('pl-onb')) askAccountIfNeeded();
// Guardado: al salir u ocultar la página y, por si acaso, cada 4 s si algo cambió
addEventListener('pagehide', saveClientState);
document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'hidden') saveClientState(); });
setInterval(saveClientState, 4000);
// El marcador de posición se crea al cargar el mapa: entonces se aplica el cosmético guardado
const cosmeticsWait = setInterval(() => { if (window.__mapReady) { clearInterval(cosmeticsWait); applyCosmetics(); } }, 300);
Object.assign(window.proto, { openAccount, logout, saveClientState, loadClientState });
