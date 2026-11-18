---
title: 'Historia 1.7 (parte 2): orientar al norte, estados del botón y aviso de ubicación desactivada'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-7-ver-mi-posicion-en-el-mapa.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La parte 1 deja el círculo, el cono y el centrado, pero el botón de ubicación siempre dice "Centrar en mi posición", no orienta al norte (UX-DR6), con el permiso denegado no hace nada, y con la ubicación del móvil apagada se queda en "Buscando" para siempre (hallazgo 3 de la revisión de la parte 1).

**Approach:** El botón pasa a tener estados. Primer toque con posición: centra. Segundo toque, ya centrado: orienta al norte (`IMapa.OrientarNorte()`). Si el usuario mueve el mapa, el siguiente toque vuelve a centrar. Con el permiso denegado o la ubicación del móvil apagada, al tocarlo sale un aviso con "Abrir Ajustes". TalkBack lee el estado. `UbicacionUnity` se separa de `Input.location` tras una interfaz pequeña para poder probarlo.

## Boundaries & Constraints

**Always:**
- **Estados nuevos de `EstadoUbicacion`:** se añade `Apagada` (permiso concedido, pero `Input.location.isEnabledByUser` es false o el servicio da `Failed`). Al volver a encenderse la ubicación, pasa a `Buscando` al volver a la app.
- **Acción del botón** (`AccionUbicacion`, puro y probado):
  - `SinPermiso` → explicación de la parte 1.
  - `Denegado` o `Apagada` → aviso de ubicación desactivada.
  - `Buscando` → nada.
  - `Activa` y mapa no centrado en el usuario → centrar.
  - `Activa`, centrado y con giro ≠ 0 → orientar al norte (giro 0, sin cambiar zoom ni inclinación).
  - "Centrado" = la posición del usuario cae a menos de 8 dp del centro de la pantalla. Cualquier arrastre lo rompe.
- **TalkBack (etiqueta del botón según la acción):** "Centrar en mi posición", "Orientar al norte", "Buscando tu posición", "Ubicación desactivada".
- **Aviso:** ventana modal (mismo patrón que `ExplicacionUbicacion`):
  - Denegado: *"La ubicación está desactivada. Puedes activarla en Ajustes > Aplicaciones > PriceMap > Permisos."*
  - Apagada: *"La ubicación del móvil está apagada. Actívala en los ajustes rápidos del móvil."*
  - Botones "Abrir Ajustes" (denegado: ajustes de la app con `android.settings.APPLICATION_DETAILS_SETTINGS`; apagada: `android.settings.LOCATION_SOURCE_SETTINGS`) y "Cerrar". El botón atrás la cierra.
  - Abrir los ajustes va por un puerto (`ISistemaAjustes` o un método nuevo de `IUbicacion`), nunca desde la UI con JNI.
- **Separación del adaptador:** `UbicacionUnity` usa una interfaz interna `IFuenteUbicacion` (posición, rumbo, `isEnabledByUser`, estado del servicio, permiso) con la implementación real `FuenteUbicacionUnity`. Las pruebas usan una fuente falsa.
- **Captura de PC "posicion":** ubicación simulada en la Plaza del Cabildo con rumbo 45°, mapa centrado; se ven el círculo y el cono.
- Textos por Localization.

**Never:** sin seguir al usuario automáticamente, sin modo brújula (girar el mapa con el móvil) y sin GPS en segundo plano.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Primer toque | `Activa`, mapa lejos del usuario | Centra; etiqueta pasa a "Orientar al norte" si giro ≠ 0 |
| Segundo toque | `Activa`, centrado, giro 40° | Giro 0; etiqueta "Centrar en mi posición" |
| Centrado y al norte | `Activa`, centrado, giro 0 | Vuelve a centrar (no hace nada visible) |
| Arrastre tras centrar | Usuario mueve el mapa 50 dp | Siguiente toque centra de nuevo |
| Denegado | `Denegado` y toque | Aviso de permiso con "Abrir Ajustes" |
| Ubicación apagada | Permiso concedido, `isEnabledByUser` false | Estado `Apagada`; toque → aviso de ubicación apagada |
| Servicio `Failed` | Permiso concedido, estado `Failed` | Estado `Apagada` |
| Se enciende al volver | `Apagada`, vuelve a la app con la ubicación encendida | `Buscando` y luego `Activa` |
| Atrás con aviso | Aviso abierto y `Escape` | Se cierra; foco al botón de ubicación |
| Buscando | `Buscando` | TalkBack: "Buscando tu posición"; toque no hace nada |

</frozen-after-approval>

## Open Questions

1. **¿Qué pasa con el toque estando ya centrado y al norte?** A) Nada visible (recomendado). B) Alternar a "modo brújula" (el mapa gira con el móvil): no está en el diseño.

## Code Map

- `Scripts/Aplicacion/EstadoUbicacion.cs`, `AccionUbicacion.cs`, `ServicioUbicacion.cs`: nuevo estado y decisión del botón.
- `Scripts/Aplicacion/IMapa.cs`: `OrientarNorte()`. `Vectorial/CamaraMapa.cs` (`giro`) y `MapaVectorial`; actualizar los `MapaFalso`.
- `Scripts/Adaptadores/Gps/UbicacionUnity.cs`: separar `IFuenteUbicacion`/`FuenteUbicacionUnity`; abrir ajustes con `AndroidJavaObject` (Intent).
- `Scripts/UI/PantallaMapa.cs` (botón de ubicación), `ExplicacionUbicacion.cs` (patrón modal) → nuevo `AvisoUbicacion.cs`, `AccesibilidadPantalla.cs` (etiqueta dinámica), `ClavesTexto.cs`, `Editor/PrepararInterfaz.cs` (`TextosEs`).
- `Scripts/Arranque/CapturaPruebas.cs`: captura "posicion".

## Tasks & Acceptance

**Execution:**
- [ ] `EstadoUbicacion.Apagada`, `AccionUbicacion` con centrar/norte/aviso y su prueba.
- [ ] `IMapa.OrientarNorte()` en `MapaVectorial` y `MapaFalso`.
- [ ] `IFuenteUbicacion` + `FuenteUbicacionUnity`; `UbicacionUnity` probado con fuente falsa (denegado, permiso retirado, apagada, `Failed`, umbral de 1° del rumbo).
- [ ] `UI/AvisoUbicacion.cs`, abrir ajustes por el puerto, etiquetas de TalkBack y textos.
- [ ] `CapturaPruebas.cs`: captura "posicion".
- [ ] `Tests/EditMode/PruebasUbicacion.cs`: cada fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas y `UbicacionUnity` se prueba sin `Input.location` real.
- Dada la build de PC, cuando se abre `posicion.png`, entonces se ven el círculo y el cono centrados.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes, con el editor cerrado: todas pasan.
- Build de PC y `PriceMap.exe -captura <carpeta>`: `posicion.png` y log sin errores. Después, borrar `link.xml` y deshacer `addressables_content_state.bin`.

**Manual checks:**
- En un Android real: segundo toque al norte, aviso con "Abrir Ajustes" que lleva a la pantalla correcta, y estado "apagada" con la ubicación del móvil desactivada.
