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
    /// texto (<c>instruction</c>) en el idioma pedido y el corto para la voz
    /// (<c>verbal_succinct_transition_instruction</c>, historia 2.8). Las alternativas llegan en <c>alternates</c>, cada
    /// una con su viaje de la misma forma (parte 5a de «Cómo llegar»).
    /// </summary>
    internal static class LecturaValhalla
    {
        [Serializable] class Respuesta { public Viaje trip; public Alternativa[] alternates; public string error; }
        [Serializable] class Alternativa { public Viaje trip; }
        [Serializable] class Viaje { public Tramo[] legs; public Resumen summary; public string units; }
        [Serializable] class Tramo { public ManiobraJson[] maneuvers; public string shape; public Resumen summary; }
        [Serializable] class Resumen { public double length; public double time; }
        [Serializable] class ManiobraJson { public int type; public string instruction; public string verbal_succinct_transition_instruction; public double length; public int begin_shape_index; }

        const double MetrosPorMilla = 1609.344;

        /// <summary>La ruta del viaje (sin las alternativas).</summary>
        public static Resultado<Ruta> Leer(string json)
        {
            var r = Abrir(json);
            return r.Ok ? LeerViaje(r.Valor.trip, r.Valor.error) : Resultado<Ruta>.Fallo(r.Error);
        }

        /// <summary>
        /// El viaje y sus alternativas en el orden en que llegan; las que no se pueden leer se saltan. Fallo (el del viaje)
        /// si no queda ninguna.
        /// </summary>
        public static Resultado<IReadOnlyList<Ruta>> LeerTodas(string json)
        {
            var r = Abrir(json);
            if (!r.Ok) return Resultado<IReadOnlyList<Ruta>>.Fallo(r.Error);
            var rutas = new List<Ruta>();
            var viaje = LeerViaje(r.Valor.trip, r.Valor.error);
            if (viaje.Ok) rutas.Add(viaje.Valor);
            if (r.Valor.alternates != null)
                foreach (var a in r.Valor.alternates)
                {
                    var alternativa = LeerViaje(a?.trip, null);
                    if (alternativa.Ok) rutas.Add(alternativa.Valor);
                }
            return rutas.Count > 0
                ? Resultado<IReadOnlyList<Ruta>>.Exito(rutas.AsReadOnly())
                : Resultado<IReadOnlyList<Ruta>>.Fallo(viaje.Error);
        }

        static Resultado<Respuesta> Abrir(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Resultado<Respuesta>.Fallo("Respuesta vacía de Valhalla.");
            Respuesta r;
            try { r = JsonUtility.FromJson<Respuesta>(json); }
            catch (Exception e) { return Resultado<Respuesta>.Fallo("Respuesta de Valhalla ilegible: " + e.Message); }
            return r != null ? Resultado<Respuesta>.Exito(r) : Resultado<Respuesta>.Fallo("Respuesta de Valhalla ilegible.");
        }

        static Resultado<Ruta> LeerViaje(Viaje viaje, string error)
        {
            var legs = viaje?.legs;
            if (legs == null || legs.Length == 0)
                return Resultado<Ruta>.Fallo("Valhalla no da ruta" + (string.IsNullOrWhiteSpace(error) ? "." : ": " + error));

            double porUnidad = string.Equals(viaje.units, "miles", StringComparison.OrdinalIgnoreCase) ? MetrosPorMilla : 1000.0;
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
                        maniobras.Add(new Maniobra(Tipo(m.type), m.instruction, donde, m.length * porUnidad,
                            m.verbal_succinct_transition_instruction));
                    }
                }
            }
            catch (FormatException e)
            {
                return Resultado<Ruta>.Fallo("Línea de Valhalla rota: " + e.Message);
            }
            if (puntos.Count < 2) return Resultado<Ruta>.Fallo("La ruta de Valhalla no tiene línea.");
            // El resumen del viaje manda si lo trae
            var resumen = viaje.summary;
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
