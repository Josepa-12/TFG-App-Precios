// Material del terreno de Cesium (escena "MapaCesium"): toma las teselas de OpenStreetMap que Cesium
// coloca encima del relieve (raster overlay con clave "0"), les quita los iconos de puntos de interés,
// las pinta con la paleta del proyecto y recibe las sombras de los edificios.
// Sin más iluminación, para que el suelo se vea plano y limpio como en el prototipo web.
//
// Cesium rellena por código las propiedades _overlayTexture_0, _overlayTranslationAndScale_0 y
// _overlayTextureCoordinateIndex_0 (mismos nombres que su CesiumDefaultTilesetShader). Los colores
// son globales (_PL_*), los pone TemaCesium.cs, así cambian a la vez en todas las teselas.
// Está en una carpeta Resources para que se incluya en las compilaciones (se busca con Shader.Find).
Shader "PrecioLocal/CesiumTerrenoPaleta"
{
    Properties
    {
        _overlayTexture_0 ("Mapa OSM (lo pone Cesium)", 2D) = "white" {}
        _overlayTranslationAndScale_0 ("Traslación y escala (lo pone Cesium)", Vector) = (0, 0, 1, 1)
        _overlayTextureCoordinateIndex_0 ("Índice de UV (lo pone Cesium)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "../PaletaOSM.cginc"

            sampler2D _overlayTexture_0;
            float4 _overlayTexture_0_TexelSize;
            float4 _overlayTranslationAndScale_0;
            float _overlayTextureCoordinateIndex_0;

            float4 _PL_Fondo, _PL_Agua, _PL_Verde, _PL_Calle, _PL_Edificio, _PL_Etiqueta, _PL_Sombra;
            float _PL_Intensidad, _PL_QuitarIconos;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; float2 uv2 : TEXCOORD2; float2 uv3 : TEXCOORD3;
            };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; SHADOW_COORDS(1) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                int i = (int)round(_overlayTextureCoordinateIndex_0);
                float2 uv = i == 0 ? v.uv0 : i == 1 ? v.uv1 : i == 2 ? v.uv2 : v.uv3;
                uv = _overlayTranslationAndScale_0.xy + uv * _overlayTranslationAndScale_0.zw;
                o.uv = float2(uv.x, 1.0 - uv.y); // Cesium invierte la V
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 osm = tex2D(_overlayTexture_0, i.uv);
                float3 c = lerp(_PL_Fondo.rgb, osm.rgb, osm.a);                     // sin tesela aún → fondo
                if (_PL_QuitarIconos > 0.5) c = QuitarIconosOSM(_overlayTexture_0, i.uv, _overlayTexture_0_TexelSize.xy, c);
                float3 col = RecolorearOSM(c, _PL_Fondo.rgb, _PL_Agua.rgb, _PL_Verde.rgb, _PL_Calle.rgb, _PL_Edificio.rgb, _PL_Etiqueta.rgb);
                col = lerp(c, col, _PL_Intensidad);
                // Sombra de los edificios: oscurece hacia el color de sombra de la paleta
                float luz = SHADOW_ATTENUATION(i);
                col = lerp(col * _PL_Sombra.rgb, col, luz);
                return fixed4(col, 1);
            }
            ENDCG
        }

        // Profundidad para las sombras en pantalla (el suelo no proyecta sombra, solo la recibe)
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
