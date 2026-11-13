// Uso: node generar-lugares.js <prototipo/index.html> <prototipo/datos-lugares.js>
// Genera el archivo de lugares REALES de Sanlúcar (OpenStreetMap) que pueden tener precios.
const { chromium } = require('playwright');
const { pathToFileURL } = require('url');
const fs = require('fs');
(async () => {
  const [, , src, out] = process.argv;
  const browser = await chromium.launch({ channel: 'msedge', args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] });
  const page = await (await browser.newContext({ viewport: { width: 1200, height: 1000 } })).newPage();
  await page.goto(pathToFileURL(src).href);
  await page.waitForFunction(() => window.__mapReady === true, null, { timeout: 30000 });
  const feats = await page.evaluate(async () => {
    const all = [];
    const centers = [];
    for (let x = -6.375; x <= -6.320; x += 0.011) for (let y = 36.755; y <= 36.795; y += 0.008) centers.push([x, y]);
    const s = Object.keys(map.getStyle().sources).find(k => map.getStyle().sources[k].type === 'vector');
    for (const c of centers) {
      map.jumpTo({ center: c, zoom: 15, pitch: 0 });
      await new Promise(r => map.once('idle', r));
      for (const f of map.querySourceFeatures(s, { sourceLayer: 'poi' })) {
        if (f.geometry.type !== 'Point') continue;
        const p = f.properties;
        all.push({ name: p.name, cls: p.class, sub: p.subclass, pos: f.geometry.coordinates.map(v => +v.toFixed(6)) });
      }
    }
    return all;
  });
  await browser.close();

  // Plantilla y tipo de negocio según la subclase de OSM
  const T = {
    restaurant: ['hosteleria', 'Restaurante'], fast_food: ['hosteleria', 'Comida rápida'], food_court: ['hosteleria', 'Taberna'],
    cafe: ['hosteleria', 'Cafetería'], bar: ['hosteleria', 'Bar'], pub: ['hosteleria', 'Pub'], nightclub: ['hosteleria', 'Discoteca'], ice_cream: ['hosteleria', 'Heladería'],
    sports_centre: ['ocio', 'Centro deportivo'], soccer: ['ocio', 'Campo de fútbol'], padel: ['ocio', 'Pista de pádel'], tennis: ['ocio', 'Pista de tenis'],
    stadium: ['ocio', 'Recinto deportivo'], swimming_pool: ['ocio', 'Piscina'], golf_course: ['ocio', 'Club de golf'], sailing: ['ocio', 'Club náutico'],
    cinema: ['ocio', 'Cine'], theatre: ['ocio', 'Teatro'], museum: ['ocio', 'Museo'], attraction: ['ocio', 'Visita turística'], castle: ['ocio', 'Monumento'],
    parking: ['ocio', 'Aparcamiento'],
    hairdresser: ['servicio', 'Peluquería'], beauty: ['servicio', 'Centro de belleza'], dentist: ['servicio', 'Clínica dental'], doctors: ['servicio', 'Clínica médica'],
    veterinary: ['servicio', 'Veterinario'], car_repair: ['servicio', 'Taller'], laundry: ['servicio', 'Lavandería'], driving_school: ['servicio', 'Autoescuela'], car_wash: ['servicio', 'Lavado de coches'],
  };
  const PUBLIC_HEALTH = /consultorio|centro de salud|urgencias|hospital/i; // sanidad pública: sin precio
  const R = 6371000, toR = d => d * Math.PI / 180;
  const dist = (a, b) => { const dLat = toR(b[1] - a[1]), dLon = toR(b[0] - a[0]); const h = Math.sin(dLat / 2) ** 2 + Math.cos(toR(a[1])) * Math.cos(toR(b[1])) * Math.sin(dLon / 2) ** 2; return 2 * R * Math.asin(Math.sqrt(h)); };
  const slug = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
  const res = [];
  for (const f of feats) {
    let t = T[f.sub]; if (!t || !f.name || PUBLIC_HEALTH.test(f.name)) continue;
    if (res.some(r => r.name.toLowerCase() === f.name.toLowerCase() && dist(r.pos, f.pos) < 80)) continue; // repetidos entre teselas o categorías
    // Afinar el tipo por el nombre cuando OSM es demasiado genérico (p. ej. un club de pádel marcado como centro deportivo)
    if (/p[aá]del/i.test(f.name)) t = ['ocio', 'Pista de pádel']; else if (/piscina/i.test(f.name)) t = ['ocio', 'Piscina'];
    let id = slug(f.name); while (res.some(r => r.id === id)) id += '-2';
    res.push({ id, tpl: t[0], cat: t[1], name: f.name, osm: `${f.cls}/${f.sub}`, pos: f.pos });
  }
  res.sort((a, b) => a.tpl.localeCompare(b.tpl) || a.cat.localeCompare(b.cat) || a.name.localeCompare(b.name));
  const counts = res.reduce((o, r) => (o[r.tpl] = (o[r.tpl] || 0) + 1, o), {});
  console.log('Lugares:', res.length, counts);
  const js = `/* Lugares REALES de Sanlúcar de Barrameda que pueden tener precios en PrecioLocal.
 * Generado automáticamente a partir de OpenStreetMap (teselas de OpenFreeMap, capa "poi") el 13/11/2026
 * con prototipo/herramientas/generar-lugares.js. Datos © colaboradores de OpenStreetMap, licencia ODbL.
 * Ningún lugar trae precios: los declara el negocio o los aporta el primer cliente.
 * ${res.length} lugares: ${Object.entries(counts).map(([k, v]) => `${k} ${v}`).join(' · ')}
 */
const REAL_PLACES = [
${res.map(r => '  ' + JSON.stringify(r)).join(',\n')},
];
`;
  fs.writeFileSync(out, js);
})();
