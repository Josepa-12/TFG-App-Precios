using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>Firebase Auth de verdad. No se usa hasta que InicioFirebase está Listo.</summary>
    internal sealed class AccesoAuth : IAccesoAuth
    {
        public UsuarioCuenta Usuario => Leido(FirebaseAuth.DefaultInstance.CurrentUser);

        public void Escuchar(Action alCambiar) => FirebaseAuth.DefaultInstance.StateChanged += (_, __) => alCambiar();

        public async Task<UsuarioCuenta> Entrar(string idToken)
        {
            try
            {
                using var credencial = GoogleAuthProvider.GetCredential(idToken, null);
                return Leido(await FirebaseAuth.DefaultInstance.SignInWithCredentialAsync(credencial));
            }
            catch (Exception e) { throw Traducido(e); }
        }

        public async Task<UsuarioCuenta> CrearConCorreo(string correo, string contrasena)
        {
            try { return Leido((await FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(correo, contrasena)).User); }
            catch (Exception e) { throw Traducido(e); }
        }

        public async Task<UsuarioCuenta> EntrarConCorreo(string correo, string contrasena)
        {
            try { return Leido((await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(correo, contrasena)).User); }
            catch (Exception e) { throw Traducido(e); }
        }

        public async Task EnviarContrasena(string correo)
        {
            try
            {
                FirebaseAuth.DefaultInstance.LanguageCode = "es";
                await FirebaseAuth.DefaultInstance.SendPasswordResetEmailAsync(correo);
            }
            catch (Exception e) { throw Traducido(e); }
        }

        public void Salir() => FirebaseAuth.DefaultInstance.SignOut();

        static UsuarioCuenta Leido(FirebaseUser u) => u == null ? null : new UsuarioCuenta(u.UserId, u.DisplayName, u.Email);

        internal static Exception Traducido(Exception e)
        {
            var causas = e is AggregateException varias ? varias.Flatten().InnerExceptions : (IEnumerable<Exception>)new[] { e };
            var firebase = causas.OfType<FirebaseException>().FirstOrDefault();
            return firebase != null ? new FalloAuth(((AuthError)firebase.ErrorCode).ToString(), firebase) : e;
        }
    }
}
