using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Calles guardadas del buscador (spec buscar-calles-guardadas): al teclear, con al menos 3 caracteres normalizados,
    /// busca ya (sin espera ni red) en las calles de las teselas guardadas. Cada búsqueda cancela la anterior: solo se
    /// avisa (Cambiado) de la última, nunca de una vieja. Se usa desde el hilo principal: las continuaciones vuelven a él.
    /// </summary>
    public sealed class BuscadorCalles
    {
        static readonly IReadOnlyList<ResultadoBusqueda> Ninguna = Array.Empty<ResultadoBusqueda>();

        readonly ICallesGuardadas calles;
        CancellationTokenSource actual; // la búsqueda en curso; null si no hay ninguna

        /// <summary>Las calles para esa consulta (el texto tal como se pidió), con su distancia, o el fallo (lista vacía).</summary>
        public event Action<string, Resultado<IReadOnlyList<ResultadoBusqueda>>> Cambiado;

        public BuscadorCalles(ICallesGuardadas calles)
        {
            this.calles = calles ?? throw new ArgumentNullException(nameof(calles));
        }

        /// <summary>Hay una búsqueda en curso.</summary>
        public bool Pendiente => actual != null;

        /// <summary>Cancela lo anterior y, si la consulta llega al mínimo, busca. Devuelve si se ha lanzado.</summary>
        public bool Pedir(string consulta, Coordenada cerca)
        {
            Cancelar();
            if (!BuscadorDirecciones.BastaParaPedir(consulta)) return false;
            var cts = new CancellationTokenSource();
            actual = cts;
            _ = Ejecutar(consulta, cerca, cts, cts.Token);
            return true;
        }

        /// <summary>Cancela la búsqueda en curso: de ella no llegará ningún aviso.</summary>
        public void Cancelar()
        {
            if (actual == null) return;
            var cts = actual;
            actual = null;
            cts.Cancel(); // sin Dispose: la tarea cancelada aún puede mirar su token
        }

        async Task Ejecutar(string consulta, Coordenada cerca, CancellationTokenSource cts, CancellationToken ct)
        {
            Resultado<IReadOnlyList<Direccion>> r;
            try
            {
                r = await calles.Buscar(consulta, cerca, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                r = Resultado<IReadOnlyList<Direccion>>.Fallo(string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message);
            }
            if (ct.IsCancellationRequested || !ReferenceEquals(actual, cts)) return; // una búsqueda vieja no se pinta nunca
            actual = null;

            if (!r.Ok)
            {
                Cambiado?.Invoke(consulta, Resultado<IReadOnlyList<ResultadoBusqueda>>.Fallo(r.Error, Ninguna));
                return;
            }
            var lista = new List<ResultadoBusqueda>();
            if (r.Valor != null)
                foreach (var d in r.Valor)
                    if (d != null) lista.Add(ResultadoBusqueda.DeDireccion(d, cerca.DistanciaMetros(d.Posicion)));
            Cambiado?.Invoke(consulta, Resultado<IReadOnlyList<ResultadoBusqueda>>.Exito(lista.AsReadOnly()));
        }
    }
}
