using System;
using System.Collections.Generic;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Etiquetas con los minutos de cada ruta de «Cómo llegar» sobre su línea (parte 5b, maqueta A), en el punto medio de su
    /// parte propia (ReglaParteRuta): la elegida con el acento y encima, las demás claras. Como los marcadores, se recolocan
    /// en cada fotograma y no capturan toques (el toque en el mapa las mira con EtiquetaEn). TalkBack lee las tarjetas.
    /// </summary>
    public class CapaMinutosRuta
    {
        public const string ClaseElegida = "minutos-ruta--elegida";

        readonly VisualElement contenedor;
        readonly IMapa mapa;
        readonly List<EtiquetaMinutos> etiquetas = new List<EtiquetaMinutos>();

        /// <summary>Etiquetas de las rutas de ahora, en su orden; vacía sin rutas.</summary>
        public IReadOnlyList<EtiquetaMinutos> Etiquetas => etiquetas;

        public CapaMinutosRuta(VisualElement contenedor, IMapa mapa)
        {
            this.contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));
            this.mapa = mapa ?? throw new ArgumentNullException(nameof(mapa));
            contenedor.pickingMode = PickingMode.Ignore;
        }

        /// <summary>Pone las etiquetas de esas rutas (sustituyen a las anteriores) con la elegida resaltada.</summary>
        public void Mostrar(IReadOnlyList<Ruta> rutas, int elegida, Func<Ruta, string> texto)
        {
            Quitar();
            for (int i = 0; i < rutas.Count; i++)
            {
                var e = new EtiquetaMinutos(texto(rutas[i]), ReglaParteRuta.PuntoMedio(rutas, i));
                etiquetas.Add(e);
                contenedor.Add(e);
            }
            Elegir(elegida);
        }

        public void Elegir(int indice)
        {
            for (int i = 0; i < etiquetas.Count; i++)
            {
                etiquetas[i].EnableInClassList(ClaseElegida, i == indice);
                if (i == indice) etiquetas[i].BringToFront();
            }
        }

        public void Quitar()
        {
            foreach (var e in etiquetas)
            {
                EscalaFuente.Olvidar(e);
                e.RemoveFromHierarchy();
            }
            etiquetas.Clear();
        }

        /// <summary>Recoloca las etiquetas con la cámara de este fotograma (escalaDp: píxeles de pantalla → dp).</summary>
        public void Actualizar(float escalaDp)
        {
            foreach (var e in etiquetas) e.Actualizar(mapa, escalaDp);
        }

        /// <summary>Índice de la ruta cuya etiqueta está en ese punto (dp del panel); −1 si ninguna. La elegida, la primera.</summary>
        public int EtiquetaEn(Vector2 puntoDp)
        {
            int r = -1;
            for (int i = 0; i < etiquetas.Count; i++)
                if (etiquetas[i].Contiene(puntoDp) && (r < 0 || etiquetas[i].ClassListContains(ClaseElegida))) r = i;
            return r;
        }
    }

    /// <summary>Una etiqueta de minutos: mide 0×0 en la coordenada y su píldora va centrada en ella.</summary>
    public class EtiquetaMinutos : VisualElement
    {
        readonly Coordenada? punto;

        public Label Texto { get; }
        public bool Visible { get; private set; }
        /// <summary>Centro de la píldora (dp del panel).</summary>
        public Vector2 Posicion { get; private set; }

        public EtiquetaMinutos(string texto, Coordenada? punto)
        {
            this.punto = punto;
            name = "minutos-ruta";
            AddToClassList("minutos-ruta");
            pickingMode = PickingMode.Ignore;
            usageHints = UsageHints.DynamicTransform; // se mueve con el mapa
            style.display = DisplayStyle.None;
            Texto = new Label(texto) { name = "minutos-ruta-texto", pickingMode = PickingMode.Ignore };
            Texto.AddToClassList("minutos-ruta__texto");
            Texto.AddToClassList("t-label");
            Add(Texto);
            // Se crea con la pantalla ya en marcha: la fuente del sistema, al disponerse
            Texto.RegisterCallback<GeometryChangedEvent>(_ => EscalaFuente.Aplicar(this, EscalaFuente.Sistema));
        }

        internal void Actualizar(IMapa mapa, float escalaDp)
        {
            bool ver = false;
            if (punto.HasValue && escalaDp > 0f)
            {
                var p = mapa.Proyectar(punto.Value);
                var dp = new Vector2(p.X * escalaDp, p.Y * escalaDp);
                ver = p.Delante && !float.IsNaN(dp.x) && !float.IsNaN(dp.y);
                if (ver && (!Visible || dp != Posicion))
                {
                    Posicion = dp;
                    style.translate = new Translate(dp.x, dp.y);
                }
            }
            if (ver == Visible) return;
            Visible = ver;
            style.display = ver ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal bool Contiene(Vector2 puntoDp)
        {
            if (!Visible) return false;
            var r = Texto.worldBound;
            return !float.IsNaN(r.width) && r.width > 0f && r.Contains(puntoDp);
        }
    }
}
