---
name: PriceMap
status: final
created: 2026-11-16
updated: 2026-11-16
sources:
  - ../../prds/prd-TFG-App-Precios-2026-11-16/prd.md
  - ../../prds/prd-TFG-App-Precios-2026-11-16/addendum.md
  - ../../../../prototipo/
---

# PriceMap — Experiencia

## Foundation

App móvil de una sola superficie: **Android**, construida en **Unity 6** con **UI Toolkit**. No hereda un sistema de diseño externo; sigue las convenciones de Android (botón atrás, barra de gestos, notificaciones, permisos) y la referencia de interfaz de Mapas de Apple que ya validó el prototipo web. `DESIGN.md` es la identidad visual; este documento es el funcionamiento. Los requisitos FR-* y UJ-* son los del PRD. El tema inicial sigue al del sistema; se cambia con el botón de tema o en Ajustes.

## Information Architecture

| Superficie | Se llega desde | Para qué |
|---|---|---|
| Bienvenida y privacidad | Primer arranque | Aviso de privacidad, consentimientos de ubicación y notificaciones (FR-41) |
| Cuenta | Bienvenida; perfil sin cuenta; acción que exige cuenta | Google o correo, tipo Cliente / Negocio, seguir sin cuenta (FR-37 a FR-39) |
| Mapa (inicio) | Arranque | Mapa, filtro "Precios", controles flotantes, cápsula (FR-1 a FR-6) |
| Hoja del buscador | Cápsula (arrastrar o tocar) | Favoritos, categorías, frecuentes, resultados, "Encuentra cerca" (FR-13 a FR-15) |
| Ficha de lugar | Marcador, resultado, punto de interés | Precios, variantes, origen, frescura, acciones, Ir (FR-7 a FR-10, FR-60) |
| Ficha de punto libre | Mantener pulsado el mapa; enlace compartido | Dirección, favorito, Ir (FR-16, FR-17) |
| Navegación | Ir | Maniobra, tiempo, posición, Terminar ruta (FR-18 a FR-22) |
| Pregunta de llegada | Notificación; botón €; ficha en revisión | ¿Has ido? → … → recompensa (FR-23 a FR-29) |
| Avisar de precio incorrecto | Ficha | Tipo de aviso y confirmación (FR-34) |
| Plan de tarde | Botón "Plan" (aparece con paradas) | Paradas, personas, gasto, Empezar ruta (FR-54 a FR-56) |
| Perfil (cliente) | Avatar | Nivel, tienda, actividad, insignias, ajustes (FR-49 a FR-52, FR-57) |
| Tienda | Perfil; pantalla de recompensa | Cosméticos (FR-50) |
| Ajustes | Perfil | Secciones de FR-58, cerrar sesión, borrar cuenta (FR-42) |
| Perfil (negocio) | Avatar | Mi lugar, precios y variantes, ofertas, avisos, en revisión (FR-43 a FR-47) |
| Alta / reclamar lugar | Perfil de negocio; "¿Es tu negocio?" | Buscar, verificar por el correo del local, o dar de alta (FR-43, FR-44) |

Sin barra de pestañas: todo nace del mapa. Las hojas se apilan como mucho dos niveles (por ejemplo, ficha sobre el buscador). El botón atrás de Android cierra la capa superior; en el mapa sin capas, sale de la app.

### Referencias visuales

Maquetas de las pantallas clave en `mockups/` y capturas del prototipo web en `docs/diseno/capturas/`. **Este documento y DESIGN.md mandan si hay conflicto** con cualquier maqueta o captura.

| Maqueta | Qué ilustra |
|---|---|
| [`mockups/key-mapa-inicio.html`](mockups/key-mapa-inicio.html) | Mapa en claro y oscuro: píldora "Ver precios", botones redondos, botón € con pendientes, cápsula con avatar |
| [`mockups/key-ficha-lugar.html`](mockups/key-ficha-lugar.html) | Ficha de Clínica Areas con precios reales, origen, frescura y variante "con bono"; estado "En revisión" |
| [`mockups/key-pregunta-llegada.html`](mockups/key-pregunta-llegada.html) | UJ-2 en ChemaBarber: primer precio con la regla de precio, Atrás / Más tarde; recompensa "¡Pionero!" |
| [`mockups/key-perfil-tienda.html`](mockups/key-perfil-tienda.html) | Perfil con nivel, reputación y monedas; tienda con estados equipado, comprar y equipar |

