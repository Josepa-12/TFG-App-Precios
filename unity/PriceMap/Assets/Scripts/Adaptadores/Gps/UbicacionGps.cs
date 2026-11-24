using System;
using System.Threading;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using UnityEngine;

namespace PriceMap.Adaptadores.Gps
{
    /// <summary>
    /// Lógica de IUbicacion sobre una IFuenteUbicacion: permiso, estados (SinPermiso, Denegado, Buscando, Activa,
    /// Apagada), lecturas del GPS y de la brújula. Solo enciende el GPS con permiso, con la ubicación del móvil
    /// encendida y con la app delante (NFR-3), salvo durante una ruta: entonces el servicio de ruta (IServicioRuta,
    /// AD-19) sigue con la app en segundo plano y sus posiciones se recogen en orden al volver. UbicacionUnity la
    /// crea con la fuente real y llama a Actualizar en cada fotograma; las pruebas la usan con fuentes falsas.
    /// </summary>
    internal class UbicacionGps : IUbicacion
    {
        /// <summary>Intent de los ajustes de la app (Aplicaciones &gt; PriceMap): permiso denegado.</summary>
        public const string AjustesApp = "android.settings.APPLICATION_DETAILS_SETTINGS";
        /// <summary>Intent de los ajustes de ubicación del móvil: ubicación apagada.</summary>
        public const string AjustesUbicacion = "android.settings.LOCATION_SOURCE_SETTINGS";
        /// <summary>Error de IniciarRuta sin permiso de ubicación.</summary>
        public const string ErrorSinPermiso = "Sin permiso de ubicación no se puede iniciar la ruta.";

        readonly IFuenteUbicacion fuente;
        readonly IServicioRuta servicioRuta; // null en el editor y en PC: la ruta sigue con la ubicación de Unity
        EstadoUbicacion estado = EstadoUbicacion.SinPermiso;
        Coordenada? posicion;
        float? rumbo;
        readonly FiltroBrujula filtroBrujula = new FiltroBrujula(); // la brújula tiembla
        readonly Func<double> reloj;
        double ultimaBrujula; // segundos de la lectura anterior de la brújula
        bool quiereFuncionar; // Iniciar sin Detener después: la app está delante
        bool encendido;       // GPS y brújula encendidos
        double ultimaLectura;
        int lecturas; // lecturas del GPS (de Unity o de la cola del servicio), aunque repitan la coordenada

        // Respuesta del permiso: puede llegar desde el hilo de Java; se aplica en Actualizar
        int respuestaPendiente; // 0 nada, 1 concedido, 2 denegado (se lee y se pone a 0 con Interlocked)

        // Si Android aún puede preguntar por la ubicación (historia 3.1)
        readonly PreguntaPermiso pregunta = new PreguntaPermiso();
        bool pidiendo, explicaba;

        // Ruta (historia 2.1)
        bool rutaActiva;          // entre IniciarRuta y TerminarRuta
        bool servicioActivo;      // el servicio de ruta está arrancado
        bool soloPrimerPlano;     // permiso de notificaciones denegado: la ruta se pausa con la pantalla apagada
        string textoRuta = "";
        int respuestaNotificaciones; // como respuestaPendiente, para POST_NOTIFICATIONS

        /// <param name="servicioRuta">Servicio nativo de la ruta; null fuera de Android.</param>
        /// <param name="reloj">Segundos que no retroceden (por defecto, los de Unity), para calmar la brújula.</param>
        public UbicacionGps(IFuenteUbicacion fuente, IServicioRuta servicioRuta = null, Func<double> reloj = null)
        {
            this.reloj = reloj ?? (() => Time.realtimeSinceStartupAsDouble);
            this.fuente = fuente ?? throw new ArgumentNullException(nameof(fuente));
            this.servicioRuta = servicioRuta;
            if (fuente.TienePermiso) estado = EstadoUbicacion.Buscando;
        }

        public EstadoUbicacion Estado => estado;
        public Coordenada? Posicion => posicion;
        public float? Rumbo => rumbo;
        public Coordenada? UltimaConocida => fuente.TienePermiso && fuente.UbicacionEncendida ? fuente.UltimaConocida : null;
        public event Action Cambiado;
        public int Lecturas => lecturas;

        /// <summary>True con el GPS y la brújula encendidos (para las pruebas).</summary>
        internal bool Encendido => encendido;

        /// <summary>True con el servicio de ruta arrancado (para las pruebas).</summary>
        internal bool ServicioRutaActivo => servicioActivo;

        public bool RutaActiva => rutaActiva;
        public bool RutaSoloPrimerPlano => soloPrimerPlano;

        public bool PuedePedirPermiso => pregunta.PuedePedir;

        public void PedirPermiso()
        {
            if (fuente.TienePermiso) { AlConceder(); return; }
            explicaba = fuente.ExplicaPermiso;
            pidiendo = true;
            fuente.PedirPermiso(concedido =>
            {
                if (concedido) Interlocked.Exchange(ref respuestaPendiente, 1);
                else Interlocked.CompareExchange(ref respuestaPendiente, 2, 0); // un "concedido" no se pisa
            });
        }

