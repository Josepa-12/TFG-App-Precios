using System;
using PriceMap.Dominio;

namespace PriceMap.UI
{
    /// <summary>
    /// Lo que lee TalkBack de cada punto de interés: "Mercadona, supermercado", solo la categoría si no tiene
    /// nombre, y "centro sanitario público" como categoría de la sanidad pública. Los textos vienen de Localization.
    /// </summary>
    public static class TextosPuntos
    {
        /// <summary>Clave del nombre de la categoría en minúsculas ("supermercado"); null con Ninguna.</summary>
        public static string ClaveCategoria(CategoriaPoi categoria)
        {
            switch (categoria)
            {
                case CategoriaPoi.Comer: return ClavesTexto.PoiComer;
                case CategoriaPoi.Rapida: return ClavesTexto.PoiRapida;
                case CategoriaPoi.Cafe: return ClavesTexto.PoiCafe;
                case CategoriaPoi.Bares: return ClavesTexto.PoiBares;
                case CategoriaPoi.Super: return ClavesTexto.PoiSuper;
                case CategoriaPoi.Tiendas: return ClavesTexto.PoiTiendas;
                case CategoriaPoi.Gasolina: return ClavesTexto.PoiGasolina;
                case CategoriaPoi.Carga: return ClavesTexto.PoiCarga;
                case CategoriaPoi.Parking: return ClavesTexto.PoiParking;
                case CategoriaPoi.Hoteles: return ClavesTexto.PoiHoteles;
                case CategoriaPoi.Farmacia: return ClavesTexto.PoiFarmacia;
                case CategoriaPoi.Salud: return ClavesTexto.PoiSalud;
                case CategoriaPoi.Deporte: return ClavesTexto.PoiDeporte;
                case CategoriaPoi.Parques: return ClavesTexto.PoiParques;
                case CategoriaPoi.Cultura: return ClavesTexto.PoiCultura;
                case CategoriaPoi.Bancos: return ClavesTexto.PoiBancos;
                case CategoriaPoi.Belleza: return ClavesTexto.PoiBelleza;
                case CategoriaPoi.Talleres: return ClavesTexto.PoiTalleres;
                case CategoriaPoi.Transporte: return ClavesTexto.PoiTransporte;
                case CategoriaPoi.Educacion: return ClavesTexto.PoiEducacion;
                case CategoriaPoi.Publico: return ClavesTexto.PoiPublico;
                case CategoriaPoi.Culto: return ClavesTexto.PoiCulto;
                default: return null;
            }
        }

        /// <summary>Texto de TalkBack de la vista de un punto, con los textos de Localization.</summary>
        public static string Anuncio(PuntoInteresVista vista) => Anuncio(vista, Textos.T);

        /// <summary>Igual, con los textos dados (las pruebas usan los es-ES del generador).</summary>
        public static string Anuncio(PuntoInteresVista vista, Func<string, string> textos)
        {
            if (vista == null) throw new ArgumentNullException(nameof(vista));
            return TextoAnuncio(vista.Punto.Nombre, vista.Categoria, vista.SanidadPublica, textos);
        }

        /// <summary>"{nombre}, {categoría}"; sin nombre, solo la categoría. La sanidad pública tiene su propia categoría.</summary>
        public static string TextoAnuncio(string nombre, CategoriaPoi categoria, bool sanidadPublica, Func<string, string> textos)
        {
            if (textos == null) throw new ArgumentNullException(nameof(textos));
            var clave = sanidadPublica ? ClavesTexto.PoiSanidadPublica : ClaveCategoria(categoria);
            string cat = clave != null ? textos(clave) : "";
            if (string.IsNullOrWhiteSpace(nombre)) return cat;
            if (string.IsNullOrWhiteSpace(cat)) return nombre.Trim(); // sin categoría: sin separador colgando
            return Textos.Formatear(textos(ClavesTexto.PoiAnuncio), nombre.Trim(), cat);
        }
    }
}
