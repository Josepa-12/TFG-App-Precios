using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.UI
{
    /// <summary>
    /// Pantalla principal: el mapa. Recibe el puerto del mapa desde el Arranque y nunca
    /// busca ni crea piezas por su cuenta (AD-2). Las siguientes historias le añaden sus controles.
    /// </summary>
    public class PantallaMapa : MonoBehaviour
    {
        /// <summary>Plaza del Cabildo, Sanlúcar de Barrameda: vista inicial del mapa.</summary>
        public static readonly Coordenada VistaInicial = new Coordenada(36.7779, -6.3528);

        IMapa mapa;

        public void Inicializar(IMapa mapa)
        {
            this.mapa = mapa;
            this.mapa.Centrar(VistaInicial);
        }
    }
}
