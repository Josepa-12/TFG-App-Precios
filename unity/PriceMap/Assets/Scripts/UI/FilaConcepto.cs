using System;
using System.Globalization;
using PriceMap.Dominio;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Fila de un concepto en la ficha del lugar (historia 1.8; maqueta key-ficha-lugar.html · .concept): a la
    /// izquierda el nombre, el origen ("Publicado en su web · 13/11/2026") y, si hay importe, el punto de frescura
    /// con su texto; a la derecha el precio ("desde 40 €" o "Consultar") y la unidad ("/sesión"). Todo sale del
    /// PrecioMostrado que calcula el Dominio (AD-3); no captura toques.
    /// </summary>
    public class FilaConcepto : VisualElement
    {
        public Concepto Concepto { get; }
        public PrecioMostrado PrecioMostrado { get; }

        public Label Nombre { get; }
        public Label Origen { get; }
        public Label Precio { get; }
        /// <summary>Unidad ("/sesión"); null si no la hay o no hay importe.</summary>
        public Label Unidad { get; }
        /// <summary>Punto y texto de frescura; null en "Consultar" (sin importe no hay frescura).</summary>
        public VisualElement PuntoFrescura { get; }
        public Label Frescura { get; }

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public FilaConcepto(Concepto concepto, PrecioMostrado precio, Func<string, string> textos = null)
        {
            Concepto = concepto ?? throw new ArgumentNullException(nameof(concepto));
            PrecioMostrado = precio ?? throw new ArgumentNullException(nameof(precio));
            textos ??= Textos.T;
            name = "concepto-" + concepto.Id;
            AddToClassList("ficha__concepto");
            pickingMode = PickingMode.Ignore;

            var izquierda = Nuevo("ficha__concepto-izquierda");
            Add(izquierda);
            Nombre = Texto(concepto.Nombre, "ficha__concepto-nombre", "t-label");
            izquierda.Add(Nombre);
            Origen = Texto(TextoOrigen(precio.Origen, textos), "ficha__origen", "t-caption");
            izquierda.Add(Origen);
            if (precio.Frescura.HasValue)
            {
                var linea = Nuevo("ficha__frescura");
                izquierda.Add(linea);
                var estado = precio.Frescura.Value.Estado;
                PuntoFrescura = Nuevo("ficha__frescura-punto");
                PuntoFrescura.AddToClassList(ClaseFrescura(estado));
                linea.Add(PuntoFrescura);
                Frescura = Texto(textos(ClaveFrescura(estado)), "ficha__frescura-texto", "t-caption");
                linea.Add(Frescura);
            }

            var derecha = Nuevo("ficha__concepto-derecha");
            Add(derecha);
            Precio = Texto(TextoPrecio(precio, textos), "ficha__precio", precio.TienePrecio ? "t-price" : "t-label");
            if (!precio.TienePrecio) Precio.AddToClassList("ficha__precio--consultar");
            derecha.Add(Precio);
            // En un rango el texto ya dice "por persona": sin la unidad repetida debajo
            if (precio.TienePrecio && precio.Valor.Value.Forma != FormaPrecio.Rango
                && !string.IsNullOrWhiteSpace(concepto.Unidad) && MuestraUnidad(concepto.Unidad))
            {
                Unidad = Texto(Textos.Formatear(textos(ClavesTexto.FichaUnidad), concepto.Unidad), "ficha__unidad", "t-caption");
                derecha.Add(Unidad);
            }
        }

        /// <summary>"desde 40 €", "90 €", "10–20 € por persona" o "Consultar".</summary>
        public static string TextoPrecio(PrecioMostrado precio, Func<string, string> textos) =>
            precio.TienePrecio ? FormatoPrecio.Precio(precio.Valor.Value, textos) : textos(ClavesTexto.FichaConsultar);

        /// <summary>"Publicado en su web · 13/11/2026", "Declarado por el negocio" o "Según lo que pagaron 3 clientes".</summary>
        public static string TextoOrigen(Origen origen, Func<string, string> textos)
        {
            switch (origen.Tipo)
            {
                case TipoOrigen.Web:
                    string fecha = origen.FechaWeb?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
                    return Textos.Formatear(textos(ClavesTexto.FichaOrigenWeb), fecha);
                case TipoOrigen.Declarado: return textos(ClavesTexto.FichaOrigenDeclarado);
                case TipoOrigen.Clientes: return Textos.Formatear(textos(ClavesTexto.FichaOrigenClientes), origen.NClientes.ToString(CultureInfo.InvariantCulture));
                default: return "";
            }
        }

        public static string ClaveFrescura(EstadoFrescura estado) => estado switch
        {
            EstadoFrescura.PodriaHaberCambiado => ClavesTexto.FrescuraPodria,
            EstadoFrescura.Antiguo => ClavesTexto.FrescuraAntiguo,
            _ => ClavesTexto.FrescuraAlDia,
        };

        /// <summary>Clase del punto con su color (--color-fresh-*).</summary>
        public static string ClaseFrescura(EstadoFrescura estado) => estado switch
        {
            EstadoFrescura.PodriaHaberCambiado => "ficha__frescura-punto--podria",
            EstadoFrescura.Antiguo => "ficha__frescura-punto--antiguo",
            _ => "ficha__frescura-punto--al-dia",
        };

        /// <summary>
        /// "servicio" no se muestra: cobrar "por servicio" no dice nada más que el propio precio (como el prototipo,
        /// app.js · UNIT, donde servicio es '').
        /// </summary>
        static bool MuestraUnidad(string unidad) => !string.Equals(unidad.Trim(), "servicio", StringComparison.OrdinalIgnoreCase);

        static VisualElement Nuevo(string clase)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(clase);
            return e;
        }

        static Label Texto(string texto, string clase, string tipografia)
        {
            var l = new Label(texto) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            return l;
        }
    }
}
