using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Qué categoría tiene cada punto de interés de OpenStreetMap, cuáles no se dibujan y cuáles son
    /// centros sanitarios públicos (FR-4). Portada de prototipo/lugares.js (SUB_CAT, CLASS_CAT,
    /// HIDDEN_SUB, poiCatOf y PUBLIC_HEALTH): la subclase manda sobre la clase.
    /// </summary>
    public static class ReglaPuntosInteres
    {
        // La subclase manda sobre la clase (p. ej. class "fuel" + subclass "charging_station" → carga eléctrica)
        static readonly Dictionary<string, CategoriaPoi> PorSubclase = new Dictionary<string, CategoriaPoi>
        {
            ["charging_station"] = CategoriaPoi.Carga, ["nightclub"] = CategoriaPoi.Bares, ["food_court"] = CategoriaPoi.Rapida,
            ["supermarket"] = CategoriaPoi.Super, ["marketplace"] = CategoriaPoi.Super, ["greengrocer"] = CategoriaPoi.Super,
            ["butcher"] = CategoriaPoi.Super, ["frozen_food"] = CategoriaPoi.Super, ["convenience"] = CategoriaPoi.Super,
            ["bakery"] = CategoriaPoi.Cafe, ["ice_cream"] = CategoriaPoi.Cafe, ["hairdresser"] = CategoriaPoi.Belleza,
            ["beauty"] = CategoriaPoi.Belleza, ["veterinary"] = CategoriaPoi.Salud,
            ["car_repair"] = CategoriaPoi.Talleres, ["car_parts"] = CategoriaPoi.Talleres,
            ["bus_station"] = CategoriaPoi.Transporte, ["taxi"] = CategoriaPoi.Transporte,
            ["post_office"] = CategoriaPoi.Publico, ["books"] = CategoriaPoi.Tiendas, ["library"] = CategoriaPoi.Cultura,
            ["visitor_centre"] = CategoriaPoi.Cultura, ["laundry"] = CategoriaPoi.Tiendas,
            ["guest_house"] = CategoriaPoi.Hoteles, ["hotel"] = CategoriaPoi.Hoteles, ["mall"] = CategoriaPoi.Tiendas,
        };

        static readonly Dictionary<string, CategoriaPoi> PorClase = new Dictionary<string, CategoriaPoi>
        {
            ["restaurant"] = CategoriaPoi.Comer, ["fast_food"] = CategoriaPoi.Rapida, ["cafe"] = CategoriaPoi.Cafe,
            ["bakery"] = CategoriaPoi.Cafe, ["ice_cream"] = CategoriaPoi.Cafe, ["bar"] = CategoriaPoi.Bares, ["beer"] = CategoriaPoi.Bares,
            ["grocery"] = CategoriaPoi.Super, ["butcher"] = CategoriaPoi.Super, ["shop"] = CategoriaPoi.Tiendas,
            ["clothing_store"] = CategoriaPoi.Tiendas, ["alcohol_shop"] = CategoriaPoi.Tiendas, ["laundry"] = CategoriaPoi.Tiendas,
            ["fuel"] = CategoriaPoi.Gasolina, ["parking"] = CategoriaPoi.Parking, ["bicycle_parking"] = CategoriaPoi.Parking,
            ["lodging"] = CategoriaPoi.Hoteles, ["campsite"] = CategoriaPoi.Hoteles,
            ["pharmacy"] = CategoriaPoi.Farmacia, ["hospital"] = CategoriaPoi.Salud, ["doctors"] = CategoriaPoi.Salud,
            ["dentist"] = CategoriaPoi.Salud, ["veterinary"] = CategoriaPoi.Salud,
            ["sports_centre"] = CategoriaPoi.Deporte, ["pitch"] = CategoriaPoi.Deporte, ["stadium"] = CategoriaPoi.Deporte,
            ["swimming_pool"] = CategoriaPoi.Deporte, ["golf"] = CategoriaPoi.Deporte, ["athletics"] = CategoriaPoi.Deporte,
            ["sailing"] = CategoriaPoi.Deporte, ["fitness"] = CategoriaPoi.Deporte, ["fitness_centre"] = CategoriaPoi.Deporte,
            ["park"] = CategoriaPoi.Parques, ["garden"] = CategoriaPoi.Parques, ["playground"] = CategoriaPoi.Parques,
            ["museum"] = CategoriaPoi.Cultura, ["art_gallery"] = CategoriaPoi.Cultura, ["attraction"] = CategoriaPoi.Cultura,
            ["castle"] = CategoriaPoi.Cultura, ["monument"] = CategoriaPoi.Cultura, ["cinema"] = CategoriaPoi.Cultura,
            ["theatre"] = CategoriaPoi.Cultura, ["library"] = CategoriaPoi.Cultura, ["information"] = CategoriaPoi.Cultura,
            ["bank"] = CategoriaPoi.Bancos, ["atm"] = CategoriaPoi.Bancos, ["hairdresser"] = CategoriaPoi.Belleza,
            ["car"] = CategoriaPoi.Talleres,
            ["ferry_terminal"] = CategoriaPoi.Transporte, ["harbor"] = CategoriaPoi.Transporte,
            ["railway"] = CategoriaPoi.Transporte, ["bus"] = CategoriaPoi.Transporte,
            ["school"] = CategoriaPoi.Educacion, ["college"] = CategoriaPoi.Educacion, ["university"] = CategoriaPoi.Educacion,
            ["kindergarten"] = CategoriaPoi.Educacion,
            ["town_hall"] = CategoriaPoi.Publico, ["police"] = CategoriaPoi.Publico, ["post"] = CategoriaPoi.Publico,
            ["fire_station"] = CategoriaPoi.Publico, ["place_of_worship"] = CategoriaPoi.Culto,
        };

        /// <summary>Subclases que nunca se dibujan: demasiadas (128 paradas en Sanlúcar) o sin interés para la app.</summary>
        static readonly HashSet<string> SubclasesOcultas = new HashSet<string> { "bus_stop", "office" };

        /// <summary>Sanidad pública: aparece en el mapa para saber dónde está, pero nunca lleva precios.</summary>
        // Palabras completas: "chare" dentro de otra palabra no cuenta
        static readonly Regex SanidadPublica = new Regex(@"\b(consultorio|centro de salud|urgencias|hospital|ambulatorio|chare)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        const string Veterinario = "veterinary";

        /// <summary>Categoría del punto; Ninguna si se oculta o no tiene (entonces no se dibuja).</summary>
        public static CategoriaPoi Categoria(string clase, string subclase)
        {
            if (subclase != null && SubclasesOcultas.Contains(subclase)) return CategoriaPoi.Ninguna;
            if (subclase != null && PorSubclase.TryGetValue(subclase, out var c)) return c;
            if (clase != null && PorClase.TryGetValue(clase, out c)) return c;
            return CategoriaPoi.Ninguna;
        }

        /// <summary>True si el punto se dibuja (tiene categoría).</summary>
        public static bool SeDibuja(string clase, string subclase) => Categoria(clase, subclase) != CategoriaPoi.Ninguna;

        /// <summary>
        /// True si es un centro sanitario público ("Centro de Salud Barrio Bajo", "Hospital..."):
        /// categoría salud y el nombre lo dice. Los veterinarios (clase o subclase "veterinary") nunca lo
        /// son, aunque se llamen "Hospital Veterinario". Nunca tendrá precio.
        /// </summary>
        public static bool EsSanidadPublica(CategoriaPoi categoria, string nombre, string clase = null, string subclase = null) =>
            categoria == CategoriaPoi.Salud && clase != Veterinario && subclase != Veterinario
            && !string.IsNullOrEmpty(nombre) && SanidadPublica.IsMatch(nombre);

        /// <summary>
        /// Distancia (m) por debajo de la cual un punto con el mismo nombre que un lugar de la app es ese
        /// mismo sitio (applyPoiDedup del prototipo). Más lejos es otra sucursal y se ve.
        /// </summary>
        public const double UmbralRepetidoMetros = 80.0;

        /// <summary>
        /// Nombre para comparar: minúsculas, sin tildes, sin signos y sin espacios repetidos
        /// ("BAR  CACHÓN!" → "bar cachon"). Vacío si no hay nombre.
        /// </summary>
        public static string NombreNormalizado(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return "";
            var descompuesto = nombre.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(descompuesto.Length);
            bool espacio = false;
            foreach (var c in descompuesto)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark || cat == UnicodeCategory.SpacingCombiningMark
                    || cat == UnicodeCategory.EnclosingMark) continue; // tildes y diéresis
                if (char.IsLetterOrDigit(c))
                {
                    if (espacio && sb.Length > 0) sb.Append(' ');
                    espacio = false;
                    sb.Append(char.ToLowerInvariant(c));
                }
                else espacio = true; // signos y espacios separan palabras, nunca se repiten
            }
            return sb.ToString();
        }

        /// <summary>
        /// True si el punto (nombre y posición) repite uno de los lugares de la app cuyo marcador se ve
        /// ahora: mismo nombre normalizado y a menos de UmbralRepetidoMetros. Entonces solo se ve el lugar.
        /// Un punto sin nombre nunca es repetido.
        /// </summary>
        public static bool EsRepetido(string nombre, Coordenada posicion, IEnumerable<Lugar> lugaresVisibles)
        {
            if (lugaresVisibles == null || string.IsNullOrEmpty(nombre)) return false;
            string n = null; // se calcula solo si hay algún lugar cerca
            foreach (var l in lugaresVisibles)
            {
                if (l == null || !DentroDelUmbral(posicion.DistanciaMetros(l.Posicion))) continue;
                n ??= NombreNormalizado(nombre);
                if (n.Length > 0 && NombreNormalizado(l.Nombre) == n) return true;
            }
            return false;
        }

        /// <summary>
        /// Igual que EsRepetido, con los nombres ya normalizados (para cada fotograma, sin crear texto):
        /// nombreNormalizado es el del punto y nombreNormalizadoDe da el de cada lugar (la UI lo guarda).
        /// </summary>
        public static bool EsRepetidoNormalizado(string nombreNormalizado, Coordenada posicion,
            IReadOnlyList<Lugar> lugaresVisibles, Func<Lugar, string> nombreNormalizadoDe)
        {
            if (lugaresVisibles == null || nombreNormalizadoDe == null || string.IsNullOrEmpty(nombreNormalizado)) return false;
            for (int i = 0; i < lugaresVisibles.Count; i++)
            {
                var l = lugaresVisibles[i];
                if (l != null && DentroDelUmbral(posicion.DistanciaMetros(l.Posicion)) && nombreNormalizadoDe(l) == nombreNormalizado)
                    return true;
            }
            return false;
        }

        /// <summary>True si a esa distancia (m) aún es el mismo sitio: estrictamente menos de UmbralRepetidoMetros.</summary>
        public static bool DentroDelUmbral(double metros) => metros < UmbralRepetidoMetros;
    }
}
