using System;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Cuentas con Firebase Auth (historia 3.3): el selector de Google del móvil da el idToken y con él se entra en Firebase,
    /// que guarda la sesión. Se crea y se usa en el hilo principal, y responde en él. Los registros solo llevan el código y
    /// el tipo del error: nunca el correo, el nombre ni el token.
    /// </summary>
    public sealed class CuentasFirebase : ICuentas
    {
        readonly IServidor servidor;
        readonly IAccesoAuth acceso;
        readonly ISelectorGoogle google;
        readonly Func<bool> hayConexion;
        readonly SynchronizationContext hilo = SynchronizationContext.Current;
        bool escuchando;
        Task entrando; // la entrada en Firebase en curso; null si no hay ninguna. De una en una

        public CuentasFirebase(IServidor servidor) : this(servidor, new AccesoAuth(), new SelectorGoogleAndroid(),
            () => Application.internetReachability != NetworkReachability.NotReachable) { }

        internal CuentasFirebase(IServidor servidor, IAccesoAuth acceso, ISelectorGoogle google, Func<bool> hayConexion)
        {
            this.servidor = servidor ?? throw new ArgumentNullException(nameof(servidor));
            this.acceso = acceso ?? throw new ArgumentNullException(nameof(acceso));
            this.google = google ?? throw new ArgumentNullException(nameof(google));
            this.hayConexion = hayConexion ?? throw new ArgumentNullException(nameof(hayConexion));
            servidor.Cambiado += EscucharSiListo;
            EscucharSiListo();
        }

        public UsuarioCuenta Usuario { get; private set; }
        public event Action Cambiado;

        public async Task<Resultado<UsuarioCuenta>> EntrarConGoogle(CancellationToken ct)
        {
            if (servidor.Estado != EstadoServidor.Listo) return Fallo(ErroresCuenta.NoDisponible);
            try
            {
                if (!hayConexion()) return Fallo(ErroresCuenta.SinConexion);
                var respuesta = await google.Pedir(ct);
                if (ct.IsCancellationRequested) return Fallo(ErroresCuenta.Cancelado);
                if (respuesta.Estado != EstadoGoogle.Hecha) return Fallo(CodigoDe(respuesta));

                return await EntrarEnFirebase(respuesta.IdToken, ct);
            }
            catch (Exception e)
            {
                string codigo = ct.IsCancellationRequested ? ErroresCuenta.Cancelado : CodigosAuth.De(e);
                if (codigo != ErroresCuenta.Cancelado)
                    Debug.LogWarning($"PriceMap: no se pudo entrar en la cuenta ({codigo}, {(e as FalloAuth)?.Codigo ?? e.GetType().Name}).");
                return Fallo(codigo);
            }
        }

        // De una en una: una cancelada que aún contesta acaba (y sale) antes de que entre la siguiente. La sesión se lee una
        // vez, al acabar: la de una cancelada no se ve nunca
        async Task<Resultado<UsuarioCuenta>> EntrarEnFirebase(string idToken, CancellationToken ct)
        {
            while (entrando != null) await entrando;
            if (ct.IsCancellationRequested) return Fallo(ErroresCuenta.Cancelado);
            var acabada = new TaskCompletionSource<bool>();
            entrando = acabada.Task;
            try
            {
                UsuarioCuenta usuario;
                try
                {
                    usuario = await acceso.Entrar(idToken);
                    if (ct.IsCancellationRequested) acceso.Salir(); // cerrada mientras Firebase contestaba
                }
                finally
                {
                    entrando = null;
                    Leer();
                }
                if (ct.IsCancellationRequested) return Fallo(ErroresCuenta.Cancelado);
                usuario = Usuario ?? usuario;
                return usuario != null ? Resultado<UsuarioCuenta>.Exito(usuario) : Fallo(ErroresCuenta.Desconocido);
            }
            finally { acabada.SetResult(true); }
        }

        static Resultado<UsuarioCuenta> Fallo(string error) => Resultado<UsuarioCuenta>.Fallo(error);

        string CodigoDe(RespuestaGoogle r)
        {
            if (r.Estado == EstadoGoogle.Cancelada) return ErroresCuenta.Cancelado;
            string codigo = r.Estado == EstadoGoogle.NoDisponible ? ErroresCuenta.NoDisponible
                : hayConexion() ? ErroresCuenta.Desconocido : ErroresCuenta.SinConexion;
            Debug.LogWarning($"PriceMap: Google no ha dado la cuenta ({codigo}, {r.Motivo}).");
            return codigo;
        }

        // Al estar Listo se lee la sesión guardada y se sigue
        void EscucharSiListo()
        {
            if (escuchando || servidor.Estado != EstadoServidor.Listo) return;
            escuchando = true;
            try
            {
                acceso.Escuchar(() => EnElHilo(Leer));
                Leer();
            }
            catch (Exception e) { Debug.LogWarning($"PriceMap: no se pudo leer la sesión ({e.GetType().Name})."); }
        }

        void Leer()
        {
            if (entrando != null) return; // se lee al acabar
            var leido = acceso.Usuario;
            if (Igual(leido, Usuario)) return;
            Usuario = leido;
            Cambiado?.Invoke();
        }

        static bool Igual(UsuarioCuenta a, UsuarioCuenta b) =>
            a == null || b == null ? a == b : a.Uid == b.Uid && a.Nombre == b.Nombre && a.Correo == b.Correo;

        void EnElHilo(Action accion)
        {
            if (hilo == null || SynchronizationContext.Current == hilo) accion();
            else hilo.Post(_ => accion(), null);
        }
    }
}
