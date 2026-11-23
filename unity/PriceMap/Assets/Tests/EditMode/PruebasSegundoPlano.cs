using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;
using PriceMap.EditorTools;
using PriceMap.Vectorial;
using UnityEditor;
using UnityEngine;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// La navegación sigue con el móvil bloqueado (historia 2.8, parte 2): AlSegundoPlano del Arranque (fotogramas y
    /// cámara del mapa), el manifiesto (la actividad propia con los filtros de compartir y WAKE_LOCK), la entrada Activity
    /// y la lógica de Java, leyendo el código (en el editor no hay Android).
    /// </summary>
    public class PruebasSegundoPlano
    {
        const string Android = "http://schemas.android.com/apk/res/android";
        GameObject camaraObjeto, arranqueObjeto;
        int fotogramasAntes;

        [SetUp]
        public void Preparar() => fotogramasAntes = Application.targetFrameRate;

        [TearDown]
        public void Limpiar()
        {
            if (camaraObjeto != null) UnityEngine.Object.DestroyImmediate(camaraObjeto);
            if (arranqueObjeto != null) UnityEngine.Object.DestroyImmediate(arranqueObjeto);
            Application.targetFrameRate = fotogramasAntes;
        }

        // ───────────────────────── Arranque ─────────────────────────

        [Test]
        public void SegundoPlano_CincoFotogramasYSinDibujarElMapa_YAlVolverLoDeSiempre()
        {
            camaraObjeto = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = camaraObjeto.GetComponent<Camera>();
            Arranque.Arranque.AplicarSegundoPlano(true, camara);
            Assert.AreEqual(5, Application.targetFrameRate);
            Assert.IsFalse(camara.enabled, "El mapa no se dibuja.");
            Arranque.Arranque.AplicarSegundoPlano(false, camara);
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.IsTrue(camara.enabled);
            Assert.DoesNotThrow(() => Arranque.Arranque.AplicarSegundoPlano(true, null), "Sin cámara, solo el ritmo.");
            Assert.AreEqual(5, Application.targetFrameRate);
        }

        [Test]
        public void VolverDeUnaPausa_RitmoYCamaraDeDelanteSinEsperarAlAviso()
        {
            camaraObjeto = new GameObject("Cámara de prueba", typeof(Camera));
            var camara = camaraObjeto.GetComponent<Camera>();
            Arranque.Arranque.AplicarSegundoPlano(true, camara); // la ruta acabó detrás: el «0» sigue en cola
            Arranque.Arranque.AlVolverDePausa(null, camara, enPausa: true);
            Assert.IsFalse(camara.enabled, "Al pausar, nada.");
            Arranque.Arranque.AlVolverDePausa(null, camara, enPausa: false);
            Assert.IsTrue(camara.enabled);
            Assert.AreEqual(Arranque.Arranque.FotogramasDelante, Application.targetFrameRate);
        }

        [Test]
        public void AlSegundoPlano_ElMensajeDeAndroidApagaYEnciendeLaCamaraDelMapa()
        {
            camaraObjeto = new GameObject("Cámara del mapa", typeof(Camera), typeof(CamaraMapa));
            arranqueObjeto = new GameObject("Arranque de prueba");
            var arranque = arranqueObjeto.AddComponent<Arranque.Arranque>();
            var camara = camaraObjeto.GetComponent<Camera>();
            arranque.AlSegundoPlano("1");
            Assert.IsFalse(camara.enabled);
            Assert.AreEqual(Arranque.Arranque.FotogramasSegundoPlano, Application.targetFrameRate);
            arranque.AlSegundoPlano("0");
            Assert.IsTrue(camara.enabled);
            Assert.AreEqual(Arranque.Arranque.FotogramasDelante, Application.targetFrameRate);
        }

        // ───────────────────────── Manifiesto y ajustes ─────────────────────────

        static string ManifiestoGameActivity() =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<manifest xmlns:android=\"" + Android + "\">\n" +
            "  <application>\n" +
            "    <activity android:name=\"com.otra.Actividad\" />\n" +
            "    <activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\" android:launchMode=\"singleTask\">\n" +
            "      <intent-filter>\n" +
            "        <category android:name=\"android.intent.category.LAUNCHER\" />\n" +
            "        <action android:name=\"android.intent.action.MAIN\" />\n" +
            "      </intent-filter>\n" +
            "      <meta-data android:name=\"android.app.lib_name\" android:value=\"game\" />\n" +
            "      <meta-data android:name=\"unityplayer.UnityActivity\" android:value=\"true\" />\n" +
            "    </activity>\n" +
            "  </application>\n" +
            "</manifest>\n";

        [Test]
        public void Manifiesto_LanzaActividadPrincipalConCompartirYWakeLock()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "PriceMapActividad_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(carpeta, "src", "main"));
            string manifiesto = Path.Combine(carpeta, "src", "main", "AndroidManifest.xml");
            File.WriteAllText(manifiesto, ManifiestoGameActivity());
            try
            {
                // En el orden de Unity (callbackOrder) y dos veces: sin duplicar nada
                for (int i = 0; i < 2; i++)
                {
                    new ActividadAndroid().OnPostGenerateGradleAndroidProject(carpeta);
                    new PermisosUbicacionAndroid().OnPostGenerateGradleAndroidProject(carpeta);
                    new CompartirAndroid().OnPostGenerateGradleAndroidProject(carpeta);
                }
                var xml = new XmlDocument();
                xml.Load(manifiesto);
                var nombres = new XmlNamespaceManager(xml.NameTable);
                nombres.AddNamespace("android", Android);

                Assert.AreEqual("false", ((XmlElement)xml.SelectSingleNode("/manifest/application")).GetAttribute("allowBackup", Android),
                    "Sin copia de seguridad: al reinstalar, la bienvenida vuelve a salir (historia 3.1).");
                var lanzadora = (XmlElement)xml.SelectSingleNode(
                    "/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN']", nombres);
                Assert.AreEqual(ActividadAndroid.Actividad, lanzadora.GetAttribute("name", Android));
                Assert.AreEqual("com.pricemap.app.ActividadPrincipal", ActividadAndroid.Actividad);
                Assert.AreEqual("singleTask", lanzadora.GetAttribute("launchMode", Android), "Lo demás de la actividad se queda.");
                Assert.IsNull(lanzadora.SelectSingleNode("meta-data[@android:name='android.app.lib_name']", nombres), "Solo de GameActivity.");
                Assert.IsNotNull(lanzadora.SelectSingleNode("meta-data[@android:name='unityplayer.UnityActivity']", nombres));
                Assert.AreEqual(1, lanzadora.SelectNodes($"intent-filter[action/@android:name='{CompartirAndroid.AccionEnviar}' and data/@android:mimeType='text/plain']", nombres).Count,
                    "Compartir texto con la app (2.5c) sigue en la actividad nueva.");
                Assert.AreEqual(1, lanzadora.SelectNodes($"intent-filter[action/@android:name='{CompartirAndroid.AccionVer}' and data/@android:scheme='geo']", nombres).Count);
                Assert.AreEqual("com.otra.Actividad",
                    ((XmlElement)xml.SelectNodes("/manifest/application/activity", nombres)[0]).GetAttribute("name", Android), "Las otras no cambian.");
                Assert.AreEqual(1, xml.SelectNodes("/manifest/uses-permission[@android:name='android.permission.WAKE_LOCK']", nombres).Count);
            }
            finally { Directory.Delete(carpeta, true); }
        }

        [Test]
        public void Ajustes_EntradaActivityYNoGameActivity()
        {
            Assert.AreEqual(AndroidApplicationEntry.Activity, PlayerSettings.Android.applicationEntry);
            StringAssert.Contains("PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;",
                File.ReadAllText("Assets/Editor/ConfigurarAndroid.cs"), "Configurar (y CompilarApk) la deja así.");
        }

        // ───────────────────────── Java y JNI, leyendo el código ─────────────────────────

        /// <summary>El cuerpo del método Java con esa firma (hasta la llave que lo cierra).</summary>
        static string Cuerpo(string java, string firma)
        {
            int inicio = java.IndexOf(firma, StringComparison.Ordinal);
            Assert.GreaterOrEqual(inicio, 0, "Falta " + firma);
            int llave = java.IndexOf('{', inicio), nivel = 0;
            for (int i = llave; i < java.Length; i++)
            {
                if (java[i] == '{') nivel++;
                else if (java[i] == '}' && --nivel == 0) return java.Substring(llave, i - llave + 1);
            }
            Assert.Fail("Sin cerrar: " + firma);
            return null;
        }

        [Test]
        public void Actividad_NavegandoNoPausaUnityYAvisaAlArranque()
        {
            string java = File.ReadAllText("Assets/Plugins/Android/ActividadPrincipal.java");
            StringAssert.Contains("package com.pricemap.app;", java);
            StringAssert.Contains("public class ActividadPrincipal extends Activity implements IUnityPlayerLifecycleEvents, IUnityPermissionRequestSupport, IUnityPlayerSupport", java);
            StringAssert.Contains("protected UnityPlayerForActivityOrService mUnityPlayer;", java, "El nombre que usa el código nativo.");
            StringAssert.Contains("static volatile boolean seguirEnSegundoPlano;", java);
            StringAssert.Contains("public static void seguir(boolean activo, String objeto)", java);
            StringAssert.Contains("UnityPlayer.UnitySendMessage(objeto, \"AlSegundoPlano\", segundoPlano ? \"1\" : \"0\")", java);
            StringAssert.Contains("seguirEnSegundoPlano && PuenteUbicacionRuta.enMarcha()", java, "Sin el servicio de ruta vivo, se pausa.");

            foreach (var (firma, llamada) in new[] { ("void onPause()", "mUnityPlayer.onPause();"), ("void onStop()", "mUnityPlayer.onStop();") })
            {
                string cuerpo = Cuerpo(java, firma);
                int salto = cuerpo.IndexOf("if (siguiendo())", StringComparison.Ordinal);
                Assert.GreaterOrEqual(salto, 0, firma + ": navegando se salta la pausa.");
                Assert.Greater(cuerpo.IndexOf(llamada, StringComparison.Ordinal), salto, firma + ": sin ruta, la pausa de siempre.");
            }
            // «Segundo plano» solo al dejar de verse (onStop): onPause llega también con la app a la vista
            StringAssert.Contains("paradaSaltada = true; avisar(true); return;", Cuerpo(java, "void onStop()"));
            StringAssert.DoesNotContain("avisar(", Cuerpo(java, "void onPause()"));
            StringAssert.Contains("pausaSaltada = false; return;", Cuerpo(java, "void onResume()"), "No reanuda lo que no se pausó.");
            StringAssert.DoesNotContain("avisar(", Cuerpo(java, "void onResume()"));
            StringAssert.Contains("paradaSaltada = false; avisar(false); return;", Cuerpo(java, "void onStart()"));
            StringAssert.Contains("mUnityPlayer.onResume();", Cuerpo(java, "void onResume()"));
            StringAssert.Contains("mUnityPlayer.onStart();", Cuerpo(java, "void onStart()"));
            // Sin actividad, el servicio de ruta se para con ella
            string destruir = Cuerpo(java, "void onDestroy ()");
            StringAssert.Contains("PuenteUbicacionRuta.parar();", destruir);
            Assert.Less(destruir.IndexOf("PuenteUbicacionRuta.parar();", StringComparison.Ordinal),
                destruir.IndexOf("mUnityPlayer.destroy();", StringComparison.Ordinal));
            // Al terminar la ruta con la app detrás, la pausa saltada se hace entonces
            string seguir = Cuerpo(java, "public static void seguir(");
            StringAssert.Contains("pausarLoSaltado", seguir);
            string pausar = Cuerpo(java, "private void pausarLoSaltado()");
            Assert.Less(pausar.IndexOf("if (siguiendo()) return;", StringComparison.Ordinal), pausar.IndexOf("mUnityPlayer.onPause();", StringComparison.Ordinal),
                "Si vuelve a haber ruta antes de que corra, no pausa.");
            Assert.GreaterOrEqual(pausar.IndexOf("if (siguiendo()) return;", StringComparison.Ordinal), 0);
            StringAssert.Contains("mUnityPlayer.onPause();", pausar);
            StringAssert.Contains("mUnityPlayer.onStop();", pausar);
            StringAssert.Contains("setIntent(intent);", Cuerpo(java, "void onNewIntent(Intent intent)"), "Compartir (2.5c) lee el último intent.");
            StringAssert.Contains("mUnityPlayer.permissionResponse(", java, "Los permisos siguen llegando a Unity.");
        }

        [Test]
        public void Servicio_MantieneElProcesadorDespiertoYLoSueltaAlParar()
        {
            string java = File.ReadAllText("Assets/Plugins/Android/ServicioUbicacionRuta.java");
            StringAssert.Contains("PowerManager.PARTIAL_WAKE_LOCK", java);
            StringAssert.Contains("despierto.acquire();", java);
            StringAssert.Contains("if (escuchando) despertar();", Cuerpo(java, "public int onStartCommand("), "Solo con algún proveedor.");
            string destruir = Cuerpo(java, "public void onDestroy()");
            StringAssert.Contains("soltar();", destruir);
            StringAssert.Contains("com.pricemap.app.ActividadPrincipal.seguir(false, null);", destruir, "Parado por el sistema: Unity vuelve a pausarse.");
            StringAssert.Contains("despierto.release();", Cuerpo(java, "private void soltar()"));
        }

        [Test]
        public void Jni_AlIniciarYPararLaRutaAvisaALaActividad()
        {
            string cs = File.ReadAllText("Assets/Scripts/Adaptadores/Gps/ServicioRutaAndroid.cs");
            StringAssert.Contains("\"com.pricemap.app.ActividadPrincipal\"", cs);
            StringAssert.Contains("CallStatic(\"seguir\", activo, objetoUnity)", cs);
            Assert.IsTrue(Regex.IsMatch(cs, @"public void Iniciar\(string textoNotificacion\)\s*\{[^}]*Seguir\(true\);"), "Al iniciar, sigue.");
            Assert.IsTrue(Regex.IsMatch(cs, @"public void Parar\(\)\s*\{[^}]*Seguir\(false\);"), "Al parar, todo como siempre.");
        }
    }
}
