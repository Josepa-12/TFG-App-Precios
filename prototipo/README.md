# Prototipo navegable â€” PrecioLocal (nombre provisional)

Prototipo web de la app, centrado en SanlÃºcar de Barrameda. Los establecimientos son **inventados**.

## CÃ³mo abrirlo
Abre `index.html` con doble clic en el navegador. Hace falta conexiÃ³n a internet para cargar el mapa y las fuentes.

## QuÃ© se puede hacer
1. **Bienvenida** con el aviso de privacidad.
2. **Mapa real en 2.5D** (MapLibre + OpenFreeMap) recoloreado con las paletas del proyecto: **B en modo claro** y **D en modo oscuro**.
3. **Interfaz estilo Mapas de Apple:**
   - abajo, una cÃ¡psula de cristal con el buscador y el avatar, que muestra tu nivel con un anillo de experiencia;
   - arriba a la izquierda, el desplegable de categorÃ­as;
   - arriba a la derecha, los controles de tema y de ubicaciÃ³n.
4. **BotÃ³n amarillo â‚¬** (pequeÃ±o y circular):
   - solo funciona si tienes visitas pendientes, es decir, si respondiste "MÃ¡s tarde";
   - un punto rojo indica que hay alguna pendiente;
   - al pulsarlo se retoman las preguntas.
5. **Buscador que se despliega hacia arriba:**
   - muestra favoritos, bÃºsqueda por tipo y lugares frecuentes o visitados; al escribir, funciona como un buscador normal;
   - se arrastra con el dedo (o con el ratÃ³n) y se queda donde lo sueltas;
   - si lo lanzas rÃ¡pido se abre o se cierra del todo;
   - se puede buscar a media altura y tambiÃ©n se arrastra desde la lista.
6. **Fichas** de cada plantilla, con favorito (â™¥) y "AvÃ­same si cambia el precio":
   - servicio local: precio fijo y sin reseÃ±as (psicologÃ­a es anÃ³nima);
   - hostelerÃ­a: rango por persona y 3 criterios de valoraciÃ³n;
   - ocio: varios precios y 3 criterios de valoraciÃ³n.
7. **NavegaciÃ³n con GPS simulado:**
   - en la ficha eliges **en coche** (por defecto) o **a pie**, y luego pulsas "Simular llegada";
   - al llegar, la pregunta "Â¿has ido?" **sale al cabo de 1 minuto** (`ASK_DELAY_MS` en `app.js`);
   - la **burbuja del altavoz**, encima de la del â‚¬, silencia o activa los sonidos.
8. **Preguntas al llegar:**
   1. Â¿has ido?
   2. Â¿quÃ© usaste?
   3. Â¿te costÃ³ X? Si no, se introduce otro precio con una **regla deslizable** o escribiendo el nÃºmero. En hostelerÃ­a se elige un tramo de gasto.
   4. ValoraciÃ³n opcional.
   5. Recompensa.
9. **Perfil** (se abre desde el avatar):
   - **Actividad:** pendientes, aportaciones, favoritos, avisos de precio e insignias.
   - **Tienda** de cosmÃ©ticos: marcador de posiciÃ³n y estilo de la ruta, que se ven en el mapa.
   - **Ajustes agrupados:**
     - Apariencia;
     - NavegaciÃ³n y voz;
     - Notificaciones;
     - Privacidad;
     - Accesibilidad;
     - Idioma;
     - Ayuda.

## Capturas automÃ¡ticas (Playwright)
```
npm install playwright@1.56.1
node herramientas/capturas.js "<ruta>/prototipo/index.html" "<ruta>/docs/diseno/capturas"
```
Usa Microsoft Edge (`channel: 'msedge'`). El script recorre todo el flujo y guarda 29 capturas.
