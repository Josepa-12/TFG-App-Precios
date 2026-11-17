using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Sistema
{
    /// <summary>Preferencias de la interfaz guardadas en el móvil (PlayerPrefs). Nada de datos personales.</summary>
    public class PreferenciasLocales : IPreferencias
    {
        const string Prefijo = "PriceMap.";

        public string Leer(string clave) =>
            PlayerPrefs.HasKey(Prefijo + clave) ? PlayerPrefs.GetString(Prefijo + clave) : null;

        public void Guardar(string clave, string valor)
        {
            PlayerPrefs.SetString(Prefijo + clave, valor);
            PlayerPrefs.Save();
        }

        public void Borrar(string clave)
        {
            PlayerPrefs.DeleteKey(Prefijo + clave);
            PlayerPrefs.Save();
        }
    }
}
