using System;
using System.Collections.Generic;
using PriceMap.Dominio;
using UnityEngine.UIElements;

namespace PriceMap.UI
{
    /// <summary>
    /// Fila de cristal con los tres modos de la voz (historia 2.8; maqueta del 22/11/2026): a la derecha del botón de voz,
    /// tres iconos de 48 dp sin texto (volumen: todas las indicaciones; volumen con «!»: solo las importantes; volumen
    /// tachado: sin voz). El elegido lleva el fondo de acento. Oculta hasta que se abre; la pantalla la cierra al elegir, al
    /// tocar el mapa o con atrás.
    /// </summary>
    public class FilaVoz : VisualElement
    {
        public const string ClaseAbierta = "fila-voz--abierta";
        public const string ClaseElegida = "fila-voz__opcion--elegida";

        /// <summary>Los modos en el orden de la fila.</summary>
        public static readonly ModoVoz[] Modos = { ModoVoz.Todo, ModoVoz.Importante, ModoVoz.Nada };

        readonly Dictionary<ModoVoz, BotonRedondo> opciones = new Dictionary<ModoVoz, BotonRedondo>();

        public bool Abierta { get; private set; }

        /// <summary>El modo marcado (fondo de acento).</summary>
        public ModoVoz Elegido { get; private set; }

        /// <param name="alElegir">Lo que pasa al tocar un modo (elegirlo y cerrar la fila).</param>
        public FilaVoz(Action<ModoVoz> alElegir)
        {
            name = "fila-voz";
            AddToClassList("fila-voz");
            pickingMode = PickingMode.Position; // tocar entre iconos no mueve el mapa
            SombraSuave.Poner(this, conBorde: true);
            for (int i = 0; i < Modos.Length; i++)
            {
                var modo = Modos[i];
                var b = new BotonRedondo(IconoDe(modo), () => alElegir?.Invoke(modo), enColumna: true) { name = "fila-voz-" + ReglaVoz.AClave(modo) };
                b.AddToClassList("fila-voz__opcion");
                if (i > 0) b.AddToClassList("fila-voz__opcion--siguiente");
                opciones[modo] = b;
                Add(b);
            }
            Abierta = true;
            Cerrar();
        }

        /// <summary>El botón de ese modo.</summary>
        public BotonRedondo Opcion(ModoVoz modo) => opciones[modo];

        /// <summary>Marca el modo elegido (fondo de acento e icono en su tinta).</summary>
        public void Marcar(ModoVoz modo)
        {
            Elegido = modo;
            foreach (var par in opciones)
            {
                par.Value.EnableInClassList(ClaseElegida, par.Key == modo);
                par.Value.MarkDirtyRepaint();
            }
        }

        public void Abrir()
        {
            if (Abierta) return;
            Abierta = true;
            AddToClassList(ClaseAbierta);
        }

        public void Cerrar()
        {
            if (!Abierta) return;
            Abierta = false;
            RemoveFromClassList(ClaseAbierta);
        }

        /// <summary>Vuelve a dibujar los iconos (cambio de tema).</summary>
        public void Repintar()
        {
            foreach (var b in opciones.Values) b.MarkDirtyRepaint();
        }

        /// <summary>El icono de cada modo (también el del botón de voz).</summary>
        public static Icono IconoDe(ModoVoz modo) =>
            modo == ModoVoz.Importante ? Icono.VozImportante : modo == ModoVoz.Nada ? Icono.VozNada : Icono.Voz;

        /// <summary>Clave del nombre del modo para TalkBack («Todas las indicaciones», «Solo las importantes», «Sin voz»).</summary>
        public static string ClaveNombre(ModoVoz modo) =>
            modo == ModoVoz.Importante ? ClavesTexto.VozImportante : modo == ModoVoz.Nada ? ClavesTexto.VozNada : ClavesTexto.VozTodo;

        /// <summary>Clave del valor del botón de voz en TalkBack («todas», «solo las importantes», «sin voz»).</summary>
        public static string ClaveValor(ModoVoz modo) =>
            modo == ModoVoz.Importante ? ClavesTexto.VozValorImportante : modo == ModoVoz.Nada ? ClavesTexto.VozValorNada : ClavesTexto.VozValorTodo;
    }
}
