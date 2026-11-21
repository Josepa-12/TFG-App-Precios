---
title: 'Historia 4.11: ganar monedas, XP y subir de nivel'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 4'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/mockups/key-perfil-tienda.html'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Aportar no tiene recompensa (FR48, FR49, UX-DR11, UX-DR12).

**Approach:** `ReglaRecompensa` (Dominio, pura) calcula monedas y XP del resultado de la pregunta. `ServicioProgreso` (dueño de monedas, XP y nivel, AD-18) las suma con incremento atómico cuando el servidor confirma el aporte. El agradecimiento pasa a pantalla de recompensa; el avatar muestra nivel con anillo de XP.

## Boundaries & Constraints

**Always:**
- **Tabla** (`ParametrosNegocio`), monedas/XP: confirmar +10/+15; precio distinto +15/+20; pionero +25/+30; valorar +5/+10 (se suma a lo anterior); extra al confirmar un precio "podría haber cambiado" +5 monedas, "antiguo" +10 monedas (solo al confirmar, no al dar otro precio).
  - Tramo de hostelería: "confirmar" si se solapa con el rango mostrado, si no "distinto".
- **Nivel:** `nivel = 1 + xp / 150` (entero); progreso = `xp % 150`.
- **Escritura:** `FieldValue.Increment` en `usuarios/<uid>.monedas` y `.xp` (AD-18). Reglas: solo incrementar `monedas` y `xp` en un lote con un aporte o valoración nuevo propio (`getAfter` de la copia privada con el mismo id de lote) y en cantidades que cuadren con la tabla (máximo +35 monedas/+40 XP por lote; ver pregunta 1). Nunca restar desde aquí (tienda: 5.1).
- **Momento:** se suma al confirmarse el lote (4.9). Sin conexión, la pantalla muestra lo que ganará con "Se sumará al enviarse".
- **Pantalla de recompensa** (UX-DR11): moneda dibujada con "+N" que sube y se desvanece (sin animación con "reducir animaciones"), "+N XP", barra de XP del nivel, "¡Pionero!" si aplica, "¡Subes a nivel 3!" si sube, botón "Volver al mapa" (y "Ir a la tienda" en la 5.1). TalkBack: "Has ganado 10 monedas y 15 puntos de experiencia".
- **Avatar con anillo** (UX-DR12): anillo circular con el progreso del nivel y número de nivel en insignia; en la cápsula y en el perfil. Sin cuenta o negocio, sin anillo.
- **Negocio:** nunca recibe recompensas.

**Never:** sin tienda (5.1), sin sonido (5.5), sin insignias (5.3).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Confirmar al día | Confirmación, frescura al día | +10 / +15 |
| Confirmar podría | Frescura "podría haber cambiado" | +15 / +15 |
| Confirmar antiguo | "Antiguo" | +20 / +15 |
| Distinto | Otro precio | +15 / +20 |
| Pionero | Primer precio | +25 / +30 |
| Pionero + valorar | Ocio | +30 / +40 |
| Tramo solapado | Mostrado 10–20, elige 10–20 | Confirmar |
| Tramo distinto | Mostrado 10–20, elige 30–50 | Distinto |
| Subir nivel | XP 140 + 15 | Nivel 2, anillo 5/150 |
| Sin conexión | Aporte en cola | "Se sumará al enviarse"; sin sumar aún |
| Rechazo | Lote rechazado | Nada sumado |
| Regla trampa | Incremento +500 sin aporte | Denegado (emulador) |

</frozen-after-approval>

## Open Questions

1. **Verificación de monedas en reglas.** Cuadrar exactamente con la tabla obliga a repetirla en `firestore.rules`. A) Tope por lote (+35/+40) y exigir aporte o valoración nuevos en el mismo lote (recomendado: frena trampas gordas sin duplicar la tabla). B) Repetir la tabla exacta en reglas.

## Code Map

- Nuevos: `Dominio/ReglaRecompensa.cs`, `Dominio/Recompensa.cs`, `Dominio/Nivel.cs`, `Aplicacion/IProgreso.cs`, `Aplicacion/ServicioProgreso.cs`, `Adaptadores/Firebase/ProgresoFirestore.cs`, `UI/PantallaRecompensa.cs`, `UI/AvatarNivel.cs`.
- `Aplicacion/ServicioAportes.cs` (resultado del aporte), `UI/HojaPregunta.cs` (Gracias → Recompensa), `UI/CapsulaBuscador.cs`, `UI/PantallaPerfil.cs`.
- `firebase/firestore.rules` (incrementos). Tests: `PruebasRecompensa.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaRecompensa` y `Nivel` con pruebas de la tabla.
- [ ] `ServicioProgreso` con incrementos tras la confirmación.
- [ ] Reglas de incrementos y pruebas.
- [ ] Pantalla de recompensa y avatar con anillo.
- [ ] Captura "recompensa".

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abre `recompensa.png`, entonces se ven la moneda, la XP y la barra.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC.
