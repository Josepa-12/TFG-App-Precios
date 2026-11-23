using System;

namespace PriceMap.Aplicacion
{
    /// <summary>Cómo está la tarjeta de un permiso en la bienvenida (historia 3.1).</summary>
    public enum Consentimiento
    {
        /// <summary>Sin contestar: «Ahora no» y «Permitir».</summary>
        SinContestar,
        /// <summary>El permiso está concedido.</summary>
        Permitido,
        /// <summary>«Ahora no» o denegado en Android, que aún puede volver a preguntar.</summary>
        Rechazado,
        /// <summary>Denegado y Android ya no pregunta: solo queda activarlo en los Ajustes del móvil.</summary>
        Bloqueado,
    }

    /// <summary>
    /// Bienvenida del primer arranque (historia 3.1): tres pasos (qué es, privacidad y permisos), el aviso completo desde
    /// el segundo y el consentimiento de la ubicación y de las notificaciones. Al terminar guarda lo elegido y que ya se ha
    /// visto; cerrada a mitad no guarda nada y vuelve a salir. Rechazar no impide nada.
    /// </summary>
    public class ServicioBienvenida
    {
        public const string ClaveVista = "bienvenida.vista";
        public const string ClaveUbicacion = "bienvenida.ubicacion";
        public const string ClaveNotificaciones = "bienvenida.notificaciones";
        /// <summary>Valores de las elecciones guardadas. Lo no contestado cuenta como «Ahora no».</summary>
        public const string ValorPermitido = "permitido", ValorAhoraNo = "ahora-no";

        public const int Pasos = 3;
        public const int PasoPrivacidad = 1;
        public const int PasoPermisos = 2;

        readonly IPreferencias preferencias;
        readonly ServicioUbicacion ubicacion;
        readonly INotificaciones notificaciones;
        bool ahoraNoUbicacion, ahoraNoNotificaciones;

        public ServicioBienvenida(IPreferencias preferencias, ServicioUbicacion ubicacion, INotificaciones notificaciones)
        {
            this.preferencias = preferencias ?? throw new ArgumentNullException(nameof(preferencias));
            this.ubicacion = ubicacion ?? throw new ArgumentNullException(nameof(ubicacion));
            this.notificaciones = notificaciones ?? throw new ArgumentNullException(nameof(notificaciones));
            Pendiente = preferencias.Leer(ClaveVista) != "1";
            if (!Pendiente) return;
            Ubicacion = LeerUbicacion();
            Notificaciones = LeerNotificaciones();
            ubicacion.Cambiado += RefrescarUbicacion;
            notificaciones.Cambiado += RefrescarNotificaciones;
        }

        /// <summary>Tiene que salir (o está a la vista): aún no se ha terminado ninguna vez.</summary>
        public bool Pendiente { get; private set; }

        /// <summary>Paso a la vista, de 0 a 2.</summary>
        public int Paso { get; private set; }

        /// <summary>El aviso de privacidad completo, encima del paso de la privacidad.</summary>
        public bool AvisoAbierto { get; private set; }

        public Consentimiento Ubicacion { get; private set; }
        public Consentimiento Notificaciones { get; private set; }

        /// <summary>Ha cambiado el paso, o se ha abierto o cerrado el aviso.</summary>
        public event Action PasoCambiado;
        /// <summary>Ha cambiado alguno de los dos consentimientos.</summary>
        public event Action ConsentimientoCambiado;
        /// <summary>«Empezar»: se acabó (una sola vez).</summary>
        public event Action Terminada;

        /// <summary>«Siguiente»; en el último paso o con el aviso abierto, nada.</summary>
        public void Siguiente()
        {
            if (!Pendiente || AvisoAbierto || Paso >= Pasos - 1) return;
            Paso++;
            if (Paso == PasoPermisos) Refrescar();
            PasoCambiado?.Invoke();
        }

