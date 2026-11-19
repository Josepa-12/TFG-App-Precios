using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Busqueda
{
    /// <summary>
    /// Lee la respuesta GeoJSON de Photon (historia 2.3, parte 2) y la convierte en direcciones. Pura: no sale a la
    /// red. <c>coordinates</c> viene como [lon, lat]. Nombre: <c>name</c> o, si no hay, <c>street</c> +
    /// <c>housenumber</c>; localidad: <c>city</c>, <c>district</c> o <c>county</c>. Fuera los registros sin nombre ni
    /// calle y los que no traen coordenadas válidas.
    /// </summary>
    internal static class LecturaPhoton
    {
        // Forma de la respuesta para JsonUtility (los campos que no vienen quedan nulos o vacíos)
        [Serializable] class Respuesta { public Rasgo[] features; }
        [Serializable] class Rasgo { public Geometria geometry; public Propiedades properties; }
        [Serializable] class Geometria { public double[] coordinates; }
        [Serializable]
        class Propiedades
        {
            public string name, street, housenumber, city, district, county;
        }

        /// <summary>Las direcciones del texto JSON, en el orden de Photon; Fallo si el texto no es GeoJSON de Photon.</summary>
        public static Resultado<IReadOnlyList<Direccion>> Leer(string json)
        {
            var vacia = (IReadOnlyList<Direccion>)Array.Empty<Direccion>();
            if (string.IsNullOrWhiteSpace(json)) return Resultado<IReadOnlyList<Direccion>>.Fallo("Respuesta vacía de Photon.", vacia);
            Respuesta r;
            try { r = JsonUtility.FromJson<Respuesta>(json); }
            catch (Exception e) { return Resultado<IReadOnlyList<Direccion>>.Fallo("Respuesta de Photon ilegible: " + e.Message, vacia); }
            if (r == null || r.features == null)
                return Resultado<IReadOnlyList<Direccion>>.Fallo("La respuesta de Photon no trae \"features\".", vacia);

            var lista = new List<Direccion>(r.features.Length);
            var vistas = new HashSet<string>(); // Photon repite tramos de una misma calle: una fila por nombre y localidad
            foreach (var f in r.features)
            {
                var d = Direccion(f);
                if (d != null && vistas.Add(d.Nombre.ToLowerInvariant() + "|" + d.Localidad.ToLowerInvariant())) lista.Add(d);
            }
            return Resultado<IReadOnlyList<Direccion>>.Exito(lista.AsReadOnly());
        }

        static Direccion Direccion(Rasgo f)
        {
            var p = f?.properties;
            var c = f?.geometry?.coordinates;
            if (p == null || c == null || c.Length < 2) return null;
            double lon = c[0], lat = c[1];
            if (double.IsNaN(lat) || double.IsNaN(lon) || Math.Abs(lat) > 90.0 || Math.Abs(lon) > 180.0) return null;
            string nombre = Nombre(p);
            if (nombre == null) return null;
            return new Direccion(nombre, Primero(p.city, p.district, p.county) ?? "", new Coordenada(lat, lon));
        }

        static string Nombre(Propiedades p)
        {
            string nombre = Limpio(p.name);
            if (nombre != null) return nombre;
            string calle = Limpio(p.street);
            if (calle == null) return null;
            string numero = Limpio(p.housenumber);
            return numero != null ? calle + " " + numero : calle;
        }

        static string Primero(params string[] valores)
        {
            foreach (var v in valores)
            {
                var l = Limpio(v);
                if (l != null) return l;
            }
            return null;
        }

        static string Limpio(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
