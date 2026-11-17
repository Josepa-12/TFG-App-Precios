---
name: PrecioLocal
description: App de mapa y navegación cuyo dato principal es cuánto cuesta cada sitio. Cercana, limpia, al estilo de Mapas de Apple, con la recompensa en amarillo.
status: final
created: 2026-11-16
updated: 2026-11-16
sources:
  - ../../prds/prd-TFG-App-Precios-2026-11-16/prd.md
  - ../../../../prototipo/styles.css
  - ../../../../docs/memoria-tfg.md
colors:
  # Paleta B (tema claro)
  bg: '#F3F5FB'
  surface: '#FFFFFF'
  surface-2: '#EEF2FC'
  border: '#D3DCF2'
  text: '#13235B'
  muted: '#4B5878'
  accent: '#2F5BEA'
  accent-ink: '#FFFFFF'
  accent-soft: '#D3DCF2'
  reward: '#FFC93C'
  reward-ink: '#13235B'
  reward-edge: '#C99300'
  success: '#0E7A4F'
  danger: '#D6334A'
  control-edge: '#6E7A99'
  focus: '#2F5BEA'
  glass: 'rgba(255,255,255,0.74)'
  tpl-servicio: '#2F5BEA'
  tpl-hosteleria: '#E07B12'
  tpl-ocio: '#1F9D6B'
  fresh-al-dia: '#0E7A4F'
  fresh-podria: '#A87A00'
  fresh-antiguo: '#6E7A99'
  # Paleta D (tema oscuro)
  bg-dark: '#0F1424'
  surface-dark: '#1A2238'
  surface-2-dark: '#232D49'
  border-dark: '#2A3A63'
  text-dark: '#E6ECFA'
  muted-dark: '#9AA6C4'
  accent-dark: '#4C7DFF'
  accent-ink-dark: '#0B1020'
  reward-dark: '#FFD166'
  reward-ink-dark: '#1A1300'
  success-dark: '#4ADE9B'
  danger-dark: '#FF6B81'
  control-edge-dark: '#7F8BAA'
  focus-dark: '#8FB0FF'
  glass-dark: 'rgba(26,34,56,0.74)'
  accent-soft-dark: '#2A3A63'
  reward-edge-dark: '#C99A2E'
  tpl-servicio-dark: '#4C7DFF'
  tpl-hosteleria-dark: '#F59A3C'
  tpl-ocio-dark: '#3DD69B'
  fresh-al-dia-dark: '#4ADE9B'
  fresh-podria-dark: '#FFD166'
  fresh-antiguo-dark: '#7F8BAA'
  # Mapa (claro / oscuro), ya implementado en PaletaMapa
  map-fondo: '#E9EDF7'
  map-agua: '#BFD0F4'
  map-verde: '#DCE6EE'
  map-calle: '#FFFFFF'
  map-calle-menor: '#F7F9FE'
  map-borde: '#C9D4EE'
  map-edificio: '#D3DCF2'
  map-fachada: '#97A6D0'
  map-etiqueta: '#4B5878'
  map-sombra: '#A7B1D4'
  map-fondo-dark: '#141B30'
  map-agua-dark: '#0A0F1F'
  map-verde-dark: '#18213A'
  map-calle-dark: '#2A3554'
  map-calle-menor-dark: '#222C48'
  map-borde-dark: '#0F1424'
  map-edificio-dark: '#2A3A63'
  map-fachada-dark: '#1A2442'
  map-etiqueta-dark: '#9AA6C4'
  map-sombra-dark: '#8A90A8'
typography:
  display:
    fontFamily: Varela Round
    fontSize: 26px
    fontWeight: 400
  title:
    fontFamily: Varela Round
    fontSize: 20px
    fontWeight: 400
  heading:
    fontFamily: Varela Round
    fontSize: 17px
    fontWeight: 400
  body:
    fontFamily: Nunito Sans
    fontSize: 16px
    fontWeight: 400
    lineHeight: 1.4
  label:
    fontFamily: Nunito Sans
    fontSize: 14px
    fontWeight: 800
  meta:
    fontFamily: Nunito Sans
    fontSize: 13px
    fontWeight: 600
  caption:
    fontFamily: Nunito Sans
    fontSize: 12px
    fontWeight: 600
  price:
    fontFamily: Nunito Sans
    fontSize: 22px
    fontWeight: 800
rounded:
  xs: 6px
  sm: 10px
  md: 14px
  lg: 18px
  sheet: 24px
  pill: 999px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 24px
  '6': 32px
