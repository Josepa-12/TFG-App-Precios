/* PrecioLocal — funciones añadidas el 16/11/2026
 *  1. Frescura del precio: cada precio indica cuándo se actualizó por última vez, con un punto (forma y color)
 *     que depende del tipo de negocio (la hostelería cambia antes). Confirmar un precio antiguo da monedas extra.
 *  2. Ofertas del negocio: las publica la cuenta de negocio con fecha de caducidad; se ven en la ficha y
 *     con una etiqueta muy pequeña en una esquina del marcador. Los clientes pueden avisar de que ya no está.
 *  3. Plan de tarde: se añaden sitios desde la ficha; el plan calcula el gasto estimado para N personas
 *     y guía la ruta parada a parada.
 *  4. "¿Algún precio está mal? Avisa de un error": avisos de error con peso según la reputación. Con 3 puntos
 *     el precio queda "en revisión" y la app pide a los siguientes clientes que lo confirmen.
 */

const DAY = 86400000;
const now = () => Date.now();

// ---------- 1. Frescura ----------
// [días hasta "podría haber cambiado", días hasta "antiguo"] según la plantilla de precios
const FRESH = { hosteleria: [30, 60], servicio: [90, 180], ocio: [90, 180] };
const FRESH_TXT = { fresh: 'Al día', aging: 'Podría haber cambiado', stale: 'Precio antiguo' };
const REWARD_STALE = { aging: { c: 5, xp: 5 }, stale: { c: 10, xp: 10 } }; // extra por confirmar precios viejos
function freshState(p, at) {
  if (at == null) return 'fresh';
  const days = (now() - at) / DAY, [g, s] = FRESH[p.tpl] || FRESH.servicio;
  return days < g ? 'fresh' : days < s ? 'aging' : 'stale';
}
function ageText(at) {
  const d = Math.floor((now() - at) / DAY);
  if (d < 1) return 'hoy';
  if (d === 1) return 'ayer';
  if (d < 30) return `hace ${d} días`;
  const m = Math.round(d / 30);
  return m === 1 ? 'hace 1 mes' : `hace ${m} meses`;
}
const itemAt = (p, it) => (p.tpl === 'hosteleria' ? p.rangeAt : it?.at);
// Texto accesible del punto (la forma también cambia: ● al día, ◐ podría haber cambiado, ○ antiguo)
const FRESH_SR = { fresh: 'al día', aging: 'podría haber cambiado', stale: 'precio antiguo' };
// ¿El dato sale de la web del negocio y aún no lo ha confirmado ningún cliente?
function fromWebAt(p, at) {
  if (!p.web) return false;
  if (p.tpl === 'hosteleria') return !p.rangeReports?.length;
  const it = p.items.find(i => i.at === at);
  return !!it && !it.reports?.length;
}
const whenText = (p, at) => (fromWebAt(p, at) ? 'consultado en su web ' : 'actualizado ') + ageText(at);
// Punto junto a cada precio: forma + color, con texto para lectores de pantalla
function freshDot(p, at) {
  const st = freshState(p, at);
  return `<i class="fr ${st}" aria-hidden="true" title="${FRESH_TXT[st]}${at ? ' · ' + whenText(p, at) : ''}"></i><span class="fr-sr">${FRESH_SR[st]}: </span>`;
}
const FRESH_LEGEND = `<span class="fr-legend" aria-hidden="true"><i class="fr fresh"></i>al día · <i class="fr aging"></i>podría haber cambiado · <i class="fr stale"></i>antiguo</span>`;
// Resumen de la ficha: el estado del precio actualizado más reciente
function freshHTML(p) {
  const ats = p.tpl === 'hosteleria' ? [p.rangeAt] : p.items.filter(i => i.p != null).map(i => i.at);
  const at = Math.max(...ats.filter(x => x != null)); if (!isFinite(at)) return '';
  const st = freshState(p, at), [g, s] = FRESH[p.tpl] || FRESH.servicio;
  const extra = st === 'fresh' ? '' : ` · confírmalo y gana +${REWARD_STALE[st].c} monedas extra`;
  const span = p.tpl === 'hosteleria' ? `En hostelería, al día hasta ${g} días; antiguo desde ${s} días` : `Al día hasta ${g / 30} meses; antiguo desde ${s / 30} meses`;
  return `<div class="freshness ${st}"><i class="fr ${st}" aria-hidden="true"></i><span><b>${FRESH_TXT[st]}</b> · ${whenText(p, at)}${extra}<small>${span}</small>${FRESH_LEGEND}</span></div>`;
}

