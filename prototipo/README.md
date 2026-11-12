# Prototipo navegable — PrecioLocal (nombre provisional)

Prototipo web de la app, centrado en Sanlúcar de Barrameda. Los establecimientos son **inventados**.

## Cómo abrirlo
Abre `index.html` con doble clic en el navegador. Hace falta conexión a internet para cargar el mapa y las fuentes.

## Qué se puede hacer
1. **Bienvenida** con el aviso de privacidad.
2. **Mapa real en 2.5D** (MapLibre + OpenFreeMap) recoloreado con las paletas del proyecto: **B en modo claro** y **D en modo oscuro**.
3. **Interfaz estilo Mapas de Apple:**
   - abajo, una cápsula de cristal con el buscador y el avatar, que muestra tu nivel con un anillo de experiencia;
   - arriba a la izquierda, el desplegable de categorías;
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
6. **Fichas** de cada plantilla, con favorito (♥) y "Avísame si cambia el precio":
   - servicio local: precio fijo y sin reseñas (psicología es anónima);
   - hostelería: rango por persona y 3 criterios de valoración;
   - ocio: varios precios y 3 criterios de valoración.
7. **Navegación con GPS simulado:**
   - en la ficha eliges **en coche** (por defecto) o **a pie**, y luego pulsas "Simular llegada";
   - durante la ruta, **en coche** ves una flecha de rumbo y **a pie** un círculo con un cono que sigue la brújula del móvil (necesita HTTPS en el móvil; en el ordenador, el cono sigue la ruta);
   - al llegar, la ruta sigue hasta pulsar **"He finalizado"** o se cierra sola en 1 minuto (`ARRIVE_END_MS`);
   - la pregunta "¿has ido?" **sale 1 hora después de llegar** (`ASK_DELAY_MS` en `app.js`; solo con la página abierta);
   - la **burbuja del altavoz**, encima de la del €, silencia o activa los sonidos.
8. **Preguntas al llegar:**
   1. ¿has ido?
   2. ¿qué usaste?
   3. ¿te costó X? Si no, se introduce otro precio con una **regla deslizable** o escribiendo el número. En hostelería se elige un tramo de gasto.
   4. Valoración opcional.
   5. Recompensa.
9. **Perfil** (se abre desde el avatar):
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
Usa Microsoft Edge (`channel: 'msedge'`). El script recorre todo el flujo y guarda 31 capturas. Acorta la espera de la pregunta de llegada con `window.__askDelayMs`.
