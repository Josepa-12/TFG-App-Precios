using System;
using NUnit.Framework;
using PriceMap.Dominio;

namespace PriceMap.Tests.EditMode
{
    /// <summary>Precio mostrado, origen, frescura y variantes (historia 1.3; AD-3, FR-8, FR-9, FR-33, FR-60).</summary>
    public class PruebasPrecio
    {
        const string UrlAreas = "https://clinicaareas.com/tarifas";
        static readonly DateTime Consulta = new DateTime(2026, 11, 13, 12, 0, 0);
        static readonly DateTime Ahora = new DateTime(2026, 11, 17, 12, 0, 0);

        // ── Precio y origen ──

        [Test]
        public void WebDesdeDaElPrecioConSuOrigenYAlDia()
        {
            var fisio = new FuentesPrecio(Plantilla.ServicioLocal, new PrecioWeb(ValorPrecio.Desde(4000), UrlAreas, Consulta));
            var p = CalculadoraPrecio.Calcular(fisio, Ahora);

            Assert.AreEqual(ValorPrecio.Desde(4000), p.Valor);
            Assert.AreEqual(TipoOrigen.Web, p.Origen.Tipo);
            Assert.AreEqual(UrlAreas, p.Origen.Url);
            Assert.AreEqual(Consulta, p.Origen.FechaWeb);
            Assert.AreEqual(EstadoFrescura.AlDia, p.Frescura.Value.Estado);
            Assert.AreEqual(Consulta, p.Frescura.Value.UltimaConfirmacion);
            Assert.IsFalse(p.EsConsultar);
        }

        [Test]
        public void WebSinImporteEsAConsultarSinFrescura()
        {
            var infiltraciones = new FuentesPrecio(Plantilla.ServicioLocal, new PrecioWeb(null, UrlAreas, Consulta));
            var p = CalculadoraPrecio.Calcular(infiltraciones, Ahora);

            Assert.IsTrue(p.EsConsultar);
            Assert.IsFalse(p.TienePrecio);
            Assert.AreEqual(TipoOrigen.Web, p.Origen.Tipo, "Hasta 'A consultar' dice de dónde sale.");
            Assert.IsNull(p.Frescura);
        }

        [Test]
        public void WebConImporteImposibleSeTrataComoAConsultar()
        {
            var p = CalculadoraPrecio.Calcular(new FuentesPrecio(Plantilla.ServicioLocal, new PrecioWeb(ValorPrecio.Fijo(0), UrlAreas, Consulta)), Ahora);
            Assert.IsTrue(p.EsConsultar);
            Assert.IsFalse(p.TienePrecio, "Nunca se muestra un precio ≤ 0 (NFR-6).");
        }

        [Test]
        public void SinWebNiDeclaracionesNoHayPrecio()
        {
            var chemaBarber = new FuentesPrecio(Plantilla.ServicioLocal);
            var p = CalculadoraPrecio.Calcular(chemaBarber, Ahora);

            Assert.IsFalse(p.TienePrecio);
            Assert.IsFalse(p.EsConsultar);
            Assert.AreEqual(TipoOrigen.SinPrecio, p.Origen.Tipo);
            Assert.IsNull(p.Frescura);
        }

        [Test]
        public void MandaLaDeclaracionMasRecienteSobreLaWeb()
        {
            var mayo = new DateTime(2026, 5, 2);
            var septiembre = new DateTime(2026, 9, 1);
            var octubre = new DateTime(2026, 10, 20);
            var fuentes = new FuentesPrecio(Plantilla.ServicioLocal,
                new PrecioWeb(ValorPrecio.Fijo(3500), "https://ejemplo.es", mayo),
                new[] { new Declaracion(ValorPrecio.Fijo(3800), septiembre), new Declaracion(ValorPrecio.Fijo(4200), octubre) });

            var p = CalculadoraPrecio.Calcular(fuentes, Ahora);

            Assert.AreEqual(ValorPrecio.Fijo(4200), p.Valor);
            Assert.AreEqual(TipoOrigen.Declarado, p.Origen.Tipo);
            Assert.AreEqual(octubre, p.Frescura.Value.UltimaConfirmacion);
        }

        [Test]
        public void LasDeclaracionesInvalidasSeIgnoran()
        {
            var web = new PrecioWeb(ValorPrecio.Fijo(3500), "https://ejemplo.es", Consulta);
            var fuentes = new FuentesPrecio(Plantilla.ServicioLocal, web, new[]
            {
                new Declaracion(ValorPrecio.Fijo(0), Ahora),
                new Declaracion(ValorPrecio.Fijo(-500), Ahora),
                new Declaracion(ValorPrecio.Rango(2000, 1000), Ahora),
            });
            var p = CalculadoraPrecio.Calcular(fuentes, Ahora);
            Assert.AreEqual(TipoOrigen.Web, p.Origen.Tipo);
            Assert.AreEqual(ValorPrecio.Fijo(3500), p.Valor);
        }

