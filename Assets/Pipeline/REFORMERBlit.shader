Shader "Reformer/CustomBlit" {
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        ZWrite Off
        Cull Off
        ZTest Always
        
        Pass {
            Name "REFORMERBlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _OverlayRes;
            sampler2D _OverlayTex;
            float4 _OverlayTex_TexelSize;

            struct appdata {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.position = TransformObjectToHClip(v.position.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target {

                float2 quantuv = i.uv;
                quantuv = floor(quantuv * _OverlayRes) / _OverlayRes;

                // lol

				return tex2D(_MainTex, i.uv);
            }
            ENDHLSL
        }
    }
}