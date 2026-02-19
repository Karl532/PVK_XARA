Shader "Hidden/SdfMatchOverlay"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Opaque" }
        Pass
        {
            Name "SdfMatchOverlay"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MatchMask2D);
            SAMPLER(sampler_linear_clamp);
            float4x4 _WorldToTracking;
            float4x4 _DepthViewProj;
            float2 _DepthSize;
            float _DepthFlipY;
            float4 _MatchColor;
            float _MatchAlpha;
            float _MatchSoftness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.posWS = posWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float4 trackingH = mul(_WorldToTracking, float4(IN.posWS, 1.0));
                float4 clip = mul(_DepthViewProj, trackingH);
                if (clip.w <= 1e-6)
                    discard;
                float2 ndc = clip.xy / clip.w;
                float2 uv = ndc * 0.5 + 0.5;
                if (_DepthFlipY > 0.5)
                    uv.y = 1.0 - uv.y;
                if (any(uv < 0.0) || any(uv > 1.0))
                    discard;

                float mask = SAMPLE_TEXTURE2D(_MatchMask2D, sampler_linear_clamp, uv);
                if (mask <= 0.01)
                    discard;

                float m = saturate(mask);
                if (_MatchSoftness > 0.0001)
                {
                    float lo = saturate(0.5 - _MatchSoftness);
                    float hi = saturate(0.5 + _MatchSoftness);
                    m = smoothstep(lo, hi, m);
                }
                else
                {
                    m = m > 0.01 ? 1.0 : 0.0;
                }
                return half4(_MatchColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
