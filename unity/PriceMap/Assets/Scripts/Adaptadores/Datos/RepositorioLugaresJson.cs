using System;
using System.Collections.Generic;
using System.Globalization;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Datos
{
    /// <summary>
    /// Lee los lugares base empaquetados (Assets/Datos/Resources/lugares.json, generado con
    /// prototipo/herramientas/exportar-datos-unity.js) y los convierte en lugares del Dominio.
    /// Si el fichero no existe o está dañado, devuelve un fallo con una lista vacía: la app no se cae.
    /// </summary>
    public class RepositorioLugaresJson : IRepositorioLugares
    {
        public const string Recurso = "lugares";

        readonly string texto;
        /// <summary>Recurso de Resources que se lee; null si se lee el texto dado.</summary>
        readonly string recurso;

        /// <summary>Lee el JSON empaquetado en Resources.</summary>
        public RepositorioLugaresJson() : this(Recurso, true) { }

        /// <summary>Lee este texto JSON (pruebas).</summary>
        public RepositorioLugaresJson(string texto)
        {
            this.texto = texto;
        }

        /// <summary>
        /// Lee otro recurso de Resources (pruebas: un recurso que no existe). El bool solo distingue
        /// esta firma de la que recibe el texto.
        /// </summary>
        internal RepositorioLugaresJson(string recurso, bool desdeRecurso)
        {
            this.recurso = recurso;
        }

        public Resultado<IReadOnlyList<Lugar>> Cargar()
        {
            var json = texto;
            if (recurso != null)
            {
                var asset = Resources.Load<TextAsset>(recurso);
                if (asset == null) return Fallar($"no se encuentra el recurso '{recurso}' (Assets/Datos/Resources/{recurso}.json)");
                json = asset.text;
                Resources.UnloadAsset(asset);
            }
            if (string.IsNullOrWhiteSpace(json)) return Fallar("el fichero de lugares está vacío");

            try
            {
                var datos = JsonUtility.FromJson<DatosJson>(json);
                // JsonUtility puede dejar una lista que falta como vacía: un fichero sin lugares también está dañado
                if (datos?.lugares == null || datos.lugares.Length == 0) return Fallar("el fichero de lugares no tiene la lista 'lugares'");
                var lugares = new List<Lugar>(datos.lugares.Length);
                var ids = new HashSet<string>();
                foreach (var l in datos.lugares)
                {
                    var lugar = ALugar(l);
                    if (!ids.Add(lugar.Id)) throw new FormatException($"id repetido '{lugar.Id}'");
                    lugares.Add(lugar);
                }
                return Resultado<IReadOnlyList<Lugar>>.Exito(lugares);
            }
            catch (Exception e)
            {
                return Fallar("el fichero de lugares está dañado: " + e.Message);
            }
        }

        static Resultado<IReadOnlyList<Lugar>> Fallar(string motivo)
        {
            Debug.LogError("PriceMap: no se pueden cargar los lugares base: " + motivo + ".");
            return Resultado<IReadOnlyList<Lugar>>.Fallo(motivo, Array.Empty<Lugar>());
        }

        static Lugar ALugar(LugarJson l)
        {
            if (l == null || string.IsNullOrWhiteSpace(l.id)) throw new FormatException("hay un lugar sin id");
            var plantilla = APlantilla(l.plantilla, l.id);
            var conceptos = new List<Concepto>();
            // Un lugar público nunca tiene precio: sus conceptos se descartan (también lo garantiza Lugar)
            if (!l.publico && l.conceptos != null)
                foreach (var c in l.conceptos)
                    conceptos.Add(AConcepto(c, plantilla, l.id));
            return new Lugar(l.id, l.nombre, l.tipo, plantilla, new Coordenada(l.lat, l.lon), l.direccion, l.publico, conceptos);
        }

        static Concepto AConcepto(ConceptoJson c, Plantilla plantilla, string lugar)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.id)) throw new FormatException($"'{lugar}' tiene un concepto sin id");
            if (!DateTime.TryParseExact(c.fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                throw new FormatException($"'{lugar}/{c.id}' tiene una fecha de consulta no válida ('{c.fecha}')");
            // Sin página no hay precio de la web (NFR-6): PrecioWeb lo rechaza y el fichero se da por dañado
            var web = new PrecioWeb(AValor(c, lugar), c.url, fecha);
            return new Concepto(c.id, c.area, c.nombre, c.unidad, new FuentesPrecio(plantilla, web));
        }

        static ValorPrecio? AValor(ConceptoJson c, string lugar)
        {
            switch (c.forma)
            {
                case "fijo": return ValorPrecio.Fijo(c.cent);
                case "desde": return ValorPrecio.Desde(c.cent);
                case "consultar": return null;
                default: throw new FormatException($"'{lugar}/{c.id}' tiene una forma de precio desconocida ('{c.forma}')");
            }
        }

        static Plantilla APlantilla(string clave, string lugar)
        {
            switch (clave)
            {
                case "servicio": return Plantilla.ServicioLocal;
                case "hosteleria": return Plantilla.Hosteleria;
                case "ocio": return Plantilla.Ocio;
                default: throw new FormatException($"'{lugar}' tiene una plantilla desconocida ('{clave}')");
            }
        }

        // DTOs de JsonUtility: campos públicos con el mismo nombre que en el JSON
#pragma warning disable 0649
        [Serializable]
        class DatosJson
        {
            public LugarJson[] lugares;
        }

        [Serializable]
        class LugarJson
        {
            public string id;
            public string nombre;
            public string tipo;
            public string plantilla;
            public double lat;
            public double lon;
            public string direccion;
            public bool publico;
            public ConceptoJson[] conceptos;
        }

        [Serializable]
        class ConceptoJson
        {
            public string id;
            public string area;
            public string nombre;
            public string unidad;
            /// <summary>"fijo", "desde" o "consultar" (sin importe).</summary>
            public string forma;
            public int cent;
            public string url;
            /// <summary>Fecha de consulta de la web, "yyyy-MM-dd".</summary>
            public string fecha;
        }
#pragma warning restore 0649
    }
}
