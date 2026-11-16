// Traduce un color del estilo estándar de OpenStreetMap al color equivalente de la paleta del proyecto
// (prototipo/app.js → PALETAS). Lo usan TeselaPaleta (escena "Mapa") y CesiumTerrenoPaleta (escena "MapaCesium").
// El proyecto está en espacio de color Gamma: los valores son los del PNG.
//
// Colores de referencia del estilo OSM: suelo #F2EFE9, residencial #E0DFDF, edificio #D9D0C9,
// agua #AAD3DF, parque #C8FACC, hierba #CDEBB0, secundaria #F7FABF, primaria #FCD6A4.
#ifndef PALETA_OSM_INCLUDED
#define PALETA_OSM_INCLUDED

float3 RecolorearOSM(float3 c, float3 fondo, float3 agua, float3 verde, float3 calle, float3 edificio, float3 etiqueta)
{
    float l = dot(c, float3(0.299, 0.587, 0.114));

    // 1. Degradado por luminosidad: texto ~0.2-0.4 · edificio 0.82 · residencial 0.88 · suelo 0.94 · calle 1.0
    float3 col = lerp(etiqueta, edificio, smoothstep(0.35, 0.80, l));
    col = lerp(col, fondo, smoothstep(0.835, 0.87, l));
    col = lerp(col, calle, smoothstep(0.955, 0.99, l));

    // 2. Elementos con color propio en el estilo OSM
    float esAgua  = smoothstep(0.10, 0.17, c.b - c.r) * smoothstep(0.55, 0.70, l);
    float esVerde = smoothstep(0.05, 0.10, c.g - max(c.r, c.b));
    float esVia   = smoothstep(0.12, 0.20, c.r - c.b) * smoothstep(0.72, 0.80, l);
    col = lerp(col, agua, esAgua);
    col = lerp(col, verde, esVerde);
    col = lerp(col, calle, esVia);
    return col;
}

// ── Quitar iconos de OSM ──
// Los iconos de puntos de interés del estilo OSM (y los nombres de esos sitios) son de colores vivos y
// oscuros: comida #734A08, tiendas #AC39AC, transporte #0092DA, salud #BF0000, ocio #0C7A3D...
// El resto del mapa es claro (suelo, calles, agua, parques) o gris neutro (texto de calles).
// Devuelve 1 si el píxel parece de icono, 0 si no.
float EsIconoOSM(float3 c)
{
    float l = dot(c, float3(0.299, 0.587, 0.114));
    float saturacion = max(c.r, max(c.g, c.b)) - min(c.r, min(c.g, c.b));
    return smoothstep(0.16, 0.28, saturacion) * (1.0 - smoothstep(0.60, 0.68, l));
}

// Si el píxel es de icono, lo sustituye por la media de lo que le rodea (sin contar otros píxeles de icono):
// en mitad de una calle queda calle, en un edificio queda edificio.
float3 QuitarIconosOSM(sampler2D tex, float2 uv, float2 texel, float3 c)
{
    float w = EsIconoOSM(c);
    if (w < 0.01) return c;
    float3 suma = 0; float peso = 0;
    const float2 dirs[8] = { float2(1,0), float2(-1,0), float2(0,1), float2(0,-1),
                             float2(0.707,0.707), float2(-0.707,0.707), float2(0.707,-0.707), float2(-0.707,-0.707) };
    [unroll] for (int r = 1; r <= 3; r++)
    {
        [unroll] for (int d = 0; d < 8; d++)
        {
            float3 s = tex2Dlod(tex, float4(uv + dirs[d] * texel * (r * 4.0), 0, 0)).rgb;
            float k = 1.0 - EsIconoOSM(s);
            suma += s * k; peso += k;
        }
    }
    return peso > 0.5 ? lerp(c, suma / peso, w) : c;
}

#endif
