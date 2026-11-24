namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Lo que SelectorGoogleAndroid necesita del plugin com.pricemap.cuenta.PuenteCuenta. El de verdad (PuenteCuentaAndroid) lo
    /// llama por JNI; las pruebas usan uno falso.
    /// </summary>
    internal interface IPuenteCuenta
    {
        int Pedir();
        int Estado(int id);
        string Token(int id);
        string Motivo(int id);
        void Cancelar(int id);
        void Liberar(int id);
    }
}
