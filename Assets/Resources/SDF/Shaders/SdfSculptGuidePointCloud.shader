Shader "Hidden/SdfSculptGuidePointCloud"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "SdfSculptGuidePointCloud"
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

            StructuredBuffer<float4> _Points;
            float _PointSizePx;
            int _PointCount;
            int _PointStride;
            TEXTURE3D(_GlobalTsdf3D);
            SAMPLER(sampler_linear_clamp);
            float3 _GlobalCorner;
            float3 _GlobalSize;
            float _GlobalMu;
            float _Alpha;
            float4x4 _WorldToWorkspace;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                uint pointIndex = (IN.vertexID / 6) * max(1, (uint)_PointStride);
                if (pointIndex >= (uint)_PointCount)
                {
                    OUT.positionCS = float4(0,0,0,0);
                    OUT.uv = 0;
                    OUT.posWS = 0;
                    return OUT;
                }
                uint localVID   = IN.vertexID % 6;

                float2 corners[6] = {
                    float2(-1,-1), float2( 1,-1), float2( 1, 1),
                    float2(-1,-1), float2( 1, 1), float2(-1, 1)
                };
                float2 c = corners[localVID];

                float3 posWS = _Points[pointIndex].xyz;
                float4 clip = TransformWorldToHClip(posWS);

                if (clip.w <= 0.0)
                {
                    OUT.positionCS = float4(0,0,0,0);
                    OUT.uv = 0;
                    OUT.posWS = posWS;
                    return OUT;
                }

                float2 ndcPerPixel = 2.0 / _ScreenParams.xy;
                float2 ndcOffset   = c * (_PointSizePx * 0.5) * ndcPerPixel;
                clip.xy += ndcOffset * clip.w;

                OUT.positionCS = clip;
                OUT.uv = c;
                OUT.posWS = posWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float dist = length(IN.uv);
                if (dist > 1.0)
                    discard;

                float alpha = _Alpha;

                float3 posWS = mul(_WorldToWorkspace, float4(IN.posWS, 1.0)).xyz;
                float3 uvw = (posWS - _GlobalCorner) / max(_GlobalSize, 1e-6);
                if (any(uvw < 0.0) || any(uvw > 1.0))
                    discard;

                float distTsdf = SAMPLE_TEXTURE3D(_GlobalTsdf3D, sampler_linear_clamp, uvw);
                float mu = max(_GlobalMu, 1e-6);
                float t = saturate(abs(distTsdf) / mu);
                half3 color = distTsdf >= 0.0
                    ? lerp(half3(0, 1, 0), half3(1, 0, 0), t)
                    : lerp(half3(0, 1, 0), half3(0, 0, 1), t);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
