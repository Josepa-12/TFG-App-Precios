using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.TestTools;

namespace PriceMap.Tests.EditMode
{
    /// <summary>Lugares base de Sanlúcar y resumen de su precio (historia 1.4, parte 1).</summary>
    public class PruebasLugares
    {
        const string UrlAreas = "https://clinicaareas.com/tarifas";
        static readonly DateTime Consulta = new DateTime(2026, 11, 13);
        static readonly DateTime Ahora = new DateTime(2026, 11, 17, 12, 0, 0);

        // Caja de Sanlúcar de Barrameda (la misma que valida exportar-datos-unity.js)
        const double LatMin = 36.70, LatMax = 36.85, LonMin = -6.45, LonMax = -6.25;

        static IReadOnlyList<Lugar> cargados;

        /// <summary>El JSON real empaquetado en Assets/Datos/Resources (se lee una vez).</summary>
        static IReadOnlyList<Lugar> Reales()
        {
            if (cargados != null) return cargados;
            var r = new RepositorioLugaresJson().Cargar();
            Assert.IsTrue(r.Ok, "El JSON empaquetado no se puede leer: " + r.Error);
            return cargados = r.Valor;
        }

        static Lugar Real(string id) => Reales().Single(l => l.Id == id);

        static Concepto ConceptoWeb(string id, ValorPrecio? valor) =>
            new Concepto(id, "Área", id, "sesión", new FuentesPrecio(Plantilla.ServicioLocal, new PrecioWeb(valor, UrlAreas, Consulta)));

        static Lugar LugarCon(params Concepto[] conceptos) =>
            new Lugar("prueba", "Prueba", "Clínica", Plantilla.ServicioLocal, new Coordenada(36.78, -6.35), conceptos: conceptos);

        // ── Migración ──

        [Test]
        public void MigracionDa124LugaresConIdUnico()
        {
            var lugares = Reales();
            Assert.AreEqual(124, lugares.Count);
            Assert.AreEqual(124, lugares.Select(l => l.Id).Distinct().Count(), "Hay ids repetidos.");
        }

        [Test]
        public void MigracionConservaNombreTipoPlantillaCoordenadaYDireccion()
        {
            var cachon = Real("bar-cachon");
            Assert.AreEqual("Bar Cachón", cachon.Nombre);
            Assert.AreEqual("Bar", cachon.Tipo);
            Assert.AreEqual(Plantilla.Hosteleria, cachon.Plantilla);
            // El prototipo da [lon, lat] = [-6.346225, 36.784314]
            Assert.AreEqual(36.784314, cachon.Posicion.Latitud, 1e-9);
            Assert.AreEqual(-6.346225, cachon.Posicion.Longitud, 1e-9);
            Assert.AreEqual("", cachon.Direccion);

            var areas = Real("clinica-areas");
            Assert.AreEqual("Clínica Areas", areas.Nombre);
            Assert.AreEqual("Clínica privada", areas.Tipo);
            Assert.AreEqual(Plantilla.ServicioLocal, areas.Plantilla);
            Assert.AreEqual(new Coordenada(36.780143, -6.35088), areas.Posicion);
            Assert.AreEqual("C/ Santo Domingo, 21-23", areas.Direccion);
            Assert.IsFalse(areas.Publico);
        }

        [Test]
        public void IntegridadDelJsonReal()
        {
            foreach (var l in Reales())
            {
                Assert.That(l.Posicion.Latitud, Is.InRange(LatMin, LatMax), $"{l.Id}: latitud fuera de Sanlúcar");
                Assert.That(l.Posicion.Longitud, Is.InRange(LonMin, LonMax), $"{l.Id}: longitud fuera de Sanlúcar");
                Assert.IsNotEmpty(l.Nombre, $"{l.Id}: sin nombre");
                if (l.Id != "clinica-areas") Assert.IsEmpty(l.Conceptos, $"{l.Id}: no debería tener conceptos (no se inventan precios)");
                foreach (var c in l.Conceptos)
                {
                    Assert.IsNotNull(c.Fuentes.Web, $"{l.Id}/{c.Id}: precio sin origen web");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(c.Fuentes.Web.Url), $"{l.Id}/{c.Id}: sin página");
                    Assert.AreNotEqual(default(DateTime), c.Fuentes.Web.FechaConsulta, $"{l.Id}/{c.Id}: sin fecha");
                    StringAssert.IsMatch("^[a-z0-9-]+/[a-z0-9-]+$", c.Id, "Los conceptos son area/concepto.");
                }
            }
        }

