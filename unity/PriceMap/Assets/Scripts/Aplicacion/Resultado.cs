using System;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Resultado de una operación entre capas: o un valor, o un error explicado (convención del proyecto).
    /// Los fallos esperables (fichero ausente, datos dañados) se devuelven así en vez de lanzar excepciones.
    /// </summary>
    public readonly struct Resultado<T>
    {
        public bool Ok { get; }
        /// <summary>El valor si Ok; si no, el que se haya dado como sustituto (p. ej. una lista vacía).</summary>
        public T Valor { get; }
        /// <summary>Explicación del fallo; vacía si Ok.</summary>
        public string Error => error ?? ""; // también en default(Resultado<T>)

        readonly string error;

        Resultado(bool ok, T valor, string error)
        {
            Ok = ok;
            Valor = valor;
            this.error = error;
        }

        public static Resultado<T> Exito(T valor) => new Resultado<T>(true, valor, null);

        public static Resultado<T> Fallo(string error, T sustituto = default)
        {
            if (string.IsNullOrWhiteSpace(error)) throw new ArgumentException("Un fallo necesita su explicación.", nameof(error));
            return new Resultado<T>(false, sustituto, error);
        }
    }
}
