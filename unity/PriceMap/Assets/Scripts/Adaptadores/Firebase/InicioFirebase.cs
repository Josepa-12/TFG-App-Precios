using System;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Inicio de Firebase (historia 3.2): se lanza desde el Arranque sin esperarlo. Si falla (sin google-services.json, sin
    /// servicios de Google…), queda NoDisponible y la app sigue sin cuenta ni servidor.
    /// </summary>
    public sealed class InicioFirebase : IServidor
    {
        readonly Func<Task<string>> preparar;
        bool iniciado;

        public InicioFirebase() : this(() => PreparacionFirebase.Preparar()) { }

        /// <param name="preparar">Prepara Firebase; null si ha ido bien y, si no, el motivo.</param>
        internal InicioFirebase(Func<Task<string>> preparar) =>
            this.preparar = preparar ?? throw new ArgumentNullException(nameof(preparar));

        public EstadoServidor Estado { get; private set; } = EstadoServidor.Iniciando;
        public event Action Cambiado;

        /// <summary>Empieza y vuelve al momento; acaba en el hilo que lo llama (el principal). Solo cuenta la primera vez.</summary>
        public async void Iniciar()
        {
            if (iniciado) return;
            iniciado = true;

            string motivo;
            try { motivo = await preparar(); }
            catch (Exception e) { motivo = e.GetType().Name + ": " + e.Message; }

            if (motivo == null) Debug.Log("PriceMap: Firebase listo");
            else Debug.LogWarning("PriceMap: Firebase no disponible; la app sigue sin cuenta ni servidor (" + motivo + ").");
            Estado = motivo == null ? EstadoServidor.Listo : EstadoServidor.NoDisponible;
            Cambiado?.Invoke();
        }
    }
}
