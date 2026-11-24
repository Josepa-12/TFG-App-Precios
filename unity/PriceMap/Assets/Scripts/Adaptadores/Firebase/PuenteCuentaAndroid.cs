#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>El plugin de verdad, por JNI. Solo se compila en Android.</summary>
    internal sealed class PuenteCuentaAndroid : IPuenteCuenta
    {
        const string ClasePuente = "com.pricemap.cuenta.PuenteCuenta";
        AndroidJavaClass puente;
        AndroidJavaClass Puente => puente ??= new AndroidJavaClass(ClasePuente);

        public int Pedir() => Puente.CallStatic<int>("pedir");
        public int Estado(int id) => Puente.CallStatic<int>("estado", id);
        public string Token(int id) => Puente.CallStatic<string>("token", id);
        public string Motivo(int id) => Puente.CallStatic<string>("motivo", id);
        public void Cancelar(int id) => Puente.CallStatic("cancelar", id);
        public void Liberar(int id) => Puente.CallStatic("liberar", id);
    }
}
#endif
