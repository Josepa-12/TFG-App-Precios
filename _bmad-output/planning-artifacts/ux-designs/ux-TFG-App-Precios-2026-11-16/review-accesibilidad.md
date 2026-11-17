# Revisión de accesibilidad — PriceMap

**Postura:** auditor de accesibilidad para una app de consumo en Android, con WCAG 2.2 AA y las pautas de accesibilidad de Android como referencia. Asumo un usuario de TalkBack, otro con la fuente al 200 % y otro con dificultades motoras.

## Veredicto
La base es buena (contraste declarado, 48 dp, "reducir animaciones", nada solo por color, TalkBack en cinco pantallas). El riesgo está en que **casi todo se basa en gestos**: mover el mapa, arrastrar la hoja, deslizar la regla y ordenar el plan. Sin alternativas, un usuario de TalkBack o con poca movilidad no puede completar los recorridos principales.

## Hallazgos
- **[high]** **El mapa solo se maneja con gestos** (EXPERIENCE.md § Interaction Primitives). Con TalkBack, mover y ampliar el mapa a ciegas no sirve. *Fix:* botones de zoom visibles con TalkBack activo y una vista de lista accesible ("Encuentra cerca" ordenado por distancia) como alternativa al mapa para llegar a cualquier ficha.
- **[high]** **La hoja del buscador depende del arrastre** (Component Patterns · Hoja del buscador). *Fix:* acciones de accesibilidad "Expandir" y "Contraer" en la hoja, y tocar la cápsula la abre del todo.
- **[medium]** **La regla de precio es un deslizador** (Component Patterns · Regla de precio). *Fix:* exponerla como control ajustable (TalkBack sube y baja de 1 € en 1 € con gestos de volumen/deslizamiento) y mantener la entrada por teclado numérico.
- **[medium]** **Ordenar el plan solo arrastrando** (Component Patterns · Tarjeta del plan). *Fix:* acciones "Subir" y "Bajar" en cada parada.
- **[medium]** **El cristal sobre el mapa no garantiza el contraste**: el fondo cambia con lo que hay debajo (DESIGN.md § Elevation & Depth). *Fix:* opacidad mínima que asegure 4,5:1 del texto en el peor caso, y fondo opaco si el sistema pide reducir transparencias o hay alto contraste.
- **[medium]** **Fuente al 200 %**: la cápsula y las píldoras tienen altura fija (DESIGN.md § Components). *Fix:* alturas mínimas, no fijas; el texto pasa a dos líneas o se acorta sin cortarse.
- **[medium]** **Los toasts duran 3 s** y pueden llevar información (Component Patterns · Toast). *Fix:* que TalkBack los anuncie como región en vivo y que nada imprescindible dependa solo de un toast.
- **[medium]** **El amarillo como texto** sobre fondo claro tiene contraste ~1,6:1 (DESIGN.md § Colors). *Fix:* regla explícita: el amarillo solo como fondo con `reward-ink` encima, nunca como color de texto en el tema claro.
- **[low]** **Los sonidos** no deben ser la única señal (EXPERIENCE.md § Interaction Primitives). *Fix:* cada sonido acompaña a un cambio visible y a un anuncio de TalkBack.
- **[low]** **Cierre automático de la ruta al minuto** de llegar: un límite de tiempo. *Fix:* aceptable porque no pierde datos; anunciarlo con TalkBack ("La ruta se cerrará en un minuto").
