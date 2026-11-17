using System.Collections.Generic;
using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>Puerto de los lugares base empaquetados con la app (Assets/Datos/).</summary>
    public interface IRepositorioLugares
    {
        /// <summary>Todos los lugares; si no se pueden leer, un fallo con una lista vacía.</summary>
        Resultado<IReadOnlyList<Lugar>> Cargar();
    }
}
