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
function loadAccount() {
  try { S.account = JSON.parse(store.get('pl-account') || 'null'); } catch { S.account = null; }
  let biz = null;
  try { biz = JSON.parse(store.get('pl-negocio') || 'null'); } catch { /* datos dañados */ }
  if (!biz) return;
  let p = placeById(biz.id);
  if (!p && biz.custom) { p = buildPlace({ id: biz.id, tpl: biz.tpl, cat: biz.cat, name: biz.name, addr: biz.addr, pos: biz.pos, custom: true }); addPlace(p); }
  if (!p) return;
  if (biz.items) p.items = biz.items.map(i => mkItem(i.n, i.u, i));
  p.declaredRange = biz.declaredRange || null;
  p.web = biz.web ?? null;
  p.owner = !!biz.owner;
}
// Guarda lo que el negocio ha declarado (y, si lo dio de alta a mano, el propio negocio)
function saveBusiness(p) {
  store.set('pl-negocio', JSON.stringify({
    id: p.id, custom: !!p.custom, tpl: p.tpl, cat: p.cat, name: p.name, addr: p.addr, pos: p.pos,
    owner: p.owner, web: p.web, declaredRange: p.declaredRange,
    items: p.items.map(({ n, u, declared, g, desde, nota, ask }) => ({ n, u, declared, g, desde, nota, ask })),
  }));
}
function saveAccount() { if (S.account) store.set('pl-account', JSON.stringify(S.account)); else unstore('pl-account'); }

// ---------- Pantalla de cuenta ----------
function openAccount(opts = {}) {
  closePlace(); closeCatMenu(); if (S.page) closeProfile();
  const type = opts.type || 'cliente';
  S.acc = { step: opts.step || (S.account && opts.type === 'negocio' ? 'business' : 'type'), type, claim: opts.claim || null, method: 'google', q: '', found: [], pick: null };
  // Desde "¿Es tu negocio?" con una cuenta de cliente abierta: hay que entrar con una cuenta de negocio
  if (opts.claim && S.account?.type !== 'negocio') S.acc.step = 'type';
  $('#screen-cuenta').hidden = false;
  renderAccount();
}
function closeAccount() { S.acc = null; $('#screen-cuenta').hidden = true; }
function askAccountIfNeeded() { if (!S.account && !store.get('pl-guest')) openAccount(); }

