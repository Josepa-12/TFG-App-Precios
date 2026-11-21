using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Mapa real dibujado en Unity a partir de teselas VECTORIALES de OpenFreeMap (las mismas que usa el
    /// prototipo web con MapLibre), con datos de OpenStreetMap y sin proveedores con cuenta:
    ///  - calles como cintas con borde, con anchura según su tipo;
    ///  - agua y zonas verdes como polígonos;
    ///  - edificios extruidos con su altura real (render_height de OpenMapTiles);
    ///  - nombres de calles sobre el suelo.
    /// Todo con los colores exactos de la paleta del proyecto (no hay que recolorear imágenes).
    ///
    /// Atribución obligatoria (la muestra PantallaMapa): OpenFreeMap © OpenMapTiles · Datos © colaboradores de OpenStreetMap.
    /// </summary>
    public class MapaVectorial : MonoBehaviour, IMapa
    {
        [Header("Zona")]
        public double latitud = 36.7779;   // Plaza del Cabildo
        public double longitud = -6.3528;
        [Tooltip("OpenFreeMap llega hasta el zoom 14 (≈ 2 km por tesela en Sanlúcar)")]
        [Range(12, 14)] public int zoom = 14;
        [Tooltip("Teselas alrededor del centro de la vista: 1 → 3×3 (unos 6 km de lado)")]
        [Range(0, 2)] public int radio = 1;
        [Tooltip("Con la cámara más lejos que esto, se carga un anillo más (5×5)")]
        public float distanciaAnilloExtra = 2000f;
        [Tooltip("Se liberan las teselas a más de esta distancia del centro (en teselas)")]
        public int radioLiberar = 3;
        public string tileJson = "https://tiles.openfreemap.org/planet";

        [Header("Aspecto")]
        public TemaMapa tema = TemaMapa.Claro;
        public bool nombresDeCalles = true;
        public float alturaTexto = 7f;          // metros
        public float alturaEdificioPorDefecto = 6f;
        public bool sombras = true;

        // Materiales de cada capa (orden de pintado = cola de render)
        Material mSuelo, mVerde, mAgua, mBorde, mCalleMenor, mCalle, mEdificios;
        readonly List<TextMesh> etiquetas = new List<TextMesh>();
        readonly Dictionary<string, List<Vector3>> nombresColocados = new Dictionary<string, List<Vector3>>();

        // Teselas cargadas (con lo que hay que deshacer al liberarlas), en cola y en carga
        class TeselaCargada
        {
            public Transform raiz;
            public readonly List<(string nombre, Vector3 pos)> nombres = new List<(string, Vector3)>();
            public readonly List<PuntoInteres> puntos = new List<PuntoInteres>(); // capa "poi"
        }
        readonly Dictionary<Vector2Int, TeselaCargada> cargadas = new Dictionary<Vector2Int, TeselaCargada>();
        readonly HashSet<Vector2Int> pendientes = new HashSet<Vector2Int>();
        readonly Queue<Vector2Int> cola = new Queue<Vector2Int>();
        Vector2Int centroAplicado = new Vector2Int(int.MinValue, int.MinValue);
        int radioAplicado = -1;
        string plantilla;            // dirección de las teselas de OpenFreeMap (cambia con cada versión de los datos)
        float ultimoIntentoPlantilla = -999f;
        float reintentarEn = -1f;    // si alguna tesela falló (sin red), cuándo volver a planificar aunque la vista no se mueva
        const float EsperaReintento = 5f;

        /// <summary>Número de teselas construidas ahora mismo (para comprobaciones y pruebas).</summary>
        public int TeselasCargadas => cargadas.Count;
        internal int TeselasPendientes => pendientes.Count;
        internal int RadioLiberarEfectivo => Mathf.Max(radioLiberar, radio + 2); // nunca por debajo del anillo extra + 1

        /// <summary>Para las pruebas: da por construida una tesela (sin descargarla), con sus puntos de interés.</summary>
        internal void SimularCargada(Vector2Int t, IEnumerable<PuntoInteres> puntos = null)
        {
            var tesela = new TeselaCargada { raiz = new GameObject($"Tesela simulada {t}").transform };
            if (puntos != null) tesela.puntos.AddRange(puntos);
            cargadas[t] = tesela;
            RecomponerPuntos();
        }

        // Puntos de interés de todas las teselas cargadas (se rehace al cargar o liberar una)
        readonly List<PuntoInteres> puntosInteres = new List<PuntoInteres>();
        TemaMapa temaAplicado = (TemaMapa)(-1);
        CamaraMapa camara;
        Font fuente;

        void Awake()
        {
            Geo.LatitudOrigen = latitud;
            Geo.LongitudOrigen = longitud;
        }

        IEnumerator Start()
        {
            CrearMateriales();
            CrearSuelo();
            Camara();
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            PrepararSombras();
            AplicarTema();

            // Las teselas se cargan de una en una, de la cola que llena Update según el centro de la vista
            while (true)
            {
                if (cola.Count == 0) { yield return null; continue; }
                var t = cola.Dequeue();
                if (PlanTeselas.Distancia(t, centroAplicado) <= radioAplicado) yield return CargarTesela(t.x, t.y);
                pendientes.Remove(t);
                // Si falló (sin red ni caché), se vuelve a planificar en unos segundos aunque la vista no se mueva
                if (!cargadas.ContainsKey(t) && reintentarEn < 0f) reintentarEn = Time.realtimeSinceStartup + EsperaReintento;
                // Si la vista se fue lejos mientras se cargaba, se libera ya (nunca más de 7×7)
                else if (cargadas.ContainsKey(t) && PlanTeselas.Distancia(t, centroAplicado) > RadioLiberarEfectivo) Liberar(t);
            }
        }

        /// <summary>Pide la dirección de las teselas (o usa la última guardada) como mucho cada 5 s.</summary>
        IEnumerator ObtenerPlantilla()
        {
            if (plantilla != null || Time.realtimeSinceStartup - ultimoIntentoPlantilla < 5f) yield break;
            ultimoIntentoPlantilla = Time.realtimeSinceStartup;
            string rutaPlantilla = Path.Combine(Application.persistentDataPath, "vectorial", "plantilla.txt");
            using (var req = UnityWebRequest.Get(tileJson))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var tj = JsonUtility.FromJson<TileJson>(req.downloadHandler.text);
                    if (tj?.tiles != null && tj.tiles.Length > 0)
                    {
                        plantilla = tj.tiles[0];
                        Directory.CreateDirectory(Path.GetDirectoryName(rutaPlantilla));
                        File.WriteAllText(rutaPlantilla, plantilla);
                    }
                }
                else if (File.Exists(rutaPlantilla)) plantilla = File.ReadAllText(rutaPlantilla); // sin conexión: la última conocida
            }
            if (plantilla == null) Debug.LogWarning("PriceMap: sin dirección de teselas de OpenFreeMap; solo se dibuja lo que está en caché.");
        }

        /// <summary>Carga las teselas que faltan alrededor del centro de la vista y libera las lejanas.</summary>
        internal void ActualizarTeselas()
        {
            if (reintentarEn >= 0f && Time.realtimeSinceStartup >= reintentarEn)
            {
                reintentarEn = -1f;
                radioAplicado = -1; // fuerza a planificar de nuevo: las que faltan vuelven a la cola
            }
            var pivote = camara != null ? camara.pivote : Vector3.zero;
            var geo = Geo.AGeo(pivote);
            var centro = Geo.Tesela(geo.lat, geo.lon, zoom);
            int radioCarga = radio + (camara != null && camara.distancia > distanciaAnilloExtra ? 1 : 0);
            if (centro == centroAplicado && radioCarga == radioAplicado) return;
            centroAplicado = centro;
            radioAplicado = radioCarga;

            var plan = PlanTeselas.Calcular(centro, new List<Vector2Int>(cargadas.Keys), pendientes, radioCarga, RadioLiberarEfectivo);
            foreach (var t in plan.aLiberar) Liberar(t);
            foreach (var t in plan.aCargar) { pendientes.Add(t); cola.Enqueue(t); }
        }

        void Liberar(Vector2Int t)
        {
            if (!cargadas.TryGetValue(t, out var tesela)) return;
            cargadas.Remove(t);
            if (tesela.puntos.Count > 0) RecomponerPuntos();
            foreach (var (nombre, pos) in tesela.nombres)
                if (nombresColocados.TryGetValue(nombre, out var usados)) usados.Remove(pos);
            if (tesela.raiz == null) return;
            var raizTesela = tesela.raiz;
            etiquetas.RemoveAll(e => e == null || e.transform.IsChildOf(raizTesela));
            // Las mallas creadas por código no se liberan solas al destruir el objeto
            foreach (var f in tesela.raiz.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null) Destruir(f.sharedMesh);
            Destruir(tesela.raiz.gameObject);
        }

        // Fuera de Play (pruebas EditMode) Unity no permite Destroy
        static void Destruir(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        [System.Serializable] class TileJson { public string[] tiles; }

        // ───────────────────────── Puerto IMapa ─────────────────────────

        /// <summary>Centra la cámara del mapa en una coordenada real.</summary>
        public void Centrar(Coordenada destino)
        {
            var c = Camara();
            if (c != null) c.CentrarEn(destino.Latitud, destino.Longitud);
        }

        /// <summary>Deja la coordenada en ese punto de pantalla (px, origen arriba a la izquierda, como Proyectar).</summary>
        public void Centrar(Coordenada destino, PuntoPantalla enPantalla)
        {
            var c = Camara();
            // Unity cuenta Y desde abajo: la inversa exacta de Proyectar
            if (c != null) c.CentrarEn(destino.Latitud, destino.Longitud, new Vector2(enPantalla.X, Screen.height - enPantalla.Y));
        }

        /// <summary>Pinta el mapa con la paleta del tema; se aplica en el siguiente fotograma.</summary>
        public void AplicarTema(Tema nuevo) => tema = nuevo == Tema.Oscuro ? TemaMapa.Oscuro : TemaMapa.Claro;

        public void Acercar() { var c = Camara(); if (c != null) c.Acercar(); }

        /// <summary>
        /// Proyecta la coordenada (en el suelo) con la cámara del mapa tal como está ahora. La cámara se
        /// mueve en Update (orden -100), así que en LateUpdate ya no cambia en este fotograma.
        /// </summary>
        public PuntoPantalla Proyectar(Coordenada punto)
        {
            var c = Camara();
            if (c == null) return PuntoPantalla.Ninguno;
            if (camaraUnity == null || camaraUnity.gameObject != c.gameObject) camaraUnity = c.GetComponent<Camera>();
            if (camaraUnity == null) return PuntoPantalla.Ninguno;
            var p = camaraUnity.WorldToScreenPoint(Geo.ALocal(punto.Latitud, punto.Longitud));
            // Unity cuenta Y desde abajo; la UI, desde arriba (en píxeles de toda la pantalla)
            return new PuntoPantalla(p.x, Screen.height - p.y, p.z);
        }
        Camera camaraUnity;

        /// <summary>Para no mover el mapa cuando el dedo empieza sobre un control de la interfaz.</summary>
        public System.Func<Vector2, bool> EntradaBloqueada
        {
            set { entradaBloqueada = value; Camara(); }
        }
        System.Func<Vector2, bool> entradaBloqueada; // se pasa a la cámara en cuanto exista
        public void Alejar() { var c = Camara(); if (c != null) c.Alejar(); }

        /// <summary>Puntos de interés (capa "poi") de las teselas cargadas, sin clasificar.</summary>
        public IReadOnlyList<PuntoInteres> PuntosInteres => puntosInteres;

        /// <summary>Se lanza al cargar o liberar una tesela con puntos de interés.</summary>
        public event System.Action PuntosInteresCambiados;

        /// <summary>Distancia de la cámara a su pivote (m); infinito si aún no hay cámara.</summary>
        public float DistanciaCamara
        {
            get { var c = Camara(); return c != null ? c.distancia : float.PositiveInfinity; }
        }

        /// <summary>Giro de la cámara en grados (0 = norte arriba, sentido horario); 0 si aún no hay cámara.</summary>
        public float Giro
        {
            get { var c = Camara(); return c != null ? c.giro : 0f; }
        }

        /// <summary>Coordenada del pivote de la cámara (centro de la vista); sin cámara, la vista inicial del mapa.</summary>
        public Coordenada Centro
        {
            get
            {
                var c = Camara();
                if (c == null) return new Coordenada(latitud, longitud);
                var g = Geo.AGeo(c.pivote);
                return new Coordenada(g.lat, g.lon);
            }
        }

        /// <summary>Gira el mapa al norte (giro 0) sin tocar zoom ni inclinación.</summary>
        public void OrientarNorte() { var c = Camara(); if (c != null) c.OrientarNorte(); }

        /// <summary>Toque de la cámara pasado a px con origen arriba a la izquierda (como Proyectar).</summary>
        public event System.Action<PuntoPantalla> Toque;

        void AlTocarCamara(Vector2 pantalla) => Toque?.Invoke(new PuntoPantalla(pantalla.x, Screen.height - pantalla.y, 1f));

        /// <summary>Pulsación larga de la cámara pasada a px con origen arriba a la izquierda (como Toque).</summary>
        public event System.Action<PuntoPantalla> PulsacionLarga;

        void AlPulsarLargoCamara(Vector2 pantalla) => PulsacionLarga?.Invoke(new PuntoPantalla(pantalla.x, Screen.height - pantalla.y, 1f));

        /// <summary>
        /// Coordenada del suelo bajo el punto (px, origen arriba a la izquierda): el rayo de la cámara contra el
        /// suelo, pasado a latitud y longitud. Null por encima del horizonte o sin cámara.
        /// </summary>
        public Coordenada? Desproyectar(PuntoPantalla punto)
        {
            var c = Camara();
            if (c == null || float.IsNaN(punto.X) || float.IsNaN(punto.Y)) return null;
            // Unity cuenta Y desde abajo: la inversa de Proyectar
            var suelo = c.PuntoEnSuelo(new Vector2(punto.X, Screen.height - punto.Y));
            if (!suelo.HasValue) return null;
            var g = Geo.AGeo(suelo.Value);
            return new Coordenada(g.lat, g.lon);
        }

        // ───────────────────────── Ruta (historia 2.6, parte 2a) ─────────────────────────

        /// <summary>Parte del ancho de la línea que es relleno: el resto es su borde, algo más oscuro, por los dos lados.</summary>
        public const float ProporcionRellenoRuta = 0.78f;

        Material mRuta, mRutaBorde;
        TemaMapa temaRuta = (TemaMapa)(-1);
        bool rutaAproximada;         // ruta aproximada sin conexión (parte 2b): colores más suaves
        List<Vector3> puntosRuta;    // puntos de la ruta dibujada (en el mundo); null sin ruta
        GameObject objetoRuta;       // cuelga del mapa (no de una tesela): no se libera con ellas
        float anchoRuta;             // ancho (m) con el que se construyó la línea

        /// <summary>Para las pruebas: el objeto de la línea (null sin ruta) y el ancho con el que se construyó.</summary>
        internal GameObject ObjetoRuta => objetoRuta;
        internal float AnchoRuta => anchoRuta;
        internal Material MaterialRuta => mRuta;
        internal Material MaterialBordeRuta => mRutaBorde;
        internal bool RutaAproximada => rutaAproximada;

        /// <summary>
        /// Dibuja la ruta por esos puntos (sustituye a la anterior); con menos de dos puntos, la quita. La aproximada
        /// (parte 2b) es la misma línea con los colores suaves de su tema.
        /// </summary>
        public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false)
        {
            if (puntos == null || puntos.Count < 2) { QuitarRuta(); return; }
            rutaAproximada = aproximada;
            puntosRuta = new List<Vector3>(puntos.Count);
            foreach (var c in puntos) puntosRuta.Add(Geo.ALocal(c.Latitud, c.Longitud));
            ConstruirRuta();
            ColorearRuta();
        }

        /// <summary>Quita la línea de la ruta y libera su malla.</summary>
        public void QuitarRuta()
        {
            puntosRuta = null;
            anchoRuta = 0f;
            DestruirObjetoRuta();
        }

        /// <summary>Encuadra los puntos en el rectángulo libre (px, origen arriba a la izquierda) con la cámara del mapa.</summary>
        public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre)
        {
            var c = Camara();
            if (c == null || puntos == null || puntos.Count == 0 || !libre.Valido) return;
            var mundo = new List<Vector3>(puntos.Count);
            foreach (var p in puntos) mundo.Add(Geo.ALocal(p.Latitud, p.Longitud));
            // Unity cuenta Y desde abajo: el borde de abajo del rectángulo es su yMin
            c.Encuadrar(mundo, Rect.MinMaxRect(libre.Izquierda, Screen.height - libre.Abajo, libre.Derecha, Screen.height - libre.Arriba));
        }

        /// <summary>
        /// Ancho de la línea (m): el de la calle (9 m) o, si con él se vería más fina de 6 dp, los metros que ocupan 6 dp
        /// en el centro de la vista. Metros por píxel = 2 · distancia · tan(FOV / 2) / alto en px; 1 dp = dpi / 160 px
        /// (sin dpi, 160).
        /// </summary>
        public static float AnchoRutaMetros(float distancia, float fovVerticalGrados, float altoPx, float dpi)
        {
            if (float.IsNaN(distancia) || float.IsInfinity(distancia) || distancia <= 0f || altoPx <= 0f || fovVerticalGrados <= 0f)
                return ParametrosNegocio.RutaAnchoMetros;
            float metrosPorPx = 2f * distancia * Mathf.Tan(fovVerticalGrados * 0.5f * Mathf.Deg2Rad) / altoPx;
            float pxPorDp = (dpi > 0f ? dpi : 160f) / 160f;
            return Mathf.Max(ParametrosNegocio.RutaAnchoMetros, ParametrosNegocio.RutaAnchoMinimoDp * pxPorDp * metrosPorPx);
        }

        /// <summary>True si el ancho nuevo se aleja más de un 10 % del aplicado (o no hay ninguno): hay que rehacer la línea.</summary>
        public static bool RehacerRuta(float anchoAplicado, float anchoNuevo) =>
            anchoAplicado <= 0f || Mathf.Abs(anchoNuevo - anchoAplicado) > ParametrosNegocio.RutaAnchoCambioRehacer * anchoAplicado;

        // Ancho que toca con la cámara de ahora (sin cámara, el de la calle)
        float AnchoRutaAhora()
        {
            var c = Camara();
            if (c == null) return ParametrosNegocio.RutaAnchoMetros;
            if (camaraUnity == null || camaraUnity.gameObject != c.gameObject) camaraUnity = c.GetComponent<Camera>();
            if (camaraUnity == null) return ParametrosNegocio.RutaAnchoMetros;
            return AnchoRutaMetros(c.distancia, camaraUnity.fieldOfView, camaraUnity.pixelHeight, Screen.dpi);
        }

        // Borde (más ancho, cola 1960) y relleno (cola 1970) encima de las calles; los edificios (después) la tapan
        void ConstruirRuta()
        {
            DestruirObjetoRuta();
            if (puntosRuta == null) return;
            MaterialesRuta();
            anchoRuta = AnchoRutaAhora();
            var borde = new Malla();
            var relleno = new Malla();
            Linea(borde, puntosRuta, anchoRuta);
            Linea(relleno, puntosRuta, anchoRuta * ProporcionRellenoRuta);
            objetoRuta = new GameObject("Ruta");
            objetoRuta.transform.SetParent(transform, false);
            Pieza(objetoRuta.transform, "Borde de la ruta", borde, mRutaBorde, false);
            Pieza(objetoRuta.transform, "Relleno de la ruta", relleno, mRuta, false);
        }

        void DestruirObjetoRuta()
        {
            if (objetoRuta == null) return;
            foreach (var f in objetoRuta.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null) Destruir(f.sharedMesh);
            Destruir(objetoRuta);
            objetoRuta = null;
        }

        // Se crean al dibujar la primera ruta (puede llegar antes que Start, o sin Start en las pruebas)
        void MaterialesRuta()
        {
            if (mRuta != null) return;
            var plano = Shader.Find("PriceMap/PlanoPaleta");
            Material Plano(string n, int cola)
            {
                var m = new Material(plano) { name = n, renderQueue = cola };
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_ZTest", (float)CompareFunction.Always);
                return m;
            }
            mRutaBorde = Plano("Borde de la ruta", 1960);
            mRuta = Plano("Relleno de la ruta", 1970);
            ColorearRuta();
        }

        void ColorearRuta()
        {
            temaRuta = tema;
            var p = PaletaMapa.De(tema);
            mRuta.color = rutaAproximada ? p.rutaAproximada : p.ruta;
            mRutaBorde.color = rutaAproximada ? p.rutaAproximadaBorde : p.rutaBorde;
        }

        /// <summary>Para las pruebas: aplica ya el tema a la línea (en la app lo hace Update).</summary>
        internal void ColorearRutaAhora() { if (mRuta != null) ColorearRuta(); }

        void RecomponerPuntos()
        {
            puntosInteres.Clear();
            foreach (var t in cargadas.Values) puntosInteres.AddRange(t.puntos);
            PuntosInteresCambiados?.Invoke();
        }

        /// <summary>Nombre que se ve: "name:es" y, si no lo hay, "name"; vacío si no tiene.</summary>
        internal static string NombreDe(ElementoMVT e)
        {
            var es = e.Texto("name:es");
            if (!string.IsNullOrWhiteSpace(es)) return es.Trim();
            var nombre = e.Texto("name");
            return string.IsNullOrWhiteSpace(nombre) ? "" : nombre.Trim();
        }

        /// <summary>
        /// Puntos de la capa "poi" con su coordenada real. Solo los que caen dentro de la tesela: el
        /// margen de cada tesela repite los de la vecina.
        /// </summary>
        internal static List<PuntoInteres> PuntosDe(CapaMVT capa, System.Func<Vector2, Coordenada> aCoordenada)
        {
            var r = new List<PuntoInteres>();
            if (capa == null) return r;
            foreach (var e in capa.elementos)
            {
                if (e.tipo != TipoGeometria.Punto || e.partes.Count == 0 || e.partes[0].Count == 0) continue;
                var p = e.partes[0][0];
                if (p.x < 0 || p.x >= capa.extension || p.y < 0 || p.y >= capa.extension) continue;
                r.Add(new PuntoInteres(e.id, NombreDe(e), e.Texto("class"), e.Texto("subclass"),
                    Mathf.RoundToInt(e.Numero("rank", 99f)), aCoordenada(p)));
            }
            return r;
        }

        // Se busca al usarla: Centrar puede llamarse antes que Start
        CamaraMapa Camara()
        {
            if (camara == null)
            {
                camara = FindFirstObjectByType<CamaraMapa>();
                if (camara != null)
                {
                    camara.Tocado += AlTocarCamara; // una sola vez, al encontrarla
                    camara.PulsacionLarga += AlPulsarLargoCamara;
                }
            }
            if (camara != null && entradaBloqueada != null) camara.entradaBloqueada = entradaBloqueada;
            return camara;
        }

        void Update()
        {
            if (mSuelo != null && tema != temaAplicado) AplicarTema(); // el tema puede llegar antes que Start
            if (mRuta != null && tema != temaRuta) ColorearRuta();
            if (mSuelo != null) ActualizarTeselas();
            if (puntosRuta != null && RehacerRuta(anchoRuta, AnchoRutaAhora())) ConstruirRuta();
            if (camara != null && sombras) QualitySettings.shadowDistance = Mathf.Clamp(camara.distancia * 2.2f, 250f, 5000f);
        }

        // ───────────────────────── Descarga ─────────────────────────

        IEnumerator CargarTesela(int x, int y)
        {
            string ruta = Path.Combine(Application.persistentDataPath, "vectorial", zoom.ToString(), x.ToString(), y + ".pbf");
            byte[] datos = null;
            if (File.Exists(ruta))
            {
                try { datos = File.ReadAllBytes(ruta); } // caché: funciona sin conexión
                catch (System.Exception e) { Debug.LogWarning($"PriceMap: no se pudo leer la tesela en caché {ruta}: {e.Message}"); yield break; }
            }
            else
            {
                yield return ObtenerPlantilla();
                if (plantilla == null) yield break; // sin red ni caché: se verá el fondo y se reintentará
                string url = plantilla.Replace("{z}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
                using var req = UnityWebRequest.Get(url);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning($"PriceMap: no se pudo descargar la tesela {zoom}/{x}/{y}: {req.error}"); yield break; }
                datos = Descomprimir(req.downloadHandler.data);
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                File.WriteAllBytes(ruta, datos);
            }

            ConstruirDesde(datos, x, y);
            yield return null; // reparte el trabajo entre fotogramas
        }

        /// <summary>
        /// Lee y construye la tesela a partir de sus bytes (ya descomprimidos) y la da por cargada; si trae
        /// puntos de interés, rehace la lista y avisa. Devuelve false si es ilegible o no se pudo construir.
        /// </summary>
        internal bool ConstruirDesde(byte[] datos, int x, int y)
        {
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            Dictionary<string, CapaMVT> capas;
            try { capas = LectorMVT.Leer(datos); }
            catch (System.Exception e) { Debug.LogWarning($"PriceMap: tesela {zoom}/{x}/{y} ilegible: {e.Message}"); return false; }
            TeselaCargada tesela;
            try { tesela = Construir(capas, x, y); }
            catch (System.Exception e) { Debug.LogWarning($"PriceMap: no se pudo construir la tesela {zoom}/{x}/{y}: {e.Message}"); return false; }
            cargadas[new Vector2Int(x, y)] = tesela;
            if (tesela.puntos.Count > 0) RecomponerPuntos();
            Debug.Log($"PriceMap: tesela vectorial {zoom}/{x}/{y} construida en {reloj.ElapsedMilliseconds} ms");
            return true;
        }

        // OpenFreeMap envía las teselas comprimidas con gzip; según la plataforma llegan ya descomprimidas o no
        static byte[] Descomprimir(byte[] d)
        {
            if (d.Length < 2 || d[0] != 0x1f || d[1] != 0x8b) return d;
            using var entrada = new GZipStream(new MemoryStream(d), CompressionMode.Decompress);
            using var salida = new MemoryStream();
            entrada.CopyTo(salida);
            return salida.ToArray();
        }

        // ───────────────────────── Construcción ─────────────────────────

        class Malla
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<int> t = new List<int>();
            public bool Vacia => t.Count == 0;
        }

        TeselaCargada Construir(Dictionary<string, CapaMVT> capas, int tx, int ty)
        {
            var resultado = new TeselaCargada();
            var origen = Geo.CentroTesela(tx, ty, zoom, out float lado);
            Vector3 AMundo(Vector2 p, float ext) => origen + new Vector3((p.x / ext - 0.5f) * lado, 0f, (0.5f - p.y / ext) * lado);

            var verde = new Malla(); var agua = new Malla(); var borde = new Malla();
            var calleMenor = new Malla(); var calle = new Malla(); var edificios = new Malla();

            // Zonas verdes
            foreach (var nombre in new[] { "park", "landcover", "landuse" })
            {
                if (!capas.TryGetValue(nombre, out var capa)) continue;
                foreach (var e in capa.elementos)
                {
                    if (e.tipo != TipoGeometria.Poligono) continue;
                    string clase = e.Texto("class");
                    bool esVerde = nombre == "park" || clase == "grass" || clase == "wood" || clase == "forest" || clase == "wetland" ||
                                   clase == "pitch" || clase == "playground" || clase == "park" || clase == "garden" || clase == "cemetery";
                    if (esVerde) Poligono(verde, e, capa.extension, AMundo);
                }
            }

            // Agua (mar, río, lagos, piscinas) y cauces
            if (capas.TryGetValue("water", out var cAgua))
                foreach (var e in cAgua.elementos) if (e.tipo == TipoGeometria.Poligono) Poligono(agua, e, cAgua.extension, AMundo);
            if (capas.TryGetValue("waterway", out var cCauces))
                foreach (var e in cCauces.elementos) if (e.tipo == TipoGeometria.Linea) Linea(agua, e, cCauces.extension, AMundo, 6f);

            // Calles: borde + relleno, anchura según el tipo
            if (capas.TryGetValue("transportation", out var cCalles))
            {
                foreach (var e in cCalles.elementos)
                {
                    string clase = e.Texto("class");
                    if (clase == "ferry" || clase == "rail" || clase == "transit" || clase == "aerialway") continue;
                    if (e.tipo == TipoGeometria.Poligono) { Poligono(calleMenor, e, cCalles.extension, AMundo); continue; } // plazas peatonales, muelles
                    if (e.tipo != TipoGeometria.Linea) continue;
                    float ancho = AnchoCalle(clase);
                    bool principal = clase == "motorway" || clase == "trunk" || clase == "primary" || clase == "secondary" || clase == "tertiary";
                    if (clase != "path" && clase != "track") Linea(borde, e, cCalles.extension, AMundo, ancho + 2.5f);
                    Linea(principal ? calle : calleMenor, e, cCalles.extension, AMundo, ancho);
                }
            }

            // Edificios extruidos (solo los que tienen el centro dentro de la tesela: el margen se repite en la vecina)
            if (capas.TryGetValue("building", out var cEdif))
            {
                foreach (var e in cEdif.elementos)
                {
                    if (e.tipo != TipoGeometria.Poligono) continue;
                    float alto = Mathf.Max(3f, e.Numero("render_height", alturaEdificioPorDefecto));
                    float bajo = e.Numero("render_min_height", 0f);
                    foreach (var (exterior, huecos) in e.Poligonos())
                    {
                        if (!CentroDentro(exterior, cEdif.extension)) continue;
                        Edificio(edificios, exterior, huecos, cEdif.extension, AMundo, bajo, alto);
                    }
                }
            }

            var raiz = new GameObject($"Tesela {zoom}/{tx}/{ty}").transform;
            raiz.SetParent(transform, false);
            Pieza(raiz, "Zonas verdes", verde, mVerde, false);
            Pieza(raiz, "Agua", agua, mAgua, false);
            Pieza(raiz, "Bordes de calle", borde, mBorde, false);
            Pieza(raiz, "Calles secundarias", calleMenor, mCalleMenor, false);
            Pieza(raiz, "Calles principales", calle, mCalle, false);
            Pieza(raiz, "Edificios", edificios, mEdificios, true);

            if (nombresDeCalles && capas.TryGetValue("transportation_name", out var cNombres))
                Nombres(raiz, cNombres, AMundo, resultado.nombres);

            // Puntos de interés: de coordenadas de tesela a lat/lon (los dibuja la UI, encima del mapa)
            if (capas.TryGetValue("poi", out var cPoi))
                resultado.puntos.AddRange(PuntosDe(cPoi, p =>
                {
                    var g = Geo.AGeo(AMundo(p, cPoi.extension));
                    return new Coordenada(g.lat, g.lon);
                }));
            resultado.raiz = raiz;
            return resultado;
        }

        static bool CentroDentro(List<Vector2> anillo, float ext)
        {
            Vector2 c = Vector2.zero; foreach (var p in anillo) c += p; c /= anillo.Count;
            return c.x >= 0 && c.x < ext && c.y >= 0 && c.y < ext;
        }

        static float AnchoCalle(string clase) => clase switch
        {
            "motorway" => 16f, "trunk" => 14f, "primary" => 12f, "secondary" => 10f, "tertiary" => 9f,
            "minor" => 6.5f, "service" => 4.5f, "track" => 3f, "path" => 1.8f, "pier" => 4f, _ => 5f,
        };

        delegate Vector3 Conversor(Vector2 p, float ext);

        void Poligono(Malla m, ElementoMVT e, float ext, Conversor aMundo)
        {
            foreach (var (exterior, huecos) in e.Poligonos())
            {
                var puntos = new List<Vector2>();
                var tris = new List<int>();
                Triangulador.Triangular(A2D(exterior, ext, aMundo), huecos.ConvertAll(h => A2D(h, ext, aMundo)), puntos, tris);
                int b = m.v.Count;
                foreach (var p in puntos) m.v.Add(new Vector3(p.x, 0f, p.y));
                foreach (var i in tris) m.t.Add(b + i);
            }
        }

        static List<Vector2> A2D(List<Vector2> anillo, float ext, Conversor aMundo)
        {
            var r = new List<Vector2>(anillo.Count);
            foreach (var p in anillo) { var w = aMundo(p, ext); r.Add(new Vector2(w.x, w.z)); }
            return r;
        }

        // Cinta de ancho fijo con extremos y uniones redondeadas
        void Linea(Malla m, ElementoMVT e, float ext, Conversor aMundo, float ancho)
        {
            var puntos = new List<Vector3>();
            foreach (var parte in e.partes)
            {
                if (parte.Count < 2) continue;
                puntos.Clear();
                foreach (var p in parte) puntos.Add(aMundo(p, ext));
                Linea(m, puntos, ancho);
            }
        }

        // La misma cinta por puntos del mundo (calles de una tesela o la ruta)
        static void Linea(Malla m, IList<Vector3> puntos, float ancho)
        {
            if (puntos == null || puntos.Count < 2) return;
            float r = ancho / 2f;
            Vector3 prev = puntos[0];
            Disco(m, prev, r);
            for (int i = 1; i < puntos.Count; i++)
            {
                Vector3 p = puntos[i];
                Vector3 d = p - prev;
                if (d.sqrMagnitude < 0.01f) continue;
                Vector3 n = new Vector3(-d.z, 0, d.x).normalized * r;
                int b = m.v.Count;
                m.v.Add(prev + n); m.v.Add(prev - n); m.v.Add(p - n); m.v.Add(p + n);
                m.t.Add(b); m.t.Add(b + 1); m.t.Add(b + 2); m.t.Add(b); m.t.Add(b + 2); m.t.Add(b + 3);
                Disco(m, p, r);
                prev = p;
            }
        }

        static void Disco(Malla m, Vector3 c, float r)
        {
            const int lados = 8;
            int b = m.v.Count;
            m.v.Add(c);
            for (int i = 0; i < lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                m.v.Add(c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
            for (int i = 0; i < lados; i++) { m.t.Add(b); m.t.Add(b + 1 + i); m.t.Add(b + 1 + (i + 1) % lados); }
        }

        void Edificio(Malla m, List<Vector2> exterior, List<List<Vector2>> huecos, float ext, Conversor aMundo, float bajo, float alto)
        {
            var ext2 = A2D(exterior, ext, aMundo);
            var huecos2 = huecos.ConvertAll(h => A2D(h, ext, aMundo));

            // Tejado
            var puntos = new List<Vector2>(); var tris = new List<int>();
            Triangulador.Triangular(ext2, huecos2, puntos, tris);
            int b = m.v.Count;
            foreach (var p in puntos) m.v.Add(new Vector3(p.x, alto, p.y));
            foreach (var i in tris) m.t.Add(b + i);

            // Paredes (contorno exterior y patios)
            Paredes(m, ext2, bajo, alto);
            foreach (var h in huecos2) Paredes(m, h, bajo, alto);
        }

        static void Paredes(Malla m, List<Vector2> anillo, float bajo, float alto)
        {
            for (int i = 0, j = anillo.Count - 1; i < anillo.Count; j = i++)
            {
                Vector2 a = anillo[j], c = anillo[i];
                if ((a - c).sqrMagnitude < 0.01f) continue;
                int b = m.v.Count;
                m.v.Add(new Vector3(a.x, bajo, a.y)); m.v.Add(new Vector3(c.x, bajo, c.y));
                m.v.Add(new Vector3(c.x, alto, c.y)); m.v.Add(new Vector3(a.x, alto, a.y));
                m.t.Add(b); m.t.Add(b + 1); m.t.Add(b + 2); m.t.Add(b); m.t.Add(b + 2); m.t.Add(b + 3);
            }
        }

        static void Pieza(Transform padre, string nombre, Malla m, Material mat, bool proyectaSombra)
        {
            if (m.Vacia) return;
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            var malla = new Mesh { name = nombre, indexFormat = IndexFormat.UInt32 };
            malla.SetVertices(m.v);
            malla.SetTriangles(m.t, 0);
            malla.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = proyectaSombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = !proyectaSombra;
        }

        // Nombres de calle: en el tramo recto más largo de cada calle, tumbados en el suelo y legibles de izquierda a derecha
        void Nombres(Transform raiz, CapaMVT capa, Conversor aMundo, List<(string, Vector3)> colocados)
        {
            float anchoLetra = alturaTexto * 0.55f;
            foreach (var e in capa.elementos)
            {
                string nombre = e.Texto("name");
                if (string.IsNullOrEmpty(nombre) || e.tipo != TipoGeometria.Linea) continue;
                Vector3 mejorA = default, mejorB = default; float mejorLargo = 0;
                foreach (var parte in e.partes)
                    for (int i = 1; i < parte.Count; i++)
                    {
                        if (!Dentro(parte[i - 1], capa.extension) && !Dentro(parte[i], capa.extension)) continue;
                        Vector3 a = aMundo(parte[i - 1], capa.extension), b = aMundo(parte[i], capa.extension);
                        float l = Vector3.Distance(a, b);
                        if (l > mejorLargo) { mejorLargo = l; mejorA = a; mejorB = b; }
                    }
                if (mejorLargo < nombre.Length * anchoLetra * 1.1f) continue;   // no cabe
                Vector3 centro = (mejorA + mejorB) / 2f;
                if (!nombresColocados.TryGetValue(nombre, out var usados)) nombresColocados[nombre] = usados = new List<Vector3>();
                bool cerca = false; foreach (var u in usados) if ((u - centro).sqrMagnitude < 300f * 300f) { cerca = true; break; }
                if (cerca) continue;
                usados.Add(centro);
                colocados.Add((nombre, centro));

                Vector3 dir = (mejorB - mejorA).normalized;
                if (dir.x < 0) dir = -dir;                                   // que se lea de izquierda a derecha
                var go = new GameObject("Nombre: " + nombre);
                go.transform.SetParent(raiz, false);
                go.transform.position = centro + Vector3.up * 0.5f;
                go.transform.rotation = Quaternion.LookRotation(Vector3.down, new Vector3(-dir.z, 0, dir.x));
                var tm = go.AddComponent<TextMesh>();
                tm.text = nombre; tm.font = fuente; tm.fontSize = 64;
                tm.characterSize = alturaTexto * 10f / 64f;
                tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
                tm.color = PaletaMapa.De(tema).etiqueta;
                go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
                etiquetas.Add(tm);
            }
        }

        static bool Dentro(Vector2 p, float ext) => p.x >= 0 && p.x <= ext && p.y >= 0 && p.y <= ext;

        // ───────────────────────── Aspecto ─────────────────────────

        void CrearMateriales()
        {
            var plano = Shader.Find("PriceMap/PlanoPaleta");
            Material Plano(string n, int cola, bool esSuelo)
            {
                var m = new Material(plano) { name = n, renderQueue = cola };
                m.SetFloat("_ZWrite", esSuelo ? 1f : 0f);
                m.SetFloat("_ZTest", (float)(esSuelo ? CompareFunction.LessEqual : CompareFunction.Always));
                return m;
            }
            mSuelo = Plano("Suelo", 1900, true);
            mVerde = Plano("Zonas verdes", 1910, false);
            mAgua = Plano("Agua", 1920, false);
            mBorde = Plano("Bordes de calle", 1930, false);
            mCalleMenor = Plano("Calles secundarias", 1940, false);
            mCalle = Plano("Calles principales", 1950, false);
            mEdificios = new Material(Shader.Find("PriceMap/EdificiosPaleta")) { name = "Edificios" };
        }

        void CrearSuelo()
        {
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            suelo.name = "Suelo";
            Destroy(suelo.GetComponent<Collider>());
            suelo.transform.SetParent(transform, false);
            suelo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            suelo.transform.localScale = new Vector3(20000f, 20000f, 1f);
            var r = suelo.GetComponent<MeshRenderer>();
            r.sharedMaterial = mSuelo;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        void AplicarTema()
        {
            temaAplicado = tema;
            var p = PaletaMapa.De(tema);
            mSuelo.color = p.fondo; mVerde.color = p.verde; mAgua.color = p.agua;
            mBorde.color = p.borde; mCalleMenor.color = p.calleMenor; mCalle.color = p.calle;
            Shader.SetGlobalColor("_PL_Edificio", p.edificio);
            Shader.SetGlobalColor("_PL_Fachada", p.fachada);
            Shader.SetGlobalColor("_PL_Sombra", p.sombra);
            etiquetas.RemoveAll(t => t == null); // las de teselas liberadas
            foreach (var t in etiquetas) t.color = p.etiqueta;
            if (Camera.main != null) Camera.main.backgroundColor = p.fondo;
        }

        void PrepararSombras()
        {
            QualitySettings.shadows = sombras ? ShadowQuality.All : ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            var sol = FindFirstObjectByType<Light>();
            if (sol != null && sol.type == LightType.Directional)
            {
                sol.shadows = LightShadows.Soft;
                sol.shadowStrength = 1f;
                sol.shadowBias = 0.2f; sol.shadowNormalBias = 0f;
                sol.transform.rotation = Quaternion.Euler(42f, 35f, 0f); // sol de tarde desde el suroeste
            }
        }
    }
}
