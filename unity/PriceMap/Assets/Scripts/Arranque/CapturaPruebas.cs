using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.Arranque
{
    /// <summary>
    /// Solo para comprobar la interfaz sin móvil: al lanzar el ejecutable con
    /// "-captura &lt;carpeta&gt;", espera a que cargue el mapa, guarda una captura en cada tema y sale.
    /// Sin ese argumento no hace nada.
    /// </summary>
    public class CapturaPruebas : MonoBehaviour
    {
        string carpeta;
        ServicioTema tema;
        ServicioFiltro filtro;
        IPreferencias preferencias;
        MapaVectorial mapa;
        PantallaMapa pantalla;

        /// <summary>Carpeta de "-captura &lt;carpeta&gt;", o null si no se pide (o en la APK de verdad).</summary>
        static string CarpetaPedida()
        {
#if !(UNITY_STANDALONE || UNITY_EDITOR || DEVELOPMENT_BUILD)
            return null; // nunca en la APK de verdad
#else
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-captura");
            return i < 0 || i + 1 >= args.Length ? null : args[i + 1];
#endif
        }

        /// <summary>True si se ha lanzado con "-captura &lt;carpeta&gt;" (el Arranque usa entonces la ubicación falsa).</summary>
        public static bool SePide() => CarpetaPedida() != null;

        /// <summary>Posición en la Plaza del Cabildo con el móvil mirando al noreste (45°): la captura no pide permisos ni usa el GPS.</summary>
        public static IUbicacion UbicacionFalsa() => new UbicacionCaptura();

        /// <summary>Ubicación fija para la captura: Activa desde el principio; Iniciar, Detener y AbrirAjustes no hacen nada.</summary>
        class UbicacionCaptura : IUbicacion
        {
            public EstadoUbicacion Estado => EstadoUbicacion.Activa;
            public Coordenada? Posicion => PantallaMapa.VistaInicial;
            public float? Rumbo => 45f;
            public event Action Cambiado { add { } remove { } }
            public void PedirPermiso() { }
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
        }

        public static void SiSePide(GameObject anfitrion, ServicioTema tema, ServicioFiltro filtro, IPreferencias preferencias, MapaVectorial mapa, PantallaMapa pantalla)
        {
            var carpetaPedida = CarpetaPedida();
            if (carpetaPedida == null) return;
            var c = anfitrion.AddComponent<CapturaPruebas>();
            c.carpeta = carpetaPedida;
            c.tema = tema;
            c.filtro = filtro;
            c.preferencias = preferencias;
            c.mapa = mapa;
            c.pantalla = pantalla;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(carpeta);
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            yield return new WaitForSeconds(8f); // teselas descargadas y construidas
            Diagnostico();
            Ubicacion();
            var pildora = pantalla != null ? pantalla.Pildora : null;
            Debug.Log($"PriceMap: filtro al arrancar {filtro.Actual}; píldora \"{pildora?.Texto.text ?? "-"}\"; " +
                      $"marcadores visibles {Visibles()}");
            if (filtro.Actual == FiltroPrecios.Ninguno && Visibles() > 0)
                Debug.LogError("PriceMap: con el filtro Ninguno no debería verse ningún marcador.");
            yield return Capturar(tema.Actual.ToString().ToLowerInvariant());
            tema.Alternar();
            yield return new WaitForSeconds(1.5f);
            yield return Capturar(tema.Actual.ToString().ToLowerInvariant());
            tema.Alternar();
            // Deja la preferencia como estaba: si no había ninguna, la app debe seguir al sistema
            if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
            else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            yield return new WaitForSeconds(1f); // CaptureScreenshot escribe el fichero al acabar el fotograma

            yield return Marcadores();
            yield return FiltroAbierto();
            yield return Puntos();
            yield return Posicion();
            yield return Ficha();

            // Recorrido: unos 5,5 km al norte y vuelta; las teselas lejanas se liberan (nunca más de 7×7)
            Debug.Log($"PriceMap: teselas cargadas al inicio: {mapa.TeselasCargadas} (cámara a {FindFirstObjectByType<CamaraMapa>().distancia:F0} m, inclinación {FindFirstObjectByType<CamaraMapa>().inclinacion:F0}°)");
            mapa.Centrar(new Coordenada(36.8279, -6.3528));
            yield return new WaitForSeconds(6f);
            Debug.Log($"PriceMap: teselas cargadas a 5,5 km: {mapa.TeselasCargadas}");
            if (mapa.TeselasCargadas > 49) Debug.LogError($"PriceMap: demasiadas teselas cargadas ({mapa.TeselasCargadas} > 7×7)");
            yield return Capturar("lejos");
            mapa.Centrar(new Coordenada(mapa.latitud, mapa.longitud));
            yield return new WaitForSeconds(4f);
            Debug.Log($"PriceMap: teselas cargadas al volver: {mapa.TeselasCargadas}");
            yield return new WaitForSeconds(1f);
            Debug.Log("PriceMap: capturas guardadas en " + carpeta);
            Application.Quit();
        }

        /// <summary>
        /// Captura "marcadores" centrada en Clínica Areas y registra el mayor desvío entre la punta de cada
        /// marcador visible (su worldBound tras la disposición) y la proyección de su coordenada.
        /// </summary>
        IEnumerator Marcadores()
        {
            var capa = pantalla != null ? pantalla.Capa : null;
            if (capa == null) { Debug.LogError("PriceMap: no hay capa de marcadores que capturar."); yield break; }
            // Con Ninguno (por defecto) no hay marcadores: elige Todos para la captura y luego deja la preferencia como estaba
            var filtroGuardado = preferencias.Leer(ServicioFiltro.ClavePreferencia);
            var filtroAntes = filtro.Actual;
            filtro.Elegir(FiltroPrecios.Todos);
            try
            {
                var e = MarcadoresConTodos(capa);
                while (e.MoveNext()) yield return e.Current;
            }
            finally
            {
                // Vuelve al filtro de antes en la app y deja la preferencia tal cual estaba (también "sin clave")
                filtro.Elegir(filtroAntes);
                if (filtroGuardado == null) preferencias.Borrar(ServicioFiltro.ClavePreferencia);
                else preferencias.Guardar(ServicioFiltro.ClavePreferencia, filtroGuardado);
                Debug.Log($"PriceMap: preferencia del filtro restaurada ({filtroGuardado ?? "sin guardar"}; antes {filtroAntes}).");
            }
        }

        /// <summary>
        /// Captura "filtro": Hostelería elegida y el menú abierto. Registra el recorrido de TalkBack con el menú
        /// abierto (píldora y las 5 opciones, Hostelería seleccionada) y deja el filtro y la preferencia como estaban.
        /// </summary>
        IEnumerator FiltroAbierto()
        {
            if (pantalla == null || pantalla.Pildora == null) { Debug.LogError("PriceMap: no hay píldora \"Precios\" que capturar."); yield break; }
            var filtroGuardado = preferencias.Leer(ServicioFiltro.ClavePreferencia);
            var filtroAntes = filtro.Actual;
            filtro.Elegir(FiltroPrecios.Hosteleria);
            try
            {
                yield return new WaitForSeconds(1f);
                pantalla.AlternarMenu();
                yield return new WaitForSeconds(0.5f);
                var recorrido = pantalla.Accesibilidad.RecorridoConEstado();
                var texto = new System.Text.StringBuilder();
                foreach (var (etiqueta, seleccionada) in recorrido) texto.Append(" | ").Append(etiqueta).Append(seleccionada ? " (seleccionada)" : "");
                Debug.Log($"PriceMap: recorrido de TalkBack con el menú abierto ({recorrido.Count}):{texto}");

                var esperado = new List<(string, bool)> { (Textos.T(ClavesTexto.PreciosControl), false) };
                foreach (var f in ReglaFiltro.Opciones) esperado.Add((Textos.T(TextosFiltro.ClaveOpcion(f)), f == FiltroPrecios.Hosteleria));
                bool igual = recorrido.Count == esperado.Count;
                for (int i = 0; igual && i < esperado.Count; i++) igual = recorrido[i] == esperado[i];
                if (!igual) Debug.LogError("PriceMap: el recorrido con el menú abierto no es la píldora y las 5 opciones con Hostelería seleccionada.");
                if (!pantalla.MenuAbierto) Debug.LogError("PriceMap: el menú \"Precios\" no está abierto en la captura.");
                var anuncio = PantallaMapa.AnuncioFiltro(FiltroPrecios.Hosteleria);
                Debug.Log($"PriceMap: anuncio al elegir Hostelería: \"{anuncio}\"");
                if (anuncio.Contains(ClavesTexto.PreciosAnuncio)) Debug.LogError("PriceMap: falta el texto del anuncio del filtro en la tabla.");
                yield return Capturar("filtro");
                yield return new WaitForSeconds(1f);

                pantalla.CerrarMenu();
                Debug.Log($"PriceMap: menú cerrado; recorrido de TalkBack con {pantalla.Accesibilidad.Recorrido().Count} entradas (vuelven botones y marcadores).");
            }
            finally
            {
                if (pantalla.MenuAbierto) pantalla.CerrarMenu();
                filtro.Elegir(filtroAntes);
                if (filtroGuardado == null) preferencias.Borrar(ServicioFiltro.ClavePreferencia);
                else preferencias.Guardar(ServicioFiltro.ClavePreferencia, filtroGuardado);
                Debug.Log($"PriceMap: preferencia del filtro restaurada tras la captura del menú ({filtroGuardado ?? "sin guardar"}; antes {filtroAntes}).");
            }
        }

        /// <summary>
        /// Captura "puntos": el centro de Sanlúcar con los puntos de interés y los marcadores (filtro Todos).
        /// Registra los puntos visibles por categoría y comprueba que no hay paradas de autobús ni oficinas,
        /// cuántos se ocultan por repetir un lugar visible y por solape, y que ningún par visible se pisa.
        /// </summary>
        IEnumerator Puntos()
        {
            var capaPuntos = pantalla != null ? pantalla.CapaPuntos : null;
            if (capaPuntos == null) { Debug.LogError("PriceMap: no hay capa de puntos de interés que capturar."); yield break; }
            var filtroGuardado = preferencias.Leer(ServicioFiltro.ClavePreferencia);
            var filtroAntes = filtro.Actual;
            filtro.Elegir(FiltroPrecios.Todos);
            try
            {
                mapa.Centrar(PantallaMapa.VistaInicial);
                yield return new WaitForSeconds(3f);
                yield return new WaitForEndOfFrame();
                var porCategoria = new SortedDictionary<string, int>();
                int visibles = 0, prohibidos = 0, sanidad = 0;
                foreach (var v in capaPuntos.Vistas)
                {
                    if (!v.Visible) continue;
                    visibles++;
                    var c = v.Categoria.ToString();
                    porCategoria[c] = porCategoria.TryGetValue(c, out var n) ? n + 1 : 1;
                    if (v.Punto.Subclase == "bus_stop" || v.Punto.Subclase == "office") prohibidos++;
                    if (v.SanidadPublica) sanidad++;
                }
                var texto = new System.Text.StringBuilder();
                foreach (var par in porCategoria) texto.Append(' ').Append(par.Key).Append('=').Append(par.Value);
                Debug.Log($"PriceMap: puntos de interés en las teselas {mapa.PuntosInteres.Count}, con categoría {capaPuntos.Vistas.Count}, " +
                          $"visibles {visibles} (cámara a {mapa.DistanciaCamara:F0} m); sanidad pública visibles {sanidad}; por categoría:{texto}");
                if (visibles == 0) Debug.LogError("PriceMap: ningún punto de interés visible en la captura de puntos.");
                if (prohibidos > 0) Debug.LogError($"PriceMap: {prohibidos} paradas de autobús u oficinas visibles.");

                // Repetidos y solapes (parte 2): con las cajas reales tras la disposición (círculo y nombre)
                var cajas = new List<(Rect caja, string nombre)>();
                foreach (var v in capaPuntos.Vistas)
                {
                    if (!v.Visible) continue;
                    var caja = v.Icono.worldBound;
                    if (v.Nombre != null && v.Nombre.worldBound.width > 0f)
                        caja = Rect.MinMaxRect(Mathf.Min(caja.xMin, v.Nombre.worldBound.xMin), Mathf.Min(caja.yMin, v.Nombre.worldBound.yMin),
                                               Mathf.Max(caja.xMax, v.Nombre.worldBound.xMax), Mathf.Max(caja.yMax, v.Nombre.worldBound.yMax));
                    if (float.IsNaN(caja.x)) continue;
                    // Medio dp de tolerancia: bordes que solo se tocan por redondeo no cuentan como solape
                    cajas.Add((Rect.MinMaxRect(caja.xMin + 0.25f, caja.yMin + 0.25f, caja.xMax - 0.25f, caja.yMax - 0.25f), v.Punto.Nombre));
                }
                int pares = 0; string ejemplo = "-";
                for (int i = 0; i < cajas.Count; i++)
                    for (int j = i + 1; j < cajas.Count; j++)
                        if (cajas[i].caja.Overlaps(cajas[j].caja)) { pares++; if (pares == 1) ejemplo = $"\"{cajas[i].nombre}\" y \"{cajas[j].nombre}\""; }
                int repetidosVisibles = 0;
                var lugares = new List<Lugar>();
                foreach (var m in pantalla.Capa.Marcadores) if (m.Visible) lugares.Add(m.Lugar);
                foreach (var v in capaPuntos.Vistas)
                    if (v.Visible && ReglaPuntosInteres.EsRepetido(v.Punto.Nombre, v.Punto.Posicion, lugares)) repetidosVisibles++;
                Debug.Log($"PriceMap: puntos ocultos por repetir un lugar visible {capaPuntos.OcultosPorRepetido}, por solape {capaPuntos.OcultosPorSolape}; " +
                          $"pares de puntos visibles que se solapan {pares} (medidos {cajas.Count}; primero: {ejemplo}); " +
                          $"puntos visibles que repiten un lugar visible {repetidosVisibles}");
                if (pares > 0) Debug.LogError($"PriceMap: {pares} pares de puntos de interés visibles se solapan.");
                if (repetidosVisibles > 0) Debug.LogError($"PriceMap: {repetidosVisibles} puntos salen a la vez como lugar y como punto.");
                if (capaPuntos.OcultosPorSolape == 0) Debug.LogError("PriceMap: ningún punto oculto por solape en el centro de Sanlúcar.");
                RecorridoDePuntos(capaPuntos);
                yield return Capturar("puntos");
                yield return new WaitForSeconds(1f);
            }
            finally
            {
                filtro.Elegir(filtroAntes);
                if (filtroGuardado == null) preferencias.Borrar(ServicioFiltro.ClavePreferencia);
                else preferencias.Guardar(ServicioFiltro.ClavePreferencia, filtroGuardado);
            }
        }

        /// <summary>
        /// Historia 1.6 (parte 3): registra el recorrido de TalkBack de la captura "puntos" (cuántos lugares y
        /// cuántos puntos, y los tres primeros puntos) y comprueba que ningún punto va antes que un lugar, que hay
        /// uno por cada vista visible y que los que tienen nombre se leen "nombre, categoría".
        /// </summary>
        void RecorridoDePuntos(CapaPuntosInteres capaPuntos)
        {
            var marcas = pantalla.Accesibilidad.RecorridoDeMarcas();
            int lugares = 0, puntos = 0, desordenados = 0;
            bool hayPunto = false;
            var primeros = new List<string>();
            var etiquetas = new HashSet<string>();
            foreach (var (etiqueta, grupo) in marcas)
            {
                if (grupo == AccesibilidadPantalla.GrupoPuntos)
                {
                    puntos++;
                    hayPunto = true;
                    etiquetas.Add(etiqueta);
                    if (primeros.Count < 3) primeros.Add("\"" + etiqueta + "\"");
                }
                else
                {
                    lugares++;
                    if (hayPunto) desordenados++;
                }
            }
            Debug.Log($"PriceMap: recorrido de TalkBack en la captura de puntos: {pantalla.Accesibilidad.Recorrido().Count} entradas, " +
                      $"{lugares} lugares y {puntos} puntos; primeros puntos: {(primeros.Count > 0 ? string.Join(", ", primeros) : "-")}");
            if (desordenados > 0) Debug.LogError($"PriceMap: {desordenados} lugares van después de algún punto en el recorrido de TalkBack.");
            if (lugares != Visibles()) Debug.LogError($"PriceMap: {lugares} lugares en el recorrido y {Visibles()} marcadores visibles.");
            if (puntos != capaPuntos.Visibles) Debug.LogError($"PriceMap: {puntos} puntos en el recorrido y {capaPuntos.Visibles} visibles.");
            if (puntos == 0) Debug.LogError("PriceMap: ningún punto de interés en el recorrido de TalkBack.");

            // Cada punto visible está en el recorrido con su texto, y la categoría no es una clave sin traducir
            int malLeidos = 0; string ejemplo = "-";
            foreach (var v in capaPuntos.Vistas)
            {
                if (!v.Visible) continue;
                var categoria = Textos.T(v.SanidadPublica ? ClavesTexto.PoiSanidadPublica : TextosPuntos.ClaveCategoria(v.Categoria));
                var esperado = TextosPuntos.Anuncio(v);
                if (etiquetas.Contains(esperado) && !categoria.StartsWith("poi.")) continue;
                malLeidos++;
                if (malLeidos == 1) ejemplo = $"\"{v.Punto.Nombre}\" ({v.Categoria}) se esperaba \"{esperado}\" con la categoría \"{categoria}\"";
            }
            if (malLeidos > 0) Debug.LogError($"PriceMap: {malLeidos} puntos no se leen como \"nombre, categoría\" o solo la categoría; primero: {ejemplo}");
        }

        /// <summary>
        /// Historia 1.7: el botón de ubicación está en la pila derecha, debajo del de tema; con la ubicación falsa
        /// (Plaza del Cabildo, rumbo 45°) se ve el círculo con el cono y no sale la explicación del permiso.
        /// </summary>
        void Ubicacion()
        {
            var boton = pantalla != null ? pantalla.BotonUbicacion : null;
            var pila = boton?.parent?.parent;
            var tema = pantalla != null ? pantalla.BotonTema : null;
            bool enPila = pila != null && pila.name == "pila-derecha";
            bool debajo = enPila && tema != null && boton.worldBound.yMin >= tema.worldBound.yMax;
            var marcador = pantalla != null ? pantalla.MarcadorPosicion : null;
            Debug.Log($"PriceMap: botón de ubicación {(boton != null ? boton.worldBound.ToString() : "-")} en la pila derecha {enPila}, debajo del de tema {debajo}; " +
                      $"círculo visible {marcador?.Visible ?? false}, cono {marcador?.ConoVisible ?? false} a {marcador?.AnguloCono ?? 0f:F1}° (giro del mapa {mapa.Giro:F1}°); " +
                      $"explicación abierta {pantalla?.ExplicacionAbierta ?? false}");
            if (!enPila) Debug.LogError("PriceMap: el botón de ubicación no está en la pila derecha.");
            else if (!debajo) Debug.LogError("PriceMap: el botón de ubicación no está debajo del de tema.");
            if (marcador == null || !marcador.Visible || !marcador.ConoVisible) Debug.LogError("PriceMap: no se ve el círculo de la posición con su cono.");
            if (pantalla != null && pantalla.ExplicacionAbierta) Debug.LogError("PriceMap: con la ubicación falsa no debería salir la explicación del permiso.");
        }

        /// <summary>
        /// Historia 1.7 (parte 2), captura "posicion": con el mapa lejos, el botón centra en el usuario (Plaza del
        /// Cabildo, rumbo 45°) y se ven el círculo y el cono en el centro. Después, el segundo toque orienta al norte.
        /// Registra la etiqueta de TalkBack del botón en cada paso.
        /// </summary>
        IEnumerator Posicion()
        {
            if (pantalla == null || pantalla.BotonUbicacion == null) { Debug.LogError("PriceMap: no hay botón de ubicación que capturar."); yield break; }
            mapa.Centrar(new Coordenada(36.7830, -6.3528)); // unos 570 m al norte: el usuario no está centrado
            yield return new WaitForSeconds(1f);
            var antes = pantalla.AccionUbicacionActual();
            Debug.Log($"PriceMap: botón de ubicación lejos del usuario: {antes} (\"{Textos.T(pantalla.ClaveBotonUbicacion())}\")");
            if (antes != AccionUbicacion.Centrar) Debug.LogError("PriceMap: lejos del usuario, el botón de ubicación debería centrar.");
            pantalla.TocarUbicacion();
            yield return new WaitForSeconds(3f);
            yield return new WaitForEndOfFrame();

            var marcador = pantalla.MarcadorPosicion;
            float escala = pantalla.EscalaDp;
            var centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f) * escala;
            float desvio = marcador != null ? Vector2.Distance(marcador.Posicion, centroPantalla) : float.PositiveInfinity;
            var despues = pantalla.AccionUbicacionActual();
            Debug.Log($"PriceMap: captura de posición: círculo visible {marcador?.Visible ?? false} a {desvio:F1} dp del centro, " +
                      $"cono {marcador?.ConoVisible ?? false} a {marcador?.AnguloCono ?? 0f:F1}° (giro del mapa {mapa.Giro:F1}°); " +
                      $"distancia al centro {pantalla.DistanciaAlCentroDp():F1} dp; botón: {despues} (\"{Textos.T(pantalla.ClaveBotonUbicacion())}\")");
            if (marcador == null || !marcador.Visible || !marcador.ConoVisible) Debug.LogError("PriceMap: no se ve el círculo de la posición con su cono.");
            else if (desvio >= ServicioUbicacion.UmbralCentradoDp) Debug.LogError($"PriceMap: el círculo no está centrado ({desvio:F1} dp).");
            if (despues != AccionUbicacion.OrientarNorte) Debug.LogError("PriceMap: centrado y con el mapa girado, el botón debería orientar al norte.");
            yield return Capturar("posicion");
            yield return new WaitForSeconds(1f);

            pantalla.TocarUbicacion(); // segundo toque: norte arriba
            yield return null;
            Debug.Log($"PriceMap: segundo toque: giro del mapa {mapa.Giro:F1}°; botón: {pantalla.AccionUbicacionActual()} (\"{Textos.T(pantalla.ClaveBotonUbicacion())}\")");
            if (!ServicioUbicacion.AlNorte(mapa.Giro)) Debug.LogError("PriceMap: el segundo toque no ha orientado el mapa al norte.");
        }

        /// <summary>
        /// Historia 1.8 (parte 1), captura "ficha": con Todos, el mapa centrado en Clínica Areas y un toque (simulado
        /// en la punta de su marcador llamando directamente a PantallaMapa.TocarMapa, sin pasar por CamaraMapa) abre su ficha. Registra áreas y filas
        /// y comprueba "desde 40 €", "Consultar", el origen y la frescura; después el botón atrás la cierra. Deja el
        /// filtro y la preferencia como estaban.
        /// </summary>
        IEnumerator Ficha()
        {
            var capa = pantalla != null ? pantalla.Capa : null;
            if (capa == null || pantalla.Ficha == null) { Debug.LogError("PriceMap: no hay ficha que capturar."); yield break; }
            MarcadorLugar areas = null;
            foreach (var m in capa.Marcadores) if (m.Lugar.Id == "clinica-areas") areas = m;
            if (areas == null) { Debug.LogError("PriceMap: falta el marcador de Clínica Areas."); yield break; }
            var filtroGuardado = preferencias.Leer(ServicioFiltro.ClavePreferencia);
            var filtroAntes = filtro.Actual;
            filtro.Elegir(FiltroPrecios.Todos);
            try
            {
                mapa.Centrar(areas.Lugar.Posicion);
                yield return new WaitForSeconds(2f);
                yield return new WaitForEndOfFrame();
                var punta = mapa.Proyectar(areas.Lugar.Posicion);
                bool abierta = pantalla.TocarMapa(punta, pantalla.EscalaDp);
                var ficha = pantalla.Ficha;
                Debug.Log($"PriceMap: toque en Clínica Areas ({punta}): ficha abierta {abierta} ({ficha.Lugar?.Nombre ?? "-"}); " +
                          $"tipo \"{ficha.Tipo.text}\"; dirección \"{ficha.Direccion.text}\"; áreas: {string.Join(", ", ficha.Areas.Select(a => a.text))}");
                foreach (var f in ficha.Filas)
                    Debug.Log($"PriceMap:   {f.Concepto.Area} | {f.Nombre.text} | {f.Precio.text}{(f.Unidad != null ? " " + f.Unidad.text : "")} | {f.Origen.text} | {f.Frescura?.text ?? "-"}");
                if (!abierta || ficha.Lugar?.Id != "clinica-areas") Debug.LogError("PriceMap: el toque en Clínica Areas no ha abierto su ficha.");
                else
                {
                    var sesion = ficha.Fila("fisioterapia/sesion-de-fisioterapia");
                    var infiltraciones = ficha.Fila("traumatologia/infiltraciones");
                    if (sesion == null || sesion.Precio.text != "desde " + FormatoPrecio.Euros(4000)) Debug.LogError("PriceMap: falta \"Sesión de fisioterapia · desde 40 €\".");
                    else if (sesion.Frescura == null) Debug.LogError("PriceMap: la sesión de fisioterapia no tiene punto de frescura.");
                    if (infiltraciones == null || infiltraciones.Precio.text != Textos.T(ClavesTexto.FichaConsultar)) Debug.LogError("PriceMap: falta \"Infiltraciones · Consultar\".");
                    if (sesion != null && !sesion.Origen.text.Contains("13/11/2026")) Debug.LogError("PriceMap: el origen no lleva la fecha de la web.");
                }
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                Debug.Log($"PriceMap: hoja de la ficha {ficha.Hoja.worldBound} (panel {ficha.worldBound}); contenido {ficha.Contenido.worldBound}");
                yield return Capturar("ficha");
                yield return new WaitForSeconds(1f);
                pantalla.Atras();
                yield return null;
                Debug.Log($"PriceMap: tras el botón atrás, ficha abierta {pantalla.FichaAbierta}");
                if (pantalla.FichaAbierta) Debug.LogError("PriceMap: el botón atrás no ha cerrado la ficha.");
            }
            finally
            {
                pantalla.CerrarFicha();
                filtro.Elegir(filtroAntes);
                if (filtroGuardado == null) preferencias.Borrar(ServicioFiltro.ClavePreferencia);
                else preferencias.Guardar(ServicioFiltro.ClavePreferencia, filtroGuardado);
            }
        }

        int Visibles()
        {
            int n = 0;
            var capa = pantalla != null ? pantalla.Capa : null;
            if (capa != null) foreach (var m in capa.Marcadores) if (m.Visible) n++;
            return n;
        }

        IEnumerator MarcadoresConTodos(CapaMarcadores capa)
        {
            MarcadorLugar areas = null;
            foreach (var m in capa.Marcadores) if (m.Lugar.Id == "clinica-areas") areas = m;
            if (areas == null) { Debug.LogError("PriceMap: falta el marcador de Clínica Areas."); yield break; }

            mapa.Centrar(areas.Lugar.Posicion);
            yield return new WaitForSeconds(3f);
            // Al final del fotograma: la disposición ya está hecha y la cámara no se mueve hasta el siguiente Update
            yield return new WaitForEndOfFrame();
            float escala = pantalla.EscalaDp;
            float mayor = 0f; int medidos = 0; string peor = "-";
            foreach (var m in capa.Marcadores)
            {
                if (!m.Visible) continue;
                var b = m.Punta.worldBound;
                if (float.IsNaN(b.x) || escala <= 0f) continue;
                var puntaPx = new Vector2(b.center.x, b.yMax) / escala;
                var p = mapa.Proyectar(m.Lugar.Posicion);
                float desvio = Vector2.Distance(puntaPx, new Vector2(p.X, p.Y));
                medidos++;
                if (desvio > mayor) { mayor = desvio; peor = m.Lugar.Id; }
            }
            Debug.Log($"PriceMap: marcadores visibles {medidos} de {capa.Marcadores.Count}; Clínica Areas con € = {areas.Insignia != null}; " +
                      $"mayor desvío punta-proyección {mayor:F3} px ({peor})");
            if (medidos == 0) Debug.LogError("PriceMap: ningún marcador visible en la captura de marcadores.");
            else if (mayor >= 1f) Debug.LogError($"PriceMap: desvío de los marcadores {mayor:F3} px ≥ 1 px");
            yield return Capturar("marcadores");
            yield return new WaitForSeconds(1f);
        }

        /// <summary>Escribe en el registro dónde está cada elemento de la interfaz, con su texto y su fuente.</summary>
        static void Diagnostico()
        {
            foreach (var doc in FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None))
            {
                var raiz = doc.rootVisualElement;
                if (raiz == null) continue;
                Debug.Log($"PriceMap: panel {raiz.layout} pantalla {Screen.width}x{Screen.height} dpi {Screen.dpi}");
                raiz.Query<UnityEngine.UIElements.VisualElement>().ForEach(e =>
                {
                    if (e.GetFirstAncestorOfType<MarcadorLugar>() != null) return; // 124 marcadores: solo su raíz
                    if (e is PuntoInteresVista || e.GetFirstAncestorOfType<PuntoInteresVista>() != null) return; // cientos de puntos
                    var t = e as UnityEngine.UIElements.TextElement;
                    Debug.Log($"PriceMap:   {e.name} [{string.Join(" ", e.GetClasses())}] {e.worldBound} display={e.resolvedStyle.display}" +
                              (t != null ? $" texto=\"{t.text}\" tamaño={t.resolvedStyle.fontSize} fuente={t.resolvedStyle.unityFont?.name ?? "-"} def={t.resolvedStyle.unityFontDefinition.fontAsset?.name ?? "-"} color={t.resolvedStyle.color}" : ""));
                });
            }
        }

        IEnumerator Capturar(string nombre)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, nombre + ".png"));
            yield return null;
        }
    }
}
