using System;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Fila de un resultado del buscador (historia 2.3; prototipo .result): el icono del tipo en un círculo de 36 dp
    /// (el de su categoría en los puntos de interés), el nombre, debajo el tipo y la distancia ("Clínica privada ·
    /// 350 m") y a la derecha el precio resumido si lo tiene ("desde 20 €"). Se toca entera.
    /// </summary>
    public class FilaResultado : VisualElement
    {
        public ResultadoBusqueda Resultado { get; }
        public VisualElement Icono { get; }
        public VisualElement Glifo { get; }
        public Label Nombre { get; }
        public Label Detalle { get; }
        /// <summary>Precio resumido; null si el resultado no tiene precio.</summary>
        public Label Precio { get; }
        /// <summary>Lo que lee TalkBack: "Clínica Areas, Clínica privada, a 350 m, desde 20 €".</summary>
        public string Anuncio { get; }

        /// <param name="alTocar">Lo que se hace al tocar la fila.</param>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public FilaResultado(ResultadoBusqueda resultado, Action<ResultadoBusqueda> alTocar, Func<string, string> textos = null)
        {
            Resultado = resultado ?? throw new ArgumentNullException(nameof(resultado));
            textos ??= Textos.T;
            name = "buscador-resultado";
            AddToClassList("buscador__resultado");
            pickingMode = PickingMode.Position;
            if (alTocar != null) this.AddManipulator(new Clickable(() => alTocar(resultado)));

            Icono = new VisualElement { name = "buscador-resultado-icono", pickingMode = PickingMode.Ignore };
            Icono.AddToClassList("buscador__resultado-icono");
            Icono.AddToClassList(resultado.Tipo == TipoResultado.Lugar ? "buscador__resultado-icono--lugar"
                : resultado.Tipo == TipoResultado.Direccion ? "buscador__resultado-icono--direccion"
                : resultado.Tipo == TipoResultado.Coordenada ? ClaseIconoCoordenada
                : "buscador__resultado-icono--" + HojaBuscador.Nombre(resultado.Categoria));
            string glifo = GlifoDe(resultado);
            Glifo = new VisualElement { name = "buscador-resultado-glifo", pickingMode = PickingMode.Ignore };
            Glifo.AddToClassList("buscador__resultado-glifo");
            if (glifo == GlifoChincheta)
            {
                // La chincheta en negativo (maqueta del 20/11/2026): la gota del color del glifo y su centro del color
                // del círculo
                Glifo.AddToClassList("buscador__resultado-glifo--chincheta");
                Glifo.generateVisualContent += mgc =>
                    ChinchetaPunto.Dibujar(mgc, Glifo.contentRect, Glifo.resolvedStyle.color, Icono.resolvedStyle.backgroundColor, 0f);
            }
            else
                Glifo.generateVisualContent += mgc => IconosLugar.Dibujar(mgc.painter2D, glifo, Glifo.contentRect, Glifo.resolvedStyle.color);
            Glifo.RegisterCallback<GeometryChangedEvent>(_ => Glifo.MarkDirtyRepaint());
            Icono.Add(Glifo);
            Add(Icono);

            var textosFila = new VisualElement { name = "buscador-resultado-textos", pickingMode = PickingMode.Ignore };
            textosFila.AddToClassList("buscador__resultado-textos");
            bool coordenada = resultado.Tipo == TipoResultado.Coordenada;
            string nombre = coordenada ? textos(ClavesTexto.BuscadorIrAPunto) : resultado.Nombre;
            Nombre = Etiqueta(nombre, "buscador-resultado-nombre", "buscador__resultado-nombre", "t-label");
            textosFila.Add(Nombre);
            string tipo = TipoDe(resultado, textos);
            string distancia = FormatoDistancia.Texto(resultado.Metros);
            Detalle = Etiqueta(DetalleDe(resultado, tipo, distancia, textos),
                "buscador-resultado-detalle", "buscador__resultado-detalle", "t-caption");
            textosFila.Add(Detalle);
            Add(textosFila);

            string precio = resultado.Precio.HasValue ? FormatoPrecio.Precio(resultado.Precio.Value, textos) : null;
            if (precio != null)
            {
                Precio = Etiqueta(precio, "buscador-resultado-precio", "buscador__resultado-precio", "t-meta-fuerte");
                Add(Precio);
            }

            string localidad = resultado.Direccion?.Localidad;
            Anuncio = coordenada
                // "Ir a este punto, 36,7781, -6,3515"
                ? Textos.Formatear(textos(ClavesTexto.BuscadorAnuncioPunto), nombre, Detalle.text)
                : resultado.Tipo == TipoResultado.Direccion
                // "Calle Ancha 10, Dirección, Sanlúcar de Barrameda, a 1,2 km" (sin localidad: "…, Dirección, a 1,2 km")
                ? string.IsNullOrEmpty(localidad)
                    ? Textos.Formatear(textos(ClavesTexto.BuscadorAnuncioResultado), resultado.Nombre, Mayuscula(tipo), distancia)
                    : Textos.Formatear(textos(ClavesTexto.BuscadorAnuncioDireccion), resultado.Nombre, Mayuscula(tipo), localidad, distancia)
                : precio != null
                ? Textos.Formatear(textos(ClavesTexto.BuscadorAnuncioResultadoPrecio), resultado.Nombre, tipo, distancia, precio)
                : Textos.Formatear(textos(ClavesTexto.BuscadorAnuncioResultado), resultado.Nombre, tipo, distancia);
        }

        /// <summary>Vuelve a dibujar el glifo (el color cambia con el tema).</summary>
        public void Repintar() => Glifo.MarkDirtyRepaint();

        /// <summary>Clase del círculo de «Ir a este punto» (historia 2.5, parte 2).</summary>
        public const string ClaseIconoCoordenada = "buscador__resultado-icono--coordenada";
        /// <summary>Lo que da GlifoDe para «Ir a este punto»: no es de IconosLugar, se dibuja la gota de ChinchetaPunto.</summary>
        public const string GlifoChincheta = "chincheta";

        /// <summary>
        /// Glifo del tipo de negocio (lugares), de la categoría (puntos de interés), el pin (direcciones) o la chincheta
        /// (coordenadas, historia 2.5, parte 2).
        /// </summary>
        public static string GlifoDe(ResultadoBusqueda r) =>
            r.Tipo == TipoResultado.Lugar ? IconosLugar.GlifoDe(r.Lugar.Tipo)
            : r.Tipo == TipoResultado.Direccion ? IconosLugar.Generico
            : r.Tipo == TipoResultado.Coordenada ? GlifoChincheta
            : IconosLugar.GlifoDe(r.Categoria);

        /// <summary>
        /// Segunda línea: "Clínica privada · 350 m" en lugares y puntos; en las direcciones (historia 2.3, parte 2)
        /// "Dirección · Sanlúcar de Barrameda · a 1,2 km" (sin localidad, "Dirección · a 1,2 km"); en «Ir a este
        /// punto» (historia 2.5, parte 2), sus coordenadas como en la ficha ("36,7781, -6,3515").
        /// </summary>
        public static string DetalleDe(ResultadoBusqueda r, string tipo, string distancia, Func<string, string> textos)
        {
            if (r.Tipo == TipoResultado.Coordenada) return FichaLugar.TextoCoordenadas(r.Posicion, textos);
            string formato = textos(ClavesTexto.BuscadorDetalle);
            if (r.Tipo != TipoResultado.Direccion) return Textos.Formatear(formato, Mayuscula(tipo), distancia);
            string localidad = r.Direccion?.Localidad;
            string cabeza = string.IsNullOrEmpty(localidad) ? Mayuscula(tipo) : Textos.Formatear(formato, Mayuscula(tipo), localidad);
            return Textos.Formatear(formato, cabeza, Textos.Formatear(textos(ClavesTexto.BuscadorDistanciaA), distancia));
        }

        /// <summary>
        /// Tipo tal como se lee: el del lugar ("Clínica privada") o el nombre de la categoría del punto en minúsculas
        /// ("supermercado"; "centro sanitario público" en la sanidad pública).
        /// </summary>
        public static string TipoDe(ResultadoBusqueda r, Func<string, string> textos)
        {
            if (r.Tipo == TipoResultado.Lugar) return r.Lugar.Tipo;
            if (r.Tipo == TipoResultado.Direccion) return textos(ClavesTexto.BuscadorDireccion).ToLowerInvariant();
            if (r.Tipo == TipoResultado.Coordenada) return "";
            var clave = r.SanidadPublica ? ClavesTexto.PoiSanidadPublica : TextosPuntos.ClaveCategoria(r.Categoria);
            return clave != null ? textos(clave) : "";
        }

        static string Mayuscula(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], System.Globalization.CultureInfo.InvariantCulture) + s.Substring(1);

        static Label Etiqueta(string texto, string nombre, string clase, string tipografia)
        {
            var l = new Label(texto) { name = nombre, pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            return l;
        }
    }
}
