# Estudio de tipos de negocio y modelo de precios unificado

*Documento de trabajo del TFG. 12/11/2026.*

## 1. Objetivo

La app tiene que mostrar el coste de cualquier establecimiento local, y cada sector cobra de una forma distinta:

- un fisio cobra por sesión;
- un restaurante, según lo que pidas;
- un polideportivo, por pista y por hora.

Este estudio repasa los tipos de negocio más habituales y busca **un único modelo de datos** que sirva para todos. Así no hay que programar cada sector por separado.

## 2. Qué cambia de un negocio a otro

Si se analizan los negocios por **cómo cobran** y no por su sector, todo se reduce a cuatro preguntas:

| Dimensión | Pregunta | Valores posibles |
|---|---|---|
| **Número de conceptos** | ¿Cuántas cosas distintas cobra? | Uno (consulta de psicología) · Varios (pistas, cortes de pelo) |
| **Forma del precio** | ¿El precio es exacto? | **Fijo** (12 €) · **Rango** (20–30 €) · **Desde** (desde 40 €) |
| **Unidad** | ¿Por qué se paga? | Persona · sesión · hora · entrada · uso · noche · mes · unidad |
| **Variante** | ¿Cambia según algo? | Horario (con luz / sin luz) · día (laborable / fin de semana) · persona (adulto / niño) · socio / no socio |

## 3. Catálogo de tipos de negocio

| Sector | Ejemplos | Conceptos | Forma | Unidad habitual | Variantes típicas | ¿Reseñas? | ¿Entra? |
|---|---|---|---|---|---|---|---|
| **Salud y terapias** | Fisioterapia, psicología, podología, nutrición, osteopatía, logopedia | 1–3 (primera visita / siguientes) | Fijo | Sesión | Duración (45/60 min), primera consulta | **No** (decidido) · categoría sensible | Sí |
| **Salud con presupuesto** | Dentista, óptica, clínica estética | Muchos | Desde | Tratamiento | Según el caso | No | Parcial: solo la consulta o la revisión |
| **Cuidado personal** | Peluquería, barbería, estética, uñas | Varios | Fijo / Desde | Servicio | Largo del pelo, hombre / mujer | No | Sí |
| **Hostelería** | Restaurantes, bares, bares de copas, cafeterías, comida rápida | 1 (gasto por persona) | **Solo rango**, nunca fijo | Persona | Comida / cena | **Sí** | Sí |
| **Deporte por reserva** | Polideportivo, pádel, tenis, fútbol 7 | Varios (una entrada por pista) | Fijo | Pista y hora (precio total de la pista) | Con luz, fin de semana, socio | **Sí** | Sí |
| **Deporte por cuota** | Gimnasio, piscina, escuela de artes marciales | 2–3 (pase de un día, cuota mensual) | Fijo | Entrada / mes | Matrícula, horario | **Sí** | Sí |
| **Ocio por entrada** | Cine, bolera, escape room, museo, discoteca, karting | 1–4 | Fijo | Entrada / persona (escape room y similares: **por persona**) | Día del espectador, hora | **Sí** | Sí |
| **Educación** | Academias, autoescuela, clases particulares, música | 2–3 (matrícula, clase, cuota) | Fijo | Hora / clase / mes | Individual / grupo | No | Sí |
| **Hogar y reparaciones** | Fontanero, electricista, cerrajero, taller | Varios | Desde | Visita / hora / servicio | Urgencia, desplazamiento | No | Parcial: solo servicios comunes (cambio de aceite, desplazamiento) |
| **Mascotas** | Veterinario, peluquería canina, residencia | Varios | Fijo / Desde | Consulta / servicio / noche | Tamaño del animal | No | Sí |
| **Lavandería y tintorería** | Autoservicio, tintorería | Varios | Fijo | Lavado / prenda | Tamaño de la máquina | No | Sí |
| **Aparcamiento** | Parking público, parking privado | 1–2 | Fijo | Hora / día | Tarifa nocturna | No | Sí |
| **Alojamiento** | Hotel, hostal | — | Muy variable | Noche | Temporada, fecha | — | **No**: ya lo cubren Booking y similares |
| **Comercio con catálogo** | Supermercado, gasolinera, tienda | Cientos | Fijo | Producto | Ofertas | — | **No**: imposible de mantener y ya existen comparadores |
| **Servicios a presupuesto** | Reformas, abogado, gestoría | — | Presupuesto | Caso | Todo | — | **No**, salvo "primera consulta" como servicio local |

