namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Lo que otra app ha compartido con PriceMap (historia 2.5, parte 3; FR17): un texto («Compartir») o un enlace
    /// <c>geo:</c> («Abrir con»). Nada se guarda.
    /// </summary>
    public interface IEntradaCompartida
    {
        /// <summary>
        /// El texto o el enlace recibido y aún no leído; null si no hay. Se consume: la siguiente llamada ya no lo
        /// devuelve (al volver a la app no se repite).
        /// </summary>
        string Tomar();
    }
}
