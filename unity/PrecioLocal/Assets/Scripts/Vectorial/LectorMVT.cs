using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PrecioLocal.Vectorial
{
    public enum TipoGeometria { Desconocido = 0, Punto = 1, Linea = 2, Poligono = 3 }

    /// <summary>Una capa de la tesela (p. ej. "transportation", "building", "water").</summary>
    public class CapaMVT
    {
        public string nombre;
        public int extension = 4096;   // coordenadas de la tesela: 0..extension
        public readonly List<ElementoMVT> elementos = new List<ElementoMVT>();
    }

    /// <summary>Un elemento (calle, edificio, lago...): tipo, atributos y geometría en coordenadas de tesela.</summary>
    public class ElementoMVT
    {
        public TipoGeometria tipo;
        public readonly Dictionary<string, object> atributos = new Dictionary<string, object>();
        /// <summary>Partes: líneas o anillos de polígono (x a la derecha, y hacia abajo).</summary>
        public readonly List<List<Vector2>> partes = new List<List<Vector2>>();

        public string Texto(string clave) => atributos.TryGetValue(clave, out var v) ? v as string : null;
        public float Numero(string clave, float porDefecto) =>
            atributos.TryGetValue(clave, out var v) && v is double d ? (float)d : porDefecto;

        /// <summary>Agrupa los anillos en polígonos: un anillo exterior (área positiva) seguido de sus huecos.</summary>
        public List<(List<Vector2> exterior, List<List<Vector2>> huecos)> Poligonos()
        {
            var r = new List<(List<Vector2>, List<List<Vector2>>)>();
            foreach (var anillo in partes)
            {
                if (anillo.Count < 3) continue;
                double a = Area(anillo);
                if (a > 0) r.Add((anillo, new List<List<Vector2>>()));
                else if (a < 0 && r.Count > 0) r[r.Count - 1].Item2.Add(anillo);
            }
            return r;
        }

        // Fórmula del agrimensor en coordenadas de tesela: positiva = anillo exterior (especificación MVT 2.1)
        static double Area(List<Vector2> p)
        {
            double s = 0;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += (double)p[j].x * p[i].y - (double)p[i].x * p[j].y;
            return s / 2;
        }
    }

    /// <summary>
    /// Lector mínimo de teselas vectoriales Mapbox Vector Tile (MVT 2.1), el formato de OpenFreeMap / OpenMapTiles.
    /// Una tesela es un mensaje protobuf: capas → elementos → atributos y geometría codificada con comandos
    /// MoveTo / LineTo / ClosePath y coordenadas relativas en zigzag. No necesita librerías externas.
    /// </summary>
    public static class LectorMVT
    {
        public static Dictionary<string, CapaMVT> Leer(byte[] datos)
        {
            var capas = new Dictionary<string, CapaMVT>();
            int i = 0;
            while (i < datos.Length)
            {
                ulong clave = Varint(datos, ref i);
                int campo = (int)(clave >> 3), tipo = (int)(clave & 7);
                if (campo == 3 && tipo == 2)
                {
                    int len = (int)Varint(datos, ref i);
                    var capa = LeerCapa(datos, i, i + len);
                    if (capa.nombre != null) capas[capa.nombre] = capa;
                    i += len;
                }
                else Saltar(datos, ref i, tipo);
            }
            return capas;
        }

        static CapaMVT LeerCapa(byte[] b, int inicio, int fin)
        {
            var capa = new CapaMVT();
            var claves = new List<string>();
            var valores = new List<object>();
            var crudos = new List<(int, int)>();
            int i = inicio;
            while (i < fin)
            {
                ulong k = Varint(b, ref i);
                int f = (int)(k >> 3), t = (int)(k & 7);
                if (t == 2)
                {
                    int len = (int)Varint(b, ref i);
                    switch (f)
                    {
                        case 1: capa.nombre = Encoding.UTF8.GetString(b, i, len); break;
                        case 2: crudos.Add((i, i + len)); break;
                        case 3: claves.Add(Encoding.UTF8.GetString(b, i, len)); break;
                        case 4: valores.Add(LeerValor(b, i, i + len)); break;
                    }
                    i += len;
                }
                else if (f == 5 && t == 0) capa.extension = (int)Varint(b, ref i);
                else Saltar(b, ref i, t);
            }
            foreach (var (a, z) in crudos) capa.elementos.Add(LeerElemento(b, a, z, claves, valores));
            return capa;
        }

        static object LeerValor(byte[] b, int i, int fin)
        {
            object v = null;
            while (i < fin)
            {
                ulong k = Varint(b, ref i);
                int f = (int)(k >> 3), t = (int)(k & 7);
                switch (f)
                {
                    case 1: { int len = (int)Varint(b, ref i); v = Encoding.UTF8.GetString(b, i, len); i += len; break; }
                    case 2: v = (double)BitConverter.ToSingle(b, i); i += 4; break;
                    case 3: v = BitConverter.ToDouble(b, i); i += 8; break;
                    case 4: v = (double)(long)Varint(b, ref i); break;
                    case 5: v = (double)Varint(b, ref i); break;
                    case 6: { ulong u = Varint(b, ref i); v = (double)((long)(u >> 1) ^ -(long)(u & 1)); break; }
                    case 7: v = Varint(b, ref i) != 0; break;
                    default: Saltar(b, ref i, t); break;
                }
            }
            return v;
        }

        static ElementoMVT LeerElemento(byte[] b, int inicio, int fin, List<string> claves, List<object> valores)
        {
            var e = new ElementoMVT();
            List<uint> geometria = null;
            int i = inicio;
            while (i < fin)
            {
                ulong k = Varint(b, ref i);
                int f = (int)(k >> 3), t = (int)(k & 7);
                if (f == 2 && t == 2)
                {
                    int final = (int)Varint(b, ref i) + i;
                    while (i < final)
                    {
                        int ki = (int)Varint(b, ref i), vi = (int)Varint(b, ref i);
                        if (ki < claves.Count && vi < valores.Count) e.atributos[claves[ki]] = valores[vi];
                    }
                }
                else if (f == 3 && t == 0) e.tipo = (TipoGeometria)(int)Varint(b, ref i);
                else if (f == 4 && t == 2)
                {
                    int final = (int)Varint(b, ref i) + i;
                    geometria = new List<uint>();
                    while (i < final) geometria.Add((uint)Varint(b, ref i));
                }
                else Saltar(b, ref i, t);
            }
            if (geometria != null) Decodificar(geometria, e);
            return e;
        }

        static void Decodificar(List<uint> g, ElementoMVT e)
        {
            int x = 0, y = 0, i = 0;
            List<Vector2> actual = null;
            while (i < g.Count)
            {
                uint comando = g[i++];
                int id = (int)(comando & 7), n = (int)(comando >> 3);
                if (id == 1 || id == 2) // MoveTo / LineTo
                {
                    for (int j = 0; j < n && i + 1 < g.Count; j++)
                    {
                        x += ZigZag(g[i++]); y += ZigZag(g[i++]);
                        if (id == 1) { actual = new List<Vector2>(); e.partes.Add(actual); }
                        actual?.Add(new Vector2(x, y));
                    }
                }
                // id == 7 (ClosePath): el anillo se cierra solo, no añade puntos
            }
        }

        static int ZigZag(uint n) => (int)(n >> 1) ^ -(int)(n & 1);

        static ulong Varint(byte[] b, ref int i)
        {
            ulong r = 0; int s = 0;
            while (true)
            {
                byte c = b[i++];
                r |= (ulong)(c & 0x7f) << s;
                if (c < 0x80) return r;
                s += 7;
            }
        }

        static void Saltar(byte[] b, ref int i, int tipo)
        {
            switch (tipo)
            {
                case 0: Varint(b, ref i); break;
                case 1: i += 8; break;
                case 2: { int len = (int)Varint(b, ref i); i += len; break; }
                case 5: i += 4; break;
                default: throw new FormatException("Tipo de campo protobuf desconocido: " + tipo);
            }
        }
    }
}
