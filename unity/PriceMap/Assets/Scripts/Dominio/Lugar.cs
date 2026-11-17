using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Lugar de Sanlúcar que puede tener precios: id estable (slug u "osm:tipo:id", AD-11), nombre,
    /// tipo de negocio, plantilla, coordenada y sus conceptos. Un lugar público (sanidad pública)
    /// nunca tiene precio: se queda sin conceptos aunque se le pasen.
    /// </summary>
    public sealed class Lugar
    {
        static readonly IReadOnlyList<Concepto> Ninguno = Array.Empty<Concepto>();

        public string Id { get; }
        public string Nombre { get; }
        /// <summary>Tipo de negocio tal como se muestra (p. ej. "Bar", "Clínica privada").</summary>
        public string Tipo { get; }
        public Plantilla Plantilla { get; }
        public Coordenada Posicion { get; }
        /// <summary>Dirección postal; vacía si no se sabe.</summary>
        public string Direccion { get; }
        /// <summary>Centro público (sanidad pública): no se paga en el momento, así que nunca tiene precio.</summary>
        public bool Publico { get; }
        public IReadOnlyList<Concepto> Conceptos { get; }

        public Lugar(string id, string nombre, string tipo, Plantilla plantilla, Coordenada posicion,
            string direccion = null, bool publico = false, IReadOnlyList<Concepto> conceptos = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("El lugar necesita su id.", nameof(id));
            Id = id;
            Nombre = nombre ?? "";
            Tipo = tipo ?? "";
            Plantilla = plantilla;
            Posicion = posicion;
            Direccion = direccion ?? "";
            Publico = publico;
            // Copia: si quien lo crea cambia su lista después, el lugar no cambia
            Conceptos = publico || conceptos == null ? Ninguno : new List<Concepto>(conceptos).AsReadOnly();
        }
    }
}
