// Capas planas del mapa vectorial (escena "MapaVectorial"): suelo, zonas verdes, agua, bordes y calles.
// Color liso de la paleta y sombra de los edificios.
//
// Todas las capas están a ras de suelo (y = 0). Para que no parpadeen entre sí, el suelo escribe la
// profundidad (_ZWrite = 1, _ZTest = LEqual) y el resto se pinta encima en orden de cola, sin escribirla
// (_ZWrite = 0, _ZTest = Always). Los edificios se pintan después y tapan lo que tengan delante.
// Está en una carpeta Resources para que se incluya en las compilaciones (se busca con Shader.Find).
Shader "PrecioLocal/PlanoPaleta"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Enum(Off, 0, On, 1)] _ZWrite ("Escribe profundidad", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Prueba de profundidad", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Off
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            float4 _Color, _PL_Sombra;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; SHADOW_COORDS(0) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 col = _Color.rgb;
                return fixed4(lerp(col * _PL_Sombra.rgb, col, SHADOW_ATTENUATION(i)), 1);
            }
            ENDCG
        }

        // Profundidad para calcular las sombras en pantalla
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct v2f { V2F_SHADOW_CASTER; };
            v2f vert (appdata_base v) { v2f o; TRANSFER_SHADOW_CASTER(o) return o; }
            float4 frag (v2f i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
