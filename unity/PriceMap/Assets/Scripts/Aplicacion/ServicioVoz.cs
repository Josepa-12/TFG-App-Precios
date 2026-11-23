using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Indicaciones por voz al navegar (historia 2.8, parte 1; FR21). Escucha a ServicioNavegacion: al empezar una ruta
    /// crea la ReglaVoz de su modo de viaje; con cada estado nuevo (Actualizado) dice lo que la regla mande según el modo
    /// de voz; al recalcular empieza de cero con la ruta nueva; al terminar calla. Al llegar
    /// (historia 2.9), en todo y en lo importante, «Has llegado a &lt;destino&gt;» una vez, y ninguna maniobra más. El modo (todo, lo importante o nada) se
    /// guarda en IPreferencias con la clave "voz" (sin valor: todo). Se usa desde el hilo principal (la UI).
    /// </summary>
    public sealed class ServicioVoz
    {
        public const string ClavePreferencia = "voz";

        readonly ServicioNavegacion navegacion;
        readonly IVoz voz;
        readonly IPreferencias preferencias;
        readonly Func<DateTime> reloj;
        readonly Func<string, string> textoLlegada;
        ReglaVoz regla; // null sin ruta
        DateTime calladaHasta = DateTime.MinValue; // tras Reanudar, hasta aquí solo se apunta (Sincronizar)
        bool reanudada; // Reanudar sin estado nuevo aún: el segundo empieza con el primero

        /// <summary>Lo que dice la voz; el guardado o, la primera vez, todas las indicaciones.</summary>
        public ModoVoz Modo { get; private set; }

        /// <summary>El usuario ha elegido otro modo.</summary>
        public event Action<ModoVoz> ModoCambiado;

        /// <param name="reloj">La hora de ahora (para el silencio de Reanudar); sin él, DateTime.UtcNow.</param>
        /// <param name="textoLlegada">Lo que dice al llegar, con el destino (la app lo saca de Localization).</param>
        public ServicioVoz(ServicioNavegacion navegacion, IVoz voz, IPreferencias preferencias, Func<DateTime> reloj = null,
            Func<string, string> textoLlegada = null)
        {
            this.textoLlegada = textoLlegada ?? (destino => "Has llegado a " + destino);
            this.reloj = reloj ?? (() => DateTime.UtcNow);
            this.navegacion = navegacion ?? throw new ArgumentNullException(nameof(navegacion));
            this.voz = voz ?? throw new ArgumentNullException(nameof(voz));
            this.preferencias = preferencias ?? throw new ArgumentNullException(nameof(preferencias));
            Modo = ReglaVoz.DeClave(preferencias.Leer(ClavePreferencia));
            navegacion.RutaIniciada += _ =>
            {
                regla = new ReglaVoz(this.navegacion.Modo);
                reanudada = false; // una ruta nueva no trae avisos viejos: su primer aviso se dice
                calladaHasta = DateTime.MinValue;
            };
            navegacion.Actualizado += AlActualizar;
            navegacion.RutaCambiada += () => regla?.Reiniciar();
            navegacion.HaLlegado += AlLlegar;
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

        /// <summary>
        /// Unity vuelve de una pausa real (historia 2.8, parte 2: el sistema la pausó aunque se navegaba): desde el primer
        /// estado nuevo (el fotograma en que llegan de golpe las posiciones guardadas, que tras una pausa larga puede tardar
        /// más de 1 s) y durante 1 s (ParametrosNegocio.VozSilencioAlVolverSegundos) no se habla; esos estados solo se
        /// apuntan como dichos, así no se dicen avisos viejos. Una ruta nueva lo anula. Lo llama el Arranque.
        /// </summary>
        public void Reanudar() => reanudada = true;

        void AlActualizar()
        {
            if (reanudada)
            {
                reanudada = false;
                calladaHasta = reloj().AddSeconds(ParametrosNegocio.VozSilencioAlVolverSegundos);
            }
            if (regla == null || !navegacion.Activa || navegacion.Llegado) return;
            // Fuera de la ruta la distancia sale de la ruta vieja: no se dice nada (tras el recálculo, de cero)
            if (navegacion.Estado == null || navegacion.Estado.FueraDeRuta) return;
            if (reloj() < calladaHasta) { regla.Sincronizar(navegacion.Estado); return; }
            string frase = regla.Avisar(navegacion.Estado, Modo);
            Decir(() => frase);
        }

        // También al volver de una pausa (Reanudar): no es un aviso viejo, se acaba de saber que ha llegado
        void AlLlegar(string destino)
        {
            if (Modo != ModoVoz.Nada) Decir(() => textoLlegada(destino));
        }

        void Decir(Func<string> texto)
        {
            try
            {
                string frase = texto();
                if (!string.IsNullOrWhiteSpace(frase)) voz.Hablar(frase);
            }
            catch (Exception) { /* la voz no debe parar la navegación */ }
        }

        void Callar()
        {
            try { voz.Parar(); }
            catch (Exception) { /* igual */ }
        }
    }
}
