// Uso: node shot.js <ruta-html> <ruta-png> [ancho] [alto] [fullPage]
const { chromium } = require('playwright');
const { pathToFileURL } = require('url');
(async () => {
  const [, , src, out, w = '1400', h = '900', full = 'true'] = process.argv;
  const browser = await chromium.launch({ channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: +w, height: +h }, deviceScaleFactor: 1 });
  await page.goto(pathToFileURL(src).href, { waitUntil: 'networkidle' });
  await page.waitForTimeout(300);
  await page.screenshot({ path: out, fullPage: full === 'true' });
  await browser.close();
})();