        [Test]
        public void RangoDeHosteleriaCerradoYAbierto()
        {
            var cerrado = CalculadoraPrecio.Calcular(new FuentesPrecio(Plantilla.Hosteleria, null,
                new[] { new Declaracion(ValorPrecio.Rango(1000, 2000), Ahora) }), Ahora);
            Assert.AreEqual(FormaPrecio.Rango, cerrado.Valor.Value.Forma);
            Assert.AreEqual(1000, cerrado.Valor.Value.MinCent);
            Assert.AreEqual(2000, cerrado.Valor.Value.MaxCent);

            var abierto = CalculadoraPrecio.Calcular(new FuentesPrecio(Plantilla.Hosteleria, null,
                new[] { new Declaracion(ValorPrecio.Rango(5000, null), Ahora) }), Ahora);
            Assert.IsNull(abierto.Valor.Value.MaxCent, "\"Más de 50 €\": rango abierto, sin inventar tope.");
        }

        [Test]
        public void SiempreHayOrigenYNuncaUnImporteNoPositivo()
        {
            var casos = new[]
            {
                new FuentesPrecio(Plantilla.Ocio),
                new FuentesPrecio(Plantilla.Ocio, new PrecioWeb(null, "u", Consulta)),
                new FuentesPrecio(Plantilla.Ocio, new PrecioWeb(ValorPrecio.Fijo(-1), "u", Consulta)),
                new FuentesPrecio(Plantilla.Ocio, new PrecioWeb(ValorPrecio.Fijo(900), "u", Consulta)),
                new FuentesPrecio(Plantilla.Hosteleria, null, new[] { new Declaracion(ValorPrecio.Rango(0, 10), Ahora) }),
            };
            var esperados = new[] { TipoOrigen.SinPrecio, TipoOrigen.Web, TipoOrigen.Web, TipoOrigen.Web, TipoOrigen.SinPrecio };
            for (int i = 0; i < casos.Length; i++)
            {
                var p = CalculadoraPrecio.Calcular(casos[i], Ahora);
                Assert.AreEqual(esperados[i], p.Origen.Tipo, $"Caso {i}");
                if (p.TienePrecio) Assert.Greater(p.Valor.Value.ImporteCent, 0);
            }
            Assert.AreEqual(TipoOrigen.SinPrecio, default(Origen).Tipo, "Un origen sin rellenar nunca parece de la web.");
        }

        // ── Frescura (límites exactos) ──

        static readonly DateTime Base = new DateTime(2026, 1, 15, 10, 0, 0);

        [TestCase(29, EstadoFrescura.AlDia)]
        [TestCase(30, EstadoFrescura.PodriaHaberCambiado)]
        [TestCase(60, EstadoFrescura.PodriaHaberCambiado)]
        [TestCase(61, EstadoFrescura.Antiguo)]
        public void FrescuraDeHosteleriaEnDias(int dias, EstadoFrescura esperado)
        {
            Assert.AreEqual(esperado, CalculadoraPrecio.Estado(Plantilla.Hosteleria, Base, Base.AddDays(dias)));
        }

        [TestCase(Plantilla.ServicioLocal)]
        [TestCase(Plantilla.Ocio)]
        public void FrescuraDeServicioYOcioEnMeses(Plantilla plantilla)
        {
            Assert.AreEqual(EstadoFrescura.AlDia, CalculadoraPrecio.Estado(plantilla, Base, Base.AddMonths(3).AddDays(-1)));
            Assert.AreEqual(EstadoFrescura.PodriaHaberCambiado, CalculadoraPrecio.Estado(plantilla, Base, Base.AddMonths(3)));
            Assert.AreEqual(EstadoFrescura.PodriaHaberCambiado, CalculadoraPrecio.Estado(plantilla, Base, Base.AddMonths(6)));
            Assert.AreEqual(EstadoFrescura.Antiguo, CalculadoraPrecio.Estado(plantilla, Base, Base.AddMonths(6).AddDays(1)));
        }

        [Test]
        public void CalcularUsaLaFrescuraDeLaPlantillaYLaFechaDeLaDeclaracion()
        {
            // 45 días: en hostelería "podría haber cambiado"; con la regla de meses seguiría "al día"
            var hace45 = Ahora.AddDays(-45);
            var bar = CalculadoraPrecio.Calcular(new FuentesPrecio(Plantilla.Hosteleria, null,
                new[] { new Declaracion(ValorPrecio.Rango(1000, 2000), hace45) }), Ahora);
            Assert.AreEqual(EstadoFrescura.PodriaHaberCambiado, bar.Frescura.Value.Estado);

            var vieja = CalculadoraPrecio.Calcular(new FuentesPrecio(Plantilla.ServicioLocal, null,
                new[] { new Declaracion(ValorPrecio.Fijo(2000), Ahora.AddMonths(-7)) }), Ahora);
            Assert.AreEqual(EstadoFrescura.Antiguo, vieja.Frescura.Value.Estado);
        }

