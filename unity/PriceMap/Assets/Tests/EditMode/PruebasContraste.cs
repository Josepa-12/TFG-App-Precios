using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PriceMap.Vectorial;
using UnityEngine;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Contraste de los tokens de Tokens.uss en los dos temas (NFR-4): texto ≥ 4,5:1 y bordes de
    /// controles ≥ 3:1, también sobre el cristal de los botones puesto encima del fondo del mapa.
    /// </summary>
    public class PruebasContraste
    {
        static string Uss => File.ReadAllText(Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));

        /// <summary>Variables --color-* de un bloque del USS.</summary>
        static Dictionary<string, Color> Colores(string selector)
        {
            var bloque = Regex.Match(Uss, Regex.Escape(selector) + @"\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            var r = new Dictionary<string, Color>();
            foreach (Match m in Regex.Matches(bloque, @"--color-(?<n>[\w-]+):\s*(?<v>#[0-9A-Fa-f]{6}|rgba\([^)]*\))"))
                r[m.Groups["n"].Value] = Leer(m.Groups["v"].Value);
            return r;
        }

        internal static Color Leer(string v)
        {
            if (v.StartsWith("#")) { ColorUtility.TryParseHtmlString(v, out var c); return c; }
            var p = v.Substring(5, v.Length - 6).Split(',');
            float F(int i) => float.Parse(p[i].Trim(), CultureInfo.InvariantCulture);
            return new Color(F(0) / 255f, F(1) / 255f, F(2) / 255f, F(3));
        }

        static Color Sobre(Color arriba, Color abajo) =>
            new Color(Mathf.Lerp(abajo.r, arriba.r, arriba.a), Mathf.Lerp(abajo.g, arriba.g, arriba.a), Mathf.Lerp(abajo.b, arriba.b, arriba.a), 1f);

        static float Luminancia(Color c)
        {
            float L(float x) => x <= 0.03928f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }

        internal static float Contraste(Color a, Color b)
        {
            float la = Luminancia(a), lb = Luminancia(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        static Dictionary<string, Color> Tema(bool oscuro)
        {
            var claro = Colores(".pm-raiz");
            if (!oscuro) return claro;
            foreach (var par in Colores(".pm-raiz.tema-oscuro")) claro[par.Key] = par.Value;
            return claro;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TextosYControlesCumplenElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            var cristal = Sobre(c["glass"], c["map-fondo"]);
            Assert.GreaterOrEqual(Contraste(c["text"], c["bg"]), 4.5f, "texto sobre bg");
            Assert.GreaterOrEqual(Contraste(c["text"], c["surface"]), 4.5f, "texto sobre surface");
            Assert.GreaterOrEqual(Contraste(c["muted"], c["surface"]), 4.5f, "muted sobre surface");
            Assert.GreaterOrEqual(Contraste(c["text"], cristal), 4.5f, "icono sobre el cristal encima del mapa");
            Assert.GreaterOrEqual(Contraste(c["muted"], cristal), 4.5f, "atribución sobre el cristal encima del mapa");
            Assert.GreaterOrEqual(Contraste(c["control-edge"], cristal), 3f, "borde del botón sobre el cristal");
            Assert.GreaterOrEqual(Contraste(c["accent-ink"], c["accent"]), 4.5f, "texto sobre el acento");
            Assert.GreaterOrEqual(Contraste(c["text"], c["surface-2"]), 4.5f, "icono del botón pulsado (surface-2)");
            Assert.GreaterOrEqual(Contraste(c["focus"], cristal), 3f, "borde de foco sobre el cristal");
            Assert.GreaterOrEqual(Contraste(c["ahora-texto"], c["surface-2"]), 4.5f, "chip \"Ahora\" de la variante vigente");
            Assert.GreaterOrEqual(Contraste(c["accent-text"], cristal), 4.5f, "icono de la columna de controles sobre el cristal");
            Assert.GreaterOrEqual(Contraste(c["accent-text"], c["surface-2"]), 4.5f, "icono de la columna pulsado (surface-2)");
        }

        /// <summary>El cristal no está siempre sobre el fondo: puede caer sobre calles, agua, verde o edificios.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ElCristalSeLeeSobreCualquierColorDelMapa(bool oscuro)
        {
            var c = Tema(oscuro);
            var p = PaletaMapa.De(oscuro ? TemaMapa.Oscuro : TemaMapa.Claro);
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("agua", p.agua), ("verde", p.verde), ("calle", p.calle),
                                                    ("calle menor", p.calleMenor), ("borde", p.borde), ("edificio", p.edificio), ("fachada", p.fachada) })
            {
                var cristal = Sobre(c["glass"], fondo);
                Assert.GreaterOrEqual(Contraste(c["text"], cristal), 4.5f, $"icono sobre el cristal encima de {nombre}");
                Assert.GreaterOrEqual(Contraste(c["muted"], cristal), 4.5f, $"atribución sobre el cristal encima de {nombre}");
                Assert.GreaterOrEqual(Contraste(c["accent-text"], cristal), 4.5f, $"icono de la columna de controles sobre el cristal encima de {nombre}");
            }
        }

        /// <summary>El nombre de cada punto de interés se lee sobre el fondo del mapa de su tema (≥ 4,5:1).</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LosNombresDeLosPuntosDeInteresCumplenElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            int comprobados = 0;
            foreach (var par in c)
            {
                if (!par.Key.StartsWith("poi-") || !par.Key.EndsWith("-texto")) continue;
                comprobados++;
                Assert.GreaterOrEqual(Contraste(par.Value, c["map-fondo"]), 4.5f, $"--color-{par.Key} sobre el fondo del mapa");
            }
            Assert.AreEqual(22, comprobados, "Un color de nombre por categoría.");
        }

        /// <summary>El fondo del mapa está en dos sitios (USS y PaletaMapa): tienen que coincidir.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ElFondoDelMapaEsElMismoEnLaUIYEnElMapa(bool oscuro)
        {
            var uss = Tema(oscuro)["map-fondo"];
            var mapa = PaletaMapa.De(oscuro ? TemaMapa.Oscuro : TemaMapa.Claro).fondo;
            Assert.AreEqual(ColorUtility.ToHtmlStringRGB(mapa), ColorUtility.ToHtmlStringRGB(uss));
        }
    }
}
