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

        /// <summary>Buscador (historia 2.2): texto del campo vacío y entrada "Encuentra cerca".</summary>
        public const string BuscadorCampo = "buscador.campo";
        public const string BuscadorCerca = "buscador.cerca";
        /// <summary>Botones de la hoja que solo ve TalkBack (historia 2.2, parte 2): "Expandir" y "Contraer".</summary>
        public const string BuscadorExpandir = "buscador.expandir";
        public const string BuscadorContraer = "buscador.contraer";
        /// <summary>Chips de categorías del buscador (en plural y con mayúscula: "Restaurantes").</summary>
        public const string BuscadorComer = "buscador.categoria.comer";
        public const string BuscadorBares = "buscador.categoria.bares";
        public const string BuscadorSuper = "buscador.categoria.super";
        public const string BuscadorFarmacia = "buscador.categoria.farmacia";
        public const string BuscadorGasolina = "buscador.categoria.gasolina";
        public const string BuscadorDeporte = "buscador.categoria.deporte";
        public const string BuscadorSalud = "buscador.categoria.salud";
        public const string BuscadorCultura = "buscador.categoria.cultura";

        /// <summary>Resultados del buscador (historia 2.3): sin resultados y cuántos hay (anuncio de TalkBack).</summary>
        public const string BuscadorSinResultados = "buscador.sinresultados";
        public const string BuscadorUnResultado = "buscador.resultados.uno";
        /// <summary>{0} número de resultados ("12 resultados").</summary>
        public const string BuscadorResultados = "buscador.resultados.varios";
        /// <summary>La X del campo: "Borrar búsqueda".</summary>
        public const string BuscadorBorrar = "buscador.borrar";
        /// <summary>Segunda línea de cada resultado: {0} tipo, {1} distancia ("Clínica privada · 350 m").</summary>
        public const string BuscadorDetalle = "buscador.resultado.detalle";
        /// <summary>TalkBack de un resultado: {0} nombre, {1} tipo, {2} distancia ("Mercadona, supermercado, a 350 m").</summary>
        public const string BuscadorAnuncioResultado = "buscador.resultado.anuncio";
        /// <summary>Igual, con {3} el precio ("Clínica Areas, Clínica privada, a 350 m, desde 20 €").</summary>
        public const string BuscadorAnuncioResultadoPrecio = "buscador.resultado.anuncio.precio";

        /// <summary>Direcciones del buscador (historia 2.3, parte 2): tipo de la fila ("Dirección").</summary>
        public const string BuscadorDireccion = "buscador.direccion";
        /// <summary>Distancia de la fila de una dirección: {0} distancia ("a 1,2 km").</summary>
        public const string BuscadorDistanciaA = "buscador.direccion.distancia";
        /// <summary>Aviso bajo la lista sin conexión o si Photon falla.</summary>
        public const string BuscadorDireccionesSinConexion = "buscador.direcciones.sinconexion";
        /// <summary>Botón junto a "No hay resultados": pide las direcciones ya (también sirve de reintento).</summary>
        public const string BuscadorBuscarDireccion = "buscador.direccion.buscar";

        /// <summary>TalkBack de una dirección con localidad (parte 3): {0} nombre, {1} "Dirección", {2} localidad, {3} distancia.</summary>
        public const string BuscadorAnuncioDireccion = "buscador.direccion.anuncio";

        /// <summary>"Encuentra cerca" (historia 2.4): título de la lista de una categoría, {0} su nombre ("Restaurantes cerca").</summary>
        public const string BuscadorCercaCategoria = "buscador.cerca.categoria";
        /// <summary>Título de la lista de "Encuentra cerca" (todas las categorías): "Cerca".</summary>
        public const string BuscadorCercaTitulo = "buscador.cerca.titulo";
        /// <summary>Bajo el título, sin posición del usuario: las distancias se miden desde el centro del mapa.</summary>
        public const string BuscadorCercaDesdeCentro = "buscador.cerca.desdecentro";
        /// <summary>Lista vacía: {0} lo que no hay, en minúsculas y en plural ("No hay gasolineras cerca").</summary>
        public const string BuscadorCercaVacio = "buscador.cerca.vacio";
        /// <summary>
        /// Lo que no hay, para "No hay {0} cerca", por categoría: no siempre es el nombre del chip ("Salud" →
        /// "sitios de salud").
        /// </summary>
        public const string CercaNombreComer = "buscador.cerca.nombre.comer", CercaNombreBares = "buscador.cerca.nombre.bares",
            CercaNombreSuper = "buscador.cerca.nombre.super", CercaNombreFarmacia = "buscador.cerca.nombre.farmacia",
            CercaNombreGasolina = "buscador.cerca.nombre.gasolina", CercaNombreDeporte = "buscador.cerca.nombre.deporte",
            CercaNombreSalud = "buscador.cerca.nombre.salud", CercaNombreCultura = "buscador.cerca.nombre.cultura";
        /// <summary>La X de la cabecera de la lista: vuelve a las categorías.</summary>
        public const string BuscadorQuitarCategoria = "buscador.cerca.quitar";
        /// <summary>La píldora con una categoría, para TalkBack: "Quitar categoría Salud" (historia 2.4, parte 2).</summary>
        public const string PildoraQuitarCategoria = "precios.quitar-categoria";
        /// <summary>
        /// TalkBack de la lista "cerca" (historia 2.4, parte 3): junta dos partes, {0} y {1} ("Salud cerca, 3 resultados";
        /// "Salud cerca, desde el centro del mapa").
        /// </summary>
        public const string BuscadorCercaJuntar = "buscador.cerca.juntar";

        /// <summary>Ficha de punto libre (historia 2.5): subtítulo bajo el título ("Punto en el mapa").</summary>
        public const string PuntoTipo = "punto.tipo";
        /// <summary>Título con las coordenadas: {0} latitud, {1} longitud, ya con 4 decimales y coma decimal ("36,7781, -6,3515").</summary>
        public const string PuntoCoordenadas = "punto.coordenadas";
        /// <summary>Título con la dirección: {0} calle, {1} número ("Calle Ancha, 10").</summary>
        public const string PuntoCalleNumero = "punto.callenumero";
        /// <summary>Bajo el subtítulo mientras se espera a Photon ("Buscando la dirección…").</summary>
        public const string PuntoBuscando = "punto.buscando";
        /// <summary>TalkBack del título con la localidad: {0} título, {1} localidad ("Calle Ancha, 10, Sanlúcar de Barrameda").</summary>
        public const string PuntoAnuncio = "punto.anuncio";

        /// <summary>Enlaces y coordenadas pegados en el buscador (historia 2.5, parte 2): la fila ("Ir a este punto").</summary>
        public const string BuscadorIrAPunto = "buscador.enlace.ir";
        /// <summary>TalkBack de esa fila: {0} "Ir a este punto", {1} coordenadas ("Ir a este punto, 36,7781, -6,3515").</summary>
        public const string BuscadorAnuncioPunto = "buscador.enlace.anuncio";
        /// <summary>En el sitio de "No hay resultados" mientras se resuelve un enlace corto.</summary>
        public const string BuscadorComprobandoEnlace = "buscador.enlace.comprobando";
        /// <summary>En el sitio de "No hay resultados" si el enlace no se reconoce (o el corto no se puede resolver).</summary>
        public const string BuscadorEnlaceNoReconocido = "buscador.enlace.noreconocido";

        /// <summary>Fila de acciones de la ficha (historia 2.6): nombres de los modos ("Coche", "A pie").</summary>
        public const string FichaModoCoche = "ficha.modo.coche", FichaModoPie = "ficha.modo.pie";
        /// <summary>Texto del botón Ir.</summary>
        public const string FichaIr = "ficha.ir";
        /// <summary>TalkBack de Ir con un lugar: {0} su nombre ("Ir a Clínica Areas").</summary>
        public const string FichaIrA = "ficha.ir.a";
        /// <summary>TalkBack de Ir con un punto libre ("Ir a este punto").</summary>
        public const string FichaIrPunto = "ficha.ir.punto";
        /// <summary>Minutos bajo el modo: {0} minutos ("6 min").</summary>
        public const string FichaMinutos = "ficha.minutos";
        /// <summary>Bajo el modo mientras se calcula ("…").</summary>
        public const string FichaCalculando = "ficha.calculando";
        /// <summary>Bajo el modo sin posición o si la ruta falla ("—").</summary>
        public const string FichaSinTiempo = "ficha.sintiempo";
        /// <summary>TalkBack de un modo: {0} nombre, {1} minutos ("Coche, 6 minutos").</summary>
        public const string FichaAnuncioMinutos = "ficha.anuncio.minutos";
        /// <summary>TalkBack de un modo con 1 minuto: {0} nombre ("A pie, 1 minuto").</summary>
        public const string FichaAnuncioMinuto = "ficha.anuncio.minuto";
        /// <summary>TalkBack de un modo sin tiempo: {0} nombre ("Coche, sin tiempo").</summary>
        public const string FichaAnuncioSinTiempo = "ficha.anuncio.sintiempo";
        /// <summary>TalkBack de un modo mientras se calcula: {0} nombre ("Coche, calculando el tiempo").</summary>
        public const string FichaAnuncioCalculando = "ficha.anuncio.calculando";
        /// <summary>Aviso de TalkBack al elegir otro modo: {0} su anuncio ("A pie, 15 minutos, seleccionado").</summary>
        public const string FichaModoElegido = "ficha.modo.elegido";

        /// <summary>Tarjeta de arriba de la ruta (historia 2.6, parte 2a) mientras llega la ruta ("Calculando la ruta…").</summary>
        public const string RutaCalculando = "ruta.calculando";
        /// <summary>Tarjeta de arriba si no hay ruta ("No se ha podido calcular la ruta").</summary>
        public const string RutaFallo = "ruta.fallo";
        /// <summary>Tarjeta de arriba con una ruta sin textos de maniobra (OSRM): {0} destino ("Ruta a Clínica Areas").</summary>
        public const string RutaA = "ruta.a";
        /// <summary>TalkBack de la tarjeta de arriba: {0} maniobra, {1} la siguiente en minúscula ("… Después, gira a la derecha…").</summary>
        public const string RutaDespues = "ruta.despues";
        /// <summary>Tarjeta de abajo: rótulo bajo la hora ("Llegada").</summary>
        public const string RutaLlegada = "ruta.llegada";
        /// <summary>Tarjeta de abajo: rótulo bajo los minutos ("min").</summary>
        public const string RutaMin = "ruta.min";
        /// <summary>TalkBack de la tarjeta de abajo: {0} hora, {1} minutos, {2} distancia ("Llegada 13:34, 4 minutos, 1,2 km").</summary>
        public const string RutaAnuncio = "ruta.anuncio";
        /// <summary>Igual con 1 minuto: {0} hora, {1} distancia ("Llegada 13:34, 1 minuto, 350 m").</summary>
        public const string RutaAnuncioMinuto = "ruta.anuncio.minuto";
        /// <summary>TalkBack de la tarjeta de abajo mientras llega la ruta.</summary>
        public const string RutaAnuncioCalculando = "ruta.anuncio.calculando";
        /// <summary>Botón X de la tarjeta de abajo ("Quitar ruta").</summary>
        public const string RutaQuitar = "ruta.quitar";

        // Ruta aproximada sin conexión (historia 2.6, parte 2b)
        /// <summary>Tarjeta de arriba con la ruta aproximada ("Ruta aproximada por las calles guardadas").</summary>
        public const string RutaAproximada = "ruta.aproximada";
        /// <summary>Valor aproximado de la tarjeta de abajo: {0} el valor ("≈ 13:34", "≈ 4", "≈ 1,2").</summary>
        public const string RutaValorAprox = "ruta.valor.aprox";
        /// <summary>TalkBack de la tarjeta de abajo aproximada: {0} hora, {1} minutos, {2} distancia.</summary>
        public const string RutaAnuncioAprox = "ruta.anuncio.aprox";
        /// <summary>Igual con 1 minuto: {0} hora, {1} distancia.</summary>
        public const string RutaAnuncioAproxMinuto = "ruta.anuncio.aprox.minuto";
        /// <summary>Minutos aproximados bajo el modo: {0} minutos ("≈ 4 min").</summary>
        public const string FichaMinutosAprox = "ficha.minutos.aprox";
        /// <summary>TalkBack de un modo con minutos aproximados: {0} nombre, {1} minutos ("Coche, unos 4 minutos").</summary>
        public const string FichaAnuncioMinutosAprox = "ficha.anuncio.minutos.aprox";
        /// <summary>Igual con 1 minuto: {0} nombre ("A pie, alrededor de 1 minuto").</summary>
        public const string FichaAnuncioMinutoAprox = "ficha.anuncio.minuto.aprox";
        /// <summary>Toast al llegar la ruta aproximada ("Sin conexión: ruta aproximada").</summary>
        public const string ToastRutaAproximada = "toast.ruta.aproximada";
        /// <summary>Toast sin ruta aproximada ("Sin conexión: no hay calles guardadas para esta ruta").</summary>
        public const string ToastSinCalles = "toast.ruta.sincalles";

        // Seguir la ruta (historia 2.7, parte 1)
        /// <summary>Distancia a la maniobra siguiente en la tarjeta de arriba: {0} distancia ("En 120 m").</summary>
        public const string RutaEn = "ruta.en";
        /// <summary>Distancia que queda en una ruta sin maniobras (OSRM o aproximada): {0} distancia ("Quedan 1,2 km").</summary>
        public const string RutaQuedan = "ruta.quedan";
        /// <summary>Lo que anuncia TalkBack al cambiar de maniobra: {0} distancia, {1} la tarjeta en minúscula ("En 120 m, gire…").</summary>
        public const string RutaAnuncioManiobra = "ruta.anuncio.maniobra";
        /// <summary>Toast si falla el recálculo ("No se ha podido recalcular la ruta").</summary>
        public const string ToastRecalculoFallo = "toast.ruta.recalculo";
        /// <summary>Notificación fija de la ruta: {0} destino, {1} minutos ("Navegando a Clínica Areas · 6 min").</summary>
        public const string NotificacionNavegando = "notificacion.navegando";

        // La cámara sigue al usuario (historia 2.7, parte 2b)
        /// <summary>Botón de ubicación navegando, con la cámara suelta ("Volver a centrar").</summary>
        public const string UbicacionRecentrar = "ubicacion.recentrar";
        /// <summary>Botón de ubicación navegando, mientras la cámara sigue ("Siguiendo tu posición").</summary>
        public const string UbicacionSiguiendo = "ubicacion.siguiendo";

        // Flecha y sin señal (historia 2.7, parte 2c)
        /// <summary>Pastilla bajo la tarjeta de arriba y anuncio de TalkBack al perder el GPS ("Buscando señal GPS").</summary>
        public const string SenalBuscando = "navegacion.senal.buscando";
        /// <summary>Anuncio de TalkBack al volver el GPS ("Señal GPS recuperada").</summary>
        public const string SenalRecuperada = "navegacion.senal.recuperada";

        // Indicaciones por voz (historia 2.8)
        /// <summary>Botón de voz en TalkBack ("Indicaciones por voz"; su valor es el modo).</summary>
        public const string VozBoton = "voz.boton";
        /// <summary>Modo con todos los avisos ("Todas las indicaciones"; TalkBack de su icono y anuncio al elegirlo).</summary>
        public const string VozTodo = "voz.todo";
        /// <summary>Modo con los avisos importantes ("Solo las importantes").</summary>
        public const string VozImportante = "voz.importante";
        /// <summary>Modo callado ("Sin voz").</summary>
        public const string VozNada = "voz.nada";
        /// <summary>Valor del botón de voz con todos los avisos ("todas").</summary>
        public const string VozValorTodo = "voz.valor.todo";
        /// <summary>Valor con los importantes ("solo las importantes").</summary>
        public const string VozValorImportante = "voz.valor.importante";
        /// <summary>Valor callado ("sin voz").</summary>
        public const string VozValorNada = "voz.valor.nada";

        // Hoja de la ruta que solo informa (22/11/2026)
        /// <summary>Botón rojo de la hoja desplegada durante la navegación ("Finalizar ruta").</summary>
        public const string RutaFinalizar = "ruta.finalizar";
        /// <summary>Valor de TalkBack de la hoja plegada: su acción ("Desplegar").</summary>
        public const string RutaDesplegar = "ruta.desplegar";
        /// <summary>Valor de TalkBack de la hoja desplegada: su acción ("Plegar").</summary>
        public const string RutaPlegar = "ruta.plegar";
        /// <summary>Modo en la fila del destino ("en coche").</summary>
        public const string RutaEnCoche = "ruta.encoche";
        /// <summary>Modo en la fila del destino ("a pie").</summary>
        public const string RutaAPie = "ruta.apie";
        /// <summary>Detalle de la fila del destino: {0} categoría, {1} modo ("Clínica privada · en coche").</summary>
        public const string RutaDestinoDetalle = "ruta.destino.detalle";
        /// <summary>TalkBack de la fila del destino: {0} nombre, {1} detalle ("Destino: Clínica Areas, Clínica privada, en coche").</summary>
        public const string RutaAnuncioDestino = "ruta.anuncio.destino";

        // Hoja «Cómo llegar» (parte 5b, 23/11/2026)
        /// <summary>Título de la hoja.</summary>
        public const string ComoLlegarTitulo = "comollegar.titulo";
        /// <summary>Debajo del título: {0} destino ("a Mesón El Rancho").</summary>
        public const string ComoLlegarA = "comollegar.a";
        /// <summary>La X de la hoja para TalkBack.</summary>
        public const string ComoLlegarCerrar = "comollegar.cerrar";
        /// <summary>Mientras llegan las rutas ("Calculando…").</summary>
        public const string ComoLlegarCalculando = "comollegar.calculando";
        /// <summary>Detalle de una ruta: {0} hora de llegada, {1} distancia ("Llegada 22:54 · 2,8 km").</summary>
        public const string ComoLlegarDetalle = "comollegar.detalle";
        /// <summary>Detalle de la más rápida: {0} el detalle ("Llegada 22:54 · 2,8 km · Más rápida").</summary>
        public const string ComoLlegarMasRapida = "comollegar.masrapida";
        /// <summary>Botón de cada ruta ("Iniciar").</summary>
        public const string ComoLlegarIniciar = "comollegar.iniciar";
        /// <summary>TalkBack de una ruta: {0} minutos, {1} hora, {2} distancia ("7 minutos, llegada 22:54, 2,8 km").</summary>
        public const string ComoLlegarAnuncio = "comollegar.anuncio";
        /// <summary>Igual con un minuto: {0} hora, {1} distancia.</summary>
        public const string ComoLlegarAnuncioMinuto = "comollegar.anuncio.minuto";
        /// <summary>TalkBack de la ruta aproximada: {0} minutos, {1} hora, {2} distancia.</summary>
        public const string ComoLlegarAnuncioAprox = "comollegar.anuncio.aprox";
        /// <summary>Igual con un minuto: {0} hora, {1} distancia.</summary>
        public const string ComoLlegarAnuncioAproxMinuto = "comollegar.anuncio.aprox.minuto";
        /// <summary>TalkBack de la más rápida: {0} el anuncio ("…, más rápida").</summary>
        public const string ComoLlegarAnuncioRapida = "comollegar.anuncio.rapida";
        /// <summary>Aviso de TalkBack al elegir una ruta: {0} su anuncio ("…, seleccionada").</summary>
        public const string ComoLlegarElegida = "comollegar.elegida";

        public static readonly string[] Todas =
        {
            ComoLlegarTitulo, ComoLlegarA, ComoLlegarCerrar, ComoLlegarCalculando, ComoLlegarDetalle, ComoLlegarMasRapida,
            ComoLlegarIniciar, ComoLlegarAnuncio, ComoLlegarAnuncioMinuto, ComoLlegarAnuncioAprox, ComoLlegarAnuncioAproxMinuto,
            ComoLlegarAnuncioRapida, ComoLlegarElegida,
            RutaFinalizar, RutaDesplegar, RutaPlegar, RutaEnCoche, RutaAPie, RutaDestinoDetalle, RutaAnuncioDestino,
            VozBoton, VozTodo, VozImportante, VozNada, VozValorTodo, VozValorImportante, VozValorNada,
            SenalBuscando, SenalRecuperada,
            UbicacionRecentrar, UbicacionSiguiendo,
            RutaCalculando, RutaFallo, RutaA, RutaDespues, RutaLlegada, RutaMin, RutaAnuncio, RutaAnuncioMinuto,
            RutaAnuncioCalculando, RutaQuitar,
            RutaAproximada, RutaValorAprox, RutaAnuncioAprox, RutaAnuncioAproxMinuto, FichaMinutosAprox,
            FichaAnuncioMinutosAprox, FichaAnuncioMinutoAprox, ToastRutaAproximada, ToastSinCalles,
            RutaEn, RutaQuedan, RutaAnuncioManiobra, ToastRecalculoFallo, NotificacionNavegando,
            FichaModoCoche, FichaModoPie, FichaIr, FichaIrA, FichaIrPunto, FichaMinutos, FichaCalculando, FichaSinTiempo,
            FichaAnuncioMinutos, FichaAnuncioMinuto, FichaAnuncioSinTiempo, FichaAnuncioCalculando, FichaModoElegido,
            BuscadorIrAPunto, BuscadorAnuncioPunto, BuscadorComprobandoEnlace, BuscadorEnlaceNoReconocido,
            PuntoTipo, PuntoCoordenadas, PuntoCalleNumero, PuntoBuscando, PuntoAnuncio,
            BuscadorCercaCategoria, BuscadorCercaTitulo, BuscadorCercaDesdeCentro, BuscadorCercaVacio, BuscadorQuitarCategoria,
            PildoraQuitarCategoria, BuscadorCercaJuntar,
            CercaNombreComer, CercaNombreBares, CercaNombreSuper, CercaNombreFarmacia, CercaNombreGasolina, CercaNombreDeporte,
            CercaNombreSalud, CercaNombreCultura,
            BuscadorAnuncioDireccion, BuscadorDireccion, BuscadorDistanciaA, BuscadorDireccionesSinConexion, BuscadorBuscarDireccion,
            BuscadorCampo, BuscadorCerca, BuscadorExpandir, BuscadorContraer, BuscadorComer, BuscadorBares, BuscadorSuper, BuscadorFarmacia,
            BuscadorGasolina, BuscadorDeporte, BuscadorSalud, BuscadorCultura,
            BuscadorSinResultados, BuscadorUnResultado, BuscadorResultados, BuscadorBorrar, BuscadorDetalle,
            BuscadorAnuncioResultado, BuscadorAnuncioResultadoPrecio,
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
