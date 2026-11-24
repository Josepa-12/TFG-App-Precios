using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Firestore;
using PriceMap.Aplicacion;

namespace PriceMap.Adaptadores.Firebase
{
    /// <summary>Firestore de verdad. No se usa hasta que InicioFirebase está Listo.</summary>
    internal sealed class AccesoFirestore : IAccesoFirestore
    {
        static DocumentReference Referencia(string ruta) => FirebaseFirestore.DefaultInstance.Document(ruta);

        public async Task<Documento> Leer(string ruta)
        {
            try { return Leido(await Referencia(ruta).GetSnapshotAsync()); }
            catch (Exception e) { throw Traducido(e); }
        }

        public async Task Escribir(string ruta, IReadOnlyDictionary<string, object> campos)
        {
            try { await Referencia(ruta).SetAsync(ValoresFirestore.Mapa(campos), SetOptions.MergeAll); }
            catch (Exception e) { throw Traducido(e); }
        }

        public IDisposable Escuchar(string ruta, Action<Documento> alCambiar, Action<Exception> alFallar)
        {
            // Con los cambios de origen: así se sabe cuándo el servidor confirma lo que se leyó del móvil
            try
            {
                var registro = Referencia(ruta).Listen(MetadataChanges.Include, foto =>
                {
                    Documento leido;
                    try { leido = Leido(foto); }
                    catch (Exception e) { alFallar(Traducido(e)); return; }
                    alCambiar(leido);
                });
                registro.ListenerTask.ContinueWith(t => alFallar(Traducido(t.Exception)),
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                return registro;
            }
            catch (Exception e) { throw Traducido(e); }
        }

        static Documento Leido(DocumentSnapshot foto) => new Documento(foto.Exists,
            // La hora del servidor que aún no ha llegado, estimada (AD-12)
            foto.Exists ? ValoresFirestore.Campos(foto.ToDictionary(ServerTimestampBehavior.Estimate)) : null,
            foto.Metadata.IsFromCache || foto.Metadata.HasPendingWrites);

        internal static Exception Traducido(Exception e)
        {
            var causas = e is AggregateException varias ? varias.Flatten().InnerExceptions : (IEnumerable<Exception>)new[] { e };
            var firestore = causas.OfType<FirestoreException>().FirstOrDefault();
            return firestore != null ? new FalloFirestore((int)firestore.ErrorCode, firestore) : e;
        }
    }
}
