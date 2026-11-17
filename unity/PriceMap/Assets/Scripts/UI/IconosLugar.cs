using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Logos de los tipos de negocio en el marcador (UX-DR23: vectores, nunca emojis). Los trazos son los
    /// del prototipo (prototipo/lugares.js · GLYPH, estilo Lucide con viewBox 24) y se dibujan con
    /// Painter2D a través de un intérprete pequeño de SVG: path (M/L/H/V/C/S/A/Z), circle y rect con rx.
    /// </summary>
    public static class IconosLugar
    {
        /// <summary>Icono genérico para un tipo que no está en la tabla.</summary>
        public const string Generico = "pin";

        /// <summary>Grosor del trazo en unidades del viewBox (prototipo: .pin-glyph stroke-width 2.2).</summary>
        const float GrosorTrazo = 2.2f;
        const float LadoViewBox = 24f;

        // Tipo de negocio → glifo. Sale de PLACE_POICAT → POI_CATS[...].glyph, con las excepciones de
        // PLACE_GLYPH (pelota para los campos y pistas, monumento, discoteca).
        static readonly Dictionary<string, string> GlifoPorTipo = new Dictionary<string, string>
        {
            ["Restaurante"] = "utensils", ["Taberna"] = "utensils", ["Comida rápida"] = "burger",
            ["Cafetería"] = "coffee", ["Heladería"] = "coffee",
            ["Bar"] = "martini", ["Pub"] = "martini", ["Discoteca"] = "martini",
            ["Centro deportivo"] = "dumbbell", ["Piscina"] = "dumbbell", ["Club de golf"] = "dumbbell", ["Club náutico"] = "dumbbell",
            ["Campo de fútbol"] = "ball", ["Pista de pádel"] = "ball", ["Pista de tenis"] = "ball", ["Recinto deportivo"] = "ball",
            ["Cine"] = "ticket", ["Teatro"] = "ticket", ["Museo"] = "ticket", ["Visita turística"] = "ticket",
            ["Monumento"] = "landmark", ["Aparcamiento"] = "parking",
            ["Peluquería"] = "scissors", ["Barbería"] = "scissors", ["Centro de belleza"] = "scissors",
            ["Clínica dental"] = "cross", ["Clínica médica"] = "cross", ["Clínica privada"] = "cross",
            ["Fisioterapia"] = "cross", ["Veterinario"] = "cross",
            ["Taller"] = "wrench", ["Lavado de coches"] = "wrench", ["Lavandería"] = "bag", ["Autoescuela"] = "school",
        };

        // Trazos copiados tal cual de prototipo/lugares.js (GLYPH): solo los que usa la tabla, más "pin"
        static readonly Dictionary<string, string> Trazos = new Dictionary<string, string>
        {
            ["utensils"] = "<path d=\"M3 2v7c0 1.1.9 2 2 2h4a2 2 0 0 0 2-2V2M7 2v20M21 15V2a5 5 0 0 0-5 5v6c0 1.1.9 2 2 2h3Zm0 0v7\"/>",
            ["burger"] = "<path d=\"M4 11h16a8 8 0 0 0-16 0ZM3 15h18M5 15v1a3 3 0 0 0 3 3h8a3 3 0 0 0 3-3v-1\"/>",
            ["coffee"] = "<path d=\"M17 8h1a4 4 0 1 1 0 8h-1M3 8h14v9a4 4 0 0 1-4 4H7a4 4 0 0 1-4-4ZM6 2v2M10 2v2M14 2v2\"/>",
            ["martini"] = "<path d=\"M8 22h8M12 11v11M19 3l-7 8-7-8Z\"/>",
            ["bag"] = "<path d=\"M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4ZM3 6h18M16 10a4 4 0 0 1-8 0\"/>",
            ["parking"] = "<path d=\"M9 17V7h4a3 3 0 0 1 0 6H9\"/>",
            ["cross"] = "<path d=\"M9 3h6v6h6v6h-6v6H9v-6H3V9h6Z\"/>",
            ["dumbbell"] = "<path d=\"M6.5 6.5v11M17.5 6.5v11M3 9.5v5M21 9.5v5M6.5 12h11\"/>",
            ["ticket"] = "<path d=\"M3 9a3 3 0 0 0 0 6v3a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1v-3a3 3 0 0 0 0-6V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1ZM13 5v2M13 17v2M13 11v2\"/>",
            ["scissors"] = "<circle cx=\"6\" cy=\"6\" r=\"3\"/><path d=\"M8.1 8.1 12 12M20 4 8.1 15.9\"/><circle cx=\"6\" cy=\"18\" r=\"3\"/><path d=\"M14.8 14.8 20 20\"/>",
            ["wrench"] = "<path d=\"M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.8-3.8a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9Z\"/>",
            ["school"] = "<path d=\"M22 10 12 5 2 10l10 5 10-5ZM6 12v5c3 2 9 2 12 0v-5\"/>",
            ["landmark"] = "<path d=\"M3 22h18M6 18v-7M10 18v-7M14 18v-7M18 18v-7M12 2l8 5H4Z\"/>",
            ["ball"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m12 7 4 3-1.5 5h-5L8 10Z\"/>",
            ["pin"] = "<path d=\"M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z\"/><circle cx=\"12\" cy=\"10\" r=\"3\"/>",
        };

        static readonly HashSet<string> tiposAvisados = new HashSet<string>();
        static readonly Dictionary<string, IReadOnlyList<Orden>> interpretados = new Dictionary<string, IReadOnlyList<Orden>>();

        /// <summary>Nombres de todos los glifos disponibles.</summary>
        public static IEnumerable<string> Glifos => Trazos.Keys;

        /// <summary>True si el tipo tiene un logo propio en la tabla (no el genérico).</summary>
        public static bool TieneGlifoPropio(string tipo) => tipo != null && GlifoPorTipo.ContainsKey(tipo);

        /// <summary>Glifo del tipo de negocio; si no está en la tabla, "pin" (y un aviso por tipo).</summary>
        public static string GlifoDe(string tipo)
        {
            if (tipo != null && GlifoPorTipo.TryGetValue(tipo, out var glifo)) return glifo;
            if (tiposAvisados.Add(tipo ?? ""))
                Debug.LogWarning($"PriceMap: el tipo de negocio \"{tipo}\" no tiene logo; se usa el genérico.");
            return Generico;
        }

        // ───────────────────────── Dibujo ─────────────────────────

        /// <summary>Dibuja el glifo (trazo, sin relleno) ajustando su viewBox de 24 al rectángulo dado.</summary>
        public static void Dibujar(Painter2D p, string glifo, Rect destino, Color color)
        {
            var ordenes = Interpretar(glifo);
            float escala = Mathf.Min(destino.width, destino.height) / LadoViewBox;
            if (escala <= 0f || float.IsNaN(escala)) return;
            var origen = destino.center - new Vector2(LadoViewBox, LadoViewBox) * (escala / 2f);
            Vector2 A(Vector2 v) => origen + v * escala;

            p.strokeColor = color;
            p.lineWidth = GrosorTrazo * escala;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            foreach (var o in ordenes)
            {
                switch (o.Tipo)
                {
                    case TipoOrden.Mover: p.MoveTo(A(o.P3)); break;
                    case TipoOrden.Linea: p.LineTo(A(o.P3)); break;
                    case TipoOrden.Curva: p.BezierCurveTo(A(o.P1), A(o.P2), A(o.P3)); break;
                    case TipoOrden.Cerrar: p.ClosePath(); break;
                    case TipoOrden.Circulo:
                        p.MoveTo(A(o.P3 + new Vector2(o.Radio, 0f)));
                        p.Arc(A(o.P3), o.Radio * escala, 0f, 360f);
                        p.ClosePath();
                        break;
                }
            }
            p.Stroke();
        }

        // ───────────────────────── Intérprete ─────────────────────────

        public enum TipoOrden { Mover, Linea, Curva, Cerrar, Circulo }

        /// <summary>Una orden de dibujo en coordenadas absolutas del viewBox (Curva: P1, P2 de control y P3 final).</summary>
        public readonly struct Orden
        {
            public readonly TipoOrden Tipo;
            public readonly Vector2 P1, P2, P3;
            public readonly float Radio;

            public Orden(TipoOrden tipo, Vector2 p1, Vector2 p2, Vector2 p3, float radio = 0f)
            {
                Tipo = tipo; P1 = p1; P2 = p2; P3 = p3; Radio = radio;
            }
        }

        static readonly Regex Elemento = new Regex(@"<(path|circle|rect)\b([^>]*?)/?>", RegexOptions.Compiled);
        static readonly Regex Atributo = new Regex(@"([\w-]+)\s*=\s*""([^""]*)""", RegexOptions.Compiled);

        /// <summary>Órdenes del glifo (se interpretan una vez). Lanza FormatException si el trazo está mal.</summary>
        public static IReadOnlyList<Orden> Interpretar(string glifo)
        {
            if (glifo == null || !Trazos.TryGetValue(glifo, out var svg)) glifo = Generico;
            if (interpretados.TryGetValue(glifo, out var hechas)) return hechas;
            var r = InterpretarSvg(Trazos[glifo]).AsReadOnly();
            interpretados[glifo] = r;
            return r;
        }

        /// <summary>Interpreta un fragmento SVG con elementos path, circle y rect.</summary>
        public static List<Orden> InterpretarSvg(string svg)
        {
            var salida = new List<Orden>();
            foreach (Match m in Elemento.Matches(svg ?? ""))
            {
                var at = new Dictionary<string, string>();
                foreach (Match a in Atributo.Matches(m.Groups[2].Value)) at[a.Groups[1].Value] = a.Groups[2].Value;
                switch (m.Groups[1].Value)
                {
                    case "path":
                        if (!at.TryGetValue("d", out var d)) throw new FormatException("path sin atributo d.");
                        InterpretarRuta(d, salida);
                        break;
                    case "circle":
                        salida.Add(new Orden(TipoOrden.Circulo, default, default,
                            new Vector2(Num(at, "cx"), Num(at, "cy")), Num(at, "r")));
                        break;
                    case "rect":
                        Rectangulo(at, salida);
                        break;
                }
            }
            if (salida.Count == 0) throw new FormatException("El glifo no tiene ningún trazo.");
            return salida;
        }

        static float Num(Dictionary<string, string> at, string nombre, float porDefecto = 0f)
        {
            if (!at.TryGetValue(nombre, out var v)) return porDefecto;
            return float.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static void Rectangulo(Dictionary<string, string> at, List<Orden> salida)
        {
            float x = Num(at, "x"), y = Num(at, "y"), w = Num(at, "width"), h = Num(at, "height");
            float rx = Num(at, "rx", at.ContainsKey("ry") ? Num(at, "ry") : 0f);
            float ry = Num(at, "ry", rx);
            rx = Mathf.Min(rx, w / 2f); ry = Mathf.Min(ry, h / 2f);
            // Invariant: con la cultura del móvil (es-ES) los decimales saldrían con coma
            var ruta = rx <= 0f || ry <= 0f
                ? FormattableString.Invariant($"M{x} {y}H{x + w}V{y + h}H{x}Z")
                : FormattableString.Invariant($"M{x + rx} {y}H{x + w - rx}A{rx} {ry} 0 0 1 {x + w} {y + ry}V{y + h - ry}A{rx} {ry} 0 0 1 {x + w - rx} {y + h}") +
                  FormattableString.Invariant($"H{x + rx}A{rx} {ry} 0 0 1 {x} {y + h - ry}V{y + ry}A{rx} {ry} 0 0 1 {x + rx} {y}Z");
            InterpretarRuta(ruta, salida);
        }

        /// <summary>Atributo d de un path: M/L/H/V/C/S/A/Z en absoluto y relativo, con números compactos.</summary>
        public static void InterpretarRuta(string d, List<Orden> salida)
        {
            var l = new LectorRuta(d);
            Vector2 actual = Vector2.zero, inicio = Vector2.zero;
            char orden = '\0';
            bool cubicaPrevia = false; // para S: solo se refleja el control de una C o S anterior
            while (true)
            {
                l.SaltarSeparadores();
                if (l.Fin) break;
                char c = l.Actual;
                if (char.IsLetter(c)) { orden = c; l.Avanzar(); }
                else if (orden == '\0') throw new FormatException($"La ruta empieza sin orden: \"{d}\".");
                // Si no hay letra, se repite la orden anterior (tras M, la repetición es L)

                bool rel = char.IsLower(orden);
                Vector2 b = rel ? actual : Vector2.zero;
                char tipo = char.ToUpperInvariant(orden);
                switch (tipo)
                {
                    case 'M':
                        actual = b + new Vector2(l.Numero(), l.Numero());
                        inicio = actual;
                        salida.Add(new Orden(TipoOrden.Mover, default, default, actual));
                        orden = rel ? 'l' : 'L';
                        break;
                    case 'L':
                        actual = b + new Vector2(l.Numero(), l.Numero());
                        salida.Add(new Orden(TipoOrden.Linea, default, default, actual));
                        break;
                    case 'H':
                        actual = new Vector2((rel ? actual.x : 0f) + l.Numero(), actual.y);
                        salida.Add(new Orden(TipoOrden.Linea, default, default, actual));
                        break;
                    case 'V':
                        actual = new Vector2(actual.x, (rel ? actual.y : 0f) + l.Numero());
                        salida.Add(new Orden(TipoOrden.Linea, default, default, actual));
                        break;
                    case 'C':
                    {
                        var c1 = b + new Vector2(l.Numero(), l.Numero());
                        var c2 = b + new Vector2(l.Numero(), l.Numero());
                        actual = b + new Vector2(l.Numero(), l.Numero());
                        salida.Add(new Orden(TipoOrden.Curva, c1, c2, actual));
                        break;
                    }
                    case 'S':
                    {
                        // Curva suave (la usa "pin"): el primer control es el reflejo del último de la curva anterior
                        var c1 = cubicaPrevia ? 2f * actual - salida[salida.Count - 1].P2 : actual;
                        var c2 = b + new Vector2(l.Numero(), l.Numero());
                        actual = b + new Vector2(l.Numero(), l.Numero());
                        salida.Add(new Orden(TipoOrden.Curva, c1, c2, actual));
                        break;
                    }
                    case 'A':
                    {
                        float rx = l.Numero(), ry = l.Numero(), rot = l.Numero();
                        bool grande = l.Bandera(), barrido = l.Bandera();
                        var fin = b + new Vector2(l.Numero(), l.Numero());
                        Arco(actual, rx, ry, rot, grande, barrido, fin, salida);
                        actual = fin;
                        break;
                    }
                    case 'Z':
                        salida.Add(new Orden(TipoOrden.Cerrar, default, default, inicio));
                        actual = inicio;
                        orden = '\0'; // tras Z hace falta una orden nueva
                        break;
                    default:
                        throw new FormatException($"Orden de ruta no admitida: '{orden}'.");
                }
                cubicaPrevia = tipo == 'C' || tipo == 'S';
            }
        }

        /// <summary>Arco elíptico de SVG convertido en curvas de Bézier (como mucho 90° cada una).</summary>
        static void Arco(Vector2 p0, float rx, float ry, float rotGrados, bool grande, bool barrido, Vector2 p1, List<Orden> salida)
        {
            if (p0 == p1) return;
            if (Mathf.Approximately(rx, 0f) || Mathf.Approximately(ry, 0f))
            {
                salida.Add(new Orden(TipoOrden.Linea, default, default, p1));
                return;
            }
            double rX = Math.Abs(rx), rY = Math.Abs(ry);
            double phi = rotGrados * Math.PI / 180.0, cos = Math.Cos(phi), sin = Math.Sin(phi);
            double dx2 = (p0.x - p1.x) / 2.0, dy2 = (p0.y - p1.y) / 2.0;
            double x1 = cos * dx2 + sin * dy2, y1 = -sin * dx2 + cos * dy2;

            double lambda = x1 * x1 / (rX * rX) + y1 * y1 / (rY * rY);
            if (lambda > 1) { double s = Math.Sqrt(lambda); rX *= s; rY *= s; }

            double num = rX * rX * rY * rY - rX * rX * y1 * y1 - rY * rY * x1 * x1;
            double den = rX * rX * y1 * y1 + rY * rY * x1 * x1;
            double coef = den == 0 ? 0 : Math.Sqrt(Math.Max(0, num / den));
            if (grande == barrido) coef = -coef;
            double cxp = coef * rX * y1 / rY, cyp = -coef * rY * x1 / rX;
            double cx = cos * cxp - sin * cyp + (p0.x + p1.x) / 2.0;
            double cy = sin * cxp + cos * cyp + (p0.y + p1.y) / 2.0;

            double Angulo(double ux, double uy, double vx, double vy) => Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
            double t1 = Angulo(1, 0, (x1 - cxp) / rX, (y1 - cyp) / rY);
            double dt = Angulo((x1 - cxp) / rX, (y1 - cyp) / rY, (-x1 - cxp) / rX, (-y1 - cyp) / rY);
            if (!barrido && dt > 0) dt -= 2 * Math.PI;
            else if (barrido && dt < 0) dt += 2 * Math.PI;

            int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(dt) / (Math.PI / 2) - 1e-9));
            double paso = dt / n, k = 4.0 / 3.0 * Math.Tan(paso / 4);
            Vector2 P(double a) => new Vector2(
                (float)(cx + rX * Math.Cos(a) * cos - rY * Math.Sin(a) * sin),
                (float)(cy + rX * Math.Cos(a) * sin + rY * Math.Sin(a) * cos));
            Vector2 D(double a) => new Vector2(
                (float)(-rX * Math.Sin(a) * cos - rY * Math.Cos(a) * sin),
                (float)(-rX * Math.Sin(a) * sin + rY * Math.Cos(a) * cos));
            for (int i = 0; i < n; i++)
            {
                double a1 = t1 + i * paso, a2 = a1 + paso;
                var fin = i == n - 1 ? p1 : P(a2);
                salida.Add(new Orden(TipoOrden.Curva, P(a1) + D(a1) * (float)k, P(a2) - D(a2) * (float)k, fin));
            }
        }

        /// <summary>Lee números y banderas de un atributo d ("2-2", ".6", "1.1.9", "0 01").</summary>
        sealed class LectorRuta
        {
            readonly string s;
            int i;

            public LectorRuta(string s) { this.s = s ?? ""; }

            public bool Fin => i >= s.Length;
            public char Actual => s[i];
            public void Avanzar() => i++;

            public void SaltarSeparadores()
            {
                while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
            }

            public float Numero()
            {
                SaltarSeparadores();
                int ini = i;
                if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                bool digitos = false;
                while (i < s.Length && char.IsDigit(s[i])) { i++; digitos = true; }
                if (i < s.Length && s[i] == '.')
                {
                    i++;
                    while (i < s.Length && char.IsDigit(s[i])) { i++; digitos = true; }
                }
                if (!digitos) throw new FormatException($"Se esperaba un número en la posición {ini} de \"{s}\".");
                if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
                {
                    int antes = i;
                    i++;
                    if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                    if (i < s.Length && char.IsDigit(s[i])) { while (i < s.Length && char.IsDigit(s[i])) i++; }
                    else i = antes; // no era un exponente
                }
                return float.Parse(s.Substring(ini, i - ini), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            public bool Bandera()
            {
                SaltarSeparadores();
                if (i < s.Length && (s[i] == '0' || s[i] == '1')) return s[i++] == '1';
                throw new FormatException($"Se esperaba una bandera de arco (0 o 1) en la posición {i} de \"{s}\".");
            }
        }
    }
}
