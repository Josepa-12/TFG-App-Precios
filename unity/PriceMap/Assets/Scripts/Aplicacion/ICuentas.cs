using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de las cuentas (historia 3.3). Todo responde en el hilo principal y los fallos llegan con un código de
    /// ErroresCuenta, nunca como excepción.
    /// </summary>
    public interface ICuentas
    {
        /// <summary>Quién ha entrado; null sin sesión. La sesión sigue al volver a abrir la app.</summary>
        UsuarioCuenta Usuario { get; }

        /// <summary>Ha cambiado Usuario.</summary>
        event Action Cambiado;

        /// <summary>Pide al usuario su cuenta de Google y entra con ella. Al acabar bien, Usuario ya es el nuevo.</summary>
        /// <param name="ct">Cancelada, acaba con Cancelado y sin sesión.</param>
        Task<Resultado<UsuarioCuenta>> EntrarConGoogle(CancellationToken ct);

        /// <summary>Crea una cuenta con correo y contraseña y entra con ella. La contraseña la guarda el servidor de cuentas.</summary>
        /// <param name="ct">Cancelada, acaba con Cancelado y sin sesión (la cuenta puede haberse creado ya).</param>
        Task<Resultado<UsuarioCuenta>> CrearConCorreo(string correo, string contrasena, CancellationToken ct);

        /// <summary>Entra con una cuenta de correo y contraseña que ya existe.</summary>
        Task<Resultado<UsuarioCuenta>> EntrarConCorreo(string correo, string contrasena, CancellationToken ct);

        /// <summary>Pide el correo para poner otra contraseña; sale bien aunque no haya cuenta con ese correo.</summary>
        Task<Resultado<bool>> EnviarContrasena(string correo, CancellationToken ct);
    }

    /// <summary>Quién ha entrado. Sus datos nunca van al registro.</summary>
    public sealed class UsuarioCuenta
    {
        public UsuarioCuenta(string uid, string nombre, string correo)
        {
            if (string.IsNullOrWhiteSpace(uid)) throw new ArgumentException("Un usuario necesita su identificador.", nameof(uid));
            Uid = uid;
            Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            Correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim();
        }

        public string Uid { get; }
        /// <summary>Null si la cuenta no lo tiene.</summary>
        public string Nombre { get; }
        /// <summary>Null si la cuenta no lo tiene.</summary>
        public string Correo { get; }

        /// <summary>Cómo se le saluda: la primera palabra del nombre o, sin nombre, el correo (vacío sin ninguno).</summary>
        public string Saludo => Nombre?.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[0] ?? Correo ?? "";

        /// <summary>Con qué ha entrado: el correo o, sin él, el nombre (vacío sin ninguno).</summary>
        public string Identidad => Correo ?? Nombre ?? "";

        /// <summary>Letra del avatar, en mayúscula: la del nombre o la del correo; null sin ninguno.</summary>
        public string Inicial
        {
            get
            {
                string texto = Nombre ?? Correo;
                return texto == null ? null : StringInfo.GetNextTextElement(texto).ToUpperInvariant();
            }
        }
    }

    /// <summary>Códigos de error al entrar (Resultado.Error); la UI los traduce a texto.</summary>
    public static class ErroresCuenta
    {
        /// <summary>El usuario ha cerrado el selector de Google o la entrada: no es un error que enseñar.</summary>
        public const string Cancelado = "cuenta-cancelado";
        public const string SinConexion = "cuenta-sin-conexion";
        /// <summary>No hay servidor o el móvil no puede entrar con Google.</summary>
        public const string NoDisponible = "cuenta-no-disponible";
        public const string Desconocido = "cuenta-desconocido";

        // Solo con correo
        /// <summary>Al crear: ya hay una cuenta con ese correo (puede ser de Google).</summary>
        public const string CorreoUsado = "cuenta-correo-usado";
        public const string CorreoNoValido = "cuenta-correo-no-valido";
        /// <summary>Al crear: demasiado corta.</summary>
        public const string ContrasenaDebil = "cuenta-contrasena-debil";
        /// <summary>Al entrar: sin contraseña escrita.</summary>
        public const string ContrasenaVacia = "cuenta-contrasena-vacia";
        /// <summary>El correo o la contraseña (no se dice cuál).</summary>
        public const string NoCoinciden = "cuenta-no-coinciden";
        public const string DemasiadosIntentos = "cuenta-demasiados-intentos";
    }
}
