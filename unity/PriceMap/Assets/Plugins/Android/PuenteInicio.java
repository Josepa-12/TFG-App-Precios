package com.pricemap.inicio;

import android.annotation.TargetApi;
import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.res.Configuration;
import android.content.res.Resources;
import android.graphics.drawable.ColorDrawable;
import android.net.Uri;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.os.SystemClock;
import android.util.Log;
import android.view.View;
import android.view.ViewGroup;

import com.unity3d.player.R;

/**
 * Pantalla de inicio (spec pantalla-de-inicio): ActividadPrincipal la pone encima de Unity en onCreate y C#
 * (PriceMap.Adaptadores.Sistema.PantallaInicioAndroid) pregunta por ella (estado) y le dice cuándo irse. Si Unity no
 * avisa, se va sola a los 15 s. Manda el tema de la app: el guardado por Unity o, sin él, el del sistema.
 */
public final class PuenteInicio {
    private static final String ETIQUETA = "PriceMap";
    /** Si Unity no avisa, la pantalla se va sola. */
    private static final long SEGURO_MS = 15000;
    private static final String CLAVE_TEMA = "PriceMap.tema"; // PreferenciasLocales + ServicioTema.ClavePreferencia

    private static final Handler principal = new Handler(Looper.getMainLooper());
    private static final Runnable seguro = new Runnable() {
        @Override public void run() { irseAhora(0.5f, 0.5f); }
    };
    private static volatile PantallaInicio vista;

    private PuenteInicio() { }

    // ───────────────────────── Lo llama C# ─────────────────────────

    /** Milisegundos desde que el pin empezó a caer (0: aún no ha podido); -1 si no hay pantalla o ya se ha ido. */
    public static int estado() {
        PantallaInicio v = vista;
        if (v == null) return -1;
        long inicio = v.inicio();
        return inicio == 0 ? 0 : (int) Math.min(Integer.MAX_VALUE, SystemClock.uptimeMillis() - inicio);
    }

    /** Se va, con el pin hacia ese punto (fracción de la pantalla de Unity, desde arriba a la izquierda). */
    public static void irse(final float x, final float y) {
        principal.post(new Runnable() {
            @Override public void run() { irseAhora(x, y); }
        });
    }

    // ───────────────────────── Lo llama ActividadPrincipal (hilo principal) ─────────────────────────

    /** El fondo de la ventana, del color de la pantalla de inicio. Antes de setContentView. */
    public static void prepararVentana(Activity actividad) {
        try {
            actividad.getWindow().setBackgroundDrawable(new ColorDrawable(colorFondo(actividad, oscuro(actividad))));
        } catch (RuntimeException e) {
            Log.w(ETIQUETA, "No se pudo poner el fondo de la ventana: " + e);
        }
        fijarArranque(actividad);
    }

    /** Pone la pantalla encima de Unity. Si falla, la app arranca sin ella. */
    public static void mostrar(Activity actividad, View unity) {
        quitar();
        PantallaInicio nueva = null;
        try {
            nueva = new PantallaInicio(actividad, unity, oscuro(actividad));
            actividad.addContentView(nueva, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
            vista = nueva;
            principal.postDelayed(seguro, SEGURO_MS);
        } catch (RuntimeException | OutOfMemoryError e) {
            Log.w(ETIQUETA, "No se pudo mostrar la pantalla de inicio: " + e);
            if (nueva != null) nueva.liberar();
        }
    }

    /** La quita ya, sin animación, y suelta sus vistas e imágenes. */
    public static void quitar() {
        principal.removeCallbacks(seguro);
        PantallaInicio v = vista;
        if (v == null) return;
        // Primero devuelve Unity a TalkBack: al ver que ya no está (-1), C# le pide el foco
        try {
            v.liberar();
        } finally {
            vista = null;
        }
    }

    /**
     * Android 12+: la ventana de arranque del próximo inicio, con el tema guardado en la app aunque el del sistema sea
     * otro (sin tema guardado, el del sistema). Antes de Android 12 no se puede: manda el del sistema.
     */
    public static void fijarArranque(Activity actividad) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) return;
        try {
            fijarArranque31(actividad);
        } catch (RuntimeException e) {
            Log.w(ETIQUETA, "No se pudo fijar el tema del arranque: " + e);
        }
    }

    @TargetApi(Build.VERSION_CODES.S)
    private static void fijarArranque31(Activity actividad) {
        String guardado = temaGuardado(actividad);
        int tema = "oscuro".equals(guardado) ? R.style.PriceMapInicioOscuro
                : "claro".equals(guardado) ? R.style.PriceMapInicioClaro : Resources.ID_NULL;
        actividad.getSplashScreen().setSplashScreenTheme(tema);
    }

    // ───────────────────────── Tema (como ReglaTema.Inicial) ─────────────────────────

    static boolean oscuro(Context contexto) {
        String guardado = temaGuardado(contexto);
        if ("oscuro".equals(guardado)) return true;
        if ("claro".equals(guardado)) return false;
        int modo = contexto.getResources().getConfiguration().uiMode & Configuration.UI_MODE_NIGHT_MASK;
        return modo == Configuration.UI_MODE_NIGHT_YES;
    }

    /** El tema que guardó Unity (PlayerPrefs: claves y textos codificados como en una URL); null sin él. */
    private static String temaGuardado(Context contexto) {
        try {
            SharedPreferences prefs = contexto.getSharedPreferences(contexto.getPackageName() + ".v2.playerprefs", Context.MODE_PRIVATE);
            String valor = prefs.getString(Uri.encode(CLAVE_TEMA), null);
            return valor == null ? null : Uri.decode(valor);
        } catch (RuntimeException e) {
            return null;
        }
    }

    static int colorFondo(Context contexto, boolean oscuro) {
        return contexto.getColor(oscuro ? R.color.inicio_oscuro : R.color.inicio_claro);
    }

    // ───────────────────────── Salida ─────────────────────────

    private static void irseAhora(float x, float y) {
        final PantallaInicio v = vista;
        if (v == null || v.yendose()) return;
        principal.removeCallbacks(seguro);
        long dura;
        try {
            dura = v.irse(x, y);
        } catch (RuntimeException e) {
            Log.w(ETIQUETA, "La pantalla de inicio se quita sin animación: " + e);
            dura = 0;
        }
        // Con el reloj y no al acabar la animación: con la pantalla apagada también se va
        principal.postDelayed(new Runnable() {
            @Override public void run() { if (vista == v) quitar(); }
        }, dura);
    }
}
