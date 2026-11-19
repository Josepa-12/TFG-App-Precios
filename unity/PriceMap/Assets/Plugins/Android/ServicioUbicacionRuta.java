package com.pricemap.ruta;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ServiceInfo;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Build;
import android.os.Bundle;
import android.os.IBinder;
import android.os.Looper;
import android.os.SystemClock;
import android.util.Log;

import java.util.Locale;

/**
 * Servicio en primer plano de tipo "location" (historia 2.1, AD-19): mantiene el GPS durante una ruta aunque la
 * pantalla esté apagada u otra app esté delante, con una notificación fija de navegación. Usa LocationManager
 * (GPS y red, cada segundo, sin distancia mínima), sin Google Play Services. Cada posición va a la cola de
 * PuenteUbicacionRuta. Lo arranca y lo para solo el puente (y este, el adaptador de GPS de Unity).
 */
public class ServicioUbicacionRuta extends Service implements LocationListener {
    static final String EXTRA_TEXTO = "texto";
    static final String CANAL = "navegacion";
    static final int ID_NOTIFICACION = 2101;
    static final long INTERVALO_MS = 1000L;
    static final String ETIQUETA = "PriceMap";

    /** Servicio en marcha (o null): para actualizar la notificación desde el puente. */
    static volatile ServicioUbicacionRuta instancia;

    private LocationManager ubicacion;
    private boolean escuchando;

    @Override
    public IBinder onBind(Intent intent) { return null; }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        // El texto más reciente (pudo cambiar entre iniciar y este arranque); si no, el del intent
        String texto = PuenteUbicacionRuta.texto();
        if ((texto == null || texto.isEmpty()) && intent != null) texto = intent.getStringExtra(EXTRA_TEXTO);
        if (texto == null) texto = "";
        crearCanal();
        try {
            Notification n = notificacion(texto);
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q)
                startForeground(ID_NOTIFICACION, n, ServiceInfo.FOREGROUND_SERVICE_TYPE_LOCATION);
            else
                startForeground(ID_NOTIFICACION, n);
        } catch (RuntimeException e) {
            // Sin permiso de ubicación o arrancado desde segundo plano: no se puede seguir
            Log.w(ETIQUETA, "PriceMap: no se pudo arrancar el servicio de ruta: " + e.getMessage());
            stopSelf();
            return START_NOT_STICKY;
        }
        instancia = this;
        escuchar();
        return START_NOT_STICKY;
    }

    private void escuchar() {
        if (escuchando) return;
        ubicacion = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
        if (ubicacion == null) return;
        // Cada proveedor por separado y aunque esté apagado: empieza a dar posiciones cuando se encienda. Con solo
        // la ubicación aproximada el GPS falla (SecurityException) pero la red sigue.
        if (pedir(LocationManager.GPS_PROVIDER)) escuchando = true;
        if (pedir(LocationManager.NETWORK_PROVIDER)) escuchando = true;
        if (escuchando) Log.i(ETIQUETA, "PriceMap: servicio de ruta en marcha.");
    }

    private boolean pedir(String proveedor) {
        try {
            ubicacion.requestLocationUpdates(proveedor, INTERVALO_MS, 0f, this, Looper.getMainLooper());
            return true;
        } catch (SecurityException | IllegalArgumentException e) {
            Log.w(ETIQUETA, "PriceMap: el servicio de ruta no puede usar " + proveedor + ": " + e.getMessage());
            return false;
        }
    }

    /** Hora (reloj del arranque) de la última posición del GPS: mientras sea reciente, las de la red se tiran. */
    private long ultimoGpsMs = -1;
    static final long GPS_RECIENTE_MS = 5000L;

    @Override
    public void onLocationChanged(Location l) {
        long ahora = SystemClock.elapsedRealtime();
        if (LocationManager.GPS_PROVIDER.equals(l.getProvider())) ultimoGpsMs = ahora;
        else if (ultimoGpsMs >= 0 && ahora - ultimoGpsMs < GPS_RECIENTE_MS) return; // la red es menos precisa: no salta el punto
        String linea = String.format(Locale.ROOT, "%.7f;%.7f;%s;%s;%s;%d",
                l.getLatitude(), l.getLongitude(),
                l.hasBearing() ? String.format(Locale.ROOT, "%.1f", l.getBearing()) : "",
                l.hasSpeed() ? String.format(Locale.ROOT, "%.2f", l.getSpeed()) : "",
                l.hasAccuracy() ? String.format(Locale.ROOT, "%.1f", l.getAccuracy()) : "",
                l.getTime());
        PuenteUbicacionRuta.anadir(linea);
    }

    // En Android 8 y 9 estos métodos no tienen implementación por defecto
    @Override public void onStatusChanged(String proveedor, int estado, Bundle extras) { }
    @Override public void onProviderEnabled(String proveedor) { }
    @Override public void onProviderDisabled(String proveedor) { }

    /** La app se cierra desde la lista de recientes: la ruta termina con ella. */
    @Override
    public void onTaskRemoved(Intent rootIntent) {
        stopSelf();
        super.onTaskRemoved(rootIntent);
    }

    @Override
    public void onDestroy() {
        instancia = null;
        if (ubicacion != null && escuchando) ubicacion.removeUpdates(this);
        escuchando = false;
        stopForeground(STOP_FOREGROUND_REMOVE);
        Log.i(ETIQUETA, "PriceMap: servicio de ruta parado.");
        super.onDestroy();
    }

    void mostrarNotificacion(String texto) {
        NotificationManager gestor = (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);
        if (gestor != null) gestor.notify(ID_NOTIFICACION, notificacion(texto));
    }

    private void crearCanal() {
        NotificationManager gestor = (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);
        if (gestor == null || gestor.getNotificationChannel(CANAL) != null) return;
        NotificationChannel canal = new NotificationChannel(CANAL, "Navegación", NotificationManager.IMPORTANCE_LOW);
        canal.setSound(null, null);
        canal.enableVibration(false);
        canal.setShowBadge(false);
        gestor.createNotificationChannel(canal);
    }

    private Notification notificacion(String texto) {
        Notification.Builder b = new Notification.Builder(this, CANAL)
                .setSmallIcon(getApplicationInfo().icon)
                .setContentTitle(getApplicationInfo().loadLabel(getPackageManager()))
                .setContentText(texto)
                .setOngoing(true)
                .setOnlyAlertOnce(true)
                .setCategory(Notification.CATEGORY_NAVIGATION);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
            b.setForegroundServiceBehavior(Notification.FOREGROUND_SERVICE_IMMEDIATE);
        // Tocar la notificación vuelve a la app
        Intent abrir = getPackageManager().getLaunchIntentForPackage(getPackageName());
        if (abrir != null) {
            abrir.setFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP | Intent.FLAG_ACTIVITY_REORDER_TO_FRONT);
            b.setContentIntent(PendingIntent.getActivity(this, 0, abrir, PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT));
        }
        return b.build();
    }
}
