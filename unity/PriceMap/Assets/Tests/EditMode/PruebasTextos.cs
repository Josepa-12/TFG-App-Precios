using NUnit.Framework;
using PriceMap.EditorTools;
using PriceMap.UI;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace PriceMap.Tests.EditMode
{
    /// <summary>Todos los textos que usa la UI existen en la tabla "Textos" en es-ES (NFR-5).</summary>
    public class PruebasTextos
    {
        [Test]
        public void CadaClaveDeLaUITieneTextoEnEspanol()
        {
            var coleccion = LocalizationEditorSettings.GetStringTableCollection(ClavesTexto.Tabla);
            Assert.IsNotNull(coleccion, "Falta la tabla de Localization \"Textos\" (menú PriceMap ▸ Preparar interfaz).");
            var tabla = coleccion.GetTable(new LocaleIdentifier("es-ES")) as StringTable;
            Assert.IsNotNull(tabla, "La tabla \"Textos\" no tiene la locale es-ES.");

            foreach (var clave in ClavesTexto.Todas)
            {
                var entrada = tabla.GetEntry(clave);
                Assert.IsNotNull(entrada, $"Falta la clave {clave} en la tabla es-ES.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entrada.Value), $"La clave {clave} está vacía.");
            }
        }

        [Test]
        public void ElGeneradorCubreTodasLasClaves()
        {
            foreach (var clave in ClavesTexto.Todas)
                Assert.IsTrue(PrepararInterfaz.TextosEs.ContainsKey(clave), $"PrepararInterfaz no define el texto de {clave}.");
        }
    }
}
