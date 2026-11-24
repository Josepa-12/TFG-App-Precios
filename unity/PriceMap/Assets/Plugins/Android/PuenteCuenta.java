package com.pricemap.cuenta;

import android.app.Activity;
import android.os.CancellationSignal;

import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.GetCredentialProviderConfigurationException;
import androidx.credentials.exceptions.GetCredentialUnsupportedException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.unity3d.player.UnityPlayer;

import java.util.Locale;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Entrar con Google (historia 3.3): enseña el selector de cuentas del sistema (Credential Manager) y devuelve el idToken,
 * con el que C# entra en Firebase. Solo lo llama el adaptador de C# (SelectorGoogleAndroid) con AndroidJavaClass: pide, pregunta
 * el estado en cada fotograma (Unity se pausa con el selector delante y sigue al volver) y, al acabar, lee y libera.
 * Aquí no se escribe nada en el registro: ni la cuenta ni el token salen de esta clase salvo hacia C#.
 */
public final class PuenteCuenta {
    /** Estados de una petición (los mismos números en SelectorGoogleAndroid). */
    public static final int EN_CURSO = 0;
    public static final int HECHA = 1;
    public static final int CANCELADA = 2;     // el usuario ha cerrado el selector (o C# ha cancelado)
    public static final int FALLO = 3;
    public static final int NO_DISPONIBLE = 4; // sin default_web_client_id o un móvil que no puede
    public static final int DESCONOCIDA = -1;  // id que no existe (ya liberada)

    private static final ConcurrentHashMap<Integer, Peticion> peticiones = new ConcurrentHashMap<Integer, Peticion>();
    private static final AtomicInteger contador = new AtomicInteger();

    private PuenteCuenta() { }

    private static final class Peticion {
        volatile int estado = EN_CURSO;
        volatile String token = "";
        volatile String motivo = "";
        final CancellationSignal senal = new CancellationSignal();
    }

    /** Abre el selector (en el hilo de la interfaz) y devuelve el id de la petición. */
    public static int pedir() {
        final int id = contador.incrementAndGet();
        final Peticion p = new Peticion();
        peticiones.put(id, p);
        final Activity actividad = UnityPlayer.currentActivity;
        if (actividad == null) {
            terminar(p, FALLO, "", "sin actividad");
            return id;
        }
        actividad.runOnUiThread(new Runnable() {
            @Override public void run() { abrir(actividad, p); }
        });
        return id;
    }

    private static void abrir(Activity actividad, final Peticion p) {
        if (p.estado != EN_CURSO) return; // cancelada antes de abrir
        try {
            // Lo genera el SDK de Firebase con google-services.json
            int recurso = actividad.getResources().getIdentifier("default_web_client_id", "string", actividad.getPackageName());
            String cliente = recurso == 0 ? "" : actividad.getString(recurso);
            if (cliente.isEmpty()) {
                terminar(p, NO_DISPONIBLE, "", "sin default_web_client_id");
                return;
            }
            GetCredentialRequest peticion = new GetCredentialRequest.Builder()
                    .addCredentialOption(new GetSignInWithGoogleOption.Builder(cliente).build())
                    .build();
            CredentialManager.create(actividad).getCredentialAsync(actividad, peticion, p.senal, Runnable::run,
                    new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                        @Override public void onResult(GetCredentialResponse respuesta) { leer(p, respuesta.getCredential()); }
                        @Override public void onError(GetCredentialException e) { fallar(p, e); }
                    });
        } catch (Throwable e) { // también los Error (una clase que falta): nunca se queda EN_CURSO
            terminar(p, FALLO, "", e.getClass().getSimpleName());
        }
    }

    private static void leer(Peticion p, Credential credencial) {
        try {
            if (credencial instanceof CustomCredential
                    && GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credencial.getType())) {
                terminar(p, HECHA, GoogleIdTokenCredential.createFrom(credencial.getData()).getIdToken(), "");
            } else {
                terminar(p, FALLO, "", "credencial inesperada");
            }
        } catch (Throwable e) {
            terminar(p, FALLO, "", e.getClass().getSimpleName());
        }
    }

    private static void fallar(Peticion p, GetCredentialException e) {
        int estado = FALLO;
        if (e instanceof GetCredentialCancellationException) {
            // «[16] Cancelled by user.» lo ha cerrado el usuario; otro «[16] …» («Account reauth failed») lo rechaza Google
            // (p. ej. la huella SHA-1 no está en Firebase)
            CharSequence texto = e.getErrorMessage();
            String mensaje = texto == null ? "" : texto.toString().toLowerCase(Locale.ROOT);
            if (!mensaje.startsWith("[16]") || mensaje.contains("cancelled by user")) estado = CANCELADA;
        } else if (e instanceof GetCredentialProviderConfigurationException || e instanceof GetCredentialUnsupportedException) {
            estado = NO_DISPONIBLE;
        }
        terminar(p, estado, "", e.getClass().getSimpleName()); // solo el tipo: el mensaje podría llevar la cuenta
    }

    /** Pone el estado final y lo que lee C#, solo si sigue EN_CURSO: gana el primero (la respuesta o cancelar). */
    private static void terminar(Peticion p, int estado, String token, String motivo) {
        synchronized (p) {
            if (p.estado != EN_CURSO) return;
            p.token = token;
            p.motivo = motivo;
            p.estado = estado;
        }
    }

    public static int estado(int id) { Peticion p = peticiones.get(id); return p == null ? DESCONOCIDA : p.estado; }

    public static String token(int id) { Peticion p = peticiones.get(id); return p == null ? "" : p.token; }

    /** Solo el tipo del fallo, para el registro. */
    public static String motivo(int id) { Peticion p = peticiones.get(id); return p == null ? "" : p.motivo; }

    /** La deja en CANCELADA al momento (si no había acabado) y cierra el selector. */
    public static void cancelar(int id) {
        final Peticion p = peticiones.get(id);
        if (p == null) return;
        terminar(p, CANCELADA, "", "");
        Activity actividad = UnityPlayer.currentActivity;
        if (actividad == null) return;
        actividad.runOnUiThread(new Runnable() {
            @Override public void run() {
                try { p.senal.cancel(); } catch (Throwable ignorada) { }
            }
        });
    }

    /** Olvida la petición y su token (C# la llama siempre al acabar, también tras cancelar). */
    public static void liberar(int id) {
        Peticion p = peticiones.remove(id);
        if (p != null) p.token = "";
    }
}
