Shader "Hidden/SdfMatchOverlay"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "SdfMatchOverlay"
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE3D(_MatchMask);
            SAMPLER(sampler_linear_clamp);
            float3 _MatchCorner;
            float3 _MatchSize;
            float4x4 _WorldToWorkspace;
            float4 _MatchColor;
            float _MatchAlpha;

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
                float3 ws = mul(_WorldToWorkspace, float4(IN.posWS, 1.0)).xyz;
                float3 uvw = (ws - _MatchCorner) / max(_MatchSize, 1e-6);
                if (any(uvw < 0.0) || any(uvw > 1.0))
                    discard;

                float mask = SAMPLE_TEXTURE3D(_MatchMask, sampler_linear_clamp, uvw);
                if (mask <= 0.01)
                    discard;

                float a = saturate(mask) * _MatchAlpha;
                return half4(_MatchColor.rgb, a);
            }
            ENDHLSL
        }
    }
}
