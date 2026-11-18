using System;
using System.Collections.Generic;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Ficha del lugar (historia 1.8, parte 1; DESIGN components.sheet): hoja opaca que sube desde abajo, con
    /// esquinas superiores de 24 dp, como mucho el 72 % de la pantalla y desplazamiento interno. Cabecera (nombre,
    /// tipo con la muestra del color de su plantilla y dirección), botón de cerrar de 48 dp, la fila de acciones
    /// reservada (vacía y sin alto hasta que otras épicas la rellenen, FR-10) y los conceptos agrupados por área
    /// en el orden de los datos, con su precio, origen y frescura (FilaConcepto). Sin precios: "Aún no tiene precio".
    ///
    /// Detrás de la hoja hay un velo transparente a pantalla completa: tocar fuera de la hoja la cierra y ese toque
    /// no llega al mapa. La abre y cierra PantallaMapa; nada se guarda (AD-3).
    /// </summary>
    public class FichaLugar : VisualElement
    {
        readonly Func<string, string> textos;
        readonly Action alCerrar;
        readonly List<Label> areas = new List<Label>();
        readonly List<FilaConcepto> filas = new List<FilaConcepto>();

        public VisualElement Velo { get; }
        public VisualElement Hoja { get; }
        public Label Titulo { get; }
        /// <summary>Muestra del color de la plantilla (--color-pin-*), junto al tipo.</summary>
        public VisualElement Muestra { get; }
        /// <summary>"Clínica privada · Servicio local".</summary>
        public Label Tipo { get; }
        /// <summary>Dirección; oculta si el lugar no la tiene.</summary>
        public Label Direccion { get; }
        public BotonRedondo BotonCerrar { get; }
        /// <summary>Fila de acciones bajo la cabecera (FR-10): vacía en la épica 1, su sitio en el árbol es fijo.</summary>
        public VisualElement FilaAcciones { get; }
        public ScrollView Contenido { get; }
        /// <summary>Hueco bajo el contenido con el alto de la barra de gestos (zona segura inferior).</summary>
        public VisualElement HuecoInferior { get; }

        /// <summary>Encabezados de área en el orden en que se ven.</summary>
        public IReadOnlyList<Label> Areas => areas;
        public IReadOnlyList<FilaConcepto> Filas => filas;
        /// <summary>"Aún no tiene precio"; null si el lugar tiene algún precio o "Consultar".</summary>
        public Label SinPrecio { get; private set; }

        public Lugar Lugar { get; private set; }
        public bool Abierta { get; private set; }

        /// <param name="alCerrar">Lo que hacen el botón de cerrar y el toque fuera de la hoja.</param>
        /// <param name="textos">Textos de la UI (por defecto Localization); las pruebas pasan los de es-ES.</param>
        public FichaLugar(Action alCerrar, Func<string, string> textos = null)
        {
            this.alCerrar = alCerrar;
            this.textos = textos ?? (c => Textos.T(c));
            name = "ficha";
            AddToClassList("ficha");
            pickingMode = PickingMode.Ignore;

            // Velo debajo de la hoja: un toque fuera (al soltar) solo cierra la ficha
            Velo = new VisualElement { name = "ficha-velo", pickingMode = PickingMode.Position };
            Velo.AddToClassList("ficha__velo");
            Velo.AddManipulator(new Clickable(TocarFuera));
            Add(Velo);

            Hoja = new VisualElement { name = "ficha-hoja", pickingMode = PickingMode.Position };
            Hoja.AddToClassList("ficha__hoja");
            Add(Hoja);

            var cabecera = Nuevo("ficha-cabecera", "ficha__cabecera");
            Hoja.Add(cabecera);
            var textosCabecera = Nuevo("ficha-cabecera-textos", "ficha__cabecera-textos");
            cabecera.Add(textosCabecera);
            Titulo = Texto("ficha-titulo", "ficha__titulo", "t-title");
            textosCabecera.Add(Titulo);
            var lineaTipo = Nuevo("ficha-linea-tipo", "ficha__linea-tipo");
            textosCabecera.Add(lineaTipo);
            Muestra = Nuevo("ficha-muestra", "ficha__muestra");
            lineaTipo.Add(Muestra);
            Tipo = Texto("ficha-tipo", "ficha__tipo", "t-meta");
            lineaTipo.Add(Tipo);
            Direccion = Texto("ficha-direccion", "ficha__direccion", "t-meta");
            textosCabecera.Add(Direccion);
            BotonCerrar = new BotonRedondo(Icono.Cerrar, Cerrar) { name = "ficha-cerrar" };
            BotonCerrar.AddToClassList("ficha__cerrar");
            cabecera.Add(BotonCerrar);

            FilaAcciones = Nuevo("fila-acciones", "ficha__acciones");
            Hoja.Add(FilaAcciones);

            // Sin barras de desplazamiento (no hay tema por defecto de Unity que las dibuje): se arrastra con el dedo
            Contenido = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "ficha-contenido",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            Contenido.AddToClassList("ficha__contenido");
            Hoja.Add(Contenido);

            HuecoInferior = Nuevo("ficha-hueco-inferior", "ficha__hueco-inferior");
            Hoja.Add(HuecoInferior);

            Mostrar(false);
        }

        /// <summary>
        /// Rellena la ficha con el lugar, calculando cada precio con "ahora" (AD-12, en UTC), y la muestra. La
        /// variante vigente de cada concepto se elige con "ahora" pasado a la hora local del móvil.
        /// </summary>
        public void Abrir(Lugar lugar, DateTime ahora)
        {
            Lugar = lugar ?? throw new ArgumentNullException(nameof(lugar));
            Titulo.text = lugar.Nombre;
            foreach (var p in new[] { Plantilla.ServicioLocal, Plantilla.Hosteleria, Plantilla.Ocio })
                Muestra.EnableInClassList(ClaseMuestra(p), p == lugar.Plantilla);
            Tipo.text = Textos.Formatear(textos(ClavesTexto.FichaTipo), lugar.Tipo, textos(ClavePlantilla(lugar.Plantilla)));
            Direccion.text = lugar.Direccion;
            Direccion.style.display = string.IsNullOrWhiteSpace(lugar.Direccion) ? DisplayStyle.None : DisplayStyle.Flex;

            // Los textos de la ficha anterior desaparecen: su tamaño base ya no hace falta (EscalaFuente)
            EscalaFuente.Olvidar(Contenido);
            Contenido.Clear();
            areas.Clear();
            filas.Clear();
            SinPrecio = null;

            // La vigente depende de la hora del móvil: "ahora" llega en UTC desde el Arranque (AD-12)
            var horaLocal = ahora.ToLocalTime();

            // Solo los conceptos con importe o "Consultar"; sin nada que mostrar, no se inventa nada (UX-DR16)
            var porArea = new List<(string area, List<FilaConcepto> filas)>();
            foreach (var c in lugar.Conceptos)
            {
                if (c == null) continue;
                var precio = CalculadoraPrecio.Calcular(c.Fuentes, ahora);
                if (!precio.TienePrecio && !precio.EsConsultar) continue;
                var grupo = porArea.Find(g => g.area == c.Area);
                if (grupo.filas == null) { grupo = (c.Area, new List<FilaConcepto>()); porArea.Add(grupo); }
                grupo.filas.Add(new FilaConcepto(c, precio, textos, ahora, horaLocal));
            }

            // Agrupados por área, en el orden en que aparece cada área en los datos
            foreach (var (area, filasArea) in porArea)
            {
                if (!string.IsNullOrWhiteSpace(area))
                {
                    var encabezado = new Label(area) { name = "ficha-area", pickingMode = PickingMode.Ignore };
                    encabezado.AddToClassList("ficha__area");
                    encabezado.AddToClassList("t-label");
                    Contenido.Add(encabezado);
                    areas.Add(encabezado);
                }
                foreach (var f in filasArea)
                {
                    Contenido.Add(f);
                    filas.Add(f);
                }
            }

            if (filas.Count == 0)
            {
                SinPrecio = new Label(textos(ClavesTexto.FichaSinPrecio)) { name = "ficha-sin-precio", pickingMode = PickingMode.Ignore };
                SinPrecio.AddToClassList("ficha__sin-precio");
                SinPrecio.AddToClassList("t-body");
                Contenido.Add(SinPrecio);
            }

            Contenido.scrollOffset = Vector2.zero;
            Mostrar(true);
        }

        /// <summary>
        /// Textos del recorrido de TalkBack en orden (historia 1.8, parte 3), sin el botón de cerrar: título, tipo,
        /// dirección si la hay y, en el orden en que se ven, encabezados de área, la frase de cada concepto seguida
        /// de sus variantes y "Aún no tiene precio". Cambian con cada apertura.
        /// </summary>
        public List<(VisualElement elemento, string texto)> TextosTalkBack()
        {
            var r = new List<(VisualElement, string)> { (Titulo, Titulo.text), (Tipo, Tipo.text) };
            if (!string.IsNullOrWhiteSpace(Direccion.text)) // sin dirección, la línea está oculta (Abrir)
                r.Add((Direccion, Direccion.text));
            foreach (var hijo in Contenido.Children())
            {
                if (hijo is FilaConcepto fila)
                {
                    r.Add((fila, fila.Anuncio));
                    foreach (var v in fila.Variantes) r.Add((v, v.Anuncio));
                }
                else if (hijo is Label texto) r.Add((texto, texto.text));
            }
            return r;
        }

        /// <summary>
        /// Desplaza el contenido hasta el elemento si está dentro de él (TalkBack ha puesto el foco en su nodo y
        /// puede quedar fuera de la parte visible). Los de la cabecera no se desplazan.
        /// </summary>
        public void DesplazarHasta(VisualElement elemento)
        {
            if (elemento == null || !Abierta || !Contenido.contentContainer.Contains(elemento)) return;
            Desplazamientos++;
            UltimoDesplazado = elemento;
            if (Contenido.panel != null) Contenido.ScrollTo(elemento); // sin panel (pruebas), ScrollTo falla
        }

        /// <summary>Veces que DesplazarHasta ha desplazado el contenido y último elemento pedido (para las pruebas).</summary>
        internal int Desplazamientos { get; private set; }
        internal VisualElement UltimoDesplazado { get; private set; }

        /// <summary>Fila del concepto con ese id; null si no se muestra.</summary>
        public FilaConcepto Fila(string idConcepto) => filas.Find(f => f.Concepto.Id == idConcepto);

        /// <summary>Lo que hace un toque en el velo (fuera de la hoja): cerrar.</summary>
        public void TocarFuera() => Cerrar();

        void Cerrar() => alCerrar?.Invoke();

        public void Mostrar(bool abierta)
        {
            Abierta = abierta;
            style.display = abierta ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Alto (dp) de la barra de gestos bajo la hoja; lo pone PantallaMapa con Screen.safeArea.</summary>
        public void PonerMargenInferior(float dp) => HuecoInferior.style.height = Mathf.Max(0f, dp);

        public static string ClavePlantilla(Plantilla p) => p switch
        {
            Plantilla.Hosteleria => ClavesTexto.PlantillaHosteleria,
            Plantilla.Ocio => ClavesTexto.PlantillaOcio,
            _ => ClavesTexto.PlantillaServicio,
        };

        public static string ClaseMuestra(Plantilla p) => p switch
        {
            Plantilla.Hosteleria => "ficha__muestra--hosteleria",
            Plantilla.Ocio => "ficha__muestra--ocio",
            _ => "ficha__muestra--servicio",
        };

        static VisualElement Nuevo(string nombre, string clase)
        {
            var e = new VisualElement { name = nombre, pickingMode = PickingMode.Ignore };
            e.AddToClassList(clase);
            return e;
        }

        static Label Texto(string nombre, string clase, string tipografia)
        {
            var l = new Label { name = nombre, pickingMode = PickingMode.Ignore };
            l.AddToClassList(clase);
            l.AddToClassList(tipografia);
            return l;
        }
    }
}