**Conclusión del catálogo:** casi todos los negocios útiles son una **lista corta de conceptos con precio**. Solo quedan fuera los que tienen catálogos enormes (comercio) o precios que cambian constantemente (hoteles, presupuestos). La app no aporta nada en esos casos.

## 4. El modelo unificado

### 4.1 Idea central

> **Un establecimiento es una lista de conceptos, y cada concepto tiene un precio.**

```
Establecimiento
 ├─ nombre, ubicación, categoría
 ├─ precioDeclarado (opcional, lo pone el negocio)
 └─ conceptos[]
      ├─ nombre            "Pista fútbol 7 A"
      ├─ forma             FIJO | RANGO | DESDE
      ├─ unidad            PERSONA | SESION | HORA | ENTRADA | USO | GRUPO | NOCHE | MES | UNIDAD
      ├─ variante          (opcional, texto)  "con luz"
      └─ precio            calculado a partir de los reportes
           ├─ valor / min / max
           ├─ origen       DECLARADO | USUARIOS
           ├─ nReportes
           └─ ultimaActualizacion
```

### 4.2 Plantillas por categoría

Cada categoría es solo una **plantilla** que rellena los valores por defecto. No hace falta programarlas aparte.

| Plantilla | Conceptos por defecto | Forma | Unidad | Reseñas | Anónimo |
|---|---|---|---|---|---|
| **Servicio local** | 1: "Sesión" | Fijo | Sesión | No | Sí, si es salud |
| **Hostelería** | 1: "Gasto por persona" | **Siempre rango** (no admite fijo) | Persona | **Sí** | No |
| **Ocio / deporte** | N: uno por pista, sala o tipo de entrada | Fijo | Hora / entrada / persona | **Sí** | No |

**Regla de la unidad en ocio (decidida):**
- **Alquiler de un espacio** (pistas, salas): **precio total** del espacio. Una pista de 12 €/hora se muestra como 12 €, no como "2 € por persona".
- **Actividades por participante** (escape room, karting, bolera y similares): **precio por persona**.

Tus tres modelos de precio son tres configuraciones del mismo modelo:

- **Fisio:** `[{ "Sesión", FIJO, SESION, 40 € }]`
- **Restaurante:** `[{ "Gasto por persona", RANGO, PERSONA, 20–30 € }]`
- **Polideportivo:** `[{ "Pista A", FIJO, HORA, 12 € }, { "Pista B", FIJO, HORA, 10 € }]`

Si más adelante añades una peluquería, un gimnasio o un parking, es otra plantilla. No hace falta tocar código.

### 4.3 Ejemplo de datos (JSON)

```json
{
  "id": "polideportivo-norte",
  "nombre": "Polideportivo Norte",
  "categoria": "ocio_deporte",
  "lat": 40.4168, "lon": -3.7038,
  "conceptos": [
    { "nombre": "Pista fútbol 7 A", "forma": "FIJO", "unidad": "HORA",
      "precio": { "valor": 12, "origen": "USUARIOS", "nReportes": 6 } },
    { "nombre": "Pista fútbol 7 B", "forma": "FIJO", "unidad": "HORA",
      "precio": { "valor": 10, "origen": "DECLARADO", "nReportes": 0 } },
    { "nombre": "Pista fútbol 7 A", "variante": "con luz", "forma": "FIJO", "unidad": "HORA",
      "precio": { "valor": 15, "origen": "USUARIOS", "nReportes": 3 } }
  ]
}
```

## 5. Cómo se calcula el precio según su forma

La forma del precio también decide cómo se combinan los reportes de los usuarios:

