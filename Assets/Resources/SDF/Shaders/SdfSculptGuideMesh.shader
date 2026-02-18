Shader "Hidden/SdfSculptGuideMesh"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "SdfSculptGuideMesh"
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE3D(_GlobalTsdf3D);
            SAMPLER(sampler_linear_clamp);
            float3 _GlobalCorner;
            float3 _GlobalSize;
            float _GlobalMu;
            float _Alpha;
            float4 _InsideColor;
            float4 _OutsideColor;
            float4 _SurfaceColor;

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

                float3 uvw = (IN.posWS - _GlobalCorner) / max(_GlobalSize, 1e-6);
                if (any(uvw < 0.0) || any(uvw > 1.0))
                    discard;

                float dist = SAMPLE_TEXTURE3D(_GlobalTsdf3D, sampler_linear_clamp, uvw);
                float mu = max(_GlobalMu, 1e-6);
                float t = saturate(abs(dist) / mu);
                half3 inside = lerp(_SurfaceColor.rgb, _InsideColor.rgb, t);
                half3 outside = lerp(_SurfaceColor.rgb, _OutsideColor.rgb, t);
                half3 color = dist >= 0.0 ? outside : inside;
                return half4(color, _Alpha);
            }
            ENDHLSL
        }
    }
}
