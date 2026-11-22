package com.pricemap.red;

import android.util.Log;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.Charset;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Peticiones HTTPS con la red de Android (arreglo «red Android TLS 1.3»). UnityWebRequest en Android solo habla
 * TLS 1.2 y los servidores de FOSSGIS (Valhalla, OSRM) solo aceptan TLS 1.3; HttpURLConnection lo tiene desde
 * Android 10. Solo lo llama el adaptador de C# (PriceMap.Adaptadores.Red.ClienteHttp) con AndroidJavaClass.
 *
 * Cada petición va en un hilo aparte (nunca en el de la interfaz ni en el de Unity); C# pregunta su estado en cada
 * fotograma y, al acabar, lee el código, el texto y la cabecera Location y la libera. Cancelar desconecta. El tiempo
 * máximo cuenta la petición entera (como el timeout de UnityWebRequest). Los certificados los comprueba el sistema
 * como siempre: aquí no se cambia nada de TLS.
 */
public final class PuenteRed {
    private static final String ETIQUETA = "PriceMap";

    /** Estados de una petición (los mismos números en ClienteHttp). */
    public static final int EN_CURSO = 0;
    public static final int HECHA = 1;        // respuesta HTTP < 400 (también una redirección sin seguir)
    public static final int PROTOCOLO = 2;    // respuesta HTTP >= 400
    public static final int CONEXION = 3;     // sin respuesta: sin red, SSL, tiempo agotado...
    public static final int CANCELADA = 4;
    public static final int DESCONOCIDA = -1; // id que no existe (ya liberada)

    private static final ThreadFactory HILOS_DEMONIO = new ThreadFactory() {
        private final AtomicInteger n = new AtomicInteger();
        @Override public Thread newThread(Runnable r) {
            Thread t = new Thread(r, "PriceMapRed-" + n.incrementAndGet());
            t.setDaemon(true);
            return t;
        }
    };
    private static final ExecutorService hilos = Executors.newCachedThreadPool(HILOS_DEMONIO);
    private static final ScheduledThreadPoolExecutor reloj = crearReloj();
    private static final ConcurrentHashMap<Integer, Peticion> peticiones = new ConcurrentHashMap<Integer, Peticion>();
    private static final AtomicInteger contador = new AtomicInteger();
    private static final Charset UTF8 = Charset.forName("UTF-8");

    private PuenteRed() { }

    /** Un hilo para los tiempos máximos; un límite cancelado sale de la cola al momento (con su Peticion). */
    private static ScheduledThreadPoolExecutor crearReloj() {
        ScheduledThreadPoolExecutor r = new ScheduledThreadPoolExecutor(1, HILOS_DEMONIO);
        r.setRemoveOnCancelPolicy(true);
        return r;
    }

    private static final class Peticion {
        volatile int estado = EN_CURSO;
        volatile int codigo;
        volatile String texto = "";
        volatile String location = "";
        volatile String error = "";
        volatile HttpURLConnection conexion;
        volatile boolean cancelada;
        volatile boolean agotada;
        volatile ScheduledFuture<?> limite;
    }

    /**
     * Empieza una petición y devuelve su id. cuerpo vacío = sin cuerpo; cabeceras en líneas «Nombre: valor».
     * seguirRedirecciones = false para los enlaces cortos (los saltos los cuenta ResolutorEnlacesWeb).
     */
    public static int pedir(final String metodo, final String url, final String cuerpo, final String cabeceras,
                            final int msMaximo, final boolean seguirRedirecciones) {
        final int id = contador.incrementAndGet();
        final Peticion p = new Peticion();
        peticiones.put(id, p);
        p.limite = reloj.schedule(new Runnable() {
            @Override public void run() {
                p.agotada = true;
                // Al momento, aunque el hilo siga en el DNS o sin socket: C# no espera más de msMaximo
                if (terminar(p, CONEXION, 0, "", "", "Request timeout"))
                    Log.w(ETIQUETA, "PuenteRed: " + metodo + " " + servidor(url) + " falla: Request timeout");
                desconectar(p);
            }
        }, Math.max(1, msMaximo), TimeUnit.MILLISECONDS);
        hilos.execute(new Runnable() {
            @Override public void run() {
                ejecutar(p, metodo, url, cuerpo, cabeceras, msMaximo, seguirRedirecciones);
            }
        });
        return id;
    }

