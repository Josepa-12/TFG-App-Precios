using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PriceMap.Vectorial;
using UnityEngine;

namespace PriceMap.Tests.EditMode
{
    /// <summary>Gesto de dos dedos: inclinar frente a ampliar y girar (historia 1.2, parte 2).</summary>
    public class PruebasGestoDosDedos
    {
        [Test]
        public void DosDedosSubiendoJuntosInclinan()
        {
            var g = GestoDosDedos.Clasificar(new Vector2(0.5f, 8f), new Vector2(-0.3f, 7f), 300f, 300.4f, 10f, 10.2f, out float dy);
            Assert.AreEqual(TipoGesto.Inclinar, g);
            Assert.Greater(dy, 0f, "Subir los dedos da dy positivo (el mapa se inclina más).");
        }

        [Test]
        public void DosDedosBajandoJuntosInclinanAlReves()
        {
            var g = GestoDosDedos.Clasificar(new Vector2(0f, -6f), new Vector2(0f, -6f), 300f, 300f, 0f, 0f, out float dy);
            Assert.AreEqual(TipoGesto.Inclinar, g);
            Assert.Less(dy, 0f);
        }

        [Test]
        public void SepararLosDedosEsZoomNoInclinacion()
        {
            var g = GestoDosDedos.Clasificar(new Vector2(-6f, 0f), new Vector2(6f, 0f), 300f, 312f, 0f, 0f, out _);
            Assert.AreEqual(TipoGesto.ZoomGiro, g);
        }

        [Test]
        public void GirarLosDedosEsGiroNoInclinacion()
        {
            var g = GestoDosDedos.Clasificar(new Vector2(0f, 5f), new Vector2(0f, -5f), 300f, 300f, 0f, 3f, out _);
            Assert.AreEqual(TipoGesto.ZoomGiro, g);
        }

        [Test]
        public void DedosEnVerticalPeroEnSentidosOpuestosNoInclinan()
        {
            // Pellizco vertical: uno sube y otro baja, la separación cambia
            var g = GestoDosDedos.Clasificar(new Vector2(0f, 6f), new Vector2(0f, -6f), 300f, 312f, 90f, 90f, out _);
            Assert.AreNotEqual(TipoGesto.Inclinar, g);
        }

        [Test]
        public void MovimientoVerticalPequenoNoInclina()
        {
            var g = GestoDosDedos.Clasificar(new Vector2(0f, 1f), new Vector2(0f, 1f), 300f, 300f, 0f, 0f, out _);
            Assert.AreEqual(TipoGesto.Nada, g);
        }

        [Test]
        public void ArrastreDiagonalNoInclina()
        {
            var g = GestoDosDedos.Clasificar(new Vector2(6f, 6f), new Vector2(6f, 6f), 300f, 300f, 0f, 0f, out _);
            Assert.AreNotEqual(TipoGesto.Inclinar, g);
        }

    }

    /// <summary>Teselas que cargar y liberar alrededor de la vista (historia 1.2, parte 2).</summary>
    public class PruebasPlanTeselas
    {

        static readonly Vector2Int Centro = new Vector2Int(7902, 6389);

        [Test]
        public void AlEmpezarCarga3x3DeDentroAFuera()
        {
            var plan = PlanTeselas.Calcular(Centro, new List<Vector2Int>(), new List<Vector2Int>(), 1, 3);
            Assert.AreEqual(9, plan.aCargar.Count);
            Assert.AreEqual(Centro, plan.aCargar[0], "La primera es la del centro.");
            Assert.IsTrue(plan.aCargar.Skip(1).Take(4).All(t => (t - Centro).sqrMagnitude == 1), "Después las cuatro de los lados.");
            Assert.IsEmpty(plan.aLiberar);
        }

        [Test]
        public void AlMoverseUnaTeselaSoloPideLasNuevas()
        {
            var cargadas = PlanTeselas.Calcular(Centro, new List<Vector2Int>(), new List<Vector2Int>(), 1, 3).aCargar;
            var plan = PlanTeselas.Calcular(Centro + Vector2Int.right, cargadas, new List<Vector2Int>(), 1, 3);
            CollectionAssert.AreEquivalent(new[]
            {
                new Vector2Int(Centro.x + 2, Centro.y - 1), new Vector2Int(Centro.x + 2, Centro.y), new Vector2Int(Centro.x + 2, Centro.y + 1),
            }, plan.aCargar);
            Assert.IsEmpty(plan.aLiberar, "A distancia 2 todavía no se libera nada.");
        }

        [Test]
        public void LiberaLasQuePasanDeLaDistanciaDeLiberar()
        {
            var cargadas = new List<Vector2Int> { Centro, Centro + new Vector2Int(3, 0), Centro + new Vector2Int(4, 0), Centro + new Vector2Int(-2, 5) };
            var plan = PlanTeselas.Calcular(Centro, cargadas, new List<Vector2Int>(), 1, 3);
            CollectionAssert.AreEquivalent(new[] { Centro + new Vector2Int(4, 0), Centro + new Vector2Int(-2, 5) }, plan.aLiberar);
        }

        [Test]
        public void LoQueEstaPendienteNoSePideOtraVez()
        {
            var pendientes = new List<Vector2Int> { Centro, Centro + Vector2Int.up };
            var plan = PlanTeselas.Calcular(Centro, new List<Vector2Int>(), pendientes, 1, 3);
            Assert.AreEqual(7, plan.aCargar.Count);
            CollectionAssert.DoesNotContain(plan.aCargar, Centro);
        }

        [Test]
        public void ConLaCamaraLejosCarga5x5()
        {
            var plan = PlanTeselas.Calcular(Centro, new List<Vector2Int>(), new List<Vector2Int>(), 2, 3);
            Assert.AreEqual(25, plan.aCargar.Count);
        }

        [Test]
        public void AlRecorrerKilometrosNuncaHayMasDe7x7()
        {
            var cargadas = new HashSet<Vector2Int>();
            for (int paso = 0; paso < 40; paso++)
            {
                var centro = Centro + new Vector2Int(paso, paso / 2);
                var plan = PlanTeselas.Calcular(centro, cargadas, new List<Vector2Int>(), 2, 3);
                foreach (var t in plan.aLiberar) cargadas.Remove(t);
                foreach (var t in plan.aCargar) cargadas.Add(t);
                Assert.LessOrEqual(cargadas.Count, 49, $"Paso {paso}: {cargadas.Count} teselas cargadas.");
            }
        }
    }
}
