using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Lee la respuesta de <c>/route/v1</c> de OSRM (historia 2.6). Pura: no sale a la red. La ruta de siempre es
    /// la de menos tiempo de las alternativas (parte 5a de «Cómo llegar»). Línea en polyline6, distancias en metros y
    /// duraciones en segundos. Las maniobras llegan sin texto: solo su tipo, con el
    /// modificador, y su punto (<c>location</c>, lon/lat).
    /// </summary>
    internal static class LecturaOsrm
    {
        [Serializable] class Respuesta { public string code; public string message; public RutaJson[] routes; }
        [Serializable] class RutaJson { public string geometry; public double distance; public double duration; public TramoJson[] legs; }
        [Serializable] class TramoJson { public Paso[] steps; }
        [Serializable] class Paso { public double distance; public ManiobraJson maneuver; }
        [Serializable] class ManiobraJson { public string type; public string modifier; public double[] location; }

        /// <summary>La de menos tiempo de las alternativas que se pueden leer.</summary>
        public static Resultado<Ruta> Leer(string json)
        {
            var r = LeerTodas(json);
            return r.Ok ? Resultado<Ruta>.Exito(ReglaAlternativas.Juntar(r.Valor)[0]) : Resultado<Ruta>.Fallo(r.Error);
        }

        /// <summary>Las alternativas en el orden en que llegan; las que no se pueden leer se saltan. Fallo si no queda ninguna.</summary>
        public static Resultado<IReadOnlyList<Ruta>> LeerTodas(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Fallo("Respuesta vacía de OSRM.");
            Respuesta r;
            try { r = JsonUtility.FromJson<Respuesta>(json); }
            catch (Exception e) { return Fallo("Respuesta de OSRM ilegible: " + e.Message); }
            if (r == null) return Fallo("Respuesta de OSRM ilegible.");
            if (!string.Equals(r.code, "Ok", StringComparison.Ordinal))
                return Fallo("OSRM no da ruta (" + (string.IsNullOrWhiteSpace(r.code) ? "sin código" : r.code)
                             + (string.IsNullOrWhiteSpace(r.message) ? ")." : "): " + r.message));
            if (r.routes == null || r.routes.Length == 0) return Fallo("OSRM no da ninguna ruta.");

            var rutas = new List<Ruta>();
            string error = null;
            foreach (var ruta in r.routes)
            {
                if (ruta == null) continue;
                try
                {
                    var linea = Polilinea.Decodificar(ruta.geometry, 6);
                    if (linea.Count < 2) { error = "una alternativa de OSRM no tiene línea."; continue; }
                    rutas.Add(new Ruta(linea, ruta.distance, ruta.duration, Maniobras(ruta, linea[0])));
                }
                catch (FormatException e)
                {
                    error = "línea de OSRM rota: " + e.Message;
                }
            }
            return rutas.Count > 0
                ? Resultado<IReadOnlyList<Ruta>>.Exito(rutas.AsReadOnly())
                : Fallo("OSRM no da ninguna ruta legible" + (error != null ? ": " + error : "."));
        }

        static Resultado<IReadOnlyList<Ruta>> Fallo(string error) => Resultado<IReadOnlyList<Ruta>>.Fallo(error);

        static IReadOnlyList<Maniobra> Maniobras(RutaJson ruta, Coordenada respaldo)
        {
            var maniobras = new List<Maniobra>();
            if (ruta.legs == null) return maniobras.AsReadOnly();
            foreach (var tramo in ruta.legs)
            {
                if (tramo?.steps == null) continue;
                foreach (var paso in tramo.steps)
                {
                    var m = paso?.maneuver;
                    if (m == null) continue;
                    var donde = m.location != null && m.location.Length >= 2 ? new Coordenada(m.location[1], m.location[0]) : respaldo;
                    maniobras.Add(new Maniobra(Tipo(m.type, m.modifier), "", donde, paso.distance));
                }
            }
            return maniobras.AsReadOnly();
        }

        /// <summary>Tipo y modificador de una maniobra de OSRM en el tipo común.</summary>
        public static TipoManiobra Tipo(string tipo, string modificador)
        {
            switch ((tipo ?? "").Trim().ToLowerInvariant())
            {
                case "depart": return TipoManiobra.Salida;
                case "arrive": return TipoManiobra.Llegada;
                case "merge": return TipoManiobra.Incorporacion;
                case "roundabout":
                case "rotary":
                case "roundabout turn": return TipoManiobra.Rotonda;
                case "exit roundabout":
                case "exit rotary": return TipoManiobra.SalidaRotonda;
            }
            switch ((modificador ?? "").Trim().ToLowerInvariant())
            {
                case "uturn": return TipoManiobra.CambioSentido;
                case "sharp right": return TipoManiobra.CerradoDerecha;
                case "right": return TipoManiobra.Derecha;
                case "slight right": return TipoManiobra.LigeroDerecha;
                case "straight": return TipoManiobra.Recto;
                case "slight left": return TipoManiobra.LigeroIzquierda;
                case "left": return TipoManiobra.Izquierda;
                case "sharp left": return TipoManiobra.CerradoIzquierda;
                case "": return string.Equals(tipo, "new name", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(tipo, "continue", StringComparison.OrdinalIgnoreCase)
                    ? TipoManiobra.Recto
                    : TipoManiobra.Otra;
                default: return TipoManiobra.Otra;
            }
        }
    }
}
