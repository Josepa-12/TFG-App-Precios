namespace PriceMap.Aplicacion
{
    /// <summary>Puerto de las preferencias guardadas en el propio móvil (ajustes de la interfaz).</summary>
    public interface IPreferencias
    {
        /// <summary>Valor guardado, o null si no hay ninguno.</summary>
        string Leer(string clave);
        void Guardar(string clave, string valor);
        void Borrar(string clave);
    }
}
