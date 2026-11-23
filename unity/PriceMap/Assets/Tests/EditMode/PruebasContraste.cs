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

        /// <summary>
        /// Ruta (historia 2.6, parte 2a): los textos de la tarjeta de acciones (≥ 4,5:1) sobre ella, puesta encima de
        /// cualquier color del mapa; la línea (relleno) ≥ 3:1 sobre las calles y el fondo; los textos de la tarjeta de
        /// abajo sobre surface.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LaRutaCumpleElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            var p = PaletaMapa.De(oscuro ? TemaMapa.Oscuro : TemaMapa.Claro);
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("agua", p.agua), ("verde", p.verde), ("calle", p.calle),
                                                    ("calle menor", p.calleMenor), ("borde", p.borde), ("edificio", p.edificio), ("fachada", p.fachada) })
            {
                var tarjeta = Sobre(c["tarjeta-maniobra"], fondo);
                Assert.GreaterOrEqual(Contraste(c["tarjeta-maniobra-texto"], tarjeta), 4.5f, $"maniobra sobre la tarjeta encima de {nombre}");
                Assert.GreaterOrEqual(Contraste(c["tarjeta-maniobra-meta"], tarjeta), 4.5f, $"maniobra siguiente sobre la tarjeta encima de {nombre}");
            }
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("calle", p.calle), ("calle menor", p.calleMenor) })
                Assert.GreaterOrEqual(Contraste(p.ruta, fondo), 3f, $"línea de la ruta sobre {nombre}");
            Assert.GreaterOrEqual(Contraste(c["text"], c["surface"]), 4.5f, "valores de la tarjeta de la ruta");
            Assert.GreaterOrEqual(Contraste(c["muted"], c["surface"]), 4.5f, "rótulos de la tarjeta de la ruta");
        }

        /// <summary>
        /// Hoja de la ruta desplegada (22/11/2026): "Finalizar ruta" con texto surface sobre danger (≥ 4,5:1, AA) y la
        /// fila del destino (nombre text y detalle muted sobre surface-2).
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LaHojaDesplegadaCumpleElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            Assert.GreaterOrEqual(Contraste(c["surface"], c["danger"]), 4.5f, "«Finalizar ruta» sobre el rojo");
            Assert.GreaterOrEqual(Contraste(c["text"], c["surface-2"]), 4.5f, "nombre del destino");
            Assert.GreaterOrEqual(Contraste(c["muted"], c["surface-2"]), 4.5f, "detalle del destino");
            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            var boton = Regex.Match(uss, @"\.tarjeta-ruta__finalizar\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("background-color: var(--color-danger);", boton);
            var texto = Regex.Match(uss, @"\.tarjeta-ruta__finalizar-texto\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("color: var(--color-surface);", texto);
        }

        /// <summary>
        /// "Has llegado" (historia 2.9): el título y el destino sobre la tarjeta de arriba (los de la de maniobras, encima
        /// de cualquier color del mapa, en LaRutaCumpleElContraste), la marca sobre su círculo, "Finalizar ruta" sobre el
        /// acento y el aviso sobre surface (≥ 4,5:1).
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LaLlegadaCumpleElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            Assert.GreaterOrEqual(Contraste(c["success-ink"], c["success"]), 4.5f, "marca de hecho sobre su círculo");
            Assert.GreaterOrEqual(Contraste(c["accent-ink"], c["accent"]), 4.5f, "«Finalizar ruta» sobre el acento");
            Assert.GreaterOrEqual(Contraste(c["muted"], c["surface"]), 4.5f, "aviso del cierre");
            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            string Bloque(string selector) => Regex.Match(uss, Regex.Escape(selector) + @"\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("background-color: var(--color-success);", Bloque(".tarjeta-llegada__marca"));
            StringAssert.Contains("color: var(--color-success-ink);", Bloque(".tarjeta-llegada__marca"));
            StringAssert.Contains("color: var(--color-tarjeta-maniobra-texto);", Bloque(".tarjeta-llegada__titulo"));
            StringAssert.Contains("color: var(--color-tarjeta-maniobra-meta);", Bloque(".tarjeta-llegada__destino"));
            StringAssert.Contains("background-color: var(--color-accent);", Bloque(".hoja-llegada__finalizar"));
            StringAssert.Contains("color: var(--color-accent-ink);", Bloque(".hoja-llegada__finalizar-texto"));
            StringAssert.Contains("color: var(--color-muted);", Bloque(".hoja-llegada__aviso"));
        }

        /// <summary>
        /// Ruta aproximada (historia 2.6, parte 2b; colores del 21/11/2026): la línea se distingue (≥ 3:1) de las calles y
        /// del fondo por su relleno o por su borde (en claro el relleno es suave y lo marca el borde; en oscuro, al revés),
        /// es distinta de la ruta normal, y el toast (fondo text, texto bg) se lee (≥ 4,5:1) y destaca sobre el mapa.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LaRutaAproximadaYElToastCumplenElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            var p = PaletaMapa.De(oscuro ? TemaMapa.Oscuro : TemaMapa.Claro);
            Assert.AreEqual(oscuro ? "6F86C9" : "8FA3E6", ColorUtility.ToHtmlStringRGB(p.rutaAproximada));
            Assert.AreEqual(oscuro ? "44599A" : "5F78C9", ColorUtility.ToHtmlStringRGB(p.rutaAproximadaBorde));
            Assert.AreNotEqual(p.ruta, p.rutaAproximada, "Se distingue de la ruta normal.");
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("calle", p.calle), ("calle menor", p.calleMenor) })
            {
                float linea = Mathf.Max(Contraste(p.rutaAproximada, fondo), Contraste(p.rutaAproximadaBorde, fondo));
                Assert.GreaterOrEqual(linea, 3f, $"línea de la ruta aproximada sobre {nombre}");
            }
            Assert.AreEqual(ColorUtility.ToHtmlStringRGB(c["text"]), ColorUtility.ToHtmlStringRGB(c["toast"]), "Fondo del toast: text.");
            Assert.AreEqual(ColorUtility.ToHtmlStringRGB(c["bg"]), ColorUtility.ToHtmlStringRGB(c["toast-texto"]), "Texto del toast: bg.");
            Assert.GreaterOrEqual(Contraste(c["toast-texto"], c["toast"]), 4.5f, "texto del toast");
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("calle", p.calle), ("edificio", p.edificio) })
                Assert.GreaterOrEqual(Contraste(c["toast"], fondo), 3f, $"toast sobre {nombre}");
        }

        /// <summary>
        /// «Cómo llegar» (parte 5b): textos de la hoja (modos y tarjetas, elegidos o no) y de las etiquetas de minutos ≥ 4,5:1;
        /// borde del modo y de la tarjeta elegidos ≥ 3:1; las líneas no elegidas (relleno o borde) ≥ 3:1 sobre las calles y
        /// el fondo, y distintas de la elegida.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ComoLlegarCumpleElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            var p = PaletaMapa.De(oscuro ? TemaMapa.Oscuro : TemaMapa.Claro);
            foreach (var fondo in new[] { "surface", "surface-2" })
            {
                Assert.GreaterOrEqual(Contraste(c["text"], c[fondo]), 4.5f, $"minutos y títulos sobre {fondo}");
                Assert.GreaterOrEqual(Contraste(c["muted"], c[fondo]), 4.5f, $"detalle y modo no elegido sobre {fondo}");
                Assert.GreaterOrEqual(Contraste(c["accent"], c[fondo]), 3f, $"borde de la tarjeta elegida sobre {fondo}");
                Assert.GreaterOrEqual(Contraste(c["control-edge"], c[fondo]), 3f, $"borde del modo elegido sobre {fondo}");
            }
            Assert.GreaterOrEqual(Contraste(c["accent-ink"], c["accent"]), 4.5f, "«Iniciar» y la etiqueta de la elegida");
            Assert.AreNotEqual(p.ruta, p.rutaAlternativa);
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("calle", p.calle), ("calle menor", p.calleMenor) })
            {
                float linea = Mathf.Max(Contraste(p.rutaAlternativa, fondo), Contraste(p.rutaAlternativaBorde, fondo));
                Assert.GreaterOrEqual(linea, 3f, $"línea no elegida sobre {nombre}");
            }
            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            string Bloque(string selector) => Regex.Match(uss, Regex.Escape(selector) + @"\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("background-color: var(--color-accent);", Bloque(".como-llegar__iniciar"));
            StringAssert.Contains("color: var(--color-accent-ink);", Bloque(".como-llegar__iniciar-texto"));
            StringAssert.Contains("background-color: var(--color-accent);", Bloque(".minutos-ruta--elegida .minutos-ruta__texto"));
            StringAssert.Contains("color: var(--color-accent-ink);", Bloque(".minutos-ruta--elegida .minutos-ruta__texto"));
        }

        /// <summary>
        /// Flecha y "Buscando señal GPS" (historia 2.7, parte 2c): la flecha (#2F5BEA, en oscuro #4C7DFF) se distingue
        /// (≥ 3:1) de su borde blanco y, con él, de cualquier color del mapa; la pastilla lleva los colores del toast (texto
        /// ≥ 4,5:1) y se ve sobre el mapa.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LaFlechaYLaPastillaDeSenalCumplenElContraste(bool oscuro)
        {
            var c = Tema(oscuro);
            var p = PaletaMapa.De(oscuro ? TemaMapa.Oscuro : TemaMapa.Claro);
            Assert.AreEqual(oscuro ? "4C7DFF" : "2F5BEA", ColorUtility.ToHtmlStringRGB(c["flecha"]));
            Assert.AreEqual("FFFFFF", ColorUtility.ToHtmlStringRGB(c["posicion-borde"]));
            Assert.GreaterOrEqual(Contraste(c["flecha"], c["posicion-borde"]), 3f, "flecha sobre su borde");
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("agua", p.agua), ("verde", p.verde), ("calle", p.calle),
                                                    ("calle menor", p.calleMenor), ("edificio", p.edificio), ("ruta", p.ruta) })
            {
                float flecha = Mathf.Max(Contraste(c["flecha"], fondo), Contraste(c["posicion-borde"], fondo));
                Assert.GreaterOrEqual(flecha, 3f, $"flecha (o su borde) sobre {nombre}");
            }
            Assert.GreaterOrEqual(Contraste(c["toast-texto"], c["toast"]), 4.5f, "texto de la pastilla");
            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "UI/PantallaMapa.uss"));
            var bloque = Regex.Match(uss, @"\.pastilla-senal__texto\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("background-color: var(--color-toast);", bloque);
            StringAssert.Contains("color: var(--color-toast-texto);", bloque);
            foreach (var (nombre, fondo) in new[] { ("fondo", p.fondo), ("calle", p.calle), ("edificio", p.edificio) })
                Assert.GreaterOrEqual(Contraste(c["toast"], fondo), 3f, $"pastilla sobre {nombre}");
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
