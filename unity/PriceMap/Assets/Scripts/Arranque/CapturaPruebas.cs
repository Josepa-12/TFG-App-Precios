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
    /// En ese modo el panel mide 412 dp de ancho, como un móvil, para comparar con el prototipo.
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
        public static IUbicacion UbicacionFalsa() => ubicacionCaptura = new UbicacionCaptura();

        static UbicacionCaptura ubicacionCaptura;

        /// <summary>
        /// Ubicación para la captura: Activa desde el principio, en la Plaza del Cabildo salvo cuando la captura
        /// "navegacion" la mueve (Mover); Iniciar, Detener y AbrirAjustes no hacen nada.
        /// </summary>
        class UbicacionCaptura : IUbicacion
        {
            Coordenada actual = PantallaMapa.VistaInicial;
            public EstadoUbicacion Estado => EstadoUbicacion.Activa;
            public Coordenada? Posicion => actual;
            public float? Rumbo => 45f;
            public event Action Cambiado;
            public void Mover(Coordenada c) { actual = c; Cambiado?.Invoke(); }
            public void PedirPermiso() { }
            public void Iniciar() { }
            public void Detener() { }
            public void AbrirAjustes() { }
            public bool RutaActiva => false;
            public bool RutaSoloPrimerPlano => false;
            public Resultado<bool> IniciarRuta(string textoNotificacion) => Resultado<bool>.Exito(false);
            public void ActualizarNotificacion(string textoNotificacion) { }
            public void TerminarRuta() { }
        }

        /// <summary>
        /// Rutas fijas para la captura (historia 2.6): 4 min en coche y 12 a pie, como la maqueta, sin red. De la Plaza
        /// del Cabildo a Clínica Areas, por las calles de Sanlúcar con las maniobras en castellano (la forma y los
        /// textos de una respuesta real de Valhalla del 20/11/2026, guardados aquí); a cualquier otro sitio, una recta.
        /// </summary>
        public static IRutas RutasFalsas() => new RutasCaptura();

        /// <summary>Clínica Areas (datos base): destino de las rutas por calles de la captura.</summary>
        static readonly Coordenada ClinicaAreas = new Coordenada(36.780143, -6.35088);

        static Coordenada C(double lat, double lon) => new Coordenada(lat, lon);

        // Valhalla, coche, "shortest": Calle Zarate, Regina, Santa Ana, Ancha, Las Cruces, Diego Benítez y Mar
        static readonly Coordenada[] CalleCoche =
        {
            C(36.777913, -6.352773), C(36.778103, -6.352915), C(36.778279, -6.352559), C(36.778455, -6.352203),
            C(36.778784, -6.351581), C(36.779015, -6.351277), C(36.779645, -6.351707), C(36.779638, -6.351752),
            C(36.779445, -6.352160), C(36.779715, -6.352402), C(36.779977, -6.351978), C(36.780226, -6.351608),
            C(36.780568, -6.351110), C(36.780269, -6.350727),
        };

        static readonly (TipoManiobra tipo, string texto, int punto, double metros)[] ManiobrasCoche =
        {
            (TipoManiobra.Salida, "Conduzca hacia el noroeste por Calle Zarate.", 0, 24),
            (TipoManiobra.Derecha, "Gire a la derecha hacia Calle Regina.", 1, 178),
            (TipoManiobra.Izquierda, "Gire a la izquierda hacia Calle Santa Ana.", 5, 80),
            (TipoManiobra.Izquierda, "Gire a la izquierda hacia Calle Ancha.", 6, 46),
            (TipoManiobra.Derecha, "Gire a la derecha hacia Calle Las Cruces.", 8, 37),
            (TipoManiobra.Derecha, "Gire a la derecha hacia Calle Diego Benítez.", 9, 149),
            (TipoManiobra.Derecha, "Gire a la derecha hacia Calle Mar.", 12, 47),
            (TipoManiobra.Llegada, "Su destino está a la derecha.", 13, 0),
        };

        // Valhalla, a pie: Calle Zarate, Regina, Carril de San Diego y Santo Domingo
        static readonly Coordenada[] CallePie =
        {
            C(36.777913, -6.352773), C(36.778103, -6.352915), C(36.778279, -6.352559), C(36.778455, -6.352203),
            C(36.778784, -6.351581), C(36.779015, -6.351277), C(36.779246, -6.350842), C(36.779847, -6.351248),
            C(36.780029, -6.350940), C(36.780106, -6.350837),
        };

        static readonly (TipoManiobra tipo, string texto, int punto, double metros)[] ManiobrasPie =
        {
            (TipoManiobra.Salida, "Camine hacia el noroeste por Calle Zarate.", 0, 24),
            (TipoManiobra.Derecha, "Gire a la derecha hacia Calle Regina.", 1, 224),
            (TipoManiobra.Izquierda, "Gire a la izquierda hacia Calle Carril de San Diego.", 6, 76),
            (TipoManiobra.Derecha, "Gire a la derecha hacia Calle Santo Domingo.", 7, 46),
            (TipoManiobra.Llegada, "Su destino está a la izquierda.", 9, 0),
        };

        /// <summary>Solo en la captura "ruta-aproximada": las rutas falsas fallan como sin conexión.</summary>
        static bool sinConexion;

        class RutasCaptura : IRutas
        {
            public System.Threading.Tasks.Task<Resultado<Ruta>> Calcular(Coordenada origen, Coordenada destino, ModoViaje modo,
                System.Threading.CancellationToken ct)
            {
                if (sinConexion)
                    return System.Threading.Tasks.Task.FromResult(Resultado<Ruta>.Fallo("Sin conexión (captura)."));
                double segundos = modo == ModoViaje.APie ? 690 : 230;
                Ruta ruta;
                if (destino.DistanciaMetros(ClinicaAreas) < 5 && origen.DistanciaMetros(CalleCoche[0]) < 60)
                {
                    var puntos = modo == ModoViaje.APie ? CallePie : CalleCoche;
                    var lista = modo == ModoViaje.APie ? ManiobrasPie : ManiobrasCoche;
                    var maniobras = lista.Select(m => new Maniobra(m.tipo, m.texto, puntos[m.punto], m.metros)).ToArray();
                    ruta = new Ruta(puntos, lista.Sum(m => m.metros), segundos, maniobras);
                }
                else ruta = new Ruta(new[] { origen, destino }, origen.DistanciaMetros(destino), segundos, Array.Empty<Maniobra>());
                return System.Threading.Tasks.Task.FromResult(Resultado<Ruta>.Exito(ruta));
            }
        }

        /// <summary>Ancho del panel en la captura: el de referencia de EXPERIENCE.md (un móvil de 412 dp).</summary>
        public const float AnchoMovilDp = 412f;

        public static void SiSePide(GameObject anfitrion, ServicioTema tema, ServicioFiltro filtro, IPreferencias preferencias, MapaVectorial mapa, PantallaMapa pantalla)
        {
            var carpetaPedida = CarpetaPedida();
            if (carpetaPedida == null) return;
            AnchoDeMovil(pantalla);
            var c = anfitrion.AddComponent<CapturaPruebas>();
            c.carpeta = carpetaPedida;
            c.tema = tema;
            c.filtro = filtro;
            c.preferencias = preferencias;
            c.mapa = mapa;
            c.pantalla = pantalla;
        }

        /// <summary>
        /// Solo en la captura: el panel pasa a medir 412 dp de ancho, sea cual sea la ventana o el dpi del PC (escala
        /// con el ancho de la pantalla). Cambia el PanelSettings del documento y lo restaura al acabar (OnDestroy), así
        /// que el asset no queda tocado; este modo no existe en la APK. PantallaMapa.EscalaDp (ancho del panel / ancho de la pantalla) se adapta sola.
        /// </summary>
        static void AnchoDeMovil(PantallaMapa pantalla)
        {
            var documento = pantalla != null ? pantalla.GetComponent<UIDocument>() : null;
            var ajustes = documento != null ? documento.panelSettings : null;
            if (ajustes == null) { Debug.LogError("PriceMap: sin PanelSettings, la captura no puede simular el ancho de un móvil."); return; }
            // Asignar una copia al documento con la app ya compuesta cuelga el reproductor (probado): se cambia el
            // compartido y se deja como estaba al acabar (OnDestroy), para que en el editor el asset no quede tocado
            ajustesCambiados = ajustes;
            escalaAntes = (ajustes.scaleMode, ajustes.screenMatchMode, ajustes.match, ajustes.referenceResolution);
            AnchoDeMovil(ajustes, Screen.width, Screen.height);
        }

        static PanelSettings ajustesCambiados;
        static (PanelScaleMode modo, PanelScreenMatchMode ajuste, float match, Vector2Int referencia) escalaAntes;

        /// <summary>Devuelve el PanelSettings compartido a su escala de antes de la captura.</summary>
        void OnDestroy()
        {
            if (ajustesCambiados == null) return;
            ajustesCambiados.scaleMode = escalaAntes.modo;
            ajustesCambiados.screenMatchMode = escalaAntes.ajuste;
            ajustesCambiados.match = escalaAntes.match;
            ajustesCambiados.referenceResolution = escalaAntes.referencia;
            ajustesCambiados = null;
        }

        /// <summary>Escala el panel con el ancho de la pantalla para que mida 412 dp, sea cual sea su dpi.</summary>
        internal static void AnchoDeMovil(PanelSettings ajustes, int anchoPx, int altoPx)
        {
            ajustes.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ajustes.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ajustes.match = 0f; // solo cuenta el ancho
            int alto = anchoPx > 0 ? Mathf.RoundToInt(AnchoMovilDp * altoPx / anchoPx) : 915;
            ajustes.referenceResolution = new Vector2Int((int)AnchoMovilDp, alto);
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(carpeta);
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            yield return new WaitForSeconds(8f); // teselas descargadas y construidas
            float anchoPanel = Screen.width * (pantalla != null ? pantalla.EscalaDp : 0f);
            Debug.Log($"PriceMap: panel de la captura de {anchoPanel:F1} dp de ancho (pantalla {Screen.width}x{Screen.height} px, escala {pantalla?.EscalaDp ?? 0f:F4} dp/px)");
            if (Mathf.Abs(anchoPanel - AnchoMovilDp) > 0.5f) Debug.LogError($"PriceMap: el panel de la captura mide {anchoPanel:F1} dp y no {AnchoMovilDp} dp.");
            Diagnostico();
            Ubicacion();
            var pildora = pantalla != null ? pantalla.Pildora : null;
            Debug.Log($"PriceMap: filtro al arrancar {filtro.Actual}; píldora \"{pildora?.Texto.text ?? "-"}\"; " +
                      $"marcadores visibles {Visibles()}");
            if (filtro.Actual == FiltroPrecios.Ninguno && Visibles() > 0)
                Debug.LogError("PriceMap: con el filtro Ninguno no debería verse ningún marcador.");
            ComprobarSombras();
            yield return Capturar(tema.Actual.ToString().ToLowerInvariant());
            tema.Alternar();
            yield return new WaitForSeconds(1.5f);
            ComprobarSombras();
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
            yield return Variantes();
            yield return Buscador();
            yield return Busqueda();
            yield return Cerca();
            yield return Punto();
            yield return Ruta();
            yield return RutaAproximada();
            yield return Navegacion();
            yield return NavegacionSigue();

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
        /// Ajuste visual de los controles: en EditMode no se resuelven estilos, así que aquí, ya dispuesta la interfaz y
        /// en el tema actual, se comprueba que la sombra de la píldora, del menú y de la columna, y el borde de cristal
        /// de la columna, tienen color (--sombra y --borde-cristal resueltos, alfa mayor que 0).
        /// </summary>
        void ComprobarSombras()
        {
            var columna = pantalla?.BotonTema?.parent?.parent;
            var controles = new (string nombre, VisualElement elemento, bool borde)[]
            {
                ("la píldora", pantalla?.Pildora, false), ("el menú", pantalla?.Menu, false), ("la columna", columna, true),
            };
            foreach (var (nombre, elemento, borde) in controles)
            {
                var sombra = SombraSuave.De(elemento);
                if (sombra == null) { Debug.LogError($"PriceMap: {nombre} no lleva sombra suave."); continue; }
                Debug.Log($"PriceMap: sombra de {nombre} en tema {tema.Actual}: {sombra.Color}" + (borde ? $"; borde de cristal {sombra.Borde}" : ""));
                if (sombra.Color.a <= 0f) Debug.LogError($"PriceMap: la sombra de {nombre} no tiene color en tema {tema.Actual} (--sombra sin resolver).");
                if (borde && (!sombra.ConBorde || sombra.Borde.a <= 0f)) Debug.LogError($"PriceMap: el borde de cristal de {nombre} no tiene color en tema {tema.Actual} (--borde-cristal sin resolver).");
            }
        }

        /// <summary>
        /// Historia 1.7: el botón de ubicación está en la columna de controles, debajo del de tema; con la ubicación
        /// falsa (Plaza del Cabildo, rumbo 45°) se ve el círculo con el cono y no sale la explicación del permiso.
        /// La columna mide 48 × 96 dp.
        /// </summary>
        void Ubicacion()
        {
            var boton = pantalla != null ? pantalla.BotonUbicacion : null;
            var columna = boton?.parent?.parent;
            var tema = pantalla != null ? pantalla.BotonTema : null;
            bool enPila = columna != null && columna.name == "columna-controles" && tema != null && tema.parent?.parent == columna;
            bool debajo = enPila && boton.worldBound.yMin >= tema.worldBound.yMax - 0.5f;
            var marcador = pantalla != null ? pantalla.MarcadorPosicion : null;
            if (columna != null)
            {
                var medida = columna.layout.size;
                Debug.Log($"PriceMap: columna de controles de {medida.x:F1} × {medida.y:F1} dp en {columna.worldBound} (panel)");
                if (Mathf.Abs(medida.x - 48f) > 0.5f || Mathf.Abs(medida.y - 96f) > 0.5f) Debug.LogError("PriceMap: la columna de controles no mide 48 × 96 dp.");
            }
            Debug.Log($"PriceMap: botón de ubicación {(boton != null ? boton.worldBound.ToString() : "-")} en la columna de controles {enPila}, debajo del de tema {debajo}; " +
                      $"círculo visible {marcador?.Visible ?? false}, cono {marcador?.ConoVisible ?? false} a {marcador?.AnguloCono ?? 0f:F1}° (giro del mapa {mapa.Giro:F1}°); " +
                      $"explicación abierta {pantalla?.ExplicacionAbierta ?? false}");
            if (!enPila) Debug.LogError("PriceMap: el botón de ubicación no está en la columna de controles con el de tema.");
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
        /// Historia 2.6: la fila de acciones de la ficha enseña coche (elegido) y a pie con sus minutos e Ir, entre la
        /// cabecera y los conceptos, sin tapar ninguno de los dos.
        /// </summary>
        void ComprobarAcciones(FichaLugar ficha)
        {
            var a = ficha.Acciones;
            var cabecera = ficha.Hoja.Q("ficha-cabecera").worldBound;
            var fila = a.worldBound;
            var contenido = ficha.Contenido.worldBound;
            Debug.Log($"PriceMap: fila de acciones {fila} (visible {ficha.AccionesVisibles}); coche \"{a.Coche.Tiempo.text}\" " +
                      $"{a.Coche.worldBound}, a pie \"{a.APie.Tiempo.text}\" {a.APie.worldBound}, Ir {a.Ir.worldBound}; elegido {a.Elegido}; " +
                      $"TalkBack \"{a.Anuncio(ModoViaje.Coche)}\", \"{a.Anuncio(ModoViaje.APie)}\", \"{a.AnuncioIr}\"");
            if (!ficha.AccionesVisibles || fila.height < 48f) Debug.LogError("PriceMap: la fila de acciones de la ficha no se ve.");
            if (a.Elegido != ModoViaje.Coche || !a.Coche.Elegido || a.APie.Elegido) Debug.LogError("PriceMap: al abrir la ficha, el coche no es el modo elegido.");
            if (a.Coche.Estado != EstadoTiempo.Minutos || a.APie.Estado != EstadoTiempo.Minutos)
                Debug.LogError("PriceMap: la fila de acciones no tiene los minutos de los dos modos.");
            if (a.Coche.worldBound.height < 48f - 0.5f || a.Ir.worldBound.height < 52f - 0.5f)
                Debug.LogError("PriceMap: los modos no llegan a 48 dp o Ir a 52 dp.");
            if (fila.yMin < cabecera.yMax - 0.5f || contenido.yMin < fila.yMax - 0.5f)
                Debug.LogError($"PriceMap: la fila de acciones tapa la cabecera ({cabecera.yMax:F0}) o los conceptos ({contenido.yMin:F0}).");
        }

        /// <summary>
        /// Historia 1.8 (parte 1), captura "ficha": con Todos, el mapa centrado en Clínica Areas y un toque (simulado
        /// en la punta de su marcador llamando directamente a PantallaMapa.TocarMapa, sin pasar por CamaraMapa) abre su ficha. Registra áreas y filas
        /// y comprueba "desde 40 €", que no queda ningún "Consultar", el origen, la frescura y que ningún texto se monta
        /// (SolapesFicha); después el botón atrás la cierra. La repite a 360 dp con la fuente al 130 % (captura
        /// "ficha-estrecha"). Deja el filtro, la preferencia, el ancho y la fuente como estaban.
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
                    if (sesion == null || sesion.Precio.text != "desde " + FormatoPrecio.Euros(4000)) Debug.LogError("PriceMap: falta \"Sesión de fisioterapia · desde 40 €\".");
                    else if (sesion.Frescura == null) Debug.LogError("PriceMap: la sesión de fisioterapia no tiene punto de frescura.");
                    // Los conceptos sin importe se borraron de los datos (18/11/2026)
                    if (ficha.Filas.Any(f => f.Precio.text == Textos.T(ClavesTexto.FichaConsultar))) Debug.LogError("PriceMap: la ficha de Clínica Areas aún tiene conceptos \"Consultar\".");
                    if (sesion != null && !sesion.Origen.text.Contains("13/11/2026")) Debug.LogError("PriceMap: el origen no lleva la fecha de la web.");
                }
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                Debug.Log($"PriceMap: hoja de la ficha {ficha.Hoja.worldBound} (panel {ficha.worldBound}); contenido {ficha.Contenido.worldBound}; " +
                          $"filas {ficha.Contenido.contentContainer.worldBound.height:F0} dp de alto en un viewport de {ficha.Contenido.contentViewport.worldBound.height:F0} dp");
                SolapesFicha($"{AnchoMovilDp:F0} dp");
                ComprobarAcciones(ficha);
                ComprobarCentrado(areas.Lugar);
                // El marcador de la ficha abierta no lo oculta la píldora aunque el centrado lo deje debajo
                Debug.Log($"PriceMap: con la ficha abierta, marcador de Clínica Areas visible {areas.Visible} en {areas.ZonaDp}");
                if (!areas.Visible) Debug.LogError("PriceMap: con su ficha abierta, el marcador de Clínica Areas no se ve.");
                yield return Capturar("ficha");
                yield return new WaitForSeconds(1f);
                pantalla.Atras();
                yield return null;
                Debug.Log($"PriceMap: tras el botón atrás, ficha abierta {pantalla.FichaAbierta}");
                if (pantalla.FichaAbierta) Debug.LogError("PriceMap: el botón atrás no ha cerrado la ficha.");

                // La misma ficha en un móvil estrecho (360 dp) y con la fuente al 130 %
                EscalaFuente.Forzada = () => 1.3f;
                CambiarAnchoPanel(360f);
                yield return new WaitForSeconds(0.5f);
                pantalla.AbrirFicha(areas.Lugar);
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                float anchoPanel = Screen.width * pantalla.EscalaDp;
                Debug.Log($"PriceMap: ficha estrecha en un panel de {anchoPanel:F0} dp con la fuente al 130 % " +
                          $"(título a {ficha.Titulo.resolvedStyle.fontSize:F1} dp)");
                if (Mathf.Abs(anchoPanel - 360f) > 0.5f) Debug.LogError($"PriceMap: la ficha estrecha se midió en un panel de {anchoPanel:F1} dp y no de 360 dp.");
                SolapesFicha("360 dp y fuente al 130 %");
                yield return Capturar("ficha-estrecha");
                yield return new WaitForSeconds(1f);
                // Se cierra la ficha y se vuelve a 412 dp y a la fuente del sistema; después, unos fotogramas para que se
                // disponga: si la siguiente ficha se abre en el mismo fotograma, la hoja aún tiene la medida de esta
                pantalla.CerrarFicha();
                RestaurarAnchoYFuente();
                yield return new WaitForSeconds(0.5f);
            }
            finally
            {
                RestaurarAnchoYFuente();
                pantalla.CerrarFicha();
                filtro.Elegir(filtroAntes);
                if (filtroGuardado == null) preferencias.Borrar(ServicioFiltro.ClavePreferencia);
                else preferencias.Guardar(ServicioFiltro.ClavePreferencia, filtroGuardado);
            }
        }

        /// <summary>Panel de 412 dp y fuente del sistema, como antes de la ficha estrecha.</summary>
        void RestaurarAnchoYFuente()
        {
            if (EscalaFuente.Forzada == null) return;
            EscalaFuente.Forzada = null;
            CambiarAnchoPanel(AnchoMovilDp);
            var raiz = pantalla.GetComponent<UIDocument>()?.rootVisualElement;
            if (raiz != null) EscalaFuente.Aplicar(raiz, EscalaFuente.Sistema); // ficha y puntos creados con el 130 %
            pantalla.CapaPuntos?.OlvidarMedidas();
        }

        /// <summary>Cambia el ancho (dp) del panel de la captura; vuelve con AnchoMovilDp.</summary>
        void CambiarAnchoPanel(float anchoDp)
        {
            if (ajustesCambiados == null) return;
            ajustesCambiados.referenceResolution = new Vector2Int((int)anchoDp,
                Screen.width > 0 ? Mathf.RoundToInt(anchoDp * Screen.height / Screen.width) : 915);
        }

        /// <summary>
        /// Ninguna caja de texto de la ficha abierta pisa a otra (medio dp de tolerancia) y ningún texto necesita más
        /// alto del que tiene su caja (si no, se dibuja fuera de ella, encima de lo de debajo). Da Debug.LogError si falla.
        /// </summary>
        void SolapesFicha(string caso)
        {
            var hoja = pantalla.Ficha.Hoja;
            var textos = new List<TextElement>();
            hoja.Query<TextElement>().ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.text) || t.resolvedStyle.display == DisplayStyle.None) return;
                var b = t.worldBound;
                if (float.IsNaN(b.x) || b.width <= 0f) return;
                textos.Add(t);
            });
            int pares = 0, desbordados = 0;
            string ejemplo = "-";
            for (int i = 0; i < textos.Count; i++)
            {
                var a = textos[i].worldBound;
                var ra = Rect.MinMaxRect(a.xMin + 0.5f, a.yMin + 0.5f, a.xMax - 0.5f, a.yMax - 0.5f);
                for (int j = i + 1; j < textos.Count; j++)
                {
                    if (!ra.Overlaps(textos[j].worldBound)) continue;
                    pares++;
                    if (ejemplo == "-") ejemplo = $"\"{textos[i].text}\" pisa \"{textos[j].text}\"";
                }
                // Alto que necesita el texto con su ancho actual (más 1 dp: medido justo a su ancho, el redondeo parte
                // en dos líneas textos que caben en una)
                var necesita = textos[i].MeasureTextSize(textos[i].text, textos[i].contentRect.width + 1f, VisualElement.MeasureMode.Exactly,
                    0f, VisualElement.MeasureMode.Undefined);
                if (necesita.y > textos[i].contentRect.height + 1f)
                {
                    desbordados++;
                    if (ejemplo == "-") ejemplo = $"\"{textos[i].text}\" necesita {necesita.y:F1} dp y tiene {textos[i].contentRect.height:F1}";
                }
            }
            Debug.Log($"PriceMap: ficha ({caso}): {textos.Count} textos; pares que se pisan {pares}; textos más altos que su caja {desbordados} (primero: {ejemplo})");
            if (textos.Count == 0) Debug.LogError($"PriceMap: ficha ({caso}) sin textos medidos.");
            if (pares > 0 || desbordados > 0) Debug.LogError($"PriceMap: la ficha ({caso}) tiene textos montados: {pares} pares y {desbordados} desbordados.");
        }

        /// <summary>
        /// Historia 1.8 (parte 2): con la ficha abierta, el lugar debe caer en el centro horizontal y a media altura de
        /// la zona del mapa sobre la hoja (±2 px).
        /// </summary>
        void ComprobarCentrado(Lugar lugar) => ComprobarCentrado(lugar.Nombre, lugar.Posicion);

        void ComprobarCentrado(string nombre, Coordenada posicion)
        {
            float escala = pantalla.EscalaDp;
            var hoja = pantalla.Ficha.Hoja.worldBound;
            if (escala <= 0f || float.IsNaN(hoja.yMin)) { Debug.LogError("PriceMap: la hoja de la ficha no tiene medida."); return; }
            var esperado = PantallaMapa.PuntoCentradoFicha(Screen.width, Screen.height, hoja.yMin / escala);
            var p = mapa.Proyectar(posicion);
            float desvio = Vector2.Distance(new Vector2(p.X, p.Y), new Vector2(esperado.X, esperado.Y));
            Debug.Log($"PriceMap: {nombre} con la ficha abierta en {p}; esperado {esperado} (borde de la hoja {hoja.yMin / escala:F0} px); desvío {desvio:F1} px");
            if (!p.Delante || desvio > 2f) Debug.LogError($"PriceMap: {nombre} no queda centrado sobre la hoja (desvío {desvio:F1} px).");
        }

        /// <summary>
        /// Historia 2.5 (parte 1), captura "punto": con el mapa en la calle Ancha, una pulsación larga (llamando a
        /// PantallaMapa.PulsarLargoMapa, sin pasar por CamaraMapa) abre la ficha del punto libre con la chincheta.
        /// Comprueba "Punto en el mapa", que Desproyectar devuelve el punto pulsado, el centrado sobre la hoja y la
        /// chincheta; espera la dirección de Photon hasta 8 s, sin dar error si no hay red (se quedan las
        /// coordenadas). Después el botón atrás cierra la ficha y quita la chincheta.
        /// </summary>
        IEnumerator Punto()
        {
            if (pantalla == null || pantalla.Ficha == null) { Debug.LogError("PriceMap: no hay ficha para el punto libre."); yield break; }
            var calleAncha = new Coordenada(36.7781, -6.3515);
            mapa.Centrar(calleAncha);
            yield return new WaitForSeconds(2f);
            yield return new WaitForEndOfFrame();
            var enPantalla = mapa.Proyectar(calleAncha);
            var desproyectado = mapa.Desproyectar(enPantalla);
            double error = desproyectado.HasValue ? desproyectado.Value.DistanciaMetros(calleAncha) : double.PositiveInfinity;
            Debug.Log($"PriceMap: pulsación larga en {enPantalla}; Desproyectar da {desproyectado?.ToString() ?? "-"} ({error:F2} m del punto)");
            if (error > 1.0) Debug.LogError("PriceMap: Desproyectar no devuelve el punto pulsado.");
            bool abierta = pantalla.PulsarLargoMapa(enPantalla);
            var ficha = pantalla.Ficha;
            Debug.Log($"PriceMap: punto libre abierto {abierta}; título \"{ficha.Titulo.text}\"; subtítulo \"{ficha.Tipo.text}\"; " +
                      $"debajo \"{ficha.Direccion.text}\"; filas {ficha.Filas.Count}");
            if (!abierta || ficha.Punto == null) { Debug.LogError("PriceMap: la pulsación larga no ha abierto la ficha del punto."); yield break; }
            if (ficha.Tipo.text != Textos.T(ClavesTexto.PuntoTipo)) Debug.LogError("PriceMap: la ficha del punto no dice \"Punto en el mapa\".");
            if (ficha.Filas.Count > 0 || ficha.SinPrecio != null) Debug.LogError("PriceMap: la ficha del punto enseña precios.");

            float espera = 0f;
            while (ficha.BuscandoDireccion && espera < 8f) { yield return new WaitForSeconds(0.25f); espera += 0.25f; }
            Debug.Log($"PriceMap: dirección del punto tras {espera:F2} s: título \"{ficha.Titulo.text}\"; debajo \"{ficha.Direccion.text}\"" +
                      (ficha.BuscandoDireccion ? " (Photon no ha contestado: se quedan las coordenadas)" : ""));
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            ComprobarCentrado("El punto libre", calleAncha);
            var chincheta = pantalla.Chincheta;
            Debug.Log($"PriceMap: chincheta visible {chincheta?.Visible ?? false} en {chincheta?.Posicion} dp; gota {chincheta?.Gota.worldBound}");
            if (chincheta == null || !chincheta.Visible) Debug.LogError("PriceMap: con la ficha del punto abierta no se ve la chincheta.");
            SolapesFicha("punto libre");
            yield return Capturar("punto");
            yield return new WaitForSeconds(1f);
            pantalla.Atras();
            yield return null;
            Debug.Log($"PriceMap: tras el botón atrás, ficha abierta {pantalla.FichaAbierta}; chincheta visible {chincheta?.Visible ?? false}");
            if (pantalla.FichaAbierta || (chincheta != null && chincheta.Visible)) Debug.LogError("PriceMap: el botón atrás no ha cerrado el punto libre.");
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Historia 2.6 (parte 2a), capturas "ruta" (claro, coche) y "ruta-oscuro" (a pie): desde la ficha de Clínica
        /// Areas, Ir dibuja la ruta por las calles (RutasFalsas), la encuadra y enseña las dos tarjetas. Comprueba que
        /// la ruta entera cae entre las dos tarjetas y dentro de la pantalla, que no se ven la píldora, el botón de tema ni
        /// la cápsula, y que el botón de ubicación queda encima de la tarjeta de abajo. La segunda reabre la ficha desde la
        /// tarjeta, elige "A pie" e Ir. Al acabar, atrás quita la ruta; deja el tema y la preferencia como estaban.
        /// </summary>
        IEnumerator Ruta()
        {
            var lugar = pantalla?.Capa?.Marcadores.Select(m => m.Lugar).FirstOrDefault(l => l.Id == "clinica-areas");
            if (lugar == null || pantalla.Rutas == null || pantalla.TarjetaRuta == null) { Debug.LogError("PriceMap: no se puede capturar la ruta."); yield break; }
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            var relojAntes = pantalla.RelojLocal;
            pantalla.RelojLocal = () => new DateTime(2026, 11, 20, 13, 30, 0, DateTimeKind.Local);
            try
            {
                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                pantalla.AbrirFicha(lugar);
                yield return new WaitForSeconds(1f);
                pantalla.Ficha.Acciones.PulsarIr();
                yield return new WaitForSeconds(2f);
                yield return new WaitForEndOfFrame();
                ComprobarRuta("coche");
                yield return Capturar("ruta");
                yield return new WaitForSeconds(1f);

                tema.Alternar();
                yield return new WaitForSeconds(1.5f);
                pantalla.TarjetaRuta.Tocar();
                yield return new WaitForSeconds(1f);
                if (!pantalla.FichaAbierta) Debug.LogError("PriceMap: tocar la tarjeta de la ruta no ha reabierto la ficha.");
                pantalla.Ficha.Acciones.Tocar(ModoViaje.APie);
                pantalla.Ficha.Acciones.PulsarIr();
                yield return new WaitForSeconds(2f);
                yield return new WaitForEndOfFrame();
                ComprobarRuta("a pie");
                yield return Capturar("ruta-oscuro");
                yield return new WaitForSeconds(1f);
                pantalla.Atras();
                yield return null;
                Debug.Log($"PriceMap: tras el botón atrás, ruta activa {pantalla.RutaActiva}; cápsula visible {pantalla.Buscador.Visible}");
                if (pantalla.RutaActiva || !pantalla.Buscador.Visible) Debug.LogError("PriceMap: el botón atrás no ha quitado la ruta.");
            }
            finally
            {
                pantalla.QuitarRuta();
                pantalla.RelojLocal = relojAntes;
                if (tema.Actual != Tema.Claro) tema.Alternar();
                if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
                else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            }
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Historia 2.6 (parte 2b), capturas "ruta-aproximada" (claro, coche) y "ruta-aproximada-oscuro" (a pie): las
        /// rutas falsas fallan como sin conexión y la ruta sale de las calles de las teselas ya guardadas
        /// (RutasCallesGuardadas). Comprueba que es aproximada, que va por las calles (más de dos puntos), que las
        /// tarjetas llevan "≈", que el toast se ve bajo la tarjeta de arriba y lo que dice TalkBack. Vacía antes la
        /// caché (si no, saldría la ruta de la captura anterior) y al acabar devuelve la conexión, el tema y la preferencia.
        /// </summary>
        IEnumerator RutaAproximada()
        {
            var lugar = pantalla?.Capa?.Marcadores.Select(m => m.Lugar).FirstOrDefault(l => l.Id == "clinica-areas");
            if (lugar == null || pantalla.Rutas == null || pantalla.TarjetaRuta == null) { Debug.LogError("PriceMap: no se puede capturar la ruta aproximada."); yield break; }
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            var relojAntes = pantalla.RelojLocal;
            pantalla.RelojLocal = () => new DateTime(2026, 11, 20, 13, 30, 0, DateTimeKind.Local);
            sinConexion = true;
            pantalla.Rutas.VaciarCache();
            var anuncios = new List<string>();
            void Anotar(string t) => anuncios.Add(t);
            AccesibilidadPantalla.AnuncioPedido += Anotar;
            try
            {
                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                pantalla.AbrirFicha(lugar);
                yield return new WaitForSeconds(2f);
                var fila = pantalla.Ficha.Acciones;
                Debug.Log($"PriceMap: ficha sin conexión: coche \"{fila.Coche.Tiempo.text}\" ({fila.Anuncio(ModoViaje.Coche)}), a pie \"{fila.APie.Tiempo.text}\" ({fila.Anuncio(ModoViaje.APie)})");
                if (!fila.Coche.Aproximados || !fila.APie.Aproximados) Debug.LogError("PriceMap: sin conexión la ficha no da los minutos aproximados.");
                fila.PulsarIr();
                yield return EsperarRuta();
                ComprobarRuta("aproximada, coche");
                ComprobarAproximada("coche", anuncios);
                yield return Capturar("ruta-aproximada");
                yield return new WaitForSeconds(3.5f); // se va el toast

                tema.Alternar();
                yield return new WaitForSeconds(1.5f);
                pantalla.TarjetaRuta.Tocar();
                yield return new WaitForSeconds(1f);
                pantalla.Ficha.Acciones.Tocar(ModoViaje.APie);
                anuncios.Clear();
                pantalla.Ficha.Acciones.PulsarIr();
                yield return EsperarRuta();
                ComprobarRuta("aproximada, a pie");
                ComprobarAproximada("a pie", anuncios);
                yield return Capturar("ruta-aproximada-oscuro");
                yield return new WaitForSeconds(1f);
            }
            finally
            {
                AccesibilidadPantalla.AnuncioPedido -= Anotar;
                sinConexion = false;
                pantalla.QuitarRuta();
                pantalla.RelojLocal = relojAntes;
                if (tema.Actual != Tema.Claro) tema.Alternar();
                if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
                else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            }
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Historia 2.7 (parte 1), captura "navegacion" (claro, coche): Ir a Clínica Areas desde la Plaza del Cabildo y la
        /// ubicación falsa pasa a la mitad de Calle Regina (entre sus puntos 3 y 4, 4 m a un lado). Comprueba que la línea
        /// empieza en la posición (el punto proyectado), que la tarjeta de arriba da la maniobra siguiente (Calle Santa
        /// Ana) con su distancia ("En … m"), que la X dice "Terminar ruta" y que bajan los minutos. Al acabar, atrás
        /// termina la ruta y la ubicación vuelve al Cabildo.
        /// </summary>
        IEnumerator Navegacion()
        {
            var lugar = pantalla?.Capa?.Marcadores.Select(m => m.Lugar).FirstOrDefault(l => l.Id == "clinica-areas");
            if (lugar == null || pantalla.Navegacion == null || ubicacionCaptura == null) { Debug.LogError("PriceMap: no se puede capturar la navegación."); yield break; }
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            var relojAntes = pantalla.RelojLocal;
            pantalla.RelojLocal = () => new DateTime(2026, 11, 20, 13, 30, 0, DateTimeKind.Local);
            pantalla.Rutas.VaciarCache();
            try
            {
                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                pantalla.AbrirFicha(lugar);
                yield return new WaitForSeconds(1f);
                pantalla.Ficha.Acciones.Tocar(ModoViaje.Coche);
                pantalla.Ficha.Acciones.PulsarIr();
                yield return EsperarRuta();
                var nav = pantalla.Navegacion;
                if (!nav.Activa) { Debug.LogError("PriceMap: Ir no ha empezado la navegación."); yield break; }
                int minutosAntes = nav.Estado.MinutosRestantes;
                // A la mitad de Calle Regina, 4 m al sureste de la línea (que va hacia el noreste)
                var a = CalleCoche[3];
                var b = CalleCoche[4];
                var mitad = new Coordenada((a.Latitud + b.Latitud) / 2 - 0.00003, (a.Longitud + b.Longitud) / 2 + 0.000025);
                ubicacionCaptura.Mover(mitad);
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                var estado = nav.Estado;
                var arriba = pantalla.TarjetaManiobra;
                var abajo = pantalla.TarjetaRuta;
                var linea = nav.Regla.PuntosRestantes(estado);
                Debug.Log($"PriceMap: navegación: posición {mitad}; proyectado {estado.Proyectado} a {estado.DistanciaRuta:F1} m de la ruta; " +
                          $"línea desde {linea[0]} ({linea.Count} puntos); arriba \"{arriba.Distancia.text}\" \"{arriba.Principal.text}\" / \"{arriba.Siguiente.text}\"; " +
                          $"abajo \"{abajo.Llegada.text} · {abajo.Minutos.text} min · {abajo.Distancia.text} {abajo.RotuloDistancia.text}\" ({minutosAntes} min al empezar); " +
                          $"TalkBack \"{arriba.AnuncioConDistancia}\"; X \"{pantalla.Accesibilidad.Recorrido().FirstOrDefault(e => e == Textos.T(ClavesTexto.RutaTerminar)) ?? "-"}\"");
                if (linea[0].DistanciaMetros(estado.Proyectado) > 0.5 || estado.DistanciaRuta > 10)
                    Debug.LogError("PriceMap: la línea de la navegación no empieza en la posición.");
                if (!arriba.Principal.text.Contains("Santa Ana")) Debug.LogError("PriceMap: la tarjeta de arriba no da la maniobra siguiente (Calle Santa Ana).");
                if (!arriba.Distancia.text.StartsWith("En ")) Debug.LogError("PriceMap: la tarjeta de arriba no da la distancia a la maniobra.");
                if (!pantalla.Navegando) Debug.LogError("PriceMap: la pantalla no está navegando.");
                yield return Capturar("navegacion");
                yield return new WaitForSeconds(1f);
                pantalla.Atras();
                yield return null;
                if (nav.Activa || pantalla.RutaActiva) Debug.LogError("PriceMap: el botón atrás no ha terminado la ruta.");
            }
            finally
            {
                ubicacionCaptura.Mover(PantallaMapa.VistaInicial);
                pantalla.QuitarRuta();
                pantalla.RelojLocal = relojAntes;
                if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
                else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            }
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Historia 2.7 (parte 2b), capturas "navegacion-sigue" (claro y oscuro) y "volver-centrar": Ir a Clínica Areas en
        /// coche y la ubicación falsa a la mitad de Calle Regina (4 m a un lado); pasados los 2 s y la transición, la
        /// cámara sigue: inclinada 45°, a 350 m, con el rumbo de la marcha arriba y el punto sobre la línea en el tercio de
        /// abajo. Después, un gesto mueve el mapa y el botón de ubicación pasa al icono de recentrar ("Volver a centrar").
        /// </summary>
        IEnumerator NavegacionSigue()
        {
            var lugar = pantalla?.Capa?.Marcadores.Select(m => m.Lugar).FirstOrDefault(l => l.Id == "clinica-areas");
            var camara = FindFirstObjectByType<CamaraMapa>();
            if (lugar == null || pantalla.Navegacion == null || ubicacionCaptura == null || camara == null)
            { Debug.LogError("PriceMap: no se puede capturar la cámara que sigue."); yield break; }
            // La captura "navegacion" pudo terminar con la cámara ya empezando a inclinarse: la vista de siempre
            camara.inclinacion = 50f;
            camara.giro = -15f;
            camara.distancia = 900f;
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            var relojAntes = pantalla.RelojLocal;
            pantalla.RelojLocal = () => new DateTime(2026, 11, 20, 13, 30, 0, DateTimeKind.Local);
            pantalla.Rutas.VaciarCache();
            try
            {
                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                pantalla.AbrirFicha(lugar);
                yield return new WaitForSeconds(1f);
                pantalla.Ficha.Acciones.Tocar(ModoViaje.Coche);
                pantalla.Ficha.Acciones.PulsarIr();
                yield return EsperarRuta();
                var nav = pantalla.Navegacion;
                if (!nav.Activa) { Debug.LogError("PriceMap: Ir no ha empezado la navegación."); yield break; }
                if (pantalla.EstadoCamara != UI.EstadoSeguimiento.Esperando)
                    Debug.LogError($"PriceMap: al pulsar Ir la cámara debería esperar 2 s con la ruta entera ({pantalla.EstadoCamara}).");
                var a = CalleCoche[3];
                var b = CalleCoche[4];
                var mitad = new Coordenada((a.Latitud + b.Latitud) / 2 - 0.00003, (a.Longitud + b.Longitud) / 2 + 0.000025);
                ubicacionCaptura.Mover(mitad);
                yield return new WaitForSeconds(3.5f); // los 2 s y la transición
                yield return new WaitForEndOfFrame();
                var estado = nav.Estado;
                var visto = pantalla.PosicionMostrada;
                var enPantalla = visto.HasValue ? mapa.Proyectar(visto.Value) : PuntoPantalla.Ninguno;
                var objetivo = pantalla.PuntoSeguir();
                Debug.Log($"PriceMap: cámara que sigue: {pantalla.EstadoCamara}; inclinación {camara.inclinacion:F1}°, distancia {camara.distancia:F0} m, " +
                          $"giro {camara.giro:F0}° (rumbo {pantalla.RumboCamara:F0}°); punto {visto} (proyectado {estado.Proyectado}, a {estado.DistanciaRuta:F1} m) " +
                          $"en ({enPantalla.X:F0}, {enPantalla.Y:F0}) px, objetivo ({objetivo.X:F0}, {objetivo.Y:F0}); botón \"{Textos.T(pantalla.ClaveBotonUbicacion())}\"");
                if (pantalla.EstadoCamara != UI.EstadoSeguimiento.Siguiendo) Debug.LogError("PriceMap: la cámara no sigue a los 2 s.");
                if (Mathf.Abs(camara.inclinacion - ParametrosNegocio.CamaraSeguirInclinacion) > 1f) Debug.LogError("PriceMap: la cámara no está inclinada 45°.");
                if (Mathf.Abs(camara.distancia - ParametrosNegocio.CamaraSeguirDistanciaCoche) > 5f) Debug.LogError("PriceMap: la cámara no está a 350 m.");
                if (!visto.HasValue || visto.Value.DistanciaMetros(estado.Proyectado) > 0.5) Debug.LogError("PriceMap: en coche el punto no va sobre la línea.");
                if (Vector2.Distance(new Vector2(enPantalla.X, enPantalla.Y), new Vector2(objetivo.X, objetivo.Y)) > 10f)
                    Debug.LogError("PriceMap: la posición no está en el tercio de abajo de la zona libre.");
                yield return Capturar("navegacion-sigue");
                tema.Alternar();
                yield return new WaitForSeconds(1.5f);
                yield return Capturar("navegacion-sigue-oscuro");
                tema.Alternar();
                yield return new WaitForSeconds(1.5f);

                // El usuario mueve el mapa: la cámara se suelta y el botón pasa a "Volver a centrar"
                camara.SimularGesto();
                camara.pivote += new Vector3(120f, 0f, 160f);
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                var boton = pantalla.BotonUbicacion;
                Debug.Log($"PriceMap: tras mover el mapa: {pantalla.EstadoCamara}; icono {boton?.Icono}; botón \"{Textos.T(pantalla.ClaveBotonUbicacion())}\"");
                if (pantalla.EstadoCamara != UI.EstadoSeguimiento.Suelto || boton == null || boton.Icono != UI.Icono.Recentrar)
                    Debug.LogError("PriceMap: tras mover el mapa el botón de ubicación no es «Volver a centrar».");
                yield return Capturar("volver-centrar");
                pantalla.TocarUbicacion();
                yield return null;
                if (pantalla.EstadoCamara != UI.EstadoSeguimiento.Siguiendo || boton.Icono != UI.Icono.Navegar)
                    Debug.LogError("PriceMap: «Volver a centrar» no vuelve a seguir.");
                pantalla.Atras();
                yield return null;
                if (nav.Activa || pantalla.EstadoCamara != UI.EstadoSeguimiento.Apagado || boton.Icono != UI.Icono.Navegar)
                    Debug.LogError("PriceMap: al terminar la ruta la cámara y el botón no vuelven a lo de siempre.");
            }
            finally
            {
                ubicacionCaptura.Mover(PantallaMapa.VistaInicial);
                pantalla.QuitarRuta();
                pantalla.RelojLocal = relojAntes;
                if (tema.Actual != Tema.Claro) tema.Alternar();
                if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
                else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            }
            // La cámara se queda inclinada donde estaba al terminar: vuelve a la vista de siempre para lo que sigue
            camara.inclinacion = 50f;
            camara.giro = -15f;
            camara.distancia = 900f;
            mapa.Centrar(PantallaMapa.VistaInicial);
            yield return new WaitForSeconds(1f);
        }

        // Hasta que llega la ruta (o el fallo), como mucho 20 s; luego un fotograma para que se disponga
        IEnumerator EsperarRuta()
        {
            float limite = Time.realtimeSinceStartup + 20f;
            while (pantalla.RutaDibujada == null && pantalla.TarjetaRuta.Minutos.text == Textos.T(ClavesTexto.FichaCalculando) &&
                   Time.realtimeSinceStartup < limite)
                yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
        }

        /// <summary>La ruta es la aproximada, va por las calles, las tarjetas llevan "≈" y el toast se ve bajo la de arriba.</summary>
        void ComprobarAproximada(string caso, List<string> anuncios)
        {
            var ruta = pantalla.RutaDibujada;
            var toast = pantalla.Toast;
            var arriba = pantalla.TarjetaManiobra;
            var abajo = pantalla.TarjetaRuta;
            Debug.Log($"PriceMap: ruta aproximada ({caso}): {ruta?.Puntos.Count ?? 0} puntos, {ruta?.Metros ?? 0:F0} m, {ruta?.Minutos ?? 0} min; " +
                      $"toast \"{toast?.Texto.text}\" visible {toast?.Visible} en {toast?.Texto.worldBound}; anuncios: {string.Join(" | ", anuncios)}");
            if (ruta == null || !ruta.Aproximada) { Debug.LogError($"PriceMap: sin conexión no ha salido la ruta aproximada ({caso})."); return; }
            if (ruta.Puntos.Count <= 2) Debug.LogError($"PriceMap: la ruta aproximada ({caso}) no va por las calles.");
            if (arriba.Principal.text != Textos.T(ClavesTexto.RutaAproximada)) Debug.LogError($"PriceMap: la tarjeta de arriba ({caso}) no dice que la ruta es aproximada.");
            if (!abajo.Llegada.text.StartsWith("≈") || !abajo.Minutos.text.StartsWith("≈") || !abajo.Distancia.text.StartsWith("≈"))
                Debug.LogError($"PriceMap: la tarjeta de abajo ({caso}) no lleva «≈».");
            if (toast == null || !toast.Visible) { Debug.LogError($"PriceMap: no se ve el toast ({caso})."); return; }
            if (toast.Texto.worldBound.yMin < arriba.worldBound.yMax) Debug.LogError($"PriceMap: el toast ({caso}) no queda bajo la tarjeta de arriba.");
            if (!anuncios.Contains(Textos.T(ClavesTexto.ToastRutaAproximada))) Debug.LogError($"PriceMap: TalkBack no anuncia el toast ({caso}).");
        }

        /// <summary>La ruta dibujada cabe entera entre las dos tarjetas y los controles son los de la ruta; Debug.LogError si no.</summary>
        void ComprobarRuta(string caso)
        {
            var ruta = pantalla.RutaDibujada;
            var arriba = pantalla.TarjetaManiobra;
            var abajo = pantalla.TarjetaRuta;
            float escala = pantalla.EscalaDp;
            Debug.Log($"PriceMap: ruta ({caso}): {ruta?.Puntos.Count ?? 0} puntos; arriba \"{arriba.Principal.text}\" / \"{arriba.Siguiente.text}\" {arriba.worldBound}; " +
                      $"abajo \"{abajo.Llegada.text} {abajo.RotuloLlegada.text} · {abajo.Minutos.text} {abajo.RotuloMinutos.text} · {abajo.Distancia.text} {abajo.RotuloDistancia.text}\" " +
                      $"{abajo.worldBound}; TalkBack \"{arriba.Anuncio}\" y \"{abajo.Anuncio}\"; cámara a {FindFirstObjectByType<CamaraMapa>().distancia:F0} m");
            if (ruta == null || ruta.Puntos.Count < 2) { Debug.LogError($"PriceMap: ruta ({caso}) sin dibujar."); return; }
            if (escala <= 0f || abajo.resolvedStyle.display == DisplayStyle.None || arriba.resolvedStyle.display == DisplayStyle.None)
            { Debug.LogError($"PriceMap: ruta ({caso}) sin las dos tarjetas a la vista."); return; }
            float techo = arriba.worldBound.yMax / escala, suelo = abajo.worldBound.yMin / escala;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var c in ruta.Puntos)
            {
                var p = mapa.Proyectar(c);
                x0 = Mathf.Min(x0, p.X); y0 = Mathf.Min(y0, p.Y); x1 = Mathf.Max(x1, p.X); y1 = Mathf.Max(y1, p.Y);
            }
            Debug.Log($"PriceMap: ruta ({caso}) en pantalla x {x0:F0}–{x1:F0}, y {y0:F0}–{y1:F0} px; libre entre y {techo:F0} y {suelo:F0} px (ancho {Screen.width})");
            if (y0 < techo || y1 > suelo || x0 < 0f || x1 > Screen.width) Debug.LogError($"PriceMap: la ruta ({caso}) no cabe entera entre las dos tarjetas.");
            if (pantalla.Pildora != null && pantalla.Pildora.resolvedStyle.display != DisplayStyle.None && pantalla.Pildora.parent.resolvedStyle.display != DisplayStyle.None)
                Debug.LogError($"PriceMap: con ruta ({caso}) se ve la píldora de precios.");
            if (pantalla.BotonTema.parent.resolvedStyle.display != DisplayStyle.None) Debug.LogError($"PriceMap: con ruta ({caso}) se ve el botón de tema.");
            if (pantalla.Buscador.Visible) Debug.LogError($"PriceMap: con ruta ({caso}) se ve la cápsula del buscador.");
            var ubic = pantalla.BotonUbicacion?.worldBound;
            if (ubic.HasValue)
            {
                Debug.Log($"PriceMap: con ruta ({caso}), botón de ubicación en {ubic.Value}");
                if (ubic.Value.yMax > abajo.worldBound.yMin || ubic.Value.xMin > Screen.width * escala / 2f)
                    Debug.LogError($"PriceMap: con ruta ({caso}) el botón de ubicación no queda abajo a la izquierda encima de la tarjeta.");
            }
        }

        /// <summary>
        /// Historia 1.8 (parte 2), captura "variantes": un lugar sintético (los datos reales no tienen variantes) con
        /// una franja que incluye la hora actual, días laborables y "con bono", abierto con PantallaMapa.AbrirFicha.
        /// Comprueba que la franja va destacada con "Ahora" y el centrado sobre la hoja.
        /// </summary>
        IEnumerator Variantes()
        {
            if (pantalla == null || pantalla.Ficha == null) { Debug.LogError("PriceMap: no hay ficha para las variantes."); yield break; }
            // La franja empieza una hora antes: el "ahora" del Arranque es de hace unos segundos
            int hora = DateTime.Now.Hour;
            var franja = CondicionVariante.Horario(TimeSpan.FromHours((hora + 23) % 24), TimeSpan.FromHours((hora + 2) % 24));
            var web = new Func<ValorPrecio, FuentesPrecio>(v =>
                new FuentesPrecio(Plantilla.Ocio, new PrecioWeb(v, "https://ejemplo.es/tarifas", DateTime.UtcNow.Date)));
            var variantes = new[]
            {
                new Variante("pista@franja", franja, web(ValorPrecio.Fijo(2400))),
                new Variante("pista@laborables", CondicionVariante.EnDias(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                    DayOfWeek.Thursday, DayOfWeek.Friday), web(ValorPrecio.Fijo(1800))),
                new Variante("pista@con-bono", CondicionVariante.DeCliente("con-bono"), web(ValorPrecio.Desde(1500))),
            };
            var lugar = new Lugar("padel-captura", "Pádel de prueba", "Club de pádel", Plantilla.Ocio, PantallaMapa.VistaInicial,
                "Lugar sintético de la captura",
                conceptos: new[] { new Concepto("pista", "Pistas", "Pista de pádel", "hora", web(ValorPrecio.Fijo(2000)), variantes) });
            try
            {
                pantalla.AbrirFicha(lugar);
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                var fila = pantalla.Ficha.Filas.FirstOrDefault();
                if (fila == null) { Debug.LogError("PriceMap: la ficha sintética no tiene el concepto."); yield break; }
                foreach (var v in fila.Variantes)
                    Debug.Log($"PriceMap:   variante {v.Variante.Id}: {(v.Vigente ? "[" + v.Chip.text + "] " : "")}{v.Texto}");
                if (fila.Variantes.Count != 3) Debug.LogError($"PriceMap: se esperaban 3 variantes y hay {fila.Variantes.Count}.");
                var vigente = fila.Variantes.Where(v => v.Vigente).ToList();
                if (vigente.Count != 1 || vigente[0].Variante.Id != "pista@franja" || vigente[0].Chip == null)
                    Debug.LogError("PriceMap: la franja de la hora actual no va destacada con \"Ahora\".");
                ComprobarCentrado(lugar);
                SolapesFicha("variantes");
                yield return Capturar("variantes");
                yield return new WaitForSeconds(1f);
            }
            finally
            {
                pantalla.CerrarFicha();
            }
        }

        /// <summary>
        /// Historia 2.2 (parte 1), capturas "buscador" (tema claro) y "buscador-oscuro": la hoja del buscador a media
        /// altura con los chips de categorías y "Encuentra cerca". Antes comprueba la cápsula plegada (56 dp como
        /// mínimo, sin pisar la atribución ni los controles) y, a media altura, que la hoja está donde se dejó, que los
        /// 8 chips se ven dentro de ella y que no pisa la píldora ni la columna de controles. Al final, el botón atrás
        /// la pliega. Deja el tema y su preferencia como estaban.
        /// </summary>
        IEnumerator Buscador()
        {
            var buscador = pantalla != null ? pantalla.Buscador : null;
            if (buscador == null) { Debug.LogError("PriceMap: no hay buscador que capturar."); yield break; }
            var raiz = pantalla.GetComponent<UIDocument>()?.rootVisualElement;
            var columna = raiz?.Q("columna-controles");
            var atribucion = raiz?.Q("atribucion");
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            var temaAntes = tema.Actual;
            try
            {
                mapa.Centrar(PantallaMapa.VistaInicial);
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                var capsula = buscador.worldBound;
                Debug.Log($"PriceMap: cápsula del buscador {capsula} ({buscador.AltoCapsula:F1} dp de alto); atribución {atribucion?.worldBound}; " +
                          $"campo \"{buscador.Campo.textEdition.placeholder}\"");
                if (capsula.height < HojaBuscador.AltoCapsulaMin - 0.5f) Debug.LogError($"PriceMap: la cápsula mide {capsula.height:F1} dp de alto (mínimo 56).");
                SinSolape("la cápsula", capsula, ("la atribución", atribucion), ("la columna de controles", columna), ("la píldora", pantalla.Pildora));

                buscador.Poner(0.5f);
                yield return new WaitForSeconds(1f); // la transición dura 0,32 s
                yield return new WaitForEndOfFrame();
                var hoja = buscador.worldBound;
                float esperado = buscador.Top;
                var viewport = buscador.Panel.contentViewport.worldBound;
                int chipsVisibles = buscador.Chips.Count(c => viewport.Contains(c.worldBound.center));
                Debug.Log($"PriceMap: hoja del buscador a media altura en {hoja} (top esperado {esperado:F1} dp, recorrido {buscador.Fisica.AltoUtil:F1} dp); " +
                          $"chips visibles {chipsVisibles} de {buscador.Chips.Count}; \"Encuentra cerca\" en {buscador.Cerca.worldBound}");
                if (Mathf.Abs(hoja.yMin - esperado) > 1f) Debug.LogError($"PriceMap: la hoja no está donde se dejó ({hoja.yMin:F1} dp y no {esperado:F1}).");
                if (chipsVisibles != HojaBuscador.Categorias.Length) Debug.LogError("PriceMap: a media altura no se ven los 8 chips de categorías.");
                if (!viewport.Contains(buscador.Cerca.worldBound.center)) Debug.LogError("PriceMap: a media altura no se ve \"Encuentra cerca\".");
                SinSolape("la hoja", hoja, ("la columna de controles", columna), ("la píldora", pantalla.Pildora));

                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                yield return Capturar("buscador");
                yield return new WaitForSeconds(1f);
                tema.Alternar();
                yield return new WaitForSeconds(1.5f);
                yield return Capturar("buscador-oscuro");
                yield return new WaitForSeconds(1f);

                pantalla.Atras();
                yield return null;
                Debug.Log($"PriceMap: tras el botón atrás, hoja del buscador en {buscador.Fraccion:F2}");
                if (buscador.Abierta) Debug.LogError("PriceMap: el botón atrás no ha plegado la hoja del buscador.");
            }
            finally
            {
                buscador.Plegar();
                if (tema.Actual != temaAntes) tema.Alternar();
                if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
                else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            }
        }

        /// <summary>
        /// Historia 2.3 (parte 1), capturas "busqueda" (tema claro) y "busqueda-oscuro": la hoja abierta del todo con
        /// "clinica" escrito y sus resultados. Comprueba que el primero es un lugar de la app con "Clínica", que no hay
        /// más de 20, que los chips no se ven y que la X sí; registra qué encuentra "mercadona" con las teselas
        /// cargadas. Al final, atrás pliega la hoja sin borrar el texto y la X lo vacía. Deja el tema como estaba.
        /// </summary>
        IEnumerator Busqueda()
        {
            var buscador = pantalla != null ? pantalla.Buscador : null;
            if (buscador == null) { Debug.LogError("PriceMap: no hay buscador para la búsqueda."); yield break; }
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            var temaAntes = tema.Actual;
            try
            {
                mapa.Centrar(PantallaMapa.VistaInicial);
                yield return new WaitForSeconds(2f); // teselas de la vista inicial cargadas: sus puntos de interés
                if (pantalla.Busqueda == null) { Debug.LogError("PriceMap: la pantalla no ha recibido el ServicioBusqueda."); yield break; }
                var mercadona = pantalla.Busqueda.Buscar("mercadona", pantalla.PuntoCercania());
                Debug.Log($"PriceMap: \"mercadona\" encuentra {mercadona.Count}: " +
                          string.Join("; ", mercadona.Take(5).Select(r => $"{r.Nombre} ({r.Tipo}, {r.Categoria}, {r.Metros:F0} m)")));

                buscador.Abrir();
                buscador.Campo.value = "clinica"; // como el teclado: pasa por el aviso de cambio del campo
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                var filas = buscador.Filas;
                int filasAntes = filas.Count; // Filas es la lista viva: se guarda cuántas había
                Debug.Log($"PriceMap: \"clinica\" da {filas.Count} resultados: " +
                          string.Join("; ", filas.Take(6).Select(f => $"{f.Nombre.text} | {f.Detalle.text} | {f.Precio?.text ?? "-"} | TalkBack \"{f.Anuncio}\"")));
                if (filas.Count == 0) Debug.LogError("PriceMap: \"clinica\" no encuentra nada.");
                else if (filas[0].Resultado.Tipo != TipoResultado.Lugar || !filas[0].Resultado.Nombre.StartsWith("Clínica"))
                    Debug.LogError($"PriceMap: el primer resultado de \"clinica\" es {filas[0].Resultado} y no un lugar \"Clínica …\".");
                if (filas.Count > ServicioBusqueda.MaximoResultados) Debug.LogError($"PriceMap: {filas.Count} resultados (máximo {ServicioBusqueda.MaximoResultados}).");
                if (buscador.FilaChips.resolvedStyle.display != DisplayStyle.None) Debug.LogError("PriceMap: con texto se siguen viendo los chips.");
                if (buscador.Borrar.resolvedStyle.display == DisplayStyle.None) Debug.LogError("PriceMap: con texto no se ve la X.");
                if (filas.Count > 0 && filas[0].worldBound.height < 55.5f) Debug.LogError($"PriceMap: la fila de resultado mide {filas[0].worldBound.height:F1} dp (mínimo 56).");

                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                yield return Capturar("busqueda");
                yield return new WaitForSeconds(1f);
                tema.Alternar();
                yield return new WaitForSeconds(1.5f);
                yield return Capturar("busqueda-oscuro");
                yield return new WaitForSeconds(1f);

                pantalla.Atras();
                yield return null;
                if (buscador.Abierta) Debug.LogError("PriceMap: el botón atrás no ha plegado la hoja con resultados.");
                if (buscador.Campo.value != "clinica" || buscador.Filas.Count != filasAntes)
                    Debug.LogError($"PriceMap: al plegar la hoja se ha perdido la búsqueda (\"{buscador.Campo.value}\", {buscador.Filas.Count} filas).");
                pantalla.BorrarBusqueda();
                yield return null;
                if (buscador.Campo.value != "" || buscador.MostrandoResultados) Debug.LogError("PriceMap: la X no ha vaciado la búsqueda.");

                yield return Direcciones(buscador);
                yield return Enlace(buscador);
                yield return Compartido(buscador);
            }
            finally
            {
                buscador.BorrarTexto();
                buscador.Plegar();
                if (tema.Actual != temaAntes) tema.Alternar();
                if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
                else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            }
        }

        /// <summary>
        /// Historia 2.3 (parte 2), captura "busqueda-direccion" (tema claro): "calle ancha 10" escrito y, tras la
        /// espera y la respuesta de Photon, las direcciones debajo de los resultados locales. Sin red no es un error:
        /// solo se registra (y se comprueba que se ve el aviso de conexión).
        /// </summary>
        IEnumerator Direcciones(HojaBuscador buscador)
        {
            if (pantalla.Direcciones == null) { Debug.LogError("PriceMap: la pantalla no ha recibido el BuscadorDirecciones."); yield break; }
            buscador.Abrir();
            buscador.Campo.value = "calle ancha 10";
            // 350 ms de espera y la respuesta (Photon da por fallida la petición a los 10 s)
            float limite = Time.realtimeSinceStartup + 12f;
            yield return new WaitForSeconds(0.5f);
            while (pantalla.Direcciones.Pendiente && Time.realtimeSinceStartup < limite) yield return null;
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            var filas = buscador.Filas;
            var direcciones = filas.Where(f => f.Resultado.Tipo == TipoResultado.Direccion).ToList();
            if (direcciones.Count > 0)
            {
                Debug.Log($"PriceMap: \"calle ancha 10\" da {direcciones.Count} direcciones (de {filas.Count} filas): " +
                          string.Join("; ", direcciones.Take(4).Select(f => $"{f.Nombre.text} | {f.Detalle.text}")));
                int primera = filas.ToList().FindIndex(f => f.Resultado.Tipo == TipoResultado.Direccion);
                if (filas.Skip(primera).Any(f => f.Resultado.Tipo != TipoResultado.Direccion))
                    Debug.LogError("PriceMap: hay resultados locales detrás de las direcciones.");
                if (direcciones.Count > BuscadorDirecciones.MaximoDirecciones)
                    Debug.LogError($"PriceMap: {direcciones.Count} direcciones (máximo {BuscadorDirecciones.MaximoDirecciones}).");
                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                yield return Capturar("busqueda-direccion");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.Log($"PriceMap: \"calle ancha 10\" sin direcciones (¿sin red?); aviso de conexión {(buscador.AvisoDireccionesVisible ? "visible" : "oculto")}, " +
                          $"botón \"Buscar como dirección\" {(buscador.BuscarDireccionVisible ? "visible" : "oculto")}.");
            }
            pantalla.BorrarBusqueda();
            yield return null;
            if (pantalla.Direcciones.Pendiente) Debug.LogError("PriceMap: la X no ha cancelado la petición de direcciones.");
        }

        /// <summary>
        /// Historia 2.5 (parte 2), captura "enlace" (tema claro): un enlace de Google Maps con @lat,lng pegado en el
        /// campo. Comprueba que la única fila es «Ir a este punto» con sus coordenadas y que no se piden direcciones;
        /// al tocarla se abre la ficha de punto libre en esa coordenada (y se cierra).
        /// </summary>
        IEnumerator Enlace(HojaBuscador buscador)
        {
            const string enlace = "https://www.google.com/maps/place/Plaza+del+Cabildo/@36.7781,-6.3515,17z";
            var esperado = new Coordenada(36.7781, -6.3515);
            buscador.Abrir();
            buscador.Campo.value = enlace;
            yield return new WaitForSeconds(1f);
            yield return new WaitForEndOfFrame();
            var filas = buscador.Filas;
            Debug.Log($"PriceMap: enlace pegado da {filas.Count} filas: " +
                      string.Join("; ", filas.Select(f => $"{f.Nombre.text} | {f.Detalle.text} | TalkBack \"{f.Anuncio}\"")));
            if (filas.Count != 1 || filas[0].Resultado.Tipo != TipoResultado.Coordenada)
                Debug.LogError("PriceMap: el enlace pegado no da una sola fila «Ir a este punto».");
            else
            {
                if (filas[0].Nombre.text != Textos.T(ClavesTexto.BuscadorIrAPunto) || filas[0].Detalle.text != "36,7781, -6,3515")
                    Debug.LogError($"PriceMap: la fila del enlace dice \"{filas[0].Nombre.text}\" / \"{filas[0].Detalle.text}\".");
                if (filas[0].Resultado.Posicion != esperado) Debug.LogError($"PriceMap: el enlace se ha leído como {filas[0].Resultado.Posicion}.");
            }
            if (pantalla.Direcciones != null && pantalla.Direcciones.Pendiente) Debug.LogError("PriceMap: con un enlace se han pedido direcciones.");
            if (buscador.SinResultadosVisible || buscador.BuscarDireccionVisible) Debug.LogError("PriceMap: con el enlace se ve el texto sin resultados.");
            // El enlace largo se recorta dentro del campo: el texto no llega a la X ni la X sale del campo
            var caja = buscador.Campo.worldBound;
            var equis = buscador.Borrar.worldBound;
            Debug.Log($"PriceMap: campo del buscador x {caja.xMin:F0}–{caja.xMax:F0}, X {equis.xMin:F0}–{equis.xMax:F0}.");
            if (caja.xMax > equis.xMin + 0.5f) Debug.LogError("PriceMap: el texto del buscador pasa por debajo de la X.");
            if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
            yield return Capturar("enlace");
            yield return new WaitForSeconds(1f);

            if (filas.Count == 1)
            {
                pantalla.ElegirResultado(filas[0].Resultado);
                yield return new WaitForSeconds(0.5f);
                if (pantalla.PuntoAbierto != esperado) Debug.LogError("PriceMap: «Ir a este punto» no ha abierto la ficha del punto.");
                pantalla.CerrarFicha();
                yield return new WaitForSeconds(0.5f);
            }
            pantalla.BorrarBusqueda();
            yield return null;
        }

        /// <summary>
        /// Historia 2.5 (parte 3), captura "compartido" (tema claro): llega un <c>geo:</c> compartido con la app (como
        /// «Abrir con» desde otra app) con la hoja plegada. Comprueba que el enlace queda en el campo y que se abre sola
        /// la ficha de punto libre en su coordenada (y se cierra).
        /// </summary>
        IEnumerator Compartido(HojaBuscador buscador)
        {
            const string geo = "geo:36.7781,-6.3515?q=36.7781,-6.3515(Plaza%20del%20Cabildo)"; // como llega de Android
            var esperado = new Coordenada(36.7781, -6.3515);
            buscador.Plegar();
            yield return null;
            pantalla.RecibirCompartido(geo);
            yield return new WaitForSeconds(1f);
            yield return new WaitForEndOfFrame();
            if (buscador.Campo.value != geo) Debug.LogError($"PriceMap: lo compartido no está en el campo (\"{buscador.Campo.value}\").");
            if (pantalla.PuntoAbierto != esperado) Debug.LogError($"PriceMap: lo compartido no ha abierto la ficha del punto ({pantalla.PuntoAbierto}).");
            else Debug.Log("PriceMap: geo: compartido abre la ficha de punto libre en " + esperado + ".");
            if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
            yield return Capturar("compartido");
            yield return new WaitForSeconds(1f);
            pantalla.CerrarFicha();
            yield return new WaitForSeconds(0.5f);
            pantalla.BorrarBusqueda();
            yield return null;
        }

        /// <summary>
        /// Historia 2.4 (parte 1), captura "cerca" (tema claro): la hoja abierta del todo con la lista de Salud.
        /// Comprueba que Clínica Areas (con precio) está entre los primeros, que los de precio van delante, que no hay
        /// más de 50 filas y que se ven la cabecera y no los chips. Al final, atrás vuelve a las categorías.
        /// </summary>
        IEnumerator Cerca()
        {
            var buscador = pantalla != null ? pantalla.Buscador : null;
            if (buscador == null) { Debug.LogError("PriceMap: no hay buscador para \"cerca\"."); yield break; }
            if (pantalla.Cerca == null) { Debug.LogError("PriceMap: la pantalla no ha recibido el ServicioCerca."); yield break; }
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            var temaAntes = tema.Actual;
            try
            {
                mapa.Centrar(PantallaMapa.VistaInicial);
                yield return new WaitForSeconds(2f); // teselas de la vista inicial: sus puntos de interés
                buscador.BorrarTexto();
                buscador.Abrir();
                buscador.ElegirCategoria(CategoriaPoi.Salud); // como el toque en el chip
                yield return new WaitForSeconds(1f);
                yield return new WaitForEndOfFrame();
                var filas = buscador.Filas;
                Debug.Log($"PriceMap: \"Salud cerca\" da {filas.Count} filas: " +
                          string.Join("; ", filas.Take(8).Select(f => $"{f.Nombre.text} | {f.Detalle.text} | {f.Precio?.text ?? "-"}")));
                if (!buscador.MostrandoCerca || buscador.CabeceraCerca.resolvedStyle.display == DisplayStyle.None)
                    Debug.LogError("PriceMap: no se ve la lista \"cerca\" con su cabecera.");
                if (buscador.FilaChips.resolvedStyle.display != DisplayStyle.None) Debug.LogError("PriceMap: con la lista \"cerca\" se siguen viendo los chips.");
                if (filas.Count > ServicioCerca.MaximoResultados) Debug.LogError($"PriceMap: {filas.Count} filas (máximo {ServicioCerca.MaximoResultados}).");
                int areas = filas.ToList().FindIndex(f => f.Resultado.Lugar?.Id == "clinica-areas");
                if (areas < 0 || areas > 4) Debug.LogError($"PriceMap: Clínica Areas no está entre los primeros de Salud (posición {areas}).");
                else if (filas[areas].Precio == null) Debug.LogError("PriceMap: Clínica Areas sale sin precio en la lista de Salud.");
                int primeraSinPrecio = filas.ToList().FindIndex(f => f.Resultado.Precio == null);
                if (primeraSinPrecio >= 0 && filas.Skip(primeraSinPrecio).Any(f => f.Resultado.Precio != null))
                    Debug.LogError("PriceMap: hay lugares con precio detrás de los que no lo tienen.");

                if (tema.Actual != Tema.Claro) { tema.Alternar(); yield return new WaitForSeconds(1.5f); }
                yield return Capturar("cerca");
                yield return new WaitForSeconds(1f);

                // Historia 2.4 (parte 2), captura "cerca-mapa": la hoja plegada, el mapa solo con Salud y la píldora
                // "Salud ✕"; después la píldora quita la categoría y vuelve el filtro guardado
                var filtroAntes = filtro.Actual;
                pantalla.PlegarBuscador();
                yield return new WaitForSeconds(1.5f);
                yield return new WaitForEndOfFrame();
                if (pantalla.CategoriaMapa != CategoriaPoi.Salud) Debug.LogError("PriceMap: al plegar la hoja el mapa ya no está filtrado por Salud.");
                var pildora = pantalla.Pildora;
                if (pildora == null || !pildora.ClassListContains(PildoraPrecios.ClaseCategoria) || pildora.Texto.text != Textos.T(ClavesTexto.BuscadorSalud))
                    Debug.LogError($"PriceMap: la píldora no dice \"Salud ✕\" (\"{pildora?.Texto.text}\").");
                var marcadoresFuera = pantalla.Capa.Marcadores.Where(m => m.Visible && !ReglaCerca.Incluye(CategoriaPoi.Salud, m.Lugar)).ToList();
                if (marcadoresFuera.Count > 0)
                    Debug.LogError("PriceMap: con Salud se ven marcadores de otras categorías: " + string.Join(", ", marcadoresFuera.Select(m => m.Lugar.Nombre)));
                var puntosFuera = pantalla.CapaPuntos.Vistas.Where(v => v.Visible && !ReglaCerca.Incluye(CategoriaPoi.Salud, v.Categoria)).ToList();
                if (puntosFuera.Count > 0)
                    Debug.LogError("PriceMap: con Salud se ven puntos de otras categorías: " + string.Join(", ", puntosFuera.Select(v => v.Punto.Nombre)));
                Debug.Log($"PriceMap: mapa de Salud con {pantalla.Capa.Marcadores.Count(m => m.Visible)} marcadores y {pantalla.CapaPuntos.Visibles} puntos.");
                yield return Capturar("cerca-mapa");
                yield return new WaitForSeconds(1f);

                pantalla.PulsarPildora();
                yield return null;
                if (pildora == null || pantalla.CercaActiva || pantalla.CategoriaMapa != null || filtro.Actual != filtroAntes
                    || pildora.ClassListContains(PildoraPrecios.ClaseCategoria) || pildora.Texto.text != Textos.T(TextosFiltro.ClavePildora(filtroAntes)))
                    Debug.LogError("PriceMap: la píldora \"Salud ✕\" no ha quitado la categoría o no ha vuelto el filtro guardado.");

                // Otra vez Salud con la hoja abierta: el botón atrás vuelve a las categorías
                buscador.Abrir();
                buscador.ElegirCategoria(CategoriaPoi.Salud);
                yield return new WaitForSeconds(1f);
                pantalla.Atras();
                yield return null;
                if (pantalla.CercaActiva || !buscador.MostrandoCategorias || !buscador.Abierta)
                    Debug.LogError("PriceMap: el botón atrás no ha vuelto a las categorías con la hoja abierta.");
                if (pantalla.CategoriaMapa != null || filtro.Actual != filtroAntes)
                    Debug.LogError("PriceMap: el botón atrás no ha devuelto el filtro guardado al mapa.");
            }
            finally
            {
                pantalla.QuitarCategoria();
                buscador.Plegar();
                if (tema.Actual != temaAntes) tema.Alternar();
                if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
                else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            }
        }

        /// <summary>Da Debug.LogError si la zona pisa alguno de los controles (medio dp de tolerancia).</summary>
        static void SinSolape(string que, Rect zona, params (string nombre, VisualElement control)[] controles)
        {
            var z = Rect.MinMaxRect(zona.xMin + 0.5f, zona.yMin + 0.5f, zona.xMax - 0.5f, zona.yMax - 0.5f);
            foreach (var (nombre, control) in controles)
            {
                if (control == null || control.resolvedStyle.display == DisplayStyle.None) continue;
                var r = control.worldBound;
                if (float.IsNaN(r.x) || r.width <= 0f) continue;
                if (z.Overlaps(r)) Debug.LogError($"PriceMap: {que} pisa {nombre} ({zona} y {r}).");
            }
        }

        int Visibles()
        {
            int n = 0;
            var capa = pantalla != null ? pantalla.Capa : null;
            if (capa != null) foreach (var m in capa.Marcadores) if (m.Visible) n++;
            return n;
        }

        /// <summary>
        /// Ningún par de marcadores visibles que se pisa y ningún punto de interés visible encima de un marcador;
        /// tampoco nada debajo de un control (cuenta además los sin precio visibles, solo informativo). Con las cajas
        /// reales tras la disposición y medio dp de tolerancia (bordes que solo se tocan por redondeo).
        /// </summary>
        void SolapesConMarcadores(CapaMarcadores capa)
        {
            static Rect Encoger(Rect r) => Rect.MinMaxRect(r.xMin + 0.25f, r.yMin + 0.25f, r.xMax - 0.25f, r.yMax - 0.25f);
            static Rect Unir(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                                                                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
            int sinPrecio = 0;
            var zonas = new List<(Rect zona, string id)>();
            foreach (var m in capa.Marcadores)
            {
                if (!m.Visible) continue;
                if (!m.TienePrecio) sinPrecio++;
                var zona = Unir(m.Cuerpo.worldBound, m.Punta.worldBound);
                if (!float.IsNaN(zona.x)) zonas.Add((Encoger(zona), m.Lugar.Id));
            }
            int paresMarcadores = 0; string ejemplo = "-";
            for (int i = 0; i < zonas.Count; i++)
                for (int j = i + 1; j < zonas.Count; j++)
                    if (zonas[i].zona.Overlaps(zonas[j].zona)) { paresMarcadores++; if (ejemplo == "-") ejemplo = $"{zonas[i].id} y {zonas[j].id}"; }

            int paresPuntos = 0;
            var cajas = new List<Rect>();
            var capaPuntos = pantalla.CapaPuntos;
            if (capaPuntos != null)
                foreach (var v in capaPuntos.Vistas)
                {
                    if (!v.Visible) continue;
                    var caja = v.Icono.worldBound;
                    if (v.Nombre != null && v.Nombre.worldBound.width > 0f) caja = Unir(caja, v.Nombre.worldBound);
                    if (float.IsNaN(caja.x)) continue;
                    caja = Encoger(caja);
                    cajas.Add(caja);
                    foreach (var z in zonas)
                        if (caja.Overlaps(z.zona)) { paresPuntos++; if (ejemplo == "-") ejemplo = $"\"{v.Punto.Nombre}\" sobre {z.id}"; }
                }

            // Controles (las mismas zonas que usa la pantalla: píldora, menú abierto y columnas): ni marcadores ni puntos debajo
            int bajoControles = 0;
            var controles = new List<Rect>(pantalla.ZonasControles()); // copia: la pantalla rehace la lista cada fotograma
            if (controles.Count == 0) Debug.LogError("PriceMap: la pantalla no da ninguna zona de controles medida.");
            // La cápsula del buscador se mide aparte (no de la lista de la pantalla): si faltara en ella, aquí se ve
            var buscador = pantalla.Buscador;
            if (buscador != null && buscador.Visible && !float.IsNaN(buscador.worldBound.x)) controles.Add(buscador.worldBound);
            foreach (var c in controles)
            {
                var r = Encoger(c);
                foreach (var z in zonas) if (r.Overlaps(z.zona)) bajoControles++;
                foreach (var k in cajas) if (r.Overlaps(k)) bajoControles++;
            }

            Debug.Log($"PriceMap: marcadores sin precio visibles {sinPrecio}; ocultos por solape {capa.OcultosPorSolape}; " +
                      $"pares que se pisan {paresMarcadores + paresPuntos} (marcadores {paresMarcadores}, puntos sobre marcador {paresPuntos}; " +
                      $"primero: {ejemplo}); bajo un control {bajoControles}");
            if (paresMarcadores + paresPuntos > 0) Debug.LogError($"PriceMap: {paresMarcadores + paresPuntos} pares se pisan con un marcador.");
            if (bajoControles > 0) Debug.LogError($"PriceMap: {bajoControles} marcadores o puntos debajo de un control.");
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
            if (!areas.Visible) Debug.LogError("PriceMap: Clínica Areas no se ve en la captura de marcadores.");
            SolapesConMarcadores(capa);
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
