using System;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>Lo que CuentasFirebase necesita de Firebase Auth, sin sus tipos; las pruebas usan uno falso. Falla con FalloAuth.</summary>
    internal interface IAccesoAuth
    {
        UsuarioCuenta Usuario { get; }
        /// <summary>Los avisos pueden llegar en otro hilo.</summary>
        void Escuchar(Action alCambiar);
        Task<UsuarioCuenta> Entrar(string idToken);
        Task<UsuarioCuenta> CrearConCorreo(string correo, string contrasena);
        Task<UsuarioCuenta> EntrarConCorreo(string correo, string contrasena);
        /// <summary>El correo de Firebase para poner otra contraseña, en español.</summary>
        Task EnviarContrasena(string correo);
        void Salir();
    }

    /// <summary>Un error de Firebase Auth con su código (AuthError).</summary>
    internal sealed class FalloAuth : Exception
    {
        public FalloAuth(string codigo, Exception causa = null) : base("Auth " + codigo, causa) => Codigo = codigo;
        /// <summary>El nombre de AuthError («NetworkRequestFailed»).</summary>
        public string Codigo { get; }
    }

    /// <summary>De los errores de Firebase Auth a los códigos propios (ErroresCuenta).</summary>
    internal static class CodigosAuth
    {
        public static string De(Exception e)
        {
            switch ((e as FalloAuth)?.Codigo)
            {
                case "NetworkRequestFailed": return ErroresCuenta.SinConexion;
                case "Cancelled": return ErroresCuenta.Cancelado;
                default: return e is OperationCanceledException ? ErroresCuenta.Cancelado : ErroresCuenta.Desconocido;
            }
        }

        /// <summary>Con correo y contraseña; con Google esos mismos códigos no hablan de lo que se ha escrito.</summary>
        public static string DeCorreo(Exception e)
        {
            switch ((e as FalloAuth)?.Codigo)
            {
                case "WrongPassword":
                case UsuarioDesconocido:
                case "InvalidCredential": return ErroresCuenta.NoCoinciden;
                case "EmailAlreadyInUse": return ErroresCuenta.CorreoUsado;
                case "InvalidEmail":
                case "MissingEmail": return ErroresCuenta.CorreoNoValido;
                case "WeakPassword": return ErroresCuenta.ContrasenaDebil;
                case "TooManyRequests":
                case "QuotaExceeded": return ErroresCuenta.DemasiadosIntentos;
                default: return De(e);
            }
        }

        /// <summary>No hay cuenta con ese correo.</summary>
        public const string UsuarioDesconocido = "UserNotFound";
    }

    internal enum EstadoGoogle
    {
        Hecha,
        Cancelada,
        Fallo,
        /// <summary>Sin default_web_client_id o un móvil que no puede.</summary>
        NoDisponible,
    }

    /// <summary>Lo que ha contestado el selector de Google.</summary>
    internal readonly struct RespuestaGoogle
    {
        public RespuestaGoogle(EstadoGoogle estado, string idToken = null, string motivo = null)
        {
            Estado = estado;
            IdToken = idToken;
            Motivo = motivo;
        }

        public EstadoGoogle Estado { get; }
        /// <summary>Nunca va al registro.</summary>
        public string IdToken { get; }
        /// <summary>Solo el tipo del fallo, para el registro.</summary>
        public string Motivo { get; }
    }

    /// <summary>El selector de cuentas de Google del móvil.</summary>
    internal interface ISelectorGoogle
    {
        Task<RespuestaGoogle> Pedir(CancellationToken ct);
    }
}
