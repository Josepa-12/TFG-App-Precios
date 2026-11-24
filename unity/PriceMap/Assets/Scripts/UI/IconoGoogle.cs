using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// La «G» de Google en sus cuatro colores (historia 3.3), dibujada con vectores. Son los de su marca, iguales en los dos
    /// temas y fuera de la paleta.
    /// </summary>
    public class IconoGoogle : VisualElement
    {
        const float LadoViewBox = 48f;

        /// <summary>Cada trozo, con su color (viewBox de 48).</summary>
        internal static readonly (Color32 color, string trazo)[] Trozos =
        {
            (new Color32(0xEA, 0x43, 0x35, 0xFF), "M24 9.5c3.5 0 6.6 1.2 9.1 3.6l6.8-6.8C35.8 2.4 30.3 0 24 0 14.6 0 6.5 5.4 2.6 13.2l7.9 6.2C12.4 13.7 17.7 9.5 24 9.5z"),
            (new Color32(0x42, 0x85, 0xF4, 0xFF), "M47 24.6c0-1.6-.2-3.1-.4-4.6H24v9h12.9c-.6 3-2.3 5.5-4.8 7.2l7.7 6c4.5-4.2 7.2-10.3 7.2-17.6z"),
            (new Color32(0xFB, 0xBC, 0x05, 0xFF), "M10.5 28.6c-.5-1.5-.8-3-.8-4.6s.3-3.1.8-4.6l-7.9-6.2C.9 16.5 0 20.1 0 24s.9 7.5 2.6 10.8l7.9-6.2z"),
            (new Color32(0x34, 0xA8, 0x53, 0xFF), "M24 48c6.5 0 11.9-2.1 15.9-5.8l-7.7-6c-2.2 1.5-4.9 2.3-8.2 2.3-6.3 0-11.6-4.2-13.5-9.9l-7.9 6.2C6.5 42.6 14.6 48 24 48z"),
        };

        static List<IconosLugar.Orden>[] ordenes;

        /// <summary>Las órdenes de dibujo de cada trozo (se interpretan una vez).</summary>
        internal static IReadOnlyList<List<IconosLugar.Orden>> Ordenes => ordenes ??= Trozos.Select(t =>
        {
            var lista = new List<IconosLugar.Orden>();
            IconosLugar.InterpretarRuta(t.trazo, lista);
            return lista;
        }).ToArray();

        public IconoGoogle()
        {
            name = "icono-google";
            AddToClassList("cuenta__google-icono");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Dibujar;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        void Dibujar(MeshGenerationContext mgc)
        {
            var r = contentRect;
            float s = Mathf.Min(r.width, r.height) / LadoViewBox;
            if (float.IsNaN(s) || s <= 0f) return;
            var origen = r.center - new Vector2(LadoViewBox, LadoViewBox) * (s / 2f);
            Vector2 A(Vector2 v) => origen + v * s;
            var p = mgc.painter2D;
            for (int i = 0; i < Trozos.Length; i++)
            {
                p.fillColor = Trozos[i].color;
                p.BeginPath();
                foreach (var o in Ordenes[i])
                    switch (o.Tipo)
                    {
                        case IconosLugar.TipoOrden.Mover: p.MoveTo(A(o.P3)); break;
                        case IconosLugar.TipoOrden.Linea: p.LineTo(A(o.P3)); break;
                        case IconosLugar.TipoOrden.Curva: p.BezierCurveTo(A(o.P1), A(o.P2), A(o.P3)); break;
                        case IconosLugar.TipoOrden.Cerrar: p.ClosePath(); break;
                    }
                p.Fill();
            }
        }
    }
}