| Forma | El usuario reporta | Precio mostrado |
|---|---|---|
| **Fijo** | Un valor ("me costó 12 €") | **Mediana** de los reportes recientes, que no se deja arrastrar por valores extremos |
| **Rango** | Lo que gastó por persona | **Percentil 25 – percentil 75** de los reportes (el 50 % central) |
| **Desde** | Un valor | **Valor bajo habitual** (por ejemplo, el percentil 10–20) |

Consecuencia importante para hostelería: **nadie declara el rango, sale de los gastos reales de los clientes.** Eso encaja con la idea del proyecto de que el poder lo tienen los clientes.

Para todas las formas se aplican las mismas reglas:

- se exige un mínimo de reportes coincidentes antes de sustituir el precio declarado;
- solo cuentan los reportes recientes (ventana de X meses);
- se muestra de dónde sale el dato: "según 6 usuarios en los últimos 3 meses".

## 6. Cómo se adapta la pregunta al llegar

La pregunta de un toque sigue siendo la misma para todos. Solo añade un paso cuando hay varios conceptos:

| Caso | Flujo |
|---|---|
| 1 concepto, fijo | "¿Has ido?" → "¿Te costó **40 €**?" → **Sí** / **No, fueron…** |
| 1 concepto, rango | "¿Has ido?" → "¿Cuánto gastaste por persona?" → botones por tramos (< 10 · 10–20 · 20–30 · 30–50 · > 50) |
| Varios conceptos | "¿Has ido?" → "¿Qué usaste?" (Pista A / Pista B) → "¿Te costó **12 €**?" → Sí / No |

En hostelería y ocio, después del precio se ofrece una **valoración opcional**, que el usuario puede saltarse para no romper el flujo de un toque. En los servicios locales nunca se pide valoración.

En el caso del rango, los tramos son más rápidos que escribir una cifra y reducen el sesgo de confirmación, porque no se muestra ningún precio de partida.

## 7. Casos difíciles y cómo tratarlos en el prototipo

| Caso | Ejemplo | Propuesta para el prototipo |
|---|---|---|
| El precio cambia según la hora | Pista con luz, happy hour | Variante como texto, en un concepto aparte ("con luz") |
| Precio por grupo | Escape room: 4 personas 60 €, 6 personas 75 € | **Precio por persona** (decidido). Si el negocio cobra por grupo, el usuario reporta el total y la app lo divide entre los participantes |
| Cuota más matrícula | Gimnasio, autoescuela | Dos conceptos: "Matrícula" y "Cuota mensual" |
| Primera visita distinta | Fisio: primera sesión 50 €, siguientes 40 € | Dos conceptos |
| Promociones temporales | 2x1, día del espectador | Fuera de alcance; los reportes con precios atípicos se diluyen en la mediana |
| Propinas o extras | Restaurante | Se reporta el gasto total por persona |

## 8. Recomendación para el TFG

1. **Programar solo el modelo genérico** (establecimiento → conceptos → precio) y **las tres plantillas**: servicio local, hostelería y ocio/deporte.
2. **Incluir en el prototipo un establecimiento de cada plantilla**, y al menos un polideportivo con 2–3 pistas para demostrar el caso de varios conceptos.
3. **Dejar fuera explícitamente** el comercio con catálogo, el alojamiento y los servicios a presupuesto, justificándolo en la memoria con el catálogo de la sección 3.
4. **Defender en la memoria** que la unificación es una decisión de diseño: añadir un sector nuevo consiste en añadir una plantilla, no código. Esto demuestra que el sistema es escalable aunque el prototipo sea pequeño.

## 9. Decisiones

Tomadas (12/11/2026):
- Hostelería y ocio llevan **valoración**. Los servicios locales no.
- Hostelería: **solo rango de precios**, nunca precios fijos, así que no hay productos de referencia (menú del día, caña…).
- Ocio: **precio total** en el alquiler de espacios (pista 12 €, no 2 € por persona); **precio por persona** en actividades como escape room.
- Valoración con **2–3 criterios por categoría**, de 1 a 5 estrellas.

Criterios de valoración (confirmados):

| Categoría | Criterios |
|---|---|
| Hostelería | Calidad (comida/bebida) · Servicio · Ambiente |
| Ocio / deporte | Instalaciones · Atención · Calidad-precio |
