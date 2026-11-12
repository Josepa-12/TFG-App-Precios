// Uso: node capture.js <index.html> <carpeta-salida>
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
  const idle = async () => {
    await page.waitForFunction(() => window.__mapReady === true, null, { timeout: 30000 });
    await page.waitForTimeout(2500);
  };

  await page.goto(pathToFileURL(src).href, { waitUntil: 'load' });
  await idle();
  await shot('01-bienvenida');
  await page.click('#onbStart');
  await shot('02-mapa-claro', 1200);

  await page.click('#searchInput');
  await page.fill('#searchInput', 'fis');
  await shot('03-busqueda', 400);
  await page.keyboard.press('Escape');
  await page.click('.chip[data-filter="todos"]');

  await page.click('.pin[data-place="marea"]');
  await shot('04-ficha-hosteleria', 1500);
  await page.click('[data-act="close"]');

  await page.evaluate(() => proto.openPlace('fisio'));
  await shot('05-ficha-servicio', 1500);
  await page.click('[data-act="close"]');

  await page.evaluate(() => proto.openPlace('marisma'));
  await shot('06-ficha-ocio', 1500);

  await page.click('[data-act="go"]');
  await shot('07-navegacion', 2500);
  await page.click('#navSimulate');
  await page.waitForSelector('#flowOverlay:not([hidden])', { timeout: 15000 });
  await shot('08-llegada', 800);
  await page.click('[data-flow="yes"]');
  await shot('09-que-usaste', 400);
  await page.click('[data-item="0"]');
  await shot('10-confirmar-precio', 400);
  await page.click('[data-flow="diff"]');
  await page.click('[data-step="1"]');
  await page.click('[data-step="1"]');
  await shot('11-otro-precio', 400);
  await page.click('[data-flow="sendPrice"]');
  for (const [c, s] of [[0, 4], [1, 5], [2, 4]]) await page.click(`[data-crit="${c}"][data-star="${s}"]`);
  await shot('12-valoracion', 400);
  await page.click('[data-flow="sendRate"]');
  await shot('13-recompensa', 1200);
  await page.click('[data-flow="shop"]');
  await shot('14-tienda', 600);
  await page.click('[data-buy="flecha-dorada"]');
  await shot('15-tienda-comprado', 600);
  await page.click('.tab[data-go="perfil"]');
  await shot('16-perfil', 600);
  await page.click('[data-theme-pref="dark"]');
  await shot('17-perfil-oscuro', 600);
  await page.click('.tab[data-go="map"]');
  await shot('18-mapa-oscuro', 2500);
  await page.click('.pin[data-place="marea"]');
  await shot('19-ficha-hosteleria-oscuro', 1500);
  await page.click('[data-act="close"]');
  await page.evaluate(() => proto.openPlace('psico'));
  await page.click('[data-act="go"]');
  await page.waitForTimeout(1500);
  await page.click('#navSimulate');
  await page.waitForSelector('#flowOverlay:not([hidden])', { timeout: 15000 });
  await page.click('[data-flow="yes"]');
  await shot('20-confirmar-anonimo-oscuro', 600);

  console.log(errors.length ? 'ERRORES:\n' + errors.join('\n') : 'Sin errores de consola');
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });

