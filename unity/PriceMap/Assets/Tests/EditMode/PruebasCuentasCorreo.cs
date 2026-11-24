using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PriceMap.Adaptadores.Firebase;
using PriceMap.Aplicacion;
using PriceMap.UI;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Entrar con correo (historia 3.3, parte 2): cada fila de la matriz con cuentas falsas, los formularios, TalkBack, el
    /// teclado, el aspecto y Firebase Auth con uno falso.
    /// </summary>
    public partial class PruebasCuentas
    {
        const string Clave8 = "12345678";

        FormularioCuenta Form => pantalla.Cuenta.Formulario;

        FormularioCuenta AbrirFormulario(PasoCuenta paso = PasoCuenta.Crear, string correo = "", string contrasena = "")
        {
            if (pantalla == null || !pantalla.CuentaAbierta)
            {
                Abrir();
                pantalla.TocarAvatar();
            }
            if (cuentas.Paso == PasoCuenta.Botones) pantalla.Cuenta.PulsarConCorreo();
            cuentas.IrA(paso);
            Form.Correo.SetValueWithoutNotify(correo);
            Form.Contrasena.SetValueWithoutNotify(contrasena);
            return Form;
        }

        void ContestarFalloCorreo(string error) =>
            google.ResponderCorreo = (_, __, ___) => Task.FromResult(Resultado<UsuarioCuenta>.Fallo(error));

        TaskCompletionSource<Resultado<UsuarioCuenta>> ContestarCorreoDespues(Action<CancellationToken> alPedir = null)
        {
            var respuesta = new TaskCompletionSource<Resultado<UsuarioCuenta>>();
            google.ResponderCorreo = (_, __, ct) => { alPedir?.Invoke(ct); return respuesta.Task; };
            return respuesta;
        }

        [TearDown]
        public void OlvidarPantalla() => pantalla = null;

        // ───────────────────────── Matriz ─────────────────────────

        [Test]
        public void Correo_Crear_SesionYAccionPendienteUnaVez()
        {
            Abrir();
            int hechas = 0;
            cuentas.ExigirCuenta(() => hechas++);
            var f = AbrirFormulario(PasoCuenta.Crear, "  ana@example.com ", " 234567 ");
            (string, string) pedido = default;
            var responder = google.ResponderCorreo;
            google.ResponderCorreo = (c, p, ct) => { pedido = (c, p); return responder(c, p, ct); };
            f.Enviar();
            Assert.AreEqual(("ana@example.com", " 234567 "), pedido, "Del correo se quitan los espacios; de la contraseña, nada.");
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.AreEqual(nameof(ICuentas.CrearConCorreo), google.Ultima);
            Assert.AreEqual("ana@example.com", cuentas.Usuario.Correo);
            Assert.AreEqual("Hola, ana@example.com", pantalla.Toast.Texto.text);
            Assert.AreEqual("A", pantalla.Buscador.Inicial.text);
            Assert.AreEqual(1, hechas);
            Assert.AreEqual("", f.Contrasena.value, "Al cerrarse se vacía.");
        }

        [Test]
        public void Correo_Entrar_SesionIniciada()
        {
            AbrirFormulario(PasoCuenta.Entrar, "ana@example.com", "corta").Enviar();
            Assert.AreEqual(nameof(ICuentas.EntrarConCorreo), google.Ultima);
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.AreEqual("ana@example.com", cuentas.Usuario.Correo);
        }

        [TestCase(PasoCuenta.Crear)]
        [TestCase(PasoCuenta.Entrar)]
        [TestCase(PasoCuenta.Olvidada)]
        public void Correo_NoValido_ErrorJuntoAlCampoSinPeticion(PasoCuenta paso)
        {
            var f = AbrirFormulario(paso, "ana@", Clave8);
            f.Enviar();
            Assert.AreEqual(0, google.Peticiones);
            Assert.IsTrue(pantalla.CuentaAbierta);
            Assert.IsTrue(SeVe(f.ErrorCorreo) && !SeVe(f.ErrorContrasena) && !SeVe(f.Aviso) && !SeVe(f.EntrarConEse));
            Assert.AreEqual(T(ClavesTexto.CuentaErrorCorreo), f.ErrorCorreo.text);
            Assert.AreSame(f.CajaCorreo, f.ErrorCorreo.parent[f.ErrorCorreo.parent.IndexOf(f.ErrorCorreo) - 1], "Justo debajo del campo.");
            Assert.IsTrue(f.CajaCorreo.ClassListContains(FormularioCuenta.ClaseMal));
            Assert.AreEqual(1, anuncios.Count(a => a == f.ErrorCorreo.text));
            f.Enviar();
            Assert.AreEqual(2, anuncios.Count(a => a == f.ErrorCorreo.text), "El mismo otra vez se vuelve a decir.");
            Assert.AreSame(f.Principal, pantalla.Accesibilidad.Enfocado);
        }

        [TestCase("ana@example.com", true)]
        [TestCase("  ana.b+c@mi.correo.es  ", true)]
        [TestCase("ana@", false)]
        [TestCase("@example.com", false)]
        [TestCase("ana", false)]
        [TestCase("ana@example", false)]
        [TestCase("ana@.com", false)]
        [TestCase("ana@example.", false)]
        [TestCase("a@b@example.com", false)]
        [TestCase("ana b@example.com", false)]
        [TestCase("ana@exam ple.com", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void Correo_Valido(string correo, bool valido) => Assert.AreEqual(valido, ServicioCuentas.CorreoValido(correo));

        [TestCase("12345", true)]
        [TestCase("1234567", true)]
        [TestCase("12345678", false)]
        public void Correo_ContrasenaCorta_ErrorJuntoAlCampoSinPeticion(string contrasena, bool corta)
        {
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", contrasena);
            Assert.IsTrue(SeVe(f.Ayuda));
            f.Enviar();
            Assert.AreEqual(corta ? 0 : 1, google.Peticiones);
            if (!corta) return;
            Assert.IsTrue(SeVe(f.ErrorContrasena) && !SeVe(f.Ayuda) && !SeVe(f.ErrorCorreo));
            Assert.AreEqual("La contraseña debe tener 8 caracteres como mínimo.", f.ErrorContrasena.text);
            Assert.IsTrue(f.CajaContrasena.ClassListContains(FormularioCuenta.ClaseMal));
            CollectionAssert.Contains(anuncios, f.ErrorContrasena.text);
        }

        [Test]
        public void Correo_YaUsado_LoDiceYDejaEntrarConElMismo()
        {
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", Clave8);
            ContestarFalloCorreo(ErroresCuenta.CorreoUsado);
            f.Enviar();
            Assert.AreEqual("Ya hay una cuenta con ese correo. Entra o, si la creaste con Google, usa Google.", f.ErrorCorreo.text);
            CollectionAssert.Contains(anuncios, f.ErrorCorreo.text);
            Assert.IsTrue(SeVe(f.ErrorCorreo) && SeVe(f.EntrarConEse));
            Assert.AreEqual("Entrar", f.EntrarConEse.text);
            CollectionAssert.Contains(Recorrido(), "Entrar");
            Tocar(f.EntrarConEse, f.EntrarConEse.clickable);
            Assert.AreEqual(PasoCuenta.Entrar, cuentas.Paso);
            Assert.AreEqual(("ana@example.com", ""), (f.Correo.value, f.Contrasena.value));
            Assert.IsFalse(SeVe(f.ErrorCorreo) || SeVe(f.EntrarConEse));
            Assert.IsFalse(f.CajaCorreo.ClassListContains(FormularioCuenta.ClaseMal));
        }

        [Test]
        public void Correo_NoCoinciden_JuntoALaContrasena()
        {
            var f = AbrirFormulario(PasoCuenta.Entrar, "ana@example.com", "12345");
            ContestarFalloCorreo(ErroresCuenta.NoCoinciden);
            f.Enviar();
            Assert.AreEqual(1, google.Peticiones);
            Assert.IsTrue(SeVe(f.ErrorContrasena) && !SeVe(f.Aviso));
            Assert.AreEqual("El correo o la contraseña no coinciden.", f.ErrorContrasena.text);
            CollectionAssert.Contains(anuncios, f.ErrorContrasena.text);
            Assert.AreEqual("12345", f.Contrasena.value, "En el mismo formulario se puede corregir.");
            Assert.AreEqual("Entrar", f.Principal.text);

            f.Contrasena.SetValueWithoutNotify("");
            f.Enviar();
            Assert.AreEqual(1, google.Peticiones, "Vacía, sin petición.");
            Assert.IsTrue(SeVe(f.ErrorContrasena) && f.CajaContrasena.ClassListContains(FormularioCuenta.ClaseMal));
            Assert.AreEqual("Escribe la contraseña.", f.ErrorContrasena.text);
        }

        [Test]
        public void Correo_Olvidada_MismoMensajeYEnviarOtro()
        {
            var f = AbrirFormulario(PasoCuenta.Entrar, "ana@example.com", "x");
            Tocar(f.Olvidada, f.Olvidada.clickable);
            Assert.AreEqual(PasoCuenta.Olvidada, cuentas.Paso);
            Assert.AreEqual("ana@example.com", f.Correo.value);
            Assert.IsTrue(SeVe(f.Para) && !SeVe(f.Aviso) && !SeVe(f.Contrasena.parent.parent));
            Assert.AreEqual(("Nueva contraseña", "Enviar enlace", "Volver a entrar"), (f.Titulo.text, f.Principal.text, f.Secundario.text));
            string pedido = null;
            google.ResponderContrasena = (c, _) => { pedido = c; return Task.FromResult(Resultado<bool>.Exito(true)); };
            f.Enviar();
            Assert.AreEqual("ana@example.com", pedido);
            Assert.AreEqual(nameof(ICuentas.EnviarContrasena), google.Ultima);
            Assert.IsTrue(cuentas.Enviado);
            const string Texto = "Si hay una cuenta con ese correo, te llegará un enlace para poner otra contraseña. Mira también en spam.";
            Assert.IsTrue(SeVe(f.Aviso) && !SeVe(f.Para));
            Assert.AreEqual(Texto, f.Aviso.text);
            Assert.AreEqual("Enviar otro", f.Principal.text);
            Assert.AreEqual(1, anuncios.Count(a => a == Texto));
            f.Enviar();
            Assert.AreEqual(2, google.Peticiones);
            Assert.AreEqual(2, anuncios.Count(a => a == Texto));

            // Un fallo al enviar otro: ya no dice que se ha enviado
            google.ResponderContrasena = (_, __) => Task.FromResult(Resultado<bool>.Fallo(ErroresCuenta.SinConexion));
            f.Enviar();
            Assert.IsFalse(cuentas.Enviado);
            Assert.AreEqual("Necesitas conexión para entrar.", f.Aviso.text);
            Assert.AreEqual("Reintentar", f.Principal.text);

            f.PulsarSecundario();
            Assert.AreEqual(PasoCuenta.Entrar, cuentas.Paso);
            Assert.IsNull(cuentas.Error);
        }

        [Test]
        public void Correo_DemasiadosIntentos()
        {
            var f = AbrirFormulario(PasoCuenta.Entrar, "ana@example.com", "12345");
            ContestarFalloCorreo(ErroresCuenta.DemasiadosIntentos);
            f.Enviar();
            Assert.IsTrue(SeVe(f.Aviso));
            Assert.AreEqual("Demasiados intentos. Espera un rato.", f.Aviso.text);
            Assert.AreEqual("Entrar", f.Principal.text, "No invita a reintentar.");
            CollectionAssert.Contains(anuncios, f.Aviso.text);
        }

        [Test, Combinatorial]
        public void Correo_SinConexionODesconocido_MensajeYReintento(
            [Values(PasoCuenta.Crear, PasoCuenta.Entrar, PasoCuenta.Olvidada)] PasoCuenta paso,
            [Values(ErroresCuenta.SinConexion, ErroresCuenta.Desconocido)] string error)
        {
            var f = AbrirFormulario(paso, "ana@example.com", Clave8);
            ContestarFalloCorreo(error);
            google.ResponderContrasena = (_, __) => Task.FromResult(Resultado<bool>.Fallo(error));
            f.Enviar();
            string texto = error == ErroresCuenta.SinConexion ? "Necesitas conexión para entrar."
                : paso == PasoCuenta.Crear ? "No se pudo crear la cuenta. Inténtalo de nuevo."
                : paso == PasoCuenta.Entrar ? "No se pudo iniciar sesión. Inténtalo de nuevo."
                : "No se pudo enviar el correo. Inténtalo de nuevo.";
            Assert.AreEqual(texto, f.Aviso.text);
            Assert.IsTrue(SeVe(f.Aviso));
            Assert.AreEqual("Reintentar", f.Principal.text);
            CollectionAssert.Contains(anuncios, texto);
            google.ResponderCorreo = (c, _, __) => Task.FromResult(Ok(CuentasFalsas.Ana));
            google.ResponderContrasena = (_, __) => Task.FromResult(Resultado<bool>.Exito(true));
            f.Enviar();
            Assert.AreEqual(2, google.Peticiones);
            if (paso == PasoCuenta.Olvidada) Assert.IsTrue(cuentas.Enviado && cuentas.Error == null);
            else Assert.IsFalse(pantalla.CuentaAbierta);
        }

        [TestCase(PasoCuenta.Crear, "Creando la cuenta…")]
        [TestCase(PasoCuenta.Entrar, "Entrando…")]
        [TestCase(PasoCuenta.Olvidada, "Enviando…")]
        public void Correo_DobleToque_UnaSolaPeticion(PasoCuenta paso, string esperando)
        {
            var f = AbrirFormulario(paso, "ana@example.com", Clave8);
            var respuesta = ContestarCorreoDespues();
            var envio = new TaskCompletionSource<Resultado<bool>>();
            google.ResponderContrasena = (_, __) => envio.Task;
            f.Enviar();
            f.Enviar();
            Assert.AreEqual(1, google.Peticiones);
            Assert.IsTrue(cuentas.Entrando);
            Assert.AreEqual(esperando, f.Principal.text);
            Assert.AreEqual(1, anuncios.Count(a => a == esperando));
            f.Correo.SetValueWithoutNotify("mal");
            f.Enviar();
            Assert.IsNull(cuentas.Error, "Mientras se espera, ni un error.");
            respuesta.SetResult(Ok(CuentasFalsas.Ana));
            envio.SetResult(Resultado<bool>.Exito(true));
            Assert.AreEqual(1, google.Peticiones);
            Assert.AreEqual(paso == PasoCuenta.Olvidada, pantalla.CuentaAbierta);
        }

        [TestCase(PasoCuenta.Crear, true)]
        [TestCase(PasoCuenta.Entrar, true)]
        [TestCase(PasoCuenta.Crear, false)]
        [TestCase(PasoCuenta.Entrar, false)]
        public void Correo_AtrasOAhoraNoConLaPeticionEnCurso_CanceladaYSinSesion(PasoCuenta paso, bool atras)
        {
            Abrir();
            int hechas = 0;
            cuentas.ExigirCuenta(() => hechas++);
            var f = AbrirFormulario(paso, "ana@example.com", Clave8);
            var ct = default(CancellationToken);
            var respuesta = ContestarCorreoDespues(t => ct = t);
            f.Enviar();
            if (atras) Assert.IsTrue(pantalla.Atras());
            else cuentas.AhoraNo();
            Assert.IsTrue(ct.IsCancellationRequested);
            Assert.IsFalse(cuentas.Entrando);
            Assert.AreEqual(atras, pantalla.CuentaAbierta, "Atrás vuelve a la hoja.");
            respuesta.SetResult(Ok(CuentasFalsas.Ana));
            Assert.IsNull(cuentas.Usuario);
            Assert.AreEqual(0, hechas);
            Assert.IsFalse(pantalla.Toast.Visible);
            Assert.IsNull(cuentas.Error);
        }

        [Test]
        public void Correo_OlvidadaCanceladaConAtras_NoDiceNada()
        {
            var f = AbrirFormulario(PasoCuenta.Olvidada, "ana@example.com");
            var envio = new TaskCompletionSource<Resultado<bool>>();
            google.ResponderContrasena = (_, __) => envio.Task;
            f.Enviar();
            pantalla.Atras();
            envio.SetResult(Resultado<bool>.Exito(true));
            Assert.IsFalse(cuentas.Enviado);
            cuentas.IrA(PasoCuenta.Olvidada);
            Assert.IsFalse(SeVe(f.Aviso));
        }

        [TestCase(PasoCuenta.Crear)]
        [TestCase(PasoCuenta.Entrar)]
        [TestCase(PasoCuenta.Olvidada)]
        public void Correo_SinFirebase_MensajeYAtrasFunciona(PasoCuenta paso)
        {
            servidor.Estado = EstadoServidor.NoDisponible;
            var f = AbrirFormulario(paso, "ana@example.com", Clave8);
            f.Enviar();
            Assert.AreEqual(0, google.Peticiones);
            Assert.AreEqual("Ahora no se pueden usar las cuentas en este móvil. Puedes seguir sin cuenta.", f.Aviso.text);
            Assert.IsTrue(pantalla.Atras());
            Assert.AreEqual(PasoCuenta.Botones, cuentas.Paso);
            Assert.IsFalse(SeVe(pantalla.Cuenta.Aviso), "El error del formulario no pasa a la hoja.");
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.CuentaAbierta);
        }

        [Test]
        public void Correo_EsperaAlServidor()
        {
            servidor.Estado = EstadoServidor.Iniciando;
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", Clave8);
            f.Enviar();
            Assert.AreEqual(0, google.Peticiones);
            Assert.IsTrue(cuentas.Entrando);
            servidor.Poner(EstadoServidor.Listo);
            Assert.IsTrue(SpinWait.SpinUntil(() => cuentas.Usuario != null, 5000));
            Assert.AreEqual(1, google.Peticiones);
        }

        [Test]
        public void Correo_LaSesionGuardadaLlegaPidiendoElEnlace_SeCierraSinSaludar()
        {
            var f = AbrirFormulario(PasoCuenta.Olvidada, "ana@example.com");
            var envio = new TaskCompletionSource<Resultado<bool>>();
            google.ResponderContrasena = (_, __) => envio.Task;
            f.Enviar();
            google.Poner(CuentasFalsas.Ana);
            envio.SetResult(Resultado<bool>.Exito(true));
            Assert.IsFalse(pantalla.CuentaAbierta);
            Assert.IsFalse(pantalla.Toast.Visible);
            Assert.AreEqual(PasoCuenta.Botones, cuentas.Paso);
        }

        [Test]
        public void Correo_DeCrearAEntrarYVuelta_ElCorreoSeConservaYLaContrasenaNo()
        {
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", Clave8);
            f.AlternarVer();
            Assert.AreEqual(("Crear cuenta", "Crear cuenta", "Ya tengo cuenta"), (f.Titulo.text, f.Principal.text, f.Secundario.text));
            Tocar(f.Secundario, f.Secundario.clickable);
            Assert.AreEqual(PasoCuenta.Entrar, cuentas.Paso);
            Assert.AreEqual(("ana@example.com", ""), (f.Correo.value, f.Contrasena.value));
            Assert.IsTrue(f.Contrasena.isPasswordField, "Otra vez oculta.");
            Assert.AreEqual(("Entrar", "Entrar", "Crear cuenta"), (f.Titulo.text, f.Principal.text, f.Secundario.text));
            Assert.IsTrue(SeVe(f.Olvidada) && !SeVe(f.Ayuda));
            f.Contrasena.SetValueWithoutNotify(Clave8);
            Tocar(f.Secundario, f.Secundario.clickable);
            Assert.AreEqual(("ana@example.com", ""), (f.Correo.value, f.Contrasena.value));
        }

        [TestCase(PasoCuenta.Crear)]
        [TestCase(PasoCuenta.Entrar)]
        [TestCase(PasoCuenta.Olvidada)]
        public void Correo_AtrasVuelveALaHoja_ElCorreoSigueYLaContrasenaNo(PasoCuenta paso)
        {
            var f = AbrirFormulario(paso, "ana@example.com", Clave8);
            Assert.IsTrue(pantalla.Cuenta.ClassListContains(HojaCuenta.ClaseFormulario));
            Assert.IsTrue(SeVe(f) && !SeVe(pantalla.Cuenta.Tarjeta));
            Tocar(f.BotonAtras, f.ToqueAtras);
            Assert.IsTrue(pantalla.CuentaAbierta);
            Assert.IsTrue(!SeVe(f) && SeVe(pantalla.Cuenta.Tarjeta));
            Assert.IsFalse(pantalla.Cuenta.ClassListContains(HojaCuenta.ClaseFormulario));
            Assert.AreEqual("", f.Contrasena.value);
            Assert.AreSame(pantalla.Cuenta.ConCorreo, pantalla.Accesibilidad.Enfocado);
            Tocar(pantalla.Cuenta.ConCorreo, pantalla.Cuenta.ConCorreo.clickable);
            Assert.AreEqual(PasoCuenta.Crear, cuentas.Paso, "Se empieza por crear la cuenta.");
            Assert.AreEqual("ana@example.com", Form.Correo.value);
            cuentas.AhoraNo();
            pantalla.TocarAvatar();
            Assert.AreEqual("", Form.Correo.value, "Cerrada del todo, nada se guarda.");
        }

        [Test]
        public void Correo_TrasLaBienvenida_TambienYAtrasVuelveALaPantallaCompleta()
        {
            Abrir(bienvenidaVista: false);
            TerminarBienvenida();
            pantalla.Cuenta.PulsarConCorreo();
            Assert.AreEqual(PasoCuenta.Crear, cuentas.Paso);
            pantalla.Atras();
            Assert.IsTrue(pantalla.Cuenta.Completa && SeVe(pantalla.Cuenta.Tarjeta));
        }

        [Test]
        public void Correo_ConGooglePidiendo_ConCorreoLoCancela()
        {
            Abrir();
            pantalla.TocarAvatar();
            var respuesta = ContestarDespues();
            pantalla.Cuenta.PulsarEntrar();
            pantalla.Cuenta.PulsarConCorreo();
            Assert.IsFalse(cuentas.Entrando);
            respuesta.SetResult(Ok(CuentasFalsas.Ana));
            Assert.IsNull(cuentas.Usuario);
            Assert.AreEqual(PasoCuenta.Crear, cuentas.Paso);
        }

        [Test]
        public void Correo_TrasUnErrorDeGoogle_ConCorreoSigueAhi()
        {
            Abrir();
            pantalla.TocarAvatar();
            ContestarFallo(ErroresCuenta.NoDisponible);
            pantalla.Cuenta.PulsarEntrar();
            Assert.IsTrue(SeVe(pantalla.Cuenta.Reintentar));
            Assert.IsTrue(pantalla.Cuenta.ConCorreo.ClassListContains("explicacion-ubicacion__boton--secundario"));
            pantalla.Cuenta.PulsarConCorreo();
            Assert.IsNull(cuentas.Error);
            Assert.IsFalse(SeVe(Form.Aviso));
            pantalla.Atras();
            Assert.IsTrue(SeVe(pantalla.Cuenta.Google) && !SeVe(pantalla.Cuenta.Aviso));
        }

        [Test]
        public void Correo_FueraDeSuPasoOCerrada_NoPideNada()
        {
            cuentas.CrearConCorreo("ana@example.com", Clave8).Wait();
            cuentas.IrA(PasoCuenta.Crear);
            Assert.AreEqual(PasoCuenta.Botones, cuentas.Paso);
            cuentas.Abrir();
            cuentas.CrearConCorreo("ana@", "").Wait();
            cuentas.IrA(PasoCuenta.Crear);
            cuentas.EntrarConCorreo("ana@example.com", Clave8).Wait();
            cuentas.EnviarContrasena("ana@example.com").Wait();
            cuentas.EntrarConGoogle().Wait();
            Assert.AreEqual((0, 0), (google.Peticiones, cuentas.Fallos));
        }

        [Test]
        public void Correo_LaContrasenaNoSeGuardaEnElServicio()
        {
            const string Secreta = "secreta-123";
            cuentas.Abrir();
            cuentas.IrA(PasoCuenta.Crear);
            ContestarFalloCorreo(ErroresCuenta.Desconocido);
            cuentas.CrearConCorreo("ana@example.com", Secreta).Wait();
            foreach (var campo in typeof(ServicioCuentas).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                Assert.AreNotEqual(Secreta, campo.GetValue(cuentas), campo.Name);
        }

        // ───────────────────────── Formulario ─────────────────────────

        [Test]
        public void Correo_VerYOcultar()
        {
            var f = AbrirFormulario();
            Assert.IsTrue(f.Contrasena.isPasswordField);
            Assert.AreEqual('•', f.Contrasena.maskChar);
            Tocar(f.Ver, f.Ver.clickable);
            Assert.IsFalse(f.Contrasena.isPasswordField);
            Assert.AreEqual("Ocultar", f.Ver.text);
            CollectionAssert.Contains(Recorrido(), "Ocultar la contraseña");
            Tocar(f.Ver, f.Ver.clickable);
            Assert.IsTrue(f.Contrasena.isPasswordField);
            Assert.AreEqual("Ver", f.Ver.text);
            Assert.AreEqual(TouchScreenKeyboardType.EmailAddress, f.Correo.keyboardType);
            Assert.AreNotEqual(f.Correo.keyboardType, f.Contrasena.keyboardType, "Con el mismo, Unity reutilizaría el teclado de uno en el otro.");
            Assert.IsFalse(f.Correo.autoCorrection || f.Correo.isPasswordField);
        }

        [Test]
        public void Correo_IntroEnElUltimoCampoEnvia()
        {
            var f = AbrirFormulario(PasoCuenta.Olvidada, "ana@example.com", "");
            f.Intro(f.Correo);
            Assert.IsTrue(cuentas.Enviado, "En la olvidada, el correo es el último.");
            cuentas.IrA(PasoCuenta.Entrar);
            f.Contrasena.SetValueWithoutNotify("x");
            f.Intro(f.Correo);
            Assert.AreEqual(1, google.Peticiones, "Del correo pasa a la contraseña.");
            f.Intro(f.Contrasena);
            Assert.AreEqual(2, google.Peticiones);
            Assert.IsFalse(pantalla.CuentaAbierta);
        }

        [Test]
        public void Correo_ConElTeclado_LosBotonesSubenYAlSalirVuelven()
        {
            var f = AbrirFormulario();
            Assert.IsFalse(f.ConTeclado);
            f.PonerTeclado(true);
            Assert.IsTrue(f.ClassListContains(FormularioCuenta.ClaseTeclado));
            cuentas.IrA(PasoCuenta.Entrar);
            Assert.IsFalse(f.ConTeclado || f.ClassListContains(FormularioCuenta.ClaseTeclado), "Al cambiar de paso se cierra.");
            StringAssert.Contains("flex-grow: 0;", Regla(@"\.cuenta-form--teclado \.cuenta-form__hueco"));
            StringAssert.Contains("flex-grow: 1;", Regla(@"\.cuenta-form__hueco"));
            StringAssert.Contains("min-height: 100%;", Regla(@"\.cuenta-form__pagina \.unity-scroll-view__content-container"));
            Assert.IsTrue(f.Pagina.Contains(f.Principal) && f.Pagina.Contains(f.Secundario), "Se desplazan con los campos.");
            Assert.Greater(f.Pagina.IndexOf(f.Principal), f.Pagina.IndexOf(f.Hueco));
            Assert.AreEqual(PickingMode.Ignore, f.Hueco.pickingMode);
        }

        // Campo en 100, botón hasta 300
        [TestCase(400f, 0f)]
        [TestCase(250f, 50f)]
        [TestCase(150f, 100f)]
        public void Correo_LoQueSeDesplazaConElTeclado(float visto, float esperado) =>
            Assert.AreEqual(esperado, FormularioCuenta.Desplazamiento(100f, 300f, visto));

        [Test]
        public void Correo_ZonaSegura()
        {
            var f = AbrirFormulario();
            pantalla.Cuenta.PonerZonaSegura(1f, 30f, 2f, 20f);
            Assert.AreEqual(20f, f.style.paddingBottom.value.value);
            Assert.AreEqual(30f, pantalla.Cuenta.style.paddingTop.value.value);
        }

        // ───────────────────────── TalkBack ─────────────────────────

        [Test]
        public void Correo_TalkBack_RecorridoYFocoAlTitulo()
        {
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", Clave8);
            Assert.AreSame(f.Titulo, pantalla.Accesibilidad.Enfocado);
            CollectionAssert.AreEqual(new[] { "Crear cuenta", "Correo", "Contraseña", "Ver la contraseña", "8 caracteres como mínimo.",
                "Crear cuenta", "Ya tengo cuenta", "Atrás" }, Recorrido());
            f.PulsarSecundario();
            Assert.AreSame(f.Titulo, pantalla.Accesibilidad.Enfocado);
            CollectionAssert.AreEqual(new[] { "Entrar", "Correo", "Contraseña", "Ver la contraseña", "He olvidado la contraseña",
                "Entrar", "Crear cuenta", "Atrás" }, Recorrido());
            ContestarFalloCorreo(ErroresCuenta.NoCoinciden);
            f.Contrasena.SetValueWithoutNotify("x");
            f.Enviar();
            CollectionAssert.AreEqual(new[] { "Entrar", "Correo", "Contraseña", "Ver la contraseña", "El correo o la contraseña no coinciden.",
                "He olvidado la contraseña", "Entrar", "Crear cuenta", "Atrás" }, Recorrido());
            cuentas.IrA(PasoCuenta.Olvidada);
            CollectionAssert.AreEqual(new[] { "Nueva contraseña", T(ClavesTexto.CuentaOlvidadaPara), "Correo", "Enviar enlace", "Volver a entrar", "Atrás" }, Recorrido());
            f.Enviar();
            CollectionAssert.AreEqual(new[] { "Nueva contraseña", "Correo", T(ClavesTexto.CuentaEnviado), "Enviar otro", "Volver a entrar", "Atrás" }, Recorrido());
            pantalla.Atras();
            CollectionAssert.AreEqual(new[] { "Entra en PriceMap", T(ClavesTexto.CuentaPara), "Continuar con Google", "Con correo", "Ahora no" }, Recorrido());
        }

        [Test]
        public void Correo_TalkBack_NodosValoresYDobleToque()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", Clave8);
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            Assert.AreEqual(AccessibilityRole.Button, Nodo(f.Correo).role);
            foreach (var n in AssistiveSupport.activeHierarchy.rootNodes)
                StringAssert.DoesNotContain(Clave8, n.label + n.value, "La contraseña nunca se dice.");
            Activar(f.Ver);
            Assert.AreEqual("Ocultar la contraseña", Nodo(f.Ver).label);
            var respuesta = ContestarCorreoDespues();
            Activar(f.Principal);
            Assert.AreEqual("Creando la cuenta…", Nodo(f.Principal).label);
            respuesta.SetResult(Resultado<UsuarioCuenta>.Fallo(ErroresCuenta.SinConexion));
            Assert.AreEqual("Reintentar", Nodo(f.Principal).label);
            Activar(f.Secundario);
            Assert.AreEqual(PasoCuenta.Entrar, cuentas.Paso);
            Activar(f.BotonAtras);
            Activar(pantalla.Cuenta.ConCorreo);
            Assert.AreEqual(PasoCuenta.Crear, cuentas.Paso);
        }

        [Test]
        public void Correo_TalkBack_ElFocoDesplazaElFormulario()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@", Clave8);
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            f.Enviar();
            LanzarFoco(f.Principal);
            Assert.AreSame(f.Principal, f.UltimoDesplazado);
            LanzarFoco(f.ErrorCorreo);
            Assert.AreSame(f.ErrorCorreo, f.UltimoDesplazado);
        }

        void LanzarFoco(VisualElement elemento)
        {
            var nodo = Nodo(elemento);
            var campo = typeof(AccessibilityNode).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(c =>
                typeof(Action<AccessibilityNode, bool>).IsAssignableFrom(c.FieldType) && c.Name.IndexOf("focusChanged", StringComparison.OrdinalIgnoreCase) >= 0);
            if (campo == null) Assert.Inconclusive("Esta versión de Unity no deja leer el evento focusChanged del nodo.");
            ((Action<AccessibilityNode, bool>)campo.GetValue(nodo))(nodo, true);
        }

        [Test]
        public void Correo_TalkBack_ElValorDelCampoSigueLoEscrito()
        {
            AccesibilidadPantalla.LectorForzado = () => true;
            var f = FormularioConPanel(PasoCuenta.Crear);
            if (AssistiveSupport.activeHierarchy == null) Assert.Ignore("En este editor no se puede activar una jerarquía de accesibilidad.");
            f.Correo.value = "ana@example.com";
            Assert.AreEqual("ana@example.com", Nodo(f.Correo).value);
        }

        // ───────────────────────── Con un panel de verdad ─────────────────────────

        FormularioCuenta FormularioConPanel(PasoCuenta paso)
        {
            ComponerConPanel();
            pantalla.TocarAvatar();
            cuentas.IrA(paso);
            Assert.IsNotNull(Form.panel);
            Disponer(Form);
            return Form;
        }

        static void Disponer(VisualElement e) => e.panel.GetType()
            .GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(e.panel, null);

        static VisualElement Dentro(TextField campo) => campo.Q(className: TextField.inputUssClassName).Q<TextElement>();

        static void Mandar(VisualElement e, EventBase evento)
        {
            using (evento)
            {
                evento.target = e;
                e.SendEvent(evento);
            }
        }

        static void Puntero(VisualElement e, EventType tipo)
        {
            var sistema = new Event { type = tipo, mousePosition = e.worldBound.center, button = 0, clickCount = 1, pointerType = UnityEngine.PointerType.Touch };
            Mandar(e, tipo == EventType.MouseDown ? PointerDownEvent.GetPooled(sistema) : (EventBase)PointerUpEvent.GetPooled(sistema));
        }

        [Test]
        public void ConPanel_ElFocoDeLosCamposMueveElTeclado()
        {
            var f = FormularioConPanel(PasoCuenta.Crear);
            foreach (var campo in new[] { f.Correo, f.Contrasena })
            {
                campo.Focus();
                Assert.IsTrue(f.ConTeclado && f.ClassListContains(FormularioCuenta.ClaseTeclado), campo.name);
                Assert.IsTrue(campo.parent.ClassListContains(FormularioCuenta.ClaseFoco), campo.name);
                Assert.Greater(f.layout.height, 0f);
                Assert.AreEqual(f.layout.height * FormularioCuenta.FraccionTeclado, f.Pagina.contentContainer.style.paddingBottom.value.value, 0.01f);
                campo.Blur();
                Assert.IsFalse(campo.parent.ClassListContains(FormularioCuenta.ClaseFoco), campo.name);
                Assert.IsTrue(f.ConTeclado, "Se espera un momento: el botón no se va de debajo del dedo.");
                f.QuitarTeclado();
                Assert.IsFalse(f.ConTeclado || f.ClassListContains(FormularioCuenta.ClaseTeclado), campo.name);
                Assert.AreEqual(StyleKeyword.Null, f.Pagina.contentContainer.style.paddingBottom.keyword);
            }
        }

        [Test]
        public void ConPanel_IntroEnElUltimoCampoEnviaUnaVezYEnElCorreoPasaALaContrasena()
        {
            var f = FormularioConPanel(PasoCuenta.Entrar);
            ContestarFalloCorreo(ErroresCuenta.NoCoinciden);
            f.Correo.SetValueWithoutNotify("ana@example.com");
            f.Contrasena.SetValueWithoutNotify("x");
            f.Correo.Focus();
            Mandar(Dentro(f.Correo), KeyDownEvent.GetPooled('\0', KeyCode.Return, EventModifiers.None));
            Assert.AreEqual(0, google.Peticiones);
            Assert.AreSame(f.Contrasena, f.focusController.focusedElement);
            Mandar(Dentro(f.Contrasena), KeyDownEvent.GetPooled('\0', KeyCode.Return, EventModifiers.None));
            Assert.AreEqual(1, google.Peticiones);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ConPanel_ElDedoSeSueltaAunqueLoCaptureUnBotonOLaPagina(bool pagina)
        {
            var f = FormularioConPanel(PasoCuenta.Crear);
            VisualElement quien = pagina ? (VisualElement)f.Pagina : f.Principal;
            Puntero(f.Principal, EventType.MouseDown);
            Assert.IsTrue(f.DedoAbajo);
            quien.CapturePointer(PointerId.touchPointerIdBase);
            quien.ReleasePointer(PointerId.touchPointerIdBase);
            f.panel.visualTree.SendEvent(PointerMoveEvent.GetPooled()); // lo que queda en la cola
            Assert.IsFalse(f.DedoAbajo, "Sin PointerUp.");
        }

        [Test]
        public void ConPanel_ElIntroAplazadoSoloValeEnSuPasoYAbierto()
        {
            var f = FormularioConPanel(PasoCuenta.Crear);
            f.Correo.SetValueWithoutNotify("ana@example.com");
            f.Contrasena.SetValueWithoutNotify(Clave8);
            f.IntroSiSigue(f.Contrasena, PasoCuenta.Entrar);
            Assert.AreEqual((0, 0), (google.Peticiones, cuentas.Fallos), "Otro paso.");
            f.IntroSiSigue(f.Contrasena, PasoCuenta.Crear);
            Assert.AreEqual(1, google.Peticiones);
            google.Poner(null);
            pantalla.TocarAvatar();
            cuentas.IrA(PasoCuenta.Crear);
            f.IntroSiSigue(f.Contrasena, PasoCuenta.Crear);
            Assert.AreEqual((1, 0), (google.Peticiones, cuentas.Fallos), "El de un formulario ya cerrado.");
        }

        [Test]
        public void Correo_AlQuitarElTeclado_SeVaAlError()
        {
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", "corta");
            f.PonerTeclado(true);
            f.Enviar();
            Assert.IsNull(f.UltimoDesplazado, "Con el teclado manda el campo.");
            f.PonerTeclado(false);
            Assert.AreSame(f.ErrorContrasena, f.UltimoDesplazado);
        }

        [Test]
        public void Correo_AlDestruirLaPantalla_SinContrasenaNiTeclado()
        {
            var f = AbrirFormulario(PasoCuenta.Crear, "ana@example.com", Clave8);
            f.PonerTeclado(true);
            Destruir();
            Assert.AreEqual("", f.Contrasena.value);
            Assert.IsFalse(f.ConTeclado);
        }

        // ───────────────────────── Aspecto ─────────────────────────

        [TestCase(false)]
        [TestCase(true)]
        public void Correo_CumpleElContraste(bool oscuro)
        {
            var c = PruebasContraste.Tema(oscuro);
            float Contraste(string a, string b) => PruebasContraste.Contraste(c[a], c[b]);
            Assert.GreaterOrEqual(Contraste("error-texto", "bg"), 4.5f, "error sobre el fondo");
            Assert.GreaterOrEqual(Contraste("error-texto", "surface"), 3f, "borde del campo con error");
            Assert.GreaterOrEqual(Contraste("control-edge", "surface"), 3f, "borde del campo, por dentro");
            Assert.GreaterOrEqual(Contraste("control-edge", "bg"), 3f, "borde del campo, por fuera");
            Assert.GreaterOrEqual(Contraste("focus", "surface"), 3f, "borde del campo enfocado");
            Assert.GreaterOrEqual(Contraste("accent-text", "surface"), 4.5f, "«Ver»");
            Assert.GreaterOrEqual(Contraste("text", "surface"), 4.5f, "lo escrito");
            StringAssert.Contains("color: var(--color-error-texto);", Regla(@"\.cuenta-form__error"));
            StringAssert.Contains("border-color: var(--color-error-texto);", Regla(@"\.cuenta-form__caja--mal"));
            StringAssert.Contains("border-color: var(--color-focus);", Regla(@"\.cuenta-form__caja--mal\.cuenta-form__caja--foco"), "Con error, el foco se ve.");
        }

        [Test]
        public void Correo_Al200YConElDedo()
        {
            foreach (var l in new[] { @"\.cuenta-form__para", @"\.cuenta-form__titulo" }) StringAssert.Contains("white-space: normal;", Regla(l));
            StringAssert.Contains("min-height: var(--bienvenida-boton-alto);", Regla(@"\.cuenta-form__caja"));
            StringAssert.Contains("min-width: var(--zona-tactil-min);", Regla(@"\.bienvenida__enlace\.cuenta-form__ver"));
            StringAssert.Contains("background-color: var(--color-bg);", Regla(@"\.cuenta--formulario"));
            var f = AbrirFormulario();
            foreach (var b in f.Query<Button>().ToList())
                Assert.IsTrue(b.ClassListContains("bienvenida__enlace") || b.ClassListContains("cuenta__boton"), b.name + ": 48 dp o más.");
        }

        // ───────────────────────── Firebase Auth ─────────────────────────

        [Test]
        public void FirebaseCorreo_CadaPeticionConLoSuyo()
        {
            var f = Firebase();
            Assert.IsTrue(f.CrearConCorreo("ana@example.com", "p1", CancellationToken.None).Result.Ok);
            Assert.AreEqual(("crear", "ana@example.com", "p1"), auth.conCorreo);
            Assert.AreSame(CuentasFalsas.Ana, f.Usuario);
            Assert.IsTrue(f.EntrarConCorreo("eva@example.com", "p2", CancellationToken.None).Result.Ok);
            Assert.AreEqual(("entrar", "eva@example.com", "p2"), auth.conCorreo);
            Assert.IsTrue(f.EnviarContrasena("eva@example.com", CancellationToken.None).Result.Ok);
            Assert.AreEqual("enviar", auth.conCorreo.que);
            Assert.AreEqual(0, selector.peticiones);
        }

        static Task<string> Error(CuentasFirebase f, PasoCuenta paso, CancellationToken ct = default)
        {
            async Task<string> De<T>(Task<Resultado<T>> t) => (await t).Error;
            return paso == PasoCuenta.Crear ? De(f.CrearConCorreo("a@b.es", Clave8, ct))
                : paso == PasoCuenta.Entrar ? De(f.EntrarConCorreo("a@b.es", Clave8, ct))
                : De(f.EnviarContrasena("a@b.es", ct));
        }

        [Test, Combinatorial]
        public void FirebaseCorreo_SinServidorOSinConexion_SinPedirNada(
            [Values(PasoCuenta.Crear, PasoCuenta.Entrar, PasoCuenta.Olvidada)] PasoCuenta paso, [Values] bool sinServidor)
        {
            if (sinServidor) servidor.Estado = EstadoServidor.NoDisponible;
            var f = Firebase();
            conexion = sinServidor;
            Assert.AreEqual(sinServidor ? ErroresCuenta.NoDisponible : ErroresCuenta.SinConexion, Error(f, paso).Result);
            Assert.IsNull(auth.conCorreo.que);
        }

        [TestCase("WrongPassword", ErroresCuenta.NoCoinciden)]
        [TestCase("UserNotFound", ErroresCuenta.NoCoinciden)]
        [TestCase("InvalidCredential", ErroresCuenta.NoCoinciden)]
        [TestCase("EmailAlreadyInUse", ErroresCuenta.CorreoUsado)]
        [TestCase("InvalidEmail", ErroresCuenta.CorreoNoValido)]
        [TestCase("MissingEmail", ErroresCuenta.CorreoNoValido)]
        [TestCase("WeakPassword", ErroresCuenta.ContrasenaDebil)]
        [TestCase("TooManyRequests", ErroresCuenta.DemasiadosIntentos)]
        [TestCase("QuotaExceeded", ErroresCuenta.DemasiadosIntentos)]
        [TestCase("NetworkRequestFailed", ErroresCuenta.SinConexion)]
        [TestCase("OperationNotAllowed", ErroresCuenta.Desconocido)]
        public void FirebaseCorreo_CodigosYSinDatosEnElRegistro(string codigo, string propio)
        {
            Assert.IsTrue(Enum.IsDefined(Tipo("Firebase.Auth", "Firebase.Auth.AuthError"), codigo), "Es un código del SDK.");
            var f = Firebase();
            auth.alEntrar = _ => Task.FromException<UsuarioCuenta>(new FalloAuth(codigo, new Exception("a@b.es " + Clave8)));
            foreach (var paso in new[] { PasoCuenta.Crear, PasoCuenta.Entrar })
            {
                LogAssert.Expect(LogType.Warning, $"PriceMap: no se pudo entrar en la cuenta ({propio}, {codigo}).");
                Assert.AreEqual(propio, Error(f, paso).Result);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirebaseCorreo_OlvidadaSinCuenta_CuentaComoEnviado()
        {
            var f = Firebase();
            auth.alEnviar = () => Task.FromException(new FalloAuth("UserNotFound"));
            Assert.AreEqual("", Error(f, PasoCuenta.Olvidada).Result);
            LogAssert.NoUnexpectedReceived();
            auth.alEnviar = () => Task.FromException(new FalloAuth("TooManyRequests"));
            LogAssert.Expect(LogType.Warning, "PriceMap: no se pudo enviar el correo de la contraseña (cuenta-demasiados-intentos, TooManyRequests).");
            Assert.AreEqual(ErroresCuenta.DemasiadosIntentos, Error(f, PasoCuenta.Olvidada).Result);
        }

        [TestCase(PasoCuenta.Crear)]
        [TestCase(PasoCuenta.Entrar)]
        public void FirebaseCorreo_CanceladaMientrasContesta_SaleYNoSePublica(PasoCuenta paso)
        {
            var f = Firebase();
            int avisos = 0;
            f.Cambiado += () => avisos++;
            var lenta = new TaskCompletionSource<UsuarioCuenta>();
            auth.alEntrar = _ => lenta.Task;
            using (var cts = new CancellationTokenSource())
            {
                var primera = Error(f, paso, cts.Token);
                cts.Cancel();
                auth.alEntrar = null;
                auth.EntrarYAvisar();
                var segunda = f.EntrarConCorreo("eva@example.com", "p2", CancellationToken.None);
                Assert.AreEqual(1, auth.entradas, "De una en una.");
                lenta.SetResult(CuentasFalsas.Ana);
                Assert.AreEqual(ErroresCuenta.Cancelado, primera.Result);
                Assert.IsTrue(segunda.Result.Ok);
                Assert.AreEqual((2, 1, 1), (auth.entradas, auth.salidas, avisos));
            }
        }

        [Test]
        public void FirebaseCorreo_ElEnlaceEsperaALaCanceladaYNoPublicaSuSesion()
        {
            var f = Firebase();
            var lenta = new TaskCompletionSource<UsuarioCuenta>();
            auth.alEntrar = _ => lenta.Task;
            using (var cts = new CancellationTokenSource())
            {
                var primera = Error(f, PasoCuenta.Crear, cts.Token);
                cts.Cancel();
                var enviar = Error(f, PasoCuenta.Olvidada);
                Assert.AreEqual("crear", auth.conCorreo.que);
                auth.EntrarYAvisar();
                lenta.SetResult(CuentasFalsas.Ana);
                Assert.AreEqual(ErroresCuenta.Cancelado, primera.Result);
                Assert.AreEqual("", enviar.Result);
                Assert.AreEqual("enviar", auth.conCorreo.que);
                Assert.IsNull(f.Usuario);
            }
        }

        [Test]
        public void FirebaseCorreo_ElEnlaceCancelado_NoCierraNingunaSesion()
        {
            var f = Firebase();
            var lento = new TaskCompletionSource<bool>();
            auth.alEnviar = () => lento.Task;
            using (var cts = new CancellationTokenSource())
            {
                var enviar = Error(f, PasoCuenta.Olvidada, cts.Token);
                cts.Cancel();
                lento.SetResult(true);
                Assert.AreEqual(ErroresCuenta.Cancelado, enviar.Result);
                Assert.AreEqual(0, auth.salidas);
            }
        }

        [TestCase("InvalidCredential")]
        [TestCase("TooManyRequests")]
        public void FirebaseGoogle_LosCodigosDelCorreoSonElFalloGenerico(string codigo)
        {
            var f = Firebase();
            auth.alEntrar = _ => Task.FromException<UsuarioCuenta>(new FalloAuth(codigo));
            LogAssert.Expect(LogType.Warning, $"PriceMap: no se pudo entrar en la cuenta (cuenta-desconocido, {codigo}).");
            Assert.AreEqual(ErroresCuenta.Desconocido, f.EntrarConGoogle(CancellationToken.None).Result.Error);
        }

        [Test]
        public void FirebaseCorreo_ElCorreoDeFirebaseEnEspanol()
        {
            string codigo = File.ReadAllText("Assets/Scripts/Adaptadores/Firebase/AccesoAuth.cs");
            int idioma = codigo.IndexOf("LanguageCode = \"es\"", StringComparison.Ordinal);
            Assert.Greater(idioma, 0);
            Assert.Greater(codigo.IndexOf("SendPasswordResetEmailAsync(correo)", StringComparison.Ordinal), idioma);
            StringAssert.Contains("CreateUserWithEmailAndPasswordAsync(correo, contrasena)", codigo);
            StringAssert.Contains("SignInWithEmailAndPasswordAsync(correo, contrasena)", codigo);
            StringAssert.DoesNotContain("Debug.", codigo, "Ni el correo ni la contraseña van al registro.");
        }
    }
}
