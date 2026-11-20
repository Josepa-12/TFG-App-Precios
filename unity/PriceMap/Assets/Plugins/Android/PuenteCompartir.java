package com.pricemap.compartir;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;

/**
 * Lo compartido con PriceMap (historia 2.5, parte 3; FR17). Solo lo llama el adaptador de C#
 * (PriceMap.Adaptadores.Sistema.EntradaCompartidaAndroid) con AndroidJavaClass, al arrancar y al volver delante.
 * La actividad de Unity es singleTask y su onNewIntent ya hace setIntent: getIntent() es siempre el último recibido.
 */
public final class PuenteCompartir {
    private PuenteCompartir() { }

    /**
     * El texto compartido (EXTRA_TEXT de un SEND text/plain) o la URI de un VIEW (geo:...); null si no hay. Se
     * consume al leerlo (la actividad se queda con un intent vacío): al volver a la app no se repite. Un intent que
     * viene del historial de recientes no cuenta.
     */
    public static String tomar(Activity actividad) {
        if (actividad == null) return null;
        Intent intent = actividad.getIntent();
        if (intent == null) return null;
        String accion = intent.getAction();
        if (!Intent.ACTION_SEND.equals(accion) && !Intent.ACTION_VIEW.equals(accion)) return null;
        actividad.setIntent(new Intent());
        if ((intent.getFlags() & Intent.FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY) != 0) return null;
        String texto = null;
        if (Intent.ACTION_SEND.equals(accion)) {
            CharSequence extra = intent.getCharSequenceExtra(Intent.EXTRA_TEXT);
            if (extra != null) texto = extra.toString();
        } else {
            Uri uri = intent.getData();
            if (uri != null) texto = uri.toString();
        }
        return texto == null || texto.trim().isEmpty() ? null : texto;
    }
}
