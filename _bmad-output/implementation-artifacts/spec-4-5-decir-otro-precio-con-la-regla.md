---
title: 'Historia 4.5: decir otro precio con la regla'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-4-4-confirmar-lo-que-pague.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Si el precio no coincide, el cliente no puede decir cuánto pagó (FR25, UX-DR10, NFR-6).

**Approach:** "No, fueron…" abre el paso `OtroPrecio` con una regla horizontal deslizable (1 € por marca), un número grande que se puede tocar para escribir con decimales, y chips con los precios ya aportados por otros para ese concepto. Enviar crea el aporte con el mismo lote de la 4.4.

## Boundaries & Constraints

**Always:**
- **Regla** (`ReglaPrecio`, control UI Toolkit propio): marcas cada 1 €, marca larga cada 5 € y número cada 10 €; empieza en el precio mostrado (o 10 € sin precio). Deslizar con inercia, encaja en el euro más cercano al soltar. Sin tope: el rango visible crece al acercarse al final.
- **Número tocable:** abre un campo numérico (teclado decimal) que acepta "12,50" o "12.50". Al confirmar, la regla salta a ese valor (con céntimos).
- **Validación** (`ValidarImporte`, Dominio, pura): > 0, máximo dos decimales, sin tope; "0" y negativos dan "El precio tiene que ser mayor que 0" junto al campo. Se guarda en céntimos enteros (AD-10).
- **Precios aportados:** hasta 4 chips con los valores distintos más frecuentes de los aportes de ese concepto en los últimos 6 meses (lectura de `lugares/<id>/aportes` por `conceptoId`); tocar uno lo elige. Sin aportes, no hay chips.
- **Envío:** botón "Enviar 12,50 €"; mismo lote que la 4.4 con forma fija (`importeCent`). Para "desde", se guarda como forma `desde` con `importeCent`. Evento `AporteCreado(tipo: distinto)`.
- **TalkBack:** la regla es un control ajustable ("Precio, 12 euros, ajustable"): deslizar arriba/abajo sube o baja 1 €. El número es un botón "Escribir el precio".
- "Atrás" vuelve a "¿Te costó X €?".

**Never:** sin tramos (4.6, hostelería no pasa por aquí), sin moneda distinta del euro.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Abrir | Precio mostrado 40 € | Regla en 40 |
| Deslizar | +3 marcas | 43 € |
| Escribir | "12,5" | 12,50 €; `importeCent: 1250` |
| Punto decimal | "12.50" | 1250 |
| Tres decimales | "12,505" | Error "Máximo dos decimales" |
| Cero | "0" | Error y no envía |
| Negativo | "-5" | Error |
| Muy alto | "2500" | Aceptado (sin tope) |
| Chip | Toque en "45 €" | Regla a 45 |
| TalkBack | Gesto subir | +1 € y anuncio "41 euros" |
| Enviar | 43 € | Lote con 4300; Gracias |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/ValidarImporte.cs`, `UI/ReglaPrecio.cs` (control con `Painter2D`), `UI/PasoOtroPrecio.cs`.
- `Dominio/FlujoPregunta.cs` (paso `OtroPrecio`), `Aplicacion/ServicioAportes.cs` (aportes recientes del concepto), `IAportes.cs` (leer aportes públicos).
- `UI/AccesibilidadPantalla.cs`: rol ajustable (`AccessibilityRole.Slider` o acciones incrementar/decrementar).
- Tests: `PruebasReglaPrecio.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `ValidarImporte` y pruebas.
- [ ] Control `ReglaPrecio` con inercia y encaje.
- [ ] Campo numérico y chips de precios aportados.
- [ ] Paso en el flujo y envío.
- [ ] TalkBack ajustable; captura "regla".

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abre `regla.png`, entonces se ven la regla, el número y los chips.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "regla".

**Manual checks:**
- Deslizar en el móvil y TalkBack con el gesto de ajuste.