        [Test]
        public void UnaDeclaracionAntiguaMandaAunqueLaWebSeaMasReciente()
        {
            // Regla de la historia: lo que declara el negocio manda sobre su web (hasta que manden los clientes, 4.10)
            var fuentes = new FuentesPrecio(Plantilla.ServicioLocal,
                new PrecioWeb(ValorPrecio.Fijo(3500), "https://ejemplo.es", Ahora.AddDays(-1)),
                new[] { new Declaracion(ValorPrecio.Fijo(3000), Ahora.AddYears(-2)) });
            var p = CalculadoraPrecio.Calcular(fuentes, Ahora);
            Assert.AreEqual(TipoOrigen.Declarado, p.Origen.Tipo);
            Assert.AreEqual(EstadoFrescura.Antiguo, p.Frescura.Value.Estado);
        }

        [Test]
        public void ConLaMismaFechaGanaLaUltimaDeclaracion()
        {
            var p = CalculadoraPrecio.Calcular(new FuentesPrecio(Plantilla.ServicioLocal, null, new[]
            {
                new Declaracion(ValorPrecio.Fijo(1000), Consulta), new Declaracion(ValorPrecio.Fijo(1100), Consulta),
            }), Ahora);
            Assert.AreEqual(ValorPrecio.Fijo(1100), p.Valor);
        }

        [Test]
        public void UtcYHoraLocalSeComparanBien()
        {
            var confirmadoUtc = DateTime.SpecifyKind(new DateTime(2026, 10, 18, 10, 0, 0), DateTimeKind.Utc);
            var ahoraLocal = confirmadoUtc.AddDays(29).ToLocalTime();
            Assert.AreEqual(EstadoFrescura.AlDia, CalculadoraPrecio.Estado(Plantilla.Hosteleria, confirmadoUtc, ahoraLocal));
        }

        [Test]
        public void UnaFechaExtremaNoRompeElCalculo()
        {
            Assert.DoesNotThrow(() => CalculadoraPrecio.Estado(Plantilla.ServicioLocal, DateTime.MaxValue, Ahora));
        }

        [Test]
        public void UnaConfirmacionFuturaCuentaComoAlDia()
        {
            Assert.AreEqual(EstadoFrescura.AlDia, CalculadoraPrecio.Estado(Plantilla.Hosteleria, Ahora.AddDays(2), Ahora));
        }

        [Test]
        public void LosUmbralesVienenDeParametrosNegocio()
        {
            Assert.AreEqual(30, ParametrosNegocio.FrescuraHosteleriaDiasAlDia);
            Assert.AreEqual(60, ParametrosNegocio.FrescuraHosteleriaDiasAntiguo);
            Assert.AreEqual(3, ParametrosNegocio.FrescuraOtrosMesesAlDia);
            Assert.AreEqual(6, ParametrosNegocio.FrescuraOtrosMesesAntiguo);
        }

        // ── Variantes ──

        static readonly DateTime SabadoNoche = new DateTime(2026, 11, 21, 21, 0, 0); // sábado 21:00

        static Variante Var(string id, CondicionVariante c, int cent) =>
            new Variante(id, c, new FuentesPrecio(Plantilla.Ocio, new PrecioWeb(ValorPrecio.Fijo(cent), "u", Consulta)));

        [Test]
        public void LaVarianteVigenteEsLaPrimeraQueCoincide()
        {
            var conLuz = Var("pista@con-luz", CondicionVariante.Horario(new TimeSpan(20, 0, 0), TimeSpan.Zero), 1600);
            var finde = Var("pista@fin-de-semana", CondicionVariante.EnDias(DayOfWeek.Saturday, DayOfWeek.Sunday), 1400);
            Assert.AreSame(conLuz, ReglaVariantes.Vigente(new[] { conLuz, finde }, SabadoNoche));
            Assert.AreSame(finde, ReglaVariantes.Vigente(new[] { conLuz, finde }, SabadoNoche.AddHours(-10)), "Sábado 11:00: sin luz, fin de semana.");
        }

        [Test]
        public void SinCoincidenciasNoHayVigente()
        {
            var finde = Var("pista@fin-de-semana", CondicionVariante.EnDias(DayOfWeek.Saturday, DayOfWeek.Sunday), 1400);
            Assert.IsNull(ReglaVariantes.Vigente(new[] { finde }, new DateTime(2026, 11, 18, 10, 0, 0))); // miércoles
            Assert.IsNull(ReglaVariantes.Vigente(null, SabadoNoche));
        }

