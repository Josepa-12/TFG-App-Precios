# Informe de validación — PriceMap

- **DESIGN.md:** `DESIGN.md`
- **EXPERIENCE.md:** `EXPERIENCE.md`
- **Fecha:** 2026-11-16

## Veredicto general
Los dos documentos sirven ya de contrato; faltan la sección de inspiración y antipatrones, los títulos literales de los recorridos y el estado de GPS sin señal. La revisión de accesibilidad añade lo más importante: casi todo depende de gestos sin alternativa accesible.

## Veredictos por categoría
- 1. Cobertura de recorridos — fuerte
- 2. Completitud de tokens — adecuada
- 3. Cobertura de componentes — adecuada
- 4. Cobertura de estados — adecuada
- 5. Referencias visuales — escasa
- 6. Exceso y sobreespecificación — adecuada
- 7. Disciplina de herencia — adecuada
- 8. Encaje de forma — adecuada

## Hallazgos por gravedad

### Critical (0)

### High (4)
**[4. Cobertura de estados]** — Falta «GPS sin señal» en navegación (EXPERIENCE.md § State Patterns)  
Es el fallo más habitual de un GPS.  
Fix: Mantener la última posición, aviso «Buscando señal GPS» y no crear ni descartar la visita mientras tanto.

**[8. Encaje de forma]** — Falta «Inspiration & Anti-patterns» (EXPERIENCE.md)  
Obligatoria con productos de referencia: Mapas de Apple, Waze, Discord, y descartes (bocetos B y C, paleta A, pestañas).  
Fix: Añadir la sección con qué se toma y qué se rechaza.

**[Accesibilidad]** — El mapa solo se maneja con gestos (EXPERIENCE.md § Interaction Primitives)  
Con TalkBack, mover y ampliar el mapa a ciegas no sirve.  
Fix: Botones de zoom con TalkBack activo y lista accesible («Encuentra cerca») como alternativa al mapa.

**[Accesibilidad]** — La hoja del buscador depende del arrastre (Component Patterns · Hoja del buscador)  
Sin alternativa para TalkBack o poca movilidad.  
Fix: Acciones de accesibilidad «Expandir» y «Contraer»; tocar la cápsula la abre del todo.


### Medium (10)
**[1. Cobertura de recorridos]** — Títulos de los recorridos no literales del PRD (EXPERIENCE.md § Key Flows)  
Ej.: «Andrés confirma lo que pagó en la barbería» frente a «Andrés confirma el precio una hora después de cortarse el pelo».  
Fix: Usar los títulos exactos del PRD.

**[2. Completitud de tokens]** — Tipografía en px en lugar de sp (DESIGN.md § typography)  
En Android el texto debe escalar con la fuente del sistema.  
Fix: Indicar que los tamaños se leen como sp.

**[4. Cobertura de estados]** — Faltan «ficha cargando» y «perfil vacío» (EXPERIENCE.md § State Patterns)  
Sin tratamiento definido.  
Fix: Añadir ambos estados.

**[5. Referencias visuales]** — Referencia poco específica a las capturas (EXPERIENCE.md § Information Architecture)  
No dice qué captura ilustra qué pantalla.  
Fix: Citar capturas por pantalla o generar maquetas de pantallas clave en el cierre.

**[Accesibilidad]** — La regla de precio es un deslizador (Component Patterns · Regla de precio)  
Difícil con TalkBack.  
Fix: Control ajustable de 1 € en 1 € y entrada por teclado numérico.

**[Accesibilidad]** — Ordenar el plan solo arrastrando (Component Patterns · Tarjeta del plan)  
Sin alternativa.  
Fix: Acciones «Subir» y «Bajar».

**[Accesibilidad]** — El cristal no garantiza el contraste (DESIGN.md § Elevation & Depth)  
El fondo cambia con el mapa.  
Fix: Opacidad mínima para 4,5:1 y fondo opaco con «reducir transparencias» o alto contraste.

**[Accesibilidad]** — Alturas fijas con fuente al 200 % (DESIGN.md § Components)  
La cápsula y las píldoras pueden cortar texto.  
Fix: Alturas mínimas, no fijas.

**[Accesibilidad]** — Toasts de 3 s (Component Patterns · Toast)  
Información que desaparece.  
Fix: Región en vivo para TalkBack y nada imprescindible solo en un toast.

**[Accesibilidad]** — Amarillo como texto en tema claro (DESIGN.md § Colors)  
Contraste ~1,6:1.  
Fix: Amarillo solo como fondo con reward-ink encima.


### Low (7)
**[2. Completitud de tokens]** — Token «pin» frente a «Marcador de lugar» (DESIGN.md § Components)  
Dos nombres para el mismo componente.  
Fix: Renombrar el token a «marcador».

**[3. Cobertura de componentes]** — «Regla deslizable de precio» y «Regla de precio» (DESIGN.md / EXPERIENCE.md)  
Mismo componente, dos nombres.  
Fix: Unificar como «Regla de precio».

**[3. Cobertura de componentes]** — Botón secundario sin comportamiento (EXPERIENCE.md § Component Patterns)  
Solo está su aspecto.  
Fix: Añadirlo junto al primario.

**[4. Cobertura de estados]** — Buscador sin favoritos ni frecuentes (EXPERIENCE.md § State Patterns)  
Primer uso.  
Fix: Mostrar solo categorías y «Encuentra cerca».

**[6. Exceso y sobreespecificación]** — Tamaños repetidos en frontmatter y prosa (DESIGN.md § Components)  
Duplicación menor.  
Fix: En la prosa, remitir al token.

**[Accesibilidad]** — Sonidos como única señal (EXPERIENCE.md § Interaction Primitives)  
Usuarios sordos o con el móvil en silencio.  
Fix: Cada sonido con cambio visible y anuncio.

**[Accesibilidad]** — Cierre automático de la ruta al minuto (Component Patterns · Navegación)  
Límite de tiempo.  
Fix: Anunciarlo con TalkBack.

## Archivos de revisión
- `review-rubric.md`
- `review-accesibilidad.md`