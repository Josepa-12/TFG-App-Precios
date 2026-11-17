using System.Collections.Generic;
using System.IO;
using PrecioLocal.Vectorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PrecioLocal.EditorTools
{
    /// <summary>
    /// Crea la escena "MapaVectorial" (opción C de la prueba técnica): el mapa de Sanlúcar dibujado en Unity
    /// a partir de las teselas vectoriales de OpenFreeMap (las del prototipo web), con calles, agua, zonas
    /// verdes, edificios extruidos y nombres de calles, todo con la paleta del proyecto. Sin Cesium ni cuentas.
    /// Misma cámara 2.5D y mismos marcadores que la escena "Mapa".
    /// Menú: PrecioLocal ▸ Preparar escena vectorial (OpenFreeMap).
    /// </summary>
    public static class PrepararEscenaVectorial
    {
        const string RutaEscena = "Assets/Scenes/MapaVectorial.unity";

        [MenuItem("PrecioLocal/Preparar escena vectorial (OpenFreeMap)")]
        public static void Crear()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var luz = new GameObject("Luz direccional").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(42f, 35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.75f);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xE9, 0xED, 0xF7, 0xFF);
            cam.fieldOfView = 45f;
            camGo.AddComponent<AudioListener>();
            var cm = camGo.AddComponent<CamaraMapa>();
            cm.distancia = 900f; cm.inclinacion = 50f; cm.giro = -15f;
            camGo.transform.SetPositionAndRotation(Quaternion.Euler(50f, -15f, 0f) * Vector3.back * 900f, Quaternion.Euler(50f, -15f, 0f));

            var mapa = new GameObject("Mapa vectorial (OpenFreeMap)").AddComponent<MapaVectorial>();
            Geo.LatitudOrigen = mapa.latitud; Geo.LongitudOrigen = mapa.longitud;

            var raiz = new GameObject("Marcadores").transform;
            PrepararEscena.CrearPosicion(raiz);
            foreach (var s in PrepararEscena.Sitios) PrepararEscena.CrearSitio(raiz, s.nombre, s.tipo, s.lat, s.lon);

            EditorSceneManager.SaveScene(escena, RutaEscena);
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!escenas.Exists(e => e.path == RutaEscena)) escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("PrecioLocal: escena vectorial creada en " + RutaEscena + ". Pulsa Play para cargar el mapa.");
        }

        /// <summary>Si la escena aún no existe, la crea sola al abrir o recompilar el proyecto.</summary>
        [InitializeOnLoadMethod]
        static void CrearSiFalta()
        {
            if (Application.isBatchMode || File.Exists(RutaEscena)) return;
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(RutaEscena) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Crear();
            };
        }
    }
}