// ---------- 2. Ofertas del negocio ----------
const fmtDay = ms => new Date(ms).toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
const startOfToday = () => { const d = new Date(); d.setHours(0, 0, 0, 0); return d.getTime(); };
const OFFER_GONE = 3; // avisos de "ya no está" para retirar la oferta
const activeOffers = p => (p.offers || []).filter(o => o.until >= startOfToday() && o.gone < OFFER_GONE);
function offersHTML(p, mine) {
  const list = activeOffers(p); if (!list.length) return '';
  return list.map(o => `<div class="offer"><span class="offer-tag">Oferta</span>
    <div class="offer-body"><b>${esc(o.text)}</b><small>Hasta el ${fmtDay(o.until)}${o.gone ? ` · ${o.gone} ${o.gone === 1 ? 'aviso' : 'avisos'} de que ya no está` : ''}</small></div>
    ${o.price != null ? `<strong>${fmtEur(o.price)}</strong>` : ''}
    ${mine || S.account?.type === 'negocio' || S.reportedOffers.includes(o.id) ? '' : `<button class="offer-gone" data-act="offerGone" data-offer="${o.id}">Ya no está</button>`}</div>`).join('');
}
// Nombre accesible del marcador: añade o quita " · oferta activa" (sin duplicarlo)
const OFFER_SFX = ' · oferta activa';
function labelOffer(el, on) {
  const cur = el.getAttribute('aria-label') || '', base = cur.split(OFFER_SFX).join(''), next = on ? base + OFFER_SFX : base;
  if (next !== cur) el.setAttribute('aria-label', next);
}
function updateOfferTag(p) {
  const el = pinOf(p.id); if (!el) return;
  const on = activeOffers(p).length > 0;
  let tag = el.querySelector('.pin-offer');
  if (on && !tag) { tag = document.createElement('i'); tag.className = 'pin-offer'; tag.setAttribute('aria-hidden', 'true'); tag.textContent = '%'; el.appendChild(tag); }
  else if (!on && tag) tag.remove();
  labelOffer(el, on);
  // Si otra parte de la app reescribe la etiqueta del marcador, se vuelve a añadir el sufijo
  if (!el._offerObs) { el._offerObs = new MutationObserver(() => labelOffer(el, activeOffers(p).length > 0)); el._offerObs.observe(el, { attributes: true, attributeFilter: ['aria-label'] }); }
}
function openOffers(id) { S.flow = { id, step: 'offers' }; renderFlow(); }
function renderOffers(card) {
  const p = placeById(S.flow.id), list = activeOffers(p);
  const def = new Date(now() + 7 * DAY).toISOString().slice(0, 10), min = new Date().toISOString().slice(0, 10);
  card.innerHTML = `<div class="flow-step">Modo negocio</div><h2>Ofertas de ${esc(p.name)}</h2>
    <p>Publica una oferta con fecha de fin. Se verá en tu ficha y con una etiqueta en tu marcador. Si varios clientes avisan de que ya no está, se retira.</p>
    ${list.length ? `<div class="options">${list.map(o => `<div class="option offer-own"><span>${esc(o.text)}${o.price != null ? ` · ${fmtEur(o.price)}` : ''}<small>Hasta el ${fmtDay(o.until)}</small></span><button class="btn ghost small" data-del="${o.id}">Quitar</button></div>`).join('')}</div>` : ''}
    <form id="offerForm">
      <label class="acc-field"><span>Oferta</span><input class="text-in" name="text" id="offText" maxlength="60" placeholder="Por ejemplo: martes, corte + barba" autocomplete="off"></label>
      <div class="acc-row">
        <label class="acc-field" style="flex:1"><span>Precio (opcional)</span><input class="num-in" name="price" id="offPrice" type="number" inputmode="decimal" min="0" step="0.5" placeholder="€"></label>
        <label class="acc-field" style="flex:1.3"><span>Válida hasta</span><input class="text-in" name="until" id="offUntil" type="date" value="${def}" min="${min}"></label>
      </div>
    </form>
    <div class="flow-actions two"><button class="btn ghost" data-flow="close">Cerrar</button><button class="btn primary" data-flow="offerAdd">Publicar oferta</button></div>`;
}

