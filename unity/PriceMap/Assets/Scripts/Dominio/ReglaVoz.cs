using System;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Qué decir y cuándo al navegar (historia 2.8; FR21). Se le pasa cada estado nuevo de la navegación (Avisar) y
    /// devuelve la frase que toca o null. Tres avisos por maniobra, cada uno una sola vez:
    ///  - Siguiente: al pasar una maniobra (o al empezar la ruta), «En 800 metros, gire a la derecha por Calle Ancha.»;
    ///  - Intermedio (solo en ModoVoz.Todo): al quedar 300 m en coche o 50 m a pie, si la siguiente se dijo a más de 1,5
    ///    veces esa distancia;
    ///  - Momento: a 40 m en coche o 15 m a pie, el texto corto («Gire a la derecha.»). Si la maniobra ya está a esa
    ///    distancia al pasar la anterior, solo este.
    /// La salida y la llegada no se dicen (la llegada la dice ServicioVoz, historia 2.9); sin textos (OSRM, ruta aproximada) no hay
    /// maniobra siguiente y calla. Tras un recálculo, Reiniciar: se empieza de cero con las maniobras nuevas. Pura.
    /// </summary>
    public sealed class ReglaVoz
    {
        readonly ModoViaje viaje;
        bool hayObjetivo;          // ya se ha visto algún estado (la maniobra de ahora es la de "objetivo")
        Maniobra objetivo;         // la maniobra que toca decir; null si no hay
        double? siguienteDichaA;   // metros a los que se dijo su aviso "siguiente"; null si no se ha dicho
        bool intermedioDicho, momentoDicho;

        public ReglaVoz(ModoViaje viaje) => this.viaje = viaje;

        /// <summary>Metros del aviso del momento (40 en coche, 15 a pie).</summary>
        public double UmbralMomento => viaje == ModoViaje.APie ? ParametrosNegocio.VozMomentoPieMetros : ParametrosNegocio.VozMomentoCocheMetros;

        /// <summary>Metros del aviso intermedio (300 en coche, 50 a pie).</summary>
        public double UmbralIntermedio => viaje == ModoViaje.APie ? ParametrosNegocio.VozIntermedioPieMetros : ParametrosNegocio.VozIntermedioCocheMetros;

        /// <summary>Ruta nueva (recálculo): se olvida lo dicho y el próximo estado dice la siguiente de la ruta nueva.</summary>
        public void Reiniciar()
        {
            hayObjetivo = false;
            objetivo = null;
            siguienteDichaA = null;
            intermedioDicho = false;
            momentoDicho = false;
        }

        /// <summary>
        /// La frase que toca decir con este estado y este modo, o null. Con Nada no se apunta nada: al volver a hablar, la
        /// maniobra de ese momento cuenta como nueva y se dice con su distancia. El intermedio callado por Importante no
        /// cuenta como dicho: si se pasa a Todo a medio camino, se dice si aún toca.
        /// </summary>
        public string Avisar(EstadoNavegacion estado, ModoVoz modo)
        {
            // Callada no se apunta nada: al volver a hablar, la maniobra de entonces es nueva y se dice con su distancia
            if (modo == ModoVoz.Nada) { hayObjetivo = false; return null; }
            Objetivo(estado, out var maniobra, out double metros);
            bool nueva = !hayObjetivo || !ReferenceEquals(maniobra, objetivo);
            if (nueva)
            {
                hayObjetivo = true;
                objetivo = maniobra;
                siguienteDichaA = null;
                intermedioDicho = false;
                momentoDicho = false;
            }
            if (maniobra == null || !SeDice(maniobra)) return null;

            if (!momentoDicho && metros <= UmbralMomento)
            {
                momentoDicho = true;
                intermedioDicho = true;
                return maniobra.TextoCorto;
            }
            if (momentoDicho) return null;
            if (nueva)
            {
                siguienteDichaA = metros;
                return ConDistancia(maniobra, metros);
            }
            if (modo == ModoVoz.Todo && !intermedioDicho && metros <= UmbralIntermedio
                && (!siguienteDichaA.HasValue || siguienteDichaA.Value > ParametrosNegocio.VozIntermedioFactor * UmbralIntermedio))
            {
                intermedioDicho = true;
                return ConDistancia(maniobra, metros);
            }
            return null;
        }

        /// <summary>
        /// Apunta este estado sin decir nada (historia 2.8, parte 2): la maniobra de ahora cuenta como avisada a su distancia
        /// de ahora y lo que ya ha pasado (el intermedio o el del momento) como dicho. Es lo que haría Avisar con todas las
        /// indicaciones, sin frase. Para las posiciones viejas que llegan de golpe al volver de una pausa de Unity.
        /// </summary>
        public void Sincronizar(EstadoNavegacion estado) => _ = Avisar(estado, ModoVoz.Todo);

        /// <summary>
        /// La maniobra que toca: la siguiente del estado y, si es la salida (al empezar, aún sin pasar), la de después. La
        /// salida que queda atrás no cambia la maniobra que toca: no se repite el aviso.
        /// </summary>
        static void Objetivo(EstadoNavegacion estado, out Maniobra maniobra, out double metros)
        {
            maniobra = estado?.Siguiente;
            metros = estado?.MetrosASiguiente ?? 0;
            if (maniobra != null && maniobra.Tipo == TipoManiobra.Salida)
            {
                maniobra = estado.Despues;
                metros = estado.MetrosADespues;
            }
        }

        static bool SeDice(Maniobra m) =>
            m.Tipo != TipoManiobra.Salida && m.Tipo != TipoManiobra.Llegada && !string.IsNullOrWhiteSpace(m.Texto);

        /// <summary>«En 800 metros, » + el texto de la maniobra con la primera letra en minúscula.</summary>
        string ConDistancia(Maniobra m, double metros) => "En " + Distancia(metros, viaje) + ", " + Minuscula(m.Texto.Trim());

        /// <summary>
        /// La distancia dicha: redondeada a 50 m en coche y a 10 m a pie («800 metros», nunca menos de un paso); de 1 km
        /// en adelante, en kilómetros con un decimal («1 kilómetro», «1,5 kilómetros», «2 kilómetros»).
        /// </summary>
        public static string Distancia(double metros, ModoViaje viaje)
        {
            double paso = viaje == ModoViaje.APie ? ParametrosNegocio.VozRedondeoPieMetros : ParametrosNegocio.VozRedondeoCocheMetros;
            if (double.IsNaN(metros) || metros < 0) metros = 0;
            double redondeado = Math.Max(paso, Math.Round(metros / paso, MidpointRounding.AwayFromZero) * paso);
            if (redondeado < 1000) return ((int)redondeado) + " metros";
            // A la décima de kilómetro desde los metros de verdad (no desde los ya redondeados a 50: 1530 m son 1,5 km)
            long decimas = Math.Max(10, (long)Math.Round(metros / 100.0, MidpointRounding.AwayFromZero));
            long enteros = decimas / 10, decima = decimas % 10;
            if (decima == 0) return enteros == 1 ? "1 kilómetro" : enteros + " kilómetros";
            return enteros + "," + decima + " kilómetros";
        }

        static string Minuscula(string texto) =>
            string.IsNullOrEmpty(texto) ? texto : char.ToLowerInvariant(texto[0]) + texto.Substring(1);

        /// <summary>Clave guardada en IPreferencias ("todo", "importante" o "nada").</summary>
        public static string AClave(ModoVoz modo) =>
            modo == ModoVoz.Importante ? "importante" : modo == ModoVoz.Nada ? "nada" : "todo";

        /// <summary>El modo de una clave guardada; sin ella o desconocida, Todo.</summary>
        public static ModoVoz DeClave(string clave) =>
            clave == "importante" ? ModoVoz.Importante : clave == "nada" ? ModoVoz.Nada : ModoVoz.Todo;
    }
}
