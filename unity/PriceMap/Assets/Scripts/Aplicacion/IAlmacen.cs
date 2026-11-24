using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PriceMap.Aplicacion
{
    /// <summary>
    /// Puerto de los datos en la nube (historia 3.2), con su copia en el móvil para cuando no hay conexión (AD-8). Las rutas
    /// son de documento («usuarios/abc»). Los fallos llegan con un código de ErroresServidor, nunca como excepción, y todo
    /// responde en el hilo principal. Si el servidor no está Listo, falla con NoDisponible.
    /// Valores de los campos: texto, número (int, long, float, double), bool, DateTime (UTC), HoraServidor, null, listas y
    /// mapas de lo anterior (en las listas, ni HoraServidor ni otra lista); al leer, los números llegan como long o double.
    /// </summary>
    public interface IAlmacen
    {
        /// <summary>Lee el documento: del servidor y, sin conexión, del móvil (sin conexión y sin copia, SinConexion).</summary>
        Task<Resultado<Documento>> Leer(string ruta);

        /// <summary>
        /// Escribe solo esos campos y deja los demás como están (crea el documento si no existe). Los mapas también se juntan:
        /// las claves que no van se quedan como estaban. Se guarda en el móvil y se
        /// ve al momento al leer y al escuchar; la tarea acaba cuando lo confirma el servidor, así que sin conexión no
        /// acaba hasta que vuelve: no hay que esperarla para seguir.
        /// </summary>
        Task<Resultado<bool>> Escribir(string ruta, IReadOnlyDictionary<string, object> campos);

        /// <summary>
        /// Avisa con el documento de ahora y con cada cambio, hasta que se llame a Dispose. Tras un fallo ya no avisa más.
        /// Un fallo inmediato puede avisar antes de que Escuchar devuelva.
        /// </summary>
        IDisposable Escuchar(string ruta, Action<Resultado<Documento>> alCambiar);
    }

    /// <summary>Un documento tal como está ahora (historia 3.2).</summary>
    public sealed class Documento
    {
        static readonly IReadOnlyDictionary<string, object> SinCampos = new Dictionary<string, object>();

        public Documento(bool existe, IReadOnlyDictionary<string, object> campos, bool delMovil)
        {
            Existe = existe;
            Campos = campos ?? SinCampos;
            DelMovil = delMovil;
        }

        /// <summary>Con DelMovil, que no exista solo quiere decir que no hay copia: en el servidor puede estar.</summary>
        public bool Existe { get; }
        /// <summary>Vacío si no existe.</summary>
        public IReadOnlyDictionary<string, object> Campos { get; }
        /// <summary>Viene de la copia del móvil o tiene cambios del móvil sin confirmar con el servidor.</summary>
        public bool DelMovil { get; }
    }

    /// <summary>Valor de un campo que pone el servidor: su hora al escribir («creado»).</summary>
    public sealed class HoraServidor
    {
        public static readonly HoraServidor Ahora = new HoraServidor();
        HoraServidor() { }
    }

    /// <summary>Códigos de error del servidor (Resultado.Error); la UI los traduce a texto.</summary>
    public static class ErroresServidor
    {
        /// <summary>El servidor no está Listo (IServidor).</summary>
        public const string NoDisponible = "servidor-no-disponible";
        public const string SinConexion = "sin-conexion";
        /// <summary>Las reglas no lo permiten o no hay sesión.</summary>
        public const string SinPermiso = "sin-permiso";
        /// <summary>Se ha agotado la cuota gratuita.</summary>
        public const string CuotaAgotada = "cuota-agotada";
        /// <summary>Ruta o campos que no se pueden usar.</summary>
        public const string NoValido = "no-valido";
        public const string NoEncontrado = "no-encontrado";
        public const string Desconocido = "desconocido";
    }
}
