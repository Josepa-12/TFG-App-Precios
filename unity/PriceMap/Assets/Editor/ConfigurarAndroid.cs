using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Ajustes de la app para Android y compilación de la APK (sin Google Play).
    /// Menú: PriceMap ▸ Configurar Android / Compilar APK. Por lotes:
    /// Unity -batchmode -quit -buildTarget Android -projectPath unity/PriceMap -executeMethod PriceMap.EditorTools.ConfigurarAndroid.CompilarApk
    /// </summary>
    public static class ConfigurarAndroid
    {
        public const string NombreApp = "PriceMap";
        public const string Identificador = "com.pricemap.app";
        public const string RutaApk = "Builds/PriceMap.apk";

        [MenuItem("PriceMap/Configurar Android")]
        public static void Configurar()
        {
            PlayerSettings.productName = NombreApp;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Identificador);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35; // API 35 (Android 15): el enum no tiene nombre para ella
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Activity y no GameActivity (historia 2.8, parte 2): con GameActivity la pausa la hace la parte nativa y no
            // se puede evitar; con Activity, ActividadPrincipal (ActividadAndroid) no pausa Unity navegando
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;

            // Solo vertical (UX-DR22)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            EditorUserBuildSettings.buildAppBundle = false; // APK instalable a mano
            AssetDatabase.SaveAssets();
            Debug.Log($"PriceMap: Android configurado ({Identificador}, minSdk 26, targetSdk 35, IL2CPP ARM64, vertical, Activity).");
        }

        [MenuItem("PriceMap/Compilar APK")]
        public static void CompilarApk()
        {
            // La APK se compila desde la escena guardada en disco: guardar antes lo que haya sin guardar
            if (!Application.isBatchMode && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Configurar();
            if (!File.Exists(PrepararEscenaPrincipal.RutaEscena)) PrepararEscenaPrincipal.Crear();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaApk));
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            // Las tablas de Localization viajan como contenido de Addressables: se construye antes de la APK
            AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetSettings.BuildPlayerContent(out var contenido);
            if (!string.IsNullOrEmpty(contenido.Error))
            {
                Debug.LogError("PriceMap: falló el contenido de Addressables: " + contenido.Error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var informe = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrepararEscenaPrincipal.RutaEscena },
                locationPathName = RutaApk,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            Terminar(informe, RutaApk);
        }

        /// <summary>
        /// Ejecutable de Windows solo para comprobar la interfaz sin móvil: lanzado con
        /// "-captura &lt;carpeta&gt;" guarda una captura en cada tema y sale (CapturaPruebas).
        /// </summary>
        public static void CompilarPcPruebas()
        {
            const string ruta = "Builds/PC/PriceMap.exe";
            if (!File.Exists(PrepararEscenaPrincipal.RutaEscena)) PrepararEscenaPrincipal.Crear();
            // El contenido de Addressables se construye para la plataforma activa: tiene que ser Windows
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetSettings.BuildPlayerContent(out var contenido);
            if (!string.IsNullOrEmpty(contenido.Error))
            {
                Debug.LogError("PriceMap: falló el contenido de Addressables: " + contenido.Error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 412;  // ancho de referencia de EXPERIENCE.md (dp)
            PlayerSettings.defaultScreenHeight = 915;
            PlayerSettings.resizableWindow = false;
            var informe = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrepararEscenaPrincipal.RutaEscena },
                locationPathName = ruta,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Terminar(informe, ruta);
        }

        static void Terminar(BuildReport informe, string ruta)
        {
            bool ok = informe.summary.result == BuildResult.Succeeded;
            Debug.Log(ok
                ? $"PriceMap: compilación creada en {ruta} ({informe.summary.totalSize / (1024 * 1024)} MB)."
                : $"PriceMap: la compilación ha fallado ({informe.summary.result}, {informe.summary.totalErrors} errores).");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
