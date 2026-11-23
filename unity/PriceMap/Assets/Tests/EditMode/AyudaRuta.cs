using NUnit.Framework;
using PriceMap.UI;

namespace PriceMap.Tests.EditMode
{
    /// <summary>Ayuda común de las pruebas que navegan: de «Cómo llegar» (Ir pulsado y las rutas llegadas) a la navegación.</summary>
    static class AyudaRuta
    {
        /// <summary>«Iniciar» de la ruta con ese índice, y la cámara llega al usuario: con navegación, ha empezado.</summary>
        public static void Iniciar(this PantallaMapa pantalla, int indice = 0)
        {
            Assert.IsTrue(pantalla.EligiendoRuta, "«Cómo llegar» está abierta.");
            Assert.IsNotNull(pantalla.OpcionesRuta, "Han llegado las rutas.");
            pantalla.IniciarRuta(indice);
            Assert.IsFalse(pantalla.EligiendoRuta, "«Iniciar» cierra la hoja.");
            pantalla.LlegaLaCamara();
        }

        /// <summary>Pasa el tiempo en que la cámara llega (el reloj de la cámara se queda adelantado 1 s) y un fotograma.</summary>
        public static void LlegaLaCamara(this PantallaMapa pantalla)
        {
            if (!pantalla.AcercandoCamara) return;
            var antes = pantalla.RelojCamara;
            pantalla.RelojCamara = () => antes().AddSeconds(1);
            pantalla.ActualizarCapas(0f);
            Assert.IsFalse(pantalla.AcercandoCamara);
            Assert.IsTrue(pantalla.Navegando);
        }
    }
}
