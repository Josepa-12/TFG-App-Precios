using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Indicaciones por voz al navegar (historia 2.8, parte 1; FR21). Escucha a ServicioNavegacion: al empezar una ruta
    /// crea la ReglaVoz de su modo de viaje; con cada estado nuevo (Actualizado) dice lo que la regla mande según el modo
    /// de voz; al recalcular empieza de cero con la ruta nueva; al terminar calla. El modo (todo, lo importante o nada) se
    /// guarda en IPreferencias con la clave "voz" (sin valor: todo). Se usa desde el hilo principal (la UI).
    /// </summary>
    public sealed class ServicioVoz
    {
        public const string ClavePreferencia = "voz";

        readonly ServicioNavegacion navegacion;
        readonly IVoz voz;
        readonly IPreferencias preferencias;
        ReglaVoz regla; // null sin ruta

        /// <summary>Lo que dice la voz; el guardado o, la primera vez, todas las indicaciones.</summary>
        public ModoVoz Modo { get; private set; }

        /// <summary>El usuario ha elegido otro modo.</summary>
        public event Action<ModoVoz> ModoCambiado;

        public ServicioVoz(ServicioNavegacion navegacion, IVoz voz, IPreferencias preferencias)
        {
            this.navegacion = navegacion ?? throw new ArgumentNullException(nameof(navegacion));
            this.voz = voz ?? throw new ArgumentNullException(nameof(voz));
            this.preferencias = preferencias ?? throw new ArgumentNullException(nameof(preferencias));
            Modo = ReglaVoz.DeClave(preferencias.Leer(ClavePreferencia));
            navegacion.RutaIniciada += _ => regla = new ReglaVoz(this.navegacion.Modo);
            navegacion.Actualizado += AlActualizar;
            navegacion.RutaCambiada += () => regla?.Reiniciar();
            navegacion.RutaTerminada += _ => { regla = null; Callar(); };
        }

        /// <summary>Cambia el modo y lo guarda. Con «sin voz», calla también lo que estuviera diciendo.</summary>
        public void Elegir(ModoVoz modo)
        {
            if (modo == Modo) return;
            Modo = modo;
            preferencias.Guardar(ClavePreferencia, ReglaVoz.AClave(modo));
            if (modo == ModoVoz.Nada) Callar();
            ModoCambiado?.Invoke(modo);
        }

        void AlActualizar()
        {
            if (regla == null || !navegacion.Activa) return;
            // Fuera de la ruta la distancia sale de la ruta vieja: no se dice nada (tras el recálculo, de cero)
            if (navegacion.Estado == null || navegacion.Estado.FueraDeRuta) return;
            string frase = regla.Avisar(navegacion.Estado, Modo);
            if (string.IsNullOrWhiteSpace(frase)) return;
            try { voz.Hablar(frase); }
            catch (Exception) { /* la voz no debe parar la navegación */ }
        }

        void Callar()
        {
            try { voz.Parar(); }
            catch (Exception) { /* igual */ }
        }
    }
}
