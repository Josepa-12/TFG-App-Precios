using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PriceMap.Tests.EditMode
{
    /// <summary>La raíz de composición entrega el mapa a la UI, o avisa sin romper si falta una pieza.</summary>
    public class PruebasArranque
    {
        class MapaFalso : IMapa
        {
            public readonly List<Coordenada> centrados = new List<Coordenada>();
            public readonly List<Tema> temas = new List<Tema>();
            public void Centrar(Coordenada destino) => centrados.Add(destino);
            public void AplicarTema(Tema tema) => temas.Add(tema);
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaOscuro : ITemaSistema { public bool EsOscuro => true; }

        static ServicioTema NuevoTema() => new ServicioTema(new PreferenciasFalsas(), new SistemaOscuro());

        class SinLugares : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() => Resultado<IReadOnlyList<Lugar>>.Fallo("sin datos", new Lugar[0]);
        }

        static ServicioLugares Lugares() { var l = new ServicioLugares(new SinLugares()); l.Cargar(); return l; }
        static readonly System.DateTime Ahora = new System.DateTime(2026, 11, 17, 12, 0, 0, System.DateTimeKind.Utc);

        GameObject objeto;
        PantallaMapa pantalla;

        [SetUp]
        public void Preparar()
        {
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(objeto);

        [Test]
        public void ConTodasLasPiezasLaPantallaRecibeElMapaYCentraEnLaPlazaDelCabildo()
        {
            var mapa = new MapaFalso();
            Assert.IsTrue(Arranque.Arranque.Componer(mapa, pantalla, NuevoTema(), Lugares(), Ahora));
            CollectionAssert.AreEqual(new[] { new Coordenada(36.7779, -6.3528) }, mapa.centrados);
        }

        [Test]
        public void ElMapaRecibeElTemaInicialYCadaCambio()
        {
            var mapa = new MapaFalso();
            var tema = NuevoTema();
            Arranque.Arranque.Componer(mapa, pantalla, tema, Lugares(), Ahora);
            tema.Alternar();
            CollectionAssert.AreEqual(new[] { Tema.Oscuro, Tema.Claro }, mapa.temas);
        }

        [Test]
        public void SinMapaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el mapa"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(null, pantalla, NuevoTema(), Lugares(), Ahora));
            Assert.IsFalse(ok);
        }

        [Test]
        public void SinServicioDeTemaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el ServicioTema"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), pantalla, null, Lugares(), Ahora));
            Assert.IsFalse(ok);
        }

        [Test]
        public void SinPantallaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta la PantallaMapa"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), null, NuevoTema(), Lugares(), Ahora));
            Assert.IsFalse(ok);
        }

        [Test]
        public void SinServicioDeLugaresAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el ServicioLugares"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), pantalla, NuevoTema(), null, Ahora));
            Assert.IsFalse(ok);
        }
    }
}
