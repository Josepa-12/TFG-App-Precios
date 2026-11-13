# Estudio de tipos de puntos de interés (POI)

> Fecha: 13/11/2026 · Relacionado con [estudio-tipos-negocio.md](estudio-tipos-negocio.md) (modelo de precios) y con el prototipo ([prototipo/lugares.js](../prototipo/lugares.js)).

## 1. Objetivo
Que la app funcione como un GPS completo:
- mostrar en el mapa **todos los comercios y servicios** (supermercados, gimnasios, farmacias…), no solo los que tienen precios;
- cada tipo con su icono integrado en el mapa, como en Mapas de Apple;
- poder ir a cualquiera de ellos.

Este estudio decide:
- qué tipos se muestran y cómo se agrupan;
- cuáles podrían tener precios en el futuro dentro del modelo de tres plantillas.

## 2. Fuente de datos
- **OpenStreetMap**, a través de las teselas vectoriales de **OpenFreeMap**, con el esquema **OpenMapTiles**, capa `poi`.
- Cada punto trae `name`, `class` (tipo general, p. ej. `grocery`), `subclass` (tipo concreto, p. ej. `supermarket`) y `rank` (importancia).
- Son datos **reales y gratuitos**, sin cuenta, y los mismos que ya usa el mapa del prototipo.

### Censo de Sanlúcar de Barrameda (13/11/2026)
Recogido automáticamente con Playwright recorriendo 6 zonas del municipio a zoom 15:
- **481 puntos únicos**;
- **98 combinaciones** de clase y subclase.

| Nº | Clase / subclase | Ejemplos reales |
|---:|---|---|
| 128 | `bus / bus_stop` | Fuente Vieja, Plaza San Roque |
| 37 | `restaurant` | El Espejo Gastrobar, Casa Ballén |
| 28 | `park` | Plaza Castillo de Santiago |
| 23 | `bar` | El Arquillo, Trascuesta |
| 19 | `place_of_worship` | Basílica de la Caridad, Iglesia de la O |
| 16 | `school` | CEIP La Dehesilla, IES Cristóbal Colón |
| 15 | `cafe` | El Esquinazo, Cafetería Los Helechos |
| 14 | `pharmacy` | Farmacia Monteolivete |
| 13 | `grocery / supermarket` | Mercadona, Lidl, Dia, El Jamón |
| 8 | `fuel` | BP, Repsol, Ballenoil |
| 7 | `lodging / hotel` | Hotel Guadalquivir, Palacio Marqués de Arizón |
| 6 | `beer / pub` | La Januela, Bar Rociero |
| 5 | `parking`, `bank`, `shop / convenience`, `art_gallery / artwork` | Aparcamiento Mercadona, Caixabank |
| 4 | `fast_food`, `doctors`, `dentist`, `sports_centre`, `bakery`, `butcher`, `attraction` | Burger King, Padel La Jara, Bodega La Gitana |
| 1–3 | `castle`, `monument`, `cinema`, `hairdresser`, `veterinary`, `laundry`, `golf`, `ferry_terminal`, `charging_station`… | Castillo de Santiago, Multicines Al-Ándalus |

## 3. Categorías del prototipo
Se agrupan las 98 combinaciones en **22 categorías** con color e icono propios. La **subclase manda sobre la clase**: por ejemplo, `fuel` + `charging_station` se clasifica como carga eléctrica y no como gasolinera.

