namespace PriceMap.UI
{
    /// <summary>
    /// Claves de la tabla de Localization "Textos" (NFR-5). El código solo usa claves; el texto en
    /// castellano vive en la tabla es-ES. Una prueba comprueba que todas existen en la tabla.
    /// </summary>
    public static class ClavesTexto
    {
        public const string Tabla = "Textos";

        public const string TemaCambiar = "tema.cambiar";
        public const string TemaClaro = "tema.claro";
        public const string TemaOscuro = "tema.oscuro";
        public const string MapaAcercar = "mapa.acercar";
        public const string MapaAlejar = "mapa.alejar";
        public const string MapaAtribucion = "mapa.atribucion";

        /// <summary>TalkBack de un marcador con precio: {0} nombre, {1} tipo en minúsculas, {2} precio.</summary>
        public const string MarcadorConPrecio = "marcador.anuncio.conprecio";
        /// <summary>TalkBack de un marcador sin precio: {0} nombre, {1} tipo en minúsculas.</summary>
        public const string MarcadorSinPrecio = "marcador.anuncio.sinprecio";

        /// <summary>Píldora del filtro "Precios" para TalkBack (el valor es la opción elegida).</summary>
        public const string PreciosControl = "precios.control";
        /// <summary>Texto de la píldora con Ninguno.</summary>
        public const string PreciosVer = "precios.ver";
        /// <summary>Texto de la píldora con Todos.</summary>
        public const string PreciosTodos = "precios.todos";
        public const string PreciosTitulo = "precios.titulo";
        public const string PreciosOpcionNinguno = "precios.opcion.ninguno";
        public const string PreciosOpcionTodos = "precios.opcion.todos";
        public const string PreciosOpcionServicios = "precios.opcion.servicios";
        public const string PreciosOpcionHosteleria = "precios.opcion.hosteleria";
        public const string PreciosOpcionOcio = "precios.opcion.ocio";
        /// <summary>TalkBack al elegir una opción: {0} nombre de la opción.</summary>
        public const string PreciosAnuncio = "precios.anuncio";

        /// <summary>TalkBack de un punto de interés con nombre: {0} nombre, {1} categoría en minúsculas.</summary>
        public const string PoiAnuncio = "poi.anuncio";
        /// <summary>Categoría de un centro sanitario público ("centro sanitario público").</summary>
        public const string PoiSanidadPublica = "poi.sanidadpublica";
        // Nombre de cada categoría de punto de interés en minúsculas (POI_CATS.one del prototipo)
        public const string PoiComer = "poi.categoria.comer";
        public const string PoiRapida = "poi.categoria.rapida";
        public const string PoiCafe = "poi.categoria.cafe";
        public const string PoiBares = "poi.categoria.bares";
        public const string PoiSuper = "poi.categoria.super";
        public const string PoiTiendas = "poi.categoria.tiendas";
        public const string PoiGasolina = "poi.categoria.gasolina";
        public const string PoiCarga = "poi.categoria.carga";
        public const string PoiParking = "poi.categoria.parking";
        public const string PoiHoteles = "poi.categoria.hoteles";
        public const string PoiFarmacia = "poi.categoria.farmacia";
        public const string PoiSalud = "poi.categoria.salud";
        public const string PoiDeporte = "poi.categoria.deporte";
        public const string PoiParques = "poi.categoria.parques";
        public const string PoiCultura = "poi.categoria.cultura";
        public const string PoiBancos = "poi.categoria.bancos";
        public const string PoiBelleza = "poi.categoria.belleza";
        public const string PoiTalleres = "poi.categoria.talleres";
        public const string PoiTransporte = "poi.categoria.transporte";
        public const string PoiEducacion = "poi.categoria.educacion";
        public const string PoiPublico = "poi.categoria.publico";
        public const string PoiCulto = "poi.categoria.culto";

        /// <summary>Botón de ubicación para TalkBack (con posición).</summary>
        public const string UbicacionCentrar = "ubicacion.centrar";
        /// <summary>Explicación antes de pedir el permiso de ubicación.</summary>
        public const string UbicacionExplicacion = "ubicacion.explicacion";
        public const string UbicacionPermitir = "ubicacion.permitir";
        public const string UbicacionAhoraNo = "ubicacion.ahorano";
        /// <summary>Botón de ubicación para TalkBack según lo que hará al tocarlo (historia 1.7, parte 2).</summary>
        public const string UbicacionNorte = "ubicacion.norte";
        public const string UbicacionBuscando = "ubicacion.buscando";
        public const string UbicacionDesactivada = "ubicacion.desactivada";
        /// <summary>Aviso de ubicación desactivada: permiso denegado o ubicación del móvil apagada.</summary>
        public const string UbicacionAvisoDenegado = "ubicacion.aviso.denegado";
        public const string UbicacionAvisoApagada = "ubicacion.aviso.apagada";
        public const string UbicacionAbrirAjustes = "ubicacion.abrirajustes";
        public const string UbicacionCerrar = "ubicacion.cerrar";

        public static readonly string[] Todas =
        {
            UbicacionCentrar, UbicacionExplicacion, UbicacionPermitir, UbicacionAhoraNo,
            UbicacionNorte, UbicacionBuscando, UbicacionDesactivada,
            UbicacionAvisoDenegado, UbicacionAvisoApagada, UbicacionAbrirAjustes, UbicacionCerrar,
            TemaCambiar, TemaClaro, TemaOscuro, MapaAcercar, MapaAlejar, MapaAtribucion,
            MarcadorConPrecio, MarcadorSinPrecio,
            PreciosControl, PreciosVer, PreciosTodos, PreciosTitulo,
            PreciosOpcionNinguno, PreciosOpcionTodos, PreciosOpcionServicios, PreciosOpcionHosteleria, PreciosOpcionOcio,
            PreciosAnuncio,
            PoiAnuncio, PoiSanidadPublica,
            PoiComer, PoiRapida, PoiCafe, PoiBares, PoiSuper, PoiTiendas, PoiGasolina, PoiCarga,
            PoiParking, PoiHoteles, PoiFarmacia, PoiSalud, PoiDeporte, PoiParques, PoiCultura, PoiBancos,
            PoiBelleza, PoiTalleres, PoiTransporte, PoiEducacion, PoiPublico, PoiCulto,
        };
    }
}