// ---------- 3. Plan de tarde ----------
const planKey = d => d.key || d.id;
const inPlan = d => S.plan.stops.findIndex(s => planKey(s.d) === planKey(d));
// Aviso arriba de la ficha cuando este sitio es la siguiente parada del plan
function planNextHTML(d) {
  const next = S.plan.active != null && S.plan.stops[S.plan.active] && planKey(S.plan.stops[S.plan.active].d) === planKey(d);
  return next ? `<div class="plan-next">${ICON.route}<span>Parada ${S.plan.active + 1} de ${S.plan.stops.length} de tu plan de tarde</span></div>` : '';
}
function planBtnHTML(d) {
  const i = inPlan(d);
  return `<button class="plan-row ${i >= 0 ? 'on' : ''}" data-act="plan">${ICON.list}<span>${i >= 0 ? `En tu plan de tarde · parada ${i + 1}` : 'Añadir al plan de tarde'}<small>${i >= 0 ? 'Toca para quitarlo del plan' : 'Junta varios sitios y calcula el gasto total'}</small></span>${i >= 0 ? ICON.check : '<b class="plus">+</b>'}</button>`;
}
// Cambia el orden o quita paradas sin perder cuál es la parada activa del plan
function editStops(fn) {
  const pl = S.plan, act = pl.active != null ? pl.stops[pl.active] : null;
  fn(pl.stops);
  if (!act) { pl.active = null; return; }
  const j = pl.stops.indexOf(act);
  if (j >= 0) pl.active = j;
  else pl.active = pl.active < pl.stops.length ? pl.active : null; // se quitó la activa: pasa a la siguiente
}
function togglePlan(d) {
  const i = inPlan(d);
  if (i >= 0) { editStops(a => a.splice(i, 1)); toast('Quitado del plan'); }
  else {
    const p = d.kind === 'place' ? placeById(d.id) : null;
    const item = p && p.tpl !== 'hosteleria' ? Math.max(0, p.items.findIndex(x => x.p != null)) : null;
    S.plan.stops.push({ d: { ...d }, item }); toast(`Añadido al plan · ${S.plan.stops.length} ${S.plan.stops.length === 1 ? 'parada' : 'paradas'}`);
  }
  updatePlanChip(); renderPlace();
}
// Precio "desde": publicado así en la web del negocio (mientras se muestre ese precio)
const isFrom = it => !!it && !!it.desde && it.p === it.declared;
// Gasto de una parada para el grupo: [mínimo, máximo] (+ from: solo "desde"; k: personas por las que se multiplica)
// o null si aún no hay precio
function stopCost(s) {
  const p = s.d.kind === 'place' ? placeById(s.d.id) : null, n = S.plan.people;
  if (!p || !priced(p)) return null;
  if (p.tpl === 'hosteleria') return Object.assign([p.range[0] * n, p.range[1] * n], { from: !!p.rangeOpen, k: n });
  const it = p.items[s.item]; if (!it || it.p == null) return null;
  const k = it.u === 'h' || it.u === 'día' ? 1 : n; // alquiler de pista o aparcamiento: precio total
  return Object.assign([it.p * k, it.p * k], { from: isFrom(it), k });
}
function planTotal() {
  let lo = 0, hi = 0, missing = 0, from = false;
  for (const s of S.plan.stops) { const c = stopCost(s); if (c) { lo += c[0]; hi += c[1]; from = from || c.from; } else missing++; }
  return { lo, hi, missing, from };
}
const planRange = (lo, hi) => (Math.round(lo) === Math.round(hi) ? fmtEur(Math.round(lo)) : `${Math.round(lo)}–${Math.round(hi)} €`);
// Coste con "desde" cuando no se conoce el máximo
const fmtCost = (lo, hi, from) => (from ? `desde ${fmtEur(Math.round(lo))}` : planRange(lo, hi));
function updatePlanChip() {
  const c = $('#planChip'), n = S.plan.stops.length;
  c.hidden = !n || !!S.navigating;
  if (!n) return;
  const t = planTotal(), has = t.lo || t.hi;
  c.innerHTML = `${ICON.list}<span>Plan · ${n} ${n === 1 ? 'parada' : 'paradas'}${has ? ` · ${fmtCost(t.lo, t.hi, t.from || t.missing > 0)}` : ''}</span>`;
}
function openPlan() { S.flow = { step: 'plan' }; renderFlow(); }
function renderPlan(card) {
  const st = S.plan.stops, t = planTotal();
  let metros = 0, prev = USER_POS; for (const s of st) { metros += distM(prev, s.d.pos) * 1.3; prev = s.d.pos; } // aprox. por calles
  const short = (s, n) => (s.length > n ? s.slice(0, n - 1).trimEnd() + '…' : s); // se recorta el nombre, nunca el precio
  const rows = st.map((s, i) => {
    const p = s.d.kind === 'place' ? placeById(s.d.id) : null, c = stopCost(s);
    const pick = p && p.tpl !== 'hosteleria' && p.items.some(x => x.p != null)
      ? `<select class="plan-item" data-stop="${i}" aria-label="Qué vas a usar en ${esc(p.name)}">${p.items.map((x, k) => x.p == null ? '' : `<option value="${k}" ${k === s.item ? 'selected' : ''}>${esc(short(x.n, 24))} · ${isFrom(x) ? 'desde ' : ''}${fmtEur(x.p)}${UNIT[x.u]}</option>`).join('')}</select>` : '';
    const what = !p ? 'Sin precios (dirección o lugar del mapa)' : !priced(p) ? 'Aún sin precio'
      : p.tpl === 'hosteleria' ? `${fmtRange(p)} por persona` : ''; // fmtRange(p) de app.js: "Más de 50 €" si el tramo es abierto
    return `<div class="plan-stop"><div class="plan-head"><span class="plan-n">${i + 1}</span>
      <div class="plan-info"><b>${esc(s.d.name)}</b>${what ? `<small>${what}</small>` : ''}</div></div>${pick}
      <div class="plan-foot"><div class="plan-cost">${c ? fmtCost(c[0], c[1], c.from) : 'Sin precio'}${c && c.k > 1 ? ` <span class="plan-x" aria-label="por ${c.k} personas">×${c.k}</span>` : ''}</div>
      <div class="plan-ops"><button data-up="${i}" aria-label="Subir ${esc(s.d.name)}" ${i ? '' : 'disabled'}>↑</button><button data-down="${i}" aria-label="Bajar ${esc(s.d.name)}" ${i < st.length - 1 ? '' : 'disabled'}>↓</button><button data-rm="${i}" aria-label="Quitar ${esc(s.d.name)} del plan">${ICON.x}</button></div></div></div>`;
  }).join('');
  const has = t.lo || t.hi, n = S.plan.people;
  const total = has ? fmtCost(t.lo, t.hi, t.from) : '—';
  const clearing = !!S.flow.confirmClear;
  card.innerHTML = `<h2>Plan de tarde</h2><p>Tus paradas en orden. El gasto se estima con los precios de la app.</p>
    <div class="plan-people"><span>Personas</span><div class="stepper"><button data-people="-1" aria-label="Una persona menos">−</button><b aria-live="polite">${n}</b><button data-people="1" aria-label="Una persona más">+</button></div></div>
    ${st.length ? `<div class="plan-list">${rows}</div>` : `<div class="text-card">Añade sitios desde su ficha con "Añadir al plan de tarde".</div>`}
    ${st.length ? `<div class="plan-total"><span>Gasto estimado${n > 1 ? ` para ${n} personas` : ''}</span><strong>${total}${t.missing ? `<small>+ ${t.missing} sin precio</small>` : ''}</strong></div>
    <p class="hint">${t.missing ? `${t.missing} ${t.missing === 1 ? 'parada no tiene' : 'paradas no tienen'} precio todavía y no ${t.missing === 1 ? 'cuenta' : 'cuentan'}. ` : ''}${t.from ? 'Algunos precios son "desde": el gasto real puede ser mayor. ' : ''}Recorrido aproximado: ${fmtDist(metros)}. Las pistas y los aparcamientos cuentan una vez para el grupo.</p>
    <button class="plan-clear${clearing ? ' confirm' : ''}" data-flow="planClear">${clearing ? '¿Vaciar el plan? Toca otra vez para confirmar' : 'Vaciar plan'}</button>` : ''}
    <div class="flow-actions two"><button class="btn ghost" data-flow="close">Cerrar</button><button class="btn primary" data-flow="planGo" ${st.length ? '' : 'disabled'}>${ICON.nav} Empezar ruta</button></div>`;
}
function startPlan() {
  S.plan.active = 0; endFlow(); updatePlanChip();
  openDest(S.plan.stops[0].d); startNav();
}
// Al terminar la ruta de una parada del plan: abre la siguiente
function planAfterNav(nav) {
  const pl = S.plan; if (pl.active == null || !nav) return;
  const cur = pl.stops[pl.active]; if (!cur || planKey(cur.d) !== planKey(nav.dest)) return;
  if (!nav.arrived) return; // salió antes de llegar: el plan sigue en la misma parada
  pl.active++;
  if (pl.active >= pl.stops.length) { pl.active = null; updatePlanChip(); return toast('¡Plan de tarde completado!'); }
  const next = pl.stops[pl.active];
  setTimeout(() => { openDest(next.d); toast(`Siguiente parada: ${next.d.name}`); }, 950);
}
const planNavText = d => {
  const pl = S.plan; if (pl.active == null) return '';
  const cur = pl.stops[pl.active];
  return cur && planKey(cur.d) === planKey(d) ? `Parada ${pl.active + 1} de ${pl.stops.length} · ` : '';
};

