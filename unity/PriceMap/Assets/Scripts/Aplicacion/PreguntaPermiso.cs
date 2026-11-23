namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Si Android aún puede preguntar por un permiso después de denegarlo (historia 3.1). Android no lo dice: solo si
    /// conviene explicarlo (shouldShowRequestPermissionRationale), que es true tras la primera denegación y vuelve a false
    /// con la definitiva. False antes y después puede ser un diálogo cerrado sin responder o un permiso ya bloqueado: la
    /// primera vez se da por posible; si se repite, no.
    /// </summary>
    public class PreguntaPermiso
    {
        bool dudosa;

        public bool PuedePedir { get; private set; } = true;

        /// <param name="explicaba">Lo que decía Android al pedirlo.</param>
        /// <param name="explica">Lo que dice ahora, tras denegarlo.</param>
        public void Denegar(bool explicaba, bool explica)
        {
            PuedePedir = explica || (!explicaba && !dudosa);
            if (!explica) dudosa = true;
        }
    }
}
