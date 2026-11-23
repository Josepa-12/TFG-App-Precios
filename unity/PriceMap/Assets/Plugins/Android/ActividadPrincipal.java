package com.pricemap.app;

import android.annotation.TargetApi;
import android.app.Activity;
import android.content.Intent;
import android.content.res.Configuration;
import android.os.Build;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.Window;

import com.pricemap.inicio.PuenteInicio;
import com.pricemap.ruta.PuenteUbicacionRuta;
import com.unity3d.player.IUnityPermissionRequestSupport;
import com.unity3d.player.IUnityPlayerLifecycleEvents;
import com.unity3d.player.IUnityPlayerSupport;
import com.unity3d.player.PermissionRequest;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerForActivityOrService;

/**
 * Actividad de PriceMap (historia 2.8, parte 2; FR21): copia de la plantilla UnityPlayerActivity de Unity 6000.3 con dos
 * cambios. Uno: pone la pantalla de inicio (com.pricemap.inicio) encima de Unity desde onCreate. Otro: mientras C# dice
 * que hay ruta (seguir(true), con el servicio de ruta en marcha), al ir a segundo plano (pantalla bloqueada u otra app
 * delante) no se pausa el reproductor de Unity: la navegación, los recálculos y la voz siguen a tiempo real, y se avisa a
 * C# ("AlSegundoPlano" "1": pocos fotogramas y sin dibujar el mapa); al volver, "0". Sin ruta, todo como la plantilla
 * (Unity se pausa y C# apaga el GPS, NFR-3). Si la ruta acaba con la app detrás, se hace entonces la pausa que se saltó.
 */
public class ActividadPrincipal extends Activity implements IUnityPlayerLifecycleEvents, IUnityPermissionRequestSupport, IUnityPlayerSupport
{
    protected UnityPlayerForActivityOrService mUnityPlayer; // no cambiar el nombre: lo usa el código nativo de Unity

    /** C# ha dicho que hay ruta: en segundo plano, Unity sigue. */
    static volatile boolean seguirEnSegundoPlano;
    /** GameObject de Unity que recibe AlSegundoPlano (el del Arranque). */
    static volatile String objetoUnity;
    /** La actividad viva (o null): para hacer la pausa saltada si la ruta acaba con la app detrás. */
    static volatile ActividadPrincipal instancia;

    // Pausas de Unity que se han saltado (solo en el hilo de la interfaz)
    private boolean pausaSaltada, paradaSaltada;
    private boolean enPausa, parada;

    /**
     * Lo llama C# (ServicioRutaAndroid) al empezar (true) y al terminar (false) la ruta. Al terminar con la app detrás,
     * Unity se pausa ya, como si no hubiera habido ruta.
     */
    public static void seguir(boolean activo, String objeto)
    {
        if (objeto != null) objetoUnity = objeto;
        seguirEnSegundoPlano = activo;
        if (activo) return;
        final ActividadPrincipal actividad = instancia;
        if (actividad != null) actividad.runOnUiThread(actividad::pausarLoSaltado);
    }

    /** Navegando y con el servicio de ruta vivo: Unity no se pausa. */
    private static boolean siguiendo()
    {
        return seguirEnSegundoPlano && PuenteUbicacionRuta.enMarcha();
    }

    private void avisar(boolean segundoPlano)
    {
        String objeto = objetoUnity;
        if (objeto == null) return;
        try {
            UnityPlayer.UnitySendMessage(objeto, "AlSegundoPlano", segundoPlano ? "1" : "0");
        } catch (Throwable ignorado) {
            // Unity puede no estar listo
        }
    }

    /** La ruta ha terminado: lo saltado se hace ahora (la app sigue detrás) y C# vuelve a lo normal. */
    private void pausarLoSaltado()
    {
        if (siguiendo()) return; // seguir(true) llegó antes que esto: se sigue navegando
        if (!pausaSaltada && !paradaSaltada) return;
        avisar(false);
        if (pausaSaltada && enPausa) { pausaSaltada = false; mUnityPlayer.onPause(); }
        if (paradaSaltada && parada) { paradaSaltada = false; mUnityPlayer.onStop(); }
    }

    // Override this in your custom UnityPlayerActivity to tweak the command line arguments passed to the Unity Android Player
    protected String updateUnityCommandLineArguments(String cmdLine)
    {
        return cmdLine;
    }

    // Setup activity layout
    @Override protected void onCreate(Bundle savedInstanceState)
    {
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        super.onCreate(savedInstanceState);
        PuenteInicio.prepararVentana(this);

        String cmdLine = updateUnityCommandLineArguments(getIntent().getStringExtra("unity"));
        getIntent().putExtra("unity", cmdLine);

        mUnityPlayer = new UnityPlayerForActivityOrService(this, this);
        setContentView(mUnityPlayer.getFrameLayout());
        mUnityPlayer.getFrameLayout().requestFocus();
        instancia = this;
        PuenteInicio.mostrar(this, mUnityPlayer.getFrameLayout()); // encima de Unity, que tarda 2-3 s en dibujar
    }

