using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Algo que el lugar cobra (una sesión, un menú, una entrada), con id "area/concepto" (AD-11),
    /// el área en la que se agrupa en la ficha y lo que se sabe de su precio.
    /// </summary>
    public sealed class Concepto
    {
        static readonly IReadOnlyList<Variante> Ninguna = Array.Empty<Variante>();

        public string Id { get; }
        /// <summary>Área de la ficha en la que se agrupa (p. ej. "Fisioterapia").</summary>
        public string Area { get; }
        public string Nombre { get; }
        /// <summary>Unidad en la que se cobra (p. ej. "sesión"); vacía si no se sabe.</summary>
        public string Unidad { get; }
        public FuentesPrecio Fuentes { get; }
        public IReadOnlyList<Variante> Variantes { get; }

        public Concepto(string id, string area, string nombre, string unidad, FuentesPrecio fuentes, IReadOnlyList<Variante> variantes = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("El concepto necesita su id.", nameof(id));
            Id = id;
            Area = area ?? "";
            Nombre = nombre ?? "";
            Unidad = unidad ?? "";
            Fuentes = fuentes ?? throw new ArgumentNullException(nameof(fuentes));
            // Copia: si quien lo crea cambia su lista después, el concepto no cambia
            Variantes = variantes == null ? Ninguna : new List<Variante>(variantes).AsReadOnly();
        }
    }
}
