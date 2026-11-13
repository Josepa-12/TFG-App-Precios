# Prototipo navegable — PrecioLocal (nombre provisional)

Prototipo web de la app, centrado en Sanlúcar de Barrameda. Los **lugares con precios son reales**: 121 de OpenStreetMap (`datos-lugares.js`) y 2 clínicas privadas añadidas a mano (`datos-precios-web.js`). **Ningún precio es inventado**:
- los que el negocio publica en su **web oficial** se usan como precio inicial, con la página y la fecha (por ahora, las tarifas de Clínica Areas);
- el resto empiezan vacíos, y los declara el negocio desde su cuenta o los aporta el primer cliente.

Los **centros sanitarios públicos** aparecen en el mapa solo para saber dónde están, sin precios.

## Cómo abrirlo
Abre `index.html` con doble clic en el navegador. Hace falta conexión a internet para cargar el mapa y las fuentes.

## Qué se puede hacer
1. **Bienvenida** con el aviso de privacidad y **creación de cuenta** (`cuentas.js`):
   - **cliente:** descubre precios, aporta lo que paga y gana monedas;
   - **negocio:** busca su local en el mapa y lo vincula, con una verificación simulada. Si no aparece, lo da de alta a mano con nombre, tipo, dirección y precios. Después gestiona sus precios desde el perfil;
   - se entra con **"Continuar con Google"** (simulado) o con correo. También se puede seguir sin cuenta.

   La sesión y los precios del negocio se guardan solo en ese navegador.
2. **Mapa real en 2.5D** (MapLibre + OpenFreeMap) recoloreado con las paletas del proyecto: **B en modo claro** y **D en modo oscuro**.
3. **Interfaz estilo Mapas de Apple:**
   - abajo, una cápsula de cristal con el buscador y el avatar, que muestra tu nivel con un anillo de experiencia;
   - arriba a la izquierda, el desplegable **"Precios"**: por defecto *Ninguno*, para que la pantalla quede limpia. Puedes mostrar todos, servicios locales, hostelería u ocio;
   - arriba a la derecha, los controles de tema y de ubicación.
4. **Botón amarillo €** (pequeño y circular):
   - solo funciona si tienes visitas pendientes, es decir, si respondiste "Más tarde";
   - un punto rojo indica que hay alguna pendiente;
   - al pulsarlo se retoman las preguntas.
5. **Buscador que se despliega hacia arriba:**
   - muestra favoritos, búsqueda por tipo y lugares frecuentes o visitados; al escribir, funciona como un buscador normal;
   - se arrastra con el dedo (o con el ratón) y se queda donde lo sueltas;
   - si lo lanzas rápido se abre o se cierra del todo;
   - se puede buscar a media altura y también se arrastra desde la lista.
6. **Mapa con logos y puntos de interés reales:**
   - los lugares con precios se ven (si los activas) con el logo de su tipo de negocio; la insignia € indica que ya tienen precio;
   - los comercios y servicios reales de OpenStreetMap (supermercados, gimnasios, farmacias…) aparecen integrados en el mapa con su icono, al acercar el zoom;
   - en el buscador, **"Encuentra cerca"** filtra por categoría.
7. **Ir a cualquier sitio**, no solo a los que tienen precios:
   - escribe una dirección;
   - pega un enlace de WhatsApp o Google Maps, o unas coordenadas;
   - mantén pulsado el mapa (o haz clic derecho);
   - o abre `index.html?ll=36.7795,-6.3540&name=Casa%20de%20mam%C3%A1`.

   Las rutas siguen las calles reales (OSRM). Sin conexión, la ruta es aproximada. El código de lugares, direcciones y rutas está en `lugares.js`.
8. **Fichas** de cada plantilla, con favorito (♥), "Avísame si cambia / cuando tenga precio" y **"¿Es tu negocio? Crea tu cuenta de negocio"**. Los servicios pueden ir agrupados por área, con "desde X €" y "Consultar". La ficha indica el origen del precio: web del negocio, declarado por el negocio o aportado por clientes. Con 3 aportes de clientes, manda lo que pagan ellos:
   - servicio local: precio fijo y sin reseñas (psicología es anónima);
   - hostelería: rango por persona y 3 criterios de valoración;
   - ocio: varios precios y 3 criterios de valoración.
9. **Navegación con GPS simulado:**
   - en la ficha eliges **en coche** (por defecto) o **a pie**, y luego pulsas "Simular llegada";
   - durante la ruta, **en coche** ves una flecha de rumbo y **a pie** un círculo con un cono que sigue la brújula del móvil (necesita HTTPS en el móvil; en el ordenador, el cono sigue la ruta);
   - al llegar, la ruta sigue hasta pulsar **"He finalizado"** o se cierra sola en 1 minuto (`ARRIVE_END_MS`);
   - la pregunta "¿has ido?" **sale 1 hora después de llegar** (`ASK_DELAY_MS` en `app.js`; solo con la página abierta);
   - la **burbuja del altavoz**, encima de la del €, silencia o activa los sonidos.
10. **Preguntas al llegar:**
   1. ¿has ido?
   2. ¿qué usaste?
   3. ¿te costó X? Si no, se introduce otro precio con una **regla deslizable** o escribiendo el número. En hostelería se elige un tramo de gasto.
   4. Valoración opcional.
   5. Recompensa.
11. **Perfil** (se abre desde el avatar; la tienda va arriba):
   - **Actividad:** pendientes, aportaciones, favoritos, avisos de precio e insignias.
   - **Tienda** de cosméticos: marcador de posición y estilo de la ruta, que se ven en el mapa.
   - **Ajustes agrupados:**
     - Apariencia;
     - Navegación y voz;
     - Notificaciones;
     - Privacidad;
     - Accesibilidad;
     - Idioma;
     - Ayuda.

## Capturas automáticas (Playwright)
```
npm install playwright@1.56.1
node herramientas/capturas.js "<ruta>/prototipo/index.html" "<ruta>/docs/diseno/capturas"
```
Usa Microsoft Edge (`channel: 'msedge'`). El script recorre todo el flujo y guarda 52 capturas. Acorta la espera de la pregunta de llegada con `window.__askDelayMs`.