Las demás superficies se construyen con estos documentos y las capturas de la tabla siguiente.

| Superficie | Capturas de referencia |
|---|---|
| Bienvenida y cuenta | `01-bienvenida.png`, `01b-crear-cuenta.png` |
| Mapa | `02-mapa-claro.png`, `02c-mapa-puntos-de-interes.png`, `03-categorias.png`, `04-filtro-hosteleria.png`, `24-mapa-oscuro.png` |
| Hoja del buscador | `05-buscador.png`, `06-buscador-resultados.png`, `06b-buscador-media-altura.png`, `06c-encuentra-cerca.png`, `06f-buscar-direccion.png` |
| Ficha de lugar | `09b-ficha-coche-por-defecto.png`, `25-clinica-precios-web.png`, `35-oferta-en-ficha-y-marcador.png`, `36-precio-antiguo.png`, `38-precio-en-revision.png`, `46-ficha-ir-arriba.png` |
| Ficha de punto libre | `06g-enlace-whatsapp.png`, `06h-ficha-ubicacion-compartida.png` |
| Navegación | `10-navegacion-coche-flecha.png`, `10b-a-pie-cono.png`, `11a-has-llegado-ruta-sigue.png` |
| Pregunta de llegada | `11-pregunta-tras-espera.png`, `13-pendientes-elegir.png`, `14-que-usaste.png`, `15-regla-primer-precio.png`, `17-hosteleria-gasto.png`, `18-valoracion.png`, `16-recompensa-pionero.png`, `45-flujo-atras-mas-tarde.png` |
| Avisar de precio incorrecto | `37-avisar-precio-incorrecto.png` |
| Plan de tarde | `41-anadir-al-plan.png`, `42-plan-de-tarde.png`, `43-plan-parada-1.png` |
| Perfil, tienda y ajustes | `19-perfil.png`, `20-tienda.png`, `47-reputacion.png`, `21-ajustes-apariencia-oscuro.png` |
| Perfil de negocio y alta | `27-cuenta-negocio.png`, `28-buscar-mi-negocio.png`, `29-verificar-negocio.png`, `31-perfil-negocio.png`, `32-alta-manual-negocio.png`, `34-negocio-publica-oferta.png`, `48-negocio-avisos.png` |

## Voice and Tone

Microcopia. La voz de marca está en `DESIGN.md`.

| Hacer | No hacer |
|---|---|
| "Desde 40 € · publicado en su web el 13/11/2026" | "Precio: 40" |
| "¿Te costó 12 €?" | "Por favor, confirme el importe abonado" |
| "Aún no tiene precio. Sé el primero en aportarlo." | "Sin datos" |
| "Al día · confirmado hace 3 días" | "Fiabilidad: alta" |
| "No hay conexión. Te mostramos lo último que guardaste." | "Error de red" |
| "+15 monedas" y "¡Pionero!" en la recompensa | Exclamaciones fuera de la recompensa |

Castellano llano, frases cortas, tuteo. Nunca culpar al usuario. Siempre decir de dónde sale un precio.

## Component Patterns

Comportamiento. El aspecto está en `DESIGN.md.Components`.

