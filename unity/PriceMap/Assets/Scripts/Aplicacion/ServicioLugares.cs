using System;
using System.Collections.Generic;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Lugares de Sanlúcar que puede tener la app y el resumen de su precio. El resumen se calcula
    /// cada vez que se pide (AD-3): nada de lo calculado se guarda.
    /// </summary>
    public class ServicioLugares
    {
        static readonly IReadOnlyList<Lugar> Ninguno = Array.Empty<Lugar>();

        readonly IRepositorioLugares repositorio;
        readonly Dictionary<string, Lugar> porId = new Dictionary<string, Lugar>();

        /// <summary>Lugares cargados; vacío antes de Cargar() o si la carga falló.</summary>
        public IReadOnlyList<Lugar> Lugares { get; private set; } = Ninguno;

        public ServicioLugares(IRepositorioLugares repositorio)
        {
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        /// <summary>Lee los lugares del repositorio. Si falla, la lista queda vacía y se devuelve el error.</summary>
        public Resultado<IReadOnlyList<Lugar>> Cargar()
        {
            var r = repositorio.Cargar();
            porId.Clear();
            if (!r.Ok || r.Valor == null)
            {
                Lugares = Ninguno;
                return Resultado<IReadOnlyList<Lugar>>.Fallo(string.IsNullOrEmpty(r.Error) ? "El repositorio no devolvió lugares." : r.Error, Ninguno);
            }
            // Lugares y Buscar ven lo mismo: sin nulos y con la primera aparición de cada id
            var filtrados = new List<Lugar>(r.Valor.Count);
            foreach (var l in r.Valor)
                if (l != null && !porId.ContainsKey(l.Id)) { porId.Add(l.Id, l); filtrados.Add(l); }
            Lugares = filtrados.AsReadOnly();
            return Resultado<IReadOnlyList<Lugar>>.Exito(Lugares);
        }

        /// <summary>El lugar con ese id, o null si no existe.</summary>
        public Lugar Buscar(string id) => id != null && porId.TryGetValue(id, out var l) ? l : null;

        /// <summary>Si el lugar tiene precio y el que se anuncia, calculado en este momento.</summary>
        public Resultado<ResumenLugar> Resumen(string id, DateTime ahora)
        {
            var lugar = Buscar(id);
            if (lugar == null) return Resultado<ResumenLugar>.Fallo($"No hay ningún lugar con id '{id}'.", ResumenLugar.SinPrecio);
            return Resultado<ResumenLugar>.Exito(ReglaResumen.Resumir(lugar, ahora));
        }
    }
}
