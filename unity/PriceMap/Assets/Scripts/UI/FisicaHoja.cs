using UnityEngine;

namespace PriceMap.UI
{
    /// <summary>A qué parada va la hoja del buscador al soltarla: cerrada (cápsula), mitad o completa.</summary>
    public enum DestinoHoja { Plegada, Mitad, Abierta }

    /// <summary>
    /// Física de la hoja del buscador (historia 2.2, UX-DR5; basada en prototipo/app.js · SHEET y setupDockDrag, pero
    /// con otra regla de suelta desde el 22/11/2026), lógica pura (solo usa Vector2 y Mathf, sin escena ni UI):
    /// la hoja sigue al dedo y, al soltarla, va siempre a una de tres paradas (sustituye a «se queda donde se suelta»
    /// del 19/11 y a los umbrales 15 %/88 % del prototipo): cerrada (0), mitad (0,5) o completa (1). Sin lanzamiento
    /// va a la más cercana (cortes en 0,25 y 0,75); un lanzamiento de más de 0,6 dp/ms lleva a la primera parada más
    /// allá de donde está la hoja en ese sentido, como Apple Maps (en el extremo se queda). Un movimiento de
    /// menos de 6 dp es un toque. Desde la lista, el dedo mueve la hoja si no está abierta del todo o, abierta, si la
    /// lista está arriba del todo y se arrastra hacia abajo.
    ///
    /// La fracción abierta va de 0 (plegada, cápsula) a 1 (abierta del todo); la altura útil es lo que recorre la hoja
    /// entre las dos. Las posiciones son dp del panel con y hacia abajo; los tiempos, milisegundos.
    /// </summary>
    public sealed class FisicaHoja
    {
        /// <summary>Hueco entre la hoja abierta del todo y la zona segura de arriba (OPEN_TOP).</summary>
        public const float HuecoArriba = 64f;
        /// <summary>Alto de la cápsula plegada con su margen inferior: 56 + 20 (CLOSED_H).</summary>
        public const float AltoPlegado = 76f;
        /// <summary>Velocidad (dp/ms) a partir de la cual soltar es un lanzamiento (FLICK).</summary>
        public const float Lanzamiento = 0.6f;
        /// <summary>
        /// Solo para la forma: hasta esta fracción la cápsula se mezcla con la hoja (HojaBuscador.Mezcla). No decide
        /// paradas.
        /// </summary>
        public const float MezclaHasta = 0.15f;
        /// <summary>Fracción de la parada de la mitad.</summary>
        public const float FraccionMitad = 0.5f;
        /// <summary>Sin lanzamiento, por debajo de esta fracción se va a cerrada; desde ella, a la mitad.</summary>
        public const float CorteCerrada = 0.25f;
        /// <summary>Sin lanzamiento, por encima de esta fracción se va a completa; hasta ella, a la mitad.</summary>
        public const float CorteCompleta = 0.75f;
        /// <summary>Menos de este movimiento (dp) es un toque; desde él, un arrastre.</summary>
        public const float UmbralToque = 6f;
        /// <summary>La velocidad se vuelve a medir solo si han pasado más de 4 ms desde la última muestra.</summary>
        public const double MuestreoMs = 4.0;
        /// <summary>Si el dedo lleva parado más de 90 ms al soltar, no es un lanzamiento.</summary>
        public const double ParadaMs = 90.0;

        enum Estado { Libre, Pendiente, Arrastre, Ignorado }

        Estado estado = Estado.Libre;
        Vector2 inicio;           // donde bajó el dedo
        bool enLista;             // el gesto empezó en la lista de la hoja
        float desplazamientoLista;// desplazamiento de la lista al bajar el dedo (0 = arriba del todo)
        float fraccionInicio;     // fracción al empezar el arrastre
        float yMuestra;           // última muestra de la velocidad
        double msMuestra;

        /// <summary>Recorrido (dp) de la hoja entre plegada y abierta del todo; nunca menos de 1.</summary>
        public float AltoUtil { get; private set; } = 1f;
        /// <summary>Fracción abierta: 0 plegada, 1 abierta del todo.</summary>
        public float Fraccion { get; private set; }
        /// <summary>Última velocidad medida (dp/ms, positiva hacia abajo).</summary>
        public float Velocidad { get; private set; }

