using System;
using System.Globalization;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Tarjeta de acciones de la ruta (historia 2.6, parte 2a; maqueta v2 del 20/11/2026): arriba, la primera maniobra
    /// en grande con su flecha y, debajo, la siguiente, con los textos de Valhalla en castellano. Fondo text al 96 % y
    /// texto blanco en claro; clara (#E6ECFA) con texto bg en oscuro (tokens --color-tarjeta-maniobra*). Mientras llega
    /// la ruta, "Calculando la ruta…"; si falla, "No se ha podido calcular la ruta"; con una ruta sin textos (OSRM, hasta
    /// la 2.7), "Ruta a Clínica Areas". TalkBack la lee como texto (Anuncio).
    /// </summary>
    public class TarjetaManiobra : VisualElement
    {
        readonly Func<string, string> textos;

        public IconoManiobra IconoPrincipal { get; }
        public Label Principal { get; }
        public VisualElement FilaSiguiente { get; }
        public IconoManiobra IconoSiguiente { get; }
        public Label Siguiente { get; }

        /// <summary>Lo que lee TalkBack: "Sal hacia el norte por Calle Ancha. Después, gira a la derecha en Calle Santo Domingo".</summary>
        public string Anuncio { get; private set; } = "";

        public TarjetaManiobra(Func<string, string> textos = null)
        {
            this.textos = textos ?? (c => Textos.T(c));
            name = "tarjeta-maniobra";
            AddToClassList("tarjeta-maniobra");
            pickingMode = PickingMode.Position; // arrastrar sobre la tarjeta no mueve el mapa

            var fila = new VisualElement { name = "tarjeta-maniobra-fila", pickingMode = PickingMode.Ignore };
            fila.AddToClassList("tarjeta-maniobra__fila");
            Add(fila);
            IconoPrincipal = new IconoManiobra { name = "tarjeta-maniobra-icono" };
            IconoPrincipal.AddToClassList("tarjeta-maniobra__icono");
            fila.Add(IconoPrincipal);
            Principal = new Label { name = "tarjeta-maniobra-principal", pickingMode = PickingMode.Ignore };
            Principal.AddToClassList("tarjeta-maniobra__principal");
            Principal.AddToClassList("t-maniobra");
            fila.Add(Principal);

            FilaSiguiente = new VisualElement { name = "tarjeta-maniobra-siguiente-fila", pickingMode = PickingMode.Ignore };
            FilaSiguiente.AddToClassList("tarjeta-maniobra__fila");
            FilaSiguiente.AddToClassList("tarjeta-maniobra__fila--siguiente");
            Add(FilaSiguiente);
            IconoSiguiente = new IconoManiobra { name = "tarjeta-maniobra-icono-siguiente" };
            IconoSiguiente.AddToClassList("tarjeta-maniobra__icono-siguiente");
            FilaSiguiente.Add(IconoSiguiente);
            Siguiente = new Label { name = "tarjeta-maniobra-siguiente", pickingMode = PickingMode.Ignore };
            Siguiente.AddToClassList("tarjeta-maniobra__siguiente");
            Siguiente.AddToClassList("t-label");
            FilaSiguiente.Add(Siguiente);

            MostrarCalculando();
        }

        /// <summary>Recién pulsado Ir: "Calculando la ruta…", sin siguiente.</summary>
        public void MostrarCalculando() => Poner(TipoManiobra.Recto, true, textos(ClavesTexto.RutaCalculando), null, TipoManiobra.Otra);

        /// <summary>Sin ruta: "No se ha podido calcular la ruta", sin flecha ni siguiente.</summary>
        public void MostrarFallo() => Poner(TipoManiobra.Otra, false, textos(ClavesTexto.RutaFallo), null, TipoManiobra.Otra);

        /// <summary>
        /// La ruta llegada: la primera maniobra con texto y la siguiente con texto (sin el punto final de Valhalla); sin
        /// ninguna (OSRM), "Ruta a {destino}".
        /// </summary>
        public void MostrarRuta(Ruta ruta, string destino)
        {
            int i = SiguienteConTexto(ruta, 0);
            if (i < 0)
            {
                Poner(TipoManiobra.Recto, true, Textos.Formatear(textos(ClavesTexto.RutaA), destino ?? ""), null, TipoManiobra.Otra);
                return;
            }
            int j = SiguienteConTexto(ruta, i + 1);
            var primera = ruta.Maniobras[i];
            var segunda = j >= 0 ? ruta.Maniobras[j] : null;
            Poner(primera.Tipo, true, SinPunto(primera.Texto), segunda != null ? SinPunto(segunda.Texto) : null,
                segunda?.Tipo ?? TipoManiobra.Otra);
        }

        static int SiguienteConTexto(Ruta ruta, int desde)
        {
            if (ruta == null) return -1;
            for (int i = desde; i < ruta.Maniobras.Count; i++)
                if (!string.IsNullOrWhiteSpace(ruta.Maniobras[i]?.Texto)) return i;
            return -1;
        }

        void Poner(TipoManiobra tipo, bool conIcono, string principal, string siguiente, TipoManiobra tipoSiguiente)
        {
            IconoPrincipal.Tipo = tipo;
            IconoPrincipal.style.display = conIcono ? DisplayStyle.Flex : DisplayStyle.None;
            Principal.text = principal ?? "";
            bool conSiguiente = !string.IsNullOrEmpty(siguiente);
            FilaSiguiente.style.display = conSiguiente ? DisplayStyle.Flex : DisplayStyle.None;
            Siguiente.text = siguiente ?? "";
            IconoSiguiente.Tipo = tipoSiguiente;
            Anuncio = conSiguiente
                ? Textos.Formatear(textos(ClavesTexto.RutaDespues), Principal.text, Minuscula(siguiente))
                : Principal.text;
        }

        /// <summary>Quita el punto final de un texto de Valhalla ("Gira a la derecha en Calle Ancha." → sin punto).</summary>
        public static string SinPunto(string texto)
        {
            var t = (texto ?? "").Trim();
            return t.EndsWith(".") ? t.Substring(0, t.Length - 1).TrimEnd() : t;
        }

        /// <summary>Primera letra en minúscula, para seguir la frase ("Después, gira a la derecha…").</summary>
        public static string Minuscula(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return char.ToLower(texto[0], new CultureInfo("es-ES")) + texto.Substring(1);
        }

        /// <summary>Vuelve a dibujar las flechas (el color cambia con el tema).</summary>
        public void Repintar()
        {
            IconoPrincipal.MarkDirtyRepaint();
            IconoSiguiente.MarkDirtyRepaint();
        }
    }

    /// <summary>
    /// Flecha de una maniobra dibujada con vectores (nunca emojis, UX-DR23) en el color (USS color) del elemento: recto,
    /// derecha, izquierda, ligero a un lado, cambio de sentido, rotonda y llegada. Viewbox de 24 con trazo 2,2.
    /// </summary>
    public class IconoManiobra : VisualElement
    {
        TipoManiobra tipo = TipoManiobra.Recto;

        public TipoManiobra Tipo
        {
            get => tipo;
            set { tipo = value; MarkDirtyRepaint(); }
        }

        public IconoManiobra()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Dibujar;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
            RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
        }

        void Dibujar(MeshGenerationContext mgc)
        {
            var r = contentRect;
            if (float.IsNaN(r.width) || r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            float lado = Mathf.Min(r.width, r.height);
            float k = lado / 24f;
            var origen = r.center - new Vector2(12f, 12f) * k;
            var color = resolvedStyle.color;
            p.strokeColor = color;
            p.fillColor = color;
            p.lineWidth = 2.2f * k;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            bool espejo = tipo == TipoManiobra.Izquierda || tipo == TipoManiobra.CerradoIzquierda || tipo == TipoManiobra.LigeroIzquierda;
            Vector2 V(float x, float y) => origen + new Vector2(espejo ? 24f - x : x, y) * k;

            void Trazo(params float[] xy)
            {
                p.BeginPath();
                p.MoveTo(V(xy[0], xy[1]));
                for (int i = 2; i + 1 < xy.Length; i += 2) p.LineTo(V(xy[i], xy[i + 1]));
                p.Stroke();
            }

            switch (tipo)
            {
                case TipoManiobra.Derecha:
                case TipoManiobra.CerradoDerecha:
                case TipoManiobra.Izquierda:
                case TipoManiobra.CerradoIzquierda:
                    // Sube y gira 90° (maqueta: "M6 20V12q0-5 5-5H19" y la punta "M14 2l5 5-5 5")
                    p.BeginPath();
                    p.MoveTo(V(6f, 21f));
                    p.LineTo(V(6f, 12f));
                    p.QuadraticCurveTo(V(6f, 7f), V(11f, 7f));
                    p.LineTo(V(19f, 7f));
                    p.Stroke();
                    Trazo(14f, 2f, 19f, 7f, 14f, 12f);
                    break;
                case TipoManiobra.LigeroDerecha:
                case TipoManiobra.LigeroIzquierda:
                    Trazo(8f, 21f, 8f, 13f, 17f, 4f);
                    Trazo(11f, 4f, 17f, 4f, 17f, 10f);
                    break;
                case TipoManiobra.CambioSentido:
                    // Sube, da la vuelta por arriba y baja con la punta abajo
                    p.BeginPath();
                    p.MoveTo(V(7f, 21f));
                    p.LineTo(V(7f, 10f));
                    p.Arc(V(12f, 10f), 5f * k, 180f, 360f, ArcDirection.Clockwise);
                    p.LineTo(V(17f, 17f));
                    p.Stroke();
                    Trazo(13f, 13f, 17f, 17f, 21f, 13f);
                    break;
                case TipoManiobra.Rotonda:
                case TipoManiobra.SalidaRotonda:
                    p.BeginPath();
                    p.Arc(V(12f, 10f), 4.5f * k, 0f, 360f);
                    p.ClosePath();
                    p.Stroke();
                    Trazo(12f, 14.5f, 12f, 22f);
                    Trazo(15.2f, 6.8f, 20f, 2f);
                    Trazo(15f, 2f, 20f, 2f, 20f, 7f);
                    break;
                case TipoManiobra.Llegada:
                    p.BeginPath();
                    p.Arc(V(12f, 12f), 7.5f * k, 0f, 360f);
                    p.ClosePath();
                    p.Stroke();
                    p.BeginPath();
                    p.Arc(V(12f, 12f), 3f * k, 0f, 360f);
                    p.ClosePath();
                    p.Fill();
                    break;
                default:
                    // Recto (también salida, incorporación y otras): "M12 21V4" y la punta "M6 10l6-6 6 6"
                    Trazo(12f, 21f, 12f, 4f);
                    Trazo(6f, 10f, 12f, 4f, 18f, 10f);
                    break;
            }
        }
    }
}