| Componente | Uso | Reglas |
|---|---|---|
| Cápsula | Mapa | Tocar el campo abre la hoja con teclado; arrastrar la abre sin teclado. |
| Hoja del buscador | Búsqueda | Sigue al dedo; se queda donde se suelta; lanzamiento > 0,6 px/ms abre o cierra del todo; < 15 % se pliega; > 88 % se abre. Más de 6 px de movimiento es arrastre, no toque. Con la hoja a media altura, arrastrar la lista mueve la hoja; abierta del todo, la lista se desplaza. Sin arrastrar: tocar la cápsula la abre del todo, y la hoja ofrece las acciones de accesibilidad "Expandir" y "Contraer". |
| Píldora "Precios" | Mapa | Menú de 5 opciones; la elegida se marca. |
| Marcador | Mapa | Tocar abre la ficha y centra el lugar en la zona del mapa no tapada por la ficha. |
| Ficha | Lugar | Fila de acciones fija bajo la cabecera. Coche por defecto. |
| Botón € | Mapa | Desactivado sin pendientes; con una, abre la pregunta; con varias, abre la lista para elegir. |
| Regla de precio | Pregunta | Deslizar mueve de 1 € en 1 €; tocar el número abre el teclado numérico con decimales. Con TalkBack es un control ajustable que sube o baja 1 € por gesto. |
| Botón Plan | Mapa | Aparece cuando hay al menos una parada; abre la tarjeta del plan. |
| Toast | Global | 3 s; no tapa el botón Ir ni la fila de acciones. TalkBack lo anuncia como región en vivo; nada imprescindible se comunica solo con un toast. |
| Avatar | Cápsula | Tocar abre el perfil; el anillo se anima al ganar XP (sin animación con "reducir animaciones"). |
| Botones redondos | Mapa | Tema alterna claro/oscuro; ubicación centra el mapa y, con un segundo toque, lo orienta al norte; altavoz silencia o activa sonidos. |
| Posición del usuario | Mapa, navegación | Círculo fuera de ruta; flecha en coche; cono a pie si hay brújula. Gira respecto al norte aunque el mapa esté girado. |
| Ruta | Navegación | Se dibuja al pulsar Ir; se recorta lo ya recorrido; se recalcula al desviarse (FR-19). |
| Tramos de hostelería | Pregunta | Un toque elige y avanza; "Más de 50 €" queda abierto. |
| Recompensa | Pregunta | Suma monedas y XP al mostrarse; "Volver al mapa" o "Ir a la tienda". |
| Tarjeta de cosmético | Tienda | Comprar si hay monedas; equipar si ya es suyo; se ve en el mapa al instante. |
| Insignia | Perfil | Tocar muestra cómo se gana. |
| Tarjeta del plan | Plan | Arrastrar para ordenar, o botones "Subir" y "Bajar" en cada parada (también como acciones de accesibilidad); quitar parada; el gasto se recalcula al cambiar personas o conceptos. |
| Botón primario | Global | Uno por pantalla; acción principal (Ir, Enviar, Empezar ruta). |
| Botón secundario | Global | Alternativa a la principal (Más tarde, Ir a la tienda, Cancelar); nunca más destacado que el primario. |
| Hoja de cuenta | Acciones que exigen cuenta | Tras iniciar sesión, vuelve a la acción que la pidió. |

## State Patterns

| Estado | Superficie | Tratamiento |
|---|---|---|
| Primer arranque | Bienvenida | Aviso de privacidad y consentimientos antes del mapa. |
| Sin ubicación (permiso denegado) | Mapa | Mapa centrado en Sanlúcar; el botón de ubicación explica cómo activarla. |
| Cargando mapa | Mapa | Fondo `{colors.map-fondo}` y las teselas aparecen al llegar; nunca pantalla en blanco. |
| Sin conexión | Mapa, ficha | Zona en caché y fichas vistas; aviso discreto. Aportes en cola. |
| Lugar sin precio | Ficha | "Aún no tiene precio" + "Avísame cuando tenga precio". |
| Precio en revisión | Ficha | Etiqueta "En revisión" junto al precio y explicación breve. |
| Variante vigente | Ficha | La variante que aplica ahora se destaca. |
| Sin cuenta | Acción que la exige | Hoja de cuenta con "Continuar con Google", correo o "Ahora no". |
| Búsqueda sin resultados | Buscador | "No hay resultados" + buscar como dirección. |
| Ruta no disponible | Navegación | Ruta aproximada y aviso. |
| Pendientes | Botón € | Punto rojo. |
| Buscando ruta / desviado | Navegación | Indicador breve; recalcula sin borrar la ruta anterior hasta tener la nueva. |
| GPS sin señal | Navegación | Mantiene la última posición conocida con el indicador atenuado y el aviso "Buscando señal GPS"; no recalcula la ruta ni crea o descarta la visita hasta recuperar la señal. |
| Ficha cargando | Ficha | Cabecera y fila de acciones al instante (datos locales); los precios muestran un esqueleto hasta llegar del servidor, o lo último guardado si no hay conexión. |
| Perfil vacío | Perfil | Sin aportes ni insignias: "Aún no has aportado precios. Navega a un sitio con la app y te preguntaremos al llegar." |
| Buscador sin favoritos ni frecuentes | Hoja del buscador | Primer uso: solo categorías y "Encuentra cerca". |
| Negocio sin verificar | Perfil de negocio | No puede editar; explica que debe confirmar el enlace enviado al correo del local o esperar la aprobación manual. |
| Correo que no coincide o sin correo público | Alta / reclamar lugar | "El correo no coincide con el del local": queda pendiente de aprobación manual, con "Reenviar enlace" si cambia el correo. |
| Enlace caducado | Alta / reclamar lugar | Mensaje claro y "Reenviar enlace". |
| Notificaciones denegadas | Pregunta de llegada | La visita va directa a pendientes (botón €); Ajustes explica cómo activarlas. |
| Aporte sin conexión | Pregunta de llegada | Se guarda en cola; la recompensa se muestra y se confirma al sincronizar. |
| Monedas insuficientes | Tienda | Botón de compra desactivado con "Te faltan N monedas". |
| Borrar cuenta | Ajustes | Confirmación en dos pasos con lo que se borra. |
| Error de inicio de sesión | Cuenta | Mensaje claro y reintento; la app sigue usable sin cuenta. |