        public void Iniciar()
        {
            quiereFuncionar = true;
            if (!fuente.TienePermiso)
            {
                // Se quitó el permiso desde Ajustes con la app en segundo plano
                if (estado == EstadoUbicacion.Buscando || estado == EstadoUbicacion.Activa || estado == EstadoUbicacion.Apagada)
                    Cambiar(EstadoUbicacion.SinPermiso, null, null);
                TerminarRuta(); // sin permiso el servicio no puede seguir
                return;
            }
            // Se dio el permiso de notificaciones desde Ajustes durante la ruta: ya puede seguir en segundo plano
            if (rutaActiva && soloPrimerPlano && servicioRuta != null && !servicioRuta.NecesitaPermisoNotificaciones)
                ArrancarServicio();
            ArrancarConPermiso();
        }

        public void Detener()
        {
            quiereFuncionar = false;
            if (!MantenerEnPausa) Apagar();
        }

        /// <summary>
        /// Si la ubicación de Unity sigue encendida con la app en segundo plano. Durante una ruta en Android el GPS
        /// en segundo plano es el servicio de ruta (sigue en marcha y guarda las posiciones), así que Unity apaga lo
        /// suyo; sin servicio en marcha (notificaciones denegadas) no hay GPS en segundo plano (NFR-3). Fuera de
        /// Android no hay servicio: durante la ruta se mantiene la ubicación de Unity.
        /// </summary>
        bool MantenerEnPausa => rutaActiva && servicioRuta == null;

        public Resultado<bool> IniciarRuta(string textoNotificacion)
        {
            if (!fuente.TienePermiso) return Resultado<bool>.Fallo(ErrorSinPermiso);
            if (rutaActiva)
            {
                ActualizarNotificacion(textoNotificacion);
                return Resultado<bool>.Exito(servicioActivo);
            }
            rutaActiva = true;
            textoRuta = textoNotificacion ?? "";
            if (servicioRuta == null)
            {
                Debug.Log("PriceMap: ruta sin servicio en segundo plano (fuera de Android): sigue la ubicación de Unity.");
                return Resultado<bool>.Exito(false);
            }
            if (servicioRuta.NecesitaPermisoNotificaciones)
            {
                // Android 13+: primero el permiso; la respuesta se aplica en Actualizar
                servicioRuta.PedirPermisoNotificaciones(concedido => Interlocked.Exchange(ref respuestaNotificaciones, concedido ? 1 : 2));
                return Resultado<bool>.Exito(false);
            }
            ArrancarServicio();
            return Resultado<bool>.Exito(true);
        }

        public void ActualizarNotificacion(string textoNotificacion)
        {
            if (!rutaActiva) return;
            textoRuta = textoNotificacion ?? "";
            if (servicioActivo) servicioRuta.ActualizarTexto(textoRuta);
        }

        public void TerminarRuta()
        {
            if (!rutaActiva) return;
            rutaActiva = false;
            Interlocked.Exchange(ref respuestaNotificaciones, 0);
            if (servicioActivo)
            {
                servicioActivo = false;
                servicioRuta.Parar();
            }
            if (!quiereFuncionar) Apagar(); // terminada con la app en segundo plano: GPS fuera (NFR-3)
            if (soloPrimerPlano)
            {
                soloPrimerPlano = false;
                Cambiado?.Invoke();
            }
        }

        void ArrancarServicio()
        {
            servicioRuta.Iniciar(textoRuta);
            servicioActivo = true;
            if (soloPrimerPlano)
            {
                soloPrimerPlano = false;
                Cambiado?.Invoke();
            }
        }

        /// <summary>
        /// Recoge en orden las posiciones que el servicio de ruta ha guardado (con la pantalla apagada o desde el
        /// último fotograma) y las entrega una a una por Posicion y Cambiado. Se llama en cada Actualizar y cuando
        /// el plugin avisa de una posición nueva (UnitySendMessage).
        /// </summary>
        internal void RecogerRuta()
        {
            if (!servicioActivo) return;
            var texto = servicioRuta.VaciarCola();
            if (string.IsNullOrEmpty(texto)) return;
            // Con la ubicación del móvil apagada (o sin permiso) las posiciones guardadas son viejas: se tiran
            if (estado != EstadoUbicacion.Activa && estado != EstadoUbicacion.Buscando) return;
            foreach (var lectura in LecturaRuta.Leer(texto))
            {
                lecturas++; // antes de Cambiar: quien escuche Cambiado ya la ve contada
                Cambiar(EstadoUbicacion.Activa, lectura.Posicion, rumbo);
            }
        }

        public void AbrirAjustes() => fuente.AbrirAjustes(estado == EstadoUbicacion.Apagada ? AjustesUbicacion : AjustesApp);

        void AlConceder()
        {
            pidiendo = false;
            ArrancarConPermiso();
        }

