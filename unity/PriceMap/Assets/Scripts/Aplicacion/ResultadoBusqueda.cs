using PriceMap.Dominio;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// De dónde sale un resultado del buscador: un lugar de la app, un punto de interés del mapa o una dirección del
    /// geocodificador (historia 2.3, parte 2) o la coordenada de un enlace o unas coordenadas pegados en el buscador
    /// (historia 2.5, parte 2: «Ir a este punto»).
    /// </summary>
    public enum TipoResultado
    {
        Lugar,
        Punto,
        Direccion,
        Coordenada,
    }

    /// <summary>
    /// Un resultado del buscador (historia 2.3): un lugar de la app (con su precio anunciado, si lo tiene) o un punto
    /// de interés de las teselas cargadas (con su categoría). Calculado en cada búsqueda; nada se guarda (FR17).
    /// </summary>
    public sealed class ResultadoBusqueda
    {
        public TipoResultado Tipo { get; }
        public string Nombre { get; }
        public Coordenada Posicion { get; }
        /// <summary>El lugar de la app; null si es un punto de interés.</summary>
        public Lugar Lugar { get; }
        /// <summary>El punto de interés; null si es un lugar de la app.</summary>
        public PuntoInteres Punto { get; }
        /// <summary>La dirección del geocodificador; null en los lugares y los puntos.</summary>
        public Direccion Direccion { get; }
        /// <summary>Categoría del punto de interés; Ninguna en los lugares de la app.</summary>
        public CategoriaPoi Categoria { get; }
        /// <summary>Punto de interés que es un centro sanitario público (ReglaPuntosInteres.EsSanidadPublica).</summary>
        public bool SanidadPublica { get; }
        /// <summary>Distancia (m) al punto de cercanía de la búsqueda (el usuario o el centro del mapa).</summary>
        public double Metros { get; }
        /// <summary>Puntuación de ReglaBusqueda (3 exacto, 2 empieza igual, 1 contiene).</summary>
        public int Puntuacion { get; }
        /// <summary>Precio anunciado del lugar ("desde X"); null en los puntos y en los lugares sin precio.</summary>
        public ValorPrecio? Precio { get; }

        ResultadoBusqueda(TipoResultado tipo, string nombre, Coordenada posicion, Lugar lugar, PuntoInteres punto,
            CategoriaPoi categoria, bool sanidadPublica, double metros, int puntuacion, ValorPrecio? precio,
            Direccion direccion = null)
        {
            Tipo = tipo;
            Nombre = nombre ?? "";
            Posicion = posicion;
            Lugar = lugar;
            Punto = punto;
            Direccion = direccion;
            Categoria = categoria;
            SanidadPublica = sanidadPublica;
            Metros = metros;
            Puntuacion = puntuacion;
            Precio = precio;
        }

        public static ResultadoBusqueda DeLugar(Lugar lugar, double metros, int puntuacion, ValorPrecio? precio) =>
            new ResultadoBusqueda(TipoResultado.Lugar, lugar.Nombre, lugar.Posicion, lugar, null, CategoriaPoi.Ninguna, false,
                metros, puntuacion, precio);

        public static ResultadoBusqueda DePunto(PuntoInteres punto, CategoriaPoi categoria, bool sanidadPublica, double metros, int puntuacion) =>
            new ResultadoBusqueda(TipoResultado.Punto, punto.Nombre, punto.Posicion, null, punto, categoria, sanidadPublica,
                metros, puntuacion, null);

        /// <summary>Una dirección del geocodificador (va siempre detrás de los lugares y los puntos).</summary>
        public static ResultadoBusqueda DeDireccion(Direccion direccion, double metros) =>
            new ResultadoBusqueda(TipoResultado.Direccion, direccion.Nombre, direccion.Posicion, null, null, CategoriaPoi.Ninguna,
                false, metros, 0, null, direccion);

        /// <summary>
        /// «Ir a este punto» (historia 2.5, parte 2): la coordenada de un enlace o de unas coordenadas pegadas. Sin
        /// nombre (lo pone la fila) y sin dirección: la ficha de punto libre la pide a Photon.
        /// </summary>
        public static ResultadoBusqueda DeCoordenada(Coordenada posicion, double metros) =>
            new ResultadoBusqueda(TipoResultado.Coordenada, "", posicion, null, null, CategoriaPoi.Ninguna, false, metros, 0, null);

        public override string ToString() => $"{Tipo} {Nombre} ({Puntuacion}, {Metros:F0} m)";
    }
}
