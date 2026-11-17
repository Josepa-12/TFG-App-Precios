# Revisión editorial (estructura + redacción) — PRD PrecioLocal

**Lectura del propósito:** este documento existe para que el autor del TFG y los flujos de BMAD (épicas, historias, arquitectura) sepan exactamente qué debe hacer la app final de PrecioLocal y cuándo está terminada.

**Modelo de estructura:** Estratégico / pirámide (PRD).

**Recuento:** prd.md 4.956 palabras; addendum.md 348 palabras.

| # | Pasada | Texto original | Texto revisado | Cambios |
|---|---|---|---|---|
| E1 | structure | §0 Propósito — no adelanta las decisiones clave | MOVE: añadir al final de §0 tres líneas con las decisiones que condicionan todo: Android con Unity; MVP completo sin priorizar; gamificación para todos los participantes | La pirámide pide la conclusión arriba; hoy están dispersas en §2.2, §8 y §9 (+~45 palabras) |
| E2 | structure | FR-59 dentro de §4.11 Perfil y ajustes | MOVE: a una nueva §4.12 "Evaluación del TFG" | No es una función del perfil; mezclarlo despista al crear historias (0 palabras) |
| E3 | structure | §8.2 "Sectores excluidos (catálogo, alojamiento, presupuesto). Fuera." | CUT: ya están en §2.2 y §7 | Tercera aparición del mismo dato (−8 palabras) |
| E4 | structure | §6.1 viñetas 2 y 3 | CONDENSE: "La pregunta de llegada y el anonimato siguen FR-17, FR-22 y FR-30." | Repiten requisitos ya definidos; mejor remitir a ellos (−15 palabras) |
| E5 | structure | §8.2 [NOTE FOR PM] sobre la voz | QUESTION: con el MVP completo decidido, ¿se mantiene esta salida? | Contradice la decisión de no priorizar |
| E6 | structure | §3 Glosario antes de las funciones; tabla de §4.2; §8.1 que resume §4; §11 | PRESERVE | Definen antes de usar, ayudan a ver de un vistazo y dejan constancia de lo confirmado |
| E7 | prose | "hostelería, al día < 30 días, podría haber cambiado 30-60, antiguo > 60; servicio local y ocio, al día < 3 meses, 3-6 meses, > 6 meses." | Tabla: Plantilla · Al día · Podría haber cambiado · Antiguo (Hostelería: < 30 días · 30-60 días · > 60 días; Servicio local y ocio: < 3 meses · 3-6 meses · > 6 meses) | La segunda mitad pierde las etiquetas y no se entiende |
| E8 | prose | "la validación impide precios ≤ 0 o negativos" | "la validación impide precios menores o iguales a 0" | "≤ 0" ya incluye los negativos |
| E9 | prose | "el primer cliente aporta el primero eligiendo un concepto sugerido" | "el primer cliente que llega aporta el precio inicial eligiendo un concepto sugerido" | Repetición confusa de "primero" |
| E10 | prose | "**Descripción:** El corazón colaborativo. Solo pregunta a quien…" | "**Descripción:** Es el núcleo colaborativo de la app: solo pregunta a quien…" | Frase sin verbo |
| E11 | prose | "Fuera de la navegación es un círculo; si el dispositivo tiene brújula, con un cono de orientación." | "Fuera de la navegación es un círculo y, si el dispositivo tiene brújula, lleva un cono de orientación." | Segunda parte sin verbo |
| E12 | prose | "Sin cuenta se puede ver el mapa, fichas, buscar y navegar" | "Sin cuenta se puede ver el mapa y las fichas, buscar y navegar" | Enumeración no paralela |
| E13 | prose | "En ocio, el alquiler de un espacio muestra el precio total y las actividades por participante, el precio por persona." | "En ocio, el alquiler de un espacio muestra el precio total, y las actividades por participante muestran el precio por persona." | Elipsis que obliga a releer |
| E14 | prose | "cuando 3 clientes avisan de que ya no están." | "cuando 3 clientes avisan de que ya no está vigente." | Sujeto ambiguo (¿las ofertas o los clientes?) |
| E15 | prose | "Solo una visita verificada por lugar y ruta." | "Cada ruta crea como máximo una visita verificada." | Frase sin verbo |
| E16 | prose | "Usuarios de iPhone: la v1 es solo Android (ver §8)." | "Usuarios de iPhone: la v1 es solo Android (ver §8.2)." | Referencia más precisa |

**Resumen:** 16 recomendaciones (5 de estructura, 1 de conservar y 10 de redacción). Si se aceptan todas, el documento cambia en unas +22 palabras netas (+0,4 %): se añade la cabecera de decisiones y se recortan repeticiones. Ninguna recomendación sacrifica comprensión.
