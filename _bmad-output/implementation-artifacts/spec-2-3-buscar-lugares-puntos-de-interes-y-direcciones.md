---
title: 'Historia 2.3: buscar lugares, puntos de interés y direcciones'
type: 'feature'
created: '2026-11-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#Épica 2'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-TFG-App-Precios-2026-11-16/ARCHITECTURE-SPINE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La hoja del buscador (2.2) no busca. El usuario necesita encontrar lugares de la app, puntos de interés y direcciones (FR14).

**Approach:** `ServicioBusqueda` (Aplicación) mezcla tres fuentes: lugares de la app (`ServicioLugares`), puntos de interés de las teselas cargadas (`IMapa.PuntosInteres`) y direcciones de Photon por un puerto `IGeocodificador` (adaptador `PriceMap.Adaptadores.Busqueda`). Ordena por coincidencia y cercanía. Tocar un resultado centra el mapa y abre la ficha del lugar (los puntos de interés y las direcciones abren la ficha de punto libre de la 2.5; hasta entonces, solo centran).

## Boundaries & Constraints

**Always:**
- **Normalización** (pura, en Dominio `ReglaBusqueda`): minúsculas, sin tildes, sin signos; coincide si cada palabra de la consulta es prefijo de alguna palabra del nombre (o de la dirección del lugar).
- **Orden:** primero lugares de la app, luego puntos de interés, luego direcciones; dentro de cada grupo, por puntuación de coincidencia (nombre exacto > prefijo > contiene) y después por distancia a la posición del usuario (o al centro del mapa si no hay posición). Máximo 20 resultados.
- **Photon:** `https://photon.komoot.io/api/?q=<texto>&lat=<lat>&lon=<lon>&limit=8&lang=default&bbox=<Sanlúcar ampliada>`; con espera de 350 ms tras la última tecla (debounce) y mínimo 3 caracteres. Cancela la petición anterior. Sin claves. Cabecera `User-Agent` genérica ("PriceMap/1.0"), sin datos del usuario.
- Los puntos de interés que repiten un lugar de la app se quitan (misma regla de repetidos que la 1.6).
- **Resultados:** fila con icono del tipo, nombre, segunda línea (tipo · distancia "350 m"/"1,2 km") y precio resumido si lo tiene.
- **Sin resultados:** "No hay resultados" y el botón "Buscar como dirección" (lanza Photon aunque el texto tenga menos de 3 caracteres o no haya coincidencias locales).
- **Sin conexión:** se buscan los lugares de la app y los puntos cargados; un aviso discreto "Las direcciones necesitan conexión".
- **TalkBack:** cada resultado es un botón con "Nombre, tipo, a 350 m, desde 20 €"; se anuncia "N resultados" al terminar.
- Errores de red como `Resultado<T>`, nunca excepciones.

**Never:** sin historial de búsquedas (las direcciones no se guardan, FR17), sin autocompletar de Google.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|--------------|---------------------------|
| Lugar | "clinica ar" | Clínica Areas el primero |
| Sin tildes | "jose" | Encuentra "José …" |
| Punto de interés | "mercadona" con la tesela cargada | Mercadona con categoría supermercado |
| Dirección | "calle ancha 10" | Resultados de Photon tras 350 ms |
| Repetido | Punto de interés con el nombre de un lugar a < 80 m | Solo el lugar |
| Sin resultados | "zzzz" | "No hay resultados" + "Buscar como dirección" |
| Sin conexión | Photon falla | Lugares locales y aviso de direcciones |
| Tecleo rápido | 5 teclas en 200 ms | Una sola petición a Photon |
| Toque en lugar | Resultado Clínica Areas | Mapa centrado y ficha abierta |

</frozen-after-approval>

## Open Questions

1. **Bbox de Photon.** A) Limitar a Sanlúcar y alrededores (≈ 15 km) (recomendado: resultados útiles). B) Sin límite, solo con prioridad por cercanía.

## Code Map

- Nuevos: `Dominio/ReglaBusqueda.cs`, `Aplicacion/IGeocodificador.cs`, `Aplicacion/ServicioBusqueda.cs`, `Aplicacion/ResultadoBusqueda.cs`, `Adaptadores/Busqueda/` (asmdef + `GeocodificadorPhoton.cs` con `UnityWebRequest`).
- `Scripts/UI/HojaBuscador.cs` (2.2): campo, lista de resultados, estados.
- `Scripts/Arranque/Arranque.cs`: compone el geocodificador y el servicio.
- Tests: `PruebasBusqueda.cs` con geocodificador falso.

## Tasks & Acceptance

**Execution:**
- [ ] `ReglaBusqueda` (normalizar, puntuar) y pruebas.
- [ ] `IGeocodificador` + `GeocodificadorPhoton` (parseo GeoJSON, cancelación).
- [ ] `ServicioBusqueda` (mezcla, debounce, sin conexión).
- [ ] Lista de resultados, estados vacío y sin conexión, TalkBack.
- [ ] Pruebas de cada fila de la matriz.

**Acceptance Criteria:**
- Dada la suite EditMode, cuando se ejecuta, entonces pasan todas y ninguna llama a la red.
- Dada la build de PC, cuando se busca "clinica", entonces el log no tiene errores y la captura "busqueda" muestra los resultados.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Pruebas EditMode por lotes; captura de PC "busqueda".

**Manual checks:**
- En el móvil con y sin datos: direcciones de Photon y aviso sin conexión.