| Categoría | Clases / subclases de OSM | Nº en Sanlúcar | "Encuentra cerca" |
|---|---|---:|:---:|
| Restaurantes | restaurant | 37 | ✓ |
| Comida rápida | fast_food, food_court | 5 | ✓ |
| Cafeterías y dulces | cafe, bakery, ice_cream | 21 | ✓ |
| Bares | bar, beer, nightclub | 30 | ✓ |
| Supermercados | grocery, supermarket, marketplace, butcher, greengrocer, convenience, frozen_food | 26 | ✓ |
| Tiendas | shop (resto), clothing_store, alcohol_shop, books, laundry, mall | ~30 | ✓ |
| Gasolineras | fuel | 7 | ✓ |
| Carga eléctrica | charging_station | 1 | ✓ |
| Aparcamientos | parking, bicycle_parking | 6 | ✓ |
| Hoteles | lodging, hotel, guest_house, campsite | 9 | ✓ |
| Farmacias | pharmacy | 14 | ✓ |
| Salud | hospital, clinic, doctors, dentist, veterinary | 11 | ✓ |
| Gimnasios y deporte | sports_centre, pitch, stadium, swimming_pool, golf, sailing, fitness | ~16 | ✓ |
| Parques y paseos | park, garden, playground | 33 | ✓ |
| Cultura y turismo | museum, art_gallery, attraction, castle, monument, cinema, theatre, library, visitor_centre | ~20 | ✓ |
| Bancos y cajeros | bank, atm | 6 | ✓ |
| Peluquerías y belleza | hairdresser, beauty | 3 | ✓ |
| Talleres | car, car_repair, car_parts | 6 | ✓ |
| Transporte | bus_station, ferry_terminal, harbor, railway, taxi | 6 | ✓ |
| Educación | school, college, kindergarten | 17 | — |
| Servicios públicos | town_hall, police, post, fire_station | 7 | — |
| Lugares de culto | place_of_worship | 20 | — |

**Ocultos a propósito:**
- **Paradas de autobús** (128): llenarían el mapa. Se podrían mostrar solo a mucho zoom.
- **Oficinas** (`office`: inmobiliarias, abogados, seguros): poco útiles para el usuario de esta app.
- **Puertas y barreras** (`gate`, `lift_gate`, `cycle_barrier`): elementos técnicos del mapa, no destinos.

Las categorías que pidió el autor (las de "Encuentra cerca" de Mapas de Apple) están todas:
- Almuerzo → Restaurantes;
- Gasolineras;
- Supermercados;
- Caminatas → Parques y paseos;
- Aparcamientos;
- Hoteles;
- Comida rápida;
- Farmacias;
- Bares.

## 4. Más tipos que conviene estudiar
Tipos que no aparecen, o aparecen poco, en los datos de Sanlúcar, pero que tienen sentido para la app:

| Tipo | Por qué | Cómo obtenerlo |
|---|---|---|
| **Gimnasios** | Solo aparecen polideportivos y pistas; en OSM los gimnasios (`leisure=fitness_centre`) de Sanlúcar están poco mapeados | Añadirlos en OSM o como establecimientos propios de la app |
| **Playas y chiringuitos** | Muy relevantes en Sanlúcar (Bajo de Guía, La Calzada) | Las playas son áreas (`natural=beach`), no puntos: habría que leerlas de otra capa |
| **Bodegas y enoturismo** | Identidad local (manzanilla). "Bodega La Gitana" aparece como `attraction` | Categoría propia, a partir de nombres o etiquetas `craft=winery` |
| **Lavado de coches e ITV** | Servicios con precio claro | `amenity=car_wash`; la ITV como `shop=car` u oficina |
| **Autoescuelas** | Precio por clase o carné: encaja con "servicio local" | `amenity=driving_school` |
| **Veterinarios** | Ya están dentro de Salud; podrían separarse | `amenity=veterinary` |
| **Farmacias de guardia** | Información que cambia cada día | Datos del Colegio de Farmacéuticos, no de OSM |
| **Alquiler de bicis y patinetes** | Movilidad turística | `amenity=bicycle_rental` |
| **Cajeros** | Hoy van con los bancos | `amenity=atm` |

## 5. Encaje con el modelo de precios
Todas las categorías se pueden **ver e ir con el GPS**. Solo algunas encajan con las plantillas de precio:

| Plantilla | Categorías | Ejemplo de concepto con precio |
|---|---|---|
| **Hostelería** (rango por persona) | Restaurantes, Comida rápida, Cafeterías, Bares | Gasto por persona 10–20 € |
| **Ocio** (varios precios) | Gimnasios y deporte, Cultura (cine, museos, visitas a bodegas), Aparcamientos | Pista 12 €/h · Entrada de cine 7 € · Parking desde 1,20 €/h |
| **Servicio local** (precio único) | Peluquerías, Salud privada (dentista, fisio, veterinario), Talleres, Lavanderías, Autoescuelas | Corte 12 € · Limpieza dental 45 € · Cambio de aceite 60 € |
| **Sin precios** (solo navegación) | Farmacias, Supermercados, Bancos, Educación, Servicios públicos, Culto, Transporte, Parques | — |

