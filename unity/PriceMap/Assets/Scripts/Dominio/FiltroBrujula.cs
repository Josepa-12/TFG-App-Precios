using System;

namespace PriceMap.Dominio
{
    /// <summary>
    /// Calma la brújula (petición del 24/11/2026: el cono se movía demasiado). Primero quita el temblor del sensor con
    /// un suavizado corto; el rumbo que sale se queda quieto hasta que el suavizado se aparta BrujulaGiroMinimoGrados, y
    /// entonces gira hacia él poco a poco (unos 0,5 s para un giro pequeño) hasta quedar a BrujulaLlegadaGrados. Los
    /// giros van siempre por el lado corto. No es seguro entre hilos.
    /// </summary>
    public sealed class FiltroBrujula
    {
        double? suave;    // lectura sin el temblor
        double mostrado;  // lo que sale
        bool girando;

        /// <summary>Olvida lo anterior: la próxima lectura sale tal cual.</summary>
        public void Reiniciar()
        {
            suave = null;
            girando = false;
        }

        /// <summary>Lectura nueva y segundos desde la anterior; devuelve el rumbo que se muestra, en [0, 360).</summary>
        public double Filtrar(double lectura, double segundos)
        {
            lectura = Normalizar(lectura);
            if (!suave.HasValue)
            {
                suave = mostrado = lectura;
                return mostrado;
            }
            suave = Acercar(suave.Value, lectura, segundos, ParametrosNegocio.BrujulaTemblorSegundos);
            if (!girando && Math.Abs(Diferencia(mostrado, suave.Value)) >= ParametrosNegocio.BrujulaGiroMinimoGrados)
                girando = true;
            if (!girando) return mostrado;
            mostrado = Acercar(mostrado, suave.Value, segundos, ParametrosNegocio.BrujulaGiroSegundos);
            if (Math.Abs(Diferencia(mostrado, suave.Value)) < ParametrosNegocio.BrujulaLlegadaGrados) girando = false;
            return mostrado;
        }

        // Acercamiento exponencial: con menos fotogramas por segundo, pasos más grandes y el mismo tiempo
        static double Acercar(double desde, double hasta, double segundos, double constante) =>
            Normalizar(desde + Diferencia(desde, hasta) * (1.0 - Math.Exp(-Math.Max(0.0, segundos) / constante)));

        // Grados de desde a hasta por el lado corto, en [-180, 180)
        static double Diferencia(double desde, double hasta) => Normalizar(hasta - desde + 180.0) - 180.0;

        static double Normalizar(double grados) => (grados % 360.0 + 360.0) % 360.0;
    }
}
