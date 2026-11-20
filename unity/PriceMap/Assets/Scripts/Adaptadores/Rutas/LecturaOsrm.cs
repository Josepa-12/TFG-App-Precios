using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Lee la respuesta de <c>/route/v1</c> de OSRM (historia 2.6). Pura: no sale a la red. De las alternativas se
    /// queda con la de menor distancia (la más corta, como Valhalla con <c>shortest</c>). Línea en polyline6,
    /// distancias en metros y duraciones en segundos. Las maniobras llegan sin texto: solo su tipo, con el
    /// modificador, y su punto (<c>location</c>, lon/lat).
    /// </summary>
    internal static class LecturaOsrm
    {
        [Serializable] class Respuesta { public string code; public string message; public RutaJson[] routes; }
        [Serializable] class RutaJson { public string geometry; public double distance; public double duration; public TramoJson[] legs; }
        [Serializable] class TramoJson { public Paso[] steps; }
        [Serializable] class Paso { public double distance; public ManiobraJson maneuver; }
        [Serializable] class ManiobraJson { public string type; public string modifier; public double[] location; }

        public static Resultado<Ruta> Leer(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Resultado<Ruta>.Fallo("Respuesta vacía de OSRM.");
            Respuesta r;
            try { r = JsonUtility.FromJson<Respuesta>(json); }
            catch (Exception e) { return Resultado<Ruta>.Fallo("Respuesta de OSRM ilegible: " + e.Message); }
            if (r == null) return Resultado<Ruta>.Fallo("Respuesta de OSRM ilegible.");
            if (!string.Equals(r.code, "Ok", StringComparison.Ordinal))
                return Resultado<Ruta>.Fallo("OSRM no da ruta (" + (string.IsNullOrWhiteSpace(r.code) ? "sin código" : r.code)
                                             + (string.IsNullOrWhiteSpace(r.message) ? ")." : "): " + r.message));
            if (r.routes == null || r.routes.Length == 0) return Resultado<Ruta>.Fallo("OSRM no da ninguna ruta.");

            // La alternativa más corta en distancia que se pueda leer
            RutaJson elegida = null;
            IReadOnlyList<Coordenada> linea = null;
            string error = null;
            foreach (var ruta in r.routes)
            {
                if (ruta == null || (elegida != null && ruta.distance >= elegida.distance)) continue;
                try
                {
                    var puntos = Polilinea.Decodificar(ruta.geometry, 6);
                    if (puntos.Count < 2) { error = "una alternativa de OSRM no tiene línea."; continue; }
                    elegida = ruta;
                    linea = puntos;
                }
                catch (FormatException e)
                {
                    error = "línea de OSRM rota: " + e.Message;
                }
            }
            if (elegida == null) return Resultado<Ruta>.Fallo("OSRM no da ninguna ruta legible" + (error != null ? ": " + error : "."));

            var maniobras = new List<Maniobra>();
            if (elegida.legs != null)
                foreach (var tramo in elegida.legs)
                {
                    if (tramo?.steps == null) continue;
                    foreach (var paso in tramo.steps)
                    {
                        var m = paso?.maneuver;
                        if (m == null) continue;
                        var donde = m.location != null && m.location.Length >= 2 ? new Coordenada(m.location[1], m.location[0]) : linea[0];
                        maniobras.Add(new Maniobra(Tipo(m.type, m.modifier), "", donde, paso.distance));
                    }
                }
            return Resultado<Ruta>.Exito(new Ruta(linea, elegida.distance, elegida.duration, maniobras.AsReadOnly()));
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
