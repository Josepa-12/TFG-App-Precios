using UnityEngine;

namespace PriceMap.UI
{
    /// <summary>A dónde va la hoja del buscador al soltarla.</summary>
    public enum DestinoHoja { Plegada, Abierta, Quedar }

    /// <summary>
    /// Física de la hoja del buscador (historia 2.2, UX-DR5; prototipo/app.js · SHEET y setupDockDrag), lógica pura
    /// (solo usa Vector2 y Mathf, sin escena ni UI):
    /// la hoja sigue al dedo; un lanzamiento de más de 0,6 dp/ms la abre o la pliega del todo según el sentido; si se
    /// suelta por debajo del 15 % de la altura útil se pliega, por encima del 88 % se abre del todo y, entre los dos,
    /// se queda exactamente donde se suelta (sin imán, decisión del 19/11/2026). Un movimiento de menos de 6 dp es un
    /// toque. Desde la lista, el dedo mueve la hoja si no está abierta del todo o, abierta, si la lista está arriba
    /// del todo y se arrastra hacia abajo.
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
        /// <summary>Por debajo de esta fracción, soltar pliega (CLOSE_BELOW).</summary>
        public const float PlegarDebajo = 0.15f;
        /// <summary>Por encima de esta fracción, soltar abre del todo (OPEN_ABOVE).</summary>
        public const float AbrirEncima = 0.88f;
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

        /// <summary>Suelta el dedo: decide el destino (con la velocidad, 0 si el dedo se paró) y deja ahí la hoja.</summary>
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
                    if (destino == DestinoHoja.Plegada) Fraccion = 0f;
                    else if (destino == DestinoHoja.Abierta) Fraccion = 1f;
                    return new Suelta(false, true, destino, Fraccion);
                default:
                    return new Suelta(false, false, DestinoHoja.Quedar, Fraccion);
            }
        }

        /// <summary>Se pierde el dedo (cancelado por el sistema): la hoja se queda donde está.</summary>
        public void Cancelar() => estado = Estado.Libre;

        /// <summary>
        /// Destino al soltar con la fracción k y la velocidad v (dp/ms, positiva hacia abajo): lanzamiento hacia
        /// arriba abre, hacia abajo pliega; si no, por debajo del 15 % pliega, por encima del 88 % abre y entre medias
        /// se queda.
        /// </summary>
        public static DestinoHoja Destino(float k, float v)
        {
            if (v < -Lanzamiento) return DestinoHoja.Abierta;
            if (v > Lanzamiento) return DestinoHoja.Plegada;
            if (k < PlegarDebajo) return DestinoHoja.Plegada;
            if (k > AbrirEncima) return DestinoHoja.Abierta;
            return DestinoHoja.Quedar;
        }

        /// <summary>
        /// Si arrastrar la lista mueve la hoja (prototipo decide): siempre si la hoja no está abierta del todo; si lo
        /// está, solo con la lista arriba del todo y arrastrando hacia abajo (dy &gt; 0).
        /// </summary>
        public static bool ListaMueveHoja(bool abiertaDelTodo, float desplazamiento, float dy) =>
            !abiertaDelTodo || (desplazamiento <= 0f && dy > 0f);
    }
}