        // ── Tarifas ──

        [Test]
        public void TarifasDeClinicaAreasCoincidenEnCentimosConElPrototipo()
        {
            var areas = Real("clinica-areas");
            var esperados = new Dictionary<string, ValorPrecio?>
            {
                ["traumatologia/consulta-de-traumatologia"] = ValorPrecio.Fijo(9000),
                ["traumatologia/tratamiento-ecoguiado"] = ValorPrecio.Fijo(5000),
                ["fisioterapia/sesion-de-fisioterapia"] = ValorPrecio.Desde(4000),
                ["fisioterapia/tratamiento-vossman"] = ValorPrecio.Fijo(4500),
                ["fisioterapia/ondas-de-choque"] = ValorPrecio.Fijo(5000),
                ["suelo-pelvico/valoracion-de-suelo-pelvico"] = ValorPrecio.Fijo(4500),
                ["suelo-pelvico/sesion-de-suelo-pelvico-incluye-phenix"] = ValorPrecio.Fijo(3500),
                ["readaptacion-y-entrenamiento/sesion-alterg"] = ValorPrecio.Fijo(2000),
            };

            Assert.AreEqual(8, areas.Conceptos.Count);
            Assert.AreEqual(4, areas.Conceptos.Select(c => c.Area).Distinct().Count());
            CollectionAssert.AreEquivalent(esperados.Keys, areas.Conceptos.Select(c => c.Id));
            foreach (var c in areas.Conceptos)
            {
                Assert.AreEqual(esperados[c.Id], c.Fuentes.Web.Valor, c.Id);
                Assert.AreEqual(UrlAreas, c.Fuentes.Web.Url, c.Id);
                Assert.AreEqual(Consulta, c.Fuentes.Web.FechaConsulta, c.Id);
                Assert.AreEqual(Plantilla.ServicioLocal, c.Fuentes.Plantilla, c.Id);
            }
            Assert.AreEqual(0, areas.Conceptos.Count(c => c.Fuentes.Web.EsConsultar), "Los conceptos sin importe no se exportan (18/11/2026).");
        }

        /// <summary>Fila "Datos de Clínica Areas": sin Infiltraciones, Factores de crecimiento ni Recuperación con frío.</summary>
        [Test]
        public void ClinicaAreasNoTraeConceptosSinImporte()
        {
            var nombres = Real("clinica-areas").Conceptos.Select(c => c.Nombre).ToList();
            CollectionAssert.DoesNotContain(nombres, "Infiltraciones");
            CollectionAssert.DoesNotContain(nombres, "Factores de crecimiento");
            Assert.IsFalse(nombres.Any(n => n.StartsWith("Recuperación con frío")));
        }

        [Test]
        public void LosConceptosConBonoNoSeExportan()
        {
            var nombres = Real("clinica-areas").Conceptos.Select(c => c.Nombre).ToList();
            CollectionAssert.DoesNotContain(nombres, "Superinductiva");
            CollectionAssert.DoesNotContain(nombres, "Entrenamiento personal");
            CollectionAssert.DoesNotContain(nombres, "Entrenamiento grupal");
        }

        // ── Formas ──

