using System.IO;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Crea la escena principal de la app: el mapa vectorial de Sanlúcar (OpenStreetMap vía OpenFreeMap),
    /// la cámara 2.5D, la interfaz (UIDocument con PantallaMapa) y el Arranque, que conecta las piezas (AD-2).
    /// Incluye la posición simulada (hasta la historia 1.7). Los lugares no van en la escena: los dibuja la
    /// interfaz encima del mapa (CapaMarcadores, historia 1.4).
    /// Menú: PriceMap ▸ Preparar escena principal. Es la única escena del build.
    /// </summary>
    public static class PrepararEscenaPrincipal
    {
        public const string RutaEscena = "Assets/Scenes/Principal.unity";
        const string CarpetaMateriales = "Assets/Materiales";

        [MenuItem("PriceMap/Preparar escena principal")]
        public static void Crear()
        {
            // Regenerar sustituye la escena abierta: antes, ofrecer guardar lo que haya sin guardar
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepararInterfaz.Asegurar();
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(CarpetaMateriales);
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var luz = new GameObject("Luz direccional").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(42f, 35f, 0f);
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

            var mapa = new GameObject("Mapa vectorial (OpenStreetMap)").AddComponent<MapaVectorial>();
            Geo.LatitudOrigen = mapa.latitud; Geo.LongitudOrigen = mapa.longitud;

            var raiz = new GameObject("Marcadores").transform;
            CrearPosicion(raiz);

            // Interfaz (UI Toolkit) sobre el mapa
            var pantalla = new GameObject("Pantalla del mapa").AddComponent<PantallaMapa>();
            var documento = pantalla.GetComponent<UIDocument>();
            documento.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PrepararInterfaz.RutaPanelSettings);
            documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PrepararInterfaz.RutaPantallaMapa);

            // Raíz de composición: referencias puestas aquí para no tener que buscarlas al arrancar
            var arranque = new GameObject("Arranque").AddComponent<Arranque.Arranque>();
            var so = new SerializedObject(arranque);
            so.FindProperty("mapa").objectReferenceValue = mapa;
            so.FindProperty("pantallaMapa").objectReferenceValue = pantalla;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(escena, RutaEscena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PriceMap: escena principal creada en " + RutaEscena + ". Pulsa Play para cargar el mapa.");
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

        static void CrearPosicion(Transform raiz)
        {
            var go = new GameObject("Tu posición (simulada)");
            go.transform.SetParent(raiz);
            var m = go.AddComponent<Marcador>();
            m.nombre = "Tu posición"; m.tipo = TipoMarcador.Posicion; m.latitud = 36.7779; m.longitud = -6.3528;
            Pieza(go.transform, PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0), new Vector3(26, 0.6f, 26), Material("Blanco", Shader.Find("Standard"), Color.white)).name = "Borde";
            Pieza(go.transform, PrimitiveType.Cylinder, new Vector3(0, 1.2f, 0), new Vector3(18, 0.8f, 18), Opaco(TipoMarcador.Posicion)).name = "Punto";
            m.Colocar();
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

        /// <summary>
        /// Una vez por sesión del editor (no en cada recompilación): crea la escena si falta y,
        /// si no hay ninguna abierta, abre la principal, sin perder cambios sin guardar.
        /// </summary>
        [InitializeOnLoadMethod]
        static void AlAbrirElProyecto()
        {
            const string Clave = "PriceMap.EscenaAbierta";
            if (Application.isBatchMode || SessionState.GetBool(Clave, false)) return;
            SessionState.SetBool(Clave, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(RutaEscena)) { Crear(); return; }
                var activa = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(activa.path) && !activa.isDirty)
                    EditorSceneManager.OpenScene(RutaEscena);
            };
        }
    }
}