**Casos especiales:**
- **Gasolineras:** el precio por litro **ya es público y oficial**: el Ministerio para la Transición Ecológica lo publica en datos abiertos, con actualización diaria. No hace falta que lo aporten los usuarios: se podría importar automáticamente. Es una buena ampliación futura.
- **Supermercados:** no tienen "un precio" (cada producto tiene el suyo). Quedan fuera del modelo. Como mucho, una "cesta básica", fuera del alcance del TFG.
- **Hoteles:** el precio por noche cambia mucho según la fecha. Queda fuera de alcance.

## 5b. Lugares reales con precios en el prototipo (13/11/2026)
Se ha generado la lista de **todos los lugares reales de Sanlúcar que encajan con una plantilla de precio**. El archivo es [prototipo/datos-lugares.js](../prototipo/datos-lugares.js) y lo crea el script `prototipo/herramientas/generar-lugares.js`, que recorre el municipio por cuadrículas y lee la capa `poi`.

| Plantilla | Nº | Tipos |
|---|---:|---|
| Hostelería | 89 | Restaurantes (37), bares y pubs (28), cafeterías (14), comida rápida, heladerías, taberna, discoteca |
| Ocio | 21 | Padel La Jara, Piscina Municipal, Pabellón, Campo de Fútbol Las Palmeras, Pistas El Picacho, Plaza de Toros, Club de Golf, Club Náutico, Multicines Al-Ándalus, Bodega La Gitana, Palacio Ducal, Castillo de Santiago, Torre de San Jacinto, aparcamientos… |
| Servicio local | 11 | Clínicas dentales (4), peluquerías (2), centro de belleza, veterinario, taller, lavandería, policlínica |
| **Total** | **121** | |

**Criterios** (decididos con el autor):
- **Ningún precio inventado.** Los negocios son reales y el repositorio es público, así que todos empiezan **sin precio**.
- **El dueño puede declarar su precio** desde la ficha ("¿Es tu negocio? Declara tus precios"). Se muestra como *declarado por el negocio*.
- **Los clientes aportan lo que pagaron** al llegar con el GPS. El primero recibe una recompensa extra ("¡Pionero!", +25 monedas).
- Cuando hay **3 aportes de clientes** (valor provisional de `MIN_REPORTS`), **manda lo que pagan los clientes**: la mediana en servicios y ocio, y del percentil 25 al 75 en hostelería. Hasta entonces se muestra lo declarado por el negocio, si existe.
- **Se incluyen las instalaciones públicas que cobran** (piscina municipal, pabellón, monumentos con entrada). Si alguno es gratuito, se puede aportar 0 €, que también es información útil.
- **Se excluye la sanidad pública** (centros de salud, consultorios, urgencias).
- **Se quitan los 7 establecimientos inventados** de las versiones anteriores.

**Conceptos sugeridos por tipo** (p. ej. peluquería: corte, corte + barba, tinte; pádel: pista, pista con luz; cine: entrada general, día del espectador). Siguen la regla del modelo: en ocio, el alquiler de espacios va con precio total por hora y las entradas y actividades van por persona. El cliente también puede añadir "otra cosa".

**Ajustes manuales:** OSM marca "Padel La Jara" y "Piscina Municipal" como `sports_centre` (centro deportivo genérico). El generador afina el tipo por el nombre para sugerir conceptos adecuados.

## 6. Limitaciones
- **Calidad de OSM:** depende de lo que hayan mapeado los voluntarios. Faltan gimnasios, algunos nombres están vacíos y las clasificaciones no siempre son correctas (p. ej. "Teatro Principal" aparece como centro comercial).
- **Cobertura en el prototipo:** la lista "Encuentra cerca" solo incluye los puntos de las zonas del mapa que ya se han cargado. La app real necesitaría un servicio de lugares propio que consulte por radio.
- **Lugares sin precio y con precio:** un mismo negocio puede aparecer dos veces, como punto de OSM y como establecimiento de la app. En la app real habría que enlazarlos, por ejemplo guardando el identificador de OSM en el establecimiento.

## 7. Decisiones pendientes
1. ¿Qué categorías abren precios primero, además de las tres plantillas actuales? Propuesta: peluquerías y talleres, porque tienen precio claro y mucha demanda.
2. ¿Se importan los precios oficiales de las gasolineras?
3. ¿Se crea la categoría local "Bodegas y enoturismo"?
