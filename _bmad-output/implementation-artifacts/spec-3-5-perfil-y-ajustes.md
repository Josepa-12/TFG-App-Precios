---
title: 'Historia 3.5: perfil y ajustes'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 3'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-TFG-App-Precios-2026-11-16/mockups/key-perfil-tienda.html'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El avatar de la cápsula no hace nada y no hay ajustes, ni forma de cerrar sesión o borrar la cuenta (FR42, FR58, UX-DR19).

**Approach:** Tocar el avatar abre el perfil (hoja a pantalla casi completa): sin cuenta, una invitación a entrar; con cuenta, nombre, correo y una lista de secciones que las siguientes épicas amplían (Tienda, Actividad, Insignias). Ajustes es una pantalla con siete secciones. Se implementan aquí los ajustes que ya existen (tema, voz, reducir animaciones, ubicación, notificaciones) y "Cerrar sesión" y "Borrar cuenta".

## Boundaries & Constraints

**Always:**
- **Perfil:** cabecera con avatar (inicial o foto de Google), nombre y correo; zona reservada para nivel y reputación (4.11, 5.4); lista de entradas registrables (`EntradaPerfil` con orden, icono, texto, acción) para que cada épica añada la suya sin tocar el perfil. Negocio: entrada "Mi negocio" reservada para la 8.5.
- **Ajustes** (FR58), en este orden:
  - Apariencia: tema Sistema/Claro/Oscuro (amplía `ServicioTema`).
  - Navegación y voz: modo por defecto (coche/a pie) e "Indicaciones por voz" (2.8).
  - Notificaciones: estado del permiso y botón "Abrir ajustes del móvil".
  - Privacidad y ubicación: estado del permiso de ubicación con "Abrir Ajustes"; aviso de privacidad completo (el de la 3.1); hueco para la evaluación (6.2).
  - Accesibilidad: "Reducir animaciones" (Sistema/Sí/No): quita transiciones de hojas y el movimiento suave de cámara (UX-DR19).
  - Idioma: "Español" (único) con texto "Más idiomas próximamente".
  - Ayuda: versión de la app y contacto.
- **Cerrar sesión:** confirmación "¿Cerrar sesión?"; vuelve al uso sin cuenta; los datos locales de Firestore de ese usuario se limpian (`ClearPersistenceAsync` tras `Terminate`, o al menos cerrar sesión).
- **Borrar cuenta** en dos pasos: 1) explica qué se borra (cuenta, progreso, favoritos, ajustes en la nube) y qué no (aportes públicos anónimos, que no se pueden atribuir); 2) confirmación escribiendo "BORRAR" o botón rojo final. Se borran `usuarios/<uid>` y sus subcolecciones conocidas y después el usuario de Auth (`DeleteAsync`; si pide reautenticación reciente, se vuelve a pedir el inicio de sesión).
- **Ajustes locales** en `IPreferencias`; se suben a la nube en la 3.6.
- TalkBack: cada ajuste con nombre, rol y estado ("Reducir animaciones, interruptor, activado").

**Never:** sin tienda, actividad ni insignias (llegan con sus épicas).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Avatar sin cuenta | Toque | Perfil con "Entra para guardar tu progreso" y botón que abre la hoja de cuenta |
| Avatar con cuenta | Toque | Nombre, correo y entradas |
| Tema oscuro | Ajuste → Oscuro | Mapa e interfaz oscuros; se recuerda |
| Reducir animaciones | Sí | Hojas sin transición; cámara sin suavizado |
| Cerrar sesión | Confirmar | Uso sin cuenta; avatar genérico |
| Borrar paso 1 | Toque en "Borrar cuenta" | Explicación de qué se borra |
| Borrar paso 2 | Confirmar | Documentos y cuenta borrados; uso sin cuenta |
| Reautenticación | Auth exige inicio reciente | Pide volver a entrar y reintenta |
| Sin conexión al borrar | Sin red | "Necesitas conexión para borrar la cuenta" |

</frozen-after-approval>

## Open Questions

1. **Tamaño.** A) Partir: parte 1 = perfil y ajustes; parte 2 = cerrar sesión y borrar cuenta (recomendado). B) Una sola.

## Code Map

- Nuevos: `UI/PantallaPerfil.cs`, `UI/PantallaAjustes.cs` (+ uxml/uss), `UI/EntradaPerfil.cs`, `Aplicacion/ServicioAjustes.cs`.
- `Aplicacion/ServicioTema.cs` (modo Sistema/Claro/Oscuro), `ServicioCuentas.cs` (cerrar, borrar), `Adaptadores/Firebase/CuentasFirebase.cs`.
- `UI/CapsulaBuscador.cs` (avatar), `UI/HojaBuscador.cs` y `Vectorial/CamaraMapa.cs` (reducir animaciones).
- `firebase/firestore.rules` (borrar lo propio). Tests: `PruebasPerfil.cs`, `PruebasAjustes.cs`.

## Tasks & Acceptance

**Execution:**
- [ ] Perfil con entradas registrables.
- [ ] Ajustes con las siete secciones y sus efectos.
- [ ] Cerrar sesión y borrar cuenta en dos pasos.
- [ ] TalkBack, capturas "perfil" y "ajustes" y pruebas.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces cada fila de la matriz pasa con cuentas falsas.
- Dada la build de PC, cuando se abren `perfil.png` y `ajustes.png`, entonces se ven las secciones sin cortes.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; capturas de PC; reglas con emulador.
