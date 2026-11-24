using System;
using System.Threading;
using System.Threading.Tasks;

namespace PriceMap.Aplicacion
{
    /// <summary>Cómo está a la vista la entrada en la cuenta (historia 3.3).</summary>
    public enum EntradaCuenta
    {
        Cerrada,
        /// <summary>Hoja sobre lo que hubiera: desde el avatar o una acción que exige cuenta.</summary>
        Hoja,
        /// <summary>A pantalla completa, una sola vez, tras la bienvenida.</summary>
        Completa,
    }

    /// <summary>
    /// Cuenta del usuario (historia 3.3): quién ha entrado y la entrada (hoja o pantalla completa), que siempre se puede
    /// cerrar sin cuenta. ExigirCuenta la abre para una acción y sigue con ella al entrar. La UI enseña su estado y le manda
    /// lo que se pulsa. Los datos del usuario nunca van al registro.
    /// </summary>
    public class ServicioCuentas
    {
        /// <summary>Preferencia: la pantalla completa ya se ha ofrecido («1»).</summary>
        public const string ClaveOfrecida = "cuenta.ofrecida";

        /// <summary>Lo que se espera a que el servidor acabe de prepararse al pedir entrar.</summary>
        public static readonly TimeSpan EsperaServidor = TimeSpan.FromSeconds(10);

        readonly ICuentas cuentas;
        readonly IServidor servidor;
        readonly IPreferencias preferencias;
        readonly Func<TimeSpan, CancellationToken, Task> esperar;
        CancellationTokenSource peticion; // la de entrar en curso; null si no hay ninguna
        Action pendiente;                 // la acción que espera a que se entre; null si ninguna

        /// <param name="servidor">Se espera a que esté Listo para entrar; null: no hay que esperar a nadie (cuentas falsas).</param>
        /// <param name="esperar">Espera cancelable (en la app, Task.Delay).</param>
        public ServicioCuentas(ICuentas cuentas, IServidor servidor, IPreferencias preferencias, Func<TimeSpan, CancellationToken, Task> esperar)
        {
            this.cuentas = cuentas ?? throw new ArgumentNullException(nameof(cuentas));
            this.servidor = servidor;
            this.preferencias = preferencias ?? throw new ArgumentNullException(nameof(preferencias));
            this.esperar = esperar ?? throw new ArgumentNullException(nameof(esperar));
            cuentas.Cambiado += AlCambiarSesion;
        }

        /// <summary>Quién ha entrado; null sin sesión.</summary>
        public UsuarioCuenta Usuario => cuentas.Usuario;

        public EntradaCuenta Entrada { get; private set; }

        /// <summary>Hay una petición de entrar en curso.</summary>
        public bool Entrando => peticion != null;

        /// <summary>Código de ErroresCuenta del último intento (sigue mientras se reintenta); null si no hay que decir nada.</summary>
        public string Error { get; private set; }

        /// <summary>Ha cambiado Usuario.</summary>
        public event Action SesionCambiada;
        /// <summary>Ha cambiado Entrada, Entrando o Error.</summary>
        public event Action EntradaCambiada;
        /// <summary>El usuario acaba de entrar (no al recuperar la sesión guardada). La entrada ya está cerrada.</summary>
        public event Action<UsuarioCuenta> HaEntrado;

        /// <summary>Abre la hoja para entrar; con sesión o ya abierta, nada.</summary>
        public void Abrir()
        {
            if (Usuario != null || Entrada != EntradaCuenta.Cerrada) return;
            Entrada = EntradaCuenta.Hoja;
            EntradaCambiada?.Invoke();
        }

        /// <summary>Tras la bienvenida: la pantalla completa, una sola vez. Con sesión no sale.</summary>
        public void Ofrecer()
        {
            if (preferencias.Leer(ClaveOfrecida) == "1") return;
            preferencias.Guardar(ClaveOfrecida, "1"); // ya al salir: cerrada la app con ella delante, tampoco vuelve
            if (Usuario != null || Entrada != EntradaCuenta.Cerrada) return;
            Entrada = EntradaCuenta.Completa;
            EntradaCambiada?.Invoke();
        }

