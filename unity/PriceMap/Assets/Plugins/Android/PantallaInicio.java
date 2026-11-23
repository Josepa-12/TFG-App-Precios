package com.pricemap.inicio;

import android.animation.Animator;
import android.animation.Keyframe;
import android.animation.ObjectAnimator;
import android.animation.PropertyValuesHolder;
import android.animation.TimeInterpolator;
import android.animation.ValueAnimator;
import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.Path;
import android.os.Build;
import android.os.SystemClock;
import android.provider.Settings;
import android.util.Property;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewTreeObserver;
import android.view.accessibility.AccessibilityManager;
import android.view.accessibility.AccessibilityNodeInfo;
import android.view.animation.AccelerateDecelerateInterpolator;
import android.view.animation.AccelerateInterpolator;
import android.view.animation.AlphaAnimation;
import android.view.animation.DecelerateInterpolator;
import android.view.animation.LinearInterpolator;
import android.view.animation.PathInterpolator;
import android.widget.FrameLayout;
import android.widget.ImageView;

import com.unity3d.player.R;

import java.util.ArrayList;
import java.util.List;

/**
 * La pantalla de inicio (spec pantalla-de-inicio, animación A): el pin cae, rebota y flota con su sombra y ondas en el
 * suelo; «PriceMap» sale debajo. Al irse, el pin se encoge hasta un punto y el fondo se desvanece (0,8 s). Medidas en px
 * de la imagen original (852 × 1846), escaladas para caber y centradas. Con «Quitar animaciones» del sistema, el logo
 * quieto y un fundido corto. Se come los toques y, para TalkBack, es lo único de la ventana mientras está.
 */
final class PantallaInicio extends FrameLayout {
    // Imagen original y sus piezas (docs/diseno/inicio/separar_capas.py): izquierda, arriba, ancho, alto
    private static final float ANCHO = 852f, ALTO = 1846f;
    private static final float EJE = 428.5f; // la punta del pin: va en el centro de la pantalla
    private static final float[] PIN = { 270f, 655f, 320f, 390f };
    private static final float[] ANILLO = { EJE - 180f, 1014f - 60f, 360f, 120f };
    private static final float[] SOMBRA = { EJE - 180f, 1008f - 60f, 360f, 120f };
    private static final float[] NOMBRE = { 130f, 1118f, 598f, 144f };
    private static final float PUNTA_Y = 0.97f; // alto del pin en el que está su punta
    private static final int COLOR_ANILLO = 0xFF3A78FA, COLOR_SOMBRA = 0xFFB2C8FA;

    // Tiempos (ms). El pin acaba de caer y de rebotar en 1 s (ParametrosNegocio.InicioPinSegundos)
    private static final int CAER = 1000, FLOTAR = 550, ONDA = 1500, SALIR = 800, FUNDIDO = 200;
    private static final float CAE_EN = 0.66f; // parte de CAER en la que toca el suelo por primera vez

    private final View unity;
    private final int accesibilidadUnity;
    private final View fondo;
    private final Elipse sombra, onda, anillo;
    private final ImageView pin, nombre;
    private final float escalaAnimaciones;
    private final boolean sinAnimaciones;
    private final List<Animator> animaciones = new ArrayList<>();
    private float u; // px de pantalla por px de la imagen
    private volatile long inicio; // uptimeMillis en que el pin empezó a caer; 0 antes
    private boolean yendose;
    private ViewTreeObserver.OnPreDrawListener alVerse; // hasta que empieza

    PantallaInicio(Context contexto, View unity, boolean oscuro) {
        super(contexto);
        this.unity = unity;
        escalaAnimaciones = Settings.Global.getFloat(contexto.getContentResolver(), Settings.Global.ANIMATOR_DURATION_SCALE, 1f);
        sinAnimaciones = !(escalaAnimaciones > 0f) || !ValueAnimator.areAnimatorsEnabled();
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) setForceDarkAllowed(false);

        fondo = new View(contexto);
        fondo.setBackgroundColor(PuenteInicio.colorFondo(contexto, oscuro));
        sombra = new Elipse(contexto, COLOR_SOMBRA, 104f, 27f, 0f, 0f);
        onda = new Elipse(contexto, COLOR_ANILLO, 173f, 54f, 143f, 40f);
        anillo = new Elipse(contexto, COLOR_ANILLO, 173f, 54f, 143f, 40f);
        float ancho = getResources().getDisplayMetrics().widthPixels;
        pin = imagen(contexto, R.drawable.inicio_pin, ancho * PIN[2] / ANCHO);
        nombre = imagen(contexto, oscuro ? R.drawable.inicio_nombre_oscuro : R.drawable.inicio_nombre_claro, ancho * NOMBRE[2] / ANCHO);
        for (View v : new View[] { fondo, sombra, onda, anillo, pin, nombre }) addView(v);

