using System;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Fila de acciones de la ficha (historia 2.6; DESIGN.md y maqueta key-ficha-lugar): dos botones de modo (coche y
    /// a pie: icono vectorial y nombre, los minutos debajo; 48 dp de alto, borde de control; el elegido con borde y
    /// texto de acento) y el botón primario Ir (52 dp, flex 1,2). Coche elegido al abrir. Los minutos van "…"
    /// mientras se calculan, "6 min" con la ruta y "—" sin posición o si falla. La fila no se desplaza: va bajo la
    /// cabecera, fuera del contenido.
    /// </summary>
    public class FilaAcciones : VisualElement
    {
        readonly Func<string, string> textos;

        public BotonModo Coche { get; }
        public BotonModo APie { get; }
        /// <summary>Botón Ir (button-primary).</summary>
        public VisualElement Ir { get; }
        public Label TextoIr { get; }

        public ModoViaje Elegido { get; private set; } = ModoViaje.Coche;
        /// <summary>Lo que lee TalkBack en Ir: "Ir a Clínica Areas" o "Ir a este punto".</summary>
        public string AnuncioIr { get; private set; } = "";

        /// <summary>El usuario ha elegido otro modo (toque, teclado o TalkBack).</summary>
        public event Action<ModoViaje> ModoCambiado;
        /// <summary>El usuario ha pulsado Ir.</summary>
        public event Action IrPulsado;

        public FilaAcciones(Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "acciones";
            AddToClassList("acciones");
            pickingMode = PickingMode.Ignore;

            Coche = new BotonModo(ModoViaje.Coche, this.textos(ClavesTexto.FichaModoCoche), () => Tocar(ModoViaje.Coche)) { name = "acciones-coche" };
            APie = new BotonModo(ModoViaje.APie, this.textos(ClavesTexto.FichaModoPie), () => Tocar(ModoViaje.APie)) { name = "acciones-pie" };
            Add(Coche);
            Add(APie);

            Ir = new VisualElement { name = "acciones-ir", focusable = true, pickingMode = PickingMode.Position };
            Ir.AddToClassList("acciones__ir");
            Ir.AddManipulator(new Clickable(PulsarIr));
            Ir.RegisterCallback<NavigationSubmitEvent>(_ => PulsarIr());
            TextoIr = new Label(this.textos(ClavesTexto.FichaIr)) { name = "acciones-ir-texto", pickingMode = PickingMode.Ignore };
            TextoIr.AddToClassList("acciones__ir-texto");
            TextoIr.AddToClassList("t-label");
            Ir.Add(TextoIr);
            Add(Ir);

            Reiniciar("");
        }

        /// <summary>Al abrir una ficha: coche elegido, los dos sin tiempo y el anuncio de Ir con su destino.</summary>
        public void Reiniciar(string anuncioIr)
        {
            AnuncioIr = anuncioIr ?? "";
            Elegir(ModoViaje.Coche);
            PonerSinTiempo(ModoViaje.Coche);
            PonerSinTiempo(ModoViaje.APie);
        }

        public BotonModo Boton(ModoViaje modo) => modo == ModoViaje.APie ? APie : Coche;

        /// <summary>Elige el modo (sin avisar): borde y texto de acento en el elegido.</summary>
        public void Elegir(ModoViaje modo)
        {
            Elegido = modo;
            Coche.Elegido = modo == ModoViaje.Coche;
            APie.Elegido = modo == ModoViaje.APie;
        }

        /// <summary>Lo que hace un toque en un modo: lo elige y, si ha cambiado, avisa.</summary>
        public void Tocar(ModoViaje modo)
        {
            if (modo == Elegido) return;
            Elegir(modo);
            ModoCambiado?.Invoke(modo);
        }

        public void PulsarIr() => IrPulsado?.Invoke();

        public void PonerCalculando(ModoViaje modo) => Boton(modo).Poner(EstadoTiempo.Calculando, 0, textos);
        public void PonerSinTiempo(ModoViaje modo) => Boton(modo).Poner(EstadoTiempo.SinTiempo, 0, textos);
        /// <summary>Minutos de la ruta de ese modo; aproximados (sin conexión, parte 2b), "≈ 4 min".</summary>
        public void PonerMinutos(ModoViaje modo, int minutos, bool aproximados = false) =>
            Boton(modo).Poner(EstadoTiempo.Minutos, Math.Max(1, minutos), textos, aproximados);

        /// <summary>
        /// Lo que lee TalkBack en un modo: "Coche, 6 minutos", "A pie, 1 minuto", "Coche, sin tiempo" o "Coche,
        /// calculando el tiempo"; aproximados, "Coche, unos 4 minutos". "Seleccionado" lo añade el estado del nodo.
        /// </summary>
        public string Anuncio(ModoViaje modo)
        {
            var b = Boton(modo);
            string nombre = b.Nombre.text;
            switch (b.Estado)
            {
                case EstadoTiempo.Minutos when b.Aproximados:
                    return b.Minutos == 1
                        ? Textos.Formatear(textos(ClavesTexto.FichaAnuncioMinutoAprox), nombre)
                        : Textos.Formatear(textos(ClavesTexto.FichaAnuncioMinutosAprox), nombre, b.Minutos);
                case EstadoTiempo.Minutos:
                    return b.Minutos == 1
                        ? Textos.Formatear(textos(ClavesTexto.FichaAnuncioMinuto), nombre)
                        : Textos.Formatear(textos(ClavesTexto.FichaAnuncioMinutos), nombre, b.Minutos);
                case EstadoTiempo.Calculando:
                    return Textos.Formatear(textos(ClavesTexto.FichaAnuncioCalculando), nombre);
                default:
                    return Textos.Formatear(textos(ClavesTexto.FichaAnuncioSinTiempo), nombre);
            }
        }

        /// <summary>Aviso de TalkBack al cambiar de modo: "A pie, 15 minutos, seleccionado".</summary>
        public string AnuncioElegido(ModoViaje modo) => Textos.Formatear(textos(ClavesTexto.FichaModoElegido), Anuncio(modo));
    }

    public enum EstadoTiempo { SinTiempo, Calculando, Minutos }

    /// <summary>Botón de un modo de la fila de acciones: icono y nombre en una línea, los minutos debajo.</summary>
    public class BotonModo : VisualElement
    {
        public const string ClaseElegido = "acciones__modo--elegido";

        readonly Action alPulsar;

        public ModoViaje Modo { get; }
        public VisualElement IconoModo { get; }
        public Label Nombre { get; }
        public Label Tiempo { get; }
        public EstadoTiempo Estado { get; private set; }
        public int Minutos { get; private set; }
        /// <summary>Los minutos son de la ruta aproximada (sin conexión, parte 2b).</summary>
        public bool Aproximados { get; private set; }

        public bool Elegido
        {
            get => ClassListContains(ClaseElegido);
            set
            {
                EnableInClassList(ClaseElegido, value);
                IconoModo.MarkDirtyRepaint();
            }
        }

        public BotonModo(ModoViaje modo, string nombre, Action alPulsar)
        {
            Modo = modo;
            this.alPulsar = alPulsar;
            AddToClassList("acciones__modo");
            focusable = true;
            pickingMode = PickingMode.Position;
            this.AddManipulator(new Clickable(Pulsar));
            RegisterCallback<NavigationSubmitEvent>(_ => Pulsar());

            var linea = new VisualElement { name = "acciones-modo-linea", pickingMode = PickingMode.Ignore };
            linea.AddToClassList("acciones__modo-linea");
            Add(linea);
            IconoModo = new VisualElement { name = "acciones-modo-icono", pickingMode = PickingMode.Ignore };
            IconoModo.AddToClassList("acciones__modo-icono");
            IconoModo.generateVisualContent += Dibujar;
            // El color cambia con el tema y con el modo elegido: se vuelve a dibujar
            IconoModo.RegisterCallback<GeometryChangedEvent>(_ => IconoModo.MarkDirtyRepaint());
            IconoModo.RegisterCallback<CustomStyleResolvedEvent>(_ => IconoModo.MarkDirtyRepaint());
            linea.Add(IconoModo);
            Nombre = new Label(nombre) { name = "acciones-modo-nombre", pickingMode = PickingMode.Ignore };
            Nombre.AddToClassList("acciones__modo-nombre");
            Nombre.AddToClassList("t-meta-fuerte");
            linea.Add(Nombre);
            Tiempo = new Label { name = "acciones-modo-tiempo", pickingMode = PickingMode.Ignore };
            Tiempo.AddToClassList("acciones__modo-tiempo");
            Tiempo.AddToClassList("t-meta");
            Add(Tiempo);
        }

        public void Pulsar() => alPulsar?.Invoke();

        internal void Poner(EstadoTiempo estado, int minutos, Func<string, string> textos, bool aproximados = false)
        {
            Estado = estado;
            Minutos = estado == EstadoTiempo.Minutos ? minutos : 0;
            Aproximados = estado == EstadoTiempo.Minutos && aproximados;
            Tiempo.text = estado switch
            {
                EstadoTiempo.Minutos when Aproximados => Textos.Formatear(textos(ClavesTexto.FichaMinutosAprox), minutos),
                EstadoTiempo.Minutos => Textos.Formatear(textos(ClavesTexto.FichaMinutos), minutos),
                EstadoTiempo.Calculando => textos(ClavesTexto.FichaCalculando),
                _ => textos(ClavesTexto.FichaSinTiempo),
            };
        }

        /// <summary>Lado del icono (dp): el de la maqueta (18).</summary>
        public const float TamanoIcono = 18f;

        void Dibujar(MeshGenerationContext mgc)
        {
            var p = mgc.painter2D;
            var c = IconoModo.contentRect.center;
            var color = IconoModo.resolvedStyle.color;
            p.strokeColor = color;
            p.fillColor = color;
            p.lineWidth = 2f * TamanoIcono / 24f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            if (Modo == ModoViaje.APie) DibujarPie(p, c); else DibujarCoche(p, c);
        }

        // Punto del viewBox de 24 de la maqueta llevado a un icono de 18 dp centrado en c
        static Vector2 V(Vector2 c, float x, float y) => c + new Vector2(x - 12f, y - 12f) * (TamanoIcono / 24f);

        static void Trazo(Painter2D p, Vector2 c, params float[] xy)
        {
            p.BeginPath();
            p.MoveTo(V(c, xy[0], xy[1]));
            for (int i = 2; i + 1 < xy.Length; i += 2) p.LineTo(V(c, xy[i], xy[i + 1]));
            p.Stroke();
        }

        static void Circulo(Painter2D p, Vector2 c, float x, float y, float r)
        {
            p.BeginPath();
            p.Arc(V(c, x, y), r * TamanoIcono / 24f, 0f, 360f);
            p.ClosePath();
            p.Stroke();
        }

        // Coche de la maqueta: "M5 16V11l2-5h10l2 5v5", "M3 16h18" y dos ruedas de radio 1,5
        static void DibujarCoche(Painter2D p, Vector2 c)
        {
            Trazo(p, c, 5, 16, 5, 11, 7, 6, 17, 6, 19, 11, 19, 16);
            Trazo(p, c, 3, 16, 21, 16);
            Circulo(p, c, 7.5f, 17.5f, 1.5f);
            Circulo(p, c, 16.5f, 17.5f, 1.5f);
        }

        // Persona andando de la maqueta: cabeza (13, 4) de radio 2, "M11 8l-2 6 3 2 1 6", "M11 8l4 3 3 1", "M9 14l-3 6"
        static void DibujarPie(Painter2D p, Vector2 c)
        {
            Circulo(p, c, 13f, 4f, 2f);
            Trazo(p, c, 11, 8, 9, 14, 12, 16, 13, 22);
            Trazo(p, c, 11, 8, 15, 11, 18, 12);
            Trazo(p, c, 9, 14, 6, 20);
        }
    }
}
