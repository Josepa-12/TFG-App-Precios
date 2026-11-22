using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Red;
using UnityEngine.TestTools;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Arreglo «red Android TLS 1.3»: el cliente HTTP común (en el editor, UnityWebRequest contra un servidor local),
    /// el puente Java leído (hilo aparte, redirecciones, cancelar, certificados) y que los adaptadores lo usen.
    /// </summary>
    public class PruebasRed
    {
        const string Java = "Assets/Plugins/Android/PuenteRed.java";

        // ---------- Cliente en el editor ----------

        [Test]
        public void EnElEditor_NoVaPorLaRedDeAndroid() => Assert.IsFalse(ClienteHttp.RedAndroid);

        [Test]
        public void Peticiones_GetYPostConSusCabeceras()
        {
            var get = PeticionHttp.Get("https://x/", "PriceMap/1.0");
            Assert.AreEqual("GET", get.Metodo);
            Assert.IsNull(get.CuerpoJson);
            Assert.IsTrue(get.SeguirRedirecciones);
            Assert.AreEqual("PriceMap/1.0", get.Cabeceras["User-Agent"]);

            var post = PeticionHttp.PostJson("https://x/", "{}", "PriceMap/1.0");
            Assert.AreEqual("POST", post.Metodo);
            Assert.AreEqual("{}", post.CuerpoJson);
            Assert.AreEqual("application/json", post.Cabeceras["Content-Type"]);
            Assert.AreEqual("Content-Type: application/json\nUser-Agent: PriceMap/1.0", ClienteHttp.CabecerasEnTexto(post.Cabeceras));
        }

        [Test]
        public void CabecerasEnTexto_SinSaltosDeLineaColados()
        {
            var c = new Dictionary<string, string> { ["A"] = "uno\ndos", [" "] = "vacía" };
            Assert.AreEqual("A: uno dos", ClienteHttp.CabecerasEnTexto(c));
        }

        [Test]
        public void Describir_ComoLosMensajesDeAntes()
        {
            Assert.AreEqual("(ConnectionError): Unable to complete SSL connection",
                new RespuestaHttp("ConnectionError", 0, null, null, "Unable to complete SSL connection").Describir());
            Assert.AreEqual("(ProtocolError, HTTP 404): HTTP/1.1 404 Not Found",
                new RespuestaHttp("ProtocolError", 404, null, null, "HTTP/1.1 404 Not Found").Describir());
            Assert.IsTrue(new RespuestaHttp("Success", 200, "x", null, null).Ok);
        }

        [Test]
        public void YaCancelada_NoSaleNingunaPeticion()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<OperationCanceledException>(() => ClienteHttp.Pedir(PeticionHttp.Get("http://127.0.0.1:1/", null), cts.Token));
        }

        /// <summary>Servidor local de un solo uso por petición: responde con lo que diga <paramref name="responder"/>.</summary>
        static (HttpListener, string) Servidor(Action<HttpListenerContext> responder)
        {
            int puerto;
            var sonda = new TcpListener(IPAddress.Loopback, 0);
            sonda.Start();
            puerto = ((IPEndPoint)sonda.LocalEndpoint).Port;
            sonda.Stop();
            var l = new HttpListener();
            string url = $"http://127.0.0.1:{puerto}/";
            l.Prefixes.Add(url);
            l.Start();
            Task.Run(async () =>
            {
                while (l.IsListening)
                {
                    HttpListenerContext ctx;
                    try { ctx = await l.GetContextAsync(); } catch { return; }
                    try { responder(ctx); } catch { /* la petición pudo cortarse */ }
                }
            });
            return (l, url);
        }

        static void Escribir(HttpListenerResponse r, int codigo, string texto)
        {
            r.StatusCode = codigo;
            var bytes = Encoding.UTF8.GetBytes(texto);
            r.ContentLength64 = bytes.Length;
            r.OutputStream.Write(bytes, 0, bytes.Length);
            r.Close();
        }

        static IEnumerator Esperar(Task t, float segundos = 10)
        {
            var hasta = DateTime.UtcNow.AddSeconds(segundos);
            while (!t.IsCompleted && DateTime.UtcNow < hasta) yield return null;
            Assert.IsTrue(t.IsCompleted, "La petición no terminó a tiempo.");
        }

        [UnityTest]
        public IEnumerator Editor_PostConCuerpoYCabeceras()
        {
            string metodo = null, cuerpo = null, agente = null, tipo = null;
            var (l, url) = Servidor(ctx =>
            {
                metodo = ctx.Request.HttpMethod;
                agente = ctx.Request.UserAgent;
                tipo = ctx.Request.ContentType;
                using (var s = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) cuerpo = s.ReadToEnd();
                Escribir(ctx.Response, 200, "{\"ok\":\"ñ\"}");
            });
            try
            {
                var t = ClienteHttp.Pedir(PeticionHttp.PostJson(url + "route", "{\"a\":1}", "PriceMap/1.0"), CancellationToken.None);
                yield return Esperar(t);
                var r = t.Result;
                Assert.IsTrue(r.Ok, r.Describir());
                Assert.AreEqual(200, r.Codigo);
                Assert.AreEqual("{\"ok\":\"ñ\"}", r.Texto);
                Assert.AreEqual("POST", metodo);
                Assert.AreEqual("{\"a\":1}", cuerpo);
                Assert.AreEqual("PriceMap/1.0", agente);
                StringAssert.StartsWith("application/json", tipo);
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Editor_SinSeguirRedirecciones_DevuelveLocation()
        {
            int pedidas = 0;
            var (l, url) = Servidor(ctx =>
            {
                Interlocked.Increment(ref pedidas);
                ctx.Response.Headers["Location"] = "https://www.google.com/maps/@36.77,-6.35,17z";
                Escribir(ctx.Response, 302, "");
            });
            try
            {
                var p = PeticionHttp.Get(url + "abc", "PriceMap/1.0");
                p.SeguirRedirecciones = false;
                var t = ClienteHttp.Pedir(p, CancellationToken.None);
                yield return Esperar(t);
                Assert.AreEqual(302, t.Result.Codigo);
                Assert.AreEqual("https://www.google.com/maps/@36.77,-6.35,17z", t.Result.Location);
                Assert.AreEqual(1, pedidas, "No se sigue la redirección.");
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Editor_ErrorHttp_EsProtocolError()
        {
            var (l, url) = Servidor(ctx => Escribir(ctx.Response, 404, "no"));
            try
            {
                var t = ClienteHttp.Pedir(PeticionHttp.Get(url, "PriceMap/1.0"), CancellationToken.None);
                yield return Esperar(t);
                Assert.IsFalse(t.Result.Ok);
                Assert.AreEqual(RespuestaHttp.ErrorProtocolo, t.Result.Resultado);
                Assert.AreEqual(404, t.Result.Codigo);
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Editor_Cancelar_CortaLaPeticion()
        {
            var (l, url) = Servidor(ctx => { /* nunca responde */ });
            using var cts = new CancellationTokenSource();
            try
            {
                var t = ClienteHttp.Pedir(PeticionHttp.Get(url, "PriceMap/1.0"), cts.Token);
                yield return null;
                cts.Cancel();
                yield return Esperar(t);
                Assert.IsTrue(t.IsCanceled || t.Exception?.InnerException is OperationCanceledException, "Termina cancelada.");
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Editor_TiempoMaximo_CortaUnServidorMudo()
        {
            var (l, url) = Servidor(ctx => { /* nunca responde */ });
            try
            {
                var p = PeticionHttp.Get(url, "PriceMap/1.0");
                p.SegundosMaximo = 1;
                var t = ClienteHttp.Pedir(p, CancellationToken.None);
                yield return Esperar(t, 5);
                Assert.IsFalse(t.Result.Ok);
                Assert.AreEqual(RespuestaHttp.ErrorConexion, t.Result.Resultado, t.Result.Describir());
            }
            finally { l.Close(); }
        }

        // ---------- Adaptadores contra el servidor local ----------

        [UnityTest]
        public IEnumerator Enlace_RedireccionDevuelveElDestinoAbsoluto()
        {
            var (l, url) = Servidor(ctx =>
            {
                ctx.Response.Headers["Location"] = "/maps/@36.77,-6.35,17z";
                Escribir(ctx.Response, 302, "");
            });
            try
            {
                var t = new PriceMap.Adaptadores.Busqueda.ResolutorEnlacesWeb().Resolver(url + "abc", CancellationToken.None);
                yield return Esperar(t);
                Assert.IsTrue(t.Result.Ok, t.Result.Error);
                Assert.AreEqual(url + "maps/@36.77,-6.35,17z", t.Result.Valor);
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Enlace_SinRedireccionSeQuedaLaUrl()
        {
            var (l, url) = Servidor(ctx => Escribir(ctx.Response, 200, "<html></html>"));
            try
            {
                var t = new PriceMap.Adaptadores.Busqueda.ResolutorEnlacesWeb().Resolver(url + "final", CancellationToken.None);
                yield return Esperar(t);
                Assert.IsTrue(t.Result.Ok, t.Result.Error);
                Assert.AreEqual(url + "final", t.Result.Valor);
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Enlace_ErrorHttpEsFallo()
        {
            var (l, url) = Servidor(ctx => Escribir(ctx.Response, 404, "no"));
            try
            {
                var t = new PriceMap.Adaptadores.Busqueda.ResolutorEnlacesWeb().Resolver(url + "roto", CancellationToken.None);
                yield return Esperar(t);
                Assert.IsFalse(t.Result.Ok);
                StringAssert.StartsWith("El enlace no responde (ProtocolError, HTTP 404)", t.Result.Error);
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Rutas_PostConJsonYAgente()
        {
            string metodo = null, agente = null, tipo = null, cuerpo = null;
            var (l, url) = Servidor(ctx =>
            {
                metodo = ctx.Request.HttpMethod;
                agente = ctx.Request.UserAgent;
                tipo = ctx.Request.ContentType;
                using (var s = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) cuerpo = s.ReadToEnd();
                Escribir(ctx.Response, 200, "{\"trip\":{}}");
            });
            try
            {
                var t = PriceMap.Adaptadores.Rutas.PeticionRutas.Pedir("Valhalla", url + "route", "{\"costing\":\"auto\"}", CancellationToken.None);
                yield return Esperar(t);
                Assert.IsTrue(t.Result.Ok, t.Result.Error);
                Assert.AreEqual("{\"trip\":{}}", t.Result.Valor);
                Assert.AreEqual("POST", metodo);
                Assert.AreEqual("{\"costing\":\"auto\"}", cuerpo);
                Assert.AreEqual("PriceMap/1.0", agente);
                StringAssert.StartsWith("application/json", tipo);
            }
            finally { l.Close(); }
        }

        [UnityTest]
        public IEnumerator Rutas_ErrorDelServidorEsFallo()
        {
            var (l, url) = Servidor(ctx => Escribir(ctx.Response, 500, "roto"));
            try
            {
                var t = PriceMap.Adaptadores.Rutas.PeticionRutas.Pedir("Valhalla", url + "route", "{}", CancellationToken.None);
                yield return Esperar(t);
                Assert.IsFalse(t.Result.Ok);
                StringAssert.StartsWith("Valhalla no responde (ProtocolError, HTTP 500)", t.Result.Error);
            }
            finally { l.Close(); }
        }

        // ---------- Puente Java ----------

        [Test]
        public void Java_PeticionEnUnHiloAparte()
        {
            string java = File.ReadAllText(Java);
            StringAssert.Contains("package com.pricemap.red;", java);
            StringAssert.Contains("HttpURLConnection", java);
            StringAssert.Contains("Executors.newCachedThreadPool", java);
            StringAssert.Contains("hilos.execute(", java);
            StringAssert.DoesNotContain("runOnUiThread", java);
            StringAssert.DoesNotContain("Looper.getMainLooper", java);
            foreach (var metodo in new[] { "int pedir(", "int estado(", "int codigo(", "String texto(", "String location(", "String error(", "void cancelar(", "void liberar(" })
                StringAssert.Contains("public static " + metodo, java);
        }

        [Test]
        public void Java_RedireccionesSegunLaPeticionYTiempoMaximoTotal()
        {
            string java = File.ReadAllText(Java);
            StringAssert.Contains("setInstanceFollowRedirects(seguirRedirecciones)", java);
            StringAssert.Contains("reloj.schedule(", java);
            StringAssert.Contains("\"Location\"", java);
            StringAssert.Contains("setRemoveOnCancelPolicy(true)", java);
        }

        [Test]
        public void Java_ElPrimerEstadoFinalGanaYNuncaSeQuedaEnCurso()
        {
            string java = File.ReadAllText(Java);
            StringAssert.Contains("if (p.estado != EN_CURSO) return false;", java);
            StringAssert.Contains("terminar(p, CONEXION, 0, \"\", \"\", \"Request timeout\")", java);
            StringAssert.Contains("terminar(p, CANCELADA, 0, \"\", \"\", \"\")", java);
            StringAssert.Contains("catch (Throwable e)", java);
            StringAssert.DoesNotContain("catch (Exception e)", java);
        }

        [Test]
        public void Java_ElRegistroNoLlevaLaUrl()
        {
            string java = File.ReadAllText(Java);
            foreach (var linea in java.Split('\n').Where(x => x.Contains("Log.")))
            {
                StringAssert.DoesNotContain("+ url +", linea);
                StringAssert.Contains("servidor(url)", linea);
            }
        }

        [Test]
        public void Java_CancelarDesconecta()
        {
            string java = File.ReadAllText(Java);
            int cancelar = java.IndexOf("public static void cancelar(", StringComparison.Ordinal);
            Assert.Greater(cancelar, 0);
            string cuerpo = java.Substring(cancelar, java.IndexOf("public static void liberar(", StringComparison.Ordinal) - cancelar);
            StringAssert.Contains("desconectar(p)", cuerpo);
            StringAssert.Contains(".disconnect()", java);
        }

        [Test]
        public void Java_NoTocaLosCertificados()
        {
            string java = File.ReadAllText(Java);
            foreach (var prohibido in new[] { "SSLSocketFactory", "HostnameVerifier", "TrustManager", "SSLContext", "X509" })
                StringAssert.DoesNotContain(prohibido, java);
        }

        [Test]
        public void ClienteAndroid_EsperaFotogramaAFotograma()
        {
            string cs = File.ReadAllText("Assets/Scripts/Adaptadores/Red/ClienteHttp.cs");
            StringAssert.Contains("com.pricemap.red.PuenteRed", cs);
            StringAssert.Contains("await Awaitable.NextFrameAsync(ct)", cs);
            StringAssert.Contains("\"cancelar\"", cs);
            StringAssert.Contains("\"liberar\"", cs);
            StringAssert.DoesNotContain(".Wait(", cs);
            StringAssert.DoesNotContain("Thread.Sleep", cs);
        }

        // ---------- Adaptadores ----------

        [Test]
        public void Adaptadores_SoloElClienteUsaUnityWebRequest()
        {
            var conUnityWebRequest = Directory.GetFiles("Assets/Scripts/Adaptadores", "*.cs", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f).Contains("UnityWebRequest"))
                .Select(f => Path.GetFileName(f))
                .ToList();
            CollectionAssert.AreEqual(new[] { "ClienteHttp.cs" }, conUnityWebRequest);
        }

        [Test]
        public void EnlacesCortos_SinSeguirRedirecciones()
        {
            string cs = File.ReadAllText("Assets/Scripts/Adaptadores/Busqueda/ResolutorEnlacesWeb.cs");
            StringAssert.Contains("SeguirRedirecciones = false", cs);
            StringAssert.Contains("ClienteHttp.Pedir", cs);
        }
    }
}
