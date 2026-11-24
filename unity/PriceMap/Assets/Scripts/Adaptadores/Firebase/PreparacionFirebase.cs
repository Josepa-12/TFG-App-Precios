using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Deja Firebase preparado: dependencias de Google Play, aplicación y Firestore con su copia en el móvil, el único almacén
    /// local de datos dinámicos (AD-8). Antes de que nadie use Firestore: después, los ajustes ya no se pueden cambiar.
    /// </summary>
    static class PreparacionFirebase
    {
        /// <summary>Copia en el móvil: al pasar de aquí, Firestore quita lo que hace más que no se usa.</summary>
        internal const long BytesCopia = 100L * 1024 * 1024;

        /// <summary>null si ha ido bien; si no, el motivo (o una excepción, p. ej. sin google-services.json).</summary>
        internal static async Task<string> Preparar()
        {
            var dependencias = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencias != DependencyStatus.Available) return "servicios de Google Play: " + dependencias;
            if (FirebaseApp.DefaultInstance == null) return "sin configuración (google-services.json)";

            var ajustes = FirebaseFirestore.DefaultInstance.Settings;
            ajustes.PersistenceEnabled = true;
            ajustes.CacheSizeBytes = BytesCopia;
            return null;
        }
    }
}
