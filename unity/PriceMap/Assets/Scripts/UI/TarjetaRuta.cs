using System;
using System.Globalization;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Hoja de la ruta abajo (historia 2.6, parte 2a; hoja que solo informa del 22/11/2026, como Apple Maps). Plegada:
    /// el asa y la hora de llegada, los minutos y la distancia en tres columnas (valor grande y rótulo debajo: "13:34 /
    /// Llegada", "4 / min", "1,2 / km"); ningún toque en ella termina la ruta ni abre la ficha, solo la despliega.
    /// Desplegada: debajo, la fila del destino (icono del tipo en un círculo del color de su plantilla, nombre y
    /// "categoría · en coche"; solo informa) y el botón rojo ancho "Finalizar ruta" ("Quitar ruta" sin navegación). Se
    /// despliega o pliega tocando la parte de arriba o arrastrando más de 6 dp (hacia arriba despliega, hacia abajo
    /// pliega). Va anclada abajo, de borde a borde, con las esquinas de arriba de hoja; su relleno de abajo deja sitio a
    /// la barra de gestos (PonerMargenInferior). Mientras llega la ruta, "…" en las tres; si falla, "—".
    /// </summary>
    public class TarjetaRuta : VisualElement
    {
        readonly Func<string, string> textos;

        /// <summary>Parte de arriba (asa y columnas): tocarla despliega o pliega.</summary>
        public VisualElement Cabecera { get; }
        /// <summary>Las tres columnas: el nodo de TalkBack con Anuncio y la acción "Desplegar"/"Plegar".</summary>
        public VisualElement Cuerpo { get; }
        public Label Llegada { get; }
        public Label RotuloLlegada { get; }
        public Label Minutos { get; }
        public Label RotuloMinutos { get; }
        public Label Distancia { get; }
        public Label RotuloDistancia { get; }

        /// <summary>Lo que sale al desplegar: la fila del destino y el botón "Finalizar ruta".</summary>
        public VisualElement Desplegable { get; }
        /// <summary>Fila del destino (solo informa: tocarla no abre la ficha).</summary>
        public VisualElement Destino { get; }
        public VisualElement DestinoIcono { get; }
        public VisualElement DestinoGlifo { get; }
        public Label DestinoNombre { get; }
        public Label DestinoDetalle { get; }
        /// <summary>Botón rojo ancho "Finalizar ruta" (o "Quitar ruta" sin navegación).</summary>
        public VisualElement Finalizar { get; }
        public Label TextoFinalizar { get; }

        /// <summary>Lo que lee TalkBack en el cuerpo: "Llegada 13:34, 4 minutos, 1,2 km".</summary>
        public string Anuncio { get; private set; } = "";
        /// <summary>Lo que lee TalkBack en la fila del destino: "Clínica Areas, Clínica privada, en coche".</summary>
        public string AnuncioDestino { get; private set; } = "";

        /// <summary>La hoja está desplegada (se ven el destino y "Finalizar ruta").</summary>
        public bool Desplegada { get; private set; }

        /// <summary>
        /// La hoja se ha desplegado (true) o plegado (false); el segundo valor dice si lo ha pedido el usuario (toque,
        /// arrastre, atrás, TalkBack) o la app (ruta nueva, recálculo, fin de la ruta).
        /// </summary>
        public event Action<bool, bool> DesplegadaCambiada;
        /// <summary>"Finalizar ruta" se ha pulsado.</summary>
        public event Action FinalizarPulsado;

        /// <summary>Clase de la parte de arriba mientras el dedo la pulsa (sin Clickable no hay :active).</summary>
        public const string ClaseCabeceraPulsada = "tarjeta-ruta__cabecera--pulsada";
        /// <summary>Clase de "Finalizar ruta" mientras el dedo lo pulsa.</summary>
        public const string ClaseFinalizarPulsado = "tarjeta-ruta__finalizar--pulsado";
        /// <summary>Clase de la hoja desplegada.</summary>
        public const string ClaseDesplegada = "tarjeta-ruta--desplegada";
        /// <summary>Clase del círculo del destino con un punto libre (la chincheta en negativo).</summary>
        public const string ClaseIconoPunto = "tarjeta-ruta__destino-icono--punto";

        string glifo = IconosLugar.Generico;
        string claseIcono;

        public TarjetaRuta(Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "tarjeta-ruta";
            AddToClassList("tarjeta-ruta");
            pickingMode = PickingMode.Position; // arrastrar sobre la tarjeta no mueve el mapa

            Cabecera = Nuevo("tarjeta-ruta-cabecera", "tarjeta-ruta__cabecera");
            Add(Cabecera);
            Cabecera.Add(Nuevo("tarjeta-ruta-asa", "tarjeta-ruta__asa"));

            var fila = Nuevo("tarjeta-ruta-fila", "tarjeta-ruta__fila");
            Cabecera.Add(fila);

            Cuerpo = new VisualElement { name = "tarjeta-ruta-cuerpo", focusable = true, pickingMode = PickingMode.Ignore };
            Cuerpo.AddToClassList("tarjeta-ruta__cuerpo");
            Cuerpo.RegisterCallback<NavigationSubmitEvent>(_ => Tocar());
            fila.Add(Cuerpo);
            (Llegada, RotuloLlegada) = Columna("llegada", this.textos(ClavesTexto.RutaLlegada));
            (Minutos, RotuloMinutos) = Columna("minutos", this.textos(ClavesTexto.RutaMin));
            (Distancia, RotuloDistancia) = Columna("distancia", "km");

            Desplegable = Nuevo("tarjeta-ruta-desplegable", "tarjeta-ruta__desplegable");
            Desplegable.pickingMode = PickingMode.Position; // un toque dentro de lo desplegado se reconoce por su objetivo
            Desplegable.style.display = DisplayStyle.None;
            Add(Desplegable);

            Destino = Nuevo("tarjeta-ruta-destino", "tarjeta-ruta__destino");
            Desplegable.Add(Destino);
            DestinoIcono = Nuevo("tarjeta-ruta-destino-icono", "tarjeta-ruta__destino-icono");
            Destino.Add(DestinoIcono);
            DestinoGlifo = Nuevo("tarjeta-ruta-destino-glifo", "tarjeta-ruta__destino-glifo");
            DestinoGlifo.generateVisualContent += DibujarGlifo;
            DestinoGlifo.RegisterCallback<GeometryChangedEvent>(_ => DestinoGlifo.MarkDirtyRepaint());
            DestinoIcono.Add(DestinoGlifo);
            var textosDestino = Nuevo("tarjeta-ruta-destino-textos", "tarjeta-ruta__destino-textos");
            Destino.Add(textosDestino);
            DestinoNombre = new Label { name = "tarjeta-ruta-destino-nombre", pickingMode = PickingMode.Ignore };
            DestinoNombre.AddToClassList("tarjeta-ruta__destino-nombre");
            DestinoNombre.AddToClassList("t-label");
            textosDestino.Add(DestinoNombre);
            DestinoDetalle = new Label { name = "tarjeta-ruta-destino-detalle", pickingMode = PickingMode.Ignore };
            DestinoDetalle.AddToClassList("tarjeta-ruta__destino-detalle");
            DestinoDetalle.AddToClassList("t-caption");
            textosDestino.Add(DestinoDetalle);

            Finalizar = new VisualElement { name = "tarjeta-ruta-finalizar", focusable = true, pickingMode = PickingMode.Position };
            Finalizar.AddToClassList("tarjeta-ruta__finalizar");
            // Lo pulsa el gesto de la hoja (AlSoltar): con un Clickable, el botón se quedaría el puntero y la hoja no
            // sabría si el dedo ha arrastrado
            Finalizar.RegisterCallback<NavigationSubmitEvent>(_ => PulsarFinalizar());
            Desplegable.Add(Finalizar);
            TextoFinalizar = new Label(this.textos(ClavesTexto.RutaQuitar)) { name = "tarjeta-ruta-finalizar-texto", pickingMode = PickingMode.Ignore };
            TextoFinalizar.AddToClassList("tarjeta-ruta__finalizar-texto");
            TextoFinalizar.AddToClassList("t-heading");
            Finalizar.Add(TextoFinalizar);

            RegisterCallback<PointerDownEvent>(AlBajar, TrickleDown.TrickleDown);

            MostrarCalculando();
        }

        static VisualElement Nuevo(string nombre, string clase)
        {
            var e = new VisualElement { name = nombre, pickingMode = PickingMode.Ignore };
            e.AddToClassList(clase);
            return e;
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

        // ───────────────────────── Plegar y desplegar ─────────────────────────

        /// <summary>
        /// Un toque en la hoja (fuera del destino y del botón), Intro o el doble toque de TalkBack: plegada, se
        /// despliega; desplegada, se pliega.
        /// </summary>
        public void Tocar() => PonerDesplegada(!Desplegada, true);

        /// <summary>Despliega la hoja; porUsuario false si lo hace la app (no mueve el foco de TalkBack).</summary>
        public void Desplegar(bool porUsuario = true) => PonerDesplegada(true, porUsuario);
        /// <summary>Pliega la hoja; porUsuario false si lo hace la app (ruta nueva, recálculo, fin de la ruta).</summary>
        public void Plegar(bool porUsuario = true) => PonerDesplegada(false, porUsuario);

        /// <summary>
        /// Fin de un gesto sobre la hoja con ese desplazamiento vertical (dp, positivo hacia abajo): más de 6 dp hacia
        /// arriba despliega, más de 6 dp hacia abajo pliega; menos es un toque (Tocar).
        /// </summary>
        public void Soltar(float desplazamientoDp)
        {
            if (desplazamientoDp <= -FisicaHoja.UmbralToque) Desplegar();
            else if (desplazamientoDp >= FisicaHoja.UmbralToque) Plegar();
            else Tocar();
        }

        void PonerDesplegada(bool desplegada, bool porUsuario)
        {
            if (Desplegada == desplegada) return;
            Desplegada = desplegada;
            EnableInClassList(ClaseDesplegada, desplegada);
            Desplegable.style.display = desplegada ? DisplayStyle.Flex : DisplayStyle.None;
            DesplegadaCambiada?.Invoke(desplegada, porUsuario);
        }

        public void PulsarFinalizar() => FinalizarPulsado?.Invoke();

        /// <summary>El texto del botón: "Finalizar ruta" navegando, "Quitar ruta" si no.</summary>
        public void PonerTextoFinalizar(string texto) => TextoFinalizar.text = texto ?? "";

        // ───────────────────────── Gesto ─────────────────────────

        int puntero = -1;
        Vector2 inicio;
        bool arrastrando;
        VisualElement raizGesto; // raíz del panel que escucha el gesto en curso
        VisualElement objetivoInicial;
        bool empezoEnDesplegable; // la pulsación empezó en lo desplegado: soltarla sin arrastrar nunca pliega ni despliega

        void AlBajar(PointerDownEvent e)
        {
            if (e.pointerType == UnityEngine.UIElements.PointerType.mouse && e.button != 0) return;
            if (puntero >= 0) SoltarPuntero(); // un gesto anterior que no llegó a soltarse no bloquea la hoja
            puntero = e.pointerId;
            inicio = e.position;
            arrastrando = false;
            objetivoInicial = e.target as VisualElement;
            empezoEnDesplegable = Desplegada && (Dentro(objetivoInicial, Desplegable) || !EncimaDelDesplegable(inicio));
            raizGesto = panel?.visualTree;
            if (raizGesto == null) { puntero = -1; return; }
            Cabecera.EnableInClassList(ClaseCabeceraPulsada, !empezoEnDesplegable);
            Finalizar.EnableInClassList(ClaseFinalizarPulsado, Desplegada && Dentro(objetivoInicial, Finalizar));
            raizGesto.RegisterCallback<PointerMoveEvent>(AlMover, TrickleDown.TrickleDown);
            raizGesto.RegisterCallback<PointerUpEvent>(AlSoltar, TrickleDown.TrickleDown);
            raizGesto.RegisterCallback<PointerCancelEvent>(AlCancelar, TrickleDown.TrickleDown);
        }

        void AlMover(PointerMoveEvent e)
        {
            if (e.pointerId != puntero) return;
            if (!arrastrando && ((Vector2)e.position - inicio).magnitude < FisicaHoja.UmbralToque) return;
            if (!arrastrando)
            {
                arrastrando = true;
                Cabecera.RemoveFromClassList(ClaseCabeceraPulsada);
                Finalizar.RemoveFromClassList(ClaseFinalizarPulsado);
                // Desde aquí el gesto es de la hoja: el botón pierde el puntero y no se pulsa al soltar
                this.CapturePointer(e.pointerId);
            }
            e.StopPropagation();
        }

        void AlSoltar(PointerUpEvent e)
        {
            if (e.pointerId != puntero) return;
            bool arrastre = arrastrando;
            float dy = e.position.y - inicio.y;
            bool enDesplegable = empezoEnDesplegable;
            bool enBoton = Dentro(objetivoInicial, Finalizar);
            SoltarPuntero();
            if (arrastre)
            {
                e.StopPropagation();
                if (Mathf.Abs(dy) >= FisicaHoja.UmbralToque) Soltar(dy); // de lado no cambia nada
                return;
            }
            // Solo la parte de arriba despliega o pliega: dentro de lo desplegado, el botón se pulsa y la fila del destino solo
            // informa, aunque la app haya plegado la hoja con el dedo encima (recálculo)
            if (enDesplegable)
            {
                // Un toque en el botón lo pulsa si sigue a la vista (la app no ha plegado la hoja) y se suelta encima
                if (enBoton && Desplegada && SobreElBoton(e.position)) PulsarFinalizar();
                return;
            }
            Tocar();
        }

        void AlCancelar(PointerCancelEvent e)
        {
            if (e.pointerId == puntero) SoltarPuntero();
        }

        void SoltarPuntero()
        {
            if (puntero >= 0 && this.HasPointerCapture(puntero)) this.ReleasePointer(puntero);
            puntero = -1;
            objetivoInicial = null;
            empezoEnDesplegable = false;
            Cabecera.RemoveFromClassList(ClaseCabeceraPulsada);
            Finalizar.RemoveFromClassList(ClaseFinalizarPulsado);
            if (raizGesto == null) return;
            raizGesto.UnregisterCallback<PointerMoveEvent>(AlMover, TrickleDown.TrickleDown);
            raizGesto.UnregisterCallback<PointerUpEvent>(AlSoltar, TrickleDown.TrickleDown);
            raizGesto.UnregisterCallback<PointerCancelEvent>(AlCancelar, TrickleDown.TrickleDown);
            raizGesto = null;
        }

        // El punto (panel) cae en el botón; sin medida, se da por bueno
        bool SobreElBoton(Vector2 punto)
        {
            var r = Finalizar.worldBound;
            return float.IsNaN(r.width) || r.width <= 0f || r.Contains(punto);
        }

        // El punto (panel) queda por encima de lo desplegado: la parte de arriba de la hoja
        bool EncimaDelDesplegable(Vector2 punto)
        {
            var r = Desplegable.worldBound;
            return float.IsNaN(r.yMin) || punto.y < r.yMin;
        }

        static bool Dentro(VisualElement e, VisualElement contenedor)
        {
            for (; e != null; e = e.parent) if (e == contenedor) return true;
            return false;
        }

        // ───────────────────────── Destino ─────────────────────────

        /// <summary>
        /// La fila del destino: el nombre, el detalle ("Clínica privada · en coche"; punto libre: su dirección), el
        /// glifo de IconosLugar (o FilaResultado.GlifoChincheta en un punto libre) y la clase del color del círculo.
        /// </summary>
        public void PonerDestino(string nombre, string detalle, string glifo, string claseIcono, string anuncio)
        {
            DestinoNombre.text = nombre ?? "";
            DestinoDetalle.text = detalle ?? "";
            DestinoDetalle.style.display = string.IsNullOrEmpty(detalle) ? DisplayStyle.None : DisplayStyle.Flex;
            AnuncioDestino = anuncio ?? DestinoNombre.text;
            this.glifo = string.IsNullOrEmpty(glifo) ? IconosLugar.Generico : glifo;
            if (this.claseIcono != null) DestinoIcono.RemoveFromClassList(this.claseIcono);
            this.claseIcono = claseIcono;
            if (claseIcono != null) DestinoIcono.AddToClassList(claseIcono);
            DestinoGlifo.EnableInClassList("tarjeta-ruta__destino-glifo--chincheta", this.glifo == FilaResultado.GlifoChincheta);
            DestinoGlifo.MarkDirtyRepaint();
        }

        /// <summary>Glifo del destino que se dibuja (para las pruebas).</summary>
        public string GlifoDestino => glifo;

        void DibujarGlifo(MeshGenerationContext mgc)
        {
            var r = DestinoGlifo.contentRect;
            if (float.IsNaN(r.width) || r.width <= 0f || r.height <= 0f) return;
            if (glifo == FilaResultado.GlifoChincheta)
                ChinchetaPunto.Dibujar(mgc, r, DestinoGlifo.resolvedStyle.color, DestinoIcono.resolvedStyle.backgroundColor, 0f);
            else
                IconosLugar.Dibujar(mgc.painter2D, glifo, r, DestinoGlifo.resolvedStyle.color);
        }

        /// <summary>Vuelve a dibujar el glifo del destino (el color cambia con el tema).</summary>
        public void Repintar() => DestinoGlifo.MarkDirtyRepaint();

        // ───────────────────────── Valores ─────────────────────────

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

        /// <summary>
        /// La ruta llegada, con la hora local de ahora para la llegada. La aproximada (parte 2b) lleva "≈" en los tres
        /// valores y TalkBack dice "Ruta aproximada. Llegada hacia las 13:34, unos 4 minutos, unos 1,2 km".
        /// </summary>
        public void MostrarRuta(Ruta ruta, DateTime ahoraLocal) => MostrarRestante(ruta.Segundos, ruta.Metros, ruta.Aproximada, ahoraLocal);

        /// <summary>
        /// Navegando (historia 2.7, parte 1): lo que queda, con la llegada a ahora + los segundos que quedan, y "≈" si la
        /// ruta es aproximada. Igual que MostrarRuta con la ruta entera.
        /// </summary>
        public void MostrarRestante(double segundos, double metros, bool aproximada, DateTime ahoraLocal)
        {
            string hora = HoraLlegada(ahoraLocal, segundos);
            var (valor, unidad) = PartirDistancia(metros);
            int minutos = Ruta.MinutosDe(segundos); // como la ficha y EstadoNavegacion.MinutosRestantes
            if (aproximada)
            {
                string Aprox(string v) => Textos.Formatear(textos(ClavesTexto.RutaValorAprox), v);
                Poner(Aprox(hora), Aprox(minutos.ToString(CultureInfo.InvariantCulture)), Aprox(valor), unidad);
                string aprox = FormatoDistancia.Texto(metros).Replace(' ', ' ');
                Anuncio = minutos == 1
                    ? Textos.Formatear(textos(ClavesTexto.RutaAnuncioAproxMinuto), hora, aprox)
                    : Textos.Formatear(textos(ClavesTexto.RutaAnuncioAprox), hora, minutos, aprox);
                return;
            }
            Poner(hora, minutos.ToString(CultureInfo.InvariantCulture), valor, unidad);
            string distancia = FormatoDistancia.Texto(metros).Replace(' ', ' ');
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
            // Al segundo: lo que queda al navegar (179,99 s) no adelanta la llegada un minuto
            return ahoraLocal.AddSeconds(Math.Round(segundos)).ToString("HH:mm", CultureInfo.InvariantCulture);
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