## Interaction Primitives

- Tocar para actuar. Mantener pulsado el mapa marca un punto.
- Un dedo mueve el mapa; dos dedos, zoom y giro; arrastre vertical con dos dedos, inclinación.
- Arrastrar hojas verticalmente con física de soltar.
- Botón atrás de Android cierra la capa superior.
- Notificaciones: pregunta de llegada (una hora después), "Avísame si cambia" y, solo durante una ruta, la notificación fija de navegación ("Navegando a ChemaBarber · 6 min"), que al tocarla vuelve a la app y desaparece al terminar la ruta. Nada más.
- Sonidos cortos y suaves en tres momentos: llegada, aparición de la pregunta y monedas ganadas. Se silencian con la burbuja del altavoz y respetan el modo silencio del móvil. Ningún sonido es la única señal: siempre acompaña a un cambio visible y a un anuncio de TalkBack.
- **Prohibido:** anuncios, notificaciones para "volver a la app", rachas que castigan, contadores rojos fuera del botón €.

## Accessibility Floor

Comportamiento. El contraste está en `DESIGN.md`.

- **TalkBack** en mapa, ficha, buscador, pregunta de llegada y perfil: cada control con nombre, rol y estado (por ejemplo, "Botón precios pendientes, 2"). Los marcadores se anuncian como "Clínica Areas, fisioterapia, desde 40 €".
- **Alternativa al mapa por gestos:** con TalkBack activo aparecen botones de acercar y alejar, y "Encuentra cerca" ofrece una lista ordenada por distancia que lleva a cualquier ficha sin tocar el mapa.
- **Nada solo con arrastre:** la hoja del buscador (Expandir / Contraer), la regla de precio (control ajustable) y el plan (Subir / Bajar) tienen alternativa con toques o acciones de accesibilidad.
- **Límites de tiempo anunciados:** al llegar, TalkBack dice "La ruta se cerrará en un minuto"; el cierre no pierde datos.
- Zonas táctiles de al menos 48 dp, el mínimo de Android (el prototipo web usaba 44 px).
- "Reducir animaciones" (sistema o ajuste propio) quita las transiciones de hojas, la animación de la moneda y el movimiento suave de la cámara.
- La escala de fuente del sistema se respeta sin cortar textos.
- El foco de TalkBack sigue el orden de lectura; al abrir una hoja, el foco va a su título; al cerrarla, vuelve a donde estaba.
- Ningún dato solo por color: frescura y plantilla siempre con texto.

## Key Flows

**UJ-1. Lucía descubre cuánto cuesta el fisio antes de pedir cita.**
1. Abre la app sin cuenta: mapa limpio.
2. Toca "Ver precios" → *Servicios locales*: aparecen los logos de las clínicas.
3. Toca Clínica Areas: la ficha sube y el lugar queda centrado encima.
4. **Clímax:** lee "Sesión de fisioterapia · desde 40 € · publicado en su web el 13/11/2026" con un punto verde.
5. Pulsa Ir.
- **Si falla:** FisioSanlúcar no tiene precio; la ficha lo dice y Lucía activa "Avísame cuando tenga precio".

