using System;
using System.Collections;
using System.IO;
using PriceMap.Aplicacion;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.Arranque
{
    /// <summary>
    /// Solo para comprobar la interfaz sin móvil: al lanzar el ejecutable con
    /// "-captura &lt;carpeta&gt;", espera a que cargue el mapa, guarda una captura en cada tema y sale.
    /// Sin ese argumento no hace nada.
    /// </summary>
    public class CapturaPruebas : MonoBehaviour
    {
        string carpeta;
        ServicioTema tema;
        IPreferencias preferencias;

        public static void SiSePide(GameObject anfitrion, ServicioTema tema, IPreferencias preferencias)
        {
#if !(UNITY_STANDALONE || UNITY_EDITOR || DEVELOPMENT_BUILD)
            return; // nunca en la APK de verdad
#else
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-captura");
            if (i < 0 || i + 1 >= args.Length) return;
            var c = anfitrion.AddComponent<CapturaPruebas>();
            c.carpeta = args[i + 1];
            c.tema = tema;
            c.preferencias = preferencias;
#endif
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(carpeta);
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            yield return new WaitForSeconds(8f); // teselas descargadas y construidas
            Diagnostico();
            yield return Capturar(tema.Actual.ToString().ToLowerInvariant());
            tema.Alternar();
            yield return new WaitForSeconds(1.5f);
            yield return Capturar(tema.Actual.ToString().ToLowerInvariant());
            tema.Alternar();
            // Deja la preferencia como estaba: si no había ninguna, la app debe seguir al sistema
            if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
            else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            yield return new WaitForSeconds(1f); // CaptureScreenshot escribe el fichero al acabar el fotograma
            Debug.Log("PriceMap: capturas guardadas en " + carpeta);
            Application.Quit();
        }

        /// <summary>Escribe en el registro dónde está cada elemento de la interfaz, con su texto y su fuente.</summary>
        static void Diagnostico()
        {
            foreach (var doc in FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None))
            {
                var raiz = doc.rootVisualElement;
                if (raiz == null) continue;
                Debug.Log($"PriceMap: panel {raiz.layout} pantalla {Screen.width}x{Screen.height} dpi {Screen.dpi}");
                raiz.Query<UnityEngine.UIElements.VisualElement>().ForEach(e =>
                {
                    var t = e as UnityEngine.UIElements.TextElement;
                    Debug.Log($"PriceMap:   {e.name} [{string.Join(" ", e.GetClasses())}] {e.worldBound} display={e.resolvedStyle.display}" +
                              (t != null ? $" texto=\"{t.text}\" tamaño={t.resolvedStyle.fontSize} fuente={t.resolvedStyle.unityFont?.name ?? "-"} def={t.resolvedStyle.unityFontDefinition.fontAsset?.name ?? "-"} color={t.resolvedStyle.color}" : ""));
                });
            }
        }

        IEnumerator Capturar(string nombre)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, nombre + ".png"));
            yield return null;
        }
    }
}
