using System;
using System.Collections.Generic;
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
        /// <summary>Variantes con importe o "Consultar", en el orden de los datos (historia 1.8, parte 2).</summary>
        public IReadOnlyList<FilaVariante> Variantes => variantes;
        readonly List<FilaVariante> variantes = new List<FilaVariante>();

        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        /// <param name="ahora">"Ahora" del Arranque (UTC) con el que se calcula el precio de cada variante; sin él no se muestran.</param>
        /// <param name="horaLocal">"Ahora" en la hora local del móvil: con él se elige la vigente entre las variantes que se muestran.</param>
        public FilaConcepto(Concepto concepto, PrecioMostrado precio, Func<string, string> textos = null,
            DateTime? ahora = null, DateTime? horaLocal = null)
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
            // Las variantes, debajo del nombre (como la maqueta), cada una con su condición y su precio
            if (ahora.HasValue)
            {
                var mostradas = new List<(Variante v, PrecioMostrado p)>();
                foreach (var v in concepto.Variantes)
                {
                    if (v?.Condicion == null || v.Fuentes == null) continue;
                    var pv = CalculadoraPrecio.Calcular(v.Fuentes, ahora.Value);
                    if (!pv.TienePrecio && !pv.EsConsultar) continue; // sin nada que mostrar no se inventa nada
                    mostradas.Add((v, pv));
                }
                // La vigente, solo entre las que se ven: una oculta no puede quitarle el chip a otra
                var vigente = horaLocal.HasValue ? ReglaVariantes.Vigente(mostradas.ConvertAll(m => m.v), horaLocal.Value) : null;
                foreach (var (v, pv) in mostradas)
                {
                    var fila = new FilaVariante(v, pv, ReferenceEquals(v, vigente), textos);
                    variantes.Add(fila);
                    izquierda.Add(fila);
                }
            }
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

            Anuncio = TextoAnuncio(concepto.Nombre, precio, Unidad != null ? concepto.Unidad.Trim() : null, textos);
        }

        /// <summary>
        /// Frase de TalkBack del concepto (historia 1.8, parte 3), sin sus variantes: "Sesión de fisioterapia, desde
        /// 40 € por sesión. Publicado en su web el 13/11/2026. Al día". En "Consultar", sin unidad ni frescura.
        /// </summary>
        public string Anuncio { get; }

        /// <param name="unidad">Unidad que se lee ("sesión"); null si la fila no la muestra.</param>
        public static string TextoAnuncio(string nombre, PrecioMostrado precio, string unidad, Func<string, string> textos)
        {
            string importe = TextoPrecio(precio, textos);
            if (precio.TienePrecio && !string.IsNullOrWhiteSpace(unidad))
                importe = Textos.Formatear(textos(ClavesTexto.FichaAnuncioUnidad), importe, unidad);
            string frase = Textos.Formatear(textos(ClavesTexto.FichaAnuncioConcepto), nombre, importe);
            string origen = TextoOrigenAnuncio(precio.Origen, textos);
            if (!string.IsNullOrEmpty(origen)) frase = Textos.Formatear(textos(ClavesTexto.FichaAnuncioFrase), frase, origen);
            if (precio.Frescura.HasValue)
                frase = Textos.Formatear(textos(ClavesTexto.FichaAnuncioFrase), frase, textos(ClaveFrescura(precio.Frescura.Value.Estado)));
            return frase;
        }

        /// <summary>Origen para TalkBack: "Publicado en su web el 13/11/2026" (sin el "·" de la ficha); los demás, igual.</summary>
        public static string TextoOrigenAnuncio(Origen origen, Func<string, string> textos)
        {
            if (origen.Tipo != TipoOrigen.Web) return TextoOrigen(origen, textos);
            string fecha = origen.FechaWeb?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
            return Textos.Formatear(textos(ClavesTexto.FichaAnuncioOrigenWeb), fecha);
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

        /// <summary>
        /// Condición de una variante: "De 20:00 a 24:00"; "Lun a vie" si los días son seguidos o "Lun, mar y vie";
        /// "Con bono" para con-bono y, para otro tipo de cliente, su texto con la primera letra en mayúscula.
        /// </summary>
        public static string TextoCondicion(CondicionVariante condicion, Func<string, string> textos)
        {
            if (condicion == null) return "";
            switch (condicion.Tipo)
            {
                case TipoCondicion.Horario:
                    return Textos.Formatear(textos(ClavesTexto.VarianteHorario), Hora(condicion.Desde, false), Hora(condicion.Hasta, true));
                case TipoCondicion.Dia:
                    return Mayuscula(TextoDias(condicion.Dias, textos));
                default:
                    var tipo = condicion.TipoCliente.Trim();
                    return string.Equals(tipo, "con-bono", StringComparison.OrdinalIgnoreCase)
                        ? textos(ClavesTexto.VarianteConBono)
                        : Mayuscula(tipo);
            }
        }

        // 00:00 al final de una franja es la medianoche del día: "De 20:00 a 24:00"
        static string Hora(TimeSpan t, bool esFinal) =>
            esFinal && t == TimeSpan.Zero ? "24:00" : $"{t.Hours:00}:{t.Minutes:00}";

        // Lunes primero, como en España
        static readonly DayOfWeek[] Semana =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
        };

        static string ClaveDia(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => ClavesTexto.DiaLunes,
            DayOfWeek.Tuesday => ClavesTexto.DiaMartes,
            DayOfWeek.Wednesday => ClavesTexto.DiaMiercoles,
            DayOfWeek.Thursday => ClavesTexto.DiaJueves,
            DayOfWeek.Friday => ClavesTexto.DiaViernes,
            DayOfWeek.Saturday => ClavesTexto.DiaSabado,
            _ => ClavesTexto.DiaDomingo,
        };

        /// <summary>"lun a vie" con tres o más días seguidos; si no, "lun, mar y vie" (en minúsculas).</summary>
        static string TextoDias(IReadOnlyCollection<DayOfWeek> dias, Func<string, string> textos)
        {
            var indices = new List<int>();
            for (int i = 0; i < Semana.Length; i++)
                foreach (var d in dias) if (d == Semana[i]) { indices.Add(i); break; }
            if (indices.Count == 0) return "";
            var nombres = indices.ConvertAll(i => textos(ClaveDia(Semana[i])));
            bool seguidos = indices.Count >= 3 && indices[indices.Count - 1] - indices[0] == indices.Count - 1;
            if (seguidos) return Textos.Formatear(textos(ClavesTexto.VarianteDiasSeguidos), nombres[0], nombres[nombres.Count - 1]);
            if (nombres.Count == 1) return nombres[0];
            string anteriores = string.Join(", ", nombres.GetRange(0, nombres.Count - 1));
            return Textos.Formatear(textos(ClavesTexto.VarianteDiasLista), anteriores, nombres[nombres.Count - 1]);
        }

        static string Mayuscula(string texto) =>
            string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto.Substring(1);

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
