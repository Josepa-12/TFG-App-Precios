using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PriceMap.Aplicacion;
using UnityEngine;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>
    /// Datos en Cloud Firestore (historia 3.2). Se crea y se usa en el hilo principal, y responde en él. Los registros solo
    /// llevan el código del error: las rutas tienen el identificador del usuario.
    /// </summary>
    public sealed class AlmacenFirestore : IAlmacen
    {
        readonly IServidor servidor;
        readonly IAccesoFirestore acceso;
        readonly SynchronizationContext hilo = SynchronizationContext.Current;

        public AlmacenFirestore(IServidor servidor) : this(servidor, new AccesoFirestore()) { }

        internal AlmacenFirestore(IServidor servidor, IAccesoFirestore acceso)
        {
            this.servidor = servidor ?? throw new ArgumentNullException(nameof(servidor));
            this.acceso = acceso ?? throw new ArgumentNullException(nameof(acceso));
        }

        public async Task<Resultado<Documento>> Leer(string ruta)
        {
            if (Impedimento(ruta) is string error) return Resultado<Documento>.Fallo(error);
            try { return Resultado<Documento>.Exito(await acceso.Leer(ruta)); }
            catch (Exception e) { return Resultado<Documento>.Fallo(Codigo(e, "leer")); }
        }

        public async Task<Resultado<bool>> Escribir(string ruta, IReadOnlyDictionary<string, object> campos)
        {
            if (Impedimento(ruta) is string error) return Resultado<bool>.Fallo(error);
            if (campos == null || campos.Count == 0) return Resultado<bool>.Fallo(ErroresServidor.NoValido);
            try
            {
                await acceso.Escribir(ruta, campos);
                return Resultado<bool>.Exito(true);
            }
            catch (Exception e) { return Resultado<bool>.Fallo(Codigo(e, "escribir")); }
        }

        public IDisposable Escuchar(string ruta, Action<Resultado<Documento>> alCambiar)
        {
            if (alCambiar == null) throw new ArgumentNullException(nameof(alCambiar));
            var escucha = new Escucha();
            void Avisar(Resultado<Documento> resultado) => EnElHilo(() =>
            {
                if (escucha.Parada) return;
                if (!resultado.Ok) escucha.Dispose();
                alCambiar(resultado);
            });

            if (Impedimento(ruta) is string error)
            {
                Avisar(Resultado<Documento>.Fallo(error));
                return escucha;
            }
            try
            {
                escucha.Registro = acceso.Escuchar(ruta,
                    documento => Avisar(Resultado<Documento>.Exito(documento)),
                    e => Avisar(Resultado<Documento>.Fallo(Codigo(e, "escuchar"))));
            }
            catch (Exception e) { Avisar(Resultado<Documento>.Fallo(Codigo(e, "escuchar"))); }
            return escucha;
        }

        // Por qué no se puede ni intentar (null: se puede)
        string Impedimento(string ruta)
        {
            if (servidor.Estado != EstadoServidor.Listo) return ErroresServidor.NoDisponible;
            return EsDeDocumento(ruta) ? null : ErroresServidor.NoValido;
        }

        /// <summary>Las rutas de documento tienen trozos pares (colección/documento…) y ninguno vacío.</summary>
        internal static bool EsDeDocumento(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return false;
            var trozos = ruta.Split('/');
            return trozos.Length % 2 == 0 && trozos.All(t => t.Trim().Length > 0);
        }

        static string Codigo(Exception e, string accion)
        {
            string codigo = CodigosFirestore.De(e);
            Debug.LogWarning($"PriceMap: no se pudo {accion} en Firestore ({codigo}, {e.GetType().Name}).");
            return codigo;
        }

        void EnElHilo(Action accion)
        {
            if (hilo == null || SynchronizationContext.Current == hilo) accion();
            else hilo.Post(_ => accion(), null);
        }

        sealed class Escucha : IDisposable
        {
            IDisposable registro;

            public bool Parada { get; private set; }

            /// <summary>La de Firestore; si llega con la escucha ya parada (falló al empezar), se suelta.</summary>
            public IDisposable Registro
            {
                set
                {
                    if (Parada) value?.Dispose();
                    else registro = value;
                }
            }

            public void Dispose()
            {
                Parada = true;
                registro?.Dispose();
                registro = null;
            }
        }
    }
}