        /// <summary>
        /// Para una acción que necesita cuenta: con sesión la hace ya; sin ella abre la hoja y la hace al entrar. Si se
        /// cierra sin entrar, no se hace. Una pendiente a la vez: la nueva sustituye a la anterior.
        /// </summary>
        public void ExigirCuenta(Action alEntrar)
        {
            if (alEntrar == null) throw new ArgumentNullException(nameof(alEntrar));
            if (Usuario != null) { alEntrar(); return; }
            pendiente = alEntrar;
            Abrir();
        }

        /// <summary>«Ahora no» o atrás: se cierra sin cuenta, y con ella la petición en curso y la acción pendiente.</summary>
        public void AhoraNo()
        {
            if (Entrada != EntradaCuenta.Cerrada) Cerrar(entrado: false);
        }

        /// <summary>«Continuar con Google» o «Reintentar»: una sola petición a la vez. Al entrar se cierra y sigue la acción pendiente.</summary>
        public async Task EntrarConGoogle()
        {
            if (Entrada == EntradaCuenta.Cerrada || Entrando) return;
            var esta = new CancellationTokenSource();
            var ct = esta.Token;
            peticion = esta;
            EntradaCambiada?.Invoke();

            Resultado<UsuarioCuenta> r;
            bool conGoogle = false;
            try
            {
                if (!await ServidorListo(ct)) r = Resultado<UsuarioCuenta>.Fallo(ErroresCuenta.NoDisponible);
                else if (Usuario != null) r = Resultado<UsuarioCuenta>.Exito(Usuario); // al prepararse había una sesión guardada
                else
                {
                    conGoogle = true;
                    r = await cuentas.EntrarConGoogle(ct);
                }
            }
            catch (OperationCanceledException) { r = Resultado<UsuarioCuenta>.Fallo(ErroresCuenta.Cancelado); }
            catch (Exception) { r = Resultado<UsuarioCuenta>.Fallo(ErroresCuenta.Desconocido); }

            if (peticion != esta) return; // cerrada mientras tanto
            if (r.Ok) { Cerrar(entrado: true, saludar: conGoogle ? r.Valor : null); return; }
            AcabarPeticion();
            Error = r.Error == ErroresCuenta.Cancelado ? null : r.Error;
            EntradaCambiada?.Invoke();
        }

        async Task<bool> ServidorListo(CancellationToken ct)
        {
            if (servidor == null) return true;
            if (servidor.Estado != EstadoServidor.Iniciando) return servidor.Estado == EstadoServidor.Listo;
            var preparado = new TaskCompletionSource<bool>();
            void AlCambiar()
            {
                if (servidor.Estado != EstadoServidor.Iniciando) preparado.TrySetResult(true);
            }
            servidor.Cambiado += AlCambiar;
            try { await Task.WhenAny(preparado.Task, esperar(EsperaServidor, ct)); }
            finally { servidor.Cambiado -= AlCambiar; }
            ct.ThrowIfCancellationRequested();
            return servidor.Estado == EstadoServidor.Listo;
        }

        void AlCambiarSesion()
        {
            SesionCambiada?.Invoke();
            // La sesión guardada llega con la entrada abierta (el servidor acababa de prepararse): ya no hace falta.
            // Si se está entrando, la cierra EntrarConGoogle
            if (Usuario != null && Entrada != EntradaCuenta.Cerrada && !Entrando) Cerrar(entrado: true);
        }

        void Cerrar(bool entrado, UsuarioCuenta saludar = null)
        {
            var accion = entrado ? pendiente : null;
            pendiente = null;
            Error = null;
            Entrada = EntradaCuenta.Cerrada;
            AcabarPeticion();
            EntradaCambiada?.Invoke();
            if (saludar != null) HaEntrado?.Invoke(saludar);
            accion?.Invoke();
        }

        void AcabarPeticion()
        {
            var acabada = peticion;
            peticion = null;
            acabada?.Cancel(); // también suelta la espera al servidor
            acabada?.Dispose();
        }
    }
}
