using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Selector de cuentas de Google (Credential Manager) con el plugin com.pricemap.cuenta.PuenteCuenta: se pide y se
    /// pregunta su estado fotograma a fotograma. Con el selector delante Unity está en pausa: se sigue al volver. En el editor
    /// y en otras plataformas no lo hay.
    /// </summary>
    internal sealed class SelectorGoogleAndroid : ISelectorGoogle
    {
        // Los mismos números que PuenteCuenta
        internal const int EnCurso = 0, Hecha = 1, Cancelada = 2, NoDisponible = 4;

        readonly IPuenteCuenta puente; // null: no lo hay
        readonly Func<CancellationToken, Task> siguienteFotograma;

        public SelectorGoogleAndroid() : this(
#if UNITY_ANDROID && !UNITY_EDITOR
            new PuenteCuentaAndroid(),
#else
            null,
#endif
            Fotograma) { }

        /// <param name="siguienteFotograma">Espera un fotograma; al cancelar lanza OperationCanceledException.</param>
        internal SelectorGoogleAndroid(IPuenteCuenta puente, Func<CancellationToken, Task> siguienteFotograma)
        {
            this.puente = puente;
            this.siguienteFotograma = siguienteFotograma;
        }

        static async Task Fotograma(CancellationToken ct) => await Awaitable.NextFrameAsync(ct);

        public async Task<RespuestaGoogle> Pedir(CancellationToken ct)
        {
            if (puente == null) return new RespuestaGoogle(EstadoGoogle.NoDisponible, motivo: "sin Android");
            int id = puente.Pedir();
            bool cortar = true;
            try
            {
                int estado;
                while ((estado = puente.Estado(id)) == EnCurso) await siguienteFotograma(ct);
                cortar = false;
                switch (estado)
                {
                    case Hecha: return new RespuestaGoogle(EstadoGoogle.Hecha, puente.Token(id));
                    case Cancelada: return new RespuestaGoogle(EstadoGoogle.Cancelada);
                    default:
                        return new RespuestaGoogle(estado == NoDisponible ? EstadoGoogle.NoDisponible : EstadoGoogle.Fallo,
                            motivo: puente.Motivo(id));
                }
            }
            finally
            {
                try
                {
                    if (cortar) puente.Cancelar(id);
                    puente.Liberar(id);
                }
                catch (Exception e) { Debug.LogWarning($"PriceMap: no se pudo cerrar la petición a Google ({e.GetType().Name})."); }
            }
        }
    }
}
