// Uso: node capturas.js <index.html> <carpeta-salida>
// Recorre todo el prototipo con Microsoft Edge y guarda una captura de cada pantalla.
const { chromium } = require('playwright');
const { pathToFileURL } = require('url');
const path = require('path');

(async () => {
  const [, , src, outDir] = process.argv;
  const browser = await chromium.launch({ channel: 'msedge', args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] });
  const ctx = await browser.newContext({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, colorScheme: 'light' });
  const page = await ctx.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push('pageerror: ' + e.message));
  page.on('console', m => { if (m.type() === 'error') errors.push('console: ' + m.text()); });

  const shot = async (name, wait = 600) => {
    await page.waitForTimeout(wait);
    await page.screenshot({ path: path.join(outDir, name + '.png') });
    console.log('ok', name);
  };
  const arrive = async id => {
    await page.evaluate(id => { proto.openPlace(id); proto.startNav(); }, id);
    await page.waitForTimeout(1000);
    await page.evaluate(() => proto.simulateArrival(true));
    await page.waitForSelector('#navFinish:not([hidden])');
    await page.click('#navFinish');
    await page.waitForSelector('#flowOverlay:not([hidden])');
  };

  await page.goto(pathToFileURL(src).href, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__mapReady === true, null, { timeout: 30000 });
  await page.waitForTimeout(2500);
  await shot('01-bienvenida');
  await page.evaluate(() => { window.__askDelayMs = 3500; }); // la pregunta real tarda 1 min; aquí se acorta
  await page.click('#onbStart');
  await shot('02-mapa-claro', 1200);
  await page.click('#soundFab');
  await shot('02b-app-silenciada', 300);
  await page.click('#soundFab');

  await page.click('#catBtn');
  await shot('03-categorias', 400);
  await page.click('#catMenu [data-cat="hosteleria"]');
  await shot('04-filtro-hosteleria', 500);
  await page.click('#catBtn'); await page.click('#catMenu [data-cat="todos"]');

  await page.click('.dock .search'); // con el buscador cerrado, el toque lo recibe la etiqueta
  await shot('05-buscador', 500);
  await page.fill('#searchInput', 'fis');
  await shot('06-buscador-resultados', 300);
  await page.click('#searchCancel');
  await page.waitForTimeout(500);

  // Arrastrar el buscador a media altura, parar y soltar: se queda ahí y se puede buscar
  const db = await page.locator('#dock').boundingBox();
  const dx = db.x + 120, dy0 = db.y + 40;
  await page.mouse.move(dx, dy0); await page.mouse.down();
  for (let i = 1; i <= 30; i++) { await page.mouse.move(dx, dy0 - i * 12); await page.waitForTimeout(20); }
  await page.waitForTimeout(200);
  await page.mouse.up();
  await page.waitForTimeout(500);
  await page.click('#searchInput');
  await page.fill('#searchInput', 'bar');
  await shot('06b-buscador-media-altura', 400);
  await page.click('#searchCancel');
  await page.waitForTimeout(500);

  await page.click('.pin[data-place="marea"]');
  await shot('07-ficha-hosteleria', 1400);
  await page.click('[data-act="close"]');
  await page.evaluate(() => proto.openPlace('marisma'));
  await shot('08-ficha-ocio', 1400);
  await page.click('[data-act="close"]');
  await page.evaluate(() => proto.openPlace('psico'));
  await shot('09-ficha-servicio', 1400);
  await page.evaluate(() => document.querySelector('#placeSheet').scrollTo(0, 9999));
  await shot('09b-ficha-coche-por-defecto', 300);
  await page.click('[data-mode="pie"]');
  await page.evaluate(() => document.querySelector('#placeSheet').scrollTo(0, 9999));
  await shot('09c-ficha-a-pie', 300);

  // A pie: círculo con cono (sin brújula en el ordenador, el cono sigue la ruta)
  await page.click('[data-act="go"]');
  await shot('10b-a-pie-cono', 1400);
  await page.click('#navExit');
  await page.waitForTimeout(1000);

  // En coche (por defecto): flecha. Navegación real con la animación hasta Psicología
  await page.evaluate(() => proto.openPlace('psico'));
  await page.waitForTimeout(800);
  await page.click('[data-act="go"]');
  await shot('10-navegacion-coche-flecha', 1400);
  await page.click('#navSimulate');
  await page.waitForTimeout(1500);
  await shot('10c-coche-flecha-girando', 0);
  await page.waitForFunction(() => document.querySelector('#navDist').textContent === 'Has llegado');
  await shot('11a-has-llegado-ruta-sigue', 300);
  await page.click('#navFinish');
  await page.waitForSelector('#flowOverlay:not([hidden])', { timeout: 15000 });
  await shot('11-pregunta-tras-espera', 500);

  // "Más tarde" → queda pendiente en el botón €
  await page.click('[data-flow="later"]');
  await shot('12-pendiente-en-boton', 1500);

  // Con dos pendientes, el botón € deja elegir
  await page.click('#priceFab');
  await shot('13-pendientes-elegir', 500);
  await page.click('[data-pend="psico"]');
  await page.click('[data-flow="yes"]');
  await shot('14-confirmar-precio', 400);
  await page.click('[data-flow="diff"]');
  await page.waitForTimeout(300);
  await page.evaluate(() => { const r = document.querySelector('#ruler'); r.scrollLeft += 12 * 7; });
  await shot('15-regla-precio', 500);
  await page.click('[data-flow="sendPrice"]');
  await shot('16-recompensa', 700);
  await page.click('[data-flow="done"]');
  await page.waitForTimeout(1000);

  // Hostelería: tramos de gasto y valoración
  await arrive('marea');
  await page.click('[data-flow="yes"]');
  await shot('17-hosteleria-gasto', 400);
  await page.click('[data-bucket="25"]');
  for (const [c, s] of [[0, 5], [1, 4], [2, 5]]) await page.click(`[data-crit="${c}"][data-star="${s}"]`);
  await shot('18-valoracion', 300);
  await page.click('[data-flow="sendRate"]');
  await page.click('[data-flow="done"]');
  await page.waitForTimeout(1000);

  // Perfil, tienda y ajustes
  await page.click('#avatarBtn');
  await shot('19-perfil', 500);
  await page.click('[data-page="tienda"]');
  await page.click('[data-buy="ruta-discont"]');
  await shot('20-tienda', 500);
  await page.click('[data-back="main"]');
  await page.click('[data-page="apariencia"]');
  await page.click('[data-seg="theme"][data-v="dark"]');
  await shot('21-ajustes-apariencia-oscuro', 600);
  await page.click('[data-back="main"]');
  await page.evaluate(() => document.querySelector('#screen-perfil').scrollTo(0, 9999));
  await shot('22-perfil-ajustes-oscuro', 400);
  await page.click('[data-page="avisos"]');
  await page.click('[data-act="demoAlert"]');
  await shot('23-aviso-cambio-precio', 1500);
  await page.waitForTimeout(800);
  await page.click('#notif').catch(() => {});
  await page.waitForTimeout(400);
  await page.click('[data-act="close"]').catch(() => {});
  await shot('24-mapa-oscuro', 2000);

  console.log(errors.length ? 'ERRORES:\n' + errors.join('\n') : 'Sin errores en consola');
  await browser.close();
})();
