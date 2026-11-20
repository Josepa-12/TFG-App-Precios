using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Rutas
{
    /// <summary>
    /// Lee la respuesta de <c>/route</c> de Valhalla (historia 2.6). Pura: no sale a la red. La línea de cada tramo
    /// es una polilínea de precisión 6; las longitudes van en kilómetros (o millas si así lo dice <c>units</c>) y
    /// los tiempos en segundos. Cada maniobra empieza en el punto <c>begin_shape_index</c> de su tramo y trae su
    /// texto (<c>instruction</c>) en el idioma pedido.
    /// </summary>
    internal static class LecturaValhalla
    {
        [Serializable] class Respuesta { public Viaje trip; public string error; }
        [Serializable] class Viaje { public Tramo[] legs; public Resumen summary; public string units; }
        [Serializable] class Tramo { public ManiobraJson[] maneuvers; public string shape; public Resumen summary; }
        [Serializable] class Resumen { public double length; public double time; }
        [Serializable] class ManiobraJson { public int type; public string instruction; public double length; public int begin_shape_index; }

        const double MetrosPorMilla = 1609.344;

        public static Resultado<Ruta> Leer(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Resultado<Ruta>.Fallo("Respuesta vacía de Valhalla.");
            Respuesta r;
            try { r = JsonUtility.FromJson<Respuesta>(json); }
            catch (Exception e) { return Resultado<Ruta>.Fallo("Respuesta de Valhalla ilegible: " + e.Message); }
            if (r == null) return Resultado<Ruta>.Fallo("Respuesta de Valhalla ilegible.");
            var legs = r.trip?.legs;
            if (legs == null || legs.Length == 0)
                return Resultado<Ruta>.Fallo("Valhalla no da ruta" + (string.IsNullOrWhiteSpace(r.error) ? "." : ": " + r.error));

            double porUnidad = string.Equals(r.trip.units, "miles", StringComparison.OrdinalIgnoreCase) ? MetrosPorMilla : 1000.0;
            var puntos = new List<Coordenada>();
            var maniobras = new List<Maniobra>();
            double metros = 0, segundos = 0;
            try
            {
                foreach (var tramo in legs)
                {
                    if (tramo == null) continue;
                    var linea = Polilinea.Decodificar(tramo.shape, 6);
                    // El primer punto de un tramo repite el último del anterior
                    int inicio = puntos.Count > 0 && linea.Count > 0 ? 1 : 0;
                    int desplazamiento = puntos.Count - inicio;
                    for (int i = inicio; i < linea.Count; i++) puntos.Add(linea[i]);
                    if (tramo.summary != null) { metros += tramo.summary.length * porUnidad; segundos += tramo.summary.time; }
                    if (tramo.maneuvers == null) continue;
                    foreach (var m in tramo.maneuvers)
                    {
                        if (m == null) continue;
                        var donde = PeticionRutas.Punto(puntos, desplazamiento + m.begin_shape_index, default);
                        maniobras.Add(new Maniobra(Tipo(m.type), m.instruction, donde, m.length * porUnidad));
                    }
                }
            }
            catch (FormatException e)
            {
                return Resultado<Ruta>.Fallo("Línea de Valhalla rota: " + e.Message);
            }
            if (puntos.Count < 2) return Resultado<Ruta>.Fallo("La ruta de Valhalla no tiene línea.");
            // El resumen del viaje manda si lo trae
            var resumen = r.trip.summary;
            if (resumen != null && (resumen.length > 0 || resumen.time > 0))
            {
                metros = resumen.length * porUnidad;
                segundos = resumen.time;
            }
            return Resultado<Ruta>.Exito(new Ruta(puntos.AsReadOnly(), metros, segundos, maniobras.AsReadOnly()));
        }

        /// <summary>
        /// Tipo de maniobra de Valhalla (su número, ver la API de Valhalla: 1 Start ... 27 RoundaboutExit, 37 y 38
        /// MergeRight/Left) en el tipo común.
        /// </summary>
        public static TipoManiobra Tipo(int tipo)
        {
            switch (tipo)
            {
                case 1: case 2: case 3: return TipoManiobra.Salida;
                case 4: case 5: case 6: return TipoManiobra.Llegada;
                case 7: case 8: case 17: case 22: return TipoManiobra.Recto;
                case 9: case 18: case 20: case 23: return TipoManiobra.LigeroDerecha;
                case 10: return TipoManiobra.Derecha;
                case 11: return TipoManiobra.CerradoDerecha;
                case 12: case 13: return TipoManiobra.CambioSentido;
                case 14: return TipoManiobra.CerradoIzquierda;
                case 15: return TipoManiobra.Izquierda;
                case 16: case 19: case 21: case 24: return TipoManiobra.LigeroIzquierda;
                case 25: case 37: case 38: return TipoManiobra.Incorporacion;
                case 26: return TipoManiobra.Rotonda;
                case 27: return TipoManiobra.SalidaRotonda;
                default: return TipoManiobra.Otra;
            }
        }
    }
}
