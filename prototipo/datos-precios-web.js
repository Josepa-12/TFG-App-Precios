/* Lugares añadidos a mano y precios iniciales tomados de internet.
 *
 * Criterio (13/11/2026): solo se usan precios PUBLICADOS POR EL PROPIO NEGOCIO en su web oficial,
 * con la página y la fecha de consulta. No se usan precios de agregadores (Doctoralia, Saludonnet,
 * Masquemedicos…), porque suelen estar desfasados o ligados a una aseguradora concreta.
 * Si el negocio no publica tarifas, el sitio empieza sin precio: lo declara el negocio desde su
 * cuenta o lo aporta el primer cliente. Después, los clientes confirman o corrigen el precio.
 * La sanidad pública queda fuera (no se paga en el momento).
 */

// Lugares que no salían en la extracción automática de OpenStreetMap (datos-lugares.js)
const EXTRA_PLACES = [
  {
    id: 'clinica-areas', tpl: 'servicio', cat: 'Clínica privada', name: 'Clínica Areas',
    addr: 'C/ Santo Domingo, 21-23', pos: [-6.35088, 36.780143],
    about: 'Clínica deportiva: traumatología, fisioterapia, suelo pélvico, readaptación, entrenamiento personal y tratamiento de la obesidad.',
  },
  {
    id: 'fisiosanlucar', tpl: 'servicio', cat: 'Fisioterapia', name: 'Clínica FisioSanlúcar',
    addr: 'Av. del Quinto Centenario, 92 (edificio América)', pos: [-6.35918, 36.77846],
    about: 'Fisioterapia y osteopatía.',
  },
];

// Precios publicados por el propio negocio. [nombre, unidad, precio, opciones]
// precio null = "consultar" (el servicio existe pero la web no da el importe).
// desde: true = la web indica "desde X €". nota: aclaración de la web (p. ej. "con bono").
const WEB_PRICES = {
  'clinica-areas': {
    url: 'https://clinicaareas.com/tarifas', fecha: '13/11/2026',
    grupos: {
      'Traumatología': [
        ['Consulta de traumatología', 'sesión', 90],
        ['Tratamiento ecoguiado', 'servicio', 50],
        ['Infiltraciones', 'servicio', null],
        ['Factores de crecimiento', 'servicio', null],
      ],
      'Fisioterapia': [
        ['Sesión de fisioterapia', 'sesión', 40, { desde: true }],
        ['Tratamiento Vossman', 'sesión', 45],
        ['Ondas de choque', 'sesión', 50],
        ['Superinductiva', 'sesión', 15, { desde: true, nota: 'con bono' }],
        ['Recuperación con frío (Aquilo / Phisiolab)', 'sesión', null],
      ],
      'Suelo pélvico': [
        ['Valoración de suelo pélvico', 'sesión', 45],
        ['Sesión de suelo pélvico (incluye Phenix)', 'sesión', 35],
      ],
      'Readaptación y entrenamiento': [
        ['Entrenamiento personal', 'sesión', 22.5, { desde: true, nota: 'con bono' }],
        ['Entrenamiento grupal', 'sesión', 10, { desde: true, nota: 'con bono' }],
        ['Sesión AlterG', 'sesión', 20],
      ],
    },
  },
};