**UJ-2. Andrés aporta el primer precio de ChemaBarber una hora después de cortarse el pelo.**
1. En la ficha de ChemaBarber ve "Aún no tiene precio" y pulsa Ir, a pie; ve el cono de la brújula.
2. Cuando faltan 5 minutos, la visita queda verificada (no lo nota).
3. Llega y pulsa Terminar ruta.
4. Una hora después recibe la notificación "¿Has ido a ChemaBarber?" y la toca.
5. "¿Has ido?" Sí → "¿Qué te hiciste?" elige "Corte de pelo" → marca lo que pagó en la regla.
6. **Clímax:** "¡Pionero!", la moneda "+25" y el anillo de nivel avanzando.
- **Si falla:** si estaba conduciendo cuando tocaba preguntar, la visita queda en pendientes y la retoma con el botón €.

**UJ-3. Carmen, dueña de un bar, publica sus precios y una oferta.**
1. Crea cuenta de negocio con Google.
2. Busca su bar, lo reclama y confirma el enlace que le llega al correo del local.
3. Declara el gasto por persona y una variante "Fin de semana".
4. Publica "Tapa + caña 3 € hasta el domingo".
5. **Clímax:** ve su marcador con la "%".
- **Si falla:** su bar no está en el mapa; lo da de alta con nombre, tipo, ubicación actual y precios.

**UJ-4. Pablo planea la tarde del sábado para cuatro amigos.**
1. Añade pádel, tapas y copas al plan desde las fichas.
2. Abre "Plan" y pone 4 personas.
3. **Clímax:** ve el gasto estimado y el aviso de que las copas aún no tienen precio.
4. Pulsa Empezar ruta y va parada a parada.
- **Si falla:** sin conexión, el plan muestra el último gasto calculado y la ruta aproximada.

**UJ-5. Rosa avisa de que un precio ya no es correcto.**
1. En la ficha de la peluquería pulsa "¿Algún precio está mal?".
2. Elige "El precio ha cambiado".
3. **Clímax:** el aviso queda registrado y, con otro vecino, el precio pasa a "En revisión".
- **Si falla:** si Rosa no tiene cuenta, la hoja de cuenta se abre y, tras entrar, vuelve al aviso.

## Inspiration & Anti-patterns

| Referencia | Qué se toma | Qué no |
|---|---|---|
| Mapas de Apple | Mapa a pantalla completa con controles flotantes; cápsula de cristal con buscador; hoja que se arrastra y se queda donde se suelta; fila de acciones con "Ir" bajo la cabecera de la ficha; "Encuentra cerca" por categorías | La densidad de información de sus fichas: aquí manda el precio |
| Waze | Botón de precios pequeño y circular (€); flecha de rumbo en coche | Avisos de tráfico y comunidad de conductores (no objetivo del PRD) |
| Discord | Ajustes del perfil agrupados en secciones | — |

**Descartado en el prototipo y que la app final no debe recuperar:**
- Boceto B (estilo Waze) y boceto C (híbrido): se eligió el A, estilo Apple.
- Paleta A (azul y gris): no tenía color de recompensa y las monedas no destacaban.
- Barra de pestañas inferior: quitaba mapa a la pantalla del GPS.
- Botones +1 / −1 para corregir el precio: lentos con precios altos; sustituidos por la regla.
- Precios visibles en el mapa por defecto: el filtro empieza en "Ninguno".
- Precios de agregadores (Doctoralia y similares): solo la web oficial del negocio.
- Proponer un precio en hostelería: se usan tramos para evitar el sesgo de confirmación.

## Responsive & Platform

Solo Android en vertical: la orientación horizontal se bloquea en la v1 (el prototipo web tenía un fallo pendiente en horizontal). Pantallas de referencia: 360×740 a 412×915 dp. Respeta las zonas seguras (barra de estado y de gestos) y el modo oscuro del sistema como valor inicial del tema.
