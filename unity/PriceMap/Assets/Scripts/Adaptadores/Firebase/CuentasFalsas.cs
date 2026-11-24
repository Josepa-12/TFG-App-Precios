using System;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Cuentas de mentira (PC, editor y captura de pruebas): nunca tocan Firebase ni guardan la sesión. Se entra siempre como
    /// Ana, salvo que se cambie Responder.
    /// </summary>
    public sealed class CuentasFalsas : ICuentas
    {
        public static readonly UsuarioCuenta Ana = new UsuarioCuenta("falsa", "Ana García", "ana@example.com");

        /// <summary>Lo que contesta Google a cada petición.</summary>
        public Func<CancellationToken, Task<Resultado<UsuarioCuenta>>> Responder { get; set; } =
            _ => Task.FromResult(Resultado<UsuarioCuenta>.Exito(Ana));

        /// <summary>Peticiones hechas.</summary>
        public int Peticiones { get; private set; }

        public UsuarioCuenta Usuario { get; private set; }
        public event Action Cambiado;

        public async Task<Resultado<UsuarioCuenta>> EntrarConGoogle(CancellationToken ct)
        {
            Peticiones++;
            var r = await Responder(ct);
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
