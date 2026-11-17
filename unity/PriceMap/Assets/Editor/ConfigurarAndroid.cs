using System.IO;
using UnityEditor;
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

            // Solo vertical (UX-DR22)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            EditorUserBuildSettings.buildAppBundle = false; // APK instalable a mano
            AssetDatabase.SaveAssets();
            Debug.Log($"PriceMap: Android configurado ({Identificador}, minSdk 26, targetSdk 35, IL2CPP ARM64, vertical).");
        }

        [MenuItem("PriceMap/Compilar APK")]
        public static void CompilarApk()
        {
            // La APK se compila desde la escena guardada en disco: guardar antes lo que haya sin guardar
            if (!Application.isBatchMode && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Configurar();
            if (!File.Exists(PrepararEscenaPrincipal.RutaEscena)) PrepararEscenaPrincipal.Crear();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaApk));

            var informe = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PrepararEscenaPrincipal.RutaEscena },
                locationPathName = RutaApk,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            bool ok = informe.summary.result == BuildResult.Succeeded;
            Debug.Log(ok
                ? $"PriceMap: APK creada en {RutaApk} ({informe.summary.totalSize / (1024 * 1024)} MB)."
                : $"PriceMap: la compilación de Android ha fallado ({informe.summary.result}, {informe.summary.totalErrors} errores).");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
