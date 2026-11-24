using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using PriceMap.Adaptadores.Firebase;
using PriceMap.Aplicacion;
using PriceMap.EditorTools;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TestTools;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Firebase dentro de la app (historia 3.2, parte 2): el inicio (bien, falla, tarda, sin Firebase), el almacén con un
    /// Firestore falso, los valores de los campos, el manifiesto de Android y que nada secreto esté en git.
    /// </summary>
    public class PruebasFirebase
    {
        SynchronizationContext contextoAntes;

        [SetUp]
        public void Preparar()
        {
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // las continuaciones, al momento
        }

        [TearDown]
        public void Limpiar() => SynchronizationContext.SetSynchronizationContext(contextoAntes);

        // ───────────────────────── Inicio ─────────────────────────

        [Test]
        public void Inicio_Bien_QuedaListoYLoDiceElRegistro()
        {
            var inicio = new InicioFirebase(() => Task.FromResult<string>(null));
            var vistos = new List<EstadoServidor>();
            inicio.Cambiado += () => vistos.Add(inicio.Estado);
            Assert.AreEqual(EstadoServidor.Iniciando, inicio.Estado);
            LogAssert.Expect(LogType.Log, "PriceMap: Firebase listo");
            inicio.Iniciar();
            CollectionAssert.AreEqual(new[] { EstadoServidor.Listo }, vistos);
        }

        [Test]
        public void Inicio_ConMotivo_NoDisponibleConAviso()
        {
            var inicio = new InicioFirebase(() => Task.FromResult("servicios de Google Play: UnavailableDisabled"));
            int avisos = 0;
            inicio.Cambiado += () => avisos++;
            LogAssert.Expect(LogType.Warning, new Regex("^PriceMap: Firebase no disponible.*UnavailableDisabled"));
            inicio.Iniciar();
            Assert.AreEqual(EstadoServidor.NoDisponible, inicio.Estado);
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Inicio_FallaAlMomento_NoDisponibleSinExcepcion()
        {
            var inicio = new InicioFirebase(() => throw new DllNotFoundException("FirebaseCppApp"));
            LogAssert.Expect(LogType.Warning, new Regex("DllNotFoundException"));
            Assert.DoesNotThrow(() => inicio.Iniciar());
            Assert.AreEqual(EstadoServidor.NoDisponible, inicio.Estado);
        }

        [Test]
        public void Inicio_FallaDespues_NoDisponible()
        {
            var tarea = new TaskCompletionSource<string>();
            var inicio = new InicioFirebase(() => tarea.Task);
            inicio.Iniciar();
            LogAssert.Expect(LogType.Warning, new Regex("InvalidOperationException: sin json"));
            tarea.SetException(new InvalidOperationException("sin json"));
            Assert.AreEqual(EstadoServidor.NoDisponible, inicio.Estado);
        }

        [Test]
        public void Inicio_Tarda_NoEsperaYAvisaCuandoAcaba()
        {
            var tarea = new TaskCompletionSource<string>();
            int veces = 0, avisos = 0;
            var inicio = new InicioFirebase(() => { veces++; return tarea.Task; });
            inicio.Cambiado += () => avisos++;
            inicio.Iniciar();
            inicio.Iniciar();
            Assert.AreEqual(EstadoServidor.Iniciando, inicio.Estado, "Vuelve sin esperar.");
            Assert.AreEqual(0, avisos);
            tarea.SetResult(null);
            Assert.AreEqual(EstadoServidor.Listo, inicio.Estado);
            Assert.AreEqual(1, veces, "Solo cuenta la primera vez.");
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Inicio_AcabaEnOtroHilo_VuelveAlQueLoInicio()
        {
            var hilo = new HiloFalso();
            SynchronizationContext.SetSynchronizationContext(hilo);
            var tarea = new TaskCompletionSource<string>();
            var inicio = new InicioFirebase(() => tarea.Task);
            inicio.Iniciar();
            SynchronizationContext.SetSynchronizationContext(null);
            tarea.SetResult(null);
            Assert.AreEqual(EstadoServidor.Iniciando, inicio.Estado, "Aún no ha vuelto al hilo principal.");
            hilo.Vaciar();
            Assert.AreEqual(EstadoServidor.Listo, inicio.Estado);
        }

        [Test]
        public void Arranque_EnElEditor_NoDisponibleSinTocarFirebase()
        {
            LogAssert.Expect(LogType.Warning, new Regex("^PriceMap: sin Firebase en esta plataforma"));
            var servidor = Arranque.Arranque.CrearServidor();
            Assert.IsInstanceOf<ServidorNulo>(servidor);
            Assert.AreEqual(EstadoServidor.NoDisponible, servidor.Estado);
        }

        [Test]
        public void Arranque_ConFirebase_LoDejaEmpezado()
        {
            int veces = 0;
            var tarea = new TaskCompletionSource<string>();
            var servidor = Arranque.Arranque.CrearServidor(true, () => new InicioFirebase(() => { veces++; return tarea.Task; }));
            Assert.IsInstanceOf<InicioFirebase>(servidor);
            Assert.AreEqual(0, veces, "Aún sin empezar: se empieza con la app compuesta.");
            Arranque.Arranque.IniciarServidor(servidor);
            Assert.AreEqual(1, veces, "Empezado.");
            Assert.DoesNotThrow(() => Arranque.Arranque.IniciarServidor(new ServidorNulo()));
            Assert.AreEqual(EstadoServidor.Iniciando, servidor.Estado);
        }

        [Test]
        public void Arranque_LoEmpiezaAlFinalYSinEsperarlo()
        {
            string codigo = File.ReadAllText("Assets/Scripts/Arranque/Arranque.cs");
            int compone = codigo.IndexOf("if (!Componer(", StringComparison.Ordinal);
            int crea = codigo.IndexOf("servidor = CrearServidor();", StringComparison.Ordinal);
            int empieza = codigo.IndexOf("            IniciarServidor(servidor);", StringComparison.Ordinal);
            Assert.Greater(crea, 0);
            Assert.Greater(compone, crea, "La cuenta (3.3) lo necesita al componer.");
            Assert.Greater(empieza, compone, "Con la app ya compuesta.");
            StringAssert.Contains("void Start()", codigo);
            StringAssert.DoesNotContain("async void Start()", codigo);
            StringAssert.DoesNotContain("await ", codigo);
        }

        [Test]
        public void Preparacion_CopiaEnElMovilAntesDeUsarFirestore()
        {
            string codigo = File.ReadAllText("Assets/Scripts/Adaptadores/Firebase/PreparacionFirebase.cs");
            StringAssert.Contains("CheckAndFixDependenciesAsync", codigo);
            StringAssert.Contains("ajustes.PersistenceEnabled = true", codigo);
            StringAssert.Contains("ajustes.CacheSizeBytes = BytesCopia", codigo);
            StringAssert.Contains("100L * 1024 * 1024", codigo);
            var conInstancia = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f).Contains("FirebaseFirestore.DefaultInstance")).Select(Path.GetFileName);
            CollectionAssert.AreEquivalent(new[] { "PreparacionFirebase.cs", "AccesoFirestore.cs" }, conInstancia);
        }

        // ───────────────────────── Almacén ─────────────────────────

        class ServidorFalso : IServidor
        {
            public EstadoServidor Estado { get; set; } = EstadoServidor.Listo;
            public event Action Cambiado { add { } remove { } }
        }

        class FirestoreFalso : IAccesoFirestore
        {
            public Task<Documento> lectura = Task.FromResult(new Documento(false, null, false));
            public Task escritura = Task.CompletedTask;
            public Exception falloAlEscuchar;
            public Action<Documento> alCambiar;
            public Action<Exception> alFallar;
            public int llamadas, sueltas;
            public IReadOnlyDictionary<string, object> escrito;

            public Task<Documento> Leer(string ruta) { llamadas++; return lectura; }
            public Task Escribir(string ruta, IReadOnlyDictionary<string, object> campos) { llamadas++; escrito = campos; return escritura; }

            public IDisposable Escuchar(string ruta, Action<Documento> alCambiar, Action<Exception> alFallar)
            {
                llamadas++;
                if (falloAlEscuchar != null) throw falloAlEscuchar;
                this.alCambiar = alCambiar;
                this.alFallar = alFallar;
                return new Soltar(() => sueltas++);
            }
        }

        class Soltar : IDisposable
        {
            readonly Action accion;
            public Soltar(Action accion) => this.accion = accion;
            public void Dispose() => accion();
        }

        class HiloFalso : SynchronizationContext
        {
            readonly Queue<Action> pendientes = new Queue<Action>();
            public override void Post(SendOrPostCallback d, object state) => pendientes.Enqueue(() => d(state));
            public void Vaciar() { while (pendientes.Count > 0) pendientes.Dequeue()(); }
        }

        static readonly Dictionary<string, object> Campos = new Dictionary<string, object> { { "tipo", "cliente" } };

        [TestCase(EstadoServidor.Iniciando)]
        [TestCase(EstadoServidor.NoDisponible)]
        public void Almacen_SinServidor_FallaSinTocarFirestore(EstadoServidor estado)
        {
            var firestore = new FirestoreFalso();
            var almacen = new AlmacenFirestore(new ServidorFalso { Estado = estado }, firestore);
            var avisos = new List<Resultado<Documento>>();
            Assert.AreEqual(ErroresServidor.NoDisponible, almacen.Leer("usuarios/a").Result.Error);
            Assert.AreEqual(ErroresServidor.NoDisponible, almacen.Escribir("usuarios/a", Campos).Result.Error);
            using (almacen.Escuchar("usuarios/a", avisos.Add))
                Assert.AreEqual(ErroresServidor.NoDisponible, avisos.Single().Error);
            Assert.AreEqual(0, firestore.llamadas);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("usuarios")]
        [TestCase("usuarios/")]
        [TestCase("usuarios/a/favoritos")]
        [TestCase("usuarios//favoritos/b")]
        public void Almacen_RutaQueNoEsDeDocumento_NoValido(string ruta)
        {
            var firestore = new FirestoreFalso();
            var almacen = new AlmacenFirestore(new ServidorFalso(), firestore);
            Assert.AreEqual(ErroresServidor.NoValido, almacen.Leer(ruta).Result.Error);
            Assert.AreEqual(ErroresServidor.NoValido, almacen.Escribir(ruta, Campos).Result.Error);
            var avisos = new List<Resultado<Documento>>();
            almacen.Escuchar(ruta, avisos.Add);
            Assert.AreEqual(ErroresServidor.NoValido, avisos.Single().Error);
            Assert.AreEqual(0, firestore.llamadas);
        }

        [Test]
        public void Almacen_LeerYEscribir_AcabanEnOtroHilo_VuelvenAlPrincipal()
        {
            var hilo = new HiloFalso();
            SynchronizationContext.SetSynchronizationContext(hilo);
            var lectura = new TaskCompletionSource<Documento>();
            var escritura = new TaskCompletionSource<bool>();
            var almacen = new AlmacenFirestore(new ServidorFalso(), new FirestoreFalso { lectura = lectura.Task, escritura = escritura.Task });
            var leer = almacen.Leer("usuarios/a");
            var escribir = almacen.Escribir("usuarios/a", Campos);
            SynchronizationContext.SetSynchronizationContext(null); // otro hilo
            lectura.SetResult(new Documento(true, Campos, false));
            escritura.SetResult(true);
            Assert.IsFalse(leer.IsCompleted || escribir.IsCompleted, "Aún no han vuelto al hilo principal.");
            hilo.Vaciar();
            Assert.IsTrue(leer.Result.Ok && escribir.Result.Ok);
        }

        // Por reflexión: las pruebas tampoco referencian Firebase
        static Type TipoFirestore(string nombre) =>
            typeof(AlmacenFirestore).Assembly.GetReferencedAssemblies().Where(a => a.Name == "Firebase.Firestore")
                .Select(System.Reflection.Assembly.Load).Single().GetType("Firebase.Firestore." + nombre, throwOnError: true);

        [TestCase("PermissionDenied", ErroresServidor.SinPermiso)]
        [TestCase("Unauthenticated", ErroresServidor.SinPermiso)]
        [TestCase("Unavailable", ErroresServidor.SinConexion)]
        [TestCase("DeadlineExceeded", ErroresServidor.SinConexion)]
        [TestCase("ResourceExhausted", ErroresServidor.CuotaAgotada)]
        [TestCase("NotFound", ErroresServidor.NoEncontrado)]
        [TestCase("InvalidArgument", ErroresServidor.NoValido)]
        public void Acceso_LosErroresDelSdk_SueltosOEnvueltos_LleganConSuCodigo(string nombre, string propio)
        {
            var error = Enum.Parse(TipoFirestore("FirestoreError"), nombre);
            var delSdk = (Exception)Activator.CreateInstance(TipoFirestore("FirestoreException"), error, "x");
            foreach (var e in new[] { delSdk, new AggregateException(new AggregateException(delSdk)) })
            {
                var traducido = AccesoFirestore.Traducido(e);
                Assert.IsInstanceOf<FalloFirestore>(traducido);
                Assert.AreEqual(propio, CodigosFirestore.De(traducido));
            }
            var otro = new InvalidOperationException();
            Assert.AreSame(otro, AccesoFirestore.Traducido(otro));
        }

        [Test]
        public void Almacen_Leer_DevuelveElDocumento()
        {
            var documento = new Documento(true, Campos, delMovil: true);
            var almacen = new AlmacenFirestore(new ServidorFalso(), new FirestoreFalso { lectura = Task.FromResult(documento) });
            var leido = almacen.Leer("usuarios/a/favoritos/b").Result;
            Assert.IsTrue(leido.Ok);
            Assert.AreSame(documento, leido.Valor);
            Assert.IsEmpty(new Documento(false, null, true).Campos, "Sin documento, sin campos.");
        }

        [TestCase(7, ErroresServidor.SinPermiso)]
        [TestCase(16, ErroresServidor.SinPermiso)]
        [TestCase(14, ErroresServidor.SinConexion)]
        [TestCase(4, ErroresServidor.SinConexion)]
        [TestCase(8, ErroresServidor.CuotaAgotada)]
        [TestCase(5, ErroresServidor.NoEncontrado)]
        [TestCase(3, ErroresServidor.NoValido)]
        [TestCase(13, ErroresServidor.Desconocido)]
        public void Almacen_LosErroresDeFirestore_SonCodigosPropiosYElRegistroNoLlevaLaRuta(int codigo, string propio)
        {
            var almacen = new AlmacenFirestore(new ServidorFalso(), new FirestoreFalso
            {
                lectura = Task.FromException<Documento>(new FalloFirestore(codigo)),
                escritura = Task.FromException(new FalloFirestore(codigo)),
            });
            LogAssert.Expect(LogType.Warning, $"PriceMap: no se pudo leer en Firestore ({propio}, FalloFirestore).");
            Assert.AreEqual(propio, almacen.Leer("usuarios/a").Result.Error);
            LogAssert.Expect(LogType.Warning, $"PriceMap: no se pudo escribir en Firestore ({propio}, FalloFirestore).");
            Assert.AreEqual(propio, almacen.Escribir("usuarios/a", Campos).Result.Error);
        }

        [Test]
        public void Almacen_ErrorInesperado_Desconocido()
        {
            var almacen = new AlmacenFirestore(new ServidorFalso(), new FirestoreFalso { lectura = Task.FromException<Documento>(new Exception("x")) });
            Assert.AreEqual(ErroresServidor.Desconocido, almacen.Leer("usuarios/a").Result.Error);
        }

        [Test]
        public void Almacen_EscribirSinConexion_QuedaEnCursoHastaQueLoConfirmaElServidor()
        {
            var confirmacion = new TaskCompletionSource<bool>();
            var firestore = new FirestoreFalso { escritura = confirmacion.Task };
            var almacen = new AlmacenFirestore(new ServidorFalso(), firestore);
            var tarea = almacen.Escribir("usuarios/a", Campos);
            Assert.IsFalse(tarea.IsCompleted);
            Assert.AreSame(Campos, firestore.escrito, "Ya está en Firestore (en la copia del móvil).");
            confirmacion.SetResult(true);
            Assert.IsTrue(tarea.Result.Ok);
        }

        [Test]
        public void Almacen_EscribirSinCampos_NoValido()
        {
            var almacen = new AlmacenFirestore(new ServidorFalso(), new FirestoreFalso());
            Assert.AreEqual(ErroresServidor.NoValido, almacen.Escribir("usuarios/a", new Dictionary<string, object>()).Result.Error);
            Assert.AreEqual(ErroresServidor.NoValido, almacen.Escribir("usuarios/a", null).Result.Error);
        }

        [Test]
        public void Almacen_EscribeConMerge_NuncaElDocumentoEntero()
        {
            string codigo = File.ReadAllText("Assets/Scripts/Adaptadores/Firebase/AccesoFirestore.cs");
            Assert.AreEqual(1, Regex.Matches(codigo, @"SetAsync\(").Count);
            StringAssert.Contains("SetOptions.MergeAll)", codigo);
        }

        [Test]
        public void Almacen_Escuchar_AvisaDeCadaCambioHastaQueSePara()
        {
            var firestore = new FirestoreFalso();
            var avisos = new List<Resultado<Documento>>();
            var escucha = new AlmacenFirestore(new ServidorFalso(), firestore).Escuchar("usuarios/a", avisos.Add);
            firestore.alCambiar(new Documento(false, null, true));
            firestore.alCambiar(new Documento(true, Campos, false));
            Assert.AreEqual(2, avisos.Count);
            Assert.IsTrue(avisos[0].Valor.DelMovil);
            Assert.IsTrue(avisos[1].Valor.Existe);
            escucha.Dispose();
            escucha.Dispose();
            firestore.alCambiar(new Documento(true, Campos, false));
            firestore.alFallar(new FalloFirestore(7));
            Assert.AreEqual(2, avisos.Count, "Parada, ya no avisa.");
            Assert.AreEqual(1, firestore.sueltas);
        }

        [Test]
        public void Almacen_EscucharYFalla_AvisaUnaVezYSeSuelta()
        {
            var firestore = new FirestoreFalso();
            var avisos = new List<Resultado<Documento>>();
            new AlmacenFirestore(new ServidorFalso(), firestore).Escuchar("usuarios/a", avisos.Add);
            firestore.alFallar(new FalloFirestore(7));
            firestore.alCambiar(new Documento(true, Campos, false));
            Assert.AreEqual(ErroresServidor.SinPermiso, avisos.Single().Error);
            Assert.AreEqual(1, firestore.sueltas);

            avisos.Clear();
            firestore.falloAlEscuchar = new FalloFirestore(14);
            new AlmacenFirestore(new ServidorFalso(), firestore).Escuchar("usuarios/a", avisos.Add);
            Assert.AreEqual(ErroresServidor.SinConexion, avisos.Single().Error, "También si falla al empezar.");
        }

        [Test]
        public void Almacen_EscucharDesdeOtroHilo_AvisaEnElPrincipalYNoSiYaSeParo()
        {
            var hilo = new HiloFalso();
            SynchronizationContext.SetSynchronizationContext(hilo);
            var firestore = new FirestoreFalso();
            var avisos = new List<Resultado<Documento>>();
            var escucha = new AlmacenFirestore(new ServidorFalso(), firestore).Escuchar("usuarios/a", avisos.Add);
            SynchronizationContext.SetSynchronizationContext(null); // otro hilo
            firestore.alCambiar(new Documento(true, Campos, false));
            Assert.IsEmpty(avisos);
            hilo.Vaciar();
            Assert.AreEqual(1, avisos.Count);
            firestore.alCambiar(new Documento(true, Campos, false));
            escucha.Dispose();
            hilo.Vaciar();
            Assert.AreEqual(1, avisos.Count, "Se paró antes de que llegara.");
        }

        // ───────────────────────── Valores de los campos ─────────────────────────

        [Test]
        public void Valores_LasFechasVanYVuelvenEnUtc_TambienDentroDeListasYMapas()
        {
            var fecha = new DateTime(2026, 11, 24, 10, 30, 0, DateTimeKind.Utc);
            var escrito = ValoresFirestore.Mapa(new Dictionary<string, object>
            {
                { "utc", fecha }, { "sinTipo", DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified) }, { "local", fecha.ToLocalTime() },
                { "lista", new[] { "a", "b" } }, { "mapa", new Dictionary<string, object> { { "desde", fecha } } },
                { "texto", "x" }, { "numero", 3 }, { "si", true }, { "nada", null },
            });
            Assert.AreEqual("Firebase.Firestore.Timestamp", escrito["utc"].GetType().FullName);
            var leido = ValoresFirestore.Campos(escrito);
            foreach (var clave in new[] { "utc", "sinTipo", "local" })
            {
                Assert.AreEqual(fecha, leido[clave], clave);
                Assert.AreEqual(DateTimeKind.Utc, ((DateTime)leido[clave]).Kind);
            }
            Assert.AreEqual(fecha, ((Dictionary<string, object>)leido["mapa"])["desde"]);
            CollectionAssert.AreEqual(new[] { "a", "b" }, (List<object>)leido["lista"]);
            Assert.AreEqual("x", leido["texto"]);
            Assert.AreEqual(true, leido["si"]);
            Assert.IsNull(leido["nada"]);
            Assert.AreEqual(3, escrito["numero"]);
        }

        [Test]
        public void Valores_LoQueFirestoreNoGuarda_ErrorDeArgumento()
        {
            Assert.Throws<ArgumentException>(() => ValoresFirestore.AFirestore(new ServidorFalso()));
            Assert.AreEqual(ErroresServidor.NoValido, CodigosFirestore.De(new ArgumentException()));
        }

        [Test]
        public void Valores_LaHoraDelServidor_EsLaDeFirestore_TambienEnMapas()
        {
            var tipo = TipoFirestore("FieldValueProxy");
            var hora = ValoresFirestore.AFirestore(HoraServidor.Ahora);
            Assert.IsInstanceOf(tipo, hora);
            Assert.AreEqual("ServerTimestamp", tipo.GetMethod("type").Invoke(hora, null).ToString());
            var mapa = (Dictionary<string, object>)ValoresFirestore.AFirestore(new Dictionary<string, object> { { "creado", HoraServidor.Ahora } });
            Assert.IsInstanceOf(tipo, mapa["creado"]);
        }

        [Test]
        public void Valores_LosMapasConTipo_SonMapas()
        {
            var mapa = (Dictionary<string, object>)ValoresFirestore.AFirestore(new Dictionary<string, bool> { { "si", true } });
            Assert.AreEqual(true, mapa["si"]);
            Assert.IsInstanceOf<Dictionary<string, object>>(ValoresFirestore.AFirestore(new Dictionary<string, string>()));
            Assert.Throws<ArgumentException>(() => ValoresFirestore.AFirestore(new Dictionary<int, string> { { 1, "a" } }));
        }

        [Test]
        public void Valores_EnUnaLista_NiOtraListaNiLaHoraDelServidor()
        {
            Assert.Throws<ArgumentException>(() => ValoresFirestore.AFirestore(new object[] { HoraServidor.Ahora }));
            Assert.Throws<ArgumentException>(() => ValoresFirestore.AFirestore(new object[] { new[] { 1, 2 } }));
            var lista = (List<object>)ValoresFirestore.AFirestore(new object[] { new Dictionary<string, object> { { "a", 1 } }, null, "b" });
            Assert.AreEqual(3, lista.Count);
        }

        // ───────────────────────── Android ─────────────────────────

        const string Android = "http://schemas.android.com/apk/res/android";
        const string Tools = "http://schemas.android.com/tools";

        [Test]
        public void Manifiesto_AnalyticsDesactivadoYSinPermisosDePublicidad()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            string principal = Directory.CreateDirectory(Path.Combine(carpeta, "launcher", "src", "main")).FullName;
            string unityLibrary = Directory.CreateDirectory(Path.Combine(carpeta, "unityLibrary")).FullName;
            string manifiesto = Path.Combine(principal, "AndroidManifest.xml");
            try
            {
                Assert.Throws<BuildFailedException>(() => new PrivacidadFirebaseAndroid().OnPostGenerateGradleAndroidProject(unityLibrary));
                File.WriteAllText(manifiesto, $"<manifest xmlns:android=\"{Android}\"><application android:label=\"a\" /></manifest>");
                for (int i = 0; i < 2; i++) new PrivacidadFirebaseAndroid().OnPostGenerateGradleAndroidProject(unityLibrary);

                var xml = new XmlDocument();
                xml.Load(manifiesto);
                var permisos = xml.SelectNodes("/manifest/uses-permission").Cast<XmlElement>().ToList();
                CollectionAssert.AreEquivalent(PrivacidadFirebaseAndroid.PermisosQuitados, permisos.Select(p => p.GetAttribute("name", Android)));
                CollectionAssert.Contains(PrivacidadFirebaseAndroid.PermisosQuitados, "com.google.android.gms.permission.AD_ID");
                Assert.IsTrue(permisos.All(p => p.GetAttribute("node", Tools) == "remove"));
                var meta = xml.SelectNodes("/manifest/application/meta-data").Cast<XmlElement>().Single();
                Assert.AreEqual("firebase_analytics_collection_deactivated", meta.GetAttribute("name", Android));
                Assert.AreEqual("true", meta.GetAttribute("value", Android));
            }
            finally { Directory.Delete(carpeta, recursive: true); }
        }

        [Test]
        public void Sdk_LasVersionesDelScriptSonLasDelProyecto()
        {
            string manifiesto = File.ReadAllText("Packages/manifest.json");
            string script = File.ReadAllText(Path.Combine(Repositorio, "firebase", "descargar-sdk.ps1"));
            var bajados = Regex.Matches(script, @"Nombre = '([^']+)'; Version = '([^']+)'; Sha256 = '([0-9a-f]{64})'").Cast<Match>().ToList();
            Assert.AreEqual(4, bajados.Count);
            foreach (var b in bajados)
                StringAssert.Contains($"\"{b.Groups[1].Value}\": \"file:Firebase/{b.Groups[1].Value}-{b.Groups[2].Value}.tgz\"", manifiesto);
            Assert.AreEqual(4, Regex.Matches(manifiesto, "\"file:Firebase/").Count);
        }

        // ───────────────────────── Nada secreto en git ─────────────────────────

        static string Repositorio => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

        static string Git(string argumentos, out int codigo)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo("git", argumentos)
                {
                    WorkingDirectory = Repositorio, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                },
            };
            try { p.Start(); }
            catch (System.ComponentModel.Win32Exception) { Assert.Ignore("Sin git no se puede comprobar."); }
            using (p)
            {
                string salida = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                codigo = p.ExitCode;
                if (codigo == 128) Assert.Ignore("No es un repositorio git.");
                return salida;
            }
        }

        [Test]
        public void Git_NoTieneLaConfiguracionDeFirebaseNiElSdk()
        {
            var ficheros = Git("ls-files --cached --others --exclude-standard", out int codigo).Split('\n');
            Assert.AreEqual(0, codigo);
            Assert.Greater(ficheros.Length, 100);
            var secretos = ficheros.Where(f => Regex.IsMatch(f,
                @"google-services.*\.(json|xml)|FirebaseApp\.androidlib|firebase-adminsdk|serviceAccount|\.tgz|\.srcaar|GeneratedLocalRepo", RegexOptions.IgnoreCase));
            Assert.IsEmpty(secretos);
        }

        [TestCase("unity/PriceMap/Assets/google-services.json")]
        [TestCase("unity/PriceMap/Assets/google-services.json.meta")]
        [TestCase("unity/PriceMap/Assets/StreamingAssets/google-services-desktop.json")]
        [TestCase("unity/PriceMap/Assets/Plugins/Android/FirebaseApp.androidlib/res/values/google-services.xml")]
        [TestCase("unity/PriceMap/Assets/GeneratedLocalRepo/Firebase/a.aar")]
        [TestCase("unity/PriceMap/Packages/Firebase/com.google.firebase.app-14.1.0.tgz")]
        [TestCase("pricemap-firebase-adminsdk-abc.json")]
        public void Git_IgnoraLoSecretoYLoQueSeGenera(string fichero)
        {
            Git("check-ignore -q --no-index " + fichero, out int codigo);
            Assert.AreEqual(0, codigo, fichero);
        }

        [Test]
        public void Git_NoTieneClavesDeGoogle()
        {
            // git grep acaba en 1 si no encuentra nada
            string donde = Git("grep --untracked -lE AIza[0-9A-Za-z_-]{35}", out int codigo);
            Assert.AreEqual(1, codigo, donde);
        }
    }
}
