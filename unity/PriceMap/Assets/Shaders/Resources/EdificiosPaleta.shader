// Material de los edificios extruidos del mapa vectorial (OpenStreetMap) con la paleta del proyecto.
// Tejado con el color de edificio de la paleta y fachadas más oscuras según la luz, para que cada
// bloque se distinga del suelo y de sus vecinos (como la extrusión del prototipo web).
// Proyecta sombra sobre el suelo (no la recibe, para evitar rayas de "acné de sombra").
// La normal se calcula por píxel a partir de la geometría, así no depende de las normales del modelo.
// Los colores son globales (_PL_*), los pone MapaVectorial.cs.
Shader "PriceMap/EdificiosPaleta"
{
    Properties { }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Off   // la normal se orienta sola hacia la cámara; así da igual el sentido de las caras
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            float4 _PL_Edificio, _PL_Fachada, _PL_Sombra;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 mundo : TEXCOORD0; SHADOW_COORDS(1) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.mundo = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(cross(ddy(i.mundo), ddx(i.mundo)));
                if (dot(n, _WorldSpaceCameraPos - i.mundo) < 0) n = -n;     // siempre hacia la cámara
                float tejado = smoothstep(0.6, 0.85, n.y);
                float luz = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
                float3 fachada = lerp(_PL_Fachada.rgb, _PL_Edificio.rgb, 0.35 + 0.65 * luz);
                // No recibe sombras: a la distancia del mapa la precisión no da y salen rayas ("acné");
                // el volumen ya lo marcan las fachadas y la sombra que proyecta sobre el suelo.
                return fixed4(lerp(fachada, _PL_Edificio.rgb, tejado), 1);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off   // las paredes extruidas no siempre tienen las caras bien orientadas
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