function finishLogin() {
  saveAccount(); closeAccount(); updateHUD();
  if (S.place) renderPlace();
}
function logout() {
  const was = S.account?.name;
  S.account = null; saveAccount(); unstore('pl-guest'); updateHUD();
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
  const back = to => `<div class="topnav"><button class="back" data-a="${to}" aria-label="Volver">${ICON.back}</button><h1></h1></div>`;
  const claimP = a.claim ? placeById(a.claim) : null;
  let html = '';

  if (a.step === 'type') {
    const card = (t, title, sub, icon) => `<button class="acc-type ${a.type === t ? 'on' : ''}" data-type="${t}" aria-pressed="${a.type === t}">
      <span class="acc-ic">${icon}</span><span><b>${title}</b><small>${sub}</small></span><i class="radio"></i></button>`;
    html = `<div class="acc-head"><div class="logo"><svg viewBox="0 0 64 64" aria-hidden="true"><path d="M32 4C20 4 11 13 11 25c0 15 21 35 21 35s21-20 21-35C53 13 44 4 32 4Z" fill="var(--accent)"/><circle cx="32" cy="25" r="12" fill="var(--reward)"/><text x="32" y="31" text-anchor="middle" font-size="16" font-weight="800" fill="var(--reward-ink)" font-family="Nunito Sans">€</text></svg></div>
        <h1>${claimP ? `¿Es tuyo ${esc(claimP.name)}?` : 'Crea tu cuenta'}</h1>
        <p>${claimP ? 'Entra con una cuenta de negocio para declarar sus precios.' : 'Elige cómo vas a usar PrecioLocal. Si ya tienes cuenta, entra igual.'}</p></div>
      <div class="acc-types">
        ${card('cliente', 'Soy cliente', 'Descubre precios, aporta lo que pagas y gana monedas', ICON.users)}
        ${card('negocio', 'Tengo un negocio', 'Declara tus precios iniciales y da de alta tu negocio si no aparece', ICON.store)}
      </div>
      <button class="btn google" data-a="google">${GOOGLE_G}Continuar con Google</button>
      <button class="btn secondary" data-a="email">Continuar con correo electrónico</button>
      ${S.account ? '' : `<button class="btn ghost" data-a="skip">Ahora no, solo quiero mirar</button>`}
      <p class="acc-legal">${ICON.shield}<span>Solo guardamos tu nombre y tu correo. Tus aportaciones de precios en categorías de salud son siempre anónimas.</span></p>`;
  } else if (a.step === 'login') {
    const g = a.method === 'google';
    html = back('toType') + `<div class="acc-sim">
        <div class="acc-sim-tag">${g ? GOOGLE_G + 'Simulación de «Continuar con Google»' : 'Simulación de registro con correo'}</div>
        <h2>${a.type === 'negocio' ? 'Cuenta de negocio' : 'Cuenta de cliente'}</h2>
        <p>${g ? 'En la app real se abre la ventana de Google, eliges tu cuenta y la app recibe tu nombre y tu correo verificados (Firebase Authentication). Aquí lo simulamos con datos de prueba.' : 'En la app real te enviaríamos un enlace al correo para confirmarlo. Aquí lo simulamos.'}</p>
        <form id="accForm">
          <label class="acc-field"><span>Nombre</span><input class="text-in" name="name" maxlength="40" value="${a.type === 'negocio' ? 'Responsable del negocio' : 'Usuario de prueba'}" autocomplete="off"></label>
          <label class="acc-field"><span>Correo</span><input class="text-in" name="email" type="email" maxlength="60" value="${a.type === 'negocio' ? 'negocio.prueba@gmail.com' : 'usuario.prueba@gmail.com'}" autocomplete="off"></label>
          ${g ? '' : '<label class="acc-field"><span>Contraseña</span><input class="text-in" name="pass" type="password" value="prueba1234" autocomplete="off"></label>'}
        </form>
        <button class="btn primary" data-a="loginOk">Continuar</button></div>`;
  } else if (a.step === 'business') {
    html = back(S.account ? 'close' : 'toType') + `<div class="acc-head left"><h1>¿Cuál es tu negocio?</h1>
        <p>Búscalo entre los lugares del mapa. Si no aparece, puedes darlo de alta tú.</p></div>
      <label class="search acc-search">${svg('<circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>')}<input id="bizQ" type="search" placeholder="Nombre del negocio" autocomplete="off" value="${esc(a.q)}"></label>
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
    const t = BIZ_TYPES(), opt = cs => cs.map(c => `<option>${esc(c)}</option>`).join('');
    html = back('business') + `<div class="acc-head left"><h1>Da de alta tu negocio</h1><p>Aparecerá en el mapa con el logo de su tipo. Después pondrás tus precios.</p></div>
      <form id="newBiz">
        <label class="acc-field"><span>Nombre del negocio</span><input class="text-in" name="name" maxlength="50" placeholder="Por ejemplo: Fisioterapia Bajo de Guía" autocomplete="off"></label>
        <label class="acc-field"><span>Tipo de negocio</span><select class="text-in" name="cat">
          <optgroup label="Servicios locales (un precio por servicio)">${opt(t.servicio)}</optgroup>
          <optgroup label="Hostelería (rango por persona)">${opt(t.hosteleria)}</optgroup>
          <optgroup label="Ocio (precio por instalación o actividad)">${opt(t.ocio)}</optgroup></select></label>
        <div class="acc-field"><span>Dirección</span><div class="acc-row"><input class="text-in" id="newAddr" maxlength="80" placeholder="Calle y número" autocomplete="off"><button type="button" class="btn secondary small" data-a="addrSearch">Buscar</button></div></div>
      </form>
      <div class="acc-list" id="addrList">${addrListHTML()}</div>
      <button class="btn primary" data-a="create">Dar de alta y poner precios</button>`;
  }
  el.innerHTML = html;
  el.scrollTop = 0;
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
  rows.push(`<button class="it ${a.pick === 'center' ? 'on' : ''}" data-addr="center">${svg('<circle cx="12" cy="12" r="3"/><path d="M12 2v4M12 18v4M2 12h4M18 12h4"/>', 'ic sm')}<span class="lbl">Usar el centro del mapa<small>Mueve el mapa antes hasta tu local</small></span>${a.pick === 'center' ? ICON.check : ''}</button>`);
  return rows.join('');
}
async function searchBizAddress() {
  const q = $('#newAddr').value.trim(); if (!q) return toast('Escribe la dirección');
  $('#addrList').innerHTML = `<p class="acc-hint">Buscando…</p>`;
  try { S.acc.found = (await searchAddresses(q + ', Sanlúcar de Barrameda')).slice(0, 4); }
  catch { S.acc.found = []; toast('Sin conexión: usa el centro del mapa'); }
  S.acc.pick = S.acc.found.length ? 0 : 'center';
  $('#addrList').innerHTML = addrListHTML();
}

function createBusiness() {
  const fd = new FormData($('#newBiz')), a = S.acc;
  const name = String(fd.get('name') || '').trim(), cat = String(fd.get('cat') || '');
  if (!name) return toast('Escribe el nombre del negocio');
  if (a.pick == null) return toast('Busca la dirección o usa el centro del mapa');
  const spot = a.pick === 'center' ? null : a.found[a.pick];
  const pos = spot ? spot.pos : (map ? map.getCenter().toArray() : USER_POS);
  const slug = normTxt(name).replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 40);
  const p = buildPlace({ id: `mi-${slug}-${Date.now().toString(36)}`, tpl: tplOfCat(cat), cat, name, addr: spot ? spot.name : '', pos, custom: true });
  p.owner = true;
  addPlace(p);
  S.account.business = p.id;
  saveBusiness(p);
  finishLogin();
  toast('Negocio dado de alta: ahora pon tus precios');
  openOwner(p.id);
}

function onAccountClick(e) {
  const b = e.target.closest('button'); if (!b) return;
  const a = S.acc, d = b.dataset;
  if (d.type) { a.type = d.type; return renderAccount(); }
  if (d.pick) { a.pick = d.pick; a.step = 'verify'; return renderAccount(); }
  if (d.addr != null) { a.pick = d.addr === 'center' ? 'center' : +d.addr; $('#addrList').innerHTML = addrListHTML(); return; }
  switch (d.a) {
    case 'google': case 'email': a.method = d.a; a.step = 'login'; return renderAccount();
    case 'toType': a.step = 'type'; return renderAccount();
    case 'skip': store.set('pl-guest', '1'); closeAccount(); return toast('Puedes crear tu cuenta cuando quieras desde el perfil');
    case 'close': return closeAccount();
    case 'loginOk': {
      const fd = new FormData($('#accForm'));
      const name = String(fd.get('name') || '').trim(), email = String(fd.get('email') || '').trim();
      if (!name || !/^\S+@\S+\.\S+$/.test(email)) return toast('Revisa el nombre y el correo');
      S.account = { type: a.type, name, email, method: a.method, business: null };
      saveAccount();
      if (a.type === 'cliente') { finishLogin(); return toast(`Hola, ${name}: tus monedas y aportaciones quedan guardadas`); }
      if (a.claim) { a.pick = a.claim; a.step = 'verify'; } else a.step = 'business';
      return renderAccount();
    }
    case 'new': a.step = 'new'; a.found = []; a.pick = null; return renderAccount();
    case 'verifyOk': {
      const p = placeById(a.pick);
      S.account.business = p.id; finishLogin();
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
loadAccount();
updateHUD();
if (store.get('pl-onb')) askAccountIfNeeded();
Object.assign(window.proto, { openAccount, logout });