components:
  button-primary:
    minHeight: 52px
    rounded: '{rounded.md}'
    background: '{colors.accent}'
    text: '{colors.accent-ink}'
    typography: '{typography.label}'
  button-secondary:
    minHeight: 52px
    rounded: '{rounded.md}'
    border: '{colors.control-edge}'
    typography: '{typography.label}'
  pill:
    minHeight: 48px
    rounded: '{rounded.pill}'
  marcador:
    size: 42px
    rounded: '{rounded.md}'
    border: '2.5px #FFFFFF'
  capsule:
    minHeight: 56px
    rounded: '{rounded.pill}'
    background: '{colors.glass}'
  sheet:
    rounded: '{rounded.sheet} {rounded.sheet} 0 0'
    maxHeight: 72%
  round-button:
    size: 48px
    rounded: '{rounded.pill}'
---

## Brand & Style

PrecioLocal quiere que saber el precio antes de ir sea tan normal como saber cómo llegar. El mapa manda: ocupa toda la pantalla, se ve limpio por defecto y los controles flotan encima, como en Mapas de Apple. La app es cercana y amable, nunca como un comparador frío: formas redondeadas, tipografía blanda y un amarillo cálido que aparece solo cuando el usuario gana algo.

Dos ideas sostienen la identidad. Primero, **el precio con su origen**: cada número va acompañado de dónde sale y cuánto de fresco está. Segundo, **la recompensa visible**: monedas, nivel y cosméticos se ven en el propio mapa, no escondidos en un menú.

## Colors

