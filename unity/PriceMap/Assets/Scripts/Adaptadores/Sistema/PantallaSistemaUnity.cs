using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Sistema
{
    /// <summary>
    /// Pantalla encendida con Screen.sleepTimeout (historia 2.7, parte 2b): NeverSleep mientras se navega y, al terminar,
    /// SystemSetting (el valor del sistema).
    /// </summary>
    public class PantallaSistemaUnity : IPantallaSistema
    {
        public void MantenerEncendida(bool encendida) =>
            Screen.sleepTimeout = encendida ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
    }
}
