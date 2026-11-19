using System;
using System.Collections.Generic;

namespace PriceMap.Dominio
{
    /// <summary>
    /// "Encuentra cerca" (historia 2.4; FR15): qué tipos de lugar de la app y qué categorías de punto de interés entran
    /// en cada categoría de la hoja del buscador (los 8 chips, que usan los valores de CategoriaPoi). Supermercados,
    /// farmacias y gasolineras solo tienen puntos de interés. El veterinario entra en Salud (decisión del 19/11/2026).
    /// </summary>
    public static class ReglaCerca
    {
        /// <summary>Las categorías de los chips, en el orden de la hoja.</summary>
        public static readonly CategoriaPoi[] Categorias =
        {
            CategoriaPoi.Comer, CategoriaPoi.Bares, CategoriaPoi.Super, CategoriaPoi.Farmacia,
            CategoriaPoi.Gasolina, CategoriaPoi.Deporte, CategoriaPoi.Salud, CategoriaPoi.Cultura,
        };

        // Tipo de negocio de los datos (Lugar.Tipo) → categoría del chip
        static readonly Dictionary<string, CategoriaPoi> PorTipo = new Dictionary<string, CategoriaPoi>(StringComparer.OrdinalIgnoreCase)
        {
            ["Restaurante"] = CategoriaPoi.Comer, ["Taberna"] = CategoriaPoi.Comer, ["Comida rápida"] = CategoriaPoi.Comer,
            ["Cafetería"] = CategoriaPoi.Comer, ["Heladería"] = CategoriaPoi.Comer,
            ["Bar"] = CategoriaPoi.Bares, ["Pub"] = CategoriaPoi.Bares, ["Discoteca"] = CategoriaPoi.Bares,
            ["Centro deportivo"] = CategoriaPoi.Deporte, ["Recinto deportivo"] = CategoriaPoi.Deporte, ["Piscina"] = CategoriaPoi.Deporte,
            ["Pista de pádel"] = CategoriaPoi.Deporte, ["Pista de tenis"] = CategoriaPoi.Deporte, ["Campo de fútbol"] = CategoriaPoi.Deporte,
            ["Club de golf"] = CategoriaPoi.Deporte, ["Club náutico"] = CategoriaPoi.Deporte,
            ["Clínica dental"] = CategoriaPoi.Salud, ["Clínica médica"] = CategoriaPoi.Salud, ["Clínica privada"] = CategoriaPoi.Salud,
            ["Fisioterapia"] = CategoriaPoi.Salud, ["Veterinario"] = CategoriaPoi.Salud,
            ["Cine"] = CategoriaPoi.Cultura, ["Teatro"] = CategoriaPoi.Cultura, ["Museo"] = CategoriaPoi.Cultura,
            ["Monumento"] = CategoriaPoi.Cultura, ["Visita turística"] = CategoriaPoi.Cultura,
        };

        /// <summary>Categoría del chip en la que entra un tipo de lugar; Ninguna si no entra en ninguna.</summary>
        public static CategoriaPoi CategoriaDeTipo(string tipo) =>
            tipo != null && PorTipo.TryGetValue(tipo.Trim(), out var c) ? c : CategoriaPoi.Ninguna;

        /// <summary>Categoría del chip en la que entra una categoría de punto de interés; Ninguna si no entra.</summary>
        public static CategoriaPoi CategoriaDePunto(CategoriaPoi poi)
        {
            switch (poi)
            {
                case CategoriaPoi.Comer:
                case CategoriaPoi.Rapida:
                case CategoriaPoi.Cafe: return CategoriaPoi.Comer;
                case CategoriaPoi.Bares: return CategoriaPoi.Bares;
                case CategoriaPoi.Super: return CategoriaPoi.Super;
                case CategoriaPoi.Farmacia: return CategoriaPoi.Farmacia;
                case CategoriaPoi.Gasolina: return CategoriaPoi.Gasolina;
                case CategoriaPoi.Deporte: return CategoriaPoi.Deporte;
                case CategoriaPoi.Salud: return CategoriaPoi.Salud; // también los centros públicos y el veterinario
                case CategoriaPoi.Cultura: return CategoriaPoi.Cultura;
                default: return CategoriaPoi.Ninguna;
            }
        }

        /// <summary>
        /// True si el lugar entra en la categoría del chip. Con <paramref name="chip"/> null ("Encuentra cerca"), si
        /// entra en alguna de las 8.
        /// </summary>
        public static bool Incluye(CategoriaPoi? chip, Lugar lugar)
        {
            if (lugar == null) return false;
            var c = CategoriaDeTipo(lugar.Tipo);
            return c != CategoriaPoi.Ninguna && (chip == null || c == chip.Value);
        }

        /// <summary>
        /// True si un punto de interés de esa categoría entra en la del chip. Con <paramref name="chip"/> null
        /// ("Encuentra cerca"), si entra en alguna de las 8.
        /// </summary>
        public static bool Incluye(CategoriaPoi? chip, CategoriaPoi poi)
        {
            var c = CategoriaDePunto(poi);
            return c != CategoriaPoi.Ninguna && (chip == null || c == chip.Value);
        }
    }
}