        void AlDenegar()
        {
            Apagar();
            // Una vez por petición: la respuesta puede llegar repetida (precisa y aproximada)
            if (pidiendo) pregunta.Denegar(explicaba, fuente.ExplicaPermiso);
            pidiendo = false;
            bool yaDenegado = estado == EstadoUbicacion.Denegado;
            Cambiar(EstadoUbicacion.Denegado, null, null);
            if (yaDenegado) Cambiado?.Invoke(); // PuedePedirPermiso ha podido cambiar
        }

        /// <summary>
        /// Con permiso: si la ubicación del móvil está apagada, Apagada; si no, Buscando (o sigue Activa con la
        /// última posición) y, con la app delante, enciende el GPS. Con el diálogo del permiso la app pierde el
        /// foco: si está en pausa, se enciende al volver (Iniciar).
        /// </summary>
        void ArrancarConPermiso()
        {
            if (!fuente.UbicacionEncendida)
            {
                Apagar();
                Cambiar(EstadoUbicacion.Apagada, null, null);
                return;
            }
            if (estado != EstadoUbicacion.Activa) Cambiar(EstadoUbicacion.Buscando, null, null);
            if (quiereFuncionar) Encender();
        }

        void Encender()
        {
            if (encendido) return;
            encendido = true;
            fuente.Encender();
        }

        void Apagar()
        {
            if (!encendido) return;
            encendido = false;
            fuente.Apagar();
        }

        /// <summary>Una vez por fotograma: aplica la respuesta del permiso y lee GPS y brújula.</summary>
        public void Actualizar()
        {
            int respuesta = Interlocked.Exchange(ref respuestaPendiente, 0);
            if (respuesta != 0)
            {
                if (respuesta == 1 || fuente.TienePermiso) AlConceder(); else AlDenegar();
            }
            int notificaciones = Interlocked.Exchange(ref respuestaNotificaciones, 0);
            if (notificaciones != 0 && rutaActiva && !servicioActivo)
            {
                if (notificaciones == 1) ArrancarServicio();
                else if (!soloPrimerPlano)
                {
                    // Sin notificación no hay servicio en primer plano: la ruta sigue solo con la app delante
                    soloPrimerPlano = true;
                    Debug.Log("PriceMap: permiso de notificaciones denegado; la ruta se pausa con la pantalla apagada.");
                    Cambiado?.Invoke();
                }
            }
            RecogerRuta();
            if (!encendido) return;

            // Se apagó la ubicación del móvil con la app abierta, o el servicio no arranca
            if (!fuente.UbicacionEncendida || fuente.EstadoServicio == LocationServiceStatus.Failed)
            {
                Apagar();
                Cambiar(EstadoUbicacion.Apagada, null, null);
                return;
            }

            var nuevoEstado = estado;
            var nuevaPosicion = posicion;
            // Con el servicio de ruta en marcha las posiciones vienen de su cola (RecogerRuta): no se mezclan con las
            // de Unity. Si no arrancó o el sistema lo paró, siguen las de Unity para que la posición no se congele.
            bool posicionesDelServicio = servicioActivo && servicioRuta.EnMarcha;
            if (!posicionesDelServicio && fuente.EstadoServicio == LocationServiceStatus.Running)
            {
                var (lectura, marca) = fuente.UltimaLectura;
                if (marca > 0 && marca != ultimaLectura)
                {
                    ultimaLectura = marca;
                    lecturas++;
                    nuevaPosicion = lectura;
                    nuevoEstado = EstadoUbicacion.Activa;
                }
            }
            // Sin brújula (o sin lecturas aún): solo el círculo
            Cambiar(nuevoEstado, nuevaPosicion, LeerBrujula());
        }

        /// <summary>Rumbo de la brújula ya calmado (FiltroBrujula); la primera lectura tras no tener rumbo, tal cual.</summary>
        float? LeerBrujula()
        {
            var lectura = fuente.Rumbo;
            if (!lectura.HasValue) return null;
            // Una lectura imposible no cuenta: dentro del filtro no saldría nunca
            if (float.IsNaN(lectura.Value) || float.IsInfinity(lectura.Value)) return rumbo;
            if (!rumbo.HasValue) filtroBrujula.Reiniciar();
            double ahora = reloj();
            double segundos = ahora - ultimaBrujula;
            ultimaBrujula = ahora;
            return Mathf.Repeat((float)filtroBrujula.Filtrar(lectura.Value, segundos), 360f);
        }

        void Cambiar(EstadoUbicacion nuevoEstado, Coordenada? nuevaPosicion, float? nuevoRumbo)
        {
            if (nuevoEstado == estado && nuevaPosicion == posicion && nuevoRumbo == rumbo) return;
            bool cambioEstado = nuevoEstado != estado;
            estado = nuevoEstado;
            posicion = nuevaPosicion;
            rumbo = nuevoRumbo;
            if (cambioEstado) Debug.Log($"PriceMap: ubicación {estado}.");
            Cambiado?.Invoke();
        }
    }
}
