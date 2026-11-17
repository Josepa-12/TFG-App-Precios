using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Punto de interés de OpenStreetMap tal como llega en la capa "poi" de las teselas (FR-4): sin
    /// clasificar. La categoría, si se oculta y si es sanidad pública los decide ReglaPuntosInteres.
    /// </summary>
    public sealed class PuntoInteres
    {
        /// <summary>Id del elemento en la tesela (sale del id de OSM; 0 si no lo trae).</summary>
        public ulong Id { get; }
        /// <summary>"name:es" o, si no lo hay, "name"; vacío si no tiene nombre.</summary>
        public string Nombre { get; }
        /// <summary>Atributo "class" de OpenMapTiles (p. ej. "grocery").</summary>
        public string Clase { get; }
        /// <summary>Atributo "subclass" de OpenMapTiles (p. ej. "supermarket").</summary>
        public string Subclase { get; }
        /// <summary>Importancia (menor = más importante); 99 si no lo trae.</summary>
        public int Rank { get; }
        public Coordenada Posicion { get; }

        public PuntoInteres(ulong id, string nombre, string clase, string subclase, int rank, Coordenada posicion)
        {
            Id = id;
            Nombre = nombre ?? "";
            Clase = clase ?? "";
            Subclase = subclase ?? "";
            Rank = rank;
            Posicion = posicion;
        }

        public override string ToString() => $"{Nombre} ({Clase}/{Subclase}) {Posicion}";
    }
}
