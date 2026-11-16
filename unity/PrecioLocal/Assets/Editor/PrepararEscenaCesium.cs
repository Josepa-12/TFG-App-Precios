using System.Collections.Generic;
using CesiumForUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PrecioLocal.EditorTools
{
    /// <summary>
    /// Crea la escena "MapaCesium" (opción B de la prueba técnica): Sanlúcar sobre el globo real de
    /// Cesium for Unity, con relieve (Cesium World Terrain), el mapa de OpenStreetMap encima y los
    /// edificios en 3D de OpenStreetMap (Cesium OSM Buildings). Usa la misma cámara 2.5D y los mismos
    /// marcadores que la escena "Mapa", así se pueden comparar.
    ///
    /// Requiere una cuenta gratuita de Cesium ion y su token por defecto
    /// (menú Cesium ▸ Cesium ▸ Connect to Cesium ion ▸ Token).
    /// Menú: PrecioLocal ▸ Preparar escena con Cesium (3D).
    /// </summary>
    public static class PrepararEscenaCesium
    {
        const string RutaEscena = "Assets/Scenes/MapaCesium.unity";

        // Plaza del Cabildo. La altura es sobre el elipsoide (no sobre el nivel del mar): en Sanlúcar el
        // geoide está unos 50 m por encima del elipsoide y el suelo a unos 10-30 m, así que el suelo queda
        // cerca de y = 0 en la escena (la cámara 2.5D usa ese plano para arrastrar y hacer zoom).
        const double Latitud = 36.7779, Longitud = -6.3528, AlturaSuelo = 60;

        // Recursos públicos de Cesium ion
        const long IonTerreno = 1;          // Cesium World Terrain
        const long IonEdificios = 96188;    // Cesium OSM Buildings

        [MenuItem("PrecioLocal/Preparar escena con Cesium (3D)")]
        public static void Crear()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Luz
            var luz = new GameObject("Luz direccional").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.75f);

            // Origen geográfico: todo lo que cuelga de aquí se coloca en coordenadas reales
            var geoGo = new GameObject("CesiumGeoreference");
            var geo = geoGo.AddComponent<CesiumGeoreference>();
            geo.SetOriginLongitudeLatitudeHeight(Longitud, Latitud, AlturaSuelo);

            // Terreno con el mapa de OpenStreetMap encima
            var terrenoGo = new GameObject("Terreno + mapa OSM");
            terrenoGo.transform.SetParent(geoGo.transform, false);
            var terreno = terrenoGo.AddComponent<Cesium3DTileset>();
            terreno.tilesetSource = CesiumDataSource.FromCesiumIon;
            terreno.ionAssetID = IonTerreno;
            terreno.createPhysicsMeshes = false; // no hay física: ahorra memoria y evita avisos de triángulos grandes
            var osm = terrenoGo.AddComponent<CesiumUrlTemplateRasterOverlay>();
            // En Cesium {y} cuenta desde el sur; OSM (esquema XYZ) cuenta desde el norte → {reverseY}
            osm.templateUrl = "https://tile.openstreetmap.org/{z}/{x}/{reverseY}.png";
            osm.projection = CesiumUrlTemplateRasterOverlayProjection.WebMercator;
            osm.maximumLevel = 19;
            osm.maximumSimultaneousTileLoads = 2; // política de uso de tile.openstreetmap.org
            osm.requestHeaders = new List<CesiumUrlTemplateRasterOverlay.HeaderEntry>
            {
                new CesiumUrlTemplateRasterOverlay.HeaderEntry { Name = "User-Agent", Value = "PrecioLocal-TFG-prototipo/0.1 (Unity; Cesium)" },
            };

            // Edificios en 3D
            var edificiosGo = new GameObject("Edificios OSM (3D)");
            edificiosGo.transform.SetParent(geoGo.transform, false);
            var edificios = edificiosGo.AddComponent<Cesium3DTileset>();
            edificios.tilesetSource = CesiumDataSource.FromCesiumIon;
            edificios.ionAssetID = IonEdificios;
            edificios.createPhysicsMeshes = false;

            // Cámara 2.5D (la misma que la escena "Mapa")
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xE9, 0xED, 0xF7, 0xFF);
            cam.fieldOfView = 45f;
            camGo.AddComponent<AudioListener>();
            var cm = camGo.AddComponent<CamaraMapa>();
            cm.distancia = 900f; cm.inclinacion = 50f; cm.giro = -15f;
            camGo.transform.SetPositionAndRotation(Quaternion.Euler(50f, -15f, 0f) * Vector3.back * 900f, Quaternion.Euler(50f, -15f, 0f));

            // Marcadores: los coloca CesiumGlobeAnchor por latitud/longitud sobre el globo real
            var raiz = new GameObject("Marcadores").transform;
            raiz.SetParent(geoGo.transform, false);
            Anclar(PrepararEscena.CrearPosicion(raiz), 36.7779, -6.3528);
            foreach (var s in PrepararEscena.Sitios)
                Anclar(PrepararEscena.CrearSitio(raiz, s.nombre, s.tipo, s.lat, s.lon), s.lat, s.lon);

            EditorSceneManager.SaveScene(escena, RutaEscena);
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!escenas.Exists(e => e.path == RutaEscena)) escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("PrecioLocal: escena con Cesium creada en " + RutaEscena +
                      ". Si no se ve nada, conecta tu cuenta en Cesium ▸ Cesium ▸ Connect to Cesium ion y crea el token.");
        }

        /// <summary>Si la escena aún no existe, la crea sola al abrir o recompilar el proyecto.</summary>
        [InitializeOnLoadMethod]
        static void CrearSiFalta()
        {
            if (Application.isBatchMode || System.IO.File.Exists(RutaEscena)) return;
            EditorApplication.delayCall += () =>
            {
                if (System.IO.File.Exists(RutaEscena) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Crear();
            };
        }

        static void Anclar(GameObject go, double lat, double lon)
        {
            go.GetComponent<Marcador>().colocarConGeo = false;
            go.transform.localPosition = Vector3.zero;
            var ancla = go.AddComponent<CesiumGlobeAnchor>();
            ancla.longitudeLatitudeHeight = new Unity.Mathematics.double3(lon, lat, AlturaSuelo);
        }
    }
}