// ---------- 4. Avisos de error en el precio y reputación ----------
const REVIEW_AT = 3; // puntos de aviso para dejar un precio "en revisión"
function reputation() {
  const n = S.contributions;
  if (n >= 20) return { name: 'Experta', w: 2, next: null };
  if (n >= 5) return { name: 'Fiable', w: 1.5, next: 20 - n };
  return { name: 'Nueva', w: 1, next: 5 - n };
}
const fmtW = w => String(w).replace('.', ',');
// Precios de los que este usuario aún puede avisar (un aviso por usuario y precio, también en hostelería)
function reportable(p) {
  if (p.tpl === 'hosteleria') return p.range && !S.reported.includes(`${p.id}:0`) ? [0] : [];
  return p.items.map((x, i) => (x.p != null && !S.reported.includes(`${p.id}:${i}`) ? i : -1)).filter(i => i >= 0);
}
function reportLinkHTML(p) {
  if (!priced(p) || S.account?.type === 'negocio') return '';
  if (!reportable(p).length) return `<p class="report-done">${ICON.check}<span>Ya has avisado de todos los precios de este sitio</span></p>`;
  return `<button class="report-link" data-act="report">${ICON.pending}<span>¿Algún precio está mal? Avisa de un error</span></button>`;
}
const reviewChip = '<span class="rev">En revisión</span>';
function openReport(id) {
  const p = placeById(id);
  if (!p || !reportable(p).length) return toast('Ya has avisado de todos los precios de este sitio');
  S.flow = { id, step: 'report', item: null }; renderFlow();
}
const REASONS = [['changed', 'Ahora cuesta otra cantidad'], ['gone', 'Ya no ofrecen este servicio'], ['closed', 'El negocio ha cerrado']];
function renderReport(card) {
  const f = S.flow, p = placeById(f.id), rep = reputation();
  const single = p.tpl === 'hosteleria', left = reportable(p);
  if (!single && f.item == null) {
    card.innerHTML = `<div class="flow-step">Avisar de un error en el precio</div><h2>¿Qué precio está mal?</h2><p>Elige el servicio.</p>
      <div class="options">${left.map(i => { const x = p.items[i]; return `<button class="option" data-ritem="${i}"><span>${esc(x.n)}</span><small>${isFrom(x) ? 'desde ' : ''}${fmtEur(x.p)}${UNIT[x.u]}</small></button>`; }).join('')}</div>
      <div class="flow-actions"><button class="btn ghost" data-flow="close">Cancelar</button></div>`;
    return;
  }
  const what = single ? `Gasto por persona: ${fmtRange(p)}` : `${p.items[f.item].n}: ${fmtEur(p.items[f.item].p)}`;
  card.innerHTML = `<div class="flow-step">Avisar de un error en el precio</div><h2>¿Qué ocurre?</h2><p>${esc(what)}</p>
    <div class="options">${REASONS.map(([k, l]) => `<button class="option" data-reason="${k}"><span>${l}</span></button>`).join('')}</div>
    <div class="note">${ICON.users}<span>Tu reputación: <b>${rep.name}</b>. Tus avisos de error cuentan ×${fmtW(rep.w)}. Con ${REVIEW_AT} puntos, el precio queda en revisión y se pide a los siguientes clientes que lo confirmen.</span></div>
    <div class="flow-actions"><button class="btn ghost" data-flow="close">Cancelar</button></div>`;
}
// Registra un aviso con un peso; devuelve true si el precio pasa a revisión
function addFlag(p, idx, w, reason) {
  p.flagLog = p.flagLog || [];
  p.flagLog.unshift({ item: idx, reason, w, when: now() });
  if (p.tpl === 'hosteleria') { p.rangeFlags += w; if (p.rangeFlags >= REVIEW_AT && !p.rangeReview) return (p.rangeReview = true); return false; }
  const it = p.items[idx]; it.flags += w;
  if (it.flags >= REVIEW_AT && !it.review) return (it.review = true);
  return false;
}
// Un cliente ha confirmado o aportado el precio: se limpian los avisos
function clearFlags(p, it) {
  if (p.tpl === 'hosteleria') { p.rangeFlags = 0; p.rangeReview = false; p.rangeAt = now(); }
  else { it.flags = 0; it.review = false; it.at = now(); }
}
const inReview = p => (p.tpl === 'hosteleria' ? p.rangeReview : p.items.some(i => i.review));

