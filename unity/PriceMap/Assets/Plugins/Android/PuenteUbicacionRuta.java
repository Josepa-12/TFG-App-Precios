package com.pricemap.ruta;

import android.app.Activity;
import android.content.Intent;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayDeque;

/**
 * Puente entre Unity y ServicioUbicacionRuta (historia 2.1, AD-19). Solo lo llama el adaptador de GPS de C#
 * (PriceMap.Adaptadores.Gps.ServicioRutaAndroid) con AndroidJavaClass: iniciar, actualizar, parar y vaciarCola.
 * Guarda en memoria las posiciones del servicio mientras Unity está en pausa (pantalla apagada u otra app delante);
 * Unity las recoge en orden al volver o en cada fotograma.
 */
public final class PuenteUbicacionRuta {
    /** Posiciones guardadas como mucho (1 h a una por segundo): si Unity no las recoge, se tiran las más viejas. */
    static final int MAX_COLA = 3600;

    private static final ArrayDeque<String> cola = new ArrayDeque<>();
    private static volatile String texto = "";
    private static volatile String objetoUnity;

    private PuenteUbicacionRuta() { }

    /**
     * Arranca el servicio en primer plano con su notificación fija. Se llama con la app delante (al pulsar Ir):
     * así basta el permiso de ubicación normal, sin ACCESS_BACKGROUND_LOCATION.
     *
     * @param textoNotificacion "Navegando a destino · N min"
     * @param objeto            GameObject de Unity que recibe el aviso de cada posición (UnitySendMessage)
     */
    public static void iniciar(String textoNotificacion, String objeto) {
        Activity actividad = UnityPlayer.currentActivity;
        if (actividad == null) return;
        synchronized (cola) { cola.clear(); }
        texto = textoNotificacion != null ? textoNotificacion : "";
        objetoUnity = objeto;
        Intent intent = new Intent(actividad, ServicioUbicacionRuta.class);
        intent.putExtra(ServicioUbicacionRuta.EXTRA_TEXTO, texto);
        actividad.startForegroundService(intent);
    }

    /** Cambia el texto de la notificación (p. ej. el tiempo restante). */
    public static void actualizar(String textoNotificacion) {
        texto = textoNotificacion != null ? textoNotificacion : "";
        ServicioUbicacionRuta servicio = ServicioUbicacionRuta.instancia;
        if (servicio != null) servicio.mostrarNotificacion(texto);
    }

    /** Para el servicio: quita la notificación, apaga el GPS y tira las posiciones que no se hayan recogido. */
    public static void parar() {
        objetoUnity = null;
        Activity actividad = UnityPlayer.currentActivity;
        if (actividad != null) actividad.stopService(new Intent(actividad, ServicioUbicacionRuta.class));
        else {
            ServicioUbicacionRuta servicio = ServicioUbicacionRuta.instancia;
            if (servicio != null) servicio.stopSelf();
        }
        synchronized (cola) { cola.clear(); }
    }

    /** True si el servicio está arrancado en primer plano y vivo (no falló al arrancar ni lo paró el sistema). */
    public static boolean enMarcha() {
        return ServicioUbicacionRuta.instancia != null;
    }

    /**
     * Devuelve y borra las posiciones guardadas, en orden, una por línea: "lat;lon;rumbo;velocidad;precision;horaMs"
     * (punto decimal; un campo vacío si el GPS no lo da). Cadena vacía si no hay ninguna.
     */
    public static String vaciarCola() {
        synchronized (cola) {
            if (cola.isEmpty()) return "";
            StringBuilder sb = new StringBuilder();
            for (String linea : cola) {
                if (sb.length() > 0) sb.append('\n');
                sb.append(linea);
            }
            cola.clear();
            return sb.toString();
        }
    }

    static String texto() { return texto; }

    /** Llamado por el servicio con cada posición: a la cola y, con Unity activo, un aviso para que la recoja ya. */
    static void anadir(String linea) {
        synchronized (cola) {
            if (cola.size() >= MAX_COLA) cola.pollFirst();
            cola.addLast(linea);
        }
        String objeto = objetoUnity;
        Activity actividad = UnityPlayer.currentActivity;
        if (objeto != null && actividad != null && actividad.hasWindowFocus()) {
            try {
                UnityPlayer.UnitySendMessage(objeto, "AlPosicionRuta", linea);
            } catch (Throwable ignorado) {
                // Unity puede no estar listo: la posición sigue en la cola
            }
        }
    }
}
