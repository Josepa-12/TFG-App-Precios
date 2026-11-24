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

    /// <summary>Qué se ve de la entrada abierta: los botones o un formulario del correo (historia 3.3, parte 2).</summary>
    public enum PasoCuenta
    {
        /// <summary>Google, «Con correo» y «Ahora no».</summary>
        Botones,
        Crear,
        Entrar,
        /// <summary>«He olvidado la contraseña».</summary>
        Olvidada,
    }

    /// <summary>
    /// Cuenta del usuario (historia 3.3): quién ha entrado y la entrada (hoja o pantalla completa), que siempre se puede
    /// cerrar sin cuenta. ExigirCuenta la abre para una acción y sigue con ella al entrar. La UI enseña su estado y le manda
    /// lo que se pulsa. Los datos del usuario nunca van al registro; el correo y la contraseña que se escriben tampoco se
    /// guardan aquí: solo pasan a ICuentas.
    /// </summary>
    public class ServicioCuentas
    {
        /// <summary>Preferencia: la pantalla completa ya se ha ofrecido («1»).</summary>
        public const string ClaveOfrecida = "cuenta.ofrecida";

        /// <summary>Lo que se espera a que el servidor acabe de prepararse al pedir entrar.</summary>
        public static readonly TimeSpan EsperaServidor = TimeSpan.FromSeconds(10);

        /// <summary>Lo que debe medir al menos la contraseña al crear la cuenta.</summary>
        public const int ContrasenaMinima = 8;

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

        /// <summary>Qué se ve de la entrada: los botones o un formulario del correo. Cerrada, los botones.</summary>
        public PasoCuenta Paso { get; private set; }

        /// <summary>En «He olvidado la contraseña»: el enlace ya se ha pedido (haya o no cuenta con ese correo).</summary>
        public bool Enviado { get; private set; }

        /// <summary>Hay una petición de entrar en curso.</summary>
        public bool Entrando => peticion != null;

        /// <summary>Código de ErroresCuenta del último intento (sigue mientras se reintenta); null si no hay que decir nada.</summary>
        public string Error { get; private set; }

        /// <summary>Fallos que ha habido que decir (para distinguir dos iguales seguidos).</summary>
        public int Fallos { get; private set; }

        /// <summary>Ha cambiado Usuario.</summary>
        public event Action SesionCambiada;
        /// <summary>Ha cambiado Entrada, Paso, Entrando, Enviado o Error.</summary>
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

        /// <summary>
        /// «Con correo» (a Crear), de un formulario a otro o de vuelta a los botones. La petición en curso se cancela y el
        /// error se olvida.
        /// </summary>
        public void IrA(PasoCuenta paso)
        {
            if (Entrada == EntradaCuenta.Cerrada || Paso == paso) return;
            AcabarPeticion();
            Error = null;
            Enviado = false;
            Paso = paso;
            EntradaCambiada?.Invoke();
        }

        /// <summary>Atrás: de un formulario, a los botones; en ellos, «Ahora no».</summary>
        public void Atras()
        {
            if (Paso != PasoCuenta.Botones) IrA(PasoCuenta.Botones);
            else AhoraNo();
        }

        /// <summary>«Continuar con Google» o «Reintentar»: una sola petición a la vez. Al entrar se cierra y sigue la acción pendiente.</summary>
        public Task EntrarConGoogle() => Pedir(PasoCuenta.Botones, cuentas.EntrarConGoogle, Acabar);

        /// <summary>«Crear cuenta», como Google. Con un correo no válido o una contraseña corta, su error sin pedir nada.</summary>
        public Task CrearConCorreo(string correo, string contrasena) =>
            PedirConCorreo(PasoCuenta.Crear, correo,
                (contrasena ?? "").Length < ContrasenaMinima ? ErroresCuenta.ContrasenaDebil : null,
                (c, ct) => cuentas.CrearConCorreo(c, contrasena, ct), Acabar);

        /// <summary>«Entrar», como Google. Con un correo no válido o sin contraseña, su error sin pedir nada.</summary>
        public Task EntrarConCorreo(string correo, string contrasena) =>
            PedirConCorreo(PasoCuenta.Entrar, correo, string.IsNullOrEmpty(contrasena) ? ErroresCuenta.ContrasenaVacia : null,
                (c, ct) => cuentas.EntrarConCorreo(c, contrasena, ct), Acabar);

        /// <summary>«Enviar enlace» o «Enviar otro». Con un correo no válido, su error sin pedir nada.</summary>
        public Task EnviarContrasena(string correo) =>
            PedirConCorreo(PasoCuenta.Olvidada, correo, null, cuentas.EnviarContrasena, _ => Enviado = true);

        /// <summary>Correo con una «@» y un punto dentro del dominio, sin espacios (los de los lados no cuentan).</summary>
        public static bool CorreoValido(string correo)
        {
            string c = (correo ?? "").Trim();
            int arroba = c.IndexOf('@');
            if (arroba <= 0 || arroba != c.LastIndexOf('@')) return false;
            foreach (char letra in c) if (char.IsWhiteSpace(letra)) return false;
            string dominio = c.Substring(arroba + 1);
            return dominio.IndexOf('.') > 0 && dominio[dominio.Length - 1] != '.';
        }

        Task PedirConCorreo<T>(PasoCuenta paso, string correo, string fallo,
            Func<string, CancellationToken, Task<Resultado<T>>> pedir, Action<T> bien)
        {
            if (Entrada == EntradaCuenta.Cerrada || Paso != paso || Entrando) return Task.CompletedTask;
            string limpio = (correo ?? "").Trim();
            if (!CorreoValido(limpio)) fallo = ErroresCuenta.CorreoNoValido;
            if (fallo == null) return Pedir(paso, ct => pedir(limpio, ct), bien);
            Fallar(fallo);
            return Task.CompletedTask;
        }

        void Acabar(UsuarioCuenta usuario) => Cerrar(entrado: true, saludar: usuario);

        void Fallar(string error)
        {
            Error = error;
            Enviado = false;
            Fallos++;
            EntradaCambiada?.Invoke();
        }

        async Task Pedir<T>(PasoCuenta paso, Func<CancellationToken, Task<Resultado<T>>> pedir, Action<T> bien)
        {
            if (Entrada == EntradaCuenta.Cerrada || Paso != paso || Entrando) return;
            var esta = new CancellationTokenSource();
            var ct = esta.Token;
            peticion = esta;
            EntradaCambiada?.Invoke();

            Resultado<T> r;
            try
            {
                if (!await ServidorListo(ct)) r = Resultado<T>.Fallo(ErroresCuenta.NoDisponible);
                else if (Usuario != null) r = default; // al prepararse había una sesión guardada: se cierra abajo
                else r = await pedir(ct);
            }
            catch (OperationCanceledException) { r = Resultado<T>.Fallo(ErroresCuenta.Cancelado); }
            catch (Exception) { r = Resultado<T>.Fallo(ErroresCuenta.Desconocido); }

            if (peticion != esta) return; // cerrada o en otro paso mientras tanto
            if (r.Ok) bien(r.Valor);
            if (peticion != esta) return; // ha entrado: cerrada
            if (Usuario != null) { Cerrar(entrado: true); return; } // la guardada ha llegado mientras tanto: sin saludar
            AcabarPeticion();
            if (!r.Ok && r.Error != ErroresCuenta.Cancelado) { Fallar(r.Error); return; }
            Error = null;
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
            // Si se está entrando, la cierra Pedir
            if (Usuario != null && Entrada != EntradaCuenta.Cerrada && !Entrando) Cerrar(entrado: true);
        }

        void Cerrar(bool entrado, UsuarioCuenta saludar = null)
        {
            var accion = entrado ? pendiente : null;
            pendiente = null;
            Error = null;
            Enviado = false;
            Paso = PasoCuenta.Botones;
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
