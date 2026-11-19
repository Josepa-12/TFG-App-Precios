using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Direcciones del buscador (historia 2.3, parte 2; FR14). Al teclear (Pedir) espera 350 ms desde la última tecla
    /// y, con al menos 3 caracteres normalizados, pregunta al geocodificador. Cada petición cancela la espera y la
    /// petición anteriores: solo se avisa (Cambiado) de la respuesta de la última consulta, nunca de una vieja.
    /// "Buscar como dirección" (PedirYa) pregunta ya, sin espera ni mínimo. Los fallos llegan como Resultado.Fallo;
    /// nada se guarda (FR17). Se usa desde el hilo principal (la UI): las continuaciones vuelven a él.
    /// </summary>
    public sealed class BuscadorDirecciones
    {
        /// <summary>Espera desde la última tecla antes de preguntar.</summary>
        public static readonly TimeSpan Espera = TimeSpan.FromMilliseconds(350);
        /// <summary>Caracteres normalizados (ReglaBusqueda.Normalizar) que hacen falta para preguntar al teclear.</summary>
        public const int MinimoCaracteres = 3;
        /// <summary>Direcciones que se muestran como mucho.</summary>
        public const int MaximoDirecciones = 8;

        static readonly IReadOnlyList<ResultadoBusqueda> Ninguna = Array.Empty<ResultadoBusqueda>();

        readonly IGeocodificador geocodificador;
        readonly Func<TimeSpan, CancellationToken, Task> esperar;
        CancellationTokenSource actual; // la petición (o espera) en curso; null si no hay ninguna

        /// <summary>
        /// Respuesta para esa consulta (el texto tal como se pidió): las direcciones como resultados, con su distancia
        /// al punto de cercanía, o el fallo (con una lista vacía).
        /// </summary>
        public event Action<string, Resultado<IReadOnlyList<ResultadoBusqueda>>> Cambiado;

        /// <param name="esperar">Espera cancelable (en la app, Task.Delay); las pruebas pasan una que no espera.</param>
        public BuscadorDirecciones(IGeocodificador geocodificador, Func<TimeSpan, CancellationToken, Task> esperar)
        {
            this.geocodificador = geocodificador ?? throw new ArgumentNullException(nameof(geocodificador));
            this.esperar = esperar ?? throw new ArgumentNullException(nameof(esperar));
        }

        /// <summary>Hay una espera o una petición en curso.</summary>
        public bool Pendiente => actual != null;

        /// <summary>La consulta llega al mínimo para preguntar al teclear.</summary>
        public static bool BastaParaPedir(string consulta) => ReglaBusqueda.Normalizar(consulta).Length >= MinimoCaracteres;

        /// <summary>
        /// Al teclear: cancela lo anterior y, si la consulta llega al mínimo, pregunta tras la espera. Devuelve si
        /// se ha programado la petición.
        /// </summary>
        public bool Pedir(string consulta, Coordenada cerca)
        {
            Cancelar();
            if (!BastaParaPedir(consulta)) return false;
            Lanzar(consulta, cerca, conEspera: true);
            return true;
        }

        /// <summary>
        /// "Buscar como dirección": cancela lo anterior y pregunta ya, sin espera ni mínimo (solo hace falta alguna
        /// letra o número). Devuelve si se ha lanzado la petición.
        /// </summary>
        public bool PedirYa(string consulta, Coordenada cerca)
        {
            Cancelar();
            if (ReglaBusqueda.Normalizar(consulta).Length == 0) return false;
            Lanzar(consulta, cerca, conEspera: false);
            return true;
        }

        /// <summary>Cancela la espera y la petición en curso: de ellas no llegará ningún aviso.</summary>
        public void Cancelar()
        {
            if (actual == null) return;
            var cts = actual;
            actual = null;
            cts.Cancel(); // sin Dispose: la tarea cancelada aún puede mirar su token (sin temporizadores, no hace falta)
        }

        void Lanzar(string consulta, Coordenada cerca, bool conEspera)
        {
            var cts = new CancellationTokenSource();
            actual = cts;
            _ = Ejecutar(consulta, cerca, conEspera, cts, cts.Token);
        }

        async Task Ejecutar(string consulta, Coordenada cerca, bool conEspera, CancellationTokenSource cts, CancellationToken ct)
        {
            Resultado<IReadOnlyList<Direccion>> r;
            try
            {
                if (conEspera) await esperar(Espera, ct);
                if (ct.IsCancellationRequested) return;
                r = await geocodificador.Buscar(consulta, cerca, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                // El puerto no debería lanzar; si lo hace, la UI lo recibe como un fallo más
                r = Resultado<IReadOnlyList<Direccion>>.Fallo(string.IsNullOrWhiteSpace(e.Message) ? e.GetType().Name : e.Message);
            }

            // Una respuesta tardía (de un texto viejo o cancelada) no se pinta nunca
            if (ct.IsCancellationRequested || !ReferenceEquals(actual, cts)) return;
            actual = null;

            Resultado<IReadOnlyList<ResultadoBusqueda>> aviso;
            if (!r.Ok) aviso = Resultado<IReadOnlyList<ResultadoBusqueda>>.Fallo(r.Error, Ninguna);
            else
            {
                var lista = new List<ResultadoBusqueda>();
                if (r.Valor != null)
                    foreach (var d in r.Valor)
                    {
                        if (d == null) continue;
                        if (lista.Count >= MaximoDirecciones) break;
                        lista.Add(ResultadoBusqueda.DeDireccion(d, cerca.DistanciaMetros(d.Posicion)));
                    }
                aviso = Resultado<IReadOnlyList<ResultadoBusqueda>>.Exito(lista.AsReadOnly());
            }
            Cambiado?.Invoke(consulta, aviso);
        }
    }
}
