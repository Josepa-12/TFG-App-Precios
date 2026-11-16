using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PrecioLocal
{
    /// <summary>
    /// Mapa real por teselas (imágenes de 256×256 del esquema XYZ de OpenStreetMap) colocadas en el suelo
    /// de la escena, a escala 1 unidad = 1 metro. Primer paso de la prueba técnica: luego se podrán sustituir
    /// por teselas vectoriales con estilo propio (OpenFreeMap / Mapbox) y edificios extruidos.
    ///
    /// Uso responsable del servidor de OpenStreetMap: pocas descargas a la vez, User-Agent propio y
    /// caché en disco (cada tesela se descarga una sola vez). Atribución obligatoria en pantalla.
    /// </summary>
    public class MapaTeselas : MonoBehaviour
    {
        [Header("Centro del mapa (origen de la escena)")]
        public double latitud = 36.7779;   // Plaza del Cabildo
        public double longitud = -6.3528;

        [Header("Teselas")]
        [Range(12, 18)] public int zoom = 16;          // z16 ≈ 490 m por tesela en Sanlúcar
        [Range(1, 6)] public int radio = 4;            // 4 → 9×9 teselas (unos 4,4 km de lado)
        public string plantillaUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
        public string userAgent = "PrecioLocal-TFG-prototipo/0.1 (Unity)";
        [Range(1, 4)] public int descargasSimultaneas = 2;
        public bool usarCache = true;

        [Header("Aspecto")]
        public Material materialTesela;               // PrecioLocal/TeselaPaleta (recolorea con la paleta del proyecto)
        [Tooltip("Original = colores de OpenStreetMap; Claro/Oscuro = paleta del prototipo web. En Play: tecla T para cambiar.")]
        public TemaMapa tema = TemaMapa.Claro;

        readonly Queue<(int x, int y, Renderer r)> cola = new Queue<(int, int, Renderer)>();
        readonly List<Material> materiales = new List<Material>();
        TemaMapa temaAplicado = (TemaMapa)(-1);

        void Awake()
        {
            Geo.LatitudOrigen = latitud;
            Geo.LongitudOrigen = longitud;
        }

        void Start()
        {
            var centro = Geo.Tesela(latitud, longitud, zoom);
            for (int dy = -radio; dy <= radio; dy++)
                for (int dx = -radio; dx <= radio; dx++)
                    CrearTesela(centro.x + dx, centro.y + dy);
            AplicarTema();
            for (int i = 0; i < descargasSimultaneas; i++) StartCoroutine(Descargador());
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T)) tema = (TemaMapa)(((int)tema + 1) % 3);
            if (tema != temaAplicado) AplicarTema();
        }

        /// <summary>Pasa los colores del tema a todas las teselas y al fondo de la cámara.</summary>
        void AplicarTema()
        {
            temaAplicado = tema;
            var p = PaletaMapa.De(tema);
            foreach (var m in materiales)
            {
                m.SetFloat("_Intensidad", tema == TemaMapa.Original ? 0f : 1f);
                m.SetColor("_Fondo", p.fondo); m.SetColor("_Agua", p.agua); m.SetColor("_Verde", p.verde);
                m.SetColor("_Calle", p.calle); m.SetColor("_Edificio", p.edificio); m.SetColor("_Etiqueta", p.etiqueta);
            }
            if (texturaCarga != null) { texturaCarga.SetPixel(0, 0, p.fondo); texturaCarga.Apply(); }
            if (Camera.main != null) Camera.main.backgroundColor = p.fondo;
        }

        void CrearTesela(int x, int y)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"Tesela {zoom}/{x}/{y}";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Geo.CentroTesela(x, y, zoom, out float lado);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // tumbada en el suelo, norte = +Z
            go.transform.localScale = new Vector3(lado, lado, 1f);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = materialTesela != null ? materialTesela.shader : Shader.Find("PrecioLocal/TeselaPaleta");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            r.material = new Material(shader);
            r.material.mainTexture = TexturaCarga();
            materiales.Add(r.material);
            cola.Enqueue((x, y, r));
        }

        IEnumerator Descargador()
        {
            while (cola.Count > 0)
            {
                var (x, y, r) = cola.Dequeue();
                yield return CargarTesela(x, y, r);
            }
        }

        string RutaCache(int x, int y) => Path.Combine(Application.persistentDataPath, "teselas", zoom.ToString(), x.ToString(), y + ".png");

        IEnumerator CargarTesela(int x, int y, Renderer r)
        {
            string ruta = RutaCache(x, y);
            Texture2D tex = null;
            if (usarCache && File.Exists(ruta))
            {
                tex = new Texture2D(256, 256, TextureFormat.RGB24, false);
                tex.LoadImage(File.ReadAllBytes(ruta));
            }
            else
            {
                string url = plantillaUrl.Replace("{z}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
                using var req = UnityWebRequestTexture.GetTexture(url, true);
                req.SetRequestHeader("User-Agent", userAgent);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"No se pudo descargar la tesela {zoom}/{x}/{y}: {req.error}");
                    yield break;
                }
                tex = DownloadHandlerTexture.GetContent(req);
                if (usarCache)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                    File.WriteAllBytes(ruta, req.downloadHandler.data);
                }
            }
            if (r == null) yield break;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;
            r.material.mainTexture = tex;
        }

        Texture2D texturaCarga;
        Texture2D TexturaCarga() // color liso mientras llega la imagen (Unlit/Texture no tiene propiedad de color)
        {
            if (texturaCarga != null) return texturaCarga;
            texturaCarga = new Texture2D(1, 1);
            texturaCarga.SetPixel(0, 0, PaletaMapa.De(tema).fondo);
            texturaCarga.Apply();
            return texturaCarga;
        }

        // Atribución obligatoria de OpenStreetMap y aviso del tema actual
        void OnGUI()
        {
            var estilo = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.LowerRight };
            estilo.normal.textColor = PaletaMapa.De(tema).etiqueta;
            GUI.Label(new Rect(0, Screen.height - 24, Screen.width - 8, 20), "© colaboradores de OpenStreetMap", estilo);
            estilo.alignment = TextAnchor.LowerLeft;
            GUI.Label(new Rect(8, Screen.height - 24, 300, 20), $"Tema: {tema} (T para cambiar)", estilo);
        }
    }

    public enum TemaMapa { Original, Claro, Oscuro }

    /// <summary>Paletas del mapa, copiadas del prototipo web (prototipo/app.js, colores light/dark).</summary>
    public struct PaletaMapa
    {
        public Color fondo, agua, verde, calle, edificio, etiqueta;
        public Color fachada; // lado en sombra de los edificios 3D (escena de Cesium)
        public Color sombra;  // multiplicador de las sombras proyectadas (azulado, como la paleta)

        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public static PaletaMapa De(TemaMapa t) => t == TemaMapa.Oscuro
            ? new PaletaMapa { fondo = H("#141B30"), agua = H("#0A0F1F"), verde = H("#18213A"), calle = H("#2A3554"), edificio = H("#2A3A63"), fachada = H("#1A2442"), etiqueta = H("#9AA6C4"), sombra = H("#8A90A8") }
            : t == TemaMapa.Claro
            ? new PaletaMapa { fondo = H("#E9EDF7"), agua = H("#BFD0F4"), verde = H("#DCE6EE"), calle = H("#FFFFFF"), edificio = H("#D3DCF2"), fachada = H("#97A6D0"), etiqueta = H("#4B5878"), sombra = H("#A7B1D4") }
            : new PaletaMapa { fondo = H("#F2EFE9"), agua = H("#AAD3DF"), verde = H("#C8FACC"), calle = H("#FFFFFF"), edificio = H("#D9D0C9"), fachada = H("#A89A8E"), etiqueta = H("#13235B"), sombra = H("#B8B8C0") };
    }
}