        /// <summary>Hay un dedo abajo que aún no se ha soltado.</summary>
        public bool Pulsado => estado != Estado.Libre;
        /// <summary>El gesto es un arrastre: la hoja sigue al dedo.</summary>
        public bool Arrastrando => estado == Estado.Arrastre;
        /// <summary>El gesto empezó en la lista y la lista se desplaza: la hoja no se mueve.</summary>
        public bool Ignorado => estado == Estado.Ignorado;
        public bool AbiertaDelTodo => Fraccion >= 1f;
        public bool Plegada => Fraccion <= 0f;

        /// <summary>Altura útil entre la hoja plegada y la abierta del todo (no cambia la fracción).</summary>
        public void PonerAltoUtil(float dp) => AltoUtil = Mathf.Max(1f, float.IsNaN(dp) ? 1f : dp);

        /// <summary>
        /// Altura útil en un panel de ese alto con esas zonas seguras: de la cápsula plegada (con su margen inferior)
        /// a 64 dp por debajo de la zona segura de arriba.
        /// </summary>
        public static float AltoUtilDe(float altoPanel, float seguraArriba, float seguraAbajo, float altoPlegado = AltoPlegado) =>
            Mathf.Max(1f, altoPanel - seguraAbajo - altoPlegado - (seguraArriba + HuecoArriba));

        /// <summary>Pone la fracción abierta sin gesto (abrir o plegar desde el código).</summary>
        public void Poner(float fraccion) => Fraccion = Mathf.Clamp01(fraccion);

        /// <summary>Baja el dedo en ese punto.</summary>
        /// <param name="enLista">El dedo bajó en la lista de la hoja (no en la cápsula ni en el tirador).</param>
        /// <param name="desplazamientoLista">Desplazamiento vertical de la lista (0 arriba del todo).</param>
        public void Bajar(Vector2 punto, double ms, bool enLista = false, float desplazamientoLista = 0f)
        {
            estado = Estado.Pendiente;
            inicio = punto;
            this.enLista = enLista;
            this.desplazamientoLista = desplazamientoLista;
            Velocidad = 0f;
            yMuestra = punto.y;
            msMuestra = ms;
        }

        /// <summary>
        /// Mueve el dedo. Con 6 dp o más desde donde bajó el gesto pasa a arrastre (o, desde la lista, a ignorado si
        /// la lista debe desplazarse) y desde entonces la hoja sigue al dedo. Devuelve true si el gesto es un arrastre.
        /// </summary>
        public bool Mover(Vector2 punto, double ms)
        {
            if (estado == Estado.Pendiente)
            {
                if ((punto - inicio).magnitude < UmbralToque) return false;
                float dy = punto.y - inicio.y;
                // Un gesto más horizontal que vertical (mover el cursor del campo, la lista de lado) no es de la hoja
                if (Mathf.Abs(punto.x - inicio.x) > Mathf.Abs(dy) ||
                    (enLista && !ListaMueveHoja(AbiertaDelTodo, desplazamientoLista, dy)))
                {
                    estado = Estado.Ignorado;
                    return false;
                }
                // Empieza el arrastre (prototipo begin): la hoja sale de donde estaba. La primera muestra de velocidad
                // abarca desde que bajó el dedo (recorrido y tiempo del mismo tramo, puestos en Bajar)
                estado = Estado.Arrastre;
                fraccionInicio = Fraccion;
            }
            if (estado != Estado.Arrastre) return false;

            // Sigue al dedo: bajar el dedo pliega, subirlo abre
            Fraccion = Mathf.Clamp01(fraccionInicio - (punto.y - inicio.y) / AltoUtil);
            if (ms - msMuestra > MuestreoMs)
            {
                Velocidad = (float)((punto.y - yMuestra) / (ms - msMuestra));
                yMuestra = punto.y;
                msMuestra = ms;
            }
            return true;
        }

