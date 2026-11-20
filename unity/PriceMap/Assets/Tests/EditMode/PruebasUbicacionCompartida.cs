using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Busqueda;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Enlaces y coordenadas pegados en el buscador (historia 2.5, parte 2): el analizador (LectorUbicacionCompartida),
    /// cada fila de la matriz y casos raros, y la hoja en la pantalla real con un resolutor de enlaces cortos falso.
    /// Nada sale a la red.
    /// </summary>
    public class PruebasUbicacionCompartida
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        static readonly Coordenada Punto = new Coordenada(36.778, -6.351);

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        // ───────────────────────── Analizador ─────────────────────────

        static void AssertPunto(Coordenada? c, double lat, double lon, string texto)
        {
            Assert.IsTrue(c.HasValue, $"\"{texto}\" debería leerse como coordenada.");
            Assert.AreEqual(lat, c.Value.Latitud, 1e-9, texto);
            Assert.AreEqual(lon, c.Value.Longitud, 1e-9, texto);
        }

        [TestCase("https://maps.google.com/?q=36.778,-6.351", 36.778, -6.351, TestName = "Google ?q=")]
        [TestCase("https://www.google.com/maps/place/Bar/@36.778,-6.351,17z", 36.778, -6.351, TestName = "Google @")]
        [TestCase("https://www.google.com/maps/place/Bar/@36.7,-6.3,17z/data=!3m1!4b1!4m6!3m5!1s0x0:0x0!8m2!3d36.778!4d-6.351", 36.778, -6.351,
            TestName = "Google !3d!4d gana a @")]
        [TestCase("https://www.google.com/maps/search/?api=1&query=36.778%2C-6.351", 36.778, -6.351, TestName = "Google query= codificado")]
        [TestCase("https://maps.google.com/maps?ll=36.778,-6.351&z=16", 36.778, -6.351, TestName = "Google ll=")]
        [TestCase("https://maps.google.com/?q=Bar+Pepe&ll=36.778,-6.351", 36.778, -6.351, TestName = "q con nombre: vale ll=")]
        [TestCase("https://maps.apple.com/?ll=36.778,-6.351", 36.778, -6.351, TestName = "Apple ll=")]
        [TestCase("https://maps.apple.com/?q=36.778,-6.351", 36.778, -6.351, TestName = "Apple q=")]
        [TestCase("geo:0,0?q=36.778,-6.351(Casa)", 36.778, -6.351, TestName = "geo con q gana")]
        [TestCase("geo:36.1,-6.1?q=36.778,-6.351(Casa)", 36.778, -6.351, TestName = "geo con q gana al camino")]
        [TestCase("geo:36.778,-6.351", 36.778, -6.351, TestName = "geo sin q")]
        [TestCase("geo:36.778,-6.351;u=35", 36.778, -6.351, TestName = "geo con incertidumbre")]
        [TestCase("Plaza del Cabildo https://maps.google.com/?q=36.778,-6.351", 36.778, -6.351, TestName = "enlace detrás del nombre")]
        [TestCase("36.7781, -6.3515", 36.7781, -6.3515, TestName = "sueltas con coma")]
        [TestCase("36.7781 -6.3515", 36.7781, -6.3515, TestName = "sueltas con espacio")]
        [TestCase("36,7781 -6,3515", 36.7781, -6.3515, TestName = "sueltas con coma decimal")]
        [TestCase("36,7781; -6,3515", 36.7781, -6.3515, TestName = "sueltas con punto y coma")]
        [TestCase("  36,778 -6,351 ", 36.778, -6.351, TestName = "sueltas con espacios alrededor")]
        [TestCase("36,7781,-6,3515", 36.7781, -6.3515, TestName = "sueltas con coma decimal y coma")]
        [TestCase("-33.8688, 151.2093", -33.8688, 151.2093, TestName = "hemisferio sur y este")]
        [TestCase("www.google.com/maps/@36.778,-6.351,17z", 36.778, -6.351, TestName = "Google sin esquema")]
        [TestCase("maps.google.com/?q=36.778,-6.351", 36.778, -6.351, TestName = "maps.google sin esquema")]
        [TestCase("Mira maps.apple.com/?ll=36.778,-6.351", 36.778, -6.351, TestName = "Apple sin esquema detrás de texto")]
        public void LeeLaCoordenada(string texto, double lat, double lon) =>
            AssertPunto(LectorUbicacionCompartida.Leer(texto), lat, lon, texto);

        [TestCase("95, 200", TestName = "fuera de rango sin decimales")]
        [TestCase("95.0, 200.0", TestName = "fuera de rango")]
        [TestCase("36.7781, 200.5", TestName = "longitud fuera de rango")]
        [TestCase("10 20", TestName = "números sin decimales")]
        [TestCase("1,5 2,5", TestName = "números con un decimal")]
        [TestCase("calle ancha 10", TestName = "texto normal")]
        [TestCase("36.7781", TestName = "un solo número")]
        [TestCase("https://maps.google.com/?cid=123", TestName = "Google cid")]
        [TestCase("https://maps.google.com/?q=Bar+Pepe", TestName = "q con nombre")]
        [TestCase("geo:0,0?q=Calle+Ancha", TestName = "geo sin coordenadas")]
        [TestCase("https://www.google.com/maps/@95.1,-6.351,17z", TestName = "@ fuera de rango")]
        [TestCase("https://maps.app.goo.gl/abc", TestName = "enlace corto")]
        [TestCase("", TestName = "vacío")]
        [TestCase(null, TestName = "null")]
        public void NoEsUnaCoordenada(string texto) => Assert.IsNull(LectorUbicacionCompartida.Leer(texto));

        [TestCase("https://maps.google.com/?cid=123", true)]
        [TestCase("geo:0,0?q=Calle+Ancha", true)]
        [TestCase("maps.google.com/algo", true)]
        [TestCase("HTTP://EJEMPLO.COM", true)]
        [TestCase("goo.gl/maps/xyz", true)]
        [TestCase("calle ancha 10", false)]
        [TestCase("95, 200", false)]
        public void PareceEnlace(string texto, bool parece) => Assert.AreEqual(parece, LectorUbicacionCompartida.PareceEnlace(texto));

        [TestCase("https://maps.app.goo.gl/abc", true)]
        [TestCase("Mira esto https://maps.app.goo.gl/AbC123", true)]
        [TestCase("https://goo.gl/maps/xyz", true)]
        [TestCase("https://www.google.com/maps/@36.77,-6.35,17z", false)]
        [TestCase("https://goo.gl/otra", false)]
        [TestCase("maps.app.goo.gl/abc", true, TestName = "corto sin esquema")]
        [TestCase("http://goo.gl/maps/xyz", true, TestName = "corto con http")]
        [TestCase("https://ejemplo.com/?r=maps.app.goo.gl", false, TestName = "solo lo nombra otro servidor")]
        [TestCase("https://ejemplo.com/x maps.app.goo.gl/abc", false, TestName = "el primer enlace es de otro servidor")]
        public void EsEnlaceCorto(string texto, bool corto) => Assert.AreEqual(corto, LectorUbicacionCompartida.EsEnlaceCorto(texto));

        [Test]
        public void ExtraeElEnlaceDelTexto()
        {
            Assert.AreEqual("https://maps.app.goo.gl/AbC123", LectorUbicacionCompartida.ExtraerEnlace("Bar Pepe https://maps.app.goo.gl/AbC123 "));
            Assert.AreEqual("https://maps.app.goo.gl/AbC123", LectorUbicacionCompartida.ExtraerEnlace("Bar Pepe maps.app.goo.gl/AbC123"),
                "Sin esquema: solo el enlace, con https.");
            Assert.IsNull(LectorUbicacionCompartida.ExtraerEnlace("calle ancha"));
        }

        [Test]
        public void ElDestinoRelativoSeHaceAbsoluto()
        {
            Assert.AreEqual("https://maps.app.goo.gl/otro", ResolutorEnlacesWeb.Absoluta("https://maps.app.goo.gl/abc", "/otro"));
            Assert.AreEqual("https://www.google.com/maps/@36.77,-6.35,17z",
                ResolutorEnlacesWeb.Absoluta("https://maps.app.goo.gl/abc", "https://www.google.com/maps/@36.77,-6.35,17z"));
        }

        // ───────────────────────── Resolutor: saltos (sin red) ─────────────────────────

        /// <summary>Saltos falsos: cada URL pedida redirige a la siguiente del mapa (o no redirige si no está).</summary>
        static (ResolutorEnlacesWeb resolutor, List<string> pedidas) ConSaltos(Dictionary<string, Resultado<string>> saltos)
        {
            var pedidas = new List<string>();
            var resolutor = new ResolutorEnlacesWeb((url, ct) =>
            {
                pedidas.Add(url);
                return Task.FromResult(saltos.TryGetValue(url, out var r) ? r : Resultado<string>.Exito(null));
            });
            return (resolutor, pedidas);
        }

        const string Final = "https://www.google.com/maps/@36.77,-6.35,17z";

        [Test]
        public void Resolutor_SigueLosCortosYParaEnLaPrimeraUrlLarga()
        {
            var (resolutor, pedidas) = ConSaltos(new Dictionary<string, Resultado<string>>
            {
                ["https://maps.app.goo.gl/abc"] = Resultado<string>.Exito("https://maps.app.goo.gl/abc2"),
                ["https://maps.app.goo.gl/abc2"] = Resultado<string>.Exito(Final),
                [Final] = Resultado<string>.Exito("https://consent.google.com/…"),
            });
            var r = resolutor.Resolver("https://maps.app.goo.gl/abc", CancellationToken.None).Result;
            Assert.IsTrue(r.Ok);
            Assert.AreEqual(Final, r.Valor);
            CollectionAssert.AreEqual(new[] { "https://maps.app.goo.gl/abc", "https://maps.app.goo.gl/abc2" }, pedidas,
                "La página de Google no se llega a pedir (en Europa manda al aviso de cookies).");
        }

        [Test]
        public void Resolutor_SinRedireccionDevuelveLaPedida()
        {
            var (resolutor, pedidas) = ConSaltos(new Dictionary<string, Resultado<string>>());
            var r = resolutor.Resolver(" https://maps.app.goo.gl/abc ", CancellationToken.None).Result;
            Assert.IsTrue(r.Ok);
            Assert.AreEqual("https://maps.app.goo.gl/abc", r.Valor);
            Assert.AreEqual(1, pedidas.Count);
        }

        [Test]
        public void Resolutor_MasDeCincoSaltosEsUnFallo()
        {
            var saltos = new Dictionary<string, Resultado<string>>();
            for (int i = 0; i < 10; i++) saltos["https://maps.app.goo.gl/" + i] = Resultado<string>.Exito("https://maps.app.goo.gl/" + (i + 1));
            var (resolutor, pedidas) = ConSaltos(saltos);
            var r = resolutor.Resolver("https://maps.app.goo.gl/0", CancellationToken.None).Result;
            Assert.IsFalse(r.Ok);
            Assert.AreEqual(ResolutorEnlacesWeb.MaximoSaltos, pedidas.Count);
        }

        [Test]
        public void Resolutor_UnFalloDelSaltoSeDevuelveTalCual()
        {
            var (resolutor, _) = ConSaltos(new Dictionary<string, Resultado<string>>
            {
                ["https://maps.app.goo.gl/abc"] = Resultado<string>.Fallo("sin conexión"),
            });
            var r = resolutor.Resolver("https://maps.app.goo.gl/abc", CancellationToken.None).Result;
            Assert.IsFalse(r.Ok);
            Assert.AreEqual("sin conexión", r.Error);
        }

        [Test]
        public void Resolutor_UnCortoConHttpSePideConHttps()
        {
            var (resolutor, pedidas) = ConSaltos(new Dictionary<string, Resultado<string>>
            {
                ["https://goo.gl/maps/xyz"] = Resultado<string>.Exito("http://maps.app.goo.gl/abc"),
                ["https://maps.app.goo.gl/abc"] = Resultado<string>.Exito(Final),
            });
            var r = resolutor.Resolver("http://goo.gl/maps/xyz", CancellationToken.None).Result;
            Assert.AreEqual(Final, r.Valor);
            CollectionAssert.AreEqual(new[] { "https://goo.gl/maps/xyz", "https://maps.app.goo.gl/abc" }, pedidas);
        }

        // ───────────────────────── Falsos ─────────────────────────

        class Resolucion
        {
            public string Url;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<string>> Tcs;
            public void Responder(string url) => Tcs.TrySetResult(Resultado<string>.Exito(url));
            public void Fallar() => Tcs.TrySetResult(Resultado<string>.Fallo("sin conexión"));
        }

        /// <summary>Resolutor falso: cada enlace queda pendiente hasta que la prueba responde.</summary>
        class ResolutorFalso : IResolutorEnlaces
        {
            /// <summary>No atiende a la cancelación: su respuesta puede llegar tarde.</summary>
            public bool IgnorarCancelacion;
            public readonly List<Resolucion> Peticiones = new List<Resolucion>();

            public Task<Resultado<string>> Resolver(string url, CancellationToken ct)
            {
                var r = new Resolucion { Url = url, Ct = ct, Tcs = new TaskCompletionSource<Resultado<string>>() };
                Peticiones.Add(r);
                if (!IgnorarCancelacion) ct.Register(() => r.Tcs.TrySetCanceled());
                return r.Tcs.Task;
            }
        }

        /// <summary>Geocodificador falso: cuenta las búsquedas (no debe haber con un enlace); la inversa queda pendiente.</summary>
        class GeoFalso : IGeocodificador
        {
            public readonly List<string> Busquedas = new List<string>();

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct)
            {
                Busquedas.Add(consulta);
                return Task.FromResult(Resultado<IReadOnlyList<Direccion>>.Exito(Array.Empty<Direccion>()));
            }

            public Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct) =>
                new TaskCompletionSource<Resultado<Direccion>>().Task;
        }

        static Task EsperaInmediata(TimeSpan t, CancellationToken ct) => Task.CompletedTask;

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        class MapaFalso : IMapa
        {
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => Cabildo;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
        }

        class Repositorio : IRepositorioLugares
        {
            readonly IReadOnlyList<Lugar> lugares;
            public Repositorio(params Lugar[] lugares) => this.lugares = lugares;
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(lugares);
        }

        GameObject objeto;
        PantallaMapa pantalla;
        HojaBuscador hoja;
        GeoFalso geo;
        ResolutorFalso resolutor;
        SynchronizationContext contextoAntes;
        List<string> anuncios;
        Action<string> oir;

        [SetUp]
        public void Preparar()
        {
            // Sin contexto de sincronización, la continuación corre en cuanto la prueba responde la petición falsa
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            // Un bar con "36" en el nombre: si se buscaran lugares con unas coordenadas, saldría
            var lugares = new ServicioLugares(new Repositorio(
                new Lugar("bar-36", "Bar 36", "Bar", Plantilla.Hosteleria, Cabildo, "Calle Ancha, 36")));
            Assert.IsTrue(lugares.Cargar().Ok);
            var mapa = new MapaFalso();
            geo = new GeoFalso();
            resolutor = new ResolutorFalso();
            pantalla.Inicializar(mapa, tema, lugares, Ahora, arbol, filtro,
                busqueda: new ServicioBusqueda(lugares, mapa, Ahora),
                direcciones: new BuscadorDirecciones(geo, EsperaInmediata),
                geocodificador: geo, resolutorEnlaces: resolutor);
            tema.Iniciar();
            filtro.Iniciar();
            hoja = pantalla.Buscador;
            hoja.Medir(800f);
        }

        [TearDown]
        public void Limpiar()
        {
            if (oir != null) AccesibilidadPantalla.AnuncioPedido -= oir;
            oir = null;
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        void AbrirYEscribir(string texto)
        {
            hoja.Abrir();
            pantalla.ActualizarCapas(0f);
            hoja.Escribir(texto);
        }

        void ConLector()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            anuncios = new List<string>();
            oir = anuncios.Add;
            AccesibilidadPantalla.AnuncioPedido += oir;
        }

        /// <summary>La hoja enseña solo «Ir a este punto» en esa coordenada, sin direcciones ni "No hay resultados".</summary>
        void AssertIrAPunto(Coordenada c, string coordenadas)
        {
            var fila = hoja.Filas.Single();
            Assert.AreEqual(TipoResultado.Coordenada, fila.Resultado.Tipo);
            Assert.AreEqual(c, fila.Resultado.Posicion);
            Assert.AreEqual("Ir a este punto", fila.Nombre.text);
            Assert.AreEqual(coordenadas, fila.Detalle.text, "De subtítulo, las coordenadas como en la ficha.");
            Assert.IsNull(fila.Precio);
            Assert.AreEqual(FilaResultado.GlifoChincheta, FilaResultado.GlifoDe(fila.Resultado), "El glifo de la chincheta.");
            Assert.IsTrue(fila.Icono.ClassListContains(FilaResultado.ClaseIconoCoordenada));
            Assert.IsTrue(fila.Glifo.ClassListContains("buscador__resultado-glifo--chincheta"));
            Assert.IsFalse(hoja.SinResultadosVisible);
            Assert.IsFalse(hoja.BuscarDireccionVisible);
            Assert.IsFalse(hoja.AvisoDireccionesVisible);
            Assert.IsEmpty(geo.Busquedas, "Con un enlace o coordenadas no se buscan direcciones.");
        }

        void AssertMensaje(string clave)
        {
            Assert.IsEmpty(hoja.Filas);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.AreEqual(T(clave), hoja.SinResultados.text, "En el sitio de \"No hay resultados\".");
            Assert.IsFalse(hoja.BuscarDireccionVisible, "Sin \"Buscar como dirección\".");
            Assert.IsFalse(hoja.AvisoDireccionesVisible);
            Assert.IsEmpty(geo.Busquedas);
        }

        // ───────────────────────── Matriz: pantalla ─────────────────────────

        [Test]
        public void GoogleQ_DaIrAEstePunto()
        {
            AbrirYEscribir("https://maps.google.com/?q=36.778,-6.351");
            AssertIrAPunto(Punto, "36,7780, -6,3510");
            Assert.IsEmpty(resolutor.Peticiones, "Un enlace completo no se resuelve.");
        }

        [Test]
        public void GoogleArroba_DaEsePunto()
        {
            AbrirYEscribir("https://www.google.com/maps/@36.778,-6.351,17z");
            AssertIrAPunto(Punto, "36,7780, -6,3510");
        }

        [Test]
        public void GoogleDatos_DaElPuntoDe3d4d()
        {
            AbrirYEscribir("https://www.google.com/maps/place/Bar/@36.7,-6.3,17z/data=!3d36.778!4d-6.351");
            AssertIrAPunto(Punto, "36,7780, -6,3510");
        }

        [Test]
        public void Apple_DaEsePunto()
        {
            AbrirYEscribir("https://maps.apple.com/?ll=36.778,-6.351");
            AssertIrAPunto(Punto, "36,7780, -6,3510");
        }

        [Test]
        public void Geo_DaElPuntoDeQ()
        {
            AbrirYEscribir("geo:0,0?q=36.778,-6.351(Casa)");
            AssertIrAPunto(Punto, "36,7780, -6,3510");
        }

        [Test]
        public void ComaDecimal_DaEsePuntoYNoBuscaLugares()
        {
            AbrirYEscribir("36,778 -6,351");
            AssertIrAPunto(Punto, "36,7780, -6,3510");
        }

        [Test]
        public void EnlaceCorto_ComprobandoYTrasResolverloEsePunto()
        {
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            var r = resolutor.Peticiones.Single();
            Assert.AreEqual("https://maps.app.goo.gl/abc", r.Url);
            AssertMensaje(ClavesTexto.BuscadorComprobandoEnlace);
            Assert.AreEqual("Comprobando el enlace…", hoja.SinResultados.text);

            r.Responder("https://www.google.com/maps/place/Bar/@36.77,-6.35,17z");
            AssertIrAPunto(new Coordenada(36.77, -6.35), "36,7700, -6,3500");
        }

        [Test]
        public void EnlaceCortoDetrasDelNombre_SeResuelveSoloElEnlace()
        {
            AbrirYEscribir("Bar Pepe https://maps.app.goo.gl/AbC123");
            Assert.AreEqual("https://maps.app.goo.gl/AbC123", resolutor.Peticiones.Single().Url);
        }

        [Test]
        public void EnlaceCortoSinEsquema_SeResuelveConHttps()
        {
            AbrirYEscribir("Bar Pepe maps.app.goo.gl/AbC123");
            Assert.AreEqual("https://maps.app.goo.gl/AbC123", resolutor.Peticiones.Single().Url);
            resolutor.Peticiones.Single().Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            AssertIrAPunto(new Coordenada(36.77, -6.35), "36,7700, -6,3500");
        }

        [Test]
        public void FueraDeRango_BusquedaNormal()
        {
            AbrirYEscribir("95, 200");
            Assert.IsFalse(pantalla.EnEnlace);
            Assert.IsFalse(hoja.Filas.Any(f => f.Resultado.Tipo == TipoResultado.Coordenada));
            Assert.AreEqual("No hay resultados", hoja.SinResultados.text);
            CollectionAssert.AreEqual(new[] { "95, 200" }, geo.Busquedas, "Se buscan direcciones como siempre.");
        }

        [Test]
        public void EnlaceRaro_MensajeDeNoReconocido()
        {
            AbrirYEscribir("https://maps.google.com/?cid=123");
            AssertMensaje(ClavesTexto.BuscadorEnlaceNoReconocido);
            Assert.AreEqual("No reconozco esta ubicación. Prueba a copiar el enlace completo.", hoja.SinResultados.text);
            Assert.IsEmpty(resolutor.Peticiones);
        }

        [Test]
        public void EnlaceCortoSinRed_MensajeDeNoReconocido()
        {
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            resolutor.Peticiones.Single().Fallar();
            AssertMensaje(ClavesTexto.BuscadorEnlaceNoReconocido);
        }

        [Test]
        public void EnlaceCortoQueAcabaEnOtraCosa_MensajeDeNoReconocido()
        {
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            resolutor.Peticiones.Single().Responder("https://www.google.com/maps?cid=123");
            AssertMensaje(ClavesTexto.BuscadorEnlaceNoReconocido);
        }

        [Test]
        public void TocarLaFila_AbreLaFichaDePuntoLibre()
        {
            AbrirYEscribir("https://maps.google.com/?q=36.778,-6.351");
            var fila = hoja.Filas.Single();
            pantalla.ElegirResultado(fila.Resultado); // lo que hace el Clickable de la fila al tocarla
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.AreEqual(Punto, pantalla.PuntoAbierto, "Ficha de punto libre en esa coordenada.");
            Assert.IsNull(pantalla.Ficha.Lugar);
            Assert.AreEqual("36,7780, -6,3510", pantalla.Ficha.Titulo.text, "Con las coordenadas mientras llega la dirección.");
        }

        // ───────────────────────── Al cambiar el texto ─────────────────────────

        [Test]
        public void SiCambiaElTextoSeCancelaYLaRespuestaTardiaNoPintaNada()
        {
            resolutor.IgnorarCancelacion = true;
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            var r = resolutor.Peticiones.Single();
            hoja.Escribir("bar");
            Assert.IsTrue(r.Ct.IsCancellationRequested, "Se cancela al cambiar el texto.");
            Assert.IsFalse(pantalla.EnEnlace);
            r.Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            Assert.IsFalse(hoja.Filas.Any(f => f.Resultado.Tipo == TipoResultado.Coordenada), "La respuesta tardía no pinta nada.");
            Assert.AreEqual("Bar 36", hoja.Filas.First().Resultado.Nombre, "Se busca como siempre.");
        }

        [Test]
        public void OtroEnlaceCortoCancelaElAnterior()
        {
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            hoja.Escribir("https://maps.app.goo.gl/abcd");
            Assert.AreEqual(2, resolutor.Peticiones.Count);
            Assert.IsTrue(resolutor.Peticiones[0].Ct.IsCancellationRequested);
            resolutor.Peticiones[1].Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            AssertIrAPunto(new Coordenada(36.77, -6.35), "36,7700, -6,3500");
        }

        [Test]
        public void AlVolverADesplegarNoSeResuelveOtraVez()
        {
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            resolutor.Peticiones.Single().Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            pantalla.PlegarBuscador();
            hoja.Abrir();
            pantalla.Buscar(hoja.Campo.value); // lo que hace la pantalla al desplegar la hoja
            Assert.AreEqual(1, resolutor.Peticiones.Count);
            AssertIrAPunto(new Coordenada(36.77, -6.35), "36,7700, -6,3500");
        }

        [Test]
        public void ElMensajeSeQuedaMientrasSigaElTextoYSeVaAlBorrarlo()
        {
            AbrirYEscribir("https://maps.google.com/?cid=123");
            pantalla.Buscar(hoja.Campo.value);
            AssertMensaje(ClavesTexto.BuscadorEnlaceNoReconocido);
            hoja.Escribir("zzzz");
            Assert.IsFalse(pantalla.EnEnlace);
            Assert.AreEqual("No hay resultados", hoja.SinResultados.text, "Vuelve el texto de siempre.");
            CollectionAssert.Contains(geo.Busquedas, "zzzz", "Y la búsqueda normal (la espera de direcciones es inmediata).");
            hoja.Escribir("");
            Assert.IsFalse(hoja.MostrandoResultados);
        }

        [Test]
        public void SinResolutor_ElEnlaceCortoNoSeReconoce()
        {
            var otro = new GameObject("Otra pantalla");
            try
            {
                var p = otro.AddComponent<PantallaMapa>();
                var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
                var prefs = new PreferenciasFalsas();
                var lugares = new ServicioLugares(new Repositorio());
                Assert.IsTrue(lugares.Cargar().Ok);
                p.Inicializar(new MapaFalso(), new ServicioTema(prefs, new SistemaClaro()), lugares, Ahora, arbol, new ServicioFiltro(prefs));
                p.Buscador.Medir(800f);
                p.Buscador.Abrir();
                p.Buscador.Escribir("https://maps.app.goo.gl/abc");
                Assert.AreEqual(T(ClavesTexto.BuscadorEnlaceNoReconocido), p.Buscador.SinResultados.text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(otro);
            }
        }

        // ───────────────────────── TalkBack ─────────────────────────

        [Test]
        public void TalkBack_LaFilaEsUnBotonYSeAnunciaUnResultado()
        {
            ConLector();
            AbrirYEscribir("https://maps.google.com/?q=36.7781,-6.3515");
            var fila = hoja.Filas.Single();
            Assert.AreEqual("Ir a este punto, 36,7781, -6,3515", fila.Anuncio);
            Assert.AreEqual("1 resultado", anuncios.LastOrDefault());
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), fila.Anuncio);
        }

        [Test]
        public void TalkBack_OtraCoordenadaSeAnunciaOtraVez()
        {
            ConLector();
            AbrirYEscribir("36.778, -6.351");
            anuncios.Clear();
            hoja.Escribir("36.779, -6.351");
            CollectionAssert.AreEqual(new[] { "1 resultado" }, anuncios, "La fila cambia de punto: se anuncia otra vez.");
            Assert.AreEqual("Ir a este punto, 36,7790, -6,3510", hoja.Filas.Single().Anuncio);
        }

        [Test]
        public void ConLaListaCercaYElCampoVacio_ElEnlaceSeOlvida()
        {
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            var r = resolutor.Peticiones.Single();
            pantalla.ElegirCategoria(null);
            pantalla.Buscar("");
            Assert.IsFalse(pantalla.EnEnlace);
            Assert.IsTrue(r.Ct.IsCancellationRequested, "Su resolución se cancela.");
        }

        [Test]
        public void TalkBack_ElMensajeDeNoReconocidoSeVeYSeAnuncia()
        {
            ConLector();
            AbrirYEscribir("https://maps.google.com/?cid=123");
            Assert.AreEqual(T(ClavesTexto.BuscadorEnlaceNoReconocido), anuncios.LastOrDefault());
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), T(ClavesTexto.BuscadorEnlaceNoReconocido));
            anuncios.Clear();
            hoja.Escribir("https://maps.google.com/?cid=1234"); // sigue sin reconocerse: no se repite
            CollectionAssert.DoesNotContain(anuncios, T(ClavesTexto.BuscadorEnlaceNoReconocido));
        }

        [Test]
        public void TalkBack_EnlaceCortoAnunciaComprobandoYLuegoUnResultado()
        {
            ConLector();
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            Assert.AreEqual("Comprobando el enlace…", anuncios.LastOrDefault());
            anuncios.Clear();
            resolutor.Peticiones.Single().Responder("https://www.google.com/maps/@36.77,-6.35,17z");
            CollectionAssert.AreEqual(new[] { "1 resultado" }, anuncios);
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), hoja.Filas.Single().Anuncio);
        }

        [Test]
        public void TalkBack_EnlaceCortoQueFallaAnunciaElMensaje()
        {
            ConLector();
            AbrirYEscribir("https://maps.app.goo.gl/abc");
            anuncios.Clear();
            resolutor.Peticiones.Single().Fallar();
            CollectionAssert.AreEqual(new[] { T(ClavesTexto.BuscadorEnlaceNoReconocido) }, anuncios);
        }

        [Test]
        public void TalkBack_ElDobleToqueEnLaFilaAbreLaFichaDelPunto()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            pantalla.Accesibilidad.Reconstruir();
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            pantalla.AbrirBuscadorConLector();
            hoja.Escribir("36.778, -6.351");
            var fila = hoja.Filas.Single();
            var nodo = AssistiveSupport.activeHierarchy.rootNodes.Single(n => n.label == fila.Anuncio);
            Assert.AreEqual(AccessibilityRole.Button, nodo.role);
            const System.Reflection.BindingFlags Campos = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var campo = typeof(AccessibilityNode).GetFields(Campos)
                .FirstOrDefault(f => typeof(Func<bool>).IsAssignableFrom(f.FieldType) && f.Name.IndexOf("invoked", StringComparison.OrdinalIgnoreCase) >= 0);
            if (campo == null) Assert.Inconclusive("Esta versión de Unity no deja leer el evento invoked del nodo.");
            Assert.IsTrue(((Func<bool>)campo.GetValue(nodo))());
            Assert.AreEqual(Punto, pantalla.PuntoAbierto);
        }
    }
}
