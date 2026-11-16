/* PrecioLocal — funciones añadidas el 16/11/2026
 *  1. Frescura del precio: cada precio indica cuándo se confirmó por última vez, con un color que depende
 *     del tipo de negocio (la hostelería cambia antes). Confirmar un precio antiguo da monedas extra.
 *  2. Ofertas del negocio: las publica la cuenta de negocio con fecha de caducidad; se ven en la ficha y
 *     con una etiqueta muy pequeña en una esquina del marcador. Los clientes pueden avisar de que ya no está.
 *  3. Plan de tarde: se añaden sitios desde la ficha; el plan calcula el gasto estimado para N personas
 *     y guía la ruta parada a parada.
 *  4. "¿Algún precio está mal?": avisos de los usuarios con peso según su reputación. Con 3 puntos de aviso
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
// Punto de color junto a cada precio
const freshDot = (p, at) => `<i class="fr ${freshState(p, at)}" title="${FRESH_TXT[freshState(p, at)]} · ${at ? 'confirmado ' + ageText(at) : ''}"></i>`;
// Resumen de la ficha: el estado del precio confirmado más reciente
function freshHTML(p) {
  const ats = p.tpl === 'hosteleria' ? [p.rangeAt] : p.items.filter(i => i.p != null).map(i => i.at);
  const at = Math.max(...ats.filter(x => x != null)); if (!isFinite(at)) return '';
  const st = freshState(p, at), [g, s] = FRESH[p.tpl];
  const extra = st === 'fresh' ? '' : ` · confírmalo y gana +${REWARD_STALE[st].c} monedas extra`;
  return `<div class="freshness ${st}"><i class="fr ${st}"></i><span><b>${FRESH_TXT[st]}</b> · confirmado ${ageText(at)}${extra}<small>${p.tpl === 'hosteleria' ? `En hostelería, al día hasta ${g} días y antiguo a partir de ${s}` : `Al día hasta ${g / 30} meses y antiguo a partir de ${s / 30}`}</small></span></div>`;
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
    ${mine || S.reportedOffers.includes(o.id) ? '' : `<button class="offer-gone" data-act="offerGone" data-offer="${o.id}">Ya no está</button>`}</div>`).join('');
}
function updateOfferTag(p) {
  const el = pinOf(p.id); if (!el) return;
  let tag = el.querySelector('.pin-offer');
  if (activeOffers(p).length && !tag) { tag = document.createElement('i'); tag.className = 'pin-offer'; tag.setAttribute('aria-hidden', 'true'); tag.textContent = '%'; el.appendChild(tag); }
  else if (!activeOffers(p).length && tag) tag.remove();
}
function openOffers(id) { S.flow = { id, step: 'offers' }; renderFlow(); }
function renderOffers(card) {
  const p = placeById(S.flow.id), list = activeOffers(p);
  const def = new Date(now() + 7 * DAY).toISOString().slice(0, 10), min = new Date().toISOString().slice(0, 10);
  card.innerHTML = `<div class="flow-step">Modo negocio</div><h2>Ofertas de ${esc(p.name)}</h2>
    <p>Publica una oferta con fecha de fin. Se verá en tu ficha y con una etiqueta en tu marcador. Si varios clientes avisan de que ya no está, se retira.</p>
    ${list.length ? `<div class="options">${list.map(o => `<div class="option offer-own"><span>${esc(o.text)}${o.price != null ? ` · ${fmtEur(o.price)}` : ''}<small>Hasta el ${fmtDay(o.until)}</small></span><button class="btn ghost small" data-del="${o.id}">Quitar</button></div>`).join('')}</div>` : ''}
    <form id="offerForm">
      <label class="acc-field"><span>Oferta</span><input class="text-in" name="text" maxlength="60" placeholder="Por ejemplo: martes, corte + barba" autocomplete="off"></label>
      <div class="acc-row">
        <label class="acc-field" style="flex:1"><span>Precio (opcional)</span><input class="num-in" name="price" type="number" inputmode="decimal" min="0" step="0.5" placeholder="€"></label>
        <label class="acc-field" style="flex:1.3"><span>Válida hasta</span><input class="text-in" name="until" type="date" value="${def}" min="${min}"></label>
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
function togglePlan(d) {
  const i = inPlan(d);
  if (i >= 0) { S.plan.stops.splice(i, 1); if (S.plan.active != null && S.plan.active >= S.plan.stops.length) S.plan.active = null; toast('Quitado del plan'); }
  else {
    const p = d.kind === 'place' ? placeById(d.id) : null;
    const item = p && p.tpl !== 'hosteleria' ? Math.max(0, p.items.findIndex(x => x.p != null)) : null;
    S.plan.stops.push({ d: { ...d }, item }); toast(`Añadido al plan · ${S.plan.stops.length} ${S.plan.stops.length === 1 ? 'parada' : 'paradas'}`);
  }
  updatePlanChip(); renderPlace();
}
// Gasto de una parada para el grupo: [mínimo, máximo] o null si aún no hay precio
function stopCost(s) {
  const p = s.d.kind === 'place' ? placeById(s.d.id) : null, n = S.plan.people;
  if (!p || !priced(p)) return null;
  if (p.tpl === 'hosteleria') return [p.range[0] * n, p.range[1] * n];
  const it = p.items[s.item]; if (!it || it.p == null) return null;
  const k = it.u === 'h' || it.u === 'día' ? 1 : n; // alquiler de pista o aparcamiento: precio total
  return [it.p * k, it.p * k];
}
function planTotal() {
  let lo = 0, hi = 0, missing = 0;
  for (const s of S.plan.stops) { const c = stopCost(s); if (c) { lo += c[0]; hi += c[1]; } else missing++; }
  return { lo, hi, missing };
}
const fmtRange = (lo, hi) => (Math.round(lo) === Math.round(hi) ? fmtEur(Math.round(lo)) : `${Math.round(lo)}–${Math.round(hi)} €`);
function updatePlanChip() {
  const c = $('#planChip'), n = S.plan.stops.length;
  c.hidden = !n || !!S.navigating;
  if (!n) return;
  const t = planTotal();
  c.innerHTML = `${ICON.list}<span>Plan · ${n} ${n === 1 ? 'parada' : 'paradas'}${t.lo || t.hi ? ` · ${fmtRange(t.lo, t.hi)}` : ''}</span>`;
}
function openPlan() { S.flow = { step: 'plan' }; renderFlow(); }
function renderPlan(card) {
  const st = S.plan.stops, t = planTotal();
  let metros = 0, prev = USER_POS; for (const s of st) { metros += distM(prev, s.d.pos) * 1.3; prev = s.d.pos; } // aprox. por calles
  const rows = st.map((s, i) => {
    const p = s.d.kind === 'place' ? placeById(s.d.id) : null, c = stopCost(s);
    const pick = p && p.tpl !== 'hosteleria' && p.items.some(x => x.p != null)
      ? `<select class="plan-item" data-stop="${i}" aria-label="Qué vas a usar en ${esc(p.name)}">${p.items.map((x, k) => x.p == null ? '' : `<option value="${k}" ${k === s.item ? 'selected' : ''}>${esc(x.n)} · ${fmtEur(x.p)}${UNIT[x.u]}</option>`).join('')}</select>` : '';
    const what = !p ? 'Sin precios (dirección o lugar del mapa)' : !priced(p) ? 'Aún sin precio' : p.tpl === 'hosteleria' ? `${p.range[0]}–${p.range[1]} € por persona` : '';
    return `<div class="plan-stop"><span class="plan-n">${i + 1}</span>
      <div class="plan-info"><b>${esc(s.d.name)}</b>${what ? `<small>${what}</small>` : ''}${pick}</div>
      <div class="plan-cost">${c ? fmtRange(c[0], c[1]) : '—'}</div>
      <div class="plan-ops"><button data-up="${i}" aria-label="Subir" ${i ? '' : 'disabled'}>↑</button><button data-down="${i}" aria-label="Bajar" ${i < st.length - 1 ? '' : 'disabled'}>↓</button><button data-rm="${i}" aria-label="Quitar">${ICON.x}</button></div></div>`;
  }).join('');
  card.innerHTML = `<h2>Plan de tarde</h2><p>Tus paradas en orden. El gasto se estima con los precios de la app.</p>
    <div class="plan-people"><span>Personas</span><div class="stepper"><button data-people="-1" aria-label="Una persona menos">−</button><b>${S.plan.people}</b><button data-people="1" aria-label="Una persona más">+</button></div></div>
    ${st.length ? `<div class="plan-list">${rows}</div>` : `<div class="text-card">Añade sitios desde su ficha con "Añadir al plan de tarde".</div>`}
    ${st.length ? `<div class="plan-total"><span>Gasto estimado${S.plan.people > 1 ? ` para ${S.plan.people} personas` : ''}</span><strong>${t.lo || t.hi ? fmtRange(t.lo, t.hi) : '—'}</strong></div>
    <p class="hint">${t.missing ? `${t.missing} ${t.missing === 1 ? 'parada no tiene' : 'paradas no tienen'} precio todavía y no ${t.missing === 1 ? 'cuenta' : 'cuentan'}. ` : ''}Recorrido aproximado: ${fmtDist(metros)}. Las pistas y los aparcamientos cuentan una vez para el grupo.</p>` : ''}
    <div class="flow-actions two"><button class="btn ghost" data-flow="${st.length ? 'planClear' : 'close'}">${st.length ? 'Vaciar' : 'Cerrar'}</button><button class="btn primary" data-flow="planGo" ${st.length ? '' : 'disabled'}>${ICON.nav} Empezar ruta</button></div>`;
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

// ---------- 4. Avisos de precio incorrecto y reputación ----------
const REVIEW_AT = 3; // puntos de aviso para dejar un precio "en revisión"
function reputation() {
  const n = S.contributions;
  if (n >= 20) return { name: 'Experta', w: 2, next: null };
  if (n >= 5) return { name: 'Fiable', w: 1.5, next: 20 - n };
  return { name: 'Nueva', w: 1, next: 5 - n };
}
const fmtW = w => String(w).replace('.', ',');
function reportLinkHTML(p) {
  if (!priced(p) || S.account?.type === 'negocio') return '';
  return `<button class="report-link" data-act="report">${ICON.pending}<span>¿Algún precio está mal? Avísanos</span></button>`;
}
const reviewChip = '<span class="rev">En revisión</span>';
function openReport(id) { S.flow = { id, step: 'report', item: null }; renderFlow(); }
const REASONS = [['changed', 'Ahora cuesta otra cantidad'], ['gone', 'Ya no ofrecen este servicio'], ['closed', 'El negocio ha cerrado']];
function renderReport(card) {
  const f = S.flow, p = placeById(f.id), rep = reputation();
  const single = p.tpl === 'hosteleria';
  if (!single && f.item == null) {
    card.innerHTML = `<div class="flow-step">Avisar de un precio</div><h2>¿Qué precio está mal?</h2><p>Elige el servicio.</p>
      <div class="options">${p.items.map((x, i) => x.p == null || S.reported.includes(`${p.id}:${i}`) ? '' : `<button class="option" data-ritem="${i}"><span>${esc(x.n)}</span><small>${fmtEur(x.p)}${UNIT[x.u]}</small></button>`).join('')}</div>
      <div class="flow-actions"><button class="btn ghost" data-flow="close">Cancelar</button></div>`;
    return;
  }
  const what = single ? `Gasto por persona: ${p.range[0]}–${p.range[1]} €` : `${p.items[f.item].n}: ${fmtEur(p.items[f.item].p)}`;
  card.innerHTML = `<div class="flow-step">Avisar de un precio</div><h2>¿Qué ocurre?</h2><p>${esc(what)}</p>
    <div class="options">${REASONS.map(([k, l]) => `<button class="option" data-reason="${k}"><span>${l}</span></button>`).join('')}</div>
    <div class="note">${ICON.users}<span>Tu reputación: <b>${rep.name}</b>. Tu aviso cuenta ×${fmtW(rep.w)}. Con ${REVIEW_AT} puntos de aviso, el precio queda en revisión y se pide a los siguientes clientes que lo confirmen.</span></div>
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
    if (d.del) { p.offers = p.offers.filter(o => o.id !== d.del); saveBusiness(p); updateOfferTag(p); refreshPlace(p); return renderFlow(); }
    if (d.flow === 'offerAdd') {
      e.preventDefault();
      const fd = new FormData($('#offerForm')), text = String(fd.get('text') || '').trim();
      const price = parseFloat(String(fd.get('price') || '').replace(',', '.')), until = Date.parse(fd.get('until') + 'T23:59:59');
      if (!text) return toast('Escribe la oferta');
      if (!isFinite(until) || until < startOfToday()) return toast('Elige una fecha de fin válida');
      p.offers.push({ id: 'of' + now().toString(36), text, price: isFinite(price) && price >= 0 ? price : null, until, gone: 0 });
      saveBusiness(p); updateOfferTag(p); refreshPlace(p);
      toast('Oferta publicada'); return renderFlow();
    }
    return;
  }
  if (f.step === 'plan') {
    if (d.people) { S.plan.people = Math.max(1, Math.min(20, S.plan.people + +d.people)); updatePlanChip(); return renderFlow(); }
    const mv = (i, j) => { const a = S.plan.stops; [a[i], a[j]] = [a[j], a[i]]; };
    if (d.up) { mv(+d.up, +d.up - 1); return renderFlow(); }
    if (d.down) { mv(+d.down, +d.down + 1); return renderFlow(); }
    if (d.rm) { S.plan.stops.splice(+d.rm, 1); S.plan.active = null; updatePlanChip(); return S.plan.stops.length ? renderFlow() : endFlow(); }
    if (d.flow === 'planClear') { S.plan.stops = []; S.plan.active = null; updatePlanChip(); toast('Plan vaciado'); return endFlow(); }
    if (d.flow === 'planGo') return startPlan();
    return;
  }
  if (f.step === 'report') {
    const p = placeById(f.id);
    if (d.ritem) { f.item = +d.ritem; return renderFlow(); }
    if (d.reason) {
      const idx = p.tpl === 'hosteleria' ? 0 : f.item, rep = reputation();
      S.reported.push(`${p.id}:${idx}`);
      const toReview = addFlag(p, idx, rep.w, d.reason);
      endFlow(); refreshPlace(p);
      return toast(toReview ? 'Gracias: el precio queda en revisión hasta que lo confirmen los clientes' : `Gracias por avisar (×${fmtW(rep.w)}). Con ${REVIEW_AT} puntos, el precio pasa a revisión`);
    }
  }
}
function onExtraAct(a, el) {
  const d = S.dest; if (!d) return;
  if (a === 'plan') return togglePlan(d);
  if (a === 'report') return openReport(S.place);
  if (a === 'offerGone') {
    const p = placeById(S.place), o = p.offers.find(x => x.id === el.dataset.offer); if (!o) return;
    o.gone++; S.reportedOffers.push(o.id);
    if (o.gone >= OFFER_GONE) toast('Oferta retirada: varios clientes dicen que ya no está'); else toast('Gracias: si más clientes lo confirman, la retiramos');
    updateOfferTag(p); renderPlace();
  }
}

// ---------- Inicio ----------
S.plan = { stops: [], people: 2, active: null };
S.reported = []; S.reportedOffers = [];
PLACES.forEach(updateOfferTag); // marcadores creados antes de cargar este archivo
$('#planChip').addEventListener('click', openPlan);
$('#flowCard').addEventListener('change', e => {
  const sel = e.target.closest('.plan-item'); if (!sel) return;
  S.plan.stops[+sel.dataset.stop].item = +sel.value; updatePlanChip(); renderFlow();
});
// Ganchos para las capturas: simular el paso del tiempo y avisos de otros usuarios (no inventan precios)
Object.assign(window.proto, {
  openPlan, openOffers, openReport,
  ageDays(id, days) { const p = placeById(id); const back = x => (x == null ? x : x - days * DAY); p.items.forEach(i => (i.at = back(i.at))); p.rangeAt = back(p.rangeAt); refreshPlace(p); },
  simReports(id, idx, n) { const p = placeById(id); for (let k = 0; k < n; k++) addFlag(p, idx, 1, 'changed'); refreshPlace(p); },
});
