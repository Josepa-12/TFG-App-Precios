---
title: 'Historia 3.4: elegir cuenta de cliente o de negocio'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 3'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-3-3-entrar-con-google-o-con-correo-o-seguir-sin-cuenta.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Todas las cuentas son iguales; un negocio no debe aportar como cliente ni tener monedas (FR39).

**Approach:** En el primer inicio de sesión (sin `usuarios/<uid>.tipo`), una pantalla pregunta "¿Cómo vas a usar PriceMap?" con Cliente y Negocio. El tipo se guarda en `usuarios/<uid>.tipo` y es inmutable. `ServicioCuentas` expone `TipoCuenta` y la UI oculta lo que no corresponde. Las reglas impiden cambiarlo y dejan preparada la función `esCliente()` para las épicas 4 y 7.

## Boundaries & Constraints

**Always:**
- **Dominio:** `TipoCuenta` (Cliente, Negocio).
- **Pantalla de elección:** dos tarjetas grandes con icono y una frase ("Cliente: descubre precios y gana monedas aportando." / "Negocio: gestiona los precios de tu local."); "Continuar" activo al elegir; aviso "No podrás cambiarlo después." No se puede saltar: atrás cierra sesión y vuelve sin cuenta.
- **Guardado:** `usuarios/<uid>` se crea con `tipo`, `creado` (`ServerTimestamp`) y, si es cliente, `monedas: 0`, `xp: 0`. Con actualización por campo (merge), nunca sobrescribir (AD-18).
- **Reglas:** crear `usuarios/<uid>` solo el propio uid, con `tipo` en ['cliente','negocio']; `tipo` no se puede modificar después; un cliente no puede fijar `monedas`/`xp` distintos de 0 al crear. Funciones `esCliente()` y `esNegocio()` que leen el documento del usuario.
- **Negocio en la app:** sin botón €, sin tienda ni monedas en el perfil, sin preguntas de llegada (esto lo aplican las historias que añaden esas piezas consultando `TipoCuenta`; aquí se deja el estado y una prueba de que `ServicioCuentas.EsCliente` es falso).
- Si el documento ya tiene tipo (otro móvil), no se pregunta.
- Sin conexión en el primer inicio: la escritura queda en la cola de Firestore y la app sigue con el tipo elegido.

**Never:** sin cambio de tipo, sin cuentas mixtas.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Primer inicio | Sin documento | Pantalla de elección |
| Elegir cliente | Cliente + Continuar | `tipo: cliente`, monedas 0, XP 0 |
| Elegir negocio | Negocio + Continuar | `tipo: negocio`, sin monedas |
| Segundo móvil | Documento con tipo | Sin pantalla |
| Atrás | En la elección | Cierra sesión; uso sin cuenta |
| Regla cambiar tipo | Update `tipo` | Denegado (emulador) |
| Regla monedas al crear | Crear con `monedas: 500` | Denegado |
| Regla otro uid | Crear `usuarios/B` siendo A | Denegado |

</frozen-after-approval>

## Code Map

- Nuevos: `Dominio/TipoCuenta.cs`, `UI/PantallaTipoCuenta.cs` (+ uxml/uss), `Adaptadores/Firebase/PerfilFirestore.cs`.
- `Aplicacion/ServicioCuentas.cs`, `ICuentas.cs` (tipo).
- `firebase/firestore.rules` y `firebase/pruebas/`.
- Tests: `PruebasCuentas.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] `TipoCuenta`, servicio y adaptador del documento.
- [ ] Pantalla de elección con TalkBack.
- [ ] Reglas y pruebas de emulador.
- [ ] Pruebas EditMode y captura "tipo-cuenta".

**Acceptance Criteria:**
- Dada la suite de reglas, cuando se ejecuta, entonces las filas de reglas pasan.
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `npm test` en `firebase/pruebas`; pruebas EditMode por lotes; captura de PC.
