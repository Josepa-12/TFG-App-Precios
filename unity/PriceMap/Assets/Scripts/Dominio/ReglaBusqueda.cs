using System;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Si un nombre coincide con lo que se escribe en el buscador y cuánto (historia 2.3, FR14). Todo se compara
    /// normalizado (minúsculas, sin tildes y sin signos, como los repetidos de la 1.6): "jose" encuentra "José".
    /// Coincide si cada palabra de la consulta es el principio de alguna palabra del nombre o, si no, de la
    /// dirección. Puntuación: 3 nombre exacto, 2 el nombre empieza por la consulta, 1 coincide de otra forma; 0 no coincide.
    /// </summary>
    public static class ReglaBusqueda
    {
        public const int NoCoincide = 0, Contiene = 1, EmpiezaIgual = 2, Exacto = 3;

        static readonly char[] Espacio = { ' ' };

        /// <summary>Minúsculas, sin tildes y sin signos ("¡Clínica  Areas!" → "clinica areas"); vacío si no hay texto.</summary>
        public static string Normalizar(string texto) => ReglaPuntosInteres.NombreNormalizado(texto);

        /// <summary>Palabras de un texto ya normalizado (sin vacías).</summary>
        public static string[] Palabras(string normalizado) =>
            string.IsNullOrEmpty(normalizado) ? Array.Empty<string>() : normalizado.Split(Espacio, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>Puntuación del nombre (y la dirección, si la hay) para la consulta tal como se escribe.</summary>
        public static int Puntuar(string consulta, string nombre, string direccion = null)
        {
            string q = Normalizar(consulta);
            return PuntuarNormalizada(q, Palabras(q), nombre, direccion);
        }

        /// <summary>
        /// Igual que Puntuar, con la consulta ya normalizada y partida en palabras (se hace una vez por búsqueda,
        /// no una vez por cada nombre).
        /// </summary>
        public static int PuntuarNormalizada(string consulta, string[] palabras, string nombre, string direccion = null)
        {
            if (string.IsNullOrEmpty(consulta) || palabras == null || palabras.Length == 0) return NoCoincide;
            string n = Normalizar(nombre);
            if (n.Length > 0)
            {
                if (n == consulta) return Exacto;
                if (n.StartsWith(consulta, StringComparison.Ordinal)) return EmpiezaIgual;
                if (TodasEmpiezanPalabra(palabras, Palabras(n))) return Contiene;
            }
            if (string.IsNullOrEmpty(direccion)) return NoCoincide;
            return TodasEmpiezanPalabra(palabras, Palabras(Normalizar(direccion))) ? Contiene : NoCoincide;
        }

        // Cada palabra de la consulta es el principio de alguna palabra del texto
        static bool TodasEmpiezanPalabra(string[] consulta, string[] texto)
        {
            if (texto.Length == 0) return false;
            foreach (var q in consulta)
            {
                bool hay = false;
                foreach (var t in texto)
                    if (t.StartsWith(q, StringComparison.Ordinal)) { hay = true; break; }
                if (!hay) return false;
            }
            return true;
        }
    }
}
