using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PrecioLocal.EditorTools
{
    /// <summary>
    /// Crea la escena "Mapa" lista para trabajar: mapa real de Sanlúcar por teselas, cámara 2.5D,
    /// luz, tu posición simulada y algunos sitios de ejemplo del prototipo web.
    /// Menú: PrecioLocal ▸ Preparar escena del mapa (la vuelve a generar desde cero).
    /// </summary>
    public static class PrepararEscena
    {
        const string RutaEscena = "Assets/Scenes/Mapa.unity";
        const string CarpetaMateriales = "Assets/Materiales";

        // Sitios reales del prototipo web (prototipo/datos-lugares.js y datos-precios-web.js)
        internal static readonly (string nombre, TipoMarcador tipo, double lat, double lon)[] Sitios =
        {
            ("Clínica Areas", TipoMarcador.Servicio, 36.780143, -6.35088),
            ("Casa Ballén", TipoMarcador.Hosteleria, 36.778127, -6.353815),
            ("Padel La Jara", TipoMarcador.Ocio, 36.770616, -6.368991),
            ("ChemaBarber", TipoMarcador.Servicio, 36.7601585, -6.3624415),
        };

        [MenuItem("PrecioLocal/Preparar escena del mapa")]
        public static void Crear()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(CarpetaMateriales);
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Luz
            var luz = new GameObject("Luz direccional").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.75f);

            // Cámara 2.5D (vista inicial del prototipo web: inclinación 50°, giro -15°)
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xE9, 0xED, 0xF7, 0xFF); // fondo de la paleta B
            cam.fieldOfView = 45f;
            camGo.AddComponent<AudioListener>();
            var cm = camGo.AddComponent<CamaraMapa>();
            cm.distancia = 900f; cm.inclinacion = 50f; cm.giro = -15f;
            camGo.transform.SetPositionAndRotation(Quaternion.Euler(50f, -15f, 0f) * Vector3.back * 900f, Quaternion.Euler(50f, -15f, 0f));

            // Mapa
            var mapa = new GameObject("Mapa").AddComponent<MapaTeselas>();
            mapa.materialTesela = Material("Tesela", Shader.Find("PrecioLocal/TeselaPaleta") ?? Shader.Find("Unlit/Texture"), Color.white);
            Geo.LatitudOrigen = mapa.latitud; Geo.LongitudOrigen = mapa.longitud;

            // Suelo de referencia (visible en el editor antes de pulsar Play; las teselas se cargan al ejecutar)
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo (referencia)";
            Object.DestroyImmediate(suelo.GetComponent<Collider>());
            suelo.transform.position = new Vector3(0f, -0.5f, 0f);
            suelo.transform.localScale = new Vector3(460f, 1f, 460f); // ~4,6 km de lado
            suelo.GetComponent<Renderer>().sharedMaterial = Material("Suelo", Shader.Find("Unlit/Color"), new Color32(0xDC, 0xE3, 0xF1, 0xFF));

            // Marcadores
            var raiz = new GameObject("Marcadores").transform;
            CrearPosicion(raiz);
            foreach (var s in Sitios) CrearSitio(raiz, s.nombre, s.tipo, s.lat, s.lon);

            EditorSceneManager.SaveScene(escena, RutaEscena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PrecioLocal: escena del mapa creada en " + RutaEscena + ". Pulsa Play para cargar el mapa real.");
        }

        static Material Material(string nombre, Shader shader, Color color)
        {
            string ruta = $"{CarpetaMateriales}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = shader;
            if (m.HasProperty("_Color")) m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Opaco(TipoMarcador t) => Material("Marcador " + t, Shader.Find("Standard"), Marcador.ColorDe(t));

        internal static GameObject CrearPosicion(Transform raiz)
        {
            var go = new GameObject("Tu posición (simulada)");
            go.transform.SetParent(raiz);
            var m = go.AddComponent<Marcador>();
            m.nombre = "Tu posición"; m.tipo = TipoMarcador.Posicion; m.latitud = 36.7779; m.longitud = -6.3528;
            var borde = Pieza(go.transform, PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0), new Vector3(26, 0.6f, 26), Material("Blanco", Shader.Find("Standard"), Color.white));
            borde.name = "Borde";
            Pieza(go.transform, PrimitiveType.Cylinder, new Vector3(0, 1.2f, 0), new Vector3(18, 0.8f, 18), Opaco(TipoMarcador.Posicion)).name = "Punto";
            m.Colocar();
            return go;
        }

        internal static GameObject CrearSitio(Transform raiz, string nombre, TipoMarcador tipo, double lat, double lon)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz);
            var m = go.AddComponent<Marcador>();
            m.nombre = nombre; m.tipo = tipo; m.latitud = lat; m.longitud = lon;
            Pieza(go.transform, PrimitiveType.Cylinder, new Vector3(0, 15, 0), new Vector3(2.5f, 15, 2.5f), Material("Blanco", Shader.Find("Standard"), Color.white)).name = "Palo";
            Pieza(go.transform, PrimitiveType.Cube, new Vector3(0, 38, 0), new Vector3(18, 18, 18), Opaco(tipo)).name = "Cabeza";

            var texto = new GameObject("Etiqueta");
            texto.transform.SetParent(go.transform, false);
            texto.transform.localPosition = new Vector3(0, 58, 0);
            var tm = texto.AddComponent<TextMesh>();
            tm.text = nombre;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            tm.fontSize = 64; tm.characterSize = 1.6f;
            tm.anchor = TextAnchor.LowerCenter; tm.alignment = TextAlignment.Center;
            tm.color = new Color32(0x13, 0x23, 0x5B, 0xFF);
            m.etiqueta = texto.transform;
            m.Colocar();
            return go;
        }

        static GameObject Pieza(Transform padre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat)
        {
            var p = GameObject.CreatePrimitive(tipo);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.transform.SetParent(padre, false);
            p.transform.localPosition = pos; p.transform.localScale = escala;
            p.GetComponent<Renderer>().sharedMaterial = mat;
            return p;
        }
    }

    /// <summary>La primera vez que se abre el proyecto, carga la escena del mapa.</summary>
    [InitializeOnLoad]
    static class AbrirEscenaMapa
    {
        static AbrirEscenaMapa()
        {
            if (Application.isBatchMode || SessionState.GetBool("PrecioLocal.EscenaAbierta", false)) return;
            SessionState.SetBool("PrecioLocal.EscenaAbierta", true);
            EditorApplication.delayCall += () =>
            {
                var activa = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(activa.path) && File.Exists("Assets/Scenes/Mapa.unity"))
                    EditorSceneManager.OpenScene("Assets/Scenes/Mapa.unity");
            };
        }
    }
}