        /// <summary>«‹» o el botón atrás: cierra el aviso o vuelve al paso anterior. En el primero, nada (false).</summary>
        public bool Atras()
        {
            if (!Pendiente) return false;
            if (AvisoAbierto) AvisoAbierto = false;
            else if (Paso > 0) Paso--;
            else return false;
            PasoCambiado?.Invoke();
            return true;
        }

        /// <summary>«Leer el aviso completo», en el paso de la privacidad.</summary>
        public void AbrirAviso()
        {
            if (!Pendiente || AvisoAbierto || Paso != PasoPrivacidad) return;
            AvisoAbierto = true;
            PasoCambiado?.Invoke();
        }

        /// <summary>«Permitir» de la ubicación: el diálogo de Android.</summary>
        public void PermitirUbicacion()
        {
            if (!Pendiente || Ubicacion == Consentimiento.Permitido) return;
            ubicacion.Permitir();
            RefrescarUbicacion();
        }

        public void AhoraNoUbicacion()
        {
            ahoraNoUbicacion = true;
            RefrescarUbicacion();
        }

        /// <summary>«Permitir» de las notificaciones: el diálogo de Android.</summary>
        public void PermitirNotificaciones()
        {
            if (!Pendiente || Notificaciones == Consentimiento.Permitido) return;
            notificaciones.PedirPermiso();
            RefrescarNotificaciones();
        }

        public void AhoraNoNotificaciones()
        {
            ahoraNoNotificaciones = true;
            RefrescarNotificaciones();
        }

        /// <summary>Vuelve a leer los permisos (al volver a la app: pueden haber cambiado en los Ajustes del móvil).</summary>
        public void Refrescar()
        {
            RefrescarUbicacion();
            RefrescarNotificaciones();
        }

        /// <summary>«Empezar»: guarda las elecciones y que ya se ha visto.</summary>
        public void Terminar()
        {
            if (!Pendiente) return;
            Refrescar();
            Pendiente = false;
            ubicacion.Cambiado -= RefrescarUbicacion;
            notificaciones.Cambiado -= RefrescarNotificaciones;
            preferencias.Guardar(ClaveUbicacion, Valor(Ubicacion));
            preferencias.Guardar(ClaveNotificaciones, Valor(Notificaciones));
            preferencias.Guardar(ClaveVista, "1"); // lo último: cortado antes, vuelve a salir
            Terminada?.Invoke();
        }

        static string Valor(Consentimiento c) => c == Consentimiento.Permitido ? ValorPermitido : ValorAhoraNo;

        void RefrescarUbicacion()
        {
            if (!Pendiente) return;
            var c = LeerUbicacion();
            if (c == Ubicacion) return;
            Ubicacion = c;
            ConsentimientoCambiado?.Invoke();
        }

        void RefrescarNotificaciones()
        {
            if (!Pendiente) return;
            var c = LeerNotificaciones();
            if (c == Notificaciones) return;
            Notificaciones = c;
            ConsentimientoCambiado?.Invoke();
        }

        Consentimiento LeerUbicacion()
        {
            switch (ubicacion.Estado)
            {
                case EstadoUbicacion.SinPermiso: return SinPermiso(denegado: false, ahoraNoUbicacion, puedePedir: true);
                case EstadoUbicacion.Denegado: return SinPermiso(denegado: true, ahoraNoUbicacion, ubicacion.PuedePedirPermiso);
                default: return Consentimiento.Permitido; // también con la ubicación del móvil apagada
            }
        }

        Consentimiento LeerNotificaciones()
        {
            var estado = notificaciones.Estado;
            return estado == EstadoNotificaciones.Concedido
                ? Consentimiento.Permitido
                : SinPermiso(estado == EstadoNotificaciones.Denegado, ahoraNoNotificaciones, notificaciones.PuedePedir);
        }

        static Consentimiento SinPermiso(bool denegado, bool ahoraNo, bool puedePedir) =>
            !denegado && !ahoraNo ? Consentimiento.SinContestar
            : puedePedir ? Consentimiento.Rechazado : Consentimiento.Bloqueado;
    }
}
