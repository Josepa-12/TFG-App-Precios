// Uso: node capturas.js <index.html> <carpeta-salida>
// Recorre todo el prototipo con Microsoft Edge y guarda una captura de cada pantalla.
// Empieza siempre con el almacenamiento local vacío (la app guarda sesión, precios y negocios en localStorage).
const { chromium } = require('playwright');
const { pathToFileURL } = require('url');
const path = require('path');

(async () => {
  const [, , src, outDir] = process.argv;
  const url = pathToFileURL(src).href;
  const browser = await chromium.launch({ channel: 'msedge', args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] });
  const ctx = await browser.newContext({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, colorScheme: 'light' });
  const page = await ctx.newPage();
  const errors = [];
  const watch = pg => {
    pg.on('pageerror', e => errors.push('pageerror: ' + e.message));
    pg.on('console', m => { if (m.type() === 'error') errors.push('console: ' + m.text()); });
  };
  watch(page);

  // La app ignora durante ~350 ms los clics repetidos en el mismo sitio tras cambiar de paso (evita dobles toques):
  // cada clic espera un poco antes de seguir.
  const click = async (sel, opt) => { await page.click(sel, opt); await page.waitForTimeout(400); };
  const shot = async (name, wait = 600, pg = page) => {
    await pg.waitForTimeout(wait);
    await pg.screenshot({ path: path.join(outDir, name + '.png') });
    console.log('ok', name);
  };
  // Espera a que se oculte el aviso flotante (toast) para que no tape la pantalla que se captura
  const noToast = async (pg = page) => {
    await pg.waitForFunction(() => !document.querySelector('#toast.show'), null, { timeout: 8000 }).catch(() => {});
    await pg.waitForTimeout(350);
  };
  const login = async (email) => { // pantalla de cuenta ya abierta: Google simulado
    await click('[data-a="google"]');
    if (email) await page.fill('#accEmail', email);
    await click('[data-a="loginOk"]');
  };
  const arrive = async id => {
    await page.evaluate(async id => { proto.openPlace(id); await proto.startNav(); }, id); // la ruta se pide al servidor
    await page.waitForTimeout(500);
    await page.evaluate(() => proto.simulateArrival(true));
    await page.waitForSelector('#navFinish:not([hidden])');
    await click('#navFinish');
    await page.waitForSelector('#flowOverlay:not([hidden])');
  };
  const closeSheet = () => click('#placeSheet [data-act="close"]');

  await page.goto(url, { waitUntil: 'load' });
  await page.evaluate(() => localStorage.clear());
  await page.reload({ waitUntil: 'load' });
  await page.waitForFunction(() => window.__mapReady === true, null, { timeout: 30000 });
  await page.waitForTimeout(2500);
  await shot('01-bienvenida');
  await page.evaluate(() => { window.__askDelayMs = 3500; }); // la pregunta real tarda 1 min; aquí se acorta
  await click('#onbStart');
  // Cuenta: cliente o negocio, con Google (simulado)
  await shot('01b-crear-cuenta', 500);
  await click('[data-a="google"]');
  await shot('01c-google-simulado', 300);
  await click('[data-a="loginOk"]');
  await noToast();
  await shot('02-mapa-claro', 1000); // pantalla limpia: sin marcadores de precio
  await click('#soundFab');
  await shot('02b-app-silenciada', 300);
  await click('#soundFab');
  // De cerca: puntos de interés reales con iconos integrados en el mapa
  await page.evaluate(() => map.jumpTo({ center: [-6.3528, 36.7779], zoom: 16.2 }));
  await shot('02c-mapa-puntos-de-interes', 2500);
  await page.evaluate(() => map.jumpTo({ center: [-6.3515, 36.7790], zoom: 14.9 }));
  await page.waitForTimeout(800);

  await click('#catBtn');
  await shot('03-categorias', 400);
  await click('#catMenu [data-cat="hosteleria"]');
  await shot('04-filtro-hosteleria', 500);
  await click('#catBtn'); await click('#catMenu [data-cat="ninguno"]');

  await click('.dock .search'); // con el buscador cerrado, el toque lo recibe la etiqueta
  await shot('05-buscador', 500);
  await page.fill('#searchInput', 'fis');
  await shot('06-buscador-resultados', 300);
  await page.fill('#searchInput', 'fisio');
  await shot('49-buscar-por-servicio', 400); // busca por servicio: clínicas de fisioterapia
  await click('#searchCancel');
  await page.waitForTimeout(300);

  // Arrastrar el buscador a media altura, parar y soltar: se queda ahí y se puede buscar
  const db = await page.locator('#dock').boundingBox();
  const dx = db.x + 120, dy0 = db.y + 40;
  await page.mouse.move(dx, dy0); await page.mouse.down();
  for (let i = 1; i <= 30; i++) { await page.mouse.move(dx, dy0 - i * 12); await page.waitForTimeout(20); }
  await page.waitForTimeout(200);
  await page.mouse.up();
  await page.waitForTimeout(500);
  await click('#searchInput');
  await page.fill('#searchInput', 'bar');
  await shot('06b-buscador-media-altura', 400);
  await click('#searchCancel');
  await page.waitForTimeout(300);

  // "Encuentra cerca" → Supermercados → ficha de un lugar del mapa (sin precios)
  await click('.dock .search');
  await page.evaluate(() => document.querySelector('#searchPanel').scrollTo(0, 260));
  await shot('06c-encuentra-cerca', 500);
  await click('[data-near="super"]');
  await shot('06d-supermercados-cerca', 2500);
  await click('#searchPanel [data-key]');
  await shot('06e-ficha-lugar-sin-precios', 1800);
  await closeSheet();

  // Cualquier dirección (Photon) y ubicación compartida por WhatsApp
  await click('.dock .search');
  await page.fill('#searchInput', 'Calle Ancha 10 Sanlúcar');
  await page.waitForFunction(() => document.querySelector('#addrResults [data-key]') || /conexión|ninguna/.test(document.querySelector('#addrResults')?.textContent || ''), null, { timeout: 15000 });
  await shot('06f-buscar-direccion', 300);
  await page.fill('#searchInput', 'https://maps.google.com/maps?q=36.7712,-6.3460');
  await shot('06g-enlace-whatsapp', 400);
  await click('#searchPanel [data-key^="shared"]');
  await shot('06h-ficha-ubicacion-compartida', 2500);
  await closeSheet();

  // Fichas de lugares reales (empiezan sin precios)
  await page.evaluate(() => proto.openPlace('casa-ballen'));
  await shot('07-ficha-hosteleria-sin-precio', 1400);
  await closeSheet();
  await page.evaluate(() => proto.openPlace('padel-la-jara'));
  await shot('08-ficha-ocio-sin-precio', 1400);
  await closeSheet();
  await page.evaluate(() => proto.openPlace('iredan-peluqueria-unisex'));
  await shot('09-ficha-servicio-sin-precio', 1400);
  // "En coche" / "A pie" e "Ir" van en una fila bajo la cabecera de la ficha
  await shot('09b-ficha-coche-por-defecto', 300);
  await click('#placeSheet [data-mode="pie"]');
  await shot('09c-ficha-a-pie', 300);

  // A pie: círculo con cono (sin brújula en el ordenador, el cono sigue la ruta)
  await click('#placeSheet [data-act="go"]');
  await page.waitForSelector('#navSimulate:not([disabled])', { timeout: 15000 }); // ruta calculada
  await shot('10b-a-pie-cono', 1400);
  await click('#navExit');
  await page.waitForTimeout(800);

  // En coche (por defecto): flecha. Navegación con la animación hasta la peluquería
  await page.evaluate(() => proto.openPlace('iredan-peluqueria-unisex'));
  await page.waitForTimeout(800);
  await click('#placeSheet [data-mode="coche"]');
  await click('#placeSheet [data-act="go"]');
  await page.waitForSelector('#navSimulate:not([disabled])', { timeout: 15000 });
  await shot('10-navegacion-coche-flecha', 1400);
  await page.click('#navSimulate');
  await page.waitForTimeout(1500);
  await shot('10c-coche-flecha-girando', 0);
  await page.waitForFunction(() => document.querySelector('#navDist').textContent === 'Has llegado', null, { timeout: 30000 });
  await shot('11a-has-llegado-ruta-sigue', 300); // al llegar solo queda "Terminar ruta"
  await click('#navFinish');
  await page.waitForSelector('#flowOverlay:not([hidden])', { timeout: 15000 });
  await shot('11-pregunta-tras-espera', 500);

  // "Más tarde" → queda pendiente en el botón €
  await click('#flowCard [data-flow="later"]');
  await shot('12-pendiente-en-boton', 1500);
  await noToast();

  // Con dos pendientes, el botón € deja elegir; se aporta el PRIMER precio del sitio
  await click('#priceFab');
  await shot('13-pendientes-elegir', 500);
  await click('[data-pend="iredan-peluqueria-unisex"]');
  await click('#flowCard [data-flow="yes"]');
  await shot('14-que-usaste', 400);
  await click('[data-item="0"]');
  await page.evaluate(() => { const r = document.querySelector('#ruler'); r.scrollLeft += 12 * -1; });
  await shot('15-regla-primer-precio', 500);
  await click('[data-flow="sendPrice"]');
  await shot('16-recompensa-pionero', 700);
  await click('[data-flow="done"]');
  await page.waitForTimeout(400);

  // Modo negocio: el dueño declara sus precios; la ficha muestra el origen del dato
  await page.evaluate(() => proto.openOwner('iredan-peluqueria-unisex'));
  await page.fill('input[name="i0"]', '12'); await page.fill('input[name="i1"]', '18');
  await shot('16b-modo-negocio', 500);
  await click('[data-flow="ownerSave"]');
  await page.evaluate(() => proto.openPlace('iredan-peluqueria-unisex'));
  await shot('16c-ficha-declarado-por-negocio', 1400);
  await click('#placeSheet [data-act="alert"]'); // vigilar su precio (para "Sitios vigilados")
  await closeSheet();
  await page.waitForTimeout(400);

  // Hostelería: primer gasto por persona y valoración
  await arrive('casa-ballen');
  await click('#flowCard [data-flow="yes"]');
  await shot('17-hosteleria-gasto', 400);
  await click('[data-bucket="2"]');
  for (const [c, s] of [[0, 5], [1, 4], [2, 5]]) await click(`[data-crit="${c}"][data-star="${s}"]`);
  await shot('18-valoracion', 300);
  await click('[data-flow="sendRate"]');
  await click('[data-flow="done"]');
  await page.waitForTimeout(800);

  // Paso del flujo con "‹ Atrás" y "Más tarde": confirmar un precio publicado (clínica con precios de su web)
  await page.evaluate(() => proto.startFlow('clinica-areas'));
  await click('#flowCard [data-flow="yes"]');
  await click('[data-item="0"]');
  await shot('45-flujo-atras-mas-tarde', 300);
  await click('#flowCard [data-flow="back"]'); // vuelve a "¿Qué servicio pagaste?"
  await click('#flowCard [data-flow="later"]'); // queda pendiente, no se registra nada
  await noToast();

  // Perfil, tienda y ajustes
  await click('#avatarBtn');
  await shot('19-perfil', 500);
  await click('[data-page="reputacion"]');
  await shot('47-reputacion', 400);
  await click('[data-back="main"]');
  await click('[data-page="tienda"]');
  await click('[data-buy="ruta-discont"]');
  await shot('20-tienda', 500);
  await noToast();
  await click('[data-back="main"]');
  await click('[data-page="apariencia"]');
  await click('[data-seg="theme"][data-v="dark"]');
  await shot('21-ajustes-apariencia-oscuro', 600);
  await click('[data-back="main"]');
  await page.evaluate(() => document.querySelector('#screen-perfil').scrollTo(0, 9999));
  await shot('22-perfil-ajustes-oscuro', 400);
  await click('[data-page="avisos"]'); // "Sitios vigilados"
  await click('[data-act="demoAlert"]');
  await shot('23-aviso-cambio-precio', 1500);
  await page.waitForTimeout(800);
  await page.click('#notif').catch(() => {});
  await page.waitForTimeout(400);
  await page.click('#placeSheet [data-act="close"]', { timeout: 3000 }).catch(() => {});
  await shot('24-mapa-oscuro', 2000);

  // Clínica privada con los precios publicados en su web y centro público solo con su ubicación
  await page.evaluate(() => proto.setTheme('light'));
  await page.evaluate(() => proto.openPlace('clinica-areas'));
  await shot('25-clinica-precios-web', 1600);
  await page.evaluate(() => document.querySelector('#placeSheet').scrollTo(0, 9999));
  await shot('25b-clinica-origen-web', 300);
  await closeSheet();
  await page.evaluate(() => map.jumpTo({ center: [-6.3601, 36.7680], zoom: 16 }));
  await page.waitForTimeout(2500);
  await page.evaluate(() => { const s = poisNear(map, VSRC, 'salud', 200).find(x => x.pub); if (s) openDest(s); });
  await shot('26-centro-publico-sin-precio', 1500);
  await closeSheet();

  // Cuenta de negocio: vincular el negocio, revisar los precios y darlo de alta a mano si no aparece
  await page.evaluate(() => proto.logout());
  await noToast();
  await click('[data-type="negocio"]');
  await shot('27-cuenta-negocio', 300);
  await login();
  await page.fill('#bizQ', 'areas');
  await shot('28-buscar-mi-negocio', 400);
  await click('[data-pick="clinica-areas"]');
  await shot('29-verificar-negocio', 300);
  await click('[data-a="verifyOk"]');
  await noToast();
  await shot('30-negocio-revisa-precios-web', 0);
  await click('[data-flow="ownerSave"]');
  await page.waitForTimeout(1000);
  await closeSheet();
  await page.evaluate(() => proto.openProfile('main'));
  await noToast();
  await shot('31-perfil-negocio', 0);
  // Otro negocio (otro correo) que no aparece en el mapa: alta a mano con tipo obligatorio
  await page.evaluate(() => proto.logout());
  await noToast();
  await click('[data-type="negocio"]');
  await login('fisio.bajodeguia@gmail.com');
  await click('[data-a="new"]');
  await page.fill('input[name="name"]', 'Fisioterapia Bajo de Guía');
  await page.selectOption('select[name="cat"]', 'Fisioterapia');
  await page.fill('#newAddr', 'Calle Banda de la Playa 20');
  await click('[data-a="addrSearch"]');
  await page.waitForSelector('#addrList [data-addr="0"]', { timeout: 15000 }).catch(() => {});
  await shot('32-alta-manual-negocio', 300);
  await click('[data-a="create"]');
  await page.fill('input[name="i0"]', '30'); await page.fill('input[name="i2"]', '35');
  await click('[data-flow="ownerSave"]');
  await shot('33-negocio-nuevo-en-el-mapa', 1600);

  // Ofertas: el negocio publica una con fecha de fin; se ve en la ficha y con una etiqueta mínima en el marcador
  await page.evaluate(() => { window.__askDelayMs = 99999999; setFilter('servicio'); }); // que no salte la pregunta de llegada
  await page.evaluate(() => proto.openOffers(proto.S.account.business));
  await page.fill('#offerForm [name="text"]', 'Primera sesión + valoración');
  await page.fill('#offerForm [name="price"]', '45');
  await shot('34-negocio-publica-oferta', 300);
  await click('[data-flow="offerAdd"]');
  await click('#flowCard [data-flow="close"]');
  await page.evaluate(() => proto.openPlace(proto.S.account.business));
  await shot('35-oferta-en-ficha-y-marcador', 1500);
  await closeSheet();

  // Cliente: frescura del precio (se simula el paso de 200 días), aviso de precio incorrecto y revisión
  await page.evaluate(() => proto.logout());
  await noToast();
  await login();
  await noToast();
  await page.evaluate(() => proto.ageDays('clinica-areas', 200));
  await page.evaluate(() => proto.openPlace('clinica-areas'));
  await page.waitForTimeout(1500);
  await page.evaluate(() => { const el = document.querySelector('#placeSheet .freshness'); document.querySelector('#placeSheet').scrollTo(0, el.offsetTop - 330); });
  await shot('36-precio-antiguo', 300);
  await click('#placeSheet [data-act="report"]');
  await click('[data-ritem="0"]');
  await shot('37-avisar-precio-incorrecto', 300);
  await click('[data-reason="changed"]');
  await page.evaluate(() => proto.simReports('clinica-areas', 0, 2)); // avisos de otros dos usuarios
  await page.evaluate(() => document.querySelector('#placeSheet').scrollTo(0, 200));
  await shot('38-precio-en-revision', 2600);
  await closeSheet();

  // El negocio ve los avisos de error de sus clientes (vuelve a entrar y recupera su negocio)
  await page.evaluate(() => proto.logout());
  await noToast();
  await click('[data-type="negocio"]');
  await login();
  await page.evaluate(() => proto.openProfile('bizAvisos'));
  await noToast();
  await shot('48-negocio-avisos', 0);
  await page.evaluate(() => proto.logout());
  await noToast();
  await click('[data-type="cliente"]');
  await login();
  await noToast();

  // El cliente siguiente confirma el precio en revisión
  await page.evaluate(() => proto.openPlace('clinica-areas'));
  await page.waitForTimeout(800);
  await page.evaluate(() => proto.startFlow('clinica-areas'));
  await click('#flowCard [data-flow="yes"]'); await click('[data-item="0"]');
  await shot('39-confirmar-precio-en-revision', 300);
  await click('[data-flow="same"]');
  await shot('40-recompensa-precio-antiguo', 600);
  await click('[data-flow="done"]');
  await noToast();

  // Plan de tarde: paradas desde la ficha, gasto estimado y ruta parada a parada
  for (const id of ['clinica-areas', 'chemabarber', 'padel-la-jara']) {
    await page.evaluate(id => proto.openPlace(id), id);
    await page.waitForTimeout(700);
    await click('#placeSheet [data-act="plan"]');
    if (id === 'clinica-areas') { await page.evaluate(() => document.querySelector('#placeSheet').scrollTo(0, 9999)); await shot('41-anadir-al-plan', 300); }
    await closeSheet();
  }
  await noToast();
  await click('#planChip');
  await page.selectOption('.plan-item[data-stop="0"]', '4');
  await shot('42-plan-de-tarde', 400);
  await click('[data-flow="planGo"]');
  await page.waitForSelector('#navSimulate:not([disabled])', { timeout: 20000 });
  await noToast();
  await shot('43-plan-parada-1', 800);
  await page.evaluate(() => proto.simulateArrival(true));
  await page.waitForTimeout(800);
  await click('#navFinish');
  await shot('44-plan-siguiente-parada', 2200);

  // Otras pantallas: sesión nueva (almacenamiento vacío), entrando sin cuenta
  const fresh = async (viewport, deviceScaleFactor) => {
    const c = await browser.newContext({ viewport, deviceScaleFactor, colorScheme: 'light' });
    const pg = await c.newPage();
    watch(pg);
    await pg.goto(url, { waitUntil: 'load' });
    await pg.evaluate(() => localStorage.clear());
    await pg.reload({ waitUntil: 'load' });
    await pg.waitForFunction(() => window.__mapReady === true, null, { timeout: 30000 });
    await pg.waitForTimeout(1500);
    await pg.click('#onbStart'); await pg.waitForTimeout(400);
    await pg.click('[data-a="skip"]');
    await noToast(pg);
    return pg;
  };
  // Móvil pequeño (360×740): en una ficha larga, "En coche / A pie" e "Ir" se ven arriba sin desplazarse
  const sp = await fresh({ width: 360, height: 740 }, 2);
  await sp.evaluate(() => proto.openPlace('clinica-areas'));
  await shot('46-ficha-ir-arriba', 1600, sp);
  // Escritorio (1366×768): el prototipo se muestra dentro del marco del teléfono
  const dp = await fresh({ width: 1366, height: 768 }, 1);
  await shot('50-escritorio-1366x768', 1000, dp);

  console.log(errors.length ? 'ERRORES:\n' + errors.join('\n') : 'Sin errores en consola');
  await browser.close();
})();
