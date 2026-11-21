---
title: 'Historia 5.1: comprar y equipar cosméticos en la tienda'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 5'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/mockups/key-perfil-tienda.html'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Las monedas no sirven para nada (FR50, UX-DR12).

**Approach:** Sección "Tienda" en el perfil (entrada registrable de la 3.5) y botón "Ir a la tienda" en la recompensa. Catálogo fijo en el Dominio (cosméticos de posición y de ruta). `ServicioProgreso` compra (incremento atómico negativo + añadir a `compras`) y equipa (`equipados.posicion` / `equipados.ruta`).

## Boundaries & Constraints

**Always:**
- **Catálogo** (`CatalogoCosmeticos`, Dominio): posición → clásica (gratis, equipada por defecto), dorada 150, neón 200; ruta → clásica (gratis), discontinua 100, dorada 250. Ids `posicion/clasica`, `posicion/dorada`, … (AD-11).
- **Tarjeta** (UX-DR12): vista previa dibujada (círculo/flecha o tramo de línea con su estilo), nombre, precio con icono de moneda y estado:
  - Equipado: etiqueta "Equipado", botón desactivado.
  - Comprado: botón "Equipar".
  - No comprado y alcanza: "Comprar · 150".
  - No alcanza: botón desactivado con "Te faltan N monedas" (UX-DR16).
- **Compra:** confirmación "¿Comprar Dorada por 150 monedas?"; un lote con `monedas: Increment(-150)` y `compras: ArrayUnion('posicion/dorada')`. Reglas: decremento = precio exacto del catálogo (tabla repetida en reglas, 4 valores), id no presente en `compras`, `monedas` resultante ≥ 0.
- **Equipar:** `equipados.posicion = id` solo si está en `compras` o es gratis (regla).
- **Saldo en la cabecera**, al instante (estado local optimista + confirmación del servidor; si se rechaza, se revierte con toast).
- Evento `CompraTienda(id)` para la 6.2.
- **Negocio o sin cuenta:** no ve la tienda.
- TalkBack: "Dorada, cosmético de posición, 150 monedas, te faltan 40 monedas, desactivado".

**Never:** sin compras con dinero real, sin cosméticos de pago fuera del catálogo.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Abrir | 120 monedas | Clásicas equipadas; discontinua "Comprar · 100"; dorada "Te faltan 30 monedas" |
| Comprar | Discontinua con 120 | Saldo 20; pasa a "Equipar" |
| Equipar | Discontinua comprada | "Equipado"; clásica de ruta pasa a "Equipar" |
| Sin saldo | Neón con 50 | Botón desactivado con "Te faltan 150 monedas" |
| Desde recompensa | "Ir a la tienda" | Abre la tienda |
| Regla precio falso | Decremento -1 para dorada | Denegado (emulador) |
| Regla doble compra | Comprar algo ya comprado | Denegado |
| Regla equipar sin comprar | Equipar neón no comprado | Denegado |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/Cosmetico.cs`, `Dominio/CatalogoCosmeticos.cs`, `UI/PantallaTienda.cs` (+ uxml/uss), `UI/TarjetaCosmetico.cs`.
- `Aplicacion/ServicioProgreso.cs`, `IProgreso.cs`, `Adaptadores/Firebase/ProgresoFirestore.cs`.
- `UI/PantallaPerfil.cs` (entrada), `UI/PantallaRecompensa.cs` (botón).
- `firebase/firestore.rules` y pruebas. Tests: `PruebasTienda.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Catálogo y estados de tarjeta (pura) con pruebas.
- [ ] Compra y equipar en servicio y adaptador.
- [ ] Reglas y pruebas de emulador.
- [ ] Pantalla de tienda y accesos; captura "tienda".

**Acceptance Criteria:**
- Dada la suite EditMode y la de reglas, cuando se ejecutan, entonces cada fila de la matriz pasa.
- Dada la build de PC, cuando se abre `tienda.png`, entonces se parece a `key-perfil-tienda.html`.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; `npm test` de reglas; captura de PC "tienda".