    private static void ejecutar(Peticion p, String metodo, String url, String cuerpo, String cabeceras,
                                 int msMaximo, boolean seguirRedirecciones) {
        HttpURLConnection c = null;
        try {
            c = (HttpURLConnection) new URL(url).openConnection();
            p.conexion = c;
            if (p.cancelada || p.agotada) throw new IOException("cortada antes de empezar");
            c.setRequestMethod(metodo);
            c.setInstanceFollowRedirects(seguirRedirecciones);
            c.setConnectTimeout(Math.max(1, msMaximo));
            c.setReadTimeout(Math.max(1, msMaximo));
            c.setUseCaches(false);
            if (cabeceras != null) {
                for (String linea : cabeceras.split("\n")) {
                    int dos = linea.indexOf(':');
                    if (dos <= 0) continue;
                    c.setRequestProperty(linea.substring(0, dos).trim(), linea.substring(dos + 1).trim());
                }
            }
            if (cuerpo != null && !cuerpo.isEmpty()) {
                byte[] bytes = cuerpo.getBytes(UTF8);
                c.setDoOutput(true);
                c.setFixedLengthStreamingMode(bytes.length);
                OutputStream salida = c.getOutputStream();
                try { salida.write(bytes); } finally { salida.close(); }
            }
            int codigo = c.getResponseCode();
            String location = c.getHeaderField("Location");
            if (codigo >= 400) {
                String texto = leer(c.getErrorStream());
                String mensaje = c.getResponseMessage();
                terminar(p, PROTOCOLO, codigo, texto, location,
                        "HTTP/1.1 " + codigo + (mensaje == null || mensaje.isEmpty() ? "" : " " + mensaje));
            } else {
                // Una redirección sin seguir no hace falta leerla (y su cuerpo puede no existir)
                String texto = codigo < 300 ? leer(c.getInputStream()) : "";
                terminar(p, HECHA, codigo, texto, location, "");
            }
        } catch (Throwable e) { // también los Error (OutOfMemoryError…): nunca se queda EN_CURSO
            if (p.cancelada) {
                terminar(p, CANCELADA, 0, "", "", "");
            } else {
                String error = p.agotada ? "Request timeout"
                        : e.getClass().getSimpleName() + (e.getMessage() == null ? "" : ": " + e.getMessage());
                if (terminar(p, CONEXION, 0, "", "", error))
                    Log.w(ETIQUETA, "PuenteRed: " + metodo + " " + servidor(url) + " falla: " + error);
            }
        } finally {
            ScheduledFuture<?> limite = p.limite;
            if (limite != null) limite.cancel(false);
            if (c != null) {
                try { c.disconnect(); } catch (Throwable ignorada) { }
            }
            p.conexion = null;
        }
    }

    /**
     * Pone el estado final y lo que lee C#, solo si sigue EN_CURSO: gana el primero (la respuesta, el tiempo agotado
     * o cancelar), y el hilo que acaba después no lo cambia. Devuelve si lo ha puesto.
     */
    private static boolean terminar(Peticion p, int estado, int codigo, String texto, String location, String error) {
        synchronized (p) {
            if (p.estado != EN_CURSO) return false;
            p.codigo = codigo;
            p.texto = texto == null ? "" : texto;
            p.location = location == null ? "" : location;
            p.error = error == null ? "" : error;
            p.estado = estado;
            return true;
        }
    }

    /** Solo el servidor (nunca la URL entera: lleva lo buscado y coordenadas, FR17). */
    private static String servidor(String url) {
        try { return new URL(url).getHost(); } catch (Throwable e) { return "(URL no válida)"; }
    }

    private static String leer(InputStream entrada) throws IOException {
        if (entrada == null) return "";
        try {
            ByteArrayOutputStream bytes = new ByteArrayOutputStream();
            byte[] trozo = new byte[16 * 1024];
            int n;
            while ((n = entrada.read(trozo)) != -1) bytes.write(trozo, 0, n);
            return new String(bytes.toByteArray(), UTF8);
        } finally {
            entrada.close();
        }
    }

    private static void desconectar(final Peticion p) {
        final HttpURLConnection c = p.conexion;
        if (c == null) return;
        // disconnect() puede tocar la red (cerrar el socket TLS): nunca en el hilo que llama (el de Unity)
        hilos.execute(new Runnable() {
            @Override public void run() {
                try { c.disconnect(); } catch (Exception ignorada) { }
            }
        });
    }

    private static Peticion buscar(int id) { return peticiones.get(id); }

    public static int estado(int id) { Peticion p = buscar(id); return p == null ? DESCONOCIDA : p.estado; }

    public static int codigo(int id) { Peticion p = buscar(id); return p == null ? 0 : p.codigo; }

    public static String texto(int id) { Peticion p = buscar(id); return p == null ? "" : p.texto; }

    public static String location(int id) { Peticion p = buscar(id); return p == null ? "" : p.location; }

    public static String error(int id) { Peticion p = buscar(id); return p == null ? "" : p.error; }

    /** Corta la petición: la deja en CANCELADA al momento (si no había acabado) y desconecta. */
    public static void cancelar(int id) {
        Peticion p = buscar(id);
        if (p == null) return;
        p.cancelada = true;
        terminar(p, CANCELADA, 0, "", "", "");
        desconectar(p);
        ScheduledFuture<?> limite = p.limite;
        if (limite != null) limite.cancel(false);
    }

    /** Olvida la petición (C# la llama siempre al acabar, también tras cancelar). */
    public static void liberar(int id) { peticiones.remove(id); }
}
