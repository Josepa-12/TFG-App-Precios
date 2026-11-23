using System;
using System.Collections.Generic;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Hoja «Cómo llegar» (parte 5b, maqueta A del 22/11/2026): título, "a &lt;destino&gt;" y la X; los modos (coche y a
    /// pie) con sus minutos; y debajo hasta tres rutas, cada una con sus minutos, "Llegada 22:54 · 2,8 km" (y "Más rápida"
    /// en la primera si hay más de una) y su botón «Iniciar». Tocar una tarjeta la elige sin empezar. Mientras llegan,
    /// "Calculando…"; si fallan, el texto del fallo. Anclada abajo, como la ficha, con la lista desplazable si no cabe (texto
    /// grande). Solo enseña y avisa: la abre, la rellena y la cierra PantallaMapa.
    /// </summary>
    public class HojaComoLlegar : VisualElement
    {
        /// <summary>Alto máximo de la hoja como parte de la pantalla (--como-llegar-alto-max).</summary>
        public const float AltoMaximo = 0.6f;

        readonly Func<string, string> textos;
        readonly List<TarjetaOpcion> tarjetas = new List<TarjetaOpcion>();

        public Label Titulo { get; }
        /// <summary>"a Mesón El Rancho".</summary>
        public Label Destino { get; }
        public BotonRedondo BotonCerrar { get; }
        public BotonModo Coche { get; }
        public BotonModo APie { get; }
        public ScrollView Lista { get; }
        /// <summary>"Calculando…" o el texto del fallo; oculto con las rutas.</summary>
        public Label Estado { get; }
        /// <summary>Segunda línea del fallo (sin calles guardadas); oculta en los demás casos.</summary>
        public Label DetalleEstado { get; }
        /// <summary>Hueco bajo la lista con el alto de la barra de gestos.</summary>
        public VisualElement HuecoInferior { get; }
        /// <summary>Las tarjetas (tres, ParametrosNegocio): solo se ven las de las rutas que hay.</summary>
        public IReadOnlyList<TarjetaOpcion> Tarjetas => tarjetas;

        public bool Abierta { get; private set; }
        public ModoViaje Modo { get; private set; }
        /// <summary>Tarjetas a la vista.</summary>
        public int Rutas { get; private set; }
        public int Elegida { get; private set; } = -1;

        public event Action CerrarPulsado;
        /// <summary>Se ha tocado el otro modo.</summary>
        public event Action<ModoViaje> ModoCambiado;
        /// <summary>Se ha tocado la tarjeta con ese índice (elegirla).</summary>
        public event Action<int> RutaTocada;
        /// <summary>Se ha pulsado el «Iniciar» de la tarjeta con ese índice.</summary>
        public event Action<int> IniciarPulsado;

        public HojaComoLlegar(Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "como-llegar";
            AddToClassList("como-llegar");
            pickingMode = PickingMode.Position; // tocar o arrastrar sobre la hoja no mueve el mapa

            Add(Nuevo("como-llegar-asa", "como-llegar__asa"));
            var cabecera = Nuevo("como-llegar-cabecera", "como-llegar__cabecera");
            Add(cabecera);
            var textosCabecera = Nuevo("como-llegar-textos", "como-llegar__textos");
            cabecera.Add(textosCabecera);
            Titulo = Texto("como-llegar-titulo", "como-llegar__titulo", "t-title");
            Titulo.text = this.textos(ClavesTexto.ComoLlegarTitulo);
            textosCabecera.Add(Titulo);
            Destino = Texto("como-llegar-destino", "como-llegar__destino", "t-meta");
            textosCabecera.Add(Destino);
            BotonCerrar = new BotonRedondo(Icono.Cerrar, () => CerrarPulsado?.Invoke()) { name = "como-llegar-cerrar" };
            BotonCerrar.AddToClassList("como-llegar__cerrar");
            cabecera.Add(BotonCerrar);

            var modos = Nuevo("como-llegar-modos", "como-llegar__modos");
            Add(modos);
            Coche = NuevoModo(ModoViaje.Coche, ClavesTexto.FichaModoCoche);
            APie = NuevoModo(ModoViaje.APie, ClavesTexto.FichaModoPie);
            modos.Add(Coche);
            modos.Add(APie);

            // Sin barras de desplazamiento, como la ficha: se arrastra con el dedo
            Lista = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "como-llegar-lista",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            Lista.AddToClassList("como-llegar__lista");
            Add(Lista);
            Estado = Texto("como-llegar-estado", "como-llegar__estado", "t-body");
            Lista.Add(Estado);
            DetalleEstado = Texto("como-llegar-detalle-estado", "como-llegar__detalle-estado", "t-meta");
            Lista.Add(DetalleEstado);
            for (int i = 0; i < ParametrosNegocio.RutaAlternativasMaximo; i++)
            {
                int indice = i;
                var t = new TarjetaOpcion(this.textos, () => RutaTocada?.Invoke(indice), () => IniciarPulsado?.Invoke(indice));
                tarjetas.Add(t);
                Lista.Add(t);
            }
            HuecoInferior = Nuevo("como-llegar-hueco", "como-llegar__hueco");
            Add(HuecoInferior);
            Cerrar();
        }

        BotonModo NuevoModo(ModoViaje modo, string clave)
        {
            var b = new BotonModo(modo, textos(clave), () => TocarModo(modo)) { name = modo == ModoViaje.APie ? "como-llegar-pie" : "como-llegar-coche" };
            // "Coche · 7 min" en una línea (maqueta): el punto va entre el nombre y los minutos
            var punto = Texto("como-llegar-punto", "acciones__modo-tiempo", "t-meta");
            punto.text = "·";
            b.Insert(b.IndexOf(b.Tiempo), punto);
            b.Poner(EstadoTiempo.SinTiempo, 0, textos);
            return b;
        }

        static VisualElement Nuevo(string nombre, string clase)
        {
            var e = new VisualElement { name = nombre, pickingMode = PickingMode.Ignore };
            e.AddToClassList(clase);
            return e;
        }

        internal static Label Texto(string nombre, string clase, string tipografia)
        {
            var l = new Label { name = nombre, pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            return l;
        }

        public BotonModo Boton(ModoViaje modo) => modo == ModoViaje.APie ? APie : Coche;

        /// <summary>Abre la hoja hacia ese destino en ese modo, con "Calculando…".</summary>
        public void Abrir(string destino, ModoViaje modo)
        {
            Abierta = true;
            PonerDestino(destino);
            ElegirModo(modo);
            MostrarCalculando();
            style.display = DisplayStyle.Flex;
        }

        public void Cerrar()
        {
            Abierta = false;
            Vaciar();
            style.display = DisplayStyle.None;
        }

        public void PonerDestino(string destino) => Destino.text = Textos.Formatear(textos(ClavesTexto.ComoLlegarA), destino ?? "");

        /// <summary>Marca el modo (sin avisar).</summary>
        public void ElegirModo(ModoViaje modo)
        {
            Modo = modo;
            Coche.Elegido = modo == ModoViaje.Coche;
            APie.Elegido = modo == ModoViaje.APie;
        }

        /// <summary>Un toque en un modo: si es el otro, avisa.</summary>
        public void TocarModo(ModoViaje modo)
        {
            if (modo != Modo) ModoCambiado?.Invoke(modo);
        }

        public void PonerTiempo(ModoViaje modo, EstadoTiempo estado, int minutos = 0, bool aproximados = false) =>
            Boton(modo).Poner(estado, Math.Max(1, minutos), textos, aproximados);

        /// <summary>Lo que lee TalkBack en un modo ("Coche, 7 minutos").</summary>
        public string AnuncioModo(ModoViaje modo) => Boton(modo).Anuncio(textos);

        /// <summary>Mientras llegan las rutas: "Calculando…", sin tarjetas.</summary>
        public void MostrarCalculando() => PonerEstado(textos(ClavesTexto.ComoLlegarCalculando), null);

        /// <summary>Sin rutas: el texto del fallo (y, si lo hay, por qué), sin tarjetas.</summary>
        public void MostrarFallo(string detalle) => PonerEstado(textos(ClavesTexto.RutaFallo), detalle);

        void PonerEstado(string texto, string detalle)
        {
            Vaciar();
            Estado.text = texto;
            Estado.style.display = DisplayStyle.Flex;
            DetalleEstado.text = detalle ?? "";
            DetalleEstado.style.display = string.IsNullOrEmpty(detalle) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void Vaciar()
        {
            Rutas = 0;
            Elegida = -1;
            foreach (var t in tarjetas) t.Ocultar();
            Estado.style.display = DisplayStyle.None;
            DetalleEstado.style.display = DisplayStyle.None;
            Lista.scrollOffset = Vector2.zero;
        }

        /// <summary>Las rutas llegadas (tres como mucho), con esa elegida y la hora local de ahora para la llegada.</summary>
        public void MostrarRutas(IReadOnlyList<Ruta> rutas, int elegida, DateTime ahoraLocal)
        {
            Vaciar();
            Rutas = Math.Min(rutas.Count, tarjetas.Count);
            for (int i = 0; i < Rutas; i++) tarjetas[i].Mostrar(rutas[i], ahoraLocal, masRapida: i == 0 && Rutas > 1);
            Elegir(elegida);
        }

        /// <summary>Resalta la tarjeta con ese índice (sin avisar).</summary>
        public void Elegir(int indice)
        {
            Elegida = indice;
            for (int i = 0; i < tarjetas.Count; i++) tarjetas[i].Elegida = i == indice && i < Rutas;
            DesplazarHasta(indice);
        }

        /// <summary>Si la lista se desplaza, deja a la vista la tarjeta con ese índice.</summary>
        public void DesplazarHasta(int indice)
        {
            // Sin panel o sin disposición (pruebas, hoja recién abierta), ScrollTo falla
            if (indice < 0 || indice >= Rutas || Lista.panel == null) return;
            float alto = tarjetas[indice].layout.height;
            if (!float.IsNaN(alto) && alto > 0f) Lista.ScrollTo(tarjetas[indice]);
        }

        /// <summary>Alto (dp) de la barra de gestos bajo la hoja; lo pone PantallaMapa con Screen.safeArea.</summary>
        public void PonerMargenInferior(float dp) => HuecoInferior.style.height = Mathf.Max(0f, dp);

        /// <summary>Vuelve a dibujar los iconos (su color llega con el estilo del tema).</summary>
        public void Repintar()
        {
            BotonCerrar.MarkDirtyRepaint();
            Coche.IconoModo.MarkDirtyRepaint();
            APie.IconoModo.MarkDirtyRepaint();
        }
    }

    /// <summary>Tarjeta de una ruta de «Cómo llegar»: minutos, llegada y distancia, y su botón «Iniciar».</summary>
    public class TarjetaOpcion : VisualElement
    {
        public const string ClaseElegida = "como-llegar__ruta--elegida";

        readonly Func<string, string> textos;
        readonly Action alTocar, alIniciar;

        public Label Minutos { get; }
        public Label Detalle { get; }
        public VisualElement Iniciar { get; }
        public Label TextoIniciar { get; }

        /// <summary>Lo que lee TalkBack: "7 minutos, llegada 22:54, 2,8 km, más rápida".</summary>
        public string Anuncio { get; private set; } = "";
        /// <summary>Lo que lee TalkBack en su «Iniciar»: "Iniciar, 7 minutos, llegada 22:54, 2,8 km, más rápida".</summary>
        public string AnuncioIniciar => TextoIniciar.text + ", " + Anuncio;
        public bool Visible => style.display.value == DisplayStyle.Flex;

        public bool Elegida
        {
            get => ClassListContains(ClaseElegida);
            set => EnableInClassList(ClaseElegida, value);
        }

        public TarjetaOpcion(Func<string, string> textos, Action alTocar, Action alIniciar)
        {
            this.textos = textos;
            this.alTocar = alTocar;
            this.alIniciar = alIniciar;
            name = "como-llegar-ruta";
            AddToClassList("como-llegar__ruta");
            focusable = true;
            pickingMode = PickingMode.Position;
            this.AddManipulator(new Clickable(Tocar));
            RegisterCallback<NavigationSubmitEvent>(e => { if (e.target == this) Tocar(); });

            var izquierda = new VisualElement { name = "como-llegar-ruta-textos", pickingMode = PickingMode.Ignore };
            izquierda.AddToClassList("como-llegar__ruta-textos");
            Add(izquierda);
            Minutos = HojaComoLlegar.Texto("como-llegar-ruta-minutos", "como-llegar__ruta-minutos", "t-title");
            izquierda.Add(Minutos);
            Detalle = HojaComoLlegar.Texto("como-llegar-ruta-detalle", "como-llegar__ruta-detalle", "t-meta");
            izquierda.Add(Detalle);

            Iniciar = new VisualElement { name = "como-llegar-iniciar", focusable = true, pickingMode = PickingMode.Position };
            Iniciar.AddToClassList("como-llegar__iniciar");
            Iniciar.AddManipulator(new Clickable(PulsarIniciar));
            Iniciar.RegisterCallback<NavigationSubmitEvent>(_ => PulsarIniciar());
            TextoIniciar = HojaComoLlegar.Texto("como-llegar-iniciar-texto", "como-llegar__iniciar-texto", "t-heading");
            TextoIniciar.text = textos(ClavesTexto.ComoLlegarIniciar);
            Iniciar.Add(TextoIniciar);
            Add(Iniciar);
        }

        public void Tocar() => alTocar?.Invoke();
        public void PulsarIniciar() => alIniciar?.Invoke();

        internal void Ocultar()
        {
            style.display = DisplayStyle.None;
            Elegida = false;
        }

        internal void Mostrar(Ruta ruta, DateTime ahoraLocal, bool masRapida)
        {
            string hora = TarjetaRuta.HoraLlegada(ahoraLocal, ruta.Segundos);
            string distancia = FormatoDistancia.Texto(ruta.Metros);
            // TalkBack lee la distancia con un espacio normal (el de no separación es solo para la vista)
            string distanciaLeida = distancia.Replace(' ', ' ');
            int minutos = ruta.Minutos;
            Minutos.text = TextoMinutos(ruta, textos);
            if (ruta.Aproximada)
            {
                string Aprox(string v) => Textos.Formatear(textos(ClavesTexto.RutaValorAprox), v);
                Detalle.text = Textos.Formatear(textos(ClavesTexto.ComoLlegarDetalle), Aprox(hora), Aprox(distancia));
                Anuncio = minutos == 1
                    ? Textos.Formatear(textos(ClavesTexto.ComoLlegarAnuncioAproxMinuto), hora, distanciaLeida)
                    : Textos.Formatear(textos(ClavesTexto.ComoLlegarAnuncioAprox), minutos, hora, distanciaLeida);
            }
            else
            {
                Detalle.text = Textos.Formatear(textos(ClavesTexto.ComoLlegarDetalle), hora, distancia);
                Anuncio = minutos == 1
                    ? Textos.Formatear(textos(ClavesTexto.ComoLlegarAnuncioMinuto), hora, distanciaLeida)
                    : Textos.Formatear(textos(ClavesTexto.ComoLlegarAnuncio), minutos, hora, distanciaLeida);
            }
            if (masRapida)
            {
                Detalle.text = Textos.Formatear(textos(ClavesTexto.ComoLlegarMasRapida), Detalle.text);
                Anuncio = Textos.Formatear(textos(ClavesTexto.ComoLlegarAnuncioRapida), Anuncio);
            }
            style.display = DisplayStyle.Flex;
        }

        /// <summary>Los minutos de la ruta, como en la tarjeta y en su etiqueta del mapa: "7 min" o "≈ 7 min".</summary>
        public static string TextoMinutos(Ruta ruta, Func<string, string> textos) =>
            Textos.Formatear(textos(ruta.Aproximada ? ClavesTexto.FichaMinutosAprox : ClavesTexto.FichaMinutos), ruta.Minutos);
    }
}
