using System.Collections.Generic;
using NUnit.Framework;
using PriceMap.Aplicacion;
using PriceMap.Dominio;

namespace PriceMap.Tests.EditMode
{
    /// <summary>Tema inicial, alternar y guardar (matriz de la historia 1.2, parte 1).</summary>
    public class PruebasTema
    {
        class PreferenciasFalsas : IPreferencias
        {
            public readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaFalso : ITemaSistema
        {
            public bool EsOscuro { get; set; }
        }

        [TestCase(null, true, Tema.Oscuro, TestName = "Primer arranque con el sistema oscuro: paleta D")]
        [TestCase(null, false, Tema.Claro, TestName = "Primer arranque con el sistema claro: paleta B")]
        [TestCase("oscuro", false, Tema.Oscuro, TestName = "Manda el tema guardado aunque el sistema sea claro")]
        [TestCase("claro", true, Tema.Claro, TestName = "Manda el tema guardado aunque el sistema sea oscuro")]
        [TestCase("violeta", true, Tema.Oscuro, TestName = "Un valor guardado desconocido se ignora")]
        [TestCase("", false, Tema.Claro, TestName = "Un valor guardado vacío se ignora")]
        public void ElTemaInicialSigueLaRegla(string guardado, bool sistemaOscuro, Tema esperado)
        {
            Assert.AreEqual(esperado, ReglaTema.Inicial(guardado, sistemaOscuro));
        }

        [Test]
        public void AlIniciarAnunciaElTemaInicial()
        {
            var servicio = new ServicioTema(new PreferenciasFalsas(), new SistemaFalso { EsOscuro = true });
            var recibidos = new List<Tema>();
            servicio.Cambiado += recibidos.Add;

            servicio.Iniciar();

            Assert.AreEqual(Tema.Oscuro, servicio.Actual);
            CollectionAssert.AreEqual(new[] { Tema.Oscuro }, recibidos);
        }

        [Test]
        public void AlternarCambiaGuardaYAnuncia()
        {
            var prefs = new PreferenciasFalsas();
            var servicio = new ServicioTema(prefs, new SistemaFalso { EsOscuro = false });
            var recibidos = new List<Tema>();
            servicio.Iniciar();
            servicio.Cambiado += recibidos.Add;

            servicio.Alternar();

            Assert.AreEqual(Tema.Oscuro, servicio.Actual);
            Assert.AreEqual("oscuro", prefs.Leer(ServicioTema.ClavePreferencia));
            CollectionAssert.AreEqual(new[] { Tema.Oscuro }, recibidos);

            servicio.Alternar();
            Assert.AreEqual(Tema.Claro, servicio.Actual);
            Assert.AreEqual("claro", prefs.Leer(ServicioTema.ClavePreferencia));
        }

        [Test]
        public void LaEleccionSeRecuerdaAlVolverAAbrir()
        {
            var prefs = new PreferenciasFalsas();
            var primera = new ServicioTema(prefs, new SistemaFalso { EsOscuro = false });
            primera.Iniciar();
            primera.Alternar(); // elige oscuro

            var segunda = new ServicioTema(prefs, new SistemaFalso { EsOscuro = false });
            segunda.Iniciar();
            Assert.AreEqual(Tema.Oscuro, segunda.Actual);
        }

        [Test]
        public void UnValorGuardadoDesconocidoSigueAlSistema()
        {
            var prefs = new PreferenciasFalsas();
            prefs.Guardar(ServicioTema.ClavePreferencia, "violeta");
            var servicio = new ServicioTema(prefs, new SistemaFalso { EsOscuro = true });
            servicio.Iniciar();
            Assert.AreEqual(Tema.Oscuro, servicio.Actual);
        }

        [Test]
        public void IniciarNoGuardaNada()
        {
            var prefs = new PreferenciasFalsas();
            new ServicioTema(prefs, new SistemaFalso { EsOscuro = true }).Iniciar();
            Assert.IsEmpty(prefs.datos, "El tema del sistema no se guarda: si el usuario no elige, sigue al sistema.");
        }
    }
}
