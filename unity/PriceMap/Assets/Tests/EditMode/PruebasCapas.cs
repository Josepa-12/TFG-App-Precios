using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Comprueba la dirección de dependencias de AD-1 leyendo todos los .asmdef de Assets:
    /// si alguien añade una referencia prohibida, esta prueba falla con el nombre del ensamblado.
    /// </summary>
    public class PruebasCapas
    {
        [System.Serializable]
        class Asmdef
        {
            public string name;
            public string[] references = new string[0];
            public bool noEngineReferences;
        }

        const string Dominio = "PriceMap.Dominio";
        const string Aplicacion = "PriceMap.Aplicacion";
        static readonly string[] SoloNucleo = { Aplicacion, Dominio };

        /// <summary>Herramientas del editor y pruebas: no son capas de la app y pueden ver cualquier capa.</summary>
        static bool EsHerramienta(string nombre) => nombre == "PriceMap.Editor" || nombre.StartsWith("PriceMap.Tests.");

        /// <summary>Lee los .asmdef del proyecto; las referencias guardadas como "GUID:..." se traducen a su nombre.</summary>
        static List<Asmdef> LeerTodos() =>
            Directory.GetFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories)
                .Select(r => JsonUtility.FromJson<Asmdef>(File.ReadAllText(r)))
                .Select(a => { a.references = a.references.Select(NombreDeReferencia).ToArray(); return a; })
                .Where(a => !EsHerramienta(a.name))
                .ToList();

        static string NombreDeReferencia(string referencia)
        {
            if (!referencia.StartsWith("GUID:")) return referencia;
            var ruta = AssetDatabase.GUIDToAssetPath(referencia.Substring(5));
            return string.IsNullOrEmpty(ruta) ? referencia : JsonUtility.FromJson<Asmdef>(File.ReadAllText(ruta)).name;
        }

        /// <summary>Referencias permitidas para cada capa; null = cualquiera (solo el Arranque); vacío = no es una capa.</summary>
        static string[] Permitidas(string nombre)
        {
            if (nombre == Dominio) return new string[0];
            if (nombre == Aplicacion) return new[] { Dominio };
            if (nombre == "PriceMap.Vectorial" || nombre == "PriceMap.UI" || nombre.StartsWith("PriceMap.Adaptadores."))
                return SoloNucleo;
            if (nombre == "PriceMap.Arranque") return null;
            return new[] { "(no es una capa de la arquitectura)" };
        }

        /// <summary>Lista de incumplimientos de AD-1; vacía si todo está bien.</summary>
        static List<string> Incumplimientos(IEnumerable<Asmdef> ensamblados)
        {
            var errores = new List<string>();
            foreach (var a in ensamblados)
            {
                var permitidas = Permitidas(a.name);
                if (permitidas == null) continue;
                if (permitidas.Length == 1 && permitidas[0].StartsWith("("))
                {
                    errores.Add($"{a.name} no es ninguna capa de la arquitectura");
                    continue;
                }
                foreach (var r in a.references.Where(r => !permitidas.Contains(r)))
                    errores.Add($"{a.name} → {r}");
                if (a.name == Dominio && !a.noEngineReferences)
                    errores.Add($"{Dominio} debe tener noEngineReferences: true");
            }
            return errores;
        }

        [Test]
        public void ExistenTodasLasCapas()
        {
            var nombres = LeerTodos().Select(a => a.name).ToList();
            foreach (var capa in new[] { Dominio, Aplicacion, "PriceMap.Vectorial", "PriceMap.UI", "PriceMap.Arranque" })
                CollectionAssert.Contains(nombres, capa, $"Falta el ensamblado {capa}.");
            Assert.That(nombres.Any(n => n.StartsWith("PriceMap.Adaptadores.")), "Falta algún ensamblado PriceMap.Adaptadores.*.");
        }

        [Test]
        public void ElProyectoCumpleLaDireccionDeDependencias()
        {
            var errores = Incumplimientos(LeerTodos());
            Assert.IsEmpty(errores, "Referencias no permitidas por AD-1:\n" + string.Join("\n", errores));
        }

        [Test]
        public void LaComprobacionDetectaReferenciasProhibidas()
        {
            var falsos = new[]
            {
                new Asmdef { name = "PriceMap.UI", references = new[] { Aplicacion, "PriceMap.Vectorial" } },
                new Asmdef { name = "PriceMap.Adaptadores.Datos", references = new[] { "PriceMap.UI" } },
                new Asmdef { name = Dominio, references = new[] { Aplicacion }, noEngineReferences = false },
                new Asmdef { name = "PriceMap.Extra" },
                new Asmdef { name = "PriceMap.Arranque", references = new[] { "PriceMap.UI", "PriceMap.Vectorial" } },
            };
            CollectionAssert.AreEquivalent(new[]
            {
                "PriceMap.UI → PriceMap.Vectorial",
                "PriceMap.Adaptadores.Datos → PriceMap.UI",
                "PriceMap.Dominio → PriceMap.Aplicacion",
                "PriceMap.Dominio debe tener noEngineReferences: true",
                "PriceMap.Extra no es ninguna capa de la arquitectura",
            }, Incumplimientos(falsos));
        }
    }
}
