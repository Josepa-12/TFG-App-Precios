using CesiumForUnity;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrecioLocal
{
    /// <summary>
    /// Pinta la escena de Cesium con la paleta del proyecto (la misma que el prototipo web):
    ///  - terreno: las teselas de OpenStreetMap recoloreadas (shader CesiumTerrenoPaleta);
    ///  - edificios 3D: color de edificio de la paleta, fachadas sombreadas (shader CesiumEdificiosPaleta).
    /// En Play, la tecla T cambia de tema: Claro → Oscuro → Original.
    /// Se añade sola a cualquier escena que tenga tilesets de Cesium (no hace falta regenerar la escena).
    /// </summary>
    public class TemaCesium : MonoBehaviour
    {
        public TemaMapa tema = TemaMapa.Claro;
        TemaMapa aplicado = (TemaMapa)(-1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AnadirSiHayCesium()
        {
            if (FindFirstObjectByType<TemaCesium>() != null) return;
            if (FindObjectsByType<Cesium3DTileset>(FindObjectsSortMode.None).Length == 0) return;
            new GameObject("Tema del mapa (Cesium)").AddComponent<TemaCesium>();
        }

        // Avisos en la consola si falla la carga del mapa o de un tileset
        static void AlFallarMapa(CesiumRasterOverlayLoadFailureDetails d) =>
            Debug.LogWarning($"PrecioLocal: no se pudo cargar el mapa ({d.type}) HTTP {d.httpStatusCode}: {d.message}");
        static void AlFallarTileset(Cesium3DTilesetLoadFailureDetails d) =>
            Debug.LogWarning($"PrecioLocal: no se pudo cargar {d.tileset?.name}: {d.message}");

        void OnEnable()
        {
            CesiumRasterOverlay.OnCesiumRasterOverlayLoadFailure += AlFallarMapa;
            Cesium3DTileset.OnCesium3DTilesetLoadFailure += AlFallarTileset;
        }

        void OnDisable()
        {
            CesiumRasterOverlay.OnCesiumRasterOverlayLoadFailure -= AlFallarMapa;
            Cesium3DTileset.OnCesium3DTilesetLoadFailure -= AlFallarTileset;
        }

        void Start()
        {
            var sTerreno = Shader.Find("PrecioLocal/CesiumTerrenoPaleta");
            var sEdificios = Shader.Find("PrecioLocal/CesiumEdificiosPaleta");
            foreach (var t in FindObjectsByType<Cesium3DTileset>(FindObjectsSortMode.None))
            {
                // El tileset que lleva el mapa encima es el terreno; el resto, edificios
                bool esTerreno = t.GetComponent<CesiumRasterOverlay>() != null;
                var shader = esTerreno ? sTerreno : sEdificios;
                if (shader == null) { Debug.LogWarning("PrecioLocal: no se encuentra el shader de la paleta para " + t.name); continue; }
                // Sombras: los edificios las proyectan; el suelo las recibe
                var modo = esTerreno ? ShadowCastingMode.Off : ShadowCastingMode.On;
                t.OnTileGameObjectCreated += go =>
                {
                    foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { r.shadowCastingMode = modo; r.receiveShadows = esTerreno; }
                };
                t.opaqueMaterial = new Material(shader) { name = esTerreno ? "Terreno (paleta)" : "Edificios (paleta)" }; // recarga el tileset
            }
            camara = FindFirstObjectByType<CamaraMapa>();
            PrepararSombras();
            Aplicar();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T)) tema = (TemaMapa)(((int)tema + 1) % 3);
            if (Input.GetKeyDown(KeyCode.I)) quitarIconos = !quitarIconos;
            if (tema != aplicado) Aplicar();
            Shader.SetGlobalFloat("_PL_QuitarIconos", quitarIconos ? 1f : 0f);
            // La distancia de sombra sigue al zoom: lo justo para cubrir lo que se ve, con la máxima nitidez
            if (camara != null && sombras) QualitySettings.shadowDistance = Mathf.Clamp(camara.distancia * 2.2f, 250f, 5000f);
        }

        [Header("Sombras y limpieza del mapa")]
        public bool sombras = true;
        [Tooltip("Quita los iconos de OSM (restaurantes, tiendas, farmacias...). En Play: tecla I")]
        public bool quitarIconos = true;
        CamaraMapa camara;

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
                // Sol de tarde (desde el suroeste, algo bajo) para sombras largas y legibles
                sol.transform.rotation = Quaternion.Euler(42f, 35f, 0f);
            }
        }

        void Aplicar()
        {
            aplicado = tema;
            var p = PaletaMapa.De(tema);
            Shader.SetGlobalColor("_PL_Fondo", p.fondo);
            Shader.SetGlobalColor("_PL_Agua", p.agua);
            Shader.SetGlobalColor("_PL_Verde", p.verde);
            Shader.SetGlobalColor("_PL_Calle", p.calle);
            Shader.SetGlobalColor("_PL_Edificio", p.edificio);
            Shader.SetGlobalColor("_PL_Fachada", p.fachada);
            Shader.SetGlobalColor("_PL_Etiqueta", p.etiqueta);
            Shader.SetGlobalColor("_PL_Sombra", p.sombra);
            Shader.SetGlobalFloat("_PL_Intensidad", tema == TemaMapa.Original ? 0f : 1f);
            if (Camera.main != null) Camera.main.backgroundColor = p.fondo;
        }

        void OnGUI()
        {
            var estilo = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.LowerLeft };
            estilo.normal.textColor = PaletaMapa.De(tema).etiqueta;
            GUI.Label(new Rect(8, Screen.height - 24, 500, 20), $"Tema: {tema} (T)   ·   Iconos OSM: {(quitarIconos ? "ocultos" : "visibles")} (I)", estilo);
        }
    }
}
