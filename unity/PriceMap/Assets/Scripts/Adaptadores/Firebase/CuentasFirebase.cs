using System;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Cuentas con Firebase Auth (historia 3.3): el selector de Google del móvil da el idToken y con él se entra en Firebase,
    /// que guarda la sesión; o correo y contraseña, que guarda Firebase y aquí solo pasan (parte 2). Se crea y se usa en el
    /// hilo principal, y responde en él. Los registros solo llevan el código y el tipo del error: nunca el correo, la
    /// contraseña, el nombre ni el token.
    /// </summary>
    public sealed class CuentasFirebase : ICuentas
    {
        readonly IServidor servidor;
        readonly IAccesoAuth acceso;
        readonly ISelectorGoogle google;
        readonly Func<bool> hayConexion;
        readonly SynchronizationContext hilo = SynchronizationContext.Current;
        bool escuchando;
        Task entrando; // la petición a Firebase en curso; null si no hay ninguna. De una en una

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

        public Task<Resultado<UsuarioCuenta>> EntrarConGoogle(CancellationToken ct) => Pedir(async () =>
        {
            var respuesta = await google.Pedir(ct);
            if (ct.IsCancellationRequested) return Fallo(ErroresCuenta.Cancelado);
            if (respuesta.Estado != EstadoGoogle.Hecha) return Fallo(CodigoDe(respuesta));
            return await EntrarEnFirebase(() => acceso.Entrar(respuesta.IdToken), ct);
        }, CodigosAuth.De, ct);

        public Task<Resultado<UsuarioCuenta>> CrearConCorreo(string correo, string contrasena, CancellationToken ct) =>
            Pedir(() => EntrarEnFirebase(() => acceso.CrearConCorreo(correo, contrasena), ct), CodigosAuth.DeCorreo, ct);

        public Task<Resultado<UsuarioCuenta>> EntrarConCorreo(string correo, string contrasena, CancellationToken ct) =>
            Pedir(() => EntrarEnFirebase(() => acceso.EntrarConCorreo(correo, contrasena), ct), CodigosAuth.DeCorreo, ct);

        public Task<Resultado<bool>> EnviarContrasena(string correo, CancellationToken ct) => Pedir(() => EntrarEnFirebase(async () =>
        {
            try { await acceso.EnviarContrasena(correo); }
            catch (FalloAuth e) when (e.Codigo == CodigosAuth.UsuarioDesconocido) { } // no se dice si el correo existe
            return true;
        }, ct, abreSesion: false), CodigosAuth.DeCorreo, ct, "enviar el correo de la contraseña");

        async Task<Resultado<T>> Pedir<T>(Func<Task<Resultado<T>>> pedir, Func<Exception, string> codigoDe, CancellationToken ct,
            string que = "entrar en la cuenta")
        {
            if (servidor.Estado != EstadoServidor.Listo) return Resultado<T>.Fallo(ErroresCuenta.NoDisponible);
            try
            {
                if (!hayConexion()) return Resultado<T>.Fallo(ErroresCuenta.SinConexion);
                return await pedir();
            }
            catch (Exception e)
            {
                string codigo = ct.IsCancellationRequested ? ErroresCuenta.Cancelado : codigoDe(e);
                if (codigo != ErroresCuenta.Cancelado)
                    Debug.LogWarning($"PriceMap: no se pudo {que} ({codigo}, {(e as FalloAuth)?.Codigo ?? e.GetType().Name}).");
                return Resultado<T>.Fallo(codigo);
            }
        }

        async Task<Resultado<UsuarioCuenta>> EntrarEnFirebase(Func<Task<UsuarioCuenta>> entrar, CancellationToken ct)
        {
            var r = await EntrarEnFirebase<UsuarioCuenta>(entrar, ct);
            if (!r.Ok) return r;
            var usuario = Usuario ?? r.Valor;
            return usuario != null ? Resultado<UsuarioCuenta>.Exito(usuario) : Fallo(ErroresCuenta.Desconocido);
        }

        // De una en una: una cancelada que aún contesta acaba (y sale) antes de que entre la siguiente. La sesión se lee una
        // vez, al acabar: la de una cancelada no se ve nunca
        async Task<Resultado<T>> EntrarEnFirebase<T>(Func<Task<T>> pedir, CancellationToken ct, bool abreSesion = true)
        {
            while (entrando != null) await entrando;
            if (ct.IsCancellationRequested) return Resultado<T>.Fallo(ErroresCuenta.Cancelado);
            var acabada = new TaskCompletionSource<bool>();
            entrando = acabada.Task;
            try
            {
                T valor;
                try
                {
                    valor = await pedir();
                    if (abreSesion && ct.IsCancellationRequested) acceso.Salir(); // cerrada mientras Firebase contestaba
                }
                finally
                {
                    entrando = null;
                    Leer();
                }
                return ct.IsCancellationRequested ? Resultado<T>.Fallo(ErroresCuenta.Cancelado) : Resultado<T>.Exito(valor);
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
