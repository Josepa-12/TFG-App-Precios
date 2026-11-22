using System.Collections.Generic;
using System.IO;
using PriceMap.UI;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace PriceMap.EditorTools
{
    /// <summary>
    /// Crea (o actualiza) lo que necesita la interfaz y no es código:
    ///  - Localization: ajustes, locale es-ES y la tabla "Textos" con todos los textos de la app (NFR-5);
    ///  - PanelSettings de UI Toolkit: 1 px = 1 dp (tamaño físico constante a 160 dpi) y el tema PriceMap.tss.
    /// Menú: PriceMap ▸ Preparar interfaz. Lo llama también la preparación de la escena.
    /// </summary>
    public static class PrepararInterfaz
    {
        public const string CarpetaLocalizacion = "Assets/Localizacion";
        public const string RutaPanelSettings = "Assets/UI/PanelSettings.asset";
        public const string RutaTema = "Assets/UI/Estilos/PriceMap.tss";
        public const string RutaPantallaMapa = "Assets/UI/PantallaMapa.uxml";
        static readonly LocaleIdentifier Espanol = new LocaleIdentifier("es-ES");

        /// <summary>Textos en castellano (tuteo, frases cortas: EXPERIENCE.md · Voice and Tone).</summary>
        public static readonly Dictionary<string, string> TextosEs = new Dictionary<string, string>
        {
            [ClavesTexto.TemaCambiar] = "Cambiar tema",
            [ClavesTexto.TemaClaro] = "Tema claro",
            [ClavesTexto.TemaOscuro] = "Tema oscuro",
            [ClavesTexto.MapaAcercar] = "Acercar",
            [ClavesTexto.MapaAlejar] = "Alejar",
            [ClavesTexto.MapaAtribucion] = "OpenFreeMap © OpenMapTiles · Datos © colaboradores de OpenStreetMap",
            [ClavesTexto.MarcadorConPrecio] = "{0}, {1}, desde {2}",
            [ClavesTexto.MarcadorSinPrecio] = "{0}, {1}, aún no tiene precio",
            [ClavesTexto.PreciosControl] = "Precios en el mapa",
            [ClavesTexto.PreciosVer] = "Ver precios",
            [ClavesTexto.PreciosTodos] = "Todos los precios",
            [ClavesTexto.PreciosTitulo] = "Mostrar precios en el mapa",
            [ClavesTexto.PreciosOpcionNinguno] = "Ninguno",
            [ClavesTexto.PreciosOpcionTodos] = "Todos",
            [ClavesTexto.PreciosOpcionServicios] = "Servicios locales",
            [ClavesTexto.PreciosOpcionHosteleria] = "Hostelería",
            [ClavesTexto.PreciosOpcionOcio] = "Ocio",
            [ClavesTexto.PreciosAnuncio] = "Precios en el mapa: {0}",
            [ClavesTexto.PoiAnuncio] = "{0}, {1}",
            [ClavesTexto.PoiSanidadPublica] = "centro sanitario público",
            [ClavesTexto.PoiComer] = "restaurante",
            [ClavesTexto.PoiRapida] = "comida rápida",
            [ClavesTexto.PoiCafe] = "cafetería",
            [ClavesTexto.PoiBares] = "bar",
            [ClavesTexto.PoiSuper] = "supermercado",
            [ClavesTexto.PoiTiendas] = "tienda",
            [ClavesTexto.PoiGasolina] = "gasolinera",
            [ClavesTexto.PoiCarga] = "punto de carga",
            [ClavesTexto.PoiParking] = "aparcamiento",
            [ClavesTexto.PoiHoteles] = "alojamiento",
            [ClavesTexto.PoiFarmacia] = "farmacia",
            [ClavesTexto.PoiSalud] = "salud",
            [ClavesTexto.PoiDeporte] = "deporte",
            [ClavesTexto.PoiParques] = "parque",
            [ClavesTexto.PoiCultura] = "cultura",
            [ClavesTexto.PoiBancos] = "banco",
            [ClavesTexto.PoiBelleza] = "peluquería",
            [ClavesTexto.PoiTalleres] = "taller",
            [ClavesTexto.PoiTransporte] = "transporte",
            [ClavesTexto.PoiEducacion] = "centro educativo",
            [ClavesTexto.PoiPublico] = "servicio público",
            [ClavesTexto.PoiCulto] = "lugar de culto",
            [ClavesTexto.UbicacionCentrar] = "Centrar en mi posición",
            [ClavesTexto.UbicacionExplicacion] = "PriceMap usa tu ubicación solo mientras tienes la app abierta, para mostrarte dónde estás en el mapa. No la guarda ni la comparte.",
            [ClavesTexto.UbicacionPermitir] = "Permitir",
            [ClavesTexto.UbicacionAhoraNo] = "Ahora no",
            [ClavesTexto.UbicacionNorte] = "Orientar al norte",
            [ClavesTexto.UbicacionBuscando] = "Buscando tu posición",
            [ClavesTexto.UbicacionDesactivada] = "Ubicación desactivada",
            [ClavesTexto.UbicacionAvisoDenegado] = "La ubicación está desactivada. Puedes activarla en Ajustes > Aplicaciones > PriceMap > Permisos.",
            [ClavesTexto.UbicacionAvisoApagada] = "La ubicación del móvil está apagada. Actívala en los ajustes rápidos del móvil.",
            [ClavesTexto.UbicacionAbrirAjustes] = "Abrir Ajustes",
            [ClavesTexto.UbicacionCerrar] = "Cerrar",
            [ClavesTexto.UbicacionAvisoNotificaciones] = "Sin permiso de notificaciones, la navegación se pausa al apagar la pantalla.",
            [ClavesTexto.FichaCerrar] = "Cerrar ficha",
            [ClavesTexto.FichaTipo] = "{0} · {1}",
            [ClavesTexto.PlantillaServicio] = "Servicio local",
            [ClavesTexto.PlantillaHosteleria] = "Hostelería",
            [ClavesTexto.PlantillaOcio] = "Ocio",
            [ClavesTexto.FichaSinPrecio] = "Aún no tiene precio",
            [ClavesTexto.FichaConsultar] = "Consultar",
            [ClavesTexto.FichaPrecioDesde] = "desde {0}",
            [ClavesTexto.FichaPrecioRango] = "{0}–{1} por persona",
            [ClavesTexto.FichaPrecioRangoAbierto] = "más de {0} por persona",
            [ClavesTexto.FichaUnidad] = "/{0}",
            [ClavesTexto.FichaOrigenWeb] = "Publicado en su web · {0}",
            [ClavesTexto.FichaOrigenDeclarado] = "Declarado por el negocio",
            [ClavesTexto.FichaOrigenClientes] = "Según lo que pagaron {0} clientes",
            [ClavesTexto.FrescuraAlDia] = "Al día",
            [ClavesTexto.FrescuraPodria] = "Podría haber cambiado",
            [ClavesTexto.FrescuraAntiguo] = "Antiguo",
            [ClavesTexto.FichaAhora] = "Ahora",
            [ClavesTexto.VarianteHorario] = "De {0} a {1}",
            [ClavesTexto.VarianteDiasSeguidos] = "{0} a {1}",
            [ClavesTexto.VarianteDiasLista] = "{0} y {1}",
            [ClavesTexto.VarianteConBono] = "Con bono",
            [ClavesTexto.DiaLunes] = "lun",
            [ClavesTexto.DiaMartes] = "mar",
            [ClavesTexto.DiaMiercoles] = "mié",
            [ClavesTexto.DiaJueves] = "jue",
            [ClavesTexto.DiaViernes] = "vie",
            [ClavesTexto.DiaSabado] = "sáb",
            [ClavesTexto.DiaDomingo] = "dom",
            [ClavesTexto.FichaAnuncioConcepto] = "{0}, {1}",
            [ClavesTexto.FichaAnuncioUnidad] = "{0} por {1}",
            [ClavesTexto.FichaAnuncioOrigenWeb] = "Publicado en su web el {0}",
            [ClavesTexto.FichaAnuncioFrase] = "{0}. {1}",
            [ClavesTexto.FichaAnuncioVariante] = "{0}, {1}",
            [ClavesTexto.FichaAnuncioVarianteAhora] = "{0}: {1}, {2}",
            [ClavesTexto.BuscadorCampo] = "Buscar en Sanlúcar",
            [ClavesTexto.BuscadorCerca] = "Encuentra cerca",
            [ClavesTexto.BuscadorExpandir] = "Expandir",
            [ClavesTexto.BuscadorContraer] = "Contraer",
            [ClavesTexto.BuscadorComer] = "Restaurantes",
            [ClavesTexto.BuscadorBares] = "Bares",
            [ClavesTexto.BuscadorSuper] = "Supermercados",
            [ClavesTexto.BuscadorFarmacia] = "Farmacias",
            [ClavesTexto.BuscadorGasolina] = "Gasolineras",
            [ClavesTexto.BuscadorDeporte] = "Deporte",
            [ClavesTexto.BuscadorSalud] = "Salud",
            [ClavesTexto.BuscadorCultura] = "Cultura",
            [ClavesTexto.BuscadorSinResultados] = "No hay resultados",
            [ClavesTexto.BuscadorUnResultado] = "1 resultado",
            [ClavesTexto.BuscadorResultados] = "{0} resultados",
            [ClavesTexto.BuscadorBorrar] = "Borrar búsqueda",
            [ClavesTexto.BuscadorDetalle] = "{0} · {1}",
            [ClavesTexto.BuscadorAnuncioResultado] = "{0}, {1}, a {2}",
            [ClavesTexto.BuscadorAnuncioResultadoPrecio] = "{0}, {1}, a {2}, {3}",
            [ClavesTexto.BuscadorDireccion] = "Dirección",
            [ClavesTexto.BuscadorDistanciaA] = "a {0}",
            [ClavesTexto.BuscadorDireccionesSinConexion] = "Las direcciones necesitan conexión",
            [ClavesTexto.BuscadorBuscarDireccion] = "Buscar como dirección",
            [ClavesTexto.BuscadorAnuncioDireccion] = "{0}, {1}, {2}, a {3}",
            [ClavesTexto.BuscadorCercaCategoria] = "{0} cerca",
            [ClavesTexto.BuscadorCercaTitulo] = "Cerca",
            [ClavesTexto.BuscadorCercaDesdeCentro] = "desde el centro del mapa",
            [ClavesTexto.BuscadorCercaVacio] = "No hay {0} cerca",
            [ClavesTexto.CercaNombreComer] = "restaurantes",
            [ClavesTexto.CercaNombreBares] = "bares",
            [ClavesTexto.CercaNombreSuper] = "supermercados",
            [ClavesTexto.CercaNombreFarmacia] = "farmacias",
            [ClavesTexto.CercaNombreGasolina] = "gasolineras",
            [ClavesTexto.CercaNombreDeporte] = "sitios de deporte",
            [ClavesTexto.CercaNombreSalud] = "sitios de salud",
            [ClavesTexto.CercaNombreCultura] = "sitios de cultura",
            [ClavesTexto.BuscadorQuitarCategoria] = "Quitar categoría",
            [ClavesTexto.PildoraQuitarCategoria] = "Quitar categoría {0}",
            [ClavesTexto.BuscadorCercaJuntar] = "{0}, {1}",
            [ClavesTexto.PuntoTipo] = "Punto en el mapa",
            [ClavesTexto.PuntoCoordenadas] = "{0}, {1}",
            [ClavesTexto.PuntoCalleNumero] = "{0}, {1}",
            [ClavesTexto.PuntoBuscando] = "Buscando la dirección…",
            [ClavesTexto.PuntoAnuncio] = "{0}, {1}",
            [ClavesTexto.BuscadorIrAPunto] = "Ir a este punto",
            [ClavesTexto.BuscadorAnuncioPunto] = "{0}, {1}",
            [ClavesTexto.BuscadorComprobandoEnlace] = "Comprobando el enlace…",
            [ClavesTexto.BuscadorEnlaceNoReconocido] = "No reconozco esta ubicación. Prueba a copiar el enlace completo.",
            [ClavesTexto.FichaModoCoche] = "Coche",
            [ClavesTexto.FichaModoPie] = "A pie",
            [ClavesTexto.FichaIr] = "Ir",
            [ClavesTexto.FichaIrA] = "Ir a {0}",
            [ClavesTexto.FichaIrPunto] = "Ir a este punto",
            [ClavesTexto.FichaMinutos] = "{0} min",
            [ClavesTexto.FichaCalculando] = "…",
            [ClavesTexto.FichaSinTiempo] = "—",
            [ClavesTexto.FichaAnuncioMinutos] = "{0}, {1} minutos",
            [ClavesTexto.FichaAnuncioMinuto] = "{0}, 1 minuto",
            [ClavesTexto.FichaAnuncioSinTiempo] = "{0}, sin tiempo",
            [ClavesTexto.FichaAnuncioCalculando] = "{0}, calculando el tiempo",
            [ClavesTexto.FichaModoElegido] = "{0}, seleccionado",
            [ClavesTexto.RutaCalculando] = "Calculando la ruta…",
            [ClavesTexto.RutaFallo] = "No se ha podido calcular la ruta",
            [ClavesTexto.RutaA] = "Ruta a {0}",
            [ClavesTexto.RutaDespues] = "{0}. Después, {1}",
            [ClavesTexto.RutaLlegada] = "Llegada",
            [ClavesTexto.RutaMin] = "min",
            [ClavesTexto.RutaAnuncio] = "Llegada {0}, {1} minutos, {2}",
            [ClavesTexto.RutaAnuncioMinuto] = "Llegada {0}, 1 minuto, {1}",
            [ClavesTexto.RutaAnuncioCalculando] = "Calculando la hora de llegada",
            [ClavesTexto.RutaAnuncioFallo] = "Sin hora de llegada",
            [ClavesTexto.RutaQuitar] = "Quitar ruta",
            [ClavesTexto.RutaAproximada] = "Ruta aproximada por las calles guardadas",
            [ClavesTexto.RutaValorAprox] = "≈ {0}",
            [ClavesTexto.RutaAnuncioAprox] = "Ruta aproximada. Llegada hacia las {0}, unos {1} minutos, unos {2}",
            [ClavesTexto.RutaAnuncioAproxMinuto] = "Ruta aproximada. Llegada hacia las {0}, alrededor de 1 minuto, unos {1}",
            [ClavesTexto.FichaMinutosAprox] = "≈ {0} min",
            [ClavesTexto.FichaAnuncioMinutosAprox] = "{0}, unos {1} minutos",
            [ClavesTexto.FichaAnuncioMinutoAprox] = "{0}, alrededor de 1 minuto",
            [ClavesTexto.ToastRutaAproximada] = "Sin conexión: ruta aproximada",
            [ClavesTexto.ToastSinCalles] = "Sin conexión: no hay calles guardadas para esta ruta",
            [ClavesTexto.RutaEn] = "En {0}",
            [ClavesTexto.RutaQuedan] = "Quedan {0}",
            [ClavesTexto.RutaAnuncioManiobra] = "{0}, {1}",
            [ClavesTexto.RutaTerminar] = "Terminar ruta",
            [ClavesTexto.ToastRecalculoFallo] = "No se ha podido recalcular la ruta",
            [ClavesTexto.NotificacionNavegando] = "Navegando a {0} · {1} min",
            [ClavesTexto.UbicacionRecentrar] = "Volver a centrar",
            [ClavesTexto.UbicacionSiguiendo] = "Siguiendo tu posición",
            [ClavesTexto.SenalBuscando] = "Buscando señal GPS",
            [ClavesTexto.SenalRecuperada] = "Señal GPS recuperada",
        };

        [MenuItem("PriceMap/Preparar interfaz")]
        public static void Asegurar()
        {
            AsegurarTextos();
            AsegurarPanelSettings();
            AssetDatabase.SaveAssets();
        }

        static void AsegurarTextos()
        {
            Directory.CreateDirectory(CarpetaLocalizacion);

            var ajustes = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (ajustes == null)
            {
                ajustes = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(ajustes, CarpetaLocalizacion + "/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = ajustes;
            }

            var locale = LocalizationEditorSettings.GetLocale(Espanol);
            if (locale == null)
            {
                locale = Locale.CreateLocale(Espanol);
                AssetDatabase.CreateAsset(locale, CarpetaLocalizacion + "/Espanol (es-ES).asset");
                LocalizationEditorSettings.AddLocale(locale);
            }

            // Arranca siempre en es-ES (única locale de la v1)
            var selectores = ajustes.GetStartupLocaleSelectors();
            var especifico = selectores.Find(s => s is SpecificLocaleSelector) as SpecificLocaleSelector;
            if (especifico == null) selectores.Insert(0, new SpecificLocaleSelector { LocaleId = Espanol });
            else especifico.LocaleId = Espanol;
            EditorUtility.SetDirty(ajustes);

            var coleccion = LocalizationEditorSettings.GetStringTableCollection(ClavesTexto.Tabla)
                            ?? LocalizationEditorSettings.CreateStringTableCollection(ClavesTexto.Tabla, CarpetaLocalizacion);
            var tabla = coleccion.GetTable(Espanol) as StringTable ?? coleccion.AddNewTable(Espanol) as StringTable;
            foreach (var par in TextosEs)
            {
                var entrada = tabla.GetEntry(par.Key) ?? tabla.AddEntry(par.Key, par.Value);
                entrada.Value = par.Value;
            }
            EditorUtility.SetDirty(tabla);
            EditorUtility.SetDirty(tabla.SharedData);
        }

        static void AsegurarPanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(RutaPanelSettings);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, RutaPanelSettings);
            }
            panel.scaleMode = PanelScaleMode.ConstantPhysicalSize; // 1 px del panel = 1 dp
            panel.referenceDpi = 160f;
            panel.fallbackDpi = 160f;
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(RutaTema);
            panel.clearColor = false; // el mapa se ve detrás
            EditorUtility.SetDirty(panel);
        }
    }
}
