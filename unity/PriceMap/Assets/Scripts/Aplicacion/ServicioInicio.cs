using System;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Decide cuándo se va la pantalla de inicio (spec pantalla-de-inicio): con la posición centrada (si se espera) y la
    /// vista del mapa cargada, o a los 4 s desde que Unity está listo, y nunca antes de que el pin haya caído (1 s). Sin
    /// pantalla de inicio (PC, editor, captura) no hace nada.
    /// </summary>
    public class ServicioInicio
    {
        readonly IPantallaInicio pantalla;
        readonly Func<double> reloj;
        readonly double listo;

        /// <param name="reloj">Segundos de un reloj que no retrocede. Se crea cuando Unity está listo: los 4 s cuentan desde ahí.</param>
        public ServicioInicio(IPantallaInicio pantalla, Func<double> reloj)
        {
            this.pantalla = pantalla ?? throw new ArgumentNullException(nameof(pantalla));
            this.reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            listo = reloj();
            ALaVista = pantalla.Segundos.HasValue;
        }

        /// <summary>La pantalla de inicio tapa la app (también mientras se anima al irse).</summary>
        public bool ALaVista { get; private set; }

        /// <summary>Ya se le ha dicho que se vaya.</summary>
        public bool Yendose { get; private set; }

        /// <summary>La pantalla de inicio se ha ido del todo (una vez; también si se fue sola a los 15 s).</summary>
        public event Action Terminada;

        /// <summary>
        /// Lo llama la pantalla del mapa en cada fotograma. True: ya toca irse (y entonces se llama a Irse). Si la pantalla
        /// de inicio ha desaparecido, avisa con Terminada.
        /// </summary>
        /// <param name="esperaPosicion">Se espera a la posición (con permiso, encendida y sin nada compartido que abra otro sitio).</param>
        /// <param name="centrada">Hay posición y el mapa está centrado en ella.</param>
        /// <param name="vistaCargada">Si las teselas de la vista están cargadas; solo se pregunta si hace falta.</param>
        public bool Comprobar(bool esperaPosicion, bool centrada, Func<bool> vistaCargada)
        {
            if (!ALaVista) return false;
            var segundos = pantalla.Segundos;
            if (!segundos.HasValue)
            {
                ALaVista = false;
                Terminada?.Invoke();
                return false;
            }
            if (Yendose || segundos.Value < ParametrosNegocio.InicioPinSegundos) return false;
            if (reloj() - listo >= ParametrosNegocio.InicioMaximoSegundos) return true;
            return (!esperaPosicion || centrada) && vistaCargada();
        }

        /// <summary>Se va, con el pin hacia ese punto (null: el centro de la pantalla). Solo la primera vez.</summary>
        public void Irse(PuntoPantalla? destino)
        {
            if (!ALaVista || Yendose) return;
            Yendose = true;
            pantalla.Irse(destino);
        }
    }
}
