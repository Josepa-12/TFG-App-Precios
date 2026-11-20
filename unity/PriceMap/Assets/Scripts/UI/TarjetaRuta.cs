using System;
using System.Globalization;
using PriceMap.Dominio;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Tarjeta de la ruta abajo (historia 2.6, parte 2a; maqueta v2 del 20/11/2026): la hora de llegada, los minutos y la
    /// distancia en tres columnas (valor grande y rótulo debajo: "13:34 / Llegada", "4 / min", "1,2 / km") y la X
    /// "Quitar ruta" de 48 dp. Tocar el cuerpo reabre la ficha. Va anclada abajo, de borde a borde, con las esquinas de
    /// arriba de hoja; su relleno de abajo deja sitio a la barra de gestos (PonerMargenInferior). Mientras llega la ruta,
    /// "…" en las tres; si falla, "—".
    /// </summary>
    public class TarjetaRuta : VisualElement
    {
        readonly Func<string, string> textos;

        /// <summary>Lo que se toca para reabrir la ficha (botón de TalkBack con Anuncio).</summary>
        public VisualElement Cuerpo { get; }
        public Label Llegada { get; }
        public Label RotuloLlegada { get; }
        public Label Minutos { get; }
        public Label RotuloMinutos { get; }
        public Label Distancia { get; }
        public Label RotuloDistancia { get; }
        /// <summary>X "Quitar ruta" (48 dp).</summary>
        public BotonRedondo Quitar { get; }

        /// <summary>Lo que lee TalkBack en el cuerpo: "Llegada 13:34, 4 minutos, 1,2 km".</summary>
        public string Anuncio { get; private set; } = "";

        /// <summary>El cuerpo se ha tocado (toque, teclado o TalkBack).</summary>
        public event Action Tocada;
        /// <summary>La X se ha pulsado.</summary>
        public event Action QuitarPulsado;

        public TarjetaRuta(Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "tarjeta-ruta";
            AddToClassList("tarjeta-ruta");
            pickingMode = PickingMode.Position; // arrastrar sobre la tarjeta no mueve el mapa

            var asa = new VisualElement { name = "tarjeta-ruta-asa", pickingMode = PickingMode.Ignore };
            asa.AddToClassList("tarjeta-ruta__asa");
            Add(asa);

            var fila = new VisualElement { name = "tarjeta-ruta-fila", pickingMode = PickingMode.Ignore };
            fila.AddToClassList("tarjeta-ruta__fila");
            Add(fila);

            Cuerpo = new VisualElement { name = "tarjeta-ruta-cuerpo", focusable = true, pickingMode = PickingMode.Position };
            Cuerpo.AddToClassList("tarjeta-ruta__cuerpo");
            Cuerpo.AddManipulator(new Clickable(Tocar));
            Cuerpo.RegisterCallback<NavigationSubmitEvent>(_ => Tocar());
            fila.Add(Cuerpo);
            (Llegada, RotuloLlegada) = Columna("llegada", this.textos(ClavesTexto.RutaLlegada));
            (Minutos, RotuloMinutos) = Columna("minutos", this.textos(ClavesTexto.RutaMin));
            (Distancia, RotuloDistancia) = Columna("distancia", "km");

            Quitar = new BotonRedondo(Icono.Cerrar, PulsarQuitar) { name = "tarjeta-ruta-quitar" };
            Quitar.AddToClassList("tarjeta-ruta__quitar");
            fila.Add(Quitar);

            MostrarCalculando();
        }

        (Label valor, Label rotulo) Columna(string nombre, string rotulo)
        {
            var col = new VisualElement { name = "tarjeta-ruta-" + nombre, pickingMode = PickingMode.Ignore };
            col.AddToClassList("tarjeta-ruta__columna");
            Cuerpo.Add(col);
            var valor = new Label { name = "tarjeta-ruta-" + nombre + "-valor", pickingMode = PickingMode.Ignore };
            valor.AddToClassList("tarjeta-ruta__valor");
            valor.AddToClassList("t-price");
            col.Add(valor);
            var r = new Label(rotulo) { name = "tarjeta-ruta-" + nombre + "-rotulo", pickingMode = PickingMode.Ignore };
            r.AddToClassList("tarjeta-ruta__rotulo");
            r.AddToClassList("t-meta");
            col.Add(r);
            return (valor, r);
        }

        public void Tocar() => Tocada?.Invoke();
        public void PulsarQuitar() => QuitarPulsado?.Invoke();

        /// <summary>Mientras llega la ruta: "…" en las tres columnas.</summary>
        public void MostrarCalculando()
        {
            Poner(textos(ClavesTexto.FichaCalculando), textos(ClavesTexto.FichaCalculando), textos(ClavesTexto.FichaCalculando), "km");
            Anuncio = textos(ClavesTexto.RutaAnuncioCalculando);
        }

        /// <summary>Sin ruta: "—" en las tres columnas.</summary>
        public void MostrarFallo()
        {
            Poner(textos(ClavesTexto.FichaSinTiempo), textos(ClavesTexto.FichaSinTiempo), textos(ClavesTexto.FichaSinTiempo), "km");
            Anuncio = textos(ClavesTexto.RutaAnuncioFallo);
        }

        /// <summary>La ruta llegada, con la hora local de ahora para la llegada.</summary>
        public void MostrarRuta(Ruta ruta, DateTime ahoraLocal)
        {
            string hora = HoraLlegada(ahoraLocal, ruta.Segundos);
            var (valor, unidad) = PartirDistancia(ruta.Metros);
            int minutos = ruta.Minutos;
            Poner(hora, minutos.ToString(CultureInfo.InvariantCulture), valor, unidad);
            string distancia = FormatoDistancia.Texto(ruta.Metros).Replace(' ', ' ');
            Anuncio = minutos == 1
                ? Textos.Formatear(textos(ClavesTexto.RutaAnuncioMinuto), hora, distancia)
                : Textos.Formatear(textos(ClavesTexto.RutaAnuncio), hora, minutos, distancia);
        }

        void Poner(string llegada, string minutos, string distancia, string unidad)
        {
            Llegada.text = llegada;
            Minutos.text = minutos;
            Distancia.text = distancia;
            RotuloDistancia.text = unidad;
        }

        /// <summary>Hora local de llegada: ahora + la duración, "HH:mm".</summary>
        public static string HoraLlegada(DateTime ahoraLocal, double segundos)
        {
            if (double.IsNaN(segundos) || segundos < 0) segundos = 0;
            return ahoraLocal.AddSeconds(segundos).ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>La distancia de FormatoDistancia partida en valor y unidad: ("1,2", "km") o ("950", "m").</summary>
        public static (string valor, string unidad) PartirDistancia(double metros)
        {
            var texto = FormatoDistancia.Texto(metros);
            int i = texto.LastIndexOfAny(new[] { ' ', ' ' });
            return i < 0 ? (texto, "") : (texto.Substring(0, i), texto.Substring(i + 1));
        }

        /// <summary>Relleno de abajo: el suyo más la barra de gestos (dp), como la hoja de la ficha.</summary>
        public void PonerMargenInferior(float dp) => style.paddingBottom = RellenoAbajo + (dp > 0f ? dp : 0f);

        /// <summary>Relleno de abajo propio (dp), --espacio-4.</summary>
        public const float RellenoAbajo = 16f;
    }
}
