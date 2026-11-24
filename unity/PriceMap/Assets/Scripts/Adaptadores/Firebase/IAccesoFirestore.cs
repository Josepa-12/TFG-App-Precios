using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>Lo que AlmacenFirestore necesita de Firestore, sin sus tipos; las pruebas usan uno falso. Falla con FalloFirestore.</summary>
    internal interface IAccesoFirestore
    {
        Task<Documento> Leer(string ruta);
        Task Escribir(string ruta, IReadOnlyDictionary<string, object> campos);
        /// <summary>Los avisos pueden llegar en otro hilo.</summary>
        IDisposable Escuchar(string ruta, Action<Documento> alCambiar, Action<Exception> alFallar);
    }

    /// <summary>Un error de Firestore con su código (los de gRPC).</summary>
    internal sealed class FalloFirestore : Exception
    {
        public FalloFirestore(int codigo, Exception causa = null) : base("Firestore " + codigo, causa) => Codigo = codigo;
        public int Codigo { get; }
    }

    /// <summary>De los errores de Firestore a los códigos propios (ErroresServidor).</summary>
    internal static class CodigosFirestore
    {
        public static string De(Exception e) => e switch
        {
            FalloFirestore f => De(f.Codigo),
            ArgumentException _ => ErroresServidor.NoValido,
            _ => ErroresServidor.Desconocido,
        };

        public static string De(int codigo) => codigo switch
        {
            3 => ErroresServidor.NoValido,        // InvalidArgument
            4 => ErroresServidor.SinConexion,     // DeadlineExceeded
            5 => ErroresServidor.NoEncontrado,    // NotFound
            7 => ErroresServidor.SinPermiso,      // PermissionDenied
            8 => ErroresServidor.CuotaAgotada,    // ResourceExhausted
            14 => ErroresServidor.SinConexion,    // Unavailable
            16 => ErroresServidor.SinPermiso,     // Unauthenticated
            _ => ErroresServidor.Desconocido,
        };
    }
}
