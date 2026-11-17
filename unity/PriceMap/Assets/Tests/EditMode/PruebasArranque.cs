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
            public void Centrar(Coordenada destino) => centrados.Add(destino);
        }

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
            Assert.IsTrue(Arranque.Arranque.Componer(mapa, pantalla));
            CollectionAssert.AreEqual(new[] { new Coordenada(36.7779, -6.3528) }, mapa.centrados);
        }

        [Test]
        public void SinMapaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta el mapa"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(null, pantalla));
            Assert.IsFalse(ok);
        }

        [Test]
        public void SinPantallaAvisaYNoLanzaExcepcion()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: falta la PantallaMapa"));
            bool ok = true;
            Assert.DoesNotThrow(() => ok = Arranque.Arranque.Componer(new MapaFalso(), null));
            Assert.IsFalse(ok);
        }
    }
}