// ---------- Flujos (tarjeta inferior) ----------
const EXTRA_STEPS = { offers: renderOffers, plan: renderPlan, report: renderReport };
function onExtraFlowClick(e, b) {
  const f = S.flow, d = b.dataset;
  if (d.flow === 'close') return endFlow();
  if (f.step === 'offers') {
    const p = placeById(f.id);
    if (d.del) { p.offers = p.offers.filter(o => o.id !== d.del); saveBusiness(p); updateOfferTag(p); refreshPlace(p); focusFlowNext('[data-del]', '#offText'); return renderFlow(); }
    if (d.flow === 'offerAdd') {
      e.preventDefault();
      const fd = new FormData($('#offerForm')), text = String(fd.get('text') || '').trim();
      const price = parseFloat(String(fd.get('price') || '').replace(',', '.')), until = Date.parse(fd.get('until') + 'T23:59:59');
      // Error bajo el campo (aria-invalid + aria-describedby) y foco a ese campo, además del aviso
      if (!text) return fieldErrors($('#flowCard'), [[$('#offText'), 'Escribe la oferta']]);
      if (!isFinite(until) || until < startOfToday()) return fieldErrors($('#flowCard'), [[$('#offUntil'), 'Elige una fecha de fin válida']]);
      // Precio opcional: vacío vale; negativo o no numérico se avisa (como en los precios del negocio)
      if ((String(fd.get('price') || '').trim() || $('#offPrice').validity.badInput) && !(isFinite(price) && price >= 0))
        return fieldErrors($('#flowCard'), [[$('#offPrice'), 'Escribe un precio válido (0 € o más) o déjalo vacío']]);
      p.offers.push({ id: 'of' + now().toString(36), text, price: isFinite(price) && price >= 0 ? price : null, until, gone: 0 });
      saveBusiness(p); updateOfferTag(p); refreshPlace(p);
      toast('Oferta publicada'); focusFlowNext('#offText'); return renderFlow();
    }
    return;
  }
  if (f.step === 'plan') {
    // "Vaciar plan" pide un segundo toque; cualquier otra acción anula la confirmación
    if (d.flow === 'planClear') {
      if (!f.confirmClear) { f.confirmClear = true; focusFlowNext('[data-flow="planClear"]'); return renderFlow(); }
      S.plan.stops = []; S.plan.active = null; updatePlanChip(); toast('Plan vaciado'); return endFlow();
    }
    f.confirmClear = false;
    if (d.people) { S.plan.people = Math.max(1, Math.min(20, S.plan.people + +d.people)); updatePlanChip(); return renderFlow(); }
    const mv = (i, j) => editStops(a => { [a[i], a[j]] = [a[j], a[i]]; });
    // El foco sigue a la parada movida (si su botón queda desactivado, al otro botón de la misma parada)
    if (d.up) { const j = +d.up - 1; mv(+d.up, j); focusFlowNext(`[data-up="${j}"]`, `[data-down="${j}"]`); return renderFlow(); }
    if (d.down) { const j = +d.down + 1; mv(+d.down, j); focusFlowNext(`[data-down="${j}"]`, `[data-up="${j}"]`); return renderFlow(); }
    if (d.rm) {
      editStops(a => a.splice(+d.rm, 1)); updatePlanChip();
      const j = Math.min(+d.rm, S.plan.stops.length - 1); focusFlowNext(`[data-rm="${j}"]`);
      return S.plan.stops.length ? renderFlow() : endFlow();
    }
    if (d.flow === 'planGo') return startPlan();
    return;
  }
  if (f.step === 'report') {
    const p = placeById(f.id);
    if (d.ritem) { f.item = +d.ritem; return renderFlow(); }
    if (d.reason) {
      const idx = p.tpl === 'hosteleria' ? 0 : f.item, rep = reputation(), key = `${p.id}:${idx}`;
      if (S.reported.includes(key)) { endFlow(); return toast('Ya has enviado un aviso de error de este precio'); }
      S.reported.push(key);
      const toReview = addFlag(p, idx, rep.w, d.reason);
      endFlow(); refreshPlace(p);
      return toast(toReview ? 'Gracias por tu aviso de error: el precio queda en revisión hasta que lo confirmen los clientes' : `Gracias por tu aviso de error (×${fmtW(rep.w)}). Con ${REVIEW_AT} puntos, el precio pasa a revisión`);
    }
  }
}
function onExtraAct(a, el) {
  const d = S.dest; if (!d) return;
  if (a === 'plan') return togglePlan(d);
  if (a === 'report') return openReport(S.place);
  if (a === 'offerGone') {
    if (S.account?.type === 'negocio') return; // las cuentas de negocio no avisan de ofertas
    const p = placeById(S.place), o = p.offers.find(x => x.id === el.dataset.offer); if (!o || S.reportedOffers.includes(o.id)) return;
    o.gone++; S.reportedOffers.push(o.id);
    if (o.gone >= OFFER_GONE) toast('Oferta retirada: varios clientes dicen que ya no está'); else toast('Gracias: si más clientes lo confirman, la retiramos');
    updateOfferTag(p); renderPlace();
  }
}

// ---------- Inicio ----------
S.plan = { stops: [], people: 1, active: null };
S.reported = []; S.reportedOffers = [];
PLACES.forEach(updateOfferTag); // marcadores creados antes de cargar este archivo
$('#planChip').addEventListener('click', openPlan);
$('#flowCard').addEventListener('change', e => {
  const sel = e.target.closest('.plan-item'); if (!sel) return;
  S.plan.stops[+sel.dataset.stop].item = +sel.value; S.flow.confirmClear = false; updatePlanChip(); renderFlow();
});
// Ganchos para las capturas: simular el paso del tiempo y avisos de otros usuarios (no inventan precios)
Object.assign(window.proto, {
  openPlan, openOffers, openReport,
  ageDays(id, days) { const p = placeById(id); const back = x => (x == null ? x : x - days * DAY); p.items.forEach(i => (i.at = back(i.at))); p.rangeAt = back(p.rangeAt); refreshPlace(p); },
  simReports(id, idx, n) { const p = placeById(id); for (let k = 0; k < n; k++) addFlag(p, idx, 1, 'changed'); refreshPlace(p); },
});
