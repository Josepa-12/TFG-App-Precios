# Prototipo navegable — PrecioLocal (nombre provisional)

Prototipo web de la app, centrado en Sanlúcar de Barrameda. Los establecimientos son **inventados**.

## Cómo abrirlo
Abre `index.html` con doble clic en el navegador. Hace falta conexión a internet para cargar el mapa y las fuentes.

## Qué se puede hacer
1. **Bienvenida** con el aviso de privacidad.
2. **Mapa real en 2.5D** (MapLibre + OpenFreeMap) recoloreado con las paletas del proyecto: **B en modo claro** y **D en modo oscuro**.
3. **Precios sobre el mapa**, con filtros por plantilla (servicios, hostelería, ocio) y buscador.
4. **Fichas** de cada plantilla:
   - servicio local: precio fijo y sin reseñas (psicología es anónima);
   - hostelería: rango por persona y 3 criterios de valoración;
   - ocio: varios precios y 3 criterios de valoración.
5. **Navegación con GPS simulado**: botón "Simular llegada".
6. **Preguntas al llegar**: ¿has ido? → ¿qué usaste? → ¿te costó X? / otro precio (o tramo de gasto en hostelería) → valoración opcional → recompensa.
7. **Tienda de cosméticos**: marcador de posición y estilo de la ruta, que se ven en el mapa.
8. **Perfil**: nivel, XP, insignias y selector de modo claro / oscuro / automático.

## Capturas automáticas (Playwright)
```
npm install playwright@1.56.1
node herramientas/capturas.js "<ruta>/prototipo/index.html" "<ruta>/docs/diseno/capturas"
```
Usa Microsoft Edge (`channel: 'msedge'`). Recorre todo el flujo y guarda 20 capturas.