        /// <summary>Resultado de soltar el dedo.</summary>
        public readonly struct Suelta
        {
            /// <summary>Se movió menos de 6 dp fuera de la lista: abre del todo, enfoca el campo y saca el teclado.</summary>
            public readonly bool EsToque;
            /// <summary>Fue un arrastre de la hoja.</summary>
            public readonly bool FueArrastre;
            public readonly DestinoHoja Destino;
            /// <summary>Fracción en la que queda la hoja.</summary>
            public readonly float Fraccion;

            public Suelta(bool esToque, bool fueArrastre, DestinoHoja destino, float fraccion)
            {
                EsToque = esToque;
                FueArrastre = fueArrastre;
                Destino = destino;
                Fraccion = fraccion;
            }
        }

        /// <summary>
        /// Suelta el dedo: tras un arrastre decide la parada (con la velocidad, 0 si el dedo se paró) y pone ahí la
        /// hoja. Si no hubo arrastre ni toque (gesto ignorado, toque en la lista) la hoja no se mueve y el destino es
        /// la parada más cercana a donde está.
        /// </summary>
        public Suelta Soltar(double ms)
        {
            var antes = estado;
            estado = Estado.Libre;
            switch (antes)
            {
                case Estado.Pendiente when !enLista:
                    Fraccion = 1f;
                    return new Suelta(true, false, DestinoHoja.Abierta, 1f);
                case Estado.Arrastre:
                    float v = ms - msMuestra > ParadaMs ? 0f : Velocidad;
                    Velocidad = v;
                    var destino = Destino(Fraccion, v);
                    Fraccion = FraccionDe(destino);
                    return new Suelta(false, true, destino, Fraccion);
                default:
                    return new Suelta(false, false, Destino(Fraccion, 0f), Fraccion);
            }
        }

        /// <summary>
        /// Termina el gesto sin soltar (la hoja se oculta, otro gesto empieza encima o se pliega desde el código): si
        /// la hoja se arrastraba, va a la parada más cercana (sin lanzamiento), para no quedarse a medias. El
        /// PointerCancel del sistema no pasa por aquí durante un arrastre: HojaBuscador.AlCancelar llama a Soltar.
        /// </summary>
        public void Cancelar()
        {
            if (estado == Estado.Arrastre) Fraccion = FraccionDe(Destino(Fraccion, 0f));
            estado = Estado.Libre;
        }

        /// <summary>Fracción de cada parada: cerrada 0, mitad 0,5, completa 1.</summary>
        public static float FraccionDe(DestinoHoja parada) => parada switch
        {
            DestinoHoja.Plegada => 0f,
            DestinoHoja.Mitad => FraccionMitad,
            _ => 1f,
        };

        /// <summary>
        /// Parada al soltar con la fracción k y la velocidad v (dp/ms, positiva hacia abajo). Sin lanzamiento
        /// (|v| ≤ 0,6), la más cercana a k (cortes en 0,25 y 0,75; en el corte, la mitad). Con lanzamiento hacia
        /// arriba, la primera parada estrictamente por encima de k (mitad o completa); hacia abajo, la primera
        /// estrictamente por debajo (mitad o cerrada). En el extremo, se queda en él.
        /// </summary>
        public static DestinoHoja Destino(float k, float v)
        {
            if (v < -Lanzamiento) return k < FraccionMitad ? DestinoHoja.Mitad : DestinoHoja.Abierta;
            if (v > Lanzamiento) return k > FraccionMitad ? DestinoHoja.Mitad : DestinoHoja.Plegada;
            if (k < CorteCerrada) return DestinoHoja.Plegada;
            if (k > CorteCompleta) return DestinoHoja.Abierta;
            return DestinoHoja.Mitad;
        }

        /// <summary>
        /// Si arrastrar la lista mueve la hoja (prototipo decide): siempre si la hoja no está abierta del todo; si lo
        /// está, solo con la lista arriba del todo y arrastrando hacia abajo (dy &gt; 0).
        /// </summary>
        public static bool ListaMueveHoja(bool abiertaDelTodo, float desplazamiento, float dy) =>
            !abiertaDelTodo || (desplazamiento <= 0f && dy > 0f);
    }
}
