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
    /// Direcciones del buscador (historia 2.3, parte 2): la lectura de Photon (LecturaPhoton, con un JSON de ejemplo),
    /// la espera y la cancelación (BuscadorDirecciones) y la hoja en la pantalla real, una prueba por fila de la
    /// matriz. Nada sale a la red: el geocodificador y la espera son falsos.
    /// </summary>
    public class PruebasDirecciones
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        static readonly Coordenada Cabildo = new Coordenada(36.7779, -6.3528);
        const string Nbsp = " ";

        // ───────────────────────── Falsos ─────────────────────────

        /// <summary>Espera falsa: inmediata, o pendiente hasta que la prueba la suelta (cancelable).</summary>
        class EsperaFalsa
        {
            public bool Inmediata = true;
            public readonly List<TimeSpan> Pedidas = new List<TimeSpan>();
            public readonly List<TaskCompletionSource<bool>> Pendientes = new List<TaskCompletionSource<bool>>();

            public Task Esperar(TimeSpan t, CancellationToken ct)
            {
                Pedidas.Add(t);
                if (Inmediata) return Task.CompletedTask;
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetCanceled());
                Pendientes.Add(tcs);
                return tcs.Task;
            }
        }

        class Peticion
        {
            public string Consulta;
            public Coordenada Cerca;
            public CancellationToken Ct;
            public TaskCompletionSource<Resultado<IReadOnlyList<Direccion>>> Tcs;
        }

        /// <summary>Geocodificador falso: responde ya (Responder) o deja la petición pendiente.</summary>
        class GeoFalso : IGeocodificador
        {
            public Func<string, Resultado<IReadOnlyList<Direccion>>> Responder;
            /// <summary>No atiende a la cancelación: su respuesta puede llegar tarde.</summary>
            public bool IgnorarCancelacion;
            public readonly List<Peticion> Peticiones = new List<Peticion>();

            public Task<Resultado<IReadOnlyList<Direccion>>> Buscar(string consulta, Coordenada cerca, CancellationToken ct)
            {
                var p = new Peticion { Consulta = consulta, Cerca = cerca, Ct = ct, Tcs = new TaskCompletionSource<Resultado<IReadOnlyList<Direccion>>>() };
                Peticiones.Add(p);
                if (Responder != null) p.Tcs.SetResult(Responder(consulta));
                else if (!IgnorarCancelacion) ct.Register(() => p.Tcs.TrySetCanceled());
                return p.Tcs.Task;
            }

            public Task<Resultado<Direccion>> Inverso(Coordenada punto, CancellationToken ct) =>
                Task.FromResult(Resultado<Direccion>.Fallo("sin conexión"));
        }

        static readonly Direccion CalleAncha = new Direccion("Calle Ancha 10", "Sanlúcar de Barrameda", AlNorte(Cabildo, 1234));

        static Resultado<IReadOnlyList<Direccion>> Unas(params Direccion[] d) => Resultado<IReadOnlyList<Direccion>>.Exito(d);
        static Resultado<IReadOnlyList<Direccion>> SinRed() =>
            Resultado<IReadOnlyList<Direccion>>.Fallo("sin conexión", Array.Empty<Direccion>());

        static Coordenada AlNorte(Coordenada c, double metros) => new Coordenada(c.Latitud + metros / 111195.0, c.Longitud);

        SynchronizationContext contextoAntes;

        [SetUp]
        public void SinContexto()
        {
            // Sin contexto de sincronización, las continuaciones corren en cuanto la prueba completa una tarea falsa
            contextoAntes = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
        }

        [TearDown]
        public void RestaurarContexto()
        {
            SinOir();
            LimpiarPantalla();
            SynchronizationContext.SetSynchronizationContext(contextoAntes);
        }

        // ───────────────────────── LecturaPhoton ─────────────────────────

        const string JsonEjemplo = @"{""type"":""FeatureCollection"",""features"":[
 {""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[-6.3539,36.7781]},
  ""properties"":{""osm_id"":1,""street"":""Calle Ancha"",""housenumber"":""10"",""city"":""Sanlúcar de Barrameda"",""county"":""Cádiz"",""country"":""España""}},
 {""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[-6.35,36.77]},
  ""properties"":{""name"":""Calle Ancha"",""district"":""Barrio Alto"",""county"":""Cádiz""}},
 {""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[-3.7,40.4]},
  ""properties"":{""name"":""Plaza Mayor"",""county"":""Madrid""}},
 {""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[-6.0,37.0]},
  ""properties"":{""housenumber"":""3"",""city"":""Sevilla""}},
 {""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[-6.0,37.0]},
  ""properties"":{""street"":""Calle Real""}}
]}";

        [Test]
        public void LeePhotonConLonLatNombreYLocalidad()
        {
            var r = LecturaPhoton.Leer(JsonEjemplo);
            Assert.IsTrue(r.Ok, r.Error);
            var d = r.Valor;
            Assert.AreEqual(4, d.Count, "Fuera el registro sin nombre ni calle.");
            Assert.AreEqual("Calle Ancha 10", d[0].Nombre, "Calle + número.");
            Assert.AreEqual("Sanlúcar de Barrameda", d[0].Localidad, "city primero.");
            Assert.AreEqual(36.7781, d[0].Posicion.Latitud, 1e-9, "coordinates viene como [lon, lat].");
            Assert.AreEqual(-6.3539, d[0].Posicion.Longitud, 1e-9);
            Assert.AreEqual("Calle Ancha", d[1].Nombre, "name.");
            Assert.AreEqual("Barrio Alto", d[1].Localidad, "district si no hay city.");
            Assert.AreEqual("Madrid", d[2].Localidad, "county si no hay city ni district.");
            Assert.AreEqual("Calle Real", d[3].Nombre, "Calle sin número.");
            Assert.AreEqual("", d[3].Localidad);
        }

        [TestCase("")]
        [TestCase("<html>502 Bad Gateway</html>")]
        [TestCase("{\"message\":\"error\"}")]
        public void UnaRespuestaRotaEsUnFallo(string json)
        {
            var r = LecturaPhoton.Leer(json);
            Assert.IsFalse(r.Ok);
            Assert.IsNotNull(r.Valor);
            Assert.IsEmpty(r.Valor);
        }

        [Test]
        public void SinRasgosNoHayDirecciones()
        {
            var r = LecturaPhoton.Leer("{\"type\":\"FeatureCollection\",\"features\":[]}");
            Assert.IsTrue(r.Ok);
            Assert.IsEmpty(r.Valor);
        }

        [Test]
        public void LaUrlLlevaConsultaCercaniaLimiteYLang()
        {
            Assert.AreEqual("https://photon.komoot.io/api/?q=calle%20ancha%2010&lat=36.778&lon=-6.353&limit=8&lang=default",
                GeocodificadorPhoton.Url(" calle ancha 10 ", Cabildo), "La cercanía va redondeada a 3 decimales (~100 m).");
        }

        [Test]
        public void LosTramosRepetidosDeUnaCalleSalenUnaVez()
        {
            const string json = @"{""features"":[
 {""geometry"":{""coordinates"":[-6.35,36.77]},""properties"":{""name"":""Calle Ancha"",""city"":""Sanlúcar de Barrameda""}},
 {""geometry"":{""coordinates"":[-6.351,36.771]},""properties"":{""name"":""Calle Ancha"",""city"":""Sanlúcar de Barrameda""}},
 {""geometry"":{""coordinates"":[-4.88,36.51]},""properties"":{""name"":""Calle Ancha"",""city"":""Marbella""}}
]}";
            var r = LecturaPhoton.Leer(json);
            Assert.IsTrue(r.Ok, r.Error);
            CollectionAssert.AreEqual(new[] { "Sanlúcar de Barrameda", "Marbella" }, r.Valor.Select(d => d.Localidad).ToArray());
        }

        // ───────────────────────── BuscadorDirecciones ─────────────────────────

        [Test]
        public void PideUnaVezTrasLaEsperaYAvisaConLaDistancia()
        {
            var espera = new EsperaFalsa();
            var geo = new GeoFalso { Responder = _ => Unas(CalleAncha) };
            var b = new BuscadorDirecciones(geo, espera.Esperar);
            var avisos = new List<(string, Resultado<IReadOnlyList<ResultadoBusqueda>>)>();
            b.Cambiado += (q, r) => avisos.Add((q, r));

            Assert.IsTrue(b.Pedir("calle ancha 10", Cabildo));
            CollectionAssert.AreEqual(new[] { TimeSpan.FromMilliseconds(350) }, espera.Pedidas);
            Assert.AreEqual(1, geo.Peticiones.Count);
            Assert.AreEqual("calle ancha 10", geo.Peticiones[0].Consulta);
            Assert.AreEqual(Cabildo, geo.Peticiones[0].Cerca);
            var aviso = avisos.Single();
            Assert.AreEqual("calle ancha 10", aviso.Item1);
            Assert.IsTrue(aviso.Item2.Ok);
            var d = aviso.Item2.Valor.Single();
            Assert.AreEqual(TipoResultado.Direccion, d.Tipo);
            Assert.AreSame(CalleAncha, d.Direccion);
            Assert.AreEqual(1234.0, d.Metros, 1.0);
            Assert.IsFalse(b.Pendiente);
        }

        [Test]
        public void TecleoRapidoHaceUnaSolaPeticion()
        {
            var espera = new EsperaFalsa { Inmediata = false };
            var geo = new GeoFalso { Responder = _ => Unas(CalleAncha) };
            var b = new BuscadorDirecciones(geo, espera.Esperar);
            var avisos = new List<string>();
            b.Cambiado += (q, _) => avisos.Add(q);

            b.Pedir("cal", Cabildo);
            b.Pedir("call", Cabildo);
            b.Pedir("calle", Cabildo);
            Assert.AreEqual(3, espera.Pendientes.Count);
            Assert.IsTrue(espera.Pendientes[0].Task.IsCanceled && espera.Pendientes[1].Task.IsCanceled, "Esperas anteriores canceladas.");
            Assert.IsEmpty(geo.Peticiones, "Nada antes de los 350 ms.");
            espera.Pendientes[2].SetResult(true);
            Assert.AreEqual("calle", geo.Peticiones.Single().Consulta);
            CollectionAssert.AreEqual(new[] { "calle" }, avisos);
        }

        [Test]
        public void CortoNoPide()
        {
            var espera = new EsperaFalsa();
            var geo = new GeoFalso { Responder = _ => Unas(CalleAncha) };
            var b = new BuscadorDirecciones(geo, espera.Esperar);
            Assert.IsFalse(b.Pedir("ca", Cabildo));
            Assert.IsFalse(b.Pedir(" ¡Ca! ", Cabildo), "Cuentan los caracteres normalizados.");
            Assert.IsEmpty(geo.Peticiones);
            Assert.IsEmpty(espera.Pedidas);
            Assert.IsFalse(b.Pendiente);
        }

        [Test]
        public void PedirYaNoEsperaNiMiraElMinimo()
        {
            var espera = new EsperaFalsa();
            var geo = new GeoFalso { Responder = _ => Unas(CalleAncha) };
            var b = new BuscadorDirecciones(geo, espera.Esperar);
            Assert.IsTrue(b.PedirYa("ca", Cabildo));
            Assert.IsEmpty(espera.Pedidas);
            Assert.AreEqual("ca", geo.Peticiones.Single().Consulta);
            Assert.IsFalse(b.PedirYa(" ¿? ", Cabildo), "Sin letras ni números no hay nada que buscar.");
        }

        [Test]
        public void SinConexionAvisaConUnFallo()
        {
            var geo = new GeoFalso { Responder = _ => SinRed() };
            var b = new BuscadorDirecciones(geo, new EsperaFalsa().Esperar);
            Resultado<IReadOnlyList<ResultadoBusqueda>>? aviso = null;
            b.Cambiado += (_, r) => aviso = r;
            b.Pedir("calle ancha", Cabildo);
            Assert.IsTrue(aviso.HasValue);
            Assert.IsFalse(aviso.Value.Ok);
            Assert.IsEmpty(aviso.Value.Valor);
        }

        [Test]
        public void SiElGeocodificadorLanzaEsUnFallo()
        {
            var geo = new GeoFalso { Responder = _ => throw new InvalidOperationException("roto") };
            var b = new BuscadorDirecciones(geo, new EsperaFalsa().Esperar);
            Resultado<IReadOnlyList<ResultadoBusqueda>>? aviso = null;
            b.Cambiado += (_, r) => aviso = r;
            Assert.DoesNotThrow(() => b.Pedir("calle ancha", Cabildo));
            Assert.IsFalse(aviso.Value.Ok);
            StringAssert.Contains("roto", aviso.Value.Error);
        }

        [Test]
        public void UnaRespuestaTardiaSeDescarta()
        {
            var geo = new GeoFalso { IgnorarCancelacion = true };
            var b = new BuscadorDirecciones(geo, new EsperaFalsa().Esperar);
            var avisos = new List<string>();
            b.Cambiado += (q, _) => avisos.Add(q);
            b.Pedir("call", Cabildo);
            b.Pedir("calle", Cabildo);
            Assert.AreEqual(2, geo.Peticiones.Count);
            Assert.IsTrue(geo.Peticiones[0].Ct.IsCancellationRequested);
            geo.Peticiones[0].Tcs.SetResult(Unas(CalleAncha)); // llega "call" con el campo en "calle"
            Assert.IsEmpty(avisos);
            Assert.IsTrue(b.Pendiente);
            geo.Peticiones[1].Tcs.SetResult(Unas(CalleAncha));
            CollectionAssert.AreEqual(new[] { "calle" }, avisos);
        }

        [Test]
        public void ComoMuchoOchoDirecciones()
        {
            var muchas = Enumerable.Range(0, 12).Select(i => new Direccion("Calle " + i, "", AlNorte(Cabildo, i))).ToArray();
            var geo = new GeoFalso { Responder = _ => Unas(muchas) };
            var b = new BuscadorDirecciones(geo, new EsperaFalsa().Esperar);
            int n = -1;
            b.Cambiado += (_, r) => n = r.Valor.Count;
            b.Pedir("calle", Cabildo);
            Assert.AreEqual(BuscadorDirecciones.MaximoDirecciones, n);
        }

        // ───────────────────────── Hoja y pantalla ─────────────────────────

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
            public readonly List<Coordenada> centrados = new List<Coordenada>();
            public void Centrar(Coordenada destino) => centrados.Add(destino);
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) => centrados.Add(destino);
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada centro = Cabildo;
            public Coordenada Centro => centro;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
            public event System.Action<PuntoPantalla> PulsacionLarga { add { } remove { } }
            public Coordenada? Desproyectar(PuntoPantalla punto) => null;
            public void MostrarRuta(IReadOnlyList<Coordenada> puntos, bool aproximada = false) { }
            public void QuitarRuta() { }
            public void Encuadrar(IReadOnlyList<Coordenada> puntos, RectPantalla libre) { }
        }

        class Repositorio : IRepositorioLugares
        {
            readonly IReadOnlyList<Lugar> lugares;
            public Repositorio(params Lugar[] lugares) => this.lugares = lugares;
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Exito(lugares);
        }

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        /// <summary>Un bar cuya dirección es la Calle Ancha: "calle ancha 10" lo encuentra como resultado local.</summary>
        static Lugar BarPepe() => new Lugar("bar-pepe", "Bar Pepe", "Bar", Plantilla.Hosteleria, AlNorte(Cabildo, 200), "Calle Ancha, 10");

        GameObject objeto;
        PantallaMapa pantalla;
        HojaBuscador hoja;
        MapaFalso mapa;
        GeoFalso geo;
        EsperaFalsa espera;

        /// <summary>La pantalla real con esos lugares (sin puntos de interés), un geocodificador y una espera falsos.</summary>
        void CrearPantalla(params Lugar[] lugares)
        {
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            var arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            var servicioLugares = new ServicioLugares(new Repositorio(lugares));
            Assert.IsTrue(servicioLugares.Cargar().Ok);
            mapa = new MapaFalso();
            geo = new GeoFalso { Responder = _ => Unas(CalleAncha) };
            espera = new EsperaFalsa();
            pantalla.Inicializar(mapa, tema, servicioLugares, Ahora, arbol, filtro,
                busqueda: new ServicioBusqueda(servicioLugares, mapa, Ahora),
                direcciones: new BuscadorDirecciones(geo, espera.Esperar));
            tema.Iniciar();
            filtro.Iniciar();
            hoja = pantalla.Buscador;
            hoja.Medir(800f);
        }

        void LimpiarPantalla()
        {
            if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            objeto = null;
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
        }

        void AbrirYEscribir(string texto)
        {
            hoja.Abrir();
            pantalla.ActualizarCapas(0f);
            hoja.Escribir(texto);
        }

        static bool Oculto(VisualElement e) => e.style.display == DisplayStyle.None;

        IEnumerable<FilaResultado> FilasDireccion => hoja.Filas.Where(f => f.Resultado.Tipo == TipoResultado.Direccion);

        [Test]
        public void LaDireccionSaleDetrasDeLosLocalesConPinYDetalle()
        {
            CrearPantalla(BarPepe());
            AbrirYEscribir("calle ancha 10");
            Assert.AreEqual(1, geo.Peticiones.Count, "Una petición tras la espera.");
            CollectionAssert.AreEqual(new[] { BuscadorDirecciones.Espera }, espera.Pedidas);
            Assert.AreEqual(2, hoja.Filas.Count);
            Assert.AreEqual(TipoResultado.Lugar, hoja.Filas[0].Resultado.Tipo, "Primero los locales.");
            var fila = hoja.Filas[1];
            Assert.AreEqual(TipoResultado.Direccion, fila.Resultado.Tipo);
            Assert.AreEqual(1, hoja.DireccionesVisibles);
            Assert.AreEqual("Calle Ancha 10", fila.Nombre.text);
            Assert.AreEqual("Dirección · Sanlúcar de Barrameda · a 1,2" + Nbsp + "km", fila.Detalle.text);
            Assert.AreEqual(IconosLugar.Generico, FilaResultado.GlifoDe(fila.Resultado), "Icono pin.");
            Assert.IsTrue(fila.Icono.ClassListContains("buscador__resultado-icono--direccion"));
            Assert.IsNull(fila.Precio);
            Assert.IsFalse(hoja.SinResultadosVisible);
            Assert.IsFalse(hoja.BuscarDireccionVisible);
            Assert.IsFalse(hoja.AvisoDireccionesVisible);
        }

        [Test]
        public void SinLocalidadElDetalleEsDireccionYDistancia()
        {
            CrearPantalla();
            geo.Responder = _ => Unas(new Direccion("Calle Real", "", AlNorte(Cabildo, 300)));
            AbrirYEscribir("calle real");
            Assert.AreEqual("Dirección · a 300" + Nbsp + "m", FilasDireccion.Single().Detalle.text);
        }

        [Test]
        public void EnLaEsperaSeOfreceBuscarComoDireccion()
        {
            CrearPantalla();
            espera.Inmediata = false;
            AbrirYEscribir("calle ancha");
            Assert.IsEmpty(geo.Peticiones);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.IsTrue(hoja.BuscarDireccionVisible);
            Assert.IsFalse(Oculto(hoja.BuscarDireccion));
            Assert.IsTrue(hoja.FilaSinResultados.Contains(hoja.SinResultados) && hoja.FilaSinResultados.Contains(hoja.BuscarDireccion),
                "El botón va junto a \"No hay resultados\".");
            espera.Pendientes.Last().SetResult(true);
            Assert.AreEqual(1, FilasDireccion.Count());
            Assert.IsFalse(hoja.BuscarDireccionVisible, "Con direcciones ya no sale.");
            Assert.IsFalse(hoja.SinResultadosVisible);
        }

        [Test]
        public void TecleoRapidoEnLaHojaPideUnaVez()
        {
            CrearPantalla();
            espera.Inmediata = false;
            AbrirYEscribir("cal");
            hoja.Escribir("call");
            hoja.Escribir("calle");
            Assert.IsTrue(espera.Pendientes.Take(2).All(t => t.Task.IsCanceled));
            espera.Pendientes.Last().SetResult(true);
            Assert.AreEqual("calle", geo.Peticiones.Single().Consulta);
            Assert.AreEqual(1, FilasDireccion.Count());
        }

        [Test]
        public void CortoNoPideYSaleElBotonSinLocales()
        {
            CrearPantalla();
            AbrirYEscribir("ca");
            Assert.IsEmpty(geo.Peticiones);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.IsTrue(hoja.BuscarDireccionVisible);
        }

        [Test]
        public void ConLocalesNoSaleElBoton()
        {
            CrearPantalla(BarPepe());
            AbrirYEscribir("ca"); // "Calle Ancha, 10" es la dirección del bar
            Assert.IsNotEmpty(hoja.Filas);
            Assert.IsFalse(hoja.BuscarDireccionVisible);
            Assert.IsTrue(Oculto(hoja.FilaSinResultados));
        }

        [Test]
        public void ElBotonPideYaConElTextoCorto()
        {
            CrearPantalla();
            AbrirYEscribir("ca");
            Assert.IsEmpty(geo.Peticiones);
            int esperasAntes = espera.Pedidas.Count;
            hoja.PulsarBuscarDireccion(); // lo que hace el toque en el botón
            Assert.AreEqual("ca", geo.Peticiones.Single().Consulta);
            Assert.AreEqual(esperasAntes, espera.Pedidas.Count, "Sin espera.");
            Assert.AreEqual(1, FilasDireccion.Count());
            Assert.IsFalse(hoja.BuscarDireccionVisible);
        }

        [Test]
        public void SinConexionSiguenLosLocalesConAviso()
        {
            CrearPantalla(BarPepe());
            geo.Responder = _ => SinRed();
            AbrirYEscribir("calle ancha");
            Assert.AreEqual(1, hoja.Filas.Count, "El local sigue.");
            Assert.AreEqual(TipoResultado.Lugar, hoja.Filas[0].Resultado.Tipo);
            Assert.IsTrue(hoja.AvisoDireccionesVisible);
            Assert.IsFalse(Oculto(hoja.AvisoDirecciones));
            Assert.AreEqual("Las direcciones necesitan conexión", hoja.AvisoDirecciones.text);
            Assert.IsFalse(hoja.BuscarDireccionVisible, "Con locales no hay botón.");
        }

        [Test]
        public void SinConexionNiLocalesElBotonReintenta()
        {
            CrearPantalla();
            geo.Responder = _ => SinRed();
            AbrirYEscribir("calle ancha");
            Assert.IsTrue(hoja.AvisoDireccionesVisible);
            Assert.IsTrue(hoja.SinResultadosVisible);
            Assert.IsTrue(hoja.BuscarDireccionVisible);
            geo.Responder = _ => Unas(CalleAncha); // vuelve la red
            hoja.PulsarBuscarDireccion();
            Assert.AreEqual(2, geo.Peticiones.Count);
            Assert.IsFalse(hoja.AvisoDireccionesVisible);
            Assert.AreEqual(1, FilasDireccion.Count());
        }

        [Test]
        public void UnaRespuestaTardiaNoSePintaEnLaHoja()
        {
            CrearPantalla();
            geo.Responder = null;
            geo.IgnorarCancelacion = true;
            AbrirYEscribir("call");
            hoja.Escribir("calle");
            geo.Peticiones[0].Tcs.SetResult(Unas(new Direccion("Callejón Viejo", "", Cabildo)));
            Assert.IsEmpty(FilasDireccion, "La de \"call\" se descarta.");
            geo.Peticiones[1].Tcs.SetResult(Unas(CalleAncha));
            Assert.AreEqual("Calle Ancha 10", FilasDireccion.Single().Nombre.text);
        }

        [Test]
        public void BorrarCancelaLaPeticionYVuelvenLasCategorias()
        {
            CrearPantalla();
            geo.Responder = null; // la petición se queda en curso
            AbrirYEscribir("calle ancha");
            Assert.IsTrue(pantalla.Direcciones.Pendiente);
            pantalla.BorrarBusqueda();
            Assert.IsTrue(geo.Peticiones.Single().Ct.IsCancellationRequested);
            Assert.IsFalse(pantalla.Direcciones.Pendiente);
            Assert.IsFalse(hoja.MostrandoResultados);
            Assert.IsFalse(Oculto(hoja.FilaChips));
            Assert.IsFalse(hoja.BuscarDireccionVisible);
            Assert.IsFalse(hoja.AvisoDireccionesVisible);
        }

        [Test]
        public void TocarUnaDireccionCentraElMapaYPliegaLaHoja()
        {
            CrearPantalla();
            AbrirYEscribir("calle ancha 10");
            var fila = FilasDireccion.Single();
            pantalla.ElegirResultado(fila.Resultado);
            Assert.AreEqual(CalleAncha.Posicion, mapa.centrados.Last());
            Assert.IsFalse(hoja.Abierta);
            Assert.IsNull(pantalla.Ficha.Lugar, "Las direcciones no tienen ficha (2.5).");
        }

        [Test]
        public void AlVolverADesplegarNoSePideOtraVez()
        {
            CrearPantalla();
            AbrirYEscribir("calle ancha 10");
            Assert.AreEqual(1, geo.Peticiones.Count);
            pantalla.PlegarBuscador();
            hoja.Abrir();
            pantalla.Buscar(hoja.Campo.value); // lo que hace la pantalla al desplegar la hoja
            Assert.AreEqual(1, geo.Peticiones.Count);
            Assert.AreEqual(1, FilasDireccion.Count());
        }

        [Test]
        public void AlVolverADesplegarLasDistanciasSeMidenDesdeElPuntoDeAhora()
        {
            CrearPantalla();
            AbrirYEscribir("calle ancha 10");
            Assert.AreEqual(1234.0, FilasDireccion.Single().Resultado.Metros, 1.0);
            pantalla.PlegarBuscador();
            mapa.centro = CalleAncha.Posicion; // el mapa se ha movido hasta la calle
            hoja.Abrir();
            pantalla.Buscar(hoja.Campo.value);
            Assert.AreEqual(1, geo.Peticiones.Count, "No se pide otra vez.");
            Assert.AreEqual(0.0, FilasDireccion.Single().Resultado.Metros, 1.0, "La distancia, desde el nuevo centro.");
        }

        [Test]
        public void AlVolverADesplegarTrasUnFalloSePideOtraVez()
        {
            CrearPantalla();
            geo.Responder = _ => SinRed();
            AbrirYEscribir("calle ancha");
            Assert.IsTrue(hoja.AvisoDireccionesVisible);
            pantalla.PlegarBuscador();
            geo.Responder = _ => Unas(CalleAncha); // vuelve la red
            hoja.Abrir();
            pantalla.Buscar(hoja.Campo.value);
            Assert.AreEqual(2, geo.Peticiones.Count);
            Assert.IsFalse(hoja.AvisoDireccionesVisible);
            Assert.AreEqual(1, FilasDireccion.Count());
        }

        [Test]
        public void ElArranqueLeDaLasDireccionesALaPantalla()
        {
            var otro = new GameObject("Otra pantalla");
            try
            {
                var p = otro.AddComponent<PantallaMapa>();
                var prefs = new PreferenciasFalsas();
                var d = new BuscadorDirecciones(new GeoFalso(), new EsperaFalsa().Esperar);
                Assert.IsTrue(Arranque.Arranque.Componer(new MapaFalso(), p, new ServicioTema(prefs, new SistemaClaro()),
                    new ServicioLugares(new Repositorio(BarPepe())), new ServicioFiltro(prefs), Ahora, direcciones: d));
                Assert.AreSame(d, p.Direcciones);
            }
            finally { UnityEngine.Object.DestroyImmediate(otro); }
        }

        // ───────────────────────── TalkBack (parte 3) ─────────────────────────

        List<string> anuncios;
        Action<string> oir;

        void ConLector()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            anuncios = new List<string>();
            oir = anuncios.Add;
            AccesibilidadPantalla.AnuncioPedido += oir;
        }

        void SinOir()
        {
            if (oir != null) AccesibilidadPantalla.AnuncioPedido -= oir;
            oir = null;
        }

        [Test]
        public void LaDireccionEsUnBotonConSuAnuncio()
        {
            CrearPantalla(BarPepe());
            ConLector();
            AbrirYEscribir("calle ancha 10");
            var fila = FilasDireccion.Single();
            Assert.AreEqual("Calle Ancha 10, Dirección, Sanlúcar de Barrameda, a 1,2" + Nbsp + "km", fila.Anuncio);
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.AreEqual(recorrido.IndexOf(hoja.Filas[0].Anuncio) + 1, recorrido.IndexOf(fila.Anuncio), "Detrás del local.");
        }

        [Test]
        public void SinLocalidadElAnuncioEsNombreDireccionYDistancia()
        {
            CrearPantalla();
            geo.Responder = _ => Unas(new Direccion("Calle Real", "", AlNorte(Cabildo, 350)));
            AbrirYEscribir("calle real");
            Assert.AreEqual("Calle Real, Dirección, a 350" + Nbsp + "m", FilasDireccion.Single().Anuncio);
        }

        [Test]
        public void AlLlegarLasDireccionesSeAnuncianSinMoverElFoco()
        {
            CrearPantalla(BarPepe());
            ConLector();
            espera.Inmediata = false;
            AbrirYEscribir("calle ancha 10");
            Assert.AreEqual("1 resultado", anuncios.LastOrDefault());
            var local = hoja.Filas[0];
            pantalla.Accesibilidad.PedirFoco(local); // el usuario recorre la lista
            anuncios.Clear();
            espera.Pendientes.Last().SetResult(true); // llegan las direcciones
            CollectionAssert.AreEqual(new[] { "2 resultados" }, anuncios, "Cuenta locales y direcciones.");
            Assert.AreSame(local, hoja.Filas[0], "La fila local se conserva: no se rehace.");
            Assert.AreSame(local, pantalla.Accesibilidad.Enfocado, "El foco no se mueve.");
            Assert.AreSame(pantalla.Accesibilidad.NodoDe(local), pantalla.Accesibilidad.UltimoNodoEnfocado,
                "TalkBack recibe otra vez el nodo de la fila en la jerarquía nueva.");
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), FilasDireccion.Single().Anuncio);
        }

        [Test]
        public void SiElNodoConElFocoDesapareceElFocoVaAlCampo()
        {
            CrearPantalla(); // sin locales: "No hay resultados" mientras se espera
            ConLector();
            espera.Inmediata = false;
            AbrirYEscribir("calle ancha 10");
            pantalla.Accesibilidad.PedirFoco(hoja.SinResultados); // el usuario está en "No hay resultados"
            espera.Pendientes.Last().SetResult(true); // llegan las direcciones: el texto se va
            Assert.IsFalse(hoja.SinResultadosVisible);
            Assert.AreSame(hoja.Campo, pantalla.Accesibilidad.Enfocado, "No se queda en nada.");
            Assert.AreEqual("1 resultado", anuncios.Last());
        }

        [Test]
        public void SinConexionElAvisoNoSeRepiteAlSeguirEscribiendo()
        {
            CrearPantalla(BarPepe());
            ConLector();
            geo.Responder = _ => SinRed();
            espera.Inmediata = false;
            AbrirYEscribir("calle ancha");
            espera.Pendientes.Last().SetResult(true);
            Assert.AreEqual(1, anuncios.Count(a => a.EndsWith("Las direcciones necesitan conexión")));
            hoja.Escribir("calle ancha 1"); // en la espera, el aviso sigue (no parpadea)
            Assert.IsTrue(hoja.AvisoDireccionesVisible);
            espera.Pendientes.Last().SetResult(true); // vuelve a fallar
            Assert.AreEqual(1, anuncios.Count(a => a.EndsWith("Las direcciones necesitan conexión")), "Una sola vez.");
            geo.Responder = _ => Unas(CalleAncha); // vuelve la red
            hoja.Escribir("calle ancha 10");
            espera.Pendientes.Last().SetResult(true);
            Assert.IsFalse(hoja.AvisoDireccionesVisible, "Una respuesta buena lo quita.");
        }

        [Test]
        public void SiLasDireccionesNoCambianLaListaNoSeRepiteElAnuncio()
        {
            CrearPantalla(BarPepe());
            ConLector();
            geo.Responder = _ => Unas();
            espera.Inmediata = false;
            AbrirYEscribir("calle ancha 10");
            anuncios.Clear();
            espera.Pendientes.Last().SetResult(true); // respuesta sin direcciones
            Assert.IsEmpty(anuncios);
        }

        [Test]
        public void ElAvisoDeConexionSeAnunciaUnaVezYEstaEnElRecorrido()
        {
            CrearPantalla(BarPepe());
            ConLector();
            geo.Responder = _ => SinRed();
            AbrirYEscribir("calle ancha");
            StringAssert.EndsWith("Las direcciones necesitan conexión", anuncios.Last(), "Se anuncia al aparecer.");
            var recorrido = pantalla.Accesibilidad.Recorrido();
            Assert.AreEqual(recorrido.Count - 1, recorrido.IndexOf("Las direcciones necesitan conexión"), "Debajo de la lista.");
            anuncios.Clear();
            hoja.Escribir("calle anch"); // sigue sin red y el aviso sigue visible
            CollectionAssert.DoesNotContain(anuncios, "Las direcciones necesitan conexión", "No se repite.");
        }

        [Test]
        public void BuscarComoDireccionEsUnBotonYTrasResponderElFocoVaAlCampo()
        {
            CrearPantalla();
            ConLector();
            geo.Responder = _ => SinRed();
            AbrirYEscribir("calle ancha");
            CollectionAssert.Contains(pantalla.Accesibilidad.Recorrido(), "Buscar como dirección");
            pantalla.Accesibilidad.PedirFoco(hoja.BuscarDireccion);
            anuncios.Clear();
            geo.Responder = _ => SinRed(); // el reintento vuelve a fallar
            pantalla.BuscarComoDireccion(); // lo que hace el doble toque del botón
            Assert.AreEqual(2, geo.Peticiones.Count);
            CollectionAssert.Contains(anuncios, "Las direcciones necesitan conexión", "Un reintento que falla se anuncia.");
            Assert.AreSame(hoja.BuscarDireccion, pantalla.Accesibilidad.Enfocado, "El botón sigue: el foco se queda.");
            geo.Responder = _ => Unas(CalleAncha); // vuelve la red
            pantalla.BuscarComoDireccion();
            Assert.IsFalse(hoja.BuscarDireccionVisible);
            Assert.AreSame(hoja.Campo, pantalla.Accesibilidad.Enfocado, "El botón desaparece: el foco va al campo.");
            CollectionAssert.DoesNotContain(pantalla.Accesibilidad.Recorrido(), "Buscar como dirección");
        }

        [Test]
        public void ElBotonBuscarComoDireccionMideAlMenos48()
        {
            // La medida mínima viene del USS (sin panel no hay disposición que medir)
            var uss = System.IO.File.ReadAllText("Assets/UI/PantallaMapa.uss");
            StringAssert.Contains(".buscador__buscar-direccion {", uss);
            int i = uss.IndexOf(".buscador__buscar-direccion {", StringComparison.Ordinal);
            StringAssert.Contains("min-height: 48px", uss.Substring(i, uss.IndexOf('}', i) - i));
        }
    }
}
