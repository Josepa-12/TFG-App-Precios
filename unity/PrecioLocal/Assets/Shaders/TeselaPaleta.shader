// Recolorea las teselas de imagen de OpenStreetMap con la paleta del proyecto (la misma que el
// prototipo web, prototipo/app.js → PALETAS). No hace falta otro proveedor de mapas ni clave de API.
//
// Cómo funciona (el proyecto está en espacio de color Gamma, así que los valores son los del PNG):
//  1. Mapa de degradado por luminosidad: texto/iconos (oscuro) → edificios → fondo → calles (blanco).
//  2. Encima, se detectan por tono los elementos con color propio en el estilo de OSM:
//     agua (azul), zonas verdes (verde) y carreteras principales (amarillo/naranja).
// Colores de referencia del estilo OSM: suelo #F2EFE9, residencial #E0DFDF, edificio #D9D0C9,
// agua #AAD3DF, parque #C8FACC, hierba #CDEBB0, secundaria #F7FABF, primaria #FCD6A4.
Shader "PrecioLocal/TeselaPaleta"
{
    Properties
    {
        _MainTex ("Tesela", 2D) = "white" {}
        _Intensidad ("Intensidad del recoloreado", Range(0, 1)) = 1
        _Fondo ("Fondo", Color) = (0.914, 0.929, 0.969, 1)        // #E9EDF7
        _Agua ("Agua", Color) = (0.749, 0.816, 0.957, 1)          // #BFD0F4
        _Verde ("Zonas verdes", Color) = (0.863, 0.902, 0.933, 1) // #DCE6EE
        _Calle ("Calles", Color) = (1, 1, 1, 1)                   // #FFFFFF
        _Edificio ("Edificios", Color) = (0.827, 0.863, 0.949, 1) // #D3DCF2
        _Etiqueta ("Texto e iconos", Color) = (0.294, 0.345, 0.471, 1) // #4B5878
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "PaletaOSM.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            float _Intensidad;
            fixed4 _Fondo, _Agua, _Verde, _Calle, _Edificio, _Etiqueta;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                float3 col = RecolorearOSM(c, _Fondo.rgb, _Agua.rgb, _Verde.rgb, _Calle.rgb, _Edificio.rgb, _Etiqueta.rgb);
                return fixed4(lerp(c, col, _Intensidad), 1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