        onda.setAlpha(0f);
        if (!sinAnimaciones) for (View v : new View[] { sombra, anillo, pin, nombre }) v.setAlpha(0f);

        // TalkBack: solo «PriceMap, cargando» (Unity, tapado, no se lee)
        setContentDescription(getResources().getString(R.string.inicio_cargando));
        setImportantForAccessibility(IMPORTANT_FOR_ACCESSIBILITY_YES);
        for (int i = 0; i < getChildCount(); i++) getChildAt(i).setImportantForAccessibility(IMPORTANT_FOR_ACCESSIBILITY_NO);
        accesibilidadUnity = unity.getImportantForAccessibility();
        unity.setImportantForAccessibility(IMPORTANT_FOR_ACCESSIBILITY_NO_HIDE_DESCENDANTS);

        // Empieza cuando se va a ver: lo que el hilo principal se bloquee antes no se come la caída
        alVerse = new ViewTreeObserver.OnPreDrawListener() {
            @Override public boolean onPreDraw() {
                if (getWidth() <= 0) return true;
                dejarDeEsperar();
                post(new Runnable() {
                    @Override public void run() { empezar(); }
                });
                return true;
            }
        };
        getViewTreeObserver().addOnPreDrawListener(alVerse);
    }

    private void dejarDeEsperar() {
        if (alVerse != null) getViewTreeObserver().removeOnPreDrawListener(alVerse);
        alVerse = null;
    }

    long inicio() { return inicio; }

    boolean yendose() { return yendose; }

    // La imagen, leída ya a un tamaño cercano al que se ve
    private static ImageView imagen(Context contexto, int recurso, float anchoVisto) {
        BitmapFactory.Options opciones = new BitmapFactory.Options();
        opciones.inScaled = false;
        opciones.inJustDecodeBounds = true;
        BitmapFactory.decodeResource(contexto.getResources(), recurso, opciones);
        opciones.inJustDecodeBounds = false;
        opciones.inSampleSize = 1;
        while (opciones.outWidth / (opciones.inSampleSize * 2) >= anchoVisto) opciones.inSampleSize *= 2;
        Bitmap mapa = BitmapFactory.decodeResource(contexto.getResources(), recurso, opciones);
        if (mapa != null) mapa.setHasMipMap(true); // se encoge mucho al irse
        ImageView vista = new ImageView(contexto);
        vista.setScaleType(ImageView.ScaleType.FIT_XY);
        vista.setImageBitmap(mapa);
        vista.setTag(mapa);
        return vista;
    }

    // ───────────────────────── Disposición ─────────────────────────

    @Override protected void onMeasure(int anchoPedido, int altoPedido) {
        int w = MeasureSpec.getSize(anchoPedido), h = MeasureSpec.getSize(altoPedido);
        setMeasuredDimension(w, h);
        u = Math.min(w / ANCHO, h / ALTO);
        medir(fondo, w, h);
        medir(sombra, SOMBRA);
        medir(onda, ANILLO);
        medir(anillo, ANILLO);
        medir(pin, PIN);
        medir(nombre, NOMBRE);
    }

    private void medir(View v, float[] caja) { medir(v, Math.round(caja[2] * u), Math.round(caja[3] * u)); }

    private static void medir(View v, int w, int h) {
        v.measure(MeasureSpec.makeMeasureSpec(w, MeasureSpec.EXACTLY), MeasureSpec.makeMeasureSpec(h, MeasureSpec.EXACTLY));
    }

    @Override protected void onLayout(boolean cambiado, int izquierda, int arriba, int derecha, int abajo) {
        fondo.layout(0, 0, derecha - izquierda, abajo - arriba);
        colocar(sombra, SOMBRA);
        colocar(onda, ANILLO);
        colocar(anillo, ANILLO);
        colocar(pin, PIN);
        colocar(nombre, NOMBRE);
        pin.setPivotX((EJE - PIN[0]) * u);
        pin.setPivotY(PIN[3] * PUNTA_Y * u);
    }

    private void colocar(View v, float[] caja) {
        int x = Math.round(getWidth() / 2f + (caja[0] - EJE) * u);
        int y = Math.round((getHeight() - ALTO * u) / 2f + caja[1] * u);
        v.layout(x, y, x + v.getMeasuredWidth(), y + v.getMeasuredHeight());
    }

    // ───────────────────────── Entrada ─────────────────────────

    private void empezar() {
        if (inicio != 0 || yendose) return;
        inicio = SystemClock.uptimeMillis();
        AccessibilityManager lector = (AccessibilityManager) getContext().getSystemService(Context.ACCESSIBILITY_SERVICE);
        if (lector != null && lector.isTouchExplorationEnabled())
            performAccessibilityAction(AccessibilityNodeInfo.ACTION_ACCESSIBILITY_FOCUS, null);
        if (sinAnimaciones) return;

        // El pin cae, rebota una vez y se queda
        TimeInterpolator caida = new PathInterpolator(0.5f, 0f, 0.8f, 0.4f);
        float alto = -0.62f * ANCHO * u, bote = -0.05f * ANCHO * u;
        animar(pin, CAER, 0, new LinearInterpolator(),
                claves(View.TRANSLATION_Y, clave(0f, alto, null), clave(CAE_EN, 0f, caida),
                        clave((1f + CAE_EN) / 2f, bote, new DecelerateInterpolator()), clave(1f, 0f, new AccelerateInterpolator())),
                claves(View.SCALE_X, clave(0f, 1.12f, null), clave(CAE_EN, 1f, caida), clave(1f, 1f, null)),
                claves(View.SCALE_Y, clave(0f, 1.12f, null), clave(CAE_EN, 1f, caida), clave(1f, 1f, null)),
                claves(View.ALPHA, clave(0f, 0f, null), clave(0.4f, 1f, null), clave(1f, 1f, null)));
        animar(sombra, Math.round(CAER * CAE_EN), 0, caida, tramo(View.SCALE_X, 0.3f, 1f), tramo(View.SCALE_Y, 0.3f, 1f), tramo(View.ALPHA, 0f, 1f));
        TimeInterpolator frena = new DecelerateInterpolator();
        animar(anillo, 400, 540, new LinearInterpolator(),
                claves(View.SCALE_X, clave(0f, 0.4f, null), clave(0.65f, 1.06f, frena), clave(1f, 1f, null)),
                claves(View.SCALE_Y, clave(0f, 0.4f, null), clave(0.65f, 1.06f, frena), clave(1f, 1f, null)),
                claves(View.ALPHA, clave(0f, 0f, null), clave(0.65f, 1f, null), clave(1f, 1f, null)));
        animar(nombre, 450, 700, frena, tramo(View.TRANSLATION_Y, 0.04f * ANCHO * u, 0f), tramo(View.ALPHA, 0f, 1f));

        // Mientras carga: flota, con la sombra al compás, y salen ondas
        TimeInterpolator suave = new AccelerateDecelerateInterpolator();
        sinFin(animar(pin, FLOTAR, CAER, suave, tramo(View.TRANSLATION_Y, 0f, -0.035f * ANCHO * u)), ValueAnimator.REVERSE);
        sinFin(animar(sombra, FLOTAR, CAER, suave, tramo(View.SCALE_X, 1f, 0.82f), tramo(View.SCALE_Y, 1f, 0.82f)), ValueAnimator.REVERSE);
        sinFin(animar(onda, ONDA, Math.round(CAER * CAE_EN), new LinearInterpolator(),
                claves(View.SCALE_X, clave(0f, 0.8f, null), clave(0.6f, 1.75f, frena), clave(1f, 1.75f, null)),
                claves(View.SCALE_Y, clave(0f, 0.8f, null), clave(0.6f, 1.75f, frena), clave(1f, 1.75f, null)),
                claves(View.ALPHA, clave(0f, 0.5f, null), clave(0.6f, 0f, null), clave(1f, 0f, null))), ValueAnimator.RESTART);
        for (Animator a : animaciones) a.start();
    }

    private ObjectAnimator animar(View v, int duracion, int espera, TimeInterpolator curva, PropertyValuesHolder... valores) {
        ObjectAnimator a = ObjectAnimator.ofPropertyValuesHolder(v, valores);
        a.setDuration(ms(duracion));
        a.setStartDelay(ms(espera));
        a.setInterpolator(curva);
        animaciones.add(a);
        return a;
    }

    private static void sinFin(ObjectAnimator a, int modo) {
        a.setRepeatCount(ValueAnimator.INFINITE);
        a.setRepeatMode(modo);
    }

    private static PropertyValuesHolder tramo(Property<View, Float> p, float desde, float hasta) {
        return PropertyValuesHolder.ofFloat(p, desde, hasta);
    }

    private static PropertyValuesHolder claves(Property<View, Float> p, Keyframe... claves) {
        return PropertyValuesHolder.ofKeyframe(p, claves);
    }

    private static Keyframe clave(float parte, float valor, TimeInterpolator curva) {
        Keyframe k = Keyframe.ofFloat(parte, valor);
        if (curva != null) k.setInterpolator(curva);
        return k;
    }

    // El sistema multiplica las duraciones por su escala de animaciones: se deshace para que C# y la pantalla vayan a la vez
    private long ms(int duracion) { return Math.max(0L, Math.round(duracion / escalaAnimaciones)); }

    // ───────────────────────── Salida ─────────────────────────

    /** Empieza a irse, con el pin hacia ese punto (fracción de la vista de Unity). Devuelve los ms que tarda. */
    long irse(float x, float y) {
        yendose = true;
        if (sinAnimaciones) {
            // Las animaciones de vista antiguas no dependen de la escala del sistema: el fundido se ve igual
            AlphaAnimation fundido = new AlphaAnimation(1f, 0f);
            fundido.setDuration(FUNDIDO);
            fundido.setFillAfter(true);
            startAnimation(fundido);
            return FUNDIDO;
        }
        pararAnimaciones();
        pin.setAlpha(1f);
        boolean medida = unity.getWidth() > 0 && unity.getHeight() > 0;
        float destinoX = medida ? unity.getLeft() - getLeft() + x * unity.getWidth() : x * getWidth();
        float destinoY = medida ? unity.getTop() - getTop() + y * unity.getHeight() : y * getHeight();
        pin.animate().translationX(destinoX - (pin.getLeft() + pin.getPivotX())).translationY(destinoY - (pin.getTop() + pin.getPivotY()))
                .scaleX(0.17f).scaleY(0.17f).setDuration(ms(600)).setInterpolator(new PathInterpolator(0.4f, 0f, 0.2f, 1f))
                .withEndAction(new Runnable() {
                    @Override public void run() {
                        pin.animate().scaleX(0.12f).scaleY(0.12f).alpha(0f).setDuration(ms(SALIR - 600)).setInterpolator(new LinearInterpolator());
                    }
                });
        fondo.animate().alpha(0f).setDuration(ms(500)).setInterpolator(new LinearInterpolator());
        sombra.animate().scaleX(0.2f).scaleY(0.2f).alpha(0f).setDuration(ms(300));
        anillo.animate().scaleX(0.25f).scaleY(0.25f).alpha(0f).setDuration(ms(400));
        onda.animate().alpha(0f).setDuration(ms(200));
        nombre.animate().translationY(0.03f * ANCHO * u).alpha(0f).setDuration(ms(300));
        return SALIR;
    }

    private void pararAnimaciones() {
        for (Animator a : animaciones) a.cancel();
        animaciones.clear();
    }

    /** Fuera de la ventana: para las animaciones, suelta las imágenes y devuelve Unity a TalkBack. */
    void liberar() {
        yendose = true;
        dejarDeEsperar();
        pararAnimaciones();
        clearAnimation();
        for (int i = 0; i < getChildCount(); i++) getChildAt(i).animate().withEndAction(null).cancel();
        ViewGroup padre = (ViewGroup) getParent();
        if (padre != null) padre.removeView(this);
        soltar(pin);
        soltar(nombre);
        removeAllViews();
        unity.setImportantForAccessibility(accesibilidadUnity);
    }

    private static void soltar(ImageView v) {
        Bitmap mapa = (Bitmap) v.getTag();
        v.setImageDrawable(null);
        v.setTag(null);
        if (mapa != null) mapa.recycle();
    }

    // ───────────────────────── Nada pasa al mapa ─────────────────────────

    @Override public boolean onInterceptTouchEvent(MotionEvent evento) { return true; }

    @Override public boolean onTouchEvent(MotionEvent evento) { return true; }

    @Override public boolean onGenericMotionEvent(MotionEvent evento) { return true; }

    // El dedo con TalkBack: se lee esta pantalla y no llega a Unity
    @Override public boolean onHoverEvent(MotionEvent evento) {
        super.onHoverEvent(evento);
        return true;
    }

    /** Elipse (o anillo, con hueco) en una caja de 360 × 120 px de la imagen. */
    private static final class Elipse extends View {
        private final Paint pintura = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Path forma = new Path();
        private final float rx, ry, huecoX, huecoY;

        Elipse(Context contexto, int color, float rx, float ry, float huecoX, float huecoY) {
            super(contexto);
            pintura.setColor(color);
            this.rx = rx;
            this.ry = ry;
            this.huecoX = huecoX;
            this.huecoY = huecoY;
        }

        @Override protected void onSizeChanged(int w, int h, int w0, int h0) {
            float s = w / 360f, cx = w / 2f, cy = h / 2f;
            forma.reset();
            forma.setFillType(Path.FillType.EVEN_ODD);
            forma.addOval(cx - rx * s, cy - ry * s, cx + rx * s, cy + ry * s, Path.Direction.CW);
            if (huecoX > 0f) forma.addOval(cx - huecoX * s, cy - huecoY * s, cx + huecoX * s, cy + huecoY * s, Path.Direction.CW);
        }

        @Override protected void onDraw(Canvas lienzo) { lienzo.drawPath(forma, pintura); }

        @Override public boolean hasOverlappingRendering() { return false; }
    }
}