        [TestCase(2, true)]
        [TestCase(23, true)]
        [TestCase(6, false, TestName = "La franja de noche termina a las 06:00 (sin incluir)")]
        [TestCase(12, false)]
        public void HorarioDeNocheCruzaLaMedianoche(int hora, bool vigente)
        {
            var noche = Var("parking@noche", CondicionVariante.Horario(new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0)), 300);
            var instante = new DateTime(2026, 11, 18, hora, 0, 0);
            Assert.AreEqual(vigente, ReglaVariantes.Vigente(new[] { noche }, instante) != null);
        }

        [TestCase(9, 0, true, TestName = "Franja de día: empieza a las 09:00 (incluida)")]
        [TestCase(13, 59, true)]
        [TestCase(14, 0, false, TestName = "Franja de día: termina a las 14:00 (sin incluir)")]
        [TestCase(20, 0, false)]
        public void HorarioDeDiaNoCruzaLaMedianoche(int hora, int minuto, bool vigente)
        {
            var manana = Var("pista@manana", CondicionVariante.Horario(new TimeSpan(9, 0, 0), new TimeSpan(14, 0, 0)), 1000);
            Assert.AreEqual(vigente, ReglaVariantes.Vigente(new[] { manana }, new DateTime(2026, 11, 18, hora, minuto, 0)) != null);
        }

        [Test]
        public void HastaMedianocheYTodoElDia()
        {
            var conLuz = Var("pista@con-luz", CondicionVariante.Horario(new TimeSpan(20, 0, 0), TimeSpan.Zero), 1600);
            Assert.IsNotNull(ReglaVariantes.Vigente(new[] { conLuz }, new DateTime(2026, 11, 18, 23, 59, 0)));
            Assert.IsNull(ReglaVariantes.Vigente(new[] { conLuz }, new DateTime(2026, 11, 18, 0, 0, 0)), "A las 00:00 ya no.");
            var siempre = Var("x@todo-el-dia", CondicionVariante.Horario(new TimeSpan(8, 0, 0), new TimeSpan(8, 0, 0)), 100);
            Assert.IsNotNull(ReglaVariantes.Vigente(new[] { siempre }, new DateTime(2026, 11, 18, 3, 0, 0)));
        }

        [Test]
        public void LasCondicionesInvalidasSeRechazan()
        {
            Assert.Throws<ArgumentException>(() => CondicionVariante.EnDias());
            Assert.Throws<ArgumentException>(() => CondicionVariante.EnDias(null));
            Assert.Throws<ArgumentException>(() => CondicionVariante.DeCliente(" "));
            Assert.Throws<ArgumentOutOfRangeException>(() => CondicionVariante.Horario(TimeSpan.FromHours(-1), TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => CondicionVariante.Horario(TimeSpan.Zero, TimeSpan.FromHours(25)));
            Assert.Throws<ArgumentException>(() => new PrecioWeb(ValorPrecio.Fijo(100), "", Consulta));
            Assert.Throws<ArgumentNullException>(() => CalculadoraPrecio.Calcular(null, Ahora));
        }

        [Test]
        public void LasVariantesDeTipoDeClienteNuncaEstanVigentes()
        {
            var bono = Var("superinductiva@con-bono", CondicionVariante.DeCliente("con-bono"), 1500);
            Assert.IsNull(ReglaVariantes.Vigente(new[] { bono }, SabadoNoche));
        }

        [Test]
        public void CadaVarianteTieneSuPrecioYSuFrescura()
        {
            var antigua = new DateTime(2026, 1, 1);
            var bono = new Variante("superinductiva@con-bono", CondicionVariante.DeCliente("con-bono"),
                new FuentesPrecio(Plantilla.ServicioLocal, new PrecioWeb(ValorPrecio.Desde(1500), UrlAreas, Consulta)));
            var socios = new Variante("superinductiva@socios", CondicionVariante.DeCliente("socio"),
                new FuentesPrecio(Plantilla.ServicioLocal, new PrecioWeb(ValorPrecio.Fijo(1200), UrlAreas, antigua)));

            var pBono = CalculadoraPrecio.Calcular(bono.Fuentes, Ahora);
            var pSocios = CalculadoraPrecio.Calcular(socios.Fuentes, Ahora);
            Assert.AreEqual(ValorPrecio.Desde(1500), pBono.Valor);
            Assert.AreEqual(EstadoFrescura.AlDia, pBono.Frescura.Value.Estado);
            Assert.AreEqual(ValorPrecio.Fijo(1200), pSocios.Valor);
            Assert.AreEqual(EstadoFrescura.Antiguo, pSocios.Frescura.Value.Estado, "Cada variante tiene su propia frescura.");
        }
    }
}