- **Azul de acción (`accent` #2F5BEA claro / #4C7DFF oscuro)**: botones principales, precios, ruta, enlaces y foco. Es el color de "hacer algo".
- **Amarillo de recompensa (`reward` #FFC93C / #FFD166)**: reservado para monedas, logros, el botón € y el rango de gasto de la hostelería. Nunca decorativo. **Siempre como fondo con `reward-ink` encima, nunca como color de texto** (sobre el fondo claro daría ~1,6:1).
- **Colores de plantilla**: servicio local azul (`tpl-servicio`), hostelería naranja (`tpl-hosteleria`), ocio verde (`tpl-ocio`). Se usan igual en el marcador, la ficha y las listas de un mismo lugar.
- **Frescura**: verde *al día*, ámbar *podría haber cambiado*, gris *antiguo*. Siempre van con un texto, nunca solo con color.
- **Superficies**: `bg` de fondo, `surface` para fichas y hojas, `surface-2` para grupos y campos.
- **Mapa**: paleta propia (`map-*`) en claro y en oscuro: suelo azulado, calles blancas con borde, agua azul, edificios lavanda con fachada en sombra. El mapa no lleva iconos de terceros.
- **Tema oscuro (paleta D)**: pensado para conducir de noche. Cada color de la paleta clara tiene su par `-dark`. En oscuro, el texto sobre el acento es oscuro (`accent-ink-dark`, contraste 5,13:1), no blanco. El naranja de hostelería se aclara a `tpl-hosteleria-dark` (#F59A3C) para mantener el contraste sobre el fondo oscuro.
- **Contraste**: texto ≥ 4,5:1 sobre su fondo en los dos temas; bordes de controles y medidores ≥ 3:1 (`control-edge`).

Evitar: rojo como color de marca (solo errores y avisos), degradados, más de un acento por pantalla, colores de plantilla fuera de su lugar.

## Typography

**Varela Round** para títulos (`display`, `title`, `heading`) y **Nunito Sans** para todo lo demás. Las dos son redondeadas y cercanas. Los precios usan `price` (Nunito Sans 800, 22 px) para destacar en la ficha. Los textos de botón y píldora van en `label` (800). **Los tamaños de la escala se leen como sp en Android**: escalan con la fuente del sistema (UI Toolkit aplica el factor de escala). Con la fuente al 200 %, nada se corta: los contenedores crecen o el texto pasa a dos líneas.

## Layout & Spacing

Escala 4 / 8 / 12 / 16 / 24 / 32 px. Márgenes laterales de 16 px. El mapa ocupa siempre toda la pantalla y nada lo empuja: las capas (cápsula, hoja, ficha) flotan encima. Los controles respetan las zonas seguras del sistema (barra de estado y barra de gestos).

## Elevation & Depth

Tres niveles:
1. **Mapa** (fondo).
2. **Controles flotantes** (cápsula, píldoras, botones redondos, marcadores): sombra suave y fondo de cristal (`glass`). En Unity, desenfoque real si el rendimiento lo permite (NFR-1); si no, fondo translúcido sin desenfoque. La opacidad del cristal nunca baja de la necesaria para que su texto tenga 4,5:1 sobre el peor fondo del mapa; con "reducir transparencias" o alto contraste del sistema, el fondo pasa a opaco (`surface`).
3. **Hojas y fichas**: `surface` opaco con sombra hacia arriba.

Los avisos breves (toast) usan el color de texto como fondo para contrastar con todo.

## Shapes

Todo redondeado. `rounded.md` (14 px) para botones, marcadores y tarjetas; `rounded.sheet` (24 px) para las esquinas superiores de hojas y fichas; `rounded.pill` para la cápsula, las píldoras y los botones redondos. Los marcadores de lugar son cuadrados redondeados con borde blanco; la posición del usuario, un círculo.

## Components

Las alturas son mínimas, no fijas: crecen con la escala de fuente. Referencia visual de los componentes aplicados: [`mockups/key-mapa-inicio.html`](mockups/key-mapa-inicio.html), [`mockups/key-ficha-lugar.html`](mockups/key-ficha-lugar.html), [`mockups/key-pregunta-llegada.html`](mockups/key-pregunta-llegada.html) y [`mockups/key-perfil-tienda.html`](mockups/key-perfil-tienda.html). Este documento manda si hay conflicto.

- **Cápsula inferior** (`{components.capsule}`): buscador + avatar con anillo de XP. Mismo hueco por encima y por debajo del campo.
- **Hoja del buscador** (`{components.sheet}`): se despliega desde la cápsula; asa superpuesta en el borde.
- **Píldora "Precios"** (`{components.pill}`): arriba a la izquierda, con icono y texto ("Ver precios" o el filtro activo).
- **Botones redondos** (`{components.round-button}`; tema, ubicación, €, altavoz): fondo `glass`. El € usa `reward` con punto rojo (`danger`) si hay pendientes.
- **Marcador de lugar** (`{components.marcador}`): color de su plantilla, logo blanco del tipo de negocio, insignia € en amarillo si tiene precio, etiqueta "%" mínima en la esquina inferior si tiene oferta. Sin precio: opacidad reducida.
- **Posición del usuario**: círculo azul con borde blanco; cono translúcido a pie; flecha en coche. Admite cosméticos (dorado, neón).
- **Ruta**: línea `accent` de 6 px con borde; cosméticos discontinua y dorada.
- **Ficha**: hoja `surface`. Cabecera (nombre en `title`, tipo con su color), fila de acciones (coche / a pie con minutos, botón Ir primario) y lista de conceptos con precio (`price`), origen (`meta`) y punto de frescura.
- **Botón primario** (`{components.button-primary}`): fondo `accent`, texto `label`.
- **Botón secundario** (`{components.button-secondary}`): borde `control-edge`, fondo transparente.
- **Regla de precio**: marcas cada 1 €, número grande en `price` que se puede tocar para escribir.
- **Tramos de hostelería**: botones grandes con el rango en amarillo.
- **Recompensa**: moneda amarilla con "+N", barra de XP y botón a la tienda.
- **Tarjeta de cosmético**: miniatura, nombre, precio en monedas; estado comprado / equipado.
- **Insignia**: icono dibujado; bloqueada en gris con candado.
- **Tarjeta del plan**: hoja `surface` con la lista ordenable de paradas (color de su plantilla), selector de personas, gasto estimado en `price` y botón primario "Empezar ruta".
- **Avatar**: círculo de 40 px con el nivel en el centro y la XP como anillo `accent` alrededor.
- **Hoja de cuenta**: botón "Continuar con Google" (primario), "Con correo" (secundario) y "Ahora no" (texto).
- **Toast**: fondo `text`, texto `bg`, 12 px de radio, arriba y centrado.

## Do's and Don'ts

| Hacer | No hacer |
|---|---|
| Amarillo solo para recompensas y el rango de hostelería, siempre como fondo con `reward-ink` encima | Amarillo como decoración, en botones normales o como color de texto |
| Alturas mínimas que crecen con la fuente | Alturas fijas que cortan el texto |
| El color de la plantilla igual en marcador, ficha y listas | Cambiar el color de un mismo lugar entre pantallas |
| Cada precio con su origen y su frescura | Mostrar un número sin decir de dónde sale |
| Iconos dibujados (vectoriales) | Emojis |
| Mapa limpio por defecto | Llenar el mapa de precios o de iconos ajenos |
| Texto oscuro sobre el acento en el tema oscuro | Blanco sobre el acento oscuro (3,7:1) en texto pequeño |
