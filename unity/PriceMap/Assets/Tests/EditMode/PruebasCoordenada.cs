using System.Globalization;
using System.Threading;
using NUnit.Framework;
using PriceMap.Dominio;

namespace PriceMap.Tests.EditMode
{
    public class PruebasCoordenada
    {
        [Test]
        public void DosCoordenadasConLosMismosValoresSonIguales()
        {
            var a = new Coordenada(36.7779, -6.3528);
            var b = new Coordenada(36.7779, -6.3528);
            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsTrue(a != new Coordenada(36.7779, -6.3529));
        }

        [Test]
        public void ElTextoUsaPuntoDecimalAunqueElMovilEsteEnCastellano()
        {
            var anterior = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("es-ES");
                Assert.AreEqual("36.777900, -6.352800", new Coordenada(36.7779, -6.3528).ToString());
            }
            finally { Thread.CurrentThread.CurrentCulture = anterior; }
        }
    }
}
