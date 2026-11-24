using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase.Firestore;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>Valores de los campos entre la app (IAlmacen) y Firestore.</summary>
    internal static class ValoresFirestore
    {
        public static Dictionary<string, object> Mapa(IEnumerable<KeyValuePair<string, object>> campos) =>
            campos.ToDictionary(c => c.Key, c => AFirestore(c.Value));

        public static Dictionary<string, object> Campos(IEnumerable<KeyValuePair<string, object>> mapa) =>
            mapa.ToDictionary(c => c.Key, c => DeFirestore(c.Value));

        public static object AFirestore(object valor) => valor switch
        {
            null => null,
            string _ => valor,
            bool _ or int _ or long _ or float _ or double _ => valor,
            HoraServidor _ => FieldValue.ServerTimestamp,
            DateTime d => Timestamp.FromDateTime(d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime()),
            IDictionary mapa => mapa.Keys.Cast<object>().ToDictionary(
                c => c as string ?? throw new ArgumentException("Mapa con claves que no son texto"), c => AFirestore(mapa[c])),
            IEnumerable<KeyValuePair<string, object>> mapa => Mapa(mapa),
            IEnumerable lista => lista.Cast<object>().Select(DeLista).ToList(),
            _ => throw new ArgumentException("Valor que Firestore no guarda: " + valor.GetType().Name),
        };

        // Firestore no admite, dentro de una lista, otra lista ni la hora del servidor
        static object DeLista(object valor)
        {
            var v = AFirestore(valor);
            if (valor is HoraServidor || v is List<object>)
                throw new ArgumentException("Valor que Firestore no guarda en una lista: " + valor.GetType().Name);
            return v;
        }

        public static object DeFirestore(object valor) => valor switch
        {
            string _ => valor,
            bool _ or long _ or double _ => valor,
            Timestamp t => t.ToDateTime(),
            IEnumerable<KeyValuePair<string, object>> mapa => Campos(mapa),
            IEnumerable lista => lista.Cast<object>().Select(DeFirestore).ToList(),
            _ => null, // null, o tipos propios de Firestore que la app no escribe (GeoPoint, referencias…): no salen del adaptador
        };
    }
}
