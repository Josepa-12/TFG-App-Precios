using System;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Cuentas de mentira (PC, editor y captura de pruebas): nunca tocan Firebase ni guardan la sesión. Se entra siempre como
    /// Ana o con el correo escrito, salvo que se cambie Responder o ResponderCorreo; la contraseña no se mira ni se guarda.
    /// </summary>
    public sealed class CuentasFalsas : ICuentas
    {
        public static readonly UsuarioCuenta Ana = new UsuarioCuenta("falsa", "Ana García", "ana@example.com");

        /// <summary>Lo que contesta Google a cada petición.</summary>
        public Func<CancellationToken, Task<Resultado<UsuarioCuenta>>> Responder { get; set; } =
            _ => Task.FromResult(Resultado<UsuarioCuenta>.Exito(Ana));

        /// <summary>Lo que contesta al crear una cuenta y al entrar con correo (correo, contraseña).</summary>
        public Func<string, string, CancellationToken, Task<Resultado<UsuarioCuenta>>> ResponderCorreo { get; set; } =
            (correo, _, __) => Task.FromResult(Resultado<UsuarioCuenta>.Exito(new UsuarioCuenta("falsa", null, correo)));

        /// <summary>Lo que contesta al pedir el enlace de la contraseña.</summary>
        public Func<string, CancellationToken, Task<Resultado<bool>>> ResponderContrasena { get; set; } =
            (_, __) => Task.FromResult(Resultado<bool>.Exito(true));

        /// <summary>Peticiones hechas.</summary>
        public int Peticiones { get; private set; }

        /// <summary>El método de la última petición (nameof), sin sus datos; null si ninguna.</summary>
        public string Ultima { get; private set; }

        public UsuarioCuenta Usuario { get; private set; }
        public event Action Cambiado;

        public Task<Resultado<UsuarioCuenta>> EntrarConGoogle(CancellationToken ct) => Entrar(nameof(EntrarConGoogle), Responder, ct);

        public Task<Resultado<UsuarioCuenta>> CrearConCorreo(string correo, string contrasena, CancellationToken ct) =>
            Entrar(nameof(CrearConCorreo), t => ResponderCorreo(correo, contrasena, t), ct);

        public Task<Resultado<UsuarioCuenta>> EntrarConCorreo(string correo, string contrasena, CancellationToken ct) =>
            Entrar(nameof(EntrarConCorreo), t => ResponderCorreo(correo, contrasena, t), ct);

        public async Task<Resultado<bool>> EnviarContrasena(string correo, CancellationToken ct)
        {
            Peticiones++;
            Ultima = nameof(EnviarContrasena);
            var r = await ResponderContrasena(correo, ct);
            return ct.IsCancellationRequested ? Resultado<bool>.Fallo(ErroresCuenta.Cancelado) : r;
        }

        async Task<Resultado<UsuarioCuenta>> Entrar(string que, Func<CancellationToken, Task<Resultado<UsuarioCuenta>>> responder, CancellationToken ct)
        {
            Peticiones++;
            Ultima = que;
            var r = await responder(ct);
            if (ct.IsCancellationRequested) return Resultado<UsuarioCuenta>.Fallo(ErroresCuenta.Cancelado);
            if (r.Ok) Poner(r.Valor);
            return r;
        }

        /// <summary>Cambia la sesión, como al recuperar la guardada (null: sin sesión).</summary>
        public void Poner(UsuarioCuenta usuario)
        {
            Usuario = usuario;
            Cambiado?.Invoke();
        }
    }
}
