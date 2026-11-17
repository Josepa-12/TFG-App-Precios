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
        [Tooltip("1 → 3×3 teselas (unos 6 km de lado)")]
        [Range(0, 2)] public int radio = 1;
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

            // Dirección de las teselas (incluye la versión de los datos, cambia con cada actualización)
            string plantilla = null;
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
            if (plantilla == null) { Debug.LogWarning("PriceMap: no se pudo obtener la dirección de las teselas de OpenFreeMap."); yield break; }

            // Teselas alrededor del centro, de dentro hacia fuera
            var centro = Geo.Tesela(latitud, longitud, zoom);
            var lista = new List<Vector2Int>();
            for (int dy = -radio; dy <= radio; dy++)
                for (int dx = -radio; dx <= radio; dx++) lista.Add(new Vector2Int(centro.x + dx, centro.y + dy));
            lista.Sort((a, b) => (a - centro).sqrMagnitude.CompareTo((b - centro).sqrMagnitude));
            foreach (var t in lista) yield return CargarTesela(plantilla, t.x, t.y);
        }

        [System.Serializable] class TileJson { public string[] tiles; }

        // ───────────────────────── Puerto IMapa ─────────────────────────

        /// <summary>Centra la cámara del mapa en una coordenada real.</summary>
        public void Centrar(Coordenada destino)
        {
            var c = Camara();
            if (c != null) c.CentrarEn(destino.Latitud, destino.Longitud);
        }

        /// <summary>Pinta el mapa con la paleta del tema; se aplica en el siguiente fotograma.</summary>
        public void AplicarTema(Tema nuevo) => tema = nuevo == Tema.Oscuro ? TemaMapa.Oscuro : TemaMapa.Claro;

        public void Acercar() { var c = Camara(); if (c != null) c.Acercar(); }

        /// <summary>Para no mover el mapa cuando el dedo empieza sobre un control de la interfaz.</summary>
        public System.Func<Vector2, bool> EntradaBloqueada
        {
            set { entradaBloqueada = value; Camara(); }
        }
        System.Func<Vector2, bool> entradaBloqueada; // se pasa a la cámara en cuanto exista
        public void Alejar() { var c = Camara(); if (c != null) c.Alejar(); }

        // Se busca al usarla: Centrar puede llamarse antes que Start
        CamaraMapa Camara()
        {
            if (camara == null) camara = FindFirstObjectByType<CamaraMapa>();
            if (camara != null && entradaBloqueada != null) camara.entradaBloqueada = entradaBloqueada;
            return camara;
        }

        void Update()
        {
            if (mSuelo != null && tema != temaAplicado) AplicarTema(); // el tema puede llegar antes que Start
            if (camara != null && sombras) QualitySettings.shadowDistance = Mathf.Clamp(camara.distancia * 2.2f, 250f, 5000f);
        }

        // ───────────────────────── Descarga ─────────────────────────

        IEnumerator CargarTesela(string plantilla, int x, int y)
        {
            string ruta = Path.Combine(Application.persistentDataPath, "vectorial", zoom.ToString(), x.ToString(), y + ".pbf");
            byte[] datos = null;
            if (File.Exists(ruta)) datos = File.ReadAllBytes(ruta);
            else
            {
                string url = plantilla.Replace("{z}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
                using var req = UnityWebRequest.Get(url);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning($"PriceMap: no se pudo descargar la tesela {zoom}/{x}/{y}: {req.error}"); yield break; }
                datos = Descomprimir(req.downloadHandler.data);
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                File.WriteAllBytes(ruta, datos);
            }

            var reloj = System.Diagnostics.Stopwatch.StartNew();
            Dictionary<string, CapaMVT> capas;
            try { capas = LectorMVT.Leer(datos); }
            catch (System.Exception e) { Debug.LogWarning($"PriceMap: tesela {zoom}/{x}/{y} ilegible: {e.Message}"); yield break; }
            Construir(capas, x, y);
            Debug.Log($"PriceMap: tesela vectorial {zoom}/{x}/{y} construida en {reloj.ElapsedMilliseconds} ms");
            yield return null; // reparte el trabajo entre fotogramas
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

        void Construir(Dictionary<string, CapaMVT> capas, int tx, int ty)
        {
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
                Nombres(raiz, cNombres, AMundo);
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
            float r = ancho / 2f;
            foreach (var parte in e.partes)
            {
                if (parte.Count < 2) continue;
                Vector3 prev = aMundo(parte[0], ext);
                Disco(m, prev, r);
                for (int i = 1; i < parte.Count; i++)
                {
                    Vector3 p = aMundo(parte[i], ext);
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
        void Nombres(Transform raiz, CapaMVT capa, Conversor aMundo)
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
            foreach (var t in etiquetas) if (t != null) t.color = p.etiqueta;
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
