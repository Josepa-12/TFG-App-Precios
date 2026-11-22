package com.pricemap.voz;

import android.content.Context;
import android.media.AudioAttributes;
import android.media.AudioFocusRequest;
import android.media.AudioManager;
import android.os.Handler;
import android.os.Looper;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import android.util.Log;

import java.util.Locale;

/**
 * Indicaciones por voz (historia 2.8; FR21). Solo lo llama el adaptador de C# (PriceMap.Adaptadores.Voz.VozAndroid) con
 * AndroidJavaClass; la historia 2.8b lo usará también desde el servicio de la ruta, con la pantalla apagada.
 *
 * Usa el TextToSpeech del sistema en castellano de España (sin voces descargadas ni en la nube), con el uso de audio de
 * guía de navegación, y pide el foco de audio transitorio dejando que las otras apps bajen el volumen; lo suelta al acabar
 * la frase (UtteranceProgressListener). Cada frase corta la anterior (QUEUE_FLUSH). Con el móvil en silencio o en
 * vibración no habla. Todo se hace en el hilo principal de Android.
 */
public final class PuenteVoz {
    private static final String ETIQUETA = "PriceMap";
    private static final Handler principal = new Handler(Looper.getMainLooper());
    private static final AudioAttributes ATRIBUTOS = new AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_ASSISTANCE_NAVIGATION_GUIDANCE)
            .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
            .build();

    private static TextToSpeech tts;
    private static boolean listo;        // el motor ha terminado de iniciarse
    private static boolean sinCastellano; // el motor no tiene es-ES: se calla
    private static String pendiente;     // la frase pedida antes de que el motor estuviera listo
    private static AudioManager audio;
    private static AudioFocusRequest foco;
    private static int contador;
    private static String ultima;        // id de la última frase: solo al acabar esa se suelta el foco

    private PuenteVoz() { }

    /** Dice la frase (corta la anterior). En silencio o vibración, nada. */
    public static void hablar(final Context contexto, final String texto) {
        if (contexto == null || texto == null || texto.trim().isEmpty()) return;
        final Context app = contexto.getApplicationContext();
        principal.post(new Runnable() {
            @Override public void run() { hablarAhora(app, texto); }
        });
    }

    /** Calla lo que esté diciendo y suelta el foco de audio. */
    public static void parar() {
        principal.post(new Runnable() {
            @Override public void run() {
                pendiente = null;
                if (tts != null) tts.stop();
                soltarFoco();
            }
        });
    }

    private static void hablarAhora(Context app, String texto) {
        if (audio == null) audio = (AudioManager) app.getSystemService(Context.AUDIO_SERVICE);
        if (audio != null && audio.getRingerMode() != AudioManager.RINGER_MODE_NORMAL) return;
        if (tts == null) iniciar(app);
        if (!listo) { pendiente = texto; return; }
        if (sinCastellano) return;
        decir(texto);
    }

    private static void iniciar(Context app) {
        listo = false;
        sinCastellano = false;
        tts = new TextToSpeech(app, new TextToSpeech.OnInitListener() {
            @Override public void onInit(final int estado) {
                principal.post(new Runnable() {
                    @Override public void run() { alIniciar(estado); }
                });
            }
        });
    }

    private static void alIniciar(int estado) {
        if (tts == null) return;
        if (estado != TextToSpeech.SUCCESS) {
            Log.w(ETIQUETA, "No se pudo iniciar la voz del sistema (" + estado + ").");
            tts.shutdown(); // suelta el enlace con el motor antes de intentarlo otra vez
            tts = null; // se intenta otra vez con la siguiente frase
            pendiente = null;
            return;
        }
        int idioma = tts.setLanguage(new Locale("es", "ES"));
        sinCastellano = idioma == TextToSpeech.LANG_MISSING_DATA || idioma == TextToSpeech.LANG_NOT_SUPPORTED;
        if (sinCastellano) Log.w(ETIQUETA, "La voz del sistema no tiene castellano (es-ES): no se dicen las indicaciones.");
        tts.setAudioAttributes(ATRIBUTOS);
        tts.setOnUtteranceProgressListener(new UtteranceProgressListener() {
            @Override public void onStart(String id) { }
            @Override public void onDone(String id) { alAcabar(id); }
            @Override @Deprecated public void onError(String id) { alAcabar(id); }
            @Override public void onError(String id, int codigo) { alAcabar(id); }
            @Override public void onStop(String id, boolean interrumpida) { alAcabar(id); }
        });
        listo = true;
        String texto = pendiente;
        pendiente = null;
        // El móvil pudo pasar a silencio o vibración mientras se iniciaba el motor
        boolean conSonido = audio == null || audio.getRingerMode() == AudioManager.RINGER_MODE_NORMAL;
        if (texto != null && !sinCastellano && conSonido) decir(texto);
    }

    private static void decir(String texto) {
        pedirFoco();
        String id = "pricemap-" + (++contador);
        ultima = id;
        if (tts.speak(texto, TextToSpeech.QUEUE_FLUSH, null, id) != TextToSpeech.SUCCESS) {
            Log.w(ETIQUETA, "La voz del sistema no ha aceptado la frase.");
            soltarFoco();
        }
    }

    // Llega desde el hilo del motor: se pasa al principal. Una frase cortada por la siguiente no suelta su foco
    private static void alAcabar(final String id) {
        principal.post(new Runnable() {
            @Override public void run() {
                if (id != null && id.equals(ultima)) soltarFoco();
            }
        });
    }

    private static void pedirFoco() {
        if (audio == null) return;
        if (foco == null) {
            foco = new AudioFocusRequest.Builder(AudioManager.AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK)
                    .setAudioAttributes(ATRIBUTOS)
                    .build();
        }
        audio.requestAudioFocus(foco);
    }

    private static void soltarFoco() {
        if (audio != null && foco != null) audio.abandonAudioFocusRequest(foco);
    }
}
