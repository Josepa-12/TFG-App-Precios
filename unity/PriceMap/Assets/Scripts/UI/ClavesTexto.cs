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
        /// <summary>Aviso de ruta sin GPS en segundo plano: permiso de notificaciones denegado (historia 2.1).</summary>
        public const string UbicacionAvisoNotificaciones = "ubicacion.aviso.notificaciones";

        /// <summary>Ficha del lugar (historia 1.8): botón de cerrar para TalkBack.</summary>
        public const string FichaCerrar = "ficha.cerrar";
        /// <summary>Tipo y plantilla: {0} tipo ("Clínica privada"), {1} plantilla ("Servicio local").</summary>
        public const string FichaTipo = "ficha.tipo";
        public const string PlantillaServicio = "plantilla.servicio";
        public const string PlantillaHosteleria = "plantilla.hosteleria";
        public const string PlantillaOcio = "plantilla.ocio";
        /// <summary>Lugar sin ningún precio (UX-DR16).</summary>
        public const string FichaSinPrecio = "ficha.sinprecio";
        /// <summary>La web nombra el servicio pero no da el importe.</summary>
        public const string FichaConsultar = "ficha.consultar";
        /// <summary>Precio "desde": {0} importe ("40 €").</summary>
        public const string FichaPrecioDesde = "ficha.precio.desde";
        /// <summary>Rango por persona: {0} mínimo sin € ("10"), {1} máximo con € ("20 €").</summary>
        public const string FichaPrecioRango = "ficha.precio.rango";
        /// <summary>Rango abierto por persona: {0} mínimo con € ("50 €").</summary>
        public const string FichaPrecioRangoAbierto = "ficha.precio.rangoabierto";
        /// <summary>Unidad en la que se cobra: {0} unidad ("sesión").</summary>
        public const string FichaUnidad = "ficha.unidad";
        /// <summary>Origen: web oficial ({0} fecha dd/MM/aaaa), declarado por el negocio, según {0} clientes.</summary>
        public const string FichaOrigenWeb = "ficha.origen.web";
        public const string FichaOrigenDeclarado = "ficha.origen.declarado";
        public const string FichaOrigenClientes = "ficha.origen.clientes";
        /// <summary>Texto de cada punto de frescura.</summary>
        public const string FrescuraAlDia = "frescura.aldia";
        public const string FrescuraPodria = "frescura.podria";
        public const string FrescuraAntiguo = "frescura.antiguo";

        /// <summary>Chip de la variante vigente (historia 1.8, parte 2).</summary>
        public const string FichaAhora = "ficha.ahora";
        /// <summary>Variante por horario: {0} desde, {1} hasta ("De 20:00 a 24:00").</summary>
        public const string VarianteHorario = "variante.horario";
        /// <summary>Días seguidos: {0} primero, {1} último ("lun a vie").</summary>
        public const string VarianteDiasSeguidos = "variante.dias.seguidos";
        /// <summary>Último elemento de una lista de días: {0} los anteriores, {1} el último ("lun, mar y vie").</summary>
        public const string VarianteDiasLista = "variante.dias.lista";
        /// <summary>Cliente con bono ("con-bono").</summary>
        public const string VarianteConBono = "variante.conbono";
        /// <summary>Días abreviados en minúsculas, de lunes a domingo.</summary>
        public const string DiaLunes = "dia.lun";
        public const string DiaMartes = "dia.mar";
        public const string DiaMiercoles = "dia.mie";
        public const string DiaJueves = "dia.jue";
        public const string DiaViernes = "dia.vie";
        public const string DiaSabado = "dia.sab";
        public const string DiaDomingo = "dia.dom";

        /// <summary>TalkBack de un concepto (historia 1.8, parte 3): {0} nombre, {1} precio ("desde 40 €" o "Consultar").</summary>
        public const string FichaAnuncioConcepto = "ficha.anuncio.concepto";
        /// <summary>Precio con su unidad: {0} precio, {1} unidad ("desde 40 € por sesión").</summary>
        public const string FichaAnuncioUnidad = "ficha.anuncio.unidad";
        /// <summary>Origen web para TalkBack: {0} fecha dd/MM/aaaa ("Publicado en su web el 13/11/2026").</summary>
        public const string FichaAnuncioOrigenWeb = "ficha.anuncio.origen.web";
        /// <summary>Une dos frases: {0} la anterior, {1} la siguiente ("…. Al día").</summary>
        public const string FichaAnuncioFrase = "ficha.anuncio.frase";
        /// <summary>Variante: {0} condición, {1} precio ("De 20:00 a 24:00, 24 €").</summary>
        public const string FichaAnuncioVariante = "ficha.anuncio.variante";
        /// <summary>Variante vigente: {0} texto de FichaAhora, {1} condición en minúscula, {2} precio ("Ahora: de 20:00 a 24:00, 24 €").</summary>
        public const string FichaAnuncioVarianteAhora = "ficha.anuncio.variante.ahora";

        public static readonly string[] Todas =
        {
            FichaAnuncioConcepto, FichaAnuncioUnidad, FichaAnuncioOrigenWeb, FichaAnuncioFrase,
            FichaAnuncioVariante, FichaAnuncioVarianteAhora,
            FichaAhora, VarianteHorario, VarianteDiasSeguidos, VarianteDiasLista, VarianteConBono,
            DiaLunes, DiaMartes, DiaMiercoles, DiaJueves, DiaViernes, DiaSabado, DiaDomingo,
            FichaCerrar, FichaTipo, PlantillaServicio, PlantillaHosteleria, PlantillaOcio, FichaSinPrecio, FichaConsultar,
            FichaPrecioDesde, FichaPrecioRango, FichaPrecioRangoAbierto, FichaUnidad,
            FichaOrigenWeb, FichaOrigenDeclarado, FichaOrigenClientes, FrescuraAlDia, FrescuraPodria, FrescuraAntiguo,
            UbicacionCentrar, UbicacionExplicacion, UbicacionPermitir, UbicacionAhoraNo,
            UbicacionNorte, UbicacionBuscando, UbicacionDesactivada,
            UbicacionAvisoDenegado, UbicacionAvisoApagada, UbicacionAbrirAjustes, UbicacionCerrar, UbicacionAvisoNotificaciones,
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