    @Override
    public UnityPlayerForActivityOrService getUnityPlayerConnection() {
        return mUnityPlayer;
    }

    // When Unity player unloaded move task to background
    @Override public void onUnityPlayerUnloaded() {
        moveTaskToBack(true);
    }

    // Callback before Unity player process is killed
    @Override public void onUnityPlayerQuitted() {
    }

    @Override protected void onNewIntent(Intent intent)
    {
        // Compartir con la app (historia 2.5c): getIntent() es siempre el último recibido
        setIntent(intent);
        mUnityPlayer.newIntent(intent);
    }

    // Quit Unity
    @Override protected void onDestroy ()
    {
        if (instancia == this) {
            instancia = null;
            PuenteInicio.quitar(); // la de una actividad más nueva, no
        }
        // Sin actividad nadie pararía el servicio de ruta (GPS, notificación y procesador despierto): se para con ella
        seguirEnSegundoPlano = false;
        if (PuenteUbicacionRuta.enMarcha()) PuenteUbicacionRuta.parar();
        mUnityPlayer.destroy();
        super.onDestroy();
    }

    // Navegando, Unity no se pausa en segundo plano (onStop/onPause). Solo onStop avisa a C# («segundo plano»): onPause
    // llega también con la app aún a la vista (pantalla partida, un diálogo encima) y el mapa debe seguir dibujándose
    @Override protected void onStop()
    {
        super.onStop();
        parada = true;
        PuenteInicio.fijarArranque(this); // el tema pudo cambiar en la app: el próximo arranque, con él
        if (siguiendo()) { paradaSaltada = true; avisar(true); return; }
        mUnityPlayer.onStop();
    }

    @Override protected void onStart()
    {
        super.onStart();
        parada = false;
        if (paradaSaltada) { paradaSaltada = false; avisar(false); return; } // Unity no se paró: no hay que arrancarlo
        mUnityPlayer.onStart();
    }

    // Pause Unity
    @Override protected void onPause()
    {
        super.onPause();
        enPausa = true;
        if (siguiendo()) { pausaSaltada = true; return; }
        mUnityPlayer.onPause();
    }

    // Resume Unity
    @Override protected void onResume()
    {
        super.onResume();
        enPausa = false;
        if (pausaSaltada) { pausaSaltada = false; return; } // Unity no se pausó: no hay que reanudarlo (el aviso, en onStart)
        mUnityPlayer.onResume();
    }

    // Low Memory Unity
    @Override public void onLowMemory()
    {
        super.onLowMemory();
        mUnityPlayer.onTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.Critical);
    }

    // Trim Memory Unity
    @Override public void onTrimMemory(int level)
    {
        super.onTrimMemory(level);
        switch (level)
        {
        case TRIM_MEMORY_RUNNING_MODERATE:
            mUnityPlayer.onTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.Medium);
            break;
        case TRIM_MEMORY_RUNNING_LOW:
            mUnityPlayer.onTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.High);
            break;
        case TRIM_MEMORY_RUNNING_CRITICAL:
            mUnityPlayer.onTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.Critical);
            break;
        }
    }

    // This ensures the layout will be correct.
    @Override public void onConfigurationChanged(Configuration newConfig)
    {
        super.onConfigurationChanged(newConfig);
        mUnityPlayer.configurationChanged(newConfig);
    }

    // Notify Unity of the focus change.
    @Override public void onWindowFocusChanged(boolean hasFocus)
    {
        super.onWindowFocusChanged(hasFocus);
        mUnityPlayer.windowFocusChanged(hasFocus);
    }

    // For some reason the multiple keyevent type is not supported by the ndk.
    // Force event injection by overriding dispatchKeyEvent().
    @Override public boolean dispatchKeyEvent(KeyEvent event)
    {
        if (event.getAction() == KeyEvent.ACTION_MULTIPLE)
            return mUnityPlayer.injectEvent(event);
        return super.dispatchKeyEvent(event);
    }

    @Override
    @TargetApi(Build.VERSION_CODES.M)
    public void requestPermissions(PermissionRequest request)
    {
        mUnityPlayer.addPermissionRequest(request);
    }

    @Override public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults)
    {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        mUnityPlayer.permissionResponse(this, requestCode, permissions, grantResults);
    }

    // Pass any events not handled by (unfocused) views straight to UnityPlayer
    @Override public boolean onKeyUp(int keyCode, KeyEvent event)     { return mUnityPlayer.getFrameLayout().onKeyUp(keyCode, event); }
    @Override public boolean onKeyDown(int keyCode, KeyEvent event)   { return mUnityPlayer.getFrameLayout().onKeyDown(keyCode, event); }
    @Override public boolean onTouchEvent(MotionEvent event)          { return mUnityPlayer.getFrameLayout().onTouchEvent(event); }
    @Override public boolean onGenericMotionEvent(MotionEvent event)  { return mUnityPlayer.getFrameLayout().onGenericMotionEvent(event); }
}