        [Test]
        public void FormasDesdeFijoYConsultarSeLeenDelJson()
        {
            const string json = @"{""lugares"":[{""id"":""x"",""nombre"":""X"",""tipo"":""Clínica"",""plantilla"":""servicio"",""lat"":36.78,""lon"":-6.35,""conceptos"":[
                {""id"":""a/desde"",""forma"":""desde"",""cent"":4000,""url"":""https://x.es"",""fecha"":""2026-11-13""},
                {""id"":""a/fijo"",""forma"":""fijo"",""cent"":9000,""url"":""https://x.es"",""fecha"":""2026-11-13""},
                {""id"":""a/consultar"",""forma"":""consultar"",""cent"":0,""url"":""https://x.es"",""fecha"":""2026-11-13""}]}]}";
            var r = new RepositorioLugaresJson(json).Cargar();

            Assert.IsTrue(r.Ok, r.Error);
            var c = r.Valor[0].Conceptos;
            Assert.AreEqual(ValorPrecio.Desde(4000), c[0].Fuentes.Web.Valor);
            Assert.AreEqual(ValorPrecio.Fijo(9000), c[1].Fuentes.Web.Valor);
            Assert.IsTrue(c[2].Fuentes.Web.EsConsultar);
            Assert.IsTrue(CalculadoraPrecio.Calcular(c[2].Fuentes, Ahora).EsConsultar);
        }

        // ── Resumen ──

        [Test]
        public void ResumenDeClinicaAreasEsDesde20Euros()
        {
            var servicio = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(servicio.Cargar().Ok);

            var r = servicio.Resumen("clinica-areas", Ahora);
            Assert.IsTrue(r.Ok, r.Error);
            Assert.IsTrue(r.Valor.TienePrecio);
            Assert.AreEqual(ValorPrecio.Desde(2000), r.Valor.PrecioAnunciado);
        }

        [Test]
        public void ResumenUsaElImporteMasBajoConFormaDesde()
        {
            var lugar = LugarCon(ConceptoWeb("a/fijo", ValorPrecio.Fijo(9000)), ConceptoWeb("a/desde", ValorPrecio.Desde(4000)),
                ConceptoWeb("a/consultar", null));
            Assert.AreEqual(ValorPrecio.Desde(4000), ReglaResumen.Resumir(lugar, Ahora).PrecioAnunciado);
        }

        [Test]
        public void BarCachonNoTienePrecio()
        {
            var servicio = new ServicioLugares(new RepositorioLugaresJson());
            servicio.Cargar();
            var r = servicio.Resumen("bar-cachon", Ahora);
            Assert.IsTrue(r.Ok, r.Error);
            Assert.IsFalse(r.Valor.TienePrecio);
            Assert.IsNull(r.Valor.PrecioAnunciado);
        }

        [Test]
        public void LugarConSoloConsultarNoTienePrecio()
        {
            var lugar = LugarCon(ConceptoWeb("a/uno", null), ConceptoWeb("a/dos", null));
            Assert.IsFalse(ReglaResumen.Resumir(lugar, Ahora).TienePrecio);
        }

        [Test]
        public void ResumenDeUnIdDesconocidoEsUnFalloSinPrecio()
        {
            var servicio = new ServicioLugares(new RepositorioLugaresJson());
            servicio.Cargar();
            var r = servicio.Resumen("no-existe", Ahora);
            Assert.IsFalse(r.Ok);
            Assert.IsFalse(r.Valor.TienePrecio);
        }

        // ── Público ──

        [Test]
        public void LugarPublicoNoTienePrecioYSeQuedaSinConceptos()
        {
            var lugar = new Lugar("centro-de-salud", "Centro de salud", "Centro de salud", Plantilla.ServicioLocal,
                new Coordenada(36.78, -6.35), publico: true, conceptos: new[] { ConceptoWeb("a/consulta", ValorPrecio.Fijo(3000)) });
            Assert.IsTrue(lugar.Publico);
            Assert.IsEmpty(lugar.Conceptos);
            Assert.IsFalse(ReglaResumen.Resumir(lugar, Ahora).TienePrecio);
        }

        [Test]
        public void LaCargaDescartaLosConceptosDeUnLugarPublico()
        {
            const string json = @"{""lugares"":[{""id"":""centro-de-salud"",""nombre"":""Centro de salud"",""plantilla"":""servicio"",""lat"":36.78,""lon"":-6.35,""publico"":true,""conceptos"":[
                {""id"":""a/consulta"",""forma"":""fijo"",""cent"":3000,""url"":""https://x.es"",""fecha"":""2026-11-13""}]}]}";
            var servicio = new ServicioLugares(new RepositorioLugaresJson(json));
            Assert.IsTrue(servicio.Cargar().Ok);

            var lugar = servicio.Buscar("centro-de-salud");
            Assert.IsTrue(lugar.Publico);
            Assert.IsEmpty(lugar.Conceptos);
            Assert.IsFalse(servicio.Resumen("centro-de-salud", Ahora).Valor.TienePrecio);
        }

        // ── JSON dañado o ausente ──

        [TestCase("{ esto no es json")]
        [TestCase("")]
        [TestCase("{\"otra\":1}")]
        [TestCase("{\"lugares\":[{\"id\":\"x\",\"plantilla\":\"gasolinera\",\"lat\":36.78,\"lon\":-6.35}]}")]
        [TestCase("{\"lugares\":[{\"id\":\"x\",\"plantilla\":\"servicio\",\"conceptos\":[{\"id\":\"a/b\",\"forma\":\"fijo\",\"cent\":100,\"url\":\"\",\"fecha\":\"2026-11-13\"}]}]}")]
        [TestCase("{\"lugares\":[{\"id\":\"x\",\"plantilla\":\"ocio\"},{\"id\":\"x\",\"plantilla\":\"ocio\"}]}")]
        [TestCase("{\"lugares\":[{\"id\":\"x\",\"plantilla\":\"servicio\",\"conceptos\":[{\"id\":\"a/b\",\"forma\":\"fijo\",\"cent\":100,\"url\":\"https://x.es\",\"fecha\":\"13/11/2026\"}]}]}")]
        [TestCase("{\"lugares\":[{\"id\":\"x\",\"plantilla\":\"servicio\",\"conceptos\":[{\"id\":\"a/b\",\"forma\":\"gratis\",\"cent\":100,\"url\":\"https://x.es\",\"fecha\":\"2026-11-13\"}]}]}")]
        public void JsonDanadoDaListaVaciaYErrorSinCaerse(string json)
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: no se pueden cargar los lugares base"));
            var servicio = new ServicioLugares(new RepositorioLugaresJson(json));
            Resultado<IReadOnlyList<Lugar>> r = default;

            Assert.DoesNotThrow(() => r = servicio.Cargar());
            Assert.IsFalse(r.Ok);
            Assert.IsNotEmpty(r.Error);
            Assert.IsNotNull(r.Valor);
            Assert.IsEmpty(r.Valor);
            Assert.IsEmpty(servicio.Lugares);
        }

        [Test]
        public void RecursoAusenteDaListaVaciaYErrorSinCaerse()
        {
            LogAssert.Expect(LogType.Error, new Regex("^PriceMap: no se pueden cargar los lugares base: no se encuentra el recurso"));
            Resultado<IReadOnlyList<Lugar>> r = default;

            Assert.DoesNotThrow(() => r = new RepositorioLugaresJson("no-existe-este-recurso", true).Cargar());
            Assert.IsFalse(r.Ok);
            Assert.IsNotEmpty(r.Error);
            Assert.IsNotNull(r.Valor);
            Assert.IsEmpty(r.Valor);
        }

        [Test]
        public void FicheroAusenteDaListaVaciaYErrorSinCaerse()
        {
            // Un repositorio que falla como lo haría el adaptador sin el recurso
            var servicio = new ServicioLugares(new RepositorioFallido());
            var r = servicio.Cargar();
            Assert.IsFalse(r.Ok);
            Assert.IsEmpty(servicio.Lugares);
            Assert.IsNull(servicio.Buscar("clinica-areas"));
        }

        [Test]
        public void ElJsonEmpaquetadoExisteDondeLoBuscaElAdaptador()
        {
            var ruta = Path.Combine(Application.dataPath, "Datos", "Resources", RepositorioLugaresJson.Recurso + ".json");
            FileAssert.Exists(ruta);
            Assert.IsNotNull(Resources.Load<TextAsset>(RepositorioLugaresJson.Recurso));
        }

        class RepositorioFallido : IRepositorioLugares
        {
            public Resultado<IReadOnlyList<Lugar>> Cargar() =>
                Resultado<IReadOnlyList<Lugar>>.Fallo("no se encuentra", Array.Empty<Lugar>());
        }
    }
}
